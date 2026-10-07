using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.DomainService;
using Hexalith.Tenants.Queries.Handlers;
using Hexalith.Tenants.Server.Projections;

namespace Hexalith.Tenants.Authorization;

/// <summary>
/// Re-establishes global-administrator authority at the Tenants domain-service boundary from the current
/// global-administrators read model (EventStore Story 5.5, FR28).
/// </summary>
/// <remarks>
/// The <c>actor:globalAdmin</c> command extension and <see cref="EventStore.Contracts.Queries.QueryEnvelope.IsGlobalAdmin"/>
/// arrive as untrusted wire hints. The EventStore domain-service SDK removes them on every inbound
/// <c>/process</c> and <c>/query</c> request unless this verifier confirms the acting user is a current member of
/// the Tenants global-administrators read model. A store failure propagates, so the request fails closed before
/// any aggregate or query work.
/// </remarks>
public sealed class TenantsGlobalAdministratorVerifier(IReadModelStore store) : IDomainServiceAdministratorVerifier {
    /// <inheritdoc/>
    public async Task<bool> IsCurrentGlobalAdministratorAsync(
        DomainServiceAdministratorClaim claim,
        CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(claim);
        if (string.IsNullOrWhiteSpace(claim.UserId)) {
            return false;
        }

        ReadModelEntry<GlobalAdministratorReadModel>? entry = await store
            .GetAsync<GlobalAdministratorReadModel>(
                TenantQueryHandlerBase.StateStoreName,
                TenantQueryHandlerBase.GlobalAdminProjectionKey,
                cancellationToken)
            .ConfigureAwait(false);
        return entry?.Value is { } model && model.Administrators.Contains(claim.UserId);
    }
}
