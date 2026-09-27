using System.Security.Claims;

using Bunit;

using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.UI.Components.Pages;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.UserTenants;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class AuditReadAuthorityBoundaryTests : BunitContext
{
    [Fact]
    public void StandaloneMyTenantsEnabledAuditLinkClosesWhileNewCallerIsPending()
    {
        MutableAuthenticationStateProvider authentication = new();
        ITenantsBffComposition bff = Substitute.For<ITenantsBffComposition>();
        bff.IsReadSurfaceConnected.Returns(true);
        bff.ResolveGlobalAdministratorsAuthorizationAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(TenantLifecycleAuthorizationReflectionState.Authorized));
        Register(authentication, bff);

        IRenderedComponent<MyTenantsPage> cut = Render<MyTenantsPage>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-my-audit-entrypoint']")
            .ParentElement.ShouldNotBeNull().GetAttribute("href").ShouldNotBeNull());

        _ = authentication.NotifyPending();

        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-my-audit-entrypoint']")
            .ParentElement.ShouldNotBeNull().GetAttribute("href").ShouldBeNull());
    }

    [Fact]
    public void StandaloneRefreshCannotReopenAuditLinkUntilPendingCallerResolves()
    {
        MutableAuthenticationStateProvider authentication = new();
        ITenantsBffComposition bff = Substitute.For<ITenantsBffComposition>();
        bff.IsReadSurfaceConnected.Returns(true);
        bff.ResolveGlobalAdministratorsAuthorizationAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(TenantLifecycleAuthorizationReflectionState.Authorized));
        Register(authentication, bff);

        IRenderedComponent<MyTenantsPage> cut = Render<MyTenantsPage>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-my-audit-entrypoint']")
            .ParentElement.ShouldNotBeNull().GetAttribute("href").ShouldNotBeNull());

        TaskCompletionSource<AuthenticationState> pending = authentication.NotifyPending();
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-my-audit-entrypoint']")
            .ParentElement.ShouldNotBeNull().GetAttribute("href").ShouldBeNull());
        cut.Find("[data-testid='tenants-audit-entrypoint-refresh']").Click();

        cut.Find("[data-testid='tenants-my-audit-entrypoint']")
            .ParentElement.ShouldNotBeNull().GetAttribute("href").ShouldBeNull();
        _ = bff.Received(1).ResolveGlobalAdministratorsAuthorizationAsync(Arg.Any<CancellationToken>());

        pending.SetResult(new AuthenticationState(new ClaimsPrincipal()));
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-my-audit-entrypoint']")
            .ParentElement.ShouldNotBeNull().GetAttribute("href").ShouldNotBeNull());
        _ = bff.Received(2).ResolveGlobalAdministratorsAuthorizationAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void FailedCallerRefreshReacquiresCurrentCaller()
    {
        MutableAuthenticationStateProvider authentication = new();
        ITenantsBffComposition bff = Substitute.For<ITenantsBffComposition>();
        bff.IsReadSurfaceConnected.Returns(true);
        bff.ResolveGlobalAdministratorsAuthorizationAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(TenantLifecycleAuthorizationReflectionState.Authorized));
        Register(authentication, bff);

        IRenderedComponent<MyTenantsPage> cut = Render<MyTenantsPage>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-my-audit-entrypoint']")
            .ParentElement.ShouldNotBeNull().GetAttribute("href").ShouldNotBeNull());

        TaskCompletionSource<AuthenticationState> pending = authentication.NotifyPending();
        pending.SetException(new InvalidOperationException("Authentication failed."));
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-my-audit-entrypoint']")
            .ParentElement.ShouldNotBeNull().GetAttribute("href").ShouldBeNull());
        cut.Find("[data-testid='tenants-audit-entrypoint-refresh']").Click();

        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-my-audit-entrypoint']")
            .ParentElement.ShouldNotBeNull().GetAttribute("href").ShouldNotBeNull());
        _ = bff.Received(2).ResolveGlobalAdministratorsAuthorizationAsync(Arg.Any<CancellationToken>());

        authentication.PublishResolved();
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-my-audit-entrypoint']")
            .ParentElement.ShouldNotBeNull().GetAttribute("href").ShouldNotBeNull());
    }

    [Fact]
    public void Synchronous_initial_caller_failure_requires_reacquisition_before_standalone_authority()
    {
        var authentication = new ThrowingInitialAuthenticationStateProvider();
        ITenantsBffComposition bff = Substitute.For<ITenantsBffComposition>();
        bff.IsReadSurfaceConnected.Returns(true);
        bff.ResolveGlobalAdministratorsAuthorizationAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(TenantLifecycleAuthorizationReflectionState.Authorized));
        Register(authentication, bff);

        IRenderedComponent<MyTenantsPage> cut = Render<MyTenantsPage>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-my-audit-entrypoint']")
            .ParentElement.ShouldNotBeNull().GetAttribute("href").ShouldBeNull());
        _ = bff.DidNotReceive().ResolveGlobalAdministratorsAuthorizationAsync(Arg.Any<CancellationToken>());

        cut.Find("[data-testid='tenants-audit-entrypoint-refresh']").Click();
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-my-audit-entrypoint']")
            .ParentElement.ShouldNotBeNull().GetAttribute("href").ShouldNotBeNull());
        authentication.Reads.ShouldBe(2);
        _ = bff.Received(1).ResolveGlobalAdministratorsAuthorizationAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void OlderCallerAuthorityCompletionCannotEnableStandaloneLink()
    {
        MutableAuthenticationStateProvider authentication = new();
        var oldCaller = new TaskCompletionSource<TenantLifecycleAuthorizationReflectionState>(TaskCreationOptions.RunContinuationsAsynchronously);
        ITenantsBffComposition bff = Substitute.For<ITenantsBffComposition>();
        bff.IsReadSurfaceConnected.Returns(true);
        bff.ResolveGlobalAdministratorsAuthorizationAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<TenantLifecycleAuthorizationReflectionState>(oldCaller.Task),
                ValueTask.FromResult(TenantLifecycleAuthorizationReflectionState.Indeterminate));
        Register(authentication, bff);

        IRenderedComponent<MyTenantsPage> cut = Render<MyTenantsPage>();
        cut.WaitForElement("[data-testid='tenants-my-audit-entrypoint']");
        authentication.PublishResolved();
        oldCaller.SetResult(TenantLifecycleAuthorizationReflectionState.Authorized);

        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-my-audit-entrypoint']")
            .ParentElement.ShouldNotBeNull().GetAttribute("href").ShouldBeNull());
    }

    [Fact]
    public void InitialCallerMustResolveBeforeStandaloneAuditAuthorityIsQueried()
    {
        var authentication = new PendingInitialAuthenticationStateProvider();
        ITenantsBffComposition bff = Substitute.For<ITenantsBffComposition>();
        bff.IsReadSurfaceConnected.Returns(true);
        bff.ResolveGlobalAdministratorsAuthorizationAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(TenantLifecycleAuthorizationReflectionState.Authorized));
        Register(authentication, bff);

        IRenderedComponent<MyTenantsPage> cut = Render<MyTenantsPage>();
        cut.Find("[data-testid='tenants-my-audit-entrypoint']")
            .ParentElement.ShouldNotBeNull().GetAttribute("href").ShouldBeNull();
        _ = bff.DidNotReceive().ResolveGlobalAdministratorsAuthorizationAsync(Arg.Any<CancellationToken>());

        authentication.Resolve();
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-my-audit-entrypoint']")
            .ParentElement.ShouldNotBeNull().GetAttribute("href").ShouldNotBeNull());
    }

    [Fact]
    public void ProvenDeniedCallerLosesStandaloneAuditColumn()
    {
        MutableAuthenticationStateProvider authentication = new();
        ITenantsBffComposition bff = Substitute.For<ITenantsBffComposition>();
        bff.IsReadSurfaceConnected.Returns(true);
        TenantLifecycleAuthorizationReflectionState reflection = TenantLifecycleAuthorizationReflectionState.Authorized;
        bff.ResolveGlobalAdministratorsAuthorizationAsync(Arg.Any<CancellationToken>())
            .Returns(_ => ValueTask.FromResult(reflection));
        Register(authentication, bff);

        IRenderedComponent<MyTenantsPage> cut = Render<MyTenantsPage>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-my-audit-entrypoint']")
            .ParentElement.ShouldNotBeNull().GetAttribute("href").ShouldNotBeNull());

        reflection = TenantLifecycleAuthorizationReflectionState.MissingPermission;
        authentication.PublishResolved();
        cut.WaitForAssertion(() => cut.FindAll("[data-testid='tenants-my-audit-entrypoint']").ShouldBeEmpty());
    }

    [Fact]
    public void AbsentAuthenticationProviderDoesNotQueryAuditAuthority()
    {
        ITenantsBffComposition bff = Substitute.For<ITenantsBffComposition>();
        bff.IsReadSurfaceConnected.Returns(true);
        Register(null, bff);

        IRenderedComponent<MyTenantsPage> cut = Render<MyTenantsPage>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-my-audit-entrypoint']")
            .ParentElement.ShouldNotBeNull().GetAttribute("href").ShouldBeNull());
        _ = bff.DidNotReceive().ResolveGlobalAdministratorsAuthorizationAsync(Arg.Any<CancellationToken>());
    }

    private void Register(AuthenticationStateProvider? authentication, ITenantsBffComposition bff)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.GetMyTenantsAsync(Arg.Any<UserTenantMembershipRequest>(), Arg.Any<UserTenantMembershipSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(UserTenantMembershipSnapshot.Ready(
                [new UserTenantMembershipRow("tenant.alpha", "Alpha", TenantStatus.Active, TenantRole.TenantReader,
                    ReadModelFreshnessState.Current, ProjectionLifecycleState.Current)],
                nextCursor: null, hasMore: false, eTag: null, freshness: ReadModelFreshnessState.Current) with
            {
                Lifecycle = ProjectionLifecycleState.Current,
            }));
        Services.AddSingleton(gateway);
        Services.AddSingleton(bff);
        if (authentication is not null)
        {
            Services.AddSingleton<AuthenticationStateProvider>(authentication);
        }
        Services.AddLocalization();
        Services.AddFluentUIComponents();
    }

    private sealed class MutableAuthenticationStateProvider : AuthenticationStateProvider
    {
        private static readonly AuthenticationState _state = new(new ClaimsPrincipal());

        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(_state);

        public TaskCompletionSource<AuthenticationState> NotifyPending()
        {
            var pending = new TaskCompletionSource<AuthenticationState>(TaskCreationOptions.RunContinuationsAsynchronously);
            NotifyAuthenticationStateChanged(pending.Task);
            return pending;
        }

        public void PublishResolved() => NotifyAuthenticationStateChanged(Task.FromResult(_state));
    }

    private sealed class PendingInitialAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly TaskCompletionSource<AuthenticationState> _pending = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override Task<AuthenticationState> GetAuthenticationStateAsync() => _pending.Task;

        public void Resolve() => _pending.SetResult(new AuthenticationState(new ClaimsPrincipal()));
    }

    private sealed class ThrowingInitialAuthenticationStateProvider : AuthenticationStateProvider
    {
        public int Reads { get; private set; }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => ++Reads == 1
                ? throw new InvalidOperationException("Authentication is unavailable.")
                : Task.FromResult(new AuthenticationState(new ClaimsPrincipal()));
    }

}
