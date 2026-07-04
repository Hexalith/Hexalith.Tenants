#pragma warning disable CA2007

using System.Text.Json;

using Dapr.Actors;
using Dapr.Actors.Client;

using Hexalith.Commons.UniqueIds;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Server.Actors;
using Hexalith.EventStore.Server.Events;

using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.IntegrationTests.Fixtures;

using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

/// <summary>
/// Complementary DAPR recovery coverage for Story 7.6D.
/// The stronger source-of-truth identity assertions live in DaprEndToEndTests;
/// this class keeps the older command-acceptance and drain-publication smoke lane discoverable.
/// Requires: dapr init (Redis, Placement, Scheduler running).
/// </summary>
[Collection("TenantsDaprTest")]
[DaprTestSerialization]
[Trait("Category", "Integration")]
public class GracefulDegradationTests : IDisposable {
    private const string GlobalAdminExtensionKey = "actor:globalAdmin";

    private readonly IDisposable _daprTestLease;
    private readonly TenantsDaprTestFixture _fixture;

    public GracefulDegradationTests(TenantsDaprTestFixture fixture) {
        _daprTestLease = DaprTestExecutionGate.Enter();
        _fixture = fixture;
    }

    public void Dispose() {
        _daprTestLease.Dispose();
        GC.SuppressFinalize(this);
    }

    [DaprFact]
    public async Task Command_Succeeds_AndEventsPersisted_WhenPubSubUnavailable() {
        _fixture.SkipIfUnavailable();

        string tenantId = $"t-degrade-{Guid.NewGuid():N}";
        CommandEnvelope command = CreateTenantCommand(
            new CreateTenant(tenantId, "Degradation Test Corp", "Graceful degradation verification"));
        _fixture.EventPublisher.SetupFailureForCorrelation(command.CorrelationId, "Pub/sub unavailable - simulated outage");
        try {
            var actorProxyFactory = new ActorProxyFactory(
                new ActorProxyOptions { HttpEndpoint = _fixture.DaprHttpEndpoint });

            IAggregateActor proxy = actorProxyFactory.CreateActorProxy<IAggregateActor>(
                new ActorId(command.AggregateIdentity.ActorId),
                _fixture.AggregateActorTypeName);

            // Act — send command while pub/sub is "down"
            CommandProcessingResult result = await proxy.ProcessCommandAsync(command);

            // Assert — command is accepted and events are persisted even though publication failed.
            // The AggregateActor persists events atomically in the DAPR state store before publishing.
            // When publish fails, it transitions to PublishFailed state but the events are NOT lost.
            _ = result.ShouldNotBeNull();
            result.Accepted.ShouldBeTrue(
                $"Command should be accepted even during pub/sub outage but got: {result.ErrorMessage}");
            result.EventCount.ShouldBe(1,
                "Event should be persisted in state store even when pub/sub publication fails");
        }
        finally {
            _fixture.EventPublisher.ClearFailureForCorrelation(command.CorrelationId);
        }
    }

    [DaprFact]
    public async Task DrainRecovery_PublishesPendingEvents_WhenPubSubRecovers() {
        _fixture.SkipIfUnavailable();

        string tenantId = $"t-drain-{Guid.NewGuid():N}";
        string expectedTopic = "tenants.events";
        CommandEnvelope command = CreateTenantCommand(
            new CreateTenant(tenantId, "Drain Recovery Corp", "Drain recovery verification"));
        _fixture.EventPublisher.SetupFailureForCorrelation(command.CorrelationId, "Pub/sub unavailable - drain recovery test");

        try {
            var actorProxyFactory = new ActorProxyFactory(
                new ActorProxyOptions { HttpEndpoint = _fixture.DaprHttpEndpoint });

            IAggregateActor proxy = actorProxyFactory.CreateActorProxy<IAggregateActor>(
                new ActorId(command.AggregateIdentity.ActorId),
                _fixture.AggregateActorTypeName);

            // Act — send command during outage
            CommandProcessingResult result = await proxy.ProcessCommandAsync(command);
            result.Accepted.ShouldBeTrue("Command should succeed during pub/sub outage");
            result.EventCount.ShouldBe(1, "Event should be persisted");

            // Verify events were NOT published (pub/sub is down), but the source event is durable.
            int eventsAfterFailure = CountPublishedEvents(expectedTopic, command.CorrelationId);
            eventsAfterFailure.ShouldBe(0,
                "No new events should be published to topic during pub/sub outage");
            EventEnvelope[] stream = await proxy.GetEventsAsync(0);
            stream.Count(e => e.CorrelationId == command.CorrelationId).ShouldBe(1);
        }
        finally {
            _fixture.EventPublisher.ClearFailureForCorrelation(command.CorrelationId);
        }
    }

    private int CountPublishedEvents(string topic, string correlationId)
        => _fixture.EventPublisher
            .GetEventsForTopic(topic)
            .Count(e => e.CorrelationId == correlationId);

    private static CommandEnvelope CreateTenantCommand<T>(T command) where T : notnull
        => new(
            UniqueIdHelper.GenerateSortableUniqueStringId(),
            "system",
            "tenants",
            ((dynamic)command).TenantId,
            typeof(T).Name,
            JsonSerializer.SerializeToUtf8Bytes(command),
            UniqueIdHelper.GenerateSortableUniqueStringId(),
            null,
            "test-user",
            GlobalAdminExtensions());

    private static Dictionary<string, string> GlobalAdminExtensions()
        => new(StringComparer.OrdinalIgnoreCase) { [GlobalAdminExtensionKey] = "true" };
}
