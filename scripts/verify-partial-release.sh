#!/usr/bin/env bash
set -euo pipefail

version="${1:?Release version is required}"
repository="${GITHUB_REPOSITORY:-Hexalith/Hexalith.Tenants}"
source_sha="${GITHUB_SHA:?GITHUB_SHA is required}"
registry="${HEXALITH_ZOT_REGISTRY:-registry.hexalith.com}"
container_repository="${HEXALITH_RECOVERY_CONTAINER_REPOSITORY:-tenants}"

for package_id in $(jq -r '.packages[].id' tools/release-packages.json); do
  lower_id="$(printf '%s' "$package_id" | tr '[:upper:]' '[:lower:]')"
  status="$(curl --silent --show-error --output /dev/null --write-out '%{http_code}' "https://api.nuget.org/v3-flatcontainer/${lower_id}/${version}/${lower_id}.${version}.nupkg")"
  [ "$status" = 200 ] || { echo "[partial-release-recovery] Package verification failed." >&2; exit 1; }
done
container_status="$(curl --silent --show-error --output /dev/null --write-out '%{http_code}' \
  --user "${HEXALITH_ZOT_USERNAME:?}:${HEXALITH_ZOT_API_KEY:?}" \
  -H 'Accept: application/vnd.oci.image.manifest.v1+json' "https://${registry}/v2/${container_repository}/manifests/${version}")"
[ "$container_status" = 200 ] || { echo "[partial-release-recovery] Container verification failed." >&2; exit 1; }
tag_sha="$(gh api "repos/${repository}/git/ref/tags/v${version}" --jq '.object.sha')"
case "$tag_sha" in "$source_sha") ;; *) echo '[partial-release-recovery] Tag does not resolve to the reviewed source.' >&2; exit 1 ;; esac
gh release view "v${version}" --json tagName,isDraft --jq 'select(.tagName == "v'"$version"'" and .isDraft == false)' >/dev/null
echo "Verified release v${version} for source ${source_sha}."
