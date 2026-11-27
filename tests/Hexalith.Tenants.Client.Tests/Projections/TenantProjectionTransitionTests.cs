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
        byte[] rejectionPayload = JsonSerializer.SerializeToUtf8Bytes(new TenantDisabledRejection(Tenant));
        var rejection = new CheckpointedProjectionEvent(
            stream,
            2,
            new CheckpointedProjectionEvidence("message-2", typeof(TenantDisabledRejection).FullName!, EventStorePayloadFingerprint.Compute(rejectionPayload)),
            s_at,
            null) { Payload = rejectionPayload, SerializationFormat = "json" };

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

    [Fact]
    public void CheckpointTransitionDecodesEverySupportedContractAndRejectsInvalidInputs() {
        var stream = new CheckpointedProjectionStream("system", "tenants", Tenant);
        TenantLocalState seeded = Seed();

        // Every projected and rejection event type exposes TenantId and decodes from JSON history.
        IEventPayload[] samples = [
            new TenantCreated(Tenant, "Created", "desc", s_at),
            new TenantUpdated(Tenant, "Updated", null, s_at),
            new TenantAccessRefreshed(Tenant, TenantStatus.Disabled, new Dictionary<string, TenantRole> { ["snapshot"] = TenantRole.TenantOwner }, []),
            new TenantDisabled(Tenant, s_at),
            new TenantEnabled(Tenant, s_at),
            new UserAddedToTenant(Tenant, "user-2", TenantRole.TenantReader),
            new UserRemovedFromTenant(Tenant, "user-2"),
            new UserRoleChanged(Tenant, "user-1", TenantRole.TenantReader, TenantRole.TenantOwner),
            new TenantConfigurationSet(Tenant, "k", "v"),
            new TenantConfigurationRemoved(Tenant, "k"),
            new ConfigurationKeyNotFoundRejection(Tenant, "k"),
            new ConfigurationLimitExceededRejection(Tenant, "keys", 101, 100),
            new InsufficientPermissionsRejection(Tenant, "user-1", TenantRole.TenantReader, "UpdateTenant"),
            new RoleEscalationRejection(Tenant, "user-1", TenantRole.TenantOwner),
            new SyntheticTenantIdInvalidRejection(Tenant),
            new TenantAccessSnapshotLimitExceededRejection(Tenant, "members", 101, 100),
            new TenantAccessStateMalformedRejection(Tenant, "status"),
            new TenantAlreadyExistsRejection(Tenant),
            new TenantDisabledRejection(Tenant),
            new TenantLifecycleStateAlreadySetRejection(Tenant, TenantStatus.Active, TenantStatus.Active, "EnableTenant"),
            new TenantNotFoundRejection(Tenant),
            new UserAlreadyInTenantRejection(Tenant, "user-1", TenantRole.TenantReader),
            new UserNotInTenantRejection(Tenant, "user-1"),
        ];

        HashSet<Type> expectedTypes = [.. TenantProjectionTransition.ProjectedEventTypes.Concat(TenantProjectionTransition.RejectionEventTypes)];
        samples.Select(static s => s.GetType()).ToHashSet().ShouldBe(expectedTypes);

        long sequence = 10;
        foreach (IEventPayload sample in samples) {
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(sample, sample.GetType());
            var history = new CheckpointedProjectionEvent(
                stream,
                ++sequence,
                new CheckpointedProjectionEvidence($"msg-{sequence}", sample.GetType().FullName!, EventStorePayloadFingerprint.Compute(bytes)),
                s_at,
                "corr") { Payload = bytes, SerializationFormat = "json" };

            TenantLocalState next = TenantProjectionCheckpointTransition.Instance.Apply(seeded, history).ShouldNotBeNull();
            next.LastEvent.ShouldNotBeNull().LastSequenceNumber.ShouldBe(sequence);
        }

        // Failure paths: non-IEventPayload DecodedEvent, mismatched live TenantId, unknown event type, null format, mismatched history TenantId.
        var nonPayload = new CheckpointedProjectionEvent(
            stream,
            100,
            new CheckpointedProjectionEvidence("msg-100", typeof(TenantCreated).FullName!, EventStorePayloadFingerprint.Compute([])),
            s_at,
            null) { DecodedEvent = new object() };
        Should.Throw<NotSupportedException>(() => TenantProjectionCheckpointTransition.Instance.Apply(seeded, nonPayload));

        var foreignLive = new CheckpointedProjectionEvent(
            stream,
            101,
            new CheckpointedProjectionEvidence("msg-101", typeof(UserAddedToTenant).FullName!, EventStorePayloadFingerprint.Compute([])),
            s_at,
            null) { DecodedEvent = new UserAddedToTenant("other-tenant", "u", TenantRole.TenantOwner) };
        Should.Throw<InvalidDataException>(() => TenantProjectionCheckpointTransition.Instance.Apply(seeded, foreignLive));

        byte[] unknownBytes = JsonSerializer.SerializeToUtf8Bytes(new TenantCreated(Tenant, "X", null, s_at));
        var unknown = new CheckpointedProjectionEvent(
            stream,
            102,
            new CheckpointedProjectionEvidence("msg-102", "Hexalith.Tenants.Contracts.Events.UnknownV2", EventStorePayloadFingerprint.Compute(unknownBytes)),
            s_at,
            null) { Payload = unknownBytes, SerializationFormat = "json" };
        NotSupportedException unknownEx = Should.Throw<NotSupportedException>(() => TenantProjectionCheckpointTransition.Instance.Apply(seeded, unknown));
        unknownEx.Message.ShouldContain("UnknownV2");

        var nullFormat = new CheckpointedProjectionEvent(
            stream,
            103,
            new CheckpointedProjectionEvidence("msg-103", typeof(TenantDisabledRejection).FullName!, EventStorePayloadFingerprint.Compute(unknownBytes)),
            s_at,
            null) { Payload = unknownBytes, SerializationFormat = null };
        NotSupportedException formatEx = Should.Throw<NotSupportedException>(() => TenantProjectionCheckpointTransition.Instance.Apply(seeded, nullFormat));
        formatEx.Message.ShouldContain("<null>");

        byte[] foreignHistoryBytes = JsonSerializer.SerializeToUtf8Bytes(new TenantDisabledRejection("other-tenant"));
        var foreignHistory = new CheckpointedProjectionEvent(
            stream,
            104,
            new CheckpointedProjectionEvidence("msg-104", typeof(TenantDisabledRejection).FullName!, EventStorePayloadFingerprint.Compute(foreignHistoryBytes)),
            s_at,
            null) { Payload = foreignHistoryBytes, SerializationFormat = "json" };
        Should.Throw<InvalidDataException>(() => TenantProjectionCheckpointTransition.Instance.Apply(seeded, foreignHistory));
    }
#endif

    private static TenantLocalState Seed() {
        var state = new TenantLocalState { TenantId = Tenant, Name = "Seed", Status = TenantStatus.Active };
        state.Members["user-1"] = TenantRole.TenantReader;
        state.Configuration["locale"] = "fr";
        return state;
    }
}
