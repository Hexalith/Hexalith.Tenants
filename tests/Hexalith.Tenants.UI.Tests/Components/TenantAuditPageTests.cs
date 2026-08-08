using System.Globalization;
using System.Reflection;
using System.Collections;
using System.Resources;
using System.Xml.Linq;

using AngleSharp.Dom;

using Bunit;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Pages;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State;
using Hexalith.Tenants.UI.State.GlobalAdministrators;
using Hexalith.Tenants.UI.State.TenantUsers;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.UI.State.UserTenants;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class TenantAuditPageTests : BunitContext
{
    [Fact]
    public async Task Notification_refreshing_affordance_retains_rows_until_the_authoritative_read_completes()
    {
        TenantAuditSnapshot confirmed = ReadySnapshot([Row("event-confirmed", AuditEventCategory.Access)]);
        StubTenantQueryGateway gateway = RegisterServices(confirmed);
        IProjectionSubscription subscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        Services.AddSingleton(subscription);
        Services.AddSingleton(notifier);
        Services.AddScoped<TenantReadRefreshSubscription>();
        var pending = new TaskCompletionSource<TenantAuditSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);

        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, "tenant.alpha"));
        cut.Find("[data-testid='tenants-audit-projection-lifecycle-status']")
            .GetAttribute("role").ShouldBe("status");
        cut.Find("[data-testid='tenants-audit-projection-lifecycle-status-badge']")
            .GetAttribute("class").ShouldNotBeNull().ShouldContain("projection-lifecycle-badge--current");
        cut.Find("[data-testid='tenants-audit-projection-lifecycle-status-badge']")
            .TextContent.Trim().ShouldBe("Current");
        cut.Find("[data-testid='tenants-audit-row-projection-lifecycle']")
            .GetAttribute("class").ShouldNotBeNull().ShouldContain("projection-lifecycle-badge--current");
        cut.Find("[data-testid='tenants-audit-row-projection-lifecycle']")
            .TextContent.Trim().ShouldBe("Current");
        await subscription.Received(1).SubscribeAsync(
            GetTenantAuditQuery.ProjectionType,
            "tenant.alpha",
            Arg.Any<CancellationToken>());
        gateway.QueueResponse(pending.Task);

        notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>(
            GetTenantAuditQuery.ProjectionType,
            "tenant.alpha");

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='tenants-audit-notification-refreshing']")
                .GetAttribute("role").ShouldBe("status");
            cut.Find("[data-testid='tenants-audit-row']")
                .GetAttribute("data-audit-reference").ShouldBe("event-confirmed");
        });

        pending.SetResult(ReadySnapshot([Row("event-refreshed", AuditEventCategory.Access)]));
        cut.WaitForAssertion(() =>
        {
            cut.FindAll("[data-testid='tenants-audit-notification-refreshing']").ShouldBeEmpty();
            cut.Find("[data-testid='tenants-audit-row']")
                .GetAttribute("data-audit-reference").ShouldBe("event-refreshed");
        });
    }

    [Fact]
    public async Task Tenant_rebinding_disposes_the_previous_subscription_and_only_the_new_scope_refreshes()
    {
        StubTenantQueryGateway gateway = RegisterServices(
            ReadySnapshot([Row("event-alpha", AuditEventCategory.Access)]),
            ReadySnapshot([Row("event-beta", AuditEventCategory.Access)]),
            ReadySnapshot([Row("event-beta-refreshed", AuditEventCategory.Access)]));
        IProjectionSubscription subscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        Services.AddSingleton(subscription);
        Services.AddSingleton(notifier);
        Services.AddScoped<TenantReadRefreshSubscription>();

        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, "tenant.alpha"));
        await subscription.Received(1).SubscribeAsync(
            GetTenantAuditQuery.ProjectionType,
            "tenant.alpha",
            Arg.Any<CancellationToken>());

        cut.Render(parameters => parameters.Add(p => p.TenantId, "tenant.beta"));

        cut.WaitForAssertion(() =>
        {
            gateway.Requests.Count.ShouldBe(2);
            cut.Find("[data-testid='tenants-audit-row']")
                .GetAttribute("data-audit-reference").ShouldBe("event-beta");
        });
        await subscription.Received(1).UnsubscribeAsync(
            GetTenantAuditQuery.ProjectionType,
            "tenant.alpha",
            Arg.Any<CancellationToken>());
        await subscription.Received(1).SubscribeAsync(
            GetTenantAuditQuery.ProjectionType,
            "tenant.beta",
            Arg.Any<CancellationToken>());

        notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>(
            GetTenantAuditQuery.ProjectionType,
            "tenant.alpha");

        // The stale-scope nudge must produce NO read, and the matching nudge that follows is what proves it.
        // Draining the dispatcher was not a settling point: `OnProjectionChanged` dispatches via
        // `_ = RunRefreshLoopAsync(key)`, and a fire-and-forget task whose first await is the gateway call
        // has not reached a dispatcher hop yet -- so the drain could complete before the nudge did anything,
        // and the absence assertion passed for a read that simply had not started.
        //
        // Both nudges go through the same renderer dispatcher, which preserves FIFO order, so once the
        // matching nudge's read has landed the stale one has provably been processed. The count is then
        // conclusive: it would be 4, not 3, had the stale-scope nudge issued a read of its own.
        notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>(
            GetTenantAuditQuery.ProjectionType,
            "tenant.beta");
        cut.WaitForAssertion(() =>
        {
            gateway.Requests.Count.ShouldBe(3);
            cut.Find("[data-testid='tenants-audit-row']")
                .GetAttribute("data-audit-reference").ShouldBe("event-beta-refreshed");
        });

        gateway.Requests.Count.ShouldBe(3, "The stale-scope nudge must not have issued a read of its own.");

        // Exactly one alpha read ever happened: the initial load, before the rebind. A stale-scope nudge
        // that slipped through would show up here as a second one.
        gateway.Requests.Count(request => request.TenantId == "tenant.alpha").ShouldBe(1);
    }

    [Fact]
    public async Task Tenant_arriving_during_subscription_setup_is_handed_to_the_setup_owner()
    {
        StubTenantQueryGateway gateway = RegisterServices(
            ReadySnapshot([Row("event-alpha", AuditEventCategory.Access)]),
            ReadySnapshot([Row("event-beta", AuditEventCategory.Access)]));
        IProjectionSubscription subscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        var alphaSetup = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        subscription
            .SubscribeAsync(GetTenantAuditQuery.ProjectionType, "tenant.alpha", Arg.Any<CancellationToken>())
            .Returns(alphaSetup.Task);
        subscription
            .SubscribeAsync(GetTenantAuditQuery.ProjectionType, "tenant.beta", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        Services.AddSingleton(subscription);
        Services.AddSingleton(notifier);
        Services.AddScoped<TenantReadRefreshSubscription>();

        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, "tenant.alpha"));
        await subscription.Received(1).SubscribeAsync(
            GetTenantAuditQuery.ProjectionType,
            "tenant.alpha",
            Arg.Any<CancellationToken>());

        cut.Render(parameters => parameters.Add(p => p.TenantId, "tenant.beta"));
        alphaSetup.SetResult();

        cut.WaitForAssertion(() => gateway.Requests.Count.ShouldBe(2));
        cut.WaitForAssertion(() => subscription.ReceivedCalls()
            .Count(call => string.Equals(call.GetArguments()[1] as string, "tenant.beta", StringComparison.Ordinal))
            .ShouldBe(1));
        await subscription.Received(1).SubscribeAsync(
            GetTenantAuditQuery.ProjectionType,
            "tenant.beta",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Newer_notification_refresh_rejects_a_late_cursor_result_and_preserves_cursor_history()
    {
        StubTenantQueryGateway gateway = RegisterServices(
            ReadySnapshot(
                [Row("event-first", AuditEventCategory.Access)],
                nextCursor: "protected-next",
                hasMore: true),
            ReadySnapshot([Row("event-newest", AuditEventCategory.Access)]));
        IProjectionSubscription subscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        Services.AddSingleton(subscription);
        Services.AddSingleton(notifier);
        Services.AddScoped<TenantReadRefreshSubscription>();
        var latePage = new TaskCompletionSource<TenantAuditSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);

        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, "tenant.alpha"));
        await subscription.Received(1).SubscribeAsync(
            GetTenantAuditQuery.ProjectionType,
            "tenant.alpha",
            Arg.Any<CancellationToken>());
        gateway.QueueResponse(latePage.Task);

        Task nextNavigation = cut.Find("[data-testid='tenants-audit-next']")
            .ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        cut.WaitForAssertion(() => gateway.Requests.Count.ShouldBe(2));

        notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>(
            GetTenantAuditQuery.ProjectionType,
            "tenant.alpha");
        cut.WaitForAssertion(() =>
        {
            gateway.Requests.Count.ShouldBe(3);
            cut.Find("[data-testid='tenants-audit-row']")
                .GetAttribute("data-audit-reference").ShouldBe("event-newest");
        });

        latePage.SetResult(ReadySnapshot([Row("event-late", AuditEventCategory.Access)]));
        await nextNavigation;

        cut.Find("[data-testid='tenants-audit-row']")
            .GetAttribute("data-audit-reference").ShouldBe("event-newest");
        cut.Markup.ShouldNotContain("event-late");
        gateway.Requests[1].Cursor.ShouldBe("protected-next");
        gateway.Requests[2].Cursor.ShouldBeNull();
        cut.Find("[data-testid='tenants-audit-previous']").HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void Tenant_audit_page_renders_grid_filters_paging_and_support_safe_rows()
    {
        TenantAuditSnapshot snapshot = ReadySnapshot(
            [
                Row("event-safe-reference", AuditEventCategory.Access, "userId: target-user; role: TenantReader"),
            ],
            nextCursor: "next-cursor",
            hasMore: true);
        StubTenantQueryGateway gateway = RegisterServices(snapshot);
        BunitJSModuleInterop module = JSInterop.SetupModule("./js/tenantsClipboard.js");
        JSRuntimeInvocationHandler writeHandler = module.SetupVoid("writeText", "event-safe-reference").SetVoidResult();

        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-audit-grid']");

        gateway.Requests.ShouldHaveSingleItem().TenantId.ShouldBe("tenant.alpha");
        cut.Find("[data-testid='tenants-audit-filter-category']").GetAttribute("value").ShouldBeNull();
        cut.Find("[data-testid='tenants-audit-filter-from']").GetAttribute("type").ShouldBe("datetime-local");
        cut.Find("[data-testid='tenants-audit-filter-to']").GetAttribute("type").ShouldBe("datetime-local");
        cut.Find("[data-testid='tenants-audit-refresh']").NodeName.ShouldBe("FLUENT-BUTTON");
        cut.Find("[data-testid='tenants-audit-next']").NodeName.ShouldBe("FLUENT-BUTTON");
        cut.Find("[data-testid='tenants-audit-previous']").NodeName.ShouldBe("FLUENT-BUTTON");
        cut.Find("[data-testid='tenants-audit-row']").GetAttribute("data-audit-reference").ShouldBe("event-safe-reference");
        cut.Find("[data-testid='tenants-audit-copy-reference']").GetAttribute("data-copy-kind").ShouldBe("ApprovedReference");
        cut.Find("[data-testid='tenants-audit-receipt-open']").NodeName.ShouldBe("FLUENT-BUTTON");
        cut.Markup.ShouldContain("target-user");
        cut.Markup.ShouldNotContain("raw payload", Case.Insensitive);
        cut.Markup.ShouldNotContain("access_token", Case.Insensitive);
        cut.Markup.ShouldNotContain("EventStore metadata", Case.Insensitive);

        cut.Find("[data-surface-testid='tenants-audit-copy-reference']").Click();
        cut.WaitForAssertion(() => writeHandler.Invocations.Count.ShouldBe(1));
        writeHandler.Invocations.Single().Arguments[0].ShouldBe("event-safe-reference");
    }

    [Fact]
    public void Tenant_audit_page_omits_grid_copy_for_an_unsafe_raw_event_reference()
    {
        RegisterServices(ReadySnapshot([Row("Bearer raw-token", AuditEventCategory.Access)]));

        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-audit-grid']");

        cut.FindAll("[data-testid='tenants-audit-copy-reference']").ShouldBeEmpty();
        cut.FindAll("[data-surface-testid='tenants-audit-copy-reference']").ShouldBeEmpty();
    }

    [Fact]
    public void Tenant_audit_page_opens_receipt_from_loaded_row_without_extra_backend_query()
    {
        StubTenantQueryGateway gateway = RegisterServices(ReadySnapshot([Row("event-safe-reference", AuditEventCategory.Access)]));
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo("/tenants/tenant.alpha/audit?supportSafeCommandReference=command-safe-reference");
        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-audit-grid']");

        cut.Find("[data-testid='tenants-audit-receipt-open']").Click();

        cut.WaitForElement("[data-testid='tenants-audit-receipt']");
        gateway.Requests.Count.ShouldBe(1);
        cut.Find("[data-testid='tenants-audit-receipt-reference']").TextContent.ShouldContain("event-safe-reference");
        cut.Find("[data-testid='tenants-audit-receipt-copy']").GetAttribute("data-copy-kind").ShouldBe("ApprovedReference");
        cut.Markup.ShouldContain("command-safe-reference");
        cut.Markup.ShouldNotContain("raw payload", Case.Insensitive);
        cut.Markup.ShouldNotContain("access_token", Case.Insensitive);
        cut.Markup.ShouldNotContain("EventStore metadata", Case.Insensitive);
    }

    [Fact]
    public void Tenant_audit_page_receipt_reference_query_fails_closed_when_row_is_not_loaded()
    {
        StubTenantQueryGateway gateway = RegisterServices(ReadySnapshot([Row("event-loaded", AuditEventCategory.Access)]));
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo("/tenants/tenant.alpha/audit?receiptReference=event-not-loaded");
        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-audit-receipt']");

        gateway.Requests.Count.ShouldBe(1);
        cut.Find("[data-testid='tenants-audit-receipt-state']").TextContent.ShouldContain("not loaded");
        cut.Find("[data-testid='tenants-audit-receipt-reference']").TextContent.ShouldContain("event-not-loaded");
        // Visible text only — avoids the Fluent success-color token false positive (see VisibleText).
        cut.VisibleText().ShouldNotContain("Success", Case.Insensitive);
    }

    [Fact]
    public void Tenant_audit_page_receipt_reference_query_opens_loaded_row_without_extra_backend_query()
    {
        StubTenantQueryGateway gateway = RegisterServices(ReadySnapshot([Row("event-safe-reference", AuditEventCategory.Access)]));
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo("/tenants/tenant.alpha/audit?receiptReference=event-safe-reference&supportSafeCommandReference=command-safe-reference");

        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, "tenant.alpha"));

        cut.WaitForElement("[data-testid='tenants-audit-receipt']");
        gateway.Requests.Count.ShouldBe(1);
        cut.Find("[data-testid='tenants-audit-receipt-state']").TextContent.ShouldContain("ready");
        cut.Find("[data-testid='tenants-audit-receipt-reference']").TextContent.ShouldContain("event-safe-reference");
        cut.Find("[data-testid='tenants-audit-receipt-copy']").GetAttribute("data-copy-kind").ShouldBe("ApprovedReference");
        cut.Markup.ShouldContain("command-safe-reference");
        cut.Markup.ShouldNotContain("raw payload", Case.Insensitive);
        cut.Markup.ShouldNotContain("access_token", Case.Insensitive);
    }

    [Fact]
    public void Tenant_audit_page_fails_closed_for_membership_correction_when_intended_role_is_missing()
    {
        StubTenantQueryGateway gateway = RegisterServices(ReadySnapshot(
            [
                Row(
                    "event-removed-member",
                    AuditEventCategory.Access,
                    referenceContext: "userId: target-user",
                    eventType: "UserRemovedFromTenant"),
            ]));

        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-audit-grid']");

        cut.Find("[data-testid='tenants-correction-unavailable-reason']").TextContent.ShouldContain("Choose the intended role");
        cut.FindAll("[data-testid='tenants-correction-start']").ShouldBeEmpty();
        cut.FindAll("[data-testid='tenants-correction-panel']").ShouldBeEmpty();
        gateway.Requests.Count.ShouldBe(1);
        cut.Markup.ShouldNotContain("POST /api/v1/commands", Case.Insensitive);
        // Visible text only — "undo" also appears inside the Fluent token --colorNeutralForegroundOnBrand.
        cut.VisibleText().ShouldNotContain("undo", Case.Insensitive);
        cut.Markup.ShouldNotContain("rollback", Case.Insensitive);
        cut.Markup.ShouldNotContain("hidden edit", Case.Insensitive);
    }

    [Fact]
    public void Tenant_audit_page_receipt_flow_keeps_original_evidence_visible_when_correction_is_blocked()
    {
        StubTenantQueryGateway gateway = RegisterServices(ReadySnapshot(
            [
                Row(
                    "event-removed-member",
                    AuditEventCategory.Access,
                    referenceContext: "userId: target-user",
                    eventType: "UserRemovedFromTenant"),
            ]));

        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-audit-grid']");

        cut.Find("[data-testid='tenants-audit-receipt-open']").Click();

        cut.WaitForElement("[data-testid='tenants-audit-receipt']");
        cut.Find("[data-testid='tenants-audit-receipt-reference']").TextContent.ShouldContain("event-removed-member");
        cut.FindAll("[data-testid='tenants-correction-unavailable-reason']")
            .Any(reason => reason.TextContent.Contains("Choose the intended role", StringComparison.Ordinal))
            .ShouldBeTrue();
        cut.FindAll("[data-testid='tenants-correction-start']").ShouldBeEmpty();
        cut.FindAll("[data-testid='tenants-correction-panel']").ShouldBeEmpty();
        gateway.Requests.Count.ShouldBe(1);
    }

    [Fact]
    public void Tenant_audit_page_opens_fixed_global_administrator_correction_panel_for_authorized_evidence()
    {
        StubTenantQueryGateway gateway = RegisterGlobalAdminServices(
            authorized: true,
            GlobalAdmins("other-admin"),
            GlobalAdminAuditSnapshot("GlobalAdministratorRemoved", "admin-user"));

        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, "system"));
        cut.WaitForElement("[data-testid='tenants-audit-grid']");

        cut.WaitForElement("[data-testid='tenants-correction-start']").Click();

        cut.WaitForElement("[data-testid='tenants-correction-panel']");
        cut.Find("[data-testid='tenants-correction-domain']").TextContent.ShouldContain("Global administrators");
        // The fixed global-administrator correction never offers a tenant role selector (AC1/AC2).
        cut.FindAll("[data-testid='tenants-correction-role']").ShouldBeEmpty();
        gateway.GlobalAdminRequests.Count.ShouldBeGreaterThanOrEqualTo(1);
        cut.VisibleText().ShouldNotContain("tenant role", Case.Insensitive);
    }

    [Fact]
    public void Tenant_audit_page_keeps_global_administrator_correction_fail_closed_when_unauthorized()
    {
        RegisterGlobalAdminServices(
            authorized: false,
            GlobalAdmins("other-admin"),
            GlobalAdminAuditSnapshot("GlobalAdministratorRemoved", "admin-user"));

        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, "system"));
        cut.WaitForElement("[data-testid='tenants-audit-grid']");

        cut.FindAll("[data-testid='tenants-correction-start']").ShouldBeEmpty();
        cut.FindAll("[data-testid='tenants-correction-panel']").ShouldBeEmpty();
        cut.FindAll("[data-testid='tenants-correction-unavailable-reason']").ShouldNotBeEmpty();
        cut.VisibleText().ShouldNotContain("Success", Case.Insensitive);
    }

    [Fact]
    public void Tenant_audit_page_renders_timestamps_in_utc_independent_of_host_timezone()
    {
        RegisterServices(ReadySnapshot([Row("event-1", AuditEventCategory.Access)]));

        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-audit-grid']");

        // Row timestamp is 2026-06-01T10:00:00Z; rendering must stay UTC, not shift to the server's tz.
        cut.Find("[data-testid='tenants-audit-row-timestamp']").TextContent.ShouldBe("2026-06-01 10:00:00 UTC");
    }

    [Fact]
    public void Tenant_audit_page_exposes_keyboard_native_controls_with_accessible_labels()
    {
        RegisterServices(ReadySnapshot([Row("event-1", AuditEventCategory.Access)], nextCursor: "next-cursor", hasMore: true));

        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-audit-grid']");

        cut.FindAll("label").Count.ShouldBeGreaterThanOrEqualTo(3);
        cut.Find("[data-testid='tenants-audit-filter-from']").ParentElement!.TextContent.ShouldContain("From");
        cut.Find("[data-testid='tenants-audit-filter-to']").ParentElement!.TextContent.ShouldContain("To");
        cut.Find("[data-testid='tenants-audit-filter-category']").ParentElement!.TextContent.ShouldContain("Category");
        cut.Find("[data-testid='tenants-audit-refresh']").NodeName.ShouldBe("FLUENT-BUTTON");
        cut.Find("[data-testid='tenants-audit-reset']").NodeName.ShouldBe("FLUENT-BUTTON");
        cut.Find("[data-testid='tenants-audit-next']").NodeName.ShouldBe("FLUENT-BUTTON");
        cut.Find("[data-testid='tenants-audit-previous']").NodeName.ShouldBe("FLUENT-BUTTON");
        cut.Find("[data-testid='tenants-audit-previous']").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("[data-testid='tenants-audit-next']").HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public void Date_and_category_filters_trigger_server_side_audit_query_and_clear_cursor()
    {
        StubTenantQueryGateway gateway = RegisterServices(
            ReadySnapshot([Row("event-1", AuditEventCategory.Access)], nextCursor: "opaque-next", hasMore: true),
            ReadySnapshot([Row("event-2", AuditEventCategory.Administrative)]),
            ReadySnapshot([Row("event-3", AuditEventCategory.Administrative)]));
        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-audit-grid']");

        cut.Find("[data-testid='tenants-audit-next']").Click();
        cut.WaitForAssertion(() => gateway.Requests.Count.ShouldBe(2));
        gateway.Requests[1].Cursor.ShouldBe("opaque-next");

        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-audit-filter-category", AuditEventCategory.Administrative.ToString());
        cut.WaitForAssertion(() => gateway.Requests.Count.ShouldBe(3));

        gateway.Requests[2].Cursor.ShouldBeNull();
        gateway.Requests[2].Category.ShouldBe(AuditEventCategory.Administrative);
    }

    [Fact]
    public void Date_filters_pass_absolute_values_to_gateway()
    {
        StubTenantQueryGateway gateway = RegisterServices(
            ReadySnapshot([Row("event-1", AuditEventCategory.Access)]),
            ReadySnapshot([Row("event-2", AuditEventCategory.Access)]),
            ReadySnapshot([Row("event-3", AuditEventCategory.Access)]));
        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-audit-grid']");

        cut.Find("[data-testid='tenants-audit-filter-from']").Change("2026-06-01T10:15");
        cut.Find("[data-testid='tenants-audit-filter-to']").Change("2026-06-02T11:45");
        cut.WaitForAssertion(() => gateway.Requests.Count.ShouldBe(3));

        gateway.Requests[1].From.ShouldNotBeNull();
        gateway.Requests[2].To.ShouldNotBeNull();
    }

    [Fact]
    public void Cursor_paging_passes_opaque_cursor_and_previous_history()
    {
        StubTenantQueryGateway gateway = RegisterServices(
            ReadySnapshot([Row("event-1", AuditEventCategory.Access)], nextCursor: "opaque-next", hasMore: true),
            ReadySnapshot([Row("event-2", AuditEventCategory.Access)], requestCursor: "opaque-next"),
            ReadySnapshot([Row("event-1", AuditEventCategory.Access)], nextCursor: "opaque-next", hasMore: true));
        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-audit-grid']");

        cut.Find("[data-testid='tenants-audit-next']").Click();
        cut.WaitForAssertion(() => gateway.Requests.Count.ShouldBe(2));
        gateway.Requests[1].Cursor.ShouldBe("opaque-next");

        cut.Find("[data-testid='tenants-audit-previous']").Click();
        cut.WaitForAssertion(() => gateway.Requests.Count.ShouldBe(3));
        gateway.Requests[2].Cursor.ShouldBeNull();
    }

    [Fact]
    public async Task Previous_completion_tolerates_history_cleared_during_its_dispatcher_hop()
    {
        StubTenantQueryGateway gateway = RegisterServices(
            ReadySnapshot([Row("event-1", AuditEventCategory.Access)], nextCursor: "opaque-next", hasMore: true),
            ReadySnapshot([Row("event-2", AuditEventCategory.Access)], requestCursor: "opaque-next"),
            // Consumed by the filter change below, which issues its own unpaged read.
            ReadySnapshot([Row("event-filtered", AuditEventCategory.Access)]),
            // Consumed by the closing refresh that proves the circuit is still live.
            ReadySnapshot([Row("event-filtered", AuditEventCategory.Access)]));
        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-audit-grid']");

        cut.Find("[data-testid='tenants-audit-next']").Click();
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-audit-previous']")
            .HasAttribute("disabled").ShouldBeFalse());

        var previousPage = new TaskCompletionSource<TenantAuditSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        gateway.QueueResponse(previousPage.Task);
        Task previousNavigation = cut.Find("[data-testid='tenants-audit-previous']")
            .ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        cut.WaitForAssertion(() => gateway.Requests.Count.ShouldBe(3));

        // Driven through the public trigger the remark names: a filter change calls ClearPaging with no
        // in-flight check, so it clears the history while the Previous read is still on its dispatcher hop.
        // Reaching into `_cursorHistory` by reflection reproduced the same state, but pinned a private field
        // name rather than the behaviour, and mutated it from the test thread -- the very cross-thread access
        // the production code is being asserted to survive.
        cut.Find("[data-testid='tenants-audit-filter-from']").Change("2026-01-01");

        previousPage.SetResult(ReadySnapshot(
            [Row("event-1-returned", AuditEventCategory.Access)],
            nextCursor: "opaque-next",
            hasMore: true));
        await previousNavigation;

        // The filter's read is newer, so it owns the surface: the superseded Previous result must not be
        // written over it, and its commit must not throw on the history the filter already emptied.
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-audit-row']")
            .GetAttribute("data-audit-reference").ShouldBe("event-filtered"));
        cut.Find("[data-testid='tenants-audit-previous']").HasAttribute("disabled").ShouldBeTrue();

        // The circuit survived: the page still responds to input rather than having faulted on the commit.
        cut.Find("[data-testid='tenants-audit-refresh']").Click();
        cut.WaitForAssertion(() => gateway.Requests.Count.ShouldBe(5));
    }

    [Fact]
    public void Metadata_degraded_requested_page_advances_cursor_and_can_return_to_page_one()
    {
        TenantAuditSnapshot degradedPage = TenantAuditSnapshot.Degraded(
            [Row("event-2", AuditEventCategory.Access)],
            TenantAuditReason.ProjectionDegraded,
            new TenantAuditRequest("tenant.alpha", Cursor: "opaque-next"));
        StubTenantQueryGateway gateway = RegisterServices(
            ReadySnapshot([Row("event-1", AuditEventCategory.Access)], nextCursor: "opaque-next", hasMore: true),
            degradedPage,
            ReadySnapshot([Row("event-1", AuditEventCategory.Access)], nextCursor: "opaque-next", hasMore: true));
        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-audit-grid']");

        cut.Find("[data-testid='tenants-audit-next']").Click();
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-audit-grid']").TextContent.ShouldContain("event-2"));

        cut.Find("[data-testid='tenants-audit-previous']").Click();
        cut.WaitForAssertion(() => gateway.Requests.Count.ShouldBe(3));
        gateway.Requests.Select(static request => request.Cursor).ShouldBe([null, "opaque-next", null]);
    }

    [Theory]
    [InlineData(TenantAuditSurfaceKind.Loading, "tenants-audit-loading")]
    [InlineData(TenantAuditSurfaceKind.Empty, "tenants-audit-empty")]
    [InlineData(TenantAuditSurfaceKind.FilteredEmpty, "tenants-audit-filtered-empty")]
    [InlineData(TenantAuditSurfaceKind.Stale, "tenants-audit-stale")]
    [InlineData(TenantAuditSurfaceKind.Degraded, "tenants-audit-degraded")]
    [InlineData(TenantAuditSurfaceKind.Unauthorized, "tenants-audit-unauthorized")]
    [InlineData(TenantAuditSurfaceKind.InvalidCursor, "tenants-audit-invalid-cursor")]
    [InlineData(TenantAuditSurfaceKind.ListRefreshed, "tenants-audit-list-refreshed")]
    [InlineData(TenantAuditSurfaceKind.Unavailable, "tenants-audit-unavailable")]
    [InlineData(TenantAuditSurfaceKind.Error, "tenants-audit-error")]
    public void Tenant_audit_page_renders_distinct_accessible_states(TenantAuditSurfaceKind kind, string selector)
    {
        RegisterServices(SnapshotFor(kind));

        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, "tenant.alpha"));
        cut.WaitForElement($"[data-testid='{selector}']");

        cut.Find($"[data-testid='{selector}']").GetAttribute("role").ShouldNotBeNull();
        cut.Find("[data-testid='tenants-audit-live-region']").TextContent.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Tenant_audit_components_do_not_call_backend_or_use_browser_token_storage()
    {
        string projectRoot = ProjectRoot();
        string[] componentFiles =
        [
            Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Components", "Pages", "TenantAuditPage.razor"),
            Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Components", "Tenants", "Audit", "AuditDataGrid.razor"),
            Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Components", "Tenants", "Audit", "AuditEvidenceReceipt.razor"),
            Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Components", "Tenants", "Audit", "CorrectionStartPanel.razor"),
            Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Components", "Tenants", "Audit", "GlobalAdministratorCorrectionPanel.razor"),
            Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "State", "TenantAudit", "TenantAuditReceipt.cs"),
            Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "State", "TenantAudit", "TenantCorrectionStartIntent.cs"),
            Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "State", "TenantAudit", "GlobalAdministratorCorrectionSnapshot.cs"),
        ];
        string combined = string.Join('\n', componentFiles.Select(File.ReadAllText));

        combined.ShouldNotContain("GET /api/tenants", Case.Insensitive);
        combined.ShouldNotContain("POST /api/v1/commands", Case.Insensitive);
        combined.ShouldNotContain("GET /api/v1/commands/status", Case.Insensitive);
        combined.ShouldNotContain("HttpClient", Case.Insensitive);
        combined.ShouldNotContain("localStorage", Case.Insensitive);
        combined.ShouldNotContain("sessionStorage", Case.Insensitive);
        combined.ShouldNotContain("access_token", Case.Insensitive);
        combined.ShouldNotContain("raw payload", Case.Insensitive);
        combined.ShouldNotContain("EventStore metadata", Case.Insensitive);
    }

    [Fact]
    public void Tenant_correction_copy_uses_forward_recovery_language_and_omits_diagnostic_markers()
    {
        string projectRoot = ProjectRoot();
        string[] files =
        [
            Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Components", "Tenants", "Audit", "AuditDataGrid.razor"),
            Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Components", "Tenants", "Audit", "AuditEvidenceReceipt.razor"),
            Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Components", "Tenants", "Audit", "CorrectionStartPanel.razor"),
            Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Components", "Tenants", "Audit", "GlobalAdministratorCorrectionPanel.razor"),
            Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "State", "TenantAudit", "TenantCorrectionStartIntent.cs"),
            Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Resources", "TenantsResources.resx"),
            Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Resources", "TenantsResources.fr.resx"),
        ];
        string combined = string.Join('\n', files.Select(File.ReadAllText));

        combined.ShouldNotContain("undo", Case.Insensitive);
        combined.ShouldNotContain("rollback", Case.Insensitive);
        combined.ShouldNotContain("hidden edit", Case.Insensitive);
        combined.ShouldNotContain("Bearer ", Case.Insensitive);
        combined.ShouldNotContain("JWT", Case.Insensitive);
        combined.ShouldNotContain("stack trace", Case.Insensitive);
        combined.ShouldNotContain("correlation id", Case.Insensitive);
        combined.ShouldNotContain("MessageId", Case.Insensitive);
        combined.ShouldNotContain("protected cursor", Case.Insensitive);
        combined.ShouldNotContain("ETag", Case.Insensitive);
    }

    [Fact]
    public void Audit_styles_preserve_responsive_safety_and_accessibility_hooks()
    {
        string projectRoot = ProjectRoot();
        string styles = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Tenants",
            "Audit",
            "AuditDataGrid.razor.css"));
        string pageStyles = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Pages",
            "TenantAuditPage.razor.css"));

        styles.ShouldContain("overflow-x: auto");
        styles.ShouldContain("min-width:");
        styles.ShouldContain("@media (forced-colors: active)");
        styles.ShouldContain("tenants-audit-critical");
        styles.ShouldContain("grid-template-columns: minmax(0, 1fr) auto");
        pageStyles.ShouldContain(":focus-visible");
        pageStyles.ShouldContain("@media (forced-colors: active)");

        string receiptStyles = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Tenants",
            "Audit",
            "AuditEvidenceReceipt.razor.css"));

        receiptStyles.ShouldContain(":focus-visible");
        receiptStyles.ShouldContain("@media (forced-colors: active)");
        receiptStyles.ShouldContain("@media (prefers-reduced-motion: reduce)");
        receiptStyles.ShouldContain("grid-template-columns: repeat(auto-fit");

        string correctionStyles = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Tenants",
            "Audit",
            "CorrectionStartPanel.razor.css"));

        correctionStyles.ShouldContain(":focus-visible");
        correctionStyles.ShouldContain("@media (forced-colors: active)");
        correctionStyles.ShouldContain("@media (prefers-reduced-motion: reduce)");

        string globalAdminCorrectionStyles = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Tenants",
            "Audit",
            "GlobalAdministratorCorrectionPanel.razor.css"));

        globalAdminCorrectionStyles.ShouldContain(":focus-visible");
        globalAdminCorrectionStyles.ShouldContain("@media (forced-colors: active)");
        globalAdminCorrectionStyles.ShouldContain("@media (prefers-reduced-motion: reduce)");
    }

    [Fact]
    public void Audit_resource_keys_have_english_and_french_parity()
    {
        string projectRoot = ProjectRoot();
        HashSet<string> english = ResourceKeys(Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Resources", "TenantsResources.resx"));
        HashSet<string> french = ResourceKeys(Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Resources", "TenantsResources.fr.resx"));
        string[] auditKeys = english
            .Where(key => key.StartsWith("Tenants.Audit.", StringComparison.Ordinal)
                || key.StartsWith("Tenants.Correction.", StringComparison.Ordinal))
            .ToArray();

        auditKeys.ShouldNotBeEmpty();
        foreach (string key in auditKeys)
        {
            french.ShouldContain(key);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Tenant_audit_page_uses_localized_fallback_heading_for_blank_tenant_id(string blankTenantId)
    {
        RegisterServices(ReadySnapshot([Row("event-1", AuditEventCategory.Access)]));

        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, blankTenantId));
        cut.WaitForElement("[data-testid='tenants-audit-grid']");

        // A blank/whitespace TenantId must render the localized fallback, never a dangling
        // "Audit trail for " heading (AC8 — cosmetic, not a crash fix).
        cut.Markup.ShouldContain("Audit trail for this tenant");
        cut.Markup.ShouldNotContain("Audit trail for <", Case.Insensitive);
    }

    [Fact]
    public void Tenant_audit_page_survives_global_administrator_projection_fault_during_load()
    {
        // A global-administrator audit row triggers a supplementary global-administrator projection read
        // during page load. If that read faults with anything the gateway does not map to a degraded
        // snapshot (here an HttpRequestException), the audit page must still render — the supplementary
        // evidence is best-effort and must not tear down the whole page.
        JSInterop.Mode = JSRuntimeMode.Loose;
        StubTenantQueryGateway gateway = new(GlobalAdminAuditSnapshot("GlobalAdministratorSet", "admin-user"))
        {
            GlobalAdminFault = new HttpRequestException("projection read failed"),
        };
        Services.AddSingleton<ITenantQueryGateway>(gateway);
        Services.AddSingleton<ITenantsBffComposition>(new StubBffComposition(authorized: true));
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddFluentUIComponents();

        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, "tenant.alpha"));

        cut.WaitForElement("[data-testid='tenants-audit-grid']");
        gateway.GlobalAdminRequests.ShouldNotBeEmpty();
    }

    private StubTenantQueryGateway RegisterServices(params TenantAuditSnapshot[] snapshots)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        StubTenantQueryGateway gateway = new(snapshots);
        Services.AddSingleton<ITenantQueryGateway>(gateway);
        Services.AddSingleton<ITenantsBffComposition>(new StubBffComposition());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddFluentUIComponents();
        return gateway;
    }

    private StubTenantQueryGateway RegisterGlobalAdminServices(
        bool authorized,
        GlobalAdministratorsSnapshot globalAdministrators,
        params TenantAuditSnapshot[] snapshots)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        StubTenantQueryGateway gateway = new(snapshots) { GlobalAdministrators = globalAdministrators };
        Services.AddSingleton<ITenantQueryGateway>(gateway);
        Services.AddSingleton<ITenantsBffComposition>(new StubBffComposition(authorized: authorized));
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddFluentUIComponents();
        return gateway;
    }

    private static GlobalAdministratorsSnapshot GlobalAdmins(params string[] userIds)
        => GlobalAdministratorsSnapshot.Ready(
            userIds.Select(userId => new GlobalAdministratorRow(userId, ReadModelFreshnessState.Current)).ToArray(),
            nextCursor: null,
            hasMore: false,
            eTag: "\"ga-etag\"",
            freshness: ReadModelFreshnessState.Current);

    private static TenantAuditSnapshot GlobalAdminAuditSnapshot(string eventType, string targetUserId)
        => TenantAuditSnapshot.Ready(
            [
                new TenantAuditRow(
                    "event-global-admin",
                    eventType,
                    AuditEventCategory.Administrative,
                    "actor-user",
                    DateTimeOffset.Parse("2026-06-01T10:00:00Z", CultureInfo.InvariantCulture),
                    "system",
                    targetUserId,
                    "global-administrators",
                    eventType,
                    $"userId: {targetUserId}",
                    ReadModelFreshnessState.Current,
                    ProjectionLifecycleState.Current,
                    QueryResponseProvenance.ProjectionBacked),
            ],
            nextCursor: null,
            hasMore: false,
            eTag: "\"etag\"",
            freshness: ReadModelFreshnessState.Current,
            new TenantAuditRequest("system"));

    /// <summary>
    /// Builds a ready audit snapshot, with the snapshot lifecycle independent of the row lifecycle.
    /// </summary>
    /// <remarks>
    /// Production derives the two separately -- the snapshot lifecycle comes from response metadata, the row
    /// lifecycle from the row -- so deriving the snapshot's from <c>rows[0]</c> made the non-collapse case
    /// (metadata says the projection is not current while the rows still say Current) inexpressible, and no
    /// test in this file rendered a non-Current snapshot lifecycle.
    /// </remarks>
    private static TenantAuditSnapshot ReadySnapshot(
        IReadOnlyList<TenantAuditRow> rows,
        string? nextCursor = null,
        bool hasMore = false,
        string? requestCursor = null,
        ProjectionLifecycleState? lifecycle = null)
        => TenantAuditSnapshot.Ready(
            rows,
            nextCursor,
            hasMore,
            eTag: "\"etag\"",
            freshness: rows.Any(row => row.Freshness == ReadModelFreshnessState.Stale)
                ? ReadModelFreshnessState.Stale
                : ReadModelFreshnessState.Current,
            new TenantAuditRequest("tenant.alpha", Cursor: requestCursor)) with
        {
            Lifecycle = lifecycle
                ?? (rows.Count == 0 ? ProjectionLifecycleState.Current : rows[0].Lifecycle),
        };

    private static TenantAuditSnapshot SnapshotFor(TenantAuditSurfaceKind kind)
    {
        TenantAuditRequest request = new("tenant.alpha", Category: kind is TenantAuditSurfaceKind.FilteredEmpty ? AuditEventCategory.Access : null);
        return kind switch
        {
            TenantAuditSurfaceKind.Loading => TenantAuditSnapshot.Loading("tenant.alpha"),
            TenantAuditSurfaceKind.Empty => TenantAuditSnapshot.Empty(true, ReadModelFreshnessState.Current, "\"etag\"", request),
            TenantAuditSurfaceKind.FilteredEmpty => TenantAuditSnapshot.Empty(true, ReadModelFreshnessState.Current, "\"etag\"", request),
            TenantAuditSurfaceKind.Stale => TenantAuditSnapshot.Stale([Row("event-stale", AuditEventCategory.Access, freshness: ReadModelFreshnessState.Stale)], null, false, "\"etag\"", request),
            TenantAuditSurfaceKind.Degraded => TenantAuditSnapshot.Degraded([Row("event-degraded", AuditEventCategory.Access)], TenantAuditReason.ProjectionDegraded, request),
            TenantAuditSurfaceKind.Unauthorized => TenantAuditSnapshot.Unauthorized(request),
            TenantAuditSurfaceKind.InvalidCursor => TenantAuditSnapshot.InvalidCursor(request),
            TenantAuditSurfaceKind.ListRefreshed => TenantAuditSnapshot.ListRefreshed([Row("event-refreshed", AuditEventCategory.Access)], null, false, "\"etag\"", ReadModelFreshnessState.Current, request),
            TenantAuditSurfaceKind.Unavailable => TenantAuditSnapshot.Unavailable(request),
            TenantAuditSurfaceKind.Error => TenantAuditSnapshot.Error(request),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
    }

    private static TenantAuditRow Row(
        string eventReference,
        AuditEventCategory category,
        string referenceContext = "userId: target-user",
        ReadModelFreshnessState freshness = ReadModelFreshnessState.Current,
        string? eventType = null,
        ProjectionLifecycleState lifecycle = ProjectionLifecycleState.Current)
    {
        string outcome = eventType ?? (category is AuditEventCategory.Access ? "UserAddedToTenant" : "TenantConfigurationSet");

        return new(
            eventReference,
            outcome,
            category,
            "actor-user",
            DateTimeOffset.Parse("2026-06-01T10:00:00Z", CultureInfo.InvariantCulture),
            "tenant.alpha",
            "target-user",
            "tenant.alpha",
            outcome,
            referenceContext,
            freshness,
            lifecycle,
            QueryResponseProvenance.ProjectionBacked);
    }

    private static HashSet<string> ResourceKeys(string path)
        => XDocument.Load(path)
            .Root!
            .Elements("data")
            .Select(element => element.Attribute("name")?.Value)
            .Where(static name => name is not null)
            .Cast<string>()
            .ToHashSet(StringComparer.Ordinal);

    private static string ProjectRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private sealed class StubTenantQueryGateway(params TenantAuditSnapshot[] snapshots) : ITenantQueryGateway
    {
        /// <summary>
        /// Explicit because <c>ITenantQueryGateway.GetTenantUsersAsync</c> is no longer a default interface
        /// method. These stubs previously inherited a silent <c>Unavailable</c> fallback, so a member-read
        /// regression would have rendered as an outage here rather than failing the build.
        /// </summary>
        public Task<TenantUsersSnapshot> GetTenantUsersAsync(
            TenantUsersRequest request,
            TenantUsersSnapshot? previous,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Task.FromResult(TenantUsersSnapshot.Unavailable(request.TenantId));
        }

        private readonly Queue<TenantAuditSnapshot> _snapshots = new(snapshots);
        private readonly Queue<Task<TenantAuditSnapshot>> _queuedResponses = [];

        public List<TenantAuditRequest> Requests { get; } = [];
        public List<TenantDetailRequest> DetailRequests { get; } = [];

        public void QueueResponse(Task<TenantAuditSnapshot> response)
            => _queuedResponses.Enqueue(response);

        public Task<TenantDetailSnapshot> GetTenantAsync(
            TenantDetailRequest request,
            TenantDetailSnapshot? previous,
            CancellationToken cancellationToken = default)
        {
            DetailRequests.Add(request);
            TenantDetail detail = new(
                request.TenantId,
                "Tenant Alpha",
                null,
                TenantStatus.Active,
                [new TenantMember("target-user", TenantRole.TenantContributor)],
                new Dictionary<string, string>(StringComparer.Ordinal),
                DateTimeOffset.Parse("2026-06-01T09:00:00Z", CultureInfo.InvariantCulture));
            return Task.FromResult(TenantDetailSnapshot.Ready(detail, "\"detail-etag\"", ReadModelFreshnessState.Current));
        }

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

        public GlobalAdministratorsSnapshot GlobalAdministrators { get; init; }
            = GlobalAdministratorsSnapshot.Empty(false, ReadModelFreshnessState.Current, "\"ga-etag\"");

        public List<GlobalAdministratorsRequest> GlobalAdminRequests { get; } = [];

        public Exception? GlobalAdminFault { get; init; }

        public Task<GlobalAdministratorsSnapshot> GetGlobalAdministratorsAsync(
            GlobalAdministratorsRequest request,
            GlobalAdministratorsSnapshot? previous,
            CancellationToken cancellationToken = default)
        {
            GlobalAdminRequests.Add(request);
            return GlobalAdminFault is not null
                ? throw GlobalAdminFault
                : Task.FromResult(GlobalAdministrators);
        }

        public Task<TenantAuditSnapshot> GetTenantAuditAsync(
            TenantAuditRequest request,
            TenantAuditSnapshot? previous,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return _queuedResponses.Count > 0
                ? _queuedResponses.Dequeue()
                : Task.FromResult(_snapshots.Dequeue());
        }
    }

    private sealed class StubBffComposition(
        bool readConnected = true,
        bool commandConnected = true,
        bool authorized = false) : ITenantsBffComposition
    {
        public bool IsReadSurfaceConnected => readConnected;

        public bool IsCommandSurfaceConnected => commandConnected;

        public TenantLifecycleAuthorizationReflectionState GlobalAdministratorsAuthorizationReflection
            => authorized
                ? TenantLifecycleAuthorizationReflectionState.Authorized
                : TenantLifecycleAuthorizationReflectionState.Indeterminate;
    }

    /// <summary>
    /// Resolution order: explicit override, then the real <c>TenantsResources.resx</c>, then throw.
    /// </summary>
    /// <remarks>
    /// Echoing an unknown key back as <c>name</c> made a missing resource indistinguishable from a present
    /// one: the component rendered the literal key as user-visible copy and any substring assertion over it
    /// passed whether or not the string existed. Falling through to the real resource lets a test assert
    /// shipped copy without hand-copying it here; a key defined in neither is a defect, not a silent echo.
    /// Same rule as the sibling stub in <c>TenantDetailSurfaceTests</c>.
    /// </remarks>
    private sealed class StubTenantsLocalizer : IStringLocalizer<TenantsResources>
    {
        private static readonly ResourceManager RealResources = new(
            "Hexalith.Tenants.UI.Resources.TenantsResources",
            typeof(TenantsResources).Assembly);

        public LocalizedString this[string name] => new(name, Resolve(name));

        public LocalizedString this[string name, params object[] arguments]
            // No arguments means no substitution. Formatting unconditionally threw FormatException the
            // moment Resolve started falling through to a real .resx string containing `{0}` -- the stub
            // used to echo the placeholder-free key back, so the path could not be reached before.
            => new(name, arguments.Length == 0
                ? Resolve(name)
                : string.Format(CultureInfo.CurrentCulture, Resolve(name), arguments));

        // CurrentUICulture, not InvariantCulture. Pinning the invariant culture made this stub answer in
        // English no matter what culture a test rendered under, so a component that had hard-coded English
        // copy was indistinguishable from one that reads the localizer -- and no test could prove the French
        // resources are ever reached.
        private static string Resolve(string name)
            => Values.TryGetValue(name, out string? value)
                ? value
                : RealResources.GetString(name, CultureInfo.CurrentUICulture)
                    ?? throw new KeyNotFoundException(
                        $"Resource key '{name}' is defined neither in this stub nor in TenantsResources.resx. "
                        + "The stub must not echo an undefined key back as user-visible copy.");

        /// <summary>
        /// Enumerates everything <see cref="Resolve"/> can return, not just the overrides. Returning
        /// <c>Values</c> alone made enumeration and lookup disagree: a key resolvable through the real
        /// resource set was absent from the enumeration, so any caller reasoning about "the available
        /// strings" saw a set the indexer did not agree with. Overrides win, matching resolution order.
        /// </summary>
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            Dictionary<string, string> all = new(StringComparer.Ordinal);
            // NOT disposed: GetResourceSet returns the ResourceManager's own cached set, so disposing it
            // corrupts every subsequent lookup for the whole process -- Resolve then throws its
            // KeyNotFoundException for keys that do exist, and every bUnit WaitForAssertion downstream burns
            // its full timeout instead of failing.
            ResourceSet? real = RealResources.GetResourceSet(
                CultureInfo.CurrentUICulture,
                createIfNotExists: true,
                tryParents: includeParentCultures);
            if (real is not null)
            {
                foreach (DictionaryEntry entry in real)
                {
                    if (entry.Key is string key && entry.Value is string text)
                    {
                        all[key] = text;
                    }
                }
            }

            foreach (KeyValuePair<string, string> over in Values)
            {
                all[over.Key] = over.Value;
            }

            return all.Select(entry => new LocalizedString(entry.Key, entry.Value));
        }

        private static readonly Dictionary<string, string> Values = new(StringComparer.Ordinal)
        {
            ["Tenants.Audit.Back"] = "Back to tenant details",
            ["Tenants.Audit.Category.Access"] = "Access",
            ["Tenants.Audit.Category.Administrative"] = "Administrative",
            ["Tenants.Audit.Column.Actor"] = "Actor",
            ["Tenants.Audit.Column.Category"] = "Category",
            ["Tenants.Audit.Column.Correction"] = "Correction",
            ["Tenants.Audit.Column.Freshness"] = "Freshness",
            ["Tenants.Audit.Column.Outcome"] = "Outcome",
            ["Tenants.Audit.Column.Reference"] = "Reference context",
            ["Tenants.Audit.Column.Receipt"] = "Receipt",
            ["Tenants.Audit.Column.Scope"] = "Tenant scope",
            ["Tenants.Audit.Column.Target"] = "Target",
            ["Tenants.Audit.Column.Timestamp"] = "Timestamp",
            ["Tenants.Audit.ControlsLabel"] = "Tenant audit filters and paging controls",
            ["Tenants.Audit.Copy.EventReference"] = "Copy audit event reference {0}",
            ["Tenants.Audit.Description"] = "Read-only tenant audit evidence from the server-side query gateway.",
            ["Tenants.Audit.Eyebrow"] = "Tenant audit trail",
            ["Tenants.Audit.Filter.Category"] = "Category",
            ["Tenants.Audit.Filter.Category.All"] = "All categories",
            ["Tenants.Audit.Filter.From"] = "From",
            ["Tenants.Audit.Filter.To"] = "To",
            ["Tenants.Audit.Freshness.Current"] = "Current",
            ["Tenants.Audit.Freshness.Stale"] = "Stale",
            ["Tenants.Audit.Freshness.Unknown"] = "Unknown",
            ["Tenants.Audit.GridTitle"] = "Audit entries",
            ["Tenants.Audit.Next"] = "Next",
            ["Tenants.Audit.PaginationLabel"] = "Tenant audit pages",
            ["Tenants.Audit.Previous"] = "Previous",
            ["Tenants.Audit.Refresh"] = "Refresh",
            ["Tenants.Audit.Reset"] = "Reset filters",
            ["Tenants.Audit.Receipt.ActionsLabel"] = "Audit receipt recovery actions",
            ["Tenants.Audit.Receipt.Action.ContinueReadOnly"] = "Continue read-only",
            ["Tenants.Audit.Receipt.Action.Escalate"] = "Escalate with reference",
            ["Tenants.Audit.Receipt.Action.InspectAudit"] = "Inspect audit",
            ["Tenants.Audit.Receipt.Action.Refresh"] = "Refresh",
            ["Tenants.Audit.Receipt.Action.Retry"] = "Retry",
            ["Tenants.Audit.Receipt.Action.Wait"] = "Wait for audit evidence",
            ["Tenants.Audit.Receipt.Copy"] = "Copy audit receipt reference",
            ["Tenants.Audit.Receipt.Field.Actor"] = "Actor",
            ["Tenants.Audit.Receipt.Field.CommandReference"] = "Command reference",
            ["Tenants.Audit.Receipt.Field.Outcome"] = "Outcome",
            ["Tenants.Audit.Receipt.Field.ProjectionMarker"] = "Projection marker",
            ["Tenants.Audit.Receipt.Field.Reference"] = "Audit reference",
            ["Tenants.Audit.Receipt.Field.Scope"] = "Tenant scope",
            ["Tenants.Audit.Receipt.Field.Target"] = "Target",
            ["Tenants.Audit.Receipt.Field.Timestamp"] = "Timestamp",
            ["Tenants.Audit.Receipt.Open"] = "View receipt",
            ["Tenants.Audit.Receipt.State.Degraded"] = "Audit evidence is degraded. Use the reference only with this limitation.",
            ["Tenants.Audit.Receipt.State.Delayed"] = "Audit evidence is delayed. Inspect audit or retry before citing proof.",
            ["Tenants.Audit.Receipt.State.InvalidReference"] = "The requested receipt reference is not loaded in the current tenant-scoped audit result.",
            ["Tenants.Audit.Receipt.State.MissingSupport"] = "Audit evidence support is missing. Escalate with the support-safe reference.",
            ["Tenants.Audit.Receipt.State.Partial"] = "Audit evidence is partial. The receipt cannot cite a complete proof.",
            ["Tenants.Audit.Receipt.State.Pending"] = "Audit evidence is pending. Wait or refresh before citing proof.",
            ["Tenants.Audit.Receipt.State.Ready"] = "Audit evidence is ready to cite.",
            ["Tenants.Audit.Receipt.State.Stale"] = "Audit evidence is stale. Refresh before treating it as current.",
            ["Tenants.Audit.Receipt.State.Unauthorized"] = "Audit evidence is not available for the current authorization scope.",
            ["Tenants.Audit.Receipt.State.Unavailable"] = "Audit evidence is unavailable. Continue read-only or retry later.",
            ["Tenants.Audit.Receipt.Title"] = "Audit evidence receipt",
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
            ["Tenants.Audit.State.Degraded.Message"] = "Audit evidence is degraded. Last confirmed support-safe rows remain visible for this exact tenant and filter scope only.",
            ["Tenants.Audit.State.Degraded.Title"] = "Audit data degraded",
            ["Tenants.Audit.State.Empty.Message"] = "No audit entries are visible for this tenant scope.",
            ["Tenants.Audit.State.Empty.Title"] = "No audit entries",
            ["Tenants.Audit.State.Error.Message"] = "Audit data could not be loaded. No raw event payloads or internal gateway details are shown.",
            ["Tenants.Audit.State.Error.Title"] = "Audit data unavailable",
            ["Tenants.Audit.State.FilteredEmpty.Message"] = "No audit entries match the selected date range and category filters.",
            ["Tenants.Audit.State.FilteredEmpty.Title"] = "No audit entries match filters",
            ["Tenants.Audit.State.InvalidCursor.Message"] = "The audit cursor is no longer valid. Use refresh to request the first page again.",
            ["Tenants.Audit.State.InvalidCursor.Title"] = "Audit page cursor invalid",
            ["Tenants.Audit.State.ListRefreshed.Message"] = "The audit cursor changed, so the list was refreshed from page one for this tenant and filter scope.",
            ["Tenants.Audit.State.ListRefreshed.Title"] = "Audit list refreshed",
            ["Tenants.Audit.State.Loading.Message"] = "Audit entries are loading through the server-side query gateway.",
            ["Tenants.Audit.State.Loading.Title"] = "Loading audit entries",
            ["Tenants.Audit.State.Ready.Message"] = "Audit entries are loaded with support-safe row fields only.",
            ["Tenants.Audit.State.Ready.Title"] = "Audit entries loaded",
            ["Tenants.Audit.State.Stale.Message"] = "Audit freshness is stale. Refresh to check the projection again before treating the list as current.",
            ["Tenants.Audit.State.Stale.Title"] = "Audit data stale",
            ["Tenants.Audit.State.Unauthorized.Message"] = "You are not authorized to view tenant audit entries. No hidden audit data is shown.",
            ["Tenants.Audit.State.Unauthorized.Title"] = "Audit access unavailable",
            ["Tenants.Audit.State.Unavailable.Message"] = "The tenant audit read surface is unavailable. No audit payloads are shown.",
            ["Tenants.Audit.State.Unavailable.Title"] = "Audit read surface unavailable",
            ["Tenants.Audit.Title"] = "Audit trail for {0}",
            ["Tenants.Audit.UnknownTenant"] = "this tenant",
            ["Tenants.Correction.Action.RestoreAccess"] = "restore intended access",
            ["Tenants.Correction.Action.RestoreAccessAccessible"] = "restore intended access for audit evidence {0}",
            ["Tenants.Correction.Action.Start"] = "start correction",
            ["Tenants.Correction.Action.StartAccessible"] = "start correction for audit evidence {0}",
            ["Tenants.Correction.Unavailable.ExplicitRoleRequired"] = "Choose the intended role before starting correction.",
            ["Tenants.Correction.Unavailable.AuthorizationIndeterminate"] = "Authorization evidence is indeterminate.",
            ["Tenants.Correction.Unavailable.GlobalAdministratorCommandSupportUnavailable"] = "Global administrator correction commands are not connected.",
            ["Tenants.Correction.Domain.GlobalAdministrators"] = "Global administrators",
            ["Tenants.Correction.Command.SetGlobalAdministrator"] = "Set global administrator",
            ["Tenants.Copy.Action"] = "Copy",
            ["Tenants.Copy.Feedback.Copied"] = "Copied.",
        };
    }

    [Fact]
    public void Metadata_degraded_page_describing_the_prior_cursor_is_not_committed_as_the_requested_page()
    {
        // The reject side of the degraded-page cursor rule. The existing coverage builds its degraded
        // snapshot with a RequestCursor that MATCHES the request, so only the accept path ran and dropping
        // the equality check kept the suite green. A degraded snapshot retaining the PRIOR page's rows and
        // cursor must not advance _currentCursor to a page that never rendered.
        TenantAuditSnapshot degradedPriorPage = TenantAuditSnapshot.Degraded(
            [Row("event-1", AuditEventCategory.Access)],
            TenantAuditReason.ProjectionDegraded,
            new TenantAuditRequest("tenant.alpha", Cursor: null));
        StubTenantQueryGateway gateway = RegisterServices(
            ReadySnapshot([Row("event-1", AuditEventCategory.Access)], nextCursor: "opaque-next", hasMore: true),
            degradedPriorPage,
            ReadySnapshot([Row("event-1", AuditEventCategory.Access)], nextCursor: "opaque-next", hasMore: true));
        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-audit-grid']");

        cut.Find("[data-testid='tenants-audit-next']").Click();
        cut.WaitForAssertion(() => gateway.Requests.Count.ShouldBe(2));

        // Paging state must not have advanced, so Previous stays unavailable: page one is still current.
        // Find, not FindAll(...).All(...): the Previous button renders only inside the rows branch, so a
        // regression that stops rendering rows satisfies an All() over an empty match set unconditionally.
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-audit-previous']")
            .HasAttribute("disabled").ShouldBeTrue());
    }

    [Fact]
    public void A_query_string_only_navigation_does_not_discard_the_retained_validator_or_blank_the_grid()
    {
        // The audit route carries six query-string parameters, none of which feed the request. The member
        // "open audit for this user" entry point therefore re-enters OnParametersSetAsync with an unchanged
        // TenantId and an identical request; re-reading unconditionally discarded the conditional-read
        // validator, reset the grid to Loading and cancelled any in-flight load.
        StubTenantQueryGateway gateway = RegisterServices(
            [.. Enumerable.Repeat(
                ReadySnapshot([Row("event-1", AuditEventCategory.Access)], nextCursor: null, hasMore: false),
                6)]);
        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-audit-grid']");
        gateway.Requests.Count.ShouldBe(1);
        gateway.Requests[0].ETag.ShouldBeNull();

        // Query-string parameters must be supplied through navigation, exactly as the member audit entry
        // point does it: same route, same tenant, new query string.
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo("/tenants/tenant.alpha/audit?targetUserId=user.alpha&returnFocus=tenants-member-row");
        cut.Render(parameters => parameters.Add(p => p.TenantId, "tenant.alpha"));

        cut.WaitForAssertion(() => gateway.Requests.Count.ShouldBeGreaterThan(1));

        // Every same-tenant re-entry is conditional on the retained validator, and the grid never blanked.
        gateway.Requests.Skip(1).ShouldAllBe(static request => request.ETag != null);
        cut.Find("[data-testid='tenants-audit-grid']").TextContent.ShouldContain("event-1");
    }

    [Fact]
    public async Task A_next_page_completing_after_the_route_moved_does_not_commit_its_cursor_onto_the_new_tenant()
    {
        // NextPageAsync's tenant-identity clause guards a LATE completion, so the alpha page read is held
        // pending across the route change. Without the clause, alpha's cursor and history entry are
        // committed onto tenant beta's surface and beta's Previous walks back through alpha's paging.
        JSInterop.Mode = JSRuntimeMode.Loose;
        var pendingAlphaPage2 = new TaskCompletionSource<TenantAuditSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        List<TenantAuditRequest> requests = [];
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.GetTenantAuditAsync(Arg.Any<TenantAuditRequest>(), Arg.Any<TenantAuditSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                TenantAuditRequest request = call.Arg<TenantAuditRequest>()!;
                requests.Add(request);
                return request.Cursor == "alpha-next"
                    ? pendingAlphaPage2.Task
                    : Task.FromResult(ReadySnapshot(
                        [Row($"{request.TenantId}-event-1", AuditEventCategory.Access)],
                        nextCursor: "alpha-next",
                        hasMore: true));
            });
        Services.AddSingleton(gateway);
        Services.AddSingleton<ITenantsBffComposition>(new StubBffComposition());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddFluentUIComponents();

        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-audit-grid']");

        Task nextClick = cut.Find("[data-testid='tenants-audit-next']")
            .ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        cut.WaitForAssertion(() => requests.Count.ShouldBe(2));

        // Route to beta while alpha's page-two read is still in flight, then let it complete.
        cut.Render(parameters => parameters.Add(p => p.TenantId, "tenant.beta"));
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-audit-grid']")
            .TextContent.ShouldContain("tenant.beta-event-1"));

        pendingAlphaPage2.SetResult(ReadySnapshot(
            [Row("alpha-event-2", AuditEventCategory.Access)],
            nextCursor: null,
            hasMore: false));
        await nextClick;

        // Beta must still be on its own first page: no inherited cursor, no inherited history. Find, not
        // FindAll(...).All(...), for the same reason as above: the throwing form is what makes "the pager is
        // rendered and disabled" distinguishable from "the pager is gone".
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-audit-previous']")
            .HasAttribute("disabled").ShouldBeTrue());
        cut.Find("[data-testid='tenants-audit-grid']").TextContent.ShouldNotContain("alpha-event-2");
    }

    /// <summary>
    /// A re-entrant route set, arriving while the old lease dispose is suspended, must not reuse the previous
    /// tenant's conditional validator or cursor.
    /// </summary>
    /// <remarks>
    /// <c>_loadedTenantId</c> was assigned before the awaited lease dispose and the marshalled clear, so a
    /// second <c>OnParametersSetAsync</c> arriving while that remote unsubscribe was suspended computed
    /// <c>tenantChanged == false</c>. The read then built its request from the *previous* tenant's
    /// <c>_snapshot.ETag</c> and <c>_currentCursor</c>, which the <c>reuseETag</c>/<c>retainConfirmed</c>
    /// change made reachable -- before it, both arguments were unconditionally false on this path.
    /// </remarks>
    [Fact]
    public async Task A_re_entrant_route_set_during_lease_disposal_never_reuses_the_previous_tenants_validator()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        List<TenantAuditRequest> requests = [];
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.GetTenantAuditAsync(Arg.Any<TenantAuditRequest>(), Arg.Any<TenantAuditSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                TenantAuditRequest request = call.Arg<TenantAuditRequest>()!;
                requests.Add(request);
                return Task.FromResult(TenantAuditSnapshot.Ready(
                    [Row($"{request.TenantId}-event", AuditEventCategory.Access)],
                    nextCursor: $"{request.TenantId}-page-2",
                    hasMore: true,
                    eTag: $"\"{request.TenantId}-etag\"",
                    freshness: ReadModelFreshnessState.Current,
                    request));
            });

        IProjectionSubscription subscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        var suspendedUnsubscribe = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        subscription
            .UnsubscribeAsync(GetTenantAuditQuery.ProjectionType, "tenant.alpha", Arg.Any<CancellationToken>())
            .Returns(suspendedUnsubscribe.Task);
        Services.AddSingleton(gateway);
        Services.AddSingleton(subscription);
        Services.AddSingleton(notifier);
        Services.AddScoped<TenantReadRefreshSubscription>();
        Services.AddSingleton<ITenantsBffComposition>(new StubBffComposition());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddFluentUIComponents();

        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-audit-grid']");

        // Page alpha forward so it holds both a cursor and a page-two validator.
        cut.Find("[data-testid='tenants-audit-next']").Click();
        cut.WaitForAssertion(() => requests.Count.ShouldBe(2));
        await subscription.Received(1).SubscribeAsync(
            GetTenantAuditQuery.ProjectionType,
            "tenant.alpha",
            Arg.Any<CancellationToken>());

        // Route to beta. The unsubscribe never completes, so the tenant-change block stays suspended...
        cut.Render(parameters => parameters.Add(p => p.TenantId, "tenant.beta"));

        // ...and a second parameter set for the same route re-enters while it is still suspended.
        cut.Render(parameters => parameters.Add(p => p.TenantId, "tenant.beta"));

        cut.WaitForAssertion(() => requests.Count(static request => request.TenantId == "tenant.beta")
            .ShouldBeGreaterThanOrEqualTo(1));
        foreach (TenantAuditRequest betaRequest in requests.Where(static request => request.TenantId == "tenant.beta"))
        {
            betaRequest.ETag.ShouldBeNull();
            betaRequest.Cursor.ShouldBeNull();
        }

        suspendedUnsubscribe.SetResult();
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-audit-grid']")
            .TextContent.ShouldContain("tenant.beta-event"));
        requests.Where(static request => request.TenantId == "tenant.beta")
            .ShouldAllBe(request => request.ETag == null && request.Cursor == null);
    }

    /// <summary>
    /// The snapshot lifecycle and the row lifecycle are independent and must not collapse into each other.
    /// </summary>
    /// <remarks>
    /// Production derives the surface lifecycle from response metadata and each row's from the row itself,
    /// so a projection that metadata reports as not current while the retained rows still carry Current is a
    /// real, renderable state. No test in this file could express it: the row factory hardcoded
    /// <c>Current</c> and the snapshot factory derived its lifecycle from <c>rows[0]</c>.
    /// </remarks>
    [Fact]
    public void Audit_surface_lifecycle_is_rendered_independently_of_the_row_lifecycle()
    {
        // Freshness is derived FROM lifecycle by TenantQueryGateway.ResolveFreshness (Current => Current,
        // Stale => Stale, everything else => Unknown), so the helper's default Current freshness paired with
        // a Rebuilding lifecycle is a snapshot the gateway cannot emit. Pinning badge independence over an
        // unproducible input proves nothing about any state a user can reach -- and a change that gated the
        // surface badge on freshness, which is what this guards, would still have passed.
        RegisterServices(ReadySnapshot(
            [Row("event-1", AuditEventCategory.Access, lifecycle: ProjectionLifecycleState.Current)],
            lifecycle: ProjectionLifecycleState.Rebuilding) with
        {
            Freshness = ReadModelFreshnessState.Unknown,
        });

        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-audit-grid']");

        cut.Find("[data-testid='tenants-audit-projection-lifecycle-status']")
            .TextContent.ShouldContain("Rebuilding", Case.Insensitive);
        cut.Find("[data-testid='tenants-audit-row-projection-lifecycle']")
            .TextContent.ShouldContain("Current", Case.Insensitive);
    }

    /// <summary>
    /// A Previous click that lands on page one because the history was trimmed must say so.
    /// </summary>
    /// <remarks>
    /// <c>CursorHistory.Trim</c> re-appends the first-page sentinel beneath the newest entries, which is what
    /// keeps page one reachable -- but it also means one Previous click walks the operator from the middle of
    /// the sequence straight to page one. Rendered as an ordinary one-page step back, the surface silently
    /// misstates where they are. This also pins adoption of the trim at this call site: deleting the
    /// <c>CursorHistory.Trim(...)</c> call previously survived the suite.
    /// </remarks>
    [Fact]
    public void A_previous_click_that_jumps_to_page_one_through_a_trimmed_history_is_announced()
    {
        // Bound taken from the production constant. Duplicating it as a literal meant a change to
        // CursorHistory.DefaultMaximum made this walk the wrong number of steps and fail with
        // "previous is not disabled" -- a diagnosis that names nothing.
        const int bound = CursorHistory.DefaultMaximum;
        JSInterop.Mode = JSRuntimeMode.Loose;
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.GetTenantAuditAsync(Arg.Any<TenantAuditRequest>(), Arg.Any<TenantAuditSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                TenantAuditRequest request = call.Arg<TenantAuditRequest>()!;
                int page = request.Cursor is null
                    ? 0
                    : int.Parse(request.Cursor["page-".Length..], CultureInfo.InvariantCulture);
                return Task.FromResult(ReadySnapshot(
                    [Row($"event-page-{page}", AuditEventCategory.Access)],
                    nextCursor: $"page-{page + 1}",
                    hasMore: true,
                    requestCursor: request.Cursor));
            });
        Services.AddSingleton(gateway);
        Services.AddSingleton<ITenantsBffComposition>(new StubBffComposition());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddFluentUIComponents();

        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-audit-grid']");

        // One page past the bound, so the trim runs and drops the oldest non-sentinel entries.
        for (int page = 1; page <= bound + 1; page++)
        {
            cut.Find("[data-testid='tenants-audit-next']").Click();
            cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-audit-grid']")
                .TextContent.ShouldContain($"event-page-{page}"));
        }

        cut.FindAll("[data-testid='tenants-audit-history-truncated']").ShouldBeEmpty(
            "Paging forward is not a jump; the notice belongs to the Previous click that lands on page one.");

        // Walk back. The retained history is 49 entries plus the re-appended sentinel, so the last of these
        // pops the sentinel and lands on page one from the middle of the sequence.
        for (int step = 1; step < bound; step++)
        {
            cut.Find("[data-testid='tenants-audit-previous']").Click();
            cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-audit-previous']")
                .HasAttribute("disabled").ShouldBeFalse());
            cut.FindAll("[data-testid='tenants-audit-history-truncated']").ShouldBeEmpty();
        }

        cut.Find("[data-testid='tenants-audit-previous']").Click();
        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='tenants-audit-grid']").TextContent.ShouldContain("event-page-0");
            cut.Find("[data-testid='tenants-audit-previous']").HasAttribute("disabled").ShouldBeTrue();
        });

        IElement notice = cut.Find("[data-testid='tenants-audit-history-truncated']");
        notice.GetAttribute("role").ShouldBe("status");
        notice.GetAttribute("aria-live").ShouldBe("polite");
        notice.TextContent.ShouldContain("first page");

        // Paging forward again retires the notice: the operator is no longer on the jumped-to page.
        cut.Find("[data-testid='tenants-audit-next']").Click();
        cut.WaitForAssertion(() => cut.FindAll("[data-testid='tenants-audit-history-truncated']").ShouldBeEmpty());
    }
}
