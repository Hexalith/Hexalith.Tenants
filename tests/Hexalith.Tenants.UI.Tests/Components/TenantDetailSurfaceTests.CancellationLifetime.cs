using Bunit;

using Hexalith.Tenants.UI.Components.Pages;
using Hexalith.Tenants.UI.Components.Tenants.Metadata;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantUsers;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed partial class TenantDetailSurfaceTests
{
    /// <summary>Cancelled detail and parameter loads retain their tokens until every owned read finishes.</summary>
    [Theory]
    [InlineData(false, "refresh")]
    [InlineData(false, "route")]
    [InlineData(false, "dispose")]
    [InlineData(true, "refresh")]
    [InlineData(true, "route")]
    [InlineData(true, "dispose")]
    public async Task DetailReadCancellationLifetimeExtendsThroughCompletion(bool parameterLoad, string transition)
    {
        var entered = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var detailFinished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<TenantDetailSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseMembers = new TaskCompletionSource<TenantUsersSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        var successorEntered = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var successorCancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseSuccessor = new TaskCompletionSource<TenantDetailSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        string currentTenant = transition == "route" ? "tenant.beta" : "tenant.alpha";
        int reads = 0;
        int memberReads = 0;
        CancellationToken memberToken = default;
        CancellationToken successorToken = default;
        RegisterServices(call =>
        {
            string requestedTenant = call.ArgAt<TenantDetailRequest>(0).TenantId;
            int read = Interlocked.Increment(ref reads);
            if (read == 2)
            {
                requestedTenant.ShouldBe("tenant.alpha");
                return HoldReadAsync(call.ArgAt<CancellationToken>(2), release, entered, cancelled);
            }
            if (read == 3)
            {
                requestedTenant.ShouldBe(currentTenant);
                return HoldReadAsync(call.ArgAt<CancellationToken>(2), releaseSuccessor, successorEntered, successorCancelled);
            }

            requestedTenant.ShouldBe(read == 1 ? "tenant.alpha" : currentTenant);
            return Task.FromResult(Dw150Detail(read == 1 ? "baseline" : "replacement", requestedTenant));
        });
        ITenantQueryGateway gateway = Services.GetRequiredService<ITenantQueryGateway>();
        gateway.GetTenantUsersAsync(Arg.Any<TenantUsersRequest>(), Arg.Any<TenantUsersSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                if (Interlocked.Increment(ref memberReads) == 2 && parameterLoad)
                {
                    memberToken = call.ArgAt<CancellationToken>(2);
                    return releaseMembers.Task;
                }
                return Task.FromResult(MemberSnapshot(Detail(call.ArgAt<TenantUsersRequest>(0).TenantId)));
            });
        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-detail-identity']");
        EventCallback refresh = cut.FindComponent<EditTenantMetadataFlow>().Instance.OnProjectionRefreshRequested;
        Task heldLoad = StartLoad("tenant.alpha");
        Task successorLoad = Task.CompletedTask;

        try
        {
            CancellationToken token = await entered.Task.WaitAsync(DefaultWaitTimeout);
            token.CanBeCanceled.ShouldBeTrue();
            token.IsCancellationRequested.ShouldBeFalse();
            if (parameterLoad)
            {
                cut.WaitForAssertion(() => Volatile.Read(ref memberReads).ShouldBe(2));
                memberToken.ShouldBe(token);
            }

            if (transition == "dispose")
            {
                await DisposeComponentsAsync().WaitAsync(DefaultWaitTimeout);
            }
            else
            {
                successorLoad = transition == "route" ? StartParameterLoad(currentTenant) : StartLoad(currentTenant);
                successorToken = await successorEntered.Task.WaitAsync(DefaultWaitTimeout);
                successorToken.IsCancellationRequested.ShouldBeFalse();
                successorLoad.IsCompleted.ShouldBeFalse();
            }

            await cancelled.Task.WaitAsync(DefaultWaitTimeout);
            heldLoad.IsCompleted.ShouldBeFalse();
            AssertCancelledTokenUsable(token);
            int renderCount = cut.RenderCount;
            release.SetResult(Dw150Detail("superseded"));
            if (parameterLoad)
            {
                await detailFinished.Task.WaitAsync(DefaultWaitTimeout);
                await Renderer.Dispatcher.InvokeAsync(() => { }).WaitAsync(DefaultWaitTimeout);
                heldLoad.IsCompleted.ShouldBeFalse();
                AssertCancelledTokenUsable(memberToken);
                releaseMembers.SetResult(MemberSnapshot(Detail("tenant.alpha")));
            }
            await heldLoad.WaitAsync(DefaultWaitTimeout);

            // WaitHandle detects disposed sources even when registration on a cancelled token succeeds.
            Should.Throw<ObjectDisposedException>(() => _ = token.WaitHandle);
            await Renderer.Dispatcher.InvokeAsync(() => { }).WaitAsync(DefaultWaitTimeout);
            Renderer.UnhandledException.IsCompleted.ShouldBeFalse();
            if (transition == "dispose")
            {
                cut.RenderCount.ShouldBe(renderCount);
                Volatile.Read(ref reads).ShouldBe(2);
            }
            else
            {
                CancellationToken successor = successorToken;
                successor.IsCancellationRequested.ShouldBeFalse();
                successor.WaitHandle.WaitOne(0).ShouldBeFalse();
                successorLoad.IsCompleted.ShouldBeFalse();
                foreach (AngleSharp.Dom.IElement renderedTable in cut.FindAll("[data-testid='tenants-config-read-table']"))
                {
                    renderedTable.TextContent.ShouldNotContain("superseded");
                }

                // Finishing an obsolete owner must not detach its still-active successor from cancellation.
                await StartLoad(currentTenant).WaitAsync(DefaultWaitTimeout);
                await successorCancelled.Task.WaitAsync(DefaultWaitTimeout);
                AssertCancelledTokenUsable(successor);
                cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-config-read-table']")
                    .TextContent.ShouldContain("replacement"));
                releaseSuccessor.SetResult(Dw150Detail("superseded-successor", currentTenant));
                await successorLoad.WaitAsync(DefaultWaitTimeout);
                Should.Throw<ObjectDisposedException>(() => _ = successor.WaitHandle);

                string table = cut.Find("[data-testid='tenants-config-read-table']").TextContent;
                table.ShouldContain("replacement");
                table.ShouldNotContain("superseded");
                cut.Find("[data-testid='tenants-detail-identity']").TextContent.ShouldContain(currentTenant);
                Volatile.Read(ref reads).ShouldBe(4);
            }
        }
        finally
        {
            release.TrySetResult(Dw150Detail("superseded"));
            releaseMembers.TrySetResult(MemberSnapshot(Detail("tenant.alpha")));
            releaseSuccessor.TrySetResult(Dw150Detail("superseded-successor", currentTenant));
            await heldLoad.WaitAsync(DefaultWaitTimeout);
            await successorLoad.WaitAsync(DefaultWaitTimeout);
        }

        Task StartLoad(string tenantId)
            => parameterLoad ? StartParameterLoad(tenantId) : cut.InvokeAsync(() => refresh.InvokeAsync());

        Task StartParameterLoad(string tenantId)
            => cut.InvokeAsync(() => cut.Instance.SetParametersAsync(ParameterView.FromDictionary(
                new Dictionary<string, object?> { [nameof(TenantDetailPage.TenantId)] = tenantId })));

        async Task<TenantDetailSnapshot> HoldReadAsync(
            CancellationToken token,
            TaskCompletionSource<TenantDetailSnapshot> completion,
            TaskCompletionSource<CancellationToken> entry,
            TaskCompletionSource cancellation)
        {
            using CancellationTokenRegistration registration = token.Register(() => cancellation.TrySetResult());
            entry.SetResult(token);
            TenantDetailSnapshot result = await completion.Task;
            if (ReferenceEquals(completion, release))
            {
                detailFinished.SetResult();
            }
            return result;
        }

        static void AssertCancelledTokenUsable(CancellationToken token)
        {
            token.IsCancellationRequested.ShouldBeTrue();
            token.WaitHandle.WaitOne(0).ShouldBeTrue();
            bool registeredAfterCancellation = false;
            using (token.Register(() => registeredAfterCancellation = true))
            {
                registeredAfterCancellation.ShouldBeTrue();
            }
        }
    }
}
