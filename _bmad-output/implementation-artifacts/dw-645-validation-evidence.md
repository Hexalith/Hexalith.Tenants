# DW-645 pre-dispatch release-fault recovery — 2026-10-17

Entry: clean `main`, canonical HEAD `e667d5391dae306e631dec23acd191863e54130c`. DW-644 is committed at entry. This run covers only DW-645: recovering an undispatched removal when activity-release notification faults, preserving fresh admission and potentially delivered identities.

## Runtime baseline

Commands from the owning repository root, before source/test edits:

```bash
aspire ps --format Json
aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --isolated --format Json
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire otel logs --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire wait tenants-ui --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --timeout 45
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj
```

All exit 0. The initial snapshot had 13 Waiting, nine Running/Healthy and one Running/Unhealthy during startup. UI readiness succeeds in 18 seconds; final snapshot has all 23 resources Running/Healthy. The isolated Tenants host was stopped before builds; the existing FrontComposer host was preserved. Raw output is stored in `/tmp/dw-645-aspire-*`; dashboard credentials are omitted. This is startup/readiness evidence, not a browser execution of the cleanup fault.

## Failing regression verification

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-class '*RemoveTenantMemberFlowTests' --minimum-expected-tests 130 --no-ansi --no-progress --results-directory /tmp/dw-645-red-focused --report-xunit-trx --report-xunit-trx-filename focused.trx
```

Build exits 0, zero warnings/errors, in 34.33 seconds (`/tmp/dw-645-red-build.log`). Against unchanged runtime source, focused execution exits 2: 130 total, 121 passed, nine failed, zero skipped, in 5.141 seconds (`/tmp/dw-645-red-focused.log`, `/tmp/dw-645-red-focused/focused.trx`). Every new case fails; every existing removal case passes. Failures expose stranded lifecycle, release exceptions and tenant replacement. The pending-release fixture also had a synchronization flaw corrected below; its original timeout alone does not prove the runtime defect. No fixture compile failure occurred.

## Implementation and verification in progress

The repair retains the attempt and submission barrier through release completion/fault. Only this positively pre-delivery safety-refusal branch catches callback exceptions. Matching live generation and attempt identity guard abandonment and activity reset; a later submit must reacquire admission. Existing `RefuseBeforeDispatch`/`RefuseRetry` helpers preserve preview or possibly delivered identity as appropriate. No shared tracker/snapshot, public API, dependency or backend behavior changed.

No commit or push is authorized by the repository user instructions.

## Post-fix focused verification

The first post-fix source build passes with zero warnings/errors in 25.95 seconds (`/tmp/dw-645-green-build.log`). Its focused run exits 2: 130 total, 128 passed, two failed, zero skipped, in 5.889 seconds (`/tmp/dw-645-green-focused.log`, `/tmp/dw-645-green-focused/focused.trx`). The pending-release fixture used a render-triggered counter wait even though the release callback suspends completion/rendering; it now awaits an explicit callback-entry signal. The replacement fixture asserted pending ownership after the default gateway had already returned terminal Failed; it now holds delivery through ownership assertions. No runtime change accompanied those fixture corrections.

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-class '*RemoveTenantMemberFlowTests' --minimum-expected-tests 130 --no-ansi --no-progress --results-directory /tmp/dw-645-rechecked-focused --report-xunit-trx --report-xunit-trx-filename focused.trx
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 6035 --no-ansi --no-progress --results-directory /tmp/dw-645-full --report-xunit-trx --report-xunit-trx-filename full.trx
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-645-predispatch-release-recovery.md
```

The rechecked build exits 0, zero warnings/errors, in 20.06 seconds (`/tmp/dw-645-green-recheck-build.log`). Focused execution passes 130/130, zero failures/skips, exit 0 in 3.183 seconds (`/tmp/dw-645-rechecked-focused.log`, `/tmp/dw-645-rechecked-focused/focused.trx`). This includes nine added cases. Story-gitlink validation passes with no pointer movement. Full UI execution and independent blind review are running; their results and final preservation checks remain pending until recorded below.

## Full-suite failure and baseline control

The first full run exits 2: 6,035 total, 6,031 passed, four failed, zero skipped, in 2m00.786s (`/tmp/dw-645-full.log`, `/tmp/dw-645-full/full.trx`). All failures are in unchanged `GlobalAdministratorsPageTests`: the three `action=replace` cases of `CancelOrTargetReplacementDuringInitialGrantPreviewStagesCancelsAndLeavesNoStaleAttempt` time out, and `TargetReplacementInvalidatesOpenUndispatchedPreviewAndReleasesItsExactLease` observes Previewed instead of UnableToVerify.

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-method '*GlobalAdministratorsPageTests.CancelOrTargetReplacementDuringInitialGrantPreviewStagesCancelsAndLeavesNoStaleAttempt' --filter-method '*GlobalAdministratorsPageTests.TargetReplacementInvalidatesOpenUndispatchedPreviewAndReleasesItsExactLease' --minimum-expected-tests 7 --no-ansi --no-progress --results-directory /tmp/dw-645-global-focused --report-xunit-trx --report-xunit-trx-filename focused.trx
```

This repeats the same four failures: 7 total, 3 passed, 4 failed, zero skipped, exit 2 in 17.619 seconds (`/tmp/dw-645-global-focused.log`). A control build temporarily uses the entry revision's removal source and restores the authored bytes unconditionally:

```bash
python3 - <<'PY'
from pathlib import Path
import subprocess
p=Path('src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor')
authored=p.read_bytes()
Path('/tmp/dw-645-reviewed-removal.razor').write_bytes(authored)
original=subprocess.check_output(['git','show','e667d5391dae306e631dec23acd191863e54130c:src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor']).replace(b'\r\n',b'\n').replace(b'\n',b'\r\n')
try:
 p.write_bytes(original)
 with Path('/tmp/dw-645-global-baseline-build.log').open('wb') as log:
  r=subprocess.run(['dotnet','build','tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj','-c','Debug','-p:UseHexalithProjectReferences=true','-warnaserror','-m:1','-nr:false'],stdout=log,stderr=subprocess.STDOUT)
finally:
 p.write_bytes(authored)
print('Baseline control build exit:',r.returncode,'; authored component restored.')
raise SystemExit(r.returncode)
PY
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-method '*GlobalAdministratorsPageTests.CancelOrTargetReplacementDuringInitialGrantPreviewStagesCancelsAndLeavesNoStaleAttempt' --filter-method '*GlobalAdministratorsPageTests.TargetReplacementInvalidatesOpenUndispatchedPreviewAndReleasesItsExactLease' --minimum-expected-tests 7 --no-ansi --no-progress --results-directory /tmp/dw-645-global-baseline --report-xunit-trx --report-xunit-trx-filename focused.trx
```

Control build passes with zero warnings/errors in 23.94 seconds; the control execution reproduces exactly the same 3 passes/4 failures/zero skips, exit 2 in 17.144 seconds (`/tmp/dw-645-global-baseline.log`, `/tmp/dw-645-global-baseline/focused.trx`). Thus the failures predate this runtime patch. No user work was reverted.

Both fixture sites send a DOM change to an explicitly disabled Fluent input. They now invoke the existing component ValueChanged EventCallback through the renderer, modeling a queued event from before disabling. Initial enabled input changes, disabled assertions, cancellation counts, invalidation state and exact lease assertions remain. Only this prerequisite test file expands the footprint; no global-administrator production code or dependency changes were needed.

## Independent review and correction recheck

The context-free blind reviewer computed `N = min(floor(sqrt(25.062) + 1), 10) = 6` and returned six findings. Every finding and disposition is recorded in the spec's Review Triage Log. Four coverage improvements are implemented: complete accepted/status/projection confirmation and release after each callback variant, disposal plus same-tenant replacement ownership, four current safety blockers plus held reacquisition invalidation, and successful as well as faulted pending-release/tenant-replacement continuations. Two older source-confirmed defects are separately deferred as DW-646 and DW-647. Their ledger entries retain canonical baseline provenance and explicitly state that cancel/reopen identity loss and wrong EN/FR feedback were not executed or repaired.

The independent read-only correction recheck accepts all four coverage corrections, both precise deferrals, and the prerequisite global-administrator fixture adaptation, with no remaining concrete correction. No reviewer ran builds or tests. Final reviewed build/tests and preservation checks are pending until recorded below.

## Reviewed validation

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-class '*RemoveTenantMemberFlowTests' --minimum-expected-tests 137 --no-ansi --no-progress --results-directory /tmp/dw-645-reviewed-focused --report-xunit-trx --report-xunit-trx-filename focused.trx
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-method '*GlobalAdministratorsPageTests.CancelOrTargetReplacementDuringInitialGrantPreviewStagesCancelsAndLeavesNoStaleAttempt' --filter-method '*GlobalAdministratorsPageTests.TargetReplacementInvalidatesOpenUndispatchedPreviewAndReleasesItsExactLease' --minimum-expected-tests 7 --no-ansi --no-progress --results-directory /tmp/dw-645-global-corrected --report-xunit-trx --report-xunit-trx-filename focused.trx
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 6042 --no-ansi --no-progress --results-directory /tmp/dw-645-reviewed-full --report-xunit-trx --report-xunit-trx-filename full.trx
```

Reviewed build passes with zero warnings/errors, exit 0 in 1m20.06s (`/tmp/dw-645-reviewed-build.log`). Focused removal passes 137/137, zero failures/skips, exit 0 in 5.197 seconds (`/tmp/dw-645-reviewed-focused.log`, `/tmp/dw-645-reviewed-focused/focused.trx`), including 16 added cases. Corrected prerequisite cases pass 7/7, zero failures/skips, exit 0 in 3.057 seconds (`/tmp/dw-645-global-corrected.log`, `/tmp/dw-645-global-corrected/focused.trx`). Final full execution is running; completion and preservation checks are recorded below.

Final reviewed full execution passes **6,042/6,042**, zero failures/skips, exit 0 in **1m51.598s** (`/tmp/dw-645-reviewed-full.log`, `/tmp/dw-645-reviewed-full/full.trx`). This includes the shared retained-flow and UI conformance cases. No runtime/test change followed that passing execution. No browser or backend rerun is needed for this component cleanup and fixture change; pre-edit Aspire readiness is recorded separately above.

## Preservation and completion

```bash
python3 /tmp/dw-645-preservation-check.py
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-645-predispatch-release-recovery.md
git diff --check
git status --short --branch
```

The preservation runner uses `/tmp/dw-645-entry-state.json` captured before implementation. It verifies all 644 unrelated original ledger bodies byte-for-byte as normalized text, original archive/sprint hashes, unchanged canonical HEAD and indexed root gitlinks, and exact single index coverage of all 303 remaining open entries. Result: **647 records, 344 done, 303 open, 271 index rows**. Only DW-645 closes; its old status/disposition/verification/evidence/action fields are retained under pre-closure keys. New DW-646 and DW-647 retain independently reviewed older defects and source-only verification limits. Prior evidence, original reconciliation and release holds are unchanged.

Story-gitlink validation passes with no pointer movement. Whitespace validation passes; Git emits only the existing CRLF-working-copy/LF-index notices for the edited Razor/C# files. Final status contains the component, its removal tests, the prerequisite global-administrator test fixture, ledger/index and two new spec/evidence documents. No commit, push, branch, dependency update or submodule content edit was performed.

The original authored removal bytes restored after the baseline control match SHA-256 `a82af63001fb440e3797ee951cf8c05f46c31584d8845c495e52d9c999fe712f`. The frozen Intent block is unchanged from spec creation. Independent review and correction recheck are complete; no remaining concrete correction is open for DW-645.

Final read-only status also observes `main` one commit behind `origin/main`, which advanced during this run. Local HEAD remains the recorded canonical entry; no fetch, merge or rebase was performed. All evidence applies to that local entry plus this uncommitted patch.


## Authorized commit and push preparation

The user subsequently requested commit and push. At this follow-up, local `main` and `origin/main` both point to `bbd95f95d00597fbdc2ba2ba25ddcdea9407040a` after the independent tenant-owner query change. `git fetch --no-recurse-submodules origin` succeeds and divergence is 0/0; the seven DW-645 files remain the reviewed patch. No branch change or rebase is needed.

Pinned commitlint CLI and conventional config both match lockfile version 21.2.3. The exact candidate is `fix(ui): recover undispatched removal after release errors` (one trailing newline). Validation command:

```bash
node_modules/.bin/commitlint --edit /tmp/dw-645-commit-message.txt --verbose
```

Result: exit 0, zero problems/warnings; successful evidence is preserved in `/tmp/dw-645-commitlint.log`.

The CONTRIBUTING.md formatting check was run narrowly without modifying sources:

```bash
dotnet format whitespace tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --no-restore --include tests/Hexalith.Tenants.UI.Tests/Components/RemoveTenantMemberFlowTests.cs tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs --verify-no-changes
```

Result: exit 2 with 91 WHITESPACE diagnostics (`/tmp/dw-645-commit-format.log`). Every diagnostic maps to an unchanged line in current HEAD; zero diagnostic locations are on added or modified lines. The line-to-baseline mapping is preserved in `/tmp/dw-645-commit-format-baseline.json`. Existing formatting debt is preserved. `git diff --check` and the story-gitlink check pass. Runtime/test contents have not changed since the reviewed validation above; no new test execution is claimed for the later independent HEAD change.

The first staged whitespace check (`git diff --cached --check`) detected one extra blank line at the new spec's EOF (exit 2). That line was removed before the final local commit amendment; source/test contents are unchanged.
