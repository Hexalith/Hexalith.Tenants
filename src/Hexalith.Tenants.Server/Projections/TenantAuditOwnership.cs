using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Queries;

namespace Hexalith.Tenants.Server.Projections;

/// <summary>Enforces the exact event and tenant policy for separated audit evidence.</summary>
public static class TenantAuditOwnership
{
    /// <summary>Checks the optional stream selector and its tenant scope.</summary>
    public static bool IsValidSelector(string? stream, string tenantId)
        => stream is null or TenantAuditStreams.Tenant || stream == TenantAuditStreams.GlobalAdministrators && tenantId == "system";

    /// <summary>Classifies only exact known event names and the requested tenant identity.</summary>
    public static string? GetOwner(TenantAuditEntry? entry, string tenantId)
    {
        if (entry is null || string.IsNullOrWhiteSpace(entry.EventId) || string.IsNullOrWhiteSpace(entry.ActorId)
            || entry.NarrativePayload is null
            || !string.Equals(entry.TenantId, tenantId, StringComparison.Ordinal))
        {
            return null;
        }

        return entry.EventType switch
        {
            nameof(GlobalAdministratorSet) or nameof(GlobalAdministratorRemoved) when tenantId == "system" && entry.Category == AuditEventCategory.Access => TenantAuditStreams.GlobalAdministrators,
            nameof(UserAddedToTenant) or nameof(UserRemovedFromTenant) or nameof(UserRoleChanged)
                when entry.Category == AuditEventCategory.Access => TenantAuditStreams.Tenant,
            nameof(TenantCreated) or nameof(TenantUpdated) or nameof(TenantDisabled) or nameof(TenantEnabled)
                or nameof(TenantConfigurationSet) or nameof(TenantConfigurationRemoved)
                when entry.Category == AuditEventCategory.Administrative => TenantAuditStreams.Tenant,
            _ => null,
        };
    }
}
