---
title: "Sprint Change Proposal - High-Impact Destructive Focus Checklist"
date: "2026-06-29"
project_name: "Hexalith.Tenants"
author: "Correct Course workflow"
trigger: "Promote tested focus containment, Escape/cancel no-commit behavior, and launcher focus return into the high-impact/destructive-flow checklist."
mode: "Batch"
scope_classification: "Minor"
status: "APPROVED"
approval:
  approved_by: "Administrator"
  approved_at: "2026-06-29T17:16:30+02:00"
  approval_note: "Approved with 'approve'."
---

# Sprint Change Proposal: High-Impact Destructive Focus Checklist

## 1. Issue Summary

Recent high-impact and destructive Tenants UI stories delivered focus-loop containment, safe non-committing Escape/cancel exits, and exact launcher focus return as tested behavior. The planning artifacts already described those behaviors in PRD/UX language, but the story-author checklist and acceptance-evidence matrix did not elevate the three behaviors as an explicit high-impact/destructive-flow checklist item.

Evidence:

- Story 3.2 delivered lifecycle disable/enable with focus-loop sentinels and launcher focus return.
- Story 3.4 delivered remove-configuration cancellation/Escape no-commit behavior and launcher focus-return coverage.
- The PRD and UX spine already require complete-or-exit behavior, modal focus trap, safe escape, and focus return for high-impact/destructive command flows.

## 2. Impact Analysis

Epic impact: No epic scope changes. This is a cross-cutting readiness-gate clarification for future high-impact/destructive UI stories.

Story impact: Future command stories that render destructive or high-impact preview/confirmation surfaces must cite explicit test evidence for the promoted behaviors.

Artifact impact: Update the dependency-map story-author checklist and the accessibility/localization acceptance-evidence spec. PRD, architecture, and completed story scope remain valid.

Technical impact: Documentation-only. No backend endpoint, command contract, projection, UI implementation, package, submodule, persistence, or AppHost change.

Checklist status:

- [x] Trigger and context identified.
- [x] PRD, epics, architecture, UX, and relevant docs reviewed.
- [x] Direct adjustment selected.
- [x] Documentation artifacts updated.

## 3. Recommended Approach

Recommended path: Direct Adjustment.

Rationale: The behaviors are already product requirements and implementation precedents. Promoting them into the checklist prevents future story authors from treating focus containment, Escape/cancel no-commit behavior, or launcher focus return as optional accessibility details.

Effort: Low.
Risk: Low.
Timeline impact: None.

## 4. Detailed Change Proposals

Artifact: `docs/tenants-ui-frontcomposer-dependency-map.md`
Section: Future Story Author Checklist

OLD:

```text
Accessibility | Keyboard, focus, live-region, reduced-motion, forced-colors, and component test evidence are explicit, usually through FC-A11Y.
```

NEW:

```text
Accessibility | Keyboard, focus, live-region, reduced-motion, forced-colors, and component test evidence are explicit, usually through FC-A11Y.
High-impact/destructive flows | Tests prove focus containment while the preview/confirmation is open, Escape/cancel are non-committing exits, and focus returns to the exact launching control after close, cancel, submit, failure, or blocked completion.
```

Rationale: The story-author checklist is the highest-friction handoff point for future UI stories. The new row makes the high-impact/destructive focus contract reviewable before a story is marked ready.

Artifact: `docs/tenants-ui-accessibility-localization-and-acceptance-evidence-spec.md`
Sections: Accessibility testing evidence set, Required acceptance scenarios, Future Implementation Story Rules

OLD:

```text
Acceptance checks for UI stories must include stale projection, rejected command, unknown confirmation, audit unavailable, last-owner warning, and permission-missing cases.
```

NEW:

```text
Acceptance checks for UI stories must include stale projection, rejected command, unknown confirmation, audit unavailable, last-owner warning, permission-missing, and high-impact/destructive-flow focus-and-cancel cases.
```

Rationale: The evidence matrix now requires explicit proof for focus containment, Escape/cancel no-commit, and exact launcher focus return rather than relying on broader accessibility language.

## 5. Implementation Handoff

Scope classification: Minor.

Route: Developer direct documentation update; QA/Test Architect should enforce in future story reviews.

Success criteria:

- The high-impact/destructive-flow checklist names all three promoted behaviors.
- The acceptance-evidence spec requires test evidence for those behaviors.
- No backend, UI implementation, or sprint sequencing scope is introduced.
