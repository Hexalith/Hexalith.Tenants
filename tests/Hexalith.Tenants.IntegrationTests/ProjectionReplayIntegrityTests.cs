using System.Text.Json;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Testing.Fakes;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Events.Rejections;
using Hexalith.Tenants.Projections;
using Hexalith.Tenants.Server.Projections;

using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

/// <summary>Checks durable replay selection and competing writers through the shared store contract.</summary>
public sealed class ProjectionReplayIntegrityTests
{
    private const string IndexKey = "projection:tenant-index:singleton";
    private const string GlobalKey = "projection:global-administrators:singleton";

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public async Task OlderOrEqualTenantReplayPreservesIndexAndUnrelatedTenants(long prefixLength)
    {
        var store = new InMemoryReadModelStore();
        var handler = new TenantProjectionHandler(store);
        ProjectionRequest full = TenantHistory("tenant-a");
        await handler.ProjectAsync(TenantHistory("tenant-b"));
        await handler.ProjectAsync(full);
        TenantIndexReadModel before = (await store.GetAsync<TenantIndexReadModel>("statestore", IndexKey)).Value!;

        await handler.ProjectAsync(full with { Events = full.Events.Take((int)prefixLength).ToArray() });

        TenantIndexReadModel after = (await store.GetAsync<TenantIndexReadModel>("statestore", IndexKey)).Value!;
        JsonSerializer.Serialize(after).ShouldBe(JsonSerializer.Serialize(before));
        after.Tenants["tenant-a"].Name.ShouldBe("Current");
        after.Tenants["tenant-a"].Status.ShouldBe(TenantStatus.Active);
        after.UserTenants.ShouldNotContainKey("removed-user");
        after.UserTenants["user-a"]["tenant-a"].ShouldBe(TenantRole.TenantOwner);
        after.UserTenants["user-a"]["tenant-b"].ShouldBe(TenantRole.TenantOwner);
        after.AggregateSequences["tenant-a"].ShouldBe(8);
        after.AggregateSequences["tenant-b"].ShouldBe(8);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompleteHistoryAllowsPersistedRejectionsBeforeCreation(bool rejectionOnlyFirst)
    {
        var store = new InMemoryReadModelStore();
        var handler = new TenantProjectionHandler(store);
        var request = new ProjectionRequest("system", "tenants", "tenant-a", [
            Event(new TenantNotFoundRejection("tenant-a"), 1, "not-found"),
            Event(new InsufficientPermissionsRejection("tenant-a", "actor", null, "CreateTenant"), 2, "denied"),
            Event(new TenantCreated("tenant-a", "Created later", null, DateTimeOffset.UnixEpoch), 3, "created-later"),
            Event(new UserAddedToTenant("tenant-a", "owner", TenantRole.TenantOwner), 4, "owner"),
        ]);
        if (rejectionOnlyFirst)
        {
            await handler.ProjectAsync(request with { Events = request.Events.Take(2).ToArray() });
            TenantIndexReadModel empty = (await store.GetAsync<TenantIndexReadModel>("statestore", IndexKey)).Value!;
            empty.Tenants.ShouldBeEmpty();
            empty.UserTenants.ShouldBeEmpty();
            empty.AggregateSequences["tenant-a"].ShouldBe(2);
        }

        await handler.ProjectAsync(request with { Events = rejectionOnlyFirst ? request.Events.Skip(2).ToArray() : request.Events });

        TenantIndexReadModel persisted = (await store.GetAsync<TenantIndexReadModel>("statestore", IndexKey)).Value!;
        persisted.Tenants["tenant-a"].Name.ShouldBe("Created later");
        persisted.AggregateSequences["tenant-a"].ShouldBe(4);
        persisted.UserTenants["owner"]["tenant-a"].ShouldBe(TenantRole.TenantOwner);
        await handler.ProjectAsync(request with { Events = request.Events.Take(2).ToArray() });
        (await store.GetAsync<TenantIndexReadModel>("statestore", IndexKey)).Value!.Tenants["tenant-a"].Name.ShouldBe("Created later");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task NonpositiveStoredIndexWatermarkRefusesBeforeAnyWrites(long sequence)
    {
        var store = new InMemoryReadModelStore();
        var handler = new TenantProjectionHandler(store);
        ProjectionRequest request = TenantHistory("tenant-a");
        await handler.ProjectAsync(request);
        TenantIndexReadModel invalid = (await store.GetAsync<TenantIndexReadModel>("statestore", IndexKey)).Value!;
        invalid.AggregateSequences["tenant-a"] = sequence;
        store.SeedRaw("statestore", IndexKey, invalid);
        ReadModelEntry<TenantIndexReadModel> indexBefore = await store.GetAsync<TenantIndexReadModel>("statestore", IndexKey);
        ReadModelEntry<TenantReadModel> detailBefore = await store.GetAsync<TenantReadModel>("statestore", "projection:tenants:tenant-a");
        ReadModelEntry<TenantAuditReadModel> auditBefore = await store.GetAsync<TenantAuditReadModel>("statestore", "audit:tenant-a");

        await Should.ThrowAsync<InvalidOperationException>(() => handler.ProjectAsync(request));

        (await store.GetAsync<TenantIndexReadModel>("statestore", IndexKey)).ETag.ShouldBe(indexBefore.ETag);
        (await store.GetAsync<TenantReadModel>("statestore", "projection:tenants:tenant-a")).ETag.ShouldBe(detailBefore.ETag);
        (await store.GetAsync<TenantAuditReadModel>("statestore", "audit:tenant-a")).ETag.ShouldBe(auditBefore.ETag);
        store.Count.ShouldBe(3);
    }

    [Fact]
    public async Task UnorderedCompleteTenantHistoryBuildsMatchingDetailAndIndex()
    {
        var store = new InMemoryReadModelStore();
        ProjectionRequest request = TenantHistory("tenant-a");

        ProjectionResponse response = await new TenantProjectionHandler(store)
            .ProjectAsync(request with { Events = request.Events.Reverse().ToArray() });

        TenantReadModel detail = (await store.GetAsync<TenantReadModel>("statestore", "projection:tenants:tenant-a")).Value!;
        TenantIndexReadModel index = (await store.GetAsync<TenantIndexReadModel>("statestore", IndexKey)).Value!;
        detail.TenantId.ShouldBe("tenant-a");
        detail.Name.ShouldBe("Current");
        detail.Status.ShouldBe(TenantStatus.Active);
        detail.Members["user-a"].ShouldBe(TenantRole.TenantOwner);
        detail.Members.ShouldNotContainKey("removed-user");
        detail.ProjectionVersion.ShouldBe("tenant-sequence:8");
        index.Tenants["tenant-a"].Name.ShouldBe(detail.Name);
        index.Tenants["tenant-a"].Status.ShouldBe(detail.Status);
        index.UserTenants["user-a"]["tenant-a"].ShouldBe(detail.Members["user-a"]);
        index.UserTenants.ShouldNotContainKey("removed-user");
        index.AggregateSequences["tenant-a"].ShouldBe(8);
        response.State.Deserialize<TenantReadModel>()!.Name.ShouldBe(detail.Name);
    }

    [Fact]
    public async Task CaseDistinctTenantIdsHaveIndependentWatermarks()
    {
        var store = new InMemoryReadModelStore();
        var handler = new TenantProjectionHandler(store);
        await handler.ProjectAsync(TenantHistory("Tenant-A"));
        ProjectionRequest lowercase = TenantHistory("tenant-a");
        await handler.ProjectAsync(lowercase with { Events = lowercase.Events.Take(2).ToArray() });

        TenantIndexReadModel persisted = (await store.GetAsync<TenantIndexReadModel>("statestore", IndexKey)).Value!;
        persisted.Tenants.Count.ShouldBe(2);
        persisted.AggregateSequences["Tenant-A"].ShouldBe(8);
        persisted.AggregateSequences["tenant-a"].ShouldBe(2);
    }

    [Fact]
    public async Task SameSequenceSiblingsPersistOnceAndSurviveRetry()
    {
        var store = new InMemoryReadModelStore();
        var request = new ProjectionRequest("system", "tenants", "tenant-a", [
            Event(new TenantCreated("tenant-a", "A", null, DateTimeOffset.UnixEpoch), 1, "create"),
            Event(new UserAddedToTenant("tenant-a", "reader", TenantRole.TenantReader), 1, "reader"),
            Event(new UserAddedToTenant("tenant-a", "owner", TenantRole.TenantOwner), 1, "owner"),
        ]);
        // The hook's third write is the index, after detail and audit; its competing
        // tenant must survive the retried transform.
        int writes = 0;
        store.ConcurrentWriteBeforeTrySave = () =>
        {
            if (++writes != 3) return;
            var concurrent = new TenantIndexReadModel();
            concurrent.Apply(new TenantCreated("tenant-b", "B", null, DateTimeOffset.UnixEpoch));
            concurrent.AggregateSequences["tenant-b"] = 7;
            store.SeedRaw("statestore", IndexKey, concurrent);
            store.ConcurrentWriteBeforeTrySave = null;
        };
        var handler = new TenantProjectionHandler(store);
        await handler.ProjectAsync(request);
        await handler.ProjectAsync(request);

        TenantIndexReadModel persisted = (await store.GetAsync<TenantIndexReadModel>("statestore", IndexKey)).Value!;
        persisted.Tenants.Count.ShouldBe(2);
        persisted.UserTenants["reader"]["tenant-a"].ShouldBe(TenantRole.TenantReader);
        persisted.UserTenants["owner"]["tenant-a"].ShouldBe(TenantRole.TenantOwner);
        persisted.AggregateSequences["tenant-a"].ShouldBe(1);
        persisted.AggregateSequences["tenant-b"].ShouldBe(7);
        TenantAuditReadModel audit = (await store.GetAsync<TenantAuditReadModel>("statestore", "audit:tenant-a")).Value!;
        audit.Entries.Select(row => row.EventId).ShouldBe(["create", "owner", "reader"], ignoreOrder: true);
    }

    [Fact]
    public async Task TenantIndexConflictReloadsAndPreservesNewerSameOwnerState()
    {
        var store = new InMemoryReadModelStore();
        var handler = new TenantProjectionHandler(store);
        ProjectionRequest full = TenantHistory("tenant-a");
        await handler.ProjectAsync(full);
        TenantIndexReadModel newer = (await store.GetAsync<TenantIndexReadModel>("statestore", IndexKey)).Value!;
        newer.Tenants["tenant-a"] = newer.Tenants["tenant-a"] with { Name = "Concurrent newer" };
        newer.AggregateSequences["tenant-a"] = 10;
        int writes = 0;
        store.ConcurrentWriteBeforeTrySave = () =>
        {
            if (++writes != 3) return;
            store.SeedRaw("statestore", IndexKey, newer);
            store.ConcurrentWriteBeforeTrySave = null;
        };

        await handler.ProjectAsync(full with { Events = [Event(new TenantUpdated("tenant-a", "Older", null, DateTimeOffset.UnixEpoch), 9, "update-9")] });

        TenantIndexReadModel persisted = (await store.GetAsync<TenantIndexReadModel>("statestore", IndexKey)).Value!;
        persisted.Tenants["tenant-a"].Name.ShouldBe("Concurrent newer");
        persisted.AggregateSequences["tenant-a"].ShouldBe(10);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingIndexOwnerRefusesSuffixOrGappedInitializationThenAcceptsCompleteReplay(bool gapped)
    {
        var store = new InMemoryReadModelStore();
        var handler = new TenantProjectionHandler(store);
        ProjectionRequest full = TenantHistory("tenant-a");
        ProjectionRequest partial = full with { Events = gapped ? [full.Events[0], full.Events[4]] : full.Events.Skip(1).ToArray() };
        await Should.ThrowAsync<InvalidOperationException>(() => handler.ProjectAsync(partial));
        (await store.GetAsync<TenantIndexReadModel>("statestore", IndexKey)).Value.ShouldBeNull();
        (await store.GetAsync<TenantReadModel>("statestore", "projection:tenants:tenant-a")).Value.ShouldBeNull();
        (await store.GetAsync<TenantAuditReadModel>("statestore", TenantAuditStreams.GetTenantKey("tenant-a"))).Value.ShouldBeNull();
        store.Count.ShouldBe(0);

        await handler.ProjectAsync(full);

        TenantIndexReadModel persisted = (await store.GetAsync<TenantIndexReadModel>("statestore", IndexKey)).Value!;
        persisted.Tenants["tenant-a"].Name.ShouldBe("Current");
        persisted.AggregateSequences["tenant-a"].ShouldBe(8);
        persisted.UserTenants["user-a"]["tenant-a"].ShouldBe(TenantRole.TenantOwner);
    }

    [Fact]
    public async Task PopulatedLegacyIndexRefusesAffectedTenantWithoutBlockingWatermarkedOwner()
    {
        var store = new InMemoryReadModelStore();
        TenantIndexReadModel legacy = JsonSerializer.Deserialize<TenantIndexReadModel>(
            "{\"Tenants\":{\"legacy\":{\"Name\":\"Legacy\",\"Status\":1}},\"UserTenants\":{}}")!;
        legacy.Apply(new TenantCreated("current", "Current", null, DateTimeOffset.UnixEpoch));
        legacy.AggregateSequences["current"] = 1;
        store.SeedRaw("statestore", IndexKey, legacy);
        var handler = new TenantProjectionHandler(store);
        ReadModelEntry<TenantIndexReadModel> before = await store.GetAsync<TenantIndexReadModel>("statestore", IndexKey);

        InvalidOperationException error = await Should.ThrowAsync<InvalidOperationException>(() => handler.ProjectAsync(TenantHistory("legacy")));

        error.Message.ShouldContain("coordinated tenant projection rebuild");
        ReadModelEntry<TenantIndexReadModel> rejected = await store.GetAsync<TenantIndexReadModel>("statestore", IndexKey);
        rejected.ETag.ShouldBe(before.ETag);
        store.Count.ShouldBe(1);
        (await store.GetAsync<TenantReadModel>("statestore", "projection:tenants:legacy")).Value.ShouldBeNull();
        (await store.GetAsync<TenantAuditReadModel>("statestore", TenantAuditStreams.GetTenantKey("legacy"))).Value.ShouldBeNull();
        JsonSerializer.Serialize(rejected.Value).ShouldBe(JsonSerializer.Serialize(before.Value));
        await handler.ProjectAsync(new ProjectionRequest("system", "tenants", "current",
            [Event(new TenantUpdated("current", "Updated", null, DateTimeOffset.UnixEpoch), 2, "current-2")]));
        TenantIndexReadModel persisted = (await store.GetAsync<TenantIndexReadModel>("statestore", IndexKey)).Value!;
        persisted.Tenants["legacy"].Name.ShouldBe("Legacy");
        persisted.Tenants["current"].Name.ShouldBe("Updated");
        persisted.AggregateSequences.ShouldNotContainKey("legacy");
        persisted.AggregateSequences["current"].ShouldBe(2);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task OlderEqualOrUnversionedGlobalReplayPreservesAuthorityAndResponse(long sequence)
    {
        var store = new InMemoryReadModelStore();
        var handler = new GlobalAdministratorProjectionHandler(store);
        await handler.ProjectAsync(GlobalHistory());
        GlobalAdministratorReadModel before = (await store.GetAsync<GlobalAdministratorReadModel>("statestore", GlobalKey)).Value!;
        var replay = new ProjectionRequest("system", "global-administrators", "global-administrators",
            sequence == 0 ? [] : [Event(new GlobalAdministratorSet("system", "stale"), sequence, "stale")]);

        ProjectionResponse response = await handler.ProjectAsync(replay);

        GlobalAdministratorReadModel persisted = (await store.GetAsync<GlobalAdministratorReadModel>("statestore", GlobalKey)).Value!;
        JsonSerializer.Serialize(persisted).ShouldBe(JsonSerializer.Serialize(before));
        JsonSerializer.Serialize(response.State.Deserialize<GlobalAdministratorReadModel>()).ShouldBe(JsonSerializer.Serialize(persisted));
        persisted.Administrators.ShouldBe(["current"]);
        persisted.ProjectionVersion.ShouldBe("tenant-sequence:2");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("legacy")]
    public async Task OrderedGlobalReplayRefusesPopulatedAuthorityWhoseFreshnessIsUnknown(string? version)
    {
        var store = new InMemoryReadModelStore();
        var legacy = new GlobalAdministratorReadModel { ProjectionVersion = version };
        legacy.Apply(new GlobalAdministratorSet("system", "current-legacy"));
        store.SeedRaw("statestore", GlobalKey, legacy);
        ReadModelEntry<GlobalAdministratorReadModel> before = await store.GetAsync<GlobalAdministratorReadModel>("statestore", GlobalKey);

        InvalidOperationException error = await Should.ThrowAsync<InvalidOperationException>(() =>
            new GlobalAdministratorProjectionHandler(store).ProjectAsync(GlobalHistory()));

        error.Message.ShouldContain("coordinated projection rebuild");
        ReadModelEntry<GlobalAdministratorReadModel> persisted = await store.GetAsync<GlobalAdministratorReadModel>("statestore", GlobalKey);
        persisted.ETag.ShouldBe(before.ETag);
        JsonSerializer.Serialize(persisted.Value).ShouldBe(JsonSerializer.Serialize(before.Value));
        (await store.GetAsync<TenantAuditReadModel>("statestore", TenantAuditStreams.GlobalAdministratorsKey)).Value.ShouldBeNull();
        store.Count.ShouldBe(1);
    }

    [Fact]
    public async Task OrderedGlobalReplayInitializesAnEmptyLegacySingleton()
    {
        var store = new InMemoryReadModelStore();
        store.SeedRaw("statestore", GlobalKey, new GlobalAdministratorReadModel());

        ProjectionResponse response = await new GlobalAdministratorProjectionHandler(store).ProjectAsync(GlobalHistory());

        GlobalAdministratorReadModel persisted = (await store.GetAsync<GlobalAdministratorReadModel>("statestore", GlobalKey)).Value!;
        persisted.Administrators.ShouldBe(["current"]);
        persisted.ProjectionVersion.ShouldBe("tenant-sequence:2");
        response.State.Deserialize<GlobalAdministratorReadModel>()!.Administrators.ShouldBe(["current"]);
    }

    [Fact]
    public async Task IncompleteHigherGlobalHistoryPreservesAuthorityAndAllowsCanonicalRecovery()
    {
        var store = new InMemoryReadModelStore();
        var handler = new GlobalAdministratorProjectionHandler(store);
        ProjectionRequest original = GlobalHistory();
        await handler.ProjectAsync(original);
        ProjectionRequest canonical = original with
        {
            Events = [.. original.Events,
                Event(new GlobalAdministratorSet("system", "next"), 3, "global-3"),
                Event(new GlobalAdministratorRemoved("system", "current"), 4, "global-4"),
                Event(new GlobalAdministratorSet("system", "final"), 5, "global-5")],
        };

        ProjectionResponse incomplete = await handler.ProjectAsync(canonical with { Events = [canonical.Events[4]] });

        GlobalAdministratorReadModel preserved = (await store.GetAsync<GlobalAdministratorReadModel>("statestore", GlobalKey)).Value!;
        preserved.Administrators.ShouldBe(["current"]);
        preserved.ProjectionVersion.ShouldBe("tenant-sequence:2");
        incomplete.State.Deserialize<GlobalAdministratorReadModel>()!.Administrators.ShouldBe(["current"]);
        incomplete.State.Deserialize<GlobalAdministratorReadModel>()!.ProjectionVersion.ShouldBe("tenant-sequence:2");

        ProjectionResponse repaired = await handler.ProjectAsync(canonical);

        GlobalAdministratorReadModel persisted = (await store.GetAsync<GlobalAdministratorReadModel>("statestore", GlobalKey)).Value!;
        persisted.Administrators.ShouldBe(["next", "final"], ignoreOrder: true);
        persisted.ProjectionVersion.ShouldBe("tenant-sequence:5");
        repaired.State.Deserialize<GlobalAdministratorReadModel>()!.Administrators.ShouldBe(persisted.Administrators, ignoreOrder: true);
        TenantAuditReadModel audit = (await store.GetAsync<TenantAuditReadModel>("statestore", TenantAuditStreams.GlobalAdministratorsKey)).Value!;
        audit.ProjectionVersion.ShouldBe("tenant-sequence:5");
        audit.Entries.Select(row => row.EventId).Distinct(StringComparer.Ordinal).Count().ShouldBe(5);
        audit.Entries.Count.ShouldBe(5);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(5)]
    public async Task IncompleteInitialGlobalGrantRefusesBeforeAuditOrAuthorityWrites(long sequence)
    {
        var store = new InMemoryReadModelStore();
        var handler = new GlobalAdministratorProjectionHandler(store);
        var incomplete = new ProjectionRequest("system", "global-administrators", "global-administrators",
            [Event(new GlobalAdministratorSet("system", "unsafe"), sequence, "unsafe-grant")]);

        await Should.ThrowAsync<InvalidOperationException>(() => handler.ProjectAsync(incomplete));

        (await store.GetAsync<GlobalAdministratorReadModel>("statestore", GlobalKey)).Value.ShouldBeNull();
        (await store.GetAsync<TenantAuditReadModel>("statestore", TenantAuditStreams.GlobalAdministratorsKey)).Value.ShouldBeNull();
        store.Count.ShouldBe(0);
        await handler.ProjectAsync(GlobalHistory());
        (await store.GetAsync<GlobalAdministratorReadModel>("statestore", GlobalKey)).Value!.ProjectionVersion.ShouldBe("tenant-sequence:2");
    }

    [Fact]
    public async Task CompetingNewerGlobalReplayWinsWhenOlderWriterFinishesLast()
    {
        var store = new InMemoryReadModelStore();
        var newer = new GlobalAdministratorReadModel { ProjectionVersion = "tenant-sequence:9", ProjectedAt = DateTimeOffset.UtcNow };
        newer.Apply(new GlobalAdministratorSet("system", "concurrent"));
        int writes = 0;
        store.ConcurrentWriteBeforeTrySave = () =>
        {
            if (++writes != 2) return;
            store.SeedRaw("statestore", GlobalKey, newer);
            store.ConcurrentWriteBeforeTrySave = null;
        };

        ProjectionResponse response = await new GlobalAdministratorProjectionHandler(store).ProjectAsync(GlobalHistory());

        GlobalAdministratorReadModel persisted = (await store.GetAsync<GlobalAdministratorReadModel>("statestore", GlobalKey)).Value!;
        persisted.Administrators.ShouldBe(["concurrent"]);
        persisted.ProjectionVersion.ShouldBe("tenant-sequence:9");
        JsonSerializer.Serialize(response.State.Deserialize<GlobalAdministratorReadModel>()).ShouldBe(JsonSerializer.Serialize(persisted));
        TenantAuditReadModel audit = (await store.GetAsync<TenantAuditReadModel>("statestore", TenantAuditStreams.GlobalAdministratorsKey)).Value!;
        audit.Entries.Count.ShouldBe(2);
        (await store.GetAsync<TenantIndexReadModel>("statestore", IndexKey)).Value.ShouldBeNull();
        (await store.GetAsync<TenantAuditReadModel>("statestore", TenantAuditStreams.GetTenantKey("system"))).Value.ShouldBeNull();
    }

    private static ProjectionRequest TenantHistory(string tenantId) => new("system", "tenants", tenantId, [
        Event(new TenantCreated(tenantId, "Original", null, DateTimeOffset.UnixEpoch), 1, tenantId + "-1"),
        Event(new UserAddedToTenant(tenantId, "user-a", TenantRole.TenantReader), 2, tenantId + "-2"),
        Event(new TenantUpdated(tenantId, "Current", null, DateTimeOffset.UnixEpoch), 3, tenantId + "-3"),
        Event(new UserRoleChanged(tenantId, "user-a", TenantRole.TenantReader, TenantRole.TenantOwner), 4, tenantId + "-4"),
        Event(new TenantDisabled(tenantId, DateTimeOffset.UnixEpoch), 5, tenantId + "-5"),
        Event(new UserAddedToTenant(tenantId, "removed-user", TenantRole.TenantReader), 6, tenantId + "-6"),
        Event(new UserRemovedFromTenant(tenantId, "removed-user"), 7, tenantId + "-7"),
        Event(new TenantEnabled(tenantId, DateTimeOffset.UnixEpoch), 8, tenantId + "-8"),
    ]);

    private static ProjectionRequest GlobalHistory() => new("system", "global-administrators", "global-administrators", [
        Event(new GlobalAdministratorSet("system", "current"), 1, "global-1"),
        Event(new GlobalAdministratorRemoved("system", "stale"), 2, "global-2"),
    ]);

    private static ProjectionEventDto Event(object payload, long sequence, string messageId) => new(
        payload.GetType().FullName!, JsonSerializer.SerializeToUtf8Bytes(payload), "json", sequence,
        DateTimeOffset.UnixEpoch, "projection-integrity", messageId, "actor");
}
