#if HEXALITH_CHECKPOINTED_TENANT_PROJECTION
using Hexalith.EventStore.Client.Projections.Checkpointed;
using Hexalith.EventStore.Client.Subscriptions;
using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Events.Rejections;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.Tenants.Client.Tests.Projections;

/// <summary>
/// Proves the opt-in durable tenant projection through the production consumer path: persisted state, health,
/// and checkpoint survive restart and are shared by replicas, and every non-healthy record fails closed.
/// </summary>
public sealed class DurableTenantProjectionStoreTests {
    private const string Tenant = "tenant-durable";
    private static readonly DateTimeOffset s_at = new(2026, 11, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AcknowledgedEventsPersistOneHealthyRecordThatARestartedHostReadsWithoutRedelivery() {
        var harness = new DurableTenantProjectionHarness();
        using (ServiceProvider first = harness.CreateHost()) {
            (await DurableTenantProjectionHarness.ProcessAsync(first, harness.Persist(new TenantCreated(Tenant, "Durable", null, s_at), Tenant, 1)))
                .ShouldBe(EventStoreDomainEventProcessingResult.Processed);
            (await DurableTenantProjectionHarness.ProcessAsync(first, harness.Persist(new UserAddedToTenant(Tenant, "user-1", TenantRole.TenantOwner), Tenant, 2)))
                .ShouldBe(EventStoreDomainEventProcessingResult.Processed);
        }

        CheckpointedProjectionRecord<TenantLocalState> persisted = harness.Record(Tenant).ShouldNotBeNull();
        persisted.Health.ShouldBe(CheckpointedProjectionHealth.Healthy);
        persisted.LastContiguousSequence.ShouldBe(2);
        persisted.State.ShouldNotBeNull().Members["user-1"].ShouldBe(TenantRole.TenantOwner);
        persisted.LastAccepted.ShouldNotBeNull().PayloadFingerprint.Length.ShouldBe(64);

        using ServiceProvider restarted = harness.CreateHost();
        TenantProjectionSnapshot snapshot = (await restarted.GetRequiredService<ITenantProjectionSnapshotStore>()
            .GetSnapshotAsync(Tenant, TestContext.Current.CancellationToken)).ShouldNotBeNull();
        snapshot.IsHealthy.ShouldBeTrue();
        snapshot.Checkpoint.ShouldBe(2);
        snapshot.RecordVersion.ShouldBe(persisted.Version);
        snapshot.State.ShouldNotBeNull().Name.ShouldBe("Durable");
        snapshot.State.Status.ShouldBe(TenantStatus.Active);
        (await restarted.GetRequiredService<ITenantProjectionStore>().GetAsync(Tenant, TestContext.Current.CancellationToken))
            .ShouldNotBeNull().Members.ShouldContainKey("user-1");
    }

    [Fact]
    public async Task ConsumerGroupDeliveryAcrossTwoReplicasConvergesOnOneRecord() {
        var harness = new DurableTenantProjectionHarness();
        using ServiceProvider replicaA = harness.CreateHost();
        using ServiceProvider replicaB = harness.CreateHost();
        EventStoreDomainEventEnvelope created = harness.Persist(new TenantCreated(Tenant, "Shared", null, s_at), Tenant, 1);
        EventStoreDomainEventEnvelope member = harness.Persist(new UserAddedToTenant(Tenant, "user-1", TenantRole.TenantReader), Tenant, 2);
        EventStoreDomainEventEnvelope disabled = harness.Persist(new TenantDisabled(Tenant, s_at), Tenant, 3);

        // Each event is delivered once, to one replica, in arrival order 1, 3, 2.
        (await DurableTenantProjectionHarness.ProcessAsync(replicaA, created)).ShouldBe(EventStoreDomainEventProcessingResult.Processed);
        (await DurableTenantProjectionHarness.ProcessAsync(replicaB, disabled)).ShouldBe(EventStoreDomainEventProcessingResult.Processed);
        (await DurableTenantProjectionHarness.ProcessAsync(replicaA, member)).ShouldBe(EventStoreDomainEventProcessingResult.Processed);

        TenantProjectionSnapshot a = (await replicaA.GetRequiredService<ITenantProjectionSnapshotStore>().GetSnapshotAsync(Tenant, TestContext.Current.CancellationToken)).ShouldNotBeNull();
        TenantProjectionSnapshot b = (await replicaB.GetRequiredService<ITenantProjectionSnapshotStore>().GetSnapshotAsync(Tenant, TestContext.Current.CancellationToken)).ShouldNotBeNull();
        a.IsHealthy.ShouldBeTrue();
        a.Checkpoint.ShouldBe(3);
        b.RecordVersion.ShouldBe(a.RecordVersion);
        a.State.ShouldNotBeNull().Status.ShouldBe(TenantStatus.Disabled, "the disable delivered out of order is never lost or regressed");
        b.State.ShouldNotBeNull().Members.ShouldContainKey("user-1");
    }

    [Fact]
    public async Task UnrecoverableGapPersistsGappedDeniesAndKeepsTheDeliveryRetryable() {
        var harness = new DurableTenantProjectionHarness();
        using ServiceProvider host = harness.CreateHost(withHistory: false);
        _ = await DurableTenantProjectionHarness.ProcessAsync(host, harness.Persist(new TenantCreated(Tenant, "Gapped", null, s_at), Tenant, 1));
        _ = harness.Persist(new UserAddedToTenant(Tenant, "user-1", TenantRole.TenantOwner), Tenant, 2);
        EventStoreDomainEventEnvelope future = harness.Persist(new TenantDisabled(Tenant, s_at), Tenant, 3);

        CheckpointedProjectionUnavailableException failure = await Should.ThrowAsync<CheckpointedProjectionUnavailableException>(
            () => DurableTenantProjectionHarness.ProcessAsync(host, future));

        failure.Reason.ShouldBe(CheckpointedProjectionUnavailableReason.HistoryUnavailable);
        CheckpointedProjectionRecord<TenantLocalState> record = harness.Record(Tenant).ShouldNotBeNull();
        record.Health.ShouldBe(CheckpointedProjectionHealth.Gapped);
        record.Expected.ShouldBe(2);
        record.RecoveryThrough.ShouldBe(3);
        TenantProjectionSnapshot snapshot = (await host.GetRequiredService<ITenantProjectionSnapshotStore>().GetSnapshotAsync(Tenant, TestContext.Current.CancellationToken)).ShouldNotBeNull();
        snapshot.Health.ShouldBe(CheckpointedProjectionHealth.Gapped);
        snapshot.State.ShouldBeNull();
        snapshot.IsHealthy.ShouldBeFalse();
        (await host.GetRequiredService<ITenantProjectionStore>().GetAsync(Tenant, TestContext.Current.CancellationToken)).ShouldBeNull();

        // The unacknowledged delivery is redelivered to a replica that can read authoritative history.
        using ServiceProvider recovering = harness.CreateHost();
        (await DurableTenantProjectionHarness.ProcessAsync(recovering, future)).ShouldBe(EventStoreDomainEventProcessingResult.Processed);
        TenantProjectionSnapshot recovered = (await recovering.GetRequiredService<ITenantProjectionSnapshotStore>().GetSnapshotAsync(Tenant, TestContext.Current.CancellationToken)).ShouldNotBeNull();
        recovered.IsHealthy.ShouldBeTrue();
        recovered.Checkpoint.ShouldBe(3);
        recovered.State.ShouldNotBeNull().Members["user-1"].ShouldBe(TenantRole.TenantOwner);
        recovered.State.Status.ShouldBe(TenantStatus.Disabled);
    }

    [Theory]
    [InlineData("json", 2)]
    [InlineData("json", 0)]
    [InlineData("json", -1)]
    [InlineData("binary", 1)]
    public async Task UnsupportedTenantHistoryMetadataKeepsTheRecordGapped(string format, int version) {
        var harness = new DurableTenantProjectionHarness();
        using ServiceProvider host = harness.CreateHost();
        _ = await DurableTenantProjectionHarness.ProcessAsync(
            host, harness.Persist(new TenantCreated(Tenant, "Created", null, s_at), Tenant, 1));
        _ = harness.Persist(new UserAddedToTenant(Tenant, "user-1", TenantRole.TenantReader), Tenant, 2);
        EventStoreDomainEventEnvelope future = harness.Persist(new TenantDisabled(Tenant, s_at), Tenant, 3);
        harness.Gateway.ReplaceHistoryMetadata(Tenant, 2, format, version);

        CheckpointedProjectionUnavailableException failure = await Should.ThrowAsync<CheckpointedProjectionUnavailableException>(
            () => DurableTenantProjectionHarness.ProcessAsync(host, future));

        failure.Reason.ShouldBe(CheckpointedProjectionUnavailableReason.TransitionFailed);
        CheckpointedProjectionRecord<TenantLocalState> record = harness.Record(Tenant).ShouldNotBeNull();
        record.Health.ShouldBe(CheckpointedProjectionHealth.Gapped);
        record.CorruptionReason.ShouldBeNull();
        record.Lease.ShouldBeNull();
        record.LastContiguousSequence.ShouldBe(1);
        (await host.GetRequiredService<ITenantProjectionStore>().GetAsync(Tenant, TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task ConflictingDeliveryAtTheAcceptedSequencePersistsCorruptAndDenies() {
        var harness = new DurableTenantProjectionHarness();
        using ServiceProvider host = harness.CreateHost();
        _ = await DurableTenantProjectionHarness.ProcessAsync(host, harness.Persist(new TenantCreated(Tenant, "Accepted", null, s_at), Tenant, 1));

        EventStoreDomainEventEnvelope forged = DurableTenantProjectionHarness.Envelope(new TenantCreated(Tenant, "Forged", null, s_at), Tenant, 1);
        (await DurableTenantProjectionHarness.ProcessAsync(host, forged)).ShouldBe(EventStoreDomainEventProcessingResult.Processed);

        CheckpointedProjectionRecord<TenantLocalState> record = harness.Record(Tenant).ShouldNotBeNull();
        record.Health.ShouldBe(CheckpointedProjectionHealth.Corrupt);
        record.CorruptionReason.ShouldBe(CheckpointedProjectionCorruptionReason.AcceptedIdentityMismatch);
        record.State.ShouldNotBeNull().Name.ShouldBe("Accepted", "a corrupt delivery never applies");
        TenantProjectionSnapshot snapshot = (await host.GetRequiredService<ITenantProjectionSnapshotStore>().GetSnapshotAsync(Tenant, TestContext.Current.CancellationToken)).ShouldNotBeNull();
        snapshot.Health.ShouldBe(CheckpointedProjectionHealth.Corrupt);
        snapshot.State.ShouldBeNull();
    }

    [Fact]
    public async Task MalformedRecordReadsAsUnavailableWithoutState() {
        var harness = new DurableTenantProjectionHarness();
        harness.Store.SeedRaw(
            DurableTenantProjectionHarness.StoreName,
            DurableTenantProjectionStore.KeyFor(Tenant),
            new CheckpointedProjectionRecord<TenantLocalState> { SchemaVersion = 1, Health = CheckpointedProjectionHealth.Gapped, LastContiguousSequence = 2 });
        using ServiceProvider host = harness.CreateHost();

        TenantProjectionSnapshot snapshot = (await host.GetRequiredService<ITenantProjectionSnapshotStore>().GetSnapshotAsync(Tenant, TestContext.Current.CancellationToken)).ShouldNotBeNull();

        snapshot.Health.ShouldBe(CheckpointedProjectionHealth.Unavailable);
        snapshot.State.ShouldBeNull();
        (await host.GetRequiredService<ITenantProjectionStore>().GetAsync(Tenant, TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task HealthyRecordWhoseStateNamesAnotherTenantReadsAsUnavailable() {
        var harness = new DurableTenantProjectionHarness();
        using ServiceProvider host = harness.CreateHost();
        _ = await DurableTenantProjectionHarness.ProcessAsync(host, harness.Persist(new TenantCreated("tenant-other", "Other", null, s_at), "tenant-other", 1));
        harness.Store.SeedRaw(
            DurableTenantProjectionHarness.StoreName,
            DurableTenantProjectionStore.KeyFor(Tenant),
            harness.Record("tenant-other")!);

        TenantProjectionSnapshot snapshot = (await host.GetRequiredService<ITenantProjectionSnapshotStore>().GetSnapshotAsync(Tenant, TestContext.Current.CancellationToken)).ShouldNotBeNull();

        snapshot.Health.ShouldBe(CheckpointedProjectionHealth.Unavailable);
        snapshot.State.ShouldBeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task HealthyRecordWithNullStateCollectionReadsAsUnavailable(bool nullMembers) {
        var harness = new DurableTenantProjectionHarness();
        using ServiceProvider host = harness.CreateHost();
        _ = await DurableTenantProjectionHarness.ProcessAsync(host, harness.Persist(new TenantCreated(Tenant, "Created", null, s_at), Tenant, 1));
        CheckpointedProjectionRecord<TenantLocalState> valid = harness.Record(Tenant).ShouldNotBeNull();
        TenantLocalState state = valid.State.ShouldNotBeNull();
        harness.Store.SeedRaw(
            DurableTenantProjectionHarness.StoreName,
            DurableTenantProjectionStore.KeyFor(Tenant),
            valid with { State = new TenantLocalState {
                TenantId = state.TenantId,
                LastEvent = state.LastEvent,
                Members = nullMembers ? null! : state.Members,
                Configuration = nullMembers ? state.Configuration : null!,
            } });

        TenantProjectionSnapshot snapshot = (await host.GetRequiredService<ITenantProjectionSnapshotStore>()
            .GetSnapshotAsync(Tenant, TestContext.Current.CancellationToken)).ShouldNotBeNull();

        snapshot.Health.ShouldBe(CheckpointedProjectionHealth.Unavailable);
        snapshot.State.ShouldBeNull();
        (await host.GetRequiredService<ITenantProjectionStore>().GetAsync(Tenant, TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task HealthyRecordWithMismatchedStateSequenceReadsAsUnavailable() {
        var harness = new DurableTenantProjectionHarness();
        using ServiceProvider host = harness.CreateHost();
        _ = await DurableTenantProjectionHarness.ProcessAsync(host, harness.Persist(new TenantCreated(Tenant, "Created", null, s_at), Tenant, 1));
        CheckpointedProjectionRecord<TenantLocalState> valid = harness.Record(Tenant).ShouldNotBeNull();
        TenantLocalState state = valid.State.ShouldNotBeNull().Clone();
        state.LastEvent = state.LastEvent.ShouldNotBeNull() with { LastSequenceNumber = 2 };
        harness.Store.SeedRaw(DurableTenantProjectionHarness.StoreName, DurableTenantProjectionStore.KeyFor(Tenant), valid with { State = state });

        TenantProjectionSnapshot snapshot = (await host.GetRequiredService<ITenantProjectionSnapshotStore>()
            .GetSnapshotAsync(Tenant, TestContext.Current.CancellationToken)).ShouldNotBeNull();

        snapshot.Health.ShouldBe(CheckpointedProjectionHealth.Unavailable);
        snapshot.State.ShouldBeNull();
        (await host.GetRequiredService<ITenantProjectionStore>().GetAsync(Tenant, TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task MissingRecordReadsAsAbsent() {
        var harness = new DurableTenantProjectionHarness();
        using ServiceProvider host = harness.CreateHost();

        (await host.GetRequiredService<ITenantProjectionSnapshotStore>().GetSnapshotAsync(Tenant, TestContext.Current.CancellationToken)).ShouldBeNull();
        (await host.GetRequiredService<ITenantProjectionStore>().GetAsync(Tenant, TestContext.Current.CancellationToken)).ShouldBeNull();
        harness.Gateway.Reads.ShouldBe(0, "reads never start recovery or verification");
    }

    [Fact]
    public async Task UnconditionalSaveIsRefused() {
        var harness = new DurableTenantProjectionHarness();
        using ServiceProvider host = harness.CreateHost();

        _ = await Should.ThrowAsync<NotSupportedException>(() => host.GetRequiredService<ITenantProjectionStore>()
            .SaveAsync(new TenantLocalState { TenantId = Tenant }, TestContext.Current.CancellationToken));
        harness.Record(Tenant).ShouldBeNull();
    }

    [Fact]
    public async Task RejectionOnlyStreamOfANeverCreatedTenantAdvancesTheCheckpointWithoutTenantState() {
        var harness = new DurableTenantProjectionHarness();
        using ServiceProvider host = harness.CreateHost(withHistory: false);
        string never = "tenant-never-created";

        (await DurableTenantProjectionHarness.ProcessAsync(host, harness.Persist(new TenantNotFoundRejection(never), never, 1)))
            .ShouldBe(EventStoreDomainEventProcessingResult.Processed);
        (await DurableTenantProjectionHarness.ProcessAsync(host, harness.Persist(new TenantDisabledRejection(never), never, 2)))
            .ShouldBe(EventStoreDomainEventProcessingResult.Processed);

        CheckpointedProjectionRecord<TenantLocalState> record = harness.Record(never).ShouldNotBeNull();
        record.Health.ShouldBe(CheckpointedProjectionHealth.Healthy);
        record.LastContiguousSequence.ShouldBe(2);
        record.State.ShouldBeNull("rejections never materialize tenant state");
        (await host.GetRequiredService<ITenantProjectionSnapshotStore>().GetSnapshotAsync(never, TestContext.Current.CancellationToken)).ShouldBeNull();
        (await host.GetRequiredService<ITenantProjectionStore>().GetAsync(never, TestContext.Current.CancellationToken)).ShouldBeNull();
        harness.Gateway.Reads.ShouldBe(0);
    }

    [Fact]
    public async Task GlobalAdministratorsDomainDeliveriesNeverTouchATenantNamedGlobalAdministrators() {
        var harness = new DurableTenantProjectionHarness();
        using ServiceProvider host = harness.CreateHost(withHistory: false);
        const string colliding = "global-administrators";
        (await DurableTenantProjectionHarness.ProcessAsync(host, harness.Persist(new TenantCreated(colliding, "Colliding", null, s_at), colliding, 1)))
            .ShouldBe(EventStoreDomainEventProcessingResult.Processed);
        CheckpointedProjectionRecord<TenantLocalState> before = harness.Record(colliding).ShouldNotBeNull();

        // Same aggregate identifier and sequence, but the global-administrators domain: a shared rejection type is
        // acknowledged without writing, and a global-administrator-only rejection has no tenant handler at all.
        EventStoreDomainEventEnvelope shared = DurableTenantProjectionHarness.Envelope(
            new InsufficientPermissionsRejection(colliding, "admin-1", null, "BootstrapGlobalAdmin"), colliding, 1) with { Domain = "global-administrators" };
        EventStoreDomainEventEnvelope globalOnly = DurableTenantProjectionHarness.Envelope(
            new GlobalAdministratorNotFoundRejection(colliding, "admin-1"), colliding, 2) with { Domain = "global-administrators" };
        (await DurableTenantProjectionHarness.ProcessAsync(host, shared)).ShouldBe(EventStoreDomainEventProcessingResult.Processed);
        (await DurableTenantProjectionHarness.ProcessAsync(host, globalOnly)).ShouldBe(EventStoreDomainEventProcessingResult.SkippedNoHandlers);
        (await host.GetRequiredService<ITenantProjectionSnapshotStore>().ApplyAsync(
            new InsufficientPermissionsRejection(colliding, "admin-1", null, "BootstrapGlobalAdmin"),
            new EventStoreDomainEventContext("system", colliding, "message-global", 3, s_at, "correlation-global") {
                Domain = "global-administrators",
                EventType = typeof(InsufficientPermissionsRejection).FullName,
                PayloadFingerprint = new string('a', 64),
            },
            TestContext.Current.CancellationToken)).ShouldBe(CheckpointedProjectionOutcome.Duplicate);

        CheckpointedProjectionRecord<TenantLocalState> after = harness.Record(colliding).ShouldNotBeNull();
        after.Version.ShouldBe(before.Version);
        after.Health.ShouldBe(CheckpointedProjectionHealth.Healthy);
        after.LastContiguousSequence.ShouldBe(1);
        after.State.ShouldNotBeNull().Name.ShouldBe("Colliding");
    }

    [Fact]
    public async Task NonCreatingEventsBeforeCreationKeepNoStateUntilTenantCreated() {
        var harness = new DurableTenantProjectionHarness();
        using ServiceProvider host = harness.CreateHost(withHistory: false);

        (await DurableTenantProjectionHarness.ProcessAsync(host, harness.Persist(new TenantNotFoundRejection(Tenant), Tenant, 1)))
            .ShouldBe(EventStoreDomainEventProcessingResult.Processed);
        (await DurableTenantProjectionHarness.ProcessAsync(host, harness.Persist(new TenantConfigurationSet(Tenant, "k", "v"), Tenant, 2)))
            .ShouldBe(EventStoreDomainEventProcessingResult.Processed);
        harness.Record(Tenant).ShouldNotBeNull().State.ShouldBeNull();
        (await DurableTenantProjectionHarness.ProcessAsync(host, harness.Persist(new TenantCreated(Tenant, "Created late", null, s_at), Tenant, 3)))
            .ShouldBe(EventStoreDomainEventProcessingResult.Processed);

        TenantProjectionSnapshot snapshot = (await host.GetRequiredService<ITenantProjectionSnapshotStore>().GetSnapshotAsync(Tenant, TestContext.Current.CancellationToken)).ShouldNotBeNull();
        snapshot.IsHealthy.ShouldBeTrue();
        snapshot.Checkpoint.ShouldBe(3);
        snapshot.State.ShouldNotBeNull().Name.ShouldBe("Created late");
        snapshot.State.Status.ShouldBe(TenantStatus.Active);
        snapshot.State.Configuration.ShouldBeEmpty("events before creation never materialize");
    }

    [Fact]
    public async Task RecoveryAcrossPreCreationRejectionsMaterializesOnlyFromTenantCreated() {
        var harness = new DurableTenantProjectionHarness();
        using ServiceProvider host = harness.CreateHost();
        _ = harness.Persist(new TenantNotFoundRejection(Tenant), Tenant, 1);
        _ = harness.Persist(new TenantCreated(Tenant, "Recovered", null, s_at), Tenant, 2);
        EventStoreDomainEventEnvelope member = harness.Persist(new UserAddedToTenant(Tenant, "user-1", TenantRole.TenantOwner), Tenant, 3);

        (await DurableTenantProjectionHarness.ProcessAsync(host, member)).ShouldBe(EventStoreDomainEventProcessingResult.Processed);

        TenantProjectionSnapshot snapshot = (await host.GetRequiredService<ITenantProjectionSnapshotStore>().GetSnapshotAsync(Tenant, TestContext.Current.CancellationToken)).ShouldNotBeNull();
        snapshot.IsHealthy.ShouldBeTrue();
        snapshot.Checkpoint.ShouldBe(3);
        snapshot.State.ShouldNotBeNull().Members["user-1"].ShouldBe(TenantRole.TenantOwner);
    }

    [Theory]
    [InlineData("tenant-a", "tenants-projection:v1:tenant-a")]
    [InlineData("Tenant_1.v2", "tenants-projection:v1:Tenant_1.v2")]
    public void KeyForKeepsSafeTenantIdentifiers(string tenantId, string expected)
        => DurableTenantProjectionStore.KeyFor(tenantId).ShouldBe(expected);

    [Theory]
    [InlineData("tenant||a")]
    [InlineData("tenant a")]
    [InlineData("tenant:a")]
    public void KeyForHashesUnsafeTenantIdentifiers(string tenantId) {
        string key = DurableTenantProjectionStore.KeyFor(tenantId);

        key.ShouldStartWith(DurableTenantProjectionStore.KeyPrefix + DurableTenantProjectionStore.HashedKeyMarker);
        key.ShouldNotContain(tenantId);
        key.Length.ShouldBe(DurableTenantProjectionStore.KeyPrefix.Length + DurableTenantProjectionStore.HashedKeyMarker.Length + 64);
    }

    [Fact]
    public void VerbatimKeysNeverCollideWithHashedKeys() {
        string unsafeTenant = "tenant||collision";
        string hashed = DurableTenantProjectionStore.KeyFor(unsafeTenant);
        string digest = hashed[(DurableTenantProjectionStore.KeyPrefix.Length + DurableTenantProjectionStore.HashedKeyMarker.Length)..];

        // A safe identifier spelling the old hashed form stays verbatim and can never produce the hashed key.
        string lookalike = DurableTenantProjectionStore.KeyFor("sha256-" + digest);
        string verbatimSegment = lookalike[DurableTenantProjectionStore.KeyPrefix.Length..];

        lookalike.ShouldNotBe(hashed);
        verbatimSegment.ShouldNotContain(':');
        hashed[DurableTenantProjectionStore.KeyPrefix.Length..].ShouldContain(':');
    }

    [Fact]
    public async Task RejectionFollowedByTheNextEventStaysHealthyWithoutHistory() {
        var harness = new DurableTenantProjectionHarness();
        using ServiceProvider host = harness.CreateHost(withHistory: false);
        EventStoreDomainEventEnvelope created = harness.Persist(new TenantCreated(Tenant, "Rejected once", null, s_at), Tenant, 1);
        EventStoreDomainEventEnvelope rejection = harness.Persist(new TenantAlreadyExistsRejection(Tenant), Tenant, 2);
        EventStoreDomainEventEnvelope member = harness.Persist(new UserAddedToTenant(Tenant, "user-1", TenantRole.TenantReader), Tenant, 3);

        (await DurableTenantProjectionHarness.ProcessAsync(host, created)).ShouldBe(EventStoreDomainEventProcessingResult.Processed);
        (await DurableTenantProjectionHarness.ProcessAsync(host, rejection)).ShouldBe(EventStoreDomainEventProcessingResult.Processed);
        CheckpointedProjectionRecord<TenantLocalState> afterRejection = harness.Record(Tenant).ShouldNotBeNull();
        (await DurableTenantProjectionHarness.ProcessAsync(host, member)).ShouldBe(EventStoreDomainEventProcessingResult.Processed);

        afterRejection.Health.ShouldBe(CheckpointedProjectionHealth.Healthy);
        afterRejection.LastContiguousSequence.ShouldBe(2);
        afterRejection.State.ShouldNotBeNull().Name.ShouldBe("Rejected once", "a rejection advances the checkpoint without changing state");
        afterRejection.State.LastEvent.ShouldNotBeNull().LastSequenceNumber.ShouldBe(2);
        CheckpointedProjectionRecord<TenantLocalState> record = harness.Record(Tenant).ShouldNotBeNull();
        record.Health.ShouldBe(CheckpointedProjectionHealth.Healthy);
        record.LastContiguousSequence.ShouldBe(3);
        record.State.ShouldNotBeNull().Members["user-1"].ShouldBe(TenantRole.TenantReader);
        harness.Gateway.Reads.ShouldBe(0, "no history was needed");
    }
}
#endif
