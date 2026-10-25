namespace Hexalith.Tenants.IntegrationTests.Fixtures;

/// <summary>Exercises the fixture's complete HTTP boundary without starting a live topology.</summary>
/// <param name="send">The per-request HTTP response or transport failure.</param>
internal sealed class BootstrapHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
{
    /// <inheritdoc/>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => send(request, cancellationToken);
}
