namespace Hexalith.Tenants.UI.State.TenantDetail;

/// <summary>Contains current tenant-scoped authority for metadata and membership mutations.</summary>
/// <param name="Metadata">Owner, contributor, or global-administrator reflection.</param>
/// <param name="Membership">Owner or global-administrator reflection.</param>
public sealed record TenantMutationAuthorizationReflection(
    TenantLifecycleAuthorizationReflectionState Metadata,
    TenantLifecycleAuthorizationReflectionState Membership)
{
    /// <summary>Gets the fail-closed result when current scoped authority cannot be proven.</summary>
    public static TenantMutationAuthorizationReflection Indeterminate { get; } = new(
        TenantLifecycleAuthorizationReflectionState.Indeterminate,
        TenantLifecycleAuthorizationReflectionState.Indeterminate);
}
