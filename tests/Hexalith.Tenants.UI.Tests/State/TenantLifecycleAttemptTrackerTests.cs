using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class TenantLifecycleAttemptTrackerTests
{
    [Fact]
    public void Retained_attempt_preserves_literal_intent_stable_handle_baseline_and_event_evidence()
    {
        TenantLifecycleAttemptTracker tracker = new();
        TenantLifecycleCommandSnapshot pending = Pending("Tenant.Mixed-01", "message-1");

        tracker.Remember(pending).ShouldBeTrue();

        TenantLifecycleCommandSnapshot retained = tracker.Find("Tenant.Mixed-01").ShouldNotBeNull();
        retained.Intent.ShouldBe(pending.Intent);
        retained.MessageId.ShouldBe("message-1");
        retained.CorrelationId.ShouldBe("correlation-1");
        retained.BaselineProjectionVersion.ShouldBe("tenant-sequence:41");
        retained.HasCommandEventEvidence.ShouldBeTrue();
        tracker.Find("tenant.mixed-01").ShouldBeNull();
    }

    [Fact]
    public void Late_terminal_attempt_cannot_remove_a_newer_retained_attempt()
    {
        TenantLifecycleAttemptTracker tracker = new();
        tracker.Remember(Pending("tenant.alpha", "message-new", DateTimeOffset.Parse("2026-06-01T12:02:00Z"))).ShouldBeTrue();

        tracker.Forget("tenant.alpha", "message-old");

        tracker.Find("tenant.alpha").ShouldNotBeNull().MessageId.ShouldBe("message-new");
        tracker.Forget("tenant.alpha", "message-new");
        tracker.Find("tenant.alpha").ShouldBeNull();
    }

    [Fact]
    public void Late_snapshot_cannot_overwrite_a_newer_retained_attempt()
    {
        TenantLifecycleAttemptTracker tracker = new();
        tracker.Remember(Pending("tenant.alpha", "message-new", DateTimeOffset.Parse("2026-06-01T12:02:00Z"))).ShouldBeTrue();

        tracker.Remember(Pending("tenant.alpha", "message-old", DateTimeOffset.Parse("2026-06-01T12:01:00Z"))).ShouldBeFalse();

        tracker.Find("tenant.alpha").ShouldNotBeNull().MessageId.ShouldBe("message-new");
    }

    [Fact]
    public void Newer_attempt_replaces_an_older_retained_attempt()
    {
        TenantLifecycleAttemptTracker tracker = new();
        tracker.Remember(Pending("tenant.alpha", "message-old", DateTimeOffset.Parse("2026-06-01T12:01:00Z"))).ShouldBeTrue();

        tracker.Remember(Pending("tenant.alpha", "message-new", DateTimeOffset.Parse("2026-06-01T12:02:00Z"))).ShouldBeTrue();

        tracker.Find("tenant.alpha").ShouldNotBeNull().MessageId.ShouldBe("message-new");
    }

    [Fact]
    public void Late_same_message_snapshot_cannot_regress_progress_event_evidence_or_poll_count()
    {
        TenantLifecycleAttemptTracker tracker = new();
        TenantLifecycleCommandSnapshot accepted = Accepted(
            "tenant.alpha",
            "message-1",
            DateTimeOffset.Parse("2026-06-01T12:01:00Z"));
        TenantLifecycleCommandSnapshot advanced = accepted
            .ApplyStatus(new TenantCommandStatusResult(
                CommandStatus.EventsStored,
                HasVerifiedCommandIdentity: true))
            .ApplyStatus(TenantCommandStatusResult.Pending("Status is not available yet."))
            .ApplyStatus(TenantCommandStatusResult.Pending("Status is not available yet."));
        tracker.Remember(advanced).ShouldBeTrue();

        tracker.Remember(accepted).ShouldBeTrue();

        TenantLifecycleCommandSnapshot retained = tracker.Find("tenant.alpha").ShouldNotBeNull();
        retained.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        retained.HasCommandEventEvidence.ShouldBeTrue();
        retained.PendingStatusPollCount.ShouldBe(2);
    }

    [Fact]
    public void Terminalized_message_cannot_be_readded_by_a_late_accepted_snapshot()
    {
        TenantLifecycleAttemptTracker tracker = new();
        TenantLifecycleCommandSnapshot accepted = Accepted(
            "tenant.alpha",
            "message-1",
            DateTimeOffset.Parse("2026-06-01T12:01:00Z"));
        tracker.Remember(accepted).ShouldBeTrue();
        tracker.Forget("tenant.alpha", "message-1");

        tracker.Remember(accepted).ShouldBeFalse();

        tracker.Find("tenant.alpha").ShouldBeNull();
    }

    [Fact]
    public void Terminal_tombstone_blocks_older_races_but_allows_a_genuinely_newer_message()
    {
        TenantLifecycleAttemptTracker tracker = new();
        tracker.Remember(Accepted(
            "tenant.alpha",
            "message-terminal",
            DateTimeOffset.Parse("2026-06-01T12:02:00Z"))).ShouldBeTrue();
        tracker.Forget("tenant.alpha", "message-terminal");

        tracker.Remember(Accepted(
            "tenant.alpha",
            "message-older",
            DateTimeOffset.Parse("2026-06-01T12:01:00Z"))).ShouldBeFalse();
        tracker.Remember(Accepted(
            "tenant.alpha",
            "message-newer",
            DateTimeOffset.Parse("2026-06-01T12:03:00Z"))).ShouldBeTrue();

        tracker.Find("tenant.alpha").ShouldNotBeNull().MessageId.ShouldBe("message-newer");
    }

    [Fact]
    public void Non_retained_snapshot_is_rejected()
    {
        TenantDetail detail = Detail("tenant.alpha");
        TenantLifecycleCommandSnapshot previewed = TenantLifecycleCommandSnapshot
            .Idle(detail)
            .Previewed(
                new TenantLifecycleCommandRequest("tenant.alpha", TenantLifecycleOperation.DisableTenant),
                detail,
                "tenant-sequence:41");

        _ = Should.Throw<ArgumentException>(() => new TenantLifecycleAttemptTracker().Remember(previewed));
    }

    private static TenantLifecycleCommandSnapshot Pending(
        string tenantId,
        string messageId,
        DateTimeOffset? attemptStartedAtUtc = null)
        => Accepted(tenantId, messageId, attemptStartedAtUtc) with
        {
            State = TenantCommandLifecycleState.ProjectionPending,
            HasCommandEventEvidence = true,
        };

    private static TenantLifecycleCommandSnapshot Accepted(
        string tenantId,
        string messageId,
        DateTimeOffset? attemptStartedAtUtc = null)
    {
        TenantDetail detail = Detail(tenantId);
        var intent = new TenantLifecycleCommandRequest(tenantId, TenantLifecycleOperation.DisableTenant);
        return TenantLifecycleCommandSnapshot
            .Idle(detail)
            .Previewed(intent, detail, "tenant-sequence:41")
            .RequestSent(intent, detail, "tenant-sequence:41", messageId)
            .Accepted(TenantCommandSubmissionResult.Accepted(messageId, "correlation-1")) with
        {
            AttemptStartedAtUtc = attemptStartedAtUtc ?? DateTimeOffset.Parse("2026-06-01T12:01:00Z"),
        };
    }

    private static TenantDetail Detail(string tenantId)
        => new(
            tenantId,
            "Alpha",
            "Tenant alpha description",
            TenantStatus.Active,
            [],
            new Dictionary<string, string>(),
            DateTimeOffset.Parse("2026-06-01T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
}
