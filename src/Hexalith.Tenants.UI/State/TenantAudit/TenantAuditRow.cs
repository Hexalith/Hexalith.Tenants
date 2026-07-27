using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;

namespace Hexalith.Tenants.UI.State.TenantAudit;

/// <summary>Represents support-safe audit evidence rendered by the Tenants UI.</summary>
/// <param name="EventReference">The stable event reference.</param>
/// <param name="EventType">The audit event type.</param>
/// <param name="Category">The audit event category.</param>
/// <param name="ActorId">The support-safe actor identifier.</param>
/// <param name="Timestamp">The event timestamp.</param>
/// <param name="TenantId">The tenant identifier carried by the evidence.</param>
/// <param name="Target">The support-safe affected target.</param>
/// <param name="Scope">The support-safe event scope.</param>
/// <param name="Outcome">The support-safe event outcome.</param>
/// <param name="ReferenceContext">The allow-listed narrative context.</param>
/// <param name="Freshness">The normalized read-model freshness.</param>
/// <param name="Lifecycle">The normalized projection lifecycle.</param>
/// <param name="Provenance">The declared route provenance for the query response.</param>
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
    ReadModelFreshnessState Freshness,
    ProjectionLifecycleState Lifecycle = ProjectionLifecycleState.Unknown,
    QueryResponseProvenance Provenance = QueryResponseProvenance.Unknown) {
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

    /// <summary>Creates a UI audit row from a query-contract entry.</summary>
    /// <param name="entry">The query-contract audit entry.</param>
    /// <param name="freshness">The normalized read-model freshness for the response.</param>
    /// <returns>A support-safe audit row.</returns>
    public static TenantAuditRow FromEntry(TenantAuditEntry entry, ReadModelFreshnessState freshness) {
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
