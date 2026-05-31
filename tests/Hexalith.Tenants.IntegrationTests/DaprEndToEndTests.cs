#pragma warning disable CA2007

using System.Text.Json;

using Dapr.Actors;
using Dapr.Actors.Client;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Server.Actors;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Events.Rejections;
using Hexalith.Tenants.IntegrationTests.Fixtures;

using Shouldly;

using EventEnvelope = Hexalith.EventStore.Server.Events.EventEnvelope;

namespace Hexalith.Tenants.IntegrationTests;

/// <summary>
/// Tier 2 DAPR slim-init end-to-end tests for Story 2.4, AC #10.
/// Validates the full command pipeline: Actor → DAPR State Store → Domain Service Invocation → /process → Aggregate → Events.
/// Requires: dapr init (Redis, Placement, Scheduler running).
/// </summary>
[Collection("TenantsDaprTest")]
public class DaprEndToEndTests {
    private const string GlobalAdminExtensionKey = "actor:globalAdmin";

    private readonly TenantsDaprTestFixture _fixture;

    public DaprEndToEndTests(TenantsDaprTestFixture fixture) => _fixture = fixture;

    [DaprFact]
    public async Task CreateTenant_succeeds_end_to_end_with_events_published() {
        _fixture.SkipIfUnavailable();

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

    [DaprFact]
    public async Task AddUserToTenant_succeeds_end_to_end_with_user_added_event_published() {
        _fixture.SkipIfUnavailable();
        _fixture.EventPublisher.Reset();

        // Arrange — create an enabled tenant, then add a member through the actor pipeline.
        ActorProxyFactory actorProxyFactory = CreateActorProxyFactory();
        string tenantId = $"t-add-user-{Guid.NewGuid():N}";

        CommandEnvelope createCmd = CreateTenantCommand(new CreateTenant(tenantId, "Membership Target", "Add user E2E"));
        IAggregateActor proxy = CreateActorProxy(actorProxyFactory, createCmd);
        CommandProcessingResult createResult = await proxy.ProcessCommandAsync(createCmd);
        createResult.Accepted.ShouldBeTrue("Setup: CreateTenant must succeed");

        // Act
        CommandEnvelope addUserCmd = CreateTenantCommand(new AddUserToTenant(tenantId, "alice", TenantRole.TenantContributor));
        CommandProcessingResult result = await proxy.ProcessCommandAsync(addUserCmd);

        // Assert
        _ = result.ShouldNotBeNull();
        result.Accepted.ShouldBeTrue(
            $"AddUserToTenant should be accepted but got error: {result.ErrorMessage}"
            + (_fixture.LastProcessException is not null ? $"\nServer exception: {_fixture.LastProcessException}" : ""));
        result.EventCount.ShouldBe(1, "AddUserToTenant should produce 1 UserAddedToTenant event");

        string expectedTopic = addUserCmd.AggregateIdentity.PubSubTopic;
        CountPublishedEvents(expectedTopic, tenantId, typeof(UserAddedToTenant).FullName).ShouldBe(1);

        EventEnvelope persisted = await AssertPersistedOnceAsync<UserAddedToTenant>(
            proxy,
            addUserCmd,
            expectedSequence: 2);
        persisted.AggregateId.ShouldBe(tenantId);
    }

    [DaprFact]
    public async Task Duplicate_AddUserToTenant_is_rejected_end_to_end_without_duplicate_UserAdded_event() {
        _fixture.SkipIfUnavailable();
        _fixture.EventPublisher.Reset();

        // Arrange — create the tenant and add the user once.
        ActorProxyFactory actorProxyFactory = CreateActorProxyFactory();
        string tenantId = $"t-dup-add-user-{Guid.NewGuid():N}";

        CommandEnvelope createCmd = CreateTenantCommand(new CreateTenant(tenantId, "Duplicate Membership Target", "Add user once"));
        IAggregateActor proxy = CreateActorProxy(actorProxyFactory, createCmd);
        CommandProcessingResult createResult = await proxy.ProcessCommandAsync(createCmd);
        createResult.Accepted.ShouldBeTrue("Setup: CreateTenant must succeed");

        CommandEnvelope firstAddCmd = CreateTenantCommand(new AddUserToTenant(tenantId, "alice", TenantRole.TenantReader));
        CommandProcessingResult firstAddResult = await proxy.ProcessCommandAsync(firstAddCmd);
        firstAddResult.Accepted.ShouldBeTrue("Setup: first AddUserToTenant must succeed");

        string expectedTopic = firstAddCmd.AggregateIdentity.PubSubTopic;
        int userAddedEventsBefore = CountPublishedEvents(expectedTopic, tenantId, typeof(UserAddedToTenant).FullName);

        // Act — submit the duplicate add with a different requested role.
        CommandEnvelope duplicateAddCmd = CreateTenantCommand(new AddUserToTenant(tenantId, "alice", TenantRole.TenantOwner));
        CommandProcessingResult result = await proxy.ProcessCommandAsync(duplicateAddCmd);

        // Assert — the duplicate is a structured rejection and no second UserAddedToTenant event is published.
        _ = result.ShouldNotBeNull();
        result.Accepted.ShouldBeFalse("Duplicate AddUserToTenant should be rejected");
        result.EventCount.ShouldBe(1, "Duplicate AddUserToTenant should persist one rejection event");
        CountPublishedEvents(expectedTopic, tenantId, typeof(UserAddedToTenant).FullName).ShouldBe(userAddedEventsBefore);
        CountPublishedEvents(expectedTopic, tenantId, typeof(UserAlreadyInTenantRejection).FullName).ShouldBe(1);
    }

    [DaprFact]
    public async Task UpdateTenant_succeeds_end_to_end_with_events_published() {
        _fixture.SkipIfUnavailable();

        // Arrange — create tenant first, then update metadata on the same aggregate actor.
        ActorProxyFactory actorProxyFactory = CreateActorProxyFactory();
        string tenantId = $"t-update-{Guid.NewGuid():N}";

        CommandEnvelope createCmd = CreateTenantCommand(
            new CreateTenant(tenantId, "Update Target", "Original metadata"));
        IAggregateActor proxy = CreateActorProxy(actorProxyFactory, createCmd);
        CommandProcessingResult createResult = await proxy.ProcessCommandAsync(createCmd);
        createResult.Accepted.ShouldBeTrue("Setup: CreateTenant must succeed");

        // Act
        CommandEnvelope updateCmd = CreateTenantCommand(
            new UpdateTenant(tenantId, "Updated Target", "Updated metadata"));
        CommandProcessingResult result = await proxy.ProcessCommandAsync(updateCmd);

        // Assert
        _ = result.ShouldNotBeNull();
        result.Accepted.ShouldBeTrue(
            $"UpdateTenant should be accepted but got error: {result.ErrorMessage}"
            + (_fixture.LastProcessException is not null ? $"\nServer exception: {_fixture.LastProcessException}" : ""));
        result.EventCount.ShouldBe(1, "UpdateTenant should produce 1 TenantUpdated event");

        string expectedTopic = updateCmd.AggregateIdentity.PubSubTopic;
        _fixture.EventPublisher.GetPublishedTopics().ShouldContain(expectedTopic);
        _fixture.EventPublisher.GetEventsForTopic(expectedTopic)
            .ShouldContain(e => e.EventTypeName.EndsWith("TenantUpdated", StringComparison.Ordinal));
    }

    [DaprFact]
    public async Task DisableTenant_succeeds_end_to_end_with_events_published() {
        _fixture.SkipIfUnavailable();

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

    [DaprFact]
    public async Task EnableTenant_succeeds_end_to_end_with_events_published() {
        _fixture.SkipIfUnavailable();

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

    [DaprFact]
    public async Task Tenant_lifecycle_commands_remain_source_of_truth_when_pubsub_publish_fails() {
        _fixture.SkipIfUnavailable();

        await AssertPublishFailurePreservesSourceOfTruthAsync<TenantCreated>(
            $"t-pubsub-create-{Guid.NewGuid():N}",
            _ => Array.Empty<CommandEnvelope>(),
            tenantId => CreateTenantCommand(new CreateTenant(tenantId, "PubSub Create", "Publish failure create")));

        await AssertPublishFailurePreservesSourceOfTruthAsync<TenantUpdated>(
            $"t-pubsub-update-{Guid.NewGuid():N}",
            tenantId => [
                CreateTenantCommand(new CreateTenant(tenantId, "PubSub Update", "Original metadata")),
            ],
            tenantId => CreateTenantCommand(new UpdateTenant(tenantId, "PubSub Updated", "Updated while pub/sub is unavailable")));

        await AssertPublishFailurePreservesSourceOfTruthAsync<TenantDisabled>(
            $"t-pubsub-disable-{Guid.NewGuid():N}",
            tenantId => [
                CreateTenantCommand(new CreateTenant(tenantId, "PubSub Disable", "Will be disabled")),
            ],
            tenantId => CreateTenantCommand(new DisableTenant(tenantId)));

        await AssertPublishFailurePreservesSourceOfTruthAsync<TenantEnabled>(
            $"t-pubsub-enable-{Guid.NewGuid():N}",
            tenantId => [
                CreateTenantCommand(new CreateTenant(tenantId, "PubSub Enable", "Will be re-enabled")),
                CreateTenantCommand(new DisableTenant(tenantId)),
            ],
            tenantId => CreateTenantCommand(new EnableTenant(tenantId)));
    }

    [DaprFact]
    public async Task Duplicate_DisableTenant_is_rejected_end_to_end_without_duplicate_TenantDisabled_event() {
        _fixture.SkipIfUnavailable();
        _fixture.EventPublisher.Reset();

        // Arrange — create and disable the tenant once.
        ActorProxyFactory actorProxyFactory = CreateActorProxyFactory();
        string tenantId = $"t-dup-disable-{Guid.NewGuid():N}";

        CommandEnvelope createCmd = CreateTenantCommand(new CreateTenant(tenantId, "Duplicate Disable Target", "Will be disabled once"));
        IAggregateActor proxy = CreateActorProxy(actorProxyFactory, createCmd);
        CommandProcessingResult createResult = await proxy.ProcessCommandAsync(createCmd);
        createResult.Accepted.ShouldBeTrue("Setup: CreateTenant must succeed");

        CommandEnvelope firstDisableCmd = CreateTenantCommand(new DisableTenant(tenantId));
        CommandProcessingResult firstDisableResult = await proxy.ProcessCommandAsync(firstDisableCmd);
        firstDisableResult.Accepted.ShouldBeTrue("Setup: first DisableTenant must succeed");

        string expectedTopic = firstDisableCmd.AggregateIdentity.PubSubTopic;
        int tenantDisabledEventsBefore = CountPublishedEvents(expectedTopic, tenantId, typeof(TenantDisabled).FullName);

        // Act — submit the duplicate disable command.
        CommandEnvelope duplicateDisableCmd = CreateTenantCommand(new DisableTenant(tenantId));
        CommandProcessingResult result = await proxy.ProcessCommandAsync(duplicateDisableCmd);

        // Assert — the duplicate is a structured rejection and no second TenantDisabled event is published.
        _ = result.ShouldNotBeNull();
        result.Accepted.ShouldBeFalse("Duplicate DisableTenant should be rejected");
        result.EventCount.ShouldBe(1, "Duplicate DisableTenant should persist one rejection event");
        CountPublishedEvents(expectedTopic, tenantId, typeof(TenantDisabled).FullName).ShouldBe(tenantDisabledEventsBefore);
        CountPublishedEvents(expectedTopic, tenantId, typeof(TenantLifecycleStateAlreadySetRejection).FullName).ShouldBe(1);
    }

    [DaprFact]
    public async Task Duplicate_EnableTenant_is_rejected_end_to_end_without_duplicate_TenantEnabled_event() {
        _fixture.SkipIfUnavailable();
        _fixture.EventPublisher.Reset();

        // Arrange — create, disable, and re-enable the tenant once.
        ActorProxyFactory actorProxyFactory = CreateActorProxyFactory();
        string tenantId = $"t-dup-enable-{Guid.NewGuid():N}";

        CommandEnvelope createCmd = CreateTenantCommand(new CreateTenant(tenantId, "Duplicate Enable Target", "Will be enabled once"));
        IAggregateActor proxy = CreateActorProxy(actorProxyFactory, createCmd);
        CommandProcessingResult createResult = await proxy.ProcessCommandAsync(createCmd);
        createResult.Accepted.ShouldBeTrue("Setup: CreateTenant must succeed");

        CommandEnvelope disableCmd = CreateTenantCommand(new DisableTenant(tenantId));
        CommandProcessingResult disableResult = await proxy.ProcessCommandAsync(disableCmd);
        disableResult.Accepted.ShouldBeTrue("Setup: DisableTenant must succeed");

        CommandEnvelope firstEnableCmd = CreateTenantCommand(new EnableTenant(tenantId));
        CommandProcessingResult firstEnableResult = await proxy.ProcessCommandAsync(firstEnableCmd);
        firstEnableResult.Accepted.ShouldBeTrue("Setup: first EnableTenant must succeed");

        string expectedTopic = firstEnableCmd.AggregateIdentity.PubSubTopic;
        int tenantEnabledEventsBefore = CountPublishedEvents(expectedTopic, tenantId, typeof(TenantEnabled).FullName);

        // Act — submit the duplicate enable command.
        CommandEnvelope duplicateEnableCmd = CreateTenantCommand(new EnableTenant(tenantId));
        CommandProcessingResult result = await proxy.ProcessCommandAsync(duplicateEnableCmd);

        // Assert — the duplicate is a structured rejection and no second TenantEnabled event is published.
        _ = result.ShouldNotBeNull();
        result.Accepted.ShouldBeFalse("Duplicate EnableTenant should be rejected");
        result.EventCount.ShouldBe(1, "Duplicate EnableTenant should persist one rejection event");
        CountPublishedEvents(expectedTopic, tenantId, typeof(TenantEnabled).FullName).ShouldBe(tenantEnabledEventsBefore);
        CountPublishedEvents(expectedTopic, tenantId, typeof(TenantLifecycleStateAlreadySetRejection).FullName).ShouldBe(1);
    }

    [DaprFact]
    public async Task UpdateTenant_on_disabled_tenant_is_rejected_end_to_end_without_TenantUpdated_event() {
        _fixture.SkipIfUnavailable();
        _fixture.EventPublisher.Reset();

        // Arrange — create and disable the tenant.
        ActorProxyFactory actorProxyFactory = CreateActorProxyFactory();
        string tenantId = $"t-disabled-update-{Guid.NewGuid():N}";

        CommandEnvelope createCmd = CreateTenantCommand(new CreateTenant(tenantId, "Disabled Update Target", "Cannot be updated while disabled"));
        IAggregateActor proxy = CreateActorProxy(actorProxyFactory, createCmd);
        CommandProcessingResult createResult = await proxy.ProcessCommandAsync(createCmd);
        createResult.Accepted.ShouldBeTrue("Setup: CreateTenant must succeed");

        CommandEnvelope disableCmd = CreateTenantCommand(new DisableTenant(tenantId));
        CommandProcessingResult disableResult = await proxy.ProcessCommandAsync(disableCmd);
        disableResult.Accepted.ShouldBeTrue("Setup: DisableTenant must succeed");

        // Act — submit a non-recovery state-changing command.
        CommandEnvelope updateCmd = CreateTenantCommand(new UpdateTenant(tenantId, "Blocked Update", "Should be rejected"));
        CommandProcessingResult result = await proxy.ProcessCommandAsync(updateCmd);

        // Assert — disabled tenants reject non-enable mutations immediately.
        _ = result.ShouldNotBeNull();
        result.Accepted.ShouldBeFalse("UpdateTenant should be rejected while the tenant is disabled");
        result.EventCount.ShouldBe(1, "UpdateTenant should persist one TenantDisabledRejection event");
        string expectedTopic = updateCmd.AggregateIdentity.PubSubTopic;
        CountPublishedEvents(expectedTopic, tenantId, typeof(TenantUpdated).FullName).ShouldBe(0);
        CountPublishedEvents(expectedTopic, tenantId, typeof(TenantDisabledRejection).FullName).ShouldBe(1);
    }

    [DaprFact]
    public async Task BootstrapGlobalAdmin_succeeds_end_to_end_with_events_published() {
        _fixture.SkipIfUnavailable();

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

    [DaprFact]
    public async Task BootstrapGlobalAdmin_duplicate_produces_rejection() {
        _fixture.SkipIfUnavailable();

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

    private int CountPublishedEvents(string topic, string tenantId, string? eventTypeName)
        => _fixture.EventPublisher
            .GetEventsForTopic(topic)
            .Count(e =>
                e.AggregateId == tenantId
                && e.EventTypeName == eventTypeName);

    private async Task AssertPublishFailurePreservesSourceOfTruthAsync<TEvent>(
        string tenantId,
        Func<string, IReadOnlyList<CommandEnvelope>> setupCommandsFactory,
        Func<string, CommandEnvelope> failingCommandFactory) {
        _fixture.EventPublisher.Reset();

        ActorProxyFactory actorProxyFactory = CreateActorProxyFactory();
        CommandEnvelope failingCommand = failingCommandFactory(tenantId);
        IAggregateActor proxy = CreateActorProxy(actorProxyFactory, failingCommand);

        foreach (CommandEnvelope setupCommand in setupCommandsFactory(tenantId)) {
            CommandProcessingResult setupResult = await proxy.ProcessCommandAsync(setupCommand);
            setupResult.Accepted.ShouldBeTrue($"Setup command {setupCommand.CommandType} must succeed before publish-failure assertion.");
        }

        _fixture.EventPublisher.Reset();
        _fixture.EventPublisher.SetupFailure("Pub/sub unavailable");

        long sequenceBeforeFailure = await proxy.GetCurrentSequenceAsync();
        CommandProcessingResult result = await proxy.ProcessCommandAsync(failingCommand);
        string topic = failingCommand.AggregateIdentity.PubSubTopic;

        result.Accepted.ShouldBeTrue($"{failingCommand.CommandType} should remain accepted after the event is persisted.");
        result.EventCount.ShouldBe(1);
        result.ErrorMessage.ShouldBeNull();
        _fixture.EventPublisher.GetEventsForTopic(topic)
            .Where(e => e.CorrelationId == failingCommand.CorrelationId)
            .ShouldBeEmpty("failed publication must not add an event to the fake publisher topic");

        EventEnvelope persisted = await AssertPersistedOnceAsync<TEvent>(
            proxy,
            failingCommand,
            sequenceBeforeFailure + 1);

        IReadOnlyList<CommandStatusRecord> historyBeforeDrain = _fixture.CommandStatusStore.GetStatusHistory(
            failingCommand.TenantId,
            failingCommand.CorrelationId);
        AssertEventsStoredThenPublishFailed(historyBeforeDrain);
        historyBeforeDrain.Select(x => x.Status).ShouldNotContain(
            CommandStatus.Completed,
            "drain recovery has not run yet, so Completed must not be required before recovery");

        _fixture.EventPublisher.ClearFailure();
        _fixture.EventPublisher.Reset();
        EventEnvelope republished = await WaitForPublishedEventAsync(
            topic,
            failingCommand.CorrelationId,
            typeof(TEvent).FullName!);

        republished.SequenceNumber.ShouldBe(persisted.SequenceNumber);
        republished.AggregateId.ShouldBe(failingCommand.AggregateId);
        republished.EventTypeName.ShouldBe(typeof(TEvent).FullName);
        republished.CorrelationId.ShouldBe(failingCommand.CorrelationId);

        _ = await AssertPersistedOnceAsync<TEvent>(
            proxy,
            failingCommand,
            persisted.SequenceNumber);
    }

    private static async Task<EventEnvelope> AssertPersistedOnceAsync<TEvent>(
        IAggregateActor proxy,
        CommandEnvelope command,
        long expectedSequence) {
        EventEnvelope[] stream = await proxy.GetEventsAsync(0);
        EventEnvelope[] matches = [
            .. stream.Where(e =>
                e.CorrelationId == command.CorrelationId
                && e.EventTypeName == typeof(TEvent).FullName),
        ];

        matches.Length.ShouldBe(1, "the persisted source event stream must not duplicate the command event");
        matches[0].SequenceNumber.ShouldBe(expectedSequence);
        matches[0].AggregateId.ShouldBe(command.AggregateId);
        matches[0].TenantId.ShouldBe(command.TenantId);
        matches[0].Domain.ShouldBe(command.Domain);
        matches[0].CorrelationId.ShouldBe(command.CorrelationId);
        return matches[0];
    }

    private async Task<EventEnvelope> WaitForPublishedEventAsync(
        string topic,
        string correlationId,
        string eventTypeName) {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(25);
        while (DateTimeOffset.UtcNow < deadline) {
            EventEnvelope? published = _fixture.EventPublisher
                .GetEventsForTopic(topic)
                .FirstOrDefault(e =>
                    e.CorrelationId == correlationId
                    && e.EventTypeName == eventTypeName);
            if (published is not null) {
                return published;
            }

            await Task.Delay(250);
        }

        throw new TimeoutException(
            $"Timed out waiting for drain recovery to publish {eventTypeName} on {topic} for {correlationId}.");
    }

    private static void AssertEventsStoredThenPublishFailed(IReadOnlyList<CommandStatusRecord> history) {
        int eventsStoredIndex = -1;
        int publishFailedIndex = -1;

        for (int i = 0; i < history.Count; i++) {
            if (history[i].Status == CommandStatus.EventsStored && eventsStoredIndex < 0) {
                eventsStoredIndex = i;
            }

            if (history[i].Status == CommandStatus.PublishFailed && publishFailedIndex < 0) {
                publishFailedIndex = i;
            }
        }

        eventsStoredIndex.ShouldBeGreaterThanOrEqualTo(0);
        publishFailedIndex.ShouldBeGreaterThanOrEqualTo(0);
        publishFailedIndex.ShouldBeGreaterThan(eventsStoredIndex);
        history[publishFailedIndex].FailureReason.ShouldBe("Pub/sub unavailable");
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

    private static Dictionary<string, string> GlobalAdminExtensions()
        => new(StringComparer.OrdinalIgnoreCase) { [GlobalAdminExtensionKey] = "true" };
}
