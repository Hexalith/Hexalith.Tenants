namespace Hexalith.Tenants.UI.State.TenantCommands;

/// <summary>Isolates concurrent proof reads by literal tenant, caller and route generation.</summary>
internal sealed class TenantProofScope
{
    private readonly Lock _sync = new();
    private readonly HashSet<CancellationTokenSource> _cancellations = [];
    private string? _tenantId;
    private string? _callerScope;
    private long _generation;

    /// <summary>Checks the last scoped literal tenant independently of caller identity.</summary>
    /// <param name="tenantId">Literal route tenant to compare with the scope.</param>
    /// <returns>Whether the scope belongs to that tenant, using ordinal identity.</returns>
    internal bool IsForTenant(string? tenantId)
    {
        lock (_sync)
        {
            return string.Equals(_tenantId, tenantId, StringComparison.Ordinal);
        }
    }

    /// <summary>Retires reads when the literal tenant or caller changes.</summary>
    /// <param name="tenantId">New literal tenant identifier, or null to invalidate the scope.</param>
    /// <param name="callerScope">New authenticated caller scope.</param>
    internal void Reset(string? tenantId, string? callerScope)
    {
        lock (_sync)
        {
            if (Matches(tenantId, callerScope))
            {
                return;
            }

            _tenantId = tenantId;
            _callerScope = callerScope;
            _generation++;
            // Cancellation and completion share the lock. Reset never disposes a source: its read may
            // still register cancellation while unwinding, and only the operation owns that disposal.
            CancellationTokenSource[] retired = [.. _cancellations];
            foreach (CancellationTokenSource cancellation in retired)
            {
                // A cancellation callback may complete another retired read or begin a new-generation
                // read reentrantly. Remove only the old source, and skip one its owner already completed.
                if (_cancellations.Remove(cancellation))
                {
                    cancellation.Cancel();
                }
            }
        }
    }

    /// <summary>Begins another independent read within the current proof generation.</summary>
    /// <param name="tenantId">Literal tenant whose proof is requested.</param>
    /// <param name="callerScope">Caller scope captured by the requesting page.</param>
    /// <param name="isCurrent">Checks current page lifetime, route and authenticated caller.</param>
    /// <returns>An owned operation, or null when its route or caller is obsolete.</returns>
    internal TenantProofOperation? Begin(string tenantId, string? callerScope, Func<bool> isCurrent)
    {
        ArgumentNullException.ThrowIfNull(isCurrent);
        lock (_sync)
        {
            if (!Matches(tenantId, callerScope) || !isCurrent())
            {
                return null;
            }

            var cancellation = new CancellationTokenSource();
            _ = _cancellations.Add(cancellation);
            return new(_generation, callerScope, cancellation, () => Complete(cancellation));
        }
    }

    /// <summary>Checks that a completed read still belongs to the active page scope.</summary>
    /// <param name="generation">Generation captured before the read.</param>
    /// <param name="tenantId">Literal tenant whose proof completed.</param>
    /// <param name="callerScope">Caller scope captured by the page at validation.</param>
    /// <param name="isCurrent">Checks current page lifetime, route and authenticated caller.</param>
    /// <returns>Whether the result can still be applied.</returns>
    internal bool CanApply(long generation, string tenantId, string? callerScope, Func<bool> isCurrent)
    {
        ArgumentNullException.ThrowIfNull(isCurrent);
        lock (_sync)
        {
            return generation == _generation && Matches(tenantId, callerScope) && isCurrent();
        }
    }

    private bool Matches(string? tenantId, string? callerScope)
        => string.Equals(_tenantId, tenantId, StringComparison.Ordinal)
            && string.Equals(_callerScope, callerScope, StringComparison.Ordinal);

    private void Complete(CancellationTokenSource cancellation)
    {
        lock (_sync)
        {
            _ = _cancellations.Remove(cancellation);
        }

        cancellation.Dispose();
    }
}
