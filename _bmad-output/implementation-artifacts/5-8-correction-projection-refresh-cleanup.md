---
created: 2026-06-29T16:07:11+02:00
source_proposal: _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-29-epic-5-retro-follow-through.md
---

# Story 5.8: Correction Projection Refresh Cleanup

Status: review

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

- [x] Rework correction status refresh to avoid duplicate projection reads. (AC: 1, 2, 3)
  - [x] Inspect `CorrectionStartPanel.RefreshStatusAsync`, `OnProjectionRefreshRequested`, and `TenantAuditPage.RefreshTenantProjectionAsync`.
  - [x] Replace the current parent-refresh-plus-direct-query pattern with one authoritative refreshed projection snapshot for the status refresh cycle.
  - [x] Preserve `TenantCorrectionPreviewSnapshot.ConfirmProjection` as the projection truth gate.
  - [x] Preserve corrective proof lookup after projection confirmation.

- [x] Preserve correction lifecycle honesty and support safety. (AC: 2, 3, 5)
  - [x] Keep command status and SignalR as lifecycle evidence or refresh nudges only.
  - [x] Do not show success until projection evidence confirms the intended correction.
  - [x] Keep `audit pending`, `audit delayed`, `audit unavailable`, and `missing support` distinct.
  - [x] Do not expose raw payloads, bearer tokens, decoded JWT contents, EventStore metadata, internal correlation ids, message ids, stack traces, protected cursors, ETags, or unsafe PII.
  - [x] Keep `undo`, `rollback`, and `hidden edit` out of visible and accessible copy.

- [x] Preserve accessibility behavior. (AC: 4, 5)
  - [x] Keep confirmed and failed terminal states focused on `data-testid="tenants-correction-lifecycle"`.
  - [x] Keep close/cancel focus return behavior unchanged.
  - [x] Preserve live-region politeness: assertive for failure/unable-to-verify/degraded states, polite for routine refresh and confirmed projection states.

- [x] Add focused tests and validation. (AC: 1-5)
  - [x] Add or update component tests proving only one authoritative tenant projection refresh/query is used during one correction status refresh.
  - [x] Add or update tests proving projection-confirmed correction still succeeds from the refreshed snapshot.
  - [x] Add or update tests proving delayed proof lookup still behaves honestly when no corrective audit row is found.
  - [x] Add or update tests proving failed and confirmed terminal states keep lifecycle focus behavior.
  - [x] Add or update support-safety/static tests if the implementation changes rendered copy or support references.
  - [x] Run `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore`.
  - [N/A] If `dotnet test` hits the known .NET 10 Microsoft.Testing.Platform/VSTest issue, run the xUnit v3 executable fallback for the UI test assembly and record the fallback.

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

GPT-5 / Codex

### Debug Log References

- `dotnet restore tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -p:Configuration=Debug -p:UseHexalithProjectReferences=true`
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -m:1 --no-restore -p:UseHexalithProjectReferences=true`
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build -p:UseHexalithProjectReferences=true`
- `dotnet restore tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -p:Configuration=Release -p:HexalithEventStoreFromSource=true -p:HexalithMemoriesFromSource=true -p:UseHexalithProjectReferences=true`
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore -p:HexalithEventStoreFromSource=true -p:HexalithMemoriesFromSource=true -p:UseHexalithProjectReferences=true`
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-build -p:HexalithEventStoreFromSource=true -p:HexalithMemoriesFromSource=true -p:UseHexalithProjectReferences=true`

### Completion Notes List

- Replaced the correction panels' parent-callback-plus-direct-query refresh pattern with a single optional projection refresh provider.
- `TenantAuditPage` now passes its page-level tenant/global-administrator projection refresh methods to the correction panels and returns the refreshed snapshot to the panel.
- Tenant-domain and global-administrator correction panels still run projection confirmation through their snapshot truth gates before corrective proof lookup.
- Added component regressions proving provider-backed correction refresh uses one projection read while still linking corrective proof.
- Standard local Release restore without source flags remains blocked by `Hexalith.Commons.UniqueIds` `3.19.0` package availability on public NuGet; Release validation passed with explicit source-reference flags.
- 2026-06-30 (dev-story resume): Independently re-verified the in-progress state. The projection-refresh-provider cleanup and the HIGH terminal-failure-survives-re-render guard are already committed (`939bebc`); the remaining working-tree delta was doc bookkeeping plus a strengthened tenant-panel regression test (`Failed_correction_survives_a_parent_re_render_without_re_arming_submit` now re-renders with a consistent user-absent projection and asserts Submit stays present-but-disabled rather than conflating the rebuild with the correction applying). Debug build 0/0 and `Hexalith.Tenants.UI.Tests` 838/838 green confirmed live; added the missing `deferred-work.md` to the File List and moved the story to review.

### File List

- `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor`
- `tests/Hexalith.Tenants.UI.Tests/Components/CorrectionStartPanelTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorCorrectionPanelTests.cs`
- `_bmad-output/implementation-artifacts/5-8-correction-projection-refresh-cleanup.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Change Log

- 2026-06-29 - Created Story 5.8 context and marked it ready for development.
- 2026-06-29 - Removed duplicate correction projection refresh pattern, preserved projection-confirmed success and proof lookup, and moved story to review.
- 2026-06-30 - Resolved the HIGH deferred review finding (terminal-failure snapshot now survives parent re-render in both correction panels) and strengthened the tenant-panel regression test; re-verified the full UI suite 838/838 green; added `deferred-work.md` to the File List and moved story to review.

## Review Findings (Adversarial Code Review 2026-06-29)

Scope reviewed: story 5.8 File List (the projection-refresh-provider cleanup) — `TenantAuditPage.razor`, `CorrectionStartPanel.razor`, `GlobalAdministratorCorrectionPanel.razor` (+ tests). Three parallel layers (Blind Hunter, Edge Case Hunter, Acceptance Auditor). AC1–AC4 verified met; AC5 partially evidenced. 1 decision-needed, 2 patch, 7 defer, 4 dismissed as noise.

### Decision Needed

- [x] [Review][Decision] RESOLVED 2026-06-29 (Administrator): **Accept the bundling** — shipping the 5.7 global-administrator correction feature together with the 5.8 cleanup is approved; both story records are reconciled as one combined unit. No code moves and no record edits required. (Original finding retained below for traceability.)
- [x] [Review][Decision] Story 5.7's entire global-administrator correction feature is bundled into this 5.8 "cleanup" diff, and the 5.8 record is internally inconsistent — The new 402-line `GlobalAdministratorCorrectionPanel.razor`, `GlobalAdministratorCorrectionSnapshot.cs`, BFF authorization/command wiring, and 7 GA tests implement Story 5.7's ACs 1–10, yet 5.8 Completion Notes call this a "refactor" of pre-existing panels and the File List claims the GA panel while omitting its hard dependencies (`GlobalAdministratorCorrectionSnapshot.cs`, the `.resx` keys, `TenantCorrectionStartIntent.cs` AC8 edit). 5.8's ACs only mention "the tenant projection query"/"the component" — never platform authority. Decide: (a) re-attribute the GA feature to 5.7 and slim 5.8's record to the pure refresh-provider change, (b) accept the bundling and reconcile both story records, or (c) split the working tree. [auditor]

### Patch

- [x] [Review][Patch] FIXED 2026-06-29 — Confirm-time provider reused the page lazy-load early-return, stranding confirmation in ProjectionPending when paging away from global-admin rows while a correction was open [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:561] — `RefreshGlobalAdministratorsProjectionAsync` no longer short-circuits on the current audit page contents; it always re-reads the fixed platform-authority projection (correct for the confirm-time `ProjectionRefreshProvider`), and the "only when global-administrator rows are present" optimization moved to the page-load call site in `LoadAsync`. Build 0/0; 48 affected tests green. [blind+edge]
- [x] [Review][Patch] FIXED 2026-06-29 — Strengthened AC5 test evidence for the refactored tenant path [tests/Hexalith.Tenants.UI.Tests/Components/CorrectionStartPanelTests.cs] — added a terminal-focus assertion (`FocusTarget == Lifecycle`) to the provider test and added `Panel_provider_confirmed_correction_reports_audit_delayed_when_no_corrective_row_exists`, proving the delayed-proof path (provider confirms the projection, proof lookup runs but finds no corrective row → `AuditDelayed`, no proof link, no false success). The `ShouldNotContain("Success")` line in `TenantAuditPageTests` was intentionally left intact — it is a documented belt-and-suspenders check accompanying strong positive "not loaded" assertions, not a sole/tautological assertion. [blind+auditor]

### Deferred (pre-existing relative to the 5.8 cleanup — belong to story 5.7 / 5.6 panel logic)

- [x] [Review][Defer→FIXED] Terminal failure states (Failed/Rejected/UnableToVerify/Degraded) reset to a fresh submittable preview on parent re-render [src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor:220; src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:286] — HIGH. RESOLVED 2026-06-30. The `OnParametersSet` preservation guard listed only in-flight + Confirmed and omitted terminal failure states, so any parent re-render rebuilt the snapshot from `FromIntent`, discarding the rejection/`RejectionCode`, re-enabling Submit, and (for UnableToVerify/Degraded) destroying the tracking handle. **Both panels now preserve any existing snapshot when the intent is unchanged** (`_snapshot is not null && !intentChanged → return`), rebuilding only on a different intent or first set — which keeps every post-submission state (incl. UnableToVerify/AlreadyApplied) intact without freezing the pre-submission preview (role re-selection re-evaluates directly via `OnRoleChanged`). The GA panel carried this fix already (with regression test `Rejected_correction_survives_a_parent_re_render_without_re_arming_submit`); the tenant panel was fixed in this review and a matching `Failed_correction_survives_a_parent_re_render_without_re_arming_submit` test added. Full UI suite 838/838 green. [blind]
- [x] [Review][Defer] `ConfirmProjection` can confirm off a known-Stale projection (`ProjectionIsReadable` ignores `Freshness`) [src/Hexalith.Tenants.UI/State/TenantAudit/GlobalAdministratorCorrectionSnapshot.cs:327] — deferred, pre-existing. Start gate requires `Freshness=Current` but the confirm gate accepts `Kind ∈ {Ready,Stale,Empty}` regardless of freshness; possibly an intentional start-vs-confirm asymmetry, but verify it does not contradict the "Stale generally fails closed for mutation actions" rule. 5.7 snapshot logic. [edge]
- [x] [Review][Defer] Corrective-proof lookup may link the wrong historical audit row as proof [src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor:382; src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:507] — deferred, pre-existing. `FirstOrDefault` matches by event-type + target + tenant and excludes only `OriginalAuditReference`; with no recency tie-back a prior historical grant/role-change for the same user can be linked as "proof." Shared pattern (tenant panel since 5.6). Fix: order by recency / match the just-submitted command's timestamp. [blind]
- [x] [Review][Defer] Focus call lacks `JSDisconnectedException` guard [src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor:246; src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:312] — deferred, pre-existing. `OnAfterRenderAsync` → `await _lifecycleElement.FocusAsync()` has no try/catch; if the circuit drops/panel disposes between the terminal `SetSnapshot` and render, it throws unhandled. The page's own `OnAfterRenderAsync` already guards this. Pre-existing in the tenant panel (5.6). [edge]
- [x] [Review][Defer] New global-admin projection query in the page-load critical path is unguarded against non-gateway exceptions [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:334] — deferred, pre-existing. `LoadAsync` now awaits `RefreshGlobalAdministratorsProjectionAsync` with no surrounding try/catch; the gateway only swallows `EventStoreGatewayException`, so any other fault now breaks the whole audit page load. 5.7 wiring; realistic gateway-error case is caught. [edge]
- [x] [Review][Defer] Corrective-proof timestamp formatted with `CurrentCulture` instead of `InvariantCulture` [src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor:216] — deferred, pre-existing. UTC audit evidence should be culture-stable; harmless for EN/FR, latent under non-Gregorian/non-ASCII-digit cultures. Trivial one-line fix when 5.7 is finalized. [blind]
- [x] [Review][Defer] EventCallback→Func change drops the automatic parent re-render after the confirm refresh [src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:202; src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:550] — deferred, intentional-but-benign. The old `OnProjectionRefreshRequested` EventCallback re-rendered the parent; the new `ProjectionRefreshProvider` Func updates the parent field without re-rendering. All three layers agree it is currently benign (those fields feed only the panel). Watch-item: restore a parent render (or document) if any other parent UI later binds the refreshed snapshots. 5.8-introduced. [blind+edge+auditor]

### Dismissed (4, analyzed — not noise carried forward)

- ETag/304 "confirmation regression" (Edge iii / Blind #4) — DISMISSED after verifying the gateway 304 path (`TenantQueryGateway.cs:54-66`): on `IsNotModified` it returns `previous with {…}`, i.e. the same pre-command detail a forced-fresh null-ETag read also returns during the projection-lag window. Both old and new code stay ProjectionPending until the projection catches up; fail-closed preserved. No real regression.
- `EventCallback`→`Func` "public contract break" (Blind #6) — DISMISSED: `CorrectionStartPanel` is an app-internal component (not a published NuGet package), the change is compile-checked, and there are no external consumers.
- GA panel groups sibling sections without `FluentAccordion` (Auditor F) — DISMISSED: mirrors the existing approved `CorrectionStartPanel` pattern and is governed by `DomainUiFluentConformanceTests`; out-of-scope 5.7 surface.
