using Hexalith.Tenants.UI.State.TenantDetail;

namespace Hexalith.Tenants.UI.Services.Gateways;

internal sealed class TenantsBffComposition(
    ITenantCommandGateway commandGateway,
    IHttpContextAccessor? httpContextAccessor = null) : ITenantsBffComposition {
    public bool IsReadSurfaceConnected => true;

    public bool IsCommandSurfaceConnected => commandGateway is not UnavailableTenantCommandGateway;

    public TenantLifecycleAuthorizationReflectionState LifecycleAuthorizationReflection
        => IsCommandSurfaceConnected && TenantsGlobalAdministratorClaims.IsGlobalAdministrator(httpContextAccessor?.HttpContext?.User)
            ? TenantLifecycleAuthorizationReflectionState.Authorized
            : TenantLifecycleAuthorizationReflectionState.Indeterminate;

    public TenantLifecycleAuthorizationReflectionState GlobalAdministratorsAuthorizationReflection
        => TenantsGlobalAdministratorClaims.IsGlobalAdministrator(httpContextAccessor?.HttpContext?.User)
            ? TenantLifecycleAuthorizationReflectionState.Authorized
            : TenantLifecycleAuthorizationReflectionState.Indeterminate;
}
