# Sprint Change Proposal: Terminal Correction Focus Handling

Date: 2026-06-29
Project: tenants
Change trigger: Epic 5 retrospective follow-up item `epic-5-retro-2026-06-29-terminal-focus`.

## 1. Issue Summary

The Epic 5 retrospective identified an accessibility gap in the tenant-domain correction panel: confirmed and failed in-panel correction lifecycle states did not programmatically move focus to the lifecycle region. Close and cancel focus return already existed, but terminal command outcomes could leave keyboard users without an intentional focus target after the panel state changed.

Evidence:

- `_bmad-output/implementation-artifacts/epic-5-retro-2026-06-29.md` action item 2: "Add tested terminal-state focus handling for correction confirmed and failed lifecycle states."
- `CorrectionStartPanel.razor` rendered a focusable lifecycle region but did not consume `FocusTarget` with `FocusAsync`.
- `TenantCorrectionPreviewSnapshot.ApplySubmissionFailure` targeted `Refresh` for failed submissions even when no command tracking existed and the refresh action was disabled.

## 2. Impact Analysis

Epic impact: Epic 5 remains complete; this is a bounded follow-up polish/accessibility correction for FR24/FR25 correction flows.

Story impact: Story 5.6 is affected because it owns correction preview, command submission, projection confirmation, and linked proof behavior. No new story is required.

Artifact conflicts: PRD and architecture remain valid. UX accessibility requirements already require focus return and complete-or-exit behavior; this change brings implementation closer to those requirements.

Technical impact: UI-only change in `Hexalith.Tenants.UI`; no backend endpoint, DTO, command contract, projection, persistence, or FrontComposer shared abstraction changes.

Checklist status:

- [x] Trigger and context identified.
- [x] Epic impact assessed as minor, Epic 5 follow-up.
- [x] PRD/architecture/UX impact checked; no artifact rewrite required.
- [x] Direct adjustment selected.
- [x] Verification completed with local EventStore and Commons source references forced for the test run.

## 3. Recommended Approach

Recommended path: Direct Adjustment.

Rationale: The behavior is local to the correction panel and its state model. It can be fixed by honoring terminal lifecycle focus in the existing component and by making failed correction submissions target the lifecycle region instead of a disabled refresh path. This preserves projection-confirmed success, support-safe copy, close/cancel focus return, and the no-history-mutation recovery model.

Effort: Low.
Risk: Low.
Timeline impact: None beyond rerunning UI tests once the shared dependency restore path is healthy.

## 4. Detailed Change Proposals

Story: 5.6 Preview and Confirm Correction with Linked Proof
Section: Accessibility / Focus Behavior

OLD:

- The correction lifecycle region was focusable but no in-panel after-render focus scheduling consumed terminal lifecycle targets.
- Failed submission snapshots used `TenantCommandFocusTarget.Refresh`, even when no status tracking existed.
- Tests asserted correction state/proof but not terminal-state focus behavior.

NEW:

- `CorrectionStartPanel.razor` captures the lifecycle region with `@ref`, schedules after-render focus for terminal `Confirmed` and `Failed` states, and calls `FocusAsync` on the lifecycle region.
- `TenantCorrectionPreviewSnapshot.ApplySubmissionFailure` maps failed correction submissions to `TenantCommandFocusTarget.Lifecycle`.
- `CorrectionStartPanelTests` observes focus interop after confirmed and failed terminal states.
- `TenantCorrectionPreviewSnapshotTests` locks the failed and confirmed lifecycle focus target contract.

Rationale: Keyboard users need an intentional destination when the correction command reaches a terminal state. The lifecycle region is the stable status and recovery context for those outcomes.

## 5. Implementation Handoff

Scope classification: Minor.

Route: Developer direct implementation.

Implementation tasks:

- Update correction panel focus scheduling.
- Update failed correction snapshot focus target.
- Add state and bUnit coverage for confirmed and failed terminal focus behavior.
- Rerun `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj` with local EventStore and Commons source references when the default package path cannot resolve the invalid Commons `3.19.0` package dependency.

Success criteria:

- Confirmed correction state moves focus intentionally to `tenants-correction-lifecycle`.
- Failed correction state moves focus intentionally to `tenants-correction-lifecycle`.
- Failed correction state does not point focus at a disabled refresh action when no tracking handle exists.
- Close/cancel launcher focus return remains unchanged.

Verification:

- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -p:HexalithEventStoreFromSource=true -p:HexalithEventStoreRoot=/home/administrator/projects/hexalith/tenants/references/Hexalith.EventStore -p:UseHexalithProjectReferences=true -p:HexalithCommonsRoot=/home/administrator/projects/hexalith/tenants/references/Hexalith.Commons -p:HexalithCommonsFromSource=true`
- Result: Passed, 779 tests.
