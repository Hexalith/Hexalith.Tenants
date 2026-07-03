namespace Hexalith.Tenants.Api.Services;

/// <summary>
/// Routes outgoing EventStore requests through the local DAPR sidecar using <c>dapr-app-id</c>.
/// </summary>
/// <param name="appId">The DAPR application id of the invocation target.</param>
/// <param name="apiToken">Optional DAPR API token.</param>
public sealed class DaprAppIdHandler(string appId, string? apiToken) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        _ = request.Headers.TryAddWithoutValidation("dapr-app-id", appId);
        if (!string.IsNullOrEmpty(apiToken))
        {
            _ = request.Headers.TryAddWithoutValidation("dapr-api-token", apiToken);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
