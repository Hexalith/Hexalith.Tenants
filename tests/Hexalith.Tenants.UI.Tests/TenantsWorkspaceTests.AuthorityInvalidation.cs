using Bunit;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Pages;
using Hexalith.Tenants.UI.Components.Users;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.Tenants.UI.State.UserTenants;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests;

public sealed partial class TenantsWorkspaceTests
{
    [Theory]
    [InlineData("list", "ready")]
    [InlineData("list", "degraded")]
    [InlineData("list", "inflight")]
    [InlineData("mine", "ready")]
    [InlineData("mine", "degraded")]
    [InlineData("mine", "inflight")]
    [InlineData("user", "ready")]
    [InlineData("user", "degraded")]
    [InlineData("user", "inflight")]
    public async Task Mounted_read_consumers_drop_old_authority_facts_before_requery_and_retire_held_reads(string surface, string phase)
    {
        var authentication = new WorkspaceAcceptanceAuthenticationStateProvider(Task.FromResult(new AuthenticationState(AdministratorPrincipal())));
        Services.AddSingleton<AuthenticationStateProvider>(authentication);
        var pendingAuthentication = new TaskCompletionSource<AuthenticationState>(TaskCreationOptions.RunContinuationsAsynchronously);
        var obsolete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var replacement = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ITenantQueryGateway reads = Substitute.For<ITenantQueryGateway>();
        int calls = 0;
        string? lastETag = null;
        int lastRetainedRows = -1;
        string? lastRetainedETag = "unset";
        async Task<int> ReadAsync(string? etag, int rows, string? retainedETag)
        {
            int current = Interlocked.Increment(ref calls);
            lastETag = etag;
            lastRetainedRows = rows;
            lastRetainedETag = retainedETag;
            if (current == 1) return current;
            if (phase == "inflight" && current == 2) await obsolete.Task;
            else await replacement.Task;
            return current;
        }
        TenantListSnapshot ListSnapshot(bool fresh)
        {
            string fact = fresh ? "Fresh-authorized-fact" : "Old-authority-fact";
            TenantListRow row = TenantListRow.FromSummary(new TenantSummary(fresh ? "tenant.fresh" : "tenant.old", fact, TenantStatus.Active))
                with { Freshness = ReadModelFreshnessState.Current, Lifecycle = ProjectionLifecycleState.Current };
            return TenantListSnapshot.Ready([row], null, false, "old-validator", ReadModelFreshnessState.Current, false) with
            {
                Kind = !fresh && phase == "degraded" ? TenantListSurfaceKind.Degraded : TenantListSurfaceKind.Ready,
                Lifecycle = ProjectionLifecycleState.Current,
            };
        }
        UserTenantMembershipSnapshot MembershipSnapshot(bool fresh)
            => UserTenantMembershipSnapshot.Ready(
                [new UserTenantMembershipRow(fresh ? "tenant.fresh" : "tenant.old", fresh ? "Fresh-authorized-fact" : "Old-authority-fact",
                    TenantStatus.Active, TenantRole.TenantReader, ReadModelFreshnessState.Current, ProjectionLifecycleState.Current)],
                null, false, "old-validator", ReadModelFreshnessState.Current, surface == "user" ? "target.alpha" : null) with
            {
                Kind = !fresh && phase == "degraded" ? UserTenantMembershipSurfaceKind.Degraded : UserTenantMembershipSurfaceKind.Ready,
                Lifecycle = ProjectionLifecycleState.Current,
            };
        reads.ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                int current = await ReadAsync(call.ArgAt<TenantListRequest>(0).ETag,
                    call.ArgAt<TenantListSnapshot?>(1)?.Rows.Count ?? 0, call.ArgAt<TenantListSnapshot?>(1)?.ETag);
                return ListSnapshot(current > (phase == "inflight" ? 2 : 1));
            });
        async Task<UserTenantMembershipSnapshot> MembershipReadAsync(NSubstitute.Core.CallInfo call)
        {
            int current = await ReadAsync(call.ArgAt<UserTenantMembershipRequest>(0).ETag,
                call.ArgAt<UserTenantMembershipSnapshot?>(1)?.Rows.Count ?? 0, call.ArgAt<UserTenantMembershipSnapshot?>(1)?.ETag);
            return MembershipSnapshot(current > (phase == "inflight" ? 2 : 1));
        }
        reads.GetMyTenantsAsync(Arg.Any<UserTenantMembershipRequest>(), Arg.Any<UserTenantMembershipSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(MembershipReadAsync);
        reads.GetUserTenantsAsync(Arg.Any<UserTenantMembershipRequest>(), Arg.Any<UserTenantMembershipSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(MembershipReadAsync);
        RegisterPagingFocusServices(reads);
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        Type component = surface switch { "mine" => typeof(MyTenantsPanel), "user" => typeof(UserMembershipLookupPanel), _ => typeof(TenantsWorkspace) };
        var cut = Render<DynamicComponent>(p => p.Add(x => x.Type, component)
            .Add(x => x.Parameters, surface == "user" ? new Dictionary<string, object> { [nameof(UserMembershipLookupPanel.InitialUserId)] = "target.alpha" } : null));
        Task? obsoleteRead = null;
        try
        {
            cut.WaitForAssertion(() => cut.Markup.ShouldContain("Old-authority-fact"));
            if (phase == "inflight")
            {
                string refresh = surface switch { "mine" => "tenants-my-refresh", "user" => "tenants-user-lookup-refresh", _ => "tenants-list-refresh" };
                obsoleteRead = cut.Find($"[data-testid='{refresh}']").ClickAsync(new MouseEventArgs());
                await WaitForWorkspaceOperationAsync(() => calls == 2);
                calls.ShouldBe(2);
            }
            int priorCalls = calls;
            authentication.Notify(pendingAuthentication.Task);
            cut.WaitForAssertion(() => cut.Markup.ShouldNotContain("Old-authority-fact"));
            cut.Render(); // Unchanged route/parameters cannot reuse retained facts or skip authority observation.
            calls.ShouldBe(priorCalls);
            if (obsoleteRead is not null)
            {
                obsolete.TrySetResult();
                await obsoleteRead.WaitAsync(TimeSpan.FromSeconds(5));
                cut.Markup.ShouldNotContain("Old-authority-fact");
            }
            pendingAuthentication.SetResult(new AuthenticationState(AdministratorPrincipal()));
            await WaitForWorkspaceOperationAsync(() => calls == priorCalls + 1);
            calls.ShouldBe(priorCalls + 1);
            cut.Markup.ShouldNotContain("Old-authority-fact");
            cut.Markup.ShouldNotContain("Fresh-authorized-fact");
            lastETag.ShouldBeNull();
            lastRetainedRows.ShouldBe(0);
            lastRetainedETag.ShouldBeNull();
            replacement.SetResult();
            cut.WaitForAssertion(() => cut.Markup.ShouldContain("Fresh-authorized-fact"));
            cut.Markup.ShouldNotContain("Old-authority-fact");
        }
        finally
        {
            cut.Dispose();
            pendingAuthentication.TrySetResult(new AuthenticationState(AdministratorPrincipal()));
            obsolete.TrySetResult();
            replacement.TrySetResult();
            if (obsoleteRead is not null) await obsoleteRead.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }
    [Theory]
    [InlineData("mine", "fault")]
    [InlineData("mine", "timeout")]
    [InlineData("mine", "disposal")]
    [InlineData("mine", "replacement")]
    [InlineData("user", "fault")]
    [InlineData("user", "timeout")]
    [InlineData("user", "disposal")]
    [InlineData("user", "replacement")]
    public async Task Membership_panels_retire_faulted_expired_disposed_or_overtaken_authority(string surface, string transition)
    {
        var clock = RegisterFiniteWorkspaceClock();
        var authentication = new WorkspaceAcceptanceAuthenticationStateProvider(Task.FromResult(new AuthenticationState(AdministratorPrincipal())));
        Services.AddSingleton<AuthenticationStateProvider>(authentication);
        var first = new TaskCompletionSource<AuthenticationState>(TaskCreationOptions.RunContinuationsAsynchronously);
        var replacementRead = new TaskCompletionSource<UserTenantMembershipSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        UserTenantMembershipSnapshot Snapshot(string name) => UserTenantMembershipSnapshot.Ready(
            [new UserTenantMembershipRow("tenant.alpha", name, TenantStatus.Active, TenantRole.TenantReader,
                ReadModelFreshnessState.Current, ProjectionLifecycleState.Current)], null, false, "validator",
            ReadModelFreshnessState.Current, surface == "user" ? "target.alpha" : null);
        ITenantQueryGateway reads = Substitute.For<ITenantQueryGateway>();
        int calls = 0;
        Task<UserTenantMembershipSnapshot> Read(NSubstitute.Core.CallInfo _) => ++calls == 1
            ? Task.FromResult(Snapshot("Old-panel-fact")) : replacementRead.Task;
        reads.GetMyTenantsAsync(Arg.Any<UserTenantMembershipRequest>(), Arg.Any<UserTenantMembershipSnapshot?>(), Arg.Any<CancellationToken>()).Returns(Read);
        reads.GetUserTenantsAsync(Arg.Any<UserTenantMembershipRequest>(), Arg.Any<UserTenantMembershipSnapshot?>(), Arg.Any<CancellationToken>()).Returns(Read);
        RegisterPagingFocusServices(reads);
        Type component = surface == "mine" ? typeof(MyTenantsPanel) : typeof(UserMembershipLookupPanel);
        var cut = Render<DynamicComponent>(p => p.Add(x => x.Type, component)
            .Add(x => x.Parameters, surface == "user" ? new Dictionary<string, object> { [nameof(UserMembershipLookupPanel.InitialUserId)] = "target.alpha" } : null));
        try
        {
            cut.WaitForAssertion(() => cut.Markup.ShouldContain("Old-panel-fact"));
            authentication.Notify(first.Task);
            cut.WaitForAssertion(() => cut.Markup.ShouldNotContain("Old-panel-fact"));
            await WaitForWorkspaceOperationAsync(() => clock.PendingTimerCount > 0);
            if (transition == "fault") first.SetException(new InvalidOperationException("Private provider failure"));
            else if (transition == "timeout") clock.Advance(TimeSpan.FromSeconds(1));
            else if (transition == "disposal") await cut.InvokeAsync(() =>
            {
                if (surface == "mine") cut.FindComponent<MyTenantsPanel>().Instance.Dispose();
                else cut.FindComponent<UserMembershipLookupPanel>().Instance.Dispose();
            });
            else
            {
                authentication.Notify(Task.FromResult(new AuthenticationState(AdministratorPrincipal())));
                await WaitForWorkspaceOperationAsync(() => calls == 2);
                cut.Markup.ShouldNotContain("Old-panel-fact");
                replacementRead.SetResult(Snapshot("Current-panel-fact"));
                cut.WaitForAssertion(() => cut.Markup.ShouldContain("Current-panel-fact"));
            }
            if (transition is "fault" or "timeout")
            {
                string error = surface == "mine" ? "tenants-my-error" : "tenants-user-error";
                cut.WaitForElement($"[data-testid='{error}']");
                cut.Markup.ShouldNotContain("Private provider failure");
                cut.Markup.ShouldNotContain("Old-panel-fact");
            }
            // The retired raw provider may settle later; it cannot publish rows or start another read.
            first.TrySetResult(new AuthenticationState(AdministratorPrincipal()));
            await WaitForWorkspaceOperationAsync(() => clock.PendingTimerCount == 0);
            if (transition != "disposal") await cut.InvokeAsync(static () => Task.CompletedTask);
            calls.ShouldBe(transition == "replacement" ? 2 : 1);
            if (transition != "disposal")
            {
                cut.Markup.ShouldNotContain("Old-panel-fact");
                if (transition == "replacement") cut.Markup.ShouldContain("Current-panel-fact");
                else cut.Markup.ShouldNotContain("Current-panel-fact");
            }
        }
        finally
        {
            cut.Dispose();
            first.TrySetResult(new AuthenticationState(AdministratorPrincipal()));
            replacementRead.TrySetResult(Snapshot("Retired-panel-fact"));
        }
    }

}
