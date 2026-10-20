# DW-178 validation evidence — 2026-10-20

Entry revision: `54bfac8da66b60aa8aa19a77d23676a7662113b4`, clean main. The user requested the next deferred work and authorized commit/push on main. DW-178 is the next dependency-free actionable Tenants record after DW-177; lower open records retain their existing ownership/dependencies/dispositions.

## Decision and scope

The ledger offers two resolutions: short-circuit unchanged authority scope, or document why every parameter update requires a fresh resolve. This change takes the latter. `OnParametersSetAsync` reloads projection evidence even when only the return context changes. Tenant ID and authentication epoch alone do not prove that the composition's current authority answer is unchanged. Preserve the existing fresh read, its pending Indeterminate state, replaceable cancellation/generation protection, and independent submit-time reauthorization. A source comment records that relationship. No runtime behavior, public API, localization, topology or dependency change is introduced by this work.

Six new rendered component cases exercise repeated same-tenant parameters and actual query-string navigation, granted/revoked/indeterminate completion, pending fail-closed behavior, follow-up recovery and existing superseded-resolution coverage. They reuse the existing fixture and assert the child authority parameter plus rendered Fluent action availability. The pre-existing authentication/supersession and command-time reauthorization regressions remain part of the full suite. This is component evidence; no new browser, assistive-technology, deployed-authorization or revocation guarantee is claimed.

## Commands and initial results

Run from the owning repository root. Logs are under `/tmp/dw178/` except the initial baseline/Aspire logs, which use `/tmp/dw178-` prefixes.

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 -v:q
```

The initial build (`/tmp/dw178-build-baseline.log`) exits 1, zero warnings/one error: `TenantReadAuthorityResult.cs(12,5): CS0246 NativeSourceProof could not be found`. This is the existing unrelated source dependency mismatch from `75627d267296d26a9f4fee05fa0495deb110fc11`. During investigation another actor advanced root dependency checkouts. The next three ordinary source builds (`build-tests.log`, `build-focused.log`, `build-corrected.log`) and the qualified-test build (`build-qualified.log`) each exit 0 with zero warnings/errors. These are source-checkout results, not qualification of the originally declared dependency pins. This work does not stage or commit those pointer changes.

All test commands use this maintained project-runner prefix:

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --no-ansi --no-progress --report-xunit-trx --report-xunit-trx-filename results.trx
```

Append `--results-directory /tmp/dw178/<directory>` and the filters below. Read counts from the resulting TRX, rather than relying only on exit status.

| Directory | Extra arguments | Result |
| --- | --- | --- |
| `focused` | `--filter-method '*SameTenantParameterLoad*' --minimum-expected-tests 7` | Exit 2: one passed/six failed/zero skipped. bUnit refuses direct assignment of SupplyParameterFromQuery; use actual NavigationManager query navigation. |
| `focused-corrected` | Same filter/minimum | Exit 2: one passed/six failed/zero skipped. The ready fixture lacked the lifecycle projection version required for actionable state. Supply the existing `tenant-sequence:41` marker. |
| `focused-qualified` | Same filter/minimum | Exit 0: seven passed/zero failed/skipped. |

## Aspire baseline

```bash
aspire ps --format Json
aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --isolated --format Json --non-interactive
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire otel logs --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --severity Error --limit 5 --format Json
aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --non-interactive
```

Initial ps is empty. Start succeeds; initial describe includes Waiting resources and unhealthy security. The final describe reports all 23 resources Running/Healthy. Inspect the bounded error sample locally; do not copy login credentials or tokens into tracked evidence. The started app stops successfully. This establishes the resource baseline, without claiming distributed functional qualification.

## Git ownership

The initial required default own-spec gitlink check exits 1 because another actor advanced dependency checkouts after entry. Its exact output is retained in `/tmp/dw178/gitlinks-initial.log`. Do not restore, stage or attribute those changes to DW-178. The committed-range check and staged-path inspection must separately establish that the DW-178 commit does not move dependency pointers. Existing release qualification holds remain in force.

## Final qualification

The first maintained six-case qualification passes: `build-six-final.log` exits 0 with zero warnings/errors; `focused-six-final` has 6/6 passed and `full-six-final` has 6618/6618 passed, zero failed/skipped. Scoped formatting (`format.log`) exits 0. Independent review requests two exact-authority assertions; final reviewed execution and reconciliation are recorded below after completion.

The experimental seventh same-tenant late-result test failed to reject removal of the generation guard (control exit 0). A direct-substitute experiment then produced an unrelated null composition fault; after repairing that fixture, the guard-removal control again passed. Remove the unproven test and experimental fixture hook rather than claiming qualification. The existing supersession regression is preserved. The cache-control replay rejected all six retained cases (plus the now-removed seventh); this is diagnostic evidence, not a final maintained seven-case result. Final qualification below uses exactly the six retained tests.

Concurrent external commit `a83021d9efa259c1afa014078d2c5fcf61c4d42c` subsequently recorded all six checkout moves (including Platform) and captured the in-progress DW-178 files. Preserve the commit. The final source validation therefore uses the now-committed updated dependency pins; the initial missing-type failure remains historical evidence. The spec File List and full target table declare all six inherited moves from the true story-entry baseline. The follow-up completion commit must contain only the owned remaining files and no gitlink changes.

## Reproducing the diagnostic controls

These controls were temporary edits to `TenantDetailPage.razor`; the original bytes were restored in `finally` after every attempt. They are diagnostics of earlier test versions, not the maintained acceptance command. No mutation remains in the final source.

For the cache control, replace the single call in `OnParametersSetAsync`:

```csharp
StartLifecycleAuthorizationResolution(tenantId);
```

with:

```csharp
if (_lifecycleAuthorizationGeneration == 0 || tenantRouteChanged)
    StartLifecycleAuthorizationResolution(tenantId);
```

Build using the ordinary Debug source command above, then run the test prefix with:

```bash
--results-directory /tmp/dw178/control-same-tenant-cache --filter-method '*SameTenantParameterLoad*' --minimum-expected-tests 7
```

The historical seven-case version builds with zero warnings/errors and exits 2 with seven failed/zero passed/skipped (all six retained cases time out waiting for fresh same-tenant authority; the removed extra case also fails). The first, stricter prototype used only `if (tenantRouteChanged)` and also failed seven cases. Neither historical run is presented as the final six-case gate.

For the ineffective generation control, delete exactly this clause from `ResolveLifecycleAuthorizationAsync`'s renderer application guard:

```csharp
|| generation != Volatile.Read(ref _lifecycleAuthorizationGeneration)
```

Build with the same ordinary Debug source command, then use the test prefix with:

```bash
--results-directory /tmp/dw178/control-stale-generation --filter-method '*SameTenantParameterLoadDiscardsSupersededLifecycleAuthority' --minimum-expected-tests 1
```

The initial and repaired direct-result experiments each exit 0 (one passed), so they do not establish guard-removal sensitivity. The intermediate direct-substitute fixture caused a null high-impact composition fault, and a missing namespace also caused a compile failure; neither is valid mutation evidence. The extra test and all experimental fixture additions are removed. Retain existing supersession coverage without claiming new deterministic same-tenant overlap qualification.

## Maintained reviewed commands

The accepted six-case lane uses the test prefix above with:

```bash
--results-directory /tmp/dw178/focused-reviewed --filter-method '*SameTenantParameterLoad*' --minimum-expected-tests 6
```

The full project uses the same prefix with:

```bash
--results-directory /tmp/dw178/full-reviewed --minimum-expected-tests 6618
```

Formatting and ownership checks:

```bash
dotnet format whitespace tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --no-restore --verify-no-changes --include tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.AuthorizationRefresh.cs src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor -v:q
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-178-detail-authorization-refresh.md
node node_modules/@commitlint/cli/cli.js --edit /tmp/dw178/commit-message.txt --verbose
```

The preliminary committed-entry-only gitlink check passes with `--ref 54bfac8da66b60aa8aa19a77d23676a7662113b4`. The initial default check fails on concurrent checkout moves; the first declaration attempt also fails because explanatory prose is not an explicit path-only declaration. After using path-only File List entries and full targets in Completion Notes List, the default current-range check passes (`gitlinks-final.log`). No pointer restoration or update was performed. Commitlint 21.2.3 validates the exact full temporary-file message with zero problems/warnings (`commitlint.log`).

The independent blind reviewer returns five findings. Two strengthen exact child authority assertions, two require maintained evidence/reproduction details, and one questions whether the frozen wording demands a new overlap guarantee. The first four are corrected. The wording finding is rejected as unnecessary expansion beyond the selected ledger recommendation: existing supersession protection remains covered, and this document explicitly excludes new deterministic same-tenant overlap proof. Every finding and rationale is retained in the spec triage log.

## Accepted final result and reconciliation

`build-reviewed.log` exits 0 with zero warnings/errors. `focused-reviewed/results.trx` has six executed/passed, and `full-reviewed/results.trx` has 6618 executed/passed; both have zero failures/errors/skips. Reviewed formatting (`format-reviewed.log`) exits 0. The reviewer confirms the exact-authority corrections, maintained commands and historical control details, accepts the explicit supersession limitation and reports no additional concrete issue.

A second external sync commit `3deb003d24a303c751f21dfe7326a0751d44ea69` captured intermediate test/evidence corrections without further gitlink moves. Preserve both concurrent commits. Final testing uses the dependency pointers already committed by that external work; no current missing-type blocker remains in this source lane. This does not establish broad package-mode or release readiness.

Closure is gated directly on the final reviewed TRX counters. Exact ledger reconciliation closes only DW-178, preserves every original field under pre-dw-178 names, keeps all 656 identifiers and every unrelated record body, and preserves archive/sprint bytes. Counts are 401 done/255 open, with 230 index rows covering every open ID exactly once. `/tmp/dw178/ledger-validation.json` retains those assertions and the exact counters. The completion commit contains only the remaining five owned files and no dependency pointer update.

Final source SHA-256: `27d73e1b6ccad5dccaa5ac12b6e944c9e73b4605999109da30b06a25d6d05362` — `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`.

Final source SHA-256: `1178462bb476989071682e627538c25141d6e58f984cad2575405918f171cc3c` — `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.AuthorizationRefresh.cs`.
