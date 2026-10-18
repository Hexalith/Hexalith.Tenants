# DW-640 validation evidence — 2026-10-18

Entry revision: `1532aef62d4532562a5837447463297e64358333`, clean `main`. The user selected DW-640. The permanent C# change adds one registration test and its options namespace import; production behavior and dependencies are unchanged.

## Effective registration coverage

`TenantsApiReadinessTests.ReadyRegistersThreeSecondGatewayTimeout` starts the actual API through `WebApplicationFactory<TenantsApi::Program>`, resolves `IOptions<HealthCheckServiceOptions>` from its service provider, and selects the unique `eventstore-gateway` registration. It asserts `Timeout == TimeSpan.FromSeconds(3)`, the `ready` tag and `FailureStatus == HealthStatus.Unhealthy`. The fixture pins `DAPR_HTTP_ENDPOINT` to a deterministic loopback origin so ambient endpoint/port settings cannot prevent registration inspection. It supplies no replacement health-check registration. Existing tests cover healthy, unhealthy and throwing gateways, direct and Dapr endpoints, invocation headers and independent liveness.

This closes the effective-registration option offered by DW-640. Coverage concerns host configuration; elapsed-time cancellation behavior and a full distributed integration-suite result are outside this evidence. The IntegrationTests project remains in its existing advisory CI tier. No CI tier or release qualification changes are claimed.

## Commands and results

Commands ran from the repository root with SDK `10.0.402`. Before review, baseline, changed-test, mutation and normal-source rebuilds all exited **0**, with **zero warnings and zero errors**, using Debug source references and the ordinary audit-enabled build. No audit suppression, dependency update or direct-assembly fallback was needed.

```bash
mkdir -p /tmp/dw-640
dotnet build tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug -m:1 -nr:false -p:UseHexalithProjectReferences=true > /tmp/dw-640/build-baseline.log 2>&1
dotnet build tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug -m:1 -nr:false -p:UseHexalithProjectReferences=true > /tmp/dw-640/build.log 2>&1
dotnet test --project tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-build --no-restore --filter-class '*TenantsApiReadinessTests' --minimum-expected-tests 6 --no-ansi --no-progress --results-directory /tmp/dw-640/readiness --report-xunit-trx --report-xunit-trx-filename readiness.trx > /tmp/dw-640/readiness.log 2>&1
```

The maintained project runner reports **6 passed, 0 failed, 0 skipped**, exit **0**. These are six unique cases, including the new registration test.

## Timeout-removal mutation

The mutation uses a temporary compile overlay. It never edits repository production source. The overlay replaces only the API's `Program.cs` compile item with a copy omitting the timeout argument. Generate it as follows:

```bash
python3 - <<'PY'
from pathlib import Path
source = Path('src/Hexalith.Tenants.Api/Program.cs').read_text()
old = 'tags: ["ready"],\n    timeout: TimeSpan.FromSeconds(3));'
if source.count(old) != 1:
    raise RuntimeError('Expected exactly one production timeout registration')
Path('/tmp/dw-640/Program.MissingTimeout.cs').write_text(source.replace(old, 'tags: ["ready"]);'))
Path('/tmp/dw-640/missing-timeout.targets').write_text('''<Project>
  <Target Name="DW640UseMissingTimeoutProgram" BeforeTargets="CoreCompile" Condition="'$(MSBuildProjectName)' == 'Hexalith.Tenants.Api'">
    <ItemGroup>
      <Compile Remove="Program.cs;$(MSBuildProjectDirectory)/Program.cs" />
      <Compile Include="/tmp/dw-640/Program.MissingTimeout.cs" Link="Program.cs" />
    </ItemGroup>
  </Target>
</Project>
''')
PY
dotnet build tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug -m:1 -nr:false -p:UseHexalithProjectReferences=true -p:CustomAfterMicrosoftCommonTargets=/tmp/dw-640/missing-timeout.targets > /tmp/dw-640/build-missing-timeout.log 2>&1
dotnet test --project tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-build --no-restore --filter-method '*ReadyRegistersThreeSecondGatewayTimeout' --minimum-expected-tests 1 --no-ansi --no-progress --results-directory /tmp/dw-640/missing-timeout --report-xunit-trx --report-xunit-trx-filename missing-timeout.trx > /tmp/dw-640/missing-timeout.log 2>&1
```

The mutation build succeeds. The test exits **2**, with **1 failed / 0 passed / 0 skipped**: `registration.Timeout` should be `00:00:03` but is `-00:00:00.0010000`, the infinite default. This is the intended assertion failure, rather than a build, discovery or startup failure.

Rebuild without the overlay before the final run:

```bash
dotnet build tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug -m:1 -nr:false -p:UseHexalithProjectReferences=true > /tmp/dw-640/build-final.log 2>&1
dotnet test --project tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-build --no-restore --filter-class '*TenantsApiReadinessTests' --minimum-expected-tests 6 --no-ansi --no-progress --results-directory /tmp/dw-640/final-readiness --report-xunit-trx --report-xunit-trx-filename readiness.trx > /tmp/dw-640/final-readiness.log 2>&1
```

Pre-review normal-source result: **6 passed / 0 failed / 0 skipped**, exit **0**. Repeated runs do not increase the unique case count. Post-review checks below cover the final fixture and additional failure-status assertion.

## Required Aspire baseline

```bash
aspire ps --format Json
aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --isolated --non-interactive --format Json > /tmp/dw-640/aspire-start.log 2>&1
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json > /tmp/dw-640/aspire-describe.json
aspire otel logs --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json > /tmp/dw-640/aspire-otel-logs.json
aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --non-interactive > /tmp/dw-640/aspire-stop.log 2>&1
aspire ps --format Json
```

All commands exit **0**. The initial and final process lists are empty. The isolated AppHost starts; its captured resource model shows `security-tpvghrvz` running/unhealthy and `tenants-api-whrdqaek` waiting. This records an incomplete distributed baseline, without asserting its root cause or topology readiness. Structured logs are captured locally without publishing their payloads. The AppHost started for this check is stopped successfully. The focused host tests require no external gateway, security server, Dapr or containers.

## Ledger reconciliation

Only DW-640 closes. Every original DW-640 field remains, with replaced values retained under `pre-dw-640-*`. The original finding and all unrelated records remain unchanged. All **655** IDs remain: **376 done / 279 open**, including **192 actionable**, across **251** maintenance rows covering each open ID exactly once. Historical preambles, unrelated maintenance content, production source, dependency pointers, archive and sprint tracking are preserved. Existing release holds remain.

The preservation check compares against the canonical entry revision, checks every unrelated record body, original field values, exact index edits and row coverage, and restricts changes to the test, two ledger/index files, specification and evidence. It checks tracked and nonignored untracked paths. Specification and evidence are the only new files. The executed check passes; its reproducible command and execution receipts follow below.

```bash
python3 - <<'PY'
from collections import Counter
from pathlib import Path
import re
import subprocess

base = '1532aef62d4532562a5837447463297e64358333'
ledger = '_bmad-output/implementation-artifacts/deferred-work.md'
index = 'docs/maintenance-backlog.md'
original = lambda p: subprocess.check_output(['git', 'show', base + ':' + p]).decode()
fields = lambda s: dict(re.findall(r'^([\w-]+): (.*)$', s, re.M))

def require(condition, message):
    if not condition:
        raise RuntimeError(message)

def records(s):
    matches = list(re.finditer(r'^### DW-(\d+):.*?(?=^### DW-|\Z)', s, re.M | re.S))
    result = {int(m[1]): m[0] for m in matches}
    require(len(result) == len(matches), 'Duplicate IDs')
    return result

old_text, new_text = original(ledger), Path(ledger).read_text()
old, new = records(old_text), records(new_text)
require(set(old) == set(new) and len(new) == 655, 'IDs changed')
require([n for n in old if old[n] != new[n]] == [640], 'Unrelated record changed')
a, b = fields(old[640]), fields(new[640])
changed = {'status', 'disposition', 'verification-date', 'verification', 'evidence-kind', 'next-action'}
for key, value in a.items():
    require(b.get('pre-dw-640-' + key if key in changed else key) == value, 'Lost original ' + key)
require(b['status'] == 'done 2026-10-18' and b['disposition'] == 'done', 'Not closed')
strip_banner = lambda s: re.sub(r'^\*\*DW-640 closure.*\n\n', '', s, count=1, flags=re.M)
require(strip_banner(new_text[:new_text.index('### DW-1:')]) == old_text[:old_text.index('### DW-1:')], 'Historical preamble changed')
text = Path(index).read_text()
rows = [line for line in text.splitlines() if line.startswith('| DW-')]
ids = [int(n) for row in rows for n in re.findall(r'DW-(\d+)', row.split('|')[1])]
open_ids = {n for n, body in new.items() if re.search(r'^status: open$', body, re.M)}
require(len(rows) == 251 and len(open_ids) == 279 and set(ids) == open_ids and len(ids) == len(set(ids)), 'Index coverage changed')
counts = Counter('done' if re.search(r'^status: done', body, re.M) else fields(body)['disposition'] for body in new.values())
require(counts == {'done': 376, 'actionable': 192, 'blocked': 15, 'awaiting-decision': 26, 'accepted-limitation': 9, 'uncertain': 37}, 'Counts changed')
expected = re.sub(r'^\| DW-640 \|.*\n', '', original(index), flags=re.M)
expected = expected.replace('| done | 375 |', '| done | 376 |', 1).replace('| actionable | 193 |', '| actionable | 192 |', 1)
expected = expected.replace('**Remaining: 280 open records across 252 index rows.**', '**Remaining: 279 open records across 251 index rows.**', 1)
require(strip_banner(text) == expected, 'Unrelated index content changed')
artifact = '_bmad-output/implementation-artifacts/'
allowed = {ledger, index, 'tests/Hexalith.Tenants.IntegrationTests/TenantsApiReadinessTests.cs', artifact + 'spec-dw-640-api-readiness-timeout.md', artifact + 'dw-640-validation-evidence.md'}
paths = subprocess.check_output(['git', 'diff', '--name-only', base, '--']).decode().splitlines()
untracked = subprocess.check_output(['git', 'ls-files', '--others', '--exclude-standard']).decode().splitlines()
require(set(paths + untracked).issubset(allowed), 'Unrelated tracked or untracked paths changed')
for path in ['src/Hexalith.Tenants.Api/Program.cs', 'src/Hexalith.Tenants.Api/Services/EventStoreGatewayReadinessHealthCheck.cs']:
    require(original(path).replace('\r\n', '\n') == Path(path).read_text(), 'Runtime source changed')
require(Path(artifact + 'spec-dw-640-api-readiness-timeout.md').is_file(), 'Missing spec')
require(Path(artifact + 'dw-640-validation-evidence.md').is_file(), 'Missing evidence')
print('PASS: DW-640-only closure; 655 IDs; 376 done/279 open; 192 actionable; 251 exact-covering index rows; original fields, history and unrelated tracked/untracked content preserved.')
PY
git diff --check
```

Both commands exit **0**. The checker prints the `PASS` sentence above.

## Durable execution receipts

These summaries retain the TRX counts and UTC run times; the commands above regenerate temporary logs and reports. Each run has zero skipped/not-executed cases.

| Run | Total | Passed | Failed | Start UTC | Finish UTC |
| --- | ---: | ---: | ---: | --- | --- |
| Initial readiness | 6 | 6 | 0 | 2026-10-18T17:09:27.0871123+00:00 | 2026-10-18T17:09:35.2184249+00:00 |
| Missing-timeout mutation | 1 | 0 | 1 | 2026-10-18T17:10:20.1490542+00:00 | 2026-10-18T17:10:20.7080740+00:00 |
| Final readiness | 6 | 6 | 0 | 2026-10-18T17:11:30.1668757+00:00 | 2026-10-18T17:11:38.2194892+00:00 |

| Artifact | SHA256 |
| --- | --- |
| `/tmp/dw-640/build-baseline.log` | `733729d110909d8726dd9bfd005d29af625bce82f60c8e450aec4a687394c41e` |
| `/tmp/dw-640/build.log` | `38327148e7fa57e7db12db6e76c7207cf97d4bcde19be96a6d88d62a8733ebea` |
| `/tmp/dw-640/build-missing-timeout.log` | `47fc4a40c3bbff401e001cffd158014ce0e6620a8aed9b8786dd341323cf0d31` |
| `/tmp/dw-640/build-final.log` | `b58e13905a91085ef8ba294cf51feb62278f2d5ae426c547692b766fff8617c5` |
| `/tmp/dw-640/readiness/readiness.trx` | `a103c6d5f74f738e9efc5e471ad08927bddf34db2d3ec4884361a00040181f70` |
| `/tmp/dw-640/missing-timeout/missing-timeout.trx` | `037df24bc95776eddc20c2d32775a597f5479a852a6593860f68cf292e16ffc4` |
| `/tmp/dw-640/final-readiness/readiness.trx` | `d0ddff2422a2ab59916473420e4b09053bfa8e444999f5ed0b8d7e2b81a908a3` |

## Retained timeout failure diagnostic

The mutation run retained above reports this method and assertion:

```text
failed Hexalith.Tenants.IntegrationTests.TenantsApiReadinessTests.ReadyRegistersThreeSecondGatewayTimeout
Shouldly.ShouldAssertException : registration.Timeout
    should be
00:00:03
    but was
-00:00:00.0010000
Test run summary: Failed!
  total: 1
  failed: 1
  succeeded: 0
  skipped: 0
Exit code: 2
```

This excerpt preserves the failure identity and expected/actual values after temporary artifacts expire. It omits the stack trace and normal host lifecycle messages.

## Review corrections and controls

Independent review reproduced host-startup failure when the initial fixture inherited `DAPR_HTTP_ENDPOINT=invalid-readiness-endpoint`. The new fixture now supplies its own loopback endpoint, like its neighbors. The corrected test passes with both ambient endpoint and port deliberately invalid:

```bash
dotnet build tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug -m:1 -nr:false -p:UseHexalithProjectReferences=true > /tmp/dw-640/build-review.log 2>&1
env DAPR_HTTP_ENDPOINT=invalid-readiness-endpoint DAPR_HTTP_PORT=invalid-readiness-port dotnet test --project tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-build --no-restore --filter-method '*ReadyRegistersThreeSecondGatewayTimeout' --minimum-expected-tests 1 --no-ansi --no-progress --results-directory /tmp/dw-640/ambient-control --report-xunit-trx --report-xunit-trx-filename ambient-control.trx > /tmp/dw-640/ambient-control.log 2>&1
```

The build has zero warnings/errors; the test passes **1/1**, exit **0**. This is the same registration case already counted in the six-case class.

Review also suggested explicitly pinning timeout failure to `Unhealthy`. The shared service defaults map `Degraded` to HTTP 200 and `Unhealthy` to HTTP 503. The test now asserts `registration.FailureStatus == HealthStatus.Unhealthy` for a direct configuration diagnostic. The review's claim that existing HTTP cases all remain green under `Degraded` was refuted: the mutation below also fails both existing 503-response cases, because their HTTP resilience retries reach the health-check deadline. No previously uncovered failure-status defect is claimed.

The scope checker now includes `git ls-files --others --exclude-standard`. A controlled nonignored `dw-640-untracked-scope-probe.md` created with exclusive creation was rejected with `Unrelated tracked or untracked paths changed`; it was removed in `finally`, and the ordinary check passed again. The diagnostic excerpt above addresses review's request for a durable failure identity and values. The final specification records every review finding and its disposition.

The second compile overlay changes only the registration's failure status:

```bash
python3 - <<'PY'
from pathlib import Path
source = Path('src/Hexalith.Tenants.Api/Program.cs').read_text()
old = 'failureStatus: HealthStatus.Unhealthy,'
if source.count(old) != 1:
    raise RuntimeError('Expected exactly one gateway failure status')
Path('/tmp/dw-640/Program.DegradedFailure.cs').write_text(source.replace(old, 'failureStatus: HealthStatus.Degraded,'))
text = Path('/tmp/dw-640/missing-timeout.targets').read_text()
text = text.replace('DW640UseMissingTimeoutProgram', 'DW640UseDegradedFailureProgram').replace('Program.MissingTimeout.cs', 'Program.DegradedFailure.cs')
Path('/tmp/dw-640/degraded-failure.targets').write_text(text)
PY
dotnet build tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug -m:1 -nr:false -p:UseHexalithProjectReferences=true -p:CustomAfterMicrosoftCommonTargets=/tmp/dw-640/degraded-failure.targets > /tmp/dw-640/build-degraded-failure.log 2>&1
dotnet test --project tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-build --no-restore --filter-class '*TenantsApiReadinessTests' --minimum-expected-tests 6 --no-ansi --no-progress --results-directory /tmp/dw-640/degraded-failure --report-xunit-trx --report-xunit-trx-filename degraded-failure.trx > /tmp/dw-640/degraded-failure.log 2>&1
```

The mutation build exits **0**, with zero warnings/errors. Its class run exits **2**, with **3 passed / 3 failed / 0 skipped**. The new test fails because `registration.FailureStatus` should be `Unhealthy` but is `Degraded`; the existing direct-endpoint and Dapr-endpoint `ServiceUnavailable` cases each fail because `/ready` returns `OK` instead. These are mutation results, not failures of the final production source.

Restore normal compilation and rerun the complete readiness class:

```bash
dotnet build tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug -m:1 -nr:false -p:UseHexalithProjectReferences=true > /tmp/dw-640/build-post-review-final.log 2>&1
dotnet test --project tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-build --no-restore --filter-class '*TenantsApiReadinessTests' --minimum-expected-tests 6 --no-ansi --no-progress --results-directory /tmp/dw-640/post-review-final --report-xunit-trx --report-xunit-trx-filename readiness.trx > /tmp/dw-640/post-review-final.log 2>&1
```

**Post-review final result:** ordinary audit-enabled source build exits **0**, with **0 warnings / 0 errors**; the maintained readiness class exits **0**, with **6 passed / 0 failed / 0 skipped**. All seven source builds in this run succeed without environment pins beyond source references.

| Review run | Total | Passed | Failed | Start UTC | Finish UTC |
| --- | ---: | ---: | ---: | --- | --- |
| Invalid ambient endpoint and port | 1 | 1 | 0 | 2026-10-18T17:17:05.4637666+00:00 | 2026-10-18T17:17:06.0556186+00:00 |
| Degraded failure-status mutation | 6 | 3 | 3 | 2026-10-18T17:18:28.1767030+00:00 | 2026-10-18T17:18:34.9917862+00:00 |
| Final normal-source readiness | 6 | 6 | 0 | 2026-10-18T17:19:52.8834292+00:00 | 2026-10-18T17:20:00.3298818+00:00 |

| Review artifact | SHA256 |
| --- | --- |
| `/tmp/dw-640/build-review.log` | `eb6e754ec3b2433df6c9000051d67b14d1924bddf198acfa447c5d157cbd2b6e` |
| `/tmp/dw-640/build-degraded-failure.log` | `f9da5b1e9bdebe01e48db578668b17ef86f4d05e96b040ed089784e6828bfd32` |
| `/tmp/dw-640/build-post-review-final.log` | `bdba7eb43a23166fcbc04a151913a3e7b9ca7dccacdafc1331679e0add4df1f7` |
| `/tmp/dw-640/ambient-control/ambient-control.trx` | `3380d3d7e5c94987966cf9430695c1190bfe17fd0c4464c28eaa79c97d8fc369` |
| `/tmp/dw-640/degraded-failure/degraded-failure.trx` | `d4b5e559f6b9351cbe99bed72f33590b12baa8ec606f9f871d0a652f67535e46` |
| `/tmp/dw-640/post-review-final/readiness.trx` | `3869a4efdb1616fb8b78ff57d68fd127a271fa51b00fe76d910f47684b48981e` |
| `/tmp/dw-640/untracked-checker-control.log` | `63b3ebc30b5d873de7d76f8a8fc277aafe38ef03937fdcdf27f5eddaf9069262` |
| `/tmp/dw-640/commitlint.log` | `1fd50ca0e2f688c062ab1929a094a39e4e1075d90e9638e6119404c713c2d2e5` |

## Review and local commit validation

The configured context-free Blind Hunter layer reviewed all five changed/new files. No configured review layer was skipped. Three findings resulted in fixture/evidence corrections. The existing-coverage claim and the unfinished-metadata claim were refuted or explained with evidence in the specification's five-entry triage log. The failure-status assertion remains as a direct diagnostic improvement. No finding is deferred and no new deferred ID is added.

The pinned `@commitlint/cli@21.2.3` accepts the exact full candidate `test(api): pin gateway readiness timeout`, exit **0**, **0 problems / 0 warnings**:

```bash
node_modules/.bin/commitlint --edit /tmp/dw-640/commit-message.txt --verbose > /tmp/dw-640/commitlint.log 2>&1
```

The invoked workflow calls for a local commit. No push is requested.

The independent correction recheck verified all 15 retained artifact hashes, the preservation checker and a fresh six-case hostile-environment run (6/6, exit 0). It retracted the initial Degraded-coverage claim and found no additional code/evidence issue. Its one remaining documentation comment is resolved by explicitly labeling the initial pending-review note as historical, preserving the append-only implementation notes. No issue remains deferred.
