using Hexalith.EventStore.Client.Subscriptions;
using Hexalith.Tenants.Client.Handlers;
using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;

using Shouldly;

namespace Hexalith.Tenants.Client.Tests.Handlers;

public class TenantProjectionEventHandlerTests {
    // The managed tenant ID flows through the platform context's AggregateId (the envelope TenantId is the
    // publisher scope, "system"); the projection handler keys local state by AggregateId.
    private static EventStoreDomainEventContext CreateContext(string tenantId, string messageId = "msg-1")
        => new("system", tenantId, messageId, 1, DateTimeOffset.UtcNow, "corr-1");

    private static EventStoreDomainEventContext CreateContext(
        string tenantId,
        string messageId,
        long sequenceNumber,
        DateTimeOffset timestamp,
        string correlationId)
        => new("system", tenantId, messageId, sequenceNumber, timestamp, correlationId);

    [Fact]
    public async Task HandleAsync_TenantCreated_InitializesState() {
        // Arrange
        var store = new InMemoryTenantProjectionStore();
        var handler = new TenantProjectionEventHandler(store);
        var @event = new TenantCreated("acme", "Acme Corp", "A description", DateTimeOffset.UtcNow);

        // Act
        await handler.HandleAsync(@event, CreateContext("acme"));

        // Assert
        TenantLocalState? state = await store.GetAsync("acme");
        _ = state.ShouldNotBeNull();
        state.TenantId.ShouldBe("acme");
        state.Name.ShouldBe("Acme Corp");
        state.Description.ShouldBe("A description");
        state.Status.ShouldBe(TenantStatus.Active);
    }

    [Fact]
    public async Task HandleAsync_AppliedEvent_CapturesBoundedLastEventMetadata() {
        // Arrange
        var store = new InMemoryTenantProjectionStore();
        var handler = new TenantProjectionEventHandler(store);
        DateTimeOffset timestamp = DateTimeOffset.Parse("2026-06-01T09:15:00+00:00");
        var context = CreateContext("acme", "msg-42", 42, timestamp, "corr-42");

        // Act
        await handler.HandleAsync(new TenantCreated("acme", "Acme Corp", null, timestamp), context);

        // Assert
        TenantLocalState? state = await store.GetAsync("acme");
        _ = state.ShouldNotBeNull();
        _ = state.LastEvent.ShouldNotBeNull();
        state.LastEvent.LastMessageId.ShouldBe("msg-42");
        state.LastEvent.LastSequenceNumber.ShouldBe(42);
        state.LastEvent.LastUpdatedAt.ShouldBe(timestamp);
        state.LastEvent.LastCorrelationId.ShouldBe("corr-42");
    }

    [Fact]
    public async Task HandleAsync_StaleSequence_DoesNotOverwriteProjectionState() {
        // Arrange
        var store = new InMemoryTenantProjectionStore();
        var handler = new TenantProjectionEventHandler(store);
        DateTimeOffset newerTimestamp = DateTimeOffset.Parse("2026-06-01T09:15:00+00:00");
        DateTimeOffset staleTimestamp = DateTimeOffset.Parse("2026-06-01T09:14:00+00:00");

        await handler.HandleAsync(
            new TenantCreated("acme", "Current Name", "Current description", newerTimestamp),
            CreateContext("acme", "msg-10", 10, newerTimestamp, "corr-10"));

        // Act
        await handler.HandleAsync(
            new TenantUpdated("acme", "Stale Name", "Stale description", staleTimestamp),
            CreateContext("acme", "msg-9", 9, staleTimestamp, "corr-9"));

        // Assert
        TenantLocalState? state = await store.GetAsync("acme");
        _ = state.ShouldNotBeNull();
        state.Name.ShouldBe("Current Name");
        state.Description.ShouldBe("Current description");
        _ = state.LastEvent.ShouldNotBeNull();
        state.LastEvent.LastMessageId.ShouldBe("msg-10");
        state.LastEvent.LastSequenceNumber.ShouldBe(10);
    }

    [Fact]
    public async Task HandleAsync_UserAddedToTenant_AddsMemberWithRole() {
        // Arrange
        var store = new InMemoryTenantProjectionStore();
        var handler = new TenantProjectionEventHandler(store);
        var @event = new UserAddedToTenant("acme", "user1", TenantRole.TenantOwner);

        // Act
        await handler.HandleAsync(@event, CreateContext("acme"));

        // Assert
        TenantLocalState? state = await store.GetAsync("acme");
        _ = state.ShouldNotBeNull();
        state.Members.ShouldContainKey("user1");
        state.Members["user1"].ShouldBe(TenantRole.TenantOwner);
    }

    [Fact]
    public async Task HandleAsync_UserRemovedFromTenant_RemovesMember() {
        // Arrange
        var store = new InMemoryTenantProjectionStore();
        var handler = new TenantProjectionEventHandler(store);
        await handler.HandleAsync(
            new UserAddedToTenant("acme", "user1", TenantRole.TenantOwner),
            CreateContext("acme", "msg-1"));

        var @event = new UserRemovedFromTenant("acme", "user1");

        // Act
        await handler.HandleAsync(@event, CreateContext("acme", "msg-2"));

        // Assert
        TenantLocalState? state = await store.GetAsync("acme");
        _ = state.ShouldNotBeNull();
        state.Members.ShouldNotContainKey("user1");
    }

    [Fact]
    public async Task HandleAsync_TenantDisabled_SetsStatusDisabled() {
        // Arrange
        var store = new InMemoryTenantProjectionStore();
        var handler = new TenantProjectionEventHandler(store);
        await handler.HandleAsync(
            new TenantCreated("acme", "Acme", null, DateTimeOffset.UtcNow),
            CreateContext("acme", "msg-1"));

        var @event = new TenantDisabled("acme", DateTimeOffset.UtcNow);

        // Act
        await handler.HandleAsync(@event, CreateContext("acme", "msg-2"));

        // Assert
        TenantLocalState? state = await store.GetAsync("acme");
        _ = state.ShouldNotBeNull();
        state.Status.ShouldBe(TenantStatus.Disabled);
    }

    [Fact]
    public async Task HandleAsync_TenantEnabled_RestoresStatusActive() {
        // Arrange
        var store = new InMemoryTenantProjectionStore();
        var handler = new TenantProjectionEventHandler(store);
        await handler.HandleAsync(
            new TenantDisabled("acme", DateTimeOffset.UtcNow),
            CreateContext("acme", "msg-1"));

        var @event = new TenantEnabled("acme", DateTimeOffset.UtcNow);

        // Act
        await handler.HandleAsync(@event, CreateContext("acme", "msg-2"));

        // Assert
        TenantLocalState? state = await store.GetAsync("acme");
        _ = state.ShouldNotBeNull();
        state.Status.ShouldBe(TenantStatus.Active);
    }

    [Fact]
    public async Task HandleAsync_TenantConfigurationSet_AddsConfiguration() {
        // Arrange
        var store = new InMemoryTenantProjectionStore();
        var handler = new TenantProjectionEventHandler(store);
        var @event = new TenantConfigurationSet("acme", "billing.plan", "pro");

        // Act
        await handler.HandleAsync(@event, CreateContext("acme"));

        // Assert
        TenantLocalState? state = await store.GetAsync("acme");
        _ = state.ShouldNotBeNull();
        state.Configuration.ShouldContainKey("billing.plan");
        state.Configuration["billing.plan"].ShouldBe("pro");
    }

    [Fact]
    public async Task HandleAsync_TenantConfigurationRemoved_RemovesConfiguration() {
        // Arrange
        var store = new InMemoryTenantProjectionStore();
        var handler = new TenantProjectionEventHandler(store);
        await handler.HandleAsync(
            new TenantConfigurationSet("acme", "billing.plan", "pro"),
            CreateContext("acme", "msg-1"));

        var @event = new TenantConfigurationRemoved("acme", "billing.plan");

        // Act
        await handler.HandleAsync(@event, CreateContext("acme", "msg-2"));

        // Assert
        TenantLocalState? state = await store.GetAsync("acme");
        _ = state.ShouldNotBeNull();
        state.Configuration.ShouldNotContainKey("billing.plan");
    }

    [Fact]
    public async Task HandleAsync_DuplicateTenantConfigurationRemoved_IsHarmlessAndUpdatesMetadata() {
        // Arrange
        var store = new InMemoryTenantProjectionStore();
        var handler = new TenantProjectionEventHandler(store);
        await handler.HandleAsync(
            new TenantConfigurationSet("acme", "sample.theme", "blue"),
            CreateContext("acme", "msg-1"));

        var @event = new TenantConfigurationRemoved("acme", "sample.theme");

        // Act
        await handler.HandleAsync(@event, CreateContext("acme", "msg-2", 2, DateTimeOffset.Parse("2026-06-01T10:00:00+00:00"), "corr-2"));
        await handler.HandleAsync(@event, CreateContext("acme", "msg-3", 3, DateTimeOffset.Parse("2026-06-01T10:01:00+00:00"), "corr-3"));

        // Assert
        TenantLocalState? state = await store.GetAsync("acme");
        _ = state.ShouldNotBeNull();
        state.Configuration.ShouldNotContainKey("sample.theme");
        _ = state.LastEvent.ShouldNotBeNull();
        state.LastEvent.LastMessageId.ShouldBe("msg-3");
        state.LastEvent.LastSequenceNumber.ShouldBe(3);
    }

    [Fact]
    public async Task HandleAsync_UserRoleChanged_UpdatesRole() {
        // Arrange
        var store = new InMemoryTenantProjectionStore();
        var handler = new TenantProjectionEventHandler(store);
        await handler.HandleAsync(
            new UserAddedToTenant("acme", "user1", TenantRole.TenantOwner),
            CreateContext("acme", "msg-1"));

        var @event = new UserRoleChanged("acme", "user1", TenantRole.TenantOwner, TenantRole.TenantReader);

        // Act
        await handler.HandleAsync(@event, CreateContext("acme", "msg-2"));

        // Assert
        TenantLocalState? state = await store.GetAsync("acme");
        _ = state.ShouldNotBeNull();
        state.Members["user1"].ShouldBe(TenantRole.TenantReader);
    }

    [Fact]
    public async Task HandleAsync_TenantUpdated_UpdatesMetadata() {
        // Arrange
        var store = new InMemoryTenantProjectionStore();
        var handler = new TenantProjectionEventHandler(store);
        await handler.HandleAsync(
            new TenantCreated("acme", "Acme Corp", "Old Desc", DateTimeOffset.UtcNow),
            CreateContext("acme", "msg-1"));

        var @event = new TenantUpdated("acme", "New Name", "New Desc", DateTimeOffset.Parse("2026-01-15T10:30:00+00:00"));

        // Act
        await handler.HandleAsync(@event, CreateContext("acme", "msg-2"));

        // Assert
        TenantLocalState? state = await store.GetAsync("acme");
        _ = state.ShouldNotBeNull();
        state.Name.ShouldBe("New Name");
        state.Description.ShouldBe("New Desc");
    }

    [Fact]
    public async Task HandleAsync_DuplicateLifecyclePayloads_KeepEquivalentState() {
        // Arrange
        var store = new InMemoryTenantProjectionStore();
        var handler = new TenantProjectionEventHandler(store);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var created = new TenantCreated("acme", "Acme Corp", "Original", now);
        var updated = new TenantUpdated("acme", "Acme International", "Updated", now);
        var disabled = new TenantDisabled("acme", now);
        var enabled = new TenantEnabled("acme", now);

        // Act
        await handler.HandleAsync(created, CreateContext("acme", "msg-created-1"));
        await handler.HandleAsync(created, CreateContext("acme", "msg-created-2"));
        await handler.HandleAsync(updated, CreateContext("acme", "msg-updated-1"));
        await handler.HandleAsync(updated, CreateContext("acme", "msg-updated-2"));
        await handler.HandleAsync(disabled, CreateContext("acme", "msg-disabled-1"));
        await handler.HandleAsync(disabled, CreateContext("acme", "msg-disabled-2"));
        await handler.HandleAsync(enabled, CreateContext("acme", "msg-enabled-1"));
        await handler.HandleAsync(enabled, CreateContext("acme", "msg-enabled-2"));

        // Assert
        TenantLocalState? state = await store.GetAsync("acme");
        _ = state.ShouldNotBeNull();
        state.Name.ShouldBe("Acme International");
        state.Description.ShouldBe("Updated");
        state.Status.ShouldBe(TenantStatus.Active);
        state.Members.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_DuplicateMembershipPayloads_KeepEquivalentState() {
        // Arrange
        var store = new InMemoryTenantProjectionStore();
        var handler = new TenantProjectionEventHandler(store);
        var added = new UserAddedToTenant("acme", "user1", TenantRole.TenantReader);
        var changed = new UserRoleChanged("acme", "user1", TenantRole.TenantReader, TenantRole.TenantOwner);
        var removed = new UserRemovedFromTenant("acme", "user1");

        // Act
        await handler.HandleAsync(added, CreateContext("acme", "msg-added-1"));
        await handler.HandleAsync(added, CreateContext("acme", "msg-added-2"));
        TenantLocalState? afterDuplicateAdd = await store.GetAsync("acme");

        await handler.HandleAsync(changed, CreateContext("acme", "msg-changed-1"));
        await handler.HandleAsync(changed, CreateContext("acme", "msg-changed-2"));
        TenantLocalState? afterDuplicateChange = await store.GetAsync("acme");

        await handler.HandleAsync(removed, CreateContext("acme", "msg-removed-1"));
        await handler.HandleAsync(removed, CreateContext("acme", "msg-removed-2"));
        TenantLocalState? afterDuplicateRemove = await store.GetAsync("acme");

        // Assert
        _ = afterDuplicateAdd.ShouldNotBeNull();
        afterDuplicateAdd.Members.Count.ShouldBe(1);
        afterDuplicateAdd.Members["user1"].ShouldBe(TenantRole.TenantReader);

        _ = afterDuplicateChange.ShouldNotBeNull();
        afterDuplicateChange.Members.Count.ShouldBe(1);
        afterDuplicateChange.Members["user1"].ShouldBe(TenantRole.TenantOwner);

        _ = afterDuplicateRemove.ShouldNotBeNull();
        afterDuplicateRemove.Members.ShouldNotContainKey("user1");
    }

    [Fact]
    public async Task HandleAsync_DuplicateConfigurationPayloads_KeepEquivalentState() {
        // Arrange
        var store = new InMemoryTenantProjectionStore();
        var handler = new TenantProjectionEventHandler(store);
        var set = new TenantConfigurationSet("acme", "sample.theme", "blue");
        var removed = new TenantConfigurationRemoved("acme", "sample.theme");

        // Act
        await handler.HandleAsync(set, CreateContext("acme", "msg-set-1"));
        await handler.HandleAsync(set, CreateContext("acme", "msg-set-2"));
        TenantLocalState? afterDuplicateSet = await store.GetAsync("acme");

        await handler.HandleAsync(removed, CreateContext("acme", "msg-removed-1"));
        await handler.HandleAsync(removed, CreateContext("acme", "msg-removed-2"));
        TenantLocalState? afterDuplicateRemove = await store.GetAsync("acme");

        // Assert
        _ = afterDuplicateSet.ShouldNotBeNull();
        afterDuplicateSet.Configuration.Count.ShouldBe(1);
        afterDuplicateSet.Configuration["sample.theme"].ShouldBe("blue");

        _ = afterDuplicateRemove.ShouldNotBeNull();
        afterDuplicateRemove.Configuration.ShouldNotContainKey("sample.theme");
        _ = afterDuplicateRemove.LastEvent.ShouldNotBeNull();
        afterDuplicateRemove.LastEvent.LastMessageId.ShouldBe("msg-removed-2");
    }

    [Fact]
    public async Task HandleAsync_UserIdentifiersRemainCaseSensitive() {
        // Arrange
        var store = new InMemoryTenantProjectionStore();
        var handler = new TenantProjectionEventHandler(store);

        // Act
        await handler.HandleAsync(
            new UserAddedToTenant("acme", "User1", TenantRole.TenantOwner),
            CreateContext("acme", "msg-1"));
        await handler.HandleAsync(
            new UserAddedToTenant("acme", "user1", TenantRole.TenantReader),
            CreateContext("acme", "msg-2"));

        // Assert
        TenantLocalState? state = await store.GetAsync("acme");
        _ = state.ShouldNotBeNull();
        state.Members.Count.ShouldBe(2);
        state.Members["User1"].ShouldBe(TenantRole.TenantOwner);
        state.Members["user1"].ShouldBe(TenantRole.TenantReader);
    }

    [Fact]
    public async Task HandleAsync_MultipleTenants_MaintainsIndependentState() {
        // Arrange
        var store = new InMemoryTenantProjectionStore();
        var handler = new TenantProjectionEventHandler(store);

        // Act
        await handler.HandleAsync(
            new UserAddedToTenant("acme", "user1", TenantRole.TenantOwner),
            CreateContext("acme", "msg-1"));

        await handler.HandleAsync(
            new UserAddedToTenant("beta", "user2", TenantRole.TenantReader),
            CreateContext("beta", "msg-2"));

        // Assert
        TenantLocalState? acmeState = await store.GetAsync("acme");
        TenantLocalState? betaState = await store.GetAsync("beta");

        acmeState!.Members.ShouldContainKey("user1");
        acmeState.Members.ShouldNotContainKey("user2");
        betaState!.Members.ShouldContainKey("user2");
        betaState.Members.ShouldNotContainKey("user1");
    }
}
