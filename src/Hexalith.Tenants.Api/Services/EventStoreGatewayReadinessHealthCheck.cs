using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Hexalith.Tenants.Api.Services;

/// <summary>Checks the EventStore gateway readiness through this API's Dapr sidecar.</summary>
public sealed class EventStoreGatewayReadinessHealthCheck(IHttpClientFactory clients) : IHealthCheck
{
    /// <summary>The named Dapr invocation client used for the readiness request.</summary>
    public const string ClientName = "eventstore-gateway-readiness";

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using HttpResponseMessage response = await clients.CreateClient(ClientName)
                .GetAsync("/ready", cancellationToken).ConfigureAwait(false);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy($"EventStore gateway /ready returned {(int)response.StatusCode}.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy("EventStore gateway /ready is unavailable.", exception);
        }
    }
}
