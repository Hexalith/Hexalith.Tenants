#!/usr/bin/env bash
# Run with run-with-temp from the FrontComposer repository.
set -euo pipefail
: "${TMPDIR:?run-with-temp must supply a disposable workspace}"
fc_repository="$PWD"
fc_evidence="$(cd ../../_bmad-output/implementation-artifacts/evidence/fix-firefox-route-focus-ci-2026-11-15 && pwd)"
fc_artifacts="$TMPDIR/artifacts"
fc_host_pid=''
cleanup() {
    if [[ -n "$fc_host_pid" ]]; then
        kill "$fc_host_pid" 2>/dev/null || true
        wait "$fc_host_pid" 2>/dev/null || true
    fi
}
trap cleanup EXIT

dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj \
    -c Debug --artifacts-path "$fc_artifacts" -m:1 -v:minimal \
    -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0 2>&1 | tee "$fc_evidence/focused-build.log"
dotnet "$fc_artifacts/bin/Hexalith.FrontComposer.Shell.Tests/debug/Hexalith.FrontComposer.Shell.Tests.dll" \
    -class Hexalith.FrontComposer.Shell.Tests.Generated.CommandRendererCompactInlineTests \
    2>&1 | tee "$fc_evidence/compact-inline-tests.log"
dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj \
    -c Debug --artifacts-path "$fc_artifacts" -m:1 -v:minimal \
    -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0 2>&1 | tee "$fc_evidence/generator-build.log"
dotnet "$fc_artifacts/bin/Hexalith.FrontComposer.SourceTools.Tests/debug/Hexalith.FrontComposer.SourceTools.Tests.dll" \
    -class Hexalith.FrontComposer.SourceTools.Tests.Emitters.CommandRendererEmitterTests \
    2>&1 | tee "$fc_evidence/renderer-generator-tests.log"

fc_port="$(python3 -c 'import socket; s = socket.socket(); s.bind(("127.0.0.1", 0)); print(s.getsockname()[1]); s.close()')"
export BASE_URL="http://127.0.0.1:$fc_port"
ASPNETCORE_ENVIRONMENT=Test \
Hexalith__FrontComposer__Specimens__Enabled=true \
Hexalith__Shell__FormAbandonmentThresholdSeconds=5 \
Hexalith__Shell__TimeoutActionThresholdMs=5000 \
Hexalith__Shell__ProjectionReconnectedNoticeDurationMs=30000 \
Hexalith__FrontComposer__StubCommandService__ConfirmDelayMs=6500 \
dotnet "$fc_artifacts/bin/Counter.Web/debug/Counter.Web.dll" \
    --contentRoot "$fc_repository/samples/Counter/Counter.Web" --urls "$BASE_URL" \
    > "$fc_evidence/counter-host.log" 2>&1 &
fc_host_pid=$!
python3 - "$BASE_URL" <<'PY'
import subprocess
import sys
import time


def wait_for_host(url, timeout=30):
    deadline = time.monotonic() + timeout
    request = """import sys, urllib.request
try:
    with urllib.request.urlopen(sys.argv[1], timeout=float(sys.argv[2])) as response:
        raise SystemExit(0 if response.status == 200 else 1)
except OSError:
    raise SystemExit(1)
"""
    while (remaining := deadline - time.monotonic()) > 0:
        try:
            # subprocess.run kills and waits for this owned child on timeout, including
            # requests that keep delivering headers or following redirects indefinitely.
            response = subprocess.run([sys.executable, '-c', request, url, str(min(1, remaining))],
                stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=remaining)
        except subprocess.TimeoutExpired:
            break
        if response.returncode == 0 and time.monotonic() < deadline:
            return
        time.sleep(min(0.25, max(0, deadline - time.monotonic())))
    raise TimeoutError(f"Counter host did not become ready within {timeout:g} seconds")


if __name__ == '__main__':
    try:
        wait_for_host(sys.argv[1])
    except TimeoutError as error:
        raise SystemExit(str(error))

PY
export PLAYWRIGHT_SKIP_WEBSERVER=1
export FC_E2E_OUTPUT_DIR="$fc_evidence/firefox-results"
export FC_E2E_HTML_REPORT_DIR="$fc_evidence/firefox-report"
export FC_E2E_JSON_PATH="$fc_evidence/firefox-results/playwright.json"
export FC_E2E_JUNIT_PATH="$fc_evidence/firefox-results/junit.xml"
npm --prefix tests/e2e test -- specs/route-contract.spec.ts \
    --project=firefox --grep 'module workspace palette activation reaches the generated full-page command' \
    --workers=1 --retries=0 2>&1 | tee "$fc_evidence/firefox-route-contract.log"
