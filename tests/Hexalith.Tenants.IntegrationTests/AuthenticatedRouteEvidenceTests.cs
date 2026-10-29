using System.Text.Json;

using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

/// <summary>Checks safe wrapper artifacts without launching authentication, Dapr, or a browser.</summary>
public sealed class AuthenticatedRouteEvidenceTests
{
    [Fact]
    public void BrowserFailureSummaryKeepsOnlyBoundedReadinessAndCategories()
    {
        using var evidence = JsonDocument.Parse("""
            {"failure":{"phase":"tenant-list-layout-1440-none-ready","label":"browser-timeout"},
             "readiness":{"available":true,"seededRowCount":1,"currentBadgeCount":1,"currentBadgeVisible":false,
                "workspace":true,"grid":true,"loading":false,"error":false,"unauthorized":false,"degraded":false,
                "pagingRecovered":false,"pageSize":20,"cursorPresent":true,"searchCursorPresent":false,
                "private":"raw-cookie-token-cursor-sentinel"}}
            """);
        string summary = AuthenticatedTenantsUiRouteTests.DescribeBrowserFailure(evidence.RootElement);
        summary.ShouldContain("Phase=tenant-list-layout-1440-none-ready");
        summary.ShouldContain("Label=browser-timeout");
        summary.ShouldContain("\"currentBadgeVisible\":false");
        summary.ShouldContain("\"pageSize\":20");
        summary.ShouldNotContain("raw-cookie-token-cursor-sentinel");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EarlyFailureAndTimeoutRetainOnlyFixedCategoriesInPrivateArtifacts(bool timeout)
    {
        string? artifact = null;
        try
        {
            InvalidOperationException failure = await Should.ThrowAsync<InvalidOperationException>(() =>
                AuthenticatedTenantsUiRouteTests.WithSafeEvidenceAsync(TimeSpan.FromMilliseconds(100), async (path, phase, cancellationToken) =>
                {
                    artifact = path;
                    File.Exists(path).ShouldBeTrue();
                    if (!OperatingSystem.IsWindows())
                    {
                        File.GetUnixFileMode(path).ShouldBe(UnixFileMode.UserRead | UnixFileMode.UserWrite);
                        File.GetUnixFileMode(Path.GetDirectoryName(path)!).ShouldBe(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
                    }

                    phase("sign-in");
                    if (timeout)
                    {
                        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                    }

                    throw new InvalidOperationException("Bearer private-token-sentinel https://private.invalid/?code=signed-url-sentinel");
                }));
            string json = await File.ReadAllTextAsync(artifact.ShouldNotBeNull());
            json.ShouldNotContain("private-token-sentinel");
            json.ShouldNotContain("signed-url-sentinel");
            failure.Message.ShouldNotContain("private-token-sentinel");
            failure.InnerException.ShouldBeNull();
            using JsonDocument evidence = JsonDocument.Parse(json);
            evidence.RootElement.GetProperty("passed").GetBoolean().ShouldBeFalse();
            evidence.RootElement.GetProperty("failure").GetProperty("phase").GetString().ShouldBe("sign-in");
            evidence.RootElement.GetProperty("failure").GetProperty("label").GetString()
                .ShouldBe(timeout ? "qualification-timeout" : "qualification-failed");
        }
        finally
        {
            if (artifact is not null) Directory.Delete(Path.GetDirectoryName(artifact)!, recursive: true);
        }
    }

    [Fact]
    public async Task CompletedSafeBrowserResultSurvivesOuterFailure()
    {
        string? artifact = null;
        const string browserResult = "{\"finishedUtc\":\"2026-10-25T00:00:00Z\",\"mode\":\"live browser\",\"passed\":false,\"cases\":[],\"failure\":{\"phase\":\"interactive-filter-change\",\"label\":\"browser-timeout\"}}";
        try
        {
            await Should.ThrowAsync<InvalidOperationException>(() =>
                AuthenticatedTenantsUiRouteTests.WithSafeEvidenceAsync(TimeSpan.FromSeconds(5), async (path, phase, _) =>
                {
                    artifact = path;
                    phase("browser-run");
                    await File.WriteAllTextAsync(path, browserResult);
                    throw new TimeoutException("private-exception-sentinel");
                }));
            (await File.ReadAllTextAsync(artifact.ShouldNotBeNull())).ShouldBe(browserResult);
        }
        finally
        {
            if (artifact is not null) Directory.Delete(Path.GetDirectoryName(artifact)!, recursive: true);
        }
    }
}
