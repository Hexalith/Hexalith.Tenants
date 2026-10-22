#!/usr/bin/env bash
set -euo pipefail

version="${1:?Release version is required}"
repository="${GITHUB_REPOSITORY:-Hexalith/Hexalith.Tenants}"
source_sha="${GITHUB_SHA:?GITHUB_SHA is required}"
registry="${HEXALITH_ZOT_REGISTRY:-registry.hexalith.com}"
container_repository="${HEXALITH_RECOVERY_CONTAINER_REPOSITORY:-tenants}"
manifest="${HEXALITH_RELEASE_PACKAGE_MANIFEST:-tools/release-packages.json}"
state_directory="${HEXALITH_RECOVERY_STATE_DIRECTORY:-.hexalith/recovery}"
source "$(dirname "${BASH_SOURCE[0]}")/partial-release-recovery-common.sh"
recovery_require_manifest

for package_id in $(jq -r '.packages[].id' "$manifest"); do
  lower_id="$(printf '%s' "$package_id" | tr '[:upper:]' '[:lower:]')"
  status="$(curl --silent --show-error --output /dev/null --write-out '%{http_code}' "https://api.nuget.org/v3-flatcontainer/${lower_id}/${version}/${lower_id}.${version}.nupkg")"
  [ "$status" = 200 ] || { echo "[partial-release-recovery] Package verification failed." >&2; exit 1; }
done
container_status="$(curl --silent --show-error --output /dev/null --write-out '%{http_code}' \
  --user "${HEXALITH_ZOT_USERNAME:?}:${HEXALITH_ZOT_API_KEY:?}" \
  -H 'Accept: application/vnd.oci.image.manifest.v1+json' "https://${registry}/v2/${container_repository}/manifests/${version}")"
[ "$container_status" = 200 ] || { echo "[partial-release-recovery] Container verification failed." >&2; exit 1; }
tag_sha="$(gh api "repos/${repository}/git/ref/tags/v${version}" --jq '.object.sha')"
tag_type="$(gh api "repos/${repository}/git/ref/tags/v${version}" --jq '.object.type')"
if [ "$tag_type" = tag ]; then
  tag_sha="$(gh api "repos/${repository}/git/tags/${tag_sha}" --jq '.object.sha')"
fi
case "$tag_type" in commit|tag) ;; *) echo '[partial-release-recovery] Tag has an unsupported object type.' >&2; exit 1 ;; esac
case "$tag_sha" in "$source_sha") ;; *) echo '[partial-release-recovery] Tag does not resolve to the reviewed source.' >&2; exit 1 ;; esac
release_json="$(gh api "repos/${repository}/releases/tags/v${version}")"
printf '%s' "$release_json" | jq -e --arg tag "v${version}" '(.tag_name == $tag) and (.draft == false)' >/dev/null
expected_prerelease=false
if [[ "$version" == *-* ]]; then
  expected_prerelease=true
fi
printf '%s' "$release_json" | jq -e --argjson expected "$expected_prerelease" '.prerelease == $expected' >/dev/null || {
  echo '[partial-release-recovery] Release prerelease classification does not match the reviewed version.' >&2
  exit 1
}
recovery_check_release_assets "$release_json" true
recovery_require_manifest
echo "Verified release v${version} for source ${source_sha}."
