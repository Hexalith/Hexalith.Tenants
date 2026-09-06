using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.UI.Services.SupportSafety;

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
/// <param name="Narrative">The typed support-safe narrative used by correction behavior.</param>
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
    QueryResponseProvenance Provenance = QueryResponseProvenance.Unknown,
    TenantAuditNarrative? Narrative = null) {

    /// <summary>Creates a UI audit row from a query-contract entry.</summary>
    /// <param name="entry">The query-contract audit entry.</param>
    /// <param name="freshness">The normalized read-model freshness for the response.</param>
    /// <returns>A support-safe audit row.</returns>
    public static TenantAuditRow FromEntry(TenantAuditEntry entry, ReadModelFreshnessState freshness) {
        ArgumentNullException.ThrowIfNull(entry);

        TenantAuditNarrative narrative = TenantAuditNarrative.FromPayload(entry.NarrativePayload);
        string tenantId = TenantAuditSupportSafety.SafeIdentifier(entry.TenantId, SupportSafeCopyValueKind.TenantId);
        string target = narrative.UserId
            ?? narrative.ConfigurationKey
            ?? tenantId;

        return new(
            TenantAuditSupportSafety.SafeApprovedReference(entry.EventId) ?? string.Empty,
            entry.EventType,
            entry.Category,
            TenantAuditSupportSafety.SafeIdentifier(entry.ActorId, SupportSafeCopyValueKind.UserId),
            entry.Timestamp,
            tenantId,
            target,
            tenantId,
            entry.EventType,
            narrative.ToDisplayString(),
            freshness,
            Narrative: narrative);
    }
}
