#pragma warning disable CA2007

using System.Text.Json;

using Dapr.Actors;
using Dapr.Actors.Client;
using Dapr.Client;

using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Server.Actors;
using Hexalith.EventStore.Server.Queries;
using Hexalith.Tenants.Contracts;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.IntegrationTests.Fixtures;
using Hexalith.Tenants.Server.Projections;

using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

/// <summary>
/// Tier 2 integration tests for Story 7.5, AC2.
/// Verifies that both the authoritative write model (aggregate, reconstructed from event
/// history/snapshots) and the read model (tenant projection, reconstructed from the shared durable
/// DAPR state store) survive an instance boundary, proving the service is stateless with no data loss.
/// Requires: dapr init (Redis, Placement, Scheduler running).
/// </summary>
[Collection("TenantsDaprTest")]
[Trait("Category", "Integration")]
public class StatelessRestartTests {
    private const string GlobalAdminExtensionKey = "actor:globalAdmin";
    private const string StateStoreName = "statestore";
    private const string TenantProjectionKeyPrefix = "projection:tenants:";

    private readonly TenantsDaprTestFixture _fixture;

    public StatelessRestartTests(TenantsDaprTestFixture fixture) => _fixture = fixture;

    [DaprFact]
    public async Task TenantState_IsReconstructedFromEventStore_AfterActorReactivation() {
        _fixture.SkipIfUnavailable();

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
            + (_fixture.LastProcessDiagnostic is not null ? $"\nServer diagnostic: {_fixture.LastProcessDiagnostic}" : ""));
        disableResult.EventCount.ShouldBe(1, "DisableTenant should produce 1 TenantDisabled event");
    }

    /// <summary>
    /// Story 7.5 AC2 (read-model lane): a tenant projection read model persisted to the shared DAPR
    /// state store is reconstructed by a fresh Tenants projection actor instance and returned through
    /// the production query path. This exercises production EventStore/DAPR state semantics (real
    /// Redis-backed state store, production projection key scheme, production read-model serialization,
    /// and the TenantsProjectionActor authorization/query path) rather than in-memory aggregate state,
    /// direct DAPR readback, or an InMemoryTenantService replay.
    /// </summary>
    [DaprFact]
    public async Task TenantProjection_QueryIsReconstructedFromStateStore_ByFreshProjectionActorInstance() {
        _fixture.SkipIfUnavailable();

        // Arrange — an authoring instance persists a tenant read model to the shared durable state store.
        string tenantId = $"t-proj-{Guid.NewGuid():N}";
        string projectionKey = TenantProjectionKeyPrefix + tenantId;
        var readModel = new TenantReadModel {
            TenantId = tenantId,
            Name = "Projection Reload Corp",
            Description = "Read-model reconstruction verification",
            Status = TenantStatus.Active,
            Members = { ["owner-user"] = TenantRole.TenantOwner },
            Configuration = { ["feature.enabled"] = "true" },
            CreatedAt = DateTimeOffset.UtcNow,
        };

        using (DaprClient authoringClient = new DaprClientBuilder()
            .UseHttpEndpoint(_fixture.DaprHttpEndpoint)
            .Build()) {
            await authoringClient.SaveStateAsync(StateStoreName, projectionKey, readModel);
        }

        string projectionActorId = QueryActorIdHelper.DeriveActorId(
            TenantProjectionRouting.ActorTypeName,
            "system",
            tenantId,
            []);
        await DeactivateActorAsync(TenantProjectionRouting.ActorTypeName, projectionActorId);

        var actorProxyFactory = new ActorProxyFactory(
            new ActorProxyOptions { HttpEndpoint = _fixture.DaprHttpEndpoint });
        IDaprProjectionActor projectionActor = actorProxyFactory.CreateActorProxy<IDaprProjectionActor>(
            new ActorId(projectionActorId),
            TenantProjectionRouting.ActorTypeName);
        var query = new QueryEnvelope(
            "system",
            GetTenantQuery.Domain,
            tenantId,
            GetTenantQuery.QueryType,
            [],
            Guid.NewGuid().ToString(),
            "owner-user",
            tenantId);

        // Act - a fresh projection actor instance serves the tenant detail query from durable state.
        QueryResult result = await projectionActor.QueryAsync(query);

        // Assert - the query path recovered and returned the previously persisted projection state.
        result.Success.ShouldBeTrue(result.ErrorMessage);
        JsonElement payload = result.GetPayload();
        payload.GetProperty("tenantId").GetString().ShouldBe(tenantId);
        payload.GetProperty("name").GetString().ShouldBe("Projection Reload Corp");
        payload.GetProperty("status").GetString().ShouldBe(nameof(TenantStatus.Active));
        payload.GetProperty("configuration").GetProperty("feature.enabled").GetString().ShouldBe("true");
        payload.GetProperty("members").EnumerateArray()
            .ShouldContain(member =>
                member.GetProperty("userId").GetString() == "owner-user"
                && member.GetProperty("role").GetString() == nameof(TenantRole.TenantOwner));
    }

    /// <summary>
    /// Forces actor deactivation by sending DELETE to the application's actor endpoint.
    /// This replicates what the DAPR runtime does during idle timeout, removing the actor
    /// from the in-process actor manager so the next invocation triggers fresh state load.
    /// </summary>
    private Task DeactivateActorAsync(string actorId)
        => DeactivateActorAsync(nameof(AggregateActor), actorId);

    private async Task DeactivateActorAsync(string actorTypeName, string actorId) {
        using var httpClient = new HttpClient();
        string url = $"{_fixture.AppEndpoint}/actors/{actorTypeName}/{Uri.EscapeDataString(actorId)}";
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
            GlobalAdminExtensions());

    private static Dictionary<string, string> GlobalAdminExtensions()
        => new(StringComparer.OrdinalIgnoreCase) { [GlobalAdminExtensionKey] = "true" };
}
