using Hexalith.Tenants.Contracts.Enums;

namespace Hexalith.Tenants.UI.State.TenantAudit;

public sealed record TenantAuditRequest(
    string TenantId,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    AuditEventCategory? Category = null,
    string? Cursor = null,
    int PageSize = 50,
    string? ETag = null);
