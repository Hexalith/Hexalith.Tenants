# Sprint Change Proposal: Global Administrator Correction Verification

Date: 2026-06-29
Project: tenants
Mode: Batch
Status: Story created; source implementation pending Developer handoff
Requested by: Administrator

## 1. Issue Summary

The Epic 5 retrospective refresh identified a changed dependency: global-administrator correction is no longer blocked by missing Epic 4 command primitives, because Stories 4.3 and 4.4 implemented `SetGlobalAdministrator` and `RemoveGlobalAdministrator` command flows. However, global-administrator correction must still remain fail-closed until a focused story verifies that the enabled correction path uses the fixed `global-administrators` scope, preserves last-admin safety, maps platform-governance rejection copy, performs projection-confirmed success, and links support-safe proof.

Evidence:

- `_bmad-output/implementation-artifacts/epic-5-retro-2026-06-29.md` records the action item: "Create a focused global-administrator correction verification story before enabling global-administrator correction."
- `TenantCorrectionStartIntent` can model global-admin correction when command support is explicitly true, but the live path still passes `HasGlobalAdministratorCommandSupport: false`.
- `CorrectionStartPanel` currently submits tenant-domain corrections only. Global-admin intents still fall through to command-support unavailable.
- Story 4.3 and Story 4.4 provide reusable global-admin grant/remove command gateway methods and projection-confirmed page behavior.

## 2. Impact Analysis

Epic Impact:

- Epic 4 remains complete for direct global-administrator grant/remove command management.
- Epic 5 remains complete for tenant-domain audit and correction, but gains focused Story 5.7 as follow-up verification before global-admin correction enablement.
- No new epic is required. The 2026-06-29 retrospective explicitly says there is no Epic 6 in the current epics document and recommends focused follow-up/correct-course work.

Story Impact:

- Add Story 5.7: Global Administrator Correction Verification.
- Story 5.5 and Story 5.6 are not reopened; their current fail-closed global-admin behavior is treated as the baseline to verify and safely lift.
- Story 4.3 and Story 4.4 become prerequisites for reuse, not implementation targets.

Artifact Conflicts:

- PRD already states that any enabled global-administrator correction path still needs story-specific verification. No PRD scope change is required.
- Architecture already states Epic 4 command support exists and Epic 5 owns correction/proof patterns. No architectural decision changes are required.
- Some historical audit/recovery docs still phrase global-admin correction as waiting for Epic 4 command support. That broader documentation drift remains a separate retrospective action item.

Technical Impact:

- The future implementation should update Tenants UI state/components/tests only.
- Expected source areas: `TenantCorrectionStartIntent`, `TenantCorrectionPreviewSnapshot` or a focused global-admin correction state, `CorrectionStartPanel` or a local successor, `TenantAuditPage`, resources, and focused UI/gateway tests.
- No backend command/query/event contract, AppHost, Aspire, EventStore, or FrontComposer changes are proposed.

## 3. Recommended Approach

Recommended path: Direct Adjustment.

Rationale:

- The issue is bounded and does not invalidate PRD, Epic 4, or Epic 5 goals.
- Existing command gateway methods and projection-confirmed global-admin page flows can be reused.
- The risk is not missing infrastructure; it is enabling a high-impact correction path without story-specific verification.
- A focused Story 5.7 keeps the change implementable by a Developer agent and avoids broad replanning.

Effort estimate: Low to medium.

Risk level: Medium until implemented and tested, because the path affects platform-governance authority and last-admin protection.

Timeline impact: Adds one focused ready-for-dev story before global-administrator correction can be enabled.

## 4. Detailed Change Proposals

### Story Changes

Story: Epic 5, new Story 5.7
Section: Epics backlog

OLD:

```text
Epic 5 ends at Story 5.6: Preview and Confirm Correction with Linked Proof.
Global-administrator correction remains fail-closed by implementation convention and retrospective action item only.
```

NEW:

```text
Story 5.7: Global Administrator Correction Verification

As an authorized operator,
I want global-administrator correction enabled only after the fixed-scope correction path is verified,
So that platform authority recovery cannot be inferred from tenant-domain correction behavior.
```

Rationale: Tenant-domain correction evidence cannot prove global-administrator correction safety. A story-level gate is required before enablement.

Story: `_bmad-output/implementation-artifacts/5-7-global-administrator-correction-verification.md`
Section: New story file

OLD:

```text
No dedicated story file exists.
```

NEW:

```text
Status: ready-for-dev
Acceptance criteria and tasks explicitly verify fixed global-administrator scope selection,
last-admin safety, platform-governance rejection copy, proof lookup, and fail-closed behavior.
```

Rationale: The Developer agent needs direct implementation context rather than a retrospective action item.

Story: `_bmad-output/implementation-artifacts/sprint-status.yaml`
Section: Epic 5 status and action item tracking

OLD:

```yaml
epic-5: done
5-6-preview-and-confirm-correction-with-linked-proof: done
epic-5-retrospective: done

status: open
```

NEW:

```yaml
epic-5: in-progress
5-6-preview-and-confirm-correction-with-linked-proof: done
5-7-global-administrator-correction-verification: ready-for-dev
epic-5-retrospective: done

status: done
```

Rationale: The story has been created and is ready for implementation; the retrospective action item is satisfied at the planning/story-creation level.

### PRD Changes

No PRD edit is required. The current PRD already records that enabled global-administrator correction needs story-specific verification.

### Architecture Changes

No architecture edit is required. The architecture already requires projection-confirmed command flows, BFF-only egress, no new backend endpoints, fixed global-administrator scope, and forward compensating commands.

### UI/UX Specification Changes

No broad UX spec edit is included in this correction. Story 5.7 carries UX acceptance for platform-governance copy, fail-closed states, accessibility, localization, and terminal focus behavior. Historical doc drift is tracked separately.

## 5. Checklist Results

- [x] 1.1 Triggering story/source identified: Epic 5 retrospective refresh action item, with Story 5.5/5.6 fail-closed behavior and Story 4.3/4.4 command availability as evidence.
- [x] 1.2 Core problem defined: missing story-level verification before enabling global-administrator correction.
- [x] 1.3 Evidence gathered from retrospective, epics, PRD, architecture, current UI code, and adjacent story files.
- [x] 2.1 Current epic can continue with a focused follow-up story.
- [x] 2.2 Epic-level change needed: add Story 5.7 under Epic 5.
- [x] 2.3 Remaining planned epics reviewed: no future epic invalidation.
- [x] 2.4 No new epic required.
- [x] 2.5 No resequencing beyond adding Story 5.7 before enablement.
- [x] 3.1 PRD conflict checked: no PRD edit required.
- [x] 3.2 Architecture conflict checked: no architecture decision change required.
- [x] 3.3 UI/UX impact checked: Story 5.7 includes UX/accessibility/localization acceptance.
- [x] 3.4 Secondary artifacts checked: sprint status and retrospective action tracking updated.
- [x] 4.1 Direct adjustment selected; effort low/medium, risk medium.
- [N/A] 4.2 Rollback not viable or needed.
- [N/A] 4.3 MVP review not needed.
- [x] 4.4 Recommended path documented.
- [x] 5.1 Issue summary created.
- [x] 5.2 Epic/artifact impacts documented.
- [x] 5.3 Recommendation documented.
- [x] 5.4 MVP impact and action plan documented.
- [x] 5.5 Handoff plan established.
- [x] 6.1 Checklist reviewed.
- [x] 6.2 Proposal accuracy reviewed.
- [x] 6.3 User approval inferred for story creation from explicit request; source implementation still requires dev-story execution.
- [x] 6.4 Sprint status updated for the added story.
- [x] 6.5 Next steps and handoff included.

## 6. Implementation Handoff

Scope classification: Minor.

Route to: Developer agent.

Deliverables:

- `_bmad-output/implementation-artifacts/5-7-global-administrator-correction-verification.md`
- Updated `_bmad-output/planning-artifacts/epics.md`
- Updated `_bmad-output/implementation-artifacts/sprint-status.yaml`

Success criteria:

- Global-administrator correction remains fail-closed until Story 5.7 implementation passes.
- Implementation verifies fixed `system/global-administrators/global-administrators` command scope.
- Grant correction confirms target presence in `GetGlobalAdministratorsAsync`.
- Remove correction confirms target absence in `GetGlobalAdministratorsAsync`.
- Last-admin removal remains unavailable/hard-blocked.
- Original and corrective global-admin audit rows are linked using support-safe system-scope proof.
- No tenant membership or tenant-role logic is used for global-admin correction.
