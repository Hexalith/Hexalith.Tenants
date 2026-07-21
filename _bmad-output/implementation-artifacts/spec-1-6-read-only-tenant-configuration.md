---
title: 'Story 1.6: Read-Only Tenant Configuration'
type: 'feature'
created: '2026-07-21'
status: ready-for-dev
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
warnings: []
---

<intent-contract>

## Intent

**Problem:** The current tenant configuration UI receives the complete configuration dictionary, filters sensitive-looking values only after they enter component state, exposes full entry/group counts, and embeds set/remove controls inside the nominally read-only region. This does not satisfy the current Story 1.6 contract that unauthorized namespaces and undefined-policy values never reach component state or rendered output.

**Approach:** Introduce an authoritative server-side BFF policy registry, supplied through deployment-owned server configuration, that emits only explicitly authorized, explicitly display-safe configuration entries; make `TenantConfigurationView` consume that safe view model and remain strictly read-only; preserve later Epic 3 commands in a separate sibling management landmark within the same Configuration accordion without weakening their projection-confirmed lifecycle.

## Boundaries & Constraints

**Policy Authority:** A typed server-side BFF policy registry loaded from deployment-owned configuration is the sole source for ordinary-user prefix grants and positive display-safe decisions. Prefix grants are keyed by literal tenant id plus authenticated `sub`; `TenantOwner`, `TenantContributor`, and `TenantReader` receive only their explicitly registered grants, and role alone grants no prefix. A server-reflected global administrator receives wildcard namespace access. Prefix and key comparisons are ordinal and case-sensitive with no trimming or normalization: grant `P` authorizes only key `P` or keys beginning `P.`. The positive display policy is an exact full-key registry owned by the relevant consumer/deployment; only registered full keys are `DisplaySafe`. Missing, malformed, duplicate-conflicting, or unregistered policy is `Unknown` and fails closed.

**Always:** Apply namespace authorization and positive display-safe classification in the server-side BFF before constructing the component-facing safe model; omit the entire entry, including its key and value, unless both checks pass; derive summaries, announcements, filters, and empty states only from that safe model; preserve literal caller-supplied keys, opaque freshness metadata, EN/FR whole-string parity, stable selectors, multi-expand accordion composition, and distinct honest read states.

**Block If:** The BFF cannot bind and validate the typed policy registry, cannot obtain the authenticated `sub`, or cannot prove server-reflected global-administrator status. These conditions produce an empty safe read model and make configuration management unavailable with localized recovery; they never fall back to the raw dictionary, tenant role, visible keys, entered prefixes, or the blacklist classifier.

**Never:** Infer prefix ownership from visible keys, ordinary tenant membership/role, client-only claims, or absence in a projection; treat the explicit global-administrator wildcard rule as evidence for any other caller; use blacklist-negative values as proof of display safety; leak hidden entry names/counts/existence; add a reveal/masking contract; broaden into Story 1.10 direct-read transport work; remove or weaken Epic 3 command safety; modify `references/` submodules.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Explicit ordinary-user grant | Authenticated owner, contributor, or reader plus registry grant `(tenantId, sub, P)` | Only exact key `P` and keys beginning `P.` are namespace-authorized; role alone adds nothing | Missing or invalid grant yields no entries for that prefix |
| Global administrator | Server-reflected global administrator opens any authorized tenant detail | All literal namespace prefixes are authorized, but values still require positive exact-key display policy | Indeterminate administrator reflection fails closed |
| Authorized safe entries | Current tenant detail plus proven caller prefixes and an exact full-key `DisplaySafe` registration | Only entries passing both checks reach the component, grouped by literal namespace | No error expected |
| Mixed authorization | Raw detail contains allowed and hidden prefixes | Hidden namespace names, keys, values, counts, and existence never enter the safe view model or UI-derived text | Render only the authorized subset |
| Undefined, sensitive, or unregistered value | Key is namespace-authorized but its exact full key is not positively registered `DisplaySafe` | The entire entry is absent from component state, DOM, copy, announcements, logs, and telemetry | Do not render the key, a placeholder row, or a reveal control |
| Missing or malformed registry | Registry binding/validation fails or contains conflicting declarations | Empty safe read model; no raw entry-derived summary or distinction from hidden data | Localized unavailable/recovery state; management actions unavailable |
| No visible entries | Authorization-safe composition returns no entries | Localized empty state reveals nothing about hidden configuration | Do not distinguish absent from hidden entries |
| Non-current read | Loading, stale, degraded, unavailable, or unknown freshness | Distinct localized state with supported recovery; no false current/success state | Continue read-only only when a qualifying last-confirmed safe model exists |
| Management composition | Caller opens configuration commands from tenant detail | A sibling management landmark in the same Configuration accordion owns set/remove entry points; `TenantConfigurationView` remains control-free | Preserve authorization, preview, focus, locking, confirmation, and recovery behavior |

</intent-contract>

## Code Map

- `src/Hexalith.Tenants/Queries/Handlers/GetTenantQueryHandler.cs` -- currently copies every projected configuration entry after tenant-wide authorization.
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs` -- retains the unfiltered `TenantDetail` and maps read freshness.
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantsBffComposition.cs` -- current BFF seam has no configuration authorization/display-policy contract.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor` -- accordion composition, full-dictionary summary leak, and command-flow wiring.
- `src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor` -- current grouping/filtering plus post-state blacklist redaction and embedded mutation controls.
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs` -- existing configuration, accessibility, state, CSS, and localization coverage.

## Tasks & Acceptance

**Execution:**
- `src/Hexalith.Tenants.UI/Services/Gateways/` and `src/Hexalith.Tenants.UI/State/TenantDetail/` -- add typed binding/validation for the deployment-owned BFF policy registry and the approved caller-prefix/value-display composition, producing a dedicated safe read model before Razor component state. Default configuration contains no ordinary-user grants and no display-safe keys.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor` -- derive configuration summary/state only from the safe model and preserve Story 1.10 transport boundaries.
- `src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor` -- consume only the safe model, remove mutation affordances from the read-only region, and preserve grouping, filtering, responsive accessibility, freshness, and stable selectors.
- `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/` -- relocate existing Epic 3 set/remove entry points to a separate sibling management landmark within the same Configuration accordion. Set obtains ordinary-user prefixes from the registry (or the explicit global-administrator wildcard); remove launches only from keys in the authorized safe model. Preserve availability, preview, focus, locking, confirmation, and recovery behavior.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` and `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx` -- add or revise whole-string state/recovery copy with exact parity.
- `tests/Hexalith.Tenants.UI.Tests/` -- prove pre-component filtering, absence of hidden and raw sensitive values from state/markup/announcements/copy, authorization-safe emptiness/counts, strict read-only markup, mutation-flow preservation, distinct truth states, Unicode/long literals, accessibility, responsive hooks, resources, and Fluent conformance.

**Acceptance Criteria:**
- Given an authenticated owner, contributor, or reader, when the BFF resolves namespace scope, then only ordinal prefix grants keyed by the literal tenant id and authenticated `sub` authorize keys; tenant role alone authorizes none.
- Given a server-reflected global administrator, when the BFF resolves namespace scope, then all namespaces are authorized by the explicit wildcard rule, while indeterminate reflection fails closed.
- Given mixed authorized and unauthorized namespaces, when the BFF composes configuration, then only entries whose exact full key is positively registered `DisplaySafe` and whose namespace is authorized reach component state, and no UI text reveals the hidden subset.
- Given a sensitive, unregistered, conflicting, or otherwise undefined-policy value, when the configuration region renders, then its key and raw value are absent from component state, markup, announcements, copy inputs, logs, and telemetry.
- Given missing or malformed policy configuration, when composition runs, then the safe model is empty, management actions are unavailable, and no raw dictionary fallback occurs.
- Given no visible entries, when a successful read renders, then an EN/FR authorization-safe empty state does not imply whether hidden configuration exists.
- Given each supported read/freshness state, when the tenant detail renders, then its localized recovery remains distinct and never infers freshness from `ServedAt` or unrelated projection data.
- Given the read-only region markup, when inspected, then it contains no set/remove controls, forms, editable cells, or command lifecycle content, while a separate sibling management landmark in the same Configuration accordion retains the Epic 3 flows and their required behavior.
- Given long or Unicode literals and desktop/tablet/mobile layouts, when rows render, then namespace context, semantic relationships, keyboard order, wrapping/overflow, forced-colors meaning, and stable selectors remain usable.

## Spec Change Log

- 2026-07-21: Human escalation resolution selected the explicit BFF policy-registry option: per-tenant/per-subject ordinary-user prefix grants, explicit global-administrator wildcard scope, exact-key positive display-safe registration with fail-closed omission, and sibling read/management landmarks within the Configuration accordion.

## Review Triage Log

## Design Notes

The human selected a deployment-owned, typed server-side BFF policy registry as the source of truth. It is not a new browser claim or projection field. Ordinary tenant roles do not imply namespace ownership; grants use `(tenantId, sub, prefix)`, while trusted global-administrator reflection supplies the one explicit wildcard rule. Display safety is a positive exact-full-key registration owned by consumers/deployment, never a content blacklist. Missing or invalid policy produces no safe entries.

Later Epic 3 stories intentionally added set/remove flows to `TenantConfigurationView`; current Story 1.6 requires the read region itself to contain no mutation controls. Keep tenant context by composing a distinct read landmark and a sibling management landmark inside the same Configuration accordion. Management consumes the same proven scope, while its command-specific state remains separate from the read-safe model and retains all existing Story 3.5/3.6 lifecycle guarantees.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore -warnaserror -m:1 -nr:false` -- expected: zero warnings and errors.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.Components.TenantDetailSurfaceTests` -- expected: all focused configuration tests pass.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.Services.Gateways.TenantQueryGatewayTests` -- expected: all BFF/freshness tests pass.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.DomainUiFluentConformanceTests` -- expected: all Fluent/FrontComposer governance tests pass.

