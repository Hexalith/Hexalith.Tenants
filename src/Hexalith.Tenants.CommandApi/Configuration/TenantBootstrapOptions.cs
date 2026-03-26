namespace Hexalith.Tenants.CommandApi.Configuration;

public record TenantBootstrapOptions {
    public string? BootstrapGlobalAdminUserId { get; init; }
}
