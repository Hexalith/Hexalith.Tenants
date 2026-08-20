using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.State.TenantCommands;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class TenantAddMemberCommandSnapshotTests
{
    [Fact]
    public void Completed_status_requires_member_projection_evidence_and_version_advancement_before_confirmation()
    {
        var intent = new AddUserToTenant("Tenant.Mixed-01", "User/CaseSensitive.01", TenantRole.TenantContributor);
        TenantAddMemberCommandSnapshot snapshot = TenantAddMemberCommandSnapshot
            .Idle()
            .RequestSent(intent, baselineProjectionVersion: "v1", baselinePostconditionMet: false)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1));

        snapshot.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);

        TenantAddMemberCommandSnapshot withoutEvidence = snapshot.ConfirmProjection(Detail(
            "Tenant.Mixed-01",
            [new TenantMember("other-user", TenantRole.TenantContributor)]),
            currentProjectionVersion: "v2");

        withoutEvidence.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        withoutEvidence.LastConfirmedMemberProjection.ShouldBeNull();

        TenantAddMemberCommandSnapshot confirmed = snapshot.ConfirmProjection(Detail(
            "Tenant.Mixed-01",
            [new TenantMember("User/CaseSensitive.01", TenantRole.TenantContributor)]),
            currentProjectionVersion: "v2");

        confirmed.State.ShouldBe(TenantCommandLifecycleState.Confirmed);
        confirmed.LastConfirmedMemberProjection.ShouldNotBeNull().Members
            .ShouldContain(member => member.UserId == "User/CaseSensitive.01" && member.Role == TenantRole.TenantContributor);
    }

    [Fact]
    public void Matching_postcondition_without_projection_version_advancement_is_not_confirmed()
    {
        var intent = new AddUserToTenant("tenant.alpha", "literal-user", TenantRole.TenantReader);
        TenantAddMemberCommandSnapshot snapshot = TenantAddMemberCommandSnapshot
            .Idle()
            .RequestSent(intent, baselineProjectionVersion: "v1", baselinePostconditionMet: false)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1));

        TenantAddMemberCommandSnapshot pending = snapshot.ConfirmProjection(Detail(
            "tenant.alpha",
            [new TenantMember("literal-user", TenantRole.TenantReader)]),
            currentProjectionVersion: "v1");

        pending.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        pending.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
        pending.FocusTarget.ShouldBe(TenantCommandFocusTarget.Refresh);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Missing_or_blank_baseline_provenance_fails_closed(string? baselineProjectionVersion)
    {
        var intent = new AddUserToTenant("tenant.alpha", "literal-user", TenantRole.TenantReader);
        TenantAddMemberCommandSnapshot result = TenantAddMemberCommandSnapshot
            .Idle()
            .RequestSent(intent, baselineProjectionVersion)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1))
            .ConfirmProjection(
                Detail("tenant.alpha", [new TenantMember("literal-user", TenantRole.TenantReader)]),
                currentProjectionVersion: "v2");

        result.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        result.SafeMessageKey.ShouldBe("Tenants.AddMember.Confirm.UnableToVerify.MissingProvenance");
    }

    [Theory]
    [InlineData("v1", "v2", null)]
    [InlineData("v2", "v1", 1)]
    [InlineData("opaque-baseline", "opaque-current", 1)]
    public void Unqualified_projection_change_cannot_confirm(
        string baselineProjectionVersion,
        string currentProjectionVersion,
        int? eventCount)
    {
        var intent = new AddUserToTenant("tenant.alpha", "literal-user", TenantRole.TenantReader);
        TenantAddMemberCommandSnapshot result = TenantAddMemberCommandSnapshot
            .Idle()
            .RequestSent(intent, baselineProjectionVersion)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: eventCount))
            .ConfirmProjection(
                Detail("tenant.alpha", [new TenantMember("literal-user", TenantRole.TenantReader)]),
                currentProjectionVersion);

        result.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        result.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
    }

    [Theory]
    [InlineData(CommandStatus.Received)]
    [InlineData(CommandStatus.EventsStored)]
    [InlineData(CommandStatus.Completed)]
    [InlineData(CommandStatus.Rejected)]
    [InlineData(CommandStatus.PublishFailed)]
    [InlineData(CommandStatus.TimedOut)]
    public void Every_status_transition_clears_stale_safe_message_key(CommandStatus status)
    {
        TenantAddMemberCommandSnapshot snapshot = TenantAddMemberCommandSnapshot.Idle() with
        {
            SafeMessageKey = "Tenants.AddMember.Confirm.UnableToVerify.MissingProvenance",
        };

        TenantAddMemberCommandSnapshot result = snapshot.ApplyStatus(
            new TenantCommandStatusResult(status, EventCount: status is CommandStatus.Completed ? 1 : null));

        result.SafeMessageKey.ShouldBeNull();
    }

    [Fact]
    public void Pre_existing_matching_postcondition_cannot_confirm_add_member()
    {
        var intent = new AddUserToTenant("tenant.alpha", "literal-user", TenantRole.TenantReader);
        TenantAddMemberCommandSnapshot snapshot = TenantAddMemberCommandSnapshot
            .Idle()
            .RequestSent(intent, baselineProjectionVersion: "v1", baselinePostconditionMet: true)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1));

        TenantAddMemberCommandSnapshot result = snapshot.ConfirmProjection(Detail(
            "tenant.alpha",
            [new TenantMember("literal-user", TenantRole.TenantReader)]),
            currentProjectionVersion: "v2");

        result.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        result.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
        result.LiveRegionPoliteness.ShouldBe(TenantCommandLiveRegionPoliteness.Assertive);
        result.SafeMessageKey.ShouldBe("Tenants.AddMember.Confirm.UnableToVerify.MissingProvenance");
        result.SafeMessage.ShouldBeNull();
    }

    [Fact]
    public void Accepted_status_stays_distinct_when_requery_has_no_member_evidence()
    {
        var intent = new AddUserToTenant("tenant.alpha", "literal-user", TenantRole.TenantReader);
        TenantAddMemberCommandSnapshot accepted = TenantAddMemberCommandSnapshot
            .Idle()
            .RequestSent(intent, baselineProjectionVersion: "v1")
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Received));

        TenantAddMemberCommandSnapshot stillAccepted = accepted.ConfirmProjection(Detail(
            "tenant.alpha",
            [new TenantMember("other-user", TenantRole.TenantReader)]),
            currentProjectionVersion: "v2");

        stillAccepted.State.ShouldBe(TenantCommandLifecycleState.Accepted);
        stillAccepted.State.ShouldNotBe(TenantCommandLifecycleState.ProjectionPending);
        stillAccepted.LastConfirmedMemberProjection.ShouldBeNull();
    }

    [Fact]
    public void Signalr_nudge_cannot_confirm_member_or_audit_success()
    {
        var intent = new AddUserToTenant("tenant.alpha", "literal-user", TenantRole.TenantReader);
        TenantAddMemberCommandSnapshot snapshot = TenantAddMemberCommandSnapshot
            .Idle()
            .RequestSent(intent, baselineProjectionVersion: "v1")
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .SignalRNudge();

        snapshot.State.ShouldBe(TenantCommandLifecycleState.Accepted);
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
        var intent = new AddUserToTenant("tenant.alpha", "literal-user", TenantRole.TenantReader);
        TenantAddMemberCommandSnapshot snapshot = TenantAddMemberCommandSnapshot
            .Idle()
            .RequestSent(intent, baselineProjectionVersion: "v1")
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(status, "Safe non-success message."));

        TenantAddMemberCommandSnapshot withProjectionEvidence = snapshot.ConfirmProjection(Detail(
            "tenant.alpha",
            [new TenantMember("literal-user", TenantRole.TenantReader)]),
            currentProjectionVersion: "v2");

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
