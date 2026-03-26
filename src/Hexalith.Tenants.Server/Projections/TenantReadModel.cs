using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;

namespace Hexalith.Tenants.Server.Projections;

public sealed class TenantReadModel {
    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public TenantStatus Status { get; private set; }
    public Dictionary<string, TenantRole> Members { get; private set; } = [];
    public Dictionary<string, string> Configuration { get; private set; } = [];
    public DateTimeOffset CreatedAt { get; private set; }

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
        Members[e.UserId] = e.Role;
    }

    public void Apply(UserRemovedFromTenant e) {
        ArgumentNullException.ThrowIfNull(e);
        _ = Members.Remove(e.UserId);
    }

    public void Apply(UserRoleChanged e) {
        ArgumentNullException.ThrowIfNull(e);
        Members[e.UserId] = e.NewRole;
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
