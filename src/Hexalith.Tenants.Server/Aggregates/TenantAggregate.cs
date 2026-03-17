using Hexalith.EventStore.Client.Aggregates;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Events.Rejections;

namespace Hexalith.Tenants.Server.Aggregates;

public class TenantAggregate : EventStoreAggregate<TenantState>
{
    // FR23: Configuration limits — 1KB value limit interpreted as 1024 characters (not bytes).
    // Using string.Length for simplicity. For Latin text chars ≈ bytes; for multi-byte this is more lenient.
    // Do NOT change to Encoding.UTF8.GetByteCount without updating tests and validator.
    internal const int MaxConfigurationKeys = 100;
    internal const int MaxKeyLength = 256;
    internal const int MaxValueLength = 1024;

    private const string GlobalAdminExtensionKey = "actor:globalAdmin";

    public static DomainResult Handle(CreateTenant command, TenantState? state)
    {
        ArgumentNullException.ThrowIfNull(command);
        return state is not null
            ? DomainResult.Rejection([new TenantAlreadyExistsRejection(command.TenantId)])
            : DomainResult.Success([new TenantCreated(command.TenantId, command.Name, command.Description, DateTimeOffset.UtcNow)]);
    }

    public static DomainResult Handle(UpdateTenant command, TenantState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);
        return state switch
        {
            null => DomainResult.Rejection([new TenantNotFoundRejection(command.TenantId)]),
            { Status: TenantStatus.Disabled } => DomainResult.Rejection([new TenantDisabledRejection(command.TenantId)]),
            _ when !IsGlobalAdmin(envelope)
                && !IsAuthorized(state, envelope.UserId, TenantRole.TenantContributor)
                => DomainResult.Rejection([new InsufficientPermissionsRejection(
                    command.TenantId, envelope.UserId,
                    state.Users.TryGetValue(envelope.UserId, out TenantRole role) ? role : null,
                    nameof(UpdateTenant))]),
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

    public static DomainResult Handle(AddUserToTenant command, TenantState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);
        return state switch
        {
            null => DomainResult.Rejection([new TenantNotFoundRejection(command.TenantId)]),
            { Status: TenantStatus.Disabled } => DomainResult.Rejection([new TenantDisabledRejection(command.TenantId)]),
            // RBAC: Owner only (skip if GlobalAdmin OR first user bootstrap on empty tenant)
            _ when !IsGlobalAdmin(envelope)
                && state.HasMembershipHistory
                && !IsAuthorized(state, envelope.UserId, TenantRole.TenantOwner)
                => DomainResult.Rejection([new InsufficientPermissionsRejection(
                    command.TenantId, envelope.UserId,
                    state.Users.TryGetValue(envelope.UserId, out TenantRole addRole) ? addRole : null,
                    nameof(AddUserToTenant))]),
            _ when !Enum.IsDefined(command.Role)
                => DomainResult.Rejection([new RoleEscalationRejection(command.TenantId, command.UserId, command.Role)]),
            _ when state.Users.ContainsKey(command.UserId)
                => DomainResult.Rejection([new UserAlreadyInTenantRejection(command.TenantId, command.UserId, state.Users[command.UserId])]),
            _ => DomainResult.Success([new UserAddedToTenant(command.TenantId, command.UserId, command.Role)]),
        };
    }

    public static DomainResult Handle(RemoveUserFromTenant command, TenantState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);
        return state switch
        {
            null => DomainResult.Rejection([new TenantNotFoundRejection(command.TenantId)]),
            { Status: TenantStatus.Disabled } => DomainResult.Rejection([new TenantDisabledRejection(command.TenantId)]),
            // RBAC: Owner only (skip if GlobalAdmin)
            _ when !IsGlobalAdmin(envelope)
                && !IsAuthorized(state, envelope.UserId, TenantRole.TenantOwner)
                => DomainResult.Rejection([new InsufficientPermissionsRejection(
                    command.TenantId, envelope.UserId,
                    state.Users.TryGetValue(envelope.UserId, out TenantRole removeRole) ? removeRole : null,
                    nameof(RemoveUserFromTenant))]),
            _ when !state.Users.ContainsKey(command.UserId)
                => DomainResult.Rejection([new UserNotInTenantRejection(command.TenantId, command.UserId)]),
            _ => DomainResult.Success([new UserRemovedFromTenant(command.TenantId, command.UserId)]),
        };
    }

    public static DomainResult Handle(SetTenantConfiguration command, TenantState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(command.Key);
        ArgumentNullException.ThrowIfNull(command.Value);
        return state switch
        {
            null => DomainResult.Rejection([new TenantNotFoundRejection(command.TenantId)]),
            { Status: TenantStatus.Disabled } => DomainResult.Rejection([new TenantDisabledRejection(command.TenantId)]),
            // RBAC: TenantOwner only (skip if GlobalAdmin)
            _ when !IsGlobalAdmin(envelope)
                && !IsAuthorized(state, envelope.UserId, TenantRole.TenantOwner)
                => DomainResult.Rejection([new InsufficientPermissionsRejection(
                    command.TenantId, envelope.UserId,
                    state.Users.TryGetValue(envelope.UserId, out TenantRole role) ? role : null,
                    nameof(SetTenantConfiguration))]),
            // Limit: key length (FR23)
            _ when command.Key.Length > MaxKeyLength
                => DomainResult.Rejection([new ConfigurationLimitExceededRejection(
                    command.TenantId, "KeyLength", command.Key.Length, MaxKeyLength)]),
            // Limit: value length (FR23)
            _ when command.Value.Length > MaxValueLength
                => DomainResult.Rejection([new ConfigurationLimitExceededRejection(
                    command.TenantId, "ValueSize", command.Value.Length, MaxValueLength)]),
            // Limit: key count — only when adding a NEW key (FR23)
            _ when !state.Configuration.ContainsKey(command.Key)
                && state.Configuration.Count >= MaxConfigurationKeys
                => DomainResult.Rejection([new ConfigurationLimitExceededRejection(
                    command.TenantId, "KeyCount", state.Configuration.Count, MaxConfigurationKeys)]),
            // Idempotent: same key, same value → NoOp
            _ when state.Configuration.TryGetValue(command.Key, out string? existing)
                && existing == command.Value
                => DomainResult.NoOp(),
            _ => DomainResult.Success([new TenantConfigurationSet(command.TenantId, command.Key, command.Value)]),
        };
    }

    public static DomainResult Handle(RemoveTenantConfiguration command, TenantState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(command.Key);
        return state switch
        {
            null => DomainResult.Rejection([new TenantNotFoundRejection(command.TenantId)]),
            { Status: TenantStatus.Disabled } => DomainResult.Rejection([new TenantDisabledRejection(command.TenantId)]),
            // RBAC: TenantOwner only (skip if GlobalAdmin)
            _ when !IsGlobalAdmin(envelope)
                && !IsAuthorized(state, envelope.UserId, TenantRole.TenantOwner)
                => DomainResult.Rejection([new InsufficientPermissionsRejection(
                    command.TenantId, envelope.UserId,
                    state.Users.TryGetValue(envelope.UserId, out TenantRole role) ? role : null,
                    nameof(RemoveTenantConfiguration))]),
            // Idempotent: key not present → NoOp (desired state already achieved)
            _ when !state.Configuration.ContainsKey(command.Key)
                => DomainResult.NoOp(),
            _ => DomainResult.Success([new TenantConfigurationRemoved(command.TenantId, command.Key)]),
        };
    }

    public static DomainResult Handle(ChangeUserRole command, TenantState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);
        return state switch
        {
            null => DomainResult.Rejection([new TenantNotFoundRejection(command.TenantId)]),
            { Status: TenantStatus.Disabled } => DomainResult.Rejection([new TenantDisabledRejection(command.TenantId)]),
            // RBAC: Owner only (skip if GlobalAdmin) — must precede domain checks so unauthorized users get rejection, not NoOp
            _ when !IsGlobalAdmin(envelope)
                && !IsAuthorized(state, envelope.UserId, TenantRole.TenantOwner)
                => DomainResult.Rejection([new InsufficientPermissionsRejection(
                    command.TenantId, envelope.UserId,
                    state.Users.TryGetValue(envelope.UserId, out TenantRole changeRole) ? changeRole : null,
                    nameof(ChangeUserRole))]),
            _ when !Enum.IsDefined(command.NewRole)
                => DomainResult.Rejection([new RoleEscalationRejection(command.TenantId, command.UserId, command.NewRole)]),
            _ when !state.Users.ContainsKey(command.UserId)
                => DomainResult.Rejection([new UserNotInTenantRejection(command.TenantId, command.UserId)]),
            _ when state.Users[command.UserId] == command.NewRole
                => DomainResult.NoOp(),
            _ => DomainResult.Success([new UserRoleChanged(command.TenantId, command.UserId, state.Users[command.UserId], command.NewRole)]),
        };
    }

    private static bool IsAuthorized(TenantState state, string actorUserId, TenantRole minimumRole)
        => state.Users.TryGetValue(actorUserId, out TenantRole actorRole) && MeetsMinimumRole(actorRole, minimumRole);

    /// <summary>
    /// Checks if the actor's role meets or exceeds the minimum required role.
    /// Uses explicit hierarchy to avoid fragile enum ordinal dependency.
    /// Default deny: unknown roles are rejected. Update this method when adding new TenantRole values.
    /// </summary>
    private static bool MeetsMinimumRole(TenantRole actorRole, TenantRole minimumRole)
        => minimumRole switch
        {
            TenantRole.TenantReader => true,
            TenantRole.TenantContributor => actorRole is TenantRole.TenantContributor or TenantRole.TenantOwner,
            TenantRole.TenantOwner => actorRole is TenantRole.TenantOwner,
            _ => false,
        };

    // SECURITY: "actor:globalAdmin" extension MUST be server-populated only (SEC-4).
    // CommandsController strips client-provided reserved extensions and only repopulates this key from trusted claims.
    private static bool IsGlobalAdmin(CommandEnvelope envelope)
        => envelope.Extensions?.TryGetValue(GlobalAdminExtensionKey, out string? value) == true
           && string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
}
