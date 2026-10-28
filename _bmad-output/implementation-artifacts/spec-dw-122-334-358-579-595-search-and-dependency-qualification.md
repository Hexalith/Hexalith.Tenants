---
title: 'Qualify search paging and adopted dependencies'
type: 'bugfix'
created: '2026-10-28'
status: 'done'
route: 'dispatch'
review_loop_iteration: 1
baseline_commit: 'db83309146816602033320270f10b1cea28d3100'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** DW-122 lacks stable search-page boundaries; DW-334/DW-358 lack live Memories secret-provider evidence. DW-579/DW-595 retain historical dependency blockers now superseded by current upstream evidence.

**Approach:** Complete these five records through owning Memories search changes, package-mode local deployment qualification, and exact-revision audit/CI reconciliation. Independently review and validate the batch, then commit and push owning `main` branches and necessary root pointers.

## Boundaries & Constraints

**Always:** Preserve BM25 relevance, raw offsets, authorization, opaque scoped cursors, historical findings and failed evidence. Guarantee deterministic ordering across windows for an unchanged query/index, including equal scores. Select the checked-in `secretstores.local.file` provider for local deployment qualification. Preserve broader runtime/release holds.

**Never:** Put index material in Tenants cursors, weaken validators, accept the failed historical audit retrospectively, count source models as package evidence, or claim cancelled tests passed. Production providers, package publication and unrelated policy decisions remain outside scope. Close records only after their evidence passes.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected behavior | Error handling |
| --- | --- | --- | --- |
| Paging | Unchanged index; equal/distinct scores | Stable relevance order without cross-window duplicates or omissions | Preserve query/cancellation contracts |
| Invalid documents | Missing required fields | Consume raw positions; retain stable later windows and totals | Existing filtering |
| Deployment | Package-mode AppHost; selected local provider | Actual component binding and healthy Memories/sidecar initialization | Retain startup failures; model-only proof is insufficient |
| Reconciliation | Current audit and hosted CI | Current evidence passes independently of failed history | Preserve historical rejection and cancelled integration limits |

</frozen-after-approval>

## Code Map

- `references/Hexalith.Memories/src/Hexalith.Memories.Server/Search/SyntacticSearchService.cs`: BM25 query construction and offset windows; reuse validation, filtering and error classification.
- `references/Hexalith.Memories/tests/Hexalith.Memories.IntegrationTests/Search/SyntacticSearchIntegrationTests.cs`: pagination tests currently avoid tied scores; extend Redis-backed contracts.
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`: preserve per-window hydration/deduplication and cursor boundaries.
- `tests/Hexalith.Tenants.AppHost.Tests/MemoriesSecretStoreResourceGraphTests.cs`: actual assembly package-mode metadata and component relationships; resources are not started.
- `src/Hexalith.Tenants.AppHost/DaprComponents/secretstore.memories.yaml`: selected local provider.
- `references/Hexalith.Builds/Tools/validate-package-version-audit.ps1`: supported current-audit validator.

## Tasks & Acceptance

**Execution:**
- [x] `references/Hexalith.Memories/src/Hexalith.Memories.Server/Search/SyntacticSearchService.cs`: implement deterministic equal-score boundaries using existing Redis infrastructure.
- [x] `references/Hexalith.Memories/tests/Hexalith.Memories.IntegrationTests/Search/SyntacticSearchIntegrationTests.cs`: prove adjacent/repeated-window uniqueness, relevance and invalid-document semantics with real Redis; demonstrate the original implementation fails the new contract.
- [x] `tests/Hexalith.Tenants.AppHost.Tests/MemoriesSecretStoreResourceGraphTests.cs`: run the existing actual-package model check, then qualify isolated live startup through Aspire. Capture provider binding and initialization without secret values; repair discovered ownership-local defects within scope.
- [x] `_bmad-output/implementation-artifacts/dw-122-334-358-579-595-validation-evidence.md`: record exact revisions, commands, results, independent review and limits for all five records.
- [x] `_bmad-output/implementation-artifacts/deferred-work.md` and `docs/maintenance-backlog.md`: append verification, preserve historical fields, close only proven selected records and recompute exact coverage/counts.
- [x] Owning repositories: validate full commit messages with pinned commitlint, inspect remote divergence, commit/push `main` and advance only required declared root pointers.

**Acceptance Criteria:**
- Given an unchanged index with tied scores, when callers walk raw windows, then every eligible identity appears once in deterministic relevance order, preserving malformed-document positions.
- Given the selected provider and package-mode AppHost, when deployment starts, then live evidence proves Memories/component initialization independently of model assertions.
- Given current Builds, when its supported validator runs, then it passes while the October 10 audit remains historically rejected.
- Given adopted EventStore, when hosted results are reconciled, then successful unit/contracts jobs are distinguished from cancelled integration qualification.
- Given passing evidence and independent review, when closure is recorded, then all 657 IDs remain, only the selected five change disposition, and validated commits reach owning `main` branches.

## Implementation Notes

Investigation started from clean Tenants `bacce171b1b58a53306d867ff9f9e6e96f15c15a`; implementation has not begun. Builds `2574bf2f3da5367d8801f0d7661d66155e5d90b0` passes its supported audit validator: 307 packages, 151 families, one source. Historical missing origins remain unavailable. EventStore `36c78f3435d2d492a73f1b0707cf865c8735d740` has successful CI run `40644656859`; Integration run `40644656405` was cancelled. DW-137 remains deferred under the October 27 shared-identity decision; policy-dependent records are excluded.

## Spec Change Log


2026-10-28, iteration 1: Independent review demonstrated partial aggregation under default timeout-return policy and differing configured search/aggregate result caps. The implementation plan now requires completeness-aware bounded execution under RESP2/RESP3 and configured deep-page limits, without unbounded timeouts or server-global mutations. KEEP: deterministic BM25/key ordering before raw offsets; graph filters before selection; existing highlighting, totals, stale logging and hydration; all passing provider/package checks, historical evidence and ledger preservation. Restore whitespace-only caseId normalization. Extend exhausted/malformed protocol coverage and exercise curated tenant identities. The first-pass implementation patch and evidence remain retained; production/test changes are re-derived from the original owning baseline.

## Review Triage Log


Final review, iteration 1:

| Finding | Verdict | Evidence and route |
| --- | --- | --- |
| Final blind 1: incomplete exception mapping | medium | Base RedisException bypasses existing endpoint connection/timeout/transient-server handlers, yielding HTTP 500. Use recognized typed backend failures without changing API responses; patch. |
| Final blind 2: hydrated raw identities | medium | Advertised total can equal selection while a configured SEARCH cap omits raw rows. Compare returned raw identities before required-field filtering; patch. |
| Final blind 3: hydration-only timeout | low | Current forced timeout rejects during selection, so a future hydration bypass would lack a site-specific control. Add a focused hydration-only refusal case; patch. |
| Final blind 4: disappearing selected hash | medium | The hydration-count guard handles the race but has no arranged mutation regression. Group with the preverified gap below; patch. |
| Final blind 5: missing profile/warning tests | low | Pinned real protocol always provides completion metadata; missing-metadata branches fail loudly. Synthetic guard-mirroring cases add complexity without an ordinary reachable defect, so reject. |
| Final blind 6: count-only RESP3 | low | New profiled index-presence code needs the ordinary empty-query/empty-graph RESP3 path. Add focused result assertions; patch. |
| Final blind 7: fixed timeout workload | low | Deadline establishment is hardware-sensitive. Bound workload/probe escalation while retaining proof of a real partial response; patch. |
| Final blind 8: large-corpus normal path | low | Current large corpus proves refusal only. Add/report successful normal bounded search on the same corpus alongside the retained existing latency smoke; patch. |
| Final blind 9: removed native endpoint reproduction | low | Published native commands require removed infrastructure. Supply and execute self-contained standard-fixture commands; documentation patch. |
| Final blind 10: ledger verifier publication | low | Preservation JSON lacks its repeatable computation in the tree. Retain a read-only verifier and exact invocation; documentation/tooling evidence patch. |
| Final blind 11: initial spec notes | low | Append-only investigation notes describe their earlier phase; current status, completed tasks and later dated evidence establish present state. Reject the spec-only editorial change under workflow rules. |
| Final verification 1: hydration mismatch regression | medium | Preverified: removing the hydration-count guard escapes all arranged current tests. Add the proposed real deletion-through-forwarder control and persisted deletion assertion; patch. |

The final edge layer reports no findings. The verification gap and final blind hydration-race finding share one regression control; every finding remains individually recorded above. No intent or public API changes are needed for these direct patches.


| Finding | Verdict | Evidence and route |
| --- | --- | --- |
| Blind 1: partial timeout results | medium | Pinned-image reproduction proves partial aggregate totals under RETURN; RESP2 omits warnings. The new completeness claim needs an execution-level fix. bad_spec. |
| Blind 2: whitespace caseId | medium | Old unscoped mapper normalized whitespace to null; the new raw mapper preserves it. Direct compatibility patch; included in re-derivation. |
| Blind 3: ignored log artifacts | medium | Root ignores *.log, so exclude-standard diff omitted retained evidence. Explicitly include owned logs in final review/publication; patch. |
| Blind 4: differing aggregate/search caps | medium | Raising MAXSEARCHRESULTS above MAXAGGREGATERESULTS makes new valid deep windows truncate relative to prior search. Requires qualified limit handling; bad_spec. |
| Blind 5: stale evidence handoff prose | low | Final paragraph still says ledger unchanged although the coordinating pass closes five. Direct evidence-text patch after review. |
| Blind 6: legacy helper assertions | low | Helpers no longer have production callers, but executed real integration tests cover the new path. Broad migration/removal is unnecessary for ordinary users and exceeds a direct correction; reject. |
| Blind 7: exhausted protocol windows | low | New 23-document/five-step loop stops at 20. Add explicit exhausted calls for graph/plain RESP2/RESP3; direct test patch. |
| Blind 8: combined malformed protocols | low | Existing malformed cases exercise plain RESP2 only. Extend the theory to graph/RESP3 combinations; direct test patch. |
| Blind 9: motivating original control | false | Evidence explicitly records original failures at deterministic tie-order assertions, without claiming observed duplicate pages. Final adjacent identity coverage proves the ordered-window contract; no false negative-control claim exists. |
| Blind 10: actual tenant identity contract | false | RedisSearchIndexMaintenanceAdapter upserts one deterministic key per aggregate and preserves sourceUri; Tenants consumes this curated index. Arbitrary duplicate sourceUri ingestion is outside that established invariant. Add an owning curated round-trip case to make the existing proof explicit. |
| Edge 1: timeout policy | medium | Same reproduced partial-window defect as Blind 1, including silent RESP2 partial results. Group with bad_spec execution completeness. |
| Edge 2: whitespace caseId | medium | Same reachable unscoped response regression as Blind 2. Group with compatibility patch. |

## Verification

Build and run Memories integration search contracts in Debug/source mode with real Redis. Build the AppHost test project in actual package mode and invoke its built xUnit assembly with a single-dash class filter; retain mode metadata. Use the exact Tenants AppHost and Aspire isolation for live resource/provider evidence. From Builds, run `pwsh -NoProfile -File Tools/validate-package-version-audit.ps1`, requiring exit 0. Query exact-revision EventStore workflow/job conclusions with `gh`. Run ledger/index preservation checks, `git diff --check`, independent workflow review and pinned commitlint before pushing. Retain any broad-gate blocker separately from focused passing checks.

Approval recorded on 2026-10-28: user approved and continued. Before implementation, main fast-forwarded to the already-fetched remote tip db83309146816602033320270f10b1cea28d3100; the frozen intent remains unchanged.

The remote fast-forward adopted EventStore `717a06e16c3afd81b904eaf4de646b74b13262d2`. Its declared root submodule was synchronized without nested updates; final qualification must use this adopted revision, retaining earlier exact-pin evidence as history.

Implementation/matrix audit: real Redis ties, malformed raw positions, graph exclusions and both protocols pass 43 integration cases; Search unit 271 and package AppHost model 1 pass. Live selected-provider startup/health passes. Current audit and exact-pin CI qualification pass. All selected ledger changes preserve 652 unselected bodies; publication remains scheduled after independent review.


Re-derivation requirements: qualify timeout-return behavior on the pinned image for RESP2 and RESP3; incomplete ordered selection must fail rather than return a successful partial window. Preserve bounded execution and default supported deployment behavior without changing server-global policy. Handle differing MAXSEARCHRESULTS/MAXAGGREGATERESULTS so an otherwise supported deep window cannot silently truncate. Add real regression controls for both findings, actual SearchAsync whitespace caseId normalization, exhausted windows, and graph/protocol malformed-entry combinations. Include a curated adapter upsert/sourceUri paging contract. Do not broaden unrelated suites or public APIs.

First-pass production/test patch for positive preservation: `/tmp/tenants-dw122-first-pass.patch`. Root bookkeeping, existing provider/audit evidence and the owning companion spec are retained during re-derivation; the two upstream source/test tasks are reopened.

Iteration-1 implementation decision: bounded profile execution exposes timeout warnings in both protocols. A raw selection shorter than the expected window from its complete total must produce an explicit Redis query failure rather than partial success when aggregation and search caps differ. This satisfies the recorded no-silent-truncation requirement without server-global changes, unlimited timeouts, new public APIs or precision-sensitive score keyset paging.


Iteration-1 final acceptance evidence: 66/66 real integration, 271/271 Search unit, 1/1 actual package AppHost model; all focused builds have zero warnings/errors. Live selected-provider startup/health passes with final source/loaded-binary binding. The fixture's established timeout policy is preserved. All ignored owned logs are included in the final review snapshot and will be explicitly staged for publication.

## File List

- `references/Hexalith.Memories`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `docs/maintenance-backlog.md`
- `_bmad-output/implementation-artifacts/spec-dw-122-334-358-579-595-search-and-dependency-qualification.md`
- `_bmad-output/implementation-artifacts/dw-122-334-358-579-595-validation-evidence.md`

The Memories pointer advances to the reviewed owning search fix. The evidence directory contains
its executed qualification and historical attempts; other root dependency pointers remain at baseline.

Owning main advanced independently to bd248eb09170bfed14bbda61320ddd9c6797e9bf during review. Its three commits touch release tooling/evidence only; no changed search source/test path overlaps. The owning checkout fast-forwarded without altering this reviewed search diff or its recorded initial baseline.


2026-10-28 final direct-patch completion: scoped blind and verification rechecks report no unresolved
issue; final edge review is clear. Parent verification passes 82 integration, 296 Search/endpoint,
and one package-mode model case, with zero skips and clean builds. The final package-dependency
server/live capture binds loaded binaries to source dda55f1f015a168f2d23682f0e04a8dd7d1329ca4ad5090256598fe06f6f2f14.
Owned resources are stopped. The preserved source/binary evidence supersedes earlier-pass hashes.
All kept findings are fixed; no new deferred records were added. Owning main publication follows
this accepted, validated tree under the user's explicit push instruction.

## Reference targets

| Reference | Baseline | Target |
| --- | --- | --- |
| `references/Hexalith.Memories` | `72c36c7113c750c8b809f9d72b945a2a8c2d4555` | `35ef40e3fdcc7a490e04cf49e6a532fd5bf32285` |

The owning source commit is published on main. Its first push was rejected after another independent tooling commit advanced main; rebase onto 6249e6bafc577934be959a3f248fbb27c8951ffc preserved all reviewed source/test paths, and the updated scope/readiness and pinned commitlint gates pass. Root publication uses the declared target above.
