using System.Globalization;
using System.Resources;

using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.Tenants.UI.Composition;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests;

public sealed class TenantsUiCompositionTests
{
    [Fact]
    public void FrontComposer_registration_exposes_minimal_tenants_manifest_without_fake_projections()
    {
        CapturingRegistry registry = new();

        TenantsFrontComposerRegistration.RegisterDomain(registry);

        registry.NavGroups.ShouldContain(("Tenants", "tenants"));
        DomainManifest manifest = registry.Manifests.ShouldHaveSingleItem();
        manifest.BoundedContext.ShouldBe("tenants");
        manifest.Projections.ShouldBeEmpty();
        manifest.Commands.ShouldBeEmpty();
    }

    [Fact]
    public void Bff_composition_marks_read_surface_connected_after_list_gateway_story()
    {
        ITenantsBffComposition composition = new TenantsBffComposition();

        composition.IsReadSurfaceConnected.ShouldBeTrue();
        composition.IsCommandSurfaceConnected.ShouldBeFalse();
    }

    [Fact]
    public void Localization_resources_resolve_english_and_french_workspace_copy()
    {
        ResourceManager manager = new(typeof(TenantsResources));

        manager.GetString("Tenants.Workspace.UnavailableHeading", CultureInfo.InvariantCulture)
            .ShouldBe("Tenant read surfaces are not connected yet");
        manager.GetString("Tenants.Workspace.UnavailableHeading", CultureInfo.GetCultureInfo("fr"))
            .ShouldBe("Les surfaces de lecture des locataires ne sont pas encore connectées");
    }

    [Fact]
    public void Main_layout_composes_body_through_frontcomposer_shell()
    {
        string layout = File.ReadAllText(
            Path.Combine(ProjectRoot(), "src", "Hexalith.Tenants.UI", "Components", "Layout", "MainLayout.razor"));

        layout.ShouldContain("<FrontComposerShell>@Body</FrontComposerShell>");
    }

    [Fact]
    public void Styles_include_forced_colors_and_visible_focus_rules()
    {
        string styles = File.ReadAllText(
            Path.Combine(ProjectRoot(), "src", "Hexalith.Tenants.UI", "Components", "Pages", "TenantsWorkspace.razor.css"));

        styles.ShouldContain("@media (forced-colors: active)");
        styles.ShouldContain(":focus-visible");
        styles.ShouldContain("outline");
    }

    private static string ProjectRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private sealed class CapturingRegistry : IFrontComposerRegistry
    {
        public List<(string Name, string BoundedContext)> NavGroups { get; } = [];

        public List<DomainManifest> Manifests { get; } = [];

        public void AddNavGroup(string name, string boundedContext)
            => NavGroups.Add((name, boundedContext));

        public IReadOnlyList<DomainManifest> GetManifests()
            => Manifests;

        public void RegisterDomain(DomainManifest manifest)
            => Manifests.Add(manifest);
    }
}
