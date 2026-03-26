using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;

using Hexalith.EventStore.Client.Handlers;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.Tenants.DomainProcessing;
using Hexalith.Tenants.Telemetry;
using Hexalith.Tenants.Contracts.Commands;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Telemetry;

[Collection("Telemetry")]
public class DomainServiceRequestHandlerTelemetryTests : IDisposable {
    private readonly ActivityListener _activityListener;
    private readonly MeterListener _meterListener;
    private readonly List<Activity> _activities = [];
    private readonly List<(string Name, double Value, KeyValuePair<string, object?>[] Tags)> _metrics = [];

    public DomainServiceRequestHandlerTelemetryTests() {
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
    public async Task ProcessAsync_Success_ShouldEmitSpanWithCorrectTags() {
        IDomainProcessor processor = Substitute.For<IDomainProcessor>();
        _ = processor.ProcessAsync(Arg.Any<CommandEnvelope>(), Arg.Any<object?>())
            .Returns(DomainResult.NoOp());

        var handler = new DomainServiceRequestHandler(
            [processor], NullLogger<DomainServiceRequestHandler>.Instance);

        _ = await handler.ProcessAsync(CreateRequest("CreateTenant", "tenant-1"));

        _activities.Count.ShouldBeGreaterThanOrEqualTo(1);
        Activity activity = _activities.First(a =>
            a.OperationName == TenantActivitySource.CommandProcess
            && a.GetTagItem(TenantActivitySource.TagTenantId) is not null);
        _ = activity.GetTagItem(TenantActivitySource.TagCommandType).ShouldNotBeNull();
        activity.GetTagItem(TenantActivitySource.TagTenantId).ShouldBe("tenant-1");
        activity.GetTagItem(TenantActivitySource.TagSuccess).ShouldBe(true);
    }

    [Fact]
    public async Task ProcessAsync_Success_ShouldRecordCommandDurationMetric() {
        IDomainProcessor processor = Substitute.For<IDomainProcessor>();
        _ = processor.ProcessAsync(Arg.Any<CommandEnvelope>(), Arg.Any<object?>())
            .Returns(DomainResult.NoOp());

        var handler = new DomainServiceRequestHandler(
            [processor], NullLogger<DomainServiceRequestHandler>.Instance);

        _ = await handler.ProcessAsync(CreateRequest("CreateTenant", "tenant-1"));

        _metrics.ShouldContain(m => m.Name == "tenants.command.duration");
        (string Name, double Value, KeyValuePair<string, object?>[] Tags) =
            _metrics.First(m => m.Name == "tenants.command.duration");
        Value.ShouldBeGreaterThanOrEqualTo(0);

        Dictionary<string, object?> tags = Tags.ToDictionary(t => t.Key, t => t.Value);
        tags["command_type"].ShouldBe("CreateTenant");
        tags["success"].ShouldBe(true);
    }

    [Fact]
    public async Task ProcessAsync_NoProcessorFound_ShouldSetErrorStatusAndRecordMetric() {
        var handler = new DomainServiceRequestHandler(
            [], NullLogger<DomainServiceRequestHandler>.Instance);

        _ = await Should.ThrowAsync<InvalidOperationException>(
            () => handler.ProcessAsync(CreateRequest("CreateTenant", "tenant-1")));

        _activities.Count.ShouldBeGreaterThanOrEqualTo(1);
        Activity activity = _activities.First(a =>
            a.OperationName == TenantActivitySource.CommandProcess
            && a.GetTagItem(TenantActivitySource.TagTenantId) is not null);
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.GetTagItem(TenantActivitySource.TagSuccess).ShouldBe(false);

        _metrics.ShouldContain(m => m.Name == "tenants.command.duration");
        (string Name, double Value, KeyValuePair<string, object?>[] Tags) =
            _metrics.First(m => m.Name == "tenants.command.duration");
        Dictionary<string, object?> tags = Tags.ToDictionary(t => t.Key, t => t.Value);
        tags["success"].ShouldBe(false);
    }

    [Fact]
    public async Task ProcessAsync_WithUnknownCommandType_ShouldSanitizeMetricDimension() {
        IDomainProcessor processor = Substitute.For<IDomainProcessor>();
        _ = processor.ProcessAsync(Arg.Any<CommandEnvelope>(), Arg.Any<object?>())
            .Returns(DomainResult.NoOp());

        var handler = new DomainServiceRequestHandler(
            [processor], NullLogger<DomainServiceRequestHandler>.Instance);

        _ = await handler.ProcessAsync(CreateRequest("UnknownMaliciousCommand", "tenant-1"));

        _metrics.ShouldContain(m => m.Name == "tenants.command.duration");
        (string Name, double Value, KeyValuePair<string, object?>[] Tags) =
            _metrics.First(m => m.Name == "tenants.command.duration");
        Dictionary<string, object?> tags = Tags.ToDictionary(t => t.Key, t => t.Value);
        tags["command_type"].ShouldBe("unknown");
    }

    private static DomainServiceRequest CreateRequest(string commandType, string tenantId) {
        var command = new CreateTenant("acme", "Acme Corp", null);
        var envelope = new CommandEnvelope(
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            tenantId,
            "tenants",
            "acme",
            commandType,
            JsonSerializer.SerializeToUtf8Bytes(command),
            "corr-1",
            null,
            "user-1",
            null);

        return new DomainServiceRequest(envelope, null);
    }
}
