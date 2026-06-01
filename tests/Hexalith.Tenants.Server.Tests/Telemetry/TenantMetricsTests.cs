using System.Diagnostics.Metrics;

using Hexalith.Tenants.Telemetry;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Telemetry;

[Collection("Telemetry")]
public class TenantMetricsTests : IDisposable {
    private readonly MeterListener _listener;
    private readonly List<(string Name, double Value, KeyValuePair<string, object?>[] Tags)> _recordings = [];

    public TenantMetricsTests() {
        _listener = new MeterListener {
            InstrumentPublished = (instrument, listener) => {
                if (instrument.Meter.Name == TenantMetrics.MeterName) {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };
        _listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) => _recordings.Add((instrument.Name, value, tags.ToArray())));
        _listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => _recordings.Add((instrument.Name, value, tags.ToArray())));
        _listener.Start();
    }

    public void Dispose() {
        _listener.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void RecordCommandDuration_WithKnownType_ShouldRecordWithCorrectDimensions() {
        TenantMetrics.RecordCommandDuration(42.5, "CreateTenant", true);
        _listener.RecordObservableInstruments();

        (string Name, double Value, KeyValuePair<string, object?>[] Tags) =
            FindRecording(
                "tenants.command.duration",
                tags =>
                    HasTag(tags, "command_type", "CreateTenant")
                    && HasTag(tags, "success", true)
                    && HasTag(tags, "outcome", "success"));
        Name.ShouldBe("tenants.command.duration");
        Value.ShouldBe(42.5);

        Dictionary<string, object?> tags = Tags.ToDictionary(t => t.Key, t => t.Value);
        tags["command_type"].ShouldBe("CreateTenant");
        tags["success"].ShouldBe(true);
        tags["outcome"].ShouldBe("success");
    }

    [Fact]
    public void RecordCommandDuration_WithKnownFullyQualifiedTenantsType_ShouldRecordShortName() {
        TenantMetrics.RecordCommandDuration(42.5, "Hexalith.Tenants.Contracts.Commands.CreateTenant", true);
        _listener.RecordObservableInstruments();

        (string Name, double Value, KeyValuePair<string, object?>[] Tags) =
            FindRecording(
                "tenants.command.duration",
                tags =>
                    HasTag(tags, "command_type", "CreateTenant")
                    && HasTag(tags, "success", true)
                    && HasTag(tags, "outcome", "success"));
        Name.ShouldBe("tenants.command.duration");
        Value.ShouldBe(42.5);

        Dictionary<string, object?> tags = Tags.ToDictionary(t => t.Key, t => t.Value);
        tags["command_type"].ShouldBe("CreateTenant");
        tags["success"].ShouldBe(true);
        tags["outcome"].ShouldBe("success");
    }

    [Fact]
    public void RecordCommandDuration_WithUnknownType_ShouldSanitizeToUnknown() {
        TenantMetrics.RecordCommandDuration(10.0, "MaliciousCommandType", false);
        _listener.RecordObservableInstruments();

        (string Name, double Value, KeyValuePair<string, object?>[] Tags) =
            FindRecording(
                "tenants.command.duration",
                tags =>
                    HasTag(tags, "command_type", "unknown")
                    && HasTag(tags, "success", false)
                    && HasTag(tags, "outcome", "failure"));
        Name.ShouldBe("tenants.command.duration");

        Dictionary<string, object?> tags = Tags.ToDictionary(t => t.Key, t => t.Value);
        tags["command_type"].ShouldBe("unknown");
        tags["outcome"].ShouldBe("failure");
        tags["success"].ShouldBe(false);
    }

    [Theory]
    [InlineData("CreateTenant")]
    [InlineData("UpdateTenant")]
    [InlineData("DisableTenant")]
    [InlineData("EnableTenant")]
    [InlineData("AddUserToTenant")]
    [InlineData("RemoveUserFromTenant")]
    [InlineData("ChangeUserRole")]
    [InlineData("SetTenantConfiguration")]
    [InlineData("RemoveTenantConfiguration")]
    [InlineData("SetGlobalAdministrator")]
    [InlineData("RemoveGlobalAdministrator")]
    [InlineData("BootstrapGlobalAdmin")]
    public void RecordCommandDuration_AllKnownTypes_ShouldPassThrough(string commandType) {
        TenantMetrics.RecordCommandDuration(1.0, commandType, true);
        _listener.RecordObservableInstruments();

        (string Name, double Value, KeyValuePair<string, object?>[] Tags) =
            FindRecording(
                "tenants.command.duration",
                tags =>
                    HasTag(tags, "command_type", commandType)
                    && HasTag(tags, "success", true)
                    && HasTag(tags, "outcome", "success"));
        Dictionary<string, object?> tags = Tags.ToDictionary(t => t.Key, t => t.Value);
        tags["command_type"].ShouldBe(commandType);
    }

    [Fact]
    public void RecordQueryDuration_ShouldRecordWithQueryTypeAndOutcome() {
        TenantMetrics.RecordQueryDuration(15.3, "get-tenant", "forbidden");
        _listener.RecordObservableInstruments();

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
    public void RecordCommandDuration_FailureCase_ShouldRecordSuccessFalse() {
        TenantMetrics.RecordCommandDuration(100.0, "DisableTenant", false);
        _listener.RecordObservableInstruments();

        (string Name, double Value, KeyValuePair<string, object?>[] Tags) =
            FindRecording(
                "tenants.command.duration",
                tags =>
                    HasTag(tags, "command_type", "DisableTenant")
                    && HasTag(tags, "success", false)
                    && HasTag(tags, "outcome", "failure"));

        Dictionary<string, object?> tags = Tags.ToDictionary(t => t.Key, t => t.Value);
        tags["success"].ShouldBe(false);
        tags["outcome"].ShouldBe("failure");
    }

    [Fact]
    public void RecordEventProcessingDuration_ShouldRecordLowCardinalityDimensionsOnly() {
        TenantMetrics.RecordEventProcessingDuration(18.0, "tenants", "tenant", "projection-dispatch", "completed");
        _listener.RecordObservableInstruments();

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
        TenantMetrics.RecordEventProcessingDuration(
            18.0,
            "tenant-123-unbounded",
            "CustomProjection-456",
            "custom-stage",
            "custom-outcome");
        _listener.RecordObservableInstruments();

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

    [Fact]
    public void RecordProjectionWriteConflict_WithKnownDimensions_ShouldRecordLowCardinalityTagsOnly() {
        TenantMetrics.RecordProjectionWriteConflict("tenant index", "TenantIndexReadModel", "guarded-save-conflict", success: true);
        _listener.RecordObservableInstruments();

        (string Name, double Value, KeyValuePair<string, object?>[] Tags) =
            FindRecording(
                "tenants.projection.write.conflicts",
                tags =>
                    HasTag(tags, "state_key_category", "tenant index")
                    && HasTag(tags, "projection_type", "TenantIndexReadModel")
                    && HasTag(tags, "reason", "guarded-save-conflict")
                    && HasTag(tags, "success", true));

        Name.ShouldBe("tenants.projection.write.conflicts");
        Value.ShouldBe(1);

        Dictionary<string, object?> tags = Tags.ToDictionary(t => t.Key, t => t.Value);
        tags.Keys.ShouldNotContain("tenant_id");
        tags.Keys.ShouldNotContain("aggregate_id");
        tags.Keys.ShouldNotContain("correlation_id");
        tags.Keys.ShouldNotContain("message_ids");
        tags.Keys.ShouldNotContain("event_types");
    }

    [Fact]
    public void RecordProjectionWriteConflict_WithUnknownDimensions_ShouldSanitizeToUnknown() {
        TenantMetrics.RecordProjectionWriteConflict("tenant-123-unbounded", "CustomProjection-456", "custom-reason-789", success: false);
        _listener.RecordObservableInstruments();

        (string Name, double _, KeyValuePair<string, object?>[] Tags) =
            FindRecording(
                "tenants.projection.write.conflicts",
                tags =>
                    HasTag(tags, "state_key_category", "unknown")
                    && HasTag(tags, "projection_type", "unknown")
                    && HasTag(tags, "reason", "unknown")
                    && HasTag(tags, "success", false));

        Name.ShouldBe("tenants.projection.write.conflicts");
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
