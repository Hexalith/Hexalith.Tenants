using System.Diagnostics;

using Hexalith.Tenants.IntegrationTests.Fixtures;

using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

/// <summary>Proves request-path settings are inherited only by their independently started topology.</summary>
[Collection("BootstrapReadiness")]
public sealed class AuthenticatedRouteFixtureIsolationTests
{
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
