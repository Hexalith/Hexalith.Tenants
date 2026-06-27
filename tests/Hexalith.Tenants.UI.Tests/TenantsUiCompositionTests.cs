using System.Globalization;
using System.Resources;
using System.Security.Claims;
using System.Xml.Linq;

using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Shell.Components.Icons;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.UI.Composition;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;

using Microsoft.AspNetCore.Http;
using Microsoft.FluentUI.AspNetCore.Components;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests;

public sealed class TenantsUiCompositionTests
{
    [Fact]
    public void FrontComposer_registration_exposes_tenants_nav_entries_and_minimal_manifest()
    {
        CapturingRegistry registry = new();

        TenantsFrontComposerRegistration.RegisterDomain(registry);

        // Domain menu is contributed as declarative data; the shell renders exactly one Tenants module entry
        // while page-local tabs own the Tenants-domain sub-surfaces.
        FrontComposerNavEntry navEntry = registry.NavEntries.ShouldHaveSingleItem();
        navEntry.Title.ShouldBe("Tenants");
        navEntry.Href.ShouldBe("/tenants");
        navEntry.BoundedContext.ShouldBe("tenants");
        navEntry.RequiredPolicy.ShouldBeNull();
        navEntry.TitleKey.ShouldBe("Tenants.Navigation.Tenants");
        navEntry.Resource.ShouldBe(typeof(TenantsResources));
        navEntry.Order.ShouldBe(0);

        // The legacy AddNavGroup stub is no longer used.
        registry.NavGroups.ShouldBeEmpty();

        DomainManifest manifest = registry.Manifests.ShouldHaveSingleItem();
        manifest.BoundedContext.ShouldBe("tenants");
        manifest.Projections.ShouldBeEmpty();
        manifest.Commands.ShouldBeEmpty();
        manifest.Icon.ShouldBe("Regular.Size20.BuildingPeople");
        manifest.NameKey.ShouldBe("Tenants.Navigation.Tenants");
        manifest.Resource.ShouldBe(typeof(TenantsResources));

        FcFluentIcons.TryCreate(manifest.Icon, out Icon? tenantIcon).ShouldBeTrue();
        tenantIcon.ShouldNotBeNull();
        tenantIcon!.Name.ShouldBe("BuildingPeople");
        tenantIcon.Size.ShouldBe(IconSize.Size20);
        tenantIcon.Variant.ShouldBe(IconVariant.Regular);
    }

    [Fact]
    public void Bff_composition_marks_read_and_command_surfaces_connected_after_command_gateway_story()
    {
        ITenantsBffComposition composition = new TenantsBffComposition(new StubTenantCommandGateway());

        composition.IsReadSurfaceConnected.ShouldBeTrue();
        composition.IsCommandSurfaceConnected.ShouldBeTrue();
    }

    [Fact]
    public void Bff_composition_keeps_command_surface_disconnected_for_unavailable_gateway()
    {
        ITenantsBffComposition composition = new TenantsBffComposition(new UnavailableTenantCommandGateway());

        composition.IsReadSurfaceConnected.ShouldBeTrue();
        composition.IsCommandSurfaceConnected.ShouldBeFalse();
    }

    [Fact]
    public void Bff_composition_reflects_lifecycle_authority_from_server_side_global_admin_principal()
    {
        ITenantsBffComposition composition = new TenantsBffComposition(
            new StubTenantCommandGateway(),
            ContextAccessor(
                new Claim("eventstore:tenant", "system"),
                new Claim(ClaimTypes.Role, "GlobalAdministrator")));

        composition.LifecycleAuthorizationReflection.ShouldBe(TenantLifecycleAuthorizationReflectionState.Authorized);
        composition.GlobalAdministratorsAuthorizationReflection.ShouldBe(TenantLifecycleAuthorizationReflectionState.Authorized);
    }

    [Fact]
    public void Bff_composition_reflects_global_admin_boolean_claim_from_keycloak_mapper()
    {
        ITenantsBffComposition composition = new TenantsBffComposition(
            new StubTenantCommandGateway(),
            ContextAccessor(
                new Claim("eventstore:tenant", "system"),
                new Claim("global_admin", "true")));

        composition.LifecycleAuthorizationReflection.ShouldBe(TenantLifecycleAuthorizationReflectionState.Authorized);
        composition.GlobalAdministratorsAuthorizationReflection.ShouldBe(TenantLifecycleAuthorizationReflectionState.Authorized);
    }

    [Fact]
    public void Global_administrator_claim_helper_matches_navigation_and_bff_authorization_shapes()
    {
        TenantsGlobalAdministratorClaims.IsGlobalAdministrator(Principal(
            new Claim("eventstore:tenant", "system"),
            new Claim("global_admin", "true"))).ShouldBeTrue();
        TenantsGlobalAdministratorClaims.IsGlobalAdministrator(Principal(
            new Claim("eventstore:tenant", "system"),
            new Claim("roles", "[\"tenant-reader\",\"global-admin\"]"))).ShouldBeTrue();
        TenantsGlobalAdministratorClaims.IsGlobalAdministrator(Principal(
            new Claim("eventstore:tenant", "system"),
            new Claim(ClaimTypes.Role, "GlobalAdministrator"))).ShouldBeTrue();

        TenantsGlobalAdministratorClaims.IsGlobalAdministrator(Principal(
            new Claim("eventstore:tenant", "system"),
            new Claim("global_admin", "false"))).ShouldBeFalse();
        TenantsGlobalAdministratorClaims.IsGlobalAdministrator(Principal(
            new Claim("global_admin", "true"))).ShouldBeFalse();
        TenantsGlobalAdministratorClaims.IsGlobalAdministrator(Principal(false,
            new Claim("eventstore:tenant", "system"),
            new Claim("global_admin", "true"))).ShouldBeFalse();
    }

    [Fact]
    public void Bff_composition_fails_closed_for_global_admin_shape_without_system_tenant_claim()
    {
        ITenantsBffComposition composition = new TenantsBffComposition(
            new StubTenantCommandGateway(),
            ContextAccessor(new Claim(ClaimTypes.Role, "GlobalAdministrator")));

        composition.LifecycleAuthorizationReflection.ShouldBe(TenantLifecycleAuthorizationReflectionState.Indeterminate);
        composition.GlobalAdministratorsAuthorizationReflection.ShouldBe(TenantLifecycleAuthorizationReflectionState.Indeterminate);
    }

    [Fact]
    public void Global_administrators_read_contract_uses_fixed_platform_scope_without_tenant_substitute()
    {
        string contractsQueryRoot = Path.Combine(ProjectRoot(), "src", "Hexalith.Tenants.Contracts", "Queries");
        string[] queryFiles = Directory.GetFiles(contractsQueryRoot, "*.cs", SearchOption.TopDirectoryOnly);
        queryFiles.Select(Path.GetFileName).ShouldContain("GetGlobalAdministratorsQuery.cs");
        queryFiles.Select(Path.GetFileName).ShouldNotContain("ListGlobalAdministratorsQuery.cs");

        string controller = File.ReadAllText(
            Path.Combine(ProjectRoot(), "src", "Hexalith.Tenants", "Controllers", "TenantsQueryController.cs"));
        controller.ShouldContain("[HttpGet(\"~/api/global-administrators\")]");
        controller.ShouldContain("GetGlobalAdministratorsQuery.Domain");
        controller.ShouldContain("TenantIdentity.GlobalAdministratorsAggregateId");
        controller.ShouldNotContain("[HttpGet(\"~/api/global-administrators/users\")]");
    }

    [Fact]
    public void Localization_resources_resolve_english_and_french_workspace_copy()
    {
        ResourceManager manager = new(typeof(TenantsResources));

        manager.GetString("Tenants.Workspace.UnavailableHeading", CultureInfo.InvariantCulture)
            .ShouldBe("Tenant read surfaces are not connected yet");
        manager.GetString("Tenants.Workspace.UnavailableHeading", CultureInfo.GetCultureInfo("fr"))
            .ShouldBe("Les surfaces de lecture des locataires ne sont pas encore connectées");
        manager.GetString("Tenants.UserLookup.Title", CultureInfo.InvariantCulture)
            .ShouldBe("User membership lookup");
        manager.GetString("Tenants.UserLookup.Title", CultureInfo.GetCultureInfo("fr"))
            .ShouldBe("Recherche des appartenances utilisateur");
        manager.GetString("Tenants.Create.State.ProjectionPending", CultureInfo.InvariantCulture)
            .ShouldBe("Projection pending; tenant is not confirmed visible yet.");
        manager.GetString("Tenants.Create.State.ProjectionPending", CultureInfo.GetCultureInfo("fr"))
            .ShouldBe("Projection en attente ; le locataire n'est pas encore confirmé visible.");
        manager.GetString("Tenants.RemoveMember.State.ProjectionPending", CultureInfo.InvariantCulture)
            .ShouldBe("Projection pending; the target user is not confirmed absent yet.");
        manager.GetString("Tenants.RemoveMember.State.ProjectionPending", CultureInfo.GetCultureInfo("fr"))
            .ShouldBe("Projection en attente ; l'utilisateur cible n'est pas encore confirmé absent.");
        manager.GetString("Tenants.GlobalAdministrators.Title", CultureInfo.InvariantCulture)
            .ShouldBe("Global Administrators");
        manager.GetString("Tenants.GlobalAdministrators.Title", CultureInfo.GetCultureInfo("fr"))
            .ShouldBe("Administrateurs globaux");
        manager.GetString("Tenants.GlobalAdministrators.State.Stale.Title", CultureInfo.InvariantCulture)
            .ShouldBe("Global administrator data stale");
        manager.GetString("Tenants.GlobalAdministrators.State.Stale.Title", CultureInfo.GetCultureInfo("fr"))
            .ShouldBe("Donnees d'administrateurs globaux perimees");
        manager.GetString("Tenants.GlobalAdministrators.State.Ready.Title", CultureInfo.InvariantCulture)
            .ShouldBe("Global administrators loaded");
        manager.GetString("Tenants.GlobalAdministrators.State.Ready.Title", CultureInfo.GetCultureInfo("fr"))
            .ShouldBe("Administrateurs globaux charges");
        manager.GetString("Tenants.GlobalAdministrators.State.Unauthorized.Title", CultureInfo.InvariantCulture)
            .ShouldBe("Platform area unavailable");
        manager.GetString("Tenants.GlobalAdministrators.State.Unauthorized.Title", CultureInfo.GetCultureInfo("fr"))
            .ShouldBe("Zone plateforme indisponible");
    }

    [Fact]
    public void Global_administrators_and_navigation_resources_have_english_french_key_parity()
    {
        string resourceRoot = Path.Combine(ProjectRoot(), "src", "Hexalith.Tenants.UI", "Resources");
        string[] prefixes = ["Tenants.GlobalAdministrators.", "Tenants.Navigation."];

        HashSet<string> englishKeys = ReadResourceKeys(Path.Combine(resourceRoot, "TenantsResources.resx"), prefixes);
        HashSet<string> frenchKeys = ReadResourceKeys(Path.Combine(resourceRoot, "TenantsResources.fr.resx"), prefixes);

        englishKeys.ShouldBe(frenchKeys);
    }

    [Fact]
    public void Audit_availability_resources_have_english_french_key_parity_and_no_machine_tokens()
    {
        string resourceRoot = Path.Combine(ProjectRoot(), "src", "Hexalith.Tenants.UI", "Resources");
        string[] prefixes = ["Tenants.Audit.Availability."];

        string englishPath = Path.Combine(resourceRoot, "TenantsResources.resx");
        string frenchPath = Path.Combine(resourceRoot, "TenantsResources.fr.resx");
        HashSet<string> englishKeys = ReadResourceKeys(englishPath, prefixes);
        HashSet<string> frenchKeys = ReadResourceKeys(frenchPath, prefixes);
        string englishAvailabilityValues = string.Join('\n', ReadResourceValues(englishPath, prefixes));
        string frenchAvailabilityValues = string.Join('\n', ReadResourceValues(frenchPath, prefixes));

        englishKeys.ShouldBe(frenchKeys);
        englishKeys.ShouldContain("Tenants.Audit.Availability.State.Pending");
        englishKeys.ShouldContain("Tenants.Audit.Availability.Action.ContinueReadOnly");
        englishAvailabilityValues.ShouldNotContain("AuditPending", Case.Insensitive);
        englishAvailabilityValues.ShouldNotContain("audit_pending", Case.Insensitive);
        frenchAvailabilityValues.ShouldNotContain("AuditPending", Case.Insensitive);
        frenchAvailabilityValues.ShouldNotContain("audit_pending", Case.Insensitive);
    }

    [Fact]
    public void Main_layout_composes_body_through_frontcomposer_shell()
    {
        string layout = File.ReadAllText(
            Path.Combine(ProjectRoot(), "src", "Hexalith.Tenants.UI", "Components", "Layout", "MainLayout.razor"));

        layout.ShouldContain("<FrontComposerShell>");
        layout.ShouldContain("@Body");
        // Sign in / out is now the framework header's FcAccountMenu (avatar) — the bespoke
        // content-area auth bar has been removed.
        layout.ShouldNotContain("tenants-auth-bar");
        // The left navigation is framework-owned now — no bespoke navigation slot/component.
        layout.ShouldNotContain("<Navigation>");
        layout.ShouldNotContain("OperationsShellNavigation");
    }

    [Fact]
    public void Styles_include_forced_colors_and_visible_focus_rules()
    {
        // 2026-06-25 ergonomic pass: TenantsWorkspace no longer ships component CSS. Its surface is
        // composed entirely from Fluent v5 primitives + FrontComposer chrome, which own their own focus
        // and forced-colors affordances; the prior workspace stylesheet only styled status/focus-link
        // classes that the markup no longer renders. The route <h1> focus ring is owned by FrontComposer
        // (FcPageHeader.razor.css). Pages that still hand-author bespoke surfaces keep their a11y CSS
        // pinned below.
        string globalAdminStyles = File.ReadAllText(
            Path.Combine(ProjectRoot(), "src", "Hexalith.Tenants.UI", "Components", "Pages", "GlobalAdministratorsPage.razor.css"));

        globalAdminStyles.ShouldContain("@media (forced-colors: active)");
        globalAdminStyles.ShouldContain(":focus-visible");
        globalAdminStyles.ShouldContain("outline");
    }

    private static string ProjectRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static HashSet<string> ReadResourceKeys(string path, string[] prefixes)
        => XDocument
            .Load(path)
            .Descendants("data")
            .Select(static element => element.Attribute("name")?.Value)
            .Where(name => name is not null && prefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
            .Select(name => name!)
            .ToHashSet(StringComparer.Ordinal);

    private static IEnumerable<string> ReadResourceValues(string path, string[] prefixes)
        => XDocument
            .Load(path)
            .Descendants("data")
            .Where(element =>
            {
                string? name = element.Attribute("name")?.Value;
                return name is not null && prefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal));
            })
            .Select(static element => element.Element("value")?.Value)
            .Where(static value => value is not null)
            .Select(static value => value!);

    private static IHttpContextAccessor ContextAccessor(params Claim[] claims)
        => new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = Principal(claims),
            },
        };

    private static ClaimsPrincipal Principal(params Claim[] claims)
        => Principal(authenticated: true, claims);

    private static ClaimsPrincipal Principal(bool authenticated, params Claim[] claims)
        => new(new ClaimsIdentity(claims, authenticated ? "test" : null));

    private sealed class CapturingRegistry : IFrontComposerRegistry, IFrontComposerNavEntryRegistry
    {
        public List<(string Name, string BoundedContext)> NavGroups { get; } = [];

        public List<FrontComposerNavEntry> NavEntries { get; } = [];

        public List<DomainManifest> Manifests { get; } = [];

        public void AddNavGroup(string name, string boundedContext)
            => NavGroups.Add((name, boundedContext));

        public void AddNavEntry(FrontComposerNavEntry entry)
            => NavEntries.Add(entry);

        public IReadOnlyList<FrontComposerNavEntry> GetNavEntries()
            => NavEntries;

        public IReadOnlyList<DomainManifest> GetManifests()
            => Manifests;

        public void RegisterDomain(DomainManifest manifest)
            => Manifests.Add(manifest);
    }

    private sealed class StubTenantCommandGateway : ITenantCommandGateway
    {
        public Task<TenantCommandSubmissionResult> CreateTenantAsync(CreateTenant request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"));

        public Task<TenantCommandSubmissionResult> AddUserToTenantAsync(AddUserToTenant request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> ChangeUserRoleAsync(ChangeUserRole request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> RemoveUserFromTenantAsync(RemoveUserFromTenant request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> UpdateTenantAsync(UpdateTenant request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> SetTenantConfigurationAsync(SetTenantConfiguration request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandStatusResult> GetStatusAsync(TenantCommandTrackingHandle handle, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandStatusResult.Unknown("Not used."));
    }
}
