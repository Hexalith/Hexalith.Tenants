using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.Tenants.UI.Resources;

namespace Hexalith.Tenants.UI.Composition;

public static class TenantsFrontComposerRegistration {
    /// <summary>
    /// Authorization policy for Global Administrators surfaces. Mirrors the server-side global-administrator
    /// principal shape (system tenant + global administrator claim) that the BFF composition reflects.
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

        // The module contributes one shell entry. Page-local tabs own the Tenants-domain sub-surfaces.
        registry.AddNavEntry(new FrontComposerNavEntry(
            "tenants",
            "Tenants",
            "/tenants",
            Order: 0,
            TitleKey: "Tenants.Navigation.Tenants",
            Resource: typeof(TenantsResources)));
    }
}
