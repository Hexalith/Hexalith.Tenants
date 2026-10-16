using System.Net;

namespace Hexalith.Tenants.UI.Tests.Components;

/// <summary>Supplies real gateway status JSON to support-safety boundary tests.</summary>
/// <param name="response">The backend status response.</param>
/// <param name="statusCode">The backend HTTP status.</param>
internal sealed class SupportSafetyCommandStatusHandler(string response, HttpStatusCode statusCode = HttpStatusCode.OK) : HttpMessageHandler
{
    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(response),
        });
}
