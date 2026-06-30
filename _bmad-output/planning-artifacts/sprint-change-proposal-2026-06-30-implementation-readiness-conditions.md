---
created: 2026-06-30T00:00:00+02:00
trigger_report: _bmad-output/planning-artifacts/implementation-readiness-report-2026-06-30.md
change_mode: Batch
scope_classification: Moderate
recommended_path: Direct Adjustment
approval_status: approved
approved_by: Administrator
approved_at: 2026-06-30
---

# Sprint Change Proposal - Implementation Readiness Conditions

- **Date:** 2026-06-30
- **Project:** tenants
- **Author:** Codex using BMAD Correct Course
- **Trigger type:** Planning/control correction after implementation readiness assessment
- **Scope classification:** Moderate
- **Change mode:** Batch
- **Path forward:** Option 1 - Direct Adjustment

## 1. Issue Summary

The 2026-06-30 implementation readiness assessment reports the planning set as **READY for continued story implementation, with explicit conditions**. It found no critical blockers, but it identified 12 attention items:

- 2 major planning/control issues.
- 5 UX/architecture evidence or deferred-capability warnings.
- 5 minor story-quality or handoff concerns.

The readiness result is not a replan trigger. The issue requiring course correction is control hygiene: implementation can continue only if the conditions are made explicit in the handoff artifacts, and if stale planning statements are reconciled with current sprint state.

Current sprint tracking already marks all epics, Story 3.5, Story 5.7, Story 5.8, and related retrospective action items as `done` in `_bmad-output/implementation-artifacts/sprint-status.yaml`. Therefore, the proposal must not create duplicate work for already-completed gates. It should instead make the planning docs self-contained and update stale "still gated by Story 5.7" wording to reflect the completed Story 5.7 verification while preserving live deferred risks.

## 2. Impact Analysis

### Epic Impact

- **Epic 3:** impacted only by planning-index completeness. Story 3.5 already exists as a completed defect-fix artifact and is referenced in `epics.md`, but the `epics.md` block should link directly to the full story artifact and test evidence.
- **Epic 5:** impacted only by gate wording. Story 5.7 is now completed and should be referenced as the verification record for enabled global-administrator correction.
- **Other epics:** no scope change.

No epic is invalidated. No new epic is required. No epic order or priority change is needed.

### Story Impact

- **Story 3.5:** no implementation change. Add a clearer planning-index handoff link to `_bmad-output/implementation-artifacts/3-5-tenant-query-gateway-rest-routing.md` and test evidence.
- **Story 5.5:** no implementation change. Preserve the boundary that Story 5.5 may model/start correction intent, but enabled global-administrator correction depends on Story 5.7 evidence.
- **Story 5.7:** no implementation change. Update planning/UX notes to say the Story 5.7 gate is satisfied, with residual deferred follow-ups still tracked.
- **Future stories:** keep per-story accessibility, localization, responsive, documentation/reference, support-safety, stable selector, contrast, forced-colors, and live-region evidence as mandatory readiness/done gates.

### Artifact Conflicts

- **PRD:** scope remains valid, but the compensating recovery note still says any enabled global-administrator correction path needs Story 5.7 verification. That is now stale because Story 5.7 is complete.
- **PRD addendum:** readiness text should stop listing resolved `FC-TBL` decisioning as a remaining gate and should distinguish delivered Tenants fallbacks from reusable FrontComposer extraction.
- **Epics:** Story 3.5 is present but not fully self-contained for handoff because the block lacks direct links to the full story/evidence artifact.
- **UX EXPERIENCE:** lines describing audit-based global-administrator correction as still Story 5.7-gated should be changed to "verified by Story 5.7" while retaining deferred limitations.
- **Architecture:** no change needed. The selected architecture spine already supports the direct-adjustment path.
- **Deferred work ledger:** already contains live residuals, including global-administrator projection pagination and tenant-domain correction proof/freshness follow-ups. No ledger rewrite is required unless the docs above need a pointer to those residuals.
- **Sprint status:** no status mutation is needed because the affected story/action records are already done.

### Technical Impact

No code, infrastructure, package, CI/CD, or deployment change is proposed by this course correction.

The technical boundary remains:

- Missing shared UI capability belongs in `Hexalith.FrontComposer`, not Tenants.
- EventStore/read-model freshness and cursor primitives remain shared platform concerns.
- Tenants continues to own tenant-domain screen composition, support-safe copy, and domain-specific fallback behavior.

## 3. Recommended Approach

**Choose Option 1 - Direct Adjustment.**

This is a documentation and handoff correction inside the current plan. It keeps implementation moving without rollback or MVP scope review.

- **Effort:** Low to medium. A few planning/UX doc edits.
- **Risk:** Low. The proposal only aligns existing artifacts with completed story evidence and existing deferred-work records.
- **Timeline impact:** Negligible.
- **Rollback:** Not viable or useful. Completed stories should not be reverted.
- **MVP review:** Not required. The readiness assessment found 100% FR coverage and no PRD/UX/architecture blocker.

## 4. Detailed Change Proposals

### A. Epics - Make Story 3.5 Self-Contained

**Artifact:** `_bmad-output/planning-artifacts/epics.md`

**Section:** Epic 3 completed defect-fix record

**OLD:**

```markdown
**Completed defect-fix record:** Story 3.5, `Tenant Query Gateway REST Routing`, is a completed Epic 3
defect-fix story created by Correct Course. It is not part of the original FR15/FR16/FR17 feature
list, but it is part of Epic 3 readiness because it retired the failed projection-actor read path and
restored REST-backed Tenants reads with freshness/ETag behavior.
```

**NEW:**

```markdown
**Completed defect-fix record:** Story 3.5, `Tenant Query Gateway REST Routing`, is a completed Epic 3
defect-fix story created by Correct Course. It is not part of the original FR15/FR16/FR17 feature
list, but it is part of Epic 3 readiness because it retired the failed projection-actor read path and
restored REST-backed Tenants reads with freshness/ETag behavior.

Implementation and evidence live in `_bmad-output/implementation-artifacts/3-5-tenant-query-gateway-rest-routing.md`
and `_bmad-output/implementation-artifacts/tests/test-summary.md`; sprint tracking records
`3-5-tenant-query-gateway-rest-routing: done` in `_bmad-output/implementation-artifacts/sprint-status.yaml`.
```

**Rationale:** The readiness report's Story 3.5 concern is a handoff completeness issue, not a missing story. Direct links make `epics.md` self-contained enough for future implementers without rewriting the completed story.

### B. PRD - Update Compensating Recovery Gate State

**Artifact:** `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md`

**Section:** 7.9 Compensating Recovery

**OLD:**

```markdown
*Post-Epic 5 / Epic 4 note: compensating recovery (FR-24, FR-25) and evidence-receipt assembly (FR-22) now have completed Epic 5 implementation evidence (`epics.md` Stories 5.3, 5.5, and 5.6 plus the matching story records). Tenant-domain correction preview, submission, projection confirmation, and proof linking are implemented through existing BFF gateways. Epic 4 Stories 4.3 and 4.4 now provide global-administrator grant/remove command support; any enabled global-administrator correction path still needs story-specific verification that it selects the fixed `global-administrators` command scope and preserves last-admin safety.* `[NOTE FOR PM]`
```

**NEW:**

```markdown
*Post-Epic 5 / Epic 4 / Story 5.7 note: compensating recovery (FR-24, FR-25) and evidence-receipt assembly (FR-22) have completed Epic 5 implementation evidence (`epics.md` Stories 5.3, 5.5, and 5.6 plus the matching story records). Tenant-domain correction preview, submission, projection confirmation, and proof linking are implemented through existing BFF gateways. Epic 4 Stories 4.3 and 4.4 provide global-administrator grant/remove command support, and Story 5.7 verifies the enabled global-administrator correction path uses the fixed `global-administrators` command scope and preserves last-admin safety. Residual follow-ups such as global-administrator projection pagination beyond the first page and story-specific routing assertions remain tracked in `_bmad-output/implementation-artifacts/deferred-work.md`; they do not re-block the completed Story 5.7 gate.* `[NOTE FOR PM]`
```

**Rationale:** The old text was correct before Story 5.7 completed. It is now stale and can cause a future implementer to treat completed verification as still missing.

### C. PRD Addendum - Clarify Remaining Gates

**Artifact:** `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md`

**Section:** B. FrontComposer dependency readiness

**OLD:**

```markdown
A story promotes to `ready` only when its `blockedBy` set empties or a **Product/UX-approved** fallback is recorded. The three interim fallbacks (`FC-AUD`/`FC-CNS`/`FC-CNC`) are approved (2026-06-03 -- see the [Fallback Approval Record](./../../fallback-approval-record-2026-06-03.md)); Story 1.0 confirms `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-A11Y`, `FC-L10N`, and `FC-DOC` (2026-06-05). The 2026-06-06 correction reclassifies FR15 as a high-impact reversible lifecycle control eligible under approved command and preview fallbacks. FR19 was previously blocked, but Epic 4 Stories 4.3 and 4.4 now provide implemented grant/remove global-administrator command flows with fixed-scope routing, one-at-a-time locking, projection confirmation, and last-admin safety. Hard destructive tenant deletion remains out of scope for this phase and belongs to a future independent administrators-only CLI tool. Remaining gates are story-specific evidence, `FC-TBL` decisioning, `FC-TOK` fallback discipline, and audit/proof evidence readiness. See `tenants-ui-phase-2-story-backlog.md` for historical per-row `blockedBy`; `sprint-status.yaml` and `epics.md` are the current implementation handoff source after the 2026-06-05 correction.
```

**NEW:**

```markdown
A story promotes to `ready` only when its `blockedBy` set empties or a **Product/UX-approved** fallback is recorded. The three interim fallbacks (`FC-AUD`/`FC-CNS`/`FC-CNC`) are approved (2026-06-03 -- see the [Fallback Approval Record](./../../fallback-approval-record-2026-06-03.md)); Story 1.0 confirms `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-A11Y`, `FC-L10N`, and `FC-DOC` (2026-06-05). Story 1.2 resolves the Tenants `FC-TBL` boundary with a Tenants-specific `TenantDataGrid`; reusable cursor/pinning/list-state capability remains FrontComposer-owned enhancement work. The 2026-06-06 correction reclassifies FR15 as a high-impact reversible lifecycle control eligible under approved command and preview fallbacks. FR19 was previously blocked, but Epic 4 Stories 4.3 and 4.4 now provide implemented grant/remove global-administrator command flows with fixed-scope routing, one-at-a-time locking, projection confirmation, and last-admin safety; Story 5.7 verifies the audit-based global-administrator correction path. Hard destructive tenant deletion remains out of scope for this phase and belongs to a future independent administrators-only CLI tool. Remaining gates are story-specific evidence, `FC-TOK` fallback discipline, reusable FrontComposer extraction for `FC-AUD`/`FC-CNS`, and live deferred audit/proof follow-ups tracked in `_bmad-output/implementation-artifacts/deferred-work.md`. See `tenants-ui-phase-2-story-backlog.md` for historical per-row `blockedBy`; `sprint-status.yaml` and `epics.md` are the current implementation handoff source after the 2026-06-05 correction.
```

**Rationale:** `FC-TBL` decisioning is no longer a remaining gate for Tenants implementation. The remaining risk is shared extraction and story-level evidence, not the already-resolved Tenants boundary.

### D. UX EXPERIENCE - Update Story 5.7 Gate Wording

**Artifact:** `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md`

**Section:** FrontComposer Readiness & Fallbacks

**OLD:**

```markdown
**Readiness severity split.** Reversible high-impact lifecycle controls, specifically **FR-15 disable/enable**, are not classified as hard destructive tenant deletion. They were implemented in Epic 3 under the approved `FC-CNS` inline consequence-preview fallback and `FC-CNC` one-at-a-time command policy. Hard destructive tenant deletion remains out of scope as future administrators-only CLI tooling. **FR-19 global-administrator grant/remove** was implemented in Epic 4 with fixed-scope projection confirmation and last-admin safety; audit-based global-administrator correction remains separately gated by Story 5.7 verification. Tenant-scoped destructive actions -- **FR-12 (remove-user)** and **FR-16/17 (config)** -- were implemented in Epic 2/Epic 3 with story-specific fallback evidence, while reusable FrontComposer extraction remains tracked separately.
```

**NEW:**

```markdown
**Readiness severity split.** Reversible high-impact lifecycle controls, specifically **FR-15 disable/enable**, are not classified as hard destructive tenant deletion. They were implemented in Epic 3 under the approved `FC-CNS` inline consequence-preview fallback and `FC-CNC` one-at-a-time command policy. Hard destructive tenant deletion remains out of scope as future administrators-only CLI tooling. **FR-19 global-administrator grant/remove** was implemented in Epic 4 with fixed-scope projection confirmation and last-admin safety; audit-based global-administrator correction was verified by Story 5.7 with the fixed `global-administrators` command scope and last-admin hard-stop safety. Tenant-scoped destructive actions -- **FR-12 (remove-user)** and **FR-16/17 (config)** -- were implemented in Epic 2/Epic 3 with story-specific fallback evidence, while reusable FrontComposer extraction and deferred correction-proof/freshness follow-ups remain tracked separately.
```

**Rationale:** This keeps UX honest about the now-completed Story 5.7 gate while preserving the fact that reusable FrontComposer and residual proof/freshness work are still separate.

### E. UX EXPERIENCE - Update Deferred-Obligation Note

**Artifact:** `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md`

**Section:** FrontComposer Readiness & Fallbacks note

**OLD:**

```markdown
[NOTE FOR UX] Sources silent / deferred, flagged rather than invented: (1) numeric freshness thresholds -- deferred to implementation + ops, must be configurable + surfaced (Open Q#10); (2) `duplicate` / `timeout` command-lifecycle tokens carry **no gloss** by spec instruction -- none invented; (3) per-story localization copy ownership remains Tenants-owned even though the shell boundary is confirmed by Story 1.0; (4) FR-22/24/25 (receipt assembly + compensating recovery) now have Epic 5 implementation evidence for the Tenants-owned audit, receipt, availability, and tenant-domain correction slice; audit-based global-administrator correction remains Story 5.7-gated; (5) the digest's pinned-version discrepancy (Fluent `rc.2-26098.1` vs `rc.3-26138.1`) is a DESIGN.md/build concern, recorded here only as context. **Owners for deferred obligations:** freshness thresholds -> product/ops; contrast (WCAG 1.4.3 / 1.4.11) verification of the inherited Fluent roles -> the implementing team's a11y reviewer at build; per-story a11y/l10n/doc evidence -> the implementing story owner plus UX/a11y reviewer.
```

**NEW:**

```markdown
[NOTE FOR UX] Sources silent / deferred, flagged rather than invented: (1) numeric freshness thresholds -- deferred to implementation + ops, must be configurable + surfaced (Open Q#10); (2) `duplicate` / `timeout` command-lifecycle tokens carry **no gloss** by spec instruction -- none invented; (3) per-story localization copy ownership remains Tenants-owned even though the shell boundary is confirmed by Story 1.0; (4) FR-22/24/25 (receipt assembly + compensating recovery) now have Epic 5 implementation evidence for the Tenants-owned audit, receipt, availability, tenant-domain correction slice, and Story 5.7-verified audit-based global-administrator correction path; residual correction-proof/freshness and projection-pagination follow-ups remain tracked in `_bmad-output/implementation-artifacts/deferred-work.md`; (5) the digest's pinned-version discrepancy (Fluent `rc.2-26098.1` vs `rc.3-26138.1`) is a DESIGN.md/build concern, recorded here only as context. **Owners for deferred obligations:** freshness thresholds -> product/ops; contrast (WCAG 1.4.3 / 1.4.11) verification of the inherited Fluent roles -> the implementing team's a11y reviewer at build; per-story a11y/l10n/doc evidence -> the implementing story owner plus UX/a11y reviewer.
```

**Rationale:** The UX note should no longer imply that enabled global-administrator correction is unverified.

## 5. Implementation Handoff

### Scope

**Moderate:** backlog/documentation reorganization and control clarification. No code implementation is included in this proposal.

### Owners

- **Product Owner / Technical Writer:** apply the PRD, addendum, UX, and epics text updates after approval.
- **Developer agent:** do not create new implementation work for Story 3.5 or Story 5.7 unless a future story touches their code paths.
- **Test Architect / UX reviewer:** continue enforcing per-story accessibility, localization, responsive, support-safety, forced-colors, contrast, live-region, and stable selector evidence.
- **Architect / FrontComposer owner:** keep reusable `FC-TOK`, `FC-AUD`, `FC-CNS`, cursor/pinning/list-state extraction, and any shared freshness/cursor concerns out of Tenants unless a specific Tenants domain slice is approved.

### Success Criteria

- `epics.md` contains direct Story 3.5 evidence links.
- PRD and UX notes state that Story 5.7 verification is complete, not still pending.
- Remaining risks are described as deferred follow-ups or reusable shared-module work, not hidden blockers.
- `sprint-status.yaml` remains unchanged unless a new human-approved action item is added.
- Continued story implementation uses the readiness conditions as gates, especially per-story UI evidence and support-safety checks.

## 6. Checklist Status

| Checklist item | Status | Notes |
| --- | --- | --- |
| 1.1 Triggering story/issue identified | [x] Done | Trigger is the 2026-06-30 readiness assessment and its explicit conditions, not a failing implementation story. |
| 1.2 Core problem defined | [x] Done | Planning/control drift: some conditions need handoff links; some "still gated" wording is stale after sprint status moved to done. |
| 1.3 Supporting evidence gathered | [x] Done | Readiness report, PRD, addendum, epics, UX spines, sprint status, Story 3.5, Story 5.7, and deferred-work were reviewed. |
| 2.1 Current epic impact evaluated | [x] Done | Epic 3 and Epic 5 impacted only by doc/handoff text. |
| 2.2 Required epic-level changes | [x] Done | No epic scope changes. Add links/clarifying notes only. |
| 2.3 Remaining epics reviewed | [x] Done | No future epic invalidation found. |
| 2.4 New/obsolete epics checked | [N/A] Skip | No new epic needed. |
| 2.5 Epic priority/order checked | [N/A] Skip | No resequencing needed. |
| 3.1 PRD conflicts checked | [x] Done | Updated stale Story 5.7 wording in PRD. |
| 3.2 Architecture conflicts checked | [x] Done | Selected architecture spine supports current plan; no architecture edit needed. |
| 3.3 UI/UX conflicts checked | [x] Done | Updated stale Story 5.7 wording in UX EXPERIENCE. |
| 3.4 Other artifacts checked | [x] Done | Added Story 3.5 evidence links in `epics.md`; no sprint-status mutation needed. |
| 4.1 Direct Adjustment evaluated | [x] Viable | Recommended. |
| 4.2 Potential Rollback evaluated | [N/A] Not viable | No completed work should be reverted. |
| 4.3 PRD MVP Review evaluated | [N/A] Not viable | MVP/scope remains valid; readiness report found full FR coverage. |
| 4.4 Path selected | [x] Done | Option 1 - Direct Adjustment. |
| 5.1 Issue summary created | [x] Done | This proposal. |
| 5.2 Epic/artifact needs documented | [x] Done | See sections 2 and 4. |
| 5.3 Recommended path documented | [x] Done | Direct Adjustment. |
| 5.4 MVP impact/action plan defined | [x] Done | No MVP change; doc updates only. |
| 5.5 Handoff plan established | [x] Done | Product Owner / Technical Writer, Developer, Test Architect, Architect/FrontComposer owner. |
| 6.1 Checklist completion reviewed | [x] Done | Open action-needed items are documented as proposed edits. |
| 6.2 Proposal accuracy verified | [x] Done | Cross-checked against current sprint status and story artifacts. |
| 6.3 User approval obtained | [x] Done | Approved by Administrator on 2026-06-30. |
| 6.4 Sprint status update | [N/A] Skip | No epics or stories added/removed/renumbered. |
| 6.5 Handoff confirmed | [x] Done | Handoff remains Product Owner / Technical Writer for doc governance, Developer for no duplicate implementation, Test Architect / UX reviewer for evidence gates, Architect / FrontComposer owner for shared capability boundaries. |

## 7. Approval Gate

This proposal was approved by Administrator on 2026-06-30 and the document-control edits were applied.
