using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;

namespace Hexalith.Tenants.Server.Aggregates;

public sealed class TenantState {
    public string TenantId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TenantStatus Status { get; set; }
    public Dictionary<string, TenantRole> Users { get; set; } = [];
    public bool HasMembershipHistory { get; set; }
    public Dictionary<string, string> Configuration { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }

    public void Apply(TenantCreated e) {
        ArgumentNullException.ThrowIfNull(e);
        TenantId = e.TenantId;
        Name = e.Name;
        Description = e.Description;
        Status = TenantStatus.Active;
        CreatedAt = e.CreatedAt;
    }

    public void Apply(TenantUpdated e) {
        ArgumentNullException.ThrowIfNull(e);
        Name = e.Name;
        Description = e.Description;
    }

    public void Apply(TenantDisabled e) {
        ArgumentNullException.ThrowIfNull(e);
        Status = TenantStatus.Disabled;
    }

    public void Apply(TenantEnabled e) {
        ArgumentNullException.ThrowIfNull(e);
        Status = TenantStatus.Active;
    }

    public void Apply(UserAddedToTenant e) {
        ArgumentNullException.ThrowIfNull(e);
        Users[e.UserId] = e.Role;
        HasMembershipHistory = true;
    }

    public void Apply(UserRemovedFromTenant e) {
        ArgumentNullException.ThrowIfNull(e);
        _ = Users.Remove(e.UserId);
    }

    public void Apply(UserRoleChanged e) {
        ArgumentNullException.ThrowIfNull(e);
        Users[e.UserId] = e.NewRole;
    }

    public void Apply(TenantConfigurationSet e) {
        ArgumentNullException.ThrowIfNull(e);
        Configuration[e.Key] = e.Value;
    }

    public void Apply(TenantConfigurationRemoved e) {
        ArgumentNullException.ThrowIfNull(e);
        _ = Configuration.Remove(e.Key);
    }
}
