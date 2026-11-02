extern alias TenantsApi;

using System.Security.Cryptography;

using Microsoft.Extensions.Http;

using ForwardingHandler = TenantsApi::Hexalith.Tenants.Api.Services.InboundBearerForwardingHandler;

namespace Hexalith.Tenants.IntegrationTests;

/// <summary>Observes real API client construction and replaces only its terminal gateway transport.</summary>
/// <param name="send">The bounded in-memory transport operation.</param>
internal sealed class ApiGatewayResilienceCapture(
    Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
    : HttpMessageHandler, IHttpMessageHandlerBuilderFilter
{
    /// <summary>Gets the actual name chosen by the production typed client registration.</summary>
    internal string GatewayClientName { get; private set; } = string.Empty;

    /// <summary>Gets the per-factory synthetic authentication key without retaining a reusable credential.</summary>
    internal string SigningKey { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

    /// <summary>Gets the per-factory synthetic sidecar credential.</summary>
    internal string SidecarToken { get; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    /// <summary>Gets the per-factory synthetic caller bearer parameter.</summary>
    internal string BearerToken { get; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    /// <summary>Gets the registered delegating handlers for each constructed client.</summary>
    internal Dictionary<string, Type[]> HandlerTypes { get; } = new(StringComparer.Ordinal);

    /// <summary>Supplies the SDK's separately owned nonredirecting invocation transport.</summary>
    internal HttpMessageHandler CreateIndependentTransport() => new ApiGatewayResilienceCapture(send);

    /// <inheritdoc/>
    public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
    {
        ArgumentNullException.ThrowIfNull(next);
        return builder =>
        {
            next(builder);
            string name = builder.Name ?? throw new InvalidOperationException("The HTTP client builder has no name.");
            HandlerTypes[name] = builder.AdditionalHandlers.Select(handler => handler.GetType()).ToArray();
            if (!builder.AdditionalHandlers.Any(handler => handler is ForwardingHandler)) return;
            GatewayClientName = name;
            builder.PrimaryHandler = this;
        };
    }

    /// <inheritdoc/>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => send(request, cancellationToken);
}
