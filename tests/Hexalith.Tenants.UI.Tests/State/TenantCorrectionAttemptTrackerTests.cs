using System.Reflection;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantCommands;

using Microsoft.Extensions.Logging;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class TenantCorrectionAttemptTrackerTests
{
    [Fact]
    public void LatePendingStatusCannotDiscardVerifiedCommandProof()
    {
        TenantCorrectionPreviewSnapshot preview = Preview();
        using TenantCorrectionAttemptTracker tracker = new();
        TenantAggregateCommandAdmissionGate gate = new();
        tracker.TryBegin(preview, gate, out TenantCorrectionAttempt? attempt).ShouldBeTrue();
        attempt.ShouldNotBeNull();
        TenantCorrectionPreviewSnapshot accepted = attempt.Snapshot.Accepted(
            TenantCommandSubmissionResult.Accepted(attempt.MessageId, "tracking-safe"));
        TenantCorrectionPreviewSnapshot pending = accepted.ApplyStatus(new(
            CommandStatus.Completed, EventCount: 1, HasVerifiedCommandIdentity: true)
        {
            CommittedEventSequence = 2,
        });
        tracker.TryUpdate(preview.TenantId, attempt.MessageId, pending).ShouldBeTrue();

        // A status read started by the previous panel returns after a remounted panel
        // has retained this command's completion proof.
        TenantCorrectionPreviewSnapshot late = accepted.ApplyStatus(new(
            CommandStatus.EventsPublished, HasVerifiedCommandIdentity: true));
        tracker.TryUpdate(preview.TenantId, attempt.MessageId, late).ShouldBeTrue();
        TenantCorrectionPreviewSnapshot retained = tracker.Find(preview.TenantId)!.Snapshot;
        retained.CommittedEventSequence.ShouldBe(2);
        gate.IsLocked(TenantCommandAggregateLock.ForTenant(preview.TenantId)).ShouldBeTrue();

        TenantCorrectionPreviewSnapshot confirmed = retained.ConfirmProjection(
            preview.Intent.CurrentProjection! with
            {
                CurrentRole = TenantRole.TenantReader,
                ProjectionVersion = "tenant-sequence:2",
            });
        confirmed.LifecycleState.ShouldBe(TenantCommandLifecycleState.Confirmed);
        tracker.TryUpdate(preview.TenantId, attempt.MessageId, confirmed).ShouldBeTrue();
        gate.IsLocked(TenantCommandAggregateLock.ForTenant(preview.TenantId)).ShouldBeFalse();
    }

    [Fact]
    public void RetainedAttemptDiagnosticsHideTrackingIdentityInLogSink()
    {
        using TenantCorrectionAttemptTracker tracker = new();
        tracker.TryBegin(Preview(), new TenantAggregateCommandAdmissionGate(),
            out TenantCorrectionAttempt? attempt).ShouldBeTrue();
        attempt.ShouldNotBeNull();

        TenantCorrectionDiagnosticLog sink = new();
        sink.LogInformation("Retained correction {Attempt}", attempt);
        sink.LogInformation("Disclosure control {MessageId} {TenantId} {TargetUserId} {AuditReference}",
            attempt.MessageId, attempt.TenantId, attempt.Snapshot.TargetUserId, attempt.Snapshot.OriginalAuditReference);
        sink.Messages.Count.ShouldBe(2);
        string diagnostics = sink.Messages[0];
        string disclosureControl = sink.Messages[1];

        diagnostics.ShouldContain(nameof(TenantCommandLifecycleState.RequestSent));
        diagnostics.ShouldNotContain(attempt.MessageId);
        diagnostics.ShouldNotContain(attempt.TenantId);
        diagnostics.ShouldNotContain(attempt.Snapshot.TargetUserId);
        diagnostics.ShouldNotContain(attempt.Snapshot.OriginalAuditReference);
        disclosureControl.ShouldContain(attempt.MessageId);
        disclosureControl.ShouldContain(attempt.TenantId);
        disclosureControl.ShouldContain(attempt.Snapshot.TargetUserId);
        disclosureControl.ShouldContain(attempt.Snapshot.OriginalAuditReference);
    }

    [Fact]
    public void Failed_dispatch_mark_rolls_back_the_attempt_and_lease()
    {
        TenantCorrectionPreviewSnapshot preview = Preview();
        TenantCorrectionAttemptTracker tracker = new();
        TenantAggregateCommandAdmissionGate gate = new();
        bool abandoned = false;
        gate.StateChanged += (_, _) =>
        {
            if (abandoned) return;
            FieldInfo field = typeof(TenantAggregateCommandAdmissionGate).GetField("_ownerByKey",
                BindingFlags.NonPublic | BindingFlags.Instance)!;
            Dictionary<string, object> owners = (Dictionary<string, object>)field.GetValue(gate)!;
            if (owners.TryGetValue(TenantCommandAggregateLock.ForTenant(preview.TenantId), out object? value)
                && value is TenantAggregateCommandLease lease && lease.CurrentOwner is { } owner)
            {
                abandoned = true;
                lease.TryAbandonBeforeDispatch(owner).ShouldBeTrue();
            }
        };

        tracker.TryBegin(preview, gate, out TenantCorrectionAttempt? attempt).ShouldBeFalse();

        attempt.ShouldBeNull();
        tracker.Find(preview.TenantId).ShouldBeNull();
        gate.IsLocked(TenantCommandAggregateLock.ForTenant(preview.TenantId)).ShouldBeFalse();
    }

    [Fact]
    public void Updates_refuse_regression_terminal_rewrite_and_lost_correlation()
    {
        TenantCorrectionPreviewSnapshot preview = Preview();
        TenantCorrectionAttemptTracker tracker = new();
        TenantAggregateCommandAdmissionGate gate = new();
        tracker.TryBegin(preview, gate, out TenantCorrectionAttempt? attempt).ShouldBeTrue();
        attempt.ShouldNotBeNull();
        TenantCorrectionPreviewSnapshot pending = attempt.Snapshot with {
            CorrelationId = "tracking-safe", LifecycleState = TenantCommandLifecycleState.ProjectionPending,
        };
        tracker.TryUpdate(preview.TenantId, attempt.MessageId, pending).ShouldBeTrue();
        tracker.TryUpdate(preview.TenantId, attempt.MessageId,
            pending with { LifecycleState = TenantCommandLifecycleState.Accepted }).ShouldBeFalse();
        tracker.TryUpdate(preview.TenantId, attempt.MessageId,
            pending with { LifecycleState = TenantCommandLifecycleState.RequestSent }).ShouldBeFalse();
        tracker.TryUpdate(preview.TenantId, attempt.MessageId,
            pending with { CorrelationId = null }).ShouldBeFalse();

        tracker.TryUpdate(preview.TenantId, attempt.MessageId,
            pending with { LifecycleState = TenantCommandLifecycleState.Rejected }).ShouldBeTrue();
        tracker.TryUpdate(preview.TenantId, attempt.MessageId,
            pending with { LifecycleState = TenantCommandLifecycleState.Failed }).ShouldBeFalse();
    }

    [Fact]
    public void Delivery_retry_requires_the_prior_operation_to_end_and_keeps_the_same_id()
    {
        TenantCorrectionPreviewSnapshot preview = Preview();
        TenantCorrectionAttemptTracker tracker = new();
        tracker.TryBegin(preview, new TenantAggregateCommandAdmissionGate(), out TenantCorrectionAttempt? attempt).ShouldBeTrue();
        attempt.ShouldNotBeNull();

        tracker.TryStartRetry(preview.TenantId, attempt.MessageId).ShouldBeFalse();
        tracker.EndDelivery(preview.TenantId, "wrong-id");
        tracker.TryStartRetry(preview.TenantId, attempt.MessageId).ShouldBeFalse();
        tracker.EndDelivery(preview.TenantId, attempt.MessageId);
        tracker.TryStartRetry(preview.TenantId, attempt.MessageId).ShouldBeTrue();
        tracker.TryStartRetry(preview.TenantId, attempt.MessageId).ShouldBeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EndingDeliveryRetainsOutcomeBeforeAnotherPanelCanRetry(bool accepted)
    {
        TenantCorrectionPreviewSnapshot preview = Preview();
        using TenantCorrectionAttemptTracker tracker = new();
        tracker.TryBegin(preview, new TenantAggregateCommandAdmissionGate(), out TenantCorrectionAttempt? attempt).ShouldBeTrue();
        attempt.ShouldNotBeNull();
        TenantCorrectionPreviewSnapshot outcome = accepted
            ? attempt.Snapshot.Accepted(TenantCommandSubmissionResult.Accepted(attempt.MessageId, "tracking-safe"))
            : attempt.Snapshot with { LifecycleState = TenantCommandLifecycleState.UnableToVerify };

        tracker.EndDelivery(preview.TenantId, attempt.MessageId, outcome);

        TenantCorrectionPreviewSnapshot retained = tracker.Find(preview.TenantId)!.Snapshot;
        retained.LifecycleState.ShouldBe(outcome.LifecycleState);
        retained.CorrelationId.ShouldBe(outcome.CorrelationId);
        tracker.TryStartRetry(preview.TenantId, attempt.MessageId).ShouldBe(!accepted);
    }

    [Theory]
    [InlineData(TenantCommandLifecycleState.Confirmed)]
    [InlineData(TenantCommandLifecycleState.Rejected)]
    [InlineData(TenantCommandLifecycleState.AlreadyApplied)]
    [InlineData(TenantCommandLifecycleState.Failed)]
    public void Every_terminal_state_releases_the_aggregate_lease(TenantCommandLifecycleState terminal)
    {
        TenantCorrectionPreviewSnapshot preview = Preview();
        TenantCorrectionAttemptTracker tracker = new();
        TenantAggregateCommandAdmissionGate gate = new();
        tracker.TryBegin(preview, gate, out TenantCorrectionAttempt? attempt).ShouldBeTrue();
        attempt.ShouldNotBeNull();

        tracker.TryUpdate(preview.TenantId, attempt.MessageId,
            attempt.Snapshot with { LifecycleState = terminal }).ShouldBeTrue();

        gate.IsLocked(TenantCommandAggregateLock.ForTenant(preview.TenantId)).ShouldBeFalse();
        tracker.Find(preview.TenantId)!.MessageId.ShouldBe(attempt.MessageId);
    }

    [Fact]
    public void Expired_uncertain_attempt_remains_viewable_until_a_new_attempt_replaces_it()
    {
        DateTimeOffset now = new(2026, 10, 3, 10, 0, 0, TimeSpan.Zero);
        TenantCorrectionAttemptTracker tracker = new(() => now);
        TenantAggregateCommandAdmissionGate gate = new();
        TenantCorrectionPreviewSnapshot preview = Preview();
        tracker.TryBegin(preview, gate, out TenantCorrectionAttempt? attempt).ShouldBeTrue();
        attempt.ShouldNotBeNull();
        now += TenantLifecycleCommandSnapshot.MaximumRetainedAttemptDuration + TimeSpan.FromSeconds(1);

        tracker.Find(preview.TenantId)!.MessageId.ShouldBe(attempt.MessageId);
        tracker.Find(preview.TenantId)!.BlocksAdmission.ShouldBeFalse();
        gate.IsLocked(TenantCommandAggregateLock.ForTenant(preview.TenantId)).ShouldBeFalse();
        tracker.TryStartRetry(preview.TenantId, attempt.MessageId).ShouldBeFalse();
        tracker.TryBegin(preview, gate, out TenantCorrectionAttempt? replacement).ShouldBeTrue();
        replacement!.MessageId.ShouldNotBe(attempt.MessageId);
        replacement.BlocksAdmission.ShouldBeTrue();
        tracker.IsExpired(preview.TenantId).ShouldBeFalse();
        tracker.TryUpdate(preview.TenantId, attempt.MessageId, attempt.Snapshot).ShouldBeFalse();
        tracker.EndDelivery(preview.TenantId, attempt.MessageId);
        tracker.TryStartRetry(preview.TenantId, replacement.MessageId).ShouldBeFalse();
        gate.IsLocked(TenantCommandAggregateLock.ForTenant(preview.TenantId)).ShouldBeTrue();
    }

    [Fact]
    public async Task Deadline_releases_a_lease_without_any_tracker_lookup_and_keeps_status_only_identity()
    {
        DateTimeOffset now = new(2026, 10, 3, 10, 0, 0, TimeSpan.Zero);
        using TenantCorrectionAttemptTracker tracker = new(() => now, TimeSpan.FromMilliseconds(10));
        TenantAggregateCommandAdmissionGate gate = new();
        TenantCorrectionPreviewSnapshot preview = Preview();
        tracker.TryBegin(preview, gate, out TenantCorrectionAttempt? attempt).ShouldBeTrue();
        attempt.ShouldNotBeNull();
        tracker.TryUpdate(preview.TenantId, attempt.MessageId,
            attempt.Snapshot.Accepted(TenantCommandSubmissionResult.Accepted(attempt.MessageId, "tracking-safe")))
            .ShouldBeTrue();
        now += TenantLifecycleCommandSnapshot.MaximumRetainedAttemptDuration;

        await Task.Run(async () => {
            while (gate.IsLocked(TenantCommandAggregateLock.ForTenant(preview.TenantId)))
                await Task.Delay(5);
        }).WaitAsync(TimeSpan.FromSeconds(2));

        TenantCorrectionAttempt retained = tracker.Find(preview.TenantId)!;
        retained.MessageId.ShouldBe(attempt.MessageId);
        retained.Snapshot.CorrelationId.ShouldBe("tracking-safe");
        retained.Snapshot.SafeMessageKey.ShouldBe("Tenants.Correction.Unavailable.AttemptExpired");
        tracker.TryUpdate(preview.TenantId, attempt.MessageId, attempt.Snapshot with {
            CorrelationId = "tracking-safe", LifecycleState = TenantCommandLifecycleState.Accepted,
        }).ShouldBeTrue();
        tracker.Find(preview.TenantId)!.Snapshot.SafeMessageKey
            .ShouldBe("Tenants.Correction.Unavailable.AttemptExpired");
        tracker.TryStartRetry(preview.TenantId, attempt.MessageId).ShouldBeFalse();
    }

    [Fact]
    public async Task Backward_clock_does_not_expire_an_attempt()
    {
        DateTimeOffset now = new(2026, 10, 3, 10, 0, 0, TimeSpan.Zero);
        using TenantCorrectionAttemptTracker tracker = new(() => now, TimeSpan.FromMilliseconds(10));
        TenantAggregateCommandAdmissionGate gate = new();
        TenantCorrectionPreviewSnapshot preview = Preview();
        tracker.TryBegin(preview, gate, out TenantCorrectionAttempt? attempt).ShouldBeTrue();
        now -= TimeSpan.FromMinutes(1);

        await Task.Delay(50);

        gate.IsLocked(TenantCommandAggregateLock.ForTenant(preview.TenantId)).ShouldBeTrue();
        tracker.IsExpired(preview.TenantId).ShouldBeFalse();
        tracker.EndDelivery(preview.TenantId, attempt!.MessageId);
        tracker.TryStartRetry(preview.TenantId, attempt.MessageId).ShouldBeTrue();
    }

    private static TenantCorrectionPreviewSnapshot Preview()
    {
        TenantCorrectionProjection projection = new("tenant.alpha", "target-user", TenantStatus.Active,
            null, false, true, false, true, ReadModelFreshnessState.Current,
            ProjectionLifecycleState.Current, QueryResponseProvenance.ProjectionBacked)
            { ProjectionVersion = "tenant-sequence:1", OwnerCount = 1 };
        TenantCorrectionStartIntent intent = new("original-reference", "tenant.alpha", "target-user",
            "UserRemovedFromTenant", "tenant-projection-current", TenantCorrectionCommandDomain.Tenants,
            TenantCorrectionCommandType.AddUserToTenant, TenantRole.TenantReader, [],
            new Dictionary<string, string>(StringComparer.Ordinal) {
                ["tenantId"] = "tenant.alpha", ["userId"] = "target-user",
                ["originalTimestamp"] = "2026-10-03T09:00:00.0000000+00:00",
                ["intendedRole"] = "TenantReader",
            }, projection);
        return TenantCorrectionPreviewSnapshot.FromIntent(intent);
    }
}
