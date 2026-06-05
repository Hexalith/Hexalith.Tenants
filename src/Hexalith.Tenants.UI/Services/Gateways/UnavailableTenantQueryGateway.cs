using Hexalith.Tenants.UI.State.TenantList;

namespace Hexalith.Tenants.UI.Services.Gateways;

internal sealed class UnavailableTenantQueryGateway : ITenantQueryGateway
{
    public Task<TenantListSnapshot> ListTenantsAsync(
        TenantListRequest request,
        TenantListSnapshot? previous,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantListSnapshot.Error("Tenant query gateway configuration is missing."));
}
