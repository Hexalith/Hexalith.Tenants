namespace Hexalith.Tenants.IntegrationTests.Fixtures;

/// <summary>
/// Shared xUnit fixture that boots the full Tenants Aspire AppHost topology
/// (CommandApi + Sample with DAPR sidecars) and creates HTTP clients for smoke tests.
/// </summary>
/// <remarks>
/// All DAPR/Aspire plumbing — placement/scheduler endpoint resolution, prerequisite probing, build/
/// start, client creation, endpoint polling, and liveness diagnostics — lives in the reusable platform
/// base <see cref="AspireTopologyFixtureBase{TAppHost}"/>. This fixture supplies only the Tenants
/// AppHost type, the resources to wait on, and the typed client accessors used by the tests.
/// </remarks>
public sealed class AspireTopologyFixture : AspireTopologyFixtureBase<Projects.Hexalith_Tenants_AppHost> {
    private static readonly TimeSpan CommandApiHealthTimeout = TimeSpan.FromMinutes(4);
    private static readonly TimeSpan TenantsApiHealthTimeout = TimeSpan.FromMinutes(4);
    private static readonly TimeSpan SampleHealthTimeout = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan CommandApiClientTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan TenantsApiClientTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan SampleClientTimeout = TimeSpan.FromSeconds(30);

    /// <inheritdoc/>
    protected override IReadOnlyList<AspireResource> Resources =>
    [
        new("eventstore", "http", CommandApiClientTimeout, CommandApiHealthTimeout, WaitForAliveness: true, CommandApiHealthTimeout),
        new("tenants", "http", CommandApiClientTimeout, CommandApiHealthTimeout, WaitForAliveness: true, CommandApiHealthTimeout),
        new("tenants-api", "https", TenantsApiClientTimeout, TenantsApiHealthTimeout, WaitForAliveness: true, TenantsApiHealthTimeout),
        new("tenants-ui", "http", CommandApiClientTimeout, CommandApiHealthTimeout, WaitForAliveness: false, CommandApiHealthTimeout),
        new("sample", "http", SampleClientTimeout, SampleHealthTimeout, WaitForAliveness: true, SampleHealthTimeout),
    ];

    /// <inheritdoc/>
    protected override IReadOnlyList<string> ExtraAppArgs => ["--EnableKeycloak=false"];

    /// <summary>Gets the HTTP client for the CommandApi (eventstore) service.</summary>
    public HttpClient CommandApiClient => Client("eventstore");

    /// <summary>Gets the HTTP client for the Tenants domain service (exposes /process endpoint).</summary>
    public HttpClient TenantsClient => Client("tenants");

    /// <summary>Gets the HTTP client for the generated Tenants REST API.</summary>
    public HttpClient TenantsApiClient => Client("tenants-api");

    /// <summary>Gets the HTTP client for the Tenants UI resource.</summary>
    public HttpClient TenantsUiClient => Client("tenants-ui");

    /// <summary>Gets the HTTP client for the Sample service.</summary>
    public HttpClient SampleClient => Client("sample");
}
