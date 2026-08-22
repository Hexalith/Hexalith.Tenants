using System.Reflection;

using Hexalith.EventStore.Contracts.Queries;
using Hexalith.FrontComposer.Shell.State.Navigation;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.UI.State.TenantDetail;

using Shouldly;
using Fluxor;
using NSubstitute;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class TenantHighImpactActionAvailabilityTests
{
    [Theory]
    [InlineData(TenantHighImpactAction.EnableTenant)]
    [InlineData(TenantHighImpactAction.DisableTenant)]
    [InlineData(TenantHighImpactAction.SetConfiguration)]
    [InlineData(TenantHighImpactAction.RemoveConfiguration)]
    public void Each_action_is_independently_eligible_with_complete_current_evidence(TenantHighImpactAction action)
    {
        TenantHighImpactActionAvailability result = Evaluate(Qualifying(action));

        result.IsEligible.ShouldBeTrue();
        result.Action.ShouldBe(action);
        result.UnavailableReason.ShouldBe(TenantHighImpactUnavailableReason.None);
        result.DomainOutcome.ShouldBe(TenantHighImpactDomainOutcome.None);
    }

    [Fact]
    public void Configuration_scope_failure_does_not_block_an_independent_lifecycle_action()
    {
        TenantHighImpactActionAvailability disable = Evaluate(Qualifying(TenantHighImpactAction.DisableTenant));
        TenantHighImpactActionAvailability set = Evaluate(Qualifying(TenantHighImpactAction.SetConfiguration) with
        {
            NamespaceScope = TenantHighImpactNamespaceScopeEvidence.Missing,
        });
        TenantHighImpactActionAvailability remove = Evaluate(Qualifying(TenantHighImpactAction.RemoveConfiguration) with
        {
            NamespaceScope = TenantHighImpactNamespaceScopeEvidence.Missing,
        });

        disable.IsEligible.ShouldBeTrue();
        set.UnavailableReason.ShouldBe(TenantHighImpactUnavailableReason.MissingPermission);
        remove.UnavailableReason.ShouldBe(TenantHighImpactUnavailableReason.MissingPermission);
    }

    [Fact]
    public void Deterministic_precedence_reports_stale_data_before_other_failed_evidence()
    {
        TenantHighImpactActionEvidence evidence = Qualifying(TenantHighImpactAction.SetConfiguration) with
        {
            Freshness = TenantHighImpactFreshnessState.Stale,
            Authority = TenantHighImpactAuthorityEvidence.Indeterminate,
            Support = TenantHighImpactSupportEvidence.Missing,
            Preview = TenantHighImpactPreviewEvidence.Missing,
            Admission = TenantHighImpactAdmissionEvidence.Busy,
            Viewport = TenantHighImpactViewportState.Unsafe,
        };

        Evaluate(evidence).UnavailableReason.ShouldBe(TenantHighImpactUnavailableReason.StaleData);
    }

    [Theory]
    [InlineData(TenantHighImpactUnavailableReason.MissingPermission)]
    [InlineData(TenantHighImpactUnavailableReason.MissingLifecycleSupport)]
    [InlineData(TenantHighImpactUnavailableReason.MissingConsequencePreview)]
    [InlineData(TenantHighImpactUnavailableReason.MissingAuditProof)]
    [InlineData(TenantHighImpactUnavailableReason.HighImpactFlowNotReady)]
    public void Canonical_reason_has_a_matching_safe_recovery(TenantHighImpactUnavailableReason reason)
    {
        TenantHighImpactActionEvidence evidence = Qualifying(TenantHighImpactAction.SetConfiguration) with
        {
            Stage = TenantHighImpactEvaluationStage.Confirmation,
            IsInputComplete = true,
            Authority = reason is TenantHighImpactUnavailableReason.MissingPermission
                ? TenantHighImpactAuthorityEvidence.Indeterminate
                : TenantHighImpactAuthorityEvidence.Authorized,
            Support = reason is TenantHighImpactUnavailableReason.MissingLifecycleSupport
                ? TenantHighImpactSupportEvidence.Missing
                : TenantHighImpactSupportEvidence.Ready,
            Preview = reason is TenantHighImpactUnavailableReason.MissingConsequencePreview
                ? TenantHighImpactPreviewEvidence.Missing
                : TenantHighImpactPreviewEvidence.Ready,
            Proof = reason is TenantHighImpactUnavailableReason.MissingAuditProof
                ? TenantHighImpactProofEvidence.Missing
                : TenantHighImpactProofEvidence.NotRequired,
            Admission = reason is TenantHighImpactUnavailableReason.HighImpactFlowNotReady
                ? TenantHighImpactAdmissionEvidence.Busy
                : TenantHighImpactAdmissionEvidence.Available,
        };

        TenantHighImpactActionAvailability result = Evaluate(evidence);

        result.UnavailableReason.ShouldBe(reason);
        result.SafeMessageKey.ShouldBe($"Tenants.HighImpact.Unavailable.{reason}");
        result.RecoveryKey.ShouldBe($"Tenants.HighImpact.Recovery.{reason}");
    }

    [Fact]
    public void Same_state_lifecycle_is_a_domain_outcome_not_an_infrastructure_failure()
    {
        TenantHighImpactActionAvailability result = Evaluate(
            Qualifying(TenantHighImpactAction.EnableTenant) with { TenantStatus = TenantStatus.Active });

        result.IsEligible.ShouldBeFalse();
        result.UnavailableReason.ShouldBe(TenantHighImpactUnavailableReason.None);
        result.DomainOutcome.ShouldBe(TenantHighImpactDomainOutcome.LifecycleStateAlreadySet);
    }

    [Theory]
    [InlineData(TenantHighImpactAction.SetConfiguration)]
    [InlineData(TenantHighImpactAction.RemoveConfiguration)]
    public void Disabled_configuration_is_a_domain_outcome(TenantHighImpactAction action)
    {
        TenantHighImpactActionAvailability result = Evaluate(
            Qualifying(action) with { TenantStatus = TenantStatus.Disabled });

        result.UnavailableReason.ShouldBe(TenantHighImpactUnavailableReason.None);
        result.DomainOutcome.ShouldBe(TenantHighImpactDomainOutcome.TenantDisabled);
    }

    [Fact]
    public void Already_applied_set_and_proven_missing_remove_are_distinct_domain_outcomes()
    {
        Evaluate(Qualifying(TenantHighImpactAction.SetConfiguration) with
        {
            TargetState = TenantHighImpactTargetState.AlreadyApplied,
        }).DomainOutcome.ShouldBe(TenantHighImpactDomainOutcome.ConfigurationAlreadyApplied);
        Evaluate(Qualifying(TenantHighImpactAction.RemoveConfiguration) with
        {
            TargetState = TenantHighImpactTargetState.Missing,
        }).DomainOutcome.ShouldBe(TenantHighImpactDomainOutcome.ConfigurationKeyNotFound);
    }

    [Fact]
    public void Entry_can_open_while_incomplete_confirmation_remains_blocked()
    {
        TenantHighImpactActionEvidence entry = Qualifying(TenantHighImpactAction.SetConfiguration) with
        {
            IsInputComplete = false,
        };

        Evaluate(entry).IsEligible.ShouldBeTrue();
        TenantHighImpactActionAvailability confirmation = Evaluate(entry with
        {
            Stage = TenantHighImpactEvaluationStage.Confirmation,
        });
        confirmation.UnavailableReason.ShouldBe(TenantHighImpactUnavailableReason.MissingConsequencePreview);
    }

    [Fact]
    public void Out_of_scope_confirmation_reports_permission_without_disclosing_a_target()
    {
        TenantHighImpactActionAvailability result = Evaluate(
            Qualifying(TenantHighImpactAction.RemoveConfiguration) with
            {
                Stage = TenantHighImpactEvaluationStage.Confirmation,
                NamespaceScope = TenantHighImpactNamespaceScopeEvidence.Missing,
                IsInputComplete = true,
                TargetState = TenantHighImpactTargetState.Unknown,
            });

        result.UnavailableReason.ShouldBe(TenantHighImpactUnavailableReason.MissingPermission);
        result.DomainOutcome.ShouldBe(TenantHighImpactDomainOutcome.None);
    }

    [Fact]
    public void Missing_target_is_not_exposed_without_role_and_namespace_authorization()
    {
        TenantHighImpactActionAvailability result = Evaluate(
            Qualifying(TenantHighImpactAction.RemoveConfiguration) with
            {
                Authority = TenantHighImpactAuthorityEvidence.MissingPermission,
                NamespaceScope = TenantHighImpactNamespaceScopeEvidence.Missing,
                TargetState = TenantHighImpactTargetState.Missing,
            });

        result.UnavailableReason.ShouldBe(TenantHighImpactUnavailableReason.MissingPermission);
        result.DomainOutcome.ShouldBe(TenantHighImpactDomainOutcome.None);
    }

    [Fact]
    public void Aging_and_refreshing_with_current_baseline_proceed_with_visible_friction()
    {
        TenantHighImpactActionAvailability aging = Evaluate(Qualifying(TenantHighImpactAction.DisableTenant) with
        {
            Freshness = TenantHighImpactFreshnessState.Aging,
        });
        TenantHighImpactActionAvailability refreshing = Evaluate(Qualifying(TenantHighImpactAction.DisableTenant) with
        {
            Freshness = TenantHighImpactFreshnessState.Refreshing,
            HasCurrentBaseline = true,
        });

        aging.IsEligible.ShouldBeTrue();
        aging.RequiresFriction.ShouldBeTrue();
        refreshing.IsEligible.ShouldBeTrue();
        refreshing.RequiresFriction.ShouldBeTrue();
    }

    [Fact]
    public void Refreshing_without_current_baseline_fails_closed_as_stale_data()
    {
        TenantHighImpactActionAvailability result = Evaluate(Qualifying(TenantHighImpactAction.DisableTenant) with
        {
            Freshness = TenantHighImpactFreshnessState.Refreshing,
            HasCurrentBaseline = false,
        });

        result.UnavailableReason.ShouldBe(TenantHighImpactUnavailableReason.StaleData);
    }

    [Theory]
    [InlineData(TenantHighImpactViewportState.Unknown)]
    [InlineData(TenantHighImpactViewportState.Unsafe)]
    public void Unknown_or_unsafe_viewport_fails_closed(TenantHighImpactViewportState viewport)
    {
        TenantHighImpactActionAvailability result = Evaluate(Qualifying(TenantHighImpactAction.DisableTenant) with
        {
            Viewport = viewport,
        });

        result.UnavailableReason.ShouldBe(TenantHighImpactUnavailableReason.HighImpactFlowNotReady);
    }

    [Fact]
    public void Proof_not_required_qualifies_while_declared_missing_proof_blocks_confirmation()
    {
        TenantHighImpactActionEvidence confirmation = Qualifying(TenantHighImpactAction.DisableTenant) with
        {
            Stage = TenantHighImpactEvaluationStage.Confirmation,
            IsInputComplete = true,
        };

        Evaluate(confirmation with { Proof = TenantHighImpactProofEvidence.NotRequired }).IsEligible.ShouldBeTrue();
        Evaluate(confirmation with { Proof = TenantHighImpactProofEvidence.Missing })
            .UnavailableReason.ShouldBe(TenantHighImpactUnavailableReason.MissingAuditProof);
    }

    [Theory]
    [InlineData(TenantHighImpactAction.EnableTenant)]
    [InlineData(TenantHighImpactAction.DisableTenant)]
    [InlineData(TenantHighImpactAction.SetConfiguration)]
    [InlineData(TenantHighImpactAction.RemoveConfiguration)]
    public void Every_story_action_requires_an_explicitly_ready_preview(TenantHighImpactAction action)
    {
        Evaluate(Qualifying(action) with { Preview = TenantHighImpactPreviewEvidence.NotRequired })
            .UnavailableReason.ShouldBe(TenantHighImpactUnavailableReason.MissingConsequencePreview);
        Evaluate(Qualifying(action) with { Preview = (TenantHighImpactPreviewEvidence)999 })
            .UnavailableReason.ShouldBe(TenantHighImpactUnavailableReason.MissingConsequencePreview);
    }

    [Fact]
    public void Whitespace_identity_and_undefined_enum_domains_all_fail_closed()
    {
        TenantHighImpactActionEvidence evidence = Qualifying(TenantHighImpactAction.SetConfiguration);

        Evaluate(evidence with { TenantId = "   " }).IsEligible.ShouldBeFalse();
        Evaluate(evidence with { Action = (TenantHighImpactAction)999 }).IsEligible.ShouldBeFalse();
        Evaluate(evidence with { Stage = (TenantHighImpactEvaluationStage)999 }).IsEligible.ShouldBeFalse();
        Evaluate(evidence with { Freshness = (TenantHighImpactFreshnessState)999 }).IsEligible.ShouldBeFalse();
        Evaluate(evidence with { TenantStatus = (TenantStatus)999 }).IsEligible.ShouldBeFalse();
        Evaluate(evidence with { SurfaceKind = (TenantDetailSurfaceKind)999 }).IsEligible.ShouldBeFalse();
        Evaluate(evidence with { ProjectionLifecycle = (ProjectionLifecycleState)999 }).IsEligible.ShouldBeFalse();
        Evaluate(evidence with { Authority = (TenantHighImpactAuthorityEvidence)999 }).IsEligible.ShouldBeFalse();
        Evaluate(evidence with { NamespaceScope = (TenantHighImpactNamespaceScopeEvidence)999 }).IsEligible.ShouldBeFalse();
        Evaluate(evidence with { Support = (TenantHighImpactSupportEvidence)999 }).IsEligible.ShouldBeFalse();
        Evaluate(evidence with { Admission = (TenantHighImpactAdmissionEvidence)999 }).IsEligible.ShouldBeFalse();
        Evaluate(evidence with { Preview = (TenantHighImpactPreviewEvidence)999 }).IsEligible.ShouldBeFalse();
        Evaluate(evidence with
        {
            Stage = TenantHighImpactEvaluationStage.Confirmation,
            IsInputComplete = true,
            Proof = (TenantHighImpactProofEvidence)999,
        }).UnavailableReason.ShouldBe(TenantHighImpactUnavailableReason.MissingAuditProof);
        Evaluate(evidence with { Viewport = (TenantHighImpactViewportState)999 }).IsEligible.ShouldBeFalse();
        Evaluate(evidence with { TargetState = (TenantHighImpactTargetState)999 }).IsEligible.ShouldBeFalse();
    }

    [Fact]
    public async Task FrontComposer_viewport_observation_starts_unknown_and_uses_the_action_tier()
    {
        TenantHighImpactViewportObservation observation = new();
        TenantHighImpactViewportEffects effects = new(observation);
        IDispatcher dispatcher = Substitute.For<IDispatcher>();

        observation.State.ShouldBe(TenantHighImpactViewportState.Unknown);
        await effects.HandleViewportTierChanged(new(ViewportTier.Phone), dispatcher);
        observation.State.ShouldBe(TenantHighImpactViewportState.Unsafe);
        await effects.HandleViewportTierChanged(new(ViewportTier.Desktop), dispatcher);
        observation.State.ShouldBe(TenantHighImpactViewportState.Safe);
        await effects.HandleViewportTierChanged(new((ViewportTier)255), dispatcher);
        observation.State.ShouldBe(TenantHighImpactViewportState.Unknown);
        dispatcher.DidNotReceiveWithAnyArgs().Dispatch(default!);
    }

    [Fact]
    public void Evaluator_surface_is_pure_evidence_in_and_result_out()
    {
        MethodInfo evaluate = typeof(TenantHighImpactActionAvailabilityEvaluator)
            .GetMethod(nameof(TenantHighImpactActionAvailabilityEvaluator.Evaluate))!;

        evaluate.GetParameters().Select(static parameter => parameter.ParameterType)
            .ShouldBe([typeof(TenantHighImpactActionEvidence)]);
        evaluate.ReturnType.ShouldBe(typeof(TenantHighImpactActionAvailability));
        typeof(TenantHighImpactActionAvailabilityEvaluator).GetFields(
            BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .ShouldBeEmpty();
    }

    private static TenantHighImpactActionAvailability Evaluate(TenantHighImpactActionEvidence evidence)
        => TenantHighImpactActionAvailabilityEvaluator.Evaluate(evidence);

    private static TenantHighImpactActionEvidence Qualifying(TenantHighImpactAction action)
        => new(
            "tenant.alpha",
            action,
            TenantHighImpactEvaluationStage.PreviewEntry,
            action is TenantHighImpactAction.EnableTenant ? TenantStatus.Disabled : TenantStatus.Active,
            TenantHighImpactFreshnessState.Current,
            HasCurrentBaseline: true,
            TenantDetailSurfaceKind.Ready,
            ProjectionLifecycleState.Current,
            TenantHighImpactAuthorityEvidence.Authorized,
            action is TenantHighImpactAction.SetConfiguration or TenantHighImpactAction.RemoveConfiguration
                ? TenantHighImpactNamespaceScopeEvidence.Authorized
                : TenantHighImpactNamespaceScopeEvidence.NotRequired,
            TenantHighImpactSupportEvidence.Ready,
            TenantHighImpactAdmissionEvidence.Available,
            TenantHighImpactPreviewEvidence.Ready,
            TenantHighImpactProofEvidence.NotRequired,
            TenantHighImpactViewportState.Safe,
            IsInputComplete: true,
            action is TenantHighImpactAction.RemoveConfiguration
                ? TenantHighImpactTargetState.Present
                : action is TenantHighImpactAction.SetConfiguration
                    ? TenantHighImpactTargetState.Unknown
                    : TenantHighImpactTargetState.NotApplicable);
}
