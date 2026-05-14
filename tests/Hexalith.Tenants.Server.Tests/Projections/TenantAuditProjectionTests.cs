using System.Text.Json;

using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Server.Projections;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Projections;

public class TenantAuditProjectionTests {
    [Fact]
    public void TenantAuditProjection_is_not_eventstore_discoverable_projection() {
        typeof(TenantAuditProjection).BaseType.ShouldBe(typeof(object));
    }

    [Fact]
    public void Project_WithFullEventList_ReturnsAuditEntriesForAllSupportedEvents() {
        DateTimeOffset timestamp = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);
        ProjectionEventDto[] events = [
            CreateEvent(new TenantCreated("tenant-1", "Acme", null, timestamp), "evt-1", timestamp),
            CreateEvent(new TenantUpdated("tenant-1", "Acme Updated", null), "evt-2", timestamp.AddMinutes(1)),
            CreateEvent(new UserAddedToTenant("tenant-1", "user-1", Contracts.Enums.TenantRole.TenantReader), "evt-3", timestamp.AddMinutes(2)),
        ];

        TenantAuditReadModel result = TenantAuditProjection.ProjectAuditEvents(events);

        result.Entries.Count.ShouldBe(3);
        result.Entries.Select(e => e.EventId).ShouldBe(["evt-1", "evt-2", "evt-3"]);
    }

    [Fact]
    public void Project_WithNullEvents_SkipsNullsAndAppliesValid() {
        DateTimeOffset timestamp = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);
        ProjectionEventDto?[] events = [
            CreateEvent(new TenantCreated("tenant-1", "Acme", null, timestamp), "evt-1", timestamp),
            null,
        ];

        TenantAuditReadModel result = TenantAuditProjection.ProjectAuditEvents(events!);

        result.Entries.Single().EventId.ShouldBe("evt-1");
    }

    private static ProjectionEventDto CreateEvent(IEventPayload payload, string messageId, DateTimeOffset timestamp) =>
        new(
            payload.GetType().FullName!,
            JsonSerializer.SerializeToUtf8Bytes(payload, payload.GetType()),
            "json",
            1,
            timestamp,
            "corr-1",
            messageId,
            "actor-1");
}
