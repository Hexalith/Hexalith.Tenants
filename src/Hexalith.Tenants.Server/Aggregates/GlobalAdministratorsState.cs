using Hexalith.Tenants.Contracts.Events;

namespace Hexalith.Tenants.Server.Aggregates;

public sealed class GlobalAdministratorsState
{
    public HashSet<string> Administrators { get; private set; } = new();
    public bool Bootstrapped { get; private set; }

    public void Apply(GlobalAdministratorSet e)
    {
        ArgumentNullException.ThrowIfNull(e);
        Administrators.Add(e.UserId);
        Bootstrapped = true;
    }

    public void Apply(GlobalAdministratorRemoved e)
    {
        ArgumentNullException.ThrowIfNull(e);
        Administrators.Remove(e.UserId);
    }
}
