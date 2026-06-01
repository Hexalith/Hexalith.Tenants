using System.Reflection;
using System.Xml.Linq;

using Dapr.Client;

using Hexalith.Tenants.Client.Configuration;
using Hexalith.Tenants.Client.Handlers;
using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Client.Registration;
using Hexalith.Tenants.Client.Subscription;
using Hexalith.Tenants.Contracts.Events;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Shouldly;

namespace Hexalith.Tenants.Client.Tests.Registration;

public class TenantServiceCollectionExtensionsTests {
    [Fact]
    public void AddHexalithTenants_RegistersDaprClient() {
        // Arrange
        IServiceCollection services = CreateServiceCollectionWithConfig();

        // Act
        _ = services.AddHexalithTenants();

        // Assert — descriptor check only, DO NOT resolve (gRPC needs DAPR sidecar)
        services.ShouldContain(s => s.ServiceType == typeof(DaprClient));
    }

    [Fact]
    public void AddHexalithTenants_RegistersExpectedServiceLifetimes() {
        // Arrange
        IServiceCollection services = CreateServiceCollectionWithConfig(
            new Dictionary<string, string?> {
                ["Tenants:PubSubName"] = "mypubsub",
            });

        // Act
        _ = services.AddHexalithTenants();

        // Assert
        GetRequiredDescriptor(services, typeof(DaprClient)).Lifetime.ShouldBe(ServiceLifetime.Singleton);
        services
            .Where(s => s.ServiceType == typeof(IConfigureOptions<HexalithTenantsOptions>))
            .Select(s => s.Lifetime)
            .Distinct()
            .ShouldBe([ServiceLifetime.Singleton]);
    }

    [Fact]
    public void AddHexalithTenants_BindsTenantsOptions() {
        // Arrange
        IServiceCollection services = CreateServiceCollectionWithConfig(
            new Dictionary<string, string?> {
                ["Tenants:PubSubName"] = "mypubsub",
            });

        // Act
        _ = services.AddHexalithTenants();

        // Assert
        using ServiceProvider provider = services.BuildServiceProvider();
        HexalithTenantsOptions options = provider.GetRequiredService<IOptions<HexalithTenantsOptions>>().Value;
        options.PubSubName.ShouldBe("mypubsub");
        options.TopicName.ShouldBe("tenants.events");
    }

    [Fact]
    public void AddHexalithTenants_IsIdempotent() {
        // Arrange
        IServiceCollection services = CreateServiceCollectionWithConfig(
            new Dictionary<string, string?> {
                ["Tenants:PubSubName"] = "mypubsub",
            });

        // Act
        _ = services.AddHexalithTenants();
        _ = services.AddHexalithTenants();

        // Assert — Configure<T>() registers IConfigureOptions<T>, check count
        services.Count(s => s.ServiceType == typeof(IConfigureOptions<HexalithTenantsOptions>)).ShouldBe(1);
        services.Count(s => s.ServiceType == typeof(DaprClient)).ShouldBe(1);
        services.Count(s => s.ServiceType == typeof(TenantEventProcessor)).ShouldBe(1);
        services.Count(s => s.ServiceType == typeof(ITenantProjectionStore)).ShouldBe(1);
    }

    [Fact]
    public void AddHexalithTenants_ReturnsSameServiceCollection() {
        // Arrange
        IServiceCollection services = CreateServiceCollectionWithConfig();

        // Act
        IServiceCollection result = services.AddHexalithTenants();

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddHexalithTenants_WithAction_ReturnsSameServiceCollection() {
        // Arrange
        IServiceCollection services = new ServiceCollection();

        // Act
        IServiceCollection result = services.AddHexalithTenants(_ => { });

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddHexalithTenants_DefaultOptionsValues() {
        // Arrange — no config section
        IServiceCollection services = CreateServiceCollectionWithConfig();

        // Act
        _ = services.AddHexalithTenants();

        // Assert
        using ServiceProvider provider = services.BuildServiceProvider();
        HexalithTenantsOptions options = provider.GetRequiredService<IOptions<HexalithTenantsOptions>>().Value;
        options.PubSubName.ShouldBe("pubsub");
        options.TopicName.ShouldBe("tenants.events");
    }

    [Fact]
    public void HexalithTenantsOptions_UsesTenantsConfigurationSectionName() =>
        HexalithTenantsOptions.ConfigurationSectionName.ShouldBe("Tenants");

    [Fact]
    public void HexalithTenantsOptions_DoesNotExposeStaleCommandApiAppIdOption() {
        // Act
        PropertyInfo? property = typeof(HexalithTenantsOptions).GetProperty(
            "CommandApiAppId",
            BindingFlags.Instance | BindingFlags.Public);

        // Assert
        property.ShouldBeNull();
    }

    [Fact]
    public void AddHexalithTenants_ThrowsOnNullServices() =>
        // Assert — must use static call syntax (extension method on null is invalid)
        Should.Throw<ArgumentNullException>(() =>
            TenantServiceCollectionExtensions.AddHexalithTenants(null!));

    [Fact]
    public void AddHexalithTenants_WithAction_ThrowsOnNullServices() => Should.Throw<ArgumentNullException>(() =>
                                                                                 TenantServiceCollectionExtensions.AddHexalithTenants(null!, _ => { }));

    [Fact]
    public void AddHexalithTenants_WithAction_ThrowsOnNullAction() {
        // Arrange
        IServiceCollection services = new ServiceCollection();

        // Assert
        _ = Should.Throw<ArgumentNullException>(() =>
            TenantServiceCollectionExtensions.AddHexalithTenants(services, null!));
    }

    [Fact]
    public void AddHexalithTenants_ConfigExistsButNoTenantsSection() {
        // Arrange — config with unrelated keys only
        IServiceCollection services = CreateServiceCollectionWithConfig(
            new Dictionary<string, string?> {
                ["Logging:LogLevel:Default"] = "Information",
            });

        // Act
        _ = services.AddHexalithTenants();

        // Assert — options resolve with defaults when config section is absent
        using ServiceProvider provider = services.BuildServiceProvider();
        HexalithTenantsOptions options = provider.GetRequiredService<IOptions<HexalithTenantsOptions>>().Value;
        options.PubSubName.ShouldBe("pubsub");
        options.TopicName.ShouldBe("tenants.events");
    }

    [Fact]
    public void AddHexalithTenants_WithAction_ConfiguresOptions() {
        // Arrange
        IServiceCollection services = new ServiceCollection();

        // Act
        _ = services.AddHexalithTenants(o => o.PubSubName = "custom");

        // Assert
        using ServiceProvider provider = services.BuildServiceProvider();
        HexalithTenantsOptions options = provider.GetRequiredService<IOptions<HexalithTenantsOptions>>().Value;
        options.PubSubName.ShouldBe("custom");
    }

    [Fact]
    public void AddHexalithTenants_WithAction_BindsConfiguredTopicName() {
        // Arrange
        IServiceCollection services = new ServiceCollection();

        // Act
        _ = services.AddHexalithTenants(o => o.TopicName = "custom.topic");

        // Assert
        using ServiceProvider provider = services.BuildServiceProvider();
        HexalithTenantsOptions options = provider.GetRequiredService<IOptions<HexalithTenantsOptions>>().Value;
        options.TopicName.ShouldBe("custom.topic");
    }

    [Fact]
    public void AddHexalithTenants_WithAction_AppliesAfterExistingOptionsConfiguration() {
        // Arrange
        IServiceCollection services = new ServiceCollection();
        _ = services.Configure<HexalithTenantsOptions>(options => options.PubSubName = "preconfigured");

        // Act
        _ = services.AddHexalithTenants(options => options.PubSubName = "explicit");

        // Assert
        using ServiceProvider provider = services.BuildServiceProvider();
        HexalithTenantsOptions options = provider.GetRequiredService<IOptions<HexalithTenantsOptions>>().Value;
        options.PubSubName.ShouldBe("explicit");
    }

    [Fact]
    public void AddHexalithTenants_WithAction_AppliesAfterDefaultConfigurationBinding() {
        // Arrange
        IServiceCollection services = CreateServiceCollectionWithConfig(
            new Dictionary<string, string?> {
                ["Tenants:PubSubName"] = "configured",
            });
        _ = services.AddHexalithTenants();

        // Act
        _ = services.AddHexalithTenants(options => options.PubSubName = "explicit");

        // Assert
        using ServiceProvider provider = services.BuildServiceProvider();
        HexalithTenantsOptions options = provider.GetRequiredService<IOptions<HexalithTenantsOptions>>().Value;
        options.PubSubName.ShouldBe("explicit");
    }

    [Theory]
    [InlineData("Tenants:PubSubName", "HexalithTenantsOptions.PubSubName")]
    [InlineData("Tenants:TopicName", "HexalithTenantsOptions.TopicName")]
    public void AddHexalithTenants_InvalidConfiguredOptionsThrowOptionsValidationException(
        string configurationKey,
        string expectedFailure) {
        // Arrange
        IServiceCollection services = CreateServiceCollectionWithConfig(
            new Dictionary<string, string?> {
                [configurationKey] = " ",
            });
        _ = services.AddHexalithTenants();

        // Act
        using ServiceProvider provider = services.BuildServiceProvider();
        OptionsValidationException exception = Should.Throw<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<HexalithTenantsOptions>>().Value);

        // Assert
        exception.Failures.ShouldContain(failure => failure.Contains(expectedFailure, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(true, "HexalithTenantsOptions.PubSubName")]
    [InlineData(false, "HexalithTenantsOptions.TopicName")]
    public void AddHexalithTenants_WithAction_InvalidOptionsThrowOptionsValidationException(
        bool invalidatePubSubName,
        string expectedFailure) {
        // Arrange
        IServiceCollection services = new ServiceCollection();
        _ = services.AddHexalithTenants(options => {
            if (invalidatePubSubName) {
                options.PubSubName = " ";
            }
            else {
                options.TopicName = " ";
            }
        });

        // Act
        using ServiceProvider provider = services.BuildServiceProvider();
        OptionsValidationException exception = Should.Throw<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<HexalithTenantsOptions>>().Value);

        // Assert
        exception.Failures.ShouldContain(failure => failure.Contains(expectedFailure, StringComparison.Ordinal));
    }

    [Fact]
    public void AddHexalithTenants_InvalidOptionsFailStartupValidation() {
        // Arrange
        IServiceCollection services = CreateServiceCollectionWithConfig(
            new Dictionary<string, string?> {
                ["Tenants:PubSubName"] = " ",
            });
        _ = services.AddHexalithTenants();

        // Act
        using ServiceProvider provider = services.BuildServiceProvider();
        OptionsValidationException exception = Should.Throw<OptionsValidationException>(
            () => provider.GetRequiredService<IStartupValidator>().Validate());

        // Assert
        exception.Failures.ShouldContain(failure => failure.Contains("HexalithTenantsOptions.PubSubName", StringComparison.Ordinal));
    }

    [Fact]
    public void AddHexalithTenants_RegistersOptionsValidationOnce() {
        // Arrange
        IServiceCollection services = CreateServiceCollectionWithConfig();

        // Act
        _ = services.AddHexalithTenants();
        _ = services.AddHexalithTenants();

        // Assert
        services.Count(s => s.ServiceType == typeof(IValidateOptions<HexalithTenantsOptions>)).ShouldBe(1);
        services.Count(s => s.ServiceType == typeof(IStartupValidator)).ShouldBe(1);
    }

    [Fact]
    public void AddHexalithTenants_SkipsDaprClientIfAlreadyRegistered() {
        // Arrange
        IServiceCollection services = CreateServiceCollectionWithConfig();
        services.AddDaprClient();
        int daprCountBefore = services.Count(s => s.ServiceType == typeof(DaprClient));

        // Act
        _ = services.AddHexalithTenants();
        int daprCountAfter = services.Count(s => s.ServiceType == typeof(DaprClient));

        // Assert
        daprCountAfter.ShouldBe(daprCountBefore);
    }

    [Fact]
    public void AddHexalithTenants_RegistersDaprClientWhenOptionsAlreadyConfigured() {
        // Arrange
        IServiceCollection services = new ServiceCollection();
        _ = services.Configure<HexalithTenantsOptions>(options => options.TopicName = "preconfigured");

        // Act
        _ = services.AddHexalithTenants();

        // Assert
        services.ShouldContain(s => s.ServiceType == typeof(DaprClient));
    }

    [Fact]
    public void AddHexalithTenants_BindsConfigurationAddedAfterInitialRegistration() {
        // Arrange
        IServiceCollection services = new ServiceCollection();
        _ = services.AddHexalithTenants();

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
            [
                new KeyValuePair<string, string?>("Tenants:PubSubName", "latepubsub"),
            ])
            .Build();
        _ = services.AddSingleton(configuration);

        // Act
        _ = services.AddHexalithTenants();

        // Assert
        using ServiceProvider provider = services.BuildServiceProvider();
        HexalithTenantsOptions options = provider.GetRequiredService<IOptions<HexalithTenantsOptions>>().Value;
        options.PubSubName.ShouldBe("latepubsub");
        services.Count(s => s.ServiceType == typeof(DaprClient)).ShouldBe(1);
    }

    [Fact]
    public void AddHexalithTenants_WorksWithoutIConfiguration() {
        // Arrange — empty ServiceCollection, no IConfiguration
        IServiceCollection services = new ServiceCollection();

        // Act
        _ = services.AddHexalithTenants();

        // Assert — options registered with defaults, no exception
        using ServiceProvider provider = services.BuildServiceProvider();
        HexalithTenantsOptions options = provider.GetRequiredService<IOptions<HexalithTenantsOptions>>().Value;
        options.PubSubName.ShouldBe("pubsub");
    }

    [Fact]
    public void AddHexalithTenants_RegistersITenantProjectionStore() {
        // Arrange
        IServiceCollection services = new ServiceCollection();

        // Act
        _ = services.AddHexalithTenants();

        // Assert
        services.ShouldContain(s => s.ServiceType == typeof(ITenantProjectionStore));
    }

    [Fact]
    public void AddHexalithTenants_RegistersTenantEventProcessor() {
        // Arrange
        IServiceCollection services = new ServiceCollection();

        // Act
        _ = services.AddHexalithTenants();

        // Assert
        services.ShouldContain(s => s.ServiceType == typeof(TenantEventProcessor));
    }

    [Fact]
    public void AddHexalithTenants_RegistersTenantProjectionEventHandler() {
        // Arrange
        IServiceCollection services = new ServiceCollection();

        // Act
        _ = services.AddHexalithTenants();

        // Assert
        services.ShouldContain(s => s.ServiceType == typeof(ITenantEventHandler<TenantCreated>));
        services.ShouldContain(s => s.ServiceType == typeof(ITenantEventHandler<TenantUpdated>));
        services.ShouldContain(s => s.ServiceType == typeof(ITenantEventHandler<TenantDisabled>));
        services.ShouldContain(s => s.ServiceType == typeof(ITenantEventHandler<TenantEnabled>));
        services.ShouldContain(s => s.ServiceType == typeof(ITenantEventHandler<UserAddedToTenant>));
        services.ShouldContain(s => s.ServiceType == typeof(ITenantEventHandler<UserRemovedFromTenant>));
        services.ShouldContain(s => s.ServiceType == typeof(ITenantEventHandler<UserRoleChanged>));
        services.ShouldContain(s => s.ServiceType == typeof(ITenantEventHandler<TenantConfigurationSet>));
        services.ShouldContain(s => s.ServiceType == typeof(ITenantEventHandler<TenantConfigurationRemoved>));
    }

    [Fact]
    public void AddTenantEventHandler_ReturnsSameServiceCollection() {
        // Arrange
        IServiceCollection services = new ServiceCollection();

        // Act
        IServiceCollection result = services.AddTenantEventHandler<UserAddedToTenant, MultiEventHandler>();

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddTenantEventHandler_ThrowsOnNullServices() =>
        // Assert — must use static call syntax (extension method on null is invalid)
        Should.Throw<ArgumentNullException>(() =>
            TenantServiceCollectionExtensions.AddTenantEventHandler<UserAddedToTenant, MultiEventHandler>(null!));

    [Fact]
    public void AddTenantEventHandler_RegistersSelectedTypedHandler() {
        // Arrange
        IServiceCollection services = new ServiceCollection();

        // Act
        _ = services.AddTenantEventHandler<UserAddedToTenant, MultiEventHandler>();

        // Assert
        services.Count(s => s.ServiceType == typeof(MultiEventHandler)).ShouldBe(1);
        services.Count(s => s.ServiceType == typeof(ITenantEventHandler<UserAddedToTenant>)).ShouldBe(1);
        services.ShouldNotContain(s => s.ServiceType == typeof(ITenantEventHandler<TenantDisabled>));
    }

    [Fact]
    public void AddTenantEventHandler_SupportsOneHandlerForMultipleSelectedEvents() {
        // Arrange
        IServiceCollection services = new ServiceCollection();

        // Act
        _ = services
            .AddTenantEventHandler<UserAddedToTenant, MultiEventHandler>()
            .AddTenantEventHandler<TenantDisabled, MultiEventHandler>();

        // Assert
        services.Count(s => s.ServiceType == typeof(MultiEventHandler)).ShouldBe(1);
        services.Count(s => s.ServiceType == typeof(ITenantEventHandler<UserAddedToTenant>)).ShouldBe(1);
        services.Count(s => s.ServiceType == typeof(ITenantEventHandler<TenantDisabled>)).ShouldBe(1);
    }

    [Fact]
    public void AddTenantEventHandler_DuplicateRegistrationIsIdempotent() {
        // Arrange
        IServiceCollection services = new ServiceCollection();

        // Act
        _ = services
            .AddTenantEventHandler<UserAddedToTenant, MultiEventHandler>()
            .AddTenantEventHandler<UserAddedToTenant, MultiEventHandler>();

        // Assert
        services.Count(s => s.ServiceType == typeof(MultiEventHandler)).ShouldBe(1);
        services.Count(s => s.ServiceType == typeof(ITenantEventHandler<UserAddedToTenant>)).ShouldBe(1);
    }

    [Fact]
    public void AddHexalithTenants_InMemoryTenantProjectionStoreIsDefaultImplementation() {
        // Arrange
        IServiceCollection services = new ServiceCollection();

        // Act
        _ = services.AddHexalithTenants();

        // Assert
        ServiceDescriptor descriptor = GetRequiredDescriptor(services, typeof(ITenantProjectionStore));
        descriptor.ImplementationType.ShouldBe(typeof(InMemoryTenantProjectionStore));
    }

    [Fact]
    public void AddHexalithTenants_CustomProjectionStorePreventsDuplicateRegistration() {
        // Arrange — register custom store before AddHexalithTenants
        IServiceCollection services = new ServiceCollection();
        _ = services.AddSingleton<ITenantProjectionStore, InMemoryTenantProjectionStore>();

        // Act
        _ = services.AddHexalithTenants();

        // Assert — only one registration
        services.Count(s => s.ServiceType == typeof(ITenantProjectionStore)).ShouldBe(1);
        services.ShouldContain(s => s.ServiceType == typeof(TenantEventProcessor));
        services.ShouldContain(s => s.ServiceType == typeof(ITenantEventHandler<TenantCreated>));
    }

    [Fact]
    public void AddHexalithTenants_UsesSingleConfigurationSectionNameConstant() {
        // Arrange
        IServiceCollection services = CreateServiceCollectionWithConfig(
            new Dictionary<string, string?> {
                [$"{HexalithTenantsOptions.ConfigurationSectionName}:PubSubName"] = "sectionpubsub",
            });

        // Act
        _ = services.AddHexalithTenants();

        // Assert
        using ServiceProvider provider = services.BuildServiceProvider();
        HexalithTenantsOptions options = provider.GetRequiredService<IOptions<HexalithTenantsOptions>>().Value;
        options.PubSubName.ShouldBe("sectionpubsub");
    }

    [Fact]
    public void ClientProject_DoesNotReferenceServerHostOrAppHostProjects() {
        // Arrange
        XDocument project = XDocument.Load(ClientProjectPath());

        // Act
        string[] projectReferences = project
            .Descendants("ProjectReference")
            .Select(reference => reference.Attribute("Include")?.Value)
            .OfType<string>()
            .ToArray();

        // Assert
        projectReferences.ShouldNotContain(reference => reference.Contains("Hexalith.Tenants.Server", StringComparison.Ordinal));
        projectReferences.ShouldNotContain(reference => reference.Contains("Hexalith.Tenants.AppHost", StringComparison.Ordinal));
        projectReferences.ShouldNotContain(reference => reference.Contains(@"Hexalith.Tenants\Hexalith.Tenants.csproj", StringComparison.Ordinal));
        projectReferences.ShouldNotContain(reference => reference.Contains("Hexalith.Tenants/Hexalith.Tenants.csproj", StringComparison.Ordinal));
    }

    [Fact]
    public void ClientProject_DoesNotUseInlinePackageVersions() {
        // Arrange
        XDocument project = XDocument.Load(ClientProjectPath());

        // Act
        string[] packageReferencesWithVersions = project
            .Descendants("PackageReference")
            .Where(reference => reference.Attribute("Version") is not null)
            .Select(reference => reference.Attribute("Include")?.Value ?? reference.Attribute("Update")?.Value ?? "<unknown>")
            .ToArray();

        // Assert
        packageReferencesWithVersions.ShouldBeEmpty();
    }

    private static IServiceCollection CreateServiceCollectionWithConfig(
        Dictionary<string, string?>? configValues = null) {
        var services = new ServiceCollection();
        if (configValues is not null) {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configValues)
                .Build();
            _ = services.AddSingleton<IConfiguration>(configuration);
        }

        return services;
    }

    private static ServiceDescriptor GetRequiredDescriptor(IServiceCollection services, Type serviceType) =>
        services.FirstOrDefault(s => s.ServiceType == serviceType)
        ?? throw new ShouldAssertException($"Expected descriptor for service type '{serviceType}'.");

    private static string ClientProjectPath()
        => Path.Combine(FindRepoRoot(), "src", "Hexalith.Tenants.Client", "Hexalith.Tenants.Client.csproj");

    private static string FindRepoRoot() {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null) {
            if (File.Exists(Path.Combine(current.FullName, "Hexalith.Tenants.slnx"))) {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate Hexalith.Tenants repository root.");
    }

    private sealed class MultiEventHandler :
        ITenantEventHandler<UserAddedToTenant>,
        ITenantEventHandler<TenantDisabled> {
        public Task HandleAsync(UserAddedToTenant @event, TenantEventContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task HandleAsync(TenantDisabled @event, TenantEventContext context, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
