using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Shouldly;

namespace Hexalith.Tenants.Contracts.Tests;

/// <summary>
/// Exercises recovery publication and its guards with isolated publication tools.
/// </summary>
public class PartialReleasePublicationTests
{
    /// <summary>
    /// A source that stays current can complete each supported publication inventory.
    /// </summary>
    /// <param name="inventory">The destinations requiring publication.</param>
    [Theory]
    [InlineData("package")]
    [InlineData("container")]
    [InlineData("release")]
    [InlineData("mixed")]
    [InlineData("all-missing")]
    public async Task UnchangedSourceCanPublishMissingArtifacts(string inventory)
    {
        await RunPublisherAsync(inventory, "stable");
    }

    /// <summary>
    /// Recovered previews are marked as prereleases while stable releases keep their classification.
    /// </summary>
    /// <param name="inventory">The destinations requiring publication.</param>
    /// <param name="version">The reviewed recovery version.</param>
    /// <param name="prerelease">Whether release creation must request prerelease classification.</param>
    [Theory]
    [InlineData("release", "1.2.3", false)]
    [InlineData("release", "1.2.3-alpha", true)]
    [InlineData("release", "1.2.3-rc.1", true)]
    [InlineData("release", "1.2.3-preview-build.7", true)]
    [InlineData("release", "1.2.3-0", true)]
    [InlineData("mixed", "1.2.3-rc.1", true)]
    [InlineData("all-missing", "1.2.3-rc.1", true)]
    [InlineData("package", "1.2.3-rc.1", true)]
    [InlineData("container", "1.2.3-rc.1", true)]
    public async Task RecoveryPreservesReleaseClassification(string inventory, string version, bool prerelease)
    {
        await RunPublisherAsync(inventory, "stable", version: version, prerelease: prerelease);
    }

    /// <summary>
    /// Main advancement during any preparation stage prevents every publication path.
    /// </summary>
    /// <param name="inventory">The destinations requiring publication.</param>
    /// <param name="stage">The preparation stage after which main advances.</param>
    [Theory]
    [InlineData("package", "build")]
    [InlineData("package", "pack")]
    [InlineData("package", "packages")]
    [InlineData("package", "consumers")]
    [InlineData("package", "hash")]
    [InlineData("container", "build")]
    [InlineData("container", "pack")]
    [InlineData("container", "packages")]
    [InlineData("container", "consumers")]
    [InlineData("container", "hash")]
    [InlineData("release", "build")]
    [InlineData("release", "pack")]
    [InlineData("release", "packages")]
    [InlineData("release", "consumers")]
    [InlineData("release", "hash")]
    [InlineData("mixed", "build")]
    [InlineData("mixed", "pack")]
    [InlineData("mixed", "packages")]
    [InlineData("mixed", "consumers")]
    [InlineData("mixed", "hash")]
    [InlineData("all-missing", "build")]
    [InlineData("all-missing", "pack")]
    [InlineData("all-missing", "packages")]
    [InlineData("all-missing", "consumers")]
    public async Task MainAdvancementDuringPreparationRefusesPublication(string inventory, string stage)
    {
        await RunPublisherAsync(inventory, "changed", stage);
    }

    /// <summary>
    /// A final main read must prove the exact source before any destination is changed.
    /// </summary>
    /// <param name="inventory">The destinations requiring publication.</param>
    /// <param name="response">The failed or unusable final main response.</param>
    [Theory]
    [InlineData("package", "unavailable")]
    [InlineData("package", "empty")]
    [InlineData("package", "malformed")]
    [InlineData("container", "unavailable")]
    [InlineData("container", "empty")]
    [InlineData("container", "malformed")]
    [InlineData("release", "unavailable")]
    [InlineData("release", "empty")]
    [InlineData("release", "malformed")]
    [InlineData("all-missing", "unavailable")]
    [InlineData("all-missing", "empty")]
    [InlineData("all-missing", "malformed")]
    public async Task UnprovenFinalSourceRefusesPublication(string inventory, string response)
    {
        await RunPublisherAsync(inventory, response);
    }

    /// <summary>
    /// Existing inventory and source refusals must still precede preparation or publication.
    /// </summary>
    /// <param name="response">The invalid initial evidence or mismatched package content.</param>
    [Theory]
    [InlineData("source-mismatch")]
    [InlineData("version-mismatch")]
    [InlineData("missing-inventory")]
    [InlineData("initially-stale")]
    [InlineData("hash-mismatch")]
    public async Task ExistingGuardsRemainFailClosed(string response)
    {
        await RunPublisherAsync("package", response);
    }

    private static async Task RunPublisherAsync(
        string inventory,
        string response,
        string stage = "hash",
        string version = "1.2.3",
        bool prerelease = false)
    {
        const string sourceSha = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        const string packageContent = "reviewed fixture package\n";
        string repoRoot = FindRepoRoot();
        string temporaryRoot = Path.Combine(Path.GetTempPath(), $"tenants-publication-{Guid.NewGuid():N}");
        string stateDirectory = Path.Combine(temporaryRoot, "state");
        string callLog = Path.Combine(temporaryRoot, "calls");
        string unexpectedLog = Path.Combine(temporaryRoot, "unexpected");
        Directory.CreateDirectory(stateDirectory);
        try
        {
            using var manifest = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(repoRoot, "tools/release-packages.json")));
            string[] ids = [.. manifest.RootElement.GetProperty("packages").EnumerateArray()
                .Select(package => package.GetProperty("id").GetString()!)];
            string[] missingIds = inventory switch
            {
                "package" => ids[1..],
                "mixed" => ids[1..],
                "all-missing" => ids,
                _ => [],
            };
            string[] existingIds = [.. ids.Except(missingIds, StringComparer.Ordinal)];
            string hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(packageContent)));
            Dictionary<string, string> originalState = new(StringComparer.Ordinal)
            {
                ["source-sha"] = response == "source-mismatch" ? new string('b', 40) + "\n" : sourceSha + "\n",
                ["version"] = response == "version-mismatch" ? "9.9.9\n" : version + "\n",
                ["missing-packages"] = missingIds.Length == 0 ? string.Empty : string.Join('\n', missingIds) + "\n",
                ["existing-packages"] = existingIds.Length == 0 ? string.Empty : string.Join('\n', existingIds) + "\n",
                ["existing-package-sha256"] = string.Concat(existingIds.Select(id => $"{hash} {id}\n")),
                ["container-state"] = inventory is "container" or "mixed" or "all-missing" ? "absent\n" : "present\n",
            };
            if (response == "missing-inventory")
            {
                originalState.Remove("missing-packages");
            }

            foreach (var file in originalState)
            {
                await File.WriteAllTextAsync(Path.Combine(stateDirectory, file.Key), file.Value);
            }

            Directory.CreateDirectory(Path.Combine(temporaryRoot, "nupkgs"));
            foreach (string id in ids)
            {
                string content = response == "hash-mismatch" && id == existingIds[0] ? "different package\n" : packageContent;
                await File.WriteAllTextAsync(Path.Combine(temporaryRoot, "nupkgs", $"{id}.{version}.nupkg"), content);
            }

            await File.WriteAllTextAsync(callLog, string.Empty);
            await File.WriteAllTextAsync(unexpectedLog, string.Empty);
            const string fixture = """
                set -euo pipefail
                unexpected() { printf '%s\n' "$*" >> "$FIXTURE_UNEXPECTED"; return 97; }
                prepare() {
                  printf '%s\n' "$1" >> "$FIXTURE_CALLS"
                  if [ "$FIXTURE_STAGE" = "$1" ]; then touch "$FIXTURE_PREPARED"; fi
                }
                gh() {
                  case "$*" in
                    'api repos/fixture/recovery/git/ref/heads/main --jq .object.sha')
                      echo main >> "$FIXTURE_CALLS"
                      if [ "$FIXTURE_RESPONSE" = initially-stale ]; then printf '%040d\n' 1; return; fi
                      if [ ! -f "$FIXTURE_PREPARED" ]; then echo "$FIXTURE_SHA"; return; fi
                      case "$FIXTURE_RESPONSE" in
                        stable) echo "$FIXTURE_SHA" ;;
                        changed) printf '%040d\n' 1 ;;
                        unavailable) echo 'Main API unavailable.' >&2; return 7 ;;
                        empty) : ;;
                        malformed) echo unproven ;;
                        *) unexpected "response $FIXTURE_RESPONSE" ;;
                      esac ;;
                    "api --include repos/fixture/recovery/releases/tags/v${FIXTURE_VERSION}")
                      echo release-read >> "$FIXTURE_CALLS"
                      if [ "$FIXTURE_INVENTORY" = release ] || [ "$FIXTURE_INVENTORY" = mixed ] || [ "$FIXTURE_INVENTORY" = all-missing ]; then
                        printf 'HTTP/2.0 404 Not Found\n'; return 1
                      fi
                      printf 'HTTP/2.0 200 OK\n' ;;
                    'release create '*)
                      mapfile -t expected_arguments < "$FIXTURE_RELEASE_ARGUMENTS"
                      [ "$#" = "${#expected_arguments[@]}" ] || { unexpected "gh argument count $*"; return 97; }
                      for expected_argument in "${expected_arguments[@]}"; do
                        [ "$1" = "$expected_argument" ] || { unexpected "gh argument $1"; return 97; }
                        shift
                      done
                      echo release-publish >> "$FIXTURE_CALLS" ;;
                    *) unexpected "gh $*" ;;
                  esac
                }
                dotnet() {
                  case "$*" in
                    "build Hexalith.Tenants.slnx --configuration Release -p:Version=${FIXTURE_VERSION}") prepare build ;;
                    'nuget push '*)
                      [ "$#" = 7 ] && [ "$4 $5 $6 $7" = '--source https://api.nuget.org/v3/index.json --api-key fixture-key' ] || { unexpected "dotnet $*"; return 97; }
                      case "|$FIXTURE_MISSING_FILES|" in
                        *"|$3|"*) printf 'package-publish %s\n' "$3" >> "$FIXTURE_CALLS" ;;
                        *) unexpected "dotnet $*" ;;
                      esac ;;
                    'publish '*)
                      [ "$*" = "$FIXTURE_CONTAINER_COMMAND" ] || { unexpected "dotnet $*"; return 97; }
                      echo container-publish >> "$FIXTURE_CALLS" ;;
                    *) unexpected "dotnet $*" ;;
                  esac
                }
                python3() {
                  case "$*" in
                    "scripts/pack-release-packages.py ./nupkgs ${FIXTURE_VERSION}") prepare pack ;;
                    'scripts/validate-nuget-packages.py ./nupkgs') prepare packages ;;
                    'scripts/validate-consumer-package-references.py ./nupkgs') prepare consumers ;;
                    *) unexpected "python3 $*" ;;
                  esac
                }
                sha256sum() {
                  printf 'hash %s\n' "$1" >> "$FIXTURE_CALLS"
                  if [ "$FIXTURE_STAGE" = hash ] && [ "$1" = "$FIXTURE_LAST_EXISTING_FILE" ]; then touch "$FIXTURE_PREPARED"; fi
                  command sha256sum "$@"
                }
                docker() {
                  case "$*" in
                    'login fixture.invalid --username fixture-user --password-stdin')
                      cat >/dev/null
                      echo container-login >> "$FIXTURE_CALLS" ;;
                    *) unexpected "docker $*" ;;
                  esac
                }
                source "$FIXTURE_PUBLISHER" "$FIXTURE_VERSION"
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
            environment["FIXTURE_PUBLISHER"] = Path.Combine(repoRoot, "scripts/publish-partial-release.sh");
            environment["FIXTURE_SHA"] = sourceSha;
            environment["FIXTURE_VERSION"] = version;
            environment["FIXTURE_STAGE"] = existingIds.Length == 0 && stage == "hash" ? "consumers" : stage;
            environment["FIXTURE_RESPONSE"] = response;
            environment["FIXTURE_INVENTORY"] = inventory;
            environment["FIXTURE_CALLS"] = callLog;
            environment["FIXTURE_UNEXPECTED"] = unexpectedLog;
            environment["FIXTURE_PREPARED"] = Path.Combine(temporaryRoot, "prepared");
            environment["FIXTURE_LAST_EXISTING_FILE"] = existingIds.Length == 0 ? string.Empty : $"nupkgs/{existingIds[^1]}.{version}.nupkg";
            environment["FIXTURE_MISSING_FILES"] = string.Join('|', missingIds.Select(id => $"nupkgs/{id}.{version}.nupkg"));
            List<string> releaseArguments = ["release", "create", $"v{version}", "--target", sourceSha, "--title", $"v{version}", "--generate-notes"];
            if (prerelease)
            {
                releaseArguments.Add("--prerelease");
            }

            releaseArguments.AddRange(ids.Order(StringComparer.Ordinal).Select(id => $"nupkgs/{id}.{version}.nupkg"));
            environment["FIXTURE_RELEASE_ARGUMENTS"] = Path.Combine(temporaryRoot, "release-arguments");
            await File.WriteAllTextAsync(environment["FIXTURE_RELEASE_ARGUMENTS"]!, string.Join('\n', releaseArguments) + "\n");
            environment["FIXTURE_CONTAINER_COMMAND"] = "publish src/Hexalith.Tenants/Hexalith.Tenants.csproj --configuration Release /t:PublishContainer "
                + "-p:RuntimeIdentifiers=linux-musl-x64;linux-musl-arm64 -p:ContainerRuntimeIdentifiers=linux-musl-x64;linux-musl-arm64 "
                + "-p:ContainerImageFormat=OCI -p:UseHexalithProjectReferences=false -p:ContainerRegistry=fixture.invalid "
                + $"-p:ContainerRepository=tenants -p:ContainerImageTag={version} -p:Version={version}";
            environment["GITHUB_REPOSITORY"] = "fixture/recovery";
            environment["GITHUB_SHA"] = sourceSha;
            environment["HEXALITH_RECOVERY_STATE_DIRECTORY"] = stateDirectory;
            environment["HEXALITH_ZOT_REGISTRY"] = "fixture.invalid";
            environment["HEXALITH_ZOT_USERNAME"] = "fixture-user";
            environment["HEXALITH_ZOT_API_KEY"] = "fixture-key";
            environment["HEXALITH_RECOVERY_CONTAINER_REPOSITORY"] = "tenants";
            environment["NUGET_API_KEY"] = "fixture-key";
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

            string diagnostics = await output + await error;
            (await File.ReadAllTextAsync(unexpectedLog)).ShouldBeEmpty(diagnostics);
            string[] calls = await File.ReadAllLinesAsync(callLog);
            List<string> expected = ["main", "build", "pack", "packages", "consumers"];
            expected.AddRange(existingIds.Select(id => $"hash nupkgs/{id}.{version}.nupkg"));
            expected.Add("main");
            string? refusal = response switch
            {
                "source-mismatch" => "Source changed during recovery.",
                "version-mismatch" => "Recovery version changed.",
                "missing-inventory" => "Missing-package state is absent.",
                "initially-stale" => "Live main changed before publication.",
                "hash-mismatch" => $"Existing package {existingIds[0]} does not match the reviewed source.",
                _ => null,
            };
            if (response is "source-mismatch" or "version-mismatch" or "missing-inventory")
            {
                expected.Clear();
            }
            else if (response == "initially-stale")
            {
                expected = ["main"];
            }
            else if (response == "hash-mismatch")
            {
                expected.RemoveAt(expected.Count - 1);
            }

            if (response == "stable")
            {
                expected.AddRange(missingIds.Select(id => $"package-publish nupkgs/{id}.{version}.nupkg"));
                if (inventory is "container" or "mixed" or "all-missing")
                {
                    expected.AddRange(["container-login", "container-publish"]);
                }

                expected.Add("release-read");
                if (inventory is "release" or "mixed" or "all-missing")
                {
                    expected.Add("release-publish");
                }
            }

            calls.ShouldBe(expected.ToArray(), customMessage: diagnostics);
            process.ExitCode.ShouldBe(response == "stable" ? 0 : response == "unavailable" ? 7 : 1, diagnostics);
            if (refusal is not null)
            {
                diagnostics.ShouldContain(refusal);
            }
            if (response is "changed" or "empty" or "malformed")
            {
                diagnostics.ShouldContain("Live main changed before publication.");
            }

            foreach (var file in originalState)
            {
                (await File.ReadAllTextAsync(Path.Combine(stateDirectory, file.Key))).ShouldBe(file.Value);
            }

            Directory.GetFiles(stateDirectory).Select(Path.GetFileName).Order(StringComparer.Ordinal)
                .ShouldBe(originalState.Keys.Order(StringComparer.Ordinal));
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    private static string FindRepoRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "Hexalith.Tenants.slnx")))
        {
            current = current.Parent;
        }

        return current?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
