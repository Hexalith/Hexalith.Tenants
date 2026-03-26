using System.Diagnostics.Metrics;

using Hexalith.Tenants.CommandApi.Telemetry;

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
                tags => HasTag(tags, "command_type", "CreateTenant") && HasTag(tags, "success", true));
        Name.ShouldBe("tenants.command.duration");
        Value.ShouldBe(42.5);

        Dictionary<string, object?> tags = Tags.ToDictionary(t => t.Key, t => t.Value);
        tags["command_type"].ShouldBe("CreateTenant");
        tags["success"].ShouldBe(true);
    }

    [Fact]
    public void RecordCommandDuration_WithUnknownType_ShouldSanitizeToUnknown() {
        TenantMetrics.RecordCommandDuration(10.0, "MaliciousCommandType", false);
        _listener.RecordObservableInstruments();

        (string Name, double Value, KeyValuePair<string, object?>[] Tags) =
            FindRecording(
                "tenants.command.duration",
                tags => HasTag(tags, "command_type", "unknown") && HasTag(tags, "success", false));
        Name.ShouldBe("tenants.command.duration");

        Dictionary<string, object?> tags = Tags.ToDictionary(t => t.Key, t => t.Value);
        tags["command_type"].ShouldBe("unknown");
        tags["success"].ShouldBe(false);
    }

    [Theory]
    [InlineData("CreateTenant")]
    [InlineData("UpdateTenantInformation")]
    [InlineData("DisableTenant")]
    [InlineData("EnableTenant")]
    [InlineData("AddUserToTenant")]
    [InlineData("RemoveUserFromTenant")]
    [InlineData("ChangeUserRole")]
    [InlineData("SetTenantConfiguration")]
    [InlineData("RemoveTenantConfiguration")]
    [InlineData("AddGlobalAdministrator")]
    [InlineData("RemoveGlobalAdministrator")]
    [InlineData("RegisterGlobalAdministrator")]
    public void RecordCommandDuration_AllKnownTypes_ShouldPassThrough(string commandType) {
        TenantMetrics.RecordCommandDuration(1.0, commandType, true);
        _listener.RecordObservableInstruments();

        (string Name, double Value, KeyValuePair<string, object?>[] Tags) =
            FindRecording(
                "tenants.command.duration",
                tags => HasTag(tags, "command_type", commandType) && HasTag(tags, "success", true));
        Dictionary<string, object?> tags = Tags.ToDictionary(t => t.Key, t => t.Value);
        tags["command_type"].ShouldBe(commandType);
    }

    [Fact]
    public void RecordQueryDuration_ShouldRecordWithQueryType() {
        TenantMetrics.RecordQueryDuration(15.3, "get-tenant");
        _listener.RecordObservableInstruments();

        (string Name, double Value, KeyValuePair<string, object?>[] Tags) =
            FindRecording(
                "tenants.projection.query.duration",
                tags => HasTag(tags, "query_type", "get-tenant"));
        Name.ShouldBe("tenants.projection.query.duration");
        Value.ShouldBe(15.3);

        Dictionary<string, object?> tags = Tags.ToDictionary(t => t.Key, t => t.Value);
        tags["query_type"].ShouldBe("get-tenant");
    }

    [Fact]
    public void RecordCommandDuration_FailureCase_ShouldRecordSuccessFalse() {
        TenantMetrics.RecordCommandDuration(100.0, "DisableTenant", false);
        _listener.RecordObservableInstruments();

        (string Name, double Value, KeyValuePair<string, object?>[] Tags) =
            FindRecording(
                "tenants.command.duration",
                tags => HasTag(tags, "command_type", "DisableTenant") && HasTag(tags, "success", false));

        Dictionary<string, object?> tags = Tags.ToDictionary(t => t.Key, t => t.Value);
        tags["success"].ShouldBe(false);
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
