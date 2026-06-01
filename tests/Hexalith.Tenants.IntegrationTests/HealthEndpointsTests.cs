#pragma warning disable CA2007

using System.Net;

using Hexalith.Tenants.Configuration;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

/// <summary>
/// Story 7.5 AC1 — deterministic readiness/liveness coverage for the Tenants host.
/// </summary>
/// <remarks>
/// <para>
/// Infrastructure-free: the host boots in-memory through <see cref="WebApplicationFactory{T}"/>
/// and the DAPR state-store readiness check is replaced with a deterministic stub. This proves the
/// endpoint contract — <c>/ready</c> reflects dependency readiness, <c>/alive</c> reflects process
/// liveness — without requiring a live DAPR sidecar.
/// </para>
/// <para>
/// Required readiness dependency list for Tenants: the DAPR sidecar/state store
/// (<c>dapr-statestore</c>, tag <c>ready</c>) that backs runtime actor and projection state.
/// EventStore service-invocation readiness is intentionally NOT probed by <c>/ready</c> — it is a
/// downstream service whose availability is proven by the DAPR end-to-end command/query tests and
/// the deployment smoke lane (Story 7.6C), so readiness stays a bounded local-dependency check that
/// never calls command-processing endpoints.
/// </para>
/// </remarks>
public class HealthEndpointsTests {
    [Fact]
    public void Readiness_registers_dapr_statestore_dependency_check_as_unhealthy_on_failure() {
        using var factory = new WebApplicationFactory<TenantBootstrapOptions>();

        HealthCheckServiceOptions options = factory.Services
            .GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;

        HealthCheckRegistration registration = options.Registrations
            .Where(r => r.Name == "dapr-statestore")
            .ShouldHaveSingleItem();
        registration.Tags.ShouldContain("ready");
        // Readiness failure must classify as Unhealthy (→ HTTP 503), never Degraded (→ HTTP 200).
        registration.FailureStatus.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task Ready_returns_503_when_readiness_dependency_is_unhealthy() {
        await using var factory = new HealthWebApplicationFactory(HealthStatus.Unhealthy);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/ready");

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Ready_returns_200_when_readiness_dependency_is_healthy() {
        await using var factory = new HealthWebApplicationFactory(HealthStatus.Healthy);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/ready");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Alive_stays_200_while_ready_fails_so_liveness_is_distinct_from_readiness() {
        await using var factory = new HealthWebApplicationFactory(HealthStatus.Unhealthy);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage aliveResponse = await client.GetAsync("/alive");
        using HttpResponseMessage readyResponse = await client.GetAsync("/ready");

        // Liveness must not depend on DAPR/EventStore — the process is up even when a dependency is down,
        // so an orchestrator restarts on liveness failure but only withholds traffic on readiness failure.
        aliveResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        readyResponse.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Ready_development_json_response_is_support_safe_and_hides_exception_internals() {
        // A failing dependency's exception can carry raw adapter internals or stack traces.
        // The development JSON response (exposed by MapDefaultEndpoints) must surface the dependency
        // category — status + a safe description — but never the raw exception internals.
        const string rawExceptionMarker = "RAW-EXCEPTION-MARKER-12345";
        await using var factory = new HealthWebApplicationFactory(
            HealthStatus.Unhealthy,
            environment: "Development",
            description: "DAPR state store is unreachable",
            exception: new InvalidOperationException(rawExceptionMarker));
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/ready");
        string body = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        // Support-safe diagnostics: status + safe category description are allowed.
        body.ShouldContain("Unhealthy");
        body.ShouldContain("DAPR state store is unreachable");
        // Never leak the failing dependency's exception message or stack trace.
        body.ShouldNotContain(rawExceptionMarker);
        body.ShouldNotContain("StackTrace", Case.Insensitive);
        body.ShouldNotContain("InvalidOperationException");
    }

    /// <summary>
    /// Boots the real Tenants host but swaps every readiness-tagged ("ready") health check for a
    /// deterministic stub reporting <paramref name="readinessStatus"/> (optionally with a
    /// <paramref name="description"/> and failing <paramref name="exception"/>), leaving the liveness
    /// ("live") self check untouched. <paramref name="environment"/> selects the host environment so
    /// tests can exercise the development JSON response writer.
    /// </summary>
    private sealed class HealthWebApplicationFactory(
        HealthStatus readinessStatus,
        string? environment = null,
        string? description = null,
        Exception? exception = null)
        : WebApplicationFactory<TenantBootstrapOptions> {
        protected override void ConfigureWebHost(IWebHostBuilder builder) {
            if (environment is not null) {
                _ = builder.UseEnvironment(environment);
            }

            _ = builder.ConfigureServices(services
                => services.Configure<HealthCheckServiceOptions>(options => {
                    List<HealthCheckRegistration> readyChecks = options.Registrations
                        .Where(r => r.Tags.Contains("ready"))
                        .ToList();
                    foreach (HealthCheckRegistration check in readyChecks) {
                        _ = options.Registrations.Remove(check);
                    }

                    options.Registrations.Add(new HealthCheckRegistration(
                        "dapr-statestore",
                        _ => new StubReadinessCheck(readinessStatus, description, exception),
                        HealthStatus.Unhealthy,
                        ["ready"]));
                }));
        }
    }

    private sealed class StubReadinessCheck(
        HealthStatus status, string? description = null, Exception? exception = null) : IHealthCheck {
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(new HealthCheckResult(status, description, exception));
    }
}
