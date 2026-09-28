namespace Hexalith.Tenants.UI.State.TenantAudit;

/// <summary>Availability of an audit evidence receipt.</summary>
public enum TenantAuditReceiptState
{
    Ready,
    Partial,
    Pending,
    Delayed,
    Loading,
    Error,
    Unavailable,
    MissingSupport,
    Stale,
    Degraded,
    Unauthorized,
    InvalidCursor,
    InvalidReference,
}
