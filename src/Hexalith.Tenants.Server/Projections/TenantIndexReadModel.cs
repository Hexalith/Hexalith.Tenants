using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;

namespace Hexalith.Tenants.Server.Projections;

public sealed class TenantIndexReadModel
{
    public Dictionary<string, TenantIndexEntry> Tenants { get; private set; } = new();

    public Dictionary<string, Dictionary<string, TenantRole>> UserTenants { get; private set; } = new();

    public void Apply(TenantCreated e)
    {
        ArgumentNullException.ThrowIfNull(e);
        Tenants[e.TenantId] = new TenantIndexEntry(e.Name, TenantStatus.Active);
    }

    public void Apply(TenantUpdated e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (Tenants.TryGetValue(e.TenantId, out TenantIndexEntry? existing))
        {
            Tenants[e.TenantId] = existing with { Name = e.Name };
        }
    }

    public void Apply(TenantDisabled e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (Tenants.TryGetValue(e.TenantId, out TenantIndexEntry? existing))
        {
            Tenants[e.TenantId] = existing with { Status = TenantStatus.Disabled };
        }
    }

    public void Apply(TenantEnabled e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (Tenants.TryGetValue(e.TenantId, out TenantIndexEntry? existing))
        {
            Tenants[e.TenantId] = existing with { Status = TenantStatus.Active };
        }
    }

    public void Apply(UserAddedToTenant e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!UserTenants.TryGetValue(e.UserId, out Dictionary<string, TenantRole>? tenants))
        {
            tenants = new Dictionary<string, TenantRole>();
            UserTenants[e.UserId] = tenants;
        }

        tenants[e.TenantId] = e.Role;
    }

    public void Apply(UserRemovedFromTenant e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (UserTenants.TryGetValue(e.UserId, out Dictionary<string, TenantRole>? tenants))
        {
            tenants.Remove(e.TenantId);
            if (tenants.Count == 0)
            {
                UserTenants.Remove(e.UserId);
            }
        }
    }

    public void Apply(UserRoleChanged e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (UserTenants.TryGetValue(e.UserId, out Dictionary<string, TenantRole>? tenants))
        {
            tenants[e.TenantId] = e.NewRole;
        }
    }
}
