using System.Globalization;

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

public sealed class SetTenantConfigurationFlowTests : FluentBunitContext
{
    [Fact]
    public void Set_configuration_flow_renders_complete_preview_and_shows_no_current_value_for_an_unapproved_key()
    {
        // The target key is inside the authorized `billing` namespace but has no safe row, which is
        // exactly how an unapproved or undefined-policy key reaches this flow. The current state must
        // read Unavailable and must not borrow another row's value: the component's only configuration
        // input is this context, and that is the property under test.
        RegisterServices(new StubTenantCommandGateway());

        IRenderedComponent<SetTenantConfigurationFlow> cut = Render<SetTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string>
            {
                ["billing.mode"] = "sibling-row-value",
            }))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-key']").Change("billing.endpoint");
        cut.Find("[data-testid='tenants-config-set-value']").Change("new-safe-value");

        cut.Find("[data-testid='tenants-config-set-flow']");
        cut.Find("[data-testid='tenants-config-set-preview']");
        cut.FindAll("[data-testid='tenants-config-set-preview-item']").Count.ShouldBe(11);
        cut.Find("[data-testid='tenants-config-set-preview-namespace']").TextContent.ShouldBe("billing");
        cut.Find("[data-testid='tenants-config-set-preview-key']").TextContent.ShouldContain("billing.endpoint");
        cut.Find("[data-testid='tenants-config-set-preview-current-state']").TextContent.ShouldContain("Unavailable");
        cut.Find("[data-testid='tenants-config-set-submit']").GetAttribute("disabled").ShouldBeNull();
        string keyDescription = cut.Find("[data-testid='tenants-config-set-key']")
            .GetAttribute("aria-describedby")
            .ShouldNotBeNull();
        keyDescription.ShouldNotContain("tenants-config-set-preview-blocked", Case.Insensitive);
        cut.Find("[data-testid='tenants-config-set-live-region']").GetAttribute("aria-live").ShouldBe("polite");
        cut.Find("[data-testid='tenants-config-set-preview']").TextContent.ShouldNotContain("sibling-row-value", Case.Insensitive);
        cut.Find("[data-testid='tenants-config-set-live-region']").TextContent.ShouldNotContain("new-safe-value", Case.Insensitive);
        cut.Markup.ShouldNotContain("audit available", Case.Insensitive);
        cut.Markup.ShouldNotContain("View receipt", Case.Insensitive);
        cut.Markup.ShouldNotContain("success", Case.Insensitive);
    }

    [Fact]
    public void Set_configuration_preview_preserves_a_legacy_safe_current_value()
    {
        RegisterServices(new StubTenantCommandGateway());
        IRenderedComponent<SetTenantConfigurationFlow> cut = Render<SetTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-key']").Change("billing.mode");
        cut.Find("[data-testid='tenants-config-set-value']").Change("paid");

        cut.Find("[data-testid='tenants-config-set-preview-current-state']").TextContent.ShouldContain("trial");
        cut.FindAll("[data-testid='tenants-config-copy-reference']").ShouldBeEmpty();
    }

    [Fact]
    public void Set_configuration_preview_shows_no_current_value_for_a_key_absent_from_the_safe_rows()
    {
        RegisterServices(new StubTenantCommandGateway());
        IRenderedComponent<SetTenantConfigurationFlow> cut = Render<SetTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-key']").Change("billing.password");
        cut.Find("[data-testid='tenants-config-set-value']").Change("paid");

        cut.Find("[data-testid='tenants-config-set-preview-current-state']").TextContent.ShouldContain("Unavailable");
        cut.Find("[data-testid='tenants-config-set-preview-current-state']").TextContent.ShouldNotContain("trial");
        cut.FindAll("[data-testid='tenants-config-copy-reference']").ShouldBeEmpty();
    }

    [Fact]
    public void Namespace_scope_must_be_proven_from_authorized_projection_before_gateway_submission()
    {
        StubTenantCommandGateway gateway = new();
        RegisterServices(gateway);

        IRenderedComponent<SetTenantConfigurationFlow> cut = Render<SetTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-key']").Change("security.mode");
        cut.Find("[data-testid='tenants-config-set-value']").Change("enabled");
        cut.Find("form").Submit();

        gateway.SetConfigurationCallCount.ShouldBe(0);
        cut.Find("[data-testid='tenants-config-set-validation']").TextContent.ShouldContain("prefix cannot be proven", Case.Insensitive);
        cut.FindAll("[data-testid='tenants-config-set-preview']").ShouldBeEmpty();
    }

    [Fact]
    public void Evidence_aware_set_with_complete_input_and_current_reauthorization_dispatches_exactly_once()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-1", "correlation-config"),
        };
        RegisterServices(gateway);
        TenantConfigurationManagementContext context = Context(
            "tenant.alpha",
            new Dictionary<string, string> { ["billing.mode"] = "trial" });

        IRenderedComponent<SetTenantConfigurationFlow> cut = Render<SetTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Context, context)
            .Add(p => p.Evidence, Evidence(TenantHighImpactAction.SetConfiguration))
            .Add(p => p.ReauthorizeProvider, () => Task.FromResult(context)));

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-key']").Change("billing.mode");
        cut.Find("[data-testid='tenants-config-set-value']").Change("enterprise");
        cut.Find("form").Submit();

        gateway.SetConfigurationCallCount.ShouldBe(1);
        gateway.LastSetConfigurationRequest.ShouldNotBeNull();
        gateway.LastSetConfigurationRequest.Key.ShouldBe("billing.mode");
    }

    [Fact]
    public void Evidence_aware_already_applied_set_preserves_safe_snapshot_without_dispatch()
    {
        StubTenantCommandGateway gateway = new();
        RegisterServices(gateway);
        TenantConfigurationManagementContext context = Context(
            "tenant.alpha",
            new Dictionary<string, string> { ["billing.mode"] = "trial" });

        IRenderedComponent<SetTenantConfigurationFlow> cut = Render<SetTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Context, context)
            .Add(p => p.Evidence, Evidence(TenantHighImpactAction.SetConfiguration))
            .Add(p => p.ReauthorizeProvider, () => Task.FromResult(context)));

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-key']").Change("billing.mode");
        cut.Find("[data-testid='tenants-config-set-value']").Change("trial");
        cut.Find("form").Submit();

        gateway.SetConfigurationCallCount.ShouldBe(0);
        cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.AlreadyApplied);
        cut.Instance.Snapshot.SafeMessage!.ShouldContain("already applied", Case.Insensitive);
    }

    [Fact]
    public void Exact_prefix_key_is_accepted_literally_without_appending_or_normalizing_segments()
    {
        RegisterServices(new StubTenantCommandGateway());

        IRenderedComponent<SetTenantConfigurationFlow> cut = Render<SetTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string> { ["billing"] = "trial" }))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-key']").Change("billing");
        cut.Find("[data-testid='tenants-config-set-value']").Change("enterprise");

        cut.Find("[data-testid='tenants-config-set-preview-key']").TextContent.ShouldBe("billing");
        cut.Find("[data-testid='tenants-config-set-preview-namespace']").TextContent.ShouldBe("billing");
        cut.Find("[data-testid='tenants-config-set-submit']").GetAttribute("disabled").ShouldBeNull();
    }

    [Fact]
    public void Submission_time_policy_revocation_blocks_set_before_gateway_dispatch()
    {
        StubTenantCommandGateway gateway = new();
        RegisterServices(gateway);

        IRenderedComponent<SetTenantConfigurationFlow> cut = Render<SetTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ReauthorizeProvider, () => Task.FromResult(Context(
                "tenant.alpha",
                new Dictionary<string, string> { ["security.mode"] = "enabled" }))));

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-key']").Change("billing.mode");
        cut.Find("[data-testid='tenants-config-set-value']").Change("enterprise");
        cut.Find("form").Submit();

        gateway.SetConfigurationCallCount.ShouldBe(0);
        cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        cut.Find("[data-testid='tenants-config-set-state']").TextContent.ShouldContain("Unable to verify", Case.Insensitive);
    }

    [Fact]
    public void Editing_valid_preview_back_to_invalid_clears_stale_preview_lifecycle_state()
    {
        RegisterServices(new StubTenantCommandGateway());

        IRenderedComponent<SetTenantConfigurationFlow> cut = Render<SetTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-key']").Change("billing.mode");
        cut.Find("[data-testid='tenants-config-set-value']").Change("enterprise");

        cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Previewed);
        cut.Find("[data-testid='tenants-config-set-preview']");

        cut.Find("[data-testid='tenants-config-set-key']").Change("");

        cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Idle);
        cut.FindAll("[data-testid='tenants-config-set-preview']").ShouldBeEmpty();
        cut.Find("[data-testid='tenants-config-set-submit']").GetAttribute("disabled").ShouldNotBeNull();
    }

    [Theory]
    [InlineData("", "enabled", "key")]
    [InlineData("billing.mode", "", "value")]
    public void Required_full_key_and_value_block_submission_with_field_safe_messages(
        string key,
        string value,
        string expectedText)
    {
        StubTenantCommandGateway gateway = new();
        RegisterServices(gateway);

        IRenderedComponent<SetTenantConfigurationFlow> cut = Render<SetTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-key']").Change(key);
        cut.Find("[data-testid='tenants-config-set-value']").Change(value);
        cut.Find("form").Submit();

        gateway.SetConfigurationCallCount.ShouldBe(0);
        cut.Find("[data-testid='tenants-config-set-validation']").TextContent.ShouldContain(expectedText, Case.Insensitive);
        cut.Markup.ShouldNotContain("raw payload", Case.Insensitive);
        cut.Markup.ShouldNotContain("\"payload\"", Case.Insensitive);
        cut.Markup.ShouldNotContain("access_token", Case.Insensitive);
        cut.Markup.ShouldNotContain("bearer ", Case.Insensitive);
        cut.Markup.ShouldNotContain("correlation", Case.Insensitive);
    }

    [Fact]
    public void Domain_key_and_value_limits_block_submission_before_gateway_call()
    {
        StubTenantCommandGateway gateway = new();
        RegisterServices(gateway);

        IRenderedComponent<SetTenantConfigurationFlow> cut = Render<SetTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-key']").Change("billing." + new string('k', 250));
        cut.Find("[data-testid='tenants-config-set-value']").Change("enabled");
        cut.Find("form").Submit();

        gateway.SetConfigurationCallCount.ShouldBe(0);
        cut.Find("[data-testid='tenants-config-set-validation']").TextContent.ShouldContain("256");

        cut.Find("[data-testid='tenants-config-set-key']").Change("billing.mode");
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

        // Already-applied is decided from the re-authorized context, not the render-time parameter, so
        // a revoked grant can no longer yield a terminal success state from stale rows.
        TenantConfigurationManagementContext current = Context(
            "tenant.alpha",
            new Dictionary<string, string> { ["billing.mode"] = "trial" });
        IRenderedComponent<SetTenantConfigurationFlow> cut = Render<SetTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, current)
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.ReauthorizeProvider, () => Task.FromResult(current))
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-key']").Change("billing.mode");
        cut.Find("[data-testid='tenants-config-set-value']").Change("trial");
        cut.Find("form").Submit();

        gateway.SetConfigurationCallCount.ShouldBe(0);
        cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.AlreadyApplied);
        cut.Find("[data-testid='tenants-config-set-state']").TextContent.ShouldContain("Already applied");
        cut.Markup.ShouldNotContain("success", Case.Insensitive);
    }

    [Fact]
    public void Already_applied_is_decided_from_the_reauthorized_context_not_the_render_time_parameter()
    {
        // The pre-existing test passed the SAME context instance as both Context and the reauthorize result,
        // so `Context.FindRemovableRow` and `currentContext.FindRemovableRow` were indistinguishable and the
        // mutation it claimed to catch survived. These two contexts genuinely differ.
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-1", "correlation-config"),
        };
        RegisterServices(gateway);

        TenantConfigurationManagementContext renderTime = Context(
            "tenant.alpha",
            new Dictionary<string, string> { ["billing.mode"] = "trial" });
        TenantConfigurationManagementContext reauthorized = Context(
            "tenant.alpha",
            new Dictionary<string, string> { ["billing.mode"] = "production" });

        IRenderedComponent<SetTenantConfigurationFlow> cut = Render<SetTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, renderTime)
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.ReauthorizeProvider, () => Task.FromResult(reauthorized))
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-key']").Change("billing.mode");
        cut.Find("[data-testid='tenants-config-set-value']").Change("trial");
        cut.Find("form").Submit();

        // Render-time rows say "trial" is already applied; the re-authorized rows say it is not. The command
        // must be sent, proving the decision came from the re-authorized context.
        cut.Instance.Snapshot.State.ShouldNotBe(TenantCommandLifecycleState.AlreadyApplied);
        gateway.SetConfigurationCallCount.ShouldBe(1);
    }

    [Fact]
    public void Submit_fails_closed_when_no_reauthorize_provider_is_wired()
    {
        // A consumer that forgets the optional callback must not silently fall back to the render-time
        // context. Every existing submit test wired one, so this branch had never executed.
        StubTenantCommandGateway gateway = new();
        RegisterServices(gateway);

        IRenderedComponent<SetTenantConfigurationFlow> cut = Render<SetTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-key']").Change("billing.mode");
        cut.Find("[data-testid='tenants-config-set-value']").Change("production");
        cut.Find("form").Submit();

        gateway.SetConfigurationCallCount.ShouldBe(0);
        cut.Instance.Snapshot.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
    }

    [Fact]
    public void Submit_fails_closed_when_reauthorization_throws()
    {
        // No reauthorize provider anywhere in the suite threw, so the catch that maps failure to an
        // unavailable context — rather than to the render-time context — had never executed.
        StubTenantCommandGateway gateway = new();
        RegisterServices(gateway);

        IRenderedComponent<SetTenantConfigurationFlow> cut = Render<SetTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ReauthorizeProvider, () => Task.FromException<TenantConfigurationManagementContext>(
                new InvalidOperationException("policy backend unreachable"))));

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-key']").Change("billing.mode");
        cut.Find("[data-testid='tenants-config-set-value']").Change("production");
        cut.Find("form").Submit();

        gateway.SetConfigurationCallCount.ShouldBe(0);
        cut.Instance.Snapshot.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
        cut.Markup.ShouldNotContain("policy backend unreachable");
        cut.Markup.ShouldNotContain("InvalidOperationException");
    }

    [Theory]
    [InlineData("billing.tier ")]
    [InlineData(" billing.tier")]
    [InlineData("billing.\u200btier")]
    // Interior separators that are neither Control nor Format. Testing only char.IsControl and
    // UnicodeCategory.Format let every one of these through: Trim() touches the ends only, so an interior
    // NBSP survives, renders identically to a plain space, and makes the key permanently unremovable --
    // the exact outcome the guard exists to prevent.
    [InlineData("billing.\u00a0tier")]
    [InlineData("billing.\u2007tier")]
    [InlineData("billing.\u202ftier")]
    [InlineData("billing.\u3000tier")]
    [InlineData("billing.\u2028tier")]
    // Astral plane. Every row above is BMP, so the whole guard still passed with a per-char scan --
    // char.GetUnicodeCategory classifies neither half of a surrogate pair as Format. U+1D173 (MUSICAL SYMBOL
    // BEGIN BEAM) is Cf and two UTF-16 units, so it is the only row that distinguishes EnumerateRunes from
    // the char indexer, which is the sole claim the guard's own comment makes for using it.
    [InlineData("billing.\U0001D173tier")]
    public void Keys_that_cannot_be_reproduced_by_typing_are_rejected_rather_than_written(string key)
    {
        // Rejection, not normalization: such a key renders identically to its clean twin and can never
        // satisfy the remove flow's ordinal confirmation, so it would be permanently unremovable.
        StubTenantCommandGateway gateway = new();
        RegisterServices(gateway);

        IRenderedComponent<SetTenantConfigurationFlow> cut = Render<SetTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ReauthorizeProvider, () => Task.FromResult(
                Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))));

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-key']").Change(key);
        cut.Find("[data-testid='tenants-config-set-value']").Change("production");
        cut.Find("form").Submit();

        gateway.SetConfigurationCallCount.ShouldBe(0);
    }

    [Fact]
    public void A_global_administrator_gets_the_same_key_shape_guard_as_everyone_else()
    {
        // IsKeyAuthorized short-circuits on IsGlobalAdministrator, so for a global administrator it rejects
        // nothing: the shape guard is the only clause between an un-typeable key and the command. Every
        // other row in the theory above uses a non-global-admin context, where a leading space is rejected
        // by the authorization clause instead -- which is why that row proved nothing about this guard.
        StubTenantCommandGateway gateway = new();
        RegisterServices(gateway);

        TenantConfigurationManagementContext globalAdministrator = TenantConfigurationManagementContext.Available(
            "tenant.alpha",
            TenantStatus.Active,
            true,
            ["*"],
            [new TenantConfigurationSafeRow("billing", "billing.mode", "trial")]);

        IRenderedComponent<SetTenantConfigurationFlow> cut = Render<SetTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, globalAdministrator)
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ReauthorizeProvider, () => Task.FromResult(globalAdministrator)));

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-key']").Change(" billing.tier");
        cut.Find("[data-testid='tenants-config-set-value']").Change("production");
        cut.Find("form").Submit();

        gateway.SetConfigurationCallCount.ShouldBe(0);
    }

    [Fact]
    public void A_global_administrator_can_set_a_key_outside_any_ordinary_prefix_grant()
    {
        // Every prior fixture set isGlobalAdministrator: false. Deleting the IsGlobalAdministrator branch of
        // IsKeyAuthorized or ResolveAuthorizedNamespace permanently blocks a proven global administrator
        // from setting configuration, and nothing in the suite failed.
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-1", "correlation-config"),
            Status = new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1),
        };
        RegisterServices(gateway);

        TenantConfigurationManagementContext globalAdministrator = TenantConfigurationManagementContext.Available(
            "tenant.alpha",
            TenantStatus.Active,
            isGlobalAdministrator: true,
            authorizedPrefixes: [],
            removableRows: []);

        IRenderedComponent<SetTenantConfigurationFlow> cut = Render<SetTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, globalAdministrator)
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ReauthorizeProvider, () => Task.FromResult(globalAdministrator))
            .Add(p => p.ProjectionEvidenceProvider, request => Task.FromResult(
                Proof(request.TenantId, TenantConfigurationProjectionProofKind.SetConfirmed))));

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-key']").Change("ops.feature");
        cut.Find("[data-testid='tenants-config-set-value']").Change("enabled");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Confirmed));
        gateway.SetConfigurationCallCount.ShouldBe(1);
        gateway.LastSetConfigurationRequest.ShouldNotBeNull().ShouldBe(
            new SetTenantConfiguration("tenant.alpha", "ops.feature", "enabled"));
    }

    [Fact]
    public void Preview_namespace_selects_the_longest_matching_authorized_prefix()
    {
        // Both flow suites previously derived prefixes by splitting keys on the first dot, so
        // AuthorizedPrefixes never contained two prefixes matching one key. OrderByDescending(length)
        // → OrderBy(length) survived, and the destructive-command preview's Namespace row is the
        // authorization evidence shown before a high-impact change.
        StubTenantCommandGateway gateway = new();
        RegisterServices(gateway);

        TenantConfigurationManagementContext context = TenantConfigurationManagementContext.Available(
            "tenant.alpha",
            TenantStatus.Active,
            isGlobalAdministrator: false,
            authorizedPrefixes: ["app", "app.feature"],
            removableRows: [new TenantConfigurationSafeRow("app.feature", "app.feature.flag", "off")]);

        IRenderedComponent<SetTenantConfigurationFlow> cut = Render<SetTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, context)
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ReauthorizeProvider, () => Task.FromResult(context)));

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-key']").Change("app.feature.flag");
        cut.Find("[data-testid='tenants-config-set-value']").Change("on");

        cut.Find("[data-testid='tenants-config-set-preview-namespace']").TextContent.ShouldBe("app.feature");
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
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ReauthorizeProvider, () => Task.FromResult(Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" })))
            .Add(p => p.ProjectionEvidenceProvider, request => Task.FromResult(
                Proof(request.TenantId, TenantConfigurationProjectionProofKind.SetConfirmed))));

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-key']").Change("billing.mode");
        cut.Find("[data-testid='tenants-config-set-value']").Change("enterprise");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Confirmed));
        gateway.SetConfigurationCallCount.ShouldBe(1);
        gateway.LastSetConfigurationRequest.ShouldNotBeNull().ShouldBe(
            new SetTenantConfiguration("tenant.alpha", "billing.mode", "enterprise"));
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
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ReauthorizeProvider, () => Task.FromResult(Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" })))
            .Add(p => p.ProjectionEvidenceProvider, request => Task.FromResult(Proof(
                request.TenantId,
                ++projectionCalls == 1
                    ? TenantConfigurationProjectionProofKind.SetNotConfirmed
                    : TenantConfigurationProjectionProofKind.SetConfirmed))));

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-key']").Change("billing.mode");
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
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ReauthorizeProvider, () => Task.FromResult(Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" })))
            .Add(p => p.ProjectionEvidenceProvider, request => Task.FromResult(
                Proof(request.TenantId, TenantConfigurationProjectionProofKind.SetConfirmed))));

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-key']").Change("billing.mode");
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
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ReauthorizeProvider, () => Task.FromResult(Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" })))
            .Add(p => p.ProjectionEvidenceProvider, request => Task.FromResult(
                Proof(request.TenantId, TenantConfigurationProjectionProofKind.SetConfirmed))));

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-key']").Change("billing.mode");
        cut.Find("[data-testid='tenants-config-set-value']").Change("enterprise");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(expectedState));
        cut.Find("[data-testid='tenants-config-set-state']").TextContent.ShouldContain(expectedText, Case.Insensitive);
        cut.Find("[data-testid='tenants-config-set-live-region']").GetAttribute("aria-live").ShouldBe(expectedLiveRegion);
        cut.Markup.ShouldNotContain("correlation-config", Case.Insensitive);
        cut.Markup.ShouldNotContain("raw payload", Case.Insensitive);
        cut.Markup.ShouldNotContain("\"payload\"", Case.Insensitive);
        cut.Markup.ShouldNotContain("access_token", Case.Insensitive);
        cut.Markup.ShouldNotContain("bearer ", Case.Insensitive);
        cut.Instance.Snapshot.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
    }

    [Theory]
    [InlineData(TenantDetailSurfaceKind.Unauthorized, ReadModelFreshnessState.Current, TenantStatus.Active, "not authorized")]
    [InlineData(TenantDetailSurfaceKind.Degraded, ReadModelFreshnessState.Current, TenantStatus.Active, "degraded")]
    [InlineData(TenantDetailSurfaceKind.Stale, ReadModelFreshnessState.Stale, TenantStatus.Active, "Refresh current")]
    [InlineData(TenantDetailSurfaceKind.Ready, ReadModelFreshnessState.Current, TenantStatus.Disabled, "lifecycle state")]
    [InlineData(TenantDetailSurfaceKind.Ready, ReadModelFreshnessState.Unknown, TenantStatus.Active, "Refresh current")]
    public void Set_configuration_fails_closed_for_stale_unknown_or_disabled_projection(
        TenantDetailSurfaceKind surfaceKind,
        ReadModelFreshnessState freshness,
        TenantStatus status,
        string expectedReason)
    {
        RegisterServices(new StubTenantCommandGateway());

        IRenderedComponent<SetTenantConfigurationFlow> cut = Render<SetTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }, status))
            .Add(p => p.SurfaceKind, surfaceKind)
            .Add(p => p.Freshness, freshness));

        cut.Find("[data-testid='tenants-config-set-unavailable-reason']").TextContent.ShouldContain(expectedReason, Case.Insensitive);
        cut.FindAll("[data-testid='tenants-config-set-open']").ShouldBeEmpty();
    }

    [Theory]
    [InlineData(ProjectionLifecycleState.Unknown)]
    [InlineData(ProjectionLifecycleState.Stale)]
    [InlineData(ProjectionLifecycleState.Rebuilding)]
    [InlineData(ProjectionLifecycleState.Degraded)]
    [InlineData(ProjectionLifecycleState.Unavailable)]
    [InlineData(ProjectionLifecycleState.LocalOnly)]
    public void Set_configuration_requires_current_projection_lifecycle(ProjectionLifecycleState lifecycle)
    {
        RegisterServices(new StubTenantCommandGateway());

        IRenderedComponent<SetTenantConfigurationFlow> cut = Render<SetTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.Lifecycle, lifecycle));

        cut.Find("[data-testid='tenants-config-set-unavailable-reason']").TextContent
            .ShouldContain("projection-confirmed lifecycle", Case.Insensitive);
        cut.FindAll("[data-testid='tenants-config-set-open']").ShouldBeEmpty();
    }

    /// <summary>
    /// Pins decision D-F's clause ORDER, which the sibling theories cannot: they fix
    /// <c>Lifecycle = Current</c> on every row, so the lifecycle clause never fires and hoisting it back
    /// above the surface and freshness clauses changes no outcome. Every failed read really does carry a
    /// non-Current lifecycle, so with the wrong order the lifecycle test answers for all of them and an
    /// operator whose read simply failed is told to refresh the projection lifecycle.
    /// The discriminator is the absence of the lifecycle reason, not the presence of the other one.
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

        IRenderedComponent<SetTenantConfigurationFlow> cut = Render<SetTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.SurfaceKind, surfaceKind)
            .Add(p => p.Freshness, ReadModelFreshnessState.Unknown)
            .Add(p => p.Lifecycle, lifecycle));

        string reason = cut.Find("[data-testid='tenants-config-set-unavailable-reason']").TextContent;
        reason.ShouldContain("unavailable or degraded", Case.Insensitive);
        reason.ShouldNotContain("projection-confirmed lifecycle", Case.Insensitive);
        cut.FindAll("[data-testid='tenants-config-set-open']").ShouldBeEmpty();
    }

    /// <summary>
    /// Freshness half of the same clause-order contract: a stale or unknown-freshness read on an otherwise
    /// ready surface must report freshness, not the projection lifecycle, even though its lifecycle is also
    /// non-Current. Asserting the projection-state text is absent keeps the two failing arms distinct.
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

        IRenderedComponent<SetTenantConfigurationFlow> cut = Render<SetTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.SurfaceKind, surfaceKind)
            .Add(p => p.Freshness, freshness)
            .Add(p => p.Lifecycle, lifecycle));

        string reason = cut.Find("[data-testid='tenants-config-set-unavailable-reason']").TextContent;
        reason.ShouldContain("Refresh current tenant detail", Case.Insensitive);
        reason.ShouldNotContain("unavailable or degraded", Case.Insensitive);
        reason.ShouldNotContain("projection-confirmed lifecycle", Case.Insensitive);
        cut.FindAll("[data-testid='tenants-config-set-open']").ShouldBeEmpty();
    }

    [Fact]
    public void Missing_authorization_or_scope_evidence_fails_closed_before_editor_opens()
    {
        StubTenantCommandGateway gateway = new();
        RegisterServices(gateway);

        // The dead IsAuthorized parameter was removed: the only call site never passed it, so it was
        // permanently true and this branch was reachable only from a test. SurfaceKind is the surviving gate.
        IRenderedComponent<SetTenantConfigurationFlow> unauthorized = Render<SetTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Unauthorized)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        unauthorized.Find("[data-testid='tenants-config-set-unavailable-reason']").TextContent
            .ShouldContain("not authorized", Case.Insensitive);
        unauthorized.FindAll("[data-testid='tenants-config-set-open']").ShouldBeEmpty();

        IRenderedComponent<SetTenantConfigurationFlow> missingScope = Render<SetTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string>()))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

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
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.OnCloseRequested, () => closeCount++));

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-cancel']").Click();
        cut.FindAll("[data-testid='tenants-config-set-key']").ShouldBeEmpty();

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-flow']").KeyDown("Escape");
        cut.FindAll("[data-testid='tenants-config-set-key']").ShouldBeEmpty();

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
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ReauthorizeProvider, () => Task.FromResult(Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" })))
            .Add(p => p.OnCommandActivityChanged, active => activity.Add(active)));

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-key']").Change("billing.mode");
        cut.Find("[data-testid='tenants-config-set-value']").Change("enterprise");
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => activity.ShouldContain(true));

        pending.SetResult(TenantCommandSubmissionResult.Failed("Safe failure."));

        cut.WaitForAssertion(() => activity.ShouldContain(false));
    }

    [Fact]
    public void Command_activity_lock_is_held_until_configuration_projection_confirms()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-1", "correlation-config"),
            Status = new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1),
        };
        string projectedValue = "trial";
        List<bool> activity = [];
        RegisterServices(gateway);

        IRenderedComponent<SetTenantConfigurationFlow> cut = Render<SetTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ReauthorizeProvider, () => Task.FromResult(Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" })))
            .Add(p => p.OnCommandActivityChanged, active => activity.Add(active))
            .Add(p => p.ProjectionEvidenceProvider, request => Task.FromResult(Proof(
                request.TenantId,
                string.Equals(projectedValue, request.Value, StringComparison.Ordinal)
                    ? TenantConfigurationProjectionProofKind.SetConfirmed
                    : TenantConfigurationProjectionProofKind.SetNotConfirmed))));

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-key']").Change("billing.mode");
        cut.Find("[data-testid='tenants-config-set-value']").Change("enterprise");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending));
        activity.ShouldBe([true]);
        cut.Find("[data-testid='tenants-config-set-submit']").GetAttribute("disabled").ShouldNotBeNull();

        projectedValue = "enterprise";
        cut.Find("[data-testid='tenants-config-set-refresh']").Click();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Confirmed));
        activity.ShouldBe([true, false]);
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
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Context, Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ReauthorizeProvider, () => Task.FromResult(Context("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" })))
            .Add(p => p.IsCommandSurfaceAvailable, true));

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-key']").Change("billing.mode");
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

    private static TenantConfigurationManagementContext Context(
        string tenantId,
        IReadOnlyDictionary<string, string> configuration,
        TenantStatus status = TenantStatus.Active)
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
            status,
            false,
            authorizedPrefixes,
            rows);
    }

    private static string Namespace(string key)
    {
        int separator = key.IndexOf('.', StringComparison.Ordinal);
        return separator > 0 ? key[..separator] : key;
    }


    private static TenantConfigurationProjectionProof Proof(
        string tenantId,
        TenantConfigurationProjectionProofKind kind)
        => TenantConfigurationProjectionProof.Create(tenantId, kind);

    private static TenantHighImpactActionEvidence Evidence(TenantHighImpactAction action)
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
            TenantHighImpactNamespaceScopeEvidence.Authorized,
            TenantHighImpactSupportEvidence.Ready,
            TenantHighImpactAdmissionEvidence.Available,
            TenantHighImpactPreviewEvidence.Ready,
            TenantHighImpactProofEvidence.NotRequired,
            TenantHighImpactViewportState.Safe,
            IsInputComplete: false,
            TenantHighImpactTargetState.Unknown,
            ProjectionVersion: null);

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

        public Func<SetTenantConfiguration, Task<TenantCommandSubmissionResult>>? SetConfigurationAsync { get; init; }

        public Func<TenantCommandTrackingHandle, Task<TenantCommandStatusResult>>? StatusAsync { get; init; }

        public SetTenantConfiguration? LastSetConfigurationRequest { get; private set; }

        public int SetConfigurationCallCount { get; private set; }

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
            ["Tenants.Configuration.Set.Title"] = "Set configuration",
            ["Tenants.Configuration.Set.Description"] = "Prepare a scoped configuration change for tenant {0} with projection confirmation.",
            ["Tenants.Configuration.Set.Open"] = "Set configuration",
            ["Tenants.Configuration.Set.Key.Label"] = "Full configuration key",
            ["Tenants.Configuration.Set.Key.Help"] = "Enter the exact literal full key within a current authorized prefix.",
            ["Tenants.Configuration.Set.Value.Label"] = "Value",
            ["Tenants.Configuration.Set.Value.Help"] = "Value is required and is not echoed in lifecycle messages.",
            ["Tenants.Configuration.Set.Submit"] = "Submit configuration change",
            ["Tenants.Configuration.Set.Refresh"] = "Refresh status",
            ["Tenants.Configuration.Set.Cancel"] = "Cancel",
            ["Tenants.Configuration.Set.Lifecycle.Title"] = "Configuration command lifecycle",
            ["Tenants.Configuration.Set.Unavailable.Authorization"] = "You are not authorized to set configuration for this tenant.",
            ["Tenants.Configuration.Set.Unavailable.ProjectionState"] = "Tenant detail is unavailable or degraded. Refresh current tenant detail before changing configuration.",
            ["Tenants.Configuration.Set.Unavailable.Freshness"] = "Refresh current tenant detail before changing configuration.",
            ["Tenants.Configuration.Set.Unavailable.ProjectionLifecycle"] = "Changing configuration requires a current, projection-confirmed lifecycle.",
            ["Tenants.Configuration.Set.Unavailable.TenantLifecycle"] = "This tenant lifecycle state does not allow configuration changes.",
            ["Tenants.Configuration.Set.Unavailable.CommandSurface"] = "Tenant command support is unavailable.",
            ["Tenants.Configuration.Set.Unavailable.InFlight"] = "A tenant command is already in progress.",
            ["Tenants.Configuration.Set.Unavailable.Identity"] = "Tenant identity is unavailable, so configuration changes fail closed.",
            ["Tenants.Configuration.Set.Unavailable.Scope"] = "No authorized namespace prefix evidence is available from the current projection.",
            ["Tenants.Configuration.Set.Unavailable.Narrow"] = "Configuration changes are unavailable on narrow layouts because preview, tenant identity, freshness, and confirmed configuration context must remain visible together.",
            ["Tenants.Configuration.Set.Validation.NamespaceScope"] = "The namespace prefix cannot be proven from the current authorized projection.",
            ["Tenants.Configuration.Set.Validation.KeyRequired"] = "Enter a configuration key before previewing the configuration change.",
            ["Tenants.Configuration.Set.Validation.KeyLength"] = "The full configuration key must be {0} characters or fewer.",
            ["Tenants.Configuration.Set.Validation.ValueRequired"] = "Enter a configuration value before previewing the configuration change.",
            ["Tenants.Configuration.Set.Validation.ValueLength"] = "The configuration value must be {0} characters or fewer.",
            ["Tenants.Configuration.Set.Preview.Title"] = "Consequence preview",
            ["Tenants.Configuration.Set.Preview.Blocked.Required"] = "Complete tenant identity, namespace, key, value, freshness, authorization, and scope evidence before submitting.",
            ["Tenants.Configuration.Set.Preview.Tenant"] = "Tenant",
            ["Tenants.Configuration.Set.Preview.Namespace"] = "Namespace",
            ["Tenants.Configuration.Set.Preview.Key"] = "Full key",
            ["Tenants.Configuration.Set.Preview.CurrentState"] = "Current known state",
            ["Tenants.Configuration.Set.Preview.IntendedEffect"] = "Intended effect",
            ["Tenants.Configuration.Set.Preview.IntendedEffect.Value"] = "The selected configuration key will be set after command acceptance and projection proof.",
            ["Tenants.Configuration.Set.Preview.Freshness"] = "Freshness evidence",
            ["Tenants.Configuration.Set.Preview.Authorization"] = "Authorization and scope evidence",
            ["Tenants.Configuration.Set.Preview.Authorization.Value"] = "The namespace prefix is visible in the authorized tenant projection; backend authorization still enforces the command.",
            ["Tenants.Configuration.Set.Preview.KnownConsequences"] = "Known consequences",
            ["Tenants.Configuration.Set.Preview.KnownConsequences.Value"] = "Consumers that own this prefix may react after projection catches up.",
            ["Tenants.Configuration.Set.Preview.KnownUnknowns"] = "Known unknowns",
            ["Tenants.Configuration.Set.Preview.KnownUnknowns.Value"] = "This UI cannot prove downstream consumer impact or audit receipt availability.",
            ["Tenants.Configuration.Set.Preview.AuditExpectation"] = "Audit expectation",
            ["Tenants.Configuration.Set.Preview.AuditExpectation.Value"] = "Audit evidence is pending until the Epic 5 evidence source exists.",
            ["Tenants.Configuration.Set.Preview.RecoveryPath"] = "Recovery path",
            ["Tenants.Configuration.Set.Preview.RecoveryPath.Value"] = "Refresh tenant detail, retry only from current projection proof, or submit a forward correction.",
            ["Tenants.Configuration.Set.Freshness.Current"] = "Current",
            ["Tenants.Configuration.Set.Freshness.Aging"] = "Aging",
            ["Tenants.Configuration.Set.Freshness.Refreshing"] = "Refreshing",
            ["Tenants.Configuration.Set.Freshness.Stale"] = "Stale",
            ["Tenants.Configuration.Set.Freshness.Unknown"] = "Unknown",
            ["Tenants.Configuration.Set.AlreadyApplied.BeforeSubmit"] = "The submitted key and value are already applied in the last confirmed projection.",
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
            ["Tenants.Configuration.Set.State.Degraded"] = "Configuration command result is degraded and needs review.",
            ["Tenants.Configuration.Set.State.UnableToVerify"] = "Unable to verify the configuration command result.",
            ["Tenants.Configuration.Set.Audit.NotStarted"] = "Audit evidence not started.",
            ["Tenants.Configuration.Set.Audit.AuditPending"] = "Audit evidence pending.",
            ["Tenants.Configuration.Set.Audit.AuditDelayed"] = "Audit evidence delayed.",
            ["Tenants.Configuration.Set.Audit.AuditUnavailable"] = "Audit evidence unavailable.",
            ["Tenants.Configuration.Set.Audit.MissingSupport"] = "Audit evidence support is missing until Epic 5 implements the evidence source.",
            ["Tenants.Audit.EntryPoint.Accessible.Command"] = "Open audit evidence for {0} in tenant {1}",
            ["Tenants.Audit.EntryPoint.CommandReason"] = "Command-specific proof is not available here; open the tenant audit list and use the visible audit state.",
            ["Tenants.Audit.EntryPoint.Label"] = "Audit evidence",
            ["Tenants.Audit.EntryPoint.Unavailable.ScopeRequired"] = "Tenant scope is required before audit evidence can be opened.",
            ["Tenants.Audit.EntryPoint.Unavailable.StaleScope"] = "Refresh tenant scope before opening audit evidence.",
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
            ["Tenants.Configuration.Set.Recovery.Idle"] = "Open the form when current projection evidence and namespace scope are available.",
            ["Tenants.Configuration.Set.Recovery.Previewed"] = "Submit, cancel, or continue read-only.",
            ["Tenants.Configuration.Set.Recovery.RequestSent"] = "Wait for command status and projection refresh.",
            ["Tenants.Configuration.Set.Recovery.Accepted"] = "Wait, refresh status, or continue read-only until projection confirms the configuration.",
            ["Tenants.Configuration.Set.Recovery.ProjectionPending"] = "Refresh tenant detail; do not display success until the submitted key and value are confirmed.",
            ["Tenants.Configuration.Set.Recovery.Confirmed"] = "Continue read-only or inspect audit when evidence becomes available.",
            ["Tenants.Configuration.Set.Recovery.Rejected"] = "Refresh projection evidence, request permission, start correction, or escalate.",
            ["Tenants.Configuration.Set.Recovery.AlreadyApplied"] = "Continue read-only or submit a forward correction if the intended configuration differs.",
            ["Tenants.Configuration.Set.Recovery.DuplicatePrevented"] = "Wait for the in-flight command, retry status lookup, or continue read-only.",
            ["Tenants.Configuration.Set.Recovery.Failed"] = "Retry after checking current projection evidence or escalate.",
            ["Tenants.Configuration.Set.Recovery.Degraded"] = "Wait, retry status lookup, inspect audit when available, or escalate.",
            ["Tenants.Configuration.Set.Recovery.UnableToVerify"] = "Refresh, retry status lookup, continue read-only, or escalate.",
        };

        public LocalizedString this[string name]
            => new(name, Values.TryGetValue(name, out string? value) ? value : name);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, string.Format(CultureInfo.CurrentCulture, Values.TryGetValue(name, out string? value) ? value : name, arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => Values.Select(v => new LocalizedString(v.Key, v.Value));
    }
}
