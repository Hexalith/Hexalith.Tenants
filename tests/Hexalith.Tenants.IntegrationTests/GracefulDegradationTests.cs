#pragma warning disable CA2007

using System.Text.Json;

using Dapr.Actors;
using Dapr.Actors.Client;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Server.Actors;

using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.IntegrationTests.Fixtures;

using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

/// <summary>
/// Tier 2 integration test for Story 7.3, AC #4 and AC #5.
/// Verifies that commands succeed and events are persisted even when pub/sub publication fails,
/// and that drain recovery publishes pending events when pub/sub recovers.
/// Requires: dapr init (Redis, Placement, Scheduler running).
/// </summary>
[Collection("TenantsDaprTest")]
[Trait("Category", "Integration")]
public class GracefulDegradationTests {
    private readonly TenantsDaprTestFixture _fixture;

    public GracefulDegradationTests(TenantsDaprTestFixture fixture) {
        _fixture = fixture;
    }

    [Fact]
    public async Task Command_Succeeds_AndEventsPersisted_WhenPubSubUnavailable() {
        // Arrange — configure FakeEventPublisher to simulate pub/sub outage
        _fixture.EventPublisher.SetupFailure("Pub/sub unavailable — simulated outage");

        try {
            var actorProxyFactory = new ActorProxyFactory(
                new ActorProxyOptions { HttpEndpoint = _fixture.DaprHttpEndpoint });

            string tenantId = $"t-degrade-{Guid.NewGuid():N}";
            CommandEnvelope command = CreateTenantCommand(
                new CreateTenant(tenantId, "Degradation Test Corp", "Graceful degradation verification"));

            IAggregateActor proxy = actorProxyFactory.CreateActorProxy<IAggregateActor>(
                new ActorId(command.AggregateIdentity.ActorId),
                nameof(AggregateActor));

            // Act — send command while pub/sub is "down"
            CommandProcessingResult result = await proxy.ProcessCommandAsync(command);

            // Assert — command is accepted and events are persisted even though publication failed.
            // The AggregateActor persists events atomically in the DAPR state store before publishing.
            // When publish fails, it transitions to PublishFailed state but the events are NOT lost.
            result.ShouldNotBeNull();
            result.Accepted.ShouldBeTrue(
                $"Command should be accepted even during pub/sub outage but got: {result.ErrorMessage}");
            result.EventCount.ShouldBe(1,
                "Event should be persisted in state store even when pub/sub publication fails");
        }
        finally {
            _fixture.EventPublisher.ClearFailure();
        }
    }

    [Fact]
    public async Task DrainRecovery_PublishesPendingEvents_WhenPubSubRecovers() {
        // Arrange — configure pub/sub outage
        _fixture.EventPublisher.SetupFailure("Pub/sub unavailable — drain recovery test");

        string tenantId = $"t-drain-{Guid.NewGuid():N}";
        string expectedTopic = "system.tenants.events";

        try {
            var actorProxyFactory = new ActorProxyFactory(
                new ActorProxyOptions { HttpEndpoint = _fixture.DaprHttpEndpoint });

            CommandEnvelope command = CreateTenantCommand(
                new CreateTenant(tenantId, "Drain Recovery Corp", "Drain recovery verification"));

            IAggregateActor proxy = actorProxyFactory.CreateActorProxy<IAggregateActor>(
                new ActorId(command.AggregateIdentity.ActorId),
                nameof(AggregateActor));

            // Record event count before this test
            int eventsBefore = _fixture.EventPublisher.GetEventsForTopic(expectedTopic).Count;

            // Act — send command during outage
            CommandProcessingResult result = await proxy.ProcessCommandAsync(command);
            result.Accepted.ShouldBeTrue("Command should succeed during pub/sub outage");
            result.EventCount.ShouldBe(1, "Event should be persisted");

            // Verify events were NOT published (pub/sub is down)
            int eventsAfterFailure = _fixture.EventPublisher.GetEventsForTopic(expectedTopic).Count;
            eventsAfterFailure.ShouldBe(eventsBefore,
                "No new events should be published to topic during pub/sub outage");

            // "Recover" pub/sub by resetting the failure state
            _fixture.EventPublisher.ClearFailure();

            // Wait for drain reminder to fire and publish pending events.
            // Default InitialDrainDelay is 30 seconds. We poll up to 90 seconds.
            bool drainSucceeded = false;
            for (int i = 0; i < 90; i++) {
                int eventsNow = _fixture.EventPublisher.GetEventsForTopic(expectedTopic).Count;
                if (eventsNow > eventsBefore) {
                    drainSucceeded = true;
                    break;
                }

                await Task.Delay(1000);
            }

            // Assert — drain recovery published the pending events
            drainSucceeded.ShouldBeTrue(
                "Drain recovery should publish pending events within 90 seconds after pub/sub recovery");
        }
        finally {
            _fixture.EventPublisher.ClearFailure();
        }
    }

    private static CommandEnvelope CreateTenantCommand<T>(T command) where T : notnull
        => new(
            Guid.NewGuid().ToString(),
            "system",
            "tenants",
            ((dynamic)command).TenantId,
            typeof(T).Name,
            JsonSerializer.SerializeToUtf8Bytes(command),
            Guid.NewGuid().ToString(),
            null,
            "test-user",
            null);
}
