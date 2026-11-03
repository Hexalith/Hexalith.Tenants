using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

using CommunityToolkit.Aspire.Hosting.Dapr;

using Shouldly;

using YamlDotNet.RepresentationModel;

namespace Hexalith.Tenants.AppHost.Tests;

/// <summary>Checks security and read endpoint wiring in the AppHost model without starting services.</summary>
public sealed class AppHostSecurityResourceGraphTests
{
    [Fact]
    public async Task SourceCatalogProfileOmitsTheOptionalSampleDomainModule()
    {
        await using IDistributedApplicationTestingBuilder builder = await AppHostModelBuilder
            .CreateAsync(
                ["--publisher", "manifest", "--output-path",
                    Path.Combine(Path.GetTempPath(), $"tenants-source-model-{Guid.NewGuid():N}.json"),
                    "--EnableKeycloak=true", "--EnableMemories=false", "--TENANTS_SOURCE_CATALOG_PROFILE=true"]);

        builder.Resources.OfType<ProjectResource>().Select(resource => resource.Name).ShouldContain("eventstore");
        builder.Resources.OfType<ProjectResource>().Select(resource => resource.Name).ShouldContain("eventstore-admin");
        builder.Resources.OfType<ProjectResource>().Select(resource => resource.Name).ShouldContain("tenants");
        builder.Resources.OfType<ProjectResource>().Select(resource => resource.Name).ShouldContain("tenants-api");
        builder.Resources.OfType<ProjectResource>().Select(resource => resource.Name).ShouldNotContain("sample");
    }

    [Fact]
    public async Task SourceCatalogProfileCanIncludeTheRealSampleConsumerForBackgroundDelivery()
    {
        await using IDistributedApplicationTestingBuilder builder = await AppHostModelBuilder
            .CreateAsync(
                ["--publisher", "manifest", "--output-path",
                    Path.Combine(Path.GetTempPath(), $"tenants-sample-model-{Guid.NewGuid():N}.json"),
                    "--EnableKeycloak=true", "--EnableMemories=false", "--TENANTS_SOURCE_CATALOG_PROFILE=true",
                    "--TENANTS_SOURCE_CATALOG_INCLUDE_SAMPLE=true"]);

        ProjectResource sample = builder.Resources.OfType<ProjectResource>()
            .Single(static resource => resource.Name == "sample");
        IDaprSidecarResource sampleSidecar = sample.Annotations.OfType<DaprSidecarAnnotation>()
            .ShouldHaveSingleItem().Sidecar;
        DaprSidecarOptions sidecar = sampleSidecar.Annotations.OfType<DaprSidecarOptionsAnnotation>()
            .ShouldHaveSingleItem().Options;
        sidecar.AppId.ShouldBe("sample");
        sampleSidecar.Annotations.OfType<DaprComponentReferenceAnnotation>()
            .Select(static reference => reference.Component.Name).Order(StringComparer.Ordinal)
            .ShouldBe(["pubsub", "statestore"]);
        await AssertAppChannelTokensAsync(builder);
    }

    [Fact]
    public async Task DefaultRunProfileDoesNotRequireDevelopmentCatalogCustody()
    {
        await using IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Hexalith_Tenants_AppHost>(
                ["--EnableKeycloak=true", "--EnableMemories=false", "--TENANTS_SOURCE_CATALOG_PROFILE=false"]);

        builder.ExecutionContext.IsRunMode.ShouldBeTrue();
        builder.Resources.ShouldNotContain(resource => resource.Name == "routing-tenants-owner");
        await AssertAppChannelTokensAsync(builder);
    }

    [Theory]
    [InlineData(null, "30")]
    [InlineData("5", "5")]
    [InlineData("45", "45")]
    public async Task GatewayDomainInvocationBudgetAccommodatesNativeAuthorityAndPreservesOperatorOverride(
        string? configuredSeconds, string expectedSeconds)
    {
        // Capture the offline model without enrolling a local source-runtime custody owner.
        List<string> args = ["--publisher", "manifest", "--output-path",
            Path.Combine(Path.GetTempPath(), $"tenants-invocation-model-{Guid.NewGuid():N}.json"),
            "--EnableKeycloak=true", "--EnableMemories=false"];
        if (configuredSeconds is not null)
            args.Add($"--EventStore:DomainServices:InvocationTimeoutSeconds={configuredSeconds}");
        await using IDistributedApplicationTestingBuilder builder = await AppHostModelBuilder
            .CreateAsync([.. args]);
        builder.ExecutionContext.IsPublishMode.ShouldBeTrue();
        ProjectResource gateway = builder.Resources.OfType<ProjectResource>()
            .Single(static resource => resource.Name == "eventstore");

        IReadOnlyDictionary<string, object> environment = await GetEnvironmentAsync(gateway, builder.ExecutionContext);
        string seconds = environment["EventStore__DomainServices__InvocationTimeoutSeconds"].ShouldBeOfType<string>();
        seconds.ShouldBe(expectedSeconds);
        if (configuredSeconds is null)
        {
            int.Parse(seconds, System.Globalization.CultureInfo.InvariantCulture).ShouldBeGreaterThan(10 + 2,
                "the gateway must leave room for the bounded native read and independent final observation");
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PublishModeRetainsOneSharedAppChannelTokenPerService(bool enableKeycloak)
    {
        await using IDistributedApplicationTestingBuilder builder = await AppHostModelBuilder
            .CreateAsync(
                ["--publisher", "manifest", "--output-path", Path.Combine(Path.GetTempPath(), $"tenants-model-{Guid.NewGuid():N}.json"),
                    $"--EnableKeycloak={enableKeycloak}", "--Authentication:JwtBearer:SigningKey=RunModelSigningKey-AtLeast32Chars!"]);
        builder.ExecutionContext.IsPublishMode.ShouldBeTrue();
        await AssertAppChannelTokensAsync(builder);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MemoriesIsIncludedByDefaultAndCanBeExplicitlyExcluded(bool includeMemories)
    {
        string[] args = includeMemories ? ["--EnableKeycloak=true"] : ["--EnableKeycloak=true", "--EnableMemories=false"];
        await using IDistributedApplicationTestingBuilder builder = await AppHostModelBuilder
            .CreateAsync(args);
        foreach (string name in new[] { "memories", "memories-vectors", "memories-graphs", "memories-secretstore" })
        {
            builder.Resources.Any(resource => resource.Name == name).ShouldBe(includeMemories);
        }

        IResource ui = builder.Resources.Single(static resource => resource.Name == "tenants-ui");
        IReadOnlyDictionary<string, object> environment = await GetEnvironmentAsync(ui, builder.ExecutionContext);
        environment.ContainsKey("Memories__BaseAddress").ShouldBe(includeMemories);
        environment.Keys.Any(static key => key.StartsWith("services__memories__", StringComparison.OrdinalIgnoreCase)).ShouldBe(includeMemories);
        builder.Resources.Any(static resource => resource.Name == "tenants-api").ShouldBeTrue();
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public async Task UiReadsUseOwnDaprSidecarAndHttpsApiReceiverInBothAuthenticationModes(
        bool enableKeycloak, bool sourceCatalogProfile)
    {
        await using IDistributedApplicationTestingBuilder builder = await AppHostModelBuilder
            .CreateAsync(
                [$"--EnableKeycloak={enableKeycloak}", "--Authentication:JwtBearer:SigningKey=RunModelSigningKey-AtLeast32Chars!",
                    $"--TENANTS_SOURCE_CATALOG_PROFILE={sourceCatalogProfile}"]);
        IResource tenantsUi = builder.Resources.Single(static resource => resource.Name == "tenants-ui");
        ProjectResource tenantsApi = builder.Resources.OfType<ProjectResource>()
            .Single(static resource => resource.Name == "tenants-api");

        DaprSidecarOptions apiSidecar = tenantsApi.Annotations.OfType<DaprSidecarAnnotation>()
            .ShouldHaveSingleItem().Sidecar.Annotations.OfType<DaprSidecarOptionsAnnotation>()
            .ShouldHaveSingleItem().Options;
        apiSidecar.AppId.ShouldBe("tenants-api");
        apiSidecar.AppProtocol.ShouldBe("https");
        Path.GetFileName(apiSidecar.Config).ShouldBe(sourceCatalogProfile
            ? "tenants-api.configuration.json" : "accesscontrol.tenants-api.yaml");
        await AssertReadReceiverPolicyAsync(apiSidecar);
        DaprSidecarOptions uiSidecar = tenantsUi.Annotations.OfType<DaprSidecarAnnotation>()
            .ShouldHaveSingleItem().Sidecar.Annotations.OfType<DaprSidecarOptionsAnnotation>()
            .ShouldHaveSingleItem().Options;
        uiSidecar.AppId.ShouldBe("tenants-ui");
        uiSidecar.AppProtocol.ShouldBe("https");
        Path.GetFileName(uiSidecar.Config).ShouldBe("accesscontrol.tenants-ui.yaml");
        uiSidecar.ResourcesPaths.ShouldNotBeNull().ShouldHaveSingleItem().ShouldEndWith("DaprComponents");
        IReadOnlyDictionary<string, object> environment = await GetEnvironmentAsync(tenantsUi, builder.ExecutionContext);
        environment.ContainsKey("Tenants__BaseAddress").ShouldBeFalse("the dynamically injected DAPR_HTTP_ENDPOINT is authoritative for reads");
    }

    [Theory]
    [InlineData("Dapr:PlacementHostAddress", "external:50005")]
    [InlineData("Dapr:SchedulerHostAddress", "external:50006")]
    [InlineData("Dapr:PlacementHostAddress", "localhost:9999")]
    [InlineData("Dapr:SchedulerHostAddress", "127.0.0.1:9999")]
    [InlineData("Dapr:PlacementHostAddress", "127.0.0.1:50006")]
    [InlineData("Dapr:SchedulerHostAddress", "localhost:50005")]
    [InlineData("Dapr:PlacementHostAddress", "[::1]:50005")]
    [InlineData("Dapr:SchedulerHostAddress", "0.0.0.0:6060")]
    public async Task LocalPrivateControlPlaneRejectsExternalHostsAndCustomPorts(string key, string endpoint)
    {
        InvalidOperationException failure = await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await using IDistributedApplicationTestingBuilder builder = await AppHostModelBuilder
                .CreateAsync(["--EnableKeycloak=true", $"--{key}={endpoint}"]);
        });
        failure.Message.ShouldContain(key);
        failure.Message.ShouldContain("private local Dapr mTLS control plane");
    }

    [Theory]
    [InlineData("localhost", 50005, 50006)]
    [InlineData("localhost", 6050, 6060)]
    [InlineData("127.0.0.1", 50005, 50006)]
    [InlineData("127.0.0.1", 6050, 6060)]
    public async Task PublishedHarnessLoopbackHintsRetainThePrivateAllocatedControlPlane(string host, int placementPort, int schedulerPort)
    {
        // The unchanged published harness appends its standard prerequisite hints after these empties.
        await using IDistributedApplicationTestingBuilder builder = await AppHostModelBuilder
            .CreateAsync(
                ["--EnableKeycloak=true", "--EnableMemories=false",
                    "--Dapr:PlacementHostAddress=", "--Dapr:SchedulerHostAddress=",
                    $"--Dapr:PlacementHostAddress={host}:{placementPort}",
                    $"--Dapr:SchedulerHostAddress={host}:{schedulerPort}"]);
        builder.ExecutionContext.IsRunMode.ShouldBeTrue();
        int privatePlacement = builder.Resources.Single(resource => resource.Name == "dapr-placement-mtls")
            .Annotations.OfType<EndpointAnnotation>().Single(endpoint => endpoint.Name == "grpc").Port!.Value;
        int privateScheduler = builder.Resources.Single(resource => resource.Name == "dapr-scheduler-mtls")
            .Annotations.OfType<EndpointAnnotation>().Single(endpoint => endpoint.Name == "grpc").Port!.Value;
        foreach (ProjectResource application in builder.Resources.OfType<ProjectResource>())
        {
            foreach (DaprSidecarAnnotation sidecar in application.Annotations.OfType<DaprSidecarAnnotation>())
            {
                DaprSidecarOptions options = sidecar.Sidecar.Annotations.OfType<DaprSidecarOptionsAnnotation>().Single().Options;
                options.PlacementHostAddress.ShouldBe("localhost:" + privatePlacement);
                options.SchedulerHostAddress.ShouldBe("localhost:" + privateScheduler);
                options.PlacementHostAddress.ShouldNotBe(host + ":" + placementPort);
                options.SchedulerHostAddress.ShouldNotBe(host + ":" + schedulerPort);
            }
        }
    }

    [Theory]
    [InlineData("llm.memories.yaml")]
    [InlineData("secretstore.memories.yaml")]
    public async Task MemoriesCapabilitiesAreScopedAwayFromBothReadSidecars(string file)
    {
        await using IDistributedApplicationTestingBuilder builder = await AppHostModelBuilder
            .CreateAsync(["--EnableKeycloak=true"]);
        string yaml = await File.ReadAllTextAsync(Path.Combine(builder.AppHostDirectory, "DaprComponents", file));
        string[] sections = yaml.Split("scopes:", StringSplitOptions.None);
        sections.Length.ShouldBe(2);
        string[] scopes = sections[1].Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.TrimStart('-').Trim()).ToArray();
        scopes.ShouldBe(["memories"]);
        foreach (string reader in new[] { "tenants-api", "tenants-ui" })
        {
            IDaprSidecarResource sidecar = builder.Resources.Single(resource => resource.Name == reader).Annotations
                .OfType<DaprSidecarAnnotation>().ShouldHaveSingleItem().Sidecar;
            string? appId = sidecar.Annotations.OfType<DaprSidecarOptionsAnnotation>().ShouldHaveSingleItem().Options.AppId;
            scopes.ShouldNotContain(appId!);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("short-key")]
    [InlineData("1234567890123456789012345678901")]
    public async Task SymmetricModeRejectsMissingOrShortSigningKey(string? signingKey)
    {
        string[] args = signingKey is null
            ? ["--EnableKeycloak=false"]
            : ["--EnableKeycloak=false", $"--Authentication:JwtBearer:SigningKey={signingKey}"];

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await using IDistributedApplicationTestingBuilder builder = await AppHostModelBuilder
                .CreateAsync(args);
        });

        exception.Message.ShouldBe(
            "Authentication:JwtBearer:SigningKey must be configured with at least 32 UTF-8 bytes when EnableKeycloak=false.");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task KeycloakModeSharesWorkloadSecretAndProtectsDomainServiceSidecarChannels(bool sourceCatalogProfile)
    {
        await using IDistributedApplicationTestingBuilder builder = await AppHostModelBuilder
            .CreateAsync(["--EnableKeycloak=true", $"--TENANTS_SOURCE_CATALOG_PROFILE={sourceCatalogProfile}"]);
        ParameterResource secret = builder.Resources.OfType<ParameterResource>()
            .Single(static resource => resource.Name == "eventstore-workload-client-secret");
        IResource keycloak = builder.Resources.Single(static resource => resource.Name == "security");
        IResource eventStore = builder.Resources.Single(static resource => resource.Name == "eventstore");

        IReadOnlyDictionary<string, object> keycloakEnvironment = await GetEnvironmentAsync(keycloak, builder.ExecutionContext);
        IReadOnlyDictionary<string, object> eventStoreEnvironment = await GetEnvironmentAsync(eventStore, builder.ExecutionContext);
        keycloakEnvironment["HEXALITH_EVENTSTORE_WORKLOAD_CLIENT_SECRET"].ShouldBeSameAs(secret);
        eventStoreEnvironment["Authentication__WorkloadIssuer__ClientSecret"].ShouldBeSameAs(secret);

        await AssertAppChannelTokensAsync(builder, sourceCatalogProfile);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SymmetricModeSharesOneSigningKeyAndProtectsDomainServiceSidecarChannels(bool sourceCatalogProfile)
    {
        await using IDistributedApplicationTestingBuilder builder = await AppHostModelBuilder
            .CreateAsync(
                ["--EnableKeycloak=false", "--Authentication:JwtBearer:SigningKey=RunModelSigningKey-AtLeast32Chars!",
                    $"--TENANTS_SOURCE_CATALOG_PROFILE={sourceCatalogProfile}"]);
        ParameterResource key = builder.Resources.OfType<ParameterResource>()
            .Single(static resource => resource.Name == "tenants-local-jwt-signing-key");

        string[] services = sourceCatalogProfile
            ? ["eventstore", "eventstore-admin", "tenants", "tenants-api"]
            : ["eventstore", "eventstore-admin", "tenants", "tenants-api", "sample"];
        foreach (string name in services)
        {
            IResource resource = builder.Resources.Single(candidate => candidate.Name == name);
            IReadOnlyDictionary<string, object> environment = await GetEnvironmentAsync(resource, builder.ExecutionContext);
            environment["Authentication__JwtBearer__SigningKey"].ShouldBeSameAs(key);
            environment["Authentication__JwtBearer__Authority"].ShouldBe(string.Empty);
            environment["Authentication__JwtBearer__Issuer"].ShouldBe("hexalith-dev");
            environment["Authentication__JwtBearer__Audience"].ShouldBe("hexalith-eventstore");
            environment["Authentication__JwtBearer__AllowedAlgorithms__0"].ShouldBe("HS256");
        }

        IResource tenantsApi = builder.Resources.Single(static resource => resource.Name == "tenants-api");
        IReadOnlyDictionary<string, object> apiEnvironment = await GetEnvironmentAsync(tenantsApi, builder.ExecutionContext);
        apiEnvironment["EventStore__Authentication__Authority"].ShouldBe(string.Empty);
        apiEnvironment["EventStore__Authentication__Issuer"].ShouldBe("hexalith-dev");
        apiEnvironment["EventStore__Authentication__Audience"].ShouldBe("hexalith-eventstore");
        apiEnvironment["EventStore__Authentication__SigningKey"].ShouldBeSameAs(key);

        await AssertAppChannelTokensAsync(builder, sourceCatalogProfile);
    }

    private static async Task AssertReadReceiverPolicyAsync(DaprSidecarOptions sidecar)
    {
        using StringReader reader = new(await File.ReadAllTextAsync(sidecar.Config!));
        YamlStream yaml = new();
        yaml.Load(reader);
        YamlNode configuration = yaml.Documents.ShouldHaveSingleItem().RootNode;
        YamlNode accessControl = MappingValue(MappingValue(configuration, "spec"), "accessControl");
        MappingValue(accessControl, "defaultAction").ToString().ShouldBe("deny");
        MappingValue(accessControl, "trustDomain").ToString().ShouldBe("public");
        YamlNode policy = MappingValue(accessControl, "policies").ShouldBeOfType<YamlSequenceNode>()
            .Children.ShouldHaveSingleItem();
        MappingValue(policy, "appId").ToString().ShouldBe("tenants-ui");
        MappingValue(policy, "defaultAction").ToString().ShouldBe("deny");
        MappingValue(policy, "trustDomain").ToString().ShouldBe("public");
        MappingValue(policy, "namespace").ToString().ShouldBe("default");
        YamlNode[] operations = [.. MappingValue(policy, "operations").ShouldBeOfType<YamlSequenceNode>().Children];
        string[] expectedRoutes =
        [
            "/api/tenants", "/api/tenants/*", "/api/tenants/**/users", "/api/tenants/**/audit",
            "/api/users/**/tenants", "/api/global-administrators",
        ];
        operations.Select(operation => MappingValue(operation, "name").ToString()).Order(StringComparer.Ordinal)
            .ShouldBe(expectedRoutes.Order(StringComparer.Ordinal));
        foreach (YamlNode operation in operations)
        {
            MappingValue(operation, "action").ToString().ShouldBe("allow");
            MappingValue(operation, "httpVerb").ShouldBeOfType<YamlSequenceNode>()
                .Children.ShouldHaveSingleItem().ToString().ShouldBe("GET");
        }
    }

    private static YamlNode MappingValue(YamlNode node, string key)
        => node.ShouldBeOfType<YamlMappingNode>().Children[new YamlScalarNode(key)];

    private static async Task AssertAppChannelTokensAsync(
        IDistributedApplicationTestingBuilder builder, bool sourceCatalogProfile = false)
    {
        // These tests evaluate the model without starting its Sentry container. Its private mount
        // receives synthetic nonempty material solely so environment callbacks can be inspected.
        IResource? sentry = builder.Resources.SingleOrDefault(resource => resource.Name == "dapr-sentry");
        if (sentry is not null)
        {
            string directory = sentry.Annotations.OfType<ContainerMountAnnotation>()
                .Single(mount => mount.Target == "/var/run/dapr/credentials").Source!;
            foreach (string name in new[] { "ca.crt", "issuer.crt", "issuer.key" })
                await File.WriteAllTextAsync(Path.Combine(directory, name), "model-only-" + name);
        }

        // The private catalog protects its four technical receivers; the ordinary and published
        // topologies retain their three domain app channels and the optional sample.
        string[] services = sourceCatalogProfile
            ? ["eventstore", "eventstore-admin", "tenants", "tenants-api"]
            : ["eventstore", "tenants", "sample"];
        builder.Resources.Any(resource => resource.Name == "sample").ShouldBe(!sourceCatalogProfile);
        string[] expectedParameters = services.Select(name => name + "-app-api-token").ToArray();
        builder.Resources.OfType<ParameterResource>()
            .Where(static resource => resource.Name.EndsWith("-app-api-token", StringComparison.Ordinal))
            .Select(static resource => resource.Name).Order(StringComparer.Ordinal)
            .ShouldBe(expectedParameters.Order(StringComparer.Ordinal));

        List<ParameterResource> tokens = [];
        List<string> values = [];
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        foreach (string name in services)
        {
            ProjectResource project = builder.Resources.OfType<ProjectResource>()
                .Single(resource => resource.Name == name);
            ParameterResource token = await GetAppChannelTokenAsync(project, builder.ExecutionContext);
            token.Name.ShouldBe(name + "-app-api-token");
            builder.Resources.OfType<ParameterResource>().Where(resource => resource.Name == token.Name)
                .ShouldHaveSingleItem().ShouldBeSameAs(token);
            token.Secret.ShouldBeTrue();
            tokens.ShouldNotContain(token);
            tokens.Add(token);
            string? value = await token.GetValueAsync(timeout.Token);
            value.ShouldNotBeNullOrWhiteSpace();
            values.Add(value);
        }
        values.Distinct(StringComparer.Ordinal).Count().ShouldBe(services.Length);
    }

    private static async Task<ParameterResource> GetAppChannelTokenAsync(
        ProjectResource project,
        DistributedApplicationExecutionContext execution)
    {
        IReadOnlyDictionary<string, object> environment = await GetEnvironmentAsync(project, execution);
        IDaprSidecarResource sidecar = project.Annotations.OfType<DaprSidecarAnnotation>()
            .ShouldHaveSingleItem().Sidecar;
        IReadOnlyDictionary<string, object> sidecarEnvironment = await GetEnvironmentAsync(sidecar, execution);
        ParameterResource token = environment["APP_API_TOKEN"].ShouldBeOfType<ParameterResource>();
        token.ShouldBeSameAs(sidecarEnvironment["APP_API_TOKEN"]);
        return token;
    }

    private static async Task<IReadOnlyDictionary<string, object>> GetEnvironmentAsync(
        IResource resource,
        DistributedApplicationExecutionContext execution,
        CancellationToken cancellationToken = default)
    {
        var context = new EnvironmentCallbackContext(
            execution,
            resource,
            new Dictionary<string, object>(),
            cancellationToken);
        foreach (EnvironmentCallbackAnnotation annotation in resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
        {
            await AppHostModelBuilder.ApplyEnvironmentCallbackAsync(annotation, context).WaitAsync(cancellationToken);
        }

        return context.EnvironmentVariables;
    }
}
