using System.Globalization;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.Tenants.UI.State.TruthState;

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
    [InlineData(CommandStatus.Received, TenantCommandLifecycleState.Accepted, TenantCommandAuditState.AuditPending)]
    [InlineData(CommandStatus.Completed, TenantCommandLifecycleState.ProjectionPending, TenantCommandAuditState.AuditPending)]
    [InlineData(CommandStatus.Rejected, TenantCommandLifecycleState.Rejected, TenantCommandAuditState.AuditUnavailable)]
    [InlineData(CommandStatus.PublishFailed, TenantCommandLifecycleState.Degraded, TenantCommandAuditState.AuditUnavailable)]
    [InlineData(CommandStatus.TimedOut, TenantCommandLifecycleState.UnableToVerify, TenantCommandAuditState.AuditUnavailable)]
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
    public void Projection_confirmation_requires_requeried_projection_before_corrective_proof_link()
    {
        TenantCorrectionPreviewSnapshot pending = TenantCorrectionPreviewSnapshot
            .FromIntent(Intent("UserRoleChanged", currentRole: TenantRole.TenantContributor, intendedRole: TenantRole.TenantReader), Detail())
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-safe", "tracking-safe"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed));

        TenantCorrectionPreviewSnapshot notConfirmed = pending.ConfirmProjection(Detail(new TenantMember("target-user", TenantRole.TenantContributor)));
        notConfirmed.LifecycleState.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        notConfirmed.ProofLink.ShouldBeNull();

        TenantCorrectionPreviewSnapshot confirmed = pending
            .ConfirmProjection(Detail(new TenantMember("target-user", TenantRole.TenantReader)))
            .WithCorrectiveProof(Row("event-corrective", "UserRoleChanged"));

        confirmed.LifecycleState.ShouldBe(TenantCommandLifecycleState.Confirmed);
        confirmed.AuditState.ShouldBe(TenantCommandAuditState.NotStarted);
        confirmed.ProofLink.ShouldNotBeNull();
        confirmed.ProofLink.OriginalAuditReference.ShouldBe("event-original");
        confirmed.ProofLink.CorrectiveAuditReference.ShouldBe("event-corrective");
        confirmed.ProofLink.CorrectiveTimestamp.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture)
            .ShouldBe("2026-06-01 10:05:00 UTC");
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
            HasTenantCommandSupport: true));

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
            TenantFreshnessState.Current);
}
