---
title: 'Honest command availability and recovery explanations'
type: 'bugfix'
created: '2026-10-21'
status: 'done'
route: 'oneshot'
baseline_commit: '73e4cbee3c3d429ee90a81562ceb9060f8a82ce5'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Five open deferred records (DW-244, DW-293, DW-294, DW-298, DW-447) describe misleading Tenants command explanations. Missing command support and a flow's own reservation are presented as another command; recovery text promises permission, audit, status retry or escalation actions that may be absent; an older spec duplicates the copy decision.

**Approach:** Keep all existing fail-closed gates, reason values, proof and command behavior while selecting accurate localized member availability explanations for unknown support, the active member flow and genuine external contention. Replace affected recovery guidance with truthful state descriptions that remain accurate without optional callbacks or after retry exhaustion. Keep EN/FR parity and whole-string resources. Link the historical decision to the authoritative ledger, qualify rendered behavior through focused component tests and the full Debug UI suite, and close only these five records with preserved historical evidence. Commit and push the verified changes on main as requested.

</frozen-after-approval>

## Implementation Notes

- Investigation settled one cohesive copy goal with no intent gaps or irreversible implementation work: one existing member reason selector, whole-string EN/FR resources and component regressions. The authorized commit/push remains the final action.
- Kept `AggregateLocked` gating, all reason enum values and precedence intact. Known owned member reservations now use neutral active-command copy on every row; supplied external reasons remain visible without an owned member lease; missing reasons use unspecified unavailability copy. The terse reason catalog is unchanged.
- Replaced affected post-submit recovery guidance in metadata, set/remove configuration and member removal with state descriptions; shared recovery controls retain their existing callback and retry gating.
- Replaced the old spec's duplicate current-copy decision with historical notes referencing the authoritative ledger. Its pre-existing broad historical gitlink attribution is being checked separately; no dependency changes belong to this task.
- Initial Debug source build failed with one CS0103 in the new recovery fixture (missing configuration namespace); adding that import yields zero warnings/errors on the repeated build.

- Original production replay against the initial 52 new tests builds cleanly and fails exactly 42 cases; ten unchanged-cause/precedence controls pass. Exact feature bytes are restored in a finally block and rebuilt successfully.
- Initial full UI executes 6,848 cases: 6,846 pass and two fail (existing localizer-double parity and a held parent-command assertion expecting old wording). Aligned only affected fixture resource values and the real held-submission explanation assertion. The first scoped formatter reports nested-loop indentation; the scoped formatter corrects it.
- Independent review corrections extend neutral copy to Previewed, make UnableToVerify descriptions valid before dispatch, and use past-tense AlreadyApplied evidence. New tests check full operation-specific EN/FR descriptions, incomplete Accepted tracking, real blocked configuration previews and pending admission refusal/fault cleanup; existing parent-driven held submission covers the normal echo path. Public new test APIs now carry XML docs.
- Review exposes the unchanged add-member null-coalescing reason defect: a temporary real-input rendered probe fails four empty/whitespace cases and passes two null controls. The probe is removed and exact feature test bytes restored; the existing production defect is deferred separately, with no failing probe committed.

## Review Triage Log

Blind Hunter computed a ten-finding floor from 81.422 kB of changed content. Every finding is retained below.

1. **medium / patch:** Previewed copy names a distinct absent read-only action. Replaced it with neutral review guidance across the four scoped flow families; added six EN/FR rendered preview cases (metadata retains Idle while editing, so no unreachable Previewed fixture is invented).
2. **medium / patch:** Configuration Blocked snapshots reach UnableToVerify before dispatch. All four families now describe insufficient evidence without inventing a command outcome; four real unavailable-preview cases assert zero dispatch/status reads.
3. **low / patch:** AlreadyApplied snapshots can outlive the freshness of their evidence. The three affected resources now describe what projection evidence previously showed.
4. **medium / defer (DW-657):** Unchanged AddTenantMemberFlow treats empty/whitespace parent support reasons as usable reasons. A valid-input temporary probe fails four cases with enabled submit despite unavailable support; null controls pass. This separate admission defect predates this copy patch.
5. **low / rejected:** Exhaustive rendered assertions for every unchanged state-to-resource selection would add a producer/retained-state fixture beyond this copy change. Existing flow/state suites retain coverage of those selectors; new regressions focus on the consequential preview, rejected, degraded, blocked/unverifiable and no-tracking/retry-exhaustion cases. No concrete defect was identified in the remaining strings.
6. **low / patch:** Prefix-only assertions could miss a wrong requested operation. Recovery tests now assert complete expected EN/FR sentences including the correct operation.
7. **low / patch:** Tracking absence was unqualified. Eight malformed-Accepted tracking cases assert truthful UnableToVerify copy and zero status reads. Refresh is absent in the three flows whose predicates require correlation; set-configuration retains message identity and its existing reconciliation affordance. A SupportsCommandStatusLookup toggle alone does not govern these flows' CanRequeryAudit predicates, so it is not used as an invented visibility premise.
8. **low / patch:** Direct lease tests alone miss normal parent rendering. Updated the existing actual held add-member submission assertion in TenantDetailSurfaceTests to require the owned-member explanation; it also checks admission remains locked and sibling launchers absent.
9. **low / patch:** Pending lease refusal/fault cleanup lacked copy coverage. Twelve EN/FR owner cases require provisional active-member text, then generic/supplied external text after cleanup.
10. **low / patch:** New public member-test methods lacked XML documentation. Added summaries, parameter documentation and generated-case documentation.


## Final Verification

- Final Debug source build: exit 0, zero warnings/errors. Final focused 82/82 and full UI 6878/6878 pass with zero failures/skips; scoped new-test formatting passes. Independent correction recheck accepts all bounded corrections and the stated coverage limit.
- Exactly five records close; DW-657 is the separately reproduced pre-existing add-member reason-normalization follow-up. All 656 original IDs and 651 unrelated record bodies survive; current ledger/index has 657 total, 410 done/247 open across 224 exact-covering rows. Archive, sprint and dependency pointers are unchanged.
- New bundle gitlink guard passes. The amended historical spec retains its inherited full-range attribution failure (eight undeclared later pointers at entry HEAD); exact command, failed earlier endpoint and unchanged provenance claims are recorded in [validation evidence](dw-244-293-294-298-447-validation-evidence.md). This is not a Story Guards or release-readiness claim.

- Repository story-gitlink guidance requires an explicit historical endpoint for documentation corrections. Added `gitlink_audit_ref: b2b80941df874c2ee6772ca316841c480e0e493b` to the old spec without changing its baseline or frozen intent. This latest eligible amendment still fails six undeclared historical pointers; no endpoint is selected to hide that history.
