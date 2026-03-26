using Hexalith.EventStore.Client.Aggregates;
using Hexalith.EventStore.Client.Attributes;

namespace Hexalith.Tenants.Server.Projections;

/// <summary>
/// Per-tenant read model projection. Auto-discovered by EventStore's assembly scanning.
/// </summary>
[EventStoreDomain("tenants")]
public sealed class TenantProjection : EventStoreProjection<TenantReadModel> {
}
