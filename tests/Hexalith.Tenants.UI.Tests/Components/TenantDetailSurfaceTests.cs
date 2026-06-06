using System.Globalization;
using System.Text.RegularExpressions;

using AngleSharp.Dom;

using Bunit;

using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Pages;
using Hexalith.Tenants.UI.Components.Tenants;
using Hexalith.Tenants.UI.Components.Tenants.Members;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class TenantDetailSurfaceTests : BunitContext
{
    private static readonly string[] AllowedConfigurationCopyKinds =
    [
        "ConfigurationKey",
        "SafeConfigurationValue",
    ];

    [Fact]
    public void Detail_page_loads_through_gateway_and_renders_operational_overview()
    {
        List<TenantDetailRequest> requests = [];
        RegisterServices(call =>
        {
            requests.Add(call.ArgAt<TenantDetailRequest>(0));
            return Task.FromResult(TenantDetailSnapshot.Ready(Detail("tenant.alpha"), "\"etag\"", TenantFreshnessState.Current));
        });

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo("/tenants/tenant.alpha?returnUrl=%2Ftenants%3Fsearch%3Dalpha%26selected%3Dtenant.alpha");

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-detail-identity']");

        requests.ShouldHaveSingleItem().TenantId.ShouldBe("tenant.alpha");
        cut.Find("[data-testid='tenants-detail-back']").GetAttribute("href").ShouldBe("/tenants?search=alpha&selected=tenant.alpha");
        cut.Find("[data-testid='tenants-detail-truth-state']").TextContent.ShouldContain("Current");
        cut.Find("[data-testid='tenants-detail-identity']").TextContent.ShouldContain("tenant.alpha");
        cut.Find("[data-testid='tenants-edit-metadata-flow']").TextContent.ShouldContain("Alpha");
        cut.Find("[data-testid='tenants-detail-copy-reference']").GetAttribute("data-copy-kind").ShouldBe("TenantId");
        cut.Find("[data-testid='tenants-detail-copy-reference']").TextContent.ShouldContain("Copy");
        cut.Find("[data-testid='tenants-lifecycle-actions']");
        cut.Find("[data-testid='tenants-lifecycle-current-status']").TextContent.ShouldContain("Active");
        cut.Find("[data-testid='tenants-lifecycle-unavailable-reason']").TextContent.ShouldContain("TenantLifecycleStateAlreadySet");
        cut.Find("[data-testid='tenants-detail-member-summary']").TextContent.ShouldContain("2 members");
        cut.Find("[data-testid='tenants-detail-configuration-summary']").TextContent.ShouldContain("1 configuration keys");
        cut.Find("[data-testid='tenants-config-set-flow']");
        cut.Find("[data-testid='tenants-config-table']").TextContent.ShouldContain("billing.mode");
        cut.Markup.ShouldContain("aria-label=\"Full tenant identifier tenant.alpha\"");
        cut.Markup.ShouldContain("aria-label=\"Tenant status Active\"");
    }

    [Fact]
    public void Detail_page_displays_loading_until_gateway_completes()
    {
        TaskCompletionSource<TenantDetailSnapshot> detailResult = new(TaskCreationOptions.RunContinuationsAsynchronously);
        RegisterServices(_ => detailResult.Task);

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));

        cut.Find("[data-testid='tenants-detail-loading']").TextContent.ShouldContain("loading", Case.Insensitive);

        detailResult.SetResult(TenantDetailSnapshot.Ready(Detail("tenant.alpha"), "\"etag\"", TenantFreshnessState.Current));

        cut.WaitForElement("[data-testid='tenants-detail-identity']");
        cut.Find("[data-testid='tenants-detail-identity']").TextContent.ShouldContain("tenant.alpha");
    }

    [Theory]
    [InlineData("https%3A%2F%2Fexample.test%2Ftenants")]
    [InlineData("%2F%2Fevil.test%2Ftenants")]
    [InlineData("%2Fglobal-administrators")]
    public void Detail_page_rejects_unsafe_return_url(string encodedReturnUrl)
    {
        RegisterServices(_ => Task.FromResult(TenantDetailSnapshot.Ready(
            Detail("tenant.alpha"),
            "\"etag\"",
            TenantFreshnessState.Current)));

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo($"/tenants/tenant.alpha?returnUrl={encodedReturnUrl}");

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-detail-identity']");

        cut.Find("[data-testid='tenants-detail-back']").GetAttribute("href").ShouldBe("/tenants");
    }

    [Theory]
    [InlineData(TenantDetailSurfaceKind.Stale, "tenants-detail-stale", "stale")]
    [InlineData(TenantDetailSurfaceKind.Degraded, "tenants-detail-degraded", "degraded")]
    [InlineData(TenantDetailSurfaceKind.Unauthorized, "tenants-detail-unauthorized", "authorized")]
    [InlineData(TenantDetailSurfaceKind.NotFound, "tenants-detail-error", "not found")]
    [InlineData(TenantDetailSurfaceKind.Unavailable, "tenants-detail-error", "unavailable")]
    [InlineData(TenantDetailSurfaceKind.Unknown, "tenants-detail-error", "unavailable")]
    public void Detail_page_renders_distinct_safe_states(
        TenantDetailSurfaceKind kind,
        string selector,
        string expectedText)
    {
        RegisterServices(_ => Task.FromResult(Snapshot(kind)));

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));
        cut.WaitForElement($"[data-testid='{selector}']");

        cut.Find($"[data-testid='{selector}']").TextContent.ShouldContain(expectedText, Case.Insensitive);
        if (kind is TenantDetailSurfaceKind.Unauthorized or TenantDetailSurfaceKind.NotFound or TenantDetailSurfaceKind.Unavailable or TenantDetailSurfaceKind.Unknown)
        {
            cut.Markup.ShouldNotContain("Tenant alpha description");
            cut.Markup.ShouldNotContain("tenants-config-table");
        }
    }

    [Fact]
    public void Configuration_view_groups_namespaces_redacts_sensitive_values_and_preserves_accessible_literals()
    {
        RegisterServices(_ => Task.FromResult(TenantDetailSnapshot.Ready(Detail("tenant.alpha"), "\"etag\"", TenantFreshnessState.Current)));
        TenantDetail detail = Detail("tenant.alpha", new Dictionary<string, string>
        {
            ["billing.mode"] = "trial",
            ["billing.endpoint"] = "Bearer raw-token",
            ["billing.connectionString"] = "Server=secret-host;Password=hidden",
            ["feature"] = "enabled",
        });

        IRenderedComponent<TenantConfigurationView> cut = Render<TenantConfigurationView>(parameters => parameters
            .Add(view => view.Detail, detail)
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, TenantFreshnessState.Current));

        cut.FindAll("[data-testid='tenants-config-group']").Count.ShouldBe(2);
        cut.Find("[data-testid='tenants-config-table']").TextContent.ShouldContain("billing.mode");
        cut.Find("[data-testid='tenants-config-table']").TextContent.ShouldContain("feature");
        cut.Find("[data-testid='tenants-config-table']").TextContent.ShouldContain("Other");
        cut.Find("[data-testid='tenants-config-table']").TextContent.ShouldContain("Unavailable");
        cut.Markup.ShouldContain("data-testid=\"tenants-config-key\"");
        cut.Markup.ShouldContain("data-testid=\"tenants-config-value-state\"");
        cut.FindAll("[data-testid='tenants-config-copy-reference']").Count.ShouldBe(4);
        cut.FindAll("[data-testid='tenants-config-copy-reference']")
            .ShouldAllBe(static copy => AllowedConfigurationCopyKinds.Contains(copy.GetAttribute("data-copy-kind"), StringComparer.Ordinal));
        cut.Markup.ShouldContain("aria-label=\"Full configuration key billing.mode\"");
        cut.Markup.ShouldContain("aria-label=\"Visible configuration value trial\"");
        cut.Markup.ShouldNotContain("secret-host");
        cut.Markup.ShouldNotContain("Password=hidden");
        cut.Markup.ShouldNotContain("raw-token");
        cut.Find("[data-testid='tenants-config-set-flow']");
        cut.FindAll("[data-testid='tenants-config-remove-open']").Count.ShouldBe(4);
        cut.FindAll("[data-testid='tenants-config-remove-open']")
            .ShouldAllBe(static action => action.GetAttribute("type") == "button");
        cut.Markup.ShouldNotContain("tenants-config-remove-flow");
    }

    [Fact]
    public void Configuration_view_launches_remove_flow_from_visible_rows_without_hiding_read_or_set_context()
    {
        RegisterServices(_ => Task.FromResult(TenantDetailSnapshot.Ready(Detail("tenant.alpha"), "\"etag\"", TenantFreshnessState.Current)));
        TenantDetail detail = Detail("tenant.alpha", new Dictionary<string, string>
        {
            ["billing.mode"] = "trial",
            ["identity.region"] = "eu",
        });

        IRenderedComponent<TenantConfigurationView> cut = Render<TenantConfigurationView>(parameters => parameters
            .Add(view => view.Detail, detail)
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, TenantFreshnessState.Current));

        cut.FindAll("[data-testid='tenants-config-remove-open']").First().Click();

        cut.Find("[data-testid='tenants-config-remove-flow']");
        cut.Find("[data-testid='tenants-config-remove-preview']").TextContent.ShouldContain("billing.mode");
        cut.Find("[data-testid='tenants-config-table']").TextContent.ShouldContain("billing.mode");
        cut.Find("[data-testid='tenants-config-table']").TextContent.ShouldContain("identity.region");
        cut.Find("[data-testid='tenants-config-set-flow']");
    }

    [Fact]
    public void Configuration_view_returns_focus_to_launching_remove_control_on_cancel()
    {
        RegisterServices(_ => Task.FromResult(TenantDetailSnapshot.Ready(Detail("tenant.alpha"), "\"etag\"", TenantFreshnessState.Current)));
        TenantDetail detail = Detail("tenant.alpha", new Dictionary<string, string>
        {
            ["billing.mode"] = "trial",
            ["identity.region"] = "eu",
        });

        IRenderedComponent<TenantConfigurationView> cut = Render<TenantConfigurationView>(parameters => parameters
            .Add(view => view.Detail, detail)
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, TenantFreshnessState.Current));

        cut.FindAll("[data-testid='tenants-config-remove-open']").First().Click();
        cut.Find("[data-testid='tenants-config-remove-flow']");
        int focusInvocationCount = JSInterop.Invocations.Count(static invocation =>
            invocation.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase));

        cut.Find("[data-testid='tenants-config-remove-cancel']").Click();

        cut.FindAll("[data-testid='tenants-config-remove-flow']").ShouldBeEmpty();
        cut.WaitForAssertion(() => JSInterop.Invocations.Count(static invocation =>
            invocation.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase)).ShouldBeGreaterThan(focusInvocationCount));
    }

    [Fact]
    public void Configuration_view_redacts_backend_error_metadata_correlation_ids_tokens_stack_traces_and_pii()
    {
        RegisterServices(_ => Task.FromResult(TenantDetailSnapshot.Ready(Detail("tenant.alpha"), "\"etag\"", TenantFreshnessState.Current)));
        TenantDetail detail = Detail("tenant.alpha", new Dictionary<string, string>
        {
            ["billing.mode"] = "trial",
            ["diagnostics.metadata"] = "EventStore metadata page=raw-cursor",
            ["diagnostics.correlation"] = "correlation-123",
            ["diagnostics.failure"] = "System.InvalidOperationException: stack trace with internal frame",
            ["identity.contact"] = "jane.doe@example.test",
            ["security.jwt"] = "eyJhbGciOiJIUzI1NiJ9.raw.payload",
        });

        IRenderedComponent<TenantConfigurationView> cut = Render<TenantConfigurationView>(parameters => parameters
            .Add(view => view.Detail, detail)
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, TenantFreshnessState.Current));

        cut.Find("[data-testid='tenants-config-table']").TextContent.ShouldContain("billing.mode");
        cut.Find("[data-testid='tenants-config-table']").TextContent.ShouldContain("trial");
        cut.FindAll("[data-testid='tenants-config-copy-reference']").Count.ShouldBe(2);
        cut.FindAll("[data-testid='tenants-config-value-state']").Count(item => item.TextContent.Contains("Unavailable", StringComparison.Ordinal)).ShouldBe(5);
        cut.Markup.ShouldNotContain("EventStore metadata", Case.Insensitive);
        cut.Markup.ShouldNotContain("raw-cursor", Case.Insensitive);
        cut.Markup.ShouldNotContain("correlation-123", Case.Insensitive);
        cut.Markup.ShouldNotContain("InvalidOperationException", Case.Insensitive);
        cut.Markup.ShouldNotContain("stack trace", Case.Insensitive);
        cut.Markup.ShouldNotContain("jane.doe@example.test", Case.Insensitive);
        cut.Markup.ShouldNotContain("eyJhbGciOiJIUzI1NiJ9", Case.Insensitive);
    }

    [Fact]
    public void Configuration_view_keeps_empty_and_filtered_empty_states_distinct()
    {
        RegisterServices(_ => Task.FromResult(TenantDetailSnapshot.Ready(Detail("tenant.alpha"), "\"etag\"", TenantFreshnessState.Current)));
        IRenderedComponent<TenantConfigurationView> empty = Render<TenantConfigurationView>(parameters => parameters
            .Add(view => view.Detail, Detail("tenant.alpha", new Dictionary<string, string>()))
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, TenantFreshnessState.Current));

        empty.Find("[data-testid='tenants-config-empty']").TextContent.ShouldContain("No visible configuration");
        empty.Markup.ShouldNotContain("tenants-config-filtered-empty");

        IRenderedComponent<TenantConfigurationView> filtered = Render<TenantConfigurationView>(parameters => parameters
            .Add(view => view.Detail, Detail("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, TenantFreshnessState.Current));

        filtered.Find("[data-testid='tenants-config-filter']").Change("missing");

        filtered.Find("[data-testid='tenants-config-filtered-empty']").TextContent.ShouldContain("No visible configuration matches");
        filtered.Find("[data-testid='tenants-config-announcer']").TextContent.ShouldContain("0 visible configuration entries");
        filtered.Find("[data-testid='tenants-config-clear-filter']").Click();
        filtered.Find("[data-testid='tenants-config-table']").TextContent.ShouldContain("billing.mode");
    }

    [Fact]
    public void Configuration_view_preserves_namespace_context_scope_freshness_and_keyboard_semantics_while_filtering()
    {
        RegisterServices(_ => Task.FromResult(TenantDetailSnapshot.Ready(Detail("tenant.alpha"), "\"etag\"", TenantFreshnessState.Current)));
        IRenderedComponent<TenantConfigurationView> cut = Render<TenantConfigurationView>(parameters => parameters
            .Add(view => view.Detail, Detail("tenant.alpha", new Dictionary<string, string>
            {
                ["billing.mode"] = "trial",
                ["identity.region"] = "eu",
            }))
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, TenantFreshnessState.Current));

        cut.Find("[data-testid='tenants-config-filter']").Change("billing");

        cut.FindAll("[data-testid='tenants-config-group']").Count.ShouldBe(1);
        cut.Find("[data-testid='tenants-config-table']").TextContent.ShouldContain("Namespace billing");
        cut.Find("[data-testid='tenants-config-table']").TextContent.ShouldContain("billing.mode");
        cut.Find("[data-testid='tenants-config-table']").TextContent.ShouldNotContain("identity.region");
        cut.Find("[data-testid='tenants-config-truth-state']").TextContent.ShouldContain("Current");
        cut.Find(".tenant-config__scope").TextContent.ShouldContain("Prefix ownership cannot be verified");
        cut.Find("[data-testid='tenants-config-announcer']").TextContent.ShouldContain("1 visible configuration entries");
        cut.Find("[data-testid='tenants-config-filter']").GetAttribute("aria-describedby").ShouldBe("tenants-config-filter-help");
        cut.Find("[data-testid='tenants-config-clear-filter']").GetAttribute("type").ShouldBe("button");
        cut.Find("[data-testid='tenants-config-row']").GetAttribute("tabindex").ShouldBe("0");
    }

    [Theory]
    [InlineData(TenantDetailSurfaceKind.Stale, TenantFreshnessState.Stale, "stale")]
    [InlineData(TenantDetailSurfaceKind.Degraded, TenantFreshnessState.Unknown, "degraded")]
    [InlineData(TenantDetailSurfaceKind.Unknown, TenantFreshnessState.Unknown, "Unknown")]
    public void Configuration_view_surfaces_non_current_truth_without_collapsing_to_success(
        TenantDetailSurfaceKind kind,
        TenantFreshnessState freshness,
        string expectedText)
    {
        RegisterServices(_ => Task.FromResult(TenantDetailSnapshot.Ready(Detail("tenant.alpha"), "\"etag\"", TenantFreshnessState.Current)));

        IRenderedComponent<TenantConfigurationView> cut = Render<TenantConfigurationView>(parameters => parameters
            .Add(view => view.Detail, Detail("tenant.alpha"))
            .Add(view => view.SurfaceKind, kind)
            .Add(view => view.Freshness, freshness));

        cut.Find("[data-testid='tenants-config-truth-state']").TextContent.ShouldContain(expectedText, Case.Insensitive);
        cut.Find("[data-testid='tenants-config-command-unavailable']").TextContent.ShouldContain("unavailable", Case.Insensitive);
        cut.Markup.ShouldNotContain("Success");
    }

    [Fact]
    public void Detail_page_composes_member_access_review_without_replacing_existing_surfaces()
    {
        RegisterServices(_ => Task.FromResult(TenantDetailSnapshot.Ready(Detail("tenant.alpha"), "\"etag\"", TenantFreshnessState.Current)));

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-member-table']");

        cut.Find("[data-testid='tenants-detail-member-summary']").TextContent.ShouldContain("2 members");
        cut.Find("[data-testid='tenants-member-section']").TextContent.ShouldContain("Member access review");
        cut.Find("[data-testid='tenants-member-table']").TextContent.ShouldContain("owner-user");
        cut.Find("[data-testid='tenants-config-table']").TextContent.ShouldContain("billing.mode");
        cut.Markup.ShouldContain("data-testid=\"tenants-member-truth-badge\"");
        cut.Markup.ShouldNotContain("tenants-list-truth-state");
    }

    [Fact]
    public void Detail_page_keeps_member_actions_fail_closed_when_routed_detail_is_stale()
    {
        RegisterServices(_ => Task.FromResult(TenantDetailSnapshot.Stale(Detail("tenant.alpha"), "\"etag\"")));

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-member-table']");

        cut.Find("[data-testid='tenants-detail-stale']").TextContent.ShouldContain("stale", Case.Insensitive);
        IElement memberSection = cut.Find("[data-testid='tenants-member-section']");
        memberSection.TextContent.ShouldContain("Member evidence is stale.");
        cut.Find("[data-testid='tenants-member-table']").TextContent.ShouldContain("owner-user");
        cut.Find("[data-testid='tenants-member-table']").TextContent.ShouldContain("stale data");
        cut.Find("[data-testid='tenants-add-member-flow']").TextContent.ShouldContain("Refresh current tenant detail");
        cut.Find("[data-testid='tenants-add-member-submit']").GetAttribute("disabled").ShouldNotBeNull();
        cut.FindAll("[data-testid='tenants-member-action-slot']")
            .ShouldAllBe(static slot => slot.TextContent.Contains("Unavailable", StringComparison.OrdinalIgnoreCase));
        memberSection.InnerHtml.ShouldNotContain("Success");
    }

    [Fact]
    public void Member_access_review_renders_literal_members_roles_context_and_accessible_table_semantics()
    {
        RegisterComponentServices();
        TenantDetail detail = Detail(
            "tenant.alpha",
            new Dictionary<string, string>(),
            TenantStatus.Active,
            [
                new TenantMember("OWNER/User.01", TenantRole.TenantOwner),
                new TenantMember("reader-user-with-a-very-long-literal-identifier", TenantRole.TenantReader),
                new TenantMember("contributor-user", TenantRole.TenantContributor),
                new TenantMember("unknown-role-user", TenantRole.Unknown),
            ]);

        IRenderedComponent<MemberAccessReview> cut = Render<MemberAccessReview>(parameters => parameters
            .Add(view => view.Detail, detail)
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, TenantFreshnessState.Current));

        cut.Find("[data-testid='tenants-member-table']").TextContent.ShouldContain("OWNER/User.01");
        cut.Find("[data-testid='tenants-member-table']").TextContent.ShouldContain("reader-user-with-a-very-long-literal-identifier");
        cut.FindAll("[data-testid='tenants-member-copy-reference']").Count.ShouldBe(4);
        cut.FindAll("[data-testid='tenants-member-copy-reference']").ShouldAllBe(static copy => copy.GetAttribute("data-copy-kind") == "UserId");
        cut.FindAll("[data-testid='tenants-member-row']").Count.ShouldBe(4);
        cut.FindAll("th[scope='row'][data-testid='tenants-member-user-id']").Count.ShouldBe(4);
        cut.FindAll("th[scope='col']").Count.ShouldBeGreaterThanOrEqualTo(6);
        cut.Find("[data-testid='tenants-member-table']").TextContent.ShouldContain("Tenant owner");
        cut.Find("[data-testid='tenants-member-table']").TextContent.ShouldContain("Tenant contributor");
        cut.Find("[data-testid='tenants-member-table']").TextContent.ShouldContain("Tenant reader");
        cut.Find("[data-testid='tenants-member-table']").TextContent.ShouldContain("Unknown role");
        cut.Find("[data-testid='tenants-member-table']").TextContent.ShouldContain("1 visible owner");
        cut.Find("[data-testid='tenants-member-truth-state']").TextContent.ShouldContain("Current");
        cut.Markup.ShouldContain("aria-describedby=\"tenants-member-reasons-0\"");
        cut.Markup.ShouldContain("aria-label=\"Literal member user identifier OWNER/User.01\"");
        cut.Find("[data-testid='tenants-member-reason-list']").GetAttribute("tabindex").ShouldBe("0");
    }

    [Fact]
    public void Member_access_review_associates_every_action_slot_with_its_row_reason_list()
    {
        RegisterComponentServices();
        TenantDetail detail = Detail(
            "tenant.alpha",
            new Dictionary<string, string>(),
            TenantStatus.Active,
            [
                new TenantMember("owner-user", TenantRole.TenantOwner),
                new TenantMember("reader-user", TenantRole.TenantReader),
            ]);

        IRenderedComponent<MemberAccessReview> cut = Render<MemberAccessReview>(parameters => parameters
            .Add(view => view.Detail, detail)
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, TenantFreshnessState.Current));

        HashSet<string> reasonListIds = cut.FindAll("[data-testid='tenants-member-reason-list']")
            .Select(static list => list.GetAttribute("id").ShouldNotBeNull())
            .ToHashSet(StringComparer.Ordinal);

        reasonListIds.Count.ShouldBe(2);
        cut.FindAll("[data-testid='tenants-member-action-slot']").Count.ShouldBe(2);
        cut.FindAll("[data-testid='tenants-change-role-open']").Count.ShouldBe(2);
        cut.FindAll("[data-testid='tenants-remove-member-open']").Count.ShouldBe(2);
        foreach (IElement slot in cut.FindAll("[data-testid='tenants-member-action-slot']"))
        {
            string describedBy = slot.GetAttribute("aria-describedby").ShouldNotBeNull();
            reasonListIds.ShouldContain(describedBy);
            slot.GetAttribute("aria-label").ShouldNotBeNull().ShouldContain("unavailable");
        }

        cut.FindAll("[data-testid='tenants-change-role-open']")
            .ShouldAllBe(static button => button.GetAttribute("aria-controls") == "tenants-change-role-flow-region");
        cut.FindAll("[data-testid='tenants-remove-member-open']")
            .ShouldAllBe(static button => button.GetAttribute("aria-controls") == "tenants-remove-member-flow-region");
        cut.FindAll("[data-testid='tenants-member-row']")
            .ShouldAllBe(static row => row.GetAttribute("tabindex") == "0");
    }

    [Fact]
    public void Member_access_review_surfaces_all_canonical_unavailable_reason_categories_without_mutation_affordances()
    {
        RegisterComponentServices();
        IRenderedComponent<MemberAccessReview> cut = Render<MemberAccessReview>(parameters => parameters
            .Add(view => view.Detail, Detail("tenant.alpha"))
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, TenantFreshnessState.Current));

        string[] categories =
        [
            "missing permission",
            "stale data",
            "missing lifecycle support",
            "missing consequence preview",
            "missing audit proof",
            "high-impact flow not ready",
        ];

        foreach (string category in categories)
        {
            cut.Find($"[data-testid='tenants-member-reason-{category.Replace(' ', '-')}']")
                .TextContent.ShouldContain(category);
        }

        cut.FindAll("[data-testid='tenants-member-action-slot']").Count.ShouldBeGreaterThanOrEqualTo(2);
        cut.FindAll("[data-testid='tenants-member-unavailable-reason']").Count.ShouldBeGreaterThanOrEqualTo(4);
        cut.Find("[data-testid='tenants-add-member-flow']");
        cut.Find("[data-testid='tenants-add-member-submit']").GetAttribute("type").ShouldBe("submit");
        cut.FindAll("[data-testid='tenants-change-role-open']").Count.ShouldBe(2);
        cut.FindAll("[data-testid='tenants-remove-member-open']").Count.ShouldBe(2);
        cut.FindAll("[data-testid='tenants-member-action-slot']")
            .ShouldAllBe(static slot => slot.TextContent.Contains("Unavailable", StringComparison.OrdinalIgnoreCase));
        cut.Markup.ShouldNotContain("command payload", Case.Insensitive);
        cut.Markup.ShouldNotContain("accepted", Case.Insensitive);
        cut.Markup.ShouldNotContain("confirmed", Case.Insensitive);
    }

    [Fact]
    public void Member_access_review_opens_change_role_flow_without_removing_add_or_remove_action_slots()
    {
        RegisterComponentServices();
        IRenderedComponent<MemberAccessReview> cut = Render<MemberAccessReview>(parameters => parameters
            .Add(view => view.Detail, Detail("tenant.alpha"))
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, TenantFreshnessState.Current));

        cut.Find("[data-testid='tenants-change-role-open']").Click();

        cut.Find("[data-testid='tenants-change-role-flow']");
        cut.Find("[data-testid='tenants-change-role-user-id']").TextContent.ShouldContain("owner-user");
        cut.Find("[data-testid='tenants-change-role-current-role']").TextContent.ShouldContain("Tenant owner");
        cut.Find("[data-testid='tenants-change-role-new-role']");
        cut.Find("[data-testid='tenants-change-role-submit']").GetAttribute("type").ShouldBe("submit");
        cut.Find("[data-testid='tenants-change-role-lifecycle']");
        cut.FindAll("[data-testid='tenants-member-action-slot']").Count.ShouldBe(2);
        cut.FindAll("[data-testid='tenants-remove-member-open']").Count.ShouldBe(2);
        cut.Find("[data-testid='tenants-add-member-flow']");
        cut.Find("[data-testid='tenants-add-member-submit']").GetAttribute("type").ShouldBe("submit");
        cut.Markup.ShouldContain("Remove member", Case.Insensitive);
    }

    [Theory]
    [InlineData(TenantStatus.Disabled, TenantDetailSurfaceKind.Ready, TenantFreshnessState.Current, "missing lifecycle support")]
    [InlineData(TenantStatus.Active, TenantDetailSurfaceKind.Stale, TenantFreshnessState.Stale, "stale data")]
    [InlineData(TenantStatus.Active, TenantDetailSurfaceKind.Ready, TenantFreshnessState.Unknown, "stale data")]
    [InlineData(TenantStatus.Active, TenantDetailSurfaceKind.Degraded, TenantFreshnessState.Unknown, "missing permission")]
    [InlineData(TenantStatus.Unknown, TenantDetailSurfaceKind.Ready, TenantFreshnessState.Current, "missing lifecycle support")]
    public void Member_access_review_fails_closed_for_disabled_stale_unknown_and_degraded_states(
        TenantStatus status,
        TenantDetailSurfaceKind surfaceKind,
        TenantFreshnessState freshness,
        string expectedReason)
    {
        RegisterComponentServices();
        IRenderedComponent<MemberAccessReview> cut = Render<MemberAccessReview>(parameters => parameters
            .Add(view => view.Detail, Detail(
                "tenant.alpha",
                new Dictionary<string, string>(),
                status,
                [new TenantMember("owner-user", TenantRole.TenantOwner)]))
            .Add(view => view.SurfaceKind, surfaceKind)
            .Add(view => view.Freshness, freshness));

        cut.Find("[data-testid='tenants-member-section']").TextContent.ShouldContain(expectedReason);
        cut.FindAll("[data-testid='tenants-member-action-slot']")
            .ShouldAllBe(static slot => slot.TextContent.Contains("Unavailable", StringComparison.OrdinalIgnoreCase));
        cut.Markup.ShouldNotContain("Success");
    }

    [Theory]
    [InlineData(TenantDetailSurfaceKind.Unauthorized)]
    [InlineData(TenantDetailSurfaceKind.Unavailable)]
    [InlineData(TenantDetailSurfaceKind.Unknown)]
    public void Member_access_review_fails_closed_for_unsafe_detail_authorization_states(TenantDetailSurfaceKind surfaceKind)
    {
        RegisterComponentServices();
        IRenderedComponent<MemberAccessReview> cut = Render<MemberAccessReview>(parameters => parameters
            .Add(view => view.Detail, Detail("tenant.alpha"))
            .Add(view => view.SurfaceKind, surfaceKind)
            .Add(view => view.Freshness, TenantFreshnessState.Current));

        cut.Find("[data-testid='tenants-member-table']").TextContent.ShouldContain("missing permission");
        cut.FindAll("[data-testid='tenants-member-action-slot']")
            .ShouldAllBe(static slot => slot.TextContent.Contains("Unavailable", StringComparison.OrdinalIgnoreCase));
        cut.Markup.ShouldNotContain("accepted", Case.Insensitive);
        cut.Markup.ShouldNotContain("confirmed", Case.Insensitive);
    }

    [Fact]
    public void Member_access_review_does_not_render_backend_urls_tokens_payloads_or_command_lifecycle_context()
    {
        RegisterComponentServices();
        IRenderedComponent<MemberAccessReview> cut = Render<MemberAccessReview>(parameters => parameters
            .Add(view => view.Detail, Detail(
                "tenant.alpha",
                new Dictionary<string, string>(),
                TenantStatus.Active,
                [new TenantMember("literal-user", TenantRole.TenantOwner)]))
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, TenantFreshnessState.Current));

        cut.Find("[data-testid='tenants-member-table']").TextContent.ShouldContain("literal-user");
        cut.Markup.ShouldNotContain("/api/tenants", Case.Insensitive);
        cut.Markup.ShouldNotContain("Bearer ", Case.Insensitive);
        cut.Markup.ShouldNotContain("jwt", Case.Insensitive);
        cut.Markup.ShouldNotContain("command payload", Case.Insensitive);
        cut.Markup.ShouldNotContain("correlation", Case.Insensitive);
        cut.Markup.ShouldNotContain("audit available", Case.Insensitive);
        cut.Markup.ShouldNotContain("preview available", Case.Insensitive);
    }

    [Fact]
    public void Member_access_review_renders_authorization_safe_empty_state()
    {
        RegisterComponentServices();
        IRenderedComponent<MemberAccessReview> cut = Render<MemberAccessReview>(parameters => parameters
            .Add(view => view.Detail, Detail(
                "tenant.empty",
                new Dictionary<string, string>(),
                TenantStatus.Active,
                []))
            .Add(view => view.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(view => view.Freshness, TenantFreshnessState.Unknown));

        cut.Find("[data-testid='tenants-member-empty']").TextContent.ShouldContain("No visible members");
        cut.Find("[data-testid='tenants-member-empty']").TextContent.ShouldContain("does not reveal hidden memberships");
        cut.Find("[data-testid='tenants-member-empty']").TextContent.ShouldContain("stale data");
        cut.Markup.ShouldNotContain("tenants-member-row");
    }

    [Fact]
    public void Workspace_detail_link_preserves_list_context_in_return_url()
    {
        TenantListSnapshot snapshot = TenantListSnapshot.Ready(
            [
                TenantListRow.FromSummary(new TenantSummary("tenant.alpha", "Alpha", TenantStatus.Active)),
            ],
            nextCursor: null,
            hasMore: false,
            eTag: "\"etag\"",
            freshness: TenantFreshnessState.Current,
            isDegraded: false);
        RegisterServices(snapshot);

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo("/tenants?search=alpha&status=Active&sort=name&desc=True&cursor=cursor-1");

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-detail-link']");

        string href = cut.Find("[data-testid='tenants-list-detail-link']").GetAttribute("href").ShouldNotBeNull();
        href.ShouldStartWith("/tenants/tenant.alpha?returnUrl=");
        string decoded = Uri.UnescapeDataString(href[(href.IndexOf("returnUrl=", StringComparison.Ordinal) + "returnUrl=".Length)..]);
        decoded.ShouldContain("search=alpha");
        decoded.ShouldContain("status=Active");
        decoded.ShouldContain("sort=name");
        decoded.ShouldContain("desc=True");
        decoded.ShouldContain("cursor=cursor-1");
        decoded.ShouldContain("selected=tenant.alpha");
        decoded.ShouldContain("anchor=tenant-row-tenant.alpha");
    }

    [Fact]
    public void Workspace_restores_return_context_from_query_before_loading_list()
    {
        List<TenantListRequest> requests = [];
        TenantListSnapshot snapshot = TenantListSnapshot.Ready(
            [
                TenantListRow.FromSummary(new TenantSummary("tenant.alpha", "Alpha", TenantStatus.Active)),
                TenantListRow.FromSummary(new TenantSummary("tenant.beta", "Beta", TenantStatus.Disabled)),
            ],
            nextCursor: null,
            hasMore: false,
            eTag: "\"etag\"",
            freshness: TenantFreshnessState.Current,
            isDegraded: false);
        RegisterListServices(call =>
        {
            requests.Add(call.ArgAt<TenantListRequest>(0));
            return Task.FromResult(snapshot);
        });

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo("/tenants?search=beta&status=Disabled&sort=name&desc=True&cursor=cursor-1&selected=tenant.beta&anchor=tenant-row-tenant.beta");

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-return-context']");

        TenantListRequest request = requests.ShouldHaveSingleItem();
        request.Search.ShouldBe("beta");
        request.Status.ShouldBe(TenantStatus.Disabled);
        request.SortColumn.ShouldBe(TenantListSortColumns.Name);
        request.SortDescending.ShouldBeTrue();
        request.Cursor.ShouldBe("cursor-1");
        cut.Find("[data-testid='tenants-list-return-context']").TextContent.ShouldContain("tenant.beta");
        cut.Markup.ShouldContain("tenant.beta");
        cut.Markup.ShouldNotContain("tenant.alpha");

        // The generated return anchor (anchor=tenant-row-tenant.beta) must resolve to a real DOM
        // target so focus/scroll restoration has something to land on.
        cut.Find("[id='tenant-row-tenant.beta']").ShouldNotBeNull();
        cut.Find("[id='tenants-list-heading']").GetAttribute("tabindex").ShouldBe("-1");
    }

    [Fact]
    public void Detail_styles_preserve_responsive_safety_and_forced_colors_hooks()
    {
        string projectRoot = ProjectRoot();
        string styles = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Pages",
            "TenantDetailPage.razor.css"));

        styles.ShouldContain("overflow-wrap: anywhere");
        styles.ShouldContain("grid-template-columns: repeat(auto-fit, minmax(12rem, 1fr))");
        styles.ShouldContain("@media (max-width: 767px)");
        styles.ShouldContain("grid-template-columns: minmax(0, 1fr)");
        styles.ShouldContain("@media (forced-colors: active)");
        styles.ShouldContain(":focus-visible");

        string configurationStyles = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Tenants",
            "TenantConfigurationView.razor.css"));

        configurationStyles.ShouldContain("overflow-wrap: anywhere");
        configurationStyles.ShouldContain("grid-template-columns");
        configurationStyles.ShouldContain("@media (max-width: 767px)");
        configurationStyles.ShouldContain("@media (forced-colors: active)");
        configurationStyles.ShouldContain(":focus-visible");

        string memberStyles = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Tenants",
            "Members",
            "MemberAccessReview.razor.css"));

        memberStyles.ShouldContain("overflow-wrap: anywhere");
        memberStyles.ShouldContain("grid-template-columns");
        memberStyles.ShouldContain("min-width");
        memberStyles.ShouldContain("@media (max-width: 767px)");
        memberStyles.ShouldContain("@media (forced-colors: active)");
        memberStyles.ShouldContain(":focus-visible");
        memberStyles.ShouldContain("grid-template-columns: minmax(0, 1fr) auto");

        string lifecycleStyles = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Tenants",
            "Lifecycle",
            "TenantLifecycleActionAvailability.razor.css"));

        lifecycleStyles.ShouldContain("overflow-wrap: anywhere");
        lifecycleStyles.ShouldContain("grid-template-columns");
        lifecycleStyles.ShouldContain("@media (max-width: 767px)");
        lifecycleStyles.ShouldContain("@media (forced-colors: active)");
        lifecycleStyles.ShouldContain(":focus-visible");

        string changeRoleStyles = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Tenants",
            "Members",
            "ChangeTenantMemberRoleFlow.razor.css"));

        changeRoleStyles.ShouldContain("overflow-wrap: anywhere");
        changeRoleStyles.ShouldContain("grid-template-columns");
        changeRoleStyles.ShouldContain("@media (max-width: 767px)");
        changeRoleStyles.ShouldContain("@media (forced-colors: active)");
        changeRoleStyles.ShouldContain(":focus-visible");

        string addMemberStyles = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Tenants",
            "Members",
            "AddTenantMemberFlow.razor.css"));

        addMemberStyles.ShouldContain("overflow-wrap: anywhere");
        addMemberStyles.ShouldContain("grid-template-columns");
        addMemberStyles.ShouldContain("@media (max-width: 767px)");
        addMemberStyles.ShouldContain("@media (forced-colors: active)");
        addMemberStyles.ShouldContain(":focus-visible");

        string copyStyles = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Shared",
            "SupportSafeCopyButton.razor.css"));

        copyStyles.ShouldContain("inline-size");
        copyStyles.ShouldContain(":focus-visible");
        copyStyles.ShouldContain("@media (forced-colors: active)");
        copyStyles.ShouldContain("min-inline-size");
    }

    [Fact]
    public void French_resources_include_detail_and_configuration_keys()
    {
        string projectRoot = ProjectRoot();
        string invariantResources = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Resources",
            "TenantsResources.resx"));
        string frenchResources = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Resources",
            "TenantsResources.fr.resx"));

        frenchResources.ShouldContain("Tenants.Detail.Title");
        frenchResources.ShouldContain("Tenants.Detail.State.Unauthorized.Message");
        frenchResources.ShouldContain("Tenants.Detail.Configuration.Summary");
        invariantResources.ShouldContain("Tenants.Configuration.Title");
        invariantResources.ShouldContain("Tenants.Configuration.State.Unauthorized");
        invariantResources.ShouldContain("Tenants.Configuration.State.Unavailable");
        invariantResources.ShouldContain("Tenants.Configuration.Value.Unavailable");
        frenchResources.ShouldContain("Tenants.Configuration.Title");
        frenchResources.ShouldContain("Tenants.Configuration.State.Unauthorized");
        frenchResources.ShouldContain("Tenants.Configuration.State.Unavailable");
        frenchResources.ShouldContain("Tenants.Configuration.Value.Unavailable");
        invariantResources.ShouldContain("Tenants.Configuration.Set.Title");
        invariantResources.ShouldContain("Tenants.Configuration.Set.State.ProjectionPending");
        frenchResources.ShouldContain("Tenants.Configuration.Set.Title");
        frenchResources.ShouldContain("Tenants.Configuration.Set.State.ProjectionPending");
        invariantResources.ShouldContain("Tenants.Configuration.Remove.Title");
        invariantResources.ShouldContain("Tenants.Configuration.Remove.State.ProjectionPending");
        frenchResources.ShouldContain("Tenants.Configuration.Remove.Title");
        frenchResources.ShouldContain("Tenants.Configuration.Remove.State.ProjectionPending");
        invariantResources.ShouldContain("Tenants.Members.Title");
        invariantResources.ShouldContain("Tenants.Members.UnavailableReason.MissingPermission");
        frenchResources.ShouldContain("Tenants.Members.Title");
        frenchResources.ShouldContain("Tenants.Members.UnavailableReason.MissingPermission");
        invariantResources.ShouldContain("Tenants.Lifecycle.Title");
        invariantResources.ShouldContain("Tenants.Lifecycle.Unavailable.Governance");
        invariantResources.ShouldContain("Tenants.Lifecycle.Unavailable.AlreadyActive");
        frenchResources.ShouldContain("Tenants.Lifecycle.Title");
        frenchResources.ShouldContain("Tenants.Lifecycle.Unavailable.Governance");
        frenchResources.ShouldContain("Tenants.Lifecycle.Unavailable.AlreadyActive");
        invariantResources.ShouldContain("Tenants.AddMember.Title");
        invariantResources.ShouldContain("Tenants.AddMember.State.ProjectionPending");
        frenchResources.ShouldContain("Tenants.AddMember.Title");
        frenchResources.ShouldContain("Tenants.AddMember.State.ProjectionPending");
        invariantResources.ShouldContain("Tenants.ChangeRole.Title");
        invariantResources.ShouldContain("Tenants.ChangeRole.State.AlreadyApplied");
        frenchResources.ShouldContain("Tenants.ChangeRole.Title");
        frenchResources.ShouldContain("Tenants.ChangeRole.State.AlreadyApplied");
        invariantResources.ShouldContain("Tenants.RemoveMember.Title");
        invariantResources.ShouldContain("Tenants.RemoveMember.State.ProjectionPending");
        frenchResources.ShouldContain("Tenants.RemoveMember.Title");
        frenchResources.ShouldContain("Tenants.RemoveMember.State.ProjectionPending");
        invariantResources.ShouldContain("Tenants.Copy.Action");
        invariantResources.ShouldContain("Tenants.Copy.Feedback.Unsafe");
        frenchResources.ShouldContain("Tenants.Copy.Action");
        frenchResources.ShouldContain("Tenants.Copy.Feedback.Unsafe");
    }

    [Fact]
    public void Configuration_resources_have_full_invariant_and_french_parity()
    {
        string projectRoot = ProjectRoot();
        HashSet<string> invariantKeys = ConfigurationResourceKeys(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Resources",
            "TenantsResources.resx"));
        HashSet<string> frenchKeys = ConfigurationResourceKeys(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Resources",
            "TenantsResources.fr.resx"));

        invariantKeys.ShouldNotBeEmpty();
        frenchKeys.ShouldBe(invariantKeys, ignoreOrder: true);
    }

    [Fact]
    public void Member_resources_have_full_invariant_and_french_parity()
    {
        string projectRoot = ProjectRoot();
        HashSet<string> invariantKeys = MemberResourceKeys(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Resources",
            "TenantsResources.resx"));
        HashSet<string> frenchKeys = MemberResourceKeys(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Resources",
            "TenantsResources.fr.resx"));

        invariantKeys.ShouldNotBeEmpty();
        frenchKeys.ShouldBe(invariantKeys, ignoreOrder: true);
    }

    [Fact]
    public void Lifecycle_resources_have_full_invariant_and_french_parity()
    {
        string projectRoot = ProjectRoot();
        HashSet<string> invariantKeys = LifecycleResourceKeys(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Resources",
            "TenantsResources.resx"));
        HashSet<string> frenchKeys = LifecycleResourceKeys(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Resources",
            "TenantsResources.fr.resx"));

        invariantKeys.ShouldNotBeEmpty();
        frenchKeys.ShouldBe(invariantKeys, ignoreOrder: true);
    }

    [Fact]
    public void Copy_resources_have_full_invariant_and_french_parity()
    {
        string projectRoot = ProjectRoot();
        HashSet<string> invariantKeys = CopyResourceKeys(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Resources",
            "TenantsResources.resx"));
        HashSet<string> frenchKeys = CopyResourceKeys(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Resources",
            "TenantsResources.fr.resx"));

        invariantKeys.ShouldNotBeEmpty();
        frenchKeys.ShouldBe(invariantKeys, ignoreOrder: true);
    }

    [Fact]
    public void Add_member_resources_have_full_invariant_and_french_parity()
    {
        string projectRoot = ProjectRoot();
        HashSet<string> invariantKeys = AddMemberResourceKeys(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Resources",
            "TenantsResources.resx"));
        HashSet<string> frenchKeys = AddMemberResourceKeys(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Resources",
            "TenantsResources.fr.resx"));

        invariantKeys.ShouldNotBeEmpty();
        frenchKeys.ShouldBe(invariantKeys, ignoreOrder: true);
    }

    [Fact]
    public void Change_role_resources_have_full_invariant_and_french_parity()
    {
        string projectRoot = ProjectRoot();
        HashSet<string> invariantKeys = ChangeRoleResourceKeys(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Resources",
            "TenantsResources.resx"));
        HashSet<string> frenchKeys = ChangeRoleResourceKeys(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Resources",
            "TenantsResources.fr.resx"));

        invariantKeys.ShouldNotBeEmpty();
        frenchKeys.ShouldBe(invariantKeys, ignoreOrder: true);
    }

    [Fact]
    public void Remove_member_resources_have_full_invariant_and_french_parity()
    {
        string projectRoot = ProjectRoot();
        HashSet<string> invariantKeys = RemoveMemberResourceKeys(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Resources",
            "TenantsResources.resx"));
        HashSet<string> frenchKeys = RemoveMemberResourceKeys(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Resources",
            "TenantsResources.fr.resx"));

        invariantKeys.ShouldNotBeEmpty();
        frenchKeys.ShouldBe(invariantKeys, ignoreOrder: true);
    }

    [Fact]
    public void Copy_source_uses_clipboard_module_without_browser_backend_or_legacy_fallbacks()
    {
        string projectRoot = ProjectRoot();
        string component = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Shared",
            "SupportSafeCopyButton.razor"));
        string script = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "wwwroot",
            "js",
            "tenantsClipboard.js"));

        component.ShouldContain("IJSRuntime");
        component.ShouldContain("JSDisconnectedException");
        component.ShouldNotContain("HttpClient");
        component.ShouldNotContain("localStorage", Case.Insensitive);
        component.ShouldNotContain("sessionStorage", Case.Insensitive);
        script.ShouldContain("navigator.clipboard.writeText");
        script.ShouldNotContain("document.execCommand", Case.Insensitive);
        script.ShouldNotContain("GET /api/", Case.Insensitive);
        script.ShouldNotContain("access_token", Case.Insensitive);
        script.ShouldNotContain("localStorage", Case.Insensitive);
        script.ShouldNotContain("sessionStorage", Case.Insensitive);
    }

    private static HashSet<string> ConfigurationResourceKeys(string resourcePath)
        => Regex.Matches(File.ReadAllText(resourcePath), "name=\"(Tenants\\.Configuration[^\"]+)\"")
            .Select(static match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

    private static HashSet<string> MemberResourceKeys(string resourcePath)
        => Regex.Matches(File.ReadAllText(resourcePath), "name=\"(Tenants\\.Members[^\"]+)\"")
            .Select(static match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

    private static HashSet<string> LifecycleResourceKeys(string resourcePath)
        => Regex.Matches(File.ReadAllText(resourcePath), "name=\"(Tenants\\.Lifecycle[^\"]+)\"")
            .Select(static match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

    private static HashSet<string> CopyResourceKeys(string resourcePath)
        => Regex.Matches(File.ReadAllText(resourcePath), "name=\"(Tenants\\.Copy[^\"]+)\"")
            .Select(static match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

    private static HashSet<string> AddMemberResourceKeys(string resourcePath)
        => Regex.Matches(File.ReadAllText(resourcePath), "name=\"(Tenants\\.AddMember[^\"]+)\"")
            .Select(static match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

    private static HashSet<string> ChangeRoleResourceKeys(string resourcePath)
        => Regex.Matches(File.ReadAllText(resourcePath), "name=\"(Tenants\\.ChangeRole[^\"]+)\"")
            .Select(static match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

    private static HashSet<string> RemoveMemberResourceKeys(string resourcePath)
        => Regex.Matches(File.ReadAllText(resourcePath), "name=\"(Tenants\\.RemoveMember[^\"]+)\"")
            .Select(static match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

    private void RegisterComponentServices()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition());
        Services.AddFluentUIComponents();
    }

    private void RegisterServices(TenantListSnapshot snapshot)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(snapshot));
        Services.AddSingleton(gateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition());
        Services.AddFluentUIComponents();
    }

    private void RegisterListServices(Func<NSubstitute.Core.CallInfo, Task<TenantListSnapshot>> resultFactory)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(resultFactory);
        Services.AddSingleton(gateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition());
        Services.AddFluentUIComponents();
    }

    private void RegisterServices(Func<NSubstitute.Core.CallInfo, Task<TenantDetailSnapshot>> detailFactory)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.GetTenantAsync(Arg.Any<TenantDetailRequest>(), Arg.Any<TenantDetailSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(detailFactory);
        Services.AddSingleton(gateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition());
        Services.AddFluentUIComponents();
    }

    private static TenantDetailSnapshot Snapshot(TenantDetailSurfaceKind kind)
        => kind switch
        {
            TenantDetailSurfaceKind.Stale => TenantDetailSnapshot.Stale(Detail("tenant.alpha"), "\"etag\""),
            TenantDetailSurfaceKind.Degraded => TenantDetailSnapshot.Degraded(Detail("tenant.alpha"), "Tenant detail projection is degraded."),
            TenantDetailSurfaceKind.Unauthorized => TenantDetailSnapshot.Unauthorized("tenant.alpha"),
            TenantDetailSurfaceKind.NotFound => TenantDetailSnapshot.NotFound("tenant.alpha"),
            TenantDetailSurfaceKind.Unavailable => TenantDetailSnapshot.Unavailable("Tenant detail query gateway is unavailable."),
            TenantDetailSurfaceKind.Unknown => TenantDetailSnapshot.Unknown("Tenant detail projection returned no payload."),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };

    private static string ProjectRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static TenantDetail Detail(string tenantId)
        => Detail(tenantId, new Dictionary<string, string>
        {
            ["billing.mode"] = "trial",
        });

    private static TenantDetail Detail(string tenantId, IReadOnlyDictionary<string, string> configuration)
        => Detail(
            tenantId,
            configuration,
            TenantStatus.Active,
            [
                new TenantMember("owner-user", TenantRole.TenantOwner),
                new TenantMember("reader-user", TenantRole.TenantReader),
            ]);

    private static TenantDetail Detail(
        string tenantId,
        IReadOnlyDictionary<string, string> configuration,
        TenantStatus status,
        IReadOnlyList<TenantMember> members)
        => new(
            tenantId,
            "Alpha",
            "Tenant alpha description",
            status,
            members,
            configuration,
            DateTimeOffset.Parse("2026-06-01T12:00:00Z", CultureInfo.InvariantCulture));

    private sealed class StubTenantCommandGateway : ITenantCommandGateway
    {
        public Task<TenantCommandSubmissionResult> CreateTenantAsync(CreateTenantCommandRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> AddUserToTenantAsync(AddUserToTenantCommandRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable."));

        public Task<TenantCommandSubmissionResult> ChangeUserRoleAsync(ChangeUserRoleCommandRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable."));

        public Task<TenantCommandSubmissionResult> RemoveUserFromTenantAsync(RemoveUserFromTenantCommandRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable."));

        public Task<TenantCommandSubmissionResult> UpdateTenantAsync(UpdateTenantCommandRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable."));

        public Task<TenantCommandSubmissionResult> SetTenantConfigurationAsync(SetTenantConfigurationCommandRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable."));

        public Task<TenantCommandSubmissionResult> RemoveTenantConfigurationAsync(RemoveTenantConfigurationCommandRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable."));

        public Task<TenantCommandStatusResult> GetStatusAsync(TenantCommandTrackingHandle handle, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandStatusResult.Unknown("Tenant command status is unavailable."));
    }

    private sealed class StubTenantsBffComposition : ITenantsBffComposition
    {
        public bool IsReadSurfaceConnected => true;

        public bool IsCommandSurfaceConnected => true;
    }

    private sealed class StubTenantsLocalizer : IStringLocalizer<TenantsResources>
    {
        public LocalizedString this[string name] => new(name, Values.TryGetValue(name, out string? value) ? value : name);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, string.Format(CultureInfo.CurrentCulture, Values.TryGetValue(name, out string? value) ? value : name, arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => Values.Select(v => new LocalizedString(v.Key, v.Value));

        private static readonly Dictionary<string, string> Values = new(StringComparer.Ordinal)
        {
            ["Tenants.Detail.Back"] = "Back to tenants",
            ["Tenants.Detail.Configuration.Empty"] = "No configuration keys are available in this detail projection.",
            ["Tenants.Detail.Configuration.Summary"] = "{0} configuration keys across {1} safe groups.",
            ["Tenants.Detail.Configuration.Title"] = "Configuration summary",
            ["Tenants.Detail.CreatedAtLabel"] = "Created",
            ["Tenants.Detail.FreshnessLabel"] = "Freshness",
            ["Tenants.Detail.FullTenantIdLabel"] = "Full tenant identifier {0}",
            ["Tenants.Detail.IdentityLabel"] = "Tenant identity",
            ["Tenants.Detail.LifecycleLabel"] = "Lifecycle",
            ["Tenants.Detail.Members.Summary"] = "{0} members, including {1} owners.",
            ["Tenants.Detail.Members.Title"] = "Member summary",
            ["Tenants.Detail.OverviewLabel"] = "Tenant overview",
            ["Tenants.Detail.State.Degraded.Message"] = "Some tenant detail evidence is degraded.",
            ["Tenants.Detail.State.Degraded.Title"] = "Tenant detail is degraded",
            ["Tenants.Detail.State.Loading.Message"] = "Tenant detail is loading.",
            ["Tenants.Detail.State.Loading.Title"] = "Loading tenant detail",
            ["Tenants.Detail.State.NotFound.Message"] = "The requested tenant was not found.",
            ["Tenants.Detail.State.NotFound.Title"] = "Tenant not found",
            ["Tenants.Detail.State.Stale.Message"] = "The latest freshness evidence says this tenant detail is stale.",
            ["Tenants.Detail.State.Stale.Title"] = "Tenant detail is stale",
            ["Tenants.Detail.State.Unauthorized.Message"] = "This operator is not authorized.",
            ["Tenants.Detail.State.Unauthorized.Title"] = "Tenant detail unauthorized",
            ["Tenants.Detail.State.Unavailable.Message"] = "Tenant detail cannot be loaded because the gateway is unavailable.",
            ["Tenants.Detail.State.Unavailable.Title"] = "Tenant detail unavailable",
            ["Tenants.Detail.Status.Active"] = "Active",
            ["Tenants.Detail.Status.Disabled"] = "Disabled",
            ["Tenants.Detail.Status.Unknown"] = "Unknown",
            ["Tenants.Detail.StatusAccessibleLabel"] = "Tenant status {0}",
            ["Tenants.Detail.StatusLabel"] = "Status",
            ["Tenants.Detail.Title"] = "Tenant detail",
            ["Tenants.Lifecycle.Unavailable.AlreadyActive"] = "{1} is unavailable for tenant {0} because the current projection already shows Active. If submitted by another surface, the safe domain outcome is {2}; continue read-only or refresh.",
            ["Tenants.Configuration.Announcement.Results"] = "{0} visible configuration entries across {1} namespace groups.",
            ["Tenants.Configuration.ClearFilter"] = "Clear",
            ["Tenants.Configuration.CommandUnavailable"] = "Configuration commands are unavailable until freshness can be verified.",
            ["Tenants.Configuration.Description"] = "Read-only visible configuration from the authorized tenant detail projection.",
            ["Tenants.Configuration.Filter.Help"] = "Scan visible namespaces and literal keys. Prefix ownership is not inferred.",
            ["Tenants.Configuration.Filter.Label"] = "Filter visible configuration",
            ["Tenants.Configuration.Filter.Placeholder"] = "Namespace or key",
            ["Tenants.Configuration.GroupLabel"] = "Namespace {0}",
            ["Tenants.Configuration.Header.Freshness"] = "Freshness",
            ["Tenants.Configuration.Header.Key"] = "Key",
            ["Tenants.Configuration.Header.Namespace"] = "Namespace",
            ["Tenants.Configuration.Header.Safety"] = "Safety",
            ["Tenants.Configuration.Header.Value"] = "Value",
            ["Tenants.Configuration.Header.Actions"] = "Actions",
            ["Tenants.Configuration.KeyAccessible"] = "Full configuration key {0}",
            ["Tenants.Configuration.ScopeNotice"] = "Visible configuration only. Prefix ownership cannot be verified in this read model.",
            ["Tenants.Configuration.State.Degraded"] = "Configuration evidence is degraded.",
            ["Tenants.Configuration.State.Empty"] = "No visible configuration is available in this tenant detail projection.",
            ["Tenants.Configuration.State.Empty.Title"] = "No visible configuration",
            ["Tenants.Configuration.State.FilteredEmpty"] = "No visible configuration matches the current namespace filter.",
            ["Tenants.Configuration.State.FilteredEmpty.Title"] = "No matching configuration",
            ["Tenants.Configuration.State.Stale"] = "Configuration evidence is stale.",
            ["Tenants.Configuration.Table.Caption"] = "Visible tenant configuration grouped by namespace",
            ["Tenants.Configuration.Title"] = "Visible configuration",
            ["Tenants.Configuration.UnscopedNamespace"] = "Other",
            ["Tenants.Configuration.Value.Safe"] = "Visible",
            ["Tenants.Configuration.Value.Sensitive"] = "Unavailable",
            ["Tenants.Configuration.Value.Unavailable"] = "Unavailable",
            ["Tenants.Configuration.ValueAccessible"] = "Visible configuration value {0}",
            ["Tenants.Configuration.Remove.Open"] = "Remove",
            ["Tenants.Configuration.Remove.Title"] = "Remove configuration",
            ["Tenants.Configuration.Remove.Description"] = "Remove tenant {0} configuration.",
            ["Tenants.Configuration.Remove.Submit"] = "Confirm removal",
            ["Tenants.Configuration.Remove.Refresh"] = "Refresh status",
            ["Tenants.Configuration.Remove.Cancel"] = "Cancel",
            ["Tenants.Configuration.Remove.Confirmation.Label"] = "Type the full configuration key to confirm removal",
            ["Tenants.Configuration.Remove.Confirmation.Help"] = "Type {0} exactly. Cancel or Escape closes without submitting.",
            ["Tenants.Configuration.Remove.Lifecycle.Title"] = "Configuration removal lifecycle",
            ["Tenants.Configuration.Remove.Unavailable.Authorization"] = "You are not authorized to remove configuration for this tenant.",
            ["Tenants.Configuration.Remove.Unavailable.ProjectionState"] = "Tenant detail is unavailable or degraded.",
            ["Tenants.Configuration.Remove.Unavailable.Freshness"] = "Refresh current tenant detail before removing configuration.",
            ["Tenants.Configuration.Remove.Unavailable.TenantLifecycle"] = "This tenant lifecycle state does not allow configuration removal.",
            ["Tenants.Configuration.Remove.Unavailable.CommandSurface"] = "Tenant command support is unavailable.",
            ["Tenants.Configuration.Remove.Unavailable.InFlight"] = "A tenant command is already in progress.",
            ["Tenants.Configuration.Remove.Unavailable.Identity"] = "Tenant identity is unavailable.",
            ["Tenants.Configuration.Remove.Unavailable.Scope"] = "No authorized namespace prefix evidence is available.",
            ["Tenants.Configuration.Remove.Unavailable.Target"] = "The selected configuration key is not visible in the current authorized projection.",
            ["Tenants.Configuration.Remove.Unavailable.Narrow"] = "Configuration removal is unavailable on narrow layouts.",
            ["Tenants.Configuration.Remove.Validation.KeyRequired"] = "Select a visible configuration key before previewing removal.",
            ["Tenants.Configuration.Remove.Validation.KeyVisible"] = "The selected key cannot be proven from the current authorized projection.",
            ["Tenants.Configuration.Remove.Validation.NamespaceScope"] = "The namespace prefix cannot be proven from the current authorized projection.",
            ["Tenants.Configuration.Remove.Validation.ConfirmationRequired"] = "Type {0} exactly before removing this configuration key.",
            ["Tenants.Configuration.Remove.Preview.Title"] = "Consequence preview",
            ["Tenants.Configuration.Remove.Preview.Blocked.Required"] = "Complete required preview inputs.",
            ["Tenants.Configuration.Remove.Preview.Tenant"] = "Tenant",
            ["Tenants.Configuration.Remove.Preview.Namespace"] = "Namespace",
            ["Tenants.Configuration.Remove.Preview.Key"] = "Full key",
            ["Tenants.Configuration.Remove.Preview.CurrentState"] = "Current known state",
            ["Tenants.Configuration.Remove.Preview.IntendedEffect"] = "Intended effect",
            ["Tenants.Configuration.Remove.Preview.IntendedEffect.Value"] = "The selected configuration key will be removed after projection proof.",
            ["Tenants.Configuration.Remove.Preview.Freshness"] = "Freshness evidence",
            ["Tenants.Configuration.Remove.Preview.Authorization"] = "Authorization and scope evidence",
            ["Tenants.Configuration.Remove.Preview.Authorization.Value"] = "The namespace prefix and key are visible in the authorized projection.",
            ["Tenants.Configuration.Remove.Preview.KnownConsequences"] = "Known consequences",
            ["Tenants.Configuration.Remove.Preview.KnownConsequences.Value"] = "Consumers may lose this configured value.",
            ["Tenants.Configuration.Remove.Preview.KnownUnknowns"] = "Known unknowns",
            ["Tenants.Configuration.Remove.Preview.KnownUnknowns.Value"] = "Downstream impact is not proven.",
            ["Tenants.Configuration.Remove.Preview.AuditExpectation"] = "Audit expectation",
            ["Tenants.Configuration.Remove.Preview.AuditExpectation.Value"] = "Audit evidence is pending.",
            ["Tenants.Configuration.Remove.Preview.RecoveryPath"] = "Recovery path",
            ["Tenants.Configuration.Remove.Preview.RecoveryPath.Value"] = "Refresh tenant detail or submit a forward correction.",
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
            ["Tenants.Configuration.Remove.State.Degraded"] = "Configuration removal result is degraded.",
            ["Tenants.Configuration.Remove.State.UnableToVerify"] = "Unable to verify the configuration removal result.",
            ["Tenants.Configuration.Remove.Audit.NotStarted"] = "Audit evidence not started.",
            ["Tenants.Configuration.Remove.Audit.AuditPending"] = "Audit evidence pending.",
            ["Tenants.Configuration.Remove.Audit.AuditDelayed"] = "Audit evidence delayed.",
            ["Tenants.Configuration.Remove.Audit.AuditUnavailable"] = "Audit evidence unavailable.",
            ["Tenants.Configuration.Remove.Audit.MissingSupport"] = "Audit evidence support is missing.",
            ["Tenants.Configuration.Remove.Recovery.Idle"] = "Choose a visible key when projection evidence is available.",
            ["Tenants.Configuration.Remove.Recovery.Previewed"] = "Confirm removal, cancel, or continue read-only.",
            ["Tenants.Configuration.Remove.Recovery.RequestSent"] = "Wait for command status and projection refresh.",
            ["Tenants.Configuration.Remove.Recovery.Accepted"] = "Wait, refresh status, or continue read-only.",
            ["Tenants.Configuration.Remove.Recovery.ProjectionPending"] = "Refresh tenant detail; do not display removal as complete.",
            ["Tenants.Configuration.Remove.Recovery.Confirmed"] = "Continue read-only or inspect audit later.",
            ["Tenants.Configuration.Remove.Recovery.Rejected"] = "Refresh projection evidence, request permission, start correction, or escalate.",
            ["Tenants.Configuration.Remove.Recovery.AlreadyApplied"] = "Refresh projection evidence before treating the missing key as already removed.",
            ["Tenants.Configuration.Remove.Recovery.DuplicatePrevented"] = "Wait for the in-flight command.",
            ["Tenants.Configuration.Remove.Recovery.Failed"] = "Retry after checking projection evidence.",
            ["Tenants.Configuration.Remove.Recovery.Degraded"] = "Wait, retry status lookup, or escalate.",
            ["Tenants.Configuration.Remove.Recovery.UnableToVerify"] = "Refresh, retry status lookup, or escalate.",
            ["Tenants.Copy.Action"] = "Copy",
            ["Tenants.Copy.Label.ConfigurationKey"] = "Copy configuration key {0}",
            ["Tenants.Copy.Label.ConfigurationValue"] = "Copy visible configuration value for {0}",
            ["Tenants.Copy.Label.TenantId"] = "Copy tenant identifier {0}",
            ["Tenants.Copy.Label.UserId"] = "Copy user identifier {0}",
            ["Tenants.Copy.Feedback.Copied"] = "Copied.",
            ["Tenants.Copy.Feedback.Disconnected"] = "Clipboard disconnected. Copy was not completed.",
            ["Tenants.Copy.Feedback.Empty"] = "Nothing is available to copy.",
            ["Tenants.Copy.Feedback.Failed"] = "Copy failed.",
            ["Tenants.Copy.Feedback.Unavailable"] = "Clipboard unavailable.",
            ["Tenants.Copy.Feedback.Unsafe"] = "This value is not support-safe to copy.",
            ["Tenants.AddMember.Title"] = "Add tenant member",
            ["Tenants.AddMember.Description"] = "Add a literal user id to tenant {0}. Current visible owner count is {1}.",
            ["Tenants.AddMember.UserId.Label"] = "User id",
            ["Tenants.AddMember.UserId.Help"] = "Use the exact caller-supplied user id.",
            ["Tenants.AddMember.Role.Label"] = "Tenant role",
            ["Tenants.AddMember.Role.Placeholder"] = "Select a role",
            ["Tenants.AddMember.Role.TenantOwner"] = "Tenant owner",
            ["Tenants.AddMember.Role.TenantContributor"] = "Tenant contributor",
            ["Tenants.AddMember.Role.TenantReader"] = "Tenant reader",
            ["Tenants.AddMember.Submit"] = "Add member",
            ["Tenants.AddMember.Refresh"] = "Refresh status",
            ["Tenants.AddMember.Lifecycle.Title"] = "Add member command lifecycle",
            ["Tenants.AddMember.Validation.UserIdRequired"] = "User id is required.",
            ["Tenants.AddMember.Validation.RoleRequired"] = "Select TenantOwner, TenantContributor, or TenantReader before adding a member.",
            ["Tenants.AddMember.Unavailable.Authorization"] = "You are not authorized to add members to this tenant.",
            ["Tenants.AddMember.Unavailable.Freshness"] = "Refresh current tenant detail before adding a member.",
            ["Tenants.AddMember.Unavailable.TenantLifecycle"] = "This tenant lifecycle state does not allow adding members.",
            ["Tenants.AddMember.Unavailable.CommandSurface"] = "Tenant command support is unavailable.",
            ["Tenants.AddMember.Unavailable.InFlight"] = "A tenant command is already in progress.",
            ["Tenants.AddMember.State.Idle"] = "No add-member command submitted.",
            ["Tenants.AddMember.State.RequestSent"] = "Add-member request sent.",
            ["Tenants.AddMember.State.Accepted"] = "Accepted by EventStore; waiting for member processing.",
            ["Tenants.AddMember.State.ProjectionPending"] = "Projection pending; the member role is not confirmed visible yet.",
            ["Tenants.AddMember.State.Confirmed"] = "Projection confirmed the user is a tenant member with the requested role.",
            ["Tenants.AddMember.State.Rejected"] = "Add-member command rejected.",
            ["Tenants.AddMember.State.Failed"] = "Add-member command submission failed.",
            ["Tenants.AddMember.State.Degraded"] = "Add-member command result is degraded and needs review.",
            ["Tenants.AddMember.State.UnableToVerify"] = "Unable to verify the add-member command result.",
            ["Tenants.AddMember.Audit.NotStarted"] = "Audit evidence not started.",
            ["Tenants.AddMember.Audit.AuditPending"] = "Audit evidence pending.",
            ["Tenants.AddMember.Audit.AuditUnavailable"] = "Audit evidence unavailable.",
            ["Tenants.AddMember.Audit.MissingSupport"] = "Audit support is missing for this flow.",
            ["Tenants.ChangeRole.Title"] = "Change tenant member role",
            ["Tenants.ChangeRole.Description"] = "Change the role for user {1} in tenant {0}. The current confirmed role is {2}.",
            ["Tenants.ChangeRole.UserId.Label"] = "User id",
            ["Tenants.ChangeRole.CurrentRole.Label"] = "Current confirmed role",
            ["Tenants.ChangeRole.OwnerContext.Label"] = "Owner context",
            ["Tenants.ChangeRole.OwnerContext.NoOwners"] = "0 visible owners; owner context is unavailable.",
            ["Tenants.ChangeRole.OwnerContext.LastOwner"] = "{0} visible owner; changing this owner can leave the tenant with zero visible owners.",
            ["Tenants.ChangeRole.OwnerContext.MultipleOwners"] = "{0} visible owners.",
            ["Tenants.ChangeRole.NewRole.Label"] = "New role",
            ["Tenants.ChangeRole.NewRole.Help"] = "Select TenantOwner, TenantContributor, or TenantReader. Selecting the current role records an already applied state.",
            ["Tenants.ChangeRole.Role.TenantOwner"] = "Tenant owner",
            ["Tenants.ChangeRole.Role.TenantContributor"] = "Tenant contributor",
            ["Tenants.ChangeRole.Role.TenantReader"] = "Tenant reader",
            ["Tenants.ChangeRole.Submit"] = "Change role",
            ["Tenants.ChangeRole.Refresh"] = "Refresh status",
            ["Tenants.ChangeRole.Cancel"] = "Close",
            ["Tenants.ChangeRole.Lifecycle.Title"] = "Change role command lifecycle",
            ["Tenants.ChangeRole.Validation.RoleRequired"] = "Select TenantOwner, TenantContributor, or TenantReader before changing a role.",
            ["Tenants.ChangeRole.Unavailable.Authorization"] = "You are not authorized to change member roles in this tenant.",
            ["Tenants.ChangeRole.Unavailable.Freshness"] = "Refresh current tenant detail before changing a member role.",
            ["Tenants.ChangeRole.Unavailable.TenantLifecycle"] = "This tenant lifecycle state does not allow changing member roles.",
            ["Tenants.ChangeRole.Unavailable.CommandSurface"] = "Tenant command support is unavailable.",
            ["Tenants.ChangeRole.Unavailable.InFlight"] = "A tenant command is already in progress.",
            ["Tenants.ChangeRole.Unavailable.UnknownRole"] = "The current role is unknown, so role change fails closed until projection evidence is refreshed.",
            ["Tenants.ChangeRole.OwnerRisk.LastOwner"] = "Warning: {0} visible owner remains. This change can reduce the visible owner count to zero, but the command is not blocked solely for that reason.",
            ["Tenants.ChangeRole.AlreadyApplied.Message"] = "User {0} already has role {1}; no role-change command was submitted.",
            ["Tenants.ChangeRole.State.Idle"] = "No change-role command submitted.",
            ["Tenants.ChangeRole.State.RequestSent"] = "Change-role request sent.",
            ["Tenants.ChangeRole.State.Accepted"] = "Accepted by EventStore; waiting for member role processing.",
            ["Tenants.ChangeRole.State.ProjectionPending"] = "Projection pending; the requested role is not confirmed visible yet.",
            ["Tenants.ChangeRole.State.Confirmed"] = "Projection confirmed the target user has the requested role.",
            ["Tenants.ChangeRole.State.Rejected"] = "Change-role command rejected.",
            ["Tenants.ChangeRole.State.AlreadyApplied"] = "Already applied; the confirmed role already matches the selected role.",
            ["Tenants.ChangeRole.State.Failed"] = "Change-role command submission failed.",
            ["Tenants.ChangeRole.State.Degraded"] = "Change-role command result is degraded and needs review.",
            ["Tenants.ChangeRole.State.UnableToVerify"] = "Unable to verify the change-role command result.",
            ["Tenants.ChangeRole.Audit.NotStarted"] = "Audit evidence not started.",
            ["Tenants.ChangeRole.Audit.AuditPending"] = "Audit evidence pending.",
            ["Tenants.ChangeRole.Audit.AuditUnavailable"] = "Audit evidence unavailable.",
            ["Tenants.ChangeRole.Audit.MissingSupport"] = "Audit support is missing for this flow.",
            ["Tenants.Members.Action.AddMember"] = "Add member",
            ["Tenants.Members.Action.ChangeRole"] = "Change role",
            ["Tenants.Members.Action.RemoveMember"] = "Remove member",
            ["Tenants.Members.Action.Unavailable"] = "Unavailable",
            ["Tenants.Members.ActionSlotAccessible"] = "{0} is unavailable for {1}: {2}",
            ["Tenants.Members.Column.Actions"] = "Action availability",
            ["Tenants.Members.Column.Freshness"] = "Freshness",
            ["Tenants.Members.Column.OwnerContext"] = "Owner context",
            ["Tenants.Members.Column.Role"] = "Role",
            ["Tenants.Members.Column.Status"] = "Tenant status",
            ["Tenants.Members.Column.UserId"] = "User id",
            ["Tenants.Members.Description"] = "Read-only member access context from the authorized tenant detail projection.",
            ["Tenants.Members.Empty.Message"] = "No visible members are available. This state does not reveal hidden memberships, and actions remain unavailable until visibility and freshness are verified: {0}.",
            ["Tenants.Members.Empty.Title"] = "No visible members",
            ["Tenants.Members.Freshness.Aging"] = "Aging",
            ["Tenants.Members.Freshness.Current"] = "Current",
            ["Tenants.Members.Freshness.Refreshing"] = "Refreshing",
            ["Tenants.Members.Freshness.Stale"] = "Stale",
            ["Tenants.Members.Freshness.Unknown"] = "Unknown",
            ["Tenants.Members.OwnerContext.LastOwner"] = "{0} visible owner; last-owner changes require a later high-impact flow.",
            ["Tenants.Members.OwnerContext.MultipleOwners"] = "{0} visible owners.",
            ["Tenants.Members.OwnerContext.NoOwners"] = "0 visible owners; owner context is unavailable.",
            ["Tenants.Members.ReasonCatalogLabel"] = "Canonical unavailable action reason categories",
            ["Tenants.Members.ReasonListLabel"] = "Unavailable action reasons for {0}",
            ["Tenants.Members.Role.TenantContributor"] = "Tenant contributor",
            ["Tenants.Members.Role.TenantOwner"] = "Tenant owner",
            ["Tenants.Members.Role.TenantReader"] = "Tenant reader",
            ["Tenants.Members.Role.Unknown"] = "Unknown role",
            ["Tenants.Members.RoleAccessible"] = "Role: {0}",
            ["Tenants.Members.ScopeNotice"] = "Visible members only. Orphan context is unavailable in this read model; disabled lifecycle is shown from tenant status.",
            ["Tenants.Members.State.Degraded"] = "Member evidence is degraded.",
            ["Tenants.Members.State.Stale"] = "Member evidence is stale.",
            ["Tenants.Members.Status.Active"] = "Active",
            ["Tenants.Members.Status.Disabled"] = "Disabled",
            ["Tenants.Members.Status.Unknown"] = "Unknown",
            ["Tenants.Members.StatusAccessible"] = "Tenant status {0}",
            ["Tenants.Members.Table.Caption"] = "Visible tenant members and read-only action availability",
            ["Tenants.Members.Title"] = "Member access review",
            ["Tenants.Members.UnavailableReason.HighImpactFlowNotReady"] = "high-impact flow not ready",
            ["Tenants.Members.UnavailableReason.MissingAuditProof"] = "missing audit proof",
            ["Tenants.Members.UnavailableReason.MissingConsequencePreview"] = "missing consequence preview",
            ["Tenants.Members.UnavailableReason.MissingLifecycleSupport"] = "missing lifecycle support",
            ["Tenants.Members.UnavailableReason.MissingPermission"] = "missing permission",
            ["Tenants.Members.UnavailableReason.StaleData"] = "stale data",
            ["Tenants.Members.UserIdAccessible"] = "Literal member user identifier {0}",
            ["Tenants.List.Column.Freshness"] = "Truth state",
            ["Tenants.List.Column.Members"] = "Members",
            ["Tenants.List.Column.Owners"] = "Owners",
            ["Tenants.List.Column.Pending"] = "Pending",
            ["Tenants.List.Column.Status"] = "Status",
            ["Tenants.List.Column.Tenant"] = "Tenant",
            ["Tenants.List.DetailLinkLabel"] = "Open tenant details for {0}",
            ["Tenants.List.Freshness.Current"] = "Current",
            ["Tenants.List.Pending.None"] = "No pending changes",
            ["Tenants.List.Pending.Unknown"] = "Pending state unknown",
            ["Tenants.List.ReturnContext"] = "Returned from tenant {0}. Filters, sort, cursor, and selection were restored before rendering.",
            ["Tenants.List.Title"] = "Tenants",
            ["Tenants.Workspace.Eyebrow"] = "Tenant workspace",
        };
    }
}
