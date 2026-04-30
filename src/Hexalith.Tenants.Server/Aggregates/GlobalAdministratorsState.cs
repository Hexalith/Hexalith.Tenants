using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Events.Rejections;

namespace Hexalith.Tenants.Server.Aggregates;

public sealed class GlobalAdministratorsState {
    public HashSet<string> Administrators { get; private set; } = [];
    public bool Bootstrapped { get; private set; }

    public void Apply(GlobalAdministratorSet e) {
        ArgumentNullException.ThrowIfNull(e);
        _ = Administrators.Add(e.UserId);
        Bootstrapped = true;
    }

    public void Apply(GlobalAdministratorRemoved e) {
        ArgumentNullException.ThrowIfNull(e);
        _ = Administrators.Remove(e.UserId);
    }

    public void Apply(GlobalAdminAlreadyBootstrappedRejection e) {
        ArgumentNullException.ThrowIfNull(e);
        MarkReplayOnlyEventHandled();
    }

    public void Apply(LastGlobalAdministratorRejection e) {
        ArgumentNullException.ThrowIfNull(e);
        MarkReplayOnlyEventHandled();
    }

    private void MarkReplayOnlyEventHandled() => _ = Bootstrapped;
}
