using System.Text.Json;

using Dapr.Client;

using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Projections;
using Hexalith.Tenants.Server.Projections;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Projections;

public class TenantProjectionHandlerTests {
    [Fact]
    public async Task ProjectAsync_WritesTenantAuditStateAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        _ = daprClient.GetStateAsync<TenantIndexReadModel>("statestore", "projection:tenant-index:singleton")
            .Returns(Task.FromResult<TenantIndexReadModel>(null!)!);
        DateTimeOffset timestamp = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);
        ProjectionRequest request = new(
            "tenant-1",
            "tenants",
            "tenant-1",
            [CreateEvent(new TenantCreated("tenant-1", "Acme", null, timestamp), "evt-1", timestamp)]);

        _ = await new TenantProjectionHandler(daprClient).ProjectAsync(request);

        await daprClient.Received(1).SaveStateAsync(
            "statestore",
            "audit:tenant-1",
            Arg.Is<TenantAuditReadModel>(m =>
                m != null
                && m.Entries.Count == 1
                && m.Entries[0].EventId == "evt-1"
                && m.Entries[0].ActorId == "actor-1"),
            Arg.Any<StateOptions>(),
            Arg.Any<IReadOnlyDictionary<string, string>>(),
            Arg.Any<CancellationToken>());
    }

    private static ProjectionEventDto CreateEvent(IEventPayload payload, string messageId, DateTimeOffset timestamp) =>
        new(
            payload.GetType().FullName!,
            JsonSerializer.SerializeToUtf8Bytes(payload, payload.GetType()),
            "json",
            1,
            timestamp,
            "corr-1",
            messageId,
            "actor-1");
}
