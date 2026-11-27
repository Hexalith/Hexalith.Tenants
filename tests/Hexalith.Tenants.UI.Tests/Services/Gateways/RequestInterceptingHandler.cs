namespace Hexalith.Tenants.UI.Tests.Services.Gateways;

/// <summary>Invokes a callback before delegating an outbound HTTP request to its inner handler.</summary>
/// <param name="onRequest">Callback invoked for each outbound request before delegation.</param>
/// <param name="innerHandler">The inner handler that executes the request.</param>
internal sealed class RequestInterceptingHandler(
    Action<HttpRequestMessage> onRequest,
    HttpMessageHandler innerHandler) : DelegatingHandler(innerHandler)
{
    private readonly Action<HttpRequestMessage> _onRequest = onRequest ?? throw new ArgumentNullException(nameof(onRequest));

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        _onRequest(request);
        return base.SendAsync(request, cancellationToken);
    }
}
