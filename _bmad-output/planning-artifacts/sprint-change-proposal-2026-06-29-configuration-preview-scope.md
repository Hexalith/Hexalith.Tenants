# Sprint Change Proposal: Configuration Preview Scope

Date: 2026-06-29
Project: tenants
Mode: Batch
Scope classification: Minor - documentation and planning clarification only
Approval: Approved by Administrator on 2026-06-29

## 1. Issue Summary

Epic 3 retrospective action item 5 identified unresolved planning language around tenant configuration edits:

- Whether every configuration edit requires Consequence Preview.
- Or whether only a defined high-risk subset requires Consequence Preview.

The implementation evidence for Epic 3 already chose the conservative path:

- Story 3.3 requires the approved inline Consequence Preview for set-configuration.
- Story 3.4 requires the approved inline Consequence Preview for remove-configuration.
- The UX design already states that the consequence-preview component is required for every config edit (FR-16/FR-17).

The PRD still contained conflicting language: FR-16 mentioned "high-impact keys" and the assumptions index still said the scope was unresolved.

## 2. Impact Analysis

### Epic Impact

Epic 3 remains complete. No epic resequencing or new epic is required.

Affected epic area:

- Epic 3: Tenant Lifecycle and Configuration Control.
- Story 3.3: Set Tenant Configuration Key Value with Consequence Preview.
- Story 3.4: Remove Tenant Configuration Key with Consequence Preview.

The existing stories already align with the decision. No story implementation change is needed.

### Story Impact

No completed story needs code changes. Existing behavior remains authoritative:

- All eligible set-configuration mutations require preview.
- All eligible remove-configuration mutations require preview.
- Identical set key/value remains a NoOp shown as `already applied`.
- Missing remove target remains a safe `ConfigurationKeyNotFound` rejection.
- Projection confirmation remains required before success.

### Artifact Conflicts

PRD required correction:

- FR-16 still described preview for high-impact keys.
- FR-16 consequences still carried an unresolved `[ASSUMPTION]`.
- Open Question 8 needed to become a resolved decision.
- Assumptions Index needed to remove the unresolved configuration-preview assumption.

UX already aligned and was not changed:

- `EXPERIENCE.md` states Consequence Preview is required for every config edit.
- `DESIGN.md` states the panel appears before every destructive or high-impact action and every config edit.

Epics and story records already aligned and were not changed:

- `epics.md` Story 3.3 and Story 3.4 both require complete Consequence Preview.
- Story 3.3 and Story 3.4 implementation artifacts already record the conservative preview-for-every-command posture.

### Technical Impact

No backend, UI, domain contract, architecture, or test code changes are required.

This decision explicitly avoids adding a key-risk classifier in Tenants. A future narrowed policy would require a separate Product/UX/Architecture decision and implementation work.

## 3. Recommended Approach

Recommended path: Direct Adjustment.

Decision:

All eligible tenant configuration set/remove mutations require Consequence Preview in v1. There is no low-risk-key bypass and no high-risk-subset classifier in the current Tenants UI scope.

Rationale:

- Configuration keys are consumer-owned and namespaced; Tenants does not currently own a durable risk taxonomy for keys.
- Story 3.3 and Story 3.4 already implemented the all-preview posture.
- The UX design already documents every config edit as preview-required.
- Narrowing now would introduce policy, copy, test, and implementation work without a recorded classification rule.
- The safer behavior preserves the existing fail-closed, support-safe, projection-confirmed command model.

Effort estimate: Low.

Risk assessment: Low product and technical risk because this aligns documentation with implemented behavior. The main tradeoff is extra user friction for low-risk configuration keys, accepted for v1 until a narrower classifier exists.

Timeline impact: None for current completed stories.

## 4. Detailed Change Proposals

### PRD: FR-16

Section: `7.6 Tenant Configuration Management`

Old:

```markdown
An authorized user can set a namespaced configuration key/value, with Consequence Preview for high-impact keys.
- **Consequences:** identical key+value is a **NoOp** (`already applied`); values exceeding domain limits are rejected with safe text (`ConfigurationLimitExceeded`); `[ASSUMPTION]` whether every config edit needs a preview or only a high-risk subset is an open question that is also a phasing lever (§16).
```

New:

```markdown
An authorized user can set a namespaced configuration key/value, with Consequence Preview required for every eligible configuration mutation in v1.
- **Consequences:** identical key+value is a **NoOp** (`already applied`); values exceeding domain limits are rejected with safe text (`ConfigurationLimitExceeded`); no low-risk-key bypass exists in v1, and any future narrowing to a high-risk subset requires a Product/UX/Architecture decision that defines key classification, user-facing reasons, tests, and phasing impact (§16).
```

Rationale:

This removes the high-impact-only implication and makes the current all-preview implementation explicit.

### PRD: FR-17

Section: `7.6 Tenant Configuration Management`

Old:

```markdown
An authorized user can remove a configuration key.
- **Consequences:** removing a missing key surfaces a safe `ConfigurationKeyNotFound` rejection; removal shows success only after projection confirmation.
```

New:

```markdown
An authorized user can remove a configuration key, with Consequence Preview required for every eligible configuration removal in v1.
- **Consequences:** removing a missing key surfaces a safe `ConfigurationKeyNotFound` rejection; removal shows success only after projection confirmation; no low-risk-key bypass exists in v1.
```

Rationale:

Removal is part of the same configuration-mutation policy and already shipped with a preview requirement.

### PRD: Open Question 8

Section: `16. Open Questions`

Old:

```markdown
8. **Consequence Preview scope for config edits (FR-16)** — default remains preview for all configuration mutations until Product/UX/Architecture records a narrower high-risk-key policy. Any narrowed policy must define the key classification rule, user-facing reason copy, test coverage for low-risk and high-risk keys, and the phasing impact. (Also a phasing lever.)
```

New:

```markdown
8. **Consequence Preview scope for config edits (FR-16/FR-17) - RESOLVED 2026-06-29.** All eligible set/remove configuration mutations require Consequence Preview in v1. No high-risk-subset bypass is defined. Any future narrowing requires a Product/UX/Architecture decision that defines the key classification rule, user-facing reason copy, test coverage for low-risk and high-risk keys, and the phasing impact.
```

Rationale:

The open question is now resolved for v1 while preserving the process for a future narrowed policy.

### PRD: Assumptions Index

Section: `17. Assumptions Index`

Old:

```markdown
- §7.6 (FR-16) — Whether every config edit needs a Consequence Preview, or only a high-risk subset, is unresolved.
```

New:

```markdown
<removed>
```

Rationale:

The topic is no longer an unresolved assumption.

## 5. Checklist Results

| Item | Status | Notes |
|---|---:|---|
| 1.1 Triggering story or source | Done | Epic 3 retrospective action item 5. |
| 1.2 Core problem | Done | Misalignment between PRD wording and implemented/UX behavior. |
| 1.3 Evidence | Done | Story 3.3, Story 3.4, UX EXPERIENCE/DESIGN, PRD FR-16/FR-17. |
| 2.1 Current epic impact | Done | Epic 3 remains complete. |
| 2.2 Epic changes | N/A | No new, removed, or redefined epic needed. |
| 2.3 Remaining epics | Done | No downstream epic blocked; future narrowing would be a separate change. |
| 2.4 Future epic invalidation | N/A | None. |
| 2.5 Priority/order change | N/A | None. |
| 3.1 PRD conflicts | Done | PRD updated. |
| 3.2 Architecture conflicts | Done | None; architecture already supports preview/gating path. |
| 3.3 UX conflicts | Done | UX already aligned; no edit needed. |
| 3.4 Other artifacts | Done | No code/test/sprint-status change required for this decision. |
| 4.1 Direct Adjustment | Viable | Low effort, low risk. |
| 4.2 Rollback | Not viable | No implementation rollback needed. |
| 4.3 MVP Review | Not viable | MVP scope unchanged. |
| 4.4 Path selected | Done | Direct Adjustment. |
| 5.1 Issue summary | Done | Included above. |
| 5.2 Impact and adjustments | Done | Included above. |
| 5.3 Recommended path | Done | Direct Adjustment. |
| 5.4 MVP/action plan | Done | MVP unaffected; docs corrected. |
| 5.5 Handoff plan | Done | See below. |
| 6.1 Checklist completion | Done | All applicable items addressed. |
| 6.2 Proposal accuracy | Done | Cross-checked against PRD, UX, epics, and story evidence. |
| 6.3 User approval | Done | Explicit approval received from Administrator on 2026-06-29. |
| 6.4 Sprint status update | N/A | No epic/story status changes. |
| 6.5 Handoff plan | Done | Minor documentation handoff only. |

## 6. Implementation Handoff

Scope: Minor.

Approval status: Approved.

Recipients:

- Developer agent: no code implementation required.
- Product Manager / UX Designer / Architect: treat the configuration preview scope as resolved for v1.
- Technical Writer: use this file plus the PRD update as the decision record for Epic 3 action item 5.

Success criteria:

- PRD states that all eligible set/remove configuration mutations require Consequence Preview in v1.
- PRD no longer lists the all-vs-high-risk configuration preview question as unresolved.
- Any future narrowed high-risk policy is handled as a new decision requiring key classification, user-facing reason copy, test coverage, and phasing impact.

## 7. Final Decision

All tenant configuration edits that would set or remove configuration in the Tenants UI require Consequence Preview in v1. There is no defined low-risk/high-risk bypass subset today.
