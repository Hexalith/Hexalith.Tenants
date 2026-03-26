using Hexalith.EventStore.Client.Aggregates;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Events.Rejections;

namespace Hexalith.Tenants.Server.Aggregates;

public class GlobalAdministratorsAggregate : EventStoreAggregate<GlobalAdministratorsState> {
    public static DomainResult Handle(BootstrapGlobalAdmin command, GlobalAdministratorsState? state) {
        ArgumentNullException.ThrowIfNull(command);
        return state?.Bootstrapped == true
            ? DomainResult.Rejection([new GlobalAdminAlreadyBootstrappedRejection("system")])
            : DomainResult.Success([new GlobalAdministratorSet("system", command.UserId)]);
    }

    public static DomainResult Handle(SetGlobalAdministrator command, GlobalAdministratorsState? state) {
        ArgumentNullException.ThrowIfNull(command);
        return state is not null && state.Administrators.Contains(command.UserId)
            ? DomainResult.NoOp()
            : DomainResult.Success([new GlobalAdministratorSet("system", command.UserId)]);
    }

    public static DomainResult Handle(RemoveGlobalAdministrator command, GlobalAdministratorsState? state) {
        ArgumentNullException.ThrowIfNull(command);
        return state switch {
            null => DomainResult.NoOp(),
            _ when !state.Administrators.Contains(command.UserId) => DomainResult.NoOp(),
            _ when state.Administrators.Count == 1 => DomainResult.Rejection([new LastGlobalAdministratorRejection("system", command.UserId)]),
            _ => DomainResult.Success([new GlobalAdministratorRemoved("system", command.UserId)]),
        };
    }
}
