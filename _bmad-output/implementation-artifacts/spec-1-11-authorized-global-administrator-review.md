---
title: 'Authorized Global Administrator Review'
type: 'feature'
created: '2026-07-28'
status: 'in-review'
baseline_commit: '2e61f57bda6379192007d1bc6fabbde61996b11d'
baseline_revision: '2e61f57bda6379192007d1bc6fabbde61996b11d'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-ux-instructions.md'
warnings:
  - oversized
---

<intent-contract>

## Intent

**Problem:** The fixed-scope global-administrator read exists, but the interactive UI can expose scope and command structure after authorization becomes indeterminate or the server denies a stale authorization reflection. It also presents unknown freshness too affirmatively, lacks an authorized contextual entry and safe return, and can mistake one cursor page for complete evidence.

**Approach:** Harden the existing review rather than create another read contract: use one strict circuit-aware principal interpretation for policy and BFF reflection, keep the server REST endpoint authoritative, gate all privileged markup and subscriptions on confirmed authorization, add contextual navigation, and represent freshness, recovery, and paging honestly.

## Boundaries & Constraints

**Always:** Keep the fixed `system / global-administrators / global-administrators` identity and server-only `GET /api/global-administrators`; use opaque requester-bound cursors, literal authorized user IDs, FrontComposer/Fluent V5, EN/FR whole strings, stable selectors, and support-safe diagnostics. Preserve the single `/tenants` shell registration and existing desktop grant/remove regressions, but require current complete projection evidence before list-wide mutation decisions.

**Block If:** Correctness requires a new backend endpoint, exposing protected metadata, treating `ServedAt` as projection age, or changing a shared platform/submodule contract.

**Never:** Do not enumerate global administrators through tenant/member reads, the generic EventStore query route, browser-side backend calls, Memories, or client filtering. Do not reveal identities, counts, freshness, fixed-scope literals, route details, or mutation affordances to unauthorized/indeterminate callers; infer totals from a page; expose cursors/ETags/tokens; or claim SignalR/HTTP success proves current projection state. Do not widen this story into AppHost composition; report the authorized live-host lane as unavailable if `Tenants:BaseAddress` remains absent.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Authorized review | Strict circuit principal and current REST projection page | Contextual entry opens the review; literal IDs, platform scope, current truth, safe copy, and protected paging render | No error expected |
| Authorization denied | Indeterminate/non-admin reflection, or server 401/403 after optimistic reflection | Generic unavailable/denied surface only; no privileged query when reflection fails closed, no rows/scope/commands/subscription | Dispose any lease and discard retained privileged state |
| Untrusted truth | Unknown, stale, degraded, missing metadata, or transport failure | Distinct non-affirmative state; previously confirmed rows may remain labelled honestly; mutations fail closed | Offer localized retry/reset without leaking diagnostics |
| Incomplete page | `HasMore`, later page, invalid cursor, or racing page/notification loads | Next/Previous are serialized; late results do not apply; no page count becomes a global total | Recover page one honestly and announce refresh |
| Narrow viewport | Authorized mobile review | Read-only rows and recovery remain usable | Hide/disable high-impact controls with a localized reason |

</intent-contract>

## Code Map

- `src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationPrincipalResolver.cs` and `src/Hexalith.Tenants.UI/Services/Gateways/TenantsGlobalAdministratorClaims.cs` -- competing principal interpretations; consolidate strict request/circuit evidence.
- `src/Hexalith.Tenants.UI/Services/Gateways/{ITenantsBffComposition,TenantsBffComposition}.cs` -- authorization reflection consumed by navigation and page gating.
- `src/Hexalith.Tenants.UI/Components/{Routes.razor,Pages/TenantsWorkspace.razor,Pages/GlobalAdministratorsPage.razor}` -- route policy, contextual entry/return, privileged rendering, paging, refresh, and commands.
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/` and `Services/Gateways/TenantQueryGateway.cs` -- truth-state, recovery, completeness, and support-safe diagnostic model.
- `tests/Hexalith.Tenants.UI.Tests/` -- outer component, principal, gateway, localization, conformance, and notification evidence.

## Tasks & Acceptance

**Execution:**
- `src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationPrincipalResolver.cs`, `src/Hexalith.Tenants.UI/Services/Gateways/TenantsGlobalAdministratorClaims.cs`, `src/Hexalith.Tenants.UI/Services/Gateways/{ITenantsBffComposition,TenantsBffComposition}.cs`, and `src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs` -- make one strict tri-state evaluator work for HTTP requests and Blazor circuits; reject malformed/conflicting identities and keep server denial final.
- `src/Hexalith.Tenants.UI/Components/Routes.razor` and `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor` -- enforce the registered policy, show one authorized contextual entry, and carry only a local canonical `/tenants...` return URL.
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorsSnapshot.cs`, `GlobalAdministratorsSurfaceKind.cs`, `GlobalAdministratorsReason.cs`, and `GlobalAdministratorsRequest.cs`, plus `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs` -- add explicit unknown/error/recovery and page-completeness semantics; bound `ToString()` output so rows, cursor, ETag, projection version, and identities cannot escape diagnostics.
- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor` and `.razor.css` -- validate return navigation; render privileged structure only while authorized; unsubscribe and clear it on denial; add retry/reset, Previous/Next race protection, non-affirmative truth copy, exact literal display/copy, and mobile read-only behavior without legacy tokens.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` and `.fr.resx` -- add parity-checked whole strings for entry/return, denied, unknown/error, recovery, incomplete evidence, and mobile read-only states.
- `tests/Hexalith.Tenants.UI.Tests/{Components/GlobalAdministratorsPageTests.cs,TenantsWorkspaceTests.cs,Services/Configuration/TenantConfigurationReadPolicyTests.cs,Services/Gateways/TenantQueryGatewayTests.cs,Services/Gateways/TenantsBffCompositionTests.cs,DomainUiFluentConformanceTests.cs}` -- cover the matrix at the rendered UI/BFF boundary, including stale reflection plus server denial, circuit fallback, exact IDs, late-load rejection, notification disposal, cursor history, localization, support safety, and responsive controls.
- `tests/Hexalith.Tenants.Server.Tests/Queries/GetGlobalAdministratorsQueryHandlerTests.cs` and `tests/Hexalith.Tenants.IntegrationTests/TenantsApiGeneratedControllerTests.cs` -- retain fixed-scope authorization/cursor and outer HTTP no-payload denial evidence without widening the API.

**Acceptance Criteria:**
- Given an authorized operator on a canonical Tenants workspace URL, when they enter and return from Global Administrators, then the exact safe workspace context is restored and no second shell entry or external return target is possible.
- Given authorization is indeterminate, malformed, non-admin, or denied by the server, when the route is requested or authorization changes, then no privileged identity, count, freshness, fixed-scope detail, command structure, subscription, or retained row is observable.
- Given current, stale, unknown, degraded, empty, invalid, and unavailable REST outcomes, when the review renders or recovers, then each state has distinct localized truth and recovery behavior, and only authoritative current evidence enables safety-sensitive actions.
- Given cursor paging, notifications, or rapid retries overlap, when completions arrive out of order, then only the newest scoped request renders, cursor history stays valid, and neither cursors nor inferred totals appear in URL, DOM, clipboard, logs, or diagnostics.
- Given an authorized narrow viewport, when the page renders, then the review, paging, copy, focus, and live-region behavior remains usable while grant/remove actions are visibly unavailable.

## Spec Change Log

## Review Triage Log

## Design Notes

The registered policy controls discoverability and route presentation, while `GET /api/global-administrators` remains the final data authorization boundary. A claim-authorized caller can still be denied safely by the server; that denial must collapse the page to generic output before any subscription or privileged markup persists.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release -warnaserror -m:1 -nr:false` -- expected: warning-clean build.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.Components.GlobalAdministratorsPageTests -class Hexalith.Tenants.UI.Tests.TenantsWorkspaceTests -class Hexalith.Tenants.UI.Tests.Services.Configuration.TenantConfigurationReadPolicyTests -class Hexalith.Tenants.UI.Tests.Services.Gateways.TenantQueryGatewayTests -class Hexalith.Tenants.UI.Tests.Services.Gateways.TenantsBffCompositionTests -class Hexalith.Tenants.UI.Tests.DomainUiFluentConformanceTests` -- expected: all focused tests pass.
- `dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Release -warnaserror -m:1 -nr:false && tests/Hexalith.Tenants.Server.Tests/bin/Release/net10.0/Hexalith.Tenants.Server.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.Server.Tests.Queries.GetGlobalAdministratorsQueryHandlerTests` -- expected: fixed-scope authorization tests pass.
- `dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false` -- expected: solution builds with no warnings; record the exact asset-graph blocker if the known environment issue recurs.
- `rg -n "SubmitQueryAsync|/api/v1/queries|QueryRouter|HandlerAwareQueryRouter" src/Hexalith.Tenants.UI` -- expected: no production generic-query read path.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-1-11-authorized-global-administrator-review.md` -- expected: exit 0.
