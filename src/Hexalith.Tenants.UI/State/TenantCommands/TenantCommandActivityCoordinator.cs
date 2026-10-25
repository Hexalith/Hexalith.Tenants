namespace Hexalith.Tenants.UI.State.TenantCommands;

/// <summary>Serializes parent activity callbacks without transferring ownership between command attempts.</summary>
internal sealed class TenantCommandActivityCoordinator
{
    private readonly object _sync = new();
    private SemaphoreSlim _gate = new(1, 1);
    private int _gateGeneration;
    private TenantRetainedCommandAttempt? _gateAttempt;

    /// <summary>Updates parent activity for the exact captured command attempt.</summary>
    /// <param name="isActive">Requested activity state.</param>
    /// <param name="isUnavailable">Whether this renderer can no longer acquire activity.</param>
    /// <param name="generation">Current attempt generation.</param>
    /// <param name="attempt">Current retained command ownership.</param>
    /// <param name="begin">Acquires local admission when an active request has no attempt yet.</param>
    /// <param name="raised">Current parent notification state.</param>
    /// <param name="setRaised">Publishes the parent notification state.</param>
    /// <param name="detach">Detaches only the supplied exact ownership.</param>
    /// <param name="notify">Requests parent activity admission.</param>
    /// <returns>Whether the requested state was admitted for the same attempt.</returns>
    internal async Task<bool> SetAsync(bool isActive, Func<bool> isUnavailable, Func<int> generation,
        Func<TenantRetainedCommandAttempt?> attempt, Func<bool> begin, Func<bool> raised,
        Action<bool> setRaised, Action<TenantRetainedCommandAttempt?> detach, Func<bool, Task<bool>> notify)
    {
        int expectedGeneration = generation();
        TenantRetainedCommandAttempt? expectedAttempt = attempt();
        if (isUnavailable())
        {
            return !isActive;
        }
        if (isActive && expectedAttempt is null)
        {
            if (!begin())
            {
                return false;
            }
            expectedAttempt = attempt();
        }
        SemaphoreSlim gate;
        lock (_sync)
        {
            if (_gateGeneration != expectedGeneration || !ReferenceEquals(_gateAttempt, expectedAttempt))
            {
                // A held callback for an obsolete attempt must not block its replacement's admission.
                _gate = new(1, 1);
                _gateGeneration = expectedGeneration;
                _gateAttempt = expectedAttempt;
            }
            gate = _gate;
        }
        await gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (isUnavailable() || expectedGeneration != generation() || !ReferenceEquals(expectedAttempt, attempt()))
            {
                return !isActive;
            }
            if (raised() == isActive)
            {
                return true;
            }

            bool accepted;
            try
            {
                accepted = await notify(isActive).ConfigureAwait(false);
            }
            catch (Exception)
            {
                if (!isActive)
                {
                    throw;
                }
                if (!isUnavailable()) detach(expectedAttempt);
                return false;
            }
            if (isUnavailable() || expectedGeneration != generation() || !ReferenceEquals(expectedAttempt, attempt()))
            {
                return !isActive;
            }
            if (isActive && !accepted)
            {
                detach(expectedAttempt);
                return false;
            }
            setRaised(isActive);
            return true;
        }
        finally
        {
            gate.Release();
        }
    }
}
