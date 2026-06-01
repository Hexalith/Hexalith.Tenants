using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Contracts.Enums;

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
    public async Task SaveAsync_ClonesNestedProjectionStateAndLastEventOnWrite() {
        // Arrange
        var store = new InMemoryTenantProjectionStore();
        var timestamp = DateTimeOffset.Parse("2026-06-01T10:00:00+00:00");
        var state = new TenantLocalState {
            TenantId = "acme",
            Name = "Acme Corp",
            LastEvent = new TenantProjectionEventMetadata("msg-1", 1, timestamp, "corr-1"),
            Members = { ["user1"] = TenantRole.TenantOwner },
            Configuration = { ["sample.theme"] = "blue" },
        };

        // Act
        await store.SaveAsync(state);
        state.Members["user1"] = TenantRole.Unknown;
        state.Configuration["sample.theme"] = "green";
        state.LastEvent = new TenantProjectionEventMetadata("msg-mutated", 99, timestamp.AddMinutes(1), "corr-mutated");

        TenantLocalState? result = await store.GetAsync("acme");

        // Assert
        _ = result.ShouldNotBeNull();
        result.Members["user1"].ShouldBe(TenantRole.TenantOwner);
        result.Configuration["sample.theme"].ShouldBe("blue");
        _ = result.LastEvent.ShouldNotBeNull();
        result.LastEvent.LastMessageId.ShouldBe("msg-1");
        result.LastEvent.LastSequenceNumber.ShouldBe(1);
        result.LastEvent.LastUpdatedAt.ShouldBe(timestamp);
        result.LastEvent.LastCorrelationId.ShouldBe("corr-1");
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
    public async Task GetAsync_ReturnedStateDoesNotMutateStoredNestedProjectionState() {
        // Arrange
        var store = new InMemoryTenantProjectionStore();
        var timestamp = DateTimeOffset.Parse("2026-06-01T10:00:00+00:00");
        await store.SaveAsync(new TenantLocalState {
            TenantId = "acme",
            LastEvent = new TenantProjectionEventMetadata("msg-1", 1, timestamp, "corr-1"),
            Members = { ["user1"] = TenantRole.TenantOwner },
            Configuration = { ["sample.theme"] = "blue" },
        });

        // Act
        TenantLocalState? retrieved = await store.GetAsync("acme");
        _ = retrieved.ShouldNotBeNull();
        retrieved.Members["user1"] = TenantRole.Unknown;
        retrieved.Configuration["sample.theme"] = "green";
        retrieved.LastEvent = new TenantProjectionEventMetadata("msg-mutated", 99, timestamp.AddMinutes(1), "corr-mutated");
        TenantLocalState? reloaded = await store.GetAsync("acme");

        // Assert
        _ = reloaded.ShouldNotBeNull();
        reloaded.Members["user1"].ShouldBe(TenantRole.TenantOwner);
        reloaded.Configuration["sample.theme"].ShouldBe("blue");
        _ = reloaded.LastEvent.ShouldNotBeNull();
        reloaded.LastEvent.LastMessageId.ShouldBe("msg-1");
        reloaded.LastEvent.LastSequenceNumber.ShouldBe(1);
        reloaded.LastEvent.LastUpdatedAt.ShouldBe(timestamp);
        reloaded.LastEvent.LastCorrelationId.ShouldBe("corr-1");
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
