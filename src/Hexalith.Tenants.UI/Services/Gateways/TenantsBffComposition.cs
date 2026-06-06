namespace Hexalith.Tenants.UI.Services.Gateways;

internal sealed class TenantsBffComposition(ITenantCommandGateway commandGateway) : ITenantsBffComposition
{
    public bool IsReadSurfaceConnected => true;

    public bool IsCommandSurfaceConnected => commandGateway is not UnavailableTenantCommandGateway;
}
