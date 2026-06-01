using System.Text.Json;

using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Identity;
using Hexalith.Tenants.Server.Projections;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Projections;

public class TenantAuditProjectionTests {
    [Fact]
    public void TenantAuditProjection_is_static_helper_not_discoverable_projection() {
        Type type = typeof(TenantAuditProjection);

        // EventStore projection discovery picks up instantiable classes; a static helper
        // must remain abstract+sealed (the IL shape of `static class`) so it cannot be
        // accidentally registered as a domain projection.
        (type.IsAbstract && type.IsSealed).ShouldBeTrue();
        type.GetInterfaces().ShouldBeEmpty();
    }

    [Fact]
    public void Project_continues_when_one_event_has_malformed_payload() {
        DateTimeOffset timestamp = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);
        ProjectionEventDto malformed = new(
            typeof(TenantCreated).FullName!,
            System.Text.Encoding.UTF8.GetBytes("{not valid json"),
            "json",
            1,
            timestamp,
            "corr-1",
            "evt-malformed",
            "actor-1");
        ProjectionEventDto[] events = [
            malformed,
            CreateEvent(new TenantUpdated("tenant-1", "Acme Updated", null, timestamp.AddMinutes(1)), "evt-good", timestamp.AddMinutes(1)),
        ];

        TenantAuditReadModel result = TenantAuditProjection.ProjectAuditEvents(events);

        result.Entries.Single().EventId.ShouldBe("evt-good");
    }

    [Fact]
    public void Project_propagates_invariant_violation_when_metadata_missing() {
        DateTimeOffset timestamp = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);
        ProjectionEventDto bad = new(
            typeof(TenantCreated).FullName!,
            JsonSerializer.SerializeToUtf8Bytes(new TenantCreated("tenant-1", "Acme", null, timestamp), typeof(TenantCreated)),
            "json",
            1,
            timestamp,
            "corr-1",
            MessageId: null,
            UserId: "actor-1");

        _ = Should.Throw<InvalidOperationException>(() => TenantAuditProjection.ProjectAuditEvents([bad]));
    }

    [Fact]
    public void Project_WithFullEventList_ReturnsAuditEntriesForAllSupportedEvents() {
        DateTimeOffset timestamp = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);
        ProjectionEventDto[] events = [
            CreateEvent(new TenantCreated("tenant-1", "Acme", null, timestamp), "evt-1", timestamp),
            CreateEvent(new TenantUpdated("tenant-1", "Acme Updated", null, timestamp.AddMinutes(1)), "evt-2", timestamp.AddMinutes(1)),
            CreateEvent(new TenantDisabled("tenant-1", timestamp.AddMinutes(2)), "evt-3", timestamp.AddMinutes(2)),
            CreateEvent(new TenantEnabled("tenant-1", timestamp.AddMinutes(3)), "evt-4", timestamp.AddMinutes(3)),
            CreateEvent(new UserAddedToTenant("tenant-1", "user-1", TenantRole.TenantReader), "evt-5", timestamp.AddMinutes(4)),
            CreateEvent(new UserRemovedFromTenant("tenant-1", "user-1"), "evt-6", timestamp.AddMinutes(5)),
            CreateEvent(new UserRoleChanged("tenant-1", "user-1", TenantRole.TenantReader, TenantRole.TenantContributor), "evt-7", timestamp.AddMinutes(6)),
            CreateEvent(new TenantConfigurationSet("tenant-1", "secret-key", "secret-value"), "evt-8", timestamp.AddMinutes(7)),
            CreateEvent(new TenantConfigurationRemoved("tenant-1", "secret-key"), "evt-9", timestamp.AddMinutes(8)),
            CreateEvent(new GlobalAdministratorSet(TenantIdentity.DefaultTenantId, "admin-2", "admin-1", timestamp.AddMinutes(9)), "evt-10", timestamp.AddMinutes(9)),
            CreateEvent(new GlobalAdministratorRemoved(TenantIdentity.DefaultTenantId, "admin-2", "admin-1", timestamp.AddMinutes(10)), "evt-11", timestamp.AddMinutes(10)),
        ];

        TenantAuditReadModel result = TenantAuditProjection.ProjectAuditEvents(events);

        result.Entries.Count.ShouldBe(11);
        result.Entries.Select(e => e.EventType).ShouldBe([
            "TenantCreated",
            "TenantUpdated",
            "TenantDisabled",
            "TenantEnabled",
            "UserAddedToTenant",
            "UserRemovedFromTenant",
            "UserRoleChanged",
            "TenantConfigurationSet",
            "TenantConfigurationRemoved",
            "GlobalAdministratorSet",
            "GlobalAdministratorRemoved",
        ]);
        result.Entries[7].NarrativePayload["key"].ShouldBe("secret-key");
        result.Entries[7].NarrativePayload.Values.ShouldNotContain("secret-value");
    }

    [Fact]
    public void Project_sorts_entries_by_timestamp_then_event_id() {
        DateTimeOffset timestamp = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);
        ProjectionEventDto[] events = [
            CreateEvent(new TenantUpdated("tenant-1", "Third", null, timestamp.AddMinutes(1)), "evt-c", timestamp.AddMinutes(1)),
            CreateEvent(new TenantCreated("tenant-1", "Second", null, timestamp), "evt-b", timestamp),
            CreateEvent(new TenantCreated("tenant-1", "First", null, timestamp), "evt-a", timestamp),
        ];

        TenantAuditReadModel result = TenantAuditProjection.ProjectAuditEvents(events);

        result.Entries.Select(e => e.EventId).ShouldBe(["evt-a", "evt-b", "evt-c"]);
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
