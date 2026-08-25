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
    [InlineData(CommandStatus.Received, TenantCommandLifecycleState.Accepted, TenantCommandAuditState.AuditPending, TenantCommandLiveRegionPoliteness.Polite)]
    [InlineData(CommandStatus.Processing, TenantCommandLifecycleState.Accepted, TenantCommandAuditState.AuditPending, TenantCommandLiveRegionPoliteness.Polite)]
    [InlineData(CommandStatus.EventsStored, TenantCommandLifecycleState.ProjectionPending, TenantCommandAuditState.AuditPending, TenantCommandLiveRegionPoliteness.Polite)]
    [InlineData(CommandStatus.EventsPublished, TenantCommandLifecycleState.ProjectionPending, TenantCommandAuditState.AuditPending, TenantCommandLiveRegionPoliteness.Polite)]
    [InlineData(CommandStatus.Completed, TenantCommandLifecycleState.ProjectionPending, TenantCommandAuditState.AuditPending, TenantCommandLiveRegionPoliteness.Polite)]
    [InlineData(CommandStatus.Rejected, TenantCommandLifecycleState.Rejected, TenantCommandAuditState.AuditUnavailable, TenantCommandLiveRegionPoliteness.Assertive)]
    [InlineData(CommandStatus.PublishFailed, TenantCommandLifecycleState.Degraded, TenantCommandAuditState.AuditDelayed, TenantCommandLiveRegionPoliteness.Assertive)]
    [InlineData(CommandStatus.TimedOut, TenantCommandLifecycleState.UnableToVerify, TenantCommandAuditState.AuditDelayed, TenantCommandLiveRegionPoliteness.Assertive)]
    public void Command_status_results_remain_distinct_before_projection_confirmation(
        CommandStatus status,
        TenantCommandLifecycleState expectedState,
        TenantCommandAuditState expectedAudit,
        TenantCommandLiveRegionPoliteness expectedPoliteness)
    {
        TenantLifecycleCommandSnapshot snapshot = Started(
            TenantLifecycleOperation.DisableTenant,
            TenantStatus.Active)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(
                status,
                "Safe lifecycle status.",
                "TenantDisabled",
                EventCount: 1,
                HasVerifiedCommandIdentity: true));

        snapshot.State.ShouldBe(expectedState);
        snapshot.AuditState.ShouldBe(expectedAudit);
        snapshot.LiveRegionPoliteness.ShouldBe(expectedPoliteness);
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
        result.SafeMessageKey.ShouldBe("Tenants.Lifecycle.Status.Pending");
        result.LiveRegionPoliteness.ShouldBe(TenantCommandLiveRegionPoliteness.Polite);
        result.PendingStatusPollCount.ShouldBe(1);
    }

    [Fact]
    public void Pending_status_store_propagation_is_bounded_and_becomes_terminal()
    {
        TenantLifecycleCommandSnapshot snapshot = Started(
                TenantLifecycleOperation.DisableTenant,
                TenantStatus.Active)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"));

        snapshot = snapshot.ApplyStatus(TenantCommandStatusResult.Pending("Status is not available yet."));
        snapshot = snapshot.ApplyStatus(TenantCommandStatusResult.Pending("Status is not available yet."));
        TenantLifecycleCommandSnapshot terminal = snapshot.ApplyStatus(
            TenantCommandStatusResult.Pending("Status is not available yet."));

        terminal.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        terminal.RetainsAttempt.ShouldBeFalse();
        terminal.SafeMessageKey.ShouldBe("Tenants.Lifecycle.UnableToVerify.StatusPollLimit");
        terminal.PendingStatusPollCount.ShouldBe(3);
        terminal.LiveRegionPoliteness.ShouldBe(TenantCommandLiveRegionPoliteness.Assertive);
    }

    [Fact]
    public void Request_sent_is_not_retained_before_the_gateway_accepts_the_attempt()
    {
        TenantLifecycleCommandSnapshot requestSent = Started(
            TenantLifecycleOperation.DisableTenant,
            TenantStatus.Active);

        requestSent.State.ShouldBe(TenantCommandLifecycleState.RequestSent);
        requestSent.RetainsAttempt.ShouldBeFalse();
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
