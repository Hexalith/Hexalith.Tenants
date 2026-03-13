namespace Hexalith.Tenants.Contracts.Commands;

public record CreateTenant(string TenantId, string Name, string? Description);
