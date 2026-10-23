using Microsoft.FluentUI.AspNetCore.Components;
using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.UI.Services;
using Hexalith.Tenants.UI.Resources;
using Microsoft.Extensions.Localization;
using Bunit;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.UI.Components.Pages;
using Hexalith.Tenants.UI.Components.Tenants;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.Tenants.UI.State.UserTenants;
using Hexalith.Tenants.UI.Tests.Components;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests;

public sealed partial class TenantsWorkspaceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Workspace_create_recovery_reloads_authoritative_proof_with_the_current_caller(bool manualRefresh)
    {
        Services.AddSingleton<AuthenticationStateProvider>(new MutableAuthenticationStateProvider(AdministratorPrincipal()));
        IProjectionSubscription subscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        Services.AddSingleton(subscription);
        Services.AddSingleton(notifier);
        Services.AddSingleton<TenantReadRefreshSubscription>();
        string caller = TenantQueryGateway.AuditCallerScope("operator.alpha");
        bool visible = false;
        int listReads = 0;
        ITenantQueryGateway reads = Substitute.For<ITenantQueryGateway>();
        reads.ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                listReads++;
                return Task.FromResult(visible
                    ? TenantCommandProofFixtures.ListProof(new TenantSummary("first-literal-tenant", "First tenant", TenantStatus.Active), "tenant-sequence:2")! with { CallerScope = caller }
                    : TenantListSnapshot.Empty(true, ReadModelFreshnessState.Unknown) with { CallerScope = caller });
            });
        reads.GetTenantAsync(Arg.Any<TenantDetailRequest>(), Arg.Any<TenantDetailSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(TenantDetailSnapshot.NotFound("first-literal-tenant"));
        ITenantCommandGateway commands = Substitute.For<ITenantCommandGateway>();
        commands.CreateTenantAsync(Arg.Any<CreateTenant>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(TenantCommandSubmissionResult.Accepted(call.ArgAt<string>(1), "first-tracking")));
        commands.GetStatusAsync(Arg.Any<TenantCommandTrackingHandle>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new TenantCommandStatusResult(visible ? CommandStatus.Completed : CommandStatus.Received,
                EventCount: visible ? 1 : null, HasVerifiedCommandIdentity: true)));
        Services.AddSingleton(reads);
        Services.AddSingleton(commands);
        bool holdAuthority = false;
        TaskCompletionSource authorityEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<TenantLifecycleAuthorizationReflectionState> authority = new(TaskCreationOptions.RunContinuationsAsynchronously);
        async ValueTask<TenantLifecycleAuthorizationReflectionState> ResolveAsync(CancellationToken token)
        {
            if (!holdAuthority) return TenantLifecycleAuthorizationReflectionState.Authorized;
            authorityEntered.TrySetResult();
            return await authority.Task.WaitAsync(token);
        }
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(cancellableResolver: ResolveAsync));
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddFluentUIComponents();
        IRenderedComponent<TenantsWorkspace> cut = RenderWorkspace();
        IRenderedComponent<CreateTenantFlow> create = cut.FindComponent<CreateTenantFlow>();
        cut.WaitForAssertion(() =>
        {
            create.Instance.IsAuthorized.ShouldBeTrue();
            create.Instance.CurrentCallerScope.ShouldBe(caller);
        });
        create.Find("[data-testid='tenants-create-tenant-id']").Change("first-literal-tenant");
        create.Find("[data-testid='tenants-create-name']").Change("First tenant");
        await create.Find("form").SubmitAsync();
        cut.WaitForAssertion(() => subscription.Received(1).SubscribeAsync("tenants", "first-literal-tenant", Arg.Any<CancellationToken>()));
        string? message = create.Instance.Snapshot.MessageId;
        int before = listReads;
        visible = true;
        if (manualRefresh)
        {
            for (int renewal = 0; renewal < 5; renewal++)
            {
                // Successful authority renewals must not consume the optional-transport failure budget.
                visible = renewal == 4;
                authorityEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
                authority = new(TaskCreationOptions.RunContinuationsAsynchronously);
                holdAuthority = true;
                Task refresh = create.Find("[data-testid='tenants-create-refresh']").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
                await authorityEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
                create.Instance.CurrentCallerScope.ShouldBe(caller);
                holdAuthority = false;
                authority.SetResult(TenantLifecycleAuthorizationReflectionState.Authorized);
                await refresh.WaitAsync(TimeSpan.FromSeconds(5));
                if (!visible)
                {
                    int expectedSubscriptions = renewal + 2;
                    await WaitForWorkspaceOperationAsync(() => subscription.ReceivedCalls()
                        .Count(call => call.GetMethodInfo().Name == nameof(IProjectionSubscription.SubscribeAsync)) >= expectedSubscriptions);
                    await subscription.Received(expectedSubscriptions).SubscribeAsync("tenants", "first-literal-tenant", Arg.Any<CancellationToken>());
                    create.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Accepted);
                }
            }
        }
        else
        {
            notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>("tenants", "first-literal-tenant");
        }
        cut.WaitForAssertion(() => create.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Confirmed));
        create.Instance.Snapshot.MessageId.ShouldBe(message);
        create.Instance.Snapshot.CorrelationId.ShouldBe("first-tracking");
        create.Instance.CurrentCallerScope.ShouldBe(caller);
        listReads.ShouldBe(before + (manualRefresh ? 6 : 1));
        await commands.Received(1).CreateTenantAsync(Arg.Any<CreateTenant>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await commands.Received(manualRefresh ? 6 : 2).GetStatusAsync(Arg.Any<TenantCommandTrackingHandle>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public async Task WorkspaceConsumerRouterTransitionCancelsUndispatchedAdmissionAndReleasesAllocatedLease(
        bool mineScope, bool finalAdmission, bool lateFault)
    {
        var clock = RegisterFiniteWorkspaceClock();
        Services.AddSingleton<AuthenticationStateProvider>(new MutableAuthenticationStateProvider(AdministratorPrincipal()));
        var held = new TaskCompletionSource<TenantLifecycleAuthorizationReflectionState>(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        bool holdAdmission = false;
        int admissionResolutions = 0;
        CancellationToken suppliedToken = default;
        async ValueTask<TenantLifecycleAuthorizationReflectionState> ResolveAsync(CancellationToken token)
        {
            if (!holdAdmission || ++admissionResolutions != (finalAdmission ? 2 : 1))
            {
                return TenantLifecycleAuthorizationReflectionState.Authorized;
            }
            suppliedToken = token;
            entered.TrySetResult();
            try
            {
                return await held.Task.ConfigureAwait(false);
            }
            finally
            {
                completed.TrySetResult();
            }
        }

        ITenantQueryGateway reads = ConsumerLifecycleReads();
        ITenantCommandGateway commands = Substitute.For<ITenantCommandGateway>();
        commands.CreateTenantAsync(Arg.Any<CreateTenant>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(TenantCommandSubmissionResult.Accepted(call.ArgAt<string>(1), "consumer-route-correlation")));
        commands.GetStatusAsync(Arg.Any<TenantCommandTrackingHandle>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new TenantCommandStatusResult(CommandStatus.Received, HasVerifiedCommandIdentity: true)));
        RegisterFiniteWorkspaceServices(reads, new StubTenantsBffComposition(cancellableResolver: ResolveAsync), commands);
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/tenants/tenants");
        IRenderedComponent<Router> router = RenderConsumerLifecycleRouter();
        IRenderedComponent<TenantsWorkspace> workspace = router.FindComponent<TenantsWorkspace>();
        IRenderedComponent<CreateTenantFlow> create = router.FindComponent<CreateTenantFlow>();
        router.WaitForAssertion(() => create.Instance.IsAuthorized.ShouldBeTrue());
        SetAcceptanceCreateIntent(create);
        CreateTenantFlow originalCreate = create.Instance;
        TenantCommandAttemptTracker attempts = Services.GetRequiredService<TenantCommandAttemptTracker>();
        TenantAggregateCommandAdmissionGate gate = Services.GetRequiredService<TenantAggregateCommandAdmissionGate>();
        holdAdmission = true;
        Task submit = create.Find("form").SubmitAsync();
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await WaitForWorkspaceOperationAsync(() => clock.PendingTimerCount > 0);
        TenantRetainedCommandAttempt? abandoned = finalAdmission
            ? ConsumerCreateField<TenantRetainedCommandAttempt>(originalCreate, "_retainedAttempt") : null;
        string? undispatchedMessageId = originalCreate.Snapshot.MessageId;
        if (finalAdmission)
        {
            abandoned.ShouldNotBeNull();
            originalCreate.Snapshot.Intent.ShouldBe(new CreateTenant("retained-create", "Retained create", "Exact retained intent"));
            undispatchedMessageId.ShouldNotBeNullOrWhiteSpace();
            originalCreate.Snapshot.State.ShouldBe(TenantCommandLifecycleState.RequestSent);
            abandoned.Lease.IsDispatchMarked.ShouldBeFalse();
            abandoned.DeliveryInFlight.ShouldBeFalse();
            gate.TryAcquire(TenantCommandAggregateLock.ForTenant("retained-create"), new object()).ShouldBeFalse();
        }
        else
        {
            originalCreate.Snapshot.MessageId.ShouldBeNull();
            attempts.HasPendingOwnership("retained-create").ShouldBeFalse();
        }

        try
        {
            await NavigateConsumerLifecycleAsync(router, navigation, mineScope);
            workspace.Instance.ShouldBeSameAs(router.FindComponent<TenantsWorkspace>().Instance,
                "Router query/path updates must drive OnParametersSetAsync on the live workspace.");
            if (mineScope)
            {
                router.FindAll("[data-testid='tenants-create-flow']").ShouldBeEmpty();
                ConsumerCreateField<bool>(originalCreate, "_disposed").ShouldBeTrue();
            }
            else
            {
                router.FindComponent<CreateTenantFlow>().Instance.ShouldBeSameAs(originalCreate);
                originalCreate.IsAuthorized.ShouldBeFalse();
            }
            await submit.WaitAsync(TimeSpan.FromSeconds(5));
            await WaitForWorkspaceOperationAsync(() => suppliedToken.IsCancellationRequested);
            commands.ReceivedCalls().ShouldBeEmpty();
            attempts.Find<TenantCreateCommandSnapshot>("retained-create").ShouldBeNull();
            attempts.HasPendingOwnership("retained-create").ShouldBeFalse();
            abandoned?.Lease.IsReleased.ShouldBeTrue();
            object probeOwner = new();
            string aggregateKey = TenantCommandAggregateLock.ForTenant("retained-create");
            gate.TryAcquire(aggregateKey, probeOwner).ShouldBeTrue("Cancelled undispatched admission must release the exact aggregate.");
            gate.Release(aggregateKey, probeOwner);

            if (lateFault)
            {
                held.SetException(new InvalidOperationException("obsolete routed admission detail"));
            }
            else
            {
                held.SetResult(TenantLifecycleAuthorizationReflectionState.Authorized);
            }
            await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await WaitForWorkspaceOperationAsync(() => clock.PendingTimerCount == 0);
            await router.InvokeAsync(static () => Task.CompletedTask);
            commands.ReceivedCalls().ShouldBeEmpty();
            attempts.Find<TenantCreateCommandSnapshot>("retained-create").ShouldBeNull();

            holdAdmission = false;
            await router.InvokeAsync(() => navigation.NavigateTo("/tenants/tenants"));
            router.WaitForElement("[data-testid='tenants-create-submit']");
            create = router.FindComponent<CreateTenantFlow>();
            router.WaitForAssertion(() => create.Instance.IsAuthorized.ShouldBeTrue());
            if (mineScope)
            {
                create.Instance.ShouldNotBeSameAs(originalCreate);
                SetAcceptanceCreateIntent(create);
            }
            else
            {
                create.Instance.ShouldBeSameAs(originalCreate);
                create.Find("[data-testid='tenants-create-tenant-id']").GetAttribute("value").ShouldBe("retained-create");
                create.Find("[data-testid='tenants-create-name']").GetAttribute("value").ShouldBe("Retained create");
            }
            await create.Find("form").SubmitAsync();
            _ = commands.Received(1).CreateTenantAsync(
                Arg.Is<CreateTenant>(request => request == new CreateTenant("retained-create", "Retained create", "Exact retained intent")),
                Arg.Is<string>(messageId => !string.IsNullOrWhiteSpace(messageId) && messageId != undispatchedMessageId),
                Arg.Is<CancellationToken>(token => token == CancellationToken.None));
            create.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Accepted);
            create.Instance.Snapshot.CorrelationId.ShouldBe("consumer-route-correlation");
            attempts.Find<TenantCreateCommandSnapshot>("retained-create").ShouldBe(create.Instance.Snapshot);
            ConsumerCreateField<TenantRetainedCommandAttempt>(create.Instance, "_retainedAttempt").Lease.IsDispatchMarked.ShouldBeTrue();
            admissionResolutions.ShouldBe(finalAdmission ? 2 : 1);
            router.Markup.ShouldNotContain("obsolete routed admission detail");
            clock.GetUtcNow().ShouldBe(DateTimeOffset.UnixEpoch);
        }
        finally
        {
            held.TrySetResult(TenantLifecycleAuthorizationReflectionState.MissingPermission);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WorkspaceConsumerRouterTransitionPreservesAlreadyDispatchedCreateAndExactRecovery(bool mineScope)
    {
        var clock = RegisterFiniteWorkspaceClock();
        Services.AddSingleton<AuthenticationStateProvider>(new MutableAuthenticationStateProvider(AdministratorPrincipal()));
        var held = new TaskCompletionSource<TenantCommandSubmissionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ITenantQueryGateway reads = ConsumerLifecycleReads();
        ITenantCommandGateway commands = Substitute.For<ITenantCommandGateway>();
        CancellationToken deliveryToken = default;
        string? deliveredMessageId = null;
        commands.CreateTenantAsync(Arg.Any<CreateTenant>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                deliveredMessageId = call.ArgAt<string>(1);
                deliveryToken = call.ArgAt<CancellationToken>(2);
                entered.TrySetResult();
                return held.Task;
            });
        commands.GetStatusAsync(Arg.Any<TenantCommandTrackingHandle>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new TenantCommandStatusResult(CommandStatus.Received, HasVerifiedCommandIdentity: true)));
        RegisterFiniteWorkspaceServices(reads, new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized), commands);
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/tenants/tenants");
        IRenderedComponent<Router> router = RenderConsumerLifecycleRouter();
        IRenderedComponent<CreateTenantFlow> create = router.FindComponent<CreateTenantFlow>();
        router.WaitForAssertion(() => create.Instance.IsAuthorized.ShouldBeTrue());
        SetAcceptanceCreateIntent(create);
        CreateTenantFlow originalCreate = create.Instance;
        Task submit = create.Find("form").SubmitAsync();
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        TenantRetainedCommandAttempt attempt = ConsumerCreateField<TenantRetainedCommandAttempt>(originalCreate, "_retainedAttempt");
        TenantAggregateCommandLease lease = attempt.Lease;
        TenantCreateCommandSnapshot sent = originalCreate.Snapshot;
        TenantCreateAttemptTracker tracker = Services.GetRequiredService<TenantCreateAttemptTracker>();
        TenantCommandAttemptTracker attempts = Services.GetRequiredService<TenantCommandAttemptTracker>();
        TenantAggregateCommandAdmissionGate gate = Services.GetRequiredService<TenantAggregateCommandAdmissionGate>();
        deliveredMessageId.ShouldNotBeNullOrWhiteSpace();
        sent.MessageId.ShouldBe(deliveredMessageId);
        sent.State.ShouldBe(TenantCommandLifecycleState.RequestSent);
        sent.CorrelationId.ShouldBeNull();
        sent.BaselineTenantAbsent.ShouldBeTrue();
        attempt.DeliveryInFlight.ShouldBeTrue();
        lease.IsDispatchMarked.ShouldBeTrue();
        deliveryToken.ShouldBe(CancellationToken.None);

        try
        {
            await NavigateConsumerLifecycleAsync(router, navigation, mineScope);
            if (mineScope)
            {
                ConsumerCreateField<bool>(originalCreate, "_disposed").ShouldBeTrue();
                attempt.SurfaceOwner.ShouldBeNull();
            }
            else
            {
                router.FindComponent<CreateTenantFlow>().Instance.ShouldBeSameAs(originalCreate);
                attempt.SurfaceOwner.ShouldBeSameAs(originalCreate);
            }
            attempt.Lease.ShouldBeSameAs(lease);
            lease.IsReleased.ShouldBeFalse();
            attempt.DeliveryInFlight.ShouldBeTrue();
            attempts.Find<TenantCreateCommandSnapshot>("retained-create").ShouldBe(sent);
            deliveryToken.IsCancellationRequested.ShouldBeFalse();
            gate.TryAcquire(TenantCommandAggregateLock.ForTenant("retained-create"), new object()).ShouldBeFalse();
            object otherOwner = new();
            string otherKey = TenantCommandAggregateLock.ForTenant("unrelated-create");
            gate.TryAcquire(otherKey, otherOwner).ShouldBeTrue();
            gate.Release(otherKey, otherOwner);

            await router.InvokeAsync(() => navigation.NavigateTo("/tenants/tenants"));
            router.WaitForElement("[data-testid='tenants-create-submit']");
            create = router.FindComponent<CreateTenantFlow>();
            router.WaitForAssertion(() => create.Instance.IsAuthorized.ShouldBeTrue());
            if (mineScope)
            {
                create.Instance.ShouldNotBeSameAs(originalCreate);
                SetAcceptanceCreateIntent(create);
                await create.Find("form").SubmitAsync();
                ConsumerCreateField<TenantRetainedCommandAttempt>(create.Instance, "_retainedAttempt").ShouldBeSameAs(attempt);
            }
            else
            {
                create.Instance.ShouldBeSameAs(originalCreate);
                await create.Find("form").SubmitAsync();
            }
            _ = commands.Received(1).CreateTenantAsync(Arg.Any<CreateTenant>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
            _ = commands.DidNotReceive().GetStatusAsync(Arg.Any<TenantCommandTrackingHandle>(), Arg.Any<CancellationToken>());
            attempt.DeliveryInFlight.ShouldBeTrue();
            attempt.Lease.ShouldBeSameAs(lease);

            held.SetResult(TenantCommandSubmissionResult.Accepted(deliveredMessageId, "consumer-dispatched-correlation"));
            await submit.WaitAsync(TimeSpan.FromSeconds(5));
            router.WaitForAssertion(() =>
            {
                create.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Accepted);
                create.Instance.Snapshot.MessageId.ShouldBe(deliveredMessageId);
                create.Instance.Snapshot.CorrelationId.ShouldBe("consumer-dispatched-correlation");
                attempt.DeliveryInFlight.ShouldBeFalse();
            });
            TenantCreateCommandSnapshot accepted = attempts.Find<TenantCreateCommandSnapshot>("retained-create").ShouldNotBeNull();
            accepted.Intent.ShouldBe(sent.Intent);
            accepted.AttemptStartedAtUtc.ShouldBe(sent.AttemptStartedAtUtc);
            accepted.BaselineProjectionVersion.ShouldBe(sent.BaselineProjectionVersion);
            accepted.BaselineDetailProjectionVersion.ShouldBe(sent.BaselineDetailProjectionVersion);
            accepted.BaselineTenantAbsent.ShouldBe(sent.BaselineTenantAbsent);
            tracker.Find("retained-create").ShouldBe(new TenantCommandTrackingHandle(deliveredMessageId, "consumer-dispatched-correlation", "retained-create")
            {
                ExpectedDomain = Hexalith.Tenants.Contracts.Identity.TenantIdentity.Domain,
            });
            int priorStatusCalls = commands.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(ITenantCommandGateway.GetStatusAsync));
            await create.Find("form").SubmitAsync();
            _ = commands.Received(priorStatusCalls + 1).GetStatusAsync(
                Arg.Is<TenantCommandTrackingHandle>(handle => handle.MessageId == deliveredMessageId
                    && handle.CorrelationId == "consumer-dispatched-correlation" && handle.AggregateId == "retained-create"
                    && handle.ExpectedDomain == Hexalith.Tenants.Contracts.Identity.TenantIdentity.Domain),
                Arg.Is<CancellationToken>(token => token == CancellationToken.None));
            _ = commands.Received(1).CreateTenantAsync(
                Arg.Is<CreateTenant>(request => request == new CreateTenant("retained-create", "Retained create", "Exact retained intent")),
                deliveredMessageId, Arg.Is<CancellationToken>(token => token == CancellationToken.None));
            attempt.Lease.ShouldBeSameAs(lease);
            lease.IsDispatchMarked.ShouldBeTrue();
            lease.IsReleased.ShouldBeFalse();
            attempt.SurfaceOwner.ShouldBeSameAs(create.Instance);
            deliveryToken.IsCancellationRequested.ShouldBeFalse();
            clock.GetUtcNow().ShouldBe(DateTimeOffset.UnixEpoch);
        }
        finally
        {
            held.TrySetResult(TenantCommandSubmissionResult.Accepted(deliveredMessageId!, "consumer-dispatched-correlation"));
        }
    }

    private static T ConsumerCreateField<T>(CreateTenantFlow instance, string name)
        => (T)(typeof(CreateTenantFlow)
            .GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.GetValue(instance)
            ?? throw new InvalidOperationException($"Field {name} was not found."));

    private IRenderedComponent<Router> RenderConsumerLifecycleRouter()
    {
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        return Render<Router>(parameters => parameters
            .Add(component => component.AppAssembly, typeof(TenantsWorkspace).Assembly)
            .Add(component => component.Found, routeData => builder =>
            {
                builder.OpenComponent<RouteView>(0);
                builder.AddAttribute(1, "RouteData", routeData);
                builder.CloseComponent();
            }));
    }

    private static ITenantQueryGateway ConsumerLifecycleReads()
    {
        ITenantQueryGateway reads = FiniteWorkspaceReads();
        reads.GetMyTenantsAsync(Arg.Any<UserTenantMembershipRequest>(), Arg.Any<UserTenantMembershipSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(UserTenantMembershipSnapshot.Empty(true, ReadModelFreshnessState.Current, null, "operator.alpha")));
        return reads;
    }

    private static async Task NavigateConsumerLifecycleAsync(IRenderedComponent<Router> router, NavigationManager navigation, bool mineScope)
    {
        string target = mineScope ? "/tenants/tenants?scope=mine" : "/tenants/workspace-users";
        await router.InvokeAsync(() => navigation.NavigateTo(target));
        router.WaitForElement(mineScope ? "[data-testid='tenants-my-empty']" : "[data-testid='tenants-user-lookup-input']");
        navigation.Uri.ShouldBe("http://localhost" + target);
        TenantWorkspaceState state = PrivateField<TenantWorkspaceState>(router.FindComponent<TenantsWorkspace>().Instance, "_workspaceState");
        state.Tab.ShouldBe(mineScope ? TenantWorkspaceState.TenantsTab : TenantWorkspaceState.UsersTab);
        state.Scope.ShouldBe(mineScope ? TenantWorkspaceState.MyScope : TenantWorkspaceState.AllScope);
    }
}
