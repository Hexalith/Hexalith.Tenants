namespace Hexalith.Tenants.Contracts.Commands;

public record RemoveUserFromTenant(string TenantId, string UserId);
