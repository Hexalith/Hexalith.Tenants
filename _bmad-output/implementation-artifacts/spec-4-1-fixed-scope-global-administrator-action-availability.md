---
title: '4.1 Fixed-Scope Global Administrator Action Availability'
type: 'feature'
created: '2026-08-28'
status: 'blocked'
baseline_commit: 'c69cdfd58a876648707782a028e1352be1e278cb'
baseline_revision: 'c69cdfd58a876648707782a028e1352be1e278cb'
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
---

<intent-contract>

## Intent

**Problem:** The existing Global Administrators page uses page-local string ternaries and CSS-only hiding to decide whether grant and remove actions are available. It can treat a paged result as sufficient removal evidence and cannot prove aggregate admission, measured viewport safety, preview readiness, or complete lifecycle support.

**Approach:** Add a pure, typed fixed-scope availability guardrail that evaluates grant and each visible removal independently from server-reflected authority, qualified direct-read evidence, explicit lifecycle capabilities, complete population evidence, fixed-aggregate admission, target visibility, preview readiness, and measured viewport safety. Render canonical localized reasons and recoveries while retaining the existing fixed routes and downstream command flows.

## Boundaries & Constraints

**Always:** Fix the identity to `system / global-administrators / global-administrators`; keep API/domain authorization authoritative; require current projection freshness, current lifecycle, non-empty projection version, and authoritative complete population evidence before removal; acquire the circuit-scoped fixed-aggregate admission gate for active attempts; preserve authorized read-only rows during degraded states; use FrontComposer/Fluent UI V5, whole-string EN/FR resources, stable selectors, and programmatic reason/recovery associations.

**Block If:** A qualifying global-administrator lifecycle capability cannot be represented without changing a public package contract outside this repository, or existing shared admission/viewport primitives cannot enforce the fixed-aggregate and measured-viewport invariants without an architecture change.

**Never:** Infer authority or totals client-side; use `HasMore`, cursor history, page count, `ServedAt`, client time, SignalR, tenant freshness, or CSS visibility as mutation evidence; derive scope from a tenant or row; disclose administrator data to unauthorized/indeterminate callers; dispatch from the evaluator; add an endpoint; weaken the last-administrator domain guard; modify `sprint-status.yaml`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Eligible grant | Authorized; qualified current direct read; lifecycle, preview, admission, and safe viewport ready | Grant available with fixed platform scope; no target-existence inference | Evaluator remains side-effect free |
| Eligible remove | Same evidence plus visible target and complete authoritative count greater than one | That row alone may open the dedicated preview | Recheck all evidence before submit |
| Incomplete population | Paged, bounded, mixed-version, capped, recovered, or otherwise incomplete read | Every remove is unavailable with count-incomplete reason and recovery | Never promote `HasMore` or page rows to total evidence |
| Last administrator | Complete authoritative count equals one | Remove unavailable before preview with localized last-admin reason | Domain still rejects a race with `LastGlobalAdministrator` |
| Missing support or lock | Command/status/requery/preview support missing, or fixed aggregate admitted elsewhere | All affected mutations unavailable with canonical lifecycle/high-impact reason | Unrelated tenant aggregates remain usable |
| Unsafe evidence | Authority indeterminate; stale/unknown provenance; target missing; viewport unknown/phone | Affected action fails closed with visible reason and recovery | Retain only authorized last-confirmed read data; disclose nothing when unauthorized |

</intent-contract>

## Code Map

- `src/Hexalith.Tenants.Contracts/Identity/TenantIdentity.cs:21` -- read-only canonical fixed identity via `ForGlobalAdministrators()`.
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorsSnapshot.cs:156` -- current mutation-evidence predicate lacks projection-version and completeness semantics; preserve support-safe diagnostics.
- `src/Hexalith.Tenants.UI/Services/Gateways/GlobalAdministratorsProjectionLoader.cs:23` -- reuse bounded, cursor-safe, projection-version-consistent complete-population loading.
- `src/Hexalith.Tenants.UI/State/TenantAggregateCommandAdmissionGate.cs:12` and `State/TenantCommandAggregateLock.cs:23` -- reuse circuit-scoped exclusivity and the fixed global-administrator key.
- `src/Hexalith.Tenants.UI/State/TenantHighImpactViewportObservation.cs:8` -- reuse measured Unknown/Phone fail-closed viewport evidence.
- `src/Hexalith.Tenants.UI/State/TenantDetail/TenantHighImpactActionAvailabilityEvaluator.cs:17` -- reuse the pure evaluator/result pattern and precedence, not tenant-specific wording.
- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:768` -- replace ad hoc grant/remove ternaries; `HasPositiveRemovalPopulationEvidence` at line 831 is the known-bad incomplete-count bypass.
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor:310` -- share the same fixed-aggregate gate so correction cannot race the page.
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs:6` and `Abstractions/ITenantsBffComposition.cs:11` -- expose explicit global command/status/requery support only as narrowly required.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` and `TenantsResources.fr.resx` -- canonical reason/recovery copy with exact EN/FR parity.
- `tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs:1156` -- reverse the historical test that enables remove from `HasMore`; retain authorization, fixed-scope, freshness, last-admin, and support-safe coverage.
- `tests/Hexalith.Tenants.UI.Tests/State/TenantHighImpactActionAvailabilityTests.cs:21` -- reference matrix/purity/precedence test style for the new global evaluator.

## Tasks & Acceptance

**Execution:**
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorActionEvidence.cs`, `GlobalAdministratorActionAvailability.cs`, and `GlobalAdministratorActionAvailabilityEvaluator.cs` -- add one-type-per-file immutable evidence/results and a pure independent grant/remove evaluator with canonical reason precedence; carry `HasMeasurement` separately from viewport state; require authorization-scoped, rowless `Empty`; ignore preview readiness for grant because Story 4.1 has no grant preview, but require actual remove-preview readiness only after complete-population, target-presence, and last-administrator evidence have selected any more specific safety reason; fail invalid/duplicate identity evidence closed; override diagnostics so UserId/projection metadata cannot appear.
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs`, `TenantCommandGateway.cs`, `UnavailableTenantCommandGateway.cs`, `Abstractions/ITenantsBffComposition.cs`, and `Services/TenantsBffComposition.cs` -- represent fixed-scope dispatch/status/requery capability independently, map each production flag only to its corresponding seam, and fail closed when any required capability is absent.
- `src/Hexalith.Tenants.UI/State/TenantAggregateCommandAdmissionGate.cs` and `State/TenantCommandAggregateLock.cs` -- add ownership-safe fixed-aggregate leases and exception-isolated change notifications: failed/repeated acquisition cannot release another attempt; a lease can mark dispatch once; terminal release requires a marked dispatch plus an explicit terminal lifecycle state, while pre-dispatch abandonment uses a separate operation. Degraded, UnableToVerify, refresh, cancel-after-dispatch, close, and component disposal retain both the lease and reconciliation state fail-closed. Make retained reconciliation adoptable by exactly one replacement surface so it can resume status/projection reconciliation and release only on terminal evidence; merely exposing a read-only handle is insufficient. Serialize retain/adopt/release transitions under a deadlock-safe synchronization order, reject lifecycle regressions, and prove a retention call cannot succeed after terminal release.
- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor` -- obtain a separate bounded complete-population snapshot through `GlobalAdministratorsProjectionLoader` without replacing Story 1.11's paged review rows; collapse the whole privileged surface if that walk returns Unauthorized; require current row freshness/lifecycle and the same stable projection version for visible and complete evidence; evaluate/re-evaluate availability; capture preview rows, freshness, lifecycle, and identity set together and reject submit on any mismatch; preserve message/correlation tracking whenever a nonterminal lease remains held and adopt any retained fixed-scope reconciliation before enabling a new attempt; derive unique DOM associations from rendered row ordinal and associate unavailable row slots as well as rendered controls; pair each reason with its own recovery; react live to viewport/admission/authentication changes; remove the `HasMore` bypass. Evidence selectors must render localized, visibly labelled, qualified scope/freshness/count/admission values and must report missing gate/evidence as unknown rather than available.
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor` -- participate in the same ownership-safe fixed-aggregate lease; require the explicit composition dispatch/status/requery capabilities, matching gateway dispatch/status capabilities, a concrete usable projection-requery path (`ProjectionRefreshProvider` or an `ITenantQueryGateway` other than the unavailable fail-closed implementation), a present live authentication provider, fresh authorization, and an actual safe viewport measurement before submit or refresh. Never treat the pre-command `CurrentProjection` as a requery fallback. Do not remove retained reconciliation from the adoptable pool until fresh authorization and concrete reconciliation support are established; adopt only reconciliation whose action and target match the current non-null intent, and ensure a mismatching claimant cannot monopolize or discard it. An unauthorized, missing-provider, indeterminate, mismatching, or hidden instance must not strand ownership from an authorized matching replacement. Retain accepted tracking before any renderer-dependent update; before clearing or replacing a nonterminal snapshot because intent, authorization, viewport, admission, or lifecycle support changed, return its reconciliation to the adoptable pool. Retry matching adoption when the current intent or concrete support becomes eligible. Adopt and resume retained reconciliation from a prior surface; marshal post-await renderer state through `InvokeAsync`, but make lease retention/release and accepted completion durable without depending on a live renderer; serialize refresh entry; generation-guard initialization, authentication transitions, refresh, and submit, reset transient submission state on every invalidation path, and either invalidate those generations on live viewport/admission/capability changes or re-evaluate every live gate inside the renderer callback immediately before dispatch/status/requery I/O. Map resolver/provider failures to indeterminate authorization. Compute disabled confirmation, reason, recovery, and `aria-describedby` from one immutable decision per render/action evaluation, use non-empty whitespace-safe fallbacks, and select state-specific canonical recovery so snapshot-state blocks, last-administrator blocks, lifecycle gaps, unavailable refresh, and every other disabled state have one truthful associated pair. If projection confirmation succeeds but corrective-audit proof lookup fails, preserve `Confirmed` and mark audit proof delayed/unavailable instead of converting the accepted command into a submission failure. Prevent close while a tracked attempt is nonterminal and preserve its reconciliation state through intent changes, authorization/support loss, disposal, and failed renderer callbacks; react live to authentication, viewport, admission, and capability changes.
- `src/Hexalith.Tenants.UI/Services/Gateways/GlobalAdministratorsProjectionLoader.cs` -- reject null rows, contradictory Empty/Ready page shapes, oversized pages, mismatched request cursor/page size, duplicate/invalid identities, mixed versions, missing cursors, repeated cursors, and noncurrent lifecycle as incomplete evidence; never normalize contradictory evidence into a successful shape. Any later-page Unauthorized result collapses all accumulated rows and provenance to the canonical unauthorized result.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`, `TenantsResources.fr.resx`, and `Components/Pages/GlobalAdministratorsPage.razor.css` -- add parity-checked whole strings and reflect measured responsive state without making CSS an evidence source. Use Fluent UI V5 text components where available; do not add raw paragraph markup for reason/recovery copy.
- `tests/Hexalith.Tenants.UI.Tests/State/GlobalAdministratorActionAvailabilityTests.cs`, `Components/GlobalAdministratorsPageTests.cs`, `Components/GlobalAdministratorCorrectionPanelTests.cs`, and focused loader/gateway/composition/admission/localization tests -- use PascalCase for every new or renamed method and cover every matrix row/reason precedence; unmeasured-safe viewport; authorization-scoped Empty; multi-page complete loading plus null/contradictory/oversized/wrong-request pages with canonical fail-closed result shape and bounded call count; later-page authorization collapse with no retained identity; lifecycle/version-qualified rows; viewport/auth/admission transitions and denied-to-authorized recovery after render; out-of-order authorization and refresh completion; composition and gateway dispatch/status/requery capabilities independently for both grant and remove; missing concrete requery despite optimistic composition flags, including the unavailable gateway and both grant/remove intents; throwing admission subscribers; atomic/monotonic retain-adopt-release behavior; actual submitted page-to-correction and correction-to-page reconciliation adoption through terminal evidence; matching-only retained adoption plus mismatch handoff; retry after support restoration; retention across null/changed intent, authorization/support loss, renderer disposal, and post-I/O renderer failure; missing/failing/live-transition authentication with retained reconciliation; cancel/close/Escape/disposal reconciliation retention followed by successful matching replacement adoption; stale preview identity/freshness/version refusal; submit races that cross the outer authorization check before viewport/admission/capability changes, status races that change a live gate while status I/O is suspended, and projection races that assert no requery after a post-status gate change; localized labelled evidence values and every unavailable branch; grant, preview-submit, correction-submit, and unique per-row reason/recovery ARIA references with asserted non-empty text; last-administrator precedence over missing-preview copy; disabled already-applied/confirmed/rejected/failed/unavailable and tracked-refresh associations with truthful state-specific recovery; no-dispatch and no-refresh assertions for every missing capability; confirmed-projection proof-query failure that retains confirmation and reports delayed/unavailable audit evidence; canonical reason vocabulary/selectors; evaluator purity/support-safe diagnostics; the final all-safe-but-preview-missing evaluator case; and each complete-population prerequisite (invalid/duplicate rows, surface kind, freshness, lifecycle, blank version, and version mismatch) taking precedence over missing preview.

**Acceptance Criteria:**
- Given any authority, direct-read provenance, lifecycle support, admission, target, count, preview, or viewport input is unknown or unsafe, when availability is evaluated, then only the affected action fails closed with a canonical localized reason and recovery and no dispatch occurs.
- Given fixed-scope read evidence is paged, bounded, recovered, mixed-version, or incomplete, when any removal row renders, then every remove is unavailable and neither `HasMore` nor client enumeration can open preview.
- Given complete current evidence proves one administrator, when its row renders, then last-administrator removal is visibly unavailable before preview; given more than one and every other gate passes, only the visible eligible target may open preview.
- Given one global-administrator attempt is active anywhere in the interactive circuit, when another page or correction action evaluates, then all fixed-aggregate mutations are unavailable until terminal evidence releases the lock while unrelated tenant aggregate locks remain independent.
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

## Design Notes

The evaluator owns no localization or I/O. It returns typed availability, reason, and recovery identifiers from immutable evidence. The page maps those identifiers to whole-string resources and re-evaluates immediately before preview/submit; the gateway and aggregate remain the security and invariant boundaries.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release -warnaserror -m:1 -nr:false` -- expected: warning-clean UI test assembly.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.State.GlobalAdministratorActionAvailabilityTests -class Hexalith.Tenants.UI.Tests.Components.GlobalAdministratorsPageTests -class Hexalith.Tenants.UI.Tests.Components.GlobalAdministratorCorrectionPanelTests -class Hexalith.Tenants.UI.Tests.Services.Gateways.TenantCommandGatewayTests -class Hexalith.Tenants.UI.Tests.Services.Gateways.TenantsBffCompositionTests -class Hexalith.Tenants.UI.Tests.State.TenantAggregateCommandAdmissionGateTests` -- expected: focused guardrail and integration tests pass.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.Services.Gateways.GlobalAdministratorsProjectionLoaderTests -class Hexalith.Tenants.UI.Tests.LocalizerDoubleParityTests -class Hexalith.Tenants.UI.Tests.TenantsUiCompositionTests` -- expected: complete-projection and localization parity gates pass.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` -- expected: full UI suite passes with no skipped or not-run tests.
- `dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false` -- expected: full solution warning-clean.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-4-1-fixed-scope-global-administrator-action-availability.md` -- expected: story file declares every moved gitlink or reports none moved.
- `git diff --check` -- expected: no whitespace errors.

## Auto Run Result

Status: blocked

The auto-build run completed implementation and verification, but the final review found contract-level reconciliation, renderer-loss, and lifecycle defects after the fifth permitted repair loop. The run halted with `review repair loop exceeded 5 iterations (non-convergence)`; no commit was created, and `sprint-status.yaml` was not modified.
