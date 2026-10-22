#!/usr/bin/env bash
# Internal functions shared by the three partial-release recovery steps.

recovery_fail() { echo "[partial-release-recovery] $1" >&2; exit 1; }

recovery_validate_manifest() {
  jq -e '
    type == "object" and (.packages | type == "array" and length == 5) and
    all(.packages[];
      (.id | type == "string" and test("^[A-Za-z0-9_][A-Za-z0-9_.-]*$")) and
      (.project | type == "string" and endswith(".csproj"))) and
    ([.packages[].id | ascii_downcase] | unique | length == 5) and
    ([.packages[].project | ascii_downcase] | unique | length == 5)
  ' "$manifest" >/dev/null || recovery_fail 'The package manifest must contain exactly five packages with unique valid IDs and projects.'
  python3 "$(dirname "${BASH_SOURCE[0]}")/pack-release-packages.py" --manifest "$manifest" --validate-only
}

recovery_require_manifest() {
  recovery_validate_manifest
  local expected_hash actual_hash
  expected_hash="$(cat "$state_directory/package-manifest-sha256")"
  actual_hash="$(sha256sum "$manifest" | awk '{ print $1 }')"
  [ "$actual_hash" = "$expected_hash" ] || recovery_fail 'The package manifest changed after recovery validation.'
}

# All metadata is checked before any download, and all downloads before any upload.
# Asset IDs select the authenticated API; asset-provided URLs are never followed.
recovery_check_release_assets() {
  local release_json="$1" require_complete="$2" expected_names asset_id asset_name
  local downloaded expected_hash actual_hash package_file expected_prerelease=false
  release_missing_assets=()
  if [[ "$version" == *-* ]]; then expected_prerelease=true; fi
  printf '%s' "$release_json" | jq -e --arg tag "v${version}" --argjson prerelease "$expected_prerelease" '
    .tag_name == $tag and .draft == false and .prerelease == $prerelease
  ' >/dev/null || recovery_fail 'Release metadata does not match the reviewed version.'
  expected_names="$(jq --arg version "$version" '[.packages[].id + "." + $version + ".nupkg"]' "$manifest")"
  printf '%s' "$release_json" | jq -e --argjson expected "$expected_names" '
    (.assets | type == "array") and
    all(.assets[];
      (.id | type == "number" and . > 0 and . == floor) and
      (.name as $name | $expected | index($name) != null)) and
    ([.assets[].name] | length == (unique | length)) and
    ([.assets[].id] | length == (unique | length))
  ' >/dev/null || recovery_fail 'Release asset inventory is unexpected, duplicated or unverifiable.'
  if [ "$require_complete" = true ]; then
    [ "$(printf '%s' "$release_json" | jq '.assets | length')" = 5 ] || recovery_fail 'Release asset count is not five.'
  fi
  while IFS=$'\t' read -r asset_id asset_name; do
    package_file="nupkgs/$asset_name"
    [ -f "$package_file" ] || recovery_fail "Missing reviewed release asset $asset_name."
    downloaded="$(mktemp)"
    if ! gh api "repos/${repository}/releases/assets/${asset_id}" -H 'Accept: application/octet-stream' > "$downloaded"; then
      rm -f "$downloaded"
      recovery_fail "Release asset $asset_name could not be downloaded."
    fi
    expected_hash="$(sha256sum "$package_file" | awk '{ print $1 }')"
    actual_hash="$(sha256sum "$downloaded" | awk '{ print $1 }')"
    rm -f "$downloaded"
    [ "$actual_hash" = "$expected_hash" ] || recovery_fail "Release asset $asset_name does not match the reviewed package."
  done < <(printf '%s' "$release_json" | jq -r '.assets[] | [.id, .name] | @tsv')
  while IFS= read -r asset_name; do
    if ! printf '%s' "$release_json" | jq -e --arg name "$asset_name" 'any(.assets[]; .name == $name)' >/dev/null; then
      [ -f "nupkgs/$asset_name" ] || recovery_fail "Missing reviewed release asset $asset_name."
      release_missing_assets+=("nupkgs/$asset_name")
    fi
  done < <(printf '%s' "$expected_names" | jq -r '.[]')
}
