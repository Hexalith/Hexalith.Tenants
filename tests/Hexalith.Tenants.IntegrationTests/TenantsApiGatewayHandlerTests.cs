extern alias TenantsApi;

using System.Net;
using System.Text;
using System.Text.Json;

using Hexalith.Commons.UniqueIds;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Client.Registration;
using Hexalith.EventStore.Contracts.Commands;
using TenantsDaprHttpEndpointResolver = TenantsApi::Hexalith.Tenants.Api.Services.DaprHttpEndpointResolver;
using TenantsInboundBearerForwardingHandler = TenantsApi::Hexalith.Tenants.Api.Services.InboundBearerForwardingHandler;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

public sealed class TenantsApiGatewayHandlerTests
{
    [Fact]
    public void DaprHttpEndpointResolver_WhenNoSidecarConfigurationExists_ReturnsDefaultLocalEndpoint()
    {
        string endpoint = TenantsDaprHttpEndpointResolver.Resolve(Configuration());

        endpoint.ShouldBe("http://localhost:3500");
    }

    [Theory]
    [InlineData("", "", "http://localhost:3500")]
    [InlineData("   ", "   ", "http://localhost:3500")]
    [InlineData("", " 03600 ", "http://localhost:3600")]
    [InlineData("   ", "03600", "http://localhost:3600")]
    public void DaprHttpEndpointResolver_WhenEndpointOrPortIsBlank_UsesTheDocumentedFallback(
        string endpointValue,
        string portValue,
        string expected)
    {
        string endpoint = TenantsDaprHttpEndpointResolver.Resolve(Configuration(
            ("DAPR_HTTP_ENDPOINT", endpointValue),
            ("DAPR_HTTP_PORT", portValue)));

        endpoint.ShouldBe(expected);
    }

    [Fact]
    public void DaprHttpEndpointResolver_WhenEndpointOriginExists_ReturnsNormalizedOrigin()
    {
        string endpoint = TenantsDaprHttpEndpointResolver.Resolve(Configuration(
            ("DAPR_HTTP_ENDPOINT", " https://LOCALHOST:3600/ ")));

        endpoint.ShouldBe("https://localhost:3600");
    }

    [Fact]
    public void DaprHttpEndpointResolver_WhenEndpointContainsPathQueryFragmentUserInfoOrInvalidPort_ThrowsConfigurationError()
    {
        string[] invalidEndpoints =
        [
            "http://localhost:3500/v1.0",
            "http://localhost:3500?x=1",
            "http://localhost:3500#sidecar",
            "http://user:password@localhost:3500",
            "http://localhost:0",
            "ftp://localhost:3500",
            "localhost:3500",
        ];

        foreach (string invalidEndpoint in invalidEndpoints)
        {
            InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
                TenantsDaprHttpEndpointResolver.Resolve(Configuration(("DAPR_HTTP_ENDPOINT", invalidEndpoint))));
            exception.Message.ShouldBe("DAPR_HTTP_ENDPOINT must be an absolute HTTP or HTTPS origin URI.");
        }
    }

    [Fact]
    public void DaprHttpEndpointResolver_WhenPortExists_ReturnsNormalizedLocalEndpoint()
    {
        string endpoint = TenantsDaprHttpEndpointResolver.Resolve(Configuration(
            ("DAPR_HTTP_PORT", " 03500 ")));

        endpoint.ShouldBe("http://localhost:3500");
    }

    [Fact]
    public void DaprHttpEndpointResolver_WhenPortIsMalformedOrOutOfRange_ThrowsConfigurationError()
    {
        string[] invalidPorts =
        [
            "0",
            "65536",
            "+3500",
            "35OO",
        ];

        foreach (string invalidPort in invalidPorts)
        {
            InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
                TenantsDaprHttpEndpointResolver.Resolve(Configuration(("DAPR_HTTP_PORT", invalidPort))));
            exception.Message.ShouldBe("DAPR_HTTP_PORT must be a TCP port number between 1 and 65535.");
        }
    }

    [Fact]
    public async Task InboundBearerForwardingHandler_WhenAuthorizationHeaderExists_ForwardsBearer()
    {
        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext(),
        };
        accessor.HttpContext.Request.Headers.Authorization = "Bearer tenant-token";
        var terminal = new CaptureHandler();
        using var handler = new TenantsInboundBearerForwardingHandler(accessor)
        {
            InnerHandler = terminal,
        };
        using var invoker = new HttpMessageInvoker(handler);

        using HttpResponseMessage response = await invoker.SendAsync(
            new HttpRequestMessage(HttpMethod.Post, "http://eventstore/api/v1/commands"),
            TestContext.Current.CancellationToken);

        HttpRequestMessage request = terminal.Request.ShouldNotBeNull();
        request.Headers.Authorization.ShouldNotBeNull();
        request.Headers.Authorization.Scheme.ShouldBe("Bearer");
        request.Headers.Authorization.Parameter.ShouldBe("tenant-token");
    }

    [Fact]
    public async Task DaprServiceInvocationExtension_ReplacesUntrustedRoutingHeaders()
    {
        var terminal = new CaptureHandler();
        var services = new ServiceCollection();
        _ = services.AddHttpClient("dapr", client =>
        {
            client.BaseAddress = new Uri("http://localhost:3500");
            _ = client.DefaultRequestHeaders.TryAddWithoutValidation("dapr-app-id", "untrusted-app");
            _ = client.DefaultRequestHeaders.TryAddWithoutValidation("dapr-api-token", "untrusted-token");
        })
            .AddEventStoreDaprServiceInvocation("eventstore", "secret-token")
            .ConfigurePrimaryHttpMessageHandler(() => terminal);

        using ServiceProvider provider = services.BuildServiceProvider();
        IHttpClientFactory factory = provider.GetRequiredService<IHttpClientFactory>();
        using HttpClient client = factory.CreateClient("dapr");

        using HttpResponseMessage response = await client.PostAsync(
            "/api/v1/queries",
            content: null,
            cancellationToken: TestContext.Current.CancellationToken);

        HttpRequestMessage request = terminal.Request.ShouldNotBeNull();
        request.Headers.GetValues("dapr-app-id").ShouldBe(["eventstore"]);
        request.Headers.GetValues("dapr-api-token").ShouldBe(["secret-token"]);
    }

    [Fact]
    public async Task GatewayClient_WhenRegisteredLikeTenantsApi_UsesSidecarBaseAddressAndHandlers()
    {
        string statusId = UniqueIdHelper.GenerateSortableUniqueStringId();
        var terminal = new CaptureHandler(new HttpResponseMessage(HttpStatusCode.Accepted)
        {
            Content = new StringContent(
                $"{{\"correlationId\":\"{statusId}\"}}",
                Encoding.UTF8,
                "application/json"),
        });
        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext(),
        };
        accessor.HttpContext.Request.Headers.Authorization = "Bearer tenant-token";

        var services = new ServiceCollection();
        _ = services.AddSingleton<IHttpContextAccessor>(accessor);
        _ = services.AddTransient<TenantsInboundBearerForwardingHandler>();
        _ = services.AddEventStoreGatewayClient(options => options.BaseAddress = new Uri("http://localhost:3500"))
            .ConfigureHttpClient(client =>
            {
                _ = client.DefaultRequestHeaders.TryAddWithoutValidation("dapr-app-id", "untrusted-app");
                _ = client.DefaultRequestHeaders.TryAddWithoutValidation("dapr-api-token", "untrusted-token");
            })
            .AddHttpMessageHandler<TenantsInboundBearerForwardingHandler>()
            .AddEventStoreDaprServiceInvocation("eventstore", "secret-token")
            .ConfigurePrimaryHttpMessageHandler(() => terminal);

        using ServiceProvider provider = services.BuildServiceProvider();
        IEventStoreGatewayClient gateway = provider.GetRequiredService<IEventStoreGatewayClient>();

        SubmitCommandResponse response = await gateway.SubmitCommandAsync(
            new SubmitCommandRequest(
                UniqueIdHelper.GenerateSortableUniqueStringId(),
                "system",
                "tenants",
                "tenant.alpha",
                "enable-tenant",
                JsonSerializer.SerializeToElement(new { tenantId = "tenant.alpha" })),
            TestContext.Current.CancellationToken);

        response.CorrelationId.ShouldBe(statusId);
        HttpRequestMessage request = terminal.Request.ShouldNotBeNull();
        request.RequestUri.ShouldBe(new Uri("http://localhost:3500/api/v1/commands"));
        request.Headers.Authorization.ShouldNotBeNull();
        request.Headers.Authorization.Scheme.ShouldBe("Bearer");
        request.Headers.Authorization.Parameter.ShouldBe("tenant-token");
        request.Headers.GetValues("dapr-app-id").ShouldBe(["eventstore"]);
        request.Headers.GetValues("dapr-api-token").ShouldBe(["secret-token"]);
    }

    private static IConfiguration Configuration(params (string Key, string? Value)[] values)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(static value => new KeyValuePair<string, string?>(value.Key, value.Value)))
            .Build();

    private sealed class CaptureHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public CaptureHandler()
            : this(new HttpResponseMessage(HttpStatusCode.Accepted))
        {
        }

        public CaptureHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(_response);
        }
    }
}
