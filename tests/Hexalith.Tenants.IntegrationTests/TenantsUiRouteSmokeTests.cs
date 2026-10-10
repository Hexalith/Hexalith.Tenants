using System.Net;
using System.Text.RegularExpressions;

using Hexalith.Tenants.IntegrationTests.Fixtures;

using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

/// <summary>
/// Aspire smoke coverage for the anonymous Tenants UI routes behind the FrontComposer scope boundary.
/// </summary>
/// <remarks>
/// The fixture disables Keycloak and supplies no validated tenant or user scope. The hosted shell must
/// render its explicit denial before any Tenants page content. Tier 1 component tests cover the page-level
/// unauthorized states when a scope is available.
/// </remarks>
[Collection("AspireTopology")]
[DaprTestSerialization]
[Trait("Category", "Integration")]
public sealed class TenantsUiRouteSmokeTests : IDisposable
{
    private const string ScopeBlockedMarker = "data-testid=\"fc-scope-blocked\"";
    private readonly IDisposable _daprTestLease;
    private readonly AspireTopologyFixture _fixture;

    public TenantsUiRouteSmokeTests(AspireTopologyFixture fixture)
    {
        _daprTestLease = DaprTestExecutionGate.Enter();
        _fixture = fixture;
    }

    public void Dispose()
    {
        _daprTestLease.Dispose();
        GC.SuppressFinalize(this);
    }

    [DaprFact]
    public async Task TenantsWorkspaceRouteBlocksAnonymousScope()
    {
        _fixture.SkipIfUnavailable();
        await AssertHostedScopeBlockedAsync("/tenants").ConfigureAwait(false);
    }

    [DaprFact]
    public async Task TenantDetailRouteBlocksAnonymousScope()
    {
        _fixture.SkipIfUnavailable();
        await AssertHostedScopeBlockedAsync(
            "/tenants/tenant.alpha?returnUrl=%2Ftenants%3Fsearch%3Dalpha").ConfigureAwait(false);
    }

    [DaprFact]
    public async Task TenantAuditRouteBlocksAnonymousScope()
    {
        _fixture.SkipIfUnavailable();
        await AssertHostedScopeBlockedAsync(
            "/tenants/tenant.alpha/audit?targetUserId=operator.support-01&source=member-row&returnUrl=%2Ftenants%2Ftenant.alpha%3FreturnUrl%3D%252Ftenants%253Fsearch%253Dalpha%2526selected%253Dtenant.alpha&returnFocus=tenants-member-operator.support-01").ConfigureAwait(false);
    }

    [DaprFact]
    public async Task MyTenantsRouteBlocksAnonymousScope()
    {
        _fixture.SkipIfUnavailable();
        await AssertHostedScopeBlockedAsync("/tenants/my").ConfigureAwait(false);
    }

    [DaprFact]
    public async Task UserLookupRouteBlocksAnonymousScope()
    {
        _fixture.SkipIfUnavailable();
        await AssertHostedScopeBlockedAsync("/tenants/users?userId=operator.support-01").ConfigureAwait(false);
    }

    [DaprFact]
    public async Task GlobalAdministratorsRouteBlocksAnonymousScope()
    {
        _fixture.SkipIfUnavailable();
        await AssertHostedScopeBlockedAsync("/global-administrators").ConfigureAwait(false);
    }

    private async Task AssertHostedScopeBlockedAsync(string requestUri)
    {
        using HttpResponseMessage response = await _fixture.TenantsUiClient
            .GetAsync(requestUri, TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        string markup = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        markup.ShouldContain(ScopeBlockedMarker);
        markup.ShouldContain("Workspace unavailable");
        markup.ShouldContain("Your workspace identity could not be verified");

        foreach (string protectedMarker in new[]
        {
            "data-testid=\"tenants-workspace\"",
            "data-testid=\"tenants-detail\"",
            "data-testid=\"tenants-audit-surface\"",
            "data-testid=\"tenants-my-page\"",
            "data-testid=\"tenants-user-lookup\"",
            "data-testid=\"tenants-global-admins-area\"",
            "data-testid=\"tenants-audit-row\"",
            "data-testid=\"tenants-my-row\"",
        })
        {
            markup.ShouldNotContain(protectedMarker);
        }

        foreach (string sensitiveValue in new[]
        {
            "sample tenant",
            "tenant-1",
            "raw payload",
            "administrator row",
            "administrator count",
            "access_token",
            "refresh_token",
            "id_token",
        })
        {
            markup.ShouldNotContain(sensitiveValue, Case.Insensitive);
        }

        // FrontComposer skip links include the caller-supplied URL in href; it is not page data.
        string markupWithoutSkipLinkTargets = Regex.Replace(
            markup,
            @"(?<=<a class=""fc-skip-link"" href="")[^""]*",
            string.Empty,
            RegexOptions.CultureInvariant);
        markupWithoutSkipLinkTargets.ShouldNotContain("tenant.alpha", Case.Insensitive);
        markupWithoutSkipLinkTargets.ShouldNotContain("operator.support-01", Case.Insensitive);
    }
}
