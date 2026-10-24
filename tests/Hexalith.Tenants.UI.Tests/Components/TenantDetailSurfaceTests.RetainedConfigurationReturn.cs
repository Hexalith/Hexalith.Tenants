using Bunit;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.UI.Components.Tenants.Configuration;
using Hexalith.Tenants.UI.Services.Configuration;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

/// <summary>Qualifies retained command ownership and parent-render safety.</summary>
public sealed partial class TenantDetailSurfaceTests
{
    /// <summary>Re-adopts only the retained tenant set identity without submitting another logical command.</summary>
    /// <param name="accepted">Whether the retained attempt has accepted response evidence.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BusyManagementRemountAdoptsOnlyItsRetainedSetWithoutRedispatch(bool accepted)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var tracker = new TenantSetConfigurationAttemptTracker(() => now);
        TenantSetConfigurationCommandSnapshot retained = RememberSetForManagementReturn(tracker, now, accepted: accepted);
        ITenantCommandGateway gateway = Substitute.For<ITenantCommandGateway>();
        gateway.SupportsTrackedSetConfigurationDispatch.Returns(true);
        gateway.GetStatusAsync(Arg.Any<TenantCommandTrackingHandle>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new TenantCommandStatusResult(CommandStatus.Received, HasVerifiedCommandIdentity: true)));
        Services.AddSingleton(tracker);
        RegisterComponentServices(gateway);
        int acquisitions = 0;
        TenantConfigurationManagementContext context = ConfigurationReturnContext();
        IRenderedComponent<TenantConfigurationManagement> cut = Render<TenantConfigurationManagement>(parameters => parameters
            .Add(p => p.Context, context)
            .Add(p => p.SetEvidence, ManagementEvidence(context, TenantHighImpactAction.SetConfiguration))
            .Add(p => p.RemoveEvidence, ManagementEvidence(context, TenantHighImpactAction.RemoveConfiguration))
            .Add(p => p.IsCommandSurfaceAvailable, false)
            .Add(p => p.SetCommandActivityLease, active =>
            {
                if (active) acquisitions++;
                return Task.FromResult(true);
            }));

        cut.WaitForAssertion(() =>
        {
            SetTenantConfigurationFlow flow = cut.FindComponent<SetTenantConfigurationFlow>().Instance;
            flow.Snapshot.MessageId.ShouldBe(retained.MessageId);
            flow.Snapshot.Intent.ShouldBe(retained.Intent);
            flow.Snapshot.State.ShouldBe(accepted ? TenantCommandLifecycleState.Accepted : TenantCommandLifecycleState.RequestSent);
            acquisitions.ShouldBe(1);
            gateway.Received(accepted ? 1 : 0).GetStatusAsync(
                Arg.Is<TenantCommandTrackingHandle>(handle => handle.MessageId == retained.MessageId
                    && handle.CorrelationId == retained.CorrelationId && handle.AggregateId == context.TenantId),
                Arg.Any<CancellationToken>());
        });
        gateway.DidNotReceive().SetTenantConfigurationTrackedAsync(
            Arg.Any<SetTenantConfiguration>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        cut.FindAll("[data-testid='tenants-config-management-unavailable']").ShouldBeEmpty();
    }

    /// <summary>Keeps unavailable management closed without matching live retention.</summary>
    /// <param name="retention">The retained snapshot shape to exercise.</param>
    [Theory]
    [InlineData("none")]
    [InlineData("another-tenant")]
    [InlineData("expired")]
    public void BusyManagementDoesNotMountSetWithoutLiveMatchingRetention(string retention)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var tracker = new TenantSetConfigurationAttemptTracker(() => now);
        if (retention != "none")
        {
            _ = RememberSetForManagementReturn(tracker, now, retention == "another-tenant" ? "tenant.beta" : "tenant.alpha");
            if (retention == "expired") now += TenantSetConfigurationCommandSnapshot.MaximumRetainedAttemptDuration;
        }
        Services.AddSingleton(tracker);
        RegisterComponentServices();
        TenantConfigurationManagementContext context = ConfigurationReturnContext();
        int acquisitions = 0;
        IRenderedComponent<TenantConfigurationManagement> cut = Render<TenantConfigurationManagement>(parameters => parameters
            .Add(p => p.Context, context)
            .Add(p => p.SetEvidence, ManagementEvidence(context, TenantHighImpactAction.SetConfiguration))
            .Add(p => p.RemoveEvidence, ManagementEvidence(context, TenantHighImpactAction.RemoveConfiguration))
            .Add(p => p.IsCommandSurfaceAvailable, false)
            .Add(p => p.SetCommandActivityLease, active =>
            {
                if (active) acquisitions++;
                return Task.FromResult(true);
            }));

        cut.FindComponents<SetTenantConfigurationFlow>().ShouldBeEmpty();
        cut.Find("[data-testid='tenants-config-management-unavailable']");
        acquisitions.ShouldBe(0);
    }

    /// <summary>Restores the surviving heading when refused retained adoption expires.</summary>
    [Fact]
    public void RefusedSetAdoptionExpiresWithoutAParameterRefreshOrAcceptedActivityCallback()
    {
        var time = new ManualTimeProvider();
        time.Advance(DateTimeOffset.UtcNow - DateTimeOffset.UnixEpoch);
        var tracker = new TenantSetConfigurationAttemptTracker(time.GetUtcNow);
        _ = RememberSetForManagementReturn(tracker, time.GetUtcNow());
        Services.AddSingleton<TimeProvider>(time);
        Services.AddSingleton(tracker);
        RegisterComponentServices();
        TenantConfigurationManagementContext context = ConfigurationReturnContext();
        List<bool> activity = [];
        IRenderedComponent<TenantConfigurationManagement> cut = Render<TenantConfigurationManagement>(parameters => parameters
            .Add(p => p.Context, context)
            .Add(p => p.SetEvidence, ManagementEvidence(context, TenantHighImpactAction.SetConfiguration))
            .Add(p => p.RemoveEvidence, ManagementEvidence(context, TenantHighImpactAction.RemoveConfiguration))
            .Add(p => p.IsCommandSurfaceAvailable, false)
            .Add(p => p.SetCommandActivityLease, active =>
            {
                activity.Add(active);
                return Task.FromResult(!active);
            }));
        cut.FindComponent<SetTenantConfigurationFlow>();
        activity.ShouldBe([true]);
        int focusCalls = FocusedElementIds().Count;
        time.Advance(TenantSetConfigurationCommandSnapshot.MaximumRetainedAttemptDuration);

        cut.WaitForAssertion(() =>
        {
            cut.FindComponents<SetTenantConfigurationFlow>().ShouldBeEmpty();
            cut.Find("[data-testid='tenants-config-management-unavailable']");
        }, TimeSpan.FromSeconds(5));
        activity.ShouldBe([true]);
        tracker.Find(context.TenantId).ShouldBeNull();
        FocusedElementIds().Count.ShouldBe(focusCalls + 1);
        FocusedElementIds()[^1].ShouldBe(CapturedElementReferenceId(cut.Instance, "_headingElement"));
    }

    /// <summary>Preserves replacement ownership and schedules its own expiry without borrowing focus.</summary>
    [Fact]
    public void SetRetentionDeadlineRearmsForARefusedReplacementWithoutBorrowingItsFocusOrIdentity()
    {
        var time = new ManualTimeProvider();
        time.Advance(DateTimeOffset.UtcNow - DateTimeOffset.UnixEpoch);
        var tracker = new TenantSetConfigurationAttemptTracker(time.GetUtcNow);
        TenantSetConfigurationCommandSnapshot first = RememberSetForManagementReturn(tracker, time.GetUtcNow());
        Services.AddSingleton<TimeProvider>(time);
        Services.AddSingleton(tracker);
        RegisterComponentServices();
        TenantConfigurationManagementContext context = ConfigurationReturnContext();
        IRenderedComponent<TenantConfigurationManagement> cut = Render<TenantConfigurationManagement>(parameters => parameters
            .Add(p => p.Context, context)
            .Add(p => p.SetEvidence, ManagementEvidence(context, TenantHighImpactAction.SetConfiguration))
            .Add(p => p.RemoveEvidence, ManagementEvidence(context, TenantHighImpactAction.RemoveConfiguration))
            .Add(p => p.IsCommandSurfaceAvailable, false)
            .Add(p => p.SetCommandActivityLease, active => Task.FromResult(!active)));
        cut.FindComponent<SetTenantConfigurationFlow>().Instance.Snapshot.MessageId.ShouldBe(first.MessageId);
        int focusCalls = FocusedElementIds().Count;
        tracker.Forget(context.TenantId, first.MessageId!);
        time.Advance(TimeSpan.FromMinutes(1));
        TenantSetConfigurationCommandSnapshot replacement = RememberSetForManagementReturn(tracker, time.GetUtcNow());
        time.Advance(TimeSpan.FromMinutes(4));

        cut.WaitForAssertion(() =>
        {
            cut.FindComponent<SetTenantConfigurationFlow>().Instance.Snapshot.MessageId.ShouldBe(replacement.MessageId);
            tracker.Find(context.TenantId).ShouldNotBeNull().MessageId.ShouldBe(replacement.MessageId);
        }, TimeSpan.FromSeconds(5));
        FocusedElementIds().Count.ShouldBe(focusCalls);
        time.Advance(TimeSpan.FromMinutes(1));
        cut.WaitForAssertion(() => cut.FindComponents<SetTenantConfigurationFlow>().ShouldBeEmpty(), TimeSpan.FromSeconds(5));
        FocusedElementIds().Count.ShouldBe(focusCalls + 1);
        FocusedElementIds()[^1].ShouldBe(CapturedElementReferenceId(cut.Instance, "_headingElement"));
    }

    /// <summary>Cancels obsolete expiry work on a tenant transition or component disposal.</summary>
    /// <param name="dispose">Whether the component is disposed instead of changing tenants.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SetRetentionWakeupIsCanceledWhenItsTenantOrComponentLeaves(bool dispose)
    {
        var time = new ManualTimeProvider();
        time.Advance(DateTimeOffset.UtcNow - DateTimeOffset.UnixEpoch);
        var tracker = new TenantSetConfigurationAttemptTracker(time.GetUtcNow);
        _ = RememberSetForManagementReturn(tracker, time.GetUtcNow());
        Services.AddSingleton<TimeProvider>(time);
        Services.AddSingleton(tracker);
        RegisterComponentServices();
        TenantConfigurationManagementContext context = ConfigurationReturnContext();
        IRenderedComponent<TenantConfigurationManagement> cut = Render<TenantConfigurationManagement>(parameters => parameters
            .Add(p => p.Context, context)
            .Add(p => p.SetEvidence, ManagementEvidence(context, TenantHighImpactAction.SetConfiguration))
            .Add(p => p.RemoveEvidence, ManagementEvidence(context, TenantHighImpactAction.RemoveConfiguration))
            .Add(p => p.IsCommandSurfaceAvailable, false)
            .Add(p => p.SetCommandActivityLease, active => Task.FromResult(!active)));
        cut.FindComponent<SetTenantConfigurationFlow>();
        time.PendingTimerCount.ShouldBe(1);
        if (dispose)
        {
            await DisposeComponentsAsync();
        }
        else
        {
            TenantConfigurationManagementContext next = TenantConfigurationManagementContext.Available(
                "tenant.beta", TenantStatus.Active, false, ["billing"], [], TenantConfigurationAuthorityState.TenantOwner);
            cut.Render(parameters => parameters
                .Add(p => p.Context, next)
                .Add(p => p.SetEvidence, ManagementEvidence(next, TenantHighImpactAction.SetConfiguration))
                .Add(p => p.RemoveEvidence, ManagementEvidence(next, TenantHighImpactAction.RemoveConfiguration)));
        }
        time.PendingTimerCount.ShouldBe(0);
        int focusCalls = FocusedElementIds().Count;
        time.Advance(TenantSetConfigurationCommandSnapshot.MaximumRetainedAttemptDuration);
        await Renderer.Dispatcher.InvokeAsync(() => { });
        FocusedElementIds().Count.ShouldBe(focusCalls);
        Renderer.UnhandledException.IsCompleted.ShouldBeFalse();
    }

    private static TenantConfigurationManagementContext ConfigurationReturnContext()
        => TenantConfigurationManagementContext.Available(
            "tenant.alpha", TenantStatus.Active, false, ["billing"],
            [new TenantConfigurationSafeRow("billing", "billing.mode", "trial")],
            TenantConfigurationAuthorityState.TenantOwner);

    private static TenantSetConfigurationCommandSnapshot RememberSetForManagementReturn(
        TenantSetConfigurationAttemptTracker tracker,
        DateTimeOffset started,
        string tenantId = "tenant.alpha",
        bool accepted = true)
    {
        var intent = new TenantSetConfigurationIntent(tenantId, "billing", "mode", "billing.mode", "fingerprint");
        TenantSetConfigurationPreview preview = TenantSetConfigurationPreview.Create(
            intent, TenantStatus.Active, TenantSetConfigurationCurrentState.Different,
            ReadModelFreshnessState.Current, ProjectionLifecycleState.Current, "tenant-sequence:41", true);
        var identity = tracker.BeginDispatch(intent, "tenant-sequence:41", started);
        TenantSetConfigurationCommandSnapshot retained = TenantSetConfigurationCommandSnapshot.Idle().Previewed(preview)
            .RequestSent(preview, identity.MessageId, identity.AttemptStartedAtUtc);
        if (accepted) retained = retained.Accepted(TenantCommandSubmissionResult.Accepted(identity.MessageId, identity.MessageId));
        tracker.Remember(retained).ShouldBeTrue();
        return retained;
    }
}
