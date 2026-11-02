#if HEXALITH_CHECKPOINTED_TENANT_PROJECTION
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Projections.Checkpointed;
using Hexalith.EventStore.Client.Subscriptions;
using Hexalith.EventStore.Testing.Fakes;
using Hexalith.Tenants.Client.Handlers;
using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Client.Registration;
using Hexalith.Tenants.Client.Tests.Projections;
using Hexalith.Tenants.Contracts.Enums;
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

        TenantProjectionTransition.RejectionEventTypes.Count.ShouldBe(10);
        TenantProjectionTransition.RejectionEventTypes.ShouldContain(typeof(TenantDisabledRejection));
        TenantProjectionTransition.RejectionEventTypes.ShouldContain(typeof(InsufficientPermissionsRejection));
        foreach (Type rejection in TenantProjectionTransition.RejectionEventTypes) {
            Type service = typeof(IEventStoreDomainEventHandler<>).MakeGenericType(rejection);
            services.Count(d => d.ServiceType == service).ShouldBe(1, rejection.Name);
            provider.GetServices(service).ShouldHaveSingleItem().ShouldNotBeNull();
        }

        // Rejections only the global-administrators aggregate emits never get a tenant checkpoint handler.
        foreach (Type rejection in new[] {
            typeof(GlobalAdminAlreadyBootstrappedRejection),
            typeof(GlobalAdministratorAlreadyExistsRejection),
            typeof(GlobalAdministratorNotFoundRejection),
            typeof(LastGlobalAdministratorRejection),
        }) {
            TenantProjectionTransition.RejectionEventTypes.ShouldNotContain(rejection);
            services.ShouldNotContain(d => d.ServiceType == typeof(IEventStoreDomainEventHandler<>).MakeGenericType(rejection), rejection.Name);
        }
    }

    [Fact]
    public void DefaultRegistrationHandlesNoRejection() {
        var services = new ServiceCollection();
        _ = services.AddHexalithTenants();

        services.ShouldNotContain(d => d.ServiceType == typeof(IEventStoreDomainEventHandler<TenantDisabledRejection>));
    }

    [Fact]
    public async Task RegisteredDurableStoreResolvesAGatewayClientPerHistoryRead() {
        int created = 0;
        var history = new TenantHistoryGateway();
        var services = new ServiceCollection();
        _ = services.AddLogging();
        _ = services.AddSingleton<IReadModelStore>(new InMemoryReadModelStore());
        _ = services.AddScoped<IEventStoreGatewayClient>(_ => {
            Interlocked.Increment(ref created);
            return history;
        });
        _ = services.AddHexalithTenantsDurableProjection("tenant-state");
        _ = services.AddHexalithTenants();
        using ServiceProvider provider = services.BuildServiceProvider();
        EventStoreDomainEventProcessor processor = provider.GetRequiredService<EventStoreDomainEventProcessor>();
        EventStoreDomainEventEnvelope first = DurableTenantProjectionHarness.Envelope(
            new TenantCreated("tenant-per-read", "Per read", null, DateTimeOffset.UnixEpoch), "tenant-per-read", 1);
        EventStoreDomainEventEnvelope late = DurableTenantProjectionHarness.Envelope(
            new TenantUpdated("tenant-per-read", "Renamed", null, DateTimeOffset.UnixEpoch), "tenant-per-read", 3);
        history.Append(first);
        history.Append(DurableTenantProjectionHarness.Envelope(
            new UserAddedToTenant("tenant-per-read", "user-1", TenantRole.TenantOwner), "tenant-per-read", 2));
        history.Append(late);

        created.ShouldBe(0, "constructing the singleton store resolves no gateway client");
        _ = provider.GetRequiredService<ITenantProjectionStore>();
        created.ShouldBe(0);
        (await processor.ProcessAsync(late, TestContext.Current.CancellationToken)).ShouldBe(EventStoreDomainEventProcessingResult.Processed);
        int afterRecovery = created;
        (await processor.ProcessAsync(first, TestContext.Current.CancellationToken)).ShouldBe(EventStoreDomainEventProcessingResult.Processed);

        afterRecovery.ShouldBe(1, "one gap recovery resolved one client");
        created.ShouldBe(2, "the older delivery's verification resolved a fresh client");
        history.Reads.ShouldBe(2);
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
#endif
