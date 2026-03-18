using Hexalith.Tenants.Contracts.Events;

namespace Hexalith.Tenants.Server.Projections;

public sealed class GlobalAdministratorReadModel
{
    public HashSet<string> Administrators { get; private set; } = new();

    public void Apply(GlobalAdministratorSet e)
    {
        ArgumentNullException.ThrowIfNull(e);
        Administrators.Add(e.UserId);
    }

    public void Apply(GlobalAdministratorRemoved e)
    {
        ArgumentNullException.ThrowIfNull(e);
        Administrators.Remove(e.UserId);
    }
}
