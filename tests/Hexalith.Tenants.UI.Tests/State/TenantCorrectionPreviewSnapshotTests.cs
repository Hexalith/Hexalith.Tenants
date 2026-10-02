using System.Globalization;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.EventStore.Client.Projections;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class TenantCorrectionPreviewSnapshotTests
{
    [Fact]
    public void Preview_snapshot_keeps_required_evidence_and_command_lifecycle_distinct()
    {
        TenantCorrectionPreviewSnapshot snapshot = TenantCorrectionPreviewSnapshot.FromIntent(
            Intent("UserRemovedFromTenant", intendedRole: TenantRole.TenantReader),
            Detail());

        snapshot.LifecycleState.ShouldBe(TenantCommandLifecycleState.Previewed);
        snapshot.AuditState.ShouldBe(TenantCommandAuditState.NotStarted);
        snapshot.OriginalAuditReference.ShouldBe("event-original");
        snapshot.TenantId.ShouldBe("tenant.alpha");
        snapshot.TargetUserId.ShouldBe("target-user");
        snapshot.IntendedRole.ShouldBe(TenantRole.TenantReader);
        snapshot.KnownConsequences.ShouldNotBeEmpty();
        snapshot.KnownUnknowns.ShouldNotBeEmpty();
        snapshot.AuditEvidenceExpectation.ShouldNotBeNullOrWhiteSpace();
        snapshot.RecoveryPath.ShouldNotBeNullOrWhiteSpace();
        snapshot.LastConfirmedProjectionEvidence.ShouldNotBeNull();
        snapshot.ProofLink.ShouldBeNull();
    }

    [Fact]
    public void Restore_preview_blocks_already_applied_projection_without_success_or_audit_proof()
    {
        TenantCorrectionPreviewSnapshot snapshot = TenantCorrectionPreviewSnapshot.FromIntent(
            Intent("UserRemovedFromTenant", intendedRole: TenantRole.TenantReader),
            Detail(new TenantMember("target-user", TenantRole.TenantReader)));

        snapshot.CanSubmit.ShouldBeFalse();
        snapshot.LifecycleState.ShouldBe(TenantCommandLifecycleState.AlreadyApplied);
        snapshot.AuditState.ShouldBe(TenantCommandAuditState.MissingSupport);
        snapshot.ProofLink.ShouldBeNull();
        snapshot.SafeMessage.ShouldBeNull();
        snapshot.SafeMessageKey.ShouldBe("Tenants.Correction.Unavailable.AlreadyApplied");
    }

    [Fact]
    public void Restore_preview_blocks_different_current_role_and_requires_change_role_path()
    {
        TenantCorrectionPreviewSnapshot snapshot = TenantCorrectionPreviewSnapshot.FromIntent(
            Intent("UserRemovedFromTenant", intendedRole: TenantRole.TenantReader),
            Detail(new TenantMember("target-user", TenantRole.TenantContributor)));

        snapshot.CanSubmit.ShouldBeFalse();
        snapshot.LifecycleState.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        snapshot.AuditState.ShouldBe(TenantCommandAuditState.MissingSupport);
        snapshot.SafeMessage.ShouldBeNull();
        snapshot.SafeMessageKey.ShouldBe("Tenants.Correction.Unavailable.CurrentRoleConflict");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-an-instant")]
    [InlineData("2026-06-01")]
    public void Original_evidence_time_must_be_a_complete_round_trip_instant(string? originalTimestamp)
    {
        TenantCorrectionStartIntent intent = Intent("UserRemovedFromTenant", intendedRole: TenantRole.TenantReader);
        Dictionary<string, string> inputs = new(intent.RequiredPreviewInputs, StringComparer.Ordinal);
        if (originalTimestamp is null) inputs.Remove("originalTimestamp");
        else inputs["originalTimestamp"] = originalTimestamp;

        TenantCorrectionPreviewSnapshot snapshot = TenantCorrectionPreviewSnapshot.FromIntent(
            intent with { RequiredPreviewInputs = inputs });

        snapshot.OriginalTimestampUtc.ShouldBeNull();
        snapshot.CanSubmit.ShouldBeFalse();
        snapshot.LifecycleState.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        snapshot.SafeMessageKey.ShouldBe("Tenants.Correction.Unavailable.OriginalTimeUnavailable");
    }

    [Theory]
    [InlineData(CommandStatus.Received, TenantCommandLifecycleState.Accepted, TenantCommandAuditState.MissingSupport)]
    [InlineData(CommandStatus.Completed, TenantCommandLifecycleState.ProjectionPending, TenantCommandAuditState.MissingSupport)]
    [InlineData(CommandStatus.Rejected, TenantCommandLifecycleState.Rejected, TenantCommandAuditState.MissingSupport)]
    [InlineData(CommandStatus.PublishFailed, TenantCommandLifecycleState.Degraded, TenantCommandAuditState.MissingSupport)]
    [InlineData(CommandStatus.TimedOut, TenantCommandLifecycleState.UnableToVerify, TenantCommandAuditState.MissingSupport)]
    public void Command_status_maps_to_distinct_correction_lifecycle_and_audit_states(
        CommandStatus commandStatus,
        TenantCommandLifecycleState lifecycleState,
        TenantCommandAuditState auditState)
    {
        TenantCorrectionPreviewSnapshot snapshot = TenantCorrectionPreviewSnapshot
            .FromIntent(Intent("UserRoleChanged", currentRole: TenantRole.TenantContributor, intendedRole: TenantRole.TenantReader), Detail())
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-safe", "tracking-safe"))
            .ApplyStatus(new TenantCommandStatusResult(commandStatus, "safe status"));

        snapshot.LifecycleState.ShouldBe(lifecycleState);
        snapshot.AuditState.ShouldBe(auditState);
    }

    [Fact]
    public void Advanced_status_and_confirmed_projection_clear_an_obsolete_unable_to_verify_message()
    {
        TenantCorrectionStartIntent intent = Intent("UserRemovedFromTenant", intendedRole: TenantRole.TenantReader);
        TenantCorrectionPreviewSnapshot uncertain = TenantCorrectionPreviewSnapshot.FromIntent(intent)
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-safe", "tracking-safe"))
            with { LifecycleState = TenantCommandLifecycleState.UnableToVerify,
                SafeMessageKey = "Tenants.Correction.State.UnableToVerify" };

        TenantCorrectionPreviewSnapshot stored = uncertain.ApplyStatus(new TenantCommandStatusResult(
            CommandStatus.EventsStored, EventCount: 1, HasVerifiedCommandIdentity: true));
        TenantCorrectionPreviewSnapshot confirmed = stored.ConfirmProjection(intent.CurrentProjection! with {
            CurrentRole = TenantRole.TenantReader,
            ProjectionVersion = "tenant-sequence:2",
        });

        stored.LifecycleState.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        stored.SafeMessageKey.ShouldBeNull();
        confirmed.LifecycleState.ShouldBe(TenantCommandLifecycleState.Confirmed);
        confirmed.SafeMessageKey.ShouldBeNull();
    }

    [Fact]
    public void Submission_failure_targets_correction_lifecycle_for_terminal_focus()
    {
        TenantCorrectionPreviewSnapshot failed = TenantCorrectionPreviewSnapshot
            .FromIntent(Intent("UserRemovedFromTenant", intendedRole: TenantRole.TenantReader), Detail())
            .RequestSent()
            .ApplySubmissionFailure(TenantCommandSubmissionResult.Failed("safe failure"));

        failed.LifecycleState.ShouldBe(TenantCommandLifecycleState.Failed);
        failed.FocusTarget.ShouldBe(TenantCommandFocusTarget.Lifecycle);
        failed.LiveRegionPoliteness.ShouldBe(TenantCommandLiveRegionPoliteness.Assertive);
    }

    [Fact]
    public void Projection_confirmation_requires_causal_version_and_refuses_unlinked_audit_row()
    {
        TenantCorrectionPreviewSnapshot pending = TenantCorrectionPreviewSnapshot
            .FromIntent(Intent("UserRoleChanged", currentRole: TenantRole.TenantContributor, intendedRole: TenantRole.TenantReader), Detail())
            .RequestSent() with { AttemptStartedAtUtc = DateTimeOffset.Parse("2026-06-01T10:01:00Z", CultureInfo.InvariantCulture) };
        pending = pending
            .Accepted(TenantCommandSubmissionResult.Accepted("message-safe", "tracking-safe"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed));

        TenantCorrectionPreviewSnapshot notConfirmed = pending.ConfirmProjection(
            pending.Intent.CurrentProjection! with { CurrentRole = TenantRole.TenantContributor,
                ProjectionVersion = "tenant-sequence:2" });
        notConfirmed.LifecycleState.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        notConfirmed.ProofLink.ShouldBeNull();

        TenantCorrectionPreviewSnapshot withoutAdvance = pending.ConfirmProjection(
            pending.Intent.CurrentProjection! with { CurrentRole = TenantRole.TenantReader });
        withoutAdvance.LifecycleState.ShouldBe(TenantCommandLifecycleState.UnableToVerify);

        TenantCorrectionProjection confirmedCapture = pending.Intent.CurrentProjection! with {
            CurrentRole = TenantRole.TenantReader,
            ProjectionVersion = "tenant-sequence:2",
        };
        TenantCorrectionPreviewSnapshot confirmed = pending
            .ConfirmProjection(confirmedCapture)
            .WithCorrectiveProof(Row("event-corrective", "UserRoleChanged"));

        // Matching target, event type and timestamp after dispatch still cannot identify this attempt.
        confirmed.AttemptStartedAtUtc.ShouldNotBeNull();
        confirmed.AttemptStartedAtUtc.Value.ShouldBeLessThan(Row("event-corrective", "UserRoleChanged").Timestamp);
        confirmed.LifecycleState.ShouldBe(TenantCommandLifecycleState.Confirmed);
        notConfirmed.LastConfirmedCorrectionProjection.ShouldBeNull();
        withoutAdvance.LastConfirmedCorrectionProjection.ShouldBeNull();
        confirmed.LastConfirmedCorrectionProjection.ShouldBe(confirmedCapture);
        confirmed.LastConfirmedCorrectionProjection!.ProjectionVersion.ShouldBe("tenant-sequence:2");
        confirmed.FocusTarget.ShouldBe(TenantCommandFocusTarget.Lifecycle);
        confirmed.AuditState.ShouldBe(TenantCommandAuditState.MissingSupport);
        confirmed.ProofLink.ShouldBeNull();
    }

    [Fact]
    public void Retained_attempt_uses_one_ULID_and_aggregate_lease_until_terminal_evidence()
    {
        TenantCorrectionPreviewSnapshot preview = TenantCorrectionPreviewSnapshot.FromIntent(
            Intent("UserRemovedFromTenant", intendedRole: TenantRole.TenantReader));
        preview.CanSubmit.ShouldBeTrue();
        var gate = new TenantAggregateCommandAdmissionGate();
        var tracker = new TenantCorrectionAttemptTracker();

        tracker.TryBegin(preview, gate, out TenantCorrectionAttempt? attempt).ShouldBeTrue();
        attempt.ShouldNotBeNull();
        NUlid.Ulid.TryParse(attempt.MessageId, out _).ShouldBeTrue();
        attempt.Snapshot.AttemptStartedAtUtc.ShouldNotBeNull();
        gate.TryAcquireLease(TenantCommandAggregateLock.ForTenant(preview.TenantId), new object(), out _)
            .ShouldBeFalse();
        tracker.TryBegin(preview, gate, out _).ShouldBeFalse();
        tracker.TryStartRetry(preview.TenantId, attempt.MessageId).ShouldBeFalse();

        tracker.EndDelivery(preview.TenantId, attempt.MessageId);
        tracker.TryStartRetry(preview.TenantId, attempt.MessageId).ShouldBeTrue();
        tracker.TryStartRetry(preview.TenantId, attempt.MessageId).ShouldBeFalse();
        tracker.EndDelivery(preview.TenantId, attempt.MessageId);

        TenantCorrectionPreviewSnapshot confirmed = attempt.Snapshot
            .Accepted(TenantCommandSubmissionResult.Accepted(attempt.MessageId, "tracking-safe"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1,
                HasVerifiedCommandIdentity: true))
            .ConfirmProjection(preview.Intent.CurrentProjection! with {
                CurrentRole = TenantRole.TenantReader,
                ProjectionVersion = "tenant-sequence:2",
            });
        tracker.TryUpdate(preview.TenantId, attempt.MessageId, confirmed).ShouldBeTrue();
        tracker.Find(preview.TenantId)!.Snapshot.LifecycleState.ShouldBe(TenantCommandLifecycleState.Confirmed);
        object nextOwner = new();
        gate.TryAcquireLease(TenantCommandAggregateLock.ForTenant(preview.TenantId), nextOwner, out TenantAggregateCommandLease? nextLease)
            .ShouldBeTrue();
        nextLease!.TryAbandonBeforeDispatch(nextOwner).ShouldBeTrue();
        tracker.TryBegin(preview, gate, out TenantCorrectionAttempt? nextAttempt).ShouldBeTrue();
        nextAttempt!.MessageId.ShouldNotBe(attempt.MessageId);
        tracker.TryUpdate(preview.TenantId, attempt.MessageId, confirmed).ShouldBeFalse();
    }

    [Fact]
    public void Unversioned_or_role_only_evidence_never_confirms_a_correction()
    {
        TenantCorrectionStartIntent intent = Intent("UserRemovedFromTenant", intendedRole: TenantRole.TenantReader);
        TenantCorrectionPreviewSnapshot preview = TenantCorrectionPreviewSnapshot.FromIntent(
            intent with { CurrentProjection = intent.CurrentProjection! with { ProjectionVersion = null } });
        preview.CanSubmit.ShouldBeFalse();

        TenantCorrectionPreviewSnapshot pending = TenantCorrectionPreviewSnapshot.FromIntent(intent)
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-safe", "tracking-safe"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1,
                HasVerifiedCommandIdentity: true));
        pending.ConfirmProjection(Detail(new TenantMember("target-user", TenantRole.TenantReader)))
            .LifecycleState.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        pending.ConfirmProjection(intent.CurrentProjection! with { CurrentRole = TenantRole.TenantReader })
            .LifecycleState.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
    }

    private static TenantCorrectionStartIntent Intent(
        string eventType,
        TenantRole? currentRole = null,
        TenantRole? intendedRole = null)
        => TenantCorrectionStartIntent.Evaluate(new(
            TenantAuditReceipt.FromRow(Row("event-original", eventType)),
            Row("event-original", eventType),
            IsAuthorized: true,
            HasCurrentProjectionSnapshot: true,
            CurrentProjectionSnapshotReference: "Current tenant projection is available.",
            TenantStatus: TenantStatus.Active,
            CurrentRole: currentRole,
            IntendedRole: intendedRole,
            HasTenantCommandSupport: true,
            Projection: new TenantCorrectionProjection("tenant.alpha", "target-user",
                TenantStatus.Active, currentRole, false, true, false, true, ReadModelFreshnessState.Current,
                ProjectionLifecycleState.Current, QueryResponseProvenance.ProjectionBacked)
                { ProjectionVersion = "tenant-sequence:1" }));

    private static TenantDetail Detail(params TenantMember[] members)
        => new(
            "tenant.alpha",
            "Tenant Alpha",
            null,
            TenantStatus.Active,
            members.Length == 0 ? [] : members,
            new Dictionary<string, string>(StringComparer.Ordinal),
            DateTimeOffset.Parse("2026-06-01T09:00:00Z", CultureInfo.InvariantCulture));

    private static TenantAuditRow Row(string eventReference, string eventType)
        => new(
            eventReference,
            eventType,
            AuditEventCategory.Access,
            "actor-user",
            eventReference == "event-corrective"
                ? DateTimeOffset.Parse("2026-06-01T10:05:00Z", CultureInfo.InvariantCulture)
                : DateTimeOffset.Parse("2026-06-01T10:00:00Z", CultureInfo.InvariantCulture),
            "tenant.alpha",
            "target-user",
            "tenant.alpha",
            eventType,
            eventType is "UserRoleChanged"
                ? "userId: target-user; oldRole: TenantContributor; newRole: TenantReader"
                : "userId: target-user; previousRole: TenantReader",
            ReadModelFreshnessState.Current,
            ProjectionLifecycleState.Current,
            QueryResponseProvenance.ProjectionBacked,
            new TenantAuditNarrative(UserId: "target-user"));
}
