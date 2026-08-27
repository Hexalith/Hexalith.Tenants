using System.Globalization;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.UI.State.GlobalAdministrators;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantCommands;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class GlobalAdministratorCorrectionSnapshotTests
{
    [Fact]
    public void Restore_with_absent_target_is_previewable_and_submittable()
    {
        GlobalAdministratorCorrectionSnapshot snapshot = GlobalAdministratorCorrectionSnapshot.FromIntent(
            RestoreIntent(),
            ProjectionReady("other-admin"));

        snapshot.IsRestoreAccessAction.ShouldBeTrue();
        snapshot.CommandType.ShouldBe(TenantCorrectionCommandType.SetGlobalAdministrator);
        snapshot.TargetUserId.ShouldBe("admin-user");
        snapshot.LifecycleState.ShouldBe(TenantCommandLifecycleState.Previewed);
        snapshot.CurrentAdministratorCount.ShouldBe(1);
        snapshot.TargetCurrentlyPresent.ShouldBeFalse();
        snapshot.CanSubmit.ShouldBeTrue();
    }

    [Fact]
    public void Restore_with_present_target_is_already_applied_and_not_submittable()
    {
        GlobalAdministratorCorrectionSnapshot snapshot = GlobalAdministratorCorrectionSnapshot.FromIntent(
            RestoreIntent(),
            ProjectionReady("admin-user", "other-admin"));

        snapshot.LifecycleState.ShouldBe(TenantCommandLifecycleState.AlreadyApplied);
        snapshot.SafeMessageKey.ShouldBe("Tenants.Correction.GlobalAdmin.AlreadyGranted");
        snapshot.CanSubmit.ShouldBeFalse();
    }

    [Fact]
    public void Revoke_with_present_target_and_multiple_admins_is_previewable()
    {
        GlobalAdministratorCorrectionSnapshot snapshot = GlobalAdministratorCorrectionSnapshot.FromIntent(
            RevokeIntent(),
            ProjectionReady("admin-user", "other-admin"));

        snapshot.IsRestoreAccessAction.ShouldBeFalse();
        snapshot.CommandType.ShouldBe(TenantCorrectionCommandType.RemoveGlobalAdministrator);
        snapshot.LifecycleState.ShouldBe(TenantCommandLifecycleState.Previewed);
        snapshot.CurrentAdministratorCount.ShouldBe(2);
        snapshot.CanSubmit.ShouldBeTrue();
    }

    [Fact]
    public void Revoke_of_last_global_administrator_is_hard_blocked_before_submit()
    {
        GlobalAdministratorCorrectionSnapshot snapshot = GlobalAdministratorCorrectionSnapshot.FromIntent(
            RevokeIntent(),
            ProjectionReady("admin-user"));

        snapshot.LifecycleState.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        snapshot.SafeMessageKey.ShouldBe("Tenants.Correction.GlobalAdmin.LastAdministrator");
        snapshot.LiveRegionPoliteness.ShouldBe(TenantCommandLiveRegionPoliteness.Assertive);
        snapshot.CanSubmit.ShouldBeFalse();
    }

    [Fact]
    public void Revoke_with_absent_target_is_already_applied()
    {
        GlobalAdministratorCorrectionSnapshot snapshot = GlobalAdministratorCorrectionSnapshot.FromIntent(
            RevokeIntent(),
            ProjectionReady("other-admin"));

        snapshot.LifecycleState.ShouldBe(TenantCommandLifecycleState.AlreadyApplied);
        snapshot.SafeMessageKey.ShouldBe("Tenants.Correction.GlobalAdmin.AlreadyRemoved");
        snapshot.CanSubmit.ShouldBeFalse();
    }

    [Fact]
    public void Revoke_with_absent_target_on_incomplete_page_fails_closed_instead_of_already_removed()
    {
        // The fixed projection is cursor-paged (PageSize=20). A target that is not on the loaded page
        // may still exist on a later page, so "absent from this page" must never collapse to
        // "already removed"; the correction fails closed until the full projection can be verified.
        GlobalAdministratorCorrectionSnapshot snapshot = GlobalAdministratorCorrectionSnapshot.FromIntent(
            RevokeIntent(),
            PagedProjectionReady("other-admin", "second-admin"));

        snapshot.LifecycleState.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        snapshot.SafeMessageKey.ShouldBe("Tenants.Correction.Unavailable.CurrentProjectionUnavailable");
        snapshot.CanSubmit.ShouldBeFalse();
    }

    [Fact]
    public void Restore_with_absent_target_on_incomplete_page_fails_closed()
    {
        // A restore arms a grant when the target is absent; but on a paged read the target may already
        // be present on a later page, so the restore must fail closed rather than submit a grant against
        // unverified authority state.
        GlobalAdministratorCorrectionSnapshot snapshot = GlobalAdministratorCorrectionSnapshot.FromIntent(
            RestoreIntent(),
            PagedProjectionReady("other-admin", "second-admin"));

        snapshot.LifecycleState.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        snapshot.SafeMessageKey.ShouldBe("Tenants.Correction.Unavailable.CurrentProjectionUnavailable");
        snapshot.CanSubmit.ShouldBeFalse();
    }

    [Fact]
    public void Revoke_is_not_confirmed_from_an_incomplete_projection_page()
    {
        GlobalAdministratorCorrectionSnapshot accepted = GlobalAdministratorCorrectionSnapshot
            .FromIntent(RevokeIntent(), ProjectionReady("admin-user", "other-admin"))
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-safe", "tracking-safe"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1));

        // The re-query returns a page that does NOT contain the target, but HasMore is true, so the
        // target could still be present on a later page. Absent-from-one-page is not proof of removal:
        // confirming here would be a fail-open, so the correction stays pending rather than collapse to
        // a false Confirmed.
        GlobalAdministratorCorrectionSnapshot notConfirmed = accepted.ConfirmProjection(
            PagedProjectionReady("other-admin", "second-admin"));

        notConfirmed.LifecycleState.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        notConfirmed.LifecycleState.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
        notConfirmed.FocusTarget.ShouldBe(TenantCommandFocusTarget.Refresh);
    }

    [Fact]
    public void Unavailable_intent_fails_closed_without_submit()
    {
        GlobalAdministratorCorrectionSnapshot snapshot = GlobalAdministratorCorrectionSnapshot.FromIntent(
            RestoreIntent(hasCommandSupport: false));

        snapshot.LifecycleState.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        snapshot.AuditState.ShouldBe(TenantCommandAuditState.MissingSupport);
        snapshot.CanSubmit.ShouldBeFalse();
    }

    [Fact]
    public void Degraded_projection_fails_closed_without_inferring_authority_state()
    {
        GlobalAdministratorCorrectionSnapshot snapshot = GlobalAdministratorCorrectionSnapshot.FromIntent(
            RevokeIntent(),
            GlobalAdministratorsSnapshot.Degraded([], GlobalAdministratorsReason.GatewayUnavailable));

        snapshot.LifecycleState.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        snapshot.SafeMessageKey.ShouldBe("Tenants.Correction.Unavailable.CurrentProjectionUnavailable");
        snapshot.CanSubmit.ShouldBeFalse();
    }

    [Fact]
    public void Preview_fails_closed_when_the_fixed_projection_is_stale_at_open()
    {
        // The pre-submit gate must fail closed on a Stale read exactly as the start gate and ConfirmProjection
        // do: a platform-authority correction can never be SUBMITTED against stale evidence, even though the
        // target is present with two admins in the stale read (which would otherwise be a submittable revoke).
        GlobalAdministratorCorrectionSnapshot snapshot = GlobalAdministratorCorrectionSnapshot.FromIntent(
            RevokeIntent(),
            GlobalAdministratorsSnapshot.Stale(
                [
                    new GlobalAdministratorRow("admin-user", ReadModelFreshnessState.Stale),
                    new GlobalAdministratorRow("other-admin", ReadModelFreshnessState.Stale),
                ],
                nextCursor: null,
                hasMore: false,
                eTag: "\"ga-etag\""));

        snapshot.LifecycleState.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        snapshot.SafeMessageKey.ShouldBe("Tenants.Correction.Unavailable.CurrentProjectionUnavailable");
        snapshot.CanSubmit.ShouldBeFalse();
    }

    [Fact]
    public void Preview_fails_closed_when_ready_projection_has_stale_freshness_at_open()
    {
        GlobalAdministratorCorrectionSnapshot snapshot = GlobalAdministratorCorrectionSnapshot.FromIntent(
            RevokeIntent(),
            GlobalAdministratorsSnapshot.Ready(
                [
                    new GlobalAdministratorRow("admin-user", ReadModelFreshnessState.Current),
                    new GlobalAdministratorRow("other-admin", ReadModelFreshnessState.Current),
                ],
                nextCursor: null,
                hasMore: false,
                eTag: "\"ga-etag\"",
                freshness: ReadModelFreshnessState.Stale));

        snapshot.LifecycleState.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        snapshot.SafeMessageKey.ShouldBe("Tenants.Correction.Unavailable.CurrentProjectionUnavailable");
        snapshot.CanSubmit.ShouldBeFalse();
    }

    [Fact]
    public void Unavailable_intent_is_not_masked_as_already_applied_by_the_projection()
    {
        // An unavailable intent must keep its fail-closed unavailability reason even when the current
        // projection shows the target already in the desired state; the AlreadyApplied copy must not hide
        // WHY the correction cannot run.
        GlobalAdministratorCorrectionSnapshot snapshot = GlobalAdministratorCorrectionSnapshot.FromIntent(
            RestoreIntent(hasCommandSupport: false),
            ProjectionReady("admin-user", "other-admin"));

        snapshot.LifecycleState.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        snapshot.AuditState.ShouldBe(TenantCommandAuditState.MissingSupport);
        snapshot.CanSubmit.ShouldBeFalse();
    }

    [Fact]
    public void Restore_is_confirmed_only_when_target_appears_in_fixed_projection()
    {
        GlobalAdministratorCorrectionSnapshot accepted = GlobalAdministratorCorrectionSnapshot
            .FromIntent(RestoreIntent(), ProjectionReady("other-admin"))
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-safe", "tracking-safe"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1));

        accepted.LifecycleState.ShouldBe(TenantCommandLifecycleState.ProjectionPending);

        GlobalAdministratorCorrectionSnapshot stillPending = accepted.ConfirmProjection(ProjectionReady("other-admin"));
        stillPending.LifecycleState.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        stillPending.FocusTarget.ShouldBe(TenantCommandFocusTarget.Refresh);

        GlobalAdministratorCorrectionSnapshot confirmed = accepted.ConfirmProjection(ProjectionReady("admin-user", "other-admin"));
        confirmed.LifecycleState.ShouldBe(TenantCommandLifecycleState.Confirmed);
    }

    [Fact]
    public void Revoke_is_confirmed_only_when_target_is_absent_in_fixed_projection()
    {
        GlobalAdministratorCorrectionSnapshot accepted = GlobalAdministratorCorrectionSnapshot
            .FromIntent(RevokeIntent(), ProjectionReady("admin-user", "other-admin"))
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-safe", "tracking-safe"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1));

        GlobalAdministratorCorrectionSnapshot stillPending = accepted.ConfirmProjection(ProjectionReady("admin-user", "other-admin"));
        stillPending.LifecycleState.ShouldBe(TenantCommandLifecycleState.ProjectionPending);

        GlobalAdministratorCorrectionSnapshot confirmed = accepted.ConfirmProjection(ProjectionReady("other-admin"));
        confirmed.LifecycleState.ShouldBe(TenantCommandLifecycleState.Confirmed);
    }

    [Fact]
    public void Revoke_is_not_confirmed_when_fixed_projection_returns_empty()
    {
        GlobalAdministratorCorrectionSnapshot accepted = GlobalAdministratorCorrectionSnapshot
            .FromIntent(RevokeIntent(), ProjectionReady("admin-user", "other-admin"))
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-safe", "tracking-safe"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1));

        // An empty/auth-scoped-empty re-query is never proof of a successful removal: a successful
        // revoke can never empty the fixed projection (the last administrator is hard-blocked), so the
        // correction must stay pending rather than collapse "target absent from an empty read" to success.
        GlobalAdministratorCorrectionSnapshot notConfirmed = accepted.ConfirmProjection(
            GlobalAdministratorsSnapshot.Empty(isAuthorizationScoped: true, ReadModelFreshnessState.Current, "\"ga-etag\""));

        notConfirmed.LifecycleState.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        notConfirmed.LifecycleState.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
        notConfirmed.FocusTarget.ShouldBe(TenantCommandFocusTarget.Refresh);
    }

    [Fact]
    public void Confirmation_requires_a_current_non_stale_projection()
    {
        GlobalAdministratorCorrectionSnapshot accepted = GlobalAdministratorCorrectionSnapshot
            .FromIntent(RestoreIntent(), ProjectionReady("other-admin"))
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-safe", "tracking-safe"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1));

        // A stale projection is held to the same fail-closed bar as the start gate's current-freshness
        // requirement; success may only be asserted from an authoritative current projection, even though
        // the target already appears present in the stale read.
        GlobalAdministratorCorrectionSnapshot stale = accepted.ConfirmProjection(
            GlobalAdministratorsSnapshot.Stale(
                [
                    new GlobalAdministratorRow("admin-user", ReadModelFreshnessState.Stale),
                    new GlobalAdministratorRow("other-admin", ReadModelFreshnessState.Stale),
                ],
                nextCursor: null,
                hasMore: false,
                eTag: "\"ga-etag\""));

        stale.LifecycleState.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        stale.LifecycleState.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
    }

    [Theory]
    [InlineData("LastGlobalAdministrator", "last global administrator")]
    [InlineData("GlobalAdministratorAlreadyExists", "already a global administrator")]
    [InlineData("GlobalAdministratorNotFound", "not a global administrator")]
    public void Rejections_stay_rejected_without_false_success(string rejectionCode, string safeMessage)
    {
        GlobalAdministratorCorrectionSnapshot snapshot = GlobalAdministratorCorrectionSnapshot
            .FromIntent(RevokeIntent(), ProjectionReady("admin-user", "other-admin"))
            .RequestSent()
            .ApplySubmissionFailure(TenantCommandSubmissionResult.Rejected(safeMessage, rejectionCode));

        snapshot.LifecycleState.ShouldBe(TenantCommandLifecycleState.Rejected);
        snapshot.LifecycleState.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
        snapshot.RejectionCode.ShouldBe(rejectionCode);
        snapshot.LiveRegionPoliteness.ShouldBe(TenantCommandLiveRegionPoliteness.Assertive);
    }

    [Fact]
    public void Corrective_proof_links_only_after_confirmation()
    {
        GlobalAdministratorCorrectionSnapshot confirmed = GlobalAdministratorCorrectionSnapshot
            .FromIntent(RestoreIntent(), ProjectionReady("other-admin"))
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-safe", "tracking-safe"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1))
            .ConfirmProjection(ProjectionReady("admin-user", "other-admin"));

        confirmed.CorrectiveEventType.ShouldBe("GlobalAdministratorSet");

        GlobalAdministratorCorrectionSnapshot delayed = confirmed.WithCorrectiveProof(null);
        delayed.ProofLink.ShouldBeNull();
        delayed.AuditState.ShouldBe(TenantCommandAuditState.AuditDelayed);

        GlobalAdministratorCorrectionSnapshot linked = confirmed.WithCorrectiveProof(CorrectiveRow("event-corrective", "GlobalAdministratorSet"));
        linked.ProofLink.ShouldNotBeNull().CorrectiveAuditReference.ShouldBe("event-corrective");
    }

    [Fact]
    public void Corrective_proof_is_ignored_before_confirmation()
    {
        GlobalAdministratorCorrectionSnapshot previewed = GlobalAdministratorCorrectionSnapshot.FromIntent(
            RestoreIntent(),
            ProjectionReady("other-admin"));

        previewed.WithCorrectiveProof(CorrectiveRow("event-corrective", "GlobalAdministratorSet")).ProofLink.ShouldBeNull();
    }

    [Fact]
    public void Corrective_proof_requires_parseable_original_timestamp()
    {
        GlobalAdministratorCorrectionSnapshot confirmed = GlobalAdministratorCorrectionSnapshot
            .FromIntent(MalformedTimestampIntent(), ProjectionReady("other-admin"))
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-safe", "tracking-safe"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1))
            .ConfirmProjection(ProjectionReady("admin-user", "other-admin"));

        GlobalAdministratorCorrectionSnapshot delayed = confirmed.WithCorrectiveProof(
            CorrectiveRow("event-corrective", "GlobalAdministratorSet"));

        delayed.ProofLink.ShouldBeNull();
        delayed.AuditState.ShouldBe(TenantCommandAuditState.AuditDelayed);
    }

    [Fact]
    public void Accepted_exposes_command_tracking_handle()
    {
        GlobalAdministratorCorrectionSnapshot snapshot = GlobalAdministratorCorrectionSnapshot
            .FromIntent(RestoreIntent(), ProjectionReady("other-admin"))
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-safe", "tracking-safe"));

        snapshot.HasCommandTracking.ShouldBeTrue();
        snapshot.TryGetTrackingHandle(out TenantCommandTrackingHandle handle).ShouldBeTrue();
        handle.ShouldBe(new TenantCommandTrackingHandle("message-safe", "tracking-safe"));
    }

    private static TenantCorrectionStartIntent RestoreIntent(bool hasCommandSupport = true)
        => TenantCorrectionStartIntent.Evaluate(Context("GlobalAdministratorRemoved", hasCommandSupport));

    private static TenantCorrectionStartIntent RevokeIntent(bool hasCommandSupport = true)
        => TenantCorrectionStartIntent.Evaluate(Context("GlobalAdministratorSet", hasCommandSupport));

    private static TenantCorrectionStartIntent MalformedTimestampIntent()
    {
        TenantCorrectionStartIntent intent = RestoreIntent();
        Dictionary<string, string> inputs = new(intent.RequiredPreviewInputs, StringComparer.Ordinal)
        {
            ["originalTimestamp"] = "not-a-timestamp",
        };

        return intent with { RequiredPreviewInputs = inputs };
    }

    private static TenantCorrectionStartContext Context(string eventType, bool hasCommandSupport)
        => new(
            TenantAuditReceipt.FromRow(Row(eventType)),
            Row(eventType),
            IsAuthorized: true,
            HasCurrentProjectionSnapshot: true,
            CurrentProjectionSnapshotReference: "Current global administrator projection is available.",
            HasTenantCommandSupport: false,
            HasGlobalAdministratorCommandSupport: hasCommandSupport);

    private static TenantAuditRow Row(string eventType)
        => new(
            "event-safe-reference",
            eventType,
            AuditEventCategory.Administrative,
            "actor-user",
            DateTimeOffset.Parse("2026-06-01T10:00:00Z", CultureInfo.InvariantCulture),
            "system",
            "admin-user",
            "global-administrators",
            eventType,
            "userId: admin-user",
            ReadModelFreshnessState.Current,
            ProjectionLifecycleState.Current,
            QueryResponseProvenance.ProjectionBacked);

    private static TenantAuditRow CorrectiveRow(string eventReference, string eventType)
        => new(
            eventReference,
            eventType,
            AuditEventCategory.Administrative,
            "actor-user",
            DateTimeOffset.Parse("2026-06-01T10:05:00Z", CultureInfo.InvariantCulture),
            "system",
            "admin-user",
            "global-administrators",
            eventType,
            "userId: admin-user",
            ReadModelFreshnessState.Current,
            ProjectionLifecycleState.Current,
            QueryResponseProvenance.ProjectionBacked);

    private static GlobalAdministratorsSnapshot ProjectionReady(params string[] userIds)
        => GlobalAdministratorsSnapshot.Ready(
            userIds.Select(userId => new GlobalAdministratorRow(
                userId,
                ReadModelFreshnessState.Current,
                ProjectionLifecycleState.Current)).ToArray(),
            nextCursor: null,
            hasMore: false,
            eTag: "\"ga-etag\"",
            freshness: ReadModelFreshnessState.Current) with
        {
            Lifecycle = ProjectionLifecycleState.Current,
            ProjectionVersion = "ga-v1",
            IsCompleteEvidence = true,
        };

    // A current, readable, but INCOMPLETE (paged) fixed projection: the loaded page is authoritative
    // for the rows it contains, but HasMore signals that more administrators exist beyond this page.
    private static GlobalAdministratorsSnapshot PagedProjectionReady(params string[] userIds)
        => GlobalAdministratorsSnapshot.Ready(
            userIds.Select(userId => new GlobalAdministratorRow(
                userId,
                ReadModelFreshnessState.Current,
                ProjectionLifecycleState.Current)).ToArray(),
            nextCursor: "ga-cursor-2",
            hasMore: true,
            eTag: "\"ga-etag\"",
            freshness: ReadModelFreshnessState.Current) with
        {
            Lifecycle = ProjectionLifecycleState.Current,
            ProjectionVersion = "ga-v1",
            IsCompleteEvidence = false,
        };
}
