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
        TenantLifecycleAttemptTracker tracker = CreateTracker();
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
        TenantLifecycleAttemptTracker tracker = CreateTracker();
        tracker.Remember(Pending("tenant.alpha", "message-new", DateTimeOffset.Parse("2026-06-01T12:02:00Z"))).ShouldBeTrue();

        tracker.Forget("tenant.alpha", "message-old");

        tracker.Find("tenant.alpha").ShouldNotBeNull().MessageId.ShouldBe("message-new");
        tracker.Forget("tenant.alpha", "message-new");
        tracker.Find("tenant.alpha").ShouldBeNull();
    }

    [Fact]
    public void Late_snapshot_cannot_overwrite_a_newer_retained_attempt()
    {
        TenantLifecycleAttemptTracker tracker = CreateTracker();
        tracker.Remember(Pending("tenant.alpha", "message-new", DateTimeOffset.Parse("2026-06-01T12:02:00Z"))).ShouldBeTrue();

        tracker.Remember(Pending("tenant.alpha", "message-old", DateTimeOffset.Parse("2026-06-01T12:01:00Z"))).ShouldBeFalse();

        tracker.Find("tenant.alpha").ShouldNotBeNull().MessageId.ShouldBe("message-new");
    }

    [Fact]
    public void Newer_attempt_replaces_an_older_retained_attempt()
    {
        TenantLifecycleAttemptTracker tracker = CreateTracker();
        tracker.Remember(Pending("tenant.alpha", "message-old", DateTimeOffset.Parse("2026-06-01T12:01:00Z"))).ShouldBeTrue();

        tracker.Remember(Pending("tenant.alpha", "message-new", DateTimeOffset.Parse("2026-06-01T12:02:00Z"))).ShouldBeTrue();

        tracker.Find("tenant.alpha").ShouldNotBeNull().MessageId.ShouldBe("message-new");
    }

    [Fact]
    public void Late_same_message_snapshot_cannot_regress_progress_event_evidence_or_poll_count()
    {
        TenantLifecycleAttemptTracker tracker = CreateTracker();
        TenantLifecycleCommandSnapshot accepted = Accepted(
            "tenant.alpha",
            "message-1",
            DateTimeOffset.Parse("2026-06-01T12:01:00Z"));
        DateTimeOffset observedAtUtc = accepted.AttemptStartedAtUtc!.Value.AddMinutes(1);
        TenantLifecycleCommandSnapshot advanced = accepted
            .ApplyStatus(new TenantCommandStatusResult(
                CommandStatus.EventsStored,
                HasVerifiedCommandIdentity: true), observedAtUtc)
            .ApplyStatus(TenantCommandStatusResult.Pending("Status is not available yet."), observedAtUtc)
            .ApplyStatus(TenantCommandStatusResult.Pending("Status is not available yet."), observedAtUtc);
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
        TenantLifecycleAttemptTracker tracker = CreateTracker();
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
        TenantLifecycleAttemptTracker tracker = CreateTracker();
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

        CreateTracker().Remember(previewed).ShouldBeFalse();
    }

    [Fact]
    public void Dispatch_identity_is_deterministic_for_the_same_logical_attempt()
    {
        TenantLifecycleAttemptTracker tracker = CreateTracker();
        var intent = new TenantLifecycleCommandRequest("Tenant.Mixed-01", TenantLifecycleOperation.DisableTenant);
        DateTimeOffset attemptStart = DateTimeOffset.Parse("2026-08-25T12:00:00Z");

        (string firstMessageId, DateTimeOffset firstStart, string firstBaseline) = tracker.BeginDispatch(
            intent,
            "tenant-sequence:41",
            attemptStart);
        (string repeatedMessageId, DateTimeOffset repeatedStart, string repeatedBaseline) = tracker.BeginDispatch(
            intent,
            "tenant-sequence:41",
            attemptStart.AddMinutes(1));

        repeatedMessageId.ShouldBe(firstMessageId);
        repeatedStart.ShouldBe(firstStart);
        repeatedBaseline.ShouldBe(firstBaseline);
        NUlid.Ulid.TryParse(firstMessageId, out _).ShouldBeTrue();
    }

    [Fact]
    public void Unresolved_dispatch_identity_survives_an_unrelated_baseline_advance()
    {
        TenantLifecycleAttemptTracker tracker = CreateTracker();
        var intent = new TenantLifecycleCommandRequest("tenant.alpha", TenantLifecycleOperation.DisableTenant);
        DateTimeOffset attemptStart = DateTimeOffset.Parse("2026-08-25T12:00:00Z");

        string first = tracker.BeginDispatch(intent, "tenant-sequence:41", attemptStart).MessageId;
        (string rebased, _, string retainedBaseline) = tracker.BeginDispatch(
            intent,
            "tenant-sequence:42",
            attemptStart.AddMinutes(1));

        rebased.ShouldBe(first);
        retainedBaseline.ShouldBe("tenant-sequence:41");
    }

    [Fact]
    public void Unresolved_dispatch_window_blocks_a_different_lifecycle_intent()
    {
        TenantLifecycleAttemptTracker tracker = CreateTracker();
        var disable = new TenantLifecycleCommandRequest("tenant.alpha", TenantLifecycleOperation.DisableTenant);
        var enable = new TenantLifecycleCommandRequest("tenant.alpha", TenantLifecycleOperation.EnableTenant);
        tracker.BeginDispatch(disable, "tenant-sequence:41", DateTimeOffset.UtcNow);

        tracker.FindDispatchIntent("tenant.alpha").ShouldBe(disable);
        _ = Should.Throw<InvalidOperationException>(() => tracker.BeginDispatch(
            enable,
            "tenant-sequence:42",
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Same_rank_equal_poll_merge_keeps_newer_projection_message_and_recovery_in_both_arrival_orders()
    {
        TenantLifecycleCommandSnapshot older = Pending("tenant.alpha", "message-1") with
        {
            LastObservedProjectionVersion = "tenant-sequence:41",
            SafeMessageKey = "older-message",
            RecoveryKey = "older-recovery",
            EvidenceRevision = 10,
        };
        TenantDetail newerDetail = Detail("tenant.alpha") with { Name = "Alpha refreshed" };
        TenantLifecycleCommandSnapshot newer = older with
        {
            LastConfirmedProjection = newerDetail,
            LastConfirmedStatus = newerDetail.Status,
            LastObservedProjectionVersion = "tenant-sequence:42",
            SafeMessageKey = "newer-message",
            RecoveryKey = "newer-recovery",
            EvidenceRevision = 11,
        };

        foreach (TenantLifecycleCommandSnapshot[] arrivalOrder in new[]
        {
            new[] { older, newer },
            new[] { newer, older },
        })
        {
            TenantLifecycleAttemptTracker tracker = CreateTracker();
            tracker.Remember(arrivalOrder[0]).ShouldBeTrue();
            tracker.Remember(arrivalOrder[1]).ShouldBeTrue();

            TenantLifecycleCommandSnapshot retained = tracker.Find("tenant.alpha").ShouldNotBeNull();
            retained.LastConfirmedProjection.ShouldBe(newerDetail);
            retained.LastObservedProjectionVersion.ShouldBe("tenant-sequence:42");
            retained.SafeMessageKey.ShouldBe("newer-message");
            retained.RecoveryKey.ShouldBe("newer-recovery");
        }
    }

    [Fact]
    public void Equal_timestamp_attempts_use_message_id_as_a_deterministic_tie_break()
    {
        DateTimeOffset startedAt = DateTimeOffset.Parse("2026-06-01T12:01:00Z");
        foreach (TenantLifecycleCommandSnapshot[] arrivalOrder in new[]
        {
            new[] { Accepted("tenant.alpha", "message-a", startedAt), Accepted("tenant.alpha", "message-b", startedAt) },
            new[] { Accepted("tenant.alpha", "message-b", startedAt), Accepted("tenant.alpha", "message-a", startedAt) },
        })
        {
            TenantLifecycleAttemptTracker tracker = CreateTracker();
            tracker.Remember(arrivalOrder[0]).ShouldBeTrue();
            _ = tracker.Remember(arrivalOrder[1]);
            tracker.Find("tenant.alpha").ShouldNotBeNull().MessageId.ShouldBe("message-b");
        }
    }

    [Fact]
    public void Expiry_prunes_dispatch_ownership_and_terminal_tombstones_without_a_timer()
    {
        DateTimeOffset now = DateTimeOffset.Parse("2026-06-01T12:00:00Z");
        var tracker = new TenantLifecycleAttemptTracker(() => now);
        var intent = new TenantLifecycleCommandRequest("tenant.alpha", TenantLifecycleOperation.DisableTenant);
        string messageId = tracker.BeginDispatch(intent, "tenant-sequence:41", now).MessageId;
        tracker.HasPendingOwnership("tenant.alpha").ShouldBeTrue();

        now += TenantLifecycleCommandSnapshot.MaximumRetainedAttemptDuration;

        tracker.HasPendingOwnership("tenant.alpha").ShouldBeFalse();
        tracker.TerminalTombstoneCount.ShouldBe(1);
        tracker.BeginDispatch(
            intent with { Operation = TenantLifecycleOperation.EnableTenant },
            "tenant-sequence:42",
            now).MessageId.ShouldNotBe(messageId);

        tracker.Forget("tenant.alpha");
        now += TenantLifecycleCommandSnapshot.MaximumRetainedAttemptDuration;
        tracker.TerminalTombstoneCount.ShouldBe(0);
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

    private static TenantLifecycleAttemptTracker CreateTracker()
        => new(() => DateTimeOffset.Parse("2026-06-01T12:04:00Z"));
}
