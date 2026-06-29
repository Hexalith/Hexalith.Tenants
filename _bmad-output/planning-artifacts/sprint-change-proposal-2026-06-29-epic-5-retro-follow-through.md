# Sprint Change Proposal: Epic 5 Retro Follow-through Reconciliation

Date: 2026-06-29
Project: tenants
Mode: Batch
Status: Approved by Administrator for implementation
Trigger: `_bmad-output/implementation-artifacts/epic-5-retro-2026-06-29.md`
Approval: 2026-06-29

## 1. Issue Summary

The Epic 5 retrospective refresh identified that Epic 5 is complete for tenant-domain audit evidence and forward recovery, but the sprint follow-through state has changed since the first Epic 5 retrospective.

The key triggering issue is not a broad Epic 5 reopen. It is a reconciliation problem:

- Global-administrator correction is no longer blocked by missing Epic 4 command primitives. Stories 4.3 and 4.4 provide `SetGlobalAdministrator` and `RemoveGlobalAdministrator` command support.
- Global-administrator correction still must remain fail-closed until Story 5.7 verifies the enabled correction path against fixed `system/global-administrators/global-administrators` scope, last-admin safety, platform-governance copy, projection confirmation, and proof lookup.
- Terminal correction focus handling has already been corrected and verified by `sprint-change-proposal-2026-06-29-terminal-correction-focus.md`.
- Redundant correction projection refresh remains open as low-risk polish debt.
- Audit/recovery documentation drift remains open, but historical retrospective files should remain valid snapshots rather than be rewritten.
- Support-safety and prohibited recovery terminology checks remain a standing gate for every audit/correction change.

Evidence reviewed:

- `_bmad-output/implementation-artifacts/epic-5-retro-2026-06-29.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md`
- `_bmad-output/planning-artifacts/architecture.md`
- `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md`
- `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-29-global-administrator-correction-verification.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-29-terminal-correction-focus.md`
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor`
- `src/Hexalith.Tenants.UI/State/TenantAudit/TenantCorrectionPreviewSnapshot.cs`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor`

## 2. Impact Analysis

### Epic Impact

Epic 5 should stay active until Story 5.7 is implemented. Current sprint status already has:

```yaml
epic-5: in-progress
5-7-global-administrator-correction-verification: ready-for-dev
epic-5-retrospective: done
```

No Epic 6 is required. The current work is focused follow-through inside Epic 5.

Epic 4 remains complete. Its global-administrator grant/remove command stories become prerequisites that Story 5.7 reuses; they are not reopened.

### Story Impact

Story 5.7 already exists and should be the next implementation gate before enabling global-administrator correction.

Story 5.6 is not reopened for terminal focus; that correction has already been implemented and verified separately.

A new focused follow-up story is recommended for redundant correction projection refresh cleanup:

```text
Story 5.8: Correction Projection Refresh Cleanup
```

This story should be small and technical. It must remove duplicate projection reads in the correction status path without weakening projection-confirmed success, support-safe proof lookup, audit availability states, or current parent page projection state.

### Artifact Conflicts

PRD: no change required. The PRD already states that compensating recovery is forward-only, that success requires projection truth, and that global-administrator correction needs story-specific verification.

Architecture: no architectural decision change required. Existing architecture already requires BFF-only backend egress, no new backend endpoints, projection truth, SignalR as nudge only, and no history mutation.

UX: no broad UX redesign required. Story 5.7 and Story 5.8 must preserve keyboard complete-or-exit, terminal focus behavior, non-collapse, support-safe copy, and prohibited terminology guards.

Sprint tracking: add Story 5.8 only if this proposal is approved. Keep the projection-refresh action item open until implementation is verified. Keep the documentation-drift action item open until current docs are audited and updated. Keep the support-safety action item as an active standing gate.

### Technical Impact

Story 5.7 expected implementation areas:

- `TenantCorrectionStartIntent`
- `TenantCorrectionPreviewSnapshot` or focused global-admin correction state
- `CorrectionStartPanel` or a local successor
- `TenantAuditPage`
- `ITenantCommandGateway` existing global-admin methods
- `ITenantQueryGateway.GetGlobalAdministratorsAsync`
- Tenants resources and UI tests

Story 5.8 expected implementation areas:

- `CorrectionStartPanel.RefreshStatusAsync`
- The projection-refresh callback boundary between `CorrectionStartPanel` and `TenantAuditPage`
- Focused bUnit/state tests proving projection read count, projection-confirmed success, and proof lookup behavior

No backend contracts, event records, projection actors, AppHost resources, FrontComposer shared code, or DAPR infrastructure changes are proposed.

## 3. Recommended Approach

Recommended path: Direct Adjustment with backlog tracking.

Rationale:

- The retrospective does not invalidate PRD, architecture, Epic 4, or tenant-domain Epic 5 delivery.
- The highest-risk follow-up already has a ready-for-dev story: Story 5.7.
- The redundant projection refresh is a bounded implementation cleanup that can be isolated in Story 5.8.
- Documentation drift should be handled as a targeted current-doc sweep, not a rewrite of historical evidence snapshots.
- Support-safety should remain a standing acceptance gate on every audit/correction story.

Effort estimate: Medium overall.

Risk level: Medium until Story 5.7 is implemented, because global-administrator correction affects platform authority and last-admin safety.

Timeline impact: Adds one small technical follow-up story after or alongside Story 5.7. No broad replan.

## 4. Detailed Change Proposals

### Proposal A: Keep Story 5.7 as the Global-Administrator Correction Gate

Artifact: `_bmad-output/implementation-artifacts/5-7-global-administrator-correction-verification.md`

OLD:

```text
Story 5.7 exists and is ready-for-dev, but implementation has not started.
The live audit path still passes HasGlobalAdministratorCommandSupport: false.
```

NEW:

```text
Route Story 5.7 to Developer implementation before any global-administrator correction path is enabled.
Do not flip HasGlobalAdministratorCommandSupport to true without evidence-based gating, fixed-scope projection confirmation, last-admin safety, and proof-link tests.
```

Rationale: Tenant-domain correction success is not evidence that global-administrator correction is safe.

### Proposal B: Add Story 5.8 for Redundant Correction Projection Refresh Cleanup

Artifact: `_bmad-output/planning-artifacts/epics.md`

OLD:

```text
Epic 5 ends at Story 5.7: Global Administrator Correction Verification.
The redundant correction projection refresh is tracked only as a retrospective action item.
```

NEW:

```text
### Story 5.8: Correction Projection Refresh Cleanup

As an authorized operator,
I want correction status refresh to use a single authoritative projection refresh,
So that correction flows stay efficient without weakening projection-confirmed success or proof lookup.

Requirements: FR24, FR25; NFR1, NFR3, NFR5, NFR8; UX-DR3, UX-DR25, UX-DR28.

Acceptance Criteria:

Given a correction status refresh runs after accepted or projection-pending command status,
When projection confirmation is required,
Then the component uses one authoritative refreshed projection snapshot for confirmation
And it does not issue both a parent page projection refresh and a second direct tenant projection query for the same status refresh.

Given the refreshed projection confirms the intended correction,
When proof lookup runs,
Then support-safe corrective audit proof lookup still executes after projection confirmation
And command status or SignalR alone still cannot prove correction success.

Given projection evidence is missing, stale, degraded, or unavailable,
When the correction lifecycle renders,
Then the UI preserves last-confirmed projection evidence, fails closed where required, and does not show success.

Given the cleanup is implemented,
When tests run,
Then focused component/state tests prove projection refresh call count, projection-confirmed success, delayed proof behavior, terminal focus, and no raw payload/token/correlation leakage.
```

Rationale: The current `CorrectionStartPanel.RefreshStatusAsync` calls `OnProjectionRefreshRequested` and then directly queries projection evidence for local confirmation. The cleanup should make one refreshed projection serve both purposes.

### Proposal C: Update Sprint Tracking Only After Approval

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

OLD:

```yaml
epic-5: in-progress
5-7-global-administrator-correction-verification: ready-for-dev
epic-5-retrospective: done

- id: epic-5-retro-2026-06-29-projection-refresh
  status: open
```

NEW, after Story 5.8 is created:

```yaml
epic-5: in-progress
5-7-global-administrator-correction-verification: ready-for-dev
5-8-correction-projection-refresh-cleanup: ready-for-dev
epic-5-retrospective: done

- id: epic-5-retro-2026-06-29-projection-refresh
  status: open
```

Rationale: Creating Story 5.8 does not by itself remove the redundant refresh. Keep the action item open until implementation and verification are complete.

### Proposal D: Run a Targeted Documentation Drift Sweep

Artifacts:

- `docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md`
- `docs/tenants-ui-truth-state-and-action-availability-spec.md`
- `docs/compensating-commands.md`
- Current planning references under `_bmad-output/planning-artifacts/`

OLD:

```text
Documentation drift is tracked as an open retrospective action item.
Some historical artifacts still describe global-administrator correction as blocked or unavailable.
```

NEW:

```text
Audit current docs and planning references for stale blocked/readiness wording.
Current docs should say:
- Epic 4 command primitives exist.
- Global-administrator correction remains fail-closed until Story 5.7 verifies the enabled path.
- Historical retrospectives remain snapshots and are not rewritten, but may receive forward pointers if needed.
```

Rationale: Current implementation guidance must not tell the next Developer that global-admin correction is waiting for missing Epic 4 commands. Historical records should remain evidence snapshots.

### Proposal E: Keep Support-Safety as a Standing Gate

Artifact: every audit or correction story touched after this proposal.

OLD:

```text
Support-safety action item remains open.
```

NEW:

```text
Carry support-safety into Story 5.7, Story 5.8, and the documentation sweep:
- no raw payloads
- no bearer tokens
- no decoded JWT contents
- no EventStore metadata
- no internal correlation ids or message ids in user-facing copy
- no stack traces
- no unsafe PII
- no undo/rollback/hidden edit language
```

Rationale: This is not a one-time task. It is a guard that must remain near every audit/correction change.

## 5. Implementation Handoff

Scope classification: Moderate.

Route to:

- Developer agent: implement Story 5.7, then Story 5.8 if approved.
- Technical Writer: run the documentation drift sweep.
- Test Architect: keep support-safety and prohibited terminology guards active for Story 5.7, Story 5.8, and documentation changes.

Developer responsibilities:

- Implement Story 5.7 before enabling global-administrator correction.
- Keep global-admin correction fixed to `system/global-administrators/global-administrators`.
- Preserve last-admin pre-submit blocking and backend `LastGlobalAdministrator` hard rejection.
- Confirm grant/remove correction from `GetGlobalAdministratorsAsync`, not tenant member projection.
- Link original/corrective proof from support-safe system-scope audit rows only.
- Implement Story 5.8 without weakening projection confirmation or proof lookup.

Technical Writer responsibilities:

- Audit current docs for stale global-admin correction wording.
- Leave historical retrospectives as snapshots unless a forward pointer is needed.
- Ensure current docs distinguish tenant-domain recovery from global-administrator recovery.

Test Architect responsibilities:

- Require focused tests for fixed-scope global-admin routing, projection confirmation, last-admin safety, proof lookup, terminal focus, and no unsafe copy.
- Keep static/resource checks for prohibited recovery wording and support-safety markers.

Success criteria:

- Global-administrator correction remains fail-closed until Story 5.7 passes.
- Story 5.7 verifies fixed scope, projection confirmation, last-admin safety, safe rejection copy, and proof links.
- Story 5.8 removes duplicate projection refresh without removing projection-confirmed success or proof lookup.
- Current docs no longer imply global-admin correction is blocked by missing Epic 4 command primitives.
- Support-safety and prohibited recovery terminology guards remain active.

## 6. Checklist Results

- [x] 1.1 Triggering source identified: Epic 5 retrospective refresh, dated 2026-06-29.
- [x] 1.2 Core problem defined: reconcile already-handled and remaining Epic 5 follow-through without reopening completed tenant-domain delivery.
- [x] 1.3 Evidence gathered from retro, sprint status, PRD, epics, architecture, UX, existing proposals, and correction UI code.
- [x] 2.1 Current epic assessed: Epic 5 remains in-progress because Story 5.7 is ready-for-dev.
- [x] 2.2 Epic-level change identified: optional Story 5.8 for projection-refresh cleanup.
- [x] 2.3 Remaining planned epics reviewed: no future epic invalidation.
- [x] 2.4 No new epic required.
- [x] 2.5 No resequencing beyond routing Story 5.7 and adding Story 5.8 if approved.
- [x] 3.1 PRD checked: no PRD edit required.
- [x] 3.2 Architecture checked: no architectural decision change required.
- [x] 3.3 UX checked: no broad UX rewrite; focus, fail-closed, non-collapse, and support-safety remain active.
- [x] 3.4 Secondary artifacts checked: sprint status, existing proposals, story files, and docs.
- [x] 4.1 Direct adjustment is viable.
- [N/A] 4.2 Rollback is not useful.
- [N/A] 4.3 MVP review is not required.
- [x] 4.4 Recommended path documented.
- [x] 5.1 Issue summary created.
- [x] 5.2 Epic/artifact impacts documented.
- [x] 5.3 Recommendation documented.
- [x] 5.4 MVP impact and action plan documented.
- [x] 5.5 Handoff plan established.
- [x] 6.1 Checklist reviewed.
- [x] 6.2 Proposal accuracy reviewed against current artifacts.
- [x] 6.3 User approval obtained from Administrator on 2026-06-29.
- [x] 6.4 Sprint status update approved for Story 5.8 creation and tracking.
- [x] 6.5 Next steps and handoff plan documented.
