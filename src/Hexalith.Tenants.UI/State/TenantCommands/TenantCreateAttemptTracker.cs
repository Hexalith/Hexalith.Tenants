namespace Hexalith.Tenants.UI.State.TenantCommands;

/// <summary>
/// Circuit-local retention for in-flight create-tenant attempt handles, so one logical attempt keeps its
/// message id and correlation across component re-mounts such as workspace tab round trips, accordion
/// collapse, or a re-render triggered by a read refresh.
/// </summary>
/// <remarks>
/// Scope is the interactive circuit, matching <see cref="TenantAggregateCommandAdmissionGate"/>. A brand
/// new circuit starts with no retained attempt, so the create flow treats a re-submit as a fresh attempt
/// only when nothing is retained for that tenant id; when a handle is retained the flow refreshes its
/// status and reuses the original message id instead of dispatching the same create twice.
/// </remarks>
public sealed class TenantCreateAttemptTracker
{
    private readonly object _sync = new();
    private readonly Dictionary<string, TenantCommandTrackingHandle> _handleByTenantId = new(StringComparer.Ordinal);

    /// <summary>
    /// Retains <paramref name="handle"/> as the tracked attempt for <paramref name="tenantId"/>.
    /// </summary>
    /// <param name="tenantId">Literal caller-supplied tenant id the attempt targets.</param>
    /// <param name="handle">Tracking handle returned when the command was accepted.</param>
    public void Remember(string tenantId, TenantCommandTrackingHandle handle)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(handle);

        lock (_sync)
        {
            _handleByTenantId[tenantId] = handle;
        }
    }

    /// <summary>
    /// Returns the retained attempt handle for <paramref name="tenantId"/>, when one is still tracked.
    /// </summary>
    /// <param name="tenantId">Literal caller-supplied tenant id to look up.</param>
    /// <returns>The retained handle, or <see langword="null"/> when nothing is tracked.</returns>
    public TenantCommandTrackingHandle? Find(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return null;
        }

        lock (_sync)
        {
            return _handleByTenantId.TryGetValue(tenantId, out TenantCommandTrackingHandle? handle)
                ? handle
                : null;
        }
    }

    /// <summary>
    /// Drops the retained attempt for <paramref name="tenantId"/> once it reaches a terminal outcome.
    /// </summary>
    /// <param name="tenantId">Literal caller-supplied tenant id to forget.</param>
    public void Forget(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return;
        }

        lock (_sync)
        {
            _ = _handleByTenantId.Remove(tenantId);
        }
    }
}
