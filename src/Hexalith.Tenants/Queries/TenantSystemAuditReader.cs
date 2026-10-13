using System.Security.Cryptography;
using System.Text.Json;

using Hexalith.EventStore.Client.Projections;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Server.Projections;

namespace Hexalith.Tenants.Queries;

/// <summary>Reads separated system audit streams with immutable, fail-closed legacy compatibility.</summary>
internal static class TenantSystemAuditReader
{
    /// <summary>Composes a selected system owner stream without altering historical evidence.</summary>
    internal static async Task<(TenantAuditReadModel? Model, string? ETag, bool Available)> ReadAsync(
        IReadModelStore store, string storeName, string? stream, CancellationToken cancellationToken)
    {
        TenantAuditReadModel? tenant = null;
        TenantAuditReadModel? global = null;
        if (stream is null or TenantAuditStreams.Tenant)
        {
            tenant = (await store.GetAsync<TenantAuditReadModel>(storeName, TenantAuditStreams.SystemTenantKey,
                cancellationToken).ConfigureAwait(false))?.Value;
        }

        if (stream is null or TenantAuditStreams.GlobalAdministrators)
        {
            global = (await store.GetAsync<TenantAuditReadModel>(storeName, TenantAuditStreams.GlobalAdministratorsKey,
                cancellationToken).ConfigureAwait(false))?.Value;
        }

        TenantAuditReadModel? legacy = (await store.GetAsync<TenantAuditReadModel>(storeName,
            TenantAuditStreams.LegacySystemKey, cancellationToken).ConfigureAwait(false))?.Value;
        cancellationToken.ThrowIfCancellationRequested();

        var evidence = new Dictionary<(string Owner, string EventId), TenantAuditEntry>();
        var sources = new List<TenantAuditReadModel>();
        var legacyEvidence = new Dictionary<(string Owner, string EventId), TenantAuditEntry>();
        foreach ((TenantAuditReadModel? model, string? expectedOwner) in new[]
        {
            (tenant, (string?)TenantAuditStreams.Tenant),
            (global, (string?)TenantAuditStreams.GlobalAdministrators),
            (legacy, (string?)null),
        })
        {
            if (model is null)
            {
                continue;
            }

            if (model.Entries is null)
            {
                return (null, null, false);
            }

            bool contributes = expectedOwner is not null;
            foreach (TenantAuditEntry? row in model.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string? owner = TenantAuditOwnership.GetOwner(row, "system");
                if (owner is null || expectedOwner is not null && owner != expectedOwner)
                {
                    return (null, null, false);
                }

                // Validate all legacy evidence, including the other owner's subset, before selecting rows.
                var identity = (owner, row!.EventId);
                if (expectedOwner is null)
                {
                    // Missing provenance is compatible only with its trusted canonical counterpart;
                    // it must never act as a wildcard across contradictory historical duplicates.
                    if (legacyEvidence.TryGetValue(identity, out TenantAuditEntry? previousLegacy)
                        && !TenantAuditStreams.SameEvidence(previousLegacy, row))
                    {
                        return (null, null, false);
                    }
                    legacyEvidence[identity] = row;
                }
                bool duplicate = evidence.TryGetValue(identity, out TenantAuditEntry? existing);
                if (duplicate
                    && !TenantAuditStreams.SameEvidence(existing!, row, allowMissingProvenance: expectedOwner is null))
                {
                    return (null, null, false);
                }

                if (!duplicate)
                {
                    evidence[identity] = row;
                }
                // Canonical duplicate evidence owns its freshness and additive proof metadata.
                contributes |= !duplicate && (stream is null || owner == stream);
            }

            if (contributes)
            {
                sources.Add(model);
            }
        }

        byte[] fingerprint = SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new { Stream = stream, Tenant = tenant,
            GlobalAdministrators = global, Legacy = legacy }));
        string validator = "audit-composite:" + Convert.ToHexString(fingerprint);
        TenantAuditReadModel combined = new()
        {
            Entries = evidence.Where(pair => stream is null || pair.Key.Owner == stream).Select(pair => pair.Value).ToList(),
            ProjectedAt = sources.Count == 0 || sources.Any(source => source.ProjectedAt is null)
                ? null : sources.Min(source => source.ProjectedAt),
            ProjectionVersion = stream == TenantAuditStreams.GlobalAdministrators
                ? CanonicalGlobalVersion(global, evidence) : validator,
        };
        combined.SortEntries();
        return (combined, validator, true);
    }

    private static string? CanonicalGlobalVersion(TenantAuditReadModel? global,
        Dictionary<(string Owner, string EventId), TenantAuditEntry> evidence)
    {
        if (global is null || !TenantAuditStreams.TryGetSequence(global.ProjectionVersion, out long sequence))
        {
            return null;
        }

        var canonical = (global.Entries ?? []).GroupBy(row => row.EventId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        foreach (TenantAuditEntry row in evidence.Where(pair => pair.Key.Owner == TenantAuditStreams.GlobalAdministrators)
            .Select(pair => pair.Value))
        {
            if (row.AggregateSequence is null || row.AggregateSequence > sequence
                || !canonical.TryGetValue(row.EventId, out TenantAuditEntry? known)
                || !TenantAuditStreams.SameEvidence(row, known))
            {
                return null;
            }
        }

        return global.ProjectionVersion;
    }
}
