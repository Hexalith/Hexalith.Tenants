using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Server.Projections;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Projections;

public partial class TenantProjectionHandlerTests
{
    [Fact]
    public async Task NullLegacyTenantEntryDoesNotBlockAnotherOwnerUpdate()
    {
        var store = new ScriptedTenantProjectionStateStore();
        TenantIndexReadModel index = CreateInitializedIndex(2);
        index.Tenants["broken-owner"] = null!;
        index.AggregateSequences["broken-owner"] = 1;
        store.EnqueueRead<TenantReadModel>(TenantProjectionKey, new() { TenantId = "tenant-1", ProjectionVersion = "tenant-sequence:2" }, null);
        store.EnqueueTrySave(TenantProjectionKey, true);
        EnqueueSuccessfulAuditSave(store);
        store.EnqueueRead(TenantIndexKey, index, null);
        store.EnqueueTrySave(TenantIndexKey, true);
        _ = await CreateHandler(store).ProjectAsync(new("system", "tenants", "tenant-1",
            [CreateEvent(new TenantUpdated("tenant-1", "Current", null, ProjectionTime), "current", ProjectionTime, sequenceNumber: 3)]));
        TenantIndexReadModel persisted = (TenantIndexReadModel)store.TrySaveAttempts.Single(save => save.Key == TenantIndexKey).Value;
        persisted.Tenants["tenant-1"].Name.ShouldBe("Current");
        persisted.AggregateSequences["tenant-1"].ShouldBe(3);
        persisted.ProjectionVersion.ShouldBeNull();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task StaleAndNoOpReplayKeepIndexValidatorStable(long incomingSequence)
    {
        var store = new ScriptedTenantProjectionStateStore();
        TenantIndexReadModel index = CreateInitializedIndex(2);
        index.ProjectionVersion = TenantIndexProjectionVersion.Create(index);
        string? before = index.ProjectionVersion;
        store.EnqueueRead<TenantReadModel>(TenantProjectionKey, new() { TenantId = "tenant-1", ProjectionVersion = "tenant-sequence:2" }, null);
        store.EnqueueTrySave(TenantProjectionKey, true);
        EnqueueSuccessfulAuditSave(store);
        store.EnqueueRead(TenantIndexKey, index, null);
        store.EnqueueTrySave(TenantIndexKey, true);
        _ = await CreateHandler(store).ProjectAsync(new("system", "tenants", "tenant-1",
            [CreateEvent(new TenantUpdated("tenant-1", "Stale name", null, ProjectionTime), "stale", ProjectionTime, sequenceNumber: incomingSequence)]));
        TenantIndexReadModel persisted = (TenantIndexReadModel)store.TrySaveAttempts.Single(save => save.Key == TenantIndexKey).Value;
        persisted.ProjectionVersion.ShouldBe(before);
        persisted.Tenants["tenant-1"].Name.ShouldBe("Persisted Name");
        persisted.AggregateSequences["tenant-1"].ShouldBe(2);
    }

    [Fact]
    public async Task IndexConflictRecomputesAuthorityFromReloadedOwnersInsideTransform()
    {
        var store = new ScriptedTenantProjectionStateStore();
        store.EnqueueRead<TenantReadModel>(TenantProjectionKey, null, null);
        store.EnqueueTrySave(TenantProjectionKey, true);
        EnqueueSuccessfulAuditSave(store);
        TenantIndexReadModel first = CreateIndex("first-owner", "First");
        first.AggregateSequences["first-owner"] = 4;
        TenantIndexReadModel reloaded = CreateIndex("reloaded-owner", "Reloaded");
        reloaded.AggregateSequences["reloaded-owner"] = 9;
        store.EnqueueRead(TenantIndexKey, first, "etag1");
        store.EnqueueRead(TenantIndexKey, reloaded, "etag2");
        store.EnqueueTrySave(TenantIndexKey, false);
        store.EnqueueTrySave(TenantIndexKey, true);
        _ = await CreateHandler(store).ProjectAsync(CreateTenantCreatedRequest("tenant-1", "Acme", "created"));
        TenantIndexReadModel[] writes = store.TrySaveAttempts.Where(save => save.Key == TenantIndexKey)
            .Select(save => (TenantIndexReadModel)save.Value).ToArray();
        writes.Length.ShouldBe(2);
        writes[0].ProjectionVersion.ShouldNotBe(writes[1].ProjectionVersion);
        writes[1].ProjectionVersion.ShouldBe(TenantIndexProjectionVersion.Create(writes[1]));
        writes[1].Tenants.ShouldNotContainKey("first-owner");
        writes[1].AggregateSequences["reloaded-owner"].ShouldBe(9);
    }
}
