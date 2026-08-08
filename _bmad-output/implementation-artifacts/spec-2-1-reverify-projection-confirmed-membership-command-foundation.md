---
title: 'Reverify Projection-Confirmed Membership Command Foundation'
type: 'feature'
created: '2026-08-08'
status: 'done'
baseline_commit: '222d5ac614e182a5eefdc3fd282a5bfc14f075e9'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-2-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Membership commands already ship through a shared gateway and lifecycle, but confirmation can treat a pre-existing matching projection as success, SignalR never nudges in-flight command state, reconnect/retry mint a new `messageId`, and locking is a page-bool rather than `(circuit, AggregateIdentity)` — so acceptance or a live refresh can be mistaken for completed access change.

**Approach:** Reverify and harden the existing shared membership command foundation used by Stories 2.2–2.4: provenance-qualified confirmation, honest SignalR-as-nudge wiring, idempotent attempt identity across reconnect/retry, AggregateIdentity-scoped admission, and BFF-backed command-surface availability — without inventing a new stack or absorbing create-tenant (Epic 3).

## Boundaries & Constraints

**Always:**
- Keep server-side BFF egress only (`ITenantCommandGateway` → fixed `POST /api/v1/commands` + correlation status lookup).
- Preserve distinct lifecycle vocabulary: submitted/request-sent, accepted, projection-pending, confirmed, already-applied, rejected, failed, degraded, unable-to-verify, audit-pending, audit-available.
- `confirmed` requires command-specific postcondition **and** projection-version advancement or safe command-specific audit provenance newer than a captured pre-submit baseline; otherwise stay pending or become `unable to verify`.
- Same logical attempt reuses its `messageId`/correlation tracking; mint a new ULID only for a deliberate new attempt.
- Lock scope is `(interactive circuit, AggregateIdentity)` through terminal evidence; unrelated aggregates may proceed; no bulk/toast/multi-row dispatch.
- Fail closed on invalid/indeterminate validation, freshness, authorization reflection, or lifecycle support with localized inline unavailable reason.
- Support-safe mapping only: no Problem Details, payloads, tokens, internal correlations, metadata, ETags, cursors, or stack traces in UI state/output/logs.
- Historical create-tenant evidence may be cited; create-tenant product behavior stays Epic 3.
- EN/FR whole-string parity and stable `data-testid` contracts remain required.

**Ask First:**
- Changing FrontComposer `CommandExecutionAdmissionGate` (circuit-global) or any `references/Hexalith.FrontComposer` API to gain AggregateIdentity keys — prefer a Tenants-owned AggregateIdentity admission seam first.
- Persisting in-flight command intent across full InteractiveServer circuit disposal beyond in-memory reconnect within the same circuit.

**Never:**
- New backend preview/receipt/status/list endpoints or unversioned command aliases.
- Browser-direct EventStore/Tenants calls or reshaped command contracts.
- Confirming from acceptance, SignalR alone, unrelated projection churn, or pre-submit-matching state without provenance.
- Re-scoping create-tenant UX into this story.
- Editing/deleting events, projections, or inventing undo/rollback.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Provenance-qualified confirm | Postcondition matches **and** projection version/audit exceeds pre-submit baseline | `Confirmed` (+ audit-pending handoff) | N/A |
| Pre-existing match | Postcondition already true at/before baseline; no advancement | Not `Confirmed` — stay pending or `unable to verify` / `already applied` per command semantics | Offer refresh / retry status / continue read-only |
| SignalR during flight | Projection notification while Accepted/RequestSent | Triggers authoritative re-query + lifecycle nudge only; never success | Keep lock; assertive only if degraded/unable-to-verify |
| Reconnect same attempt | Circuit refresh/retry with stored tracking | Reuse `messageId`/correlation; no second dispatch | If tracking lost → `unable to verify`, no silent re-submit |
| Same-aggregate overlap | Second membership command while first in-flight | Unavailable with inline lock reason | First attempt retains lock through terminal evidence |
| Command surface down | `UnavailableTenantCommandGateway` / BFF disconnected | Surfaces fail closed before dispatch | Localized unavailable reason; focus recoverable |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs` -- membership + status contract; optional `messageId` on add/change/remove
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs` -- `AddUserToTenantAsync`/`ChangeUserRoleAsync`/`RemoveUserFromTenantAsync` reuse provided `messageId` or mint ULID when absent; status via correlationId
- `src/Hexalith.Tenants.UI/Services/Gateways/UnavailableTenantCommandGateway.cs` -- fail-closed gateway stand-in
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs` -- `IsCommandSurfaceConnected`; threaded into membership `IsCommandSurfaceAvailable`
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs` -- shared enums + membership snapshots with baseline provenance; `ConfirmProjection` requires postcondition **plus** projection-version advancement (or maps pre-existing/missing baseline to unable-to-verify / already-applied); `SignalRNudge` exists; confirm copy uses `SafeMessageKey`
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCommandFlowGuard.cs` -- `RetainsCommandActivity` for RequestSent/Accepted/ProjectionPending (not Degraded/UnableToVerify)
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantAggregateCommandAdmissionGate.cs` (+ `TenantCommandAggregateLock`) -- AggregateIdentity-shaped per-aggregate admission
- `src/Hexalith.Tenants.UI/State/TenantDetail/TenantDetailSnapshot.cs` -- `ProjectionVersion` for baseline / confirmation
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/AddTenantMemberFlow.razor` (+ Change/Remove siblings) -- attempt tracking + messageId reuse; baseline via `ResolveCurrentProjectionVersion()`; `HandleAuthoritativeRefreshNudgeAsync` for SignalR/status re-query without notify-alone confirm; continue-read-only for recoverable uncertain states
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor` -- forwards SignalR/read-refresh into in-flight membership flows via nudge + authoritative re-query
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor` -- captured aggregate lock key + `_commandInFlight` for surface availability; BFF connected gate; forwards read refresh into membership flows
- `src/Hexalith.Tenants.UI/Services/TenantReadRefreshSubscription.cs` -- SignalR coalesces into read refresh only
- `references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/State/PendingCommands/CommandExecutionAdmissionGate.cs` -- FC-CNC circuit-global admission (read-only unless Ask First); not AggregateIdentity-keyed
- Tests: `tests/Hexalith.Tenants.UI.Tests/State/TenantAddMemberCommandSnapshotTests.cs`, `TenantChangeRoleCommandSnapshotTests.cs`, `TenantRemoveMemberCommandSnapshotTests.cs`, `TenantCommandFlowGuardTests.cs`, `TenantAggregateCommandAdmissionGateTests.cs`, `CommandFlowGuardConformanceTests.cs`, `Services/Gateways/TenantCommandGatewayTests.cs`, `Components/AddTenantMemberFlowTests.cs` (+ Change/Remove), `Components/TenantDetailSurfaceTests.cs`
- Evidence only (do not re-own create product): `src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor`, historical `_bmad-output/implementation-artifacts/2-1-create-tenant-with-projection-confirmed-command-lifecycle.md`

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs` -- capture pre-submit projection-version/audit baseline on membership snapshots; harden `ConfirmProjection` to require postcondition **plus** baseline advancement (or safe audit provenance); map no-advancement / missing provenance to pending or `unable to verify` / `already applied` without false success -- closes provenance gap shared by add/change/remove
- [x] `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs` (+ `ITenantCommandGateway.cs` if needed) -- accept optional existing `messageId` for deliberate retry/reconnect of the same attempt; mint ULID only when absent -- stops double-dispatch on reconnect
- [x] `src/Hexalith.Tenants.UI/Components/Tenants/Members/AddTenantMemberFlow.razor` (+ `ChangeTenantMemberRoleFlow.razor`, `RemoveTenantMemberFlow.razor`) -- persist attempt tracking across refresh/reconnect within the circuit; call `ApplySignalRNudge` when parent signals projection refresh during flight; pass baseline into confirmation path -- wires nudge + idempotency at the flows
- [x] `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor` (+ `MemberAccessReview.razor` if bridging required) -- replace page-bool-only serialization with AggregateIdentity-scoped admission through terminal evidence; thread `BffComposition.IsCommandSurfaceConnected` into membership `IsCommandSurfaceAvailable`; forward SignalR/read-refresh into in-flight membership flows without confirming -- AD-12 lock + fail-closed surface
- [x] `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCommandFlowGuard.cs` -- keep retention semantics aligned with AggregateIdentity lock (in-flight through terminal) -- shared guard stays truthful
- [x] `tests/Hexalith.Tenants.UI.Tests/State/TenantAddMemberCommandSnapshotTests.cs` (+ change/remove snapshot tests) -- cover provenance-qualified confirm, pre-existing match non-success, SignalR cannot confirm -- matrix edge cases
- [x] `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs` -- cover messageId reuse vs new attempt -- idempotency
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/AddTenantMemberFlowTests.cs` (+ Change/Remove + `TenantDetailSurfaceTests.cs` / conformance) -- cover AggregateIdentity lock, BFF surface availability, SignalR nudge wiring, reconnect reuse -- page/flow integration
- [x] `_bmad-output/implementation-artifacts/sprint-status.yaml` -- move `2-1-reverify-projection-confirmed-membership-command-foundation` through ready-for-dev → in-progress → review/done with the story -- tracking only

**Acceptance Criteria:**
- Given a membership command reaches projection reconciliation, when the postcondition matches but projection version/audit does not advance past the pre-submit baseline, then the attempt is not `Confirmed`.
- Given status polling or SignalR fires during flight, when lifecycle updates, then only an authoritative re-query/nudge occurs and success styling/announcement is withheld until provenance-qualified confirmation.
- Given the same logical attempt is refreshed, reconnected, or retried with tracking available, when submit is considered, then the original `messageId` is reused and a second dispatch is not performed.
- Given one membership command is in-flight for a tenant aggregate, when another membership command for that same AggregateIdentity is attempted, then it stays unavailable with an inline lock reason through terminal evidence while unrelated aggregates may still proceed.
- Given `IsCommandSurfaceConnected` is false, when membership availability is calculated, then dispatch fails closed with a localized unavailable reason.
- Given focused gateway, snapshot, lock, nudge, reconnect, localization, accessibility, and support-safety tests run, when verification completes, then the non-collapse and confirmation invariants pass with recorded commands/results.

## Spec Change Log

- 2026-08-08: Patch pass — SignalR nudge no longer confirms from notification alone; baseline via ResolveCurrentProjectionVersion; ChangeRole baseline postcondition; continue-read-only + FlowGuard releases Degraded/UnableToVerify locks; SafeMessageKey localization; blank-TenantId / captured lock-key admission; whitespace messageId; expanded gateway/flow/page tests; Code Map refreshed.
- 2026-08-08: Implemented provenance-qualified membership confirmation, optional messageId reuse, AggregateIdentity-shaped admission, SignalR nudge wiring, and expanded flow-guard retention; verification suite passed (361 tests).

## Design Notes

Prefer Tenants AggregateIdentity admission over wiring FC `CommandExecutionAdmissionGate` as-is: that gate is circuit-global (any pending command blocks all), which is stricter than AD-12 and would serialize unrelated aggregates. Treat FC as a reuse reference unless the human authorizes FC API changes.

Baseline capture should use the authoritative detail `ProjectionVersion` already on `TenantDetailSnapshot` (and safe audit markers when present). Do not invent new backend provenance fields.

## Verification

**Commands:**
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --filter "FullyQualifiedName~TenantAddMemberCommandSnapshotTests|FullyQualifiedName~TenantChangeRoleCommandSnapshotTests|FullyQualifiedName~TenantRemoveMemberCommandSnapshotTests|FullyQualifiedName~TenantCommandGatewayTests|FullyQualifiedName~TenantCommandFlowGuardTests|FullyQualifiedName~CommandFlowGuardConformanceTests|FullyQualifiedName~AddTenantMemberFlowTests|FullyQualifiedName~ChangeTenantMemberRoleFlowTests|FullyQualifiedName~RemoveTenantMemberFlowTests|FullyQualifiedName~TenantDetailSurfaceTests"` -- expected: all matching tests pass (use xUnit v3 executable fallback if MTP/VSTest incompatibility hits)
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md` -- expected: exit 0; declare or revert any `references/` moves

## Suggested Review Order

**Provenance-qualified confirmation**

- Opaque baseline advancement is the confirmation gate for all membership snapshots.
  [`TenantMembershipCommandProvenance.cs:15`](../../src/Hexalith.Tenants.UI/State/TenantCommands/TenantMembershipCommandProvenance.cs#L15)

- Add-member confirm rejects pre-existing/missing baseline before version check.
  [`TenantCreateCommandModels.cs:362`](../../src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs#L362)

**SignalR nudge-only wiring**

- Parent refresh forwards nudge without confirming from the notification alone.
  [`MemberAccessReview.razor:710`](../../src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor#L710)

- Detail page only nudges while membership activity is retained.
  [`TenantDetailPage.razor:900`](../../src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor#L900)

**AggregateIdentity admission**

- Surface availability combines BFF connectivity, local in-flight, and aggregate lock.
  [`TenantDetailPage.razor:293`](../../src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor#L293)

- Acquire/release captures the lock key so TenantId changes cannot orphan admission.
  [`TenantDetailPage.razor:1295`](../../src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor#L1295)

- AggregateIdentity-shaped keys avoid EventStore identity validation on literal ids.
  [`TenantCommandAggregateLock.cs:16`](../../src/Hexalith.Tenants.UI/State/TenantCommands/TenantCommandAggregateLock.cs#L16)

**Idempotency and recovery**

- Optional messageId reuse mints a ULID only when absent/whitespace.
  [`TenantCommandGateway.cs:929`](../../src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs#L929)

- Lock retention stops before Degraded/UnableToVerify so continue-read-only works.
  [`TenantCommandFlowGuard.cs:16`](../../src/Hexalith.Tenants.UI/State/TenantCommands/TenantCommandFlowGuard.cs#L16)

- Continue read-only returns Idle and releases parent activity.
  [`AddTenantMemberFlow.razor:497`](../../src/Hexalith.Tenants.UI/Components/Tenants/Members/AddTenantMemberFlow.razor#L497)

**Tests**

- Snapshot provenance, SignalR non-confirm, and UnableToVerify mappings.
  [`TenantAddMemberCommandSnapshotTests.cs:1`](../../tests/Hexalith.Tenants.UI.Tests/State/TenantAddMemberCommandSnapshotTests.cs#L1)

- Gateway messageId reuse for add/change/remove.
  [`TenantCommandGatewayTests.cs:1051`](../../tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs#L1051)

- Page sibling lock and SignalR nudge integration.
  [`TenantDetailSurfaceTests.cs:1`](../../tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs#L1)
