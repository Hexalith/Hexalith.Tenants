#!/usr/bin/env bash
# Internal functions shared by the partial-release recovery steps.

recovery_fail() { echo "[partial-release-recovery] $1" >&2; exit 1; }

# Resolve existing symlinks before accepting any retained evidence destination.
recovery_confine_directory() {
  local directory
  directory="$(realpath -m -- "$1")"
  case "$directory/" in
    "$(realpath -e "$PWD")"/*) ;;
    *) recovery_fail 'Evidence directory must remain below the workspace.' ;;
  esac
  [ "$directory" != "$(realpath -e "$PWD")" ] || recovery_fail 'Evidence directory must remain below the workspace.'
  if [ -d "$directory" ] && [ -n "$(find "$directory" -type l -print -quit)" ]; then
    recovery_fail 'Evidence directory must not contain symbolic links.'
  fi
  printf '%s\n' "$directory"
}

recovery_require_separate_evidence() {
  local output="$1" input resolved
  for input in nupkgs "$manifest" "${receipt:-}"; do
    [ -n "$input" ] || continue
    resolved="$(realpath -m -- "$input")"
    case "$resolved/" in "$output/"*) recovery_fail 'Evidence directory overlaps reviewed inputs.' ;; esac
    if [ "$input" = nupkgs ]; then
      case "$output/" in "$resolved/"*) recovery_fail 'Evidence directory overlaps reviewed inputs.' ;; esac
    fi
  done
}

recovery_verify_container() {
  local evidence_directory="$1" receipt="${2:-}" image expected_digest actual_digest
  local validator="${HEXALITH_RELEASE_OCI_VALIDATOR:-references/Hexalith.Builds/Github/publish-containers/oci_registry_validator.py}"
  evidence_directory="$(recovery_confine_directory "$evidence_directory")"
  image="${HEXALITH_ZOT_REGISTRY:-registry.hexalith.com}/${HEXALITH_RECOVERY_CONTAINER_REPOSITORY:-tenants}:${version}"
  if [ -n "$receipt" ]; then
    # Snapshot the expected digest before the fresh observation can write any evidence.
    [ -f "$receipt" ] || recovery_fail 'Reviewed OCI receipt is missing.'
    jq -e --arg image "$image" '
      .result == "pass" and .image == $image and
      .platforms == ["linux/amd64", "linux/arm64"] and
      (.index_digest | type == "string" and test("^sha256:[0-9a-f]{64}$"))
    ' "$receipt" >/dev/null || recovery_fail 'Reviewed OCI receipt does not match the release identity.'
    expected_digest="$(jq -r '.index_digest' "$receipt")"
  fi
  python3 "$validator" --image "$image" --repository "$repository" --source-sha "$source_sha" \
    --version "$version" --evidence-directory "$evidence_directory" || return $?
  if [ -n "$receipt" ]; then
    actual_digest="$(jq -r '.index_digest' "$evidence_directory/oci-validation.json")"
    [ "$actual_digest" = "$expected_digest" ] || recovery_fail 'OCI destination digest changed from the reviewed receipt.'
  fi
}

recovery_recheck_container() {
  local status
  status="$(curl --silent --show-error --output /dev/null --write-out '%{http_code}' \
    --user "${HEXALITH_ZOT_USERNAME:?}:${HEXALITH_ZOT_API_KEY:?}" \
    -H 'Accept: application/vnd.oci.image.index.v1+json, application/vnd.oci.image.manifest.v1+json, application/vnd.docker.distribution.manifest.v2+json, application/vnd.docker.distribution.manifest.list.v2+json' \
    "https://${HEXALITH_ZOT_REGISTRY:-registry.hexalith.com}/v2/${HEXALITH_RECOVERY_CONTAINER_REPOSITORY:-tenants}/manifests/${version}")"
  if [ "$(cat "$state_directory/container-state")" = present ]; then
    [ "$status" = 200 ] || recovery_fail 'Reviewed OCI destination is no longer present.'
    recovery_verify_container "$state_directory/oci-recheck" "$state_directory/oci-validated/oci-validation.json"
  else
    [ "$status" = 404 ] || recovery_fail 'OCI destination changed after recovery validation.'
  fi
}

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
  state_directory="$(recovery_confine_directory "$state_directory")"
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
  local reviewed_hashes manifest_hash_before current_hashes
  manifest_hash_before="$(sha256sum "$manifest")"
  if [ -n "${release_reviewed_hashes_file:-}" ]; then
    reviewed_hashes="$(cat "$release_reviewed_hashes_file")"
  elif [ -s "${state_directory:-}/prepared-packages-sha256" ]; then
    reviewed_hashes="$(sed 's|  nupkgs/| |' "$state_directory/prepared-packages-sha256")"
  else
    reviewed_hashes="$(recovery_package_hashes | sed 's|  nupkgs/| |')" || recovery_fail 'Reviewed package hashes could not be frozen.'
  fi
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
    expected_hash="$(printf '%s\n' "$reviewed_hashes" | awk -v name="$asset_name" '$2 == name { print $1 }')"
    [[ "$expected_hash" =~ ^[0-9a-f]{64}$ ]] || recovery_fail "Missing frozen reviewed hash for $asset_name."
    actual_hash="$(sha256sum "$downloaded" | awk '{ print $1 }')"
    if [ -n "${release_asset_evidence_directory:-}" ]; then
      mkdir -p "$release_asset_evidence_directory/github"
      cp "$downloaded" "$release_asset_evidence_directory/github/$asset_name"
      printf '%s %s\n' "$actual_hash" "$asset_name" >> "$release_asset_evidence_directory/github-sha256"
    fi
    rm -f "$downloaded"
    [ "$actual_hash" = "$expected_hash" ] || recovery_fail "Release asset $asset_name does not match the reviewed package."
  done < <(printf '%s' "$release_json" | jq -r '.assets[] | [.id, .name] | @tsv')
  while IFS= read -r asset_name; do
    if ! printf '%s' "$release_json" | jq -e --arg name "$asset_name" 'any(.assets[]; .name == $name)' >/dev/null; then
      [ -f "nupkgs/$asset_name" ] || recovery_fail "Missing reviewed release asset $asset_name."
      release_missing_assets+=("nupkgs/$asset_name")
    fi
  done < <(printf '%s' "$expected_names" | jq -r '.[]')
  [ "$(sha256sum "$manifest")" = "$manifest_hash_before" ] || recovery_fail 'Reviewed manifest changed during asset verification.'
  if [ -z "${release_reviewed_hashes_file:-}" ] && [ ! -s "${state_directory:-}/prepared-packages-sha256" ]; then
    current_hashes="$(recovery_package_hashes | sed 's|  nupkgs/| |')" || recovery_fail 'Reviewed package bytes disappeared during asset verification.'
    [ "$current_hashes" = "$reviewed_hashes" ] || recovery_fail 'Reviewed package bytes changed during asset verification.'
  fi
}


# Bind prepared bytes to the validated inventory, source and destination decisions.
recovery_preparation_state() {
  local state_file
  for state_file in source-sha version package-manifest-sha256 missing-packages existing-packages container-state; do
    [ -f "$state_directory/$state_file" ] || recovery_fail "Required recovery state $state_file is absent."
  done
  case "$(cat "$state_directory/container-state")" in
    present|absent) ;;
    *) recovery_fail 'Container recovery state must be present or absent.' ;;
  esac
  printf 'repository %s\nregistry %s\ncontainer %s\n' "$repository" "${HEXALITH_ZOT_REGISTRY:-registry.hexalith.com}" "${HEXALITH_RECOVERY_CONTAINER_REPOSITORY:-tenants}"
  for state_file in source-sha version package-manifest-sha256 missing-packages existing-packages existing-package-sha256 container-state; do
    if [ -f "$state_directory/$state_file" ]; then
      sha256sum "$state_directory/$state_file" || return 1
    else
      printf 'absent %s\n' "$state_file"
    fi
  done
  if [ "$(cat "$state_directory/container-state")" = present ]; then
    [ -s "$state_directory/oci-validated/oci-validation.json" ] || recovery_fail 'Validated OCI receipt is absent.'
    sha256sum "$state_directory/oci-validated/oci-validation.json" || return 1
  fi
}

recovery_package_hashes() {
  local package_id
  while IFS= read -r package_id; do
    sha256sum "nupkgs/${package_id}.${version}.nupkg" || return 1
  done < <(jq -r '.packages[].id' "$manifest")
}

recovery_require_prepared() {
  local current_packages current_state
  [ -s "$state_directory/prepared-packages-sha256" ] && [ -s "$state_directory/prepared-state-sha256" ] ||
    recovery_fail 'Prepared package evidence is required before publication.'
  current_packages="$(recovery_package_hashes)" || recovery_fail 'Prepared package bytes are missing.'
  [ "$current_packages" = "$(cat "$state_directory/prepared-packages-sha256")" ] ||
    recovery_fail 'Prepared package bytes changed before publication.'
  current_state="$(recovery_preparation_state)" || recovery_fail 'Recovery state changed after package preparation or could not be read.'
  [ "$current_state" = "$(cat "$state_directory/prepared-state-sha256")" ] ||
    recovery_fail 'Recovery state changed after package preparation.'
}
