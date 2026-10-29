---
title: 'Load complete global-administrator correction evidence'
type: 'bugfix'
created: '2026-08-27'
status: 'done'
baseline_commit: '6bd5fc29cf66e0238a4ea5ceb8c743c358fe3513'
rejected_baseline_commit: '23b2a7691ae667fd160a48a5d7293f3cf38d48fe'
implementation_creation_ref: '7716f0b11423eb54d74935b6cc6e3edf405dc400'
implementation_endpoint: 'ded413dfd4786983585c7f56a399825cc1afbae3'
gitlink_audit_ref: 'ded413dfd4786983585c7f56a399825cc1afbae3'
gitlink_original_baseline: '23b2a7691ae667fd160a48a5d7293f3cf38d48fe'
gitlink_creation_ref: '7716f0b11423eb54d74935b6cc6e3edf405dc400'
reviewed_head: '1d8c31d2abe809829e10ce1ddadc978c79489f3e'
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

**Always:** Forward opaque cursors verbatim, clear page-scoped ETags after page one, preserve cancellation, use ordinal user identity and preserve later strict duplicate rejection, require current lifecycle/freshness and one stable nonblank projection version, caller scope and authorization revision across all pages, reject cursor recovery/cycles/missing cursors/page-cap exhaustion, and retain the existing `HasMore` fail-closed behavior.

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
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs:750` -- maps one protected-cursor page and flags invalid-cursor recovery with `PagingRecovered`; preserve empty-page cursor/HasMore evidence without changing its public contract.
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

### 2026-10-29 — Preserve current safety policy and repair the original provenance

Resumed at current HEAD under the user's explicit paging-only boundary. Preserve completed Story 5.7 and all unrelated later work. The original baseline is restored from the creation commit's recorded `baseline_revision` and independently verified parent, never moved to HEAD. Later duplicate-rejection policy supersedes historical permissive deduplication wording. Current paging fixes retain unknown display evidence only for an unchanged authorized detail caller, require one authorization revision through the walk, and preserve empty-page paging signals so invalid terminal evidence stays fail-closed. Historical gitlink audit and current worktree movement are separate checks; a failed applicable CI provenance gate must keep this spec in review.

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

### 2026-10-29 — Current-HEAD independent review triage

All three fresh-context review layers completed before classification. Blind Hunter reviewed the scoped current diff (260,187 bytes; floor 10); Edge Case Hunter returned no findings after current-source tracing; Verification Gap Reviewer returned V1. Current integrations were read with the original paging patch for attribution; adjacent later code in the contextual diff was not adopted as paging ownership.

| Finding | Verdict and evidence | Disposition |
| --- | --- | --- |
| B1: returned failure loses retained detail evidence | medium — `LoadGlobalAdministratorsForGenerationAsync` retains only on exceptions, while the real gateway returns `Unavailable` for ordinary failures. The canonical loader response replaces known rows. Existing generation and caller guards invalidate retained data on authentication changes. | patch: retain only old same-caller rows as incomplete/unknown after a returned unavailable read; keep unauthorized redaction. |
| B2: audit enrichment delays primary grid | medium, outside this review — the current load/enrichment branch was introduced by Story 5.7 in `362a957de11ce141f48d73d9ed4301f31a1c870e`; primary audit rendering already awaited supplementary tenant reads before the original paging baseline. This is later page-render scheduling rather than a paging-owned change. | excluded by the user's paging-only scope; preserve later work. |
| B3: obsolete correction reads lack cancellation | low — the shared loader correctly observes supplied cancellation. The audit-open use of `None` is later Story 5.7 operation ownership; the standalone fallback also serves dispatched commands whose reconciliation must outlive the mounted panel. Reads remain capped at 50 and generation guards reject obsolete results. Adding cancellation/ownership branches across those operations is not a direct paging correction. | reject under the low-finding complexity rule; preserve command ownership. |
| B4: older audit-open evidence replaces newer refresh | medium, outside this review — the current open/install code and separate open generation were introduced by Story 5.7 (`362a957de11ce141f48d73d9ed4301f31a1c870e`, hardened in `9345d9d0fc8666205816ec960b9f9328e7602f93`). Submit re-reads complete evidence. This concerns later competing UI operations, not consistency within the paging walk. | excluded by the user's paging-only scope; preserve later work. |
| B5: same-subject authorization revisions mix | medium — the gateway explicitly versions same-subject authority transitions, but the loader checks only `CallerScope` and stamps the terminal revision onto all rows. Two individually valid pages from different revisions can be accepted as one current authority population. | patch: require a stable authorization revision alongside caller and projection version. |
| B6: no direct successful empty-walk coverage | low — valid scoped empty evidence is required for first-administrator restoration; only malformed empty shapes are directly exercised. | patch: add an empty success case with cleared paging/validator and valid restoration preview. |
| B7: exact page-limit success untested | low — cap exhaustion is tested but a terminal page exactly at the allowed count is not. | patch: cover terminal success at caps one and three without an extra request. |
| B8: terminal cursor contradiction untested | medium — `HasMore=false` with a nonblank cursor must never become complete evidence; direct malformed-shape coverage lacks this case. | patch: exercise a final continuation with a stray cursor, plus the real gateway empty-page seam in R1. |
| B9: completeness guards not isolated | medium — combined `HasMore=true`/`IsCompleteEvidence=false` fixtures cannot independently protect either boundary. | patch: isolate both contradictory signal combinations for preview and event-backed confirmation, restore and revoke. |
| B10: supplementary continuation cancellation coverage | low — there is no combined held-continuation/owner-lifetime test. Existing navigation tests check captured supplementary-token cancellation, loader tests stop after cancellation before another read/acceptance, and the lifetime lane verifies owner disposal discipline. The same token reaches every page; no changed cancellation owner or demonstrated failure was identified. | reject under the low-finding complexity rule; retain existing component and loader coverage. |
| V1: terminal incomplete confirmation not independently tested | medium — pre-verified gap: the existing pending fixture is also `HasMore=true` and at the old version, masking loss of the `IsCompleteEvidence` guard. A current terminal version reaching committed progress must stay pending without complete-population evidence. | patch, grouped with B9 after individual classification. |
| R1: empty-page mapping hides continuation | high — root tracing found `TenantQueryGateway` constructs `Empty` without the payload's `HasMore`/cursor. The REST shape validator permits an empty continuation. A full walk can therefore accept an early empty page as terminal and prove a false absence, bypassing the loader's existing empty/terminal guards. | patch: preserve the one-page payload's paging signals in the snapshot, without changing server/public contracts; prove canonical incomplete output through the real gateway. |

B2/B4 are explicitly outside the user's requested paging ownership boundary, rather than being reassigned to this spec. No deferred-ledger edits are authorized by this spec. B1/B5/R1 and the grouped verification gaps are the retained patch scope. Story 5.7 command identity, causal proof, last-admin guards, attempt/lease retention, and later strict duplicate rejection are KEEP requirements.

### 2026-10-29 — Resumed review of the existing paging patches

All three fresh-context review layers completed before triage. The complete baseline-to-worktree diff, including untracked files, was staged only in `/tmp/paging-resume-review-19q464t7/all-since-baseline.diff` (113,500,729 bytes). Reviewers used extracted current paging hunks and scoped historical diffs to distinguish paging changes from unrelated later work. Blind Hunter returned ten findings; Edge Case Hunter returned none; Verification Gap Reviewer returned one finding. No review file was added to the Git index.

| Finding | Verdict and evidence | Disposition |
| --- | --- | --- |
| RB1: member paging supersedes an unfinished detail administrator walk | medium, pre-existing — the original baseline already starts the bounded full walk under the shared load generation and rejects its result after `LoadMemberPageAsync` calls `BeginLoad`. Extraction and the current failure-retention patch do not introduce this ownership behavior. | excluded from this paging resumption; preserve the existing operation owners and the explicit prohibition on deferred-ledger edits. |
| RB2: empty-page tests do not isolate `HasMore` preservation | medium — every malformed empty REST response also has a nonblank cursor. Removing only the new `HasMore` assignment would leave those assertions green while a null-cursor, `HasMore=true` response could be accepted as complete. | patch: exercise that response on page one and on a continuation. |
| RB3: real authorization-revision producer is not composed with the walk in tests | medium — current gateway tests cover same-subject permission transitions and loader tests cover manufactured revision changes, but no test requires the real gateway's revision stamps to differ between two pages of one walk. | patch: combine the real gateway/provider and loader with an authority transition between completed page reads; require canonical incomplete output. |
| RB4: retained-detail tests do not include a partial replacement walk | medium — the retained-evidence regression supplies an immediate refresh failure; it does not distinguish old completed rows from rows newly collected before a continuation failure or revision change. | patch: cover both continuation outcomes and require only the previous completed rows to survive as unknown/incomplete evidence. |
| RB5: retained rows keep current row-level metadata | false as a current safety defect — `ToFailClosedGlobalAdministrators` predates this patch, and current member consumers also require the enclosing snapshot's current lifecycle/freshness and completeness. No current mutation bypass or divergent caller was identified. | reject the speculative future-consumer change. |
| RB6: unsupported initial page sizes fail closed after gateway normalization | low — current scoped callers all use the valid default. Normalization/rejection adds a new helper boundary for an input no present caller supplies; the observed outcome already fails closed. | reject under the low-finding complexity rule. |
| RB7: the helper's initial validator is ineffective in the real gateway | false as a helper-contract defect — the loader preserves the validator when calling the one-page seam, as documented and tested; the gateway intentionally strips validators without matching retained evidence. No complete projection is incorrectly accepted. | reject; preserve the gateway's validator boundary. |
| RB8: cap exhaustion shares the unavailable reason | false as a defect — the explicit bounded-walk contract requires incomplete evidence at exhaustion. The canonical unavailable result satisfies that contract without exposing cursor or identity material; no distinct operator reason is required. | reject the optional diagnostic expansion. |
| RB9: preview reads continue under `CancellationToken.None` | low — carried from current-HEAD B3: later audit operation ownership and standalone dispatched-command reconciliation supply that token; the shared loader observes supplied cancellation and caps requests. | keep the prior rejection; preserve command ownership and do not patch or defer again. |
| RB10: a stalled production read can wait indefinitely | false for the current transport — `TenantsRestQueryClient.SendAsync` links the caller token to the configured `HttpClient.Timeout` deadline, including response-body work. The registered client retains its finite default timeout; the walk is also request-count bounded. No production infinite-timeout configuration or reachable unbounded stall was shown. | reject the speculative overall-deadline policy. |
| RV1: null-cursor `HasMore=true` empty responses are untested | medium — pre-verified gap: the existing theory always supplies a nonblank cursor, masking removal of `HasMore` preservation. Both first-page and continuation cases are needed to protect complete absence evidence. | patch, grouped with RB2 after individual classification. |

RB2/RV1 share one missing signal-isolation case. RB3 and RB4 are separate integration checks; all retained patches are tests, with no production/public-contract change. The current three production fixes, later strict duplicate rejection, Story 5.7 semantics, unrelated edits and gitlinks are KEEP requirements. The CI provenance refusal remains an independent completion blocker.


### 2026-10-29 — Review after paging commit

Review resumed at `e2efd286fed08caa45977530f5e7ba5246b5d066`, committed by another process during initial investigation. All three fresh-context layers completed before classification. The complete original-baseline diff, including untracked files, was captured at `/tmp/paging-build-review-z8s0adz5/all-since-baseline.diff` (113,517,863 bytes); extracted historical and current paging diffs supported attribution without adopting unrelated later changes.

| Finding | Verdict and evidence | Disposition |
| --- | --- | --- |
| CB1: member paging drops an unfinished detail administrator walk | medium — carried from RB1: the shared-generation behavior already exists at the original baseline; current extraction and retention changes do not introduce it. | retain the prior exclusion and operation owners; no repeat patch or deferred-ledger entry. |
| CB2: one-page list completeness ignores a stray terminal cursor | medium, outside paging correction — the cited consumer is `GlobalAdministratorsPage`, whose separate list paging is explicitly excluded by the intent. Correction and detail consumers use the shared loader, which rejects the contradictory cursor. | preserve the explicit unrelated-list boundary. |
| CB3: grant/remove preview boundaries trust contradictory complete flags | false for the scoped correction path — the complete loader rejects contradictory paging before producing evidence, and correction snapshots independently reject `HasMore` and incomplete evidence before preview/confirmation. The cited alternate entry is the excluded separate list path. | reject the demonstrated-outside-scope/speculative-inside-scope change. |
| CB4: audit-page eligibility/opening lacks administrator continuations | medium — all existing parent providers return terminal administrator snapshots; multi-page detail and standalone tests cannot protect the audit parent's initial enrichment and opening. | patch: rendered later-page target and full-count coverage. |
| CB5: audit parent's confirmation provider lacks multi-page verification | medium — standalone fallback tests cannot detect replacement of the parent's complete provider with a single-page read. | patch: restore and revoke through the mounted parent, with complete post-command sequence evidence. |
| CB6: stale/lifecycle/missing-version continuation boundaries lack isolated coverage | medium — the direct surface-state theory fails on page one for these cases, while later duplicate-row cases combine other rejection reasons. | patch: a valid first page followed by invalid continuation evidence; require canonical redaction. |
| CB7: malformed continuation shapes lack isolated coverage | medium — request-scope, page-size, and malformed-identity cases currently fail before accumulating rows. | patch: continuation variants that cannot retain previously accumulated rows. |
| CB8: non-adjacent cursor cycles lack direct coverage | low — the existing repeated-cursor case does not distinguish a visited set from comparison with only the preceding cursor. | patch: one three-response cycle with no fourth request. |
| CB9: cancellation while awaiting a continuation lacks direct coverage | low — synchronous first-response cancellation does not prove cancellation after an awaited terminal continuation. | patch: hold the continuation, cancel, then return valid-looking evidence and require cancellation. |
| CB10: stable nonzero revision lacks a positive multi-page control | low — negative revision-change tests need a successful revision-qualified continuation to distinguish unconditional rejection. | patch: qualify the existing successful aggregation fixture and assert its revision. |
| CE1: unavailable intent cannot recover through the opening callback | false as a rendered recovery failure — grid and receipt emit opening callbacks only for available intents. The deliberate `!intent.IsAvailable` admission gate came from completed Story 5.7; audit Refresh reloads complete evidence and rederives the rendered launcher before opening. | preserve the admission gate; cover rendered incomplete-to-complete recovery alongside CB4. |
| CV1: audit-page complete refresh lacks multi-page verification | medium — pre-verified gap: existing audit-page providers ignore the administrator cursor and return terminal pages, so single-page refresh mutation would remain green. | patch, grouped with CB4/CB5 after individual classification. |

CB4/CB5/CV1 share the missing rendered parent-integration verification. CB6 and CB7 protect different page invariants and remain separately classified. These accepted items change tests only. Current paging production changes, Story 5.7 admission/command/proof semantics, strict duplicate rejection, unrelated work, the deferred ledger, and gitlinks are KEEP requirements. The CI baseline-policy question remains separate and pending the user's scope answer. During verification, external Git work replayed the paging commit as `1d8c31d2abe809829e10ce1ddadc978c79489f3e` on top of `148e0fd209d690ed35cf33cfbce78b2f27bace43`; the intervening changes update SDK/documentation only, and the production, tests, and gitlinks match the initially reviewed commit. This invocation performed no Git mutations.

## Design Notes

The helper belongs above the one-page gateway: the server already supplies protected requester-scoped cursors, and `TenantQueryGateway` deliberately maps one page. A complete result is created only after every page passes invariant checks; `HasMore=false` alone is insufficient because an aborted terminal page may be mixed-version. `PagingRecovered=true` on a continuation is also incomplete because the gateway silently restarted at page one.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` -- expected: zero warnings and errors.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` -- expected: all UI tests pass.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-global-admin-projection-paging.md --ref ded413dfd4786983585c7f56a399825cc1afbae3` -- expected: every historical range gitlink is declared with its exact endpoint target. Independently require `git diff --exit-code HEAD -- references/` to prove this resumption moves no current gitlinks.


### 2026-10-08 — Final verification evidence

- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0`: exit 0; 0 warnings, 0 errors. Output retained at `/tmp/global-admin-paging-final-build.log`.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none`: exit 0; 3,986 total, 0 errors, 0 failed, 0 skipped, 0 not run, 55.896 seconds. Output retained at `/tmp/global-admin-paging-final-tests.log`.
- The full run includes aggregation/deduplication, opaque cursor forwarding and ETag clearing, version/lifecycle/freshness/recovery/cycle/cap failures, cancellation, detail retained evidence, standalone restore/revoke confirmation, and audit-page submission through the real complete-read provider. Additional review regressions cover current row lifecycle, rendered refresh recovery, stale/degraded pending-command recovery, pre-submit cancellation with independent dispatched-command ownership, focus, accordion grouping, and proof receipt navigation outside the displayed page.
- Scoped `git diff --check -- <paging bundle paths>`: exit 0, no whitespace errors; Git emitted only CRLF-to-LF normalization notices.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-global-admin-projection-paging.md`: exit 1; `RESULT: FAIL`; `baseline_commit 23b2a76 is not an ancestor of HEAD (695e965). The recorded baseline does not describe this working tree.` HEAD at this check was `695e9658b92aac7e500e554acc3366a0c5f877c7`. Concurrent work rewrote history after this run captured its baseline. The original canonical baseline is preserved, and this bundle contains no gitlink changes. This validation gate remains blocked; status stays `in-review` under the rendered review step's halt rule.
- `aspire run --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj` started the baseline; `aspire describe --format Json` reported the tenant UI/API running and EventStore finished. `aspire logs eventstore --tail 45` identified `OptionsValidationException`: `Authentication:WorkloadIssuer:ClientId` and `Authentication:WorkloadIssuer:ClientSecret` are missing while `Authentication:JwtBearer:Authority` is configured. This existing runtime configuration limits live command validation; no hosting/configuration changes belong to this implementation. `aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj`: exit 0, successfully stopped the AppHost started by this run.
- Full baseline-to-worktree and scoped review diffs are retained as temporary artifacts. Review and patch verification isolate this paging bundle from unrelated concurrent dependency, submodule, documentation, and test edits. All unrelated edits and the deferred-work ledger were preserved; this run performed no staging, commits, branching, submodule updates, or remote operations.

### 2026-10-29 — Resumed verification; review boundary unresolved

- Verified tree: `03615cc91d940ca37b47fb13413a90a31151dd71`. The paging implementation already exists; this resumption changed no production code or tests.
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0`: exit 0; 0 warnings, 0 errors. Output retained at `/tmp/global-admin-paging-resume-build.log`.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none`: exit 0; 8,011 total, 0 errors, 0 failed, 0 skipped, 0 not run, 99.965 seconds. Output retained at `/tmp/global-admin-paging-resume-tests.log`.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-global-admin-projection-paging.md`: exit 1; `RESULT: FAIL`; `baseline_commit 23b2a76 is not an ancestor of HEAD (03615cc). The recorded baseline does not describe this working tree.` The recorded `baseline_commit` is unchanged.
- Provenance investigation established that the existing `baseline_revision`, `6bd5fc29cf66e0238a4ea5ceb8c743c358fe3513`, is the parent of the first paging implementation, `7716f0b11423eb54d74935b6cc6e3edf405dc400`, and is an ancestor of the verified tree. A temporary copy of this spec using that original baseline passes gitlink validation with `--ref 7716f0b11423eb54d74935b6cc6e3edf405dc400` (no pointer changes). The same temporary copy fails against current HEAD with nine undeclared later submodule changes. This probe does not repair or replace the required gate against the tracked spec.
- Current sprint tracking and `5-7-global-administrator-correction-verification.md` record Story 5.7 as done; the current audit-page correction path therefore includes later work beyond this spec's historical scope correction. A review endpoint was requested to distinguish a current paging review from a historical implementation review. No independent review layers ran while that boundary remained unresolved. Status remains `in-review`; no staging, commits, submodule changes, or deferred-ledger edits were performed, and unrelated concurrent edits were preserved.


## Provenance Repair — 2026-10-29

The original committed spec at `7716f0b11423eb54d74935b6cc6e3edf405dc400` records `baseline_revision: 6bd5fc29cf66e0238a4ea5ceb8c743c358fe3513`. Git independently confirms that revision is the creation commit's exact single parent and an ancestor of current HEAD. It is now also the validator's `baseline_commit`. The later, rejected value `23b2a7691ae667fd160a48a5d7293f3cf38d48fe` is retained as `rejected_baseline_commit`; it is an available commit outside this implementation's ancestry, so the prior claims of an original baseline and a proven concurrent rewrite are not established provenance. Earlier failed command results above remain historical evidence.

The initial implementation endpoint is `7716f0b11423eb54d74935b6cc6e3edf405dc400` and moves no gitlinks. It is insufficient as the final endpoint: `5a3bc6dd4c1aa77cec50d0a960e986aa89c08cbe` subsequently records and ships this spec's review patches together with unrelated dependency work. `10c9f6f66632f6861fe13fb9f41ab05b90c37f67` records the Story 5.6 scope correction; `ded413dfd4786983585c7f56a399825cc1afbae3` completes the cleanup and is the final historical implementation endpoint. These are all canonical Git commit IDs obtained from this repository, in ancestry order.

The historical audit uses the unchanged validator with explicit `--ref ded413dfd4786983585c7f56a399825cc1afbae3`. The nine pointers below inventory the entire original-baseline-to-endpoint range, including intervening and bundled dependency work; they do not assert paging runtime ownership or authorize restoring those old pointers. No `Story-Owned Gitlink Commits` record is fabricated for unrelated updates. The current review is against `73478ea3b21dbdc416d5b54b59260f851fbcd965`, restricted to paging-owned behavior and its current integrations. Completed Story 5.7, its causal command/proof and attempt-retention gates, later strict duplicate rejection, and later submodule updates remain authoritative and must survive this repair. The historical read-only audit-page restriction above is not reinstated over completed Story 5.7.

## File List

Paging source and tests under review are listed in Code Map and Tasks & Acceptance above. The following are historical range declarations only:

- `references/Hexalith.AI.Tools`
- `references/Hexalith.Builds`
- `references/Hexalith.Commons`
- `references/Hexalith.EventStore`
- `references/Hexalith.FrontComposer`
- `references/Hexalith.McpCli`
- `references/Hexalith.Memories`
- `references/Hexalith.Platform`
- `references/Hexalith.PolymorphicSerializations`

## Completion Notes List

These exact targets belong to the reconstructed historical range only. Paging introduces no new dependency requirement. Current submodule checkouts and later independent updates are preserved.

| Path | Original baseline | Historical endpoint |
| --- | --- | --- |
| `references/Hexalith.AI.Tools` | `de38f78ef7672df2a0997ddc60bf35ba0d02fa25` | `3f194e17174994d308ec84af9ee2b5aa68674d0d` |
| `references/Hexalith.Builds` | `5c3ff35c590cfae9f3a9784b75d08dd065c55cef` | `58d9b546b4741a246121ab40fc3945703db2e19b` |
| `references/Hexalith.Commons` | `feab4efc8804692fbe3c522a9d753559959ea22d` | `b247ed116c6523f8c596ec0a933eff8973d11568` |
| `references/Hexalith.EventStore` | `5ddda34f2ff0ffb0f72a60c44b265f2e4838a332` | `07d1e23a6c5b06bbbb1fc8ddb5174cc3382d3d93` |
| `references/Hexalith.FrontComposer` | `dca2d63a2542e72d6fe6d24142f3c267a6b8fde5` | `0e114214007c22f5cdbac21a6853cff4208340ee` |
| `references/Hexalith.McpCli` | `0000000000000000000000000000000000000000` | `342e072231279831c2fca0a85cbcd9205444fbec` |
| `references/Hexalith.Memories` | `647093889d2231d13b9d8c9544670ffc8c174843` | `906bc07ad6a8e4912a7222d9d097da434148266a` |
| `references/Hexalith.Platform` | `0000000000000000000000000000000000000000` | `f5a0d72f9b72e88008562147a7085da0607d55f7` |
| `references/Hexalith.PolymorphicSerializations` | `65fc33613db30f562dfe7daf92bf84b5cbb7eb4c` | `98de6e013840ece9f0fa7c68ab7dcdf2bba3b375` |


## Resumed Verification — 2026-10-29

Three current paging defects are patched: returned unavailable reads preserve only previously authorized detail rows as incomplete/unknown evidence; the complete loader rejects a same-caller authorization-revision change; and the one-page gateway preserves empty-page cursor/HasMore signals so the full loader can reject false terminal evidence. No server/public contract, Story 5.7 command/proof semantics, duplicate policy, unrelated later source, or submodule checkout was changed.

- Before the production patches, the nine focused defect cases produced seven failures and two passing controls. After the patches, all 17 focused regressions passed, including the independent completeness-signal matrix, valid empty restoration, exact page-cap success, and contradictory terminal cursors.
- Final required UI build: exit 0, zero warnings and errors. Final full executable suite: exit 0, **8,027 passed**, zero errors/failures/skips/not-run, 97.718 seconds. The pre-patch suite passed 8,011 tests. Existing Story 5.7 and later UI behavior is included in the final full suite.
- The tracked repaired spec passes `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-global-admin-projection-paging.md --ref ded413dfd4786983585c7f56a399825cc1afbae3`: exit 0, `RESULT: PASS`; every historical range pointer has its exact full target. `git diff --exit-code HEAD -- references/` and scoped `git diff --check` also pass. The validator and CI configuration are unchanged.
- The read-only CI provenance preflight, `python3 _bmad-output/implementation-artifacts/evidence/global-admin-projection-paging-2026-10-29/check-ci-provenance.py`, exits 1: `historical audit must retain the endpoint story's baseline_commit.` It invokes the existing `audit_ref` gate against the current uncommitted Markdown and existing Git history, not a fabricated committed event. The historical endpoint records the rejected baseline; the documented correction exception is unavailable because that rejected object exists. This is a separate policy limitation from the passing direct historical gitlink audit. No validation was weakened to bypass it.
- Runtime prerequisite: the Aspire AppHost started successfully; EventStore, security, Tenants, tenant API and tenant UI reached Running/Healthy. The historical workload-issuer failure did not recur. No live domain command was submitted. The AppHost started by this run was stopped successfully before final test verification.
- **Status remains `in-review`** because the applicable CI provenance gate refuses the repaired metadata. A separately authorized guard-policy correction is required to make that gate accept this independently verified original-baseline repair. Marking this spec done, changing the baseline to HEAD, inventing unavailable-object metadata, or restoring old submodules would misrepresent the evidence.

Persistent evidence: [verification commands and results](evidence/global-admin-projection-paging-2026-10-29/verification.txt), [canonical provenance](evidence/global-admin-projection-paging-2026-10-29/provenance.json), [historical gitlink output](evidence/global-admin-projection-paging-2026-10-29/gitlinks-historical.txt), [CI refusal](evidence/global-admin-projection-paging-2026-10-29/ci-provenance-preflight.txt), and [raw verification logs](evidence/global-admin-projection-paging-2026-10-29/verification-logs.zip). All preexisting spec notes are retained. Unrelated concurrent edits were preserved; this run made no changes to the deferred-work ledger, Story 5.7 artifacts, dependency documents, root guidance, or gitlinks.

### Additional review resumption — 2026-10-29

The three independent review layers ran again against the existing paging patches and current integrations; all findings are individually classified in the resumed-review triage above. Six regression cases now independently protect empty-page `HasMore` preservation, real same-subject authorization-revision changes between pages, and retention of only previous completed rows after an incomplete replacement walk. This invocation made no additional production changes.

The final UI build passes with zero warnings/errors. All 14 focused cases pass. The maintained Microsoft.Testing.Platform project lane passes **8,033 tests**, zero failed/skipped, in 98.665 seconds; its initial run passed 8,027 before these six cases. The historical gitlink audit, unchanged current gitlinks and scoped whitespace gate pass. Eight other preexisting changed files match their invocation hashes, including the three production files, deferred-work ledger and unrelated dependency/context documents.

The exact CI provenance preflight still exits 1 with `historical audit must retain the endpoint story's baseline_commit.` No policy or metadata bypass was applied. Status remains **in-review**; completing this spec requires a separately scoped CI guard-policy correction. See [resumed verification and exact commands](evidence/global-admin-projection-paging-2026-10-29/resumed-review.md) and [raw resumed logs](evidence/global-admin-projection-paging-2026-10-29/resumed-review-logs.zip). The earlier verification evidence and all preexisting spec notes remain intact.


### Post-commit review verification — 2026-10-29

All three fresh-context review layers completed against the existing paging implementation and current integrations. The accepted findings add 19 regression cases across the audit-page and complete-loader test files, including the real parent provider for restore/revoke confirmation and rendered recovery from initially incomplete evidence. No production change was required; all individual findings and preservation decisions are recorded in the post-commit triage above.

Final UI build: exit 0, zero warnings/errors, using SDK `10.0.403` after the external SDK/documentation update. The final spec-command executable run passes **8,052 tests**, zero errors/failures/skips/not-run, 95.864 seconds. The maintained project lane also passes **8,052**, zero failed/skipped. Focused rendered parent cases pass 7/7 and loader cases pass 124/124. An earlier unchanged-tree run had one deadline-test timeout while the runtime baseline was starting; both cases passed immediately in isolation, and both later full runs pass. No unrelated timer production change was made.

The exact historical gitlink audit and preservation/whitespace checks pass. The AppHost started by this invocation was stopped; no live command or full runtime/browser qualification is claimed. External Git work replayed the existing paging commit at `1d8c31d2abe809829e10ce1ddadc978c79489f3e`; this invocation performed no staging, commits, branching, pushes or submodule operations and preserved the external SDK/documentation changes.

The exact CI preflight still exits 1 with `historical audit must retain the endpoint story's baseline_commit.` An existing committed-range replay independently reproduces that refusal for this spec while the other selected spec passes. The scope question about the separately authorized CI guard-policy repair remains pending; no policy or metadata bypass was applied. Status remains **in-review**.

See [post-commit verification, exact commands and remaining gate](evidence/global-admin-projection-paging-2026-10-29/post-commit-review.md) and [raw logs](evidence/global-admin-projection-paging-2026-10-29/post-commit-review-logs.zip). All previous notes, the deferred ledger, later Story 5.7 behavior, production changes and current gitlinks are preserved.

### CI provenance policy repair completion — 2026-10-29

The separately authorized [CI policy repair](spec-ci-historical-baseline-repair.md) resolves the final local completion gate through a general verification rule. `gitlink_original_baseline` retains the rejected, locally available commit `23b2a7691ae667fd160a48a5d7293f3cf38d48fe`; `gitlink_creation_ref` names the independently discovered sole creation `7716f0b11423eb54d74935b6cc6e3edf405dc400`. Git verifies that creation's recorded `baseline_revision` and exact single parent are `6bd5fc29cf66e0238a4ea5ceb8c743c358fe3513`, and that the rejected commit is outside endpoint ancestry. The historical endpoint and all nine target declarations remain unchanged.

The provenance preflight and historical gitlink audit now pass. Actual-history disposable committed fixtures pass repair push/PR events and later prose edits both with the available rejected object and in a natural transport clone that lacks it; source object availability is preserved. False creation and gitlink-bearing move/revert events remain refused. The original unrepaired committed event still fails, as it must; no hosted CI run or source commit is claimed.

All three independent guard-policy reviews and their follow-up checks passed after correcting the identified clone, parsing and error-handling gaps. The final regression lanes pass 101 changed-story, 77 production-guard, 13 attachment and 37 default-branch tests. The paging build passes with zero warnings/errors; the final full UI run passes **8,052 tests**, zero errors/failures/skips/not-run, 91.863 seconds. An initial deadline-test timeout and its passing isolated/full reruns are retained as evidence. All applicable local completion gates pass, so status is now **done**.

The [exact verification commands, results and review triage](evidence/ci-historical-baseline-repair-2026-10-29/verification.md) retain prior failures and the original paging spec bytes. The preservation audit confirms no changes to paging production code, completed Story 5.7 behavior, the existing UI test edits, unrelated tracked work, the deferred ledger, HEAD or current gitlinks. No staging, source commit, push or submodule operation was performed.
