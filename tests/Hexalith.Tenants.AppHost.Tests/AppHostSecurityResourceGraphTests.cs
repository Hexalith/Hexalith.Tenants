using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

using CommunityToolkit.Aspire.Hosting.Dapr;

using Shouldly;

namespace Hexalith.Tenants.AppHost.Tests;

/// <summary>Checks security and read endpoint wiring in the AppHost model without starting services.</summary>
public sealed class AppHostSecurityResourceGraphTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PublishModeRetainsOneSharedAppChannelTokenPerService(bool enableKeycloak)
    {
        await using IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Hexalith_Tenants_AppHost>(
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
        await using IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Hexalith_Tenants_AppHost>(args);
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
    [InlineData(true)]
    [InlineData(false)]
    public async Task UiReadsUseOwnDaprSidecarAndHttpsApiReceiverInBothAuthenticationModes(bool enableKeycloak)
    {
        await using IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Hexalith_Tenants_AppHost>(
                [$"--EnableKeycloak={enableKeycloak}", "--Authentication:JwtBearer:SigningKey=RunModelSigningKey-AtLeast32Chars!"]);
        IResource tenantsUi = builder.Resources.Single(static resource => resource.Name == "tenants-ui");
        ProjectResource tenantsApi = builder.Resources.OfType<ProjectResource>()
            .Single(static resource => resource.Name == "tenants-api");

        DaprSidecarOptions apiSidecar = tenantsApi.Annotations.OfType<DaprSidecarAnnotation>()
            .ShouldHaveSingleItem().Sidecar.Annotations.OfType<DaprSidecarOptionsAnnotation>()
            .ShouldHaveSingleItem().Options;
        apiSidecar.AppId.ShouldBe("tenants-api");
        apiSidecar.AppProtocol.ShouldBe("https");
        Path.GetFileName(apiSidecar.Config).ShouldBe("accesscontrol.tenants-api.yaml");
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
            await using IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder
                .CreateAsync<Projects.Hexalith_Tenants_AppHost>(args);
        });

        exception.Message.ShouldBe(
            "Authentication:JwtBearer:SigningKey must be configured with at least 32 UTF-8 bytes when EnableKeycloak=false.");
    }

    [Fact]
    public async Task KeycloakModeSharesWorkloadSecretAndProtectsDomainServiceSidecarChannels()
    {
        await using IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Hexalith_Tenants_AppHost>(["--EnableKeycloak=true"]);
        ParameterResource secret = builder.Resources.OfType<ParameterResource>()
            .Single(static resource => resource.Name == "eventstore-workload-client-secret");
        IResource keycloak = builder.Resources.Single(static resource => resource.Name == "security");
        IResource eventStore = builder.Resources.Single(static resource => resource.Name == "eventstore");

        IReadOnlyDictionary<string, object> keycloakEnvironment = await GetEnvironmentAsync(keycloak, builder.ExecutionContext);
        IReadOnlyDictionary<string, object> eventStoreEnvironment = await GetEnvironmentAsync(eventStore, builder.ExecutionContext);
        keycloakEnvironment["HEXALITH_EVENTSTORE_WORKLOAD_CLIENT_SECRET"].ShouldBeSameAs(secret);
        eventStoreEnvironment["Authentication__WorkloadIssuer__ClientSecret"].ShouldBeSameAs(secret);

        await AssertAppChannelTokensAsync(builder);
    }

    [Fact]
    public async Task SymmetricModeSharesOneSigningKeyAndProtectsDomainServiceSidecarChannels()
    {
        await using IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Hexalith_Tenants_AppHost>(
                ["--EnableKeycloak=false", "--Authentication:JwtBearer:SigningKey=RunModelSigningKey-AtLeast32Chars!"]);
        ParameterResource key = builder.Resources.OfType<ParameterResource>()
            .Single(static resource => resource.Name == "tenants-local-jwt-signing-key");

        foreach (string name in new[] { "eventstore", "eventstore-admin", "tenants", "tenants-api", "sample" })
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

        await AssertAppChannelTokensAsync(builder);
    }

    private static async Task AssertAppChannelTokensAsync(IDistributedApplicationTestingBuilder builder)
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

        ProjectResource eventStore = builder.Resources.OfType<ProjectResource>()
            .Single(static resource => resource.Name == "eventstore");
        ProjectResource tenants = builder.Resources.OfType<ProjectResource>()
            .Single(static resource => resource.Name == "tenants");
        ProjectResource sample = builder.Resources.OfType<ProjectResource>()
            .Single(static resource => resource.Name == "sample");

        ParameterResource eventStoreToken = await GetAppChannelTokenAsync(eventStore, builder.ExecutionContext);
        ParameterResource tenantsToken = await GetAppChannelTokenAsync(tenants, builder.ExecutionContext);
        ParameterResource sampleToken = await GetAppChannelTokenAsync(sample, builder.ExecutionContext);
        builder.Resources.OfType<ParameterResource>()
            .Where(static resource => resource.Name == "eventstore-app-api-token")
            .ShouldHaveSingleItem().ShouldBeSameAs(eventStoreToken);
        builder.Resources.OfType<ParameterResource>()
            .Where(static resource => resource.Name.EndsWith("-app-api-token", StringComparison.Ordinal))
            .Select(static resource => resource.Name).Distinct(StringComparer.Ordinal)
            .Count().ShouldBe(3);
        eventStoreToken.ShouldNotBeSameAs(tenantsToken);
        eventStoreToken.ShouldNotBeSameAs(sampleToken);
        tenantsToken.ShouldNotBeSameAs(sampleToken);
        eventStoreToken.Secret.ShouldBeTrue();
        tenantsToken.Secret.ShouldBeTrue();
        sampleToken.Secret.ShouldBeTrue();

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        string? eventStoreValue = await eventStoreToken.GetValueAsync(timeout.Token);
        string? tenantsValue = await tenantsToken.GetValueAsync(timeout.Token);
        string? sampleValue = await sampleToken.GetValueAsync(timeout.Token);
        eventStoreValue.ShouldNotBeNullOrWhiteSpace();
        tenantsValue.ShouldNotBeNullOrWhiteSpace();
        sampleValue.ShouldNotBeNullOrWhiteSpace();
        string.Equals(tenantsValue, sampleValue, StringComparison.Ordinal).ShouldBeFalse();
        string.Equals(eventStoreValue, tenantsValue, StringComparison.Ordinal).ShouldBeFalse();
        string.Equals(eventStoreValue, sampleValue, StringComparison.Ordinal).ShouldBeFalse();
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
            await annotation.Callback(context).WaitAsync(cancellationToken);
        }

        return context.EnvironmentVariables;
    }
}
