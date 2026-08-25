using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class TenantLifecycleCommandSnapshotTests
{
    [Theory]
    [InlineData(TenantLifecycleOperation.DisableTenant, TenantStatus.Active, TenantStatus.Disabled)]
    [InlineData(TenantLifecycleOperation.EnableTenant, TenantStatus.Disabled, TenantStatus.Active)]
    public void Lifecycle_confirms_only_from_command_events_intended_status_and_newer_authoritative_version(
        TenantLifecycleOperation operation,
        TenantStatus baselineStatus,
        TenantStatus intendedStatus)
    {
        TenantLifecycleCommandSnapshot snapshot = Pending(operation, baselineStatus, hasEventEvidence: true);

        TenantLifecycleCommandSnapshot confirmed = snapshot.ConfirmProjection(
            Proof("tenant.alpha", intendedStatus, "tenant-sequence:42"));

        confirmed.State.ShouldBe(TenantCommandLifecycleState.Confirmed);
        confirmed.LastConfirmedStatus.ShouldBe(intendedStatus);
        confirmed.AuditState.ShouldBe(TenantCommandAuditState.AuditPending);
    }

    [Theory]
    [InlineData(false, TenantStatus.Disabled, "tenant-sequence:42", TenantCommandLifecycleState.UnableToVerify)]
    [InlineData(true, TenantStatus.Active, "tenant-sequence:42", TenantCommandLifecycleState.ProjectionPending)]
    [InlineData(true, TenantStatus.Disabled, "tenant-sequence:41", TenantCommandLifecycleState.ProjectionPending)]
    [InlineData(true, TenantStatus.Disabled, "tenant-sequence:40", TenantCommandLifecycleState.ProjectionPending)]
    [InlineData(true, TenantStatus.Disabled, "opaque-version", TenantCommandLifecycleState.ProjectionPending)]
    public void Disable_withholds_confirmation_when_any_causal_proof_conjunct_is_missing(
        bool hasEventEvidence,
        TenantStatus projectedStatus,
        string projectedVersion,
        TenantCommandLifecycleState expectedState)
    {
        TenantLifecycleCommandSnapshot snapshot = Pending(
            TenantLifecycleOperation.DisableTenant,
            TenantStatus.Active,
            hasEventEvidence);

        TenantLifecycleCommandSnapshot result = snapshot.ConfirmProjection(
            Proof("tenant.alpha", projectedStatus, projectedVersion));

        result.State.ShouldBe(expectedState);
        result.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
    }

    [Fact]
    public void Signalr_is_only_a_refresh_nudge_and_cannot_supply_command_event_evidence()
    {
        TenantLifecycleCommandSnapshot snapshot = Started(
            TenantLifecycleOperation.DisableTenant,
            TenantStatus.Active)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"));

        TenantLifecycleCommandSnapshot nudged = snapshot.SignalRNudge();
        TenantLifecycleCommandSnapshot result = nudged.ConfirmProjection(
            Proof("tenant.alpha", TenantStatus.Disabled, "tenant-sequence:42"));

        nudged.State.ShouldBe(TenantCommandLifecycleState.Accepted);
        nudged.HasCommandEventEvidence.ShouldBeFalse();
        result.State.ShouldBe(TenantCommandLifecycleState.Accepted);
    }

    [Theory]
    [InlineData("Tenant.Alpha", ReadModelFreshnessState.Current, ProjectionLifecycleState.Current, TenantDetailSurfaceKind.Ready)]
    [InlineData("tenant.alpha", ReadModelFreshnessState.Stale, ProjectionLifecycleState.Current, TenantDetailSurfaceKind.Ready)]
    [InlineData("tenant.alpha", ReadModelFreshnessState.Current, ProjectionLifecycleState.Stale, TenantDetailSurfaceKind.Ready)]
    [InlineData("tenant.alpha", ReadModelFreshnessState.Current, ProjectionLifecycleState.Current, TenantDetailSurfaceKind.Stale)]
    public void Wrong_scope_or_non_authoritative_projection_never_confirms(
        string tenantId,
        ReadModelFreshnessState freshness,
        ProjectionLifecycleState lifecycle,
        TenantDetailSurfaceKind kind)
    {
        TenantLifecycleCommandSnapshot snapshot = Pending(
            TenantLifecycleOperation.DisableTenant,
            TenantStatus.Active,
            hasEventEvidence: true);
        TenantDetailSnapshot proof = kind is TenantDetailSurfaceKind.Ready
            ? Proof(tenantId, TenantStatus.Disabled, "tenant-sequence:42", freshness, lifecycle)
            : TenantDetailSnapshot.Stale(
                Detail(tenantId, TenantStatus.Disabled),
                eTag: null,
                lifecycle,
                "tenant-sequence:42");

        TenantLifecycleCommandSnapshot result = snapshot.ConfirmProjection(proof);

        result.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
    }

    [Theory]
    [InlineData(CommandStatus.Received, null, TenantCommandLifecycleState.Accepted, TenantCommandAuditState.AuditPending, TenantCommandLiveRegionPoliteness.Polite, null)]
    [InlineData(CommandStatus.Processing, null, TenantCommandLifecycleState.Accepted, TenantCommandAuditState.AuditPending, TenantCommandLiveRegionPoliteness.Polite, null)]
    [InlineData(CommandStatus.EventsStored, null, TenantCommandLifecycleState.ProjectionPending, TenantCommandAuditState.AuditPending, TenantCommandLiveRegionPoliteness.Polite, null)]
    [InlineData(CommandStatus.EventsPublished, null, TenantCommandLifecycleState.ProjectionPending, TenantCommandAuditState.AuditPending, TenantCommandLiveRegionPoliteness.Polite, null)]
    [InlineData(CommandStatus.Completed, null, TenantCommandLifecycleState.ProjectionPending, TenantCommandAuditState.AuditPending, TenantCommandLiveRegionPoliteness.Polite, null)]
    [InlineData(CommandStatus.Rejected, "InsufficientPermissions", TenantCommandLifecycleState.Rejected, TenantCommandAuditState.AuditUnavailable, TenantCommandLiveRegionPoliteness.Assertive, "Tenants.Lifecycle.Message.Rejected.InsufficientPermissions")]
    [InlineData(CommandStatus.Rejected, "TenantDisabled", TenantCommandLifecycleState.Rejected, TenantCommandAuditState.AuditUnavailable, TenantCommandLiveRegionPoliteness.Assertive, "Tenants.Lifecycle.Message.Rejected.TenantDisabled")]
    [InlineData(CommandStatus.Rejected, "TenantNotFound", TenantCommandLifecycleState.Rejected, TenantCommandAuditState.AuditUnavailable, TenantCommandLiveRegionPoliteness.Assertive, "Tenants.Lifecycle.Message.Rejected.TenantNotFound")]
    [InlineData(CommandStatus.Rejected, "TenantLifecycleStateAlreadySet", TenantCommandLifecycleState.Rejected, TenantCommandAuditState.AuditUnavailable, TenantCommandLiveRegionPoliteness.Assertive, "Tenants.Lifecycle.Message.Rejected.TenantLifecycleStateAlreadySet")]
    [InlineData(CommandStatus.Rejected, "UnexpectedCode", TenantCommandLifecycleState.Rejected, TenantCommandAuditState.AuditUnavailable, TenantCommandLiveRegionPoliteness.Assertive, "Tenants.Lifecycle.Message.Rejected")]
    [InlineData(CommandStatus.PublishFailed, null, TenantCommandLifecycleState.Degraded, TenantCommandAuditState.AuditDelayed, TenantCommandLiveRegionPoliteness.Assertive, "Tenants.Lifecycle.Message.Degraded")]
    [InlineData(CommandStatus.TimedOut, null, TenantCommandLifecycleState.UnableToVerify, TenantCommandAuditState.AuditDelayed, TenantCommandLiveRegionPoliteness.Assertive, "Tenants.Lifecycle.Message.UnableToVerify")]
    public void Command_status_results_remain_distinct_before_projection_confirmation(
        CommandStatus status,
        string? rejectionCode,
        TenantCommandLifecycleState expectedState,
        TenantCommandAuditState expectedAudit,
        TenantCommandLiveRegionPoliteness expectedPoliteness,
        string? expectedSafeMessageKey)
    {
        TenantLifecycleCommandSnapshot snapshot = Started(
            TenantLifecycleOperation.DisableTenant,
            TenantStatus.Active)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(
                status,
                "Safe lifecycle status.",
                rejectionCode,
                EventCount: 1,
                HasVerifiedCommandIdentity: true));

        snapshot.State.ShouldBe(expectedState);
        snapshot.AuditState.ShouldBe(expectedAudit);
        snapshot.LiveRegionPoliteness.ShouldBe(expectedPoliteness);
        snapshot.SafeMessageKey.ShouldBe(expectedSafeMessageKey);
        snapshot.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
    }

    [Theory]
    [InlineData(CommandStatus.Rejected, TenantCommandLifecycleState.Rejected)]
    [InlineData(CommandStatus.PublishFailed, TenantCommandLifecycleState.Degraded)]
    [InlineData(CommandStatus.TimedOut, TenantCommandLifecycleState.UnableToVerify)]
    public void Matching_projection_cannot_convert_terminal_non_success_to_confirmed(
        CommandStatus status,
        TenantCommandLifecycleState expectedState)
    {
        TenantLifecycleCommandSnapshot terminal = Started(
            TenantLifecycleOperation.DisableTenant,
            TenantStatus.Active)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(
                status,
                "Safe non-success.",
                EventCount: 1,
                HasVerifiedCommandIdentity: true));

        TenantLifecycleCommandSnapshot result = terminal.ConfirmProjection(
            Proof("tenant.alpha", TenantStatus.Disabled, "tenant-sequence:42"));

        result.State.ShouldBe(expectedState);
        result.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
    }

    [Fact]
    public void Unknown_status_is_terminal_unable_to_verify_with_localizable_message()
    {
        TenantLifecycleCommandSnapshot result = Started(
            TenantLifecycleOperation.DisableTenant,
            TenantStatus.Active)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(TenantCommandStatusResult.Unknown("Lifecycle status is unavailable."));

        result.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        result.SafeMessage.ShouldBeNull();
        result.SafeMessageKey.ShouldBe("Tenants.Lifecycle.UnableToVerify.Status");
        result.FocusTarget.ShouldBe(TenantCommandFocusTarget.Refresh);
        result.LiveRegionPoliteness.ShouldBe(TenantCommandLiveRegionPoliteness.Assertive);
    }

    [Fact]
    public void Pending_status_store_propagation_retains_the_same_accepted_attempt()
    {
        TenantLifecycleCommandSnapshot accepted = Started(
            TenantLifecycleOperation.DisableTenant,
            TenantStatus.Active)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"));

        TenantLifecycleCommandSnapshot result = accepted.ApplyStatus(
            TenantCommandStatusResult.Pending("Status is not available yet."));

        result.State.ShouldBe(TenantCommandLifecycleState.Accepted);
        result.RetainsAttempt.ShouldBeTrue();
        result.MessageId.ShouldBe("message-1");
        result.CorrelationId.ShouldBe("correlation-1");
        result.SafeMessageKey.ShouldBe("Tenants.Lifecycle.StatusEvidence.Pending");
        result.LiveRegionPoliteness.ShouldBe(TenantCommandLiveRegionPoliteness.Polite);
        result.PendingStatusPollCount.ShouldBe(1);
    }

    [Fact]
    public void Pending_status_store_propagation_is_bounded_by_elapsed_time_and_becomes_terminal()
    {
        DateTimeOffset attemptStart = DateTimeOffset.Parse("2026-08-25T12:00:00Z");
        TenantLifecycleCommandSnapshot snapshot = Started(
                TenantLifecycleOperation.DisableTenant,
                TenantStatus.Active) with { AttemptStartedAtUtc = attemptStart };
        snapshot = snapshot
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"));

        snapshot = snapshot.ApplyStatus(
            TenantCommandStatusResult.Pending("Status is not available yet."),
            attemptStart.AddMinutes(4));
        TenantLifecycleCommandSnapshot terminal = snapshot.ApplyStatus(
            TenantCommandStatusResult.Pending("Status is not available yet."),
            attemptStart + TenantLifecycleCommandSnapshot.MaximumRetainedAttemptDuration);

        terminal.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        terminal.RetainsAttempt.ShouldBeFalse();
        terminal.SafeMessageKey.ShouldBe("Tenants.Lifecycle.UnableToVerify.StatusTimeout");
        terminal.PendingStatusPollCount.ShouldBe(2);
        terminal.LiveRegionPoliteness.ShouldBe(TenantCommandLiveRegionPoliteness.Assertive);
    }

    [Theory]
    [InlineData(CommandStatus.Received)]
    [InlineData(CommandStatus.Processing)]
    [InlineData(CommandStatus.EventsStored)]
    [InlineData(CommandStatus.EventsPublished)]
    [InlineData(CommandStatus.Completed)]
    public void Non_terminal_and_completed_event_statuses_release_ownership_at_the_attempt_deadline(
        CommandStatus status)
    {
        DateTimeOffset attemptStart = DateTimeOffset.Parse("2026-08-25T12:00:00Z");
        TenantLifecycleCommandSnapshot accepted = Started(
            TenantLifecycleOperation.DisableTenant,
            TenantStatus.Active) with { AttemptStartedAtUtc = attemptStart };
        accepted = accepted.Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"));

        TenantLifecycleCommandSnapshot result = accepted.ApplyStatus(
            new TenantCommandStatusResult(
                status,
                EventCount: 1,
                HasVerifiedCommandIdentity: true),
            attemptStart + TenantLifecycleCommandSnapshot.MaximumRetainedAttemptDuration);

        result.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        result.RetainsAttempt.ShouldBeFalse();
        result.SafeMessageKey.ShouldBe("Tenants.Lifecycle.UnableToVerify.StatusTimeout");
    }

    [Fact]
    public void Retryable_status_failure_retains_until_the_same_wall_clock_deadline()
    {
        DateTimeOffset attemptStart = DateTimeOffset.Parse("2026-08-25T12:00:00Z");
        TenantLifecycleCommandSnapshot accepted = Started(
            TenantLifecycleOperation.DisableTenant,
            TenantStatus.Active) with { AttemptStartedAtUtc = attemptStart };
        accepted = accepted.Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"));

        TenantLifecycleCommandSnapshot retryable = accepted.ApplyStatus(
            TenantCommandStatusResult.RetryableFailure("Temporary transport fault."),
            attemptStart.AddMinutes(1));
        TenantLifecycleCommandSnapshot terminal = retryable.ApplyStatus(
            TenantCommandStatusResult.RetryableFailure("Temporary transport fault."),
            attemptStart + TenantLifecycleCommandSnapshot.MaximumRetainedAttemptDuration);

        retryable.RetainsAttempt.ShouldBeTrue();
        retryable.SafeMessageKey.ShouldBe("Tenants.Lifecycle.StatusEvidence.RetryableFailure");
        terminal.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        terminal.SafeMessageKey.ShouldBe("Tenants.Lifecycle.UnableToVerify.StatusTimeout");
    }

    [Fact]
    public void Explicit_abandon_terminalizes_a_retained_attempt()
    {
        TenantLifecycleCommandSnapshot retained = Started(
                TenantLifecycleOperation.DisableTenant,
                TenantStatus.Active)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"));

        TenantLifecycleCommandSnapshot result = retained.Abandon();

        result.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        result.RetainsAttempt.ShouldBeFalse();
        result.SafeMessageKey.ShouldBe("Tenants.Lifecycle.UnableToVerify.Abandoned");
    }

    [Fact]
    public void Non_comparable_baseline_and_current_version_tokens_do_not_confirm_lifecycle_success()
    {
        TenantLifecycleCommandSnapshot pending = Pending(
            TenantLifecycleOperation.DisableTenant,
            TenantStatus.Active,
            hasEventEvidence: true) with { BaselineProjectionVersion = "legacy-etag" };

        TenantLifecycleCommandSnapshot result = pending.ConfirmProjection(
            Proof("tenant.alpha", TenantStatus.Disabled, "tenant-sequence:42"));

        result.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
    }

    [Fact]
    public void Parseable_projection_versions_with_different_prefixes_remain_pending()
    {
        TenantLifecycleCommandSnapshot pending = Pending(
            TenantLifecycleOperation.DisableTenant,
            TenantStatus.Active,
            hasEventEvidence: true) with { BaselineProjectionVersion = "tenant-sequence:41" };

        TenantLifecycleCommandSnapshot result = pending.ConfirmProjection(
            Proof("tenant.alpha", TenantStatus.Disabled, "other-sequence:42"));

        result.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        result.LastObservedProjectionVersion.ShouldBe("other-sequence:42");
    }

    [Fact]
    public void Request_sent_is_retained_when_dispatch_delivery_is_ambiguous()
    {
        TenantLifecycleCommandSnapshot requestSent = Started(
            TenantLifecycleOperation.DisableTenant,
            TenantStatus.Active);

        requestSent.State.ShouldBe(TenantCommandLifecycleState.RequestSent);
        requestSent.RetainsAttempt.ShouldBeTrue();
        requestSent.AttemptStartedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Blocking_an_accepted_attempt_preserves_its_recovery_identity()
    {
        TenantLifecycleCommandSnapshot accepted = Started(
                TenantLifecycleOperation.DisableTenant,
                TenantStatus.Active)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"));

        TenantLifecycleCommandSnapshot blocked = accepted.BlockedWithTracking(
            "Tenants.Lifecycle.UnableToVerify.ProofRead");

        blocked.State.ShouldBe(TenantCommandLifecycleState.Accepted);
        blocked.RetainsAttempt.ShouldBeTrue();
        blocked.Intent.ShouldBe(accepted.Intent);
        blocked.MessageId.ShouldBe("message-1");
        blocked.CorrelationId.ShouldBe("correlation-1");
        blocked.BaselineProjectionVersion.ShouldBe("tenant-sequence:41");
        blocked.SafeMessageKey.ShouldBe("Tenants.Lifecycle.UnableToVerify.ProofRead");
        blocked.LiveRegionPoliteness.ShouldBe(TenantCommandLiveRegionPoliteness.Assertive);
    }

    [Fact]
    public void Duplicate_prevention_keeps_the_preview_identity_and_is_non_success()
    {
        TenantLifecycleCommandSnapshot previewed = TenantLifecycleCommandSnapshot
            .Idle(Detail("tenant.alpha", TenantStatus.Active))
            .Previewed(
                new TenantLifecycleCommandRequest("tenant.alpha", TenantLifecycleOperation.DisableTenant),
                Detail("tenant.alpha", TenantStatus.Active),
                "tenant-sequence:41");

        TenantLifecycleCommandSnapshot duplicate = previewed.DuplicatePrevented(
            "A lifecycle command is already in progress.");

        duplicate.State.ShouldBe(TenantCommandLifecycleState.DuplicatePrevented);
        duplicate.Intent.ShouldBe(previewed.Intent);
        duplicate.PreviewProjectionVersion.ShouldBe("tenant-sequence:41");
        duplicate.RetainsAttempt.ShouldBeFalse();
        duplicate.HasTerminalOwnership.ShouldBeFalse();
        duplicate.LiveRegionPoliteness.ShouldBe(TenantCommandLiveRegionPoliteness.Assertive);
    }

    [Theory]
    [InlineData(TenantCommandLifecycleState.Confirmed)]
    [InlineData(TenantCommandLifecycleState.Rejected)]
    [InlineData(TenantCommandLifecycleState.Degraded)]
    [InlineData(TenantCommandLifecycleState.UnableToVerify)]
    public void Late_status_cannot_reopen_a_terminal_attempt(TenantCommandLifecycleState terminalState)
    {
        TenantLifecycleCommandSnapshot terminal = Pending(
            TenantLifecycleOperation.DisableTenant,
            TenantStatus.Active,
            hasEventEvidence: true) with
        {
            State = terminalState,
        };

        TenantLifecycleCommandSnapshot result = terminal.ApplyStatus(new TenantCommandStatusResult(
            CommandStatus.Received,
            HasVerifiedCommandIdentity: true));

        result.State.ShouldBe(terminalState);
    }

    [Fact]
    public void Completed_without_event_evidence_is_terminal_unable_to_verify()
    {
        TenantLifecycleCommandSnapshot result = Started(
            TenantLifecycleOperation.DisableTenant,
            TenantStatus.Active)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(
                CommandStatus.Completed,
                EventCount: 0,
                HasVerifiedCommandIdentity: true));

        result.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        result.SafeMessageKey.ShouldBe("Tenants.Lifecycle.UnableToVerify.MissingEventEvidence");
    }

    [Theory]
    [InlineData(CommandStatus.Received)]
    [InlineData(CommandStatus.Processing)]
    public void Late_pre_event_status_cannot_regress_projection_pending_or_erase_event_evidence(CommandStatus lateStatus)
    {
        TenantLifecycleCommandSnapshot projectionPending = Started(
                TenantLifecycleOperation.DisableTenant,
                TenantStatus.Active)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(
                CommandStatus.EventsStored,
                HasVerifiedCommandIdentity: true));

        TenantLifecycleCommandSnapshot result = projectionPending.ApplyStatus(new TenantCommandStatusResult(
            lateStatus,
            HasVerifiedCommandIdentity: true));

        result.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        result.HasCommandEventEvidence.ShouldBeTrue();
    }

    [Fact]
    public void Completed_without_event_count_preserves_prior_exact_command_event_evidence()
    {
        TenantLifecycleCommandSnapshot projectionPending = Started(
                TenantLifecycleOperation.DisableTenant,
                TenantStatus.Active)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(
                CommandStatus.EventsPublished,
                HasVerifiedCommandIdentity: true));

        TenantLifecycleCommandSnapshot result = projectionPending.ApplyStatus(new TenantCommandStatusResult(
            CommandStatus.Completed,
            EventCount: null,
            HasVerifiedCommandIdentity: true));

        result.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        result.HasCommandEventEvidence.ShouldBeTrue();
        result.SafeMessageKey.ShouldBeNull();
    }

    [Fact]
    public void Undefined_authoritative_tenant_status_is_rejected_without_replacing_last_confirmed_truth()
    {
        TenantLifecycleCommandSnapshot snapshot = Pending(
            TenantLifecycleOperation.DisableTenant,
            TenantStatus.Active,
            hasEventEvidence: true);

        TenantLifecycleCommandSnapshot result = snapshot.ConfirmProjection(
            Proof("tenant.alpha", (TenantStatus)999, "tenant-sequence:42"));

        result.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        result.SafeMessageKey.ShouldBe("Tenants.Lifecycle.UnableToVerify.ProofRead");
        result.LastConfirmedStatus.ShouldBe(TenantStatus.Active);
        result.LastConfirmedProjection.ShouldNotBeNull().Status.ShouldBe(TenantStatus.Active);
    }

    private static TenantLifecycleCommandSnapshot Pending(
        TenantLifecycleOperation operation,
        TenantStatus baselineStatus,
        bool hasEventEvidence)
    {
        TenantLifecycleCommandSnapshot snapshot = Started(operation, baselineStatus)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"));
        return hasEventEvidence
            ? snapshot.ApplyStatus(new TenantCommandStatusResult(
                CommandStatus.Completed,
                EventCount: 1,
                HasVerifiedCommandIdentity: true))
            : snapshot.ApplyStatus(new TenantCommandStatusResult(
                CommandStatus.Completed,
                EventCount: 0,
                HasVerifiedCommandIdentity: true));
    }

    private static TenantLifecycleCommandSnapshot Started(
        TenantLifecycleOperation operation,
        TenantStatus baselineStatus)
    {
        TenantDetail detail = Detail("tenant.alpha", baselineStatus);
        var intent = new TenantLifecycleCommandRequest("tenant.alpha", operation);
        return TenantLifecycleCommandSnapshot
            .Idle(detail)
            .Previewed(intent, detail, "tenant-sequence:41")
            .RequestSent(intent, detail, "tenant-sequence:41", "message-1");
    }

    private static TenantDetailSnapshot Proof(
        string tenantId,
        TenantStatus status,
        string projectionVersion,
        ReadModelFreshnessState freshness = ReadModelFreshnessState.Current,
        ProjectionLifecycleState lifecycle = ProjectionLifecycleState.Current)
        => TenantDetailSnapshot.Ready(
            Detail(tenantId, status),
            eTag: null,
            freshness,
            lifecycle,
            projectionVersion);

    private static TenantDetail Detail(string tenantId, TenantStatus status)
        => new(
            tenantId,
            "Alpha",
            "Tenant alpha description",
            status,
            [],
            new Dictionary<string, string>(),
            DateTimeOffset.Parse("2026-06-01T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
}
