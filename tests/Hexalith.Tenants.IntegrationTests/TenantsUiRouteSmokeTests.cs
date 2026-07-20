using System.Net;

using Hexalith.Tenants.IntegrationTests.Fixtures;

using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

/// <summary>
/// Aspire route smoke coverage for the Tenants UI bootstrap surface.
/// </summary>
/// <remarks>
/// The Aspire topology runs with Keycloak disabled, so the hosted UI has no authenticated user.
/// Every read surface therefore renders its fail-closed <c>unauthorized</c> state (no tenant, user, or
/// audit data is revealed). These smoke tests assert that each route renders, preserves its scoped
/// navigation context, and fails closed without leaking data.
/// </remarks>
[Collection("AspireTopology")]
[DaprTestSerialization]
[Trait("Category", "Integration")]
public sealed class TenantsUiRouteSmokeTests : IDisposable {
    private const string TenantsListUnauthorizedMarker = "data-testid=\"tenants-list-unauthorized\"";
    private const string TenantsDetailUnauthorizedMarker = "data-testid=\"tenants-detail-unauthorized\"";
    private const string TenantsAuditUnauthorizedMarker = "data-testid=\"tenants-audit-unauthorized\"";
    private static readonly TimeSpan UiRouteReadinessTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan UiRouteReadinessDelay = TimeSpan.FromMilliseconds(250);
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
    public async Task Tenants_workspace_route_renders_unauthorized_state_in_hosted_ui() {
        _fixture.SkipIfUnavailable();

        string markup = await GetHostedUiMarkupWhenReadyAsync("/tenants", TenantsListUnauthorizedMarker)
            .ConfigureAwait(false);

        markup.ShouldContain("data-testid=\"tenants-workspace\"");
        markup.ShouldContain("data-testid=\"tenants-list-search\"");
        markup.ShouldContain("data-testid=\"tenants-list-refresh\"");
        markup.ShouldContain(TenantsListUnauthorizedMarker);
        markup.ShouldContain("Sign in required");
        markup.ShouldNotContain("sample tenant", Case.Insensitive);
        markup.ShouldNotContain("tenant-1", Case.Insensitive);
    }

    [DaprFact]
    public async Task Tenant_detail_route_renders_unauthorized_state_in_hosted_ui() {
        _fixture.SkipIfUnavailable();

        string markup = await GetHostedUiMarkupWhenReadyAsync(
                "/tenants/tenant.alpha?returnUrl=%2Ftenants%3Fsearch%3Dalpha",
                TenantsDetailUnauthorizedMarker)
            .ConfigureAwait(false);

        markup.ShouldContain("data-testid=\"tenants-detail\"");
        markup.ShouldContain("data-testid=\"tenants-detail-back\"");
        markup.ShouldContain("href=\"/tenants?search=alpha\"");
        markup.ShouldContain(TenantsDetailUnauthorizedMarker);
        markup.ShouldContain("role=\"alert\"");
        markup.ShouldContain("Tenant detail unauthorized");
        markup.ShouldNotContain("data-testid=\"tenants-detail-identity\"");
        markup.ShouldNotContain("sample tenant", Case.Insensitive);
    }

    [DaprFact]
    public async Task Tenant_audit_route_renders_scoped_context_and_unauthorized_state_in_hosted_ui() {
        _fixture.SkipIfUnavailable();

        string markup = await GetHostedUiMarkupWhenReadyAsync(
                "/tenants/tenant.alpha/audit?targetUserId=operator.support-01&source=member-row&returnUrl=%2Ftenants%3Fsearch%3Dalpha%26selected%3Dtenant.alpha&returnFocus=tenants-member-operator.support-01",
                TenantsAuditUnauthorizedMarker)
            .ConfigureAwait(false);

        markup.ShouldContain("data-testid=\"tenants-audit-surface\"");
        markup.ShouldContain("data-testid=\"tenants-audit-context\"");
        markup.ShouldContain("operator.support-01");
        markup.ShouldContain("data-testid=\"tenants-audit-return-context\"");
        markup.ShouldContain("tenants-member-operator.support-01");
        markup.ShouldContain("data-testid=\"tenants-audit-back\"");
        markup.ShouldContain("href=\"/tenants?search=alpha");
        markup.ShouldContain("selected=tenant.alpha");
        markup.ShouldContain(TenantsAuditUnauthorizedMarker);
        markup.ShouldContain("role=\"alert\"");
        markup.ShouldContain("You are not authorized to view tenant audit entries");
        markup.ShouldNotContain("data-testid=\"tenants-audit-row\"");
        markup.ShouldNotContain("raw payload", Case.Insensitive);
        markup.ShouldNotContain("access_token", Case.Insensitive);
    }

    [DaprFact]
    public async Task My_tenants_route_renders_unauthorized_self_audit_state_in_hosted_ui() {
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
        markup.ShouldContain("My Tenants is unauthorized");
        markup.ShouldNotContain("data-testid=\"tenants-my-row\"");
        markup.ShouldNotContain("sample tenant", Case.Insensitive);
        markup.ShouldNotContain("access_token", Case.Insensitive);
    }

    [DaprFact]
    public async Task User_lookup_route_canonicalizes_url_preserving_prefilled_user_id_in_hosted_ui() {
        _fixture.SkipIfUnavailable();

        using HttpResponseMessage response = await _fixture.TenantsUiClient
            .GetAsync("/tenants/users?userId=operator.support-01")
            .ConfigureAwait(false);

        // The compatibility route redirects before issuing the lookup and preserves the prefilled user
        // identifier. Canonical workspace URLs omit the default tenant sort.
        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        string location = response.Headers.Location?.ToString() ?? string.Empty;
        location.ShouldContain("userId=operator.support-01");
        location.ShouldNotContain("sort=");
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
        markup.ShouldContain("role=\"alert\"");
        markup.ShouldContain("Platform area unavailable");
        markup.ShouldContain("The area fails closed");
        markup.ShouldNotContain("data-testid=\"tenants-global-admins-nav\"");
        markup.ShouldNotContain("data-testid=\"tenants-global-admins-read-contract\"");
        markup.ShouldNotContain("administrator row", Case.Insensitive);
        markup.ShouldNotContain("administrator count", Case.Insensitive);
        markup.ShouldNotContain("/api/tenants", Case.Insensitive);
        markup.ShouldNotContain("/api/users", Case.Insensitive);
        markup.ShouldNotContain("access_token", Case.Insensitive);
    }

    private async Task<string> GetHostedUiMarkupWhenReadyAsync(string requestUri, string readinessMarker) {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(readinessMarker);

        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(UiRouteReadinessTimeout);
        HttpStatusCode? lastStatusCode = null;
        Uri? lastLocation = null;
        string lastMarkup = string.Empty;

        while (true) {
            using HttpResponseMessage response = await _fixture.TenantsUiClient
                .GetAsync(requestUri)
                .ConfigureAwait(false);

            lastStatusCode = response.StatusCode;
            lastLocation = response.Headers.Location;
            lastMarkup = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.OK
                && lastMarkup.Contains(readinessMarker, StringComparison.OrdinalIgnoreCase)) {
                return lastMarkup;
            }

            if (DateTimeOffset.UtcNow >= deadline) {
                break;
            }

            await Task.Delay(UiRouteReadinessDelay).ConfigureAwait(false);
        }

        lastStatusCode.ShouldBe(
            HttpStatusCode.OK,
            $"Hosted UI route '{requestUri}' did not return HTTP 200 before the readiness timeout. Last Location: {lastLocation?.ToString() ?? "<none>"}.");
        lastMarkup.ShouldContain(readinessMarker, Case.Insensitive);
        return lastMarkup;
    }
}
