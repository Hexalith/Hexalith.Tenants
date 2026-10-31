#!/usr/bin/env bash
set -euo pipefail

version="${1:?Release version is required}"
state_directory="${HEXALITH_RECOVERY_STATE_DIRECTORY:-.hexalith/recovery}"
manifest="${HEXALITH_RELEASE_PACKAGE_MANIFEST:-tools/release-packages.json}"
repository="${GITHUB_REPOSITORY:-Hexalith/Hexalith.Tenants}"
source "$(dirname "${BASH_SOURCE[0]}")/partial-release-recovery-common.sh"
source_sha="$(cat "$state_directory/source-sha")"
test "$source_sha" = "${GITHUB_SHA:?GITHUB_SHA is required}" || { echo '[partial-release-recovery] Source changed during recovery.' >&2; exit 1; }
[ "$(cat "$state_directory/version")" = "$version" ] || { echo '[partial-release-recovery] Recovery version changed.' >&2; exit 1; }
[ -f "$state_directory/missing-packages" ] || { echo '[partial-release-recovery] Missing-package state is absent.' >&2; exit 1; }
recovery_require_manifest
recovery_require_execution
recovery_require_source_gates "$state_directory/source-gates-preparation.json"
live_sha="$(gh api "repos/${GITHUB_REPOSITORY}/git/ref/heads/main" --jq '.object.sha')"
[ "$live_sha" = "$source_sha" ] || { echo '[partial-release-recovery] Live main changed before publication.' >&2; exit 1; }

validated_state="$(recovery_preparation_state)"

rm -f "$state_directory/prepared-packages-sha256" "$state_directory/prepared-state-sha256"

export RepositoryCommit="$source_sha"
dotnet build Hexalith.Tenants.slnx --configuration Release -p:Version="$version"
python3 scripts/pack-release-packages.py ./nupkgs "$version" --manifest "$manifest"
python3 scripts/validate-nuget-packages.py ./nupkgs --manifest "$manifest"
python3 scripts/verify-catalog-package-consumer.py ./nupkgs --expected-source-sha "$source_sha" --output "$state_directory/catalog-package-consumer.json"
python3 scripts/validate-consumer-package-references.py ./nupkgs

while IFS= read -r package_id || [ -n "$package_id" ]; do
  [ -n "$package_id" ] || continue
  package_file="nupkgs/${package_id}.${version}.nupkg"
  expected_hash="$(awk -v id="$package_id" '$2 == id { print $1 }' "$state_directory/existing-package-sha256")"
  [ -n "$expected_hash" ] || { echo "[partial-release-recovery] Existing package $package_id has no reviewed hash." >&2; exit 1; }
  actual_hash="$(sha256sum "$package_file" | awk '{ print $1 }')"
  observed_package="$state_directory/${package_id}.${version}.nupkg"
  [ -f "$observed_package" ] || recovery_fail "Existing package $package_id has no retained destination archive."
  observed_hash="$(sha256sum "$observed_package" | awk '{ print $1 }')"
  [ "$observed_hash" = "$expected_hash" ] || recovery_fail "Existing package $package_id destination archive changed after recovery validation."
  python3 "$(dirname "${BASH_SOURCE[0]}")/release-package-content.py" compare "$package_file" "$observed_package" --reviewed-sha256 "$actual_hash" ||
    recovery_fail "Existing package $package_id does not match the reviewed source."
done < "$state_directory/existing-packages"

recovery_require_manifest
recovery_require_execution
[ "$(recovery_preparation_state)" = "$validated_state" ] || recovery_fail 'Recovery state changed during package preparation.'
recovery_package_hashes > "$state_directory/prepared-packages-sha256"
printf '%s\n' "$validated_state" > "$state_directory/prepared-state-sha256"
