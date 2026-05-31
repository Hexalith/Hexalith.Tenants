using System.Text.Json;

using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Identity;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Server.Projections;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Projections;

public class TenantAuditReadModelTests {
    private static readonly DateTimeOffset Timestamp = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);

    public static IEnumerable<object[]> ClassifiedEvents() {
        yield return [new UserAddedToTenant("tenant-1", "user-1", TenantRole.TenantOwner), AuditEventCategory.Access, "UserAddedToTenant", "tenant-1"];
        yield return [new UserRemovedFromTenant("tenant-1", "user-1"), AuditEventCategory.Access, "UserRemovedFromTenant", "tenant-1"];
        yield return [new UserRoleChanged("tenant-1", "user-1", TenantRole.TenantReader, TenantRole.TenantContributor), AuditEventCategory.Access, "UserRoleChanged", "tenant-1"];
        yield return [
            new GlobalAdministratorSet(TenantIdentity.DefaultTenantId, "admin-2", "admin-1", Timestamp),
            AuditEventCategory.Access,
            "GlobalAdministratorSet",
            TenantIdentity.DefaultTenantId];
        yield return [
            new GlobalAdministratorRemoved(TenantIdentity.DefaultTenantId, "admin-2", "admin-1", Timestamp),
            AuditEventCategory.Access,
            "GlobalAdministratorRemoved",
            TenantIdentity.DefaultTenantId];
        yield return [new TenantCreated("tenant-1", "Acme", null, Timestamp), AuditEventCategory.Administrative, "TenantCreated", "tenant-1"];
        yield return [new TenantUpdated("tenant-1", "Acme Updated", null, Timestamp), AuditEventCategory.Administrative, "TenantUpdated", "tenant-1"];
        yield return [new TenantDisabled("tenant-1", Timestamp), AuditEventCategory.Administrative, "TenantDisabled", "tenant-1"];
        yield return [new TenantEnabled("tenant-1", Timestamp), AuditEventCategory.Administrative, "TenantEnabled", "tenant-1"];
        yield return [new TenantConfigurationSet("tenant-1", "theme", "dark"), AuditEventCategory.Administrative, "TenantConfigurationSet", "tenant-1"];
        yield return [new TenantConfigurationRemoved("tenant-1", "theme"), AuditEventCategory.Administrative, "TenantConfigurationRemoved", "tenant-1"];
    }

    [Theory]
    [MemberData(nameof(ClassifiedEvents))]
    public void Apply_classifies_supported_event_types(IEventPayload payload, AuditEventCategory category, string eventType, string tenantId) {
        ArgumentNullException.ThrowIfNull(payload);

        var model = new TenantAuditReadModel();

        model.Apply(CreateEvent(payload, "evt-1", "actor-1", Timestamp));

        TenantAuditEntry entry = model.Entries.Single();
        entry.EventId.ShouldBe("evt-1");
        entry.EventType.ShouldBe(eventType);
        entry.Category.ShouldBe(category);
        entry.ActorId.ShouldBe("actor-1");
        entry.Timestamp.ShouldBe(Timestamp);
        entry.TenantId.ShouldBe(tenantId);
        entry.NarrativePayload.ShouldNotBeEmpty();
    }

    [Fact]
    public void Apply_builds_stable_narrative_payload_fields() {
        var model = new TenantAuditReadModel();

        model.Apply(CreateEvent(new UserRoleChanged("tenant-1", "user-1", TenantRole.TenantReader, TenantRole.TenantContributor)));
        model.Apply(CreateEvent(new TenantConfigurationSet("tenant-1", "secret-key", "do-not-store"), messageId: "evt-2"));
        model.Apply(CreateEvent(
            new GlobalAdministratorSet(TenantIdentity.DefaultTenantId, "admin-2", "admin-1", Timestamp),
            messageId: "evt-3"));
        model.Apply(CreateEvent(
            new GlobalAdministratorRemoved(TenantIdentity.DefaultTenantId, "admin-2", "admin-1", Timestamp.AddMinutes(1)),
            messageId: "evt-4"));
        model.Apply(CreateEvent(
            new TenantUpdated("tenant-1", "Acme Updated", null, Timestamp.AddMinutes(2)),
            messageId: "evt-5"));

        model.Entries[0].NarrativePayload["userId"].ShouldBe("user-1");
        model.Entries[0].NarrativePayload["oldRole"].ShouldBe("TenantReader");
        model.Entries[0].NarrativePayload["newRole"].ShouldBe("TenantContributor");
        model.Entries[1].NarrativePayload["key"].ShouldBe("secret-key");
        model.Entries[1].NarrativePayload.Values.ShouldNotContain("do-not-store");
        model.Entries[2].NarrativePayload["userId"].ShouldBe("admin-2");
        model.Entries[2].NarrativePayload["actorUserId"].ShouldBe("admin-1");
        model.Entries[2].NarrativePayload["setAt"].ShouldBe(Timestamp.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
        model.Entries[3].NarrativePayload["userId"].ShouldBe("admin-2");
        model.Entries[3].NarrativePayload["actorUserId"].ShouldBe("admin-1");
        model.Entries[3].NarrativePayload["removedAt"].ShouldBe(Timestamp.AddMinutes(1).ToString("O", System.Globalization.CultureInfo.InvariantCulture));
        model.Entries[4].NarrativePayload["updatedAt"].ShouldBe(Timestamp.AddMinutes(2).ToString("O", System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void New_model_has_empty_entries() {
        var model = new TenantAuditReadModel();

        model.Entries.ShouldBeEmpty();
    }

    [Fact]
    public void SortEntries_orders_entries_by_timestamp_then_event_id() {
        var model = new TenantAuditReadModel();

        model.Apply(CreateEvent(new TenantCreated("tenant-1", "Third", null, Timestamp.AddMinutes(1)), "evt-c", timestamp: Timestamp.AddMinutes(1)));
        model.Apply(CreateEvent(new TenantCreated("tenant-1", "Second", null, Timestamp), "evt-b", timestamp: Timestamp));
        model.Apply(CreateEvent(new TenantCreated("tenant-1", "First", null, Timestamp), "evt-a", timestamp: Timestamp));

        model.SortEntries();

        model.Entries.Select(e => e.EventId).ShouldBe(["evt-a", "evt-b", "evt-c"]);
    }

    [Fact]
    public void Apply_preserves_insertion_order_until_SortEntries_is_called() {
        var model = new TenantAuditReadModel();

        model.Apply(CreateEvent(new TenantCreated("tenant-1", "Third", null, Timestamp.AddMinutes(1)), "evt-c", timestamp: Timestamp.AddMinutes(1)));
        model.Apply(CreateEvent(new TenantCreated("tenant-1", "Second", null, Timestamp), "evt-b", timestamp: Timestamp));
        model.Apply(CreateEvent(new TenantCreated("tenant-1", "First", null, Timestamp), "evt-a", timestamp: Timestamp));

        model.Entries.Select(e => e.EventId).ShouldBe(["evt-c", "evt-b", "evt-a"]);
    }

    [Fact]
    public void Apply_ignores_unknown_events() {
        var model = new TenantAuditReadModel();
        ProjectionEventDto evt = new(
            "UnknownEvent",
            JsonSerializer.SerializeToUtf8Bytes(new { tenantId = "tenant-1" }),
            "json",
            1,
            Timestamp,
            "corr-1",
            "evt-1",
            "actor-1");

        model.Apply(evt);

        model.Entries.ShouldBeEmpty();
    }

    [Fact]
    public void Apply_throws_when_message_id_is_missing() {
        var model = new TenantAuditReadModel();
        ProjectionEventDto evt = CreateEvent(new TenantCreated("tenant-1", "Acme", null, Timestamp), messageId: null);

        _ = Should.Throw<InvalidOperationException>(() => model.Apply(evt));
        model.Entries.ShouldBeEmpty();
    }

    [Fact]
    public void Apply_throws_when_user_id_is_missing() {
        var model = new TenantAuditReadModel();
        ProjectionEventDto evt = CreateEvent(new TenantCreated("tenant-1", "Acme", null, Timestamp), messageId: "evt-2", userId: null);

        _ = Should.Throw<InvalidOperationException>(() => model.Apply(evt));
        model.Entries.ShouldBeEmpty();
    }

    private static ProjectionEventDto CreateEvent(
        IEventPayload payload,
        string? messageId = "evt-1",
        string? userId = "actor-1",
        DateTimeOffset? timestamp = null) =>
        new(
            payload.GetType().FullName!,
            JsonSerializer.SerializeToUtf8Bytes(payload, payload.GetType()),
            "json",
            1,
            timestamp ?? Timestamp,
            "corr-1",
            messageId,
            userId);
}
