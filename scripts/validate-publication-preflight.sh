#!/usr/bin/env bash
set -euo pipefail

version="${1:-}"
phase="${2:-}"
builds_execution_sha="${HEXALITH_BUILDS_EXECUTION_SHA:-}"
registry="${HEXALITH_ZOT_REGISTRY:-registry.hexalith.com}"
source_sha="${GITHUB_SHA:-}"
source_branch="${HEXALITH_RELEASE_SOURCE_BRANCH:-}"
source_ci_workflow="${HEXALITH_RELEASE_SOURCE_CI_WORKFLOW:-}"
package_manifest="${HEXALITH_RELEASE_PACKAGE_MANIFEST:-}"
release_environment="${HEXALITH_RELEASE_ENVIRONMENT:-}"
contract_directory="${HEXALITH_RELEASE_CONTRACT_DIRECTORY:-$PWD/.hexalith/release}"
publication_preflight="${HEXALITH_PUBLICATION_PREFLIGHT:-./.hexalith/release/publication_preflight.py}"
evidence_directory="${HEXALITH_RELEASE_EVIDENCE_DIRECTORY:-$PWD/.hexalith/release-evidence/$version/preflight}"

# Hexalith.Tenants publishes exactly these five NuGet packages. The count is declared
# here rather than counted from the manifest so that adding or dropping a package fails
# closed until the change is reviewed alongside tools/release-packages.json.
expected_package_count=5

# No version at or below 3.15.1 can be released. Contracts, Server and Aspire already
# occupy 3.3.0 through 3.15.1, published from this repository in May 2026; Client and
# Testing reach 3.2.18. All five ship one shared version, so one occupied ID is enough to
# block the release. Tags v3.2.0 through v3.15.1 were deleted afterwards, which is how
# semantic-release came to resume inside that range and propose 3.3.0 in run 30291329462.
# 4.x is the authoritative line. Declaring the floor here, beside the package count, turns
# a version line that slips back below it into an actionable local failure instead of a
# NuGet destination collision. Raise it only alongside a deliberate version-line decision,
# and update the "Release Version Line" section of CONTRIBUTING.md with it.
minimum_release_version=4.0.0

fail() {
  echo "[publication-preflight] $1" >&2
  exit 1
}

[[ "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z.-]+)?$ ]] ||
  fail "A plain semantic release version is required."
# Compare release cores so the ordering does not depend on how a given sort implementation
# ranks prerelease suffixes. Assign before comparing: an inline command substitution inside
# [[ ]] discards the pipeline status, so a sort that is missing or errors would produce an
# empty string and be misreported as a below-floor version instead of aborting.
version_core="${version%%-*}"
lowest_version="$(printf '%s\n%s\n' "$minimum_release_version" "$version_core" | sort -V | head -n 1)"
[[ "$lowest_version" = "$minimum_release_version" ]] ||
  fail "Version $version is below the $minimum_release_version release floor. Everything at or below 3.15.1 is published and immutable. Either the tag line lost its release history, in which case restore the deleted tags so semantic-release resumes above the floor, or the analyzed commits do not justify the major bump out of the 3.x line, in which case land the change with a BREAKING CHANGE footer. Never lower the floor to make a release pass."
[[ "$phase" =~ ^(verify|publish)$ ]] ||
  fail "Publication phase must be verify or publish."
[[ "$builds_execution_sha" =~ ^[0-9a-f]{40}$ ]] ||
  fail "HEXALITH_BUILDS_EXECUTION_SHA must be an exact lowercase commit SHA."
[[ "$source_sha" =~ ^[0-9a-f]{40}$ ]] ||
  fail "GITHUB_SHA must identify the exact workflow source commit."
[[ "$source_branch" = "main" ]] ||
  fail "HEXALITH_RELEASE_SOURCE_BRANCH must be exactly main."
[[ "$source_ci_workflow" = "ci.yml" ]] ||
  fail "HEXALITH_RELEASE_SOURCE_CI_WORKFLOW must be exactly ci.yml."
[[ "$package_manifest" = "tools/release-packages.json" ]] ||
  fail "HEXALITH_RELEASE_PACKAGE_MANIFEST must identify the authoritative manifest."
[[ "$release_environment" = "production" ]] ||
  fail "HEXALITH_RELEASE_ENVIRONMENT must identify the protected production environment."
[[ "$registry" = "registry.hexalith.com" ]] ||
  fail "The Tenants container registry must be registry.hexalith.com."
# Use ${VAR-} (unset only) rather than ${VAR:-} so a set-but-empty value is compared and rejected
# instead of silently substituting the local default and passing the check vacuously.
[[ "${HEXALITH_RELEASE_EXPECTED_PACKAGE_COUNT-}" = "$expected_package_count" ]] ||
  fail "The workflow expected-package-count input must be exactly $expected_package_count."
[[ -x "$publication_preflight" ]] ||
  fail "The shared publication preflight is unavailable."
[[ -f "$package_manifest" ]] ||
  fail "The authoritative release package manifest is unavailable."

exec "$publication_preflight" \
  --repository "Hexalith/Hexalith.Tenants" \
  --version "$version" \
  --source-sha "$source_sha" \
  --source-branch "$source_branch" \
  --source-ci-workflow "$source_ci_workflow" \
  --container-repository "registry.hexalith.com/tenants" \
  --builds-execution-sha "$builds_execution_sha" \
  --environment-name "$release_environment" \
  --package-manifest "$package_manifest" \
  --expected-package-count "$expected_package_count" \
  --contract-directory "$contract_directory" \
  --evidence-directory "$evidence_directory" \
  --phase "$phase"
