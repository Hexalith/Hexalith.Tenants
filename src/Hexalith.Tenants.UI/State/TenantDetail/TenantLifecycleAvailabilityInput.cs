using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Enums;

namespace Hexalith.Tenants.UI.State.TenantDetail;

/// <summary>Adapts legacy lifecycle inputs into the shared high-impact evidence kernel.</summary>
/// <param name="TenantId">Literal tenant identifier.</param>
/// <param name="CurrentStatus">Last-confirmed lifecycle status.</param>
/// <param name="Freshness">Projection freshness.</param>
/// <param name="SurfaceKind">Detail surface state.</param>
/// <param name="IsCommandSurfaceConnected">Whether lifecycle command support is connected.</param>
/// <param name="GovernanceReadiness">Compatibility admission readiness.</param>
/// <param name="AuthorizationReflection">Server-reflected lifecycle authority.</param>
/// <param name="IsNarrowSafetyContext">Whether the measured viewport is unsafe.</param>
/// <param name="Lifecycle">Projection lifecycle.</param>
/// <param name="ProjectionVersion">Ordered authoritative projection marker.</param>
public sealed record TenantLifecycleAvailabilityInput(
    string TenantId,
    TenantStatus CurrentStatus,
    ReadModelFreshnessState Freshness,
    TenantDetailSurfaceKind SurfaceKind,
    bool IsCommandSurfaceConnected,
    TenantLifecycleGovernanceReadiness GovernanceReadiness = TenantLifecycleGovernanceReadiness.Unresolved,
    TenantLifecycleAuthorizationReflectionState AuthorizationReflection = TenantLifecycleAuthorizationReflectionState.Indeterminate,
    bool IsNarrowSafetyContext = false,
    ProjectionLifecycleState Lifecycle = ProjectionLifecycleState.Unknown,
    string? ProjectionVersion = null)
{
    /// <summary>Evaluates one legacy operation through the shared kernel.</summary>
    /// <param name="operation">Lifecycle operation.</param>
    /// <returns>The typed and compatibility availability result.</returns>
    public TenantLifecycleAvailability Evaluate(TenantLifecycleOperation operation)
        => TenantLifecycleAvailability.FromEvidence(
            new(
                TenantId,
                operation is TenantLifecycleOperation.EnableTenant
                    ? TenantHighImpactAction.EnableTenant
                    : TenantHighImpactAction.DisableTenant,
                TenantHighImpactEvaluationStage.PreviewEntry,
                CurrentStatus,
                TenantHighImpactActionEvidence.FromReadModelFreshness(Freshness),
                Freshness is ReadModelFreshnessState.Current,
                SurfaceKind,
                Lifecycle,
                AuthorizationReflection switch
                {
                    TenantLifecycleAuthorizationReflectionState.Authorized => TenantHighImpactAuthorityEvidence.Authorized,
                    TenantLifecycleAuthorizationReflectionState.MissingPermission => TenantHighImpactAuthorityEvidence.MissingPermission,
                    _ => TenantHighImpactAuthorityEvidence.Indeterminate,
                },
                TenantHighImpactNamespaceScopeEvidence.NotRequired,
                IsCommandSurfaceConnected
                    ? TenantHighImpactSupportEvidence.Ready
                    : TenantHighImpactSupportEvidence.Missing,
                GovernanceReadiness is TenantLifecycleGovernanceReadiness.Ready
                    ? TenantHighImpactAdmissionEvidence.Available
                    : TenantHighImpactAdmissionEvidence.Unknown,
                TenantHighImpactPreviewEvidence.Ready,
                TenantHighImpactProofEvidence.NotRequired,
                IsNarrowSafetyContext
                    ? TenantHighImpactViewportState.Unsafe
                    : TenantHighImpactViewportState.Safe,
                IsInputComplete: true,
                TenantHighImpactTargetState.NotApplicable,
                ProjectionVersion),
            GovernanceReadiness,
            AuthorizationReflection,
            preferDomainOutcome: true);
}
