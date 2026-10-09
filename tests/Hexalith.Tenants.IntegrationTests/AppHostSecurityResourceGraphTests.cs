using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

using CommunityToolkit.Aspire.Hosting.Dapr;

using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

/// <summary>Checks security environment wiring in the built AppHost model without starting services.</summary>
[Trait("Category", "Integration")]
public sealed class AppHostSecurityResourceGraphTests
{
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
    }

    [Fact]
    public async Task SymmetricModeSharesOneSigningKeyAndProtectsTheSampleSidecarChannel()
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

        ProjectResource sample = builder.Resources.OfType<ProjectResource>()
            .Single(static resource => resource.Name == "sample");
        IReadOnlyDictionary<string, object> sampleEnvironment = await GetEnvironmentAsync(sample, builder.ExecutionContext);
        IDaprSidecarResource sidecar = sample.Annotations.OfType<DaprSidecarAnnotation>()
            .ShouldHaveSingleItem().Sidecar;
        IReadOnlyDictionary<string, object> sidecarEnvironment = await GetEnvironmentAsync(sidecar, builder.ExecutionContext);
        sampleEnvironment["APP_API_TOKEN"].ShouldBeSameAs(sidecarEnvironment["APP_API_TOKEN"]);
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
