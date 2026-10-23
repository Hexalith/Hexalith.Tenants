#!/usr/bin/env bash
set -euo pipefail

version="${1:?Release version is required}"
repository="${GITHUB_REPOSITORY:-Hexalith/Hexalith.Tenants}"
source_sha="${GITHUB_SHA:?GITHUB_SHA is required}"
manifest="${HEXALITH_RELEASE_PACKAGE_MANIFEST:-tools/release-packages.json}"
state_directory="${HEXALITH_RECOVERY_STATE_DIRECTORY:-.hexalith/recovery}"
source "$(dirname "${BASH_SOURCE[0]}")/partial-release-recovery-common.sh"
state_directory="$(recovery_confine_directory "$state_directory")"
recovery_require_manifest
recovery_require_prepared
[ "$(cat "$state_directory/source-sha")" = "$source_sha" ] || recovery_fail 'Recovery source changed.'
[ "$(cat "$state_directory/version")" = "$version" ] || recovery_fail 'Recovery version changed.'
if [ "$(cat "$state_directory/container-state")" = present ]; then
  export HEXALITH_RELEASE_OCI_RECEIPT="$state_directory/oci-validated/oci-validation.json"
else
  export HEXALITH_RELEASE_OCI_RECEIPT="$state_directory/oci-published/oci-validation.json"
fi
export HEXALITH_RELEASE_DESTINATION_EVIDENCE_DIRECTORY="$state_directory/destinations"
( source "$(dirname "${BASH_SOURCE[0]}")/verify-release-artifacts.sh" "$version" )
recovery_require_manifest
recovery_require_prepared
