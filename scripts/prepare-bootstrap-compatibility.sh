#!/usr/bin/env bash
# Frozen topology tests launch non-packaged platform hosts beside the 3.128.4
# packages. Select their published source identity only in disposable CI checkouts.
set -euo pipefail

compatibility_revision=717a06e16c3afd81b904eaf4de646b74b13262d2
reference_path=references/Hexalith.EventStore
workspace_root="$(git rev-parse --show-toplevel)"

if [ "$(git -C "$workspace_root" config --file .gitmodules --get submodule.Hexalith.EventStore.path)" != "$reference_path" ]; then
    echo 'The compatibility host must be a root-declared EventStore submodule.' >&2
    exit 1
fi
reference_root="$workspace_root/$reference_path"
git -C "$reference_root" status --short --branch
git -C "$reference_root" remote -v
git -C "$reference_root" log --oneline --max-count=3
if [ -n "$(git -C "$reference_root" status --porcelain)" ]; then
    echo 'The compatibility host checkout contains changes; refusing to replace them.' >&2
    exit 1
fi
if [ "${1:-}" = --check ]; then
    git -C "$reference_root" cat-file -e "${compatibility_revision}^{commit}"
    echo "Frozen EventStore 3.128.4 host source: $compatibility_revision (check only)."
    exit 0
fi
if [ "$#" -ne 0 ] || [ "${GITHUB_ACTIONS:-}" != true ]; then
    echo 'Host selection requires a disposable GitHub Actions checkout; use --check locally.' >&2
    exit 1
fi
case "${GITHUB_JOB:-}" in
    build-and-test)
        # This reusable job runs package/unit checks, including current-source
        # tooling checks, and never launches the compatibility topology.
        echo 'Keeping tracked EventStore source for the build-and-test job.'
        exit 0
        ;;
    authenticated-routes|aspire-tests|performance-tests) ;;
    *)
        echo 'Host selection is limited to the declared compatibility topology jobs.' >&2
        exit 1
        ;;
esac
if ! git -C "$reference_root" cat-file -e "${compatibility_revision}^{commit}" 2>/dev/null; then
    git -C "$reference_root" -c submodule.recurse=false fetch --no-recurse-submodules origin "$compatibility_revision"
fi
git -C "$reference_root" -c submodule.recurse=false checkout --detach "$compatibility_revision"
test "$(git -C "$reference_root" rev-parse HEAD)" = "$compatibility_revision"
echo "Frozen EventStore 3.128.4 host source: $compatibility_revision."
