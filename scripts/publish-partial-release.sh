#!/usr/bin/env bash
set -euo pipefail

version="${1:?Release version is required}"
state_directory="${HEXALITH_RECOVERY_STATE_DIRECTORY:-.hexalith/recovery}"
manifest="${HEXALITH_RELEASE_PACKAGE_MANIFEST:-tools/release-packages.json}"
source_sha="$(cat "$state_directory/source-sha")"
test "$source_sha" = "${GITHUB_SHA:?GITHUB_SHA is required}" || { echo '[partial-release-recovery] Source changed during recovery.' >&2; exit 1; }
[ -f "$state_directory/missing-packages" ] || { echo '[partial-release-recovery] Missing-package state is absent.' >&2; exit 1; }

if [ -s "$state_directory/missing-packages" ]; then
  dotnet build Hexalith.Tenants.slnx --configuration Release -p:Version="$version"
  python3 scripts/pack-release-packages.py ./nupkgs "$version"
  python3 scripts/validate-nuget-packages.py ./nupkgs
  python3 scripts/validate-consumer-package-references.py ./nupkgs
fi

while IFS= read -r package_id; do
  [ -n "$package_id" ] || continue
  package_file="nupkgs/${package_id}.${version}.nupkg"
  [ -f "$package_file" ] || { echo "[partial-release-recovery] Missing artifact $package_id." >&2; exit 1; }
  dotnet nuget push "$package_file" --source https://api.nuget.org/v3/index.json --api-key "${NUGET_API_KEY:?NUGET_API_KEY is required}"
done < "$state_directory/missing-packages"

if [ "$(cat "$state_directory/container-state")" = absent ]; then
  registry="${HEXALITH_ZOT_REGISTRY:-registry.hexalith.com}"
  repository="${HEXALITH_RECOVERY_CONTAINER_REPOSITORY:-tenants}"
  echo "${HEXALITH_ZOT_API_KEY:?HEXALITH_ZOT_API_KEY is required}" | docker login "$registry" --username "${HEXALITH_ZOT_USERNAME:?HEXALITH_ZOT_USERNAME is required}" --password-stdin
  dotnet publish src/Hexalith.Tenants/Hexalith.Tenants.csproj --configuration Release /t:PublishContainer \
    -p:RuntimeIdentifiers='linux-musl-x64;linux-musl-arm64' \
    -p:ContainerRuntimeIdentifiers='linux-musl-x64;linux-musl-arm64' \
    -p:ContainerImageFormat=OCI -p:UseHexalithProjectReferences=false \
    -p:ContainerRegistry="$registry" -p:ContainerRepository="$repository" -p:ContainerImageTag="$version" -p:Version="$version"
fi

if ! gh release view "v${version}" >/dev/null 2>&1; then
  gh release create "v${version}" --target "$source_sha" --title "v${version}" --generate-notes nupkgs/*.nupkg
fi
