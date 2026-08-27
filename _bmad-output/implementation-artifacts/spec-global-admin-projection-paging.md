---
title: 'Load complete global-administrator correction evidence'
type: 'bugfix'
created: '2026-08-27'
status: 'in-progress'
baseline_revision: '6bd5fc29cf66e0238a4ea5ceb8c743c358fe3513'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01-deferred-work-pagination-and-submodule-docs.md'
warnings: []
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
- `src/Hexalith.Tenants.UI/Services/Gateways/GlobalAdministratorsProjectionLoader.cs` -- add one bounded reusable full-page loader with stable-version/current-evidence validation, cursor recovery/cycle protection, aggregation, and complete/incomplete result shaping.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor` -- replace the private duplicate walker with the shared loader without changing supplementary-read cancellation or retained-evidence behavior.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor` -- use complete reads for initial enrichment, correction-open refresh, and confirmation; require complete mutation evidence and rederive a refreshed global-admin intent before opening.
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor` -- use the complete loader when no parent refresh provider exists.
- `src/Hexalith.Tenants.UI/State/TenantAudit/GlobalAdministratorCorrectionSnapshot.cs` -- require `IsCompleteEvidence` plus lifecycle-backed current evidence while preserving explicit `HasMore` fail-closed checks.
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/GlobalAdministratorsProjectionLoaderTests.cs` -- cover aggregation, deduplication, cursor/version/lifecycle/recovery/cap/cancellation matrix.
- `tests/Hexalith.Tenants.UI.Tests/Components/{TenantDetailSurfaceTests,GlobalAdministratorCorrectionPanelTests,TenantAuditPageTests}.cs` -- preserve extracted behavior and add multi-page preview, restore, revoke, and refreshed-intent tests.
- `tests/Hexalith.Tenants.UI.Tests/State/GlobalAdministratorCorrectionSnapshotTests.cs` -- align complete fixtures with lifecycle/version/completeness and retain incomplete-page regression assertions.

**Acceptance Criteria:**
- Given the revoke target exists only on page two, when correction eligibility is evaluated, then both pages are loaded, the full distinct count is used, and revoke is previewable unless the true aggregate has one administrator.
- Given a restore or revoke command reaches projection-pending, when the intended state is visible only after walking every page, then confirmation occurs only from the complete stable aggregate.
- Given any page is incomplete, recovered, stale, non-current, version-inconsistent, cyclic, missing its continuation, or beyond the cap, when eligibility or confirmation runs, then it remains unavailable or pending and no command success is asserted.
- Given a correction intent was formed from incomplete evidence, when correction-open refresh obtains complete evidence, then the intent is re-evaluated from that evidence rather than remaining permanently unavailable.

## Spec Change Log

## Review Triage Log

## Design Notes

The helper belongs above the one-page gateway: the server already supplies protected requester-scoped cursors, and `TenantQueryGateway` deliberately maps one page. A complete result is created only after every page passes invariant checks; `HasMore=false` alone is insufficient because an aborted terminal page may be mixed-version. `PagingRecovered=true` on a continuation is also incomplete because the gateway silently restarted at page one.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` -- expected: zero warnings and errors.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` -- expected: all UI tests pass.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-global-admin-projection-paging.md` -- expected: no undeclared gitlink movement.
