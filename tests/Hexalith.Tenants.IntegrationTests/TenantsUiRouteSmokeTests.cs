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
    public async Task Tenants_workspace_route_renders_unavailable_status_in_hosted_ui() {
        _fixture.SkipIfUnavailable();

        using HttpResponseMessage response = await _fixture.TenantsUiClient
            .GetAsync("/tenants")
            .ConfigureAwait(false);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        string markup = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        markup.ShouldContain("data-testid=\"tenants-shell-status\"");
        markup.ShouldContain("role=\"status\"");
        markup.ShouldContain("data-connected=\"false\"");
        markup.ShouldContain("Tenant read surfaces are not connected yet");
        markup.ShouldContain("Review status details");
        markup.ShouldNotContain("sample tenant", Case.Insensitive);
        markup.ShouldNotContain("tenant-1", Case.Insensitive);
        markup.ShouldNotContain("success", Case.Insensitive);
    }
}
