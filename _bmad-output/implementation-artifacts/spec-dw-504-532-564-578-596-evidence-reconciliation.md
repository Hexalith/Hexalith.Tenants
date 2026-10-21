---
title: 'Reconcile five deferred documentation and evidence gaps'
type: 'refactor'
created: '2026-10-21'
status: 'done'
route: 'oneshot'
baseline_commit: '092bcd0eafffb9e85b6c3506ee532f1340d6e87f'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Five open deferred records leave repository documentation inconsistent with current behavior and recorded evidence. Readers can infer tenant audit-proof links, immediate authority revocation or dependency/CI acceptance that the authoritative sources do not establish.

**Approach:** Add dated historical clarifications and refresh current dependency and CI guidance from tracked sources. Preserve historical acceptance text, unresolved audit/runtime/gitlink blockers and all unrelated work. Close exactly these five records with reproducible evidence and an exact maintenance-index reconciliation. Validate historical documentation against truthful repository provenance, run independent review, validate the Conventional Commit message, commit and push to main as requested.

</frozen-after-approval>

## Implementation Notes

- Investigation found no product decision needed and no runtime changes. The footprint is four existing documentation targets, the deferred ledger/index and supporting evidence. No dependency update, deployment or data operation is required. The requested commit and push are authorized.
- DW-504: the tenant correction path deliberately clears corrective proof and reports missing audit association; the separate global-administrator path belongs to Story 5.7.
- DW-532: Python equality proves structural equality of parsed rows; catalog byte/hash checks are a separate guarantee.
- Scope selection: DW-558 was investigated but excluded before implementation acceptance because its invalid historical baseline cannot satisfy the current CI gate through a bounded documentation correction. Its file and ledger record are restored exactly to entry bytes. DW-564 replaces it: document the existing eventually consistent revocation boundary without claiming an enforcement change. The five selected IDs are DW-504, DW-532, DW-564, DW-578 and DW-596.
- DW-578: ci.yml lists both AppHost.Tests and Server.Tests in the blocking integration tier.
- DW-596: committed Builds 9e6b6ca3e8b4e22e6642d4f0d70d25edbf2db08b supplied 3.120.0 and root b3878f1488d2a4a097bac2e5c233fa07da6eb2cf pinned it. Current pinned Builds 17d0c6ffec5fb3c8d8058e67cb85644460cb5256 selects EventStore 3.126.0. Historical 3.120.0 test/audit evidence remains historical; no new audit acceptance is inferred.
- Historical story guards initially fail at current HEAD due to later independent dependency evolution. Retain original baselines; use explicitly verified historical audit endpoints for the two amended historical stories, without attributing later pointer work to this documentation batch.


## Review Triage Log

Blind Hunter: 40,438 bytes / 1,024 = 39.490 kB; N = min(floor(sqrt(39.490) + 1), 10) = 7. All findings checked against the changed documents, actual authority verifier and executable audit.

1. **low / patch:** Catalog byte wording could imply the intended version bytes never changed. The added historical annotation and ledger/evidence wording now explicitly exclude the intended version substitution; original historical text remains intact.
2. **medium / patch:** Identity-renewal wording could imply old claims override the membership check. The new guidance now says projected absence removes effective server global-administrator standing even while a UI principal retains old claims.
3. **low / patch:** Operator verification lacked observable criteria. Added a deployment checklist row requiring projected absence, renewed identity evidence and refusal of an operation requiring global-administrator authority; tenant-owner-capable operations do not qualify. No runtime result is claimed.
4. **low / patch:** Structured records lacked current verification paths and two retained n/a locations. Added current verification-files and location to all five, preserving every original value under the history prefix.
5. **low / patch:** Excluded paging-history reasoning lacked a reproducible search. The evidence and checker now bind the exact entry revision, three ancestry outcomes and all four reachable file snapshots; the claim is restricted to that inspected history.
6. **medium / patch:** Audit replay overwrote saved evidence. Default mode now compares computed evidence with saved JSON without writing; explicit --write-result regenerates after intentional corrections and --result-path supports a mismatch control.
7. **low / patch:** Whitespace success was attributed RESULT: PASS output. Evidence now distinguishes the three explicit validator results from silent exit-0 whitespace success.

No finding is deferred. Final independent recheck and verification precede completion and push.

Final independent recheck resolves all seven findings and reports no remaining concrete finding. The embedded/executed checker is identical, default replay preserves saved JSON bytes, all six document hashes match and exactly five closures remain: 657 records, 445 done, 212 open across 193 index rows. Source/configuration facts, historical story policy, links, whitespace and pinned commitlint pass. No application build or test run is claimed for this documentation-only batch. Fetched main remains 0/0 before commit. The committed-event CI guard will run before the authorized push.
