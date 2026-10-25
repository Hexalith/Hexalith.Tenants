using System.Security.Claims;

using Bunit;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
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
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests;

public sealed partial class TenantsWorkspaceTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task WorkspaceConsumerAuthenticationExpiryRequiresCurrentProviderRecovery(
        bool initialAuthentication,
        bool lateFault)
    {
        var clock = RegisterFiniteWorkspaceClock();
        var held = new TaskCompletionSource<AuthenticationState>(TaskCreationOptions.RunContinuationsAsynchronously);
        var authentication = new WorkspaceAcceptanceAuthenticationStateProvider(initialAuthentication
            ? held.Task : Task.FromResult(new AuthenticationState(AdministratorPrincipal())));
        int resolutions = 0;
        ValueTask<TenantLifecycleAuthorizationReflectionState> ResolveAsync(CancellationToken _)
        {
            resolutions++;
            return ValueTask.FromResult(TenantLifecycleAuthorizationReflectionState.Authorized);
        }

        ITenantQueryGateway reads = FiniteWorkspaceReads();
        ITenantCommandGateway commands = Substitute.For<ITenantCommandGateway>();
        Services.AddSingleton<AuthenticationStateProvider>(authentication);
        RegisterFiniteWorkspaceServices(reads, new StubTenantsBffComposition(cancellableResolver: ResolveAsync), commands);
        IRenderedComponent<TenantsWorkspace> cut = RenderWorkspace();
        if (!initialAuthentication)
        {
            cut.WaitForElement("[data-testid='tenants-global-administrators-entry']");
            authentication.Notify(held.Task);
        }
        int priorResolutions = resolutions;
        IRenderedComponent<CreateTenantFlow> create = cut.FindComponent<CreateTenantFlow>();
        cut.WaitForAssertion(() => AssertUnconfirmedWorkspace(cut));
        cut.Find("[data-testid='tenants-list-refresh']").NodeName.ShouldBe("FLUENT-BUTTON");
        await create.Find("form").SubmitAsync();
        resolutions.ShouldBe(priorResolutions, "Pending authentication cannot start create authorization.");
        await WaitForWorkspaceOperationAsync(() => clock.PendingTimerCount > 0);
        CancellationTokenSource initialSource = PrivateField<CancellationTokenSource>(cut.Instance, "_authenticationStateCancellation");
        clock.Advance(TimeSpan.FromSeconds(1));
        cut.WaitForAssertion(() =>
        {
            PrivateField<bool>(cut.Instance, "_auditAuthorityResolved").ShouldBeTrue();
            PrivateField<long>(cut.Instance, "_pendingAuditCallerVersion").ShouldBe(0);
            AssertUnconfirmedWorkspace(cut);
        });
        await WaitForWorkspaceOperationAsync(() => WorkspaceConsumerSourceIsDisposed(initialSource));
        await create.Find("form").SubmitAsync();
        resolutions.ShouldBe(priorResolutions, "Expired authentication cannot start create authorization.");
        commands.ReceivedCalls().ShouldBeEmpty();

        // The provider still owns the held task: Refresh must reread it rather than bypass expiry.
        await cut.Find("[data-testid='tenants-list-refresh']").ClickAsync(new MouseEventArgs());
        authentication.ReadCount.ShouldBe(2);
        await WaitForWorkspaceOperationAsync(() => clock.PendingTimerCount > 0);
        CancellationTokenSource retrySource = PrivateField<CancellationTokenSource>(cut.Instance, "_authenticationStateCancellation");
        resolutions.ShouldBe(priorResolutions);
        clock.Advance(TimeSpan.FromSeconds(1));
        cut.WaitForAssertion(() =>
        {
            PrivateField<bool>(cut.Instance, "_auditAuthorityResolved").ShouldBeTrue();
            AssertUnconfirmedWorkspace(cut);
        });
        await WaitForWorkspaceOperationAsync(() => WorkspaceConsumerSourceIsDisposed(retrySource));

        authentication.SetCurrent(Task.FromException<AuthenticationState>(new InvalidOperationException("current provider private detail")));
        await cut.Find("[data-testid='tenants-list-refresh']").ClickAsync(new MouseEventArgs());
        cut.WaitForAssertion(() => AssertUnconfirmedWorkspace(cut));
        authentication.ReadCount.ShouldBe(3);
        resolutions.ShouldBe(priorResolutions);
        cut.Markup.ShouldNotContain("current provider private detail");

        authentication.SetCurrent(Task.FromResult(new AuthenticationState(AdministratorPrincipal())));
        await cut.Find("[data-testid='tenants-list-refresh']").ClickAsync(new MouseEventArgs());
        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='tenants-global-administrators-entry']");
            create.Instance.IsAuthorized.ShouldBeTrue();
            create.Find("[data-testid='tenants-create-submit']").HasAttribute("disabled").ShouldBeFalse();
        });
        authentication.ReadCount.ShouldBe(4);
        resolutions.ShouldBe(priorResolutions + 1);
        _ = reads.Received(4).ListTenantsAsync(
            Arg.Is<TenantListRequest>(request => request.Cursor == null && request.PageSize == 20),
            Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>());
        long recoveredVersion = PrivateField<long>(cut.Instance, "_authorizationVersion");
        if (lateFault)
        {
            held.SetException(new InvalidOperationException("obsolete provider private detail"));
        }
        else
        {
            held.SetResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
        }
        await WaitForWorkspaceOperationAsync(() => clock.PendingTimerCount == 0);
        await cut.InvokeAsync(static () => Task.CompletedTask);
        PrivateField<long>(cut.Instance, "_authorizationVersion").ShouldBe(recoveredVersion);
        resolutions.ShouldBe(priorResolutions + 1);
        cut.Find("[data-testid='tenants-global-administrators-entry']");
        create.Instance.IsAuthorized.ShouldBeTrue();
        commands.ReceivedCalls().ShouldBeEmpty();
        cut.Markup.ShouldNotContain("obsolete provider private detail");
        clock.GetUtcNow().ShouldBe(DateTimeOffset.UnixEpoch.AddSeconds(2));
    }

    [Theory]
    [InlineData(TenantLifecycleAuthorizationReflectionState.Authorized)]
    [InlineData(TenantLifecycleAuthorizationReflectionState.MissingPermission)]
    public async Task WorkspaceConsumerAccessorChangeClosesEntryAuditAndCreateOnAnUnrelatedRender(
        TenantLifecycleAuthorizationReflectionState previousAuthority)
    {
        RegisterFiniteWorkspaceClock();
        var authentication = new WorkspaceAcceptanceAuthenticationStateProvider(
            Task.FromResult(new AuthenticationState(AdministratorPrincipal())));
        TenantListRow row = TenantListRow.FromSummary(new TenantSummary("tenant.alpha", "Alpha", TenantStatus.Active)) with
        {
            Freshness = ReadModelFreshnessState.Current,
            Lifecycle = ProjectionLifecycleState.Current,
        };
        ITenantQueryGateway reads = FiniteWorkspaceReads();
        reads.ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(TenantListSnapshot.Ready([row], null, false, null, ReadModelFreshnessState.Current, false)
                with { Lifecycle = ProjectionLifecycleState.Current }));
        ITenantCommandGateway commands = Substitute.For<ITenantCommandGateway>();
        Services.AddSingleton<AuthenticationStateProvider>(authentication);
        var composition = new StubTenantsBffComposition(previousAuthority);
        RegisterFiniteWorkspaceServices(reads, composition, commands);
        IRenderedComponent<TenantsWorkspace> cut = RenderWorkspace();
        cut.WaitForAssertion(() => cut.FindComponent<CreateTenantFlow>().Instance.AuthorizationReflection.ShouldBe(previousAuthority));
        if (previousAuthority is TenantLifecycleAuthorizationReflectionState.Authorized)
        {
            cut.Find("[data-testid='tenants-global-administrators-entry']");
            cut.Find("[data-testid='tenants-list-audit-entrypoint']").ParentElement.ShouldNotBeNull()
                .GetAttribute("href").ShouldNotBeNull();
        }
        else
        {
            cut.FindAll("[data-testid='tenants-list-audit-entrypoint']").ShouldBeEmpty();
            cut.FindComponent<CreateTenantFlow>().Instance.AuditReadDenied.ShouldBeTrue();
        }

        Services.GetRequiredService<IUserContextAccessor>().UserId.Returns("operator.beta");
        FluentSelect<int, int> pageSize = cut.FindComponents<FluentSelect<int, int>>()
            .Select(component => component.Instance).Single();
        await cut.InvokeAsync(() => pageSize.ValueChanged.InvokeAsync(50));
        cut.WaitForAssertion(() =>
        {
            AssertUnconfirmedWorkspace(cut);
            cut.FindComponent<CreateTenantFlow>().Instance.AuditReadDenied.ShouldBeFalse();
            cut.Find("[data-testid='tenants-list-audit-entrypoint']").ParentElement.ShouldNotBeNull()
                .GetAttribute("href").ShouldBeNull();
        });
        authentication.ReadCount.ShouldBe(1, "Changing page size must not manufacture an authentication event.");
        _ = reads.Received(1).ListTenantsAsync(Arg.Is<TenantListRequest>(request => request.PageSize == 50),
            Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>());

        authentication.SetCurrent(Task.FromResult(new AuthenticationState(AcceptanceBetaPrincipal())));
        composition.Reflection = TenantLifecycleAuthorizationReflectionState.Authorized;
        await cut.Find("[data-testid='tenants-list-refresh']").ClickAsync(new MouseEventArgs());
        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='tenants-global-administrators-entry']");
            cut.FindComponent<CreateTenantFlow>().Instance.IsAuthorized.ShouldBeTrue();
            PrivateField<string>(cut.Instance, "_createAuthorityUserId").ShouldBe("operator.beta");
        });
        commands.ReceivedCalls().ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task WorkspaceConsumerViewTransitionCancelsHeldCreateAndPermitsExactExplicitRetry(bool mineScope, bool lateFault)
    {
        var clock = RegisterFiniteWorkspaceClock();
        Services.AddSingleton<AuthenticationStateProvider>(new MutableAuthenticationStateProvider(AdministratorPrincipal()));
        var held = new TaskCompletionSource<TenantLifecycleAuthorizationReflectionState>(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        bool hold = false;
        CancellationToken suppliedToken = default;
        async ValueTask<TenantLifecycleAuthorizationReflectionState> ResolveAsync(CancellationToken token)
        {
            if (!hold)
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
        ITenantQueryGateway reads = FiniteWorkspaceReads();
        reads.GetMyTenantsAsync(Arg.Any<UserTenantMembershipRequest>(), Arg.Any<UserTenantMembershipSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(UserTenantMembershipSnapshot.Empty(true, ReadModelFreshnessState.Current, null, "operator.alpha")));
        reads.GetTenantAsync(Arg.Any<TenantDetailRequest>(), Arg.Any<TenantDetailSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(TenantDetailSnapshot.NotFound("retained-create")));
        ITenantCommandGateway commands = Substitute.For<ITenantCommandGateway>();
        commands.CreateTenantAsync(Arg.Any<CreateTenant>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(TenantCommandSubmissionResult.Accepted(call.ArgAt<string>(1), "retained-create-tracking")));
        commands.GetStatusAsync(Arg.Any<TenantCommandTrackingHandle>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new TenantCommandStatusResult(CommandStatus.Received, HasVerifiedCommandIdentity: true)));
        RegisterFiniteWorkspaceServices(reads, new StubTenantsBffComposition(cancellableResolver: ResolveAsync), commands);
        IRenderedComponent<TenantsWorkspace> cut = RenderWorkspace();
        IRenderedComponent<CreateTenantFlow> create = cut.FindComponent<CreateTenantFlow>();
        cut.WaitForAssertion(() => create.Instance.IsAuthorized.ShouldBeTrue());
        SetAcceptanceCreateIntent(create);
        CreateTenantFlow originalCreate = create.Instance;
        hold = true;
        Task submit = create.Find("form").SubmitAsync();
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await WaitForWorkspaceOperationAsync(() => clock.PendingTimerCount > 0);
        CancellationTokenSource originalSource = PrivateField<CancellationTokenSource>(cut.Instance, "_createAuthorizationCancellation");
        try
        {
            if (mineScope)
            {
                FluentSelectInterop.ChangeFluentSelect(cut, "tenants-workspace-scope", TenantWorkspaceState.MyScope);
                cut.WaitForElement("[data-testid='tenants-my-empty']");
                Services.GetRequiredService<NavigationManager>().Uri.ShouldBe("http://localhost/tenants/tenants?scope=mine");
                PrivateField<TenantWorkspaceState>(cut.Instance, "_workspaceState").Scope.ShouldBe(TenantWorkspaceState.MyScope);
                _ = reads.Received(1).GetMyTenantsAsync(
                    Arg.Is<UserTenantMembershipRequest>(request => request.TargetUserId == null && request.Cursor == null && request.PageSize == 20),
                    Arg.Any<UserTenantMembershipSnapshot?>(), Arg.Any<CancellationToken>());
            }
            else
            {
                await cut.InvokeAsync(() => cut.FindComponent<FluentTabs>().Instance.ActiveTabIdChanged.InvokeAsync("workspace-users"));
                cut.WaitForElement("[data-testid='tenants-user-lookup-input']");
                Services.GetRequiredService<NavigationManager>().Uri.ShouldBe("http://localhost/tenants/workspace-users");
                cut.FindComponent<CreateTenantFlow>().Instance.ShouldBeSameAs(originalCreate);
                cut.FindComponent<CreateTenantFlow>().Instance.IsAuthorized.ShouldBeFalse();
            }
            cut.WaitForAssertion(() => suppliedToken.IsCancellationRequested.ShouldBeTrue());
            await submit.WaitAsync(TimeSpan.FromSeconds(5));
            commands.ReceivedCalls().ShouldBeEmpty();
            originalCreate.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Idle);
            originalCreate.Snapshot.MessageId.ShouldBeNull();
            await WaitForWorkspaceOperationAsync(() => WorkspaceConsumerSourceIsDisposed(originalSource));

            // Establish a new accepted attempt while the obsolete provider still owns its raw task.
            hold = false;
            if (mineScope)
            {
                FluentSelectInterop.ChangeFluentSelect(cut, "tenants-workspace-scope", TenantWorkspaceState.AllScope);
                cut.WaitForElement("[data-testid='tenants-create-submit']");
                create = cut.FindComponent<CreateTenantFlow>();
                SetAcceptanceCreateIntent(create);
            }
            else
            {
                await cut.InvokeAsync(() => cut.FindComponent<FluentTabs>().Instance.ActiveTabIdChanged.InvokeAsync(TenantWorkspaceState.TenantsTab));
                cut.FindComponent<CreateTenantFlow>().Instance.ShouldBeSameAs(create.Instance);
                create.Find("[data-testid='tenants-create-tenant-id']").GetAttribute("value").ShouldBe("retained-create");
                create.Find("[data-testid='tenants-create-name']").GetAttribute("value").ShouldBe("Retained create");
            }
            await cut.Find("[data-testid='tenants-list-refresh']").ClickAsync(new MouseEventArgs());
            cut.WaitForAssertion(() => create.Find("[data-testid='tenants-create-submit']").HasAttribute("disabled").ShouldBeFalse());
            await create.Find("form").SubmitAsync();
            _ = commands.Received(1).CreateTenantAsync(
                Arg.Is<CreateTenant>(request => request == new CreateTenant("retained-create", "Retained create", "Exact retained intent")),
                Arg.Is<string>(actual => !string.IsNullOrWhiteSpace(actual)), Arg.Any<CancellationToken>());
            create.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Accepted);
            create.Instance.Snapshot.CorrelationId.ShouldBe("retained-create-tracking");
            string messageId = create.Instance.Snapshot.MessageId.ShouldNotBeNull();
            string correlationId = create.Instance.Snapshot.CorrelationId.ShouldNotBeNull();
            if (lateFault)
            {
                held.SetException(new InvalidOperationException("obsolete view authority detail"));
            }
            else
            {
                held.SetResult(TenantLifecycleAuthorizationReflectionState.Authorized);
            }
            await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await WaitForWorkspaceOperationAsync(() => clock.PendingTimerCount == 0);
            await cut.InvokeAsync(static () => Task.CompletedTask);
            create.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Accepted);
            create.Instance.Snapshot.MessageId.ShouldBe(messageId);
            create.Instance.Snapshot.CorrelationId.ShouldBe(correlationId);
            _ = commands.Received(1).CreateTenantAsync(
                Arg.Is<CreateTenant>(request => request == new CreateTenant("retained-create", "Retained create", "Exact retained intent")),
                Arg.Is<string>(actual => actual == messageId), Arg.Any<CancellationToken>());
            cut.Markup.ShouldNotContain("obsolete view authority detail");
            clock.GetUtcNow().ShouldBe(DateTimeOffset.UnixEpoch);
        }
        finally
        {
            held.TrySetResult(TenantLifecycleAuthorizationReflectionState.MissingPermission);
        }
    }

    [Theory]
    [InlineData("entry", false)]
    [InlineData("entry", true)]
    [InlineData("event", false)]
    [InlineData("event", true)]
    [InlineData("create", false)]
    [InlineData("create", true)]
    public async Task WorkspaceConsumerActiveReplacementAndDisposalIgnoreLateFaults(string operation, bool dispose)
    {
        var clock = RegisterFiniteWorkspaceClock();
        var authentication = new WorkspaceAcceptanceAuthenticationStateProvider(
            Task.FromResult(new AuthenticationState(AdministratorPrincipal())));
        var held = new TaskCompletionSource<TenantLifecycleAuthorizationReflectionState>(TaskCreationOptions.RunContinuationsAsynchronously);
        var eventState = new TaskCompletionSource<AuthenticationState>(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        bool hold = operation is "entry";
        int resolutions = 0;
        CancellationToken providerToken = default;
        async ValueTask<TenantLifecycleAuthorizationReflectionState> ResolveAsync(CancellationToken token)
        {
            resolutions++;
            if (!hold)
            {
                return TenantLifecycleAuthorizationReflectionState.Authorized;
            }
            providerToken = token;
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
        ITenantQueryGateway reads = FiniteWorkspaceReads();
        ITenantCommandGateway commands = Substitute.For<ITenantCommandGateway>();
        Services.AddSingleton<AuthenticationStateProvider>(authentication);
        RegisterFiniteWorkspaceServices(reads, new StubTenantsBffComposition(cancellableResolver: ResolveAsync), commands);
        IRenderedComponent<TenantsWorkspace> cut = RenderWorkspace();
        IRenderedComponent<CreateTenantFlow> create = cut.FindComponent<CreateTenantFlow>();
        Task? submit = null;
        if (operation is "event")
        {
            cut.WaitForElement("[data-testid='tenants-global-administrators-entry']");
            authentication.Notify(eventState.Task);
        }
        else if (operation is "create")
        {
            cut.WaitForAssertion(() => create.Instance.IsAuthorized.ShouldBeTrue());
            SetAcceptanceCreateIntent(create);
            hold = true;
            submit = create.Find("form").SubmitAsync();
        }
        if (operation is not "event")
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        await WaitForWorkspaceOperationAsync(() => clock.PendingTimerCount > 0);
        CancellationTokenSource activeSource = PrivateField<CancellationTokenSource>(cut.Instance, operation switch
        {
            "event" => "_authenticationStateCancellation",
            "create" => "_createAuthorizationCancellation",
            _ => "_entryAuthorizationCancellation",
        });
        CancellationToken activeToken = operation is "event"
            ? activeSource.Token
            : providerToken;
        activeToken.IsCancellationRequested.ShouldBeFalse();
        hold = false;
        if (dispose)
        {
            await cut.InvokeAsync(() =>
            {
                cut.Instance.Dispose();
                create.Instance.Dispose();
            });
        }
        else
        {
            Services.GetRequiredService<IUserContextAccessor>().UserId.Returns("operator.beta");
            authentication.Notify(AcceptanceBetaPrincipal());
            cut.WaitForAssertion(() =>
            {
                PrivateField<string>(cut.Instance, "_createAuthorityUserId").ShouldBe("operator.beta");
                cut.Find("[data-testid='tenants-global-administrators-entry']");
            });
        }
        activeToken.IsCancellationRequested.ShouldBeTrue();
        if (submit is not null)
        {
            await submit.WaitAsync(TimeSpan.FromSeconds(5));
            create.Instance.Snapshot.MessageId.ShouldBeNull();
        }
        await WaitForWorkspaceOperationAsync(() => WorkspaceConsumerSourceIsDisposed(activeSource));
        long currentVersion = PrivateField<long>(cut.Instance, "_authorizationVersion");
        int currentResolutions = resolutions;
        if (operation is "event")
        {
            eventState.SetException(new InvalidOperationException("obsolete active event detail"));
        }
        else
        {
            held.SetException(new InvalidOperationException("obsolete active resolver detail"));
            await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        await WaitForWorkspaceOperationAsync(() => clock.PendingTimerCount == 0);
        await cut.InvokeAsync(static () => Task.CompletedTask);
        resolutions.ShouldBe(currentResolutions);
        PrivateField<long>(cut.Instance, "_authorizationVersion").ShouldBe(currentVersion);
        commands.ReceivedCalls().ShouldBeEmpty();
        _ = reads.Received(dispose ? 1 : 2).ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>());
        if (!dispose)
        {
            cut.Find("[data-testid='tenants-global-administrators-entry']");
            create.Instance.IsAuthorized.ShouldBeTrue();
            cut.Markup.ShouldNotContain("obsolete active");
        }
        clock.GetUtcNow().ShouldBe(DateTimeOffset.UnixEpoch);
    }

    [Fact]
    public async Task WorkspaceConsumerSynchronousRecoveryFailureCancelsPriorAuthenticationBeforeExpiry()
    {
        var clock = RegisterFiniteWorkspaceClock();
        var held = new TaskCompletionSource<AuthenticationState>(TaskCreationOptions.RunContinuationsAsynchronously);
        var authentication = new WorkspaceAcceptanceAuthenticationStateProvider(held.Task);
        int resolutions = 0;
        ValueTask<TenantLifecycleAuthorizationReflectionState> ResolveAsync(CancellationToken _)
        {
            resolutions++;
            return ValueTask.FromResult(TenantLifecycleAuthorizationReflectionState.Authorized);
        }
        ITenantQueryGateway reads = FiniteWorkspaceReads();
        ITenantCommandGateway commands = Substitute.For<ITenantCommandGateway>();
        Services.AddSingleton<AuthenticationStateProvider>(authentication);
        RegisterFiniteWorkspaceServices(reads, new StubTenantsBffComposition(cancellableResolver: ResolveAsync), commands);
        IRenderedComponent<TenantsWorkspace> cut = RenderWorkspace();
        await WaitForWorkspaceOperationAsync(() => clock.PendingTimerCount > 0);
        CancellationTokenSource originalSource = PrivateField<CancellationTokenSource>(cut.Instance, "_authenticationStateCancellation");
        CancellationToken originalToken = originalSource.Token;
        authentication.ReadFailure = new InvalidOperationException("synchronous provider private detail");

        await cut.Find("[data-testid='tenants-list-refresh']").ClickAsync(new MouseEventArgs());
        cut.WaitForAssertion(() =>
        {
            AssertUnconfirmedWorkspace(cut);
            PrivateField<bool>(cut.Instance, "_auditAuthorityResolved").ShouldBeTrue();
            PrivateField<long>(cut.Instance, "_pendingAuditCallerVersion").ShouldBe(0);
        });
        originalToken.IsCancellationRequested.ShouldBeTrue();
        await WaitForWorkspaceOperationAsync(() => WorkspaceConsumerSourceIsDisposed(originalSource));
        await cut.FindComponent<CreateTenantFlow>().Find("form").SubmitAsync();
        resolutions.ShouldBe(0);
        authentication.ReadCount.ShouldBe(2);
        commands.ReceivedCalls().ShouldBeEmpty();
        cut.Markup.ShouldNotContain("synchronous provider private detail");
        clock.PendingTimerCount.ShouldBe(0);
        clock.GetUtcNow().ShouldBe(DateTimeOffset.UnixEpoch);

        authentication.ReadFailure = null;
        authentication.SetCurrent(Task.FromResult(new AuthenticationState(AdministratorPrincipal())));
        await cut.Find("[data-testid='tenants-list-refresh']").ClickAsync(new MouseEventArgs());
        cut.WaitForElement("[data-testid='tenants-global-administrators-entry']");
        cut.FindComponent<CreateTenantFlow>().Instance.IsAuthorized.ShouldBeTrue();
        authentication.ReadCount.ShouldBe(3);
        resolutions.ShouldBe(1);
        held.SetException(new InvalidOperationException("obsolete synchronous recovery detail"));
        await cut.InvokeAsync(static () => Task.CompletedTask);
        cut.Find("[data-testid='tenants-global-administrators-entry']");
        resolutions.ShouldBe(1);
        commands.ReceivedCalls().ShouldBeEmpty();
        _ = reads.Received(3).ListTenantsAsync(
            Arg.Is<TenantListRequest>(request => request.Cursor == null && request.PageSize == 20),
            Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task WorkspaceConsumerRefreshReplacesPendingCurrentProviderBeforeExpiry(bool initialAuthentication, bool lateFault)
    {
        var clock = RegisterFiniteWorkspaceClock();
        var held = new TaskCompletionSource<AuthenticationState>(TaskCreationOptions.RunContinuationsAsynchronously);
        var authentication = new WorkspaceAcceptanceAuthenticationStateProvider(initialAuthentication
            ? held.Task : Task.FromResult(new AuthenticationState(AdministratorPrincipal())));
        int resolutions = 0;
        ValueTask<TenantLifecycleAuthorizationReflectionState> ResolveAsync(CancellationToken _)
        {
            resolutions++;
            return ValueTask.FromResult(TenantLifecycleAuthorizationReflectionState.Authorized);
        }
        ITenantQueryGateway reads = FiniteWorkspaceReads();
        ITenantCommandGateway commands = Substitute.For<ITenantCommandGateway>();
        Services.AddSingleton<AuthenticationStateProvider>(authentication);
        RegisterFiniteWorkspaceServices(reads, new StubTenantsBffComposition(cancellableResolver: ResolveAsync), commands);
        IRenderedComponent<TenantsWorkspace> cut = RenderWorkspace();
        if (!initialAuthentication)
        {
            cut.WaitForElement("[data-testid='tenants-global-administrators-entry']");
            authentication.Notify(held.Task);
        }
        cut.WaitForAssertion(() => AssertUnconfirmedWorkspace(cut));
        await WaitForWorkspaceOperationAsync(() => clock.PendingTimerCount > 0);
        CancellationTokenSource originalSource = PrivateField<CancellationTokenSource>(cut.Instance, "_authenticationStateCancellation");
        CancellationToken originalToken = originalSource.Token;
        int priorResolutions = resolutions;
        Services.GetRequiredService<IUserContextAccessor>().UserId.Returns("operator.beta");
        authentication.SetCurrent(Task.FromResult(new AuthenticationState(AcceptanceBetaPrincipal())));

        await cut.Find("[data-testid='tenants-list-refresh']").ClickAsync(new MouseEventArgs());
        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='tenants-global-administrators-entry']");
            cut.FindComponent<CreateTenantFlow>().Instance.IsAuthorized.ShouldBeTrue();
            PrivateField<string>(cut.Instance, "_createAuthorityUserId").ShouldBe("operator.beta");
            PrivateField<long>(cut.Instance, "_pendingAuditCallerVersion").ShouldBe(0);
        });
        originalToken.IsCancellationRequested.ShouldBeTrue();
        await WaitForWorkspaceOperationAsync(() => WorkspaceConsumerSourceIsDisposed(originalSource));
        authentication.ReadCount.ShouldBe(2);
        resolutions.ShouldBe(priorResolutions + 1);
        long recoveredVersion = PrivateField<long>(cut.Instance, "_authorizationVersion");
        if (lateFault)
        {
            held.SetException(new InvalidOperationException("obsolete pending refresh detail"));
        }
        else
        {
            held.SetResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
        }
        await cut.InvokeAsync(static () => Task.CompletedTask);
        PrivateField<long>(cut.Instance, "_authorizationVersion").ShouldBe(recoveredVersion);
        PrivateField<string>(cut.Instance, "_createAuthorityUserId").ShouldBe("operator.beta");
        cut.Find("[data-testid='tenants-global-administrators-entry']");
        cut.FindComponent<CreateTenantFlow>().Instance.IsAuthorized.ShouldBeTrue();
        authentication.ReadCount.ShouldBe(2);
        resolutions.ShouldBe(priorResolutions + 1);
        commands.ReceivedCalls().ShouldBeEmpty();
        _ = reads.Received(2).ListTenantsAsync(
            Arg.Is<TenantListRequest>(request => request.Cursor == null && request.PageSize == 20),
            Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>());
        cut.Markup.ShouldNotContain("obsolete pending refresh detail");
        clock.PendingTimerCount.ShouldBe(0);
        clock.GetUtcNow().ShouldBe(DateTimeOffset.UnixEpoch);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WorkspaceConsumerDelegatedAuthorityRefreshReplacesHeldEntryBeforeExpiry(bool lateFault)
    {
        var clock = RegisterFiniteWorkspaceClock();
        var authentication = new WorkspaceAcceptanceAuthenticationStateProvider(
            Task.FromResult(new AuthenticationState(AdministratorPrincipal())));
        var held = new TaskCompletionSource<TenantLifecycleAuthorizationReflectionState>(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        bool hold = true;
        int resolutions = 0;
        CancellationToken providerToken = default;
        async ValueTask<TenantLifecycleAuthorizationReflectionState> ResolveAsync(CancellationToken token)
        {
            resolutions++;
            if (!hold)
            {
                return TenantLifecycleAuthorizationReflectionState.Authorized;
            }
            providerToken = token;
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
        ITenantQueryGateway reads = FiniteWorkspaceReads();
        ITenantCommandGateway commands = Substitute.For<ITenantCommandGateway>();
        Services.AddSingleton<AuthenticationStateProvider>(authentication);
        RegisterFiniteWorkspaceServices(reads, new StubTenantsBffComposition(cancellableResolver: ResolveAsync), commands);
        IRenderedComponent<TenantsWorkspace> cut = RenderWorkspace();
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await WaitForWorkspaceOperationAsync(() => clock.PendingTimerCount > 0);
        CancellationTokenSource originalSource = PrivateField<CancellationTokenSource>(cut.Instance, "_entryAuthorizationCancellation");
        Func<Task> refresh = cut.FindComponents<CascadingValue<Func<Task>>>()
            .Single(component => component.Instance.Name == "AuditAuthorityRefresh").Instance.Value.ShouldNotBeNull();
        hold = false;

        await cut.InvokeAsync(refresh);
        cut.WaitForElement("[data-testid='tenants-global-administrators-entry']");
        providerToken.IsCancellationRequested.ShouldBeTrue();
        await WaitForWorkspaceOperationAsync(() => WorkspaceConsumerSourceIsDisposed(originalSource));
        resolutions.ShouldBe(2);
        authentication.ReadCount.ShouldBe(1);
        long recoveredVersion = PrivateField<long>(cut.Instance, "_authorizationVersion");
        if (lateFault)
        {
            held.SetException(new InvalidOperationException("obsolete delegated entry detail"));
        }
        else
        {
            held.SetResult(TenantLifecycleAuthorizationReflectionState.MissingPermission);
        }
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await cut.InvokeAsync(static () => Task.CompletedTask);
        cut.Find("[data-testid='tenants-global-administrators-entry']");
        cut.FindComponent<CreateTenantFlow>().Instance.IsAuthorized.ShouldBeTrue();
        PrivateField<long>(cut.Instance, "_authorizationVersion").ShouldBe(recoveredVersion);
        resolutions.ShouldBe(2);
        authentication.ReadCount.ShouldBe(1);
        commands.ReceivedCalls().ShouldBeEmpty();
        _ = reads.Received(1).ListTenantsAsync(
            Arg.Is<TenantListRequest>(request => request.Cursor == null && request.PageSize == 20),
            Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>());
        cut.Markup.ShouldNotContain("obsolete delegated entry detail");
        clock.PendingTimerCount.ShouldBe(0);
        clock.GetUtcNow().ShouldBe(DateTimeOffset.UnixEpoch);
    }

    [Fact]
    public async Task WorkspaceConsumerRetainedAuthorityRefreshCannotStartPostDisposalWork()
    {
        var clock = RegisterFiniteWorkspaceClock();
        var authentication = new WorkspaceAcceptanceAuthenticationStateProvider(
            Task.FromResult(new AuthenticationState(AdministratorPrincipal())));
        int resolutions = 0;
        ValueTask<TenantLifecycleAuthorizationReflectionState> ResolveAsync(CancellationToken _)
        {
            resolutions++;
            return ValueTask.FromResult(TenantLifecycleAuthorizationReflectionState.Authorized);
        }
        ITenantQueryGateway reads = FiniteWorkspaceReads();
        Services.AddSingleton<AuthenticationStateProvider>(authentication);
        RegisterFiniteWorkspaceServices(reads, new StubTenantsBffComposition(cancellableResolver: ResolveAsync));
        IRenderedComponent<TenantsWorkspace> cut = RenderWorkspace();
        cut.WaitForElement("[data-testid='tenants-global-administrators-entry']");
        Func<Task> refresh = cut.FindComponents<CascadingValue<Func<Task>>>()
            .Single(component => component.Instance.Name == "AuditAuthorityRefresh").Instance.Value.ShouldNotBeNull();
        await cut.InvokeAsync(cut.Instance.Dispose);
        long authorityVersion = PrivateField<long>(cut.Instance, "_authorizationVersion");
        long refreshVersion = PrivateField<long>(cut.Instance, "_auditRefreshVersion");
        int callerReads = authentication.ReadCount;
        int callerResolutions = resolutions;
        await cut.InvokeAsync(refresh);
        authentication.Notify(AcceptanceBetaPrincipal());
        PrivateField<long>(cut.Instance, "_authorizationVersion").ShouldBe(authorityVersion);
        PrivateField<long>(cut.Instance, "_auditRefreshVersion").ShouldBe(refreshVersion);
        authentication.ReadCount.ShouldBe(callerReads);
        resolutions.ShouldBe(callerResolutions);
        clock.PendingTimerCount.ShouldBe(0);
        _ = reads.Received(1).ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>());
    }

    private static void AssertUnconfirmedWorkspace(IRenderedComponent<TenantsWorkspace> cut)
    {
        cut.FindAll("[data-testid='tenants-global-administrators-entry']").ShouldBeEmpty();
        IRenderedComponent<CreateTenantFlow> create = cut.FindComponent<CreateTenantFlow>();
        create.Instance.IsAuthorized.ShouldBeFalse();
        create.Instance.AuthorizationReflection.ShouldBe(TenantLifecycleAuthorizationReflectionState.Indeterminate);
        create.Find("[data-testid='tenants-create-submit']").HasAttribute("disabled").ShouldBeTrue();
    }

    private static bool WorkspaceConsumerSourceIsDisposed(CancellationTokenSource source)
    {
        try
        {
            _ = source.Token;
            return false;
        }
        catch (ObjectDisposedException)
        {
            return true;
        }
    }

    private static void SetAcceptanceCreateIntent(IRenderedComponent<CreateTenantFlow> create)
    {
        create.Find("[data-testid='tenants-create-tenant-id']").Change("retained-create");
        create.Find("[data-testid='tenants-create-name']").Change("Retained create");
        create.Find("[data-testid='tenants-create-description']").Change("Exact retained intent");
    }

    private static ClaimsPrincipal AcceptanceBetaPrincipal()
        => new(new ClaimsIdentity(
            [new Claim("sub", "operator.beta"), new Claim("global_admin", "true"), new Claim("eventstore:tenant", "system")], "test"));
}
