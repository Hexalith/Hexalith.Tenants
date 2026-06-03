using System.Xml.Linq;

using Dapr.Client;

using Hexalith.EventStore.Client.Subscriptions;
using Hexalith.Tenants.Client.Handlers;
using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Client.Registration;
using Hexalith.Tenants.Contracts.Events;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Shouldly;

namespace Hexalith.Tenants.Client.Tests.Registration;

/// <summary>
/// Verifies the domain-centric composition root: <see cref="TenantServiceCollectionExtensions"/> wires the
/// tenant consumer onto the platform A3 generics (the deduplicating processor + options live in the
/// EventStore client SDK; only the tenant local projection and store are domain-specific here).
/// </summary>
public class TenantServiceCollectionExtensionsTests {
    [Fact]
    public void AddHexalithTenants_RegistersDaprClient() {
        IServiceCollection services = new ServiceCollection();

        _ = services.AddHexalithTenants();

        // Descriptor check only, DO NOT resolve (gRPC needs the DAPR sidecar).
        services.ShouldContain(s => s.ServiceType == typeof(DaprClient));
        GetRequiredDescriptor(services, typeof(DaprClient)).Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddHexalithTenants_SkipsDaprClientIfAlreadyRegistered() {
        IServiceCollection services = new ServiceCollection();
        services.AddDaprClient();
        int daprCountBefore = services.Count(s => s.ServiceType == typeof(DaprClient));

        _ = services.AddHexalithTenants();

        services.Count(s => s.ServiceType == typeof(DaprClient)).ShouldBe(daprCountBefore);
    }

    [Fact]
    public void AddHexalithTenants_ReturnsSameServiceCollection() {
        IServiceCollection services = new ServiceCollection();

        IServiceCollection result = services.AddHexalithTenants();

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddHexalithTenants_WithAction_ReturnsSameServiceCollection() {
        IServiceCollection services = new ServiceCollection();

        IServiceCollection result = services.AddHexalithTenants(_ => { });

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddHexalithTenants_ThrowsOnNullServices() =>
        Should.Throw<ArgumentNullException>(() =>
            TenantServiceCollectionExtensions.AddHexalithTenants(null!));

    [Fact]
    public void AddHexalithTenants_WithAction_ThrowsOnNullServices() =>
        Should.Throw<ArgumentNullException>(() =>
            TenantServiceCollectionExtensions.AddHexalithTenants(null!, _ => { }));

    [Fact]
    public void AddHexalithTenants_WithAction_ThrowsOnNullAction() {
        IServiceCollection services = new ServiceCollection();

        _ = Should.Throw<ArgumentNullException>(() =>
            TenantServiceCollectionExtensions.AddHexalithTenants(services, null!));
    }

    [Fact]
    public void AddHexalithTenants_ConfiguresTenantSubscriptionDefaults() {
        IServiceCollection services = new ServiceCollection();

        _ = services.AddHexalithTenants();

        using ServiceProvider provider = services.BuildServiceProvider();
        EventStoreDomainEventsOptions options = provider
            .GetRequiredService<IOptions<EventStoreDomainEventsOptions>>().Value;
        options.PubSubName.ShouldBe("pubsub");
        options.TopicName.ShouldBe("tenants.events");
        options.SubscriptionRoute.ShouldBe("/tenants/events");
        // The tenant event payload's TenantId must equal the envelope aggregate ID (the managed tenant ID).
        options.PayloadAggregateIdPropertyName.ShouldBe("TenantId");
    }

    [Fact]
    public void AddHexalithTenants_WithAction_OverridesOptionsAfterDefaults() {
        IServiceCollection services = new ServiceCollection();

        _ = services.AddHexalithTenants(options => options.PubSubName = "custom");

        using ServiceProvider provider = services.BuildServiceProvider();
        EventStoreDomainEventsOptions options = provider
            .GetRequiredService<IOptions<EventStoreDomainEventsOptions>>().Value;
        options.PubSubName.ShouldBe("custom");
        // Tenant topic/route still apply because the override runs after the tenant defaults.
        options.TopicName.ShouldBe("tenants.events");
        options.SubscriptionRoute.ShouldBe("/tenants/events");
    }

    [Fact]
    public void AddHexalithTenants_RegistersPlatformEventProcessor() {
        IServiceCollection services = new ServiceCollection();

        _ = services.AddHexalithTenants();

        services.ShouldContain(s => s.ServiceType == typeof(EventStoreDomainEventProcessor));
    }

    [Fact]
    public void AddHexalithTenants_RegistersProjectionHandlerForEveryTenantEvent() {
        IServiceCollection services = new ServiceCollection();

        _ = services.AddHexalithTenants();

        services.ShouldContain(s => s.ServiceType == typeof(IEventStoreDomainEventHandler<TenantCreated>));
        services.ShouldContain(s => s.ServiceType == typeof(IEventStoreDomainEventHandler<TenantUpdated>));
        services.ShouldContain(s => s.ServiceType == typeof(IEventStoreDomainEventHandler<TenantDisabled>));
        services.ShouldContain(s => s.ServiceType == typeof(IEventStoreDomainEventHandler<TenantEnabled>));
        services.ShouldContain(s => s.ServiceType == typeof(IEventStoreDomainEventHandler<UserAddedToTenant>));
        services.ShouldContain(s => s.ServiceType == typeof(IEventStoreDomainEventHandler<UserRemovedFromTenant>));
        services.ShouldContain(s => s.ServiceType == typeof(IEventStoreDomainEventHandler<UserRoleChanged>));
        services.ShouldContain(s => s.ServiceType == typeof(IEventStoreDomainEventHandler<TenantConfigurationSet>));
        services.ShouldContain(s => s.ServiceType == typeof(IEventStoreDomainEventHandler<TenantConfigurationRemoved>));
    }

    [Fact]
    public void AddHexalithTenants_ProjectionHandlerIsSharedSingleton() {
        IServiceCollection services = new ServiceCollection();

        _ = services.AddHexalithTenants();

        using ServiceProvider provider = services.BuildServiceProvider();
        // The singleton handler keeps its per-tenant write locks shared across every consumed event type.
        var created = provider.GetRequiredService<IEventStoreDomainEventHandler<TenantCreated>>();
        var disabled = provider.GetRequiredService<IEventStoreDomainEventHandler<TenantDisabled>>();
        created.ShouldBeOfType<TenantProjectionEventHandler>();
        created.ShouldBeSameAs(disabled);
    }

    [Fact]
    public void AddHexalithTenants_IsIdempotent() {
        IServiceCollection services = new ServiceCollection();

        _ = services.AddHexalithTenants();
        _ = services.AddHexalithTenants();

        services.Count(s => s.ServiceType == typeof(DaprClient)).ShouldBe(1);
        services.Count(s => s.ServiceType == typeof(EventStoreDomainEventProcessor)).ShouldBe(1);
        services.Count(s => s.ServiceType == typeof(ITenantProjectionStore)).ShouldBe(1);
        services.Count(s => s.ServiceType == typeof(IEventStoreDomainEventHandler<TenantCreated>)).ShouldBe(1);
    }

    [Fact]
    public void AddHexalithTenants_RegistersInMemoryProjectionStoreByDefault() {
        IServiceCollection services = new ServiceCollection();

        _ = services.AddHexalithTenants();

        services.ShouldContain(s => s.ServiceType == typeof(ITenantProjectionStore));
        GetRequiredDescriptor(services, typeof(ITenantProjectionStore))
            .ImplementationType.ShouldBe(typeof(InMemoryTenantProjectionStore));
    }

    [Fact]
    public void AddHexalithTenants_CustomProjectionStorePreventsDuplicateRegistration() {
        IServiceCollection services = new ServiceCollection();
        _ = services.AddSingleton<ITenantProjectionStore, CustomTenantProjectionStore>();

        _ = services.AddHexalithTenants();

        services.Count(s => s.ServiceType == typeof(ITenantProjectionStore)).ShouldBe(1);
        GetRequiredDescriptor(services, typeof(ITenantProjectionStore))
            .ImplementationType.ShouldBe(typeof(CustomTenantProjectionStore));
        services.ShouldContain(s => s.ServiceType == typeof(EventStoreDomainEventProcessor));
        services.ShouldContain(s => s.ServiceType == typeof(IEventStoreDomainEventHandler<TenantCreated>));
    }

    [Fact]
    public void ClientProject_DoesNotReferenceServerHostOrAppHostProjects() {
        XDocument project = XDocument.Load(ClientProjectPath());

        string[] projectReferences = project
            .Descendants("ProjectReference")
            .Select(reference => reference.Attribute("Include")?.Value)
            .OfType<string>()
            .ToArray();

        projectReferences.ShouldNotContain(reference => reference.Contains("Hexalith.Tenants.Server", StringComparison.Ordinal));
        projectReferences.ShouldNotContain(reference => reference.Contains("Hexalith.Tenants.AppHost", StringComparison.Ordinal));
        projectReferences.ShouldNotContain(reference => reference.Contains(@"Hexalith.Tenants\Hexalith.Tenants.csproj", StringComparison.Ordinal));
        projectReferences.ShouldNotContain(reference => reference.Contains("Hexalith.Tenants/Hexalith.Tenants.csproj", StringComparison.Ordinal));
    }

    [Fact]
    public void ClientProject_DoesNotUseInlinePackageVersions() {
        XDocument project = XDocument.Load(ClientProjectPath());

        string[] packageReferencesWithVersions = project
            .Descendants("PackageReference")
            .Where(reference => reference.Attribute("Version") is not null)
            .Select(reference => reference.Attribute("Include")?.Value ?? reference.Attribute("Update")?.Value ?? "<unknown>")
            .ToArray();

        packageReferencesWithVersions.ShouldBeEmpty();
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

    private sealed class CustomTenantProjectionStore : ITenantProjectionStore {
        public Task<TenantLocalState?> GetAsync(string tenantId, CancellationToken cancellationToken = default) =>
            Task.FromResult<TenantLocalState?>(null);

        public Task SaveAsync(TenantLocalState state, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
