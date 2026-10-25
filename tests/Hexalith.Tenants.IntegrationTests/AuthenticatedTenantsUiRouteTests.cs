using System.Diagnostics;
using System.Net;
using System.Text.Json;

using Hexalith.Tenants.IntegrationTests.Fixtures;

using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

/// <summary>Qualifies protected FrontComposer pages and contextual audit navigation in the live topology.</summary>
[Collection("AuthenticatedTenantsTopology")]
[DaprTestSerialization]
[Trait("Category", "Integration")]
public sealed class AuthenticatedTenantsUiRouteTests(AuthenticatedTenantsTopologyFixture fixture) : IDisposable
{
    private readonly IDisposable _daprTestLease = DaprTestExecutionGate.Enter();

    /// <inheritdoc/>
    public void Dispose() => _daprTestLease.Dispose();

    [DaprFact]
    public async Task ProtectedRoutesRenderForValidatedCallerAndCurrentTenant()
    {
        fixture.SkipIfUnavailable();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        string scopeMarkup = await GetMarkupAsync("/tenants", AuthenticatedTenantsTopologyFixture.CreateCookie(AspireTopologyTests.CreateDemoJwt()), timeout.Token);
        scopeMarkup.ShouldNotContain("data-testid=\"fc-scope-blocked\"");
        scopeMarkup.ShouldContain("data-testid=\"tenants-workspace\"");
        AuthenticatedRouteState state = await AuthenticatedRouteState.CreateAsync(fixture.CommandApiClient, timeout.Token);
        string cookie = AuthenticatedTenantsTopologyFixture.CreateCookie(state.Token);
        string? listCursor = await state.WaitForListPageAsync(fixture.TenantsApiClient, 20, timeout.Token);
        foreach ((string route, string marker) in Routes(state, listCursor))
        {
            string markup = await GetReadyMarkupAsync(route, cookie, marker, state, timeout.Token);
            markup.ShouldNotContain("data-testid=\"fc-scope-blocked\"");
            markup.ShouldContain($"data-testid=\"{marker}\"");
            HasSeededReadyContent(markup, marker, state).ShouldBeTrue($"The {marker} surface must render its authoritative seeded rows.");
        }

        string detail = await GetMarkupAsync($"/tenants/{state.TenantId}", cookie, timeout.Token);
        detail.ShouldContain(state.Persisted.Name);
        detail.ShouldContain(state.TargetUserId);
        detail.Contains(state.Token, StringComparison.Ordinal).ShouldBeFalse("Rendered content must not contain the fixture access token.");
    }

    [DaprFact]
    public async Task AmbiguousCurrentTenantDeniesProtectedContent()
    {
        fixture.SkipIfUnavailable();
        string markup = await GetMarkupAsync("/tenants", AuthenticatedTenantsTopologyFixture.CreateCookie(AspireTopologyTests.CreateDemoJwt(), ambiguousTenant: true), TestContext.Current.CancellationToken);
        markup.ShouldContain("data-testid=\"fc-scope-blocked\"");
        markup.ShouldNotContain("data-testid=\"tenants-workspace\"");
    }

    [DaprFact]
    public async Task LiveBrowserMeasuresResponsiveForcedColorsAndRestoresContextualAuditBackFocus()
    {
        fixture.SkipIfUnavailable();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(8));
        AuthenticatedRouteState state = await AuthenticatedRouteState.CreateAsync(fixture.CommandApiClient, timeout.Token);
        string? listCursor = await state.WaitForListPageAsync(fixture.TenantsApiClient, 20, timeout.Token);
        string directory = Path.Combine(Path.GetTempPath(), $"tenants-live-routes-{Guid.NewGuid():N}");
        if (OperatingSystem.IsWindows())
        {
            Directory.CreateDirectory(directory);
        }
        else
        {
            Directory.CreateDirectory(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
        string inputPath = Path.Combine(directory, "session.private.json");
        string outputPath = Path.Combine(directory, "result.json");
        try
        {
            var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.None, Options = FileOptions.Asynchronous };
            if (!OperatingSystem.IsWindows())
            {
                options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
            }

            await using (var input = new FileStream(inputPath, options))
            {
                await JsonSerializer.SerializeAsync(input, new
                {
                    baseUrl = fixture.TenantsUiClient.BaseAddress!.AbsoluteUri,
                    tenantId = state.TenantId,
                    tenantName = state.Persisted.Name,
                    targetUserId = state.TargetUserId,
                    callerId = AuthenticatedRouteState.CallerId,
                    listPath = listCursor is null ? "/tenants" : $"/tenants?cursor={Uri.EscapeDataString(listCursor)}",
                    cookie = AuthenticatedTenantsTopologyFixture.CreateCookie(state.Token),
                    configuration = AppContext.BaseDirectory.Contains($"{Path.DirectorySeparatorChar}Release{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ? "Release" : "Debug",
                }, cancellationToken: timeout.Token);
            }

            var start = new ProcessStartInfo("node") { WorkingDirectory = AuthenticatedTenantsTopologyFixture.RepositoryRoot, RedirectStandardOutput = true, RedirectStandardError = true };
            start.ArgumentList.Add("tests/Hexalith.Tenants.UI.Tests/Browser/validate-authenticated-live-routes.mjs");
            start.ArgumentList.Add(inputPath);
            start.ArgumentList.Add(outputPath);
            using Process process = Process.Start(start).ShouldNotBeNull();
            // Output is deliberately discarded: the maintained validator writes only fixed safe
            // failure labels into result.json, never browser URLs, cookie tickets or access tokens.
            Task stdout = process.StandardOutput.ReadToEndAsync(timeout.Token);
            Task stderr = process.StandardError.ReadToEndAsync(timeout.Token);
            try
            {
                await process.WaitForExitAsync(timeout.Token);
                await Task.WhenAll(stdout, stderr);
            }
            finally
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }

            File.Exists(outputPath).ShouldBeTrue("The live validator must produce its support-safe result artifact.");
            using JsonDocument evidence = JsonDocument.Parse(await File.ReadAllTextAsync(outputPath, timeout.Token));
            process.ExitCode.ShouldBe(0, evidence.RootElement.GetRawText());
            evidence.RootElement.GetProperty("passed").GetBoolean().ShouldBeTrue();
            evidence.RootElement.GetProperty("cases").GetArrayLength().ShouldBeGreaterThanOrEqualTo(21);
            TestContext.Current.TestOutputHelper?.WriteLine($"Safe live browser evidence: {outputPath}");
        }
        finally
        {
            File.Delete(inputPath);
        }
    }

    private static IEnumerable<(string Route, string Marker)> Routes(AuthenticatedRouteState state, string? listCursor)
    {
        yield return (listCursor is null ? "/tenants" : $"/tenants?cursor={Uri.EscapeDataString(listCursor)}", "tenants-workspace");
        yield return ($"/tenants/{state.TenantId}", "tenants-detail");
        yield return ($"/tenants/{state.TenantId}/audit", "tenants-audit-surface");
        yield return ("/tenants/my", "tenants-my-page");
        yield return ($"/tenants/users?userId={state.TargetUserId}", "tenants-user-lookup");
        yield return ("/global-administrators", "tenants-global-admins-area");
    }

    private async Task<string> GetReadyMarkupAsync(string route, string cookie, string marker, AuthenticatedRouteState state, CancellationToken cancellationToken)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(30);
        string markup;
        do
        {
            markup = await GetMarkupAsync(route, cookie, cancellationToken);
            if (HasSeededReadyContent(markup, marker, state))
            {
                return markup;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
        }
        while (DateTimeOffset.UtcNow < deadline);

        return markup;
    }

    private static bool HasSeededReadyContent(string markup, string marker, AuthenticatedRouteState state)
        => marker switch
        {
            "tenants-workspace" => markup.Contains("data-testid=\"tenants-list-grid\"", StringComparison.Ordinal)
                && markup.Contains($"title=\"{state.TenantId}\"", StringComparison.Ordinal)
                && markup.Contains("truth-state-badge--current", StringComparison.Ordinal),
            "tenants-detail" => markup.Contains("data-testid=\"tenants-member-row\"", StringComparison.Ordinal)
                && markup.Contains($"id=\"tenants-member-{state.TargetUserId}\"", StringComparison.Ordinal)
                && markup.Contains("truth-state-badge--current", StringComparison.Ordinal),
            "tenants-audit-surface" => markup.Contains("data-testid=\"tenants-audit-ready\"", StringComparison.Ordinal)
                && markup.Contains("data-testid=\"tenants-audit-row\"", StringComparison.Ordinal),
            "tenants-my-page" => markup.Contains("data-testid=\"tenants-my-row\"", StringComparison.Ordinal)
                && markup.Contains($"title=\"{state.TenantId}\"", StringComparison.Ordinal)
                && markup.Contains("truth-state-badge--current", StringComparison.Ordinal),
            "tenants-user-lookup" => markup.Contains("data-testid=\"tenants-user-row\"", StringComparison.Ordinal)
                && markup.Contains($"title=\"{state.TenantId}\"", StringComparison.Ordinal)
                && markup.Contains("truth-state-badge--current", StringComparison.Ordinal),
            "tenants-global-admins-area" => markup.Contains("data-testid=\"tenants-global-admins-ready\"", StringComparison.Ordinal)
                && markup.Contains("data-testid=\"tenants-global-admins-row\"", StringComparison.Ordinal)
                && markup.Contains($"title=\"{AuthenticatedRouteState.CallerId}\"", StringComparison.Ordinal)
                && !markup.Contains("data-testid=\"tenants-global-admins-restricted-header\"", StringComparison.Ordinal)
                && !markup.Contains("data-testid=\"tenants-global-admins-unavailable\"", StringComparison.Ordinal),
            _ => false,
        };

    private async Task<string> GetMarkupAsync(string route, string cookie, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, route);
        request.Headers.Add("Cookie", cookie);
        using HttpResponseMessage response = await fixture.TenantsUiClient.SendAsync(request, cancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
