using System.Globalization;

using Hexalith.Tenants.Contracts.Projections;
using Hexalith.Tenants.Contracts.Queries;

namespace Hexalith.Tenants.Server.Projections;

/// <summary>Defines audit ownership, disjoint storage keys, and evidence-preserving merges.</summary>
public static class TenantAuditStreams
{
    /// <summary>The tenant-only query selector.</summary>
    public const string Tenant = "tenant";

    /// <summary>The global-administrator query selector.</summary>
    public const string GlobalAdministrators = "global-administrators";

    /// <summary>The immutable historical shared audit key.</summary>
    public const string LegacySystemKey = "audit:system";

    /// <summary>The audit key for the literal system tenant.</summary>
    public const string SystemTenantKey = "audit-tenants:system";

    /// <summary>The singleton global-administrator audit key, outside the arbitrary tenant prefix.</summary>
    public const string GlobalAdministratorsKey = "audit-global-administrators:singleton";

    /// <summary>Returns the tenant owner's audit key without reserving any tenant identifier.</summary>
    public static string GetTenantKey(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        return tenantId == "system" ? SystemTenantKey : "audit:" + tenantId;
    }

    /// <summary>Rejects incoming rows that belong to another aggregate owner before any write.</summary>
    public static void ValidateIncoming(TenantAuditReadModel model, string tenantId, string owner)
    {
        ArgumentNullException.ThrowIfNull(model);
        if ((model.Entries ?? []).Any(entry => TenantAuditOwnership.GetOwner(entry, tenantId) != owner))
        {
            throw new InvalidOperationException("Audit projection ownership is inconsistent.");
        }

        var seen = new Dictionary<string, TenantAuditEntry>(StringComparer.Ordinal);
        foreach (TenantAuditEntry entry in model.Entries ?? [])
        {
            if (seen.TryGetValue(entry.EventId, out TenantAuditEntry? existing) && !SameEvidence(existing, entry))
            {
                throw new InvalidOperationException("Audit projection contains conflicting evidence.");
            }

            seen[entry.EventId] = entry;
        }
    }

    /// <summary>Compares all stored evidence fields, independently of dictionary insertion order.</summary>
    public static bool SameEvidence(TenantAuditEntry left, TenantAuditEntry right, bool allowMissingProvenance = false)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.EventId == right.EventId && left.EventType == right.EventType
            && left.Category == right.Category && left.ActorId == right.ActorId
            && left.Timestamp == right.Timestamp && left.TenantId == right.TenantId
            && (left.CommandCorrelationId == right.CommandCorrelationId
                || allowMissingProvenance && (left.CommandCorrelationId is null || right.CommandCorrelationId is null))
            && (left.AggregateSequence == right.AggregateSequence
                || allowMissingProvenance && (left.AggregateSequence is null || right.AggregateSequence is null))
            && (left.NarrativePayload is null ? right.NarrativePayload is null
                : right.NarrativePayload is not null && left.NarrativePayload.Count == right.NarrativePayload.Count
                    && left.NarrativePayload.All(pair => right.NarrativePayload.TryGetValue(pair.Key, out string? value)
                        && string.Equals(pair.Value, value, StringComparison.Ordinal)));
    }

    /// <summary>Merges by EventId without mutating the persisted instance or deleting prior evidence.</summary>
    public static TenantAuditReadModel Merge(TenantAuditReadModel persisted, TenantAuditReadModel incoming, bool rejectConflicts = true)
    {
        ArgumentNullException.ThrowIfNull(persisted);
        ArgumentNullException.ThrowIfNull(incoming);
        TenantAuditReadModel merged = new()
        {
            Entries = [.. persisted.Entries ?? []],
            ProjectedAt = persisted.ProjectedAt is null ? incoming.ProjectedAt
                : incoming.ProjectedAt is null || persisted.ProjectedAt >= incoming.ProjectedAt
                    ? persisted.ProjectedAt : incoming.ProjectedAt,
            ProjectionVersion = LatestVersion(persisted.ProjectionVersion, incoming.ProjectionVersion),
        };

        var existingById = new Dictionary<string, TenantAuditEntry>(StringComparer.Ordinal);
        foreach (TenantAuditEntry? row in merged.Entries)
        {
            if (row is not null && !string.IsNullOrWhiteSpace(row.EventId)) existingById.TryAdd(row.EventId, row);
        }

        foreach (TenantAuditEntry entry in incoming.Entries ?? [])
        {
            existingById.TryGetValue(entry.EventId, out TenantAuditEntry? existing);
            if (rejectConflicts && existing is not null && !SameEvidence(existing, entry))
            {
                throw new InvalidOperationException("Audit projection contains conflicting evidence.");
            }

            if (existing is null)
            {
                merged.Entries.Add(entry);
                existingById[entry.EventId] = entry;
            }
        }

        // Corrupt historical rows are retained; compatible reads will fail closed rather than discard them.
        merged.Entries = merged.Entries.OrderBy(row => row?.Timestamp)
            .ThenBy(row => row?.EventId, StringComparer.Ordinal).ToList();
        return merged;
    }

    /// <summary>Parses the shared ordered aggregate version format.</summary>
    public static bool TryGetSequence(string? version, out long sequence)
    {
        sequence = 0;
        return version is not null && version.StartsWith(TenantProjectionVersionFormat.SequencePrefix, StringComparison.Ordinal)
            && long.TryParse(version.AsSpan(TenantProjectionVersionFormat.SequencePrefix.Length), NumberStyles.None,
                CultureInfo.InvariantCulture, out sequence) && sequence > 0;
    }

    private static string? LatestVersion(string? persisted, string? incoming)
        => TryGetSequence(persisted, out long previous) && (!TryGetSequence(incoming, out long next) || previous >= next)
            ? persisted : incoming;
}
