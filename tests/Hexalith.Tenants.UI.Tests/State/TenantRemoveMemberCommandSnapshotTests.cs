using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.State.TenantAudit;
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
    public void Completed_status_requires_absence_and_version_advancement_before_confirmed_state()
    {
        var intent = new RemoveUserFromTenant("Tenant.Mixed-01", "User/CaseSensitive.01");
        TenantRemoveMemberCommandSnapshot snapshot = TenantRemoveMemberCommandSnapshot
            .Idle()
            .Previewed(intent, TenantRole.TenantReader, ownerCount: 2, targetGlobalAdministratorFriction: false, Detail(
                "Tenant.Mixed-01",
                [new TenantMember("User/CaseSensitive.01", TenantRole.TenantReader)]))
            .RequestSent(baselineProjectionVersion: "v1")
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed));

        snapshot.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);

        TenantRemoveMemberCommandSnapshot stillPending = snapshot.ConfirmProjection(Detail(
            "Tenant.Mixed-01",
            [new TenantMember("User/CaseSensitive.01", TenantRole.TenantReader)]));

        stillPending.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        stillPending.LastConfirmedMemberProjection.ShouldNotBeNull().Members
            .ShouldContain(member => member.UserId == "User/CaseSensitive.01");

        TenantRemoveMemberCommandSnapshot withoutAdvancement = snapshot.ConfirmProjection(Detail(
            "Tenant.Mixed-01",
            [new TenantMember("other-user", TenantRole.TenantOwner)]),
            currentProjectionVersion: "v1");

        withoutAdvancement.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        withoutAdvancement.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);

        TenantRemoveMemberCommandSnapshot confirmed = snapshot.ConfirmProjection(Detail(
            "Tenant.Mixed-01",
            [new TenantMember("other-user", TenantRole.TenantOwner)]),
            currentProjectionVersion: "v2");

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
            .RequestSent(baselineProjectionVersion: "v1")
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
        alreadyApplied.SafeMessageKey.ShouldBe("Tenants.RemoveMember.Confirm.AlreadyApplied.RejectedAbsence");
        alreadyApplied.SafeMessage.ShouldBeNull();
        alreadyApplied.AuditState.ShouldBe(TenantCommandAuditState.MissingSupport);
    }


    [Fact]
    public void Pre_existing_absence_maps_to_already_applied_not_confirmed()
    {
        var intent = new RemoveUserFromTenant("tenant.alpha", "literal-user");
        TenantRemoveMemberCommandSnapshot snapshot = TenantRemoveMemberCommandSnapshot
            .Idle()
            .Previewed(intent, TenantRole.TenantReader, ownerCount: 2, targetGlobalAdministratorFriction: false, Detail(
                "tenant.alpha",
                [new TenantMember("owner-user", TenantRole.TenantOwner)]))
            .RequestSent(baselineProjectionVersion: "v1", baselinePostconditionMet: true)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed));

        TenantRemoveMemberCommandSnapshot result = snapshot.ConfirmProjection(Detail(
            "tenant.alpha",
            [new TenantMember("owner-user", TenantRole.TenantOwner)]),
            currentProjectionVersion: "v2");

        result.State.ShouldBe(TenantCommandLifecycleState.AlreadyApplied);
        result.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
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
            .RequestSent(baselineProjectionVersion: "v1")
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
            .RequestSent(baselineProjectionVersion: "v1")
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(status, "Safe status.", "SafeCode"));

        TenantRemoveMemberCommandSnapshot withAbsentProjection = snapshot.ConfirmProjection(Detail(
            "tenant.alpha",
            [new TenantMember("owner-user", TenantRole.TenantOwner)]));

        withAbsentProjection.State.ShouldBe(expectedState);
        withAbsentProjection.AuditState.ShouldBe(expectedAuditState);
        withAbsentProjection.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
    }

    [Fact]
    public void Missing_baseline_maps_to_unable_to_verify_without_audit_available()
    {
        var intent = new RemoveUserFromTenant("tenant.alpha", "literal-user");
        TenantRemoveMemberCommandSnapshot snapshot = TenantRemoveMemberCommandSnapshot
            .Idle()
            .Previewed(intent, TenantRole.TenantReader, ownerCount: 2, targetGlobalAdministratorFriction: false, Detail(
                "tenant.alpha",
                [new TenantMember("literal-user", TenantRole.TenantReader)]))
            .RequestSent(baselineProjectionVersion: null)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed));

        TenantRemoveMemberCommandSnapshot result = snapshot.ConfirmProjection(Detail(
            "tenant.alpha",
            [new TenantMember("owner-user", TenantRole.TenantOwner)]),
            currentProjectionVersion: "v2");

        result.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        result.SafeMessageKey.ShouldBe("Tenants.RemoveMember.Confirm.UnableToVerify.MissingBaseline");
        result.AuditState.ShouldBe(TenantCommandAuditState.AuditUnavailable);
        result.AuditState.ShouldNotBe(TenantCommandAuditState.AuditAvailable);
    }

    [Fact]
    public void Audit_provenance_can_confirm_when_version_has_not_advanced()
    {
        DateTimeOffset started = DateTimeOffset.Parse("2026-08-08T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
        var intent = new RemoveUserFromTenant("tenant.alpha", "literal-user");
        TenantRemoveMemberCommandSnapshot snapshot = TenantRemoveMemberCommandSnapshot
            .Idle()
            .Previewed(intent, TenantRole.TenantReader, ownerCount: 2, targetGlobalAdministratorFriction: false, Detail(
                "tenant.alpha",
                [new TenantMember("literal-user", TenantRole.TenantReader)]))
            .RequestSent(baselineProjectionVersion: "v1", attemptStartedAtUtc: started)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed));

        TenantRemoveMemberCommandSnapshot withoutProvenance = snapshot.ConfirmProjection(Detail(
            "tenant.alpha",
            [new TenantMember("owner-user", TenantRole.TenantOwner)]),
            currentProjectionVersion: "v1");

        withoutProvenance.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);

        TenantRemoveMemberCommandSnapshot confirmed = snapshot.ConfirmProjection(Detail(
            "tenant.alpha",
            [new TenantMember("owner-user", TenantRole.TenantOwner)]),
            currentProjectionVersion: "v1",
            hasQualifyingAuditProvenance: true);

        confirmed.State.ShouldBe(TenantCommandLifecycleState.Confirmed);
        confirmed.AuditState.ShouldBe(TenantCommandAuditState.AuditPending);
        confirmed.AuditState.ShouldNotBe(TenantCommandAuditState.AuditAvailable);
    }

    [Fact]
    public void Matching_removal_proof_promotes_confirmed_to_audit_available()
    {
        DateTimeOffset started = DateTimeOffset.Parse("2026-08-08T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
        var intent = new RemoveUserFromTenant("tenant.alpha", "literal-user");
        TenantRemoveMemberCommandSnapshot confirmed = TenantRemoveMemberCommandSnapshot
            .Idle()
            .Previewed(intent, TenantRole.TenantReader, ownerCount: 2, targetGlobalAdministratorFriction: false, Detail(
                "tenant.alpha",
                [new TenantMember("literal-user", TenantRole.TenantReader)]))
            .RequestSent(baselineProjectionVersion: "v1", attemptStartedAtUtc: started)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed))
            .ConfirmProjection(Detail(
                "tenant.alpha",
                [new TenantMember("owner-user", TenantRole.TenantOwner)]),
                currentProjectionVersion: "v2");

        confirmed.State.ShouldBe(TenantCommandLifecycleState.Confirmed);
        confirmed.AuditState.ShouldBe(TenantCommandAuditState.AuditPending);

        TenantRemoveMemberCommandSnapshot available = confirmed.ApplyRemovalProofMatch(matched: true);
        available.State.ShouldBe(TenantCommandLifecycleState.Confirmed);
        available.AuditState.ShouldBe(TenantCommandAuditState.AuditAvailable);

        TenantRemoveMemberCommandSnapshot unmatched = confirmed.ApplyRemovalProofMatch(matched: false);
        unmatched.State.ShouldBe(TenantCommandLifecycleState.Confirmed);
        unmatched.AuditState.ShouldBe(TenantCommandAuditState.AuditPending);
    }

    [Fact]
    public void Audit_query_failure_keeps_confirmed_and_never_invents_available()
    {
        TenantRemoveMemberCommandSnapshot confirmed = TenantRemoveMemberCommandSnapshot
            .Idle()
            .Previewed(new RemoveUserFromTenant("tenant.alpha", "literal-user"), TenantRole.TenantReader, 2, false, Detail(
                "tenant.alpha",
                [new TenantMember("literal-user", TenantRole.TenantReader)]))
            .RequestSent(baselineProjectionVersion: "v1")
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed))
            .ConfirmProjection(Detail(
                "tenant.alpha",
                [new TenantMember("owner-user", TenantRole.TenantOwner)]),
                currentProjectionVersion: "v2");

        TenantRemoveMemberCommandSnapshot delayed = confirmed.ApplyRemovalProofQueryFailure(TenantCommandAuditState.AuditDelayed);
        delayed.State.ShouldBe(TenantCommandLifecycleState.Confirmed);
        delayed.AuditState.ShouldBe(TenantCommandAuditState.AuditDelayed);

        TenantRemoveMemberCommandSnapshot available = confirmed.ApplyRemovalProofMatch(matched: true);
        TenantRemoveMemberCommandSnapshot stillAvailable = available.ApplyRemovalProofQueryFailure(TenantCommandAuditState.AuditUnavailable);
        stillAvailable.State.ShouldBe(TenantCommandLifecycleState.Confirmed);
        stillAvailable.AuditState.ShouldBe(TenantCommandAuditState.AuditAvailable);
    }

    [Fact]
    public void FindMatchingRemovalProof_requires_event_type_tenant_target_and_causal_bound()
    {
        DateTimeOffset started = DateTimeOffset.Parse("2026-08-08T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
        TenantAuditRow older = RemovalRow("tenant.alpha", "literal-user", started.AddMinutes(-1));
        TenantAuditRow equalBound = RemovalRow("tenant.alpha", "literal-user", started);
        TenantAuditRow match = RemovalRow("tenant.alpha", "literal-user", started.AddMinutes(1));
        TenantAuditRow wrongTarget = RemovalRow("tenant.alpha", "other-user", started.AddMinutes(1));

        TenantRemoveMemberCommandSnapshot.FindMatchingRemovalProof(
            [older, wrongTarget],
            "tenant.alpha",
            "literal-user",
            started).ShouldBeNull();

        TenantRemoveMemberCommandSnapshot.FindMatchingRemovalProof(
            [older, wrongTarget, equalBound],
            "tenant.alpha",
            "literal-user",
            started).ShouldBe(equalBound);

        TenantRemoveMemberCommandSnapshot.FindMatchingRemovalProof(
            [older, wrongTarget, match],
            "tenant.alpha",
            "literal-user",
            started).ShouldBe(match);
    }

    private static TenantAuditRow RemovalRow(string tenantId, string target, DateTimeOffset timestamp)
        => new(
            EventReference: $"evt-{target}-{timestamp.UtcTicks}",
            EventType: "UserRemovedFromTenant",
            Category: AuditEventCategory.Access,
            ActorId: "actor-1",
            Timestamp: timestamp,
            TenantId: tenantId,
            Target: target,
            Scope: tenantId,
            Outcome: "removed",
            ReferenceContext: $"userId: {target}",
            Freshness: Hexalith.EventStore.Client.Projections.ReadModelFreshnessState.Current);

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
