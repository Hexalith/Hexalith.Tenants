namespace Hexalith.Tenants.UI.State.UserTenants;

public sealed record UserTenantMembershipRequest(
    string? Cursor = null,
    int PageSize = 20,
    string? ETag = null);
