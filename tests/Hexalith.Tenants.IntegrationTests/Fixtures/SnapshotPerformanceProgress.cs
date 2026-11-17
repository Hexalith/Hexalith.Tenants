using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Hexalith.Tenants.IntegrationTests.Fixtures;

/// <summary>Private, support-safe checkpoints for the opt-in real actor benchmark.</summary>
internal sealed class SnapshotPerformanceProgress : IDisposable
{
    internal const string DirectoryVariable = "HEXALITH_TENANTS_PERFORMANCE_PROGRESS_DIRECTORY";
    internal const string InitialDocument = "{\"schemaVersion\":1,\"phase\":\"RunnerStarting\",\"status\":\"incomplete\"}\n";
    internal const string OwnerDocument = "tenants-performance-progress-v1\n";
    private const UnixFileMode PrivateDirectoryMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
    private const UnixFileMode PrivateFileMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
    private readonly object _sync = new();
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly Dictionary<string, double> _completedPhaseSeconds = [];
    private readonly string? _root;
    private readonly string? _directory;
    private readonly FileStream? _lease;
    private byte[]? _previousDocument;
    private Phase _phase = Phase.FixtureCreated;
    private Phase? _failedPhase;
    private double _phaseStarted;
    private int _commandsAccepted;
    private int _configurationWritesAccepted;
    private int _tenantsSeeded;
    private bool _benchmarkPassed;
    private bool _fixtureDisposed;
    private int? _snapshotInterval;
    private IntervalSource? _snapshotIntervalSource;
    private bool? _persistedPolicyResolverRegistered;
    private double? _rehydrationSeconds;

    internal enum Phase
    {
        FixtureCreated, FixtureInitialization, DomainConfiguration, HostStartup, FixtureReady,
        ExecutionGateWait, BenchmarkSetup, Seeding, SeedingComplete, TargetDeactivation,
        TargetDeactivated, Rehydration, RehydrationComplete, AssertionsPassed, FixtureTeardown, FixtureDisposed,
    }

    internal enum IntervalSource { Default, Domain, TenantDomain }

    private SnapshotPerformanceProgress() { }

    private SnapshotPerformanceProgress(string root, string directory)
    {
        _root = root;
        _directory = directory;
        ValidateDirectory();
        string path = Path.Combine(directory, "progress.json");
        byte[] initial = Encoding.UTF8.GetBytes(InitialDocument);
        string owner = Path.Combine(directory, "owner");
        if (Directory.EnumerateFileSystemEntries(directory).Any(entry => entry != path && entry != owner)
            || !IsPrivateFile(owner) || new FileInfo(owner).Length != Encoding.UTF8.GetByteCount(OwnerDocument)
            || File.ReadAllText(owner) != OwnerDocument
            || !IsPrivateFile(path) || new FileInfo(path).Length != initial.Length
            || !File.ReadAllBytes(path).AsSpan().SequenceEqual(initial))
        {
            throw InvalidProgress();
        }

        _previousDocument = initial;
        _lease = CreatePrivateFile(Path.Combine(directory, "writer.lock"));
        try { Write(); }
        catch { _lease.Dispose(); throw; }
    }

    internal static SnapshotPerformanceProgress FromEnvironment()
    {
        string? directory = Environment.GetEnvironmentVariable(DirectoryVariable);
        if (string.IsNullOrEmpty(directory)) return new();
        if (Environment.GetEnvironmentVariable("HEXALITH_EVENTSTORE_RUN_PERFORMANCE_TESTS") != "1"
            || Environment.GetEnvironmentVariable("HEXALITH_TENANTS_RUN_PERFORMANCE_TESTS") != "1")
        {
            throw InvalidProgress();
        }

        return Open(Environment.GetEnvironmentVariable("RUNNER_TEMP") ?? string.Empty, directory);
    }

    internal static SnapshotPerformanceProgress Open(string root, string directory)
    {
        try { return new(root, directory); }
        catch { throw InvalidProgress(); }
    }

    internal void SetPhase(Phase phase)
    {
        lock (_sync)
        {
            if (_directory is null) return;
            if (!Enum.IsDefined(phase)) throw InvalidProgress();
            double now = _clock.Elapsed.TotalSeconds;
            string previous = _phase.ToString();
            _completedPhaseSeconds[previous] = _completedPhaseSeconds.GetValueOrDefault(previous) + now - _phaseStarted;
            _phase = phase;
            _phaseStarted = now;
            Write();
        }
    }

    internal void RecordAcceptedCommand(bool configuration)
    {
        lock (_sync)
        {
            if (_directory is null) return;
            if (_phase != Phase.Seeding || _commandsAccepted >= 500_000 || (configuration && _configurationWritesAccepted >= 167_000)) throw InvalidProgress();
            _commandsAccepted++;
            if (configuration) _configurationWritesAccepted++;
            // At most 101 command checkpoints for the full workload, outside rehydration.
            if (_commandsAccepted == 1 || _commandsAccepted % 5_000 == 0) Write();
        }
    }

    internal void RecordSeededTenant()
    {
        lock (_sync)
        {
            if (_directory is null) return;
            if (_phase != Phase.Seeding || _tenantsSeeded >= 1_000 || _commandsAccepted < (_tenantsSeeded + 1) * 500) throw InvalidProgress();
            _tenantsSeeded++;
            if (_tenantsSeeded % 10 == 0) Write();
        }
    }

    internal void RecordSnapshotInterval(int interval, IntervalSource source, bool persistedPolicyResolverRegistered)
    {
        lock (_sync)
        {
            if (_directory is null) return;
            if (interval < 10 || !Enum.IsDefined(source)) throw InvalidProgress();
            _snapshotInterval = interval;
            _snapshotIntervalSource = source;
            _persistedPolicyResolverRegistered = persistedPolicyResolverRegistered;
            // Options may be materialized lazily. Their observation performs no checkpoint I/O.
        }
    }

    internal void RecordRehydration(TimeSpan elapsed)
    {
        lock (_sync)
        {
            if (_directory is null) return;
            if (elapsed < TimeSpan.Zero || !double.IsFinite(elapsed.TotalSeconds)) throw InvalidProgress();
            _rehydrationSeconds = elapsed.TotalSeconds;
            Write();
        }
    }

    internal void BenchmarkPassed()
    {
        lock (_sync)
        {
            if (_directory is null) return;
            if (_phase != Phase.RehydrationComplete || _commandsAccepted != 500_000 || _configurationWritesAccepted != 167_000 || _tenantsSeeded != 1_000
                || _rehydrationSeconds is null or >= 30 || _failedPhase is not null) throw InvalidProgress();
            _benchmarkPassed = true;
            SetPhase(Phase.AssertionsPassed);
        }
    }

    internal void Failed()
    {
        // Failure reporting must never replace a real actor/assertion/lifecycle exception.
        try
        {
            lock (_sync)
            {
                if (_directory is null) return;
                _failedPhase ??= _phase;
                Write();
            }
        }
        catch { }
    }

    internal void FixtureDisposed()
    {
        lock (_sync)
        {
            _fixtureDisposed = true;
            SetPhase(Phase.FixtureDisposed);
        }
    }

    public void Dispose() => _lease?.Dispose();

    private void Write()
    {
        try { WriteCore(); }
        catch { throw InvalidProgress(); }
    }

    private void WriteCore()
    {
        ValidateDirectory();
        string path = Path.Combine(_directory!, "progress.json");
        if (!IsPrivateFile(path) || new FileInfo(path).Length != _previousDocument!.Length
            || !File.ReadAllBytes(path).AsSpan().SequenceEqual(_previousDocument)) throw InvalidProgress();
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(new
        {
            schemaVersion = 1,
            phase = _phase.ToString(),
            status = _failedPhase is not null ? "failed" : _benchmarkPassed && _fixtureDisposed ? "passed" : "incomplete",
            failedPhase = _failedPhase?.ToString(),
            benchmarkPassed = _benchmarkPassed,
            fixtureDisposed = _fixtureDisposed,
            commandsAccepted = _commandsAccepted,
            configurationWritesAccepted = _configurationWritesAccepted,
            tenantsSeeded = _tenantsSeeded,
            elapsedSeconds = _clock.Elapsed.TotalSeconds,
            phaseElapsedSeconds = _clock.Elapsed.TotalSeconds - _phaseStarted,
            completedPhaseSeconds = _completedPhaseSeconds,
            rehydrationSeconds = _rehydrationSeconds,
            optionsSnapshotInterval = _snapshotInterval,
            persistedPolicyResolverRegistered = _persistedPolicyResolverRegistered,
            effectiveSnapshotInterval = _persistedPolicyResolverRegistered == false ? _snapshotInterval : null,
            snapshotIntervalSource = _snapshotIntervalSource?.ToString(),
            workload = new { tenants = 1_000, commandsPerTenant = 500, configurationKeys = 100, concurrency = 50, documentedSnapshotInterval = 50, rehydrationLimitSeconds = 30 },
        });
        string staging = Path.Combine(_directory!, $"checkpoint-{Guid.NewGuid():N}.tmp");
        try
        {
            using (FileStream stream = CreatePrivateFile(staging))
            {
                stream.Write(bytes);
                stream.Flush(flushToDisk: true);
            }

            // Revalidate after staging, before replacing only our exact previous checkpoint.
            ValidateDirectory();
            if (!IsPrivateFile(path) || new FileInfo(path).Length != _previousDocument.Length
                || !File.ReadAllBytes(path).AsSpan().SequenceEqual(_previousDocument)) throw InvalidProgress();
            File.Move(staging, path, overwrite: true);
            _previousDocument = bytes;
        }
        catch { throw InvalidProgress(); }
        finally { if (IsPrivateFile(staging)) File.Delete(staging); }
    }

    private void ValidateDirectory()
    {
        if (OperatingSystem.IsWindows()) throw InvalidProgress();
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS()) throw InvalidProgress();
        if (string.IsNullOrEmpty(_root) || string.IsNullOrEmpty(_directory)
            || !Path.IsPathFullyQualified(_root) || !Path.IsPathFullyQualified(_directory)
            || Path.GetFullPath(_root) != _root || Path.GetFullPath(_directory) != _directory
            || Path.GetDirectoryName(_directory) != _root
            || !System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(_directory), "^tenants-performance-progress-[A-Za-z0-9]{8}$", System.Text.RegularExpressions.RegexOptions.CultureInvariant)
            || !Directory.Exists(_directory) || File.GetUnixFileMode(_directory) != PrivateDirectoryMode) throw InvalidProgress();
        for (DirectoryInfo? entry = new(_directory); entry is not null; entry = entry.Parent)
        {
            if (entry.LinkTarget is not null || (entry.Attributes & FileAttributes.ReparsePoint) != 0) throw InvalidProgress();
        }
    }

    private static bool IsPrivateFile(string path)
        => (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()) && File.Exists(path)
            && new FileInfo(path).LinkTarget is null && (File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0
            && File.GetUnixFileMode(path) == PrivateFileMode;

    private static FileStream CreatePrivateFile(string path)
    {
        if (OperatingSystem.IsWindows()) throw InvalidProgress();
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS()) throw InvalidProgress();
        return new(path, new FileStreamOptions
        {
            Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.None,
            UnixCreateMode = PrivateFileMode,
        });
    }

    private static InvalidOperationException InvalidProgress() => new("performance-progress-invalid");
}
