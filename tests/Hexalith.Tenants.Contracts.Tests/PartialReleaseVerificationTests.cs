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
        string sourceSha = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
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
            using Process setup = new()
            {
                StartInfo = new ProcessStartInfo("python3")
                {
                    WorkingDirectory = temporaryRoot,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                },
            };
            setup.StartInfo.ArgumentList.Add(Path.Combine(repoRoot, "tests/scripts/recovery_execution_fixture.py"));
            setup.StartInfo.ArgumentList.Add(temporaryRoot);
            setup.Start();
            string setupOutput = await setup.StandardOutput.ReadToEndAsync();
            string setupError = await setup.StandardError.ReadToEndAsync();
            await setup.WaitForExitAsync();
            setup.ExitCode.ShouldBe(0, setupError);
            using JsonDocument execution = JsonDocument.Parse(setupOutput);
            sourceSha = execution.RootElement.GetProperty("source_sha").GetString()!;
            string buildsSha = execution.RootElement.GetProperty("builds_sha").GetString()!;
            string manifestPath = Path.Combine(repoRoot, "tools/release-packages.json");
            File.Copy(manifestPath, Path.Combine(temporaryRoot, "tools/release-packages.json"));
            using var manifest = JsonDocument.Parse(await File.ReadAllTextAsync(manifestPath));
            string[] assets = [.. manifest.RootElement.GetProperty("packages").EnumerateArray()
                .Select(package => $"{package.GetProperty("id").GetString()}.{version}.nupkg")];
            JsonObject release = new()
            {
                ["tag_name"] = $"v{version}",
                ["draft"] = false,
                ["assets"] = new JsonArray(assets.Select((name, index) => (JsonNode)new JsonObject { ["name"] = name, ["id"] = index + 1 }).ToArray()),
            };
            if (classification is not null)
            {
                release["prerelease"] = JsonNode.Parse(classification);
            }

            string stateDirectory = Path.Combine(temporaryRoot, "state");
            Directory.CreateDirectory(stateDirectory);
            File.Copy(execution.RootElement.GetProperty("provenance").GetString()!, Path.Combine(stateDirectory, "workflow-provenance.json"));
            File.Copy(execution.RootElement.GetProperty("identity").GetString()!, Path.Combine(stateDirectory, "execution-identity.json"));
            await File.WriteAllTextAsync(Path.Combine(stateDirectory, "source-gates.json"), "{\"result\":\"pass\"}\n");
            Directory.CreateDirectory(Path.Combine(temporaryRoot, "nupkgs"));
            await File.WriteAllTextAsync(Path.Combine(stateDirectory, "package-manifest-sha256"),
                Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(await File.ReadAllBytesAsync(manifestPath))) + "\n");
            foreach (string asset in assets)
            {
                await File.WriteAllTextAsync(Path.Combine(temporaryRoot, "nupkgs", asset), "reviewed fixture bytes\n");
            }

            string releasePath = Path.Combine(temporaryRoot, "release.json");
            await File.WriteAllTextAsync(releasePath, release.ToJsonString());
            const string fixture = """
                set -euo pipefail
                command python3 "$FIXTURE_REGISTRY" "$PWD" "${FIXTURE_VERSION:-1.2.3}"
                gh() {
                  case "$*" in
                    "api repos/fixture/recovery/git/ref/tags/v${FIXTURE_VERSION}") printf '{"object":{"sha":"%s","type":"commit"}}\n' "$GITHUB_SHA" ;;
                    "api repos/fixture/recovery/git/ref/tags/v${FIXTURE_VERSION} --jq .object.type") echo commit ;;
                    "api repos/fixture/recovery/releases/tags/v${FIXTURE_VERSION}") cat "$FIXTURE_RELEASE" ;;
                    'api repos/fixture/recovery/releases/assets/'*) printf 'reviewed fixture bytes\n' ;;
                    *) echo 'Unexpected GitHub call.'  >&2; return 97 ;;
                  esac
                }
                curl() {
                  while [ "$#" -gt 0 ]; do
                    if [ "$1" = --output ]; then printf 'reviewed fixture bytes\n' > "$2"; echo 200; return; fi
                    shift
                  done
                  return 97
                }
                version="$FIXTURE_VERSION"; repository="$GITHUB_REPOSITORY"; source_sha="$GITHUB_SHA"
                manifest="$FIXTURE_MANIFEST"; state_directory="$HEXALITH_RECOVERY_STATE_DIRECTORY"
                source "$FIXTURE_COMMON"
                printf '%s\n' "$GITHUB_SHA" > "$state_directory/source-sha"
                printf '%s\n' "$FIXTURE_VERSION" > "$state_directory/version"
                printf 'present\n' > "$state_directory/container-state"
                : > "$state_directory/missing-packages"
                : > "$state_directory/existing-packages"
                recovery_package_hashes > "$state_directory/prepared-packages-sha256"
                recovery_preparation_state > "$state_directory/prepared-state-sha256"
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
            environment["HEXALITH_RECOVERY_STATE_DIRECTORY"] = stateDirectory;
            environment["FIXTURE_VERIFIER"] = Path.Combine(repoRoot, "scripts/verify-partial-release.sh");
            environment["FIXTURE_RELEASE"] = releasePath;
            environment["FIXTURE_MANIFEST"] = Path.Combine(temporaryRoot, "tools/release-packages.json");
            environment["FIXTURE_COMMON"] = Path.Combine(repoRoot, "scripts/partial-release-recovery-common.sh");
            environment["HEXALITH_ZOT_REGISTRY"] = "fixture.invalid";
            environment["HEXALITH_RELEASE_PACKAGE_MANIFEST"] = Path.Combine(temporaryRoot, "tools/release-packages.json");
            environment["FIXTURE_VERSION"] = version;
            environment["GITHUB_SHA"] = sourceSha;
            environment["RECOVERY_FIXTURE"] = temporaryRoot;
            environment["FIXTURE_REGISTRY"] = Path.Combine(repoRoot, "tests/scripts/release_registry_fixture.py");
            environment["HEXALITH_BUILDS_EXECUTION_SHA"] = buildsSha;
            environment["PYTHONPATH"] = temporaryRoot + Path.PathSeparator + Path.Combine(repoRoot, "tests/scripts");
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
                output.ShouldContain($"Verified release v{version} destination content for source {sourceSha}.");
            }
            else
            {
                output.ShouldNotContain("Verified release");
                error.ShouldContain("Release metadata does not match the reviewed version.");
            }
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }
}
