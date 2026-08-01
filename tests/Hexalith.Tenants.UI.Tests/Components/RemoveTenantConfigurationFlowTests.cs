using System.Globalization;
using System.Reflection;

using Bunit;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.UI.Components.Tenants.Configuration;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.EventStore.Client.Projections;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class RemoveTenantConfigurationFlowTests : FluentBunitContext
{
    [Fact]
    public void Remove_configuration_flow_blocks_a_target_absent_from_the_safe_rows_without_borrowing_a_value()
    {
        // The target sits inside the authorized `billing` namespace but has no safe row — how an
        // unapproved or undefined-policy key reaches this flow. It must block, and it must not display
        // a sibling row's value: this context is the component's only configuration input.
        RegisterServices(new StubTenantCommandGateway());

        IRenderedComponent<RemoveTenantConfigurationFlow> cut = Render<RemoveTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string>
            {
                ["billing.mode"] = "sibling-row-value",
            }))
            .Add(p => p.TargetKey, "billing.endpoint")
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-config-remove-flow']");
        cut.Find("[data-testid='tenants-config-remove-focus-start']");
        cut.Find("[data-testid='tenants-config-remove-focus-end']");
        cut.FindAll("[data-testid='tenants-config-remove-preview']").ShouldBeEmpty();
        cut.Find("[data-testid='tenants-config-remove-preview-blocked']").TextContent.ShouldContain("not visible", Case.Insensitive);
        cut.Find("[data-testid='tenants-config-remove-submit']").GetAttribute("disabled").ShouldNotBeNull();
        cut.Find("[data-testid='tenants-config-remove-live-region']").GetAttribute("aria-live").ShouldBe("polite");
        cut.Markup.ShouldNotContain("sibling-row-value", Case.Insensitive);
        cut.Markup.ShouldNotContain("audit available", Case.Insensitive);
        cut.Markup.ShouldNotContain("receipt", Case.Insensitive);
        cut.Markup.ShouldNotContain("success", Case.Insensitive);
    }

    [Fact]
    public void Remove_preview_reports_the_longest_authorized_prefix_not_the_first_dot_segment()
    {
        // The consumer namespace is the longest matching authorized prefix, exactly as the safe composer and
        // the set flow resolve it. Splitting on the first dot made this destructive-command preview claim
        // scope `app` for a caller granted only `app.feature`, contradicting both the read grid and the
        // sibling set preview over the same key -- and overstating the blast radius of a destructive action.
        //
        // The grant here is deliberately multi-segment: with a single-segment grant the two implementations
        // return the same string and the test proves nothing.
        RegisterServices(new StubTenantCommandGateway());

        TenantConfigurationManagementContext context = TenantConfigurationManagementContext.Available(
            "tenant.alpha",
            TenantStatus.Active,
            false,
            ["app.feature"],
            [new TenantConfigurationSafeRow("app", "app.feature.flag", "enabled")]);

        IRenderedComponent<RemoveTenantConfigurationFlow> cut = Render<RemoveTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, context)
            .Add(p => p.TargetKey, "app.feature.flag")
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        string rendered = cut.Find("[data-testid='tenants-config-remove-preview-namespace']").TextContent.Trim();
        rendered.ShouldBe("app.feature");
        rendered.ShouldNotBe("app");
    }

    [Fact]
    public void Remove_configuration_preview_preserves_a_legacy_safe_current_value()
    {
        RegisterServices(new StubTenantCommandGateway());
        IRenderedComponent<RemoveTenantConfigurationFlow> cut = Render<RemoveTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.TargetKey, "billing.mode")
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-config-remove-preview-current-state']").TextContent.ShouldContain("trial");
        cut.FindAll("[data-testid='tenants-config-copy-reference']").ShouldBeEmpty();
    }

    [Fact]
    public void Remove_configuration_preview_excludes_a_key_absent_from_the_safe_targets()
    {
        RegisterServices(new StubTenantCommandGateway());
        IRenderedComponent<RemoveTenantConfigurationFlow> cut = Render<RemoveTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.TargetKey, "billing.password")
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.FindAll("[data-testid='tenants-config-remove-preview-current-state']").ShouldBeEmpty();
        cut.Find("[data-testid='tenants-config-remove-preview-blocked']").TextContent.ShouldContain("not visible", Case.Insensitive);
        cut.Markup.ShouldNotContain("trial");
        cut.FindAll("[data-testid='tenants-config-copy-reference']").ShouldBeEmpty();
    }

    [Fact]
    public void Missing_or_hidden_target_blocks_submission_without_gateway_call_or_success_state()
    {
        StubTenantCommandGateway gateway = new();
        RegisterServices(gateway);

        IRenderedComponent<RemoveTenantConfigurationFlow> cut = Render<RemoveTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.TargetKey, "security.mode")
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.FindAll("[data-testid='tenants-config-remove-preview']").ShouldBeEmpty();
        cut.Find("[data-testid='tenants-config-remove-preview-blocked']").TextContent
            .ShouldContain("not visible", Case.Insensitive);
        cut.Find("form").Submit();

        gateway.RemoveConfigurationCallCount.ShouldBe(0);
        cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        cut.Markup.ShouldNotContain("success", Case.Insensitive);
    }

    [Theory]
    [InlineData(ProjectionLifecycleState.Unknown)]
    [InlineData(ProjectionLifecycleState.Stale)]
    [InlineData(ProjectionLifecycleState.Rebuilding)]
    [InlineData(ProjectionLifecycleState.Degraded)]
    [InlineData(ProjectionLifecycleState.Unavailable)]
    [InlineData(ProjectionLifecycleState.LocalOnly)]
    public void Remove_configuration_requires_current_projection_lifecycle(ProjectionLifecycleState lifecycle)
    {
        RegisterServices(new StubTenantCommandGateway());

        IRenderedComponent<RemoveTenantConfigurationFlow> cut = Render<RemoveTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.TargetKey, "billing.mode")
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.Lifecycle, lifecycle));

        cut.Find("[data-testid='tenants-config-remove-unavailable-reason']").TextContent
            .ShouldContain("projection-confirmed lifecycle", Case.Insensitive);
        cut.Find("[data-testid='tenants-config-remove-submit']").GetAttribute("disabled").ShouldNotBeNull();
    }

    /// <summary>
    /// Pins decision D-F's clause ORDER on this flow, which nothing did: every render in this file used
    /// <see cref="TenantDetailSurfaceKind.Ready"/>, so the surface clause never fired and hoisting the
    /// lifecycle clause back above it changed no outcome here. Every failed read also carries a non-Current
    /// lifecycle, so with the wrong order an operator whose read simply failed is told to refresh the
    /// projection lifecycle. The discriminator is the absence of the lifecycle reason.
    /// </summary>
    [Theory]
    [InlineData(TenantDetailSurfaceKind.Unavailable, ProjectionLifecycleState.Unavailable)]
    [InlineData(TenantDetailSurfaceKind.Unknown, ProjectionLifecycleState.Unknown)]
    [InlineData(TenantDetailSurfaceKind.Degraded, ProjectionLifecycleState.Degraded)]
    public void A_failed_read_reports_the_projection_state_rather_than_the_projection_lifecycle(
        TenantDetailSurfaceKind surfaceKind,
        ProjectionLifecycleState lifecycle)
    {
        RegisterServices(new StubTenantCommandGateway());

        IRenderedComponent<RemoveTenantConfigurationFlow> cut = Render<RemoveTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.TargetKey, "billing.mode")
            .Add(p => p.SurfaceKind, surfaceKind)
            .Add(p => p.Freshness, ReadModelFreshnessState.Unknown)
            .Add(p => p.Lifecycle, lifecycle));

        string reason = cut.Find("[data-testid='tenants-config-remove-unavailable-reason']").TextContent;
        reason.ShouldContain("unavailable or degraded", Case.Insensitive);
        reason.ShouldNotContain("projection-confirmed lifecycle", Case.Insensitive);
        cut.Find("[data-testid='tenants-config-remove-submit']").GetAttribute("disabled").ShouldNotBeNull();
    }

    /// <summary>
    /// Freshness half of the same clause-order contract: a stale or unknown-freshness read on an otherwise
    /// ready surface must report freshness, not the projection lifecycle, even though its lifecycle is also
    /// non-Current.
    /// </summary>
    [Theory]
    [InlineData(TenantDetailSurfaceKind.Stale, ReadModelFreshnessState.Stale, ProjectionLifecycleState.Stale)]
    [InlineData(TenantDetailSurfaceKind.Ready, ReadModelFreshnessState.Unknown, ProjectionLifecycleState.Unknown)]
    [InlineData(TenantDetailSurfaceKind.Ready, ReadModelFreshnessState.Stale, ProjectionLifecycleState.Rebuilding)]
    public void A_stale_or_unknown_read_reports_freshness_rather_than_the_projection_lifecycle(
        TenantDetailSurfaceKind surfaceKind,
        ReadModelFreshnessState freshness,
        ProjectionLifecycleState lifecycle)
    {
        RegisterServices(new StubTenantCommandGateway());

        IRenderedComponent<RemoveTenantConfigurationFlow> cut = Render<RemoveTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.TargetKey, "billing.mode")
            .Add(p => p.SurfaceKind, surfaceKind)
            .Add(p => p.Freshness, freshness)
            .Add(p => p.Lifecycle, lifecycle));

        string reason = cut.Find("[data-testid='tenants-config-remove-unavailable-reason']").TextContent;
        reason.ShouldContain("Refresh current tenant detail", Case.Insensitive);
        reason.ShouldNotContain("unavailable or degraded", Case.Insensitive);
        reason.ShouldNotContain("projection-confirmed lifecycle", Case.Insensitive);
        cut.Find("[data-testid='tenants-config-remove-submit']").GetAttribute("disabled").ShouldNotBeNull();
    }

    [Fact]
    public void Confirmation_text_must_match_literal_key_before_gateway_submission()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-1", "correlation-config-remove"),
        };
        RegisterServices(gateway);

        IRenderedComponent<RemoveTenantConfigurationFlow> cut = Render<RemoveTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.TargetKey, "billing.mode")
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-config-remove-submit']").GetAttribute("disabled").ShouldNotBeNull();
        cut.Find("[data-testid='tenants-config-remove-confirmation']").Change("Billing.Mode");
        cut.Find("form").Submit();

        gateway.RemoveConfigurationCallCount.ShouldBe(0);
        cut.Find("[data-testid='tenants-config-remove-validation']").TextContent.ShouldContain("billing.mode");
        cut.Find("[data-testid='tenants-config-remove-live-region']").GetAttribute("aria-live").ShouldBe("polite");

        cut.Find("[data-testid='tenants-config-remove-confirmation']").Change("billing.mode");
        cut.Find("[data-testid='tenants-config-remove-submit']").GetAttribute("disabled").ShouldBeNull();
    }

    [Fact]
    public void Keyboard_submission_confirms_only_after_status_and_projection_absence()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-1", "correlation-config-remove"),
            Status = new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1),
        };
        RegisterServices(gateway);

        IRenderedComponent<RemoveTenantConfigurationFlow> cut = Render<RemoveTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.TargetKey, "billing.mode")
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ReauthorizeProvider, () => Task.FromResult(Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" })))
            .Add(p => p.ProjectionEvidenceProvider, request => Task.FromResult(
                Proof(request.TenantId, TenantConfigurationProjectionProofKind.RemoveConfirmed))));

        cut.Find("[data-testid='tenants-config-remove-confirmation']").Change("billing.mode");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Confirmed));
        gateway.RemoveConfigurationCallCount.ShouldBe(1);
        gateway.LastRemoveConfigurationRequest.ShouldNotBeNull().ShouldBe(
            new RemoveTenantConfiguration("tenant.alpha", "billing.mode"));
        cut.Find("[data-testid='tenants-config-remove-state']").TextContent.ShouldContain("Projection confirmed");
        cut.Find("[data-testid='tenants-config-remove-live-region']").GetAttribute("aria-live").ShouldBe("polite");
    }

    [Fact]
    public void Projection_that_still_contains_key_does_not_optimistically_delete_or_confirm()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-1", "correlation-config-remove"),
            Status = new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1),
        };
        RegisterServices(gateway);
        List<bool> commandActivity = [];

        IRenderedComponent<RemoveTenantConfigurationFlow> cut = Render<RemoveTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.TargetKey, "billing.mode")
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ReauthorizeProvider, () => Task.FromResult(Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" })))
            .Add(p => p.OnCommandActivityChanged, isActive => commandActivity.Add(isActive))
            .Add(p => p.ProjectionEvidenceProvider, request => Task.FromResult(
                Proof(request.TenantId, TenantConfigurationProjectionProofKind.RemoveNotConfirmed))));

        cut.Find("[data-testid='tenants-config-remove-confirmation']").Change("billing.mode");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending));
        commandActivity.ShouldNotBeEmpty();
        commandActivity[^1].ShouldBeTrue();
        cut.Find("[data-testid='tenants-config-remove-state']").TextContent.ShouldContain("Projection pending");
        cut.Markup.ShouldContain("billing.mode");
    }

    [Fact]
    public void A_settling_removal_can_still_be_dismissed_and_releases_its_activity_lease()
    {
        // ProjectionPending is not terminal and is not self-healing: it is left only by a RemoveConfirmed
        // proof, which the gateway declines whenever authorization, provenance, freshness or lifecycle
        // evidence is missing. Blocking dismissal on IsOwnedCommandInFlight -- which covers Accepted and
        // ProjectionPending -- therefore left this role="dialog" aria-modal="true" section with no exit for
        // the whole projection-lag window, and permanently when the proof never landed: cancel disabled,
        // Escape swallowed by CloseAsync, and the focus sentinels bouncing focus back in.
        //
        // Dismissal is bounded now, and it must release the activity lease on the way out: the parent
        // landmark keeps both flows mounted while a child owns command activity, so a dismissal that
        // skipped that would leave the landmark and the detail page believing a command was still settling.
        List<bool> commandActivity = [];
        int closeRequests = 0;
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-1", "correlation-config-remove"),
            Status = new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1),
        };
        RegisterServices(gateway);

        IRenderedComponent<RemoveTenantConfigurationFlow> cut = Render<RemoveTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.TargetKey, "billing.mode")
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ReauthorizeProvider, () => Task.FromResult(Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" })))
            .Add(p => p.OnCommandActivityChanged, isActive => commandActivity.Add(isActive))
            .Add(p => p.OnCloseRequested, () => closeRequests++)
            .Add(p => p.ProjectionEvidenceProvider, request => Task.FromResult(
                Proof(request.TenantId, TenantConfigurationProjectionProofKind.RemoveNotConfirmed))));

        cut.Find("[data-testid='tenants-config-remove-confirmation']").Change("billing.mode");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending));
        commandActivity[^1].ShouldBeTrue();

        // The dialog is dismissable in exactly the state a real removal spends most of its life in.
        cut.Find("[data-testid='tenants-config-remove-cancel']").GetAttribute("disabled").ShouldBeNull();
        cut.Find("[data-testid='tenants-config-remove-cancel']").Click();

        cut.WaitForAssertion(() => closeRequests.ShouldBe(1));
        commandActivity[^1].ShouldBeFalse();
    }

    [Fact]
    public async Task A_status_refresh_that_completes_after_dismissal_cannot_reacquire_command_activity()
    {
        TaskCompletionSource<TenantCommandStatusResult> statusGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-1", "correlation-config-remove"),
            Status = new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1),
            StatusGate = statusGate,
            GateFromCall = 2,
        };
        RegisterServices(gateway);
        List<bool> commandActivity = [];
        int closeRequests = 0;

        IRenderedComponent<RemoveTenantConfigurationFlow> cut = Render<RemoveTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.TargetKey, "billing.mode")
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ReauthorizeProvider, () => Task.FromResult(Context(
                "tenant.alpha",
                new Dictionary<string, string> { ["billing.mode"] = "trial" })))
            .Add(p => p.OnCommandActivityChanged, isActive => commandActivity.Add(isActive))
            .Add(p => p.OnCloseRequested, () => closeRequests++)
            .Add(p => p.ProjectionEvidenceProvider, request => Task.FromResult(
                Proof(request.TenantId, TenantConfigurationProjectionProofKind.RemoveNotConfirmed))));

        cut.Find("[data-testid='tenants-config-remove-confirmation']").Change("billing.mode");
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending));
        cut.Find("[data-testid='tenants-config-remove-refresh']").Click();
        cut.WaitForAssertion(() => gateway.GetStatusCallCount.ShouldBe(2));

        cut.Find("[data-testid='tenants-config-remove-cancel']").Click();
        cut.WaitForAssertion(() => closeRequests.ShouldBe(1));
        commandActivity[^1].ShouldBeFalse();
        int activityCountAfterDismissal = commandActivity.Count;

        statusGate.SetResult(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1));

        // The gated read has now returned inside the gateway, so the flow's own continuation is scheduled.
        await gateway.GateObserved.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);

        // The correct behaviour is that nothing further happens, so there is no positive signal to wait for
        // and the absence is asserted over a bounded stability window instead. WaitForAssertion is wrong for
        // a negative: the state was already ProjectionPending before the gate released, so it returned on
        // its first evaluation and the activity assertion below ran before the continuation could have.
        await Task.Delay(TimeSpan.FromMilliseconds(250), Xunit.TestContext.Current.CancellationToken);

        cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        commandActivity.Count.ShouldBe(activityCountAfterDismissal);
        commandActivity[^1].ShouldBeFalse();
    }

    [Fact]
    public void Dismissal_never_turns_an_unconfirmed_removal_into_a_success_claim()
    {
        // The guard that created the trap existed to protect the non-collapse model. That protection has to
        // survive the fix: dismissal is allowed, but it must confirm nothing, advance no truth state and
        // render no success -- the operator has chosen to stop watching, which is not an outcome.
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-1", "correlation-config-remove"),
            Status = new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1),
        };
        RegisterServices(gateway);

        IRenderedComponent<RemoveTenantConfigurationFlow> cut = Render<RemoveTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.TargetKey, "billing.mode")
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ReauthorizeProvider, () => Task.FromResult(Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" })))
            .Add(p => p.ProjectionEvidenceProvider, request => Task.FromResult(
                Proof(request.TenantId, TenantConfigurationProjectionProofKind.RemoveNotConfirmed))));

        cut.Find("[data-testid='tenants-config-remove-confirmation']").Change("billing.mode");
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending));

        cut.Find("[data-testid='tenants-config-remove-cancel']").Click();

        cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
    }

    [Fact]
    public async Task Refresh_is_disabled_while_a_refresh_is_in_flight_rather_than_silently_dropping_the_click()
    {
        // The re-entrancy guard returns before the status read, before ConfirmProjection and before the
        // activity update. With the button still enabled, a second click was silently discarded and the
        // control was indistinguishable from a dead one -- so the guard belongs in CanRefresh, not only in
        // the method. Reverting CanRefresh to omit `!_isRefreshing` leaves the button enabled here and the
        // second click reaches the gateway, taking GetStatusCallCount to 2.
        // RunContinuationsAsynchronously, like every sibling gate in this file. Without it SetResult runs the
        // awaiting continuation synchronously on the xUnit thread, and that continuation invokes
        // EventCallbacks and StateHasChanged -- the documented trap where work resuming off the Blazor
        // Dispatcher tears down the circuit.
        TaskCompletionSource<TenantCommandStatusResult> gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-1", "correlation-config-remove"),
            Status = new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1),
            StatusGate = gate,

            // The submit path reads status itself; gating that would hang the submit instead of the refresh.
            GateFromCall = 2,
        };
        RegisterServices(gateway);

        IRenderedComponent<RemoveTenantConfigurationFlow> cut = Render<RemoveTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.TargetKey, "billing.mode")
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ReauthorizeProvider, () => Task.FromResult(Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))));

        cut.Find("[data-testid='tenants-config-remove-confirmation']").Change("billing.mode");
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-config-remove-refresh']").GetAttribute("disabled").ShouldBeNull());

        cut.Find("[data-testid='tenants-config-remove-refresh']").Click();

        // The status read has not returned, so the control must report itself as busy rather than accept a
        // second click it would silently drop.
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-config-remove-refresh']").GetAttribute("disabled").ShouldNotBeNull());
        gateway.GetStatusCallCount.ShouldBe(2);

        // The second click the test is named for. It was never dispatched, so the closing assertion was
        // satisfied by a click that did not exist: the re-entrancy guard could have been deleted entirely
        // and nothing here would have noticed.
        cut.Find("[data-testid='tenants-config-remove-refresh']").Click();
        gateway.GetStatusCallCount.ShouldBe(2);

        gate.SetResult(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1));
        await gateway.GateObserved.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);

        // The in-flight refresh has settled and re-enabled the control, which is a positive signal that the
        // resumed continuation ran -- so the count assertion below is now made after the window in which a
        // queued second read would have been issued.
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-config-remove-refresh']").GetAttribute("disabled").ShouldBeNull());
        gateway.GetStatusCallCount.ShouldBe(2);
    }

    [Fact]
    public void Projection_evidence_is_taken_before_the_parent_refresh_is_requested()
    {
        // Both halves of the ordering fix have to hold. The parent keeps the flow mounted when its row
        // leaves the context; the child must read its proof before asking the parent to refresh, or the
        // refresh drops the row first and the proof is taken against a context that no longer contains it.
        // Only the parent half was covered.
        List<string> order = [];
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-1", "correlation-config-remove"),
            Status = new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1),
        };
        RegisterServices(gateway);

        IRenderedComponent<RemoveTenantConfigurationFlow> cut = Render<RemoveTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.TargetKey, "billing.mode")
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ReauthorizeProvider, () => Task.FromResult(Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" })))
            .Add(p => p.OnProjectionRefreshRequested, () => order.Add("refresh"))
            .Add(p => p.ProjectionEvidenceProvider, request =>
            {
                order.Add("proof");
                return Task.FromResult(Proof(request.TenantId, TenantConfigurationProjectionProofKind.RemoveConfirmed));
            }));

        cut.Find("[data-testid='tenants-config-remove-confirmation']").Change("billing.mode");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => order.ShouldContain("proof"));
        order.ShouldContain("refresh");
        order.IndexOf("proof").ShouldBeLessThan(order.IndexOf("refresh"));
    }

    [Fact]
    public void Submission_time_policy_revocation_blocks_remove_before_gateway_dispatch()
    {
        StubTenantCommandGateway gateway = new();
        RegisterServices(gateway);

        IRenderedComponent<RemoveTenantConfigurationFlow> cut = Render<RemoveTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.TargetKey, "billing.mode")
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ReauthorizeProvider, () => Task.FromResult(Context(
                "tenant.alpha",
                new Dictionary<string, string> { ["security.mode"] = "enabled" }))));

        cut.Find("[data-testid='tenants-config-remove-confirmation']").Change("billing.mode");
        cut.Find("form").Submit();

        gateway.RemoveConfigurationCallCount.ShouldBe(0);
        cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        cut.Find("[data-testid='tenants-config-remove-state']").TextContent.ShouldContain("Unable to verify", Case.Insensitive);
    }

    [Fact]
    public void Submit_fails_closed_when_no_reauthorize_provider_is_wired()
    {
        // The Set flow has this coverage; the destructive Remove flow did not, over byte-identical code.
        // Every existing Remove submit test wires a provider, so the null branch had never executed. A
        // consumer that forgets the optional callback must resolve an Unavailable context and block --
        // never fall back to the render-time context, whose grant may since have been revoked.
        StubTenantCommandGateway gateway = new();
        RegisterServices(gateway);

        IRenderedComponent<RemoveTenantConfigurationFlow> cut = Render<RemoveTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.TargetKey, "billing.mode")
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-config-remove-confirmation']").Change("billing.mode");
        cut.Find("form").Submit();

        gateway.RemoveConfigurationCallCount.ShouldBe(0);
        cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
    }

    [Fact]
    public void Submit_fails_closed_when_reauthorization_throws()
    {
        // No Remove reauthorize provider in the suite threw, so the catch that maps failure to an
        // unavailable context -- rather than to the render-time context -- had never executed on the
        // destructive flow. The failure must also stay support-safe: no message, no exception type.
        StubTenantCommandGateway gateway = new();
        RegisterServices(gateway);

        IRenderedComponent<RemoveTenantConfigurationFlow> cut = Render<RemoveTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.TargetKey, "billing.mode")
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ReauthorizeProvider, () => Task.FromException<TenantConfigurationManagementContext>(
                new InvalidOperationException("policy backend unreachable"))));

        cut.Find("[data-testid='tenants-config-remove-confirmation']").Change("billing.mode");
        cut.Find("form").Submit();

        gateway.RemoveConfigurationCallCount.ShouldBe(0);
        cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        cut.Markup.ShouldNotContain("policy backend unreachable");
        cut.Markup.ShouldNotContain("InvalidOperationException");
    }

    [Fact]
    public void Gateway_unavailable_status_refresh_releases_configuration_command_activity_lock()
    {
        RegisterServices();
        List<bool> commandActivity = [];
        TenantConfigurationManagementContext context = Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" });

        IRenderedComponent<RemoveTenantConfigurationFlow> cut = Render<RemoveTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, context)
            .Add(p => p.TargetKey, "billing.mode")
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.OnCommandActivityChanged, active => commandActivity.Add(active)));

        TenantRemoveConfigurationCommandSnapshot tracked = TenantRemoveConfigurationCommandSnapshot
            .Idle()
            .Previewed(new RemoveTenantConfiguration("tenant.alpha", "billing.mode"))
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-config-remove"));
        SetPrivateField(cut.Instance, "_snapshot", tracked);
        SetPrivateField(cut.Instance, "_hasRaisedCommandActivity", true);
        cut.Render(parameters => parameters
            .Add(p => p.Context, context)
            .Add(p => p.TargetKey, "billing.mode")
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.OnCommandActivityChanged, active => commandActivity.Add(active)));

        cut.Find("[data-testid='tenants-config-remove-refresh']").Click();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify));
        commandActivity.ShouldBe([false]);
    }

    [Fact]
    public void Configuration_key_not_found_rejection_renders_safe_rejected_state()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-1", "correlation-config-remove"),
            Status = new TenantCommandStatusResult(
                CommandStatus.Rejected,
                "The configuration key was not found.",
                "ConfigurationKeyNotFound"),
        };
        RegisterServices(gateway);

        IRenderedComponent<RemoveTenantConfigurationFlow> cut = Render<RemoveTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.TargetKey, "billing.mode")
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ReauthorizeProvider, () => Task.FromResult(Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" })))
            .Add(p => p.ProjectionEvidenceProvider, request => Task.FromResult(
                Proof(request.TenantId, TenantConfigurationProjectionProofKind.RemoveConfirmed))));

        cut.Find("[data-testid='tenants-config-remove-confirmation']").Change("billing.mode");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Rejected));
        cut.Find("[data-testid='tenants-config-remove-state']").TextContent.ShouldContain("rejected", Case.Insensitive);
        cut.Find("[data-testid='tenants-config-remove-live-region']").GetAttribute("aria-live").ShouldBe("assertive");
        cut.Markup.ShouldNotContain("success", Case.Insensitive);
        cut.Markup.ShouldNotContain("raw payload", Case.Insensitive);
        cut.Markup.ShouldNotContain("correlation-config-remove", Case.Insensitive);
    }

    [Fact]
    public void Cancel_and_escape_close_without_committing_action()
    {
        StubTenantCommandGateway gateway = new();
        RegisterServices(gateway);
        int closeCount = 0;

        IRenderedComponent<RemoveTenantConfigurationFlow> cut = Render<RemoveTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.TargetKey, "billing.mode")
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.OnCloseRequested, () => closeCount++));

        cut.Find("[data-testid='tenants-config-remove-cancel']").Click();
        cut.Find("[data-testid='tenants-config-remove-flow']").KeyDown("Escape");

        closeCount.ShouldBe(2);
        gateway.RemoveConfigurationCallCount.ShouldBe(0);
    }

    [Fact]
    public void Css_contains_forced_colors_focus_and_narrow_layout_hooks()
    {
        string styles = File.ReadAllText(Path.Combine(
            ProjectRoot(),
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Tenants",
            "Configuration",
            "RemoveTenantConfigurationFlow.razor.css"));

        styles.ShouldContain("@media (forced-colors: active)");
        styles.ShouldContain(":focus-visible");
        styles.ShouldContain("tenants-config-remove__narrow");
    }

    private void RegisterServices(ITenantCommandGateway? gateway = null)
    {
        if (gateway is not null)
        {
            Services.AddSingleton(gateway);
        }

        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
    }

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

    private static TenantConfigurationManagementContext Context(
        string tenantId,
        IReadOnlyDictionary<string, string> configuration)
    {
        string[] authorizedPrefixes = configuration.Keys
            .Select(Namespace)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        // No filtering here. This helper previously dropped rows through a verbatim copy of the old
        // deny-list, which meant the "redaction" assertions below were satisfied by the fixture rather
        // than by the component under test. The context now contains exactly what a caller passes.
        TenantConfigurationSafeRow[] rows = configuration
            .Select(item => new TenantConfigurationSafeRow(Namespace(item.Key), item.Key, item.Value))
            .ToArray();
        return TenantConfigurationManagementContext.Available(
            tenantId,
            TenantStatus.Active,
            false,
            authorizedPrefixes,
            rows);
    }

    // First dot segment. This is the *display* grouping a safe row carries, and it is deliberately NOT the
    // authorized-namespace resolution the flow performs: keeping the two identical is what made
    // ResolveAuthorizedNamespace unprovable, because every fixture grant equalled the first dot segment and
    // both implementations agreed by construction. Tests that care about prefix resolution build their own
    // context with a multi-segment grant -- see
    // Remove_preview_reports_the_longest_authorized_prefix_not_the_first_dot_segment.
    private static string Namespace(string key)
    {
        int separator = key.IndexOf('.', StringComparison.Ordinal);
        return separator > 0 ? key[..separator] : key;
    }


    private static TenantConfigurationProjectionProof Proof(
        string tenantId,
        TenantConfigurationProjectionProofKind kind)
        => TenantConfigurationProjectionProof.Create(tenantId, kind);

    private static string ProjectRoot()
    {
        string directory = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(directory))
        {
            if (File.Exists(Path.Combine(directory, "Hexalith.Tenants.slnx")))
            {
                return directory;
            }

            directory = Directory.GetParent(directory)?.FullName ?? string.Empty;
        }

        throw new InvalidOperationException("Could not locate project root.");
    }

    private sealed class StubTenantCommandGateway : ITenantCommandGateway
    {
        public TenantCommandSubmissionResult Submission { get; init; }
            = TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable.");

        public TenantCommandStatusResult Status { get; init; }
            = TenantCommandStatusResult.Unknown("Command status is unavailable.");

        public RemoveTenantConfiguration? LastRemoveConfigurationRequest { get; private set; }

        public int RemoveConfigurationCallCount { get; private set; }

        public Task<TenantCommandSubmissionResult> CreateTenantAsync(CreateTenant request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> AddUserToTenantAsync(AddUserToTenant request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> ChangeUserRoleAsync(ChangeUserRole request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> RemoveUserFromTenantAsync(RemoveUserFromTenant request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> UpdateTenantAsync(UpdateTenant request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> SetTenantConfigurationAsync(SetTenantConfiguration request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> RemoveTenantConfigurationAsync(RemoveTenantConfiguration request, CancellationToken cancellationToken = default)
        {
            RemoveConfigurationCallCount++;
            LastRemoveConfigurationRequest = request;
            return Task.FromResult(Submission);
        }

        public int GetStatusCallCount { get; private set; }

        /// <summary>
        /// When set, status resolution blocks on this from <see cref="GateFromCall"/> onwards, so a refresh
        /// can be observed while it is genuinely in flight.
        /// </summary>
        public TaskCompletionSource<TenantCommandStatusResult>? StatusGate { get; init; }

        /// <summary>
        /// The 1-based call number from which <see cref="StatusGate"/> applies. Defaults to gating every
        /// call; set it to 2 to let the submit path's own status read resolve normally.
        /// </summary>
        public int GateFromCall { get; init; } = 1;

        /// <summary>
        /// Completes once a gated status read has actually returned inside this gateway, which happens
        /// strictly before the caller's own continuation resumes. Tests asserting that nothing follows a
        /// released gate need this: without it they asserted the absence at an instant when the resumed
        /// continuation had provably not run yet, so the assertion passed for the wrong reason.
        /// </summary>
        public TaskCompletionSource GateObserved { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<TenantCommandStatusResult> GetStatusAsync(TenantCommandTrackingHandle handle, CancellationToken cancellationToken = default)
        {
            GetStatusCallCount++;
            if (StatusGate is null || GetStatusCallCount < GateFromCall)
            {
                return Task.FromResult(Status);
            }

            return AwaitGateAsync(StatusGate);

            async Task<TenantCommandStatusResult> AwaitGateAsync(TaskCompletionSource<TenantCommandStatusResult> gate)
            {
                try
                {
                    return await gate.Task.ConfigureAwait(false);
                }
                finally
                {
                    _ = GateObserved.TrySetResult();
                }
            }
        }
    }

    private sealed class StubTenantsLocalizer : IStringLocalizer<TenantsResources>
    {
        private static readonly Dictionary<string, string> Values = new(StringComparer.Ordinal)
        {
            ["Tenants.Configuration.Remove.Title"] = "Remove configuration",
            ["Tenants.Configuration.Remove.Description"] = "Prepare a scoped configuration removal for tenant {0} with projection confirmation.",
            ["Tenants.Configuration.Remove.Submit"] = "Confirm removal",
            ["Tenants.Configuration.Remove.Refresh"] = "Refresh status",
            ["Tenants.Configuration.Remove.Cancel"] = "Cancel",
            ["Tenants.Configuration.Remove.Confirmation.Label"] = "Type the full configuration key to confirm removal",
            ["Tenants.Configuration.Remove.Confirmation.Help"] = "Type {0} exactly. Cancel or Escape closes without submitting.",
            ["Tenants.Configuration.Remove.Lifecycle.Title"] = "Configuration removal lifecycle",
            ["Tenants.Configuration.Remove.Unavailable.Authorization"] = "You are not authorized to remove configuration for this tenant.",
            ["Tenants.Configuration.Remove.Unavailable.ProjectionState"] = "Tenant detail is unavailable or degraded. Refresh current tenant detail before removing configuration.",
            ["Tenants.Configuration.Remove.Unavailable.Freshness"] = "Refresh current tenant detail before removing configuration.",
            ["Tenants.Configuration.Remove.Unavailable.ProjectionLifecycle"] = "Removing configuration requires a current, projection-confirmed lifecycle.",
            ["Tenants.Configuration.Remove.Unavailable.TenantLifecycle"] = "This tenant lifecycle state does not allow configuration removal.",
            ["Tenants.Configuration.Remove.Unavailable.CommandSurface"] = "Tenant command support is unavailable.",
            ["Tenants.Configuration.Remove.Unavailable.InFlight"] = "A tenant command is already in progress.",
            ["Tenants.Configuration.Remove.Unavailable.Identity"] = "Tenant identity is unavailable, so configuration removal fails closed.",
            ["Tenants.Configuration.Remove.Unavailable.Scope"] = "No authorized namespace prefix evidence is available from the current projection.",
            ["Tenants.Configuration.Remove.Unavailable.Target"] = "The selected configuration key is not visible in the current authorized projection. Refresh tenant detail before trying again.",
            ["Tenants.Configuration.Remove.Unavailable.Narrow"] = "Configuration removal is unavailable on narrow layouts because preview, tenant identity, freshness, target key, and confirmed configuration context must remain visible together.",
            ["Tenants.Configuration.Remove.Validation.KeyRequired"] = "Select a visible configuration key before previewing removal.",
            ["Tenants.Configuration.Remove.Validation.KeyVisible"] = "The selected key cannot be proven from the current authorized projection.",
            ["Tenants.Configuration.Remove.Validation.NamespaceScope"] = "The namespace prefix cannot be proven from the current authorized projection.",
            ["Tenants.Configuration.Remove.Validation.ConfirmationRequired"] = "Type {0} exactly before removing this configuration key.",
            ["Tenants.Configuration.Remove.Preview.Title"] = "Consequence preview",
            ["Tenants.Configuration.Remove.Preview.Blocked.Required"] = "Complete tenant identity, namespace, key, current state, freshness, authorization, and scope evidence before submitting.",
            ["Tenants.Configuration.Remove.Preview.Tenant"] = "Tenant",
            ["Tenants.Configuration.Remove.Preview.Namespace"] = "Namespace",
            ["Tenants.Configuration.Remove.Preview.Key"] = "Full key",
            ["Tenants.Configuration.Remove.Preview.CurrentState"] = "Current known state",
            ["Tenants.Configuration.Remove.Preview.IntendedEffect"] = "Intended effect",
            ["Tenants.Configuration.Remove.Preview.IntendedEffect.Value"] = "The selected configuration key will be removed only after command acceptance and projection proof.",
            ["Tenants.Configuration.Remove.Preview.Freshness"] = "Freshness evidence",
            ["Tenants.Configuration.Remove.Preview.Authorization"] = "Authorization and scope evidence",
            ["Tenants.Configuration.Remove.Preview.Authorization.Value"] = "The namespace prefix and key are visible in the authorized tenant projection; backend authorization still enforces the command.",
            ["Tenants.Configuration.Remove.Preview.KnownConsequences"] = "Known consequences",
            ["Tenants.Configuration.Remove.Preview.KnownConsequences.Value"] = "Consumers that own this prefix may lose the configured value after projection catches up.",
            ["Tenants.Configuration.Remove.Preview.KnownUnknowns"] = "Known unknowns",
            ["Tenants.Configuration.Remove.Preview.KnownUnknowns.Value"] = "This UI cannot prove downstream consumer impact or audit receipt availability.",
            ["Tenants.Configuration.Remove.Preview.AuditExpectation"] = "Audit expectation",
            ["Tenants.Configuration.Remove.Preview.AuditExpectation.Value"] = "Audit evidence is pending until the Epic 5 evidence source exists.",
            ["Tenants.Configuration.Remove.Preview.RecoveryPath"] = "Recovery path",
            ["Tenants.Configuration.Remove.Preview.RecoveryPath.Value"] = "Refresh tenant detail, retry only from current projection proof, or submit a forward correction to set the key again.",
            ["Tenants.Configuration.Remove.Freshness.Current"] = "Current",
            ["Tenants.Configuration.Remove.Freshness.Aging"] = "Aging",
            ["Tenants.Configuration.Remove.Freshness.Refreshing"] = "Refreshing",
            ["Tenants.Configuration.Remove.Freshness.Stale"] = "Stale",
            ["Tenants.Configuration.Remove.Freshness.Unknown"] = "Unknown",
            ["Tenants.Configuration.Remove.DuplicatePrevented.Message"] = "A configuration removal command is already in progress.",
            ["Tenants.Configuration.Remove.State.Idle"] = "No configuration removal command submitted.",
            ["Tenants.Configuration.Remove.State.Previewed"] = "Configuration removal preview ready.",
            ["Tenants.Configuration.Remove.State.RequestSent"] = "Configuration removal request sent.",
            ["Tenants.Configuration.Remove.State.Accepted"] = "Accepted by EventStore; waiting for configuration removal processing.",
            ["Tenants.Configuration.Remove.State.ProjectionPending"] = "Projection pending; the key is not confirmed removed yet.",
            ["Tenants.Configuration.Remove.State.Confirmed"] = "Projection confirmed the selected configuration key is removed.",
            ["Tenants.Configuration.Remove.State.Rejected"] = "Configuration removal command rejected.",
            ["Tenants.Configuration.Remove.State.AlreadyApplied"] = "Already applied.",
            ["Tenants.Configuration.Remove.State.DuplicatePrevented"] = "Duplicate configuration removal prevented.",
            ["Tenants.Configuration.Remove.State.Failed"] = "Configuration removal submission failed.",
            ["Tenants.Configuration.Remove.State.Degraded"] = "Configuration removal result is degraded and needs review.",
            ["Tenants.Configuration.Remove.State.UnableToVerify"] = "Unable to verify the configuration removal result.",
            ["Tenants.Configuration.Remove.Audit.NotStarted"] = "Audit evidence not started.",
            ["Tenants.Configuration.Remove.Audit.AuditPending"] = "Audit evidence pending.",
            ["Tenants.Configuration.Remove.Audit.AuditDelayed"] = "Audit evidence delayed.",
            ["Tenants.Configuration.Remove.Audit.AuditUnavailable"] = "Audit evidence unavailable.",
            ["Tenants.Configuration.Remove.Audit.MissingSupport"] = "Audit evidence support is missing until Epic 5 implements the evidence source.",
            ["Tenants.Audit.EntryPoint.Accessible.Command"] = "Open audit evidence for {0} in tenant {1}",
            ["Tenants.Audit.EntryPoint.CommandReason"] = "Command-specific proof is not available here; open the tenant audit list and use the visible audit state.",
            ["Tenants.Audit.EntryPoint.Label"] = "Audit evidence",
            ["Tenants.Audit.EntryPoint.Unavailable.ScopeRequired"] = "Tenant scope is required before audit evidence can be opened.",
            ["Tenants.Audit.EntryPoint.Unavailable.StaleScope"] = "Refresh tenant scope before opening audit evidence.",
            ["Tenants.Audit.Availability.Accessible.Delayed"] = "Audit evidence is delayed; retry status lookup or inspect audit before citing proof.",
            ["Tenants.Audit.Availability.Accessible.MissingSupport"] = "Audit evidence support is missing; continue read-only or escalate with support-safe information.",
            ["Tenants.Audit.Availability.Accessible.Pending"] = "Audit evidence is pending; wait, refresh status, or inspect audit before citing proof.",
            ["Tenants.Audit.Availability.Accessible.Unavailable"] = "Audit evidence is unavailable; continue read-only, retry status lookup, or escalate with support-safe information.",
            ["Tenants.Audit.Availability.Action.ContinueReadOnly"] = "Continue read-only",
            ["Tenants.Audit.Availability.Action.Escalate"] = "Escalate",
            ["Tenants.Audit.Availability.Action.InspectAudit"] = "Inspect audit",
            ["Tenants.Audit.Availability.Action.Refresh"] = "Retry status lookup",
            ["Tenants.Audit.Availability.Action.Wait"] = "Wait",
            ["Tenants.Audit.Availability.ActionsLabel"] = "Audit availability recovery actions",
            ["Tenants.Audit.Availability.Reason.MissingSupport"] = "This flow cannot verify audit proof from the available implementation support. Continue read-only or escalate using only the visible support-safe reference.",
            ["Tenants.Audit.Availability.Reason.Unavailable"] = "Audit proof cannot be verified right now. Continue read-only, retry status lookup, or escalate without including raw diagnostics, tokens, payloads, or personal data.",
            ["Tenants.Audit.Availability.State.Delayed"] = "Audit delayed",
            ["Tenants.Audit.Availability.State.MissingSupport"] = "Missing implementation support",
            ["Tenants.Audit.Availability.State.Pending"] = "Audit pending",
            ["Tenants.Audit.Availability.State.Unavailable"] = "Audit unavailable",
            ["Tenants.Configuration.Remove.Recovery.Idle"] = "Choose a visible key when current projection evidence and namespace scope are available.",
            ["Tenants.Configuration.Remove.Recovery.Previewed"] = "Confirm removal, cancel, or continue read-only.",
            ["Tenants.Configuration.Remove.Recovery.RequestSent"] = "Wait for command status and projection refresh.",
            ["Tenants.Configuration.Remove.Recovery.Accepted"] = "Wait, refresh status, or continue read-only until projection confirms removal.",
            ["Tenants.Configuration.Remove.Recovery.ProjectionPending"] = "Refresh tenant detail; do not display removal as complete until the key is absent from projection.",
            ["Tenants.Configuration.Remove.Recovery.Confirmed"] = "Continue read-only or inspect audit when evidence becomes available.",
            ["Tenants.Configuration.Remove.Recovery.Rejected"] = "Refresh projection evidence, request permission, start correction, or escalate.",
            ["Tenants.Configuration.Remove.Recovery.AlreadyApplied"] = "Refresh projection evidence before treating the missing key as already removed.",
            ["Tenants.Configuration.Remove.Recovery.DuplicatePrevented"] = "Wait for the in-flight command, retry status lookup, or continue read-only.",
            ["Tenants.Configuration.Remove.Recovery.Failed"] = "Retry after checking current projection evidence or escalate.",
            ["Tenants.Configuration.Remove.Recovery.Degraded"] = "Wait, retry status lookup, inspect audit when available, or escalate.",
            ["Tenants.Configuration.Remove.Recovery.UnableToVerify"] = "Refresh, retry status lookup, continue read-only, or escalate.",
        };

        public LocalizedString this[string name]
            => new(name, Values.TryGetValue(name, out string? value) ? value : name);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, string.Format(CultureInfo.CurrentCulture, Values.TryGetValue(name, out string? value) ? value : name, arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => Values.Select(v => new LocalizedString(v.Key, v.Value));
    }
}
