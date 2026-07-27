using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using Hexalith.EventStore.Client.Queries;
using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Shell.Components.Icons;
using Hexalith.Memories.Client.Rest;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.UI.Composition;
using Hexalith.Tenants.UI.Extensions;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Configuration;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.FluentUI.AspNetCore.Components;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests;

public sealed class TenantsUiCompositionTests
{
    private static readonly TimeSpan DependencyResolutionTimeout = TimeSpan.FromMinutes(3);

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
    public void Configuration_policy_services_are_idempotent_and_do_not_make_missing_policy_startup_fatal()
    {
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder().Build();

        services.AddHexalithTenantsUiModule(configuration, enableGatewayAuthorization: false);
        services.AddHexalithTenantsUiModule(configuration, enableGatewayAuthorization: false);

        services.Count(descriptor => descriptor.ServiceType == typeof(ITenantConfigurationPrincipalResolver)).ShouldBe(1);
        services.Count(descriptor => descriptor.ServiceType == typeof(TenantConfigurationReadPolicyProvider)).ShouldBe(1);

        using ServiceProvider provider = services.BuildServiceProvider();
        TenantConfigurationReadPolicyProvider policyProvider = provider.GetRequiredService<TenantConfigurationReadPolicyProvider>();
        TenantConfigurationReadPolicyResolution resolution = policyProvider.Resolve(
            "tenant.alpha",
            TenantConfigurationPrincipalEvidence.NonAdministrator("operator-user"));

        resolution.IsAvailable.ShouldBeFalse();
    }

    [Fact]
    public void Tenant_search_composition_is_purpose_isolated_scoped_and_server_configured()
    {
        ServiceCollection services = new();

        // Purpose isolation is proven with ONE Data Protection provider so only the codec purposes vary;
        // two providers would prove key-ring separation instead.
        var dataProtectionProvider = new EphemeralDataProtectionProvider();
        var hostCodec = new QueryCursorCodec(dataProtectionProvider, "host-purpose");
        services.AddSingleton<IDataProtectionProvider>(dataProtectionProvider);
        services.AddSingleton<IQueryCursorCodec>(hostCodec);
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EventStore:BaseAddress"] = "https://eventstore.invalid",
                ["Memories:BaseAddress"] = "https://memories.invalid",
                ["HEXALITH_MEMORIES_API_TOKEN"] = "server-only-token",
            })
            .Build();

        services.AddHexalithTenantsUiModule(configuration, enableGatewayAuthorization: false);

        using ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);
        provider.GetRequiredService<IDataProtectionProvider>().ShouldBeSameAs(dataProtectionProvider);
        provider.GetRequiredService<IQueryCursorCodec>().ShouldBeSameAs(hostCodec);
        ITenantSearchCursorCodec searchCodec = provider.GetRequiredService<ITenantSearchCursorCodec>();
        provider.GetRequiredService<ITenantSearchCursorCodec>().ShouldBeSameAs(searchCodec);
        string scope = TenantSearchCursorScopes.Create(
            "operator-user",
            "needle",
            status: null,
            TenantListSortColumns.TenantId,
            descending: false,
            pageSize: 20);
        string protectedCursor = searchCodec.Encode(scope, 20);
        hostCodec.TryDecode(
            protectedCursor,
            TenantSearchCursorPosition.QueryType,
            scope,
            out _,
            out _).ShouldBeFalse();
        searchCodec.TryDecode(protectedCursor, scope, out int decodedOffset).ShouldBeTrue();
        decodedOffset.ShouldBe(20);

        // Purpose isolation was previously inferred from the *container's* provider identity, which says
        // nothing about which provider the codec actually received. A codec that ignored injection and
        // built its own EphemeralDataProtectionProvider passed every assertion while making each cursor
        // undecodable after a restart or on a second replica. Prove the injected provider is the one in
        // use: an independent codec over the same provider must decode this cursor, and the same codec
        // over a different provider must not.
        var sameProviderCodec = new TenantSearchCursorCodec(dataProtectionProvider);
        sameProviderCodec.TryDecode(protectedCursor, scope, out int sameProviderOffset).ShouldBeTrue(
            "the registered codec must protect with the injected provider, not one it constructed itself");
        sameProviderOffset.ShouldBe(20);

        var foreignProviderCodec = new TenantSearchCursorCodec(new EphemeralDataProtectionProvider());
        foreignProviderCodec.TryDecode(protectedCursor, scope, out _).ShouldBeFalse();

        using IServiceScope firstScope = provider.CreateScope();
        using IServiceScope secondScope = provider.CreateScope();
        TenantSearchPagingState firstPaging = firstScope.ServiceProvider.GetRequiredService<TenantSearchPagingState>();
        firstPaging.ShouldBeSameAs(firstScope.ServiceProvider.GetRequiredService<TenantSearchPagingState>());
        firstPaging.ShouldNotBeSameAs(secondScope.ServiceProvider.GetRequiredService<TenantSearchPagingState>());

        MemoriesClient memories = firstScope.ServiceProvider.GetRequiredService<MemoriesClient>();
        memories.BaseAddress.ShouldBe(new Uri("https://memories.invalid"));
        MemoriesClientOptions options = provider.GetRequiredService<IOptions<MemoriesClientOptions>>().Value;
        options.Endpoint.ShouldBe(new Uri("https://memories.invalid"));
        options.ApiToken.ShouldBe("server-only-token");
    }

    [Fact]
    public async Task Embedded_ui_module_emits_no_default_memories_http_logs_that_could_carry_query_or_offset()
    {
        CapturingLoggerProvider capture = new();
        ServiceCollection services = new();
        services.AddLogging(builder =>
        {
            _ = builder.SetMinimumLevel(LogLevel.Trace);
            _ = builder.AddProvider(capture);
        });
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Memories:BaseAddress"] = "https://memories.invalid",
            })
            .Build();
        services.AddHexalithTenantsUiModule(configuration, enableGatewayAuthorization: false);
        AddStubPrimaryHandler(services, MemoriesHttpClientName);
        AddStubPrimaryHandler(services, ControlHttpClientName);

        using ServiceProvider provider = services.BuildServiceProvider();

        await AssertMemoriesHttpLoggingIsSuppressedAsync(provider, capture);
    }

    [Fact]
    public async Task Standalone_ui_host_resolves_the_same_server_side_search_composition()
    {
        CapturingLoggerProvider capture = new();
        await using WebApplicationFactory<global::Program> baseFactory = new();
        using WebApplicationFactory<global::Program> factory = baseFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.AddLogging(logging =>
                {
                    _ = logging.SetMinimumLevel(LogLevel.Trace);
                    _ = logging.AddProvider(capture);
                });
                AddStubPrimaryHandler(services, MemoriesHttpClientName);
                AddStubPrimaryHandler(services, ControlHttpClientName);
            }));

        ITenantSearchCursorCodec searchCodec = factory.Services.GetRequiredService<ITenantSearchCursorCodec>();
        searchCodec.ShouldBeSameAs(factory.Services.GetRequiredService<ITenantSearchCursorCodec>());

        using IServiceScope firstScope = factory.Services.CreateScope();
        using IServiceScope secondScope = factory.Services.CreateScope();
        firstScope.ServiceProvider.GetRequiredService<TenantSearchPagingState>()
            .ShouldNotBeSameAs(secondScope.ServiceProvider.GetRequiredService<TenantSearchPagingState>());

        // The query gateway takes an optional reason-code logger; resolving it proves the added parameter
        // still satisfies the container's constructor selection in a real host.
        firstScope.ServiceProvider.GetRequiredService<ITenantQueryGateway>().ShouldNotBeNull();

        // Lifetimes alone hold for any registration whatsoever. The standalone host protects real cursors,
        // so the substance the embedded-module test proves is proven here too, against the real host: a
        // round trip, scope binding, protection by the host's own Data Protection provider rather than one
        // the codec built for itself, and a search purpose that is not interchangeable with the generic
        // query-cursor purpose.
        string scope = TenantSearchCursorScopes.Create(
            "operator-user",
            "needle",
            status: null,
            TenantListSortColumns.TenantId,
            descending: false,
            pageSize: 20);
        string protectedCursor = searchCodec.Encode(scope, 20);
        searchCodec.TryDecode(protectedCursor, scope, out int decodedOffset).ShouldBeTrue();
        decodedOffset.ShouldBe(20);
        searchCodec.TryDecode(protectedCursor, scope + "-other", out _).ShouldBeFalse();

        IDataProtectionProvider hostProvider = factory.Services.GetRequiredService<IDataProtectionProvider>();
        new TenantSearchCursorCodec(hostProvider)
            .TryDecode(protectedCursor, scope, out int hostProviderOffset)
            .ShouldBeTrue("the registered codec must protect with the host's provider, not one it constructed");
        hostProviderOffset.ShouldBe(20);
        new TenantSearchCursorCodec(new EphemeralDataProtectionProvider())
            .TryDecode(protectedCursor, scope, out _)
            .ShouldBeFalse();
        new QueryCursorCodec(hostProvider, "host-purpose")
            .TryDecode(protectedCursor, TenantSearchCursorPosition.QueryType, scope, out _, out _)
            .ShouldBeFalse();

        await AssertMemoriesHttpLoggingIsSuppressedAsync(factory.Services, capture);
    }

    [Fact]
    public void Authoritative_search_resources_resolve_complete_english_and_french_copy()
    {
        ResourceManager manager = new(typeof(TenantsResources));
        Dictionary<string, (string English, string French)> expected = new(StringComparer.Ordinal)
        {
            ["Tenants.List.Notice.SearchUnavailable"] = (
                "Protected whole-set search is temporarily unavailable. You can continue browsing the authorized tenant list.",
                "La recherche protégée sur l'ensemble des locataires est temporairement indisponible. Vous pouvez continuer à parcourir la liste autorisée."),
            // Not "the page was no longer available": the browser-Back path discards a position that was
            // still perfectly available, because the return context could not be validated. The copy has to
            // be true of every trigger that raises it.
            ["Tenants.List.Notice.SearchRefreshed"] = (
                "Protected search paging could not be restored. Search has restarted from the first page.",
                "La pagination de recherche protégée n'a pas pu être restaurée. La recherche a redémarré depuis la première page."),
            ["Tenants.List.Notice.SearchAndListUnavailable"] = (
                "Protected whole-set search is temporarily unavailable, and the authorized tenant list could not be loaded either. Try again later.",
                "La recherche protégée sur l'ensemble des locataires est temporairement indisponible, et la liste autorisée n'a pas pu être chargée non plus. Réessayez plus tard."),
            ["Tenants.List.Notice.SearchTermTooLong"] = (
                "The search term was too long to apply, so the full authorized tenant list is shown. Shorten the term and search again.",
                "Le terme recherché était trop long pour être appliqué, la liste autorisée complète est donc affichée. Raccourcissez le terme et relancez la recherche."),
            ["Tenants.List.Notice.SearchPagingRestarted"] = (
                "The available tenant source changed. Paging restarted from the first page.",
                "La source de locataires disponible a changé. La pagination a redémarré depuis la première page."),
            ["Tenants.List.State.SearchPageEmpty.Title"] = (
                "No tenants match this search",
                "Aucun locataire ne correspond à cette recherche"),

            // One message for both causes. A window that yields no authorized row and a window that
            // matched nothing must be described identically, or the copy itself discloses that hidden
            // candidates existed.
            ["Tenants.List.State.SearchPageEmpty.Message"] = (
                "No tenants you can access match this search. Check the search term, or clear it to return to the full list.",
                "Aucun locataire auquel vous avez accès ne correspond à cette recherche. Vérifiez le terme recherché ou effacez-le pour revenir à la liste complète."),
            ["Tenants.List.Reason.SearchPartiallyAvailable"] = (
                "Some search results could not be verified. Only authorized tenant rows that were verified are shown.",
                "Certains résultats de recherche n'ont pas pu être vérifiés. Seules les lignes de locataire autorisées et vérifiées sont affichées."),
            ["Tenants.List.StatusFilterLabel.Authoritative"] = (
                "Status across indexed candidates",
                "Statut parmi les candidats indexés"),
            ["Tenants.List.AuthoritativeSearchSemantics"] = (
                "Search and status apply across indexed tenant candidates. Only authorized, verified tenant rows are shown; sorting applies within this protected page.",
                "La recherche et le statut s'appliquent aux candidats de locataire indexés. Seules les lignes autorisées et vérifiées sont affichées ; le tri s'applique dans cette page protégée."),
        };

        foreach ((string key, (string english, string french)) in expected)
        {
            manager.GetString(key, CultureInfo.InvariantCulture).ShouldBe(english);
            manager.GetString(key, CultureInfo.GetCultureInfo("fr")).ShouldBe(french);
        }
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

        File.Exists(Path.Combine(ProjectRoot(), "src", "Hexalith.Tenants", "Controllers", "TenantsQueryController.cs"))
            .ShouldBeFalse();
        string apiAssemblyInfo = File.ReadAllText(
            Path.Combine(ProjectRoot(), "src", "Hexalith.Tenants.Api", "RestApiAssemblyInfo.cs"));
        apiAssemblyInfo.ShouldContain("RestApi(\"api/tenants\", \"tenants\", RestTenantSource.System)");
        string globalAdministratorsQuery = File.ReadAllText(
            Path.Combine(contractsQueryRoot, "GetGlobalAdministratorsQuery.cs"));
        globalAdministratorsQuery.ShouldContain("[RestRoute(RestVerb.Get, \"~/api/global-administrators\", ApiScope = \"tenants\")]");
        globalAdministratorsQuery.ShouldContain("RestQueryBindingSource.Constant, \"global-administrators\"");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TenantsUiProject_DoesNotHostGeneratedRestApiOrReferenceExternalApiHost(bool useProjectReferences)
    {
        string uiRoot = Path.Combine(ProjectRoot(), "src", "Hexalith.Tenants.UI");
        string uiProjectPath = Path.Combine(uiRoot, "Hexalith.Tenants.UI.csproj");

        // Each resolved dependency mode is asserted on its own. Unioning the two modes let a
        // non-empty result from one satisfy the positive assertion for both, and let every negative
        // assertion pass vacuously whenever a mode returned nothing at all.
        string[] dependencies = await ReadResolvedDependencyValuesAsync(uiProjectPath, useProjectReferences);

        dependencies.ShouldNotBeEmpty(
            $"MSBuild resolution returned no dependency items for UseHexalithProjectReferences={useProjectReferences}; "
            + "every assertion below would pass vacuously.");
        dependencies.ShouldContain(
            static dependency => MatchesDependencyIdentity(dependency, "Hexalith.Tenants.Client"),
            "Interactive Tenants UI must consume the approved typed Tenants client seam.");
        dependencies.Where(static dependency => MatchesDependencyIdentity(dependency, "Hexalith.Tenants.Api"))
            .ShouldBeEmpty("Interactive Tenants UI must not reference the external generated API host.");
        dependencies.Where(static dependency => MatchesDependencyIdentity(dependency, "Hexalith.EventStore.RestApi.Generators"))
            .ShouldBeEmpty("Generated REST analyzers belong only in Hexalith.Tenants.Api.");
        dependencies.Where(static dependency => MatchesDependencyIdentity(dependency, "Hexalith.Tenants"))
            .ShouldBeEmpty("Interactive Tenants UI must not reference the Tenants domain-service host.");
    }

    /// <summary>
    /// Complements the resolved dependency-closure test with compiled-artifact assertions: the REST
    /// generator is opt-in on an assembly-level <c>RestApi</c> attribute, and the referenced-assembly
    /// set proves which client and contract seams the compiled UI actually binds to.
    /// </summary>
    [Fact]
    public void TenantsUiAssembly_BindsTypedClientSeamAndCarriesNoRestApiOptIn()
    {
        Assembly uiAssembly = typeof(TenantsUiServiceCollectionExtensions).Assembly;
        string[] referenced = uiAssembly
            .GetReferencedAssemblies()
            .Select(static name => name.Name ?? string.Empty)
            .ToArray();

        referenced.ShouldNotBeEmpty("Referenced-assembly metadata is required for the assertions below.");
        referenced.ShouldContain(
            "Hexalith.EventStore.Client",
            "Interactive Tenants UI must bind the EventStore typed query client; this is the seam AC1 "
            + "requires and it reaches the UI transitively, so only the compiled assembly can prove it.");
        referenced.ShouldContain(
            "Hexalith.Tenants.Contracts",
            "Interactive Tenants UI must bind the shared Tenants contracts through the typed client seam.");

        foreach (string forbidden in new[] { "Hexalith.Tenants.Api", "Hexalith.Tenants", "Hexalith.EventStore.RestApi.Generators" })
        {
            referenced.ShouldNotContain(
                forbidden,
                $"Interactive Tenants UI must not directly bind {forbidden}; the resolved MSBuild closure test covers transitive acquisition.");
        }

        // "Components never call Memories" was an unenforced claim. Hexalith.Memories.Client.Rest is
        // necessarily referenced -- the gateway uses it -- so the assembly-level ban above cannot express
        // this. Adding @inject MemoriesClient to a razor component would have been caught by nothing.
        Type[] componentTypes = uiAssembly.GetTypes()
            .Where(static type => typeof(Microsoft.AspNetCore.Components.IComponent).IsAssignableFrom(type))
            .ToArray();
        componentTypes.ShouldNotBeEmpty("the component scan must observe real components to mean anything");

        foreach (Type componentType in componentTypes)
        {
            IEnumerable<Type> injectedTypes = componentType
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(static property => property.GetCustomAttributes(typeof(Microsoft.AspNetCore.Components.InjectAttribute), inherit: true).Length > 0)
                .Select(static property => property.PropertyType)
                .Concat(componentType
                    .GetConstructors()
                    .SelectMany(static constructor => constructor.GetParameters())
                    .Select(static parameter => parameter.ParameterType));

            foreach (Type injected in injectedTypes)
            {
                (injected.FullName ?? string.Empty).StartsWith("Hexalith.Memories", StringComparison.Ordinal)
                    .ShouldBeFalse(
                        $"{componentType.FullName} must reach Memories through the server-side gateway, never directly.");
            }
        }

        // The declared-injection scan above is necessary but not sufficient, and on its own it was blind to
        // the idiom this codebase actually writes: TenantsWorkspace injects IServiceProvider and resolves
        // through it, so Services.GetRequiredService<MemoriesClient>() inside any component satisfied every
        // assertion above. IServiceProvider is not a Hexalith.Memories type, and Lazy<MemoriesClient> reports
        // System.Lazy. A source scan closes the service-locator route that reflection over declared
        // dependencies cannot see.
        string componentsRoot = Path.Combine(UiProjectRoot(), "Components");
        Directory.Exists(componentsRoot).ShouldBeTrue($"the component source scan must find {componentsRoot}");
        string[] componentSources = Directory
            .EnumerateFiles(componentsRoot, "*.*", SearchOption.AllDirectories)
            .Where(static path => path.EndsWith(".razor", StringComparison.Ordinal)
                || path.EndsWith(".cs", StringComparison.Ordinal))
            .ToArray();
        componentSources.Length.ShouldBeGreaterThan(10, "the source scan must observe real component files");

        List<string> memoriesReferences = [];
        foreach (string path in componentSources)
        {
            if (File.ReadAllText(path).Contains("Memories", StringComparison.Ordinal))
            {
                memoriesReferences.Add(Path.GetRelativePath(componentsRoot, path));
            }
        }

        memoriesReferences.ShouldBeEmpty(
            "No Tenants UI component may name Memories at all -- not by injection, not through "
            + "IServiceProvider, not through a wrapper. The index is reached only by the server-side gateway. "
            + "Offending files: " + string.Join(", ", memoriesReferences));

        uiAssembly.GetCustomAttributesData()
            .Where(static attribute => string.Equals(
                attribute.AttributeType.FullName,
                "Hexalith.EventStore.Contracts.Rest.RestApiAttribute",
                StringComparison.Ordinal))
            .ShouldBeEmpty(
                "Interactive Tenants UI must not declare the generated-REST assembly opt-in. The generator "
                + "emits controllers only for assemblies carrying this attribute, so its absence keeps the "
                + "generator inert even if it were acquired transitively.");
    }

    [Fact]
    public void TenantsUiAssembly_DoesNotContainMvcControllers()
    {
        var manager = new ApplicationPartManager();
        manager.ApplicationParts.Add(new AssemblyPart(typeof(TenantsUiServiceCollectionExtensions).Assembly));
        manager.FeatureProviders.Add(new ControllerFeatureProvider());
        var controllers = new ControllerFeature();
        manager.PopulateFeature(controllers);

        controllers.Controllers.ShouldBeEmpty(
            "Interactive Tenants UI must not compile any MVC-discoverable controller, including POCO types marked [Controller].");
    }

    [Fact]
    public async Task TenantsUiHost_DoesNotExposeTenantManagementApiEndpoints()
    {
        await using var factory = new WebApplicationFactory<global::Program>();
        Endpoint[] endpoints = factory.Services
            .GetServices<EndpointDataSource>()
            .SelectMany(static source => source.Endpoints)
            .ToArray();

        // Positive control. Without it the negative assertion below would pass for the wrong reason
        // if the reduced test host ever stopped producing endpoints at all.
        endpoints.ShouldNotBeEmpty(
            "The Tenants UI test host produced no endpoints, so the forbidden-endpoint assertion "
            + "below would pass vacuously rather than because the host is clean.");

        string[] forbiddenEndpoints = endpoints
            .Where(static endpoint => endpoint.Metadata.GetMetadata<ControllerActionDescriptor>() is not null
                || endpoint is RouteEndpoint route && IsTenantManagementApiRoute(route.RoutePattern.RawText))
            .Select(static endpoint => endpoint.DisplayName ?? "<unnamed endpoint>")
            .ToArray();

        forbiddenEndpoints.ShouldBeEmpty(
            "Interactive Tenants UI must not expose MVC controllers or tenant-management API routes.");
    }

    [Theory]
    [InlineData("api/tenants")]
    [InlineData("api/v1/tenants/{tenantId}")]
    [InlineData("tenant-gateway/api/v12/users/{userId}/tenants")]
    [InlineData("{deploymentPrefix}/api/global-administrators")]
    public void Tenant_management_route_detection_is_prefix_and_version_independent(string routePattern)
    {
        IsTenantManagementApiRoute(routePattern).ShouldBeTrue();
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
    public void Clipboard_feedback_is_complete_polite_recovery_copy_in_english_and_french()
    {
        ResourceManager manager = new(typeof(TenantsResources));
        Dictionary<string, (string English, string French)> expected = new(StringComparer.Ordinal)
        {
            ["Tenants.Copy.Feedback.Copied"] = ("Copied.", "Copié."),
            ["Tenants.Copy.Feedback.Canceled"] = (
                "Copy could not be completed. Select the value and copy it manually.",
                "La copie n'a pas pu être effectuée. Sélectionnez la valeur et copiez-la manuellement."),
            ["Tenants.Copy.Feedback.Disconnected"] = (
                "Clipboard disconnected. Copy was not completed. Select the value and copy it manually.",
                "Presse-papiers déconnecté. La copie n'a pas été effectuée. Sélectionnez la valeur et copiez-la manuellement."),
            ["Tenants.Copy.Feedback.Failed"] = (
                "Copy failed. Select the value and copy it manually.",
                "La copie a échoué. Sélectionnez la valeur et copiez-la manuellement."),
            ["Tenants.Copy.Feedback.Insecure"] = (
                "Clipboard is unavailable in this browser context. Select the value and copy it manually.",
                "Le presse-papiers est indisponible dans ce contexte de navigateur. Sélectionnez la valeur et copiez-la manuellement."),
            ["Tenants.Copy.Feedback.PermissionDenied"] = (
                "Clipboard permission was not granted. Select the value and copy it manually.",
                "L'autorisation du presse-papiers n'a pas été accordée. Sélectionnez la valeur et copiez-la manuellement."),
            ["Tenants.Copy.Feedback.Unavailable"] = (
                "Clipboard unavailable. Select the value and copy it manually.",
                "Presse-papiers indisponible. Sélectionnez la valeur et copiez-la manuellement."),
        };

        foreach ((string key, (string english, string french)) in expected)
        {
            manager.GetString(key, CultureInfo.InvariantCulture).ShouldBe(english);
            manager.GetString(key, CultureInfo.GetCultureInfo("fr")).ShouldBe(french);
        }
    }

    [Fact]
    public void Every_shared_copy_consumer_declares_explicit_approval_and_configuration_fails_closed()
    {
        string componentsRoot = Path.Combine(ProjectRoot(), "src", "Hexalith.Tenants.UI", "Components");
        string[] razorFiles = Directory.GetFiles(componentsRoot, "*.razor", SearchOption.AllDirectories);
        List<string> implicitConsumers = [];
        int consumerCount = 0;
        int inspectedConsumerCount = 0;

        foreach (string file in razorFiles)
        {
            string source = File.ReadAllText(file);
            consumerCount += Regex.Matches(
                source,
                "<SupportSafeCopyButton\\b",
                RegexOptions.CultureInvariant).Count;
            foreach (Match match in Regex.Matches(
                source,
                "<SupportSafeCopyButton\\b(?<attributes>[^>]*)>",
                RegexOptions.CultureInvariant | RegexOptions.Singleline))
            {
                inspectedConsumerCount++;
                if (!match.Groups["attributes"].Value.Contains("IsApproved=\"true\"", StringComparison.Ordinal))
                {
                    implicitConsumers.Add(Path.GetRelativePath(componentsRoot, file));
                }
            }
        }

        implicitConsumers.ShouldBeEmpty(
            $"Every shared copy control must receive explicit outer-surface approval: {string.Join(", ", implicitConsumers)}");
        inspectedConsumerCount.ShouldBe(consumerCount, "Every opening copy-control tag must be inspected, including paired tags.");

        string configuration = File.ReadAllText(Path.Combine(
            componentsRoot,
            "Tenants",
            "TenantConfigurationView.razor"));
        configuration.ShouldNotContain("<SupportSafeCopyButton");
        configuration.ShouldNotContain("tenants-config-copy-reference");
    }

    [Fact]
    public void Story_1_8_evidence_tracks_closed_safe_model_separately_from_open_external_blockers()
    {
        string report = File.ReadAllText(Path.Combine(
            ProjectRoot(),
            "_bmad-output",
            "implementation-artifacts",
            "story-1-8-support-safe-identifier-copy-and-read-experience-evidence-2026-07-21.md"));

        string configurationRecord = EvidenceRecord(report, "CFG-1.6-SAFE-MODEL");
        configurationRecord.ShouldContain("owner:");
        configurationRecord.ShouldContain("CLOSED 2026-07-22");
        configurationRecord.ShouldContain("clipboard activation and certification remain intentionally absent");

        foreach (string blocker in new[] { "BROWSER-COPY-1.8", "AT-NVDA-1.8" })
        {
            string blockerRecord = EvidenceRecord(report, blocker);
            blockerRecord.ShouldContain("owner:");
            blockerRecord.ShouldContain("Consequence:");
            blockerRecord.ShouldContain("Reopen trigger:");
        }
    }

    private static string EvidenceRecord(string report, string recordId)
    {
        int recordStart = report.IndexOf(recordId, StringComparison.Ordinal);
        recordStart.ShouldBeGreaterThanOrEqualTo(0);
        string record = report[recordStart..];
        int nextRecord = record.IndexOf("\n- **", 1, StringComparison.Ordinal);
        return nextRecord >= 0 ? record[..nextRecord] : record;
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
    public void All_english_and_french_resources_have_key_parity()
    {
        string resourceRoot = Path.Combine(ProjectRoot(), "src", "Hexalith.Tenants.UI", "Resources");
        HashSet<string> englishKeys = ReadAllResourceKeys(Path.Combine(resourceRoot, "TenantsResources.resx"));
        HashSet<string> frenchKeys = ReadAllResourceKeys(Path.Combine(resourceRoot, "TenantsResources.fr.resx"));

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

        // The shell title is configured through FcShellOptions so deployments can override it
        // without changing layout markup.
        layout.ShouldContain("<FrontComposerShell>@Body</FrontComposerShell>");
        layout.ShouldNotContain("AppTitle=");
        layout.ShouldContain("@Body");

        using JsonDocument settings = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(ProjectRoot(), "src", "Hexalith.Tenants.UI", "appsettings.json")));
        JsonElement shellSettings = settings.RootElement.GetProperty("Hexalith").GetProperty("Shell");
        shellSettings.GetProperty("AppTitle").GetString().ShouldBe("Hexalith Tenants");

        // Sign in / out is now the framework header's FcAccountMenu (avatar) — the bespoke
        // content-area auth bar has been removed.
        layout.ShouldNotContain("tenants-auth-bar");
        // The left navigation is framework-owned now — no bespoke navigation slot/component.
        layout.ShouldNotContain("<Navigation>");
        layout.ShouldNotContain("OperationsShellNavigation");
    }

    [Fact]
    public void Document_language_uses_the_active_request_culture()
    {
        string app = File.ReadAllText(
            Path.Combine(ProjectRoot(), "src", "Hexalith.Tenants.UI", "Components", "App.razor"));

        // Document language is driven by the active request culture (not a hardcoded value)...
        app.ShouldContain("CultureInfo.CurrentUICulture.TwoLetterISOLanguageName");
        // ...but clamped to a supported tag so the invariant culture cannot emit an invalid lang="iv".
        app.ShouldContain("? \"fr\" : \"en\"");
        app.ShouldNotContain("<html lang=\"en\">");
        app.ShouldNotContain("lang=\"iv\"");
    }

    [Fact]
    public void Release_workflow_does_not_claim_an_unsupported_tenants_ui_container_handoff()
    {
        string workflow = File.ReadAllText(Path.Combine(ProjectRoot(), ".github/workflows/release.yml"));

        workflow.ShouldNotContain("src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj|tenants-ui");
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

    /// <summary>The typed-client name used by <see cref="MemoriesClient"/> HTTP factory options.</summary>
    private const string MemoriesHttpClientName = nameof(MemoriesClient);

    /// <summary>A sibling client that keeps default logging so the capture is proven able to observe it.</summary>
    private const string ControlHttpClientName = "tenants-ui-tests-control-client";

    /// <summary>
    /// A URL whose query string carries exactly the raw search material that must never be logged. The
    /// framework already redacts query values in its default request logs, so the channel this closes is the
    /// remaining per-request record (path, timing, outcome) for the Memories client.
    /// </summary>
    private const string RawSearchUrl = "https://memories.invalid/v1/search?query=needle&offset=40";

    private static void AddStubPrimaryHandler(IServiceCollection services, string clientName)
        => services.AddHttpClient(clientName)
            .ConfigurePrimaryHttpMessageHandler(static () => new StubPrimaryHandler());

    private static async Task AssertMemoriesHttpLoggingIsSuppressedAsync(
        IServiceProvider provider,
        CapturingLoggerProvider capture)
    {
        IHttpClientFactory factory = provider.GetRequiredService<IHttpClientFactory>();

        // Control: the same capture does observe default HttpClient request logging for a sibling client
        // that keeps its loggers. Without this the suppression assertion below could never fail.
        using (HttpClient control = factory.CreateClient(ControlHttpClientName))
        {
            using HttpResponseMessage controlResponse = await control.GetAsync(new Uri(RawSearchUrl));
            controlResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        capture.Messages.ShouldContain(static message => message.Contains("/v1/search", StringComparison.Ordinal));
        capture.Clear();

        using HttpClient memories = factory.CreateClient(MemoriesHttpClientName);
        using HttpResponseMessage response = await memories.GetAsync(new Uri(RawSearchUrl));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        capture.Messages.ShouldBeEmpty();
    }

    private sealed class StubPrimaryHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        private readonly System.Collections.Concurrent.ConcurrentQueue<string> _messages = new();

        public IReadOnlyCollection<string> Messages => [.. _messages];

        public void Clear() => _messages.Clear();

        public ILogger CreateLogger(string categoryName)
            => categoryName.StartsWith("System.Net.Http.HttpClient.", StringComparison.Ordinal)
                ? new CapturingLogger(_messages, categoryName)
                : NullLogger.Instance;

        public void Dispose()
        {
        }

        private sealed class CapturingLogger(
            System.Collections.Concurrent.ConcurrentQueue<string> messages,
            string categoryName) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull
                => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                ArgumentNullException.ThrowIfNull(formatter);
                messages.Enqueue($"{categoryName}: {formatter(state, exception)}");
            }
        }
    }

    private static string ProjectRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static async Task<string[]> ReadResolvedDependencyValuesAsync(
        string projectPath,
        bool useProjectReferences)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = ProjectRoot(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("restore");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("--force-evaluate");
        startInfo.ArgumentList.Add("-nodeReuse:false");

        // NuGet's assets file is the resolved project/package closure. It includes transitive packages
        // (including analyzers when they actually flow to the UI) and transitive project libraries,
        // without compiling shared outputs or relying on direct evaluation-only items.
        startInfo.ArgumentList.Add($"-property:UseHexalithProjectReferences={useProjectReferences.ToString().ToLowerInvariant()}");
        startInfo.ArgumentList.Add($"-property:Configuration={(useProjectReferences ? "Debug" : "Release")}");
        startInfo.ArgumentList.Add("-property:HexalithMemoriesFromSource=false");
        startInfo.ArgumentList.Add("-property:HexalithCommonsFromSource=false");

        Process? started;
        try
        {
            started = Process.Start(startInfo);
        }
        catch (Win32Exception ex)
        {
            throw new InvalidOperationException(
                "Could not launch 'dotnet' for UI dependency resolution. The .NET SDK must be on PATH "
                + "for this test to have any authority; a missing SDK must fail loudly rather than "
                + "silently skip the generated-REST governance assertions.",
                ex);
        }

        using Process process = started
            ?? throw new InvalidOperationException("Could not start dotnet restore for UI dependency resolution.");
        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync();
        Task<string> standardError = process.StandardError.ReadToEndAsync();

        using var timeout = new CancellationTokenSource(DependencyResolutionTimeout);
        try
        {
            await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                // The process exited between the timeout firing and the kill request.
            }

            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            _ = await standardOutput.ConfigureAwait(false);
            _ = await standardError.ConfigureAwait(false);

            throw new TimeoutException(
                $"dotnet restore dependency resolution for '{projectPath}' did not exit within "
                + $"{DependencyResolutionTimeout.TotalSeconds:F0}s and was terminated.");
        }

        string output = await standardOutput.ConfigureAwait(false);
        string error = await standardError.ConfigureAwait(false);

        process.ExitCode.ShouldBe(0, $"dotnet restore dependency resolution failed: {error}{Environment.NewLine}{output}");

        string assetsPath = Path.Combine(
            Path.GetDirectoryName(projectPath)
                ?? throw new InvalidOperationException($"Project path '{projectPath}' has no parent directory."),
            "obj",
            "project.assets.json");
        File.Exists(assetsPath).ShouldBeTrue(
            $"dotnet restore succeeded but did not produce the expected assets closure at '{assetsPath}'.");

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(await File.ReadAllTextAsync(assetsPath).ConfigureAwait(false));
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"NuGet assets file '{assetsPath}' is not valid JSON, so the UI dependency closure could not be inspected.",
                ex);
        }

        using (document)
        {
            return ReadDependencyValues(document);
        }
    }

    private static string[] ReadDependencyValues(JsonDocument document)
    {
        if (!document.RootElement.TryGetProperty("libraries", out JsonElement libraries)
            || libraries.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                "NuGet assets output contained no 'libraries' object. The resolution contract changed, "
                + "and every dependency assertion built on it would pass vacuously.");
        }

        var values = new List<string>();
        foreach (JsonProperty library in libraries.EnumerateObject())
        {
            int versionSeparator = library.Name.IndexOf('/', StringComparison.Ordinal);
            values.Add(versionSeparator < 0 ? library.Name : library.Name[..versionSeparator]);
        }

        return values.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static bool MatchesDependencyIdentity(string value, string expectedIdentity)
    {
        string normalized = value.Replace('\\', '/').Trim();
        return string.Equals(normalized, expectedIdentity, StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith($"{expectedIdentity},", StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith($"/{expectedIdentity}", StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith($"/{expectedIdentity}.csproj", StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith($"/{expectedIdentity}.dll", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTenantManagementApiRoute(string? routePattern)
    {
        string[] segments = (routePattern ?? string.Empty)
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        for (int index = 0; index < segments.Length; index++)
        {
            if (!string.Equals(segments[index], "api", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            int resourceIndex = index + 1;
            if (resourceIndex < segments.Length && IsApiVersionSegment(segments[resourceIndex]))
            {
                resourceIndex++;
            }

            if (resourceIndex < segments.Length
                && segments[resourceIndex] is string resource
                && (resource.Equals("tenants", StringComparison.OrdinalIgnoreCase)
                    || resource.Equals("users", StringComparison.OrdinalIgnoreCase)
                    || resource.Equals("global-administrators", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsApiVersionSegment(string segment)
        => segment.Length > 1
            && (segment[0] is 'v' or 'V')
            && segment[1..].All(char.IsDigit);

    private static HashSet<string> ReadResourceKeys(string path, string[] prefixes)
        => XDocument
            .Load(path)
            .Descendants("data")
            .Select(static element => element.Attribute("name")?.Value)
            .Where(name => name is not null && prefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
            .Select(name => name!)
            .ToHashSet(StringComparer.Ordinal);

    private static HashSet<string> ReadAllResourceKeys(string path)
        => XDocument
            .Load(path)
            .Descendants("data")
            .Select(static element => element.Attribute("name")?.Value)
            .Where(static name => name is not null)
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

    /// <summary>
    /// Locates the shipped Tenants UI project directory by walking up from the test binary to the repository
    /// layout. Used by the component source scan, which reflection over declared dependencies cannot replace.
    /// </summary>
    private static string UiProjectRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null
            && !File.Exists(Path.Combine(directory.FullName, "Hexalith.Tenants.slnx")))
        {
            directory = directory.Parent;
        }

        string root = Path.Combine(
            directory.ShouldNotBeNull("The repository root must be discoverable for the component source scan.").FullName,
            "src",
            "Hexalith.Tenants.UI");
        Directory.Exists(root).ShouldBeTrue($"The Tenants UI project source must be discoverable at {root}.");
        return root;
    }
}
