using System.Reflection;
using System.Text.Json;

using Hexalith.EventStore.Server.Configuration;
using Hexalith.Tenants.IntegrationTests.Fixtures;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

/// <summary>Fast filesystem and host-registration controls; no real sidecar or benchmark runs.</summary>
public sealed class SnapshotPerformanceProgressTests
{
    [Fact]
    public void PrivateCheckpointContainsOnlyAuthoredPhasesAndBoundedAggregates()
    {
        using var scope = new ProgressScope();
        using var progress = SnapshotPerformanceProgress.Open(scope.Root, scope.Directory);
        progress.SetPhase(SnapshotPerformanceProgress.Phase.Seeding);
        progress.RecordAcceptedCommand(configuration: true);
        using JsonDocument document = scope.Read();
        document.RootElement.GetProperty("commandsAccepted").GetInt32().ShouldBe(1);
        document.RootElement.GetProperty("configurationWritesAccepted").GetInt32().ShouldBe(1);
        document.RootElement.GetProperty("tenantsSeeded").GetInt32().ShouldBe(0);
        document.RootElement.GetProperty("phase").GetString().ShouldBe("Seeding");
        document.RootElement.GetProperty("status").GetString().ShouldBe("incomplete");
        document.RootElement.GetProperty("elapsedSeconds").GetDouble().ShouldBeGreaterThanOrEqualTo(0);
        document.RootElement.GetProperty("completedPhaseSeconds").GetProperty("FixtureCreated").GetDouble().ShouldBeGreaterThanOrEqualTo(0);
        File.ReadAllText(scope.Path).ShouldNotContain(scope.Sentinel);
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            File.GetUnixFileMode(scope.Path).ShouldBe(UnixFileMode.UserRead | UnixFileMode.UserWrite);
        System.IO.Directory.EnumerateFiles(scope.Directory, "*.tmp").ShouldBeEmpty();
    }

    [Fact]
    public void SparseCheckpointsNeverCountUnacknowledgedOperations()
    {
        using var scope = new ProgressScope();
        using var progress = SnapshotPerformanceProgress.Open(scope.Root, scope.Directory);
        progress.SetPhase(SnapshotPerformanceProgress.Phase.Seeding);
        progress.RecordAcceptedCommand(configuration: false);
        byte[] first = File.ReadAllBytes(scope.Path);
        for (int i = 1; i < 499; i++) progress.RecordAcceptedCommand(configuration: false);
        File.ReadAllBytes(scope.Path).ShouldBe(first);
        Should.Throw<InvalidOperationException>(progress.RecordSeededTenant).Message.ShouldBe("performance-progress-invalid");
        progress.RecordAcceptedCommand(configuration: false);
        progress.RecordSeededTenant();
        progress.SetPhase(SnapshotPerformanceProgress.Phase.SeedingComplete);
        using JsonDocument document = scope.Read();
        document.RootElement.GetProperty("commandsAccepted").GetInt32().ShouldBe(500);
        document.RootElement.GetProperty("tenantsSeeded").GetInt32().ShouldBe(1);
    }

    [Fact]
    public void FixtureDisposalAndRecordedDurationCannotTurnAnIncompleteBenchmarkIntoPass()
    {
        using var scope = new ProgressScope();
        using var progress = SnapshotPerformanceProgress.Open(scope.Root, scope.Directory);
        progress.RecordRehydration(TimeSpan.FromSeconds(1));
        Should.Throw<InvalidOperationException>(progress.BenchmarkPassed).Message.ShouldBe("performance-progress-invalid");
        progress.FixtureDisposed();
        using JsonDocument document = scope.Read();
        document.RootElement.GetProperty("status").GetString().ShouldBe("incomplete");
        document.RootElement.GetProperty("benchmarkPassed").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public void OnlyCompleteSuccessfulCountsAssertionsAndDisposalProducePassedStatus()
    {
        using var scope = new ProgressScope();
        using var progress = SnapshotPerformanceProgress.Open(scope.Root, scope.Directory);
        progress.SetPhase(SnapshotPerformanceProgress.Phase.Seeding);
        // Counter-only controls, with the real workload's exact mix; no actor operations.
        for (int tenant = 0; tenant < 1_000; tenant++)
        {
            for (int command = 0; command < 500; command++) progress.RecordAcceptedCommand(configuration: command % 3 == 1);
            progress.RecordSeededTenant();
        }
        Should.Throw<InvalidOperationException>(() => progress.RecordAcceptedCommand(configuration: false));
        Should.Throw<InvalidOperationException>(progress.RecordSeededTenant);
        progress.SetPhase(SnapshotPerformanceProgress.Phase.RehydrationComplete);
        progress.RecordRehydration(TimeSpan.FromSeconds(30));
        Should.Throw<InvalidOperationException>(progress.BenchmarkPassed);
        progress.RecordRehydration(TimeSpan.FromSeconds(1));
        progress.BenchmarkPassed();
        using (JsonDocument beforeDisposal = scope.Read()) beforeDisposal.RootElement.GetProperty("status").GetString().ShouldBe("incomplete");
        progress.FixtureDisposed();
        using JsonDocument document = scope.Read();
        document.RootElement.GetProperty("commandsAccepted").GetInt32().ShouldBe(500_000);
        document.RootElement.GetProperty("configurationWritesAccepted").GetInt32().ShouldBe(167_000);
        document.RootElement.GetProperty("tenantsSeeded").GetInt32().ShouldBe(1_000);
        document.RootElement.GetProperty("status").GetString().ShouldBe("passed");
    }

    [Fact]
    public void FirstFailureSurvivesTeardownAndCheckpointRefusalWithoutRetainingExceptionText()
    {
        using var scope = new ProgressScope();
        using var progress = SnapshotPerformanceProgress.Open(scope.Root, scope.Directory);
        progress.SetPhase(SnapshotPerformanceProgress.Phase.Seeding);
        progress.Failed();
        progress.SetPhase(SnapshotPerformanceProgress.Phase.FixtureTeardown);
        progress.Failed();
        progress.FixtureDisposed();
        using (JsonDocument document = scope.Read())
        {
            document.RootElement.GetProperty("status").GetString().ShouldBe("failed");
            document.RootElement.GetProperty("failedPhase").GetString().ShouldBe("Seeding");
        }
        File.WriteAllText(scope.Path, scope.Sentinel);
        progress.Failed(); // Secondary reporting failure does not replace the original exception.
        File.ReadAllText(scope.Path).ShouldBe(scope.Sentinel);
        Exception refusal = Should.Throw<InvalidOperationException>(() => progress.SetPhase(SnapshotPerformanceProgress.Phase.FixtureTeardown));
        refusal.Message.ShouldBe("performance-progress-invalid");
        refusal.ToString().ShouldNotContain(scope.Sentinel);
        refusal.InnerException.ShouldBeNull();
    }

    [Fact]
    public async Task ConcurrentSuccessfulCountersPublishOnlyWholeAtomicDocuments()
    {
        using var scope = new ProgressScope();
        using var progress = SnapshotPerformanceProgress.Open(scope.Root, scope.Directory);
        progress.SetPhase(SnapshotPerformanceProgress.Phase.Seeding);
        Task writer = Task.Run(() => Parallel.For(0, 10_000, _ => progress.RecordAcceptedCommand(configuration: false)));
        int observations = 0;
        do
        {
            using JsonDocument document = scope.Read();
            int count = document.RootElement.GetProperty("commandsAccepted").GetInt32();
            (count is >= 0 and <= 10_000).ShouldBeTrue();
            observations++;
        } while (!writer.IsCompleted);
        await writer;
        observations.ShouldBeGreaterThan(0);
        using JsonDocument final = scope.Read();
        final.RootElement.GetProperty("commandsAccepted").GetInt32().ShouldBe(10_000);
    }

    [Theory]
    [InlineData("root")]
    [InlineData("ancestor")]
    [InlineData("sibling")]
    [InlineData("noncanonical")]
    [InlineData("directory-link")]
    [InlineData("file-link")]
    [InlineData("public-directory")]
    [InlineData("public-file")]
    [InlineData("foreign-document")]
    [InlineData("foreign-file")]
    [InlineData("foreign-owner")]
    [InlineData("missing-owner")]
    public void RefusesForeignOrNonprivatePathsWithoutModifyingThem(string mutation)
    {
        using var scope = new ProgressScope();
        string directory = scope.Directory;
        string original = File.ReadAllText(scope.Path);
        switch (mutation)
        {
            case "root": directory = scope.Root; break;
            case "ancestor": directory = System.IO.Directory.GetParent(scope.Root)!.FullName; break;
            case "sibling": directory = scope.Root + "-foreign"; break;
            case "noncanonical": directory += "/../" + System.IO.Path.GetFileName(directory); break;
            case "directory-link":
                directory = System.IO.Path.Combine(scope.Root, "tenants-performance-progress-12345678");
                System.IO.Directory.CreateSymbolicLink(directory, scope.Directory); break;
            case "file-link":
                File.Move(scope.Path, scope.Path + ".foreign");
                File.CreateSymbolicLink(scope.Path, scope.Path + ".foreign"); break;
            case "public-directory":
                if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()) File.SetUnixFileMode(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.OtherExecute);
                break;
            case "public-file":
                if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()) File.SetUnixFileMode(scope.Path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.OtherRead);
                break;
            case "foreign-document": File.WriteAllText(scope.Path, scope.Sentinel); original = scope.Sentinel; break;
            case "foreign-file": File.WriteAllText(System.IO.Path.Combine(directory, "foreign"), scope.Sentinel); break;
            case "foreign-owner": File.WriteAllText(System.IO.Path.Combine(directory, "owner"), scope.Sentinel); break;
            case "missing-owner": File.Delete(System.IO.Path.Combine(directory, "owner")); break;
        }
        Exception refusal = Should.Throw<InvalidOperationException>(() => SnapshotPerformanceProgress.Open(scope.Root, directory));
        refusal.Message.ShouldBe("performance-progress-invalid");
        refusal.ToString().ShouldNotContain(scope.Sentinel);
        refusal.InnerException.ShouldBeNull();
        File.ReadAllText(scope.Path).ShouldBe(original);
    }

    [Fact]
    public void ConcurrentWriterAndSubstitutedCheckpointAreRefused()
    {
        using var scope = new ProgressScope();
        using var progress = SnapshotPerformanceProgress.Open(scope.Root, scope.Directory);
        Should.Throw<InvalidOperationException>(() => SnapshotPerformanceProgress.Open(scope.Root, scope.Directory));
        string foreign = System.IO.Path.Combine(scope.Root, "foreign");
        File.WriteAllText(foreign, scope.Sentinel);
        File.Delete(scope.Path);
        File.CreateSymbolicLink(scope.Path, foreign);
        Should.Throw<InvalidOperationException>(() => progress.SetPhase(SnapshotPerformanceProgress.Phase.Seeding));
        progress.Failed();
        File.ReadAllText(foreign).ShouldBe(scope.Sentinel);
    }

    [Theory]
    [InlineData(null, null, 100, "Default")]
    [InlineData(75, null, 75, "Domain")]
    [InlineData(75, 125, 125, "TenantDomain")]
    public void ActualHostRegistrationObservesResolvedOptionsWithoutRetuningThem(int? domain, int? tenantDomain, int expected, string source)
    {
        using var scope = new ProgressScope();
        using var environment = scope.EnableEnvironment();
        var fixture = new TenantsDaprTestFixture();
        try
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = [] });
            if (domain.HasValue) builder.Configuration["EventStore:Snapshots:DomainIntervals:tenants"] = domain.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (tenantDomain.HasValue) builder.Services.Configure<SnapshotOptions>(options => options.TenantDomainIntervals["system:tenants"] = tenantDomain.Value);
            MethodInfo configure = typeof(TenantsDaprTestFixture).GetMethod("ConfigureDomain", BindingFlags.Instance | BindingFlags.NonPublic)!;
            configure.Invoke(fixture, [builder]);
            using ServiceProvider provider = builder.Services.BuildServiceProvider();
            SnapshotOptions options = provider.GetRequiredService<IOptions<SnapshotOptions>>().Value;
            options.DefaultInterval.ShouldBe(100);
            fixture.PerformanceProgress.SetPhase(SnapshotPerformanceProgress.Phase.FixtureReady);
            using JsonDocument document = scope.Read();
            document.RootElement.GetProperty("optionsSnapshotInterval").GetInt32().ShouldBe(expected);
            document.RootElement.GetProperty("snapshotIntervalSource").GetString().ShouldBe(source);
            document.RootElement.GetProperty("persistedPolicyResolverRegistered").GetBoolean().ShouldBeFalse();
            document.RootElement.GetProperty("effectiveSnapshotInterval").GetInt32().ShouldBe(expected);
        }
        finally { fixture.PerformanceProgress.Dispose(); }
    }

    [Fact]
    public void PersistedPolicyPresenceKeepsEffectiveIntervalUnqualified()
    {
        using var scope = new ProgressScope();
        using var progress = SnapshotPerformanceProgress.Open(scope.Root, scope.Directory);
        progress.RecordSnapshotInterval(100, SnapshotPerformanceProgress.IntervalSource.Default, persistedPolicyResolverRegistered: true);
        progress.SetPhase(SnapshotPerformanceProgress.Phase.HostStartup);
        using JsonDocument document = scope.Read();
        document.RootElement.GetProperty("optionsSnapshotInterval").GetInt32().ShouldBe(100);
        document.RootElement.GetProperty("effectiveSnapshotInterval").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public void FixtureInterfaceKeepsTheObservableLifecycleWrappers()
    {
        foreach (Type contract in new[] { typeof(IAsyncLifetime), typeof(IAsyncDisposable) })
        {
            InterfaceMapping map = typeof(TenantsDaprTestFixture).GetInterfaceMap(contract);
            MethodInfo[] lifecycle = map.TargetMethods.Where(method => method.Name is "InitializeAsync" or "DisposeAsync").ToArray();
            lifecycle.ShouldNotBeEmpty();
            lifecycle.ShouldAllBe(method => method.DeclaringType == typeof(TenantsDaprTestFixture));
        }
    }

    [Fact]
    public async Task ActualFrozenDisposerKeepsAnUnexecutedBenchmarkIncomplete()
    {
        using var scope = new ProgressScope();
        using var environment = scope.EnableEnvironment();
        using var ports = new EnvironmentScope(new Dictionary<string, string>
        {
            ["DAPR_HTTP_PORT"] = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? string.Empty,
            ["DAPR_GRPC_PORT"] = Environment.GetEnvironmentVariable("DAPR_GRPC_PORT") ?? string.Empty,
        });
        var fixture = new TenantsDaprTestFixture();
        await ((IAsyncDisposable)fixture).DisposeAsync();
        using JsonDocument document = scope.Read();
        document.RootElement.GetProperty("fixtureDisposed").GetBoolean().ShouldBeTrue();
        document.RootElement.GetProperty("benchmarkPassed").GetBoolean().ShouldBeFalse();
        document.RootElement.GetProperty("status").GetString().ShouldBe("incomplete");
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void ProgressEnrollmentRequiresBothOriginalPerformanceOptIns(bool eventStore, bool tenants)
    {
        using var scope = new ProgressScope();
        using var environment = scope.EnableEnvironment();
        Environment.SetEnvironmentVariable("HEXALITH_EVENTSTORE_RUN_PERFORMANCE_TESTS", eventStore ? "1" : "0");
        Environment.SetEnvironmentVariable("HEXALITH_TENANTS_RUN_PERFORMANCE_TESTS", tenants ? "1" : "0");
        Should.Throw<InvalidOperationException>(() => SnapshotPerformanceProgress.FromEnvironment()).Message.ShouldBe("performance-progress-invalid");
        File.ReadAllText(scope.Path).ShouldBe(SnapshotPerformanceProgress.InitialDocument);
    }

    private sealed class ProgressScope : IDisposable
    {
        internal string Sentinel { get; } = "credential-sentinel-" + Guid.NewGuid().ToString("N");
        internal string Root { get; }
        internal string Directory { get; }
        internal string Path => System.IO.Path.Combine(Directory, "progress.json");

        internal ProgressScope()
        {
            Root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "progress-control-" + Guid.NewGuid().ToString("N"));
            Directory = System.IO.Path.Combine(Root, "tenants-performance-progress-Ab12Cd34");
            if (OperatingSystem.IsWindows()) throw new InvalidOperationException("private-progress-platform-unavailable");
            System.IO.Directory.CreateDirectory(Directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            WritePrivate(Path, SnapshotPerformanceProgress.InitialDocument);
            WritePrivate(System.IO.Path.Combine(Directory, "owner"), SnapshotPerformanceProgress.OwnerDocument);
        }

        internal JsonDocument Read() => JsonDocument.Parse(File.ReadAllBytes(Path));
        internal IDisposable EnableEnvironment() => new EnvironmentScope(new Dictionary<string, string>
        {
            ["RUNNER_TEMP"] = Root,
            [SnapshotPerformanceProgress.DirectoryVariable] = Directory,
            ["HEXALITH_EVENTSTORE_RUN_PERFORMANCE_TESTS"] = "1",
            ["HEXALITH_TENANTS_RUN_PERFORMANCE_TESTS"] = "1",
        });
        public void Dispose() => System.IO.Directory.Delete(Root, recursive: true);

        private static void WritePrivate(string path, string content)
        {
            if (OperatingSystem.IsWindows()) throw new InvalidOperationException("private-progress-platform-unavailable");
            using var stream = new FileStream(path, new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write, UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite });
            using var writer = new StreamWriter(stream);
            writer.Write(content);
        }
    }

    private sealed class EnvironmentScope : IDisposable
    {
        private readonly Dictionary<string, string?> _previous;
        internal EnvironmentScope(Dictionary<string, string> values)
        {
            _previous = values.ToDictionary(pair => pair.Key, pair => Environment.GetEnvironmentVariable(pair.Key));
            foreach ((string key, string value) in values) Environment.SetEnvironmentVariable(key, value);
        }
        public void Dispose()
        {
            foreach ((string key, string? value) in _previous) Environment.SetEnvironmentVariable(key, value);
        }
    }
}
