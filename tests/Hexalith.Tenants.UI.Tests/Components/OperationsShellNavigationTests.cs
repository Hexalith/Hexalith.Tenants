using System.Globalization;

using Bunit;

using Hexalith.Tenants.UI.Components.Layout;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantDetail;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class OperationsShellNavigationTests : BunitContext
{
    [Fact]
    public void Authorized_operator_sees_global_administrators_between_tenants_and_audit()
    {
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<OperationsShellNavigation> cut = Render<OperationsShellNavigation>();

        string markup = cut.Markup;
        int tenantsIndex = markup.IndexOf("data-testid=\"tenants-nav-tenants\"", StringComparison.Ordinal);
        int globalAdminsIndex = markup.IndexOf("data-testid=\"tenants-global-admins-nav\"", StringComparison.Ordinal);
        int auditIndex = markup.IndexOf("data-testid=\"tenants-nav-audit\"", StringComparison.Ordinal);

        tenantsIndex.ShouldBeGreaterThanOrEqualTo(0);
        globalAdminsIndex.ShouldBeGreaterThan(tenantsIndex);
        auditIndex.ShouldBeGreaterThan(globalAdminsIndex);
        cut.Find("[data-testid='tenants-global-admins-nav']").GetAttribute("href").ShouldBe("/global-administrators");
        cut.Find("[data-testid='tenants-nav-audit']").GetAttribute("aria-disabled").ShouldBe("true");
        cut.Markup.ShouldNotContain("Users", Case.Sensitive);
    }

    [Fact]
    public void Tenant_owner_without_platform_authority_does_not_see_global_administrators_nav()
    {
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Indeterminate));
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<OperationsShellNavigation> cut = Render<OperationsShellNavigation>();

        cut.Find("[data-testid='tenants-nav-tenants']");
        cut.Find("[data-testid='tenants-nav-audit']");
        cut.FindAll("[data-testid='tenants-global-admins-nav']").ShouldBeEmpty();
        cut.Markup.ShouldNotContain("Global Administrators");
        cut.Markup.ShouldNotContain("/global-administrators");
    }

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
            ["Tenants.Navigation.AriaLabel"] = "Operations shell primary navigation",
            ["Tenants.Navigation.Audit"] = "Audit",
            ["Tenants.Navigation.AuditUnavailable"] = "Audit area is unavailable until the audit UI read surface is implemented.",
            ["Tenants.Navigation.GlobalAdministrators"] = "Global Administrators",
            ["Tenants.Navigation.Tenants"] = "Tenants",
        };

        public LocalizedString this[string name]
            => new(name, Values[name]);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, string.Format(CultureInfo.CurrentCulture, Values[name], arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => Values.Select(static v => new LocalizedString(v.Key, v.Value));
    }
}
