using System.Globalization;

using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Services.SupportSafety;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.EventStore.Client.Projections;

namespace Hexalith.Tenants.UI.State.TenantAudit;

public enum TenantAuditReceiptState {
    Ready,
    Partial,
    Pending,
    Delayed,
    Unavailable,
    MissingSupport,
    Stale,
    Degraded,
    Unauthorized,
    InvalidReference,
}

public sealed record TenantAuditReceipt(
    string Actor,
    string Target,
    string Scope,
    string Outcome,
    DateTimeOffset? Timestamp,
    ReadModelFreshnessState ProjectionMarker,
    string AuditReference,
    string? CommandReference,
    TenantAuditReceiptState State) {
    public string TimestampLabel
        => Timestamp?.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.CurrentCulture) ?? string.Empty;

    public static TenantAuditReceipt FromEntry(
        TenantAuditEntry entry,
        ReadModelFreshnessState freshness,
        string? supportSafeCommandReference = null,
        TenantAuditSurfaceKind surfaceKind = TenantAuditSurfaceKind.Ready,
        TenantCommandAuditState auditState = TenantCommandAuditState.NotStarted) {
        ArgumentNullException.ThrowIfNull(entry);

        return FromRow(TenantAuditRow.FromEntry(entry, freshness), supportSafeCommandReference, surfaceKind, auditState);
    }

    public static TenantAuditReceipt FromRow(
        TenantAuditRow row,
        string? supportSafeCommandReference = null,
        TenantAuditSurfaceKind surfaceKind = TenantAuditSurfaceKind.Ready,
        TenantCommandAuditState auditState = TenantCommandAuditState.NotStarted) {
        ArgumentNullException.ThrowIfNull(row);

        string outcome = $"{row.EventType} ({row.Category})";
        TenantAuditReceiptState state = ResolveState(row, outcome, surfaceKind, auditState);
        string? commandReference = TenantAuditSupportSafety.SafeApprovedReference(supportSafeCommandReference);

        return new(
            TenantAuditSupportSafety.SafeIdentifier(row.ActorId, SupportSafeCopyValueKind.UserId),
            SafeTarget(row),
            TenantAuditSupportSafety.SafeIdentifier(row.Scope, SupportSafeCopyValueKind.TenantId),
            outcome,
            row.Timestamp,
            row.Freshness,
            TenantAuditSupportSafety.SafeApprovedReference(row.EventReference) ?? string.Empty,
            commandReference,
            state);
    }

    public static TenantAuditReceipt Unavailable(
        string? requestedReference,
        string tenantId,
        string? supportSafeCommandReference = null) {
        string auditReference = TenantAuditSupportSafety.SafeApprovedReference(requestedReference) ?? string.Empty;
        string scope = TenantAuditSupportSafety.SafeIdentifier(tenantId, SupportSafeCopyValueKind.TenantId);
        string? commandReference = TenantAuditSupportSafety.SafeApprovedReference(supportSafeCommandReference);

        return new(
            string.Empty,
            string.Empty,
            scope,
            string.Empty,
            null,
            ReadModelFreshnessState.Unknown,
            auditReference,
            commandReference,
            TenantAuditReceiptState.InvalidReference);
    }

    private static TenantAuditReceiptState ResolveState(
        TenantAuditRow row,
        string outcome,
        TenantAuditSurfaceKind surfaceKind,
        TenantCommandAuditState auditState) {
        TenantAuditReceiptState surfaceState = surfaceKind switch {
            TenantAuditSurfaceKind.Stale => TenantAuditReceiptState.Stale,
            TenantAuditSurfaceKind.Degraded => TenantAuditReceiptState.Degraded,
            TenantAuditSurfaceKind.Unauthorized => TenantAuditReceiptState.Unauthorized,
            TenantAuditSurfaceKind.InvalidCursor => TenantAuditReceiptState.InvalidReference,
            TenantAuditSurfaceKind.Unavailable or TenantAuditSurfaceKind.Error => TenantAuditReceiptState.Unavailable,
            TenantAuditSurfaceKind.Loading or TenantAuditSurfaceKind.Empty or TenantAuditSurfaceKind.FilteredEmpty => TenantAuditReceiptState.Unavailable,
            _ => TenantAuditReceiptState.Ready,
        };

        if (surfaceState is not TenantAuditReceiptState.Ready) {
            return surfaceState;
        }

        TenantAuditReceiptState auditReceiptState = TenantAuditAvailability.FromCommandAuditState(auditState).State switch {
            TenantAuditAvailabilityState.Pending => TenantAuditReceiptState.Pending,
            TenantAuditAvailabilityState.Delayed => TenantAuditReceiptState.Delayed,
            TenantAuditAvailabilityState.Unavailable => TenantAuditReceiptState.Unavailable,
            TenantAuditAvailabilityState.MissingSupport => TenantAuditReceiptState.MissingSupport,
            _ => TenantAuditReceiptState.Ready,
        };

        if (auditReceiptState is not TenantAuditReceiptState.Ready) {
            return auditReceiptState;
        }

        if (row.Freshness is ReadModelFreshnessState.Stale) {
            return TenantAuditReceiptState.Stale;
        }

        return HasRequiredFields(row, outcome)
            ? TenantAuditReceiptState.Ready
            : TenantAuditReceiptState.Partial;
    }

    private static bool HasRequiredFields(TenantAuditRow row, string outcome)
        => !string.IsNullOrWhiteSpace(TenantAuditSupportSafety.SafeApprovedReference(row.EventReference))
            && !string.IsNullOrWhiteSpace(TenantAuditSupportSafety.SafeIdentifier(row.ActorId, SupportSafeCopyValueKind.UserId))
            && !string.IsNullOrWhiteSpace(SafeTarget(row))
            && !string.IsNullOrWhiteSpace(TenantAuditSupportSafety.SafeIdentifier(row.Scope, SupportSafeCopyValueKind.TenantId))
            && !string.IsNullOrWhiteSpace(outcome);

    private static string SafeTarget(TenantAuditRow row)
        => TenantAuditSupportSafety.SafeIdentifier(row.Target, TargetValueKind(row));

    private static SupportSafeCopyValueKind TargetValueKind(TenantAuditRow row) {
        if (!string.IsNullOrWhiteSpace(row.Narrative?.UserId)) {
            return SupportSafeCopyValueKind.UserId;
        }

        return !string.IsNullOrWhiteSpace(row.Narrative?.ConfigurationKey)
            ? SupportSafeCopyValueKind.ConfigurationKey
            : SupportSafeCopyValueKind.TenantId;
    }

}
