using System.Text.Json;

using Dapr.Client;

using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
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
        tenantSave.StateOptions.Concurrency.ShouldBe(ConcurrencyMode.FirstWrite);
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
        tenantSave.StateOptions.Concurrency.ShouldBe(ConcurrencyMode.FirstWrite);
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

        exception.Message.ShouldContain("tenant read-model");
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

        exception.Message.ShouldContain("tenant index");
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
        auditSave.StateOptions.Concurrency.ShouldBe(ConcurrencyMode.FirstWrite);
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

        exception.Message.ShouldContain("tenant audit");
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
        stateStore.PlainSaveAttempts.ShouldBeEmpty();
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
        stateStore.PlainSaveAttempts.ShouldBeEmpty();
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
        string? userId = "actor-1") =>
        new(
            payload.GetType().FullName!,
            JsonSerializer.SerializeToUtf8Bytes(payload, payload.GetType()),
            "json",
            1,
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

    private sealed class ScriptedTenantProjectionStateStore : ITenantProjectionStateStore {
        private readonly Dictionary<string, Queue<object>> _reads = [];
        private readonly Dictionary<string, Queue<bool>> _trySaveResults = [];
        private CancellationTokenSource? _cancelAfterTrySaveSource;
        private string? _cancelAfterTrySaveKey;

        public List<SaveAttempt> PlainSaveAttempts { get; } = [];

        public List<ReadCall> ReadCalls { get; } = [];

        public List<SaveAttempt> TrySaveAttempts { get; } = [];

        public void EnqueueRead<TValue>(string key, TValue? value, string? etag)
            where TValue : class {
            if (!_reads.TryGetValue(key, out Queue<object>? queue)) {
                queue = new Queue<object>();
                _reads[key] = queue;
            }

            queue.Enqueue(new ProjectionStateRead<TValue>(value, etag));
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

        public Task<ProjectionStateRead<TValue>> GetStateAndETagAsync<TValue>(
            string storeName,
            string key,
            CancellationToken cancellationToken)
            where TValue : class {
            ReadCalls.Add(new ReadCall(storeName, key, typeof(TValue), cancellationToken));
            Queue<object> queue = _reads[key];
            return Task.FromResult((ProjectionStateRead<TValue>)queue.Dequeue());
        }

        public Task SaveStateAsync<TValue>(
            string storeName,
            string key,
            TValue value,
            StateOptions? stateOptions = null,
            IReadOnlyDictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default)
            where TValue : class {
            PlainSaveAttempts.Add(new SaveAttempt(
                storeName,
                key,
                value,
                string.Empty,
                stateOptions ?? new StateOptions(),
                typeof(TValue),
                cancellationToken));
            return Task.CompletedTask;
        }

        public Task<bool> TrySaveStateAsync<TValue>(
            string storeName,
            string key,
            TValue value,
            string etag,
            StateOptions stateOptions,
            IReadOnlyDictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default)
            where TValue : class {
            TrySaveAttempts.Add(new SaveAttempt(storeName, key, value, etag, stateOptions, typeof(TValue), cancellationToken));
            Queue<bool> queue = _trySaveResults[key];
            bool result = queue.Dequeue();
            if (string.Equals(_cancelAfterTrySaveKey, key, StringComparison.Ordinal)) {
                _cancelAfterTrySaveSource?.Cancel();
            }

            return Task.FromResult(result);
        }
    }

    private sealed record ReadCall(string StoreName, string Key, Type ValueType, CancellationToken CancellationToken);

    private sealed record SaveAttempt(
        string StoreName,
        string Key,
        object Value,
        string ETag,
        StateOptions StateOptions,
        Type ValueType,
        CancellationToken CancellationToken);
}
