namespace Hexalith.Tenants.UI.Services.Gateways;

/// <summary>
/// Identifies fixed support-safe Tenants REST query failure categories.
/// </summary>
public enum TenantsRestQueryFailureKind
{
    /// <summary>No failure occurred.</summary>
    None = 0,

    /// <summary>The caller is not authenticated.</summary>
    Unauthorized,

    /// <summary>The caller is not authorized for the requested read.</summary>
    Forbidden,

    /// <summary>The requested resource was not found.</summary>
    NotFound,

    /// <summary>The request or its protected cursor was invalid.</summary>
    InvalidRequest,

    /// <summary>The response metadata could not support the claimed result.</summary>
    InvalidMetadata,

    /// <summary>The response payload did not match the expected contract.</summary>
    InvalidPayload,

    /// <summary>The Tenants read service was unavailable.</summary>
    Unavailable,

    /// <summary>The Tenants read service did not respond before the transport timeout.</summary>
    Timeout,
}
