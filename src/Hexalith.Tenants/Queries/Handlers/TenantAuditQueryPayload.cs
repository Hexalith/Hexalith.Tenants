using Hexalith.Tenants.Contracts.Enums;

namespace Hexalith.Tenants.Queries.Handlers;

/// <summary>Common audit query fields parsed from the authenticated request payload.</summary>
internal sealed record TenantAuditQueryPayload(
    DateTimeOffset? From,
    DateTimeOffset? To,
    AuditEventCategory? Category,
    string? Cursor,
    int PageSize,
    string? Stream,
    string? ErrorMessage);
