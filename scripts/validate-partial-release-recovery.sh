#!/usr/bin/env bash
set -euo pipefail

version="${1:-}"
source_sha="${2:-${GITHUB_SHA:-}}"
repository="${GITHUB_REPOSITORY:-Hexalith/Hexalith.Tenants}"
source_branch="${HEXALITH_RELEASE_SOURCE_BRANCH:-main}"
source_ci_workflow="${HEXALITH_RELEASE_SOURCE_CI_WORKFLOW:-ci.yml}"
builds_sha="${HEXALITH_BUILDS_EXECUTION_SHA:-}"
environment_name="${HEXALITH_RELEASE_ENVIRONMENT:-}"
manifest="${HEXALITH_RELEASE_PACKAGE_MANIFEST:-tools/release-packages.json}"
state_directory="${HEXALITH_RECOVERY_STATE_DIRECTORY:-.hexalith/recovery}"
registry="${HEXALITH_ZOT_REGISTRY:-registry.hexalith.com}"
container_repository="${HEXALITH_RECOVERY_CONTAINER_REPOSITORY:-tenants}"

fail() { echo "[partial-release-recovery] $1" >&2; exit 1; }

[[ "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z.-]+)?$ ]] || fail "Version must be plain SemVer."
[[ "$source_sha" =~ ^[0-9a-f]{40}$ ]] || fail "Source SHA must be an exact lowercase commit SHA."
[[ "$source_branch" = main ]] || fail "Recovery source branch must be main."
[[ "$source_ci_workflow" = ci.yml ]] || fail "Recovery source CI workflow must be ci.yml."
[[ "$builds_sha" =~ ^[0-9a-f]{40}$ ]] || fail "Approved Builds execution SHA is required."
[[ "$environment_name" = production ]] || fail "Recovery requires the production environment."
[ -f "$manifest" ] || fail "The authoritative package manifest is missing."

live_sha="$(gh api "repos/${repository}/git/ref/heads/main" --jq '.object.sha')"
[ "$live_sha" = "$source_sha" ] || fail "The requested source is not the live main tip."
ci_count="$(gh api --method GET "repos/${repository}/actions/workflows/${source_ci_workflow}/runs" \
  -f branch=main -f event=push -f head_sha="$source_sha" -f status=success -f per_page=100 \
  --jq '[.workflow_runs[] | select(.head_sha == "'"$source_sha"'" and .conclusion == "success")] | length')"
[ "$ci_count" -gt 0 ] || fail "No successful push CI run exists for the exact source SHA."

mapfile -t package_ids < <(jq -r '.packages[] | .id' "$manifest")
[ "${#package_ids[@]}" -eq 5 ] || fail "The package manifest must contain exactly five packages."
mkdir -p "$state_directory"
: > "$state_directory/missing-packages"
existing=0
for package_id in "${package_ids[@]}"; do
  lower_id="$(printf '%s' "$package_id" | tr '[:upper:]' '[:lower:]')"
  status="$(curl --silent --show-error --output /dev/null --write-out '%{http_code}' \
    "https://api.nuget.org/v3-flatcontainer/${lower_id}/${version}/${lower_id}.${version}.nupkg")"
  case "$status" in
    200) existing=$((existing + 1)) ;;
    404) printf '%s\n' "$package_id" >> "$state_directory/missing-packages" ;;
    *) fail "NuGet destination status for a package could not be proved." ;;
  esac
done
container_status="$(curl --silent --show-error --output /dev/null --write-out '%{http_code}' \
  --user "${HEXALITH_ZOT_USERNAME:?}:${HEXALITH_ZOT_API_KEY:?}" \
  -H 'Accept: application/vnd.oci.image.manifest.v1+json' \
  "https://${registry}/v2/${container_repository}/manifests/${version}" || true)"
printf '%s\n' "$source_sha" > "$state_directory/source-sha"
printf '%s\n' "$version" > "$state_directory/version"
printf '%s\n' "$existing" > "$state_directory/existing-package-count"
printf '%s\n' "$([ "$existing" -eq 5 ] && echo complete || echo partial)" > "$state_directory/package-state"
printf '%s\n' "$([ "$container_status" = 200 ] && echo present || echo absent)" > "$state_directory/container-state"
if [ "$container_status" != 404 ] && [ "$container_status" != 200 ]; then
  fail "The container destination state could not be proved."
fi
echo "Validated recovery with ${existing} existing and $((5 - existing)) missing packages."
