#!/usr/bin/env bash
# Select the executable host source for the catalog release tested by the Tenants-owned harness.
set -euo pipefail

reference_path=references/Hexalith.EventStore
workspace_root="$(git rev-parse --show-toplevel)"

if [ "$(git -C "$workspace_root" config --file .gitmodules --get submodule.Hexalith.EventStore.path)" != "$reference_path" ]; then
    echo 'The compatibility host must be a root-declared EventStore submodule.' >&2
    exit 1
fi
reference_root="$workspace_root/$reference_path"
builds_root="$workspace_root/references/Hexalith.Builds"
read -r version compatibility_revision < <(python3 - "$builds_root" <<'PYCODE'
import json, re, sys
from pathlib import Path
from xml.etree import ElementTree as ET
root = Path(sys.argv[1]) / "Props"
versions = ET.parse(root / "Directory.Packages.props").findall("PropertyGroup/HexalithEventStoreVersion")
if len(versions) != 1 or not re.fullmatch(r"[0-9]+(?:\.[0-9]+){2,3}", versions[0].text or ""):
    raise ValueError("The Builds catalog must select one stable EventStore version")
version = versions[0].text
release = json.loads((root / "release-declarations.json").read_bytes())["eventstore/" + version]
if release["repository"] != "Hexalith.EventStore" or release["version"] != version or not re.fullmatch("[0-9a-f]{40}", release["commit"]):
    raise ValueError("Invalid EventStore release declaration")
print(version, release["commit"])
PYCODE
)

if [ -n "$(git -C "$reference_root" status --porcelain)" ]; then
    echo 'The compatibility host checkout contains changes; refusing to replace them.' >&2
    exit 1
fi
if [ "${1:-}" = --check ]; then
    git -C "$reference_root" cat-file -e "${compatibility_revision}^{commit}"
    echo "EventStore $version host source: $compatibility_revision (check only)."
    exit 0
fi
if [ "$#" -ne 0 ] || [ "${GITHUB_ACTIONS:-}" != true ]; then
    echo 'Host selection requires a disposable GitHub Actions checkout; use --check locally.' >&2
    exit 1
fi
case "${GITHUB_JOB:-}" in
    build-and-test|authenticated-routes|aspire-tests|performance-tests) ;;
    *)
        echo 'Host selection is limited to the declared CI jobs.' >&2
        exit 1
        ;;
esac
if ! git -C "$reference_root" cat-file -e "${compatibility_revision}^{commit}" 2>/dev/null; then
    git -C "$reference_root" -c submodule.recurse=false fetch --no-recurse-submodules origin "$compatibility_revision"
fi
git -C "$reference_root" -c submodule.recurse=false checkout --detach "$compatibility_revision"
test "$(git -C "$reference_root" rev-parse HEAD)" = "$compatibility_revision"
echo "EventStore $version host source: $compatibility_revision."
