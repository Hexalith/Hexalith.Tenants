using System.Reflection;

using Bunit;

using Hexalith.FrontComposer.Contracts.Communication;
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
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed partial class TenantDetailSurfaceTests
{
    /// <summary>Unproven delayed authority cannot adopt or reconcile a retained membership attempt.</summary>
    [Theory]
    [InlineData("add", "denied")]
    [InlineData("change", "denied")]
    [InlineData("remove", "denied")]
    [InlineData("add", "indeterminate")]
    [InlineData("change", "indeterminate")]
    [InlineData("remove", "indeterminate")]
    [InlineData("add", "fault")]
    [InlineData("change", "fault")]
    [InlineData("remove", "fault")]
    public async Task EarlyMembershipNudgeRequiresProvenDelayedAuthority(string kind, string outcome)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var initial = new TaskCompletionSource<TenantDetailSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        var initialReturned = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var authority = new TaskCompletionSource<TenantMutationAuthorizationReflection>(TaskCreationOptions.RunContinuationsAsynchronously);
        var authorityEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var composition = new StubTenantsBffComposition
        {
            MutationResolver = (_, _, _) =>
            {
                authorityEntered.TrySetResult();
                return new(authority.Task);
            },
        };
        var commands = new TrackingMembershipCommandGateway(
            Task.FromResult(TenantCommandSubmissionResult.Failed("Unexpected redispatch.")),
            new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1, HasVerifiedCommandIdentity: true)
            {
                CommittedEventSequence = 2,
            });
        var services = RegisterEarlyNudgeBoundaryServices(commands, composition);
        TenantDetail baseline = Detail("tenant.alpha");
        TenantDetail refreshed = EarlyMembershipResult(baseline, kind);
        int detailReads = 0;
        CancellationToken initialToken = default;
        services.Gateway.GetTenantAsync(Arg.Any<TenantDetailRequest>(), Arg.Any<TenantDetailSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                if (Interlocked.Increment(ref detailReads) == 1)
                {
                    initialToken = call.ArgAt<CancellationToken>(2);
                    TenantDetailSnapshot result = await initial.Task;
                    initialReturned.TrySetResult();
                    return result;
                }
                return ReadyWithSafeConfiguration(refreshed, ProjectionLifecycleState.Current, "tenant-sequence:2");
            });
        services.Gateway.GetTenantUsersAsync(Arg.Any<TenantUsersRequest>(), Arg.Any<TenantUsersSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(MemberSnapshot(refreshed) with { ProjectionVersion = "tenant-sequence:2" }));
        TenantRetainedCommandAttempt attempt = SeedEarlyNudgeBoundaryAttempt(services.Tracker, baseline, kind);
        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters.Add(page => page.TenantId, baseline.TenantId));
        try
        {
            cut.WaitForElement("[data-testid='tenants-detail-loading']");
            cut.WaitForAssertion(() => services.Backend.ReceivedCalls()
                .Count(call => call.GetMethodInfo().Name == nameof(IProjectionSubscription.SubscribeAsync)).ShouldBe(1));
            DetailField(cut, "_memberAccessReview").ShouldBeNull();
            await cut.InvokeAsync(() => services.Notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>("tenants", baseline.TenantId));
            cut.WaitForElement("[data-testid='tenants-member-section']");
            await authorityEntered.Task.WaitAsync(DefaultWaitTimeout);
            services.Tracker.IsAttached(attempt, EarlyNudgeBoundaryFlow(cut, kind)).ShouldBeFalse();
            commands.StatusCallCount.ShouldBe(0);

            TenantLifecycleAuthorizationReflectionState expected = outcome == "denied"
                ? TenantLifecycleAuthorizationReflectionState.MissingPermission
                : TenantLifecycleAuthorizationReflectionState.Indeterminate;
            if (outcome == "fault")
            {
                authority.SetException(new InvalidOperationException("Injected authorization failure."));
            }
            else
            {
                authority.SetResult(new(expected, expected));
            }
            cut.WaitForAssertion(() =>
            {
                DetailField(cut, "_mutationAuthorizationResolved").ShouldBeSameAs(DetailField(cut, "_snapshot"));
                cut.FindComponent<MemberAccessReview>().Instance.AuthorizationReflection.ShouldBe(expected);
            });
            services.Tracker.IsAttached(attempt, EarlyNudgeBoundaryFlow(cut, kind)).ShouldBeFalse();
            commands.StatusCallCount.ShouldBe(0);
            Volatile.Read(ref detailReads).ShouldBe(2, "unproven authority does not issue command proof reads");
            EarlyMembershipTrackedIdentity(services.Tracker, kind, baseline.TenantId)
                .ShouldBe((TenantCommandLifecycleState.Accepted, "01ARZ3NDEKTSV4RRFFQ69G5FAV", "correlation-original"));
            services.Admission.IsLocked(TenantCommandAggregateLock.ForTenant(baseline.TenantId)).ShouldBeTrue();
            AssertEarlyMembershipNoDispatch(commands);

            initial.SetResult(ReadyWithSafeConfiguration(baseline, ProjectionLifecycleState.Current, "tenant-sequence:1"));
            await initialReturned.Task.WaitAsync(DefaultWaitTimeout);
            await AwaitEarlyNudgeBoundaryDisposalAsync(initialToken);
            ((TenantDetailSnapshot)DetailField(cut, "_snapshot")!).Detail!.Members.ShouldBe(refreshed.Members);
            commands.StatusCallCount.ShouldBe(0);
            Renderer.UnhandledException.IsCompleted.ShouldBeFalse();
        }
        finally
        {
            authority.TrySetResult(new(TenantLifecycleAuthorizationReflectionState.Indeterminate,
                TenantLifecycleAuthorizationReflectionState.Indeterminate));
            initial.TrySetResult(ReadyWithSafeConfiguration(baseline, ProjectionLifecycleState.Current, "tenant-sequence:1"));
            await initialReturned.Task.WaitAsync(DefaultWaitTimeout);
        }
    }

    /// <summary>Old reads and delayed authority cannot reconcile an attempt for a replacement caller.</summary>
    [Theory]
    [InlineData("add")]
    [InlineData("change")]
    [InlineData("remove")]
    public async Task EarlyMembershipNudgeRejectsPreviousCallerAuthority(string kind)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var authentication = new MutableAuthenticationStateProvider();
        Services.AddSingleton<AuthenticationStateProvider>(authentication);
        var initial = new TaskCompletionSource<TenantDetailSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        var initialReturned = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var oldAuthority = new TaskCompletionSource<TenantMutationAuthorizationReflection>(TaskCreationOptions.RunContinuationsAsynchronously);
        var authorityEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var authorityReturned = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken oldAuthorityToken = default;
        var composition = new StubTenantsBffComposition
        {
            MutationResolver = (_, detail, token) => detail.Name == "Replacement caller detail"
                ? ValueTask.FromResult(new TenantMutationAuthorizationReflection(
                    TenantLifecycleAuthorizationReflectionState.MissingPermission,
                    TenantLifecycleAuthorizationReflectionState.MissingPermission))
                : new(HoldPreviousAuthorityAsync(token)),
        };
        var commands = new TrackingMembershipCommandGateway(
            Task.FromResult(TenantCommandSubmissionResult.Failed("Unexpected redispatch.")),
            new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1, HasVerifiedCommandIdentity: true)
            {
                CommittedEventSequence = 2,
            });
        var services = RegisterEarlyNudgeBoundaryServices(commands, composition);
        TenantDetail baseline = Detail("tenant.alpha");
        TenantDetail oldDetail = EarlyMembershipResult(baseline, kind) with { Name = "Previous caller detail" };
        TenantDetail nextDetail = baseline with { Name = "Replacement caller detail" };
        string nextScope = TenantQueryGateway.AuditCallerScope("operator.next");
        bool replacementCaller = false;
        int detailReads = 0;
        CancellationToken initialToken = default;
        services.Gateway.GetTenantAsync(Arg.Any<TenantDetailRequest>(), Arg.Any<TenantDetailSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                if (Interlocked.Increment(ref detailReads) == 1)
                {
                    initialToken = call.ArgAt<CancellationToken>(2);
                    TenantDetailSnapshot result = await initial.Task;
                    initialReturned.TrySetResult();
                    return result;
                }
                return replacementCaller
                    ? ReadyWithSafeConfiguration(nextDetail, ProjectionLifecycleState.Current, "tenant-sequence:3").WithCallerScope(nextScope)
                    : ReadyWithSafeConfiguration(oldDetail, ProjectionLifecycleState.Current, "tenant-sequence:2");
            });
        services.Gateway.GetTenantUsersAsync(Arg.Any<TenantUsersRequest>(), Arg.Any<TenantUsersSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(replacementCaller
                ? MemberSnapshot(nextDetail) with { ProjectionVersion = "tenant-sequence:3", CallerScope = nextScope }
                : MemberSnapshot(oldDetail) with { ProjectionVersion = "tenant-sequence:2" }));
        TenantRetainedCommandAttempt attempt = SeedEarlyNudgeBoundaryAttempt(services.Tracker, baseline, kind);
        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters.Add(page => page.TenantId, baseline.TenantId));
        try
        {
            cut.WaitForElement("[data-testid='tenants-detail-loading']");
            cut.WaitForAssertion(() => services.Backend.ReceivedCalls()
                .Count(call => call.GetMethodInfo().Name == nameof(IProjectionSubscription.SubscribeAsync)).ShouldBe(1));
            DetailField(cut, "_memberAccessReview").ShouldBeNull();
            await cut.InvokeAsync(() => services.Notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>("tenants", baseline.TenantId));
            cut.WaitForElement("[data-testid='tenants-member-section']");
            await authorityEntered.Task.WaitAsync(DefaultWaitTimeout);
            commands.StatusCallCount.ShouldBe(0);

            await cut.InvokeAsync(() =>
            {
                replacementCaller = true;
                authentication.Notify(AdministratorPrincipal("operator.next"));
            });
            cut.WaitForAssertion(() =>
            {
                TenantDetailSnapshot snapshot = (TenantDetailSnapshot)DetailField(cut, "_snapshot")!;
                snapshot.Detail!.Name.ShouldBe(nextDetail.Name);
                snapshot.CallerScope.ShouldBe(nextScope);
                DetailField(cut, "_mutationAuthorizationResolved").ShouldBeSameAs(snapshot);
                cut.FindComponent<MemberAccessReview>().Instance.AuthorizationReflection
                    .ShouldBe(TenantLifecycleAuthorizationReflectionState.MissingPermission);
            });
            oldAuthorityToken.IsCancellationRequested.ShouldBeTrue();
            oldAuthority.SetResult(new(TenantLifecycleAuthorizationReflectionState.Authorized,
                TenantLifecycleAuthorizationReflectionState.Authorized));
            await authorityReturned.Task.WaitAsync(DefaultWaitTimeout);
            await AwaitEarlyNudgeBoundaryDisposalAsync(oldAuthorityToken);
            initial.SetResult(ReadyWithSafeConfiguration(baseline, ProjectionLifecycleState.Current, "tenant-sequence:1"));
            await initialReturned.Task.WaitAsync(DefaultWaitTimeout);
            await AwaitEarlyNudgeBoundaryDisposalAsync(initialToken);
            await Renderer.Dispatcher.InvokeAsync(() => { });

            TenantDetailSnapshot current = (TenantDetailSnapshot)DetailField(cut, "_snapshot")!;
            current.Detail!.Name.ShouldBe(nextDetail.Name);
            current.CallerScope.ShouldBe(nextScope);
            cut.FindComponent<MemberAccessReview>().Instance.AuthorizationReflection
                .ShouldBe(TenantLifecycleAuthorizationReflectionState.MissingPermission);
            services.Tracker.IsAttached(attempt, EarlyNudgeBoundaryFlow(cut, kind)).ShouldBeFalse();
            EarlyMembershipState(cut, kind).MessageId.ShouldBeNull();
            EarlyMembershipTrackedIdentity(services.Tracker, kind, baseline.TenantId)
                .ShouldBe((TenantCommandLifecycleState.Accepted, "01ARZ3NDEKTSV4RRFFQ69G5FAV", "correlation-original"));
            services.Admission.IsLocked(TenantCommandAggregateLock.ForTenant(baseline.TenantId)).ShouldBeTrue();
            commands.StatusCallCount.ShouldBe(0);
            Volatile.Read(ref detailReads).ShouldBe(3, "old authority and read responses cannot request command proof");
            AssertEarlyMembershipNoDispatch(commands);
            Renderer.UnhandledException.IsCompleted.ShouldBeFalse();
        }
        finally
        {
            oldAuthority.TrySetResult(new(TenantLifecycleAuthorizationReflectionState.Authorized,
                TenantLifecycleAuthorizationReflectionState.Authorized));
            initial.TrySetResult(ReadyWithSafeConfiguration(baseline, ProjectionLifecycleState.Current, "tenant-sequence:1"));
            await initialReturned.Task.WaitAsync(DefaultWaitTimeout);
        }

        async Task<TenantMutationAuthorizationReflection> HoldPreviousAuthorityAsync(CancellationToken token)
        {
            oldAuthorityToken = token;
            authorityEntered.TrySetResult();
            TenantMutationAuthorizationReflection result = await oldAuthority.Task;
            authorityReturned.TrySetResult();
            return result;
        }
    }

    /// <summary>A replay response arriving after route or disposal cannot release retained ownership.</summary>
    [Theory]
    [InlineData("status", "route")]
    [InlineData("status", "dispose")]
    [InlineData("proof", "route")]
    [InlineData("proof", "dispose")]
    [InlineData("status", "notify")]
    [InlineData("proof", "notify")]
    public async Task EarlyMembershipNudgeContainsResponseAfterBoundary(string boundary, string transition)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var initial = new TaskCompletionSource<TenantDetailSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        var initialReturned = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var status = new TaskCompletionSource<TenantCommandStatusResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var proof = new TaskCompletionSource<TenantDetailSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        var boundaryEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        TenantCommandStatusResult completed = new(CommandStatus.Completed, EventCount: 1, HasVerifiedCommandIdentity: true)
        {
            CommittedEventSequence = 2,
        };
        ITenantCommandGateway commands = Substitute.For<ITenantCommandGateway>();
        int statusReads = 0;
        commands.GetStatusAsync(Arg.Any<TenantCommandTrackingHandle>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                Interlocked.Increment(ref statusReads);
                if (boundary == "status")
                {
                    boundaryEntered.TrySetResult();
                    return status.Task;
                }
                return Task.FromResult(completed);
            });
        var services = RegisterEarlyNudgeBoundaryServices(commands, new StubTenantsBffComposition());
        TenantDetail baseline = Detail("tenant.alpha");
        TenantDetail refreshed = EarlyMembershipResult(baseline, "add");
        int alphaReads = 0;
        CancellationToken initialToken = default;
        services.Gateway.GetTenantAsync(Arg.Any<TenantDetailRequest>(), Arg.Any<TenantDetailSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                string tenantId = call.ArgAt<TenantDetailRequest>(0).TenantId;
                if (tenantId != baseline.TenantId)
                {
                    return ReadyWithSafeConfiguration(Detail(tenantId), ProjectionLifecycleState.Current, "tenant-sequence:2");
                }
                int read = Interlocked.Increment(ref alphaReads);
                if (read == 1)
                {
                    initialToken = call.ArgAt<CancellationToken>(2);
                    TenantDetailSnapshot result = await initial.Task;
                    initialReturned.TrySetResult();
                    return result;
                }
                if (read == 3 && boundary == "proof")
                {
                    boundaryEntered.TrySetResult();
                    return await proof.Task;
                }
                return ReadyWithSafeConfiguration(refreshed, ProjectionLifecycleState.Current, "tenant-sequence:2");
            });
        services.Gateway.GetTenantUsersAsync(Arg.Any<TenantUsersRequest>(), Arg.Any<TenantUsersSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(MemberSnapshot(call.ArgAt<TenantUsersRequest>(0).TenantId == baseline.TenantId
                ? refreshed : Detail(call.ArgAt<TenantUsersRequest>(0).TenantId)) with
            { ProjectionVersion = "tenant-sequence:2" }));
        _ = SeedEarlyNudgeBoundaryAttempt(services.Tracker, baseline, "add");
        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters.Add(page => page.TenantId, baseline.TenantId));
        try
        {
            cut.WaitForElement("[data-testid='tenants-detail-loading']");
            cut.WaitForAssertion(() => services.Backend.ReceivedCalls()
                .Count(call => call.GetMethodInfo().Name == nameof(IProjectionSubscription.SubscribeAsync)).ShouldBe(1));
            DetailField(cut, "_memberAccessReview").ShouldBeNull();
            await cut.InvokeAsync(() => services.Notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>("tenants", baseline.TenantId));
            await boundaryEntered.Task.WaitAsync(DefaultWaitTimeout);
            AddTenantMemberFlow oldFlow = cut.FindComponent<AddTenantMemberFlow>().Instance;
            Task replayCompletion = ((TaskCompletionSource)typeof(AddTenantMemberFlow)
                .GetField("_refreshCompletion", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(oldFlow)!).Task;
            replayCompletion.IsCompleted.ShouldBeFalse();
            services.Admission.IsLocked(TenantCommandAggregateLock.ForTenant(baseline.TenantId)).ShouldBeTrue();

            int disposedRenderCount = 0;
            if (transition == "notify")
            {
                await cut.InvokeAsync(() => services.Notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>("tenants", baseline.TenantId));
                cut.WaitForAssertion(() =>
                {
                    Volatile.Read(ref alphaReads).ShouldBe(boundary == "proof" ? 4 : 3);
                    cut.FindComponent<MemberAccessReview>().Instance.Members.IsRefreshing.ShouldBeFalse();
                });
                replayCompletion.IsCompleted.ShouldBeFalse();
                EarlyMembershipState(cut, "add").State.ShouldBe(boundary == "proof"
                    ? TenantCommandLifecycleState.ProjectionPending : TenantCommandLifecycleState.Accepted);
                Volatile.Read(ref statusReads).ShouldBe(1);
            }
            else if (transition == "route")
            {
                cut.Render(parameters => parameters.Add(page => page.TenantId, "tenant.beta"));
                cut.WaitForAssertion(() => cut.FindComponent<MemberAccessReview>().Instance.Detail.TenantId.ShouldBe("tenant.beta"));
            }
            else
            {
                await DisposeComponentsAsync().WaitAsync(DefaultWaitTimeout);
                disposedRenderCount = cut.RenderCount;
            }

            status.TrySetResult(completed);
            proof.TrySetResult(ReadyWithSafeConfiguration(refreshed, ProjectionLifecycleState.Current, "tenant-sequence:2"));
            await replayCompletion.WaitAsync(DefaultWaitTimeout);
            initial.SetResult(ReadyWithSafeConfiguration(baseline, ProjectionLifecycleState.Current, "tenant-sequence:1"));
            await initialReturned.Task.WaitAsync(DefaultWaitTimeout);
            await AwaitEarlyNudgeBoundaryDisposalAsync(initialToken);
            await Renderer.Dispatcher.InvokeAsync(() => { });

            var retained = EarlyMembershipTrackedIdentity(services.Tracker, "add", baseline.TenantId);
            retained.State.ShouldBe(transition == "notify"
                ? TenantCommandLifecycleState.Confirmed : TenantCommandLifecycleState.ProjectionPending);
            retained.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
            retained.CorrelationId.ShouldBe("correlation-original");
            services.Tracker.HasPendingOwnership(baseline.TenantId).ShouldBe(transition != "notify");
            services.Admission.IsLocked(TenantCommandAggregateLock.ForTenant(baseline.TenantId)).ShouldBe(transition != "notify");
            Volatile.Read(ref statusReads).ShouldBe(1);
            Volatile.Read(ref alphaReads).ShouldBe(transition == "notify" ? 4 : boundary == "proof" ? 3 : 2);
            AssertEarlyNudgeBoundaryNoDispatch(commands);
            Renderer.UnhandledException.IsCompleted.ShouldBeFalse();
            if (transition == "route")
            {
                ((TenantDetailSnapshot)DetailField(cut, "_snapshot")!).Detail!.TenantId.ShouldBe("tenant.beta");
                cut.FindComponent<AddTenantMemberFlow>().Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Idle);
                cut.FindComponent<AddTenantMemberFlow>().Instance.Snapshot.MessageId.ShouldBeNull();
                services.Admission.IsLocked(TenantCommandAggregateLock.ForTenant("tenant.beta")).ShouldBeFalse();
            }
            else if (transition == "dispose")
            {
                cut.RenderCount.ShouldBe(disposedRenderCount);
            }
            else
            {
                EarlyMembershipState(cut, "add")
                    .ShouldBe((TenantCommandLifecycleState.Confirmed, "01ARZ3NDEKTSV4RRFFQ69G5FAV", "correlation-original"));
            }
        }
        finally
        {
            status.TrySetResult(completed);
            proof.TrySetResult(ReadyWithSafeConfiguration(refreshed, ProjectionLifecycleState.Current, "tenant-sequence:2"));
            initial.TrySetResult(ReadyWithSafeConfiguration(baseline, ProjectionLifecycleState.Current, "tenant-sequence:1"));
            await initialReturned.Task.WaitAsync(DefaultWaitTimeout);
        }
    }

    private (
        ITenantQueryGateway Gateway,
        TenantCommandAttemptTracker Tracker,
        TenantAggregateCommandAdmissionGate Admission,
        IProjectionSubscription Backend,
        IProjectionChangeNotifierWithTenant Notifier)
        RegisterEarlyNudgeBoundaryServices(ITenantCommandGateway commands, StubTenantsBffComposition composition)
    {
        IProjectionSubscription backend = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        backend.SubscribeAsync("tenants", Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        RegisterMembershipPageServices(commands, composition);
        Services.AddSingleton(backend);
        Services.AddSingleton(notifier);
        Services.AddScoped<TenantReadRefreshSubscription>();
        return (Services.GetRequiredService<ITenantQueryGateway>(),
            Services.GetRequiredService<TenantCommandAttemptTracker>(),
            Services.GetRequiredService<TenantAggregateCommandAdmissionGate>(), backend, notifier);
    }

    private static TenantRetainedCommandAttempt SeedEarlyNudgeBoundaryAttempt(
        TenantCommandAttemptTracker tracker, TenantDetail baseline, string kind)
    {
        object original = EarlyMembershipAcceptedAttempt(baseline, kind,
            "01ARZ3NDEKTSV4RRFFQ69G5FAV", "correlation-original");
        object oldOwner = new();
        tracker.TryBegin(baseline.TenantId, oldOwner, original, out TenantRetainedCommandAttempt? attempt).ShouldBeTrue();
        tracker.TryStartDelivery(attempt!, oldOwner).ShouldBeTrue();
        tracker.TryCompleteDelivery(attempt!, original, original).ShouldBeTrue();
        tracker.Detach(attempt, oldOwner);
        return attempt!;
    }

    private static object EarlyNudgeBoundaryFlow(IRenderedComponent<TenantDetailPage> cut, string kind)
        => kind switch
        {
            "add" => cut.FindComponent<AddTenantMemberFlow>().Instance,
            "change" => cut.FindComponent<ChangeTenantMemberRoleFlow>().Instance,
            _ => cut.FindComponent<RemoveTenantMemberFlow>().Instance,
        };

    private static void AssertEarlyNudgeBoundaryNoDispatch(ITenantCommandGateway commands)
        => commands.ReceivedCalls().Where(call => call.GetMethodInfo().Name is
            nameof(ITenantCommandGateway.AddUserToTenantAsync)
            or nameof(ITenantCommandGateway.ChangeUserRoleAsync)
            or nameof(ITenantCommandGateway.RemoveUserFromTenantAsync)).ShouldBeEmpty();

    private static async Task AwaitEarlyNudgeBoundaryDisposalAsync(CancellationToken token)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (true)
        {
            try
            {
                _ = token.WaitHandle;
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            await Task.Delay(1, deadline.Token);
        }
    }
}
