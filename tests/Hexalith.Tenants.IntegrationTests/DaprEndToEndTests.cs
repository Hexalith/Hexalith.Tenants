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
/// Tier 2 DAPR slim-init end-to-end tests for Story 2.4, AC #10.
/// Validates the full command pipeline: Actor → DAPR State Store → Domain Service Invocation → /process → Aggregate → Events.
/// Requires: dapr init (Redis, Placement, Scheduler running).
/// </summary>
[Collection("TenantsDaprTest")]
public class DaprEndToEndTests {
    private readonly TenantsDaprTestFixture _fixture;

    public DaprEndToEndTests(TenantsDaprTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task CreateTenant_succeeds_end_to_end_with_events_published() {
        // Arrange
        ActorProxyFactory actorProxyFactory = CreateActorProxyFactory();
        string tenantId = $"t-create-{Guid.NewGuid():N}";
        CommandEnvelope command = CreateTenantCommand(
            new CreateTenant(tenantId, "Acme Corp", "E2E test tenant"));

        IAggregateActor proxy = CreateActorProxy(actorProxyFactory, command);

        // Act
        CommandProcessingResult result = await proxy.ProcessCommandAsync(command);

        // Assert
        _ = result.ShouldNotBeNull();
        result.Accepted.ShouldBeTrue("CreateTenant should be accepted");
        result.EventCount.ShouldBe(1, "CreateTenant should produce 1 TenantCreated event");
        result.CorrelationId.ShouldBe(command.CorrelationId);

        // Verify events were published to the correct topic
        string expectedTopic = command.AggregateIdentity.PubSubTopic;
        _fixture.EventPublisher.GetPublishedTopics().ShouldContain(expectedTopic);
        _fixture.EventPublisher.GetEventsForTopic(expectedTopic).ShouldNotBeEmpty();
    }

    [Fact]
    public async Task DisableTenant_succeeds_end_to_end_with_events_published() {
        // Arrange — create tenant first, then disable it
        ActorProxyFactory actorProxyFactory = CreateActorProxyFactory();
        string tenantId = $"t-disable-{Guid.NewGuid():N}";

        CommandEnvelope createCmd = CreateTenantCommand(new CreateTenant(tenantId, "Disable Target", "Will be disabled"));
        IAggregateActor proxy = CreateActorProxy(actorProxyFactory, createCmd);
        CommandProcessingResult createResult = await proxy.ProcessCommandAsync(createCmd);
        createResult.Accepted.ShouldBeTrue("Setup: CreateTenant must succeed");

        // Act — disable the tenant
        CommandEnvelope disableCmd = CreateTenantCommand(new DisableTenant(tenantId));
        CommandProcessingResult result = await proxy.ProcessCommandAsync(disableCmd);

        // Assert
        _ = result.ShouldNotBeNull();
        result.Accepted.ShouldBeTrue(
            $"DisableTenant should be accepted but got error: {result.ErrorMessage}"
            + (_fixture.LastProcessException is not null ? $"\nServer exception: {_fixture.LastProcessException}" : ""));
        result.EventCount.ShouldBe(1, "DisableTenant should produce 1 TenantDisabled event");

        string expectedTopic = disableCmd.AggregateIdentity.PubSubTopic;
        _fixture.EventPublisher.GetPublishedTopics().ShouldContain(expectedTopic);
    }

    [Fact]
    public async Task EnableTenant_succeeds_end_to_end_with_events_published() {
        // Arrange — create tenant, disable it, then enable it
        ActorProxyFactory actorProxyFactory = CreateActorProxyFactory();
        string tenantId = $"t-enable-{Guid.NewGuid():N}";

        CommandEnvelope createCmd = CreateTenantCommand(new CreateTenant(tenantId, "Enable Target", "Will be re-enabled"));
        IAggregateActor proxy = CreateActorProxy(actorProxyFactory, createCmd);
        CommandProcessingResult createResult = await proxy.ProcessCommandAsync(createCmd);
        createResult.Accepted.ShouldBeTrue("Setup: CreateTenant must succeed");

        CommandEnvelope disableCmd = CreateTenantCommand(new DisableTenant(tenantId));
        CommandProcessingResult disableResult = await proxy.ProcessCommandAsync(disableCmd);
        disableResult.Accepted.ShouldBeTrue(
            $"Setup: DisableTenant must succeed. Error: {disableResult.ErrorMessage}"
            + (_fixture.LastProcessException is not null ? $"\nServer exception: {_fixture.LastProcessException}" : ""));

        // Act — enable the tenant
        CommandEnvelope enableCmd = CreateTenantCommand(new EnableTenant(tenantId));
        CommandProcessingResult result = await proxy.ProcessCommandAsync(enableCmd);

        // Assert
        _ = result.ShouldNotBeNull();
        result.Accepted.ShouldBeTrue("EnableTenant should be accepted");
        result.EventCount.ShouldBe(1, "EnableTenant should produce 1 TenantEnabled event");

        string expectedTopic = enableCmd.AggregateIdentity.PubSubTopic;
        _fixture.EventPublisher.GetPublishedTopics().ShouldContain(expectedTopic);
    }

    [Fact]
    public async Task BootstrapGlobalAdmin_succeeds_end_to_end_with_events_published() {
        // Arrange
        ActorProxyFactory actorProxyFactory = CreateActorProxyFactory();

        // Use a unique aggregate ID to avoid collision with other test runs.
        // The global-administrators aggregate is a singleton, but we use a unique suffix
        // to avoid interference between parallel test runs sharing the same Redis.
        string uniqueAggId = $"global-administrators-{Guid.NewGuid():N}";
        CommandEnvelope command = CreateGlobalAdminCommand(
            new BootstrapGlobalAdmin("admin-e2e-1"),
            uniqueAggId);

        IAggregateActor proxy = CreateActorProxy(actorProxyFactory, command);

        // Act
        CommandProcessingResult result = await proxy.ProcessCommandAsync(command);

        // Assert
        _ = result.ShouldNotBeNull();
        result.Accepted.ShouldBeTrue("BootstrapGlobalAdmin should be accepted on first run");
        result.EventCount.ShouldBe(1, "BootstrapGlobalAdmin should produce 1 GlobalAdministratorSet event");

        string expectedTopic = command.AggregateIdentity.PubSubTopic;
        _fixture.EventPublisher.GetPublishedTopics().ShouldContain(expectedTopic);
        _fixture.EventPublisher.GetEventsForTopic(expectedTopic).ShouldNotBeEmpty();
    }

    [Fact]
    public async Task BootstrapGlobalAdmin_duplicate_produces_rejection() {
        // Arrange — bootstrap once, then try again
        ActorProxyFactory actorProxyFactory = CreateActorProxyFactory();
        string uniqueAggId = $"global-administrators-{Guid.NewGuid():N}";

        CommandEnvelope firstCmd = CreateGlobalAdminCommand(new BootstrapGlobalAdmin("admin-e2e-dup"), uniqueAggId);
        IAggregateActor proxy = CreateActorProxy(actorProxyFactory, firstCmd);
        CommandProcessingResult firstResult = await proxy.ProcessCommandAsync(firstCmd);
        firstResult.Accepted.ShouldBeTrue("Setup: first bootstrap must succeed");

        // Act — second bootstrap should be rejected
        CommandEnvelope secondCmd = CreateGlobalAdminCommand(new BootstrapGlobalAdmin("admin-e2e-dup2"), uniqueAggId);
        CommandProcessingResult result = await proxy.ProcessCommandAsync(secondCmd);

        // Assert
        _ = result.ShouldNotBeNull();
        result.Accepted.ShouldBeFalse("Duplicate BootstrapGlobalAdmin should be rejected");
        result.EventCount.ShouldBe(1, "Rejection event should be persisted");
    }

    private ActorProxyFactory CreateActorProxyFactory()
        => new(new ActorProxyOptions { HttpEndpoint = _fixture.DaprHttpEndpoint });

    private static IAggregateActor CreateActorProxy(ActorProxyFactory factory, CommandEnvelope command)
        => factory.CreateActorProxy<IAggregateActor>(
            new ActorId(command.AggregateIdentity.ActorId),
            nameof(AggregateActor));

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

    private static CommandEnvelope CreateGlobalAdminCommand<T>(T command, string aggregateId) where T : notnull
        => new(
            Guid.NewGuid().ToString(),
            "system",
            "tenants",
            aggregateId,
            typeof(T).Name,
            JsonSerializer.SerializeToUtf8Bytes(command),
            Guid.NewGuid().ToString(),
            null,
            "test-user",
            null);
}
