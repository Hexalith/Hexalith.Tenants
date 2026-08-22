---
title: 'Remove Tenant Member WP-2A Proof and audit_available'
type: 'feature'
created: '2026-08-08'
status: 'done'
baseline_commit: 'bb85fadb149fed1fa00dfd9c8d3315df541566e8'
review_loop_iteration: 3
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-2-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** After remove-member projection confirmation, audit stays frozen at `AuditPending`, `IsAuditAvailable` is hard-false, proof-capability fail-closed is unwired, and WP-2A proof is never assembled — so FR12/WP-2A remains incomplete after 2.4a.

**Approach (2.4b only):** Refine remove reconciliation provenance, assemble minimum WP-2A proof from the existing authorized audit read path, surface distinct `audit_available` only on matching evidence, fail closed when proof capability is indeterminate, and cover proof-state recovery with tests. Keep 2.4a dialog/preview/dispatch unchanged.

## Boundaries & Constraints

**Always:**
- Reuse `ITenantQueryGateway.GetTenantAuditAsync` + `TenantAuditReceipt` / `TenantAuditSupportSafety`; no new preview/receipt/status endpoints.
- Keep `confirmed` distinct from `audit_available`; SignalR/status polling only nudge re-query; never invent available without matching `UserRemovedFromTenant` evidence (tenant + target + causal lower bound).
- Available proof fields only: support-safe actor, target, tenant, outcome, absolute timestamp, projection marker, reference — no raw narrative/payload/token/correlation/ETag/cursor/stack in UI, copy, announcements, logs, or component state.
- Wire `UnavailableReason.MissingAuditProof` into remove eligibility when proof capability is stale/missing/unknown; incomplete proof never silently upgrades.
- Confirmed access outcome survives pending/delayed/unavailable/denied audit; honest named recoveries (wait, refresh, inspect audit, escalate, continue read-only).
- EN/FR parity; `data-testid="tenants-remove-member-*"`.

**Ask First:**
- Extending audit-provenance confirmation beyond remove-member into shared add/change helpers.
- Rendering a different receipt primitive than existing `AuditEvidenceReceipt` / `TenantAuditReceipt`.

**Never:**
- Re-open 2.4a dialog/preview/friction/GA-standing work; new command contracts; browser-direct calls.
- Claiming Epic 5 browse UI is required for FR12; promising undo/rollback/`restore intended access` without correction capability.
- Confirming from acceptance/SignalR alone; treating pre-existing absence as newly confirmed success; editing events/projections.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Proof capability missing | Audit query/capability indeterminate before open | Remove unavailable with `MissingAuditProof` + recovery | No dispatch |
| Confirmed, proof match | Absence + provenance; matching removal audit row | Lifecycle `confirmed`; audit `audit_available` + WP-2A receipt | Keep confirmed if later audit flap |
| Confirmed, no match yet | Absence confirmed; audit empty/unmatched | Stay `audit_pending` (or delayed/unavailable per truth) + recovery | Never invent available |
| Already applied / UTV | Pre-existing absence or missing baseline | `already applied` / `unable to verify`; no fake available | MissingSupport / AuditUnavailable |
| Audit denied/fail | Query unauthorized/error after confirm | Honest unavailable/delayed + recoveries; confirmed intact | No silent success |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs` — `TenantCommandAuditState.AuditAvailable`; `TenantRemoveMemberCommandSnapshot` AttemptStartedAtUtc, ConfirmProjection(version OR audit provenance), ApplyRemovalProofMatch / ApplyRemovalProofQueryFailure, FindMatchingRemovalProof
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantMembershipCommandProvenance.cs` — version inequality + HasQualifyingAuditProvenance (>= attempt start)
- `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditAvailability.cs` — Available state; `IsAuditAvailable` only when Available
- `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditReceipt.cs` — WP-2A field set + support-safe redaction
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantQueryGateway.cs` (+ `TenantQueryGateway.GetTenantAuditAsync`) — existing audit read; command gateway stays submit+status only
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor` — QueryCorrectiveProofAsync match pattern adapted for `UserRemovedFromTenant`
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor` — TryAssembleRemovalProofAsync; receipt when Available; MissingAuditProof fail-closed
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor` — RemoveMember emits MissingAuditProof when query gateway unavailable
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceReceipt.razor` (+ `AuditAvailabilityState.razor`) — available proof UI
- Resources/tests: `TenantsResources*.resx` AuditAvailable keys; snapshot + flow matrix coverage; TenantDetailSurfaceTests registers capable query gateway
- Continuity: done 2.4a spec; sprint key `2-4-remove-tenant-member-with-complete-preview-and-proof`

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs` (+ provenance helper as needed) -- add `AuditAvailable`; refine remove confirm/reconcile so absence + version OR safe audit-provenance can confirm; add post-confirm proof-match → `AuditAvailable` without collapsing lifecycle states
- [x] `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditAvailability.cs` (+ receipt/availability components if needed) -- map Available; `IsAuditAvailable` true only with matching evidence; keep other command flows from inventing available
- [x] `src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor` (+ `MemberAccessReview.razor`) -- post-confirm `GetTenantAuditAsync` WP-2A assembly; wire `MissingAuditProof` fail-closed; render receipt only when available; honest pending/delayed/unavailable recoveries
- [x] `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` (+ `.fr.resx`) -- AuditAvailable + proof/recovery/unavailable copy parity
- [x] `tests/Hexalith.Tenants.UI.Tests/...` (`TenantRemoveMemberCommandSnapshotTests`, `RemoveTenantMemberFlowTests`, availability/gateway as needed) -- matrix: MissingAuditProof, confirmed≠available, match→available, unmatched stays pending, already-applied/UTV, SignalR cannot invent available
- [x] `_bmad-output/implementation-artifacts/sprint-status.yaml` -- advance Story 2.4 after 2.4b verification (canonical key incomplete until both 2.4a+2.4b pass)

### Review Findings

#### Code review (2026-08-22) — diff `28d32ca8~1..HEAD`, 4 layers (blind-hunter, edge-case-hunter, verification-gap, acceptance-auditor)

- [x] [Review][Decision] **RESOLVED (2026-08-22): SCOPE THE EXCLUSION TO MEMORIES ONLY.** `Hexalith.Tenants.slnx` had opted 26 `references/*` projects (Commons, EventStore, FrontComposer, Memories) out of the Release build via `<Build Solution="Release|*" Project="false" />`, added fresh in this range (0→26 vs `28d32ca8~1`) with no stated reason beyond the known `Hexalith.Memories` CS0618×3/SER301 failure. Reverted the exclusion for Commons/EventStore/FrontComposer (20 projects) and re-verified: `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore` -- Build succeeded, 0 Warning(s), 0 Error(s). Only `references/Hexalith.Memories` (8 projects) remains excluded, matching the actual known blocker. [Hexalith.Tenants.slnx:1]
- [x] [Review][Decision] **RESOLVED (2026-08-22): MIRROR SPEC-2-4'S PRECEDENT.** None of the 7 undeclared `references/` pointer moves are in the reviewed diff (`28d32ca8~1..HEAD`) — all are pre-existing drift relative to spec-2-4b's older `baseline_commit` (2026-08-08). Spec-2-4 (same story, same sprint key) already resolved this exact question in its own chunk-A decision: declare the one story-owned move (`EventStore`, via commit `d3f74f58`), accept the other 6 as external dependency drift. Applied the same declaration to this spec's new `## File List` section below; the other 6 remain undeclared here too, matching spec-2-4's accepted stance — `validate-story-gitlinks.py` still exits 1 for those 6, which is expected and already accepted, not a new gap. [_bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md:1]
- [x] [Review][Decision] **DEFERRED (2026-08-22): ACCEPT CURRENT COPY FOR THIS PASS.** Blank `CommandSurfaceUnavailableReason` maps to `UnavailableReason.AggregateLocked`'s copy ("Another command is already in progress for this tenant"), a specific, potentially false cause when the real reason is e.g. a missing admission-gate registration with no actual contention (`MemberAccessReview.razor:660`). The fail-closed behavior is correct; only the wording is imprecise in this narrow edge case. Adding a new `UnavailableReason` value plus EN/FR localized copy is a real wording/UX decision better suited to its own pass than folded into this review — deferred rather than fixed now. [src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor:660] — deferred, needs dedicated copy/UX pass

- [x] [Review][Patch] `Continue read-only` can silently reconstruct the removal preview via a parent-render race — `RemoveTenantMemberFlow.razor`'s continue-read-only handler calls `SetSnapshot(Idle())` before `await SetCommandActivityRaisedAsync(false)`; that await yields through `MemberAccessReview.HandleCommandActivityLeaseAsync` → `TenantDetailPage.SetCommandActivityLeaseAsync` → `RenderIfAliveAsync()`, and because `OnCloseRequested` (which clears `_activeRemoveMemberUserId`) only runs after the await, `OnParametersSet` can fire mid-await, see `_snapshot.State is Idle && IsPreviewComplete`, and re-preview — the exact regression this loop's own patch list claims to have closed. The only regression test stubs `CommandActivityLease` with `Task.FromResult(true)` (no real yield) so it can't catch this. Fix: gate the `OnParametersSet` re-preview condition on `!_dismissed`. [src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor:1148]
- [x] [Review][Patch] Row launcher buttons show no disabled state or reason when a sibling flow already holds the child command lease — `_childCommandLeaseOwner` (`MemberAccessReview.razor`) is checked by `OpenChangeRole`/`OpenRemoveMember` (silently no-op when set) but never fed into `ResolveFailClosedReasons`/`BuildActionSlots`, so the row's launcher renders fully enabled and a click while a sibling row's flow holds the lease does nothing — no `Disabled`, no reason text, no `aria-describedby`, no live-region announcement. Matches this same loop's own "silent return reads as broken control" fix pattern applied elsewhere. Fix: fold lease-owner contention into the reasons pipeline. [src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor:874]
- [x] [Review][Patch] Unhandled exception in the lease-release branch permanently strands `_childCommandLeaseOwner` — the release branch of `HandleCommandActivityLeaseAsync` has no try/catch, unlike the acquire branch; if `CommandActivityLease(false)` throws, `_childCommandLeaseOwner` is never cleared and stays set forever, blocking `OpenChangeRole`/`OpenRemoveMember` for that tenant. Current wired delegate can't reach the throw in practice today, but the asymmetry is a latent trap. Fix: wrap in try/finally. [src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor:797]
- [x] [Review][Patch] **RESOLVED (2026-08-22, pass 2):** No test covers the metadata audit-proof pagination's cross-page ambiguous match, mid-walk projection-version drift, or the new 50-page exhaustion cap — `GetUpdateMetadataAuditEvidenceAsync`'s `matches.Count > 1` and `projectionVersion` equality checks both correctly run every loop iteration, but only a single-match-on-page-2 case is tested; ambiguity split across two pages, version drift between pages, and exhausting `MetadataAuditProofMaximumPageCount` (50) are all untested on this correctness-critical fail-closed path. Closed by the `ambiguous-cross-page`/`version-drift-cross-page`/`page-exhaustion` cases added to `TenantDetailSurfaceTests.cs`; independently traced against `GetUpdateMetadataAuditEvidenceAsync` and confirmed each case exercises its intended fail-closed branch. [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:1810]
- [x] [Review][Patch] spec-2-4's frontmatter flips to `status: 'done'` while `sprint-status.yaml` keeps the story at `review` — the identical tracking contradiction this same diff explicitly fixes one section earlier for spec-3-2. Pick one status. [_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md:5]
- [x] [Review][Patch] Silent skip of null/malformed projection events with no logging — the `continue` on `evt is null` in the projection-apply loop has no diagnostic trace, unlike the fail-fast `ThrowIfCancellationRequested` pattern used elsewhere in the same method; a data-loss bug upstream could go unnoticed. [src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs:94]
- [x] [Review][Patch] Duplicated `"tenant-sequence:"` magic string across server and UI projects — independently defined as a prefix constant in both `TenantProjectionHandler.cs` (server) and `TenantMembershipCommandProvenance.cs` (UI) with no shared constant/contract type; a rename in one place would silently desynchronize confirmation logic. [src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs:32]
- [x] [Review][Patch] `Css_contains_focus_trap_and_narrow_layout_hooks` asserts against raw `.razor` source text (`File.ReadAllText(...).ShouldContain(...)`) instead of the rendered DOM via bUnit — proves the class name exists in source, not that it's actually applied at runtime. [tests/Hexalith.Tenants.UI.Tests/Components/RemoveTenantMemberFlowTests.cs:1]
- [x] [Review][Patch] Design Notes section of spec-2-4 not updated to document the new `tenant-sequence:<n>` ordered projection-version scheme or the bounded, projection-version-consistent metadata audit-proof pagination walk, even though both are now central to the story's confirmation guarantees. [_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md:260]
- [x] [Review][Patch] Concurrency test uses a real 5-second wall-clock `WaitAsync` timeout — `Member_access_review_reserves_child_lease_before_delayed_parent_and_rejects_queued_switches` waits on `parentEntered.Task.WaitAsync(TimeSpan.FromSeconds(5))`, a plausible source of CI flakiness under load with no diagnostic fallback if `parentCompletion` never resolves. [tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs:1]

#### Code review (2026-08-22, pass 2) — diff: uncommitted changes on `main` (staged+unstaged+untracked, closing pass-1 findings above), 4 layers (blind-hunter, edge-case-hunter, verification-gap, acceptance-auditor)

- [x] [Review][Patch] **APPLIED (2026-08-22, pass 2).** Null-projection-event skip warning can log multiple times per real occurrence — `ReadModelWritePolicy.UpdateAsync`'s transform is retried up to `DefaultMaxAttempts` (3) times on optimistic-concurrency conflict and is documented as MUST be idempotent because it "may run more than once (on each retry)"; the new `_logger.LogWarning(NullProjectionEventSkippedEvent, ...)` call sits inside that retried delegate, so a single real null-event occurrence can log up to 3 times, inflating log volume as a false frequency signal. [src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs:97]
- [x] [Review][Patch] **APPLIED (2026-08-22, pass 2).** Null-event skip branch has zero test coverage — no test constructs an event batch mixing a null entry with real events; the only existing null-related test (`ProjectAsync_AllNullEventBatchReturnsDefaultProjectionWithoutStateStoreAccessAsync`) is an all-null batch that returns before the loop even starts. A future edit reordering the null-check after `HasAlreadyAppliedAggregateSequence` would throw a `NullReferenceException` on `evt.SequenceNumber` for any mixed batch, uncaught by any test. [src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs:97-108]
- [x] [Review][Patch] **APPLIED (2026-08-22, pass 2).** Lease-release exception path is incomplete and untested — `HandleCommandActivityLeaseAsync`'s `finally` clears `_childCommandLeaseOwner` when `CommandActivityLease(false)` throws, but the exception itself still propagates unhandled; no caller (`RemoveTenantMemberFlow.SetCommandActivityRaisedAsync`/`ContinueReadOnlyAsync`, both on `ConfigureAwait(false)` chains with no try/catch) catches it, risking the known ConfigureAwait(false)-after-await Blazor circuit-crash pattern already present elsewhere in this codebase. No test forces the release delegate to throw, so neither the propagation risk nor the resulting owner-cleared-but-flow-still-open inconsistency is caught. [src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor:802-819]
- [x] [Review][Patch] **APPLIED (2026-08-22, pass 2).** The `!_dismissed` re-preview-race fix is unverified by any yielding test — the only regression test (`Continue_read_only_releases_activity_and_closes_the_remove_flow`) stubs `CommandActivityLease` with an already-completed `Task.FromResult(true)`, so `await SetCommandActivityRaisedAsync(false)` never actually yields and no parameter re-render can land mid-dismissal; the exact race this diff's `_dismissed = true` reordering claims to close is not exercised by any test. [src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor:528-534,1148-1153]
- [x] [Review][Patch] **APPLIED (2026-08-22, pass 2).** spec-2-4's tracking status still doesn't match `sprint-status.yaml` — this diff flips spec-2-4's frontmatter `status` from `done` to `in-progress`, but `sprint-status.yaml` keeps the story at `review` (line 92); the same class of contradiction this diff explicitly fixed for spec-3-2 remains here, just with a different mismatched value. [_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md:5]
- [ ] [Review][Patch] Stale unchecked checklist item above — the `[ ]` item ("No test covers the metadata audit-proof pagination's cross-page ambiguous match...") is now closed by this diff's own `ambiguous-cross-page`/`version-drift-cross-page`/`page-exhaustion` additions to `TenantDetailSurfaceTests.cs`; independently traced against `GetUpdateMetadataAuditEvidenceAsync` and confirmed each case exercises its intended fail-closed branch (ambiguous match, cross-page version drift, 50-page exhaustion). Flip that checkbox to `[x]`. [_bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md:88]
- [x] [Review][Patch] **APPLIED (2026-08-22, pass 2).** Tests still hardcode the literal `"tenant-sequence:"` instead of the new shared constant — `TenantProjectionHandlerTests.cs`, `TenantQueryFreshnessTests.cs`, and `TenantMembershipCommandProvenanceTests.cs` all hardcode the prefix rather than referencing `TenantProjectionVersionFormat.SequencePrefix`, so a future rename of the prefix would desync these tests silently instead of failing to compile. [src/Hexalith.Tenants.Contracts/Projections/TenantProjectionVersionFormat.cs:1]
- [x] [Review][Defer] Self-lock reason text — a row whose own flow currently holds `_childCommandLeaseOwner` also renders its own launcher buttons as `AggregateLocked` ("another command is already in progress"), which is imprecise for its own open flow; not newly introduced by this diff (the same row was already gated unavailable via `IsCommandSurfaceAvailable` once any child flow raises the parent lease) — same class of issue as the already-deferred `AggregateLocked`-copy item above; fold into that dedicated wording/UX pass rather than treating separately. [src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor:663] — deferred, fold into existing AggregateLocked copy pass
- [x] [Review][Defer] New `EventId(2001, "TenantProjectionNullEventSkipped")` is an unregistered magic literal with no cross-project collision check — no EventId registry exists in this codebase to conform to; establishing one is out of scope for this fix. [src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs:32] — deferred, no existing registry to conform to
- [x] [Review][Defer] New `TenantProjectionVersionFormat` type deliberately sits outside the namespaces `EventContractReferenceDocumentationTests` sweeps (documented intentionally in its own XML remarks) — sets a precedent that future non-contract public types in `Hexalith.Tenants.Contracts` can bypass the assembly's only doc-completeness governance check; worth a broader governance-scope discussion, not a defect in this diff. [src/Hexalith.Tenants.Contracts/Projections/TenantProjectionVersionFormat.cs:1] — deferred, governance-scope discussion
- [x] [Review][Defer] The Release-build `.slnx` topology revert (Commons/EventStore/FrontComposer re-enabled, Memories kept excluded) is validated only in review-findings prose (`dotnet build ... --configuration Release --no-restore` — 0/0), not captured as a reproducible command in this spec's formal `## Verification` section, which lists only a filtered `dotnet test`. [_bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md:123] — deferred, doc polish
- [x] [Review][Defer] The "blank `CommandSurfaceUnavailableReason` → `AggregateLocked` copy" deferred decision is now recorded independently, with drifting prose, in both `deferred-work.md` and this spec's own Review Findings section above — two sources of truth for one decision invite silent divergence on the next edit. [_bmad-output/implementation-artifacts/deferred-work.md:1362] — deferred, doc consolidation

**Acceptance Criteria:**
- Given proof capability is stale/missing/unknown, when remove availability is calculated, then the action fails closed with `MissingAuditProof` (or equivalent localized reason) and named recovery, without dispatch.
- Given projection-confirmed removal, when matching WP-2A audit evidence is assembled from `GetTenantAuditAsync`, then audit becomes `audit_available` with support-safe receipt fields only; without a match, confirmed remains distinct and audit stays pending/delayed/unavailable with honest recovery.
- Given already-applied, unable-to-verify, audit deny/error, or SignalR nudge, when reconciliation runs, then none invent `audit_available` or collapse acceptance/projection/proof into one success.
- Given EN/FR resources and focused tests, when verification completes, then matrix scenarios pass and Story 2.4 WP-2A completion criteria are met without new endpoints.

## File List

_Declared per the 2026-08-22 code-review gitlink decision, mirroring spec-2-4's chunk-A precedent._

- `references/Hexalith.EventStore`
  — declared per spec-2-4's chunk-A decision (commit `d3f74f58`); same story, same sprint key
  (`2-4-remove-tenant-member-with-complete-preview-and-proof`). This spec's own reviewed range
  (`28d32ca8~1..HEAD`) does not move this pointer; declared here for continuity with the sibling spec.

**Not declared here:** the other 6 pointers (`AI.Tools`, `Builds`, `Commons`, `FrontComposer`, `Memories`,
`PolymorphicSerializations`) are accepted external dependency drift per spec-2-4's resolved chunk-A
decision — not attributed to 2.4b. `validate-story-gitlinks.py` is expected to keep exiting 1 for these 6.

## Spec Change Log

## Design Notes

Mirror `CorrectionStartPanel.QueryCorrectiveProofAsync`: query audit with a causal lower bound from the attempt, match `UserRemovedFromTenant` + tenant + target, take newest qualifying row, map through `TenantAuditReceipt`. Prefer extending remove snapshot + flow first; only touch shared `TenantMembershipCommandProvenance` for a remove-safe audit-provenance confirm branch if version inequality alone cannot satisfy the Always clause. Do not flip `IsAuditAvailable` globally to true — gate on evidence-backed Available state.

## Verification

**Commands:**
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --filter "FullyQualifiedName~RemoveTenantMember|FullyQualifiedName~TenantRemoveMember|FullyQualifiedName~TenantAuditAvailability"` -- expected: matching tests pass

## Suggested Review Order

**Proof assembly entry**

- Post-confirm audit re-query + match → Available + receipt.
  [`RemoveTenantMemberFlow.razor:769`](../../src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor#L769)

- Render support-safe receipt only when evidence-backed Available.
  [`RemoveTenantMemberFlow.razor:189`](../../src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor#L189)

**Snapshot / provenance**

- Confirm with version advancement OR qualifying audit provenance.
  [`TenantCreateCommandModels.cs:822`](../../src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs#L822)

- Promote Available only on match; keep confirmed across flaps.
  [`TenantCreateCommandModels.cs:916`](../../src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs#L916)

- Causal lower-bound match for UserRemovedFromTenant rows.
  [`TenantCreateCommandModels.cs:980`](../../src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs#L980)

- Shared audit-provenance helper (>= attempt start).
  [`TenantMembershipCommandProvenance.cs:29`](../../src/Hexalith.Tenants.UI/State/TenantCommands/TenantMembershipCommandProvenance.cs#L29)

**Fail-closed + availability**

- List remove emits MissingAuditProof when query gateway unavailable.
  [`MemberAccessReview.razor:591`](../../src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor#L591)

- IsAuditAvailable true only for Available state.
  [`TenantAuditAvailability.cs:28`](../../src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditAvailability.cs#L28)

**Peripherals**

- Matrix coverage for pending / available / unauthorized audit paths.
  [`RemoveTenantMemberFlowTests.cs:518`](../../tests/Hexalith.Tenants.UI.Tests/Components/RemoveTenantMemberFlowTests.cs#L518)
