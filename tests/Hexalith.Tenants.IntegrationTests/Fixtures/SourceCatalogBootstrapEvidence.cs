#if HEXALITH_DEVELOPMENT_CATALOG_COHORT
using System.Net;
using System.Text.Json;

using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Hexalith.Tenants.Bootstrap;
using Hexalith.Tenants.Contracts.Identity;

using StackExchange.Redis;

namespace Hexalith.Tenants.IntegrationTests.Fixtures;

/// <summary>Reads the actual isolated authority and the actual hosted bootstrap receipt; it never manufactures a caller or submits an internal human command.</summary>
internal static class SourceCatalogBootstrapEvidence
{
    internal static async Task<(RouteCatalogActivationState State, string Directory)> ReadCommittedAsync(CancellationToken token)
    {
        string component = Environment.GetEnvironmentVariable("Tenants__DevelopmentRouteCatalog__AuthorityComponentName")
            ?? throw new InvalidOperationException("source-catalog-authority-unconfigured");
        string key = Environment.GetEnvironmentVariable("Tenants__DevelopmentRouteCatalog__AuthorityStateKey")
            ?? throw new InvalidOperationException("source-catalog-authority-unconfigured");
        // Dapr's real keyPrefix=name format is component||key. See its v1.18
        // state-sharing contract; no application prefix or alternate key is guessed.
        using var redis = await ConnectionMultiplexer.ConnectAsync("localhost:6379,abortConnect=false,connectTimeout=5000,asyncTimeout=5000").WaitAsync(token);
        while (true)
        {
            token.ThrowIfCancellationRequested();
            RedisValue value = await redis.GetDatabase().HashGetAsync(component + "||" + key, "data").WaitAsync(token);
            if (!value.IsNull && value.ToString().Length <= 16 * 1024 * 1024)
            {
                var state = JsonSerializer.Deserialize<RouteCatalogActivationState>(value.ToString(), JsonSerializerOptions.Web);
                if (state is { Committed: { } committed, Pending: null, Epoch: > 0 })
                {
                    string[] directories = System.IO.Directory.EnumerateDirectories(Path.GetTempPath(), "hexalith-route-catalog-*")
                        .Where(directory => Matches(directory, committed)).ToArray();
                    if (directories.Length > 1) throw new InvalidOperationException("source-catalog-run-ambiguous");
                    if (directories.Length == 1) return (state, directories[0]);
                }
            }
            await Task.Delay(100, token);
        }
    }

    internal static async Task<(long? Sequence, string? Failure)> WaitForOwnedBootstrapAsync(HttpClient commandApi,
        string humanToken, bool requireColdBootstrap, CancellationToken token)
    {
        var (state, directory) = await ReadCommittedAsync(token);
        while (true)
        {
            string path = Path.Combine(directory, "tenants.bootstrap.json");
            if (File.Exists(path))
            {
                TenantBootstrapReceipt? receipt = null;
                try { receipt = JsonSerializer.Deserialize<TenantBootstrapReceipt>(ReadPrivate(path, 64 * 1024)); }
                catch (JsonException) { }
                if (receipt is not null && receipt.Phase != "attempted")
                {
                    if (receipt.AttemptId != state.Committed!.AttemptId || receipt.RootDigest != state.Committed.RootDigest
                        || receipt.PrincipalId != AuthenticatedRouteState.CallerId || receipt.Phase != "accepted"
                        || receipt.HttpStatus != (int)HttpStatusCode.Accepted)
                        return (null, "bootstrap-owned-receipt-invalid");
                    var status = await AspireTopologyTests.WaitForTerminalStatusAsync(commandApi, receipt.CorrelationId, humanToken, token);
                    if (status.CorrelationId != receipt.CorrelationId || status.MessageId is not null && status.MessageId != receipt.MessageId
                        || status.Domain is not null && status.Domain != TenantIdentity.GlobalAdministratorsDomain || status.AggregateId != TenantIdentity.GlobalAdministratorsAggregateId
                        || status.Status != "Completed" || status.StatusCode != (int)Hexalith.EventStore.Contracts.Commands.CommandStatus.Completed || status.EventCount != 1 || status.CommittedEventSequence is not > 0)
                        return (null, "bootstrap-owned-terminal-invalid");
                    return requireColdBootstrap && status.CommittedEventSequence != 1
                        ? (null, "bootstrap-cold-state-required") : (status.CommittedEventSequence, null);
                }
            }
            await Task.Delay(100, token);
        }
    }

    internal static byte[] ReadPrivate(string path, int maximum)
    {
        var file = new FileInfo(path);
        if (!file.Exists || file.LinkTarget is not null || (file.Attributes & FileAttributes.ReparsePoint) != 0
            || file.Length > maximum || !OperatingSystem.IsWindows() && (File.GetUnixFileMode(path) & (UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute
                | UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute)) != 0)
            throw new InvalidOperationException("source-catalog-private-evidence-invalid");
        using var source = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var bytes = new MemoryStream();
        byte[] buffer = new byte[8192];
        while (source.Read(buffer) is int count && count > 0)
        {
            if (bytes.Length + count > maximum) throw new InvalidOperationException("source-catalog-evidence-limit");
            bytes.Write(buffer, 0, count);
        }
        return bytes.ToArray();
    }

    private static bool Matches(string directory, RouteCatalogActivation activation)
    {
        var entry = new DirectoryInfo(directory);
        if (entry.LinkTarget is not null || (entry.Attributes & FileAttributes.ReparsePoint) != 0) return false;
        string path = Path.Combine(directory, "receipt.json");
        if (!File.Exists(path)) return false;
        using var document = JsonDocument.Parse(ReadPrivate(path, 64 * 1024));
        JsonElement root = document.RootElement;
        return root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty("instanceId", out var instance) && instance.ValueKind == JsonValueKind.String && instance.GetString() == activation.AttemptId
            && root.TryGetProperty("rootDigest", out var digest) && digest.ValueKind == JsonValueKind.String && digest.GetString() == activation.RootDigest
            && root.TryGetProperty("bindingVersion", out var version) && version.ValueKind == JsonValueKind.Number && version.TryGetInt32(out int value) && value == 2;
    }
}
#endif
