using System.Diagnostics;

using Shouldly;

namespace Hexalith.Tenants.Contracts.Tests;

/// <summary>
/// Exercises recovery inventory admission through the production shell validator.
/// </summary>
public class PartialReleaseRecoveryTests
{
    /// <summary>
    /// A release without its tag must fail before destination reads or recovery-state writes.
    /// </summary>
    /// <param name="existingState">Whether a previous recovery left state on disk.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExistingReleaseWithoutTagIsRejectedBeforePreparingRecovery(bool existingState)
    {
        var result = await RunRecoveryAsync("missing-tag", existingState: existingState);

        result.ExitCode.ShouldBe(1, result.Output + result.Error);
        result.Error.ShouldContain("The existing GitHub Release has no matching release tag.");
    }

    /// <summary>
    /// Consistent inventories retain the existing recovery paths and persisted inventory.
    /// </summary>
    /// <param name="scenario">The supported tag and release inventory.</param>
    [Theory]
    [InlineData("lightweight")]
    [InlineData("annotated")]
    [InlineData("absent-release")]
    [InlineData("annotated-absent-release")]
    [InlineData("both-absent")]
    public async Task ConsistentInventoryCanPrepareRecovery(string scenario)
    {
        var result = await RunRecoveryAsync(scenario, expectInventory: true);

        result.ExitCode.ShouldBe(0, result.Error);
        result.Output.ShouldContain("Validated recovery with 0 existing and 5 missing packages.");
    }

    /// <summary>
    /// Existing source, CI, tag and release refusals still precede inventory preparation.
    /// </summary>
    /// <param name="scenario">The invalid evidence supplied by the isolated API fixture.</param>
    /// <param name="diagnostic">The existing refusal that must remain observable.</param>
    [Theory]
    [InlineData("stale-source", "The requested source is not the live main tip.")]
    [InlineData("no-ci", "No successful push CI run exists for the exact source SHA.")]
    [InlineData("unknown-tag", "The release tag state could not be proved.")]
    [InlineData("unknown-release", "The GitHub Release state could not be proved.")]
    [InlineData("wrong-source", "The existing release tag resolves to a different source.")]
    [InlineData("wrong-annotated-source", "The existing release tag resolves to a different source.")]
    [InlineData("unsupported-tag", "The existing release tag has an unsupported object type.")]
    [InlineData("draft-release", "The existing GitHub Release is a draft.")]
    [InlineData("wrong-release-tag", "The existing GitHub Release tag is inconsistent.")]
    public async Task ExistingEvidenceGuardsRemainFailClosed(string scenario, string diagnostic)
    {
        var result = await RunRecoveryAsync(scenario);

        result.ExitCode.ShouldBe(1, result.Output + result.Error);
        result.Error.ShouldContain(diagnostic);
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunRecoveryAsync(
        string scenario,
        bool expectInventory = false,
        bool existingState = false)
    {
        const string sourceSha = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        string repoRoot = FindRepoRoot();
        string temporaryRoot = Path.Combine(Path.GetTempPath(), $"tenants-recovery-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryRoot);
        string stateDirectory = Path.Combine(temporaryRoot, "state");
        string callLog = Path.Combine(temporaryRoot, "calls");
        string manifest = Path.Combine(repoRoot, "tools/release-packages.json");
        try
        {
            if (existingState)
            {
                Directory.CreateDirectory(stateDirectory);
                await File.WriteAllTextAsync(Path.Combine(stateDirectory, "missing-packages"), "previous inventory\n");
            }

            // Functions shadow every external API client; unexpected requests fail locally.
            const string fixture = """
                set -euo pipefail
                gh() {
                  printf '%s\n' "$*" >> "$FIXTURE_CALL_LOG"
                  case "$*" in
                    'api repos/fixture/recovery/git/ref/heads/main --jq .object.sha')
                      if [ "$FIXTURE_SCENARIO" = stale-source ]; then echo wrong-source; else echo "$FIXTURE_SHA"; fi ;;
                    "api --method GET repos/fixture/recovery/actions/workflows/ci.yml/runs -f branch=main -f event=push -f head_sha=$FIXTURE_SHA -f status=success -f per_page=100 --jq "*)
                      if [ "$FIXTURE_SCENARIO" = no-ci ]; then echo 0; else echo 1; fi ;;
                    'api --include repos/fixture/recovery/git/ref/tags/v1.2.3')
                      case "$FIXTURE_SCENARIO" in
                        missing-tag|both-absent) printf 'HTTP/2.0 404 Not Found\n'; return 1 ;;
                        unknown-tag) printf 'HTTP/2.0 503 Unavailable\n'; return 1 ;;
                        *) printf 'HTTP/2.0 200 OK\n' ;;
                      esac ;;
                    'api repos/fixture/recovery/git/ref/tags/v1.2.3')
                      case "$FIXTURE_SCENARIO" in
                        annotated|annotated-absent-release|wrong-annotated-source) printf '{"object":{"type":"tag","sha":"fixture-annotation"}}\n' ;;
                        wrong-source) printf '{"object":{"type":"commit","sha":"wrong-source"}}\n' ;;
                        unsupported-tag) printf '{"object":{"type":"tree","sha":"%s"}}\n' "$FIXTURE_SHA" ;;
                        *) printf '{"object":{"type":"commit","sha":"%s"}}\n' "$FIXTURE_SHA" ;;
                      esac ;;
                    'api repos/fixture/recovery/git/tags/fixture-annotation --jq .object.sha')
                      if [ "$FIXTURE_SCENARIO" = wrong-annotated-source ]; then echo wrong-source; else echo "$FIXTURE_SHA"; fi ;;
                    'api --include repos/fixture/recovery/releases/tags/v1.2.3')
                      case "$FIXTURE_SCENARIO" in
                        absent-release|annotated-absent-release|both-absent) printf 'HTTP/2.0 404 Not Found\n'; return 1 ;;
                        unknown-release) printf 'HTTP/2.0 503 Unavailable\n'; return 1 ;;
                        *) printf 'HTTP/2.0 200 OK\n' ;;
                      esac ;;
                    'api repos/fixture/recovery/releases/tags/v1.2.3')
                      case "$FIXTURE_SCENARIO" in
                        draft-release) printf '{"draft":true,"tag_name":"v1.2.3"}\n' ;;
                        wrong-release-tag) printf '{"draft":false,"tag_name":"v9.9.9"}\n' ;;
                        *) printf '{"draft":false,"tag_name":"v1.2.3"}\n' ;;
                      esac ;;
                    *) echo "Unexpected gh request: $*" >&2; return 97 ;;
                  esac
                }
                curl() {
                  printf 'curl %s\n' "${!#}" >> "$FIXTURE_CALL_LOG"
                  case "${!#}" in
                    https://api.nuget.org/v3-flatcontainer/*/1.2.3/*.1.2.3.nupkg|https://fixture.invalid/v2/tenants/manifests/1.2.3)
                      printf 404 ;;
                    *) echo "Unexpected curl request: $*" >&2; return 97 ;;
                  esac
                }
                source "$FIXTURE_VALIDATOR" 1.2.3 "$FIXTURE_SHA"
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
            environment["FIXTURE_SCENARIO"] = scenario;
            environment["FIXTURE_SHA"] = sourceSha;
            environment["FIXTURE_CALL_LOG"] = callLog;
            environment["FIXTURE_VALIDATOR"] = Path.Combine(repoRoot, "scripts/validate-partial-release-recovery.sh");
            environment["GITHUB_REPOSITORY"] = "fixture/recovery";
            environment["HEXALITH_RELEASE_SOURCE_BRANCH"] = "main";
            environment["HEXALITH_RELEASE_SOURCE_CI_WORKFLOW"] = "ci.yml";
            environment["HEXALITH_BUILDS_EXECUTION_SHA"] = sourceSha;
            environment["HEXALITH_RELEASE_ENVIRONMENT"] = "production";
            environment["HEXALITH_RELEASE_PACKAGE_MANIFEST"] = manifest;
            environment["HEXALITH_RECOVERY_STATE_DIRECTORY"] = stateDirectory;
            environment["HEXALITH_ZOT_REGISTRY"] = "fixture.invalid";
            environment["HEXALITH_RECOVERY_CONTAINER_REPOSITORY"] = "tenants";
            environment["HEXALITH_ZOT_USERNAME"] = "fixture-user";
            environment["HEXALITH_ZOT_API_KEY"] = "fixture-key";
            environment["TMPDIR"] = temporaryRoot;

            process.Start();
            Task<string> output = process.StandardOutput.ReadToEndAsync();
            Task<string> error = process.StandardError.ReadToEndAsync();
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

            string stdout = await output;
            string stderr = await error;
            stderr.ShouldNotContain("Unexpected", stdout + stderr);
            string[] calls = await File.ReadAllLinesAsync(callLog);
            string[] destinationReads = [.. calls.Where(call => call.StartsWith("curl ", StringComparison.Ordinal))];
            if (expectInventory)
            {
                using var inventory = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(manifest));
                string[] ids = [.. inventory.RootElement.GetProperty("packages").EnumerateArray()
                    .Select(package => package.GetProperty("id").GetString()!)];
                destinationReads.ShouldBe([.. ids.Select(id =>
                    $"curl https://api.nuget.org/v3-flatcontainer/{id.ToLowerInvariant()}/1.2.3/{id.ToLowerInvariant()}.1.2.3.nupkg"),
                    "curl https://fixture.invalid/v2/tenants/manifests/1.2.3"]);
                (await File.ReadAllLinesAsync(Path.Combine(stateDirectory, "missing-packages"))).ShouldBe(ids);
                (await File.ReadAllTextAsync(Path.Combine(stateDirectory, "source-sha"))).ShouldBe(sourceSha + "\n");
                (await File.ReadAllTextAsync(Path.Combine(stateDirectory, "version"))).ShouldBe("1.2.3\n");
                (await File.ReadAllTextAsync(Path.Combine(stateDirectory, "package-state"))).ShouldBe("partial\n");
                (await File.ReadAllTextAsync(Path.Combine(stateDirectory, "container-state"))).ShouldBe("absent\n");
                (await File.ReadAllTextAsync(Path.Combine(stateDirectory, "existing-packages"))).ShouldBeEmpty();
            }
            else
            {
                destinationReads.ShouldBeEmpty(stdout + stderr);
                Directory.Exists(stateDirectory).ShouldBe(existingState, stdout + stderr);
                if (existingState)
                {
                    Directory.GetFiles(stateDirectory).ShouldBe([Path.Combine(stateDirectory, "missing-packages")]);
                    (await File.ReadAllTextAsync(Path.Combine(stateDirectory, "missing-packages"))).ShouldBe("previous inventory\n");
                }
            }

            return (process.ExitCode, stdout, stderr);
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    private static string FindRepoRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Hexalith.Tenants.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate Hexalith.Tenants repository root.");
    }
}
