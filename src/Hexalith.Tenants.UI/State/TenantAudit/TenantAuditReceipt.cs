using System.Globalization;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Services.SupportSafety;
using Hexalith.Tenants.UI.State.TenantCommands;

namespace Hexalith.Tenants.UI.State.TenantAudit;

/// <summary>Only the approved presentation facts of a tenant audit event.</summary>
/// <param name="Actor">Approved actor ID.</param>
/// <param name="Target">Approved target ID or key.</param>
/// <param name="Scope">Approved tenant scope.</param>
/// <param name="Outcome">Supported event type token.</param>
/// <param name="Timestamp">Absolute audit event time.</param>
/// <param name="ProjectionMarker">Safe freshness marker.</param>
/// <param name="AuditReference">Approved event reference.</param>
/// <param name="CommandReference">Only a proven command reference, when available.</param>
/// <param name="State">Receipt availability.</param>
/// <param name="IsRequestedReferenceMissing">Whether an authorized checked page lacks the requested row.</param>
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
    bool IsRequestedReferenceMissing = false)
{
    /// <summary>Formats the event time as absolute UTC using the current culture.</summary>
    public string TimestampLabel
        => Timestamp?.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.CurrentCulture) ?? string.Empty;

    /// <summary>Builds a receipt from an authorized audit entry.</summary>
    public static TenantAuditReceipt FromEntry(
        TenantAuditEntry entry,
        ReadModelFreshnessState freshness,
        TenantAuditSurfaceKind surfaceKind = TenantAuditSurfaceKind.Ready,
        TenantCommandAuditState auditState = TenantCommandAuditState.NotStarted)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return FromRow(TenantAuditRow.FromEntry(entry, freshness), surfaceKind, auditState);
    }

    /// <summary>Builds a receipt from a current BFF-mapped audit row.</summary>
    public static TenantAuditReceipt FromRow(
        TenantAuditRow row,
        TenantAuditSurfaceKind surfaceKind = TenantAuditSurfaceKind.Ready,
        TenantCommandAuditState auditState = TenantCommandAuditState.NotStarted)
    {
        ArgumentNullException.ThrowIfNull(row);
        string actor = TenantAuditSupportSafety.SafeIdentifier(row.ActorId, SupportSafeCopyValueKind.UserId);
        string target = TenantAuditSupportSafety.SafeIdentifier(row.Target, TargetValueKind(row));
        string scope = TenantAuditSupportSafety.SafeIdentifier(row.Scope, SupportSafeCopyValueKind.TenantId);
        string reference = TenantAuditSupportSafety.SafeApprovedReference(row.EventReference) ?? string.Empty;
        string outcome = IsKnownOutcome(row.EventType, row.Category) ? row.EventType : string.Empty;
        DateTimeOffset? timestamp = row.Timestamp != default && row.Timestamp.Offset == TimeSpan.Zero
            ? row.Timestamp : null;
        bool complete = actor.Length > 0 && target.Length > 0 && scope.Length > 0
            && reference.Length > 0 && outcome.Length > 0 && timestamp is not null
            && (row.Category is not AuditEventCategory.Access
                || TenantAuditSupportSafety.IsSafe(row.Narrative?.UserId, SupportSafeCopyValueKind.UserId))
            && row.Freshness is ReadModelFreshnessState.Current
            && row.Lifecycle is ProjectionLifecycleState.Current
            && row.Provenance is QueryResponseProvenance.ProjectionBacked;
        TenantAuditReceiptState state = ResolveState(surfaceKind, auditState, row.Freshness, complete);
        return new(actor, target, scope, outcome, timestamp,
            row.Freshness, reference, null, state);
    }

    /// <summary>Builds an unverified requested-reference state without claiming event proof.</summary>
    public static TenantAuditReceipt Unavailable(
        TenantAuditSurfaceKind surfaceKind = TenantAuditSurfaceKind.Ready,
        bool checkedPage = true)
    {
        bool missing = checkedPage && surfaceKind is TenantAuditSurfaceKind.Ready
            or TenantAuditSurfaceKind.Empty or TenantAuditSurfaceKind.FilteredEmpty
            or TenantAuditSurfaceKind.Stale or TenantAuditSurfaceKind.Degraded
            or TenantAuditSurfaceKind.ListRefreshed;
        TenantAuditReceiptState state = surfaceKind switch
        {
            TenantAuditSurfaceKind.Loading => TenantAuditReceiptState.Loading,
            TenantAuditSurfaceKind.Error => TenantAuditReceiptState.Error,
            TenantAuditSurfaceKind.Unavailable => TenantAuditReceiptState.Unavailable,
            TenantAuditSurfaceKind.Unauthorized => TenantAuditReceiptState.Unauthorized,
            TenantAuditSurfaceKind.InvalidCursor => TenantAuditReceiptState.InvalidCursor,
            TenantAuditSurfaceKind.Stale => TenantAuditReceiptState.Stale,
            TenantAuditSurfaceKind.Degraded => TenantAuditReceiptState.Degraded,
            _ => TenantAuditReceiptState.InvalidReference,
        };
        return new(string.Empty, string.Empty, string.Empty, string.Empty, null,
            ReadModelFreshnessState.Unknown, string.Empty, null, state, missing);
    }

    /// <summary>Checks whether a known event has a supported outcome translation.</summary>
    public static bool IsKnownOutcome(string? eventType, AuditEventCategory category)
        => eventType switch
        {
            "UserAddedToTenant" or "UserRemovedFromTenant" or "UserRoleChanged"
                => category is AuditEventCategory.Access,
            "GlobalAdministratorSet" or "GlobalAdministratorRemoved"
                => category is AuditEventCategory.Access or AuditEventCategory.Administrative,
            "TenantCreated" or "TenantUpdated" or "TenantDisabled" or "TenantEnabled"
                or "TenantConfigurationSet" or "TenantConfigurationRemoved"
                => category is AuditEventCategory.Administrative,
            _ => false,
        };

    private static TenantAuditReceiptState ResolveState(
        TenantAuditSurfaceKind surfaceKind,
        TenantCommandAuditState auditState,
        ReadModelFreshnessState freshness,
        bool complete)
    {
        TenantAuditReceiptState surfaceState = surfaceKind switch
        {
            TenantAuditSurfaceKind.Loading => TenantAuditReceiptState.Loading,
            TenantAuditSurfaceKind.Error => TenantAuditReceiptState.Error,
            TenantAuditSurfaceKind.Unavailable => TenantAuditReceiptState.Unavailable,
            TenantAuditSurfaceKind.Unauthorized => TenantAuditReceiptState.Unauthorized,
            TenantAuditSurfaceKind.InvalidCursor => TenantAuditReceiptState.InvalidCursor,
            TenantAuditSurfaceKind.Stale => TenantAuditReceiptState.Stale,
            TenantAuditSurfaceKind.Degraded => TenantAuditReceiptState.Degraded,
            _ => TenantAuditReceiptState.Ready,
        };
        if (surfaceState is not TenantAuditReceiptState.Ready)
        {
            return surfaceState;
        }

        TenantAuditReceiptState auditReceiptState = TenantAuditAvailability.FromCommandAuditState(auditState).State switch
        {
            TenantAuditAvailabilityState.Pending => TenantAuditReceiptState.Pending,
            TenantAuditAvailabilityState.Delayed => TenantAuditReceiptState.Delayed,
            TenantAuditAvailabilityState.Unavailable => TenantAuditReceiptState.Unavailable,
            TenantAuditAvailabilityState.MissingSupport => TenantAuditReceiptState.MissingSupport,
            _ => TenantAuditReceiptState.Ready,
        };
        if (auditReceiptState is not TenantAuditReceiptState.Ready)
        {
            return auditReceiptState;
        }

        return freshness is ReadModelFreshnessState.Stale
            ? TenantAuditReceiptState.Stale
            : complete ? TenantAuditReceiptState.Ready : TenantAuditReceiptState.Partial;
    }

    private static SupportSafeCopyValueKind TargetValueKind(TenantAuditRow row)
        => !string.IsNullOrWhiteSpace(row.Narrative?.UserId)
            ? SupportSafeCopyValueKind.UserId
            : !string.IsNullOrWhiteSpace(row.Narrative?.ConfigurationKey)
                ? SupportSafeCopyValueKind.ConfigurationKey
                : SupportSafeCopyValueKind.TenantId;
}
