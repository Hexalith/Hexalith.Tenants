# DW-639 validation evidence — 2026-10-18

Entry revision: `d93ba9ab613d9c31638390ed99c898c72ef3bfc2`, clean `main`, one existing local commit ahead of origin. This run reconciles one stale deferred record. Application code and tests already satisfy the recorded request.

## Existing implementation

Commit `b185ae80de474bedc6c60af38bacc5550d4e8cd7` already contains the diagnostic correction and corresponding tests. The current `src/Hexalith.Tenants.Api/Program.cs` names both `EventStore:Authentication:SigningKey` and `Jwt:SigningKey` in its missing and short signing-key exceptions. Missing-key guidance also names both accepted authority alternatives. These are static messages containing configuration names, with no supplied values.

In `tests/Hexalith.Tenants.IntegrationTests/TenantsApiGeneratedControllerTests.cs`, `Symmetric_key_mode_rejects_missing_blank_or_short_signing_key` resolves production authentication options through the actual API host and asserts the complete message for four inputs: null, empty, whitespace and a short legacy key. `Short_alias_signing_key_names_both_key_aliases` asserts the complete short-alias message. The factory clears configuration sources before applying fixture settings, isolating these inputs from ambient configuration. No duplicate tests or production changes were needed.

## Commands and results

Executed from the repository root, SDK `10.0.402`. The ordinary audit-enabled Debug source build exits **0**, with **0 warnings / 0 errors**. The maintained project runner exits **0**, with **44 passed / 0 failed / 0 skipped**. All five diagnostic cases are included in those 44 unique cases; the remaining cases cover adjacent generated-controller and authentication behavior. These are the original command lines; use the fresh-directory recipe below for reproduction.

```bash
mkdir -p /tmp/dw-639
dotnet build tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug -m:1 -nr:false -p:UseHexalithProjectReferences=true > /tmp/dw-639/build.log 2>&1
dotnet test --project tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-build --no-restore --filter-class '*TenantsApiGeneratedControllerTests' --minimum-expected-tests 44 --no-ansi --no-progress --results-directory /tmp/dw-639/generated-controller --report-xunit-trx --report-xunit-trx-filename generated-controller.trx > /tmp/dw-639/generated-controller.log 2>&1
```

For a fresh reproduction, run the following Bash commands and the checker below in the same shell. `set -euo pipefail` prevents continuing after a failed build or test invocation, and the unique directory cannot contain a prior passing TRX. A missing fresh TRX fails the checker.

```bash
set -euo pipefail
DW639_RUN_DIRECTORY=$(mktemp -d /tmp/dw-639.XXXXXXXX)
export DW639_RUN_DIRECTORY
dotnet build tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug -m:1 -nr:false -p:UseHexalithProjectReferences=true > "$DW639_RUN_DIRECTORY/build.log" 2>&1
dotnet test --project tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-build --no-restore --filter-class '*TenantsApiGeneratedControllerTests' --minimum-expected-tests 44 --no-ansi --no-progress --results-directory "$DW639_RUN_DIRECTORY/generated-controller" --report-xunit-trx --report-xunit-trx-filename generated-controller.trx > "$DW639_RUN_DIRECTORY/generated-controller.log" 2>&1
```

No fallback build flag or direct-assembly test fallback was necessary. Historical DW-641 audit-blocker evidence remains historical; this run does not change dependencies or audit policy. These results qualify the selected in-process host class, not the full IntegrationTests project, solution, external identity provider, live Aspire topology or release readiness. No live topology was started for this documentation reconciliation.

The TRX run spans `2026-10-18T17:27:27.3507526+00:00` to `2026-10-18T17:27:29.1536501+00:00`. Counters: total/executed/passed **44**, failed/error/notExecuted **0**. All 44 individual outcomes are `Passed`, including four instances of the missing/blank/short theory and the short-alias fact.

| Artifact | SHA256 |
| --- | --- |
| `/tmp/dw-639/build.log` | `7908a91df417b47a0b8720f630cbcf08d88435362520caeaf4105252c877a7b8` |
| `/tmp/dw-639/generated-controller.log` | `98ac721203033e44c94390621f116602a6008938e7f67f16f21e79c8c73dd767` |
| `/tmp/dw-639/generated-controller/generated-controller.trx` | `23c19417bc0b14aaf42954c52a3ddfcbec681ad16b390cea99732d7ed41a0f44` |
| Executed Debug IntegrationTests assembly | `8e89569e223a19b807d93ccbdde4dce2e7064743f2b72ebb20da95dd512c3fdb` |

## Durable execution excerpts

The complete logs and TRX are temporary local artifacts. These copied log excerpts and parsed TRX attribute receipts preserve the minimal execution evidence here; their original full-artifact hashes above identify the source files, but cannot recover those files after deletion. Fresh reproduction creates new evidence rather than proving the historical hashes.

```text
Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:19.63

Running tests from /home/administrator/projects/hexalith/tenants/tests/Hexalith.Tenants.IntegrationTests/bin/Debug/net10.0/Hexalith.Tenants.IntegrationTests.dll (net10.0|x64)
/home/administrator/projects/hexalith/tenants/tests/Hexalith.Tenants.IntegrationTests/bin/Debug/net10.0/Hexalith.Tenants.IntegrationTests.dll (net10.0|x64) passed (2s 282ms)

  In process file artifacts produced:
    - /tmp/dw-639/generated-controller/generated-controller.trx

Test run summary: Passed!
  total: 44
  failed: 0
  succeeded: 44
  skipped: 0
  duration: 2s 470ms
```

```json
{
  "times": {
    "creation": "2026-10-18T17:27:27.3507526+00:00",
    "queuing": "2026-10-18T17:27:27.3507526+00:00",
    "start": "2026-10-18T17:27:27.3507526+00:00",
    "finish": "2026-10-18T17:27:29.1536501+00:00"
  },
  "counters": {
    "total": "44",
    "executed": "44",
    "passed": "44",
    "failed": "0",
    "error": "0",
    "timeout": "0",
    "aborted": "0",
    "inconclusive": "0",
    "passedButRunAborted": "0",
    "notRunnable": "0",
    "notExecuted": "0",
    "disconnected": "0",
    "warning": "0",
    "completed": "0",
    "inProgress": "0",
    "pending": "0"
  },
  "diagnostics": [
    {
      "testId": "b539b49a-3e24-4fa4-939a-c413f3b8a00d",
      "testName": "Hexalith.Tenants.IntegrationTests.TenantsApiGeneratedControllerTests.Short_alias_signing_key_names_both_key_aliases",
      "outcome": "Passed"
    },
    {
      "testId": "9b126181-b39e-43d5-8488-29ecfbbef1a1",
      "testName": "Hexalith.Tenants.IntegrationTests.TenantsApiGeneratedControllerTests.Symmetric_key_mode_rejects_missing_blank_or_short_signing_key(signingKey: \"\", expectedMessage: \"EventStore:Authentication:SigningKey or Jwt:Signin\"\u00b7\u00b7\u00b7)",
      "outcome": "Passed"
    },
    {
      "testId": "81711c18-36bf-450c-ae28-d13557fd8e9a",
      "testName": "Hexalith.Tenants.IntegrationTests.TenantsApiGeneratedControllerTests.Symmetric_key_mode_rejects_missing_blank_or_short_signing_key(signingKey: \"   \", expectedMessage: \"EventStore:Authentication:SigningKey or Jwt:Signin\"\u00b7\u00b7\u00b7)",
      "outcome": "Passed"
    },
    {
      "testId": "2a420bc4-ee44-448b-9218-3407b99d453c",
      "testName": "Hexalith.Tenants.IntegrationTests.TenantsApiGeneratedControllerTests.Symmetric_key_mode_rejects_missing_blank_or_short_signing_key(signingKey: null, expectedMessage: \"EventStore:Authentication:SigningKey or Jwt:Signin\"\u00b7\u00b7\u00b7)",
      "outcome": "Passed"
    },
    {
      "testId": "9386ac0b-e76f-4552-96fe-d56af98e0dff",
      "testName": "Hexalith.Tenants.IntegrationTests.TenantsApiGeneratedControllerTests.Symmetric_key_mode_rejects_missing_blank_or_short_signing_key(signingKey: \"short-key\", expectedMessage: \"The configured EventStore:Authentication:SigningKe\"\u00b7\u00b7\u00b7)",
      "outcome": "Passed"
    }
  ]
}
```

## Reconciliation and preservation

Only DW-639 changes status. Its original finding, source specification, legacy detail, priority, owner, dependencies and relationship remain. Superseded fields survive as `pre-dw-639-*`. Only its maintenance row is removed; current totals become **655 IDs, 377 done / 278 open**, including **191 actionable** records, across **250 index rows** covering every open ID exactly once. Historical closure paragraphs remain date-bound, including their earlier DW-639 references. Archive, sprint tracking, dependency pointers and existing release qualification holds remain preserved. Release remains not ready.

The following receipts were captured after the build and before test execution. Original-run checks compare working-file hashes with these receipts; portable reproduction checks compare normalized Git contents with the canonical entry revision. This avoids rejecting an identical LF checkout because the original C# working files used CRLF. `--original-receipts` additionally verifies all original artifact and assembly hashes above; a new execution normally has different timestamps and hashes and must omit that option. Root dependencies are inspected nonrecursively; their revisions equal the entry gitlinks and they have no tracked changes.

<!-- dw639-provenance -->
```json
{
  "baseline": "d93ba9ab613d9c31638390ed99c898c72ef3bfc2",
  "files": {
    "src/Hexalith.Tenants.Api/Program.cs": "5df20c29ec77a50d3b6726168621006e521dfe83245c53a995f87314d7efebf1",
    "tests/Hexalith.Tenants.IntegrationTests/TenantsApiGeneratedControllerTests.cs": "2a6edcdd7b6d7f7ac92821fcbcd38569a8c01d1d582fa27e2d899d2b47e0a821",
    "_bmad-output/implementation-artifacts/deferred-work-archive.md": "38d114794138a086eba42f10cf0c5eddeec650ed38744d0eaaace0d06d864878",
    "_bmad-output/implementation-artifacts/sprint-status.yaml": "8c7dd8eab8027c0455c27decbfc6c08be8d472e8a2a276a74be8f1b5a9a93fc7"
  },
  "dependencies": {
    "references/Hexalith.EventStore": {
      "revision": "d55c6f7c11e9bce6f17d490a84c83f1561979d63",
      "status": ""
    },
    "references/Hexalith.Commons": {
      "revision": "5c23c818f639c5bb3175f964c5549440bd5b876d",
      "status": ""
    },
    "references/Hexalith.AI.Tools": {
      "revision": "3f194e17174994d308ec84af9ee2b5aa68674d0d",
      "status": ""
    },
    "references/Hexalith.FrontComposer": {
      "revision": "50f4acbc56192b2ad42b912aa6a83edf3bf2c264",
      "status": ""
    },
    "references/Hexalith.Builds": {
      "revision": "1dfc39e3a3e6ab1c8ce95138490b39d76e7feb2c",
      "status": ""
    },
    "references/Hexalith.PolymorphicSerializations": {
      "revision": "221ac4592ba908ae7661ba1fa9e2d1fcc23baee0",
      "status": ""
    },
    "references/Hexalith.Memories": {
      "revision": "4f6b3b1265d15d208761e6419e5c825d3f39fea3",
      "status": ""
    },
    "references/Hexalith.McpCli": {
      "revision": "ef2d9e508609e6f645baa88b1b3956c02e5878a9",
      "status": ""
    },
    "references/Hexalith.Platform": {
      "revision": "5151bef5b68d6bd1f33d94f0a3a1b11d32dd48a6",
      "status": ""
    }
  }
}
```

The complete preservation check below exits **0** with:

```text
PASS: 655 IDs; only DW-639 reconciled; 377 done/278 open; 191 actionable; 250 rows exactly cover open IDs; historical fields and unrelated content preserved; protected files and nine root dependencies unchanged; new links resolve; maintained class 44/44 includes five diagnostics; build zero warnings/errors.
```

Additional checks exit **0**:

```bash
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-639-signing-key-diagnostic-closure.md
git diff --check
```

The story guard reports `No references/ pointer changes in range` and `RESULT: PASS`. An initial metadata-count inspection exited **1** with `TypeError: 'NoneType' object is not subscriptable` because it assumed archived done records contain a disposition. That helper made no edits. The corrected count and the checker below handle archived records by status.

## Independent review and commit validation

Independent review found five documentation gaps: stale-result reuse during reproduction, unverified historical artifact hashes, insufficient unique-case/matrix checks, CRLF byte receipts applied to portable reproduction, and temporary-only raw execution artifacts. All five are corrected in this document and recorded in the specification's triage log. No application defect or new deferred work was identified.

Both checker modes pass against the original retained artifacts: ordinary mode checks normalized content and the complete execution matrix; `--original-receipts` also verifies every recorded artifact/assembly/working-file hash. Their mode labels make the scope explicit. Fresh reproduction requires a new directory and successful build/test commands; no new build/test run is implied by checking both modes on the original artifacts.

Independent correction recheck confirms all five findings resolved, with no concrete remaining issue or deferred finding. The exact embedded checker was executed successfully, and `git diff --check` and the story-gitlink guard pass. Isolated temporary copies also confirm these rejection controls; the original artifacts remain unchanged:

| Evidence control | Result |
| --- | --- |
| Missing TRX in supplied directory | Nonzero exit: `FileNotFoundError` |
| Replace one diagnostic result with a duplicate of another | Nonzero exit: `Duplicate test result identity: testId` |
| Substitute the null diagnostic input with an unexpected value | Nonzero exit: `Diagnostic input matrix differs` |
| Append text to the original test log | Nonzero exit in original-receipts mode: `Original artifact receipt mismatch: generated-controller.log` |

The owning repository's installed commitlint CLI matches pinned lockfile version **21.2.3**. The exact candidate is written to `/tmp/dw-639/commit-message.txt` with a final newline and validated with:

```bash
node node_modules/@commitlint/cli/cli.js --verbose < /tmp/dw-639/commit-message.txt > /tmp/dw-639/commitlint.log 2>&1
```

Exit **0**, durable output:

```text
⧗   --- input ---
docs(auth): close verified signing-key diagnostic gap
✔   found 0 problems, 0 warnings
```

The candidate's SHA256 is `0bc513262baf80a5aec49771811e64ba9072d61a72dc2ebca51bda59e8e7f5d1`. The invoked workflow calls for a local commit; no remote push is requested.

## Reproducible preservation check

After the fresh-directory recipe above, run this block in the same Bash shell from the repository root. It verifies the complete unrelated ledger/index contents, preserved fields, exact open-ID coverage, normalized protected files, all nine root dependencies, new links, 44 unique passing test identities and each of the five distinct diagnostic inputs. Missing files or failed conditions stop the check. It uses explicit failures and remains effective under Python optimization.

For the original retained artifacts only, set `DW639_RUN_DIRECTORY=/tmp/dw-639` and add `--original-receipts` to the invocation before the heredoc. This additionally verifies the recorded log/TRX/assembly hashes and original working-file byte hashes. Omit that flag for fresh reproduction: timestamps, assembly bytes and checkout line endings can legitimately differ.

```bash
python3 - --run-directory "${DW639_RUN_DIRECTORY:?Run the fresh-directory recipe first}" <<'PY'
from collections import Counter
import argparse
from pathlib import Path
import hashlib
import json
import re
import subprocess
import xml.etree.ElementTree as ET

BASE = 'd93ba9ab613d9c31638390ed99c898c72ef3bfc2'
LEDGER = '_bmad-output/implementation-artifacts/deferred-work.md'
INDEX = 'docs/maintenance-backlog.md'
SPEC = '_bmad-output/implementation-artifacts/spec-dw-639-signing-key-diagnostic-closure.md'
EVIDENCE = '_bmad-output/implementation-artifacts/dw-639-validation-evidence.md'
parser = argparse.ArgumentParser()
parser.add_argument('--run-directory', type=Path, required=True)
parser.add_argument('--original-receipts', action='store_true')
args = parser.parse_args()
run = args.run_directory

def require(condition, message):
    if not condition:
        raise RuntimeError(message)

def git(*args):
    return subprocess.check_output(['git', *args], text=True)

def original(path):
    return git('show', BASE + ':' + path)

def records(text):
    return dict(re.findall(r'^### (DW-\d+):([^\n]*\n.*?)(?=^### DW-|\Z)', text, re.M | re.S))

before, after = records(original(LEDGER)), records(Path(LEDGER).read_text())
require(set(before) == set(after) and len(after) == 655, 'Ledger IDs changed')
require([key for key in before if before[key] != after[key]] == ['DW-639'], 'Unrelated ledger record changed')
for field in ('status', 'disposition', 'verification-date', 'verification', 'evidence-kind', 'next-action'):
    value = re.search(r'^' + field + r': (.*)$', before['DW-639'], re.M)[1]
    require('pre-dw-639-' + field + ': ' + value in after['DW-639'].splitlines(), 'Historical field lost: ' + field)
for line in before['DW-639'].splitlines():
    if line and not re.match(r'^(status|disposition|verification-date|verification|evidence-kind|next-action):', line):
        require(line in after['DW-639'].splitlines(), 'Original finding metadata lost')
statuses = Counter(re.search(r'^status: (.*)$', body, re.M)[1].split()[0] for body in after.values())
require(statuses == {'done': 377, 'open': 278}, 'Wrong status counts')
require(sum('\ndisposition: actionable\n' in body for body in after.values()) == 191, 'Wrong actionable count')
current = Path(LEDGER).read_text()
no_summary, count = re.subn(r'(?<=# Deferred Work\n\n)\*\*DW-639 closure .*?\n\n', '', current, count=1)
require(count == 1, 'Missing ledger closure summary')
pattern = r'^### DW-639:.*?(?=^### DW-|\Z)'
old_record = re.search(pattern, original(LEDGER), re.M | re.S)[0]
restored = re.sub(pattern, lambda match: old_record, no_summary, count=1, flags=re.M | re.S)
require(restored == original(LEDGER), 'Ledger content beyond DW-639/summary changed')
expected = original(INDEX)
row = next(line for line in expected.splitlines(keepends=True) if line.startswith('| DW-639 |'))
expected = expected.replace(row, '', 1).replace('| done | 376 |', '| done | 377 |', 1).replace('| actionable | 192 |', '| actionable | 191 |', 1).replace('**Remaining: 279 open records across 251 index rows.**', '**Remaining: 278 open records across 250 index rows.**', 1)
index = Path(INDEX).read_text()
no_summary, count = re.subn(r'(?<=# Maintenance backlog\n\n)\*\*DW-639 closure .*?\n\n', '', index, count=1)
require(count == 1 and no_summary == expected, 'Unrelated maintenance content changed')
rows = [line for line in index.splitlines() if line.startswith('| DW-')]
coverage = Counter(key for row in rows for key in re.findall(r'DW-\d+', row.split('|')[1]))
open_ids = {key for key, body in after.items() if '\nstatus: open\n' in body}
require(len(rows) == 250 and set(coverage) == open_ids and set(coverage.values()) == {1}, 'Index must cover every open record exactly once')
provenance = json.loads(re.search(r'<!-- dw639-provenance -->\n```json\n(.*?)\n```', Path(EVIDENCE).read_text(), re.S)[1])
for path, digest in provenance['files'].items():
    if args.original_receipts:
        require(hashlib.sha256(Path(path).read_bytes()).hexdigest() == digest, 'Original working-file bytes changed: ' + path)
    require(Path(path).read_text() == original(path), 'Protected Git content changed: ' + path)
for path, receipt in provenance['dependencies'].items():
    require(not receipt['status'], 'Dependency was dirty at receipt capture: ' + path)
    require(git('-C', path, 'rev-parse', 'HEAD').strip() == receipt['revision'], 'Dependency HEAD changed: ' + path)
    require(git('ls-tree', BASE, path).split()[2] == receipt['revision'], 'Dependency differs from entry gitlink: ' + path)
    require(not git('-C', path, 'status', '--porcelain','--untracked-files=no'), 'Dependency became dirty: ' + path)
for path in (LEDGER, INDEX, SPEC, EVIDENCE):
    for target in re.findall(r'\]\(([^)]+)\)', Path(path).read_text()):
        if 'dw-639' in target:
            require((Path(path).parent / target.split('#')[0]).resolve().is_file(), 'New link target missing: ' + target)
ns = {'t': 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
root = ET.parse(run / 'generated-controller/generated-controller.trx').getroot()
counters = root.find('t:ResultSummary/t:Counters', ns).attrib
require(counters['total'] == counters['passed'] == '44' and all(counters[key] == '0' for key in ('failed', 'error', 'notExecuted')), 'Unexpected test totals')
results = root.findall('t:Results/t:UnitTestResult', ns)
require(len(results) == 44 and all(item.attrib['outcome'] == 'Passed' for item in results), 'Tests not all passing')
for attribute in ('testId', 'testName', 'executionId'):
    require(len({item.attrib[attribute] for item in results}) == 44, 'Duplicate test result identity: ' + attribute)
definitions = root.findall('t:TestDefinitions/t:UnitTest', ns)
require(len(definitions) == 44 and {item.attrib['id'] for item in definitions} == {item.attrib['testId'] for item in results}, 'Result/definition identity mismatch')
diags = [item for item in results if any(name in item.attrib['testName'] for name in ('Symmetric_key_mode_rejects_missing_blank_or_short_signing_key', 'Short_alias_signing_key_names_both_key_aliases'))]
require(len(diags) == 5, 'Missing diagnostic case execution')
legacy = [item for item in diags if 'Symmetric_key_mode_rejects_missing_blank_or_short_signing_key(' in item.attrib['testName']]
values = [re.search(r'\(signingKey: (.*?), expectedMessage:', item.attrib['testName'])[1] for item in legacy]
require(Counter(values) == Counter(['null', '""', '"   "', '"short-key"']), 'Diagnostic input matrix differs')
require(sum(item.attrib['testName'].endswith('.Short_alias_signing_key_names_both_key_aliases') for item in diags) == 1, 'Missing unique alias case')
require('Build succeeded.' in (run / 'build.log').read_text() and '0 Warning(s)' in (run / 'build.log').read_text() and '0 Error(s)' in (run / 'build.log').read_text(), 'Build not clean')
if args.original_receipts:
    artifact_receipts = {
        'build.log': '7908a91df417b47a0b8720f630cbcf08d88435362520caeaf4105252c877a7b8',
        'generated-controller.log': '98ac721203033e44c94390621f116602a6008938e7f67f16f21e79c8c73dd767',
        'generated-controller/generated-controller.trx': '23c19417bc0b14aaf42954c52a3ddfcbec681ad16b390cea99732d7ed41a0f44',
    }
    for relative, digest in artifact_receipts.items():
        require(hashlib.sha256((run / relative).read_bytes()).hexdigest() == digest, 'Original artifact receipt mismatch: ' + relative)
    require(hashlib.sha256(Path('tests/Hexalith.Tenants.IntegrationTests/bin/Debug/net10.0/Hexalith.Tenants.IntegrationTests.dll').read_bytes()).hexdigest() == '8e89569e223a19b807d93ccbdde4dce2e7064743f2b72ebb20da95dd512c3fdb', 'Original executed assembly receipt mismatch')
print('Receipt mode:', 'original run; all recorded artifact and working-file hashes verified' if args.original_receipts else 'fresh reproduction; normalized Git content checked, historical byte hashes not applied')
print('PASS: 655 IDs; only DW-639 reconciled; 377 done/278 open; 191 actionable; 250 rows exactly cover open IDs; historical fields and unrelated content preserved; protected files and nine root dependencies unchanged; new links resolve; maintained class 44/44 includes five diagnostics; build zero warnings/errors.')
PY
```
