using Hexalith.EventStore.Client.Projections.Checkpointed;
using Hexalith.EventStore.Client.Subscriptions;
using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;

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
            new CheckpointedProjectionRecord<TenantLocalState> { Health = CheckpointedProjectionHealth.Gapped, LastContiguousSequence = 2 });
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

        key.ShouldStartWith(DurableTenantProjectionStore.KeyPrefix + "sha256-");
        key.ShouldNotContain(tenantId);
        key.Length.ShouldBe(DurableTenantProjectionStore.KeyPrefix.Length + 7 + 64);
    }
}
