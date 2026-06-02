#pragma warning disable CA2007

using System.Text.Json;

using Dapr.Actors;
using Dapr.Actors.Client;

using Hexalith.Commons.UniqueIds;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Server.Actors;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Events.Rejections;
using Hexalith.Tenants.Contracts.Identity;
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
[DaprTestSerialization]
public class DaprEndToEndTests : IDisposable {
    private const string GlobalAdminExtensionKey = "actor:globalAdmin";

    private readonly IDisposable _daprTestLease;
    private readonly TenantsDaprTestFixture _fixture;

    public DaprEndToEndTests(TenantsDaprTestFixture fixture) {
        _daprTestLease = DaprTestExecutionGate.Enter();
        _fixture = fixture;
    }

    public void Dispose() {
        _daprTestLease.Dispose();
        GC.SuppressFinalize(this);
    }

    [DaprFact]
    public async Task CreateTenant_succeeds_end_to_end_with_events_published() {
        _fixture.SkipIfUnavailable();
        _fixture.EventPublisher.Reset();

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

        EventEnvelope persisted = await AssertPersistedOnceAsync<TenantCreated>(
            proxy,
            command,
            expectedSequence: 1);
        persisted.AggregateId.ShouldBe(tenantId);
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
            + (_fixture.LastProcessDiagnostic is not null ? $"\nServer diagnostic: {_fixture.LastProcessDiagnostic}" : ""));
        result.EventCount.ShouldBe(1, "AddUserToTenant should produce 1 UserAddedToTenant event");

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

        _ = await AssertPersistedOnceAsync<UserAddedToTenant>(
            proxy,
            firstAddCmd,
            expectedSequence: 2);

        // Act — submit the duplicate add with a different requested role.
        CommandEnvelope duplicateAddCmd = CreateTenantCommand(new AddUserToTenant(tenantId, "alice", TenantRole.TenantOwner));
        CommandProcessingResult result = await proxy.ProcessCommandAsync(duplicateAddCmd);

        // Assert — the duplicate is a structured rejection and no second UserAddedToTenant event is published.
        _ = result.ShouldNotBeNull();
        result.Accepted.ShouldBeFalse("Duplicate AddUserToTenant should be rejected");
        result.EventCount.ShouldBe(1, "Duplicate AddUserToTenant should persist one rejection event");
        _ = await AssertPersistedOnceAsync<UserAlreadyInTenantRejection>(
            proxy,
            duplicateAddCmd,
            expectedSequence: 3);
    }

    [DaprFact]
    public async Task Duplicate_RemoveUserFromTenant_is_rejected_end_to_end_without_duplicate_UserRemoved_event() {
        _fixture.SkipIfUnavailable();
        _fixture.EventPublisher.Reset();

        // Arrange — create the tenant, add a user, and remove that user once.
        ActorProxyFactory actorProxyFactory = CreateActorProxyFactory();
        string tenantId = $"t-dup-remove-user-{Guid.NewGuid():N}";

        CommandEnvelope createCmd = CreateTenantCommand(new CreateTenant(tenantId, "Duplicate Remove Target", "Remove user once"));
        IAggregateActor proxy = CreateActorProxy(actorProxyFactory, createCmd);
        CommandProcessingResult createResult = await proxy.ProcessCommandAsync(createCmd);
        createResult.Accepted.ShouldBeTrue("Setup: CreateTenant must succeed");

        CommandEnvelope addUserCmd = CreateTenantCommand(new AddUserToTenant(tenantId, "alice", TenantRole.TenantReader));
        CommandProcessingResult addUserResult = await proxy.ProcessCommandAsync(addUserCmd);
        addUserResult.Accepted.ShouldBeTrue("Setup: AddUserToTenant must succeed");

        CommandEnvelope firstRemoveCmd = CreateTenantCommand(new RemoveUserFromTenant(tenantId, "alice"));
        CommandProcessingResult firstRemoveResult = await proxy.ProcessCommandAsync(firstRemoveCmd);
        firstRemoveResult.Accepted.ShouldBeTrue("Setup: first RemoveUserFromTenant must succeed");

        _ = await AssertPersistedOnceAsync<UserRemovedFromTenant>(
            proxy,
            firstRemoveCmd,
            expectedSequence: 3);

        // Act — submit the duplicate remove command after retry rehydration would observe the missing user.
        CommandEnvelope duplicateRemoveCmd = CreateTenantCommand(new RemoveUserFromTenant(tenantId, "alice"));
        CommandProcessingResult result = await proxy.ProcessCommandAsync(duplicateRemoveCmd);

        // Assert — the duplicate is structured and no second UserRemovedFromTenant event is produced.
        _ = result.ShouldNotBeNull();
        result.Accepted.ShouldBeFalse("Duplicate RemoveUserFromTenant should be rejected");
        result.EventCount.ShouldBe(1, "Duplicate RemoveUserFromTenant should persist one rejection event");

        _ = await AssertPersistedOnceAsync<UserNotInTenantRejection>(
            proxy,
            duplicateRemoveCmd,
            expectedSequence: 4);
    }

    [DaprFact]
    public async Task Duplicate_ChangeUserRole_converges_to_noop_end_to_end_without_duplicate_UserRoleChanged_event() {
        _fixture.SkipIfUnavailable();
        _fixture.EventPublisher.Reset();

        // Arrange — create the tenant, add a user, and change the user's role once.
        ActorProxyFactory actorProxyFactory = CreateActorProxyFactory();
        string tenantId = $"t-dup-change-role-{Guid.NewGuid():N}";

        CommandEnvelope createCmd = CreateTenantCommand(new CreateTenant(tenantId, "Duplicate Role Target", "Change role once"));
        IAggregateActor proxy = CreateActorProxy(actorProxyFactory, createCmd);
        CommandProcessingResult createResult = await proxy.ProcessCommandAsync(createCmd);
        createResult.Accepted.ShouldBeTrue("Setup: CreateTenant must succeed");

        CommandEnvelope addUserCmd = CreateTenantCommand(new AddUserToTenant(tenantId, "alice", TenantRole.TenantReader));
        CommandProcessingResult addUserResult = await proxy.ProcessCommandAsync(addUserCmd);
        addUserResult.Accepted.ShouldBeTrue("Setup: AddUserToTenant must succeed");

        CommandEnvelope firstChangeCmd = CreateTenantCommand(new ChangeUserRole(tenantId, "alice", TenantRole.TenantContributor));
        CommandProcessingResult firstChangeResult = await proxy.ProcessCommandAsync(firstChangeCmd);
        firstChangeResult.Accepted.ShouldBeTrue("Setup: first ChangeUserRole must succeed");

        _ = await AssertPersistedOnceAsync<UserRoleChanged>(
            proxy,
            firstChangeCmd,
            expectedSequence: 3);
        long sequenceBeforeDuplicate = await proxy.GetCurrentSequenceAsync();

        // Act — submit the duplicate role change after retry rehydration would observe the new role.
        CommandEnvelope duplicateChangeCmd = CreateTenantCommand(new ChangeUserRole(tenantId, "alice", TenantRole.TenantContributor));
        CommandProcessingResult result = await proxy.ProcessCommandAsync(duplicateChangeCmd);

        // Assert — same-role retry is a deterministic no-op, not an unordered overwrite.
        _ = result.ShouldNotBeNull();
        result.Accepted.ShouldBeTrue("Duplicate same-role ChangeUserRole should converge to a no-op");
        result.EventCount.ShouldBe(0);
        (await proxy.GetCurrentSequenceAsync()).ShouldBe(sequenceBeforeDuplicate);
    }

    [DaprFact]
    public async Task SetTenantConfiguration_same_key_updates_preserve_ordered_persisted_state() {
        _fixture.SkipIfUnavailable();
        _fixture.EventPublisher.Reset();

        // Arrange — create a tenant, then apply same-key configuration updates through the actor pipeline.
        ActorProxyFactory actorProxyFactory = CreateActorProxyFactory();
        string tenantId = $"t-config-order-{Guid.NewGuid():N}";

        CommandEnvelope createCmd = CreateTenantCommand(new CreateTenant(tenantId, "Configuration Target", "Ordered config E2E"));
        IAggregateActor proxy = CreateActorProxy(actorProxyFactory, createCmd);
        CommandProcessingResult createResult = await proxy.ProcessCommandAsync(createCmd);
        createResult.Accepted.ShouldBeTrue("Setup: CreateTenant must succeed");

        CommandEnvelope firstSetCmd = CreateTenantCommand(new SetTenantConfiguration(tenantId, "billing.plan", "pro"));
        CommandEnvelope secondSetCmd = CreateTenantCommand(new SetTenantConfiguration(tenantId, "billing.plan", "enterprise"));

        // Act
        CommandProcessingResult firstResult = await proxy.ProcessCommandAsync(firstSetCmd);
        CommandProcessingResult secondResult = await proxy.ProcessCommandAsync(secondSetCmd);

        // Assert — persisted actor state remains gapless and final state is the last ordered event.
        firstResult.Accepted.ShouldBeTrue("First SetTenantConfiguration should be accepted");
        firstResult.EventCount.ShouldBe(1);
        secondResult.Accepted.ShouldBeTrue("Second SetTenantConfiguration should be accepted");
        secondResult.EventCount.ShouldBe(1);

        EventEnvelope firstPersisted = await AssertPersistedOnceAsync<TenantConfigurationSet>(
            proxy,
            firstSetCmd,
            expectedSequence: 2);
        EventEnvelope secondPersisted = await AssertPersistedOnceAsync<TenantConfigurationSet>(
            proxy,
            secondSetCmd,
            expectedSequence: 3);

        TenantConfigurationSet firstPayload = JsonSerializer.Deserialize<TenantConfigurationSet>(firstPersisted.Payload)!;
        TenantConfigurationSet secondPayload = JsonSerializer.Deserialize<TenantConfigurationSet>(secondPersisted.Payload)!;

        firstPayload.Key.ShouldBe("billing.plan");
        firstPayload.Value.ShouldBe("pro");
        secondPayload.Key.ShouldBe("billing.plan");
        secondPayload.Value.ShouldBe("enterprise");

    }

    [DaprFact]
    public async Task RemoveTenantConfiguration_after_set_preserves_order_and_duplicate_remove_is_rejected() {
        _fixture.SkipIfUnavailable();
        _fixture.EventPublisher.Reset();

        // Arrange — create a tenant, set a configuration key, then remove the same key.
        ActorProxyFactory actorProxyFactory = CreateActorProxyFactory();
        string tenantId = $"t-config-remove-{Guid.NewGuid():N}";

        CommandEnvelope createCmd = CreateTenantCommand(new CreateTenant(tenantId, "Configuration Remove Target", "Remove config E2E"));
        IAggregateActor proxy = CreateActorProxy(actorProxyFactory, createCmd);
        CommandProcessingResult createResult = await proxy.ProcessCommandAsync(createCmd);
        createResult.Accepted.ShouldBeTrue("Setup: CreateTenant must succeed");

        CommandEnvelope setCmd = CreateTenantCommand(new SetTenantConfiguration(tenantId, "billing.plan", "pro"));
        CommandProcessingResult setResult = await proxy.ProcessCommandAsync(setCmd);
        setResult.Accepted.ShouldBeTrue("Setup: SetTenantConfiguration must succeed");

        CommandEnvelope removeCmd = CreateTenantCommand(new RemoveTenantConfiguration(tenantId, "billing.plan"));
        CommandProcessingResult removeResult = await proxy.ProcessCommandAsync(removeCmd);

        // Assert — set and remove are persisted in a gapless ordered stream.
        removeResult.Accepted.ShouldBeTrue("RemoveTenantConfiguration should be accepted for an existing key");
        removeResult.EventCount.ShouldBe(1);
        _ = await AssertPersistedOnceAsync<TenantConfigurationSet>(
            proxy,
            setCmd,
            expectedSequence: 2);
        _ = await AssertPersistedOnceAsync<TenantConfigurationRemoved>(
            proxy,
            removeCmd,
            expectedSequence: 3);

        // Act — submit the duplicate remove after retry rehydration would observe the missing key.
        CommandEnvelope duplicateRemoveCmd = CreateTenantCommand(new RemoveTenantConfiguration(tenantId, "billing.plan"));
        CommandProcessingResult duplicateRemoveResult = await proxy.ProcessCommandAsync(duplicateRemoveCmd);

        // Assert — the duplicate remove is observable as a structured rejection without another remove event.
        duplicateRemoveResult.Accepted.ShouldBeFalse("Duplicate RemoveTenantConfiguration should be rejected");
        duplicateRemoveResult.EventCount.ShouldBe(1);

        _ = await AssertPersistedOnceAsync<ConfigurationKeyNotFoundRejection>(
            proxy,
            duplicateRemoveCmd,
            expectedSequence: 4);
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
            + (_fixture.LastProcessDiagnostic is not null ? $"\nServer diagnostic: {_fixture.LastProcessDiagnostic}" : ""));
        result.EventCount.ShouldBe(1, "UpdateTenant should produce 1 TenantUpdated event");

        _ = await AssertPersistedOnceAsync<TenantUpdated>(
            proxy,
            updateCmd,
            expectedSequence: 2);
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
            + (_fixture.LastProcessDiagnostic is not null ? $"\nServer diagnostic: {_fixture.LastProcessDiagnostic}" : ""));
        result.EventCount.ShouldBe(1, "DisableTenant should produce 1 TenantDisabled event");

        _ = await AssertPersistedOnceAsync<TenantDisabled>(
            proxy,
            disableCmd,
            expectedSequence: 2);
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
            + (_fixture.LastProcessDiagnostic is not null ? $"\nServer diagnostic: {_fixture.LastProcessDiagnostic}" : ""));

        // Act — enable the tenant
        CommandEnvelope enableCmd = CreateTenantCommand(new EnableTenant(tenantId));
        CommandProcessingResult result = await proxy.ProcessCommandAsync(enableCmd);

        // Assert
        _ = result.ShouldNotBeNull();
        result.Accepted.ShouldBeTrue("EnableTenant should be accepted");
        result.EventCount.ShouldBe(1, "EnableTenant should produce 1 TenantEnabled event");

        _ = await AssertPersistedOnceAsync<TenantEnabled>(
            proxy,
            enableCmd,
            expectedSequence: 3);
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

        _ = await AssertPersistedOnceAsync<TenantDisabled>(
            proxy,
            firstDisableCmd,
            expectedSequence: 2);

        // Act — submit the duplicate disable command.
        CommandEnvelope duplicateDisableCmd = CreateTenantCommand(new DisableTenant(tenantId));
        CommandProcessingResult result = await proxy.ProcessCommandAsync(duplicateDisableCmd);

        // Assert — the duplicate is a structured rejection and no second TenantDisabled event is published.
        _ = result.ShouldNotBeNull();
        result.Accepted.ShouldBeFalse("Duplicate DisableTenant should be rejected");
        result.EventCount.ShouldBe(1, "Duplicate DisableTenant should persist one rejection event");
        _ = await AssertPersistedOnceAsync<TenantLifecycleStateAlreadySetRejection>(
            proxy,
            duplicateDisableCmd,
            expectedSequence: 3);
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

        _ = await AssertPersistedOnceAsync<TenantEnabled>(
            proxy,
            firstEnableCmd,
            expectedSequence: 3);

        // Act — submit the duplicate enable command.
        CommandEnvelope duplicateEnableCmd = CreateTenantCommand(new EnableTenant(tenantId));
        CommandProcessingResult result = await proxy.ProcessCommandAsync(duplicateEnableCmd);

        // Assert — the duplicate is a structured rejection and no second TenantEnabled event is published.
        _ = result.ShouldNotBeNull();
        result.Accepted.ShouldBeFalse("Duplicate EnableTenant should be rejected");
        result.EventCount.ShouldBe(1, "Duplicate EnableTenant should persist one rejection event");
        _ = await AssertPersistedOnceAsync<TenantLifecycleStateAlreadySetRejection>(
            proxy,
            duplicateEnableCmd,
            expectedSequence: 4);
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
        _ = await AssertPersistedOnceAsync<TenantDisabledRejection>(
            proxy,
            updateCmd,
            expectedSequence: 3);
    }

    [DaprFact]
    public async Task BootstrapGlobalAdmin_succeeds_end_to_end_with_events_published() {
        _fixture.SkipIfUnavailable();
        _fixture.EventPublisher.Reset();

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

        EventEnvelope persisted = await AssertPersistedOnceAsync<GlobalAdministratorSet>(
            proxy,
            command,
            expectedSequence: 1);
        persisted.TenantId.ShouldBe(TenantIdentity.DefaultTenantId);
        persisted.Domain.ShouldBe(TenantIdentity.GlobalAdministratorsDomain);
        persisted.AggregateId.ShouldBe(uniqueAggId);
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

    [DaprFact]
    public async Task GlobalAdministrator_events_publish_to_shared_tenants_events_topic_with_global_domain_preserved() {
        _fixture.SkipIfUnavailable();
        _fixture.EventPublisher.Reset();

        ActorProxyFactory actorProxyFactory = CreateActorProxyFactory();
        string uniqueAggId = $"global-administrators-{Guid.NewGuid():N}";
        CommandEnvelope bootstrapCmd = CreateGlobalAdminCommand(new BootstrapGlobalAdmin("admin-e2e-shared-1"), uniqueAggId);
        IAggregateActor proxy = CreateActorProxy(actorProxyFactory, bootstrapCmd);

        CommandProcessingResult bootstrapResult = await proxy.ProcessCommandAsync(bootstrapCmd);
        CommandEnvelope setCmd = CreateGlobalAdminCommand(new SetGlobalAdministrator("admin-e2e-shared-2"), uniqueAggId, "admin-e2e-shared-1");
        CommandProcessingResult setResult = await proxy.ProcessCommandAsync(setCmd);
        CommandEnvelope removeCmd = CreateGlobalAdminCommand(new RemoveGlobalAdministrator("admin-e2e-shared-1"), uniqueAggId, "admin-e2e-shared-2");
        CommandProcessingResult removeResult = await proxy.ProcessCommandAsync(removeCmd);

        bootstrapResult.Accepted.ShouldBeTrue("BootstrapGlobalAdmin should produce GlobalAdministratorSet");
        setResult.Accepted.ShouldBeTrue("SetGlobalAdministrator should produce GlobalAdministratorSet");
        removeResult.Accepted.ShouldBeTrue("RemoveGlobalAdministrator should produce GlobalAdministratorRemoved");

        _ = await AssertPersistedOnceAsync<GlobalAdministratorSet>(
            proxy,
            bootstrapCmd,
            expectedSequence: 1);
        EventEnvelope globalAdminEnvelope = await AssertPersistedOnceAsync<GlobalAdministratorSet>(
            proxy,
            setCmd,
            expectedSequence: 2);
        _ = await AssertPersistedOnceAsync<GlobalAdministratorRemoved>(
            proxy,
            removeCmd,
            expectedSequence: 3);

        globalAdminEnvelope.TenantId.ShouldBe(TenantIdentity.DefaultTenantId);
        globalAdminEnvelope.Domain.ShouldBe(TenantIdentity.GlobalAdministratorsDomain);
        globalAdminEnvelope.AggregateId.ShouldBe(uniqueAggId);

        GlobalAdministratorSet payload = JsonSerializer.Deserialize<GlobalAdministratorSet>(globalAdminEnvelope.Payload)!;
        payload.TenantId.ShouldBe(TenantIdentity.DefaultTenantId);
    }

    private ActorProxyFactory CreateActorProxyFactory()
        => new(new ActorProxyOptions { HttpEndpoint = _fixture.DaprHttpEndpoint });

    private static IAggregateActor CreateActorProxy(ActorProxyFactory factory, CommandEnvelope command)
        => factory.CreateActorProxy<IAggregateActor>(
            new ActorId(command.AggregateIdentity.ActorId),
            nameof(AggregateActor));

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
        _fixture.EventPublisher.SetupFailureForCorrelation(failingCommand.CorrelationId, "Pub/sub unavailable");

        try {
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
            if (historyBeforeDrain.Count > 0) {
                AssertEventsStoredThenPublishFailed(historyBeforeDrain);
                historyBeforeDrain.Select(x => x.Status).ShouldNotContain(
                    CommandStatus.Completed,
                    "drain recovery has not run yet, so Completed must not be required before recovery");
            }

            _ = await AssertPersistedOnceAsync<TEvent>(
                proxy,
                failingCommand,
                persisted.SequenceNumber);
        }
        finally {
            _fixture.EventPublisher.ClearFailureForCorrelation(failingCommand.CorrelationId);
        }
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

        string statusSequence = string.Join(" -> ", history.Select(x => x.Status));
        eventsStoredIndex.ShouldBeGreaterThanOrEqualTo(0, $"Recorded status sequence: {statusSequence}");
        publishFailedIndex.ShouldBeGreaterThanOrEqualTo(0, $"Recorded status sequence: {statusSequence}");
        publishFailedIndex.ShouldBeGreaterThan(eventsStoredIndex);
        history[publishFailedIndex].FailureReason.ShouldBe("Pub/sub unavailable");
    }

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

    private static CommandEnvelope CreateGlobalAdminCommand<T>(
        T command,
        string aggregateId,
        string actorUserId = "test-user") where T : notnull
        => new(
            UniqueIdHelper.GenerateSortableUniqueStringId(),
            "system",
            TenantIdentity.GlobalAdministratorsDomain,
            aggregateId,
            typeof(T).Name,
            JsonSerializer.SerializeToUtf8Bytes(command),
            UniqueIdHelper.GenerateSortableUniqueStringId(),
            null,
            actorUserId,
            null);

    private static Dictionary<string, string> GlobalAdminExtensions()
        => new(StringComparer.OrdinalIgnoreCase) { [GlobalAdminExtensionKey] = "true" };
}
