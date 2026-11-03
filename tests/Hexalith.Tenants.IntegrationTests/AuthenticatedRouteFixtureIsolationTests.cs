using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;

using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Client.Routing;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.Tenants.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Hexalith.Tenants.Authorization;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;

using Hexalith.Tenants.IntegrationTests.Fixtures;

using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

/// <summary>Proves request-path settings are inherited only by their independently started topology.</summary>
[Collection("BootstrapReadiness")]
public sealed class AuthenticatedRouteFixtureIsolationTests
{
    /// <summary>Authentication finishes at the cookie-issuing callback before any workspace data read.</summary>
    [Theory]
    [InlineData("valid")]
    [InlineData("foreign-return")]
    [InlineData("wrong-return")]
    [InlineData("missing-cookie")]
    [InlineData("foreign-callback")]
    public async Task OidcRedirectControlRetainsLocalOriginsAndCookieWithoutLoadingTheWorkspace(string scenario)
    {
        var ui = new Uri("https://localhost:44521");
        var issuer = new Uri("http://localhost:44522");
        var cookies = new CookieContainer();
        int sends = 0;
        const string cookieName = ".AspNetCore.Hexalith.FrontComposer.Cookie";
        Task<HttpResponseMessage> Send(Uri destination, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            sends++;
            Uri next;
            if (sends == 1) next = new(issuer, "/authorize");
            else if (sends == 2) next = scenario == "foreign-callback"
                ? new Uri("https://localhost:44523/signin-oidc") : new Uri(ui, "/signin-oidc");
            else if (sends == 3 && destination == new Uri(ui, "/signin-oidc"))
            {
                if (scenario != "missing-cookie")
                    cookies.Add(ui, new Cookie(cookieName, "synthetic-opaque-ticket", "/") { Secure = true, HttpOnly = true });
                next = scenario == "foreign-return" ? new Uri("https://localhost:44523/tenants")
                    : new Uri(ui, scenario == "wrong-return" ? "/unexpected" : "/tenants");
            }
            else throw new InvalidOperationException("Authentication must finish before loading workspace data.");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Redirect) { Headers = { Location = next } });
        }
        Task<string> pending = AuthenticatedTenantsTopologyFixture.FollowSignInRedirectsAsync(ui, issuer, cookies,
            Send, TestContext.Current.CancellationToken);
        if (scenario == "valid")
        {
            (await pending).ShouldBe(cookieName + "=synthetic-opaque-ticket");
            sends.ShouldBe(3);
        }
        else await Should.ThrowAsync<InvalidOperationException>(() => pending);
    }

    [Theory]
    [InlineData(false, "true")]
    [InlineData(true, "true")]
    [InlineData(false, null)]
    [InlineData(true, null)]
    public async Task RequestPathProfileIsInheritedByChildAndRestoredBeforeOtherFixtures(bool fail, string? previous)
    {
        const string key = "EventStore__Projections__WorkersEnabled";
        string? original = Environment.GetEnvironmentVariable(key);
        try
        {
            Environment.SetEnvironmentVariable(key, previous);
            async ValueTask Initialize()
            {
                Environment.GetEnvironmentVariable(key).ShouldBe("false");
                var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true };
                start.ArgumentList.Add("--info");
                start.Environment[key].ShouldBe("false");
                await Task.Yield();
                if (fail) throw new InvalidOperationException("startup-control");
            }

            if (fail) await Should.ThrowAsync<InvalidOperationException>(async () => await AuthenticatedRouteTopologyFixture.WithRequestPathSettingsAsync(Initialize));
            else await AuthenticatedRouteTopologyFixture.WithRequestPathSettingsAsync(Initialize);
            Environment.GetEnvironmentVariable(key).ShouldBe(previous);
            await AspireTopologyFixture.WithBootstrapStartupSettingsAsync(() =>
            {
                Environment.GetEnvironmentVariable(key).ShouldBe(previous);
                return ValueTask.CompletedTask;
            });
            var next = new ProcessStartInfo("dotnet");
            next.Environment.TryGetValue(key, out string? inherited).ShouldBe(previous is not null);
            inherited.ShouldBe(previous);
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, original);
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task NativeOwnerKeyIsInheritedOnlyDuringStartupAndRestoredOnFailure(bool fail, bool inherited)
    {
        const string parameter = "Parameters__tenants-source-private-key";
        string? original = Environment.GetEnvironmentVariable(parameter);
        string? previous = inherited ? "previous-owner-key-control" : null;
        string privateKey = NativeOwnerReadServiceComposition.CreatePrivateKey();
        try
        {
            Environment.SetEnvironmentVariable(parameter, previous);
            async ValueTask Initialize()
            {
                string.Equals(Environment.GetEnvironmentVariable(parameter), privateKey, StringComparison.Ordinal)
                    .ShouldBeTrue("only the scoped child-startup environment receives the fixture key");
                var child = new ProcessStartInfo("dotnet");
                string.Equals(child.Environment[parameter], privateKey, StringComparison.Ordinal).ShouldBeTrue();
                using RSA rsa = RSA.Create();
                rsa.ImportPkcs8PrivateKey(Convert.FromBase64String(child.Environment[parameter]!), out _);
                rsa.KeySize.ShouldBe(2048);
                await Task.Yield();
                if (fail) throw new InvalidOperationException("startup-control");
            }

            if (fail)
                await Should.ThrowAsync<InvalidOperationException>(async () =>
                    await AspireTopologyFixture.WithNativeOwnerStartupSettingsAsync(privateKey, Initialize));
            else
                await AspireTopologyFixture.WithNativeOwnerStartupSettingsAsync(privateKey, Initialize);
            string.Equals(Environment.GetEnvironmentVariable(parameter), previous, StringComparison.Ordinal).ShouldBeTrue();
            var next = new ProcessStartInfo("dotnet");
            next.Environment.TryGetValue(parameter, out string? nextKey).ShouldBe(inherited);
            string.Equals(nextKey, previous, StringComparison.Ordinal).ShouldBeTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable(parameter, original);
        }
    }

    [Fact]
    public async Task NativeOwnerCompositionUsesTheCanonicalProductionIssuerReadersAndDurableAuthority()
    {
        string privateKey = NativeOwnerReadServiceComposition.CreatePrivateKey();
        string.Equals(privateKey, NativeOwnerReadServiceComposition.CreatePrivateKey(), StringComparison.Ordinal).ShouldBeFalse();
        var sidecar = new Uri("http://127.0.0.1:54321");
        var configuration = NativeOwnerReadServiceComposition.CreateConfiguration(sidecar, privateKey);
        string.Equals(configuration["Authentication:BoundStreamRead:PrivateKey"], privateKey, StringComparison.Ordinal).ShouldBeTrue();
        configuration["DAPR_HTTP_ENDPOINT"].ShouldBe(sidecar.AbsoluteUri);
        configuration["Authentication:WorkloadIssuer:Workload"].ShouldBe("tenants");
        configuration["Tenants:EventStoreAppId"].ShouldBe("eventstore");
        IServiceCollection services = NativeOwnerReadServiceComposition.CreateServices(configuration);
        await using ServiceProvider provider = services.BuildServiceProvider();
        provider.GetRequiredService<IHostEnvironment>().EnvironmentName.ShouldBe(Environments.Development);
        provider.GetRequiredService<IRouteCatalogStateStore>().ShouldBeOfType<DaprRouteCatalogStateStore>();
        provider.GetRequiredService<IRouteCatalogAuthority>().ShouldBeOfType<RouteCatalogAuthority>();
        provider.GetRequiredService<TenantOwnerSourceGateway>().ShouldNotBeNull();
        var policies = provider.GetRequiredService<IOptionsMonitor<HttpStandardResilienceOptions>>();
        var ownedPolicy = policies.Get(nameof(TenantOwnerSourceGateway) + "-standard");
        ownedPolicy.AttemptTimeout.Timeout.ShouldBe(TimeSpan.FromSeconds(30));
        ownedPolicy.TotalRequestTimeout.Timeout.ShouldBe(TimeSpan.FromSeconds(30));
        ownedPolicy.CircuitBreaker.SamplingDuration.ShouldBe(TimeSpan.FromSeconds(60));
        var inheritedPolicy = policies.Get("-standard");
        inheritedPolicy.AttemptTimeout.Timeout.ShouldBe(TimeSpan.FromSeconds(10));
        inheritedPolicy.TotalRequestTimeout.Timeout.ShouldBe(TimeSpan.FromSeconds(30));
        provider.GetRequiredService<IAuthoritativeEventStreamReader>().ShouldBeOfType<AuthoritativeEventStreamReader>();
        provider.GetRequiredService<INativeSourceMetadataReader>().ShouldBeOfType<GatewayNativeSourceMetadataReader>();
        var issuer = provider.GetRequiredService<IWorkloadAssertionIssuer>();
        issuer.ShouldBeOfType<JwtWorkloadAssertionIssuer>();
        string? assertion = await issuer.IssueAsync(new WorkloadAssertionRequest("eventstore", "eventstore:stream-read"),
            TestContext.Current.CancellationToken);
        string.IsNullOrWhiteSpace(assertion).ShouldBeFalse();
        var token = new JwtSecurityTokenHandler().ReadJwtToken(assertion);
        token.Issuer.ShouldBe("hexalith-dev");
        token.Audiences.ShouldContain("eventstore");
        token.Claims.Single(claim => claim.Type == "azp").Value.ShouldBe("tenants");
        token.Claims.Single(claim => claim.Type == EventStoreWorkloadAuthenticationDefaults.OperationClaimType)
            .Value.ShouldBe("eventstore:stream-read");
        token.Claims.Any(claim => claim.Type == EventStoreWorkloadAuthenticationDefaults.TenantBindingClaimType
            && claim.Value == "system").ShouldBeFalse("the production owner proof supplies canonical resource binding");
    }

#if HEXALITH_DEVELOPMENT_CATALOG_COHORT
    [Theory]
    [InlineData("foreign-host")]
    [InlineData("foreign-app")]
    [InlineData("wrong-component")]
    [InlineData("wrong-state-key")]
    [InlineData("wrong-sidecar")]
    [InlineData("missing-membership")]
    [InlineData("wrong-membership")]
    [InlineData("changed-evidence")]
    [InlineData("removed-evidence")]
    public async Task RegisteredNativeOwnerObserverRereadsAndRefusesChangedSyntheticCustody(string change)
    {
        var (configuration, observation, activation, hosts) = CreateSyntheticOwnerObservation();
        string directory = Directory.CreateTempSubdirectory("tenants-synthetic-owner-observer-").FullName;
        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        string path = Path.Combine(directory, "tenants.observation.json");
        try
        {
            WriteSyntheticOwnerObservation(path, observation);
            IServiceCollection services = NativeOwnerReadServiceComposition.CreateServices(configuration);
            NativeOwnerReadServiceComposition.RegisterCommittedObservation(services, configuration, activation, directory,
                TestContext.Current.CancellationToken);
            await using ServiceProvider provider = services.BuildServiceProvider();
            var observer = provider.GetRequiredService<IRouteCatalogHostObserver>();
            observer.ShouldBeOfType<ObservedRouteCatalogHost>();
            var coordinates = provider.GetRequiredService<IRouteCatalogStateStore>().Coordinates.ShouldNotBeNull();
            var observed = await observer.ObserveAsync(activation, coordinates, TestContext.Current.CancellationToken);
            observed.ShouldBe(ObservedRouteCatalogHost.Identity(observation),
                "synthetic unit data exercises the real registered observer; it establishes no runtime acceptance");

            switch (change)
            {
                case "foreign-host": observation = observation with
                {
                    HostId = "synthetic-foreign-host", RequiredHostIds = ["synthetic-foreign-host"],
                }; break;
                case "foreign-app": observation = observation with { AppId = "synthetic-foreign-app" }; break;
                case "wrong-component": observation = observation with { AuthorityCoordinates = coordinates with { StateStore = "synthetic-other-component" } }; break;
                case "wrong-state-key": observation = observation with { AuthorityCoordinates = coordinates with { StateKey = "synthetic-other-key" } }; break;
                case "wrong-sidecar": observation = observation with { AuthorityCoordinates = coordinates with { HttpEndpoint = "http://127.0.0.1:54322/" } }; break;
                case "missing-membership": hosts.Clear(); break;
                case "wrong-membership": hosts[0] = hosts[0] with { InventoryDigest = new string('0', 64) }; break;
                case "changed-evidence": observation = observation with { AuthenticationProfile = "synthetic-changed-authentication"u8.ToArray() }; break;
                case "removed-evidence": File.Delete(path); break;
                default: throw new InvalidOperationException("Unknown synthetic custody control.");
            }
            // Matching synthetic membership must not hide an explicit role or coordinate violation.
            if (change is "foreign-host" or "foreign-app" or "wrong-component" or "wrong-state-key" or "wrong-sidecar")
                hosts[0] = ObservedRouteCatalogHost.Identity(observation);
            if (change is not ("removed-evidence" or "missing-membership" or "wrong-membership"))
                WriteSyntheticOwnerObservation(path, observation);
            var refusal = await Should.ThrowAsync<InvalidOperationException>(() => observer.ObserveAsync(
                activation, coordinates, TestContext.Current.CancellationToken));
            refusal.Message.ShouldBe(change == "removed-evidence"
                ? "source-catalog-private-evidence-invalid" : "native-owner-observation-mismatched");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void NativeOwnerObservationRegistrationRefusesMissingSyntheticPrivateEvidence()
    {
        var (configuration, _, activation, _) = CreateSyntheticOwnerObservation();
        string directory = Directory.CreateTempSubdirectory("tenants-synthetic-owner-observer-missing-").FullName;
        try
        {
            IServiceCollection services = NativeOwnerReadServiceComposition.CreateServices(configuration);
            Should.Throw<InvalidOperationException>(() => NativeOwnerReadServiceComposition.RegisterCommittedObservation(
                services, configuration, activation, directory, TestContext.Current.CancellationToken));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static (IConfiguration Configuration, RouteCatalogHostObservation Observation,
        RouteCatalogActivation Activation, List<RouteCatalogHostIdentity> Hosts) CreateSyntheticOwnerObservation()
    {
        // These are deliberately synthetic private unit inputs. No authority is activated and no native transport is invoked.
        var configuration = NativeOwnerReadServiceComposition.CreateConfiguration(new Uri("http://127.0.0.1:54321"),
            NativeOwnerReadServiceComposition.CreatePrivateKey());
        var coordinates = RouteCatalogStateCoordinates.Capture(configuration, new RouteCatalogHostOptions
        {
            StateStore = configuration["EventStore:RouteCatalog:StateStore"]!,
            StateKey = configuration["EventStore:RouteCatalog:StateKey"]!,
        });
        var declaration = CompiledRouteCatalogDeclarations.Read([typeof(TenantCatalogDeclarations).Assembly])
            .First(route => route.Kind == "query");
        var operation = CompiledRouteCatalogDeclarations.ReadOperations([typeof(TenantCatalogDeclarations).Assembly])
            .Single(item => item.RouteId == declaration.RouteId);
        var envelope = VersionedRouteCatalogBuilder.Build(new(1, "Development", [declaration], [operation],
            [new(declaration.RouteId, "tenants", TenantCatalogDeclarations.ReceiverMethods[declaration.RouteId],
                declaration.ContractVersion, false, "*")], []));
        var observation = new RouteCatalogHostObservation("tenants", "tenants", [declaration], envelope.Routes, ["tenants"],
            [JsonSerializer.SerializeToUtf8Bytes(new
            {
                kind = "Component", metadata = new { name = coordinates.StateStore },
                spec = new { type = "state.redis", metadata = new[] { new { name = "keyPrefix", value = "none" } } },
                scopes = new[] { "tenants" },
            }), "{\"spec\":{\"accessControl\":{\"defaultAction\":\"deny\",\"trustDomain\":\"synthetic-unit-only\"}}}"u8.ToArray()],
            "synthetic-unit-authentication"u8.ToArray(), AdmissionPolicies: [], ProjectionCapabilities: [], AuxiliaryOperations: [],
            AuthorityCoordinates: coordinates, Operations: [operation]);
        List<RouteCatalogHostIdentity> hosts = [ObservedRouteCatalogHost.Identity(observation)];
        var activation = new RouteCatalogActivation("synthetic-observer-unit-control", RouteCatalogCodec.Encode(envelope),
            RouteCatalogCodec.ComputeRootDigest(envelope), null, hosts, "synthetic-unit-data-not-runtime-acceptance", "");
        return (configuration, observation, activation, hosts);
    }

    private static void WriteSyntheticOwnerObservation(string path, RouteCatalogHostObservation observation)
    {
        File.WriteAllBytes(path, JsonSerializer.SerializeToUtf8Bytes(observation));
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }
#endif

    [Fact]
    public void RestAndBackgroundDeliveryUseSeparateFixturesAndLifecycleMappings()
    {
        using var runner = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "xunit.runner.json")));
        runner.RootElement.GetProperty("parallelizeTestCollections").GetBoolean().ShouldBeFalse();
        runner.RootElement.GetProperty("maxParallelThreads").GetInt32().ShouldBe(1);
        using var platform = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "testconfig.json")));
        platform.RootElement.GetProperty("xUnit").GetProperty("parallelizeTestCollections").GetBoolean().ShouldBeFalse();
        platform.RootElement.GetProperty("xUnit").GetProperty("maxParallelThreads").GetInt32().ShouldBe(1);
        foreach (Type fixture in new[] { typeof(AuthenticatedRouteTopologyFixture), typeof(AuthenticatedTenantsTopologyFixture) })
        {
            var mapping = fixture.GetInterfaceMap(typeof(IAsyncLifetime));
            int initialize = Array.FindIndex(mapping.InterfaceMethods, method => method.Name == nameof(IAsyncLifetime.InitializeAsync));
            mapping.TargetMethods[initialize].DeclaringType.ShouldBe(fixture);
        }
        typeof(AuthenticatedTenantsRouteTests).GetCustomAttributes(typeof(CollectionAttribute), false)
            .Cast<CollectionAttribute>().Single().Name.ShouldBe("AuthenticatedRouteTopology");
        typeof(AuthenticatedTenantsUiRouteTests).GetCustomAttributes(typeof(CollectionAttribute), false)
            .Cast<CollectionAttribute>().Single().Name.ShouldBe("AuthenticatedTenantsTopology");
        typeof(AspireTopologyTests).GetCustomAttributes(typeof(CollectionAttribute), false)
            .Cast<CollectionAttribute>().Single().Name.ShouldBe("AspireTopology");
        typeof(TenantsUiRouteSmokeTests).GetCustomAttributes(typeof(CollectionAttribute), false)
            .Cast<CollectionAttribute>().Single().Name.ShouldBe("AspireTopology");
        typeof(AuthenticatedTenantsTopologyCollection).GetInterfaces().ShouldContain(typeof(ICollectionFixture<AuthenticatedTenantsTopologyFixture>));
        typeof(AspireTopologyCollection).GetInterfaces().ShouldContain(typeof(ICollectionFixture<AspireTopologyFixture>));
        typeof(AuthenticatedRouteTopologyCollection).GetInterfaces().ShouldContain(typeof(ICollectionFixture<AuthenticatedRouteTopologyFixture>));
    }
}
