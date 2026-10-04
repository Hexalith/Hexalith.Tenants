using System.Net;
using System.Net.Http.Json;

namespace Hexalith.Tenants.UI.Tests.Components;

/// <summary>Returns tenant configuration status for a tenant sharing the platform aggregate name.</summary>
internal sealed class TenantConfigurationStatusHandler : HttpMessageHandler
{
    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
        => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new
            {
                correlationId = "correlation-configuration",
                status = "Completed",
                statusCode = 4,
                tenantId = "system",
                domain = "tenants",
                aggregateId = "global-administrators",
                eventCount = 1,
                messageId = Uri.UnescapeDataString(request.RequestUri!.Segments[^1]),
            }),
        });
}
