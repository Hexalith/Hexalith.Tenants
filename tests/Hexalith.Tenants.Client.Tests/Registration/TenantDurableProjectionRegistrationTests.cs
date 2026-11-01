using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Projections.Checkpointed;
using Hexalith.EventStore.Client.Subscriptions;
using Hexalith.EventStore.Testing.Fakes;
using Hexalith.Tenants.Client.Handlers;
using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Client.Registration;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Events.Rejections;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.Tenants.Client.Tests.Registration;

/// <summary>Proves the durable projection is opt-in and that the default registration is unchanged.</summary>
public sealed class TenantDurableProjectionRegistrationTests {
    [Fact]
    public void DefaultRegistrationKeepsTheInMemoryStoreAndNoSnapshotStore() {
        var services = new ServiceCollection();
        _ = services.AddLogging();
        _ = services.AddHexalithTenants();
        using ServiceProvider provider = services.BuildServiceProvider();

        _ = provider.GetRequiredService<ITenantProjectionStore>().ShouldBeOfType<InMemoryTenantProjectionStore>();
        provider.GetService<ITenantProjectionSnapshotStore>().ShouldBeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DurableOptInRegistersOneSharedSnapshotStoreInEitherOrder(bool optInFirst) {
        var services = new ServiceCollection();
        _ = services.AddLogging();
        _ = services.AddSingleton<IReadModelStore>(new InMemoryReadModelStore());
        if (optInFirst) {
            _ = services.AddHexalithTenantsDurableProjection("tenant-state");
            _ = services.AddHexalithTenants();
        }
        else {
            _ = services.AddHexalithTenants();
            _ = services.AddHexalithTenantsDurableProjection("tenant-state");
        }

        using ServiceProvider provider = services.BuildServiceProvider();

        DurableTenantProjectionStore store = provider.GetRequiredService<ITenantProjectionStore>().ShouldBeOfType<DurableTenantProjectionStore>();
        provider.GetRequiredService<ITenantProjectionSnapshotStore>().ShouldBeSameAs(store);
        provider.GetRequiredService<DurableTenantProjectionOptions>().StateStoreName.ShouldBe("tenant-state");
        services.Count(static d => d.ServiceType == typeof(ITenantProjectionStore)).ShouldBe(1);
    }

    [Fact]
    public void DurableOptInRegistersACheckpointHandlerForEveryRejectionOnlyOnce() {
        var services = new ServiceCollection();
        _ = services.AddLogging();
        _ = services.AddSingleton<IReadModelStore>(new InMemoryReadModelStore());
        _ = services.AddHexalithTenantsDurableProjection("tenant-state");
        _ = services.AddHexalithTenantsDurableProjection("tenant-state");
        _ = services.AddHexalithTenants();
        using ServiceProvider provider = services.BuildServiceProvider();

        TenantProjectionTransition.RejectionEventTypes.Count.ShouldBe(14);
        TenantProjectionTransition.RejectionEventTypes.ShouldContain(typeof(TenantDisabledRejection));
        foreach (Type rejection in TenantProjectionTransition.RejectionEventTypes) {
            Type service = typeof(IEventStoreDomainEventHandler<>).MakeGenericType(rejection);
            services.Count(d => d.ServiceType == service).ShouldBe(1, rejection.Name);
            provider.GetServices(service).ShouldHaveSingleItem().ShouldNotBeNull();
        }
    }

    [Fact]
    public void DefaultRegistrationHandlesNoRejection() {
        var services = new ServiceCollection();
        _ = services.AddHexalithTenants();

        services.ShouldNotContain(d => d.ServiceType == typeof(IEventStoreDomainEventHandler<TenantDisabledRejection>));
    }

    [Fact]
    public void DurableOptInRequiresAStateStoreName() {
        var services = new ServiceCollection();

        _ = Should.Throw<ArgumentException>(() => services.AddHexalithTenantsDurableProjection(" "));
    }

    [Fact]
    public async Task HandlerRoutesThroughTheDurableSnapshotStoreWhenRegistered() {
        var readModels = new InMemoryReadModelStore();
        var store = new DurableTenantProjectionStore(readModels, new DurableTenantProjectionOptions("tenant-state"));
        var handler = new TenantProjectionEventHandler(store);
        var context = new EventStoreDomainEventContext("system", "tenant-route", "message-1", 1, DateTimeOffset.UnixEpoch, "correlation-1") {
            Domain = "tenants",
            EventType = typeof(TenantCreated).FullName,
            PayloadFingerprint = EventStorePayloadFingerprint.Compute("{}"u8),
        };

        await handler.HandleAsync(new TenantCreated("tenant-route", "Routed", null, DateTimeOffset.UnixEpoch), context, TestContext.Current.CancellationToken);

        CheckpointedProjectionRecord<TenantLocalState> record = readModels
            .Snapshot<CheckpointedProjectionRecord<TenantLocalState>>("tenant-state", DurableTenantProjectionStore.KeyFor("tenant-route"))
            .ShouldNotBeNull();
        record.Health.ShouldBe(CheckpointedProjectionHealth.Healthy);
        record.LastContiguousSequence.ShouldBe(1);
        record.State.ShouldNotBeNull().Name.ShouldBe("Routed");
    }

    [Fact]
    public async Task HandlerRefusesADeliveryWithoutEventIdentity() {
        var store = new DurableTenantProjectionStore(new InMemoryReadModelStore(), new DurableTenantProjectionOptions("tenant-state"));
        var handler = new TenantProjectionEventHandler(store);
        var context = new EventStoreDomainEventContext("system", "tenant-route", "message-1", 1, DateTimeOffset.UnixEpoch, "correlation-1");

        _ = await Should.ThrowAsync<ArgumentException>(() => handler.HandleAsync(
            new TenantCreated("tenant-route", "Routed", null, DateTimeOffset.UnixEpoch),
            context,
            TestContext.Current.CancellationToken));
    }
}
