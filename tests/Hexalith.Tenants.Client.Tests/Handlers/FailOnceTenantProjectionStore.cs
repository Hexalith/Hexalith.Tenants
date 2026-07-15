using Hexalith.Tenants.Client.Projections;

namespace Hexalith.Tenants.Client.Tests.Handlers;

/// <summary>Fails the first projection save, then delegates to an in-memory store.</summary>
internal sealed class FailOnceTenantProjectionStore : ITenantProjectionStore {
    private readonly InMemoryTenantProjectionStore _inner = new();
    private int _failuresRemaining = 1;

    /// <summary>Gets the number of attempted durable saves.</summary>
    public int SaveAttempts { get; private set; }

    /// <inheritdoc/>
    public Task<TenantLocalState?> GetAsync(string tenantId, CancellationToken cancellationToken = default)
        => _inner.GetAsync(tenantId, cancellationToken);

    /// <inheritdoc/>
    public Task SaveAsync(TenantLocalState state, CancellationToken cancellationToken = default) {
        SaveAttempts++;
        if (Interlocked.Exchange(ref _failuresRemaining, 0) == 1) {
            throw new InvalidOperationException("Injected projection persistence failure.");
        }

        return _inner.SaveAsync(state, cancellationToken);
    }
}
