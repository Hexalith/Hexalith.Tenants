using System.Diagnostics;
using System.Diagnostics.Metrics;

using Hexalith.EventStore.DomainService;
using Hexalith.Tenants.Telemetry;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Telemetry;

/// <summary>
/// Verifies the rehomed <see cref="TenantTelemetry"/> instruments. The domain no longer declares its own
/// <see cref="ActivitySource"/>/<see cref="Meter"/>: the source and meter are owned by the platform
/// <see cref="EventStoreDomainDiagnostics"/> under the convention name
/// <c>Hexalith.EventStore.Domain.tenants</c> (Epic A5). The bounded span names, tag keys, and
/// cardinality-sanitized metric dimensions stay with the domain.
/// </summary>
[Collection("Telemetry")]
public class TenantTelemetryTests : IDisposable {
    private static readonly string s_conventionName = EventStoreDomainTelemetry.ActivitySourceName("tenants");

    private readonly EventStoreDomainDiagnostics _diagnostics = new("tenants");
    private readonly TenantTelemetry _telemetry;
    private readonly ActivityListener _activityListener;
    private readonly MeterListener _meterListener;
    private readonly List<Activity> _activities = [];
    private readonly List<(string Name, double Value, KeyValuePair<string, object?>[] Tags)> _recordings = [];

    public TenantTelemetryTests() {
        _telemetry = new TenantTelemetry(_diagnostics);

        _activityListener = new ActivityListener {
            ShouldListenTo = source => source.Name == s_conventionName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => _activities.Add(activity),
        };
        ActivitySource.AddActivityListener(_activityListener);

        _meterListener = new MeterListener {
            InstrumentPublished = (instrument, listener) => {
                if (instrument.Meter.Name == s_conventionName) {
                    listener.EnableMeasurementEvents(instrument);
                }
            },
        };
        _meterListener.SetMeasurementEventCallback<double>((instrument, value, tags, _) => _recordings.Add((instrument.Name, value, tags.ToArray())));
        _meterListener.Start();
    }

    public void Dispose() {
        _activityListener.Dispose();
        _meterListener.Dispose();
        _diagnostics.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Diagnostics_ShouldUseConventionSourceName() {
        s_conventionName.ShouldBe("Hexalith.EventStore.Domain.tenants");
        _diagnostics.ActivitySource.Name.ShouldBe(s_conventionName);
        _diagnostics.Meter.Name.ShouldBe(s_conventionName);
    }

    [Fact]
    public void StartActivity_QueryExecute_ShouldCreateSpanWithCorrectName() {
        using Activity? activity = _telemetry.StartActivity(TenantTelemetry.QueryExecute);

        _ = activity.ShouldNotBeNull();
        activity.OperationName.ShouldBe("Tenants.Projection.Query");
        _activities.ShouldContain(activity);
    }

    [Fact]
    public void StartActivity_ProjectionProject_ShouldCreateSpanWithCorrectName() {
        using Activity? activity = _telemetry.StartActivity(TenantTelemetry.ProjectionProject);

        _ = activity.ShouldNotBeNull();
        activity.OperationName.ShouldBe("Tenants.Projection.Project");
    }

    [Fact]
    public void Activity_ShouldAcceptQueryTags() {
        using Activity? activity = _telemetry.StartActivity(TenantTelemetry.QueryExecute);

        _ = activity.ShouldNotBeNull();
        _ = activity.SetTag(TenantTelemetry.TagQueryType, "get-tenant");

        activity.GetTagItem(TenantTelemetry.TagQueryType).ShouldBe("get-tenant");
    }

    [Fact]
    public void Activity_ShouldAcceptProjectionDispatchTags() {
        using Activity? activity = _telemetry.StartActivity(TenantTelemetry.ProjectionProject);

        _ = activity.ShouldNotBeNull();
        _ = activity.SetTag(TenantTelemetry.TagStage, "projection-dispatch");
        _ = activity.SetTag(TenantTelemetry.TagTenantId, "system");
        _ = activity.SetTag(TenantTelemetry.TagDomain, "tenants");
        _ = activity.SetTag(TenantTelemetry.TagAggregateId, "tenant-1");
        _ = activity.SetTag(TenantTelemetry.TagProjectionType, "tenant");
        _ = activity.SetTag(TenantTelemetry.TagEventCount, 1);
        _ = activity.SetTag(TenantTelemetry.TagCausationIdStatus, "unavailable-from-projection-dto");
        _ = activity.SetTag(TenantTelemetry.TagOutcome, "completed");

        activity.GetTagItem(TenantTelemetry.TagStage).ShouldBe("projection-dispatch");
        activity.GetTagItem(TenantTelemetry.TagDomain).ShouldBe("tenants");
        activity.GetTagItem(TenantTelemetry.TagProjectionType).ShouldBe("tenant");
        activity.GetTagItem(TenantTelemetry.TagEventCount).ShouldBe(1);
        activity.GetTagItem(TenantTelemetry.TagCausationIdStatus).ShouldBe("unavailable-from-projection-dto");
        activity.GetTagItem(TenantTelemetry.TagOutcome).ShouldBe("completed");
    }

    [Fact]
    public void Activity_ErrorStatus_ShouldBeSettable() {
        using Activity? activity = _telemetry.StartActivity(TenantTelemetry.QueryExecute);

        _ = activity.ShouldNotBeNull();
        _ = activity.SetStatus(ActivityStatusCode.Error, "Test error");

        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.StatusDescription.ShouldBe("Test error");
    }

    [Fact]
    public void RecordQueryDuration_ShouldRecordWithQueryTypeAndOutcome() {
        _telemetry.RecordQueryDuration(15.3, "get-tenant", "forbidden");

        (string Name, double Value, KeyValuePair<string, object?>[] Tags) =
            FindRecording(
                "tenants.projection.query.duration",
                tags => HasTag(tags, "query_type", "get-tenant") && HasTag(tags, "outcome", "forbidden"));
        Name.ShouldBe("tenants.projection.query.duration");
        Value.ShouldBe(15.3);

        Dictionary<string, object?> tags = Tags.ToDictionary(t => t.Key, t => t.Value);
        tags["query_type"].ShouldBe("get-tenant");
        tags["outcome"].ShouldBe("forbidden");
    }

    [Fact]
    public void RecordQueryDuration_WithUnknownType_ShouldSanitizeToUnknown() {
        _telemetry.RecordQueryDuration(1.0, "MaliciousQueryType", "weird-outcome");

        (string Name, double _, KeyValuePair<string, object?>[] Tags) =
            FindRecording(
                "tenants.projection.query.duration",
                tags => HasTag(tags, "query_type", "unknown") && HasTag(tags, "outcome", "failure"));
        Name.ShouldBe("tenants.projection.query.duration");
    }

    [Fact]
    public void RecordEventProcessingDuration_ShouldRecordLowCardinalityDimensionsOnly() {
        _telemetry.RecordEventProcessingDuration(18.0, "tenants", "tenant", "projection-dispatch", "completed");

        (string Name, double Value, KeyValuePair<string, object?>[] Tags) =
            FindRecording(
                "tenants.event.processing.duration",
                tags =>
                    HasTag(tags, "domain", "tenants")
                    && HasTag(tags, "projection_type", "tenant")
                    && HasTag(tags, "stage", "projection-dispatch")
                    && HasTag(tags, "outcome", "completed"));

        Name.ShouldBe("tenants.event.processing.duration");
        Value.ShouldBe(18.0);

        Dictionary<string, object?> tags = Tags.ToDictionary(t => t.Key, t => t.Value);
        tags.Keys.ShouldNotContain("tenant_id");
        tags.Keys.ShouldNotContain("aggregate_id");
        tags.Keys.ShouldNotContain("correlation_id");
        tags.Keys.ShouldNotContain("causation_id");
        tags.Keys.ShouldNotContain("message_id");
        tags.Keys.ShouldNotContain("event_types");
    }

    [Fact]
    public void RecordEventProcessingDuration_WithUnknownDimensions_ShouldSanitizeToUnknown() {
        _telemetry.RecordEventProcessingDuration(
            18.0,
            "tenant-123-unbounded",
            "CustomProjection-456",
            "custom-stage",
            "custom-outcome");

        (string Name, double _, KeyValuePair<string, object?>[] Tags) =
            FindRecording(
                "tenants.event.processing.duration",
                tags =>
                    HasTag(tags, "domain", "unknown")
                    && HasTag(tags, "projection_type", "unknown")
                    && HasTag(tags, "stage", "unknown")
                    && HasTag(tags, "outcome", "failure"));

        Name.ShouldBe("tenants.event.processing.duration");
    }

    private (string Name, double Value, KeyValuePair<string, object?>[] Tags) FindRecording(
        string metricName,
        Func<KeyValuePair<string, object?>[], bool> predicate)
        => _recordings.Last(recording =>
            recording.Name == metricName
            && predicate(recording.Tags));

    private static bool HasTag(KeyValuePair<string, object?>[] tags, string key, object? value)
        => tags.Any(tag => tag.Key == key && Equals(tag.Value, value));
}
