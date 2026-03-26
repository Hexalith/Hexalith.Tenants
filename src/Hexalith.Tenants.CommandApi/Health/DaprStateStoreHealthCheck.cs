using Dapr.Client;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Hexalith.Tenants.CommandApi.Health;

/// <summary>
/// Health check that verifies DAPR state store reachability by performing a lightweight read.
/// The probe goes through the sidecar, so if the sidecar is down the probe fails too.
/// </summary>
internal sealed class DaprStateStoreHealthCheck(DaprClient daprClient) : IHealthCheck {
    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default) {
        try {
            _ = await daprClient.GetStateAsync<string>("statestore", "health-probe", cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex) {
            return HealthCheckResult.Unhealthy("DAPR state store is unreachable", ex);
        }
    }
}
