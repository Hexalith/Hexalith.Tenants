namespace Hexalith.Tenants.Api.Services;

/// <summary>
/// Forwards the validated external caller bearer to EventStore gateway requests.
/// </summary>
public sealed class InboundBearerForwardingHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        _ = request.Headers.Remove("Authorization");
        if (httpContextAccessor.HttpContext is { } context)
        {
            _ = request.Headers.TryAddWithoutValidation("Authorization", context.Request.Headers.Authorization.ToArray());
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
