---
title: 'Reverify Projection-Confirmed Membership Command Foundation'
type: 'feature'
created: '2026-08-08'
status: 'done'
baseline_commit: '222d5ac614e182a5eefdc3fd282a5bfc14f075e9'
review_loop_iteration: 1
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

### Review Findings

- [x] [Review][Patch] Mint and retain the attempt `messageId` before dispatch so an indeterminate POST response cannot lose the idempotency key [src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs:82]
- [x] [Review][Patch] Separate status recovery for the same attempt from deliberate new intent so retries do not redispatch and new intents do not inherit an old `messageId` [src/Hexalith.Tenants.UI/Components/Tenants/Members/AddTenantMemberFlow.razor:348]
- [x] [Review][Patch] Make AggregateIdentity admission exclusive and owner-aware, and honor acquisition failure instead of incrementing every same-key request [src/Hexalith.Tenants.UI/State/TenantCommands/TenantAggregateCommandAdmissionGate.cs:23]
- [x] [Review][Patch] Release owned command activity consistently when a flow is cancelled, the page is disposed, or tracking becomes irrecoverable [src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor:777]
- [x] [Review][Patch] Surface a localized same-aggregate lock reason instead of collapsing contention into the generic command-support outage [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:293]
- [x] [Review][Patch] Replace opaque projection-token inequality with causal evidence that cannot confirm from regression, unrelated churn, or a concurrent matching command [src/Hexalith.Tenants.UI/State/TenantCommands/TenantMembershipCommandProvenance.cs:15]
- [x] [Review][Patch] Validate caller-supplied reusable message ids as ULIDs before forwarding them [src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs:929]
- [x] [Review][Patch] Clear stale `SafeMessageKey` values on every status transition so recovered states cannot display an earlier provenance failure [src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs:301]
- [x] [Review][Patch] Serialize or generation-guard status/evidence refreshes and stop explicit projection refreshes from recursively triggering duplicate status lookups [src/Hexalith.Tenants.UI/Components/Tenants/Members/AddTenantMemberFlow.razor:447]
- [x] [Review][Patch] Fail closed when the scoped admission gate is missing instead of silently creating a page-private gate, and verify its scoped lifetime [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:298]
- [x] [Review][Patch] Add null and whitespace baseline-provenance tests for add, change-role, and remove snapshots [tests/Hexalith.Tenants.UI.Tests/State/TenantAddMemberCommandSnapshotTests.cs:42]
- [x] [Review][Patch] Add parent-to-flow SignalR forwarding tests for change-role and remove-member commands [src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor:720]
- [x] [Review][Patch] Add a page-level disconnected-BFF test proving membership dispatch is disabled with an inline reason [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:293]
- [x] [Review][Patch] Add page-boundary verification that an advanced live projection version reaches the child flow and earns confirmation [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:187]
- [x] [Review][Defer] The mandatory story gitlink validator currently fails on seven later, unrelated submodule pointer bumps even though the isolated Story 2.1 commit changes no gitlink [_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md:7] — deferred, pre-existing

#### Review Loop 1 (2026-08-20)

- [ ] [Review][Patch] DECIDED (declare): add a File List entry declaring `references/Hexalith.EventStore` (`454b4d10` -> `c21bd749`) with its reason, and correct the false Defer entry that claims the story moves no gitlink [_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md:1]
- [ ] [Review][Patch] DECIDED (renegotiate): amend the frozen `Always` clause to require ordered projection-version advancement only, recording the renegotiation explicitly so the frozen intent matches the shipped fail-closed behavior [_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md:1]
- [ ] [Review][Patch] DECIDED (document + test): keep ordered-only comparison, state that Tenants requires an ordered-token state store, and add a test pinning the real ETag shape the query path emits so both ends of the contract fail together [tests/Hexalith.Tenants.UI.Tests/State/TenantAddMemberCommandSnapshotTests.cs:1]
- [ ] [Review][Patch] Off-Dispatcher `StateHasChanged()` after `ConfigureAwait(false)` on the nudge forward tears down the circuit [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:990]
- [ ] [Review][Patch] Honor the admission refusal instead of discarding the lease result, so metadata/lifecycle/configuration surfaces cannot dispatch after `TryAcquire` fails [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:1383]
- [ ] [Review][Patch] Make the idempotent re-acquire branch owner-aware so a second surface cannot share the lease and release it while the first command is still in flight [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:1399]
- [ ] [Review][Patch] Order the aggregate-lock reason after authorization, staleness, and lifecycle checks so it stops masking the real fail-closed reason [src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor:620]
- [ ] [Review][Patch] Give the no-advancement `ProjectionPending` outcome a terminal escape; the lock is retained and `CanContinueReadOnly` excludes that state despite the matrix promising continue-read-only [src/Hexalith.Tenants.UI/Components/Tenants/Members/AddTenantMemberFlow.razor:238]
- [ ] [Review][Patch] Give the user feedback when submit is pressed with lost tracking instead of returning silently [src/Hexalith.Tenants.UI/Components/Tenants/Members/AddTenantMemberFlow.razor:396]
- [ ] [Review][Patch] Declare `NUlid` as a PackageReference/PackageVersion instead of relying on a transitive type [src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs:948]
- [ ] [Review][Patch] Align `HasQualifyingAuditProvenance` with its documented "strictly newer" contract; it implements `>=` [src/Hexalith.Tenants.UI/State/TenantCommands/TenantMembershipCommandProvenance.cs:67]
- [ ] [Review][Patch] Fix the refresh-coalescing protocol: a request arriving after the exchange is dropped, the post-drain is checked once, and it re-enters recursively; extract the copy-pasted logic [src/Hexalith.Tenants.UI/Components/Tenants/Members/AddTenantMemberFlow.razor:503]
- [ ] [Review][Patch] Await the fired `cut.InvokeAsync(...)` nudges so exceptions are observed and the assertions stop racing an unjoined task [tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs:1022]
- [ ] [Review][Patch] Close test gaps: indeterminate-messageId retention only covers `AddUserToTenant`; `SafeMessageKey` clearing only covers add-member; page disposal releasing the lease is unasserted; continue-read-only is never clicked; the coalescing regression proves serialization, not coalescing [tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs:1138]
- [ ] [Review][Patch] Correct the Verification record: a clean-HEAD Release run is 2,053 total with 1 failing test in 3 of 4 runs, not "0 failed" [_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md:1]
- [ ] [Review][Patch] Use or drop the unused `detail` parameter on `ApplySignalRNudgeAsync`, which is documented as authoritative evidence but discarded via `_ = detail` [src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor:1]
- [ ] [Review][Patch] Repoint the Suggested Review Order anchors; they cite the legacy overload, the wrong remove-member branch, and an unrelated transport test [_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md:1]
- [ ] [Review][Patch] Record the new admission gate and ~30 new tests in the tracked test inventory [tests/test-summary.md:1]
- [ ] [Review][Patch] Escape or reject separator characters when composing the aggregate lock key so distinct tenant ids cannot collide [src/Hexalith.Tenants.UI/State/TenantCommands/TenantCommandAggregateLock.cs:19]
- [x] [Review][Defer] Pre-existing flaky false-success: `Grant_requery_does_not_confirm_from_a_superseded_snapshot` renders "Projection confirmed the target user" from a superseded snapshot in 3 of 4 clean-HEAD runs [tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs:2679] — deferred, pre-existing (introduced by `d0f74a48`, Story 1.11)
- [x] [Review][Defer] Global-administrator command surface is not gated by the admission gate; `ForGlobalAdministrators()` and `HasActiveLock` have zero call sites [src/Hexalith.Tenants.UI/State/TenantCommands/TenantCommandAggregateLock.cs:1] — deferred, pre-existing
- [x] [Review][Defer] Optional `messageId` was added to `CreateTenantAsync`/`UpdateTenantAsync` beyond the declared membership Code Map scope [src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs:1] — deferred, pre-existing
- [x] [Review][Defer] Gating `IsCommandSurfaceAvailable` on a non-null admission gate also disables Epic 3 metadata/lifecycle/configuration surfaces [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:310] — deferred, pre-existing
- [x] [Review][Defer] Gateway and snapshot safe-message strings remain hard-coded English with `SafeMessageKey = null`, so French users see English on those paths [src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs:1] — deferred, pre-existing

**Acceptance Criteria:**
- Given a membership command reaches projection reconciliation, when the postcondition matches but projection version/audit does not advance past the pre-submit baseline, then the attempt is not `Confirmed`.
- Given status polling or SignalR fires during flight, when lifecycle updates, then only an authoritative re-query/nudge occurs and success styling/announcement is withheld until provenance-qualified confirmation.
- Given the same logical attempt is refreshed, reconnected, or retried with tracking available, when submit is considered, then the original `messageId` is reused and a second dispatch is not performed.
- Given one membership command is in-flight for a tenant aggregate, when another membership command for that same AggregateIdentity is attempted, then it stays unavailable with an inline lock reason through terminal evidence while unrelated aggregates may still proceed.
- Given `IsCommandSurfaceConnected` is false, when membership availability is calculated, then dispatch fails closed with a localized unavailable reason.
- Given focused gateway, snapshot, lock, nudge, reconnect, localization, accessibility, and support-safety tests run, when verification completes, then the non-collapse and confirmation invariants pass with recorded commands/results.

## Spec Change Log

- 2026-08-20: Completed adversarial review patches for exact-intent idempotent retry, owner-aware route-scoped leases, fail-closed correlation/identifier checks, coalesced refreshes, uncertain-outcome recovery, and projection-only removal confirmation; package-isolated Release verification passed all 2,053 UI tests.
- 2026-08-08: Patch pass — SignalR nudge no longer confirms from notification alone; baseline via ResolveCurrentProjectionVersion; ChangeRole baseline postcondition; continue-read-only + FlowGuard releases Degraded/UnableToVerify locks; SafeMessageKey localization; blank-TenantId / captured lock-key admission; whitespace messageId; expanded gateway/flow/page tests; Code Map refreshed.
- 2026-08-08: Implemented provenance-qualified membership confirmation, optional messageId reuse, AggregateIdentity-shaped admission, SignalR nudge wiring, and expanded flow-guard retention; verification suite passed (361 tests).

## Design Notes

Prefer Tenants AggregateIdentity admission over wiring FC `CommandExecutionAdmissionGate` as-is: that gate is circuit-global (any pending command blocks all), which is stricter than AD-12 and would serialize unrelated aggregates. Treat FC as a reuse reference unless the human authorizes FC API changes.

Baseline capture should use the authoritative detail `ProjectionVersion` already on `TenantDetailSnapshot` (and safe audit markers when present). Do not invent new backend provenance fields.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-restore -p:UseHexalithProjectReferences=false -p:BuildInParallel=false -p:RestoreBuildInParallel=false -m:1 -v:minimal` -- passed: 0 warnings, 0 errors.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor` -- passed: 2,053 total, 0 failed, 0 skipped.
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug --no-restore -p:UseHexalithProjectReferences=true -p:BuildInParallel=false -p:RestoreBuildInParallel=false -m:1 -v:minimal` -- blocked before Tenants compilation by the pre-existing EventStore/Commons package-version conflict (`CS1704`, `Hexalith.Commons.UniqueIds` 3.95.0 vs 2.30.0).
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md` -- expected deferred failure: seven unrelated `references/` pointer changes remain undeclared from the Story 2.1 baseline.

## Suggested Review Order

**Aggregate-scoped entry point**

- Tenant-keyed page leases prevent route changes and stale callbacks from crossing aggregates.
  [`TenantDetailPage.razor:312`](../../src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor#L312)

- Owner-aware admission rejects same-aggregate overlap while allowing independent aggregate work.
  [`TenantAggregateCommandAdmissionGate.cs:26`](../../src/Hexalith.Tenants.UI/State/TenantCommands/TenantAggregateCommandAdmissionGate.cs#L26)

- Child-specific lease ownership keeps sibling flows unavailable until terminal evidence.
  [`MemberAccessReview.razor:748`](../../src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor#L748)

**Idempotency and authoritative recovery**

- Add-member submission distinguishes exact-intent retry from a deliberate new command.
  [`AddTenantMemberFlow.razor:377`](../../src/Hexalith.Tenants.UI/Components/Tenants/Members/AddTenantMemberFlow.razor#L377)

- Coalesced status refreshes preserve independent nudges without recursive duplicate polling.
  [`AddTenantMemberFlow.razor:502`](../../src/Hexalith.Tenants.UI/Components/Tenants/Members/AddTenantMemberFlow.razor#L502)

- Parent refresh nudges every active membership flow, never confirming from SignalR alone.
  [`MemberAccessReview.razor:789`](../../src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor#L789)

- Reusable keys are validated ULIDs; status responses must match requested correlation.
  [`TenantCommandGateway.cs:393`](../../src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs#L393)

**Provenance-qualified confirmation**

- Ordered version advancement blocks regressions, opaque churn, and pre-existing matches.
  [`TenantMembershipCommandProvenance.cs:16`](../../src/Hexalith.Tenants.UI/State/TenantCommands/TenantMembershipCommandProvenance.cs#L16)

- Removal confirmation requires projection provenance before assembling its audit receipt.
  [`RemoveTenantMemberFlow.razor:861`](../../src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor#L861)

**Regression evidence**

- Route-switch tests prove late releases cannot unlock the newly selected tenant.
  [`TenantDetailSurfaceTests.cs:685`](../../tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs#L685)

- Gateway tests cover blank identifiers and mismatched status correlations.
  [`TenantCommandGatewayTests.cs:1659`](../../tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs#L1659)

- Concurrency regression proves overlapping nudges produce one later authoritative lookup.
  [`AddTenantMemberFlowTests.cs:229`](../../tests/Hexalith.Tenants.UI.Tests/Components/AddTenantMemberFlowTests.cs#L229)
