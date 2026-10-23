using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.UI.Extensions;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantCommands;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class TenantCorrectionAttemptCallerTests
{
    [Fact]
    public void ProductionCompositionBindsRetainedVisibilityWithoutReleasingTheExistingLease()
    {
        TenantCorrectionAuthenticationStateProvider authentication = new();
        ServiceCollection registrations = new();
        registrations.AddLogging();
        registrations.AddSingleton<AuthenticationStateProvider>(authentication);
        registrations.AddHexalithTenantsUiModule(new ConfigurationBuilder().Build(), enableGatewayAuthorization: false);
        using ServiceProvider services = registrations.BuildServiceProvider();
        using IServiceScope scope = services.CreateScope();
        TenantCorrectionAttemptTracker tracker = scope.ServiceProvider.GetRequiredService<TenantCorrectionAttemptTracker>();
        TenantAggregateCommandAdmissionGate gate = scope.ServiceProvider.GetRequiredService<TenantAggregateCommandAdmissionGate>();
        TenantCorrectionPreviewSnapshot preview = Preview();
        tracker.TryBegin(preview, gate, out TenantCorrectionAttempt? attempt).ShouldBeTrue();
        tracker.Find(preview.TenantId)!.MessageId.ShouldBe(attempt!.MessageId);

        authentication.Change(Task.FromResult(TenantCorrectionAuthenticationStateProvider.State("operator.beta")));

        tracker.Find(preview.TenantId).ShouldBeNull();
        tracker.TryBegin(preview, gate, out TenantCorrectionAttempt? blocked).ShouldBeFalse();
        blocked.ShouldBeNull();
        tracker.TryStartRetry(preview.TenantId, attempt.MessageId).ShouldBeFalse();
        gate.IsLocked(TenantCommandAggregateLock.ForTenant(preview.TenantId)).ShouldBeTrue();
        authentication.Change(Task.FromResult(TenantCorrectionAuthenticationStateProvider.State("operator.alpha")));
        tracker.Find(preview.TenantId)!.MessageId.ShouldBe(attempt.MessageId);
    }

    [Theory]
    [InlineData("operator.beta")]
    [InlineData("Operator.alpha")]
    [InlineData(null)]
    public void CallerChangeHidesFactsWithoutReleasingUnresolvedAdmission(string? subject)
    {
        TenantCorrectionAuthenticationStateProvider authentication = new();
        using TenantCorrectionAttemptTracker tracker = new(authentication, static () => DateTimeOffset.UtcNow);
        TenantAggregateCommandAdmissionGate gate = new();
        TenantCorrectionPreviewSnapshot preview = Preview();
        tracker.TryBegin(preview, gate, out TenantCorrectionAttempt? attempt).ShouldBeTrue();
        tracker.EndDelivery(preview.TenantId, attempt!.MessageId, attempt.Snapshot);
        bool invalidationWasPublished = false;
        tracker.StateChanged += (_, _) => invalidationWasPublished |= tracker.Find(preview.TenantId) is null;

        authentication.Change(Task.FromResult(TenantCorrectionAuthenticationStateProvider.State(subject)));

        invalidationWasPublished.ShouldBeTrue();
        tracker.Find(preview.TenantId).ShouldBeNull();
        tracker.TryStartRetry(preview.TenantId, attempt.MessageId).ShouldBeFalse();
        tracker.TryBegin(preview, gate, out TenantCorrectionAttempt? blocked).ShouldBeFalse();
        blocked.ShouldBeNull();
        gate.IsLocked(TenantCommandAggregateLock.ForTenant(preview.TenantId)).ShouldBeTrue();

        // Exact owner completion still retires the hidden lease; redaction cannot strand delivery.
        tracker.TryUpdate(preview.TenantId, attempt.MessageId, attempt.Snapshot with
        {
            LifecycleState = TenantCommandLifecycleState.Rejected,
        }).ShouldBeTrue();
        gate.IsLocked(TenantCommandAggregateLock.ForTenant(preview.TenantId)).ShouldBeFalse();
        tracker.Find(preview.TenantId).ShouldBeNull();
        authentication.Change(Task.FromResult(TenantCorrectionAuthenticationStateProvider.State("operator.alpha")));
        tracker.Find(preview.TenantId)!.Snapshot.LifecycleState.ShouldBe(TenantCommandLifecycleState.Rejected);
        tracker.Find(preview.TenantId)!.MessageId.ShouldBe(attempt.MessageId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PendingOrFaultedCallerCannotAdmitOrRestoreAnObsoleteIdentity(bool fail)
    {
        TenantCorrectionAuthenticationStateProvider authentication = new();
        using TenantCorrectionAttemptTracker tracker = new(authentication, static () => DateTimeOffset.UtcNow);
        TenantAggregateCommandAdmissionGate gate = new();
        TenantCorrectionPreviewSnapshot preview = Preview();
        tracker.TryBegin(preview, gate, out TenantCorrectionAttempt? attempt).ShouldBeTrue();
        TaskCompletionSource<AuthenticationState> pending = new();

        authentication.Change(pending.Task);

        tracker.Find(preview.TenantId).ShouldBeNull();
        tracker.TryBegin(preview with { TenantId = "tenant.beta" }, gate, out _).ShouldBeFalse();
        authentication.Change(Task.FromResult(TenantCorrectionAuthenticationStateProvider.State("operator.beta")));
        if (fail) pending.SetException(new InvalidOperationException("Identity unavailable"));
        else pending.SetResult(TenantCorrectionAuthenticationStateProvider.State("operator.alpha"));
        // This source runs the old identity continuation inline, making obsolete completion deterministic.
        tracker.Find(preview.TenantId).ShouldBeNull();
        gate.IsLocked(TenantCommandAggregateLock.ForTenant(preview.TenantId)).ShouldBeTrue();
        authentication.Change(Task.FromResult(TenantCorrectionAuthenticationStateProvider.State("operator.alpha")));
        tracker.Find(preview.TenantId)!.MessageId.ShouldBe(attempt!.MessageId);
    }

    [Fact]
    public void SameSubjectNotificationPreservesTheOriginalAttemptAndRetryIdentity()
    {
        TenantCorrectionAuthenticationStateProvider authentication = new();
        using TenantCorrectionAttemptTracker tracker = new(authentication, static () => DateTimeOffset.UtcNow);
        TenantAggregateCommandAdmissionGate gate = new();
        TenantCorrectionPreviewSnapshot preview = Preview();
        tracker.TryBegin(preview, gate, out TenantCorrectionAttempt? attempt).ShouldBeTrue();
        tracker.EndDelivery(preview.TenantId, attempt!.MessageId, attempt.Snapshot);

        authentication.Change(Task.FromResult(TenantCorrectionAuthenticationStateProvider.State("operator.alpha")));

        tracker.Find(preview.TenantId)!.MessageId.ShouldBe(attempt.MessageId);
        tracker.Find(preview.TenantId)!.Snapshot.Intent.ShouldBe(attempt.Snapshot.Intent);
        tracker.TryStartRetry(preview.TenantId, attempt.MessageId).ShouldBeTrue();
        gate.IsLocked(TenantCommandAggregateLock.ForTenant(preview.TenantId)).ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MissingOrFaultedInitialIdentityFailsClosed(bool faulted)
    {
        TenantCorrectionAuthenticationStateProvider? authentication = faulted ? new() : null;
        authentication?.Change(Task.FromException<AuthenticationState>(new InvalidOperationException("Identity unavailable")));
        using TenantCorrectionAttemptTracker tracker = new(authentication, static () => DateTimeOffset.UtcNow);
        TenantAggregateCommandAdmissionGate gate = new();
        TenantCorrectionPreviewSnapshot preview = Preview();

        tracker.TryBegin(preview, gate, out TenantCorrectionAttempt? attempt).ShouldBeFalse();
        attempt.ShouldBeNull();
        gate.IsLocked(TenantCommandAggregateLock.ForTenant(preview.TenantId)).ShouldBeFalse();
    }

    [Fact]
    public void HiddenAttemptExpiryKeepsTheEstablishedDeadlineAndAllowsFreshCallerAdmission()
    {
        DateTimeOffset now = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);
        TenantCorrectionAuthenticationStateProvider authentication = new();
        using TenantCorrectionAttemptTracker tracker = new(authentication, () => now);
        TenantAggregateCommandAdmissionGate gate = new();
        TenantCorrectionPreviewSnapshot preview = Preview();
        tracker.TryBegin(preview, gate, out TenantCorrectionAttempt? attempt).ShouldBeTrue();
        authentication.Change(Task.FromResult(TenantCorrectionAuthenticationStateProvider.State("operator.beta")));
        now += TenantLifecycleCommandSnapshot.MaximumRetainedAttemptDuration;

        tracker.Find(preview.TenantId).ShouldBeNull();
        gate.IsLocked(TenantCommandAggregateLock.ForTenant(preview.TenantId)).ShouldBeFalse();
        tracker.TryBegin(preview, gate, out TenantCorrectionAttempt? replacement).ShouldBeTrue();
        replacement!.MessageId.ShouldNotBe(attempt!.MessageId);
        replacement.CallerSubject.ShouldBe("operator.beta");
        tracker.TryUpdate(preview.TenantId, attempt.MessageId, attempt.Snapshot).ShouldBeFalse();
    }

    private static TenantCorrectionPreviewSnapshot Preview()
    {
        TenantCorrectionProjection projection = new("tenant.alpha", "target-user", TenantStatus.Active,
            null, false, true, false, true, ReadModelFreshnessState.Current,
            ProjectionLifecycleState.Current, QueryResponseProvenance.ProjectionBacked)
        { ProjectionVersion = "tenant-sequence:1", OwnerCount = 1 };
        TenantCorrectionStartIntent intent = new("original-reference", "tenant.alpha", "target-user",
            "UserRemovedFromTenant", "tenant-projection-current", TenantCorrectionCommandDomain.Tenants,
            TenantCorrectionCommandType.AddUserToTenant, TenantRole.TenantReader, [],
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["tenantId"] = "tenant.alpha", ["userId"] = "target-user",
                ["originalTimestamp"] = "2026-10-23T09:00:00.0000000+00:00",
                ["intendedRole"] = "TenantReader",
            }, projection);
        return TenantCorrectionPreviewSnapshot.FromIntent(intent);
    }
}
