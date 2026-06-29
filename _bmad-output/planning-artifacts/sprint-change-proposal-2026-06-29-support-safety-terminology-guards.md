---
title: "Sprint Change Proposal - Audit and Correction Support-Safety Guards"
date: "2026-06-29"
project_name: "Hexalith.Tenants"
author: "Correct Course workflow"
trigger: "epic-5-retro-2026-06-29-support-safety"
mode: "Batch"
scope_classification: "Minor"
status: "APPROVED"
approval:
  approved_by: "Administrator"
  approved_at: "2026-06-29T16:38:44+02:00"
  approval_note: "Approved with 'yes'."
---

# Sprint Change Proposal: Audit and Correction Support-Safety Guards

## 1. Issue Summary

The active change trigger is the Epic 5 retrospective action item:

> Keep support-safety and prohibited recovery terminology guards on every audit or correction change.

The issue is not a missing product requirement. The PRD, architecture, UX, and Epic 5 stories already
state the core rules:

- Audit and correction UI must be support-safe.
- Raw payloads, bearer tokens, decoded JWT contents, raw EventStore metadata, protected cursors,
  ETags, internal correlation ids, message ids, stack traces, and unsafe PII must not render or be
  copied.
- Recovery must be forward correction through compensating commands.
- Labels, tooltips, announcements, accessible names, resources, and rendered copy must not use
  prohibited recovery terminology: `undo`, `rollback`, or `hidden edit`.

The gap is durability. Current Epic 5 stories contain these guards unevenly as story-local
requirements. Future audit or correction changes, especially small cleanup, review, or code-review
patches, can bypass the guard unless the rule is promoted into the cross-cutting story/change
guardrail and checked during every audit/correction implementation.

Concrete evidence:

- `sprint-status.yaml` has an open action item:
  `epic-5-retro-2026-06-29-support-safety`.
- `epics.md` already contains the product rules in FR7, FR22, FR24, NFR8, UX-DR22, and Epic 5
  story acceptance criteria.
- Story 5.7 already requires support-safe proof lookup, forbidden terminology tests, and unsafe
  support-marker guards.
- Story 5.8 already requires support-safety and prohibited terminology preservation during correction
  projection-refresh cleanup.
- The existing requirements are correct, but there is no single "every audit/correction change"
  rule in the cross-cutting story guardrails.

## 2. Impact Analysis

### Epic Impact

- **Epic 5:** Directly affected. Epic 5 remains in progress with Story 5.7 in progress and Story 5.8
  ready for development. The epic does not need scope expansion, rollback, or reordering.
- **Epics 1-4:** No reopened scope. Their support-safety requirements remain valid. Epic 4 is relevant
  only because global-administrator correction in Story 5.7 consumes Epic 4 command support.
- **Future audit/correction work:** Any story, patch, audit review, correction cleanup, proof-link
  change, receipt change, audit-row mapping change, localization change, or code-review fix touching
  audit/correction surfaces must carry the guard.

### Story Impact

- **Story 5.7:** No replan needed. It already contains the required guard in acceptance criteria,
  tasks, and expected tests. The implementation review should verify those guard tests were actually
  run.
- **Story 5.8:** No scope change needed. It already includes support-safety, prohibited terminology,
  and focused test requirements. The handoff should explicitly treat those as non-optional cleanup
  criteria.
- **Future stories:** The story creation and dev-story handoff must include the audit/correction
  guard whenever a change touches audit evidence, receipts, correction start, correction preview,
  correction lifecycle, proof links, audit availability, support-safe copy, or related localization.

### Artifact Conflicts

- **PRD:** No PRD change required. It already states support-safety and correction vocabulary rules.
- **Architecture:** No architecture change required. It already places support-safety/redaction in the
  server-side BFF and requires safe localized rejection text.
- **UX:** No UX change required. `EXPERIENCE.md` and the audit/recovery spec already define the
  allowed recovery verbs and prohibited terms.
- **Epics/story guardrails:** Action needed. The cross-cutting guardrails should make this a mandatory
  audit/correction change rule, not only a story-local detail.
- **Sprint status:** Action needed after implementation. The retrospective action item should remain
  open until the guard is applied in the planning/story handoff and verified in the active or next
  audit/correction validation.

### Technical Impact

No backend endpoint, command contract, projection, AppHost, EventStore, FrontComposer, package, or
schema change is required. The implementation impact is limited to planning/story text and focused
test expectations. If the current Story 5.7 implementation lacks static/rendered guard tests, those
tests should be added in Story 5.7 rather than spun into a new feature.

## 3. Recommended Approach

Use **Direct Adjustment**.

Rationale:

- The core requirement already exists in the PRD, UX, architecture, and Epic 5 story records.
- The risk is regression during small audit/correction changes, not missing product scope.
- A rollback would not help.
- MVP scope does not need review.
- A minor cross-cutting guardrail update plus developer/test handoff gives the next implementation
  agent a concrete, repeatable rule.

Effort estimate: Low.

Risk level: Low, provided the implementation does not touch active Story 5.7 code in the same edit
unless the Developer agent owns that story context.

Timeline impact: Minimal. Apply before or during the next audit/correction story handoff or review.

## 4. Detailed Change Proposals

### Proposal A - Epics Cross-Cutting Quality Bar

Artifact: `_bmad-output/planning-artifacts/epics.md`

Section: `Cross-Cutting Quality Bar`

OLD:

```markdown
Every epic inherits the shared quality bar from the requirements inventory: projection truth is authoritative, Success is reserved for projection-proven or audit-proven states, command and audit lifecycle states do not collapse into each other, authorization and support-safety are server-enforced, copy is Tenants-owned and localized through `.resx`, accessibility/responsive evidence is required for readiness, and stable `data-testid` selectors are used for all interactive controls and statuses.
```

NEW:

```markdown
Every epic inherits the shared quality bar from the requirements inventory: projection truth is authoritative, Success is reserved for projection-proven or audit-proven states, command and audit lifecycle states do not collapse into each other, authorization and support-safety are server-enforced, copy is Tenants-owned and localized through `.resx`, accessibility/responsive evidence is required for readiness, and stable `data-testid` selectors are used for all interactive controls and statuses.

Every audit or correction change also carries a mandatory guard: static or rendered tests must prove support-safe output and prohibited recovery terminology protection for visible copy, accessible names, tooltips, announcements, resources, copied references, audit rows, receipts, correction previews, lifecycle panels, proof links, and error/unavailable states.
```

Rationale: This promotes the existing Epic 5 safety rule to an inherited change guard.

### Proposal B - Story Creation Guardrails

Artifact: `_bmad-output/planning-artifacts/epics.md`

Section: `Story Creation Guardrails`

OLD:

```markdown
Every story created from these epics must make the safety contract explicit in acceptance criteria and test expectations. Each story states the actor and job, names the projection truth source and staleness behavior, names the permission boundary and server-side authorization result, preserves pending/failed/denied/unknown states without false Success, consumes existing backend endpoints without adding local Tenants infrastructure, includes Tenants-owned `.resx` copy, and identifies the required accessibility, responsive, live-region, forced-colors, and stable `data-testid` evidence. Every command story also includes audit/evidence behavior, including delayed or unavailable audit states, and every story includes a test contract naming the fixture, observable state, and automation level such as unit, component, API, or Playwright.
```

NEW:

```markdown
Every story created from these epics must make the safety contract explicit in acceptance criteria and test expectations. Each story states the actor and job, names the projection truth source and staleness behavior, names the permission boundary and server-side authorization result, preserves pending/failed/denied/unknown states without false Success, consumes existing backend endpoints without adding local Tenants infrastructure, includes Tenants-owned `.resx` copy, and identifies the required accessibility, responsive, live-region, forced-colors, and stable `data-testid` evidence. Every command story also includes audit/evidence behavior, including delayed or unavailable audit states, and every story includes a test contract naming the fixture, observable state, and automation level such as unit, component, API, or Playwright.

Any story or correction request that touches audit evidence, audit rows, audit availability, receipts, correction start, correction preview, correction lifecycle, proof linking, support-safe copy, or audit/correction localization must include tests or static guards for:

- no raw payloads, command payloads, decoded JWT contents, bearer tokens, raw EventStore metadata, protected cursors, ETags, internal correlation ids, message ids, stack traces, or unsafe PII in rendered or copied output;
- no prohibited recovery terminology in visible copy, accessible names, tooltips, announcements, resource values, or rendered snapshots;
- forward-only correction language that uses the approved recovery verbs and never implies event, projection, or state-store mutation.
```

Rationale: Story creation is where small future audit/correction changes are easiest to catch before
they become implementation gaps.

### Proposal C - Sprint Status Action Item

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

Section: `action_items`

OLD:

```yaml
- id: epic-5-retro-2026-06-29-support-safety
  epic: 5
  action: "Keep support-safety and prohibited recovery terminology guards on every audit or correction change."
  owner: "Murat (Test Architect)"
  status: open
  source: "_bmad-output/implementation-artifacts/epic-5-retro-2026-06-29.md"
```

NEW after Proposal A and B are applied, and after the active audit/correction validation cites the
guard:

```yaml
- id: epic-5-retro-2026-06-29-support-safety
  epic: 5
  action: "Keep support-safety and prohibited recovery terminology guards on every audit or correction change."
  owner: "Murat (Test Architect)"
  status: done
  source: "_bmad-output/implementation-artifacts/epic-5-retro-2026-06-29.md"
```

Rationale: The item should not be marked done merely because this proposal exists. It should close
only after the cross-cutting guard is applied and at least the active or next audit/correction story
validation demonstrates it.

### Proposal D - Active Story 5.7 and Ready Story 5.8 Handoff

Artifacts:

- `_bmad-output/implementation-artifacts/5-7-global-administrator-correction-verification.md`
- `_bmad-output/implementation-artifacts/5-8-correction-projection-refresh-cleanup.md`

Change:

No required text edit is proposed because both story files already contain the guard in their
acceptance criteria, task lists, and test contracts. The handoff requirement is verification:

- Story 5.7 review must prove its static/rendered copy guard covers global-administrator correction
  copy, resources, receipts, lifecycle states, proof links, and unavailable/error states.
- Story 5.8 implementation must preserve the already-listed support-safety and terminology guard
  while changing projection-refresh behavior.

Rationale: Avoid rewriting active in-progress story work unnecessarily. Enforce the guard through
review evidence instead.

## 5. Checklist Results

- [x] 1.1 Triggering story/context identified: Epic 5 retrospective support-safety action item.
- [x] 1.2 Core problem defined: durable guardrail gap, not missing product scope.
- [x] 1.3 Evidence gathered from sprint status, PRD, epics, architecture, UX, audit/recovery specs,
  and active Stories 5.7/5.8.
- [x] 2.1 Current epic assessed: Epic 5 can continue as planned.
- [x] 2.2 Epic-level changes identified: promote guard into cross-cutting epics/story guardrails.
- [x] 2.3 Remaining epics reviewed: no Epic 1-4 rework needed.
- [N/A] 2.4 No new epic required.
- [N/A] 2.5 No epic resequencing required.
- [x] 3.1 PRD checked: no conflict, no PRD update required.
- [x] 3.2 Architecture checked: no conflict, no architecture update required.
- [x] 3.3 UX checked: existing allowed/prohibited terminology remains authoritative.
- [!] 3.4 Secondary artifact action: update epics guardrails and close sprint-status action only after
  validation evidence exists.
- [x] 4.1 Direct Adjustment viable: low effort, low risk.
- [N/A] 4.2 Rollback not viable or useful.
- [N/A] 4.3 MVP review not needed.
- [x] 4.4 Recommended path selected: Direct Adjustment.
- [x] 5.1-5.5 Proposal sections, impact, action plan, and handoff defined.
- [x] 6.3 User approval received from Administrator on 2026-06-29T16:38:44+02:00.
- [N/A] 6.4 Sprint status changes deferred until implementation and validation.

## 6. Implementation Handoff

Scope classification: **Minor**.

Route to: Developer agent and Test Architect.

Responsibilities:

- Developer agent applies Proposal A and Proposal B to `epics.md`.
- Developer agent avoids touching active Story 5.7 implementation files unless explicitly working
  that story.
- Test Architect confirms Story 5.7 or the next audit/correction change has guard coverage for
  support-safe rendered/copy output and prohibited terminology.
- Sprint owner marks `epic-5-retro-2026-06-29-support-safety` done only after the guard is applied
  and validation evidence is cited.

Success criteria:

- Cross-cutting epics/story guardrails explicitly require the audit/correction support-safety and
  terminology guard.
- Active Story 5.7 review or next audit/correction story validation cites static/rendered guard tests.
- Story 5.8 does not weaken the existing guard while performing projection-refresh cleanup.
- No implementation introduces backend endpoints, raw audit payload exposure, unsafe copied
  references, optimistic proof, or recovery wording that implies in-place history mutation.

## 7. Approval Record

Approved by Administrator on 2026-06-29T16:38:44+02:00 with `yes`.

The minor direct adjustment has been applied to `_bmad-output/planning-artifacts/epics.md`.
The sprint-status action remains open until validation evidence cites the guard.
