using Hexalith.Tenants.Client.Handlers;
using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;

using Shouldly;

namespace Hexalith.Tenants.Client.Tests.Handlers;

public class TenantProjectionEventHandlerTests
{
    private static TenantEventContext CreateContext(string tenantId, string messageId = "msg-1")
        => new(tenantId, messageId, 1, DateTimeOffset.UtcNow, "corr-1");

    [Fact]
    public async Task HandleAsync_TenantCreated_InitializesState()
    {
        // Arrange
        var store = new InMemoryTenantProjectionStore();
        var handler = new TenantProjectionEventHandler(store);
        var @event = new TenantCreated("acme", "Acme Corp", "A description", DateTimeOffset.UtcNow);

        // Act
        await handler.HandleAsync(@event, CreateContext("acme"));

        // Assert
        TenantLocalState? state = await store.GetAsync("acme");
        state.ShouldNotBeNull();
        state.TenantId.ShouldBe("acme");
        state.Name.ShouldBe("Acme Corp");
        state.Description.ShouldBe("A description");
        state.Status.ShouldBe(TenantStatus.Active);
    }

    [Fact]
    public async Task HandleAsync_UserAddedToTenant_AddsMemberWithRole()
    {
        // Arrange
        var store = new InMemoryTenantProjectionStore();
        var handler = new TenantProjectionEventHandler(store);
        var @event = new UserAddedToTenant("acme", "user1", TenantRole.TenantOwner);

        // Act
        await handler.HandleAsync(@event, CreateContext("acme"));

        // Assert
        TenantLocalState? state = await store.GetAsync("acme");
        state.ShouldNotBeNull();
        state.Members.ShouldContainKey("user1");
        state.Members["user1"].ShouldBe(TenantRole.TenantOwner);
    }

    [Fact]
    public async Task HandleAsync_UserRemovedFromTenant_RemovesMember()
    {
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
        state.ShouldNotBeNull();
        state.Members.ShouldNotContainKey("user1");
    }

    [Fact]
    public async Task HandleAsync_TenantDisabled_SetsStatusDisabled()
    {
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
        state.ShouldNotBeNull();
        state.Status.ShouldBe(TenantStatus.Disabled);
    }

    [Fact]
    public async Task HandleAsync_TenantEnabled_RestoresStatusActive()
    {
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
        state.ShouldNotBeNull();
        state.Status.ShouldBe(TenantStatus.Active);
    }

    [Fact]
    public async Task HandleAsync_TenantConfigurationSet_AddsConfiguration()
    {
        // Arrange
        var store = new InMemoryTenantProjectionStore();
        var handler = new TenantProjectionEventHandler(store);
        var @event = new TenantConfigurationSet("acme", "billing.plan", "pro");

        // Act
        await handler.HandleAsync(@event, CreateContext("acme"));

        // Assert
        TenantLocalState? state = await store.GetAsync("acme");
        state.ShouldNotBeNull();
        state.Configuration.ShouldContainKey("billing.plan");
        state.Configuration["billing.plan"].ShouldBe("pro");
    }

    [Fact]
    public async Task HandleAsync_TenantConfigurationRemoved_RemovesConfiguration()
    {
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
        state.ShouldNotBeNull();
        state.Configuration.ShouldNotContainKey("billing.plan");
    }

    [Fact]
    public async Task HandleAsync_UserRoleChanged_UpdatesRole()
    {
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
        state.ShouldNotBeNull();
        state.Members["user1"].ShouldBe(TenantRole.TenantReader);
    }

    [Fact]
    public async Task HandleAsync_TenantUpdated_UpdatesMetadata()
    {
        // Arrange
        var store = new InMemoryTenantProjectionStore();
        var handler = new TenantProjectionEventHandler(store);
        await handler.HandleAsync(
            new TenantCreated("acme", "Acme Corp", "Old Desc", DateTimeOffset.UtcNow),
            CreateContext("acme", "msg-1"));

        var @event = new TenantUpdated("acme", "New Name", "New Desc");

        // Act
        await handler.HandleAsync(@event, CreateContext("acme", "msg-2"));

        // Assert
        TenantLocalState? state = await store.GetAsync("acme");
        state.ShouldNotBeNull();
        state.Name.ShouldBe("New Name");
        state.Description.ShouldBe("New Desc");
    }

    [Fact]
    public async Task HandleAsync_MultipleTenants_MaintainsIndependentState()
    {
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
