namespace Hexalith.Tenants.Contracts.Commands;

public record UpdateTenant(string TenantId, string Name, string? Description);
