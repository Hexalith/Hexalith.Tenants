namespace Hexalith.Tenants.Api.Services;

/// <summary>
/// Forwards the validated external caller bearer to EventStore gateway requests.
/// </summary>
public sealed class InboundBearerForwardingHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        string? authorization = httpContextAccessor.HttpContext?.Request.Headers.Authorization;
        if (!string.IsNullOrWhiteSpace(authorization))
        {
            _ = request.Headers.TryAddWithoutValidation("Authorization", authorization);
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
