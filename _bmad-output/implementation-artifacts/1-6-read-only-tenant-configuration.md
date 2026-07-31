---
baseline_commit: b73093bd10608afe4e6036439a48a08924d0358b
---

# Story 1.6: Read-Only Tenant Configuration

Status: in-progress

<!-- 2026-07-27 code review: 2 decisions resolved, 39 of 39 patches closed, two items deferred. The last open item
(empty-array vs empty-scalar DisplaySafe) was resolved as Decision 3 below: accepted as a bounded, test-pinned
limitation rather than traded for a redundant cardinality discriminator. No spec change was required. -->

<!-- Note: This corrective story supersedes the June 2026 implementation context in
1-6-read-only-tenant-configuration-view.md. Preserve that file as historical evidence. -->

## Story

As an authorized tenant user,
I want to inspect the tenant configuration namespaces I am allowed to see,
so that I can understand operational configuration without exposing other consumers' or sensitive values.

## Acceptance Criteria

1. **Given** an authorized user opens a tenant detail surface, **when** the configuration region loads, **then** visible key/value pairs are grouped by consumer-owned namespace using Fluent/FrontComposer read-only composition, **and** the region is clearly presented as inspection rather than an editable form.
2. **Given** configuration contains keys inside and outside the caller's authorized namespace prefixes, **when** the server-side BFF composes the view model, **then** only authorized namespace entries reach component state and rendered output, **and** hidden namespace names, key counts, key names, values, and existence cannot be inferred from empty or summary text.
3. **Given** a value is classified as sensitive or its display policy is undefined, **when** configuration is rendered, **then** the value is not displayed, copied, announced, logged, or serialized into component state, **and** no reveal control or implicit masking contract is invented in v1.
4. **Given** the caller has no visible configuration entries, **when** the read completes successfully, **then** an authorization-safe localized empty state is shown rather than an error, **and** it does not imply whether hidden configuration exists.
5. **Given** the configuration read is loading, unavailable, stale, degraded, or has unknown freshness, **when** the state is displayed, **then** each condition remains honest and distinct with the appropriate refresh, retry, or continue-read-only recovery, **and** parent/detail freshness is not inferred from `ServedAt` or from unrelated projection data.
6. **Given** the tenant detail contains multiple titled content regions, **when** configuration is composed with sibling overview or membership regions, **then** it follows the FrontComposer page-layout contract and uses `FluentAccordion` with multi-expand behavior where the project UX rules require grouping, **and** the only primary content region is never hidden by default.
7. **Given** the configuration region is read-only, **when** its rendered markup is inspected, **then** it contains no raw or Fluent mutation controls, raw forms, editable cells, or misleading command affordances, **and** semantic key/value relationships, headings, focus order, and state announcements remain accessible.
8. **Given** long, Unicode, or visually similar namespace keys and values, **when** the region renders at desktop, tablet, and mobile widths, **then** literal text remains distinguishable, safely wrapped or horizontally available without dropping namespace context, **and** no layout style or truncation exposes hidden data or makes safety-critical state disappear.
9. **Given** English and French resources, **when** headings, namespaces, empty, loading, error, stale, degraded, unknown, and recovery text render, **then** whole-string parity and culture-aware formatting are preserved, **and** stable selectors identify regions and states without depending on keys, values, localized text, or color.
10. **Given** the completed read-only configuration slice, **when** focused authorization, namespace-filtering, sensitive-value, gateway, bUnit, localization, responsive, accessibility, support-safety, and Fluent-conformance tests run, **then** visible, hidden, empty, error, and malicious/edge-case value scenarios pass, **and** exact commands, results, and any unresolved sensitive-display policy are recorded without broadening scope.

## Tasks / Subtasks

- [x] Add the typed, deployment-owned configuration-read policy and fail-closed BFF composition seam (AC: 2, 3, 4, 5, 10)
  - [x] Define Tenants-owned `Tenants:ConfigurationReadPolicy` options and runtime semantic validation under `src/Hexalith.Tenants.UI/Services/Gateways/` (or a focused `Services/Configuration/` subfolder). Add one idempotent registration extension and call it from both `Program.cs` and `TenantsUiServiceCollectionExtensions.cs`; include an explicit valid-empty section in `appsettings.json`. Do not put grants or safe-value decisions in browser code, a public query contract, or `references/`.
  - [x] Model ordinary-user grants as literal `(tenantId, authenticatedSub, prefix)` entries. Role alone grants no namespace. Obtain the subject from the server-side `IUserContextAccessor`; never trust an entered prefix, a browser-only claim, or visible projection keys as authorization evidence.
  - [x] Add a configuration-policy-specific administrator reflection with three outcomes: proven global administrator grants wildcard scope; proven non-administrator evaluates only explicit ordinary-user grants; missing/malformed/ambiguous principal evidence is indeterminate and makes configuration unavailable. Do not reuse the current two-outcome `GlobalAdministratorsAuthorizationReflection` in a way that blocks ordinary users or turns claim absence/malformation into wildcard access.
  - [x] Compare tenant ids, subjects, prefixes, and configuration keys with `StringComparison.Ordinal`, without trimming, case folding, Unicode normalization, GUID/ULID parsing, or delimiter rewriting. A non-empty, non-whitespace prefix that does not end in `.` authorizes only exact key `P` or keys beginning `P.`. When multiple grants match, the longest ordinal prefix is the consumer namespace; reject duplicate grants, duplicate safe-key declarations, and conflicting declarations during semantic validation. Add boundary tests so `a`, `a.`, `ab`, `A`, leading/consecutive empty segments, and visually confusable prefixes cannot broaden scope.
  - [x] Model display approval as a positive exact-full-key `DisplaySafe` registry whose keys are non-empty literal strings. A key must pass both namespace authorization and exact-key display approval before a component-facing row is constructed. Missing, malformed, conflicting, unregistered, or indeterminate policy omits the complete entry, including its key and value; a blacklist-negative result is never approval.
  - [x] Bind the section without startup-fatal validation. The semantic validator returns a valid policy or an unavailable policy result at composition time; it must catch safe binding/validation failures and never expose exception details. `ValidateOnStart` is prohibited for this section because malformed deployment policy must render the required localized unavailable/recovery state rather than terminate the host.
  - [x] Return an explicit safe composition result that distinguishes valid-empty from policy/authentication unavailable. Error details, policy contents, hidden literals, raw counts, and configuration values must not enter messages, logs, metrics, `ToString()` output, or telemetry.

- [x] Ensure raw configuration is transient inside the server-side BFF and never enters any Razor component state (AC: 2, 3, 4, 5)
  - [x] Apply the policy after the server-side tenant-detail response is received but before `TenantDetailSnapshot`, a page view model, a Razor parameter, or a projection-evidence callback result is constructed. Add dedicated safe read and management DTOs under `src/Hexalith.Tenants.UI/State/TenantDetail/` (or `State/TenantConfiguration/`) containing only authorized/display-safe rows, tenant identity needed by commands, proven prefixes, and policy-safe state/recovery metadata.
  - [x] Update `TenantQueryGateway.GetTenantAsync` and/or its dedicated composition dependency so ready, stale, degraded, `304`, and last-confirmed paths retain only previously composed safe rows. Replace the component-facing raw `TenantDetail` payload with a safe tenant-detail view model, or otherwise prove its configuration member contains only composed safe rows. Never fall back to `TenantDetail.Configuration` when the policy, subject, or administrator reflection is missing or invalid.
  - [x] Keep `GetTenantQuery` / `GET /api/tenants/{tenantId}` as the existing read source. Do not add a configuration endpoint, change `TenantDetail` public contracts, move filtering into the browser, read EventStore state directly, or absorb Story 1.10 direct-read/provenance work.
  - [x] Treat a valid policy with zero visible rows as successful authorization-safe empty. Treat missing/malformed policy, missing authenticated subject, indeterminate policy-specific administrator reflection, and composition failure as an explicit localized unavailable state—not as empty, current, or successful. Map initial transport/policy errors to localized unavailable; map a failed refresh with a qualifying last-confirmed safe model to degraded/continue-read-only. Add focused tests for both error paths and their announcement/recovery copy.
  - [x] Derive every configuration summary, group, filter result, announcement, count, management target list, and empty-state decision from the safe model only. Remove the raw `Detail.Configuration.Count` and raw prefix-count summary path from `TenantDetailPage.razor`.

- [x] Make `TenantConfigurationView` a strict read-only consumer of the safe model (AC: 1, 4, 5, 6, 7, 8, 9)
  - [x] Change `TenantConfigurationView.razor` to accept only the safe configuration read model. Remove `Detail`, set/remove projection delegates, command-availability parameters, command state, per-row action cells, and all `SetTenantConfigurationFlow` / `RemoveTenantConfigurationFlow` composition from this read landmark.
  - [x] Remove `LegacyConfigurationDisplaySanitizer` from every configuration-read path. It is still used by the existing Epic 3 command previews; keep any command-only use isolated and explicitly transitional unless the positive policy can replace it without weakening or broadening those flows. It must never act as a fallback, display approval, safety badge source, or copy classifier for the read model.
  - [x] Keep the proven Fluent/FrontComposer patterns that remain applicable: visible inspection-only title/description, explicit truth state, authorization-safe empty and filtered-empty states, a scan/filter control that is not presented as editing, multi-expand namespace groups, `FluentDataGrid`, and stable `tenants-config-read-*` selectors.
  - [x] Preserve semantic namespace/key/value relationships and accessible headings. Use literal text rendering, logical focus order, dedicated announcement intent, visible focus, reduced-motion compatibility, and forced-colors-safe meaning; no selector, DOM id, or focus target may be derived from a raw key/value or localized string.
  - [x] Preserve long, empty-segment, reserved-character, markup-like, bidi/Unicode, and visually confusable literals without transformation. Use safe wrapping or horizontal availability at 320–767, 768–1023, 1024+, and 1440+ widths; never drop namespace, value-safety, or truth-state context.
  - [x] Render loading, unavailable, stale, degraded, unknown, and mapped error states distinctly. Read-unavailable, routine results, and recovery progress use polite announcements; assertive is reserved for rejection, failure, unable-to-verify, degraded, or destructive-block intent. Continue-read-only may show only a qualifying last-confirmed safe model.

- [x] Preserve Epic 3 set/remove behavior in a separate management landmark (AC: 2, 3, 6, 7, 10)
  - [x] Add a sibling Tenants-owned configuration-management component under `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/` and compose it beside—not inside—the read-only landmark within the existing expanded Configuration accordion item on `TenantDetailPage.razor`.
  - [x] Move the existing `SetTenantConfigurationFlow` and `RemoveTenantConfigurationFlow` entry points into that management landmark, then replace their component-facing `TenantDetail` inputs and `Task<TenantDetail?>` projection callbacks with safe management context and proof-only projection-evidence DTOs. No Razor component—including management—may receive a server-returned raw configuration dictionary. Preserve Story 3.5/3.6 authorization, projection freshness, complete preview, focus return, narrow-layout fail-closed behavior, aggregate-scoped command locking, duplicate prevention, projection confirmation, audit/recovery states, and refresh callbacks.
  - [x] Let set operations use only proven ordinary-user prefixes or the explicit global-administrator wildcard from the same policy result. Let remove operations target only keys in the current safe read model. Missing/invalid policy, stale/unknown/degraded truth, or absent safe targets keeps management unavailable with one localized, programmatically associated reason.
  - [x] Perform set/remove projection comparison inside the server-side BFF against the transient raw response, then return only boolean/version/lifecycle proof needed by the command state machine. A set key/value that is not positively `DisplaySafe` may be submitted under the proven command scope but must remain absent from read/component state; confirmation or no-op proof must not echo the projected key/value. Removal proof likewise returns presence/absence status without a raw dictionary.
  - [x] Keep read and management state separate: command lifecycle updates, preview values, validation, and action announcements must not mutate the safe read model or appear inside `tenants-config-read-*` markup. Do not add configuration-value copy or reveal controls; the existing Story 1.8 certification remains a separate follow-up.

- [x] Update localized resources, documentation evidence, and focused regression coverage (AC: 1–10)
  - [x] Add/revise whole-string `Tenants.Configuration.*` resources in `TenantsResources.resx` and `TenantsResources.fr.resx` for inspection-only copy, policy-unavailable recovery, authorization-safe empty, truth states, and the separate management landmark. Preserve exact EN/FR key parity and named placeholders; do not assemble sentences from fragments.
  - [x] Add pure policy/composer tests for ordinary grants, proven non-administrator handling, indeterminate administrator reflection, global-administrator wildcard, longest-prefix matching, rejected invalid/trailing-dot/duplicate declarations, exact full-key approval, ordinal/case-sensitive boundaries, missing/malformed/conflicting policy, zero visible entries, hidden counts/existence, and safe last-confirmed/`304` handling.
  - [x] Update `TenantQueryGatewayTests` and `TenantDetailSurfaceTests`; add focused tests if clearer. Prove that forbidden and undefined-policy keys/values are absent from snapshot/component state, DOM, accessible names, announcements, filter text, copy inputs, logs, telemetry, exception strings, and management targets—not merely visually redacted.
  - [x] Preserve and re-run `SetTenantConfigurationFlowTests` and `RemoveTenantConfigurationFlowTests` to prove relocation did not weaken Epic 3 behavior. Add assertions that read markup contains no action column, buttons, inputs that mutate data, forms, command lifecycle content, or misleading command affordances.
  - [x] Cover valid-empty versus policy-unavailable, initial error-to-unavailable and failed-refresh-to-degraded mapping, loading/stale/degraded/unknown/unavailable, overlap/boundary prefixes, long/markup-like/bidi/Unicode/confusable values, EN/FR parity, stable data-independent selectors, keyboard/focus/live-region behavior, table semantics, responsive overflow, forced colors, reduced motion, and `DomainUiFluentConformanceTests`.
  - [x] Update `tests/test-summary.md` and the Dev Agent Record with exact commands/results, current package evidence, NFR10 accessibility/localization/responsive/documentation evidence, and any genuinely unresolved display-policy decision. Close or update the Story 1.8 `CFG-1.6-SAFE-MODEL` deferred evidence only when the positive model is proven; do not claim configuration clipboard certification automatically.

## Dev Notes

### Corrective Story Context

- This is a corrective reopening of Story 1.6. The historical `1-6-read-only-tenant-configuration-view.md` and commit `32366bc` implemented the June contract and remain historical evidence; they are not proof that this July contract is complete. The resolved `spec-1-6-read-only-tenant-configuration.md` is the authoritative implementation kernel. [Source: `_bmad-output/implementation-artifacts/1-6-read-only-tenant-configuration-view.md`; `_bmad-output/implementation-artifacts/spec-1-6-read-only-tenant-configuration.md`]
- The stale `bmad-dev-auto-result-1-6-read-only-tenant-configuration.md` dirty-tree blocker was resolved by commit `c0451de`; do not copy it into implementation status. The current work is scoped by this story and normal repository rules. [Source: `git show c0451de`; `_bmad-output/implementation-artifacts/bmad-dev-auto-result-1-6-read-only-tenant-configuration.md`]
- Story 1.5's key regression lesson applies directly: every new policy/freshness state needs an explicit render path and regression test so it cannot fall through to false empty or false success. Preserve literal ids, BFF-only access, selector/resource separation, and full EN/FR parity. [Source: `_bmad-output/implementation-artifacts/1-5-user-membership-lookup.md#Senior Developer Review (AI)`]

### Authoritative Policy Contract

- The deployment-owned typed BFF registry is the sole source for ordinary-user prefix grants and positive display approval. Ordinary roles grant nothing by themselves. The one wildcard rule belongs only to server-proven global administrators. [Source: `_bmad-output/implementation-artifacts/spec-1-6-read-only-tenant-configuration.md#Intent`; `#Boundaries & Constraints`]
- Prefix comparison is ordinal and case-sensitive: `P` authorizes `P` and `P.*`, not `Prefix`, `p`, or a normalized equivalent. Display approval is an exact full-key positive decision. Both gates must pass before a safe row exists. Undefined or invalid policy omits the whole entry; masking and deny-list heuristics are prohibited. [Source: `_bmad-output/implementation-artifacts/spec-1-6-read-only-tenant-configuration.md#I/O & Edge-Case Matrix`]
- A present, semantically valid `Tenants:ConfigurationReadPolicy` section with empty grant/safe-key arrays is the repository default and produces no ordinary-user rows. A missing, unbindable, or semantically invalid section produces an explicit safe unavailable result. Reject empty/whitespace/trailing-dot prefixes and duplicate/conflicting grant or safe-key declarations; use longest matching ordinal prefix when valid grants overlap. Keep these outcomes distinct so deployment mistakes cannot masquerade as authorization-safe empty data. [Source: `_bmad-output/implementation-artifacts/spec-1-6-read-only-tenant-configuration.md#Tasks & Acceptance`]
- ASP.NET Core's typed options pattern supports binding and custom validation; normal options validation throws on access, while `ValidateOnStart` moves failure to startup. For this policy, bind and validate through a safe runtime policy provider that translates binding/semantic failure to the required unavailable result. Do not use `ValidateOnStart`, and do not expose exception or policy details to the user or logs. [Source: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-10.0#options-validation]

### Current Implementation Gaps To Remove

- `TenantQueryGateway.GetTenantAsync` currently retains the unfiltered `TenantDetail` returned by the tenant-detail query; `TenantDetailSnapshot` can therefore carry the raw dictionary into page state. Set/remove projection callbacks also return `Task<TenantDetail?>`. The new composition and command confirmation comparison must happen before either payload crosses the Razor boundary; page/read/management components receive only safe view models and proof-only evidence. [Source: `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`; `src/Hexalith.Tenants.UI/State/TenantDetail/TenantDetailSnapshot.cs`; `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`]
- `TenantConfigurationView.razor` currently builds rows from `Detail.Configuration`, calls `LegacyConfigurationDisplaySanitizer` only while creating rendered rows, always exposes keys, derives announcements from those rows, and embeds set/remove controls. `TenantDetailPage.razor` separately exposes raw entry and prefix counts. Every one of these raw-derived read paths must use the safe model or be removed. The sanitizer also serves existing set/remove previews, so isolate any retained command-only use and do not mistake it for positive read approval. [Source: `src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor`; `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/LegacyConfigurationDisplaySanitizer.cs`; `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor#ConfigurationSummary`]
- `ITenantsBffComposition` currently exposes connection and authorization-reflection flags only. Extend the server-side composition design or add a narrowly focused collaborator; do not turn the interface into a generic infrastructure abstraction. [Source: `src/Hexalith.Tenants.UI/Services/Gateways/ITenantsBffComposition.cs`; `_bmad-output/planning-artifacts/architecture.md#Core Architectural Decisions`]
- Story 1.8 recorded `CFG-1.6-SAFE-MODEL` because the deny-list can miss unknown secret formats and unsafe keys still enter DOM/accessibility state. Implementing the positive model resolves that prerequisite only; it does not itself approve clipboard support. [Source: `_bmad-output/implementation-artifacts/deferred-work.md`; `_bmad-output/implementation-artifacts/story-1-8-support-safe-identifier-copy-and-read-experience-evidence-2026-07-21.md`]

### Architecture And Scope Guardrails

- Keep the application as the existing .NET 10 Blazor `InteractiveServer` BFF composed through FrontComposer. Browser components call injected server-side contracts only. No browser backend/token access, copied DTO, new endpoint, direct state-store access, or generic EventStore query route is allowed. [Source: `_bmad-output/planning-artifacts/architecture.md#Core Architectural Decisions`; `_bmad-output/implementation-artifacts/epic-1-context.md#Technical Decisions`]
- Story 1.10 owns direct Tenants read transport, separate host references, and authoritative freshness provenance. Story 1.6 consumes the current detail-read seam and keeps unknown/degraded states honest; it must not synthesize freshness from HTTP success, ETag alone, `ServedAt`, request time, SignalR, or unrelated projections. [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.10: Direct Tenants Reads and Authoritative Freshness`; `_bmad-output/planning-artifacts/architecture.md#Known Divergence Register`]
- Preserve Epic 3 behavior rather than rebuilding it. Set/remove flows were added after the historical 1.6 implementation and currently live inside the read component; move their composition, replace raw projection evidence with server-computed proof, and keep their command states, previews, focus behavior, locks, confirmation, and recovery intact. [Source: `_bmad-output/planning-artifacts/epics.md#Story 3.5: Set Namespaced Configuration with Complete Preview`; `#Story 3.6: Remove Configuration Key with Complete Preview`; `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/`]
- Support safety is absolute: do not expose bearer tokens, decoded JWTs, raw command/event payloads, `NarrativePayload`, EventStore metadata, cursors, ETags, internal correlations, stack traces, policy contents, hidden counts, or PII through UI, DOM, accessibility, clipboard, logs, telemetry, exceptions, or test snapshots. [Source: `_bmad-output/planning-artifacts/epics.md#Additional Requirements`; `_bmad-output/project-context.md#Critical Don't-Miss Rules`]

### UX, Accessibility, And Localization

- Use FrontComposer and Fluent UI Blazor V5 first. The current detail page already uses `FcAggregateDetailPage` and a multi-expand `FluentAccordion`; retain that contract and keep primary read content expanded/default-visible. The official Fluent component documentation confirms that `ExpandMode` supports single or multi expansion and defaults to Multi; set it explicitly where the project convention requires deterministic source evidence. [Source: `_bmad-output/planning-artifacts/epics.md#UX Design Requirements`; https://fluentui-blazor.azurewebsites.net/Accordion]
- Preserve the calm, compact operational visual posture, inherited Fluent tokens/shapes, 4/8/12/16/24/32 spacing rhythm, stable footprints, and full-width/horizontal availability where literals need it. Do not redefine the theme or hard-code semantic colors. [Source: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md#Layout & Spacing`; `#Shapes`; `#Do's and Don'ts`]
- Mobile 320–767 is read-only; tablet 768–1023 stacks while retaining horizontal availability; desktop 1024+ is the dense workstation; 1440+ adds space without changing semantics. Use logical start/end properties and remain RTL-ready without claiming RTL verification. [Source: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Responsive & Platform`]
- Meet WCAG 2.1 AA and only claim 2.2 AA where verified against the pinned stack. Meaning uses semantic role, icon, and visible localized text rather than color. Keep dedicated live-region intent, absolute culture-aware timestamps when timestamps exist, keyboard/focus order, forced-colors meaning, and reduced-motion independence. Read-unavailable is polite; degraded/failure/unable-to-verify/destructive-block is assertive. [Source: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Accessibility Floor`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/review-accessibility.md`]
- Tenants owns domain `.resx` copy and FrontComposer owns shell chrome. Use dotted PascalCase resource concepts, whole strings, named placeholders, and exact EN/FR key parity. Stored configuration literals remain literal; culture-aware formatting applies to surrounding UI copy/metadata, not to transforming keys or values. [Source: `_bmad-output/planning-artifacts/epics.md#Additional Requirements`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#17. Assumptions Index`]
- NFR10 requires accessibility, localization, responsive, documentation/reference, and focused test evidence before completion. Story 1.0 evidence does not waive story-specific keyboard, focus, live-region, forced-colors, narrow-width, and screen-reader checks. [Source: `_bmad-output/planning-artifacts/epics.md#NonFunctional Requirements`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#FrontComposer Readiness & Fallbacks`]

### Technology And Testing Baseline

- Use SDK `10.0.302`, target `net10.0`, nullable/implicit usings, `TreatWarningsAsErrors=true`, and central package management. Do not add package versions to project files or upgrade dependencies in this story. [Source: `global.json`; `Directory.Build.props`; `Directory.Packages.props`]
- The current centralized Fluent UI Blazor pin is `5.0.0-rc.4-26180.1`, not the rc.3 value in the historical June story. Verify APIs against the local pin and current official component documentation. [Source: `references/Hexalith.Builds/Props/Directory.Packages.props`; `_bmad-output/planning-artifacts/architecture.md#Technical Context`]
- Tests use xUnit v3, Shouldly, NSubstitute, and bUnit. Use plural `{Class}Tests.cs`, Shouldly assertions, one behavior per test, deterministic substitutes, and real policy-boundary values designed to prove ordinal behavior. Do not weaken conformance guards or use raw `Assert.*`. [Source: `_bmad-output/project-context.md#Testing Rules`; `tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj`]
- Required clean-checkout and focused verification should include, at minimum:
  - `dotnet restore Hexalith.Tenants.slnx`
  - `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore -warnaserror -m:1 -nr:false`
  - `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.Services.Gateways.TenantConfigurationReadPolicyTests`
  - `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.Services.Gateways.TenantQueryGatewayTests`
  - `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.Components.TenantDetailSurfaceTests`
  - `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.Components.SetTenantConfigurationFlowTests`
  - `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.Components.RemoveTenantConfigurationFlowTests`
  - `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.DomainUiFluentConformanceTests`
  - `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none`
  - `dotnet build Hexalith.Tenants.slnx -c Release --no-restore -warnaserror -m:1 -nr:false`
- If `dotnet test` still hits the repository's documented .NET 10 Microsoft.Testing.Platform/VSTest target issue, use the exact xUnit v3 in-process executable fallback above and record both the failed command and fallback result. Run the full UI suite and solution Release build after focused tests because this corrective refactor crosses the shared detail gateway and preserves two command flows. [Source: `_bmad-output/implementation-artifacts/1-5-user-membership-lookup.md#Debug Log References`; `tests/test-summary.md`]

### Project Structure Notes

- Primary source ownership remains `src/Hexalith.Tenants.UI/`: server policy/composition in `Services/Gateways/`, safe state in `State/TenantDetail/`, read UI in `Components/Tenants/TenantConfigurationView.razor`, management UI in `Components/Tenants/Configuration/`, orchestration in `Components/Pages/TenantDetailPage.razor`, and domain copy in `Resources/`.
- Implement one idempotent Tenants-owned configuration-policy registration extension, then call it from both the standalone host (`Program.cs`) and embeddable module (`Extensions/TenantsUiServiceCollectionExtensions.cs`) so tests and composing hosts cannot diverge.
- Keep tests in `tests/Hexalith.Tenants.UI.Tests/`, extending existing gateway, detail-surface, command-flow, composition, resource-parity, and Fluent-conformance coverage. Add a focused policy/composer test file rather than burying all security-boundary cases in bUnit tests.
- Update `src/Hexalith.Tenants.UI/appsettings.json` only to declare the valid-empty `Tenants:ConfigurationReadPolicy` schema. Do not add real grants/safe keys to repository defaults or environment-specific files.
- Do not modify `src/Hexalith.Tenants/Queries/Handlers/GetTenantQueryHandler.cs`, public query contracts, `references/` submodules, solution files, Dockerfiles, package pins, shared test harnesses, or backend endpoints for this story.
- Preserve the old `1-6-read-only-tenant-configuration-view.md`, the resolved spec, and historical evidence. The canonical delivery artifact for this corrective work is this file.

### References

- Story and AC source: `_bmad-output/planning-artifacts/epics.md#Story 1.6: Read-Only Tenant Configuration`
- Epic context: `_bmad-output/implementation-artifacts/epic-1-context.md`
- Resolved implementation kernel: `_bmad-output/implementation-artifacts/spec-1-6-read-only-tenant-configuration.md`
- Historical story: `_bmad-output/implementation-artifacts/1-6-read-only-tenant-configuration-view.md`
- Previous story intelligence: `_bmad-output/implementation-artifacts/1-5-user-membership-lookup.md`
- PRD and reconciliation: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md`; `addendum.md`; `reconcile-operations-shell.md`; `reconcile-a11y-l10n.md`; `reconcile-responsive-visual.md`; `reconcile-truth-state.md`
- Architecture: `_bmad-output/planning-artifacts/architecture.md`
- UX: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md`; `EXPERIENCE.md`; `review-accessibility.md`
- Current BFF/read state: `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`; `ITenantsBffComposition.cs`; `TenantsBffComposition.cs`; `TenantsGlobalAdministratorClaims.cs`; `src/Hexalith.Tenants.UI/State/TenantDetail/TenantDetailSnapshot.cs`
- Current UI and commands: `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`; `src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor`; `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/`
- Current tests/evidence: `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs`; `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs`; `tests/Hexalith.Tenants.UI.Tests/Components/SetTenantConfigurationFlowTests.cs`; `RemoveTenantConfigurationFlowTests.cs`; `tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs`; `tests/test-summary.md`
- Official ASP.NET Core options guidance: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-10.0
- Official Fluent UI Blazor accordion documentation: https://fluentui-blazor.azurewebsites.net/Accordion
- Project rules: `AGENTS.md`; `_bmad-output/project-context.md`; `references/Hexalith.AI.Tools/hexalith-llm-instructions.md`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Baseline recorded at `b73093bd10608afe4e6036439a48a08924d0358b`; repository guidance, resolved Story 1.6 specs, sprint state, architecture/UX context, and existing candidate implementation were inspected before modification.
- Red/green trust-boundary checks: principal resolver tests first failed on the missing `IUserContextAccessor` corroboration, then passed after the SSR/circuit identity boundary was corrected.

**Correction (2026-07-27 review).** The originally recorded evidence used aggregate `dotnet test` runs and a build without `-warnaserror`/`-nr:false`, so it did not match the commands the story and kernel prescribe, and no focused per-class result was recorded. It also credited a `Sample 39/39` project that does not exist under `tests/`. Re-run with the prescribed commands after the review patches:

- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore -warnaserror -m:1 -nr:false` — 0 warnings, 0 errors.
- `… -class Hexalith.Tenants.UI.Tests.Services.Configuration.TenantConfigurationReadPolicyTests` — 36/36 (was 20; +16 boundary, fail-closed and cross-tenant cases).
- `… -class Hexalith.Tenants.UI.Tests.Services.Gateways.TenantsBffCompositionTests` — 9/9 (new file; the submit-time re-authorization seam had no test).
- `… -class Hexalith.Tenants.UI.Tests.Services.Gateways.TenantQueryGatewayTests` — 284/284.
- `… -class Hexalith.Tenants.UI.Tests.Components.TenantDetailSurfaceTests` — 61/61.
- `… -class Hexalith.Tenants.UI.Tests.Components.SetTenantConfigurationFlowTests` — 28/28.
- `… -class Hexalith.Tenants.UI.Tests.Components.RemoveTenantConfigurationFlowTests` — 12/12.
- `… -class Hexalith.Tenants.UI.Tests.DomainUiFluentConformanceTests` — 51/51.
- `… -class Hexalith.Tenants.UI.Tests.TenantConfigurationEndToEndTests` — 1/1.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` — 1312/1312.
- `dotnet build Hexalith.Tenants.slnx -c Release --no-restore -warnaserror -m:1 -nr:false` — 0 warnings, 0 errors.
- Other Release tiers, run from the built xUnit v3 executables: Contracts 116/116, Client 50/50, Testing 181/181, Server 738/738. `Hexalith.Tenants.IntegrationTests` (Tier 3, non-blocking) was not run in this pass.

**2026-07-27 closure pass** (final open review item + story-file integrity). The `NU1102` restore blocker recorded earlier in the day is gone — `references/Hexalith.Builds@1b1c0b0` now pins `HexalithEventStoreVersion=3.83.0`, so `dotnet restore Hexalith.Tenants.slnx` succeeds with no version override.

- Decision-3 evidence was gathered against the pinned .NET 10 configuration stack in an isolated probe before any source change, because the review's "not implementable" verdict rested on an untested assumption about blast radius. Observed: `"DisplaySafe": []` → `Value == ""`/0 children; `"DisplaySafe": ""` → `Value == ""`/0 children; emptied env override → `Value == ""`/0 children (indistinguishable, review correct); JSON `["a","b"]` + emptied env override → binds to **2** items (override cannot clear a declared list); `…__DisplaySafe__0=` → binds to `["", "b"]` → already rejected by `TryValidate`.
- Red/green for the 3 new tests was established by mutation rather than by writing them against absent behaviour, since they characterise existing guards. Mutation 1 (`HasScalarCollection` → `child.Value is not null`, i.e. the review's reverted fix) failed 5 tests including 2 of the 3 new ones, independently reproducing why that fix was rejected. Mutation 2 (drop `IsNullOrWhiteSpace(safeKey)` from `TryValidate`) failed exactly the third. Provider restored byte-identical after each.
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore -warnaserror -m:1 -nr:false` — 0 warnings, 0 errors.
- `… -class …Services.Configuration.TenantConfigurationReadPolicyTests` — 39/39 (was 36; +3 closure tests).
- `… -class …Services.Gateways.TenantsBffCompositionTests` — 9/9.
- `… -class …Services.Gateways.TenantQueryGatewayTests` — 284/284.
- `… -class …Components.TenantDetailSurfaceTests` — 61/61.
- `… -class …Components.SetTenantConfigurationFlowTests` — 28/28.
- `… -class …Components.RemoveTenantConfigurationFlowTests` — 12/12.
- `… -class …DomainUiFluentConformanceTests` — 51/51.
- `… -class …TenantConfigurationEndToEndTests` — 1/1.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` — 1315/1315.
- `dotnet build Hexalith.Tenants.slnx -c Release --no-restore -warnaserror -m:1 -nr:false` — 0 warnings, 0 errors.
- Other Release tiers: Contracts 116/116, Client 50/50, Testing 181/181, Server 738/738. `Hexalith.Tenants.IntegrationTests` (Tier 3, non-blocking) was again not run in this pass.

**2026-07-28 trust-boundary re-review patch pass.** The story-owned portion of the narrowed review chunk closed all
11 accepted patches. Principal evidence now rejects normalized subjects and ambiguous tenant scopes; cache reads and
reload invalidation share one lock; projection-proof policy failures fail closed; diagnostics use source-generated,
support-safe category events; and the accepted DI, wildcard, retained-state, reload, failure, and cancellation behavior is
pinned by regression tests.

- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --no-restore -p:UseHexalithProjectReferences=false` — 1325/1325.
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true -warnaserror -m:1 -nr:false` — 0 warnings, 0 errors.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` — 1325/1325.
- `dotnet build Hexalith.Tenants.slnx -c Release -p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true -warnaserror -m:1 -nr:false` — 0 warnings, 0 errors.
- `git diff --check` — passed.
- An initial `--no-restore` Release attempt was invalidated by generated assets left in source-reference mode; running the
  pinned package-mode restore and build as one graph resolved the environmental mismatch without dependency changes.

### Implementation Plan

- Harden the policy/principal boundary and sanitize all tenant configuration before snapshot construction.
- Split the inspection-only read landmark from safe-context command management and move projection comparison behind proof-only gateway contracts.
- Preserve Epic 3 lifecycle behavior while adding exact-full-key set semantics and submit-time reauthorization.
- Complete localized truth/recovery states, end-to-end absence proof, repository-wide verification, and Story 1.8 deferred-evidence reconciliation.

### Completion Notes List

- Resolves one authenticated SSR/circuit identity and derives subject, system scope, and administrator evidence from it; malformed, cross-identity, or indeterminate evidence fails closed. Policy registration is idempotent and runtime policy failure is not startup-fatal. **Correction (2026-07-27 review):** the `IUserContextAccessor` comparison was described as independent corroboration of the SSR/circuit identity. It is not — the accessor is configured to read the same `sub` claim from the same principal the resolver already selected, so the check detects claim-type misconfiguration, not identity divergence. The single-identity guarantee comes from the `authenticated.Length != 1` and cross-identity claim checks, which are real.
- Raw configuration remains transient in the server gateway. Ready, stale, degraded, unconditional `304`, wrong-tenant, and failed-refresh paths construct sanitized detail plus immutable safe read/management state; hidden keys, values, counts, and exception details do not reach Razor state.
- Rebuilt `TenantConfigurationView` as a strict `tenants-config-read-*` inspection landmark using only positive safe rows, distinct truth/empty/filter states, literal accessible values, multi-expand Fluent grouping, and responsive/forced-colors-safe overflow without mutation, copy, or reveal controls.
- Added a sibling management landmark. Set accepts the exact literal full key, remove targets only current safe rows, both reauthorize immediately before dispatch, and projection callbacks/snapshots carry proof status instead of raw dictionaries while retaining preview, locking, focus, audit, and recovery behavior.
- Added complete EN/FR configuration/read/management copy and exact resource parity. The deployment-owned positive `DisplaySafe` decision is resolved; `CFG-1.6-SAFE-MODEL` is closed. Configuration clipboard activation/certification remains intentionally out of scope and absent.
- **2026-07-27, final open item.** The empty-scalar `DisplaySafe` shape is a known, accepted limitation, not an oversight: `[]`, `""` and an emptied environment override are indistinguishable at the configuration layer, and failing closed on that state takes the shipped valid-empty default dark. It is safe to accept because the failure direction is one-way — an empty declaration withholds approval and can never grant it — and because the environment cannot produce it: an emptied override leaves a declared list intact, and an emptied element is rejected outright. All three properties are pinned by `An_emptied_environment_override_cannot_clear_a_declared_display_safe_list`, `An_emptied_display_safe_element_is_rejected_rather_than_silently_dropped`, and `An_empty_display_safe_scalar_approves_nothing_rather_than_widening_approval`, each verified by mutation.
- **2026-07-27, story-file integrity.** Commit `ec7ec8c` had stripped the `- [ ]`/`- [x]` markers from all 34 Tasks/Subtasks lines rather than checking them, leaving the story with no completion record. The original structure was restored from `91d5980` (task text verified byte-identical) and marked complete.
- **2026-07-28, trust-boundary re-review.** Applied all 11 accepted patches from the narrowed trust-boundary chunk and
  added 10 net-new test cases. The full UI count moved 1315 → 1325. Story status remains `review` because the agreed
  chunking leaves the UI composition/accessibility and broader test/evidence review groups for follow-up.
- No public query contract, endpoint, dependency, or package changed; Story 1.10 transport/provenance work remains separate. **Correction (2026-07-27 review):** the `references/Hexalith.EventStore` gitlink *was* changed by `ec7ec8c` (`c6b72ca` → `440ff4c`), which this sentence originally denied. See the resolved decisions below.

### File List

**Correction (2026-07-27 review).** The original list named 26 paths because `baseline_commit` pointed at `b73093b`, an in-story implementation commit, so it was computed from a mid-story baseline and omitted 23 of the 49 delivered paths — including every file the kernel's Execution bullets name. Regenerated from the true story range `2f190a1..ec7ec8c`:

- `_bmad-output/implementation-artifacts/1-6-read-only-tenant-configuration.md`
- `_bmad-output/implementation-artifacts/spec-1-6-read-only-tenant-configuration-2.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/story-1-8-support-safe-identifier-copy-and-read-experience-evidence-2026-07-21.md`
- `references/Hexalith.EventStore`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/RemoveTenantConfigurationFlow.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/SetTenantConfigurationFlow.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/TenantConfigurationManagement.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/TenantConfigurationManagement.razor.css`
- `src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor.css`
- `src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs`
- `src/Hexalith.Tenants.UI/Program.cs`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`
- `src/Hexalith.Tenants.UI/Services/Configuration/ITenantConfigurationPrincipalResolver.cs`
- `src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationComposition.cs`
- `src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationPrefixGrantOptions.cs`
- `src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationPrincipalEvidence.cs`
- `src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationPrincipalEvidenceState.cs`
- `src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationPrincipalResolver.cs`
- `src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationReadPolicyOptions.cs`
- `src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationReadPolicyProvider.cs`
- `src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationReadPolicyResolution.cs`
- `src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationSafeComposer.cs`
- `src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationServiceCollectionExtensions.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantQueryGateway.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantsBffComposition.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs`
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs`
- `src/Hexalith.Tenants.UI/State/TenantDetail/TenantConfigurationManagementContext.cs`
- `src/Hexalith.Tenants.UI/State/TenantDetail/TenantConfigurationProjectionProof.cs`
- `src/Hexalith.Tenants.UI/State/TenantDetail/TenantConfigurationProjectionProofKind.cs`
- `src/Hexalith.Tenants.UI/State/TenantDetail/TenantConfigurationSafeModel.cs`
- `src/Hexalith.Tenants.UI/State/TenantDetail/TenantConfigurationSafeRow.cs`
- `src/Hexalith.Tenants.UI/State/TenantDetail/TenantDetailSnapshot.cs`
- `src/Hexalith.Tenants.UI/appsettings.json`
- `tests/Hexalith.Tenants.UI.Tests/Components/RemoveTenantConfigurationFlowTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/SetTenantConfigurationFlowTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Configuration/TenantConfigurationReadPolicyTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/State/TenantRemoveConfigurationCommandSnapshotTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/State/TenantSetConfigurationCommandSnapshotTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/TenantConfigurationEndToEndTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs`
- `tests/test-summary.md`

Added by the 2026-07-27 review pass:

- `src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationPolicyFailure.cs` (new)
- `src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationValidatedPolicy.cs` (new)
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantsBffCompositionTests.cs` (new)
- `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/LegacyConfigurationDisplaySanitizer.cs` (deleted — unreferenced)

Touched by the 2026-07-27 closure pass (all already listed above; no new paths):

- `src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationReadPolicyProvider.cs` (comment only — records Decision 3; no behaviour change)
- `tests/Hexalith.Tenants.UI.Tests/Services/Configuration/TenantConfigurationReadPolicyTests.cs` (+3 mutation-verified tests)
- `_bmad-output/implementation-artifacts/1-6-read-only-tenant-configuration.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `tests/test-summary.md`

Touched by the 2026-07-28 trust-boundary re-review patch pass (all source/test paths already listed above):

- `_bmad-output/implementation-artifacts/1-6-read-only-tenant-configuration.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationPrincipalResolver.cs`
- `src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationReadPolicyProvider.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Configuration/TenantConfigurationReadPolicyTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantsBffCompositionTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs`
- `tests/test-summary.md`

### Change Log

- 2026-07-22: Completed the corrective positive-policy safe configuration model, sanitized BFF integration, strict read-only landmark, sibling safe management/proof boundary, localization, accessibility, focused/end-to-end coverage, and repository-wide validation; moved Story 1.6 to review.
- 2026-07-27: Adversarial code review over `2f190a1..ec7ec8c`; 2 decisions resolved, 38 of 39 patches applied, 2 items deferred. UI suite 1281 → 1312. Story held at in-progress for the one open patch.
- 2026-07-27: Closed the last open review patch as Decision 3 (bounded limitation, no spec change) with 3 mutation-verified regression tests; restored the Tasks/Subtasks checkboxes that `ec7ec8c` stripped from this file; re-ran the prescribed verification. UI suite 1312 → 1315. Story 1.6 back to review.
- 2026-07-28: Applied all 11 accepted patches from the narrowed trust-boundary re-review chunk; Release UI and solution builds passed warning-clean and the UI suite passed 1325/1325. Story remains in review for the agreed follow-up chunks.

### Review Findings

Adversarial code review 2026-07-27 over `2f190a1..ec7ec8c` (49 files, +3800/-967). Four independent layers: blind-hunter, edge-case-hunter, verification-gap, acceptance-auditor. Every finding below was re-verified against the real source at `HEAD` (`0ded4a1`) before being recorded; findings already resolved by later commits were dismissed rather than listed.

#### Decisions resolved (2026-07-27, by story owner)

- **RESOLVED → option A.** Global-administrator claim present but `eventstore:tenant=system` absent resolved to Indeterminate, stripping the user's *explicit* `(tenantId, sub, prefix)` grants as well as the wildcard. An admin role scoped to a non-system tenant is a well-formed claim that simply does not meet the bar — that is proven-non-administrator, not ambiguous evidence. Decision: fall back to `NonAdministrator` so explicit grants are honoured. Recorded as a patch below.
- **RESOLVED → option B.** The forbidden `references/Hexalith.EventStore` bump (`c6b72ca` → `440ff4c`) is absorbed into main — nine later commits touched the gitlink and the story SHA is an ancestor of the current pointer `c8c7003`, so no revert is possible. Decision: correct the false Completion Notes claim *and* log a sprint action item, because Story 1.4's commit `41e047e` exhibited the identical defect (bundled submodule bumps while claiming untouched). Second occurrence makes it a pattern worth guarding, not a slip.
- **RESOLVED → close as a bounded limitation (2026-07-27, story owner).** The empty-scalar `DisplaySafe` item below
  (the one patch left open) is closed without a spec change. Re-probed against the pinned .NET 10 configuration stack
  before deciding: `"DisplaySafe": []`, `"DisplaySafe": ""` and an emptied `Tenants__ConfigurationReadPolicy__DisplaySafe`
  override all present as `Value == ""` with zero element children, so the review's "indistinguishable" claim is correct
  and no code-only fix exists. Two further facts, absent from the review record, bound the residual: an emptied
  environment override does **not** shorten an already-declared list (`["a","b"]` still binds to two entries, because the
  declaring provider's element children win), and the one override shape that does reach the bound list
  (`…__DisplaySafe__0=`) arrives as a blank element that semantic validation already rejects. The residual is therefore a
  hand-authored JSON scalar typo whose only effect is zero approved keys — it can withhold approval, never grant it.
  Rejected alternative: a required declared cardinality (`DisplaySafeCount`) would catch "intended 2, got 0", but since
  the environment cannot produce that state the redundant counts buy an operator sync footgun for near-zero benefit.
  Pinned by three mutation-verified tests instead.

#### Patches

Security and correctness:

- [x] [Review][Patch] (from Decision 1) Admin claim without `eventstore:tenant=system` must resolve to `NonAdministrator`, not `Indeterminate`, so the user's explicit `(tenantId, sub, prefix)` grants are still evaluated; add a policy test pinning both the no-wildcard and grants-honoured halves. [`src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationPrincipalResolver.cs:87`]
- [x] [Review][Patch] Projection-proof gateway applies no configuration policy — gates only on a non-empty `UserId`, tenant-id match and Current freshness, then probes the raw dictionary, making it an existence/value oracle for keys outside the caller's grants. Two layers converged on this independently. Still byte-identical at HEAD. [`src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs:1413-1450`]
- [x] [Review][Patch] Submit-time re-authorization throws instead of failing closed — the provider dereferences `_snapshot.Detail ?? throw`, and both flows await it *outside* their `try` blocks, so a background refresh that nulls Detail turns the TOCTOU guard into a circuit teardown. [`src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:220,287`; `SetTenantConfigurationFlow.razor:518-520` (try opens at 534); `RemoveTenantConfigurationFlow.razor:461-463`]
- [x] [Review][Patch] A safe row whose key contains no `.` renders a Remove button that can never complete — `NamespacePrefix` returns empty unless a dot exists at index > 0, so the flow opens straight into Unavailable.Scope. The Set flow handles the identical key correctly; the change's own fixtures seed such a key. [`RemoveTenantConfigurationFlow.razor:594-598` vs `SetTenantConfigurationFlow.razor:694-707`]
- [x] [Review][Patch] `AlreadyApplied` short-circuits *before* `ReauthorizeProvider()`, so a terminal already-applied state can be derived entirely from pre-revocation rows. [`SetTenantConfigurationFlow.razor:509-516`]
- [x] [Review][Patch] `_removeLaunchElements` is never pruned — a successful remove leaves a detached `ElementReference` that `OnAfterRenderAsync` then focuses, throwing `JSException`; the dictionary also retains configuration key names from previously visited tenants for the life of the circuit. [`TenantConfigurationManagement.razor:102,158-162`]
- [x] [Review][Patch] No `JSDisconnectedException` guard around `FocusAsync` in `OnAfterRenderAsync`; six sibling components in this repo carry the guard and this one is the outlier. [`TenantConfigurationManagement.razor:156-164`]
- [x] [Review][Patch] One null configuration value discards the whole tenant detail page — `ThrowIfNull` inside `Compose` is swallowed by the gateway's blanket catch, taking members, metadata, lifecycle and audit down with it. Filter nulls per row instead. [`TenantConfigurationSafeComposer.cs:111-117`]
- [x] [Review][Patch] Row filter uses `CurrentCultureIgnoreCase` in a feature that is ordinal everywhere else — NFD input matches an NFC key, U+200B matches every row, and EN/FR give different results over identical literals. [`TenantConfigurationView.razor:165-166`]
- [x] [Review][Patch] Null payload now maps to Degraded instead of the deleted Unknown state, so the UI claims retained evidence where there is none — AC5 requires these to stay distinct. [`TenantQueryGateway.cs` null-payload path]
- [x] [Review][Patch → CLOSED AS BOUNDED LIMITATION, see Decision 3] `"DisplaySafe": ""` binding to an empty allow-list instead of failing closed. The review's blocking analysis was reproduced and confirmed: `"DisplaySafe": []`, `"DisplaySafe": ""` and an emptied `Tenants__ConfigurationReadPolicy__DisplaySafe` override are one observable state (`Value == ""`, no element children), and re-applying the "treat empty value as scalar" fix still fails `Global_administrator_receives_only_the_namespace_wildcard…`, `Valid_empty_policy_is_safe_empty…` and `Display_approval_is_exact_and_ordinal…` — the shipped valid-empty default goes dark, which the kernel forbids. What the review did not establish is the *blast radius*: the environment cannot reach this state at all. Closed by accepting the residual (a hand-authored scalar typo that withholds approval and can never grant it) and pinning all three facts with mutation-verified tests, rather than by a spec change. [`TenantConfigurationReadPolicyProvider.cs` `HasScalarCollection`; `TenantConfigurationReadPolicyTests.cs`]
- [x] [Review][Patch] Subject equality compares the raw `sub` claim against `IUserContextAccessor.UserId`, which FrontComposer already trimmed — a `sub` with surrounding whitespace makes configuration silently unavailable while every other surface works. [`TenantConfigurationPrincipalResolver.cs:67-72`]
- [x] [Review][Patch] No diagnostic on any Unavailable path — missing section, duplicate grant, trailing-dot prefix, unbindable section and indeterminate principal are indistinguishable at runtime, so a one-character deployment typo takes the surface dark with zero operator signal. Log the failure *category* only, never policy contents. [`TenantConfigurationReadPolicyProvider.cs`]
- [x] [Review][Patch] Provider captures the `IConfiguration` passed to `AddTenantConfigurationReadPolicy` rather than resolving it from DI, so an embedding host that passes a sub-section gets a permanently unavailable policy. [`TenantConfigurationServiceCollectionExtensions.cs:30`]
- [x] [Review][Patch] Singleton provider caches nothing — a full reflection bind plus two HashSet rebuilds run on every detail read, degraded reauthorization and command reauthorization. [`TenantConfigurationReadPolicyProvider.cs:31-65`]
- [x] [Review][Patch] Detail reads still send the conditional ETag although the 304 path always re-reads unconditionally, doubling backend queries in the common case. [`TenantQueryGateway.cs` detail read + 304 retry]

Copy, layout and accessibility:

- [x] [Review][Patch] Page-level summary still says "No configuration keys are available in this detail projection" while now computed from the *safe* model — an absolute claim about the projection derived from the approved subset, against AC2/AC4. The region-level string was correctly reworded to "No **visible** configuration"; this one was not (EN+FR). [`TenantDetailPage.razor:303-311`; `TenantsResources.resx:591`]
- [x] [Review][Patch] A valid-but-empty policy renders "authorization policy cannot be verified" to a proven non-administrator — false, and it contradicts the sibling read landmark on the shipped `appsettings.json` default. [`TenantConfigurationManagement.razor:14,133-135`]
- [x] [Review][Patch] `display: none` removes the entire removable-target region below 768px with no localized substitute and no `fc-css-exception` marker, while the Set flow keeps an explicit narrow message — AC8 forbids layout that makes safety-critical state disappear. [`TenantConfigurationManagement.razor.css:38-46`]
- [x] [Review][Patch] The read landmark can never render its Loading state (every Loading snapshot carries an unavailable model), leaving `State.Loading` resources dead. User-visible behaviour is mitigated by page-level `LoadingContent`. [`TenantConfigurationView.razor:189-200`]
- [x] [Review][Patch] Unmarked layout/typography rules lacking the required `/* fc-css-exception: … */` marker; they pass CI only because the ratcheted guard tracks a narrower property set. [`TenantConfigurationView.razor.css:27-31,76-79,97-106,129-141`; `TenantConfigurationManagement.razor.css:23-31,44-46`]
- [x] [Review][Patch] 14 orphaned EN/FR resource pairs left unreachable by the read/set rewrite (`Set.Namespace.*`, `Value.Safe`, `Value.Sensitive`, `Header.Safety`, `State.Unauthorized*`, …). Parity is preserved; the keys are simply dead.
- [x] [Review][Patch] `LegacyConfigurationDisplaySanitizer` is now wholly unreferenced dead code (4 call sites → 0). Delete it. [`Components/Tenants/Configuration/LegacyConfigurationDisplaySanitizer.cs`]

Verification that does not verify:

- [x] [Review][Patch] The configuration support-safety redaction test was deleted and never replaced — 10 assertions on `correlation-123`, JWT-shaped strings, `InvalidOperationException`, stack-trace text and PII at `2f190a1`, 0 at `ec7ec8c`, still 0 at HEAD — in the same change that added new exception paths through the gateway and composer. [`tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs`]
- [x] [Review][Patch] The composer's fail-closed branch is never executed by any test; `Unavailable(...)` → `Available(tenantId, [])` survives the whole suite, meaning broken deployment policy would render authorization-safe empty instead of unavailable. [`TenantConfigurationSafeComposer.cs:28-34,65-70`]
- [x] [Review][Patch] No test feeds `PrincipalEvidence.Indeterminate()` to `Resolve` — deleting the fail-closed guard yields `IsAvailable == true` with zero prefixes and nothing fails. [`TenantConfigurationReadPolicyProvider.cs:25-29`]
- [x] [Review][Patch] `TenantsBffComposition.ReauthorizeConfigurationManagementAsync` — the production submit-time re-authorization seam — has zero test references; both revocation tests assert against a hand-written lambda's return value. [`TenantsBffComposition.cs:59-73`]
- [x] [Review][Patch] Cross-tenant grant isolation is untested — every fixture grants and queries `tenant.alpha`, so deleting the `grant.TenantId` conjunct (a cross-tenant policy leak) survives. [`TenantConfigurationReadPolicyProvider.cs:60-61`]
- [x] [Review][Patch] Four Epic 3 "redaction" tests became tautologies — their fixtures are pre-filtered through `IsSafeForTestContext`, a verbatim test-local copy of the deny-list, so the sensitive value never reaches the component under test. [`SetTenantConfigurationFlowTests.cs:635-637`; `RemoveTenantConfigurationFlowTests.cs:370-372`]
- [x] [Review][Patch] `Valid_empty_policy_is_safe_empty_and_defensively_copies_caller_owned_data` cannot fail — with an empty `DisplaySafe` the rows are empty with or without a defensive copy. [`TenantConfigurationReadPolicyTests.cs:209-235`]
- [x] [Review][Patch] `DisplaySafeKeys` ordinal case-sensitivity is unproven; the one case-confusable fixture key is itself in `DisplaySafe`, so it exercises the prefix gate, not the display gate. `OrdinalIgnoreCase` would leak case-variant keys to global administrators. [`TenantConfigurationReadPolicyResolution.cs:19`]
- [x] [Review][Patch] Boundary cases the story explicitly required are absent — leading/consecutive empty segments (`.a`, `a..b`) and visually confusable prefixes (Cyrillic/Greek) have no policy-level test. [`TenantConfigurationReadPolicyTests.cs`]
- [x] [Review][Patch] The focus-return regression test was deleted while `test-summary.md` still claims focus return is preserved; the replacement asserts only that the flow disappears. [`TenantDetailSurfaceTests.cs:363-390`]

Record accuracy:

- [x] [Review][Patch] Completion Notes state "No public query contract, endpoint, dependency, package, or `references/` content changed" — the `references/` clause is false. [story line 185]
- [x] [Review][Patch] (from Decision 2) Register a sprint action item for undeclared submodule-pointer bumps riding along in story commits — second occurrence after Story 1.4's `41e047e`; the guard belongs in the story-completion checklist, not in a per-story fix. [`_bmad-output/implementation-artifacts/sprint-status.yaml` action_items]
- [x] [Review][Patch] File List omits 23 of the 49 changed paths, including every file the kernel's Execution bullets name. Root cause: `baseline_commit: b73093b` names an in-story implementation commit, so the list was computed from a mid-story baseline. [story frontmatter line 2, lines 189-214]
- [x] [Review][Patch] Recorded verification commands do not match the prescribed ones — `-warnaserror` and `-nr:false` are absent and no focused per-class run is recorded with its result. [story lines 163-169]
- [x] [Review][Patch] `test-summary.md:270-273` and the Dev Agent Record overclaim coverage for Unicode/case boundaries, defensive copying, hidden-state absence and submission-time reauthorization; narrow the claims once the gaps above are closed.
- [x] [Review][Patch] The "corroborated the SSR/circuit `sub` with server-side `IUserContextAccessor`" claim is circular — the resolver reproduces FrontComposer's own principal precedence and then compares that principal's `sub` against a `UserId` configured to read the same claim from the same principal. It catches claim-type misconfiguration, not identity divergence. Correct the record or make the corroboration independent. [`TenantConfigurationPrincipalResolver.cs:30-41,67-72`]

#### Deferred

- [x] [Review][Defer] A third divergent global-administrator claim parser now coexists with `TenantsGlobalAdministratorClaims`, with four verified behavioural divergences — deferred, consolidation spans lifecycle and global-administrator surfaces beyond Story 1.6. [`TenantConfigurationPrincipalResolver.cs:102-194`]
- [x] [Review][Defer] `LifecycleAuthorizationReflection` and `GlobalAdministratorsAuthorizationReflection` still read `HttpContext.User` with no circuit fallback while the new configuration path has one — deferred, pre-existing and outside this story's file scope.

#### Dismissed (recorded so they are not re-raised)

- Six `ToString()`-based absence assertions were genuinely vacuous at `ec7ec8c` (`TenantDetailSnapshot` went `record` → `class`, losing the generated `ToString`). **Already fixed at HEAD** by `de2ded0`, which added support-safe overrides to both types. Real defect, resolved by a later commit.
- Malformed/unsupported role encodings resolving to Indeterminate, and duplicate policy declarations disabling the section — both are spec-conformant by design. The missing *diagnostics* are retained as a patch above.
- The acceptance-auditor's claim that `ToString()` overrides existed at `ec7ec8c` is factually wrong; it read the working tree, not the reviewed revision.

### Review Findings

#### Trust-boundary re-review (2026-07-28, `b73093b..f279cb1` narrowed chunk)

- [x] [Review][Patch] [HIGH] Reject padded `sub` claims instead of normalizing them into literal policy grants [src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationPrincipalResolver.cs:67]
- [x] [Review][Patch] [HIGH] Reject conflicting or malformed tenant-scope claims before granting the global-administrator wildcard [src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationPrincipalResolver.cs:83]
- [x] [Review][Patch] [HIGH] Synchronize policy-cache reads with reload invalidation so revoked grants cannot remain visible [src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationReadPolicyProvider.cs:143]
- [x] [Review][Patch] [HIGH] Contain configuration-policy authorization failures inside the projection-proof fail-closed boundary [src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs:1440]
- [x] [Review][Patch] Pin the accepted root-configuration precedence when an embedding host passes a subsection [src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationServiceCollectionExtensions.cs:35]
- [x] [Review][Patch] Prove policy-cache reload invalidation with a mutable configuration and grant revocation [src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationReadPolicyProvider.cs:36]
- [x] [Review][Patch] Verify support-safe policy diagnostics, failure categories, levels, and once-per-load emission [src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationReadPolicyProvider.cs:168]
- [x] [Review][Patch] Cover global-administrator wildcard key authorization without explicit prefix grants [src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs:84]
- [x] [Review][Patch] Cover retained-detail reauthorization failure as a support-safe degraded snapshot [src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs:1502]
- [x] [Review][Patch] Convert Story 1.6 policy diagnostics to source-generated `LoggerMessage` methods [src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationReadPolicyProvider.cs:57]
- [x] [Review][Patch] Cover cancellation propagation through initial composition and retained-detail reauthorization [src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs:140]
- [x] [Review][Defer] Partially hidden authoritative-search windows still disclose hidden candidates through surviving-row count plus live paging [src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs:890] — deferred, pre-existing relative to Story 1.6 and already owned by Story 1.9
- [x] [Review][Defer] Treating search-hydration 404 and 403 identically can stop paging before later authorized matches [src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs:1013] — deferred, pre-existing relative to Story 1.6 and owned by Story 1.9

All 11 patch findings in this narrowed chunk were resolved and verified on 2026-07-28. The two defers remain in the
deferred-work ledger under Story 1.9 ownership. Story and sprint status intentionally remain `review`: the agreed
chunking still leaves UI composition/accessibility and broader test/evidence groups for follow-up review.

#### UI composition and accessibility re-review (2026-07-31, `2f190a1..HEAD` narrowed to the UI chunk)

Four independent layers (blind-hunter, edge-case-hunter, verification-gap, acceptance-auditor) over
`git diff 2f190a1 HEAD` restricted to `TenantConfigurationView.razor(.css)`,
`Components/Tenants/Configuration/`, `TenantDetailPage.razor`, and both `.resx` files
(10 files, +1766/-625). Every finding below was re-read against the real working tree at `HEAD` (`625061b`)
before being recorded; candidates that the 2026-07-27 and 2026-07-28 passes already closed were re-verified
as closed and dismissed rather than listed. EN/FR parity was independently re-confirmed by three layers
(1223 keys each, zero one-sided, zero placeholder mismatches).

Gitlink guard: `python3 scripts/validate-story-gitlinks.py` exits 1, but every UNDECLARED pointer it names was
moved by a Story 1.9 / Epic 2 commit after this story's stale baseline. No Story 1.6 commit after `ec7ec8c`
moves a gitlink, and `ec7ec8c`'s EventStore bump is declared. The FAIL is a stale-baseline artifact.

Decisions resolved (2026-07-31, by story owner):

- **RESOLVED → reject at validation.** Configuration keys gain a visible-distinctness rule enforced by
  *rejection*, not normalization: `TryBuildRequest` rejects keys containing Unicode `Cf`/format characters or
  leading/trailing whitespace, with a localized validation message. No submitted key is ever silently
  rewritten, so the kernel's "ordinal, no trimming or normalization" comparison rule is untouched — that rule
  governs how keys are compared, not which keys the UI is willing to accept. This closes both halves of the
  finding: the trailing-space twin and the permanently unremovable zero-width key. `Trim()` is deliberately
  **not** restored, because trimming would silently submit a key different from the one typed.
- **RESOLVED → move Cancel outside the form.** The narrow-layout collision is resolved without reverting
  either prior patch: `Cancel` moves outside `.tenants-config-remove__form` so it survives the ≤767px
  `display: none` rule, leaving the dialog dismissable by pointer. The removable-target grid stays rendered at
  narrow widths (preserving the 2026-07-27 patch that kept a safety-critical surface in the accessibility
  tree), and the launcher stays available.

Decision detail retained for the record:

- [x] [Review][Decision → PATCH, reject at validation] Configuration keys have no visible-distinctness rule, so a key can be created that cannot be typed back to remove it — `Trim()` was dropped from key capture (`SetTenantConfigurationFlow.razor:637`, was `_key?.Trim()`), `TryBuildRequest` checks length only, and `IsKeyAuthorized` uses `IsNullOrWhiteSpace`, for which `​` is `false`. `RemoveTenantConfigurationFlow.razor:253` gates removal on ordinal equality with text the operator types, and neither flow offers a copy affordance. A key such as `app.​flag` or `billing.tier ` is therefore writable, renders indistinguishably from its clean twin, and is permanently unremovable through the UI. The kernel's "ordinal, no trimming or normalization" rule governs *comparison*; whether it also forbids *rejecting* such input at capture is the open question. Options: (a) reject keys containing Unicode `Cf` characters or leading/trailing whitespace at input validation — rejection, not normalization; (b) restore `Trim()` on capture only; (c) accept and add a support-safe copy affordance to the remove confirmation; (d) accept as a documented bounded limitation.
- [x] [Review][Decision → PATCH, move Cancel outside the form] At ≤767px the remove dialog can be opened but has no pointer-reachable dismissal — two prior review patches now collide. `TenantConfigurationManagement.razor.css:40-49` deliberately stopped hiding the removable-target grid at narrow widths (its comment records why), so the per-row Remove launcher is clickable on mobile; `RemoveTenantConfigurationFlow.razor.css:117-131` still sets `.tenants-config-remove__form { display: none }` at the same breakpoint, and that form contains the confirmation input, Submit, Refresh **and Cancel**. The result is a `role="dialog" aria-modal="true"` region holding only a heading and the localized narrow substitute, dismissable only by Escape and only when focus is already inside. Options: (a) re-hide the targets grid at ≤767px, restoring symmetry with the Set flow whose Open button is hidden at that breakpoint; (b) render Cancel outside `.tenants-config-remove__form` so it survives the narrow rule; (c) hide only the launcher column and keep the grid readable.

Patches — correctness:

- [x] [Review][Patch] [HIGH] A successful removal unmounts its own confirmation before the proof is applied: `RefreshStatusAsync` awaits `OnProjectionRefreshRequested` (which re-reads detail and drops the removed key from `Context.RemovableRows`) *before* computing the proof, so `OnParametersSet` nulls `_removeKey` and the render gate unmounts the flow; `SetSnapshot(_snapshot.ConfirmProjection(proof))` then renders into a dead component. The operator never sees Confirmed, the audit entry point, or recovery text, and because `CloseRemoveFlowAsync` never runs, focus is orphaned on the destroyed subtree while the prune loop also drops the launch element. [`src/Hexalith.Tenants.UI/Components/Tenants/Configuration/TenantConfigurationManagement.razor:166-169`; `RemoveTenantConfigurationFlow.razor:562-567`]
- [x] [Review][Patch] [HIGH] The projection proof ignores `ProjectionLifecycleState`, so confirmation is weaker than submission: this chunk added `Lifecycle is not ProjectionLifecycleState.Current` gates to the Set flow, the Remove flow and the management landmark, but the proof that actually flips the truth state gates only on tenant match, `IsNotModified`, non-null payload and `ResolveFreshness(...) is Current`. A projection that is `Rebuilding` or `Unavailable` while reporting Current freshness still returns `SetConfirmed`/`RemoveConfirmed`. [`src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs:2009-2035`]
- [x] [Review][Patch] The read landmark renders a lifecycle badge it excludes from its own truth model — `CanInspect`, `IsDegraded`, `HasNonCurrentState`, `EffectiveFreshness`, `StateResourcePrefix` and `LivePoliteness` never reference `Lifecycle`. With `Lifecycle = Unavailable` and `Freshness = Current`, `HasNonCurrentState` is false, so the header shows an "Unavailable" lifecycle badge beside a "Current" truth badge and no state sentence at all. Every sibling in this diff requires `Lifecycle is Current`; the read view is the sole outlier. [`src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor:180-222`]
- [x] [Review][Patch] The remove preview reports a namespace that contradicts both the read grid and the Set flow: `NamespacePrefix` splits on the first dot, while `TenantConfigurationSafeComposer.TryResolveNamespace` and `SetTenantConfigurationFlow.ResolveAuthorizedNamespace` both resolve the longest matching authorized prefix. With grant `app.feature` and key `app.feature.flag`, the destructive-command preview — the operator's consequence contract — claims scope `app`. [`RemoveTenantConfigurationFlow.razor:617-621`]
- [ ] [Review][Patch] `AlreadyApplied` is decided from render-time values: `Reauthorize` re-resolves *policy* against the already-composed snapshot rows and performs no projection re-read, so `currentContext.FindRemovableRow(...).Value` is byte-identical to the parameter context's. A value changed server-side since the last read yields a terminal `AlreadyApplied` rendered with the "OK" symbol, with no command sent and no proof requested. The grant-revocation half of the guard is real; the value half is not. [`SetTenantConfigurationFlow.razor:543-550`; `src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationSafeComposer.cs:60-92`]
- [x] [Review][Patch] `FocusAsync` on a detached `ElementReference` throws an unguarded `JSException`: the prune loop keys liveness on `Context.RemovableRows` (data) rather than on whether the grid was rendered, but the `@ref` spans render only inside the `else` branch gated by `UnavailableReason`. When a background refresh flips freshness or lifecycle in the same cycle as a Cancel, the keys survive both the prune and the `_focusRemoveLaunchKey` guard, and `OnAfterRenderAsync` focuses a detached reference. The `catch` handles only `JSDisconnectedException`, so the circuit tears down. [`TenantConfigurationManagement.razor:174-205`]
- [x] [Review][Patch] Ten `FocusAsync` calls across the two flows carry no `JSDisconnectedException` guard (Set 4, Remove 6, both with zero guards) while the third component added in this same diff has one. A queued focus that resolves after the circuit drops throws into `OnAfterRenderAsync`. [`SetTenantConfigurationFlow.razor`; `RemoveTenantConfigurationFlow.razor`]
- [x] [Review][Patch] `RefreshStatusAsync` has no reentrancy guard and read-modify-writes `_snapshot` across awaits, so two overlapping runs let a stale status overwrite a newer terminal one. `SubmitAsync` has an explicit `_isSubmitting` guard; `CanRefresh` does not consider an in-flight refresh, and Blazor re-renders (re-enabling the button) at the first incomplete await. Three entry points reach it: the Refresh button, `SubmitAsync`'s own call, and `AuditAvailabilityState.OnRefresh`. [`RemoveTenantConfigurationFlow.razor:537-570`; `SetTenantConfigurationFlow.razor:596-627`]
- [x] [Review][Patch] Cancel/Escape dismisses the remove dialog while the destructive command is in flight — `CloseAsync` has no `IsOwnedCommandInFlight` check although `IsSubmitDisabled` does. The command still executes; its Confirmed/Rejected/UnableToVerify outcome, safe message and audit entry point become unrenderable. [`RemoveTenantConfigurationFlow.razor:607-612`]
- [x] [Review][Patch] The Set preview cannot distinguish "no such key" from "value withheld": `current?.Value ?? …CurrentState.Unavailable` where `FindRemovableRow` returns only display-safe rows, and `Set.Preview.CurrentState.Absent` was deleted (0 occurrences in either `.resx`). Creating a new key and overwriting a key whose value is not `DisplaySafe` now render identical previews. [`SetTenantConfigurationFlow.razor:300-302`]
- [x] [Review][Patch] Off-Dispatcher check-then-write of `_snapshot`: after `.ConfigureAwait(false)`, `if (CanApply(generation, tenantId)) { _snapshot = snapshot; }` runs on a thread-pool continuation with no `InvokeAsync` and no re-check, while every sibling path in the same file marshals and re-checks inside `InvokeAsync` with an explicit comment saying why. [`TenantDetailPage.razor:503-506`]
- [x] [Review][Patch] `FluentDataGrid` lost its `ItemKey`, and the new management grid never had one. `TenantConfigurationSafeRow` is a class without value equality and rows are rebuilt on every read, so each refresh diffs every row as new: full re-render, `@ref` re-capture into `_removeLaunchElements`, and focus lost from inside a row on every SignalR-driven refresh. [`TenantConfigurationView.razor:103-107`; `TenantConfigurationManagement.razor:57-60`]

Patches — accessibility, copy and layout:

- [x] [Review][Patch] The non-current truth state is never announced, while the routine result count is what gets escalated to assertive. On the `CanInspect` path the stale/degraded/unknown sentence is a bare `<p>` inside `div.tenant-config__truth` with no role and no `aria-live`; the only live region is the announcer, whose content is the entry/group count and whose politeness is `assertive` when degraded. A degraded read therefore announces the count assertively and never announces that it is degraded — the inverse of the story's announcement rule. [`TenantConfigurationView.razor:19-22,61-67,221-222`]
- [x] [Review][Patch] The focusable scroll region has no visible focus indicator: `.tenant-config__table-wrap` carries `role="region" tabindex="0"`, but the focus rule is `.tenant-config__table-wrap [tabindex]:focus-visible`, a *descendant* selector that cannot match the wrap itself, and the descendants it used to match were deleted by this same diff. The sibling landmark gets this right with `.tenant-config-management__targets:focus-visible`. [`TenantConfigurationView.razor.css:116-121,162-166`]
- [x] [Review][Patch] `role="status"` on a direct `<dl>` child breaks the definition-list association for the projection-lifecycle fact: ARIA-in-HTML permits only `dt`, `dd`, generic `div`, `script` and `template` as `dl` children, so overriding that div's generic role detaches its `dt`/`dd` pair. `aria-live="assertive"` on `role="status"` (implicitly polite) is additionally contradictory; `role="alert"` is the correct pairing. Every sibling fact div is left generic. [`TenantDetailPage.razor:120-127`]
- [x] [Review][Patch] `aria-label` on `<code>` is name-prohibited (HTML-AAM maps `<code>` to role `code`), so `Tenants.Configuration.KeyAccessible` and `ValueAccessible` never reach the accessibility tree in either locale and axe flags `aria-prohibited-attr`. The literals still read as text content, so the safety floor holds. [`TenantConfigurationView.razor:116-117,122-124`]
- [x] [Review][Patch] Degraded and Unknown render an identical truth badge — `EffectiveFreshness => IsDegraded ? Unknown : Freshness` collapses both into label "Unknown" with the same colour and icon, in the header and in every per-row cell. The only surviving discriminator is the state sentence that the announcement finding above shows is never announced. AC5 requires these to stay distinct. [`TenantConfigurationView.razor:190-191`]
- [x] [Review][Patch] The unavailable sentence renders twice: `HasNonCurrentState` is true whenever `!CanInspect`, so the header emits `<p>@StateMessage</p>` and the state section immediately below emits the same string again under its heading. [`TenantConfigurationView.razor:19-22,32-33`]
- [x] [Review][Patch] Six state resources are unreachable through the composed surface and one is reachable only when it would be wrong. `TenantConfigurationView` renders solely inside `FcAggregateDetailPage`'s `ReadyContent`, which is gated to `Ready|Stale|Degraded`, so `SurfaceKind` is never `Loading` and `StateTitle` always resolves to the `Unavailable` prefix: `State.Loading`, `State.Loading.Title`, `State.Ready.Title`, `State.Stale.Title`, `State.Degraded.Title` and `State.Unknown.Title` are dead in both locales (12 entries). `State.Ready` is reachable only via `ReadModelFreshnessState.Aging`, which would render "Configuration evidence is current." beside a warning `Aging` badge — latent today because `ResolveFreshness` never emits `Aging`. [`TenantConfigurationView.razor:199-222`]
- [x] [Review][Patch] `IsAuthorized` is declared on both flows with default `true` but never passed by the only call site, so `!IsAuthorized || SurfaceKind is Unauthorized` can never be true (the page routes `Unauthorized` away from `ReadyContent`). Not a security hole — submit re-authorizes — but the gate reads live and `Set.Unavailable.Authorization` / `Remove.Unavailable.Authorization` are dead in both locales. [`TenantConfigurationManagement.razor:34-42,89-99`]
- [x] [Review][Patch] Per-row freshness and lifecycle columns bind the single page-scoped `EffectiveFreshness`/`Lifecycle`, so an N-row table emits 2N identical badges and 2N identical accessible names implying per-key evidence that does not exist, all sharing one `data-testid`. With `ItemKey` also gone, no stable per-row selector remains. [`TenantConfigurationView.razor:126-138`; `TenantConfigurationManagement.razor:69,76`]
- [x] [Review][Patch] Two live regions announce simultaneously in the empty state — the always-rendered announcer emits "0 visible configuration entries across 0 namespace groups" at the same moment the empty panel's `role="status"` emits the empty message. [`TenantConfigurationView.razor:61-77`]
- [x] [Review][Patch] Namespace accordion groups are rendered without `@key`, so Blazor reuses `FluentAccordionItem` instances positionally and the component's DOM-held collapsed state follows position rather than namespace: collapse the first group, filter it away, and the next namespace appears collapsed. [`TenantConfigurationView.razor:98-101`]
- [x] [Review][Patch] `id="tenants-config-management-unavailable"` is emitted on both unavailable branches but referenced by no `aria-describedby`/`aria-errormessage`, so the "one localized, programmatically associated reason" the story requires is not associated. The sibling Set flow does wire its equivalent. [`TenantConfigurationManagement.razor:17-29`]

Patches — verification that does not verify:

- [ ] [Review][Patch] The page→management→flow provider wiring is never exercised. All nine management renders pass only `Context`/`SurfaceKind`/`Freshness`/`Lifecycle`; no test submits a set or remove through `TenantDetailPage`. Rewriting the page wrapper as `=> Task.FromResult(_snapshot.ConfigurationManagement)` — returning the render-time context instead of re-resolving policy — fails **open** and survives the whole suite. The same defect class was closed one layer down at the composition seam, not at this page seam. [`TenantDetailPage.razor:205-214,898-905`]
- [x] [Review][Patch] Fail-closed handling of a null or throwing `ReauthorizeProvider` is unverified. Every test that reaches submit with a valid request sets the provider, and every submit without it fails `TryBuildRequest` first, so `currentContext = Unavailable(...)` on both the null branch and the catch branch is never executed. No provider anywhere in `tests/` throws. Mutating either branch to `Context` (fail open) survives. [`SetTenantConfigurationFlow.razor:521-532`; `RemoveTenantConfigurationFlow.razor:471-482`]
- [ ] [Review][Patch] Global-administrator scope is absent from every component fixture (`isGlobalAdministrator: false` everywhere, end-to-end authenticates `global_admin=false`), so the admin branches are unpinned: deleting `!Context.IsGlobalAdministrator &&` or the `IsGlobalAdministrator` branch of `ResolveAuthorizedNamespace` permanently blocks a global administrator from setting configuration and no test fails. [`SetTenantConfigurationFlow.razor:259,716-729`]
- [x] [Review][Patch] `Already_applied_is_decided_from_the_re_authorized_context` cannot fail — the test passes the *same context instance* as both `Context` and the `ReauthorizeProvider` result, so `Context.FindRemovableRow(...)` and `currentContext.FindRemovableRow(...)` are indistinguishable, despite the comment asserting the opposite. [`tests/Hexalith.Tenants.UI.Tests/Components/SetTenantConfigurationFlowTests.cs:256-282`]
- [ ] [Review][Patch] Longest-prefix authorization-evidence selection is untestable with the current fixtures: both flow suites derive prefixes through a helper that splits on the first dot, so `AuthorizedPrefixes` never contains two prefixes matching one key. `OrderByDescending(length)` → `OrderBy(...)` survives, and the preview's Namespace row is the authorization evidence shown before a high-impact change. [`SetTenantConfigurationFlow.razor:724-728`]
- [ ] [Review][Patch] Per-read fault handling on the detail page is unverified — no test faults `ITenantQueryGateway`. Replacing the two independent try/catch blocks with a bare `Task.WhenAll` survives, leaving the member surface stuck on Loading with an unobserved task exception, exactly what the code comment says it prevents. [`TenantDetailPage.razor:340-369,386-387`]
- [x] [Review][Patch] The management landmark's stale-target reset and element-reference prune never execute in any test — no management render is re-rendered with a changed `Context`, so `OnParametersSet` only ever runs with `_removeKey == null` and an empty dictionary. Both guarded bodies, and both defects they exist to prevent, are unreachable in the suite. [`TenantConfigurationManagement.razor:164-188`]
- [ ] [Review][Patch] The detail page's configuration summary cannot distinguish "unavailable" from "empty" under test: deleting the `!configuration.IsAvailable` branch makes unverifiable configuration evidence render the absence claim "No visible configuration is available in this detail projection", and no test asserts the unavailable string. The sibling read landmark keeps exactly this distinction under test. [`TenantDetailPage.razor:919-927`]

Deferred:

- [x] [Review][Defer] Read-refresh lease retry (empty lease / superseded setup) is unverified on the detail page while the sibling global-administrators page pins it — every detail-page test stubs a successful subscription, so the `!lease.IsSubscribed` early return and the `OnAfterRenderAsync` retry are both unexecuted. [`TenantDetailPage.razor:394-400,441-446`] — deferred, shared read-refresh pattern rather than Story 1.6 surface.
- [x] [Review][Defer] An in-flight `RefreshTenantReadsAsync` is aborted silently by a concurrent detail refresh: the documented guard covers only `_memberPageLoadInFlight`, which the read-refresh path never sets, so `BeginLoad()` cancels the shared token and clears `IsRefreshing` while the member read is outstanding. Unverified in either direction. [`TenantDetailPage.razor:470-482`] — deferred, member-paging surface owned outside Story 1.6.
- [x] [Review][Defer] The working tree carries an undeclared `references/Hexalith.EventStore` gitlink bump (`a40ab8a` → `e4618d9`, v3.86.0) that belongs to no story File List. — deferred, not Story 1.6's change; needs a separate `build(deps)` commit or a revert by whoever is holding it.

#### Patch application results (2026-07-31)

All 34 patches were accepted for application. Outcome:

- **27 applied and verified.** Both HIGH findings are closed: the remove flow now applies its projection proof
  before requesting the parent refresh, and the management landmark holds an open flow mounted once a command
  has started (releasing it only on explicit close or a tenant switch), so a proven removal can no longer
  destroy its own Confirmed state, audit entry point and focus target. The projection proof now carries the
  same `ProjectionLifecycleState.Current` clause the submission gates already carried.
- **1 applied, then reverted on evidence** — the proof-based `AlreadyApplied` probe. Calling
  `ProjectionEvidenceProvider` before submit turned a post-submit confirmation seam into a pre-submit
  authoritative read on every submit, and broke six existing tests by short-circuiting any flow whose provider
  reports confirmed. The finding is downgraded to an accepted bounded limitation: the claim is at most one
  read stale, the landmark renders only while freshness and lifecycle are Current, and the failure direction is
  one-way — a stale match declines to write a value the UI believes is already set, it never writes one. The
  misleading comment that prompted the finding has been corrected in place.
- **1 resolved differently than proposed** — the Set preview's absent-versus-withheld conflation. Restoring
  `CurrentState.Absent` would have leaked the existence of authorized-but-not-display-safe keys, which AC3
  forbids. The conflation is mandated; the copy was made honest instead ("Not visible — this key has no
  approved value to display, or no value is set." / EN+FR).
- **5 verification patches deliberately left open** (unchecked above) — they belong to the deferred
  test/evidence chunk, which will cover that ground with its own review rather than in passing here.

Resource changes: `Header.Freshness`, `KeyAccessible` and `ValueAccessible` deleted (unused after the per-row
badge columns and the ARIA-prohibited `aria-label`s were removed); `State.ProjectionLifecycle{,.Title}` and
`Set.Validation.KeyLiteral` added. EN/FR parity holds at 1223 keys each, verified by parse. `State.Loading`
remains reachable only for a consumer rendering the landmark outside `FcAggregateDetailPage`'s `ReadyContent`;
it is retained as component contract and documented in place rather than deleted.

Verification (all commands run 2026-07-31):

- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore -warnaserror -m:1 -nr:false` — 0 warnings, 0 errors.
- `dotnet build Hexalith.Tenants.slnx -c Release --no-restore -warnaserror -m:1 -nr:false` — 0 warnings, 0 errors.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` — **1654/1654** (was 1641; +13 net: 10 new regression tests, 4 new lifecycle theory cases, and one reflection assertion retired with the dead `IsAuthorized` parameter).
- Focused: `TenantDetailSurfaceTests` 100/100, `SetTenantConfigurationFlowTests` 40/40, `RemoveTenantConfigurationFlowTests` 18/18, `TenantQueryGatewayTests` 335/335, `TenantConfigurationReadPolicyTests` 48/48, `DomainUiFluentConformanceTests` 51/51, `TenantConfigurationEndToEndTests` 1/1.

New regression tests added: projection-proof fail-closed on four non-Current lifecycles; management keeps an
open flow mounted after its target row leaves the context; management still closes an untouched flow whose row
disappears; a tenant switch drops every open interaction even mid-command; already-applied decided from a
genuinely different re-authorized context; submit fails closed with no reauthorize provider; submit fails
closed and support-safe when reauthorization throws; keys that cannot be reproduced by typing are rejected.
