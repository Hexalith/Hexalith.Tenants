# DW-187 validation evidence — 2026-10-21

Entry revision: `7b33d851d5637dfaf6f310f5cfc2f57479317d9b`, clean main after the user's archiving push. User explicitly authorizes completing the next deferred item and pushing on main. Only DW-187 is selected. Earlier open items retain their owning-module, privacy, reversible-route and topology dependencies.

## Scope and contract

A change to the value pair `(TenantStatus, ProjectionLifecycleState)` invalidates undispatched metadata and set-configuration drafts and removal confirmation. Metadata/set editors close, discard draft values and use existing launcher/lifecycle focus recovery. A return to Current/Active requires a deliberate reopen or fresh destructive confirmation. Same-lifecycle replacement Detail/Context objects preserve drafts. Existing per-flow generation checks invalidate suspended unsent admission/preview continuations; sent, retained and accepted attempts keep identity, tracking and late-delivery processing. No API, dependency, persistent state, topology or localization contract changes.

This is component/state acceptance evidence. It does not establish distributed functional, authenticated browser or release qualification. Existing release holds remain.

## Aspire baseline

```bash
aspire ps --format Json
aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --isolated --format Json --non-interactive
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire otel logs --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --severity Error --limit 5 --format Json
aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --non-interactive
```

Initial ps is empty; start, describe, telemetry and stop exit 0. All 23 resources report Running/Healthy before code changes. The bounded telemetry sample contains four Memories errors, including registry initialization failures. Healthy resources therefore do not establish successful initialization or functional acceptance. Local raw output is under `/tmp/dw187-aspire-*`; credentials and telemetry internals are excluded from tracked evidence. Only the app started by this task was stopped.

## Baseline, reproduction and initial fixed-code verification

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 -v:q
```

Baseline, initial-test, corrected-test and fixed-code builds exit 0 with zero warnings/errors (`/tmp/dw187-build-baseline.log`, `dw187-build-red.log`, `dw187-build-red-corrected.log`, `dw187-build-green.log`). The first reproduction reports 11 failures/8 passes: ten intended obsolete-editor/confirmation assertions and one incorrect test assertion that read TextContent rather than the Fluent textarea's value attribute. Correcting that fixture gives ten intended failures/nine passes against unchanged production, zero skips.

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --no-ansi --no-progress --report-xunit-trx --report-xunit-trx-filename results.trx --results-directory /tmp/dw187/red-corrected --filter-method '*LifecycleChanges*' --filter-method '*LifecycleRoundTrip*' --minimum-expected-tests 19
```

Entry production exits 2. The same command with results-directory `/tmp/dw187/focused` against fixed production exits 0: 19 passed, zero failed/skipped. This covers both lifecycle dimensions, unchanged-lifecycle replacement controls, held metadata authority/set preview, actual held gateway delivery, RequestSent message identity, late Accepted delivery and subsequent accepted-state preservation across all three flows. Metadata's existing admission guard already prevents the held authority from dispatching; the new test additionally requires closing its obsolete editor.

## Initial formatting and provenance

```bash
dotnet format whitespace tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --no-restore --verify-no-changes --include tests/Hexalith.Tenants.UI.Tests/Components/EditTenantMetadataFlowTests.cs tests/Hexalith.Tenants.UI.Tests/Components/SetTenantConfigurationFlowTests.cs tests/Hexalith.Tenants.UI.Tests/Components/RemoveTenantConfigurationFlowTests.cs --verbosity quiet
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-187-editor-lifecycle-invalidation.md
```

Scoped formatting exits 0. The initial gitlink check exits 1 because the new spec omitted baseline_commit; recording the canonical entry revision in frontmatter fixes the evidence omission. The repeated guard exits 0 with no references pointer changes. Installed commitlint CLI and lockfile both specify 21.2.3; exact candidate validation and final ownership checks are recorded below after review.

## Independent review and corrections

Blind Hunter review reports six findings; all are accepted and patched. The spec retains the verdict and evidence for each. Two held provisional-metadata cases cover the lease and second authority lookup before actual delivery. Their first assertions ran before bUnit completed the post-await render; bounded rendered assertions correct that timing. The set lease test exposes a missing busy guard on the closed editor's launcher; both the launcher and queued open handler now refuse while provisional cleanup runs. The next run passes 27/28, with the remaining immediate activity-list assertion racing asynchronous cleanup; a bounded assertion compares a captured array after cleanup. These intermediate failures remain in `/tmp/dw187-test-focused-reviewed*.log` and their TRX files.

The subsequent reviewed lane passes 28/28, zero failures/skips. Additional cases cover terminal set evidence with a held new preview, terminal removal reconfirmation, ambiguous evidence/ownership in all three flows, settled metadata evidence and reset/reopen focus. Full snapshot equality preserves identity, intent, audit evidence and baseline, with tracker ownership checked independently. The real sent/Accepted cases continue to preserve late delivery and tracking.

Correction recheck accepts the six original fixes, then identifies a liveness regression: waiting for obsolete preview work would prevent reopening indefinitely if its provider never returned. A local marker now distinguishes preview-only work from lease acquisition. Preview-only work invalidates and permits immediate reopen; once lease acquisition starts, busy admission remains until cleanup. Existing held-preview cases now reopen and enter a fresh draft before releasing the old result, then prove that late completion cannot replace the fresh preview. The held-lease case still forbids reopen until cleanup. The liveness lane passes 28/28 with zero failures/skips (`/tmp/dw187/focused-liveness/results.trx`). Independent final read-only recheck accepts this correction with no remaining issue. No new work is deferred.

Final Debug source build exits 0 with zero warnings/errors (`/tmp/dw187-build-final.log`), including the local marker reset on tenant replacement and removal of a new nullable suppression. Scoped formatting is checked again on the final test sources. Final full-project validation follows.

## Commit validation

```bash
node_modules/.bin/commitlint --edit /tmp/dw187-commit.txt --verbose
```

Exit 0 with zero problems/warnings (`/tmp/dw187-commitlint.log`). Installed and locked CLI version: 21.2.3. The exact validated input is `fix(ui): invalidate unsent editors on lifecycle changes` followed by a newline. Remote fetch succeeds with submodule recursion disabled; main and origin/main are initially aligned (0/0 divergence). Final divergence/ownership checks and push occur after qualification.

## Final qualification and closure

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --no-ansi --no-progress --report-xunit-trx --report-xunit-trx-filename results.trx --results-directory /tmp/dw187/full-final --minimum-expected-tests 6749
```

Exit 0: 6749 passed, zero failed/skipped/errors, confirmed from TRX counters (`/tmp/dw187-test-full-final.log`, `/tmp/dw187/full-final/results.trx`). This includes all 28 dedicated lifecycle cases on the final sources, the lease-versus-preview liveness correction, tracker retention, existing consumers and governance tests. Final scoped formatting exits 0 (`/tmp/dw187-format-final.log`). Independent review and correction rechecks have no unresolved finding.

Only DW-187's record changes. Its original status/disposition/verification/action fields are retained with `pre-dw-187-` prefixes. Reconciliation retains all 656 IDs: 404 done/252 open across 227 maintenance rows; every open ID is covered exactly once. A local structural comparison verifies every other record is unchanged (`/tmp/dw187-ledger-validation.json`). The archive, sprint, dependency pointers and existing release holds remain preserved. The final own-spec gitlink check and working-tree whitespace check exit 0. The completed spec retains its frozen intent; no new work is deferred.

The commit includes the three flow components, three existing test files, the spec, this evidence file and the ledger/maintenance closure: ten task-owned paths. Final staging, exact-message commitlint and remote divergence checks run before commit/push; no release or deployment is requested.

Final staged-diff inspection removed an unrelated assertion-wait change from an existing metadata test. All existing test bodies are preserved; test changes are additions only. The subsequent Debug source rebuild exits 0 with zero warnings/errors (`/tmp/dw187-build-commit.log`), scoped formatting exits 0 (`/tmp/dw187-format-commit.log`), and the full maintained UI command above, using results-directory `/tmp/dw187/full-commit`, passes 6749/6749 with zero failures/skips/errors (`/tmp/dw187-test-full-commit.log`). The repeated own-spec gitlink guard and staged whitespace check pass.
