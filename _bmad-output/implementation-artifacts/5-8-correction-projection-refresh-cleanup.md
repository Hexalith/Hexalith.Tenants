---
created: 2026-06-29T16:07:11+02:00
source_proposal: _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-29-epic-5-retro-follow-through.md
---

# Story 5.8: Correction Projection Refresh Cleanup

Status: ready-for-dev

<!-- Created by the BMAD correct-course workflow after Administrator approval. -->

## Story

As an authorized operator,
I want correction status refresh to use a single authoritative projection refresh,
so that correction flows stay efficient without weakening projection-confirmed success or proof lookup.

## Acceptance Criteria

1. Given a correction status refresh runs after accepted or projection-pending command status, when projection confirmation is required, then the component uses one authoritative refreshed projection snapshot for confirmation, and it does not issue both a parent page projection refresh and a second direct tenant projection query for the same status refresh.
2. Given the refreshed projection confirms the intended correction, when proof lookup runs, then support-safe corrective audit proof lookup still executes after projection confirmation, and command status or SignalR alone still cannot prove correction success.
3. Given projection evidence is missing, stale, degraded, or unavailable, when the correction lifecycle renders, then the UI preserves last-confirmed projection evidence, fails closed where required, and does not show success.
4. Given correction status reaches confirmed or failed terminal lifecycle states, when the panel updates, then terminal focus behavior remains directed to the correction lifecycle region, and close/cancel launcher focus behavior remains unchanged.
5. Given this cleanup is implemented, when tests run, then focused component/state tests prove projection refresh call count, projection-confirmed success, delayed proof behavior, terminal focus, and no raw payload/token/correlation leakage.

## Tasks / Subtasks

- [ ] Rework correction status refresh to avoid duplicate projection reads. (AC: 1, 2, 3)
  - [ ] Inspect `CorrectionStartPanel.RefreshStatusAsync`, `OnProjectionRefreshRequested`, and `TenantAuditPage.RefreshTenantProjectionAsync`.
  - [ ] Replace the current parent-refresh-plus-direct-query pattern with one authoritative refreshed projection snapshot for the status refresh cycle.
  - [ ] Preserve `TenantCorrectionPreviewSnapshot.ConfirmProjection` as the projection truth gate.
  - [ ] Preserve corrective proof lookup after projection confirmation.

- [ ] Preserve correction lifecycle honesty and support safety. (AC: 2, 3, 5)
  - [ ] Keep command status and SignalR as lifecycle evidence or refresh nudges only.
  - [ ] Do not show success until projection evidence confirms the intended correction.
  - [ ] Keep `audit pending`, `audit delayed`, `audit unavailable`, and `missing support` distinct.
  - [ ] Do not expose raw payloads, bearer tokens, decoded JWT contents, EventStore metadata, internal correlation ids, message ids, stack traces, protected cursors, ETags, or unsafe PII.
  - [ ] Keep `undo`, `rollback`, and `hidden edit` out of visible and accessible copy.

- [ ] Preserve accessibility behavior. (AC: 4, 5)
  - [ ] Keep confirmed and failed terminal states focused on `data-testid="tenants-correction-lifecycle"`.
  - [ ] Keep close/cancel focus return behavior unchanged.
  - [ ] Preserve live-region politeness: assertive for failure/unable-to-verify/degraded states, polite for routine refresh and confirmed projection states.

- [ ] Add focused tests and validation. (AC: 1-5)
  - [ ] Add or update component tests proving only one authoritative tenant projection refresh/query is used during one correction status refresh.
  - [ ] Add or update tests proving projection-confirmed correction still succeeds from the refreshed snapshot.
  - [ ] Add or update tests proving delayed proof lookup still behaves honestly when no corrective audit row is found.
  - [ ] Add or update tests proving failed and confirmed terminal states keep lifecycle focus behavior.
  - [ ] Add or update support-safety/static tests if the implementation changes rendered copy or support references.
  - [ ] Run `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore`.
  - [ ] If `dotnet test` hits the known .NET 10 Microsoft.Testing.Platform/VSTest issue, run the xUnit v3 executable fallback for the UI test assembly and record the fallback.

## Dev Notes

### Story Source And Correct-Course Context

- This story comes from the 2026-06-29 Epic 5 retrospective refresh and approved correct-course proposal. The retrospective marks redundant correction projection refresh as open polish debt that must not weaken projection-confirmed success or proof lookup. [Source: `_bmad-output/implementation-artifacts/epic-5-retro-2026-06-29.md`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-29-epic-5-retro-follow-through.md`]
- Epic 5 tenant-domain audit and correction remain complete. This story is a cleanup, not a broad Epic 5 reopen. [Source: `_bmad-output/implementation-artifacts/epic-5-retro-2026-06-29.md#Epic Summary`]

### Existing Implementation To Inspect

- `CorrectionStartPanel.RefreshStatusAsync` currently calls `OnProjectionRefreshRequested` and then directly calls `QueryProjectionAsync` before `ConfirmProjection`. This is the likely duplicate refresh path. [Source: `src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor`]
- `TenantAuditPage.RefreshTenantProjectionAsync` refreshes the page-level tenant projection through `ITenantQueryGateway.GetTenantAsync`. Prefer reusing the refreshed projection evidence instead of issuing another direct read for the same status refresh. [Source: `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor`]
- `TenantCorrectionPreviewSnapshot.ConfirmProjection` owns the tenant-domain correction truth gate. Keep this gate intact and feed it one authoritative projection snapshot. [Source: `src/Hexalith.Tenants.UI/State/TenantAudit/TenantCorrectionPreviewSnapshot.cs`]
- Corrective proof lookup currently runs only after projection confirmation. Keep that sequencing. [Source: `src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor`]

### Boundaries

- Do not add backend endpoints, EventStore registrations, projection actors, direct state-store reads, generic recovery APIs, or FrontComposer shared code.
- Do not change command contracts, audit row wire shape, or domain events.
- Keep browser-side components behind the server-side BFF gateway; do not introduce browser backend calls or token storage.
- Keep tenant ids and user ids as caller-supplied strings; do not parse them as GUIDs or ULIDs.

### References

- Correct-course proposal: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-29-epic-5-retro-follow-through.md`
- Epic 5 retrospective refresh: `_bmad-output/implementation-artifacts/epic-5-retro-2026-06-29.md`
- Story 5.6: `_bmad-output/implementation-artifacts/5-6-preview-and-confirm-correction-with-linked-proof.md`
- Code: `src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor`
- Code: `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor`
- Code: `src/Hexalith.Tenants.UI/State/TenantAudit/TenantCorrectionPreviewSnapshot.cs`
- Tests: `tests/Hexalith.Tenants.UI.Tests/Components/CorrectionStartPanelTests.cs`
- Project rules: `_bmad-output/project-context.md`; `AGENTS.md`

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List

### Change Log

- 2026-06-29 - Created Story 5.8 context and marked it ready for development.
