using System.Net;

using Hexalith.Tenants.IntegrationTests.Fixtures;

using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

/// <summary>
/// Aspire route smoke coverage for the Tenants UI bootstrap surface.
/// </summary>
[Collection("AspireTopology")]
[DaprTestSerialization]
[Trait("Category", "Integration")]
public sealed class TenantsUiRouteSmokeTests : IDisposable {
    private readonly IDisposable _daprTestLease;
    private readonly AspireTopologyFixture _fixture;

    public TenantsUiRouteSmokeTests(AspireTopologyFixture fixture) {
        _daprTestLease = DaprTestExecutionGate.Enter();
        _fixture = fixture;
    }

    public void Dispose() {
        _daprTestLease.Dispose();
        GC.SuppressFinalize(this);
    }

    [DaprFact]
    public async Task Tenants_workspace_route_renders_tenant_list_error_state_in_hosted_ui() {
        _fixture.SkipIfUnavailable();

        using HttpResponseMessage response = await _fixture.TenantsUiClient
            .GetAsync("/tenants")
            .ConfigureAwait(false);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        string markup = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        markup.ShouldContain("data-testid=\"tenants-workspace\"");
        markup.ShouldContain("data-testid=\"tenants-list-search\"");
        markup.ShouldContain("data-testid=\"tenants-list-refresh\"");
        markup.ShouldContain("data-testid=\"tenants-list-error\"");
        markup.ShouldContain("role=\"alert\"");
        markup.ShouldContain("Tenant query gateway configuration is missing");
        markup.ShouldNotContain("Tenant read surfaces are not connected yet");
        markup.ShouldNotContain("data-connected=\"false\"");
        markup.ShouldNotContain("sample tenant", Case.Insensitive);
        markup.ShouldNotContain("tenant-1", Case.Insensitive);
        markup.ShouldNotContain("success", Case.Insensitive);
    }
}
