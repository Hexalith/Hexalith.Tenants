using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;

using Dapr.Actors;
using Dapr.Actors.Runtime;
using Dapr.Client;

using Hexalith.EventStore.Server.Actors;
using Hexalith.EventStore.Server.Queries;
using Hexalith.Tenants.CommandApi.Actors;
using Hexalith.Tenants.CommandApi.Telemetry;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Server.Projections;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Telemetry;

[Collection("Telemetry")]
public class TenantsProjectionActorTelemetryTests : IDisposable
{
    private readonly ActivityListener _activityListener;
    private readonly MeterListener _meterListener;
    private readonly List<Activity> _activities = [];
    private readonly List<(string Name, double Value, KeyValuePair<string, object?>[] Tags)> _metrics = [];

    public TenantsProjectionActorTelemetryTests()
    {
        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == TenantActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => _activities.Add(activity),
        };
        ActivitySource.AddActivityListener(_activityListener);

        _meterListener = new MeterListener();
        _meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == TenantMetrics.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        _meterListener.SetMeasurementEventCallback<double>((instrument, value, tags, _) =>
        {
            _metrics.Add((instrument.Name, value, tags.ToArray()));
        });
        _meterListener.Start();
    }

    public void Dispose()
    {
        _activityListener.Dispose();
        _meterListener.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task QueryAsync_KnownQuery_ShouldEmitSpanAndMetric()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        SetupTenantState(
            daprClient,
            "tenant-1",
            CreateTenantReadModel(members: new() { ["user-1"] = TenantRole.TenantOwner }));
        SetupNoGlobalAdmin(daprClient);

        TenantsProjectionActor actor = CreateActor(daprClient);

        QueryResult result = await actor.QueryAsync(CreateEnvelope("get-tenant"));

        result.Success.ShouldBeTrue();
        _activities.Count.ShouldBeGreaterThanOrEqualTo(1);

        Activity activity = FindActivity("get-tenant");
        activity.GetTagItem(TenantActivitySource.TagQueryType).ShouldBe("get-tenant");
        activity.Status.ShouldBe(ActivityStatusCode.Unset);

        (string Name, double Value, KeyValuePair<string, object?>[] Tags) metric = FindMetric(
            "tenants.projection.query.duration",
            tags => HasTag(tags, "query_type", "get-tenant"));
        Dictionary<string, object?> tags = metric.Tags.ToDictionary(t => t.Key, t => t.Value);
        tags["query_type"].ShouldBe("get-tenant");
    }

    [Fact]
    public async Task QueryAsync_UnknownQuery_ShouldSanitizeMetricDimension()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantsProjectionActor actor = CreateActor(daprClient);

        QueryResult result = await actor.QueryAsync(CreateEnvelope("unknown-query"));

        result.Success.ShouldBeFalse();
        (string Name, double Value, KeyValuePair<string, object?>[] Tags) metric = FindMetric(
            "tenants.projection.query.duration",
            tags => HasTag(tags, "query_type", "unknown"));
        Dictionary<string, object?> tags = metric.Tags.ToDictionary(t => t.Key, t => t.Value);
        tags["query_type"].ShouldBe("unknown");
    }

    [Fact]
    public async Task QueryAsync_WhenHandlerThrows_ShouldMarkActivityAsErrorAndRecordMetric()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        daprClient.GetStateAsync<TenantReadModel>(
                TenantsProjectionActor.StateStoreName,
                TenantsProjectionActor.TenantProjectionKeyPrefix + "tenant-1")
            .ThrowsAsync(new HttpRequestException("State store unavailable"));

        TenantsProjectionActor actor = CreateActor(daprClient);

        await Should.ThrowAsync<HttpRequestException>(() => actor.QueryAsync(CreateEnvelope("get-tenant")));

        Activity activity = FindActivity("get-tenant");
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.StatusDescription.ShouldBe("State store unavailable");

        _ = FindMetric(
            "tenants.projection.query.duration",
            tags => HasTag(tags, "query_type", "get-tenant"));
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

    private static TenantReadModel CreateTenantReadModel(
        string tenantId = "tenant-1",
        string name = "Test Tenant",
        Dictionary<string, TenantRole>? members = null)
    {
        TenantReadModel model = new();
        model.Apply(new Contracts.Events.TenantCreated(tenantId, name, "Test", DateTimeOffset.UtcNow));
        if (members is not null)
        {
            foreach (KeyValuePair<string, TenantRole> member in members)
            {
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

    private static TenantsProjectionActor CreateActor(DaprClient daprClient)
    {
        ActorHost host = ActorHost.CreateForTest<TenantsProjectionActor>(
            new ActorTestOptions { ActorId = new ActorId("test-actor") });
        IETagService eTagService = Substitute.For<IETagService>();
        ILogger<TenantsProjectionActor> logger = NullLogger<TenantsProjectionActor>.Instance;
        return new TenantsProjectionActor(host, eTagService, daprClient, logger);
    }

    private static void SetupTenantState(DaprClient daprClient, string tenantId, TenantReadModel model)
    {
        daprClient.GetStateAsync<TenantReadModel>(
            TenantsProjectionActor.StateStoreName,
            TenantsProjectionActor.TenantProjectionKeyPrefix + tenantId)
            .Returns(Task.FromResult(model)!);
    }

    private static void SetupNoGlobalAdmin(DaprClient daprClient)
    {
        daprClient.GetStateAsync<GlobalAdministratorReadModel>(
            TenantsProjectionActor.StateStoreName,
            TenantsProjectionActor.GlobalAdminProjectionKey)
            .Returns(Task.FromResult<GlobalAdministratorReadModel>(null!)!);
    }
}