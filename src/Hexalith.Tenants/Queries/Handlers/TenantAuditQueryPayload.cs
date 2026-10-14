using Hexalith.Tenants.Contracts.Enums;

namespace Hexalith.Tenants.Queries.Handlers;

/// <summary>Common audit query fields parsed from the authenticated request payload.</summary>
/// <param name="From">The inclusive lower timestamp filter.</param>
/// <param name="To">The inclusive upper timestamp filter.</param>
/// <param name="Category">The optional audit category filter.</param>
/// <param name="Cursor">The protected continuation cursor.</param>
/// <param name="PageSize">The clamped requested page size.</param>
/// <param name="Stream">The optional audit owner selector.</param>
/// <param name="ErrorMessage">The safe validation failure, if parsing failed.</param>
internal sealed record TenantAuditQueryPayload(
    DateTimeOffset? From,
    DateTimeOffset? To,
    AuditEventCategory? Category,
    string? Cursor,
    int PageSize,
    string? Stream,
    string? ErrorMessage);
