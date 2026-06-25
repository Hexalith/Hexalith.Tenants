# Sprint Change Proposal — Adopt EventStore Read-Model Freshness Metadata in Tenants

- **Date:** 2026-06-25
- **Author / Facilitator:** Correct Course workflow (with Administrator)
- **Change trigger key:** `cc-2026-06-21-eventstore-read-model-freshness-metadata`
- **New story key:** `cc-2026-06-25-tenant-read-model-freshness-adoption`
- **Approval:** Administrator — "Server-side, 3-state" option, Incremental review (2026-06-25)
- **Scope classification:** **Moderate** (one new Tenants-owned story + planning/tracking artifact updates; no epic restructure, no submodule edits)

---

## Section 1 — Issue Summary

The EventStore owner handoff `eventstore-2026-06-19-read-model-freshness-metadata` **shipped** the
generic, persisted-timestamp read-model freshness surface in `Hexalith.EventStore.Client.Projections`
(EventStore `main` → `5613fed4`):

- `IReadModelFreshness` (`ProjectedAt`, `ProjectionVersion`)
- `ReadModelFreshnessState` (`Unknown`/`Current`/`Aging`/`Stale`)
- `ReadModelFreshnessThresholds` (+ validating `Create`)
- `ReadModelFreshness.Classify(...)` / `.Age(...)` (pure, explicit-clock)
- `IReadModelStore.GetWithFreshnessAsync<T>()` and `IReadModelFreshness.ToQueryResponseMetadata(...)`

This was the platform capability the prior Tenants story `cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening`
**deferred** (its **AC2** — produce real `current`/`aging`/`stale`/`unknown` from a persisted projection
timestamp against configurable thresholds — was explicitly routed to the EventStore owner rather than
hand-rolled in Tenants). With the surface now available, the Tenants-side adoption is the open
follow-up recorded in `deferred-work.md:120-129`.

**Why it matters (evidence, from current code):**

- `src/Hexalith.Tenants/Queries/TenantQueryResult.cs` (`FromPayload`) derives metadata **entirely from
  the read-model ETag** (`ProjectionVersion = ETag`); it sets **no** `ProjectedAt` and **no** `ServedAt`.
- The Tenants server **never** sets `IsStale`, `IsDegraded`, or `WarningCodes`.
- `TenantQueryGateway.ResolveFreshness` therefore can only ever yield **`Current`** (ETag present) or
  **`Unknown`** (absent). `TenantFreshnessState.Aging`, `.Stale`, and `.Refreshing` are **declared,
  resourced (EN+FR across 11 surfaces), and badge-rendered, but have no producer** in the real read flow.
- The Tenants read models (`TenantReadModel`, `TenantIndexReadModel`, `TenantAuditReadModel`,
  `GlobalAdministratorReadModel`) carry **no projection timestamp** (only `TenantReadModel.CreatedAt`,
  the tenant's own creation time — not a projection-write instant).

The result is a freshness model that promises five badge states but produces two, with a private,
duplicate enum (`TenantFreshnessState`) shadowing the now-shared platform type.

---

## Section 2 — Impact Analysis

### Epic Impact

- **No epic restructure.** Product epics 1–5 are `done`; this is cross-cutting hardening that *completes*
  NFR-3 / D6 rather than changing product scope. No epic added, removed, or resequenced.

### Story Impact

- **New:** `cc-2026-06-25-tenant-read-model-freshness-adoption` (full spec in Section 4).
- **Closes the follow-up** opened by `cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening`
  (AC2) and the `deferred-work.md` handoff entry.
- The EventStore-side tracking story `cc-2026-06-21-eventstore-read-model-freshness-metadata` (status
  `review`) is **left as-is** — it tracks the already-pushed EventStore implementation; its Tenants
  follow-up is now carried by the new story.

### Artifact Conflicts

- **Architecture `architecture.md` (D6)** — `:311-312` and `:337-338` assert a **5-state** badge
  (`current/refreshing/aging/stale/unknown`) and "thresholds configurable + surfaced." The platform
  wire contract `QueryResponseMetadata` carries only **`current`/`stale`/`unknown`**; `refreshing` is a
  client transient; `aging` collapses into `current` (per EventStore's own `ToQueryResponseMetadata`)
  until a `QueryResponseMetadata.ProjectedAt` wire field exists. D6 reconciled to the 3-state-on-wire
  reality (Edits 2a/2b).
- **`deferred-work.md`** — the handoff's Tenants-adoption follow-up is routed to the new story; the
  residual (`aging` end-to-end) is recorded as a future, separate EventStore handoff (Edit 3).
- **`sprint-status.yaml`** — new story entry `ready-for-dev` (Edit 4).
- **`tests/test-summary.md`** — AC2 coverage note truth-up: deferred → routed/delivered as
  `current`/`stale`/`unknown` (Edit 5).
- **PRD / UX spec** — no document change required; the Truth State Badge's 5-state intent is reconciled
  in D6 (the badge keeps rendering all five labels; only the *producible* set is clarified).

### Technical Impact (handed to the Developer agent — NOT applied by this workflow)

- **Server (`Hexalith.Tenants.Server` + host):** four read models implement `IReadModelFreshness`
  (persisted `ProjectedAt`, optional `ProjectionVersion`); projection handlers stamp `ProjectedAt` on
  **every** write via an injected `TimeProvider`; query handlers classify via `ToQueryResponseMetadata`
  with thresholds bound from configuration (D6); `ServedAt` is populated honestly (and never used as
  age for classification).
- **UI (`Hexalith.Tenants.UI`):** delete `State/TruthState/TenantFreshnessState.cs`; migrate 11
  snapshot/row types + `ResolveFreshness` + `TruthStateBadge.razor` to the shared
  `ReadModelFreshnessState`; `Refreshing` becomes a transient badge flag (separate from the persisted
  classification). `.resx` keys (`Current`/`Aging`/`Stale`/`Unknown`/`Refreshing`) already align — no
  resource churn.
- **Tests:** server tests per freshness state (the deferred AC2 coverage); UI gateway/badge tests
  updated to the shared enum.
- **Coverage gates:** the branch-coverage-gated files (`TenantAggregate.cs`,
  `GlobalAdministratorsAggregate.cs`, `ChangeUserRoleValidator.cs`) are untouched → gate unaffected;
  the 80% line gate spans the Server + UI edits.

### ⚠️ Design caveat carried into the story (semantics of `ProjectedAt`)

`ReadModelFreshness.Classify` measures `ProjectedAt` against **`now`**. In Tenants' **synchronous
projection-as-truth** model, `ProjectedAt` = last projection write = last event applied to that
aggregate. So a tenant that is perfectly current but simply **quiescent** would age toward `Stale`
(quiescence ≠ lag). The story therefore (a) stamps `ProjectedAt` on every projection write, (b) makes
thresholds **configuration** (D6), and (c) **defaults them conservatively** so only genuinely extreme
ages classify as `Stale`. True per-aggregate lag detection (comparing against latest-event-time) is a
documented future enhancement, not in scope.

---

## Section 3 — Recommended Approach

**Direct Adjustment (Option 1) — "Server-side, 3-state" (Administrator-approved).**

Adopt the shipped EventStore surface end-to-end on the server, surfacing `current`/`stale`/`unknown`
over the existing `QueryResponseMetadata` (the states the wire can carry), and unify the UI onto the
shared `ReadModelFreshnessState`, retiring the duplicate `TenantFreshnessState`. `Refreshing` is kept
as a client transient; `aging` is deliberately collapsed to `current` per the platform contract.

**Rationale & trade-offs:**

- **Faithful to the platform contract** — uses `ToQueryResponseMetadata` exactly as EventStore intends
  (server classifies from the persisted timestamp; the boolean outcome crosses the wire). No
  Tenants-owned generic scaffolding (CLAUDE.md domain-boundary rule honored).
- **No submodule round-trip** — the EventStore surface already shipped; this is a single Tenants-owned
  story.
- **Honest** — finally makes `Stale` a *producible* state from real data and retires three dead enum
  values' false promise; the `aging` gap is explicitly documented, not silently dropped.
- **Trade-off accepted:** real `aging` is not surfaced (the wire has no `ProjectedAt`); recovering it
  later is a small, separate EventStore handoff (add `QueryResponseMetadata.ProjectedAt`) so the UI can
  own thresholds — captured as the residual in `deferred-work.md`.

Alternatives considered and declined: full 5-state with a new EventStore wire change (reopens a
cross-submodule round-trip for marginal UX gain now); UI-only type-unification (cosmetic; leaves
`Stale` unproducible); close-as-won't-do + simplify to two states (discards the just-shipped platform
capability). Effort **Medium**, risk **Low–Medium**.

---

## Section 4 — Detailed Change Proposals

### 4.1 New story (create)

`_bmad-output/implementation-artifacts/cc-2026-06-25-tenant-read-model-freshness-adoption.md`

```
---
title: 'Adopt EventStore read-model freshness metadata in Tenants (retire hand-rolled TenantFreshnessState)'
type: 'correct-course-hardening'
created: '2026-06-25'
status: 'ready-for-dev'
sprint_key: 'cc-2026-06-25-tenant-read-model-freshness-adoption'
source_proposal: '_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-25-tenant-read-model-freshness-adoption.md'
approval: 'Administrator approved 2026-06-25 (Server-side, 3-state option)'
context:
  - '{project-root}/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/Hexalith.AI.Tools/hexalith-state-instructions.md'
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture.md'
  - '{project-root}/_bmad-output/implementation-artifacts/cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening.md'
  - '{project-root}/_bmad-output/implementation-artifacts/deferred-work.md'
---

## Intent
Complete the deferred AC2 of cc-2026-06-19 by adopting the shipped EventStore surface
(IReadModelFreshness, ReadModelFreshness.Classify, ReadModelFreshnessThresholds,
ToQueryResponseMetadata) so Tenants produces real current/stale/unknown freshness from a
persisted projection timestamp, and retire the hand-rolled TenantFreshnessState enum.

## Boundaries & Constraints
Always:
- Consume the EventStore Client freshness types; add NO Tenants-owned generic freshness scaffolding.
- ProjectedAt is stamped on EVERY projection write (TenantProjectionHandler + audit + index + GA).
- Freshness thresholds are configuration (D6), bound in the Tenants host; default CONSERVATIVE so
  quiescent-but-current aggregates do not render Stale (ProjectedAt measures last projection write).
- Keep reads on the REST Tenants endpoints; keep domain ids as strings; keep failure copy support-safe.
Never:
- Do not use ServedAt as projection age for classification (classify from persisted ProjectedAt).
- Do not surface Aging over the wire (collapses to current per ToQueryResponseMetadata) — Aging stays
  a dormant UI value pending a future QueryResponseMetadata.ProjectedAt wire field (separate handoff).
- Do not reintroduce TenantsProjectionActor / generic query-gateway routing.

## Code Map
- src/Hexalith.Tenants.Server/Projections/{TenantReadModel,TenantIndexReadModel,
  TenantAuditReadModel,GlobalAdministratorReadModel}.cs  (implement IReadModelFreshness)
- src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs (+ audit/index/GA projection paths)
- src/Hexalith.Tenants/Queries/TenantQueryResult.cs + Queries/Handlers/TenantQueryHandlerBase.cs
- src/Hexalith.Tenants/<host>/appsettings*.json + freshness-thresholds options binding
- src/Hexalith.Tenants.UI/State/TruthState/TenantFreshnessState.cs (DELETE)
- src/Hexalith.Tenants.UI/State/**/*Snapshot.cs,*Row.cs (11 sites) + Services/Gateways/TenantQueryGateway.cs
- src/Hexalith.Tenants.UI/Components/Shared/TruthStateBadge.razor (Refreshing → transient param)
- tests/Hexalith.Tenants.Server.Tests/* + tests/Hexalith.Tenants.UI.Tests/*

## Tasks & Acceptance
1. Read models implement IReadModelFreshness (ProjectedAt + ProjectionVersion=ETag-or-monotonic);
   projection handlers stamp ProjectedAt via injected TimeProvider on every write. [test]
2. Query handlers classify via ToQueryResponseMetadata(thresholds, now, eTag); emit current/stale/
   unknown; thresholds from config, conservative defaults. ServedAt no longer faked as age. [test each state]
3. UI: delete TenantFreshnessState; migrate 11 snapshot/row types + ResolveFreshness +
   TruthStateBadge to ReadModelFreshnessState; Refreshing becomes a transient badge flag. [test]
4. .resx keys verified (Current/Aging/Stale/Unknown/Refreshing) unchanged; no fragment assembly.
5. All suites green; coverage gates hold; D6 architecture + deferred-work + test-summary updated.

## Verification
- dotnet test tests/Hexalith.Tenants.Server.Tests -c Release --filter "Freshness|Query"
- dotnet test tests/Hexalith.Tenants.UI.Tests -c Release --filter "Freshness|TenantQuery|Badge"
- dotnet build Hexalith.Tenants.slnx -c Release  (warnaserror)
```

### 4.2 Architecture (edit) — `architecture.md`

**Edit 2a (`:311-312`):**

```
OLD:
- **D6 Freshness:** **server-side** conditional reads (`If-None-Match`→`304`); thresholds
  configurable + surfaced; `unknown` when unmeasurable (fail-closed).

NEW:
- **D6 Freshness:** **server-side** conditional reads (`If-None-Match`→`304`); freshness is
  classified **server-side** from a **persisted projection timestamp** (`IReadModelFreshness.ProjectedAt`,
  EventStore Client) against **configurable thresholds**; the wire (`QueryResponseMetadata`) carries
  **`current`/`stale`/`unknown`** only (`refreshing` is a client transient; `aging` collapses into
  `current` until a `QueryResponseMetadata.ProjectedAt` wire field exists — future EventStore handoff);
  `unknown` when unmeasurable (fail-closed). Thresholds default **conservative** because `ProjectedAt`
  measures the last projection write (quiescence ≠ lag).
```

**Edit 2b (`:337-338`):**

```
OLD:
- **Freshness/caching (D6):** conditional requests executed server-side; the Truth State Badge
  derives `current/refreshing/aging/stale/unknown` from ETag / timestamp / projection-version;
  thresholds are configuration, **no magic numbers**; unmeasurable → `unknown` → fail-closed.

NEW:
- **Freshness/caching (D6):** conditional requests executed server-side; freshness is classified
  server-side from the persisted projection timestamp via the shared `ReadModelFreshness.Classify`
  (EventStore Client) and surfaced as `QueryResponseMetadata.IsStale`. The Truth State Badge renders
  the shared `ReadModelFreshnessState` (`current/aging/stale/unknown`) plus a client-only `refreshing`
  transient; on the wire only `current/stale/unknown` are producible today (`aging` collapses to
  `current`). Thresholds are configuration, **no magic numbers**, defaulted conservatively;
  unmeasurable → `unknown` → fail-closed.
```

### 4.3 Deferred-work (edit) — `deferred-work.md:122`

```
OLD:
Status: **IMPLEMENTED 2026-06-21** in the `Hexalith.EventStore.Client.Projections` namespace under Administrator approval (`IReadModelFreshness` + `ReadModelFreshness*` types + `IReadModelStore.GetWithFreshnessAsync<T>()`/`ToQueryResponseMetadata()`). Committed + pushed (EventStore `main` → `5613fed4`). Tenants-UI adoption (replace the hand-rolled `TenantFreshnessState` with this shared surface) remains a follow-up.

NEW:
Status: **IMPLEMENTED 2026-06-21** in the `Hexalith.EventStore.Client.Projections` namespace under Administrator approval (`IReadModelFreshness` + `ReadModelFreshness*` types + `IReadModelStore.GetWithFreshnessAsync<T>()`/`ToQueryResponseMetadata()`). Committed + pushed (EventStore `main` → `5613fed4`). **Tenants-side adoption ROUTED 2026-06-25** to Tenants-owned story `cc-2026-06-25-tenant-read-model-freshness-adoption` (Correct Course, "Server-side, 3-state" option approved by Administrator): read models implement `IReadModelFreshness` (persisted `ProjectedAt`), query handlers classify via `ToQueryResponseMetadata` (current/stale/unknown), and the UI retires the hand-rolled `TenantFreshnessState` for the shared `ReadModelFreshnessState`. **Residual (not this story):** `aging` is not producible end-to-end because `QueryResponseMetadata` has no `ProjectedAt` field and `ToQueryResponseMetadata` collapses `aging`→`current`; surfacing real `aging` would need a `QueryResponseMetadata.ProjectedAt` wire addition (a NEW EventStore owner handoff) so the UI can classify with its own thresholds — deferred, not in scope.
```

### 4.4 Sprint status (edit) — `sprint-status.yaml`

Append after the `cc-2026-06-25-collapse-duplicate-ui-command-request-dtos: done` block, and bump
`last_updated`:

```
  # --- Correct Course 2026-06-25: adopt EventStore read-model freshness metadata ---
  # Approved by Administrator 2026-06-25 (Correct Course; "Server-side, 3-state" option).
  # Proposal: sprint-change-proposal-2026-06-25-tenant-read-model-freshness-adoption.md
  # Scope (Moderate): complete the deferred AC2 of cc-2026-06-19 now that the EventStore read-model
  # freshness surface has shipped (5613fed4). Tenants read models implement IReadModelFreshness
  # (persisted ProjectedAt, stamped on every projection write); query handlers classify server-side
  # via ToQueryResponseMetadata (current/stale/unknown; configurable, conservatively-defaulted
  # thresholds; ServedAt no longer faked as age); the UI retires the hand-rolled TenantFreshnessState
  # for the shared ReadModelFreshnessState (Refreshing kept as a client transient). No submodule edits.
  # Aging stays a dormant UI value pending a future QueryResponseMetadata.ProjectedAt wire field
  # (separate EventStore handoff). Architecture D6 + deferred-work.md + test-summary.md reconciled to
  # the 3-state-on-wire reality.
  cc-2026-06-25-tenant-read-model-freshness-adoption: ready-for-dev
```

### 4.5 Test-summary (edit) — `tests/test-summary.md:310`

```
OLD:
- Coverage: story AC1, AC3-AC8 covered; AC2's `aging`/`stale` threshold portion is deferred to the EventStore read-model freshness handoff (`eventstore-2026-06-19-read-model-freshness-metadata`). The direct-read D6 model is ETag/projection-version `current`; unmarked responses are `unknown`; generic persisted projection age/version remains an EventStore owner handoff before threshold-based `aging` can be computed.

NEW:
- Coverage: story AC1, AC3-AC8 covered; AC2's `aging`/`stale` threshold portion was deferred to the EventStore read-model freshness handoff (`eventstore-2026-06-19-read-model-freshness-metadata`), which **shipped (EventStore `5613fed4`)**. Tenants-side adoption is now **routed to `cc-2026-06-25-tenant-read-model-freshness-adoption`** (Correct Course 2026-06-25, "Server-side, 3-state"): read models persist `ProjectedAt` (`IReadModelFreshness`) and query handlers classify server-side via `ToQueryResponseMetadata` into `current`/`stale`/`unknown` against configurable, conservatively-defaulted thresholds (test-per-state added by that story). `aging` stays dormant end-to-end (the wire `QueryResponseMetadata` carries no `ProjectedAt` and collapses `aging`→`current`); real `aging` needs a future `QueryResponseMetadata.ProjectedAt` wire field. Until `cc-2026-06-25` lands, the live model stays direct-read ETag/projection-version `current`, unmarked → `unknown`.
```

---

## Section 5 — Implementation Handoff

- **Scope classification:** **Moderate** — backlog reorganization (new story) + planning/tracking
  artifact updates. No PRD/MVP change, no epic restructure, no submodule edits.
- **This workflow applies (on final approval):** create the story file (4.1) and apply the four
  artifact edits (4.2–4.5).
- **Routed to: Developer agent** for the code implementation under the new story
  `cc-2026-06-25-tenant-read-model-freshness-adoption` (server read models + projection handlers +
  query-handler classification + thresholds config + UI enum migration + tests), followed by the
  standard adversarial code review.
- **Success criteria:**
  - Server emits `current`/`stale`/`unknown` from a real persisted `ProjectedAt` via
    `ToQueryResponseMetadata`; `ServedAt` is never used as age for classification.
  - `TenantFreshnessState` is deleted; the UI consumes the shared `ReadModelFreshnessState`;
    `Refreshing` is a transient badge flag.
  - Per-state server tests + updated UI gateway/badge tests pass; `Hexalith.Tenants.slnx` Release
    `-warnaserror` clean; coverage gates hold.
  - Conservative threshold defaults documented; `aging` end-to-end residual recorded as a future
    EventStore handoff.
- **Conventional Commits:** `feat` (new user-observable `stale` capability) on a `feat/…` branch; the
  doc/tracking edits ride along or land as `docs`/`chore` per the implementer's split.
