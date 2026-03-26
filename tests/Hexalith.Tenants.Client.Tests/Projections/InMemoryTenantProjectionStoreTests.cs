using Hexalith.Tenants.Client.Projections;

using Shouldly;

namespace Hexalith.Tenants.Client.Tests.Projections;

public class InMemoryTenantProjectionStoreTests {
    [Fact]
    public async Task GetAsync_UnknownTenant_ReturnsNull() {
        // Arrange
        var store = new InMemoryTenantProjectionStore();

        // Act
        TenantLocalState? result = await store.GetAsync("unknown");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task SaveAsync_ThenGetAsync_ReturnsState() {
        // Arrange
        var store = new InMemoryTenantProjectionStore();
        var state = new TenantLocalState { TenantId = "acme", Name = "Acme Corp" };

        // Act
        await store.SaveAsync(state);
        TenantLocalState? result = await store.GetAsync("acme");

        // Assert
        _ = result.ShouldNotBeNull();
        result.TenantId.ShouldBe("acme");
        result.Name.ShouldBe("Acme Corp");
        ReferenceEquals(result, state).ShouldBeFalse();
    }

    [Fact]
    public async Task SaveAsync_OverwritesExistingState() {
        // Arrange
        var store = new InMemoryTenantProjectionStore();
        var original = new TenantLocalState { TenantId = "acme", Name = "Original" };
        var updated = new TenantLocalState { TenantId = "acme", Name = "Updated" };

        // Act
        await store.SaveAsync(original);
        await store.SaveAsync(updated);
        TenantLocalState? result = await store.GetAsync("acme");

        // Assert
        _ = result.ShouldNotBeNull();
        result.Name.ShouldBe("Updated");
    }

    [Fact]
    public async Task GetAsync_ReturnedStateDoesNotMutateStoredState() {
        // Arrange
        var store = new InMemoryTenantProjectionStore();
        await store.SaveAsync(new TenantLocalState { TenantId = "acme", Name = "Original" });

        // Act
        TenantLocalState? retrieved = await store.GetAsync("acme");
        _ = retrieved.ShouldNotBeNull();
        retrieved.Name = "Mutated";
        TenantLocalState? reloaded = await store.GetAsync("acme");

        // Assert
        _ = reloaded.ShouldNotBeNull();
        reloaded.Name.ShouldBe("Original");
    }

    [Fact]
    public async Task GetAsync_TenantIsolation() {
        // Arrange
        var store = new InMemoryTenantProjectionStore();
        var acme = new TenantLocalState { TenantId = "acme", Name = "Acme" };
        var beta = new TenantLocalState { TenantId = "beta", Name = "Beta" };

        // Act
        await store.SaveAsync(acme);
        await store.SaveAsync(beta);

        // Assert
        TenantLocalState? acmeResult = await store.GetAsync("acme");
        TenantLocalState? betaResult = await store.GetAsync("beta");
        _ = acmeResult.ShouldNotBeNull();
        _ = betaResult.ShouldNotBeNull();
        acmeResult.Name.ShouldBe("Acme");
        betaResult.Name.ShouldBe("Beta");
    }

    [Fact]
    public async Task SaveAsync_NullState_ThrowsArgumentNullException() {
        // Arrange
        var store = new InMemoryTenantProjectionStore();

        // Act & Assert
        _ = await Should.ThrowAsync<ArgumentNullException>(() => store.SaveAsync(null!));
    }

    [Fact]
    public async Task GetAsync_EmptyTenantId_ThrowsArgumentException() {
        // Arrange
        var store = new InMemoryTenantProjectionStore();

        // Act & Assert
        _ = await Should.ThrowAsync<ArgumentException>(() => store.GetAsync(""));
    }

    [Fact]
    public async Task SaveAsync_EmptyTenantId_ThrowsArgumentException() {
        // Arrange
        var store = new InMemoryTenantProjectionStore();
        var state = new TenantLocalState { TenantId = "" };

        // Act & Assert
        _ = await Should.ThrowAsync<ArgumentException>(() => store.SaveAsync(state));
    }
}
