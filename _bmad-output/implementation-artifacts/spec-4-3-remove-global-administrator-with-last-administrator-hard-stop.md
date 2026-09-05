---
title: '4.3 Remove Global Administrator with Last-Administrator Hard Stop'
type: 'feature'
created: '2026-09-01'
status: 'in-progress'
baseline_revision: '91d233558ad830555e5ed09803498a6d36c8de50'
baseline_commit: 'ed0c0d681ae15913bc999908bc9e25bfd622536c'
review_loop_iteration: 2
followup_review_recommended: false
context:
  - '{project-root}/_bmad-output/project-context.md'
warnings:
  - oversized
deferred: []
---

<intent-contract>

## Intent

**Problem:** The legacy removal flow enforces the domain last-administrator rule but can lose its command identity on ambiguous delivery, confirm from page-scoped absence without causal projection evidence, and expose an incomplete removal preview/focus lifecycle. The generic command boundary also accepts a removal envelope outside the fixed singleton identity.

**Approach:** Retain the proven aggregate hard stop and fixed-scope availability guardrail, then bring removal to parity with the tracked grant lifecycle: BFF-composed immutable preview, caller-owned ULID, exact status/event provenance, bounded complete-projection reconciliation, retained recovery, and an isolated accessible destructive dialog. Enforce the fixed removal identity before routing.

## Boundaries & Constraints

**Always:** Treat UserId as a literal, case-sensitive ordinal string; use `system/global-administrators/global-administrators`; require authoritative current complete population evidence and count greater than one before preview and immediately before dispatch; retain one message id and fixed-aggregate lease through terminal evidence; keep submitted, accepted, projection-pending, confirmed, rejected, unable-to-verify, and audit states distinct; confirm only from exact-command event evidence plus bounded complete target absence and projection-version advancement; use FrontComposer/Fluent UI V5 and parity-checked whole-string EN/FR resources; keep last-confirmed rows separate from intent.

**Never:** Change the existing aggregate/rejection semantics, tenant membership, event history, query/controller read contract, AppHost topology, dependencies, submodules, or shared FrontComposer; add an endpoint, bulk action, tenant-derived scope, optimistic deletion, last-admin override/friction path, NoOp/already-applied removal result, raw interactive control, raw diagnostic disclosure, or generic command framework; write, stage, revert, or use `_bmad-output/implementation-artifacts/sprint-status.yaml` as evidence.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Eligible removal | Authorized caller, current versioned complete population, target present, count > 1, safe viewport, free aggregate | Complete BFF preview opens; exact target confirmation dispatches one retained-ID fixed-scope command | No error expected |
| Last administrator | Complete population contains only the target | Visible hard-stop reason before preview; no callable launcher or dispatch | Refresh/continue read-only only; no override or retry friction |
| Unsafe evidence | Stale/unknown/partial/version-mismatched data, target absent, missing support, busy aggregate, unsafe viewport | Affected action fails closed with associated reason and recovery | Preserve authorized last-confirmed rows; dispatch nothing |
| Self-removal | Target equals authoritative caller subject | Preview names possible future governance loss without claiming session/token or tenant effects | Require the same exact typed confirmation |
| Ambiguous delivery | Timeout, transport loss, 408/429/5xx after request may have reached server | Preserve preview, message id, intent, lifecycle, and lock; retry/adopt only the same id | Status lookup/requery or same-id retry; never mint a replacement id |
| Domain race | Target becomes final administrator before handling | `LastGlobalAdministrator` is an assertive rejection and rows remain unchanged | Refresh/continue read-only; never success, NoOp, or member-removal recovery |
| Target already absent | `GlobalAdministratorNotFound` | Rejected state; pre-existing absence is not proof of this attempt | Refresh/inspect current authority; never already-applied |
| Unqualified status/projection | Mismatched tracking, zero-event Completed, SignalR only, visible-page absence, unchanged version | No confirmation; lifecycle remains pending or becomes unable-to-verify | Offer safe status/requery/escalation recovery |
| Reconnect/replacement | Preview or tracked command survives rerender/circuit replacement | Matching owner adopts retained evidence and resumes without double dispatch or stranded lock | Collapse privileged rows on authoritative permission loss |

</intent-contract>

## Code Map

- `src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsAggregate.cs:38` -- read-only invariant owner; already rejects unauthorized, absent-target, and last-administrator removal against rehydrated singleton state.
- `src/Hexalith.Tenants/Validation/TenantSubmitCommandValidator.cs:11` and new `Validation/RemoveGlobalAdministratorValidator.cs` -- validate payload plus exact ordinal singleton envelope before `ICommandRouter` sees removal.
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorGrantPreview.cs:29` -- golden pattern for a new immutable `GlobalAdministratorRemovePreview` with ten facts, baseline, target/caller context, completeness, matching, and support-safe diagnostics.
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantsBffComposition.cs:26` and `TenantsBffComposition.cs:79` -- add concrete removal preview composition/localization readiness; replace unconditional remove readiness.
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs:75` and `TenantCommandGateway.cs:335` -- mirror tracked grant dispatch for removal with explicit canonical ULID, exact fixed payload, and ambiguous-delivery result mapping.
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorRemoveCommandSnapshot.cs:9` -- replace absence-only lifecycle with typed preview, retained attempt id, verified tracking/event evidence, monotonic states, localization keys, and causal confirmation.
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorReconciliationState.cs:16` -- retain removal preview/baseline, event evidence, ambiguity, and correlation when present for exclusive adoption.
- `src/Hexalith.Tenants.UI/Services/Gateways/GlobalAdministratorsProjectionLoader.cs:23` -- reuse unchanged bounded cursor-safe complete-population load for preview and post-command proof.
- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:3203` -- integrate typed preview, exact confirmation, final-boundary rechecks, tracked dispatch, status/requery provenance, notification nudge, retained adoption, modal isolation, and exact launcher focus return.
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor:773` and `State/TenantAudit/GlobalAdministratorCorrectionSnapshot.cs:455` -- consume tracked removal and the same causal absence proof without changing correction semantics or fabricating audit proof.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`, `TenantsResources.fr.resx`, and `Components/Pages/GlobalAdministratorsPage.razor.css` -- whole-string preview/confirmation/self-removal/rejection/recovery copy and Fluent-safe dialog/focus/responsive rules.
- `tests/Hexalith.Tenants.Server.Tests/CommandPipeline/TenantSubmitCommandValidatorTests.cs` and `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs` -- prove wrong/case-variant fixed identities and invalid literal payloads fail before routing.
- `tests/Hexalith.Tenants.UI.Tests/State/GlobalAdministratorRemoveCommandSnapshotTests.cs` and focused gateway/composition/page/correction/admission tests -- removal lifecycle, proof, modal, responsive, reconnect, localization, and no-dispatch matrix.

## Tasks & Acceptance

**Execution:**
- `src/Hexalith.Tenants/Validation/RemoveGlobalAdministratorValidator.cs`, `src/Hexalith.Tenants/Validation/TenantSubmitCommandValidator.cs`, `tests/Hexalith.Tenants.Server.Tests/CommandPipeline/TenantSubmitCommandValidatorTests.cs`, and `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs` -- reject blank payload identities and every non-exact singleton envelope for both supported command discriminators before routing; preserve mixed-case UserId verbatim.
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorRemovePreview.cs`, `src/Hexalith.Tenants.UI/Services/Gateways/ITenantsBffComposition.cs`, `src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs`, and both `src/Hexalith.Tenants.UI/Resources/TenantsResources*.resx` files -- assemble all ten facts from one freshly authorized complete snapshot, including resulting count and truthful self-removal context; missing principal, evidence, or localized fact blocks readiness.
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs`, `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`, `src/Hexalith.Tenants.UI/Services/Gateways/UnavailableTenantCommandGateway.cs`, and `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs` -- add an independent tracked-remove capability/method; validate and reuse the supplied ULID, preserve literal payload/fixed route, and distinguish ambiguous retryable delivery from definite rejection.
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorRemoveCommandSnapshot.cs`, `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorReconciliationState.cs`, `src/Hexalith.Tenants.UI/State/TenantAudit/GlobalAdministratorCorrectionSnapshot.cs`, and `src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor` -- retain preview/message identity and exact event evidence; verify message/correlation/aggregate status; make SignalR nudge-only; require complete current target absence plus advanced projection version and exact-command evidence; retain ambiguous attempts through adoption.
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorActionEvidence.cs`, `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorActionAvailabilityEvaluator.cs`, and `tests/Hexalith.Tenants.UI.Tests/State/GlobalAdministratorActionAvailabilityTests.cs` -- require tracked-remove and concrete remove-preview support independently without weakening Story 4.1 count, authority, admission, freshness, target, or viewport precedence.
- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor`, `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor.css`, and `tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs` -- compose fresh preview, capture typed exact-target acknowledgement, recheck every gate in the final renderer boundary, dispatch once, reconcile through the complete loader while preserving the paged display, resume removal on notifications/reconnects, isolate/trap the dialog, restore the exact launcher, and keep cancel visible after a live unsafe resize.
- `tests/Hexalith.Tenants.UI.Tests/State/GlobalAdministratorRemoveCommandSnapshotTests.cs`, `tests/Hexalith.Tenants.UI.Tests/State/GlobalAdministratorCorrectionSnapshotTests.cs`, `tests/Hexalith.Tenants.UI.Tests/State/TenantAggregateCommandAdmissionGateTests.cs`, `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantsBffCompositionTests.cs`, and `tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorCorrectionPanelTests.cs` -- cover every remaining matrix row: complete multi-page proof, last-admin pre-block/race mapping, self-removal, ambiguous same-id retry, status mismatch/zero-event refusal, notification non-confirmation, mutual grant/remove lock, no optimistic deletion, redaction, EN/FR parity, modal/focus semantics, and responsive safe exit.

**Review repair constraints (iteration 1):**
- `TenantAggregateCommandAdmissionGate.cs` and its tests -- accept a correlationless removal reconciliation only when it is the retained ambiguous request-sent attempt with a complete removal preview; compare the removal preview as part of command identity so adoption cannot replace its baseline or facts.
- Removal and correction projection confirmation -- call the ordered, command-event-qualified projection-version advancement contract. An unequal, regressed, malformed, or unrelated token is never advancement.
- Page notification/retry handling -- SignalR is requery-only and must never enter ambiguous redispatch. Operator-initiated ambiguous retry is single-flight and uses the retained ID only after fresh authorization, a new complete-population walk, an exactly matching rebuilt preview, target/count/version checks, and current viewport/capability evidence.
- Page safety transitions -- re-evaluate availability after awaited preview composition and immediately before dispatch/retry; render the associated localized reason and recovery when live evidence becomes unsafe; collapse privileged rows for unauthorized complete reads during preflight or reconciliation; move invalidated-preview focus to the visible lifecycle/launcher rather than a removed dialog control.
- Failure presentation -- every definite removal failure, rejection, publish failure, timeout, and unable-to-verify branch carries localized EN/FR recovery; do not surface English gateway literals inside a French lifecycle. Preserve exact literal identifiers visibly, including leading/trailing spaces, and provide a usable exact-value confirmation path.
- Correction removal -- derive remove-preview readiness from the concrete BFF capability, fail closed inside exception handling when projection/preview composition fails, and keep existing correction confirmation semantics; do not require a second dialog or expose the BFF preview as a new UI surface.
- Dispatch composition -- shared connectivity represents the base command surface only; tracked grant and tracked removal capability remain independent action-specific gates.
- Executable coverage -- add negative case/partial exact-acknowledgement tests; removal modal inertness, initial/sentinel focus, exact-launcher restoration, and responsive safe-exit tests; page-level multi-page reconciliation; page and correction same-ID ambiguous retry; exact removal status handle; notification non-redispatch; correlationless replacement adoption; version regression/malformed-token refusal; and preserve the existing aggregate last-administrator rejection test in the full server lane.

**Review repair constraints (iteration 2):**
- Polling and submission states -- only start projection reconciliation after exact positive command-event evidence; `Received` and `Processing` remain accepted and never become unable-to-verify merely because no event exists yet. Treat every unsupported submission lifecycle, including `AlreadyApplied` or `Confirmed`, as unable-to-verify without releasing the lease or claiming success.
- Ambiguous retry ownership -- acquire the page retry single-flight guard before reauthorization or generation invalidation. If a fresh retry preflight changes or fails, keep the correlationless dispatched attempt in a retainable/adoptable ambiguous `RequestSent` shape with the same message id, preview, lock, localized reason, and recovery; apply the same rule in correction.
- Authorization and staleness -- collapse privileged page/correction state when BFF preview composition proves missing permission during preview, submit, or retry. Re-check operation generation after every awaited complete read before applying authorization collapse, and turn a null correction projection into an explicit localized unable-to-verify recovery state.
- Associated failure evidence -- when final page or correction availability changes, carry the exact current localized reason and recovery instead of generic preview-changed copy. Make all removal correction tracking, status, rejection, failure, timeout, and retry messages removal-specific resource keys; never render raw gateway prose.
- Localization contract -- removal readiness covers every success and fail-closed key used by preview and lifecycle paths in both EN and FR, and verifies required `{0}` placeholders for exact-target display and acknowledgement. Render literal correction identifiers with preserved whitespace.
- Focus contract -- record a remove launcher only after winning the preview single-flight gate; restore focus to the exact interactive row button, fall back to the visible lifecycle region when that launcher disappeared, and make the end sentinel cycle to the acknowledgement input while the start sentinel cycles to Cancel.
- Uniform identity contract -- apply the 256-character and control-character rules consistently in availability, preview, gateway, validator, and both command discriminators so an advertised action cannot later reject the same visible identifier.
- Correction recovery -- live parent projection refreshes can recover every removal-preview projection block, including evidence, target-missing, and last-administrator states, without recreating the panel.
- Executable coverage -- add tests for 256/257-character identities at server and gateway boundaries; tracked-remove capability absence; safety changing while preview composition awaits; retry preflight invalidation without a second dispatch or lost lease; correlationless page replacement adoption; correction composition exception and null projection; operation-time authorization denial; `Received`/`Processing`, `PublishFailed`, and `TimedOut`; unsupported submission states; exact interactive focus/fallback; complete localized key and placeholder readiness; and unchanged aggregate hard-stop execution in the full server lane.

**KEEP during re-derivation:** Preserve exact fixed-envelope/payload validation, literal case-sensitive UserId transport, immutable complete BFF preview facts and self-removal copy, caller-owned canonical ULID dispatch, verified status identity/event-count gates, complete-population absence proof, no optimistic row deletion, retained aggregate exclusivity, whole-string EN/FR resources, the isolated responsive dialog shape, and unchanged aggregate/event/controller/AppHost/dependency/submodule surfaces.

**Acceptance Criteria:**
- Given authoritative current complete fixed-scope evidence and every support/admission/viewport gate, when a visible non-last target is evaluated, then only that exact removal may open a complete ten-fact BFF preview; any unknown or unsafe input renders a localized associated reason and sends no command.
- Given exactly one administrator, when its action slot renders, then last-administrator removal is unavailable before preview and cannot be invoked as override, friction, NoOp, already-applied, or tenant-member removal.
- Given an eligible preview, when the caller is the target or types/cancels/navigates the confirmation, then self-removal copy is truthful, exact ordinal target text is required, background content is inert, focus is trapped and restored to the row launcher, and cancel/Escape dispatch nothing even after viewport invalidation.
- Given dispatch or delivery/reconnect races, when lifecycle handling runs, then one retained ULID and one fixed-aggregate lease survive ambiguous delivery/adoption, all grant/removal actions stay locked through terminal evidence, and no second logical attempt is dispatched.
- Given status, SignalR, or projection changes, when reconciliation evaluates them, then only exact verified command-event evidence plus bounded complete target absence and projection-version advancement confirms; pre-existing/page-only absence, status/SignalR alone, unrelated change, mismatch, or zero-event completion never succeeds.
- Given last-admin, not-found, permission, transport, degraded, or unable-to-verify outcomes, when the lifecycle renders, then last-confirmed truth remains unchanged, safe EN/FR state-specific recovery is shown, audit availability is not fabricated, and no sensitive diagnostic or tenant-scope implication is observable.
- Given server, UI, localization, accessibility, responsive, reconnect, and fixed-routing verification runs, when the story is assessed, then the existing aggregate invariant remains intact and all focused/full gates pass without modifying orchestrator-owned status, dependencies, submodules, or unrelated code.

## Spec Change Log

### 2026-09-01 — Review repair iteration 1

- Trigger: the first independent review found that the plan omitted the admission-gate implementation seam and did not make several causal/retry/accessibility tests mechanically explicit, allowing correlationless removal adoption to fail and notification/retry/projection paths to diverge from the intent contract.
- Amended: added the iteration-1 repair constraints covering reconciliation validity/identity, ordered causal versions, nudge-only notification behavior, single-flight fresh retry preflight, authoritative permission collapse, localized failure/recovery, correction BFF readiness/exception containment, independent dispatch capability, literal whitespace usability, and named executable tests.
- Known-bad state avoided: do not retain the first implementation's correlation-required removal reconciliation, any-unequal version confirmation, SignalR redispatch, stale retry preflight, detached-dialog focus, silent safety disablement, or incomplete verification suite.
- KEEP: preserve the fixed validator, immutable BFF preview, tracked ULID gateway, verified event evidence, complete projection loader, modal foundation, localization parity, and invariant/domain surfaces enumerated above.

### 2026-09-01 — Review repair iteration 2

- Trigger: the second independent review found that the re-derived consumers still reconciled accepted-without-event status, allowed an overlapping retry to cancel the active single-flight operation, and could make a correlationless ambiguous preflight failure non-retainable; it also exposed incomplete authorization collapse, removal-specific localization, focus, and boundary verification.
- Amended: added explicit polling-state, retry-ownership, retained-preflight, authorization-generation, associated-reason, localization-placeholder, interactive-focus, uniform-identity, correction-recovery, and executable-test constraints.
- Known-bad state avoided: do not requery projection from `Received`/`Processing`, invalidate the active retry before winning single-flight, convert a dispatched correlationless attempt into a non-retainable state, retain privileged rows after authoritative denial, render grant/raw-English recovery for removal, focus wrapper spans, or advertise overlong targets.
- KEEP: preserve exact fixed-envelope/payload validation, the immutable ten-fact BFF preview, caller-owned ULID dispatch, ordered causal confirmation, complete multi-page projection loading, nudge-only notifications, independent action capabilities, EN/FR resource parity, inert responsive modal behavior, and the unchanged aggregate/domain/AppHost/dependency/submodule surfaces.

## Review Triage Log

### 2026-09-01 — Review pass
- verdicts: 29 findings — high 1, medium 24, low 1, false 3, maybe-false 0
- findings:
  - `[medium]` `[bad_spec]` Correlationless ambiguous removal reconciliation was rejected by the admission gate — `IsValidReconciliation` required a removal correlation even though ambiguous tracked delivery intentionally retains only the message ID; the repair constraints now name the exact valid ambiguous shape.
  - `[medium]` `[bad_spec]` Removal preview was absent from retained command-identity comparison — two removal reconciliations with different preview baselines compared equal because both grant previews were null; the repair constraints now require removal-preview equality.
  - `[medium]` `[bad_spec]` Page removal confirmation accepted any unequal projection token — the two-argument helper proves difference, not ordered advancement; the repair constraints now require the event-qualified ordered overload.
  - `[medium]` `[bad_spec]` Correction removal confirmation repeated the unequal-token flaw — the same legacy overload was used with a removal preview baseline; the ordered causal contract is now explicit for both consumers.
  - `[medium]` `[bad_spec]` SignalR could redispatch a correlationless ambiguous removal — notification handling called the operator refresh method, whose ambiguous branch sends the command; nudge-only notification behavior is now explicit.
  - `[medium]` `[bad_spec]` Ambiguous removal retry skipped fresh complete-population and preview matching — capability checks alone allowed a resend after target/count/version evidence changed; the repair constraints now require a fresh final-boundary preflight.
  - `[medium]` `[bad_spec]` Live unsafe evidence silently disabled the removal confirmation — the disabled predicate changed but lifecycle reason/recovery still read only stored snapshot messages; dynamic associated explanation is now required.
  - `[high]` `[bad_spec]` Unauthorized complete reads during submit or reconciliation left privileged rows visible — unlike ordinary loads and preview entry, those paths did not call authorization collapse; every authoritative unauthorized mutation read must now collapse the surface.
  - `[medium]` `[bad_spec]` Invalidated preview focus targeted a removed dialog control — `InvalidatePreview` selected Submit and the renderer attempted the detached acknowledgement element instead of visible recovery; the repair constraints now require lifecycle/launcher focus.
  - `[medium]` `[bad_spec]` Definite removal outcomes omitted state-specific recovery keys — failed, rejected, publish-failed, and timeout branches cleared recovery while disabling further action; localized recovery is now required for each state.
  - `[low]` `[bad_spec]` French removal lifecycle could display English gateway prose — non-resource safe messages passed through unchanged; the repair constraints now require localized keys at the interactive removal surface.
  - `[medium]` `[defer]` Correction preflight still classifies a previously absent revoke target as AlreadyApplied — the branch predates this story's baseline and would require changing the retained correction-start semantics; it will be recorded for follow-up if it survives re-derivation.
  - `[false]` `[reject]` Correction was said to hide a required second ten-fact preview — the panel already presents its correction consequence preview, and the BFF object is a final preflight evidence model; neither the intent nor existing correction contract requires a second confirmation surface.
  - `[medium]` `[bad_spec]` Correction remove availability used snapshot submit state instead of concrete BFF preview readiness — a live Confirm button could no-op when composition support was absent; concrete capability is now required in evidence.
  - `[medium]` `[bad_spec]` Correction removal preview composition occurred outside exception containment — resolver/composition failure could escape the event callback; fail-closed handling is now explicit.
  - `[medium]` `[bad_spec]` Shared dispatch connectivity depended on tracked grant support — independent tracked-remove deployments were incorrectly blocked before their own capability gate; base and action-specific capabilities are now separated.
  - `[false]` `[reject]` Preview matching was said to ignore mutable complete fact keys — production `Create` emits constant keys for every complete preview and checks completeness plus the only variable caller-context key, so the alleged changed complete fact set is unreachable.
  - `[medium]` `[bad_spec]` Whitespace-bearing literal targets were visually collapsed while exact acknowledgement remained ordinal — the validator and gateway intentionally preserve those identifiers, so the dialog must expose a usable exact-value path.
  - `[medium]` `[bad_spec]` Edge review independently confirmed correlationless removal adoption failure — the admission-gate switch proves the same missing valid ambiguous shape; the reconciliation repair constraint covers it.
  - `[medium]` `[bad_spec]` Edge review independently confirmed notification-driven ambiguous redispatch — the notification call path reaches tracked removal dispatch; the nudge-only repair constraint covers it.
  - `[medium]` `[bad_spec]` Edge review independently confirmed unhandled correction composition failure — the awaited composition precedes the method's catch block; correction exception containment is now explicit.
  - `[medium]` `[bad_spec]` Viewport could become unsafe while removal preview composition awaited and the modal still opened — the final renderer callback did not re-run availability; post-await re-evaluation and associated copy are now required.
  - `[medium]` `[bad_spec]` Page ambiguous refresh lacked a single-flight guard — overlapping activations could cancel and resend the same transport operation concurrently; an atomic operator-retry gate is now required.
  - `[medium]` `[bad_spec]` Exact ordinal acknowledgement lacked negative verification — every test helper entered the exact value, so case-insensitive or nonblank acceptance could survive; case-variant and partial no-dispatch tests are now named.
  - `[medium]` `[bad_spec]` Removal modal isolation/focus lifecycle lacked executable assertions — existing tests checked modal attributes and sentinels but not inert background, initial/sentinel focus, or exact launcher restoration; those observations are now required.
  - `[medium]` `[bad_spec]` Page multi-page removal reconciliation lacked an executed component scenario — loader unit coverage and a one-page page test would not catch replacing the complete walk with page one; a two-page page-level proof is now required.
  - `[medium]` `[bad_spec]` Same-ID ambiguous removal retry lacked page and correction execution — snapshots retained the ID but no consumer test proved the resend reused it; both consumers are now named verification targets.
  - `[medium]` `[bad_spec]` Page status lookup did not assert the fixed aggregate handle — its stub fabricated verified identity for any handle; the exact message/correlation/aggregate tuple is now required.
  - `[false]` `[reject]` Intent audit said the authoritative hard stop was unverified — unchanged `GlobalAdministratorsAggregateTests` already execute the one-administrator `LastGlobalAdministratorRejection`, and the independently run full server lane passed all 759 tests including it.

### 2026-09-01 — Review pass
- verdicts: 38 findings — high 8, medium 29, low 0, false 1, maybe-false 0
- findings:
  - `[medium]` `[bad_spec]` Page retry acquired single-flight after reauthorization and generation invalidation — an overlapping click could cancel the active retry and then reject itself; iteration-2 retry-ownership constraints require winning single-flight first.
  - `[medium]` `[bad_spec]` An ignored second preview click changed the stored launcher target — target assignment preceded the preview gate, so focus could restore to the wrong row; iteration 2 moves capture after gate acquisition.
  - `[medium]` `[bad_spec]` Removal cancellation focused a wrapper span rather than the exact interactive launcher — the stored `ElementReference` belonged to `span tabindex=-1`; iteration 2 requires the actual button.
  - `[medium]` `[bad_spec]` The end sentinel focused the submit wrapper and skipped the acknowledgement input — the implemented loop did not cycle through the first interactive control; iteration 2 names both sentinel destinations.
  - `[high]` `[bad_spec]` Page ambiguous retry invalidation stranded a dispatched aggregate lease — `UnableToVerify` with no correlation was neither retainable nor terminal; iteration 2 preserves the correlationless ambiguous `RequestSent` shape.
  - `[high]` `[bad_spec]` Submit and retry ignored authorization-unavailable BFF preview evidence — a permission transition between reauthorization and composition could leave privileged rows visible; iteration 2 requires collapse at every composition boundary.
  - `[medium]` `[bad_spec]` Final page preflight discarded the live availability reason — a complete rebuilt preview plus unsafe viewport/capability rendered generic invalidation copy; iteration 2 requires the associated reason and recovery.
  - `[high]` `[bad_spec]` Correction preview composition could prove missing permission without collapsing the panel — the result was treated as ordinary unavailable evidence; iteration 2 extends authoritative collapse to correction.
  - `[medium]` `[bad_spec]` A null correction projection silently left the preview re-armed — repeated Confirm clicks could no-op without explanation; iteration 2 requires an explicit localized unable-to-verify state.
  - `[medium]` `[bad_spec]` New removal-preview block keys were absent from correction live recovery — later parent projection refreshes could not recover target/evidence/last-administrator blocks; iteration 2 enumerates them.
  - `[medium]` `[bad_spec]` Correction target rendering collapsed significant whitespace — ordinal identifiers with leading/trailing spaces were not visibly distinguishable; iteration 2 requires literal whitespace preservation.
  - `[medium]` `[bad_spec]` Correction ambiguous tracking mismatch used grant message keys — removal displayed the wrong lifecycle explanation; iteration 2 requires action-specific keys.
  - `[medium]` `[bad_spec]` Correction unable-to-verify hard-coded grant recovery — removal verification failures offered the wrong recovery path; iteration 2 requires removal-specific recovery.
  - `[medium]` `[bad_spec]` Correction rendered raw status/submission prose — configured French UI could expose English gateway strings; iteration 2 requires resource keys only.
  - `[medium]` `[bad_spec]` Removal localization readiness omitted reachable fail-closed keys — readiness could be true while an unavailable path rendered an unresolved key; iteration 2 expands the complete key contract.
  - `[medium]` `[bad_spec]` Exact-target and acknowledgement resources were not checked for `{0}` — localized copy could omit the literal value the operator must type; iteration 2 requires placeholder validation.
  - `[high]` `[bad_spec]` Removal submission copied unsupported lifecycle states such as `AlreadyApplied` or `Confirmed` — an alternate gateway could release the lease and claim a forbidden outcome; iteration 2 fail-closes every unsupported state.
  - `[medium]` `[bad_spec]` Availability accepted identifiers longer than the downstream 256-character contract — a live launcher could lead only to preview rejection; iteration 2 makes the identity rule uniform.
  - `[medium]` `[bad_spec]` Page status polling re-queried projection for `Received`/`Processing` — `ConfirmProjection` then converted normal pre-event acceptance into unable-to-verify; iteration 2 permits requery only after positive event evidence.
  - `[medium]` `[bad_spec]` Edge tracing independently confirmed the retry single-flight cancellation race — the second generation superseded the first before losing the atomic gate; covered by the iteration-2 ownership amendment.
  - `[high]` `[bad_spec]` Edge tracing independently confirmed page correlationless preflight lease loss — the invalidated state could not be retained during disposal; covered by the iteration-2 ambiguous-shape amendment.
  - `[high]` `[bad_spec]` Correction ambiguous preflight invalidation likewise stranded its dispatched lease — `ToReconciliation` rejected the correlationless `UnableToVerify` state; iteration 2 applies the same retained shape to correction.
  - `[medium]` `[bad_spec]` An obsolete removal projection read could collapse a newer authorized surface — requery did not re-check its generation after the awaited complete read; iteration 2 requires a post-await generation guard.
  - `[medium]` `[bad_spec]` Launcher disappearance had no focus fallback — notification-driven row replacement could leave cancellation without a focus target; iteration 2 requires lifecycle fallback.
  - `[medium]` `[bad_spec]` Edge tracing independently confirmed generic final-preflight recovery — stored invalidation copy did not identify the current unsafe gate; covered by the associated-failure amendment.
  - `[high]` `[bad_spec]` Edge tracing independently confirmed unsupported page submission states could bypass causal proof — arbitrary state copying allowed terminal success semantics; covered by fail-closed submission-state constraints.
  - `[high]` `[bad_spec]` Correction submission failure also copied arbitrary lifecycle states — a custom result could confirm or release without projection proof; iteration 2 applies fail-closed state handling to both consumers.
  - `[medium]` `[bad_spec]` Edge tracing independently confirmed grant recovery keys on removal correction — tracking mismatch and zero-event paths selected the wrong action resources; covered by removal-specific localization constraints.
  - `[medium]` `[bad_spec]` Edge tracing independently confirmed raw English correction lifecycle text — rejection, publish failure, timeout, and unavailable status retained gateway prose; covered by resource-key-only presentation.
  - `[medium]` `[bad_spec]` The 256-character removal boundary lacked 256/257 executable cases — deleting either validator or gateway limit would leave existing tests green; iteration 2 names both boundaries and discriminators.
  - `[medium]` `[bad_spec]` Tracked-remove capability absence lacked negative availability and page tests — the new independent gate could regress without detection; iteration 2 requires both observations.
  - `[medium]` `[bad_spec]` Preview safety changing during asynchronous composition was untested — existing viewport coverage changed only after the dialog opened; iteration 2 requires a suspended-composition race.
  - `[medium]` `[bad_spec]` Ambiguous retry tests never changed retained preview evidence — both page and correction could lose their no-redispatch guard unnoticed; iteration 2 requires unsafe rebuilt-preview cases.
  - `[medium]` `[bad_spec]` Page correlationless replacement adoption was untested — gate and correction tests did not execute the page restore seam; iteration 2 requires same-id/preview/lock recovery with no automatic dispatch.
  - `[medium]` `[bad_spec]` Correction removal composition exception containment was untested — removing the catch would not fail current tests; iteration 2 requires exception, localization, and no-dispatch assertions.
  - `[medium]` `[bad_spec]` Operation-time authoritative denial was untested across removal preview/submit/retry/requery — simple returns could preserve privileged markup; iteration 2 requires collapse and lease assertions.
  - `[medium]` `[bad_spec]` Removal `PublishFailed` and `TimedOut` mappings lacked state/recovery execution — grant tests did not protect removal-specific behavior; iteration 2 requires snapshot and rendered recovery assertions.
  - `[false]` `[reject]` Intent alignment reported no new aggregate hard-stop test — the aggregate is intentionally unchanged and the independently executed 758-test server lane includes the existing one-administrator `LastGlobalAdministratorRejection`, so authoritative behavior is already exercised.

## Design Notes

Removal is intentionally a causal proof pipeline rather than an absence check:

`immutable complete preview -> retained ULID + aggregate lease -> verified command events -> complete authoritative absence + advanced version -> confirmed`

Any missing link remains pending/rejected/unable-to-verify. The generic API validator protects the fixed actor boundary; the existing aggregate remains the atomic last-administrator authority.

## Verification

**Commands:**
- `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Release -warnaserror -m:1 -nr:false` -- expected: validator, aggregate, projection/query, and server regressions pass warning-clean.
- `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Release -warnaserror -m:1 -nr:false --filter 'Category!=Performance'` -- expected: fixed-route rejection and command integration scenarios pass when DAPR prerequisites are available.
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release -warnaserror -m:1 -nr:false` -- expected: warning-clean UI test assembly.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -class Hexalith.Tenants.UI.Tests.State.GlobalAdministratorRemoveCommandSnapshotTests -class Hexalith.Tenants.UI.Tests.State.GlobalAdministratorCorrectionSnapshotTests -class Hexalith.Tenants.UI.Tests.State.TenantAggregateCommandAdmissionGateTests -class Hexalith.Tenants.UI.Tests.Services.Gateways.TenantCommandGatewayTests -class Hexalith.Tenants.UI.Tests.Services.Gateways.TenantsBffCompositionTests -class Hexalith.Tenants.UI.Tests.Components.GlobalAdministratorsPageTests -class Hexalith.Tenants.UI.Tests.Components.GlobalAdministratorCorrectionPanelTests` -- expected: focused lifecycle matrix passes with no failed, skipped, or not-run tests.
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-build --no-restore` -- expected: full UI suite passes.
- `dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false` -- expected: full canonical solution build passes warning-clean without dependency overrides.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md` -- expected: no undeclared gitlink movement.
- `git diff --check && git diff --cached --check` -- expected: no whitespace errors.
