using System.Diagnostics;

using Bunit;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Shell.State.Navigation;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Identity;
using Hexalith.Tenants.UI.Tests.State;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Pages;
using Hexalith.Tenants.UI.Components.Tenants.Members;
using Hexalith.Tenants.UI.Services;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantUsers;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed partial class TenantDetailSurfaceTests
{
    /// <summary>An early notification survives child attachment and delayed membership authority.</summary>
    [Theory]
    [InlineData("add", true, false, false, "ready", true)]
    [InlineData("add", false, false, false, "ready", true)]
    [InlineData("change", true, false, false, "ready", true)]
    [InlineData("change", false, false, false, "ready", true)]
    [InlineData("remove", true, false, false, "ready", true)]
    [InlineData("remove", false, false, false, "ready", true)]
    [InlineData("add", true, true, false, "ready", true)]
    [InlineData("add", false, true, false, "ready", true)]
    [InlineData("change", true, true, false, "ready", true)]
    [InlineData("change", false, true, false, "ready", true)]
    [InlineData("remove", true, true, false, "ready", true)]
    [InlineData("remove", false, true, false, "ready", true)]
    [InlineData("add", true, true, true, "ready", true)]
    [InlineData("add", false, true, true, "ready", true)]
    [InlineData("add", true, false, false, "stale", true)]
    [InlineData("add", true, false, false, "stale", false)]
    [InlineData("add", true, false, false, "degraded", true)]
    [InlineData("add", true, false, false, "degraded", false)]
    public async Task EarlyMembershipNudgeReconcilesAfterChildAttachment(
        string kind, bool projectionAdvanced, bool holdMembershipAuthority, bool secondNotification, string surface, bool authoritativeProof)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        const string TenantId = "tenant.alpha";
        const string MessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        const string CorrelationId = "correlation-original";
        var initialDetail = new TaskCompletionSource<TenantDetailSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        var initialReadReturned = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var authority = new TaskCompletionSource<TenantMutationAuthorizationReflection>(TaskCreationOptions.RunContinuationsAsynchronously);
        var authorityEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var composition = new StubTenantsBffComposition
        {
            MutationResolver = (_, _, _) =>
            {
                authorityEntered.TrySetResult();
                return holdMembershipAuthority
                    ? new(authority.Task)
                    : ValueTask.FromResult(new TenantMutationAuthorizationReflection(
                        TenantLifecycleAuthorizationReflectionState.Authorized,
                        TenantLifecycleAuthorizationReflectionState.Authorized));
            },
        };
        ITenantCommandGateway commandGateway = Substitute.For<ITenantCommandGateway>();
        int statusReads = 0;
        commandGateway.GetStatusAsync(Arg.Any<TenantCommandTrackingHandle>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                TenantCommandTrackingHandle handle = call.ArgAt<TenantCommandTrackingHandle>(0);
                handle.MessageId.ShouldBe(MessageId);
                handle.CorrelationId.ShouldBe(CorrelationId);
                handle.AggregateId.ShouldBe(TenantId);
                handle.ExpectedDomain.ShouldBe(TenantIdentity.Domain);
                call.ArgAt<CancellationToken>(1).ShouldBe(CancellationToken.None);
                Interlocked.Increment(ref statusReads);
                return Task.FromResult(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1, HasVerifiedCommandIdentity: true)
                {
                    CommittedEventSequence = 2,
                });
            });
        IProjectionSubscription backendSubscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        backendSubscription.SubscribeAsync("tenants", TenantId, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        RegisterMembershipPageServices(commandGateway, composition);
        Services.AddSingleton(backendSubscription);
        Services.AddSingleton(notifier);
        Services.AddScoped<TenantReadRefreshSubscription>();
        ITenantQueryGateway gateway = Services.GetRequiredService<ITenantQueryGateway>();
        TenantCommandAttemptTracker tracker = Services.GetRequiredService<TenantCommandAttemptTracker>();
        TenantAggregateCommandAdmissionGate admission = Services.GetRequiredService<TenantAggregateCommandAdmissionGate>();
        TenantDetail baseline = Detail(TenantId) with { Name = "Initial held detail" };
        TenantDetail refreshed = EarlyMembershipResult(baseline, kind) with { Name = "Notification refreshed detail" };
        string proofVersion = projectionAdvanced ? "tenant-sequence:2" : "tenant-sequence:1";
        TenantDetailSnapshot currentDetail = ReadyWithSafeConfiguration(refreshed, ProjectionLifecycleState.Current, proofVersion);
        TenantDetailSnapshot notificationDetail = surface switch
        {
            "stale" => TenantDetailSnapshot.Stale(refreshed, null, ProjectionLifecycleState.Current, proofVersion),
            "degraded" => TenantDetailSnapshot.Degraded(refreshed, "Safe degraded detail.", lifecycle: ProjectionLifecycleState.Current, projectionVersion: proofVersion),
            _ => currentDetail,
        };
        TenantDetailSnapshot commandProof = authoritativeProof ? currentDetail
            : TenantDetailSnapshot.Stale(refreshed, null, ProjectionLifecycleState.Current, proofVersion);
        notificationDetail = notificationDetail.WithCallerScope(currentDetail.CallerScope.ShouldNotBeNull());
        commandProof = commandProof.WithCallerScope(currentDetail.CallerScope.ShouldNotBeNull());
        TenantUsersSnapshot currentMembers = MemberSnapshot(refreshed) with { ProjectionVersion = proofVersion };
        int detailReads = 0;
        int memberReads = 0;
        CancellationToken initialToken = default;
        gateway.GetTenantAsync(Arg.Any<TenantDetailRequest>(), Arg.Any<TenantDetailSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                int read = Interlocked.Increment(ref detailReads);
                if (read == 1)
                {
                    initialToken = call.ArgAt<CancellationToken>(2);
                    TenantDetailSnapshot result = await initialDetail.Task;
                    initialReadReturned.TrySetResult();
                    return result;
                }
                return read == 2 || secondNotification && read == 3 ? notificationDetail : commandProof;
            });
        gateway.GetTenantUsersAsync(Arg.Any<TenantUsersRequest>(), Arg.Any<TenantUsersSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(Interlocked.Increment(ref memberReads) == 1
                ? MemberSnapshot(baseline) with { ProjectionVersion = "tenant-sequence:1" }
                : currentMembers));
        object original = EarlyMembershipAcceptedAttempt(baseline, kind, MessageId, CorrelationId);
        object oldOwner = new();
        tracker.TryBegin(TenantId, oldOwner, original, out TenantRetainedCommandAttempt? attempt).ShouldBeTrue();
        tracker.TryStartDelivery(attempt!, oldOwner).ShouldBeTrue();
        tracker.TryCompleteDelivery(attempt!, original, original).ShouldBeTrue();
        tracker.Detach(attempt, oldOwner);

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters.Add(page => page.TenantId, TenantId));
        try
        {
            cut.WaitForElement("[data-testid='tenants-detail-loading']");
            cut.WaitForAssertion(() => backendSubscription.ReceivedCalls()
                .Count(call => call.GetMethodInfo().Name == nameof(IProjectionSubscription.SubscribeAsync)).ShouldBe(1));
            cut.FindComponents<MemberAccessReview>().ShouldBeEmpty();
            DetailField(cut, "_memberAccessReview").ShouldBeNull();
            admission.IsLocked(TenantCommandAggregateLock.ForTenant(TenantId)).ShouldBeTrue();

            await cut.InvokeAsync(() => notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>("tenants", TenantId));
            cut.WaitForElement("[data-testid='tenants-member-section']");
            await authorityEntered.Task.WaitAsync(DefaultWaitTimeout);
            if (holdMembershipAuthority)
            {
                cut.FindComponent<MemberAccessReview>().Instance.IsAddMemberAuthorized.ShouldBeFalse();
                Volatile.Read(ref statusReads).ShouldBe(0);
                admission.IsLocked(TenantCommandAggregateLock.ForTenant(TenantId)).ShouldBeTrue();
                if (secondNotification)
                {
                    await cut.InvokeAsync(() => notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>("tenants", TenantId));
                    cut.WaitForAssertion(() => Volatile.Read(ref detailReads).ShouldBe(3));
                    Volatile.Read(ref statusReads).ShouldBe(0);
                    cut.FindComponent<MemberAccessReview>().Instance.IsAddMemberAuthorized.ShouldBeFalse();
                }
                authority.SetResult(new(TenantLifecycleAuthorizationReflectionState.Authorized,
                    TenantLifecycleAuthorizationReflectionState.Authorized));
            }

            TenantCommandLifecycleState expected = projectionAdvanced && authoritativeProof
                ? TenantCommandLifecycleState.Confirmed
                : TenantCommandLifecycleState.ProjectionPending;
            cut.WaitForAssertion(() =>
            {
                EarlyMembershipState(cut, kind).State.ShouldBe(expected);
                Volatile.Read(ref statusReads).ShouldBe(1);
                Volatile.Read(ref detailReads).ShouldBe(secondNotification ? 4 : 3,
                    "each notification read and the separate command proof run");
                admission.IsLocked(TenantCommandAggregateLock.ForTenant(TenantId)).ShouldBe(!(projectionAdvanced && authoritativeProof));
            });
            (TenantCommandLifecycleState _, string? messageId, string? correlationId) = EarlyMembershipState(cut, kind);
            messageId.ShouldBe(MessageId);
            correlationId.ShouldBe(CorrelationId);
            tracker.HasPendingOwnership(TenantId).ShouldBe(!(projectionAdvanced && authoritativeProof));
            AssertEarlyMembershipNoDispatch(commandGateway);
            initialToken.IsCancellationRequested.ShouldBeTrue();

            // The superseded initial response deliberately ignores cancellation and arrives last.
            initialDetail.SetResult(ReadyWithSafeConfiguration(baseline, ProjectionLifecycleState.Current, "tenant-sequence:1"));
            await initialReadReturned.Task.WaitAsync(DefaultWaitTimeout);
            await WaitForEarlyMembershipTokenDisposalAsync(initialToken);
            ((TenantDetailSnapshot)DetailField(cut, "_snapshot")!).Detail!.Name.ShouldBe(refreshed.Name);
            cut.FindComponent<MemberAccessReview>().Instance.Detail.Name.ShouldBe(refreshed.Name);
            EarlyMembershipState(cut, kind).State.ShouldBe(expected);
            EarlyMembershipState(cut, kind).MessageId.ShouldBe(MessageId);
            EarlyMembershipState(cut, kind).CorrelationId.ShouldBe(CorrelationId);
            Volatile.Read(ref statusReads).ShouldBe(1);
            Volatile.Read(ref detailReads).ShouldBe(secondNotification ? 4 : 3);
            AssertEarlyMembershipNoDispatch(commandGateway);
            Renderer.UnhandledException.IsCompleted.ShouldBeFalse();
        }
        finally
        {
            authority.TrySetResult(new(TenantLifecycleAuthorizationReflectionState.Authorized,
                TenantLifecycleAuthorizationReflectionState.Authorized));
            initialDetail.TrySetResult(ReadyWithSafeConfiguration(baseline, ProjectionLifecycleState.Current, "tenant-sequence:1"));
            await initialReadReturned.Task.WaitAsync(DefaultWaitTimeout);
        }
    }

    /// <summary>A pending early nudge cannot cross route or disposal boundaries.</summary>
    [Theory]
    [InlineData("add", "route")]
    [InlineData("change", "route")]
    [InlineData("remove", "route")]
    [InlineData("add", "dispose")]
    [InlineData("change", "dispose")]
    [InlineData("remove", "dispose")]
    public async Task EarlyMembershipNudgeDiscardsObsoleteAuthorization(string kind, string transition)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        const string TenantId = "tenant.alpha";
        const string MessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        const string CorrelationId = "correlation-original";
        var initialDetail = new TaskCompletionSource<TenantDetailSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        var initialReadReturned = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var authority = new TaskCompletionSource<TenantMutationAuthorizationReflection>(TaskCreationOptions.RunContinuationsAsynchronously);
        var authorityEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var authorityReturned = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken authorityToken = default;
        var composition = new StubTenantsBffComposition
        {
            MutationResolver = (tenantId, _, token) => tenantId == TenantId
                ? new(HoldAuthorityAsync(token))
                : ValueTask.FromResult(new TenantMutationAuthorizationReflection(
                    TenantLifecycleAuthorizationReflectionState.Authorized,
                    TenantLifecycleAuthorizationReflectionState.Authorized)),
        };
        var commandGateway = new TrackingMembershipCommandGateway(
            Task.FromResult(TenantCommandSubmissionResult.Failed("Unexpected redispatch.")),
            new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1, HasVerifiedCommandIdentity: true)
            {
                CommittedEventSequence = 2,
            });
        IProjectionSubscription backendSubscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        backendSubscription.SubscribeAsync("tenants", Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        RegisterMembershipPageServices(commandGateway, composition);
        Services.AddSingleton(backendSubscription);
        Services.AddSingleton(notifier);
        Services.AddScoped<TenantReadRefreshSubscription>();
        ITenantQueryGateway gateway = Services.GetRequiredService<ITenantQueryGateway>();
        TenantCommandAttemptTracker tracker = Services.GetRequiredService<TenantCommandAttemptTracker>();
        TenantAggregateCommandAdmissionGate admission = Services.GetRequiredService<TenantAggregateCommandAdmissionGate>();
        TenantDetail baseline = Detail(TenantId);
        TenantDetail refreshed = EarlyMembershipResult(baseline, kind);
        int alphaReads = 0;
        CancellationToken initialToken = default;
        gateway.GetTenantAsync(Arg.Any<TenantDetailRequest>(), Arg.Any<TenantDetailSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                string tenantId = call.ArgAt<TenantDetailRequest>(0).TenantId;
                if (tenantId == TenantId && Interlocked.Increment(ref alphaReads) == 1)
                {
                    initialToken = call.ArgAt<CancellationToken>(2);
                    TenantDetailSnapshot result = await initialDetail.Task;
                    initialReadReturned.TrySetResult();
                    return result;
                }
                return ReadyWithSafeConfiguration(tenantId == TenantId ? refreshed : Detail(tenantId),
                    ProjectionLifecycleState.Current, "tenant-sequence:2");
            });
        gateway.GetTenantUsersAsync(Arg.Any<TenantUsersRequest>(), Arg.Any<TenantUsersSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(MemberSnapshot(call.ArgAt<TenantUsersRequest>(0).TenantId == TenantId
                ? refreshed : Detail(call.ArgAt<TenantUsersRequest>(0).TenantId)) with
            { ProjectionVersion = "tenant-sequence:2" }));
        object original = EarlyMembershipAcceptedAttempt(baseline, kind, MessageId, CorrelationId);
        object oldOwner = new();
        tracker.TryBegin(TenantId, oldOwner, original, out TenantRetainedCommandAttempt? attempt).ShouldBeTrue();
        tracker.TryStartDelivery(attempt!, oldOwner).ShouldBeTrue();
        tracker.TryCompleteDelivery(attempt!, original, original).ShouldBeTrue();
        tracker.Detach(attempt, oldOwner);
        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters.Add(page => page.TenantId, TenantId));
        try
        {
            cut.WaitForElement("[data-testid='tenants-detail-loading']");
            cut.WaitForAssertion(() => backendSubscription.ReceivedCalls()
                .Count(call => call.GetMethodInfo().Name == nameof(IProjectionSubscription.SubscribeAsync)).ShouldBe(1));
            DetailField(cut, "_memberAccessReview").ShouldBeNull();
            await cut.InvokeAsync(() => notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>("tenants", TenantId));
            cut.WaitForElement("[data-testid='tenants-member-section']");
            await authorityEntered.Task.WaitAsync(DefaultWaitTimeout);
            cut.FindComponent<MemberAccessReview>().Instance.IsAddMemberAuthorized.ShouldBeFalse();
            commandGateway.StatusCallCount.ShouldBe(0);
            Volatile.Read(ref alphaReads).ShouldBe(2);

            int renderCount;
            if (transition == "route")
            {
                cut.Render(parameters => parameters.Add(page => page.TenantId, "tenant.beta"));
                cut.WaitForAssertion(() => cut.FindComponent<MemberAccessReview>().Instance.Detail.TenantId.ShouldBe("tenant.beta"));
                cut.FindComponents<ChangeTenantMemberRoleFlow>().ShouldBeEmpty();
                cut.FindComponents<RemoveTenantMemberFlow>().ShouldBeEmpty();
                cut.FindComponent<AddTenantMemberFlow>().Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Idle);
                admission.IsLocked(TenantCommandAggregateLock.ForTenant("tenant.beta")).ShouldBeFalse();
                renderCount = cut.RenderCount;
            }
            else
            {
                await DisposeComponentsAsync().WaitAsync(DefaultWaitTimeout);
                renderCount = cut.RenderCount;
            }

            authorityToken.IsCancellationRequested.ShouldBeTrue();
            authority.SetResult(new(TenantLifecycleAuthorizationReflectionState.Authorized,
                TenantLifecycleAuthorizationReflectionState.Authorized));
            await authorityReturned.Task.WaitAsync(DefaultWaitTimeout);
            await WaitForEarlyMembershipTokenDisposalAsync(authorityToken);
            initialDetail.SetResult(ReadyWithSafeConfiguration(baseline, ProjectionLifecycleState.Current, "tenant-sequence:1"));
            await initialReadReturned.Task.WaitAsync(DefaultWaitTimeout);
            await WaitForEarlyMembershipTokenDisposalAsync(initialToken);
            await Renderer.Dispatcher.InvokeAsync(() => { });

            EarlyMembershipTrackedIdentity(tracker, kind, TenantId)
                .ShouldBe((TenantCommandLifecycleState.Accepted, MessageId, CorrelationId));
            tracker.HasPendingOwnership(TenantId).ShouldBeTrue();
            admission.IsLocked(TenantCommandAggregateLock.ForTenant(TenantId)).ShouldBeTrue();
            commandGateway.StatusCallCount.ShouldBe(0);
            Volatile.Read(ref alphaReads).ShouldBe(2, "obsolete authority cannot issue command proof reads");
            AssertEarlyMembershipNoDispatch(commandGateway);
            Renderer.UnhandledException.IsCompleted.ShouldBeFalse();
            if (transition == "route")
            {
                ((TenantDetailSnapshot)DetailField(cut, "_snapshot")!).Detail!.TenantId.ShouldBe("tenant.beta");
                cut.FindComponent<MemberAccessReview>().Instance.Detail.TenantId.ShouldBe("tenant.beta");
                cut.FindComponent<AddTenantMemberFlow>().Instance.Snapshot.MessageId.ShouldBeNull();
            }
            else
            {
                cut.RenderCount.ShouldBe(renderCount);
            }
        }
        finally
        {
            authority.TrySetResult(new(TenantLifecycleAuthorizationReflectionState.Authorized,
                TenantLifecycleAuthorizationReflectionState.Authorized));
            initialDetail.TrySetResult(ReadyWithSafeConfiguration(baseline, ProjectionLifecycleState.Current, "tenant-sequence:1"));
            await initialReadReturned.Task.WaitAsync(DefaultWaitTimeout);
        }

        async Task<TenantMutationAuthorizationReflection> HoldAuthorityAsync(CancellationToken token)
        {
            authorityToken = token;
            authorityEntered.TrySetResult();
            TenantMutationAuthorizationReflection result = await authority.Task;
            authorityReturned.TrySetResult();
            return result;
        }
    }

    /// <summary>A failed replay is contained and only a new notification retries authoritative status.</summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task EarlyMembershipNudgeContainsStatusFaultAndRecoversOnNewNotification(bool proofFault, bool loggerFault)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        const string TenantId = "tenant.alpha";
        const string MessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        const string CorrelationId = "correlation-original";
        var initialDetail = new TaskCompletionSource<TenantDetailSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        var initialReadReturned = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ITenantCommandGateway commands = Substitute.For<ITenantCommandGateway>();
        int statusReads = 0;
        commands.GetStatusAsync(Arg.Any<TenantCommandTrackingHandle>(), Arg.Any<CancellationToken>())
            .Returns(_ => Interlocked.Increment(ref statusReads) == 1 && !proofFault
                ? Task.FromException<TenantCommandStatusResult>(new InvalidOperationException("Injected optional status failure."))
                : Task.FromResult(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1, HasVerifiedCommandIdentity: true)
                {
                    CommittedEventSequence = 2,
                }));
        IProjectionSubscription backendSubscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        backendSubscription.SubscribeAsync("tenants", TenantId, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        RegisterMembershipPageServices(commands, new StubTenantsBffComposition());
        Services.AddSingleton(backendSubscription);
        Services.AddSingleton(notifier);
        Services.AddScoped<TenantReadRefreshSubscription>();
        var logger = new TenantCorrectionDiagnosticLog();
        ILoggerFactory loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(typeof(TenantDetailPage).FullName.ShouldNotBeNull())
            .Returns(_ => loggerFault ? throw new ObjectDisposedException("Retired optional logging scope.") : logger);
        Services.AddSingleton(loggerFactory);
        ITenantQueryGateway gateway = Services.GetRequiredService<ITenantQueryGateway>();
        TenantCommandAttemptTracker tracker = Services.GetRequiredService<TenantCommandAttemptTracker>();
        TenantAggregateCommandAdmissionGate admission = Services.GetRequiredService<TenantAggregateCommandAdmissionGate>();
        TenantDetail baseline = Detail(TenantId);
        TenantDetail refreshed = EarlyMembershipResult(baseline, "add");
        int detailReads = 0;
        CancellationToken initialToken = default;
        gateway.GetTenantAsync(Arg.Any<TenantDetailRequest>(), Arg.Any<TenantDetailSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                int read = Interlocked.Increment(ref detailReads);
                if (read == 1)
                {
                    initialToken = call.ArgAt<CancellationToken>(2);
                    TenantDetailSnapshot result = await initialDetail.Task;
                    initialReadReturned.TrySetResult();
                    return result;
                }
                if (proofFault && read == 3)
                {
                    throw new InvalidOperationException("Injected optional proof failure.");
                }
                return ReadyWithSafeConfiguration(refreshed, ProjectionLifecycleState.Current, "tenant-sequence:2");
            });
        gateway.GetTenantUsersAsync(Arg.Any<TenantUsersRequest>(), Arg.Any<TenantUsersSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(MemberSnapshot(refreshed) with { ProjectionVersion = "tenant-sequence:2" }));
        object original = EarlyMembershipAcceptedAttempt(baseline, "add", MessageId, CorrelationId);
        object oldOwner = new();
        tracker.TryBegin(TenantId, oldOwner, original, out TenantRetainedCommandAttempt? attempt).ShouldBeTrue();
        tracker.TryStartDelivery(attempt!, oldOwner).ShouldBeTrue();
        tracker.TryCompleteDelivery(attempt!, original, original).ShouldBeTrue();
        tracker.Detach(attempt, oldOwner);
        TenantCommandLifecycleState failedState = proofFault ? TenantCommandLifecycleState.ProjectionPending : TenantCommandLifecycleState.Accepted;
        int failureDetailReads = proofFault ? 3 : 2;
        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters.Add(page => page.TenantId, TenantId));
        try
        {
            cut.WaitForElement("[data-testid='tenants-detail-loading']");
            cut.WaitForAssertion(() => backendSubscription.ReceivedCalls()
                .Count(call => call.GetMethodInfo().Name == nameof(IProjectionSubscription.SubscribeAsync)).ShouldBe(1));
            DetailField(cut, "_memberAccessReview").ShouldBeNull();
            await cut.InvokeAsync(() => notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>("tenants", TenantId));
            cut.WaitForAssertion(() => Volatile.Read(ref statusReads).ShouldBe(1));
            EarlyMembershipState(cut, "add").ShouldBe((failedState, MessageId, CorrelationId));
            admission.IsLocked(TenantCommandAggregateLock.ForTenant(TenantId)).ShouldBeTrue();
            Volatile.Read(ref detailReads).ShouldBe(failureDetailReads);
            Renderer.UnhandledException.IsCompleted.ShouldBeFalse();
            if (loggerFault)
            {
                cut.WaitForAssertion(() => loggerFactory.Received(1).CreateLogger(typeof(TenantDetailPage).FullName.ShouldNotBeNull()));
                logger.Messages.ShouldBeEmpty();
            }
            else
            {
                cut.WaitForAssertion(() => logger.Messages.ShouldHaveSingleItem()
                    .ShouldBe("Optional membership projection refresh failed. ReasonCode=notification-callback-failed"));
            }

            initialDetail.SetResult(ReadyWithSafeConfiguration(baseline, ProjectionLifecycleState.Current, "tenant-sequence:1"));
            await initialReadReturned.Task.WaitAsync(DefaultWaitTimeout);
            await WaitForEarlyMembershipTokenDisposalAsync(initialToken);
            int rendersBefore = cut.RenderCount;
            TenantHighImpactViewportObservation viewport = Services.GetRequiredService<TenantHighImpactViewportObservation>();
            await cut.InvokeAsync(() => viewport.Observe(ViewportTier.Phone));
            cut.WaitForAssertion(() => cut.RenderCount.ShouldBeGreaterThan(rendersBefore));
            await cut.InvokeAsync(() => viewport.Observe(ViewportTier.Desktop));
            await Renderer.Dispatcher.InvokeAsync(() => { });
            Volatile.Read(ref statusReads).ShouldBe(1, "unrelated renders do not retry a failed notification");
            Volatile.Read(ref detailReads).ShouldBe(failureDetailReads);
            EarlyMembershipTrackedIdentity(tracker, "add", TenantId)
                .ShouldBe((failedState, MessageId, CorrelationId));
            admission.IsLocked(TenantCommandAggregateLock.ForTenant(TenantId)).ShouldBeTrue();

            await cut.InvokeAsync(() => notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>("tenants", TenantId));
            cut.WaitForAssertion(() =>
            {
                EarlyMembershipState(cut, "add").ShouldBe((TenantCommandLifecycleState.Confirmed, MessageId, CorrelationId));
                Volatile.Read(ref statusReads).ShouldBe(2);
                Volatile.Read(ref detailReads).ShouldBe(failureDetailReads + 2);
                admission.IsLocked(TenantCommandAggregateLock.ForTenant(TenantId)).ShouldBeFalse();
            });
            tracker.HasPendingOwnership(TenantId).ShouldBeFalse();
            commands.ReceivedCalls().Where(call => call.GetMethodInfo().Name is
                nameof(ITenantCommandGateway.AddUserToTenantAsync)
                or nameof(ITenantCommandGateway.ChangeUserRoleAsync)
                or nameof(ITenantCommandGateway.RemoveUserFromTenantAsync)).ShouldBeEmpty();
            Renderer.UnhandledException.IsCompleted.ShouldBeFalse();
        }
        finally
        {
            initialDetail.TrySetResult(ReadyWithSafeConfiguration(baseline, ProjectionLifecycleState.Current, "tenant-sequence:1"));
            await initialReadReturned.Task.WaitAsync(DefaultWaitTimeout);
        }
    }

    /// <summary>A same-route reload leaves an obsolete Razor reference until the replacement mounts.</summary>
    [Fact]
    public async Task EarlyMembershipNudgeWaitsForReplacementAfterSameRouteUnmount()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        const string TenantId = "tenant.alpha";
        const string MessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        const string CorrelationId = "correlation-original";
        var heldReload = new TaskCompletionSource<TenantDetailSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        var reloadEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var commandGateway = new TrackingMembershipCommandGateway(
            Task.FromResult(TenantCommandSubmissionResult.Failed("Unexpected redispatch.")),
            new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1, HasVerifiedCommandIdentity: true)
            {
                CommittedEventSequence = 2,
            });
        IProjectionSubscription backendSubscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        backendSubscription.SubscribeAsync("tenants", TenantId, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        RegisterMembershipPageServices(commandGateway, new StubTenantsBffComposition());
        Services.AddSingleton(backendSubscription);
        Services.AddSingleton(notifier);
        Services.AddScoped<TenantReadRefreshSubscription>();
        ITenantQueryGateway gateway = Services.GetRequiredService<ITenantQueryGateway>();
        TenantCommandAttemptTracker tracker = Services.GetRequiredService<TenantCommandAttemptTracker>();
        TenantAggregateCommandAdmissionGate admission = Services.GetRequiredService<TenantAggregateCommandAdmissionGate>();
        TenantDetail baseline = Detail(TenantId) with { Name = "Initial mounted detail" };
        TenantDetail refreshed = EarlyMembershipResult(baseline, "add") with { Name = "Replacement refreshed detail" };
        int detailReads = 0;
        CancellationToken reloadToken = default;
        gateway.GetTenantAsync(Arg.Any<TenantDetailRequest>(), Arg.Any<TenantDetailSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                int read = Interlocked.Increment(ref detailReads);
                if (read == 2)
                {
                    reloadToken = call.ArgAt<CancellationToken>(2);
                    reloadEntered.TrySetResult();
                    return heldReload.Task;
                }
                return Task.FromResult(ReadyWithSafeConfiguration(read == 1 ? baseline : refreshed,
                    ProjectionLifecycleState.Current, read == 1 ? "tenant-sequence:1" : "tenant-sequence:2"));
            });
        gateway.GetTenantUsersAsync(Arg.Any<TenantUsersRequest>(), Arg.Any<TenantUsersSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(MemberSnapshot(Volatile.Read(ref detailReads) <= 2 ? baseline : refreshed) with
            {
                ProjectionVersion = Volatile.Read(ref detailReads) <= 2 ? "tenant-sequence:1" : "tenant-sequence:2",
            }));
        object original = EarlyMembershipAcceptedAttempt(baseline, "add", MessageId, CorrelationId);
        object oldOwner = new();
        tracker.TryBegin(TenantId, oldOwner, original, out TenantRetainedCommandAttempt? attempt).ShouldBeTrue();
        tracker.TryStartDelivery(attempt!, oldOwner).ShouldBeTrue();
        tracker.TryCompleteDelivery(attempt!, original, original).ShouldBeTrue();
        tracker.Detach(attempt, oldOwner);
        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters.Add(page => page.TenantId, TenantId));
        cut.WaitForAssertion(() => EarlyMembershipState(cut, "add").State.ShouldBe(TenantCommandLifecycleState.Accepted));
        cut.WaitForAssertion(() => backendSubscription.ReceivedCalls()
            .Count(call => call.GetMethodInfo().Name == nameof(IProjectionSubscription.SubscribeAsync)).ShouldBe(1));
        MemberAccessReview oldReview = cut.FindComponent<MemberAccessReview>().Instance;
        AddTenantMemberFlow oldFlow = cut.FindComponent<AddTenantMemberFlow>().Instance;
        tracker.IsAttached(attempt!, oldFlow).ShouldBeTrue();
        Task reload = cut.InvokeAsync(() => cut.Instance.SetParametersAsync(ParameterView.FromDictionary(
            new Dictionary<string, object?> { [nameof(TenantDetailPage.TenantId)] = TenantId })));
        try
        {
            await reloadEntered.Task.WaitAsync(DefaultWaitTimeout);
            cut.WaitForElement("[data-testid='tenants-detail-loading']");
            cut.WaitForAssertion(() => cut.FindComponents<MemberAccessReview>().ShouldBeEmpty());
            DetailField(cut, "_memberAccessReview").ShouldBeSameAs(oldReview);
            tracker.IsAttached(attempt!, oldFlow).ShouldBeFalse();
            admission.IsLocked(TenantCommandAggregateLock.ForTenant(TenantId)).ShouldBeTrue();
            commandGateway.StatusCallCount.ShouldBe(0);

            await cut.InvokeAsync(() => notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>("tenants", TenantId));
            cut.WaitForAssertion(() =>
            {
                EarlyMembershipState(cut, "add").ShouldBe((TenantCommandLifecycleState.Confirmed, MessageId, CorrelationId));
                commandGateway.StatusCallCount.ShouldBe(1);
                Volatile.Read(ref detailReads).ShouldBe(4);
                admission.IsLocked(TenantCommandAggregateLock.ForTenant(TenantId)).ShouldBeFalse();
            });
            MemberAccessReview replacement = cut.FindComponent<MemberAccessReview>().Instance;
            replacement.ShouldNotBeSameAs(oldReview);
            tracker.IsAttached(attempt!, cut.FindComponent<AddTenantMemberFlow>().Instance).ShouldBeTrue();
            reloadToken.IsCancellationRequested.ShouldBeTrue();
            heldReload.SetResult(ReadyWithSafeConfiguration(baseline, ProjectionLifecycleState.Current, "tenant-sequence:1"));
            await reload.WaitAsync(DefaultWaitTimeout);
            replacement.Detail.Name.ShouldBe(refreshed.Name);
            ((TenantDetailSnapshot)DetailField(cut, "_snapshot")!).Detail!.Name.ShouldBe(refreshed.Name);
            EarlyMembershipTrackedIdentity(tracker, "add", TenantId)
                .ShouldBe((TenantCommandLifecycleState.Confirmed, MessageId, CorrelationId));
            commandGateway.StatusCallCount.ShouldBe(1);
            Volatile.Read(ref detailReads).ShouldBe(4);
            AssertEarlyMembershipNoDispatch(commandGateway);
            Renderer.UnhandledException.IsCompleted.ShouldBeFalse();
        }
        finally
        {
            heldReload.TrySetResult(ReadyWithSafeConfiguration(baseline, ProjectionLifecycleState.Current, "tenant-sequence:1"));
            await reload.WaitAsync(DefaultWaitTimeout);
        }
    }

    private static (TenantCommandLifecycleState State, string? MessageId, string? CorrelationId) EarlyMembershipTrackedIdentity(
        TenantCommandAttemptTracker tracker, string kind, string tenantId)
        => EarlyMembershipIdentity(kind switch
        {
            "add" => tracker.Find<TenantAddMemberCommandSnapshot>(tenantId)!,
            "change" => (object)tracker.Find<TenantChangeRoleCommandSnapshot>(tenantId)!,
            _ => tracker.Find<TenantRemoveMemberCommandSnapshot>(tenantId)!,
        });

    /// <summary>Observes completion cleanup even when obsolete work produces no render event.</summary>
    private static async Task WaitForEarlyMembershipTokenDisposalAsync(CancellationToken token)
    {
        Stopwatch elapsed = Stopwatch.StartNew();
        while (elapsed.Elapsed < DefaultWaitTimeout)
        {
            try
            {
                _ = token.WaitHandle;
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            await Task.Delay(10);
        }
        throw new ShouldAssertException("The completed membership read did not dispose its cancellation source.");
    }

    private static TenantDetail EarlyMembershipResult(TenantDetail baseline, string kind)
        => baseline with
        {
            Members = kind switch
            {
                "add" => [.. baseline.Members, new TenantMember("new-member", TenantRole.TenantReader)],
                "change" => baseline.Members.Select(member => member.UserId == "reader-user"
                    ? member with { Role = TenantRole.TenantContributor } : member).ToArray(),
                _ => baseline.Members.Where(member => member.UserId != "reader-user").ToArray(),
            },
        };

    private static object EarlyMembershipAcceptedAttempt(TenantDetail baseline, string kind, string messageId, string correlationId)
        => kind switch
        {
            "add" => TenantAddMemberCommandSnapshot.Idle()
                .RequestSent(new AddUserToTenant(baseline.TenantId, "new-member", TenantRole.TenantReader), "tenant-sequence:1")
                .Accepted(TenantCommandSubmissionResult.Accepted(messageId, correlationId)),
            "change" => TenantChangeRoleCommandSnapshot.Idle()
                .RequestSent(new ChangeUserRole(baseline.TenantId, "reader-user", TenantRole.TenantContributor), TenantRole.TenantReader, 1, "tenant-sequence:1")
                .Accepted(TenantCommandSubmissionResult.Accepted(messageId, correlationId)),
            _ => TenantRemoveMemberCommandSnapshot.Idle()
                .Previewed(new RemoveUserFromTenant(baseline.TenantId, "reader-user"), TenantRole.TenantReader, 1, false, baseline)
                .RequestSent("tenant-sequence:1")
                .Accepted(TenantCommandSubmissionResult.Accepted(messageId, correlationId)),
        };

    private static (TenantCommandLifecycleState State, string? MessageId, string? CorrelationId) EarlyMembershipState(
        IRenderedComponent<TenantDetailPage> cut, string kind)
        => kind switch
        {
            "add" => EarlyMembershipIdentity(cut.FindComponent<AddTenantMemberFlow>().Instance.Snapshot),
            "change" => EarlyMembershipIdentity(cut.FindComponent<ChangeTenantMemberRoleFlow>().Instance.Snapshot),
            _ => EarlyMembershipIdentity(cut.FindComponent<RemoveTenantMemberFlow>().Instance.Snapshot),
        };

    private static (TenantCommandLifecycleState State, string? MessageId, string? CorrelationId) EarlyMembershipIdentity(object snapshot)
        => snapshot switch
        {
            TenantAddMemberCommandSnapshot add => (add.State, add.MessageId, add.CorrelationId),
            TenantChangeRoleCommandSnapshot change => (change.State, change.MessageId, change.CorrelationId),
            TenantRemoveMemberCommandSnapshot remove => (remove.State, remove.MessageId, remove.CorrelationId),
            _ => throw new InvalidOperationException("Unexpected membership snapshot."),
        };

    private static void AssertEarlyMembershipNoDispatch(ITenantCommandGateway gateway)
        => gateway.ReceivedCalls().Where(call => call.GetMethodInfo().Name is
            nameof(ITenantCommandGateway.AddUserToTenantAsync)
            or nameof(ITenantCommandGateway.ChangeUserRoleAsync)
            or nameof(ITenantCommandGateway.RemoveUserFromTenantAsync)).ShouldBeEmpty();

    private static void AssertEarlyMembershipNoDispatch(TrackingMembershipCommandGateway gateway)
    {
        gateway.AddMemberCallCount.ShouldBe(0);
        gateway.ChangeRoleCallCount.ShouldBe(0);
        gateway.RemoveMemberCallCount.ShouldBe(0);
    }
}
