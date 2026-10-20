# DW-158 validation evidence — 2026-10-20

Entry revision: `b8470b6aa0568270576811b13674cea91a6c086b`, clean main.

## Scope

DW-158 is the next dependency-free actionable Tenants item after DW-155. Earlier open actionable records depend on another owner, privacy decisions or transport/security design. This change affects only `TenantReadRefreshSubscriptionTests.cs`: the sole existing wait supplies the name `the first successful refresh callback`, and expiry of its unchanged two-second cancellable polling delay becomes a Shouldly assertion identifying that condition. The catch surrounds only Task.Delay; predicate exceptions remain outside it. No production source, dependency, new public API, browser or assistive-technology behavior changes or qualification are claimed.

Seven final cases cover immediate success, success on the next poll, two different unmet-condition descriptions with an elapsed-time lower bound, and unchanged predicate exception identity for InvalidOperationException and OperationCanceledException after an initial false evaluation, including cancellation raised from a synchronous predicate held past the helper deadline. The existing real notification/sibling-callback test remains included in focused acceptance.

## Build and initial failures

Every build uses this exact command, from the repository root:

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 -v:q
```

All executed builds exit 0 with zero warnings/errors. Local build logs are in `/tmp/dw158/`: `build-original-helper.log`, `build-original-helper-corrected.log`, `build-fixed.log`, `build-final.log`, `final-original-helper/build.log`, and `build-restored.log`. The first two add the description parameter and tests but preserve the original helper body; the final-original replay uses the final tests with only the repaired delay catch removed. `build-final.log` predates that replay; `build-restored.log` rebuilds the restored intended source afterward.

Test invocations use the maintained Microsoft.Testing.Platform project lane, not a direct-assembly fallback. Each includes `-c Debug --no-build --no-restore --no-ansi --no-progress --report-xunit-trx --report-xunit-trx-filename results.trx`, plus these filters/result directories:

| Run | Additional arguments | Exit and result |
| --- | --- | --- |
| Initial original helper | `--filter-method '*WaitHelper*' --minimum-expected-tests 5 --results-directory /tmp/dw158/original-helper` | 2; 3 passed, 2 failed, 0 skipped. One intended timeout failure and one fixture failure: Shouldly's Task overload substitutes TaskCanceledException, preventing the identity assertion. |
| Corrected original helper | `--filter-method '*WaitHelper*' --minimum-expected-tests 6 --results-directory /tmp/dw158/original-helper-corrected` | 2; 4 passed, 2 failed, 0 skipped. Both named-timeout cases fail; direct await now preserves predicate exception identity. |
| Initial repaired helper | `--filter-class '*TenantReadRefreshSubscriptionTests' --minimum-expected-tests 29 --results-directory /tmp/dw158/focused` | 2; 27 passed, 2 failed, 0 skipped. Shouldly's async exception assertion rethrows ShouldAssertException, so both timeout tests require direct typed capture too. |
| Final tests/original helper | `--filter-method '*WaitHelper*' --minimum-expected-tests 6 --results-directory /tmp/dw158/final-original-helper` | 2; 4 passed, 2 failed, 0 skipped. Exactly both timeout cases escape as TaskCanceledException; all success/predicate controls pass. |

Prefix each row with `dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj`. Logs are `tests-original-helper.log`, `tests-original-helper-corrected.log`, `tests-focused.log` and `final-original-helper/tests.log`; the first three result directories retain their exact TRX. The six-case original-helper replay directory was later superseded as detailed below; its original JSON counters remain. The corrected final timeout tests directly catch only ShouldAssertException and assert its full message, so original cancellation cannot satisfy them.

## Reproduce the original-helper control

The complete final driver below temporarily removes only the repaired catch, builds and runs the seven reviewed cases, checks the exact failure inventory and recorded exception type, and restores the exact entry bytes in finally. Matching works for LF and CRLF input. A fresh normal build is required after it, because its binaries contain the original helper. Run from the owning repository root with no concurrent build/edit of this test file. The actual execution exits 0; its test subprocess exits 2 as expected. `/tmp/dw158/final-original-helper-replay.json` retains the initial six-case counters/names. The later seven-case replay supersedes the original directory; its exact logs/TRX were copied to `reviewed-original-helper`, and `final-original-helper-replay-reviewed.json` retains its counters/names. It has 5 passed, exactly 2 expected timeout failures and zero skips, with TaskCanceledException checked in both failed results. The embedded final procedure writes directly to the reviewed directory.

```python
from pathlib import Path
import json
import subprocess
import xml.etree.ElementTree as ET

root = Path.cwd()
source = root / 'tests/Hexalith.Tenants.UI.Tests/Services/TenantReadRefreshSubscriptionTests.cs'
entry_bytes = source.read_bytes()
intended = entry_bytes.replace(b'\r\n', b'\n')
fixed = b'''            try\r\n            {\r\n                await Task.Delay(10, timeout.Token);\r\n            }\r\n            catch (OperationCanceledException) when (timeout.IsCancellationRequested)\r\n            {\r\n                throw new ShouldAssertException($"Timed out after two seconds waiting for {conditionDescription}.");\r\n            }'''
fixed = fixed.replace(b'\r\n', b'\n')
original = b'            await Task.Delay(10, timeout.Token);'
if intended.count(fixed) != 1:
    raise RuntimeError('Expected exactly one repaired helper')
output = Path('/tmp/dw158/reviewed-original-helper')
output.mkdir(parents=True, exist_ok=True)
project = 'tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj'
build = ['dotnet', 'build', project, '-c', 'Debug', '-p:UseNuGetDeps=false', '-m:1', '-v:q']
test = ['dotnet', 'test', '--project', project, '-c', 'Debug', '--no-build', '--no-restore', '--filter-method', '*WaitHelper*', '--minimum-expected-tests', '7', '--no-ansi', '--no-progress', '--results-directory', str(output), '--report-xunit-trx', '--report-xunit-trx-filename', 'results.trx']
try:
    source.write_bytes(intended.replace(fixed, original))
    with (output / 'build.log').open('w') as log:
        built = subprocess.run(build, stdout=log, stderr=subprocess.STDOUT)
    if built.returncode:
        raise RuntimeError('Original-helper build failed')
    with (output / 'tests.log').open('w') as log:
        tested = subprocess.run(test, stdout=log, stderr=subprocess.STDOUT)
    trx = ET.parse(output / 'results.trx').getroot()
    ns = {'t': trx.tag.split('}')[0][1:]}
    counts = trx.find('.//t:Counters', ns).attrib
    failures = [r.attrib['testName'] for r in trx.findall('.//t:UnitTestResult', ns) if r.attrib['outcome'] != 'Passed']
    if tested.returncode != 2 or counts['total'] != '7' or counts['failed'] != '2' or counts['passed'] != '5' or len(failures) != 2 or any('WaitHelperReportsTheUnmetCondition' not in name for name in failures):
        raise RuntimeError(f'Unexpected original-helper result: {tested.returncode}, {counts}, {failures}')
    failed_rows = [r for r in trx.findall('.//t:UnitTestResult', ns) if r.attrib['outcome'] != 'Passed']
    if any('TaskCanceledException' not in ''.join(r.itertext()) for r in failed_rows):
        raise RuntimeError('Timeout failures do not record TaskCanceledException')
    print(json.dumps({'build_exit': built.returncode, 'test_exit': tested.returncode, 'counts': counts, 'failed_tests': failures}, indent=2))
finally:
    source.write_bytes(entry_bytes)
```

## Runtime baseline and limits

```bash
aspire ps --format Json
aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --isolated --no-build --format Json
aspire wait tenants-ui --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --timeout 45 --non-interactive
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire otel logs --severity Error --limit 3 --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json --non-interactive
aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --non-interactive
```

All commands exit 0. Another Parties AppHost was already running; the isolated Tenants AppHost was started for the required baseline and stopped before test builds. Tenants UI reaches healthy in 4.6 seconds, and all 23 resources report Running/Healthy. The three bounded Error entries still include InitializeTenantRegistryActivity failures and HTTP 500 retries. This does not establish entirely healthy topology, browser or live SignalR qualification. The independent Parties process is left running. Local state/telemetry logs are `/tmp/dw158/aspire-{start,describe,errors}.json`, `aspire-wait.txt` and `aspire-stop.txt`; token-bearing start output is not copied here.

## Initial acceptance (before review corrections)

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 -v:q
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-class '*TenantReadRefreshSubscriptionTests' --minimum-expected-tests 29 --no-ansi --no-progress --results-directory /tmp/dw158/focused-final --report-xunit-trx --report-xunit-trx-filename results.trx
```

The restored intended-source build exits 0 with zero warnings/errors (`build-restored.log`), and the maintained subscription class passes 29/29 with zero failures/skips (`tests-focused-final.log`, `focused-final/results.trx`). The six new cases account for the increase from 23 subscription cases.

## Review corrections and final reviewed build

The configured Blind Hunter reviewer found four qualification improvements. All four were accepted and independently rechecked: the driver matches LF/CRLF input while restoring exact bytes; both named timeout cases enforce elapsed time greater than 1.9 seconds without a fragile upper bound; a seventh case holds synchronous predicate evaluation for three seconds before raising its original cancellation exception; and the driver verifies TaskCanceledException in both failed results, in addition to exact names/counts. The three-second hold is deliberate test input to cross the helper deadline, not production polling behavior. The reviewer confirms no new concrete issue; no finding is deferred.

The reviewed original-helper build has zero warnings/errors and the seven-case lane has 5 passed/2 intended failures/0 skipped, with exact logs/TRX in `/tmp/dw158/reviewed-original-helper`. The final intended-source rebuild (`build-reviewed.log`) exits 0 with zero warnings/errors.

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-class '*TenantReadRefreshSubscriptionTests' --minimum-expected-tests 30 --no-ansi --no-progress --results-directory /tmp/dw158/focused-reviewed --report-xunit-trx --report-xunit-trx-filename results.trx
dotnet format whitespace tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --include /home/administrator/projects/hexalith/tenants/tests/Hexalith.Tenants.UI.Tests/Services/TenantReadRefreshSubscriptionTests.cs --verify-no-changes --no-restore
```

The final maintained subscription lane passes 30/30 with zero failures/skips (`tests-focused-reviewed.log`, `focused-reviewed/results.trx`). Scoped formatting exits 0 with the generic workspace-load warning and no formatting diagnostic (`format-reviewed.log`). This is a scoped whitespace check, not whole-repository formatting qualification.

## Final reviewed acceptance and closure

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 6580 --no-ansi --no-progress --results-directory /tmp/dw158/full-reviewed --report-xunit-trx --report-xunit-trx-filename results.trx
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-158-subscription-wait-diagnostics.md
git diff --check
node_modules/.bin/commitlint --edit /tmp/dw158/commit-message.txt --verbose
git fetch origin
git rev-list --left-right --count main...origin/main
```

The final full UI project passes 6580/6580 with zero failures/skips in 2m32s (`tests-full-reviewed.log`, `full-reviewed/results.trx`); the final focused class passes 30/30. The restored reviewed Debug source build has zero warnings/errors. Independent review confirms all four findings resolved and no new concrete issue; no follow-up is deferred. Scoped formatting passes as described above. Own-spec gitlink and whitespace checks pass; Git reports only the existing CRLF-to-LF normalization notice for the edited C# file.

Closure was gated on the exact final focused/full TRX counts, unchanged entry ledger/index snapshots and equality of all unrelated record bodies. Only DW-158 changes; every replaced historical field is retained as pre-dw-158-*. All 656 IDs remain: 398 done/258 open, with 233 maintenance rows covering all open IDs exactly once. Archive, sprint, production sources, dependency pointers and release qualification holds remain preserved (`/tmp/dw158/ledger-closure.txt`).

Pinned commitlint 21.2.3 validates the exact full message `test(ui): name subscription wait timeout failures` with zero problems/warnings (`commitlint.txt`). The first origin fetch succeeds with `0 0` divergence. The final pre-commit fetch reports `0 2`: origin/main advanced through `18e73d5b43abd687199db3313467b5a60c0d2691` to `9a94131a26d0c5d80d5540706e267ee123d54b68`. Their only tree change adds the Parties caller and its GET /ready operation to `module/hexalith.module-manifest.v2.json`; no tested UI source, tests or dependency pointer changes. The local commit will be rebased onto that exact upstream revision before push, preserving the incoming work. The executed full UI results above remain the tested entry-source evidence; no combined-tree full rerun is claimed. The user explicitly authorizes commit/push on main; no release workflow is dispatched.
