---
title: 'Load complete global-administrator correction evidence'
type: 'bugfix'
created: '2026-08-27'
status: 'in-review'
baseline_commit: '23b2a7691ae667fd160a48a5d7293f3cf38d48fe'
baseline_revision: '6bd5fc29cf66e0238a4ea5ceb8c743c358fe3513'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01-deferred-work-pagination-and-submodule-docs.md'
warnings:
  - 'Gitlink verification blocked: preserved baseline_commit is not an ancestor of the concurrently rewritten HEAD.'
  - 'Live runtime command verification limited by existing EventStore workload issuer configuration.'
deferred: []
---

<intent-contract>

## Intent

**Problem:** Global-administrator correction eligibility and confirmation inspect only the first 20-row projection page, so a target on a later page cannot be restored or revoked even though the existing `HasMore` checks prevent false success.

**Approach:** Extract the existing bounded full-projection walk into a reusable UI helper, route correction preview and confirmation through its aggregated evidence, and require complete lifecycle-backed evidence before platform-authority state is inferred.

## Boundaries & Constraints

**Always:** Forward opaque cursors verbatim, clear page-scoped ETags after page one, preserve cancellation, ordinal-deduplicate user IDs, require current lifecycle/freshness and one stable nonblank projection version across all pages, reject cursor recovery/cycles/missing cursors/page-cap exhaustion, and retain the existing `HasMore` fail-closed behavior.

**Block If:** The existing query surface cannot distinguish a complete stable walk from invalid/recovered/mixed-version paging without changing the public server contract.

**Never:** Edit the deferred-work ledger; decode, expose, or log cursors; add offset paging; make the walk unbounded; weaken last-administrator, freshness, authorization, or projection-confirmation gates; change backend query contracts or unrelated global-administrator list paging.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Complete multi-page read | Current pages share lifecycle/version and terminate with `HasMore=false` | Rows aggregate once per ordinal user ID; result has no next cursor and is complete evidence | No error expected |
| Later-page target | Correction target exists only beyond page one | Full count/presence drives preview; restore/revoke confirmation waits for terminal evidence | Never infer from page one |
| Invalid continuation | Blank/repeated cursor, gateway page-one recovery, or page cap | Aggregate remains incomplete and cannot enable or confirm correction | Fail closed without cursor disclosure |
| Inconsistent evidence | Stale/degraded page, missing/changing version, non-current lifecycle | Mixed evidence is not accepted as platform-authority truth | Fail closed as incomplete evidence |
| Cancellation | Caller cancels during the walk | Stop before another page and propagate cancellation | Do not retain a partial result as complete |

</intent-contract>

## Code Map

- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:1637` -- existing hardened 50-page walker and incomplete-result shaping to extract, then continue consuming through the helper.
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantQueryGateway.cs:115` -- one-page query seam; keep its public contract unchanged and compose pages above it.
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs:750` -- maps one protected-cursor page and flags invalid-cursor recovery with `PagingRecovered`; read-only evidence.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:893` -- refresh-before-open, eligibility construction, initial enrichment, and confirm provider currently consume one page.
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor:364` -- confirmation refresh fallback currently consumes one page.
- `src/Hexalith.Tenants.UI/State/TenantAudit/GlobalAdministratorCorrectionSnapshot.cs:82` -- presence/count/confirmation gates; incomplete `HasMore` regression guards already live here.
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs:221` -- existing full-walk, cursor, version, and cap coverage to preserve while extracting.
- `tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorCorrectionPanelTests.cs:56` -- restore/revoke submission and confirmation flows to exercise multi-page evidence.
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs:412` -- correction-opening path and query stub for later-page eligibility coverage.
- `tests/Hexalith.Tenants.UI.Tests/State/GlobalAdministratorCorrectionSnapshotTests.cs:85` -- existing incomplete-page restore/revoke fail-closed tests that must remain green.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.Tenants.UI/Services/Gateways/GlobalAdministratorsProjectionLoader.cs` -- add one bounded reusable full-page loader with stable-version/current-evidence validation, cursor recovery/cycle protection, aggregation, and complete/incomplete result shaping.
- [x] `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor` -- replace the private duplicate walker with the shared loader without changing supplementary-read cancellation or retained-evidence behavior.
- [x] `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor` -- use complete reads for initial enrichment, correction-open refresh, and confirmation; require complete mutation evidence and rederive a refreshed global-admin intent before opening.
- [x] `src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor` -- use the complete loader when no parent refresh provider exists.
- [x] `src/Hexalith.Tenants.UI/State/TenantAudit/GlobalAdministratorCorrectionSnapshot.cs` -- require `IsCompleteEvidence` plus lifecycle-backed current evidence while preserving explicit `HasMore` fail-closed checks.
- [x] `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/GlobalAdministratorsProjectionLoaderTests.cs` -- cover aggregation, deduplication, cursor/version/lifecycle/recovery/cap/cancellation matrix.
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/{TenantDetailSurfaceTests,GlobalAdministratorCorrectionPanelTests,TenantAuditPageTests}.cs` -- preserve extracted behavior and add multi-page preview, restore, revoke, and refreshed-intent tests.
- [x] `tests/Hexalith.Tenants.UI.Tests/State/GlobalAdministratorCorrectionSnapshotTests.cs` -- align complete fixtures with lifecycle/version/completeness and retain incomplete-page regression assertions.

**Acceptance Criteria:**
- Given the revoke target exists only on page two, when correction eligibility is evaluated, then both pages are loaded, the full distinct count is used, and revoke is previewable unless the true aggregate has one administrator.
- Given a restore or revoke command reaches projection-pending, when the intended state is visible only after walking every page, then confirmation occurs only from the complete stable aggregate.
- Given any page is incomplete, recovered, stale, non-current, version-inconsistent, cyclic, missing its continuation, or beyond the cap, when eligibility or confirmation runs, then it remains unavailable or pending and no command success is asserted.
- Given a correction intent was formed from incomplete evidence, when correction-open refresh obtains complete evidence, then the intent is re-evaluated from that evidence rather than remaining permanently unavailable.

## Spec Change Log

## Review Triage Log

### 2026-10-08 — Blocking scope correction from Story 5.6, pass 17

Story 5.6's pass-17 D1 decision selected option (a): restore the tenant audit page's read-only global-administrator gate. The paging implementation's re-enablement of `GlobalAdministratorCorrectionPanel` in root `5a3bc6dd4c1aa77cec50d0a960e986aa89c08cbe` exceeded this spec's complete-evidence loading intent and violated the frozen Story 5.5/5.6 boundary. The pass-17 remediation removed that audit-page branch; the pass-19 cleanup also removes its complete-evidence initial enrichment and restores read-only grid and receipt copy. Audit-page enrichment waits until Story 5.7 enables global-administrator correction. The standalone panel, snapshot, complete-evidence loader and their independent tests remain available for Story 5.7, which still owns enabling global-administrator correction and deterministic linked proof. This spec remains `in-review`; its earlier passing page-submission evidence is historical and cannot authorize restoring that branch. Any future re-enablement must resolve the Story 5.7 acceptance requirements first.

### 2026-10-08 — Independent review

- Blind B1 — `medium`, `patch`: stale/degraded audit reads fail the current-surface gate, and the retained-display predicate only recognizes tenant attempts. Include the mounted submitted global snapshot in retained display so status and recovery remain available.
- Blind B2 — `medium`, `patch`: the global refresh provider returns evidence without updating the parent capture; a later render can resupply the older projection. Apply both projection and authorization under existing renderer/generation guards.
- Blind B3 — `medium`, `patch`: the global proof link targets a fragment absent from the audit grid. Use the existing system-audit receipt route for corrective evidence. The global component does not expose an original-receipt callback or promise such a control; no new public callback is required.
- Blind B4 — `medium`, `patch`: global opening passes no cancellation token, so navigation/disposal/superseding opens can leave a bounded but unnecessary walk running. Connect this pre-submit read to cancellation without changing dispatched-command ownership.
- Blind B5 — `low`, `patch`: the direct-callback test proves rederivation, but does not exercise the rendered recovery route. The audit refresh already reloads complete evidence; add coverage for refresh followed by the available launcher.
- Blind B6 — `medium`, `patch`: audit tests stop at previews and the tracked-dispatch stub cannot exercise submission. Add controllable tracked responses and restore/revoke confirmation tests through the real parent provider, including incomplete post-command evidence.
- Blind B7 — `low`, `patch`: the newly reachable global panel has sibling titled preview/lifecycle sections outside an accordion. Group those sections using existing Fluent components and include the panel in conformance coverage.
- Edge E1 — `medium`, `patch`: the global opening branch invalidates old focus without scheduling preview-heading focus, and the heading is not programmatically focusable. Reuse the tenant handoff focus convention and add a focus assertion.
- Verification V1 — `medium`, `patch`: replacing the parent provider with a null result would leave existing preview/fallback tests green while preventing audit-page confirmation. This corroborates B6 and is patched by the same submission coverage.
- Verification O1 — `medium`, `patch`: submitted global attempts can remain pending in the admission gate while the stale audit display hides their recovery controls. This corroborates B1 and is patched by the same retained-display correction.

All three review layers ran with fresh context. Duplicate findings are recorded individually above and grouped by their shared defect for patching. No finding was deferred; the deferred-work ledger is unchanged.


### 2026-10-08 — Patch verification

All eight grouped patch items are resolved. The focused build had zero warnings/errors and all 356 tests across `TenantAuditPageTests`, `GlobalAdministratorCorrectionPanelTests`, and `DomainUiFluentConformanceTests` passed. The final full-suite run below verifies the patched tree. The outside-page proof receipt uses only the panel's exact verified corrective row, with current circuit authority plus the parent's authorized system-audit scope. No public callback or backend contract was added.

## Design Notes

The helper belongs above the one-page gateway: the server already supplies protected requester-scoped cursors, and `TenantQueryGateway` deliberately maps one page. A complete result is created only after every page passes invariant checks; `HasMore=false` alone is insufficient because an aborted terminal page may be mixed-version. `PagingRecovered=true` on a continuation is also incomplete because the gateway silently restarted at page one.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` -- expected: zero warnings and errors.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` -- expected: all UI tests pass.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-global-admin-projection-paging.md` -- expected: no undeclared gitlink movement.


### 2026-10-08 — Final verification evidence

- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0`: exit 0; 0 warnings, 0 errors. Output retained at `/tmp/global-admin-paging-final-build.log`.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none`: exit 0; 3,986 total, 0 errors, 0 failed, 0 skipped, 0 not run, 55.896 seconds. Output retained at `/tmp/global-admin-paging-final-tests.log`.
- The full run includes aggregation/deduplication, opaque cursor forwarding and ETag clearing, version/lifecycle/freshness/recovery/cycle/cap failures, cancellation, detail retained evidence, standalone restore/revoke confirmation, and audit-page submission through the real complete-read provider. Additional review regressions cover current row lifecycle, rendered refresh recovery, stale/degraded pending-command recovery, pre-submit cancellation with independent dispatched-command ownership, focus, accordion grouping, and proof receipt navigation outside the displayed page.
- Scoped `git diff --check -- <paging bundle paths>`: exit 0, no whitespace errors; Git emitted only CRLF-to-LF normalization notices.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-global-admin-projection-paging.md`: exit 1; `RESULT: FAIL`; `baseline_commit 23b2a76 is not an ancestor of HEAD (695e965). The recorded baseline does not describe this working tree.` HEAD at this check was `695e9658b92aac7e500e554acc3366a0c5f877c7`. Concurrent work rewrote history after this run captured its baseline. The original canonical baseline is preserved, and this bundle contains no gitlink changes. This validation gate remains blocked; status stays `in-review` under the rendered review step's halt rule.
- `aspire run --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj` started the baseline; `aspire describe --format Json` reported the tenant UI/API running and EventStore finished. `aspire logs eventstore --tail 45` identified `OptionsValidationException`: `Authentication:WorkloadIssuer:ClientId` and `Authentication:WorkloadIssuer:ClientSecret` are missing while `Authentication:JwtBearer:Authority` is configured. This existing runtime configuration limits live command validation; no hosting/configuration changes belong to this implementation. `aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj`: exit 0, successfully stopped the AppHost started by this run.
- Full baseline-to-worktree and scoped review diffs are retained as temporary artifacts. Review and patch verification isolate this paging bundle from unrelated concurrent dependency, submodule, documentation, and test edits. All unrelated edits and the deferred-work ledger were preserved; this run performed no staging, commits, branching, submodule updates, or remote operations.
