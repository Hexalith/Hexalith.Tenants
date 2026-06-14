using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.State.TruthState;

namespace Hexalith.Tenants.UI.State.TenantAudit;

public sealed record TenantAuditRow(
    string EventReference,
    string EventType,
    AuditEventCategory Category,
    string ActorId,
    DateTimeOffset Timestamp,
    string TenantId,
    string Target,
    string Scope,
    string Outcome,
    string ReferenceContext,
    TenantFreshnessState Freshness) {
    private static readonly string[] ApprovedNarrativeKeys =
    [
        "userId",
        "key",
        "role",
        "oldRole",
        "newRole",
        "previousRole",
        "timestamp",
        "occurredAt",
    ];

    public static TenantAuditRow FromEntry(TenantAuditEntry entry, TenantFreshnessState freshness) {
        ArgumentNullException.ThrowIfNull(entry);

        return new(
            entry.EventId,
            entry.EventType,
            entry.Category,
            entry.ActorId,
            entry.Timestamp,
            entry.TenantId,
            SafeValue(entry.Target),
            SafeValue(entry.Scope),
            SafeValue(entry.Outcome),
            BuildReferenceContext(entry.NarrativePayload),
            freshness);
    }

    private static string BuildReferenceContext(IReadOnlyDictionary<string, string>? narrative) {
        if (narrative is null || narrative.Count == 0) {
            return string.Empty;
        }

        string[] references = ApprovedNarrativeKeys
            .Where(key => narrative.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value) && IsSafeValue(value))
            .Select(key => $"{key}: {narrative[key]}")
            .ToArray();

        return string.Join("; ", references);
    }

    private static string SafeValue(string? value)
        => IsSafeValue(value) ? value! : string.Empty;

    private static bool IsSafeValue(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return false;
        }

        string candidate = value.Trim();
        return !ContainsUnsafeText(candidate);
    }

    private static bool ContainsUnsafeText(string value)
        => value.Contains("bearer ", StringComparison.OrdinalIgnoreCase)
        || value.Contains("access_token", StringComparison.OrdinalIgnoreCase)
        || value.Contains("authorization", StringComparison.OrdinalIgnoreCase)
        || value.Contains("stack trace", StringComparison.OrdinalIgnoreCase)
        || value.Contains("correlation", StringComparison.OrdinalIgnoreCase)
        || value.Contains("etag", StringComparison.OrdinalIgnoreCase)
        || value.Contains("cursor", StringComparison.OrdinalIgnoreCase)
        || value.Contains("raw payload", StringComparison.OrdinalIgnoreCase)
        || value.Contains("eventstore metadata", StringComparison.OrdinalIgnoreCase);
}
