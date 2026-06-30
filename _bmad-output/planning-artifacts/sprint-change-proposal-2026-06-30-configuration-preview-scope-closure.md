# Sprint Change Proposal: Configuration Preview Scope Closure

Date: 2026-06-30
Project: tenants
Mode: Batch
Scope classification: Minor - documentation/status closure only
Approval: Requested by Administrator on 2026-06-30

## 1. Issue Summary

The Epic 3 retrospective carried an open action item:

> Resolve and document whether all configuration edits require consequence preview or only a defined high-risk subset.

The policy was already decided and documented on 2026-06-29 in `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-29-configuration-preview-scope.md`, and the PRD now states the decision in FR-16, FR-17, and Open Question 8:

- All eligible tenant configuration set mutations require Consequence Preview in v1.
- All eligible tenant configuration remove mutations require Consequence Preview in v1.
- No low-risk-key or high-risk-subset bypass is defined for v1.
- Any future narrowing requires a new Product/UX/Architecture decision defining key classification, user-facing reason copy, low-risk/high-risk test coverage, and phasing impact.

The remaining issue was not product ambiguity. It was closure drift: `sprint-status.yaml` still showed the retrospective action as open, and the historical Phase 2 backlog still listed the all-vs-subset question as a deferred decision even though the `ui-10` row already reflected an approved all-preview fallback.

## 2. Impact Analysis

### Epic Impact

Epic 3 remains done. No epic scope, sequencing, or acceptance criteria change is required.

Affected area:

- Epic 3: Tenant Lifecycle and Configuration Control.
- Story 3.3: Set Tenant Configuration Key Value with Consequence Preview.
- Story 3.4: Remove Tenant Configuration Key with Consequence Preview.
- Retrospective action: `epic-3-retro-2026-06-29-config-preview-scope`.

### Story Impact

No implementation story is reopened. Stories 3.3 and 3.4 already implemented the all-preview posture:

- Set configuration uses the approved inline Consequence Preview fallback before submission.
- Remove configuration uses Consequence Preview and exact-key destructive confirmation before submission.
- Identical set key/value remains `already applied`.
- Missing remove target remains `ConfigurationKeyNotFound`.
- Projection confirmation remains required before success.

### Artifact Conflicts

Current PRD and UX artifacts align with the decision:

- PRD FR-16 and FR-17 require preview for every eligible configuration set/remove mutation.
- PRD Open Question 8 is marked resolved as of 2026-06-29.
- UX `EXPERIENCE.md` requires Consequence Preview for every config edit.
- UX `DESIGN.md` describes the preview panel as shown before every config edit.

Artifacts requiring closure updates:

- `_bmad-output/implementation-artifacts/sprint-status.yaml` still marked the retrospective action open.
- `docs/tenants-ui-phase-2-story-backlog.md` still listed the policy as a deferred decision, despite the current `ui-10` row using `inline-consequence-preview-for-all-configuration-set-and-remove-commands`.

Historical readiness reports and prior dated proposals are not rewritten; they remain time-stamped evidence of earlier state.

### Technical Impact

No code, backend contract, UI behavior, infrastructure, or test change is required.

This closure explicitly avoids introducing a configuration key risk classifier in Tenants. Such a classifier would be new product and architecture scope.

## 3. Recommended Approach

Recommended path: Direct Adjustment.

Decision:

All eligible tenant configuration set/remove mutations require Consequence Preview in v1. There is no defined low-risk/high-risk bypass subset today.

Rationale:

- Tenants configuration keys are consumer-owned namespaced strings; Tenants does not own a durable key-risk taxonomy.
- The implemented Epic 3 flows already require preview for set and remove.
- The current PRD and UX artifacts already align with the all-preview policy.
- Narrowing the policy would add classifier, copy, accessibility, localization, and low-risk/high-risk test coverage work without an approved classification rule.
- The all-preview policy preserves the fail-closed and projection-confirmed command model.

Effort estimate: Low.

Risk assessment: Low. The change closes documentation/status drift and does not alter runtime behavior.

Timeline impact: None.

## 4. Detailed Change Proposals

### Sprint Status: Epic 3 Retrospective Action

File: `_bmad-output/implementation-artifacts/sprint-status.yaml`

Old:

```yaml
- id: epic-3-retro-2026-06-29-config-preview-scope
  epic: 3
  action: "Resolve and document whether all configuration edits require consequence preview or only a defined high-risk subset."
  owner: "John (Product Manager), Sally (UX Designer), and Winston (System Architect)"
  status: open
  source: "_bmad-output/implementation-artifacts/epic-3-retro-2026-06-29.md"
```

New:

```yaml
- id: epic-3-retro-2026-06-29-config-preview-scope
  epic: 3
  action: "Resolve and document whether all configuration edits require consequence preview or only a defined high-risk subset."
  owner: "John (Product Manager), Sally (UX Designer), and Winston (System Architect)"
  status: done
  source: "_bmad-output/implementation-artifacts/epic-3-retro-2026-06-29.md"
  resolution_source: "_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-30-configuration-preview-scope-closure.md"
```

Rationale:

The retrospective action now has a documented decision record and should no longer surface as open work.

### Phase 2 Story Backlog: Status Supersession

File: `docs/tenants-ui-phase-2-story-backlog.md`

Old:

```markdown
Epic 3 later implemented `ui-13` lifecycle disable/enable and `ui-10` tenant configuration set/remove through the approved inline structured-text `FC-CNS` fallback, the `FC-CNC` one-at-a-time policy, server-side BFF command gateways, projection re-query confirmation, exact destructive confirmation where applicable, focus-loop/focus-return evidence, EN/FR resource parity, and support-safe rejection/audit handoff states. The historical rows remain for planning traceability and reusable FrontComposer component work, but they are no longer the implementation-readiness source for the delivered Epic 3 lifecycle and configuration flows.
```

New:

```markdown
Epic 3 later implemented `ui-13` lifecycle disable/enable and `ui-10` tenant configuration set/remove through the approved inline structured-text `FC-CNS` fallback, the `FC-CNC` one-at-a-time policy, server-side BFF command gateways, projection re-query confirmation, exact destructive confirmation where applicable, focus-loop/focus-return evidence, EN/FR resource parity, and support-safe rejection/audit handoff states. The current `ui-10` policy is that every eligible configuration set/remove command requires Consequence Preview in v1; no high-risk-subset bypass is defined. The historical rows remain for planning traceability and reusable FrontComposer component work, but they are no longer the implementation-readiness source for the delivered Epic 3 lifecycle and configuration flows.
```

Rationale:

Readers of the historical backlog see the current policy before they reach old planning rows.

### Phase 2 Story Backlog: Deferred Decisions

File: `docs/tenants-ui-phase-2-story-backlog.md`

Old:

```markdown
| Decide whether high-impact configuration changes always require consequence preview or can be split by key risk class. | Tenants Product/UX + Tenants module owner | `ui-10` | Configuration edit story is split into low-impact and high-impact rows, or Product/UX records a single consequence-preview policy. |
```

New:

```markdown
The previous `ui-10` deferred question about all configuration edits versus a high-risk key subset is resolved. The policy is recorded in `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-29-configuration-preview-scope.md` and closed by `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-30-configuration-preview-scope-closure.md`: all eligible configuration set/remove mutations require Consequence Preview in v1, and any future narrowing requires a new Product/UX/Architecture decision with key classification, user-facing reason copy, low-risk/high-risk test coverage, and phasing impact.
```

Rationale:

The deferred-decision table should list unresolved decisions only. The resolved policy is retained as context above the table.

## 5. Checklist Results

| Item | Status | Notes |
|---|---:|---|
| 1.1 Triggering story or source | Done | Epic 3 retrospective action `epic-3-retro-2026-06-29-config-preview-scope`. |
| 1.2 Core problem | Done | Closure drift after the 2026-06-29 decision record. |
| 1.3 Evidence | Done | PRD FR-16/FR-17/Q8, Stories 3.3/3.4, UX DESIGN/EXPERIENCE, existing 2026-06-29 proposal. |
| 2.1 Current epic impact | Done | Epic 3 remains complete. |
| 2.2 Epic changes | N/A | No new, removed, or redefined epic. |
| 2.3 Remaining epics | Done | No downstream epic change required. |
| 2.4 Future epic invalidation | N/A | None. |
| 2.5 Priority/order change | N/A | None. |
| 3.1 PRD conflicts | Done | PRD already aligned. |
| 3.2 Architecture conflicts | Done | None; architecture supports preview/fail-closed command flows. |
| 3.3 UX conflicts | Done | UX already aligned. |
| 3.4 Other artifacts | Done | Sprint status and historical backlog required closure updates. |
| 4.1 Direct Adjustment | Viable | Low effort, low risk. |
| 4.2 Rollback | Not viable | No implementation rollback needed. |
| 4.3 MVP Review | Not viable | MVP and Epic 3 scope unchanged. |
| 4.4 Path selected | Done | Direct Adjustment. |
| 5.1 Issue summary | Done | Included above. |
| 5.2 Impact and adjustments | Done | Included above. |
| 5.3 Recommended path | Done | All-preview v1 policy retained. |
| 5.4 MVP/action plan | Done | MVP unaffected; action plan is documentation/status closure. |
| 5.5 Handoff plan | Done | See below. |
| 6.1 Checklist completion | Done | All applicable items addressed. |
| 6.2 Proposal accuracy | Done | Cross-checked against PRD, UX, epics, stories, backlog, and sprint status. |
| 6.3 User approval | Done | Administrator requested the resolution and documentation on 2026-06-30. |
| 6.4 Sprint status update | Done | Retrospective action marked done. |
| 6.5 Handoff plan | Done | Minor documentation handoff only. |

## 6. Implementation Handoff

Scope: Minor.

Recipients:

- Product Manager / UX Designer / Architect: treat configuration preview scope as resolved for v1.
- Developer agent: no code change required; future configuration command work must keep all-preview behavior unless a new Product/UX/Architecture decision narrows it.
- Technical Writer: cite the 2026-06-29 decision record and this closure record when updating future backlog/readiness material.

Success criteria:

- Sprint status no longer lists the configuration preview scope action as open.
- The active/historical backlog no longer lists the all-vs-subset question as a deferred decision.
- The documented policy is explicit: all eligible configuration set/remove mutations require Consequence Preview in v1.
- Future narrowing is treated as new scope, not an implicit bypass.

## 7. Final Decision

All tenant configuration edits that set or remove configuration through the Tenants UI require Consequence Preview in v1. There is no defined low-risk/high-risk bypass subset today.
