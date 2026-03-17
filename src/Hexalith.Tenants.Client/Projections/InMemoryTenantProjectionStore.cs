using System.Collections.Concurrent;

namespace Hexalith.Tenants.Client.Projections;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="ITenantProjectionStore"/>.
/// Suitable for single-instance services. For scaled-out services,
/// consumers should implement <see cref="ITenantProjectionStore"/> against a durable store (DAPR state store, database, etc.).
/// </summary>
public class InMemoryTenantProjectionStore : ITenantProjectionStore
{
    private readonly ConcurrentDictionary<string, TenantLocalState> _states = new(StringComparer.Ordinal);

    /// <inheritdoc/>
    public Task<TenantLocalState?> GetAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        _ = _states.TryGetValue(tenantId, out TenantLocalState? state);
        return Task.FromResult(state?.Clone());
    }

    /// <inheritdoc/>
    public Task SaveAsync(TenantLocalState state, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(state.TenantId);
        _states[state.TenantId] = state.Clone();
        return Task.CompletedTask;
    }
}
