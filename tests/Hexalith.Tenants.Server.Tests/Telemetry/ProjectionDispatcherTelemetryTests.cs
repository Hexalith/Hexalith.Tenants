using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text;
using System.Text.Json;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.DomainService;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Projections;
using Hexalith.Tenants.Server.Projections;
using Hexalith.Tenants.Telemetry;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Telemetry;

[Collection("Telemetry")]
public class ProjectionDispatcherTelemetryTests : IDisposable {
    private static readonly JsonSerializerOptions s_options = new() {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly string s_conventionName = EventStoreDomainTelemetry.ActivitySourceName("tenants");

    private readonly EventStoreDomainDiagnostics _diagnostics = new("tenants");
    private readonly TenantTelemetry _telemetry;
    private readonly ActivityListener _activityListener;
    private readonly MeterListener _meterListener;
    private readonly List<Activity> _activities = [];
    private readonly List<(string Name, double Value, KeyValuePair<string, object?>[] Tags)> _metrics = [];

    public ProjectionDispatcherTelemetryTests() {
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
        _meterListener.SetMeasurementEventCallback<double>((instrument, value, tags, _) =>
            _metrics.Add((instrument.Name, value, tags.ToArray())));
        _meterListener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
            _metrics.Add((instrument.Name, value, tags.ToArray())));
        _meterListener.Start();
    }

    public void Dispose() {
        _activityListener.Dispose();
        _meterListener.Dispose();
        _diagnostics.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task DispatchAsync_TenantsDomain_ShouldEmitProjectionSpanAndMetricAsync() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        SetupSuccessfulTenantProjection(store);

        ProjectionRequest request = CreateTenantRequest();

        IResult result = await new ProjectionDispatcher(store, _telemetry).DispatchAsync(request);

        _ = result.ShouldBeOfType<Ok<ProjectionResponse>>();
        Activity activity = FindProjectionActivity("tenants");
        activity.GetTagItem(TenantTelemetry.TagOutcome).ShouldBe("completed");
        activity.GetTagItem(TenantTelemetry.TagStage).ShouldBe("projection-dispatch");
        activity.GetTagItem(TenantTelemetry.TagTenantId).ShouldBe("system");
        activity.GetTagItem(TenantTelemetry.TagDomain).ShouldBe("tenants");
        activity.GetTagItem(TenantTelemetry.TagAggregateId).ShouldBe("tenant-1");
        activity.GetTagItem(TenantTelemetry.TagProjectionType).ShouldBe("tenant");
        activity.GetTagItem(TenantTelemetry.TagEventCount).ShouldBe(1);
        activity.GetTagItem(TenantTelemetry.TagCorrelationId).ShouldBe("corr-1");
        activity.GetTagItem(TenantTelemetry.TagCausationIdStatus).ShouldBe("unavailable-from-projection-dto");
        activity.GetTagItem(TenantTelemetry.TagEventTypes).ShouldBe("TenantCreated");

        (string Name, double Value, KeyValuePair<string, object?>[] Tags) = FindMetric(
            tags =>
                HasTag(tags, "domain", "tenants")
                && HasTag(tags, "projection_type", "tenant")
                && HasTag(tags, "stage", "projection-dispatch")
                && HasTag(tags, "outcome", "completed"));
        Name.ShouldBe("tenants.event.processing.duration");
        Value.ShouldBeGreaterThanOrEqualTo(0);

        Dictionary<string, object?> metricTags = Tags.ToDictionary(t => t.Key, t => t.Value);
        metricTags.Keys.ShouldNotContain("tenant_id");
        metricTags.Keys.ShouldNotContain("aggregate_id");
        metricTags.Keys.ShouldNotContain("correlation_id");
        metricTags.Keys.ShouldNotContain("causation_id");
        metricTags.Keys.ShouldNotContain("message_id");
        metricTags.Keys.ShouldNotContain("event_types");
    }

    [Fact]
    public async Task DispatchAsync_GlobalAdministratorsDomain_ShouldEmitCompletedOutcomeAsync() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        ProjectionRequest request = CreateGlobalAdminRequest();

        IResult result = await new ProjectionDispatcher(store, _telemetry).DispatchAsync(request);

        _ = result.ShouldBeOfType<Ok<ProjectionResponse>>();
        Activity activity = FindProjectionActivity("global-administrators");
        activity.GetTagItem(TenantTelemetry.TagOutcome).ShouldBe("completed");
        activity.GetTagItem(TenantTelemetry.TagProjectionType).ShouldBe("global-administrators");

        _ = FindMetric(tags =>
            HasTag(tags, "domain", "global-administrators")
            && HasTag(tags, "projection_type", "global-administrators")
            && HasTag(tags, "outcome", "completed"));
    }

    [Fact]
    public async Task DispatchAsync_UnsupportedDomain_ShouldEmitUnsupportedDomainOutcomeAsync() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        ProjectionRequest request = new("system", "orders", "tenant-1", []);

        IResult result = await new ProjectionDispatcher(store, _telemetry).DispatchAsync(request);

        _ = result.ShouldBeOfType<ProblemHttpResult>();
        Activity activity = FindProjectionActivity("unknown");
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.GetTagItem(TenantTelemetry.TagOutcome).ShouldBe("unsupported-domain");

        _ = FindMetric(tags =>
            HasTag(tags, "domain", "unknown")
            && HasTag(tags, "projection_type", "unknown")
            && HasTag(tags, "outcome", "unsupported-domain"));
    }

    [Fact]
    public async Task DispatchAsync_InvalidGlobalAdministratorIdentity_ShouldEmitInvalidIdentityOutcomeAsync() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        ProjectionRequest request = new(
            "tenant-a",
            "global-administrators",
            "global-administrators",
            [CreateEventDto(new GlobalAdministratorSet("system", "admin-user"))]);

        IResult result = await new ProjectionDispatcher(store, _telemetry).DispatchAsync(request);

        _ = result.ShouldBeOfType<ProblemHttpResult>();
        Activity activity = FindProjectionActivity("global-administrators");
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.GetTagItem(TenantTelemetry.TagOutcome).ShouldBe("invalid-identity");

        _ = FindMetric(tags =>
            HasTag(tags, "domain", "global-administrators")
            && HasTag(tags, "projection_type", "global-administrators")
            && HasTag(tags, "outcome", "invalid-identity"));
    }

    [Fact]
    public async Task DispatchAsync_UnknownEventType_ShouldCollapseSpanEventTypeSummaryAsync() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        SetupSuccessfulTenantProjection(store);
        ProjectionRequest request = new(
            "system",
            "tenants",
            "tenant-1",
            [
                new ProjectionEventDto(
                    "Bearer abc.secret@example.com",
                    Encoding.UTF8.GetBytes("{}"),
                    "json",
                    1L,
                    DateTimeOffset.UtcNow,
                    "corr-1",
                    MessageId: "evt-test",
                    UserId: "actor-test"),
            ]);

        _ = await new ProjectionDispatcher(store, _telemetry).DispatchAsync(request);

        Activity activity = FindProjectionActivity("tenants");
        activity.GetTagItem(TenantTelemetry.TagEventTypes).ShouldBe("unknown");
    }

    [Fact]
    public async Task DispatchAsync_TenantProjectionRetryExhausted_ShouldEmitRetryExhaustedOutcomeAsync() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        store.GetAsync<TenantReadModel>("statestore", "projection:tenants:tenant-1", Arg.Any<CancellationToken>())
            .Returns(new ReadModelEntry<TenantReadModel>(null, "tenant-etag"));
        store.TrySaveAsync("statestore", "projection:tenants:tenant-1", Arg.Any<TenantReadModel>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        ProjectionRequest request = CreateTenantRequest();

        _ = await Should.ThrowAsync<InvalidOperationException>(() =>
            new ProjectionDispatcher(store, _telemetry).DispatchAsync(request));

        Activity activity = FindProjectionActivity("tenants");
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.GetTagItem(TenantTelemetry.TagOutcome).ShouldBe("retry-exhausted");

        _ = FindMetric(tags =>
            HasTag(tags, "domain", "tenants")
            && HasTag(tags, "projection_type", "tenant")
            && HasTag(tags, "outcome", "retry-exhausted"));
    }

    [Fact]
    public async Task DispatchAsync_TenantProjectionRecoveredConflict_ShouldKeepEventProcessingCompletedAsync() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        store.GetAsync<TenantReadModel>("statestore", "projection:tenants:tenant-1", Arg.Any<CancellationToken>())
            .Returns(
                new ReadModelEntry<TenantReadModel>(null, "tenant-etag-1"),
                new ReadModelEntry<TenantReadModel>(new TenantReadModel(), "tenant-etag-2"));
        store.TrySaveAsync("statestore", "projection:tenants:tenant-1", Arg.Any<TenantReadModel>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false, true);
        SetupSuccessfulGuardedSave<TenantAuditReadModel>(store, "audit:tenant-1");
        SetupSuccessfulGuardedSave<TenantIndexReadModel>(store, "projection:tenant-index:singleton");

        IResult result = await new ProjectionDispatcher(store, _telemetry).DispatchAsync(CreateTenantRequest());

        _ = result.ShouldBeOfType<Ok<ProjectionResponse>>();
        Activity activity = FindProjectionActivity("tenants");
        activity.GetTagItem(TenantTelemetry.TagOutcome).ShouldBe("completed");

        // The dispatcher still records the overall event-processing outcome. The per-write conflict counter
        // (formerly tenants.projection.write.conflicts) was emitted by the removed TenantProjectionWritePolicy;
        // the platform ReadModelWritePolicy logs conflicts instead of emitting a domain metric (A8).
        _ = FindMetric(tags =>
            HasTag(tags, "domain", "tenants")
            && HasTag(tags, "projection_type", "tenant")
            && HasTag(tags, "outcome", "completed"));
    }

    [Fact]
    public async Task DispatchAsync_TenantProjectionInfrastructureFailure_ShouldEmitFailureOutcomeAsync() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        store.GetAsync<TenantReadModel>("statestore", "projection:tenants:tenant-1", Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("State store unavailable"));

        ProjectionRequest request = CreateTenantRequest();

        _ = await Should.ThrowAsync<HttpRequestException>(() =>
            new ProjectionDispatcher(store, _telemetry).DispatchAsync(request));

        Activity activity = FindProjectionActivity("tenants");
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.GetTagItem(TenantTelemetry.TagOutcome).ShouldBe("failure");

        _ = FindMetric(tags =>
            HasTag(tags, "domain", "tenants")
            && HasTag(tags, "projection_type", "tenant")
            && HasTag(tags, "outcome", "failure"));
    }

    [Fact]
    public async Task DispatchAsync_ShouldLogCompletedAndUnsupportedOutcomesWithoutPayloadDataAsync() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        SetupSuccessfulTenantProjection(store);
        var loggerFactory = new TestLoggerFactory();
        var dispatcher = new ProjectionDispatcher(store, _telemetry, loggerFactory);

        _ = await dispatcher.DispatchAsync(CreateTenantRequest());
        _ = await dispatcher.DispatchAsync(new ProjectionRequest("system", "orders", "tenant-1", []));

        IReadOnlyList<TestLogEntry> completedEntries = loggerFactory.Entries
            .Where(entry => entry.EventId.Id == 100301)
            .ToList();
        completedEntries.Count.ShouldBe(2);
        completedEntries.ShouldContain(entry => Equals(entry.Properties["Outcome"], "completed"));
        completedEntries.ShouldContain(entry => Equals(entry.Properties["Outcome"], "unsupported-domain"));

        foreach (TestLogEntry entry in completedEntries) {
            entry.Level.ShouldBe(LogLevel.Information);
            entry.Properties["Stage"].ShouldBe("projection-dispatch");
            entry.Properties.Keys.ShouldNotContain("Payload");
            entry.Properties.Keys.ShouldNotContain("EventTypes");
            entry.Properties.Keys.ShouldNotContain("MessageId");
            entry.Properties.Keys.ShouldNotContain("UserId");
            entry.Properties.Keys.ShouldNotContain("Token");
            entry.Properties.Keys.ShouldNotContain("Secret");
        }
    }

    [Fact]
    public async Task DispatchAsync_InfrastructureFailure_ShouldLogFailureClassificationWithoutPayloadDataAsync() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        store.GetAsync<TenantReadModel>("statestore", "projection:tenants:tenant-1", Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("State store unavailable"));
        var loggerFactory = new TestLoggerFactory();

        _ = await Should.ThrowAsync<HttpRequestException>(() =>
            new ProjectionDispatcher(store, _telemetry, loggerFactory).DispatchAsync(CreateTenantRequest()));

        TestLogEntry failureEntry = loggerFactory.Entries.Single(entry => entry.EventId.Id == 100302);
        failureEntry.Level.ShouldBe(LogLevel.Error);
        failureEntry.Properties["Outcome"].ShouldBe("failure");
        failureEntry.Properties["ExceptionType"].ShouldBe(nameof(HttpRequestException));
        failureEntry.Properties["Stage"].ShouldBe("projection-dispatch");
        failureEntry.Properties.Keys.ShouldNotContain("Payload");
        failureEntry.Properties.Keys.ShouldNotContain("EventTypes");
        failureEntry.Properties.Keys.ShouldNotContain("MessageId");
        failureEntry.Properties.Keys.ShouldNotContain("UserId");
        failureEntry.Properties.Keys.ShouldNotContain("Token");
        failureEntry.Properties.Keys.ShouldNotContain("Secret");
    }

    private (string Name, double Value, KeyValuePair<string, object?>[] Tags) FindMetric(
        Func<KeyValuePair<string, object?>[], bool> predicate)
        => FindNamedMetric("tenants.event.processing.duration", predicate);

    private (string Name, double Value, KeyValuePair<string, object?>[] Tags) FindNamedMetric(
        string metricName,
        Func<KeyValuePair<string, object?>[], bool> predicate)
        => _metrics.Last(metric =>
            metric.Name == metricName
            && predicate(metric.Tags));

    private Activity FindProjectionActivity(string domain)
        => _activities.Last(activity =>
            activity.OperationName == TenantTelemetry.ProjectionProject
            && Equals(activity.GetTagItem(TenantTelemetry.TagDomain), domain));

    private static bool HasTag(KeyValuePair<string, object?>[] tags, string key, object? value)
        => tags.Any(tag => tag.Key == key && Equals(tag.Value, value));

    private static void SetupSuccessfulTenantProjection(IReadModelStore store) {
        SetupSuccessfulGuardedSave<TenantReadModel>(store, "projection:tenants:tenant-1");
        SetupSuccessfulGuardedSave<TenantAuditReadModel>(store, "audit:tenant-1");
        SetupSuccessfulGuardedSave<TenantIndexReadModel>(store, "projection:tenant-index:singleton");
    }

    private static void SetupSuccessfulGuardedSave<TValue>(IReadModelStore store, string key)
        where TValue : class {
        store.GetAsync<TValue>("statestore", key, Arg.Any<CancellationToken>())
            .Returns(new ReadModelEntry<TValue>(null, null));
        store.TrySaveAsync("statestore", key, Arg.Any<TValue>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private static ProjectionRequest CreateTenantRequest()
        => new(
            "system",
            "tenants",
            "tenant-1",
            [CreateEventDto(new TenantCreated("tenant-1", "Acme", null, DateTimeOffset.UtcNow))]);

    private static ProjectionRequest CreateGlobalAdminRequest()
        => new(
            "system",
            "global-administrators",
            "global-administrators",
            [CreateEventDto(new GlobalAdministratorSet("system", "admin-user"))]);

    private static ProjectionEventDto CreateEventDto(object @event) {
        string typeName = @event.GetType().FullName ?? @event.GetType().Name;
        byte[] payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(@event, s_options));
        return new ProjectionEventDto(
            typeName,
            payload,
            "json",
            1L,
            DateTimeOffset.UtcNow,
            "corr-1",
            MessageId: "evt-test",
            UserId: "actor-test");
    }

    private sealed record TestLogEntry(LogLevel Level, EventId EventId, IReadOnlyDictionary<string, object?> Properties);

    private sealed class TestLoggerFactory : ILoggerFactory {
        private readonly TestLogger _logger = new();

        public IReadOnlyList<TestLogEntry> Entries => _logger.Entries;

        public void AddProvider(ILoggerProvider provider) {
        }

        public ILogger CreateLogger(string categoryName) => _logger;

        public void Dispose() {
        }
    }

    private sealed class TestLogger : ILogger {
        private readonly List<TestLogEntry> _entries = [];

        public IReadOnlyList<TestLogEntry> Entries => _entries;

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
            => NullLogger.Instance.BeginScope(state);

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
