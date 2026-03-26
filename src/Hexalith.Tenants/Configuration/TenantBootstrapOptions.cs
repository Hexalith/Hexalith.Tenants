namespace Hexalith.Tenants.Configuration;

public record TenantBootstrapOptions {
    public string? BootstrapGlobalAdminUserId { get; init; }
}
