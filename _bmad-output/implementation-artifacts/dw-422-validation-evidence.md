# DW-422 validation evidence — 2026-10-20

Entry revision: `8224b5baf9c7d9ce51d53e90eea61d85291afa46`; clean `main`.

## Scope and existing protection

The read-only recovery anchor now uses `ShouldRenderRows`, the same predicate that renders its `#tenant-audit-grid-heading` destination, followed by its existing Stale/Degraded/ListRefreshed restriction. This includes the grid's validation and authentication-transition guards without maintaining a second row-visibility rule.

The original invalid-filter defect was already prevented by commit `34c485e81bc5b00e39cb2920195ce319efe465e1`. `TryCreateRequest` clears the old snapshot, increments the load generation and cancels active work on invalid input. Results are published only through the generation-guarded dispatcher; notification refresh ignores dirty filters. This run does not claim a reproduced dangling link on entry production. DW-422 closes through qualification of that existing behavior plus explicit predicate alignment. DW-444 focus handling remains separate.

## Executed verification

Run from the repository root:

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug -m:1 -nr:false -p:UseHexalithProjectReferences=true
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug --no-build --no-restore --filter-method '*ReadOnlyRecoveryTargetTracksInvalidFiltersAndReset' --minimum-expected-tests 12 --no-ansi --no-progress --results-directory /tmp/dw-422/entry --report-xunit-trx --report-xunit-trx-filename results.trx
```

Executed before the predicate edit and review strengthening: build exit 0, zero warnings/errors; the initial theory version passes 12/12, zero failures/skips. Temporary logs are `/tmp/dw-422-build-entry.log` and `/tmp/dw-422-test-entry.log`. This is a positive existing-fix control, not a failing original-production replay.

The twelve-case theory exercises bUnit component events for all three recovery states. Nine cases inject malformed datetime strings or a category absent from the selector; these are synthetic validation events, not proof of browser input reachability. Three use individually valid date values in reversed order. Browser qualification remains unavailable. Initially the anchor resolves to an H2 and a row exists. Invalid input produces the alert state, removes anchor/grid/heading and the refresh recovery, performs no query, and keeps Reset available. Reset performs one unfiltered request with a null cursor and no ETag and restores the anchor, destination and row. This matrix does not qualify clearing a previously populated cursor history. Existing recovery-action tests retain coverage of other states; the existing `Invalid_local_filter_discards_an_in_flight_ready_completion` case covers superseded asynchronous reads.

The same Debug source build after the predicate edit exits 0 with zero warnings/errors (`/tmp/dw-422-build-final.log`). Final test commands:

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug --no-build --no-restore --filter-class '*TenantAuditPageTests' --minimum-expected-tests 224 --no-ansi --no-progress --results-directory /tmp/dw-422/focused-corrected --report-xunit-trx --report-xunit-trx-filename results.trx
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug --no-build --no-restore --minimum-expected-tests 6278 --no-ansi --no-progress --results-directory /tmp/dw-422/full --report-xunit-trx --report-xunit-trx-filename results.trx
```

Before review strengthening, both exit 0: focused **224/224**, full UI **6278/6278**, zero failures/skips. Temporary logs are `/tmp/dw-422-test-focused-corrected.log` and `/tmp/dw-422-test-full.log`; TRX reports are in the specified result directories. The first focused invocation mistakenly set `--minimum-expected-tests 300`: all 224 cases passed, but the runner exited 9 for that excessive threshold. The corrected invocation above uses the observed full class count; no tests were removed or filtered out within the class.

`dotnet format whitespace tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --include tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs --no-restore --verify-no-changes` exits 2 with 22 pre-existing diagnostics at 20 distinct lines. A `difflib.SequenceMatcher` comparison against entry production confirms every reported line belongs to an unchanged block; none points to the new theory (starting at line 2836). Existing diagnostics occur before it (671–2704) or after it (3873–5100). No unrelated formatting edits were made. `git diff --check` succeeds.

## Commit validation

Pinned `@commitlint/cli@21.2.3` validates the exact full message with `node_modules/.bin/commitlint --edit /tmp/dw-422/commit-message.txt --verbose`: exit 0, `found 0 problems, 0 warnings`. Successful output is retained here and temporarily at `/tmp/dw-422/commitlint.txt`:

```text
fix(ui): bind audit read-only recovery to the rendered grid
```

The user authorized the Git push; no release dispatch or application publication is part of this work.

## Runtime baseline limitation

`aspire ps --format Json --non-interactive` returned no running AppHosts. An isolated `aspire start --no-build --isolated --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json --non-interactive` succeeded using existing assets. `aspire describe` reported 23 resources: nine Running, thirteen Waiting, one Starting (`security`). `aspire wait tenants-ui --timeout 35 --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --non-interactive` exited 17 with a timeout. `aspire otel logs --severity Error --limit 5` recorded Memories tenant-registry initialization errors with `EventStoreGatewayException: Forbidden`. `aspire stop` succeeded before any rebuild.

No healthy distributed-runtime, browser focus, screen-reader or viewport qualification is claimed. The change adds no markup, localization or CSS and does not change command/recovery policy. Dependency pointers, historical evidence, sprint and existing release holds remain outside this closure.

## Ledger and report integrity

The executed check below exits 0: only DW-422 closes; all historical fields remain under `pre-dw-422-*`, all 656 IDs remain (388 done / 268 open), and 240 maintenance rows cover open IDs exactly once. Archive, sprint and dependency pointers have no differences from entry. It also verifies six owned files (with the five late concurrent files fingerprinted separately) and distinct all-passing TRX inventories for the initial runs (12 entry / 224 focused / 6278 full) and the final review runs (230 focused / 6284 full). The initial reports contain twelve added cases; the final reports contain all eighteen. Run after producing the reports above; the script is self-contained and can be saved anywhere.

```python
from pathlib import Path
import re
from hashlib import sha256
import subprocess
import xml.etree.ElementTree as ET

BASE = '8224b5baf9c7d9ce51d53e90eea61d85291afa46'
LEDGER = '_bmad-output/implementation-artifacts/deferred-work.md'
INDEX = 'docs/maintenance-backlog.md'
FIELDS = {b'status', b'disposition', b'verification-date', b'verification', b'verification-files', b'evidence-kind', b'next-action'}
EXPECTED = {LEDGER, INDEX, 'src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor',
            'tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs',
            '_bmad-output/implementation-artifacts/spec-dw-422-audit-read-only-target.md',
            '_bmad-output/implementation-artifacts/dw-422-validation-evidence.md'}

def require(value, message):
    if not value:
        raise SystemExit(message)

def original(path):
    return subprocess.check_output(['git', 'show', f'{BASE}:{path}'])

def records(data):
    pairs = [(m[1], m[0]) for m in re.finditer(rb'^### DW-(\d+):[^\n]*\n.*?(?=^### DW-|\Z)', data, re.M | re.S)]
    require(len(pairs) == len(dict(pairs)), 'Duplicate IDs')
    return dict(pairs)

def without_receipt(data):
    title, receipt, rest = data.split(b'\n\n', 2)
    require(receipt.startswith(b'**DW-422 closure (2026-10-20):**'), 'Missing receipt')
    return title + b'\n\n' + rest

before_data, after_data = original(LEDGER), Path(LEDGER).read_bytes()
before, after = records(before_data), records(after_data)
require(set(before) == set(after) and len(after) == 656, 'ID inventory changed')
require({k for k in before if before[k] != after[k]} == {b'422'}, 'Unrelated record changed')
expected = [b'pre-dw-422-' + line if line.partition(b':')[0] in FIELDS else line for line in before[b'422'].splitlines()]
retained = [line for line in after[b'422'].splitlines() if line.partition(b':')[0] not in FIELDS | {b'resolution-evidence', b'resolution-spec'}]
require(expected == retained, 'Selected historical fields changed')
for key in FIELDS | {b'resolution-evidence', b'resolution-spec'}:
    values = re.findall(rb'^' + key + rb': ([^\n]+)$', after[b'422'], re.M)
    require(len(values) == 1, 'Missing/duplicate field: ' + repr(key))
    if key.startswith(b'resolution-'):
        require(Path(values[0].decode()).is_file(), 'Missing resolution artifact')
require(without_receipt(after_data).split(b'### DW-1:')[0] == before_data.split(b'### DW-1:')[0], 'Preamble changed')
open_ids = {k for k,v in after.items() if re.search(rb'^status: open\b', v, re.M)}
done_ids = {k for k,v in after.items() if re.search(rb'^status: done\b', v, re.M)}
require(len(open_ids) == 268 and len(done_ids) == 388 and open_ids | done_ids == set(after), 'Incorrect counts')
old_index, new_index = original(INDEX), Path(INDEX).read_bytes()
removed = [line for line in old_index.splitlines(keepends=True) if line.startswith(b'| DW-422 |')]
require(len(removed) == 1 and without_receipt(new_index) == old_index.replace(removed[0], b'', 1), 'Unrelated index edit')
rows = [line for line in new_index.splitlines() if line.startswith(b'| DW-')]
covered = [k for line in rows for k in re.findall(rb'DW-(\d+)', line.split(b'|')[1])]
require(len(rows) == 240 and len(covered) == len(set(covered)) and set(covered) == open_ids, 'Incorrect index coverage')
subprocess.run(['git', 'diff', '--exit-code', BASE, '--', 'references/', '_bmad-output/implementation-artifacts/deferred-work-archive.md', '_bmad-output/implementation-artifacts/sprint-status.yaml'], check=True)
changed = subprocess.check_output(['git', 'diff', '--name-only', '-z', BASE]).decode().split('\0')
untracked = subprocess.check_output(['git', 'ls-files', '--others', '--exclude-standard', '-z']).decode().split('\0')
external = {'src/Hexalith.Tenants.Contracts/Commands/AddUserToTenant.cs': 'ba3bd661b272c0f476580935e03015be143130c1d4fe3cba670aee673ed51ec8', 'src/Hexalith.Tenants.Contracts/Commands/SetTenantConfiguration.cs': '3340b6e4a46021da5544f5d7baca89a75a4cbde910ce18d4a2328e76f36c2ee4', 'src/Hexalith.Tenants.Contracts/Module.cs': 'b572283d6f2e81f623b7a7c63791f46072de457707944e84d4556c91b8f447dd', 'src/Hexalith.Tenants.Contracts/Queries/GetTenantAuditQuery.cs': 'ee7dcd443fbf9d817bc336bbe65abaab1fd2a71efb2bb69074a8e80639232061', 'src/Hexalith.Tenants.Contracts/Queries/GetUserTenantsQuery.cs': '7fd94062df7452c91cc019850c2f79e5ec261c135434792b68fd96718686f5fa'}
for path, digest in external.items():
    require(sha256(Path(path).read_bytes()).hexdigest() == digest, 'Concurrent file changed: ' + path)
require((set(changed) | set(untracked)) - {''} - set(external) == EXPECTED, 'Unexpected owned scope')
print('PASS: only DW-422 closes; original fields preserved; 656 IDs / 388 done / 268 open / 240 index rows; six owned files; five observed external files byte-preserved')
ns = {'t': 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
for run, count, added in [('entry', 12, 12), ('focused-corrected', 224, 12), ('full', 6278, 12), ('review-focused', 230, 18), ('review-full', 6284, 18)]:
    root = ET.parse(f'/tmp/dw-422/{run}/results.trx').getroot()
    results = root.findall('.//t:UnitTestResult', ns)
    require(len(results) == count and len({x.attrib['testId'] for x in results}) == count, f'{run}: incorrect inventory')
    require(all(x.attrib['outcome'] == 'Passed' for x in results), f'{run}: nonpassing case')
    new = [x for x in results if 'ReadOnlyRecoveryTargetTracksInvalidFiltersAndReset' in x.attrib['testName']]
    require(len(new) == 12, f'{run}: missing new cases')
    added_results = [x for x in results if 'ReadOnlyRecovery' in x.attrib['testName']]
    require(len(added_results) == added, f'{run}: missing added cases')
    print(f'PASS: {run} {count}/{count} distinct results, all passed, all {added} added cases present')
```

## Independent review corrections

Blind Hunter reviewed the change and reported five improvements (16.908 kB; finding floor five). The owning spec records every verdict. Four are patched: empty-row coverage for all three eligible kinds, distinct newly queried reset data, held late-response coverage for all three kinds, and precise synthetic-event evidence wording. The suggested filtered-second-page reset scenario was rejected as a low-priority expansion: this change qualifies target visibility, and the matrix does not claim to prove disposal of pre-existing paging history. No findings were deferred. The independent correction recheck confirms no remaining concrete issue.

The review build uses the same ordinary Debug/source-reference flags and exits 0 with zero warnings/errors (`/tmp/dw-422-build-review.log`). Focused validation exits 0 with **230/230**, zero failures/skips:

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug --no-build --no-restore --filter-class '*TenantAuditPageTests' --minimum-expected-tests 230 --no-ansi --no-progress --results-directory /tmp/dw-422/review-focused --report-xunit-trx --report-xunit-trx-filename results.trx
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug --no-build --no-restore --minimum-expected-tests 6284 --no-ansi --no-progress --results-directory /tmp/dw-422/review-full --report-xunit-trx --report-xunit-trx-filename results.trx
```

Final whole-file formatting verification repeats the same 22 inherited diagnostics on 20 unchanged lines, with none on the new cases. After the six additional cases shift line numbers, the reported ranges are 671–2704 and 3925–5152 (`/tmp/dw-422-format-review.log`). Comparison against the canonical entry source again confirms every diagnostic belongs to an unchanged block. Scope and historical-record checks still pass.

Final maintained UI exits 0: **6284/6284**, zero failures/skips (`/tmp/dw-422-test-review-full.log`, 1m24.981s). The final report checker passes for all five historical/final reports and confirms all eighteen added cases exactly once in both final inventories. The strengthened reset assertions and six added boundary/late-response cases are final-production qualification; the entry report preserves the original twelve-case theory version.

Final own-story validation, `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-422-audit-read-only-target.md`, returns `RESULT: PASS` with no dependency pointer changes. The final selected ledger/index receipt reports 230 focused / 6284 maintained UI. Only DW-422 closes; all original fields, unrelated records and release holds remain. The spec is complete.

## Late concurrent workspace edits

The final whole-tree scope check initially refused with `Unexpected scope`. Five unrelated Contracts operation-description edits appeared after the earlier six-file check. They were not made or reverted by this task and are excluded from its commit: `Commands/AddUserToTenant.cs`, `Commands/SetTenantConfiguration.cs`, `Module.cs`, `Queries/GetTenantAuditQuery.cs`, and `Queries/GetUserTenantsQuery.cs`, all under `src/Hexalith.Tenants.Contracts/`. The checker above records their exact observed SHA-256 fingerprints and verifies they remain byte-identical while still requiring the exact six owned paths. This scopes the preservation check explicitly; it does not qualify those external edits. The completed Debug source build and full UI evidence apply to this task's committed source, before those text edits appeared. No dependency pointer or history movement accompanied them.
