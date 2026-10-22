using Hexalith.Tenants.UI.State.TenantCommands;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

/// <summary>Qualifies iterative status recovery, projection intent and exact generation handoff.</summary>
public sealed class TenantMembershipStatusRefreshDrainTests
{
    [Fact]
    public async Task GenerationChangeAtHandoffCannotApplyOldProjectionIntentToReplacement()
    {
        TenantMembershipStatusRefreshDrain drain = new();
        int generation = 1;
        int reads = 0;
        List<bool> applied = [];
        int ReadGeneration()
        {
            int observed = generation;
            if (++reads == 2)
            {
                generation = 2;
            }
            return observed;
        }
        Task Refresh(int expectedGeneration, bool projection)
        {
            if (expectedGeneration == generation)
            {
                applied.Add(projection);
            }
            return Task.CompletedTask;
        }
        await drain.RunAsync(true, true, ReadGeneration, () => true, Refresh);
        applied.ShouldBeEmpty();
        await drain.RunAsync(false, true, ReadGeneration, () => true, Refresh);
        applied.ShouldBe(new[] { false });
    }

    [Fact]
    public async Task FailedOlderGenerationStillReportsFailureToJoinedReplacementAfterServingItsWork()
    {
        TenantMembershipStatusRefreshDrain drain = new();
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int generation = 1;
        List<int> served = [];
        async Task Refresh(int expectedGeneration, bool projection)
        {
            served.Add(expectedGeneration);
            if (expectedGeneration == 1)
            {
                await release.Task;
                throw new InvalidOperationException("old batch failed");
            }
        }
        Task first = drain.RunAsync(false, true, () => generation, () => true, Refresh);
        generation = 2;
        Task replacement = drain.RunAsync(true, true, () => generation, () => true, Refresh);
        release.SetResult();
        await Should.ThrowAsync<InvalidOperationException>(() => first.WaitAsync(TimeSpan.FromSeconds(5)));
        await Should.ThrowAsync<InvalidOperationException>(() => replacement.WaitAsync(TimeSpan.FromSeconds(5)));
        served.ShouldBe(new[] { 1, 2 });
    }

    [Fact]
    public async Task DisposalDuringHeldBatchRetiresEveryWaiterWithoutRunningQueuedProjection()
    {
        TenantMembershipStatusRefreshDrain drain = new();
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        bool alive = true;
        int batches = 0;
        async Task Refresh(int generation, bool projection)
        {
            batches++;
            await release.Task;
        }
        Task owner = drain.RunAsync(false, true, () => 1, () => alive, Refresh);
        Task manual = drain.RunAsync(true, true, () => 1, () => alive, Refresh);
        alive = false;
        release.SetResult();
        await Task.WhenAll(owner, manual).WaitAsync(TimeSpan.FromSeconds(5));
        batches.ShouldBe(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QueuedProjectionIntentDrainsAfterHeldFailureAndAllWaitersObserveIt(bool fail)
    {
        TenantMembershipStatusRefreshDrain drain = new();
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        List<bool> batches = [];
        async Task Refresh(int expectedGeneration, bool projection)
        {
            batches.Add(projection);
            if (batches.Count == 1)
            {
                entered.SetResult();
                await release.Task;
                if (fail)
                {
                    throw new InvalidOperationException("batch failure");
                }
            }
        }
        Task owner = drain.RunAsync(false, true, () => 1, () => true, Refresh);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Task manual = drain.RunAsync(true, true, () => 1, () => true, Refresh);
        await drain.RunAsync(false, false, () => 1, () => true, Refresh);
        manual.IsCompleted.ShouldBeFalse();
        release.SetResult();
        if (fail)
        {
            await Should.ThrowAsync<InvalidOperationException>(() => owner.WaitAsync(TimeSpan.FromSeconds(5)));
            await Should.ThrowAsync<InvalidOperationException>(() => manual.WaitAsync(TimeSpan.FromSeconds(5)));
        }
        else
        {
            await Task.WhenAll(owner, manual).WaitAsync(TimeSpan.FromSeconds(5));
        }
        batches.ShouldBe(new[] { false, true });
        await drain.RunAsync(false, true, () => 1, () => true, Refresh);
        batches.ShouldBe(new[] { false, true, false });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReplacementDropsOnlyObsoleteProjectionIntentAndContinuesItsOwnStatus(bool replacementRequestsProjection)
    {
        TenantMembershipStatusRefreshDrain drain = new();
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int generation = 1;
        List<bool> batches = [];
        async Task Refresh(int expectedGeneration, bool projection)
        {
            batches.Add(projection);
            if (batches.Count == 1)
            {
                await release.Task;
            }
        }
        Task owner = drain.RunAsync(false, true, () => generation, () => true, Refresh);
        await drain.RunAsync(true, false, () => generation, () => true, Refresh);
        generation++;
        Task replacement = drain.RunAsync(replacementRequestsProjection, true, () => generation, () => true, Refresh);
        release.SetResult();
        await Task.WhenAll(owner, replacement).WaitAsync(TimeSpan.FromSeconds(5));
        batches.ShouldBe(new[] { false, replacementRequestsProjection });
    }

    [Fact]
    public async Task SustainedSynchronousNudgesUseOneIterativeOwnerAndStopWhenDisposed()
    {
        TenantMembershipStatusRefreshDrain drain = new();
        int batches = 0;
        bool alive = true;
        Func<int, bool, Task> refresh = null!;
        refresh = (_, _) =>
        {
            if (++batches < 10000)
            {
                drain.RunAsync(false, false, () => 1, () => alive, refresh).IsCompletedSuccessfully.ShouldBeTrue();
            }
            return Task.CompletedTask;
        };
        await drain.RunAsync(false, true, () => 1, () => alive, refresh);
        batches.ShouldBe(10000);
        alive = false;
        await drain.RunAsync(true, true, () => 1, () => alive, refresh);
        batches.ShouldBe(10000);
    }
}
