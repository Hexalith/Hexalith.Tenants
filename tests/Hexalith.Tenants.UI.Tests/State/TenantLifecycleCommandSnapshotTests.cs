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
    [InlineData(CommandStatus.Received, TenantCommandLifecycleState.Accepted, TenantCommandAuditState.AuditPending)]
    [InlineData(CommandStatus.Processing, TenantCommandLifecycleState.Accepted, TenantCommandAuditState.AuditPending)]
    [InlineData(CommandStatus.EventsStored, TenantCommandLifecycleState.ProjectionPending, TenantCommandAuditState.AuditPending)]
    [InlineData(CommandStatus.EventsPublished, TenantCommandLifecycleState.ProjectionPending, TenantCommandAuditState.AuditPending)]
    [InlineData(CommandStatus.Completed, TenantCommandLifecycleState.ProjectionPending, TenantCommandAuditState.AuditPending)]
    [InlineData(CommandStatus.Rejected, TenantCommandLifecycleState.Rejected, TenantCommandAuditState.AuditUnavailable)]
    [InlineData(CommandStatus.PublishFailed, TenantCommandLifecycleState.Degraded, TenantCommandAuditState.AuditDelayed)]
    [InlineData(CommandStatus.TimedOut, TenantCommandLifecycleState.UnableToVerify, TenantCommandAuditState.AuditDelayed)]
    public void Command_status_results_remain_distinct_before_projection_confirmation(
        CommandStatus status,
        TenantCommandLifecycleState expectedState,
        TenantCommandAuditState expectedAudit)
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
