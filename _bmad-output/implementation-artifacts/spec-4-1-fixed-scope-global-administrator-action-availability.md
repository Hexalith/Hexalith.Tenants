---
title: '4.1 Fixed-Scope Global Administrator Action Availability'
type: 'feature'
created: '2026-08-28'
status: 'in-progress'
baseline_commit: '6e070c859b3eddcb21c25380f34065d07b5bac7b'
baseline_revision: '6e070c859b3eddcb21c25380f34065d07b5bac7b'
review_loop_iteration: 2
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
- `src/Hexalith.Tenants.UI/State/TenantAggregateCommandAdmissionGate.cs` and `State/TenantCommandAggregateLock.cs` -- add ownership-safe fixed-aggregate leases and exception-isolated change notifications: failed/repeated acquisition cannot release another attempt; a lease can mark dispatch once; terminal release requires a marked dispatch plus an explicit terminal lifecycle state, while pre-dispatch abandonment uses a separate operation. Degraded, UnableToVerify, refresh, cancel-after-dispatch, close, and component disposal retain both the lease and reconciliation state fail-closed.
- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor` -- obtain a separate bounded complete-population snapshot through `GlobalAdministratorsProjectionLoader` without replacing Story 1.11's paged review rows; collapse the whole privileged surface if that walk returns Unauthorized; require current row freshness/lifecycle and the same stable projection version for visible and complete evidence; evaluate/re-evaluate availability; capture preview rows, freshness, lifecycle, and identity set together and reject submit on any mismatch; preserve message/correlation tracking whenever a nonterminal lease remains held; derive unique DOM associations from rendered row ordinal; pair each reason with its own recovery; react live to viewport/admission/authentication changes; remove the `HasMore` bypass.
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor` -- participate in the same ownership-safe fixed-aggregate lease; require dispatch/status/requery, fresh authorization, and an actual safe viewport measurement before submit or refresh; marshal post-await renderer state through `InvokeAsync`; render reason/recovery from one paired availability decision; prevent close while a tracked attempt is nonterminal and preserve its reconciliation state through disposal; react live to authentication, viewport, and admission changes.
- `src/Hexalith.Tenants.UI/Services/Gateways/GlobalAdministratorsProjectionLoader.cs` -- reject null rows, contradictory Empty/Ready page shapes, oversized pages, mismatched request cursor/page size, duplicate/invalid identities, mixed versions, missing cursors, repeated cursors, and noncurrent lifecycle as incomplete evidence; never normalize contradictory evidence into a successful shape.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`, `TenantsResources.fr.resx`, and `Components/Pages/GlobalAdministratorsPage.razor.css` -- add parity-checked whole strings and reflect measured responsive state without making CSS an evidence source.
- `tests/Hexalith.Tenants.UI.Tests/State/GlobalAdministratorActionAvailabilityTests.cs`, `Components/GlobalAdministratorsPageTests.cs`, `Components/GlobalAdministratorCorrectionPanelTests.cs`, and focused loader/gateway/composition/admission/localization tests -- use PascalCase method names and cover every matrix row/reason precedence; unmeasured-safe viewport; authorization-scoped Empty; multi-page complete loading plus null/contradictory/oversized/wrong-request pages; lifecycle/version-qualified rows; viewport/auth/admission transitions after render; each capability independently; throwing admission subscribers; illegal/terminal lease release; actual page-to-correction and correction-to-page submissions through terminal evidence; cancel/close reconciliation retention; stale preview identity/freshness/version refusal; grant, preview-submit, and unique per-row reason/recovery ARIA references; canonical reason vocabulary/selectors; evaluator purity/support-safe diagnostics.

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
