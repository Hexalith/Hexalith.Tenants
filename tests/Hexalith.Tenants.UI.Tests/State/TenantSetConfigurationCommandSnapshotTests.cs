using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class TenantSetConfigurationCommandSnapshotTests
{
    [Fact]
    public void Matching_value_requires_command_event_evidence_and_ordered_advancement()
    {
        TenantSetConfigurationIntent intent = Intent();
        TenantSetConfigurationCommandSnapshot pending = Pending(intent, eventCount: 1);

        pending.ConfirmProjection(Proof(intent, "tenant-sequence:40")).State
            .ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        pending.ConfirmProjection(Proof(intent, "tenant-sequence:41")).State
            .ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        pending.ConfirmProjection(Proof(intent, "tenant-sequence:42")).State
            .ShouldBe(TenantCommandLifecycleState.Confirmed);
    }

    [Fact]
    public void Proof_for_another_exact_intent_cannot_confirm()
    {
        TenantSetConfigurationIntent intent = Intent();
        TenantSetConfigurationIntent other = new(
            intent.TenantId,
            intent.NamespacePrefix,
            intent.KeySuffix,
            intent.FullKey,
            "different-fingerprint");

        TenantSetConfigurationCommandSnapshot result = Pending(intent, eventCount: 1)
            .ConfirmProjection(Proof(other, "tenant-sequence:42"));

        result.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
    }

    [Theory]
    [InlineData("tenant-sequence:40", TenantCommandLifecycleState.ProjectionPending)]
    [InlineData("tenant-sequence:41", TenantCommandLifecycleState.AlreadyApplied)]
    [InlineData("tenant-sequence:42", TenantCommandLifecycleState.AlreadyApplied)]
    [InlineData("opaque:42", TenantCommandLifecycleState.ProjectionPending)]
    [InlineData(null, TenantCommandLifecycleState.ProjectionPending)]
    public void Zero_event_noop_requires_exact_proof_at_least_at_the_ordered_baseline(
        string? proofVersion,
        TenantCommandLifecycleState expectedState)
    {
        TenantSetConfigurationIntent intent = Intent();
        TenantSetConfigurationCommandSnapshot result = Pending(intent, eventCount: 0)
            .ConfirmProjection(Proof(intent, proofVersion));

        result.State.ShouldBe(expectedState);
        if (expectedState is TenantCommandLifecycleState.AlreadyApplied)
        {
            result.SafeMessageKey.ShouldBe("Tenants.Configuration.Set.AlreadyApplied.NoOp");
            result.AuditState.ShouldBe(TenantCommandAuditState.MissingSupport);
        }
    }

    [Theory]
    [InlineData(CommandStatus.Rejected, TenantCommandLifecycleState.Rejected)]
    [InlineData(CommandStatus.PublishFailed, TenantCommandLifecycleState.Degraded)]
    [InlineData(CommandStatus.TimedOut, TenantCommandLifecycleState.UnableToVerify)]
    public void Exact_projection_proof_never_erases_authoritative_terminal_status(
        CommandStatus status,
        TenantCommandLifecycleState expectedState)
    {
        TenantSetConfigurationIntent intent = Intent();
        TenantSetConfigurationCommandSnapshot terminal = RequestSent(intent)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(
                status,
                EventCount: status is CommandStatus.PublishFailed ? 1 : 0,
                HasVerifiedCommandIdentity: true));

        terminal.ConfirmProjection(Proof(intent, "tenant-sequence:42")).State.ShouldBe(expectedState);
    }

    [Fact]
    public void Tracking_mismatch_cannot_be_erased_by_projection_proof()
    {
        TenantSetConfigurationIntent intent = Intent();
        TenantSetConfigurationCommandSnapshot mismatch = RequestSent(intent)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(
                CommandStatus.Completed,
                EventCount: 1,
                HasVerifiedCommandIdentity: false));

        mismatch.ConfirmProjection(Proof(intent, "tenant-sequence:42")).State
            .ShouldBe(TenantCommandLifecycleState.UnableToVerify);
    }

    [Fact]
    public void Status_without_verified_aggregate_identity_fails_closed()
    {
        TenantSetConfigurationIntent intent = Intent();
        TenantSetConfigurationCommandSnapshot snapshot = RequestSent(intent)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(
                CommandStatus.Completed,
                EventCount: 1,
                HasVerifiedCommandIdentity: false));

        snapshot.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        snapshot.SafeMessageKey.ShouldBe("Tenants.Configuration.Set.UnableToVerify.TrackingMismatch");
    }

    [Fact]
    public void Signalr_is_only_a_nudge()
    {
        TenantSetConfigurationCommandSnapshot snapshot = RequestSent(Intent())
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"));

        TenantSetConfigurationCommandSnapshot nudged = snapshot.SignalRNudge();

        nudged.State.ShouldBe(TenantCommandLifecycleState.Accepted);
        nudged.LastConfigurationProof.ShouldBeNull();
        nudged.FocusTarget.ShouldBe(TenantCommandFocusTarget.Refresh);
    }

    [Fact]
    public void Retained_snapshot_contains_only_safe_intent_and_preview_types()
    {
        const string rawValue = "very-secret-value";
        TenantSetConfigurationIntent intent = new(
            "tenant.alpha",
            "billing",
            "mode",
            "billing.mode",
            TenantSetConfigurationValueFingerprint.Create(rawValue));
        TenantSetConfigurationCommandSnapshot snapshot = RequestSent(intent);

        snapshot.Intent.ShouldBe(intent);
        snapshot.Preview!.Intent.ShouldBe(intent);
        typeof(TenantSetConfigurationCommandSnapshot).GetProperties()
            .ShouldNotContain(property => property.PropertyType == typeof(SetTenantConfiguration));
    }

    [Fact]
    public void Snapshot_string_is_support_safe_and_omits_tracking_and_projection_metadata()
    {
        TenantSetConfigurationCommandSnapshot snapshot = RequestSent(Intent())
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"));

        string text = snapshot.ToString();

        text.ShouldNotContain("message-1", Case.Sensitive);
        text.ShouldNotContain("correlation-1", Case.Sensitive);
        text.ShouldNotContain("tenant-sequence", Case.Sensitive);
        text.ShouldNotContain("tenant.alpha", Case.Sensitive);
        text.ShouldContain("HasTracking = True");
    }

    private static TenantSetConfigurationCommandSnapshot Pending(
        TenantSetConfigurationIntent intent,
        int eventCount)
        => RequestSent(intent)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(
                CommandStatus.Completed,
                EventCount: eventCount,
                HasVerifiedCommandIdentity: true));

    private static TenantSetConfigurationCommandSnapshot RequestSent(TenantSetConfigurationIntent intent)
        => TenantSetConfigurationCommandSnapshot.Idle()
            .Previewed(Preview(intent))
            .RequestSent(Preview(intent), "message-1", DateTimeOffset.UtcNow);

    private static TenantSetConfigurationIntent Intent()
        => new("tenant.alpha", "billing", "mode", "billing.mode", "value-fingerprint");

    private static TenantSetConfigurationPreview Preview(TenantSetConfigurationIntent intent)
        => TenantSetConfigurationPreview.Create(
            intent,
            TenantStatus.Active,
            TenantSetConfigurationCurrentState.Different,
            ReadModelFreshnessState.Current,
            ProjectionLifecycleState.Current,
            "tenant-sequence:41",
            isAuthorized: true);

    private static TenantConfigurationProjectionProof Proof(
        TenantSetConfigurationIntent intent,
        string? projectionVersion)
        => TenantConfigurationProjectionProof.Create(
            intent.TenantId,
            TenantConfigurationProjectionProofKind.SetConfirmed,
            projectionVersion,
            intent.AttemptFingerprint);
}
