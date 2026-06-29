---
title: "Sprint Change Proposal - Epic 3 Retrospective Course Correction"
date: "2026-06-29"
project_name: "Hexalith.Tenants"
author: "Correct Course workflow"
trigger: "_bmad-output/implementation-artifacts/epic-3-retro-2026-06-29.md"
mode: "Batch"
scope_classification: "Moderate"
status: "APPROVED"
implementation_status: "Story 3.5 tracking reconciliation applied"
approval:
  approved_by: "Administrator"
  approved_at: "2026-06-29T15:58:30+02:00"
  approval_note: "Approved with 'c' / continue."
---

# Sprint Change Proposal: Epic 3 Retrospective Course Correction

## 1. Issue Summary

The Epic 3 retrospective found that Epic 3 is story-complete, but the implementation ledger and
planning artifacts do not fully reflect the work and lessons from the epic.

The trigger is `_bmad-output/implementation-artifacts/epic-3-retro-2026-06-29.md`. It identifies six
follow-up items:

1. Reconcile Epic 3 tracking with completed Story 3.5.
2. Promote command-lock retention through accepted/projection-pending states into a shared guard.
3. Promote focus containment into the high-impact/destructive-flow checklist.
4. Add reason-honesty regression checks for degraded/unavailable/unknown projection states.
5. Resolve configuration preview scope.
6. Audit Epic 3 docs for retired projection-actor and FR15 hard-delete drift.

Concrete evidence:

- `sprint-status.yaml` lists Epic 3 Stories 3.1 through 3.5 as done, including
  `3-5-tenant-query-gateway-rest-routing`, and marks the Epic 3 retrospective done.
- `3-5-tenant-query-gateway-rest-routing.md` is complete and materially changed Epic 3 readiness by
  retiring the failed `TenantsProjectionActor` read path and restoring REST-backed tenant reads.
- The retro records repeated review fixes around command-lock retention, focus containment, and
  reason-honesty.
- Some docs still mention actor-era freshness wording, for example `CachingProjectionActor`, even
  though current architecture and implementation require REST-backed Tenants read endpoints and
  in-process query handlers.

## 2. Impact Analysis

### Epic Impact

- **Epic 3:** Feature scope remains complete. Ledger reconciliation is applied; the remaining work is
  explicit follow-through on reusable safety gates. No Epic 3 feature should be reopened.
- **Epic 4 and Epic 5:** Both consume the same command-flow invariants. Future global-administrator
  and correction work should reuse the command-lock, focus, and reason-honesty gates instead of
  rediscovering them in review.
- **Future epics/stories:** Any high-impact, destructive, correction, or configuration command flow
  should cite these guards before moving to done.

### Story Impact

- Story 3.5 is visible in sprint tracking as a completed Epic 3 defect-fix story.
- Completed Stories 3.1 through 3.4 should not be rewritten as if their original scopes were wrong.
  Their review lessons should be promoted into shared gates and future-story acceptance evidence.

### Artifact Conflicts

- `sprint-status.yaml`: Story 3.5 is present in the Epic 3 development-status block as done, matching
  the completed story record.
- `epics.md`: Epic 3 keeps the canonical FR15/FR16/FR17 feature list at Stories 3.1 through 3.4 and
  now names Story 3.5 as a completed defect-fix readiness record.
- PRD: Accessibility evidence already covers focus return and hover-free explanations, but it does
  not explicitly call out command-lock retention through accepted/projection-pending states or
  reason-honesty for data-availability failures.
- Architecture: The command confirmation and focus patterns exist, but the repeated Epic 3 review
  failures justify making command-lock retention and reason-honesty explicit enforcement points.
- UX/domain docs: Some historical docs still reference actor-era freshness sources. These should be
  corrected to REST read-model evidence without changing the product behavior.

### Technical Impact

This proposal does not require a rollback, endpoint change, domain contract change, data migration,
or submodule edit. It is a planning/tracking and test-governance adjustment, with later focused
implementation work for tests/checklists where the action items require it.

## 3. Recommended Approach

**Selected path: Direct Adjustment.**

Effort: Medium. Risk: Low to medium.

Rationale:

- The product scope and implemented behavior are still valid.
- The missing Story 3.5 visibility is a ledger drift problem, not a feature defect.
- The recurring command-lock, focus, and reason-honesty findings should become reusable gates rather
  than story-specific review memories.
- The configuration preview scope remains a Product/UX/Architecture decision, so it should stay an
  explicit open decision until approved.

Rejected alternatives:

- **Rollback:** Not useful. Story 3.5 fixed a real architectural regression and should remain.
- **MVP review/reduction:** Not needed. MVP scope does not change.
- **Reopen Epic 3 feature stories:** Not needed. The changes are follow-up governance and tracking,
  not corrections to completed feature acceptance.

Timeline impact:

- Tracking and documentation reconciliation: small, likely one focused documentation pass.
- Test/checklist hardening: small to medium, depending on whether existing component tests can reuse
  current command-flow helpers.
- Configuration preview scope decision: dependent on PM/UX/Architecture approval.

## 4. Detailed Change Proposals

### 4.1 Sprint Status: Track Story 3.5 Under Epic 3

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

OLD:

```yaml
  # --- Epic 3: Tenant Lifecycle and Configuration Control ---
  epic-3: done
  3-1-tenant-lifecycle-command-availability-and-blocked-state-guardrail: done
  3-2-disable-or-enable-tenant-with-high-impact-confirmation: done
  3-3-set-tenant-configuration-key-value-with-consequence-preview: done
  3-4-remove-tenant-configuration-key-with-consequence-preview: done
  epic-3-retrospective: done
```

NEW:

```yaml
  # --- Epic 3: Tenant Lifecycle and Configuration Control ---
  epic-3: done
  3-1-tenant-lifecycle-command-availability-and-blocked-state-guardrail: done
  3-2-disable-or-enable-tenant-with-high-impact-confirmation: done
  3-3-set-tenant-configuration-key-value-with-consequence-preview: done
  3-4-remove-tenant-configuration-key-with-consequence-preview: done
  3-5-tenant-query-gateway-rest-routing: done
  epic-3-retrospective: done
```

Rationale: Story 3.5 is a completed Epic 3 defect-fix record and materially affects readiness. The
tracking ledger should not require future retrospectives to rediscover it from loose artifacts.

Follow-up after applying this edit is complete:

```yaml
  - id: epic-3-retro-2026-06-29-story-35-tracking
    status: done
```

### 4.2 Epics: Add Explicit Defect-Fix Record To Epic 3

Artifact: `_bmad-output/planning-artifacts/epics.md`

OLD:

```markdown
## Epic 3: Tenant Lifecycle and Configuration Control

Authorized users can safely control tenant lifecycle and tenant configuration while preserving high-impact safety rules and projection truth.
```

NEW:

```markdown
## Epic 3: Tenant Lifecycle and Configuration Control

Authorized users can safely control tenant lifecycle and tenant configuration while preserving high-impact safety rules and projection truth.

**Completed defect-fix record:** Story 3.5, `Tenant Query Gateway REST Routing`, is a completed Epic 3
defect-fix story created by Correct Course. It is not part of the original FR15/FR16/FR17 feature
list, but it is part of Epic 3 readiness because it retired the failed projection-actor read path and
restored REST-backed Tenants reads with freshness/ETag behavior.
```

Rationale: Keeps the canonical feature list intact while making the completed defect-fix story visible
to downstream planning and retrospective readers.

### 4.3 PRD: Strengthen Acceptance Evidence For Repeated Epic 3 Safety Failures

Artifact: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md`

OLD:

```markdown
- **Acceptance evidence (definition of done for UI work):** keyboard-only navigation; screen-reader review (NVDA + at least one browser/SR pairing); automated accessibility checks; forced-colors/high-contrast; reduced-motion; contrast; live-region announcements; focus return; hover-free disabled explanations. Required acceptance scenarios: **stale projection, rejected command, unknown confirmation, audit unavailable, last-owner warning, permission-missing.** Responsive evidence at desktop (1024/1366/1440 + wide), tablet (768/1024), mobile (375/430), plus distinct narrow-width behavior (horizontal table overflow, nav collapse, dialog behavior).
```

NEW:

```markdown
- **Acceptance evidence (definition of done for UI work):** keyboard-only navigation; screen-reader review (NVDA + at least one browser/SR pairing); automated accessibility checks; forced-colors/high-contrast; reduced-motion; contrast; live-region announcements; focus return; hover-free disabled explanations; command-lock retention through `accepted` and `projection_pending` states; and reason-honesty for degraded, unavailable, and unknown projection states. Required acceptance scenarios: **stale projection, rejected command, unknown confirmation, audit unavailable, last-owner warning, permission-missing, command accepted but still projection-pending, focus escape/cancel no-commit, and data unavailable but not authorization-denied.** Responsive evidence at desktop (1024/1366/1440 + wide), tablet (768/1024), mobile (375/430), plus distinct narrow-width behavior (horizontal table overflow, nav collapse, dialog behavior).
```

Rationale: Epic 3 repeatedly found late issues in command locks, focus containment, and unavailable
reason mapping. These should be definition-of-done evidence, not review-only lessons.

### 4.4 PRD: Make Configuration Preview Scope Decisionable

Artifact: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md`

OLD:

```markdown
8. **Consequence Preview scope for config edits (FR-16)** - always required, or only for a high-risk key subset? (Also a phasing lever.)
```

NEW:

```markdown
8. **Consequence Preview scope for config edits (FR-16)** - default remains preview for all configuration mutations until Product/UX/Architecture records a narrower high-risk-key policy. Any narrowed policy must define the key classification rule, user-facing reason copy, test coverage for low-risk and high-risk keys, and the phasing impact. (Also a phasing lever.)
```

Rationale: This preserves the current safe default while giving John, Sally, and Winston a concrete
decision record to produce.

### 4.5 Architecture: Promote Command-Flow Safety Gates

Artifact: `_bmad-output/planning-artifacts/architecture.md`

OLD:

```markdown
**Pattern Enforcement:** bUnit asserts non-collapse + the six states + no-color-only; Playwright
asserts the six required acceptance scenarios (stale projection, rejected command, unknown
confirmation, audit unavailable, last-owner warning, permission-missing) keyed on `data-testid`; a
guard test fails any surface that references a raw state literal instead of the Vocabulary library.
Pattern changes are recorded here + in `project-context.md`.
```

NEW:

```markdown
**Pattern Enforcement:** bUnit asserts non-collapse + the six states + no-color-only; Playwright
asserts the required acceptance scenarios (stale projection, rejected command, unknown
confirmation, audit unavailable, last-owner warning, permission-missing, command accepted but still
projection-pending, focus escape/cancel no-commit, and data unavailable but not authorization-denied)
keyed on `data-testid`; a guard test fails any surface that references a raw state literal instead of
the Vocabulary library. Command-flow tests must prove sibling command surfaces stay unavailable
through `accepted` and `projection_pending` until projection truth or a terminal non-pending state.
Pattern changes are recorded here + in `project-context.md`.
```

Rationale: Makes the Epic 3 review lessons enforceable at the architecture pattern level.

### 4.6 Truth-State Spec: Replace Actor-Era Freshness Source

Artifact: `docs/tenants-ui-truth-state-and-action-availability-spec.md`

OLD:

```markdown
Freshness is bound to read-model evidence only: a timestamp, a projection version, or an ETag. The freshness primitive is `If-None-Match` -> `304 Not Modified`, served by `CachingProjectionActor`. If none of these can be measured, the freshness state is `unknown`. [Source: `_bmad-output/project-context.md#API Surface`; `docs/tenants-ui-operations-shell-spec.md#Truth-State Vocabulary (referenced, not redefined)`]
```

NEW:

```markdown
Freshness is bound to read-model evidence only: a timestamp, a projection version, or an ETag. The freshness primitive is `If-None-Match` -> `304 Not Modified`, served by the REST-backed Tenants read endpoints through in-process query handlers and read-model metadata. If none of these can be measured, the freshness state is `unknown`. [Source: `_bmad-output/project-context.md#Domain, Eventing & Framework Rules`; `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`; `docs/tenants-ui-operations-shell-spec.md#Truth-State Vocabulary (referenced, not redefined)`]
```

Rationale: Avoids preserving `CachingProjectionActor` or projection-actor terminology after Story 3.5
and later freshness hardening moved Tenants reads to REST/in-process handlers.

### 4.7 Audit/Recovery Spec: Replace Actor-Era Audit Freshness Source

Artifact: `docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md`

OLD:

```markdown
- Audit evidence comes only from the existing read endpoint `GET /api/tenants/{tenantId}/audit` (`GetTenantAuditQuery`, rows = `TenantAuditEntry`). Cursor-based pagination only - signed, opaque, scope-bound cursors; never offset/limit. ETag `If-None-Match` -> `304` is the freshness primitive served by `CachingProjectionActor`. **No new audit/receipt/consequence endpoint.** [Source: `_bmad-output/project-context.md#API Surface`; `#Projections`]
```

NEW:

```markdown
- Audit evidence comes only from the existing read endpoint `GET /api/tenants/{tenantId}/audit` (`GetTenantAuditQuery`, rows = `TenantAuditEntry`). Cursor-based pagination only; cursors are signed, opaque, and scope-bound, never offset/limit. ETag `If-None-Match` -> `304` is the freshness primitive served by the REST-backed Tenants audit read path and read-model metadata. **No new audit/receipt/consequence endpoint.** [Source: `_bmad-output/project-context.md#Domain, Eventing & Framework Rules`; `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`]
```

Rationale: Keeps audit/recovery docs aligned with the current read architecture and prevents future
agents from reintroducing the retired projection-actor path.

### 4.8 Story Records: No Historical Acceptance Rewrite

No completed Story 3.1 through 3.5 acceptance criteria should be rewritten as part of this proposal.
The correct change is to:

- make Story 3.5 visible in sprint tracking;
- add the defect-fix note to `epics.md`;
- strengthen PRD/architecture/doc gates for future command-flow work;
- leave completed story evidence as historical implementation records.

Rationale: Rewriting completed story scopes would blur history. The retro lessons are real, but they
belong in shared gates and future-story evidence, not as retroactive acceptance edits.

## 5. Checklist Execution Summary

| Item | Status | Notes |
| --- | --- | --- |
| 1.1 Triggering story/context | Done | Epic 3 retro, including completed Story 3.5 drift. |
| 1.2 Core problem | Done | Tracking drift plus repeated command-flow safety hardening. |
| 1.3 Supporting evidence | Done | Retro, sprint-status, Story 3.5 record, PRD, architecture, UX/docs reviewed. |
| 2.1 Current epic viability | Done | Epic 3 remains complete; tracking/docs need adjustment. |
| 2.2 Epic-level changes | Done | Add explicit Story 3.5 visibility; no feature scope change. |
| 2.3 Remaining epic impact | Done | Epics 4/5 should consume strengthened command-flow gates. |
| 2.4 New/obsolete epics | N/A | No new epic needed. |
| 2.5 Epic order/priority | N/A | No resequencing needed. |
| 3.1 PRD conflicts | Done | Acceptance evidence and config-preview decision wording need updates. |
| 3.2 Architecture conflicts | Done | Pattern enforcement should name the recurring gates. |
| 3.3 UX conflicts | Done | Focus/preview obligations already exist; promote checklist/test visibility. |
| 3.4 Other artifacts | Done | sprint-status and docs contain the main drift. |
| 4.1 Direct adjustment | Viable | Recommended. |
| 4.2 Rollback | Not viable | No rollback simplifies the issue. |
| 4.3 MVP review | Not viable | MVP scope unaffected. |
| 4.4 Recommended path | Done | Direct adjustment with moderate coordination. |
| 5.1 Issue summary | Done | See section 1. |
| 5.2 Impact summary | Done | See section 2. |
| 5.3 Path forward | Done | See section 3. |
| 5.4 MVP/action plan | Done | See sections 3 and 4. |
| 5.5 Handoff plan | Done | See section 6. |
| 6.1 Checklist completion | Done | All applicable sections addressed. |
| 6.2 Proposal accuracy | Done | Drafted from current artifacts. |
| 6.3 User approval | Done | Approved by Administrator on 2026-06-29T15:58:30+02:00. |
| 6.4 sprint-status update | Done | Story 3.5 is tracked under Epic 3 and the tracking action item is done. |
| 6.5 Handoff confirmation | Done | Tracking reconciliation routed to Developer/Tech Writer; remaining non-tracking follow-ups stay in action items. |

## 6. Implementation Handoff

Scope classification: **Moderate**.

Handoff recipients:

- **Amelia (Developer):** preserve sprint-status tracking, add or update focused command-flow guards,
  and keep Story 3.5 visible in Epic 3 evidence.
- **Paige (Technical Writer):** continue docs drift corrections for Story 3.5, retired projection actor
  language, and FR15 hard-deletion wording.
- **Murat (Test Architect):** define regression coverage for command-lock retention and reason-honesty.
- **Sally (UX Designer):** ensure high-impact/destructive-flow checklist explicitly covers focus loop,
  Escape/cancel no-commit, and launcher focus return.
- **John (Product Manager), Sally (UX Designer), Winston (System Architect):** resolve configuration
  preview scope.

Success criteria:

- `sprint-status.yaml` names Story 3.5 under Epic 3.
- The Story 3.5 tracking action item is closed with that visibility present.
- PRD/architecture/docs no longer imply Tenants UI reads require a retired projection actor path.
- Command-flow test guidance explicitly verifies command-lock retention during accepted/projection
  pending states.
- High-impact/destructive flows require tested focus containment and focus return.
- Degraded/unavailable/unknown projection states map to data-availability reasons, not permission
  failures unless authorization is actually the failed gate.
- Configuration preview scope has a recorded Product/UX/Architecture decision before further config
  edit scope changes.

## 7. Approval Request

Administrator approved this proposal on 2026-06-29T15:58:30+02:00. The Story 3.5 tracking
reconciliation has been applied to sprint tracking and planning artifacts. Remaining non-tracking
follow-ups continue through their recorded action items.
