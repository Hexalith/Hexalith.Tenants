using Hexalith.EventStore.Client.Aggregates;

namespace Hexalith.Tenants.Server.Projections;

/// <summary>
/// Cross-tenant index projection. Aggregates data from all tenant aggregates.
/// Auto-discovered by EventStore's assembly scanning.
/// Domain name resolves to "tenant-index" via convention (distinct from per-tenant "tenants" projection).
/// </summary>
public sealed class TenantIndexProjection : EventStoreProjection<TenantIndexReadModel> {
}
