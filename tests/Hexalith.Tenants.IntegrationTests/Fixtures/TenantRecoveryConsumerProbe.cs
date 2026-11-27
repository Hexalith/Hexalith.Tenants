#if HEXALITH_CHECKPOINTED_TENANT_PROJECTION
using System.Text.Json;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Subscriptions;
using Hexalith.EventStore.Testing.Fakes;
using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Client.Registration;
using Hexalith.Tenants.Server.Projections;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using StackExchange.Redis;

namespace Hexalith.Tenants.IntegrationTests.Fixtures;

/// <summary>Runs the registered durable Tenants consumer against live Dapr publications in the fixture's broker.</summary>
internal sealed class TenantRecoveryConsumerProbe(
    IDatabase database, RedisValue startingPosition, ServiceProvider consumer) : IDisposable
{
    private RedisValue _position = startingPosition;
    private long _sequence;

    /// <summary>Captures the broker offset before the unique tenant is seeded and composes its isolated consumer.</summary>
    internal static async Task<TenantRecoveryConsumerProbe> CreateAsync(IDatabase database, CancellationToken cancellationToken)
    {
        StreamEntry[] latest = await database.StreamRangeAsync(TenantServiceCollectionExtensions.TopicName,
            count: 1, messageOrder: Order.Descending).WaitAsync(cancellationToken);
        RedisValue startingPosition = latest.Length == 0 ? (RedisValue)"0-0" : latest[0].Id;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IReadModelStore>(new InMemoryReadModelStore());
        services.AddHexalithTenantsDurableProjection("recovery-consumer");
        services.AddHexalithTenants();
        return new TenantRecoveryConsumerProbe(database, startingPosition, services.BuildServiceProvider());
    }

    /// <summary>Consumes the exact tenant prefix from the live topic and returns its terminal delivery.</summary>
    internal async Task<EventStoreDomainEventEnvelope> ConsumeThroughAsync(
        string tenantId, long sequence, CancellationToken cancellationToken)
    {
        sequence.ShouldBeGreaterThan(_sequence);
        var processor = consumer.GetRequiredService<EventStoreDomainEventProcessor>();
        while (true)
        {
            StreamEntry[] entries = await database.StreamReadAsync(TenantServiceCollectionExtensions.TopicName,
                _position, count: 64).WaitAsync(cancellationToken);
            foreach (StreamEntry entry in entries)
            {
                _position = entry.Id;
                string publication = entry["data"].ToString();
                if (!publication.Contains(tenantId, StringComparison.Ordinal))
                {
                    continue;
                }

                using JsonDocument cloudEvent = JsonDocument.Parse(publication);
                var delivery = cloudEvent.RootElement.GetProperty("data")
                    .Deserialize<EventStoreDomainEventEnvelope>(JsonSerializerOptions.Web).ShouldNotBeNull();
                if (delivery.AggregateId != tenantId || delivery.Domain != "tenants")
                {
                    continue;
                }

                if (delivery.SequenceNumber <= _sequence)
                {
                    (await processor.ProcessAsync(delivery, cancellationToken))
                        .ShouldBe(EventStoreDomainEventProcessingResult.Duplicate);
                    continue;
                }

                delivery.SequenceNumber.ShouldBe(_sequence + 1);
                (await processor.ProcessAsync(delivery, cancellationToken))
                    .ShouldBe(EventStoreDomainEventProcessingResult.Processed);
                _sequence = delivery.SequenceNumber;
                if (_sequence == sequence)
                {
                    return delivery;
                }
            }

            if (entries.Length == 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            }
        }
    }

    /// <summary>Reads the checkpointed record through the consumer's registered snapshot API.</summary>
    internal async Task AssertProjectionAsync(TenantReadModel expected, long sequence, CancellationToken cancellationToken)
    {
        var snapshot = (await consumer.GetRequiredService<ITenantProjectionSnapshotStore>()
            .GetSnapshotAsync(expected.TenantId, cancellationToken)).ShouldNotBeNull();
        snapshot.IsHealthy.ShouldBeTrue();
        snapshot.Checkpoint.ShouldBe(sequence);
        TenantLocalState actual = snapshot.State.ShouldNotBeNull();
        actual.LastEvent.ShouldNotBeNull().LastSequenceNumber.ShouldBe(sequence);
        actual.Name.ShouldBe(expected.Name);
        actual.Description.ShouldBe(expected.Description);
        actual.Status.ShouldBe(expected.Status);
        actual.Members.OrderBy(item => item.Key, StringComparer.Ordinal)
            .ShouldBe(expected.Members.OrderBy(item => item.Key, StringComparer.Ordinal));
        actual.Configuration.OrderBy(item => item.Key, StringComparer.Ordinal)
            .ShouldBe(expected.Configuration.OrderBy(item => item.Key, StringComparer.Ordinal));
    }

    /// <inheritdoc/>
    public void Dispose() => consumer.Dispose();
}
#endif
