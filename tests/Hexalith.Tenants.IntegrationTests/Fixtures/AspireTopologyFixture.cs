using Hexalith.EventStore.Testing.Integration;

namespace Hexalith.Tenants.IntegrationTests.Fixtures;

/// <summary>
/// Shared xUnit fixture that boots the full Tenants Aspire AppHost topology
/// (eventstore + tenants + tenants-ui + sample with DAPR sidecars) on the shared EventStore platform
/// fixture base, and exposes typed HTTP clients for smoke tests.
/// </summary>
/// <remarks>
/// This fixture verifies <strong>process liveness</strong>, not full readiness — it waits for
/// resources to reach <c>Running</c> and for <c>/alive</c> to return HTTP 200. Full Dapr readiness
/// (placement registration, sidecar handshake, state-store availability) is covered by the
/// Dapr-specific integration tests. All generic probing/build/start/client-create logic lives in the
/// EventStore platform package (<see cref="AspireTopologyFixtureBase{TAppHost}"/>).
/// </remarks>
public sealed class AspireTopologyFixture : AspireTopologyFixtureBase<Projects.Hexalith_Tenants_AppHost> {
    /// <inheritdoc/>
    protected override IReadOnlyList<string> ResourceNames =>
        ["eventstore", "tenants", "tenants-ui", "sample"];

    /// <inheritdoc/>
    protected override IReadOnlyList<string> AlivenessResourceNames =>
        ["eventstore", "tenants", "sample"];

    /// <inheritdoc/>
    protected override IReadOnlyList<string> ExtraAppArgs => ["--EnableKeycloak=false"];

    /// <summary>Gets the HTTP client for the EventStore CommandApi service.</summary>
    public HttpClient CommandApiClient => Client("eventstore");

    /// <summary>Gets the HTTP client for the Tenants domain service (exposes /process endpoint).</summary>
    public HttpClient TenantsClient => Client("tenants");

    /// <summary>Gets the HTTP client for the Tenants UI resource.</summary>
    public HttpClient TenantsUiClient => Client("tenants-ui");

    /// <summary>Gets the HTTP client for the Sample service.</summary>
    public HttpClient SampleClient => Client("sample");
}
