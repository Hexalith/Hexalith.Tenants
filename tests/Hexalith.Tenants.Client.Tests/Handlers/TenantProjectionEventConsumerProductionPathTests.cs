using System.Text.Json;

using Hexalith.Commons.UniqueIds;
using Hexalith.EventStore.Client.Subscriptions;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Client.Registration;
using Hexalith.Tenants.Contracts.Events;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.Tenants.Client.Tests.Handlers;

/// <summary>Proves tenant projection behavior through the production consumer composition.</summary>
public sealed class TenantProjectionEventConsumerProductionPathTests {
    [Fact]
    public async Task Processor_DeduplicatesProtectsSequenceAndIsolatesTenantState() {
        var store = new InMemoryTenantProjectionStore();
        using ServiceProvider provider = CreateProvider(store);
        EventStoreDomainEventProcessor processor = provider.GetRequiredService<EventStoreDomainEventProcessor>();
        DateTimeOffset timestamp = new(2026, 7, 16, 11, 0, 0, TimeSpan.Zero);
        EventStoreDomainEventEnvelope current = CreateEnvelope(
            new TenantCreated("tenant-a", "Current Name", "current", timestamp),
            "tenant-a",
            2,
            timestamp);
        EventStoreDomainEventEnvelope stale = CreateEnvelope(
            new TenantUpdated("tenant-a", "Stale Name", "stale", timestamp.AddMinutes(-1)),
            "tenant-a",
            1,
            timestamp.AddMinutes(-1));
        EventStoreDomainEventEnvelope otherTenant = CreateEnvelope(
            new TenantCreated("tenant-b", "Other Tenant", null, timestamp),
            "tenant-b",
            1,
            timestamp);

        (await processor.ProcessAsync(current).ConfigureAwait(true))
            .ShouldBe(EventStoreDomainEventProcessingResult.Processed);
        (await processor.ProcessAsync(stale).ConfigureAwait(true))
            .ShouldBe(EventStoreDomainEventProcessingResult.Processed);
        (await processor.ProcessAsync(otherTenant).ConfigureAwait(true))
            .ShouldBe(EventStoreDomainEventProcessingResult.Processed);
        (await processor.ProcessAsync(current).ConfigureAwait(true))
            .ShouldBe(EventStoreDomainEventProcessingResult.Duplicate);

        TenantLocalState? tenantA = await store.GetAsync("tenant-a").ConfigureAwait(true);
        TenantLocalState? tenantB = await store.GetAsync("tenant-b").ConfigureAwait(true);
        tenantA.ShouldNotBeNull().Name.ShouldBe("Current Name");
        tenantA.Description.ShouldBe("current");
        tenantA.LastEvent.ShouldNotBeNull().LastSequenceNumber.ShouldBe(2);
        tenantB.ShouldNotBeNull().Name.ShouldBe("Other Tenant");
        tenantB.LastEvent.ShouldNotBeNull().LastSequenceNumber.ShouldBe(1);
    }

    [Fact]
    public async Task Processor_ReleasesMarkerAfterSaveFailureAndCompletesOnlyAfterRetryPersists() {
        var store = new FailOnceTenantProjectionStore();
        using ServiceProvider provider = CreateProvider(store);
        EventStoreDomainEventProcessor processor = provider.GetRequiredService<EventStoreDomainEventProcessor>();
        DateTimeOffset timestamp = new(2026, 7, 16, 11, 30, 0, TimeSpan.Zero);
        EventStoreDomainEventEnvelope envelope = CreateEnvelope(
            new TenantCreated("tenant-retry", "Recovered Tenant", null, timestamp),
            "tenant-retry",
            1,
            timestamp);

        _ = await Should.ThrowAsync<InvalidOperationException>(
            () => processor.ProcessAsync(envelope)).ConfigureAwait(true);
        (await processor.ProcessAsync(envelope).ConfigureAwait(true))
            .ShouldBe(EventStoreDomainEventProcessingResult.Processed);
        (await processor.ProcessAsync(envelope).ConfigureAwait(true))
            .ShouldBe(EventStoreDomainEventProcessingResult.Duplicate);

        store.SaveAttempts.ShouldBe(2);
        TenantLocalState? state = await store.GetAsync("tenant-retry").ConfigureAwait(true);
        state.ShouldNotBeNull().Name.ShouldBe("Recovered Tenant");
        state.LastEvent.ShouldNotBeNull().LastSequenceNumber.ShouldBe(1);
    }

    private static ServiceProvider CreateProvider(ITenantProjectionStore store) {
        var services = new ServiceCollection();
        _ = services.AddLogging();
        _ = services.AddSingleton(store);
        _ = services.AddHexalithTenants();
        return services.BuildServiceProvider();
    }

    private static EventStoreDomainEventEnvelope CreateEnvelope<TEvent>(
        TEvent @event,
        string tenantId,
        long sequenceNumber,
        DateTimeOffset timestamp)
        where TEvent : IEventPayload
        => new(
            UniqueIdHelper.GenerateSortableUniqueStringId(),
            tenantId,
            "system",
            @event.GetType().FullName!,
            sequenceNumber,
            timestamp,
            UniqueIdHelper.GenerateSortableUniqueStringId(),
            "json",
            JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType())) {
            Domain = "tenants",
            UserId = "consumer-test-user",
        };
}
