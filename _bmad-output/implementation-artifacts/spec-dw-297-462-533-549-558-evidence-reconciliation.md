---
title: 'Reconcile five deferred verification and planning records'
type: 'chore'
created: '2026-10-21'
status: 'done'
route: 'oneshot'
baseline_commit: '0c10ef637171f9ac2639d4b5ac42f513ea9ef133'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Complete DW-297, DW-462, DW-533, DW-549 and DW-558 by reconciling historical verification and planning evidence. Surface the already recorded removal-proof Release command in formal verification; clarify Story 5.4 evidence in its linked report; restore the Epic 5 planning constraints and source citations; bind Story 5.6 reverification to its actual immutable endpoint; and qualify the paging work reverted before Story 5.7 supplied its integration.

Preserve frozen intent, original evidence, failures, statuses, dependency pointers and unrelated records. Leave the invalid legacy Story 5.4 baseline and its separate open records unchanged. Validate the documentation and applicable historical gitlink checks, independently review the changes, update only the five selected ledger dispositions and maintenance coverage, then validate the commit message, commit and push main as requested.

</frozen-after-approval>

## Implementation Notes

- Planning found no unresolved user choice, new API or irreversible implementation step. This is a small documentation repair using the oneshot route; the user explicitly requested commit/push to main.
- Read the shared baseline/Git instructions, project context, architecture, tracked format/build configuration, selected ledger records, source documents, gitlink validators and tests. No application or dependency changes are needed, so application builds and Aspire runs do not validate this documentation scope.
- Code map: spec-2-4b-wp-2a-removal-proof-and-audit-available.md already records its historical Release command/result; story-5-4-re-review-verification-2026-09-30.md already records exact later commands and nine named mutations. Add qualification there without editing the legacy story, whose unresolved baseline still belongs to DW-461/DW-467/DW-478.
- Epic 5 source authorities are planning-artifacts/epics.md (Stories 5.1–5.7), planning-artifacts/architecture.md (McpCli and non-collapse), and docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md. Keep the existing R1–R4 implementation constraints and distinguish them from planning citations.
- spec-5-6-preview-confirm-and-link-a-tenant-correction-2.md has a valid preserved baseline and two historical edits; use its final historical commit and exact table targets. The existing changed-story CI guard must accept the immutable endpoint without a policy change.
- spec-global-admin-projection-paging.md remains in-review with its existing non-ancestor baseline blocker. Its tasks/ACs are outside intent-contract and can be qualified in place; do not silently rewrite that baseline or claim its blocked guard passed.

- Investigation confirmed that editing the paging spec itself would trigger its unresolved baseline gate. Qualify DW-558 in this batch's ordinary evidence report and authoritative ledger instead; preserve that historical artifact byte-for-byte. This stays within the frozen evidence-reconciliation intent and leaves its in-review status and guard hold explicit.

- Implemented the five documentation reconciliations and ledger/index closeout. Both historical endpoint guards and three CI endpoint preflights pass; 77/77 production guard regressions and 49/49 changed-story regressions pass. Five focused wrong-target/declaration/checkpoint controls fail with the expected diagnostics. Structural comparison proves exactly five closures, 657 retained IDs, 460 done/197 open and 181 exact-covering rows, with frozen intent and both blocked historical artifacts unchanged. Initial draft parser-placement failure is preserved in the evidence report.

- Independent review found seven document/provenance improvements. Corrected source precedence and the two Story 5.4 checkpoints, restored concrete grid fields, explicitly separated the approved tenant expiry exception and current global proof gates, and retained the snapshot comparison/control code with exact arguments and observed diagnostics. The checker is an evidence artifact, not a new application test lane, and keeps fixed-snapshot expectations explicit.

- Independent recheck resolves all seven findings and reports no remaining defects in the corrections. The retained verifier exits 0 and all 24 new local Markdown links/anchors resolve. During recheck, unrelated external work moved and dirtied the EventStore checkout; preserve it and exclude its gitlink from this documentation commit. Validate committed snapshots to distinguish that external worktree state from this pointer-free event.
- Repository-pinned @commitlint/cli 21.2.3 accepts the exact full message stored in /tmp/dw-297-462-533-549-558-commit-message.txt with zero problems/warnings; its SHA-256 is c2c2e5656dd60c03fc5babb3f930a13575bc025918c0df674f98bc7e7287abba. Successful CLI output is preserved in /tmp/dw-297-462-533-549-558-commitlint.log. The user authorized the commit and push to main.

## Review Triage Log

Blind Hunter: 36.137 kB; `N = min(floor(sqrt(36.137) + 1), 10) = 7`.

| Finding | Verdict / disposition | Evidence and correction |
| --- | --- | --- |
| 1. Seven-field receipt citation conflicts with older eight-field spec | medium / patch | Story 5.3 explicitly restricts rendered inputs to seven fields. Cite it directly and qualify the older spec's optional eighth reference. |
| 2. Story 5.4 original guard PASS conflated with later command | medium / patch | Original retained review names spec-2 at d728ff46; the report command names spec-3 at 55fc6f91. Record both checkpoints separately, preserving the legacy failure. |
| 3. Explicit audit-grid fields still omitted | low / patch | Story 5.1 and the earlier context enumerate timestamp, actor, outcome, category, freshness/projection context and reference. Restore that list with its planning citation. |
| 4. Separate global correction confirmation rule omitted | medium / patch | Planning requires postcondition plus provenance; current approved Story 5.7 and GlobalAdministratorCorrectionSnapshot further require verified committed progress. State the stricter current global rule explicitly without weakening it to the older shorthand. |
| 5. Planning precedence ambiguous for tenant retention expiry | medium / patch | Approved Story 5.6 explicitly supersedes indefinite tenant admission with bounded expiry and retained recovery. Identify the tenant-only exception and preserve global terminal-evidence admission. |
| 6. CI preflight/control claims lack exact retained invocation | medium / patch | Retain the executable checker and per-control arguments, replacement values, expected exits and observed diagnostics. |
| 7. Conservation/coverage claims lack retained comparison code | medium / patch | The same missing evidence-source root cause is resolved by retaining record-boundary parsing, grouped-ID expansion, exact old/new checks and historical/frozen preservation code. |
