namespace Hexalith.Tenants.Client.Projections;

/// <summary>
/// Abstraction for persisting and retrieving per-tenant local projections.
/// </summary>
public interface ITenantProjectionStore {
    /// <summary>
    /// Gets the tenant local state for the specified tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tenant local state, or null if not found.</returns>
    Task<TenantLocalState?> GetAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the tenant local state.
    /// </summary>
    /// <param name="state">The tenant local state to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SaveAsync(TenantLocalState state, CancellationToken cancellationToken = default);
}
