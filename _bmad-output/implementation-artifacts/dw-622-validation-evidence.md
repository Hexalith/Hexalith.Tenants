# DW-622 validation evidence — 2026-10-18

Entry revision: `482b04769855531ec5630e01b4c0403444bad0d5`.

Two new cases evaluate the actual, non-started AppHost in Keycloak and symmetric authentication modes. They allocate the API HTTPS endpoint, execute every UI environment callback in order with a five-second cancellation bound, normalize equivalent configuration keys, and assert the final read binding's resource, endpoint name and resolved URL. The maintained AppHost project is already in the blocking integration lane in `.github/workflows/ci.yml`; no CI wiring change is needed.

## Maintained checks

From the repository root:

```bash
mkdir -p /tmp/tenants-dw-622
dotnet build tests/Hexalith.Tenants.AppHost.Tests/Hexalith.Tenants.AppHost.Tests.csproj -c Debug -m:1 -nr:false -p:UseHexalithProjectReferences=true > /tmp/tenants-dw-622/build.log 2>&1
dotnet test --project tests/Hexalith.Tenants.AppHost.Tests/Hexalith.Tenants.AppHost.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 8 --no-ansi --no-progress --results-directory /tmp/tenants-dw-622/initial --report-xunit-trx --report-xunit-trx-filename apphost.trx > /tmp/tenants-dw-622/initial-tests.log 2>&1
```

The initial ordinary Debug source build exits 0 with zero warnings/errors. The initial maintained project run passes 8/8 with zero failures/skips. After review corrections, the ordinary-source restoration also passes 8/8. Repeated runs do not increase the number of unique cases: six existing cases plus two new cases.

## Override qualification and restoration

Each mutation appends a later environment callback while retaining the correct original production registration. Temporary compile overlays replace only the AppHost `Program.cs` compile item; repository production source is never rewritten. The final wrapper exits 0 and validates exact failure identities, Shouldly assertion failures, exit codes, counts and no skips. Every mutation fails only `UiReadsResolveToTenantsApiHttpsInBothAuthenticationModes`, once per mode; six existing tests remain green.

| AppHost variant | Passed | Failed | Skipped | Test exit | Finished UTC |
| --- | ---: | ---: | ---: | ---: | --- |
| late-eventstore-override | 6 | 2 | 0 | 2 | 2026-10-18T21:15:07.6166878+00:00 |
| late-http-override | 6 | 2 | 0 | 2 | 2026-10-18T21:15:21.0625240+00:00 |
| late-uppercase-alias | 6 | 2 | 0 | 2 | 2026-10-18T21:15:36.5410663+00:00 |
| late-colon-alias | 6 | 2 | 0 | 2 | 2026-10-18T21:15:51.2230445+00:00 |
| late-allocated-override | 6 | 2 | 0 | 2 | 2026-10-18T21:16:07.3167884+00:00 |
| restored | 8 | 0 | 0 | 0 | 2026-10-18T21:16:19.8236968+00:00 |

The EventStore and allocation-dependent variants fail resource identity; the HTTP variant fails the endpoint-name assertion; both alias variants fail the single normalized binding assertion. The ordinary-source restoration passes both URL checks at `https://localhost:45123`. All twelve final isolated compilation commands (AppHost plus test project for five overlays and restoration) exit 0 with zero warnings/errors.

The first harness attempt generated a trailing apostrophe in the temporary Compile Include path. `python3 /tmp/tenants-dw-622/check-variants.py` exited 1 because its mutation build reported `CS2001: Source file '/tmp/tenants-dw-622/late-eventstore-override.cs'' could not be found`; its finally rebuild and suite passed 8/8. This was a harness build failure, not evidence that an override was rejected. XML element construction corrects the path.

The second attempt rejected all five overrides, but an incremental ordinary-source build retained the last mutated assembly; the final suite caught this with 6 passed/2 failed (test exit 2, wrapper exit 1). Forced nonincremental AppHost/test compilation restored 8/8. The final wrapper below forces compilation for every variant and in finally, then verifies the restored suite and unchanged production source bytes. After an externally forced process termination, run those ordinary-source forced builds before using local outputs.

Reproduce the final wrapper from the repository root (after the ordinary Debug source build above):

```bash
python3 - <<'PY'
from pathlib import Path
import json
import subprocess
import xml.etree.ElementTree as ET

root = Path.cwd()
work = Path('/tmp/tenants-dw-622')
project = 'tests/Hexalith.Tenants.AppHost.Tests/Hexalith.Tenants.AppHost.Tests.csproj'
program = root / 'src/Hexalith.Tenants.AppHost/Program.cs'
source_bytes = program.read_bytes()
source = program.read_text()
needle = '    .WithEnvironment("Tenants__BaseAddress", tenantsApi.GetEndpoint("https"))'
if source.count(needle) != 1:
    raise RuntimeError('Expected exactly one production UI read endpoint registration')
build = ['dotnet', 'build', '-c', 'Debug', '-m:1', '-nr:false', '-p:UseHexalithProjectReferences=true', '--no-restore', '--no-incremental', '-p:BuildProjectReferences=false']
test = ['dotnet', 'test', '--project', project, '-c', 'Debug', '--no-build', '--no-restore', '--minimum-expected-tests', '8', '--no-ansi', '--no-progress', '--report-xunit-trx', '--report-xunit-trx-filename', 'apphost.trx']
results = []

def run_build(label, extra=()):
    for target, suffix in [('src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj', '-apphost'), (project, '-tests')]:
        with (work / (label + suffix + '-build.log')).open('w') as log:
            arguments = list(extra) if suffix == '-apphost' else []
            subprocess.run(build + [target] + arguments, stdout=log, stderr=subprocess.STDOUT, check=True)

def run_tests(label, failed):
    directory = work / label
    directory.mkdir(exist_ok=True)
    with (work / (label + '-tests.log')).open('w') as log:
        result = subprocess.run(test + ['--results-directory', str(directory)], stdout=log, stderr=subprocess.STDOUT)
    trx = ET.parse(directory / 'apphost.trx')
    counters = trx.find('.//{*}Counters').attrib
    expected_exit = 2 if failed else 0
    if result.returncode != expected_exit or int(counters['passed']) != 8 - failed or int(counters['failed']) != failed or int(counters['total']) != 8:
        raise RuntimeError(f'{label}: unexpected exit/counts {result.returncode}: {counters}')
    cases = trx.findall('.//{*}UnitTestResult')
    failures = [case for case in cases if case.attrib['outcome'] == 'Failed']
    if len(cases) != 8 or any(case.attrib['outcome'] not in ('Passed', 'Failed') for case in cases):
        raise RuntimeError(f'{label}: missing/skipped/unexpected test results')
    if len(failures) != failed or any('UiReadsResolveToTenantsApiHttpsInBothAuthenticationModes' not in case.attrib['testName'] for case in failures):
        raise RuntimeError(f'{label}: wrong failure identities')
    if any('Shouldly.ShouldAssertException' not in ''.join(case.itertext()) for case in failures):
        raise RuntimeError(f'{label}: failure was not a Shouldly assertion')
    times = trx.find('./{*}Times').attrib
    row = {'start_utc': times.get('start'), 'finish_utc': times.get('finish'), 'variant': label, 'exit': result.returncode, 'passed': 8 - failed, 'failed': failed, 'skipped': 0, 'failures': [case.attrib['testName'] for case in failures]}
    results.append(row)
    print(json.dumps(row), flush=True)

try:
    for label, key, expression in [('late-eventstore-override', 'Tenants__BaseAddress', 'eventStoreHttps'), ('late-http-override', 'Tenants__BaseAddress', 'tenantsApi.GetEndpoint("http")'), ('late-uppercase-alias', 'TENANTS__BASEADDRESS', 'eventStoreHttps'), ('late-colon-alias', 'Tenants:BaseAddress', 'eventStoreHttps'), ('late-allocated-override', None, '.WithEnvironment(context => { if (tenantsApi.GetEndpoint("https").IsAllocated) { context.EnvironmentVariables["Tenants__BaseAddress"] = eventStoreHttps; } })')]:
        overlay = work / (label + '.cs')
        addition = '.WithEnvironment("' + key + '", ' + expression + ')' if key is not None else expression
        overlay.write_text(source.replace(needle, needle + '\n    ' + addition))
        targets = work / (label + '.targets')
        target_root = ET.Element('Project')
        target = ET.SubElement(target_root, 'Target', {
            'Name': 'DW622OverrideUiReadEndpoint',
            'BeforeTargets': 'CoreCompile',
            'Condition': "'$(MSBuildProjectName)' == 'Hexalith.Tenants.AppHost'",
        })
        items = ET.SubElement(target, 'ItemGroup')
        ET.SubElement(items, 'Compile', {'Remove': 'Program.cs;$(MSBuildProjectDirectory)/Program.cs'})
        ET.SubElement(items, 'Compile', {'Include': str(overlay), 'Link': 'Program.cs'})
        ET.ElementTree(target_root).write(targets, encoding='unicode')
        run_build(label, ['-p:CustomAfterMicrosoftCommonTargets=' + str(targets)])
        run_tests(label, 2)
finally:
    run_build('restored')
    run_tests('restored', 0)
    if program.read_bytes() != source_bytes:
        raise RuntimeError('Production source changed during overlay validation')
    (work / 'variant-results.json').write_text(json.dumps(results, indent=2) + '\n')
PY
```

## Independent review and limits

The workflow's blind review identified callback evaluation before endpoint allocation/unbounded callback waits, and missed case/colon configuration aliases. Both findings were patched and recorded in the spec. A read-only correction recheck accepted both fixes without further findings. No configured review layer was skipped and no new item was deferred.

Read-only `aspire ps --format Json` and targeted `aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json` inspections found the existing EventStore, security, Tenants, API and UI resources running/healthy; the live UI read address matched the API HTTPS address. That existing session was not restarted or stopped. The new tests do not start services or perform HTTP reads; they qualify AppHost configuration, not backend authorization or end-to-end behavior. Existing release qualification holds remain.

The browser/workflow edits present at activation were externally committed during investigation. Further external browser-script edits and `dump-validation-dom.mjs` appeared during verification; they are outside DW-622 and are excluded from staging. Production AppHost wiring, dependency pointers, archive and sprint tracking are outside this change.

DW-622 alone is reconciled: 655 IDs retained, 380 done/275 open, 247 index rows covering exactly the 275 open IDs without duplicates. All other ledger blocks/index rows compare identically to the entry snapshots; original DW-622 values remain under `pre-dw-622-*`.

## Final verification and commit policy

```bash
dotnet build tests/Hexalith.Tenants.AppHost.Tests/Hexalith.Tenants.AppHost.Tests.csproj -c Debug -m:1 -nr:false -p:UseHexalithProjectReferences=true > /tmp/tenants-dw-622/final-build.log 2>&1
dotnet test --project tests/Hexalith.Tenants.AppHost.Tests/Hexalith.Tenants.AppHost.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 8 --no-ansi --no-progress --results-directory /tmp/tenants-dw-622/final --report-xunit-trx --report-xunit-trx-filename apphost.trx > /tmp/tenants-dw-622/final-tests.log 2>&1
git diff --check
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-622-effective-ui-read-endpoint.md
node_modules/.bin/commitlint --edit /tmp/tenants-dw-622/commit-message.txt --verbose
```

Final ordinary Debug source graph build exits 0 with zero warnings/errors; final maintained AppHost suite exits 0 with 8 passed/0 failed/0 skipped. Diff checks pass. The story gitlink validator reports no references pointer changes and `RESULT: PASS` against the canonical baseline. Pinned `@commitlint/cli@21.2.3` validates the complete one-line candidate below with `found 0 problems, 0 warnings` (exit 0); the message file includes its final newline.

```text
test(apphost): verify effective tenant read endpoint
```

The independent browser update was committed externally as `579ec157ac38eb30593e736042aedac4d1acd08d` during verification. Its changes are not attributed to DW-622.
