using System.Text.Json;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Projections;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Projections;
using Hexalith.Tenants.Server.Projections;

using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Projections;

public class TenantProjectionHandlerTests {
    private const string StateStoreName = "statestore";
    private const string TenantAuditProjectionKey = "audit:tenant-1";
    private const string TenantIndexKey = "projection:tenant-index:singleton";
    private const string TenantProjectionKey = "projection:tenants:tenant-1";
    private static readonly DateTimeOffset ProjectionTime = new(2026, 6, 25, 11, 45, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ProjectAsync_WhitespaceAggregateIdThrowsBeforeStateStoreAccessAsync(string aggregateId) {
        var stateStore = new ScriptedTenantProjectionStateStore();
        ProjectionRequest request = new(
            "tenant-1",
            "tenants",
            aggregateId,
            [CreateEvent(new TenantCreated("tenant-1", "Acme", null, DateTimeOffset.UtcNow), "evt-1", DateTimeOffset.UtcNow)]);

        _ = await Should.ThrowAsync<ArgumentException>(() => CreateHandler(stateStore).ProjectAsync(request));

        stateStore.ReadCalls.ShouldBeEmpty();
        stateStore.TrySaveAttempts.ShouldBeEmpty();
    }

    [Fact]
    public async Task ProjectAsync_NullAggregateIdThrowsBeforeStateStoreAccessAsync() {
        var stateStore = new ScriptedTenantProjectionStateStore();
        ProjectionRequest request = new(
            "tenant-1",
            "tenants",
            null!,
            [CreateEvent(new TenantCreated("tenant-1", "Acme", null, DateTimeOffset.UtcNow), "evt-1", DateTimeOffset.UtcNow)]);

        _ = await Should.ThrowAsync<ArgumentException>(() => CreateHandler(stateStore).ProjectAsync(request));

        stateStore.ReadCalls.ShouldBeEmpty();
        stateStore.TrySaveAttempts.ShouldBeEmpty();
    }

    [Fact]
    public async Task ProjectAsync_EmptyEventBatchReturnsDefaultProjectionWithoutStateStoreAccessAsync() {
        var stateStore = new ScriptedTenantProjectionStateStore();
        ProjectionRequest request = new("tenant-1", "tenants", "tenant-1", []);

        ProjectionResponse response = await CreateHandler(stateStore).ProjectAsync(request);

        response.ProjectionType.ShouldBe("tenants");
        TenantReadModel? state = response.State.Deserialize<TenantReadModel>();
        state.ShouldNotBeNull();
        state.TenantId.ShouldBe(string.Empty);
        stateStore.ReadCalls.ShouldBeEmpty();
        stateStore.TrySaveAttempts.ShouldBeEmpty();
    }

    [Fact]
    public async Task ProjectAsync_AllNullEventBatchReturnsDefaultProjectionWithoutStateStoreAccessAsync() {
        var stateStore = new ScriptedTenantProjectionStateStore();
        ProjectionRequest request = new("tenant-1", "tenants", "tenant-1", [null!]);

        ProjectionResponse response = await CreateHandler(stateStore).ProjectAsync(request);

        response.ProjectionType.ShouldBe("tenants");
        stateStore.ReadCalls.ShouldBeEmpty();
        stateStore.TrySaveAttempts.ShouldBeEmpty();
    }

    [Fact]
    public async Task ProjectAsync_MixedNullAndRealEventBatchSkipsNullAndAppliesRealEventAsync() {
        var stateStore = new ScriptedTenantProjectionStateStore();
        stateStore.EnqueueRead<TenantReadModel>(TenantProjectionKey, null, null);
        stateStore.EnqueueTrySave(TenantProjectionKey, true);
        EnqueueSuccessfulAuditSave(stateStore);
        stateStore.EnqueueRead<TenantIndexReadModel>(TenantIndexKey, null, null);
        stateStore.EnqueueTrySave(TenantIndexKey, true);
        DateTimeOffset timestamp = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);
        ProjectionRequest request = new(
            "tenant-1",
            "tenants",
            "tenant-1",
            [
                null!,
                CreateEvent(new TenantCreated("tenant-1", "Acme", null, timestamp), "evt-1", timestamp),
                null!,
            ]);

        _ = await CreateHandler(stateStore).ProjectAsync(request);

        TenantReadModel saved = (TenantReadModel)stateStore.TrySaveAttempts.Single(a => a.Key == TenantProjectionKey).Value;
        saved.Name.ShouldBe("Acme");
    }

    [Fact]
    public async Task ProjectAsync_AuditMergeTreatsNullPersistedEntriesAsEmptyAsync() {
        var stateStore = new ScriptedTenantProjectionStateStore();
        stateStore.EnqueueRead<TenantReadModel>(TenantProjectionKey, null, null);
        stateStore.EnqueueTrySave(TenantProjectionKey, true);
        stateStore.EnqueueRead(TenantAuditProjectionKey, new TenantAuditReadModel { Entries = null! }, "audit-etag-1");
        stateStore.EnqueueTrySave(TenantAuditProjectionKey, true);
        stateStore.EnqueueRead<TenantIndexReadModel>(TenantIndexKey, null, null);
        stateStore.EnqueueTrySave(TenantIndexKey, true);
        DateTimeOffset timestamp = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);
        ProjectionRequest request = new(
            "tenant-1",
            "tenants",
            "tenant-1",
            [CreateEvent(new TenantCreated("tenant-1", "Acme", null, timestamp), "evt-1", timestamp)]);

        _ = await CreateHandler(stateStore).ProjectAsync(request);

        TenantAuditReadModel saved = (TenantAuditReadModel)stateStore.TrySaveAttempts.Single(a => a.Key == TenantAuditProjectionKey).Value;
        saved.Entries.Select(e => e.EventId).ShouldBe(["evt-1"]);
    }

    [Fact]
    public async Task ProjectAsync_TenantReadModelNullCollectionsAreReinitializedDuringReplayAsync() {
        TenantReadModel existing = new() {
            TenantId = "tenant-1",
            Name = "Acme",
            Members = null!,
            Configuration = null!,
        };
        var stateStore = new ScriptedTenantProjectionStateStore();
        stateStore.EnqueueRead(TenantProjectionKey, existing, "tenant-etag-1");
        stateStore.EnqueueTrySave(TenantProjectionKey, true);
        EnqueueSuccessfulAuditSave(stateStore);
        stateStore.EnqueueRead<TenantIndexReadModel>(TenantIndexKey, null, null);
        stateStore.EnqueueTrySave(TenantIndexKey, true);
        DateTimeOffset timestamp = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);
        ProjectionRequest request = new(
            "tenant-1",
            "tenants",
            "tenant-1",
            [
                CreateEvent(new UserAddedToTenant("tenant-1", "user-1", TenantRole.TenantOwner), "evt-added", timestamp, sequenceNumber: 1),
                CreateEvent(new TenantConfigurationSet("tenant-1", "feature", "enabled"), "evt-config", timestamp.AddMinutes(1), sequenceNumber: 2),
            ]);

        _ = await CreateHandler(stateStore).ProjectAsync(request);

        TenantReadModel saved = (TenantReadModel)stateStore.TrySaveAttempts.Single(a => a.Key == TenantProjectionKey).Value;
        saved.Members["user-1"].ShouldBe(TenantRole.TenantOwner);
        saved.Configuration["feature"].ShouldBe("enabled");
    }

    [Fact]
    public async Task ProjectAsync_TenantIndexNullCollectionsAreReinitializedDuringReplayAsync() {
        TenantIndexReadModel existing = new() {
            Tenants = null!,
            UserTenants = null!,
        };
        var stateStore = new ScriptedTenantProjectionStateStore();
        stateStore.EnqueueRead<TenantReadModel>(TenantProjectionKey, null, null);
        stateStore.EnqueueTrySave(TenantProjectionKey, true);
        EnqueueSuccessfulAuditSave(stateStore);
        stateStore.EnqueueRead(TenantIndexKey, existing, "index-etag-1");
        stateStore.EnqueueTrySave(TenantIndexKey, true);
        DateTimeOffset timestamp = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);
        ProjectionRequest request = new(
            "tenant-1",
            "tenants",
            "tenant-1",
            [
                CreateEvent(new TenantCreated("tenant-1", "Acme", null, timestamp), "evt-created", timestamp),
                CreateEvent(new UserAddedToTenant("tenant-1", "user-1", TenantRole.TenantReader), "evt-added", timestamp.AddMinutes(1)),
            ]);

        _ = await CreateHandler(stateStore).ProjectAsync(request);

        TenantIndexReadModel saved = (TenantIndexReadModel)stateStore.TrySaveAttempts.Single(a => a.Key == TenantIndexKey).Value;
        saved.Tenants["tenant-1"].Name.ShouldBe("Acme");
        saved.UserTenants["user-1"]["tenant-1"].ShouldBe(TenantRole.TenantReader);
    }

    [Fact]
    public async Task ProjectAsync_StampsAllTenantProjectionWritesWithTimeProviderAsync() {
        var stateStore = new ScriptedTenantProjectionStateStore();
        stateStore.EnqueueRead<TenantReadModel>(TenantProjectionKey, null, null);
        stateStore.EnqueueTrySave(TenantProjectionKey, true);
        stateStore.EnqueueRead<TenantAuditReadModel>(TenantAuditProjectionKey, null, null);
        stateStore.EnqueueTrySave(TenantAuditProjectionKey, true);
        stateStore.EnqueueRead<TenantIndexReadModel>(TenantIndexKey, null, null);
        stateStore.EnqueueTrySave(TenantIndexKey, true);

        _ = await new TenantProjectionHandler(
                stateStore,
                NullLogger<TenantProjectionHandler>.Instance,
                new FixedTimeProvider(ProjectionTime))
            .ProjectAsync(CreateTenantCreatedRequest("tenant-1", "Acme", "evt-1"));

        ((TenantReadModel)stateStore.TrySaveAttempts.Single(a => a.Key == TenantProjectionKey).Value)
            .ProjectedAt.ShouldBe(ProjectionTime);
        ((TenantAuditReadModel)stateStore.TrySaveAttempts.Single(a => a.Key == TenantAuditProjectionKey).Value)
            .ProjectedAt.ShouldBe(ProjectionTime);
        ((TenantIndexReadModel)stateStore.TrySaveAttempts.Single(a => a.Key == TenantIndexKey).Value)
            .ProjectedAt.ShouldBe(ProjectionTime);
    }

    [Fact]
    public async Task ProjectAsync_StampsTenantReadModelWithHighestAggregateSequenceAsync() {
        var stateStore = new ScriptedTenantProjectionStateStore();
        stateStore.EnqueueRead<TenantReadModel>(TenantProjectionKey, null, null);
        stateStore.EnqueueTrySave(TenantProjectionKey, true);
        EnqueueSuccessfulAuditSave(stateStore);
        stateStore.EnqueueRead<TenantIndexReadModel>(TenantIndexKey, null, null);
        stateStore.EnqueueTrySave(TenantIndexKey, true);
        DateTimeOffset timestamp = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);
        ProjectionRequest request = new(
            "tenant-1",
            "tenants",
            "tenant-1",
            [
                CreateEvent(
                    new TenantCreated("tenant-1", "Acme", null, timestamp),
                    "evt-created",
                    timestamp,
                    sequenceNumber: 9),
                CreateEvent(
                    new UserAddedToTenant("tenant-1", "user-1", TenantRole.TenantReader),
                    "evt-added",
                    timestamp.AddMinutes(1),
                    sequenceNumber: 10),
            ]);

        ProjectionResponse response = await CreateHandler(stateStore).ProjectAsync(request);

        TenantReadModel saved = (TenantReadModel)stateStore.TrySaveAttempts
            .Single(attempt => attempt.Key == TenantProjectionKey)
            .Value;
        saved.ProjectionVersion.ShouldBe(TenantProjectionVersionFormat.SequencePrefix + "10");
        response.State.Deserialize<TenantReadModel>()!.ProjectionVersion.ShouldBe(TenantProjectionVersionFormat.SequencePrefix + "10");
    }

    [Fact]
    public async Task ProjectAsync_AppliesAllEventsThatShareAnIncomingSequenceAsync() {
        var stateStore = new ScriptedTenantProjectionStateStore();
        stateStore.EnqueueRead<TenantReadModel>(TenantProjectionKey, null, null);
        stateStore.EnqueueTrySave(TenantProjectionKey, true);
        EnqueueSuccessfulAuditSave(stateStore);
        stateStore.EnqueueRead<TenantIndexReadModel>(TenantIndexKey, null, null);
        stateStore.EnqueueTrySave(TenantIndexKey, true);
        DateTimeOffset timestamp = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);
        ProjectionRequest request = new(
            "tenant-1",
            "tenants",
            "tenant-1",
            [
                CreateEvent(
                    new TenantCreated("tenant-1", "Acme", null, timestamp),
                    "evt-created",
                    timestamp,
                    sequenceNumber: 1),
                CreateEvent(
                    new TenantUpdated("tenant-1", "Acme Renamed", null, timestamp.AddMinutes(1)),
                    "evt-updated",
                    timestamp.AddMinutes(1),
                    sequenceNumber: 1),
                CreateEvent(
                    new UserAddedToTenant("tenant-1", "user-1", TenantRole.TenantOwner),
                    "evt-added",
                    timestamp.AddMinutes(2),
                    sequenceNumber: 1),
            ]);

        _ = await CreateHandler(stateStore).ProjectAsync(request);

        TenantReadModel saved = (TenantReadModel)stateStore.TrySaveAttempts
            .Single(attempt => attempt.Key == TenantProjectionKey)
            .Value;
        saved.Name.ShouldBe("Acme Renamed");
        saved.Members["user-1"].ShouldBe(TenantRole.TenantOwner);
        saved.ProjectionVersion.ShouldBe(TenantProjectionVersionFormat.SequencePrefix + "1");
    }

    [Theory]
    [InlineData(10)]
    [InlineData(11)]
    public async Task ProjectAsync_OlderOrEqualReplayDoesNotRegressTenantStateAsync(long replaySequence) {
        DateTimeOffset persistedProjectedAt = ProjectionTime.AddMinutes(-5);
        var stateStore = new ScriptedTenantProjectionStateStore();
        stateStore.EnqueueRead<TenantReadModel>(
            TenantProjectionKey,
            new TenantReadModel {
                TenantId = "tenant-1",
                Name = "Persisted Name",
                ProjectedAt = persistedProjectedAt,
                ProjectionVersion = TenantProjectionVersionFormat.SequencePrefix + "11",
            },
            "tenant-etag-1");
        stateStore.EnqueueTrySave(TenantProjectionKey, true);
        EnqueueSuccessfulAuditSave(stateStore);
        stateStore.EnqueueRead<TenantIndexReadModel>(TenantIndexKey, null, null);
        stateStore.EnqueueTrySave(TenantIndexKey, true);
        DateTimeOffset timestamp = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);
        ProjectionRequest request = new(
            "tenant-1",
            "tenants",
            "tenant-1",
            [CreateEvent(
                new TenantCreated("tenant-1", "Replayed Name", null, timestamp),
                "evt-created",
                timestamp,
                sequenceNumber: replaySequence)]);

        _ = await CreateHandler(stateStore).ProjectAsync(request);

        TenantReadModel saved = (TenantReadModel)stateStore.TrySaveAttempts
            .Single(attempt => attempt.Key == TenantProjectionKey)
            .Value;
        saved.ProjectionVersion.ShouldBe(TenantProjectionVersionFormat.SequencePrefix + "11");
        saved.TenantId.ShouldBe("tenant-1");
        saved.Name.ShouldBe("Persisted Name");
        saved.ProjectedAt.ShouldBe(persistedProjectedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("legacy-etag-opaque")]
    public async Task ProjectAsync_AppliesEventOverLegacyOrMissingAggregateVersionAsync(string? persistedVersion) {
        var stateStore = new ScriptedTenantProjectionStateStore();
        stateStore.EnqueueRead<TenantReadModel>(
            TenantProjectionKey,
            new TenantReadModel {
                TenantId = "tenant-1",
                Name = "Before",
                ProjectionVersion = persistedVersion,
            },
            "tenant-etag-1");
        stateStore.EnqueueTrySave(TenantProjectionKey, true);
        EnqueueSuccessfulAuditSave(stateStore);
        stateStore.EnqueueRead<TenantIndexReadModel>(TenantIndexKey, null, null);
        stateStore.EnqueueTrySave(TenantIndexKey, true);
        DateTimeOffset timestamp = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);
        ProjectionRequest request = new(
            "tenant-1",
            "tenants",
            "tenant-1",
            [CreateEvent(
                new TenantUpdated("tenant-1", "After", null, timestamp),
                "evt-updated",
                timestamp,
                sequenceNumber: 12)]);

        _ = await new TenantProjectionHandler(
                stateStore,
                NullLogger<TenantProjectionHandler>.Instance,
                new FixedTimeProvider(ProjectionTime))
            .ProjectAsync(request);

        TenantReadModel saved = (TenantReadModel)stateStore.TrySaveAttempts
            .Single(attempt => attempt.Key == TenantProjectionKey)
            .Value;
        saved.Name.ShouldBe("After");
        saved.ProjectionVersion.ShouldBe(TenantProjectionVersionFormat.SequencePrefix + "12");
        saved.ProjectedAt.ShouldBe(ProjectionTime);
    }

    [Fact]
    public async Task ProjectAsync_RetryAndMergeDoNotMoveProjectedAtBackwardAsync() {
        DateTimeOffset newerProjectionTime = ProjectionTime.AddMinutes(5);
        var stateStore = new ScriptedTenantProjectionStateStore();
        stateStore.EnqueueRead<TenantReadModel>(
            TenantProjectionKey,
            new TenantReadModel { ProjectedAt = ProjectionTime.AddMinutes(-5) },
            "tenant-etag-1");
        stateStore.EnqueueRead<TenantReadModel>(
            TenantProjectionKey,
            new TenantReadModel {
                TenantId = "tenant-1",
                Name = "Concurrent Name",
                ProjectedAt = newerProjectionTime,
            },
            "tenant-etag-2");
        stateStore.EnqueueTrySave(TenantProjectionKey, false);
        stateStore.EnqueueTrySave(TenantProjectionKey, true);
        stateStore.EnqueueRead(
            TenantAuditProjectionKey,
            new TenantAuditReadModel { ProjectedAt = newerProjectionTime },
            "audit-etag-1");
        stateStore.EnqueueTrySave(TenantAuditProjectionKey, true);
        stateStore.EnqueueRead(
            TenantIndexKey,
            new TenantIndexReadModel { ProjectedAt = newerProjectionTime },
            "index-etag-1");
        stateStore.EnqueueTrySave(TenantIndexKey, true);

        _ = await new TenantProjectionHandler(
                stateStore,
                NullLogger<TenantProjectionHandler>.Instance,
                new FixedTimeProvider(ProjectionTime))
            .ProjectAsync(CreateTenantCreatedRequest("tenant-1", "Acme", "evt-1"));

        ((TenantReadModel)stateStore.TrySaveAttempts.Last(a => a.Key == TenantProjectionKey).Value)
            .ProjectedAt.ShouldBe(newerProjectionTime);
        ((TenantAuditReadModel)stateStore.TrySaveAttempts.Single(a => a.Key == TenantAuditProjectionKey).Value)
            .ProjectedAt.ShouldBe(newerProjectionTime);
        ((TenantIndexReadModel)stateStore.TrySaveAttempts.Single(a => a.Key == TenantIndexKey).Value)
            .ProjectedAt.ShouldBe(newerProjectionTime);
    }

    [Fact]
    public async Task ProjectAsync_ExistingTenantStateUsesLoadedETagAndFirstWriteOptionsAsync() {
        TenantReadModel existing = new() {
            TenantId = "tenant-1",
            Name = "Prior",
        };
        var stateStore = new ScriptedTenantProjectionStateStore();
        stateStore.EnqueueRead(TenantProjectionKey, existing, "tenant-etag-1");
        stateStore.EnqueueTrySave(TenantProjectionKey, true);
        EnqueueSuccessfulAuditSave(stateStore);
        stateStore.EnqueueRead<TenantIndexReadModel>(TenantIndexKey, null, null);
        stateStore.EnqueueTrySave(TenantIndexKey, true);
        ProjectionRequest request = CreateTenantCreatedRequest("tenant-1", "Acme", "evt-1");

        _ = await CreateHandler(stateStore).ProjectAsync(request);

        SaveAttempt tenantSave = stateStore.TrySaveAttempts.Single(a => a.Key == TenantProjectionKey);
        tenantSave.ETag.ShouldBe("tenant-etag-1");
        TenantReadModel saved = (TenantReadModel)tenantSave.Value;
        saved.ShouldBeSameAs(existing);
        saved.Name.ShouldBe("Acme");
    }

    [Fact]
    public async Task ProjectAsync_MissingTenantStateUsesNoETagAndFirstWriteOptionsAsync() {
        var stateStore = new ScriptedTenantProjectionStateStore();
        stateStore.EnqueueRead<TenantReadModel>(TenantProjectionKey, null, null);
        stateStore.EnqueueTrySave(TenantProjectionKey, true);
        EnqueueSuccessfulAuditSave(stateStore);
        stateStore.EnqueueRead<TenantIndexReadModel>(TenantIndexKey, null, null);
        stateStore.EnqueueTrySave(TenantIndexKey, true);
        ProjectionRequest request = CreateTenantCreatedRequest("tenant-1", "Acme", "evt-1");

        _ = await CreateHandler(stateStore).ProjectAsync(request);

        SaveAttempt tenantSave = stateStore.TrySaveAttempts.Single(a => a.Key == TenantProjectionKey);
        tenantSave.ETag.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task ProjectAsync_TenantStateConflictReloadsStateAndRetriesExactlyOnceAsync() {
        var stateStore = new ScriptedTenantProjectionStateStore();
        stateStore.EnqueueRead<TenantReadModel>(TenantProjectionKey, new TenantReadModel(), "tenant-etag-1");
        stateStore.EnqueueRead<TenantReadModel>(
            TenantProjectionKey,
            new TenantReadModel {
                TenantId = "tenant-1",
                Name = "Concurrent Name",
            },
            "tenant-etag-2");
        stateStore.EnqueueTrySave(TenantProjectionKey, false);
        stateStore.EnqueueTrySave(TenantProjectionKey, true);
        EnqueueSuccessfulAuditSave(stateStore);
        stateStore.EnqueueRead<TenantIndexReadModel>(TenantIndexKey, null, null);
        stateStore.EnqueueTrySave(TenantIndexKey, true);
        ProjectionRequest request = CreateTenantCreatedRequest("tenant-1", "Acme", "evt-1");

        _ = await CreateHandler(stateStore).ProjectAsync(request);

        stateStore.ReadCalls.Count(c => c.Key == TenantProjectionKey).ShouldBe(2);
        stateStore.TrySaveAttempts.Count(a => a.Key == TenantProjectionKey).ShouldBe(2);
        stateStore.TrySaveAttempts[0].ETag.ShouldBe("tenant-etag-1");
        stateStore.TrySaveAttempts[1].ETag.ShouldBe("tenant-etag-2");
        ((TenantReadModel)stateStore.TrySaveAttempts[1].Value).Name.ShouldBe("Acme");
    }

    [Fact]
    public async Task ProjectAsync_TenantIndexConflictPreservesReloadedExistingTenantsAsync() {
        var stateStore = new ScriptedTenantProjectionStateStore();
        stateStore.EnqueueRead<TenantReadModel>(TenantProjectionKey, null, null);
        stateStore.EnqueueTrySave(TenantProjectionKey, true);
        EnqueueSuccessfulAuditSave(stateStore);
        stateStore.EnqueueRead<TenantIndexReadModel>(TenantIndexKey, CreateIndex("tenant-a", "Existing A"), "index-etag-1");
        stateStore.EnqueueRead<TenantIndexReadModel>(TenantIndexKey, CreateIndex("tenant-b", "Existing B"), "index-etag-2");
        stateStore.EnqueueTrySave(TenantIndexKey, false);
        stateStore.EnqueueTrySave(TenantIndexKey, true);
        ProjectionRequest request = CreateTenantCreatedRequest("tenant-1", "Acme", "evt-1");

        _ = await CreateHandler(stateStore).ProjectAsync(request);

        stateStore.TrySaveAttempts.Count(a => a.Key == TenantIndexKey).ShouldBe(2);
        TenantIndexReadModel savedIndex = (TenantIndexReadModel)stateStore.TrySaveAttempts.Last(a => a.Key == TenantIndexKey).Value;
        savedIndex.Tenants.ShouldContainKey("tenant-b");
        savedIndex.Tenants.ShouldContainKey("tenant-1");
        savedIndex.Tenants.ShouldNotContainKey("tenant-a");
        savedIndex.Tenants["tenant-1"].Name.ShouldBe("Acme");
    }

    [Fact]
    public async Task ProjectAsync_RetryExhaustionThrowsAfterMaxAttemptsAsync() {
        var stateStore = new ScriptedTenantProjectionStateStore();
        stateStore.EnqueueRead<TenantReadModel>(TenantProjectionKey, null, "tenant-etag-1");
        stateStore.EnqueueRead<TenantReadModel>(TenantProjectionKey, null, "tenant-etag-2");
        stateStore.EnqueueRead<TenantReadModel>(TenantProjectionKey, null, "tenant-etag-3");
        stateStore.EnqueueTrySave(TenantProjectionKey, false);
        stateStore.EnqueueTrySave(TenantProjectionKey, false);
        stateStore.EnqueueTrySave(TenantProjectionKey, false);
        ProjectionRequest request = CreateTenantCreatedRequest("tenant-1", "Acme", "evt-1");

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(
            () => CreateHandler(stateStore).ProjectAsync(request));

        exception.Message.ShouldContain(TenantProjectionKey);
        stateStore.ReadCalls.Count(c => c.Key == TenantProjectionKey).ShouldBe(3);
        stateStore.TrySaveAttempts.Count(a => a.Key == TenantProjectionKey).ShouldBe(3);
        stateStore.TrySaveAttempts.ShouldAllBe(a => a.Key == TenantProjectionKey);
    }

    [Fact]
    public async Task ProjectAsync_IndexRetryExhaustionAfterTenantSaveThrowsAsync() {
        var stateStore = new ScriptedTenantProjectionStateStore();
        stateStore.EnqueueRead<TenantReadModel>(TenantProjectionKey, null, null);
        stateStore.EnqueueTrySave(TenantProjectionKey, true);
        EnqueueSuccessfulAuditSave(stateStore);
        stateStore.EnqueueRead<TenantIndexReadModel>(TenantIndexKey, null, "index-etag-1");
        stateStore.EnqueueRead<TenantIndexReadModel>(TenantIndexKey, null, "index-etag-2");
        stateStore.EnqueueRead<TenantIndexReadModel>(TenantIndexKey, null, "index-etag-3");
        stateStore.EnqueueTrySave(TenantIndexKey, false);
        stateStore.EnqueueTrySave(TenantIndexKey, false);
        stateStore.EnqueueTrySave(TenantIndexKey, false);
        ProjectionRequest request = CreateTenantCreatedRequest("tenant-1", "Acme", "evt-1");

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(
            () => CreateHandler(stateStore).ProjectAsync(request));

        exception.Message.ShouldContain(TenantIndexKey);
        stateStore.TrySaveAttempts.Count(a => a.Key == TenantProjectionKey).ShouldBe(1);
        stateStore.TrySaveAttempts.Count(a => a.Key == TenantIndexKey).ShouldBe(3);
    }

    [Fact]
    public async Task ProjectAsync_WritesTenantAuditStateAsync() {
        var stateStore = new ScriptedTenantProjectionStateStore();
        stateStore.EnqueueRead<TenantReadModel>(TenantProjectionKey, null, null);
        stateStore.EnqueueTrySave(TenantProjectionKey, true);
        stateStore.EnqueueRead<TenantAuditReadModel>(TenantAuditProjectionKey, null, null);
        stateStore.EnqueueTrySave(TenantAuditProjectionKey, true);
        stateStore.EnqueueRead<TenantIndexReadModel>(TenantIndexKey, null, null);
        stateStore.EnqueueTrySave(TenantIndexKey, true);
        DateTimeOffset timestamp = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);
        ProjectionRequest request = new(
            "tenant-1",
            "tenants",
            "tenant-1",
            [CreateEvent(new TenantCreated("tenant-1", "Acme", null, timestamp), "evt-1", timestamp)]);

        _ = await CreateHandler(stateStore).ProjectAsync(request);

        SaveAttempt auditSave = stateStore.TrySaveAttempts.Single(a => a.Key == TenantAuditProjectionKey);
        auditSave.StoreName.ShouldBe(StateStoreName);
        auditSave.ETag.ShouldBe(string.Empty);
        auditSave.Value.ShouldBeOfType<TenantAuditReadModel>();
        TenantAuditReadModel model = (TenantAuditReadModel)auditSave.Value;
        model.Entries.Count.ShouldBe(1);
        model.Entries[0].EventId.ShouldBe("evt-1");
        model.Entries[0].ActorId.ShouldBe("actor-1");
    }

    [Fact]
    public async Task ProjectAsync_AuditStateConflictReloadsAndMergesEntriesByEventIdAsync() {
        var stateStore = new ScriptedTenantProjectionStateStore();
        stateStore.EnqueueRead<TenantReadModel>(TenantProjectionKey, null, null);
        stateStore.EnqueueTrySave(TenantProjectionKey, true);
        stateStore.EnqueueRead(TenantAuditProjectionKey, CreateAuditModel(
            CreateAuditEntry("evt-existing", "UserAddedToTenant", new DateTimeOffset(2026, 5, 14, 9, 0, 0, TimeSpan.Zero))),
            "audit-etag-1");
        stateStore.EnqueueRead(TenantAuditProjectionKey, CreateAuditModel(
            CreateAuditEntry("evt-existing", "UserAddedToTenant", new DateTimeOffset(2026, 5, 14, 9, 0, 0, TimeSpan.Zero)),
            CreateAuditEntry("evt-concurrent", "UserRemovedFromTenant", new DateTimeOffset(2026, 5, 14, 10, 0, 0, TimeSpan.Zero))),
            "audit-etag-2");
        stateStore.EnqueueTrySave(TenantAuditProjectionKey, false);
        stateStore.EnqueueTrySave(TenantAuditProjectionKey, true);
        stateStore.EnqueueRead<TenantIndexReadModel>(TenantIndexKey, null, null);
        stateStore.EnqueueTrySave(TenantIndexKey, true);
        ProjectionRequest request = CreateAccessChangeRequest();

        _ = await CreateHandler(stateStore).ProjectAsync(request);

        stateStore.ReadCalls.Count(c => c.Key == TenantAuditProjectionKey).ShouldBe(2);
        List<SaveAttempt> auditSaves = stateStore.TrySaveAttempts.Where(a => a.Key == TenantAuditProjectionKey).ToList();
        auditSaves.Count.ShouldBe(2);
        auditSaves[0].ETag.ShouldBe("audit-etag-1");
        auditSaves[1].ETag.ShouldBe("audit-etag-2");
        TenantAuditReadModel saved = (TenantAuditReadModel)auditSaves[1].Value;
        saved.Entries.Select(e => e.EventId).ShouldBe([
            "evt-existing",
            "evt-concurrent",
            "evt-added",
            "evt-removed",
            "evt-role",
        ]);
        saved.Entries.Select(e => e.EventId).Distinct(StringComparer.Ordinal).Count().ShouldBe(saved.Entries.Count);
    }

    [Fact]
    public async Task ProjectAsync_AuditRetryExhaustionThrowsWithoutSuccessfulProjectionAsync() {
        var stateStore = new ScriptedTenantProjectionStateStore();
        stateStore.EnqueueRead<TenantReadModel>(TenantProjectionKey, null, null);
        stateStore.EnqueueTrySave(TenantProjectionKey, true);
        stateStore.EnqueueRead<TenantAuditReadModel>(TenantAuditProjectionKey, null, "audit-etag-1");
        stateStore.EnqueueRead<TenantAuditReadModel>(TenantAuditProjectionKey, null, "audit-etag-2");
        stateStore.EnqueueRead<TenantAuditReadModel>(TenantAuditProjectionKey, null, "audit-etag-3");
        stateStore.EnqueueTrySave(TenantAuditProjectionKey, false);
        stateStore.EnqueueTrySave(TenantAuditProjectionKey, false);
        stateStore.EnqueueTrySave(TenantAuditProjectionKey, false);
        ProjectionRequest request = CreateAccessChangeRequest();

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(
            () => CreateHandler(stateStore).ProjectAsync(request));

        exception.Message.ShouldContain(TenantAuditProjectionKey);
        stateStore.ReadCalls.Count(c => c.Key == TenantAuditProjectionKey).ShouldBe(3);
        stateStore.TrySaveAttempts.Count(a => a.Key == TenantAuditProjectionKey).ShouldBe(3);
        stateStore.TrySaveAttempts
            .ShouldAllBe(a => a.Key == TenantProjectionKey || a.Key == TenantAuditProjectionKey);
        stateStore.ReadCalls.ShouldNotContain(c => c.Key == TenantIndexKey);
    }

    [Fact]
    public async Task ProjectAsync_AuditMergeSkipsMalformedPayloadsAndPreservesValidEventsDuringRetryAsync() {
        var stateStore = new ScriptedTenantProjectionStateStore();
        stateStore.EnqueueRead<TenantReadModel>(TenantProjectionKey, null, null);
        stateStore.EnqueueTrySave(TenantProjectionKey, true);
        stateStore.EnqueueRead<TenantAuditReadModel>(TenantAuditProjectionKey, null, "audit-etag-1");
        stateStore.EnqueueRead(TenantAuditProjectionKey, CreateAuditModel(
            CreateAuditEntry("evt-concurrent", "UserRemovedFromTenant", new DateTimeOffset(2026, 5, 14, 10, 0, 0, TimeSpan.Zero))),
            "audit-etag-2");
        stateStore.EnqueueTrySave(TenantAuditProjectionKey, false);
        stateStore.EnqueueTrySave(TenantAuditProjectionKey, true);
        stateStore.EnqueueRead<TenantIndexReadModel>(TenantIndexKey, null, null);
        stateStore.EnqueueTrySave(TenantIndexKey, true);
        DateTimeOffset timestamp = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);
        ProjectionRequest request = new(
            "tenant-1",
            "tenants",
            "tenant-1",
            [
                CreateEvent(new UserAddedToTenant("tenant-1", "user-1", TenantRole.TenantReader), "evt-added", timestamp.AddMinutes(1)),
                new ProjectionEventDto(
                    typeof(GlobalAdministratorSet).FullName!,
                    "{not valid json"u8.ToArray(),
                    "json",
                    1,
                    timestamp.AddMinutes(2),
                    "corr-1",
                    "evt-malformed",
                    "actor-1"),
            ]);

        _ = await CreateHandler(stateStore).ProjectAsync(request);

        TenantAuditReadModel saved = (TenantAuditReadModel)stateStore.TrySaveAttempts.Last(a => a.Key == TenantAuditProjectionKey).Value;
        saved.Entries.Select(e => e.EventId).ShouldBe(["evt-concurrent", "evt-added"]);
        saved.Entries.ShouldNotContain(e => e.EventId == "evt-malformed");
    }

    [Theory]
    [InlineData(null, "actor-1")]
    [InlineData("evt-added", null)]
    public async Task ProjectAsync_AuditInvariantFailureAbortsBeforeAnyStateStoreWriteAsync(string? messageId, string? userId) {
        // Invariant validation now runs before the tenant TrySaveStateAsync, so a
        // missing MessageId/UserId aborts the whole batch without any state-store
        // read or write. No reads are enqueued — the scripted store would throw
        // KeyNotFoundException if the handler attempted to read any key.
        var stateStore = new ScriptedTenantProjectionStateStore();
        DateTimeOffset timestamp = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);
        ProjectionRequest request = new(
            "tenant-1",
            "tenants",
            "tenant-1",
            [
                CreateEvent(new UserRemovedFromTenant("tenant-1", "user-2"), "evt-removed", timestamp),
                CreateEvent(new UserAddedToTenant("tenant-1", "user-1", TenantRole.TenantReader), messageId, timestamp.AddMinutes(1), userId),
            ]);

        _ = await Should.ThrowAsync<InvalidOperationException>(() => CreateHandler(stateStore).ProjectAsync(request));

        stateStore.ReadCalls.ShouldBeEmpty();
        stateStore.TrySaveAttempts.ShouldBeEmpty();
    }

    [Fact]
    public async Task ProjectAsync_AuditDuplicateEventIdKeepsPersistedEntryAuthoritativeAsync() {
        var stateStore = new ScriptedTenantProjectionStateStore();
        stateStore.EnqueueRead<TenantReadModel>(TenantProjectionKey, null, null);
        stateStore.EnqueueTrySave(TenantProjectionKey, true);
        TenantAuditEntry persisted = CreateAuditEntry(
            "evt-added",
            "UserRemovedFromTenant",
            new DateTimeOffset(2026, 5, 14, 10, 0, 0, TimeSpan.Zero),
            new Dictionary<string, string>(StringComparer.Ordinal) { ["source"] = "persisted" });
        stateStore.EnqueueRead(TenantAuditProjectionKey, CreateAuditModel(persisted), "audit-etag-1");
        stateStore.EnqueueTrySave(TenantAuditProjectionKey, true);
        stateStore.EnqueueRead<TenantIndexReadModel>(TenantIndexKey, null, null);
        stateStore.EnqueueTrySave(TenantIndexKey, true);
        ProjectionRequest request = new(
            "tenant-1",
            "tenants",
            "tenant-1",
            [CreateEvent(new UserAddedToTenant("tenant-1", "user-1", TenantRole.TenantOwner), "evt-added", persisted.Timestamp)]);

        _ = await CreateHandler(stateStore).ProjectAsync(request);

        TenantAuditReadModel saved = (TenantAuditReadModel)stateStore.TrySaveAttempts.Single(a => a.Key == TenantAuditProjectionKey).Value;
        TenantAuditEntry savedEntry = saved.Entries.Single();
        // Persisted entry must remain authoritative on EventId collision: identifying
        // fields (EventType, NarrativePayload, ActorId) must be the persisted values,
        // NOT the incoming UserAddedToTenant/TenantOwner replay payload. Reference
        // equality alone would pass trivially if MergeAuditState were ever silently
        // changed to overwrite persisted with semantically-equal incoming entries.
        savedEntry.EventId.ShouldBe(persisted.EventId);
        savedEntry.EventType.ShouldBe(persisted.EventType);
        savedEntry.NarrativePayload["source"].ShouldBe("persisted");
        savedEntry.ActorId.ShouldBe(persisted.ActorId);
    }

    [Fact]
    public async Task ProjectAsync_ReplayAfterLaterProjectionFailureDoesNotDuplicateAuditEntriesAsync() {
        ProjectionRequest request = CreateAccessChangeRequest();
        var firstStateStore = new ScriptedTenantProjectionStateStore();
        firstStateStore.EnqueueRead<TenantReadModel>(TenantProjectionKey, null, null);
        firstStateStore.EnqueueTrySave(TenantProjectionKey, true);
        EnqueueSuccessfulAuditSave(firstStateStore);
        firstStateStore.EnqueueRead<TenantIndexReadModel>(TenantIndexKey, null, "index-etag-1");
        firstStateStore.EnqueueRead<TenantIndexReadModel>(TenantIndexKey, null, "index-etag-2");
        firstStateStore.EnqueueRead<TenantIndexReadModel>(TenantIndexKey, null, "index-etag-3");
        firstStateStore.EnqueueTrySave(TenantIndexKey, false);
        firstStateStore.EnqueueTrySave(TenantIndexKey, false);
        firstStateStore.EnqueueTrySave(TenantIndexKey, false);

        _ = await Should.ThrowAsync<InvalidOperationException>(() => CreateHandler(firstStateStore).ProjectAsync(request));

        TenantAuditReadModel persistedAudit = (TenantAuditReadModel)firstStateStore.TrySaveAttempts.Single(a => a.Key == TenantAuditProjectionKey).Value;
        var replayStateStore = new ScriptedTenantProjectionStateStore();
        replayStateStore.EnqueueRead<TenantReadModel>(TenantProjectionKey, null, null);
        replayStateStore.EnqueueTrySave(TenantProjectionKey, true);
        replayStateStore.EnqueueRead(TenantAuditProjectionKey, persistedAudit, "audit-etag-replay");
        replayStateStore.EnqueueTrySave(TenantAuditProjectionKey, true);
        replayStateStore.EnqueueRead<TenantIndexReadModel>(TenantIndexKey, null, null);
        replayStateStore.EnqueueTrySave(TenantIndexKey, true);

        _ = await CreateHandler(replayStateStore).ProjectAsync(request);

        TenantAuditReadModel replaySaved = (TenantAuditReadModel)replayStateStore.TrySaveAttempts.Single(a => a.Key == TenantAuditProjectionKey).Value;
        replaySaved.Entries.Select(e => e.EventId).ShouldBe(["evt-added", "evt-removed", "evt-role"]);
    }

    [Fact]
    public async Task ProjectAsync_WithPreCancelledTokenThrowsBeforeStateStoreAccessAsync() {
        var stateStore = new ScriptedTenantProjectionStateStore();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        OperationCanceledException exception = await Should.ThrowAsync<OperationCanceledException>(
            () => CreateHandler(stateStore).ProjectAsync(CreateTenantCreatedRequest("tenant-1", "Acme", "evt-1"), cancellation.Token));

        exception.CancellationToken.ShouldBe(cancellation.Token);
        stateStore.ReadCalls.ShouldBeEmpty();
        stateStore.TrySaveAttempts.ShouldBeEmpty();
    }

    [Fact]
    public async Task ProjectAsync_PassesCancellationTokenToProjectionStateReadsAndSavesAsync() {
        var stateStore = new ScriptedTenantProjectionStateStore();
        stateStore.EnqueueRead<TenantReadModel>(TenantProjectionKey, null, null);
        stateStore.EnqueueTrySave(TenantProjectionKey, true);
        EnqueueSuccessfulAuditSave(stateStore);
        stateStore.EnqueueRead<TenantIndexReadModel>(TenantIndexKey, null, null);
        stateStore.EnqueueTrySave(TenantIndexKey, true);
        using var cancellation = new CancellationTokenSource();

        _ = await CreateHandler(stateStore).ProjectAsync(
            CreateTenantCreatedRequest("tenant-1", "Acme", "evt-1"),
            cancellation.Token);

        stateStore.ReadCalls.ShouldAllBe(c => c.CancellationToken == cancellation.Token);
        stateStore.TrySaveAttempts.ShouldAllBe(a => a.CancellationToken == cancellation.Token);
    }

    [Fact]
    public async Task ProjectAsync_CancellationAfterTenantSaveStopsBeforeLaterProjectionWritesAsync() {
        var stateStore = new ScriptedTenantProjectionStateStore();
        stateStore.EnqueueRead<TenantReadModel>(TenantProjectionKey, null, null);
        stateStore.EnqueueTrySave(TenantProjectionKey, true);
        using var cancellation = new CancellationTokenSource();
        stateStore.CancelAfterTrySave(TenantProjectionKey, cancellation);

        _ = await Should.ThrowAsync<OperationCanceledException>(
            () => CreateHandler(stateStore).ProjectAsync(
                CreateTenantCreatedRequest("tenant-1", "Acme", "evt-1"),
                cancellation.Token));

        stateStore.ReadCalls.Single().Key.ShouldBe(TenantProjectionKey);
        stateStore.TrySaveAttempts.Single().Key.ShouldBe(TenantProjectionKey);
        stateStore.TrySaveAttempts.ShouldNotContain(a => a.Key == TenantAuditProjectionKey);
        stateStore.TrySaveAttempts.ShouldNotContain(a => a.Key == TenantIndexKey);
    }

    private static TenantProjectionHandler CreateHandler(ScriptedTenantProjectionStateStore stateStore) =>
        new(stateStore, NullLogger<TenantProjectionHandler>.Instance);

    private static void EnqueueSuccessfulAuditSave(ScriptedTenantProjectionStateStore stateStore) {
        stateStore.EnqueueRead<TenantAuditReadModel>(TenantAuditProjectionKey, null, null);
        stateStore.EnqueueTrySave(TenantAuditProjectionKey, true);
    }

    private static TenantIndexReadModel CreateIndex(string tenantId, string name) {
        var model = new TenantIndexReadModel();
        model.Apply(new TenantCreated(tenantId, name, null, DateTimeOffset.UtcNow));
        return model;
    }

    private static ProjectionRequest CreateTenantCreatedRequest(string tenantId, string name, string messageId) {
        DateTimeOffset timestamp = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);
        return new(
            tenantId,
            "tenants",
            tenantId,
            [CreateEvent(new TenantCreated(tenantId, name, null, timestamp), messageId, timestamp)]);
    }

    private static ProjectionRequest CreateAccessChangeRequest() {
        DateTimeOffset timestamp = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);
        return new(
            "tenant-1",
            "tenants",
            "tenant-1",
            [
                CreateEvent(new UserAddedToTenant("tenant-1", "user-1", TenantRole.TenantReader), "evt-added", timestamp.AddMinutes(1)),
                CreateEvent(new UserRemovedFromTenant("tenant-1", "user-2"), "evt-removed", timestamp.AddMinutes(2)),
                CreateEvent(new UserRoleChanged("tenant-1", "user-3", TenantRole.TenantReader, TenantRole.TenantOwner), "evt-role", timestamp.AddMinutes(2)),
            ]);
    }

    private static ProjectionEventDto CreateEvent(
        IEventPayload payload,
        string? messageId,
        DateTimeOffset timestamp,
        string? userId = "actor-1",
        long sequenceNumber = 1) =>
        new(
            payload.GetType().FullName!,
            JsonSerializer.SerializeToUtf8Bytes(payload, payload.GetType()),
            "json",
            sequenceNumber,
            timestamp,
            "corr-1",
            messageId,
            userId);

    private static TenantAuditEntry CreateAuditEntry(
        string eventId,
        string eventType,
        DateTimeOffset timestamp,
        IReadOnlyDictionary<string, string>? narrativePayload = null) =>
        new(
            eventId,
            eventType,
            AuditEventCategory.Access,
            "actor-1",
            timestamp,
            "tenant-1",
            narrativePayload ?? new Dictionary<string, string>(StringComparer.Ordinal) { ["userId"] = "existing-user" });

    private static TenantAuditReadModel CreateAuditModel(params TenantAuditEntry[] entries) => new() {
        Entries = [.. entries],
    };

    private sealed class ScriptedTenantProjectionStateStore : IReadModelStore {
        private readonly Dictionary<string, Queue<object>> _reads = [];
        private readonly Dictionary<string, Queue<bool>> _trySaveResults = [];
        private CancellationTokenSource? _cancelAfterTrySaveSource;
        private string? _cancelAfterTrySaveKey;

        public List<SaveAttempt> PlainSaveAttempts { get; } = [];

        public List<ReadCall> ReadCalls { get; } = [];

        public List<SaveAttempt> TrySaveAttempts { get; } = [];

        public List<EraseAttempt> TryEraseAttempts { get; } = [];

        public void EnqueueRead<TValue>(string key, TValue? value, string? etag)
            where TValue : class {
            if (!_reads.TryGetValue(key, out Queue<object>? queue)) {
                queue = new Queue<object>();
                _reads[key] = queue;
            }

            queue.Enqueue(new ReadModelEntry<TValue>(value, etag));
        }

        public void EnqueueTrySave(string key, bool result) {
            if (!_trySaveResults.TryGetValue(key, out Queue<bool>? queue)) {
                queue = new Queue<bool>();
                _trySaveResults[key] = queue;
            }

            queue.Enqueue(result);
        }

        public void CancelAfterTrySave(string key, CancellationTokenSource cancellationSource) {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentNullException.ThrowIfNull(cancellationSource);

            _cancelAfterTrySaveKey = key;
            _cancelAfterTrySaveSource = cancellationSource;
        }

        public Task<ReadModelEntry<TValue>> GetAsync<TValue>(
            string storeName,
            string key,
            CancellationToken cancellationToken = default)
            where TValue : class {
            ReadCalls.Add(new ReadCall(storeName, key, typeof(TValue), cancellationToken));
            Queue<object> queue = _reads[key];
            return Task.FromResult((ReadModelEntry<TValue>)queue.Dequeue());
        }

        public Task SaveAsync<TValue>(
            string storeName,
            string key,
            TValue value,
            CancellationToken cancellationToken = default)
            where TValue : class {
            PlainSaveAttempts.Add(new SaveAttempt(storeName, key, value, string.Empty, typeof(TValue), cancellationToken));
            return Task.CompletedTask;
        }

        public Task<bool> TrySaveAsync<TValue>(
            string storeName,
            string key,
            TValue value,
            string etag,
            CancellationToken cancellationToken = default)
            where TValue : class {
            TrySaveAttempts.Add(new SaveAttempt(storeName, key, value, etag, typeof(TValue), cancellationToken));
            Queue<bool> queue = _trySaveResults[key];
            bool result = queue.Dequeue();
            if (string.Equals(_cancelAfterTrySaveKey, key, StringComparison.Ordinal)) {
                _cancelAfterTrySaveSource?.Cancel();
            }

            return Task.FromResult(result);
        }

        public Task<bool> TryEraseAsync(
            string storeName,
            string key,
            string etag,
            CancellationToken cancellationToken = default) {
            TryEraseAttempts.Add(new EraseAttempt(storeName, key, etag, cancellationToken));
            return Task.FromResult(true);
        }
    }

    private sealed record ReadCall(string StoreName, string Key, Type ValueType, CancellationToken CancellationToken);

    private sealed record SaveAttempt(
        string StoreName,
        string Key,
        object Value,
        string ETag,
        Type ValueType,
        CancellationToken CancellationToken);

    private sealed record EraseAttempt(
        string StoreName,
        string Key,
        string ETag,
        CancellationToken CancellationToken);

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
