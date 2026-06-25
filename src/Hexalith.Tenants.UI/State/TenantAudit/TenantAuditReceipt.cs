using System.Globalization;
using System.Text;

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
    TenantAuditReceiptState State,
    string CopyableReferenceText) {
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
        string? commandReference = SafeApprovedReference(supportSafeCommandReference);
        string copyableReferenceText = BuildCopyableReferenceText(row, outcome, commandReference, state);

        return new(
            SafeIdentifier(row.ActorId, SupportSafeCopyValueKind.UserId),
            SafeTarget(row),
            SafeIdentifier(row.Scope, SupportSafeCopyValueKind.TenantId),
            outcome,
            row.Timestamp,
            row.Freshness,
            SafeApprovedReference(row.EventReference) ?? string.Empty,
            commandReference,
            state,
            copyableReferenceText);
    }

    public static TenantAuditReceipt Unavailable(
        string? requestedReference,
        string tenantId,
        string? supportSafeCommandReference = null) {
        string auditReference = SafeApprovedReference(requestedReference) ?? string.Empty;
        string scope = SafeIdentifier(tenantId, SupportSafeCopyValueKind.TenantId);
        string? commandReference = SafeApprovedReference(supportSafeCommandReference);

        return new(
            string.Empty,
            string.Empty,
            scope,
            string.Empty,
            null,
            ReadModelFreshnessState.Unknown,
            auditReference,
            commandReference,
            TenantAuditReceiptState.InvalidReference,
            string.Empty);
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
        => !string.IsNullOrWhiteSpace(SafeApprovedReference(row.EventReference))
            && !string.IsNullOrWhiteSpace(SafeIdentifier(row.ActorId, SupportSafeCopyValueKind.UserId))
            && !string.IsNullOrWhiteSpace(SafeTarget(row))
            && !string.IsNullOrWhiteSpace(SafeIdentifier(row.Scope, SupportSafeCopyValueKind.TenantId))
            && !string.IsNullOrWhiteSpace(outcome);

    private static string BuildCopyableReferenceText(
        TenantAuditRow row,
        string outcome,
        string? commandReference,
        TenantAuditReceiptState state) {
        string? auditReference = SafeApprovedReference(row.EventReference);
        if (state is TenantAuditReceiptState.Partial || string.IsNullOrWhiteSpace(auditReference)) {
            return string.Empty;
        }

        StringBuilder builder = new();
        AppendLine(builder, "Audit reference", auditReference);
        AppendLine(builder, "Command reference", commandReference);
        AppendLine(builder, "Tenant scope", SafeIdentifier(row.Scope, SupportSafeCopyValueKind.TenantId));
        AppendLine(builder, "Target", SafeTarget(row));
        AppendLine(builder, "Outcome", SafeApprovedReference(outcome));
        AppendLine(builder, "Projection marker", row.Freshness.ToString());
        AppendLine(builder, "Timestamp", row.Timestamp.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture));

        string result = builder.ToString();
        return SupportSafeCopyClassifier.IsAllowed(result, SupportSafeCopyValueKind.ApprovedReference)
            ? result
            : string.Empty;
    }

    private static void AppendLine(StringBuilder builder, string label, string? value) {
        if (!string.IsNullOrWhiteSpace(value)) {
            _ = builder.Append(label).Append(": ").AppendLine(value);
        }
    }

    private static string SafeTarget(TenantAuditRow row)
        => SafeIdentifier(row.Target, TargetValueKind(row));

    private static SupportSafeCopyValueKind TargetValueKind(TenantAuditRow row) {
        string userMarker = $"userId: {row.Target}";
        if (row.ReferenceContext.Contains(userMarker, StringComparison.Ordinal)) {
            return SupportSafeCopyValueKind.UserId;
        }

        string keyMarker = $"key: {row.Target}";
        return row.ReferenceContext.Contains(keyMarker, StringComparison.Ordinal)
            ? SupportSafeCopyValueKind.ConfigurationKey
            : SupportSafeCopyValueKind.TenantId;
    }

    private static string SafeIdentifier(string? value, SupportSafeCopyValueKind kind)
        => SupportSafeCopyClassifier.IsAllowed(value, kind) ? value! : string.Empty;

    private static string? SafeApprovedReference(string? value)
        => SupportSafeCopyClassifier.IsAllowed(value, SupportSafeCopyValueKind.ApprovedReference) ? value : null;
}
