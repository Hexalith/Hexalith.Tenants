using System.Globalization;
using System.Reflection;

using Bunit;

using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Tenants.Lifecycle;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.EventStore.Client.Projections;
using Hexalith.Tenants.UI.State.TenantCommands;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class TenantLifecycleActionAvailabilityTests : FluentBunitContext
{
    [Fact]
    public void Lifecycle_availability_renders_stable_selectors_visible_reasons_and_disabled_actions()
    {
        RegisterServices();

        IRenderedComponent<TenantLifecycleActionAvailability> cut = Render<TenantLifecycleActionAvailability>(parameters => parameters
            .Add(component => component.Lifecycle, ProjectionLifecycleState.Current)
            .Add(component => component.TenantId, "tenant.alpha")
            .Add(component => component.CurrentStatus, TenantStatus.Active)
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(component => component.Freshness, ReadModelFreshnessState.Current)
            .Add(component => component.IsCommandSurfaceConnected, true)
            .Add(component => component.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Indeterminate)
            .Add(component => component.GovernanceReadiness, TenantLifecycleGovernanceReadiness.Unresolved));

        cut.Find("[data-testid='tenants-lifecycle-actions']");
        cut.Find("[data-testid='tenants-lifecycle-state']").TextContent.ShouldContain("Active");
        cut.Find("[data-testid='tenants-lifecycle-current-status']").TextContent.ShouldContain("Active");
        cut.Find("[data-testid='tenants-lifecycle-freshness']").TextContent.ShouldContain("Current");
        cut.Find("[data-testid='tenants-lifecycle-projection-lifecycle-badge']")
            .TextContent.Trim()
            .ShouldBe("Current");
        cut.Find("[data-testid='tenants-lifecycle-governance-gate']").TextContent.ShouldContain("Unresolved");
        cut.Find("[data-testid='tenants-lifecycle-enable']").GetAttribute("disabled").ShouldNotBeNull();
        cut.Find("[data-testid='tenants-lifecycle-disable']").GetAttribute("disabled").ShouldNotBeNull();
        cut.FindAll("[data-testid='tenants-lifecycle-unavailable-reason']").Count.ShouldBe(2);
        cut.FindAll("[data-testid='tenants-lifecycle-unavailable-reason']")
            .ShouldAllBe(static reason => reason.GetAttribute("tabindex") == "0");
        cut.Find("[data-testid='tenants-lifecycle-live-region']").GetAttribute("aria-live").ShouldBe("assertive");
        cut.Markup.ShouldNotContain("<form", Case.Insensitive);
        cut.Markup.ShouldNotContain("type=\"submit\"", Case.Insensitive);
        // Assert against visible text: Fluent v5 badge tokens (color="success", --colorStatusSuccessForeground)
        // live in attributes/styles and would false-trigger a raw-markup "Success" guard.
        cut.VisibleText().ShouldNotContain("Success", Case.Insensitive);
        cut.Markup.ShouldNotContain("accepted", Case.Insensitive);
        cut.Markup.ShouldNotContain("confirmed", Case.Insensitive);
    }

    [Fact]
    public void Open_lifecycle_confirm_closes_when_tenant_identity_changes()
    {
        RegisterServices();

        IRenderedComponent<TenantLifecycleActionAvailability> cut = Render<TenantLifecycleActionAvailability>(parameters => parameters
            .Add(component => component.Lifecycle, ProjectionLifecycleState.Current)
            .Add(component => component.TenantId, "tenant.alpha")
            .Add(component => component.Detail, Detail("tenant.alpha", TenantStatus.Active))
            .Add(component => component.ProjectionVersion, "tenant-sequence:41")
            .Add(component => component.CurrentStatus, TenantStatus.Active)
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(component => component.Freshness, ReadModelFreshnessState.Current)
            .Add(component => component.IsCommandSurfaceConnected, true)
            .Add(component => component.IsCommandSurfaceAvailable, true)
            .Add(component => component.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Authorized)
            .Add(component => component.GovernanceReadiness, TenantLifecycleGovernanceReadiness.Ready));

        cut.Find("[data-testid='tenants-lifecycle-disable']").Click();
        cut.Find("[data-testid='tenants-lifecycle-command-flow']");

        cut.Render(parameters => parameters
            .Add(component => component.Lifecycle, ProjectionLifecycleState.Current)
            .Add(component => component.TenantId, "tenant.beta")
            .Add(component => component.Detail, Detail("tenant.beta", TenantStatus.Active))
            .Add(component => component.ProjectionVersion, "tenant-sequence:41")
            .Add(component => component.CurrentStatus, TenantStatus.Active)
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(component => component.Freshness, ReadModelFreshnessState.Current)
            .Add(component => component.IsCommandSurfaceConnected, true)
            .Add(component => component.IsCommandSurfaceAvailable, true)
            .Add(component => component.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Authorized)
            .Add(component => component.GovernanceReadiness, TenantLifecycleGovernanceReadiness.Ready));

        cut.FindAll("[data-testid='tenants-lifecycle-command-flow']").ShouldBeEmpty();
    }

    [Fact]
    public void Facts_header_keeps_lifecycle_badge_independent_of_freshness()
    {
        RegisterServices();

        IRenderedComponent<TenantLifecycleActionAvailability> cut = Render<TenantLifecycleActionAvailability>(parameters => parameters
            .Add(component => component.Lifecycle, ProjectionLifecycleState.Stale)
            .Add(component => component.TenantId, "tenant.alpha")
            .Add(component => component.CurrentStatus, TenantStatus.Active)
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(component => component.Freshness, ReadModelFreshnessState.Current)
            .Add(component => component.IsCommandSurfaceConnected, true)
            .Add(component => component.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Authorized)
            .Add(component => component.GovernanceReadiness, TenantLifecycleGovernanceReadiness.Ready));

        cut.Find("[data-testid='tenants-lifecycle-freshness-badge']").TextContent.ShouldContain("Current");
        cut.Find("[data-testid='tenants-lifecycle-projection-lifecycle-badge']")
            .TextContent.Trim()
            .ShouldBe("Stale");
        (cut.Find("[data-testid='tenants-lifecycle-projection-lifecycle-badge']").GetAttribute("class") ?? string.Empty)
            .ShouldContain("projection-lifecycle-badge--stale");
    }

    [Fact]
    public void Governance_blocking_keeps_lifecycle_command_unavailable_with_high_impact_reason()
    {
        RegisterServices();

        IRenderedComponent<TenantLifecycleActionAvailability> cut = Render<TenantLifecycleActionAvailability>(parameters => parameters
            .Add(component => component.Lifecycle, ProjectionLifecycleState.Current)
            .Add(component => component.TenantId, "tenant.alpha")
            .Add(component => component.CurrentStatus, TenantStatus.Active)
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(component => component.Freshness, ReadModelFreshnessState.Current)
            .Add(component => component.IsCommandSurfaceConnected, true)
            .Add(component => component.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Authorized)
            .Add(component => component.GovernanceReadiness, TenantLifecycleGovernanceReadiness.Unresolved));

        cut.Find("[data-testid='tenants-lifecycle-disable']").GetAttribute("disabled").ShouldNotBeNull();
        cut.Find("[data-testid='tenants-lifecycle-actions']").TextContent.ShouldContain("read-only", Case.Insensitive);
        cut.Find("[data-testid='tenants-lifecycle-actions']").TextContent.ShouldContain("aggregate admission", Case.Insensitive);
    }

    [Fact]
    public void Disabled_tenant_keeps_disabled_projection_truth_and_blocks_enable_without_optimistic_transition()
    {
        RegisterServices();

        IRenderedComponent<TenantLifecycleActionAvailability> cut = Render<TenantLifecycleActionAvailability>(parameters => parameters
            .Add(component => component.Lifecycle, ProjectionLifecycleState.Current)
            .Add(component => component.TenantId, "tenant.disabled")
            .Add(component => component.CurrentStatus, TenantStatus.Disabled)
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(component => component.Freshness, ReadModelFreshnessState.Current)
            .Add(component => component.IsCommandSurfaceConnected, true)
            .Add(component => component.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Authorized)
            .Add(component => component.GovernanceReadiness, TenantLifecycleGovernanceReadiness.Unresolved));

        cut.Find("[data-testid='tenants-lifecycle-state']").TextContent.ShouldContain("Disabled");
        cut.Find("[data-testid='tenants-lifecycle-current-status']").TextContent.ShouldContain("Disabled");
        cut.Find("[data-testid='tenants-lifecycle-enable']").GetAttribute("disabled").ShouldNotBeNull();
        cut.Find("[data-testid='tenants-lifecycle-disable']").GetAttribute("disabled").ShouldNotBeNull();
        cut.Find("#tenants-lifecycle-enable-reason").TextContent.ShouldContain("read-only", Case.Insensitive);
        cut.Find("#tenants-lifecycle-enable-reason").TextContent.ShouldContain("aggregate admission", Case.Insensitive);
        // Unresolved governance blocks disable the same way it blocks enable: the same-state domain claim
        // must not be shown as if proven while the admission/viewport gate behind it is still unresolved.
        cut.Find("#tenants-lifecycle-disable-reason").TextContent.ShouldContain("read-only", Case.Insensitive);
        cut.Find("#tenants-lifecycle-disable-reason").TextContent.ShouldContain("aggregate admission", Case.Insensitive);
        cut.Find("[data-testid='tenants-lifecycle-actions']").TextContent.ShouldNotContain("already has the requested lifecycle state");
        cut.Markup.ShouldNotContain("confirmed", Case.Insensitive);
    }

    [Fact]
    public void Same_state_action_names_the_safe_expected_domain_outcome()
    {
        RegisterServices();

        IRenderedComponent<TenantLifecycleActionAvailability> cut = Render<TenantLifecycleActionAvailability>(parameters => parameters
            .Add(component => component.Lifecycle, ProjectionLifecycleState.Current)
            .Add(component => component.TenantId, "tenant.alpha")
            .Add(component => component.CurrentStatus, TenantStatus.Active)
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(component => component.Freshness, ReadModelFreshnessState.Current)
            .Add(component => component.IsCommandSurfaceConnected, true)
            .Add(component => component.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Authorized)
            .Add(component => component.GovernanceReadiness, TenantLifecycleGovernanceReadiness.Ready));

        cut.Find("[data-testid='tenants-lifecycle-enable']").GetAttribute("disabled").ShouldNotBeNull();
        string reasonText = cut.Find("#tenants-lifecycle-enable-reason").TextContent;
        reasonText.ShouldContain("already has the requested lifecycle state", Case.Insensitive);
        reasonText.Split(
            "already has the requested lifecycle state",
            StringSplitOptions.None).Length.ShouldBe(2);
        cut.Find("[data-testid='tenants-lifecycle-live-region']").GetAttribute("aria-live").ShouldBe("polite");
    }

    [Fact]
    public void Narrow_safety_context_keeps_available_direction_unavailable_with_visible_mobile_reason()
    {
        RegisterServices();

        IRenderedComponent<TenantLifecycleActionAvailability> cut = Render<TenantLifecycleActionAvailability>(parameters => parameters
            .Add(component => component.Lifecycle, ProjectionLifecycleState.Current)
            .Add(component => component.TenantId, "tenant.alpha")
            .Add(component => component.CurrentStatus, TenantStatus.Active)
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(component => component.Freshness, ReadModelFreshnessState.Current)
            .Add(component => component.IsCommandSurfaceConnected, true)
            .Add(component => component.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Authorized)
            .Add(component => component.GovernanceReadiness, TenantLifecycleGovernanceReadiness.Ready)
            .Add(component => component.IsNarrowSafetyContext, true));

        cut.Find("[data-testid='tenants-lifecycle-disable']").GetAttribute("disabled").ShouldNotBeNull();
        cut.Find("[data-testid='tenants-lifecycle-actions']").TextContent.ShouldContain("measured viewport");
        cut.Find("[data-testid='tenants-lifecycle-current-status']").TextContent.ShouldContain("Active");
        cut.Find("[data-testid='tenants-lifecycle-freshness']").TextContent.ShouldContain("Current");
    }

    [Fact]
    public void Lifecycle_component_uses_existing_command_gateway_and_does_not_submit_from_availability_model()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        string component = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Tenants",
            "Lifecycle",
            "TenantLifecycleActionAvailability.razor"));
        string flow = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Tenants",
            "Lifecycle",
            "TenantLifecycleCommandFlow.razor"));
        string commandGateway = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Services",
            "Gateways",
            "ITenantCommandGateway.cs"));

        component.ShouldNotContain("SubmitCommand");
        flow.ShouldContain("ITenantCommandGateway");
        flow.ShouldContain("EnableTenantAsync");
        flow.ShouldContain("DisableTenantAsync");
        commandGateway.ShouldContain("EnableTenantAsync");
        commandGateway.ShouldContain("DisableTenantAsync");
    }

    [Fact]
    public void Ready_authorized_lifecycle_flow_requires_confirmation_and_confirms_only_after_projection_evidence()
    {
        var gateway = new StubTenantCommandGateway
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-life", "correlation-life"),
            Status = new TenantCommandStatusResult(Hexalith.EventStore.Contracts.Commands.CommandStatus.Completed, EventCount: 1),
        };
        List<bool> activity = [];
        int proofReads = 0;
        RegisterServices(gateway);

        IRenderedComponent<TenantLifecycleActionAvailability> cut = Render<TenantLifecycleActionAvailability>(parameters => parameters
            .Add(component => component.Lifecycle, ProjectionLifecycleState.Current)
            .Add(component => component.TenantId, "tenant.alpha")
            .Add(component => component.Detail, Detail("tenant.alpha", TenantStatus.Active))
            .Add(component => component.ProjectionVersion, "tenant-sequence:41")
            .Add(component => component.CurrentStatus, TenantStatus.Active)
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(component => component.Freshness, ReadModelFreshnessState.Current)
            .Add(component => component.IsCommandSurfaceConnected, true)
            .Add(component => component.IsCommandSurfaceAvailable, true)
            .Add(component => component.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Authorized)
            .Add(component => component.GovernanceReadiness, TenantLifecycleGovernanceReadiness.Ready)
            .Add(component => component.OnCommandActivityChanged, active => activity.Add(active))
            .Add(component => component.OnProjectionRefreshRequested, () => Task.CompletedTask)
            .Add(component => component.AuthorizationReflectionProvider, () => Task.FromResult(TenantLifecycleAuthorizationReflectionState.Authorized))
            .Add(component => component.ProjectionEvidenceProvider, request => Task.FromResult<TenantDetailSnapshot?>(
                ++proofReads == 1
                    ? Proof(request.TenantId, TenantStatus.Active, "tenant-sequence:41")
                    : Proof(request.TenantId, TenantStatus.Disabled, "tenant-sequence:42"))));

        cut.Find("[data-testid='tenants-lifecycle-disable']").GetAttribute("disabled").ShouldBeNull();
        cut.Find("[data-testid='tenants-lifecycle-disable']").Click();

        cut.Find("[data-testid='tenants-lifecycle-preview']");
        cut.Find("[data-testid='tenants-lifecycle-focus-start']");
        cut.Find("[data-testid='tenants-lifecycle-focus-end']");
        cut.FindAll("[data-testid='tenants-lifecycle-preview-item']").Count.ShouldBe(10);
        cut.Find("[data-testid='tenants-lifecycle-confirmation']").Change("wrong");
        cut.Find("form").Submit();

        cut.Find("[data-testid='tenants-lifecycle-validation']").TextContent.ShouldContain("tenant.alpha");
        gateway.DisableSubmissions.ShouldBe(0);

        cut.Find("[data-testid='tenants-lifecycle-confirmation']").Change("tenant.alpha");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
            cut.FindComponent<TenantLifecycleCommandFlow>().Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Confirmed));
        gateway.LastDisableRequest.ShouldNotBeNull().TenantId.ShouldBe("tenant.alpha");
        gateway.DisableSubmissions.ShouldBe(1);
        activity.ShouldBe([true, false]);
        cut.Find("[data-testid='tenants-lifecycle-confirmed-status']").TextContent.ShouldContain("Disabled");
        cut.Markup.ShouldNotContain("correlation-life", Case.Insensitive);
    }

    [Fact]
    public void Cancel_and_escape_close_lifecycle_preview_without_gateway_submission()
    {
        var gateway = new StubTenantCommandGateway
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-life", "correlation-life"),
        };
        RegisterServices(gateway);

        IRenderedComponent<TenantLifecycleActionAvailability> cut = Render<TenantLifecycleActionAvailability>(parameters => parameters
            .Add(component => component.Lifecycle, ProjectionLifecycleState.Current)
            .Add(component => component.TenantId, "tenant.alpha")
            .Add(component => component.Detail, Detail("tenant.alpha", TenantStatus.Active))
            .Add(component => component.ProjectionVersion, "tenant-sequence:41")
            .Add(component => component.CurrentStatus, TenantStatus.Active)
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(component => component.Freshness, ReadModelFreshnessState.Current)
            .Add(component => component.IsCommandSurfaceConnected, true)
            .Add(component => component.IsCommandSurfaceAvailable, true)
            .Add(component => component.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Authorized)
            .Add(component => component.GovernanceReadiness, TenantLifecycleGovernanceReadiness.Ready));

        cut.Find("[data-testid='tenants-lifecycle-disable']").Click();
        cut.Find("[data-testid='tenants-lifecycle-preview']");
        cut.Find("[data-testid='tenants-lifecycle-cancel']").Click();

        cut.FindAll("[data-testid='tenants-lifecycle-command-flow']").ShouldBeEmpty();
        gateway.DisableSubmissions.ShouldBe(0);
        cut.Find("[data-testid='tenants-lifecycle-disable']").GetAttribute("disabled").ShouldBeNull();

        cut.Find("[data-testid='tenants-lifecycle-disable']").Click();
        cut.Find("[data-testid='tenants-lifecycle-command-flow']").KeyDown("Escape");

        cut.FindAll("[data-testid='tenants-lifecycle-command-flow']").ShouldBeEmpty();
        gateway.DisableSubmissions.ShouldBe(0);
        cut.Find("[data-testid='tenants-lifecycle-current-status']").TextContent.ShouldContain("Active");
        cut.Markup.ShouldNotContain("correlation-life", Case.Insensitive);
    }

    [Fact]
    public void Pending_attempt_cannot_be_dismissed_and_remount_adopts_handle_without_redispatch()
    {
        var gateway = new StubTenantCommandGateway
        {
            Submission = TenantCommandSubmissionResult.Accepted("ignored-by-stub", "correlation-life"),
            Status = new TenantCommandStatusResult(CommandStatus.Processing),
        };
        List<bool> firstActivity = [];
        RegisterServices(gateway);

        IRenderedComponent<TenantLifecycleActionAvailability> first = RenderLifecycleAvailability(firstActivity);
        first.Find("[data-testid='tenants-lifecycle-disable']").Click();
        first.Find("[data-testid='tenants-lifecycle-confirmation']").Change("tenant.alpha");
        first.Find("form").Submit();

        first.WaitForAssertion(() => first.FindComponent<TenantLifecycleCommandFlow>()
            .Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Accepted));
        gateway.DisableSubmissions.ShouldBe(1);
        gateway.LastDisableMessageId.ShouldNotBeNullOrWhiteSpace();
        string stableMessageId = gateway.LastDisableMessageId!;
        first.Find("[data-testid='tenants-lifecycle-cancel']").Click();
        first.Find("[data-testid='tenants-lifecycle-command-flow']");
        first.Find("[data-testid='tenants-lifecycle-safe-message']").TextContent
            .ShouldContain("cannot be dismissed", Case.Insensitive);
        firstActivity.ShouldBe([true]);
        first.Dispose();

        List<bool> remountActivity = [];
        IRenderedComponent<TenantLifecycleActionAvailability> remounted = RenderLifecycleAvailability(remountActivity);

        remounted.WaitForAssertion(() => remounted.FindComponent<TenantLifecycleCommandFlow>()
            .Instance.Snapshot.MessageId.ShouldBe(stableMessageId));
        gateway.DisableSubmissions.ShouldBe(1);
        gateway.StatusCalls.ShouldBe(2);
        remountActivity.ShouldBe([true]);
        remounted.Find("[data-testid='tenants-lifecycle-enable']").GetAttribute("disabled").ShouldNotBeNull();
        remounted.Find("#tenants-lifecycle-disable-reason").GetAttribute("data-reason-category")
            .ShouldBe("RetainedAttempt");
    }

    [Fact]
    public void Submission_transport_failure_is_localized_terminal_and_releases_activity()
    {
        var gateway = new StubTenantCommandGateway
        {
            SubmissionException = new InvalidOperationException("raw transport stack token"),
        };
        List<bool> activity = [];
        RegisterServices(gateway);
        IRenderedComponent<TenantLifecycleActionAvailability> cut = RenderLifecycleAvailability(activity);

        cut.Find("[data-testid='tenants-lifecycle-disable']").Click();
        cut.Find("[data-testid='tenants-lifecycle-confirmation']").Change("tenant.alpha");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.FindComponent<TenantLifecycleCommandFlow>()
            .Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Failed));
        gateway.DisableSubmissions.ShouldBe(1);
        activity.ShouldBe([true, false]);
        cut.VisibleText().ShouldNotContain("raw transport", Case.Insensitive);
        cut.VisibleText().ShouldNotContain("token", Case.Insensitive);
    }

    [Fact]
    public void Accepted_tracking_mismatch_fails_closed_without_status_poll_and_releases_activity()
    {
        var gateway = new StubTenantCommandGateway
        {
            Submission = TenantCommandSubmissionResult.Accepted("ignored-by-stub", string.Empty),
        };
        List<bool> activity = [];
        RegisterServices(gateway);
        IRenderedComponent<TenantLifecycleActionAvailability> cut = RenderLifecycleAvailability(activity);

        cut.Find("[data-testid='tenants-lifecycle-disable']").Click();
        cut.Find("[data-testid='tenants-lifecycle-confirmation']").Change("tenant.alpha");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.FindComponent<TenantLifecycleCommandFlow>()
            .Instance.Snapshot.SafeMessageKey.ShouldBe("Tenants.Lifecycle.UnableToVerify.TrackingMismatch"));
        gateway.DisableSubmissions.ShouldBe(1);
        gateway.StatusCalls.ShouldBe(0);
        activity.ShouldBe([true, false]);
    }

    [Fact]
    public void Status_transport_failure_is_unable_to_verify_and_releases_activity()
    {
        var gateway = new StubTenantCommandGateway
        {
            Submission = TenantCommandSubmissionResult.Accepted("ignored-by-stub", "correlation-life"),
            StatusException = new InvalidOperationException("raw status payload token"),
        };
        List<bool> activity = [];
        RegisterServices(gateway);
        IRenderedComponent<TenantLifecycleActionAvailability> cut = RenderLifecycleAvailability(activity);

        cut.Find("[data-testid='tenants-lifecycle-disable']").Click();
        cut.Find("[data-testid='tenants-lifecycle-confirmation']").Change("tenant.alpha");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.FindComponent<TenantLifecycleCommandFlow>()
            .Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify));
        gateway.DisableSubmissions.ShouldBe(1);
        activity.ShouldBe([true, false]);
        cut.VisibleText().ShouldNotContain("raw status", Case.Insensitive);
        cut.VisibleText().ShouldNotContain("token", Case.Insensitive);
    }

    [Fact]
    public void Pending_status_propagation_retains_attempt_and_activity_without_redispatch()
    {
        var gateway = new StubTenantCommandGateway
        {
            Submission = TenantCommandSubmissionResult.Accepted("ignored-by-stub", "correlation-life"),
            Status = TenantCommandStatusResult.Pending("Status is not available yet."),
        };
        List<bool> activity = [];
        RegisterServices(gateway);
        IRenderedComponent<TenantLifecycleActionAvailability> cut = RenderLifecycleAvailability(activity);

        cut.Find("[data-testid='tenants-lifecycle-disable']").Click();
        cut.Find("[data-testid='tenants-lifecycle-confirmation']").Change("tenant.alpha");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.FindComponent<TenantLifecycleCommandFlow>()
            .Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Accepted));
        gateway.DisableSubmissions.ShouldBe(1);
        gateway.StatusCalls.ShouldBe(1);
        activity.ShouldBe([true]);
        cut.Find("[data-testid='tenants-lifecycle-safe-message']").TextContent
            .ShouldContain("not available yet", Case.Insensitive);
    }

    [Fact]
    public async Task Authoritative_refresh_nudge_reconciles_without_redispatch_or_recursive_parent_refresh()
    {
        var gateway = new StubTenantCommandGateway
        {
            Submission = TenantCommandSubmissionResult.Accepted("ignored-by-stub", "correlation-life"),
            Status = new TenantCommandStatusResult(CommandStatus.Received),
        };
        TenantStatus projectionStatus = TenantStatus.Active;
        int parentRefreshes = 0;
        RegisterServices(gateway);
        IRenderedComponent<TenantLifecycleActionAvailability> cut = Render<TenantLifecycleActionAvailability>(parameters => parameters
            .Add(component => component.Lifecycle, ProjectionLifecycleState.Current)
            .Add(component => component.TenantId, "tenant.alpha")
            .Add(component => component.Detail, Detail("tenant.alpha", TenantStatus.Active))
            .Add(component => component.ProjectionVersion, "tenant-sequence:41")
            .Add(component => component.CurrentStatus, TenantStatus.Active)
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(component => component.Freshness, ReadModelFreshnessState.Current)
            .Add(component => component.IsCommandSurfaceConnected, true)
            .Add(component => component.IsCommandSurfaceAvailable, true)
            .Add(component => component.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Authorized)
            .Add(component => component.GovernanceReadiness, TenantLifecycleGovernanceReadiness.Ready)
            .Add(component => component.OnProjectionRefreshRequested, () => { parentRefreshes++; })
            .Add(component => component.AuthorizationReflectionProvider, () => Task.FromResult(TenantLifecycleAuthorizationReflectionState.Authorized))
            .Add(component => component.ProjectionEvidenceProvider, request => Task.FromResult<TenantDetailSnapshot?>(
                Proof(
                    request.TenantId,
                    projectionStatus,
                    projectionStatus is TenantStatus.Active ? "tenant-sequence:41" : "tenant-sequence:42"))));
        cut.Find("[data-testid='tenants-lifecycle-disable']").Click();
        cut.Find("[data-testid='tenants-lifecycle-confirmation']").Change("tenant.alpha");
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => cut.FindComponent<TenantLifecycleCommandFlow>()
            .Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Accepted));

        gateway.Status = new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1);
        projectionStatus = TenantStatus.Disabled;
        await cut.InvokeAsync(cut.Instance.ApplySignalRNudgeAsync);

        cut.FindComponent<TenantLifecycleCommandFlow>().Instance.Snapshot.State
            .ShouldBe(TenantCommandLifecycleState.Confirmed);
        gateway.DisableSubmissions.ShouldBe(1);
        gateway.StatusCalls.ShouldBe(2);
        parentRefreshes.ShouldBe(1);
    }

    [Fact]
    public void Open_lifecycle_flow_rechecks_projection_lifecycle_when_parent_evidence_changes()
    {
        var gateway = new StubTenantCommandGateway();
        RegisterServices(gateway);

        IRenderedComponent<TenantLifecycleActionAvailability> cut = Render<TenantLifecycleActionAvailability>(parameters => parameters
            .Add(component => component.Lifecycle, ProjectionLifecycleState.Current)
            .Add(component => component.TenantId, "tenant.alpha")
            .Add(component => component.Detail, Detail("tenant.alpha", TenantStatus.Active))
            .Add(component => component.ProjectionVersion, "tenant-sequence:41")
            .Add(component => component.CurrentStatus, TenantStatus.Active)
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(component => component.Freshness, ReadModelFreshnessState.Current)
            .Add(component => component.IsCommandSurfaceConnected, true)
            .Add(component => component.IsCommandSurfaceAvailable, true)
            .Add(component => component.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Authorized)
            .Add(component => component.GovernanceReadiness, TenantLifecycleGovernanceReadiness.Ready));

        cut.Find("[data-testid='tenants-lifecycle-disable']").Click();
        cut.Find("[data-testid='tenants-lifecycle-preview']");

        cut.Render(parameters => parameters
            .Add(component => component.Lifecycle, ProjectionLifecycleState.Stale));

        cut.Find("[data-testid='tenants-lifecycle-unavailable-reason']").TextContent
            .ShouldContain("current authoritative data", Case.Insensitive);
        cut.Find("[data-testid='tenants-lifecycle-confirm']").GetAttribute("disabled").ShouldNotBeNull();
        gateway.DisableSubmissions.ShouldBe(0);
    }

    [Fact]
    public void Command_surface_unavailable_disables_lifecycle_action_without_gateway_call()
    {
        var gateway = new StubTenantCommandGateway
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-life", "correlation-life"),
        };
        RegisterServices(gateway);

        IRenderedComponent<TenantLifecycleActionAvailability> cut = Render<TenantLifecycleActionAvailability>(parameters => parameters
            .Add(component => component.Lifecycle, ProjectionLifecycleState.Current)
            .Add(component => component.TenantId, "tenant.alpha")
            .Add(component => component.Detail, Detail("tenant.alpha", TenantStatus.Active))
            .Add(component => component.CurrentStatus, TenantStatus.Active)
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(component => component.Freshness, ReadModelFreshnessState.Current)
            .Add(component => component.IsCommandSurfaceConnected, true)
            .Add(component => component.IsCommandSurfaceAvailable, false)
            .Add(component => component.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Authorized)
            .Add(component => component.GovernanceReadiness, TenantLifecycleGovernanceReadiness.Ready));

        cut.Find("[data-testid='tenants-lifecycle-disable']").GetAttribute("disabled").ShouldNotBeNull();
        cut.Find("#tenants-lifecycle-disable-reason").TextContent.ShouldContain("A tenant command is already in flight or the lifecycle");
        cut.Find("[data-testid='tenants-lifecycle-disable']").Click();

        cut.FindAll("[data-testid='tenants-lifecycle-command-flow']").ShouldBeEmpty();
        gateway.DisableSubmissions.ShouldBe(0);
        cut.Find("[data-testid='tenants-lifecycle-current-status']").TextContent.ShouldContain("Active");
        cut.Find("[data-testid='tenants-lifecycle-live-region']").GetAttribute("aria-live").ShouldBe("assertive");
        cut.Markup.ShouldNotContain("correlation-life", Case.Insensitive);
    }

    [Fact]
    public void Lifecycle_command_activity_lock_is_held_until_projection_confirms()
    {
        var gateway = new StubTenantCommandGateway
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-life", "correlation-life"),
            Status = new TenantCommandStatusResult(Hexalith.EventStore.Contracts.Commands.CommandStatus.Completed, EventCount: 1),
        };
        TenantStatus projectionStatus = TenantStatus.Active;
        List<bool> activity = [];
        RegisterServices(gateway);

        IRenderedComponent<TenantLifecycleCommandFlow> cut = Render<TenantLifecycleCommandFlow>(parameters => parameters
            .Add(component => component.Detail, Detail("tenant.alpha", TenantStatus.Active))
            .Add(component => component.ProjectionVersion, "tenant-sequence:41")
            .Add(component => component.Availability, AvailableLifecycle(TenantStatus.Active, TenantLifecycleOperation.DisableTenant))
            .Add(component => component.IsCommandSurfaceAvailable, true)
            .Add(component => component.OnProjectionRefreshRequested, () => Task.CompletedTask)
            .Add(component => component.OnCommandActivityChanged, active => activity.Add(active))
            .Add(component => component.AuthorizationReflectionProvider, () => Task.FromResult(TenantLifecycleAuthorizationReflectionState.Authorized))
            .Add(component => component.ProjectionEvidenceProvider, request => Task.FromResult<TenantDetailSnapshot?>(
                Proof(
                    request.TenantId,
                    projectionStatus,
                    projectionStatus is TenantStatus.Active ? "tenant-sequence:41" : "tenant-sequence:42"))));

        cut.Find("[data-testid='tenants-lifecycle-confirmation']").Change("tenant.alpha");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
            cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending));
        activity.ShouldBe([true]);
        cut.Instance.Snapshot.LastConfirmedStatus.ShouldBe(TenantStatus.Active);

        projectionStatus = TenantStatus.Disabled;
        cut.Find("[data-testid='tenants-lifecycle-refresh']").Click();

        cut.WaitForAssertion(() =>
            cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Confirmed));
        activity.ShouldBe([true, false]);
        cut.Instance.Snapshot.LastConfirmedStatus.ShouldBe(TenantStatus.Disabled);
    }

    [Fact]
    public void Gateway_unavailable_status_refresh_releases_lifecycle_command_activity_lock()
    {
        RegisterServices();
        List<bool> activity = [];
        TenantDetail detail = Detail("tenant.alpha", TenantStatus.Active);
        TenantLifecycleAvailability availability = AvailableLifecycle(TenantStatus.Active, TenantLifecycleOperation.DisableTenant);

        IRenderedComponent<TenantLifecycleCommandFlow> cut = Render<TenantLifecycleCommandFlow>(parameters => parameters
            .Add(component => component.Detail, detail)
            .Add(component => component.Availability, availability)
            .Add(component => component.IsCommandSurfaceAvailable, true)
            .Add(component => component.OnProjectionRefreshRequested, () => Task.CompletedTask)
            .Add(component => component.OnCommandActivityChanged, active => activity.Add(active)));

        TenantLifecycleCommandSnapshot tracked = TenantLifecycleCommandSnapshot
            .Idle(detail)
            .Previewed(
                new TenantLifecycleCommandRequest("tenant.alpha", TenantLifecycleOperation.DisableTenant),
                detail,
                "tenant-sequence:41")
            .RequestSent(
                new TenantLifecycleCommandRequest("tenant.alpha", TenantLifecycleOperation.DisableTenant),
                detail,
                "tenant-sequence:41",
                "message-life")
            .Accepted(TenantCommandSubmissionResult.Accepted("message-life", "correlation-life"));
        SetPrivateField(cut.Instance, "_snapshot", tracked);
        SetPrivateField(cut.Instance, "_hasRaisedCommandActivity", true);
        cut.Render(parameters => parameters
            .Add(component => component.Detail, detail)
            .Add(component => component.Availability, availability)
            .Add(component => component.IsCommandSurfaceAvailable, true)
            .Add(component => component.OnProjectionRefreshRequested, () => Task.CompletedTask)
            .Add(component => component.OnCommandActivityChanged, active => activity.Add(active)));

        cut.Find("[data-testid='tenants-lifecycle-refresh']").Click();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify));
        activity.ShouldBe([false]);
    }

    [Fact]
    public void Lifecycle_rejection_displays_safe_non_success_state_without_projection_success_or_raw_details()
    {
        var gateway = new StubTenantCommandGateway
        {
            Submission = TenantCommandSubmissionResult.Rejected("The tenant lifecycle already matches the requested state.", "TenantLifecycleStateAlreadySet"),
        };
        RegisterServices(gateway);

        IRenderedComponent<TenantLifecycleActionAvailability> cut = Render<TenantLifecycleActionAvailability>(parameters => parameters
            .Add(component => component.Lifecycle, ProjectionLifecycleState.Current)
            .Add(component => component.TenantId, "tenant.alpha")
            .Add(component => component.Detail, Detail("tenant.alpha", TenantStatus.Active))
            .Add(component => component.ProjectionVersion, "tenant-sequence:41")
            .Add(component => component.CurrentStatus, TenantStatus.Active)
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(component => component.Freshness, ReadModelFreshnessState.Current)
            .Add(component => component.IsCommandSurfaceConnected, true)
            .Add(component => component.IsCommandSurfaceAvailable, true)
            .Add(component => component.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Authorized)
            .Add(component => component.GovernanceReadiness, TenantLifecycleGovernanceReadiness.Ready)
            .Add(component => component.AuthorizationReflectionProvider, () => Task.FromResult(TenantLifecycleAuthorizationReflectionState.Authorized))
            .Add(component => component.ProjectionEvidenceProvider, request => Task.FromResult<TenantDetailSnapshot?>(
                Proof(request.TenantId, TenantStatus.Active, "tenant-sequence:41"))));

        cut.Find("[data-testid='tenants-lifecycle-disable']").Click();
        cut.Find("[data-testid='tenants-lifecycle-confirmation']").Change("tenant.alpha");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
            cut.FindComponent<TenantLifecycleCommandFlow>().Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Rejected));
        cut.FindComponent<TenantLifecycleCommandFlow>().Instance.Snapshot.AuditState.ShouldBe(TenantCommandAuditState.AuditUnavailable);
        cut.Find("[data-testid='tenants-lifecycle-command-flow'] [data-testid='tenants-lifecycle-state']").TextContent.ShouldContain("Rejected");
        cut.Find("[data-testid='tenants-lifecycle-command-flow'] [data-testid='tenants-lifecycle-safe-message']").TextContent.ShouldContain("already matches");
        cut.Find("[data-testid='tenants-lifecycle-command-flow'] [data-testid='tenants-lifecycle-confirmed-status']").TextContent.ShouldContain("Active");
        cut.Find("[data-testid='tenants-lifecycle-command-flow'] [data-testid='tenants-lifecycle-live-region']").GetAttribute("aria-live").ShouldBe("assertive");
        cut.Markup.ShouldNotContain("raw payload", Case.Insensitive);
        cut.Markup.ShouldNotContain("access_token", Case.Insensitive);
        cut.Markup.ShouldNotContain("bearer ", Case.Insensitive);
        cut.Markup.ShouldNotContain("correlation", Case.Insensitive);
        // Visible text only — avoids the Fluent success-color token false positive (see VisibleText).
        cut.VisibleText().ShouldNotContain("Success", Case.Insensitive);
    }

    [Fact]
    public void Shared_evidence_renders_associated_facts_and_unknown_viewport_creates_no_flow_or_dispatch()
    {
        StubTenantCommandGateway gateway = new();
        RegisterServices(gateway);
        TenantHighImpactActionEvidence enable = HighImpactEvidence(TenantHighImpactAction.EnableTenant);
        TenantHighImpactActionEvidence disable = HighImpactEvidence(TenantHighImpactAction.DisableTenant) with
        {
            Viewport = TenantHighImpactViewportState.Unknown,
        };

        IRenderedComponent<TenantLifecycleActionAvailability> cut = Render<TenantLifecycleActionAvailability>(parameters => parameters
            .Add(component => component.TenantId, "tenant.alpha")
            .Add(component => component.Detail, Detail("tenant.alpha", TenantStatus.Active))
            .Add(component => component.CurrentStatus, TenantStatus.Active)
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(component => component.Freshness, ReadModelFreshnessState.Current)
            .Add(component => component.Lifecycle, ProjectionLifecycleState.Current)
            .Add(component => component.EnableEvidence, enable)
            .Add(component => component.DisableEvidence, disable));

        cut.Find("[data-testid='tenants-lifecycle-enable-availability']");
        cut.Find("[data-testid='tenants-lifecycle-disable-availability']");
        cut.Find("[data-testid='tenants-lifecycle-disable-identity']").TextContent.ShouldContain("tenant.alpha");
        cut.Find("[data-testid='tenants-lifecycle-disable-status']").TextContent.ShouldContain("Active");
        cut.Find("[data-testid='tenants-lifecycle-disable-freshness']").TextContent.ShouldContain("Current");
        cut.Find("[data-testid='tenants-lifecycle-disable-action']").TextContent.ShouldContain("Disable");
        cut.Find("[data-testid='tenants-lifecycle-disable-reason']").TextContent.ShouldContain("viewport", Case.Insensitive);
        cut.Find("[data-testid='tenants-lifecycle-disable-recovery']").TextContent.ShouldNotBeNullOrWhiteSpace();
        cut.FindAll("[data-testid='tenants-lifecycle-command-flow']").ShouldBeEmpty();
        gateway.DisableSubmissions.ShouldBe(0);
    }

    [Fact]
    public void Eligible_typed_lifecycle_evidence_opens_the_complete_preview_before_confirmation_input()
    {
        RegisterServices(new StubTenantCommandGateway());
        TenantHighImpactActionEvidence enable = HighImpactEvidence(TenantHighImpactAction.EnableTenant);
        TenantHighImpactActionEvidence disable = HighImpactEvidence(TenantHighImpactAction.DisableTenant) with
        {
            IsInputComplete = false,
        };

        IRenderedComponent<TenantLifecycleActionAvailability> cut = Render<TenantLifecycleActionAvailability>(parameters => parameters
            .Add(component => component.TenantId, "tenant.alpha")
            .Add(component => component.Detail, Detail("tenant.alpha", TenantStatus.Active))
            .Add(component => component.ProjectionVersion, "tenant-sequence:41")
            .Add(component => component.EnableEvidence, enable)
            .Add(component => component.DisableEvidence, disable));

        cut.Find("[data-testid='tenants-lifecycle-disable']").Click();

        cut.Find("[data-testid='tenants-lifecycle-preview']");
        cut.FindAll("[data-testid='tenants-lifecycle-preview-item']").Count.ShouldBe(10);
        cut.Find("[data-testid='tenants-lifecycle-confirm']").GetAttribute("disabled").ShouldNotBeNull();
        cut.VisibleText().ShouldNotContain("tenant-sequence:41", Case.Sensitive);
    }

    [Theory]
    [InlineData(TenantHighImpactFreshnessState.Aging, "Aging")]
    [InlineData(TenantHighImpactFreshnessState.Refreshing, "Refreshing")]
    public void Aging_and_refreshing_typed_lifecycle_evidence_remain_eligible_with_visible_friction(
        TenantHighImpactFreshnessState freshness,
        string expectedText)
    {
        RegisterServices(new StubTenantCommandGateway());
        TenantHighImpactActionEvidence disable = HighImpactEvidence(TenantHighImpactAction.DisableTenant) with
        {
            Freshness = freshness,
            HasCurrentBaseline = true,
            IsInputComplete = false,
        };

        IRenderedComponent<TenantLifecycleActionAvailability> cut = Render<TenantLifecycleActionAvailability>(parameters => parameters
            .Add(component => component.TenantId, "tenant.alpha")
            .Add(component => component.Detail, Detail("tenant.alpha", TenantStatus.Active))
            .Add(component => component.ProjectionVersion, "tenant-sequence:41")
            .Add(component => component.EnableEvidence, HighImpactEvidence(TenantHighImpactAction.EnableTenant))
            .Add(component => component.DisableEvidence, disable));

        cut.Find("[data-testid='tenants-lifecycle-disable']").GetAttribute("disabled").ShouldBeNull();
        cut.Find("[data-testid='tenants-lifecycle-disable-freshness']").TextContent.ShouldContain(expectedText);
        cut.Find("[data-testid='tenants-lifecycle-disable-reason']").TextContent
            .ShouldContain("aging or refreshing", Case.Insensitive);
        cut.Find("[data-testid='tenants-lifecycle-disable']").Click();
        cut.Find("[data-testid='tenants-lifecycle-preview']");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Mismatched_tenant_or_action_evidence_fails_closed_without_rendering_foreign_facts(bool tenantMismatch)
    {
        RegisterServices(new StubTenantCommandGateway());
        TenantHighImpactActionEvidence supplied = HighImpactEvidence(
            tenantMismatch ? TenantHighImpactAction.DisableTenant : TenantHighImpactAction.EnableTenant) with
        {
            TenantId = tenantMismatch ? "tenant.beta" : "tenant.alpha",
        };

        IRenderedComponent<TenantLifecycleActionAvailability> cut = Render<TenantLifecycleActionAvailability>(parameters => parameters
            .Add(component => component.TenantId, "tenant.alpha")
            .Add(component => component.Detail, Detail("tenant.alpha", TenantStatus.Active))
            .Add(component => component.DisableEvidence, supplied));

        cut.Find("[data-testid='tenants-lifecycle-disable']").GetAttribute("disabled").ShouldNotBeNull();
        cut.Find("[data-testid='tenants-lifecycle-disable-identity']").TextContent.ShouldBe("tenant.alpha");
        cut.Markup.ShouldNotContain("tenant.beta", Case.Sensitive);
        cut.FindAll("[data-testid='tenants-lifecycle-command-flow']").ShouldBeEmpty();
    }

    [Fact]
    public void Undefined_typed_evidence_fails_closed_without_throwing_or_leaking_a_resource_key()
    {
        RegisterServices(new StubTenantCommandGateway());
        TenantHighImpactActionEvidence disable = HighImpactEvidence(TenantHighImpactAction.DisableTenant) with
        {
            Freshness = (TenantHighImpactFreshnessState)999,
        };

        IRenderedComponent<TenantLifecycleActionAvailability> cut = Render<TenantLifecycleActionAvailability>(parameters => parameters
            .Add(component => component.TenantId, "tenant.alpha")
            .Add(component => component.Detail, Detail("tenant.alpha", TenantStatus.Active))
            .Add(component => component.EnableEvidence, HighImpactEvidence(TenantHighImpactAction.EnableTenant))
            .Add(component => component.DisableEvidence, disable));

        cut.Find("[data-testid='tenants-lifecycle-disable']").GetAttribute("disabled").ShouldNotBeNull();
        cut.Find("[data-testid='tenants-lifecycle-disable-freshness']").TextContent.ShouldBe("Unknown");
        cut.Markup.ShouldNotContain("999", Case.Sensitive);
    }

    [Theory]
    [InlineData(true, TenantHighImpactUnavailableReason.MissingConsequencePreview, "consequence preview")]
    [InlineData(false, TenantHighImpactUnavailableReason.MissingAuditProof, "required proof")]
    public void Lifecycle_ui_preserves_canonical_preview_and_proof_reasons(
        bool missingPreview,
        TenantHighImpactUnavailableReason expectedReason,
        string expectedText)
    {
        RegisterServices(new StubTenantCommandGateway());
        TenantHighImpactActionEvidence disable = HighImpactEvidence(TenantHighImpactAction.DisableTenant) with
        {
            Preview = missingPreview
                ? TenantHighImpactPreviewEvidence.Missing
                : TenantHighImpactPreviewEvidence.Ready,
            Proof = missingPreview
                ? TenantHighImpactProofEvidence.NotRequired
                : (TenantHighImpactProofEvidence)999,
        };

        IRenderedComponent<TenantLifecycleActionAvailability> cut = Render<TenantLifecycleActionAvailability>(parameters => parameters
            .Add(component => component.TenantId, "tenant.alpha")
            .Add(component => component.Detail, Detail("tenant.alpha", TenantStatus.Active))
            .Add(component => component.EnableEvidence, HighImpactEvidence(TenantHighImpactAction.EnableTenant))
            .Add(component => component.DisableEvidence, disable));

        cut.Find("#tenants-lifecycle-disable-reason").GetAttribute("data-reason-category")
            .ShouldBe(expectedReason.ToString());
        cut.Find("[data-testid='tenants-lifecycle-disable-reason']").TextContent
            .ShouldContain(expectedText, Case.Insensitive);
    }

    [Theory]
    [InlineData(TenantLifecycleAuthorizationReflectionState.MissingPermission)]
    [InlineData((TenantLifecycleAuthorizationReflectionState)999)]
    public void Submit_time_lifecycle_authority_revocation_or_undefined_evidence_blocks_before_activity_or_gateway(
        TenantLifecycleAuthorizationReflectionState currentAuthorization)
    {
        StubTenantCommandGateway gateway = new();
        List<bool> activity = [];
        RegisterServices(gateway);

        IRenderedComponent<TenantLifecycleActionAvailability> cut = Render<TenantLifecycleActionAvailability>(parameters => parameters
            .Add(component => component.TenantId, "tenant.alpha")
            .Add(component => component.Detail, Detail("tenant.alpha", TenantStatus.Active))
            .Add(component => component.ProjectionVersion, "tenant-sequence:41")
            .Add(component => component.EnableEvidence, HighImpactEvidence(TenantHighImpactAction.EnableTenant))
            .Add(component => component.DisableEvidence, HighImpactEvidence(TenantHighImpactAction.DisableTenant))
            .Add(component => component.OnCommandActivityChanged, active => activity.Add(active))
            .Add(component => component.AuthorizationReflectionProvider, () => Task.FromResult(currentAuthorization))
            .Add(component => component.ProjectionEvidenceProvider, request => Task.FromResult<TenantDetailSnapshot?>(
                Proof(request.TenantId, TenantStatus.Active, "tenant-sequence:41"))));

        cut.Find("[data-testid='tenants-lifecycle-disable']").Click();
        cut.Find("[data-testid='tenants-lifecycle-confirmation']").Change("tenant.alpha");
        cut.Find("form").Submit();

        gateway.DisableSubmissions.ShouldBe(0);
        activity.ShouldBeEmpty();
        cut.FindComponent<TenantLifecycleCommandFlow>().Instance.Snapshot.State
            .ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        cut.Find("[data-testid='tenants-lifecycle-safe-message']").TextContent
            .ShouldContain("authority", Case.Insensitive);
    }

    [Theory]
    [InlineData("version")]
    [InlineData("tenant")]
    [InlineData("name")]
    [InlineData("description")]
    [InlineData("status")]
    [InlineData("freshness")]
    [InlineData("lifecycle")]
    [InlineData("surface")]
    public void Changed_authoritative_preview_fact_blocks_before_activity_or_gateway(string mismatch)
    {
        StubTenantCommandGateway gateway = new();
        List<bool> activity = [];
        RegisterServices(gateway);
        TenantDetail baseline = Detail("tenant.alpha", TenantStatus.Active);
        TenantDetailSnapshot proof = mismatch switch
        {
            "version" => Proof("tenant.alpha", TenantStatus.Active, "tenant-sequence:42"),
            "tenant" => Proof("Tenant.Alpha", TenantStatus.Active, "tenant-sequence:41"),
            "name" => TenantDetailSnapshot.Ready(
                baseline with { Name = "Renamed" },
                eTag: null,
                ReadModelFreshnessState.Current,
                ProjectionLifecycleState.Current,
                "tenant-sequence:41"),
            "description" => TenantDetailSnapshot.Ready(
                baseline with { Description = "Changed description" },
                eTag: null,
                ReadModelFreshnessState.Current,
                ProjectionLifecycleState.Current,
                "tenant-sequence:41"),
            "status" => Proof("tenant.alpha", TenantStatus.Disabled, "tenant-sequence:41"),
            "freshness" => Proof(
                "tenant.alpha",
                TenantStatus.Active,
                "tenant-sequence:41",
                ReadModelFreshnessState.Stale),
            "lifecycle" => Proof(
                "tenant.alpha",
                TenantStatus.Active,
                "tenant-sequence:41",
                lifecycle: ProjectionLifecycleState.Stale),
            "surface" => TenantDetailSnapshot.Stale(
                baseline,
                eTag: null,
                ProjectionLifecycleState.Current,
                "tenant-sequence:41"),
            _ => throw new ArgumentOutOfRangeException(nameof(mismatch)),
        };

        IRenderedComponent<TenantLifecycleActionAvailability> cut = Render<TenantLifecycleActionAvailability>(parameters => parameters
            .Add(component => component.TenantId, "tenant.alpha")
            .Add(component => component.Detail, baseline)
            .Add(component => component.ProjectionVersion, "tenant-sequence:41")
            .Add(component => component.EnableEvidence, HighImpactEvidence(TenantHighImpactAction.EnableTenant))
            .Add(component => component.DisableEvidence, HighImpactEvidence(TenantHighImpactAction.DisableTenant))
            .Add(component => component.OnCommandActivityChanged, active => activity.Add(active))
            .Add(component => component.AuthorizationReflectionProvider, () => Task.FromResult(TenantLifecycleAuthorizationReflectionState.Authorized))
            .Add(component => component.ProjectionEvidenceProvider, _ => Task.FromResult<TenantDetailSnapshot?>(proof)));

        cut.Find("[data-testid='tenants-lifecycle-disable']").Click();
        cut.Find("[data-testid='tenants-lifecycle-confirmation']").Change("tenant.alpha");
        cut.Find("form").Submit();

        gateway.DisableSubmissions.ShouldBe(0);
        activity.ShouldBeEmpty();
        cut.Find("[data-testid='tenants-lifecycle-safe-message']").TextContent
            .ShouldContain("changed", Case.Insensitive);
    }

    [Fact]
    public void Enable_flow_dispatches_only_enable_with_the_stable_message_id()
    {
        var gateway = new StubTenantCommandGateway
        {
            Submission = TenantCommandSubmissionResult.Failed("Safe terminal failure."),
        };
        RegisterServices(gateway);
        TenantHighImpactActionEvidence enable = HighImpactEvidence(TenantHighImpactAction.EnableTenant) with
        {
            TenantStatus = TenantStatus.Disabled,
        };
        TenantHighImpactActionEvidence disable = HighImpactEvidence(TenantHighImpactAction.DisableTenant) with
        {
            TenantStatus = TenantStatus.Disabled,
        };

        IRenderedComponent<TenantLifecycleActionAvailability> cut = Render<TenantLifecycleActionAvailability>(parameters => parameters
            .Add(component => component.TenantId, "tenant.alpha")
            .Add(component => component.Detail, Detail("tenant.alpha", TenantStatus.Disabled))
            .Add(component => component.ProjectionVersion, "tenant-sequence:41")
            .Add(component => component.EnableEvidence, enable)
            .Add(component => component.DisableEvidence, disable)
            .Add(component => component.AuthorizationReflectionProvider, () => Task.FromResult(TenantLifecycleAuthorizationReflectionState.Authorized))
            .Add(component => component.ProjectionEvidenceProvider, request => Task.FromResult<TenantDetailSnapshot?>(
                Proof(request.TenantId, TenantStatus.Disabled, "tenant-sequence:41"))));

        cut.Find("[data-testid='tenants-lifecycle-enable']").Click();
        cut.Find("[data-testid='tenants-lifecycle-confirmation']").Change("tenant.alpha");
        cut.Find("form").Submit();

        gateway.EnableSubmissions.ShouldBe(1);
        gateway.DisableSubmissions.ShouldBe(0);
        gateway.LastEnableRequest.ShouldNotBeNull().Operation.ShouldBe(TenantLifecycleOperation.EnableTenant);
        NUlid.Ulid.TryParse(gateway.LastEnableMessageId, out _).ShouldBeTrue();
    }

    [Theory]
    [InlineData("en", "Active", "Aging", "Disable tenant", "aging or refreshing", "No recovery")]
    [InlineData("fr", "Actif", "Vieillissante", "Désactiver le locataire", "vieillit ou est en cours", "Aucune récupération")]
    public void English_and_french_lifecycle_facts_keep_whole_associated_strings(
        string cultureName,
        string expectedStatus,
        string expectedFreshness,
        string expectedAction,
        string expectedReason,
        string expectedRecovery)
    {
        CultureInfo priorCulture = CultureInfo.CurrentCulture;
        CultureInfo priorUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo culture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            Services.AddLocalization();
            Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
            TenantHighImpactActionEvidence disable = HighImpactEvidence(TenantHighImpactAction.DisableTenant) with
            {
                Freshness = TenantHighImpactFreshnessState.Aging,
                IsInputComplete = false,
            };

            IRenderedComponent<TenantLifecycleActionAvailability> cut = Render<TenantLifecycleActionAvailability>(parameters => parameters
                .Add(component => component.TenantId, "tenant.alpha")
                .Add(component => component.Detail, Detail("tenant.alpha", TenantStatus.Active))
                .Add(component => component.EnableEvidence, HighImpactEvidence(TenantHighImpactAction.EnableTenant))
                .Add(component => component.DisableEvidence, disable));

            cut.Find("[data-testid='tenants-lifecycle-disable-identity']").TextContent.ShouldBe("tenant.alpha");
            cut.Find("[data-testid='tenants-lifecycle-disable-status']").TextContent.ShouldContain(expectedStatus);
            cut.Find("[data-testid='tenants-lifecycle-disable-freshness']").TextContent.ShouldContain(expectedFreshness);
            cut.Find("[data-testid='tenants-lifecycle-disable-action']").TextContent.ShouldContain(expectedAction);
            cut.Find("[data-testid='tenants-lifecycle-disable-reason']").TextContent.ShouldContain(expectedReason);
            cut.Find("[data-testid='tenants-lifecycle-disable-recovery']").TextContent.ShouldContain(expectedRecovery);
        }
        finally
        {
            CultureInfo.CurrentCulture = priorCulture;
            CultureInfo.CurrentUICulture = priorUiCulture;
        }
    }

    private static TenantHighImpactActionEvidence HighImpactEvidence(TenantHighImpactAction action)
        => new(
            "tenant.alpha",
            action,
            TenantHighImpactEvaluationStage.PreviewEntry,
            TenantStatus.Active,
            TenantHighImpactFreshnessState.Current,
            HasCurrentBaseline: true,
            TenantDetailSurfaceKind.Ready,
            ProjectionLifecycleState.Current,
            TenantHighImpactAuthorityEvidence.Authorized,
            TenantHighImpactNamespaceScopeEvidence.NotRequired,
            TenantHighImpactSupportEvidence.Ready,
            TenantHighImpactAdmissionEvidence.Available,
            TenantHighImpactPreviewEvidence.Ready,
            TenantHighImpactProofEvidence.NotRequired,
            TenantHighImpactViewportState.Safe,
            IsInputComplete: true,
            TenantHighImpactTargetState.NotApplicable);

    private void RegisterServices(ITenantCommandGateway? gateway = null)
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<TenantLifecycleAttemptTracker>();
        if (gateway is not null)
        {
            Services.AddSingleton(gateway);
        }
    }

    private IRenderedComponent<TenantLifecycleActionAvailability> RenderLifecycleAvailability(List<bool> activity)
        => Render<TenantLifecycleActionAvailability>(parameters => parameters
            .Add(component => component.Lifecycle, ProjectionLifecycleState.Current)
            .Add(component => component.TenantId, "tenant.alpha")
            .Add(component => component.Detail, Detail("tenant.alpha", TenantStatus.Active))
            .Add(component => component.ProjectionVersion, "tenant-sequence:41")
            .Add(component => component.CurrentStatus, TenantStatus.Active)
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(component => component.Freshness, ReadModelFreshnessState.Current)
            .Add(component => component.IsCommandSurfaceConnected, true)
            .Add(component => component.IsCommandSurfaceAvailable, true)
            .Add(component => component.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Authorized)
            .Add(component => component.GovernanceReadiness, TenantLifecycleGovernanceReadiness.Ready)
            .Add(component => component.OnCommandActivityChanged, active => activity.Add(active))
            .Add(component => component.OnProjectionRefreshRequested, () => Task.CompletedTask)
            .Add(component => component.AuthorizationReflectionProvider, () => Task.FromResult(TenantLifecycleAuthorizationReflectionState.Authorized))
            .Add(component => component.ProjectionEvidenceProvider, request => Task.FromResult<TenantDetailSnapshot?>(
                Proof(request.TenantId, TenantStatus.Active, "tenant-sequence:41"))));

    private static TenantDetail Detail(string tenantId, TenantStatus status)
        => new(
            tenantId,
            "Alpha",
            "Tenant alpha description",
            status,
            [],
            new Dictionary<string, string>(),
            DateTimeOffset.Parse("2026-06-01T12:00:00Z", CultureInfo.InvariantCulture));

    private static TenantDetailSnapshot Proof(
        string tenantId,
        TenantStatus status,
        string projectionVersion,
        ReadModelFreshnessState freshness = ReadModelFreshnessState.Current,
        ProjectionLifecycleState lifecycle = ProjectionLifecycleState.Current)
        => TenantDetailSnapshot.Ready(
            Detail(tenantId, status),
            eTag: null,
            freshness,
            lifecycle,
            projectionVersion);

    private static TenantLifecycleAvailability AvailableLifecycle(TenantStatus currentStatus, TenantLifecycleOperation operation)
        => new TenantLifecycleAvailabilityInput(
                "tenant.alpha",
                currentStatus,
                ReadModelFreshnessState.Current,
                TenantDetailSurfaceKind.Ready,
                IsCommandSurfaceConnected: true,
                TenantLifecycleGovernanceReadiness.Ready,
                TenantLifecycleAuthorizationReflectionState.Authorized,
                Lifecycle: ProjectionLifecycleState.Current)
            .Evaluate(operation);

    private static void SetPrivateField<TComponent, TValue>(
        TComponent component,
        string fieldName,
        TValue value)
        where TComponent : class
    {
        FieldInfo field = typeof(TComponent).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(TComponent).FullName, fieldName);
        field.SetValue(component, value);
    }

    private sealed class StubTenantCommandGateway : ITenantCommandGateway
    {
        public TenantCommandSubmissionResult Submission { get; init; }
            = TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable.");

        public TenantCommandStatusResult Status { get; set; }
            = TenantCommandStatusResult.Unknown("Tenant command status is unavailable.");

        public Exception? SubmissionException { get; init; }

        public Exception? StatusException { get; init; }

        public TenantLifecycleCommandRequest? LastDisableRequest { get; private set; }

        public string? LastDisableMessageId { get; private set; }

        public TenantLifecycleCommandRequest? LastEnableRequest { get; private set; }

        public string? LastEnableMessageId { get; private set; }

        public int EnableSubmissions { get; private set; }

        public int DisableSubmissions { get; private set; }

        public int StatusCalls { get; private set; }

        public Task<TenantCommandSubmissionResult> CreateTenantAsync(CreateTenant request, string? messageId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> AddUserToTenantAsync(AddUserToTenant request, string? messageId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> ChangeUserRoleAsync(ChangeUserRole request, string? messageId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> RemoveUserFromTenantAsync(RemoveUserFromTenant request, string? messageId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> UpdateTenantAsync(UpdateTenant request, string? messageId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> SetTenantConfigurationAsync(SetTenantConfiguration request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> EnableTenantAsync(
            TenantLifecycleCommandRequest request,
            string? messageId = null,
            CancellationToken cancellationToken = default)
        {
            LastEnableRequest = request;
            LastEnableMessageId = messageId;
            EnableSubmissions++;
            if (SubmissionException is not null)
            {
                return Task.FromException<TenantCommandSubmissionResult>(SubmissionException);
            }

            return Task.FromResult(Submission with { MessageId = messageId });
        }

        public Task<TenantCommandSubmissionResult> DisableTenantAsync(
            TenantLifecycleCommandRequest request,
            string? messageId = null,
            CancellationToken cancellationToken = default)
        {
            LastDisableRequest = request;
            LastDisableMessageId = messageId;
            DisableSubmissions++;
            if (SubmissionException is not null)
            {
                return Task.FromException<TenantCommandSubmissionResult>(SubmissionException);
            }

            return Task.FromResult(Submission with { MessageId = messageId });
        }

        public Task<TenantCommandStatusResult> GetStatusAsync(TenantCommandTrackingHandle handle, CancellationToken cancellationToken = default)
        {
            StatusCalls++;
            return StatusException is null
                ? Task.FromResult(Status.Status is null
                    ? Status
                    : Status with { HasVerifiedCommandIdentity = true })
                : Task.FromException<TenantCommandStatusResult>(StatusException);
        }
    }

    private sealed class StubTenantsLocalizer : IStringLocalizer<TenantsResources>
    {
        public LocalizedString this[string name] => new(name, Values.TryGetValue(name, out string? value) ? value : name);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, string.Format(CultureInfo.CurrentCulture, Values.TryGetValue(name, out string? value) ? value : name, arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => Values.Select(static value => new LocalizedString(value.Key, value.Value));

        private static readonly Dictionary<string, string> Values = new(StringComparer.Ordinal)
        {
            ["Tenants.Lifecycle.Description"] = "Read-only high-impact lifecycle availability for tenant {0}. Commands remain unavailable until authority, freshness, command support, and governance are proven.",
            ["Tenants.Lifecycle.FactsLabel"] = "Tenant lifecycle safety facts",
            ["Tenants.Lifecycle.Freshness.Current"] = "Current",
            ["Tenants.Lifecycle.Freshness.Stale"] = "Stale",
            ["Tenants.Lifecycle.Freshness.Unknown"] = "Unknown",
            ["Tenants.Lifecycle.FreshnessLabel"] = "Freshness",
            ["Tenants.Lifecycle.Governance.Ready"] = "Ready",
            ["Tenants.Lifecycle.Governance.Unresolved"] = "Unresolved",
            ["Tenants.Lifecycle.GovernanceGateLabel"] = "Governance gate",
            ["Tenants.Lifecycle.Operation.DisableTenant"] = "Disable tenant",
            ["Tenants.Lifecycle.Operation.EnableTenant"] = "Enable tenant",
            ["Tenants.Lifecycle.StateLabel"] = "Lifecycle state",
            ["Tenants.Lifecycle.CurrentStatusLabel"] = "Current status",
            ["Tenants.Lifecycle.FreshnessLabel"] = "Freshness",
            ["Tenants.Lifecycle.Freshness.Current"] = "Current",
            ["Tenants.Lifecycle.Freshness.Stale"] = "Stale",
            ["Tenants.Lifecycle.Freshness.Unknown"] = "Unknown",
            ["Tenants.Lifecycle.Freshness.Aging"] = "Aging",
            ["Tenants.Lifecycle.Status.Active"] = "Active",
            ["Tenants.Lifecycle.Status.Disabled"] = "Disabled",
            ["Tenants.Lifecycle.Status.Unknown"] = "Unknown",
            ["Tenants.Lifecycle.Title"] = "Lifecycle command availability",
            ["Tenants.ProjectionLifecycle.Label"] = "Projection lifecycle",
            ["Tenants.ProjectionLifecycle.Current"] = "Current",
            ["Tenants.ProjectionLifecycle.Stale"] = "Stale",
            ["Tenants.ProjectionLifecycle.Unknown"] = "Unknown",
            ["Tenants.ProjectionLifecycle.Rebuilding"] = "Rebuilding",
            ["Tenants.ProjectionLifecycle.Degraded"] = "Degraded",
            ["Tenants.ProjectionLifecycle.Unavailable"] = "Unavailable",
            ["Tenants.ProjectionLifecycle.LocalOnly"] = "Local only",
            ["Tenants.Lifecycle.Audit.AuditPending"] = "Audit evidence pending; no receipt is fabricated.",
            ["Tenants.Lifecycle.Audit.AuditUnavailable"] = "Audit evidence unavailable for this result.",
            ["Tenants.Lifecycle.Audit.MissingSupport"] = "Audit support is missing for this visible state.",
            ["Tenants.Lifecycle.Audit.NotStarted"] = "Audit evidence has not started.",
            ["Tenants.Audit.Availability.Accessible.Delayed"] = "Audit evidence is delayed; retry status lookup or inspect audit before citing proof.",
            ["Tenants.Audit.Availability.Accessible.MissingSupport"] = "Audit evidence support is missing; continue read-only or escalate with support-safe information.",
            ["Tenants.Audit.Availability.Accessible.MissingSupport.NoEscalation"] = "Audit evidence support is missing; continue read-only.",
            ["Tenants.Audit.Availability.Accessible.Pending"] = "Audit evidence is pending; wait, refresh status, or inspect audit before citing proof.",
            ["Tenants.Audit.Availability.Accessible.Unavailable"] = "Audit evidence is unavailable; continue read-only, retry status lookup, or escalate with support-safe information.",
            ["Tenants.Audit.Availability.Accessible.Unavailable.NoEscalation"] = "Audit evidence is unavailable; continue read-only or retry status lookup.",
            ["Tenants.Audit.Availability.Action.ContinueReadOnly"] = "Continue read-only",
            ["Tenants.Audit.Availability.Action.Escalate"] = "Escalate",
            ["Tenants.Audit.Availability.Action.InspectAudit"] = "Inspect audit",
            ["Tenants.Audit.Availability.Action.Refresh"] = "Retry status lookup",
            ["Tenants.Audit.Availability.Action.Wait"] = "Wait",
            ["Tenants.Audit.Availability.ActionsLabel"] = "Audit availability recovery actions",
            ["Tenants.Audit.Availability.Reason.MissingSupport"] = "This flow cannot verify audit proof from the available implementation support. Continue read-only or escalate using only the visible support-safe reference.",
            ["Tenants.Audit.Availability.Reason.MissingSupport.NoEscalation"] = "This flow cannot verify audit proof from the available implementation support. Continue read-only.",
            ["Tenants.Audit.Availability.Reason.Unavailable"] = "Audit proof cannot be verified right now. Continue read-only, retry status lookup, or escalate without including raw diagnostics, tokens, payloads, or personal data.",
            ["Tenants.Audit.Availability.Reason.Unavailable.NoEscalation"] = "Audit proof cannot be verified right now. Continue read-only or retry status lookup.",
            ["Tenants.Audit.Availability.State.Delayed"] = "Audit delayed",
            ["Tenants.Audit.Availability.State.MissingSupport"] = "Missing implementation support",
            ["Tenants.Audit.Availability.State.Pending"] = "Audit pending",
            ["Tenants.Audit.Availability.State.Unavailable"] = "Audit unavailable",
            ["Tenants.Audit.EntryPoint.Accessible.Command"] = "Open audit evidence for {0} in tenant {1}",
            ["Tenants.Audit.EntryPoint.CommandReason"] = "Command-specific proof is not available here; open the tenant audit list and use the visible audit state.",
            ["Tenants.Audit.EntryPoint.Label"] = "Audit evidence",
            ["Tenants.Audit.EntryPoint.Unavailable.ScopeRequired"] = "Tenant scope is required before audit evidence can be opened.",
            ["Tenants.Audit.EntryPoint.Unavailable.StaleScope"] = "Refresh tenant scope before opening audit evidence.",
            ["Tenants.Lifecycle.Cancel"] = "Cancel",
            ["Tenants.Lifecycle.Confirmation.Help"] = "Type {0} exactly. Cancel or Escape closes without submitting.",
            ["Tenants.Lifecycle.Confirmation.Label"] = "Type the tenant id to confirm",
            ["Tenants.Lifecycle.Confirm"] = "Confirm {0}",
            ["Tenants.Lifecycle.ConfirmedStatus"] = "Last confirmed lifecycle: {0}",
            ["Tenants.Lifecycle.Preview.AuditExpectation"] = "Audit expectation",
            ["Tenants.Lifecycle.Preview.AuditExpectation.Value"] = "Audit evidence is expected from the command/event pipeline and is shown as pending until explicit evidence is available.",
            ["Tenants.Lifecycle.Preview.AuthorizationGovernance"] = "Authorization and governance facts",
            ["Tenants.Lifecycle.Preview.AuthorizationGovernance.Value"] = "Server-reflected global-administrator authority and the approved reversible availability-control governance path are present.",
            ["Tenants.Lifecycle.Preview.CommandSurface"] = "Command-surface readiness",
            ["Tenants.Lifecycle.Preview.CommandSurface.Value"] = "The existing tenant command gateway is connected and one-at-a-time command submission is enforced.",
            ["Tenants.Lifecycle.Preview.CurrentLifecycle"] = "Current lifecycle",
            ["Tenants.Lifecycle.Preview.Description"] = "Review the full lifecycle consequence preview for tenant {0} before submitting.",
            ["Tenants.Lifecycle.Preview.IntendedLifecycle"] = "Intended lifecycle",
            ["Tenants.Lifecycle.Preview.KnownConsequences"] = "Known consequences",
            ["Tenants.Lifecycle.Preview.KnownConsequences.EnableTenant"] = "Tenant availability is restored only after exact-command event evidence and a newer tenant projection confirm Active.",
            ["Tenants.Lifecycle.Preview.KnownConsequences.DisableTenant"] = "Tenant availability is suspended as reversible availability control only after exact-command event evidence and a newer tenant projection confirm Disabled. Tenant data is not deleted or purged.",
            ["Tenants.Lifecycle.Preview.KnownUnknowns"] = "Known unknowns",
            ["Tenants.Lifecycle.Preview.KnownUnknowns.Value"] = "Downstream consumers may observe tenant availability asynchronously; audit proof may arrive after projection confirmation.",
            ["Tenants.Lifecycle.Preview.ListTitle"] = "Lifecycle consequence preview",
            ["Tenants.Lifecycle.Preview.ProjectionEvidence"] = "Freshness and projection evidence",
            ["Tenants.Lifecycle.Preview.ProjectionEvidence.Value"] = "The last confirmed tenant projection shows {0} with {1} freshness. Confirmation requires exact-command event evidence and a strictly newer internal projection marker from an authoritative re-query.",
            ["Tenants.Lifecycle.ProjectionVersion.Unknown"] = "unknown",
            ["Tenants.Lifecycle.Unavailable.ProofRead"] = "Fresh authoritative lifecycle proof could not be read. Refresh tenant detail before submitting.",
            ["Tenants.Lifecycle.Unavailable.PreviewChanged"] = "Tenant identity, lifecycle, freshness, or projection evidence changed while the preview was open. Refresh and review a new complete preview before submitting.",
            ["Tenants.Lifecycle.Unavailable.RetainedDifferentIntent"] = "A different lifecycle intent is already pending for this tenant. Reconcile that retained attempt before choosing another lifecycle action.",
            ["Tenants.Lifecycle.Retained.Resume"] = "This lifecycle attempt is still pending. Open it to resume authoritative reconciliation without submitting again.",
            ["Tenants.Lifecycle.Dismissal.Pending"] = "This lifecycle attempt is still pending and cannot be dismissed. Refresh status until it reaches a terminal, support-safe outcome.",
            ["Tenants.Lifecycle.UnableToVerify.Status"] = "The lifecycle command status could not be verified.",
            ["Tenants.Lifecycle.Status.Pending"] = "The lifecycle command is accepted, but status evidence is not available yet. Keep this attempt open and refresh.",
            ["Tenants.Lifecycle.UnableToVerify.MissingEventEvidence"] = "The lifecycle command completed without exact event evidence, so the requested outcome cannot be confirmed.",
            ["Tenants.Lifecycle.UnableToVerify.MissingBaseline"] = "The lifecycle outcome cannot be verified because its pre-submit projection baseline is unavailable.",
            ["Tenants.Lifecycle.UnableToVerify.TrackingMismatch"] = "The lifecycle gateway returned tracking evidence for a different logical attempt. No lifecycle success is asserted.",
            ["Tenants.Lifecycle.UnableToVerify.ProofRead"] = "Authoritative lifecycle proof could not be read. No lifecycle success is asserted.",
            ["Tenants.Lifecycle.Message.Rejected"] = "The lifecycle command was rejected. Refresh tenant detail before taking another action.",
            ["Tenants.Lifecycle.Message.Rejected.InsufficientPermissions"] = "You are not authorized to submit tenant lifecycle commands.",
            ["Tenants.Lifecycle.Message.Rejected.TenantDisabled"] = "This tenant is disabled, so this lifecycle command cannot be completed.",
            ["Tenants.Lifecycle.Message.Rejected.TenantNotFound"] = "The tenant was not found. Refresh tenant detail before taking another action.",
            ["Tenants.Lifecycle.Message.Rejected.TenantLifecycleStateAlreadySet"] = "The tenant lifecycle already matches the requested state. Refresh tenant detail before taking another action.",
            ["Tenants.Lifecycle.Message.Failed"] = "The lifecycle command failed before its outcome could be verified.",
            ["Tenants.Lifecycle.Message.Degraded"] = "The lifecycle command was accepted, but publication could not be verified.",
            ["Tenants.Lifecycle.Message.UnableToVerify"] = "The lifecycle command outcome could not be verified. Refresh before taking another action.",
            ["Tenants.Lifecycle.Preview.RecoveryPath"] = "Recovery path",
            ["Tenants.Lifecycle.Preview.RecoveryPath.Value"] = "Submit the opposite lifecycle command after refreshed projection evidence shows the current state.",
            ["Tenants.Lifecycle.Preview.TenantIdentity"] = "Tenant identity",
            ["Tenants.Lifecycle.Preview.Title"] = "{0} consequence preview",
            ["Tenants.Lifecycle.Recovery.Confirmed"] = "Use the refreshed tenant projection as the source of truth.",
            ["Tenants.Lifecycle.Recovery.Previewed"] = "Cancel to return without submitting, or submit only after confirmation is exact.",
            ["Tenants.Lifecycle.Refresh"] = "Refresh status",
            ["Tenants.Lifecycle.Result.Title"] = "Lifecycle command result",
            ["Tenants.Lifecycle.State.Confirmed"] = "Projection confirmed the lifecycle change.",
            ["Tenants.Lifecycle.State.Failed"] = "Lifecycle command failed before it could be verified.",
            ["Tenants.Lifecycle.State.Previewed"] = "Preview ready; no lifecycle command has been submitted.",
            ["Tenants.Lifecycle.State.UnableToVerify"] = "Lifecycle command outcome is unable to verify.",
            ["Tenants.Lifecycle.Unavailable.AlreadyActive"] = "{1} is unavailable for tenant {0} because the current projection already shows Active. If submitted by another surface, the safe domain outcome is {2}; continue read-only or refresh.",
            ["Tenants.Lifecycle.Unavailable.AlreadyDisabled"] = "{1} is unavailable for tenant {0} because the current projection already shows Disabled. If submitted by another surface, the safe domain outcome is {2}; continue read-only or refresh.",
            ["Tenants.Lifecycle.Unavailable.CommandSurface"] = "{1} is unavailable for tenant {0} because lifecycle command support is not connected. Continue read-only or escalate command-surface readiness.",
            ["Tenants.Lifecycle.Unavailable.Governance"] = "{1} is unavailable for tenant {0} because high-impact lifecycle governance is not ready. Continue read-only until the platform gate is cleared.",
            ["Tenants.Lifecycle.Unavailable.MissingPermission"] = "{1} is unavailable for tenant {0} because server-side global-administrator authority is not proven. Request permission or continue read-only.",
            ["Tenants.Lifecycle.Unavailable.Mobile"] = "{1} is unavailable for tenant {0} because this viewport cannot preserve the full high-impact safety context. Continue read-only on this view.",
            ["Tenants.Lifecycle.Unavailable.InFlightOrCommandSurface"] = "A tenant command is already in flight or the lifecycle command surface is not connected.",
            ["Tenants.Lifecycle.Unavailable.Identity"] = "Tenant identity is incomplete, so lifecycle submission is blocked.",
            ["Tenants.Lifecycle.Unavailable.PreviewIncomplete"] = "The consequence preview is incomplete, so lifecycle submission is blocked.",
            ["Tenants.Lifecycle.Unavailable.ProjectionLifecycle"] = "{1} is unavailable for tenant {0} because the projection lifecycle is not current. Continue read-only and refresh projection evidence.",
            ["Tenants.Lifecycle.Unavailable.SameState"] = "The current projection already shows {0}; lifecycle submission is blocked.",
            ["Tenants.Lifecycle.Unavailable.StaleFreshness"] = "{1} is unavailable for tenant {0} because tenant freshness is stale or unknown. Refresh before considering lifecycle action availability.",
            ["Tenants.Lifecycle.Unavailable.UnknownStatus"] = "{1} is unavailable for tenant {0} because the tenant lifecycle state is unknown. Continue read-only and refresh projection evidence.",
            ["Tenants.Lifecycle.UnavailableReason.HighImpactFlowNotReady"] = "high-impact flow not ready",
            ["Tenants.Lifecycle.UnavailableReason.MissingLifecycleSupport"] = "missing lifecycle support",
            ["Tenants.Lifecycle.UnavailableReason.MissingPermission"] = "missing permission",
            ["Tenants.Lifecycle.UnavailableReason.None"] = "available",
            ["Tenants.Lifecycle.UnavailableReason.StaleData"] = "stale data",
            ["Tenants.Lifecycle.Validation.ConfirmationRequired"] = "Type tenant id {0} exactly before submitting this lifecycle command.",
            ["Tenants.HighImpact.Action.EnableTenant"] = "Enable tenant",
            ["Tenants.HighImpact.Action.DisableTenant"] = "Disable tenant",
            ["Tenants.HighImpact.Available"] = "This action is available.",
            ["Tenants.HighImpact.AvailableWithFriction"] = "This action is available, but the projection is aging or refreshing. Review the last-confirmed facts carefully.",
            ["Tenants.HighImpact.Unavailable.MissingPermission"] = "This action is unavailable because the required authority or namespace scope is not reflected.",
            ["Tenants.HighImpact.Unavailable.StaleData"] = "This action is unavailable because current authoritative data is not available.",
            ["Tenants.HighImpact.Unavailable.MissingLifecycleSupport"] = "This action is unavailable because its command lifecycle support is not ready.",
            ["Tenants.HighImpact.Unavailable.MissingConsequencePreview"] = "This action is unavailable until its complete consequence preview is ready.",
            ["Tenants.HighImpact.Unavailable.MissingAuditProof"] = "This action is unavailable because its required proof is not ready.",
            ["Tenants.HighImpact.Unavailable.HighImpactFlowNotReady"] = "This high-impact action is read-only until the measured viewport and aggregate admission are safe.",
            ["Tenants.HighImpact.Recovery.None"] = "No recovery is required.",
            ["Tenants.HighImpact.Recovery.MissingPermission"] = "Ask an administrator to verify your role and the exact namespace grant.",
            ["Tenants.HighImpact.Recovery.StaleData"] = "Refresh the authoritative tenant data and review the last-confirmed facts.",
            ["Tenants.HighImpact.Recovery.MissingLifecycleSupport"] = "Restore the action's command lifecycle support, then retry.",
            ["Tenants.HighImpact.Recovery.MissingConsequencePreview"] = "Complete the safe preview facts and required confirmation inputs.",
            ["Tenants.HighImpact.Recovery.MissingAuditProof"] = "Restore the proof source declared by this action, then retry.",
            ["Tenants.HighImpact.Recovery.HighImpactFlowNotReady"] = "Use a wider measured viewport or wait for the tenant's current command to finish.",
            ["Tenants.HighImpact.DomainOutcome.LifecycleStateAlreadySet"] = "The tenant already has the requested lifecycle state.",
            ["Tenants.HighImpact.Freshness.Unknown"] = "Unknown",
            ["Tenants.HighImpact.Freshness.Current"] = "Current",
            ["Tenants.HighImpact.Freshness.Refreshing"] = "Refreshing with a current baseline",
            ["Tenants.HighImpact.Freshness.Aging"] = "Aging",
            ["Tenants.HighImpact.Freshness.Stale"] = "Stale",
        };
    }
}
