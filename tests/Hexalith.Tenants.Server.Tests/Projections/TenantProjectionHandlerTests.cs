using System.Text.Json;

using Dapr.Client;

using Hexalith.Tenants.Contracts.Enums;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Projections;
using Hexalith.Tenants.Server.Projections;

using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Projections;

public class TenantProjectionHandlerTests {
    private const string StateStoreName = "statestore";
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
        stateStore.EnqueueRead<TenantIndexReadModel>(TenantIndexKey, null, null);
        stateStore.EnqueueTrySave(TenantIndexKey, true);
        DateTimeOffset timestamp = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);
        ProjectionRequest request = new(
            "tenant-1",
            "tenants",
            "tenant-1",
            [CreateEvent(new TenantCreated("tenant-1", "Acme", null, timestamp), "evt-1", timestamp)]);

        _ = await CreateHandler(stateStore).ProjectAsync(request);

        SaveAttempt auditSave = stateStore.PlainSaveAttempts.Single(a => a.Key == "audit:tenant-1");
        auditSave.StoreName.ShouldBe(StateStoreName);
        auditSave.Value.ShouldBeOfType<TenantAuditReadModel>();
        TenantAuditReadModel model = (TenantAuditReadModel)auditSave.Value;
        (model != null
                && model.Entries.Count == 1
                && model.Entries[0].EventId == "evt-1"
                && model.Entries[0].ActorId == "actor-1").ShouldBeTrue();
    }

    private static TenantProjectionHandler CreateHandler(ScriptedTenantProjectionStateStore stateStore) =>
        new(stateStore, NullLogger<TenantProjectionHandler>.Instance);

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

    private static ProjectionEventDto CreateEvent(IEventPayload payload, string messageId, DateTimeOffset timestamp) =>
        new(
            payload.GetType().FullName!,
            JsonSerializer.SerializeToUtf8Bytes(payload, payload.GetType()),
            "json",
            1,
            timestamp,
            "corr-1",
            messageId,
            "actor-1");

    private sealed class ScriptedTenantProjectionStateStore : ITenantProjectionStateStore {
        private readonly Dictionary<string, Queue<object>> _reads = [];
        private readonly Dictionary<string, Queue<bool>> _trySaveResults = [];

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

        public Task<ProjectionStateRead<TValue>> GetStateAndETagAsync<TValue>(
            string storeName,
            string key,
            CancellationToken cancellationToken)
            where TValue : class {
            ReadCalls.Add(new ReadCall(storeName, key, typeof(TValue)));
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
                typeof(TValue)));
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
            TrySaveAttempts.Add(new SaveAttempt(storeName, key, value, etag, stateOptions, typeof(TValue)));
            Queue<bool> queue = _trySaveResults[key];
            return Task.FromResult(queue.Dequeue());
        }
    }

    private sealed record ReadCall(string StoreName, string Key, Type ValueType);

    private sealed record SaveAttempt(
        string StoreName,
        string Key,
        object Value,
        string ETag,
        StateOptions StateOptions,
        Type ValueType);
}
