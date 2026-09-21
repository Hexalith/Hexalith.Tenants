---
title: '4.2 Grant Global Administrator with Projection Confirmation'
type: 'feature'
created: '2026-08-31'
status: done
baseline_revision: 7f564930055a89251f512bafb62ee61491891a8f
baseline_commit: '7e88a571588fc7aa769ee1af01e91113f6f9b01f'
review_loop_iteration: 3
followup_review_recommended: true
operator_actions:
  - 'Provide the local EventStore authentication parameters required to start tenants-api, then run an authenticated browser acceptance trace for grant-preview focus containment, launcher restoration, inactive-panel visibility, and short-viewport scrolling.'
  - 'Publish a Hexalith.EventStore package containing the HandlerAwareQueryRouter projection-provenance fix, update the Hexalith.Builds EventStore pin, and rerun the package-reference generated-controller integration suite until every test passes.'
  - 'Align the source-reference integration output so Hexalith.Tenants.Contracts, Version=5.7.0.0 loads during MVC controller discovery, then rerun the four real-handler metadata cases.'
context:
  - '{project-root}/_bmad-output/project-context.md'
warnings:
  - oversized
deferred:
  - summary: >-
      The concurrently introduced tenant-workspace tab migration lacks browser-level proof that inactive panels are not visible or focusable.
    evidence: |-
      The workspace-tab change arrived in a separate concurrent commit and is outside Story 4.2. Existing component assertions cover attributes, but an authenticated browser active-element and visibility trace would settle the remaining interaction risk.
    location: >-
      tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs:420
    severity: medium
  - summary: >-
      The concurrently introduced Memories secret-store topology lacks a deployed Aspire model and health verification.
    evidence: |-
      The AppHost secret-store change arrived in a separate concurrent commit and is outside Story 4.2. An Aspire resource-model inspection plus a healthy deployed startup using the intended secret provider would settle the topology risk.
    location: >-
      src/Hexalith.Tenants.AppHost/Program.cs:117
    severity: medium
  - summary: >-
      The tenant-workspace tab migration removed every inactive-panel visibility assertion inside this story's own commit, not a concurrent one.
    evidence: |-
      Commit 8da765ad -- the same commit that carries the Story 4.2 grant work -- replaced the `hidden`/`aria-hidden` assertions on `#tenants-retained-panel`/`#users-retained-panel` with `role="tabpanel"` checks that hold for the active and inactive panel alike. Fluent UI v5 owns the panel flip client-side, so bUnit cannot observe it and no assertion anywhere distinguishes the two states. An authenticated browser trace showing the inactive panel is neither visible, focusable, nor exposed to assistive technology would settle it.
    location: >-
      tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs:426
    severity: medium
  - summary: >-
      Focus containment, focus restoration, and viewport measurement are proven at the interop layer rather than in a real browser.
    evidence: |-
      The component tests assert which ElementReference the page asked the runtime to focus and drive the viewport by calling Observe on the observation singleton. Neither reaches document.activeElement, a real Tab cycle, `inert` semantics, or a real JS measurement, and there is no browser-driven lane in this repository. An authenticated browser trace over the grant preview -- open, Tab cycle, Escape, restore -- and a real viewport measurement would settle it.
    location: >-
      tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs
    severity: medium
  - summary: >-
      Identical transport conditions are classified two different ways depending on which tracked command was dispatched.
    evidence: |-
      `SetGlobalAdministratorTrackedAsync` now treats a retryable EventStoreGatewayException and a plain OperationCanceledException as same-identity ambiguity, while `SetTenantConfigurationTrackedAsync`, `RemoveTenantConfigurationTrackedAsync`, `EnableTenantTrackedAsync`, and `DisableTenantTrackedAsync` still key off status codes and `TaskCanceledException` alone. Those four are unchanged pre-existing behaviour outside this story's fixed-scope intent; a decision on whether the grant rule supersedes them would settle it.
    location: >-
      src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs:258
    severity: low
  - summary: >-
      Nothing in this repository pins EventStore's command-status contract, which the grant lifecycle reasons over directly.
    evidence: |-
      Every status assertion is fed by a stub. This pass corrected one concrete assumption -- that EventsStored/EventsPublished carry an EventCount -- only by reading AggregateActor and CommandStatusRecord in the submodule. A contract or integration test over a real command-status response would settle the remaining assumptions the same way.
    location: >-
      src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs:575
    severity: low
  - summary: >-
      The full-solution Release build fails because this range's Aspire secret-store call site is ahead of every published Hexalith.Memories.Aspire package.
    evidence: |-
      `dotnet build Hexalith.Tenants.slnx --configuration Release` reports
      `src/Hexalith.Tenants.AppHost/Program.cs(132,5): error CS1503: Argument 4: cannot convert from
      'IResourceBuilder<IDaprComponentResource>' to 'string'`. Commit 7453ba5b (inside baseline..HEAD, a
      concurrent change this story does not own) moved that argument to match the Hexalith.Memories
      submodule, which does declare the new overload -- the AppHost builds clean in the source-reference
      lane. The newest published Hexalith.Memories.Aspire is 2.22.1, the version Hexalith.Builds pins via
      HexalithMemoriesVersion, and it still declares the `string` overload. The selected resolution keeps
      the Story 29.2 consumer-owned `IResourceBuilder<IDaprComponentResource>` signature canonical: publish
      Hexalith.Memories.Aspire with that signature and bump HexalithMemoriesVersion in Hexalith.Builds before
      re-arming this story. Reverting the Tenants call site to the removed path-based overload or waiving the
      Release gate is explicitly forbidden.
    location: >-
      src/Hexalith.Tenants.AppHost/Program.cs:132
    severity: high
  - summary: >-
      Hexalith.Builds pins an unpublished EventStore proof version, so no project restores without an override.
    evidence: |-
      references/Hexalith.Builds/Props/Directory.Packages.props:8 pins
      HexalithEventStoreVersion = 999.1.20-proof.fa2d1c9910f8, which is not on nuget.org, so a default
      restore fails NU1102 for every project in the repository. Every command in this review pass was run
      with `-p:HexalithEventStoreVersion=3.100.1` (and the environment variable of the same name, for the
      tests that shell out to a subprocess restore). This is the Builds/EventStore release owner's
      deliberate, temporary state and must never be reverted from inside Tenants; it is recorded here
      because it makes this story's stated verification commands unrunnable as written.
    location: >-
      references/Hexalith.Builds/Props/Directory.Packages.props:8
    severity: medium
  - summary: >-
      The four sibling tracked-dispatch gateway methods did not adopt the grant path's retryable and cancellation mapping.
    evidence: |-
      SetGlobalAdministratorTrackedAsync now maps `ex.Retryable == true` and any non-caller
      OperationCanceledException to same-id ambiguity. SetTenantConfigurationTrackedAsync (:258),
      RemoveTenantConfigurationTrackedAsync (:314), EnableTenantTrackedAsync (:467) and
      DisableTenantTrackedAsync (:525) still use the status-code-only test and `catch
      (TaskCanceledException)`, so the same transport condition is ambiguous on grant and terminal there,
      and a bare OperationCanceledException escapes them entirely. Pre-existing behaviour this story did
      not touch; their theory source AmbiguousTransportExceptions has neither new case. Settled by moving
      the two rows into a shared theory source driven from all five methods.
    location: >-
      src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs:258
    severity: medium
  - summary: >-
      The page's focus disconnect guard has no runtime test, including the JSException arm added in this pass.
    evidence: |-
      FocusSafelyAsync swallows JSDisconnectedException, ObjectDisposedException and (new in this pass)
      JSException across all six pending-focus sites, but GlobalAdministratorsPageTests contains no
      SetException, JSDisconnected or ObjectDisposedException reference: reverting any site to a bare
      FocusAsync leaves every test green, because bUnit's focus handler never throws. Settled by a bUnit
      `JSInterop.Setup(...).SetException(...)` on the focus invocation, as SupportSafeCopyButtonTests.cs:186
      already does for writeText, asserting the render completes without the exception escaping.
    location: >-
      src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1323
    severity: medium
  - summary: >-
      The correction panel's restore-only tracked-dispatch requirement is never exercised in its false state.
    evidence: |-
      HasReconciliationSupport gained `(_snapshot?.IsRestoreAccessAction != true ||
      CommandGateway.SupportsTrackedGlobalAdministratorDispatch)`, which gates CanRefreshSnapshot, the
      MissingLifecycleSupport copy, and CanOwnReconciliation. The panel's stub gateway declares
      `SupportsTrackedGlobalAdministratorDispatch { get; set; } = true` and no test in that file assigns
      it, and the LiveGateChangeAfterOuterSubmitCheckPreventsDispatch theory has no tracked-dispatch case,
      so deleting the clause stays green -- the same dead-control failure the page side fixed with two new
      tests. Settled by a `gateway-tracked-dispatch` theory case plus one adoption test with the stub set
      false.
    location: >-
      src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor:382
    severity: medium
  - summary: >-
      Retained tracked-command lifecycle is proven only across a bUnit re-render sharing one service provider.
    evidence: |-
      The renderer-replacement tests re-render against the same DI singletons, so the caller-owned ULID,
      preview baseline and fixed-aggregate lease live in process memory for the life of the process. No
      test exercises circuit loss and reconnect, a process restart, or a second node. Settled by an
      integration test that drops and re-establishes the circuit, or by an explicit decision that
      retention is scoped to the process.
    location: >-
      tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs
    severity: medium
  - summary: >-
      The newly elevated grant preview dialog has no browser-level proof of its focus ring or short-viewport overflow.
    evidence: |-
      This pass gave the role="dialog" section fixed centred positioning, elevation and `overflow-y: auto`
      with a `max-block-size: 90vh` bound and a forced-colors border. bUnit does not evaluate CSS, so
      nothing proves the Fluent v5 buttons keep a visible focus ring against the elevated surface, or that
      a short viewport scrolls the dialog rather than clipping it. Settled by the same authenticated
      browser trace that would settle the existing focus-containment entry.
    location: >-
      src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor.css:288
    severity: medium
  - summary: >-
      No test pins the fail-closed default that GlobalAdministratorActionEvidence.SupportsTrackedGrantDispatch now carries.
    evidence: |-
      The property lost its `= SupportsDispatch` initializer, so an external construction that does not set
      it explicitly now gets false and permanently disables grant behind a generic missing-lifecycle-support
      reason. The direction is correct (fail closed), but the property is not `required`, the XML doc does
      not state the default, and both existing tests set it explicitly. Settled by a cross-repository
      reference search for constructions of this record, plus one test pinning the unset default.
    location: >-
      src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorActionEvidence.cs:62
    severity: low
  - summary: >-
      Publishing the Memories package alone cannot satisfy the unchanged Release gate while the EventStore proof package set is unavailable.
    evidence: |-
      The default UI and solution restores fail with NU1102 for Hexalith.EventStore version 999.1.20-proof.fa2d1c9910f8 before the AppHost can compile. The complete proof-version package set must be published by the EventStore release owner before the package-reference gate can run.
    location: >-
      references/Hexalith.Builds/Props/Directory.Packages.props:8
    severity: medium
  - summary: >-
      The dependency-alignment acceptance criterion remains operator-owned external release work.
    evidence: |-
      Repository implementation and fallback verification are complete, but this checkout cannot publish Hexalith.Memories.Aspire or change the package version selected by the separate Hexalith.Builds release workflow without release credentials and a published version.
    location: >-
      src/Hexalith.Tenants.AppHost/Program.cs:132
    severity: medium
  - summary: >-
      Real-browser focus containment and restoration remain unproven.
    evidence: |-
      The focused component lane verifies the requested ElementReference targets and all 395 focused tests pass, but it does not observe document.activeElement or a real Tab cycle. An authenticated browser trace across open, forward and reverse Tab, Escape, and launcher restoration would settle the outcome.
    location: >-
      tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs:2327
    severity: medium
---

<intent-contract>

## Intent

**Problem:** Production grant remains intentionally unavailable because the historical form dispatches without a complete consequence preview, normalizes the target, lets the gateway mint an unrecoverable message id, and can confirm from target presence without causal projection evidence.

**Approach:** Install a BFF-composed, complete fixed-scope grant preview and a retained tracked-command lifecycle. Preserve the exact target and one caller-owned ULID, then report success only when a complete authoritative re-query proves the target present, the projection version advanced beyond the preview baseline, and status for that exact command proves an event was produced.

## Boundaries & Constraints

**Always:** Fix identity to `system / global-administrators / global-administrators`; keep API/domain authorization authoritative and recheck current circuit authority before preview and dispatch; treat non-whitespace UserIds as literal ordinal values without trimming, casing, parsing, or regeneration; require all preview facts, current complete-population evidence, measured safe viewport, lifecycle support, and fixed-aggregate admission; retain one message id and the preview baseline through ambiguous transport and renderer replacement; keep last-confirmed rows unchanged until qualified confirmation; use FrontComposer/Fluent UI V5, whole-string EN/FR resources, stable selectors, focus containment, and support-safe lifecycle states; keep the Story 29.2 consumer-owned secret-store resource-builder API canonical in both source- and package-reference lanes; before re-arm, require a published `Hexalith.Memories.Aspire` package exposing the `IResourceBuilder<IDaprComponentResource>` signature and a corresponding `HexalithMemoriesVersion` pin in Hexalith.Builds.

**Never:** Add an endpoint, tenant-membership lookup, bulk grant, optimistic row, direct state-store access, or a second aggregate identity; treat acceptance, SignalR, target presence alone, opaque version churn without exact-command event evidence, `GlobalAdministratorAlreadyExists`, or an audit promise as success; expose correlation ids, message ids, tokens, claims, payloads, ETags, cursors, projection versions, metadata, stack traces, or hidden administrator data; change removal/domain behavior; revert Tenants to the removed path-based Memories secret-store overload, accept a source-only build as completion evidence, or waive or downgrade the full-solution Release gate; modify, stage, revert, or otherwise write `_bmad-output/implementation-artifacts/sprint-status.yaml`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Eligible preview | Authorized caller, exact absent target, complete current fixed-scope snapshot, safe viewport and free aggregate | Render a modal preview containing fixed scope, target, current/resulting count, authority change, freshness, recovery, audit expectation, caller/target context, known consequences, and known unknowns; dispatch nothing | Any missing or contradictory fact blocks confirmation with associated reason/recovery |
| Existing target | Complete current snapshot already contains the exact ordinal UserId | Refuse preview/dispatch and retain confirmed rows | Show localized safe rejection; never NoOp, AlreadyApplied, or success |
| Preview invalidated | Authority, projection version, completeness, viewport, support, target absence, or aggregate admission changes before confirmation | Refuse dispatch and require a refreshed preview | Preserve read-only rows; focus the visible failure/recovery region |
| Ambiguous dispatch | Timeout, retryable gateway error, cancellation not requested by the caller, or renderer loss after request send | Retain the same ULID, intent, baseline, and fixed-aggregate ownership for status lookup or same-id redispatch | Never release as terminal or mint a replacement id |
| Unqualified status | Missing/mismatched tracking identity, zero event evidence, rejected, publish-failed, timed-out, or unknown status | Keep the distinct non-success state and do not confirm | Duplicate and permission failures remain localized rejections; retryable/unknown evidence remains recoverable without double dispatch |
| Qualified confirmation | Exact tracked command has event evidence; complete current re-query contains target; current projection version differs from nonblank preview baseline | Transition to confirmed and update visible rows from the authoritative read | Audit remains pending/delayed/unavailable unless separately proven; no fabricated receipt |
| Non-causal projection | Target appears with unchanged version, version changes without exact-command event evidence, target is absent, or evidence is page-scoped/stale | Do not confirm | Remain projection-pending or unable-to-verify with explicit recovery |
| Cancel or Escape | Preview is open before dispatch | Close without I/O and return focus to the launcher | Abandon only a pre-dispatch lease; never cancel accepted work |
| Memories dependency alignment | Source references expose the Story 29.2 resource-builder signature but the published package or Hexalith.Builds pin does not | Keep the story blocked and do not re-arm | Publish the matching Hexalith.Memories.Aspire package, update `HexalithMemoriesVersion` in Hexalith.Builds, then rerun the unchanged Release gate; do not roll back Tenants |

</intent-contract>

## Code Map

- `references/Hexalith.EventStore/src/Hexalith.EventStore/Queries/HandlerAwareQueryRouter.cs` and `tests/Hexalith.EventStore.QueryRouting.Tests/HandlerAwareQueryRouterTests.cs` -- preserve explicitly declared projection-backed metadata only when a trusted domain handler returns both a nonblank projection type and `ProjectionBacked` provenance; ordinary handler results remain `HandlerComputed` and fail closed.
- `src/Hexalith.Tenants/Queries/TenantQueryResult.cs`, `Queries/Handlers/TenantQueryHandlerBase.cs`, and `Queries/Handlers/GetGlobalAdministratorsQueryHandler.cs` -- carry `IReadModelFreshness` timestamps/version through the explicit projection-backed handler result instead of discarding them.
- `src/Hexalith.Tenants.Contracts/Identity/GlobalAdministratorUserId.cs` and `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorIdentityDisplay.cs` -- provide one literal, bounded, non-control-character, well-formed-UTF-16 command policy; render format scalars with a collision-free presentation encoding that doubles literal backslashes and emits generated tokens as `\{U+XXXX}` / `\{U+XXXXXXXX}`; never alter the retained or dispatched ordinal value.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor` -- apply the same display-only identity encoding to restored target context so a grant target cannot regain bidi-spoofing behavior on the audit handoff.
- `src/Hexalith.Tenants.UI/State/TenantAudit/GlobalAdministratorCorrectionSnapshot.cs` and `tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorCorrectionPanelTests.cs` -- propagate grant lifecycle resource keys through restore-access correction consumers so key-only gateway outcomes remain visible and localized.
- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:202` -- replace the direct-submit grant form with preview handoff, focus trap, live rechecks, tracked dispatch, retained recovery, and complete-projection confirmation; bind both initial and retry delivery to a shared lease-level single-flight token so renderer replacement cannot overlap the same POST; preserve the review rows and removal ownership.
- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor.css:24` -- reuse existing preview/focus/responsive/forced-colors layout hooks without defining theme primitives.
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorGrantCommandSnapshot.cs:8` -- current lifecycle lacks preview baseline, caller-owned message identity, exact-command event evidence, and causal confirmation.
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorReconciliationState.cs:11` -- retained circuit state currently loses preview/version/ambiguous-dispatch evidence.
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorActionAvailabilityEvaluator.cs:13` -- preserve Story 4.1's pure availability handoff; production readiness becomes true only after the actual preview exists.
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantsBffComposition.cs:11` and `TenantsBffComposition.cs:33` -- add the fail-closed grant-preview composition seam and replace the intentional production `false` readiness declaration.
- `src/Hexalith.Tenants.UI/Services/Gateways/GlobalAdministratorsProjectionLoader.cs:23` -- reuse the bounded, cursor-safe, version-consistent complete walk for preview and post-command evidence.
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs:67`, `TenantCommandGateway.cs:333`, and `UnavailableTenantCommandGateway.cs` -- add caller-owned tracked grant dispatch, fixed identity, ULID validation, and ambiguous transport mapping while retaining compatibility.
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantAggregateCommandAdmissionGate.cs:27` -- retain/adopt ambiguous and accepted grant reconciliation, own one durable initial-or-retry delivery at a time across renderer replacement, and publish completion to the current owner without weakening unrelated aggregate concurrency or terminal-only release.
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantMembershipCommandProvenance.cs:20` -- reuse opaque projection-version advancement only in conjunction with exact-command event evidence.
- `src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsAggregate.cs:22` and `src/Hexalith.Tenants.Contracts/Commands/SetGlobalAdministrator.cs:7` -- read-only enforcement/contracts; do not change domain behavior or payload shape.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` and `TenantsResources.fr.resx` -- add parity-matched preview, acknowledgement, invalidation, ambiguity, and recovery whole strings.
- `tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs:2170`, `State/GlobalAdministratorGrantCommandSnapshotTests.cs:13`, `Services/Gateways/TenantCommandGatewayTests.cs:35`, `Services/Gateways/TenantsBffCompositionTests.cs:75`, and `State/TenantAggregateCommandAdmissionGateTests.cs:11` -- replace superseded direct-submit/presence-only expectations and cover the full matrix.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` -- orchestrator-owned and strictly read-only; its row state is not verification evidence.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorGrantPreview.cs` and `Services/Gateways/ITenantsBffComposition.cs`, `TenantsBffComposition.cs` -- add an immutable BFF-owned preview that validates current authority, exact absent target, matching complete/current/versioned evidence, and all ten required safe facts; expose readiness only when this concrete composition is installed.
- [x] `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorGrantCommandSnapshot.cs` -- model previewed intent, baseline projection version/count, one message id, attempt/event evidence, ambiguity, and qualified confirmation; status must require verified fixed-command identity and positive event evidence, and SignalR must only request reconciliation.
- [x] `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorReconciliationState.cs` and `State/TenantCommands/TenantAggregateCommandAdmissionGate.cs` -- retain the minimum support-safe grant attempt needed to adopt request-sent/ambiguous/accepted/projection-pending work after renderer replacement; preserve monotonic state and terminal-only release.
- [x] `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs`, `TenantCommandGateway.cs`, and `UnavailableTenantCommandGateway.cs` -- add explicit-message-id grant dispatch, preserve the exact UserId, verify the ULID, pass the fixed aggregate in status handles, and map timeout/HTTP/retryable gateway outcomes to a same-id ambiguous result rather than terminal failure.
- [x] `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor` -- render and focus-contain the complete preview, require deliberate acknowledgement, reauthorize and rebuild matching evidence immediately before dispatch, preserve last-confirmed rows, dispatch/retry one tracked attempt, adopt retained attempts, and confirm from the complete loader only after target presence plus baseline advancement plus exact-command event evidence.
- [x] `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor.css`, `Resources/TenantsResources.resx`, and `TenantsResources.fr.resx` -- add only necessary preview layout/focus hooks and parity-matched whole strings for every preview item, invalidation, ambiguity, lifecycle, and recovery state.
- [x] `tests/Hexalith.Tenants.UI.Tests/State/GlobalAdministratorGrantCommandSnapshotTests.cs`, `State/TenantAggregateCommandAdmissionGateTests.cs`, `Services/Gateways/TenantCommandGatewayTests.cs`, `Services/Gateways/TenantsBffCompositionTests.cs`, and `Components/GlobalAdministratorsPageTests.cs` -- cover every matrix row, preview completeness and stale-submit refusal, exact literal whitespace/case preservation, stable ULID redispatch/adoption, verified status/event evidence, complete advanced-version confirmation, same-version/unrelated/page-scoped refusal, no optimistic rows, authorization/viewport/admission races, focus/Escape/keyboard/live-region behavior, EN/FR parity, forced colors, responsive fail-closed behavior, support safety, and grant/remove/unrelated-tenant lock isolation; use PascalCase for new test methods.

**Review-derived safeguards:**
- [x] Treat confirmation as a single-flight operation. Capture and recheck the exact preview object, lease, message id, acknowledgement, and mutation generation across every await; a superseded handler must not dispatch a later preview or clear a lease another handler marked dispatched.
- [x] On authorization loss, preview-composition failure, tracking mismatch, or blank acceptance correlation, preserve recoverability: abandon an undispatched lease, retain a dispatched same-id attempt, and collapse privileged rows whenever authoritative composition denies the caller. Never strand a marked lease.
- [x] Compose the initial preview from a new complete bounded walk. Render every preview fact and its failure-specific recovery from BFF-owned completeness evidence; missing resource/evidence must keep confirmation unavailable.
- [x] Keep lifecycle evidence monotonic. Pending/unknown status is handled before exact-identity qualification, `Received`/`Processing` cannot regress event evidence, `EventsStored` and `EventsPublished` are qualified like `Completed` only with positive exact-command event evidence, and SignalR triggers reconciliation without changing lifecycle state.
- [x] Retain request-sent work across renderer replacement as explicitly ambiguous until acceptance identity exists, and make adoption wake the replacement for same-id redispatch or status lookup. A changed projection during ambiguous recovery must remain recoverable without releasing or orphaning the fixed lock.
- [x] Make the modal interaction real rather than declarative only: focus actual interactive controls, restore the launcher, contain keyboard and assistive focus, and prevent background grant/removal/navigation actions while preview is open.
- [x] On qualified confirmation, update authoritative evidence but preserve the existing paged display contract instead of replacing page one with the complete population.
- [x] Keep strict test doubles: unexpected projection reads fail unless a test explicitly opts into repetition, and status identity is never forced true globally. Add PascalCase tests for concurrent confirmation, preview supersession, authorization collapse, composition exceptions, failure recovery copy, exact count rendering, fixed aggregate tracking handles, every event-producing status, renderer replacement, paged confirmation, and behavioral focus/background isolation.
- [x] Treat preview creation as a cancellable, generation-bound operation. Capture the exact input and generation before the first await; Cancel, authorization collapse, disposal, input replacement, or a superseding attempt must cancel the bounded walk, prevent lease acquisition, and prevent a stale continuation from opening a preview.
- [x] Re-evaluate authority, support, measured viewport, fixed-aggregate admission, acknowledgement, exact preview identity, and ownership inside the renderer callback that marks the lease dispatched. Evidence captured before that callback is not a dispatch permit.
- [x] Use an actual Fluent/FrontComposer modal focus mechanism or explicit `ElementReference.FocusAsync` calls for initial focus, both Tab directions, and launcher restoration. Make the whole background subtree inert while the preview is open; disabling accordion headers or asserting `autofocus` markup is insufficient.
- [x] Validate localized fact values as part of BFF preview completeness, not only their resource-key names. Missing, blank, or unresolved EN/FR values keep confirmation disabled and expose visible recovery.
- [x] At every grant consumer, tracked-dispatch capability must be explicit. Retryable gateway Problem Details and non-caller transport cancellation map to same-id ambiguity; compatibility callers must preserve the returned message id, safe recovery key, fixed aggregate handle, and verified command identity rather than stranding a marked lease.
- [x] Label correlation-less same-id redispatch as delivery retry, not status refresh. Projection confirmation copy must describe observed evidence without promising downstream authority-enforcement timing, and French resources must use native-quality diacritics.
- [x] Add focused tests for initial-preview cancellation/supersession, input replacement, final-arm prerequisite races, stale-preview invalidation, independent tracked-dispatch capability, retryable Problem Details, non-caller cancellation, correction identity mismatch, positive event-evidence retention across renderer replacement, resolved-resource completeness, real active-element focus containment/restoration, and honest delivery-retry copy.

**2026-09-21 re-derivation tasks:**
- [x] `references/Hexalith.EventStore/src/Hexalith.EventStore/Queries/HandlerAwareQueryRouter.cs` and `references/Hexalith.EventStore/tests/Hexalith.EventStore.QueryRouting.Tests/HandlerAwareQueryRouterTests.cs` -- preserve a handler's `ProjectionBacked` metadata only when its result also declares a nonblank projection type; retain `HandlerComputed`/unknown lifecycle for every undeclared, incomplete, or ordinary handler result.
- [x] `src/Hexalith.Tenants/Queries/TenantQueryResult.cs`, `src/Hexalith.Tenants/Queries/Handlers/TenantQueryHandlerBase.cs`, and query-handler tests -- restore metadata derived from `IReadModelFreshness`, mark only that overload `ProjectionBacked`, and keep null/degenerate ETags plus absent freshness fail-closed.
- [x] `tests/Hexalith.Tenants.IntegrationTests/TenantsApiGeneratedControllerTests.cs` and `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs` -- exercise the real global-administrator handler through handler-aware routing, generated REST metadata, client parsing, and the UI gateway; prove both current/versioned evidence and stale/incomplete evidence survive the chain without fabricated response metadata, while unknown/handler-computed evidence remains unavailable.
- [x] `src/Hexalith.Tenants.Contracts/Identity/GlobalAdministratorUserId.cs`, `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorIdentityDisplay.cs`, all grant/removal validation consumers, and focused tests -- centralize the 256/nonblank/control-character boundary, preserve every established well-formed accepted/rejected case, and newly reject unpaired UTF-16 surrogates before preview or JSON serialization. Render whitespace literally; double each literal backslash and encode each accepted format scalar as `\{U+XXXX}` / `\{U+XXXXXXXX}` so distinct identities never share a display while retained, copied, and dispatched values remain original.
- [x] `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor`, `src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor`, and `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor` -- use the collision-free display encoding for rows, grant/removal previews, correction target, accessible names, and restored audit context; preserve original identity for copy and dispatch; add component cases covering real format scalars, literal escape-looking text, and the corresponding original command value.
- [x] `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor`, `src/Hexalith.Tenants.UI/State/TenantCommands/TenantAggregateCommandAdmissionGate.cs`, and focused admission/component tests -- preserve withdrawal diagnostics, automatic same-ID re-arm, exact notification snapshot/lease guards, and local retry serialization; additionally use the lease's durable delivery token for grant initial dispatch and redispatch so disposal/replacement cannot overlap a POST, completion wakes the current owner, and every outcome retains the one ULID and terminal-only lease rules.
- [x] `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorGrantCommandSnapshot.cs`, `Services/Gateways/TenantsBffComposition.cs`, both Tenants resource files, and focused tests -- preserve the distinct reversible withdrawal transition and original diagnostic, the complete 87-key EN/FR fail-closed grant-surface gate, and all prior green regression coverage; withdrawal recovery must never instruct an operator to mint a new attempt while the retained lease is unresolved.
- [x] `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`, `GlobalAdministratorsPage.razor`, `src/Hexalith.Tenants.UI/State/TenantAudit/GlobalAdministratorCorrectionSnapshot.cs`, both Tenants resource files, and gateway/snapshot/component tests -- return, retain, and render stable EN/FR resource keys for grant-visible rejection, pending, publication-failure, timeout, mismatch, and unavailable lifecycle copy in every grant consumer, including restore-access correction; include key-only gateway-to-page cases and restore-correction theories for pending, unknown, rejected, publish-failed, and timed-out outcomes.
- [x] Add focused component coverage proving correlated notifications perform exactly one status lookup with the retained message/correlation/fixed aggregate identity and zero command redispatch, while correlation-less notifications perform neither status lookup nor redispatch; retain the superseded-notification snapshot-and-lease race case.
- [x] Run the EventStore query-routing tests, the Tenants server/query integration tests, the focused Story 4.2 UI matrix, the full UI lane, source-reference Debug builds, package-reference Release builds, and `git diff --check`; record the EventStore package-publication blocker rather than treating a source-only platform fix as package-mode acceptance.

**Acceptance Criteria:**
- Given an authorized operator enters an exact non-whitespace UserId, when grant is initiated, then no command is sent and an accessible, focus-contained preview shows all ten BFF-composed facts for the fixed platform scope without tenant-membership data.
- Given preview is open, when the operator cancels or presses Escape, then no command is sent and focus returns to the grant launcher; when any required fact is absent, confirmation remains unavailable with visible associated recovery.
- Given the preview remains current and deliberately acknowledged, when confirmation is submitted, then exactly one `SetGlobalAdministrator` attempt uses the fixed aggregate and one caller-owned ULID while last-confirmed rows remain unchanged.
- Given transport outcome is ambiguous or the renderer is replaced, when the attempt is retried or adopted, then the exact intent, baseline, message id, and lock are reused and a second command identity is never generated.
- Given status for the exact fixed-scope handle proves command events and a complete current re-query contains the target at a projection version advanced beyond the preview baseline, when reconciliation runs, then and only then the UI reports confirmed and refreshes rows.
- Given status, SignalR, target presence, version change, or audit availability is unqualified in isolation, when reconciliation runs, then success is not shown and the operator receives an honest pending, degraded, rejected, or unable-to-verify recovery state.
- Given the target already exists or current authority/evidence/support/viewport/admission changes, when preview or dispatch is evaluated, then the flow fails closed without data disclosure, normalization, optimistic mutation, or dispatch.
- Given EN/FR, keyboard, screen-reader, forced-colors, reduced-motion, desktop/tablet, unknown/phone viewport, and support-safety checks run, when the flow renders across its lifecycle, then stable selectors, localized whole strings, focus/live-region semantics, read-only unsafe-width behavior, and redaction all pass.
- Given the source-reference and package-reference AppHost lanes are verified, when Story 4.2 is re-armed, then both lanes compile the same consumer-owned `IResourceBuilder<IDaprComponentResource>` secret-store call using a published Hexalith.Memories.Aspire package selected by Hexalith.Builds; the removed path-based overload and a waived Release gate are not acceptable alternatives.

## Spec Change Log

### 2026-08-31 — Review pass 1 repair
- Triggering findings: the first independent review found confirmation races, stranded leases, non-monotonic status/SignalR transitions, incomplete recovery rendering, authority-collapse disclosure, renderer-replacement gaps, modal isolation issues, paged-display regression, and test seams that concealed those failures.
- Amendment: added the review-derived safeguards above so ownership, generation, authority, recovery, lifecycle monotonicity, BFF completeness, paging, accessibility, and verification behavior are explicit implementation obligations.
- Known-bad state avoided: a later or duplicate handler must not dispatch an unacknowledged target, a dispatched fixed-aggregate lease must never become ownerless, and unqualified evidence must never become confirmation or visible privileged data.
- KEEP: preserve exact literal UserIds, caller-owned canonical ULIDs, fixed `system / global-administrators / global-administrators` routing, complete projection walks, exact-command positive event evidence, last-confirmed rows until qualification, EN/FR whole strings, removal behavior, and the passing focused/full verification lanes.

### 2026-08-31 — Review pass 2 repair
- Triggering findings: the second review demonstrated cancellable-preview races, contradictory target rendering, a stale final dispatch arm, broken focus wrapping/restoration and modal isolation, retryable transport becoming terminal, correction consumers stranding or trusting unverified tracking, unresolved resource keys passing completeness, and required lifecycle/accessibility tests that remained declarative or absent.
- Amendment: generation-bound preview creation, last-moment live prerequisite checks, actual focus/inert behavior, resolved-resource completeness, explicit tracked capability and correction identity handling, truthful delivery-retry/enforcement copy, native French, and the named focused tests are now mandatory safeguards.
- Known-bad state avoided: Cancel cannot be undone by a stale bounded walk; an operator cannot acknowledge target A while editing target B or dispatch after support/viewport withdrawal; retryable delivery cannot release or orphan the fixed lock; and a modal or localized fact cannot claim safety from markup/key presence alone.
- KEEP: preserve the fixed identity, exact literal target, caller-owned canonical ULID, complete bounded reads, exact-command event evidence plus differing opaque projection version, same-id ambiguous recovery, last-confirmed rows, authoritative authorization collapse, paged display contract, support-safe diagnostics, and every warning-clean focused/full test that still expresses the corrected behavior.

### 2026-09-01 — Escalation resolution
- Human-confirmed decision: keep the provider-neutral Story 29.2 resource-builder API canonical; publish Hexalith.Memories.Aspire with that signature and update the Hexalith.Builds `HexalithMemoriesVersion` pin before re-arming Story 4.2.
- Gate: retain the full-solution Release build unchanged. A Tenants rollback to the removed path-based overload, a source-only success, or a Release-gate waiver does not resolve this story.

### 2026-09-21 — Review pass 4 repair
- Triggering findings: the resumed review proved that handler-aware routing erased the persisted read model's projection provenance while integration tests fabricated it, grant-visible lifecycle failures bypassed EN/FR resources, literal identity policy had drift-prone copies, and ambiguous delivery retry was not single-flight.
- Amendment: added the EventStore handler-provenance boundary, a real handler-to-REST-to-UI verification lane, a shared literal identity contract, lifecycle resource-key mapping, serialized delivery retry, literal-safe target rendering, and notification-only reconciliation as explicit re-derivation tasks.
- Known-bad state avoided: production must not leave grant permanently unavailable while fabricated tests pass; French operators must not receive English lifecycle failures; two retry activations must not cancel each other; and SignalR must never redispatch a privileged command.
- KEEP: preserve the distinct retry-withdrawal state and original diagnostic, the 87-key EN/FR fail-closed surface gate, rendered-only ARIA references, first-statement disabled-submit guard, surfaced redispatch identity races, paired nonblank localization overrides, caller-owned ULID, exact literal target, terminal-only lease release, and the previously green focused/full verification lanes.

### 2026-09-21 — Review pass 5 repair
- Triggering findings: the second resumed review proved that embedded bidi overrides remain visually deceptive inside `<bdi>`, transient prerequisite loss permanently strands the retained retry, correction consumers discard key-only lifecycle messages, and the positive notification plus stale-provenance paths lack end-to-end verification.
- Amendment: require visible code-point escaping at presentation only, reversible same-ID withdrawal after all live prerequisites recover, lifecycle-key propagation through restore-access correction, key-only gateway-to-page tests, a correlated-notification status test, and stale handler-to-UI evidence coverage.
- Known-bad state avoided: the preview must not display a reordered identity; transient viewport/capability loss must not force a forbidden replacement command; correction flows must not render a blank diagnostic; and stale evidence or a no-op notification branch must not pass an all-green matrix.
- KEEP: preserve trusted projection provenance with fail-closed incomplete cases, the shared literal identity policy and untouched dispatched value, the distinct withdrawal state and original diagnostic, single-flight caller-owned same-ID retry, correlation-less notification safety, 87-key EN/FR readiness, rendered-only ARIA, first-statement disabled-submit protection, terminal-only lease release, and all previously green focused/full lanes.

### 2026-09-21 — Review pass 6 repair
- Triggering findings: the third resumed review proved that the visible format-control token collides with literal escape-looking text, lone UTF-16 surrogates mutate to U+FFFD during JSON serialization, audit context bypasses the display boundary, and grant delivery serialization does not survive renderer replacement.
- Amendment: require a self-escaping one-to-one presentation encoding at every global-administrator identity consumer, reject ill-formed UTF-16 before preview/serialization, add the omitted component and restore-status verification, and move initial/retry delivery single-flight ownership into the shared aggregate lease with durable completion publication.
- Known-bad state avoided: two distinct identities must never look identical; a dispatched identity must never differ from the reviewed literal; audit handoff must not reintroduce bidi spoofing; and a replacement renderer must not overlap the original same-ULID POST.
- KEEP: preserve trusted projection provenance and real current/stale chain tests, all established well-formed literal boundaries, original copied/dispatched values, the distinct reversible withdrawal state and original diagnostic, exact snapshot-plus-lease notification guards, stable correction lifecycle keys, the 87-key EN/FR gate, caller-owned ULID, terminal-only release, and every previously green focused/full verification lane.

## Review Triage Log

### 2026-08-31 — Review pass
- verdicts: 47 findings — high 15, medium 20, low 2, false 8, maybe-false 2
- findings:
  - `[medium]` `[bad_spec]` Pending or unknown status is mislabeled as a tracking mismatch — `ApplyStatus` checks identity before its null-status branch; the safeguard now requires pending/unknown handling first.
  - `[high]` `[bad_spec]` Concurrent confirmations can clear a lease already marked by another handler — the failed `TryMarkDispatched` arm nulls `_grantAdmissionLease`; the safeguard now requires single-flight ownership.
  - `[high]` `[bad_spec]` A superseded revalidation can dispatch a later preview without its acknowledgement — no preview/lease/generation identity is rechecked after the awaits; the safeguard now binds every continuation to the exact preview attempt.
  - `[high]` `[bad_spec]` Ambiguous recovery can strand a marked lease after preview invalidation — correlation-less `UnableToVerify` is neither retainable nor releasable; the safeguard now requires same-id recovery for every dispatched ambiguous state.
  - `[high]` `[bad_spec]` Renderer replacement can adopt non-ambiguous request-sent work that cannot poll or redispatch — the adopted null-correlation state fails both recovery branches; the safeguard now retains it as explicitly ambiguous and wakes the adopter.
  - `[medium]` `[bad_spec]` Initial preview facts can come from a stale cached complete snapshot — initiation passes `_completeSnapshot` without a new bounded walk; the safeguard now requires a fresh complete load before preview.
  - `[high]` `[bad_spec]` Preview-composition exceptions can escape and retain a lease — initiation catches only caller cancellation and revalidation catches nothing; the safeguard now requires bounded fail-closed recovery and correct lease disposition.
  - `[high]` `[bad_spec]` Composition can deny authority without collapsing already rendered administrator rows — the unavailable preview changes only command state; the safeguard now requires authoritative denial to collapse privileged data.
  - `[medium]` `[bad_spec]` The confirm control remains enabled after live prerequisites change — support, viewport, authority, and exact ownership are rechecked only after activation; the safeguard now requires the modal state to reflect current prerequisites.
  - `[maybe-false]` `[defer]` Focus wrappers may produce an unusable active-element sequence — the wrappers are programmatically focusable, but only a real browser active-element trace can settle Tab and Shift+Tab behavior; re-derivation must add that behavioral evidence before deciding.
  - `[medium]` `[bad_spec]` `aria-modal` leaves background actions operable — preview state does not make the surrounding surface inert or disable removal/navigation controls; the safeguard now requires background isolation.
  - `[medium]` `[bad_spec]` Failure-specific recovery is discarded — `RecoveryKey` is neither retained nor rendered; the safeguard now requires visible associated recovery.
  - `[medium]` `[bad_spec]` Most claimed BFF-composed facts are static UI text outside preview completeness — missing or contradictory resources cannot fail `IsComplete`; the safeguard now makes every rendered fact part of BFF-owned completeness evidence.
  - `[medium]` `[bad_spec]` Verified event lifecycle can regress from projection-pending to accepted — `Received`/`Processing` assigns `Accepted` unconditionally; the safeguard now requires monotonic evidence.
  - `[maybe-false]` `[defer]` Plain `TimeoutException` may escape tracked dispatch — current concrete clients normally surface gateway, HTTP, or task-cancellation exceptions; a demonstrated reachable timeout type is needed to settle whether another catch is required.
  - `[false]` `[reject]` Fixed-scope string constants necessarily diverge from command identity — current values exactly match the authoritative contract and the story intentionally fixes this identity; no bad outcome exists today.
  - `[medium]` `[bad_spec]` The component gateway forces every status identity to verified — this masks null/unverified status paths; the safeguard now requires per-case identity evidence and an exact handle assertion.
  - `[medium]` `[bad_spec]` Projection test responses repeat by default and hide extra reads — the changed default suppresses queue-exhaustion failures across the suite; the safeguard now restores strict opt-in repetition.
  - `[low]` `[bad_spec]` New tests violate the explicit PascalCase naming rule — the added methods use underscores; re-derivation must rename them.
  - `[high]` `[bad_spec]` Critical concurrency, replacement, exception, prerequisite, focus, isolation, and recovery paths lack tests — the amended verification obligations enumerate those cases.
  - `[high]` `[bad_spec]` Duplicate confirmation can make a marked lease ownerless — independently confirmed at the dispatch guard; covered by the single-flight amendment.
  - `[high]` `[bad_spec]` Authorization collapse can retain an undispatched preview lease forever — collapse cannot reconcile `Previewed` and does not abandon it; covered by the lease-disposition amendment.
  - `[high]` `[bad_spec]` Mismatched acceptance identity or blank correlation can strand the fixed lock — the resulting state is neither valid reconciliation nor terminal; covered by the same-id recovery amendment.
  - `[high]` `[bad_spec]` A second authority-resolution exception can leave the event callback and pre-dispatch lock stuck — confirmed in revalidation; covered by bounded composition recovery.
  - `[false]` `[reject]` `Int32.MaxValue` administrator counts can reach the preview — the only production caller uses the bounded complete-population loader, so that population size is unreachable.
  - `[high]` `[bad_spec]` A different target can dispatch without its own acknowledgement — confirmed by the unguarded post-await handoff; covered by exact-attempt continuation guards.
  - `[high]` `[bad_spec]` Adopted request-sent work without correlation cannot resume — confirmed by the ambiguous/status branch predicates; covered by explicit ambiguous retention and wake-up.
  - `[medium]` `[bad_spec]` Status application is non-monotonic — older received/processing evidence regresses projection-pending; covered by the lifecycle amendment.
  - `[medium]` `[bad_spec]` SignalR changes request-sent/accepted into projection-pending without event evidence — `SignalRNudge` mutates lifecycle before lookup; the safeguard now limits it to requesting reconciliation.
  - `[false]` `[reject]` Opaque version inequality violates the intended advancement rule — the design notes deliberately require a different opaque version plus exact-command event evidence because this projection exposes ETag-like tokens.
  - `[medium]` `[bad_spec]` Pending status is reported as identity mismatch — duplicate confirmation of the null-status ordering defect; covered by the status amendment.
  - `[medium]` `[bad_spec]` Unavailable previews omit associated recovery — duplicate confirmation that `RecoveryKey` is dropped; covered by BFF/recovery completeness.
  - `[high]` `[bad_spec]` Authority loss during composition leaves privileged rows visible — duplicate confirmation of the collapse gap; covered by authoritative denial handling.
  - `[medium]` `[bad_spec]` Dialog semantics do not isolate focus or background actions — background controls remain reachable; covered by real modal containment.
  - `[medium]` `[bad_spec]` Six or more preview facts cannot participate in BFF completeness — duplicate confirmation of static consequence copy; covered by the BFF evidence amendment.
  - `[low]` `[bad_spec]` Added tests use underscore-separated names — duplicate confirmation of the naming deviation; re-derivation must use PascalCase.
  - `[medium]` `[bad_spec]` Qualified confirmation replaces the paged display with the entire complete population — `_snapshot = snapshot` removes paging and can render all administrators; the safeguard now preserves the paged display contract.
  - `[medium]` `[bad_spec]` Rendered consequence counts are only selector-tested — the stub localizer hides the numeric binding; the amended tests must assert both actual values.
  - `[medium]` `[bad_spec]` The page's fixed aggregate status handle is not verified — the stub ignores the handle and forces identity true; the amended tests must capture message, correlation, and aggregate.
  - `[medium]` `[bad_spec]` `EventsStored` and `EventsPublished` qualification is untested — only `Completed` exercises the branch; the amended matrix requires positive and zero-event cases for all three statuses.
  - `[high]` `[bad_spec]` Renderer replacement is tested only below the component boundary — no test disposes a page and proves automatic same-id adoption/resumption; the amended tests require that end-to-end component transition.
  - `[medium]` `[bad_spec]` Focus containment and launcher restoration lack behavioral verification — selector tests cannot prove active-element behavior; the amended tests require browser-level focus and background isolation evidence.
  - `[false]` `[reject]` The product diff should itself prove skill invocation — workflow execution is control-plane evidence, not a product-code responsibility.
  - `[false]` `[reject]` Product implementation diverges merely because the slug admits a narrower reading — repository planning context legitimately elaborates the named story.
  - `[false]` `[reject]` Absence of `operator_actions` is a defect — no acceptance criterion requires an external human setup action, so the ordinary completion branch applies.
  - `[false]` `[reject]` Repository tests must prove subagent use, commit existence, or orchestrator bookkeeping — those are workflow controls verified outside the product test surface.
  - `[false]` `[reject]` The in-review spec must already contain final command results and historical sprint proof — `## Auto Run Result` and terminal status are written only after review, while the unchanged sprint file is intentionally not evidence.

### 2026-08-31 — Review pass 2
- verdicts: 31 findings — high 8, medium 16, low 1, false 4, maybe-false 2
- findings:
  - `[medium]` `[bad_spec]` Same-attempt stale-preview invalidation has no behavioral test — current code compares the rebuilt preview, but no test changes version, count, completeness, or target presence and proves zero dispatch plus undispatched-lease release; the amendment names that matrix row.
  - `[medium]` `[bad_spec]` Modal focus containment is verified only as markup — existing tests never assert initial active element, Tab/Shift+Tab wrapping, or actual launcher restoration; the amendment requires browser-level active-element evidence.
  - `[medium]` `[bad_spec]` Tracked grant dispatch capability is not tested independently — no case enables legacy dispatch while disabling tracked dispatch, so deleting the evaluator guard would stay green; the amendment requires an independent capability case.
  - `[medium]` `[bad_spec]` Renderer replacement does not verify retained positive event evidence — adoption is tested only with `HasCommandEventEvidence: false`; the amendment requires projection-pending remount with monotonic positive evidence through final confirmation.
  - `[high]` `[bad_spec]` Cancel during the initial complete walk can later reopen the preview and reserve the lock — preview creation captured no generation and its final callback ignored Cancel's generation increment; generation-bound cancellable initiation now prevents stale lease acquisition.
  - `[high]` `[bad_spec]` The target field remains editable during preview composition — target A is captured before awaits while the field can change to B, producing a contradictory confirmation surface; the amendment binds and freezes the exact input for the operation.
  - `[medium]` `[bad_spec]` The start focus sentinel does not focus Cancel — it toggles `AutoFocus`, and `OnAfterRenderAsync` only clears the flag; actual focus calls or a Fluent focus mechanism are now required.
  - `[medium]` `[bad_spec]` Cancel and Escape do not reliably restore the launcher — the derivation removed the launcher reference and replaced `FocusAsync` with a dynamic `autofocus` attribute; actual restoration is now mandatory.
  - `[medium]` `[bad_spec]` The modal leaves background status content focusable — only the list is inert and disabled accordion headers do not isolate descendants; the amendment requires whole-background keyboard and assistive inertness.
  - `[high]` `[bad_spec]` Ambiguous compatibility dispatch strands the correction surface — the gateway returns request-sent tracking, but correction failure application discards its message id and recovery key after marking a nonterminal lease; all grant consumers must retain same-id ambiguity.
  - `[high]` `[bad_spec]` Correction status can advance from an unverified non-fixed handle — its handle omits the aggregate and status application ignores verified identity before creating retained reconciliation; fixed identity verification is now explicit at every consumer.
  - `[high]` `[bad_spec]` Tracked capability defaults to generic dispatch — the correction consumer inherits a fail-open default while using the compatibility method, exposing ambiguous-lease loss; tracked support must now be supplied explicitly.
  - `[maybe-false]` `[defer]` Retained reconciliation does not compare preview target with reconciliation target — every current internal producer binds them, so a contradictory producer must be demonstrated before this is a reachable defect.
  - `[false]` `[reject]` Admission must parse every retained tracking value — current producers already mint or validate canonical message ULIDs, correlation ids are intentionally opaque, and no invalid production producer was shown.
  - `[medium]` `[bad_spec]` Initial and confirmation bounded walks ignore cancellation — `CancellationToken.None` prevents Cancel, authority collapse, disposal, or supersession from stopping I/O and amplifies the stale-initiation race; generation-bound cancellation is now required.
  - `[false]` `[reject]` Fail-closed preview-composition exception mapping hides a defect — the wrapper intentionally converts composition failure into incomplete recoverable evidence, exactly as the specification requires.
  - `[medium]` `[bad_spec]` A control labelled status refresh can issue a command POST — correlation-less ambiguity performs same-id redispatch without disclosing delivery retry; the amendment requires truthful action copy.
  - `[medium]` `[bad_spec]` The correction test gateway forces all statuses to verified identity — this masks the production identity gap and violates the strict-double safeguard; mismatch coverage is now named explicitly.
  - `[medium]` `[bad_spec]` Focus and background isolation tests assert attributes rather than behavior — broken sentinel wrapping and launcher focus remain green; the amendment requires real active-element and background-focus evidence.
  - `[medium]` `[patch]` English consequence copy promises authority is exercisable after projection confirmation while admitting downstream enforcement timing is unknown — re-derivation must describe confirmation as observed evidence, not an enforcement boundary.
  - `[low]` `[patch]` New French preview strings omit native diacritics throughout — the direct resource correction is carried into re-derivation as native-quality French copy.
  - `[high]` `[bad_spec]` Initial-preview cancellation race is independently reproduced — Cancel clears state but the stale continuation can acquire the fixed lease and reopen; covered by generation-bound preview initiation.
  - `[high]` `[bad_spec]` Support or viewport withdrawal can race the final dispatch arm — revalidation snapshots prerequisites before a later renderer callback that marks dispatch without rechecking them; the final-arm callback must evaluate current evidence.
  - `[high]` `[bad_spec]` Retryable gateway Problem Details can become terminal — tracked dispatch ignores `EventStoreGatewayException.Retryable` outside its status-code list, losing same-id recovery; the gateway amendment requires retryable metadata mapping.
  - `[medium]` `[bad_spec]` A non-caller `OperationCanceledException` can escape tracked dispatch — the page compensates, but the correction caller converts it to terminal failure; transport cancellation without caller cancellation must map to same-id ambiguity.
  - `[maybe-false]` `[defer]` Malformed global-administrator correction intents can map tenant commands to removal reconciliation — normal evaluator/routing pairs domain and command type correctly; a reachable malformed public caller must be demonstrated before repair.
  - `[false]` `[reject]` Preview count can overflow at `Int32.MaxValue` in production — the bounded loader caps the complete population at 50 pages of 20 rows, making that input unreachable.
  - `[false]` `[reject]` Differing opaque projection tokens permit confirmation without causal evidence — confirmation already requires exact-command positive event evidence, complete current fixed-scope evidence, and target presence, matching the deliberate opaque-token design.
  - `[medium]` `[bad_spec]` Resource-key presence is mistaken for localized-fact presence — unresolved or blank localized values can leave `IsComplete` true; the amendment makes resolved EN/FR values part of completeness.
  - `[medium]` `[bad_spec]` Launcher focus restoration has no behavioral assertion — checking only `autofocus` cannot prove the browser's active element; covered by the active-element requirement.
  - `[medium]` `[bad_spec]` The correction strict double globally rewrites identity evidence — this independently confirms that mismatch paths cannot be exercised; the amendment forbids global identity forcing and names the negative case.

### 2026-09-01 — Review pass 3
- verdicts: 40 findings — high 11, medium 23, low 3, false 2, maybe-false 1
- findings:
  - `[medium]` `[patch]` Grant-preview readiness accepted fallback-only localization — preview completeness now requires explicit resolved values in both invariant English and French resource sets.
  - `[high]` `[patch]` Count resources could omit or corrupt required placeholders — composition now formats sentinel values and requires both `{0}` and `{1}` to survive valid composite formatting.
  - `[high]` `[patch]` Initial preview authorization, complete-walk, and composition work ignored cancellation — the generation token now flows through every initial preview stage and caller cancellation is preserved.
  - `[high]` `[patch]` The renderer's final dispatch arm could use stale preview readiness or ownership — the callback now rechecks live readiness, exact attempt identity, acknowledgement, capabilities, and the exact lease before marking dispatch.
  - `[medium]` `[patch]` Correction adoption discarded retained safe recovery — reconciliation now preserves support-safe message and recovery keys, including same-command delivery retry guidance.
  - `[medium]` `[patch]` Correction reconciliation could lose an ambiguous restore's lifecycle meaning — restore adoption now retains ambiguity, delayed audit state, assertive announcement, and same-ID retry semantics.
  - `[medium]` `[patch]` Later correction progress retained stale failure copy — accepted, pending, already-applied, and event-evidence transitions now clear obsolete safe message, recovery, rejection, and ambiguity fields.
  - `[medium]` `[patch]` Grant-specific ambiguity keys could leak onto removal correction states — ambiguous same-ID grant recovery is now restricted to restore-access actions; removal stays terminal and uses correction-specific copy.
  - `[low]` `[patch]` French grant resources contained pervasive missing diacritics and awkward wording — the Story 4.2 resource block now uses native-quality French copy.
  - `[medium]` `[patch]` Initial preview cancellation and supersession had no behavioral proof — tests now cancel during authorization, complete walk, and BFF composition and assert no stale preview or lease.
  - `[high]` `[patch]` Final-arm support, viewport, readiness, and lease races lacked coverage — parameterized component tests now withdraw each prerequisite and prove zero dispatch plus exact undispatched-lease release.
  - `[medium]` `[patch]` Projection mutation between preview and final confirmation lacked an explicit refusal test — tests now mutate the evidence and require a fresh reviewed preview before dispatch.
  - `[medium]` `[patch]` Focus behavior was inferred from markup — the page now focuses referenced interactive controls and tests capture actual focus interop for initial focus, both sentinels, cancel, and launcher restoration.
  - `[medium]` `[patch]` Retryable gateway and transport-cancellation branches were under-specified in tests — gateway cases now pin retryable Problem Details and non-caller cancellation as same-message ambiguity.
  - `[medium]` `[patch]` The correction retry action was not proven to redispatch the retained identity — a component test now clicks the honest delivery-retry action and asserts the original message ID is reused.
  - `[medium]` `[patch]` Correction status tests did not prove mismatched identity fails closed — negative tests now retain the attempt and prevent projection confirmation from unverified status.
  - `[medium]` `[patch]` Renderer replacement did not prove positive event evidence survives — replacement tests now retain event evidence through adoption and release only on qualified projection confirmation.
  - `[medium]` `[defer]` Concurrent workspace tabs lack browser visibility and focus proof for inactive panels — this separate committed change is outside Story 4.2; an authenticated browser active-element and visibility trace would settle it.
  - `[false]` `[reject]` Tenant-audit tests hide user-facing Razor text by removing `@code` — the scan intentionally excludes identifiers such as `MessageId`; markup, resources, rendered output, and support-safety tests still cover visible copy, and no forbidden literal was demonstrated.
  - `[false]` `[reject]` The reviewed diff improperly mixes unrelated feature changes — the cited submodule, AppHost, and workspace changes are separate concurrent commits; Story 4.2's uncommitted and staged file set does not own or revert them.
  - `[high]` `[patch]` Cancel or target replacement could leave initial preview I/O running — cancellation is now generation-bound and observable at each awaited initial stage, with stale continuations unable to reserve admission.
  - `[medium]` `[patch]` Replacing the target after an open preview left the old intent and lease active — exact input replacement now invalidates that preview and abandons only its undispatched lease.
  - `[high]` `[patch]` Preview readiness could disappear after revalidation but before lease marking — the final renderer arm now checks current BFF preview readiness and invalidates the undispatched attempt on loss.
  - `[maybe-false]` `[patch]` The existing keyed Fluent autofocus mechanism might not focus the intended browser element — no runtime failure was proven, but this grouped focus repair removes the uncertainty with explicit `ElementReference.FocusAsync` calls and target-aware interop tests.
  - `[medium]` `[patch]` EN/FR localized-fact fallback could make an incomplete preview appear complete — both cultures now require every explicit nonblank, non-key-echo fact value.
  - `[high]` `[patch]` Malformed or incomplete administrator-count formats could pass readiness and fail during render — count composition now fails closed unless valid formatting consumes both count arguments.
  - `[high]` `[patch]` Correction confirmation used ordered-version semantics incompatible with opaque projection tokens — restore confirmation now follows Story 4.2's nonblank opaque-version inequality only after exact-command positive event evidence.
  - `[high]` `[patch]` Ambiguous removal delivery could become a nonterminal state with no safe retry and strand admission — removal ambiguity is now terminal correction failure and releases through the normal completion path.
  - `[low]` `[patch]` French preview and lifecycle text was not publication quality — corrected diacritics, punctuation, terminology, and evidence wording now match the English intent without overclaiming.
  - `[high]` `[patch]` The combined race, localization, correction, gateway, focus, and retention matrix was incomplete — focused Story 4.2 and correction tests now exercise all named branches and pass as a 549-test lane.
  - `[medium]` `[patch]` Initial preview cancellation was not verified at all three await boundaries — the component suite now proves cancellation during authorization, bounded projection loading, and preview composition.
  - `[high]` `[patch]` No test proved the last renderer callback rejects changed prerequisites — parameterized tests now remove authorization/support/viewport/readiness/ownership evidence at the final arm and assert no command.
  - `[medium]` `[patch]` Focus tests did not identify the actual focused target — test JS interop now records element-reference IDs and asserts preview Cancel, acknowledgement, and launcher targets.
  - `[medium]` `[patch]` Gateway tests omitted retryable Problem Details and non-caller `OperationCanceledException` — both outcomes are now asserted as recoverable ambiguity with the caller-owned message identity.
  - `[medium]` `[patch]` Localizer tests omitted absent, blank, key-echo, culture-specific, and count-format failures — the BFF suite now covers each fail-closed resource shape.
  - `[medium]` `[patch]` Correction identity-negative status behavior was untested — tests now prove unverified restore status cannot contribute event evidence or confirm projection.
  - `[medium]` `[patch]` The correction same-ID retry control was not exercised — the new interaction test proves the label, retained identity, marked lease, and retry dispatch.
  - `[medium]` `[patch]` Positive grant event evidence could be lost during correction renderer replacement without detection — adoption and qualified-release tests now pin monotonic evidence retention.
  - `[low]` `[patch]` Main-flow ambiguous delivery still used generic refresh wording — the visible action now says delivery retry and its recovery warns against creating a new grant attempt.
  - `[medium]` `[defer]` Concurrent Memories secret-store topology lacks deployed Aspire health evidence — this separate AppHost change is outside Story 4.2; Aspire model inspection and a healthy deployment with the intended provider would settle it.

### 2026-09-01 — Review pass
- verdicts: 61 findings — high 1, medium 16, low 37, false 7, maybe-false 0
- findings:
  - `[low]` `[reject]` Background isolation stops at the page content area, leaving the app shell tabbable — refuted for keyboard users: the preview's start/end focus sentinels return focus to Cancel/Acknowledge, so Tab never reaches the shell; only the shell's assistive-technology exposure remains, and the fix is a layout-shell restructure rather than a direct correction.
  - `[medium]` `[patch]` New focus interop is unguarded — `OnAfterRenderAsync` and both sentinel handlers now route through `FocusSafelyAsync`, which swallows `JSDisconnectedException`/`ObjectDisposedException` exactly as `GlobalAdministratorCorrectionPanel` and five sibling components already do.
  - `[false]` `[reject]` `ConfigureAwait(false)` on the focus chain drops off the renderer Dispatcher — the recorded circuit-teardown mode needs an `EventCallback`/`StateHasChanged` off-Dispatcher; this method invokes neither, and `ElementReference.FocusAsync` is a plain interop call.
  - `[low]` `[reject]` Two raw `<fluent-button>` custom elements — `FluentButton` exposes no `ElementReference`, unlike the `FluentTextInput`/`FluentCheckbox` `.Element` used elsewhere in the file, so the raw element is the only way to obtain the focus target the story requires.
  - `[medium]` `[patch]` Localization completeness runs a full resource scan on the render path — the fixed-key walk is now resolved once per composition instead of twice per read, several times per render.
  - `[low]` `[reject]` The validated culture set is hardcoded to invariant + `fr` — the app ships exactly two resource sets and configures no additional supported cultures, so the checked set is the shipped set.
  - `[low]` `[reject]` `resourceLocalizer` is an optional trailing parameter whose absence means "not ready" — every collaborator on this type is optional by design and absence already fails closed with its own localized reason.
  - `[low]` `[reject]` Two hand-synchronized lists of required fact keys — real drift risk, but deriving one from the other is a restructure, not a direct correction; the new preview-key coverage test pins the two lists' lengths instead.
  - `[low]` `[reject]` Duplicated capability predicates — behaviour is identical and the memoization above removes the cost that made the duplication matter.
  - `[false]` `[reject]` `ConfirmGrantAsync`'s cancellation catch has no exception filter — a transport `OperationCanceledException` never reaches it: `DispatchGrantAsync` wraps the gateway call in its own bare `catch` that maps to same-message-id ambiguity.
  - `[low]` `[reject]` The correction retry path is uncancellable and over-catches — `CancellationToken.None` matches the panel's pre-existing submit paths, and mapping a swallowed failure to ambiguity is the fail-closed outcome the intent requires.
  - `[low]` `[reject]` Grant-namespaced recovery keys leak into the removal path — the shared recovery string reads "Refresh the complete fixed-scope projection and rebuild the preview", which is accurate for the correction flow too; only the key's namespace is grant-flavoured and users never see it.
  - `[low]` `[patch]` `Tenants.Correction.GlobalAdmin.AlreadyGranted` is orphaned — deleted from both resx files and from the correction-panel localizer double; the localizer parity gate now passes on the reduced set.
  - `[low]` `[reject]` "Already a global administrator" is reported as a verification failure — the intent explicitly forbids `AlreadyApplied`, NoOp, and success for an existing grant target, so a non-success state is mandated; only the generic state label reads oddly, and no other state exists to move it to.
  - `[false]` `[reject]` `HasCommandTracking` and `TryGetTrackingHandle` disagree — deliberate and consistent: `RefreshStatusAsync` branches on the ambiguous/no-correlation shape before ever calling the handle-based status path, so no caller reaches an unexpected `false`.
  - `[low]` `[reject]` No dedicated unavailable reason for missing tracked dispatch — adding an enum member plus reason-to-copy wiring is more than a direct correction, and the generic lifecycle-support copy is not misleading.
  - `[low]` `[reject]` `SupportsTrackedGrantDispatch` default flipped without compiler enforcement — fail-closed is correct and both construction sites set it explicitly; making it `required` changes the public record's contract.
  - `[low]` `[defer]` `Retryable == false` is not honored and only the grant dispatch path was updated — over-classifying as ambiguous is fail-closed, and the sibling tracked dispatches are unchanged pre-existing behaviour outside this story's fixed-scope intent.
  - `[medium]` `[defer]` Deleted workspace-panel test coverage was not replaced — confirmed, and the deletion is in this story's own commit `8da765ad`, not a separate concurrent one; bUnit cannot observe Fluent's client-side panel flip, so a browser lane is what would settle it.
  - `[medium]` `[reject]` Five root gitlinks move with no File List declaration — real and reproduced by `validate-story-gitlinks.py`, but the only fix is to declare them in this build's spec File List.
  - `[low]` `[reject]` French repair is partial and introduces terminology drift — the untouched neighbours are pre-existing strings this story never edited, and "statut"/"état" both render correctly.
  - `[low]` `[reject]` The new localization-failure copy is not actionable for its audience — a missing satellite resource is a deployment defect; naming it is more useful than hiding it behind a generic support pointer.
  - `[low]` `[patch]` Dead `_grantPreviewInFlight` branch plus an unmarshalled cross-thread read — the branch is defence in depth behind the input's `disabled` term and is kept; the missing half was that no test pinned that `disabled` term, which the target-replacement test now asserts on both sides of the invalidation.
  - `[false]` `[reject]` Pending status arrives with `HasVerifiedCommandIdentity` still false — pending is classified first: `status.Status is null` returns before the identity gate is reached.
  - `[high]` `[patch]` `status.EventCount is null` on `EventsStored`/`EventsPublished` collapses to `UnableToVerify` — confirmed end to end: `CommandStatusRecord.EventCount` is "Completed status only" and `AggregateActor.WriteAdvisoryStatusAsync(command, CommandStatus.EventsStored)` leaves it null, so the story's own positive event evidence was reported as missing on both the grant page and the correction panel. Both snapshots now qualify `EventsStored`/`EventsPublished` on the status alone and keep the positive-count requirement for `Completed`; the two theories that pinned the fictional counts were rewritten to the shipped shape.
  - `[low]` `[patch]` `CultureInfo.GetCultureInfo("fr")` sits outside the fail-closed try — moved into its own guarded lookup so globalization-invariant hosting fails closed instead of throwing out of a render-path property.
  - `[low]` `[reject]` Only invariant and `fr` cultures are validated — same refutation as above: those are the shipped resource sets.
  - `[low]` `[reject]` A null localizer is indistinguishable from incomplete resources — absence already produces the localized unavailable reason and recovery.
  - `[medium]` `[patch]` Delivery-retry click can do nothing silently — `CanRefreshGrantStatus` now withdraws the action when the redispatch's own live prerequisites cannot hold, and a new test proves the withdrawal.
  - `[medium]` `[patch]` `isRedispatch` with lapsed live prerequisites returns with no state change or feedback — same root cause and same fix as the entry above.
  - `[low]` `[reject]` Correction delivery-retry guards return without a render — the panel's inner guards mirror `CreateCorrectionDecision().CanRefresh`, the same predicate that enables its button, so the silent window is a narrow race rather than a reachable dead end.
  - `[low]` `[reject]` Component disposed while the tracked redispatch await is outstanding — the `InvokeAsync` throw happens only during renderer teardown, where the renderer already absorbs it.
  - `[low]` `[reject]` The bare catch also swallows cancellation and disposal — mapping them to retained ambiguity is the fail-closed outcome; releasing them as terminal is what the intent forbids.
  - `[medium]` `[patch]` `RequestSent` retains a stale message id on the untracked revoke path — it now takes exactly the identity its caller owns, so the revoke path returns to letting acceptance supply the id instead of pairing an old message id with a new correlation id.
  - `[false]` `[reject]` A non-ambiguous failure clears an established correlation id — unreachable: every resubmit passes through `RequestSent`, which has already cleared `CorrelationId` to null.
  - `[false]` `[reject]` `_snapshot is null` short-circuits the tracked-dispatch requirement — with no snapshot there is no intent and nothing is submittable, so no unsupported path opens.
  - `[medium]` `[patch]` `JSDisconnectedException` from the new focus interop — same entry and same fix as the unguarded-focus finding above.
  - `[low]` `[reject]` Shell chrome outside the inert stack — same refutation as the background-isolation finding above.
  - `[medium]` `[defer]` Inactive workspace panels lost their invisibility guarantee — same entry as the deleted-coverage finding above.
  - `[low]` `[patch]` `_focusGrantAcknowledgementPending` is never set true — the dead field and its `OnAfterRenderAsync` branch were deleted; the acknowledgement checkbox is still focused by the end-sentinel handler.
  - `[low]` `[reject]` `SupportsTrackedGrantDispatch` initializer removed — same as above.
  - `[low]` `[patch]` Orphaned EN/FR resource pair plus a stale test-double string — same entry and same fix as the orphan-key finding above.
  - `[low]` `[reject]` Removed revoke `AlreadyApplied` and gateway-exception tests — the revoke `Completed`+`EventCount == 0` arm is unreachable in production because the platform writes a null count instead of zero; re-adding a test for it would pin a shape the platform never emits.
  - `[low]` `[reject]` Adjacent French `Remove.*` values keep undiacritized text — pre-existing strings this story never touched.
  - `[low]` `[patch]` Target-replacement invalidation is reachable only through bUnit and the `disabled` term is unpinned — the target-replacement test now asserts the field is disabled while a preview is open and live again after invalidation.
  - `[medium]` `[patch]` The grant page's delivery-retry action is never clicked by any test — the renderer-replacement test now clicks it and proves a third dispatch on the retained message id.
  - `[medium]` `[defer]` Inactive workspace tab panels lost their `hidden`/`aria-hidden` assertions with no browser lane — same entry as above.
  - `[medium]` `[patch]` The localization readiness gate is a production kill switch tested only against substitutes — a new test resolves the real `IStringLocalizer<TenantsResources>` over the shipped resources and asserts the gate holds, plus a test tying the gate's key list to the keys a real preview renders.
  - `[low]` `[defer]` The new ambiguity classification was adopted only in the grant dispatch method — same entry as the `Retryable` finding above.
  - `[medium]` `[patch]` Culture mutation and double enumeration on the render path — same entry and same fix as the memoization above.
  - `[low]` `[patch]` `GetCultureInfo` outside the guarded walk — same entry and same fix as above.
  - `[low]` `[reject]` The forbidden-copy scan now truncates `.razor` inputs at `@code` — all user-visible copy comes from the resx files, which are still scanned whole; the narrowing only stops matching C# identifiers.
  - `[medium]` `[defer]` The AppHost Memories secret-store component name is coupled to `EmbeddingSecretStore.SecretStoreName` with no topology test — a separate concurrent commit (`7453ba5b`) outside this story; already recorded in this spec's deferred list.
  - `[low]` `[defer]` Command-status semantics are asserted only against stubs — the concrete `EventCount` assumption is now corrected from the platform source, but nothing pins EventStore's status contract from this repository.
  - `[false]` `[reject]` "Advanced" is implemented as opaque-token inequality — the Design Notes settle this deliberately: this projection exposes ETag-like tokens, so inequality plus exact-command event evidence is the intended rule.
  - `[low]` `[reject]` Authorization is exercised through a private-field poke — a test technique for an in-process component, not a production defect.
  - `[medium]` `[defer]` Focus containment and background inertness are proven at the interop layer, not in a browser — real, and unsettled by this pass; a browser active-element and background-focus trace is what would close it.
  - `[low]` `[defer]` Measured viewport is driven through the observation singleton and never measured — same browser-lane entry as above.
  - `[low]` `[reject]` Localization completeness is verified for cultures a third-culture viewer will not see — same refutation as the hardcoded-culture entries.
  - `[low]` `[reject]` The message-id exposure scan now covers markup only — same refutation as the forbidden-copy entry.
  - `[medium]` `[reject]` The diff carries material outside any reading of the intent (gitlinks, AppHost, tab migration) — the gitlink half's only fix is to edit this build's spec File List; the AppHost and tab halves are already deferred.


### 2026-09-01 — Review pass
- verdicts: 56 findings — high 1, medium 14, low 20, false 18, maybe-false 3
- findings:
  - `[low]` `[reject]` `git diff --check` reports a trailing blank line at EOF of this spec — real, but the only fix edits this build's spec; the EOF is normalized by the mandatory finalization write-back instead.
  - `[medium]` `[patch]` `scripts/validate-story-gitlinks.py` FAILs: five `references/` gitlinks move inside this story's own commit `8da765ad` with no declaration anywhere — a `## File List` section now declares all five with their exact `old -> new` SHAs and the reason each moved.
  - `[false]` `[reject]` "sprint-status.yaml flipped backlog -> done inside this change" — `git log 7e88a571..HEAD -- sprint-status.yaml` is empty; the flip is an uncommitted working-tree edit owned by the orchestrator, not a change this story's commits made.
  - `[low]` `[reject]` Two unrelated spec files change status inside commit `8da765ad` (`spec-frontcomposer-shell-and-adminui-fluent-conformance-audit.md` ready-for-dev -> done, `spec-frontcomposer-tab-contract.md` ready-for-dev -> in-progress) — commit hygiene only; the fix is rewriting already-pushed history and would corrupt two other stories' recorded state.
  - `[low]` `[reject]` The `UnableToVerify` + `SafeMessageKey` branch in `CreateCorrectionDecision` is shadowed by the preceding `HasCommandTracking` branch — confirmed unreachable for tracked snapshots, but the reason still renders from `SafeMessageKey` and the recovery that does render ("Refresh command status and the fixed projection until terminal evidence is available") is more apt for a tracked state than the shadowed "rebuild the preview" copy. No user-visible harm; reordering would degrade the copy.
  - `[false]` `[reject]` `ConfirmProjection`'s refusal branch "leaves lifecycle untouched and stores unqualified evidence" — the outer guard already proved the projection Ready/Current/complete/non-stale, so `LastConfirmedProjectionEvidence` is qualified; what is unqualified is the correction, and holding the lifecycle at Accepted/ProjectionPending is the correct pending state. `SafeMessageKey` distinguishes it.
  - `[low]` `[patch]` `RefreshGrantStatusAsync` re-inlined the capability list that `CanRetryAmbiguousGrantDelivery` reads through `AreGrantDispatchCapabilitiesCurrent()`, omitting the grant-preview readiness term — the handler now calls the same predicate, so control and handler cannot disagree.
  - `[low]` `[patch]` `AreGrantPreviewPrerequisitesCurrent` restated seven terms already inside `AreGrantDispatchCapabilitiesCurrent()` and re-walked localization readiness twice per render-path evaluation — the duplicates were deleted.
  - `[medium]` `[patch]` The readiness gate demanded only the nine consequence `.Value` facts; every `<dt>` label, the dialog chrome, and both localization-failure strings were unchecked, so a missing one still passed the gate and rendered a raw resource key inside the modal that gates a platform-authority grant — `RequiredGrantFactKeys` now carries all 26 strings the dialog renders. (Root cause shared with the row below.)
  - `[medium]` `[patch]` Three hand-copied key lists with a guard test asserting only equal lengths — `GrantPreviewReadinessCoversEveryFactKeyARealPreviewRenders` compared a rendered-key count against a test-local copy, so renaming a production key kept it green. The test list is now derived from the production `RequiredGrantFactKeys` and the test asserts containment of every rendered fact plus every chrome key.
  - `[medium]` `[patch]` `role="dialog" aria-modal="true"` on an in-flow `<section>` below the administrator table with no positioning, elevation, or overflow — the surface that announces itself as modal rendered as more page, leaving the inert background unchanged and apparently interactive. Added a scoped fixed, centred, elevated, scrollable dialog rule plus a forced-colors border fallback.
  - `[low]` `[reject]` `inert` + `aria-hidden` on the grant area silences its live regions while the preview is open — no grant or remove lifecycle transition can occur in that window (dispatch closes the preview by leaving `Previewed`, and authorization collapse swaps to the unauthorized render branch, outside the hidden subtree), so no announcement is lost.
  - `[low]` `[reject]` The withdrawn delivery-retry control is disabled with no reason or recovery copy — the state is reflected by the control itself and the fix adds a branch and new strings for a transient prerequisite lapse.
  - `[low]` `[reject]` The target-replacement invalidation branch in `OnGrantUserIdInput` is unreachable in a browser because the field is `Disabled` while a preview is open — confirmed dead, but it is defence in depth behind a disabled control; both proposed fixes (dropping the branch, or re-enabling the field mid-preview) are worse.
  - `[low]` `[reject]` `GlobalAdministratorActionEvidence.SupportsTrackedGrantDispatch` lost its `= SupportsDispatch` initializer and now defaults to `false` — a deliberate fail-closed default for a capability flag, matching the intent's "require lifecycle support"; making it `required` adds public surface.
  - `[low]` `[reject]` No progress state while the bounded preview walk runs — the target field disabling is the only feedback; adding `aria-busy`/spinner plumbing is new surface for a bounded 50-page read.
  - `[low]` `[reject]` Mixed test naming (`PascalCase` new tests beside retained snake_case neighbours) — cosmetic; a blanket rename churns unrelated tests.
  - `[low]` `[reject]` Test hygiene: redundant `JSInterop.Mode` assignment, an undisposed `ServiceProvider`, a weak `ReferenceEquals(...).ShouldBeFalse()` — none changes what the tests prove.
  - `[medium]` `[patch]` The grant path's `ex.Retryable == true` ambiguity arm had no negative counterpart, so widening it to `!= false` — which would report a definite rejection as unresolved delivery and offer a same-id redispatch — kept every grant test green. Added `NonRetryableGrantRejectionsStayTerminal` (4 cases).
  - `[low]` `[reject]` French repair scoped to Story 4.2 keys leaves adjacent `Remove.*` strings undiacritized — real, but the removal copy belongs to Story 4.3 and is out of this story's resource scope.
  - `[medium]` `[patch]` The `!preview.IsComplete` continuation wrote `_completeSnapshot` and the grant lifecycle with no ownership check, while the sibling complete branch had one — a cancelled or superseded bounded walk could replace current evidence with an abandoned attempt's outcome. It now runs the same `CanApplyGrantPreviewOperation` guard.
  - `[medium]` `[patch]` `FocusSafelyAsync` caught only `JSDisconnectedException`/`ObjectDisposedException`; a stale `ElementReference` (authorization collapse, a closed preview, any render that drops the target) makes the focus interop throw `JSException`, which escapes `OnAfterRenderAsync` and tears down the circuit. Added the `JSException` arm.
  - `[low]` `[reject]` The redispatch renderer callback returns silently when prerequisites lapse between click and callback — the control itself then reflects the lapse; adding a message for the click-window race is a new branch for a transient state.
  - `[false]` `[reject]` "`CanRetryAmbiguousGrantDelivery` must also require `MatchesGrantPreviewAttempt`" — the handler already calls it at the redispatch site before dispatching; the button predicate guards capability, the handler guards exact attempt identity.
  - `[false]` `[reject]` `EventsStored`/`EventsPublished` with an explicit `EventCount` of 0 accepted as positive evidence (grant snapshot) — `AggregateActor.WriteAdvisoryStatusAsync(command, CommandStatus.EventsStored/EventsPublished)` is called with `eventCount` defaulted to null at every call site, so the platform never emits an explicit 0 for those statuses.
  - `[false]` `[reject]` Same claim on the correction snapshot — refuted by the same platform call sites.
  - `[false]` `[reject]` A definitive 4xx with `Retryable == true` misreported as ambiguous — `Retryable` is the platform's own assertion that the outcome is unresolved; honouring it is the contract, not a defect.
  - `[false]` `[reject]` `HasCompleteGrantLocalization` hard-codes invariant + `fr` and would miss a third culture — no `RequestLocalizationOptions`/`SupportedUICultures` configuration exists in this repository, so no third culture is served; adding one requires a satellite resx and localization config together.
  - `[false]` `[reject]` The correction panel's `messageId` is hoisted above the renderer callback and could mismatch a changed command type — every `_snapshot` reassignment that changes the intent is preceded by `InvalidateOperation()`, and the callback rechecks `CanApplyOperation(generation)` before using it.
  - `[low]` `[patch]` The delivery-retry path re-read `_admissionLease!` on the continuation after a renderer callback had validated it; a concurrent callback clearing the lease made the null-forgiving deref throw out of the event handler. The lease is now captured inside the callback that validated it.
  - `[false]` `[reject]` Circuit disposal during the retry `InvokeAsync` orphans the dispatched lease — `Dispose()` calls `RetainReconciliationForReplacement()`, and the delivery outcome was already advanced onto the lease before the callback, so the lease is retained, not orphaned.
  - `[false]` `[reject]` Same claim for the projection-confirmation `InvokeAsync` — refuted by the same `Dispose()` retention.
  - `[medium]` `[defer]` `TenantsWorkspace.razor` dropped the hand-rolled `hidden`/`aria-hidden` panel wrappers for `FcPageTabs`/`FcPageTab` — already carried verbatim by the existing deferred entry at `TenantListSurfaceTests.cs:426`; not duplicated.
  - `[medium]` `[defer]` `TenantListSurfaceTests` replaced eight inactive-panel `hidden`/`aria-hidden` assertions with `role="tabpanel"` checks true of both panels — same existing deferred entry.
  - `[false]` `[reject]` Spec claims `EventsStored`/`EventsPublished` are "qualified like `Completed`" while the code tests `EventCount` only for `Completed` — a spec-wording mismatch whose only fix edits this build's spec.
  - `[low]` `[reject]` "The whole background subtree is inert" overstates it: shell header, nav rail and hamburger sit outside this page and stay focusable — but the focus-sentinel pair contains the Tab cycle inside the dialog in both directions, and abandoning a pre-dispatch preview by clicking shell navigation releases the undispatched lease on dispose.
  - `[medium]` `[defer]` The four sibling tracked-dispatch gateway methods (`SetTenantConfigurationTrackedAsync`, `RemoveTenantConfigurationTrackedAsync`, `EnableTenantTrackedAsync`, `DisableTenantTrackedAsync`) still use the status-code-only test and `catch (TaskCanceledException)` — pre-existing behaviour this story did not touch; the same transport condition now maps to ambiguity on grant and terminally on those, and a bare `OperationCanceledException` escapes them entirely.
  - `[medium]` `[defer]` `FocusSafelyAsync`'s disconnect guard has no runtime test — the added `JSException` arm is likewise unpinned; a bUnit `SetException` on the focus invocation would settle it.
  - `[medium]` `[defer]` The correction panel's restore-only tracked-dispatch requirement is never exercised false — its stub gateway pins `SupportsTrackedGlobalAdministratorDispatch = true` and no test assigns it, so deleting the guard stays green.
  - `[medium]` `[defer]` The AppHost Dapr secret-store topology is verified only by source-text assertions — already carried by the existing deferred entry at `src/Hexalith.Tenants.AppHost/Program.cs:117`; not duplicated.
  - `[low]` `[reject]` `TenantAuditPageTests` now truncates `.razor` inputs at `@code` before the forbidden-word scan — a source-text scan was never verification; visible copy is still covered by markup, resource, and support-safety tests.
  - `[low]` `[reject]` `HasCompleteGrantLocalization` swaps `CultureInfo.CurrentUICulture` during a render-path property read and memoizes in a non-volatile `bool?` — the swap spans no await and is restored in `finally`; the memo is idempotent, so a concurrent first read repeats work rather than corrupting it.
  - `[medium]` `[defer]` Retention is proven only across a bUnit re-render sharing one service provider — nothing exercises circuit loss, reconnect, process restart, or a second node, so the caller-owned ULID and preview baseline live in process memory.
  - `[medium]` `[defer]` Focus, inertness, and viewport are proven at the interop layer — already carried by the existing deferred entry at `TenantListSurfaceTests.cs:420` / the interop-layer entry; not duplicated.
  - `[medium]` `[defer]` The EventStore command-status contract this story's confirmation rule depends on is asserted only against hand-written stubs — already carried by the existing deferred entry; not duplicated.
  - `[low]` `[reject]` The change expands beyond the grant page into the shared correction/revoke snapshot — deliberate and required: the same fixed aggregate and the same "never confirm from presence alone" rule apply at both consumers, and the revoke path's domain behaviour is unchanged.
  - `[low]` `[reject]` Grant availability now depends on globalization/hosting configuration through the readiness kill switch — that is the intent's own "require all preview facts" rule, and it fails closed.
  - `[high]` `[defer]` `dotnet build Hexalith.Tenants.slnx --configuration Release` FAILS: `src/Hexalith.Tenants.AppHost/Program.cs(132,5): error CS1503` — commit `7453ba5b` in this range moved the `AddHexalithMemoriesSearchIndexServer` secret-store argument from `string` to `IResourceBuilder<IDaprComponentResource>` to match the `Hexalith.Memories` submodule, but the newest published `Hexalith.Memories.Aspire` (2.22.1, the version `Hexalith.Builds` pins) still declares the `string` overload. Unfixable inside this repository; see the blocking condition below.
  - `[medium]` `[defer]` `Hexalith.Builds@9d77ed7` pins `HexalithEventStoreVersion = 999.1.20-proof.fa2d1c9910f8`, which is not published, so every project fails `NU1102` on a default restore — all verification in this pass required `-p:HexalithEventStoreVersion=3.100.1`. Deliberate, temporary, owned by the Builds/EventStore release owner.
  - `[false]` `[reject]` "The reviewed diff mixes unrelated feature changes" — restated from pass 3 and resolved the same way, except that the gitlinks are now declared rather than disowned.
  - `[false]` `[reject]` "Review pass metadata contradicts the sprint board" — the board is orchestrator bookkeeping and is explicitly not verification evidence for this story.
  - `[false]` `[reject]` `review_loop_iteration` "regressed" from 1 to 0 — the reset is the documented routing for re-entering review on a `done` spec, not a lost iteration.
  - `[maybe-false]` `[defer]` Whether a Fluent v5 `fluent-button` inside the newly fixed-position dialog keeps its focus ring against the elevated surface — settled by an authenticated browser trace at the same time as the existing focus-containment entry.
  - `[maybe-false]` `[defer]` Whether the fixed-position dialog clips its own content on a short viewport below the `max-block-size: 90vh` bound — `overflow-y: auto` is set, but only a real layout pass settles it.
  - `[maybe-false]` `[defer]` Whether any consumer outside this repository constructs `GlobalAdministratorActionEvidence` and now silently loses grant availability to the fail-closed `SupportsTrackedGrantDispatch` default — settled by a cross-repository reference search.

### 2026-09-01 — Review pass
- verdicts: 36 findings — high 0, medium 9, low 12, false 14, maybe-false 1
- findings:
  - `[false]` `[reject]` The sprint row says done while the spec is under review — the invocation explicitly defines that row as independent orchestrator bookkeeping and not verification evidence; the spec remains the implementation authority.
  - `[false]` `[reject]` The sprint-row edit violates this story's read-only boundary — it was already staged by the orchestrator before this run, and this run neither wrote, staged, nor reverted it.
  - `[false]` `[reject]` The story is being recorded complete despite failed gates — the spec is finalized as `awaiting-operator`, not `done`; the orchestrator row does not change that result.
  - `[low]` `[reject]` DW-333 and DW-335 preserve contradictory ownership descriptions for the tab migration — the inconsistency is real, but repairing it edits this build's historical spec/ledger account and does not change product behavior.
  - `[low]` `[reject]` The tracked-dispatch inconsistency appears twice with different settlement language — both entries describe historical review evidence; consolidation would only edit this build's spec.
  - `[false]` `[reject]` Every spec-level deferred item must also appear in deferred-work.md — no repository rule or consumer establishes one-to-one mirroring, and the workflow's authoritative field is the spec's single `deferred` list.
  - `[low]` `[reject]` The preceding 2026-09-01 review header miscounts its rows — recounting finds 55 formatted rows with 17 medium and 14 false, not the stated totals; the only correction edits this build's historical review record.
  - `[medium]` `[reject]` The event-evidence safeguard can be read as giving EventsStored and EventsPublished Completed's EventCount rule — current code and passing snapshot tests implement the platform contract, while changing the ambiguous wording edits this build's spec.
  - `[medium]` `[reject]` The verification list does not explicitly name a source-reference lane — the lane was run separately, but adding it to the list edits this build's spec.
  - `[medium]` `[defer]` Publishing Memories alone cannot make the Release commands runnable while the EventStore proof package set is unpublished — default restore reproduces NU1102 and the EventStore release owner must publish the pinned package set.
  - `[false]` `[reject]` The current pass has no Auto Run Result — that section is intentionally written during finalization and is added below after review and verification finish.
  - `[false]` `[reject]` The File List must include current metadata files and unrelated specs changed in older commits — the section is explicitly scoped to source and tests owned by the broader story, not every artifact in this resumed iteration.
  - `[low]` `[reject]` The test glob is too broad for traceability — explicit verification classes and totals provide the operational trace, and narrowing the prose list edits this build's spec only.
  - `[low]` `[reject]` GlobalAdministratorGrantPreview.cs is listed although its final blob matches the original baseline — the historical file account is imprecise, but correcting it edits this build's spec and has no runtime effect.
  - `[low]` `[reject]` The File List overstates the FrontComposer pointer as necessary for FcPageTabs — the older pointer contains identical tab blobs, but correcting the rationale edits this build's historical spec.
  - `[low]` `[reject]` The story-specific need for all five historical gitlink moves is unsupported — the concern is valid commit hygiene, but fixing it would rewrite existing commits or edit this build's spec, and the current intent forbids rolling back the canonical dependency path.
  - `[low]` `[reject]` Gitlink revisions are recorded with seven-character abbreviations rather than full identifiers — the validator resolves them successfully; the only fix edits this build's spec.
  - `[low]` `[reject]` AppHost ownership is described as both concurrent and normative — the prose inconsistency is real, but its correction is a spec edit and the operator handoff below states the actionable dependency ownership.
  - `[false]` `[reject]` The referenced FrontComposer commit's historical subject must be remediated in this story — current commit policy governs messages this assistant creates or uses, not rewriting a dependency's existing history.
  - `[low]` `[reject]` Bare git diff --check does not inspect staged changes — both `git diff --check` and `git diff --cached --check` were run; changing the command list edits this build's spec.
  - `[false]` `[reject]` Retrospective tooling must cross-check the orchestrator row against spec status — the invocation explicitly makes the row independent bookkeeping and excludes it as story verification evidence.
  - `[false]` `[reject]` The resumed diff does not implement the runtime grant feature — the runtime implementation is already present in commits before this iteration's captured baseline and its focused behavior was re-executed.
  - `[medium]` `[defer]` The resumed diff documents but cannot perform the package publication and Builds pin update — those operations require external release credentials and a published package version, so they are recorded under `operator_actions`.
  - `[false]` `[reject]` No executable tests accompany the resumed metadata diff — the executable tests already exist at the captured baseline and this pass added six reviewed code/test files plus reran 395 focused and 2,734 full UI tests.
  - `[false]` `[reject]` The spec and sprint row represent contradictory story state — they serve different authorities by explicit invocation contract, and only the spec status reports this run's verification result.
  - `[false]` `[reject]` This run crossed the sprint-status read-only boundary — the orchestrator-owned staged edit predated the run and was preserved byte-for-byte.
  - `[low]` `[reject]` The two tab-migration ledger entries can cause duplicate follow-up work — their distinct origins preserve review history; consolidation is ledger/spec housekeeping with no everyday product impact.
  - `[false]` `[reject]` An unmet package prerequisite makes the sprint row itself a defect — the row is orchestrator bookkeeping by contract; the unmet gate is represented by `awaiting-operator` and operator actions.
  - `[medium]` `[patch]` Preview readiness covered only value strings and allowed missing labels, chrome, or failure copy — the production set now contains all 26 rendered keys, both readiness paths share it, and tests derive coverage from that set.
  - `[medium]` `[patch]` A superseded incomplete preview could mutate state from its queued renderer callback — the callback now rechecks generation, preview generation, and exact target ownership before writing.
  - `[false]` `[reject]` Withdrawn preview readiness can redispatch a same-id command — DispatchGrantAsync already rechecked the final arm and prevented that outcome; the control path was additionally aligned to the shared capability predicate for spec consistency.
  - `[medium]` `[patch]` A detached focus target can raise JSException out of post-render focus — FocusSafelyAsync now absorbs that stale-element case and a focused test exercises it.
  - `[medium]` `[patch]` The aria-modal preview was still an in-flow card — scoped CSS now fixes, centers, elevates, bounds, and scrolls the dialog with a forced-colors border, and focused tests pin the rule.
  - `[medium]` `[patch]` Definite non-retryable grant rejections lacked negative coverage — four HTTP cases now prove terminal failed/rejected states and preserved caller message identity without ambiguity.
  - `[maybe-false]` `[defer]` Component focus assertions may not match a real browser's active-element behavior — an authenticated browser trace of open, Tab, Shift+Tab, Escape, and launcher restoration is required to settle it.
  - `[low]` `[reject]` The shell outside the page-local inert subtree remains mouse-operable behind the modal — focus sentinels contain keyboard traversal and navigation safely abandons only an undispatched lease; a shell restructure is disproportionate to this residual behavior.

### 2026-09-21 — Review pass
- verdicts: 23 findings — high 5, medium 9, low 1, false 5, maybe-false 3
- findings:
  - `[high]` `[bad_spec]` Production fixed-scope reads cannot supply the projection-backed freshness evidence the grant gate requires — `TenantQueryResult.FromPayload` discards `IReadModelFreshness`, thresholds, and time, then EventStore's handler router stamps the result `HandlerComputed`, while `TenantQueryGateway.ResolveFreshness` accepts only `ProjectionBacked`; the non-frozen Code Map omitted this end-to-end provenance boundary.
  - `[high]` `[bad_spec]` The integration lane fabricates `ProjectionBacked`/`Current` metadata instead of exercising handler-to-router-to-REST provenance — this conceals the same production grant-availability failure and must be replaced by an end-to-end metadata test during re-derivation.
  - `[high]` `[patch]` The grant preview collapses leading, trailing, and repeated spaces in the literal target — the raw `<dd>` lacks the removal surface's literal-identity rendering, so an operator can approve a visually different platform-wide identity.
  - `[high]` `[patch]` Bidirectional format characters can reorder the literal target and surrounding confirmation text — the identity is intentionally preserved, so the preview must bidi-isolate rather than normalize or reject it.
  - `[medium]` `[bad_spec]` Grant rejection and lifecycle status copy can bypass EN/FR resources — `TenantCommandGateway` returns hard-coded English safe messages and `ResolveSafeMessage` passes them through, contradicting the lifecycle-wide whole-string requirement; the non-frozen implementation map omitted status localization.
  - `[medium]` `[bad_spec]` Literal global-administrator identity validation is duplicated across server validators, UI previews, the page, loader, evaluator, and gateway — the grant gateway already depends on the removal preview's helper, so a future removal-policy change can silently alter grant dispatch; re-derivation needs one shared policy boundary.
  - `[false]` `[reject]` A retained ambiguous attempt can lock the aggregate indefinitely — terminal-only release and same-ID recovery are deliberate safety invariants in the approved intent; releasing unresolved privileged work would permit a second command identity.
  - `[maybe-false]` `[defer]` carried: browser focus containment, restoration, inertness, focus-ring visibility, and short-viewport scrolling remain unproven — the existing operator action already names the authenticated browser trace required to settle them.
  - `[false]` `[reject]` carried: this run edited the orchestrator-owned sprint file — the sprint change is committed historical state in the baseline-to-HEAD range; the current run has not modified it.
  - `[medium]` `[defer]` The committed sprint file truncates Story 4.3's canonical key to `hard-sto` — status lookup for that separate story can no longer match its specification filename, but Story 4.2 must not write the orchestrator-owned file.
  - `[medium]` `[defer]` The committed sprint rewrite removed Story 1.12 and four completed conformance rows — this is real tracking-history loss in separate orchestrator work and is not repairable inside the Story 4.2 implementation boundary.
  - `[false]` `[reject]` carried: `status: in-review` and sprint `in-progress` are not contradictory authorities — the spec reports this review run while sprint status remains independent orchestrator bookkeeping.
  - `[medium]` `[reject]` The frontmatter's package-release blockers are stale after later dependency publications and a passing Release build — the claim is real, but its only correction edits this build's own historical spec account, which review findings may not patch.
  - `[low]` `[reject]` Three committed review-prompt snapshots add substantial repository bulk — they are historical evidence outside runtime behavior, and deleting committed evidence during this grant build would be disproportionate and destructive.
  - `[false]` `[reject]` carried: the baseline diff improperly mixes unrelated work — the preserved historical baseline spans separately committed mainline work; this run's staged implementation remains isolated and does not own or revert those commits.
  - `[medium]` `[bad_spec]` Ambiguous grant delivery retry lacks a single-flight gate — two overlapping activations can cancel/supersede each other before either dispatch arms, despite the approved no-double-dispatch requirement; re-derivation must serialize the retained same-ID retry.
  - `[false]` `[reject]` carried: `EventsStored` or `EventsPublished` with explicit zero event count can confirm — EventStore emits those advisory statuses without an event count at every production call site, so the alleged zero-valued state is unreachable.
  - `[high]` `[patch]` A SignalR projection notification calls the operator recovery method for a correlation-less ambiguous grant and therefore redispatches the privileged command — notifications must request reconciliation only and never perform same-ID delivery retry.
  - `[medium]` `[defer]` Concrete tracked-removal capability advertisement is unpinned — the gap is verified, but it belongs to the separately tracked Story 4.3 removal intent.
  - `[medium]` `[defer]` Concrete tracked-removal ambiguity mapping is mocked by component stubs — the missing transport theory is verified but belongs to Story 4.3, not this grant intent.
  - `[medium]` `[defer]` carried: workspace-tab tests cannot observe inactive-panel visibility or focusability — an authenticated browser trace in both tab directions remains the settlement evidence.
  - `[maybe-false]` `[defer]` carried: modal focus containment is simulated through interop IDs rather than real browser Tab order and `document.activeElement` — the existing browser acceptance action remains required.
  - `[maybe-false]` `[defer]` Keycloak credential substitution is verified only as template/model text — a Keycloak-enabled Aspire token-and-authorized-request smoke test would settle the separate AppHost risk.

### 2026-09-21 — Review pass 2
- verdicts: 21 findings — high 3, medium 9, low 2, false 6, maybe-false 1
- findings:
  - `[false]` `[reject]` A clean checkout would omit the dirty EventStore source edit — this review is intentionally over an uncommitted staged worktree, the inline reviewer artifact contains the submodule patch, and no root gitlink move is expected before the separately required EventStore commit/publication; the actual deployability gap is recorded independently by the failing package lane.
  - `[high]` `[defer]` The published EventStore package still stamps the handler result as computed — the exact package-reference integration lane fails 27/28 at the missing `global-administrators` projection type, so the EventStore release owner must publish the working-tree router fix and advance the Builds pin before this story can be accepted.
  - `[false]` `[reject]` A domain handler can unilaterally self-promote untrusted data — handler invocation resolves a platform-registered domain service that already owns the query payload, and the explicit nonblank projection type plus projection-backed metadata pair is the trusted producer contract; no client-controlled or unregistered handler path was demonstrated.
  - `[maybe-false]` `[defer]` The in-process handler/router/controller/client test may miss a production Dapr, DI, or serialization failure — the package-mode variant already catches an omitted router implementation and the component contracts are exercised, but only a real registered-domain-service invocation would settle the remaining transport-composition doubt.
  - `[false]` `[reject]` carried: a retained correlation-less ambiguous attempt can lock the aggregate indefinitely — terminal-only lease retention and same-ID recovery remain deliberate safety invariants; releasing or replacing unresolved privileged work would permit a second command identity.
  - `[medium]` `[bad_spec]` A transient viewport or capability lapse irreversibly withdraws same-ID delivery retry — `IsDeliveryRetryWithdrawn` is never cleared even when every live prerequisite returns, so the non-frozen retry design must specify safe re-arming of the retained attempt without releasing the lease or minting an identity.
  - `[medium]` `[bad_spec]` The withdrawal recovery tells the operator to start a new reviewed attempt although the dispatched aggregate lease correctly prevents one — recovery copy and behavior must instead restore only the retained same-ID retry after its live prerequisites recover, while preserving the original diagnostic.
  - `[false]` `[reject]` Omitting HTML `maxlength` permits an unsafe unbounded command identity — the full literal is bounded before preview/dispatch, and the deliberate no-`maxlength` test prevents browser truncation from silently converting an invalid pasted identity into a valid different one.
  - `[high]` `[bad_spec]` `<bdi>` does not neutralize bidi override characters embedded inside the exact identity — the non-frozen rendering plan must visibly escape Unicode format controls while retaining and dispatching the original ordinal value so the approval target cannot be visually reordered.
  - `[false]` `[reject]` Generic localized rejection copy violates the approved distinction — the intent requires duplicate and permission outcomes to remain safe localized rejections, which the stable resource key and retained safe rejection code satisfy; it does not require exposing distinct codes or recovery copy to the operator.
  - `[medium]` `[bad_spec]` Restore-access correction status drops the grant gateway's new `SafeMessageKey` while `SafeMessage` is null — pending, rejected, publication-failed, timeout, and mismatch states can render without localized explanation, so every grant consumer must propagate the stable lifecycle keys.
  - `[low]` `[reject]` carried: the shell outside the page-local inert subtree remains operable behind the modal — focus sentinels contain keyboard traversal and an authenticated browser trace remains the proportional evidence for the residual shell behavior.
  - `[low]` `[reject]` carried: three committed reviewer prompt snapshots substantially enlarge future diffs — they are historical evidence outside runtime behavior, and deleting committed artifacts inside this story would be disproportionate and destructive.
  - `[medium]` `[defer]` carried: the committed sprint file truncates Story 4.3's canonical key to `hard-sto` — the separate orchestrator-owned tracking corruption remains outside Story 4.2 and this run must not write the sprint file.
  - `[medium]` `[defer]` Restore-access correction lacks a tracked-grant-capability-disabled test — the capability gate is in the separately introduced correction surface and no test sets its stub false; a focused correction-panel test must prove it fails closed without traffic or retained ownership.
  - `[medium]` `[bad_spec]` Key-bearing grant results are not verified across gateway, snapshot, and rendered page boundaries — the spec already requires gateway/component coverage, so re-derivation must add key-only submission and status cases that prove the exact localized message renders.
  - `[medium]` `[bad_spec]` Correlated SignalR grant notification has no positive test — re-derivation must prove it performs one status lookup with the retained fixed identity and never submits another command, alongside the existing correlation-less negative case.
  - `[high]` `[bad_spec]` Stale handler metadata is not exercised through the real router-to-UI chain — a stale-to-current normalization regression could open the mutation gate, so the real-handler integration must also prove stale lifecycle remains incomplete at the UI.
  - `[medium]` `[defer]` Audit payload validation accepts `HasMore` with a blank cursor while the Next control enables from `HasMore` and its handler silently returns on the blank cursor — this pre-existing audit navigation defect belongs to the audit surface, not the fixed-scope grant intent.
  - `[false]` `[reject]` carried: projection advancement accepts any unequal opaque version — the approved design deliberately uses opaque-token inequality only with exact-command positive event evidence, complete current population evidence, and exact target presence.
  - `[medium]` `[bad_spec]` Restore-access correction discards localized grant status keys — this duplicates the verified correction-consumer boundary defect and shares its required lifecycle-key propagation fix.

### 2026-09-21 — Review pass 3
- verdicts: 25 findings — high 8, medium 10, low 1, false 4, maybe-false 2
- findings:
  - `[medium]` `[defer]` Project-root resolver behavior is verified only through mocked wrappers — the gap is real in separately committed BMAD tooling, not in Story 4.2; an isolated executable resolver test would settle override precedence and warnings.
  - `[medium]` `[defer]` carried: restore-access correction lacks a tracked-grant-capability-disabled test — the separately introduced correction surface still has no component case that sets the stub capability false; this remains the previously recorded correction-surface gap.
  - `[medium]` `[patch]` Restore-correction localization is pinned only for pending status — rejected, publish-failed, timed-out, and unknown key-to-rendered-copy branches need a compact snapshot/component theory.
  - `[medium]` `[patch]` Unicode-format escaping is helper-tested but not verified at its page and correction consumers — component tests must prove safe rendered text while copy and dispatch preserve the original identity.
  - `[maybe-false]` `[defer]` carried: modal focus trapping is mocked at the interop boundary — the existing authenticated browser action remains necessary to observe `document.activeElement`, Tab wrapping, and restoration.
  - `[medium]` `[defer]` carried: inactive workspace panels lack browser visibility and focusability proof — the existing workspace migration deferral and authenticated browser trace remain the settlement evidence.
  - `[medium]` `[defer]` carried: the Memories secret-store graph is not started in the package-mode test — the existing Aspire health deferral already records the required deployed startup evidence.
  - `[high]` `[bad_spec]` The visible format-control token is not injective — an actual U+202E and literal text `U+202E` render identically, so the presentation-only encoding must escape its own sentinel and remain one-to-one.
  - `[high]` `[bad_spec]` The edge-case trace independently confirms that literal escape-looking text collides with a real format character — destructive target selection can therefore become ambiguous.
  - `[high]` `[bad_spec]` The shared policy accepts lone UTF-16 surrogates that `System.Text.Json` serializes as U+FFFD — command dispatch can target a different identity, so ill-formed scalar sequences must fail before preview or serialization.
  - `[low]` `[reject]` carried: three committed review-prompt snapshots substantially enlarge the baseline diff — they are historical evidence, and deleting committed artifacts in this build would be disproportionate and destructive.
  - `[false]` `[reject]` carried: the baseline diff mixes hundreds of unrelated committed files — the captured baseline spans concurrent mainline work while this run's staged Story 4.2 implementation remains isolated.
  - `[high]` `[bad_spec]` The blind review independently reproduces the non-injective `U+XXXX` display — a literal token and an escaped format scalar cannot be distinguished by the operator.
  - `[high]` `[bad_spec]` The blind review independently reproduces lone-surrogate identity mutation through JSON — the validator must reject ill-formed UTF-16 before any tracked command is constructed.
  - `[false]` `[reject]` The 256 limit counts .NET UTF-16 code units — that is the established validator boundary this re-derivation was required to preserve, and the contract never promised a scalar- or grapheme-count limit.
  - `[medium]` `[defer]` Historical malformed administrator events can make complete projection evidence unavailable — new commands are validated and the loader fails closed; changing projection/domain migration behavior is explicitly excluded from this intent.
  - `[medium]` `[bad_spec]` Tenant-audit context renders `TargetUserId` without the new display-safety boundary — a bidi-formatted grant target can spoof the informational audit context, so every visible global-administrator identity consumer must share the injective formatter.
  - `[false]` `[reject]` carried: grant acceptance must impose a canonical parser on correlation ids — correlations are intentionally opaque, while message ULIDs and exact returned identity are the canonical tracked fields.
  - `[false]` `[reject]` carried: generic localized rejection copy discards the required duplicate/permission distinction — the stable key plus retained `RejectionCode` preserves safe distinction without exposing backend prose, matching the approved intent.
  - `[high]` `[bad_spec]` Grant same-ID retry is single-flight only inside one component instance — disposal while the first POST is pending lets the replacement adopt and concurrently redispatch; the shared lease must own initial and retry delivery tokens and publish durable completion.
  - `[medium]` `[defer]` carried: correction-panel unsupported tracked-grant capability remains untested — this is the previously recorded separately introduced correction-surface gap.
  - `[maybe-false]` `[defer]` carried: real-browser focus containment, restoration, and short-viewport scrolling remain unproven — the existing operator action names the required trace.
  - `[high]` `[defer]` carried: the EventStore router fix is only a dirty submodule source change and is not yet consumable by the package-reference lane — the combined review artifact is not an apply-ready root patch, and the release owner must publish and advance the dependency pin.
  - `[medium]` `[defer]` Restore-backed package-boundary validation can accept unintended dependency growth because restore evidence defines both sides of the comparison — this is a separately committed package-governance design; a reviewed allow-list or baseline belongs to that workflow.
  - `[high]` `[defer]` carried: the package-reference generated-controller lane still fails on the unpublished EventStore router fix — source-reference success is not release evidence, and the existing operator action remains required.

### 2026-09-21 — Review pass 4
- verdicts: 28 findings — high 4, medium 18, low 2, false 4, maybe-false 0
- findings:
  - `[medium]` `[patch]` A handler can return projection-backed metadata for a different projection than the nonblank route requested — the router checks only that the result declares some projection type; require an ordinal match whenever the request supplied one and test the mismatch.
  - `[high]` `[patch]` `QueriesController` can replace the handler's read-consistent ETag with a newer current ETag — a payload read at v1 can be labeled and cached as v2; prefer the producer ETag for a projection-backed handler result and pin the race with a controller test.
  - `[medium]` `[patch]` The real-handler integration bypasses EventStore's HTTP controller metadata merge — the Tenants generated REST/client/UI chain is real, but the changed router-to-controller boundary is composed only manually; add source-side controller evidence while retaining the package lane as the publication gate.
  - `[false]` `[reject]` Null freshness fabricates usable projection evidence — the freshness overload deliberately carries projection provenance with `Unknown` lifecycle and no version, every grant evidence gate requires current/versioned evidence, and production handlers supply a concrete read model.
  - `[medium]` `[patch]` The complete grant localization gate omits the required and invalid UserId validation strings — both are rendered locally and a missing resource currently passes readiness; add both keys and missing-resource cases.
  - `[high]` `[patch]` The grant launcher's click handler still lacks its specified first-statement disabled guard — a programmatic/custom-element activation can cancel and supersede an in-flight privileged attempt; recheck `IsGrantSubmitDisabled` before touching validation or mutation state.
  - `[medium]` `[patch]` Grant localization overrides accept whitespace and resolve reason/recovery independently — a partial or blank override can re-enable submission or pair unrelated copy; consume the pair only when both keys are nonblank.
  - `[medium]` `[patch]` Ambiguous retry enablement omits exact snapshot/preview/lease/message/generation identity — the control can be enabled while both handlers silently refuse the attempt; include the exact-attempt predicate and surface a stale retry activation.
  - `[medium]` `[patch]` An unknown future `CommandStatus` leaves the grant snapshot unchanged — fail closed to the existing unsupported-submission diagnostic and cover an undefined enum value.
  - `[false]` `[reject]` Generic submission-rejection copy discards the rejection distinction — the support-safe `RejectionCode` is retained, while operator copy is intentionally generic and adding code-specific public strings is not required by the approved intent.
  - `[false]` `[reject]` Aggregate-only grant status localization corrupts removal rendering — both removal snapshots map lifecycle outcomes through removal-owned keys and ignore the gateway's grant key; the correction adapter now delegates through that same mapping.
  - `[medium]` `[patch]` Restored audit context collapses significant identity whitespace — the encoded text is placed in a normal-flow paragraph without `white-space: pre-wrap`; add literal-whitespace styling and component evidence.
  - `[medium]` `[patch]` Row and copy accessible names reuse a whitespace-literal visual string — accessible-name whitespace normalization can collapse distinct identities; use a private accessible representation that tokenizes significant whitespace while preserving the original copy value.
  - `[medium]` `[patch]` Accepted default-ignorable non-Format scalars remain visually invisible — U+034F and variation selectors can still collide with an otherwise identical identity; token-encode default-ignorable scalars and test literal escape-looking counterparts.
  - `[low]` `[patch]` Withdrawal reason copy is visibly rendered twice and nested in two live regions — remove the duplicate live-region child while retaining the visible reason, recovery, and lifecycle announcement.
  - `[false]` `[reject]` A terminal delivery completion with no current owner must remain adoptable — terminal-only release is an explicit invariant; releasing the aggregate lock is correct once rejection/failure is durable, and retaining it for a hypothetical later renderer would strand admission.
  - `[high]` `[patch]` Grant redispatch single-flight is not verified across renderer disposal/replacement — hold a retry POST open, replace the renderer, and prove the durable lease token prevents a second POST until completion.
  - `[medium]` `[patch]` Restore-access `UnableToVerify` reconciliation is not pinned as retainable — assert `ToReconciliation()` and component disposal/adoption preserve the same attempt so the fixed aggregate lock cannot be stranded.
  - `[medium]` `[patch]` Fixed-aggregate gateway resource-key mapping is unverified for pending, unknown, mismatch, rejected, publish-failed, and timed-out statuses — add a gateway theory asserting each exact key.
  - `[medium]` `[patch]` The grant page lacks a malformed UTF-16 boundary case — add lone high/low surrogate component rows proving local rejection, no preview composition, and no dispatch.
  - `[medium]` `[patch]` The validation-resource omission is independently confirmed by the verification layer — extend the complete grant-surface manifest and fail-closed missing-resource theory rather than relying on the hard-coded count alone.
  - `[medium]` `[patch]` Audit whitespace preservation has no rendered-style evidence — add the `pre-wrap` rule and a component/style assertion for repeated, leading, and trailing spaces.
  - `[medium]` `[patch]` Unchecked audit/correction identities can carry raw control scalars through the display helper — token-encode Unicode controls as well as formats so historical or URL-provided data cannot alter layout.
  - `[high]` `[patch]` A second retry activation can call `BeginGrantMutation` and cancel the first before the shared delivery token refuses the duplicate — gate the handler on the lease's in-flight token before beginning a new mutation and add concurrent activation coverage.
  - `[medium]` `[patch]` Adopted `Degraded` and nonambiguous `UnableToVerify` grant states are reconstructed as audit-pending/polite — map retained lifecycle to its original audit, focus, and live-region severity and test renderer adoption.
  - `[medium]` `[patch]` Audit context whitespace collapse is independently confirmed by the edge-case layer — the current CSS has only `overflow-wrap`, so distinct spaced identities render alike.
  - `[medium]` `[patch]` The missing grant validation resources are independently confirmed by the edge-case layer — the 87-key claim excludes two live validation branches.
  - `[low]` `[patch]` `ApplyGrantSubmissionExceptionAsync` is orphaned after completion handling moved to the durable token path — delete the dead private method to prevent its obsolete state rules from drifting.

## Design Notes

The preview baseline is the complete fixed-scope projection version captured before dispatch. A different post-command version is necessary but not sufficient: confirmation also requires positive event evidence from status whose message, correlation, and fixed aggregate identity match the retained attempt, plus exact ordinal target presence in a new complete current walk. This prevents pre-existing, concurrent, page-scoped, or unrelated projection observations from becoming success.

## Verification

**External prerequisite before re-arm:** Publish a Hexalith.Memories.Aspire version containing the Story 29.2 `IResourceBuilder<IDaprComponentResource>` secret-store signature and update `HexalithMemoriesVersion` in Hexalith.Builds to that version. Only then run the unchanged source- and package-reference verification lanes below.

**Commands:**
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release -warnaserror -m:1 -nr:false` -- expected: warning-clean UI test assembly.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -class Hexalith.Tenants.UI.Tests.State.GlobalAdministratorGrantCommandSnapshotTests -class Hexalith.Tenants.UI.Tests.State.TenantAggregateCommandAdmissionGateTests -class Hexalith.Tenants.UI.Tests.Services.Gateways.TenantCommandGatewayTests -class Hexalith.Tenants.UI.Tests.Services.Gateways.TenantsBffCompositionTests -class Hexalith.Tenants.UI.Tests.Components.GlobalAdministratorsPageTests` -- expected: focused grant preview, lifecycle, gateway, lock, and component matrix passes with no skipped/not-run tests.
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-build --no-restore` -- expected: maintained full UI test lane passes.
- `dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false` -- expected: full solution builds with zero warnings and errors.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md` -- expected: every moved root gitlink is declared or no gitlink moved.
- `git diff --check` -- expected: no whitespace errors.

## File List

Source and tests changed by this story:

- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor` -- grant preview handoff, generation-bound cancellable composition, focus containment, tracked dispatch, ambiguous-delivery retry, qualified projection confirmation.
- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor.css` -- preview/focus/responsive/forced-colors hooks and the dialog's modal presentation.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor` -- concurrent tab migration to `FcPageTabs`/`FcPageTab`.
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor` -- restore-access correction adopts the tracked-grant lifecycle.
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs` -- fail-closed grant-preview composition and the localized-fact readiness gate.
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs` -- caller-owned tracked grant dispatch and retryable/cancellation ambiguity mapping.
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorGrantPreview.cs` -- the immutable BFF-owned preview.
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorGrantCommandSnapshot.cs` -- previewed intent, baseline, one message id, event evidence, qualified confirmation.
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorReconciliationState.cs` -- retained support-safe message and recovery keys.
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorActionEvidence.cs` -- explicit tracked-dispatch capability.
- `src/Hexalith.Tenants.UI/State/TenantAudit/GlobalAdministratorCorrectionSnapshot.cs` -- fixed-identity tracking, ambiguity split, event-evidence qualification.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`, `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx` -- parity-matched whole strings.
- `src/Hexalith.Tenants.AppHost/Program.cs` -- concurrent Memories secret-store topology change (see the high-severity deferred entry; this is the file that fails the solution build).
- `tests/Hexalith.Tenants.UI.Tests/**` -- component, state, gateway, composition, workspace and audit coverage for the above.

Root submodule pointers this story's commit `8da765ad` moved. They were undeclared until this review pass; they are declared here rather than reverted because the change set depends on them -- `TenantsWorkspace.razor` uses `FcPageTabs`, which only exists in the newer FrontComposer, and the EventStore/Commons/PolymorphicSerializations pointers moved with the repository-wide .NET SDK 10.0.400 rebuild:

- `references/Hexalith.Builds`
  - `references/Hexalith.Builds` 12b6951 -> 9d77ed7 -- "fix(deps): pin EventStore to story-28.1 SDK-10.0.400 rebuild". Carries the unpublished EventStore proof pin recorded as a deferred entry.
- `references/Hexalith.Commons`
  - `references/Hexalith.Commons` feab4ef -> 372d715 -- "build(deps): bump .NET SDK to 10.0.400 and Hexalith.Builds submodule".
- `references/Hexalith.EventStore`
  - `references/Hexalith.EventStore` 1194dfe -> e38c125 -- "build(deps): bump Hexalith.Builds submodule to 88024c8".
- `references/Hexalith.FrontComposer`
  - `references/Hexalith.FrontComposer` 9d7710a -> c6fe14c -- "chore: Update Hexalith.EventStore subproject reference". Required by the `FcPageTabs`/`FcPageTab` migration.
- `references/Hexalith.PolymorphicSerializations`
  - `references/Hexalith.PolymorphicSerializations` 65fc336 -> 8aeed1d -- "build(deps): bump .NET SDK to 10.0.400 and Hexalith.Builds submodule".

## Auto Run Result

Status: done

### Summary

Completed every repository-owned part of Story 4.2 and repaired all 24 actionable findings from the final independent review. The implementation now preserves projection provenance and producer ETags, validates global-administrator identities before serialization, renders identities injectively across visual and accessible contexts, requires all 89 grant-surface localization keys, and keeps same-attempt delivery retries single-flight across renderer replacement. The remaining package publication, source-reference output alignment, and authenticated-browser checks are recorded as operator actions rather than claimed as passing evidence.

### Files Changed

- EventStore query routing/controller -- preserves projection provenance only for the requested projection and keeps the producer's read-consistent ETag.
- Tenants contracts, validation, query results, and projection loading -- reject malformed identities before serialization and preserve complete provenance metadata.
- Global-administrator UI, state, audit/correction surfaces, resources, and gateway -- implement the qualified grant lifecycle, durable same-attempt delivery ownership, injective identity display, and the 89-key fail-closed readiness boundary.
- Integration, server, UI, routing, controller, identity, lifecycle, accessibility, correction, and concurrency tests -- pin the behavior and failure boundaries above.
- This specification -- records implementation tasks, independent review triage, verification, residual risks, operator actions, and terminal status. The detailed source/test inventory remains in the File List above.

`_bmad-output/implementation-artifacts/sprint-status.yaml` was not written, staged, reverted, or committed by this run.

### Review Findings

- Verdicts: 28 findings -- high 4, medium 18, low 2, false 4, maybe-false 0.
- Patches applied: 24 -- including projection-type matching, producer-ETag preservation, source-side controller evidence, the complete 89-key localization gate, first-statement submit protection, atomic localization overrides, exact retry identity, fail-closed future statuses, injective identity display, durable cross-renderer delivery ownership, restored-state reconstruction, and expanded boundary/concurrency coverage.
- Rejected: four -- each contradicted an established contract or existing fail-closed gate; the decisions are recorded in Review pass 4.
- New deferrals: none. Existing browser, publication, package-governance, and cross-surface items remain preserved in frontmatter and the deferred-work ledger.
- Follow-up review recommendation: true -- the final pass required substantial patches; root follow-up inspection corrected the replacement-renderer concurrency test and restored the intended public encoder surface before verification.

### Verification

- UI Release build with `HexalithEventStoreVersion=3.106.0` -- passed with 0 warnings and 0 errors.
- Full UI executable -- 2,953 passed, 0 failed, 0 skipped, 0 not run.
- EventStore query-routing executable -- 19 passed, 0 failed.
- Server/query Release build with `HexalithEventStoreVersion=3.106.0` -- passed with 0 warnings and 0 errors; the focused freshness, result, and submit-validator executable lane passed 85 of 85 tests.
- Focused page lane -- 181 passed; the affected UI/state/gateway/correction lane passed 56; the follow-up and identity lanes passed 15 and 14 respectively.
- Source-reference integration build with `UseHexalithProjectReferences=true` -- passed with 0 warnings and 0 errors. Its four-case runtime lane is blocked before assertions during MVC discovery by `ReflectionTypeLoadException: Could not load Hexalith.Tenants.Contracts, Version=5.7.0.0`, so it is not claimed as passing.
- Package-reference real-handler integration -- 4 executed: 1 passed and 3 expected projection-backed cases failed because the published EventStore package still returns `HandlerComputed`; this is the recorded publication gate.
- EventStore controller-test project -- blocked by the existing test dependency graph (`ParallelizationAttribute`/`ParallelMode` unavailable and JwtBearer 10.0.12 requiring a newer framework than 10.0.9). The changed EventStore source project builds, the routing executable passes, and controller coverage is present, but this blocked lane is not claimed as passing.
- `git diff --check` and `git diff --cached --check` -- passed after finalization.

### Residual Risks

- The package-reference integration gate cannot pass until the EventStore projection-provenance fix is published and the dependency pin is advanced.
- The source-reference runtime lane requires output-graph alignment for `Hexalith.Tenants.Contracts, Version=5.7.0.0` before its four cases can execute.
- Real-browser focus containment, launcher restoration, inactive-tab visibility, and short-viewport scrolling remain unproven by bUnit interop assertions.
- Aspire health against the published Memories secret-store topology remains unverified.
- The preserved frontmatter `deferred` list records additional lower-severity cross-process retention, sibling transport consistency, EventStore contract, and test-coverage risks with settlement evidence.

### Review Findings — 2026-09-06 grant-core chunk

Independent review of the grant-core file group (`7e88a571..d0c534ff`, 14 files). Correction-path, workspace/AppHost/gitlink, and later mainline commits were out of this pass.

- [x] [Review][Patch] Grant preview opens with Cancel focused — Opening sets `_focusGrantPreviewCancelPending` and tests pin the first focused control to `_grantPreviewCancelElement`. **Decision 2026-09-06: acknowledgement first** (match removal; update the focus assertion). [`src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:2999`]
- [x] [Review][Patch] Localization failure never shows its associated recovery on the grant surface — `ComposeGlobalAdministratorGrantPreviewAsync` can return `Grant.Preview.Unavailable.Localization` / `Recovery.Localization`, but the page maps `IsGlobalAdministratorGrantPreviewReady == false` to `MissingConsequencePreview` and `SubmitGrantAsync` returns before compose whenever `GrantUnavailableReason` is set. **Decision 2026-09-06: show localization copy** when readiness fails on localization. [`src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs:165`]

- [x] [Review][Patch] `FocusSafelyAsync` does not catch `InvalidOperationException`, and cancel/invalidate leave `_focusGrantPreviewCancelPending` set [`src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1385`]
- [x] [Review][Patch] `RequiredGrantFactKeys` omits `Grant.DeliveryRetry` and `Grant.DeliveryRetry.Recovery` that the live retry control renders [`src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs:25`]
- [x] [Review][Patch] Grant modal CSS lacks `box-sizing: border-box` that the sibling remove modal and its style test already pin [`src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor.css:198`]
- [x] [Review][Patch] Grant focus sentinels are not in the visually-hidden sentinel rule that only targets `.global-admins__remove-focus-sentinel` [`src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor.css:115`]
- [x] [Review][Patch] Modal-isolation test never asserts the grant dialog is outside the inert `tenants-global-admins-area` subtree [`tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs:2359`]
- [x] [Review][Patch] Ambiguous grant `SafeRecoveryKey` is not asserted on the grant snapshot or page recovery paragraph [`src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorGrantCommandSnapshot.cs:212`]
- [x] [Review][Patch] Grant launcher and preview-cancel use raw `fluent-button`; tests submit the form and never activate the launcher control [`src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:289`]
- [x] [Review][Patch] Ambiguous delivery redispatch returns silently when live prerequisites drop after the retry click [`src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:3266`]
- [x] [Review][Patch] `SupportsTrackedGrantDispatch` fail-closed default is never observed by an omitting-property construction [`src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorActionEvidence.cs:62`]
- [x] [Review][Patch] When ambiguous delivery retry is withdrawn, the disabled refresh control keeps the retry label and has no accessible reason [`src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:305`]

- [x] [Review][Defer] Focus containment is still interop-ID proof, not `document.activeElement` or a real Tab cycle [`tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs:2384`] — deferred: maybe-false / medium if true; already recorded as DW-336; an authenticated browser trace of open, Tab, Shift+Tab, Escape, and launcher restoration would settle it.
- [x] [Review][Defer] Package-reference Memories secret-store AC is not in this grant-core chunk [`src/Hexalith.Tenants.AppHost/Program.cs:132`] — deferred: pre-existing operator-owned gate (AC9 / DW-334); AppHost is group 3 of this review, not this pass.

Rejected:
- `false` Hand-rolled `section role="dialog"` instead of `FluentDialog` — Tenants has no `FluentDialog` usage; remove preview uses the same pattern; sentinels need a page-owned host.
- `false` `HasCompleteLocalization` leaks `CurrentUICulture` across requests — the setter is AsyncLocal; the grant property caches after the first walk; restore in `finally` is correct for that execution context.
- `false` French completeness disables English operators — intended EN/FR fail-closed gate; no third culture is served.
- `false` `AmbiguousTrackingFailure` recovery disagrees with status tracking-mismatch recovery — those are different states (same-id delivery retry vs verified identity mismatch → refresh).
- `false` `aria-hidden` on the grant area silences lifecycle live regions while preview is open — no grant lifecycle transition can occur in `Previewed`; invalidation leaves that state in the same render that clears `aria-hidden`.
- `false` `ConfirmGrantAsync` swallowing every `OperationCanceledException` leaves an undispatched preview as if confirm never ran — `ReauthorizeAsync` / compose rethrow only when the confirmation token is cancelled; `DispatchGrantAsync` handles its own OCE; leftover preview is the recoverable undispatched state.
- `false` Remove preview leaves the page background interactive — `IsAnyPreviewOpen` includes remove; `RemovePreviewUsesFluentControlsAndIsolatesTheBackground` asserts `inert` / `aria-hidden`.
- `false` `Retryable == false` with HTTP 5xx still invites ambiguous retry — 5xx is classified as maybe-stored by design; the non-retryable terminal tests cover 4xx.
- `false` Completeness ignores the current UI culture satellite — only invariant English and `fr` are served; a third culture needs a satellite and localization config together.
- `false` `EventsStored` / `EventsPublished` treated as positive evidence with no `EventCount` — platform contract (`CommandStatusRecord.EventCount` is Completed-only); changing the safeguard wording would edit this spec.
- `low` FrontComposer shell chrome stays mouse-operable behind the modal — keyboard is page-trapped; navigation abandons an undispatched lease; a shell overlay is not a direct correction.

### Review Findings — 2026-09-07 grant-core chunk

Independent follow-up of the same grant-core file group (`7e88a571..d0c534ff`, 14 files). The 2026-09-06 patch list was checked off in this spec, but the product tree still exhibits those defects. Correction-path, workspace/AppHost/gitlink, and later mainline commits remain out of this pass.

- [x] [Review][Patch] Localization failure never shows its associated recovery on the grant surface — `IsGlobalAdministratorGrantPreviewReady == false` maps through `EvaluateGrant` to `MissingConsequencePreview`; `SubmitGrantAsync` returns before compose, so operators get "safety flow is not ready" instead of `Grant.Preview.Unavailable.Localization` / `Recovery.Localization`. 2026-09-06 decision: show localization copy when readiness fails on localization. [`src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs:165`]
- [x] [Review][Patch] Grant preview opens with Cancel focused — opening still sets `_focusGrantPreviewCancelPending`; tests still pin `_grantPreviewCancelElement`. 2026-09-06 decision: acknowledgement first (match removal). [`src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:2999`]
- [x] [Review][Patch] Grant focus sentinels are not in the visually-hidden sentinel rule that only targets `.global-admins__remove-focus-sentinel` [`src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor.css:115`]
- [x] [Review][Patch] Grant modal CSS lacks `box-sizing: border-box` that the sibling remove modal and its style test already pin [`src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor.css:198`]
- [x] [Review][Patch] `FocusSafelyAsync` does not catch `InvalidOperationException`, and cancel/invalidate leave `_focusGrantPreviewCancelPending` set [`src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1385`]
- [x] [Review][Patch] `RequiredGrantFactKeys` omits `Grant.DeliveryRetry` and `Grant.DeliveryRetry.Recovery` that the live retry control renders [`src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs:25`]
- [x] [Review][Patch] Tests submit the grant form and never activate the launcher control [`tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs:5524`]
- [x] [Review][Patch] Modal-isolation test never asserts the grant dialog is outside the inert `tenants-global-admins-area` subtree [`tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs:2359`]
- [x] [Review][Patch] When ambiguous delivery retry is withdrawn, the disabled refresh control keeps the retry label and has no accessible reason [`src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:305`]
- [x] [Review][Patch] Ambiguous delivery redispatch returns silently when live prerequisites drop after the retry click [`src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:3266`]
- [x] [Review][Patch] Ambiguous grant `SafeRecoveryKey` is not asserted on the grant snapshot or page recovery paragraph [`src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorGrantCommandSnapshot.cs:212`]
- [x] [Review][Patch] Renderer replacement of an ambiguous grant does not pin `AuditDelayed` [`tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs:2803`]
- [x] [Review][Patch] `SupportsTrackedGrantDispatch` fail-closed default is never observed by an omitting-property construction [`src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorActionEvidence.cs:62`]

- [x] [Review][Defer] Focus containment is still interop-ID proof, not `document.activeElement` or a real Tab cycle [`tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs:2384`] — deferred: maybe-false / medium if true; already recorded as DW-336; an authenticated browser trace of open, Tab, Shift+Tab, Escape from Fluent shadow DOM, and launcher restoration would settle it.
- [x] [Review][Defer] `validate-story-gitlinks.py` FAILs against HEAD because later mainline moved `references/` pointers after `d0c534ff` [`scripts/validate-story-gitlinks.py`] — deferred: not caused by this 14-file grant-core chunk; UNDECLARED `Hexalith.AI.Tools` / `Hexalith.Memories` and MISSTATED Builds/Commons/EventStore/FrontComposer SHAs belong to group 3 / later `build(deps)` work.
- [x] [Review][Defer] Opening preview while `_focusGrantLauncherPending` is still set can focus the inert launcher [`src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1335`] — deferred: maybe-false / medium if true; settle with a same-turn cancel-then-reopen test that leaves both focus flags set before `OnAfterRenderAsync`.

Rejected:
- `false` Raw `<fluent-button>` for grant launch/cancel — `FluentButton` exposes no `ElementReference`; `GlobalAdministratorRemoveLauncher` uses the same host; preview cancel sits outside `EditForm`.
- `false` `HasCompleteLocalization` leaks `CurrentUICulture` across circuits — the setter is AsyncLocal; restore in `finally` is correct.
- `false` French completeness disables English operators — intended EN/FR fail-closed gate; no third culture is served.
- `false` `AmbiguousTrackingFailure` recovery disagrees with status tracking-mismatch recovery — different states (same-id delivery retry vs verified identity mismatch → refresh); the retry control keys off `IsSubmissionAmbiguous`, which status mismatch does not set.
- `false` `ConfirmGrantAsync` swallowing every `OperationCanceledException` leaves an undispatched preview — `ReauthorizeAsync` / load / compose rethrow only when the confirmation token is cancelled; those cancels already run `InvalidateGrantMutation`.
- `false` Target field can change after an undispatched preview exists — the input is `Disabled` while `Previewed` or in-flight; the handler invalidates if `ValueChanged` still fires.
- `false` `EventsStored` / `EventsPublished` with `EventCount` 0 still advance — platform contract (`CommandStatusRecord.EventCount` is Completed-only); advisory writes leave the count null.
- `false` Authority-change copy understates the privilege being granted — the 2026-09-01 safeguard requires observed-evidence wording without promising downstream enforcement timing.
- `false` Page tests stub `IsGlobalAdministratorGrantPreviewReady` so the EN/FR kill switch is untested — `GrantPreviewReadinessHoldsAgainstTheShippedEnglishAndFrenchResources` walks the production composition.
- `false` Remove preview was left as an in-flow section — current HEAD already presents removal as the same fixed, elevated modal.
- `false` Preview readiness must also list `Grant.Preview.Invalidated` and `Grant.SubmissionEvidence.Ambiguous` — those are post-dispatch lifecycle strings, not preview-dialog facts.
- `low` FrontComposer shell chrome stays mouse-operable behind the modal — keyboard is page-trapped; navigation abandons an undispatched lease; a shell overlay is not a direct correction.

### Review Findings — 2026-09-13 fix-round verification

Verification pass over the uncommitted working tree that closes the 2026-09-07 patch list (10 files, 649 diff lines: `GlobalAdministratorsPage.razor` + `.razor.css`, `ITenantsBffComposition.cs`, `TenantsBffComposition.cs`, four test files, two `references/` pointers). Four review layers ran: blind-hunter, edge-case-hunter, verification-gap, acceptance-auditor. Earlier mainline commits in `7e88a571..HEAD` remain out of this pass.

- [x] [Review][Patch] The withdrawn ambiguous retry still has no representation an operator can perceive — `SurfaceAmbiguousGrantRetryUnavailable` writes exactly the five fields `ApplySubmission`'s ambiguous branch already wrote (`SafeMessage=null`, generic `SafeMessageKey`, `SafeRecoveryKey=Grant.DeliveryRetry.Recovery`, `FocusTarget=Refresh`, `LiveRegionPoliteness=Assertive`), so on the common path it renders nothing new; on a snapshot from `AmbiguousTrackingFailure()` — or from `ApplySubmission` with a gateway-supplied `SafeMessageKey` — the hardcoded generic key **destroys** the specific diagnostic, collapsing exactly the two states this spec's own 2026-09-07 rejection insisted were "different states". The `Assertive` flip re-announces nothing because the live region's only content is `@GrantStateText` and `State` stays `RequestSent`. `GrantRefreshActionText` still returns `Grant.DeliveryRetry` ("Retry delivery") on the now-disabled control, so the first half of the ticked patch item is not done. Decide: define a distinct withdrawal state (a `WithdrawDeliveryRetry()` transition on the snapshot record plus an EN/FR reason+recovery+label triple), 2026-09-13 decision: define a distinct withdrawal state — add a `WithdrawDeliveryRetry()` transition on `GlobalAdministratorGrantCommandSnapshot` that preserves the incoming `SafeMessageKey`/`SafeMessage`, plus an EN/FR reason+recovery+label triple so the disabled control states why the retry was withdrawn. [`src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:3638`]
- [x] [Review][Patch] `RequiredGrantFactKeys` scope now contradicts this same round's recorded rejection — the round rejected adding `Grant.SubmissionEvidence.Ambiguous` as "post-dispatch lifecycle strings, not preview-dialog facts", then added `Grant.DeliveryRetry` and `Grant.DeliveryRetry.Recovery`, which are post-dispatch retry-control strings by the same test. The list is now neither rule: `Grant.SubmissionEvidence.Ambiguous` — hardcoded by the new `SurfaceAmbiguousGrantRetryUnavailable` — is still absent, as are `Grant.UnableToVerify.TrackingMismatch`, `Grant.Refresh`, `Grant.Description`, `Grant.Lifecycle.Title`, `Grant.Preview.Invalidated`, every `Grant.Preview.Unavailable.*`/`Recovery.*` and every `Grant.State.*`/`Grant.Audit.*`, all of which `RequiredRemoveFactKeys` carries counterparts for. A missing grant string therefore passes the readiness gate and renders to the operator as a raw resource key. All seven sampled keys exist in EN and FR, so either direction is safe to execute. 2026-09-13 decision: the list covers every string the grant surface renders — bring it to `RequiredRemoveFactKeys` parity and update the guard test's hard-coded count accordingly. [`src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs:25`]
- [x] [Review][Patch] `validate-story-gitlinks.py` exits 1 on two pointer moves this working tree itself adds — UNDECLARED `references/Hexalith.Memories d1b95ab -> d99bc96` and `references/Hexalith.AI.Tools de38f78 -> 5f93d2e`; MISSTATED Builds/Commons/EventStore/FrontComposer against the File List. The 2026-09-07 deferral attributed the failure to "later mainline `build(deps)` work", but `references/Hexalith.Memories` and `references/Hexalith.EventStore` are modified in this uncommitted tree, so that attribution no longer covers them. Both ranges touch only `_bmad-output/*.md` and `tools/*.py`, so nothing Tenants compiles against changes — this is traceability, not behaviour. 2026-09-13 decision: restore the baseline pointers for the paths this tree moves and land the bumps as a separate `build(deps)` commit, keeping the story diff to the grant-core files. [`scripts/validate-story-gitlinks.py`]

- [x] [Review][Patch] `GrantRefreshDescribedBy` can point `aria-describedby` at ids that are not rendered — it emits all three of `tenants-global-admin-grant-unavailable-reason`, `-recovery` and `-safe-recovery` whenever `IsAmbiguousGrantDeliveryRetry && !CanRetryAmbiguousGrantDelivery`, but the first two render only under `!string.IsNullOrWhiteSpace(GrantUnavailableReason)`, driven by `EvaluateGrant` — a disjoint input set from `CanRetryAmbiguousGrantDelivery` (which also goes false on incomplete `PreviewEvidence` or an unmarked lease). The disabled control then announces no reason at all, i.e. the original defect in a different state. The new assertion only covers the viewport-lapse variant, where availability happens to fail too. Derive the id list from the same predicates that render each element. [`src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1151`]
- [x] [Review][Patch] The launcher's new `@onclick` bypasses the rendered disabled state — `SubmitGrantAsync` performs no `IsGrantSubmitDisabled` recheck at entry, and `_grantPreviewInFlight` is 0 once the preview has closed, so a click reaching the handler during an in-flight tracked attempt runs `BeginGrantMutation()`, which cancels the active grant CTS and bumps the generation. Before this change the sole entry point was form submission, which a disabled submitter suppresses in the browser; enforcement now rests entirely on Fluent's `disabled` semantics for a custom element. (The click/submit double-entry itself is benign: the `Interlocked.CompareExchange` is reached synchronously before the first await on the valid path.) Add `if (IsGrantSubmitDisabled) { return; }` as the first statement. [`src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:2912`]
- [x] [Review][Patch] Redispatch still returns silently on the attempt-identity branches — only the `AreGrantDispatchCapabilitiesCurrent()` term was moved out of the guard. `RefreshGrantStatusAsync` still falls to a bare `return;` when `MatchesGrantPreviewAttempt` fails, and the `!exactAttempt` branch inside the dispatch callback returns with no surfaced state for `isRedispatch`. `CanRefreshGrantStatus` does gate on `CanRetryAmbiguousGrantDelivery`, but that predicate omits `MatchesGrantPreviewAttempt`, so the control stays enabled while the handler no-ops. [`src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:3411`]
- [x] [Review][Patch] `AmbiguousGrantDeliveryRetryPrerequisiteRaceSurfacesRecovery` cannot fail on the behaviour it names — `MessageId`, `IsSubmissionAmbiguous`, `SafeRecoveryKey` and `LiveRegionPoliteness` all already hold on `before`, because `ApplySubmission`'s ambiguous branch set them; the only discriminating assertion is `ShouldNotBeSameAs(before)`, which a record `with` satisfies unconditionally even if the whole body is deleted. No DOM assertion, no `SafeMessageKey` assertion, so neither the surfaced copy nor the tracking-mismatch overwrite is observed. [`tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs:2901`]
- [x] [Review][Patch] The grant unavailable-reason override admits a whitespace key and lets its two halves drift — `is { Length: > 0 }` accepts `" "`, after which `GrantUnavailableReason` returns whitespace and `IsGrantSubmitDisabled` (which tests `IsNullOrWhiteSpace`) flips to false, re-enabling submit while grant is unavailable. The reason and recovery keys are also resolved independently, so a composition that supplies only one yields localization reason paired with generic recovery. Tighten both patterns to require non-whitespace and require both halves together. [`src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:895`]

- [x] [Review][Defer] The `DispatchGrantAsync` redispatch-guard call site has no test [`src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:3316`] — deferred: filed by the verification-gap layer with that disposition; the window sits between an off-dispatcher capability read and the on-dispatcher `livePrerequisites` re-read, with no injectable seam in the bUnit harness, and the deterministic sibling guard is now covered. Reverting the call to a bare `return;` fails nothing.
- [x] [Review][Defer] Grant status refresh lacks the in-flight gate the remove side has [`src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1124`] — deferred: pre-existing, not touched by this change. `CanRefreshRemoveStatus` opens with `Volatile.Read(ref _removeSubmissionInFlight) == 0 && _removeAdmissionLease?.IsReconciliationDispatchInFlight != true`; `CanRefreshGrantStatus` has no equivalent, so a second retry click during an in-flight redispatch re-enters and can divert the returning submission result.
- [x] [Review][Defer] The sibling grant consumers did not adopt this round's pattern [`src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor:1173`] — deferred: pre-existing and outside the grant-page file group. `RetryAmbiguousDeliveryAsync` still returns silently for the same grant command, and neither the correction panel nor the remove surface consumes a localization-specific unavailable reason although `Remove.Preview.Unavailable.Localization` already exists in both resx files and in `RequiredRemoveFactKeys`.

Rejected:
- `false` NullReferenceException on `_grantAcknowledgementElement.Element` when the preview checkbox never rendered — the flag is set only inside the callback that transitions the snapshot to `Previewed` with complete evidence, which is precisely the condition that renders the checkbox; the component ref is assigned during that render, before `OnAfterRenderAsync`; and `InvalidateGrantMutation`, cancel and reset each clear the flag.
- `false` `AmbiguousSubmissionRetainsSameIdentityRecovery` duplicates its two neighbouring tests — it drives `ApplySubmission`'s ambiguous branch; the neighbours drive `Accepted()` into `AmbiguousTrackingFailure()`. Different code paths.
- `low` `catch (InvalidOperationException)` in `FocusSafelyAsync` is too broad, and a failed focus has no fallback target — the catch is exactly what the 2026-09-07 patch item asked for, and narrowing it or adding a fallback adds branches for a path reachable only when focus itself fails.
- `low` The localization reason key is drawn from the same key set whose incompleteness selects it, so `Localizer[key].Value` can render the raw key name — reachable only if the shipped resx is built missing those two specific keys; the fix adds a `ResourceNotFound` branch rather than a direct correction.
- `low` Memoizing `HasCompleteFixedGrantLocalization` into `ComposeGrantPreview` caches a first-read false — the layer filed this as "cached for the service lifetime", but `TenantsBffComposition` is registered `TryAddScoped`, so the cache is per-circuit.
