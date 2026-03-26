using Hexalith.Tenants.Contracts.Events;

namespace Hexalith.Tenants.Server.Projections;

public sealed class GlobalAdministratorReadModel {
    public HashSet<string> Administrators { get; private set; } = [];

    public void Apply(GlobalAdministratorSet e) {
        ArgumentNullException.ThrowIfNull(e);
        _ = Administrators.Add(e.UserId);
    }

    public void Apply(GlobalAdministratorRemoved e) {
        ArgumentNullException.ThrowIfNull(e);
        _ = Administrators.Remove(e.UserId);
    }
}
