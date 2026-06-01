using System.Diagnostics.Metrics;

using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Projections;
using Hexalith.Tenants.Server.Projections;
using Hexalith.Tenants.Server.Tests.Projections;
using Hexalith.Tenants.Telemetry;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Telemetry;

[Collection("Telemetry")]
public class TenantProjectionWritePolicyMetricsTests : IDisposable {
    private readonly MeterListener _listener;
    private readonly List<(string Name, long Value, KeyValuePair<string, object?>[] Tags)> _recordings = [];

    public TenantProjectionWritePolicyMetricsTests() {
        _listener = new MeterListener {
            InstrumentPublished = (instrument, listener) => {
                if (instrument.Meter.Name == TenantMetrics.MeterName) {
                    listener.EnableMeasurementEvents(instrument);
                }
            },
        };
        _listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
            _recordings.Add((instrument.Name, value, tags.ToArray())));
        _listener.Start();
    }

    public void Dispose() {
        _listener.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ProjectAsync_RecoveredTenantDetailConflict_RecordsRecoverableConflictMetricOnlyAsync() {
        var fixture = new ProjectionWriteConformanceFixture();
        DateTimeOffset timestamp = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);

        fixture.StateStore.EnqueueRead<TenantReadModel>(
            ProjectionWriteConformanceFixture.TenantProjectionKey,
            null,
            "tenant-etag-1");
        fixture.StateStore.EnqueueRead(
            ProjectionWriteConformanceFixture.TenantProjectionKey,
            ProjectionWriteConformanceFixture.SeedTenantReadModel(),
            "tenant-etag-2");
        fixture.StateStore.EnqueueTrySave(ProjectionWriteConformanceFixture.TenantProjectionKey, false);
        fixture.StateStore.EnqueueTrySave(ProjectionWriteConformanceFixture.TenantProjectionKey, true);
        fixture.EnqueueSuccessfulAuditSave();
        fixture.EnqueueSuccessfulIndexSave();

        _ = await fixture.RunProjectionHandlerAsync(ProjectionWriteConformanceFixture.CreateRequest(
            ProjectionWriteConformanceFixture.CreateEvent(
                new TenantCreated(ProjectionWriteConformanceFixture.TenantId, "Acme", null, timestamp),
                "evt-created",
                timestamp)));

        FindProjectionWriteConflictMetric(tags =>
            HasTag(tags, "state_key_category", "tenant read-model")
            && HasTag(tags, "projection_type", nameof(TenantReadModel))
            && HasTag(tags, "reason", "guarded-save-conflict")
            && HasTag(tags, "success", true)).Value.ShouldBe(1);

        _recordings.ShouldNotContain(recording =>
            recording.Name == "tenants.projection.write.conflicts"
            && HasTag(recording.Tags, "reason", "retry-exhausted"));
    }

    [Fact]
    public async Task ProjectAsync_RecoveredTenantAuditConflict_RecordsAuditConflictMetricOnlyAsync() {
        var fixture = new ProjectionWriteConformanceFixture();
        DateTimeOffset timestamp = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);

        fixture.EnqueueSuccessfulTenantDetailSave();
        fixture.StateStore.EnqueueRead<TenantAuditReadModel>(
            ProjectionWriteConformanceFixture.TenantAuditProjectionKey,
            null,
            "audit-etag-1");
        fixture.StateStore.EnqueueRead(
            ProjectionWriteConformanceFixture.TenantAuditProjectionKey,
            ProjectionWriteConformanceFixture.SeedAuditWith(),
            "audit-etag-2");
        fixture.StateStore.EnqueueTrySave(ProjectionWriteConformanceFixture.TenantAuditProjectionKey, false);
        fixture.StateStore.EnqueueTrySave(ProjectionWriteConformanceFixture.TenantAuditProjectionKey, true);
        fixture.EnqueueSuccessfulIndexSave();

        _ = await fixture.RunProjectionHandlerAsync(ProjectionWriteConformanceFixture.CreateRequest(
            ProjectionWriteConformanceFixture.CreateEvent(
                new UserAddedToTenant(
                    ProjectionWriteConformanceFixture.TenantId,
                    "user-1",
                    TenantRole.TenantReader),
                "evt-added",
                timestamp)));

        FindProjectionWriteConflictMetric(tags =>
            HasTag(tags, "state_key_category", "tenant audit")
            && HasTag(tags, "projection_type", nameof(TenantAuditReadModel))
            && HasTag(tags, "reason", "guarded-save-conflict")
            && HasTag(tags, "success", true)).Value.ShouldBe(1);

        _recordings.ShouldNotContain(recording =>
            recording.Name == "tenants.projection.write.conflicts"
            && HasTag(recording.Tags, "state_key_category", "tenant audit")
            && HasTag(recording.Tags, "reason", "retry-exhausted"));
    }

    [Fact]
    public async Task ProjectAsync_TenantIndexRetryExhaustion_RecordsTerminalConflictMetricAsync() {
        var fixture = new ProjectionWriteConformanceFixture();
        DateTimeOffset timestamp = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);

        fixture.EnqueueSuccessfulTenantDetailSave();
        fixture.EnqueueSuccessfulAuditSave();
        for (int attempt = 0; attempt < TenantProjectionWritePolicy.MaxAttempts; attempt++) {
            fixture.StateStore.EnqueueRead<TenantIndexReadModel>(
                ProjectionWriteConformanceFixture.TenantIndexProjectionKey,
                null,
                $"index-etag-{attempt + 1}");
            fixture.StateStore.EnqueueTrySave(ProjectionWriteConformanceFixture.TenantIndexProjectionKey, false);
        }

        _ = await Should.ThrowAsync<InvalidOperationException>(() =>
            fixture.RunProjectionHandlerAsync(ProjectionWriteConformanceFixture.CreateRequest(
                ProjectionWriteConformanceFixture.CreateEvent(
                    new TenantCreated(ProjectionWriteConformanceFixture.TenantId, "Acme", null, timestamp),
                    "evt-created",
                    timestamp))));

        FindProjectionWriteConflictMetric(tags =>
            HasTag(tags, "state_key_category", "tenant index")
            && HasTag(tags, "projection_type", nameof(TenantIndexReadModel))
            && HasTag(tags, "reason", "retry-exhausted")
            && HasTag(tags, "success", false)).Value.ShouldBe(1);

        _recordings.Count(recording =>
            recording.Name == "tenants.projection.write.conflicts"
            && HasTag(recording.Tags, "state_key_category", "tenant index")
            && HasTag(recording.Tags, "reason", "guarded-save-conflict")
            && HasTag(recording.Tags, "success", true))
            .ShouldBe(TenantProjectionWritePolicy.MaxAttempts - 1);
    }

    private (string Name, long Value, KeyValuePair<string, object?>[] Tags) FindProjectionWriteConflictMetric(
        Func<KeyValuePair<string, object?>[], bool> predicate)
        => _recordings.Last(recording =>
            recording.Name == "tenants.projection.write.conflicts"
            && predicate(recording.Tags));

    private static bool HasTag(KeyValuePair<string, object?>[] tags, string key, object? value)
        => tags.Any(tag => tag.Key == key && Equals(tag.Value, value));
}
