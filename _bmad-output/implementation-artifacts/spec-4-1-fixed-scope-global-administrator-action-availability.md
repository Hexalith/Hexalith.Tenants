---
title: '4.1 Fixed-Scope Global Administrator Action Availability'
type: 'feature'
created: '2026-08-28'
status: 'in-progress'
baseline_commit: '6e070c859b3eddcb21c25380f34065d07b5bac7b'
baseline_revision: '03566fb1fcd8f8eea5c89130303b676f39d1d6ff'
review_loop_iteration: 3
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
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorActionEvidence.cs`, `GlobalAdministratorActionAvailability.cs`, and `GlobalAdministratorActionAvailabilityEvaluator.cs` -- add one-type-per-file immutable evidence/results and a pure independent grant/remove evaluator with canonical reason precedence; carry `HasMeasurement` separately from viewport state; require authorization-scoped, rowless `Empty`; ignore preview readiness for grant because Story 4.1 has no grant preview, but require actual remove-preview readiness; fail invalid/duplicate identity evidence closed; override diagnostics so UserId/projection metadata cannot appear.
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs`, `TenantCommandGateway.cs`, `UnavailableTenantCommandGateway.cs`, `Abstractions/ITenantsBffComposition.cs`, and `Services/TenantsBffComposition.cs` -- represent fixed-scope dispatch/status/requery capability independently, map each production flag only to its corresponding seam, and fail closed when any required capability is absent.
- `src/Hexalith.Tenants.UI/State/TenantAggregateCommandAdmissionGate.cs` and `State/TenantCommandAggregateLock.cs` -- add ownership-safe fixed-aggregate leases and exception-isolated change notifications: failed/repeated acquisition cannot release another attempt; a lease can mark dispatch once; terminal release requires a marked dispatch plus an explicit terminal lifecycle state, while pre-dispatch abandonment uses a separate operation. Degraded, UnableToVerify, refresh, cancel-after-dispatch, close, and component disposal retain both the lease and reconciliation state fail-closed. Make retained reconciliation adoptable by exactly one replacement surface so it can resume status/projection reconciliation and release only on terminal evidence; merely exposing a read-only handle is insufficient. Serialize retain/adopt/release transitions under a deadlock-safe synchronization order, reject lifecycle regressions, and prove a retention call cannot succeed after terminal release.
- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor` -- obtain a separate bounded complete-population snapshot through `GlobalAdministratorsProjectionLoader` without replacing Story 1.11's paged review rows; collapse the whole privileged surface if that walk returns Unauthorized; require current row freshness/lifecycle and the same stable projection version for visible and complete evidence; evaluate/re-evaluate availability; capture preview rows, freshness, lifecycle, and identity set together and reject submit on any mismatch; preserve message/correlation tracking whenever a nonterminal lease remains held and adopt any retained fixed-scope reconciliation before enabling a new attempt; derive unique DOM associations from rendered row ordinal and associate unavailable row slots as well as rendered controls; pair each reason with its own recovery; react live to viewport/admission/authentication changes; remove the `HasMore` bypass. Evidence selectors must render localized, visibly labelled, qualified scope/freshness/count/admission values and must report missing gate/evidence as unknown rather than available.
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor` -- participate in the same ownership-safe fixed-aggregate lease; require the explicit composition dispatch/status/requery capabilities, a present live authentication provider, fresh authorization, and an actual safe viewport measurement before submit or refresh; retain accepted tracking before any renderer-dependent update; adopt and resume retained reconciliation from a prior surface; marshal post-await renderer state through `InvokeAsync`; serialize refresh entry; generation-guard initialization, authentication transitions, refresh, and submit so stale authorization or projection results cannot redisclose or overwrite newer state; map resolver/provider failures to indeterminate authorization; render and programmatically associate one non-empty paired reason/recovery decision for every disabled confirm state; prevent close while a tracked attempt is nonterminal and preserve its reconciliation state through disposal; react live to authentication, viewport, and admission changes.
- `src/Hexalith.Tenants.UI/Services/Gateways/GlobalAdministratorsProjectionLoader.cs` -- reject null rows, contradictory Empty/Ready page shapes, oversized pages, mismatched request cursor/page size, duplicate/invalid identities, mixed versions, missing cursors, repeated cursors, and noncurrent lifecycle as incomplete evidence; never normalize contradictory evidence into a successful shape. Any later-page Unauthorized result collapses all accumulated rows and provenance to the canonical unauthorized result.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`, `TenantsResources.fr.resx`, and `Components/Pages/GlobalAdministratorsPage.razor.css` -- add parity-checked whole strings and reflect measured responsive state without making CSS an evidence source. Use Fluent UI V5 text components where available; do not add raw paragraph markup for reason/recovery copy.
- `tests/Hexalith.Tenants.UI.Tests/State/GlobalAdministratorActionAvailabilityTests.cs`, `Components/GlobalAdministratorsPageTests.cs`, `Components/GlobalAdministratorCorrectionPanelTests.cs`, and focused loader/gateway/composition/admission/localization tests -- use PascalCase for every new or renamed method and cover every matrix row/reason precedence; unmeasured-safe viewport; authorization-scoped Empty; multi-page complete loading plus null/contradictory/oversized/wrong-request pages with canonical fail-closed result shape and bounded call count; later-page authorization collapse with no retained identity; lifecycle/version-qualified rows; viewport/auth/admission transitions and denied-to-authorized recovery after render; out-of-order authorization and refresh completion; each capability independently; throwing admission subscribers; atomic/monotonic retain-adopt-release behavior; actual submitted page-to-correction and correction-to-page reconciliation adoption through terminal evidence; cancel/close reconciliation retention; stale preview identity/freshness/version refusal; localized labelled evidence values and every unavailable branch; grant, preview-submit, correction-submit, and unique per-row reason/recovery ARIA references with asserted text; canonical reason vocabulary/selectors; evaluator purity/support-safe diagnostics.

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
