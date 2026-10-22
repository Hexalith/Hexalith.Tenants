using System.Globalization;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

/// <summary>Qualifies bounded lifecycle recovery and allocation-free retained reads.</summary>
public sealed class TenantLifecycleMaintenanceTests
{
    private static readonly DateTimeOffset Start = DateTimeOffset.Parse("2026-10-22T12:00:00Z", CultureInfo.InvariantCulture);

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void PendingPollBudgetPreservesIdentityUntilExactLimitThenTerminatesUncertainty(int observationKind)
    {
        TenantLifecycleCommandSnapshot original = Accepted("tenant.alpha", "message-1");
        TenantLifecycleCommandSnapshot snapshot = original;
        for (int poll = 1; poll < TenantLifecycleCommandSnapshot.MaximumPendingStatusPollCount; poll++)
        {
            snapshot = snapshot.ApplyStatus(Observation(observationKind), Start.AddSeconds(poll));
            snapshot.RetainsAttempt.ShouldBeTrue();
            snapshot.PendingStatusPollCount.ShouldBe(poll);
            snapshot.MessageId.ShouldBe(original.MessageId);
            snapshot.CorrelationId.ShouldBe(original.CorrelationId);
            snapshot.AttemptStartedAtUtc.ShouldBe(Start);
        }

        TenantLifecycleCommandSnapshot terminal = snapshot.ApplyStatus(Observation(observationKind), Start.AddSeconds(30));
        terminal.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        terminal.HasTerminalOwnership.ShouldBeTrue();
        terminal.RetainsAttempt.ShouldBeFalse();
        terminal.PendingStatusPollCount.ShouldBe(TenantLifecycleCommandSnapshot.MaximumPendingStatusPollCount);
        terminal.SafeMessageKey.ShouldBe("Tenants.Lifecycle.UnableToVerify.PollBudget");
        terminal.AuditState.ShouldBe(TenantCommandAuditStates.Delayed);
        terminal.LastConfirmedProjection.ShouldBe(original.LastConfirmedProjection);
        terminal.BaselineProjectionVersion.ShouldBe(original.BaselineProjectionVersion);
        terminal.MessageId.ShouldBe(original.MessageId);
        terminal.CorrelationId.ShouldBe(original.CorrelationId);
        terminal.ApplyStatus(Observation(observationKind), Start.AddSeconds(31)).ShouldBe(terminal);
    }

    [Fact]
    public void MixedPendingObservationsAndTrackerRemountCannotRenewPollBudget()
    {
        var tracker = new TenantLifecycleAttemptTracker(() => Start.AddMinutes(1));
        TenantLifecycleCommandSnapshot original = Accepted("tenant.alpha", "message-1");
        TenantLifecycleCommandSnapshot snapshot = original;
        for (int poll = 1; poll < TenantLifecycleCommandSnapshot.MaximumPendingStatusPollCount; poll++)
        {
            snapshot = snapshot.ApplyStatus(Observation(poll % 4), Start.AddSeconds(poll));
            tracker.Remember(snapshot).ShouldBeTrue();
            tracker.Remember(original).ShouldBeTrue();
            snapshot = tracker.Find("tenant.alpha").ShouldNotBeNull();
            snapshot.PendingStatusPollCount.ShouldBe(poll);
        }

        TenantLifecycleCommandSnapshot terminal = snapshot.ApplyStatus(Observation(0), Start.AddMinutes(1));
        terminal.HasTerminalOwnership.ShouldBeTrue();
        tracker.Forget("tenant.alpha", terminal.MessageId!);
        tracker.HasPendingOwnership("tenant.alpha").ShouldBeFalse();
        tracker.Remember(original).ShouldBeFalse();
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    public void ExhaustedStatusBudgetPreservesStoredEventsForFinalAuthoritativeProjection(int observationKind, bool expired)
    {
        TenantLifecycleCommandSnapshot snapshot = Accepted("tenant.alpha", "message-1")
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.EventsStored, HasVerifiedCommandIdentity: true), Start) with
        {
            PendingStatusPollCount = TenantLifecycleCommandSnapshot.MaximumPendingStatusPollCount,
        };

        TenantLifecycleCommandSnapshot waiting = snapshot.ApplyStatus(Observation(observationKind), Start.AddMinutes(expired ? 5 : 1));
        waiting.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        waiting.HasCommandEventEvidence.ShouldBeTrue();
        waiting.MessageId.ShouldBe(snapshot.MessageId);
        waiting.CorrelationId.ShouldBe(snapshot.CorrelationId);
        TenantDetail proof = snapshot.LastConfirmedProjection! with { Status = TenantStatus.Disabled };
        waiting.ConfirmProjection(TenantDetailSnapshot.Ready(
            proof,
            eTag: null,
            ReadModelFreshnessState.Current,
            ProjectionLifecycleState.Current,
            "tenant-sequence:42")).State.ShouldBe(TenantCommandLifecycleState.Confirmed);
    }

    [Theory]
    [InlineData(CommandStatus.EventsStored, 1, TenantCommandLifecycleState.ProjectionPending)]
    [InlineData(CommandStatus.EventsPublished, 1, TenantCommandLifecycleState.ProjectionPending)]
    [InlineData(CommandStatus.Completed, 1, TenantCommandLifecycleState.ProjectionPending)]
    [InlineData(CommandStatus.Completed, 0, TenantCommandLifecycleState.UnableToVerify)]
    [InlineData(CommandStatus.Completed, null, TenantCommandLifecycleState.UnableToVerify)]
    [InlineData(CommandStatus.Rejected, 0, TenantCommandLifecycleState.Rejected)]
    [InlineData(CommandStatus.PublishFailed, 1, TenantCommandLifecycleState.Degraded)]
    [InlineData(CommandStatus.TimedOut, 0, TenantCommandLifecycleState.UnableToVerify)]
    public void AuthoritativeStatusAtBudgetBoundaryStillDeterminesOutcome(CommandStatus status, int? eventCount, TenantCommandLifecycleState expected)
    {
        TenantLifecycleCommandSnapshot snapshot = Accepted("tenant.alpha", "message-1") with
        {
            PendingStatusPollCount = TenantLifecycleCommandSnapshot.MaximumPendingStatusPollCount - 1,
        };

        snapshot.ApplyStatus(new TenantCommandStatusResult(status, EventCount: eventCount, HasVerifiedCommandIdentity: true), Start)
            .State.ShouldBe(expected);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void RequestedTenantReadsExpireAtExactDeadlineAndCannotResurrectOldOwnership(int readKind)
    {
        DateTimeOffset now = Start;
        var tracker = new TenantLifecycleAttemptTracker(() => now);
        TenantLifecycleCommandSnapshot accepted = Accepted("Tenant.Alpha", "message-1");
        tracker.BeginDispatch(accepted.Intent!, accepted.BaselineProjectionVersion!, Start);
        tracker.Remember(accepted).ShouldBeTrue();
        now = Start.AddMinutes(5).AddTicks(-1);
        tracker.HasPendingOwnership("Tenant.Alpha").ShouldBeTrue();
        now = Start.AddMinutes(5);

        switch (readKind)
        {
            case 0: tracker.Find("Tenant.Alpha").ShouldBeNull(); break;
            case 1: tracker.FindDispatchIntent("Tenant.Alpha").ShouldBeNull(); break;
            default: tracker.HasPendingOwnership("Tenant.Alpha").ShouldBeFalse(); break;
        }

        tracker.Find("tenant.alpha").ShouldBeNull();
        tracker.HasPendingOwnership("Tenant.Alpha").ShouldBeFalse();
        tracker.Remember(accepted).ShouldBeFalse();
        tracker.Remember(Accepted("Tenant.Alpha", "message-2") with { AttemptStartedAtUtc = now }).ShouldBeTrue();
        tracker.Find("Tenant.Alpha").ShouldNotBeNull().MessageId.ShouldBe("message-2");
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    public void DispatchOnlyReadsExpireAtExactDeadlineOrRegressedClock(int readKind, bool regressed)
    {
        DateTimeOffset now = Start;
        var tracker = new TenantLifecycleAttemptTracker(() => now);
        var intent = new TenantLifecycleCommandRequest("tenant.alpha", TenantLifecycleOperation.DisableTenant);
        string messageId = tracker.BeginDispatch(intent, "tenant-sequence:41", Start).MessageId;
        now = Start.AddMinutes(5).AddTicks(-1);
        tracker.FindDispatchIntent("tenant.alpha").ShouldBe(intent);
        tracker.HasPendingOwnership("tenant.alpha").ShouldBeTrue();
        now = regressed ? Start.AddTicks(-1) : Start.AddMinutes(5);

        switch (readKind)
        {
            case 0: tracker.Find("tenant.alpha").ShouldBeNull(); break;
            case 1: tracker.FindDispatchIntent("tenant.alpha").ShouldBeNull(); break;
            default: tracker.HasPendingOwnership("tenant.alpha").ShouldBeFalse(); break;
        }

        tracker.HasPendingOwnership("tenant.alpha").ShouldBeFalse();
        tracker.FindDispatchIntent("tenant.alpha").ShouldBeNull();
        tracker.Remember(Accepted("tenant.alpha", messageId)).ShouldBeFalse();
        string replacement = tracker.BeginDispatch(intent with { Operation = TenantLifecycleOperation.EnableTenant }, "tenant-sequence:42", now).MessageId;
        replacement.ShouldNotBe(messageId);
        tracker.HasPendingOwnership("tenant.alpha").ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void RequestedTenantReadsFailClosedWhenClockRegresses(int readKind)
    {
        DateTimeOffset now = Start;
        var tracker = new TenantLifecycleAttemptTracker(() => now);
        TenantLifecycleCommandSnapshot accepted = Accepted("tenant.alpha", "message-1");
        tracker.BeginDispatch(accepted.Intent!, accepted.BaselineProjectionVersion!, Start);
        tracker.Remember(accepted).ShouldBeTrue();
        now = Start.AddTicks(-1);

        switch (readKind)
        {
            case 0: tracker.Find("tenant.alpha").ShouldBeNull(); break;
            case 1: tracker.FindDispatchIntent("tenant.alpha").ShouldBeNull(); break;
            default: tracker.HasPendingOwnership("tenant.alpha").ShouldBeFalse(); break;
        }

        tracker.HasPendingOwnership("tenant.alpha").ShouldBeFalse();
    }

    [Fact]
    public void RepeatedRenderReadsAllocateNoDictionarySnapshotsForUnrelatedTenants()
    {
        var tracker = new TenantLifecycleAttemptTracker(() => Start);
        for (int index = 0; index < 256; index++)
        {
            string tenant = $"tenant.{index}";
            string messageId = tracker.BeginDispatch(new TenantLifecycleCommandRequest(tenant, TenantLifecycleOperation.DisableTenant), "tenant-sequence:41", Start).MessageId;
            tracker.Remember(Accepted(tenant, messageId)).ShouldBeTrue();
        }

        for (int index = 0; index < 256; index++)
        {
            string tenant = $"dispatch.{index}";
            tracker.BeginDispatch(new TenantLifecycleCommandRequest(tenant, TenantLifecycleOperation.DisableTenant), "tenant-sequence:41", Start);
            string terminalTenant = $"terminal.{index}";
            tracker.Remember(Accepted(terminalTenant, $"terminal-message-{index}")).ShouldBeTrue();
            tracker.Forget(terminalTenant, $"terminal-message-{index}");
        }

        tracker.Find("tenant.0").ShouldNotBeNull();
        tracker.FindDispatchIntent("dispatch.0").ShouldNotBeNull();
        tracker.HasPendingOwnership("tenant.0").ShouldBeTrue();
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int index = 0; index < 1000; index++)
        {
            _ = tracker.Find("tenant.0");
            _ = tracker.FindDispatchIntent("dispatch.0");
            _ = tracker.HasPendingOwnership("tenant.0");
        }

        long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        bytes.ShouldBe(0);
    }

    private static TenantCommandStatusResult Observation(int kind)
        => kind switch
        {
            0 => TenantCommandStatusResult.Pending(string.Empty),
            1 => TenantCommandStatusResult.RetryableFailure(string.Empty),
            2 => new TenantCommandStatusResult(CommandStatus.Received, HasVerifiedCommandIdentity: true),
            _ => new TenantCommandStatusResult(CommandStatus.Processing, HasVerifiedCommandIdentity: true),
        };

    private static TenantLifecycleCommandSnapshot Accepted(string tenantId, string messageId)
    {
        var detail = new TenantDetail(tenantId, "Alpha", "Description", TenantStatus.Active, [], new Dictionary<string, string>(), Start);
        return TenantLifecycleCommandSnapshot.Idle(detail)
            .RequestSent(new TenantLifecycleCommandRequest(tenantId, TenantLifecycleOperation.DisableTenant), detail, "tenant-sequence:41", messageId, Start)
            .Accepted(TenantCommandSubmissionResult.Accepted(messageId, "correlation-1"));
    }
}
