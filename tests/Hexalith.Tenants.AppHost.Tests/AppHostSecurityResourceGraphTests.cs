using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

using CommunityToolkit.Aspire.Hosting.Dapr;

using Shouldly;

namespace Hexalith.Tenants.AppHost.Tests;

/// <summary>Checks security environment wiring in the built AppHost model without starting services.</summary>
public sealed class AppHostSecurityResourceGraphTests
{
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
    public async Task KeycloakAndEventStoreShareTheSameWorkloadSecretParameter()
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
        ProjectResource tenants = builder.Resources.OfType<ProjectResource>()
            .Single(static resource => resource.Name == "tenants");
        ProjectResource sample = builder.Resources.OfType<ProjectResource>()
            .Single(static resource => resource.Name == "sample");

        ParameterResource tenantsToken = await GetAppChannelTokenAsync(tenants, builder.ExecutionContext);
        ParameterResource sampleToken = await GetAppChannelTokenAsync(sample, builder.ExecutionContext);
        tenantsToken.ShouldNotBeSameAs(sampleToken);
        tenantsToken.Secret.ShouldBeTrue();
        sampleToken.Secret.ShouldBeTrue();

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        string? tenantsValue = await tenantsToken.GetValueAsync(timeout.Token);
        string? sampleValue = await sampleToken.GetValueAsync(timeout.Token);
        tenantsValue.ShouldNotBeNullOrWhiteSpace();
        sampleValue.ShouldNotBeNullOrWhiteSpace();
        string.Equals(tenantsValue, sampleValue, StringComparison.Ordinal).ShouldBeFalse();
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
        DistributedApplicationExecutionContext execution)
    {
        var context = new EnvironmentCallbackContext(
            execution,
            resource,
            new Dictionary<string, object>(),
            CancellationToken.None);
        foreach (EnvironmentCallbackAnnotation annotation in resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
        {
            await annotation.Callback(context);
        }

        return context.EnvironmentVariables;
    }
}
