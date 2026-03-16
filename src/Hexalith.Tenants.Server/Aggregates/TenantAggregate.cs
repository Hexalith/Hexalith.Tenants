using Hexalith.EventStore.Client.Aggregates;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Events.Rejections;

namespace Hexalith.Tenants.Server.Aggregates;

public class TenantAggregate : EventStoreAggregate<TenantState>
{
    public static DomainResult Handle(CreateTenant command, TenantState? state)
    {
        ArgumentNullException.ThrowIfNull(command);
        return state is not null
            ? DomainResult.Rejection([new TenantAlreadyExistsRejection(command.TenantId)])
            : DomainResult.Success([new TenantCreated(command.TenantId, command.Name, command.Description, DateTimeOffset.UtcNow)]);
    }

    public static DomainResult Handle(UpdateTenant command, TenantState? state)
    {
        ArgumentNullException.ThrowIfNull(command);
        return state switch
        {
            null => DomainResult.Rejection([new TenantNotFoundRejection(command.TenantId)]),
            { Status: TenantStatus.Disabled } => DomainResult.Rejection([new TenantDisabledRejection(command.TenantId)]),
            _ => DomainResult.Success([new TenantUpdated(command.TenantId, command.Name, command.Description)]),
        };
    }

    public static DomainResult Handle(DisableTenant command, TenantState? state)
    {
        ArgumentNullException.ThrowIfNull(command);
        return state switch
        {
            null => DomainResult.Rejection([new TenantNotFoundRejection(command.TenantId)]),
            { Status: TenantStatus.Disabled } => DomainResult.NoOp(),
            _ => DomainResult.Success([new TenantDisabled(command.TenantId, DateTimeOffset.UtcNow)]),
        };
    }

    public static DomainResult Handle(EnableTenant command, TenantState? state)
    {
        ArgumentNullException.ThrowIfNull(command);
        return state switch
        {
            null => DomainResult.Rejection([new TenantNotFoundRejection(command.TenantId)]),
            { Status: TenantStatus.Active } => DomainResult.NoOp(),
            _ => DomainResult.Success([new TenantEnabled(command.TenantId, DateTimeOffset.UtcNow)]),
        };
    }

    public static DomainResult Handle(AddUserToTenant command, TenantState? state)
    {
        ArgumentNullException.ThrowIfNull(command);
        return state switch
        {
            null => DomainResult.Rejection([new TenantNotFoundRejection(command.TenantId)]),
            { Status: TenantStatus.Disabled } => DomainResult.Rejection([new TenantDisabledRejection(command.TenantId)]),
            _ when !Enum.IsDefined(command.Role)
                => DomainResult.Rejection([new RoleEscalationRejection(command.TenantId, command.UserId, command.Role)]),
            _ when state.Users.ContainsKey(command.UserId)
                => DomainResult.Rejection([new UserAlreadyInTenantRejection(command.TenantId, command.UserId, state.Users[command.UserId])]),
            _ => DomainResult.Success([new UserAddedToTenant(command.TenantId, command.UserId, command.Role)]),
        };
    }

    public static DomainResult Handle(RemoveUserFromTenant command, TenantState? state)
    {
        ArgumentNullException.ThrowIfNull(command);
        return state switch
        {
            null => DomainResult.Rejection([new TenantNotFoundRejection(command.TenantId)]),
            { Status: TenantStatus.Disabled } => DomainResult.Rejection([new TenantDisabledRejection(command.TenantId)]),
            _ when !state.Users.ContainsKey(command.UserId)
                => DomainResult.Rejection([new UserNotInTenantRejection(command.TenantId, command.UserId)]),
            _ => DomainResult.Success([new UserRemovedFromTenant(command.TenantId, command.UserId)]),
        };
    }

    public static DomainResult Handle(ChangeUserRole command, TenantState? state)
    {
        ArgumentNullException.ThrowIfNull(command);
        return state switch
        {
            null => DomainResult.Rejection([new TenantNotFoundRejection(command.TenantId)]),
            { Status: TenantStatus.Disabled } => DomainResult.Rejection([new TenantDisabledRejection(command.TenantId)]),
            _ when !Enum.IsDefined(command.NewRole)
                => DomainResult.Rejection([new RoleEscalationRejection(command.TenantId, command.UserId, command.NewRole)]),
            _ when !state.Users.ContainsKey(command.UserId)
                => DomainResult.Rejection([new UserNotInTenantRejection(command.TenantId, command.UserId)]),
            _ when state.Users[command.UserId] == command.NewRole
                => DomainResult.NoOp(),
            _ => DomainResult.Success([new UserRoleChanged(command.TenantId, command.UserId, state.Users[command.UserId], command.NewRole)]),
        };
    }
}
