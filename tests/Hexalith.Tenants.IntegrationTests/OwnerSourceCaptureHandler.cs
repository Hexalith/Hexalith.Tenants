using System.Net;
using System.Net.Http.Json;

using Hexalith.EventStore.Contracts.Streams;

namespace Hexalith.Tenants.IntegrationTests;

/// <summary>Captures the owner-source HTTP boundary without a sidecar or network listener.</summary>
internal sealed class OwnerSourceCaptureHandler : HttpMessageHandler
{
    internal HttpMethod? Method { get; private set; }
    internal Uri? Uri { get; private set; }
    internal Dictionary<string, string[]> Headers { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    internal string Body { get; private set; } = string.Empty;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Method = request.Method;
        Uri = request.RequestUri;
        Headers = request.Headers.ToDictionary(static header => header.Key, static header => header.Value.ToArray(), StringComparer.OrdinalIgnoreCase);
        Body = await request.Content!.ReadAsStringAsync(cancellationToken);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new StreamReadPage("system", "tenants", "owner-source", [], new StreamReadMetadata(7, 12, null, 12, 0, false, null))),
        };
    }
}
