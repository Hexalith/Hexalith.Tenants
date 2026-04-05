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
/// Tier 2 integration test for Story 7.3, AC #1.
/// Verifies that tenant state is reconstructed from the event store after actor deactivation/reactivation,
/// proving the service is stateless with no data loss.
/// Requires: dapr init (Redis, Placement, Scheduler running).
/// </summary>
[Collection("TenantsDaprTest")]
[Trait("Category", "Integration")]
public class StatelessRestartTests {
    private readonly TenantsDaprTestFixture _fixture;

    public StatelessRestartTests(TenantsDaprTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task TenantState_IsReconstructedFromEventStore_AfterActorReactivation() {
        // Arrange — create a tenant
        var actorProxyFactory = new ActorProxyFactory(
            new ActorProxyOptions { HttpEndpoint = _fixture.DaprHttpEndpoint });

        string tenantId = $"t-restart-{Guid.NewGuid():N}";

        CommandEnvelope createCmd = CreateTenantCommand(
            new CreateTenant(tenantId, "Restart Test Corp", "Stateless restart verification"));

        IAggregateActor proxy1 = actorProxyFactory.CreateActorProxy<IAggregateActor>(
            new ActorId(createCmd.AggregateIdentity.ActorId),
            nameof(AggregateActor));

        CommandProcessingResult createResult = await proxy1.ProcessCommandAsync(createCmd);
        createResult.Accepted.ShouldBeTrue("Setup: CreateTenant must succeed");
        createResult.EventCount.ShouldBe(1, "Setup: CreateTenant should produce 1 event");

        // Act — force actor deactivation by calling DELETE on the app's actor endpoint.
        // This mimics what the DAPR runtime does during idle timeout, removing the actor from
        // the in-memory actor manager. The next proxy call will trigger OnActivateAsync() and
        // reload state from the event store.
        await DeactivateActorAsync(createCmd.AggregateIdentity.ActorId);

        IAggregateActor proxy2 = actorProxyFactory.CreateActorProxy<IAggregateActor>(
            new ActorId(createCmd.AggregateIdentity.ActorId),
            nameof(AggregateActor));

        CommandEnvelope disableCmd = CreateTenantCommand(new DisableTenant(tenantId));
        CommandProcessingResult disableResult = await proxy2.ProcessCommandAsync(disableCmd);

        // Assert — DisableTenant succeeds, proving state was reconstructed from the event store.
        // If state were lost, DisableTenant would fail because the aggregate wouldn't know the tenant exists.
        _ = disableResult.ShouldNotBeNull();
        disableResult.Accepted.ShouldBeTrue(
            $"DisableTenant should be accepted after actor reactivation but got: {disableResult.ErrorMessage}"
            + (_fixture.LastProcessException is not null ? $"\nServer exception: {_fixture.LastProcessException}" : ""));
        disableResult.EventCount.ShouldBe(1, "DisableTenant should produce 1 TenantDisabled event");
    }

    /// <summary>
    /// Forces actor deactivation by sending DELETE to the application's actor endpoint.
    /// This replicates what the DAPR runtime does during idle timeout, removing the actor
    /// from the in-process actor manager so the next invocation triggers fresh state load.
    /// </summary>
    private async Task DeactivateActorAsync(string actorId) {
        using var httpClient = new HttpClient();
        string url = $"{_fixture.AppEndpoint}/actors/{nameof(AggregateActor)}/{actorId}";
        HttpResponseMessage response = await httpClient.DeleteAsync(url);
        // 200 or 404 both acceptable — 404 means actor was never activated in this host instance
        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound) {
            string body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Actor deactivation request failed with {response.StatusCode}: {body}");
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
