---
title: 'Remove Configuration Key with Complete Preview'
type: 'feature'
created: '2026-08-27'
status: 'done'
baseline_revision: 'c3b213689571d47e2b3b82facb181007134aaa2d'
baseline_commit: 'c3b213689571d47e2b3b82facb181007134aaa2d'
review_loop_iteration: 0
followup_review_recommended: true
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-3-context.md'
  - '{project-root}/docs/tenants-ui-truth-state-and-action-availability-spec.md'
warnings:
  - oversized
deferred:
  - summary: >-
      TenantConfigurationManagement latches _removeCommandInFlight from a retained attempt with no reset
      branch, so the flag survives the tracker's autonomous expiry when the flow is unmounted.
    evidence: |-
      OnParametersSet sets _removeCommandInFlight = true whenever RemoveAttemptTracker.Find returns a
      retained attempt, and only the flow's own lease callback lowers it. Tracker expiry raised while the
      flow is unmounted never runs that callback. The obvious else-reset was implemented and reverted: it
      lowers ChildCommandInFlight at the instant of confirmation, and the management landmark then replaces
      both flows with the unavailable paragraph before the operator can see the terminal state
      (Matching_signalr_notification_reconciles_retained_remove_without_redispatch_or_nudge_success fails).
      Impact is limited: the page clears its own _commandInFlight on expiry, so IsCommandSurfaceAvailable
      still recovers. A correct fix needs a distinct "flow owns the lease" signal.
    location: >-
      src/Hexalith.Tenants.UI/Components/Tenants/Configuration/TenantConfigurationManagement.razor:363
    severity: low
  - summary: >-
      One of the ten mandated preview facts is a constant, and roughly two dozen enum-keyed EN/FR strings
      can never render.
    evidence: |-
      TenantRemoveConfigurationPreview.IsComplete requires IsAuthoritative, which requires
      Freshness == Current and Lifecycle == Current. PreviewItems returns [] unless IsComplete, so the
      merged "Read model: {0}; projection lifecycle: {1}." fact always reads Current/Current, and only one
      of the Remove.Freshness.*, Remove.Lifecycle.* and Preview.CurrentState.* values is ever reachable.
      A degraded-freshness operator sees a block message instead of a degraded-freshness fact.
    location: >-
      src/Hexalith.Tenants.UI/State/TenantCommands/TenantRemoveConfigurationPreview.cs:50
    severity: low
  - summary: >-
      The untracked RemoveTenantConfigurationAsync overload silently changed its failure contract to keyed,
      SafeMessage-null results.
    evidence: |-
      It now delegates to RemoveTenantConfigurationTrackedAsync, so it can return Ambiguous or
      FailedWithKey("Tenants.Commands.Unavailable.InvalidTrackingReference") with SafeMessage null. No
      production caller remains, and no test pins the overload, so a future caller rendering SafeMessage
      would show empty text.
    location: >-
      src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs:273
    severity: low
---

<intent-contract>

## Intent

**Problem:** The historical remove-configuration flow renders the current value, locally assembles mutable preview evidence, loses attempt ownership on remount, and confirms from simple key absence. These gaps can disclose sensitive data, redispatch an ambiguous attempt, or label pre-existing absence as success.

**Approach:** Preserve the fixed command/read routes and destructive confirmation UX, but obtain an immutable redacted ten-fact preview from the authorization-first BFF, retain one caller-owned attempt per tenant, and confirm removal only from exact intent-bound absence with qualifying post-baseline provenance.

## Boundaries & Constraints

**Always:** Preserve literal case-sensitive tenant ids, namespaces, and keys; authorize before any raw dictionary lookup; require a complete preview and exact-key confirmation; retain one ULID/message identity and aggregate lease through terminal evidence or bounded abandon; keep accepted, projected, rejected, and audit states distinct; preserve last-confirmed rows; use Fluent UI V5, EN/FR whole strings, stable selectors, accessible focus/live regions, and support-safe diagnostics.

**Block If:** A new endpoint, domain-contract or aggregate change, submodule edit, preview bypass decision, or disclosure of a raw configuration value becomes necessary.

**Never:** Write or revert `_bmad-output/implementation-artifacts/sprint-status.yaml`; expose a current/raw value outside the command payload boundary; treat pre-submit absence or `ConfigurationKeyNotFound` as `AlreadyApplied`; optimistically remove a row; confirm from status, SignalR, pre-existing absence, an unchanged version, or unrelated projection movement; weaken server/API/domain authorization.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Eligible removal | Authorized present key, fresh/current ordered preview | Show exactly ten redacted facts; dispatch once with retained id; confirm only after exact absence plus qualifying causal provenance | Pending evidence remains non-success and expires or abandons as unable to verify |
| Missing or changed target | Authorized preview or submit-time recheck finds absence/different route | Preserve rows, block without dispatch, and explain refresh/reselect | Never report already applied or disclose whether an unauthorized key exists |
| Interrupted attempt | Timeout, ambiguous transport, refresh, remount, reconnect, or SignalR nudge | Adopt the same intent/message/baseline and reconcile without redispatch | Aggregate-aware status/proof failures remain retained or end unable to verify |
| Unsafe evidence | Revoked scope, disabled tenant, stale/incomplete preview, narrow viewport, mismatched proof, or non-advanced version | Keep read-only truth visible and fail closed with localized recovery | No raw payload, value, token, correlation, ETag, cursor, or internal metadata escapes |

</intent-contract>

## Code Map

- `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/RemoveTenantConfigurationFlow.razor` -- replace locally assembled value-bearing preview and absence-only reconciliation while preserving dialog, exact-key confirmation, bounded exit, focus trap, and lifecycle UI.
- `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/{TenantConfigurationManagement.razor,RemoveTenantConfigurationFlow.razor.css}` and `Components/Pages/TenantDetailPage.razor` -- retain a remove attempt across row disappearance/remount/policy changes, forward SignalR only as a nudge, and use Fluent 2 responsive/forced-colors styling.
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs` plus new `TenantRemoveConfiguration{Intent,CurrentState,Preview,CommandSnapshot,AttemptTracker}.cs` -- extract the legacy reducer and mirror Story 3.5's immutable preview, caller identity, bounded retention, monotonic status, aggregate ownership, and causal proof without a remove NoOp path.
- `src/Hexalith.Tenants.UI/Services/Gateways/{ITenantsBffComposition,TenantsBffComposition,ITenantQueryGateway,TenantQueryGateway}.cs` and `State/TenantDetail/TenantConfigurationProjectionProof.cs` -- authorize before lookup; emit present/absent classification, ordered baseline, and exact attempt fingerprint; never return raw values.
- `src/Hexalith.Tenants.UI/Services/Gateways/{ITenantCommandGateway,TenantCommandGateway,UnavailableTenantCommandGateway}.cs` -- add tracked remove dispatch, ambiguous-delivery retention, and aggregate-qualified status identity over existing routes.
- `src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs` and `Resources/TenantsResources*.resx` -- register circuit-local retention and replace stale/already-applied/value-bearing copy with support-safe EN/FR parity.
- `src/Hexalith.Tenants.Contracts/Commands/RemoveTenantConfiguration.cs`, `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`, and validator -- read-only evidence: the existing command, authorization, disabled-tenant ordering, missing-key rejection, and removal event remain authoritative.
- `tests/Hexalith.Tenants.UI.Tests/**` -- existing remove tests are the regression base; Story 3.5 set tests are the reuse model for BFF authority ordering, retained identity, monotonic evidence, remount, proof, localization, and sensitive-value absence.

## Tasks & Acceptance

**Execution:**
- `State/TenantCommands/TenantRemoveConfiguration*.cs`, `State/TenantDetail/TenantConfigurationProjectionProof.cs`, and `Extensions/TenantsUiServiceCollectionExtensions.cs` -- implement exact safe intent, ten-fact preview, extracted reducer, retained tracker, ordered causal proof, expiry, and explicit abandon.
- `Services/Gateways/*Tenant*.cs` -- compose authorization-first redacted preview/proof, accept the retained message id, contain ambiguous transport/cancellation safely, and verify message plus aggregate identity.
- `Components/Tenants/Configuration/{RemoveTenantConfigurationFlow,TenantConfigurationManagement}.razor*` and `Components/Pages/TenantDetailPage.razor` -- adopt/reconcile retained attempts, recheck preview evidence before dispatch, preserve aggregate lock, keep SignalR truth-neutral, remove value disclosure, and maintain keyboard/responsive behavior.
- `Resources/TenantsResources*.resx` -- localize complete preview, missing-target, retained, rejection, audit, recovery, and unable-to-verify states with exact placeholder parity.
- `tests/Hexalith.Tenants.UI.Tests/{Components,Services/Gateways,State}/**` and `TenantConfigurationEndToEndTests.cs` -- invert legacy leak/already-applied assertions and cover every matrix row, ten selectors, authorization-before-lookup, exact one-dispatch identity, ambiguous remount, aggregate status, causal version failures, EN/FR, focus/live regions, forced colors, and 767/768 refusal.

**Acceptance Criteria:**
- Given fresh authorized present-key evidence, when the operator reviews and confirms the exact key, then one fixed-route command is dispatched with retained identity and success appears only after intent-bound absence plus post-baseline causal evidence.
- Given missing, unauthorized, stale, incomplete, narrow, interrupted, mismatched, or non-advanced evidence, when the flow evaluates or reconciles, then last-confirmed truth remains visible, a precise safe recovery is exposed, and no value leaks or false success occurs.
- Given keyboard, screen-reader, forced-colors, English/French, SignalR, refresh, route, reconnect, and remount use, when lifecycle state changes, then focus, announcements, selectors, aggregate ownership, and accepted/projected/audited distinctions remain truthful.

## Spec Change Log

## Review Triage Log

### 2026-08-28 — Review pass (follow-up)
- intent_gap: 0
- bad_spec: 0
- patch: 7: (high 0, medium 5, low 2)
- defer: 0
- reject: 31: (high 0, medium 0, low 31)
- addressed_findings:
  - `[medium]` `[patch]` `TenantDetailPage.OnRemoveConfigurationOwnershipExpired` cleared `_activeAggregateLockKey`, `_aggregateLeaseHolder` and `_commandInFlight` on a tenant-lock-key match alone. A lifecycle or set-configuration surface can hold the same tenant's key while a stale remove retention expires; the owner-scoped `Release` is then a no-op, so the page forgot a lease it still held and orphaned that gate entry. Added the `ReferenceEquals(_aggregateLeaseHolder, _removeConfigurationLeaseOwner)` guard that the sibling `TryReclaimExpiredRemoveConfigurationLease` already carries.
  - `[medium]` `[patch]` `TenantRemoveConfigurationCommandSnapshot.ConfirmProjection` retained `LastConfigurationProof` for a proof that fails `Matches(Intent)` — another tenant or another exact key. The code this story replaced discarded it, and the test that asserted so (`wrongTenant.LastConfigurationProof.ShouldBeNull()`) was dropped with it. `TenantRemoveConfigurationAttemptTracker.Merge` orders retained against incoming snapshots by `LastConfigurationProof.ProjectionVersion`, so a foreign version skewed that choice. Proof is now discarded and the assertion restored.
  - `[medium]` `[patch]` The remove flow's status handle carries the tenant id, but no test asserted it: `TenantCommandGateway.GetStatusAsync` treats a blank `handle.AggregateId` as vacuously verified, so dropping the third argument silently downgraded the destructive flow to message-id-only identity and left every test green. Added the `AggregateId` assertion to `Page_remount_adopts_retained_remove_attempt_without_redispatch`, matching the set sibling.
  - `[medium]` `[patch]` Every submit test handed the flow an `Accepted` result, so the branch deciding whether an undeliverable dispatch keeps or drops its tracked identity was never executed — replacing it with a non-retaining `Blocked` snapshot passed the suite. Added `Ambiguous_remove_submission_retains_the_attempt_without_redispatch_and_renders_its_safe_message`.
  - `[medium]` `[patch]` `[data-testid='tenants-config-remove-safe-message']` had zero coverage. Because every keyed transition nulls `SafeMessage`, reverting `DisplaySafeMessage` to `_snapshot.SafeMessage` blanked the only support-safe explanation on all of ambiguous submission, status timeout, missing event evidence, projection-proof failure and abandon, undetected. The new test asserts the rendered copy, and the 16 flow-rendered keys the stub localizer was missing (`Abandon`, `Status.*`, `Submission.*`, `SubmissionEvidence.Ambiguous`, `UnableToVerify.*`, `Unavailable.Preview*`/`TrackedDispatch`) are now stubbed, which brings them under the localizer-double parity gate.
  - `[low]` `[patch]` Refresh was offered on a retained attempt with no correlation id, where `RefreshStatusAsync` skips the status read and the proof read and projection nudge below it are gated on `Accepted`/`ProjectionPending` — no read, no message, no state change. `CanRefresh` now requires a correlation id; Abandon remains reachable throughout.
  - `[low]` `[patch]` The ten-fact merge left `Preview.KnownConsequences` ("Known consequences") labelling a sentence that is explicitly about unknowns. Relabelled EN/FR to "Known consequences versus unknowns", matching the `Tenants.RemoveMember.Preview.ConsequencesVersusUnknowns` precedent.

### 2026-08-28 — Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 11: (high 0, medium 6, low 5)
- defer: 3: (high 0, medium 0, low 3)
- reject: 16: (high 1, medium 8, low 7)
- addressed_findings:
  - `[medium]` `[patch]` Moved `RefreshPreviewAsync`'s bail-out above `StartSubmitOperation` and the `_previewEvidenceUnavailable` reset, so a declined refresh no longer cancels an in-flight submit's token or erases a rendered evidence-unavailable explanation.
  - `[medium]` `[patch]` Unsubscribed `OwnershipExpired` in `TenantDetailPage.DisposeAsync`; the circuit-scoped tracker previously kept one disposed page pinned per navigation.
  - `[medium]` `[patch]` Deferred `OwnershipExpired` notifications until `_sync` is released in the tracker's prune path, matching the shape `ObserveExpiryAsync` already used deliberately.
  - `[medium]` `[patch]` Bound the component stub localizers to the shipped English copy for the story's new preview, lifecycle, freshness and target-missing keys; the stubs echoed unknown keys, so the assertions passed on key names rather than operator-visible text.
  - `[medium]` `[patch]` Added `Confirmed_remove_dispatches_once_through_the_composed_detail_page`: the flow-only tests hold `IsCommandSurfaceAvailable` static, so nothing exercised a real dispatch through page → management → flow with the live aggregate lease.
  - `[medium]` `[patch]` Added `Remove_configuration_attempt_tracker_is_scoped_to_a_circuit`; the scoped registration was pinned by no test, so a missing or singleton descriptor would have shipped undetected.
  - `[low]` `[patch]` Deleted seven EN/FR resource pairs left unreachable by the reducer rewrite and the ten-fact merge (`Unavailable.InFlight`, three `DuplicatePrevented` strings, `Preview.KnownUnknowns`, `.KnownUnknowns.Value`, `.KnownConsequences.Value`) and the matching stale stub entries.
  - `[low]` `[patch]` Restored danger semantics on the destructive preview accent: `--colorPaletteRedBorder2` with a `CanvasText` forced-colors fallback, instead of the brand stroke with a `LinkText` fallback.
  - `[low]` `[patch]` Removed the now-callerless `ApplySignalRNudge()` / `ApplyProjectionEvidence(...)` members from the remove flow; the page routes through the async seams.
  - `[low]` `[patch]` Dropped the redundant second `IsPrefixMatch` term in `TenantsBffComposition.ComposeRemoveConfigurationPreviewAsync`, already guaranteed by the method's entry guard.
  - `[low]` `[patch]` Replaced `SafeMessage.ShouldNotBe("ConfigurationKeyNotFound")` — an exact-equality negation that passed for any string — with an equality assertion on the shipped target-missing copy plus substring exclusions.

### 2026-08-28 — Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 15: (high 8, medium 6, low 1)
- defer: 0
- reject: 7: (high 2, medium 4, low 1)
- addressed_findings:
  - `[high]` `[patch]` Revalidated the full safe input scope after suspended preview and aggregate-lease awaits so stale surfaces cannot dispatch.
  - `[high]` `[patch]` Added bounded cancellation, in-flight expiry cleanup, and late-completion invalidation so hung operations cannot retain dismissal or aggregate ownership indefinitely.
  - `[high]` `[patch]` Made tracker-retention rejection invalidate UI ownership and release activity instead of leaving an untracked attempt active.
  - `[medium]` `[patch]` Made lease cleanup exception-safe so submit state always resets.
  - `[medium]` `[patch]` Invalidated and refreshed preview evidence when the target or other same-tenant preview inputs change.
  - `[high]` `[patch]` Added autonomous tracker expiry notification and page gate release so route-away ownership remains bounded without a later lookup.
  - `[high]` `[patch]` Made newer tracking-mismatch and timeout evidence survive tracker merge while still allowing stronger later event evidence to recover.
  - `[medium]` `[patch]` Separated projection-proof failure from status failure so verified event truth remains projection-pending.
  - `[high]` `[patch]` Prevented delayed status responses from regressing stored, published, or degraded evidence.
  - `[low]` `[patch]` Added missing English and French tracked-dispatch and projection-proof recovery resources.
  - `[medium]` `[patch]` Added gateway proof coverage for an authoritative present key, exact fingerprint, and ordered version.
  - `[high]` `[patch]` Added page remount coverage proving retained message adoption, gate ownership, and no redispatch.
  - `[medium]` `[patch]` Added page-level SignalR coverage proving reread without redispatch or truth promotion from the nudge alone.
  - `[medium]` `[patch]` Added exact ten-fact preview selector coverage with value non-disclosure.
  - `[high]` `[patch]` Preserved tracked terminal lifecycle evidence across the parent refresh that supplies causal confirmation.

## Design Notes

Removal has no NoOp success path. A pre-submit absent target is stale/missing evidence; a handled `ConfigurationKeyNotFound` is a rejection. Confirmation requires `RemoveConfirmed`, exact intent fingerprint, command-event evidence, and an ordered projection version newer than the present-key preview baseline.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-restore -warnaserror -m:1 -nr:false` -- expected: zero warnings and errors.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` -- expected: all UI tests pass.
- `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -warnaserror -m:1 -nr:false` -- expected: solution builds cleanly.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-3-6-remove-configuration-key-with-complete-preview.md` -- expected: no undeclared gitlink movement.
- `git diff --check` -- expected: no whitespace errors.

## Auto Run Result

Status: done
Pass: follow-up review of a completed run (`review_loop_iteration` reset to 0). No implementation loopback was required — no `intent_gap` and no `bad_spec` findings.

**Implemented change (this pass).** Two behavioural corrections and five verification corrections on top of the shipped story:

- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor` -- the remove-tracker ownership-expiry handler now proves the removal surface holds the page lease before clearing `_activeAggregateLockKey`, `_activeAggregateTenantId`, `_aggregateLeaseHolder` and `_commandInFlight`.
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantRemoveConfigurationCommandSnapshot.cs` -- `ConfirmProjection` discards a projection proof bound to another tenant or another exact key instead of retaining it as this attempt's evidence.
- `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/RemoveTenantConfigurationFlow.razor` -- `CanRefresh` additionally requires a correlation id, so the flow stops offering a Refresh control that performs no read and changes no state.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`, `TenantsResources.fr.resx` -- `Tenants.Configuration.Remove.Preview.KnownConsequences` relabelled to "Known consequences versus unknowns" / "Conséquences connues versus inconnues".
- `tests/Hexalith.Tenants.UI.Tests/Components/RemoveTenantConfigurationFlowTests.cs` -- new `Ambiguous_remove_submission_retains_the_attempt_without_redispatch_and_renders_its_safe_message` and `Refresh_is_not_offered_while_a_retained_attempt_has_no_correlation_to_read`; the stub localizer gained the 16 flow-rendered keys it was missing and the relabelled value.
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs` -- the remount test now pins the status handle's `AggregateId`; stub localizer synced with the relabelled value.
- `tests/Hexalith.Tenants.UI.Tests/State/TenantRemoveConfigurationCommandSnapshotTests.cs` -- `Proof_for_another_exact_intent_cannot_confirm` now also asserts the foreign proof is not retained.

**Review findings breakdown.** 7 patches applied (0 high, 5 medium, 2 low), 0 items deferred (the three pre-existing `deferred` entries are untouched), 31 items rejected. Rejections were dropped either as verified-false (`PruneExpiredLocked` cannot double-notify, since its first loop removes the `_dispatches` entry; `TenantDetail.Configuration` is a non-nullable contract member; `Tenants.Configuration.Remove.Freshness.Refreshing` is a repo-wide convention present in 12 string families; `PreviewInputScope` reference-equality churn cannot lose a dispatch, because the dispatch runs on a separate bounded token and `retainedOwnershipEstablished` gates the ambiguous fallback), as established parity with the shipped set sibling from Story 3.5 (`internal` preview `Create`, non-matching-proof handling shape, `Members` null-coalescing), or as style and cost observations with no consequence for the operator.

**Follow-up review recommendation:** `true`. Patched counts: high 0, medium 5, low 2. Score = 3 × 5 + 1 × 2 = 17, which is at or above the threshold of 5.

**Verification performed.**

- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-restore -warnaserror -m:1 -nr:false` -- Build succeeded, 0 Warning(s), 0 Error(s).
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` -- Total: 2540, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0.
- `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -warnaserror -m:1 -nr:false` -- Build succeeded, 0 Warning(s), 0 Error(s).
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-3-6-remove-configuration-key-with-complete-preview.md` -- RESULT: PASS, no `references/` pointer changes in range.
- `git diff --check` -- clean.
- Mutation verification, each run against the single named test: reinstating `LastConfigurationProof = proof` on the non-matching branch fails `Proof_for_another_exact_intent_cannot_confirm`; dropping the tenant id from the flow's `TenantCommandTrackingHandle` fails `Page_remount_adopts_retained_remove_attempt_without_redispatch`; replacing the ambiguous branch with a non-retaining `Blocked` snapshot, and separately reverting `DisplaySafeMessage` to `_snapshot.SafeMessage`, each fail `Ambiguous_remove_submission_retains_the_attempt_without_redispatch_and_renders_its_safe_message`; reverting the `CanRefresh` correlation guard fails `Refresh_is_not_offered_while_a_retained_attempt_has_no_correlation_to_read`.

**Residual risks.**

- The aggregate-lease guard on `OnRemoveConfigurationOwnershipExpired` ships without a dedicated regression test. Reproducing it needs a composed page where a lifecycle surface holds the tenant lease while a real 500 ms remove-retention timer expires, and the only observable difference is page-private bookkeeping, which this repository's data-testid test convention does not reach. The full suite proves no regression; the guard mirrors the already-tested `TryReclaimExpiredRemoveConfigurationLease`.
- The attempt tracker is registered `Scoped`, so the intent matrix's *refresh* and *reconnect* interruptions start a new circuit and are not adopted. This is not specific to removal: all four attempt trackers (`Create`, `Lifecycle`, `SetConfiguration`, `RemoveConfiguration`) are circuit-local, and `TenantsUiCompositionTests` pins that lifetime deliberately. Durable cross-circuit adoption would need a server-side per-user retention seam, which the intent's Block-If clause puts outside this story.
- One of the ten preview facts remains a constant and roughly two dozen enum-keyed EN/FR strings remain unreachable while `PreviewItems` returns `[]` unless the preview `IsComplete`. Already recorded in `deferred` and untouched by this pass.
