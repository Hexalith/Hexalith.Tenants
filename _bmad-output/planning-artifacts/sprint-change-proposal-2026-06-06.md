---
title: "Sprint Change Proposal - FR15 Disable/Enable Soft Delete Reclassification"
date: "2026-06-06"
project_name: "Hexalith.Tenants"
author: "Correct Course workflow"
trigger: "Story 3.2 blocked because disable/enable was classified with hard destructive deletion"
mode: "Batch"
scope_classification: "Moderate"
status: "APPROVED"
approval:
  approved_by: "Administrator"
  approved_at: "2026-06-06T10:54:05+02:00"
  approval_note: "Disable/enable is a reversible soft delete / availability-control flow. Hard delete will be added in a future release as an independent administrators-only CLI tool."
affectedArtifacts:
  - "_bmad-output/planning-artifacts/fallback-approval-record-2026-06-03.md"
  - "_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md"
  - "_bmad-output/planning-artifacts/epics.md"
  - "_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md"
  - "_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md"
  - "docs/tenants-ui-frontcomposer-dependency-map.md"
  - "docs/tenants-ui-phase-2-story-backlog.md"
  - "docs/tenants-ui-truth-state-and-action-availability-spec.md"
  - "_bmad-output/planning-artifacts/implementation-readiness-report-2026-06-05-v2.md"
  - "_bmad-output/implementation-artifacts/3-2-disable-or-enable-tenant-with-high-impact-confirmation.md"
  - "_bmad-output/implementation-artifacts/3-3-set-tenant-configuration-key-value-with-consequence-preview.md"
  - "_bmad-output/implementation-artifacts/sprint-status.yaml"
---

# Sprint Change Proposal: FR15 Disable/Enable Soft Delete Reclassification

## 1. Issue Summary

Story 3.2, "Disable or Enable Tenant with High-Impact Confirmation," is currently blocked because FR15 was grouped with hard destructive platform actions. The user clarified on 2026-06-06 that disable/enable is a reversible lifecycle "soft delete" or availability control for this phase, not hard destructive tenant deletion.

Hard delete remains out of scope for this phase and will be added in a future release as an independent CLI tool for administrators only. This correction only reclassifies FR15 disable/enable.

Evidence from existing artifacts supports this correction:

- `docs/event-contract-reference.md` already models `DisableTenant` and `EnableTenant` as paired lifecycle commands.
- `TenantDisabled` stops tenant-scoped access/background work, while `TenantEnabled` resumes it.
- `docs/compensating-commands.md` explicitly names `EnableTenant` as the compensating command after accidental `DisableTenant`.
- The current blocker is governance classification in planning artifacts, not a domain model impossibility.

## 2. Checklist Summary

| Checklist Area | Status | Notes |
| --- | --- | --- |
| 1. Understand Trigger and Context | Complete | Trigger is Story 3.2 blocked by the hard destructive classification of FR15. |
| 2. Epic Impact Assessment | Complete | Epic 3 remains valid. Story 3.2 should be unblocked after artifact correction. FR19 remains blocked. |
| 3. Artifact Conflict and Impact Analysis | Complete | PRD, UX, epics, fallback approval record, dependency map, story file, and sprint status need aligned wording. |
| 4. Path Evaluation | Complete | Direct adjustment is sufficient. Rollback, de-scope, and MVP replanning are not warranted. |
| 5. Sprint Change Proposal | Complete | This document is the proposal. |
| 6. Final Review and Handoff | Complete | Approved by Administrator on 2026-06-06; artifact edits are authorized. |

## 3. Recommended Approach

Use a direct adjustment to reclassify FR15:

- FR15 disable/enable is a reversible lifecycle soft-delete / availability-control operation.
- FR15 remains high-impact, global-admin-only, projection-confirmed, and consequence-preview-required.
- FR15 may proceed under the approved FC-CNS inline consequence preview and FC-CNC one-at-a-time command fallback once story-specific evidence is satisfied.
- Hard destructive tenant deletion remains out of scope for this phase and belongs to a future independent administrators-only CLI tool.
- FR19 global-administrator grant/remove remains categorically blocked unless a separate governance decision clears it.

Update the affected artifacts, then refresh Story 3.2 and promote it from blocked/backlog to ready for development if no other story-specific gates remain.

## 4. Proposed Artifact Changes

### 4.1 Fallback Approval Record

File: `_bmad-output/planning-artifacts/fallback-approval-record-2026-06-03.md`

Update the prior shared-blocker statement with language that distinguishes FR15 from hard destructive deletion:

```markdown
This approval does not authorize hard destructive tenant deletion or global-administrator governance changes. FR15 disable/enable is reclassified by the approved 2026-06-06 Sprint Change Proposal as a reversible lifecycle soft-delete / availability-control operation, not hard destructive tenant deletion. FR15 may use the approved FC-CNS inline consequence-preview fallback once its story-specific evidence is satisfied. FR19 remains categorically blocked with no fallback. Actual tenant hard deletion is future independent administrators-only CLI tooling and out of scope for this phase.
```

### 4.2 UX Experience Readiness Split

File: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md`

Replace the readiness severity split that classifies FR15 as categorically blocked:

```markdown
Readiness severity split. Reversible high-impact lifecycle controls, specifically FR15 disable/enable, are not classified as hard destructive deletion. They are implementation-eligible under the approved FC-CNS inline preview and FC-CNC one-at-a-time fallback after story-specific evidence is satisfied. Hard destructive tenant deletion remains out of scope as future administrators-only CLI tooling. Global-administrator grant/remove FR19 remains categorically blocked unless a separate governance decision clears it. Tenant-scoped destructive actions FR12, FR16, and FR17 remain fallback-eligible.
```

### 4.3 Epics

File: `_bmad-output/planning-artifacts/epics.md`

Update the global blocked-action note:

```markdown
FR15 disable/enable is a reversible lifecycle soft-delete / availability-control operation, not hard destructive tenant deletion, and may proceed under approved command and preview fallbacks once story-specific evidence is satisfied. FR19 remains categorically blocked. Hard destructive tenant deletion is future administrators-only CLI tooling and out of scope for this phase.
```

Update Story 3.2 acceptance criteria to remove the platform-policy blocker and keep the implementation gates:

```markdown
Given lifecycle disable/enable is approved as a reversible soft-delete availability control, the story is ready only when global-admin authorization reflection, complete consequence preview, one-at-a-time command policy, projection-confirmed lifecycle feedback, accessibility, localization, and responsive evidence are present; hard destructive tenant deletion is out of scope.
```

### 4.4 PRD

File: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md`

Add this clarification to FR15:

```markdown
For this phase, disable/enable is a reversible lifecycle soft-delete / availability-control operation, not hard destructive tenant deletion. Hard destructive tenant deletion is out of scope and belongs to future independent administrators-only CLI tooling.
```

### 4.5 PRD Addendum

File: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md`

Add this correction near promotion or fallback gate language:

```markdown
The 2026-06-06 correction reclassifies FR15 as a high-impact reversible lifecycle control eligible under approved command and preview fallbacks. FR19 remains blocked. Hard destructive tenant deletion remains out of scope for this phase and belongs to a future independent administrators-only CLI tool.
```

### 4.6 Dependency Map and Backlog

Files:

- `docs/tenants-ui-frontcomposer-dependency-map.md`
- `docs/tenants-ui-phase-2-story-backlog.md`

Update Disable Tenant rows and rationale from `blocked` to `planning-only / fallback-eligible after 2026-06-06 correction`, preserving high-impact safeguards:

```markdown
Status rationale: Reversible lifecycle availability control, not hard destructive tenant deletion. Still global-admin-only, high-impact, consequence-preview-required, one-at-a-time, and projection-confirmed.
```

### 4.7 Story 3.2 and Sprint Status

Files:

- `_bmad-output/implementation-artifacts/3-2-disable-or-enable-tenant-with-high-impact-confirmation.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

Applied after approval and artifact updates:

- Refreshed Story 3.2 with corrected implementation context.
- Promoted Story 3.2 to `Status: ready-for-dev` because no other story-specific gate remained in the handoff source.
- Changed sprint status for `3-2-disable-or-enable-tenant-with-high-impact-confirmation` from `backlog` to `ready-for-dev`.

## 5. Implementation Handoff

Approved implementation handoff:

1. Apply the artifact changes listed in section 4.
2. Refresh Story 3.2 so its implementation context reflects the corrected governance classification.
3. Validate that Story 3.2 still preserves the high-impact controls: global-admin authorization reflection, consequence preview, same-state rejection handling, projection-confirmed success, one-at-a-time command handling, localization, accessibility, and responsive constraints.
4. Update sprint status only after the refreshed story is no longer blocked.

Recommended follow-up workflow after these artifact edits:

```text
$bmad-create-story 3.2
```
