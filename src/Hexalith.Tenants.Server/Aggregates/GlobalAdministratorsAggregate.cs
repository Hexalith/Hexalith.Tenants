using Hexalith.EventStore.Client.Aggregates;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Events.Rejections;
using Hexalith.Tenants.Contracts.Identity;

namespace Hexalith.Tenants.Server.Aggregates;

public class GlobalAdministratorsAggregate : EventStoreAggregate<GlobalAdministratorsState> {
    /// <summary>Bootstraps only the authenticated command actor as the first administrator.</summary>
    /// <param name="command">The bootstrap target.</param>
    /// <param name="state">The current administrator state.</param>
    /// <param name="envelope">The envelope carrying the authenticated actor.</param>
    /// <returns>A bootstrap event or a permission/already-bootstrapped rejection.</returns>
    public static DomainResult Handle(BootstrapGlobalAdmin command, GlobalAdministratorsState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);
        if (!string.Equals(command.UserId, envelope.UserId, StringComparison.Ordinal))
        {
            return Unauthorized(command, envelope.UserId);
        }

        return state?.Bootstrapped == true
            ? DomainResult.Rejection([new GlobalAdminAlreadyBootstrappedRejection(TenantIdentity.DefaultTenantId)])
            : DomainResult.Success([new GlobalAdministratorSet(
                TenantIdentity.DefaultTenantId,
                command.UserId,
                envelope.UserId,
                DateTimeOffset.UtcNow)]);
    }

    public static DomainResult Handle(SetGlobalAdministrator command, GlobalAdministratorsState? state, CommandEnvelope envelope) {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);
        return state switch {
            null => Unauthorized(command, envelope.UserId),
            _ when !state.Administrators.Contains(envelope.UserId) => Unauthorized(command, envelope.UserId),
            _ when state.Administrators.Contains(command.UserId)
                => DomainResult.Rejection([new GlobalAdministratorAlreadyExistsRejection(TenantIdentity.DefaultTenantId, command.UserId)]),
            _ => DomainResult.Success([new GlobalAdministratorSet(
                TenantIdentity.DefaultTenantId,
                command.UserId,
                envelope.UserId,
                DateTimeOffset.UtcNow)]),
        };
    }

    public static DomainResult Handle(RemoveGlobalAdministrator command, GlobalAdministratorsState? state, CommandEnvelope envelope) {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);
        return state switch {
            null => DomainResult.Rejection([new GlobalAdministratorNotFoundRejection(TenantIdentity.DefaultTenantId, command.UserId)]),
            _ when !state.Administrators.Contains(envelope.UserId) => Unauthorized(command, envelope.UserId),
            _ when !state.Administrators.Contains(command.UserId)
                => DomainResult.Rejection([new GlobalAdministratorNotFoundRejection(TenantIdentity.DefaultTenantId, command.UserId)]),
            _ when state.Administrators.Count == 1 => DomainResult.Rejection([new LastGlobalAdministratorRejection(TenantIdentity.DefaultTenantId, command.UserId)]),
            _ => DomainResult.Success([new GlobalAdministratorRemoved(
                TenantIdentity.DefaultTenantId,
                command.UserId,
                envelope.UserId,
                DateTimeOffset.UtcNow)]),
        };
    }

    private static DomainResult Unauthorized(object command, string actorUserId)
        => DomainResult.Rejection([new InsufficientPermissionsRejection(
            TenantIdentity.DefaultTenantId,
            actorUserId,
            ActorRole: null,
            command.GetType().Name)]);
}
