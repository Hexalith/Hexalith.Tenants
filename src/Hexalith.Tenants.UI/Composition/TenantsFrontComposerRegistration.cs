using Hexalith.FrontComposer.Contracts.Registration;

namespace Hexalith.Tenants.UI.Composition;

public static class TenantsFrontComposerRegistration
{
    /// <summary>
    /// Authorization policy gating the Global Administrators menu entry. Mirrors the server-side
    /// global-administrator principal shape (system tenant + GlobalAdministrator role) that the BFF
    /// composition reflects, so the entry only surfaces for platform operators. Evaluated by the
    /// FrontComposer shell via <c>AuthorizeView</c>.
    /// </summary>
    public const string GlobalAdministratorPolicy = "Tenants.GlobalAdministrator";

    public static DomainManifest Manifest { get; } = new(
        "Tenants",
        "tenants",
        [],
        []);

    public static void RegisterDomain(IFrontComposerRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        // The manifest provides the "Tenants" category title for the shell's left navigation.
        registry.RegisterDomain(Manifest);

        // Domain modules contribute their left-menu items as plain data; the FrontComposer shell owns
        // all rendering (icons, grouping, active state, responsive collapse). Entries group under the
        // "tenants" bounded-context category in declared order.
        registry.AddNavEntry(new FrontComposerNavEntry("tenants", "Tenants", "/tenants", Order: 0));
        registry.AddNavEntry(new FrontComposerNavEntry("tenants", "My tenants", "/tenants/my", Order: 1));
        registry.AddNavEntry(new FrontComposerNavEntry(
            "tenants",
            "User lookup",
            "/tenants/users",
            Icon: "Regular.Size20.Search",
            Order: 2));
        registry.AddNavEntry(new FrontComposerNavEntry(
            "tenants",
            "Global Administrators",
            "/global-administrators",
            Icon: "Regular.Size20.Settings",
            Order: 3,
            RequiredPolicy: GlobalAdministratorPolicy));
    }
}
