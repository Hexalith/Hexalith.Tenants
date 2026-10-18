# DW-641 validation evidence — 2026-10-18

Entry revision: `3e20b0372203fa31f93d60dad9a07c65b4d905df`, clean `main`. This run reconciles one stale deferred record; it introduces no application or test changes.

## Existing implementation and scope

Commit `b185ae80de474bedc6c60af38bacc5550d4e8cd7` already added both requested precedence tests. Its author date is 2026-10-12 and commit date is 2026-10-13. In `tests/Hexalith.Tenants.IntegrationTests/TenantsApiGeneratedControllerTests.cs`:

- `Legacy_signing_key_wins_over_alias` supplies distinct nonblank signing keys under `EventStore:Authentication:SigningKey` and `Jwt:SigningKey`, resolves the real API host's `JwtBearerOptions`, and asserts the selected key bytes equal the legacy key.
- `Legacy_authority_wins_over_alias` supplies distinct nonblank values under `EventStore:Authentication:Authority` and `Oidc:Authority`, resolves the same options, and asserts the selected authority equals the legacy value.
- `TenantsApiWebApplicationFactory.ConfigureWebHost` clears configuration sources before adding the fixture's in-memory settings. Ambient environment configuration cannot replace these conflicting inputs.

`src/Hexalith.Tenants.Api/Program.cs::FirstConfigured` selects the nonblank legacy value first. These tests exercise production option configuration through `WebApplicationFactory`, rather than reimplementing the selector in the test. Adjacent alias-only and blank-legacy fallback tests remain unchanged. This closes the exact DW-641 test gap; it does not claim that conflicting settings were exercised over a live OIDC discovery server or a deployed Aspire topology. DW-639 and all other deferred records remain unchanged.

## Build gate and fallback

The normal Debug source build fails with **NU1902** in `references/Hexalith.Commons/src/libraries/Hexalith.Commons.Aspire/Hexalith.Commons.Aspire.csproj`: MessagePack `2.5.302` has a moderate advisory (`GHSA-qhrr-8q5h-9q3h`) treated as an error. The final summary is **0 warnings / 1 error**. This failure occurs during restore, before tests run.

```bash
mkdir -p /tmp/dw-641
dotnet build tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug -m:1 -nr:false -p:UseHexalithProjectReferences=true > /tmp/dw-641/build.log 2>&1
```

The Hexalith baseline's documented environment fallback disables NuGet audit for this local verification invocation only. No dependency, project, audit policy or CI gate is changed. The ordinary audit-enabled build remains blocked; fallback success is not evidence that the ordinary gate passed.

```bash
dotnet build tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug -m:1 -nr:false -p:UseHexalithProjectReferences=true -p:NuGetAudit=false > /tmp/dw-641/build-audit-disabled.log 2>&1
```

The fallback build exits **0**, with **0 warnings / 0 errors**. SDK: `10.0.402`; executed runtime: `.NET 10.0.13`; runner: xUnit v3 `4.0.2`. All following runs use that freshly built Debug assembly.

## Focused execution

```bash
dotnet tests/Hexalith.Tenants.IntegrationTests/bin/Debug/net10.0/Hexalith.Tenants.IntegrationTests.dll -method Hexalith.Tenants.IntegrationTests.TenantsApiGeneratedControllerTests.Legacy_signing_key_wins_over_alias -method Hexalith.Tenants.IntegrationTests.TenantsApiGeneratedControllerTests.Legacy_authority_wins_over_alias -noColor -result-xml /tmp/dw-641/precedence.xml > /tmp/dw-641/precedence-exact.log 2>&1
dotnet tests/Hexalith.Tenants.IntegrationTests/bin/Debug/net10.0/Hexalith.Tenants.IntegrationTests.dll -class Hexalith.Tenants.IntegrationTests.TenantsApiGeneratedControllerTests -noColor -result-xml /tmp/dw-641/generated-controller.xml > /tmp/dw-641/generated-controller.log 2>&1
```

| Selection | Passed | Failed | Skipped / errors | Exit |
| --- | ---: | ---: | ---: | ---: |
| Two exact precedence methods | 2 | 0 | 0 / 0 | 0 |
| Complete generated-controller class | 44 | 0 | 0 / 0 | 0 |

The class total includes the two focused methods; these are **44 unique passing cases**, not 46. It also covers alias-only authenticated requests, blank-legacy fallback, issuer validation and invalid-key diagnostics. No other IntegrationTests classes, external services or full solution gate are claimed.

The first attempted selector, `-method 'Hexalith.Tenants.IntegrationTests.TenantsApiGeneratedControllerTests.Legacy_*_wins_over_alias'`, exits **3** before running tests: this xUnit runner permits wildcards only at filter boundaries. The two exact `-method` arguments above correct the selector. `/tmp/dw-641/precedence.log` retains that rejected attempt separately from the successful log/XML.

## Reconciliation

Only DW-641 changes status. Its original finding, source specification, legacy detail and metadata remain; superseded status, disposition, verification date/text, evidence kind and next action survive under `pre-dw-641-*`. Only its maintenance row is removed. All **655** IDs remain: **375 done / 280 open**, including **193 actionable** records, with **252** maintenance rows covering every open ID exactly once. DW-639, the archive, sprint tracking, production/tests, dependencies and prior release qualification holds remain unchanged. Release remains not ready.

The closure checks compare the complete unrelated record bodies and maintenance content against the entry revision, verify the preserved historical fields, calculate counts and exact row coverage, resolve the new local links, and check whitespace and protected paths.

## Durable execution receipts

These compact XML assembly summaries retain the executed counts and UTC run times. Temporary logs and XML can be regenerated with the commands above; their hashes below identify this run's artifacts. The two exact test identities are listed in Existing implementation and scope.

```json
{
  "precedence": {
    "total": "2",
    "passed": "2",
    "failed": "0",
    "skipped": "0",
    "errors": "0",
    "not-run": "0",
    "start-rtf": "2026-10-18T15:08:20.3852652+00:00",
    "finish-rtf": "2026-10-18T15:08:21.3584833+00:00"
  },
  "generated-controller": {
    "total": "44",
    "passed": "44",
    "failed": "0",
    "skipped": "0",
    "errors": "0",
    "not-run": "0",
    "start-rtf": "2026-10-18T15:08:36.0896128+00:00",
    "finish-rtf": "2026-10-18T15:08:53.3339277+00:00"
  }
}
```

| Artifact | SHA256 |
| --- | --- |
| `/tmp/dw-641/build.log` | `e6969a1578a9d7f38648c6d70b05ee6adbe893055384e685063306d8f67f2404` |
| `/tmp/dw-641/build-audit-disabled.log` | `c5ce2239e64500f055583d7e4f85bda0ecc360d1bcf688fde896474475ba688b` |
| `/tmp/dw-641/precedence-exact.log` | `843eb06fc5cd9e2068f0a495a56fed07ad54590906122f37990697dfc65d31e2` |
| `/tmp/dw-641/precedence.xml` | `24976273a67136c2ab697274842ebc76067699a8a3bdb1498055912eaaa89c27` |
| `/tmp/dw-641/generated-controller.log` | `2ec20949aabe723966cdd246d00e97341825a21637cfe63bcb565666345e786d` |
| `/tmp/dw-641/generated-controller.xml` | `f95890f01bcab2ffa37ed2213134e9ba85e6943d6a261ee81d1487954959db09` |
| Executed Debug IntegrationTests assembly | `5d81d0db96e2db5ad8f9f9a947300332bad9e28ce60c4afcc3a8a7a683a839e6` |

The application/test working-file SHA256 values captured before the fallback build equal the final values:

| Working file | SHA256 |
| --- | --- |
| `src/Hexalith.Tenants.Api/Program.cs` | `5df20c29ec77a50d3b6726168621006e521dfe83245c53a995f87314d7efebf1` |
| `tests/Hexalith.Tenants.IntegrationTests/TenantsApiGeneratedControllerTests.cs` | `2a6edcdd7b6d7f7ac92821fcbcd38569a8c01d1d582fa27e2d899d2b47e0a821` |

Normalized Git-content comparisons also match the entry revision; normalization accounts for CRLF working files and LF Git blobs. Nonrecursive dependency inspection verifies the following root-declared submodules are clean and their checked-out HEADs equal the entry revision's gitlinks. No nested submodule operation or dependency update was performed.

| Root-declared submodule | Canonical revision |
| --- | --- |
| `Hexalith.AI.Tools` | `3f194e17174994d308ec84af9ee2b5aa68674d0d` |
| `Hexalith.Builds` | `4629ff2d3bd66376b29a553663c241d3a7b87853` |
| `Hexalith.Commons` | `9e874b1a3c6e7b6ff07b6f40b81d36e4e1403798` |
| `Hexalith.EventStore` | `39b366bca3dbe508b4cf3c5afa69db1ddadb6dd2` |
| `Hexalith.FrontComposer` | `1f8c7b2b4424b6232121a7cc567f7129e46577d3` |
| `Hexalith.McpCli` | `ce9ffdbea051ea6410c4dd8db1617e79dedb8fe3` |
| `Hexalith.Memories` | `64e09a1f3c15e78d500511ff2948d674158da446` |
| `Hexalith.Platform` | `2cc03cdee1261fa256546992e28b111a11f3faf8` |
| `Hexalith.PolymorphicSerializations` | `98de6e013840ece9f0fa7c68ab7dcdf2bba3b375` |

## Reproducible preservation check

Run from the repository root after the build/test commands. This checks normalized Git content, IDs, preserved historical fields, unrelated record bodies, the exact maintenance edit, dependency HEADs/cleanliness, test identities/totals and new links. The working-file hashes above separately retain this run's byte-level receipts.

```bash
python3 - <<'PY'
from collections import Counter
from pathlib import Path
import re
import subprocess
import xml.etree.ElementTree as ET

baseline = '3e20b0372203fa31f93d60dad9a07c65b4d905df'
ledger_path = '_bmad-output/implementation-artifacts/deferred-work.md'
index_path = 'docs/maintenance-backlog.md'

def require(condition, message):
    if not condition:
        raise RuntimeError(message)

def original(path):
    return subprocess.run(['git', 'show', f'{baseline}:{path}'], check=True, capture_output=True).stdout

def records(text):
    matches = list(re.finditer(r'^### DW-(\d+):.*?(?=^### DW-|\Z)', text, re.M | re.S))
    result = {int(m[1]): m[0] for m in matches}
    require(len(result) == len(matches), 'Duplicate ledger IDs')
    return result

old_text = original(ledger_path).decode()
new_text = Path(ledger_path).read_text()
old, new = records(old_text), records(new_text)
require(set(old) == set(new) and len(new) == 655, 'Ledger IDs changed')
require([n for n in old if old[n] != new[n]] == [641], 'Unrelated record changed')
old_fields = dict(re.findall(r'^([\w-]+): (.*)$', old[641], re.M))
new_fields = dict(re.findall(r'^([\w-]+): (.*)$', new[641], re.M))
replaced = {'status', 'disposition', 'verification-date', 'verification', 'evidence-kind', 'next-action'}
for key, value in old_fields.items():
    preserved_key = f'pre-dw-641-{key}' if key in replaced else key
    require(new_fields.get(preserved_key) == value, f'Lost original {key}')
require(new_fields['status'] == 'done 2026-10-18' and new_fields['disposition'] == 'done', 'DW-641 not closed')
old_prefix = old_text[:old_text.index('### DW-1:')]
new_prefix = new_text[:new_text.index('### DW-1:')]
closure_pattern = r'^\*\*DW-641 closure \(2026-10-18\):\*\* [^\n]+\n\n'
require(re.sub(closure_pattern, '', new_prefix, count=1, flags=re.M) == old_prefix, 'Historical ledger preamble changed')

index = Path(index_path).read_text()
rows = [line for line in index.splitlines() if line.startswith('| DW-')]
references = [int(n) for row in rows for n in re.findall(r'DW-(\d+)', row.split('|')[1])]
open_ids = {n for n, body in new.items() if re.search(r'^status: open$', body, re.M)}
require(set(references) == open_ids and len(references) == len(set(references)), 'Index coverage not exact')
require(len(open_ids) == 280 and len(rows) == 252, 'Incorrect open/index counts')
counts = Counter('done' if re.search(r'^status: done', body, re.M) else re.search(r'^disposition: (.*)$', body, re.M)[1] for body in new.values())
require(counts == {'done': 375, 'actionable': 193, 'blocked': 15, 'awaiting-decision': 26, 'accepted-limitation': 9, 'uncertain': 37}, 'Disposition counts differ')
expected_index = original(index_path).decode().replace('| done | 374 |', '| done | 375 |').replace('| actionable | 194 |', '| actionable | 193 |').replace('**Remaining: 281 open records across 253 index rows.**', '**Remaining: 280 open records across 252 index rows.**')
expected_index = re.sub(r'^\| DW-641 \|[^\n]*\n', '', expected_index, count=1, flags=re.M)
require(re.sub(closure_pattern, '', index, count=1, flags=re.M) == expected_index, 'Unrelated maintenance content changed')

protected = ['src/Hexalith.Tenants.Api/Program.cs', 'tests/Hexalith.Tenants.IntegrationTests/TenantsApiGeneratedControllerTests.cs', '_bmad-output/implementation-artifacts/deferred-work-archive.md', '_bmad-output/implementation-artifacts/sprint-status.yaml']
for path in protected:
    require(Path(path).read_text() == original(path).decode().replace('\r\n', '\n'), f'Protected baseline content changed: {path}')
for line in subprocess.run(['git', 'ls-tree', f'{baseline}:references'], check=True, capture_output=True, text=True).stdout.splitlines():
    metadata, name = line.split('\t')
    if not metadata.startswith('160000'):
        continue
    directory = f'references/{name}'
    head = subprocess.run(['git', '-C', directory, 'rev-parse', 'HEAD'], check=True, capture_output=True, text=True).stdout.strip()
    status = subprocess.run(['git', '-C', directory, 'status', '--porcelain'], check=True, capture_output=True, text=True).stdout
    require(head == metadata.split()[2] and not status, f'Changed dependency: {name}')
subprocess.run(['git', 'diff', '--exit-code', baseline, '--', 'references/'], check=True)
subprocess.run(['git', 'diff', '--check'], check=True)

expected_precedence = {f'Hexalith.Tenants.IntegrationTests.TenantsApiGeneratedControllerTests.{name}' for name in ('Legacy_signing_key_wins_over_alias', 'Legacy_authority_wins_over_alias')}
for name, total in [('precedence', 2), ('generated-controller', 44)]:
    root = ET.parse(f'/tmp/dw-641/{name}.xml').getroot()
    assemblies = root.findall('assembly')
    require(len(assemblies) == 1, f'Expected one assembly: {name}')
    assembly = assemblies[0]
    require(all(int(assembly.attrib[key]) == value for key, value in {'total': total, 'passed': total, 'failed': 0, 'skipped': 0, 'errors': 0, 'not-run': 0}.items()), f'Incorrect test totals: {name}')
    tests = assembly.findall('.//test')
    require(len(tests) == total and all(t.attrib['result'] == 'Pass' for t in tests), f'Nonpassing test case: {name}')
    require(expected_precedence <= {t.attrib['name'] for t in tests}, f'Missing precedence case: {name}')

changed = [ledger_path, index_path, '_bmad-output/implementation-artifacts/spec-dw-641-authentication-alias-precedence-closure.md', '_bmad-output/implementation-artifacts/dw-641-validation-evidence.md']
for path in changed:
    text = Path(path).read_text()
    require(text.endswith('\n') and not any(line.rstrip() != line for line in text.splitlines()), f'Whitespace: {path}')
    for target in re.findall(r'\]\(([^)]+)\)', text):
        if 'dw-641' in target and not target.startswith(('http:', 'https:')):
            require((Path(path).parent / target).exists(), f'Broken closure link: {path}: {target}')
print('PASS: DW-641-only closure; 655 IDs, 375 done/280 open, 193 actionable, 252 exact-covering rows; preserved historical fields/unrelated content/protected files; focused 2/2 and class 44/44; links and whitespace.')
PY
```

Result: **exit 0** — `PASS: DW-641-only closure; 655 IDs, 375 done/280 open, 193 actionable, 252 exact-covering rows; preserved historical fields/unrelated content/protected files; focused 2/2 and class 44/44; links and whitespace.`

## Independent review

The workflow's context-free Blind Hunter review requested reproducible reconciliation commands/results, durable test/provenance receipts and completed specification metadata. All four are addressed in this document and the specification's review triage. No authentication change or new deferred work is needed. No configured review layer was skipped.

The independent correction recheck confirms all four findings are resolved, the embedded checker matches its executed copy and passes, hashes match the retained logs/XML/source receipts, and no remaining issue is found.

## Local commit validation

The pinned `@commitlint/cli@21.2.3` validates the exact full candidate `docs(auth): close verified authentication alias precedence gap` with **exit 0, 0 problems and 0 warnings**:

```bash
node_modules/.bin/commitlint --edit /tmp/dw-641/commit-message.txt --verbose > /tmp/dw-641/commitlint.log 2>&1
```

An intermittent disk-full event initially left the candidate file empty; that attempt supplies no valid commit-message evidence. After space became available, the exact nonempty candidate was written, its contents verified and commitlint rerun successfully. A subsequent evidence write was interrupted; the incomplete tail was restored and the full embedded checker rerun. No source, dependency or prior artifact was deleted to make space. No push or remote mutation is requested.
