---
stepsCompleted: [1, 2, 3]
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
→ **2c** (high-impact + audit + recovery: FR-12/15/16/17/19/20–25). FR-22/24/25 (and
weakly FR-7/FR-23) currently lack backing stories.

**Non-Functional Requirements — the honesty contract is the architecture driver.**
- **NFR-3 Reliability/consistency (defining):** eventually-consistent, event-sourced;
  projection is the source of truth; correct under at-least-once delivery + projection
  lag; under Blazor Auto (prerender→Server→WASM + reconnect) the UI re-derives truth and
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
- Primary domain: **Web frontend** — a Blazor (Auto) domain UI composed on the
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
  authorization-reflection, localization) composing the **10 domain UI components**
  (DESIGN.md) over FC-TBL.

### Technical Constraints & Dependencies

- **Consume-only backend (fixed):** 5 read endpoints (`GET /api/tenants`,
  `/api/tenants/{id}`, `/api/tenants/{id}/users`, `/api/users/{id}/tenants`,
  `/api/tenants/{id}/audit`) + `POST /api/v1/commands` +
  `GET /api/v1/commands/status/{correlationId}`. No new endpoints; receipts/previews/
  status assembled client-side from already-loaded read-model fields.
- **FrontComposer is the mandated UI framework AND the critical path.** Per repo domain-
  boundary policy, missing shared UI capability belongs in FrontComposer, not Tenants.
  Readiness: `FC-TBL` available; `FC-LYT`/`FC-CMD`/`FC-A11Y`/`FC-L10N`/`FC-DOC`
  needs-confirmation; `FC-CNC`/`FC-TOK`/`FC-AUD`/`FC-CNS` missing. **FC-LYT gates even
  the read-only MVP; FC-CMD+FC-CNC gate all commands.** No fallback is recorded as
  approved (a PRD↔UX contradiction to reconcile).
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
requirement and the domain-boundary policy, reuses the available FC-TBL DataGrid for every read
surface, and inherits Fluent v5 + theming + manifest-driven navigation instead of rebuilding
them in Tenants — while mirroring the proven EventStore reference UIs for host bootstrap, auth,
and backend access. (Option 2 is recorded as the FC-LYT-blocked fallback.)

**Initialization Command** *(no scaffolder exists — manual recipe; this is the first
implementation story):*

```bash
# from repo root: create the Blazor Web App host, then wire FrontComposer + Fluent
dotnet new blazor -n Hexalith.Tenants.UI -o src/Hexalith.Tenants.UI \
  --interactivity Auto --all-interactive -f net10.0
# then: add to Hexalith.Tenants.slnx; reference Tenants.Client (+ a ServiceDefaults);
# add FrontComposer.Shell + Fluent UI Blazor packages (versions via Directory.Packages.props);
# compose the shell in MainLayout and register the Tenants domain manifest.
```

> The `--interactivity` value (Auto vs Server) is an **open decision finalized in step-4**; no
> package versions go in the .csproj (central `Directory.Packages.props`).

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

**Open foundation decisions (resolved in step-4 — Decisions):**
- **Render mode** — UX `EXPERIENCE.md` assumes **Blazor Auto** (prerender→Server→WASM+reconnect)
  and frames the honesty contract around reconnect-safety; the ecosystem reference UIs use
  **InteractiveServer**, which most directly satisfies "never resurrect optimistic success"
  (server-held circuit state). Pick one and align the consistency model to it.
- **Use the Shell vs. fallback custom layout** — tied to FC-LYT readiness.
- **Backend transport** — DAPR service invocation vs. HttpClient + Aspire service discovery.

**Note:** Project initialization using this recipe should be the **first implementation story**
(the "Epic 1 / Story 1 bootstrap": shell composition, routing, auth, projection/SignalR client),
per the implementation-readiness report's recommendation.
