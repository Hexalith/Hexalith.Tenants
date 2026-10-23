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
    [InlineData("package-existing")]
    [InlineData("container")]
    [InlineData("release")]
    [InlineData("mixed")]
    [InlineData("mixed-existing")]
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

    /// <summary>
    /// Different bytes at any existing-package position stop every recovery destination.
    /// </summary>
    /// <param name="inventory">The destinations requiring publication.</param>
    /// <param name="mismatchIndex">The existing-package position with different bytes.</param>
    [Theory]
    [InlineData("package", 0)]
    [InlineData("package-existing", 0)]
    [InlineData("package-existing", 1)]
    [InlineData("package-existing", 2)]
    [InlineData("package-existing", 3)]
    [InlineData("container", 0)]
    [InlineData("container", 1)]
    [InlineData("container", 2)]
    [InlineData("container", 3)]
    [InlineData("container", 4)]
    [InlineData("release", 0)]
    [InlineData("release", 1)]
    [InlineData("release", 2)]
    [InlineData("release", 3)]
    [InlineData("release", 4)]
    [InlineData("mixed-existing", 0)]
    [InlineData("mixed-existing", 1)]
    [InlineData("mixed-existing", 2)]
    [InlineData("mixed-existing", 3)]
    public async Task ExistingPackageHashMismatchPreventsEveryPublication(string inventory, int mismatchIndex)
    {
        await RunPublisherAsync(inventory, "hash-mismatch", mismatchIndex: mismatchIndex);
    }

    /// <summary>
    /// Every existing package needs reviewed hash evidence before any recovery publication.
    /// </summary>
    /// <param name="inventory">The destinations requiring publication.</param>
    /// <param name="missingHashIndex">The existing-package position without hash evidence.</param>
    [Theory]
    [InlineData("package", 0)]
    [InlineData("package-existing", 0)]
    [InlineData("package-existing", 1)]
    [InlineData("package-existing", 2)]
    [InlineData("package-existing", 3)]
    [InlineData("container", 0)]
    [InlineData("container", 1)]
    [InlineData("container", 2)]
    [InlineData("container", 3)]
    [InlineData("container", 4)]
    [InlineData("release", 0)]
    [InlineData("release", 1)]
    [InlineData("release", 2)]
    [InlineData("release", 3)]
    [InlineData("release", 4)]
    [InlineData("mixed-existing", 0)]
    [InlineData("mixed-existing", 1)]
    [InlineData("mixed-existing", 2)]
    [InlineData("mixed-existing", 3)]
    public async Task MissingExpectedHashPreventsEveryPublication(string inventory, int missingHashIndex)
    {
        await RunPublisherAsync(inventory, "hash-record-absent", mismatchIndex: missingHashIndex);
    }

    /// <summary>
    /// Missing or unusable evidence files cannot authorize recovery publication.
    /// </summary>
    /// <param name="inventory">The destinations requiring publication.</param>
    /// <param name="response">The missing or unusable hash evidence.</param>
    [Theory]
    [InlineData("package-existing", "hash-file-empty")]
    [InlineData("package-existing", "hash-file-whitespace")]
    [InlineData("package-existing", "hash-wrong-id")]
    [InlineData("package-existing", "hash-record-blank")]
    [InlineData("package-existing", "hash-file-absent")]
    [InlineData("container", "hash-file-empty")]
    [InlineData("container", "hash-file-whitespace")]
    [InlineData("container", "hash-wrong-id")]
    [InlineData("container", "hash-record-blank")]
    [InlineData("container", "hash-file-absent")]
    [InlineData("release", "hash-file-empty")]
    [InlineData("release", "hash-file-whitespace")]
    [InlineData("release", "hash-wrong-id")]
    [InlineData("release", "hash-record-blank")]
    [InlineData("release", "hash-file-absent")]
    [InlineData("mixed-existing", "hash-file-empty")]
    [InlineData("mixed-existing", "hash-file-whitespace")]
    [InlineData("mixed-existing", "hash-wrong-id")]
    [InlineData("mixed-existing", "hash-record-blank")]
    [InlineData("mixed-existing", "hash-file-absent")]
    public async Task UnusableExpectedHashesPreventEveryPublication(string inventory, string response)
    {
        await RunPublisherAsync(inventory, response);
    }

    /// <summary>
    /// Recovery without existing packages does not need a hash evidence file.
    /// </summary>
    [Fact]
    public async Task NoExistingPackagesCanPublishWithoutHashFile()
    {
        await RunPublisherAsync("all-missing", "stable", omitHashFile: true);
    }

    /// <summary>
    /// A final existing package without a newline still needs reviewed hash evidence.
    /// </summary>
    /// <param name="inventory">The destinations requiring publication.</param>
    /// <param name="response">The missing hash evidence for the final existing package.</param>
    [Theory]
    [InlineData("package", "hash-record-absent")]
    [InlineData("mixed", "hash-record-absent")]
    [InlineData("package-existing", "hash-record-absent")]
    [InlineData("container", "hash-record-absent")]
    [InlineData("release", "hash-record-absent")]
    [InlineData("mixed-existing", "hash-record-absent")]
    [InlineData("package", "hash-file-absent")]
    [InlineData("mixed", "hash-file-absent")]
    public async Task UnterminatedFinalPackageWithoutReviewedHashPreventsEveryPublication(string inventory, string response)
    {
        await RunPublisherAsync(inventory, response, terminateExistingInventory: false);
    }

    /// <summary>
    /// Different bytes for the final unterminated package stop every recovery destination.
    /// </summary>
    /// <param name="inventory">The destinations requiring publication.</param>
    [Theory]
    [InlineData("package")]
    [InlineData("mixed")]
    [InlineData("package-existing")]
    [InlineData("container")]
    [InlineData("release")]
    [InlineData("mixed-existing")]
    public async Task UnterminatedFinalPackageHashMismatchPreventsEveryPublication(string inventory)
    {
        await RunPublisherAsync(inventory, "hash-mismatch", terminateExistingInventory: false);
    }

    /// <summary>
    /// Matching hashes allow publication after every unterminated inventory entry is checked.
    /// </summary>
    /// <param name="inventory">The destinations requiring publication.</param>
    [Theory]
    [InlineData("package")]
    [InlineData("mixed")]
    [InlineData("package-existing")]
    [InlineData("container")]
    [InlineData("release")]
    [InlineData("mixed-existing")]
    public async Task UnterminatedInventoryWithMatchingHashesCanPublish(string inventory)
    {
        await RunPublisherAsync(inventory, "stable", terminateExistingInventory: false);
    }

    /// <summary>
    /// Main advancement during the final unterminated hash still stops every publication.
    /// </summary>
    /// <param name="inventory">The destinations requiring publication.</param>
    [Theory]
    [InlineData("package")]
    [InlineData("mixed")]
    [InlineData("package-existing")]
    [InlineData("container")]
    [InlineData("release")]
    [InlineData("mixed-existing")]
    public async Task MainAdvancementDuringUnterminatedFinalHashRefusesPublication(string inventory)
    {
        await RunPublisherAsync(inventory, "changed", stage: "hash", terminateExistingInventory: false);
    }

    private static async Task RunPublisherAsync(
        string inventory,
        string response,
        string stage = "hash",
        string version = "1.2.3",
        bool prerelease = false,
        int mismatchIndex = 0,
        bool omitHashFile = false,
        bool terminateExistingInventory = true)
    {
        ArgumentNullException.ThrowIfNull(response);
        const string sourceSha = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
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
                "package-existing" => ids[^1..],
                "mixed" => ids[1..],
                "mixed-existing" => ids[^1..],
                "all-missing" => ids,
                _ => [],
            };
            string[] existingIds = [.. ids.Except(missingIds, StringComparer.Ordinal)];
            if (response is "hash-wrong-id" or "hash-record-blank" || (!terminateExistingInventory && existingIds.Length > 0))
            {
                mismatchIndex = existingIds.Length - 1;
            }

            Dictionary<string, string> packageContents = ids.ToDictionary(id => id, id => $"reviewed fixture package {id}\n", StringComparer.Ordinal);
            string manifestPath = Path.Combine(repoRoot, "tools/release-packages.json");
            Dictionary<string, string> originalState = new(StringComparer.Ordinal)
            {
                ["source-sha"] = response == "source-mismatch" ? new string('b', 40) + "\n" : sourceSha + "\n",
                ["version"] = response == "version-mismatch" ? "9.9.9\n" : version + "\n",
                ["missing-packages"] = missingIds.Length == 0 ? string.Empty : string.Join('\n', missingIds) + "\n",
                ["existing-packages"] = existingIds.Length == 0 ? string.Empty : string.Join('\n', existingIds) + (terminateExistingInventory ? "\n" : string.Empty),
                ["existing-package-sha256"] = string.Concat(existingIds.Reverse().Select(id =>
                    $"{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(packageContents[id])))} {id}\n")),
                ["package-manifest-sha256"] = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(manifestPath))) + "\n",
                ["container-state"] = inventory is "container" or "mixed" or "mixed-existing" or "all-missing" ? "absent\n" : "present\n",
            };
            if (response == "missing-inventory")
            {
                originalState.Remove("missing-packages");
            }

            if (response == "hash-file-absent" || omitHashFile)
            {
                originalState.Remove("existing-package-sha256");
            }
            else if (response.StartsWith("hash-", StringComparison.Ordinal) && response != "hash-mismatch")
            {
                string selectedId = existingIds[mismatchIndex];
                string[] hashRecords = originalState["existing-package-sha256"].Split('\n', StringSplitOptions.RemoveEmptyEntries);
                originalState["existing-package-sha256"] = response switch
                {
                    "hash-file-empty" => string.Empty,
                    "hash-file-whitespace" => " \t\n\n",
                    "hash-wrong-id" => originalState["existing-package-sha256"].Replace($" {selectedId}\n", " Unrelated.Package\n", StringComparison.Ordinal),
                    "hash-record-blank" => string.Concat(hashRecords.Select(line => line.EndsWith($" {selectedId}", StringComparison.Ordinal)
                        ? $" {selectedId}\n" : line + "\n")),
                    "hash-record-absent" => string.Concat(hashRecords
                        .Where(line => !line.EndsWith($" {selectedId}", StringComparison.Ordinal))
                        .Select(line => line + "\n")),
                    _ => throw new ArgumentOutOfRangeException(nameof(response)),
                };
            }

            foreach (var file in originalState)
            {
                await File.WriteAllTextAsync(Path.Combine(stateDirectory, file.Key), file.Value);
            }

            foreach (string id in existingIds)
            {
                await File.WriteAllTextAsync(Path.Combine(stateDirectory, $"{id}.{version}.nupkg"), packageContents[id]);
            }

            Directory.CreateDirectory(Path.Combine(temporaryRoot, "nupkgs"));
            foreach (string id in ids)
            {
                string content = response == "hash-mismatch" && id == existingIds[mismatchIndex] ? "different package\n" : packageContents[id];
                await File.WriteAllTextAsync(Path.Combine(temporaryRoot, "nupkgs", $"{id}.{version}.nupkg"), content);
            }

            await File.WriteAllTextAsync(callLog, string.Empty);
            await File.WriteAllTextAsync(unexpectedLog, string.Empty);
            const string fixture = """
                set -euo pipefail
                command python3 "$FIXTURE_REGISTRY" "$PWD" "${FIXTURE_VERSION:-1.2.3}"
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
                      if [ "$FIXTURE_INVENTORY" = release ] || [ "$FIXTURE_INVENTORY" = mixed ] || [ "$FIXTURE_INVENTORY" = mixed-existing ] || [ "$FIXTURE_INVENTORY" = all-missing ]; then
                        printf 'HTTP/2.0 404 Not Found\n'; return 1
                      fi
                      printf 'HTTP/2.0 200 OK\n' ;;
                    "api repos/fixture/recovery/releases/tags/v${FIXTURE_VERSION}")
                      printf '{"tag_name":"v%s","draft":false,"prerelease":%s,"assets":[]}\n' "$FIXTURE_VERSION" "$FIXTURE_PRERELEASE" ;;
                    'release upload '*) : ;;
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
                date() { printf '2026-10-23T00:00:00Z\n'; }
                curl() { [ "$(cat "$HEXALITH_RECOVERY_STATE_DIRECTORY/container-state")" = present ] && echo 200 || echo 404; }
                python3() {
                  case "$*" in
                    *oci_registry_validator.py*|*release-package-content.py*) command python3 "$@" ;;
                    "${FIXTURE_PACKER} --manifest ${FIXTURE_MANIFEST} --validate-only") command python3 "$@" ;;
                    "scripts/pack-release-packages.py ./nupkgs ${FIXTURE_VERSION} --manifest ${FIXTURE_MANIFEST}") prepare pack ;;
                    "scripts/validate-nuget-packages.py ./nupkgs --manifest ${FIXTURE_MANIFEST}") prepare packages ;;
                    'scripts/validate-consumer-package-references.py ./nupkgs') prepare consumers ;;
                    *) unexpected "python3 $*" ;;
                  esac
                }
                sha256sum() {
                  if [[ "$1" == nupkgs/* ]]; then printf 'hash %s\n' "$1" >> "$FIXTURE_CALLS"; fi
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
                unset NUGET_API_KEY
                source "$FIXTURE_PREPARER" "$FIXTURE_VERSION"
                export NUGET_API_KEY=fixture-key
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
            environment["FIXTURE_PACKER"] = Path.Combine(repoRoot, "scripts/pack-release-packages.py");
            environment["FIXTURE_MANIFEST"] = manifestPath;
            environment["HEXALITH_RELEASE_PACKAGE_MANIFEST"] = manifestPath;
            environment["FIXTURE_PREPARER"] = Path.Combine(repoRoot, "scripts/prepare-partial-release.sh");
            environment["FIXTURE_PUBLISHER"] = Path.Combine(repoRoot, "scripts/publish-partial-release.sh");
            environment["FIXTURE_SHA"] = sourceSha;
            environment["FIXTURE_VERSION"] = version;
            environment["FIXTURE_PRERELEASE"] = prerelease ? "true" : "false";
            environment["FIXTURE_STAGE"] = existingIds.Length == 0 && stage == "hash" ? "consumers" : stage;
            environment["FIXTURE_RESPONSE"] = response.StartsWith("hash-", StringComparison.Ordinal) ? "stable" : response;
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
                + $"-p:ContainerRepository=tenants -p:ContainerImageTag={version} -p:Version={version} "
                + $"-p:ContainerProvenanceSourceSha={sourceSha} -p:ContainerProvenanceReleaseVersion={version} -p:ContainerProvenanceCreated=2026-10-23T00:00:00Z";
            environment["RECOVERY_FIXTURE"] = temporaryRoot;
            environment["FIXTURE_REGISTRY"] = Path.Combine(repoRoot, "tests/scripts/release_registry_fixture.py");
            environment["HEXALITH_RELEASE_OCI_VALIDATOR"] = Path.Combine(repoRoot, "references/Hexalith.Builds/Github/publish-containers/oci_registry_validator.py");
            environment["PYTHONPATH"] = temporaryRoot + Path.PathSeparator + Path.Combine(repoRoot, "tests/scripts");
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
            expected.AddRange(ids.Select(id => $"hash nupkgs/{id}.{version}.nupkg"));
            expected.AddRange(ids.Select(id => $"hash nupkgs/{id}.{version}.nupkg"));
            expected.Add("main");
            if (response == "stable")
            {
                expected.Add("release-read");
                expected.Add("main");
                expected.AddRange(ids.Select(id => $"hash nupkgs/{id}.{version}.nupkg"));
            }
            string? refusal = response switch
            {
                "source-mismatch" => "Source changed during recovery.",
                "version-mismatch" => "Recovery version changed.",
                "missing-inventory" => "Missing-package state is absent.",
                "initially-stale" => "Live main changed before publication.",
                "hash-mismatch" => $"Existing package {existingIds[mismatchIndex]} does not match the reviewed source.",
                "hash-record-absent" or "hash-file-empty" or "hash-file-whitespace" or "hash-wrong-id" or "hash-record-blank"
                    => $"Existing package {existingIds[mismatchIndex]} has no reviewed hash.",
                "hash-file-absent" => "existing-package-sha256",
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
                expected = ["main", "build", "pack", "packages", "consumers"];
                expected.AddRange(existingIds.Take(mismatchIndex + 1).Select(id => $"hash nupkgs/{id}.{version}.nupkg"));
            }
            else if (response.StartsWith("hash-", StringComparison.Ordinal))
            {
                expected = ["main", "build", "pack", "packages", "consumers"];
                expected.AddRange(existingIds.Take(mismatchIndex).Select(id => $"hash nupkgs/{id}.{version}.nupkg"));
            }

            if (response == "stable")
            {
                expected.Add("main");
                expected.AddRange(ids.Select(id => $"hash nupkgs/{id}.{version}.nupkg"));
                expected.AddRange(missingIds.Select(id => $"package-publish nupkgs/{id}.{version}.nupkg"));
                if (inventory is "container" or "mixed" or "mixed-existing" or "all-missing")
                {
                    expected.AddRange(["container-login", "container-publish"]);
                }

                if (inventory is "release" or "mixed" or "mixed-existing" or "all-missing")
                {
                    expected.Add("release-publish");
                }
            }

            calls.ShouldBe(expected.ToArray(), customMessage: diagnostics);
            if (response == "hash-file-absent")
            {
                process.ExitCode.ShouldNotBe(0, diagnostics);
            }
            else
            {
                process.ExitCode.ShouldBe(response == "stable" ? 0 : response == "unavailable" ? 7 : 1, diagnostics);
            }
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

            string[] preparedFiles = response is "stable" or "changed" or "empty" or "malformed" or "unavailable"
                ? ["prepared-packages-sha256", "prepared-state-sha256"] : [];
            Directory.GetFiles(stateDirectory).Select(Path.GetFileName).Order(StringComparer.Ordinal)
                .ShouldBe(originalState.Keys.Concat(preparedFiles).Concat(existingIds.Select(id => $"{id}.{version}.nupkg")).Order(StringComparer.Ordinal));
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
