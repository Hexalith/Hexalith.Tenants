using System.Diagnostics;
using System.Text.Json;

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
    [InlineData("existing-artifacts")]
    public async Task ConsistentInventoryCanPrepareRecovery(string scenario)
    {
        var result = await RunRecoveryAsync(scenario, expectInventory: true);

        result.ExitCode.ShouldBe(0, result.Error);
        result.Output.ShouldContain(scenario == "existing-artifacts"
            ? "Validated recovery with 1 existing and 4 missing packages."
            : "Validated recovery with 0 existing and 5 missing packages.");
    }

    /// <summary>
    /// Existing source, CI, tag and release refusals still precede inventory preparation.
    /// </summary>
    /// <param name="scenario">The invalid evidence supplied by the isolated API fixture.</param>
    /// <param name="diagnostic">The existing refusal that must remain observable.</param>
    [Theory]
    [InlineData("invalid-source", "Source SHA must be an exact lowercase commit SHA.")]
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
        string unexpectedLog = Path.Combine(temporaryRoot, "unexpected-calls");
        string manifest = Path.Combine(repoRoot, "tools/release-packages.json");
        try
        {
            await File.WriteAllTextAsync(callLog, string.Empty);
            await File.WriteAllTextAsync(unexpectedLog, string.Empty);
            using var inventory = JsonDocument.Parse(await File.ReadAllTextAsync(manifest));
            string[] ids = [.. inventory.RootElement.GetProperty("packages").EnumerateArray()
                .Select(package => package.GetProperty("id").GetString()!)];
            Dictionary<string, string> previousState = new(StringComparer.Ordinal)
            {
                ["source-sha"] = "previous source\n",
                ["version"] = "0.1.0\n",
                ["missing-packages"] = "previous inventory\n",
                ["existing-packages"] = "previous package\n",
                ["existing-package-sha256"] = "previous hash\n",
                ["existing-package-count"] = "1\n",
                ["package-state"] = "partial\n",
                ["container-state"] = "present\n",
            };
            if (existingState)
            {
                Directory.CreateDirectory(stateDirectory);
                foreach (var file in previousState)
                {
                    await File.WriteAllTextAsync(Path.Combine(stateDirectory, file.Key), file.Value);
                }
            }

            // Functions shadow every external API client; unexpected requests fail locally.
            const string fixture = """
                set -euo pipefail
                command python3 "$FIXTURE_REGISTRY" "$PWD" "${FIXTURE_VERSION:-1.2.3}" no-receipt
                gh() {
                  printf '%s\n' "$*" >> "$FIXTURE_CALL_LOG"
                  case "$*" in
                    'api repos/fixture/recovery/git/ref/heads/main --jq .object.sha')
                      if [ "$FIXTURE_SCENARIO" = stale-source ]; then echo wrong-source; else echo "$FIXTURE_SHA"; fi ;;
                    "api --method GET repos/fixture/recovery/actions/workflows/ci.yml/runs -f branch=main -f event=push -f head_sha=$FIXTURE_SHA -f status=success -f per_page=100 --jq "*)
                      printf '%s\n' "$FIXTURE_CI_RUNS" | jq -er "${!#}" ;;
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
                        absent-release|annotated-absent-release|both-absent|existing-artifacts) printf 'HTTP/2.0 404 Not Found\n'; return 1 ;;
                        unknown-release) printf 'HTTP/2.0 503 Unavailable\n'; return 1 ;;
                        *) printf 'HTTP/2.0 200 OK\n' ;;
                      esac ;;
                    'api repos/fixture/recovery/releases/tags/v1.2.3')
                      case "$FIXTURE_SCENARIO" in
                        draft-release) printf '{"draft":true,"tag_name":"v1.2.3"}\n' ;;
                        wrong-release-tag) printf '{"draft":false,"tag_name":"v9.9.9"}\n' ;;
                        *) printf '{"draft":false,"tag_name":"v1.2.3"}\n' ;;
                      esac ;;
                    *) printf 'gh %s\n' "$*" >> "$FIXTURE_UNEXPECTED_LOG"; echo "Unexpected gh request: $*" >&2; return 97 ;;
                  esac
                }
                curl() {
                  local url='' output_file=''
                  while [ "$#" -gt 0 ]; do
                    case "$1" in
                      --output) output_file="$2"; shift 2 ;;
                      https://*) url="$1"; shift ;;
                      *) shift ;;
                    esac
                  done
                  printf 'curl %s\n' "$url" >> "$FIXTURE_CALL_LOG"
                  case "$url" in
                    "https://api.nuget.org/v3-flatcontainer/$FIXTURE_EXISTING_ID/1.2.3/$FIXTURE_EXISTING_ID.1.2.3.nupkg")
                      if [ "$FIXTURE_SCENARIO" = existing-artifacts ]; then
                        if [ "$output_file" = /dev/null ]; then printf 200; else printf 'existing fixture package\n' > "$output_file"; fi
                      else printf 404; fi ;;
                    https://api.nuget.org/v3-flatcontainer/*/1.2.3/*.1.2.3.nupkg) printf 404 ;;
                    https://fixture.invalid/v2/tenants/manifests/1.2.3)
                      if [ "$FIXTURE_SCENARIO" = existing-artifacts ] || [ "$FIXTURE_SCENARIO" = both-absent ]; then printf 200; else printf 404; fi ;;
                    *) printf 'curl %s\n' "$url" >> "$FIXTURE_UNEXPECTED_LOG"; echo "Unexpected curl request: $url" >&2; return 97 ;;
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
            environment["FIXTURE_SHA"] = scenario == "invalid-source" ? "main" : sourceSha;
            environment["FIXTURE_CALL_LOG"] = callLog;
            environment["FIXTURE_UNEXPECTED_LOG"] = unexpectedLog;
            environment["FIXTURE_EXISTING_ID"] = ids[0].ToLowerInvariant();
            string[] ciCases = ["valid", "wrong-sha", "wrong-branch", "wrong-event", "running", "failed"];
            environment["FIXTURE_CI_RUNS"] = JsonSerializer.Serialize(new
            {
                workflow_runs = ciCases.Where(item => scenario != "no-ci" || item != "valid").Select(item => new
                {
                    head_sha = item == "wrong-sha" ? "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb" : sourceSha,
                    head_branch = item == "wrong-branch" ? "feature" : "main",
                    @event = item == "wrong-event" ? "pull_request" : "push",
                    status = item == "running" ? "in_progress" : "completed",
                    conclusion = item == "failed" ? "failure" : "success",
                }),
            });
            environment["FIXTURE_VALIDATOR"] = Path.Combine(repoRoot, "scripts/validate-partial-release-recovery.sh");
            environment["RECOVERY_FIXTURE"] = temporaryRoot;
            environment["FIXTURE_REGISTRY"] = Path.Combine(repoRoot, "tests/scripts/release_registry_fixture.py");
            environment["HEXALITH_RELEASE_OCI_VALIDATOR"] = Path.Combine(repoRoot, "references/Hexalith.Builds/Github/publish-containers/oci_registry_validator.py");
            environment["PYTHONPATH"] = temporaryRoot + Path.PathSeparator + Path.Combine(repoRoot, "tests/scripts");
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
            (await File.ReadAllTextAsync(unexpectedLog)).ShouldBeEmpty(stdout + stderr);
            stderr.ShouldNotContain("Unexpected", customMessage: stdout + stderr);
            string[] calls = await File.ReadAllLinesAsync(callLog);
            if (scenario == "invalid-source")
            {
                calls.ShouldBeEmpty();
            }
            string[] destinationReads = [.. calls.Where(call => call.StartsWith("curl ", StringComparison.Ordinal))];
            if (expectInventory)
            {
                bool hasExistingArtifacts = scenario == "existing-artifacts";
                List<string> expectedReads = [];
                foreach (string id in ids)
                {
                    string read = $"curl https://api.nuget.org/v3-flatcontainer/{id.ToLowerInvariant()}/1.2.3/{id.ToLowerInvariant()}.1.2.3.nupkg";
                    expectedReads.Add(read);
                    if (hasExistingArtifacts && id == ids[0])
                    {
                        expectedReads.Add(read);
                    }
                }

                expectedReads.Add("curl https://fixture.invalid/v2/tenants/manifests/1.2.3");
                destinationReads.ShouldBe(expectedReads);
                (await File.ReadAllLinesAsync(Path.Combine(stateDirectory, "missing-packages")))
                    .ShouldBe(hasExistingArtifacts ? ids[1..] : ids);
                (await File.ReadAllTextAsync(Path.Combine(stateDirectory, "source-sha"))).ShouldBe(sourceSha + "\n");
                (await File.ReadAllTextAsync(Path.Combine(stateDirectory, "version"))).ShouldBe("1.2.3\n");
                (await File.ReadAllTextAsync(Path.Combine(stateDirectory, "package-state"))).ShouldBe("partial\n");
                (await File.ReadAllTextAsync(Path.Combine(stateDirectory, "container-state")))
                    .ShouldBe(hasExistingArtifacts || scenario == "both-absent" ? "present\n" : "absent\n");
                (await File.ReadAllTextAsync(Path.Combine(stateDirectory, "existing-package-count")))
                    .ShouldBe(hasExistingArtifacts ? "1\n" : "0\n");
                (await File.ReadAllLinesAsync(Path.Combine(stateDirectory, "existing-packages")))
                    .ShouldBe(hasExistingArtifacts ? [ids[0]] : []);
                string expectedHash = string.Empty;
                if (hasExistingArtifacts)
                {
                    const string packageContent = "existing fixture package\n";
                    (await File.ReadAllTextAsync(Path.Combine(stateDirectory, $"{ids[0]}.1.2.3.nupkg"))).ShouldBe(packageContent);
                    expectedHash = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(
                        System.Text.Encoding.UTF8.GetBytes(packageContent))) + $" {ids[0]}\n";
                }

                (await File.ReadAllTextAsync(Path.Combine(stateDirectory, "existing-package-sha256"))).ShouldBe(expectedHash);
            }
            else
            {
                destinationReads.ShouldBeEmpty(stdout + stderr);
                Directory.Exists(stateDirectory).ShouldBe(existingState, stdout + stderr);
                if (existingState)
                {
                    Directory.GetFileSystemEntries(stateDirectory).Order(StringComparer.Ordinal)
                        .ShouldBe(previousState.Keys.Select(name => Path.Combine(stateDirectory, name)).Order(StringComparer.Ordinal));
                    foreach (var file in previousState)
                    {
                        (await File.ReadAllTextAsync(Path.Combine(stateDirectory, file.Key))).ShouldBe(file.Value);
                    }
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
