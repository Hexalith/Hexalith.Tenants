#if HEXALITH_CHECKPOINTED_TENANT_PROJECTION
using System.Text.Json;

using Hexalith.Commons.UniqueIds;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Projections.Checkpointed;
using Hexalith.EventStore.Client.Subscriptions;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Testing.Fakes;
using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Client.Registration;

using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Tenants.Client.Tests.Projections;

/// <summary>One shared read-model store and authoritative stream behind any number of consumer hosts.</summary>
internal sealed class DurableTenantProjectionHarness {
    public const string StoreName = "tenant-projection-test-state";

    public InMemoryReadModelStore Store { get; } = new();

    public TenantHistoryGateway Gateway { get; } = new();

    public static EventStoreDomainEventEnvelope Envelope<TEvent>(TEvent @event, string tenantId, long sequence, string? messageId = null)
        where TEvent : IEventPayload
        => new(
            messageId ?? UniqueIdHelper.GenerateSortableUniqueStringId(),
            tenantId,
            "system",
            @event.GetType().FullName!,
            sequence,
            new DateTimeOffset(2026, 11, 1, 10, 0, 0, TimeSpan.Zero).AddMinutes(sequence),
            UniqueIdHelper.GenerateSortableUniqueStringId(),
            "json",
            JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType())) {
            Domain = "tenants",
            UserId = "durable-consumer-test",
        };

    /// <summary>Creates one consumer host (a replica or a restarted process) over the shared persisted state.</summary>
    public ServiceProvider CreateHost(bool withHistory = true) {
        var services = new ServiceCollection();
        _ = services.AddLogging();
        _ = services.AddSingleton<IReadModelStore>(Store);
        if (withHistory) {
            _ = services.AddSingleton<IEventStoreGatewayClient>(Gateway);
        }

        _ = services.AddHexalithTenantsDurableProjection(StoreName);
        _ = services.AddHexalithTenants();
        return services.BuildServiceProvider();
    }

    /// <summary>Persists the event in authoritative history and returns its delivery envelope.</summary>
    public EventStoreDomainEventEnvelope Persist<TEvent>(TEvent @event, string tenantId, long sequence)
        where TEvent : IEventPayload {
        EventStoreDomainEventEnvelope envelope = Envelope(@event, tenantId, sequence);
        Gateway.Append(envelope);
        return envelope;
    }

    public CheckpointedProjectionRecord<TenantLocalState>? Record(string tenantId)
        => Store.Snapshot<CheckpointedProjectionRecord<TenantLocalState>>(StoreName, DurableTenantProjectionStore.KeyFor(tenantId));

    public static Task<EventStoreDomainEventProcessingResult> ProcessAsync(ServiceProvider host, EventStoreDomainEventEnvelope envelope)
        => host.GetRequiredService<EventStoreDomainEventProcessor>().ProcessAsync(envelope, TestContext.Current.CancellationToken);
}
#endif
