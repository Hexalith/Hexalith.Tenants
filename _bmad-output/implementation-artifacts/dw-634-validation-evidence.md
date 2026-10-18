# DW-634 validation evidence — 2026-10-18

Entry revision: `20293a86babf337299639770efb3209aac9f192e`. During investigation,
external `/pushall` work introduced `8c1e1aa5fe579884477ebfe1454b95bc0a5815fc`
and `132630d14e54981ef7e99bfe15ce70fd887371c3`. Those dependency updates are
preserved. The five-file ownership check uses the latter revision for scope
and the entry revision for lossless ledger comparisons.

## Result and scope

DW-634 closes with deterministic disabled-bootstrap lifetime-token non-access
assertions and an enabled control. Production returns before reading
ApplicationStopping/ApplicationStarted when disabled. The test proves that
no started token was obtained for registration, before and after startup is
signaled. It retains the configured administrator and disabled diagnostic.
The enabled control observes both lifetime getters and awaits actual HTTP
work, recording that startup was signaled at send time. Following review it
also awaits observed scope disposal, checks one request and requires the
successful diagnostic before disposing its real scope. It proves the observed
send ordering; it does not claim deterministic rejection of every alternate
eager-queueing implementation. Neither case uses a fixed 50 ms delay. These checks use public lifetime behavior and NSubstitute,
without reflection, new fixture types or production changes.

The final ordinary Debug source build has **0 warnings / 0 errors**. The
maintained bootstrap class passes **85/85**; full Server passes **1316/1316**;
both have **0 failures / 0 skips**. These are project-based Microsoft.Testing.Platform
runs. No package-mode, Release, other test project or distributed acceptance
claim is made. Existing release qualification holds remain.

## Ordinary build and maintained runner

Run from the repository root. Commands below exit 0. The first bootstrap run
preceded the final enabled-control assertion; final runs cover the complete
change. The two initial builds also exit 0 with zero warnings/errors.

```bash
mkdir -p /tmp/dw-634
dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug -m:1 -nr:false -p:UseHexalithProjectReferences=true > /tmp/dw-634/build-baseline.log 2>&1
dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug -m:1 -nr:false -p:UseHexalithProjectReferences=true > /tmp/dw-634/build-tests.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-build --no-restore --filter-class '*TenantBootstrapHostedServiceTests' --minimum-expected-tests 1 --no-ansi --no-progress --results-directory /tmp/dw-634/bootstrap --report-xunit-trx --report-xunit-trx-filename bootstrap.trx > /tmp/dw-634/bootstrap.log 2>&1
```

## Missing-disabled-return control

Only the temporary production compile item changes. The disabled diagnostic
is retained while its early return is removed. The normal repository source
is untouched. Generate the overlay with:

```bash
python3 - <<'PYCONTROL'
from pathlib import Path
source = Path('src/Hexalith.Tenants/Bootstrap/TenantBootstrapHostedService.cs').read_text()
old = """        if (!options.Value.BootstrapGlobalAdminEnabled) {
            Log.BootstrapDisabled(logger);
            return Task.CompletedTask;
        }"""
if source.count(old) != 1:
    raise RuntimeError('Expected exactly one disabled early return')
Path('/tmp/dw-634/TenantBootstrapHostedService.MissingReturn.cs').write_text(source.replace(old, old.replace('            return Task.CompletedTask;\n', '')))
Path('/tmp/dw-634/missing-return.targets').write_text("""<Project>
  <Target Name="DW634MissingDisabledReturn" BeforeTargets="CoreCompile" Condition="'$(MSBuildProjectName)' == 'Hexalith.Tenants'">
    <ItemGroup>
      <Compile Remove="Bootstrap/TenantBootstrapHostedService.cs;$(MSBuildProjectDirectory)/Bootstrap/TenantBootstrapHostedService.cs" />
      <Compile Include="/tmp/dw-634/TenantBootstrapHostedService.MissingReturn.cs" Link="Bootstrap/TenantBootstrapHostedService.cs" />
    </ItemGroup>
  </Target>
</Project>
""")
PYCONTROL
cat > /tmp/dw-634/run-review-control.py <<'PYRUN'
from pathlib import Path
import subprocess
import xml.etree.ElementTree as E

project = 'tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj'
build = ['dotnet', 'build', project, '--configuration', 'Debug', '-m:1', '-nr:false', '-p:UseHexalithProjectReferences=true']
test = ['dotnet', 'test', '--project', project, '--configuration', 'Debug', '--no-build', '--no-restore', '--filter-class', '*TenantBootstrapHostedServiceTests', '--minimum-expected-tests', '85', '--no-ansi', '--no-progress', '--results-directory', '/tmp/dw-634/review-missing-return', '--report-xunit-trx', '--report-xunit-trx-filename', 'review-missing-return.trx']
try:
    with Path('/tmp/dw-634/build-post-review-missing-return.log').open('w') as log:
        subprocess.run(build + ['-p:CustomAfterMicrosoftCommonTargets=/tmp/dw-634/missing-return.targets'], stdout=log, stderr=subprocess.STDOUT, check=True)
    with Path('/tmp/dw-634/review-missing-return.log').open('w') as log:
        result = subprocess.run(test, stdout=log, stderr=subprocess.STDOUT)
    if result.returncode != 2:
        raise RuntimeError(f'Expected mutation test exit 2, got {result.returncode}')
    ns = {'t': 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
    root = E.parse('/tmp/dw-634/review-missing-return/review-missing-return.trx').getroot()
    counts = root.find('t:ResultSummary/t:Counters', ns).attrib
    failed = [r for r in root.findall('t:Results/t:UnitTestResult', ns) if r.attrib['outcome'] != 'Passed']
    if (counts['total'], counts['passed'], counts['failed'], counts['notExecuted']) != ('85', '84', '1', '0'):
        raise RuntimeError('Unexpected mutation counts')
    if len(failed) != 1 or not failed[0].attrib['testName'].endswith('StartAsyncWithDisabledBootstrapPreservesConfiguredAdministrator'):
        raise RuntimeError('Unexpected mutation failure identity')
    print('PASS: missing disabled return fails exactly the disabled-registration assertion; 84 passed / 1 expected failure / 0 skipped.')
finally:
    with Path('/tmp/dw-634/build-post-review-final.log').open('w') as log:
        subprocess.run(build, stdout=log, stderr=subprocess.STDOUT, check=True)
    print('Restored ordinary production compile outputs with a successful normal-source build.')
PYRUN
python3 /tmp/dw-634/run-review-control.py > /tmp/dw-634/review-control.log 2>&1
```

The wrapper restores normal compile outputs in `finally`, including when the
mutation build, expected failure validation or result parsing raises. A forced
process kill can still interrupt any rebuild; perform a normal build if the
wrapper did not report successful restoration. The initial, manually restored
mutation run built with zero warnings/errors and the class exited **2**:
**84 passed / 1 failed / 0 skipped**. The enabled control still passes.
The only failure is the intended synchronous assertion:

```text
StartAsyncWithDisabledBootstrapPreservesConfiguredAdministrator
NSubstitute.Exceptions.ReceivedCallsException
Expected to receive no calls matching:
    ApplicationStarted
Actually received 1 matching call:
    ApplicationStarted
```

An earlier overlay build also passed before the enabled control was refined.
It produced no additional test result. The evidence does not claim the old
50 ms test necessarily fails: the original gap was a scheduling-dependent
negative proof, not broken production behavior.

The initial manual restoration and verification used the following commands;
all exited 0. The wrapper above is the supported mutation reproduction path.
Post-review normal-source runs and receipts are recorded below:

```bash
dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug -m:1 -nr:false -p:UseHexalithProjectReferences=true > /tmp/dw-634/build-final.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-build --no-restore --filter-class '*TenantBootstrapHostedServiceTests' --minimum-expected-tests 85 --no-ansi --no-progress --results-directory /tmp/dw-634/final-bootstrap --report-xunit-trx --report-xunit-trx-filename final-bootstrap.trx > /tmp/dw-634/final-bootstrap.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-build --no-restore --minimum-expected-tests 1316 --no-ansi --no-progress --results-directory /tmp/dw-634/server --report-xunit-trx --report-xunit-trx-filename server.trx > /tmp/dw-634/server.log 2>&1
```

## Runtime baseline

An AppHost was already running when checked. Read-only commands exit 0:

```bash
aspire ps --format Json
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json > /tmp/dw-634/aspire-describe.json
aspire otel logs --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json > /tmp/dw-634/aspire-otel-logs.json
```

Tenants, tenants-api, tenants-ui, security and EventStore report running/healthy.
The Memories Dapr executable reports finished; other observed resources report
running/healthy. This state observation does not qualify the distributed topology.
The existing AppHost was neither restarted nor stopped. The tests use their own
in-process fake HTTP/lifetime dependencies and do not call the running topology.

## Ledger preservation

Only DW-634 changes disposition. Original fields remain, including the migrated
finding; replaced values are retained under `pre-dw-634-*`. All 655 IDs remain:
379 done / 276 open, including 189 actionable, across 248 index rows. Every open
ID appears exactly once in that index. Archive, sprint, production, dependency
pointers, unrelated records and historical preambles are unchanged by this work.

Run the following from the repository root (or save as
`/tmp/dw-634/check-preservation.py` and execute with Python). It checks both
tracked changes and nonignored untracked paths. It uses explicit exceptions,
so Python optimization cannot disable the checks:

```python
from collections import Counter
from pathlib import Path
import re
import subprocess

entry = '20293a86babf337299639770efb3209aac9f192e'
scope_base = '132630d14e54981ef7e99bfe15ce70fd887371c3'
ledger = '_bmad-output/implementation-artifacts/deferred-work.md'
index = 'docs/maintenance-backlog.md'
artifact = '_bmad-output/implementation-artifacts/'
original = lambda p: subprocess.check_output(['git', 'show', entry + ':' + p]).decode()
fields = lambda s: dict(re.findall(r'^([\w-]+): (.*)$', s, re.M))

def require(condition, message):
    if not condition:
        raise RuntimeError(message)

def records(s):
    matches = list(re.finditer(r'^### DW-(\d+):.*?(?=^### DW-|\Z)', s, re.M | re.S))
    result = {int(m[1]): m[0] for m in matches}
    require(len(result) == len(matches), 'Duplicate IDs')
    return result

before, after = original(ledger), Path(ledger).read_text()
old, new = records(before), records(after)
require(set(old) == set(new) and len(new) == 655, 'IDs changed')
require([n for n in old if old[n] != new[n]] == [634], 'Unrelated record changed')
a, b = fields(old[634]), fields(new[634])
changed = {'status', 'disposition', 'verification-date', 'verification', 'evidence-kind', 'next-action'}
for key, value in a.items():
    require(b.get('pre-dw-634-' + key if key in changed else key) == value, 'Lost original ' + key)
require(b['status'] == 'done 2026-10-18' and b['disposition'] == 'resolved', 'Not closed')
strip_banner = lambda s: re.sub(r'^\*\*DW-634 closure.*\n\n', '', s, count=1, flags=re.M)
require(strip_banner(after[:after.index('### DW-1:')]) == before[:before.index('### DW-1:')], 'Historical preamble changed')
counts = Counter('done' if fields(body)['status'].startswith('done') else fields(body)['disposition'] for body in new.values())
require(counts == {'done': 379, 'actionable': 189, 'blocked': 15, 'awaiting-decision': 26, 'accepted-limitation': 9, 'uncertain': 37}, 'Counts changed')
text = Path(index).read_text()
rows = [line for line in text.splitlines() if line.startswith('| DW-')]
ids = [int(n) for row in rows for n in re.findall(r'DW-(\d+)', row.split('|')[1])]
open_ids = {n for n, body in new.items() if fields(body)['status'] == 'open'}
require(len(rows) == 248 and len(open_ids) == 276 and set(ids) == open_ids and len(ids) == len(set(ids)), 'Index coverage changed')
expected = re.sub(r'^\| DW-634 \|.*\n', '', original(index), flags=re.M)
expected = expected.replace('| done | 378 |', '| done | 379 |', 1).replace('| actionable | 190 |', '| actionable | 189 |', 1)
expected = expected.replace('**Remaining: 277 open records across 249 index rows.**', '**Remaining: 276 open records across 248 index rows.**', 1)
require(strip_banner(text) == expected, 'Unrelated index content changed')
allowed = {ledger, index, 'tests/Hexalith.Tenants.Server.Tests/Bootstrap/TenantBootstrapHostedServiceTests.cs', artifact + 'spec-dw-634-disabled-bootstrap-registration.md', artifact + 'dw-634-validation-evidence.md'}
paths = subprocess.check_output(['git', 'diff', '--name-only', scope_base, '--']).decode().splitlines()
untracked = subprocess.check_output(['git', 'ls-files', '--others', '--exclude-standard']).decode().splitlines()
require(set(paths + untracked) == allowed, 'Changed paths differ from the five owned files')
production = 'src/Hexalith.Tenants/Bootstrap/TenantBootstrapHostedService.cs'
require(original(production).replace('\r\n', '\n') == Path(production).read_text(), 'Runtime source changed')
source = Path('tests/Hexalith.Tenants.Server.Tests/Bootstrap/TenantBootstrapHostedServiceTests.cs').read_bytes()
require(source.count(b'\n') == source.count(b'\r\n'), 'Test file has non-CRLF lines')
print('PASS: DW-634-only closure; 655 IDs; 379 done / 276 open; 189 actionable; 248 exact-covering index rows; original fields/history and production source preserved; five owned files; CRLF tests.')
```

`python3 /tmp/dw-634/check-preservation.py` exits 0 and reports the DW-634-only
closure and the counts above. `git diff --check` exits 0; Git emits its existing
CRLF-to-LF working-copy notice for the C# file. The file remains CRLF on disk.

## Execution receipts

| Run | Total | Passed | Failed | Start UTC | Finish UTC |
| --- | ---: | ---: | ---: | --- | --- |
| Initial bootstrap | 85 | 85 | 0 | 2026-10-18T20:45:56.3574095+00:00 | 2026-10-18T20:45:59.0892771+00:00 |
| Missing-return mutation | 85 | 84 | 1 | 2026-10-18T20:47:04.3872447+00:00 | 2026-10-18T20:47:07.3028455+00:00 |
| Final bootstrap | 85 | 85 | 0 | 2026-10-18T20:47:26.7589949+00:00 | 2026-10-18T20:47:29.4500657+00:00 |
| Final full Server | 1316 | 1316 | 0 | 2026-10-18T20:47:38.7687395+00:00 | 2026-10-18T20:47:41.9815869+00:00 |

| Retained artifact | SHA256 |
| --- | --- |
| `/tmp/dw-634/build-baseline.log` | `50bb2cb20e209c34d9fe549cfb6080dd958c09f08809972eb9934c829cb09b3c` |
| `/tmp/dw-634/build-tests.log` | `33e072f548a4b9b16c55b794f984a39db31eefbfe3327468f17bb90bebdce980` |
| `/tmp/dw-634/build-missing-return-final-tests.log` | `a048848adbbaa2017f40300e938ded6795e4fad10fd39908b648271a5f800ae2` |
| `/tmp/dw-634/build-final.log` | `3834d53e967fcab98026c964e5884c523903ba76f6c71ca7c123b406d62b2f61` |
| `/tmp/dw-634/bootstrap/bootstrap.trx` | `0f96b5f4c8a20669800806d3b0fd3f86860f8e33f45f7c48f7096e589f170c14` |
| `/tmp/dw-634/missing-return/missing-return.trx` | `608d0acc53c68743d84c1641adea55caa8ad65ec052a6d9eecae5b7bad45b5ce` |
| `/tmp/dw-634/final-bootstrap/final-bootstrap.trx` | `3b18d8c637913cbb509a4f8d3834fb3f00e7ca4acff95a81c909610d300a855f` |
| `/tmp/dw-634/server/server.trx` | `edb05871aef7e78f11042fc3bc98d4a0846b00fa5363b74bcfe64b9396f292ce` |
| `/tmp/dw-634/commitlint.log` | `95d319eca1cc2a174bc8e98884dd0bf7cd99e952410c613ab832d04c7314b6ff` |

## Workflow review and commit validation

The configured context-free Blind Hunter reviewed all five owned files; no
configured review layer was skipped. All six findings are retained with their
verdicts and evidence in the specification. Five findings prompted narrower
claims, completion/count checks, precise banner wording and finally-based
mutation restoration. The raw `/tmp` logs are temporary; the committed tables,
exit results, UTC times, counts, diagnostic excerpt and hashes are the durable
compact receipts, with commands to regenerate the raw reports. No full raw-log
retention or immutable artifact-store guarantee is claimed.

The owning pinned `@commitlint/cli@21.2.3` accepts the exact full candidate
`test(bootstrap): prove disabled startup registers no work` with exit 0,
0 problems / 0 warnings:

```bash
node_modules/.bin/commitlint --edit /tmp/dw-634/commit-message.txt --verbose > /tmp/dw-634/commitlint.log 2>&1
```

The explicitly invoked workflow requires a local commit. It does not request a push.

## Post-review final verification

The wrapper described above was executed against the final test source. It
exits 0 after validating exactly 84 passing cases and the single intended
disabled assertion failure (test-process exit 2), then restoring normal-source
outputs in finally. Both its mutation and restoration builds have zero
warnings/errors. The wrapper transcript reports successful restoration.

These final maintained commands exit 0:

```bash
dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-build --no-restore --filter-class '*TenantBootstrapHostedServiceTests' --minimum-expected-tests 85 --no-ansi --no-progress --results-directory /tmp/dw-634/post-review-bootstrap --report-xunit-trx --report-xunit-trx-filename post-review-bootstrap.trx > /tmp/dw-634/post-review-bootstrap.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-build --no-restore --minimum-expected-tests 1316 --no-ansi --no-progress --results-directory /tmp/dw-634/post-review-server --report-xunit-trx --report-xunit-trx-filename post-review-server.trx > /tmp/dw-634/post-review-server.log 2>&1
python3 /tmp/dw-634/check-preservation.py
git diff --check
```

Final bootstrap: **85 passed / 0 failed / 0 skipped**. Final Server:
**1316 passed / 0 failed / 0 skipped**. The preservation checker and diff check
also pass. The mutation again fails at the ApplicationStarted non-access
assertion, and the enabled positive control passes. No new deferral is added.

The post-review normal build retains this compact successful receipt:

```text
Build succeeded.
    0 Warning(s)
    0 Error(s)
Exit code: 0
```

| Review run | Total | Passed | Failed | Start UTC | Finish UTC |
| --- | ---: | ---: | ---: | --- | --- |
| Missing-return mutation | 85 | 84 | 1 | 2026-10-18T20:53:51.7237109+00:00 | 2026-10-18T20:53:55.1288616+00:00 |
| Final bootstrap | 85 | 85 | 0 | 2026-10-18T20:54:27.9711532+00:00 | 2026-10-18T20:54:31.1105669+00:00 |
| Final full Server | 1316 | 1316 | 0 | 2026-10-18T20:54:35.0670837+00:00 | 2026-10-18T20:54:39.4864337+00:00 |

| Review artifact | SHA256 |
| --- | --- |
| `/tmp/dw-634/build-post-review-missing-return.log` | `718d4eafb2719c462d3169492d8d1d07f16da0da824f63ea3e7743272473d003` |
| `/tmp/dw-634/build-post-review-final.log` | `df1f5e187c1ba68973ef40ec3a30600b254d7d3a77ee49310845a071bd7b1ea7` |
| `/tmp/dw-634/review-control.log` | `45a42e57c78e485bef3639fd7f37cfbff23d35f9d1e6b5e70bf0f43e2000acb6` |
| `/tmp/dw-634/review-missing-return/review-missing-return.trx` | `2fc7236a94675f891c3ddd95b21e69887cc69282275eefb192a0a89c8a40793b` |
| `/tmp/dw-634/post-review-bootstrap/post-review-bootstrap.trx` | `6c9654b872dd632d9a558a0225e6511d2e1d88b64a2258392e902fe07232fe6a` |
| `/tmp/dw-634/post-review-server/post-review-server.trx` | `dd387598238b3acc172b706025504d9a9e2c9674437690bc46fd71af5488ae52` |

The independent correction recheck found no remaining material problems. It
confirmed the documented limits, receipt hashes, mutation failure, zero-warning
builds, lossless preservation and full Server counts, and independently reran
the bootstrap class at 85/85 with zero failures/skips. All 15 recorded artifact
hashes match; the documented finally-based wrapper exactly matches the executed
file. No finding is deferred and no review layer was skipped. The specification
is finalized as done; earlier pending notes are historical.
