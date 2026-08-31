---
title: '4.1 Fixed-Scope Global Administrator Action Availability'
type: 'feature'
created: '2026-08-28'
status: done
baseline_commit: 'c69cdfd58a876648707782a028e1352be1e278cb'
baseline_revision: 34e4b7ac846aff2dd0a4e9cb7346038aa7705cd5
review_loop_iteration: 6
followup_review_recommended: false
context:
  - '{project-root}/_bmad-output/project-context.md'
warnings:
  - oversized
deferred:
  - summary: >-
      Global-administrator command retries do not retain a caller-owned message id across ambiguous transport failures.
    evidence: |-
      The existing gateway creates the message id inside SetGlobalAdministratorAsync/RemoveGlobalAdministratorAsync and catches only EventStoreGatewayException, so timeout/HttpRequestException retry semantics belong to the downstream command-lifecycle stories rather than this availability-only story.
    location: >-
      src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs:329
    severity: high
  - summary: >-
      Existing grant/remove projection confirmation does not require baseline projection-version advancement or command-specific audit provenance.
    evidence: |-
      The historical downstream command snapshots can accept qualifying target presence/absence without comparing the pre-command projection version; Story 4.1 dispatches no command and owns availability, so confirmation hardening remains follow-up work for the grant/remove lifecycle owners.
    location: >-
      src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorGrantCommandSnapshot.cs:150
    severity: high
  - summary: >-
      A contradictory complete-read authorization-scope flag is not revalidated by the pure removal evaluator.
    evidence: |-
      GlobalAdministratorActionAvailabilityEvaluator accepts CompleteKind Ready with non-empty complete rows without checking CompleteIsAuthorizationScopedEmpty. The bounded page loader rejects that shape and both installed production callers currently supply internally consistent snapshots, so the defect predates and is not caused by this re-drive's action-specific readiness change; a direct public evaluator caller could still construct the contradictory evidence and receive an available removal result.
    location: >-
      src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorActionAvailabilityEvaluator.cs:46
    severity: medium
  - summary: >-
      Repeated availability evaluation during one Razor render could theoretically produce mismatched reason and recovery associations.
    evidence: |-
      Grant and removal availability are recomputed independently for visible copy, disabled state, and aria-describedby. Blazor serializes renderer callbacks, but the admission gate's state can change before its notification callback is rendered; a deterministic test that changes gate or viewport evidence between those synchronous property reads is needed to establish whether an inconsistent render is reachable. The evaluation pattern predates this re-drive.
    location: >-
      src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:821
---

<intent-contract>

## Intent

**Problem:** The existing Global Administrators page uses page-local string ternaries and CSS-only hiding to decide whether grant and remove actions are available. It can treat a paged result as sufficient removal evidence and cannot prove aggregate admission, measured viewport safety, preview readiness, or complete lifecycle support.

**Approach:** Add a pure, typed fixed-scope availability guardrail that evaluates grant and each visible removal independently from server-reflected authority, qualified direct-read evidence, explicit lifecycle capabilities, complete population evidence, fixed-aggregate admission, target visibility, action-specific downstream preview readiness, and measured viewport safety. Render canonical localized reasons and recoveries while retaining the existing fixed routes and downstream command flows. Story 4.1 is a read-only consumer of preview/lifecycle-capability and fixed-aggregate-admission evidence: it may block an entry point when its downstream preview is unavailable or the existing downstream lifecycle owner reports an active attempt, but it does not build a preview or own or mutate any command lifecycle.

## Boundaries & Constraints

**Always:** Fix the identity to `system / global-administrators / global-administrators`; keep API/domain authorization authoritative; require current projection freshness, current direct-read lifecycle, non-empty projection version, and the evaluated action's downstream preview readiness; require authoritative complete population evidence before removal; consume the circuit-scoped fixed-aggregate admission state as immutable availability evidence; preserve authorized read-only rows during degraded states; use FrontComposer/Fluent UI V5, whole-string EN/FR resources, stable selectors, and programmatic reason/recovery associations.

**Block If:** A qualifying global-administrator lifecycle capability cannot be represented without changing a public package contract outside this repository, or existing shared admission/viewport primitives cannot expose the fixed-aggregate and measured-viewport evidence required by availability without an architecture change. Do not resolve such a block by adding a second lifecycle owner in this story.

**Never:** Infer authority or totals client-side; use `HasMore`, cursor history, page count, `ServedAt`, client time, SignalR, tenant freshness, or CSS visibility as mutation evidence; derive scope from a tenant or row; disclose administrator data to unauthorized/indeterminate callers; dispatch from the evaluator; acquire, mark, retain, adopt, advance, resume, refresh, or release a command lease; store command tracking; poll status; perform post-command projection/audit reconciliation or renderer-loss recovery; alter `GlobalAdministratorCorrectionPanel` post-dispatch behavior; add an endpoint; weaken the last-administrator domain guard; modify `sprint-status.yaml`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Eligible grant | Authorized; qualified current direct read; command-support capability, grant-preview readiness, admission, and safe viewport ready | Grant available with fixed platform scope; no target-existence inference | Evaluator remains side-effect free and only hands off to the downstream preview |
| Eligible remove | Same evidence plus remove-preview readiness, visible target, and complete authoritative count greater than one | That row alone may open the dedicated preview | Recheck all availability evidence before preview handoff; Story 4.1 does not submit |
| Incomplete population | Paged, bounded, mixed-version, capped, recovered, or otherwise incomplete read | Every remove is unavailable with count-incomplete reason and recovery | Never promote `HasMore` or page rows to total evidence |
| Last administrator | Complete authoritative count equals one | Remove unavailable before preview with localized last-admin reason | Domain still rejects a race with `LastGlobalAdministrator` |
| Missing support or lock | Command/status/requery/preview support missing, or the existing lifecycle owner reports the fixed aggregate active | All affected entry points unavailable with canonical lifecycle/high-impact reason | Observe only; Story 4.1 neither acquires nor releases the lock, and unrelated tenant aggregates remain usable |
| Unsafe evidence | Authority indeterminate; stale/unknown provenance; target missing; viewport unknown/phone | Affected action fails closed with visible reason and recovery | Retain only authorized last-confirmed read data; disclose nothing when unauthorized |

</intent-contract>

## Code Map

- `src/Hexalith.Tenants.Contracts/Identity/TenantIdentity.cs:21` -- read-only canonical fixed identity via `ForGlobalAdministrators()`.
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorsSnapshot.cs:156` -- current mutation-evidence predicate lacks projection-version and completeness semantics; preserve support-safe diagnostics.
- `src/Hexalith.Tenants.UI/Services/Gateways/GlobalAdministratorsProjectionLoader.cs:23` -- reuse bounded, cursor-safe, projection-version-consistent complete-population loading.
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantAggregateCommandAdmissionGate.cs:12` and `State/TenantCommands/TenantCommandAggregateLock.cs:23` -- consume existing circuit-scoped busy/available evidence and the fixed global-administrator key without changing lease lifecycle semantics.
- `src/Hexalith.Tenants.UI/State/TenantHighImpactViewportObservation.cs:8` -- reuse measured Unknown/Phone fail-closed viewport evidence.
- `src/Hexalith.Tenants.UI/State/TenantDetail/TenantHighImpactActionAvailabilityEvaluator.cs:17` -- reuse the pure evaluator/result pattern and precedence, not tenant-specific wording.
- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:768` -- replace ad hoc grant/remove ternaries; `HasPositiveRemovalPopulationEvidence` at line 831 is the known-bad incomplete-count bypass.
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor:310` -- downstream Story 5.7 lifecycle owner; do not modify its dispatch, lease, reconciliation, proof, renderer-loss, or reconnect behavior in Story 4.1.
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs:6` and `Abstractions/ITenantsBffComposition.cs:11` -- expose explicit global command/status/requery support only as narrowly required.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` and `TenantsResources.fr.resx` -- canonical reason/recovery copy with exact EN/FR parity.
- `tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs:1156` -- reverse the historical test that enables remove from `HasMore`; retain authorization, fixed-scope, freshness, last-admin, and support-safe coverage.
- `tests/Hexalith.Tenants.UI.Tests/State/TenantHighImpactActionAvailabilityTests.cs:21` -- reference matrix/purity/precedence test style for the new global evaluator.

## File List

- `_bmad-output/implementation-artifacts/spec-4-1-fixed-scope-global-administrator-action-availability.md`
- `references/Hexalith.FrontComposer`
- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionDecision.cs`
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantsBffComposition.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs`
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorActionAvailabilityEvaluator.cs`
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorActionEvidence.cs`
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorActionUnavailableReason.cs`
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantAggregateCommandAdmissionGate.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorCorrectionPanelTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/GlobalAdministratorsProjectionLoaderTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantsBffCompositionTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/State/GlobalAdministratorActionAvailabilityTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/State/TenantAggregateCommandAdmissionGateTests.cs`

The baseline-to-current range includes the prior review-loop implementation and the already-committed umbrella dependency-alignment move for `references/Hexalith.FrontComposer`. This re-drive preserved that root pointer and did not modify submodule content. The human escalation resolution supersedes the earlier lifecycle-ownership requirements; the current implementation delta does not alter the correction panel or admission-gate production behavior.

## Tasks & Acceptance

**Execution:**
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorActionEvidence.cs`, `GlobalAdministratorActionAvailability.cs`, and `GlobalAdministratorActionAvailabilityEvaluator.cs` -- add one-type-per-file immutable evidence/results and a pure independent grant/remove evaluator with canonical reason precedence; carry `HasMeasurement` separately from viewport state; require authorization-scoped, rowless `Empty`; carry distinct grant-preview and remove-preview readiness evidence and fail the evaluated action closed when its downstream preview is unavailable; evaluate remove-preview readiness only after complete-population, target-presence, and last-administrator evidence have selected any more specific safety reason; fail invalid/duplicate identity evidence closed; override diagnostics so UserId/projection metadata cannot appear.
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs`, `TenantCommandGateway.cs`, `UnavailableTenantCommandGateway.cs`, `Abstractions/ITenantsBffComposition.cs`, and `Services/TenantsBffComposition.cs` -- represent fixed-scope dispatch/status/requery capability independently, map each production flag only to its corresponding seam, and fail closed when any required capability is absent.
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantAggregateCommandAdmissionGate.cs` and `State/TenantCommands/TenantCommandAggregateLock.cs` -- use the existing read-only busy/available observation and fixed global-administrator key as evaluator input. Do not add or change acquisition, dispatch marking, retention, adoption, advancement, reconciliation, terminal release, notification ordering, or lifecycle-regression semantics; those belong to the existing downstream command owners.
- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor` -- obtain a separate bounded complete-population snapshot through `GlobalAdministratorsProjectionLoader` without replacing Story 1.11's paged review rows; collapse the whole privileged surface if that walk returns Unauthorized; require current row freshness/lifecycle and the same stable projection version for visible and complete evidence; evaluate/re-evaluate availability; capture preview rows, freshness, lifecycle, and identity set together and refuse an unsafe handoff; derive unique DOM associations from rendered row ordinal and associate unavailable row slots as well as rendered controls; pair each reason with its own recovery; react live to viewport/admission/authentication changes; remove the `HasMore` bypass. Evidence selectors must render localized, visibly labelled, qualified scope/freshness/count/admission values and must report missing gate/evidence as unknown rather than available. Once an eligible entry point hands control to an existing preview/command flow, Story 4.1 owns no subsequent dispatch or lifecycle state.
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor` -- make no Story 4.1 changes. Story 5.7 retains ownership of correction eligibility beyond the reused availability decision, preview, dispatch, lease/reconciliation state, status/projection/audit proof, renderer loss, disposal, and reconnect behavior.
- `src/Hexalith.Tenants.UI/Services/Gateways/GlobalAdministratorsProjectionLoader.cs` -- reject null rows, contradictory Empty/Ready page shapes, oversized pages, mismatched request cursor/page size, duplicate/invalid identities, mixed versions, missing cursors, repeated cursors, and noncurrent lifecycle as incomplete evidence; never normalize contradictory evidence into a successful shape. Any later-page Unauthorized result collapses all accumulated rows and provenance to the canonical unauthorized result.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`, `TenantsResources.fr.resx`, and `Components/Pages/GlobalAdministratorsPage.razor.css` -- add parity-checked whole strings and reflect measured responsive state without making CSS an evidence source. Use Fluent UI V5 text components where available; do not add raw paragraph markup for reason/recovery copy.
- `tests/Hexalith.Tenants.UI.Tests/State/GlobalAdministratorActionAvailabilityTests.cs`, `Components/GlobalAdministratorsPageTests.cs`, and focused loader/gateway/composition/localization tests -- use PascalCase for every new or renamed method and cover every matrix row/reason precedence; unmeasured-safe viewport; authorization-scoped Empty; multi-page complete loading plus null/contradictory/oversized/wrong-request pages with canonical fail-closed result shape and bounded call count; later-page authorization collapse with no retained identity; lifecycle/version-qualified rows; viewport/auth/admission transitions and denied-to-authorized recovery after render; composition and gateway dispatch/status/requery capabilities independently for both grant and remove; localized labelled evidence values and every unavailable branch; grant, preview-entry, and unique per-row reason/recovery ARIA references with asserted non-empty text; last-administrator precedence over missing-preview copy; canonical reason vocabulary/selectors; evaluator purity/support-safe diagnostics; the all-safe-but-action-preview-missing evaluator case for both grant and remove; and each complete-population prerequisite (invalid/duplicate rows, surface kind, freshness, lifecycle, blank version, and version mismatch) taking precedence over missing remove preview. Assert that availability evaluation and blocked entry points dispatch no command. Do not add Story 4.1 tests for lease mutation, retained adoption, status/projection/audit reconciliation, correction-panel lifecycle, renderer loss, disposal, reconnect, or terminal release.

**Acceptance Criteria:**
- Given any authority, direct-read provenance, lifecycle support, admission, target, count, action-specific downstream preview, or viewport input is unknown or unsafe, when availability is evaluated, then only the affected action fails closed with a canonical localized reason and recovery and no dispatch occurs.
- Given the grant preview or remove preview required by its downstream command story is unavailable, when that action's availability is evaluated, then that action is unavailable with the canonical missing-preview reason and recovery; Story 4.1 neither builds the preview nor permits a bypass to submission.
- Given fixed-scope read evidence is paged, bounded, recovered, mixed-version, or incomplete, when any removal row renders, then every remove is unavailable and neither `HasMore` nor client enumeration can open preview.
- Given complete current evidence proves one administrator, when its row renders, then last-administrator removal is visibly unavailable before preview; given more than one and every other gate passes, only the visible eligible target may open preview.
- Given the existing downstream lifecycle owner reports one global-administrator attempt active in the interactive circuit, when another grant or removal entry point evaluates, then all fixed-aggregate mutations are unavailable while unrelated tenant aggregate locks remain independent; Story 4.1 only consumes this busy state and neither determines terminality nor acquires, adopts, advances, reconciles, or releases the lock.
- Given authorization becomes missing or indeterminate, when the route or action slot re-renders, then no identity, count, fixed-scope detail, action, route detail, or retained privileged state is observable and server enforcement remains unchanged.
- Given keyboard, screen-reader, forced-colors, reduced-motion, desktop/tablet, and unknown/phone viewport use, when actions render, then visible hover-independent reasons and recoveries are programmatically associated, stable selectors expose scope/freshness/count/lock state, and unsafe widths remain read-only.
- Given English and French resources and focused tests run, when the corrected guardrail is verified, then localization parity, fixed routing, support safety, complete-count refusal, last-admin protection, target visibility, aggregate admission, responsive failure, and no accidental submission all pass.

## Spec Change Log

### 2026-08-28 — Review loop 1
- Trigger: the first implementation could never make a population larger than one page removable, accepted current-freshness rows with unknown lifecycle, proxied rather than validated preview readiness, and released or failed to publish the fixed-aggregate lock before terminal evidence.
- Amendment: execution tasks now require a separate bounded complete-projection load, lifecycle-qualified/valid population evidence, submit-time preview-to-current-evidence matching, ownership-safe circuit leases with live notifications, explicit correction requery/viewport/recovery gates, and transition/cross-surface/accessibility tests.
- Known-bad state avoided: `HasMore` remains blocked without making large valid populations permanently read-only; nonterminal or disposed attempts cannot unlock or be overwritten by a second owner; stale previews and unsafe row provenance cannot dispatch; assistive associations and live viewport/lock changes cannot silently regress.
- KEEP: preserve the pure typed evaluator, fixed `system/global-administrators/global-administrators` identity, explicit independent capability seams, measured viewport observation, canonical EN/FR reason/recovery resources, stable evidence selectors, complete-count refusal, server/domain enforcement boundaries, and warning-clean test/build evidence from the first derivation.

### 2026-08-28 — Review loop 2
- Trigger: the second implementation left privileged rows visible when the complete walk returned Unauthorized, retained leases while discarding their only reconciliation state, treated seeded Safe as measured, normalized contradictory loader pages, allowed unsafe lease release/subscriber failure, and lacked actual-command cross-surface verification.
- Amendment: tasks now require full authorization collapse, measurement provenance, visible/complete projection-version equality, immutable preview evidence, preserved reconciliation state, explicit terminal lease APIs, exception-isolated notifications, fail-closed page-shape/request validation, fresh correction authorization, paired reason/recovery rendering, and actual submitted-command integration tests.
- Known-bad state avoided: an operator cannot retain privileged data after background denial; a circuit cannot become permanently and invisibly locked; an unmeasured viewport or contradictory page cannot enable mutation; a throwing observer cannot orphan a lease; cancellation, close, or renderer races cannot dispatch without ownership or lose the path to terminal release.
- KEEP: preserve loop 1 KEEP constraints plus the separate complete-population walk, live viewport/admission updates, lifecycle-qualified rows, unique ARIA association design, independent capability seams, ownership-token leases, and all passing first-repair tests that remain valid.

### 2026-08-28 — Review loop 3
- Trigger: the third implementation retained command tracking without giving a replacement surface an exclusive adoption/resumption path, allowed authorization and refresh completions to arrive out of order, left retain/release and lifecycle advancement races in the lease, and rendered or tested several evidence/recovery states only structurally rather than semantically.
- Amendment: execution tasks now require adoptable single-owner reconciliation through terminal evidence, deadlock-safe atomic and monotonic lease transitions, generation-guarded authorization/refresh/submit work, explicit capability and live-authentication requirements, immediate renderer-independent tracking retention, canonical later-page authorization collapse, localized labelled evidence, complete reason/recovery associations, and real cross-surface/recovery/race tests.
- Known-bad state avoided: navigation or authorization collapse cannot strand an accepted fixed-scope command forever; stale authorization cannot redisclose privileged content; a late refresh cannot overwrite newer lifecycle evidence; released leases cannot accept retained state; evidence selectors cannot claim availability from absent gates or unqualified counts; assistive copy cannot be present but empty, mismatched, or unassociated.
- KEEP: preserve the pure evaluator and typed evidence, separate bounded complete-population loader, fixed identity and independent capability seams, measured viewport provenance, qualified visible/complete projection-version matching, support-safe reconciliation diagnostics, ownership-safe pre-dispatch abandonment and terminal-only release, full authorization collapse, canonical EN/FR reason vocabulary, and every previously passing matrix/localization/build test that remains valid.

### 2026-08-30 — Review loop 4
- Trigger: the fourth implementation still allowed confirmation without a concrete requery provider, let hidden unauthorized correction instances strand adopted reconciliation, failed to recheck viewport/admission evidence at the dispatch and refresh boundaries, selected missing-preview copy ahead of last-administrator truth, and left disabled confirmation associations and independent gateway capabilities incompletely verified.
- Amendment: the correction task now requires both declared and concrete lifecycle seams, retained ownership to remain adoptable until fresh authorization, boundary-time live-gate rechecks, one disabled-decision reason/recovery association, and explicit gateway/auth/viewport/admission/race tests; the evaluator task now fixes last-administrator reason precedence.
- Known-bad state avoided: an accepted command cannot lock the aggregate without a reconciliation path; an indeterminate hidden surface cannot monopolize retained work; a viewport or admission change cannot race into command or refresh I/O; last-administrator refusal cannot be mislabeled as a missing preview; disabled controls cannot lose their assistive explanation.
- KEEP: preserve all loop 1-3 KEEP constraints, the correct fail-closed gateway capability operands, missing/throwing-provider collapse, renderer marshalling, complete existing focused/full-suite evidence, and the unrelated repository gitlink state without reverting or claiming those pointers as story changes.

### 2026-08-30 — Review loop 5
- Trigger: the fifth review found that unfiltered retained-work adoption, intent/support invalidation, and renderer-dependent completion could strand a fixed-aggregate lease; unavailable query gateways could masquerade as concrete requery support; disabled decisions and recovery copy could diverge; and a corrective-proof lookup failure could overwrite confirmed command truth.
- Amendment: the correction task now requires usable requery seams, matching-only adoption with retry, durable retention across every invalidation and renderer-loss path, one immutable whitespace-safe decision with state-specific recovery, stable submission reset, and proof-query failure isolation. The test task now names the exact dispatch/status/projection suspension boundaries, retained-work handoffs, unavailable-gateway paths, evaluator precedence branches, and terminal/recovery states that must be demonstrated.
- Known-bad state avoided: a mismatching or disappearing surface cannot steal retained work; a fail-closed gateway cannot enable mutation; accepted tracking survives renderer loss and evidence changes; confirmed projection truth survives delayed audit proof; and a disabled action cannot expose blank, stale, or contradictory assistive guidance.
- KEEP: preserve all loop 1-4 KEEP constraints, evaluator last-administrator precedence, live renderer-boundary checks, no pre-command projection fallback, independent gateway capabilities, authorization collapse, warning-clean focused/full-suite evidence, and unrelated repository and orchestrator-owned files exactly as found.

### 2026-08-31 — Human escalation resolution
- Trigger: after five repair loops, Story 4.1's availability contract had accumulated a second command-lifecycle architecture in the admission gate and correction panel, contradicting the upstream requirement that Story 4.1 dispatch no command and retain downstream lifecycle ownership.
- Decision: restore Story 4.1 to availability-only. It consumes immutable action-specific preview, lifecycle-capability, and fixed-aggregate busy/available evidence, computes and presents eligibility, and refuses unsafe entry; both grant and remove fail closed when their downstream preview is unavailable. Stories 4.2 and 4.3 retain grant/remove preview, dispatch, and lifecycle ownership, and Story 5.7 retains correction preview, dispatch, reconciliation, proof, renderer-loss, and reconnect ownership.
- Supersession: any earlier review-loop amendment or triage finding that requires Story 4.1 to acquire, mutate, retain, adopt, advance, reconcile, or release a lease; store command tracking; poll status; alter correction-panel post-dispatch behavior; or recover after renderer loss is historical provenance, not an implementation requirement for this re-drive.
- KEEP: preserve the pure typed evaluator, fixed identity, qualified direct-read and complete-population evidence, action-specific preview precedence, authorization isolation, read-only admission/capability observation, measured viewport safety, localized visible reason/recovery associations, and no-dispatch availability verification.

## Review Triage Log

### 2026-08-28 — Review pass
- intent_gap: 0
- bad_spec: 13: (high 9, medium 4, low 0)
- patch: 0
- defer: 2: (high 2, medium 0, low 0)
- reject: 3: (high 0, medium 2, low 1)
- addressed_findings:
  - `[high]` `[bad_spec]` Require the existing bounded projection loader so multi-page populations can produce complete removal evidence without treating `HasMore` as proof.
  - `[high]` `[bad_spec]` Require row freshness and lifecycle, stable projection provenance, invalid-row refusal, and duplicate-safe complete counts.
  - `[high]` `[bad_spec]` Replace preview proxies with real action-specific readiness and reject submit when preview evidence no longer matches the current complete projection.
  - `[high]` `[bad_spec]` Retain an ownership-safe circuit lease through every nonterminal, refresh, cancel-after-dispatch, close, and disposal path.
  - `[high]` `[bad_spec]` Make page and correction consumers react to live admission changes and verify both ownership directions through terminal release.
  - `[high]` `[bad_spec]` Require correction requery capability and measured safe viewport, with visible associated recovery, before confirmation.
  - `[high]` `[bad_spec]` Verify viewport transitions after render and independent production capability mapping.
  - `[medium]` `[bad_spec]` Verify unique per-row reason/recovery ARIA associations and support-safe evidence diagnostics.

### 2026-08-28 — Review pass 2
- intent_gap: 0
- bad_spec: 16: (high 12, medium 4, low 0)
- patch: 0
- defer: 0
- reject: 5: (high 0, medium 3, low 2)
- addressed_findings:
  - `[high]` `[bad_spec]` Collapse the privileged page when complete-population authorization fails and reauthorize correction status/requery paths.
  - `[high]` `[bad_spec]` Preserve reconciliation state whenever a dispatched lease remains held; prevent cancel, close, or disposal from creating an unrecoverable circuit lock.
  - `[high]` `[bad_spec]` Carry actual viewport-measurement provenance and require it on every high-impact consumer.
  - `[high]` `[bad_spec]` Fail contradictory Empty/Ready shapes, wrong request cursors/page sizes, oversized/null rows, and version-mismatched visible/complete evidence closed.
  - `[high]` `[bad_spec]` Make lease acquisition/release atomic, exception-isolated, dispatch-aware, and terminal-state validated.
  - `[high]` `[bad_spec]` Marshal correction state after awaits and pair its reason and recovery from one decision.
  - `[high]` `[bad_spec]` Verify actual submitted page/correction commands hold the sibling surface closed through terminal evidence.
  - `[medium]` `[bad_spec]` Require unique ordinal DOM associations and test grant/preview/per-row ARIA references, canonical reason vocabulary, and stable evidence selectors.
  - `[medium]` `[bad_spec]` Enforce PascalCase test names and isolate every lifecycle capability in evaluator and component tests.

### 2026-08-28 — Review pass 3
- intent_gap: 0
- bad_spec: 15: (high 9, medium 6, low 0)
- patch: 0
- defer: 0
- reject: 2: (high 0, medium 1, low 1)
- addressed_findings:
  - `[high]` `[bad_spec]` Make retained fixed-scope reconciliation exclusively adoptable by a replacement surface and prove real page-to-correction and correction-to-page completion through terminal release.
  - `[high]` `[bad_spec]` Generation-guard authorization, refresh, and submit work so out-of-order results cannot redisclose privileged content or overwrite newer evidence.
  - `[high]` `[bad_spec]` Serialize correction refresh entry and map missing providers, unsupported composition capabilities, and resolver failures to a recoverable fail-closed state.
  - `[high]` `[bad_spec]` Retain accepted tracking before renderer work and preserve a resumable owner across disposal, close, cancellation, and authorization collapse.
  - `[high]` `[bad_spec]` Make retain/adopt/release atomic under a deadlock-safe order and reject lifecycle regression or retention after terminal release.
  - `[high]` `[bad_spec]` Collapse accumulated complete-population rows when any later page returns Unauthorized and verify the canonical redacted result.
  - `[high]` `[bad_spec]` Require non-empty paired reason/recovery copy and programmatic association for every disabled high-impact confirmation state.
  - `[medium]` `[bad_spec]` Render localized, visibly labelled, qualified evidence values and report absent gates or incomplete counts as unknown rather than available.
  - `[medium]` `[bad_spec]` Associate unavailable per-row reason/recovery content with its rendered action slot, not only with enabled controls.
  - `[medium]` `[bad_spec]` Replace raw preview paragraph markup with Fluent UI V5 text components and assert the reason/recovery text, not only element existence.
  - `[medium]` `[bad_spec]` Strengthen malformed-loader tests to assert canonical fail-closed shape and bounded calls for every contradictory input.

### 2026-08-30 — Review pass 4
- verdicts: 23 findings — high 5, medium 13, low 0, false 4, maybe-false 1
- findings:
  - `[medium]` `[patch]` Gateway capability operands lack independent component tests — the implementation added both guards, but every correction-panel gateway stub reports both capabilities as true; re-derivation must vary each gateway capability and assert disabled associated copy with no dispatch.
  - `[false]` `[reject]` The narrow diff does not independently implement the page-wide intent — this run is an incremental review delta over the already-present evaluator/page implementation, whose focused and full suites ran from the cumulative tree.
  - `[medium]` `[reject]` Resetting the story baseline narrows historical review and gitlink evidence — the proposed correction edits this build's spec, which review rules reject; the current-run baseline remains the revision used for this unattended iteration and unrelated gitlinks are neither reverted nor declared as story work.
  - `[high]` `[bad_spec]` Confirmation can be enabled when composition claims requery support but no projection provider exists — the correction task now requires both the composition flag and a concrete refresh/query provider before submit.
  - `[high]` `[bad_spec]` Accepted work can become permanently unverifiable and retain the aggregate lock without a requery path — the task now forbids falling back to the pre-command projection and blocks submission unless reconciliation is concrete.
  - `[high]` `[bad_spec]` A hidden missing/failing-authentication instance can adopt and monopolize retained reconciliation before authorization — the task now keeps retained work adoptable until fresh authorization and concrete reconciliation support are established.
  - `[high]` `[bad_spec]` Submit can dispatch after viewport, admission, or capability evidence changes during reauthorization — the task now requires generation invalidation or a live boundary recheck inside the renderer callback before dispatch.
  - `[medium]` `[bad_spec]` Refresh can start status/requery I/O after the measured-safe or admission gate changes — the same boundary-recheck requirement now covers refresh I/O.
  - `[medium]` `[bad_spec]` Remove availability reports missing preview before the proven last-administrator stop — evaluator precedence now selects complete-population, target, and last-administrator safety reasons before preview readiness.
  - `[medium]` `[bad_spec]` Disabled snapshot states can render reason/recovery without `aria-describedby` — the correction task now derives disabled state and its associated pair from one decision.
  - `[medium]` `[patch]` Missing-lifecycle association is tested only for grant corrections — re-derivation must cover the remove-specific key and association.
  - `[medium]` `[patch]` The new gateway dispatch/status guards have no direct component regression coverage — re-derivation must independently disable each gateway seam while composition remains connected.
  - `[medium]` `[patch]` Live correction authentication transitions lack collapse and no-dispatch tests — the expanded test task now requires missing, failing, and live-transition authentication coverage with retained reconciliation.
  - `[medium]` `[patch]` Correction authorization generation ordering lacks an out-of-order completion test — the expanded test task now requires superseded authorization completion coverage.
  - `[medium]` `[patch]` Correction viewport changes are not behaviorally verified after render — the expanded test task now requires safe-to-unknown/phone disablement, recovery, and suspended-submit races.
  - `[medium]` `[patch]` Live fixed-aggregate admission changes are not verified on an open correction panel — the expanded test task now requires busy/released reason, recovery, and re-enablement coverage.
  - `[medium]` `[patch]` Nonterminal close/cancel/Escape retention is not directly covered, although disposal adoption is covered by `SubmittedCorrectionIsAdoptedAndCompletedByGlobalAdministratorsPage` — re-derivation must add the missing interaction checks without duplicating disposal evidence.
  - `[medium]` `[patch]` Capability tests do not attempt the action, assert empty command collections, or exercise unavailable tracked refresh — re-derivation must add explicit no-dispatch and refresh refusal assertions; live capability restoration is not required.
  - `[high]` `[bad_spec]` Requery availability uses only the composition flag instead of also requiring an actual provider — this is the same concrete-reconciliation root cause and triggered the strengthened correction task.
  - `[false]` `[reject]` An unauthenticated provider can still yield production authorization — `TenantsBffComposition` resolves through `TenantConfigurationPrincipalResolver`, which reads that same circuit provider and returns non-authorized evidence; a custom resolver returning Authorized would violate the authoritative seam.
  - `[maybe-false]` `[defer]` A never-completing provider or authorization resolver could leave initialization pending — no installed provider with that behavior was demonstrated; a production task that never completes, or a contractually required timeout, would settle the claim.
  - `[false]` `[reject]` Repeated `CommandGateway` resolution can combine different services — production registers the gateway scoped and tests register it singleton, so repeated resolution returns the same instance.
  - `[false]` `[reject]` A provider failure can be discarded while the production BFF returns Authorized — the production principal resolver catches provider failures and returns indeterminate evidence, disproving the reachable production outcome.

### 2026-08-30 — Review pass 5
- verdicts: 31 findings — high 7, medium 19, low 0, false 2, maybe-false 3
- intent_gap: 0
- bad_spec: 14: (high 7, medium 7, low 0)
- patch: 12: (high 0, medium 12, low 0)
- defer: 3: (high 0, medium 3, low 0)
- reject: 2: (high 0, medium 0, low 0, false 2)
- findings:
  - `[medium]` `[patch]` The suspended-reauthorization submit test changes evidence before the outer disable check and therefore never exercises the new dispatch-boundary guard — require a test that crosses the outer check, suspends before dispatch, then changes the live gate and proves no command is sent.
  - `[medium]` `[patch]` Projection requery has no test that changes a gate while status I/O is in flight — require that suspension point and prove projection I/O never begins afterward.
  - `[medium]` `[patch]` Missing concrete requery is not tested against retained correction ownership — require an authorized matching replacement with retained work and no usable query/refresh provider to leave the work adoptable and perform no status I/O.
  - `[high]` `[bad_spec]` Any registered query gateway, including the fail-closed unavailable implementation, counts as concrete requery support — require a usable gateway or explicit provider rather than mere service presence.
  - `[high]` `[bad_spec]` Retained reconciliation is adopted before action and target compatibility are checked — require matching-only adoption and ownership-safe mismatch handoff.
  - `[high]` `[bad_spec]` A null or changed intent can replace the only snapshot carrying nonterminal reconciliation — return tracking to the adoptable pool before clearing or replacing that state.
  - `[high]` `[bad_spec]` Loss of requery or lifecycle support can leave a nonterminal owner unable to reconcile while still monopolizing the lease — retain it for a qualifying replacement before disabling the surface.
  - `[medium]` `[bad_spec]` Support becoming usable after initialization does not retry retained adoption — retry matching adoption on eligible parameter/support reevaluation.
  - `[medium]` `[defer]` Dispatch/status capability or viewport evidence could theoretically change in the continuation gap after the renderer-boundary check — the implementation performs the required immediate callback check and no deterministic installed interleaving was demonstrated; a concrete reproducer would settle any stronger atomicity requirement.
  - `[medium]` `[defer]` A projection provider could theoretically be replaced after the requery guard and before invocation — no deterministic installed interleaving was demonstrated; capture-and-invoke requirements can be revisited with a reachable reproduction.
  - `[medium]` `[bad_spec]` Generation invalidation can leave `_isSubmitting` true because cleanup is conditioned on the old generation — reset transient submission state on every invalidation/abandonment path.
  - `[high]` `[bad_spec]` Submission or status completion depends on a renderer callback to retain tracking, and renderer disposal can reject that callback and strand a dispatch-marked lease — make ownership completion durable before renderer-dependent presentation.
  - `[medium]` `[bad_spec]` Disabled state, reason, recovery, and ARIA association independently recompute the decision — compute one immutable decision per render/action evaluation.
  - `[medium]` `[bad_spec]` Generic recovery copy is misleading for already-applied, confirmed, rejected, failed, and unavailable snapshot states — map each disabled lifecycle class to truthful canonical recovery.
  - `[medium]` `[bad_spec]` A tracked snapshot can mask a newer availability failure with stale lifecycle copy and leave refresh refusal unexplained — the unified decision must prioritize the current actionable block and its restoration path.
  - `[medium]` `[patch]` Missing concrete requery coverage exercises only grant — add remove as well as grant with no-dispatch assertions.
  - `[medium]` `[patch]` Failing-authentication coverage does not seed and recover retained reconciliation through a matching authorized replacement — add the handoff scenario.
  - `[medium]` `[patch]` Existing suspended submit/refresh tests change evidence before the newly added I/O boundary — add deterministic suspensions after the outer gate and during status/projection transitions.
  - `[medium]` `[patch]` No suspended-operation test changes an individual lifecycle capability at the dispatch or refresh boundary — vary each live capability and prove no downstream I/O.
  - `[medium]` `[patch]` Close/cancel/Escape tests assert only that the panel stays locked and do not prove the retained attempt remains reconcilable — dispose afterward and require a matching replacement to adopt and resume it.
  - `[medium]` `[patch]` Evaluator coverage omits the terminal precedence case where every safety prerequisite passes and only preview is missing — add the explicit `MissingConsequencePreview` assertion.
  - `[medium]` `[patch]` Incomplete-population precedence tests toggle only the aggregate completeness flag — independently cover invalid/duplicate rows, surface kind, freshness, lifecycle, blank version, and visible/complete version mismatch ahead of preview.
  - `[false]` `[reject]` The incremental review diff does not restate every page-wide acceptance path — verification evaluates the cumulative tree, where the unchanged page/evaluator implementation and full focused suite remain in scope.
  - `[false]` `[reject]` The eligible-grant matrix allegedly conflicts with the explicit rule that grant has no preview prerequisite — preview readiness is vacuous for the direct grant path, while the dedicated preview row applies to removal; the task text is the intentional disambiguation.
  - `[high]` `[bad_spec]` A correction for a different action or target can claim retained work and leave it owned but unusable — this is the same matching-adoption root cause and now has an explicit handoff requirement.
  - `[medium]` `[bad_spec]` An empty non-null safe message bypasses the state-text fallback and can leave disabled reason/ARIA copy blank — treat null, empty, and whitespace uniformly before selecting the canonical fallback.
  - `[high]` `[bad_spec]` A corrective-audit proof query exception after projection confirmation is caught as a submission failure, overwriting confirmed command truth — isolate proof lookup and retain `Confirmed` with delayed/unavailable audit evidence.
  - `[medium]` `[defer]` A never-completing authentication provider or resolver could leave work pending — no installed provider with that behavior or contractually required timeout was demonstrated; production timeout ownership remains outside this story without such evidence.
  - `[medium]` `[bad_spec]` Repeated decision evaluation can produce inconsistent disabled copy and associations — this duplicates the one-decision root cause and is covered by the immutable decision requirement.
  - `[medium]` `[patch]` Remove intent still lacks the missing-concrete-requery regression scenario — add it with optimistic composition flags and no command/status calls.
  - `[medium]` `[patch]` Retained reconciliation is not exercised through failing authentication and a later matching authorized owner — add that end-to-end ownership recovery assertion.

### 2026-08-30 — Review pass 6
- verdicts: 41 findings — high 8, medium 20, low 0, false 10, maybe-false 3
- intent_gap: 0
- bad_spec: 15: (high 8, medium 7, low 0)
- patch: 12: (high 0, medium 12, low 0)
- defer: 4: (high 0, medium 1, low 0, maybe-false 3)
- reject: 10: (high 0, medium 0, low 0, false 10)
- findings:
  - `[false]` `[reject]` Advancing the baseline revision allegedly hides the story delta — step 3 requires capturing the current `HEAD` before this implementation derivation, so `c69cdfd` is the mandated loop baseline; changing the build spec back is also an explicitly rejected review fix.
  - `[high]` `[bad_spec]` A command-gateway exception after renderer disposal can strand a dispatch-marked lease because terminal release still occurs only inside `InvokeAsync` — the gateway-await/disposal path is reachable and `completedIoSnapshot` remains null, so the final-loop renderer-independent completion requirement was not implemented.
  - `[high]` `[bad_spec]` `AuthenticationStateChanged` returns reconciliation and mutates component fields before renderer dispatch — the provider event may arrive off-dispatcher and can race submit, refresh, parameter processing, or disposal, contrary to the final-loop marshalling contract.
  - `[high]` `[bad_spec]` Refresh and completion continuations mutate reconciliation ownership after `ConfigureAwait(false)` outside `InvokeAsync` — lines that retain/release after reauthorization, status, and projection work change component fields without the required renderer serialization.
  - `[high]` `[bad_spec]` Several completion paths call `TryAdoptEligibleReconciliation` outside a renderer callback — the method assigns the lease, retained evidence, snapshot, and resume flag, so these paths violate the same renderer-ownership invariant.
  - `[false]` `[reject]` Any decorator or custom query gateway can falsely count as concrete support — installed composition validates the connected/unavailable pairing and the built-in unavailable gateway is rejected explicitly; a custom gateway that advertises connection while intentionally returning only unavailable evidence violates its host contract rather than demonstrating this outcome.
  - `[maybe-false]` `[defer]` Matching only action and target could attach retained work to a different audit row for the same outcome — timestamp filtering prevents an older proof from being linked, but product semantics must settle whether equivalent fixed-scope work is intentionally reusable across same-action/same-target audit rows.
  - `[false]` `[reject]` Capability changes cannot trigger a live panel render — production gateway/composition capability values are immutable for the scoped service lifetime, so the mutable-stub transition is not an installed runtime state.
  - `[medium]` `[bad_spec]` Manual refresh can overlap automatic accepted-command reconciliation — the refresh button becomes enabled after `Accepted`, while automatic reconciliation bypasses `_refreshInFlight`, allowing duplicate status/projection walks despite the spec's serialization requirement.
  - `[medium]` `[bad_spec]` Unrelated aggregate gate notifications can start global-administrator reconciliation — `StateChanged` is circuit-global and the panel resumes any owned dispatch-marked lease after every notification, so unrelated tenant activity can cause redundant or overlapping fixed-scope status I/O.
  - `[maybe-false]` `[defer]` Synchronous retain notification may run before the caller clears its local lease pointer — the gate notifies before `RetainReconciliationForReplacement` nulls `_admissionLease`; a deterministic dispatcher-order test is needed to prove whether `InvokeAsync` can observe and act on that stale pointer inline.
  - `[medium]` `[bad_spec]` Status completion after ownership retention can update only the disposed/disabled panel while the retained ledger stays stale — `TryAdvanceReconciliation` fails for an ownerless lease, but the unchanged generation can still apply the newer local state, forcing a replacement to restart from older evidence.
  - `[medium]` `[bad_spec]` Reconciliation high-water logic allows `Degraded` or `UnableToVerify` to regress to `Accepted` — `IsLifecycleRegression` rejects only `RequestSent` and `ProjectionPending` to `Accepted`, violating the explicit monotonic lifecycle requirement.
  - `[high]` `[bad_spec]` Corrective-audit proof I/O starts without another live authorization/viewport/support check after projection I/O — authorization can be revoked while projection loading is suspended, yet privileged audit I/O still begins before the stale generation is noticed.
  - `[medium]` `[defer]` Confirmed audit-delayed state has no in-panel proof retry — the proof-retry limitation predates this availability change and the new copy does not promise automatic polling; a later lifecycle owner can decide whether confirmed audit evidence needs an explicit retry surface.
  - `[medium]` `[bad_spec]` Visible `RequestSent` removal state reports missing consequence preview — with no tracking handle and `CanSubmit == false`, the decision falls through the evaluator and masks the already-dispatched lifecycle with pre-submit copy.
  - `[false]` `[reject]` Localized reason/recovery strings lack a whitespace fallback — tracked production resources are nonblank and parity-checked, while missing resource keys resolve to nonblank keys; a custom whitespace localizer is not a reachable installed outcome.
  - `[medium]` `[patch]` Preview precedence lacks explicit target-missing and last-administrator combinations with preview disabled — the code order is correct, but lower patch work is moot because verified bad-spec defects exceeded the final repair loop.
  - `[medium]` `[patch]` Retained-adoption tests do not isolate same-action/different-target mismatch — action mismatch is covered, but deleting the target comparison would still pass; the test patch is moot under non-convergence.
  - `[medium]` `[patch]` Retained support-restoration coverage exercises grant only — remove is covered for initial unavailable-query refusal but not retained adoption after support recovery; the test patch is moot under non-convergence.
  - `[medium]` `[patch]` Authentication coverage omits an authorized tracked owner losing authentication during status I/O and handing work to a replacement — the missing regression is verified but lower patch work is moot under non-convergence.
  - `[medium]` `[bad_spec]` The suite omits renderer-loss gateway failure, concurrent manual/automatic refresh, and post-projection authorization-loss paths that expose verified lifecycle defects — these are nontrivial state-machine gaps governed by the final-loop contract, not isolated assertion additions.
  - `[medium]` `[patch]` Preview precedence is not verified for preview-disabled target-missing and last-administrator removal — existing tests leave preview ready in those branches, so the direct test patch remains missing and is moot under non-convergence.
  - `[medium]` `[patch]` Target-only retained-work mismatch is untested — deleting the target predicate while retaining action matching passes current tests and can monopolize the lease; the direct test patch is moot under non-convergence.
  - `[medium]` `[patch]` Missing `AuthenticationStateProvider` behavior has no component regression — the implementation fails closed, but all current panel tests register a provider; the direct hidden/no-I/O/no-adoption test is moot under non-convergence.
  - `[medium]` `[patch]` Accepted completion is not tested against renderer loss before its post-I/O callback — the durable catch path exists for accepted results, but the direct disposal-and-replacement proof is absent and moot under non-convergence.
  - `[medium]` `[patch]` Healthy tracked recovery copy and control association are unverified — current coverage changes the viewport first and therefore exercises the live-block branch; the direct assertion patch is moot under non-convergence.
  - `[medium]` `[patch]` Whitespace `SafeMessage` fallback has no regression test — the implementation uses `IsNullOrWhiteSpace`, but terminal tests supply nonblank messages; the direct fallback/ARIA test is moot under non-convergence.
  - `[false]` `[reject]` The incremental diff does not restate the page-centered implementation — this run reviews the cumulative tree from its required loop baseline, and unchanged page/loader/routing behavior passed the exact focused and full suites.
  - `[false]` `[reject]` The matrix requires a grant preview even though the task ignores it — the action-specific reading is explicit: grant is direct and only removal opens the dedicated preview, so preview readiness is vacuous for grant.
  - `[medium]` `[patch]` Blank target identity can throw during filtered retained adoption — a global-administrator intent may be fail-closed with an empty target while support is otherwise present, and the filtered gate validates before checking retained work; a simple pre-adoption whitespace guard is required but moot under non-convergence.
  - `[false]` `[reject]` A tenant-domain correction can be coerced to grant and claim retained work — the production parent renders this panel only when `IntendedCommandDomain` is `GlobalAdministrators`; tenant-domain intents route to `CorrectionStartPanel`.
  - `[false]` `[reject]` Ignoring unavailable-reason and preview-input differences in intent identity can permit stale dispatch — pre-submit snapshots are rebuilt from the new intent when projection evidence exists, and absent projection evidence independently makes availability fail closed before dispatch.
  - `[high]` `[bad_spec]` An `Accepted` gateway result with blank tracking identifiers leaves a dispatch-marked lease neither refreshable, retainable, nor terminally releasable — the public gateway result type permits the state and the panel accepts it without validating the required handle.
  - `[high]` `[bad_spec]` A stale status response can regress `ProjectionPending` to `Accepted`, fail ledger advancement, and leave disposal unable to retain the local regressed state — this reachable monotonicity failure can strand ownership and duplicates the incomplete high-water root cause.
  - `[maybe-false]` `[defer]` A corrective-proof query that never completes can delay terminal lease release indefinitely — installed HTTP timeout/cancellation behavior and a deterministic noncompletion reproducer are needed to distinguish bounded latency from an actual permanent lock.
  - `[high]` `[bad_spec]` Renderer loss during a gateway exception can strand the dispatch-marked lease — independent edge tracing confirms the terminal failure path has no renderer-independent release and shares the first lifecycle-completion root cause.
  - `[false]` `[reject]` Disabled decisions can lose all explanation when localization returns whitespace — tracked EN/FR resources are nonblank and missing keys remain visible keys, so the cited resource state is not reachable in the installed application.
  - `[medium]` `[bad_spec]` Retaining nonterminal reconciliation clears `_admissionLease`, which makes `CanClose` true while the snapshot still tracks active work — close, cancel, or Escape can hide an active correction despite the explicit nonterminal-close requirement.
  - `[false]` `[reject]` Runtime capability loss leaves controls enabled until another render — installed capability objects are immutable for the circuit scope, so no live production transition was demonstrated.
  - `[medium]` `[patch]` Post-I/O renderer-loss retention lacks a direct disposal test — blocking/invalidation tests still allow callbacks to run, so the missing accepted-completion replacement proof is verified but moot under non-convergence.

### 2026-08-31 — Review pass
- verdicts: 28 findings — high 0, medium 1, low 2, false 24, maybe-false 1
- findings:
  - `[false]` `[reject]` The page's historical grant submit method bypasses a required preview handoff — the installed composition reports grant preview unavailable, so production availability blocks before this unchanged downstream flow; enabling readiness requires the owning downstream story to install its preview and change that flow.
  - `[false]` `[reject]` The page-local removal flow violates downstream ownership — the intent explicitly preserves existing downstream command flows, and the unchanged removal preview/dispatch code is that installed downstream flow; this re-drive only gates entry into it.
  - `[false]` `[reject]` Existing page lifecycle reconciliation makes Story 4.1 a lifecycle owner — those methods are unchanged downstream-story behavior, while every changed availability path only reads capability and admission evidence.
  - `[false]` `[reject]` Static production preview flags are not valid readiness evidence — the concrete composition truthfully reflects the compile-time installed state: no grant preview and an existing removal preview; no runtime readiness provider exists to consult.
  - `[false]` `[reject]` Composition tests do not prove downstream handoff — the focused page suite separately exercises the installed removal preview and command flow, while the concrete-composition test correctly pins only installed capability evidence.
  - `[false]` `[reject]` The evidence constructor can accidentally enable grant from removal readiness — the multi-action page explicitly supplies both flags, and the only single-flag production caller supplies `_snapshot.CanSubmit` for its one current correction action, so no installed caller conflates the two actions.
  - `[false]` `[reject]` The shared evaluator changes correction-panel post-dispatch behavior — correction evidence continues to map its action-specific `_snapshot.CanSubmit` value to the evaluated action, the production panel diff is empty, and all correction-panel regressions pass.
  - `[medium]` `[defer]` Contradictory complete Ready evidence can retain `CompleteIsAuthorizationScopedEmpty` and pass removal evaluation — the omission is real in the public pure evaluator, but the bounded loader and both installed callers already prevent the contradictory shape and the code predates this re-drive; recorded in frontmatter for follow-up.
  - `[false]` `[reject]` Mutable backing lists make current availability decisions race — both installed production callers pass snapshot arrays that are replaced rather than mutated, and no caller mutates row collections in place during evaluation.
  - `[maybe-false]` `[defer]` Repeated Razor property evaluation can mismatch reason, recovery, disabled state, and ARIA associations — renderer callbacks are serialized, but a deterministic test that changes admission or viewport evidence between synchronous property reads is needed to settle whether the gate's immediately changed state can interleave; recorded in frontmatter.
  - `[false]` `[reject]` Count evidence claims mutation-safe qualification despite a visible/complete version mismatch — the label describes the complete snapshot's count evidence only; action availability independently compares visible and complete versions and remains fail-closed.
  - `[false]` `[reject]` Freshness evidence ignores invalid row-level state — the selector states only the snapshot's current lifecycle/version fact, while row validity is independently enforced before either action can become available.
  - `[false]` `[reject]` Test preview defaults mask the production-disabled grant — the concrete composition test pins production grant readiness to false, and test doubles intentionally enable downstream flows for legacy lifecycle tests while explicit missing-preview cases verify fail-closed behavior.
  - `[false]` `[reject]` The admission-transition test violates the no-lease-mutation boundary — lease calls are fixture setup for an external downstream owner; the page under test only observes busy/available state and assertions prove it dispatches nothing.
  - `[false]` `[reject]` Missing requery cannot be tested independently — evaluator tests vary requery independently for both actions, composition tests isolate the read-surface seam, and page tests verify disconnected requery/read support blocks before dispatch.
  - `[false]` `[reject]` Permission, busy, and unsafe-viewport evaluator behavior lacks verification — the executed focused page suite covers missing and indeterminate authority, live admission transitions, measured phone width, and no dispatch; unit placement is not an acceptance requirement.
  - `[false]` `[reject]` Loader tests omit required Empty and contradictory Ready coverage — authorization-scoped rowless Empty is covered at evaluator/page surfaces, while loader tests exercise both contradictory Empty-with-row and Ready-without-row shapes plus the loader's explicit Ready authorization-scope guard.
  - `[low]` `[patch]` Qualified count copy rendered `1 administrators` — changed EN/FR whole strings to count-oriented wording valid for every numeric value and updated the focused assertion and stub localization.
  - `[low]` `[reject]` Verification commands use the deprecated `-parallel none` switch — the warning is runner deprecation noise rather than a build warning or test failure, and the only proposed correction edits this build's spec, which review policy rejects.
  - `[false]` `[reject]` Omitted grant readiness can enable grant without grant-specific evidence — the correction caller's single supplied readiness value is its current grant preview decision when evaluating grant, and the multi-action page initializes grant readiness explicitly.
  - `[false]` `[reject]` Eligible grant currently submits without a consequence preview — eligible grant is unreachable through the installed composition because grant preview readiness is false; the cited submit path is unchanged downstream behavior awaiting its owning preview story.
  - `[false]` `[reject]` A strict component-level handoff reading is the only intent reading — the intent also explicitly preserves existing downstream flows, so ownership is change-level: the availability slice observes and hands off while co-located downstream code retains its prior ownership.
  - `[false]` `[reject]` Production must expose an eligible grant in this diff — fail-closed unavailability is the required result until the downstream grant-preview story is installed; evaluator and injected page tests prove the eventual evidence state without claiming it is currently installed.
  - `[false]` `[reject]` Removal readiness must be dynamically owner-reflected — the installed removal preview is compile-time application behavior, making immutable composition readiness a truthful capability declaration.
  - `[false]` `[reject]` Unchanged lifecycle methods violate availability immutability — the changed availability surface only calls `IsLockedByAnother` and reads capability flags; unchanged methods remain owned by the preserved downstream flows.
  - `[false]` `[reject]` Structural readiness independence is absent at the installed consumer — the page supplies distinct values, while the correction surface evaluates only one action and supplies that action's own preview state.
  - `[false]` `[reject]` Loader verification without a production loader edit diverges from intent — the required hardened loader behavior already existed at the captured baseline, and the new executed tests legitimately verify that dependency rather than duplicating it.
  - `[false]` `[reject]` Correction-panel behavior diverges from the no-change requirement — the production correction panel is untouched; only its test double implements the expanded capability interface.

## Design Notes

The evaluator owns no localization or I/O. It returns typed availability, reason, and recovery identifiers from immutable evidence. The page maps those identifiers to whole-string resources and re-evaluates immediately before handing an eligible action to its existing downstream preview; the gateway and aggregate remain the security and invariant boundaries.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release -warnaserror -m:1 -nr:false` -- expected: warning-clean UI test assembly.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.State.GlobalAdministratorActionAvailabilityTests -class Hexalith.Tenants.UI.Tests.Components.GlobalAdministratorsPageTests -class Hexalith.Tenants.UI.Tests.Services.Gateways.TenantCommandGatewayTests -class Hexalith.Tenants.UI.Tests.Services.Gateways.TenantsBffCompositionTests` -- expected: focused availability guardrail and capability tests pass.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.Services.Gateways.GlobalAdministratorsProjectionLoaderTests -class Hexalith.Tenants.UI.Tests.LocalizerDoubleParityTests -class Hexalith.Tenants.UI.Tests.TenantsUiCompositionTests` -- expected: complete-projection and localization parity gates pass.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` -- expected: full UI suite passes with no skipped or not-run tests.
- `dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false` -- expected: full solution warning-clean.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-4-1-fixed-scope-global-administrator-action-availability.md` -- expected: story file declares every moved gitlink or reports none moved.
- `git diff --check` -- expected: no whitespace errors.

## Auto Run Result

Status: done

### Summary

Implemented the fixed-scope, availability-only global-administrator guardrail. Grant and removal now consume independent downstream-preview readiness, concrete dispatch/status capability, projection-requery support, fixed-aggregate admission, qualified direct-read evidence, complete-population proof, and measured viewport safety. Production grant remains visibly fail-closed until its owning preview flow is installed; the existing removal preview remains available when every guard passes. The evaluator stays pure, the page dispatches nothing from blocked entry points, and correction-panel production behavior is unchanged.

### Files Changed

- `_bmad-output/implementation-artifacts/spec-4-1-fixed-scope-global-administrator-action-availability.md` — records the human scope resolution, cumulative file/gitlink declaration, review triage, verification, and terminal result.
- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor` — composes independent preview readiness and concrete gateway capability into page availability.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` — adds action-specific English preview recovery and count-oriented evidence wording.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx` — adds parity-matched French preview recovery and count-oriented evidence wording.
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantsBffComposition.cs` — exposes fail-closed grant/removal preview-readiness seams.
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs` — reflects the installed state: grant preview absent, removal preview present.
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorActionAvailabilityEvaluator.cs` — blocks grant when its downstream preview is absent and returns action-specific recovery keys.
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorActionEvidence.cs` — carries grant readiness separately and preserves support-safe diagnostics.
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorActionUnavailableReason.cs` — generalizes missing-preview semantics to the evaluated action.
- `tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorCorrectionPanelTests.cs` — keeps the correction test composition compatible without changing production behavior.
- `tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs` — verifies preview/capability isolation, no-dispatch blocks, live viewport/admission transitions, labelled evidence, localization, and truthful count copy.
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/GlobalAdministratorsProjectionLoaderTests.cs` — verifies malformed-page and duplicate-identity refusal with bounded canonical results.
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs` — verifies concrete fixed-scope gateway capability reporting.
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantsBffCompositionTests.cs` — verifies independent capability mapping and installed preview readiness.
- `tests/Hexalith.Tenants.UI.Tests/State/GlobalAdministratorActionAvailabilityTests.cs` — verifies independent action readiness, precedence, purity, and support-safe diagnostics.

### Review Findings

- Patches applied: 1 (`high 0`, `medium 0`, `low 1`). Corrected the EN/FR complete-evidence count wording so every numeric value is grammatical.
- Items deferred this pass: 2. One pre-existing medium evaluator edge accepts a contradictory complete-read authorization-scope flag that installed callers already prevent; one maybe-false render-consistency concern needs a deterministic admission/viewport interleaving reproducer. Both are recorded in frontmatter alongside the two previously deferred downstream lifecycle risks.
- Rejected: grant direct-dispatch bypass — installed grant readiness is false, so the unchanged downstream flow is unreachable until its owning preview story changes it.
- Rejected: removal ownership mismatch — the unchanged installed removal flow is the explicitly preserved downstream flow.
- Rejected: page lifecycle code makes this availability slice a lifecycle owner — the changed slice only reads evidence; lifecycle methods are unchanged downstream behavior.
- Rejected: static preview flags are invalid — they truthfully describe compile-time installed flows and no dynamic readiness provider exists.
- Rejected: composition tests omit handoff — page tests separately exercise the installed removal flow while composition tests pin capability evidence.
- Rejected: constructor readiness conflation — the page supplies both flags and the correction caller supplies the one current action's preview decision.
- Rejected: correction post-dispatch behavior changed — production correction code is untouched and its full regressions pass.
- Rejected: mutable evidence rows race — installed callers use replacement snapshots backed by arrays and never mutate rows in place.
- Rejected: count selector overclaims mutation safety — it describes complete-snapshot count evidence; availability separately enforces cross-snapshot version equality.
- Rejected: freshness selector ignores rows — it states only snapshot lifecycle/version freshness, while row validity is independently enforced.
- Rejected: test defaults mask production grant state — concrete-composition and explicit missing-preview tests pin the production and fail-closed cases.
- Rejected: the admission transition test owns lifecycle — lease calls only arrange an external owner's busy state; the page under test dispatches nothing.
- Rejected: requery loss is not isolated — evaluator, composition, and page tests cover the independent fail-closed seams.
- Rejected: permission/busy/viewport branches lack coverage — the executed focused page suite exercises all three and asserts no dispatch.
- Rejected: Empty/Ready loader shapes lack coverage — evaluator/page tests cover scoped Empty, and loader tests cover contradictory Empty and Ready shapes.
- Rejected: deprecated `-parallel` syntax is a build defect — it emits runner deprecation noise but no build warning or test failure, and review policy forbids fixing a finding by editing this build's spec.
- Rejected: omitted grant readiness enables grant — no installed multi-action caller omits it, and correction supplies its grant-specific state for grant.
- Rejected: eligible grant currently bypasses preview — eligible grant is not installed or production-reachable.
- Rejected: strict component-level ownership is the only intent reading — the intent explicitly preserves co-located downstream flows and constrains this change's ownership.
- Rejected: this diff must make grant eligible — fail-closed unavailability is required until the downstream grant story is installed.
- Rejected: removal readiness must be dynamically reflected — the existing preview is compile-time behavior and the immutable flag is truthful.
- Rejected: unchanged lifecycle methods violate immutability — only the availability delta is owned here and it observes state without mutating it.
- Rejected: structural action independence is absent — the page supplies distinct flags and correction evaluates one action-specific preview at a time.
- Rejected: loader tests without loader edits diverge — the behavior already existed at the captured baseline and the story must verify reused dependencies.
- Rejected: correction behavior diverges — the production panel has no current-iteration diff.
- Follow-up review recommendation: `false`; patched entry counts are `high 0`, `medium 0`, `low 1`.

### Verification

- UI test-project Release build with warnings as errors — passed with 0 warnings and 0 errors.
- Primary focused availability/capability suite — passed 322/322, with 0 skipped and 0 not run.
- Loader/localization/composition suite — passed 111/111, with 0 skipped and 0 not run.
- Full UI suite — passed 2,650/2,650, with 0 failed, 0 skipped, and 0 not run.
- Full `Hexalith.Tenants.slnx` Release build with warnings as errors — passed with 0 warnings and 0 errors.
- Story gitlink validator — passed; the preserved `references/Hexalith.FrontComposer` baseline-range movement is declared and no submodule content was changed.
- `git diff --check` and staged `git diff --cached --check` — passed.
- Matrix audit — every matrix row ran in the focused suites: eligible grant/removal, incomplete population, last administrator, lifecycle/preview/admission blocks, and unsafe authority/evidence/viewport states.

### Residual Risks

- The four preserved/deferred items in frontmatter remain follow-up work; the two high-severity items belong to downstream command-lifecycle owners, and neither is introduced or broadened by this availability-only re-drive.
- The installed grant preview remains unavailable by design, so production grant stays read-only until its downstream story supplies the complete preview flow.
- No operator action is required. `sprint-status.yaml` was not modified.
