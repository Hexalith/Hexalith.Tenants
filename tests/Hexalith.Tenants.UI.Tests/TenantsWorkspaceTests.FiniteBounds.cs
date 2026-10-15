using System.Security.Claims;

using Bunit;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.UI.Components.Pages;
using Hexalith.Tenants.UI.Components.Tenants;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
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
    public async Task WorkspaceFiniteResolverBoundSettlesIndeterminateAndRecoversWithoutObsoleteAuthority(
        bool ignoreCancellation,
        bool lateFault)
    {
        var clock = RegisterFiniteWorkspaceClock();
        var authentication = new MutableAuthenticationStateProvider(AdministratorPrincipal());
        var held = new TaskCompletionSource<TenantLifecycleAuthorizationReflectionState>(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken suppliedToken = default;
        bool hold = true;
        int resolutions = 0;
        async ValueTask<TenantLifecycleAuthorizationReflectionState> ResolveAsync(CancellationToken token)
        {
            resolutions++;
            if (!hold)
            {
                return TenantLifecycleAuthorizationReflectionState.Authorized;
            }

            suppliedToken = token;
            entered.TrySetResult();
            try
            {
                return ignoreCancellation
                    ? await held.Task.ConfigureAwait(false)
                    : await held.Task.WaitAsync(token).ConfigureAwait(false);
            }
            finally
            {
                completed.TrySetResult();
            }
        }

        ITenantQueryGateway reads = FiniteWorkspaceReads();
        Services.AddSingleton<AuthenticationStateProvider>(authentication);
        RegisterFiniteWorkspaceServices(reads, new StubTenantsBffComposition(cancellableResolver: ResolveAsync));
        IRenderedComponent<TenantsWorkspace> cut = RenderWorkspace();
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cut.Find("[data-testid='tenants-list-refresh']").NodeName.ShouldBe("FLUENT-BUTTON");
        cut.FindAll("[data-testid='tenants-global-administrators-entry']").ShouldBeEmpty();
        await WaitForWorkspaceOperationAsync(() => clock.PendingTimerCount > 0);

        clock.Advance(TimeSpan.FromSeconds(1));
        cut.WaitForAssertion(() =>
        {
            PrivateField<bool>(cut.Instance, "_auditAuthorityResolved").ShouldBeTrue();
            PrivateField<long>(cut.Instance, "_pendingAuditCallerVersion").ShouldBe(0);
            cut.FindComponent<CreateTenantFlow>().Instance.AuthorizationReflection
                .ShouldBe(TenantLifecycleAuthorizationReflectionState.Indeterminate);
            cut.Find("[data-testid='tenants-create-submit']").HasAttribute("disabled").ShouldBeTrue();
            cut.FindAll("[data-testid='tenants-global-administrators-entry']").ShouldBeEmpty();
        });
        suppliedToken.CanBeCanceled.ShouldBeTrue();
        suppliedToken.IsCancellationRequested.ShouldBeTrue();
        _ = reads.Received(1).ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>());

        hold = false;
        authentication.Set(AdministratorPrincipal());
        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='tenants-global-administrators-entry']");
            cut.FindComponent<CreateTenantFlow>().Instance.AuthorizationReflection
                .ShouldBe(TenantLifecycleAuthorizationReflectionState.Authorized);
        });
        int currentResolutions = resolutions;
        if (ignoreCancellation && lateFault)
        {
            held.SetException(new InvalidOperationException("obsolete workspace principal detail"));
        }
        else
        {
            held.SetResult(TenantLifecycleAuthorizationReflectionState.MissingPermission);
        }

        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await cut.InvokeAsync(static () => Task.CompletedTask);
        resolutions.ShouldBe(currentResolutions);
        cut.Find("[data-testid='tenants-global-administrators-entry']");
        cut.FindComponent<CreateTenantFlow>().Instance.IsAuthorized.ShouldBeTrue();
        cut.Markup.ShouldNotContain("obsolete workspace principal detail");
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task WorkspaceFiniteAuthenticationEventBoundReleasesPendingCallerAndIgnoresLateCompletion(
        bool disposeBeforeCompletion,
        bool lateFault)
    {
        var clock = RegisterFiniteWorkspaceClock();
        var authentication = new MutableAuthenticationStateProvider(AdministratorPrincipal());
        var composition = new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized);
        ITenantQueryGateway reads = FiniteWorkspaceReads();
        Services.AddSingleton<AuthenticationStateProvider>(authentication);
        RegisterFiniteWorkspaceServices(reads, composition);
        IRenderedComponent<TenantsWorkspace> cut = RenderWorkspace();
        cut.WaitForElement("[data-testid='tenants-global-administrators-entry']");

        TaskCompletionSource<AuthenticationState> pending = authentication.NotifyPending();
        cut.WaitForAssertion(() =>
        {
            cut.FindAll("[data-testid='tenants-global-administrators-entry']").ShouldBeEmpty();
            cut.FindComponent<CreateTenantFlow>().Instance.IsAuthorized.ShouldBeFalse();
        });
        await WaitForWorkspaceOperationAsync(() => clock.PendingTimerCount > 0);
        clock.Advance(TimeSpan.FromSeconds(1));
        cut.WaitForAssertion(() =>
        {
            PrivateField<bool>(cut.Instance, "_auditAuthorityResolved").ShouldBeTrue();
            PrivateField<long>(cut.Instance, "_pendingAuditCallerVersion").ShouldBe(0);
            cut.FindComponent<CreateTenantFlow>().Instance.AuthorizationReflection
                .ShouldBe(TenantLifecycleAuthorizationReflectionState.Indeterminate);
            cut.FindAll("[data-testid='tenants-global-administrators-entry']").ShouldBeEmpty();
        });
        _ = reads.Received(1).ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>());

        authentication.Set(AdministratorPrincipal());
        cut.WaitForElement("[data-testid='tenants-global-administrators-entry']");
        cut.FindComponent<CreateTenantFlow>().Instance.IsAuthorized.ShouldBeTrue();
        TenantsWorkspace instance = cut.Instance;
        if (disposeBeforeCompletion)
        {
            await cut.InvokeAsync(instance.Dispose);
        }

        long currentVersion = PrivateField<long>(instance, "_authorizationVersion");
        if (lateFault)
        {
            pending.SetException(new InvalidOperationException("obsolete authentication event detail"));
        }
        else
        {
            pending.SetResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
        }

        await cut.InvokeAsync(static () => Task.CompletedTask);
        PrivateField<long>(instance, "_authorizationVersion").ShouldBe(currentVersion);
        if (disposeBeforeCompletion)
        {
            PrivateField<bool>(instance, "_disposed").ShouldBeTrue();
            authentication.Set(NonAdministratorPrincipal());
            PrivateField<long>(instance, "_authorizationVersion").ShouldBe(currentVersion);
        }
        else
        {
            cut.Find("[data-testid='tenants-global-administrators-entry']");
            cut.FindComponent<CreateTenantFlow>().Instance.IsAuthorized.ShouldBeTrue();
            cut.Markup.ShouldNotContain("obsolete authentication event detail");
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WorkspaceSharedResolverDeadlineReleasesActualCreateAdmissionForExplicitRetry(bool lateFault)
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
        reads.GetTenantAsync(Arg.Any<TenantDetailRequest>(), Arg.Any<TenantDetailSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(TenantDetailSnapshot.NotFound("finite-create")));
        ITenantCommandGateway commands = Substitute.For<ITenantCommandGateway>();
        commands.CreateTenantAsync(Arg.Any<CreateTenant>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(TenantCommandSubmissionResult.Accepted(call.ArgAt<string>(1), "finite-tracking")));
        commands.GetStatusAsync(Arg.Any<TenantCommandTrackingHandle>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new TenantCommandStatusResult(CommandStatus.Received, HasVerifiedCommandIdentity: true)));
        RegisterFiniteWorkspaceServices(reads, new StubTenantsBffComposition(cancellableResolver: ResolveAsync), commands);
        IRenderedComponent<TenantsWorkspace> cut = RenderWorkspace();
        IRenderedComponent<CreateTenantFlow> create = cut.FindComponent<CreateTenantFlow>();
        cut.WaitForAssertion(() => create.Instance.IsAuthorized.ShouldBeTrue());
        create.Find("[data-testid='tenants-create-tenant-id']").Change("finite-create");
        create.Find("[data-testid='tenants-create-name']").Change("Finite create");
        hold = true;
        Task firstSubmit = create.Find("form").SubmitAsync();
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await WaitForWorkspaceOperationAsync(() => clock.PendingTimerCount > 0);
        commands.ReceivedCalls().ShouldBeEmpty();

        clock.Advance(TimeSpan.FromSeconds(1));
        await firstSubmit.WaitAsync(TimeSpan.FromSeconds(5));
        suppliedToken.IsCancellationRequested.ShouldBeTrue();
        create.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Idle);
        create.Instance.Snapshot.MessageId.ShouldBeNull();
        create.Find("[data-testid='tenants-create-unavailable-reason']").TextContent.ShouldContain("could not be verified");
        commands.ReceivedCalls().ShouldBeEmpty();
        hold = false;
        await cut.Find("[data-testid='tenants-list-refresh']")
            .ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        cut.WaitForAssertion(() => create.Find("[data-testid='tenants-create-submit']").HasAttribute("disabled").ShouldBeFalse());
        create.Find("[data-testid='tenants-create-tenant-id']").GetAttribute("value").ShouldBe("finite-create");
        await create.Find("form").SubmitAsync();
        _ = commands.Received(1).CreateTenantAsync(
            Arg.Is<CreateTenant>(command => command.TenantId == "finite-create"), Arg.Any<string>(), Arg.Any<CancellationToken>());
        create.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Accepted);
        string messageId = create.Instance.Snapshot.MessageId.ShouldNotBeNull();

        if (lateFault)
        {
            held.SetException(new InvalidOperationException("obsolete create authority detail"));
        }
        else
        {
            held.SetResult(TenantLifecycleAuthorizationReflectionState.Authorized);
        }

        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await cut.InvokeAsync(static () => Task.CompletedTask);
        create.Instance.Snapshot.MessageId.ShouldBe(messageId);
        create.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Accepted);
        _ = commands.Received(1).CreateTenantAsync(Arg.Any<CreateTenant>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        cut.Markup.ShouldNotContain("obsolete create authority detail");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WorkspaceCancelsActivelyHeldResolverOnCallerReplacementOrDisposalBeforeDeadline(bool dispose)
    {
        var clock = RegisterFiniteWorkspaceClock();
        var authentication = new MutableAuthenticationStateProvider(AdministratorPrincipal());
        var held = new TaskCompletionSource<TenantLifecycleAuthorizationReflectionState>(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken suppliedToken = default;
        bool hold = true;
        int resolutions = 0;
        async ValueTask<TenantLifecycleAuthorizationReflectionState> ResolveAsync(CancellationToken token)
        {
            resolutions++;
            if (!hold)
            {
                return TenantLifecycleAuthorizationReflectionState.MissingPermission;
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
        Services.AddSingleton<AuthenticationStateProvider>(authentication);
        RegisterFiniteWorkspaceServices(reads, new StubTenantsBffComposition(cancellableResolver: ResolveAsync));
        IRenderedComponent<TenantsWorkspace> cut = RenderWorkspace();
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await WaitForWorkspaceOperationAsync(() => clock.PendingTimerCount > 0);
        suppliedToken.CanBeCanceled.ShouldBeTrue();
        suppliedToken.IsCancellationRequested.ShouldBeFalse();
        if (dispose)
        {
            await cut.InvokeAsync(cut.Instance.Dispose);
        }
        else
        {
            hold = false;
            ReplaceFiniteWorkspaceCaller(authentication);
            cut.WaitForAssertion(() => cut.FindComponent<CreateTenantFlow>().Instance.AuthorizationReflection
                .ShouldBe(TenantLifecycleAuthorizationReflectionState.MissingPermission));
        }
        suppliedToken.IsCancellationRequested.ShouldBeTrue("Caller invalidation must cancel an active provider before the deadline.");
        long currentVersion = PrivateField<long>(cut.Instance, "_authorizationVersion");
        int currentResolutions = resolutions;
        held.SetResult(TenantLifecycleAuthorizationReflectionState.Authorized);
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await WaitForWorkspaceOperationAsync(() => clock.PendingTimerCount == 0);
        await cut.InvokeAsync(static () => Task.CompletedTask);
        resolutions.ShouldBe(currentResolutions);
        PrivateField<long>(cut.Instance, "_authorizationVersion").ShouldBe(currentVersion);
        _ = reads.Received(1).ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>());
        if (!dispose)
        {
            cut.FindAll("[data-testid='tenants-global-administrators-entry']").ShouldBeEmpty();
            cut.FindComponent<CreateTenantFlow>().Instance.IsAuthorized.ShouldBeFalse();
            PrivateField<string>(cut.Instance, "_createAuthorityUserId").ShouldBe("operator.beta");
        }
        clock.GetUtcNow().ShouldBe(DateTimeOffset.UnixEpoch);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WorkspaceCancelsActivelyHeldAuthenticationEventOnReplacementOrDisposalBeforeDeadline(bool dispose)
    {
        var clock = RegisterFiniteWorkspaceClock();
        var authentication = new MutableAuthenticationStateProvider(AdministratorPrincipal());
        TenantLifecycleAuthorizationReflectionState reflection = TenantLifecycleAuthorizationReflectionState.Authorized;
        int resolutions = 0;
        ValueTask<TenantLifecycleAuthorizationReflectionState> ResolveAsync(CancellationToken _)
        {
            resolutions++;
            return ValueTask.FromResult(reflection);
        }
        ITenantQueryGateway reads = FiniteWorkspaceReads();
        Services.AddSingleton<AuthenticationStateProvider>(authentication);
        RegisterFiniteWorkspaceServices(reads, new StubTenantsBffComposition(cancellableResolver: ResolveAsync));
        IRenderedComponent<TenantsWorkspace> cut = RenderWorkspace();
        cut.WaitForElement("[data-testid='tenants-global-administrators-entry']");
        TaskCompletionSource<AuthenticationState> pending = authentication.NotifyPending();
        cut.WaitForAssertion(() =>
        {
            cut.FindAll("[data-testid='tenants-global-administrators-entry']").ShouldBeEmpty();
            cut.FindComponent<CreateTenantFlow>().Instance.IsAuthorized.ShouldBeFalse();
        });
        await WaitForWorkspaceOperationAsync(() => clock.PendingTimerCount > 0);
        CancellationToken eventToken = PrivateField<CancellationTokenSource>(cut.Instance, "_authenticationStateCancellation").Token;
        if (dispose)
        {
            await cut.InvokeAsync(cut.Instance.Dispose);
        }
        else
        {
            reflection = TenantLifecycleAuthorizationReflectionState.MissingPermission;
            ReplaceFiniteWorkspaceCaller(authentication);
            cut.WaitForAssertion(() =>
            {
                PrivateField<long>(cut.Instance, "_pendingAuditCallerVersion").ShouldBe(0);
                PrivateField<bool>(cut.Instance, "_auditAuthorityResolved").ShouldBeTrue();
                cut.FindComponent<CreateTenantFlow>().Instance.AuthorizationReflection
                    .ShouldBe(TenantLifecycleAuthorizationReflectionState.MissingPermission);
            });
        }
        eventToken.IsCancellationRequested.ShouldBeTrue();
        long currentVersion = PrivateField<long>(cut.Instance, "_authorizationVersion");
        int currentResolutions = resolutions;
        pending.SetResult(new AuthenticationState(AdministratorPrincipal()));
        await WaitForWorkspaceOperationAsync(() => clock.PendingTimerCount == 0);
        await cut.InvokeAsync(static () => Task.CompletedTask);
        resolutions.ShouldBe(currentResolutions, "An obsolete settled event cannot start a replacement authorization lookup.");
        PrivateField<long>(cut.Instance, "_authorizationVersion").ShouldBe(currentVersion);
        if (!dispose)
        {
            cut.FindAll("[data-testid='tenants-global-administrators-entry']").ShouldBeEmpty();
            cut.FindComponent<CreateTenantFlow>().Instance.IsAuthorized.ShouldBeFalse();
        }
        _ = reads.Received(1).ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>());
        clock.GetUtcNow().ShouldBe(DateTimeOffset.UnixEpoch);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WorkspaceCancelsActiveCreateAuthorityOnCallerReplacementOrDisposalWithoutDispatch(bool dispose)
    {
        var clock = RegisterFiniteWorkspaceClock();
        var authentication = new MutableAuthenticationStateProvider(AdministratorPrincipal());
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
        ITenantCommandGateway commands = Substitute.For<ITenantCommandGateway>();
        Services.AddSingleton<AuthenticationStateProvider>(authentication);
        RegisterFiniteWorkspaceServices(reads, new StubTenantsBffComposition(cancellableResolver: ResolveAsync), commands);
        IRenderedComponent<TenantsWorkspace> cut = RenderWorkspace();
        IRenderedComponent<CreateTenantFlow> create = cut.FindComponent<CreateTenantFlow>();
        cut.WaitForAssertion(() => create.Instance.IsAuthorized.ShouldBeTrue());
        create.Find("[data-testid='tenants-create-tenant-id']").Change("canceled-create");
        create.Find("[data-testid='tenants-create-name']").Change("Canceled create");
        hold = true;
        Task submit = create.Find("form").SubmitAsync();
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await WaitForWorkspaceOperationAsync(() => clock.PendingTimerCount > 0);
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
            hold = false;
            ReplaceFiniteWorkspaceCaller(authentication);
        }
        await submit.WaitAsync(TimeSpan.FromSeconds(5));
        suppliedToken.IsCancellationRequested.ShouldBeTrue();
        commands.ReceivedCalls().ShouldBeEmpty();
        create.Instance.Snapshot.MessageId.ShouldBeNull();
        create.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Idle);
        long currentVersion = PrivateField<long>(cut.Instance, "_authorizationVersion");
        held.SetResult(TenantLifecycleAuthorizationReflectionState.Authorized);
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await WaitForWorkspaceOperationAsync(() => clock.PendingTimerCount == 0);
        await cut.InvokeAsync(static () => Task.CompletedTask);
        commands.ReceivedCalls().ShouldBeEmpty();
        create.Instance.Snapshot.MessageId.ShouldBeNull();
        create.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Idle);
        PrivateField<long>(cut.Instance, "_authorizationVersion").ShouldBe(currentVersion);
        clock.GetUtcNow().ShouldBe(DateTimeOffset.UnixEpoch);
    }

    private void ReplaceFiniteWorkspaceCaller(MutableAuthenticationStateProvider authentication)
    {
        var caller = Services.GetRequiredService<Hexalith.FrontComposer.Contracts.Rendering.IUserContextAccessor>();
        caller.UserId.Returns("operator.beta");
        authentication.Set(new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", "operator.beta")], "test")));
    }

    private ManualTimeProvider RegisterFiniteWorkspaceClock()
    {
        var clock = new ManualTimeProvider();
        Services.AddSingleton<TimeProvider>(clock);
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Tenants:Ui:AuthorizationAndSubscriptionTimeout"] = "00:00:01",
            }).Build());
        return clock;
    }

    private static ITenantQueryGateway FiniteWorkspaceReads()
    {
        ITenantQueryGateway reads = Substitute.For<ITenantQueryGateway>();
        reads.ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(TenantListSnapshot.Empty(true, ReadModelFreshnessState.Unknown)));
        return reads;
    }

    private void RegisterFiniteWorkspaceServices(
        ITenantQueryGateway reads,
        ITenantsBffComposition composition,
        ITenantCommandGateway? commands = null)
    {
        Services.AddSingleton(reads);
        Services.AddSingleton(commands ?? new StubTenantCommandGateway());
        Services.AddSingleton(composition);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddFluentUIComponents();
    }

    private static async Task WaitForWorkspaceOperationAsync(Func<bool> condition)
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!condition())
        {
            await Task.Delay(10, cancellation.Token);
        }
    }
}
