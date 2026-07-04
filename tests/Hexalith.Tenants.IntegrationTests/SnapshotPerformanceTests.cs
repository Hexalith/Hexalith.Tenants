#pragma warning disable CA2007

using System.Diagnostics;
using System.Text.Json;

using Dapr.Actors;
using Dapr.Actors.Client;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Server.Actors;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.IntegrationTests.Fixtures;

using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

/// <summary>
/// Tier 3 scheduled performance evidence for Story 7.5, AC5 (NFR13 startup reconstruction).
/// Verifies that cold-start actor rehydration completes within 30 seconds
/// for a tenant aggregate seeded with 500,000 events (1,000 tenants x 500 events average)
/// using a 50-event snapshot interval.
///
/// This is the scheduled performance lane: it is guarded by <see cref="DaprPerformanceFactAttribute"/>
/// (HEXALITH_TENANTS_RUN_PERFORMANCE_TESTS=1 plus DAPR prerequisites), so it is skipped on ordinary PR
/// and Tier 1 runs and only contributes the NFR13 30-second claim when the benchmark actually executes.
/// It requires a running DAPR sidecar with Redis state store. NFR11 tenant/user-volume load evidence is
/// a separate release/scheduled concern and is not asserted here.
/// </summary>
[Collection("TenantsDaprTest")]
[DaprTestSerialization]
[Trait("Category", "Performance")]
public class SnapshotPerformanceTests : IDisposable {
    private const int TenantCount = 1_000;
    private const int EventsPerTenant = 500;
    private const int ConfigurationKeyCount = 100;
    private const int MaxConcurrency = 50;
    private const int RehydrationTimeoutSeconds = 30;

    private readonly IDisposable _daprTestLease;
    private readonly TenantsDaprTestFixture _fixture;

    public SnapshotPerformanceTests(TenantsDaprTestFixture fixture) {
        _daprTestLease = DaprTestExecutionGate.Enter();
        _fixture = fixture;
    }

    public void Dispose() {
        _daprTestLease.Dispose();
        GC.SuppressFinalize(this);
    }

    [DaprPerformanceFact]
    public async Task ColdStartRehydration_CompletesWithin30Seconds_With500KEvents() {
        _fixture.SkipIfUnavailable();

        // Arrange — seed 1,000 tenant aggregates with 500 events each
        var actorProxyFactory = new ActorProxyFactory(
            new ActorProxyOptions { HttpEndpoint = _fixture.DaprHttpEndpoint });

        using var semaphore = new SemaphoreSlim(MaxConcurrency);

        // Phase 1: Seed tenants with events (Create + 499 commands per tenant)
        string[] tenantIds = Enumerable.Range(0, TenantCount)
            .Select(i => $"t-perf-{i:D4}-{Guid.NewGuid():N}")
            .ToArray();

        var seedTasks = new List<Task>();
        foreach (string tenantId in tenantIds) {
            await semaphore.WaitAsync();

            seedTasks.Add(Task.Run(async () => {
                try {
                    await SeedTenantEventsAsync(actorProxyFactory, _fixture.AggregateActorTypeName, tenantId, EventsPerTenant);
                }
                finally {
                    _ = semaphore.Release();
                }
            }));
        }

        await Task.WhenAll(seedTasks);

        // Phase 2: Pick a random tenant for cold-start rehydration measurement
        string targetTenantId = tenantIds[Random.Shared.Next(tenantIds.Length)];

        // Phase 2.5: Deactivate the target actor to force a cold-start rehydration.
        // Calling DELETE on the app's actor endpoint mimics what the DAPR runtime does during
        // idle timeout, evicting the actor from the in-process actor manager so the next
        // invocation triggers OnActivateAsync() and replays state from the event store + snapshots.
        string targetActorId = BuildActorId(targetTenantId);
        await DeactivateActorAsync(targetActorId);

        // Phase 3: Measure cold-start rehydration time.
        // Create a new actor proxy and send a command to trigger full rehydration.
        // With 50-event snapshot interval, each tenant has ~10 snapshots, and rehydration
        // replays at most 50 events from the last snapshot.
        CommandEnvelope rehydrationCmd = CreateTenantCommand(
            new UpdateTenant(targetTenantId, "Rehydration Test", "Cold-start rehydration measurement"));

        IAggregateActor proxy = actorProxyFactory.CreateActorProxy<IAggregateActor>(
            new ActorId(rehydrationCmd.AggregateIdentity.ActorId),
            _fixture.AggregateActorTypeName);

        var stopwatch = Stopwatch.StartNew();
        CommandProcessingResult result = await proxy.ProcessCommandAsync(rehydrationCmd);
        stopwatch.Stop();

        // Assert — rehydration completes within 30 seconds (NFR13)
        _ = result.ShouldNotBeNull();
        result.Accepted.ShouldBeTrue(
            $"UpdateTenant should succeed after rehydration but got: {result.ErrorMessage}");

        stopwatch.Elapsed.TotalSeconds.ShouldBeLessThan(
            RehydrationTimeoutSeconds,
            $"Cold-start rehydration took {stopwatch.Elapsed.TotalSeconds:F2}s, " +
            $"exceeding the {RehydrationTimeoutSeconds}s NFR13 threshold");
    }

    private static async Task SeedTenantEventsAsync(
        ActorProxyFactory actorProxyFactory,
        string actorTypeName,
        string tenantId,
        int eventCount) {
        // Event 1: CreateTenant
        CommandEnvelope createCmd = CreateTenantCommand(
            new CreateTenant(tenantId, $"Perf Tenant {tenantId}", "Performance test seed"));

        IAggregateActor proxy = actorProxyFactory.CreateActorProxy<IAggregateActor>(
            new ActorId(createCmd.AggregateIdentity.ActorId),
            actorTypeName);

        CommandProcessingResult createResult = await proxy.ProcessCommandAsync(createCmd);
        if (!createResult.Accepted) {
            throw new InvalidOperationException(
                $"Failed to create tenant {tenantId}: {createResult.ErrorMessage}");
        }

        // Events 2 through eventCount: mix of AddUser, SetConfig, and Update commands
        for (int i = 1; i < eventCount; i++) {
            CommandEnvelope cmd = (i % 3) switch {
                0 => CreateTenantCommand(
                    new AddUserToTenant(tenantId, $"user-{i}", TenantRole.TenantReader)),
                1 => CreateTenantCommand(
                    new SetTenantConfiguration(tenantId, $"config-key-{i % ConfigurationKeyCount}", $"value-{i}")),
                _ => CreateTenantCommand(
                    new UpdateTenant(tenantId, $"Perf Tenant {tenantId} v{i}", $"Seed event {i}")),
            };

            CommandProcessingResult result = await proxy.ProcessCommandAsync(cmd);
            if (!result.Accepted) {
                throw new InvalidOperationException(
                    $"Failed to seed event {i} for tenant {tenantId}: {result.ErrorMessage}");
            }
        }
    }

    private static string BuildActorId(string tenantId) =>
        // AggregateIdentity.ActorId format: {tenantId}|{domain}|{aggregateId}
        // For tenant aggregates the aggregateId matches the tenantId
        $"system|tenants|{tenantId}";

    private async Task DeactivateActorAsync(string actorId) {
        using var httpClient = new HttpClient();
        string url = $"{_fixture.AppEndpoint}/actors/{_fixture.AggregateActorTypeName}/{actorId}";
        HttpResponseMessage response = await httpClient.DeleteAsync(url);
        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound) {
            string body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Actor deactivation request failed with {response.StatusCode}: {body}");
        }
    }

    private static readonly Dictionary<string, string> s_globalAdminExtensions = new() { ["actor:globalAdmin"] = "true" };

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
            s_globalAdminExtensions);
}
