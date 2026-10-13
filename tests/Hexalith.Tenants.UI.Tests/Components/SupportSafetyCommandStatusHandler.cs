using System.Net;

namespace Hexalith.Tenants.UI.Tests.Components;

/// <summary>Supplies real gateway status JSON to support-safety boundary tests.</summary>
/// <param name="response">The backend status response.</param>
internal sealed class SupportSafetyCommandStatusHandler(string response) : HttpMessageHandler
{
    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(response),
        });
}
