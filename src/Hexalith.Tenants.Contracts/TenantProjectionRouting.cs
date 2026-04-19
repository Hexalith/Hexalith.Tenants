namespace Hexalith.Tenants.Contracts;

/// <summary>
/// Routing constants for the tenant projection actor. Shared between hosts that register the
/// actor (Tenants service) and callers that target it via the EventStore query pipeline.
/// </summary>
public static class TenantProjectionRouting {
    /// <summary>
    /// DAPR actor type name hosting the tenant projection (distinct from EventStore's generic
    /// "ProjectionActor" to avoid placement collisions when both services are deployed together).
    /// </summary>
    public const string ActorTypeName = "TenantsProjectionActor";
}
