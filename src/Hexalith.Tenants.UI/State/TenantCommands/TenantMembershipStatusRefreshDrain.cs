namespace Hexalith.Tenants.UI.State.TenantCommands;

/// <summary>Serializes tenant membership status batches while retaining projection intent and attempt generation.</summary>
/// <remarks>Joined callers await the complete drain and observe its first failure, including across generation replacement.
/// A failed batch is consumed once; only explicitly queued work is served before that failure is reported.</remarks>
internal sealed class TenantMembershipStatusRefreshDrain
{
    private readonly object _sync = new();
    private TaskCompletionSource? _completion;
    private int _pending;
    private int _pendingGeneration;

    /// <summary>Enqueues status work and optionally waits until all explicitly queued recovery has drained.</summary>
    /// <param name="requestProjectionRefresh">Whether this request also requires a parent projection reload.</param>
    /// <param name="waitForRunningRefresh">Whether a joined caller waits for the owning drain.</param>
    /// <param name="generation">Reads the flow's current attempt generation.</param>
    /// <param name="canRefresh">Checks disposal and the flow-specific terminal audit recovery policy.</param>
    /// <param name="refresh">Reconciles one batch after checking its supplied generation before capturing attempt state.</param>
    /// <returns>The owning drain or the requested joined wait.</returns>
    internal async Task RunAsync(
        bool requestProjectionRefresh,
        bool waitForRunningRefresh,
        Func<int> generation,
        Func<bool> canRefresh,
        Func<int, bool, Task> refresh)
    {
        TaskCompletionSource completion;
        bool ownsRefresh;
        lock (_sync)
        {
            if (!canRefresh())
            {
                return;
            }
            int currentGeneration = generation();
            if (_pendingGeneration != currentGeneration)
            {
                _pending = 0;
                _pendingGeneration = currentGeneration;
            }
            _pending |= requestProjectionRefresh ? 3 : 1;
            ownsRefresh = _completion is null;
            completion = _completion ??= new(TaskCreationOptions.RunContinuationsAsynchronously);
        }
        if (!ownsRefresh)
        {
            if (waitForRunningRefresh)
            {
                await completion.Task.ConfigureAwait(false);
            }
            return;
        }

        Exception? failure = null;
        try
        {
            while (true)
            {
                int pending;
                int expectedGeneration;
                lock (_sync)
                {
                    pending = _pending;
                    _pending = 0;
                    if (!canRefresh() || pending == 0)
                    {
                        // Enqueue and retirement share this lock; no recursive release-window replay is needed.
                        _completion = null;
                        break;
                    }
                    expectedGeneration = _pendingGeneration;
                    if (expectedGeneration != generation())
                    {
                        continue;
                    }
                }
                try
                {
                    await refresh(expectedGeneration, (pending & 2) != 0).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    // Consume a failed batch once; serve only explicit queued work, then report its failure.
                    failure ??= exception;
                }
            }
        }
        finally
        {
            lock (_sync)
            {
                if (ReferenceEquals(_completion, completion))
                {
                    _completion = null;
                }
            }
            if (failure is null)
            {
                completion.TrySetResult();
            }
            else
            {
                completion.TrySetException(failure);
            }
        }
        await completion.Task.ConfigureAwait(false);
    }
}
