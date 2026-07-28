namespace Hexalith.Tenants.UI.Services;

/// <summary>
/// Releases one reference-counted tenant read refresh callback.
/// </summary>
internal sealed class TenantReadRefreshLease(Func<ValueTask>? release = null) : IAsyncDisposable
{
    internal static readonly TenantReadRefreshLease Empty = new();

    private Func<ValueTask>? _release = release;

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Func<ValueTask>? callback = Interlocked.Exchange(ref _release, null);
        return callback is null ? ValueTask.CompletedTask : callback();
    }
}
