using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Enums;

namespace Hexalith.Tenants.UI.State.TenantDetail;

/// <summary>
/// Pure evaluator for the four staged high-impact tenant actions.
/// </summary>
public static class TenantHighImpactActionAvailabilityEvaluator
{
    /// <summary>
    /// Evaluates one action without dispatching a command, acquiring admission, or creating attempt state.
    /// </summary>
    /// <param name="evidence">Immutable action evidence.</param>
    /// <returns>A deterministic, support-safe availability result.</returns>
    public static TenantHighImpactActionAvailability Evaluate(TenantHighImpactActionEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);

        TenantHighImpactDomainOutcome domainOutcome = DomainOutcome(evidence);
        bool friction = evidence.Freshness is TenantHighImpactFreshnessState.Aging
            or TenantHighImpactFreshnessState.Refreshing;

        // Precedence is a user-facing contract. Read failures remain actionable as read failures; an
        // unauthorized surface remains a permission result; dependency and confirmation gates follow.
        if (!Enum.IsDefined(evidence.SurfaceKind)
            || !Enum.IsDefined(evidence.Freshness)
            || evidence.SurfaceKind is TenantDetailSurfaceKind.Stale
            || evidence.Freshness is TenantHighImpactFreshnessState.Stale
                or TenantHighImpactFreshnessState.Unknown
            || (evidence.Freshness is TenantHighImpactFreshnessState.Refreshing
                && !evidence.HasCurrentBaseline))
        {
            return Blocked(evidence, TenantHighImpactUnavailableReason.StaleData, domainOutcome, friction);
        }

        if (evidence.SurfaceKind is TenantDetailSurfaceKind.Unauthorized)
        {
            return Blocked(evidence, TenantHighImpactUnavailableReason.MissingPermission, domainOutcome, friction);
        }

        if (evidence.SurfaceKind is not TenantDetailSurfaceKind.Ready)
        {
            return Blocked(evidence, TenantHighImpactUnavailableReason.StaleData, domainOutcome, friction);
        }

        if (!Enum.IsDefined(evidence.ProjectionLifecycle)
            || evidence.ProjectionLifecycle is not ProjectionLifecycleState.Current)
        {
            return Blocked(evidence, TenantHighImpactUnavailableReason.StaleData, domainOutcome, friction);
        }

        if (string.IsNullOrWhiteSpace(evidence.TenantId)
            || !Enum.IsDefined(evidence.Action)
            || !Enum.IsDefined(evidence.TenantStatus)
            || evidence.TenantStatus is TenantStatus.Unknown)
        {
            return Blocked(evidence, TenantHighImpactUnavailableReason.MissingLifecycleSupport, domainOutcome, friction);
        }

        if (!Enum.IsDefined(evidence.Stage)
            || !Enum.IsDefined(evidence.Viewport)
            || evidence.Viewport is not TenantHighImpactViewportState.Safe)
        {
            return Blocked(evidence, TenantHighImpactUnavailableReason.HighImpactFlowNotReady, domainOutcome, friction);
        }

        if (!Enum.IsDefined(evidence.Authority)
            || evidence.Authority is not TenantHighImpactAuthorityEvidence.Authorized)
        {
            return Blocked(evidence, TenantHighImpactUnavailableReason.MissingPermission, domainOutcome, friction);
        }

        if (!Enum.IsDefined(evidence.NamespaceScope)
            || RequiresNamespaceScope(evidence.Action)
                && evidence.NamespaceScope is not TenantHighImpactNamespaceScopeEvidence.Authorized
            || !RequiresNamespaceScope(evidence.Action)
                && evidence.NamespaceScope is not TenantHighImpactNamespaceScopeEvidence.NotRequired)
        {
            return Blocked(evidence, TenantHighImpactUnavailableReason.MissingPermission, domainOutcome, friction);
        }

        if (!Enum.IsDefined(evidence.Support)
            || evidence.Support is not TenantHighImpactSupportEvidence.Ready)
        {
            return Blocked(evidence, TenantHighImpactUnavailableReason.MissingLifecycleSupport, domainOutcome, friction);
        }

        if (!Enum.IsDefined(evidence.Preview)
            || evidence.Preview is not TenantHighImpactPreviewEvidence.Ready
            || (evidence.Stage is TenantHighImpactEvaluationStage.Confirmation && !evidence.IsInputComplete))
        {
            return Blocked(evidence, TenantHighImpactUnavailableReason.MissingConsequencePreview, domainOutcome, friction);
        }

        if (!Enum.IsDefined(evidence.Proof)
            || evidence.Stage is TenantHighImpactEvaluationStage.Confirmation
                && evidence.Proof is not TenantHighImpactProofEvidence.Ready
                    and not TenantHighImpactProofEvidence.NotRequired)
        {
            return Blocked(evidence, TenantHighImpactUnavailableReason.MissingAuditProof, domainOutcome, friction);
        }

        if (!Enum.IsDefined(evidence.TargetState)
            || !IsTargetStateValid(evidence.Action, evidence.TargetState)
            || !Enum.IsDefined(evidence.Admission)
            || evidence.Admission is not TenantHighImpactAdmissionEvidence.Available)
        {
            return Blocked(evidence, TenantHighImpactUnavailableReason.HighImpactFlowNotReady, domainOutcome, friction);
        }

        if (domainOutcome is not TenantHighImpactDomainOutcome.None)
        {
            return new(
                evidence.TenantId,
                evidence.Action,
                evidence.Stage,
                IsEligible: false,
                TenantHighImpactUnavailableReason.None,
                $"Tenants.HighImpact.DomainOutcome.{domainOutcome}",
                $"Tenants.HighImpact.DomainRecovery.{domainOutcome}",
                domainOutcome,
                friction);
        }

        return new(
            evidence.TenantId,
            evidence.Action,
            evidence.Stage,
            IsEligible: true,
            TenantHighImpactUnavailableReason.None,
            friction ? "Tenants.HighImpact.AvailableWithFriction" : "Tenants.HighImpact.Available",
            "Tenants.HighImpact.Recovery.None",
            TenantHighImpactDomainOutcome.None,
            friction);
    }

    private static TenantHighImpactActionAvailability Blocked(
        TenantHighImpactActionEvidence evidence,
        TenantHighImpactUnavailableReason reason,
        TenantHighImpactDomainOutcome domainOutcome,
        bool friction)
        => new(
            evidence.TenantId,
            evidence.Action,
            evidence.Stage,
            IsEligible: false,
            reason,
            $"Tenants.HighImpact.Unavailable.{reason}",
            $"Tenants.HighImpact.Recovery.{reason}",
            domainOutcome,
            friction);

    private static TenantHighImpactDomainOutcome DomainOutcome(TenantHighImpactActionEvidence evidence)
    {
        if (evidence.Action is TenantHighImpactAction.EnableTenant
            && evidence.TenantStatus is TenantStatus.Active
            || evidence.Action is TenantHighImpactAction.DisableTenant
            && evidence.TenantStatus is TenantStatus.Disabled)
        {
            return TenantHighImpactDomainOutcome.LifecycleStateAlreadySet;
        }

        if (RequiresNamespaceScope(evidence.Action)
            && evidence.TenantStatus is TenantStatus.Disabled)
        {
            return TenantHighImpactDomainOutcome.TenantDisabled;
        }

        if (evidence.Action is TenantHighImpactAction.SetConfiguration
            && evidence.Authority is TenantHighImpactAuthorityEvidence.Authorized
            && evidence.NamespaceScope is TenantHighImpactNamespaceScopeEvidence.Authorized
            && evidence.TargetState is TenantHighImpactTargetState.AlreadyApplied)
        {
            return TenantHighImpactDomainOutcome.ConfigurationAlreadyApplied;
        }

        return evidence.Action is TenantHighImpactAction.RemoveConfiguration
            && evidence.Authority is TenantHighImpactAuthorityEvidence.Authorized
            && evidence.NamespaceScope is TenantHighImpactNamespaceScopeEvidence.Authorized
            && evidence.TargetState is TenantHighImpactTargetState.Missing
                ? TenantHighImpactDomainOutcome.ConfigurationKeyNotFound
                : TenantHighImpactDomainOutcome.None;
    }

    private static bool RequiresNamespaceScope(TenantHighImpactAction action)
        => action is TenantHighImpactAction.SetConfiguration
            or TenantHighImpactAction.RemoveConfiguration;

    private static bool IsTargetStateValid(
        TenantHighImpactAction action,
        TenantHighImpactTargetState targetState)
        => action switch
        {
            TenantHighImpactAction.EnableTenant or TenantHighImpactAction.DisableTenant
                => targetState is TenantHighImpactTargetState.NotApplicable,
            TenantHighImpactAction.SetConfiguration
                => targetState is TenantHighImpactTargetState.Unknown
                    or TenantHighImpactTargetState.Present
                    or TenantHighImpactTargetState.AlreadyApplied,
            TenantHighImpactAction.RemoveConfiguration
                => targetState is TenantHighImpactTargetState.Unknown
                    or TenantHighImpactTargetState.Present
                    or TenantHighImpactTargetState.Missing,
            _ => false,
        };
}
