---
title: 'Complete five deferred historical dependency provenance records'
type: 'refactor'
created: '2026-10-21'
status: 'done'
route: 'oneshot'
baseline_commit: 'ffe9522b10f37f56cd9339c3387cbb4b7d16d897'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Complete DW-198, DW-246, DW-343, DW-360 and DW-384 as one historical dependency-provenance repair. Bind the removal, grant and dependency-refresh artifacts to verified immutable historical endpoints. Distinguish story implementation from independent dependency changes included in their historical ranges, and supply the refresh inventory missing from its File List. Preserve frozen intent, historical decisions, validation failures and original evidence.

Qualify the repaired documents with the existing gitlink guards and focused historical controls. Close only these five ledger records and update their maintenance index; preserve every unrelated record, current dependency pointer and release hold. Independently review, validate the exact commit message, commit and push main under the user's authorization.

</frozen-after-approval>

## Implementation Notes

- Planning found no unresolved human choice, new API or irreversible implementation step. Use the oneshot route for four historical Markdown repairs, one guard-guidance sentence, this spec and ledger/index/evidence closeout. Commit and push are explicitly requested.
- Read the Hexalith baseline/Git instructions, project context, architecture constraints, tracked build/format configuration, gitlink CI guidance and validator/tests. No application code, dependencies, topology, persistence or runtime configuration changes are needed; application/Aspire tests are outside this documentation repair.
- Code map: scripts/validate-story-gitlinks.py supports legacy range inventories and explicit recorded-commit mode; scripts/validate-changed-story-gitlinks.py binds gitlink_audit_ref only for pointer-free documentation events and requires the endpoint's original baseline. Preserve those contracts.
- Story 2.4's final historical artifact edit is b2b80941df874c2ee6772ca316841c480e0e493b, beyond its initial fa5ca559 endpoint. Story 4.2's final historical edit is cf0e420be4873e5d7a005316f3b056b192d3fb87. Document every range pointer without representing independent commits as product implementation.
- Dependency refresh retains its historical mid-story baseline de5784ca751f51a4cfe282f67e11b13dd1ed4b45 and closes at d90b074e37bd31bc817b7c5f39d3245ba20738fc. Explicitly qualify the earlier implementation window, including the topology alias in a3321266b61fc98f98f700901bd2cc5166eff4b0, rather than claiming the preserved baseline includes it. Recorded-commit mode can audit the final scoped Builds move while retaining the warning and separately proving earlier history.

- Initial historical guards pass for all four repaired artifacts; refresh retains its explicit checkpoint warning. Maintained Python regression lanes pass 77/77 and 49/49. The companion history checker exposed the need to model merge landings with first-parent traversal; correcting the manifest and checks preserves complete endpoint chains without double counting merged work.

## Review Triage Log

Blind Hunter: 454.566 kB; N = min(floor(sqrt(454.566) + 1), 10) = 10. All ten findings were checked against source, Git and the documents. Each is a simple evidence/index correction within the authorized historical-provenance repair; no frozen intent changes or new deferred items.

| Finding | Verdict / disposition | Evidence and correction |
| --- | --- | --- |
| 1. DW-549 row retains the removed record's disposition | low / patch | Normalize the surviving row to DW-549's awaiting-decision state and Story 5.6 action; retain its ledger body. |
| 2. Binding table baseline cells unchecked | medium / patch | Compare both full table endpoints against exact Git range differences. |
| 3. Refresh complete-window table unchecked | medium / patch | Verify all four complete-window rows and the actual artifact-introducing parent's identity. |
| 4. Omitted targets silently shrink controls | medium / patch | Require exact expected target keys and full SHA equality before running mutations. |
| 5. Saved validation JSON unchecked | low / patch | Default execution requires exact saved/computed equality; explicit --write-result records intentional successful updates. |
| 6. Merge branch evidence unchecked | low / patch | Verify all parent lists and the exact merged-commit list against Git. |
| 7. Attribution categories/support insufficient | low / patch | Validate categories and supporting paths. Correct the grant-preview commit's nonexistent spec-edit claim to its actual new grant-preview implementation. |
| 8. Entry replay accepts the wrong failure | medium / patch | Require every expected undeclared path, the specific baseline error and exact retained checkpoint warning. |
| 9. Superseded endpoints untested | low / patch | Both removal artifacts reject their initial endpoint and grant rejects its intermediate grant-core endpoint, with the omitted-pointer-commit diagnostic. |
| 10. Future main prevents historical replay | low / patch | Document an immutable introducing-commit detached-worktree recipe, preserving deliberate fixed-snapshot checks. |

- Corrected companion checker and manifest now cover exact table baselines/targets, merge parents, supported categories, original diagnostics and saved results. Full original document lines are preserved in order, beyond frozen-block equality. The grant support correction records an actual code path rather than inventing a spec edit. Final checker, independent recheck and committed-event verification follow.

- Independent recheck resolves all ten findings, with no remaining or deferred findings. The normal checker passes 63 controls, saved-result equality and exact preservation/coverage checks; seven focused bad-input review probes reject corrupted evidence. Both unchanged guard regression suites pass 77/77 and 49/49. Whitespace and the new-work gitlink audit pass. Final committed-event and immutable replay checks run before the authorized push.
