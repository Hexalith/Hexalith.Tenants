using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.State.TenantCommands;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class TenantAddMemberCommandSnapshotTests
{
    [Fact]
    public void Completed_status_requires_member_projection_evidence_before_confirmation()
    {
        var intent = new AddUserToTenantCommandRequest("Tenant.Mixed-01", "User/CaseSensitive.01", TenantRole.TenantContributor);
        TenantAddMemberCommandSnapshot snapshot = TenantAddMemberCommandSnapshot
            .Idle()
            .RequestSent(intent)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed));

        snapshot.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);

        TenantAddMemberCommandSnapshot withoutEvidence = snapshot.ConfirmProjection(Detail(
            "Tenant.Mixed-01",
            [new TenantMember("other-user", TenantRole.TenantContributor)]));

        withoutEvidence.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        withoutEvidence.LastConfirmedMemberProjection.ShouldBeNull();

        TenantAddMemberCommandSnapshot confirmed = snapshot.ConfirmProjection(Detail(
            "Tenant.Mixed-01",
            [new TenantMember("User/CaseSensitive.01", TenantRole.TenantContributor)]));

        confirmed.State.ShouldBe(TenantCommandLifecycleState.Confirmed);
        confirmed.LastConfirmedMemberProjection.ShouldNotBeNull().Members
            .ShouldContain(member => member.UserId == "User/CaseSensitive.01" && member.Role == TenantRole.TenantContributor);
    }

    [Fact]
    public void Accepted_status_stays_distinct_when_requery_has_no_member_evidence()
    {
        var intent = new AddUserToTenantCommandRequest("tenant.alpha", "literal-user", TenantRole.TenantReader);
        TenantAddMemberCommandSnapshot accepted = TenantAddMemberCommandSnapshot
            .Idle()
            .RequestSent(intent)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Received));

        TenantAddMemberCommandSnapshot stillAccepted = accepted.ConfirmProjection(Detail(
            "tenant.alpha",
            [new TenantMember("other-user", TenantRole.TenantReader)]));

        stillAccepted.State.ShouldBe(TenantCommandLifecycleState.Accepted);
        stillAccepted.State.ShouldNotBe(TenantCommandLifecycleState.ProjectionPending);
        stillAccepted.LastConfirmedMemberProjection.ShouldBeNull();
    }

    [Fact]
    public void Signalr_nudge_cannot_confirm_member_or_audit_success()
    {
        var intent = new AddUserToTenantCommandRequest("tenant.alpha", "literal-user", TenantRole.TenantReader);
        TenantAddMemberCommandSnapshot snapshot = TenantAddMemberCommandSnapshot
            .Idle()
            .RequestSent(intent)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .SignalRNudge();

        snapshot.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        snapshot.AuditState.ShouldBe(TenantCommandAuditState.AuditPending);
        snapshot.LastConfirmedMemberProjection.ShouldBeNull();
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
        var intent = new AddUserToTenantCommandRequest("tenant.alpha", "literal-user", TenantRole.TenantReader);
        TenantAddMemberCommandSnapshot snapshot = TenantAddMemberCommandSnapshot
            .Idle()
            .RequestSent(intent)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(status, "Safe non-success message."));

        TenantAddMemberCommandSnapshot withProjectionEvidence = snapshot.ConfirmProjection(Detail(
            "tenant.alpha",
            [new TenantMember("literal-user", TenantRole.TenantReader)]));

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
