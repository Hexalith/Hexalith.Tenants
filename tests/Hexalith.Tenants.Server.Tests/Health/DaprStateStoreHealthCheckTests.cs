using Dapr;
using Dapr.Client;

using Hexalith.Tenants.CommandApi.Health;

using Microsoft.Extensions.Diagnostics.HealthChecks;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Health;

public class DaprStateStoreHealthCheckTests {
    [Fact]
    public async Task CheckHealthAsync_WhenStateStoreReachable_ShouldReturnHealthy() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        _ = daprClient.GetStateAsync<string?>("statestore", "health-probe", cancellationToken: Arg.Any<CancellationToken>())
            .Returns((string?)null);

        var healthCheck = new DaprStateStoreHealthCheck(daprClient);
        HealthCheckContext context = CreateContext();

        HealthCheckResult result = await healthCheck.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenDaprExceptionThrown_ShouldReturnUnhealthy() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        _ = daprClient.GetStateAsync<string>("statestore", "health-probe", cancellationToken: Arg.Any<CancellationToken>())
            .ThrowsAsync(new DaprException("State store unavailable"));

        var healthCheck = new DaprStateStoreHealthCheck(daprClient);
        HealthCheckContext context = CreateContext();

        HealthCheckResult result = await healthCheck.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldBe("DAPR state store is unreachable");
        _ = result.Exception.ShouldBeOfType<DaprException>();
    }

    [Fact]
    public async Task CheckHealthAsync_WhenTaskCanceledExceptionThrown_ShouldReturnUnhealthy() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        _ = daprClient.GetStateAsync<string>("statestore", "health-probe", cancellationToken: Arg.Any<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Connection timed out"));

        var healthCheck = new DaprStateStoreHealthCheck(daprClient);
        HealthCheckContext context = CreateContext();

        HealthCheckResult result = await healthCheck.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        _ = result.Exception.ShouldBeOfType<TaskCanceledException>();
    }

    [Fact]
    public async Task CheckHealthAsync_WhenHttpRequestExceptionThrown_ShouldReturnUnhealthy() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        _ = daprClient.GetStateAsync<string>("statestore", "health-probe", cancellationToken: Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var healthCheck = new DaprStateStoreHealthCheck(daprClient);
        HealthCheckContext context = CreateContext();

        HealthCheckResult result = await healthCheck.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        _ = result.Exception.ShouldBeOfType<HttpRequestException>();
    }

    private static HealthCheckContext CreateContext()
        => new() {
            Registration = new HealthCheckRegistration(
                "dapr-statestore",
                Substitute.For<IHealthCheck>(),
                failureStatus: HealthStatus.Degraded,
                tags: ["ready"]),
        };
}
