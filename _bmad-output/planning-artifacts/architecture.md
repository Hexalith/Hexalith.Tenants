---
stepsCompleted: [1, 2, 3, 4, 5, 6, 7, 8]
inputDocuments:
  # --- PRD (final) + process artifacts ---
  - _bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md
  - _bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md
  - _bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/.decision-log.md
  - _bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/reconcile-a11y-l10n.md
  - _bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/reconcile-audit-recovery.md
  - _bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/reconcile-frontcomposer-depmap.md
  - _bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/reconcile-operations-shell.md
  - _bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/reconcile-phase-2-backlog.md
  - _bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/reconcile-remove-user-journey.md
  - _bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/reconcile-responsive-visual.md
  - _bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/reconcile-truth-state.md
  - _bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/review-adversarial.md
  - _bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/review-domain-fidelity.md
  - _bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/review-downstream-readiness.md
  - _bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/review-rubric.md
  # --- UX (final spines) + process artifacts ---
  - _bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md
  - _bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md
  - _bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/.working/prd-ux-digest.md
  - _bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/.decision-log.md
  - _bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/review-accessibility.md
  - _bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/review-rubric.md
  # --- Implementation readiness ---
  - _bmad-output/planning-artifacts/implementation-readiness-report-2026-06-02.md
  # --- Domain & UI specs (docs/) ---
  - docs/tenants-ui-frontcomposer-dependency-map.md
  - docs/tenants-ui-operations-shell-spec.md
  - docs/tenants-ui-truth-state-and-action-availability-spec.md
  - docs/tenants-ui-responsive-layout-and-visual-system-spec.md
  - docs/tenants-ui-remove-user-from-tenant-journey-spec.md
  - docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md
  - docs/tenants-ui-accessibility-localization-and-acceptance-evidence-spec.md
  - docs/tenants-ui-phase-2-story-backlog.md
  - docs/event-contract-reference.md
  - docs/compensating-commands.md
  - docs/cross-aggregate-timing.md
  - docs/idempotent-event-processing.md
  - docs/production-auth-claim-contract.md
  - docs/production-auth-readiness.md
  - docs/deployment-readiness.md
  - docs/quickstart.md
  - docs/demo.md
  - docs/sample-consuming-service-walkthrough.md
  # --- Project context (AI agent rules) ---
  - _bmad-output/project-context.md
  - references/Hexalith.Commons/_bmad-output/project-context.md
  - references/Hexalith.EventStore/_bmad-output/project-context.md
workflowType: 'architecture'
lastStep: 8
status: 'complete'
completedAt: '2026-06-03'
updatedAt: '2026-07-15'
architectureSpineMerged: true
architectureSpineArchive: '_bmad-output/archive/planning-artifacts/architecture-tenants-2026-06-25'
project_name: 'Hexalith.Tenants'
user_name: 'Administrator'
date: '2026-06-02'
---

# Architecture Decision Document

_This document builds collaboratively through step-by-step discovery. Sections are appended as we work through each architectural decision together._

## Canonical Architecture Spine

This section merges the finalized feature-level architecture spine into the canonical architecture document. **AD-1 through AD-14 are the precedence layer for Tenants UI composition.** Earlier descriptive sections remain useful context and implementation history, but where their navigation, state-management, ownership, host, or operations wording conflicts with these ADs, the ADs govern.

### Design Paradigm

Tenants Management UI is a **FrontComposer-composed domain UI**: FrontComposer owns shell, page chrome, domain registration, and reusable UI primitives; Tenants owns tenant-domain surfaces, domain state vocabulary, server-side gateway composition, and support-safe user-facing domain copy. The event-sourced backend is inherited context rather than a second UI architecture.

### AD-1 - Tenants Is One FrontComposer Module Entry [ADOPTED]

- **Binds:** FR-1..FR-4, FR-18..FR-21, Operations Shell IA.
- **Prevents:** independently-built surfaces registering separate shell entries for All Tenants, My Tenants, Users, Global Administrators, or Audit.
- **Rule:** Tenants contributes exactly one shell navigation entry at `/tenants`; Tenants-domain sub-surfaces live as page-local tabs, scope modes, aliases, or contextual links inside the module workspace.

### AD-2 - Page-Local Tabs Own Tenants Sub-Surface Switching [ADOPTED]

- **Binds:** `/tenants`, `/tenants/my`, `/tenants/users`, tenant detail, global-administrator, and audit return flows.
- **Prevents:** shell navigation, route aliases, and return links from encoding incompatible information architecture.
- **Rule:** `/tenants` plus `tab=tenants|users`, `scope=all|mine`, `userId`, `search`, `status`, `sort`, `desc`, and `cursor` is canonical workspace state. Invalid values normalize fail-safe; changing any tab/scope/filter/sort field resets cursor. `/tenants/my` and `/tenants/users` remain renderable compatibility routes, while generated navigation and return URLs use canonical `/tenants` state. `/tenants/{tenantId}`, `/tenants/{tenantId}/audit`, and `/global-administrators` are contextual routes and none registers another shell entry.

### AD-3 - FrontComposer And Fluent Are The First UI Composition Surface [ADOPTED]

- **Binds:** all Razor components, UX-DR1..UX-DR33, and Fluent conformance tests.
- **Prevents:** raw interactive controls, duplicate page chrome, theme redefinition, and Tenants-owned generic UI infrastructure.
- **Rule:** use FrontComposer or Fluent UI Blazor V5 components before custom markup; custom CSS or raw semantic markup is allowed only for documented gaps not covered by FrontComposer or Fluent.

### AD-4 - Tenants Owns Domain Composition, Not Generic UI Infrastructure [ADOPTED]

- **Binds:** `Components/Tenants`, `Components/Users`, `State`, `Services`, and `Resources`.
- **Prevents:** generic grids, tabs, shell layout, theme primitives, or reusable command chrome being implemented inside Tenants.
- **Rule:** Tenants-specific components may encode tenant safety, freshness, audit, support-safety, and command behavior; reusable UI capability is a FrontComposer change or an approved fallback.

### AD-5 - Server-Side Gateways Are The Only Backend Egress [ADOPTED]

- **Binds:** query surfaces, command flows, auth token relay, and Memories search hydration.
- **Prevents:** browser-side backend calls, token exposure, component-to-HTTP coupling, and multiple transport paths for the same data.
- **Rule:** UI components never call Tenants, EventStore, or Memories directly; backend egress goes through `ITenantQueryGateway`, `ITenantCommandGateway`, and their server-side collaborators.

### AD-6 - Direct Tenants REST Reads Are The Read Transport [ADOPTED]

- **Binds:** FR-1..FR-9, FR-18, FR-20..FR-23, NFR-1, and NFR-3.
- **Prevents:** routing tenant reads through the EventStore generic query gateway or retired projection-actor paths that drop projection metadata.
- **Rule:** read composition calls direct Tenants REST endpoints through the BFF and preserves ETag, cursor, authorization, and read-model freshness metadata.

### AD-7 - Projection-Confirmed Truth Is Shared Composition State [ADOPTED]

- **Binds:** truth badges, command lifecycle panels, list/detail/member/audit/global-administrator surfaces, and command flows.
- **Prevents:** optimistic success, per-surface state vocabularies, and collapsing `accepted`, `confirmed`, and `audit available`.
- **Rule:** every actionable surface renders from typed shared truth, freshness, lifecycle, audit, and authorization state; SignalR and command status are nudges until an authoritative projection re-query confirms.

### AD-8 - Freshness Comes From EventStore Read-Model Metadata [ADOPTED]

- **Binds:** action availability, list states, badges, and stale/degraded behavior.
- **Prevents:** duplicate Tenants freshness enums, `ServedAt` as projection age, search results as freshness proof, or 304 responses being treated as recovery without metadata.
- **Rule:** Tenants UI uses `ReadModelFreshnessState`; `Refreshing` is client-transient; stale or unknown data fails closed where the safety contract requires it.

### AD-9 - Domain Copy And Support Safety Stay Tenants-Owned [ADOPTED]

- **Binds:** receipts, consequence previews, rejection text, copy actions, and resource files.
- **Prevents:** shell-owned domain wording, fragment-assembled localization, and unsafe data leaking into rendered output.
- **Rule:** domain-facing text uses Tenants-owned whole-string resources; the BFF assembles and redacts receipts, previews, and rejection text before anything reaches the DOM.

### AD-10 - Memories Is Search-As-Index-Only [ADOPTED]

- **Binds:** FR-1 cross-set tenant search and tenant-list search states.
- **Prevents:** adding a Tenants/EventStore list-filter endpoint, rendering row truth from Memories, or blocking the tenant list on search outage.
- **Rule:** Memories defines an offset result window and returns tenant ids from `tenants-index`; the BFF deduplicates ids in returned order, hydrates and authorization-filters them through the authoritative Tenants read path, and applies the requested deterministic visible sort within the page. The next offset advances by raw Memories hits consumed, including malformed, duplicate, unauthorized, or unhydrated hits; dropped hits are not backfilled and partial hydration is degraded. The search cursor is opaque and bound to the authenticated user plus normalized query/status/sort/page-size scope; mismatch resets page 1 with an honest notice. Memories outage degrades to the cursor list.

### AD-11 - UI Conformance Tests Are Architectural Guardrails [ADOPTED]

- **Binds:** UI tests, route smoke tests, localization parity, selector stability, and support safety.
- **Prevents:** accidental drift from FrontComposer/Fluent composition, raw controls, unsupported routes, or unsafe rendered output.
- **Rule:** every UI surface change updates focused bUnit/conformance coverage; guards are not loosened without an explicit approved story.

### AD-12 - Command Flows Share One FrontComposer Command Posture [ADOPTED]

- **Binds:** FR-10..FR-17, FR-19, FR-24..FR-25, and CP-2..CP-8.
- **Prevents:** independent command flows choosing optimistic success, concurrent submits, bulk action, toast batching, or bypassing preview and gating.
- **Rule:** command UX composes the shared gateway/lifecycle pattern and consequence preview where required. Lock scope is `(interactive circuit, AggregateIdentity)`: one command for that aggregate remains active from submit through accepted/projection-pending until terminal evidence, while unrelated aggregates may proceed. `confirmed` requires the expected postcondition plus projection-version advancement or safe command-specific audit evidence beyond the pre-submit baseline. A pre-existing expected state or NoOp is `already applied`, never `confirmed`; unavailable provenance is `unable to verify`. Unrelated projection data, command status, and SignalR nudges never confirm.

### AD-13 - The UI Host Is Domain-Owned; Orchestration Is Platform-Owned [ADOPTED]

- **Binds:** deployment, local orchestration, auth/service references, and containerization.
- **Prevents:** moving Tenants domain UI into FrontComposer, shipping it as a NuGet package, adding Dockerfiles, expanding a repository-owned AppHost/Aspire surface, or duplicating shared hosting and ServiceDefaults infrastructure.
- **Rule:** `src/Hexalith.Tenants.UI` is a publishable app/container owned by this repository; distributed orchestration belongs to a platform/composing host. The existing `src/Hexalith.Tenants.AppHost` is transitional legacy to migrate or remove and must not gain shared hosting plumbing.

### AD-14 - Production Operations And Scaling Are Platform-Governed [ADOPTED]

- **Binds:** NFR-1..NFR-5, container deployment, configuration, secrets, health, telemetry, and replica count.
- **Prevents:** embedding secrets, treating UI memory as durable truth, shipping bespoke observability, or scaling InteractiveServer with incompatible key/session/cursor assumptions.
- **Rule:** externalize configuration and secrets; consume shared health, telemetry, and non-root SDK-container defaults; keep persistent truth outside the UI host; and do not scale InteractiveServer beyond one replica until shared DataProtection, circuit/session routing, and cursor durability are verified.

### Architecture Conformance Finding - 2026-07-15

The 2026-07-15 reality check found four active implementation divergences:

- **AD-6 / AD-8:** `TenantQueryGateway` currently calls `IEventStoreGatewayClient.SubmitQueryAsync`, and the configured generic handler route normalizes freshness provenance to `Unknown`. Before switching the BFF, the direct Tenants REST surface must propagate ETag/read-model freshness headers and metadata; a composing host must expose distinct Tenants-query and EventStore-command service references; then the UI BFF must split those clients.
- **AD-13:** `src/Hexalith.Tenants.AppHost` remains repository-owned transitional orchestration and must migrate to a platform/composing host rather than expand.
- **AD-14:** the UI host lacks shared health endpoint mapping and OpenTelemetry/ServiceDefaults integration; multi-replica InteractiveServer is not approved until those controls plus shared DataProtection, session routing, and cursor durability are verified.
- **AD-10:** the implemented Memories cursor is a plaintext offset without authenticated-user/query scope binding; replace it with the opaque scoped cursor required by AD-10.

These are remediation items; they do not amend or weaken the ADs.

### Deferred Decisions

- Complete all-users inventory only after an authorization-scoped backend query exists.
- Replace approved fallbacks when FrontComposer ships reusable equivalents.
- Expose freshness `aging` only when EventStore provides the necessary projection metadata on the wire.
- Revisit multi-replica cursor durability when shared DataProtection/cursor durability lands.
- Remove the repository-owned AppHost when a platform/composing host owns the full Tenants topology.
- Reconcile `_bmad-output/project-context.md` with AD-13 and the governing domain-module boundary in a separately authorized project-context update.
- Claim RTL/WCAG 2.2 only after the pinned Fluent/FrontComposer behavior is verified.
- Define sensitive-configuration masking, reveal, and audit policy before exposing it.

## Project Context Analysis

### Requirements Overview

**Functional Requirements — 25 FRs across 9 feature groups, in 3 build phases.**
The PRD defines FR-1..FR-25 as **UI composition over an already-built event-sourced
backend** (no backend endpoints, no contract reshaping). They collapse into two
construction patterns:
- **Read/projection surfaces** (FR-1..FR-9, FR-18; audit-read FR-20..FR-23) — compose
  6 REST projection queries through the FrontComposer/DataGrid composition: cursor
  pagination (never offset/limit), ETag→304 freshness, authorization-scoped results.
  This is the entire MVP.
- **Custom command flows** (FR-10..FR-17, FR-19, FR-24..FR-25) — *not generated CRUD*;
  each dispatches `POST /api/v1/commands` and tracks an async
  `accepted → projection-confirmed → audit-available` lifecycle, with server-side BFF-assembled
  Consequence Previews and Audit Evidence Receipts (no new backend endpoints).
Phasing: **2a/MVP** (read: FR-1..9, FR-18) → **2b** (first commands: FR-10/11/13/14)
→ **2c** (high-impact + audit + recovery: FR-12/15/16/17/19/20–25). Epic 5 now provides
backing stories for FR-20..FR-25, including the flat audit DataGrid, support-safe receipts,
audit availability, tenant-domain correction preview/confirmation, and proof linking. Epic 4
Stories 4.3 and 4.4 now provide FR-19 global-administrator grant/remove command support in the
fixed `global-administrators` scope.

**Non-Functional Requirements — the honesty contract is the architecture driver.**
- **NFR-3 Reliability/consistency (defining):** eventually-consistent, event-sourced;
  projection is the source of truth; correct under at-least-once delivery + projection
  lag; under Blazor InteractiveServer the UI re-derives truth from server-side BFF reads and
  never resurrects optimistic success.
- **NFR-2 Security/authorization:** server-enforced at API (L1) + domain RBAC (L2); the
  UI **reflects, never enforces**, and must stay safe even if it misjudges; role-scoping
  read from JWT claims.
- **NFR-1 Performance/freshness:** cursor pagination + conditional requests (ETag/304);
  ~1s warm tenant-read target remains `[ASSUMPTION]`; audit performance has no numeric target until the Product/Operations §16.14 decision record approves the representative dataset, budgets, test method, and fallback trigger.
- **NFR-4 Observability/testability:** stable automation selectors/component contracts —
  never keyed on row text or color.
- **NFR-5 No data-store edits:** corrections are forward compensating commands only.
On top sits the **CP-1..CP-10 interaction contract** (five truth dimensions, fail-closed
gating, non-collapse invariant, SignalR-nudge-only, consequence-preview-before-
destruction, asymmetric high-risk, correct-forward-never-undo, canonical-vocabulary-
verbatim) — translating directly into a **shared typed UI truth-state model**.

**Scale & Complexity:**
- Primary domain: **Web frontend** — a Blazor InteractiveServer domain UI composed on the
  **Hexalith.FrontComposer** shell, consuming an event-sourced CQRS backend over
  REST + DAPR/SignalR. .NET 10; Fluent UI Blazor v5 pinned `5.0.0-rc.5-26219.1`.
- Complexity level: **HIGH.** Drivers: eventual-consistency correctness as the core
  thesis; a 5-dimension truth-state model with casing-significant canonical vocabularies
  (13 badge / 10 lifecycle / 10 feedback / 6 reasons / 5 freshness / 4 audit) and a strict
  non-collapse invariant; role-scoped multi-tenant authorization reflection; a heavy,
  partly-missing external dependency (FrontComposer) gating even the MVP; first-class
  a11y/l10n/responsive-fail-closed; hard support-safety rules.
- Implemented architectural surface: the **Blazor InteractiveServer UI host** plus shell
  composition, query and command gateways, typed lifecycle/truth snapshots,
  authorization reflection, localization, and domain UI components over FrontComposer.

### Technical Constraints & Dependencies

- **Consume-only backend (fixed):** 6 read endpoints (`GET /api/tenants`,
  `/api/tenants/{id}`, `/api/tenants/{id}/users`, `/api/users/{id}/tenants`,
  `/api/tenants/{id}/audit`, `/api/global-administrators`) + `POST /api/v1/commands` +
  `GET /api/v1/commands/status/{correlationId}`. No new endpoints; receipts/previews/
  status, receipts, and previews assembled server-side in the BFF from safe read-model fields.
- **FrontComposer is the mandated UI framework AND the critical path.** Per repo domain-
  boundary policy, missing shared UI capability belongs in FrontComposer, not Tenants.
  Readiness updated by Story 1.0 spike note (2026-06-05): `FC-LYT`, `FC-CMD`,
  `FC-CNC`, `FC-A11Y`, `FC-L10N`, and `FC-DOC` are confirmed; `FC-TBL` is available
  with a resolved Tenants boundary; `FC-AUD` and `FC-CNS` remain covered by Product/UX-approved fallbacks;
  `FC-TOK` remains a missing shared capability covered by Tenants' canonical vocabulary
  and verified Fluent semantic/icon mapping until a shared token contract exists. The
  FC-AUD/FC-CNS/FC-CNC fallbacks are Product/UX-approved (2026-06-03 - see the
  Fallback Approval Record, `fallback-approval-record-2026-06-03.md`). `FC-TBL` does
  not provide cursor pagination, safety-column pinning, or the six non-collapsing list
  states required by Tenants by itself. Story 1.2 resolved the Epic 1 path by composing a Tenants-specific
  `TenantDataGrid` from Fluent/FrontComposer primitives while keeping generic reusable
  cursor/pinning/list-state capability as a FrontComposer concern.
- **Fluent UI Blazor v5 pinned `5.0.0-rc.5-26219.1`** — exact token/component/ARIA names
  verified against the pinned package at build; none asserted available without check.
- **Current centralized platform package baselines (2026-08-29):** Hexalith.FrontComposer `4.1.1`,
  Hexalith.EventStore `3.100.0`, and Hexalith.Memories `2.21.3`. Debug may consume source
  references, but submodule revisions are implementation state rather than architecture invariants.
- **Identity:** TenantId/UserId are meaningful caller-supplied strings, case-sensitive
  (Ordinal), **NOT ULIDs** — never `Guid`/`Ulid.TryParse`; copy-full-id is literal.
- **Async, eventually-consistent integration:** commands return `202` + correlationId;
  outcome proven only by status poll + projection re-query. DAPR pub/sub is at-least-once;
  **SignalR projection notifications are freshness nudges only.**
- **Cursors:** opaque, signed (DataProtection), scope-bound, session-scoped; multi-replica
  durability is an open backend gap (Epic 11) — UI must handle cursor invalidation.
- **Auth/claims:** JWT `sub` actor; tenant scope via `eventstore:tenant=system`; global-
  admin via `global_admin`/`role` claim shapes; Keycloak (prod) or symmetric-key JWT (dev).
- **Repo/build conventions:** `.slnx` only; central package versions; no copyright headers;
  `ConfigureAwait(false)`; xUnit v3 + Shouldly (+ bunit/Playwright); SDK containers (no
  Dockerfiles); Conventional Commits.
- **UI host placement — resolved by AD-13:** `src/Hexalith.Tenants.UI` is a domain
  presentation host owned by this repository. Distributed orchestration belongs to a
  platform/composing host; the existing repository AppHost is transitional and must not
  accumulate shared AppHost, Aspire, ServiceDefaults, or generic hosting capability.

### Cross-Cutting Concerns Identified

1. **Truth-state / honesty model (CP-1..CP-4)** — one shared client model behind every
   surface; never collapse `accepted`/`confirmed`/`audit available`; never show
   unconfirmed success.
2. **Authorization reflection (CP-9 / NFR-2)** — every actionable element reflects server
   auth, fail-closed; the UI is never the gate.
3. **Freshness & eventual consistency (NFR-3)** — ETag/304 + projection-as-truth + Blazor
   InteractiveServer circuit-reconnect re-derivation.
4. **Support-safety / privacy (§10)** — no tokens, payloads, correlation-ids, raw metadata,
   or PII in any surface/log/receipt/copy; safe localized rejection text only.
5. **Accessibility (WCAG 2.1 AA; conditional 2.2)** — no-color-only, live-region politeness
   (assertive reserved for failures), complete-or-exit every workflow; non-removable even
   under fallback.
6. **Localization** — whole-string resources with named placeholders (no runtime fragment
   assembly), culture-aware; **resource ownership (shell vs. Tenants) routed to this
   architecture** (Open Q#4).
7. **Canonical state vocabularies (CP-10)** — verbatim, casing-significant; a single shared
   enumeration source across components.
8. **Command lifecycle & idempotency** — `messageId` idempotency key; async confirm;
   duplicate-submit dedup; one-at-a-time policy (FC-CNC fallback).
9. **FrontComposer dependency & fallback governance** — the build-readiness gate this
   architecture must convert into an actionable resolution + sequencing plan.
10. **Testability/automation (NFR-4)** — stable selectors/component contracts pervade all
    components.

## Starter Template Evaluation

### Primary Technology Domain

.NET 10 Blazor web application (interactive, server-rendered) — a domain UI composed on the
**Hexalith.FrontComposer** shell with **Microsoft Fluent UI Blazor v5**, consuming the existing
Tenants/EventStore REST + DAPR/SignalR backend. The implemented host is
`src/Hexalith.Tenants.UI`; the starter evaluation below is retained as decision history.

### Starter Options Considered

This is not a "pick a JS starter" decision; the ecosystem dictates the stack. Three
foundations were evaluated, grounded in the initialized submodules:

1. **New Blazor host composing the FrontComposer Shell** *(recommended)* — satisfies the PRD/UX
   "Operations Shell = FrontComposer shell" mandate and the repo domain-boundary policy (shared
   UI lives in FrontComposer, not Tenants). The Shell provides shell layout (FC-LYT), navigation
   from registered domain manifests, the projection DataGrid (FC-TBL), command dispatch, and
   theming. Typed Tenants state remains domain-owned under AD-4 and AD-7. FC-LYT readiness is closed.
2. **Standalone Fluent UI Blazor app, no FrontComposer Shell** (the `EventStore.Admin.UI`
   pattern) — technically viable, but contradicts the Operations-Shell requirement and the
   boundary policy; retained only as the **constrained fallback** if FC-LYT never resolves.
3. **Generic `dotnet new blazor`, no FrontComposer** — rejected; fails the composition mandate
   and would re-implement shared shell scaffolding inside Tenants.

Verified ecosystem facts:
- `Hexalith.FrontComposer.Cli` (`frontcomposer` dotnet tool) is an **inspect/migrate** tool,
  **not a project scaffolder** — the host is created manually from the reference pattern.
- Reference hosts `Hexalith.EventStore.Admin.UI` and `Hexalith.EventStore.Sample.BlazorUI` use
  **Blazor Server / `InteractiveServer`** (no separate WASM `.Client` project).
- FrontComposer.Shell + both reference UIs pin **Fluent UI Blazor `5.0.0-rc.5-26219.1`** (still
  RC; no GA as of 2026-06).

### Selected Starter: `src/Hexalith.Tenants.UI` Blazor host composing the FrontComposer Shell

**Rationale for Selection:**
It is the only foundation that satisfies the "Operations Shell within a FrontComposer shell"
requirement and the domain-boundary policy. Epic 1 reuses FrontComposer shell/layout contracts and
uses Tenants-specific read components where FC-TBL does not meet cursor/safety-state needs, while
inheriting Fluent v5 + theming + manifest-driven navigation instead of rebuilding them in Tenants.
The implementation mirrors the proven EventStore reference UIs for host bootstrap, auth, and
backend access. (Option 2 remains a historical fallback path.)

**Historical initialization command** *(the host is now implemented):*

```bash
# from repo root: create the Blazor Web App host, then wire FrontComposer + Fluent
dotnet new blazor -n Hexalith.Tenants.UI -o src/Hexalith.Tenants.UI \
  --interactivity Server -f net10.0
# then: add to Hexalith.Tenants.slnx; reference Tenants.Client (+ a ServiceDefaults);
# add FrontComposer.Shell + Fluent UI Blazor packages (versions via Directory.Packages.props);
# compose the shell in MainLayout and register the Tenants domain manifest.
```

> The implemented host uses Blazor InteractiveServer (`AddInteractiveServerComponents` and
> `AddInteractiveServerRenderMode`). No package versions go in `.csproj` files; versions stay in
> central `Directory.Packages.props`.

**Architectural Decisions Provided by Starter:**

**Language & Runtime:** C# / .NET 10 (`net10.0`, SDK `10.0.400` pinned), `Microsoft.NET.Sdk.Web`;
Nullable + ImplicitUsings + `TreatWarningsAsErrors` + `ConfigureAwait(false)` per repo props.

**UI / Styling:** Microsoft Fluent UI Blazor v5 (`5.0.0-rc.5-26219.1`, RC — no GA yet), inherited
through the FrontComposer shell; semantic theme roles, no bespoke palette; Fluent type ramp /
shapes / elevation. Tenants tracks FrontComposer's transitive Fluent pin; tokens/ARIA verified
against the pinned package at build. UI uses FrontComposer or Fluent v5 components, never raw
`<button>/<input>/<select>/<textarea>`, and expresses page/section layout and spacing through
Fluent layout primitives (`FluentStack`/`FluentGrid`) and Fluent design tokens rather than
component-local layout/typography CSS. Raw semantic landmarks (`<header>/<section>/<nav>`),
description/bullet lists, and `<a>` nav links remain the documented fallback where Fluent v5 has
no equivalent (governance allowlist in `DomainUiFluentConformanceTests`). See
`sprint-change-proposal-2026-06-18-fluent-only-components-and-styles.md`.

**Shell / Composition:** Hexalith.FrontComposer.Shell — shell layout (FC-LYT), navigation from
registered domain manifests, projection DataGrid (FC-TBL), command dispatch, theming. Consumed
via the Shell's registration extensions + a Tenants domain manifest + projection routing (exact
`AddHexalithFrontComposer*` / registry / route API names to be confirmed against the Shell source
in the integration spec).

**State Management:** typed immutable Tenants snapshots and lifecycle models under `State/`;
FrontComposer owns shell state, while Tenants owns domain truth state under AD-4 and AD-7.

**Backend Access:** REST to the existing query API + `POST /api/v1/commands` (+ status poll),
over DAPR service invocation (EventStore pattern) or HttpClient + Aspire service discovery
(decided in step-4); SignalR client for freshness nudges only.

**Testing:** bunit (component) + Playwright (E2E) + xUnit v3 + Shouldly; NFR-4 stable automation
selectors are first-class.

**Hosting:** project added to `Hexalith.Tenants.slnx`; the current repository AppHost remains
transitional local wiring while a platform/composing host becomes the owner of tenants,
eventstore, memories, identity, and UI references. SDK container support
(`EnableContainer`, `ContainerRepository=tenants-ui`), no Dockerfile.

**Foundation decisions (resolved in step-4 — Decisions):**
- **Render mode** — resolved to **InteractiveServer**. Earlier UX material assumed Blazor Auto
  (prerender→Server→WASM+reconnect), but the ecosystem reference UIs use InteractiveServer and
  Epic 1 implemented `AddInteractiveServerComponents` / `AddInteractiveServerRenderMode`.
- **Use the Shell vs. fallback custom layout** — tied to FC-LYT readiness.
- **Backend transport** — DAPR service invocation vs. HttpClient + Aspire service discovery.

**Note:** Project initialization using this recipe should be the **first implementation story**
(the "Epic 1 / Story 1 bootstrap": shell composition, routing, auth, projection/SignalR client),
per the implementation-readiness report's recommendation.

## Core Architectural Decisions

### Decision Priority Analysis

**Critical Decisions (made — block implementation):**
- **D1 Runtime model:** Blazor **InteractiveServer** + a **server-side BFF** in the UI host.
- **D2 Command confirmation:** on dispatch, run **status-poll and SignalR concurrently**;
  the first terminal/projection-change signal triggers the **authoritative projection
  re-query**; lifecycle flips to `confirmed` only on the re-queried projection.
- **D3 FrontComposer posture:** **hybrid** — FC-LYT/FC-CMD/FC-CNC are confirmed contracts;
  FC-AUD/FC-CNS are delivered via the **approved fallbacks**
  (flat audit DataGrid, inline consequence text).
- **D4 Localization ownership:** **Tenants-owned** whole-string `.resx` keys; inherit only
  shell-chrome strings from `FcShellResources`.

**Important Decisions (made — cascade from D1–D4):**
- **D5 Truth-state model:** shared typed immutable truth/lifecycle snapshots plus a
  casing-faithful **canonical-vocabulary library**; no specific state framework is required.
- **D6 Freshness:** **server-side** conditional reads (`If-None-Match`→`304`); freshness is
  classified **server-side** from a **persisted projection timestamp** (`IReadModelFreshness.ProjectedAt`,
  EventStore Client) against **configurable thresholds**; the wire (`QueryResponseMetadata`) carries
  **`current`/`stale`/`unknown`** only (`refreshing` is a client transient; `aging` collapses into
  `current` until a `QueryResponseMetadata.ProjectedAt` wire field exists — future EventStore handoff);
  `unknown` when unmeasurable (fail-closed). Thresholds default **conservative** because `ProjectedAt`
  measures the last projection write (quiescence ≠ lag).
- **D7 Authorization reflection:** **server-side** claims→action-availability service.
- **D8 Support-safety:** **server-side** receipt/preview/redaction assembly.
- **D9 Cursors:** opaque, **server-held** pass-through; page-1 re-query on invalidation.
- **D10 UI host placement:** **`src/Hexalith.Tenants.UI`** in the Tenants repo, governed by AD-13.

**Deferred Decisions (post-MVP, with rationale):**
- NFR performance budgets (set against the real projection at implementation).
- Freshness threshold tuning (product/ops input; code defaults stay conservative and config-owned).
- RTL shipping (Open Q#6), WCAG 2.2 confirmation (against the pinned Fluent build),
  sensitive-config display (Open Q#11) — none blocks the MVP.

### Data Architecture

No database decision — the UI **owns no datastore** and never writes one (NFR-5); it consumes
existing projections only.
- **Read access:** a typed query gateway in the BFF wrapping the 6 REST endpoints, using the
  `Hexalith.Tenants.Client`/`.Contracts` DTOs (`PaginatedResult<T>`, `TenantSummary`,
  `TenantDetail`, `TenantMember`, `UserTenantMembership`, `TenantAuditEntry`).
  - **Transport (regression guard, added 2026-06-06):** the BFF calls these `GET /api/tenants*`
    endpoints on the Tenants domain service directly (DAPR service invocation, server-side, bearer
    relayed). It MUST NOT route tenant reads through the EventStore generic query gateway
    (`POST /api/v1/queries` → `QueryRouter` / `HandlerAwareQueryRouter`): the projection actor is
    retired, and the handler-aware path drops projection ETags — breaking the D6 freshness contract
    below. See `sprint-change-proposal-2026-06-06-tenant-query-routing.md`.
- **Freshness/caching (D6):** conditional requests executed server-side; freshness is classified
  server-side from the persisted projection timestamp via the shared `ReadModelFreshness.Classify`
  (EventStore Client) and surfaced as `QueryResponseMetadata.IsStale`. The Truth State Badge renders
  the shared `ReadModelFreshnessState` (`current/aging/stale/unknown`) plus a client-only `refreshing`
  transient; on the wire only `current/stale/unknown` are producible today (`aging` collapses to
  `current`). Thresholds are configuration, **no magic numbers**, defaulted conservatively;
  unmeasurable → `unknown` → fail-closed.
- **Cursors (D9):** opaque, signed, scope-bound; held server-side, never surfaced as user-facing
  ids; on invalidation re-query page 1 with an honest "list refreshed" notice; multi-replica
  durability treated as **not-yet-guaranteed** (backend Epic 11).
- **Client read-model:** typed server-side UI snapshots are the runtime cache; the re-queried
  projection is authoritative; **last-confirmed projection is retained separately from
  in-flight intent** (non-collapse, CP-3).

### Authentication & Security

- **JWT bearer** at the UI host (Keycloak/OIDC `Authority` in prod; symmetric-key dev). Under
  InteractiveServer the **access token stays server-side** — the browser never receives it.
- **Authorization reflection (D7):** a server-side service maps the actor's claims
  (`sub`, `eventstore:tenant=system`, `global_admin`/`role` shapes) + projection facts → per-action
  availability and the 6-category **Unavailable Action Reason**. The UI **reflects only**; the
  server remains the enforcing gate (NFR-2/CP-9). Indeterminate → fail-closed.
- **Support-safety (D8):** NarrativePayload→receipt assembly, consequence-preview assembly, and
  rejection→text mapping all run in the BFF; only safe, localized, **redacted** projections reach
  the browser. Never client-side: tokens, payloads, correlation-ids, raw metadata, PII, stack
  traces. Domain rejections (RFC 7807 at the boundary) map to safe localized strings via a
  Tenants-owned catalog keyed by each rejection's safe reason code.

### API & Communication Patterns

- **Backend transport:** server-to-server from the BFF to direct Tenants REST query endpoints
  and the EventStore command endpoint, with AppHost/DAPR service discovery. **No new backend
  endpoints.** Query reads must not use the generic EventStore query gateway (AD-6).
- **Command dispatch:** `POST /api/v1/commands` with a client-generated **`messageId` (ULID)**
  idempotency key; envelope `tenant=system`, `domain ∈ {tenants, global-administrators}`,
  `aggregateId`; returns `202` + `correlationId`.
- **Confirmation (D2):** parallel `GET /api/v1/commands/status/{correlationId}` poll **+** SignalR
  nudge → authoritative projection re-query; `confirmed` only from the re-query; SignalR never
  advances lifecycle/audit (CP-4); duplicate submit/refresh dedups by `correlationId`. NoOp →
  `already applied`; rejection → safe text; unverifiable → `unable to verify` (never success).
- **Concurrency policy:** the confirmed FC-CNC posture is one active command per
  `(interactive circuit, AggregateIdentity)` until terminal evidence; no bulk or toast-batching.
  A stronger shared concurrency contract may replace it later. `409 ConcurrencyConflict` (+`Retry-After`) →
  `retry status lookup`.
- **Tenant search (Memories-backed, index-only; cc-2026-06-21):** cross-set list search is served by
  `Hexalith.Memories`, **not** a new EventStore endpoint (the read backend stays consume-only — no
  server-side filter on `ListTenantsQuery`). Data path: `MemoriesClient.SearchAsync(tenants-index,
  syntactic)` returns a **match-set of tenant ids** (parsed from `ScoredResult.SourceUri` = `tenant:{id}`);
  the BFF then **hydrates each row through the existing ETag-fresh detail read** (D6). Memories decides
  *which* tenants appear; the read path decides *what each row shows*, so a stale index never renders wrong
  data. Index maintenance is a separate async flow: on tenant lifecycle events the Tenants consumer
  publishes **one curated `SearchIndexEntryChanged` per tenant** (upsert by `(tenants-index, tenantId)`,
  searchable text = name + id, `status` attribute) to the `memories-events` topic — the cross-domain
  **index-maintenance pattern** for feeding Memories without a raw-event ingestion adapter. This dissolves
  the FC-`IQueryService`-vs-REST/ETag tension: search bypasses the query-service abstraction entirely and
  reuses the ETag/freshness read path. Memories unavailable → non-blocking fallback to the cursor list.
  Full end-to-end search is gated on the Memories server handoff
  (`memories-search-index-handoff-2026-06-21.md`): upsert ingestion, attribute indexing + REST attribute
  filter, `tenants-index` registration.

### Frontend Architecture

- **Render mode:** Blazor **InteractiveServer**; components kept render-mode-agnostic where
  practical to preserve a future Auto option. Root composes `FluentProviders` + the FrontComposer
  shell. *(Reconcile the UX `EXPERIENCE.md` "Auto" assumption to InteractiveServer.)*
- **Shell composition:** compose `Hexalith.FrontComposer.Shell` and register exactly one
  `/tenants` shell entry. The workspace owns Tenants and Users tabs/scope modes; tenant detail,
  Global Administrators, and Audit remain contextual or policy-gated routes (AD-1 and AD-2).
  Story 1.2 resolved the FC-TBL caveat with Tenants-specific grid/table components while generic
  reusable grid capability remains FrontComposer-owned.
- **Truth-state model (D5):** shared typed immutable state is the one source for the 5 truth
  dimensions and the canonical vocabularies (13 badge / 10 lifecycle / 10 feedback / 6
  reasons / 5 freshness / 4 audit), exposed as a typed, **casing-faithful** library used verbatim
  by every component (CP-10); **non-collapse enforced in the model** (`accepted`≠`confirmed`≠
  `audit available`; `degraded`/`unable to verify` success-prohibited). The 10 DESIGN.md
  components bind to this model.
- **Routing:** shell-managed routes + deep-linkable tenant detail; selection/filters/scroll
  preserved across navigation.
- **Localization (D4):** Tenants-owned whole-string `.resx` (named placeholders, no fragment
  assembly), culture-aware via `IStringLocalizer`; inherits only `FcShellResources` chrome.

### Infrastructure & Deployment

- **UI host (D10/AD-13):** `src/Hexalith.Tenants.UI` (`Microsoft.NET.Sdk.Web`, `net10.0`) in the
  Tenants repo and added to `Hexalith.Tenants.slnx`. It consumes platform ServiceDefaults and is
  wired by a platform/composing host. The existing repository AppHost is transitional migration
  debt, not an architecture pattern to expand.
- **Auth wiring:** AppHost wires the Keycloak realm + `Authentication:JwtBearer:*`;
  `EnableKeycloak=false` → symmetric-key JWT locally.
- **Containers:** SDK container support, `EnableContainer=true`,
  `ContainerRepository=tenants-ui` → `registry.hexalith.com/tenants-ui`; no Dockerfile.
- **CI/CD:** extend the existing pipeline (build Release `-warnaserror`); add bUnit unit + Playwright
  E2E tiers (E2E likely non-blocking like the Aspire tier). The UI host ships as a **container
  image, not a NuGet package** (unlike the 5 libraries). OpenTelemetry via ServiceDefaults; NFR-4
  stable automation selectors as component contracts.

### Decision Impact Analysis

**Implementation Sequence (historical build order):**
1. Bootstrap the UI host, shell composition, auth, BFF gateways, typed truth-state foundation,
   and canonical vocabulary.
2. Add read surfaces for FR-1..FR-9 and FR-18.
3. Add initial command flows for FR-10/11/13/14.
4. Add high-impact, audit, and recovery flows for FR-12/15-17/19/20-25.

The current architecture remediation priority is to bring `TenantQueryGateway` into AD-6
conformance by routing reads directly to the Tenants REST query endpoints.

**Cross-Component Dependencies:**
The **typed truth-state model**, **canonical-vocabulary library**, **BFF query/command gateway**,
**authorization-reflection service**, and **support-safety/redaction layer** are shared foundations
every surface depends on → built first. FrontComposer contract confirmations (FC-LYT/FC-CMD/FC-CNC)
are closed by Story 1.0 (2026-06-05); the FC-AUD/FC-CNS/FC-CNC fallback **approvals are secured**
(2026-06-03 - see `fallback-approval-record-2026-06-03.md`). The `FC-TBL` caveat is resolved for
Tenants by Story 1.2 with Tenants-specific `TenantDataGrid` composition; reusable grid enhancement
remains FrontComposer-owned future work.

**Action items this architecture surfaces:**
- ✅ Product/UX approval for the **FC-AUD flat-audit**, **FC-CNS inline-consequence**, and
  **FC-CNC one-at-a-time** fallbacks — **secured 2026-06-03** (see `fallback-approval-record-2026-06-03.md`);
  the hybrid posture's fallback premise is confirmed.
- ✅ Confirm **FC-LYT / FC-CMD / FC-CNC** contracts with the FrontComposer team - **closed by Story 1.0**
  (2026-06-05; see `_bmad-output/implementation-artifacts/story-1-0-spike-note-2026-06-05.md`).
- ✅ Resolve the **FC-TBL grid decision** before Story 1.2 tenant-list implementation — closed by
  Story 1.2 with Tenants-specific grid/table composition.
- ✅ Correct the **ULID-vs-string** spec discrepancy; reconcile the UX **"Auto"** assumption to
  InteractiveServer; resolve the **Users-nav IA** to "contextual." Epic 1 implementation follows
  all three.

## Implementation Patterns & Consistency Rules

### Pattern Categories Defined

**Critical conflict points:** ~10 areas where agents building surfaces could diverge — most
dangerously the **canonical state vocabularies**, **localization keys**, **automation selectors**,
the **six list-surface states**, and the **command-confirmation flow**. C# language/style is **not
re-stated** here — it is governed by `project-context.md` (file-scoped namespaces, Allman,
`_camelCase` fields, `I`-prefix, `Async` suffix, namespace=folder, no copyright headers,
`ConfigureAwait(false)`, central package versions, `.slnx` only) and inherited verbatim.

### Naming Patterns

**Backend contract (consume verbatim — never redefine):** routes, DTO shapes, enum values
(`TenantRole`/`TenantStatus`/`AuditEventCategory`, by name), and rejection types are backend-owned.
Agents bind to `Hexalith.Tenants.Client`/`.Contracts` types — never re-declare a DTO, re-case a
wire field (PascalCase on the wire), or `Guid`/`Ulid.TryParse` a `TenantId`/`UserId`.

**Razor components:** `PascalCase.razor`, one per file. Implemented shared/domain primitives include
`TruthStateBadge`, `AuditEvidenceReceipt`, `TenantDataGrid`, and `AuditDataGrid`; command flows are
named by domain intent. Folders are organized by actual surface: route components under
`Components/Pages/`, tenant surfaces under `Components/Tenants/`, user lookup/self-audit under
`Components/Users/`, and domain reusable views under `Components/Shared/`.

**State models:** `{Area}Snapshot` or `{Area}State` immutable records with explicit transition
methods; request and result models remain surface-specific while shared truth, freshness,
lifecycle, and audit vocabularies remain canonical across surfaces.

**Localization keys (D4 — Tenants-owned):** dotted `PascalCase` under a `Tenants.` root mirroring
the concept — `Tenants.Freshness.Stale`, `Tenants.UnavailableReason.MissingPermission`,
`Tenants.Rejection.UserAlreadyInTenant`, `Tenants.Journey.RemoveUser.Confirm`. Whole strings with
named placeholders (`{userName}`, `{tenantName}`) — **never** concatenate localized fragments.

**Automation selectors (NFR-4):** every interactive element/status carries
`data-testid="tenants-{surface}-{element}"` (kebab) — `tenants-tenant-list-row`,
`tenants-remove-user-confirm`, `tenants-truth-badge`. **Never** key a test on row text or color.

### Structure Patterns

```
src/Hexalith.Tenants.UI/
├── Components/            # Razor — by surface
│   ├── Pages/             # workspace, detail, audit, global-admin, and compatibility routes
│   ├── Tenants/           # tenant, member, configuration, lifecycle, and audit flows
│   ├── Users/             # my-tenants and user-membership lookup panels
│   ├── Shared/            # domain reusable views and truth-state primitives
│   └── Layout/            # FrontComposer shell composition
├── State/                # typed immutable snapshots and lifecycle/truth models by surface
├── Services/             # BFF gateways (query/command), authorization-reflection,
│                         #   support-safety/redaction, freshness, SignalR client
├── Resources/            # Tenants-owned .resx (D4)
└── Program.cs _Imports.razor wwwroot/css/
tests/Hexalith.Tenants.UI.Tests/   # bUnit + xUnit v3; {Class}Tests.cs (plural)
```

Tests live in a **separate `*.UI.Tests` project** (repo convention, never co-located); Playwright
E2E in its own tier.

### Format Patterns

- **Canonical state tokens (CP-10):** consumed from typed enums/records under `State/` and localized
  through `Resources/`, never hand-typed at call sites.
  Casing is significant — badge `audit pending` vs state-machine `audit_pending` stay distinct; **no
  agent unifies them**. The library is the single source.
- **Timestamps:** absolute, culture-formatted, monospace; **never relative-only**.
- **Identifiers:** literal caller-supplied strings, monospace; copy-full-id copies the literal;
  never parsed as ULID/Guid.
- **Truth-state shape:** every status = `{ token + freshness + absolute-timestamp + accessible-name }`;
  color never the sole carrier (icon + text always present).

### Communication Patterns

- **State discipline:** immutable snapshots and pure transition methods; I/O stays in server-side
  BFF gateways/composition services. The **non-collapse invariant is enforced by the state model** —
  `accepted`, `confirmed`, and `audit available` are distinct fields and never overwrite the last
  confirmed projection with in-flight intent.
- **Command confirmation (D2) — the ONE pattern:** submit → status-poll and SignalR nudge →
  **authoritative projection re-query** → state transition. `confirmed` requires expected
  postcondition plus new projection version or safe command-specific audit evidence beyond the
  baseline; NoOp/pre-existing state is `already applied`, and missing provenance is `unable to verify`.
  A SignalR nudge requests re-query only and never advances lifecycle state directly (CP-4).
- **Idempotency:** one client `messageId` (ULID) per attempt; resubmit/refresh reuses it; dedup by
  `correlationId`.

### Process Patterns

- **Six list-surface states (every grid, non-collapsible):** `loading`, `empty`, `filtered-empty`,
  `error`, `stale`, `degraded` — a shared component; `filtered-empty` offers reset, `stale` a refresh
  path, `degraded` explains what still works; **empty is authorization-safe**. Agents use the shared
  component; they never collapse or re-invent these.
- **Fail-closed gating ORDER (load-bearing):** validation **+** freshness **+** authorization all
  `eligible` **before** a consequence preview opens — not only at submit; missing any → blocked with
  the inline `UnavailableActionReason`.
- **Error/rejection handling:** domain rejections → safe localized text via the Tenants rejection
  catalog (keyed by safe reason code); `409 ConcurrencyConflict` → `retry status lookup`; **never**
  render raw problem-details/payloads/stack traces. Every failure → a **named recovery verb** (never
  a dead end); prohibited words `undo`/`rollback`/`hidden edit` never appear.
- **Live-region politeness:** bound to a **dedicated announcement-intent field**, never derived from
  `BadgeColor`/`MessageBarIntent`; `assertive` reserved for rejection/failure/`unable to verify`/
  `degraded`/destructive-block; else `polite`; **never announce success before projection confirm**.
- **Focus:** every modal/preview traps focus; `Esc`/cancel is a **safe non-committing** escape; focus
  **returns to the launching control** on close/cancel/submit/failure.

### Enforcement Guidelines

**All AI agents MUST:**
- Use the typed canonical state enums/records verbatim (casing-significant) — never hand-type a
  token or unify badge vs state-machine forms.
- Route every backend call through a **BFF gateway**; never call the API from the browser or place
  tokens/payloads client-side.
- Confirm commands **only** via the D2 parallel-poll+SignalR→re-query path; never optimistic success.
- Localize via Tenants `.resx` whole-string keys; never assemble fragments.
- Tag interactive elements `data-testid="tenants-{surface}-{element}"`.
- Render the six list states, the fail-closed gating order, and the recovery-verb mapping as the
  shared patterns.

**Pattern Enforcement:** bUnit asserts non-collapse + the six states + no-color-only; Playwright
asserts the required acceptance scenarios (stale projection, rejected command, unknown
confirmation, audit unavailable, last-owner warning, permission-missing, command accepted but still
projection-pending, focus escape/cancel no-commit, and data unavailable but not authorization-denied)
keyed on `data-testid`; a guard test fails any surface that references a raw state literal instead of
the Vocabulary library. Command-flow tests must prove sibling command surfaces stay unavailable
through `accepted` and `projection_pending` until projection truth or a terminal non-pending state.
Pattern changes are recorded here + in `project-context.md`.

### Pattern Examples

**Good:** `badge.Token = TruthState.Freshness.Stale;` + `Localizer["Tenants.Freshness.Stale"]` +
`data-testid="tenants-tenant-list-stale"`; command shows `confirmed` only after the re-query action.

**Anti-patterns:** typing `"audit_pending"` inline; merging `accepted` and `confirmed` into one
"success"; `string.Format` over localized fragments; `Ulid.TryParse(tenantId)`; a grid showing
`empty` for an error; announcing "Saved!" on `accepted`; a browser-side `HttpClient` to
`/api/tenants` carrying the bearer token.

## Project Structure & Boundaries

### Complete Project Directory Structure

```text
tenants/
├── Hexalith.Tenants.slnx
├── src/
│   ├── Hexalith.Tenants.AppHost/             # transitional local wiring; migrate to composing host
│   ├── Hexalith.Tenants.Client/              # domain client contracts
│   ├── Hexalith.Tenants.Contracts/           # DTOs, enums, commands, and events
│   ├── Hexalith.Tenants.Server/              # domain-service and query handlers
│   └── Hexalith.Tenants.UI/                  # InteractiveServer domain presentation host
│       ├── Composition/                      # one FrontComposer domain registration and /tenants entry
│       ├── Components/
│       │   ├── Pages/                        # workspace and contextual routes
│       │   ├── Tenants/                      # tenant-domain surfaces and command flows
│       │   ├── Users/                        # my-tenants and user lookup panels
│       │   ├── Shared/                       # tenant-domain reusable view components
│       │   └── Layout/                       # FrontComposer shell composition only
│       ├── Services/
│       │   ├── Gateways/                     # only backend egress from the UI host
│       │   └── SupportSafety/                # rendered/copy safety classification
│       ├── State/                            # typed snapshots and lifecycle/truth models
│       ├── Resources/                        # Tenants-owned EN/FR domain copy
│       └── Program.cs                        # InteractiveServer, FrontComposer, Fluent, auth, BFF wiring
└── tests/
    └── Hexalith.Tenants.UI.Tests/            # bUnit, conformance, gateway, and state coverage
```

### Architectural Boundaries

**API boundary (the trust edge):** the `Services/Gateways/` are the **only** egress to the backend
— direct Tenants `GET /api/tenants*` reads plus EventStore `POST /api/v1/commands` and
`GET /api/v1/commands/status/{id}`; projection notification is a nudge only. The
**browser never calls the backend** and never holds a token (InteractiveServer, D1). No new backend
endpoints (NFR-5).

**Component boundary:** components render typed snapshots and may call the injected BFF
composition/gateway contracts; they never create backend clients or call Tenants, EventStore, or
Memories directly. Shared components remain pure views over typed state.

**Service boundary:** gateways, authorization-reflection, freshness, support-safety/redaction, and
rejection-text all run **server-side** in the circuit; this is where redaction happens, so nothing
unsafe (tokens, payloads, correlation-ids, PII, stack traces) can cross into the rendered DOM (D8/§10).

**Data boundary:** the UI owns **no datastore**; the re-queried projection is the source of truth;
typed UI snapshots are an ephemeral cache; **last-confirmed projection is held separately from
in-flight intent** (non-collapse). Cursors are opaque and server-held (D9).

**FrontComposer boundary:** Tenants composes the Shell with one `/tenants` entry and page-local
workspace navigation. It consumes FrontComposer layout, registration, and reusable primitives and
**never re-implements** a missing FC capability — those are contracts (FC-LYT/FC-CMD/FC-CNC) or
approved fallbacks (FC-AUD/FC-CNS), per the domain-boundary policy.

### Requirements to Structure Mapping

| Feature group (FRs) | Lives in | Phase |
|---|---|---|
| 7.1 Discovery & Triage (FR-1..4) | `Components/Pages/TenantsWorkspace`, `Components/Users/*` | 2a |
| 7.2 Detail & Config view (FR-5..7) | `Components/Pages/TenantDetailPage`, `Components/Tenants/TenantConfigurationView` | 2a |
| 7.3 Member & Access review (FR-8..9) | `Components/Tenants/Members/MemberAccessReview` | 2a |
| 7.4 Member & Role mgmt (FR-10..12) | `Components/Tenants/Members/{AddUser,ChangeRole,RemoveUser}Flow` | 2b/2c |
| 7.5 Lifecycle (FR-13..15) | `Components/Tenants/{CreateTenant,EditTenantMetadata,DisableEnableTenant}Flow` | 2b/2c |
| 7.6 Configuration mgmt (FR-16..17) | `Components/Tenants/TenantConfigurationView` (edit) | 2c |
| 7.7 Global-admin governance (FR-18..19) | `Components/Pages/GlobalAdministratorsPage`, `State/GlobalAdministrators/*` | 2a/2c |
| 7.8 Audit trail & evidence (FR-20..23) | `Components/Pages/TenantAuditPage` and `Components/Tenants/Audit/{AuditDataGrid,AuditEvidenceReceipt,AuditAvailabilityState}` | 2c |
| 7.9 Compensating recovery (FR-24..25) | `Components/Tenants/Audit/CorrectionStartPanel` plus `State/TenantAudit/TenantCorrection*` models | 2c |

**Cross-cutting concerns → location:** truth/lifecycle/audit models → `State/`; query and command
egress → `Services/Gateways/`; support safety → `Services/SupportSafety`; localization →
`Resources/`; live-region/focus accessibility → `Components/Shared` and the focused flow components.

### Integration Points

**Internal communication:** component/panel → server-side BFF gateway or composition service →
immutable snapshot transition → component re-render. SignalR remains a re-query nudge only.

**External integrations:** direct Tenants REST query API, EventStore command API and SignalR hub,
Memories search index, and Keycloak/OIDC — all reached server-side through AppHost-wired references.

**Data flow (command):** UI intent → `CommandGateway` (`POST /commands`, `messageId`) → 202 →
parallel status-poll **+** SignalR → authoritative projection re-query → `confirmed` → audit re-query
→ `audit available`. No optimistic path.

### File Organization Patterns

- **Configuration:** `appsettings*.json` (UI host); AppHost supplies `Authentication:JwtBearer:*` +
  service references; no secrets in the repo.
- **Source:** by surface under `Components/`; shared view in `Components/Shared/`; logic split across
  `State/` (typed snapshots/lifecycle) and `Services/` (server-side BFF and safety concerns).
- **Test:** separate `*.UI.Tests` (bUnit, Tier 1) mirroring src + `*.UI.E2E` (Playwright, Tier 3);
  `{Class}Tests.cs` plural; never co-located.
- **Assets:** `wwwroot/css/app.css`; Fluent bundle via the package's static web assets; no bespoke palette.

### Development Workflow Integration

- **Dev server:** currently launched by the transitional `Hexalith.Tenants.AppHost` (`aspire run`)
  alongside tenants/eventstore/keycloak; the target is an external platform/composing host.
  Placement+scheduler start first in slim mode; `EnableKeycloak=false` enables local symmetric-key JWT.
- **Build:** `.slnx` restore/build (`-warnaserror`); per-project `dotnet test`; coverage gates as configured.
- **Deployment:** SDK container (`ContainerRepository=tenants-ui` → `registry.hexalith.com/tenants-ui`),
  no Dockerfile; ships as an **image, not a NuGet package** (unlike the 5 libraries).

## Architecture Validation Results

### Coherence Validation ✅

**Decision Compatibility:** D1–D10 are mutually reinforcing — InteractiveServer (D1) makes the
server-side BFF the natural home for D2 confirm, D6 freshness, D7 authorization-reflection, D8
redaction, and D9 cursors, so tokens/payloads never reach the browser and reconnect re-derives from
server state. No contradictory decisions. One **recorded divergence** (not a contradiction): D1
InteractiveServer vs. the UX `EXPERIENCE.md` "Auto" assumption — logged as a reconciliation action
item (the UX named an assumption, not a hard requirement; NFR-3 holds either way, more simply under
InteractiveServer). Versions consistent: .NET 10 (10.0.400) + Fluent v5 RC pin inherited from
FrontComposer.

**Pattern Consistency:** patterns enforce the decisions — Vocabulary-verbatim (CP-10), BFF-only
egress (D1/D8), the single D2 confirm path, the six list states, l10n keys (D4), stable selectors
(NFR-4). No pattern contradicts a decision.

**Structure Alignment:** the tree realizes the decisions — `Services/` (BFF, D6–D9), `State/`
(typed truth and lifecycle models, D5), and `Resources/` (D4) — and the boundaries make the trust
edge explicit.

### Requirements Coverage Validation ✅

**Feature Coverage:** all 9 PRD feature groups mapped to concrete surfaces; all six journeys
(UJ-1..6) land on them.

**Functional Requirements:** all **25 FRs** have an architectural home, including the previously
story-less FR-22/24/25 (now structurally homed in `Components/Tenants/Audit/` and covered by Epic 5 stories).

**Non-Functional Requirements:** NFR-1 (cursor + 304 + freshness; numeric budgets deferred), NFR-2
(server-enforced + reflection, tokens server-side), NFR-3 (D2 confirm + InteractiveServer + non-
collapse), NFR-4 (selectors + test tiers), NFR-5 (no datastore; compensating commands). CP-1..CP-10
encoded in the truth-state model + Vocabulary + process patterns.

### Implementation Readiness Validation ⚠️ design complete / implementation remediation required

**Decision Completeness:** D1–D10 documented with the version posture. **Structure Completeness:**
complete tree + boundaries + FR mapping. **Pattern Completeness:** naming/structure/format/
communication/process + examples + anti-patterns; all conflict points addressed.

> The architecture is complete as a decision set. FrontComposer dependencies are closed, but
> AD-6/AD-8 query provenance, AD-10 search-cursor scope, AD-13 orchestration ownership, and AD-14
> health/telemetry/scaling controls must be remediated before architecture conformance can be claimed.

### Gap Analysis Results

**Critical implementation conformance issues:**
- **AD-6 / AD-8 query provenance - OPEN 2026-07-15.** `TenantQueryGateway` currently uses
  `IEventStoreGatewayClient.SubmitQueryAsync`, and the configured handler route normalizes
  freshness to `Unknown`. The platform REST surface must first propagate ETag and freshness
  provenance; the composing host must then expose separate Tenants-query and EventStore-command
  references; finally the UI BFF can split clients without losing metadata.
- **AD-13 orchestration ownership - OPEN 2026-07-15.** The repository-owned AppHost is
  transitional and must migrate to a platform/composing host rather than expand.
- **AD-10 search cursor - OPEN 2026-07-15.** The current Memories cursor is a plaintext offset;
  replace it with an opaque cursor bound to authenticated user and normalized search scope.
- **AD-14 production operations - OPEN 2026-07-15.** Add shared health endpoint mapping and
  OpenTelemetry/ServiceDefaults integration. InteractiveServer remains single-replica until shared
  DataProtection, circuit/session routing, and cursor durability are verified.

**Closed external/downstream gates:**
- **FrontComposer readiness - CLOSED 2026-06-05 for FC-LYT/FC-CMD/FC-CNC.** D3 commits to a
  hybrid posture. The FC-AUD/FC-CNS/FC-CNC fallback **approvals are secured** (2026-06-03 - see
  `fallback-approval-record-2026-06-03.md`), and Story 1.0 confirms the shell/layout/command/
  concurrency/accessibility/localization/docs contracts. Story 1.2 resolves the `FC-TBL`
  tenant-list grid decision with Tenants-specific grid/table composition.
- **FrontComposer Shell integration spec - CLOSED 2026-06-05.** Story 1.0 is complete; see
  `_bmad-output/implementation-artifacts/story-1-0-spike-note-2026-06-05.md`.
- **Epics & stories layer - CLOSED.** `epics.md` exists and covers FR1-FR25. The active handoff
  risk is synchronization: `sprint-status.yaml` must match the canonical story IDs before the
  next story is created.

**Important (refine; non-blocking to the architecture):**
- Deferred numerics — NFR performance budgets + freshness thresholds (product/ops input).
- Doc reconciliations (action items) — UX "Auto"→InteractiveServer; Users-nav→contextual; ULID-vs-
  string spec correction.
- Fluent v5 **RC→GA** risk — track FrontComposer's pin; verify tokens at build.

**Nice-to-have:**
- Consider extracting canonical state contracts only if another package genuinely needs to consume them.
- Deepen observability/telemetry + NFR test-design specifics.

### Validation Issues Addressed

No contradiction remains in the decision set after merging AD-1..AD-14. The render-mode source
divergence is resolved in favor of InteractiveServer. The open AD-6/AD-8, AD-10, AD-13, and AD-14
implementation divergences are recorded explicitly rather than weakening the architecture rules.

### Architecture Completeness Checklist

**Requirements Analysis**
- [x] Project context thoroughly analyzed
- [x] Scale and complexity assessed
- [x] Technical constraints identified
- [x] Cross-cutting concerns mapped

**Architectural Decisions**
- [x] Critical decisions documented with versions
- [x] Technology stack fully specified
- [x] Integration patterns defined
- [x] Performance considerations addressed

**Implementation Patterns**
- [x] Naming conventions established
- [x] Structure patterns defined
- [x] Communication patterns specified
- [x] Process patterns documented

**Project Structure**
- [x] Complete directory structure defined
- [x] Component boundaries established
- [x] Integration points mapped
- [x] Requirements to structure mapping complete

### Architecture Readiness Assessment

**Overall Status:** READY WITH REMEDIATION *(architecture design)* — AD-1..AD-14 are canonical,
FrontComposer contract readiness and fallback approvals are closed, and the epics/stories layer
exists. AD-6/AD-8 query provenance, AD-10 search cursor scope, AD-13 orchestration ownership, and
AD-14 health/telemetry/scaling controls remain open in the platform/implementation.

**Confidence Level:** MEDIUM-HIGH — the decision set is coherent and covers the 25 FRs, NFRs, and
CP contract, while the explicitly listed platform and implementation remediations remain open.

**Key Strengths:**
- The honesty/trust thesis is enforced **structurally** (server-side BFF + the D2 confirm path +
  typed canonical state + non-collapse transitions), not left to per-surface discipline.
- Tight alignment with the existing ecosystem (FrontComposer Shell, reference UIs, repo conventions,
  fixed backend contract) — minimal new surface area, maximal reuse.
- Every FR has a home; every cross-cutting concern has a single owner.

**Areas for Future Enhancement:**
- Keep the Story 1.0 gate-clearing evidence current as FrontComposer evolves.
- Keep the resolved Story 1.2 `FC-TBL` tenant-list grid path synchronized with any future FrontComposer reusable grid enhancement.
- Set the deferred numerics; reconcile the flagged doc items; track Fluent RC→GA.

### Implementation Handoff

**AI Agent Guidelines:**
- Follow AD-1..AD-14, D1–D10 where complementary, and the patterns exactly; never optimistic
  success; use canonical typed state; keep BFF-only egress; localize via Tenants `.resx`; tag
  `data-testid`.
- Respect the five boundaries; the projection re-query is the only source of truth.
- Refer to this document for all architectural questions; record any change here + in `project-context.md`.

**Current Implementation Priority:**

1. Extend the platform REST path to preserve ETag and read-model freshness provenance.
2. Move topology ownership to a platform/composing host with separate Tenants-query and
   EventStore-command references.
3. Split the UI BFF query and command clients, then route all six reads directly to Tenants.
4. Replace the plaintext Memories search offset with an opaque, user/query-scoped cursor.
5. Add shared health and OpenTelemetry/ServiceDefaults integration; keep InteractiveServer
   single-replica until the remaining AD-14 operational prerequisites are proven.
