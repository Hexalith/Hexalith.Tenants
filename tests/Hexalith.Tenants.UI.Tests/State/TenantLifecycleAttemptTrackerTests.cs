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
        DateTimeOffset attemptStart = DateTimeOffset.Parse("2026-06-01T12:02:00Z");

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
        DateTimeOffset attemptStart = DateTimeOffset.Parse("2026-06-01T12:02:00Z");

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
        DateTimeOffset attemptStart = DateTimeOffset.Parse("2026-06-01T12:02:00Z");
        tracker.BeginDispatch(disable, "tenant-sequence:41", attemptStart);

        tracker.FindDispatchIntent("tenant.alpha").ShouldBe(disable);
        _ = Should.Throw<InvalidOperationException>(() => tracker.BeginDispatch(
            enable,
            "tenant-sequence:42",
            attemptStart));
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

    [Theory]
    [InlineData("tenant-sequence:41", "tenant-sequence:42", "incoming-message")]
    [InlineData("tenant-sequence:42", "tenant-sequence:41", "retained-message")]
    [InlineData("tenant-sequence:41", "not-a-tenant-sequence-marker", "retained-message")]
    public void Same_rank_same_evidence_same_poll_count_merge_breaks_ties_by_projection_sequence_relation(
        string retainedProjectionVersion,
        string incomingProjectionVersion,
        string expectedSafeMessageKey)
    {
        // Isolates MergeSameAttempt's own preferred-selection tie-break (line ~292) from the separate
        // projectionEvidence switch: rank, EvidenceRevision, and PendingStatusPollCount are held equal so only
        // the TenantLifecycleSequenceRelation between the two LastObservedProjectionVersion markers decides
        // which snapshot's SafeMessageKey survives the merge.
        TenantLifecycleCommandSnapshot retained = Pending("tenant.alpha", "message-1") with
        {
            LastObservedProjectionVersion = retainedProjectionVersion,
            SafeMessageKey = "retained-message",
            EvidenceRevision = 10,
            PendingStatusPollCount = 2,
        };
        TenantLifecycleCommandSnapshot incoming = retained with
        {
            LastObservedProjectionVersion = incomingProjectionVersion,
            SafeMessageKey = "incoming-message",
        };
        TenantLifecycleAttemptTracker tracker = CreateTracker();
        tracker.Remember(retained).ShouldBeTrue();

        tracker.Remember(incoming).ShouldBeTrue();

        TenantLifecycleCommandSnapshot merged = tracker.Find("tenant.alpha").ShouldNotBeNull();
        merged.SafeMessageKey.ShouldBe(expectedSafeMessageKey);
    }

    [Fact]
    public void New_evidence_revision_outranks_a_higher_poll_count_without_losing_the_count()
    {
        TenantLifecycleCommandSnapshot observedNothing = Pending("tenant.alpha", "message-1") with
        {
            PendingStatusPollCount = 5,
            SafeMessageKey = "poll-only",
            EvidenceRevision = 10,
        };
        TenantLifecycleCommandSnapshot observedProjection = observedNothing with
        {
            PendingStatusPollCount = 1,
            SafeMessageKey = "projection-observed",
            LastObservedProjectionVersion = "tenant-sequence:42",
            EvidenceRevision = 11,
        };
        TenantLifecycleAttemptTracker tracker = CreateTracker();

        tracker.Remember(observedNothing).ShouldBeTrue();
        tracker.Remember(observedProjection).ShouldBeTrue();

        TenantLifecycleCommandSnapshot retained = tracker.Find("tenant.alpha").ShouldNotBeNull();
        retained.SafeMessageKey.ShouldBe("projection-observed");
        retained.PendingStatusPollCount.ShouldBe(5);
        retained.LastObservedProjectionVersion.ShouldBe("tenant-sequence:42");
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
        (string newerMessageId, DateTimeOffset newerStart, _) = tracker.BeginDispatch(
            intent with { Operation = TenantLifecycleOperation.EnableTenant },
            "tenant-sequence:42",
            now);
        newerMessageId.ShouldNotBe(messageId);

        tracker.Forget("tenant.alpha", newerMessageId);
        tracker.Remember(Accepted("tenant.alpha", newerMessageId, newerStart)).ShouldBeFalse();
        now += TenantLifecycleCommandSnapshot.MaximumRetainedAttemptDuration;
        tracker.Remember(Accepted("tenant.alpha", newerMessageId, now)).ShouldBeTrue();
    }

    [Fact]
    public void Deterministic_dispatch_identity_handles_a_pre_epoch_test_clock()
    {
        DateTimeOffset beforeEpoch = DateTimeOffset.Parse("1969-12-31T23:59:59Z");
        var tracker = new TenantLifecycleAttemptTracker(() => beforeEpoch);

        string messageId = tracker.BeginDispatch(
            new TenantLifecycleCommandRequest("tenant.alpha", TenantLifecycleOperation.DisableTenant),
            "tenant-sequence:41",
            beforeEpoch).MessageId;

        NUlid.Ulid.TryParse(messageId, out _).ShouldBeTrue();
    }

    [Fact]
    public void Regressed_clock_prunes_attempt_ownership_instead_of_retaining_forever()
    {
        DateTimeOffset now = DateTimeOffset.Parse("2026-06-01T12:00:00Z");
        var tracker = new TenantLifecycleAttemptTracker(() => now);
        tracker.Remember(Accepted("tenant.alpha", "message-1", now)).ShouldBeTrue();

        now = now.AddMinutes(-1);

        tracker.HasPendingOwnership("tenant.alpha").ShouldBeFalse();
    }

    [Fact]
    public void Same_attempt_rejects_different_correlations_in_both_arrival_orders()
    {
        foreach (TenantLifecycleCommandSnapshot[] order in new[]
        {
            new[] { Accepted("tenant.alpha", "message-1") with { CorrelationId = "correlation-a" }, Accepted("tenant.alpha", "message-1") with { CorrelationId = "correlation-b" } },
            new[] { Accepted("tenant.alpha", "message-1") with { CorrelationId = "correlation-b" }, Accepted("tenant.alpha", "message-1") with { CorrelationId = "correlation-a" } },
        })
        {
            TenantLifecycleAttemptTracker tracker = CreateTracker();
            tracker.Remember(order[0]).ShouldBeTrue();
            tracker.Remember(order[1]).ShouldBeFalse();
            tracker.Find("tenant.alpha").ShouldNotBeNull().CorrelationId.ShouldBe(order[0].CorrelationId);
        }
    }

    [Fact]
    public void Older_expired_attempt_cannot_replace_newer_terminal_tombstone()
    {
        DateTimeOffset now = DateTimeOffset.Parse("2026-06-01T12:04:00Z");
        var tracker = new TenantLifecycleAttemptTracker(() => now);
        TenantLifecycleCommandSnapshot newer = Accepted(
            "tenant.alpha",
            "message-newer",
            DateTimeOffset.Parse("2026-06-01T12:03:00Z"));
        tracker.Remember(newer).ShouldBeTrue();
        tracker.Forget("tenant.alpha", "message-newer");

        tracker.Remember(Accepted(
            "tenant.alpha",
            "message-expired",
            DateTimeOffset.Parse("2026-06-01T11:59:00Z"))).ShouldBeFalse();

        tracker.Remember(Accepted(
            "tenant.alpha",
            "message-between",
            DateTimeOffset.Parse("2026-06-01T12:02:00Z"))).ShouldBeFalse();
    }

    [Fact]
    public void Same_attempt_rejects_changed_immutable_baselines_or_start()
    {
        TenantLifecycleCommandSnapshot original = Accepted("tenant.alpha", "message-1");
        foreach (TenantLifecycleCommandSnapshot inconsistent in new[]
        {
            original with { PreviewProjectionVersion = "tenant-sequence:40" },
            original with { BaselineProjectionVersion = "tenant-sequence:40" },
            original with { AttemptStartedAtUtc = original.AttemptStartedAtUtc!.Value.AddSeconds(1) },
        })
        {
            TenantLifecycleAttemptTracker tracker = CreateTracker();
            tracker.Remember(original).ShouldBeTrue();
            tracker.Remember(inconsistent).ShouldBeFalse();
            tracker.Find("tenant.alpha").ShouldBe(original);
        }
    }

    [Fact]
    public void Projection_version_alone_decides_same_attempt_merge_in_both_arrival_orders()
    {
        TenantLifecycleCommandSnapshot lower = Pending("tenant.alpha", "message-1") with
        {
            LastObservedProjectionVersion = "tenant-sequence:42",
            EvidenceRevision = 7,
            PendingStatusPollCount = 3,
            SafeMessageKey = "lower",
        };
        TenantLifecycleCommandSnapshot higher = lower with
        {
            LastObservedProjectionVersion = "tenant-sequence:43",
            SafeMessageKey = "higher",
        };
        foreach (TenantLifecycleCommandSnapshot[] order in new[]
        {
            new[] { lower, higher },
            new[] { higher, lower },
        })
        {
            TenantLifecycleAttemptTracker tracker = CreateTracker();
            tracker.Remember(order[0]).ShouldBeTrue();
            tracker.Remember(order[1]).ShouldBeTrue();
            TenantLifecycleCommandSnapshot retained = tracker.Find("tenant.alpha").ShouldNotBeNull();
            retained.LastObservedProjectionVersion.ShouldBe("tenant-sequence:43");
            retained.SafeMessageKey.ShouldBe("higher");
        }
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
