using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Tenants.IntegrationTests.Fixtures;

/// <summary>Classifies owned bootstrap failures before cleanup, without retaining resource log text or credentials.</summary>
internal static class BootstrapResourceFailureDiagnostics
{
    private static readonly string[] Reasons = ["configuration-missing", "owner-trust-refused", "topology-disagreement", "installed-inventory-disagreement", "store-or-transport-unavailable", "catalog-disagreement", "catalog-invalid", "host-validation-unavailable"];
    private static readonly string[] SensitiveMarkers = ["Bearer ", "Authorization:", "password", "privatekey", "signingkey", "apikey", "api_key", "token="];

    internal sealed record Observation(string[] ExceptionTypes, string[] ApiSymbols, string[] CatalogReasons, bool Truncated);

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
        return new(exceptions.Order(StringComparer.Ordinal).ToArray(), symbols.Order(StringComparer.Ordinal).ToArray(), reasons.Order(StringComparer.Ordinal).ToArray(), truncated);
    }

    internal static Task<IReadOnlyDictionary<string, Observation>> ReadAsync(ResourceLoggerService logger, CancellationToken cancellationToken)
        => ReadAsync(logger.GetAllAsync, cancellationToken);

    internal static async Task<IReadOnlyDictionary<string, Observation>> ReadAsync(
        Func<string, IAsyncEnumerable<IReadOnlyList<LogLine>>> read, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Dictionary<string, Observation> observations = new(StringComparer.Ordinal);
        foreach (string resource in new[] { "eventstore", "tenants", "tenants-api" })
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

    internal static async Task CaptureAsync(AspireTopologyFixture fixture)
    {
        // The frozen legacy 3.128.4 fixture exposes no public application handle. Read its
        // pinned private handle solely for diagnostics; no configuration or service is changed.
        if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true"
            || Environment.GetEnvironmentVariable("RUNNER_TEMP") is not { Length: > 0 } temporary) return;
        try
        {
            DistributedApplication? app = typeof(AspireTopologyFixtureBase<Projects.Hexalith_Tenants_AppHost>)
                .GetField("_app", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(fixture) as DistributedApplication;
            if (app is null) return;
            using CancellationTokenSource bound = new(TimeSpan.FromSeconds(3));
            IReadOnlyDictionary<string, Observation> resources = await ReadAsync(app.Services.GetRequiredService<ResourceLoggerService>(), bound.Token);
            string json = JsonSerializer.Serialize(new { kind = "owned-bootstrap-failure-categories", fixtureVersion = typeof(AspireTopologyFixtureBase<>).Assembly.GetName().Version?.ToString(), resources });
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
