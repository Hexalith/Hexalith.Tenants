---
title: 'Resolve five deferred UI runtime qualification records'
type: 'bugfix'
created: '2026-10-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: 3808cd73f6560d035c417109d27d0a2b3365992b
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Complete five deferred UI records: DW-268, DW-328, DW-391, DW-434 and DW-489, then push verified changes on main. Repair scoped style ownership on the five originally identified Fluent component surfaces. Qualify administrator dialog keyboard, inert background, responsive layout and long identities, detail command recovery after an audit round trip, and the composed audit/correction layout through authenticated production components. Independently review configuration-removal retained expiry/proof. Preserve existing fail-closed behavior, exact command ownership, the approved heading/notice recovery fallback, and unrelated deferred work and release limitations.

</frozen-after-approval>

## Implementation Notes

- Investigation: DW-268 includes UserMembershipLookupPanel, TenantAuditPage, GlobalAdministratorsPage, AuditEvidenceReceipt and AuditEvidenceEntryPoint. Several classes on Fluent hosts lack the owning Razor scope. Existing semantic HTML rules work and should remain intact.
- Existing authenticated workspace validator covers grant keyboard trapping and safe widths; the navigation validator supplies a separate production UI process authenticated through local Keycloak, with controlled read-only loopback upstream. Extend that established fixture approach, explicitly disclosing backend/runtime limits and prohibiting domain command writes.
- TenantDetailPage initializes lifecycle/configuration accordion items expanded. Verify a collapsed-flow audit round trip before adding selective restoration; preserve Option A fallback and rejected Option B cursor/state persistence.
- DW-328 is a fresh independent follow-up against current removal expiry/proof source and focused regressions; later DW-325/326/327/587 fixes are continuity evidence rather than substitutes for this review.
- No unresolved user-visible policy choice, new public API, dependency movement, schema change or deployment is planned. Footprint is limited to existing UI layout ownership, regression/browser fixtures and qualification documents. The authorized main push follows review and verification.
- Verify Debug/source UI build; targeted component/state tests and full UI project; actual authenticated browser measurements and original-style negative controls; exact five-record ledger/index reconciliation and unchanged unrelated entries; pinned commitlint and story gitlink guard.
- Browser-driven discovery: FluentDataGrid repeats the supplied class on generated header nodes; activate the minimum-width rule only on the outer table with role=grid. The shared audit entrypoint gains a native isolation wrapper, and its existing create-flow test now checks both that owner and the exact enclosing recovery shell.
- DW-434 exposed a separate mount condition within the requested retained return: configuration management recognizes retained remove ownership but not retained set ownership. A returning set attempt consequently prevents its own child from mounting/re-adopting. The narrow correction preserves mounting for a matching, unexpired tenant set attempt; availability, dispatch and aggregate admission remain governed by their existing exact-owner checks. Add positive/expired/other-tenant regressions and real browser acceptance rather than introducing additional persistence.
- The initial full suite executed7549 cases:7547 passed, the changed entrypoint parent assertion failed, and an unchanged cancellation coordination test timed out. Both cases subsequently pass in a focused2/2 recheck after the expected parent assertion correction. Preserve that initial packet separately from final acceptance.

## Review Triage Log

Blind Hunter inspected approximately128kB of current changes (finding floor10). All ten claims were checked against source/callers and accepted for narrow correction; no new deferred record is required.

| Finding | Verdict | Correction and acceptance evidence |
| --- | --- | --- |
| Refused set expiry can remove the focused child without recovery. | medium, patch | Restore the existing management heading only when current retention is gone and no sibling child remains active; regression checks one exact heading focus. |
| Same-tenant retained replacement can lack its own expiry wake-up. | medium, patch | Re-read current tracker facts and schedule the replacement deadline before rendering; deterministic replacement regression preserves the new owner. |
| Audit-return browser only covers Accepted. | medium, patch | Cover retained RequestSent as well, with an unresolved isolated response, original identity/no redispatch and honest heading-notice fallback while audit is not started. |
| Expiry regression mixes injected and wall clocks. | medium, patch | Reuse the existing ManualTimeProvider for both tracker time and the standard DI TimeProvider timer; advance deterministically. |
| Shared entrypoint unavailable branch is unmeasured. | medium, patch | A controlled audit-probe failure checks the disabled Fluent root and named explanation plus the owner-scope negative control. |
| Changed media selectors lack runtime evidence. | medium, patch | Emulate forced colors and reduced motion; assert affected borders, keyboard focus outlines and receipt child transition duration. |
| Narrow entrypoint compares against its shrink-wrapped span. | medium, patch | Make the isolation owner display:contents and measure against the actual page-header/action-shell content width, with viewport bounds and a zero-box owner check. |
| Layout field containers can pass while showing wrong data. | medium, patch | Require exact fixture actor, target, scope, event reference, outcome and all eight start facts before accepting bounds. |
| Fingerprints captured only after build can misbind source. | medium, patch | Capture revisions, tracked build/source diffs and untracked source bytes before build, require equality after build and at acceptance, and qualify a source-change failure control. |
| Ledger validator accepts malformed/ambiguous current closure fields. | medium, patch | Require exactly one current done status/date/disposition and nonempty current verification fields with the owning evidence file. |
- The composed lifecycle test reproduced a pre-dispatch race: a successful own-lease callback re-renders the page as busy, changing the command flow parameter fingerprint before transport and refusing its own valid preview. The coupled correction must recognize only exact-own admission bookkeeping, retaining authority, scope, projection, confirmation and other-owner guards. Forced parent-render positive/negative regressions qualify this precondition for the requested audit-return journey; rejected browser packets remain evidence rather than being hidden through retries.
- Final delivery-policy qualification: earlier implementation-note shorthand “no redispatch” applies to Accepted returns and configuration RequestSent returns. Lifecycle RequestSent intentionally follows its existing automatic same-message redelivery contract. The browser requires all repeated fields/identity to match, six logical attempts and only the permitted seventh raw synthetic request; no new logical command or backend event write is introduced.
- Removal had the analogous exact-own lease render precondition. Its captured preview-input scope now tolerates only the original Available-to-Busy bookkeeping transition during the same submit with an actual lease callback; normal eligibility additionally requires confirmed activity ownership. Optional/no-op callbacks cannot normalize external Busy evidence. Forced-render negatives cover authority, version, tenant, support, viewport, confirmation and transient changes.
- Independent recheck confirms all ten findings resolved and establishes no additional reachable authorization, tenant, version, confirmation, generation or admission-ownership bypass. Final combined Debug/source test build passes with zero warnings/errors (14.26 seconds); all 43 new forced-render/expiry/return regressions pass with zero failures/skips. The new C# file passes focused dotnet format whitespace verification; the formatter reported workspace-loading warnings separately from the clean compilation.

| Additional verification finding | Verdict | Resolution and evidence |
| --- | --- | --- |
| One frozen-source grant Escape check left focus at body; final standalone recheck and safe instrumented trace passed. | maybe-false, unverified medium, defer | Preserve the failed packet, safe trace and final passing packet; an unnumbered follow-up requires controlled foreground/subscription timing to distinguish a reachable production defect from driver behavior. No speculative production focus patch or concurrency cause claim is made. |

## Final acceptance

Final normal source builds and all 33 authenticated browser cases pass (17 scoped/layout, 9 administrator, 7 retained return). All three packets bind current validator/source hashes and the same UI assembly used by the full test output. The final full UI project passes 7592/7592 with zero failures/skips in 134.29 seconds; all 43 new interleaving regressions pass. Documentation-only public-test XML comments were then added to follow the baseline, with a fresh zero-warning/error build and no assertion/runtime change.

Exactly DW-268/DW-328/DW-391/DW-434/DW-489 close: 657 numbered IDs, 590 done/67 open across 62 exact-covering rows. Original fields/duplicates, unrelated numbered records, archive/sprint, dependencies and release holds remain. Ledger checks pass normally and with Python optimization; five malformed/missing-field controls reject explicitly. Story gitlink validation and diff checks pass; the exact full candidate passes pinned commitlint 21.2.3 with zero problems/warnings. The unexplained earlier grant focus observation is the single new unnumbered, unverified-medium follow-up; its failed packet and later passing instrumented/normal evidence remain preserved.

- Remote integration: the first push failed in remote index-pack while main advanced to `9c343c2d36d6d291ffea60928b6377013d8fcc84`. Linear rebase preserves that build-default commit. UI/test Git trees are unchanged, source project selections remain true, fresh no-restore build passes with zero warnings/errors and integration 43/43 passes. Immutable original full/browser packets remain qualified at their recorded revision/binary; subsequent source-revision metadata differs. See `dw-268-328-391-434-489-main-integration.json`.
