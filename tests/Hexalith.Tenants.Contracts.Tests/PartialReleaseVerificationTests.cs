using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

using Shouldly;

namespace Hexalith.Tenants.Contracts.Tests;

/// <summary>
/// Exercises recovery verification against isolated destination metadata.
/// </summary>
public class PartialReleaseVerificationTests
{
    /// <summary>
    /// Final verification requires Boolean classification matching the reviewed version.
    /// </summary>
    /// <param name="version">The reviewed recovery version.</param>
    /// <param name="classification">The returned JSON classification, or an absent property.</param>
    /// <param name="accepted">Whether the metadata proves the expected release classification.</param>
    [Theory]
    [InlineData("1.2.3", "false", true)]
    [InlineData("1.2.3-alpha", "true", true)]
    [InlineData("1.2.3-rc.1", "true", true)]
    [InlineData("1.2.3-preview-build.7", "true", true)]
    [InlineData("1.2.3-0", "true", true)]
    [InlineData("1.2.3", "true", false)]
    [InlineData("1.2.3-rc.1", "false", false)]
    [InlineData("1.2.3", null, false)]
    [InlineData("1.2.3-rc.1", null, false)]
    [InlineData("1.2.3", "null", false)]
    [InlineData("1.2.3-rc.1", "null", false)]
    [InlineData("1.2.3", "\"false\"", false)]
    [InlineData("1.2.3-rc.1", "\"true\"", false)]
    public async Task FinalVerificationRequiresMatchingClassification(string version, string? classification, bool accepted)
    {
        const string sourceSha = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Hexalith.Tenants.slnx")))
        {
            root = root.Parent;
        }

        string repoRoot = root?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
        string temporaryRoot = Path.Combine(Path.GetTempPath(), $"tenants-verification-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(temporaryRoot, "tools"));
        try
        {
            string manifestPath = Path.Combine(repoRoot, "tools/release-packages.json");
            File.Copy(manifestPath, Path.Combine(temporaryRoot, "tools/release-packages.json"));
            using var manifest = JsonDocument.Parse(await File.ReadAllTextAsync(manifestPath));
            string[] assets = [.. manifest.RootElement.GetProperty("packages").EnumerateArray()
                .Select(package => $"{package.GetProperty("id").GetString()}.{version}.nupkg")];
            JsonObject release = new()
            {
                ["tag_name"] = $"v{version}",
                ["draft"] = false,
                ["assets"] = new JsonArray(assets.Select(name => (JsonNode)new JsonObject { ["name"] = name }).ToArray()),
            };
            if (classification is not null)
            {
                release["prerelease"] = JsonNode.Parse(classification);
            }

            string releasePath = Path.Combine(temporaryRoot, "release.json");
            await File.WriteAllTextAsync(releasePath, release.ToJsonString());
            const string fixture = """
                set -euo pipefail
                gh() {
                  case "$*" in
                    "api repos/fixture/recovery/git/ref/tags/v${FIXTURE_VERSION} --jq .object.sha") echo "$GITHUB_SHA" ;;
                    "api repos/fixture/recovery/git/ref/tags/v${FIXTURE_VERSION} --jq .object.type") echo commit ;;
                    "api repos/fixture/recovery/releases/tags/v${FIXTURE_VERSION}") cat "$FIXTURE_RELEASE" ;;
                    *) echo 'Unexpected GitHub call.' >&2; return 97 ;;
                  esac
                }
                curl() { echo 200; }
                source "$FIXTURE_VERIFIER" "$FIXTURE_VERSION"
                """;
            using Process process = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "bash",
                    WorkingDirectory = temporaryRoot,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                },
            };
            process.StartInfo.ArgumentList.Add("-c");
            process.StartInfo.ArgumentList.Add(fixture.ReplaceLineEndings("\n"));
            var environment = process.StartInfo.Environment;
            environment["FIXTURE_VERIFIER"] = Path.Combine(repoRoot, "scripts/verify-partial-release.sh");
            environment["FIXTURE_RELEASE"] = releasePath;
            environment["FIXTURE_VERSION"] = version;
            environment["GITHUB_SHA"] = sourceSha;
            environment["GITHUB_REPOSITORY"] = "fixture/recovery";
            environment["HEXALITH_ZOT_USERNAME"] = "fixture-user";
            environment["HEXALITH_ZOT_API_KEY"] = "fixture-key";
            process.Start();
            Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> errorTask = process.StandardError.ReadToEndAsync();
            using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));
            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
                throw;
            }

            string output = await outputTask;
            string error = await errorTask;
            process.ExitCode.ShouldBe(accepted ? 0 : 1, output + error);
            if (accepted)
            {
                output.ShouldContain($"Verified release v{version} for source {sourceSha}.");
            }
            else
            {
                output.ShouldNotContain("Verified release");
                error.ShouldContain("Release prerelease classification does not match the reviewed version.");
            }
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }
}
