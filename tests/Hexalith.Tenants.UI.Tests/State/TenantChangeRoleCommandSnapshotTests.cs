using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.State.TenantCommands;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class TenantChangeRoleCommandSnapshotTests
{
    [Fact]
    public void Same_role_selection_records_already_applied_without_command_tracking()
    {
        var intent = new ChangeUserRole("tenant.alpha", "literal-user", TenantRole.TenantReader);

        TenantChangeRoleCommandSnapshot snapshot = TenantChangeRoleCommandSnapshot
            .Idle()
            .AlreadyApplied(intent, TenantRole.TenantReader, ownerCount: 1, "Already applied.");

        snapshot.State.ShouldBe(TenantCommandLifecycleState.AlreadyApplied);
        snapshot.Intent.ShouldBe(intent);
        snapshot.CurrentConfirmedRole.ShouldBe(TenantRole.TenantReader);
        snapshot.MessageId.ShouldBeNull();
        snapshot.CorrelationId.ShouldBeNull();
        snapshot.LiveRegionPoliteness.ShouldBe(TenantCommandLiveRegionPoliteness.Polite);
        snapshot.AuditState.ShouldBe(TenantCommandAuditState.MissingSupport);
    }

    [Fact]
    public void Completed_status_requires_target_user_role_projection_evidence_before_confirmation()
    {
        var intent = new ChangeUserRole("Tenant.Mixed-01", "User/CaseSensitive.01", TenantRole.TenantContributor);
        TenantChangeRoleCommandSnapshot snapshot = TenantChangeRoleCommandSnapshot
            .Idle()
            .RequestSent(intent, TenantRole.TenantReader, ownerCount: 2)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed));

        snapshot.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);

        TenantChangeRoleCommandSnapshot withoutRequestedRole = snapshot.ConfirmProjection(Detail(
            "Tenant.Mixed-01",
            [new TenantMember("User/CaseSensitive.01", TenantRole.TenantReader)]));

        withoutRequestedRole.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        withoutRequestedRole.LastConfirmedMemberProjection.ShouldBeNull();

        TenantChangeRoleCommandSnapshot confirmed = snapshot.ConfirmProjection(Detail(
            "Tenant.Mixed-01",
            [new TenantMember("User/CaseSensitive.01", TenantRole.TenantContributor)]));

        confirmed.State.ShouldBe(TenantCommandLifecycleState.Confirmed);
        confirmed.LastConfirmedMemberProjection.ShouldNotBeNull().Members
            .ShouldContain(member => member.UserId == "User/CaseSensitive.01" && member.Role == TenantRole.TenantContributor);
    }

    [Fact]
    public void Missing_target_user_after_terminal_status_is_unable_to_verify()
    {
        var intent = new ChangeUserRole("tenant.alpha", "literal-user", TenantRole.TenantContributor);
        TenantChangeRoleCommandSnapshot snapshot = TenantChangeRoleCommandSnapshot
            .Idle()
            .RequestSent(intent, TenantRole.TenantReader, ownerCount: 2)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed));

        TenantChangeRoleCommandSnapshot missingMember = snapshot.ConfirmProjection(Detail(
            "tenant.alpha",
            [new TenantMember("other-user", TenantRole.TenantContributor)]));

        missingMember.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        missingMember.LiveRegionPoliteness.ShouldBe(TenantCommandLiveRegionPoliteness.Assertive);
        missingMember.LastConfirmedMemberProjection.ShouldBeNull();
    }

    [Fact]
    public void Accepted_status_stays_distinct_when_requery_has_no_requested_role_evidence()
    {
        var intent = new ChangeUserRole("tenant.alpha", "literal-user", TenantRole.TenantContributor);
        TenantChangeRoleCommandSnapshot accepted = TenantChangeRoleCommandSnapshot
            .Idle()
            .RequestSent(intent, TenantRole.TenantReader, ownerCount: 2)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Received));

        TenantChangeRoleCommandSnapshot stillAccepted = accepted.ConfirmProjection(Detail(
            "tenant.alpha",
            [new TenantMember("literal-user", TenantRole.TenantReader)]));

        stillAccepted.State.ShouldBe(TenantCommandLifecycleState.Accepted);
        stillAccepted.LastConfirmedMemberProjection.ShouldBeNull();
    }

    [Fact]
    public void Signalr_nudge_cannot_confirm_role_or_audit_success()
    {
        var intent = new ChangeUserRole("tenant.alpha", "literal-user", TenantRole.TenantContributor);
        TenantChangeRoleCommandSnapshot snapshot = TenantChangeRoleCommandSnapshot
            .Idle()
            .RequestSent(intent, TenantRole.TenantReader, ownerCount: 2)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .SignalRNudge();

        snapshot.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        snapshot.AuditState.ShouldBe(TenantCommandAuditState.AuditPending);
        snapshot.LastConfirmedMemberProjection.ShouldBeNull();
        snapshot.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
    }

    [Fact]
    public void Completed_zero_event_status_maps_backend_noop_to_already_applied()
    {
        var intent = new ChangeUserRole("tenant.alpha", "literal-user", TenantRole.TenantContributor);

        TenantChangeRoleCommandSnapshot snapshot = TenantChangeRoleCommandSnapshot
            .Idle()
            .RequestSent(intent, TenantRole.TenantReader, ownerCount: 2)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 0));

        snapshot.State.ShouldBe(TenantCommandLifecycleState.AlreadyApplied);
        snapshot.LiveRegionPoliteness.ShouldBe(TenantCommandLiveRegionPoliteness.Polite);
        snapshot.AuditState.ShouldBe(TenantCommandAuditState.MissingSupport);
        snapshot.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
    }

    [Theory]
    [InlineData(CommandStatus.Rejected, TenantCommandLifecycleState.Rejected)]
    [InlineData(CommandStatus.PublishFailed, TenantCommandLifecycleState.Degraded)]
    [InlineData(CommandStatus.TimedOut, TenantCommandLifecycleState.UnableToVerify)]
    public void Projection_evidence_cannot_convert_terminal_non_success_states_to_confirmed(
        CommandStatus status,
        TenantCommandLifecycleState expectedState)
    {
        var intent = new ChangeUserRole("tenant.alpha", "literal-user", TenantRole.TenantContributor);
        TenantChangeRoleCommandSnapshot snapshot = TenantChangeRoleCommandSnapshot
            .Idle()
            .RequestSent(intent, TenantRole.TenantReader, ownerCount: 2)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(status, "Safe non-success message."));

        TenantChangeRoleCommandSnapshot withProjectionEvidence = snapshot.ConfirmProjection(Detail(
            "tenant.alpha",
            [new TenantMember("literal-user", TenantRole.TenantContributor)]));

        withProjectionEvidence.State.ShouldBe(expectedState);
        withProjectionEvidence.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
        withProjectionEvidence.LastConfirmedMemberProjection.ShouldBeNull();
    }

    private static TenantDetail Detail(string tenantId, IReadOnlyList<TenantMember> members)
        => new(
            tenantId,
            "Alpha",
            "Tenant alpha description",
            TenantStatus.Active,
            members,
            new Dictionary<string, string>(),
            DateTimeOffset.Parse("2026-06-01T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
}
