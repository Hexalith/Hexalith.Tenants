using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.Testing.Fakes;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Projections;
using Hexalith.Tenants.Queries.Handlers;
using Hexalith.Tenants.Server.Projections;
using Hexalith.Tenants.Telemetry;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

/// <summary>Checks separated audit evidence against the platform store's persisted JSON end state.</summary>
public sealed class AuditStreamPersistenceTests
{
    private const string StoreName = "statestore";
    private static readonly DateTimeOffset Timestamp = DateTimeOffset.UtcNow;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };
    private readonly IQueryCursorCodec _codec = new QueryCursorCodec(new EphemeralDataProtectionProvider(), "audit-stream-tests");

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task BothOrdersAndRepeatedShorterReplayRetainBothOwners(bool tenantFirst)
    {
        var store = AuthorizedStore();
        ProjectionRequest tenant = TenantRequest("system", TenantEvent("system", "shared-event", 1),
            TenantEvent("system", "tenant-second", 2));
        ProjectionRequest global = GlobalRequest(GlobalEvent("shared-event", 1), GlobalEvent("global-second", 2));
        var tenantWriter = new TenantProjectionHandler(store);
        var globalWriter = new GlobalAdministratorProjectionHandler(store);
        if (tenantFirst)
        {
            await tenantWriter.ProjectAsync(tenant);
            await globalWriter.ProjectAsync(global);
        }
        else
        {
            await globalWriter.ProjectAsync(global);
            await tenantWriter.ProjectAsync(tenant);
        }

        await tenantWriter.ProjectAsync(tenant);
        await globalWriter.ProjectAsync(global);
        await tenantWriter.ProjectAsync(TenantRequest("system", tenant.Events[0], tenant.Events[0]));
        await globalWriter.ProjectAsync(GlobalRequest(global.Events[0], global.Events[0]));

        Snapshot(store, TenantAuditStreams.SystemTenantKey).Entries.Select(row => row.EventId)
            .ShouldBe(["shared-event", "tenant-second"]);
        TenantAuditReadModel globalState = Snapshot(store, TenantAuditStreams.GlobalAdministratorsKey);
        globalState.Entries.Select(row => row.EventId).ShouldBe(["global-second", "shared-event"]);
        globalState.ProjectionVersion.ShouldBe("tenant-sequence:2");
        store.Snapshot<TenantAuditReadModel>(StoreName, TenantAuditStreams.LegacySystemKey).ShouldBeNull();
        Page(await Read(store)).Items.Count.ShouldBe(4);
        Page(await Read(store, TenantAuditStreams.Tenant)).Items.Count.ShouldBe(2);
        (await Read(store, TenantAuditStreams.GlobalAdministrators)).Metadata!.ProjectionVersion.ShouldBe("tenant-sequence:2");
    }

    [Theory]
    [InlineData("global")]
    [InlineData("tenant")]
    [InlineData("mixed")]
    public async Task LegacySubsetsRemainReadableAndOriginalStoredVersionIsUntouched(string shape)
    {
        var store = AuthorizedStore();
        TenantAuditReadModel legacy = Model(shape is "tenant" ? [TenantRow()] : shape is "global"
            ? [GlobalRow()] : [TenantRow(), GlobalRow()]);
        legacy.ProjectionVersion = "tenant-sequence:9999";
        store.SeedRaw(StoreName, TenantAuditStreams.LegacySystemKey, legacy);
        ReadModelEntry<TenantAuditReadModel> before = await store.GetAsync<TenantAuditReadModel>(StoreName, TenantAuditStreams.LegacySystemKey);
        string original = JsonSerializer.Serialize(before.Value, JsonOptions);

        QueryResult combined = await Read(store);
        combined.Success.ShouldBeTrue();
        Page(combined).Items.Count.ShouldBe(legacy.Entries.Count);
        combined.Metadata!.ProjectionVersion.ShouldStartWith("audit-composite:");
        (await Read(store, TenantAuditStreams.GlobalAdministrators)).Metadata!.ProjectionVersion.ShouldBeNull();
        Page(await Read(store, TenantAuditStreams.Tenant)).Items.ShouldAllBe(row => row.EventType == nameof(TenantCreated));
        Page(await Read(store, TenantAuditStreams.GlobalAdministrators)).Items.ShouldAllBe(row => row.EventType == nameof(GlobalAdministratorSet));

        await new TenantProjectionHandler(store).ProjectAsync(TenantRequest("system", TenantEvent("system", "new-tenant", 1)));
        await new GlobalAdministratorProjectionHandler(store).ProjectAsync(GlobalRequest(GlobalEvent("new-global", 1)));
        ReadModelEntry<TenantAuditReadModel> after = await store.GetAsync<TenantAuditReadModel>(StoreName, TenantAuditStreams.LegacySystemKey);
        after.ETag.ShouldBe(before.ETag);
        JsonSerializer.Serialize(after.Value, JsonOptions).ShouldBe(original);
        Page(await Read(store)).Items.Count.ShouldBe(legacy.Entries.Count + 2);
        (await Read(store, TenantAuditStreams.GlobalAdministrators)).Metadata!.ProjectionVersion
            .ShouldBe(shape == "tenant" ? "tenant-sequence:1" : null);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("null-row")]
    [InlineData("null-entries")]
    [InlineData("foreign-tenant")]
    [InlineData("duplicate-conflict")]
    [InlineData("source-conflict")]
    [InlineData("impostor-type")]
    [InlineData("missing-first-sequence-conflict")]
    [InlineData("missing-first-correlation-conflict")]
    public async Task AmbiguousLegacyFailsClosedWithoutChangingPersistedEvidence(string shape)
    {
        var store = AuthorizedStore();
        TenantAuditEntry row = TenantRow();
        TenantAuditReadModel legacy = Model([row]);
        switch (shape)
        {
            case "unknown": legacy.Entries = [row with { EventType = "UnknownEvent" }]; break;
            case "null-row": legacy.Entries = [null!]; break;
            case "null-entries": legacy.Entries = null!; break;
            case "foreign-tenant": legacy.Entries = [row with { TenantId = "another-tenant" }]; break;
            case "duplicate-conflict": legacy.Entries.Add(row with { ActorId = "different-actor" }); break;
            case "source-conflict": store.SeedRaw(StoreName, TenantAuditStreams.SystemTenantKey,
                Model([row with { ActorId = "different-actor" }])); break;
            case "impostor-type": legacy.Entries = [row with { EventType = "Forged.TenantCreated" }]; break;
            case "missing-first-sequence-conflict": legacy.Entries = [GlobalRow(),
                GlobalRow() with { AggregateSequence = 1 }, GlobalRow() with { AggregateSequence = 2 }]; break;
            case "missing-first-correlation-conflict": legacy.Entries = [GlobalRow(),
                GlobalRow() with { CommandCorrelationId = "first-proof" }, GlobalRow() with { CommandCorrelationId = "second-proof" }]; break;
        }

        store.SeedRaw(StoreName, TenantAuditStreams.LegacySystemKey, legacy);
        ReadModelEntry<TenantAuditReadModel> original = await store.GetAsync<TenantAuditReadModel>(StoreName, TenantAuditStreams.LegacySystemKey);
        foreach (string? stream in new[] { null, TenantAuditStreams.Tenant, TenantAuditStreams.GlobalAdministrators })
        {
            QueryResult response = await Read(store, stream);
            if (shape == "source-conflict" && stream == TenantAuditStreams.GlobalAdministrators)
            {
                response.Success.ShouldBeTrue();
                Page(response).Items.ShouldBeEmpty();
                continue;
            }
            response.Success.ShouldBeFalse();
            response.ErrorMessage.ShouldBe("AuditUnavailable");
            response.PayloadBytes.ShouldBeNull();
            response.Metadata.ShouldBeNull();
        }

        ReadModelEntry<TenantAuditReadModel> after = await store.GetAsync<TenantAuditReadModel>(StoreName, TenantAuditStreams.LegacySystemKey);
        after.ETag.ShouldBe(original.ETag);
        JsonSerializer.Serialize(after.Value, JsonOptions).ShouldBe(JsonSerializer.Serialize(original.Value, JsonOptions));
    }

    [Fact]
    public async Task LegacyAndDedicatedDuplicatesDeduplicatePerOwnerAndMaintainCanonicalProof()
    {
        var store = AuthorizedStore();
        await new TenantProjectionHandler(store).ProjectAsync(TenantRequest("system", TenantEvent("system", "same", 1)));
        await new GlobalAdministratorProjectionHandler(store).ProjectAsync(GlobalRequest(GlobalEvent("same", 1)));
        TenantAuditEntry tenant = Snapshot(store, TenantAuditStreams.SystemTenantKey).Entries.Single();
        TenantAuditEntry global = Snapshot(store, TenantAuditStreams.GlobalAdministratorsKey).Entries.Single();
        store.SeedRaw(StoreName, TenantAuditStreams.LegacySystemKey, Model([tenant, global, tenant, global]));

        QueryResult first = await Read(store, pageSize: 1);
        Page(first).Items.Count.ShouldBe(1);
        Page(first).HasMore.ShouldBeTrue();
        QueryResult second = await Read(store, cursor: Page(first).Cursor, pageSize: 1);
        Page(second).Items.Count.ShouldBe(1);
        Page(second).HasMore.ShouldBeFalse();
        Page(first).Items.Single().EventType.ShouldNotBe(Page(second).Items.Single().EventType);
        (await Read(store, TenantAuditStreams.GlobalAdministrators)).Metadata!.ProjectionVersion.ShouldBe("tenant-sequence:1");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CanonicalReplaySuppliesFreshnessAndProvenanceForHistoricalDuplicates(bool missingTimestamp)
    {
        var store = AuthorizedStore();
        await new GlobalAdministratorProjectionHandler(store).ProjectAsync(GlobalRequest(GlobalEvent("canonical", 1)));
        TenantAuditEntry canonical = Snapshot(store, TenantAuditStreams.GlobalAdministratorsKey).Entries.Single();
        TenantAuditReadModel legacy = Model([canonical with { CommandCorrelationId = null, AggregateSequence = null }]);
        legacy.ProjectedAt = missingTimestamp ? null : Timestamp.AddDays(-30);
        store.SeedRaw(StoreName, TenantAuditStreams.LegacySystemKey, legacy);
        ReadModelEntry<TenantAuditReadModel> original = await store.GetAsync<TenantAuditReadModel>(StoreName, TenantAuditStreams.LegacySystemKey);
        QueryResult global = await Read(store, TenantAuditStreams.GlobalAdministrators);
        global.Metadata!.ProjectionVersion.ShouldBe("tenant-sequence:1");
        global.Metadata.IsStale.ShouldBe(false);
        global.Metadata.Lifecycle.ShouldBe(ProjectionLifecycleState.Current);
        Page(global).Items.Single().AggregateSequence.ShouldBe(1);
        Page(global).Items.Single().CommandCorrelationId.ShouldBe("event-correlation");
        (await store.GetAsync<TenantAuditReadModel>(StoreName, TenantAuditStreams.LegacySystemKey)).ETag.ShouldBe(original.ETag);

        legacy.Entries = [canonical with { AggregateSequence = 999 }];
        store.SeedRaw(StoreName, TenantAuditStreams.LegacySystemKey, legacy);
        (await Read(store, TenantAuditStreams.GlobalAdministrators)).ErrorMessage.ShouldBe("AuditUnavailable");
    }

    [Fact]
    public async Task CombinedPagingPreservesOrdinalPrefixAndColonEventIds()
    {
        var store = AuthorizedStore();
        store.SeedRaw(StoreName, TenantAuditStreams.SystemTenantKey,
            Model([TenantRow("x"), TenantRow("x:A"), TenantRow("x:global-administrators"), TenantRow("x!"), TenantRow("x:tenant")]));
        store.SeedRaw(StoreName, TenantAuditStreams.GlobalAdministratorsKey, Model([GlobalRow("x")]));
        var seen = new List<(string Id, string Type)>();
        string? cursor = null;
        do
        {
            PaginatedResult<TenantAuditEntry> page = Page(await Read(store, cursor: cursor, pageSize: 1));
            TenantAuditEntry row = page.Items.Single();
            seen.Add((row.EventId, row.EventType));
            cursor = page.Cursor;
        }
        while (cursor is not null);
        seen.Select(row => row.Id).ShouldBe(["x", "x", "x!", "x:A", "x:global-administrators", "x:tenant"]);
        seen.Distinct().Count().ShouldBe(6);
    }

    [Fact]
    public async Task PreSeparationSystemCursorIsRejectedWithSafeRestartResult()
    {
        var store = AuthorizedStore();
        string oldScope = QueryCursorScope.Create().Add("requester", "admin").Add("tenant", "system")
            .Add("from", (DateTimeOffset?)null).Add("to", (DateTimeOffset?)null).Add("category", (string?)null).Build();
        string cursor = _codec.Encode(GetTenantAuditQuery.QueryType, oldScope, "00000000000000000001:event");
        (await Read(store, cursor: cursor)).ErrorMessage.ShouldBe(QueryAdapterFailureReason.InvalidCursor);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("")]
    [InlineData("Tenant")]
    [InlineData("global-administrators", "ordinary")]
    public async Task InvalidSelectorIsRejectedAndUnauthorizedCallerWins(string stream, string tenantId = "system")
    {
        var store = AuthorizedStore();
        (await Read(store, stream, tenantId: tenantId)).ErrorMessage.ShouldBe(QueryAdapterFailureReason.InvalidEnvelope);
        (await Read(store, stream, tenantId: tenantId, userId: "outsider")).ErrorMessage.ShouldBe(QueryAdapterFailureReason.Forbidden);
        store.Snapshot<TenantAuditReadModel>(StoreName, TenantAuditStreams.LegacySystemKey).ShouldBeNull();
    }

    [Fact]
    public async Task NonStringSelectorFailsWithSafeValidationResult()
    {
        var store = AuthorizedStore();
        QueryResult response = await Handler(store).ExecuteAsync(new QueryEnvelope("system", "tenants", "system",
            GetTenantAuditQuery.QueryType, JsonSerializer.SerializeToUtf8Bytes(new { stream = 123 }), "query-reference", "admin"), default);
        response.ErrorMessage.ShouldBe(QueryAdapterFailureReason.InvalidEnvelope);
    }

    [Fact]
    public async Task CursorSelectorAndFiltersRemainBound()
    {
        var store = AuthorizedStore();
        await new TenantProjectionHandler(store).ProjectAsync(TenantRequest("system", TenantEvent("system", "first", 1),
            TenantEvent("system", "second", 2)));
        QueryResult first = await Read(store, TenantAuditStreams.Tenant, pageSize: 1);
        string cursor = Page(first).Cursor!;
        Page(await Read(store, TenantAuditStreams.Tenant, cursor, 1)).Items.Count.ShouldBe(1);
        (await Read(store, cursor: cursor, pageSize: 1)).ErrorMessage.ShouldBe(QueryAdapterFailureReason.InvalidCursor);
        (await Read(store, TenantAuditStreams.GlobalAdministrators, cursor, 1)).ErrorMessage.ShouldBe(QueryAdapterFailureReason.InvalidCursor);
        QueryResult filtered = await Read(store, TenantAuditStreams.Tenant, from: Timestamp, to: Timestamp,
            category: AuditEventCategory.Administrative);
        Page(filtered).Items.Count.ShouldBe(2);
        Page(await Read(store, TenantAuditStreams.Tenant, category: AuditEventCategory.Access)).Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task CompositeValidatorTracksEverySourceAndSelector()
    {
        var store = AuthorizedStore();
        await new TenantProjectionHandler(store).ProjectAsync(TenantRequest("system", TenantEvent("system", "first", 1)));
        QueryResult before = await Read(store);
        string initial = before.Metadata!.ETag!;
        (await Read(store, TenantAuditStreams.Tenant)).Metadata!.ETag.ShouldNotBe(initial);
        await new GlobalAdministratorProjectionHandler(store).ProjectAsync(GlobalRequest(GlobalEvent("global-first", 1)));
        string globalChanged = (await Read(store)).Metadata!.ETag!;
        globalChanged.ShouldNotBe(initial);
        await new TenantProjectionHandler(store).ProjectAsync(TenantRequest("system", TenantEvent("system", "second", 2)));
        string tenantChanged = (await Read(store)).Metadata!.ETag!;
        tenantChanged.ShouldNotBe(globalChanged);
        TenantAuditReadModel legacy = Model([TenantRow("legacy-first")]);
        legacy.ProjectedAt = null;
        store.SeedRaw(StoreName, TenantAuditStreams.LegacySystemKey, legacy);
        QueryResult legacyChanged = await Read(store);
        legacyChanged.Metadata!.ETag.ShouldNotBe(tenantChanged);
        legacyChanged.Metadata.IsStale.ShouldBeNull();
        legacyChanged.Metadata.Lifecycle.ShouldBe(ProjectionLifecycleState.Unknown);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(5)]
    public async Task IncompleteGlobalHistoryNeverProvidesOrderedProof(long sequence)
    {
        var store = AuthorizedStore();
        await new GlobalAdministratorProjectionHandler(store).ProjectAsync(GlobalRequest(GlobalEvent("partial", sequence)));
        Snapshot(store, TenantAuditStreams.GlobalAdministratorsKey).Entries.Count.ShouldBe(1);
        (await Read(store, TenantAuditStreams.GlobalAdministrators)).Metadata!.ProjectionVersion.ShouldBeNull();
        (await Read(store)).Metadata!.ProjectionVersion.ShouldStartWith("audit-composite:");
    }

    [Fact]
    public async Task GlobalMergeRetriesAgainstPersistedCompetingWriter()
    {
        var store = AuthorizedStore();
        store.ConcurrentWriteBeforeTrySave = () =>
        {
            store.ConcurrentWriteBeforeTrySave = null;
            store.SeedRaw(StoreName, TenantAuditStreams.GlobalAdministratorsKey, Model([GlobalRow("concurrent")]));
        };
        await new GlobalAdministratorProjectionHandler(store).ProjectAsync(GlobalRequest(GlobalEvent("incoming", 1)));
        Snapshot(store, TenantAuditStreams.GlobalAdministratorsKey).Entries.Select(row => row.EventId)
            .ShouldBe(["concurrent", "incoming"]);
    }

    [Theory]
    [InlineData("global-administrators:singleton")]
    [InlineData("audit-global-administrators:singleton")]
    [InlineData("audit-tenants:system")]
    [InlineData("system:extra")]
    public async Task PrefixLookingTenantIdsCannotShareDedicatedOwnerKeys(string tenantId)
    {
        var store = AuthorizedStore();
        await new GlobalAdministratorProjectionHandler(store).ProjectAsync(GlobalRequest(GlobalEvent("global", 1)));
        await new TenantProjectionHandler(store).ProjectAsync(TenantRequest(tenantId, TenantEvent(tenantId, "tenant", 1)));
        Snapshot(store, TenantAuditStreams.GetTenantKey(tenantId)).Entries.Single().TenantId.ShouldBe(tenantId);
        Snapshot(store, TenantAuditStreams.GlobalAdministratorsKey).Entries.Single().EventId.ShouldBe("global");
        Page(await Read(store, tenantId: tenantId)).Items.Single().TenantId.ShouldBe(tenantId);
        store.Snapshot<TenantAuditReadModel>(StoreName, TenantAuditStreams.SystemTenantKey).ShouldBeNull();
    }

    [Fact]
    public async Task IncomingWrongOwnerAndConflictingDuplicateFailBeforeProjectionWrites()
    {
        var store = AuthorizedStore();
        await Should.ThrowAsync<InvalidOperationException>(() => new TenantProjectionHandler(store)
            .ProjectAsync(TenantRequest("system", GlobalEvent("wrong-owner", 1))));
        await Should.ThrowAsync<InvalidOperationException>(() => new GlobalAdministratorProjectionHandler(store)
            .ProjectAsync(GlobalRequest(TenantEvent("system", "wrong-owner", 1))));
        ProjectionEventDto first = TenantEvent("system", "duplicate", 1);
        await Should.ThrowAsync<InvalidOperationException>(() => new TenantProjectionHandler(store)
            .ProjectAsync(TenantRequest("system", first, first with { UserId = "different" })));
        store.Snapshot<TenantReadModel>(StoreName, "projection:tenants:system").ShouldBeNull();
        store.Snapshot<TenantAuditReadModel>(StoreName, TenantAuditStreams.SystemTenantKey).ShouldBeNull();
        store.Snapshot<TenantAuditReadModel>(StoreName, TenantAuditStreams.GlobalAdministratorsKey).ShouldBeNull();
    }

    [Fact]
    public async Task CancellationLeavesPersistedAuditEvidenceUnchanged()
    {
        var store = AuthorizedStore();
        store.SeedRaw(StoreName, TenantAuditStreams.LegacySystemKey, Model([TenantRow()]));
        ReadModelEntry<TenantAuditReadModel> original = await store.GetAsync<TenantAuditReadModel>(StoreName, TenantAuditStreams.LegacySystemKey);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(() => new TenantProjectionHandler(store)
            .ProjectAsync(TenantRequest("system", TenantEvent("system", "cancelled", 1)), cancellation.Token));
        await Should.ThrowAsync<OperationCanceledException>(() => new GlobalAdministratorProjectionHandler(store)
            .ProjectAsync(GlobalRequest(GlobalEvent("cancelled", 1)), cancellation.Token));
        await Should.ThrowAsync<OperationCanceledException>(() => Read(store, cancellationToken: cancellation.Token));
        (await store.GetAsync<TenantAuditReadModel>(StoreName, TenantAuditStreams.LegacySystemKey)).ETag.ShouldBe(original.ETag);
        store.Snapshot<TenantAuditReadModel>(StoreName, TenantAuditStreams.SystemTenantKey).ShouldBeNull();
        store.Snapshot<TenantAuditReadModel>(StoreName, TenantAuditStreams.GlobalAdministratorsKey).ShouldBeNull();
    }

    [Fact]
    public async Task CancellationAfterGuardedAuditCommitPreservesEvidenceAndStopsSingletonWrite()
    {
        var store = AuthorizedStore();
        store.SeedRaw(StoreName, TenantAuditStreams.LegacySystemKey, Model([TenantRow()]));
        ReadModelEntry<TenantAuditReadModel> legacy = await store.GetAsync<TenantAuditReadModel>(StoreName, TenantAuditStreams.LegacySystemKey);
        using var cancellation = new CancellationTokenSource();
        store.ConcurrentWriteBeforeTrySave = () => cancellation.Cancel();
        ProjectionEventDto incoming = Event(new GlobalAdministratorSet("system", "another-admin", "actor", Timestamp), "committed-audit", 1);
        await Should.ThrowAsync<OperationCanceledException>(() => new GlobalAdministratorProjectionHandler(store)
            .ProjectAsync(GlobalRequest(incoming), cancellation.Token));
        Snapshot(store, TenantAuditStreams.GlobalAdministratorsKey).Entries.Single().EventId.ShouldBe("committed-audit");
        store.Snapshot<GlobalAdministratorReadModel>(StoreName, GlobalAdministratorProjectionHandler.GlobalAdministratorsProjectionKey)!
            .Administrators.ShouldBe(["admin"]);
        (await store.GetAsync<TenantAuditReadModel>(StoreName, TenantAuditStreams.LegacySystemKey)).ETag.ShouldBe(legacy.ETag);
    }

    private GetTenantAuditQueryHandler Handler(InMemoryReadModelStore store)
        => new(store, _codec, new TenantTelemetry(new EventStoreDomainDiagnostics("tenants")),
            NullLogger<GetTenantAuditQueryHandler>.Instance);

    private Task<QueryResult> Read(InMemoryReadModelStore store, string? stream = null, string? cursor = null,
        int pageSize = 100, string tenantId = "system", string userId = "admin", DateTimeOffset? from = null,
        DateTimeOffset? to = null, AuditEventCategory? category = null, CancellationToken cancellationToken = default)
        => Handler(store).ExecuteAsync(new QueryEnvelope("system", "tenants", tenantId, GetTenantAuditQuery.QueryType,
            JsonSerializer.SerializeToUtf8Bytes(new { stream, cursor, pageSize, from, to, category }, JsonOptions),
            "query-reference", userId), cancellationToken);

    private static InMemoryReadModelStore AuthorizedStore()
    {
        var store = new InMemoryReadModelStore();
        store.SeedRaw(StoreName, GlobalAdministratorProjectionHandler.GlobalAdministratorsProjectionKey,
            new GlobalAdministratorReadModel { Administrators = ["admin"] });
        return store;
    }

    private static TenantAuditReadModel Snapshot(InMemoryReadModelStore store, string key)
        => store.Snapshot<TenantAuditReadModel>(StoreName, key)!;

    private static TenantAuditReadModel Model(List<TenantAuditEntry> rows)
        => new() { Entries = rows, ProjectedAt = DateTimeOffset.UtcNow };

    private static PaginatedResult<TenantAuditEntry> Page(QueryResult result)
    {
        result.Success.ShouldBeTrue();
        return JsonSerializer.Deserialize<PaginatedResult<TenantAuditEntry>>(result.PayloadBytes!, JsonOptions)!;
    }

    private static TenantAuditEntry TenantRow(string id = "legacy-tenant")
        => new(id, nameof(TenantCreated), AuditEventCategory.Administrative, "actor", Timestamp, "system",
            new Dictionary<string, string> { ["name"] = "System tenant" });

    private static TenantAuditEntry GlobalRow(string id = "legacy-global")
        => new(id, nameof(GlobalAdministratorSet), AuditEventCategory.Access, "actor", Timestamp, "system",
            new Dictionary<string, string> { ["userId"] = "admin" });

    private static ProjectionRequest TenantRequest(string tenantId, params ProjectionEventDto[] events)
        => new(tenantId, "tenants", tenantId, events);

    private static ProjectionRequest GlobalRequest(params ProjectionEventDto[] events)
        => new("system", "global-administrators", "global-administrators", events);

    private static ProjectionEventDto TenantEvent(string tenantId, string id, long sequence)
        => Event(new TenantCreated(tenantId, "System tenant", null, Timestamp), id, sequence);

    private static ProjectionEventDto GlobalEvent(string id, long sequence)
        => Event(new GlobalAdministratorSet("system", "admin", "actor", Timestamp), id, sequence);

    private static ProjectionEventDto Event<T>(T payload, string id, long sequence)
        => new(typeof(T).FullName!, JsonSerializer.SerializeToUtf8Bytes(payload), "json", sequence, Timestamp,
            "event-correlation", id, "actor");
}
