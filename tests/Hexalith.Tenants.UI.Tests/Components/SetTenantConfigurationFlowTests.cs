using System.Globalization;

using Bunit;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Tenants.Configuration;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.Tenants.UI.State.TruthState;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class SetTenantConfigurationFlowTests : FluentBunitContext
{
    [Fact]
    public void Set_configuration_flow_renders_complete_preview_with_stable_selectors_and_redacts_sensitive_current_value()
    {
        RegisterServices(new StubTenantCommandGateway());

        IRenderedComponent<SetTenantConfigurationFlow> cut = Render<SetTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha", new Dictionary<string, string>
            {
                ["billing.endpoint"] = "Bearer raw-token",
            }))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, TenantFreshnessState.Current));

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-namespace']").Change("billing");
        cut.Find("[data-testid='tenants-config-set-key']").Change("endpoint");
        cut.Find("[data-testid='tenants-config-set-value']").Change("new-safe-value");

        cut.Find("[data-testid='tenants-config-set-flow']");
        cut.Find("[data-testid='tenants-config-set-preview']");
        cut.FindAll("[data-testid='tenants-config-set-preview-item']").Count.ShouldBe(11);
        cut.Find("[data-testid='tenants-config-set-preview-namespace']").TextContent.ShouldBe("billing");
        cut.Find("[data-testid='tenants-config-set-preview-key']").TextContent.ShouldContain("billing.endpoint");
        cut.Find("[data-testid='tenants-config-set-preview-current-state']").TextContent.ShouldContain("Unavailable");
        cut.Find("[data-testid='tenants-config-set-submit']").GetAttribute("disabled").ShouldBeNull();
        string namespaceDescription = cut.Find("[data-testid='tenants-config-set-namespace']")
            .GetAttribute("aria-describedby")
            .ShouldNotBeNull();
        namespaceDescription.ShouldNotContain("tenants-config-set-preview-blocked", Case.Insensitive);
        cut.Find("[data-testid='tenants-config-set-live-region']").GetAttribute("aria-live").ShouldBe("polite");
        cut.Find("[data-testid='tenants-config-set-preview']").TextContent.ShouldNotContain("raw-token", Case.Insensitive);
        cut.Find("[data-testid='tenants-config-set-live-region']").TextContent.ShouldNotContain("new-safe-value", Case.Insensitive);
        cut.Markup.ShouldNotContain("audit available", Case.Insensitive);
        cut.Markup.ShouldNotContain("receipt", Case.Insensitive);
        cut.Markup.ShouldNotContain("success", Case.Insensitive);
    }

    [Fact]
    public void Namespace_scope_must_be_proven_from_authorized_projection_before_gateway_submission()
    {
        StubTenantCommandGateway gateway = new();
        RegisterServices(gateway);

        IRenderedComponent<SetTenantConfigurationFlow> cut = Render<SetTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, TenantFreshnessState.Current));

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-namespace']").Change("security");
        cut.Find("[data-testid='tenants-config-set-key']").Change("mode");
        cut.Find("[data-testid='tenants-config-set-value']").Change("enabled");
        cut.Find("form").Submit();

        gateway.SetConfigurationCallCount.ShouldBe(0);
        cut.Find("[data-testid='tenants-config-set-validation']").TextContent.ShouldContain("prefix cannot be proven", Case.Insensitive);
        cut.FindAll("[data-testid='tenants-config-set-preview']").ShouldBeEmpty();
    }

    [Fact]
    public void Editing_valid_preview_back_to_invalid_clears_stale_preview_lifecycle_state()
    {
        RegisterServices(new StubTenantCommandGateway());

        IRenderedComponent<SetTenantConfigurationFlow> cut = Render<SetTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, TenantFreshnessState.Current));

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-namespace']").Change("billing");
        cut.Find("[data-testid='tenants-config-set-key']").Change("mode");
        cut.Find("[data-testid='tenants-config-set-value']").Change("enterprise");

        cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Previewed);
        cut.Find("[data-testid='tenants-config-set-preview']");

        cut.Find("[data-testid='tenants-config-set-key']").Change("");

        cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Idle);
        cut.FindAll("[data-testid='tenants-config-set-preview']").ShouldBeEmpty();
        cut.Find("[data-testid='tenants-config-set-submit']").GetAttribute("disabled").ShouldNotBeNull();
    }

    [Theory]
    [InlineData("", "mode", "enabled", "namespace")]
    [InlineData("billing", "", "enabled", "key")]
    [InlineData("billing", "mode", "", "value")]
    public void Required_namespace_key_and_value_block_submission_with_field_safe_messages(
        string namespaceValue,
        string key,
        string value,
        string expectedText)
    {
        StubTenantCommandGateway gateway = new();
        RegisterServices(gateway);

        IRenderedComponent<SetTenantConfigurationFlow> cut = Render<SetTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, TenantFreshnessState.Current));

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-namespace']").Change(namespaceValue);
        cut.Find("[data-testid='tenants-config-set-key']").Change(key);
        cut.Find("[data-testid='tenants-config-set-value']").Change(value);
        cut.Find("form").Submit();

        gateway.SetConfigurationCallCount.ShouldBe(0);
        cut.Find("[data-testid='tenants-config-set-validation']").TextContent.ShouldContain(expectedText, Case.Insensitive);
        cut.Markup.ShouldNotContain("payload", Case.Insensitive);
        cut.Markup.ShouldNotContain("token", Case.Insensitive);
        cut.Markup.ShouldNotContain("correlation", Case.Insensitive);
    }

    [Fact]
    public void Domain_key_and_value_limits_block_submission_before_gateway_call()
    {
        StubTenantCommandGateway gateway = new();
        RegisterServices(gateway);

        IRenderedComponent<SetTenantConfigurationFlow> cut = Render<SetTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, TenantFreshnessState.Current));

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-namespace']").Change("billing");
        cut.Find("[data-testid='tenants-config-set-key']").Change(new string('k', 260));
        cut.Find("[data-testid='tenants-config-set-value']").Change("enabled");
        cut.Find("form").Submit();

        gateway.SetConfigurationCallCount.ShouldBe(0);
        cut.Find("[data-testid='tenants-config-set-validation']").TextContent.ShouldContain("256");

        cut.Find("[data-testid='tenants-config-set-key']").Change("mode");
        cut.Find("[data-testid='tenants-config-set-value']").Change(new string('v', 1025));
        cut.Find("form").Submit();

        gateway.SetConfigurationCallCount.ShouldBe(0);
        cut.Find("[data-testid='tenants-config-set-validation']").TextContent.ShouldContain("1024");
    }

    [Fact]
    public void Identical_value_is_already_applied_before_submit_without_gateway_or_success_state()
    {
        StubTenantCommandGateway gateway = new();
        RegisterServices(gateway);

        IRenderedComponent<SetTenantConfigurationFlow> cut = Render<SetTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, TenantFreshnessState.Current));

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-namespace']").Change("billing");
        cut.Find("[data-testid='tenants-config-set-key']").Change("mode");
        cut.Find("[data-testid='tenants-config-set-value']").Change("trial");
        cut.Find("form").Submit();

        gateway.SetConfigurationCallCount.ShouldBe(0);
        cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.AlreadyApplied);
        cut.Find("[data-testid='tenants-config-set-state']").TextContent.ShouldContain("Already applied");
        cut.Markup.ShouldNotContain("success", Case.Insensitive);
    }

    [Fact]
    public void Keyboard_form_submission_confirms_only_after_status_and_projection_evidence()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-1", "correlation-config"),
            Status = new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1),
        };
        RegisterServices(gateway);

        IRenderedComponent<SetTenantConfigurationFlow> cut = Render<SetTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, TenantFreshnessState.Current)
            .Add(p => p.ProjectionEvidenceProvider, request => Task.FromResult<TenantDetail?>(
                Detail(request.TenantId, new Dictionary<string, string> { [request.Key] = request.Value }))));

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-namespace']").Change("billing");
        cut.Find("[data-testid='tenants-config-set-key']").Change("mode");
        cut.Find("[data-testid='tenants-config-set-value']").Change("enterprise");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Confirmed));
        gateway.SetConfigurationCallCount.ShouldBe(1);
        gateway.LastSetConfigurationRequest.ShouldNotBeNull().ShouldBe(
            new SetTenantConfigurationCommandRequest("tenant.alpha", "billing.mode", "enterprise"));
        cut.Find("[data-testid='tenants-config-set-state']").TextContent.ShouldContain("Projection confirmed");
        cut.Find("[data-testid='tenants-config-set-live-region']").GetAttribute("aria-live").ShouldBe("polite");
    }

    [Fact]
    public void Accepted_command_confirms_only_after_matching_projection_requery_and_keeps_states_distinct()
    {
        int projectionCalls = 0;
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-1", "correlation-config"),
            StatusAsync = _ => Task.FromResult(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1)),
        };
        RegisterServices(gateway);

        IRenderedComponent<SetTenantConfigurationFlow> cut = Render<SetTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, TenantFreshnessState.Current)
            .Add(p => p.ProjectionEvidenceProvider, request => Task.FromResult<TenantDetail?>(++projectionCalls == 1
                ? Detail(request.TenantId, new Dictionary<string, string> { ["billing.mode"] = "trial" })
                : Detail(request.TenantId, new Dictionary<string, string> { [request.Key] = request.Value }))));

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-namespace']").Change("billing");
        cut.Find("[data-testid='tenants-config-set-key']").Change("mode");
        cut.Find("[data-testid='tenants-config-set-value']").Change("enterprise");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending));
        gateway.LastSetConfigurationRequest.ShouldNotBeNull().TenantId.ShouldBe("tenant.alpha");
        gateway.LastSetConfigurationRequest.ShouldNotBeNull().Key.ShouldBe("billing.mode");
        gateway.LastSetConfigurationRequest.ShouldNotBeNull().Value.ShouldBe("enterprise");
        cut.Find("[data-testid='tenants-config-set-state']").TextContent.ShouldContain("Projection pending");
        cut.Markup.ShouldNotContain("correlation-config", Case.Insensitive);
        cut.Find("[data-testid='tenants-config-set-state']").TextContent.ShouldNotContain("success", Case.Insensitive);

        cut.Find("[data-testid='tenants-config-set-refresh']").Click();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Confirmed));
        cut.Find("[data-testid='tenants-config-set-state']").TextContent.ShouldContain("Projection confirmed");
        cut.Find("[data-testid='tenants-config-set-audit']").TextContent.ShouldContain("Audit evidence pending");
    }

    [Fact]
    public void Completed_without_events_becomes_already_applied_only_after_projection_matches()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-1", "correlation-config"),
            Status = new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 0),
        };
        RegisterServices(gateway);

        IRenderedComponent<SetTenantConfigurationFlow> cut = Render<SetTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, TenantFreshnessState.Current)
            .Add(p => p.ProjectionEvidenceProvider, request => Task.FromResult<TenantDetail?>(
                Detail(request.TenantId, new Dictionary<string, string> { [request.Key] = request.Value }))));

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-namespace']").Change("billing");
        cut.Find("[data-testid='tenants-config-set-key']").Change("mode");
        cut.Find("[data-testid='tenants-config-set-value']").Change("enterprise");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.AlreadyApplied));
        cut.Find("[data-testid='tenants-config-set-state']").TextContent.ShouldContain("Already applied");
        cut.Find("[data-testid='tenants-config-set-safe-message']").TextContent.ShouldContain("already applied", Case.Insensitive);
        cut.Find("[data-testid='tenants-config-set-state']").TextContent.ShouldNotContain("success", Case.Insensitive);
    }

    [Theory]
    [InlineData(CommandStatus.Rejected, TenantCommandLifecycleState.Rejected, "rejected", "assertive")]
    [InlineData(CommandStatus.PublishFailed, TenantCommandLifecycleState.Degraded, "degraded", "assertive")]
    [InlineData(CommandStatus.TimedOut, TenantCommandLifecycleState.UnableToVerify, "Unable to verify", "assertive")]
    public void Terminal_statuses_remain_distinct_assertive_and_support_safe(
        CommandStatus status,
        TenantCommandLifecycleState expectedState,
        string expectedText,
        string expectedLiveRegion)
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-1", "correlation-config"),
            Status = new TenantCommandStatusResult(status, "Safe status message.", "ConfigurationLimitExceeded"),
        };
        RegisterServices(gateway);

        IRenderedComponent<SetTenantConfigurationFlow> cut = Render<SetTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, TenantFreshnessState.Current)
            .Add(p => p.ProjectionEvidenceProvider, request => Task.FromResult<TenantDetail?>(
                Detail(request.TenantId, new Dictionary<string, string> { [request.Key] = request.Value }))));

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-namespace']").Change("billing");
        cut.Find("[data-testid='tenants-config-set-key']").Change("mode");
        cut.Find("[data-testid='tenants-config-set-value']").Change("enterprise");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(expectedState));
        cut.Find("[data-testid='tenants-config-set-state']").TextContent.ShouldContain(expectedText, Case.Insensitive);
        cut.Find("[data-testid='tenants-config-set-live-region']").GetAttribute("aria-live").ShouldBe(expectedLiveRegion);
        cut.Markup.ShouldNotContain("correlation-config", Case.Insensitive);
        cut.Markup.ShouldNotContain("payload", Case.Insensitive);
        cut.Markup.ShouldNotContain("token", Case.Insensitive);
        cut.Instance.Snapshot.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
    }

    [Theory]
    [InlineData(TenantDetailSurfaceKind.Unauthorized, TenantFreshnessState.Current, TenantStatus.Active, "not authorized")]
    [InlineData(TenantDetailSurfaceKind.Degraded, TenantFreshnessState.Current, TenantStatus.Active, "degraded")]
    [InlineData(TenantDetailSurfaceKind.Stale, TenantFreshnessState.Stale, TenantStatus.Active, "Refresh current")]
    [InlineData(TenantDetailSurfaceKind.Ready, TenantFreshnessState.Current, TenantStatus.Disabled, "lifecycle state")]
    [InlineData(TenantDetailSurfaceKind.Ready, TenantFreshnessState.Unknown, TenantStatus.Active, "Refresh current")]
    public void Set_configuration_fails_closed_for_stale_unknown_or_disabled_projection(
        TenantDetailSurfaceKind surfaceKind,
        TenantFreshnessState freshness,
        TenantStatus status,
        string expectedReason)
    {
        RegisterServices(new StubTenantCommandGateway());

        IRenderedComponent<SetTenantConfigurationFlow> cut = Render<SetTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }) with { Status = status })
            .Add(p => p.SurfaceKind, surfaceKind)
            .Add(p => p.Freshness, freshness));

        cut.Find("[data-testid='tenants-config-set-unavailable-reason']").TextContent.ShouldContain(expectedReason, Case.Insensitive);
        cut.FindAll("[data-testid='tenants-config-set-open']").ShouldBeEmpty();
    }

    [Fact]
    public void Missing_authorization_or_scope_evidence_fails_closed_before_editor_opens()
    {
        StubTenantCommandGateway gateway = new();
        RegisterServices(gateway);

        IRenderedComponent<SetTenantConfigurationFlow> unauthorized = Render<SetTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, TenantFreshnessState.Current)
            .Add(p => p.IsAuthorized, false));

        unauthorized.Find("[data-testid='tenants-config-set-unavailable-reason']").TextContent
            .ShouldContain("not authorized", Case.Insensitive);
        unauthorized.FindAll("[data-testid='tenants-config-set-open']").ShouldBeEmpty();

        IRenderedComponent<SetTenantConfigurationFlow> missingScope = Render<SetTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha", new Dictionary<string, string>()))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, TenantFreshnessState.Current));

        missingScope.Find("[data-testid='tenants-config-set-unavailable-reason']").TextContent
            .ShouldContain("namespace prefix evidence", Case.Insensitive);
        missingScope.FindAll("[data-testid='tenants-config-set-open']").ShouldBeEmpty();
        gateway.SetConfigurationCallCount.ShouldBe(0);
    }

    [Fact]
    public void Cancel_and_escape_close_editor_without_submitting_and_request_focus_return()
    {
        int closeCount = 0;
        StubTenantCommandGateway gateway = new();
        RegisterServices(gateway);

        IRenderedComponent<SetTenantConfigurationFlow> cut = Render<SetTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, TenantFreshnessState.Current)
            .Add(p => p.OnCloseRequested, () => closeCount++));

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-cancel']").Click();
        cut.FindAll("[data-testid='tenants-config-set-namespace']").ShouldBeEmpty();

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-flow']").KeyDown("Escape");
        cut.FindAll("[data-testid='tenants-config-set-namespace']").ShouldBeEmpty();

        closeCount.ShouldBe(2);
        gateway.SetConfigurationCallCount.ShouldBe(0);
    }

    [Fact]
    public void Configuration_set_styles_preserve_forced_colors_focus_and_mobile_blocking_hooks()
    {
        string styles = File.ReadAllText(Path.Combine(
            ProjectRoot(),
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Tenants",
            "Configuration",
            "SetTenantConfigurationFlow.razor.css"));

        styles.ShouldContain("@media (forced-colors: active)");
        styles.ShouldContain("@media (max-width: 767px)");
        styles.ShouldContain(":focus-visible");
        styles.ShouldContain("tenants-config-set__narrow");
        styles.ShouldContain("tenants-config-set__state");
        styles.ShouldNotContain("tenants-config-set__state--alreadyapplied");
    }

    [Fact]
    public void Command_activity_callback_wraps_in_flight_submission_for_parent_locking()
    {
        TaskCompletionSource<TenantCommandSubmissionResult> pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        List<bool> activity = [];
        StubTenantCommandGateway gateway = new()
        {
            SetConfigurationAsync = _ => pending.Task,
        };
        RegisterServices(gateway);

        IRenderedComponent<SetTenantConfigurationFlow> cut = Render<SetTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, TenantFreshnessState.Current)
            .Add(p => p.OnCommandActivityChanged, active => activity.Add(active)));

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-namespace']").Change("billing");
        cut.Find("[data-testid='tenants-config-set-key']").Change("mode");
        cut.Find("[data-testid='tenants-config-set-value']").Change("enterprise");
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => activity.ShouldContain(true));

        pending.SetResult(TenantCommandSubmissionResult.Failed("Safe failure."));

        cut.WaitForAssertion(() => activity.ShouldContain(false));
    }

    [Fact]
    public void Owned_in_flight_command_keeps_visible_reason_specific_when_parent_lock_updates()
    {
        TaskCompletionSource<TenantCommandSubmissionResult> pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        StubTenantCommandGateway gateway = new()
        {
            SetConfigurationAsync = _ => pending.Task,
        };
        RegisterServices(gateway);

        IRenderedComponent<SetTenantConfigurationFlow> cut = Render<SetTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, TenantFreshnessState.Current)
            .Add(p => p.IsCommandSurfaceAvailable, true));

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-namespace']").Change("billing");
        cut.Find("[data-testid='tenants-config-set-key']").Change("mode");
        cut.Find("[data-testid='tenants-config-set-value']").Change("enterprise");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.RequestSent));
        cut.Render(parameters => parameters.Add(p => p.IsCommandSurfaceAvailable, false));

        cut.Find("[data-testid='tenants-config-set-unavailable-reason']").TextContent
            .ShouldContain("in progress", Case.Insensitive);
        cut.Find("[data-testid='tenants-config-set-unavailable-reason']").TextContent
            .ShouldNotContain("unavailable", Case.Insensitive);

        pending.SetResult(TenantCommandSubmissionResult.Failed("Safe failure."));
        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Failed));
    }

    private void RegisterServices(StubTenantCommandGateway gateway)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(gateway);
    }

    private static TenantDetail Detail(string tenantId, IReadOnlyDictionary<string, string> configuration)
        => new(
            tenantId,
            "Alpha",
            "Tenant alpha description",
            TenantStatus.Active,
            [new TenantMember("owner-user", TenantRole.TenantOwner)],
            configuration,
            DateTimeOffset.Parse("2026-06-01T12:00:00Z", CultureInfo.InvariantCulture));

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

        public Func<SetTenantConfigurationCommandRequest, Task<TenantCommandSubmissionResult>>? SetConfigurationAsync { get; init; }

        public Func<TenantCommandTrackingHandle, Task<TenantCommandStatusResult>>? StatusAsync { get; init; }

        public SetTenantConfigurationCommandRequest? LastSetConfigurationRequest { get; private set; }

        public int SetConfigurationCallCount { get; private set; }

        public Task<TenantCommandSubmissionResult> CreateTenantAsync(CreateTenantCommandRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> AddUserToTenantAsync(AddUserToTenantCommandRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> ChangeUserRoleAsync(ChangeUserRoleCommandRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> RemoveUserFromTenantAsync(RemoveUserFromTenantCommandRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> UpdateTenantAsync(UpdateTenantCommandRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> SetTenantConfigurationAsync(SetTenantConfigurationCommandRequest request, CancellationToken cancellationToken = default)
        {
            SetConfigurationCallCount++;
            LastSetConfigurationRequest = request;
            return SetConfigurationAsync is null ? Task.FromResult(Submission) : SetConfigurationAsync(request);
        }

        public Task<TenantCommandStatusResult> GetStatusAsync(TenantCommandTrackingHandle handle, CancellationToken cancellationToken = default)
            => StatusAsync is null ? Task.FromResult(Status) : StatusAsync(handle);
    }

    private sealed class StubTenantsLocalizer : IStringLocalizer<TenantsResources>
    {
        private static readonly Dictionary<string, string> Values = new(StringComparer.Ordinal)
        {
            ["Tenants.Configuration.Value.Unavailable"] = "Unavailable",
            ["Tenants.Configuration.Set.Title"] = "Set configuration",
            ["Tenants.Configuration.Set.Description"] = "Set tenant {0} configuration.",
            ["Tenants.Configuration.Set.Open"] = "Set configuration",
            ["Tenants.Configuration.Set.Namespace.Label"] = "Namespace prefix",
            ["Tenants.Configuration.Set.Namespace.Help"] = "Use a visible authorized namespace prefix.",
            ["Tenants.Configuration.Set.Key.Label"] = "Key",
            ["Tenants.Configuration.Set.Key.Help"] = "Use the key segment.",
            ["Tenants.Configuration.Set.Value.Label"] = "Value",
            ["Tenants.Configuration.Set.Value.Help"] = "Value is required.",
            ["Tenants.Configuration.Set.Submit"] = "Submit configuration change",
            ["Tenants.Configuration.Set.Refresh"] = "Refresh status",
            ["Tenants.Configuration.Set.Cancel"] = "Cancel",
            ["Tenants.Configuration.Set.Lifecycle.Title"] = "Configuration command lifecycle",
            ["Tenants.Configuration.Set.Unavailable.Authorization"] = "You are not authorized to set configuration for this tenant.",
            ["Tenants.Configuration.Set.Unavailable.ProjectionState"] = "Tenant detail is unavailable or degraded. Refresh current tenant detail before changing configuration.",
            ["Tenants.Configuration.Set.Unavailable.Freshness"] = "Refresh current tenant detail before changing configuration.",
            ["Tenants.Configuration.Set.Unavailable.TenantLifecycle"] = "This tenant lifecycle state does not allow configuration changes.",
            ["Tenants.Configuration.Set.Unavailable.CommandSurface"] = "Tenant command support is unavailable.",
            ["Tenants.Configuration.Set.Unavailable.InFlight"] = "A tenant command is already in progress.",
            ["Tenants.Configuration.Set.Unavailable.Identity"] = "Tenant identity is unavailable.",
            ["Tenants.Configuration.Set.Unavailable.Scope"] = "No authorized namespace prefix evidence is available.",
            ["Tenants.Configuration.Set.Unavailable.Narrow"] = "Configuration changes are unavailable on narrow layouts.",
            ["Tenants.Configuration.Set.Validation.NamespaceRequired"] = "Enter an authorized namespace prefix before previewing.",
            ["Tenants.Configuration.Set.Validation.NamespaceScope"] = "The namespace prefix cannot be proven from the current authorized projection.",
            ["Tenants.Configuration.Set.Validation.KeyRequired"] = "Enter a configuration key before previewing.",
            ["Tenants.Configuration.Set.Validation.KeyLength"] = "The full configuration key must be {0} characters or fewer.",
            ["Tenants.Configuration.Set.Validation.ValueRequired"] = "Enter a configuration value before previewing.",
            ["Tenants.Configuration.Set.Validation.ValueLength"] = "The configuration value must be {0} characters or fewer.",
            ["Tenants.Configuration.Set.Preview.Title"] = "Consequence preview",
            ["Tenants.Configuration.Set.Preview.Blocked.Required"] = "Complete required preview inputs.",
            ["Tenants.Configuration.Set.Preview.Tenant"] = "Tenant",
            ["Tenants.Configuration.Set.Preview.Namespace"] = "Namespace",
            ["Tenants.Configuration.Set.Preview.Key"] = "Full key",
            ["Tenants.Configuration.Set.Preview.CurrentState"] = "Current known state",
            ["Tenants.Configuration.Set.Preview.CurrentState.Absent"] = "No current value is visible for this key.",
            ["Tenants.Configuration.Set.Preview.IntendedEffect"] = "Intended effect",
            ["Tenants.Configuration.Set.Preview.IntendedEffect.Value"] = "The selected configuration key will be set after projection proof.",
            ["Tenants.Configuration.Set.Preview.Freshness"] = "Freshness evidence",
            ["Tenants.Configuration.Set.Preview.Authorization"] = "Authorization and scope evidence",
            ["Tenants.Configuration.Set.Preview.Authorization.Value"] = "The namespace prefix is visible in the authorized projection.",
            ["Tenants.Configuration.Set.Preview.KnownConsequences"] = "Known consequences",
            ["Tenants.Configuration.Set.Preview.KnownConsequences.Value"] = "Consumers may react after projection catches up.",
            ["Tenants.Configuration.Set.Preview.KnownUnknowns"] = "Known unknowns",
            ["Tenants.Configuration.Set.Preview.KnownUnknowns.Value"] = "Downstream impact is not proven.",
            ["Tenants.Configuration.Set.Preview.AuditExpectation"] = "Audit expectation",
            ["Tenants.Configuration.Set.Preview.AuditExpectation.Value"] = "Audit evidence is pending.",
            ["Tenants.Configuration.Set.Preview.RecoveryPath"] = "Recovery path",
            ["Tenants.Configuration.Set.Preview.RecoveryPath.Value"] = "Refresh tenant detail or submit a forward correction.",
            ["Tenants.Configuration.Set.Freshness.Current"] = "Current",
            ["Tenants.Configuration.Set.Freshness.Aging"] = "Aging",
            ["Tenants.Configuration.Set.Freshness.Refreshing"] = "Refreshing",
            ["Tenants.Configuration.Set.Freshness.Stale"] = "Stale",
            ["Tenants.Configuration.Set.Freshness.Unknown"] = "Unknown",
            ["Tenants.Configuration.Set.AlreadyApplied.BeforeSubmit"] = "The submitted key and value are already applied.",
            ["Tenants.Configuration.Set.DuplicatePrevented.Message"] = "A configuration command is already in progress.",
            ["Tenants.Configuration.Set.State.Idle"] = "No configuration command submitted.",
            ["Tenants.Configuration.Set.State.Previewed"] = "Configuration change preview ready.",
            ["Tenants.Configuration.Set.State.RequestSent"] = "Configuration change request sent.",
            ["Tenants.Configuration.Set.State.Accepted"] = "Accepted by EventStore; waiting for configuration processing.",
            ["Tenants.Configuration.Set.State.ProjectionPending"] = "Projection pending; submitted configuration is not confirmed visible yet.",
            ["Tenants.Configuration.Set.State.Confirmed"] = "Projection confirmed the submitted configuration change.",
            ["Tenants.Configuration.Set.State.Rejected"] = "Configuration command rejected.",
            ["Tenants.Configuration.Set.State.AlreadyApplied"] = "Already applied.",
            ["Tenants.Configuration.Set.State.DuplicatePrevented"] = "Duplicate configuration submission prevented.",
            ["Tenants.Configuration.Set.State.Failed"] = "Configuration command submission failed.",
            ["Tenants.Configuration.Set.State.Degraded"] = "Configuration command result is degraded.",
            ["Tenants.Configuration.Set.State.UnableToVerify"] = "Unable to verify the configuration command result.",
            ["Tenants.Configuration.Set.Audit.NotStarted"] = "Audit evidence not started.",
            ["Tenants.Configuration.Set.Audit.AuditPending"] = "Audit evidence pending.",
            ["Tenants.Configuration.Set.Audit.AuditDelayed"] = "Audit evidence delayed.",
            ["Tenants.Configuration.Set.Audit.AuditUnavailable"] = "Audit evidence unavailable.",
            ["Tenants.Configuration.Set.Audit.MissingSupport"] = "Audit evidence support is missing.",
            ["Tenants.Audit.EntryPoint.Accessible.Command"] = "Open audit evidence for {0} in tenant {1}",
            ["Tenants.Audit.EntryPoint.CommandReason"] = "Open the tenant audit list and use the visible audit state.",
            ["Tenants.Audit.EntryPoint.Label"] = "Audit evidence",
            ["Tenants.Audit.EntryPoint.Unavailable.ScopeRequired"] = "Tenant scope is required.",
            ["Tenants.Audit.EntryPoint.Unavailable.StaleScope"] = "Refresh tenant scope.",
            ["Tenants.Audit.Availability.Accessible.Delayed"] = "Audit delayed; retry status lookup or inspect audit.",
            ["Tenants.Audit.Availability.Accessible.MissingSupport"] = "Missing implementation support; continue read-only or escalate.",
            ["Tenants.Audit.Availability.Accessible.Pending"] = "Audit pending; wait, retry status lookup, or inspect audit.",
            ["Tenants.Audit.Availability.Accessible.Unavailable"] = "Audit unavailable; continue read-only, retry status lookup, or escalate.",
            ["Tenants.Audit.Availability.Action.ContinueReadOnly"] = "Continue read-only",
            ["Tenants.Audit.Availability.Action.Escalate"] = "Escalate",
            ["Tenants.Audit.Availability.Action.InspectAudit"] = "Inspect audit",
            ["Tenants.Audit.Availability.Action.Refresh"] = "Retry status lookup",
            ["Tenants.Audit.Availability.Action.Wait"] = "Wait",
            ["Tenants.Audit.Availability.ActionsLabel"] = "Audit availability recovery actions",
            ["Tenants.Audit.Availability.Reason.MissingSupport"] = "Continue read-only or escalate using support-safe information.",
            ["Tenants.Audit.Availability.Reason.Unavailable"] = "Continue read-only, retry status lookup, or escalate without raw diagnostics.",
            ["Tenants.Audit.Availability.State.Delayed"] = "Audit delayed",
            ["Tenants.Audit.Availability.State.MissingSupport"] = "Missing implementation support",
            ["Tenants.Audit.Availability.State.Pending"] = "Audit pending",
            ["Tenants.Audit.Availability.State.Unavailable"] = "Audit unavailable",
            ["Tenants.Configuration.Set.Recovery.Idle"] = "Open the form when projection evidence is available.",
            ["Tenants.Configuration.Set.Recovery.Previewed"] = "Submit, cancel, or continue read-only.",
            ["Tenants.Configuration.Set.Recovery.RequestSent"] = "Wait for command status and projection refresh.",
            ["Tenants.Configuration.Set.Recovery.Accepted"] = "Wait, refresh status, or continue read-only.",
            ["Tenants.Configuration.Set.Recovery.ProjectionPending"] = "Refresh tenant detail; do not display success until confirmed.",
            ["Tenants.Configuration.Set.Recovery.Confirmed"] = "Continue read-only or inspect audit later.",
            ["Tenants.Configuration.Set.Recovery.Rejected"] = "Refresh projection evidence, request permission, start correction, or escalate.",
            ["Tenants.Configuration.Set.Recovery.AlreadyApplied"] = "Continue read-only or submit a forward correction.",
            ["Tenants.Configuration.Set.Recovery.DuplicatePrevented"] = "Wait for the in-flight command.",
            ["Tenants.Configuration.Set.Recovery.Failed"] = "Retry after checking projection evidence.",
            ["Tenants.Configuration.Set.Recovery.Degraded"] = "Wait, retry status lookup, or escalate.",
            ["Tenants.Configuration.Set.Recovery.UnableToVerify"] = "Refresh, retry status lookup, or escalate.",
        };

        public LocalizedString this[string name]
            => new(name, Values.TryGetValue(name, out string? value) ? value : name);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, string.Format(CultureInfo.CurrentCulture, Values.TryGetValue(name, out string? value) ? value : name, arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => Values.Select(v => new LocalizedString(v.Key, v.Value));
    }
}
