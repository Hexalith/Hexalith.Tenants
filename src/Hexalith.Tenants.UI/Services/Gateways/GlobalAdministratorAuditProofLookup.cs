using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantCommands;

namespace Hexalith.Tenants.UI.Services.Gateways;

/// <summary>Checks command provenance inside the BFF before returning a redacted receipt row.</summary>
internal static class GlobalAdministratorAuditProofLookup
{
    internal static async Task<(GlobalAdministratorAuditProof? Proof, bool Complete)> FindAsync(
        Func<TenantAuditRequest, CancellationToken, Task<TenantAuditSnapshot>> read,
        string correlationId,
        long committedEventSequence,
        string targetUserId,
        string correctiveEventType,
        DateTimeOffset originalTimestamp,
        DateTimeOffset submittedAtUtc,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(correlationId) || committedEventSequence <= 0
            || string.IsNullOrWhiteSpace(targetUserId)
            || correctiveEventType is not ("GlobalAdministratorSet" or "GlobalAdministratorRemoved")
            || submittedAtUtc <= originalTimestamp)
        {
            return (null, false);
        }

        var visited = new HashSet<string>(StringComparer.Ordinal);
        string? cursor = null;
        string? version = null;
        TenantAuditRow? matching = null;
        for (int page = 0; page < 50; page++)
        {
            TenantAuditRequest request = new("system", From: submittedAtUtc, Cursor: cursor);
            TenantAuditSnapshot audit = await read(request, cancellationToken).ConfigureAwait(false);
            if (audit.Kind is not TenantAuditSurfaceKind.Ready and not TenantAuditSurfaceKind.Empty
                and not TenantAuditSurfaceKind.FilteredEmpty
                || audit.Freshness is not ReadModelFreshnessState.Current
                || audit.Lifecycle is not ProjectionLifecycleState.Current
                || !audit.MatchesScope(request)
                || audit.Rows.Count > request.PageSize
                || !TenantLifecycleProjectionVersion.IsOrdered(audit.ProjectionVersion)
                || audit.HasMore == string.IsNullOrWhiteSpace(audit.NextCursor)
                || version is not null && !string.Equals(version, audit.ProjectionVersion, StringComparison.Ordinal))
            {
                return (null, false);
            }

            version ??= audit.ProjectionVersion;
            foreach (TenantAuditRow row in audit.Rows)
            {
                if (string.Equals(row.CommandCorrelationId, correlationId, StringComparison.Ordinal)
                    && row.AggregateSequence == committedEventSequence
                    && row.Category is AuditEventCategory.Access
                    && string.Equals(row.EventType, correctiveEventType, StringComparison.Ordinal)
                    && string.Equals(row.TenantId, "system", StringComparison.Ordinal)
                    && string.Equals(row.Scope, "system", StringComparison.Ordinal)
                    && string.Equals(row.Target, targetUserId, StringComparison.Ordinal)
                    && row.Timestamp > originalTimestamp
                    && row.Timestamp >= submittedAtUtc
                    && TenantAuditReceipt.FromRow(row, audit.Kind).State is TenantAuditReceiptState.Ready)
                {
                    if (matching is not null)
                    {
                        return (null, false);
                    }

                    matching = row;
                }
            }

            if (!audit.HasMore)
            {
                if (!TenantLifecycleProjectionVersion.HasReached(committedEventSequence, version))
                {
                    return (null, false);
                }

                return (matching is null ? null : new GlobalAdministratorAuditProof(matching with
                {
                    CommandCorrelationId = null,
                    AggregateSequence = null,
                }), true);
            }

            if (string.IsNullOrWhiteSpace(audit.NextCursor) || !visited.Add(audit.NextCursor))
            {
                return (null, false);
            }

            cursor = audit.NextCursor;
        }

        return (null, false);
    }
}
