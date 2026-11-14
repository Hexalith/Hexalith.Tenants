using System.Text.Json;

#if HEXALITH_CHECKPOINTED_TENANT_PROJECTION
using Hexalith.EventStore.Client.Projections.Checkpointed;
#endif
using Hexalith.EventStore.Client.Subscriptions;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Events.Rejections;

using Shouldly;

namespace Hexalith.Tenants.Client.Tests.Projections;

/// <summary>Proves the extracted tenant transition is pure and covers every projected tenant event.</summary>
public sealed class TenantProjectionTransitionTests {
    private const string Tenant = "tenant-pure";
    private static readonly DateTimeOffset s_at = new(2026, 11, 1, 8, 0, 0, TimeSpan.Zero);

    public static TheoryData<IEventPayload, Action<TenantLocalState>> ProjectedEvents => new() {
        { new TenantCreated(Tenant, "Created", "description", s_at), static s => { s.Name.ShouldBe("Created"); s.Description.ShouldBe("description"); s.Status.ShouldBe(TenantStatus.Active); } },
        { new TenantUpdated(Tenant, "Renamed", null, s_at), static s => { s.Name.ShouldBe("Renamed"); s.Description.ShouldBeNull(); } },
        { new TenantAccessRefreshed(Tenant, TenantStatus.Disabled, new Dictionary<string, TenantRole> { ["snapshot-only"] = TenantRole.TenantOwner }, []),
            static s => { s.Status.ShouldBe(TenantStatus.Active); s.Members.ShouldNotContainKey("snapshot-only"); } },
        { new TenantDisabled(Tenant, s_at), static s => s.Status.ShouldBe(TenantStatus.Disabled) },
        { new TenantEnabled(Tenant, s_at), static s => s.Status.ShouldBe(TenantStatus.Active) },
        { new UserAddedToTenant(Tenant, "user-2", TenantRole.TenantContributor), static s => s.Members["user-2"].ShouldBe(TenantRole.TenantContributor) },
        { new UserRemovedFromTenant(Tenant, "user-1"), static s => s.Members.ShouldNotContainKey("user-1") },
        { new UserRoleChanged(Tenant, "user-1", TenantRole.TenantReader, TenantRole.TenantOwner), static s => s.Members["user-1"].ShouldBe(TenantRole.TenantOwner) },
        { new TenantConfigurationSet(Tenant, "theme", "dark"), static s => s.Configuration["theme"].ShouldBe("dark") },
        { new TenantConfigurationRemoved(Tenant, "locale"), static s => s.Configuration.ShouldNotContainKey("locale") },
    };

    [Theory]
    [MemberData(nameof(ProjectedEvents))]
    public void ApplyProjectsTheEventIntoACopyAndNeverMutatesTheInput(IEventPayload @event, Action<TenantLocalState> assert) {
        ArgumentNullException.ThrowIfNull(assert);
        TenantLocalState current = Seed();
        string before = JsonSerializer.Serialize(current);
        var metadata = new TenantProjectionEventMetadata("message-7", 7, s_at, "correlation-7");

        TenantLocalState next = TenantProjectionTransition.Apply(current, Tenant, @event, metadata);

        next.ShouldNotBeSameAs(current);
        JsonSerializer.Serialize(current).ShouldBe(before);
        next.TenantId.ShouldBe(Tenant);
        next.LastEvent.ShouldBe(metadata);
        assert(next);
    }

    [Fact]
    public void ProjectedEventTypesCoverEveryHandledTenantEvent()
        => TenantProjectionTransition.ProjectedEventTypes.Count.ShouldBe(10);

    [Fact]
    public void NonProjectedEventsOnlyAdvanceTheCheckpointMetadata() {
        TenantLocalState current = Seed();
        var metadata = new TenantProjectionEventMetadata("message-8", 8, s_at, "correlation-8");

        TenantLocalState next = TenantProjectionTransition.Apply(current, Tenant, new TenantDisabledRejection(Tenant), metadata);

        next.Status.ShouldBe(current.Status);
        next.Members.ShouldBe(current.Members);
        next.LastEvent.ShouldBe(metadata);
    }

    [Fact]
    public void FirstEventCreatesStateForTheManagedTenant() {
        TenantLocalState next = TenantProjectionTransition.Apply(
            null,
            Tenant,
            new TenantCreated(Tenant, "First", null, s_at),
            new TenantProjectionEventMetadata("message-1", 1, s_at, "correlation-1"));

        next.TenantId.ShouldBe(Tenant);
        next.Status.ShouldBe(TenantStatus.Active);
    }

#if HEXALITH_CHECKPOINTED_TENANT_PROJECTION
    [Fact]
    public void CheckpointTransitionDecodesAuthoritativeHistoryLikeTheLiveDelivery() {
        var created = new TenantCreated(Tenant, "Replayed", "from history", s_at);
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(created);
        var stream = new CheckpointedProjectionStream("system", "tenants", Tenant);
        var evidence = new CheckpointedProjectionEvidence("message-1", typeof(TenantCreated).FullName!, EventStorePayloadFingerprint.Compute(payload));
        var history = new CheckpointedProjectionEvent(stream, 1, evidence, s_at, "correlation-1") { Payload = payload, SerializationFormat = "json" };
        var live = new CheckpointedProjectionEvent(stream, 1, evidence, s_at, "correlation-1") { DecodedEvent = created };
        var rejection = new CheckpointedProjectionEvent(
            stream,
            2,
            new CheckpointedProjectionEvidence("message-2", typeof(TenantDisabledRejection).FullName!, EventStorePayloadFingerprint.Compute([])),
            s_at,
            null) { Payload = [] };

        TenantLocalState replayed = TenantProjectionCheckpointTransition.Instance.Apply(null, history).ShouldNotBeNull();
        TenantLocalState delivered = TenantProjectionCheckpointTransition.Instance.Apply(null, live).ShouldNotBeNull();
        TenantLocalState advanced = TenantProjectionCheckpointTransition.Instance.Apply(replayed, rejection).ShouldNotBeNull();
        TenantLocalState? beforeCreation = TenantProjectionCheckpointTransition.Instance.Apply(null, rejection);

        JsonSerializer.Serialize(replayed).ShouldBe(JsonSerializer.Serialize(delivered));
        replayed.Name.ShouldBe("Replayed");
        advanced.LastEvent.ShouldNotBeNull().LastSequenceNumber.ShouldBe(2);
        advanced.Name.ShouldBe("Replayed");
        beforeCreation.ShouldBeNull("events before TenantCreated never materialize tenant state");
    }
#endif

    private static TenantLocalState Seed() {
        var state = new TenantLocalState { TenantId = Tenant, Name = "Seed", Status = TenantStatus.Active };
        state.Members["user-1"] = TenantRole.TenantReader;
        state.Configuration["locale"] = "fr";
        return state;
    }
}
