#!/usr/bin/env bash
# Build and pack the release candidate before any publication credential exists.
set -euo pipefail

if [ -n "${NUGET_API_KEY:-}" ] || [ -n "${HEXALITH_ZOT_API_KEY:-}" ]; then
  echo "Release candidate preparation must not see publication credentials." >&2
  exit 1
fi

mkdir -p .hexalith/release-candidate nupkgs
version="$(node scripts/calculate-release-version.mjs)"
if [ "$version" = none ]; then
  printf 'none\n' > .hexalith/release-candidate/disposition
  exit 0
fi

# Authenticated destination checks run in Semantic Release's protected verifyRelease
# hook. This job only prepares artifacts and must never receive registry credentials.

printf '%s\n' "$version" > .hexalith/release-candidate/version
git rev-parse HEAD > .hexalith/release-candidate/source-sha
export RepositoryCommit="$(cat .hexalith/release-candidate/source-sha)"
printf 'release\n' > .hexalith/release-candidate/disposition
dotnet build Hexalith.Tenants.slnx --no-restore --configuration Release -warnaserror -p:Version="$version"
python3 scripts/pack-release-packages.py ./nupkgs "$version"
python3 scripts/validate-nuget-packages.py ./nupkgs
python3 scripts/verify-catalog-package-consumer.py ./nupkgs --expected-source-sha "$RepositoryCommit" --output .hexalith/release-candidate/catalog-package-consumer.json
python3 scripts/validate-consumer-package-references.py ./nupkgs
python3 scripts/release-validation-context.py capture
