using Hexalith.Tenants.Contracts.Enums;

namespace Hexalith.Tenants.Contracts.Events;

/// <summary>A bounded, complete authoritative access snapshot; it changes no tenant business state.</summary>
/// <param name="TenantId">The managed tenant identifier.</param>
/// <param name="Status">The current tenant lifecycle status, including disabled tenants.</param>
/// <param name="Members">The exact current principal-to-role mapping.</param>
/// <param name="ConfigurationKeys">Current configuration-key names, without configuration values.</param>
public sealed record TenantAccessRefreshed(
    string TenantId,
    TenantStatus Status,
    IReadOnlyDictionary<string, TenantRole> Members,
    IReadOnlyList<string> ConfigurationKeys) : IEventPayload;
