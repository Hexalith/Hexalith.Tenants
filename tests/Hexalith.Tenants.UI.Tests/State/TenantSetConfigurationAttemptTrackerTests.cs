using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.UI.State.TenantCommands;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class TenantSetConfigurationAttemptTrackerTests
{
    [Fact]
    public void Same_logical_dispatch_reuses_one_deterministic_ulid_and_rejects_a_different_intent()
    {
        DateTimeOffset now = new(2026, 8, 26, 10, 0, 0, TimeSpan.Zero);
        var tracker = new TenantSetConfigurationAttemptTracker(() => now);
        TenantSetConfigurationIntent intent = Intent("fingerprint-one");

        (string first, _) = tracker.BeginDispatch(intent, "tenant-sequence:41", now);
        (string second, _) = tracker.BeginDispatch(intent, "tenant-sequence:41", now.AddMinutes(1));

        first.ShouldBe(second);
        NUlid.Ulid.TryParse(first, out _).ShouldBeTrue();
        Should.Throw<InvalidOperationException>(() => tracker.BeginDispatch(
            Intent("fingerprint-two"),
            "tenant-sequence:41",
            now));
    }

    [Fact]
    public void Retained_attempt_is_adoptable_until_bounded_expiry()
    {
        DateTimeOffset now = new(2026, 8, 26, 10, 0, 0, TimeSpan.Zero);
        var tracker = new TenantSetConfigurationAttemptTracker(() => now);
        TenantSetConfigurationIntent intent = Intent("fingerprint-one");
        (string messageId, DateTimeOffset startedAt) = tracker.BeginDispatch(
            intent,
            "tenant-sequence:41",
            now);
        TenantSetConfigurationPreview preview = TenantSetConfigurationPreview.Create(
            intent,
            TenantStatus.Active,
            TenantSetConfigurationCurrentState.Different,
            ReadModelFreshnessState.Current,
            ProjectionLifecycleState.Current,
            "tenant-sequence:41",
            isAuthorized: true);
        TenantSetConfigurationCommandSnapshot retained = TenantSetConfigurationCommandSnapshot.Idle()
            .Previewed(preview)
            .RequestSent(preview, messageId, startedAt)
            .AmbiguousSubmission("Tenants.Configuration.Set.SubmissionEvidence.Ambiguous");

        tracker.Remember(retained).ShouldBeTrue();
        tracker.Find(intent.TenantId).ShouldBe(retained);

        now = now.Add(TenantSetConfigurationCommandSnapshot.MaximumRetainedAttemptDuration);
        tracker.Find(intent.TenantId).ShouldBeNull();
        tracker.HasPendingOwnership(intent.TenantId).ShouldBeFalse();
    }

    [Fact]
    public void Later_verified_event_evidence_clears_zero_event_interpretation()
    {
        DateTimeOffset now = new(2026, 8, 26, 10, 0, 0, TimeSpan.Zero);
        var tracker = new TenantSetConfigurationAttemptTracker(() => now);
        TenantSetConfigurationCommandSnapshot accepted = AcceptedSnapshot(tracker, now);
        TenantSetConfigurationCommandSnapshot zeroEvents = accepted.ApplyStatus(new TenantCommandStatusResult(
            CommandStatus.Completed,
            EventCount: 0,
            HasVerifiedCommandIdentity: true));
        TenantSetConfigurationCommandSnapshot eventEvidence = zeroEvents.ApplyStatus(new TenantCommandStatusResult(
            CommandStatus.Completed,
            EventCount: 1,
            HasVerifiedCommandIdentity: true));

        tracker.Remember(zeroEvents).ShouldBeTrue();
        tracker.Remember(eventEvidence).ShouldBeTrue();

        TenantSetConfigurationCommandSnapshot retained = tracker.Find("tenant.alpha").ShouldNotBeNull();
        retained.HasCommandEventEvidence.ShouldBeTrue();
        retained.CompletedWithoutEvents.ShouldBeFalse();
    }

    [Fact]
    public void Newer_verified_progress_supersedes_transient_degraded_evidence()
    {
        DateTimeOffset now = new(2026, 8, 26, 10, 0, 0, TimeSpan.Zero);
        var tracker = new TenantSetConfigurationAttemptTracker(() => now);
        TenantSetConfigurationCommandSnapshot accepted = AcceptedSnapshot(tracker, now);
        TenantSetConfigurationCommandSnapshot degraded = accepted.ApplyStatus(new TenantCommandStatusResult(
            CommandStatus.PublishFailed,
            EventCount: 0,
            HasVerifiedCommandIdentity: true));
        TenantSetConfigurationCommandSnapshot recovered = degraded.ApplyStatus(new TenantCommandStatusResult(
            CommandStatus.Processing,
            HasVerifiedCommandIdentity: true));

        tracker.Remember(degraded).ShouldBeTrue();
        tracker.Remember(recovered).ShouldBeTrue();

        tracker.Find("tenant.alpha").ShouldNotBeNull().State.ShouldBe(TenantCommandLifecycleState.Accepted);
    }

    [Fact]
    public void Attempt_fingerprint_binds_prefix_and_suffix_even_when_full_key_matches()
    {
        var first = new TenantSetConfigurationIntent("tenant.alpha", "a", "b.c", "a.b.c", "value-fingerprint");
        var second = new TenantSetConfigurationIntent("tenant.alpha", "a.b", "c", "a.b.c", "value-fingerprint");

        first.AttemptFingerprint.ShouldNotBe(second.AttemptFingerprint);
    }

    [Fact]
    public void Undefined_current_state_is_not_a_complete_preview()
    {
        TenantSetConfigurationPreview preview = TenantSetConfigurationPreview.Create(
            Intent("fingerprint-one"),
            TenantStatus.Active,
            (TenantSetConfigurationCurrentState)999,
            ReadModelFreshnessState.Current,
            ProjectionLifecycleState.Current,
            "tenant-sequence:41",
            isAuthorized: true);

        preview.IsComplete.ShouldBeFalse();
        preview.IsAlreadyApplied.ShouldBeFalse();
    }

    private static TenantSetConfigurationCommandSnapshot AcceptedSnapshot(
        TenantSetConfigurationAttemptTracker tracker,
        DateTimeOffset now)
    {
        TenantSetConfigurationIntent intent = Intent("fingerprint-one");
        (string messageId, DateTimeOffset startedAt) = tracker.BeginDispatch(intent, "tenant-sequence:41", now);
        TenantSetConfigurationPreview preview = TenantSetConfigurationPreview.Create(
            intent,
            TenantStatus.Active,
            TenantSetConfigurationCurrentState.Different,
            ReadModelFreshnessState.Current,
            ProjectionLifecycleState.Current,
            "tenant-sequence:41",
            isAuthorized: true);
        return TenantSetConfigurationCommandSnapshot.Idle()
            .Previewed(preview)
            .RequestSent(preview, messageId, startedAt)
            .Accepted(TenantCommandSubmissionResult.Accepted(messageId, "correlation-1"));
    }

    private static TenantSetConfigurationIntent Intent(string fingerprint)
        => new("tenant.alpha", "billing", "mode", "billing.mode", fingerprint);
}
