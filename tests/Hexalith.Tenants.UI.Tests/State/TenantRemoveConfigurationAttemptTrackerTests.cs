using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.UI.State.TenantCommands;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class TenantRemoveConfigurationAttemptTrackerTests
{
    [Fact]
    public void Same_logical_dispatch_reuses_one_ulid_and_different_intent_is_rejected()
    {
        DateTimeOffset now = new(2026, 8, 28, 10, 0, 0, TimeSpan.Zero);
        string generated = NUlid.Ulid.NewUlid().ToString();
        var tracker = new TenantRemoveConfigurationAttemptTracker(() => now, () => generated);
        TenantRemoveConfigurationIntent intent = Intent("tenant.alpha", "billing.mode");

        (string first, DateTimeOffset firstStartedAt) = tracker.BeginDispatch(
            intent,
            "tenant-sequence:41",
            now);
        (string second, DateTimeOffset secondStartedAt) = tracker.BeginDispatch(
            intent,
            "tenant-sequence:41",
            now.AddMinutes(1));

        first.ShouldBe(second);
        firstStartedAt.ShouldBe(secondStartedAt);
        NUlid.Ulid.TryParse(first, out _).ShouldBeTrue();
        Should.Throw<InvalidOperationException>(() => tracker.BeginDispatch(
            Intent("tenant.alpha", "billing.other"),
            "tenant-sequence:41",
            now));
        Should.Throw<InvalidOperationException>(() => tracker.BeginDispatch(
            intent,
            "tenant-sequence:42",
            now));
    }

    [Fact]
    public void Retained_attempt_is_adoptable_and_newer_evidence_merges_monotonically()
    {
        DateTimeOffset now = new(2026, 8, 28, 10, 0, 0, TimeSpan.Zero);
        var tracker = new TenantRemoveConfigurationAttemptTracker(() => now);
        TenantRemoveConfigurationCommandSnapshot accepted = AcceptedSnapshot(
            tracker,
            now,
            Intent("tenant.alpha", "billing.mode"));
        TenantRemoveConfigurationCommandSnapshot ambiguous = accepted.AmbiguousSubmission(
            "Tenants.Configuration.Remove.SubmissionEvidence.Ambiguous");
        TenantRemoveConfigurationCommandSnapshot projected = accepted.ApplyStatus(new TenantCommandStatusResult(
            CommandStatus.EventsPublished,
            EventCount: 1,
            HasVerifiedCommandIdentity: true));

        tracker.Remember(ambiguous).ShouldBeTrue();
        tracker.Find("tenant.alpha").ShouldNotBeNull().MessageId.ShouldBe(accepted.MessageId);
        tracker.Remember(projected).ShouldBeTrue();
        tracker.Remember(ambiguous).ShouldBeTrue();

        TenantRemoveConfigurationCommandSnapshot retained = tracker.Find("tenant.alpha").ShouldNotBeNull();
        retained.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        retained.HasCommandEventEvidence.ShouldBeTrue();
        retained.Intent.ShouldBe(accepted.Intent);
        retained.Preview.ShouldBe(accepted.Preview);
        retained.MessageId.ShouldBe(accepted.MessageId);
        retained.BaselineProjectionVersion.ShouldBe("tenant-sequence:41");
    }

    [Fact]
    public void Retained_attempt_and_pre_response_dispatch_are_pruned_at_bounded_expiry()
    {
        DateTimeOffset now = new(2026, 8, 28, 10, 0, 0, TimeSpan.Zero);
        var tracker = new TenantRemoveConfigurationAttemptTracker(() => now);
        TenantRemoveConfigurationIntent retainedIntent = Intent("tenant.alpha", "billing.mode");
        TenantRemoveConfigurationIntent dispatchOnlyIntent = Intent("tenant.beta", "billing.mode");
        TenantRemoveConfigurationCommandSnapshot retained = AcceptedSnapshot(tracker, now, retainedIntent)
            .AmbiguousSubmission("Tenants.Configuration.Remove.SubmissionEvidence.Ambiguous");
        _ = tracker.BeginDispatch(dispatchOnlyIntent, "tenant-sequence:12", now);

        tracker.Remember(retained).ShouldBeTrue();
        tracker.Find(retainedIntent.TenantId).ShouldBe(retained);
        tracker.HasPendingOwnership(dispatchOnlyIntent.TenantId).ShouldBeTrue();

        now = now.Add(TenantRemoveConfigurationCommandSnapshot.MaximumRetainedAttemptDuration);

        tracker.Find(retainedIntent.TenantId).ShouldBeNull();
        tracker.HasPendingOwnership(retainedIntent.TenantId).ShouldBeFalse();
        tracker.HasPendingOwnership(dispatchOnlyIntent.TenantId).ShouldBeFalse();
    }

    [Fact]
    public void Literal_tenant_ids_have_isolated_dispatch_and_retained_ownership()
    {
        DateTimeOffset now = new(2026, 8, 28, 10, 0, 0, TimeSpan.Zero);
        Queue<string> ids = new([
            NUlid.Ulid.NewUlid().ToString(),
            NUlid.Ulid.NewUlid().ToString(),
        ]);
        var tracker = new TenantRemoveConfigurationAttemptTracker(() => now, ids.Dequeue);
        TenantRemoveConfigurationIntent lower = Intent("tenant.alpha", "billing.mode");
        TenantRemoveConfigurationIntent mixed = Intent("Tenant.Alpha", "billing.mode");
        TenantRemoveConfigurationCommandSnapshot lowerSnapshot = AcceptedSnapshot(tracker, now, lower);
        TenantRemoveConfigurationCommandSnapshot mixedSnapshot = AcceptedSnapshot(tracker, now, mixed);

        tracker.Remember(lowerSnapshot).ShouldBeTrue();
        tracker.Remember(mixedSnapshot).ShouldBeTrue();

        lowerSnapshot.MessageId.ShouldNotBe(mixedSnapshot.MessageId);
        tracker.Find("tenant.alpha").ShouldNotBeNull().Intent.ShouldBe(lower);
        tracker.Find("Tenant.Alpha").ShouldNotBeNull().Intent.ShouldBe(mixed);
        tracker.Find("TENANT.ALPHA").ShouldBeNull();

        tracker.Forget("tenant.alpha", lowerSnapshot.MessageId!);
        tracker.HasPendingOwnership("tenant.alpha").ShouldBeFalse();
        tracker.HasPendingOwnership("Tenant.Alpha").ShouldBeTrue();
    }

    [Fact]
    public void Newer_safety_evidence_wins_then_later_stronger_event_evidence_can_recover()
    {
        DateTimeOffset now = new(2026, 8, 28, 10, 0, 0, TimeSpan.Zero);
        var tracker = new TenantRemoveConfigurationAttemptTracker(() => now);
        TenantRemoveConfigurationCommandSnapshot accepted = AcceptedSnapshot(
            tracker,
            now,
            Intent("tenant.alpha", "billing.mode"));
        TenantRemoveConfigurationCommandSnapshot projected = accepted.ApplyStatus(new TenantCommandStatusResult(
            CommandStatus.EventsStored,
            EventCount: 1,
            HasVerifiedCommandIdentity: true));
        TenantRemoveConfigurationCommandSnapshot unsafeIdentity = projected.ApplyStatus(new TenantCommandStatusResult(
            CommandStatus.Completed,
            EventCount: 1,
            HasVerifiedCommandIdentity: false));

        tracker.Remember(projected).ShouldBeTrue();
        tracker.Remember(unsafeIdentity).ShouldBeTrue();
        tracker.Find("tenant.alpha").ShouldNotBeNull().State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);

        TenantRemoveConfigurationCommandSnapshot recovered = unsafeIdentity.ApplyStatus(new TenantCommandStatusResult(
            CommandStatus.EventsPublished,
            EventCount: 1,
            HasVerifiedCommandIdentity: true));
        tracker.Remember(recovered).ShouldBeTrue();

        TenantRemoveConfigurationCommandSnapshot retained = tracker.Find("tenant.alpha").ShouldNotBeNull();
        retained.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        retained.HasCommandEventEvidence.ShouldBeTrue();
        retained.StatusObservationCount.ShouldBe(recovered.StatusObservationCount);
    }

    private static TenantRemoveConfigurationCommandSnapshot AcceptedSnapshot(
        TenantRemoveConfigurationAttemptTracker tracker,
        DateTimeOffset now,
        TenantRemoveConfigurationIntent intent)
    {
        (string messageId, DateTimeOffset startedAt) = tracker.BeginDispatch(
            intent,
            "tenant-sequence:41",
            now);
        TenantRemoveConfigurationPreview preview = TenantRemoveConfigurationPreview.Create(
            intent,
            TenantStatus.Active,
            TenantRemoveConfigurationCurrentState.Present,
            ReadModelFreshnessState.Current,
            ProjectionLifecycleState.Current,
            "tenant-sequence:41",
            isAuthorized: true);
        return TenantRemoveConfigurationCommandSnapshot.Idle()
            .Previewed(preview)
            .RequestSent(preview, messageId, startedAt)
            .Accepted(TenantCommandSubmissionResult.Accepted(messageId, $"correlation-{intent.TenantId}"));
    }

    private static TenantRemoveConfigurationIntent Intent(string tenantId, string fullKey)
        => new(tenantId, "billing", fullKey);
}
