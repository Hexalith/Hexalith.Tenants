using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Extensions;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using static Hexalith.Tenants.UI.Tests.TenantCommandProofFixtures;

namespace Hexalith.Tenants.UI.Tests.State;

/// <summary>Verifies membership waiting bounds independently of rendering and command delivery.</summary>
public sealed class TenantMembershipProjectionWaitTests
{
    private const string TenantId = "tenant.alpha";
    private const string MessageId = "01JARYZ6S41R0CKFWRD8C5MP47";
    private const string CorrelationId = "original-correlation";
    private const string ConfigurationKey = "Tenants:Ui:MembershipProjectionTimeout";
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);

    [Theory]
    [InlineData("00:00:00.0000001", 1L)]
    [InlineData("00:00:00.125", 1250000L)]
    [InlineData("49.17:02:47.294", 42949672940000L)]
    public void ValidBoundaryDurationsUseInvariantParsingUnderFrenchCulture(string configured, long ticks)
    {
        System.Globalization.CultureInfo original = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
            ManualTimeProvider clock = new();
            using TenantCommandAttemptTracker tracker = new(new(), Configuration(configured), clock);
            TenantRetainedCommandAttempt attempt = BeginPending(tracker, new(), "add");
            clock.Advance(TimeSpan.FromTicks(ticks - 1));
            StateOf(attempt.Snapshot).ShouldBe(TenantCommandLifecycleState.ProjectionPending);
            clock.Advance(TimeSpan.FromTicks(1));
            StateOf(attempt.Snapshot).ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void DisposalDuringExpiryNotificationPreventsFurtherObserverClaimsAndRetainsAdmission()
    {
        ManualTimeProvider clock = new();
        TenantAggregateCommandAdmissionGate gate = new();
        using TenantCommandAttemptTracker tracker = new(gate, timeProvider: clock);
        TenantRetainedCommandAttempt attempt = BeginPending(tracker, new(), "add");
        int laterObservers = 0;
        tracker.StateChanged += (_, _) => tracker.Dispose();
        tracker.StateChanged += (_, _) => laterObservers++;
        clock.Advance(DefaultTimeout);
        laterObservers.ShouldBe(0);
        StateOf(attempt.Snapshot).ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        clock.PendingTimerCount.ShouldBe(0);
        AssertUnresolvedAdmission(tracker, gate, attempt);
    }

    [Theory]
    [InlineData("add")]
    [InlineData("change")]
    [InlineData("remove")]
    public void DefaultBoundStartsOnlyAtFirstProjectionPendingAndExpiresAtExactBoundary(string kind)
    {
        ManualTimeProvider clock = new();
        TenantAggregateCommandAdmissionGate gate = new();
        using TenantCommandAttemptTracker tracker = new(gate, timeProvider: clock);
        object owner = new();
        TenantRetainedCommandAttempt attempt = BeginAccepted(tracker, owner, kind);
        object accepted = attempt.Snapshot;

        clock.Advance(TimeSpan.FromHours(1));
        attempt.Snapshot.ShouldBeSameAs(accepted);
        clock.PendingTimerCount.ShouldBe(0);

        object pending = ApplyStatus(accepted, Completed());
        tracker.TryUpdate(attempt, accepted, pending).ShouldBeTrue();
        clock.PendingTimerCount.ShouldBe(1);
        int expiryNotifications = 0;
        tracker.StateChanged += (_, _) => expiryNotifications++;

        clock.Advance(DefaultTimeout - TimeSpan.FromTicks(1));
        StateOf(attempt.Snapshot).ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        expiryNotifications.ShouldBe(0);
        clock.Advance(TimeSpan.FromTicks(1));

        AssertExpired(attempt.Snapshot, pending);
        clock.PendingTimerCount.ShouldBe(0);
        expiryNotifications.ShouldBe(1);
        AssertUnresolvedAdmission(tracker, gate, attempt);
        clock.Advance(TimeSpan.FromHours(1));
        expiryNotifications.ShouldBe(1);
    }

    [Theory]
    [InlineData("add")]
    [InlineData("change")]
    [InlineData("remove")]
    public void DeploymentConfigurationSelectsTheFixedDeadline(string kind)
    {
        ManualTimeProvider clock = new();
        TenantAggregateCommandAdmissionGate gate = new();
        using TenantCommandAttemptTracker tracker = new(gate, Configuration("00:00:07"), clock);
        TenantRetainedCommandAttempt attempt = BeginPending(tracker, new(), kind);
        object pending = attempt.Snapshot;

        clock.Advance(TimeSpan.FromSeconds(7) - TimeSpan.FromTicks(1));
        StateOf(attempt.Snapshot).ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        clock.Advance(TimeSpan.FromTicks(1));

        AssertExpired(attempt.Snapshot, pending);
        AssertUnresolvedAdmission(tracker, gate, attempt);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("not-a-duration")]
    [InlineData("00:00:00")]
    [InlineData("-00:00:01")]
    [InlineData("00:00:01,5")]
    [InlineData("50.00:00:00")]
    [InlineData("10675199.02:48:05.4775807")]
    public void InvalidConfiguredBoundsFailBeforeAnAttemptCanOwnAdmission(string configuredValue)
    {
        ManualTimeProvider clock = new();
        TenantAggregateCommandAdmissionGate gate = new();

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
        {
            using TenantCommandAttemptTracker tracker = new(gate, Configuration(configuredValue), clock);
        });

        exception.Message.ShouldContain(ConfigurationKey);
        clock.PendingTimerCount.ShouldBe(0);
        gate.IsLocked(TenantCommandAggregateLock.ForTenant(TenantId)).ShouldBeFalse();
    }

    [Fact]
    public void MissingConfigurationValueUsesDefaultRatherThanDisablingTheBound()
    {
        ManualTimeProvider clock = new();
        using TenantCommandAttemptTracker tracker = new(new(), new ConfigurationBuilder().Build(), clock);
        TenantRetainedCommandAttempt attempt = BeginPending(tracker, new(), "add");

        clock.Advance(DefaultTimeout);

        StateOf(attempt.Snapshot).ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        IsExpired(attempt.Snapshot).ShouldBeTrue();
    }

    [Theory]
    [InlineData("add")]
    [InlineData("change")]
    [InlineData("remove")]
    public void ModuleCompositionUsesItsConfigurationArgumentWhenTheHostHasNoConfigurationService(string kind)
    {
        ManualTimeProvider clock = new();
        ServiceCollection services = new();
        services.AddSingleton<TimeProvider>(clock);
        services.AddHexalithTenantsUiModule(Configuration("00:00:07"), enableGatewayAuthorization: false);
        using ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);
        using IServiceScope scope = provider.CreateScope();
        scope.ServiceProvider.GetService<IConfiguration>().ShouldBeNull();
        TenantCommandAttemptTracker tracker = scope.ServiceProvider.GetRequiredService<TenantCommandAttemptTracker>();
        TenantAggregateCommandAdmissionGate gate = scope.ServiceProvider.GetRequiredService<TenantAggregateCommandAdmissionGate>();
        TenantRetainedCommandAttempt attempt = BeginPending(tracker, new(), kind);
        object pending = attempt.Snapshot;

        clock.Advance(TimeSpan.FromSeconds(7) - TimeSpan.FromTicks(1));
        StateOf(attempt.Snapshot).ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        clock.Advance(TimeSpan.FromTicks(1));

        AssertExpired(attempt.Snapshot, pending);
        AssertUnresolvedAdmission(tracker, gate, attempt);
        clock.PendingTimerCount.ShouldBe(0);
    }

    [Theory]
    [InlineData("not-a-duration")]
    [InlineData("00:00:00")]
    public void ModuleCompositionRejectsItsInvalidConfigurationArgumentWhenTheTrackerIsResolved(string value)
    {
        ManualTimeProvider clock = new();
        ServiceCollection services = new();
        services.AddSingleton<TimeProvider>(clock);
        services.AddHexalithTenantsUiModule(Configuration(value), enableGatewayAuthorization: false);
        using ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);
        using IServiceScope scope = provider.CreateScope();
        scope.ServiceProvider.GetService<IConfiguration>().ShouldBeNull();

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
            scope.ServiceProvider.GetRequiredService<TenantCommandAttemptTracker>());

        exception.Message.ShouldContain(ConfigurationKey);
        clock.PendingTimerCount.ShouldBe(0);
        scope.ServiceProvider.GetRequiredService<TenantAggregateCommandAdmissionGate>()
            .IsLocked(TenantCommandAggregateLock.ForTenant(TenantId)).ShouldBeFalse();
    }

    [Fact]
    public void ModuleCompositionHonorsTheHostConfigurationServiceBeforeItsFallbackArgument()
    {
        ManualTimeProvider clock = new();
        ServiceCollection services = new();
        services.AddSingleton<TimeProvider>(clock);
        services.AddSingleton(Configuration("00:00:03"));
        services.AddHexalithTenantsUiModule(Configuration("00:00:07"), enableGatewayAuthorization: false);
        using ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);
        using IServiceScope scope = provider.CreateScope();
        TenantCommandAttemptTracker tracker = scope.ServiceProvider.GetRequiredService<TenantCommandAttemptTracker>();
        TenantRetainedCommandAttempt attempt = BeginPending(tracker, new(), "add");
        object pending = attempt.Snapshot;

        clock.Advance(TimeSpan.FromSeconds(3) - TimeSpan.FromTicks(1));
        StateOf(attempt.Snapshot).ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        clock.Advance(TimeSpan.FromTicks(1));

        AssertExpired(attempt.Snapshot, pending);
        clock.PendingTimerCount.ShouldBe(0);
    }

    [Theory]
    [InlineData("add")]
    [InlineData("change")]
    [InlineData("remove")]
    public void RepeatedStatusNudgesAndTemporaryFailureConsumeTheOriginalElapsedBound(string kind)
    {
        ManualTimeProvider clock = new();
        TenantAggregateCommandAdmissionGate gate = new();
        using TenantCommandAttemptTracker tracker = new(gate, timeProvider: clock);
        TenantRetainedCommandAttempt attempt = BeginPending(tracker, new(), kind);

        clock.Advance(TimeSpan.FromMinutes(2));
        for (int index = 0; index < 12; index++)
        {
            object current = attempt.Snapshot;
            tracker.TryUpdate(attempt, current, ApplyStatus(current, Completed())).ShouldBeTrue();
        }
        object beforeFailure = attempt.Snapshot;
        tracker.TryUpdate(attempt, beforeFailure,
            ApplyStatus(beforeFailure, TenantCommandStatusResult.Unknown("Status unavailable"))).ShouldBeTrue();
        StateOf(attempt.Snapshot).ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        IsExpired(attempt.Snapshot).ShouldBeFalse();

        clock.Advance(TimeSpan.FromMinutes(2));
        object beforeRecovery = attempt.Snapshot;
        tracker.TryUpdate(attempt, beforeRecovery, ApplyStatus(beforeRecovery, Completed())).ShouldBeTrue();
        StateOf(attempt.Snapshot).ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        clock.PendingTimerCount.ShouldBe(1);
        clock.Advance(TimeSpan.FromMinutes(1));

        IsExpired(attempt.Snapshot).ShouldBeTrue();
        StateOf(attempt.Snapshot).ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        object expired = attempt.Snapshot;
        tracker.TryUpdate(attempt, expired, ApplyStatus(expired, Completed())).ShouldBeTrue();
        StateOf(attempt.Snapshot).ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        clock.PendingTimerCount.ShouldBe(0);
        AssertUnresolvedAdmission(tracker, gate, attempt);
    }

    [Theory]
    [InlineData("add")]
    [InlineData("change")]
    [InlineData("remove")]
    public void DetachedAttemptExpiresWithoutLosingEvidenceOrAdmissionAndCanBeAdopted(string kind)
    {
        ManualTimeProvider clock = new();
        TenantAggregateCommandAdmissionGate gate = new();
        using TenantCommandAttemptTracker tracker = new(gate, timeProvider: clock);
        object owner = new();
        TenantRetainedCommandAttempt attempt = BeginPending(tracker, owner, kind);
        object pending = attempt.Snapshot;
        tracker.Detach(attempt, owner);

        clock.Advance(DefaultTimeout);

        attempt.SurfaceOwner.ShouldBeNull();
        AssertExpired(attempt.Snapshot, pending);
        AssertUnresolvedAdmission(tracker, gate, attempt);
        object replacementOwner = new();
        tracker.TryAdopt<object>(TenantId, replacementOwner, out TenantRetainedCommandAttempt? adopted,
            out object? adoptedSnapshot).ShouldBeTrue();
        adopted.ShouldBeSameAs(attempt);
        adoptedSnapshot.ShouldBeSameAs(attempt.Snapshot);
        tracker.IsAttached(attempt, replacementOwner).ShouldBeTrue();
        tracker.TryAdopt<object>(TenantId, new(), out _, out _).ShouldBeFalse();
        clock.Advance(DefaultTimeout);
        adoptedSnapshot.ShouldBeSameAs(attempt.Snapshot);
        clock.PendingTimerCount.ShouldBe(0);
    }

    [Theory]
    [InlineData("add")]
    [InlineData("change")]
    [InlineData("remove")]
    public void HeldQualifiedResultCanFinishTheExactAttemptAfterOnlyExpiryChangesItsSnapshot(string kind)
    {
        ManualTimeProvider clock = new();
        TenantAggregateCommandAdmissionGate gate = new();
        using TenantCommandAttemptTracker tracker = new(gate, timeProvider: clock);
        TenantRetainedCommandAttempt attempt = BeginPending(tracker, new(), kind);
        object capturedBeforeAwait = attempt.Snapshot;
        object lateQualifiedResult = Confirm(capturedBeforeAwait, QualifiedProof(kind));
        StateOf(lateQualifiedResult).ShouldBe(TenantCommandLifecycleState.Confirmed);

        clock.Advance(DefaultTimeout);
        IsExpired(attempt.Snapshot).ShouldBeTrue();
        tracker.TryUpdate(attempt, capturedBeforeAwait, lateQualifiedResult).ShouldBeTrue();

        StateOf(attempt.Snapshot).ShouldBe(TenantCommandLifecycleState.Confirmed);
        tracker.HasPendingOwnership(TenantId).ShouldBeFalse();
        gate.IsLocked(TenantCommandAggregateLock.ForTenant(TenantId)).ShouldBeFalse();
        clock.PendingTimerCount.ShouldBe(0);
    }

    [Theory]
    [InlineData("add")]
    [InlineData("change")]
    [InlineData("remove")]
    public void RepeatedFocusOnlyNudgesPreservePreExpiryHeldEvidenceUntilItsQualifiedResultArrives(string kind)
    {
        ManualTimeProvider clock = new();
        TenantAggregateCommandAdmissionGate gate = new();
        using TenantCommandAttemptTracker tracker = new(gate, timeProvider: clock);
        TenantRetainedCommandAttempt attempt = BeginPending(tracker, new(), kind);
        object capturedBeforeAwait = attempt.Snapshot;
        clock.Advance(DefaultTimeout);

        for (int index = 0; index < 4; index++)
        {
            object expired = attempt.Snapshot;
            object nudged = expired switch
            {
                TenantAddMemberCommandSnapshot add => add.SignalRNudge(),
                TenantChangeRoleCommandSnapshot change => change.SignalRNudge(),
                TenantRemoveMemberCommandSnapshot remove => remove.SignalRNudge(),
                _ => throw new InvalidOperationException(),
            };
            tracker.TryUpdate(attempt, expired, nudged).ShouldBeTrue();
            tracker.IsExpectedEvidence(attempt, capturedBeforeAwait).ShouldBeTrue();
            AssertExpired(attempt.Snapshot, capturedBeforeAwait);
            AssertUnresolvedAdmission(tracker, gate, attempt);
            clock.PendingTimerCount.ShouldBe(0);
        }

        object qualified = Confirm(capturedBeforeAwait, QualifiedProof(kind));
        tracker.TryUpdate(attempt, capturedBeforeAwait, qualified).ShouldBeTrue();

        StateOf(attempt.Snapshot).ShouldBe(TenantCommandLifecycleState.Confirmed);
        tracker.HasPendingOwnership(TenantId).ShouldBeFalse();
        gate.IsLocked(TenantCommandAggregateLock.ForTenant(TenantId)).ShouldBeFalse();
        clock.PendingTimerCount.ShouldBe(0);
    }

    [Theory]
    [InlineData("add")]
    [InlineData("change")]
    [InlineData("remove")]
    public void AuthoritativeEvidenceReadAfterExpiryCanConfirmWithoutResettingOrRedispatching(string kind)
    {
        ManualTimeProvider clock = new();
        TenantAggregateCommandAdmissionGate gate = new();
        using TenantCommandAttemptTracker tracker = new(gate, timeProvider: clock);
        TenantRetainedCommandAttempt attempt = BeginPending(tracker, new(), kind);
        clock.Advance(DefaultTimeout);
        object expired = attempt.Snapshot;

        object confirmed = Confirm(expired, QualifiedProof(kind));
        StateOf(confirmed).ShouldBe(TenantCommandLifecycleState.Confirmed);
        tracker.TryUpdate(attempt, expired, confirmed).ShouldBeTrue();

        tracker.HasPendingOwnership(TenantId).ShouldBeFalse();
        gate.IsLocked(TenantCommandAggregateLock.ForTenant(TenantId)).ShouldBeFalse();
        if (confirmed is TenantRemoveMemberCommandSnapshot removal)
        {
            removal.CommittedEventSequence.ShouldBe(2);
            removal.ProjectionProofCallerScope.ShouldBe("test-caller");
            removal.AuditState.ShouldBe(TenantCommandAuditState.AuditPending);
        }
    }

    [Theory]
    [InlineData("add")]
    [InlineData("change")]
    [InlineData("remove")]
    public void ExpiryDoesNotAllowAResultCapturedBeforeAnotherEvidenceTransition(string kind)
    {
        ManualTimeProvider clock = new();
        TenantAggregateCommandAdmissionGate gate = new();
        using TenantCommandAttemptTracker tracker = new(gate, timeProvider: clock);
        TenantRetainedCommandAttempt attempt = BeginPending(tracker, new(), kind);
        object stale = attempt.Snapshot;
        clock.Advance(DefaultTimeout);
        tracker.TryUpdate(attempt, stale,
            ApplyStatus(stale, TenantCommandStatusResult.Unknown("Status unavailable"))).ShouldBeTrue();

        tracker.IsExpectedEvidence(attempt, stale).ShouldBeFalse();
        tracker.TryUpdate(attempt, stale, Confirm(stale, QualifiedProof(kind))).ShouldBeFalse();
        StateOf(attempt.Snapshot).ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        AssertUnresolvedAdmission(tracker, gate, attempt);
    }

    [Theory]
    [InlineData("add")]
    [InlineData("change")]
    [InlineData("remove")]
    public void UnavailableMismatchedAndUnadvancedEvidenceCannotEscapeExpiredUncertainty(string kind)
    {
        ManualTimeProvider clock = new();
        TenantAggregateCommandAdmissionGate gate = new();
        using TenantCommandAttemptTracker tracker = new(gate, timeProvider: clock);
        TenantRetainedCommandAttempt attempt = BeginPending(tracker, new(), kind);
        clock.Advance(DefaultTimeout);
        TenantDetailSnapshot?[] unqualified =
        [
            null,
            Proof(Detail("tenant.other", []), "tenant-sequence:2"),
            QualifiedProof(kind, "tenant-sequence:1"),
        ];

        foreach (TenantDetailSnapshot? proof in unqualified)
        {
            object current = attempt.Snapshot;
            tracker.TryUpdate(attempt, current, Confirm(current, proof)).ShouldBeTrue();
            StateOf(attempt.Snapshot).ShouldBe(TenantCommandLifecycleState.UnableToVerify);
            IsExpired(attempt.Snapshot).ShouldBeTrue();
            clock.PendingTimerCount.ShouldBe(0);
        }

        AssertUnresolvedAdmission(tracker, gate, attempt);
    }

    [Theory]
    [InlineData("add")]
    [InlineData("change")]
    [InlineData("remove")]
    public void TerminalEvidenceStopsItsTimerAndCannotExpireAReplacementAttempt(string kind)
    {
        ManualTimeProvider clock = new();
        TenantAggregateCommandAdmissionGate gate = new();
        using TenantCommandAttemptTracker tracker = new(gate, timeProvider: clock);
        TenantRetainedCommandAttempt original = BeginPending(tracker, new(), kind);
        clock.Advance(TimeSpan.FromMinutes(1));
        object pending = original.Snapshot;
        object rejected = ApplyStatus(pending, new(CommandStatus.Rejected, HasVerifiedCommandIdentity: true));
        tracker.TryUpdate(original, pending, rejected).ShouldBeTrue();
        clock.PendingTimerCount.ShouldBe(0);
        gate.IsLocked(TenantCommandAggregateLock.ForTenant(TenantId)).ShouldBeFalse();
        TenantRetainedCommandAttempt replacement = BeginPending(tracker, new(), kind,
            "01JARYZ6S41R0CKFWRD8C5MP48", "replacement-correlation");
        replacement.ShouldNotBeSameAs(original);

        clock.Advance(TimeSpan.FromMinutes(4));

        StateOf(original.Snapshot).ShouldBe(TenantCommandLifecycleState.Rejected);
        StateOf(replacement.Snapshot).ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        tracker.TryUpdate(original, pending, Confirm(pending, QualifiedProof(kind))).ShouldBeFalse();
        gate.IsOwnedBy(TenantCommandAggregateLock.ForTenant(TenantId), replacement.Lease).ShouldBeTrue();
        replacement.Lease.CurrentOwner.ShouldBeSameAs(replacement.LeaseOwner);
        clock.Advance(TimeSpan.FromMinutes(1));
        IsExpired(replacement.Snapshot).ShouldBeTrue();
        AssertUnresolvedAdmission(tracker, gate, replacement);
    }

    [Theory]
    [InlineData("add")]
    [InlineData("change")]
    [InlineData("remove")]
    public void TrackerDisposalStopsTimersAndObserversWithoutInventingTerminalEvidence(string kind)
    {
        ManualTimeProvider clock = new();
        TenantAggregateCommandAdmissionGate gate = new();
        using TenantCommandAttemptTracker tracker = new(gate, timeProvider: clock);
        TenantRetainedCommandAttempt attempt = BeginPending(tracker, new(), kind);
        object pending = attempt.Snapshot;
        int notifications = 0;
        tracker.StateChanged += (_, _) => notifications++;
        clock.Advance(TimeSpan.FromMinutes(1));

        tracker.Dispose();
        tracker.Dispose();
        clock.Advance(TimeSpan.FromHours(1));

        clock.PendingTimerCount.ShouldBe(0);
        notifications.ShouldBe(0);
        attempt.Snapshot.ShouldBeSameAs(pending);
        IsExpired(attempt.Snapshot).ShouldBeFalse();
        gate.IsOwnedBy(TenantCommandAggregateLock.ForTenant(TenantId), attempt.Lease).ShouldBeTrue();
        attempt.Lease.CurrentOwner.ShouldBeSameAs(attempt.LeaseOwner);
    }

    [Theory]
    [InlineData("create")]
    [InlineData("metadata")]
    public void MembershipBoundDoesNotScheduleOrExpireOtherRetainedCommandKinds(string kind)
    {
        ManualTimeProvider clock = new();
        TenantAggregateCommandAdmissionGate gate = new();
        using TenantCommandAttemptTracker tracker = new(gate, Configuration("00:00:01"), clock);
        object pending = kind switch
        {
            "create" => new TenantCreateCommandSnapshot(TenantCommandLifecycleState.ProjectionPending,
                MessageId: MessageId, CorrelationId: CorrelationId),
            _ => new TenantUpdateMetadataCommandSnapshot(TenantCommandLifecycleState.ProjectionPending,
                MessageId: MessageId, CorrelationId: CorrelationId),
        };
        object owner = new();
        tracker.TryBegin(TenantId, owner, pending, out TenantRetainedCommandAttempt? attempt).ShouldBeTrue();
        tracker.TryStartDelivery(attempt!, owner).ShouldBeTrue();
        tracker.TryCompleteDelivery(attempt!, pending, pending).ShouldBeTrue();

        clock.Advance(TimeSpan.FromDays(1));

        clock.PendingTimerCount.ShouldBe(0);
        attempt!.Snapshot.ShouldBeSameAs(pending);
        AssertUnresolvedAdmission(tracker, gate, attempt);
    }

    [Theory]
    [InlineData("create")]
    [InlineData("metadata")]
    [InlineData("lifecycle")]
    public void MembershipPolicyLeavesOtherPendingLifecycleContractsUntouched(string kind)
    {
        object snapshot = kind switch
        {
            "create" => new TenantCreateCommandSnapshot(TenantCommandLifecycleState.ProjectionPending,
                MessageId: MessageId, CorrelationId: CorrelationId),
            "metadata" => new TenantUpdateMetadataCommandSnapshot(TenantCommandLifecycleState.ProjectionPending,
                MessageId: MessageId, CorrelationId: CorrelationId),
            _ => new TenantLifecycleCommandSnapshot(TenantCommandLifecycleState.ProjectionPending,
                MessageId: MessageId, CorrelationId: CorrelationId, PendingStatusPollCount: 23),
        };

        TenantMembershipProjectionWaitPolicy.IsPending(snapshot).ShouldBeFalse();
        TenantMembershipProjectionWaitPolicy.Expire(snapshot).ShouldBeSameAs(snapshot);
        if (snapshot is TenantLifecycleCommandSnapshot lifecycle)
        {
            lifecycle.PendingStatusPollCount.ShouldBe(23);
            lifecycle.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        }
    }

    private static TenantRetainedCommandAttempt BeginAccepted(TenantCommandAttemptTracker tracker, object owner,
        string kind, string messageId = MessageId, string correlationId = CorrelationId)
    {
        object request = kind switch
        {
            "add" => TenantAddMemberCommandSnapshot.Idle().RequestSent(
                new(TenantId, "target-user", TenantRole.TenantContributor), "tenant-sequence:1") with { MessageId = messageId },
            "change" => TenantChangeRoleCommandSnapshot.Idle().RequestSent(
                new(TenantId, "target-user", TenantRole.TenantContributor), TenantRole.TenantReader, 2,
                "tenant-sequence:1") with { MessageId = messageId },
            "remove" => TenantRemoveMemberCommandSnapshot.Idle().Previewed(
                new(TenantId, "target-user"), TenantRole.TenantReader, 2, false,
                Detail(TenantId, [new("target-user", TenantRole.TenantReader)]))
                .RequestSent("tenant-sequence:1", attemptStartedAtUtc: DateTimeOffset.UnixEpoch) with { MessageId = messageId },
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
        tracker.TryBegin(TenantId, owner, request, out TenantRetainedCommandAttempt? attempt).ShouldBeTrue();
        tracker.TryStartDelivery(attempt!, owner).ShouldBeTrue();
        TenantCommandSubmissionResult result = TenantCommandSubmissionResult.Accepted(messageId, correlationId);
        object accepted = request switch
        {
            TenantAddMemberCommandSnapshot add => add.Accepted(result),
            TenantChangeRoleCommandSnapshot change => change.Accepted(result),
            TenantRemoveMemberCommandSnapshot remove => remove.Accepted(result),
            _ => throw new InvalidOperationException(),
        };
        tracker.TryCompleteDelivery(attempt!, request, accepted).ShouldBeTrue();
        return attempt!;
    }

    private static TenantRetainedCommandAttempt BeginPending(TenantCommandAttemptTracker tracker, object owner,
        string kind, string messageId = MessageId, string correlationId = CorrelationId)
    {
        TenantRetainedCommandAttempt attempt = BeginAccepted(tracker, owner, kind, messageId, correlationId);
        object accepted = attempt.Snapshot;
        tracker.TryUpdate(attempt, accepted, ApplyStatus(accepted, Completed())).ShouldBeTrue();
        StateOf(attempt.Snapshot).ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        return attempt;
    }

    private static TenantCommandStatusResult Completed()
        => new(CommandStatus.Completed, EventCount: 1, HasVerifiedCommandIdentity: true) { CommittedEventSequence = 2 };

    private static IConfiguration Configuration(string value)
        => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [ConfigurationKey] = value,
        }).Build();

    private static object ApplyStatus(object snapshot, TenantCommandStatusResult status)
        => snapshot switch
        {
            TenantAddMemberCommandSnapshot add => add.ApplyStatus(status),
            TenantChangeRoleCommandSnapshot change => change.ApplyStatus(status),
            TenantRemoveMemberCommandSnapshot remove => remove.ApplyStatus(status),
            _ => throw new ArgumentException("Expected membership snapshot.", nameof(snapshot)),
        };

    private static object Confirm(object snapshot, TenantDetailSnapshot? proof)
        => snapshot switch
        {
            TenantAddMemberCommandSnapshot add => add.ConfirmProjection(proof),
            TenantChangeRoleCommandSnapshot change => change.ConfirmProjection(proof),
            TenantRemoveMemberCommandSnapshot remove => remove.ConfirmProjection(proof),
            _ => throw new ArgumentException("Expected membership snapshot.", nameof(snapshot)),
        };

    private static TenantCommandLifecycleState StateOf(object snapshot)
        => snapshot switch
        {
            TenantAddMemberCommandSnapshot add => add.State,
            TenantChangeRoleCommandSnapshot change => change.State,
            TenantRemoveMemberCommandSnapshot remove => remove.State,
            _ => throw new ArgumentException("Expected membership snapshot.", nameof(snapshot)),
        };

    private static bool IsExpired(object snapshot)
        => snapshot switch
        {
            TenantAddMemberCommandSnapshot add => add.ProjectionWaitExpired,
            TenantChangeRoleCommandSnapshot change => change.ProjectionWaitExpired,
            TenantRemoveMemberCommandSnapshot remove => remove.ProjectionWaitExpired,
            _ => throw new ArgumentException("Expected membership snapshot.", nameof(snapshot)),
        };

    private static TenantDetailSnapshot QualifiedProof(string kind, string version = "tenant-sequence:2")
        => Proof(Detail(TenantId, kind == "remove" ? [] : [new("target-user", TenantRole.TenantContributor)]),
            version)!;

    private static TenantDetail Detail(string tenantId, IReadOnlyList<TenantMember> members)
        => new(tenantId, "Alpha", null, TenantStatus.Active, members, new Dictionary<string, string>(),
            DateTimeOffset.UnixEpoch);

    private static void AssertExpired(object snapshot, object original)
    {
        StateOf(snapshot).ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        IsExpired(snapshot).ShouldBeTrue();
        switch (snapshot, original)
        {
            case (TenantAddMemberCommandSnapshot actual, TenantAddMemberCommandSnapshot before):
                (actual.Intent, actual.MessageId, actual.CorrelationId, actual.BaselineProjectionVersion,
                    actual.HasCommandEventEvidence, actual.AuditState)
                    .ShouldBe((before.Intent, before.MessageId, before.CorrelationId, before.BaselineProjectionVersion,
                        before.HasCommandEventEvidence, before.AuditState));
                actual.SafeMessageKey.ShouldNotBeNullOrWhiteSpace();
                actual.FocusTarget.ShouldBe(TenantCommandFocusTarget.Refresh);
                actual.LiveRegionPoliteness.ShouldBe(TenantCommandLiveRegionPoliteness.Assertive);
                break;
            case (TenantChangeRoleCommandSnapshot actual, TenantChangeRoleCommandSnapshot before):
                (actual.Intent, actual.MessageId, actual.CorrelationId, actual.BaselineProjectionVersion,
                    actual.HasCommandEventEvidence, actual.CurrentConfirmedRole, actual.OwnerCount, actual.AuditState)
                    .ShouldBe((before.Intent, before.MessageId, before.CorrelationId, before.BaselineProjectionVersion,
                        before.HasCommandEventEvidence, before.CurrentConfirmedRole, before.OwnerCount, before.AuditState));
                actual.SafeMessageKey.ShouldNotBeNullOrWhiteSpace();
                actual.FocusTarget.ShouldBe(TenantCommandFocusTarget.Refresh);
                actual.LiveRegionPoliteness.ShouldBe(TenantCommandLiveRegionPoliteness.Assertive);
                break;
            case (TenantRemoveMemberCommandSnapshot actual, TenantRemoveMemberCommandSnapshot before):
                (actual.Intent, actual.MessageId, actual.CorrelationId, actual.BaselineProjectionVersion,
                    actual.HasCommandEventEvidence, actual.CommittedEventSequence, actual.ProjectionProofCallerScope,
                    actual.AuditState, actual.AttemptStartedAtUtc)
                    .ShouldBe((before.Intent, before.MessageId, before.CorrelationId, before.BaselineProjectionVersion,
                        before.HasCommandEventEvidence, before.CommittedEventSequence, before.ProjectionProofCallerScope,
                        before.AuditState, before.AttemptStartedAtUtc));
                actual.SafeMessageKey.ShouldNotBeNullOrWhiteSpace();
                actual.FocusTarget.ShouldBe(TenantCommandFocusTarget.Refresh);
                actual.LiveRegionPoliteness.ShouldBe(TenantCommandLiveRegionPoliteness.Assertive);
                break;
            default:
                throw new ArgumentException("Expected matching membership snapshots.", nameof(snapshot));
        }
    }

    private static void AssertUnresolvedAdmission(TenantCommandAttemptTracker tracker,
        TenantAggregateCommandAdmissionGate gate, TenantRetainedCommandAttempt attempt)
    {
        tracker.HasPendingOwnership(TenantId).ShouldBeTrue();
        TenantCommandAttemptTracker.IsTerminal(attempt.Snapshot).ShouldBeFalse();
        attempt.Lease.IsDispatchMarked.ShouldBeTrue();
        gate.IsOwnedBy(TenantCommandAggregateLock.ForTenant(TenantId), attempt.Lease).ShouldBeTrue();
        attempt.Lease.CurrentOwner.ShouldBeSameAs(attempt.LeaseOwner);
        gate.TryAcquireLease(TenantCommandAggregateLock.ForTenant(TenantId), new(), out _).ShouldBeFalse();
        object otherOwner = new();
        gate.TryAcquireLease(TenantCommandAggregateLock.ForTenant("tenant.other"), otherOwner,
            out TenantAggregateCommandLease? otherLease).ShouldBeTrue();
        otherLease!.TryAbandonBeforeDispatch(otherOwner).ShouldBeTrue();
    }
}
