---
baseline_commit: b73093bd10608afe4e6036439a48a08924d0358b
---

# Story 1.6: Read-Only Tenant Configuration

Status: review

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

 Add the typed, deployment-owned configuration-read policy and fail-closed BFF composition seam (AC: 2, 3, 4, 5, 10)
 Define Tenants-owned `Tenants:ConfigurationReadPolicy` options and runtime semantic validation under `src/Hexalith.Tenants.UI/Services/Gateways/` (or a focused `Services/Configuration/` subfolder). Add one idempotent registration extension and call it from both `Program.cs` and `TenantsUiServiceCollectionExtensions.cs`; include an explicit valid-empty section in `appsettings.json`. Do not put grants or safe-value decisions in browser code, a public query contract, or `references/`.
 Model ordinary-user grants as literal `(tenantId, authenticatedSub, prefix)` entries. Role alone grants no namespace. Obtain the subject from the server-side `IUserContextAccessor`; never trust an entered prefix, a browser-only claim, or visible projection keys as authorization evidence.
 Add a configuration-policy-specific administrator reflection with three outcomes: proven global administrator grants wildcard scope; proven non-administrator evaluates only explicit ordinary-user grants; missing/malformed/ambiguous principal evidence is indeterminate and makes configuration unavailable. Do not reuse the current two-outcome `GlobalAdministratorsAuthorizationReflection` in a way that blocks ordinary users or turns claim absence/malformation into wildcard access.
 Compare tenant ids, subjects, prefixes, and configuration keys with `StringComparison.Ordinal`, without trimming, case folding, Unicode normalization, GUID/ULID parsing, or delimiter rewriting. A non-empty, non-whitespace prefix that does not end in `.` authorizes only exact key `P` or keys beginning `P.`. When multiple grants match, the longest ordinal prefix is the consumer namespace; reject duplicate grants, duplicate safe-key declarations, and conflicting declarations during semantic validation. Add boundary tests so `a`, `a.`, `ab`, `A`, leading/consecutive empty segments, and visually confusable prefixes cannot broaden scope.
 Model display approval as a positive exact-full-key `DisplaySafe` registry whose keys are non-empty literal strings. A key must pass both namespace authorization and exact-key display approval before a component-facing row is constructed. Missing, malformed, conflicting, unregistered, or indeterminate policy omits the complete entry, including its key and value; a blacklist-negative result is never approval.
 Bind the section without startup-fatal validation. The semantic validator returns a valid policy or an unavailable policy result at composition time; it must catch safe binding/validation failures and never expose exception details. `ValidateOnStart` is prohibited for this section because malformed deployment policy must render the required localized unavailable/recovery state rather than terminate the host.
 Return an explicit safe composition result that distinguishes valid-empty from policy/authentication unavailable. Error details, policy contents, hidden literals, raw counts, and configuration values must not enter messages, logs, metrics, `ToString()` output, or telemetry.

 Ensure raw configuration is transient inside the server-side BFF and never enters any Razor component state (AC: 2, 3, 4, 5)
 Apply the policy after the server-side tenant-detail response is received but before `TenantDetailSnapshot`, a page view model, a Razor parameter, or a projection-evidence callback result is constructed. Add dedicated safe read and management DTOs under `src/Hexalith.Tenants.UI/State/TenantDetail/` (or `State/TenantConfiguration/`) containing only authorized/display-safe rows, tenant identity needed by commands, proven prefixes, and policy-safe state/recovery metadata.
 Update `TenantQueryGateway.GetTenantAsync` and/or its dedicated composition dependency so ready, stale, degraded, `304`, and last-confirmed paths retain only previously composed safe rows. Replace the component-facing raw `TenantDetail` payload with a safe tenant-detail view model, or otherwise prove its configuration member contains only composed safe rows. Never fall back to `TenantDetail.Configuration` when the policy, subject, or administrator reflection is missing or invalid.
 Keep `GetTenantQuery` / `GET /api/tenants/{tenantId}` as the existing read source. Do not add a configuration endpoint, change `TenantDetail` public contracts, move filtering into the browser, read EventStore state directly, or absorb Story 1.10 direct-read/provenance work.
 Treat a valid policy with zero visible rows as successful authorization-safe empty. Treat missing/malformed policy, missing authenticated subject, indeterminate policy-specific administrator reflection, and composition failure as an explicit localized unavailable state—not as empty, current, or successful. Map initial transport/policy errors to localized unavailable; map a failed refresh with a qualifying last-confirmed safe model to degraded/continue-read-only. Add focused tests for both error paths and their announcement/recovery copy.
 Derive every configuration summary, group, filter result, announcement, count, management target list, and empty-state decision from the safe model only. Remove the raw `Detail.Configuration.Count` and raw prefix-count summary path from `TenantDetailPage.razor`.

 Make `TenantConfigurationView` a strict read-only consumer of the safe model (AC: 1, 4, 5, 6, 7, 8, 9)
 Change `TenantConfigurationView.razor` to accept only the safe configuration read model. Remove `Detail`, set/remove projection delegates, command-availability parameters, command state, per-row action cells, and all `SetTenantConfigurationFlow` / `RemoveTenantConfigurationFlow` composition from this read landmark.
 Remove `LegacyConfigurationDisplaySanitizer` from every configuration-read path. It is still used by the existing Epic 3 command previews; keep any command-only use isolated and explicitly transitional unless the positive policy can replace it without weakening or broadening those flows. It must never act as a fallback, display approval, safety badge source, or copy classifier for the read model.
 Keep the proven Fluent/FrontComposer patterns that remain applicable: visible inspection-only title/description, explicit truth state, authorization-safe empty and filtered-empty states, a scan/filter control that is not presented as editing, multi-expand namespace groups, `FluentDataGrid`, and stable `tenants-config-read-*` selectors.
 Preserve semantic namespace/key/value relationships and accessible headings. Use literal text rendering, logical focus order, dedicated announcement intent, visible focus, reduced-motion compatibility, and forced-colors-safe meaning; no selector, DOM id, or focus target may be derived from a raw key/value or localized string.
 Preserve long, empty-segment, reserved-character, markup-like, bidi/Unicode, and visually confusable literals without transformation. Use safe wrapping or horizontal availability at 320–767, 768–1023, 1024+, and 1440+ widths; never drop namespace, value-safety, or truth-state context.
 Render loading, unavailable, stale, degraded, unknown, and mapped error states distinctly. Read-unavailable, routine results, and recovery progress use polite announcements; assertive is reserved for rejection, failure, unable-to-verify, degraded, or destructive-block intent. Continue-read-only may show only a qualifying last-confirmed safe model.

 Preserve Epic 3 set/remove behavior in a separate management landmark (AC: 2, 3, 6, 7, 10)
 Add a sibling Tenants-owned configuration-management component under `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/` and compose it beside—not inside—the read-only landmark within the existing expanded Configuration accordion item on `TenantDetailPage.razor`.
 Move the existing `SetTenantConfigurationFlow` and `RemoveTenantConfigurationFlow` entry points into that management landmark, then replace their component-facing `TenantDetail` inputs and `Task<TenantDetail?>` projection callbacks with safe management context and proof-only projection-evidence DTOs. No Razor component—including management—may receive a server-returned raw configuration dictionary. Preserve Story 3.5/3.6 authorization, projection freshness, complete preview, focus return, narrow-layout fail-closed behavior, aggregate-scoped command locking, duplicate prevention, projection confirmation, audit/recovery states, and refresh callbacks.
 Let set operations use only proven ordinary-user prefixes or the explicit global-administrator wildcard from the same policy result. Let remove operations target only keys in the current safe read model. Missing/invalid policy, stale/unknown/degraded truth, or absent safe targets keeps management unavailable with one localized, programmatically associated reason.
 Perform set/remove projection comparison inside the server-side BFF against the transient raw response, then return only boolean/version/lifecycle proof needed by the command state machine. A set key/value that is not positively `DisplaySafe` may be submitted under the proven command scope but must remain absent from read/component state; confirmation or no-op proof must not echo the projected key/value. Removal proof likewise returns presence/absence status without a raw dictionary.
 Keep read and management state separate: command lifecycle updates, preview values, validation, and action announcements must not mutate the safe read model or appear inside `tenants-config-read-*` markup. Do not add configuration-value copy or reveal controls; the existing Story 1.8 certification remains a separate follow-up.

 Update localized resources, documentation evidence, and focused regression coverage (AC: 1–10)
 Add/revise whole-string `Tenants.Configuration.*` resources in `TenantsResources.resx` and `TenantsResources.fr.resx` for inspection-only copy, policy-unavailable recovery, authorization-safe empty, truth states, and the separate management landmark. Preserve exact EN/FR key parity and named placeholders; do not assemble sentences from fragments.
 Add pure policy/composer tests for ordinary grants, proven non-administrator handling, indeterminate administrator reflection, global-administrator wildcard, longest-prefix matching, rejected invalid/trailing-dot/duplicate declarations, exact full-key approval, ordinal/case-sensitive boundaries, missing/malformed/conflicting policy, zero visible entries, hidden counts/existence, and safe last-confirmed/`304` handling.
 Update `TenantQueryGatewayTests` and `TenantDetailSurfaceTests`; add focused tests if clearer. Prove that forbidden and undefined-policy keys/values are absent from snapshot/component state, DOM, accessible names, announcements, filter text, copy inputs, logs, telemetry, exception strings, and management targets—not merely visually redacted.
 Preserve and re-run `SetTenantConfigurationFlowTests` and `RemoveTenantConfigurationFlowTests` to prove relocation did not weaken Epic 3 behavior. Add assertions that read markup contains no action column, buttons, inputs that mutate data, forms, command lifecycle content, or misleading command affordances.
 Cover valid-empty versus policy-unavailable, initial error-to-unavailable and failed-refresh-to-degraded mapping, loading/stale/degraded/unknown/unavailable, overlap/boundary prefixes, long/markup-like/bidi/Unicode/confusable values, EN/FR parity, stable data-independent selectors, keyboard/focus/live-region behavior, table semantics, responsive overflow, forced colors, reduced motion, and `DomainUiFluentConformanceTests`.
 Update `tests/test-summary.md` and the Dev Agent Record with exact commands/results, current package evidence, NFR10 accessibility/localization/responsive/documentation evidence, and any genuinely unresolved display-policy decision. Close or update the Story 1.8 `CFG-1.6-SAFE-MODEL` deferred evidence only when the positive model is proven; do not claim configuration clipboard certification automatically.

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

### Change Log

- 2026-07-22: Completed the corrective positive-policy safe configuration model, sanitized BFF integration, strict read-only landmark, sibling safe management/proof boundary, localization, accessibility, focused/end-to-end coverage, and repository-wide validation; moved Story 1.6 to review.

### Review Findings

Adversarial code review 2026-07-27 over `2f190a1..ec7ec8c` (49 files, +3800/-967). Four independent layers: blind-hunter, edge-case-hunter, verification-gap, acceptance-auditor. Every finding below was re-verified against the real source at `HEAD` (`0ded4a1`) before being recorded; findings already resolved by later commits were dismissed rather than listed.

#### Decisions resolved (2026-07-27, by story owner)

- **RESOLVED → option A.** Global-administrator claim present but `eventstore:tenant=system` absent resolved to Indeterminate, stripping the user's *explicit* `(tenantId, sub, prefix)` grants as well as the wildcard. An admin role scoped to a non-system tenant is a well-formed claim that simply does not meet the bar — that is proven-non-administrator, not ambiguous evidence. Decision: fall back to `NonAdministrator` so explicit grants are honoured. Recorded as a patch below.
- **RESOLVED → option B.** The forbidden `references/Hexalith.EventStore` bump (`c6b72ca` → `440ff4c`) is absorbed into main — nine later commits touched the gitlink and the story SHA is an ancestor of the current pointer `c8c7003`, so no revert is possible. Decision: correct the false Completion Notes claim *and* log a sprint action item, because Story 1.4's commit `41e047e` exhibited the identical defect (bundled submodule bumps while claiming untouched). Second occurrence makes it a pattern worth guarding, not a slip.

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
- [~] [Review][Patch → NOT IMPLEMENTABLE AS SPECIFIED] `"DisplaySafe": ""` binding to an empty allow-list instead of failing closed. **Attempted, then reverted.** The JSON configuration provider materializes `"DisplaySafe": []` and an emptied `Tenants__ConfigurationReadPolicy__DisplaySafe` environment override to the *same* observable state (`IConfigurationSection.Value == ""`, no element children), so this layer cannot distinguish them. Treating an empty value as a scalar made the shipped `appsettings.json` valid-empty default resolve to unavailable — which the kernel explicitly forbids ("a present, semantically valid section with empty arrays is the repository default"). Verified empirically: implementing it failed `Global_administrator_receives_only_the_namespace_wildcard…` and `Valid_empty_policy_is_safe_empty…`, and both passed again on revert. Closing this needs a policy shape that distinguishes the two cases — e.g. a required explicit discriminator — which is a spec change, not a patch.
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
- 2026-07-27: Adversarial code review over `2f190a1..ec7ec8c`; 2 decisions resolved, 39 patches applied (1 reclassified as not implementable), 2 items deferred. UI suite 1281 → 1312.
