using Hexalith.EventStore.Client.Aggregates;
using Hexalith.EventStore.Client.Attributes;

namespace Hexalith.Tenants.Server.Projections;

/// <summary>
/// Global administrator read model projection. Auto-discovered by EventStore's assembly scanning.
/// </summary>
[EventStoreDomain("global-administrators")]
public sealed class GlobalAdministratorProjection : EventStoreProjection<GlobalAdministratorReadModel>
{
}
