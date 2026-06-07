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
  - Hexalith.Commons/_bmad-output/project-context.md
  - Hexalith.EventStore/_bmad-output/project-context.md
workflowType: 'architecture'
lastStep: 8
status: 'complete'
completedAt: '2026-06-03'
project_name: 'Hexalith.Tenants'
user_name: 'Administrator'
date: '2026-06-02'
---

# Architecture Decision Document

_This document builds collaboratively through step-by-step discovery. Sections are appended as we work through each architectural decision together._

## Project Context Analysis

### Requirements Overview

**Functional Requirements — 25 FRs across 9 feature groups, in 3 build phases.**
The PRD defines FR-1..FR-25 as **UI composition over an already-built event-sourced
backend** (no backend endpoints, no contract reshaping). They collapse into two
construction patterns:
- **Read/projection surfaces** (FR-1..FR-9, FR-18; audit-read FR-20..FR-23) — compose
  5 REST projection queries through the FrontComposer DataGrid (FC-TBL): cursor
  pagination (never offset/limit), ETag→304 freshness, authorization-scoped results.
  This is the entire MVP.
- **Custom command flows** (FR-10..FR-17, FR-19, FR-24..FR-25) — *not generated CRUD*;
  each dispatches `POST /api/v1/commands` and tracks an async
  `accepted → projection-confirmed → audit-available` lifecycle, with client-assembled
  Consequence Previews and Audit Evidence Receipts (no new backend endpoints).
Phasing: **2a/MVP** (read: FR-1..9, FR-18) → **2b** (first commands: FR-10/11/13/14)
→ **2c** (high-impact + audit + recovery: FR-12/15/16/17/19/20–25). Epic 5 now provides
backing stories for FR-20..FR-25, including the flat audit DataGrid, support-safe receipts,
audit availability, tenant-domain correction preview/confirmation, and proof linking. FR-19
global-administrator command work remains separately gated by Epic 4 Stories 4.3 and 4.4.

**Non-Functional Requirements — the honesty contract is the architecture driver.**
- **NFR-3 Reliability/consistency (defining):** eventually-consistent, event-sourced;
  projection is the source of truth; correct under at-least-once delivery + projection
  lag; under Blazor InteractiveServer the UI re-derives truth from server-side BFF reads and
  never resurrects optimistic success.
- **NFR-2 Security/authorization:** server-enforced at API (L1) + domain RBAC (L2); the
  UI **reflects, never enforces**, and must stay safe even if it misjudges; role-scoping
  read from JWT claims.
- **NFR-1 Performance/freshness:** cursor pagination + conditional requests (ETag/304);
  ~1s warm render; ~500-event audit target. Budgets are `[ASSUMPTION]`.
- **NFR-4 Observability/testability:** stable automation selectors/component contracts —
  never keyed on row text or color.
- **NFR-5 No data-store edits:** corrections are forward compensating commands only.
On top sits the **CP-1..CP-10 interaction contract** (five truth dimensions, fail-closed
gating, non-collapse invariant, SignalR-nudge-only, consequence-preview-before-
destruction, asymmetric high-risk, correct-forward-never-undo, canonical-vocabulary-
verbatim) — translating directly into a **shared client-side truth-state model**.

**Scale & Complexity:**
- Primary domain: **Web frontend** — a Blazor InteractiveServer domain UI composed on the
  **Hexalith.FrontComposer** shell, consuming an event-sourced CQRS backend over
  REST + DAPR/SignalR. .NET 10; Fluent UI Blazor v5 pinned `5.0.0-rc.3-26138.1`.
- Complexity level: **HIGH.** Drivers: eventual-consistency correctness as the core
  thesis; a 5-dimension truth-state model with casing-significant canonical vocabularies
  (13 badge / 10 lifecycle / 10 feedback / 6 reasons / 5 freshness / 4 audit) and a strict
  non-collapse invariant; role-scoped multi-tenant authorization reflection; a heavy,
  partly-missing external dependency (FrontComposer) gating even the MVP; first-class
  a11y/l10n/responsive-fail-closed; hard support-safety rules.
- Estimated architectural surface: a **new Blazor UI host** + ~6 client layers (shell
  composition, query/API client, command-lifecycle client, truth-state model,
  authorization-reflection, localization) composing the domain UI components over the
  FrontComposer shell and Tenants-specific read components where required.

### Technical Constraints & Dependencies

- **Consume-only backend (fixed):** 5 read endpoints (`GET /api/tenants`,
  `/api/tenants/{id}`, `/api/tenants/{id}/users`, `/api/users/{id}/tenants`,
  `/api/tenants/{id}/audit`) + `POST /api/v1/commands` +
  `GET /api/v1/commands/status/{correlationId}`. No new endpoints; receipts/previews/
  status assembled client-side from already-loaded read-model fields.
- **FrontComposer is the mandated UI framework AND the critical path.** Per repo domain-
  boundary policy, missing shared UI capability belongs in FrontComposer, not Tenants.
  Readiness updated by Story 1.0 spike note (2026-06-05): `FC-LYT`, `FC-CMD`,
  `FC-CNC`, `FC-A11Y`, `FC-L10N`, and `FC-DOC` are confirmed; `FC-TBL` is available
  with caveats; `FC-AUD` and `FC-CNS` remain covered by Product/UX-approved fallbacks;
  `FC-TOK` remains a missing shared capability covered by Tenants' canonical vocabulary
  and verified Fluent semantic/icon mapping until a shared token contract exists. The
  FC-AUD/FC-CNS/FC-CNC fallbacks are Product/UX-approved (2026-06-03 - see the
  Fallback Approval Record, `fallback-approval-record-2026-06-03.md`). `FC-TBL` does
  not provide cursor pagination, safety-column pinning, or the six non-collapsing list
  states required by Tenants. Tenant-list implementation must record a boundary decision
  before build-start. Story 1.2 resolved the Epic 1 path by composing a Tenants-specific
  `TenantDataGrid` from Fluent/FrontComposer primitives while keeping generic reusable
  cursor/pinning/list-state capability as a FrontComposer concern.
- **Fluent UI Blazor v5 pinned `5.0.0-rc.3-26138.1`** — exact token/component/ARIA names
  verified against the pinned package at build; none asserted available without check.
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
- **Open architectural question — UI host placement:** EventStore's domain-module policy
  forbids domain modules shipping their own AppHost/Aspire/ServiceDefaults; a Blazor UI
  host is a new artifact whose location (Tenants vs. FrontComposer vs. a composing host)
  and Aspire/DAPR wiring is itself a decision for this architecture.

### Cross-Cutting Concerns Identified

1. **Truth-state / honesty model (CP-1..CP-4)** — one shared client model behind every
   surface; never collapse `accepted`/`confirmed`/`audit available`; never show
   unconfirmed success.
2. **Authorization reflection (CP-9 / NFR-2)** — every actionable element reflects server
   auth, fail-closed; the UI is never the gate.
3. **Freshness & eventual consistency (NFR-3)** — ETag/304 + projection-as-truth + Blazor
   Auto reconnect re-derivation.
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
Tenants/EventStore REST + DAPR/SignalR backend. **No UI host exists today** (only AppHost,
Client, Contracts, Server, Testing) — one must be created as the first implementation story.

### Starter Options Considered

This is not a "pick a JS starter" decision; the ecosystem dictates the stack. Three
foundations were evaluated, grounded in the initialized submodules:

1. **New Blazor host composing the FrontComposer Shell** *(recommended)* — satisfies the PRD/UX
   "Operations Shell = FrontComposer shell" mandate and the repo domain-boundary policy (shared
   UI lives in FrontComposer, not Tenants). The Shell provides shell layout (FC-LYT), navigation
   from registered domain manifests, the projection DataGrid (FC-TBL), command dispatch, theming,
   and Fluxor-based state. Gated by FC-LYT readiness.
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
- FrontComposer.Shell + both reference UIs pin **Fluent UI Blazor `5.0.0-rc.3-26138.1`** (still
  RC; no GA as of 2026-06).

### Selected Starter: new `src/Hexalith.Tenants.UI` Blazor host composing the FrontComposer Shell

**Rationale for Selection:**
It is the only foundation that satisfies the "Operations Shell within a FrontComposer shell"
requirement and the domain-boundary policy. Epic 1 reuses FrontComposer shell/layout contracts and
uses Tenants-specific read components where FC-TBL does not meet cursor/safety-state needs, while
inheriting Fluent v5 + theming + manifest-driven navigation instead of rebuilding them in Tenants.
The implementation mirrors the proven EventStore reference UIs for host bootstrap, auth, and
backend access. (Option 2 remains a historical fallback path.)

**Initialization Command** *(no scaffolder exists — manual recipe; this is the first
implementation story):*

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

**Language & Runtime:** C# / .NET 10 (`net10.0`, SDK `10.0.300` pinned), `Microsoft.NET.Sdk.Web`;
Nullable + ImplicitUsings + `TreatWarningsAsErrors` + `ConfigureAwait(false)` per repo props.

**UI / Styling:** Microsoft Fluent UI Blazor v5 (`5.0.0-rc.3-26138.1`, RC — no GA yet), inherited
through the FrontComposer shell; semantic theme roles, no bespoke palette; Fluent type ramp /
shapes / elevation. Tenants tracks FrontComposer's transitive Fluent pin; tokens/ARIA verified
against the pinned package at build.

**Shell / Composition:** Hexalith.FrontComposer.Shell — shell layout (FC-LYT), navigation from
registered domain manifests, projection DataGrid (FC-TBL), command dispatch, theming. Consumed
via the Shell's registration extensions + a Tenants domain manifest + projection routing (exact
`AddHexalithFrontComposer*` / registry / route API names to be confirmed against the Shell source
in the integration spec).

**State Management:** Fluxor (the Shell's state substrate) — the natural home for the shared
client-side truth-state model.

**Backend Access:** REST to the existing query API + `POST /api/v1/commands` (+ status poll),
over DAPR service invocation (EventStore pattern) or HttpClient + Aspire service discovery
(decided in step-4); SignalR client for freshness nudges only.

**Testing:** bunit (component) + Playwright (E2E) + xUnit v3 + Shouldly; NFR-4 stable automation
selectors are first-class.

**Hosting:** new project added to `Hexalith.Tenants.slnx`; orchestrated by the existing
`Hexalith.Tenants.AppHost` (Aspire) with references to tenants/eventstore/keycloak; SDK container
support (`EnableContainer`, `ContainerRepository=tenants-ui`), no Dockerfile.

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
- **D3 FrontComposer posture:** **hybrid** — FC-LYT/FC-CMD/FC-CNC treated as contracts to
  confirm with the FrontComposer team; FC-AUD/FC-CNS delivered via the **approved fallbacks**
  (flat audit DataGrid, inline consequence text).
- **D4 Localization ownership:** **Tenants-owned** whole-string `.resx` keys; inherit only
  shell-chrome strings from `FcShellResources`.

**Important Decisions (made — cascade from D1–D4):**
- **D5 Truth-state model:** one shared **Fluxor "truth-state" feature** + a typed,
  casing-faithful **canonical-vocabulary library**.
- **D6 Freshness:** **server-side** conditional reads (`If-None-Match`→`304`); thresholds
  configurable + surfaced; `unknown` when unmeasurable (fail-closed).
- **D7 Authorization reflection:** **server-side** claims→action-availability service.
- **D8 Support-safety:** **server-side** receipt/preview/redaction assembly.
- **D9 Cursors:** opaque, **server-held** pass-through; page-1 re-query on invalidation.
- **D10 UI host placement:** new **`src/Hexalith.Tenants.UI`** in the Tenants repo.

**Deferred Decisions (post-MVP, with rationale):**
- NFR performance budgets (set against the real projection at implementation).
- Freshness numeric thresholds (product/ops input; kept out of the model as config).
- RTL shipping (Open Q#6), WCAG 2.2 confirmation (against the pinned Fluent build),
  sensitive-config display (Open Q#11) — none blocks the MVP.

### Data Architecture

No database decision — the UI **owns no datastore** and never writes one (NFR-5); it consumes
existing projections only.
- **Read access:** a typed query gateway in the BFF wrapping the 5 REST endpoints, using the
  `Hexalith.Tenants.Client`/`.Contracts` DTOs (`PaginatedResult<T>`, `TenantSummary`,
  `TenantDetail`, `TenantMember`, `UserTenantMembership`, `TenantAuditEntry`).
  - **Transport (regression guard, added 2026-06-06):** the BFF calls these `GET /api/tenants*`
    endpoints on the Tenants domain service directly (DAPR service invocation, server-side, bearer
    relayed). It MUST NOT route tenant reads through the EventStore generic query gateway
    (`POST /api/v1/queries` → `QueryRouter` / `HandlerAwareQueryRouter`): the projection actor is
    retired, and the handler-aware path drops projection ETags — breaking the D6 freshness contract
    below. See `sprint-change-proposal-2026-06-06-tenant-query-routing.md`.
- **Freshness/caching (D6):** conditional requests executed server-side; the Truth State Badge
  derives `current/refreshing/aging/stale/unknown` from ETag / timestamp / projection-version;
  thresholds are configuration, **no magic numbers**; unmeasurable → `unknown` → fail-closed.
- **Cursors (D9):** opaque, signed, scope-bound; held server-side, never surfaced as user-facing
  ids; on invalidation re-query page 1 with an honest "list refreshed" notice; multi-replica
  durability treated as **not-yet-guaranteed** (backend Epic 11).
- **Client read-model:** the Fluxor store is the runtime cache; the re-queried projection is
  authoritative; **last-confirmed projection is retained separately from in-flight intent**
  (non-collapse, CP-3).

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

- **Backend transport:** server-to-server from the BFF to the query API + command endpoint via
  **DAPR service invocation** (Aspire service discovery), mirroring the EventStore reference UIs.
  **No new backend endpoints.**
- **Command dispatch:** `POST /api/v1/commands` with a client-generated **`messageId` (ULID)**
  idempotency key; envelope `tenant=system`, `domain ∈ {tenants, global-administrators}`,
  `aggregateId`; returns `202` + `correlationId`.
- **Confirmation (D2):** parallel `GET /api/v1/commands/status/{correlationId}` poll **+** SignalR
  nudge → authoritative projection re-query; `confirmed` only from the re-query; SignalR never
  advances lifecycle/audit (CP-4); duplicate submit/refresh dedups by `correlationId`. NoOp →
  `already applied`; rejection → safe text; unverifiable → `unable to verify` (never success).
- **Concurrency policy:** **one-at-a-time** commands (FC-CNC fallback) until FC-CNC lands — no
  concurrent submission, bulk, or toast-batching. `409 ConcurrencyConflict` (+`Retry-After`) →
  `retry status lookup`.

### Frontend Architecture

- **Render mode:** Blazor **InteractiveServer**; components kept render-mode-agnostic where
  practical to preserve a future Auto option. Root composes `FluentProviders` + the FrontComposer
  shell. *(Reconcile the UX `EXPERIENCE.md` "Auto" assumption to InteractiveServer.)*
- **Shell composition:** compose `Hexalith.FrontComposer.Shell` — Operations Shell IA (**Tenants**
  default / **Global Administrators** / **Audit** primary; **Users contextual**, per the UX
  decision); register a Tenants domain manifest. Story 1.0 confirmed FC-LYT, and Story 1.2
  resolved the FC-TBL caveat by using Tenants-specific grid/table components for Epic 1 read
  surfaces while leaving generic reusable grid capability in FrontComposer.
- **Truth-state model (D5):** a single Fluxor **truth-state feature** is the one source for the 5
  truth dimensions and the canonical vocabularies (13 badge / 10 lifecycle / 10 feedback / 6
  reasons / 5 freshness / 4 audit), exposed as a typed, **casing-faithful** library used verbatim
  by every component (CP-10); **non-collapse enforced in the model** (`accepted`≠`confirmed`≠
  `audit available`; `degraded`/`unable to verify` success-prohibited). The 10 DESIGN.md
  components bind to this model.
- **Routing:** shell-managed routes + deep-linkable tenant detail; selection/filters/scroll
  preserved across navigation.
- **Localization (D4):** Tenants-owned whole-string `.resx` (named placeholders, no fragment
  assembly), culture-aware via `IStringLocalizer`; inherits only `FcShellResources` chrome.

### Infrastructure & Deployment

- **UI host (D10):** new `src/Hexalith.Tenants.UI` (`Microsoft.NET.Sdk.Web`, `net10.0`) in the
  Tenants repo; added to `Hexalith.Tenants.slnx`; orchestrated by the existing
  `Hexalith.Tenants.AppHost` with references to tenants + eventstore + keycloak; SignalR client to
  the EventStore hub. *(Reconcile with the EventStore domain-module boundary policy — the policy
  constrains domain-**service** modules from shipping AppHost/Aspire/ServiceDefaults; a presentation
  host is distinct, but consume platform `ServiceDefaults` rather than re-implement it.)*
- **Auth wiring:** AppHost wires the Keycloak realm + `Authentication:JwtBearer:*`;
  `EnableKeycloak=false` → symmetric-key JWT locally.
- **Containers:** SDK container support, `EnableContainer=true`,
  `ContainerRepository=tenants-ui` → `registry.hexalith.com/tenants-ui`; no Dockerfile.
- **CI/CD:** extend the existing pipeline (build Release `-warnaserror`); add bUnit unit + Playwright
  E2E tiers (E2E likely non-blocking like the Aspire tier). The UI host ships as a **container
  image, not a NuGet package** (unlike the 5 libraries). OpenTelemetry via ServiceDefaults; NFR-4
  stable automation selectors as component contracts.

### Decision Impact Analysis

**Implementation Sequence:**
1. **Bootstrap** `Hexalith.Tenants.UI` — shell composition, auth, BFF query gateway, Fluxor
   truth-state foundation + canonical-vocabulary library (the "Epic 1 / Story 1" bootstrap).
2. **Read surfaces (MVP — FR-1..9, FR-18)** using the confirmed **FC-LYT** contract.
3. **First command flows (FR-10/11/13/14)** using the confirmed **FC-CMD + FC-CNC** contracts.
4. **High-impact + audit + recovery (FR-12/15-17/19/20-25)** on the approved **FC-AUD/FC-CNS**
   fallbacks.

**Cross-Component Dependencies:**
The **Fluxor truth-state model**, **canonical-vocabulary library**, **BFF query/command gateway**,
**authorization-reflection service**, and **support-safety/redaction layer** are shared foundations
every surface depends on → built first. FrontComposer contract confirmations (FC-LYT/FC-CMD/FC-CNC)
are closed by Story 1.0 (2026-06-05); the FC-AUD/FC-CNS/FC-CNC fallback **approvals are secured**
(2026-06-03 - see `fallback-approval-record-2026-06-03.md`). The remaining pre-list implementation
decision is the `FC-TBL` caveat: Tenants-specific `TenantDataGrid` composition versus a reusable
FrontComposer grid enhancement.

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

**Razor components:** `PascalCase.razor`, one per file; the 10 DESIGN.md components keep exact names
(`TruthStateBadge`, `ConsequencePreview`, `CommandLifecyclePanel`, `UnavailableActionReason`,
`AuditEvidenceReceipt`, `TenantDataGrid`, `MemberTable`, `AuditDataGrid`, `PrimaryCommandButton`,
`DestructiveControl`). Folders organized **by surface/feature** (`Components/Tenants/`,
`Components/Audit/`, `Components/Shared/`), not by type.

**Fluxor:** `{Area}State` (immutable record); `{Verb}{Noun}Action` intent (`LoadTenantListAction`);
`{Noun}{Outcome}Action` result (`TenantListLoadedAction`, `TenantListLoadFailedAction`); reducers
`On{Action}`; effects `Async`-suffixed. One feature per surface + one shared `TruthStateFeature`.

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
│   ├── Shared/            # the 10 domain components + truth-state primitives
│   ├── Tenants/ Audit/ GlobalAdministrators/ Users/
│   └── Layout/ App.razor Routes.razor
├── State/                # Fluxor features (one per surface) + TruthState feature
├── Services/             # BFF gateways (query/command), authorization-reflection,
│                         #   support-safety/redaction, freshness, SignalR client
├── Vocabulary/           # the typed, casing-faithful canonical-state library (CP-10)
├── Resources/            # Tenants-owned .resx (D4)
└── Program.cs _Imports.razor wwwroot/css/
tests/Hexalith.Tenants.UI.Tests/   # bUnit + xUnit v3; {Class}Tests.cs (plural)
```

Tests live in a **separate `*.UI.Tests` project** (repo convention, never co-located); Playwright
E2E in its own tier.

### Format Patterns

- **Canonical state tokens (CP-10):** consumed **verbatim** from `Vocabulary/`, never hand-typed.
  Casing is significant — badge `audit pending` vs state-machine `audit_pending` stay distinct; **no
  agent unifies them**. The library is the single source.
- **Timestamps:** absolute, culture-formatted, monospace; **never relative-only**.
- **Identifiers:** literal caller-supplied strings, monospace; copy-full-id copies the literal;
  never parsed as ULID/Guid.
- **Truth-state shape:** every status = `{ token + freshness + absolute-timestamp + accessible-name }`;
  color never the sole carrier (icon + text always present).

### Communication Patterns

- **Fluxor discipline:** immutable state; **pure reducers** (no I/O); all I/O in **effects** calling
  the BFF gateways; UI dispatches **intent**, never mutates state. The **non-collapse invariant is
  enforced in the reducer** — `accepted`/`confirmed`/`audit available` are distinct fields and never
  overwrite last-confirmed projection with in-flight intent.
- **Command confirmation (D2) — the ONE pattern:** dispatch → effect runs status-poll **+** SignalR
  concurrently → first terminal/projection-change → **authoritative re-query action** → reducer flips
  `confirmed`. A SignalR nudge dispatches **only** a re-query action, never a state-advancing one
  (CP-4). No surface implements an alternative or optimistic path.
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
- Use the `Vocabulary/` canonical-state library verbatim (casing-significant) — never hand-type a
  token or unify badge vs state-machine forms.
- Route every backend call through a **BFF gateway**; never call the API from the browser or place
  tokens/payloads client-side.
- Confirm commands **only** via the D2 parallel-poll+SignalR→re-query path; never optimistic success.
- Localize via Tenants `.resx` whole-string keys; never assemble fragments.
- Tag interactive elements `data-testid="tenants-{surface}-{element}"`.
- Render the six list states, the fail-closed gating order, and the recovery-verb mapping as the
  shared patterns.

**Pattern Enforcement:** bUnit asserts non-collapse + the six states + no-color-only; Playwright
asserts the six required acceptance scenarios (stale projection, rejected command, unknown
confirmation, audit unavailable, last-owner warning, permission-missing) keyed on `data-testid`; a
guard test fails any surface that references a raw state literal instead of the Vocabulary library.
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

```
tenants/                                      # repo root (existing)
├── Hexalith.Tenants.slnx                     # + add the two new projects
├── Directory.Packages.props                  # + FluentUI, FrontComposer.Shell, SignalR.Client, JwtBearer
├── src/
│   ├── Hexalith.Tenants.AppHost/             # (existing) + AddProject<HexalithTenantsUI> + Keycloak wiring
│   ├── Hexalith.Tenants.Client/              # (existing) consumed by the BFF gateways
│   ├── Hexalith.Tenants.Contracts/           # (existing) DTOs/enums consumed verbatim
│   ├── Hexalith.Tenants.Server/              # (existing) domain service — untouched
│   └── Hexalith.Tenants.UI/                  # NEW Blazor InteractiveServer host (D1, D10)
│       ├── Hexalith.Tenants.UI.csproj        # Microsoft.NET.Sdk.Web, net10.0, EnableContainer
│       ├── Program.cs                        # AddRazorComponents().AddInteractiveServerComponents();
│       │                                     #   AddFluentUIComponents(); AddHexalithFrontComposer();
│       │                                     #   AddTenantsDomainManifest(); JwtBearer; BFF services
│       ├── _Imports.razor  appsettings*.json  Properties/launchSettings.json
│       ├── Components/
│       │   ├── App.razor  Routes.razor       # InteractiveServer root + FluentProviders
│       │   ├── Layout/
│       │   │   ├── MainLayout.razor          # composes <FrontComposerShell> (FC-LYT)
│       │   │   └── TenantsShellManifest.cs   # nav areas, surfaces, columns, routes, command policies
│       │   ├── Pages/
│       │   │   └── TenantAuditPage.razor      # FR-20..FR-25 (UJ-4)
│       │   ├── Shared/                        # the 10 DESIGN.md components + primitives
│       │   │   ├── TruthStateBadge.razor  ConsequencePreview.razor  CommandLifecyclePanel.razor
│       │   │   ├── UnavailableActionReason.razor  AuditEvidenceReceipt.razor
│       │   │   ├── TenantDataGrid.razor  MemberTable.razor  AuditDataGrid.razor
│       │   │   ├── PrimaryCommandButton.razor  DestructiveControl.razor
│       │   │   └── ListSurfaceStates.razor   # the six non-collapsible states
│       │   ├── Tenants/                       # nav area: Tenants (default)
│       │   │   ├── TenantListPage.razor       # FR-1, FR-2  (UJ-1)
│       │   │   ├── TenantDetailPage.razor     # FR-5, FR-7  (UJ-1)
│       │   │   ├── TenantConfigurationView.razor   # FR-6 (read) · FR-16/17 (edit)
│       │   │   ├── CreateTenantFlow.razor     # FR-13      (UJ-6)
│       │   │   ├── EditTenantMetadataFlow.razor    # FR-14
│       │   │   ├── DisableEnableTenantFlow.razor   # FR-15
│       │   │   ├── Audit/                    # Tenants-owned audit/recovery components
│       │   │   │   ├── AuditDataGrid.razor        # FR-20, FR-21
│       │   │   │   ├── AuditEvidenceReceipt.razor # FR-22, FR-23
│       │   │   │   ├── AuditAvailabilityState.razor
│       │   │   │   └── CorrectionStartPanel.razor # FR-24, FR-25 tenant-domain correction
│       │   │   └── Members/
│       │   │       ├── MemberAccessReview.razor    # FR-8, FR-9 (UJ-2)
│       │   │       ├── AddUserFlow.razor           # FR-10     (UJ-6)
│       │   │       ├── ChangeRoleFlow.razor        # FR-11     (UJ-5)
│       │   │       └── RemoveUserFlow.razor        # FR-12     (UJ-3 flagship)
│       │   ├── Users/                         # nav area: Users (contextual)
│       │   │   ├── MyTenantsPage.razor        # FR-3       (UJ-5 / Marc)
│       │   │   └── UserMembershipsLookup.razor     # FR-4
│       │   ├── GlobalAdministrators/          # nav area: Global Administrators
│       │   │   ├── GlobalAdminReview.razor    # FR-18      (UJ-2)
│       │   │   └── GlobalAdminCommandFlow.razor    # FR-19
│       ├── State/                            # Fluxor features (immutable state, pure reducers, effects)
│       │   ├── TruthState/                   # the shared 5-dimension model (D5)
│       │   ├── CommandLifecycle/             # the D2 confirm state machine
│       │   ├── TenantList/  TenantDetail/  Members/  Audit/  GlobalAdministrators/
│       ├── Services/                         # the server-side BFF + cross-cutting (D6–D8)
│       │   ├── Gateways/  TenantQueryGateway.cs  CommandGateway.cs   # the ONLY backend egress
│       │   ├── Authorization/  AuthorizationReflectionService.cs     # claims→availability (D7)
│       │   ├── SupportSafety/  NarrativePayloadAssembler.cs  Redactor.cs  ReceiptBuilder.cs (D8)
│       │   ├── Freshness/  FreshnessEvaluator.cs                     # ETag/304→freshness (D6)
│       │   ├── Realtime/  ProjectionNotificationClient.cs           # SignalR, nudge-only
│       │   └── Rejections/  RejectionTextCatalog.cs                 # rejection code→safe text
│       ├── Vocabulary/                       # CP-10 canonical-state library (single source)
│       │   ├── TruthStateBadge.cs (13)  Freshness.cs (5)  CommandLifecycle.cs (10 + machine tokens)
│       │   ├── LayeredFeedback.cs (10)  UnavailableActionReason.cs (6)  AuditAvailability.cs (4)
│       │   └── RecoveryVerbs.cs
│       ├── Resources/  TenantsResources.resx  TenantsResources.fr.resx   # D4, Tenants-owned
│       └── wwwroot/css/app.css
├── tests/
│   ├── Hexalith.Tenants.UI.Tests/            # NEW bUnit + xUnit v3 (Tier 1); {Class}Tests.cs
│   │   ├── Components/  State/  Services/  Vocabulary/   # mirrors src
│   └── Hexalith.Tenants.UI.E2E/              # NEW Playwright (Tier 3); the 6 acceptance scenarios
└── (existing) src/Hexalith.Tenants/ … Server/Testing as-is
```

### Architectural Boundaries

**API boundary (the trust edge):** the `Services/Gateways/` are the **only** egress to the backend
— `GET /api/tenants*`, `POST /api/v1/commands`, `GET /api/v1/commands/status/{id}` over DAPR service
invocation; `ProjectionNotificationClient` connects to the EventStore SignalR hub (nudge-only). The
**browser never calls the backend** and never holds a token (InteractiveServer, D1). No new backend
endpoints (NFR-5).

**Component boundary:** components are presentation-only — they subscribe to Fluxor state and
dispatch **intent** actions; **no component calls a gateway directly** (only effects do). The 10
Shared components are pure-view bound to the `TruthState` model.

**Service boundary:** gateways, authorization-reflection, freshness, support-safety/redaction, and
rejection-text all run **server-side** in the circuit; this is where redaction happens, so nothing
unsafe (tokens, payloads, correlation-ids, PII, stack traces) can cross into the rendered DOM (D8/§10).

**Data boundary:** the UI owns **no datastore**; the re-queried projection is the source of truth;
the Fluxor store is an ephemeral cache; **last-confirmed projection is held separately from in-flight
intent** (non-collapse). Cursors are opaque and server-held (D9).

**FrontComposer boundary:** Tenants composes the Shell (layout, manifest nav, FC-TBL DataGrid) and
**never re-implements** a missing FC capability — those are contracts (FC-LYT/FC-CMD/FC-CNC) or
approved fallbacks (FC-AUD/FC-CNS), per the domain-boundary policy.

### Requirements to Structure Mapping

| Feature group (FRs) | Lives in | Phase |
|---|---|---|
| 7.1 Discovery & Triage (FR-1..4) | `Components/Tenants/TenantList*`, `Components/Users/*` | 2a |
| 7.2 Detail & Config view (FR-5..7) | `Components/Tenants/TenantDetail*`, `TenantConfigurationView` | 2a |
| 7.3 Member & Access review (FR-8..9) | `Components/Tenants/Members/MemberAccessReview` | 2a |
| 7.4 Member & Role mgmt (FR-10..12) | `Components/Tenants/Members/{AddUser,ChangeRole,RemoveUser}Flow` | 2b/2c |
| 7.5 Lifecycle (FR-13..15) | `Components/Tenants/{CreateTenant,EditTenantMetadata,DisableEnableTenant}Flow` | 2b/2c |
| 7.6 Configuration mgmt (FR-16..17) | `Components/Tenants/TenantConfigurationView` (edit) | 2c |
| 7.7 Global-admin governance (FR-18..19) | `Components/GlobalAdministrators/*` | 2a/2c |
| 7.8 Audit trail & evidence (FR-20..23) | `Components/Pages/TenantAuditPage` and `Components/Tenants/Audit/{AuditDataGrid,AuditEvidenceReceipt,AuditAvailabilityState}` | 2c |
| 7.9 Compensating recovery (FR-24..25) | `Components/Tenants/Audit/CorrectionStartPanel` plus `State/TenantAudit/TenantCorrection*` models | 2c |

**Cross-cutting concerns → location:** truth-state model → `State/TruthState` + `Vocabulary/`;
command confirm → `State/CommandLifecycle` + `Services/Gateways/CommandGateway`; authorization
reflection → `Services/Authorization`; support-safety → `Services/SupportSafety`; freshness →
`Services/Freshness`; localization → `Resources/`; live-region/focus a11y → `Components/Shared`.

### Integration Points

**Internal communication:** Component → `dispatch(intent action)` → Reducer (pure) + Effect (I/O via
gateway) → `re-query action` → Reducer → Component re-renders from state. SignalR nudge → re-query
action only.

**External integrations:** Tenants/EventStore query+command API (DAPR), EventStore SignalR hub,
Keycloak/OIDC (or symmetric-key JWT dev) — all reached server-side via the AppHost-wired references.

**Data flow (command):** UI intent → `CommandGateway` (`POST /commands`, `messageId`) → 202 →
parallel status-poll **+** SignalR → authoritative projection re-query → `confirmed` → audit re-query
→ `audit available`. No optimistic path.

### File Organization Patterns

- **Configuration:** `appsettings*.json` (UI host); AppHost supplies `Authentication:JwtBearer:*` +
  service references; no secrets in the repo.
- **Source:** by surface under `Components/`; shared view in `Components/Shared/`; logic split across
  `State/` (Fluxor), `Services/` (server-side BFF), `Vocabulary/` (canonical tokens).
- **Test:** separate `*.UI.Tests` (bUnit, Tier 1) mirroring src + `*.UI.E2E` (Playwright, Tier 3);
  `{Class}Tests.cs` plural; never co-located.
- **Assets:** `wwwroot/css/app.css`; Fluent bundle via the package's static web assets; no bespoke palette.

### Development Workflow Integration

- **Dev server:** launched by `Hexalith.Tenants.AppHost` (`aspire run`) alongside tenants/eventstore/
  keycloak; placement+scheduler started first (slim mode); `http://localhost:8080`; `EnableKeycloak=false`
  → symmetric-key JWT.
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
InteractiveServer). Versions consistent: .NET 10 (10.0.300) + Fluent v5 RC pin inherited from
FrontComposer.

**Pattern Consistency:** patterns enforce the decisions — Vocabulary-verbatim (CP-10), BFF-only
egress (D1/D8), the single D2 confirm path, the six list states, l10n keys (D4), stable selectors
(NFR-4). No pattern contradicts a decision.

**Structure Alignment:** the tree realizes the decisions — `Services/` (BFF, D6–D9), `State/` (Fluxor,
D5), `Vocabulary/` (CP-10), `Resources/` (D4) — and the five boundaries make the trust edge explicit.

### Requirements Coverage Validation ✅

**Feature Coverage:** all 9 PRD feature groups mapped to concrete surfaces; all six journeys
(UJ-1..6) land on them.

**Functional Requirements:** all **25 FRs** have an architectural home, including the previously
story-less FR-22/24/25 (now structurally homed in `Components/Audit/` and covered by Epic 5 stories).

**Non-Functional Requirements:** NFR-1 (cursor + 304 + freshness; numeric budgets deferred), NFR-2
(server-enforced + reflection, tokens server-side), NFR-3 (D2 confirm + InteractiveServer + non-
collapse), NFR-4 (selectors + test tiers), NFR-5 (no datastore; compensating commands). CP-1..CP-10
encoded in the truth-state model + Vocabulary + process patterns.

### Implementation Readiness Validation ✅ design / ⛔ build-start (externally gated)

**Decision Completeness:** D1–D10 documented with the version posture. **Structure Completeness:**
complete tree + boundaries + FR mapping. **Pattern Completeness:** naming/structure/format/
communication/process + examples + anti-patterns; all conflict points addressed.

> The architecture is implementation-ready **as a design**. Build-start remains **externally gated**
> (see Gap Analysis), exactly as the PRD (§14) and the readiness report predicted.

### Gap Analysis Results

**Critical (block BUILD-START — external/downstream, not architecture deficiencies):**
- **FrontComposer readiness - CLOSED 2026-06-05 for FC-LYT/FC-CMD/FC-CNC.** D3 commits to a
  hybrid posture. The FC-AUD/FC-CNS/FC-CNC fallback **approvals are secured** (2026-06-03 - see
  `fallback-approval-record-2026-06-03.md`), and Story 1.0 confirms the shell/layout/command/
  concurrency/accessibility/localization/docs contracts. The remaining pre-list implementation
  item is the `FC-TBL` grid decision.
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
- Decide whether `Vocabulary/` becomes a shared project (so Server/Testing assert the same tokens).
- Deepen observability/telemetry + NFR test-design specifics.

### Validation Issues Addressed

No architecture-internal contradictions surfaced. The single decision↔source divergence (render mode
vs. the UX "Auto" assumption) is recorded as a reconciliation action item with rationale (trust/
support-safety + ecosystem alignment), for UX sign-off. All build-blocking items are external/
downstream and captured as explicit action items, not silent gaps.

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

**Overall Status:** READY WITH MINOR GAPS *(architecture design)* — **build-start remains externally
gated** by FrontComposer **contract** readiness (FC-LYT/FC-CMD/FC-CNC). The FC-AUD/FC-CNS/FC-CNC fallback
approvals are secured (2026-06-03 — see `fallback-approval-record-2026-06-03.md`); the epics/stories layer
now exists (`epics.md`). This is the documented external/downstream dependency chain (PRD §14, readiness
report), not an architecture deficiency.

**Confidence Level:** HIGH — coherent, full coverage of the 25 FRs + NFRs + CP contract, and
unambiguous decisions/patterns/structure for AI agents.

**Key Strengths:**
- The honesty/trust thesis is enforced **structurally** (server-side BFF + the D2 confirm path +
  Vocabulary-verbatim + non-collapse in the reducer), not left to per-surface discipline.
- Tight alignment with the existing ecosystem (FrontComposer Shell, reference UIs, repo conventions,
  fixed backend contract) — minimal new surface area, maximal reuse.
- Every FR has a home; every cross-cutting concern has a single owner.

**Areas for Future Enhancement:**
- Keep the Story 1.0 gate-clearing evidence current as FrontComposer evolves.
- Decide the `FC-TBL` tenant-list grid path before Story 1.2.
- Set the deferred numerics; reconcile the flagged doc items; track Fluent RC→GA.

### Implementation Handoff

**AI Agent Guidelines:**
- Follow D1–D10 and the patterns exactly; never optimistic success; Vocabulary verbatim; BFF-only
  egress; localize via Tenants `.resx`; tag `data-testid`.
- Respect the five boundaries; the projection re-query is the only source of truth.
- Refer to this document for all architectural questions; record any change here + in `project-context.md`.

**First Implementation Priority:**
The "Epic 1 / Story 1 bootstrap" — create `src/Hexalith.Tenants.UI` (the step-3 recipe), compose the
FrontComposer Shell, wire JWT + the BFF query gateway, stand up the Fluxor `TruthState` feature + the
`Vocabulary/` library — **after** FC-LYT is confirmed and the Shell integration spike is done.
