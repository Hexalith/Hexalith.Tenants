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

    [DaprFact]
    public async Task Tenant_detail_route_renders_safe_unavailable_state_in_hosted_ui() {
        _fixture.SkipIfUnavailable();

        using HttpResponseMessage response = await _fixture.TenantsUiClient
            .GetAsync("/tenants/tenant.alpha?returnUrl=%2Ftenants%3Fsearch%3Dalpha")
            .ConfigureAwait(false);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        string markup = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        markup.ShouldContain("data-testid=\"tenants-detail\"");
        markup.ShouldContain("data-testid=\"tenants-detail-back\"");
        markup.ShouldContain("href=\"/tenants?search=alpha\"");
        markup.ShouldContain("data-testid=\"tenants-detail-error\"");
        markup.ShouldContain("role=\"alert\"");
        markup.ShouldContain("Tenant detail cannot be loaded");
        markup.ShouldNotContain("data-testid=\"tenants-detail-identity\"");
        markup.ShouldNotContain("sample tenant", Case.Insensitive);
        markup.ShouldNotContain("success", Case.Insensitive);
    }

    [DaprFact]
    public async Task Tenant_audit_route_renders_scoped_context_and_safe_unavailable_state_in_hosted_ui() {
        _fixture.SkipIfUnavailable();

        using HttpResponseMessage response = await _fixture.TenantsUiClient
            .GetAsync("/tenants/tenant.alpha/audit?targetUserId=operator.support-01&source=member-row&returnUrl=%2Ftenants%3Fsearch%3Dalpha%26selected%3Dtenant.alpha&returnFocus=tenants-member-operator.support-01")
            .ConfigureAwait(false);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        string markup = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        markup.ShouldContain("data-testid=\"tenants-audit-surface\"");
        markup.ShouldContain("data-testid=\"tenants-audit-context\"");
        markup.ShouldContain("operator.support-01");
        markup.ShouldContain("data-testid=\"tenants-audit-return-context\"");
        markup.ShouldContain("tenants-member-operator.support-01");
        markup.ShouldContain("data-testid=\"tenants-audit-back\"");
        markup.ShouldContain("href=\"/tenants?search=alpha");
        markup.ShouldContain("selected=tenant.alpha");
        markup.ShouldContain("data-testid=\"tenants-audit-unavailable\"");
        markup.ShouldContain("role=\"alert\"");
        markup.ShouldContain("The tenant audit read surface is unavailable");
        markup.ShouldNotContain("data-testid=\"tenants-audit-row\"");
        markup.ShouldNotContain("hidden audit", Case.Insensitive);
        markup.ShouldNotContain("raw payload", Case.Insensitive);
        markup.ShouldNotContain("access_token", Case.Insensitive);
        markup.ShouldNotContain("success", Case.Insensitive);
    }

    [DaprFact]
    public async Task My_tenants_route_renders_safe_unavailable_self_audit_state_in_hosted_ui() {
        _fixture.SkipIfUnavailable();

        using HttpResponseMessage response = await _fixture.TenantsUiClient
            .GetAsync("/tenants/my")
            .ConfigureAwait(false);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        string markup = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        markup.ShouldContain("data-testid=\"tenants-my-page\"");
        markup.ShouldContain("data-testid=\"tenants-my-refresh\"");
        markup.ShouldContain("data-testid=\"tenants-my-back\"");
        markup.ShouldContain("data-testid=\"tenants-my-error\"");
        markup.ShouldContain("role=\"alert\"");
        markup.ShouldContain("My Tenants is unavailable");
        markup.ShouldNotContain("data-testid=\"tenants-my-row\"");
        markup.ShouldNotContain("sample tenant", Case.Insensitive);
        markup.ShouldNotContain("access_token", Case.Insensitive);
        markup.ShouldNotContain("success", Case.Insensitive);
    }

    [DaprFact]
    public async Task User_lookup_route_renders_safe_unavailable_prefilled_state_in_hosted_ui() {
        _fixture.SkipIfUnavailable();

        using HttpResponseMessage response = await _fixture.TenantsUiClient
            .GetAsync("/tenants/users?userId=operator.support-01")
            .ConfigureAwait(false);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        string markup = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        markup.ShouldContain("data-testid=\"tenants-user-lookup\"");
        markup.ShouldContain("data-testid=\"tenants-user-lookup-input\"");
        markup.ShouldContain("value=\"operator.support-01\"");
        markup.ShouldContain("data-testid=\"tenants-user-lookup-target\"");
        markup.ShouldContain("operator.support-01");
        markup.ShouldContain("data-testid=\"tenants-user-error\"");
        markup.ShouldContain("role=\"alert\"");
        markup.ShouldContain("User membership lookup is unavailable");
        markup.ShouldNotContain("data-testid=\"tenants-user-row\"");
        markup.ShouldNotContain("hidden membership", Case.Insensitive);
        markup.ShouldNotContain("access_token", Case.Insensitive);
        markup.ShouldNotContain("success", Case.Insensitive);
    }

    [DaprFact]
    public async Task Global_administrators_route_renders_fail_closed_unavailable_state_in_hosted_ui() {
        _fixture.SkipIfUnavailable();

        using HttpResponseMessage response = await _fixture.TenantsUiClient
            .GetAsync("/global-administrators")
            .ConfigureAwait(false);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        string markup = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        markup.ShouldContain("data-testid=\"tenants-global-admins-area\"");
        markup.ShouldContain("data-testid=\"tenants-global-admins-unavailable\"");
        markup.ShouldContain("data-testid=\"tenants-global-admins-live-region\"");
        markup.ShouldContain("data-testid=\"tenants-global-admins-recovery\"");
        markup.ShouldContain("role=\"alert\"");
        markup.ShouldContain("aria-live=\"assertive\"");
        markup.ShouldContain("Platform area unavailable");
        markup.ShouldContain("The area fails closed");
        markup.ShouldNotContain("data-testid=\"tenants-global-admins-nav\"");
        markup.ShouldNotContain("data-testid=\"tenants-global-admins-read-contract\"");
        markup.ShouldNotContain("administrator row", Case.Insensitive);
        markup.ShouldNotContain("administrator count", Case.Insensitive);
        markup.ShouldNotContain("/api/tenants", Case.Insensitive);
        markup.ShouldNotContain("/api/users", Case.Insensitive);
        markup.ShouldNotContain("access_token", Case.Insensitive);
        markup.ShouldNotContain("success", Case.Insensitive);
    }
}
