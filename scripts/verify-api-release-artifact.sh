#!/usr/bin/env bash
# Verify the API image from the normal release without changing historical server recovery.
set -euo pipefail

repository="${GITHUB_REPOSITORY:-Hexalith/Hexalith.Tenants}"
source_sha="${GITHUB_SHA:-}"
source "$(dirname "${BASH_SOURCE[0]}")/partial-release-recovery-common.sh"
shopt -s nullglob
for prior in .hexalith/release-evidence/*/tenants-api-destination; do
  prior="$(recovery_confine_directory "$prior")"
  rm -f "$prior/verified.json"
done
shopt -u nullglob
[[ "$source_sha" =~ ^[0-9a-f]{40}$ ]] || recovery_fail 'Source SHA must be an exact lowercase commit SHA.'
[ "$repository" = 'Hexalith/Hexalith.Tenants' ] || recovery_fail 'The API image must belong to Hexalith.Tenants.'

receipt="${HEXALITH_RELEASE_API_OCI_RECEIPT:-}"
if [ -z "$receipt" ]; then
  shopt -s nullglob
  receipts=(.hexalith/release-evidence/*/tenants-api/oci-validation.json)
  shopt -u nullglob
  [ "${#receipts[@]}" -eq 1 ] || recovery_fail 'Exactly one API publisher OCI receipt is required.'
  receipt="${receipts[0]}"
fi
recovery_confine_directory "$(dirname "$receipt")" >/dev/null
[ -f "$receipt" ] || recovery_fail 'The API publisher OCI receipt is missing.'
shopt -s nullglob
release_proofs=(.hexalith/release-evidence/*/destinations/verified.json)
shopt -u nullglob
if [ -n "${HEXALITH_RELEASE_DESTINATION_EVIDENCE_DIRECTORY:-}" ]; then
  release_proofs=("$HEXALITH_RELEASE_DESTINATION_EVIDENCE_DIRECTORY/verified.json")
fi
[ "${#release_proofs[@]}" -eq 1 ] || recovery_fail 'Exactly one verified normal release result is required.'
release_proof="${release_proofs[0]}"
recovery_confine_directory "$(dirname "$release_proof")" >/dev/null
[ -f "$release_proof" ] || recovery_fail 'The verified normal release result is missing.'
version="$(jq -er '.version' "$release_proof")"
[[ "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z.-]+)?$ ]] || recovery_fail 'The verified release version must be plain SemVer.'
evidence_directory="$(recovery_confine_directory ".hexalith/release-evidence/$version/tenants-api-destination")"
manifest=tools/release-packages.json
recovery_require_separate_evidence "$evidence_directory"
case "$(realpath -m -- "$release_proof")/" in "$evidence_directory/"*) recovery_fail 'API evidence overlaps the verified release result.' ;; esac
mkdir -p "$evidence_directory/reviewed"
rm -f "$evidence_directory/verified.json"
cp "$release_proof" "$evidence_directory/reviewed/release.json"
jq -e --arg source "$source_sha" --arg repository "$repository" --arg version "$version" \
  '.result == "pass" and .source_sha == $source and .repository == $repository and .version == $version' \
  "$evidence_directory/reviewed/release.json" >/dev/null || recovery_fail 'The verified normal release result does not match the current source.'
cp "$receipt" "$evidence_directory/reviewed/receipt.json"
reviewed_hash="$(sha256sum "$evidence_directory/reviewed/receipt.json" "$evidence_directory/reviewed/release.json")"
HEXALITH_RECOVERY_CONTAINER_REPOSITORY=tenants-api recovery_verify_container \
  "$evidence_directory/oci" "$evidence_directory/reviewed/receipt.json"
[ "$(sha256sum "$evidence_directory/reviewed/receipt.json" "$evidence_directory/reviewed/release.json")" = "$reviewed_hash" ] || recovery_fail 'The reviewed API receipt changed during verification.'
jq -n --arg source "$source_sha" --arg version "$version" \
  --arg digest "$(jq -r '.index_digest' "$evidence_directory/oci/oci-validation.json")" \
  '{result: "pass", source_sha: $source, version: $version, index_digest: $digest}' > "$evidence_directory/verified.json"
echo "Verified immutable Tenants API image v${version} for source ${source_sha}."
