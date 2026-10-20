# DW-155 validation evidence — 2026-10-20

Entry revision: `630b072c3b48b1907705a7ed5b3c2360e2d94882`, clean main.

## Scope and behavior

DW-155 is the oldest actionable Tenants record with no dependency. Earlier open actionable items require another owner, a privacy requirement, or a route/topology contract. The change qualifies the existing member-pager behavior and removes visibility widened solely for tests; it does not claim a reproduced runtime defect.

The ten final `MemberPagingControls*` cases replace one direct-handler test (the initial eight were strengthened after review). They capture the rendered Fluent buttons' `OnClick` callbacks while enabled, then deliver events after refresh, open change-role/removal flows, or direction boundaries. This represents already-dispatched events; ordinary disabled DOM clicks alone cannot exercise the component guards. Tests also verify rendered disabled attributes, ordinary enabled DOM delivery, preservation of the same open child component, cancellation recovery and each boundary combination, including removal of the pager. Two real-page cases hold the actual member read in either direction, deliver both queued callbacks, assert exactly one read with the correct cursor, release the read and navigate back/forward using rendered controls. The change-role/removal cases also preserve edited role/confirmation values and hold real command submissions in RequestSent, retain their message IDs and allow exactly one dispatch before completing with a controlled rejection. Boundary cases restore both directions and exercise newly rendered controls after pager reappearance. No new sleeps or private-handler reflection is used.

All twelve direct handler invocations are replaced, including four existing page-test callers and the shared page-two helper. Both handlers are private again. Their guard expressions, button bindings and UI behavior remain unchanged. DW-610 concerns separate membership panels and remains open. The final command-flow cases qualify open editors, pending submissions, retained identity and controlled rejection. They do not newly qualify accepted-command polling, browser behavior or assistive technology.

## Entry-runtime and negative controls

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 -v:q
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-method '*MemberPagingControls*' --minimum-expected-tests 8 --no-ansi --no-progress --results-directory /tmp/dw155/entry-runtime --report-xunit-trx --report-xunit-trx-filename results.trx
python3 /tmp/dw155/mutation-check.py
```

The first build fails with CS1929 because the generic bUnit helper lacked a reference-type constraint (`/tmp/dw155/build-entry-runtime.log`). Adding `class` to the helper's constraint fixes the fixture. The corrected Debug source build exits 0 with zero warnings/errors (`build-entry-runtime-fixed.log`), and all eight new cases pass against unchanged entry runtime (`tests-entry-runtime.log`, `entry-runtime/results.trx`). The subsequent private-handler build also exits 0 with zero warnings/errors (`build-private.log`).

The local mutation driver builds each temporary source variant and runs the same eight cases with a separate results directory. It restores the intended source in a `finally` block. Every mutation builds successfully and the test command exits 2:

| Temporary change | Failed / total | Matching evidence |
| --- | --- | --- |
| Remove both refreshing guards | 2 / 8 | Both refresh/open-flow theories fail during refresh |
| Remove both open-flow guards | 2 / 8 | Change-role and removal cases fail with the editor open |
| Remove both direction boundary guards | 3 / 8 | Both unavailable and each single unavailable direction fail |
| Bind Next to Previous | 7 / 8 | Rendered callback/navigation controls detect incorrect wiring |

Exact commands are retained in `/tmp/dw155/mutation-check.py`; build/test logs are `/tmp/dw155/mutation-<name>-build.log` and `mutation-<name>-tests.log`, with each TRX in `mutation-<name>/results.trx`. The names are `refresh-guards`, `open-flow-guards`, `boundary-guards` and `next-binding`. `/tmp/dw155/mutations.json` preserves exit statuses. These are four separate controls; they do not claim independent mutation of all six individual predicates. The real-page parent also has guards, so the component callback counters supply the direct evidence for the component guards.

## Runtime baseline and limits

```bash
aspire ps --format Json
aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --no-build --format Json
aspire wait tenants-ui --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --timeout 45 --non-interactive
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire otel logs --severity Error --limit 3 --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json --non-interactive
aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --non-interactive
```

No AppHost was initially running. Start, the corrected wait, describe, telemetry and stop exit 0. The initial wait attempted unsupported `--format Json` and exited 1 with usage text; the command above corrects it. The final state snapshot reports 23 Running/Healthy resources (`/tmp/dw155-aspire-describe.json`), but the bounded Error query includes bootstrap ActorMethodInvocationException and HTTP 500 errors (`/tmp/dw155-aspire-errors.json`). The started AppHost was stopped successfully before changed-runtime builds. This is a recorded baseline, not an entirely healthy topology or browser qualification.

## Initial acceptance and integrity (before review corrections)

After restoring the intended source:

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 -v:q
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-method '*MemberPagingControls*' --minimum-expected-tests 8 --no-ansi --no-progress --results-directory /tmp/dw155/focused --report-xunit-trx --report-xunit-trx-filename results.trx
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 6571 --no-ansi --no-progress --results-directory /tmp/dw155/full --report-xunit-trx --report-xunit-trx-filename results.trx
dotnet format whitespace tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --include /home/administrator/projects/hexalith/tenants/tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs --verify-no-changes --no-restore
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-155-rendered-member-paging.md
git diff --check
python3 /tmp/dw155/close-ledger.py
```

The final Debug source build exits 0 with zero warnings/errors (`/tmp/dw155/build-final.log`). Focused 8/8 and full UI 6,571/6,571 pass with zero failures/skips (`tests-focused.log`, `tests-full.log`, respective `focused/results.trx` and `full/results.trx`). This is seven more cases than the entry suite because eight replace one. The scoped formatting check exits 0 with the generic workspace-load warning and no formatting diagnostic (`format.log`); it does not claim whole-repository formatting conformance. Own-spec gitlink and whitespace checks pass; Git also reports the existing CRLF-to-LF normalization notice for the Razor worktree file.

The local closure script gates on exact all-passing focused/full TRX inventories and unchanged ledger/index input snapshots. It preserves every replaced field as `pre-dw-155-*`, verifies only DW-155 changes among 656 records, and removes exactly its index row. The result is 397 done/259 open and 234 maintenance rows covering every open ID exactly once (`/tmp/dw155/ledger-closure.txt`). Archive, sprint status, dependency pointers, unrelated records and existing release qualification holds remain preserved.

Pinned commitlint 21.2.3 validates the exact full message below with zero problems/warnings via `node_modules/.bin/commitlint --edit /tmp/dw155/commit-message.txt --verbose` (`/tmp/dw155/commitlint.txt`):

```text
refactor(ui): qualify member paging through rendered controls
```

`git fetch origin` succeeds and `git rev-list --left-right --count main...origin/main` reports `0 0` before committing. The user explicitly authorized the final main push. No release dispatch is included.

## Durable mutation procedure

The following is the complete final control procedure. Run it as Python from the owning repository root after the tests compile; it temporarily edits only the component, restores it in `finally`, and leaves mutation build outputs that require a normal build before acceptance testing. Initial controls above used the eight-case version; the final procedure selects all ten cases after review strengthened pending-command coverage. It writes separate results so the initial control evidence is retained.

```python
from pathlib import Path
import json
import subprocess

root = Path.cwd()
output = Path('/tmp/dw155/review-mutations')
output.mkdir(parents=True, exist_ok=True)
component = root / 'src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor'
original = component.read_bytes()
build = ['dotnet', 'build', 'tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj', '-c', 'Debug', '-p:UseNuGetDeps=false', '-m:1', '-v:q']
test = ['dotnet', 'test', '--project', 'tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj', '-c', 'Debug', '--no-build', '--no-restore', '--filter-method', '*MemberPagingControls*', '--minimum-expected-tests', '10', '--no-ansi', '--no-progress', '--report-xunit-trx', '--report-xunit-trx-filename', 'results.trx']
mutations = {
    'refresh-guards': original.replace(b' && !Members.IsRefreshing', b''),
    'open-flow-guards': original.replace(b' && !HasOpenMemberCommandFlow', b''),
    'boundary-guards': original.replace(b'=> Members.HasMore && ', b'=> ').replace(b'=> CanGoPrevious && ', b'=> '),
    'next-binding': original.replace(b'OnClick="RequestNextPageAsync"', b'OnClick="RequestPreviousPageAsync"'),
}
results = []
try:
    for name, mutated in mutations.items():
        if mutated == original:
            raise RuntimeError(f'No mutation for {name}')
        component.write_bytes(mutated)
        with open(output / f'{name}-build.log', 'w') as log:
            built = subprocess.run(build, cwd=root, stdout=log, stderr=subprocess.STDOUT)
        if built.returncode:
            raise RuntimeError(f'{name} did not build')
        with open(output / f'{name}-tests.log', 'w') as log:
            tested = subprocess.run(test + ['--results-directory', str(output / name)], cwd=root, stdout=log, stderr=subprocess.STDOUT)
        results.append({'mutation': name, 'build_exit': built.returncode, 'test_exit': tested.returncode})
        print(json.dumps(results[-1]), flush=True)
        if tested.returncode != 2:
            raise RuntimeError(f'{name} must fail tests, got {tested.returncode}')
finally:
    component.write_bytes(original)
    (output / 'mutations.json').write_text(json.dumps(results, indent=2) + '\n')
```

## Independent review and corrections

The blind reviewer returned five findings. Each is addressed: edited-form values now survive queued paging; both change-role and removal submit an actual command whose response remains pending while the events are delivered, preserving RequestSent and the same message ID with one gateway dispatch; all four direction boundaries recover through fresh rendered buttons; the full mutation procedure is embedded above; and the initial acceptance section records the completed 6,571-case run. That last section was added while review was running, before its report arrived. No finding requires a runtime behavior change or deferred follow-up.

The first review build fails with CS0029 because `ShouldNotBeNullOrWhiteSpace` returns void (`/tmp/dw155/build-review.log`). Capturing the message ID separately fixes the fixture; the corrected Debug source build has zero warnings/errors (`build-review-fixed.log`). Strengthened focused qualification passes 10/10 with zero failures/skips (`tests-focused-review.log`, `focused-review/results.trx`) using the same focused command with minimum 10 and the new results directory.

The embedded procedure was executed as `python3 /tmp/dw155/mutation-check-review.py` from the repository root. All four variants build with exit 0 and fail with exit 2. The final ten-case inventories report refresh guards 4 failed/6 passed, open-flow guards 4/6, boundary guards 3/7 and Next-to-Previous binding 10/0, with zero skips in all variants. Exact logs are `/tmp/dw155/review-mutations/<name>-build.log` and `<name>-tests.log`, with TRX at `<name>/results.trx` and exit evidence in `mutations.json`. The intended private handlers, original guard expressions and correct button bindings were explicitly checked after restoration.

## Final acceptance after review

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 -v:q
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-method '*MemberPagingControls*' --minimum-expected-tests 10 --no-ansi --no-progress --results-directory /tmp/dw155/focused-review-final --report-xunit-trx --report-xunit-trx-filename results.trx
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 6573 --no-ansi --no-progress --results-directory /tmp/dw155/full-review-final --report-xunit-trx --report-xunit-trx-filename results.trx
dotnet format whitespace tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --include /home/administrator/projects/hexalith/tenants/tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs --verify-no-changes --no-restore
```

The final restored-source Debug build exits 0 with zero warnings/errors (`/tmp/dw155/build-review-final.log`). Focused 10/10 and full UI 6,573/6,573 pass with zero failures/skips (`tests-focused-review-final.log`, `tests-full-review-final.log` and their respective `focused-review-final/results.trx`, `full-review-final/results.trx`). These are the final test sources: ten cases replace one entry case, yielding nine additional tests. The final scoped formatting check exits 0 with only the generic workspace-load warning (`format-review-final.log`). No source changes follow these checks.

The reviewer independently rechecks all five corrections and confirms no remaining concrete code/test issue. It verifies all four final mutation inventories and ledger isolation, including preserved historical fields, the one removed index row and exact open-ID coverage. No deferred follow-up is needed. After final acceptance, the target closure summary/verification is refreshed to the strongest results above; the original 656 IDs, 397 done/259 open and 234 exactly covering maintenance rows remain verified. Every unrelated record body still equals the entry snapshot. Own-spec gitlink, whitespace and pinned commitlint checks remain the final commit gates; the user-authorized push targets main.
