using Hexalith.EventStore.Contracts.Events;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Events.Rejections;

namespace Hexalith.Tenants.Client.Projections;

/// <summary>
/// The pure tenant projection transition shared by the local and the durable checkpointed projections.
/// </summary>
/// <remarks>
/// The transition never mutates its input: it returns a new <see cref="TenantLocalState"/> with the event applied
/// and <see cref="TenantLocalState.LastEvent"/> set to the supplied metadata. Event types that do not affect the
/// projection (for example rejection events persisted in the tenant stream) only advance the metadata, so a
/// contiguous replay of authoritative history stays contiguous.
/// </remarks>
public static class TenantProjectionTransition {
    /// <summary>Gets the tenant event types that change the projected state.</summary>
    public static IReadOnlyList<Type> ProjectedEventTypes { get; } = [
        typeof(TenantCreated),
        typeof(TenantUpdated),
        typeof(TenantDisabled),
        typeof(TenantEnabled),
        typeof(UserAddedToTenant),
        typeof(UserRemovedFromTenant),
        typeof(UserRoleChanged),
        typeof(TenantConfigurationSet),
        typeof(TenantConfigurationRemoved),
    ];

    /// <summary>
    /// Gets the persisted tenant-domain rejection event types, which only advance the checkpoint.
    /// </summary>
    /// <remarks>
    /// Rejections that only the <c>global-administrators</c> aggregate emits are excluded, so they never address the
    /// record of a tenant whose identifier is <c>global-administrators</c>.
    /// </remarks>
    public static IReadOnlyList<Type> RejectionEventTypes { get; } = [
        .. typeof(TenantDisabledRejection).Assembly.GetTypes()
            .Where(static type => type is { IsClass: true, IsAbstract: false }
                && typeof(IRejectionEvent).IsAssignableFrom(type)
                && string.Equals(type.Namespace, typeof(TenantDisabledRejection).Namespace, StringComparison.Ordinal)
                && !IsGlobalAdministratorRejection(type))
            .OrderBy(static type => type.FullName, StringComparer.Ordinal),
    ];

    /// <summary>
    /// Applies one tenant event to a copy of the current state.
    /// </summary>
    /// <param name="current">The current state, or <see langword="null"/> before the first event.</param>
    /// <param name="tenantId">The managed tenant identifier (the tenant aggregate identifier).</param>
    /// <param name="event">The event payload, or <see langword="null"/> for an event that does not affect the projection.</param>
    /// <param name="metadata">The bounded metadata of the applied event.</param>
    /// <returns>The next state.</returns>
    public static TenantLocalState Apply(
        TenantLocalState? current,
        string tenantId,
        IEventPayload? @event,
        TenantProjectionEventMetadata metadata) {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(metadata);

        TenantLocalState next = current?.Clone() ?? new TenantLocalState { TenantId = tenantId };
        switch (@event) {
            case TenantCreated created:
                next.Name = created.Name;
                next.Description = created.Description;
                next.Status = TenantStatus.Active;
                break;
            case TenantUpdated updated:
                next.Name = updated.Name;
                next.Description = updated.Description;
                break;
            case TenantDisabled:
                next.Status = TenantStatus.Disabled;
                break;
            case TenantEnabled:
                next.Status = TenantStatus.Active;
                break;
            case UserAddedToTenant added:
                next.Members[added.UserId] = added.Role;
                break;
            case UserRemovedFromTenant removed:
                _ = next.Members.Remove(removed.UserId);
                break;
            case UserRoleChanged changed:
                next.Members[changed.UserId] = changed.NewRole;
                break;
            case TenantConfigurationSet configurationSet:
                next.Configuration[configurationSet.Key] = configurationSet.Value;
                break;
            case TenantConfigurationRemoved configurationRemoved:
                _ = next.Configuration.Remove(configurationRemoved.Key);
                break;
            default:
                break;
        }

        next.LastEvent = metadata;
        return next;
    }

    private static bool IsGlobalAdministratorRejection(Type type)
        => type == typeof(GlobalAdminAlreadyBootstrappedRejection)
            || type == typeof(GlobalAdministratorAlreadyExistsRejection)
            || type == typeof(GlobalAdministratorNotFoundRejection)
            || type == typeof(LastGlobalAdministratorRejection);
}
