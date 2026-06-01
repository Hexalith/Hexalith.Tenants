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
    IReadOnlyDictionary<string, string> NarrativePayload) {
    public string Target => ResolveTarget();

    public string Scope => TenantId;

    public string Outcome => EventType;

    private string ResolveTarget() {
        if (NarrativePayload is not null && NarrativePayload.TryGetValue("userId", out string? userId)) {
            return userId;
        }

        return NarrativePayload is not null && NarrativePayload.TryGetValue("key", out string? key) ? key : TenantId;
    }
}
