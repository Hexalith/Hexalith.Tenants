using Hexalith.Tenants.Contracts.Enums;

namespace Hexalith.Tenants.Contracts.Queries;

/// <summary>
/// Tenant audit entry returned by tenant audit queries.
/// </summary>
public sealed record TenantAuditEntry(
    string EventId,
    string EventType,
    AuditEventCategory Category,
    string ActorId,
    DateTimeOffset Timestamp,
    string TenantId,
    IReadOnlyDictionary<string, string> NarrativePayload);
