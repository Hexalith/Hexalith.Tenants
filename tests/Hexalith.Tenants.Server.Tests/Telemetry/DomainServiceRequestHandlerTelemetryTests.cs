using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;

using Hexalith.EventStore.Client.Handlers;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Events.Rejections;
using Hexalith.Tenants.DomainProcessing;
using Hexalith.Tenants.Telemetry;

using Microsoft.Extensions.Logging;
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
        activity.GetTagItem(TenantActivitySource.TagOutcome).ShouldBe("noop");
        activity.GetTagItem(TenantActivitySource.TagStage).ShouldBe("domain-processing");
        activity.GetTagItem(TenantActivitySource.TagCorrelationId).ShouldBe("corr-1");
        activity.GetTagItem(TenantActivitySource.TagDomain).ShouldBe("tenants");
        activity.GetTagItem(TenantActivitySource.TagAggregateId).ShouldBe("acme");
        activity.GetTagItem(TenantActivitySource.TagCausationId).ShouldBe("cause-1");
    }

    [Fact]
    public async Task ProcessAsync_Success_ShouldRecordCommandDurationMetric() {
        IDomainProcessor processor = Substitute.For<IDomainProcessor>();
        _ = processor.ProcessAsync(Arg.Any<CommandEnvelope>(), Arg.Any<object?>())
            .Returns(DomainResult.NoOp());

        var handler = new DomainServiceRequestHandler(
            [processor], NullLogger<DomainServiceRequestHandler>.Instance);

        _ = await handler.ProcessAsync(CreateRequest("CreateTenant", "tenant-1"));

        (string Name, double Value, KeyValuePair<string, object?>[] Tags) =
            FindCommandDurationMetric(tags =>
                HasTag(tags, "command_type", "CreateTenant")
                && HasTag(tags, "success", true)
                && HasTag(tags, "outcome", "noop"));
        Value.ShouldBeGreaterThanOrEqualTo(0);

        Dictionary<string, object?> tags = Tags.ToDictionary(t => t.Key, t => t.Value);
        tags["command_type"].ShouldBe("CreateTenant");
        tags["success"].ShouldBe(true);
        tags["outcome"].ShouldBe("noop");
        tags.Keys.ShouldNotContain("tenant_id");
        tags.Keys.ShouldNotContain("aggregate_id");
        tags.Keys.ShouldNotContain("correlation_id");
        tags.Keys.ShouldNotContain("causation_id");
        tags.Keys.ShouldNotContain("user_id");
        tags.Keys.ShouldNotContain("message_id");
    }

    [Fact]
    public async Task ProcessAsync_DomainRejection_ShouldRecordRejectionOutcomeWithoutFailureStatus() {
        IDomainProcessor processor = Substitute.For<IDomainProcessor>();
        _ = processor.ProcessAsync(Arg.Any<CommandEnvelope>(), Arg.Any<object?>())
            .Returns(DomainResult.Rejection([new TenantAlreadyExistsRejection("acme")]));

        var handler = new DomainServiceRequestHandler(
            [processor], NullLogger<DomainServiceRequestHandler>.Instance);

        _ = await handler.ProcessAsync(CreateRequest("CreateTenant", "tenant-1"));

        Activity activity = _activities.First(a =>
            a.OperationName == TenantActivitySource.CommandProcess
            && a.GetTagItem(TenantActivitySource.TagTenantId) is not null);
        activity.Status.ShouldBe(ActivityStatusCode.Unset);
        activity.GetTagItem(TenantActivitySource.TagSuccess).ShouldBe(true);
        activity.GetTagItem(TenantActivitySource.TagOutcome).ShouldBe("rejection");

        (string Name, double Value, KeyValuePair<string, object?>[] Tags) =
            FindCommandDurationMetric(tags =>
                HasTag(tags, "command_type", "CreateTenant")
                && HasTag(tags, "success", true)
                && HasTag(tags, "outcome", "rejection"));
        Value.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task ProcessAsync_DomainSuccess_ShouldRecordSuccessOutcome() {
        IDomainProcessor processor = Substitute.For<IDomainProcessor>();
        _ = processor.ProcessAsync(Arg.Any<CommandEnvelope>(), Arg.Any<object?>())
            .Returns(DomainResult.Success([new TenantCreated("acme", "Acme", null, DateTimeOffset.UtcNow)]));

        var handler = new DomainServiceRequestHandler(
            [processor], NullLogger<DomainServiceRequestHandler>.Instance);

        _ = await handler.ProcessAsync(CreateRequest("CreateTenant", "tenant-1"));

        Activity activity = _activities.First(a =>
            a.OperationName == TenantActivitySource.CommandProcess
            && a.GetTagItem(TenantActivitySource.TagTenantId) is not null);
        activity.GetTagItem(TenantActivitySource.TagOutcome).ShouldBe("success");

        _ = FindCommandDurationMetric(tags =>
            HasTag(tags, "command_type", "CreateTenant")
            && HasTag(tags, "success", true)
            && HasTag(tags, "outcome", "success"));
    }

    [Fact]
    public async Task ProcessAsync_NoProcessorFound_ShouldSetErrorStatusAndRecordFailureOutcomeMetric() {
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
        activity.GetTagItem(TenantActivitySource.TagOutcome).ShouldBe("failure");

        (string Name, double Value, KeyValuePair<string, object?>[] Tags) =
            FindCommandDurationMetric(tags =>
                HasTag(tags, "success", false)
                && HasTag(tags, "outcome", "failure"));
        Dictionary<string, object?> tags = Tags.ToDictionary(t => t.Key, t => t.Value);
        tags["success"].ShouldBe(false);
        tags["outcome"].ShouldBe("failure");
    }

    [Fact]
    public async Task ProcessAsync_WithUnknownCommandType_ShouldSanitizeMetricDimension() {
        IDomainProcessor processor = Substitute.For<IDomainProcessor>();
        _ = processor.ProcessAsync(Arg.Any<CommandEnvelope>(), Arg.Any<object?>())
            .Returns(DomainResult.NoOp());

        var handler = new DomainServiceRequestHandler(
            [processor], NullLogger<DomainServiceRequestHandler>.Instance);

        _ = await handler.ProcessAsync(CreateRequest("UnknownMaliciousCommand", "tenant-1"));

        (string Name, double Value, KeyValuePair<string, object?>[] Tags) =
            FindCommandDurationMetric(tags => HasTag(tags, "command_type", "unknown"));
        Dictionary<string, object?> tags = Tags.ToDictionary(t => t.Key, t => t.Value);
        tags["command_type"].ShouldBe("unknown");
    }

    [Fact]
    public async Task ProcessAsync_WithControlledDelayedProcessor_ShouldRecordObservableElapsedDuration() {
        IDomainProcessor processor = Substitute.For<IDomainProcessor>();
        _ = processor.ProcessAsync(Arg.Any<CommandEnvelope>(), Arg.Any<object?>())
            .Returns(async _ => {
                await Task.Delay(TimeSpan.FromMilliseconds(25));
                return DomainResult.NoOp();
            });

        var handler = new DomainServiceRequestHandler(
            [processor], NullLogger<DomainServiceRequestHandler>.Instance);

        _ = await handler.ProcessAsync(CreateRequest("CreateTenant", "tenant-1"));

        (string Name, double Value, KeyValuePair<string, object?>[] Tags) =
            FindCommandDurationMetric(tags =>
                HasTag(tags, "command_type", "CreateTenant")
                && HasTag(tags, "outcome", "noop"));
        Value.ShouldBeGreaterThanOrEqualTo(10);
    }

    [Fact]
    public async Task ProcessAsync_ShouldLogDomainRejectionSeparatelyFromInfrastructureFailure() {
        IDomainProcessor rejectingProcessor = Substitute.For<IDomainProcessor>();
        _ = rejectingProcessor.ProcessAsync(Arg.Any<CommandEnvelope>(), Arg.Any<object?>())
            .Returns(DomainResult.Rejection([new TenantAlreadyExistsRejection("acme")]));
        var rejectionLogger = new TestLogger<DomainServiceRequestHandler>();
        var rejectionHandler = new DomainServiceRequestHandler([rejectingProcessor], rejectionLogger);

        _ = await rejectionHandler.ProcessAsync(CreateRequest("CreateTenant", "tenant-1"));

        TestLogEntry rejectionEntry = rejectionLogger.Entries.Single(entry => entry.EventId.Id == 100201);
        rejectionEntry.Level.ShouldBe(LogLevel.Information);
        rejectionEntry.Properties["Outcome"].ShouldBe("rejection");
        rejectionEntry.Properties["Stage"].ShouldBe("domain-processing");
        rejectionEntry.Properties.Keys.ShouldNotContain("Payload");
        rejectionEntry.Properties.Keys.ShouldNotContain("Token");
        rejectionEntry.Properties.Keys.ShouldNotContain("Secret");

        var failureLogger = new TestLogger<DomainServiceRequestHandler>();
        var failureHandler = new DomainServiceRequestHandler([], failureLogger);

        _ = await Should.ThrowAsync<InvalidOperationException>(
            () => failureHandler.ProcessAsync(CreateRequest("CreateTenant", "tenant-1")));

        TestLogEntry failureEntry = failureLogger.Entries.Single(entry => entry.EventId.Id == 100202);
        failureEntry.Level.ShouldBe(LogLevel.Error);
        failureEntry.Properties["Outcome"].ShouldBe("failure");
        failureEntry.Properties["ExceptionType"].ShouldBe(nameof(InvalidOperationException));
        failureEntry.Properties["Stage"].ShouldBe("domain-processing");
    }

    private (string Name, double Value, KeyValuePair<string, object?>[] Tags) FindCommandDurationMetric(
        Func<KeyValuePair<string, object?>[], bool> predicate)
        => _metrics.Last(metric =>
            metric.Name == "tenants.command.duration"
            && predicate(metric.Tags));

    private static bool HasTag(KeyValuePair<string, object?>[] tags, string key, object? value)
        => tags.Any(tag => tag.Key == key && Equals(tag.Value, value));

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
            "cause-1",
            "user-1",
            null);

        return new DomainServiceRequest(envelope, null);
    }

    private sealed record TestLogEntry(LogLevel Level, EventId EventId, IReadOnlyDictionary<string, object?> Properties);

    private sealed class TestLogger<T> : ILogger<T> {
        private readonly List<TestLogEntry> _entries = [];

        public IReadOnlyList<TestLogEntry> Entries => _entries;

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
            => NullLogger<T>.Instance.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) {
            var properties = new Dictionary<string, object?>(StringComparer.Ordinal);
            if (state is IEnumerable<KeyValuePair<string, object?>> pairs) {
                foreach (KeyValuePair<string, object?> pair in pairs) {
                    properties[pair.Key] = pair.Value;
                }
            }

            _entries.Add(new TestLogEntry(logLevel, eventId, properties));
        }
    }
}
