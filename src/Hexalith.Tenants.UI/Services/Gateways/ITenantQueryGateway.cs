using Hexalith.Tenants.UI.State.TenantList;

namespace Hexalith.Tenants.UI.Services.Gateways;

public interface ITenantQueryGateway
{
    Task<TenantListSnapshot> ListTenantsAsync(
        TenantListRequest request,
        TenantListSnapshot? previous,
        CancellationToken cancellationToken = default);
}
