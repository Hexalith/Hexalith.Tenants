using System.Globalization;

using Bunit;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.UI.Components.Pages;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.GlobalAdministrators;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.Tenants.UI.State.UserTenants;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class GlobalAdministratorsPageTests : BunitContext
{
    [Fact]
    public void Authorized_operator_sees_global_administrators_from_fixed_scope()
    {
        var gateway = new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("platform-admin.alpha", TenantFreshnessState.Current)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"etag\"",
            freshness: TenantFreshnessState.Current));
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        gateway.GlobalAdministratorCalls.ShouldBe(1);
        cut.Find("[data-testid='tenants-global-admins-area']");
        cut.Find("[data-testid='tenants-global-admins-scope']").TextContent.ShouldContain("global-administrators");
        cut.Find("[data-testid='tenants-global-admins-scope']").TextContent.ShouldContain("system");
        cut.Find("[data-testid='tenants-global-admins-list']");
        cut.Find("[data-testid='tenants-global-admins-row']");
        cut.Find("[data-testid='tenants-global-admins-user-id']").TextContent.ShouldBe("platform-admin.alpha");
        cut.Find("[data-testid='tenants-global-admins-authority-scope']").TextContent.ShouldContain("Platform authority");
        cut.Find("[data-testid='tenants-global-admins-action-reasons']").TextContent.ShouldContain("Grant is available");
        cut.Find("[data-testid='tenants-global-admins-live-region']").GetAttribute("aria-live").ShouldBeNull();
        cut.Markup.ShouldNotContain("/api/tenants", Case.Insensitive);
        cut.Markup.ShouldNotContain("/api/users", Case.Insensitive);
        cut.Markup.ShouldNotContain("tenant ownership", Case.Insensitive);
        cut.Markup.ShouldContain("data-testid=\"tenants-global-admins-list\"");
    }

    [Fact]
    public void Tenant_owner_without_platform_authority_gets_fail_closed_without_querying_gateway()
    {
        var gateway = new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("hidden-admin", TenantFreshnessState.Current)],
            null,
            false,
            "\"etag\"",
            TenantFreshnessState.Current));
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Indeterminate));
        Services.AddSingleton<ITenantQueryGateway>(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        gateway.GlobalAdministratorCalls.ShouldBe(0);
        cut.Find("[data-testid='tenants-global-admins-unavailable']").GetAttribute("role").ShouldBe("alert");
        cut.Find("[data-testid='tenants-global-admins-live-region']").TextContent.ShouldContain("fails closed");
        cut.Markup.ShouldNotContain("hidden-admin");
        cut.Markup.ShouldNotContain("tenants-global-admins-list");
        cut.Markup.ShouldNotContain("success", Case.Insensitive);
    }

    [Theory]
    [InlineData(GlobalAdministratorsSurfaceKind.Stale, TenantFreshnessState.Stale, "freshness")]
    [InlineData(GlobalAdministratorsSurfaceKind.Degraded, TenantFreshnessState.Unknown, "freshness")]
    public void Stale_or_degraded_review_surface_keeps_rows_visible_and_actions_unavailable(
        GlobalAdministratorsSurfaceKind kind,
        TenantFreshnessState freshness,
        string expectedReason)
    {
        GlobalAdministratorsSnapshot snapshot = kind is GlobalAdministratorsSurfaceKind.Stale
            ? GlobalAdministratorsSnapshot.Stale([new GlobalAdministratorRow("admin-1", freshness)], null, false, "\"etag\"")
            : GlobalAdministratorsSnapshot.Degraded([new GlobalAdministratorRow("admin-1", freshness)], GlobalAdministratorsReason.ProjectionDegraded, "\"etag\"");
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(snapshot));
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.Find("[data-testid='tenants-global-admins-list']");
        cut.Find("[data-testid='tenants-global-admins-row']").TextContent.ShouldContain("admin-1");
        cut.Find("[data-testid='tenants-global-admins-live-region']").TextContent.ShouldContain(expectedReason, Case.Insensitive);
        cut.Find("[data-testid='tenants-global-admins-grant-reason']").TextContent.ShouldContain("freshness", Case.Insensitive);
        cut.Find("[data-testid='tenants-global-admins-remove-reason']").TextContent.ShouldContain("freshness", Case.Insensitive);
    }

    [Theory]
    [InlineData(GlobalAdministratorsSurfaceKind.Empty, "tenants-global-admins-empty", "No global administrators")]
    [InlineData(GlobalAdministratorsSurfaceKind.Invalid, "tenants-global-admins-invalid", "Invalid global administrator page")]
    [InlineData(GlobalAdministratorsSurfaceKind.Unavailable, "tenants-global-admins-unavailable", "Global administrator data unavailable")]
    public void Empty_invalid_and_unavailable_states_do_not_render_false_success_or_hidden_rows(
        GlobalAdministratorsSurfaceKind kind,
        string expectedTestId,
        string expectedCopy)
    {
        GlobalAdministratorsSnapshot snapshot = kind switch
        {
            GlobalAdministratorsSurfaceKind.Empty => GlobalAdministratorsSnapshot.Empty(
                isAuthorizationScoped: true,
                TenantFreshnessState.Current,
                "\"empty\""),
            GlobalAdministratorsSurfaceKind.Invalid => GlobalAdministratorsSnapshot.Invalid(),
            GlobalAdministratorsSurfaceKind.Unavailable => GlobalAdministratorsSnapshot.Unavailable(),
            _ => throw new InvalidOperationException($"Unsupported state {kind}."),
        };
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(snapshot));
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.Find($"[data-testid='{expectedTestId}']").TextContent.ShouldContain(expectedCopy);
        cut.Markup.ShouldNotContain("tenants-global-admins-row");
        cut.Markup.ShouldNotContain("hidden-admin", Case.Insensitive);
        cut.Markup.ShouldNotContain("success", Case.Insensitive);
        cut.Markup.ShouldNotContain("access_token", Case.Insensitive);
    }

    [Fact]
    public void Refresh_reuses_etag_and_preserves_previous_snapshot_for_server_side_bff_query()
    {
        var gateway = new StubTenantQueryGateway(
            GlobalAdministratorsSnapshot.Ready(
                [new GlobalAdministratorRow("admin-1", TenantFreshnessState.Current)],
                nextCursor: null,
                hasMore: false,
                eTag: "\"etag-1\"",
                freshness: TenantFreshnessState.Current),
            GlobalAdministratorsSnapshot.Ready(
                [new GlobalAdministratorRow("admin-1", TenantFreshnessState.Current)],
                nextCursor: null,
                hasMore: false,
                eTag: "\"etag-1\"",
                freshness: TenantFreshnessState.Current));
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.Find("[data-testid='tenants-global-admins-refresh']").Click();

        gateway.GlobalAdministratorCalls.ShouldBe(2);
        gateway.Requests[0].ETag.ShouldBeNull();
        gateway.Requests[1].ETag.ShouldBe("\"etag-1\"");
        gateway.PreviousSnapshots[1].ShouldNotBeNull().Rows.ShouldHaveSingleItem().UserId.ShouldBe("admin-1");
        cut.Find("[data-testid='tenants-global-admins-user-id']").TextContent.ShouldBe("admin-1");
    }

    [Fact]
    public void Next_page_uses_protected_cursor_without_offset_or_tenant_substitute_markers()
    {
        var gateway = new StubTenantQueryGateway(
            GlobalAdministratorsSnapshot.Ready(
                [new GlobalAdministratorRow("admin-1", TenantFreshnessState.Current)],
                nextCursor: "protected-next-cursor",
                hasMore: true,
                eTag: "\"etag-1\"",
                freshness: TenantFreshnessState.Current),
            GlobalAdministratorsSnapshot.Ready(
                [new GlobalAdministratorRow("admin-2", TenantFreshnessState.Current)],
                nextCursor: null,
                hasMore: false,
                eTag: "\"etag-2\"",
                freshness: TenantFreshnessState.Current));
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.Find("[data-testid='tenants-global-admins-next']").Click();

        gateway.GlobalAdministratorCalls.ShouldBe(2);
        gateway.Requests[0].Cursor.ShouldBeNull();
        gateway.Requests[0].PageSize.ShouldBe(20);
        gateway.Requests[1].Cursor.ShouldBe("protected-next-cursor");
        gateway.Requests[1].ETag.ShouldBeNull();
        cut.Find("[data-testid='tenants-global-admins-user-id']").TextContent.ShouldBe("admin-2");
        cut.Markup.ShouldNotContain("offset", Case.Insensitive);
        cut.Markup.ShouldNotContain("/api/tenants", Case.Insensitive);
        cut.Markup.ShouldNotContain("/api/users", Case.Insensitive);
    }

    [Fact]
    public void Grant_flow_renders_fixed_scope_form_without_tenant_membership_inputs()
    {
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("admin-1", TenantFreshnessState.Current)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"etag\"",
            freshness: TenantFreshnessState.Current)));
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.Find("[data-testid='tenants-global-admin-grant']");
        cut.Find("[data-testid='tenants-global-admin-grant-user-id']");
        cut.Find("[data-testid='tenants-global-admin-grant-scope']").TextContent.ShouldContain("system");
        cut.Find("[data-testid='tenants-global-admin-grant-scope']").TextContent.ShouldContain("global-administrators");
        cut.Markup.ShouldNotContain("TenantRole", Case.Insensitive);
        cut.Markup.ShouldNotContain("tenant-member", Case.Insensitive);
        cut.Markup.ShouldNotContain("member table", Case.Insensitive);
    }

    [Fact]
    public void Blank_keyboard_form_submission_keeps_command_local_and_focuses_user_id_recovery()
    {
        var commandGateway = new StubTenantCommandGateway();
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("admin-1", TenantFreshnessState.Current)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"etag\"",
            freshness: TenantFreshnessState.Current)));
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.Find("[data-testid='tenants-global-admin-grant-form']").Submit();

        commandGateway.SetGlobalAdministratorCalls.ShouldBe(0);
        cut.Find("[data-testid='tenants-global-admin-grant-submit']").GetAttribute("type").ShouldBe("submit");
        cut.Find("[data-testid='tenants-global-admin-grant-validation']").TextContent.ShouldContain("User id is required");
        cut.Find("[data-testid='tenants-global-admin-grant-validation']").GetAttribute("role").ShouldBe("alert");
        cut.Find("[data-testid='tenants-global-admin-grant-user-id']")
            .GetAttribute("aria-describedby")
            .ShouldNotBeNull()
            .ShouldContain("tenants-global-admin-grant-validation");
        cut.Find("[data-testid='tenants-global-admin-grant-live-region']").GetAttribute("aria-live").ShouldBe("polite");
    }

    [Fact]
    public void Grant_submission_confirms_only_after_projection_requery_contains_target_user()
    {
        var queryGateway = new StubTenantQueryGateway(
            GlobalAdministratorsSnapshot.Ready(
                [new GlobalAdministratorRow("admin-1", TenantFreshnessState.Current)],
                null,
                false,
                "\"etag-1\"",
                TenantFreshnessState.Current),
            GlobalAdministratorsSnapshot.Ready(
                [
                    new GlobalAdministratorRow("admin-1", TenantFreshnessState.Current),
                    new GlobalAdministratorRow("target-user", TenantFreshnessState.Current),
                ],
                null,
                false,
                "\"etag-2\"",
                TenantFreshnessState.Current));
        var commandGateway = new StubTenantCommandGateway(
            TenantCommandSubmissionResult.Accepted("message-grant", "correlation-grant"),
            new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1));
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(queryGateway);
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.Find("[data-testid='tenants-global-admin-grant-user-id']").Input("target-user");
        cut.Find("[data-testid='tenants-global-admin-grant-form']").Submit();

        cut.WaitForAssertion(() =>
        {
            commandGateway.SetGlobalAdministratorCalls.ShouldBe(1);
            commandGateway.Requests.ShouldHaveSingleItem().UserId.ShouldBe("target-user");
            queryGateway.GlobalAdministratorCalls.ShouldBe(2);
            cut.Find("[data-testid='tenants-global-admin-grant-state']").TextContent.ShouldContain("Projection confirmed");
            cut.Find("[data-testid='tenants-global-admin-grant-live-region']").GetAttribute("aria-live").ShouldBe("polite");
            cut.FindAll("[data-testid='tenants-global-admins-user-id']").Select(static element => element.TextContent)
                .ShouldContain("target-user");
        });
    }

    [Fact]
    public void Command_or_read_surface_unavailable_blocks_grant_without_command_submission()
    {
        var commandGateway = new StubTenantCommandGateway();
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(
            TenantLifecycleAuthorizationReflectionState.Authorized,
            isReadSurfaceConnected: true,
            isCommandSurfaceConnected: false));
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("admin-1", TenantFreshnessState.Current)],
            null,
            false,
            "\"etag\"",
            TenantFreshnessState.Current)));
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.Find("[data-testid='tenants-global-admin-grant-user-id']").Input("target-user");
        cut.Find("[data-testid='tenants-global-admin-grant-form']").Submit();

        commandGateway.SetGlobalAdministratorCalls.ShouldBe(0);
        cut.Find("[data-testid='tenants-global-admin-grant-submit']").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("[data-testid='tenants-global-admin-grant-unavailable-reason']").TextContent.ShouldContain("command surface");
        cut.Find("[data-testid='tenants-global-admin-grant-state']").TextContent.ShouldContain("could not be verified");
        cut.Find("[data-testid='tenants-global-admin-grant-audit-state']").TextContent.ShouldContain("Audit support");
        cut.Find("[data-testid='tenants-global-admin-grant-live-region']").GetAttribute("aria-live").ShouldBe("assertive");
        cut.Markup.ShouldNotContain("access_token", Case.Insensitive);
        cut.Markup.ShouldNotContain("correlation-", Case.Insensitive);
    }

    [Fact]
    public void Completed_grant_without_projection_evidence_is_unable_to_verify_and_not_optimistic()
    {
        var queryGateway = new StubTenantQueryGateway(
            GlobalAdministratorsSnapshot.Ready(
                [new GlobalAdministratorRow("admin-1", TenantFreshnessState.Current)],
                null,
                false,
                "\"etag-1\"",
                TenantFreshnessState.Current),
            GlobalAdministratorsSnapshot.Ready(
                [new GlobalAdministratorRow("admin-1", TenantFreshnessState.Current)],
                null,
                false,
                "\"etag-2\"",
                TenantFreshnessState.Current));
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(queryGateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway(
            TenantCommandSubmissionResult.Accepted("message-grant", "correlation-grant"),
            new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1)));
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.Find("[data-testid='tenants-global-admin-grant-user-id']").Input("target-user");
        cut.Find("[data-testid='tenants-global-admin-grant-form']").Submit();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='tenants-global-admin-grant-state']").TextContent.ShouldContain("could not be verified");
            cut.Find("[data-testid='tenants-global-admin-grant-safe-message']").TextContent.ShouldContain("did not confirm");
            cut.Find("[data-testid='tenants-global-admin-grant-live-region']").GetAttribute("aria-live").ShouldBe("assertive");
            cut.FindAll("[data-testid='tenants-global-admins-user-id']").Select(static element => element.TextContent)
                .ShouldNotContain("target-user");
        });
    }

    [Theory]
    [InlineData(CommandStatus.PublishFailed, "degraded", "Audit evidence is delayed.")]
    [InlineData(CommandStatus.TimedOut, "could not be verified", "Audit evidence is delayed.")]
    public void Terminal_status_without_projection_confirmation_stays_distinct_and_assertive(
        CommandStatus status,
        string expectedStateText,
        string expectedAuditText)
    {
        var queryGateway = new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("admin-1", TenantFreshnessState.Current)],
            null,
            false,
            "\"etag\"",
            TenantFreshnessState.Current));
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(queryGateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway(
            TenantCommandSubmissionResult.Accepted("message-grant", "correlation-grant"),
            new TenantCommandStatusResult(status, "Status remained support-safe.")));
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.Find("[data-testid='tenants-global-admin-grant-user-id']").Input("target-user");
        cut.Find("[data-testid='tenants-global-admin-grant-form']").Submit();

        cut.WaitForAssertion(() =>
        {
            queryGateway.GlobalAdministratorCalls.ShouldBe(1);
            cut.Find("[data-testid='tenants-global-admin-grant-state']").TextContent.ShouldContain(expectedStateText, Case.Insensitive);
            cut.Find("[data-testid='tenants-global-admin-grant-audit-state']").TextContent.ShouldContain(expectedAuditText);
            cut.Find("[data-testid='tenants-global-admin-grant-safe-message']").TextContent.ShouldContain("support-safe");
            cut.Find("[data-testid='tenants-global-admin-grant-lifecycle']").GetAttribute("role").ShouldBe("alert");
            cut.Find("[data-testid='tenants-global-admin-grant-live-region']").GetAttribute("aria-live").ShouldBe("assertive");
            cut.FindAll("[data-testid='tenants-global-admins-user-id']").Select(static element => element.TextContent)
                .ShouldNotContain("target-user");
        });
    }

    [Fact]
    public void Already_global_administrator_rejection_stays_rejected_without_success_copy()
    {
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("existing-admin", TenantFreshnessState.Current)],
            null,
            false,
            "\"etag\"",
            TenantFreshnessState.Current)));
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway(
            TenantCommandSubmissionResult.Rejected(
                "This user is already a global administrator. Refresh the platform authority projection before trying another action.",
                "GlobalAdministratorAlreadyExists")));
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.Find("[data-testid='tenants-global-admin-grant-user-id']").Input("existing-admin");
        cut.Find("[data-testid='tenants-global-admin-grant-form']").Submit();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='tenants-global-admin-grant-state']").TextContent.ShouldContain("rejected", Case.Insensitive);
            cut.Find("[data-testid='tenants-global-admin-grant-safe-message']").TextContent.ShouldContain("already a global administrator");
            cut.Markup.ShouldNotContain("AlreadyApplied");
            cut.Markup.ShouldNotContain("success", Case.Insensitive);
        });
    }

    [Fact]
    public void Insufficient_permissions_rejection_uses_safe_platform_governance_copy()
    {
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("admin-1", TenantFreshnessState.Current)],
            null,
            false,
            "\"etag\"",
            TenantFreshnessState.Current)));
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway(
            TenantCommandSubmissionResult.Rejected(
                "The caller is not authorized for platform governance changes.",
                "InsufficientPermissions")));
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.Find("[data-testid='tenants-global-admin-grant-user-id']").Input("target-user");
        cut.Find("[data-testid='tenants-global-admin-grant-form']").Submit();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='tenants-global-admin-grant-state']").TextContent.ShouldContain("rejected", Case.Insensitive);
            cut.Find("[data-testid='tenants-global-admin-grant-safe-message']").TextContent.ShouldContain("platform governance");
            cut.Find("[data-testid='tenants-global-admin-grant-live-region']").GetAttribute("aria-live").ShouldBe("assertive");
            cut.Markup.ShouldNotContain("tenant command succeeded", Case.Insensitive);
            cut.Markup.ShouldNotContain("target-user is now", Case.Insensitive);
            cut.Markup.ShouldNotContain("correlation-", Case.Insensitive);
        });
    }

    [Fact]
    public void Processing_grant_keeps_one_at_a_time_lock_and_remove_placeholder_unavailable()
    {
        var queryGateway = new StubTenantQueryGateway(
            GlobalAdministratorsSnapshot.Ready(
                [new GlobalAdministratorRow("admin-1", TenantFreshnessState.Current)],
                null,
                false,
                "\"etag-1\"",
                TenantFreshnessState.Current),
            GlobalAdministratorsSnapshot.Ready(
                [new GlobalAdministratorRow("admin-1", TenantFreshnessState.Current)],
                null,
                false,
                "\"etag-2\"",
                TenantFreshnessState.Current));
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(queryGateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway(
            TenantCommandSubmissionResult.Accepted("message-grant", "correlation-grant"),
            new TenantCommandStatusResult(CommandStatus.Processing)));
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.Find("[data-testid='tenants-global-admin-grant-user-id']").Input("target-user");
        cut.Find("[data-testid='tenants-global-admin-grant-form']").Submit();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='tenants-global-admin-grant-state']").TextContent.ShouldContain("accepted", Case.Insensitive);
            cut.Find("[data-testid='tenants-global-admin-grant-submit']").HasAttribute("disabled").ShouldBeTrue();
            cut.Find("[data-testid='tenants-global-admins-remove-reason']").TextContent.ShouldContain("in flight");
        });
    }

    [Fact]
    public void Grant_resources_and_styles_cover_accessible_forced_colors_support_safe_states()
    {
        string projectRoot = ProjectRoot();
        string resourceRoot = Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Resources");
        string styles = File.ReadAllText(
            Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Components", "Pages", "GlobalAdministratorsPage.razor.css"));
        HashSet<string> englishKeys = ResourceKeys(Path.Combine(resourceRoot, "TenantsResources.resx"), "Tenants.GlobalAdministrators.Grant.");
        HashSet<string> frenchKeys = ResourceKeys(Path.Combine(resourceRoot, "TenantsResources.fr.resx"), "Tenants.GlobalAdministrators.Grant.");

        englishKeys.ShouldBe(frenchKeys);
        englishKeys.ShouldContain("Tenants.GlobalAdministrators.Grant.State.Confirmed");
        englishKeys.ShouldContain("Tenants.GlobalAdministrators.Grant.State.Rejected");
        englishKeys.ShouldContain("Tenants.GlobalAdministrators.Grant.State.UnableToVerify");
        englishKeys.ShouldContain("Tenants.GlobalAdministrators.Grant.Audit.AuditDelayed");
        englishKeys.ShouldContain("Tenants.GlobalAdministrators.Grant.Unavailable.CommandSurface");
        styles.ShouldContain("@media (forced-colors: active)");
        styles.ShouldContain(".global-admins__grant-lifecycle:focus-visible");
        styles.ShouldContain(".global-admins__grant-state-symbol");
        styles.ShouldContain("overflow-wrap: anywhere");
    }

    [Fact]
    public void Route_and_workspace_keep_users_contextual_and_global_admins_top_level()
    {
        string projectRoot = ProjectRoot();
        string page = File.ReadAllText(
            Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Components", "Pages", "GlobalAdministratorsPage.razor"));
        string workspace = File.ReadAllText(
            Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Components", "Pages", "TenantsWorkspace.razor"));
        string detail = File.ReadAllText(
            Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Components", "Pages", "TenantDetailPage.razor"));

        page.ShouldContain("@page \"/global-administrators\"");
        workspace.ShouldContain("href=\"/tenants/my\"");
        workspace.ShouldContain("href=\"/tenants/users\"");
        workspace.ShouldNotContain("href=\"/users\"");
        detail.ShouldContain("returnUrl.StartsWith(\"/tenants\", StringComparison.Ordinal)");
    }

    private static string ProjectRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static HashSet<string> ResourceKeys(string path, string prefix)
        => System.Xml.Linq.XDocument
            .Load(path)
            .Descendants("data")
            .Select(static element => element.Attribute("name")?.Value)
            .Where(name => name is not null && name.StartsWith(prefix, StringComparison.Ordinal))
            .Select(name => name!)
            .ToHashSet(StringComparer.Ordinal);

    private sealed class StubTenantsBffComposition(
        TenantLifecycleAuthorizationReflectionState reflection,
        bool isReadSurfaceConnected = true,
        bool isCommandSurfaceConnected = true) : ITenantsBffComposition
    {
        public bool IsReadSurfaceConnected => isReadSurfaceConnected;

        public bool IsCommandSurfaceConnected => isCommandSurfaceConnected;

        public TenantLifecycleAuthorizationReflectionState GlobalAdministratorsAuthorizationReflection => reflection;
    }

    private sealed class StubTenantQueryGateway(params GlobalAdministratorsSnapshot[] snapshots) : ITenantQueryGateway
    {
        private readonly Queue<GlobalAdministratorsSnapshot> _snapshots = new(snapshots);

        public int GlobalAdministratorCalls { get; private set; }

        public List<GlobalAdministratorsRequest> Requests { get; } = [];

        public List<GlobalAdministratorsSnapshot?> PreviousSnapshots { get; } = [];

        public Task<TenantDetailSnapshot> GetTenantAsync(
            TenantDetailRequest request,
            TenantDetailSnapshot? previous,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantListSnapshot> ListTenantsAsync(
            TenantListRequest request,
            TenantListSnapshot? previous,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<UserTenantMembershipSnapshot> GetMyTenantsAsync(
            UserTenantMembershipRequest request,
            UserTenantMembershipSnapshot? previous,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<UserTenantMembershipSnapshot> GetUserTenantsAsync(
            UserTenantMembershipRequest request,
            UserTenantMembershipSnapshot? previous,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<GlobalAdministratorsSnapshot> GetGlobalAdministratorsAsync(
            GlobalAdministratorsRequest request,
            GlobalAdministratorsSnapshot? previous,
            CancellationToken cancellationToken = default)
        {
            GlobalAdministratorCalls++;
            Requests.Add(request);
            PreviousSnapshots.Add(previous);
            return Task.FromResult(_snapshots.Dequeue());
        }

        public Task<TenantAuditSnapshot> GetTenantAuditAsync(
            TenantAuditRequest request,
            TenantAuditSnapshot? previous,
            CancellationToken cancellationToken = default)
            => Task.FromResult(TenantAuditSnapshot.Unavailable(request));
    }

    private sealed class StubTenantCommandGateway(
        TenantCommandSubmissionResult? submission = null,
        params TenantCommandStatusResult[] statuses) : ITenantCommandGateway
    {
        private readonly Queue<TenantCommandStatusResult> _statuses = new(statuses);

        public int SetGlobalAdministratorCalls { get; private set; }

        public List<SetGlobalAdministratorCommandRequest> Requests { get; } = [];

        public Task<TenantCommandSubmissionResult> CreateTenantAsync(
            CreateTenantCommandRequest request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantCommandSubmissionResult> AddUserToTenantAsync(
            AddUserToTenantCommandRequest request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantCommandSubmissionResult> ChangeUserRoleAsync(
            ChangeUserRoleCommandRequest request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantCommandSubmissionResult> RemoveUserFromTenantAsync(
            RemoveUserFromTenantCommandRequest request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantCommandSubmissionResult> UpdateTenantAsync(
            UpdateTenantCommandRequest request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantCommandSubmissionResult> SetTenantConfigurationAsync(
            SetTenantConfigurationCommandRequest request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantCommandSubmissionResult> RemoveTenantConfigurationAsync(
            RemoveTenantConfigurationCommandRequest request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantCommandSubmissionResult> SetGlobalAdministratorAsync(
            SetGlobalAdministratorCommandRequest request,
            CancellationToken cancellationToken = default)
        {
            SetGlobalAdministratorCalls++;
            Requests.Add(request);
            return Task.FromResult(submission ?? TenantCommandSubmissionResult.Failed("No command response configured."));
        }

        public Task<TenantCommandSubmissionResult> EnableTenantAsync(
            TenantLifecycleCommandRequest request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantCommandSubmissionResult> DisableTenantAsync(
            TenantLifecycleCommandRequest request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantCommandStatusResult> GetStatusAsync(
            TenantCommandTrackingHandle handle,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_statuses.Count == 0
                ? TenantCommandStatusResult.Unknown("No command status configured.")
                : _statuses.Dequeue());
    }

    private sealed class StubTenantsLocalizer : IStringLocalizer<TenantsResources>
    {
        private static readonly Dictionary<string, string> Values = new(StringComparer.Ordinal)
        {
            ["Tenants.Copy.Action"] = "Copy",
            ["Tenants.Copy.Feedback.Copied"] = "Copied",
            ["Tenants.GlobalAdministrators.Action.Unavailable.Freshness"] = "Unavailable until projection freshness is current.",
            ["Tenants.GlobalAdministrators.Action.Unavailable.ReadOnly"] = "Unavailable in this read-only review.",
            ["Tenants.GlobalAdministrators.Aggregate.Domain.Label"] = "Domain",
            ["Tenants.GlobalAdministrators.Aggregate.Domain.Value"] = "global-administrators",
            ["Tenants.GlobalAdministrators.Aggregate.Id.Label"] = "Aggregate id",
            ["Tenants.GlobalAdministrators.Aggregate.Id.Value"] = "global-administrators",
            ["Tenants.GlobalAdministrators.Aggregate.Tenant.Label"] = "Tenant scope",
            ["Tenants.GlobalAdministrators.Aggregate.Tenant.Value"] = "system",
            ["Tenants.GlobalAdministrators.Column.Actions"] = "Grant/remove availability",
            ["Tenants.GlobalAdministrators.Column.Freshness"] = "Freshness",
            ["Tenants.GlobalAdministrators.Column.Identity"] = "Administrator identity",
            ["Tenants.GlobalAdministrators.Column.Scope"] = "Authority scope",
            ["Tenants.GlobalAdministrators.Copy.UserId"] = "Copy global administrator identifier {0}",
            ["Tenants.GlobalAdministrators.Description"] = "Review platform-level administrators from the fixed global-administrators authority scope.",
            ["Tenants.GlobalAdministrators.Eyebrow"] = "Platform governance",
            ["Tenants.GlobalAdministrators.Freshness.Current"] = "Current",
            ["Tenants.GlobalAdministrators.Freshness.Stale"] = "Stale",
            ["Tenants.GlobalAdministrators.Freshness.Unknown"] = "Unknown",
            ["Tenants.GlobalAdministrators.Grant.Audit.AuditDelayed"] = "Audit evidence is delayed.",
            ["Tenants.GlobalAdministrators.Grant.Audit.AuditPending"] = "Audit evidence is pending.",
            ["Tenants.GlobalAdministrators.Grant.Audit.AuditUnavailable"] = "Audit evidence is unavailable.",
            ["Tenants.GlobalAdministrators.Grant.Audit.MissingSupport"] = "Audit support is not available.",
            ["Tenants.GlobalAdministrators.Grant.Audit.NotStarted"] = "No audit evidence is available before command submission.",
            ["Tenants.GlobalAdministrators.Grant.Available"] = "Grant is available from the confirmed platform authority projection.",
            ["Tenants.GlobalAdministrators.Grant.Cancel"] = "Cancel",
            ["Tenants.GlobalAdministrators.Grant.Description"] = "Grant platform authority in tenant system, domain global-administrators, aggregate global-administrators. Completion requires projection confirmation.",
            ["Tenants.GlobalAdministrators.Grant.Lifecycle.Title"] = "Grant lifecycle",
            ["Tenants.GlobalAdministrators.Grant.Refresh"] = "Refresh status",
            ["Tenants.GlobalAdministrators.Grant.State.Accepted"] = "Command accepted; projection confirmation is still required.",
            ["Tenants.GlobalAdministrators.Grant.State.AlreadyApplied"] = "Already-applied is not used for global administrator grants.",
            ["Tenants.GlobalAdministrators.Grant.State.Confirmed"] = "Projection confirmed the target user in the fixed global-administrators scope.",
            ["Tenants.GlobalAdministrators.Grant.State.Degraded"] = "Grant verification is degraded.",
            ["Tenants.GlobalAdministrators.Grant.State.DuplicatePrevented"] = "A concurrent grant command was prevented.",
            ["Tenants.GlobalAdministrators.Grant.State.Failed"] = "Grant command failed before it could be verified.",
            ["Tenants.GlobalAdministrators.Grant.State.Idle"] = "No global administrator grant command has been submitted.",
            ["Tenants.GlobalAdministrators.Grant.State.Previewed"] = "Grant intent is previewed but not submitted.",
            ["Tenants.GlobalAdministrators.Grant.State.ProjectionPending"] = "Projection pending; the target user is not confirmed as a global administrator yet.",
            ["Tenants.GlobalAdministrators.Grant.State.Rejected"] = "Grant command was rejected.",
            ["Tenants.GlobalAdministrators.Grant.State.RequestSent"] = "Grant command request was sent.",
            ["Tenants.GlobalAdministrators.Grant.State.UnableToVerify"] = "Grant command could not be verified from command status and projection evidence.",
            ["Tenants.GlobalAdministrators.Grant.Submit"] = "Grant global administrator",
            ["Tenants.GlobalAdministrators.Grant.Title"] = "Grant global administrator",
            ["Tenants.GlobalAdministrators.Grant.Unavailable.Authorization"] = "Platform authority is not confirmed, so grant fails closed without revealing administrator data.",
            ["Tenants.GlobalAdministrators.Grant.Unavailable.CommandSurface"] = "The command surface is unavailable for platform governance changes.",
            ["Tenants.GlobalAdministrators.Grant.Unavailable.Freshness"] = "Refresh projection freshness before granting platform authority.",
            ["Tenants.GlobalAdministrators.Grant.Unavailable.InFlight"] = "Another platform authority command is in flight.",
            ["Tenants.GlobalAdministrators.Grant.Unavailable.ReadSurface"] = "The global administrator read projection must be available before grant can be submitted.",
            ["Tenants.GlobalAdministrators.Grant.Unavailable.RemoveDeferred"] = "Remove global administrator is handled by a separate guarded flow.",
            ["Tenants.GlobalAdministrators.Grant.UserId.Help"] = "Enter the literal caller-supplied user id. It is not parsed as a tenant member, GUID, or ULID.",
            ["Tenants.GlobalAdministrators.Grant.UserId.Label"] = "User id",
            ["Tenants.GlobalAdministrators.Grant.Validation.UserIdRequired"] = "User id is required before granting global administrator authority.",
            ["Tenants.GlobalAdministrators.Identity.Accessible"] = "Global administrator identifier {0}",
            ["Tenants.GlobalAdministrators.List.Title"] = "Current global administrators",
            ["Tenants.GlobalAdministrators.Next"] = "Next",
            ["Tenants.GlobalAdministrators.PaginationLabel"] = "Global administrator pages",
            ["Tenants.GlobalAdministrators.Previous"] = "Previous",
            ["Tenants.GlobalAdministrators.Refresh"] = "Refresh",
            ["Tenants.GlobalAdministrators.RestrictedTitle"] = "Platform area unavailable",
            ["Tenants.GlobalAdministrators.Row.Scope"] = "Platform authority, not tenant owner",
            ["Tenants.GlobalAdministrators.Scope.Message"] = "This surface uses the singleton platform authority aggregate and never substitutes tenant membership data.",
            ["Tenants.GlobalAdministrators.Scope.Title"] = "Fixed aggregate scope",
            ["Tenants.GlobalAdministrators.State.Degraded.Message"] = "Projection freshness is degraded. Last confirmed administrators remain visible.",
            ["Tenants.GlobalAdministrators.State.Degraded.Title"] = "Global administrator data degraded",
            ["Tenants.GlobalAdministrators.State.Empty.Message"] = "No global administrators were returned.",
            ["Tenants.GlobalAdministrators.State.Empty.Title"] = "No global administrators returned",
            ["Tenants.GlobalAdministrators.State.Invalid.Message"] = "The requested page cursor is invalid.",
            ["Tenants.GlobalAdministrators.State.Invalid.Title"] = "Invalid global administrator page",
            ["Tenants.GlobalAdministrators.State.Loading.Message"] = "Loading global administrators.",
            ["Tenants.GlobalAdministrators.State.Loading.Title"] = "Loading global administrators",
            ["Tenants.GlobalAdministrators.State.Ready.Message"] = "Global administrators are loaded.",
            ["Tenants.GlobalAdministrators.State.Ready.Title"] = "Global administrators loaded",
            ["Tenants.GlobalAdministrators.State.Stale.Message"] = "Projection freshness is stale. Last confirmed administrators remain visible.",
            ["Tenants.GlobalAdministrators.State.Stale.Title"] = "Global administrator data stale",
            ["Tenants.GlobalAdministrators.State.Unauthorized.Message"] = "Platform authority was not confirmed. The area fails closed and does not reveal administrator data.",
            ["Tenants.GlobalAdministrators.State.Unauthorized.Title"] = "Platform area unavailable",
            ["Tenants.GlobalAdministrators.State.Unavailable.Message"] = "The global administrator read surface is unavailable.",
            ["Tenants.GlobalAdministrators.State.Unavailable.Title"] = "Global administrator data unavailable",
            ["Tenants.GlobalAdministrators.Title"] = "Global Administrators",
        };

        public LocalizedString this[string name]
            => new(name, Values.TryGetValue(name, out string? value) ? value : name);

        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                string value = Values.TryGetValue(name, out string? template) ? template : name;
                return new(name, string.Format(CultureInfo.CurrentCulture, value, arguments));
            }
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => Values.Select(static v => new LocalizedString(v.Key, v.Value));
    }
}
