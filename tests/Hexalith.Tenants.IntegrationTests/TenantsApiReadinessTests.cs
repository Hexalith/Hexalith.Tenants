extern alias TenantsApi;

using System.Net;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

/// <summary>HTTP readiness coverage for the public API's EventStore gateway dependency.</summary>
public sealed class TenantsApiReadinessTests
{
    [Theory]
    [InlineData(HttpStatusCode.OK, HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.ServiceUnavailable, HttpStatusCode.ServiceUnavailable)]
    public async Task Ready_FollowsGatewayThroughConfiguredDaprEndpoint_AliveRemainsIndependent(
        HttpStatusCode gatewayStatus,
        HttpStatusCode expectedReady)
    {
        var handler = new CaptureHandler(gatewayStatus);
        using WebApplicationFactory<TenantsApi::Program> factory = new WebApplicationFactory<TenantsApi::Program>()
            .WithWebHostBuilder(builder =>
            {
                _ = builder.UseSetting("EventStore:Authentication:SigningKey", "readiness-test-signing-key-at-least-32-bytes");
                _ = builder.UseSetting("DAPR_HTTP_ENDPOINT", "http://localhost:4367");
                builder.ConfigureServices(services =>
                    services.AddHttpClient(TenantsApi::Hexalith.Tenants.Api.Services.EventStoreGatewayReadinessHealthCheck.ClientName)
                        .ConfigurePrimaryHttpMessageHandler(() => handler));
            });
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage ready = await client.GetAsync("/ready", TestContext.Current.CancellationToken);
        using HttpResponseMessage alive = await client.GetAsync("/alive", TestContext.Current.CancellationToken);

        ready.StatusCode.ShouldBe(expectedReady);
        alive.StatusCode.ShouldBe(HttpStatusCode.OK);
        handler.RequestUri.ShouldBe(new Uri("http://localhost:4367/ready"));
        handler.DaprAppId.ShouldBe("eventstore");
    }

    private sealed class CaptureHandler(HttpStatusCode status) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        public string? DaprAppId { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            DaprAppId = request.Headers.TryGetValues("dapr-app-id", out IEnumerable<string>? values)
                ? values.Single()
                : null;
            return Task.FromResult(new HttpResponseMessage(status));
        }
    }
}
