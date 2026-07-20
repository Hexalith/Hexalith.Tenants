namespace Hexalith.Tenants.UI.State.TenantList;

/// <summary>
/// Defines canonical sort columns for user-membership lookup results.
/// </summary>
public static class UserTenantMembershipSortColumns
{
    /// <summary>Sorts by tenant identifier.</summary>
    public const string Tenant = "tenant";

    /// <summary>Sorts by tenant display name.</summary>
    public const string Name = "name";

    /// <summary>Sorts by membership role.</summary>
    public const string Role = "role";

    /// <summary>Sorts by tenant status.</summary>
    public const string Status = "status";
}
