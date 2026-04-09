using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;

namespace Hexalith.Tenants.Server.Projections;

public sealed class TenantIndexReadModel {
    public Dictionary<string, TenantIndexEntry> Tenants { get; set; } = [];

    public Dictionary<string, Dictionary<string, TenantRole>> UserTenants { get; set; } = [];

    public void Apply(TenantCreated e) {
        ArgumentNullException.ThrowIfNull(e);
        if (Tenants.ContainsKey(e.TenantId)) {
            return;
        }

        Tenants[e.TenantId] = new TenantIndexEntry(e.Name, TenantStatus.Active);
    }

    public void Apply(TenantUpdated e) {
        ArgumentNullException.ThrowIfNull(e);
        if (Tenants.TryGetValue(e.TenantId, out TenantIndexEntry? existing)) {
            Tenants[e.TenantId] = existing with { Name = e.Name };
        }
    }

    public void Apply(TenantDisabled e) {
        ArgumentNullException.ThrowIfNull(e);
        if (Tenants.TryGetValue(e.TenantId, out TenantIndexEntry? existing)) {
            Tenants[e.TenantId] = existing with { Status = TenantStatus.Disabled };
        }
    }

    public void Apply(TenantEnabled e) {
        ArgumentNullException.ThrowIfNull(e);
        if (Tenants.TryGetValue(e.TenantId, out TenantIndexEntry? existing)) {
            Tenants[e.TenantId] = existing with { Status = TenantStatus.Active };
        }
    }

    public void Apply(UserAddedToTenant e) {
        ArgumentNullException.ThrowIfNull(e);
        if (!Tenants.ContainsKey(e.TenantId)) {
            return;
        }

        if (!UserTenants.TryGetValue(e.UserId, out Dictionary<string, TenantRole>? tenants)) {
            tenants = [];
            UserTenants[e.UserId] = tenants;
        }

        tenants[e.TenantId] = e.Role;
    }

    public void Apply(UserRemovedFromTenant e) {
        ArgumentNullException.ThrowIfNull(e);
        if (UserTenants.TryGetValue(e.UserId, out Dictionary<string, TenantRole>? tenants)) {
            _ = tenants.Remove(e.TenantId);
            if (tenants.Count == 0) {
                _ = UserTenants.Remove(e.UserId);
            }
        }
    }

    public void Apply(UserRoleChanged e) {
        ArgumentNullException.ThrowIfNull(e);
        if (UserTenants.TryGetValue(e.UserId, out Dictionary<string, TenantRole>? tenants)
            && tenants.ContainsKey(e.TenantId)) {
            tenants[e.TenantId] = e.NewRole;
        }
    }
}
