namespace Hexalith.Tenants.UI.Services;

/// <summary>
/// Releases one reference-counted tenant read refresh callback.
/// </summary>
// Public to match the already-public TenantReadRefreshSubscription that hands it out. Hexalith.Tenants.UI
// is an application/container project, not one of the five published packages, so this widens no shipped
// API surface.
public sealed class TenantReadRefreshLease(Func<ValueTask>? release = null) : IAsyncDisposable
{
    /// <summary>
    /// The lease returned when no callback was registered — an optional-service no-op, or a failed setup.
    /// </summary>
    public static readonly TenantReadRefreshLease Empty = new();

    private Func<ValueTask>? _release = release;

    /// <summary>
    /// Gets a value indicating whether this lease represents a live registered subscription.
    /// </summary>
    /// <remarks>
    /// Callers must not record <see cref="Empty"/> as an established subscription. Doing so made a single
    /// transient setup failure permanent: the stored non-null lease satisfied the "already subscribed"
    /// guard, so no later attempt was ever made and the surface silently stopped auto-refreshing for the
    /// remainder of the circuit.
    /// </remarks>
    public bool IsSubscribed => !ReferenceEquals(this, Empty);

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Func<ValueTask>? callback = Interlocked.Exchange(ref _release, null);
        return callback is null ? ValueTask.CompletedTask : callback();
    }
}
