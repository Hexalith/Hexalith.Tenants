using System.Text.Json.Serialization;

namespace Hexalith.Tenants.UI.Services.Gateways;

/// <summary>
/// Identifies fixed support-safe Tenants REST query failure categories.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<TenantsRestQueryFailureKind>))]
public enum TenantsRestQueryFailureKind
{
    /// <summary>The value is absent or was not recognized; callers must fail closed.</summary>
    Unknown = 0,

    /// <summary>No failure occurred.</summary>
    None,

    /// <summary>The caller is not authenticated.</summary>
    Unauthorized,

    /// <summary>The caller is not authorized for the requested read.</summary>
    Forbidden,

    /// <summary>The requested resource was not found.</summary>
    NotFound,

    /// <summary>The request was invalid for a reason the service did not further identify.</summary>
    InvalidRequest,

    /// <summary>
    /// The service explicitly identified the supplied protected cursor as invalid.
    /// </summary>
    /// <remarks>
    /// Distinct from <see cref="InvalidRequest"/> on purpose. Page-one recovery may only follow an explicit
    /// contract signal — the <c>reason</c> Problem Details extension carrying the shared
    /// <c>invalid-cursor</c> sentinel — never an undifferentiated HTTP 400, which could equally mean a
    /// malformed page size or an unsupported filter and must not silently restart paging.
    /// </remarks>
    InvalidCursor,

    /// <summary>
    /// This client refused to build a route from the supplied identity; no request was sent.
    /// </summary>
    /// <remarks>
    /// Distinct from <see cref="InvalidRequest"/>, which is a rejection the service issued. A string this
    /// client cannot escape into a route segment can never name a resource, so the detail read may safely
    /// report it as not-found; a 400 the server chose to return means only that the request was refused, and
    /// reporting that as non-existence tells the operator a tenant that exists does not.
    /// </remarks>
    UnsupportedRouteIdentifier,

    /// <summary>The response metadata could not support the claimed result.</summary>
    InvalidMetadata,

    /// <summary>The response payload did not match the expected contract.</summary>
    InvalidPayload,

    /// <summary>The Tenants read service was unavailable.</summary>
    Unavailable,

    /// <summary>The Tenants read service did not respond before the transport timeout.</summary>
    Timeout,
}
