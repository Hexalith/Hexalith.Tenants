---
title: 'Disable or Enable Tenant with Complete Preview'
type: 'feature'
created: '2026-08-22'
status: 'done'
baseline_commit: '536e5c33230f2c2b04b80fb07ed0be631db9b5db'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-3-context.md'
  - '{project-root}/docs/tenants-ui-truth-state-and-action-availability-spec.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The legacy lifecycle flow renders a complete-looking preview but can confirm from target status alone after a status check or SignalR nudge, without causal projection advancement, and can lose its tracked attempt when the flow is dismissed or remounted. Some preview copy also describes disable as soft deletion instead of reversible availability control.

**Approach:** Preserve Story 3.3's four-action eligibility kernel and the existing enable/disable UI, while binding each logical attempt to fresh authorized preview evidence, a stable message id, exact-command event evidence, and an authoritative post-baseline projection proof. Retain pending attempts until reconciliation and keep command, projection, and audit truth distinct.

## Boundaries & Constraints

**Always:** Require server-reflected global-administrator authority, usable freshness/lifecycle, safe viewport, free aggregate admission, and all preview facts before submit. Preserve literal case-sensitive tenant ids and last-confirmed detail. Confirm only from the intended status plus ordered projection-version advancement backed by the tracked command's event evidence, or equally specific safe audit provenance. SignalR only requests reconciliation; pending work retains its aggregate lease and attempt identity.

**Ask First:** Adding an endpoint, changing domain contracts/aggregate behavior, touching a submodule, making audit proof mandatory, or broadening shared command infrastructure beyond the focused lifecycle seam.

**Never:** Implement hard delete/purge, Stories 3.5/3.6 configuration commands, optimistic status changes, confirmation from acceptance/status/SignalR/pre-existing state/unrelated projection churn, or exposure of tokens, payloads, correlations, ETags, cursors, raw metadata, unsafe values, or stack traces.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Eligible preview | Current authoritative detail, authority, support, admission, and safe viewport | Show tenant, current/intended state, operational impact, freshness marker, recovery, audit expectation, authorization, known consequences/unknowns; require exact tenant-id confirmation | Any missing or changed fact blocks with localized reason and refresh recovery |
| Accepted attempt | Fresh submit-time evidence and no retained attempt | Dispatch once with a stable message id; retain handle, baseline version, intent, and lease through reconciliation | Same intent/remount adopts the handle and polls; different intent remains blocked while pending |
| Proven outcome | Exact-command event evidence plus current matching-tenant projection at intended status and version strictly after baseline | Render Confirmed while audit remains independently pending/available | Missing, unchanged, regressed, stale, wrong-tenant, or unrelated evidence stays pending or becomes unable to verify |
| Exit or failure | Cancel/Escape before dispatch, or close/remount during pending work | Pre-dispatch exit sends nothing and restores launcher focus; pending work cannot be silently discarded | Rejection/transport/proof failure uses localized non-success copy and releases only terminal ownership |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Tenants.UI/State/TenantDetail/TenantHighImpactActionAvailabilityEvaluator.cs` and `TenantLifecycleAvailability.cs` -- reuse the dirty Story 3.3 fail-closed kernel; extend only submit-time evidence threading.
- `src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycle{ActionAvailability,CommandFlow}.razor` -- existing launch, ten-item preview, focus loop, lifecycle rendering, refresh, and close ownership.
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs` and `TenantMembershipCommandProvenance.cs` -- strengthen lifecycle snapshot with baseline, command-event evidence, and ordered proof; do not weaken other snapshots.
- `src/Hexalith.Tenants.UI/Services/Gateways/{ITenantQueryGateway,TenantQueryGateway}.cs` and `Components/Pages/TenantDetailPage.razor` -- add an unconditional, route-safe lifecycle proof carrying detail plus freshness/lifecycle/version from one read.
- `src/Hexalith.Tenants.UI/Services/Gateways/{ITenantCommandGateway,TenantCommandGateway,UnavailableTenantCommandGateway}.cs` and `State/TenantCommands/TenantLifecycleAttemptTracker.cs` -- retained message-id dispatch and circuit-local handle recovery.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources*.resx` and lifecycle CSS -- whole-string EN/FR availability-control, proof, dismissal, focus, and forced-colors behavior.
- `src/Hexalith.Tenants.Contracts/**` and `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs` -- read-only; commands, rejections, authorization, and events already exist.

## File List

- `docs/tenants-ui-truth-state-and-action-availability-spec.md`
- `references/Hexalith.Builds`
- `references/Hexalith.EventStore`
- `references/Hexalith.FrontComposer`
- `references/Hexalith.Memories`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/SetTenantConfigurationFlow.razor.css`
- `src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleActionAvailability.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleCommandFlow.razor`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs`
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantLifecycleAttemptTracker.cs`
- `src/Hexalith.Tenants.UI/State/TenantDetail/TenantHighImpactActionAvailabilityEvaluator.cs`
- `src/Hexalith.Tenants.UI/State/TenantDetail/TenantHighImpactActionEvidence.cs`
- `src/Hexalith.Tenants.UI/State/TenantDetail/TenantHighImpactReasonCategoryNames.cs`
- `src/Hexalith.Tenants.UI/State/TenantDetail/TenantLifecycleAvailabilityInput.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantLifecycleActionAvailabilityTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/State/TenantHighImpactActionAvailabilityTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/State/TenantLifecycleAttemptTrackerTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/State/TenantLifecycleAvailabilityTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/State/TenantLifecycleCommandSnapshotTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs`

## Completion Notes List

- The reviewed baseline-to-head range is not Story 3.4-only and already includes the user-authored root pointer movements for `references/Hexalith.Builds`, `references/Hexalith.EventStore`, `references/Hexalith.FrontComposer`, and `references/Hexalith.Memories`. Story 3.4 preserves those umbrella dependency-alignment movements and does not modify submodule content; the EventStore pointer is also the pinned source used to verify lifecycle status `MessageId` provenance.
- Lifecycle launch now requires a current projection version, while retained reconciliation resumes only with reflected authority and command connectivity. Retained attempts keep their stable identity through proof/read blockers, concurrent refreshes replay, and unavailable status polling terminalizes after three bounded attempts.
- Page-level coverage now exercises real launcher-to-confirmation wiring, route-safe proof cancellation, SignalR reconciliation, and tracker-owned aggregate lease reacquisition without redispatch.
- EN/FR whole-string recovery copy, governed automation categories, visible companion domain outcomes, and the narrow-layout focus target are aligned with the implemented behavior.

## Tasks & Acceptance

**Execution:**
- [x] `State/TenantCommands/{TenantCreateCommandModels,TenantLifecycleAttemptTracker}.cs` and `TenantMembershipCommandProvenance.cs` -- retain one logical attempt and require causal proof without changing sibling command semantics.
- [x] `Services/Gateways/{ITenantQueryGateway,TenantQueryGateway}.cs` and `Components/Pages/TenantDetailPage.razor` -- perform qualified unconditional proof reads with cancellation/generation guards and forward refresh nudges.
- [x] `Components/Tenants/Lifecycle/*.razor`, lifecycle CSS, command gateway interfaces/implementations, and `TenantsResources*.resx` -- revalidate preview at submit, reconcile retained attempts, block unsafe dismissal, and render localized support-safe states.
- [x] `tests/Hexalith.Tenants.UI.Tests/{State,Components,Services}` and `sprint-status.yaml` -- cover both directions, provenance failures, remount/close/lease behavior, preview selectors, EN/FR parity, and current tracking.

**Acceptance Criteria:**
- Given any eligibility fact is missing, stale, mismatched, or changes while preview is open, when enable/disable is evaluated or submitted, then the affected action fails closed before dispatch with a visible reason and named recovery.
- Given an accepted lifecycle attempt and arbitrary status, SignalR, projection, close, or remount events, when reconciliation runs, then exactly one logical command is tracked and success appears only from causal authoritative proof.
- Given enable and disable in English/French keyboard, narrow, and forced-colors contexts, when the flow is used or exited, then complete support-safe facts, stable selectors, non-color-only states, focus containment/return, and truthful audit handoff remain intact.

## Spec Change Log

- 2026-08-25: Applied all patch findings from the completed review pass and added focused plus page-level regression evidence; full UI suite and gitlink validation pass.

## Design Notes

Lifecycle proof is conjunctive: exact tracked-command event evidence AND intended authoritative status AND a strictly newer comparable tenant projection version. A SignalR notification may trigger that read but contributes no proof itself. Audit provenance is an optional equivalent only when it is uniquely command-specific and support-safe.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-restore -warnaserror -m:1 -nr:false` -- expected: zero warnings and errors.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` -- expected: all UI tests pass.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-3-4-disable-or-enable-tenant-with-complete-preview.md` -- expected: no undeclared gitlink movement.

## Initial Review Order

**Submit boundary and preview integrity**

- Revalidates captured authoritative facts and serializes dispatch before any command leaves the UI.
  [`TenantLifecycleCommandFlow.razor:489`](../../src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleCommandFlow.razor#L489)

- Rejects every preview/proof mismatch against the immutable preview snapshot.
  [`TenantLifecycleCommandFlow.razor:710`](../../src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleCommandFlow.razor#L710)

**Causal reconciliation and retained ownership**

- Keeps status monotonic and requires verified identity plus event evidence.
  [`TenantCreateCommandModels.cs:1978`](../../src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs#L1978)

- Confirms only intended authoritative state after a strictly newer comparable projection.
  [`TenantCreateCommandModels.cs:2103`](../../src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs#L2103)

- Prevents stale renders from replacing a newer retained logical attempt.
  [`TenantLifecycleAttemptTracker.cs:45`](../../src/Hexalith.Tenants.UI/State/TenantCommands/TenantLifecycleAttemptTracker.cs#L45)

**Proof and command identity boundaries**

- Fetches lifecycle proof independently from mutable page state.
  [`TenantQueryGateway.cs:296`](../../src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs#L296)

- Cancels route-stale proof reads before returning evidence to the flow.
  [`TenantDetailPage.razor:2075`](../../src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor#L2075)

- Verifies returned message and aggregate identity for lifecycle status.
  [`TenantCommandGateway.cs:432`](../../src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs#L432)

**Regression evidence and localized states**

- Exercises remount adoption without redispatch or dismissing retained ownership.
  [`TenantLifecycleActionAvailabilityTests.cs:354`](../../tests/Hexalith.Tenants.UI.Tests/Components/TenantLifecycleActionAvailabilityTests.cs#L354)

- Proves every changed preview fact blocks before activity and dispatch.
  [`TenantLifecycleActionAvailabilityTests.cs:931`](../../tests/Hexalith.Tenants.UI.Tests/Components/TenantLifecycleActionAvailabilityTests.cs#L931)

- Covers conjunctive proof, pending propagation, and terminal monotonicity.
  [`TenantLifecycleCommandSnapshotTests.cs:18`](../../tests/Hexalith.Tenants.UI.Tests/State/TenantLifecycleCommandSnapshotTests.cs#L18)

- Keeps EN/FR pending and rejection outcomes whole-string and support-safe.
  [`TenantsResources.resx:927`](../../src/Hexalith.Tenants.UI/Resources/TenantsResources.resx#L927)

## Review Findings

<!-- bmad-code-review 2026-08-25 — layers: blind-hunter, edge-case-hunter, verification-gap, acceptance-auditor (all completed) -->

- [x] [Review][Patch] **[Decision resolved: drop `RequestSent` from `RetainsAttempt`]** Retained `RequestSent` attempt can never reconcile, terminalize, or be dismissed — `RequestSent` sets `CorrelationId = null` (`TenantCreateCommandModels.cs:1947`) yet `RetainsAttempt` includes `RequestSent` (`:1871-1874`), so `SetSnapshot` remembers it in the tracker. `RefreshStatusCoreAsync` polls status only when `MessageId is not null && CorrelationId is not null` (`TenantLifecycleCommandFlow.razor:779`) and `ConfirmProjection` advances only from `ProjectionPending`, so an adopted `RequestSent` snapshot never reaches a terminal state. `CloseAsync` then refuses dismissal (`:867-871`) and the aggregate lease is deliberately retained across route change and dispose (`TenantDetailPage.razor:560-578`, `:2274-2292`), blocking every command on that tenant for the circuit's lifetime. Reachable when the circuit is interrupted between dispatch and result. Options: (a) drop `RequestSent` from `RetainsAttempt`; (b) allow status polling on `MessageId` alone; (c) bound retention with a deadline plus an explicit abandon affordance. Severity: high.
- [x] [Review][Patch] **[Decision resolved: bounded poll count, then terminal `UnableToVerify`]** Unbounded pending retention after 404 → `Pending` — `TenantCommandGateway.cs:411-413` now maps a 404 status lookup to `TenantCommandStatusResult.Pending(...)` instead of `Unknown`, and `ApplyStatus` leaves state untouched for `IsPending` (`TenantCreateCommandModels.cs:1987-1997`). No poll cap, elapsed-time bound, attempt counter, or user-facing abandon path exists anywhere in the change. A genuinely lost command therefore never terminalizes: `RetainsAttempt` stays true, dismissal stays blocked, and the lease is never released. Needs a product call on the bound and the recovery affordance. Severity: medium.
- [x] [Review][Patch] **[Decision resolved: gate resume on authority + connectivity; freshness/viewport stay bypassed since reconciliation is read-only]** `CanResumeRetained` bypasses every eligibility gate at the launcher — `IsActionDisabled` became `!CanResumeRetained(availability) && (!HighImpactAvailability(availability).IsEligible || !IsCommandSurfaceAvailable)` (`TenantLifecycleActionAvailability.razor:238-240`). Once an attempt is retained the button is enabled even when the surface is unauthorized, stale, or disconnected, and nothing re-checks authority on the resume path before it re-acquires the lease and starts polling. Resuming reconciliation of an already-dispatched command is arguably not a new high-impact action, but the current form is the opposite direction from AC1's "fails closed". Needs an intent call: gate resume on authority/connectivity, or document the exemption. Severity: medium.
- [x] [Review][Dismissed] **[Verified 2026-08-25 against pinned EventStore `1b2718c1`: all five status-write sites populate `MessageId` (`AggregateActor.cs:1532/1771/3188`, `SubmitCommandHandler.cs:342`, `ConcurrencyConflictExceptionHandler.cs:54`) and `CommandStatusResponse.cs:40` maps it onto the wire response. No deployment coupling.]** Status identity verification hard-requires a `messageId` the deployed status API may not emit — `TenantCommandGateway.cs:430-436` returns `Unknown` (→ terminal `UnableToVerify`) whenever `status.MessageId` is blank or mismatched, for any handle carrying an `AggregateId`. `TenantCommandStatusResponse.MessageId` was added in this change with a `= null` default, so if the deployed EventStore status endpoint does not yet return it, every lifecycle command ends `UnableToVerify` after succeeding. Fail-closed is correct per spec; the open question is deployment ordering. Severity: medium.
- [x] [Review][Patch] **[Decision resolved: extend §4.1 of the governed truth-state doc with the two non-blocking categories, and replace the magic strings with a shared enum/const]** `data-reason-category` now emits values outside the canonical six-category taxonomy — `TenantLifecycleActionAvailability.razor:251-258` returns the literals `"RetainedAttempt"` and `"InFlightOrCommandSurface"`; the attribute previously always carried a `TenantHighImpactUnavailableReason` member. `docs/tenants-ui-truth-state-and-action-availability-spec.md` §4.1 states the reason categories are "exactly these six". The literals are also duplicated unenforceably in `TenantLifecycleActionAvailabilityTests.cs`. Either extend the governed taxonomy or map these to existing members. Severity: low.
- [x] [Review][Patch] Launcher advertises enable/disable as available while the flow requires a `ProjectionVersion` it never checks, then blocks with no named recovery [src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleCommandFlow.razor:223] — `IsPreviewComplete` now requires `!string.IsNullOrWhiteSpace(ProjectionVersion)` on both branches (`:223`, `:231`), but `ProjectionVersion` is not part of `TenantHighImpactActionEvidence`, so `IsActionDisabled` at `TenantLifecycleActionAvailability.razor:238` ignores it. `TenantQueryGateway.cs:245-250` builds `TenantDetailSnapshot.Ready(..., result.Metadata?.ProjectionVersion)` with no guard, so a Ready + Current snapshot with a null version is reachable. The user gets an enabled button that opens a flow reporting `Tenants.Lifecycle.Unavailable.PreviewIncomplete` — a string with no recovery sentence at all — and submit is additionally blocked by `IsMatchingPreviewProof`. Violates AC1 ("fails closed before dispatch with a visible reason and named recovery"). Severity: high.
- [x] [Review][Patch] Broad `catch (Exception)` spans post-acceptance work and reports an accepted command as failed [src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleCommandFlow.razor:691] — the catch wraps not only the dispatch but also `SetSnapshot(_snapshot.Accepted(result))` and `await RefreshStatusAsync()` (`:665-667`). `RefreshStatusCoreAsync` deliberately rethrows `OperationCanceledException` from the proof read (`:838-841`), which the page raises on route change or cancellation. Any such fault after acceptance sets `State = Failed` → `HasTerminalOwnership` → `AttemptTracker.Forget` (`:426-429`) and releases the lease, asserting "the lifecycle command failed before its outcome could be verified" for a command the server accepted and may still complete. Violates "pending work cannot be silently discarded". Severity: high.
- [x] [Review][Patch] `TenantLifecycleCommandSnapshot.Blocked` discards the attempt and disables the recovery its own copy names [src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs:1862] — `Blocked` builds a fresh record, so `Intent`, `MessageId`, `PreviewProjectionVersion`, `LastConfirmedProjection`, and `LastConfirmedStatus` are all lost. This story adds three call sites (`TenantLifecycleCommandFlow.razor:543`, `:593`, `:601`). Consequences: `CanRefresh` requires `Intent is not null` (`:274`) so the in-flow Refresh button turns off, while `Tenants.Lifecycle.Unavailable.PreviewChanged` and `.ProofRead` both name "Refresh" as the recovery; `PreviewDetail` (`:207`) silently reverts from the frozen preview to live `Detail`; the confirmed-status line renders `Unknown`. The story added `BlockedWithTracking` (`:2208`), which preserves state, so the preserving variant was available and unused here. Severity: medium.
- [x] [Review][Patch] `Blocked(...)` after `RequestSent` orphans the tracker entry permanently [src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleCommandFlow.razor:634] — the `CommandGateway is null` branch runs after `SetSnapshot(_snapshot.RequestSent(...))` has already remembered the attempt, and `Blocked` yields `Intent = null`. `SetSnapshot`'s cleanup branch is `else if (_snapshot.HasTerminalOwnership && _snapshot.Intent is not null)` (`:426`), so `Forget` is never called and the tracker retains a never-dispatched `RequestSent` attempt. `CanResumeRetained` then stays true forever at the launcher. Defensive branch, but it composes with the `RequestSent` decision item above. Severity: medium.
- [x] [Review][Patch] A null or cancelled proof read is reported as "the preview changed" [src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleCommandFlow.razor:598] — `TenantDetailPage.GetLifecycleProjectionEvidenceAsync` returns `null` when `BeginLifecycleProof` refuses (disposed, route changed) or the read is cancelled. That is not an exception, so it skips the `ProofRead` branch (`:590-595`) and falls into `IsMatchingPreviewProof(null) == false` → `Tenants.Lifecycle.Unavailable.PreviewChanged` ("Tenant identity, lifecycle, freshness, or projection evidence changed while the preview was open"), which asserts a tenant fact changed when nothing was read. The dedicated `Tenants.Lifecycle.Unavailable.ProofRead` string exists and is used only for thrown exceptions. Severity: medium.
- [x] [Review][Patch] Overridden reason renders beside an unoverridden recovery, producing mismatched pairs [src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleActionAvailability.razor:100] — `HighImpactReason` and `ReasonCategory` both override for the `RetainedAttempt` and `InFlightOrCommandSurface` cases (`:242-258`), but the adjacent recovery paragraph still renders `Localizer[highImpact.RecoveryKey]` unchanged. A retained pending attempt on eligible evidence shows "This lifecycle attempt is still pending. Open it to resume authoritative reconciliation without submitting again." beside "No recovery is required."; on stale evidence it shows the same resume text beside "Refresh the authoritative tenant data and review the last-confirmed facts." Severity: medium.
- [x] [Review][Patch] Refresh gate drops a concurrent SignalR reconciliation with no replay [src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleCommandFlow.razor:754] — `RefreshStatusAsync` does `if (Interlocked.Exchange(ref _refreshGate, 1) != 0) { return; }` and sets no pending flag. An inbound nudge (`HandleAuthoritativeRefreshNudgeAsync` → `RefreshStatusAsync(requestProjectionRefresh: false)`) arriving while a refresh is running is discarded, so the confirming projection advance can be missed and the attempt stays pending until the user refreshes by hand. Related: `CanRefresh` reads `_refreshGate` (`:275`) but the gate is written without any `StateHasChanged`, so the button's disabled state does not reliably track it. Severity: medium.
- [x] [Review][Patch] The whole page→flow lifecycle wiring is unverified; four separate mutations leave the 2297-test suite green [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:177] — (a) delete `ProjectionVersion="@_snapshot.ProjectionVersion"` at `:177` and no test fails, because every component test injects its own value and no page test opens the flow; (b) `GetLifecycleProjectionEvidenceAsync` (`:2075-2096`) and its `ResetLifecycleProofScope`/`BeginLifecycleProof`/`CanApplyLifecycleProof` guards (`:1843-1900`) have zero test references — inverting `BeginLifecycleProof` to always return `null` blocks every submit with nothing failing; (c) `TenantAggregateCommandAdmissionGate.IsOwnedBy` (`:102-112`) is referenced only at `TenantDetailPage.razor:2173` and by no test, though it is the sole path by which a remounted flow re-adopts its retained lease — dropping the clause turns every remount into `TrackingMismatch`; (d) the SignalR forward at `:1123-1126` never executes in the suite, because the notifier-driven page tests run with no in-flight command. Add page-level tests that drive the launcher → preview → submit path through `TenantDetailPage`. Severity: medium.
- [x] [Review][Patch] `TenantLifecycleAttemptTracker` registration and circuit scoping are asserted nowhere [src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs:98] — all three production resolvers use nullable `GetService` (`TenantLifecycleCommandFlow.razor:288`, `TenantLifecycleActionAvailability.razor:213`, `TenantDetailPage.razor:414`), and every test registers its own `AddSingleton` instance. Deleting line 98 silently removes remount adoption in production; changing `TryAddScoped` to `TryAddSingleton` leaks one user's pending attempt into another user's circuit, since `Find(tenantId)` is keyed only by tenant id. Both pass the suite. `TenantsUiCompositionTests.Aggregate_command_admission_gate_is_scoped_to_a_circuit` (`:141-165`) is the exact pattern to mirror. Severity: medium.
- [x] [Review][Patch] Deleted snapshot coverage was not replaced [tests/Hexalith.Tenants.UI.Tests/State/TenantLifecycleCommandSnapshotTests.cs:1] — `Blocked_and_duplicate_lifecycle_states_are_assertive_non_success_states` was removed and nothing replaced it, so `TenantLifecycleCommandSnapshot.Blocked` and lifecycle `DuplicatePrevented` now have zero test coverage despite both being reachable from `SubmitCoreAsync` (and `Blocked` being implicated in three findings above). The per-status `LiveRegionPoliteness` column was also dropped from the status theory; only one politeness assertion survives in the whole file, so politeness regressions in the rewritten `ApplyStatus` switch go undetected. Severity: medium.
- [x] [Review][Patch] The flow-level `TrackingMismatch` path is unreachable in tests [tests/Hexalith.Tenants.UI.Tests/Components/TenantLifecycleActionAvailabilityTests.cs:1] — `StubTenantCommandGateway.GetStatusAsync` unconditionally returns `Status with { HasVerifiedCommandIdentity = true }` whenever `Status.Status` is non-null, so no bUnit test drives `ApplyStatus`'s `!status.HasVerifiedCommandIdentity → TrackingMismatch` branch (`TenantCreateCommandModels.cs:2011-2014`) through the real component. It is covered only at the isolated snapshot and gateway levels, though it is the story's central "no success without exact-command evidence" guarantee. Severity: medium.
- [x] [Review][Patch] Submit-time re-evaluation is skipped entirely when `Availability.Evidence` is null [src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleCommandFlow.razor:602] — `currentAuthorization` is computed at `:563-576` and then discarded unless `Availability.Evidence is not null`, so the null path dispatches with no authority re-verification. Not reachable through `TenantDetailPage` today (it always supplies `HighImpactEvidence(...)`, and `HighImpactAvailability` throws on null), but it is a latent fail-open: a future caller omitting evidence silently disables the entire submit-time gate. Make the null case block rather than skip. Severity: medium.
- [x] [Review][Patch] Undeclared `references/Hexalith.Builds` gitlink movement, plus two status contradictions [_bmad-output/implementation-artifacts/spec-3-4-disable-or-enable-tenant-with-complete-preview.md:1] — `python3 scripts/validate-story-gitlinks.py <this file>` exits 1: `references/Hexalith.Builds 4eb3392 -> 2f46aae` is `[UNDECLARED]` (the File List declares only `Hexalith.FrontComposer`), which the spec's own Verification section says must not happen and which "Ask First: touching a submodule" covers. Separately, this file's frontmatter says `status: 'done'` while `sprint-status.yaml:103` says `review`. Also, the `536e5c33..HEAD` range bundles Story 3.3's review closure (commit `d3527c84`: the `TenantHighImpactActionAvailabilityEvaluator` domain-outcome gating, FR diacritics across ~30 strings, the two configuration-flow CSS restorations, and removal of the `{action}-domain-outcome` stable selector) without 3.4's File List or Completion Notes acknowledging that the range is not 3.4-only. Severity: medium.
- [x] [Review][Patch] Dead resource key added in EN, FR, and the test stub [src/Hexalith.Tenants.UI/Resources/TenantsResources.resx:906] — `Tenants.Lifecycle.ProjectionVersion.Unknown` is referenced by zero production code paths; its only other occurrence is the bUnit localizer stub at `TenantLifecycleActionAvailabilityTests.cs:1350`. Either wire it to the missing-version state or remove all three. (EN/FR key parity itself is clean: 1344/1344, and all 20 touched keys carry distinct French values.) Severity: low.
- [x] [Review][Patch] Transport-failure copy duplicates the state line instead of using the message added for it [src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleCommandFlow.razor:697] — the catch sets `SafeMessageKey = "Tenants.Lifecycle.State.Failed"`, a state-description string, so the safe-message paragraph repeats verbatim what the state paragraph already says. The gateway-result failure branch immediately above (`:678`) uses the newly added `Tenants.Lifecycle.Message.Failed`. Two different strings for the same user-visible situation. Severity: low.
- [x] [Review][Patch] Narrow-viewport `display: none` hides a `tabindex="-1"` focus target [src/Hexalith.Tenants.UI/Components/Tenants/Configuration/SetTenantConfigurationFlow.razor.css:123] — the restored rule hides `.tenants-config-set__open-focus`, which is the `@ref="_openElement"` span at `SetTenantConfigurationFlow.razor:23`. `FocusAsync()` on a `display: none` element is a no-op, so focus falls back to the document body. `RemoveTenantConfigurationFlow.razor.css` has no counterpart rule because that flow has no `__open-focus` element, so the asymmetry itself is fine. Both files belong to Stories 3.5/3.6 and appear in neither spec's File List. Severity: low.
- [x] [Review][Patch] `EvaluateSubmitProof` hard-codes the freshness evidence the evaluator is meant to judge [src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleCommandFlow.razor:733] — it sets `Freshness = TenantHighImpactFreshnessState.Current` and `HasCurrentBaseline = true` unconditionally before calling `TenantHighImpactActionAvailabilityEvaluator.Evaluate`. This is sound only because `IsMatchingPreviewProof` (which requires `Freshness: ReadModelFreshnessState.Current`) ran first at `:597`. Nothing asserts that ordering, so reordering or short-circuiting either call silently evaporates the freshness gate. Pass the proof's real freshness, or assert the precondition. Severity: low.
- [x] [Review][Patch] Domain-outcome sentence is no longer rendered when it accompanies another blocker [src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleActionAvailability.razor:96] — the `<small data-testid="{actionId}-domain-outcome">` element was deleted. When the domain outcome is the *primary* blocker the copy survives, because `TenantHighImpactActionAvailabilityEvaluator.cs:124-133` sets `SafeMessageKey = "Tenants.HighImpact.DomainOutcome.{x}"` and `HighImpactReason` renders it. But `Blocked(...)` still carries `domainOutcome` alongside the viewport, admission, preview, and proof blockers (`:110-122`), and in those cases the domain fact is computed, carried, and shown to nobody. Either fold it into the reason or restore a render site. Severity: low.
- [x] [Review][Patch] `TenantLifecycleAttemptTracker.Remember` accepts snapshots it should reject and silently drops ones it should keep [src/Hexalith.Tenants.UI/State/TenantCommands/TenantLifecycleAttemptTracker.cs:60] — `Remember` never validates `snapshot.RetainsAttempt`, so any caller of this public API can pin a terminal or fabricated snapshot and poison a tenant's lifecycle surface. Conversely, the newer-wins guard returns silently when a *different* `MessageId` is already retained, so a genuinely newer dispatched attempt is never retained and loses its pending state on remount, with no signal to the caller. Severity: low.
- [x] [Review][Patch] Per-render service resolution and lock acquisition in the render path [src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleActionAvailability.razor:212] — `AttemptTracker` is an expression-bodied `Services.GetService<TenantLifecycleAttemptTracker>()` with no caching in both this component and `TenantLifecycleCommandFlow.razor:288`, and `CanResumeRetained` calls the locking `Find` from `IsActionDisabled`, `HighImpactReason`, and `ReasonCategory` — several resolutions plus lock acquisitions per action per render. Cache the resolved service in a field, as `TenantDetailPage` already does for `_aggregateAdmissionGate`. Severity: low.

## Suggested Review Order

**Submission and causal reconciliation**

- Revalidates every preview fact before one stable-message dispatch.
  [`TenantLifecycleCommandFlow.razor:523`](../../src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleCommandFlow.razor#L523)

- Preserves monotonic command-event evidence across delayed status responses.
  [`TenantCreateCommandModels.cs:1977`](../../src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs#L1977)

- Confirms only authoritative intended state after ordered projection advancement.
  [`TenantCreateCommandModels.cs:2120`](../../src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs#L2120)

- Replays concurrent refresh nudges without treating SignalR as proof.
  [`TenantLifecycleCommandFlow.razor:813`](../../src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleCommandFlow.razor#L813)

**Retained ownership and page lifecycle**

- Merges same-message progress and tombstones terminal attempts against stale writers.
  [`TenantLifecycleAttemptTracker.cs:48`](../../src/Hexalith.Tenants.UI/State/TenantCommands/TenantLifecycleAttemptTracker.cs#L48)

- Synchronizes retained snapshots and releases ownership only at terminal states.
  [`TenantLifecycleCommandFlow.razor:432`](../../src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleCommandFlow.razor#L432)

- Preserves tracker-owned admission across routing, then releases safely after terminalization.
  [`TenantDetailPage.razor:2147`](../../src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor#L2147)

- Qualifies lifecycle proof reads against route and generation identity.
  [`TenantDetailPage.razor:2083`](../../src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor#L2083)

**Gateway, compatibility, and automation**

- Verifies message, correlation, and literal aggregate identity together.
  [`TenantCommandGateway.cs:411`](../../src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs#L411)

- Preserves legacy gateway calls while requiring explicit stable-message overloads.
  [`ITenantCommandGateway.cs:51`](../../src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs#L51)

- Associates complete blockers, recoveries, and domain outcomes with each launcher.
  [`TenantLifecycleActionAvailability.razor:79`](../../src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleActionAvailability.razor#L79)

- Gates same-state domain facts on current authorized evidence.
  [`TenantHighImpactActionAvailabilityEvaluator.cs:172`](../../src/Hexalith.Tenants.UI/State/TenantDetail/TenantHighImpactActionAvailabilityEvaluator.cs#L172)

**Regression evidence**

- Exercises real page routing, disposal, lease retention, and stale-child cleanup.
  [`TenantDetailSurfaceTests.cs:818`](../../tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs#L818)

- Proves exact aggregate status identity and lifecycle accessibility wiring.
  [`TenantLifecycleActionAvailabilityTests.cs:352`](../../tests/Hexalith.Tenants.UI.Tests/Components/TenantLifecycleActionAvailabilityTests.cs#L352)

- Covers late-status monotonicity and sparse completed evidence.
  [`TenantLifecycleCommandSnapshotTests.cs:306`](../../tests/Hexalith.Tenants.UI.Tests/State/TenantLifecycleCommandSnapshotTests.cs#L306)

- Covers same-message races, terminal tombstones, and newer attempt ordering.
  [`TenantLifecycleAttemptTrackerTests.cs:68`](../../tests/Hexalith.Tenants.UI.Tests/State/TenantLifecycleAttemptTrackerTests.cs#L68)

- Compiles legacy gateway implementations and verifies fail-closed stable dispatch.
  [`TenantCommandGatewayTests.cs:1853`](../../tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs#L1853)
