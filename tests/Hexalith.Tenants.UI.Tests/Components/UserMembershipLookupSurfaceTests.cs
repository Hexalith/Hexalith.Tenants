using System.Globalization;

using Bunit;

using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.UI.Components.Pages;
using Hexalith.Tenants.UI.Components.Users;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.EventStore.Client.Projections;
using Hexalith.Tenants.UI.State.UserTenants;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class UserMembershipLookupSurfaceTests : BunitContext
{
    [Fact]
    public void User_lookup_direct_route_prefills_target_and_renders_authorization_scoped_results()
    {
        List<UserTenantMembershipRequest> requests = [];
        RegisterServices(call =>
        {
            requests.Add(call.ArgAt<UserTenantMembershipRequest>(0));
            return Task.FromResult(ReadySnapshot(
                [
                    Row("tenant.alpha", "Alpha", TenantStatus.Active, TenantRole.TenantOwner, ReadModelFreshnessState.Current),
                    Row("tenant.beta", "Beta", TenantStatus.Disabled, TenantRole.TenantReader, ReadModelFreshnessState.Unknown),
                ],
                nextCursor: "next",
                hasMore: true,
                targetUserId: "target.user@example"));
        });

        NavigateToLookup("target.user@example");
        IRenderedComponent<UserMembershipLookupPage> cut = Render<UserMembershipLookupPage>();
        cut.WaitForElement("[data-testid='tenants-user-lookup-results']");

        requests.ShouldHaveSingleItem().TargetUserId.ShouldBe("target.user@example");
        cut.Find("[data-testid='tenants-user-lookup-input']").GetAttribute("value").ShouldBe("target.user@example");
        cut.Find("[data-testid='tenants-user-lookup-target']").TextContent.ShouldContain("target.user@example");
        cut.FindAll("[data-testid='tenants-user-row']").Count.ShouldBe(2);
        cut.Find("[data-testid='tenants-user-tenant-id']").TextContent.ShouldContain("tenant.alpha");
        cut.FindAll("[data-testid='tenants-user-copy-reference']").Count.ShouldBe(2);
        cut.Find("[data-testid='tenants-user-copy-reference']").GetAttribute("data-copy-kind").ShouldBe("TenantId");
        cut.Find("[data-testid='tenants-user-role']").TextContent.ShouldContain("Tenant owner");
        cut.Find("[data-testid='tenants-user-status']").TextContent.ShouldContain("Active");
        cut.Find("[data-testid='tenants-user-truth-state']").TextContent.ShouldContain("Current");
        Services.GetRequiredService<NavigationManager>().Uri.ShouldContain(
            "/tenants?tab=users&userId=target.user%40example");
        cut.Markup.ShouldNotContain("Lifecycle", Case.Insensitive);
        cut.Markup.ShouldNotContain("tenants-my-list", Case.Insensitive);
        cut.Markup.ShouldNotContain("remove", Case.Insensitive);
        cut.Markup.ShouldNotContain("change role", Case.Insensitive);
        cut.Markup.ShouldNotContain("access_token", Case.Insensitive);
    }

    [Fact]
    public void User_lookup_manual_submit_sends_literal_target_without_identifier_parsing()
    {
        List<UserTenantMembershipRequest> requests = [];
        RegisterServices(call =>
        {
            requests.Add(call.ArgAt<UserTenantMembershipRequest>(0));
            return Task.FromResult(UserTenantMembershipSnapshot.Empty(
                isAuthorizationScoped: true,
                ReadModelFreshnessState.Current,
                eTag: "\"etag\"",
                targetUserId: "USER.Target-01"));
        });

        IRenderedComponent<UserMembershipLookupPage> cut = Render<UserMembershipLookupPage>();
        cut.Find("[data-testid='tenants-user-lookup-input']").Change("USER.Target-01");
        cut.Find("form").Submit();
        cut.WaitForElement("[data-testid='tenants-user-empty']");

        requests.ShouldHaveSingleItem().TargetUserId.ShouldBe("USER.Target-01");
        cut.Find("[data-testid='tenants-user-lookup-status']").GetAttribute("tabindex").ShouldBe("-1");
        cut.Find("[data-testid='tenants-user-empty']").GetAttribute("role").ShouldBe("status");
        cut.Markup.ShouldContain("No visible memberships for this lookup");
        cut.Markup.ShouldNotContain("does not exist", Case.Insensitive);
        cut.Markup.ShouldNotContain("orphan membership id", Case.Insensitive);
    }

    [Fact]
    public void Query_parameter_changes_reapply_lookup_state_on_the_existing_component()
    {
        List<UserTenantMembershipRequest> requests = [];
        RegisterServices(call =>
        {
            UserTenantMembershipRequest request = call.ArgAt<UserTenantMembershipRequest>(0);
            requests.Add(request);
            return Task.FromResult(ReadySnapshot(
                [Row("tenant.alpha", "Alpha", TenantStatus.Active, TenantRole.TenantOwner, ReadModelFreshnessState.Current)],
                targetUserId: request.TargetUserId));
        });

        IRenderedComponent<UserMembershipLookupPanel> cut = Render<UserMembershipLookupPanel>(parameters => parameters
            .Add(component => component.InitialUserId, "user.one"));
        cut.WaitForAssertion(() => requests.ShouldHaveSingleItem().TargetUserId.ShouldBe("user.one"));

        cut.Render(parameters => parameters
            .Add(component => component.InitialUserId, "user.two")
            .Add(component => component.InitialSort, UserTenantMembershipSortColumns.Role)
            .Add(component => component.InitialCursor, "user-two-cursor"));

        cut.WaitForAssertion(() =>
        {
            requests.Count.ShouldBe(2);
            requests[^1].TargetUserId.ShouldBe("user.two");
            requests[^1].Cursor.ShouldBe("user-two-cursor");
        });
    }

    [Fact]
    public void User_lookup_invalid_submit_does_not_call_gateway_and_uses_assertive_state()
    {
        ITenantQueryGateway gateway = RegisterServices(UserTenantMembershipSnapshot.Unavailable());

        IRenderedComponent<UserMembershipLookupPage> cut = Render<UserMembershipLookupPage>();
        cut.Find("[data-testid='tenants-user-lookup-input']").Change(" ");
        cut.Find("form").Submit();
        cut.WaitForElement("[data-testid='tenants-user-invalid']");

        cut.Find("[data-testid='tenants-user-invalid']").GetAttribute("role").ShouldBe("alert");
        Services.GetRequiredService<NavigationManager>().Uri.ShouldBe("http://localhost/tenants?tab=users");
        gateway.DidNotReceiveWithAnyArgs()
            .GetUserTenantsAsync(default!, default, default);
    }

    [Fact]
    public void User_lookup_clear_resets_results_and_preserves_input_control()
    {
        RegisterServices(ReadySnapshot(
            [Row("tenant.alpha", "Alpha", TenantStatus.Active, TenantRole.TenantReader, ReadModelFreshnessState.Current)],
            targetUserId: "target.user"));

        NavigateToLookup("target.user");
        IRenderedComponent<UserMembershipLookupPage> cut = Render<UserMembershipLookupPage>();
        cut.WaitForElement("[data-testid='tenants-user-lookup-results']");

        cut.Find("[data-testid='tenants-user-lookup-clear']").Click();

        cut.Find("[data-testid='tenants-user-lookup-input']").GetAttribute("value").ShouldBe(string.Empty);
        cut.FindAll("[data-testid='tenants-user-lookup-results']").ShouldBeEmpty();
        cut.Markup.ShouldContain("User membership lookup cleared.");
        Services.GetRequiredService<NavigationManager>().Uri.ShouldBe("http://localhost/tenants?tab=users");
    }

    [Theory]
    [InlineData(UserTenantMembershipSurfaceKind.Unauthorized, "tenants-user-error", "alert", "User membership lookup is unauthorized")]
    [InlineData(UserTenantMembershipSurfaceKind.Unavailable, "tenants-user-error", "alert", "User membership lookup is unavailable")]
    [InlineData(UserTenantMembershipSurfaceKind.Stale, "tenants-user-stale", "status", "User membership data is stale")]
    [InlineData(UserTenantMembershipSurfaceKind.Degraded, "tenants-user-degraded", "alert", "User membership data is degraded")]
    public void User_lookup_renders_distinct_safe_states(
        UserTenantMembershipSurfaceKind kind,
        string selector,
        string expectedRole,
        string expectedTitle)
    {
        UserTenantMembershipSnapshot snapshot = kind switch
        {
            UserTenantMembershipSurfaceKind.Unauthorized => UserTenantMembershipSnapshot.Unauthorized(targetUserId: "target.user"),
            UserTenantMembershipSurfaceKind.Unavailable => UserTenantMembershipSnapshot.Unavailable(targetUserId: "target.user"),
            UserTenantMembershipSurfaceKind.Stale => UserTenantMembershipSnapshot.Stale(
                [Row("tenant.alpha", "Alpha", TenantStatus.Disabled, TenantRole.TenantReader, ReadModelFreshnessState.Stale)],
                nextCursor: null,
                hasMore: false,
                eTag: "\"etag\"",
                targetUserId: "target.user"),
            UserTenantMembershipSurfaceKind.Degraded => UserTenantMembershipSnapshot.Degraded(
                [Row("tenant.alpha", "Alpha", TenantStatus.Unknown, TenantRole.Unknown, ReadModelFreshnessState.Unknown)],
                UserTenantMembershipReason.ProjectionDegraded,
                targetUserId: "target.user"),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
        RegisterServices(snapshot);

        NavigateToLookup("target.user");
        IRenderedComponent<UserMembershipLookupPage> cut = Render<UserMembershipLookupPage>();
        cut.WaitForElement($"[data-testid='{selector}']");

        cut.Find($"[data-testid='{selector}']").GetAttribute("role").ShouldBe(expectedRole);
        cut.Markup.ShouldContain(expectedTitle);
        cut.Markup.ShouldNotContain("raw payload", Case.Insensitive);
        cut.Markup.ShouldNotContain("correlation", Case.Insensitive);
        cut.Markup.ShouldNotContain("stack trace", Case.Insensitive);
    }

    [Fact]
    public void User_lookup_paging_refresh_and_sort_preserve_target_and_cursor_scope()
    {
        Queue<UserTenantMembershipSnapshot> snapshots = new(
            [
                ReadySnapshot(
                    [
                        Row("tenant.beta", "Beta", TenantStatus.Disabled, TenantRole.TenantReader, ReadModelFreshnessState.Current),
                        Row("tenant.alpha", "Alpha", TenantStatus.Active, TenantRole.TenantOwner, ReadModelFreshnessState.Current),
                    ],
                    nextCursor: "opaque-next",
                    hasMore: true,
                    targetUserId: "target.user"),
                ReadySnapshot(
                    [Row("tenant.gamma", "Gamma", TenantStatus.Active, TenantRole.TenantContributor, ReadModelFreshnessState.Current)],
                    targetUserId: "target.user"),
                ReadySnapshot(
                    [Row("tenant.alpha", "Alpha", TenantStatus.Active, TenantRole.TenantOwner, ReadModelFreshnessState.Current)],
                    nextCursor: "opaque-next",
                    hasMore: true,
                    targetUserId: "target.user"),
                ReadySnapshot(
                    [Row("tenant.alpha", "Alpha", TenantStatus.Active, TenantRole.TenantOwner, ReadModelFreshnessState.Current)],
                    targetUserId: "target.user"),
            ]);
        List<UserTenantMembershipRequest> requests = [];
        RegisterServices(call =>
        {
            requests.Add(call.ArgAt<UserTenantMembershipRequest>(0));
            return Task.FromResult(snapshots.Dequeue());
        });

        NavigateToLookup("target.user");
        IRenderedComponent<UserMembershipLookupPage> cut = Render<UserMembershipLookupPage>();
        cut.WaitForElement("[data-testid='tenants-user-lookup-results']");

        cut.Find("[data-testid='tenants-user-lookup-next']").Click();
        cut.WaitForAssertion(() => requests[1].Cursor.ShouldBe("opaque-next"));
        cut.Markup.ShouldContain("tenant.gamma");

        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-user-lookup-sort", "name");
        cut.WaitForAssertion(() =>
        {
            requests[2].Cursor.ShouldBeNull();
            cut.Markup.ShouldContain("tenant.alpha");
            cut.Markup.ShouldContain("Visible memberships sorted.");
            cut.Find("[data-testid='tenants-user-lookup-previous']").HasAttribute("disabled").ShouldBeTrue();
        });
        Services.GetRequiredService<NavigationManager>().Uri.ShouldBe("http://localhost/tenants?tab=users&userId=target.user&sort=name");

        cut.Find("[data-testid='tenants-user-lookup-refresh']").Click();
        cut.WaitForAssertion(() => requests[3].ETag.ShouldBe("\"etag\""));
        requests.All(request => request.TargetUserId == "target.user").ShouldBeTrue();
    }

    [Fact]
    public void User_lookup_components_have_no_browser_backend_http_or_token_storage()
    {
        string projectRoot = ProjectRoot();
        string[] componentFiles = Directory
            .GetFiles(Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Components"), "*.razor", SearchOption.AllDirectories)
            .Where(static path => !path.EndsWith("App.razor", StringComparison.Ordinal))
            .ToArray();
        string combined = string.Join('\n', componentFiles.Select(File.ReadAllText));

        combined.ShouldNotContain("GET /api/users", Case.Insensitive);
        combined.ShouldNotContain("HttpClient");
        combined.ShouldNotContain("localStorage", Case.Insensitive);
        combined.ShouldNotContain("sessionStorage", Case.Insensitive);
        combined.ShouldNotContain("access_token", Case.Insensitive);
    }

    [Fact]
    public void User_lookup_layout_preserves_responsive_fluent_grid_and_forced_colors_controls()
    {
        string projectRoot = ProjectRoot();
        string panel = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Users",
            "UserMembershipLookupPanel.razor"));
        string styles = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Users",
            "UserMembershipLookupPanel.razor.css"));

        panel.ShouldContain("<FluentGrid");
        panel.ShouldContain("<FluentGridItem Xs=\"12\" Md=\"6\" Lg=\"5\">");
        panel.ShouldContain("<FluentGridItem Xs=\"12\" Md=\"6\" Lg=\"7\">");
        styles.ShouldNotContain("grid-template-columns");
        styles.ShouldContain("@media (forced-colors: active)");
        styles.ShouldContain(":focus-visible");
    }

    private ITenantQueryGateway RegisterServices(UserTenantMembershipSnapshot snapshot)
        => RegisterServices(_ => Task.FromResult(snapshot));

    private ITenantQueryGateway RegisterServices(Func<NSubstitute.Core.CallInfo, Task<UserTenantMembershipSnapshot>> resultFactory)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.GetUserTenantsAsync(Arg.Any<UserTenantMembershipRequest>(), Arg.Any<UserTenantMembershipSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(resultFactory);
        Services.AddSingleton(gateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddFluentUIComponents();

        return gateway;
    }

    private static UserTenantMembershipSnapshot ReadySnapshot(
        IReadOnlyList<UserTenantMembershipRow> rows,
        string? nextCursor = null,
        bool hasMore = false,
        string? targetUserId = null)
        => UserTenantMembershipSnapshot.Ready(
            rows,
            nextCursor,
            hasMore,
            eTag: "\"etag\"",
            freshness: rows.Any(row => row.Freshness == ReadModelFreshnessState.Stale)
                ? ReadModelFreshnessState.Stale
                : ReadModelFreshnessState.Current,
            targetUserId: targetUserId);

    private static UserTenantMembershipRow Row(
        string tenantId,
        string name,
        TenantStatus status,
        TenantRole role,
        ReadModelFreshnessState freshness)
        => new(tenantId, name, status, role, freshness);

    private static string ProjectRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private void NavigateToLookup(string userId)
    {
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo($"/tenants/users?userId={Uri.EscapeDataString(userId)}");
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
            ["Tenants.Workspace.Eyebrow"] = "Tenant workspace",
            ["Tenants.UserLookup.Announcement.Cleared"] = "User membership lookup cleared.",
            ["Tenants.UserLookup.Announcement.Degraded"] = "Membership evidence for {0} is degraded.",
            ["Tenants.UserLookup.Announcement.Empty"] = "No memberships are visible for {0} in this authorization-scoped lookup.",
            ["Tenants.UserLookup.Announcement.Invalid"] = "Enter a user identifier before running the lookup.",
            ["Tenants.UserLookup.Announcement.Loading"] = "Looking up visible memberships for {0}.",
            ["Tenants.UserLookup.Announcement.Ready"] = "{0} visible memberships loaded for {1}.",
            ["Tenants.UserLookup.Announcement.Sorted"] = "Visible memberships sorted.",
            ["Tenants.UserLookup.Announcement.Stale"] = "Membership evidence for {0} is stale.",
            ["Tenants.UserLookup.Announcement.Unauthorized"] = "The user membership lookup is not authorized for {0}.",
            ["Tenants.UserLookup.Announcement.Unavailable"] = "The user membership lookup is unavailable for {0}.",
            ["Tenants.UserLookup.Back"] = "Back to tenants",
            ["Tenants.UserLookup.Clear"] = "Clear",
            ["Tenants.UserLookup.Column.Freshness"] = "Freshness",
            ["Tenants.UserLookup.Column.Role"] = "Role",
            ["Tenants.UserLookup.Column.Status"] = "Status",
            ["Tenants.UserLookup.Column.Tenant"] = "Tenant",
            ["Tenants.UserLookup.Description"] = "Read-only membership lookup for a caller-supplied user identifier.",
            ["Tenants.UserLookup.FormLabel"] = "User membership lookup controls",
            ["Tenants.UserLookup.Freshness.Current"] = "Current",
            ["Tenants.UserLookup.Freshness.Stale"] = "Stale",
            ["Tenants.UserLookup.Freshness.Unknown"] = "Unknown",
            ["Tenants.UserLookup.Initial.Message"] = "Enter a user identifier to run an authorization-scoped membership lookup.",
            ["Tenants.UserLookup.Initial.Title"] = "User membership lookup ready",
            ["Tenants.UserLookup.InputHelp"] = "Use the exact caller-supplied user identifier.",
            ["Tenants.UserLookup.InputLabel"] = "User identifier",
            ["Tenants.UserLookup.Next"] = "Next",
            ["Tenants.UserLookup.PaginationLabel"] = "User membership result pages",
            ["Tenants.UserLookup.Previous"] = "Previous",
            ["Tenants.UserLookup.Refresh"] = "Refresh",
            ["Tenants.UserLookup.Role.TenantContributor"] = "Tenant contributor",
            ["Tenants.UserLookup.Role.TenantOwner"] = "Tenant owner",
            ["Tenants.UserLookup.Role.TenantReader"] = "Tenant reader",
            ["Tenants.UserLookup.Role.Unknown"] = "Unknown role",
            ["Tenants.UserLookup.RoleAccessible"] = "Role: {0}",
            ["Tenants.UserLookup.Sort.Name"] = "Name",
            ["Tenants.UserLookup.Sort.Role"] = "Role",
            ["Tenants.UserLookup.Sort.Status"] = "Status",
            ["Tenants.UserLookup.Sort.Tenant"] = "Tenant identifier",
            ["Tenants.UserLookup.SortLabel"] = "Sort results",
            ["Tenants.UserLookup.State.Degraded.Message"] = "Some membership evidence is degraded.",
            ["Tenants.UserLookup.State.Degraded.Title"] = "User membership data is degraded",
            ["Tenants.UserLookup.State.Empty.Message"] = "No memberships are visible for this lookup.",
            ["Tenants.UserLookup.State.Empty.Title"] = "No visible memberships for this lookup",
            ["Tenants.UserLookup.State.Invalid.Message"] = "Enter a supported user identifier before running the lookup.",
            ["Tenants.UserLookup.State.Invalid.Title"] = "User lookup input is invalid",
            ["Tenants.UserLookup.State.Loading.Message"] = "User memberships are loading through the server-side query gateway.",
            ["Tenants.UserLookup.State.Loading.Title"] = "Loading user memberships",
            ["Tenants.UserLookup.State.Ready.Title"] = "Visible memberships loaded",
            ["Tenants.UserLookup.State.Stale.Message"] = "The latest freshness evidence says these memberships are stale.",
            ["Tenants.UserLookup.State.Stale.Title"] = "User membership data is stale",
            ["Tenants.UserLookup.State.Unauthorized.Message"] = "The signed-in operator could not be authorized for this user membership lookup.",
            ["Tenants.UserLookup.State.Unauthorized.Title"] = "User membership lookup is unauthorized",
            ["Tenants.UserLookup.State.Unavailable.Message"] = "User membership lookup is unavailable until the server-side query gateway can be reached.",
            ["Tenants.UserLookup.State.Unavailable.Title"] = "User membership lookup is unavailable",
            ["Tenants.UserLookup.Status.Active"] = "Active",
            ["Tenants.UserLookup.Status.Disabled"] = "Disabled",
            ["Tenants.UserLookup.Status.Unknown"] = "Unknown status",
            ["Tenants.UserLookup.StatusAccessible"] = "Status: {0}",
            ["Tenants.UserLookup.Submit"] = "Look up",
            ["Tenants.UserLookup.TargetContext"] = "Lookup target: {0}",
            ["Tenants.UserLookup.Title"] = "User membership lookup",
            ["Tenants.Copy.Action"] = "Copy",
            ["Tenants.Copy.Label.TenantId"] = "Copy tenant identifier {0}",
            ["Tenants.Copy.Feedback.Copied"] = "Copied.",
            ["Tenants.Copy.Feedback.Disconnected"] = "Clipboard disconnected. Copy was not completed.",
            ["Tenants.Copy.Feedback.Empty"] = "Nothing is available to copy.",
            ["Tenants.Copy.Feedback.Failed"] = "Copy failed.",
            ["Tenants.Copy.Feedback.Unavailable"] = "Clipboard unavailable.",
            ["Tenants.Copy.Feedback.Unsafe"] = "This value is not support-safe to copy.",
        };
    }
}
