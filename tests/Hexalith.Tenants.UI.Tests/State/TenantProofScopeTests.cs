using Hexalith.Tenants.UI.State.TenantCommands;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class TenantProofScopeTests
{
    [Fact]
    public void TenantIdentityKeepsSameRouteAcrossCallerChangesAndRetiresOnInvalidation()
    {
        var scope = new TenantProofScope();
        scope.IsForTenant("tenant.alpha").ShouldBeFalse();
        scope.Reset("tenant.alpha", "caller-a");
        scope.IsForTenant("tenant.alpha").ShouldBeTrue();
        scope.IsForTenant("Tenant.Alpha").ShouldBeFalse();

        scope.Reset("tenant.alpha", "caller-b");
        scope.IsForTenant("tenant.alpha").ShouldBeTrue();
        scope.Reset(null, "caller-b");
        scope.IsForTenant("tenant.alpha").ShouldBeFalse();
    }

    [Fact]
    public void ConcurrentProofsShareGenerationAndUnchangedScopePreservesBoth()
    {
        var scope = new TenantProofScope();
        scope.Reset("tenant.alpha", "caller-a");
        using TenantProofOperation first = scope.Begin("tenant.alpha", "caller-a", () => true).ShouldNotBeNull();
        using TenantProofOperation second = scope.Begin("tenant.alpha", "caller-a", () => true).ShouldNotBeNull();

        scope.Reset("tenant.alpha", "caller-a");
        first.Generation.ShouldBe(second.Generation);
        first.CancellationToken.IsCancellationRequested.ShouldBeFalse();
        second.CancellationToken.IsCancellationRequested.ShouldBeFalse();
        first.Dispose();
        scope.CanApply(second.Generation, "tenant.alpha", "caller-a", () => true).ShouldBeTrue();
        second.CancellationToken.IsCancellationRequested.ShouldBeFalse();
    }

    [Theory]
    [InlineData("tenant.beta", "caller-a")]
    [InlineData("tenant.alpha", "caller-b")]
    [InlineData("Tenant.Alpha", "caller-a")]
    [InlineData("tenant.alpha", "Caller-A")]
    [InlineData(null, "caller-a")]
    public void ScopeReplacementCancelsWithoutDisposingAndRejectsOldGeneration(string? tenantId, string callerScope)
    {
        var scope = new TenantProofScope();
        scope.Reset("tenant.alpha", "caller-a");
        TenantProofOperation operation = scope.Begin("tenant.alpha", "caller-a", () => true).ShouldNotBeNull();
        CancellationToken token = operation.CancellationToken;

        scope.Reset(tenantId, callerScope);

        token.IsCancellationRequested.ShouldBeTrue();
        token.WaitHandle.WaitOne(0).ShouldBeTrue();
        scope.CanApply(operation.Generation, "tenant.alpha", "caller-a", () => true).ShouldBeFalse();
        // Returning to the same literal route must not admit an older visit's completion.
        scope.Reset("tenant.alpha", "caller-a");
        scope.CanApply(operation.Generation, "tenant.alpha", "caller-a", () => true).ShouldBeFalse();
        using TenantProofOperation replacement = scope.Begin("tenant.alpha", "caller-a", () => true).ShouldNotBeNull();
        replacement.Generation.ShouldBeGreaterThan(operation.Generation);

        operation.Dispose();
        operation.Dispose();
        Should.Throw<ObjectDisposedException>(() => _ = token.WaitHandle);
        replacement.CancellationToken.IsCancellationRequested.ShouldBeFalse();
    }

    [Fact]
    public void ResettingOneSurfaceDoesNotCancelAnotherSurfaceForTheSameTenant()
    {
        var metadata = new TenantProofScope();
        var lifecycle = new TenantProofScope();
        metadata.Reset("tenant.alpha", "caller-a");
        lifecycle.Reset("tenant.alpha", "caller-a");
        using TenantProofOperation metadataRead = metadata.Begin("tenant.alpha", "caller-a", () => true).ShouldNotBeNull();
        using TenantProofOperation lifecycleRead = lifecycle.Begin("tenant.alpha", "caller-a", () => true).ShouldNotBeNull();

        metadata.Reset(null, "caller-a");

        metadataRead.CancellationToken.IsCancellationRequested.ShouldBeTrue();
        lifecycleRead.CancellationToken.IsCancellationRequested.ShouldBeFalse();
        lifecycle.CanApply(lifecycleRead.Generation, "tenant.alpha", "caller-a", () => true).ShouldBeTrue();
    }

    [Fact]
    public void ResetSurvivesReentrantCompletionAndPreservesReentrantNewGenerationRead()
    {
        var scope = new TenantProofScope();
        scope.Reset("tenant.alpha", "caller-a");
        using TenantProofOperation first = scope.Begin("tenant.alpha", "caller-a", () => true).ShouldNotBeNull();
        using TenantProofOperation second = scope.Begin("tenant.alpha", "caller-a", () => true).ShouldNotBeNull();
        TenantProofOperation? replacement = null;
        using CancellationTokenRegistration registration = first.CancellationToken.Register(() =>
        {
            second.Dispose();
            replacement = scope.Begin("tenant.beta", "caller-a", () => true);
            first.Dispose();
        });

        scope.Reset("tenant.beta", "caller-a");

        using TenantProofOperation current = replacement.ShouldNotBeNull();
        current.CancellationToken.IsCancellationRequested.ShouldBeFalse();
        scope.CanApply(current.Generation, "tenant.beta", "caller-a", () => true).ShouldBeTrue();
        scope.Reset(null, "caller-a");
        current.CancellationToken.IsCancellationRequested.ShouldBeTrue();
    }

    [Fact]
    public async Task ConcurrentResetAndOwnerDisposalKeepCancellationSourceAliveUntilCallbackReturns()
    {
        var scope = new TenantProofScope();
        scope.Reset("tenant.alpha", "caller-a");
        using TenantProofOperation operation = scope.Begin("tenant.alpha", "caller-a", () => true).ShouldNotBeNull();
        CancellationToken token = operation.CancellationToken;
        var cancellationEntered = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var disposalEntered = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var releaseCancellation = new ManualResetEventSlim();
        using CancellationTokenRegistration registration = token.Register(() =>
        {
            cancellationEntered.SetResult(Environment.CurrentManagedThreadId);
            if (!releaseCancellation.Wait(TimeSpan.FromSeconds(10)))
            {
                throw new TimeoutException("Proof cancellation callback was not released.");
            }

            // The competing owner completion cannot dispose the source while Reset holds its lock.
            token.WaitHandle.WaitOne(0).ShouldBeTrue();
        });
        Task reset = Task.Run(() => scope.Reset("tenant.beta", "caller-a"));
        Task? disposal = null;
        try
        {
            int resetThreadId = await cancellationEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            disposal = Task.Run(() =>
            {
                disposalEntered.SetResult(Environment.CurrentManagedThreadId);
                operation.Dispose();
            });
            int disposalThreadId = await disposalEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            disposalThreadId.ShouldNotBe(resetThreadId);
            disposal.IsCompleted.ShouldBeFalse();
            token.IsCancellationRequested.ShouldBeTrue();
            token.WaitHandle.WaitOne(0).ShouldBeTrue();
        }
        finally
        {
            releaseCancellation.Set();
            await Task.WhenAll(reset, disposal ?? Task.CompletedTask).WaitAsync(TimeSpan.FromSeconds(10));
        }

        Should.Throw<ObjectDisposedException>(() => _ = token.WaitHandle);
        scope.CanApply(operation.Generation, "tenant.alpha", "caller-a", () => true).ShouldBeFalse();
        using TenantProofOperation replacement = scope.Begin("tenant.beta", "caller-a", () => true).ShouldNotBeNull();
        replacement.CancellationToken.IsCancellationRequested.ShouldBeFalse();
        scope.CanApply(replacement.Generation, "tenant.beta", "caller-a", () => true).ShouldBeTrue();
    }

    [Fact]
    public void ObsoletePageLifetimeOrMismatchedKeyCannotBeginOrApplyProof()
    {
        var scope = new TenantProofScope();
        scope.Reset("tenant.alpha", "caller-a");
        using TenantProofOperation operation = scope.Begin("tenant.alpha", "caller-a", () => true).ShouldNotBeNull();

        scope.Begin("tenant.beta", "caller-a", () => true).ShouldBeNull();
        scope.Begin("tenant.alpha", "caller-b", () => true).ShouldBeNull();
        scope.Begin("tenant.alpha", "caller-a", () => false).ShouldBeNull();
        scope.CanApply(operation.Generation, "tenant.alpha", "caller-a", () => false).ShouldBeFalse();
        scope.CanApply(operation.Generation, "tenant.beta", "caller-a", () => true).ShouldBeFalse();
        scope.CanApply(operation.Generation, "tenant.alpha", "caller-b", () => true).ShouldBeFalse();
    }
}
