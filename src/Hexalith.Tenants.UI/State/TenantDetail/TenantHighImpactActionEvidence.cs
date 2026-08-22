using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Enums;

namespace Hexalith.Tenants.UI.State.TenantDetail;

/// <summary>
/// Carries immutable, support-safe evidence for one staged high-impact action evaluation.
/// </summary>
/// <param name="TenantId">Literal tenant identifier.</param>
/// <param name="Action">Action being evaluated.</param>
/// <param name="Stage">Evaluation stage.</param>
/// <param name="TenantStatus">Last-confirmed tenant lifecycle state.</param>
/// <param name="Freshness">Authoritative freshness including the UI-only refreshing state.</param>
/// <param name="HasCurrentBaseline">Whether refreshing retained a current authoritative baseline.</param>
/// <param name="SurfaceKind">Authoritative detail surface state.</param>
/// <param name="ProjectionLifecycle">Authoritative projection lifecycle evidence.</param>
/// <param name="Authority">Server-reflected role authority.</param>
/// <param name="NamespaceScope">Ordinal namespace-scope evidence.</param>
/// <param name="Support">Action-specific lifecycle support.</param>
/// <param name="Admission">Observed aggregate-admission state; evaluation never acquires it.</param>
/// <param name="Preview">Safe consequence-preview readiness.</param>
/// <param name="Proof">Action-declared proof readiness.</param>
/// <param name="Viewport">Measured FrontComposer viewport evidence.</param>
/// <param name="IsInputComplete">Whether confirmation inputs are complete.</param>
/// <param name="TargetState">Safe action-specific target state.</param>
public sealed record TenantHighImpactActionEvidence(
    string TenantId,
    TenantHighImpactAction Action,
    TenantHighImpactEvaluationStage Stage,
    TenantStatus TenantStatus,
    TenantHighImpactFreshnessState Freshness,
    bool HasCurrentBaseline,
    TenantDetailSurfaceKind SurfaceKind,
    ProjectionLifecycleState ProjectionLifecycle,
    TenantHighImpactAuthorityEvidence Authority,
    TenantHighImpactNamespaceScopeEvidence NamespaceScope,
    TenantHighImpactSupportEvidence Support,
    TenantHighImpactAdmissionEvidence Admission,
    TenantHighImpactPreviewEvidence Preview,
    TenantHighImpactProofEvidence Proof,
    TenantHighImpactViewportState Viewport,
    bool IsInputComplete,
    TenantHighImpactTargetState TargetState)
{
    /// <summary>
    /// Maps persisted read-model freshness to the shared high-impact vocabulary.
    /// </summary>
    /// <param name="freshness">Persisted read-model freshness.</param>
    /// <returns>The matching high-impact freshness state.</returns>
    public static TenantHighImpactFreshnessState FromReadModelFreshness(ReadModelFreshnessState freshness)
        => freshness switch
        {
            ReadModelFreshnessState.Current => TenantHighImpactFreshnessState.Current,
            ReadModelFreshnessState.Aging => TenantHighImpactFreshnessState.Aging,
            ReadModelFreshnessState.Stale => TenantHighImpactFreshnessState.Stale,
            _ => TenantHighImpactFreshnessState.Unknown,
        };
}
