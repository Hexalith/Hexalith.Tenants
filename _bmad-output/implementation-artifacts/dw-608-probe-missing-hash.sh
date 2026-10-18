#!/usr/bin/env bash
# Reproduce the separately deferred missing-hash gap using only local tools.
# "missing" demonstrates unsafe continuation; "present" must exit 1 at the guard.
set -euo pipefail
mode="${1:?Use missing or present}"
[[ "$mode" == missing || "$mode" == present ]] || exit 2
project_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"
fixture_root="$(mktemp -d)"
trap 'rm -rf -- "$fixture_root"' EXIT
cd -- "$fixture_root"
mkdir state nupkgs
source_sha=aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa
printf '%s\n' "$source_sha" > state/source-sha
printf '1.2.3\n' > state/version
printf 'Hexalith.Tenants.Contracts\n' > state/existing-packages
printf '%s\n' Hexalith.Tenants.Client Hexalith.Tenants.Server Hexalith.Tenants.Testing Hexalith.Tenants.Aspire > state/missing-packages
: > state/existing-package-sha256
if [[ "$mode" == present ]]; then
  printf 'reviewed package\n' | sha256sum | awk '{ print $1 " Hexalith.Tenants.Contracts" }' > state/existing-package-sha256
fi
printf 'absent\n' > state/container-state
for id in Contracts Client Server Testing Aspire; do
  printf 'different package\n' > "nupkgs/Hexalith.Tenants.${id}.1.2.3.nupkg"
done
: > calls
gh() {
  case "$*" in
    'api repos/fixture/recovery/git/ref/heads/main --jq .object.sha') printf '%s\n' "$source_sha" ;;
    'api --include repos/fixture/recovery/releases/tags/v1.2.3') printf 'HTTP/2.0 404 Not Found\n'; return 1 ;;
    'release create '*) printf 'release-publish\n' >> calls ;;
    *) return 97 ;;
  esac
}
dotnet() { printf 'dotnet %s\n' "$*" >> calls; }
python3() { :; }
docker() { cat >/dev/null; printf 'container-login\n' >> calls; }
export GITHUB_SHA="$source_sha" GITHUB_REPOSITORY=fixture/recovery
export HEXALITH_RECOVERY_STATE_DIRECTORY="$fixture_root/state"
export NUGET_API_KEY=fixture-key HEXALITH_ZOT_API_KEY=fixture-key HEXALITH_ZOT_USERNAME=fixture-user
export HEXALITH_ZOT_REGISTRY=fixture.invalid HEXALITH_RECOVERY_CONTAINER_REPOSITORY=tenants
source "$project_root/scripts/publish-partial-release.sh" 1.2.3
[[ "$(< calls)" == *'dotnet nuget push '* ]]
[[ "$(< calls)" == *'dotnet publish '* ]]
[[ "$(< calls)" == *'release-publish'* ]]
cat calls
