using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Shell.Components.Icons;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.UI.Composition;
using Hexalith.Tenants.UI.Extensions;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
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

    [Fact]
    public async Task TenantsUiProject_DoesNotHostGeneratedRestApiOrReferenceExternalApiHost()
    {
        string uiRoot = Path.Combine(ProjectRoot(), "src", "Hexalith.Tenants.UI");
        string uiProjectPath = Path.Combine(uiRoot, "Hexalith.Tenants.UI.csproj");
        List<string> dependencies = [];
        dependencies.AddRange(await ReadEvaluatedDependencyValuesAsync(uiProjectPath, useProjectReferences: true));
        dependencies.AddRange(await ReadEvaluatedDependencyValuesAsync(uiProjectPath, useProjectReferences: false));

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

    [Fact]
    public void TenantsUiAssembly_DoesNotContainMvcControllers()
    {
        Type[] controllers = typeof(TenantsUiServiceCollectionExtensions)
            .Assembly
            .GetTypes()
            .Where(static type => !type.IsAbstract
                && (typeof(Controller).IsAssignableFrom(type)
                    || typeof(ControllerBase).IsAssignableFrom(type)
                    || type.Name.EndsWith("Controller", StringComparison.Ordinal)))
            .ToArray();

        controllers.ShouldBeEmpty("Interactive Tenants UI must not compile generated or hand-written MVC controllers.");
    }

    [Fact]
    public async Task TenantsUiHost_DoesNotExposeTenantManagementApiEndpoints()
    {
        await using var factory = new WebApplicationFactory<global::Program>();
        Endpoint[] endpoints = factory.Services
            .GetServices<EndpointDataSource>()
            .SelectMany(static source => source.Endpoints)
            .ToArray();

        string[] forbiddenEndpoints = endpoints
            .Where(static endpoint => endpoint.Metadata.GetMetadata<ControllerActionDescriptor>() is not null
                || endpoint is RouteEndpoint route && IsTenantManagementApiRoute(route.RoutePattern.RawText))
            .Select(static endpoint => endpoint.DisplayName ?? "<unnamed endpoint>")
            .ToArray();

        forbiddenEndpoints.ShouldBeEmpty(
            "Interactive Tenants UI must not expose MVC controllers or tenant-management API routes.");
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
                "Copy was canceled. Select the value and copy it manually.",
                "La copie a été annulée. Sélectionnez la valeur et copiez-la manuellement."),
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
    public void Story_1_8_evidence_dependencies_record_owner_consequence_and_reopen_trigger()
    {
        string report = File.ReadAllText(Path.Combine(
            ProjectRoot(),
            "_bmad-output",
            "implementation-artifacts",
            "story-1-8-support-safe-identifier-copy-and-read-experience-evidence-2026-07-21.md"));

        foreach (string blocker in new[] { "CFG-1.6-SAFE-MODEL", "BROWSER-COPY-1.8", "AT-NVDA-1.8" })
        {
            int blockerStart = report.IndexOf(blocker, StringComparison.Ordinal);
            blockerStart.ShouldBeGreaterThanOrEqualTo(0);
            string blockerRecord = report[blockerStart..];
            int nextRecord = blockerRecord.IndexOf("\n- **", 1, StringComparison.Ordinal);
            if (nextRecord >= 0)
            {
                blockerRecord = blockerRecord[..nextRecord];
            }

            blockerRecord.ShouldContain("owner:");
            blockerRecord.ShouldContain("Consequence:");
            blockerRecord.ShouldContain("Reopen trigger:");
        }
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

    private static string ProjectRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static async Task<string[]> ReadEvaluatedDependencyValuesAsync(
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
        startInfo.ArgumentList.Add("msbuild");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("-nologo");
        startInfo.ArgumentList.Add("-verbosity:quiet");
        startInfo.ArgumentList.Add("-getItem:ProjectReference,PackageReference,Reference,Analyzer");
        startInfo.ArgumentList.Add($"-property:UseHexalithProjectReferences={useProjectReferences.ToString().ToLowerInvariant()}");

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start dotnet msbuild for UI dependency evaluation.");
        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync();
        Task<string> standardError = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync().ConfigureAwait(false);
        string output = await standardOutput.ConfigureAwait(false);
        string error = await standardError.ConfigureAwait(false);

        process.ExitCode.ShouldBe(0, $"dotnet msbuild dependency evaluation failed: {error}");

        using JsonDocument document = JsonDocument.Parse(output);
        var values = new List<string>();
        foreach (JsonProperty itemType in document.RootElement.GetProperty("Items").EnumerateObject())
        {
            foreach (JsonElement item in itemType.Value.EnumerateArray())
            {
                foreach (string propertyName in new[] { "Identity", "FullPath", "HintPath", "NuGetPackageId", "Filename" })
                {
                    if (item.TryGetProperty(propertyName, out JsonElement value)
                        && value.ValueKind == JsonValueKind.String
                        && !string.IsNullOrWhiteSpace(value.GetString()))
                    {
                        values.Add(value.GetString()!);
                    }
                }
            }
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
        string normalized = routePattern?.Trim().TrimStart('/') ?? string.Empty;
        return normalized.Equals("api/tenants", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("api/tenants/", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("api/users", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("api/users/", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("api/global-administrators", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("api/global-administrators/", StringComparison.OrdinalIgnoreCase);
    }

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
}
