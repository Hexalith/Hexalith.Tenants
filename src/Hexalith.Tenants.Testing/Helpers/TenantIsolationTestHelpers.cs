using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Testing.Fakes;

namespace Hexalith.Tenants.Testing.Helpers;

/// <summary>
/// Consumer-facing tenant isolation helpers for fast, infrastructure-free projection and access tests.
/// </summary>
public static class TenantIsolationTestHelpers {
    private const string DefaultGlobalAdminUserId = "global-admin";

    /// <summary>
    /// Creates a fresh in-memory tenant service and seeds the requested tenants, roles, configuration, and lifecycle state.
    /// </summary>
    /// <param name="tenantRoles">Tenant IDs mapped to user role maps.</param>
    /// <param name="tenantConfiguration">Optional tenant IDs mapped to configuration key/value maps.</param>
    /// <param name="disabledTenantIds">Optional tenant IDs to disable after membership and configuration are seeded.</param>
    /// <param name="globalAdminUserId">The trusted global administrator actor used for setup commands.</param>
    /// <returns>A fresh <see cref="InMemoryTenantService"/> containing only the seeded scenario.</returns>
    public static InMemoryTenantService CreateServiceWithTenants(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, TenantRole>> tenantRoles,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>? tenantConfiguration = null,
        IReadOnlySet<string>? disabledTenantIds = null,
        string globalAdminUserId = DefaultGlobalAdminUserId) {
        var service = new InMemoryTenantService();
        _ = SeedTenants(service, tenantRoles, tenantConfiguration, disabledTenantIds, globalAdminUserId);
        return service;
    }

    /// <summary>
    /// Seeds tenants into an explicitly supplied in-memory service using tenant commands.
    /// </summary>
    /// <param name="service">The in-memory tenant service to mutate.</param>
    /// <param name="tenantRoles">Tenant IDs mapped to user role maps.</param>
    /// <param name="tenantConfiguration">Optional tenant IDs mapped to configuration key/value maps.</param>
    /// <param name="disabledTenantIds">Optional tenant IDs to disable after membership and configuration are seeded.</param>
    /// <param name="globalAdminUserId">The trusted global administrator actor used for setup commands.</param>
    /// <returns>Every <see cref="DomainResult"/> produced by the setup commands, in command order.</returns>
    public static IReadOnlyList<DomainResult> SeedTenants(
        InMemoryTenantService service,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, TenantRole>> tenantRoles,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>? tenantConfiguration = null,
        IReadOnlySet<string>? disabledTenantIds = null,
        string globalAdminUserId = DefaultGlobalAdminUserId) {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(tenantRoles);
        ArgumentException.ThrowIfNullOrWhiteSpace(globalAdminUserId);

        var results = new List<DomainResult>();

        foreach (KeyValuePair<string, IReadOnlyDictionary<string, TenantRole>> tenant in tenantRoles) {
            string tenantId = tenant.Key;
            ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

            results.Add(TenantTestHelpers.CreateTenant(service, tenantId, tenantId));

            foreach (KeyValuePair<string, TenantRole> membership in tenant.Value) {
                results.Add(AddUser(service, tenantId, membership.Key, membership.Value, globalAdminUserId));
            }

            if (tenantConfiguration?.TryGetValue(tenantId, out IReadOnlyDictionary<string, string>? configuration) == true) {
                foreach (KeyValuePair<string, string> entry in configuration) {
                    results.Add(SetConfiguration(service, tenantId, entry.Key, entry.Value, globalAdminUserId));
                }
            }

            if (disabledTenantIds?.Contains(tenantId) == true) {
                results.Add(DisableTenant(service, tenantId, globalAdminUserId));
            }
        }

        return results;
    }

    /// <summary>
    /// Adds a user to a tenant through the in-memory service command path.
    /// </summary>
    public static DomainResult AddUser(
        InMemoryTenantService service,
        string tenantId,
        string userId,
        TenantRole role,
        string actorUserId = DefaultGlobalAdminUserId) {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorUserId);

        return service.ProcessCommand(
            new AddUserToTenant(tenantId, userId, role),
            userId: actorUserId,
            isGlobalAdmin: true);
    }

    /// <summary>
    /// Removes a user from a tenant through the in-memory service command path.
    /// </summary>
    public static DomainResult RemoveUser(
        InMemoryTenantService service,
        string tenantId,
        string userId,
        string actorUserId = DefaultGlobalAdminUserId) {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorUserId);

        return service.ProcessCommand(
            new RemoveUserFromTenant(tenantId, userId),
            userId: actorUserId,
            isGlobalAdmin: true);
    }

    /// <summary>
    /// Disables a tenant through the in-memory service command path.
    /// </summary>
    public static DomainResult DisableTenant(
        InMemoryTenantService service,
        string tenantId,
        string actorUserId = DefaultGlobalAdminUserId) {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorUserId);

        return service.ProcessCommand(new DisableTenant(tenantId), userId: actorUserId, isGlobalAdmin: true);
    }

    /// <summary>
    /// Enables a tenant through the in-memory service command path.
    /// </summary>
    public static DomainResult EnableTenant(
        InMemoryTenantService service,
        string tenantId,
        string actorUserId = DefaultGlobalAdminUserId) {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorUserId);

        return service.ProcessCommand(new EnableTenant(tenantId), userId: actorUserId, isGlobalAdmin: true);
    }

    /// <summary>
    /// Sets a tenant configuration value through the in-memory service command path.
    /// </summary>
    public static DomainResult SetConfiguration(
        InMemoryTenantService service,
        string tenantId,
        string key,
        string value,
        string actorUserId = DefaultGlobalAdminUserId) {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorUserId);

        return service.ProcessCommand(
            new SetTenantConfiguration(tenantId, key, value),
            userId: actorUserId,
            isGlobalAdmin: true);
    }

    /// <summary>
    /// Gets the successful tenant events from a service history for one tenant.
    /// </summary>
    public static IReadOnlyList<IEventPayload> GetTenantEvents(InMemoryTenantService service, string tenantId) {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        return SelectTenantEvents(service.EventHistory, tenantId);
    }

    /// <summary>
    /// Selects successful tenant-scoped events by the payload TenantId value.
    /// </summary>
    public static IReadOnlyList<IEventPayload> SelectTenantEvents(IEnumerable<IEventPayload> events, string tenantId) {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        return events
            .Where(e => string.Equals(GetTenantId(e), tenantId, StringComparison.Ordinal))
            .ToList();
    }

    /// <summary>
    /// Returns the tenant ID carried by a tenant-scoped event payload, or null for non-tenant events.
    /// </summary>
    public static string? GetTenantId(IEventPayload eventPayload) {
        ArgumentNullException.ThrowIfNull(eventPayload);

        return eventPayload switch {
            TenantCreated e => e.TenantId,
            TenantUpdated e => e.TenantId,
            TenantDisabled e => e.TenantId,
            TenantEnabled e => e.TenantId,
            UserAddedToTenant e => e.TenantId,
            UserRemovedFromTenant e => e.TenantId,
            UserRoleChanged e => e.TenantId,
            TenantConfigurationSet e => e.TenantId,
            TenantConfigurationRemoved e => e.TenantId,
            _ => null,
        };
    }

    /// <summary>
    /// Repeats the selected event sequence to simulate duplicate delivery.
    /// </summary>
    public static IReadOnlyList<IEventPayload> DuplicateDelivery(IEnumerable<IEventPayload> events, int deliveryCount = 2) {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentOutOfRangeException.ThrowIfLessThan(deliveryCount, 2);

        IEventPayload[] selectedEvents = events.ToArray();
        var duplicated = new List<IEventPayload>(selectedEvents.Length * deliveryCount);

        for (int i = 0; i < deliveryCount; i++) {
            duplicated.AddRange(selectedEvents);
        }

        return duplicated;
    }

    /// <summary>
    /// Gets the latest known role for a user in each tenant represented by the service event history.
    /// </summary>
    public static IReadOnlyDictionary<string, TenantRole> GetTenantRolesForUser(InMemoryTenantService service, string userId) {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var roles = new Dictionary<string, TenantRole>(StringComparer.Ordinal);

        foreach (IEventPayload eventPayload in service.EventHistory) {
            switch (eventPayload) {
                case UserAddedToTenant e when string.Equals(e.UserId, userId, StringComparison.Ordinal):
                    roles[e.TenantId] = e.Role;
                    break;
                case UserRoleChanged e when string.Equals(e.UserId, userId, StringComparison.Ordinal):
                    roles[e.TenantId] = e.NewRole;
                    break;
                case UserRemovedFromTenant e when string.Equals(e.UserId, userId, StringComparison.Ordinal):
                    _ = roles.Remove(e.TenantId);
                    break;
                default:
                    break;
            }
        }

        return roles;
    }

    /// <summary>
    /// Checks whether a user has a role meeting the requested tenant-scoped minimum role.
    /// </summary>
    public static bool IsAuthorizedForTenant(
        InMemoryTenantService service,
        string tenantId,
        string userId,
        TenantRole minimumRole) {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return service.GetTenantState(tenantId) is { Status: TenantStatus.Active } state
               && state.Users.TryGetValue(userId, out TenantRole role)
               && MeetsMinimumRole(role, minimumRole);
    }

    private static bool MeetsMinimumRole(TenantRole role, TenantRole minimumRole)
        => minimumRole switch {
            TenantRole.TenantReader => role is TenantRole.TenantReader or TenantRole.TenantContributor or TenantRole.TenantOwner,
            TenantRole.TenantContributor => role is TenantRole.TenantContributor or TenantRole.TenantOwner,
            TenantRole.TenantOwner => role is TenantRole.TenantOwner,
            _ => false,
        };
}
