---
title: 'Five deferred tenant read-safety decisions'
type: 'bugfix'
created: '2026-10-28'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'd490461f4d6baeff4b2373ef9c54edfcd5e363a2'
context:
  - AGENTS.md
  - references/Hexalith.AI.Tools/hexalith-llm-instructions.md
  - references/Hexalith.AI.Tools/hexalith-ux-instructions.md
  - references/Hexalith.AI.Tools/hexalith-git-instructions.md
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Resolve DW-117/118/127/599/618: nonfatal cursor failures, argument-contract asymmetry, hidden search windows and configuration disclosure. Implement approved policies, preserve original findings, verify and push main.

## Boundaries & Constraints

Always preserve caller/authorization binding, authoritative hydration, cancellation, disposal/stale-load guards, safe diagnostics and named recovery. Continuations stay protected, numeric and server-held. Fatal failures propagate. Preserve dependency pins and unrelated records.

Never expose raw totals/hidden identities, weaken authorization or claim snapshot isolation, assistive-technology/release qualification. Preserve historical frozen stories; record approved supersession separately.

## I/O & Edge-Case Matrix

| State | Required outcome |
| --- | --- |
| Nonfatal provider/argument defect | Apply approved containment policy with safe recovery and diagnostic classification. |
| Hidden/missing/malformed search candidates | Derive displayed pagination under the approved privacy/completeness policy. |
| Invalid/old search continuation | Recover to page one without exposing continuation material. |
| Scan budget or hydration failure | Report the approved unavailable outcome; never claim a falsely final search page. |
| Missing/malformed configuration policy | Apply the explicitly selected API/UI boundary. |
| Caller cancellation, scope change, disposal | Retire obsolete results and retain no unauthorized state. |

## Approved Decisions (2026-10-28)

The user approved all recommended choices and continuing implementation, verification and push to main.

- DW-117/118: contain nonfatal provider/programming failures at the workspace Error/Retry boundary with safe diagnostics. Preserve fatal propagation and deliberate gateway null-versus-empty classification; qualify nonblank wrapper inputs. DW-118 closes as an approved contract decision, not a claimed reachable null-input production fix.
- DW-127/618: approve BFF backfill/lookahead so rendered pagination uses authorized results only. Each load examines at most 1,000 raw positions, including replay/lookahead; exhaustion discards incomplete search results and uses the existing labelled authorized-list fallback. Privacy covers rendered rows/pager, excluding timing, work, fallback-mode independence and completeness beyond budget. This supersedes Story 1.9's no-backfill rule for this work.
- DW-599: FR-6 remains server-side UI display policy. Preserve the full configuration API for authorized consumers, and qualify/document the boundary.

</frozen-after-approval>

## Code Map

Production paths below are under `src/Hexalith.Tenants.UI/`; matching tests under `tests/Hexalith.Tenants.UI.Tests/`.

- `Services/Gateways/TenantQueryGateway.cs`: search hydration, HasMore, safe fallback/concurrency.
- `Services/Gateways/TenantSearchCursor{Codec,Position,Scopes}.cs`: numeric protected continuation and caller/query binding.
- `Components/Pages/TenantsWorkspace.razor`: load, disposal and recovery ownership.
- `Services/Configuration/TenantConfiguration{ReadPolicyProvider,SafeComposer}.cs`: fail-closed display policy.
- `src/Hexalith.Tenants/Queries/Handlers/GetTenantQueryHandler.cs`: authorized full configuration contract; tests in `tests/Hexalith.Tenants.Server.Tests/Queries/`.

## Tasks & Acceptance

- [x] This file: user decisions recorded and frozen after approval.
- [x] `TenantsWorkspace.razor`, `TenantsWorkspaceTests.cs`: implement selected containment with retry, safe diagnostics, scope/disposal races and fatal-error controls.
- [x] Cursor files and `Services/Gateways/TenantSearchCursorTests.cs`: qualify nonblank inputs; version continuation as window start plus consumed returned-result count. Replay fixed-size windows without confusing filtered ordinals with raw offsets.
- [x] `TenantQueryGateway.cs`, `TenantQueryGatewayTests.cs`: implement approved bounded scan/lookahead; cover hidden prefixes/suffixes, sparse results, overflow/replay, budgets, cancellation, invalid cursors and identical visible paging for equal authorized results. Preserve existing sort semantics.
- [x] Configuration files/tests: qualify selected boundary, exact-prefix/display-safe selection and administrator rules.
- [x] `docs/maintenance-backlog.md`, `_bmad-output/implementation-artifacts/deferred-work.md`, `_bmad-output/project-context.md`, `_bmad-output/planning-artifacts/architecture.md`: record approved supersession and only evidence-supported selected dispositions; preserve history.

Acceptance: Given approved policies, when matrix cases execute, then tests prove the selected outcomes/limits. Given unchanged query/index within budget, when paging through hidden/malformed windows, then authorized results remain reachable without raw-total paging disclosure. Given unrelated records, when reconciling, then their bodies remain unchanged.

## Implementation Notes

The user approved the recommended decisions and spec on 2026-10-28; status passed ready-for-dev before implementation. The implementation handoff ends with tested changes and a report. The root agent owns subsequent workflow review, commit and push. Do not perform remote operations during implementation.

Clean baseline: `d490461f4d6baeff4b2373ef9c54edfcd5e363a2`. Three read-only agents confirmed policy gaps. No application edits/tests yet. Footprint: gateway/workspace/cursor, focused tests and policy/evidence documents. No migration/dependency update. Index mutations retain offset paging's existing limits.

## Spec Change Log

## Review Triage Log

### Review pass 1 — 2026-10-28

| Finding | Verdict | Evidence and route |
| --- | --- | --- |
| Blind 1: fatal wrappers are contained by gateway filters | high | IsSurfacingDefect examines outer types only; allowed wrappers can bypass the workspace fatal guard. Patch existing classification to recognize fatal inners. |
| Blind 2: WhenAll hides a sibling fatal error | high | Await exposes one fault although the combined task retains every hydration exception. Patch the existing batch await to preserve a fatal aggregate. |
| Blind 3: caller retirement replaces a fatal read failure | medium | CallerBoundTenantsRestQueryClient.ReadAsync finally and the outer caller-transition catch already existed unchanged at entry HEAD; concurrent retirement can replace dependency exceptions. Defer this pre-existing cancellation/authority precedence case. |
| Blind 4: duplicate tenant rows recur across pages | false | The adopted curated adapter upserts one deterministic index key per tenant; its real CuratedUpserts contract and stable-key ordering prove unique tenant candidates for an unchanged curated index. Repeated tenant-source arrays violate that consumed contract. |
| Blind 5: sequential backfill lacks a whole-load deadline | maybe-false | Position budget, caller cancellation and per-request timeouts are real bounds; no unacceptable elapsed-latency threshold or slow-upstream reproduction is established. Defer as unverified medium pending controlled latency evidence and an owning deadline decision. |
| Blind 6: null gateway snapshots escape containment | false | The production gateway returns nonnull factories/bound records; its bind operation faults inside the awaited call on a null, which is already contained. A nonconforming test replacement returning null does not establish a reachable production path. |
| Blind 7: diagnostic logging can prevent recovery | medium | ILogger providers may throw; the newly added diagnostic call runs before Error/Retry assignment. Patch the diagnostic path to contain nonfatal logging faults while preserving fatal ones. |
| Blind 8: browser acceptance ignores command attempts | medium | The new branch bypasses old dialog command-count assertions. Patch zero-command assertions and the read-safety closure predicate. |
| Blind 9: package-governance tests were not run | false | Those tests belong to independent already-committed 6deb5d5dbaad4e26405910b347d45b82deae1057 and are absent from this batch's staged diff. The evidence explicitly distinguishes that change and claims no new Contracts qualification. |
| Blind 10: stale test names/empty continuations obscure reachability | low | Several updated cases assert terminal scans under names claiming continued paging. Directly correct those labels/comments and retain an explicit later-authorized status-match control. Patch. |
| Edge 1: wrapped hydration fatal becomes fallback | high | Same verified gateway filter defect as Blind 1; group and patch once. |
| Edge 2: concurrent nonfatal/fatal hydration loses fatal | high | Same verified WhenAll defect as Blind 2; group and patch once. |
| Edge 3: duplicate suppression depends on hidden alignment | false | The counterexample requires repeated tenant candidates in an unchanged curated index; deterministic per-aggregate upserts and the adopted real-Redis uniqueness contract exclude that input. No broader corrupt-index guarantee is claimed. |
| Verification 1: ordinary fatal wrappers lack regression coverage | medium | Direct/aggregate cases leave the ordinary InnerException recursion untested. Extend the existing fatal theory with ordinary/nested wrappers and preserve identity assertions. Patch. |
| Root 1: new cursor code uses compact braces | low | New declarations/control blocks should follow the tracked Allman setting. Correct new/touched code formatting without broad unrelated cleanup. Patch. |


## Verification

After approval, capture Aspire baseline; build affected Debug/source projects. Run focused built xUnit assemblies, then affected UI/Server suites and authenticated browser search/retry. Record exact commands/results and blockers. Check ledger preservation, whitespace and repository guards. Validate the exact message with pinned commitlint, fetch/check divergence, commit and push main.


### Implementation handoff — 2026-10-28

All implementation tasks are complete. Final Debug/source builds have zero warnings/errors; focused gateway/cursor/config/workspace tests pass 1,129/1,129, maintained full UI 7,969/7,969, maintained Server 1,586/1,586, and authenticated rebuilt browser search/Next/Previous/Error-Retry passes four cases with zero circuit errors/commands. Environment-only pins resolve the reproduced McpCli source identity/floor blockers without changing dependencies or gates. All 657 ledger IDs remain; 649 done/8 open across seven exact-cover rows, with all 652 unselected bodies and selected original fields preserved. Frozen stories and the approved block remain unchanged. Story gitlinks, whitespace and browser syntax pass. See [exact commands, failed controls, matrix coverage and limits](dw-117-118-127-599-618-validation-evidence.md). Workflow review, commit and push remain with the root agent.

Root audit: read the staged baseline diff including the preserved independent build commit; all matrix rows are covered by the passing full/focused runs. Explicitly retained ignored validation logs, compressed the raw child-restore failure, verified every evidence link, and verified production source/validator bytes against the passing browser packet after staging. Review patch verification will supersede the initial handoff results.

Final root verification: all review patches inspected; full UI 7,986/7,986, retained unchanged Server 1,586/1,586, review focus 1,034/1,034 and a fresh rebuilt browser 4/4 pass. Every matrix row is covered; no high findings remain. Two separate unnumbered follow-ups are retained with their exact limits. Selected/unselected ledger preservation and pinned commitlint pass. The user explicitly authorized pushing main; this overrides the workflow default against automatic pushes.
