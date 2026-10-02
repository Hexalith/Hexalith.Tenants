using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.UI.Services.SupportSafety;

namespace Hexalith.Tenants.UI.State.TenantAudit;

/// <summary>Redacted facts from one unconditional current tenant read and current circuit authority.</summary>
/// <param name="TenantId">Literal, support-safe tenant identifier.</param>
/// <param name="TargetUserId">Literal, support-safe target identifier.</param>
/// <param name="TenantStatus">Current tenant lifecycle.</param>
/// <param name="CurrentRole">Current role, or null for a verified absent target.</param>
/// <param name="IsMembershipEmpty">Whether the complete validated membership is empty.</param>
/// <param name="IsAuthorized">Whether the current principal has owner or global authority.</param>
/// <param name="IsGlobalAdministrator">Whether current global authority is proven.</param>
/// <param name="HasVerifiedMembership">Whether all membership evidence is complete and unambiguous.</param>
/// <param name="Freshness">Freshness from the direct projection read.</param>
/// <param name="Lifecycle">Lifecycle from the direct projection read.</param>
/// <param name="Provenance">Provenance of the direct projection read.</param>
public sealed record TenantCorrectionProjection(
    string TenantId,
    string TargetUserId,
    TenantStatus TenantStatus,
    TenantRole? CurrentRole,
    bool IsMembershipEmpty,
    bool IsAuthorized,
    bool IsGlobalAdministrator,
    bool HasVerifiedMembership,
    ReadModelFreshnessState Freshness,
    ProjectionLifecycleState Lifecycle,
    QueryResponseProvenance Provenance)
{
    /// <summary>Gets whether the direct read carries current projection evidence.</summary>
    public bool IsCurrent => HasVerifiedMembership
        && Freshness is ReadModelFreshnessState.Current
        && ProjectionLifecyclePolicy.IsProjectionConfirmed(Provenance, Lifecycle);

    /// <summary>Creates a fail-closed result without retaining raw response or authority evidence.</summary>
    public static TenantCorrectionProjection Unavailable(string tenantId, string targetUserId)
        => new(TenantAuditSupportSafety.SafeIdentifier(tenantId, SupportSafeCopyValueKind.TenantId),
            TenantAuditSupportSafety.SafeIdentifier(targetUserId, SupportSafeCopyValueKind.UserId), TenantStatus.Unknown, null, false, false, false, false,
            ReadModelFreshnessState.Unknown, ProjectionLifecycleState.Unknown, QueryResponseProvenance.Unknown);
}
