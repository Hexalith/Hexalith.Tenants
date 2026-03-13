namespace Hexalith.Tenants.Contracts.Commands;

public record SetTenantConfiguration(string TenantId, string Key, string Value);
