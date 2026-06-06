using System.Globalization;

using Bunit;

using Hexalith.Tenants.UI.Components.Pages;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantDetail;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class GlobalAdministratorsPageTests : BunitContext
{
    [Fact]
    public void Authorized_operator_sees_missing_read_support_without_fabricated_rows_or_counts()
    {
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.Find("[data-testid='tenants-global-admins-area']");
        cut.Find("[data-testid='tenants-global-admins-unavailable']").GetAttribute("role").ShouldBe("alert");
        cut.Find("[data-testid='tenants-global-admins-live-region']").GetAttribute("aria-live").ShouldBe("assertive");
        cut.Find("[data-testid='tenants-global-admins-read-contract']").TextContent.ShouldContain("global-administrators");
        cut.Find("[data-testid='tenants-global-admins-read-contract']").TextContent.ShouldContain("system");
        cut.Find("[data-testid='tenants-global-admins-recovery']").TextContent.ShouldContain("Do not use tenant");
        cut.Markup.ShouldNotContain("success", Case.Insensitive);
        cut.Markup.ShouldNotContain("administrator row", Case.Insensitive);
        cut.Markup.ShouldNotContain("administrator count", Case.Insensitive);
        cut.Markup.ShouldNotContain("/api/tenants", Case.Insensitive);
        cut.Markup.ShouldNotContain("/api/users", Case.Insensitive);
        cut.Markup.ShouldNotContain("data-testid=\"tenants-global-admins-nav\"");
    }

    [Fact]
    public void Tenant_owner_without_platform_authority_gets_fail_closed_unavailable_state()
    {
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Indeterminate));
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.Find("[data-testid='tenants-global-admins-area']");
        cut.Find("[data-testid='tenants-global-admins-unavailable']").TextContent.ShouldContain("Platform area unavailable");
        cut.Find("[data-testid='tenants-global-admins-live-region']").GetAttribute("aria-live").ShouldBe("assertive");
        cut.Markup.ShouldNotContain("GlobalAdministrator");
        cut.Markup.ShouldNotContain("global administrator list", Case.Insensitive);
        cut.Markup.ShouldNotContain("0 administrators", Case.Insensitive);
        cut.Markup.ShouldNotContain("success", Case.Insensitive);
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

    private sealed class StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState reflection) : ITenantsBffComposition
    {
        public bool IsReadSurfaceConnected => true;

        public bool IsCommandSurfaceConnected => true;

        public TenantLifecycleAuthorizationReflectionState GlobalAdministratorsAuthorizationReflection => reflection;
    }

    private sealed class StubTenantsLocalizer : IStringLocalizer<TenantsResources>
    {
        private static readonly Dictionary<string, string> Values = new(StringComparer.Ordinal)
        {
            ["Tenants.GlobalAdministrators.Aggregate.Domain.Label"] = "Domain",
            ["Tenants.GlobalAdministrators.Aggregate.Domain.Value"] = "global-administrators",
            ["Tenants.GlobalAdministrators.Aggregate.Id.Label"] = "Aggregate id",
            ["Tenants.GlobalAdministrators.Aggregate.Id.Value"] = "global-administrators",
            ["Tenants.GlobalAdministrators.Aggregate.Tenant.Label"] = "Tenant scope",
            ["Tenants.GlobalAdministrators.Aggregate.Tenant.Value"] = "system",
            ["Tenants.GlobalAdministrators.Eyebrow"] = "Platform governance",
            ["Tenants.GlobalAdministrators.ReadContract.Description"] = "No confirmed global-administrator read query or REST route exists in this UI story.",
            ["Tenants.GlobalAdministrators.ReadContract.Title"] = "Read contract boundary",
            ["Tenants.GlobalAdministrators.Recovery.MissingPermission"] = "Request platform authority through the approved operations path; this page does not reveal hidden authority data.",
            ["Tenants.GlobalAdministrators.Recovery.MissingPermission.Title"] = "Recovery guidance",
            ["Tenants.GlobalAdministrators.Recovery.MissingReadSupport"] = "Do not use tenant, member, or user membership endpoints as a substitute. Wait for a backend/API read-contract story.",
            ["Tenants.GlobalAdministrators.Recovery.MissingReadSupport.Title"] = "Recovery guidance",
            ["Tenants.GlobalAdministrators.RestrictedTitle"] = "Platform area unavailable",
            ["Tenants.GlobalAdministrators.States.MissingPermission"] = "Missing platform authority",
            ["Tenants.GlobalAdministrators.States.MissingReadSupport"] = "Missing implementation support",
            ["Tenants.GlobalAdministrators.States.UnknownFreshness"] = "Freshness unknown until a read route exists",
            ["Tenants.GlobalAdministrators.Title"] = "Global Administrators",
            ["Tenants.GlobalAdministrators.Unavailable.MissingPermission.Message"] = "Server-side authorization reflection did not confirm platform authority. The area fails closed.",
            ["Tenants.GlobalAdministrators.Unavailable.MissingPermission.Title"] = "Platform area unavailable",
            ["Tenants.GlobalAdministrators.Unavailable.MissingReadSupport.Message"] = "The fixed aggregate has projection evidence, but there is no confirmed UI read contract yet.",
            ["Tenants.GlobalAdministrators.Unavailable.MissingReadSupport.Title"] = "Global administrator read support is not implemented yet",
        };

        public LocalizedString this[string name]
            => new(name, Values[name]);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, string.Format(CultureInfo.CurrentCulture, Values[name], arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => Values.Select(static v => new LocalizedString(v.Key, v.Value));
    }
}
