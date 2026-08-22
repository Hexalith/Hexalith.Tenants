namespace Hexalith.Tenants.UI.State.TenantDetail;

/// <summary>Represents server-reflected lifecycle mutation authority.</summary>
public enum TenantLifecycleAuthorizationReflectionState
{
    /// <summary>Authority could not be determined safely.</summary>
    Indeterminate,

    /// <summary>The required lifecycle authority is proven.</summary>
    Authorized,

    /// <summary>The principal is proven to lack lifecycle authority.</summary>
    MissingPermission,
}
