namespace Hexalith.Tenants.Configuration;

public record TenantBootstrapOptions {
    public bool BootstrapGlobalAdminEnabled { get; init; } = true;

    public string? BootstrapGlobalAdminUserId { get; init; }
}
