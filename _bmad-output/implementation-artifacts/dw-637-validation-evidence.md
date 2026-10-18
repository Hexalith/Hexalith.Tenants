# DW-637 validation evidence — 2026-10-18

Entry revision: `45741f9583f78a9beacf315b49241aaf59eb2ff9`, clean `main`. This run selects the next independent actionable authentication test gap from the deferred-work queue. Production already unsubscribes correctly; the permanent C# changes add two component regressions only.

Each test uses the component's existing provider and observes a real notification while mounted. It then removes the rendered root through bUnit, asserts that Blazor actually invoked component disposal, takes the generation baseline after disposal, raises another provider notification and requires no further generation change. Handler-entry increments happen synchronously, so these assertions need no timing-based negative wait. The page also requires no additional reads. Private generation observation deliberately detects retained event subscriptions even when disposal guards prevent visible rendering.

## Pre-edit environment and initial checks

The repository-required Aspire check used its local Aspire skill:

```bash
aspire ps --format Json
aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --isolated --format Json
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire otel logs --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
```

All exit 0. Initially no AppHost was running. The started model reports Tenants, Tenants UI/API, EventStore and security as Running. The telemetry sample contains 169 Information, 30 Warning and one Critical record: Memories reports a sidecar-query replay-safety problem (event 9173). Running state does not qualify distributed health or release readiness; that unrelated diagnostic is outside these component regressions. The local AppHost remains running.

The ordinary audit-enabled Debug source build succeeds with zero warnings/errors. The corrected two-test run passes 2/2, correction-panel class 131/131 and administrator-page class 260/260, all with zero failures/skips.

Two earlier filter expressions selected zero cases: `--filter-method '*Disposed*DoesNotReactToPrincipalChange'` and `--filter-class '*GlobalAdministratorCorrectionPanelTests|*GlobalAdministratorsPageTests'`. Both correctly exit 5 under the minimum-test gate. They supply no acceptance evidence. Use one suffix glob and separate class invocations below.

The first external Razor mutation build failed with RZ10012/CS0246 because its physical path changed the generated component namespace. No mutation test ran from that failed build. Adding the namespace alone also failed with the same two errors. The successful generator places temporary mutants inside the ignored UI `obj/dw-637` directory, adds the original namespace explicitly and preserves logical path, imports and CSS scope. Each mutant otherwise only removes one unsubscribe statement. No failed build supplies mutation-test evidence.

## Reproduce with fresh temporary outputs

Run from the repository root. Do not run builds concurrently with another build/test session using the same output directories. Tracked production source files remain untouched; ignored generated Razor files and mutation builds replace local generated binaries until the final normal rebuild. The commands preserve audit checks and Debug source references.

```bash
set -euo pipefail
export DW637_RUN_DIRECTORY="$(mktemp -d /tmp/dw-637-reproduction.XXXXXX)"
python3 - <<'PYGEN'
#!/usr/bin/env python3
"""Create isolated Razor unsubscribe mutations for DW-637 verification."""

from hashlib import sha256
import json
import os
from pathlib import Path
import xml.etree.ElementTree as ET


ROOT = Path.cwd()
OUTPUT = Path(os.environ['DW637_RUN_DIRECTORY'])
PROJECT = ROOT / 'src/Hexalith.Tenants.UI'
STATEMENT = b'_authenticationStateProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;'
MUTATIONS = (
    ('panel', 'Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor',
     'Hexalith.Tenants.UI.Components.Tenants.Audit'),
    ('page', 'Components/Pages/GlobalAdministratorsPage.razor',
     'Hexalith.Tenants.UI.Components.Pages'),
)


def main():
    results = []
    for name, logical_path, namespace in MUTATIONS:
        source = PROJECT / logical_path
        original = source.read_bytes()
        lines = original.splitlines(keepends=True)
        matches = [index for index, line in enumerate(lines) if line.strip() == STATEMENT]
        if len(matches) != 1:
            raise RuntimeError(f'{source}: expected exactly one unsubscribe, got {len(matches)}')

        directory = OUTPUT / name
        directory.mkdir(parents=True, exist_ok=True)
        mutant_directory = PROJECT / 'obj/dw-637' / name
        mutant_directory.mkdir(parents=True, exist_ok=True)
        mutant = mutant_directory / source.name
        newline = b'\r\n' if original.startswith(lines[0].rstrip(b'\r\n') + b'\r\n') else b'\n'
        declaration = f'@namespace {namespace}'.encode('utf-8') + newline
        mutant.write_bytes(declaration + b''.join(
            line for index, line in enumerate(lines) if index != matches[0]))

        project = ET.Element('Project')
        target = ET.SubElement(project, 'Target', {
            'Name': f'DW637Replace{name.title()}Razor',
            'BeforeTargets': 'AssignRazorComponentTargetPaths',
            'DependsOnTargets': 'ResolveCssScopes',
            'Condition': "'$(MSBuildProjectName)' == 'Hexalith.Tenants.UI'",
        })
        capture = ET.SubElement(target, 'ItemGroup')
        ET.SubElement(capture, '_DW637OriginalRazor', {
            'Include': '@(RazorComponent)',
            'Condition': f"'%(RazorComponent.Identity)' == '{logical_path}'",
        })
        ET.SubElement(target, 'Error', {
            'Condition': "'@(_DW637OriginalRazor->Count())' != '1'",
            'Text': f'DW-637: expected one original RazorComponent for {logical_path}; got @(_DW637OriginalRazor->Count()).',
        })
        replacement = ET.SubElement(target, 'ItemGroup')
        ET.SubElement(replacement, 'RazorComponent', {'Remove': '@(_DW637OriginalRazor)'})
        ET.SubElement(replacement, 'RazorComponent', {
            'Include': str(mutant),
            'Link': logical_path,
            'TargetPath': logical_path,
            'CssScope': "@(_DW637OriginalRazor->'%(CssScope)')",
        })
        ET.SubElement(target, 'Message', {
            'Importance': 'High',
            'Text': f'DW-637 {name} mutation overlay: {mutant} -> {logical_path}; CssScope=@(_DW637OriginalRazor->\'%(CssScope)\').',
        })
        ET.indent(project, space='  ')
        targets = directory / 'mutation.targets'
        ET.ElementTree(project).write(targets, encoding='utf-8', xml_declaration=True)
        with targets.open('ab') as stream:
            stream.write(b'\n')

        if source.read_bytes() != original:
            raise RuntimeError(f'Production source changed unexpectedly: {source}')
        results.append({
            'mutation': name,
            'source': str(source),
            'source_sha256': sha256(original).hexdigest(),
            'mutant': str(mutant),
            'targets': str(targets),
            'compilation_namespace': namespace,
            'removed_line': matches[0] + 1,
        })
    print(json.dumps(results, indent=2))


if __name__ == '__main__':
    main()
PYGEN
```

The following commands show the successful validation shape. Use unique results directories; do not accept a previous TRX after a failed launch. After each test, require the named cases and counts below from the newly produced TRX. After each deliberately failing mutation, restore a normal build before further ordinary testing.

```bash
(
set -euo pipefail
project=tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj
dotnet build "$project" --configuration Debug -m:1 -nr:false -p:UseHexalithProjectReferences=true
dotnet test --project "$project" --configuration Debug --no-build --no-restore --filter-method '*DoesNotReactToPrincipalChange' --minimum-expected-tests 2 --no-ansi --no-progress --results-directory "$DW637_RUN_DIRECTORY/initial-corrected" --report-xunit-trx --report-xunit-trx-filename results.trx
dotnet test --project "$project" --configuration Debug --no-build --no-restore --filter-class '*GlobalAdministratorCorrectionPanelTests' --minimum-expected-tests 131 --no-ansi --no-progress --results-directory "$DW637_RUN_DIRECTORY/focused-panel" --report-xunit-trx --report-xunit-trx-filename results.trx
dotnet test --project "$project" --configuration Debug --no-build --no-restore --filter-class '*GlobalAdministratorsPageTests' --minimum-expected-tests 260 --no-ansi --no-progress --results-directory "$DW637_RUN_DIRECTORY/focused-page" --report-xunit-trx --report-xunit-trx-filename results.trx
restore_required=false
restore_normal() {
    if "$restore_required"; then
        dotnet build "$project" --configuration Debug -m:1 -nr:false -p:UseHexalithProjectReferences=true
    fi
}
trap restore_normal EXIT
for mutation in panel page; do
    restore_required=true
    dotnet build "$project" --configuration Debug -m:1 -nr:false -p:UseHexalithProjectReferences=true "-p:CustomAfterMicrosoftCommonTargets=$DW637_RUN_DIRECTORY/$mutation/mutation.targets"
    if dotnet test --project "$project" --configuration Debug --no-build --no-restore --filter-method '*DoesNotReactToPrincipalChange' --minimum-expected-tests 2 --no-ansi --no-progress --results-directory "$DW637_RUN_DIRECTORY/$mutation-result" --report-xunit-trx --report-xunit-trx-filename results.trx; then
        echo 'Unsubscription mutation unexpectedly passed' >&2
        exit 1
    else
        result=$?
        test "$result" -eq 2
    fi
    python3 - "$DW637_RUN_DIRECTORY/$mutation-result/results.trx" "$mutation" <<'PYTRX'
from pathlib import Path
import sys
import xml.etree.ElementTree as E
ns = {'t': 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
root = E.parse(Path(sys.argv[1])).getroot()
c = root.find('t:ResultSummary/t:Counters', ns).attrib
expected = {'total': 2, 'executed': 2, 'passed': 1, 'failed': 1}
if any(int(c[k]) != v for k,v in expected.items()) or any(int(v) for k,v in c.items() if k not in expected):
    raise RuntimeError('Mutation counters differ from exactly one pass and one failure')
results = root.findall('t:Results/t:UnitTestResult', ns)
failed = 'DisposedCorrectionPanelDoesNotReactToPrincipalChange' if sys.argv[2] == 'panel' else 'DisposedAdministratorsPageDoesNotReactToPrincipalChange'
expected_names = {'DisposedCorrectionPanelDoesNotReactToPrincipalChange', 'DisposedAdministratorsPageDoesNotReactToPrincipalChange'}
if len(results) != 2 or {r.attrib['testName'].rsplit('.', 1)[-1] for r in results} != expected_names:
    raise RuntimeError('Missing or duplicate mutation test identity')
for result in results:
    name = result.attrib['testName'].rsplit('.', 1)[-1]
    if result.attrib['outcome'] != ('Failed' if name == failed else 'Passed'):
        raise RuntimeError('Wrong mutation failure identity')
    if name == failed:
        message = result.find('t:Output/t:ErrorInfo/t:Message', ns).text
        field = '_authorizationGeneration' if sys.argv[2] == 'panel' else '_authorizationTransitionVersion'
        if field not in message or '2L' not in message or '3L' not in message:
            raise RuntimeError('Unexpected mutation failure diagnostic')
print('PASS: only the matching missing-unsubscribe test fails; its control passes.')
PYTRX
done
dotnet build "$project" --configuration Debug -m:1 -nr:false -p:UseHexalithProjectReferences=true
dotnet test --project "$project" --configuration Debug --no-build --no-restore --minimum-expected-tests 6266 --no-ansi --no-progress --results-directory "$DW637_RUN_DIRECTORY/full" --report-xunit-trx --report-xunit-trx-filename results.trx
restore_required=false
trap - EXIT
verify_passing() {
    python3 - "$@" <<'PYPASS'
from pathlib import Path
import sys
import xml.etree.ElementTree as E
ns = {'t': 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
root = E.parse(Path(sys.argv[1])).getroot()
expected_count = int(sys.argv[2])
prefix = sys.argv[3]
c = root.find('t:ResultSummary/t:Counters', ns).attrib
expected = {'total': expected_count, 'executed': expected_count, 'passed': expected_count}
if any(int(c[k]) != v for k,v in expected.items()) or any(int(v) for k,v in c.items() if k not in expected):
    raise RuntimeError('Ordinary counters contain failures, errors, skips or an unexpected count')
results = root.findall('t:Results/t:UnitTestResult', ns)
if len(results) != expected_count or any(r.attrib['outcome'] != 'Passed' for r in results):
    raise RuntimeError('Not all expected result elements passed')
for key in ['testId', 'executionId']:
    if len({r.attrib[key] for r in results}) != expected_count:
        raise RuntimeError('Duplicate ordinary test identity: ' + key)
if prefix != '.' and any(not r.attrib['testName'].startswith(prefix + '.') for r in results):
    raise RuntimeError('Unexpected focused test class')
for method in sys.argv[4:]:
    if sum(r.attrib['testName'].endswith('.' + method) for r in results) != 1:
        raise RuntimeError('Missing or duplicate required test method: ' + method)
print('PASS:', expected_count, 'distinct passing tests; no skips/errors; required cases present.')
PYPASS
}
verify_passing "$DW637_RUN_DIRECTORY/initial-corrected/results.trx" 2 . DisposedCorrectionPanelDoesNotReactToPrincipalChange DisposedAdministratorsPageDoesNotReactToPrincipalChange
verify_passing "$DW637_RUN_DIRECTORY/focused-panel/results.trx" 131 Hexalith.Tenants.UI.Tests.Components.GlobalAdministratorCorrectionPanelTests DisposedCorrectionPanelDoesNotReactToPrincipalChange
verify_passing "$DW637_RUN_DIRECTORY/focused-page/results.trx" 260 Hexalith.Tenants.UI.Tests.Components.GlobalAdministratorsPageTests DisposedAdministratorsPageDoesNotReactToPrincipalChange
verify_passing "$DW637_RUN_DIRECTORY/full/results.trx" 6266 . DisposedCorrectionPanelDoesNotReactToPrincipalChange DisposedAdministratorsPageDoesNotReactToPrincipalChange
)
```

If any mutation build fails, stop and rebuild normally; a compilation failure is not evidence that a regression test detects a missing unsubscribe. These commands share standard generated output folders; the repository's tracked production source stays unchanged.

## Executed results

All ordinary/accepted builds exit **0**, with **0 warnings / 0 errors**. Corrected new tests, both focused classes and final maintained full UI pass with **zero failures/errors/skips**. Mutation tests intentionally exit **2**, with exactly the matching case failing on the post-disposal generation assertion and its counterpart passing. The complete full-suite TRX has 6,266 distinct test IDs, all Passed, and exactly one result for each new regression.

| Run | Total | Passed | Failed | Not executed | Start UTC | Finish UTC |
| --- | ---: | ---: | ---: | ---: | --- | --- |
| initial-corrected | 2 | 2 | 0 | 0 | 2026-10-18T17:40:03.9285846+00:00 | 2026-10-18T17:40:04.4714187+00:00 |
| focused-panel | 131 | 131 | 0 | 0 | 2026-10-18T17:40:27.7183782+00:00 | 2026-10-18T17:40:30.9419384+00:00 |
| focused-page | 260 | 260 | 0 | 0 | 2026-10-18T17:40:57.9725969+00:00 | 2026-10-18T17:41:27.6438390+00:00 |
| panel-mutation | 2 | 1 | 1 | 0 | 2026-10-18T17:47:30.6867961+00:00 | 2026-10-18T17:47:31.7723016+00:00 |
| page-mutation | 2 | 1 | 1 | 0 | 2026-10-18T17:48:32.5156874+00:00 | 2026-10-18T17:48:33.2322614+00:00 |
| full | 6266 | 6266 | 0 | 0 | 2026-10-18T17:49:03.8941243+00:00 | 2026-10-18T17:50:56.2946809+00:00 |

Durable failure excerpts:

```text
panel mutation: DisposedCorrectionPanelDoesNotReactToPrincipalChange
PrivateField<long>(cut.Instance, "_authorizationGeneration")
    should be 2L, but was 3L
page mutation: DisposedAdministratorsPageDoesNotReactToPrincipalChange
PrivateField<long>(cut.Instance, "_authorizationTransitionVersion")
    should be 2L, but was 3L
Both runs: total 2, failed 1, succeeded 1, skipped 0; exit 2.
```

The actual ordinary initial/final builds and tests used the same commands as the recipe, with `/tmp/dw-637` outputs: initial test `initial-corrected`, focused classes `focused-panel` and `focused-page`, mutation tests `panel-mutation` and `page-mutation`, final `full`. Successful mutation builds additionally used `-p:EmitCompilerGeneratedFiles=true -p:CompilerGeneratedFilesOutputPath=/tmp/dw-637/generated-panel` for compiler inspection. The final normal build omits both diagnostic properties and the custom overlay. Initial/final build durations are 53.34s/21.18s; successful panel/page mutation builds 112.08s/34.90s. Their logs remain temporarily at `/tmp/dw-637/build-initial.log`, `build-final.log`, `build-panel-mutation-obj.log` and `build-page-mutation.log`. Failed external overlays remain in `build-panel-mutation.log` and `build-panel-mutation-corrected.log`. Test logs use `test-<run>.log`, with `test-full.log` for the full suite. These parsed counts and excerpts are durable summaries; full logs/TRX remain temporary and can be regenerated. No full historical-artifact retention is claimed.


## Formatting check

```bash
dotnet format whitespace tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --include tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorCorrectionPanelTests.cs tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs --no-restore --verify-no-changes
```

Exit **2**: 82 existing WHITESPACE diagnostics in unchanged portions of `GlobalAdministratorsPageTests.cs`; the first is at line 149, beyond the inserted test. No diagnostic is in either added test, and the correction-panel file has none. No broad formatting changes were made. `git diff --check` passes. This does not claim that the existing files meet the complete formatter gate.

## Scope and preservation

Only DW-637 and its maintenance row close. All 655 IDs remain: **378 done / 277 open**, **190 actionable**, across **249 index rows** covering every open ID exactly once. Prior fields survive as `pre-dw-637-*`. Production Razor, archive/sprint and dependency pointers remain unchanged. Historical release qualification holds remain; these component tests do not claim browser, deployment, broad backend/solution, package or release acceptance.

The following executable check compares all unrelated ledger blocks and index contents against the canonical entry revision, verifies original target fields, exact coverage, protected files, local links, six allowed changed/new paths and C# CRLF. Run from the repository root:

```bash
python3 - <<'PYCHECK'
from collections import Counter
from pathlib import Path
import re
import subprocess

BASE = '45741f9583f78a9beacf315b49241aaf59eb2ff9'
LEDGER = '_bmad-output/implementation-artifacts/deferred-work.md'
INDEX = 'docs/maintenance-backlog.md'
SPEC = '_bmad-output/implementation-artifacts/spec-dw-637-authentication-disposal-regressions.md'
EVIDENCE = '_bmad-output/implementation-artifacts/dw-637-validation-evidence.md'
TESTS = ['tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorCorrectionPanelTests.cs',
         'tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs']

def require(condition, message):
    if not condition:
        raise RuntimeError(message)

def git(*args):
    return subprocess.check_output(['git', *args], text=True)

def original(path):
    return git('show', BASE + ':' + path)

def entries(text):
    matches = list(re.finditer(r'^### (DW-\d+):[^\n]*\n', text, re.M))
    result = {m[1]: text[m.start():matches[i+1].start() if i+1 < len(matches) else len(text)]
              for i,m in enumerate(matches)}
    require(len(matches) == len(result), 'Duplicate ledger IDs')
    return result

ledger = Path(LEDGER).read_text()
stripped_ledger, count = re.subn(r'(?<=# Deferred Work\n\n)\*\*DW-637 closure .*?\n\n', '', ledger, count=1)
require(count == 1 and stripped_ledger.split('### DW-1:', 1)[0] == original(LEDGER).split('### DW-1:', 1)[0], 'Historical ledger preamble changed')
before, after = entries(original(LEDGER)), entries(ledger)
require(set(before) == set(after) and len(after) == 655, 'Ledger IDs changed')
require(all(before[k] == after[k] for k in before if k != 'DW-637'), 'Unrelated ledger record changed')
fields = lambda s: dict(re.findall(r'^([a-z][a-z0-9-]*): (.*)$', s, re.M))
old, new = fields(before['DW-637']), fields(after['DW-637'])
for key, value in old.items():
    require(new.get(key) == value or new.get('pre-dw-637-' + key) == value, 'Lost original field: ' + key)
require(new['status'] == 'done 2026-10-18' and new['disposition'] == 'done', 'Target not closed')
open_ids = {key for key, body in after.items() if '\nstatus: open\n' in body}
require(len(open_ids) == 277, 'Wrong open count')
require(sum(fields(body).get('disposition') == 'actionable' for body in after.values()) == 190, 'Wrong actionable count')
old_index = original(INDEX)
row = [line for line in old_index.splitlines(keepends=True) if line.startswith('| DW-637 |')]
require(len(row) == 1, 'Expected one original index row')
expected = old_index.replace(row[0], '', 1).replace('| done | 377 |', '| done | 378 |', 1).replace('| actionable | 191 |', '| actionable | 190 |', 1).replace('**Remaining: 278 open records across 250 index rows.**', '**Remaining: 277 open records across 249 index rows.**', 1)
index = Path(INDEX).read_text()
stripped, count = re.subn(r'(?<=# Maintenance backlog\n\n)\*\*DW-637 closure .*?\n\n', '', index, count=1)
require(count == 1 and stripped == expected, 'Unrelated index content changed')
rows = [line for line in index.splitlines() if line.startswith('| DW-')]
coverage = Counter(key for row in rows for key in re.findall(r'DW-\d+', row.split('|')[1]))
require(len(rows) == 249 and set(coverage) == open_ids and set(coverage.values()) == {1}, 'Incomplete/duplicate index coverage')
for path in ['src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor',
             'src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor',
             '_bmad-output/implementation-artifacts/deferred-work-archive.md',
             '_bmad-output/implementation-artifacts/sprint-status.yaml']:
    require(Path(path).read_text() == original(path), 'Protected file changed: ' + path)
for path in TESTS:
    data = Path(path).read_bytes()
    require(data.count(b'\n') == data.count(b'\r\n'), 'C# CRLF policy failed: ' + path)
allowed = {LEDGER, INDEX, SPEC, EVIDENCE, *TESTS}
changed = set(git('diff', BASE, '--name-only').splitlines()) | set(git('ls-files', '--others', '--exclude-standard').splitlines())
require(changed == allowed, 'Unrelated paths changed: ' + repr(changed - allowed))
for link in re.findall(r'\]\(([^)]+)\)', Path(EVIDENCE).read_text() + Path(SPEC).read_text()):
    if not link.startswith(('http:', 'https:', '#')):
        require((Path(EVIDENCE).parent / link.split('#')[0]).exists(), 'Missing relative link: ' + link)
print('PASS: all 655 IDs, 378 done/277 open, 190 actionable, 249 exact-covering index rows; original fields/unrelated records and protected files preserved; C# CRLF and allowed paths verified.')
PYCHECK
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-637-authentication-disposal-regressions.md
git diff --check
```

All three exit **0**. The story-gitlink guard reports no `references/` pointer changes. The two C# additions are CRLF; Git emits its existing normalization notice when reading them.

Pinned `@commitlint/cli@21.2.3` validates the exact full local candidate `test(ui): pin administrator authentication disposal`, exit **0**, **0 problems / 0 warnings**:

```bash
node_modules/.bin/commitlint --edit /tmp/dw-637/commit-message.txt --verbose
```

The invoked oneshot workflow requires a local commit. No push is requested.

## Review corrections

The configured Blind Hunter layer reviewed all six changed/new files; no configured layer was skipped. Two findings are patched, three broader test proposals are rejected with scope/code evidence, and consolidating append-only notes is refuted by workflow requirements. All six decisions appear in the spec; no finding is deferred and no new deferred ID is added.

The final tests use `await DisposeComponentsAsync()` and assert the captured component's `_disposed` flag before raising the post-disposal notification. This exercises renderer lifecycle wiring as well as unsubscription. The first correction incorrectly used `IRenderedComponent.Dispose()`, which disposes only its wrapper; both new assertions failed (2 total / 0 passed / 2 failed, exit 2). That report is `/tmp/dw-637/review-initial/results.trx` and its ordinary build is `build-review.log` (0 warnings/errors). It is a rejected fixture attempt. The corrected renderer path is the same API already used by neighboring lifecycle tests.

The ordinary-report checker initially rejected three existing theory inputs because xUnit truncates their display names to the same text. It now requires distinct test IDs and execution IDs, exact counters/outcomes and each required method once. The corrected checker passes all four pre-review ordinary reports; display names are not identity keys.

Final revalidation after these test/evidence changes passes. Earlier full-suite and mutation receipts above describe the pre-review tests and remain historical evidence.


| Post-review run | Total | Passed | Failed | Not executed | Start UTC | Finish UTC |
| --- | ---: | ---: | ---: | ---: | --- | --- |
| review-initial | 2 | 0 | 2 | 0 | 2026-10-18T17:57:48.2445084+00:00 | 2026-10-18T17:57:49.0102233+00:00 |
| review-corrected | 2 | 2 | 0 | 0 | 2026-10-18T17:59:36.3653765+00:00 | 2026-10-18T17:59:36.9823627+00:00 |
| review-panel-mutation | 2 | 1 | 1 | 0 | 2026-10-18T18:00:25.4599183+00:00 | 2026-10-18T18:00:26.0315668+00:00 |
| review-page-mutation | 2 | 1 | 1 | 0 | 2026-10-18T18:00:41.5797717+00:00 | 2026-10-18T18:00:42.1599334+00:00 |
| post-review-full | 6266 | 6266 | 0 | 0 | 2026-10-18T18:01:01.2918162+00:00 | 2026-10-18T18:02:43.2712954+00:00 |

The corrected renderer-disposal pair passes **2/2**. Both repeated mutation builds succeed with **zero warnings/errors**, using the portable recipe's ordinary source-build flags plus only the respective `CustomAfterMicrosoftCommonTargets` overlay; no emitted-source diagnostic properties are needed. Both mutation reports pass the embedded mutation checker, with exactly the matching expected-2L/actual-3L failure and its passing counterpart. The restored ordinary build (`build-post-review-final.log`) exits **0**, **0 warnings / 0 errors**, 17.18s. Final maintained UI (`test-post-review-full.log`) exits **0**, **6266 passed / 0 failed / 0 skipped**. The executable passing-run checker confirms 6,266 unique test/execution IDs and both new methods exactly once.

These final invocations use the recipe's exact build/test flags with result directories `/tmp/dw-637/review-corrected`, `review-panel-mutation`, `review-page-mutation` and `post-review-full`; the mutation logs are `build-review-{panel,page}-mutation.log` and `test-review-{panel,page}-mutation.log`. The panel mutation build takes 17.65s; the page mutation build reports `00:00:13.73`. The corrected positive fixture build is `build-review-corrected.log` (49.43s, zero warnings/errors). All temporary logs and reports are local execution artifacts, with durable parsed receipts above.

Independent correction recheck confirms both requested improvements and finds no remaining concrete issue. It independently executes the passing-report checker against the retained pre-review reports and corrected 2/2 fixture. Final mutation/full-suite verification then completes successfully as recorded above. No finding is deferred. The exact embedded preservation check also passes under Python optimization; shell recipes parse, local links resolve, the story-gitlink guard passes, and `git diff --check` passes. The formatter's existing 82 diagnostics remain an explicitly reported broad formatting limitation. Production, archive/sprint and dependencies remain unchanged.
