namespace Hexalith.Tenants.Server.Tests.Bootstrap;

/// <summary>Observes the actual factory-composed transport without starting a sidecar.</summary>
/// <param name="send">The instrumented inner send operation.</param>
internal sealed class BootstrapCommandHttpMessageHandler(
    Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
{
    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => send(request, cancellationToken);
}
