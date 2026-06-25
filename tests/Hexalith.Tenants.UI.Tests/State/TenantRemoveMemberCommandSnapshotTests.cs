using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.State.TenantCommands;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class TenantRemoveMemberCommandSnapshotTests
{
    [Fact]
    public void Preview_records_complete_context_without_submitting_command()
    {
        var intent = new RemoveUserFromTenant("tenant.alpha", "owner-user");
        TenantDetail detail = Detail("tenant.alpha", [new TenantMember("owner-user", TenantRole.TenantOwner)]);

        TenantRemoveMemberCommandSnapshot snapshot = TenantRemoveMemberCommandSnapshot
            .Idle()
            .Previewed(intent, TenantRole.TenantOwner, ownerCount: 1, targetGlobalAdministratorFriction: true, detail);

        snapshot.State.ShouldBe(TenantCommandLifecycleState.Previewed);
        snapshot.Intent.ShouldBe(intent);
        snapshot.CurrentConfirmedRole.ShouldBe(TenantRole.TenantOwner);
        snapshot.OwnerCount.ShouldBe(1);
        snapshot.TargetGlobalAdministratorFriction.ShouldBeTrue();
        snapshot.IsPreviewComplete.ShouldBeTrue();
        snapshot.MessageId.ShouldBeNull();
        snapshot.CorrelationId.ShouldBeNull();
        snapshot.LastConfirmedMemberProjection.ShouldBe(detail);
        snapshot.AuditState.ShouldBe(TenantCommandAuditState.MissingSupport);
    }

    [Fact]
    public void Completed_status_requires_projection_confirmed_absence_before_confirmed_state()
    {
        var intent = new RemoveUserFromTenant("Tenant.Mixed-01", "User/CaseSensitive.01");
        TenantRemoveMemberCommandSnapshot snapshot = TenantRemoveMemberCommandSnapshot
            .Idle()
            .Previewed(intent, TenantRole.TenantReader, ownerCount: 2, targetGlobalAdministratorFriction: false, Detail(
                "Tenant.Mixed-01",
                [new TenantMember("User/CaseSensitive.01", TenantRole.TenantReader)]))
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed));

        snapshot.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);

        TenantRemoveMemberCommandSnapshot stillPending = snapshot.ConfirmProjection(Detail(
            "Tenant.Mixed-01",
            [new TenantMember("User/CaseSensitive.01", TenantRole.TenantReader)]));

        stillPending.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        stillPending.LastConfirmedMemberProjection.ShouldNotBeNull().Members
            .ShouldContain(member => member.UserId == "User/CaseSensitive.01");

        TenantRemoveMemberCommandSnapshot confirmed = snapshot.ConfirmProjection(Detail(
            "Tenant.Mixed-01",
            [new TenantMember("other-user", TenantRole.TenantOwner)]));

        confirmed.State.ShouldBe(TenantCommandLifecycleState.Confirmed);
        confirmed.LastConfirmedMemberProjection.ShouldNotBeNull().Members
            .ShouldNotContain(member => member.UserId == "User/CaseSensitive.01");
        confirmed.AuditState.ShouldBe(TenantCommandAuditState.AuditPending);
    }

    [Fact]
    public void User_not_in_tenant_rejection_becomes_already_applied_only_after_absence_requery()
    {
        var intent = new RemoveUserFromTenant("tenant.alpha", "literal-user");
        TenantRemoveMemberCommandSnapshot rejected = TenantRemoveMemberCommandSnapshot
            .Idle()
            .Previewed(intent, TenantRole.TenantReader, ownerCount: 2, targetGlobalAdministratorFriction: false, Detail(
                "tenant.alpha",
                [new TenantMember("literal-user", TenantRole.TenantReader)]))
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Rejected, "Safe rejection.", "UserNotInTenant"));

        TenantRemoveMemberCommandSnapshot stillRejected = rejected.ConfirmProjection(Detail(
            "tenant.alpha",
            [new TenantMember("literal-user", TenantRole.TenantReader)]));

        stillRejected.State.ShouldBe(TenantCommandLifecycleState.Rejected);
        stillRejected.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);

        TenantRemoveMemberCommandSnapshot alreadyApplied = rejected.ConfirmProjection(Detail(
            "tenant.alpha",
            [new TenantMember("owner-user", TenantRole.TenantOwner)]));

        alreadyApplied.State.ShouldBe(TenantCommandLifecycleState.AlreadyApplied);
        alreadyApplied.SafeMessage.ShouldNotBeNull().ShouldContain("already absent");
        alreadyApplied.AuditState.ShouldBe(TenantCommandAuditState.MissingSupport);
    }

    [Fact]
    public void Signalr_nudge_cannot_confirm_absence_or_audit_available()
    {
        var intent = new RemoveUserFromTenant("tenant.alpha", "literal-user");
        TenantRemoveMemberCommandSnapshot snapshot = TenantRemoveMemberCommandSnapshot
            .Idle()
            .Previewed(intent, TenantRole.TenantReader, ownerCount: 2, targetGlobalAdministratorFriction: false, Detail(
                "tenant.alpha",
                [new TenantMember("literal-user", TenantRole.TenantReader)]))
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .SignalRNudge();

        snapshot.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        snapshot.AuditState.ShouldBe(TenantCommandAuditState.AuditPending);
        snapshot.LastConfirmedMemberProjection.ShouldNotBeNull().Members
            .ShouldContain(member => member.UserId == "literal-user");
        snapshot.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
    }

    [Fact]
    public void Duplicate_submit_is_distinct_from_failure_and_success()
    {
        TenantRemoveMemberCommandSnapshot snapshot = TenantRemoveMemberCommandSnapshot
            .Idle()
            .DuplicatePrevented("A command is already in progress.");

        snapshot.State.ShouldBe(TenantCommandLifecycleState.DuplicatePrevented);
        snapshot.LiveRegionPoliteness.ShouldBe(TenantCommandLiveRegionPoliteness.Assertive);
        snapshot.AuditState.ShouldBe(TenantCommandAuditState.MissingSupport);
        snapshot.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
        snapshot.State.ShouldNotBe(TenantCommandLifecycleState.Failed);
    }

    [Theory]
    [InlineData(CommandStatus.PublishFailed, TenantCommandLifecycleState.Degraded, TenantCommandAuditState.AuditDelayed)]
    [InlineData(CommandStatus.TimedOut, TenantCommandLifecycleState.UnableToVerify, TenantCommandAuditState.AuditDelayed)]
    [InlineData(CommandStatus.Rejected, TenantCommandLifecycleState.Rejected, TenantCommandAuditState.AuditUnavailable)]
    public void Terminal_non_success_states_do_not_collapse_to_confirmed(
        CommandStatus status,
        TenantCommandLifecycleState expectedState,
        TenantCommandAuditState expectedAuditState)
    {
        var intent = new RemoveUserFromTenant("tenant.alpha", "literal-user");
        TenantRemoveMemberCommandSnapshot snapshot = TenantRemoveMemberCommandSnapshot
            .Idle()
            .Previewed(intent, TenantRole.TenantReader, ownerCount: 2, targetGlobalAdministratorFriction: false, Detail(
                "tenant.alpha",
                [new TenantMember("literal-user", TenantRole.TenantReader)]))
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(status, "Safe status.", "SafeCode"));

        TenantRemoveMemberCommandSnapshot withAbsentProjection = snapshot.ConfirmProjection(Detail(
            "tenant.alpha",
            [new TenantMember("owner-user", TenantRole.TenantOwner)]));

        withAbsentProjection.State.ShouldBe(expectedState);
        withAbsentProjection.AuditState.ShouldBe(expectedAuditState);
        withAbsentProjection.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
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
