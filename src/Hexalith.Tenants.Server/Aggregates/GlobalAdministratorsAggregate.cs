using Hexalith.EventStore.Client.Aggregates;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Events.Rejections;
using Hexalith.Tenants.Contracts.Identity;

namespace Hexalith.Tenants.Server.Aggregates;

public class GlobalAdministratorsAggregate : EventStoreAggregate<GlobalAdministratorsState> {
    public static DomainResult Handle(BootstrapGlobalAdmin command, GlobalAdministratorsState? state) {
        ArgumentNullException.ThrowIfNull(command);
        return state?.Bootstrapped == true
            ? DomainResult.Rejection([new GlobalAdminAlreadyBootstrappedRejection(TenantIdentity.DefaultTenantId)])
            : DomainResult.Success([new GlobalAdministratorSet(TenantIdentity.DefaultTenantId, command.UserId)]);
    }

    public static DomainResult Handle(SetGlobalAdministrator command, GlobalAdministratorsState? state) {
        ArgumentNullException.ThrowIfNull(command);
        return state is not null && state.Administrators.Contains(command.UserId)
            ? DomainResult.NoOp()
            : DomainResult.Success([new GlobalAdministratorSet(TenantIdentity.DefaultTenantId, command.UserId)]);
    }

    public static DomainResult Handle(RemoveGlobalAdministrator command, GlobalAdministratorsState? state) {
        ArgumentNullException.ThrowIfNull(command);
        return state switch {
            null => DomainResult.NoOp(),
            _ when !state.Administrators.Contains(command.UserId) => DomainResult.NoOp(),
            _ when state.Administrators.Count == 1 => DomainResult.Rejection([new LastGlobalAdministratorRejection(TenantIdentity.DefaultTenantId, command.UserId)]),
            _ => DomainResult.Success([new GlobalAdministratorRemoved(TenantIdentity.DefaultTenantId, command.UserId)]),
        };
    }
}
