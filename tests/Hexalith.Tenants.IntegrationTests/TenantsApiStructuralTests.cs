extern alias TenantsApi;

using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Rest;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

public sealed class TenantsApiStructuralTests
{
    private static readonly Regex MinimalEndpointMappingPattern = new(
        @"\.\s*Map(?:Get|Post|Put|Delete|Patch|Group|Methods|Fallback|FallbackToFile)\s*\(",
        RegexOptions.Compiled);

    [Fact]
    public async Task TenantsApiProject_UsesContractsClientServiceDefaultsAndGeneratorAnalyzerOnly()
    {
        XDocument project = XDocument.Load(TenantsApiProjectPath());
        XElement[] projectReferences = project
            .Descendants()
            .Where(static element => string.Equals(element.Name.LocalName, "ProjectReference", StringComparison.Ordinal))
            .ToArray();
        XElement[] packageReferences = project
            .Descendants()
            .Where(static element => string.Equals(element.Name.LocalName, "PackageReference", StringComparison.Ordinal))
            .ToArray();

        ProjectReferenceFileNames(projectReferences).ShouldBe(
            [
                "Hexalith.Tenants.Contracts.csproj",
                "Hexalith.EventStore.Client.csproj",
                "Hexalith.EventStore.RestApi.Generators.csproj",
                "Hexalith.EventStore.ServiceDefaults.csproj",
            ],
            ignoreOrder: true);

        XElement generatorReference = projectReferences.Single(static reference =>
            ((string?)reference.Attribute("Include"))?.Replace('\\', '/').EndsWith(
                "src/Hexalith.EventStore.RestApi.Generators/Hexalith.EventStore.RestApi.Generators.csproj",
                StringComparison.Ordinal) == true);
        ((string?)generatorReference.Attribute("OutputItemType")).ShouldBe("Analyzer");
        ((string?)generatorReference.Attribute("ReferenceOutputAssembly")).ShouldBe("false");
        ((string?)generatorReference.Attribute("Condition")).ShouldBe("'$(HexalithEventStoreFromSource)' == 'true'");

        XElement generatorPackage = packageReferences.Single(static reference =>
            string.Equals((string?)reference.Attribute("Include"), "Hexalith.EventStore.RestApi.Generators", StringComparison.Ordinal));
        ((string?)generatorPackage.Attribute("PrivateAssets")).ShouldBe("all");
        ((string?)generatorPackage.Attribute("Condition")).ShouldBe("'$(HexalithEventStoreFromSource)' != 'true'");

        List<string> dependencies = [];
        dependencies.AddRange(await ReadEvaluatedDependencyValuesAsync(TenantsApiProjectPath(), useProjectReferences: true));
        dependencies.AddRange(await ReadEvaluatedDependencyValuesAsync(TenantsApiProjectPath(), useProjectReferences: false));

        dependencies.Where(static dependency => MatchesDependencyIdentity(dependency, "Hexalith.Tenants"))
            .ShouldBeEmpty("The external Tenants API host must not reference the domain implementation.");
        dependencies.Where(static dependency => MatchesDependencyIdentity(dependency, "Hexalith.Tenants.UI"))
            .ShouldBeEmpty("The external Tenants API host must not reference the interactive UI host.");
    }

    [Fact]
    public void TenantsApiAssembly_OptsIntoSystemTenantRestApiScope()
    {
        RestApiAttribute attribute = typeof(TenantsApi::Program).Assembly
            .GetCustomAttributes<RestApiAttribute>()
            .Single();

        attribute.RoutePrefix.ShouldBe("api/tenants");
        attribute.Tag.ShouldBe("tenants");
        attribute.TenantSource.ShouldBe(RestTenantSource.System);
    }

    [Fact]
    public void TenantsApiSource_UsesControllersGatewayHandlersAndNoMinimalEndpoints()
    {
        string apiRoot = TenantsApiRoot();
        string projectText = File.ReadAllText(TenantsApiProjectPath());
        string programText = File.ReadAllText(Path.Combine(apiRoot, "Program.cs"));
        string sourceText = ReadTenantsApiSourceAndProject();

        Directory.EnumerateFiles(apiRoot, "*.razor", SearchOption.AllDirectories)
            .Where(static file => !IsBuildArtifact(file))
            .ShouldBeEmpty("The external API host must not contain Razor UI components.");

        projectText.ShouldNotContain("Microsoft.FluentUI.AspNetCore.Components");
        projectText.ShouldNotContain("Microsoft.AspNetCore.Components.Web");
        projectText.ShouldNotContain("Hexalith.Tenants.UI");
        projectText.ShouldNotContain("Hexalith.Tenants.csproj");

        programText.ShouldContain("builder.Services.AddControllers();");
        programText.ShouldContain("builder.Services.AddHttpContextAccessor();");
        programText.ShouldContain("builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)");
        programText.ShouldContain("builder.Services.AddAuthorization();");
        programText.ShouldContain("DaprHttpEndpointResolver.Resolve(builder.Configuration)");
        programText.ShouldContain("options.BaseAddress = new Uri(daprHttpEndpoint)");
        programText.ShouldContain(".AddHttpMessageHandler<InboundBearerForwardingHandler>()");
        programText.ShouldContain(".AddEventStoreDaprServiceInvocation(\"eventstore\", daprApiToken)");
        sourceText.ShouldNotContain("DaprAppIdHandler");
        sourceText.ShouldNotContain("TryAddWithoutValidation(\"dapr-app-id\"");
        sourceText.ShouldNotContain("TryAddWithoutValidation(\"dapr-api-token\"");
        programText.ShouldContain("Encoding.UTF8.GetByteCount(signingKey) < 32");
        programText.ShouldContain("app.UseAuthentication();");
        programText.ShouldContain("app.UseAuthorization();");
        programText.ShouldContain("app.MapControllers();");

        programText.IndexOf("app.UseAuthentication();", StringComparison.Ordinal)
            .ShouldBeLessThan(programText.IndexOf("app.UseAuthorization();", StringComparison.Ordinal));
        programText.IndexOf("app.UseAuthorization();", StringComparison.Ordinal)
            .ShouldBeLessThan(programText.IndexOf("app.MapControllers();", StringComparison.Ordinal));

        MinimalEndpointMappingPattern.IsMatch(sourceText)
            .ShouldBeFalse("Tenants.Api must expose typed generated controllers only, not hand-written minimal API endpoints.");
    }

    [Fact]
    public void TenantsApiHost_ExposesOnlyGeneratedControllersAndPlatformHealthEndpoints()
    {
        using WebApplicationFactory<TenantsApi::Program> factory = new WebApplicationFactory<TenantsApi::Program>()
            .WithWebHostBuilder(builder =>
            {
                _ = builder.UseSetting("EventStore:Authentication:Issuer", "hexalith-dev");
                _ = builder.UseSetting("EventStore:Authentication:Audience", "hexalith-eventstore");
                _ = builder.UseSetting(
                    "EventStore:Authentication:SigningKey",
                    "this-is-a-structural-test-signing-key-minimum-32-chars");
            });

        RouteEndpoint[] nonControllerRoutes = factory.Services
            .GetServices<EndpointDataSource>()
            .SelectMany(static source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(static endpoint => endpoint.Metadata.GetMetadata<ControllerActionDescriptor>() is null)
            .ToArray();

        nonControllerRoutes
            .Select(static endpoint => endpoint.RoutePattern.RawText?.Trim().TrimStart('/') ?? string.Empty)
            .ShouldBe(["alive", "health", "ready"], ignoreOrder: true);
    }

    [Fact]
    public void TenantsRestController_IsOnlyGeneratedControllerAndUsesGatewayBoundary()
    {
        Type[] controllers = typeof(TenantsApi::Program).Assembly
            .GetTypes()
            .Where(static type => typeof(ControllerBase).IsAssignableFrom(type)
                || type.GetCustomAttribute<ApiControllerAttribute>() is not null)
            .ToArray();

        controllers.Select(static type => type.FullName).ShouldBe(
            ["Hexalith.Tenants.Api.Generated.TenantsRestController"],
            ignoreOrder: true);

        Type controller = typeof(TenantsApi::Program).Assembly.GetType(
            "Hexalith.Tenants.Api.Generated.TenantsRestController",
            throwOnError: true)!;

        controller.GetCustomAttribute<ApiControllerAttribute>().ShouldNotBeNull();
        controller.GetCustomAttribute<AuthorizeAttribute>().ShouldNotBeNull();
        controller.GetCustomAttribute<RouteAttribute>().ShouldNotBeNull().Template.ShouldBe("api/tenants");

        ConstructorInfo constructor = controller.GetConstructors().Single();
        constructor.GetParameters()
            .Select(static parameter => parameter.ParameterType)
            .ShouldBe(
                [
                    typeof(IEventStoreGatewayClient),
                    typeof(ICommandStatusLocationBuilder),
                ]);

        AssertAction(controller, "ListTenantsQueryQueryAsync", typeof(HttpGetAttribute), "");
        AssertAction(controller, "GetTenantQueryQueryAsync", typeof(HttpGetAttribute), "{tenantId}");
        AssertAction(controller, "GetTenantUsersQueryQueryAsync", typeof(HttpGetAttribute), "{tenantId}/users");
        AssertAction(controller, "GetUserTenantsQueryQueryAsync", typeof(HttpGetAttribute), "~/api/users/{userId}/tenants");
        AssertAction(controller, "GetTenantAuditQueryQueryAsync", typeof(HttpGetAttribute), "{tenantId}/audit");
        AssertAction(controller, "GetGlobalAdministratorsQueryQueryAsync", typeof(HttpGetAttribute), "~/api/global-administrators");

        AssertAction(controller, "CreateTenantCommandAsync", typeof(HttpPostAttribute), "{tenantId}");
        AssertAction(controller, "UpdateTenantCommandAsync", typeof(HttpPutAttribute), "{tenantId}");
        AssertAction(controller, "EnableTenantCommandAsync", typeof(HttpPostAttribute), "{tenantId}/enable");
        AssertAction(controller, "DisableTenantCommandAsync", typeof(HttpPostAttribute), "{tenantId}/disable");
        AssertAction(controller, "AddUserToTenantCommandAsync", typeof(HttpPostAttribute), "{tenantId}/users/{userId}/add");
        AssertAction(controller, "RemoveUserFromTenantCommandAsync", typeof(HttpPostAttribute), "{tenantId}/users/{userId}/remove");
        AssertAction(controller, "ChangeUserRoleCommandAsync", typeof(HttpPatchAttribute), "{tenantId}/users/{userId}/role");
        AssertAction(controller, "SetTenantConfigurationCommandAsync", typeof(HttpPutAttribute), "{tenantId}/configuration/{key}");
        AssertAction(controller, "RemoveTenantConfigurationCommandAsync", typeof(HttpPostAttribute), "{tenantId}/configuration/{key}/remove");
        AssertAction(controller, "SetGlobalAdministratorCommandAsync", typeof(HttpPostAttribute), "~/api/global-administrators/{userId}/set");
        AssertAction(controller, "RemoveGlobalAdministratorCommandAsync", typeof(HttpPostAttribute), "~/api/global-administrators/{userId}/remove");

        controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(static method => method.GetCustomAttributes<HttpMethodAttribute>().Any())
            .Select(static method => method.Name)
            .ShouldBe(
                [
                    "AddUserToTenantCommandAsync",
                    "ChangeUserRoleCommandAsync",
                    "CreateTenantCommandAsync",
                    "DisableTenantCommandAsync",
                    "EnableTenantCommandAsync",
                    "GetGlobalAdministratorsQueryQueryAsync",
                    "GetTenantAuditQueryQueryAsync",
                    "GetTenantQueryQueryAsync",
                    "GetTenantUsersQueryQueryAsync",
                    "GetUserTenantsQueryQueryAsync",
                    "ListTenantsQueryQueryAsync",
                    "RemoveGlobalAdministratorCommandAsync",
                    "RemoveTenantConfigurationCommandAsync",
                    "RemoveUserFromTenantCommandAsync",
                    "SetGlobalAdministratorCommandAsync",
                    "SetTenantConfigurationCommandAsync",
                    "UpdateTenantCommandAsync",
                ],
                ignoreOrder: true);
    }

    private static void AssertAction(Type controller, string methodName, Type attributeType, string routeTemplate)
    {
        MethodInfo method = controller.GetMethod(methodName)
            ?? throw new MissingMethodException(controller.FullName, methodName);

        HttpMethodAttribute attribute = method.GetCustomAttributes<HttpMethodAttribute>()
            .Single(actionAttribute => actionAttribute.GetType() == attributeType);
        attribute.Template.ShouldBe(routeTemplate);
        method.GetCustomAttribute<AuthorizeAttribute>().ShouldBeNull("Authorization is applied at generated controller level.");
    }

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
            ?? throw new InvalidOperationException("Could not start dotnet msbuild for Tenants API dependency evaluation.");
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

    private static bool IsBuildArtifact(string path)
        => path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal);

    private static string[] ProjectReferenceFileNames(IEnumerable<XElement> references)
        => references
            .Select(static reference => Path.GetFileName(((string?)reference.Attribute("Include"))?.Replace('\\', '/') ?? string.Empty))
            .ToArray();

    private static string ProjectRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Hexalith.Tenants.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not find Hexalith.Tenants.slnx from the test output path.");
    }

    private static string ReadTenantsApiSourceAndProject()
        => string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(TenantsApiRoot(), "*.*", SearchOption.AllDirectories)
                .Where(static file => file.EndsWith(".cs", StringComparison.Ordinal)
                    || file.EndsWith(".csproj", StringComparison.Ordinal))
                .Where(static file => !IsBuildArtifact(file))
                .Select(File.ReadAllText));

    private static string TenantsApiProjectPath()
        => Path.Combine(TenantsApiRoot(), "Hexalith.Tenants.Api.csproj");

    private static string TenantsApiRoot()
        => Path.Combine(ProjectRoot(), "src", "Hexalith.Tenants.Api");
}
