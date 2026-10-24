---
title: 'Resolve five deferred navigation accessibility records'
type: 'bugfix'
created: '2026-10-23'
status: 'done'
route: 'dispatch'
baseline_commit: '48b0cd98be5ff75402e1206e083e561552146454'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Paging and audit recovery can remove keyboard focus targets. Detail-return focus lacks browser proof; the shared header permits empty titles and focus before rendering.

**Approach:** Resolve DW-119, DW-211, DW-212, DW-213 and DW-444; verify actual browser focus and shared diagnostics, then commit and push owning changes and Tenants integration to main.

## Boundaries & Constraints

**Always:** Keep shared header fixes in FrontComposer; preserve explicit title/blank heading and heading-derived titles. Restore focus only when the removed control owned it, using its available origin or existing heading fallback. Preserve authorization, cursors, generations, caller/disposal guards and submitted command ownership. Retain original findings; close exactly five verified records.

**Never:** Treat tabindex alone as browser proof; steal surviving focus; dispatch commands for evidence; rewrite historical baselines; change unrelated dependencies; initialize nested submodules. Accepted limitations and legacy Story 5.4 decisions remain open.

## I/O & Edge-Case Matrix

| State | Expected behavior |
| --- | --- |
| Focused pager; held read | Stable control or explicit recovery; no duplicate paging |
| Disabled/missing paging origin | List heading fallback |
| Missing-row detail return | Actual focus at tenants-list-heading; existing notice retained |
| Both title inputs blank | Diagnostic naming PageTitle and Heading |
| Heading focus before element capture | Diagnostic before browser interop; rendered path works |
| Audit reset removes focused unsubmitted panel | Origin/fallback recovery; surviving focus untouched |

</frozen-after-approval>

## Code Map

Tenants UI paths below are relative to `src/Hexalith.Tenants.UI/`; UI test paths to `tests/Hexalith.Tenants.UI.Tests/`. FrontComposer paths are relative to its root submodule `references/Hexalith.FrontComposer`.

- `Components/Pages/TenantsWorkspace.razor`: hard paging sets Loading and unmounts pager; reuse heading and detail-return handling.
- `Components/Pages/TenantAuditPage.razor`: LoadAsync calls ClearPaging after CaptureCorrectionAuthority; ClearPaging invalidates pending focus. Reuse origin, generations and receipt probes.
- `wwwroot/js/tenantsFocus.js`: existing active-element, launcher and fallback helpers.
- FrontComposer `src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageHeader.razor.cs`: title and focus guards. Entry revision: `9b8cbf28c621cbf45429eab8db1a50cfc4bf1c38`.

## Tasks & Acceptance

**Execution:**

- [x] FrontComposer `src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageHeader.razor.cs` — reject both blank inputs and premature focus; handle heading removal/reappearance.
- [x] FrontComposer `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcPageHeaderTests.cs` and `docs/reference/components/page-patterns.md` — cover invalid combinations, valid titles and rendered timing; document diagnostics.
- [x] `Components/Pages/TenantsWorkspace.razor` and UI test `TenantsWorkspaceTests.cs` — preserve/recover paging focus; pin missing-row heading, held reads, disabled endpoints and supersession.
- [x] `Components/Pages/TenantAuditPage.razor`, `wwwroot/js/tenantsFocus.js` and UI test `Components/TenantAuditPageTests.cs` — capture focus before reset; restore after clearing without obsolete probes, stolen focus or submitted-state loss.
- [x] UI test `Browser/validate-authenticated-navigation-focus.mjs` — reuse authenticated harness; record deepest focus for paging, missing-row return and audit removal, with surviving-control/missing-launcher controls.
- [x] `_bmad-output/implementation-artifacts/deferred-work.md`, `docs/maintenance-backlog.md` and colocated `dw-119-211-212-213-444-validation-evidence.md` — reconcile verified closures and exact open-row coverage; preserve unrelated records. Commit FrontComposer first; record exact target and update only its root gitlink.

**Acceptance Criteria:**

- Given authenticated keyboard use, when paging/detail return removes the target, then focus reaches a usable origin or list heading without duplicate paging.
- Given invalid header parameters or premature focus, when invoked, then diagnostic failure occurs while valid rendered/title paths work.
- Given an audit reset, when a focused unsubmitted panel disappears, then its origin/fallback receives focus; when another control owns focus, then it retains focus.
- Given passing relevant suites and independent review, when reconciling, then exactly five records close with evidence and owning commits are pushed to main.

## Implementation Notes

No unresolved product choice: DW-212 already records rejection of blank titles. No migration, deployment or domain-data mutation. Two repositories and live browser coverage require dispatch. Both repositories were clean on main. Existing policy keeps accepted limitations open for visibility.

Baseline preparation (2026-10-23): Aspire found no running AppHost, then started the existing target. Security and tenants-ui reached healthy state through `aspire wait`; structured telemetry was inspected/captured privately. The temporary non-persistent realm's chosen browser username/password are in mode-0600 `/tmp/tenants-navigation-browser-private.json`; consume them only as process environment and never print or track their values. Aspire-discovered state is privately retained at `/tmp/tenants-navigation-aspire-state.json`; rediscover the current HTTPS UI endpoint before browser use. Restart the owning UI resource after the accepted Debug build so browser evidence uses the rebuilt source. No source or stored-state repair was required for startup.

Implementation audit: the shared header changes are committed locally as `adc0dbe2228d6291df8b1fe5ba1dc8fb0080f631`; Tenants integrates only this root gitlink. Browser qualification uses the rebuilt production UI and its real OIDC flow in a separate process, with controlled loopback REST/SignalR read upstreams. It qualifies UI focus, not backend authorization or real domain projections. All nine browser scenarios pass without command requests or circuit errors. Relevant header/layout, workspace/audit and inventory regressions pass. The broad Shell run and failed initial browser fixtures are retained separately in the evidence packet; no nested submodule was initialized. A replayable ledger validator preserves original fields and verifies exact open-index coverage.

Remote integration: fetched FrontComposer main advanced by 22 commits to `4a4051c2511e5c4b7c0285fbc129c72146c1c275` without touching this batch's three header files or its commitlint policy. Rebased the sole local header commit with submodule recursion disabled; current target is `adc0dbe2228d6291df8b1fe5ba1dc8fb0080f631`. The original local endpoint `25b620001c1fe9bbb5171e823338d6ee1733d13f` remains historical evidence. This integrates published upstream changes required for a linear push; no nested worktree was initialized or updated. Reverify the resulting source and browser artifacts after review patches.

Review patch audit: retain an immutable paging-operation version and defer cursor mutation until after owned focus moves. A browser capture tracks focus until actual panel removal, including blur-before-disconnect and initially negative probes. Normal CI now exercises the DOM helpers; authenticated qualification covers Previous, authoritative search/fallback, preview and receipt origins. Narrow component, browser and spawn/signal/write-failure checks pass before parent verification. New C# names/braces follow the required conventions. The normal-build validator uses no audit/version fallback pins. The unpublished shared commit was amended for new-code formatting only; its prior rebased endpoint `376076a3b7f0e98f4e144d62120d5cdfd184e1b7` remains historical provenance.

Publication integration: three additional public commits advanced FrontComposer main to `2d92bb1a1061e1fecb7d87c9fb450f39698326f6`. Rebased the unchanged three-file patch again to `adc0dbe2228d6291df8b1fe5ba1dc8fb0080f631`; the prior formatting endpoint `b3a02724278e4027eb88a00da331df6b5dbc3deb` remains historical evidence. The additional source change is an upstream generated-command disposal guard; normal builds and layout checks pass at the new base, and integrated UI/browser checks are refreshed. The header source, tests and docs are identical to the independently reviewed patch. No nested worktree was initialized or updated.

## Spec Change Log

## Review Triage Log

Blind Hunter floor: 141,791 bytes / 1,000 = 141.791 kB; min(floor(sqrt(141.791) + 1), 10) = 10. All three layers completed before classification. Each finding is retained separately; duplicate root causes share the ensuing patch work.

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| B1 | medium | patch | OnAfterRenderAsync captures the origin but rereads a mutable version after awaiting module import. Capture that operation's version locally and reject an old continuation after a newer completed direction. |
| B2 | medium | patch | A false pre-removal focus probe can become stale while its result travels back and renderer work queues. Native focus can enter the panel before ClearPaging; preserve focus ownership through actual removal rather than treating the earlier negative as final. |
| B3 | low | patch | The harness accepts IP origins but starts its isolated UI on localhost, where the IP-scoped authentication cookie is absent. Align the accepted hostname with the fixture. |
| B4 | medium | patch | Evidence write occurs before cleanup in finally, so an invalid output directory skips resource shutdown. Ensure shutdown runs even when writing fails. |
| B5 | medium | patch | The new child process has no error listener; a launch error bypasses safe diagnostics. Handle and report failed startup while cleaning resources. |
| B6 | low | patch | Enter on the recovered heading is not a second pager callback. The component test genuinely invokes the captured callback; remove or accurately label the redundant browser claim. |
| B7 | medium | patch | Both Previous call sites changed but current focus cases use Next. Add held backward paging and disabled-first-page assertions. |
| B8 | medium | patch | The fixture has no search request, leaving the protected-search and fallback branches without focus assertions. Qualify these consumers as well as ordinary paging. |
| B9 | medium | patch | Only the grid start panel is exercised. The new selector and eligibility branches also handle preview, receipt origin and submitted/retained states; add focused positive and negative cases. |
| B10 | medium | patch | Source and assembly hashes are independent snapshots, allowing an unbuilt edit to be labeled with passing old binaries. Make the validator build or verify source-bound build provenance before execution. |
| V1 | medium | patch | Pre-verified: the normal CI Chromium runner does not execute the new exports. Add actual focus/ownership assertions to that maintained lane. |
| V2 | medium | patch | Pre-verified: changing Previous's helper argument to Next preserves current content/cursor checks. Add actual backward-focus coverage for ordinary and protected-search paging. |
| E1 | medium | patch | Same write-failure path as B4: cleanup is skipped on filesystem error. Preserve its individual report and fix the shared cause once. |
| E2 | medium | patch | A signal-terminated child has null exitCode and nonnull signalCode; waiting for an already-emitted exit can hang. Track termination or include signal state before awaiting it. |
| P1 | medium | defer | The unchanged subscription cancellation test asserts before detached cleanup must finish. One full run failed, then isolated/full rechecks passed; record deterministic test synchronization as an unnumbered follow-up without changing cleanup policy or weakening assertions. |
| P2 | low | patch | Newly added C# tests used legacy underscore names and new shared declarations inherited same-line braces. Apply the required PascalCase/Allman conventions only to new code, preserving unrelated legacy text. |
| P3 | low | patch | The new validator auto-build inherited audit/version fallback pins although normal source builds pass. Use the normal audit-enabled Debug source command before recording build-bound fingerprints. |
| R2-B2 | medium | patch | The recheck reproduced focus loss when a focused tenant start panel is removed while a separate global correction panel remains. TenantAuditPage independently renders these intents and ClearPaging retains the global intent. The capture waits for every matching panel to disconnect and its blur check considers surviving unrelated panels. Track the specific focus-owning panel; cover coexistence and late ownership in normal Chromium. |

Correction recheck 1: verification gaps V1/V2 and cleanup findings E1/E2 are independently clear; B1 and B3–B10 are independently clear. The concrete coexistence case R2-B2 remains a private DOM-capture patch with no intent change. All prior build/test/fault evidence was independently checked and retained; repeat the affected browser lanes after the patch.

Final correction recheck: Edge Case Hunter confirms E1/E2 resolved and no remaining cleanup finding. Verification Gap Reviewer reports no gaps. Blind Hunter confirms B1 and B3–B10 resolved, then independently reproduces all six coexistence controls after the owner-specific R2-B2 patch and reports no remaining defect. The retained cancellation-test timing follow-up is the only newly deferred item. The final two-file patch is requalified through the normal Chromium lane and authenticated source-building validator.

## Verification

- Before source edits, inspect/start existing AppHost through Aspire; establish health and discover browser endpoints.
- Build FrontComposer Shell.Tests and Tenants UI.Tests individually in Debug source mode. Run focused header/workspace/audit classes and relevant suites; record exact commands/results and fallback evidence.
- Run authenticated validator against rebuilt local UI; retain source/assembly/validator provenance, active-element observations and failed attempts.
- Run this spec's story-gitlink validator and committed-event guard, JavaScript syntax and owning whitespace checks.
- Validate each exact message with owning pinned commitlint; fetch/inspect divergence before pushing. Failed acceptance remains open.

## File List

- `references/Hexalith.FrontComposer`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor`
- `src/Hexalith.Tenants.UI/wwwroot/js/tenantsFocus.js`
- `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Browser/validate-authenticated-navigation-focus.mjs`
- `tests/Hexalith.Tenants.UI.Tests/Browser/tenants-focus-browser-validation.html`
- `_bmad-output/implementation-artifacts/deferred-work.md`, `docs/maintenance-backlog.md`, this spec and the colocated `dw-119-211-212-213-444-*` validation artifacts.

| Path | Baseline | Target |
| --- | --- | --- |
| `references/Hexalith.FrontComposer` | `9b8cbf28c621cbf45429eab8db1a50cfc4bf1c38` | `adc0dbe2228d6291df8b1fe5ba1dc8fb0080f631` |

## Completion Notes List

The FrontComposer pointer integrates this batch's shared header guards, regressions and component documentation. No other dependency pointer changes.

Final acceptance: current published shared base builds cleanly; 341 layout, 416 focused UI and 7,514 full UI cases pass with zero failures/skips. The normal Chromium lane, all six independent coexistence controls and the post-recovery source-building 24-case authenticated validator pass. All three independent review rechecks are clear. Browser/test UI bytes and source fingerprints agree. Exactly five numbered records close; the existing cancellation-test timing follow-up is separately retained. Shared main is published; the Tenants committed-event check and authorized push follow the final commit.
