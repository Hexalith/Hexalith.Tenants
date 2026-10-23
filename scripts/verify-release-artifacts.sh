#!/usr/bin/env bash
# Read-only destination checks shared by normal publication and partial recovery.
set -euo pipefail

version="${1:-}"
repository="${GITHUB_REPOSITORY:-Hexalith/Hexalith.Tenants}"
source_sha="${GITHUB_SHA:-}"
manifest="${HEXALITH_RELEASE_PACKAGE_MANIFEST:-tools/release-packages.json}"
source "$(dirname "${BASH_SOURCE[0]}")/partial-release-recovery-common.sh"
# Revoke every previous attempt before version/receipt admission can fail. Refuse
# overlap before even removing an old marker, so reviewed inputs remain untouched.
receipt="${HEXALITH_RELEASE_OCI_RECEIPT:-}"
shopt -s nullglob
previous_attempts=(.hexalith/release-evidence/*/destinations)
if [ -n "${HEXALITH_RELEASE_DESTINATION_EVIDENCE_DIRECTORY:-}" ]; then
  previous_attempts+=("$HEXALITH_RELEASE_DESTINATION_EVIDENCE_DIRECTORY")
fi
admitted_attempts=()
for previous_attempt in "${previous_attempts[@]}"; do
  previous_attempt="$(recovery_confine_directory "$previous_attempt")"
  recovery_require_separate_evidence "$previous_attempt"
  admitted_attempts+=("$previous_attempt")
done
for previous_attempt in "${admitted_attempts[@]}"; do rm -f "$previous_attempt/verified.json"; done
shopt -u nullglob
[[ "$source_sha" =~ ^[0-9a-f]{40}$ ]] || recovery_fail 'Source SHA must be an exact lowercase commit SHA.'
[[ "$repository" =~ ^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$ ]] || recovery_fail 'Release repository is invalid.'
recovery_validate_manifest
receipt="${HEXALITH_RELEASE_OCI_RECEIPT:-}"
if [ -z "$receipt" ]; then
  shopt -s nullglob
  receipts=(.hexalith/release-evidence/*/tenants/oci-validation.json)
  shopt -u nullglob
  [ "${#receipts[@]}" -eq 1 ] || recovery_fail 'Exactly one publisher OCI receipt is required.'
  receipt="${receipts[0]}"
fi
recovery_confine_directory "$(dirname "$receipt")" >/dev/null
if [ -z "$version" ]; then
  # Select the version actually prepared by Semantic Release, never the latest remote release.
  version="$(python3 - "$manifest" <<'PY'
import importlib.util
from pathlib import Path
import sys
spec = importlib.util.spec_from_file_location("release_packages", Path("scripts/validate-nuget-packages.py"))
module = importlib.util.module_from_spec(spec)
sys.modules[spec.name] = module
spec.loader.exec_module(module)
inventory = module.load_json_object(Path(sys.argv[1]), "Release package manifest")
expected = {package["id"].casefold() for package in inventory["packages"]}
metadata = [module.get_metadata(path) for path in Path("nupkgs").glob("*.nupkg")]
if len(metadata) != 5 or {item.package_id.casefold() for item in metadata} != expected:
    raise ValueError("Reviewed package inventory does not match the five-package manifest")
versions = {item.version for item in metadata}
if len(versions) != 1:
    raise ValueError("Reviewed package versions disagree")
print(versions.pop())
PY
)"
fi
[[ "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z.-]+)?$ ]] || recovery_fail 'Version must be plain SemVer.'
evidence_directory="$(recovery_confine_directory "${HEXALITH_RELEASE_DESTINATION_EVIDENCE_DIRECTORY:-.hexalith/release-evidence/$version/destinations}")"
recovery_require_separate_evidence "$evidence_directory"
mkdir -p "$evidence_directory/nuget" "$evidence_directory/github"
rm -f "$evidence_directory/verified.json"
package_content_helper="$(dirname "${BASH_SOURCE[0]}")/release-package-content.py"
python3 "$package_content_helper" freeze "$manifest" "$receipt" "$evidence_directory/reviewed"
reviewed_inputs_hash="$(sha256sum "$evidence_directory/reviewed/inputs.json")"
manifest="$evidence_directory/reviewed/manifest.json"
receipt="$evidence_directory/reviewed/receipt.json"
release_reviewed_hashes_file="$evidence_directory/reviewed/reviewed-sha256"
expected_names="$(jq --arg version "$version" '[.packages[].id + "." + $version + ".nupkg"] | sort' "$manifest")"
actual_names="$(python3 - <<'PY'
import json
from pathlib import Path
print(json.dumps(sorted(path.name for path in Path("nupkgs").glob("*.nupkg"))))
PY
)"
[ "$(printf '%s' "$actual_names" | jq -c .)" = "$(printf '%s' "$expected_names" | jq -c .)" ] || recovery_fail 'Reviewed package file inventory differs from the release manifest.'
wait_seconds="${HEXALITH_RELEASE_NUGET_WAIT_SECONDS:-600}"
wait_interval="${HEXALITH_RELEASE_NUGET_WAIT_INTERVAL:-10}"
[[ "$wait_seconds" =~ ^[0-9]+$ && "$wait_interval" =~ ^[0-9]+$ ]] || recovery_fail 'NuGet readiness wait must use nonnegative seconds.'
[ "$wait_seconds" -le 1800 ] && [ "$wait_interval" -le 60 ] || recovery_fail 'NuGet readiness wait exceeds its bounded limits.'
: > "$evidence_directory/nuget-sha256"
: > "$evidence_directory/nuget-readiness"
while IFS= read -r package_id; do
  lower_id="$(printf '%s' "$package_id" | tr '[:upper:]' '[:lower:]')"
  downloaded="$evidence_directory/nuget/${package_id}.${version}.nupkg"
  deadline=$((SECONDS + wait_seconds))
  while true; do
    request_timeout=$((deadline - SECONDS))
    [ "$request_timeout" -gt 0 ] || request_timeout=1
    [ "$request_timeout" -le 30 ] || request_timeout=30
    status="$(curl --silent --show-error --location --max-time "$request_timeout" \
      --output "$downloaded" --write-out '%{http_code}' \
      "https://api.nuget.org/v3-flatcontainer/${lower_id}/${version}/${lower_id}.${version}.nupkg")" || status=transport-failure
    printf '%s %s\n' "$status" "$package_id" >> "$evidence_directory/nuget-readiness"
    [ "$status" != 200 ] || break
    case "$status" in
      404|409|425|429|5??|transport-failure) ;;
      *) recovery_fail "NuGet package $package_id returned unexpected status $status." ;;
    esac
    [ "$SECONDS" -lt "$deadline" ] || recovery_fail "NuGet package $package_id was not ready within ${wait_seconds} seconds."
    remaining=$((deadline - SECONDS))
    pause="$wait_interval"
    [ "$pause" -le "$remaining" ] || pause="$remaining"
    sleep "$pause"
  done
  actual_hash="$(sha256sum "$downloaded" | awk '{ print $1 }')"
  printf '%s %s\n' "$actual_hash" "$package_id" >> "$evidence_directory/nuget-sha256"
  python3 "$package_content_helper" compare "$evidence_directory/reviewed/${package_id}.${version}.nupkg" "$downloaded" ||
    recovery_fail "NuGet package $package_id does not match the reviewed package."
done < <(jq -r '.packages[].id' "$manifest")
recovery_verify_container "$evidence_directory/oci" "$receipt"
tag_json="$(gh api "repos/${repository}/git/ref/tags/v${version}")"
tag_sha="$(printf '%s' "$tag_json" | jq -er '.object.sha')"
tag_type="$(printf '%s' "$tag_json" | jq -er '.object.type')"
if [ "$tag_type" = tag ]; then
  tag_json="$(gh api "repos/${repository}/git/tags/${tag_sha}")"
  tag_type="$(printf '%s' "$tag_json" | jq -er '.object.type')"
  tag_sha="$(printf '%s' "$tag_json" | jq -er '.object.sha')"
fi
[ "$tag_type" = commit ] || recovery_fail 'Release tag has an unsupported object type.'
[ "$tag_sha" = "$source_sha" ] || recovery_fail 'Release tag does not resolve to the reviewed source.'
release_json="$(gh api "repos/${repository}/releases/tags/v${version}")"
printf '%s\n' "$release_json" > "$evidence_directory/github-release.json"
release_asset_evidence_directory="$evidence_directory"
: > "$evidence_directory/github-sha256"
recovery_check_release_assets "$release_json" true
[ "$(sha256sum "$evidence_directory/reviewed/inputs.json")" = "$reviewed_inputs_hash" ] || recovery_fail 'Frozen input hashes changed during destination verification.'
python3 "$package_content_helper" check "$evidence_directory/reviewed"
jq -n --arg version "$version" --arg source "$source_sha" --arg repository "$repository" \
  --arg digest "$(jq -r '.index_digest' "$evidence_directory/oci/oci-validation.json")" \
  '{result: "pass", version: $version, source_sha: $source, repository: $repository, index_digest: $digest}' > "$evidence_directory/verified.json"
echo "Verified release v${version} destination content for source ${source_sha}."
