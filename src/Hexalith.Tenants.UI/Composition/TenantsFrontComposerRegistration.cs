using Hexalith.FrontComposer.Contracts.Registration;

namespace Hexalith.Tenants.UI.Composition;

public static class TenantsFrontComposerRegistration
{
    public static DomainManifest Manifest { get; } = new(
        "Tenants",
        "tenants",
        [],
        []);

    public static void RegisterDomain(IFrontComposerRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        registry.AddNavGroup("Tenants", "tenants");
        registry.AddNavGroup("Global Administrators", "global-administrators");
        registry.AddNavGroup("Audit", "audit");
        registry.RegisterDomain(Manifest);
    }
}
