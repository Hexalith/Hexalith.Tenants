---
title: 'Keep command waiting, availability and recovery feedback accurate'
type: 'bugfix'
created: '2026-10-21'
status: 'done'
route: 'oneshot'
baseline_commit: 'ab450987de52d3cb305e74da3039c9b0d2cc335b'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Complete five related command-feedback deferred records: DW-303, DW-449, DW-506, DW-586 and DW-657. Lifecycle actions with missing projection-version evidence must remain unavailable and explain that missing evidence with dedicated English/French message and recovery keys, preserving the existing unavailable-reason enum and precedence. A temporary pending command-status lookup must preserve waiting state, identity and independent audit evidence with polite feedback across create, add-member, change-role, remove-member and metadata commands. Global-administrator correction recovery text must name a delivery retry only when the actual retry predicate permits it, including blank correlation handling. Unavailable add-member command support must normalize null, empty and whitespace reasons to localized fallback text and refuse dispatch even through captured submit events; available-support recovery must remain usable. Close only these five records after verification and independent review, preserving historical ledger evidence and unrelated work. The user requests committing and pushing the completed changes to main.

</frozen-after-approval>

## Implementation Notes

- Small cohesive command-feedback maintenance batch: private message selection/guard corrections, whole-string resources and focused regressions. No new public API, unresolved intent choice, persistence/dependency change or deployment is needed; use the one-shot route. Main starts clean at the canonical baseline above.
- Loaded root guidance, Hexalith baseline/Git/UX instructions, project context, architecture AD-1 through AD-14, contributor/build/test and formatting configuration. Aspire inspection finds the existing AppHost running, with Tenants API/domain/UI, Memories and security healthy; no AppHost change is required. Runtime details remain outside the repository.
- Code map: lifecycle compatibility adapter plus `Components/Tenants/Lifecycle/TenantLifecycleActionAvailability.razor` and `TenantLifecycleCommandFlow.razor` render high-impact reasons independently; qualify both launcher and mounted flow. Preserve shared evaluator taxonomy, precedence and fail-closed behavior. Resource files own complete EN/FR strings.
- `Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor` `RefreshActionText` currently selects retry copy from null correlation/ambiguity alone; use the existing actual refresh/retry predicate. Reuse rendered correction tests with real retained attempts.
- `Components/Tenants/Members/AddTenantMemberFlow.razor` `EligibilityUnavailableReason` and admission-refusal text use null-only fallback; normalize absent reasons consistently. Reuse `MemberCommandExplanationTests` valid-input flows and capture form callbacks for submit-time guards.
- Investigation finds DW-449/DW-586 already implemented in all five type-named snapshot files: `ApplyStatus` handles pending/retryable lookups before null-status failure and preserves state with polite feedback. Add focused cross-command state/identity/audit regression evidence and close as existing fixes verified, without rewriting production snapshots.
- Validate with Debug source UI-test build, focused and full maintained MTP project lanes, scoped formatting/whitespace and story-gitlink checks. Preserve all ledger IDs/historical fields/unrelated records and exact open-ID index coverage; retain release holds and submodule pointers. Validate the exact full commit message with installed lockfile-pinned commitlint before committing; fetch/inspect remote divergence before pushing main.

- Initial Debug build passes with zero warnings/errors. The initial combined filter used an unsupported pipe expression and selected zero tests (minimum-count refusal, exit 5); subsequent separate class filters run the maintained project lane. Pending 20/20, member 52/52 and lifecycle 126/126 pass. Initial correction 133/135 exposes a new grant-test fixture preflight simulation adding the grant target; disabling that simulation in this test yields 135/135. Six admission-fallback cases are then added.
- Original-source replay substitutes the exact baseline bytes for the five changed production logic files, leaves final test/resources intact and restores patched bytes in finally. Original build has zero warnings/errors; missing-version 0/24, member 46/58 and correction-label 0/4 demonstrate the defects; pending 20/20 confirms already-fixed production. All runs have zero skips.
- Independent Blind Hunter computes its floor as min(floor(sqrt(41.368) + 1), 10) = 7. Review identifies seven in-scope corrections: recover from a known unsent support refusal, align the recovery paragraph with retry eligibility, refuse whitespace correlation status lookup, cover additional freshness branches, cover later-blocker precedence, actually dispatch after version restoration, and independently apply each pending lookup to the original audit state. All are small patches or verification improvements, with no new deferred work planned.
- Recovery now retains an exact-reference marker only for known unsent refusals; current support can reset that precise snapshot before the tracking-lost gate. Adopted/ambiguous/expired/replaced attempts do not match it. Admission refusals use the same boundary while retained retry identities remain protected. Blank correlation cannot enable status refresh. Busy ambiguous correction recovery uses existing active-attempt guidance; actual delivery eligibility still controls retry labels.

## Review Triage Log

1. **medium / patch:** Captured support/admission refusals could enable a later submit that only reported lost tracking. A known-unsent exact-reference refusal marker permits recovery; repeated refusals preserve their actual reason. Existing uncertain/adopted attempts cannot match the marker. EN/FR post-refusal recovery dispatches once.
2. **low / patch:** The recovery paragraph still named a delivery retry while reconciliation was busy. Both button and paragraph now use display eligibility including the refresh-in-progress interval; held authorization and adopted delivery cover busy guidance and restoration afterward. The core predicate used by running refresh remains unchanged.
3. **low / patch:** Synthetic whitespace correlation could enable an invalid status lookup. The correlated-status predicate rejects blank correlation; captured blank refresh callbacks dispatch neither a lookup nor a command. No production-gateway blank-tracking claim is made.
4. **low / patch:** Missing-version tests omitted Aging and Refreshing variants. Added positive baseline cases and a Refreshing-without-baseline precedence control.
5. **low / patch:** Missing-version precedence over later support/viewport/admission blockers lacked direct coverage. Added combined evidence controls while preserving earlier authority/read/lifecycle failures.
6. **low / patch:** Restored-version recovery stopped at enabled controls. All 24 recovery cases now submit once using restored version evidence and remain unconfirmed until later authoritative proof.
7. **low / patch:** Sequential temporary lookup cases did not independently retain original AuditPending for keyed Pending. Each raw/keyed pending/retryable case now starts from the original accepted/stored snapshot.

- Final independent recheck resolves all seven findings; no new deferrals. Reviewed focused evidence is 351/351 with zero failures/skips. Scoped formatting, whitespace and story-gitlink checks pass. Initial failures and precise original-source replay limits are retained in the companion validation evidence.

- First full UI run passes 7032/7033; the single old missing-version test still requires the stale-data refresh promise DW-303 intentionally replaces. Its name, stub keys and exact expected support guidance now match the new behavior, preserving disabled and zero-dispatch assertions. The complete affected lifecycle-action class passes 113/113, bringing focused evidence to 464/464. Final acceptance Debug build has zero warnings/errors; both prior scoped whitespace checks and the newly touched lifecycle-action test-file check pass.

- Final full UI acceptance passes 7033/7033 with zero failures/skips/errors after the expected-copy test updates; final Debug source build has zero warnings/errors. Exactly five records close: all 657 IDs remain, 425 done/232 open across 211 maintenance rows with unique exact open-ID coverage. Baseline parsed fields survive under their historical names and all unrelated records remain byte-identical. Final commands, initial failures, review and preservation evidence are in `dw-303-449-506-586-657-validation-evidence.md` and its ledger JSON. The exact full candidate commit message passes installed pinned commitlint 21.2.3 with zero problems/warnings.
