using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.Tenants.UI.Resources;

namespace Hexalith.Tenants.UI.Composition;

public static class TenantsFrontComposerRegistration {
    /// <summary>
    /// Authorization policy gating the Global Administrators menu entry. Mirrors the server-side
    /// global-administrator principal shape (system tenant + global administrator claim) that the BFF
    /// composition reflects, so the entry only surfaces for platform operators. Evaluated by the
    /// FrontComposer shell via <c>AuthorizeView</c>.
    /// </summary>
    public const string GlobalAdministratorPolicy = "Tenants.GlobalAdministrator";

    public static DomainManifest Manifest { get; } = new(
        "Tenants",
        "tenants",
        [],
        [],
        // Icon + localization for the left-nav category: the shell shows the BuildingPeople glyph on the
        // collapsed rail and resolves the category title ("Tenants" / "Locataires") from TenantsResources
        // per the request culture, matching the localized page body. Name stays the invariant fallback.
        Icon: "Regular.Size20.BuildingPeople",
        NameKey: "Tenants.Navigation.Tenants",
        Resource: typeof(TenantsResources));

    public static void RegisterDomain(IFrontComposerRegistry registry) {
        ArgumentNullException.ThrowIfNull(registry);

        // The manifest provides the localized "Tenants" category title for the shell's left navigation.
        registry.RegisterDomain(Manifest);

        // Domain modules contribute their left-menu items as plain data; the FrontComposer shell owns
        // all rendering (icons, grouping, active state, responsive collapse). Entries group under the
        // "tenants" bounded-context category in declared order. Each entry carries a TitleKey + Resource
        // so the shell localizes the label per request culture; the Title argument stays the invariant
        // English fallback that also drives stable test ids and sort order. The list entry is labelled
        // "All tenants" (not "Tenants") so the category and its first child are not the same word.
        registry.AddNavEntry(new FrontComposerNavEntry(
            "tenants",
            "All tenants",
            "/tenants",
            Order: 0,
            TitleKey: "Tenants.Navigation.AllTenants",
            Resource: typeof(TenantsResources)));
        registry.AddNavEntry(new FrontComposerNavEntry(
            "tenants",
            "My tenants",
            "/tenants/my",
            Order: 1,
            TitleKey: "Tenants.MyTenants.Link",
            Resource: typeof(TenantsResources)));
        registry.AddNavEntry(new FrontComposerNavEntry(
            "tenants",
            "User lookup",
            "/tenants/users",
            Icon: "Regular.Size20.Search",
            Order: 2,
            TitleKey: "Tenants.UserLookup.Link",
            Resource: typeof(TenantsResources)));
        registry.AddNavEntry(new FrontComposerNavEntry(
            "tenants",
            "Global Administrators",
            "/global-administrators",
            Icon: "Regular.Size20.Settings",
            Order: 3,
            RequiredPolicy: GlobalAdministratorPolicy,
            TitleKey: "Tenants.Navigation.GlobalAdministrators",
            Resource: typeof(TenantsResources)));
    }
}
