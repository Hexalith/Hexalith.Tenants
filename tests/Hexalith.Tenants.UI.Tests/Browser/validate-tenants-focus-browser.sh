#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
project_root="$(cd -- "$script_dir/../../.." && pwd)"
harness_path="$script_dir/tenants-focus-browser-validation.html"
focus_module_path="$project_root/src/Hexalith.Tenants.UI/wwwroot/js/tenantsFocus.js"
project_assets_path="$project_root/src/Hexalith.Tenants.UI/obj/project.assets.json"

if [[ ! -f "$harness_path" || ! -f "$focus_module_path" || ! -f "$project_assets_path" ]]; then
    echo "Focus validator inputs are missing." >&2
    exit 1
fi

fluent_module_path="$(python3 - "$project_assets_path" <<'PY'
import json
from pathlib import Path
import sys

assets = json.loads(Path(sys.argv[1]).read_text(encoding="utf-8"))
package = next(
    (name for name in assets["libraries"]
     if name.lower().startswith("microsoft.fluentui.aspnetcore.components/")),
    None,
)
if package is None:
    raise SystemExit("The restored Fluent UI package was not found in project.assets.json.")
version = package.split("/", 1)[1]
package_root = next(iter(assets["packageFolders"]))
module = (
    Path(package_root)
    / "microsoft.fluentui.aspnetcore.components"
    / version
    / "staticwebassets"
    / "Microsoft.FluentUI.AspNetCore.Components.lib.module.js"
)
if not module.is_file():
    raise SystemExit(f"The restored Fluent UI browser module is missing: {module}")
print(module)
PY
)"

browser_path="${CHROMIUM_BIN:-}"
if [[ -z "$browser_path" ]]; then
    for candidate in google-chrome-stable google-chrome chromium chromium-browser; do
        if command -v "$candidate" >/dev/null 2>&1; then
            browser_path="$(command -v "$candidate")"
            break
        fi
    done
fi

if [[ -z "$browser_path" && -d "${HOME}/.cache/ms-playwright" ]]; then
    browser_path="$(find "${HOME}/.cache/ms-playwright" -type f \( -name chrome -o -name headless_shell \) -perm -u+x -print 2>/dev/null | sort | tail -n 1)"
fi

if [[ -z "$browser_path" || ! -x "$browser_path" ]]; then
    echo "Real Chromium is required. Set CHROMIUM_BIN to a Chromium or Chrome executable." >&2
    exit 1
fi

validation_tmp="$(mktemp -d -t tenants-focus-browser.XXXXXX)"
server_pid=""
cleanup() {
    if [[ -n "$server_pid" ]]; then
        kill "$server_pid" >/dev/null 2>&1 || true
        wait "$server_pid" 2>/dev/null || true
    fi
    rm -rf -- "$validation_tmp"
}
trap cleanup EXIT

cp -- "$harness_path" "$validation_tmp/index.html"
cp -- "$focus_module_path" "$validation_tmp/tenantsFocus.js"
cp -- "$fluent_module_path" "$validation_tmp/fluent-ui.js"

python3 - "$validation_tmp/tenantsFocus.js" "$validation_tmp/tenantsFocus-return-true.js" <<'PY'
from pathlib import Path
import sys

source = Path(sys.argv[1]).read_text(encoding="utf-8")
signature = "export function focusElementById(elementId)"
start = source.index(signature)
brace = source.index("{", start)
depth = 0
end = None
for index in range(brace, len(source)):
    if source[index] == "{":
        depth += 1
    elif source[index] == "}":
        depth -= 1
        if depth == 0:
            end = index + 1
            break
if end is None:
    raise SystemExit("Could not isolate focusElementById for the mutation check.")
mutated = source[:start] + signature + " {\n  return true;\n}" + source[end:]
Path(sys.argv[2]).write_text(mutated, encoding="utf-8")
PY

validation_port="$(python3 - <<'PY'
import socket
with socket.socket() as listener:
    listener.bind(("127.0.0.1", 0))
    print(listener.getsockname()[1])
PY
)"
if [[ "${TENANTS_FOCUS_TEST_SERVER_START_FAILURE:-false}" == true ]]; then
    python3 -c 'raise SystemExit("forced focus-validator server startup failure")' \
        >"$validation_tmp/server.log" 2>&1 &
else
    python3 -m http.server "$validation_port" --bind 127.0.0.1 --directory "$validation_tmp" \
        >"$validation_tmp/server.log" 2>&1 &
fi
server_pid="$!"

validation_url="http://127.0.0.1:${validation_port}/index.html"
server_ready=false
for _ in $(seq 1 50); do
    if python3 - "$validation_url" <<'PY' >/dev/null 2>&1
import sys
import urllib.request
with urllib.request.urlopen(sys.argv[1], timeout=0.25) as response:
    if response.status != 200:
        raise SystemExit(1)
PY
    then
        server_ready=true
        break
    fi
    if ! kill -0 "$server_pid" >/dev/null 2>&1; then
        break
    fi
    sleep 0.05
done
if [[ "$server_ready" != true ]]; then
    echo "Focus validation server did not become ready at $validation_url." >&2
    sed -n '1,120p' "$validation_tmp/server.log" >&2
    exit 1
fi

run_browser() {
    local module_name="$1"
    local profile_name="$2"
    local output_path="$3"
    if [[ -n "${TENANTS_FOCUS_BROWSER_INVOCATION_MARKER:-}" ]]; then
        printf '%s\n' "invoked" >"$TENANTS_FOCUS_BROWSER_INVOCATION_MARKER"
    fi
    "$browser_path" \
        --headless=new \
        --disable-dev-shm-usage \
        --disable-gpu \
        --no-default-browser-check \
        --no-first-run \
        --user-data-dir="$validation_tmp/$profile_name" \
        --virtual-time-budget=3000 \
        --dump-dom \
        "${validation_url}?module=./${module_name}" >"$output_path" 2>"${output_path}.stderr"
}

positive_output="$validation_tmp/shipped.html"
run_browser "tenantsFocus.js" "profile-shipped" "$positive_output"
if ! grep -q 'data-validation-status="passed"' "$positive_output"; then
    echo "Shipped focus module failed real-Chromium validation:" >&2
    grep -o '<output id="validation-report">[^<]*' "$positive_output" >&2 || true
    sed -n '1,120p' "${positive_output}.stderr" >&2
    exit 1
fi

mutation_output="$validation_tmp/return-true.html"
run_browser "tenantsFocus-return-true.js" "profile-mutation" "$mutation_output"
if grep -q 'data-validation-status="passed"' "$mutation_output"; then
    echo "Validator accepted a return-true focusElementById mutation without focus movement." >&2
    exit 1
fi
if ! grep -q 'data-validation-status="failed"' "$mutation_output"; then
    echo "Mutation validation did not complete in Chromium." >&2
    exit 1
fi

browser_version="$($browser_path --version | head -n 1)"
positive_report="$(grep -o '<output id="validation-report">[^<]*' "$positive_output" | sed 's/.*>//')"
mutation_report="$(grep -o '<output id="validation-report">[^<]*' "$mutation_output" | sed 's/.*>//')"
printf '%s\n' "Browser: $browser_version"
printf '%s\n' "Shipped module: $positive_report"
printf '%s\n' "Return-true mutation: correctly rejected ($mutation_report)"
