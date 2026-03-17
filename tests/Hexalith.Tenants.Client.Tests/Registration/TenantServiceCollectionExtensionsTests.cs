using Dapr.Client;

using Hexalith.Tenants.Client.Configuration;
using Hexalith.Tenants.Client.Registration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Shouldly;

namespace Hexalith.Tenants.Client.Tests.Registration;

public class TenantServiceCollectionExtensionsTests
{
    [Fact]
    public void AddHexalithTenants_RegistersDaprClient()
    {
        // Arrange
        IServiceCollection services = CreateServiceCollectionWithConfig();

        // Act
        services.AddHexalithTenants();

        // Assert — descriptor check only, DO NOT resolve (gRPC needs DAPR sidecar)
        services.ShouldContain(s => s.ServiceType == typeof(DaprClient));
    }

    [Fact]
    public void AddHexalithTenants_RegistersExpectedServiceLifetimes()
    {
        // Arrange
        IServiceCollection services = CreateServiceCollectionWithConfig(
            new Dictionary<string, string?>
            {
                ["Tenants:PubSubName"] = "mypubsub",
            });

        // Act
        services.AddHexalithTenants();

        // Assert
        GetRequiredDescriptor(services, typeof(DaprClient)).Lifetime.ShouldBe(ServiceLifetime.Singleton);
        services
            .Where(s => s.ServiceType == typeof(IConfigureOptions<HexalithTenantsOptions>))
            .Select(s => s.Lifetime)
            .Distinct()
            .ShouldBe([ServiceLifetime.Singleton]);
    }

    [Fact]
    public void AddHexalithTenants_BindsTenantsOptions()
    {
        // Arrange
        IServiceCollection services = CreateServiceCollectionWithConfig(
            new Dictionary<string, string?>
            {
                ["Tenants:PubSubName"] = "mypubsub",
            });

        // Act
        services.AddHexalithTenants();

        // Assert
        using ServiceProvider provider = services.BuildServiceProvider();
        HexalithTenantsOptions options = provider.GetRequiredService<IOptions<HexalithTenantsOptions>>().Value;
        options.PubSubName.ShouldBe("mypubsub");
    }

    [Fact]
    public void AddHexalithTenants_IsIdempotent()
    {
        // Arrange
        IServiceCollection services = CreateServiceCollectionWithConfig(
            new Dictionary<string, string?>
            {
                ["Tenants:PubSubName"] = "mypubsub",
            });

        // Act
        services.AddHexalithTenants();
        services.AddHexalithTenants();

        // Assert — Configure<T>() registers IConfigureOptions<T>, check count
        services.Count(s => s.ServiceType == typeof(IConfigureOptions<HexalithTenantsOptions>)).ShouldBe(1);
        services.Count(s => s.ServiceType == typeof(DaprClient)).ShouldBe(1);
    }

    [Fact]
    public void AddHexalithTenants_ReturnsSameServiceCollection()
    {
        // Arrange
        IServiceCollection services = CreateServiceCollectionWithConfig();

        // Act
        IServiceCollection result = services.AddHexalithTenants();

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddHexalithTenants_DefaultOptionsValues()
    {
        // Arrange — no config section
        IServiceCollection services = CreateServiceCollectionWithConfig();

        // Act
        services.AddHexalithTenants();

        // Assert
        using ServiceProvider provider = services.BuildServiceProvider();
        HexalithTenantsOptions options = provider.GetRequiredService<IOptions<HexalithTenantsOptions>>().Value;
        options.PubSubName.ShouldBe("pubsub");
        options.TopicName.ShouldBe("system.tenants.events");
        options.CommandApiAppId.ShouldBe("commandapi");
    }

    [Fact]
    public void AddHexalithTenants_ThrowsOnNullServices()
    {
        // Assert — must use static call syntax (extension method on null is invalid)
        Should.Throw<ArgumentNullException>(() =>
            TenantServiceCollectionExtensions.AddHexalithTenants(null!));
    }

    [Fact]
    public void AddHexalithTenants_WithAction_ThrowsOnNullServices()
    {
        Should.Throw<ArgumentNullException>(() =>
            TenantServiceCollectionExtensions.AddHexalithTenants(null!, _ => { }));
    }

    [Fact]
    public void AddHexalithTenants_WithAction_ThrowsOnNullAction()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();

        // Assert
        Should.Throw<ArgumentNullException>(() =>
            TenantServiceCollectionExtensions.AddHexalithTenants(services, (Action<HexalithTenantsOptions>)null!));
    }

    [Fact]
    public void AddHexalithTenants_ConfigExistsButNoTenantsSection()
    {
        // Arrange — config with unrelated keys only
        IServiceCollection services = CreateServiceCollectionWithConfig(
            new Dictionary<string, string?>
            {
                ["Logging:LogLevel:Default"] = "Information",
            });

        // Act
        services.AddHexalithTenants();

        // Assert — options resolve with defaults when config section is absent
        using ServiceProvider provider = services.BuildServiceProvider();
        HexalithTenantsOptions options = provider.GetRequiredService<IOptions<HexalithTenantsOptions>>().Value;
        options.PubSubName.ShouldBe("pubsub");
        options.TopicName.ShouldBe("system.tenants.events");
        options.CommandApiAppId.ShouldBe("commandapi");
    }

    [Fact]
    public void AddHexalithTenants_WithAction_ConfiguresOptions()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();

        // Act
        services.AddHexalithTenants(o => o.PubSubName = "custom");

        // Assert
        using ServiceProvider provider = services.BuildServiceProvider();
        HexalithTenantsOptions options = provider.GetRequiredService<IOptions<HexalithTenantsOptions>>().Value;
        options.PubSubName.ShouldBe("custom");
    }

    [Fact]
    public void AddHexalithTenants_SkipsDaprClientIfAlreadyRegistered()
    {
        // Arrange
        IServiceCollection services = CreateServiceCollectionWithConfig();
        services.AddDaprClient();
        int daprCountBefore = services.Count(s => s.ServiceType == typeof(DaprClient));

        // Act
        services.AddHexalithTenants();
        int daprCountAfter = services.Count(s => s.ServiceType == typeof(DaprClient));

        // Assert
        daprCountAfter.ShouldBe(daprCountBefore);
    }

    [Fact]
    public void AddHexalithTenants_RegistersDaprClientWhenOptionsAlreadyConfigured()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();
        services.Configure<HexalithTenantsOptions>(options => options.TopicName = "preconfigured");

        // Act
        services.AddHexalithTenants();

        // Assert
        services.ShouldContain(s => s.ServiceType == typeof(DaprClient));
    }

    [Fact]
    public void AddHexalithTenants_BindsConfigurationAddedAfterInitialRegistration()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();
        services.AddHexalithTenants();

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
            [
                new KeyValuePair<string, string?>("Tenants:PubSubName", "latepubsub"),
            ])
            .Build();
        services.AddSingleton(configuration);

        // Act
        services.AddHexalithTenants();

        // Assert
        using ServiceProvider provider = services.BuildServiceProvider();
        HexalithTenantsOptions options = provider.GetRequiredService<IOptions<HexalithTenantsOptions>>().Value;
        options.PubSubName.ShouldBe("latepubsub");
        services.Count(s => s.ServiceType == typeof(DaprClient)).ShouldBe(1);
    }

    [Fact]
    public void AddHexalithTenants_WorksWithoutIConfiguration()
    {
        // Arrange — empty ServiceCollection, no IConfiguration
        IServiceCollection services = new ServiceCollection();

        // Act
        services.AddHexalithTenants();

        // Assert — options registered with defaults, no exception
        using ServiceProvider provider = services.BuildServiceProvider();
        HexalithTenantsOptions options = provider.GetRequiredService<IOptions<HexalithTenantsOptions>>().Value;
        options.PubSubName.ShouldBe("pubsub");
    }

    private static IServiceCollection CreateServiceCollectionWithConfig(
        Dictionary<string, string?>? configValues = null)
    {
        var services = new ServiceCollection();
        if (configValues is not null)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configValues)
                .Build();
            services.AddSingleton<IConfiguration>(configuration);
        }

        return services;
    }

    private static ServiceDescriptor GetRequiredDescriptor(IServiceCollection services, Type serviceType) =>
        services.FirstOrDefault(s => s.ServiceType == serviceType)
        ?? throw new ShouldAssertException($"Expected descriptor for service type '{serviceType}'.");
}
