namespace Hexalith.Tenants.UI.Services.Gateways;

internal sealed class TenantsBffComposition : ITenantsBffComposition
{
    public bool IsReadSurfaceConnected => true;

    public bool IsCommandSurfaceConnected => false;
}
