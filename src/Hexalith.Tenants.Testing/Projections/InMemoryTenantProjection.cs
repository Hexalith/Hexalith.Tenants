using Hexalith.EventStore.Contracts.Events;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Server.Projections;

namespace Hexalith.Tenants.Testing.Projections;

/// <summary>
/// In-memory read model container that reuses the same TenantReadModel and GlobalAdministratorReadModel
/// classes from the Server project. Applies events to these read models using the same Apply() methods,
/// maintaining query-testable state without DAPR state store.
/// </summary>
public sealed class InMemoryTenantProjection
{
    private readonly GlobalAdministratorReadModel _globalAdmins = new();
    private readonly Dictionary<string, TenantReadModel> _tenants = new();

    /// <summary>
    /// Applies a single event to the appropriate read model.
    /// </summary>
    /// <param name="eventPayload">The event payload to apply.</param>
    /// <exception cref="ArgumentNullException">Thrown when eventPayload is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when an unknown event type is encountered or when a tenant event is applied before TenantCreated.</exception>
    public void Apply(IEventPayload eventPayload)
    {
        ArgumentNullException.ThrowIfNull(eventPayload);

        switch (eventPayload)
        {
            // Tenant events — route to per-tenant TenantReadModel
            case TenantCreated e:
                if (_tenants.ContainsKey(e.TenantId))
                {
                    throw new InvalidOperationException($"Duplicate TenantCreated for '{e.TenantId}'");
                }
                var model = new TenantReadModel();
                model.Apply(e);
                _tenants[e.TenantId] = model;
                break;
            case TenantUpdated e:
                GetOrThrow(e.TenantId).Apply(e);
                break;
            case TenantDisabled e:
                GetOrThrow(e.TenantId).Apply(e);
                break;
            case TenantEnabled e:
                GetOrThrow(e.TenantId).Apply(e);
                break;
            case UserAddedToTenant e:
                GetOrThrow(e.TenantId).Apply(e);
                break;
            case UserRemovedFromTenant e:
                GetOrThrow(e.TenantId).Apply(e);
                break;
            case UserRoleChanged e:
                GetOrThrow(e.TenantId).Apply(e);
                break;
            case TenantConfigurationSet e:
                GetOrThrow(e.TenantId).Apply(e);
                break;
            case TenantConfigurationRemoved e:
                GetOrThrow(e.TenantId).Apply(e);
                break;

            // Global admin events — route to singleton GlobalAdministratorReadModel
            case GlobalAdministratorSet e:
                _globalAdmins.Apply(e);
                break;
            case GlobalAdministratorRemoved e:
                _globalAdmins.Apply(e);
                break;

            // IRejectionEvent — skip (projections only process success events)
            case IRejectionEvent:
                break;

            default:
                // Silently ignore unknown events to maintain parity with service processing behavior
                break;
        }
    }

    /// <summary>
    /// Applies multiple events in sequence.
    /// </summary>
    /// <param name="events">The events to apply.</param>
    /// <exception cref="ArgumentNullException">Thrown when events is null.</exception>
    public void ApplyEvents(IEnumerable<IEventPayload> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        // Apply continuously, no atomic tracking. Document non-atomic semantics for in-memory model.
        foreach (IEventPayload eventPayload in events)
        {
            Apply(eventPayload);
        }
    }

    /// <summary>
    /// Returns all projected tenants.
    /// </summary>
    /// <returns>A read-only list of all tenant read models.</returns>
    public IReadOnlyList<TenantReadModel> GetAllTenants() => _tenants.Values.ToList();

    /// <summary>
    /// Returns the global administrator read model (never null — initialized empty).
    /// </summary>
    /// <returns>The global administrator read model.</returns>
    public GlobalAdministratorReadModel GetGlobalAdministrators() => _globalAdmins;

    /// <summary>
    /// Returns the read model for a tenant, or null if the tenant is unknown.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <returns>The tenant read model, or null if not found.</returns>
    public TenantReadModel? GetTenant(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        return _tenants.TryGetValue(tenantId, out TenantReadModel? model) ? model : null;
    }

    private TenantReadModel GetOrThrow(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId, nameof(tenantId));
        return _tenants.TryGetValue(tenantId, out TenantReadModel? m)
            ? m
            : throw new InvalidOperationException($"Tenant '{tenantId}' not found in projection. Was TenantCreated applied first?");
    }
}
