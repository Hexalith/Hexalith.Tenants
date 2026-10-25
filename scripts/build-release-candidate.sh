#!/usr/bin/env bash
# Build and pack the release candidate before any publication credential exists.
set -euo pipefail

if [ -n "${NUGET_API_KEY:-}" ] || [ -n "${HEXALITH_ZOT_API_KEY:-}" ]; then
  echo "Release candidate preparation must not see publication credentials." >&2
  exit 1
fi

mkdir -p .hexalith/release-candidate nupkgs
dotnet build Hexalith.Tenants.slnx --no-restore --configuration Release -warnaserror

set +e
dry_output="$(HEXALITH_RELEASE_PREPARE=skip npx semantic-release --dry-run 2>&1)"
dry_status=$?
set -e
printf '%s\n' "$dry_output"
if [ "$dry_status" -ne 0 ]; then
  exit "$dry_status"
fi

version="$(printf '%s\n' "$dry_output" | sed -n 's/^.*The next release version is \([0-9][0-9A-Za-z.+-]*\).*$/\1/p' | head -n 1)"
if [ -z "$version" ]; then
  printf 'none\n' > .hexalith/release-candidate/disposition
  exit 0
fi

printf '%s\n' "$version" > .hexalith/release-candidate/version
git rev-parse HEAD > .hexalith/release-candidate/source-sha
printf 'release\n' > .hexalith/release-candidate/disposition
dotnet build Hexalith.Tenants.slnx --no-restore --configuration Release -warnaserror -p:Version="$version"
python3 scripts/pack-release-packages.py ./nupkgs "$version"
python3 scripts/validate-nuget-packages.py ./nupkgs
python3 scripts/validate-consumer-package-references.py ./nupkgs
