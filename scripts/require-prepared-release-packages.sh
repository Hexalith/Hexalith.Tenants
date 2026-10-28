#!/usr/bin/env bash
# Confirm the credential-free preparation already packed this exact version.
set -euo pipefail

expected="${1:?Release version is required}"
version_file=".hexalith/release-candidate/version"
[ -f "$version_file" ] || {
  echo "Prepared release packages are missing." >&2
  exit 1
}
actual="$(tr -d '[:space:]' < "$version_file")"
[ "$actual" = "$expected" ] || {
  echo "Prepared release packages are ${actual}, not ${expected}." >&2
  exit 1
}
prepared_sha="$(tr -d '[:space:]' < .hexalith/release-candidate/source-sha)"
checkout_sha="$(git rev-parse HEAD)"
[ "$prepared_sha" = "$checkout_sha" ] || {
  echo "Prepared release packages were built from ${prepared_sha}, not ${checkout_sha}." >&2
  exit 1
}

python3 scripts/release-validation-context.py restore
python3 scripts/validate-nuget-packages.py ./nupkgs
python3 scripts/validate-consumer-package-references.py ./nupkgs
