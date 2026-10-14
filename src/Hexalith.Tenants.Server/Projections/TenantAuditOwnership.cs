using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Queries;

namespace Hexalith.Tenants.Server.Projections;

/// <summary>Enforces the exact event and tenant policy for separated audit evidence.</summary>
public static class TenantAuditOwnership
{
    private static readonly HashSet<string> s_global = new([nameof(GlobalAdministratorSet), nameof(GlobalAdministratorRemoved)], StringComparer.Ordinal);
    private static readonly HashSet<string> s_access = new([nameof(UserAddedToTenant), nameof(UserRemovedFromTenant), nameof(UserRoleChanged)], StringComparer.Ordinal);
    private static readonly HashSet<string> s_administrative = new([nameof(TenantCreated), nameof(TenantUpdated), nameof(TenantDisabled), nameof(TenantEnabled),
        nameof(TenantConfigurationSet), nameof(TenantConfigurationRemoved)], StringComparer.Ordinal);

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

        if (s_global.Contains(entry.EventType))
        {
            return tenantId == "system" && entry.Category == AuditEventCategory.Access ? TenantAuditStreams.GlobalAdministrators : null;
        }
        if (s_access.Contains(entry.EventType))
        {
            return entry.Category == AuditEventCategory.Access ? TenantAuditStreams.Tenant : null;
        }
        if (s_administrative.Contains(entry.EventType))
        {
            return entry.Category == AuditEventCategory.Administrative ? TenantAuditStreams.Tenant : null;
        }
        return null;
    }
}
