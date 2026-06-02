using System.Diagnostics;
using System.Diagnostics.Metrics;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Queries.Handlers;
using Hexalith.Tenants.Server.Projections;
using Hexalith.Tenants.Server.Tests.Support;
using Hexalith.Tenants.Telemetry;

using Microsoft.AspNetCore.DataProtection;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Telemetry;

/// <summary>
/// Telemetry coverage for the tenant query handlers (formerly the <c>TenantsProjectionActor</c>): the
/// span + duration metric emitted by <c>TenantQueryHandlerBase</c> on success, forbidden, and failure.
/// </summary>
[Collection("Telemetry")]
public class TenantsProjectionActorTelemetryTests : IDisposable {
    private readonly ActivityListener _activityListener;
    private readonly MeterListener _meterListener;
    private readonly List<Activity> _activities = [];
    private readonly List<(string Name, double Value, KeyValuePair<string, object?>[] Tags)> _metrics = [];

    public TenantsProjectionActorTelemetryTests() {
        _activityListener = new ActivityListener {
            ShouldListenTo = source => source.Name == TenantActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => _activities.Add(activity),
        };
        ActivitySource.AddActivityListener(_activityListener);

        _meterListener = new MeterListener {
            InstrumentPublished = (instrument, listener) => {
                if (instrument.Meter.Name == TenantMetrics.MeterName) {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };
        _meterListener.SetMeasurementEventCallback<double>((instrument, value, tags, _) => _metrics.Add((instrument.Name, value, tags.ToArray())));
        _meterListener.Start();
    }

    public void Dispose() {
        _activityListener.Dispose();
        _meterListener.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task QueryAsync_KnownQuery_ShouldEmitSpanAndMetric() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        SetupTenantState(
            store,
            "tenant-1",
            CreateTenantReadModel(members: new() { ["user-1"] = TenantRole.TenantOwner }));
        SetupNoGlobalAdmin(store);

        QueryResult result = await ExecuteAsync(store, CreateEnvelope("get-tenant"));

        result.Success.ShouldBeTrue();
        _activities.Count.ShouldBeGreaterThanOrEqualTo(1);

        Activity activity = FindActivity("get-tenant");
        activity.GetTagItem(TenantActivitySource.TagQueryType).ShouldBe("get-tenant");
        activity.GetTagItem(TenantActivitySource.TagOutcome).ShouldBe("success");
        activity.GetTagItem(TenantActivitySource.TagStage).ShouldBe("projection-query");
        activity.GetTagItem(TenantActivitySource.TagCorrelationId).ShouldNotBeNull();
        activity.GetTagItem(TenantActivitySource.TagDomain).ShouldBe("tenants");
        activity.GetTagItem(TenantActivitySource.TagAggregateId).ShouldBe("tenant-1");
        activity.Status.ShouldBe(ActivityStatusCode.Unset);

        (string Name, double Value, KeyValuePair<string, object?>[] Tags) = FindMetric(
            "tenants.projection.query.duration",
            tags => HasTag(tags, "query_type", "get-tenant") && HasTag(tags, "outcome", "success"));
        Dictionary<string, object?> tags = Tags.ToDictionary(t => t.Key, t => t.Value);
        tags["query_type"].ShouldBe("get-tenant");
        tags["outcome"].ShouldBe("success");
        tags.Keys.ShouldNotContain("tenant_id");
        tags.Keys.ShouldNotContain("aggregate_id");
        tags.Keys.ShouldNotContain("correlation_id");
        tags.Keys.ShouldNotContain("causation_id");
        tags.Keys.ShouldNotContain("user_id");
        tags.Keys.ShouldNotContain("message_id");
    }

    [Fact]
    public async Task QueryAsync_ForbiddenQueryResult_ShouldRecordForbiddenOutcome() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        SetupNoGlobalAdmin(store);

        QueryResult result = await ExecuteAsync(store, CreateEnvelope("get-tenant", userId: "user-2"));

        result.Success.ShouldBeFalse();
        Activity activity = FindActivity("get-tenant");
        activity.GetTagItem(TenantActivitySource.TagOutcome).ShouldBe("forbidden");

        _ = FindMetric(
            "tenants.projection.query.duration",
            tags => HasTag(tags, "query_type", "get-tenant") && HasTag(tags, "outcome", "forbidden"));
    }

    [Fact]
    public async Task QueryAsync_WhenHandlerThrows_ShouldMarkActivityAsErrorAndRecordMetric() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        _ = store.GetAsync<TenantReadModel>(
                TenantQueryHandlerBase.StateStoreName,
                TenantQueryHandlerBase.TenantProjectionKeyPrefix + "tenant-1")
            .ThrowsAsync(new HttpRequestException("State store unavailable"));

        _ = await Should.ThrowAsync<HttpRequestException>(() => ExecuteAsync(store, CreateEnvelope("get-tenant")));

        Activity activity = FindActivity("get-tenant");
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.StatusDescription.ShouldBe("State store unavailable");
        activity.GetTagItem(TenantActivitySource.TagOutcome).ShouldBe("failure");

        _ = FindMetric(
            "tenants.projection.query.duration",
            tags => HasTag(tags, "query_type", "get-tenant") && HasTag(tags, "outcome", "failure"));
    }

    private (string Name, double Value, KeyValuePair<string, object?>[] Tags) FindMetric(
        string metricName,
        Func<KeyValuePair<string, object?>[], bool> predicate)
        => _metrics.Last(metric => metric.Name == metricName && predicate(metric.Tags));

    private Activity FindActivity(string queryType)
        => _activities.Last(activity =>
            activity.OperationName == TenantActivitySource.QueryExecute
            && Equals(activity.GetTagItem(TenantActivitySource.TagQueryType), queryType));

    private static bool HasTag(KeyValuePair<string, object?>[] tags, string key, object? value)
        => tags.Any(tag => tag.Key == key && Equals(tag.Value, value));

    private static Task<QueryResult> ExecuteAsync(IReadModelStore store, QueryEnvelope envelope)
        => TenantQueryTestHarness.ExecuteAsync(store, CreateCursorCodec(), envelope);

    private static IQueryCursorCodec CreateCursorCodec()
        => new QueryCursorCodec(new EphemeralDataProtectionProvider(), "Hexalith.Tenants.QueryCursor.v1");

    private static TenantReadModel CreateTenantReadModel(
        string tenantId = "tenant-1",
        string name = "Test Tenant",
        Dictionary<string, TenantRole>? members = null) {
        TenantReadModel model = new();
        model.Apply(new Contracts.Events.TenantCreated(tenantId, name, "Test", DateTimeOffset.UtcNow));
        if (members is not null) {
            foreach (KeyValuePair<string, TenantRole> member in members) {
                model.Apply(new Contracts.Events.UserAddedToTenant(tenantId, member.Key, member.Value));
            }
        }

        return model;
    }

    private static QueryEnvelope CreateEnvelope(
        string queryType,
        string userId = "user-1",
        string aggregateId = "tenant-1",
        string? entityId = null,
        byte[]? payload = null)
        => new(
            tenantId: "system",
            domain: "tenants",
            aggregateId: aggregateId,
            queryType: queryType,
            payload: payload ?? [],
            correlationId: Guid.NewGuid().ToString(),
            userId: userId,
            entityId: entityId);

    private static void SetupTenantState(IReadModelStore store, string tenantId, TenantReadModel model) => store.GetAsync<TenantReadModel>(
            TenantQueryHandlerBase.StateStoreName,
            TenantQueryHandlerBase.TenantProjectionKeyPrefix + tenantId)
            .Returns(Task.FromResult(new ReadModelEntry<TenantReadModel>(model, "etag-1")));

    private static void SetupNoGlobalAdmin(IReadModelStore store) => store.GetAsync<GlobalAdministratorReadModel>(
            TenantQueryHandlerBase.StateStoreName,
            TenantQueryHandlerBase.GlobalAdminProjectionKey)
            .Returns(Task.FromResult(new ReadModelEntry<GlobalAdministratorReadModel>(null, null)));
}
