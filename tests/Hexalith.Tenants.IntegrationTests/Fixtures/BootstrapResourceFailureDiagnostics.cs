using System.Text.Json;
using System.Text.RegularExpressions;

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Tenants.IntegrationTests.Fixtures;

/// <summary>Classifies owned bootstrap failures before cleanup, without retaining resource log text or credentials.</summary>
internal static class BootstrapResourceFailureDiagnostics
{
    private static readonly string[] Resources = ["eventstore", "tenants", "tenants-api"];
    private static readonly string[] CatalogHosts = ["eventstore", "tenants", "tenants-api", "eventstore-admin"];
    private static readonly string[] Reasons = ["configuration-missing", "owner-trust-refused", "topology-disagreement", "installed-inventory-disagreement", "store-or-transport-unavailable", "catalog-disagreement", "catalog-invalid", "host-validation-unavailable"];
    private static readonly string[] SensitiveMarkers = ["Bearer ", "Authorization:", "password", "privatekey", "signingkey", "apikey", "api_key", "token="];

    internal sealed record Observation(string[] ExceptionTypes, string[] ApiSymbols, string[] CatalogReasons, bool Truncated, int CapturedLineCount);

    internal static Observation Classify(IEnumerable<string> lines)
    {
        HashSet<string> exceptions = new(StringComparer.Ordinal);
        HashSet<string> symbols = new(StringComparer.Ordinal);
        HashSet<string> reasons = new(StringComparer.Ordinal);
        bool truncated = false;
        int count = 0;
        foreach (string line in lines)
        {
            if (++count > 2048) { truncated = true; break; }
            if (line.Length > 8192) { truncated = true; continue; }
            if (SensitiveMarkers.Any(marker => line.Contains(marker, StringComparison.OrdinalIgnoreCase))) continue;
            Match[] found = Regex.Matches(line, @"\b(?:System|Microsoft|Hexalith|Dapr)(?:\.[A-Za-z_][A-Za-z0-9_+`]*?)*(?:Exception|Error)\b", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100)).Cast<Match>().ToArray();
            string[] categories = Reasons.Where(reason => line.Contains(reason, StringComparison.Ordinal)).ToArray();
            if (found.Length == 0 && categories.Length == 0) continue;
            foreach (Match match in found.Take(16)) if (exceptions.Count < 16) exceptions.Add(match.Value);
            foreach (string reason in categories) reasons.Add(reason);
            foreach (Match match in Regex.Matches(line, @"\b(?:System|Microsoft|Hexalith|Dapr)\.[A-Za-z_][A-Za-z0-9_.+`]{1,180}", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100)).Cast<Match>().Take(32))
                if (symbols.Count < 32) symbols.Add(match.Value);
        }
        return new(exceptions.Order(StringComparer.Ordinal).ToArray(), symbols.Order(StringComparer.Ordinal).ToArray(), reasons.Order(StringComparer.Ordinal).ToArray(), truncated, Math.Min(count, 2048));
    }

    internal static Task<IReadOnlyDictionary<string, Observation>> ReadAsync(ResourceLoggerService logger, CancellationToken cancellationToken)
        => ReadAsync(logger.GetAllAsync, cancellationToken);

    internal static async Task<IReadOnlyDictionary<string, Observation>> ReadAsync(
        Func<string, IAsyncEnumerable<IReadOnlyList<LogLine>>> read, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Dictionary<string, Observation> observations = new(StringComparer.Ordinal);
        foreach (string resource in Resources)
        {
            List<string> lines = [];
            await foreach (IReadOnlyList<LogLine> batch in read(resource).WithCancellation(cancellationToken))
            {
                lines.AddRange(batch.Select(line => line.Content).Take(2049 - lines.Count));
                if (lines.Count > 2048) break;
            }
            observations.Add(resource, Classify(lines));
        }
        return observations;
    }

    /// <summary>Reduces rendered environment and private file-presence facts to booleans and bounded counts only.</summary>
    internal static BootstrapCatalogEnrollmentObservation ClassifyEnrollment(
        CustomResourceSnapshot? snapshot, string resource, string privateRoot)
    {
        if (!Resources.Contains(resource, StringComparer.Ordinal)) throw new ArgumentException("Unknown diagnostic resource.", nameof(resource));
        bool environmentObserved = snapshot is not null && !snapshot.EnvironmentVariables.IsDefaultOrEmpty;
        string? Value(string name)
        {
            if (!environmentObserved) return null;
            var entries = snapshot!.EnvironmentVariables.Where(entry => entry.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).ToArray();
            return entries.Length == 1 ? entries[0].Value : null;
        }
        string? Setting(string name) => Value("EventStore__RouteCatalog__DevelopmentBootstrap__" + name);
        string? directory = Setting("Directory");
        bool privateDirectory = IsPrivateCatalogDirectory(directory, privateRoot);
        bool FilePresent(string? path) => privateDirectory && IsRegularCatalogFile(path, directory!);
        bool Present(string name) => FilePresent(Path.Combine(directory!, name));
        int Count(string suffix) => privateDirectory ? CatalogHosts.Count(host => Present(host + suffix)) : 0;
        return new(snapshot is not null, environmentObserved,
            Value("DOTNET_ENVIRONMENT") == "Development" || Value("ASPNETCORE_ENVIRONMENT") == "Development",
            !string.IsNullOrEmpty(directory), !string.IsNullOrEmpty(Setting("CatalogPath")),
            !string.IsNullOrEmpty(Setting("ComponentPath")), !string.IsNullOrEmpty(Setting("AccessControlPath")),
            Setting("BindingVersion") == "2", Setting("HostId") == resource, privateDirectory,
            privateDirectory && Setting("CatalogPath") == Path.Combine(directory!, "catalog.json") && Present("catalog.json"),
            FilePresent(Setting("ComponentPath")), FilePresent(Setting("AccessControlPath")),
            privateDirectory && Present(resource + ".observation.json"),
            Count(".observation.json"), Count(".ready.json"), Count(".committed.json"),
            privateDirectory && Present("activation.json"), privateDirectory && Present("tenants.bootstrap.json"));
    }

    /// <summary>Reads current allocated-instance snapshots without evaluating model environment callbacks or retaining their values.</summary>
    internal static async Task<IReadOnlyDictionary<string, BootstrapCatalogEnrollmentObservation>> ReadEnrollmentAsync(
        IAsyncEnumerable<ResourceEvent> snapshots, string privateRoot, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var observations = Resources.ToDictionary(name => name, name => ClassifyEnrollment(null, name, privateRoot), StringComparer.Ordinal);
        int count = 0;
        try
        {
            await foreach (ResourceEvent resource in snapshots.WithCancellation(cancellationToken))
            {
                if (++count > 512) break;
                if (!observations.TryGetValue(resource.Resource.Name, out var previous)) continue;
                var current = ClassifyEnrollment(resource.Snapshot, resource.Resource.Name, privateRoot);
                if (!previous.EnvironmentObserved || current.EnvironmentObserved) observations[resource.Resource.Name] = current;
                if (observations.Values.All(value => value.EnvironmentObserved)) break;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // A bounded partial snapshot is explicitly marked unobserved; it never proves absence.
        }
        return observations;
    }

    private static bool IsPrivateCatalogDirectory(string? path, string privateRoot)
    {
        if (string.IsNullOrEmpty(path) || !Path.IsPathFullyQualified(path) || !Path.IsPathFullyQualified(privateRoot)) return false;
        string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(privateRoot));
        if (Path.GetFullPath(path) != path || Path.GetDirectoryName(path) != root
            || !Path.GetFileName(path).StartsWith("hexalith-route-catalog-", StringComparison.Ordinal)) return false;
        var directory = new DirectoryInfo(path);
        var parent = new DirectoryInfo(root);
        return directory.Exists && parent.Exists && directory.LinkTarget is null && parent.LinkTarget is null
            && (directory.Attributes & FileAttributes.ReparsePoint) == 0 && (parent.Attributes & FileAttributes.ReparsePoint) == 0
            && (OperatingSystem.IsWindows() || (File.GetUnixFileMode(path) & (UnixFileMode.GroupRead | UnixFileMode.GroupWrite
                | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute)) == 0);
    }

    private static bool IsRegularCatalogFile(string? path, string directory)
    {
        if (string.IsNullOrEmpty(path) || !Path.IsPathFullyQualified(path) || Path.GetFullPath(path) != path
            || Path.GetDirectoryName(path) != directory) return false;
        var file = new FileInfo(path);
        return file.Exists && file.LinkTarget is null && (file.Attributes & (FileAttributes.ReparsePoint | FileAttributes.Directory)) == 0;
    }

    internal static async Task CaptureAsync(AspireTopologyFixture fixture)
    {
        if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true"
            || Environment.GetEnvironmentVariable("RUNNER_TEMP") is not { Length: > 0 } temporary) return;
        try
        {
            DistributedApplication? app = fixture.Application;
            if (app is null) return;
            using CancellationTokenSource bound = new(TimeSpan.FromSeconds(3));
            ResourceLoggerService logger = app.Services.GetRequiredService<ResourceLoggerService>();
            DistributedApplicationModel model = app.Services.GetRequiredService<DistributedApplicationModel>();
            // Resolve each logical resource to its allocated DCP instance names through Aspire's
            // resource overload; a project replica does not write to the logical-name stream.
            IReadOnlyDictionary<string, Observation> resources = await ReadAsync(
                name => logger.GetAllAsync(model.Resources.Single(resource => resource.Name == name)), bound.Token);
            using CancellationTokenSource snapshotBound = new(TimeSpan.FromSeconds(1));
            ResourceNotificationService notifications = app.Services.GetRequiredService<ResourceNotificationService>();
            var enrollment = await ReadEnrollmentAsync(notifications.WatchAsync(snapshotBound.Token), Path.GetTempPath(), snapshotBound.Token);
            string json = JsonSerializer.Serialize(new
            {
                kind = "owned-bootstrap-failure-categories", source = "Aspire.GetAllAsync(IResource)",
                fixtureVersion = typeof(TenantsAspireHarness<>).Assembly.GetName().Version?.ToString(), resources,
                enrollmentSource = "Aspire.ResourceNotificationService.WatchAsync", enrollment,
            });
            string directory = Path.Combine(temporary, "tenants-bootstrap-diagnostics");
            Directory.CreateDirectory(directory);
            await File.WriteAllTextAsync(Path.Combine(directory, Guid.NewGuid().ToString("N") + ".json"), json, bound.Token);
            Console.Error.WriteLine("TENANTS_BOOTSTRAP_DIAGNOSTICS " + json);
        }
        catch (Exception exception)
        {
            // Observability must never replace the bootstrap failure or prevent its owned cleanup.
            Console.Error.WriteLine("TENANTS_BOOTSTRAP_DIAGNOSTICS_UNAVAILABLE " + exception.GetType().Name);
        }
    }
}
