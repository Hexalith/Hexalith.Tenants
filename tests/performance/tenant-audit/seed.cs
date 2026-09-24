using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;

using Dapr.Client;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Server.Projections;

const string generatorRevision = "tenant-audit-seed-v2";
const int randomSeed = 5101;
const string storeName = "statestore";

bool replay = args.Length == 2 && args[0] == "--replay";
bool verifyReplay = args.Length == 2 && args[0] == "--verify-replay";
bool fresh = args.Length == 3 && !args[0].StartsWith("--", StringComparison.Ordinal);
if (!fresh && !replay && !verifyReplay)
{
    throw new ArgumentException("Usage: seed <isolated-tenant-id> <UTC-anchor-ISO-8601> <manifest-path> | --replay <original-manifest-path> | --verify-replay <original-manifest-path>");
}

string manifestPath = fresh ? args[2] : args[1];
JsonDocument? sourceManifest = fresh ? null : JsonDocument.Parse(await File.ReadAllTextAsync(manifestPath));
JsonElement sourceDataset = sourceManifest?.RootElement.GetProperty("dataset") ?? default;
string tenantId = fresh ? args[0] : sourceDataset.GetProperty("tenantId").GetString()!;
string anchorText = fresh ? args[1] : sourceDataset.GetProperty("anchorUtc").GetString()!;
if (!DateTimeOffset.TryParse(anchorText, out DateTimeOffset anchor)
    || anchor.Offset != TimeSpan.Zero || string.IsNullOrWhiteSpace(tenantId))
{
    throw new ArgumentException("A nonempty tenant ID and UTC anchor are required.");
}
DateTimeOffset? replayProjectedAt = null;
if (!fresh)
{
    string projectedAtText = sourceDataset.GetProperty("projectedAtUtc").GetString()!;
    if (!DateTimeOffset.TryParse(projectedAtText, out DateTimeOffset projectedAt)
        || projectedAt.Offset != TimeSpan.Zero)
    {
        throw new ArgumentException("Replay manifest must contain a UTC projection freshness timestamp.");
    }
    replayProjectedAt = projectedAt;
}
var eventKinds = new List<string>(500);
AddKinds(nameof(UserAddedToTenant), 100);
AddKinds(nameof(UserRoleChanged), 100);
AddKinds(nameof(UserRemovedFromTenant), 50);
AddKinds(nameof(TenantCreated), 1);
AddKinds(nameof(TenantUpdated), 99);
AddKinds(nameof(TenantConfigurationSet), 100);
AddKinds(nameof(TenantConfigurationRemoved), 50);

var random = new Random(randomSeed);
for (int i = eventKinds.Count - 1; i > 0; i--)
{
    int j = random.Next(i + 1);
    (eventKinds[i], eventKinds[j]) = (eventKinds[j], eventKinds[i]);
}

var events = new List<ProjectionEventDto>(eventKinds.Count);
for (int i = 0; i < eventKinds.Count; i++)
{
    // Equal timestamps at every 25th entry exercise the event-reference tie break.
    int timeIndex = i > 0 && i % 25 == 0 ? i - 1 : i;
    DateTimeOffset timestamp = anchor.AddDays(-30).AddTicks(TimeSpan.FromDays(30).Ticks * timeIndex / 499);
    string userId = $"audit-user-{i % 100:D5}";
    string configKey = $"audit-config-key-{i % 100:D4}";
    object payload = eventKinds[i] switch
    {
        nameof(UserAddedToTenant) => new UserAddedToTenant(tenantId, userId, TenantRole.TenantReader),
        nameof(UserRoleChanged) => new UserRoleChanged(tenantId, userId, TenantRole.TenantReader, TenantRole.TenantContributor),
        nameof(UserRemovedFromTenant) => new UserRemovedFromTenant(tenantId, userId),
        nameof(TenantCreated) => new TenantCreated(tenantId, "Audit Performance", "Synthetic audit projection", timestamp),
        nameof(TenantUpdated) => new TenantUpdated(tenantId, "Audit Performance", "Synthetic audit projection", timestamp),
        nameof(TenantConfigurationSet) => new TenantConfigurationSet(tenantId, configKey, "synthetic-value"),
        nameof(TenantConfigurationRemoved) => new TenantConfigurationRemoved(tenantId, configKey),
        _ => throw new InvalidOperationException("Unexpected event kind."),
    };
    events.Add(new ProjectionEventDto(
        payload.GetType().FullName!, JsonSerializer.SerializeToUtf8Bytes(payload), "json", i + 1,
        timestamp, $"audit-correlation-{i:D4}", $"audit-event-{i:D6}", $"audit-actor-{i % 100:D4}"));
}

TenantAuditReadModel projection = TenantAuditProjection.ProjectAuditEvents(events);
projection.ProjectedAt = replayProjectedAt ?? DateTimeOffset.UtcNow;
projection.ProjectionVersion = $"{generatorRevision}-{anchor:yyyyMMddHHmmss}";
if (projection.Entries.Count != 500)
{
    throw new InvalidOperationException("Seed generator did not create exactly 500 supported entries.");
}

var manifest = new
{
    generatorRevision,
    randomSeed,
    tenantId,
    anchorUtc = anchor.ToString("O"),
    projectedAtUtc = projection.ProjectedAt?.ToString("O"),
    pageSize = 50,
    mix = eventKinds.GroupBy(kind => kind).ToDictionary(group => group.Key, group => group.Count()),
    entries = projection.Entries.Select(entry => new
    {
        reference = entry.EventId,
        timestampUtc = entry.Timestamp.ToUniversalTime().ToString("O"),
        category = entry.Category.ToString(),
        eventType = entry.EventType,
    }).ToArray(),
};
byte[] manifestBytes = JsonSerializer.SerializeToUtf8Bytes(manifest,
    new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
string hash = Convert.ToHexString(SHA256.HashData(manifestBytes)).ToLowerInvariant();
if (!fresh)
{
    byte[] originalBytes = JsonSerializer.SerializeToUtf8Bytes(sourceDataset,
        new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
    string originalHash = Convert.ToHexString(SHA256.HashData(originalBytes)).ToLowerInvariant();
    if (sourceDataset.GetProperty("pageSize").GetInt32() != 50
        || sourceManifest!.RootElement.GetProperty("hashSha256").GetString() != originalHash
        || hash != originalHash)
    {
        throw new InvalidOperationException("Replay did not reproduce the original 500-entry, 50-row manifest and SHA-256.");
    }
}
if (verifyReplay)
{
    Console.WriteLine($"verified replay tenant={tenantId} entries=500 sha256={hash}");
    return;
}
if (replay)
{
    // The manifest and its hash identify the same audit entries and anchor across runs.
    // Refresh only read-model provenance so a later fallback run still presents a current grid.
    projection.ProjectedAt = DateTimeOffset.UtcNow;
}

using DaprClient dapr = new DaprClientBuilder().Build();
IReadModelStore store = new DaprReadModelStore(dapr);
string key = $"audit:{tenantId}";
ReadModelEntry<TenantAuditReadModel> existing = await store.GetAsync<TenantAuditReadModel>(storeName, key);
if (existing.Value is not null && !replay)
{
    throw new InvalidOperationException("The isolated tenant audit projection already exists; choose a fresh tenant id.");
}
if (existing.Value is not null && (existing.Value.ProjectionVersion != projection.ProjectionVersion
    || existing.Value.Entries.Count != 500
    || !JsonSerializer.Serialize(existing.Value.Entries).Equals(
        JsonSerializer.Serialize(projection.Entries), StringComparison.Ordinal)))
{
    throw new InvalidOperationException("Existing tenant audit projection does not match the original dataset.");
}
await store.SaveAsync(storeName, key, projection);
ReadModelEntry<TenantAuditReadModel> persisted = await store.GetAsync<TenantAuditReadModel>(storeName, key);
if (persisted.Value is null || persisted.Value.Entries.Count != 500
    || persisted.Value.ProjectedAt != projection.ProjectedAt
    || persisted.Value.ProjectionVersion != projection.ProjectionVersion
    || !JsonSerializer.Serialize(persisted.Value.Entries).Equals(
        JsonSerializer.Serialize(projection.Entries), StringComparison.Ordinal))
{
    throw new InvalidOperationException("Persisted audit projection end-state does not match the generated dataset.");
}

if (fresh)
{
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(manifestPath))!);
    await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(new { hashSha256 = hash, dataset = manifest }, new JsonSerializerOptions { WriteIndented = true }));
}
Console.WriteLine($"{(replay ? "replayed" : "seeded")} tenant={tenantId} entries=500 sha256={hash} effective-projected-at={projection.ProjectedAt:O} etag-present={!string.IsNullOrWhiteSpace(persisted.ETag)}");
sourceManifest?.Dispose();

void AddKinds(string kind, int count)
{
    for (int i = 0; i < count; i++)
    {
        eventKinds.Add(kind);
    }
}
