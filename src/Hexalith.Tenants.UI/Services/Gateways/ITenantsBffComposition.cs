using Hexalith.Tenants.UI.State.TenantDetail;

namespace Hexalith.Tenants.UI.Services.Gateways;

public interface ITenantsBffComposition
{
    bool IsReadSurfaceConnected { get; }

    bool IsCommandSurfaceConnected { get; }

    TenantLifecycleAuthorizationReflectionState LifecycleAuthorizationReflection
        => TenantLifecycleAuthorizationReflectionState.Indeterminate;
}
