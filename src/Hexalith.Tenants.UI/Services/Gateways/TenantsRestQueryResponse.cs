using Hexalith.EventStore.Contracts.Queries;

namespace Hexalith.Tenants.UI.Services.Gateways;

/// <summary>
/// Contains one typed Tenants REST read result and its conservatively normalized metadata.
/// </summary>
/// <remarks>
/// Deliberately carries no Freshness member. It previously did, and nothing outside this type ever read it:
/// the rendered stale/current banners come from <c>TenantQueryGateway.ResolveFreshness</c>, a second and
/// stricter implementation that normalizes through <c>ProjectionLifecyclePolicy</c>. Publishing an
/// independently-maintained freshness value here made the client's tests read as if they pinned the
/// rendered freshness. The client still resolves freshness internally, where it IS load-bearing -- as the
/// input to the supported-304 gate.
/// </remarks>
/// <typeparam name="TPayload">Expected response payload contract.</typeparam>
/// <param name="Payload">Typed payload when the read succeeded; otherwise <see langword="null"/>.</param>
/// <param name="Metadata">Conservatively normalized response metadata.</param>
/// <param name="FailureKind">Fixed support-safe failure category.</param>
/// <param name="StatusCode">Effective HTTP status used by downstream gateway mappings.</param>
public sealed record TenantsRestQueryResponse<TPayload>(
    TPayload? Payload,
    QueryResponseMetadata Metadata,
    TenantsRestQueryFailureKind FailureKind,
    int StatusCode)
{
    /// <summary>Gets the normalized strong ETag retained only inside the server-side BFF.</summary>
    internal string? ETag => Metadata.ETag;

    /// <summary>Gets whether the response is a usable payload or supported not-modified result.</summary>
    internal bool IsSuccess => FailureKind == TenantsRestQueryFailureKind.None;

    /// <summary>Gets whether supported projection metadata proves a not-modified result.</summary>
    internal bool IsNotModified => IsSuccess && Metadata.IsNotModified == true;

    /// <summary>Returns a fixed support-safe description that omits payload and metadata values.</summary>
    public override string ToString()
        => $"{nameof(TenantsRestQueryResponse<TPayload>)} {{ IsSuccess = {IsSuccess}, IsNotModified = {IsNotModified}, FailureKind = {FailureKind}, StatusCode = {StatusCode} }}";
}
