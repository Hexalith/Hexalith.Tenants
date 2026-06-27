---
baseline_commit: 58fdaef696f77247c7a8efb29615557864d28b0b
title: 'Adopt EventStore read-model freshness metadata in Tenants (retire hand-rolled TenantFreshnessState)'
type: 'correct-course-hardening'
created: '2026-06-25'
status: 'review'
sprint_key: 'cc-2026-06-25-tenant-read-model-freshness-adoption'
source_proposal: '_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-25-tenant-read-model-freshness-adoption.md'
approval: 'Administrator approved 2026-06-25 (Server-side, 3-state option)'
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-state-instructions.md'
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture.md'
  - '{project-root}/_bmad-output/implementation-artifacts/cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening.md'
  - '{project-root}/_bmad-output/implementation-artifacts/deferred-work.md'
---

<frozen-after-approval reason="human-approved correct-course scope - do not expand without re-approval">

## Intent

Complete the deferred AC2 of `cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening` by
adopting the shipped EventStore surface (`IReadModelFreshness`, `ReadModelFreshness.Classify`,
`ReadModelFreshnessThresholds`, `IReadModelStore.GetWithFreshnessAsync<T>()`,
`IReadModelFreshness.ToQueryResponseMetadata()`) so Tenants produces real `current`/`stale`/`unknown`
freshness from a persisted projection timestamp, and retire the hand-rolled `TenantFreshnessState` enum
in favor of the shared `ReadModelFreshnessState`.

## Boundaries & Constraints

**Always:**
- Consume the EventStore Client freshness types; add NO Tenants-owned generic freshness scaffolding
  (domain-boundary rule — generic capability lives in EventStore).
- Stamp `ProjectedAt` on EVERY projection write (`TenantProjectionHandler` per-aggregate, audit, and
  cross-aggregate index paths, plus the global-administrators projection path) via an injected
  `TimeProvider` — never `DateTimeOffset.UtcNow` inline.
- Freshness thresholds are configuration (D6), bound in the Tenants host; default CONSERVATIVE so a
  quiescent-but-current aggregate does not render `Stale` (`ProjectedAt` measures the last projection
  write, i.e. the last applied event — quiescence is not lag).
- Keep reads on the REST Tenants endpoints; keep domain ids as caller-supplied strings; keep all
  user-facing failure copy support-safe (no payloads, tokens, ETags, cursors, correlation ids).

**Never:**
- Do not use `ServedAt` (response time) as projection age for classification — classify from the
  persisted `ProjectedAt`.
- Do not surface `Aging` over the wire (it collapses to `current` per `ToQueryResponseMetadata`).
  `Aging` stays a dormant UI value pending a future `QueryResponseMetadata.ProjectedAt` wire field
  (a separate EventStore owner handoff).
- Do not reintroduce `TenantsProjectionActor` / `TenantProjectionRouting` / the EventStore generic
  query-gateway route for tenant reads.

</frozen-after-approval>

## Story

As a Tenants module maintainer,
I want Tenants to adopt the shared EventStore read-model freshness metadata end to end,
so that tenant query surfaces can report real `current`/`stale`/`unknown` freshness without a
Tenants-owned duplicate freshness model or fabricated age.

The approved scope above is frozen. The sections below are implementation context and guardrails for
the Developer agent; they do not expand the approved correct-course scope.

## Code Map

- `src/Hexalith.Tenants.Server/Projections/TenantReadModel.cs` (implement `IReadModelFreshness`)
- `src/Hexalith.Tenants.Server/Projections/TenantIndexReadModel.cs` (implement `IReadModelFreshness`)
- `src/Hexalith.Tenants.Server/Projections/TenantAuditReadModel.cs` (implement `IReadModelFreshness`)
- `src/Hexalith.Tenants.Server/Projections/GlobalAdministratorReadModel.cs` (implement `IReadModelFreshness`)
- `src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs` (+ audit/index paths) and the
  `src/Hexalith.Tenants/Projections/GlobalAdministratorProjectionHandler.cs` path (stamp `ProjectedAt`
  via `TimeProvider`)
- `src/Hexalith.Tenants/Projections/ProjectionDispatcher.cs` and `src/Hexalith.Tenants/Program.cs`
  (thread/register `TimeProvider`; keep `/project` mapping behavior unchanged)
- `src/Hexalith.Tenants/Queries/TenantQueryResult.cs` (classify via `ToQueryResponseMetadata`)
- `src/Hexalith.Tenants/Queries/Handlers/TenantQueryHandlerBase.cs` and all six query handlers
  (`GetTenant*`, `ListTenants`, `GetUserTenants`, `GetGlobalAdministrators`) so `CreateSuccessResult`
  threads the read model + thresholds + clock, not just the ETag
- `src/Hexalith.Tenants/Program.cs` + `appsettings*.json` (bind `ReadModelFreshnessThresholds` from
  config; conservative defaults; register `TimeProvider`)
- `src/Hexalith.Tenants.UI/State/TruthState/TenantFreshnessState.cs` — DELETE
- Minimum UI state-layer migration targets:
  `TenantDetailSnapshot`, `TenantListSnapshot`, `TenantListRow`, `UserTenantMembershipSnapshot`,
  `UserTenantMembershipRow`, `GlobalAdministratorsSnapshot`, `GlobalAdministratorRow`,
  `TenantAuditSnapshot`, `TenantAuditRow`, `TenantAuditReceipt`, `TenantLifecycleAvailability`,
  `TenantCorrectionStartIntent`, plus `TenantQueryGateway.ResolveFreshness`
- `src/Hexalith.Tenants.UI/Components/Shared/TruthStateBadge.razor` (`Refreshing` → transient param/flag
  separate from the persisted classification)
- Every production/test hit from `rg -n "TenantFreshnessState" src tests` is in scope for migration.
  Several command and audit components gate on `TenantFreshnessState.Current`; do not only update row
  DTOs.
- `tests/Hexalith.Tenants.Server.Tests/*` + `tests/Hexalith.Tenants.UI.Tests/*`

## Developer Context

### Existing Server State

- `TenantReadModel`, `TenantIndexReadModel`, `TenantAuditReadModel`, and
  `GlobalAdministratorReadModel` are plain JSON-serializable read models with no freshness properties
  today. `TenantReadModel.CreatedAt` is the tenant domain creation time and must not be reused as
  projection freshness.
- `TenantProjectionHandler.ProjectAsync` writes three Tenants models: per-aggregate tenant read model,
  per-aggregate audit read model, and singleton tenant index. The audit model is built first to fail
  the whole batch before any write if `MessageId`/`UserId` invariants are broken. Preserve that
  write-order safety.
- `GlobalAdministratorProjectionHandler.ProjectAsync` rebuilds the singleton global-administrators
  model and writes `audit:system`. It currently uses unconditional `IReadModelStore.SaveAsync`; stamp
  both the global-admin model and its audit model before saving.
- Query handlers currently read `ReadModelEntry<T>` for tenant/index/audit models and pass only the
  ETag into `TenantQueryResult.FromPayload`. `GetGlobalAdministratorsQueryHandler` is the exception:
  it uses `GetStateAsync<GlobalAdministratorReadModel>()`, which drops the ETag. Change it to the
  entry-returning path before building metadata.
- `TenantQueryResult.FromPayload` currently normalizes the ETag and creates metadata from ETag only:
  `ETag` and `ProjectionVersion` are both the normalized ETag. Keep the empty/quote-only ETag
  normalization behavior from `cc-2026-06-19`; do not reintroduce metadata with an empty ETag.

### Shared EventStore Freshness Surface

- The source of truth is `Hexalith.EventStore.Client.Projections` in the EventStore submodule:
  `IReadModelFreshness`, `ReadModelFreshnessState`, `ReadModelFreshnessThresholds`,
  `ReadModelFreshness.Classify/Age`, `GetWithFreshnessAsync<T>()`, and
  `IReadModelFreshness.ToQueryResponseMetadata(...)`.
- `IReadModelFreshness.ProjectedAt` is a persisted read-model timestamp set by the projection on
  write. It is not response time and not the domain event timestamp.
- `ToQueryResponseMetadata` maps `ReadModelFreshnessState.Stale` to `IsStale=true`, maps
  `Current` and `Aging` to `IsStale=false`, and maps `Unknown` to `IsStale=null`. This is why the
  wire is only `current`/`stale`/`unknown` today.
- `ReadModelFreshnessThresholds.Create(aging, stale)` rejects negative thresholds and rejects
  `stale < aging`. Use the factory when validating configuration.
- `ProjectionEventDto.SequenceNumber` is aggregate-local. Do not treat it as a global version for the
  singleton tenant index or global-administrators model. `ProjectionVersion` is optional; preserving a
  defensible opaque token is fine, but `ProjectedAt` is the acceptance-critical field.

### Projection Stamping Rules

- Inject `TimeProvider` into projection handlers through production composition. Preserve convenient
  test constructors or update the existing tests explicitly; never call `DateTimeOffset.UtcNow` inline.
- Stamp `ProjectedAt = timeProvider.GetUtcNow()` once per projection write, after the incoming events
  have been applied/merged and before the model is saved. Use the same `now` for all models written by
  one projection request unless tests require a different seam.
- Do not use `ProjectionEventDto.Timestamp` as `ProjectedAt`; that timestamp belongs to the persisted
  event/audit narrative, while this story tracks when the read model was projected.
- Preserve audit ordering and support-safety: `TenantAuditReadModel.SortEntries()` still orders by
  audit entry timestamp/event id, and narrative payload filtering must not start exposing raw payloads,
  correlation ids, ETags, cursors, tokens, or stack traces.

### Query Classification Rules

- Keep tenant reads on the REST-backed Tenants endpoints and in-process `IDomainQueryHandler`s. Do not
  route through `TenantsProjectionActor`, `TenantProjectionRouting`, `POST /api/v1/queries`, or the
  EventStore generic query gateway.
- Build metadata from the persisted read model via `ToQueryResponseMetadata(thresholds, now, eTag)`.
  `ServedAt` may be populated by that helper as response metadata, but no code may use it as projection
  age.
- A missing read model or a model with `ProjectedAt = null` is `unknown`, not `current`. Existing 404
  and forbidden behaviors still apply before successful metadata is emitted.
- Keep authorization gates and cursor handling unchanged. The story is about metadata/freshness, not
  read visibility, cursor shape, or endpoint contracts.
- Choose conservative default thresholds because `ProjectedAt` measures last projection write. A quiet
  but current tenant should not become practically unusable just because no new event has arrived.
  Real lag detection against latest-event-time is explicitly out of scope.

### UI Migration Rules

- Replace the Tenants enum with `Hexalith.EventStore.Client.Projections.ReadModelFreshnessState`.
  The shared enum values are `Unknown = 0`, `Current = 1`, `Aging = 2`, and `Stale = 3`; it has no
  `Refreshing`.
- `Refreshing` remains a UI transient, not persisted snapshot state. `TruthStateBadge` can expose an
  explicit transient flag/parameter and render the existing `*.Freshness.Refreshing` resource key when
  that flag is true.
- Existing resource keys for `Current`, `Aging`, `Stale`, `Unknown`, and `Refreshing` should remain
  whole-string EN/FR resources. Do not assemble freshness copy from fragments.
- Preserve the fail-closed command/action gates: stale or unknown freshness still blocks high-impact
  actions; aging is not producible over the wire yet and must not silently become a new bypass for
  existing stale/unknown checks.
- The list/search path hydrates Memories search hits through the authoritative tenant detail read.
  Do not derive freshness from Memories search results.
- The badge must continue to use FrontComposer/Fluent v5 components, icon + visible text, and no
  color-only meaning. Do not replace it with raw HTML controls or custom theme tokens.

### Previous Story Intelligence

- `cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening` fixed a false-freshness bug:
  `ServedAt` alone no longer proves `Current`. Preserve that regression guard.
- That story intentionally deferred AC2 (`aging`/`stale` threshold classification) to EventStore until
  the shared persisted freshness surface shipped. This story closes only the Tenants adoption side.
- The 2026-06-19 implementation established the direct-read fallback: ETag/projection-version evidence
  meant `Current`, absent markers meant `Unknown`. After this story, persisted `ProjectedAt` supersedes
  that fallback for successful reads backed by freshness-aware models.

### Testing Requirements

- Add read-model tests proving all four read models implement `IReadModelFreshness`, persist
  `ProjectedAt`, and do not confuse domain `CreatedAt` / audit entry timestamps with projection
  freshness.
- Add projection-handler tests using a deterministic/fake `TimeProvider` so tenant read model, audit
  model, tenant index, global-administrators model, and global-administrators audit model are stamped
  on write.
- Add query-handler or integration-style tests for `current`, `stale`, and `unknown` metadata. Include
  the `aging` band collapsing to wire-current (`IsStale=false`) and the absent timestamp path
  (`IsStale=null`).
- Preserve ETag tests: null/whitespace/quote-only ETags must still fail closed; a real ETag must still
  drive conditional `304` behavior.
- Add/adjust UI gateway and badge tests so `ReadModelFreshnessState` maps to the same visible labels and
  stale/degraded surface kinds, with `Refreshing` covered only through the transient badge flag.
- Before completion, run `rg -n "TenantFreshnessState" src tests`; production references should be gone
  and tests should only mention it if intentionally asserting deletion/migration.

### References

- Source proposal: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-25-tenant-read-model-freshness-adoption.md`
- Architecture D6: `_bmad-output/planning-artifacts/architecture.md`
- Prior freshness story: `_bmad-output/implementation-artifacts/cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening.md`
- Deferred handoff: `_bmad-output/implementation-artifacts/deferred-work.md`
- EventStore API source:
  `references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Projections/ReadModelFreshness*.cs`,
  `IReadModelFreshness.cs`, `IReadModelStore.cs`,
  `references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Queries/QueryResponseMetadata.cs`
- UX freshness and badge rules:
  `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md`,
  `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md`
- Project rules: `_bmad-output/project-context.md`,
  `references/Hexalith.AI.Tools/hexalith-llm-instructions.md`,
  `references/Hexalith.AI.Tools/hexalith-state-instructions.md`,
  `references/Hexalith.AI.Tools/hexalith-ux-instructions.md`

## Tasks & Acceptance

**Execution:**
- [x] Implement `IReadModelFreshness` on the four read models (`ProjectedAt`; `ProjectionVersion` may
      map to the ETag or a monotonic counter).
- [x] Stamp `ProjectedAt` on every projection write via an injected `TimeProvider`.
- [x] Thread read-model entries through all query handlers, including global administrators, and
      classify server-side via `ToQueryResponseMetadata(thresholds, now, eTag)`; emit
      `current`/`stale`/`unknown`; bind and validate thresholds from config with conservative defaults.
- [x] Delete `TenantFreshnessState`; migrate the 11 snapshot/row types, `ResolveFreshness`, and
      `TruthStateBadge` to `ReadModelFreshnessState`; make `Refreshing` a transient badge flag.
- [x] Run `rg -n "TenantFreshnessState" src tests` after migration and resolve every production/test
      reference intentionally.
- [x] Verify `.resx` keys (`Current`/`Aging`/`Stale`/`Unknown`/`Refreshing`) still resolve; no fragment
      assembly; EN/FR parity intact.
- [x] Run focused suites; broaden as needed; keep coverage gates green; update D6 architecture,
      `deferred-work.md`, and `test-summary.md` (applied by the source Correct Course proposal).

**Acceptance Criteria:**
1. Given a successful tenant query backed by a persisted read model with `ProjectedAt`, when the handler
   builds metadata, then freshness is classified server-side via `ReadModelFreshness`/`ToQueryResponseMetadata`
   against configured thresholds and `ServedAt` is never used as projection age.
2. Given the configured thresholds, when `ProjectedAt` age is within `Aging`, between `Aging` and
   `Stale`, or beyond `Stale`, then the wire reports `current` (incl. the `aging`-collapsed band) or
   `stale` accordingly, and an absent/unmarked timestamp reports `unknown` — with a test for each state.
3. Given no Tenants-owned generic persistence scaffolding is added, when freshness metadata is needed,
   then only the EventStore Client freshness types are consumed.
4. Given the UI, when `TenantFreshnessState` is removed, then all 11 snapshot/row types, `ResolveFreshness`,
   and `TruthStateBadge` consume the shared `ReadModelFreshnessState`, with `Refreshing` rendered from a
   transient flag rather than the persisted classification, and EN/FR badge labels unchanged.
5. Given thresholds measure `ProjectedAt`-vs-`now`, when defaults are chosen, then they are conservative
   and documented so a quiescent-but-current aggregate does not render `Stale`; real per-aggregate lag
   detection (vs latest-event-time) is recorded as out of scope.
6. Given all configured suites, when the story completes, then `Hexalith.Tenants.slnx` Release builds
   `-warnaserror` clean, coverage gates hold, and per-state freshness tests pass.

### Review Findings

- [ ] [Review][Decision] Clarify submodule pointer updates in a no-submodule story - The reviewed diff updates `Hexalith.EventStore` from `d9d3ee0f8eb39a43c25a728d31fc4b19e6d85a0d` to `825a849cd07110a1c8c2ccb124c9123934c9fabd` and `Hexalith.Memories` from `183b53dcced10d5f41b8c804afc6be5858a4cdad` to `0c07af3c2633d6ffacf08ffee742b9536019ed4a`, while the source proposal says this story has no submodule edits and no submodule round-trip. The EventStore pointer may be required for the freshness API, but the Memories pointer appears unrelated to the freshness acceptance criteria; choose whether to keep and document these as dependency updates or split/revert them before accepting the story.
- [x] [Review][Patch] REST query responses drop freshness classification before the UI can consume it [src/Hexalith.Tenants/Controllers/TenantsQueryController.cs:453] — Fixed 2026-06-27: emitted and parsed `X-Hexalith-Is-Stale` freshness metadata.
- [x] [Review][Patch] ETag-less successful read models skip `ProjectedAt` freshness classification [src/Hexalith.Tenants/Queries/TenantQueryResult.cs:50] — Fixed 2026-06-27: freshness metadata now classifies read-model age without requiring an ETag.
- [ ] [Review][Patch] Gateway maps server-unknown freshness with an ETag back to current [src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs:782]
- [ ] [Review][Patch] Conditional 304 paths overwrite cached non-current freshness as current [src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs:54]
- [ ] [Review][Patch] Search results force list-level freshness to current even when hydrated rows are stale or unknown [src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs:514]
- [ ] [Review][Patch] Create tenant command gate treats unknown list freshness as fresh [src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:78]

Recheck 2026-06-27 (server freshness/projection/query chunk): the first two patch findings remain
current. No additional server projection/query findings were accepted after triage; the remaining
chunk candidates were dismissed as already intentional, covered by existing references, or not caused
by a projection write path.

## Verification

- `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Release --no-restore --filter "Freshness|Query"`
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore --filter "Freshness|TenantQuery|Badge"`
- `dotnet build Hexalith.Tenants.slnx -c Release` (with `-warnaserror`)
- `git diff --check`

## Dev Agent Record

### Implementation Plan

- Follow the story task order: first make persisted read-model freshness available, then stamp writes,
  classify query metadata from the persisted timestamp, migrate UI state to the shared enum, and update
  verification/docs evidence.
- Use only `Hexalith.EventStore.Client.Projections` freshness types; do not add Tenants-owned generic
  freshness abstractions.
- Keep freshness timestamps separate from domain event timestamps and response `ServedAt`.

### Debug Log

- Added red-phase read-model freshness tests; initial run failed on missing `ProjectedAt` members, as
  expected.
- Implemented `IReadModelFreshness` on `TenantReadModel`, `TenantIndexReadModel`,
  `TenantAuditReadModel`, and `GlobalAdministratorReadModel`.
- Verified focused server read-model freshness tests pass:
  `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Release --no-restore --filter ReadModelFreshness`.
- Added red-phase projection stamping tests for tenant, audit, index, global-administrators, and
  global-administrators audit writes; initial run failed on missing `TimeProvider` constructors, as
  expected.
- Injected `TimeProvider` through projection handlers, `ProjectionDispatcher`, and the `/project`
  endpoint; focused stamping tests pass:
  `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Release --no-restore --filter Stamps`.
- Added red-phase query freshness tests for `current`, `aging`-collapsed-to-wire-current, `stale`,
  and `unknown`, plus primary read-model coverage across all six query handlers.
- Bound conservative freshness thresholds from `Tenants:ReadModelFreshness`, validated through
  `ReadModelFreshnessThresholds.Create`, and changed query handlers to build metadata from
  `IReadModelFreshness.ToQueryResponseMetadata`.
- Verified broader server query tests pass:
  `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Release --no-restore --filter Query`.
- Migrated durable UI state, gateway mapping, action gates, and tests from `TenantFreshnessState` to
  EventStore `ReadModelFreshnessState`; deleted the Tenants-owned enum.
- Converted `TruthStateBadge` `Refreshing` rendering to the transient `IsRefreshing` flag and added
  focused badge coverage for the flag.
- Verified `rg -n "TenantFreshnessState" src tests` and
  `rg -n "ReadModelFreshnessState\\.Refreshing" src tests` produce no matches.
- Verified EN/FR freshness resource parity remains intact at 55 matching keys per culture.
- Full Server.Tests initially exposed unrelated local DAPR scope drift (`memories-server` expectation
  vs current `memories` AppHost id). Reconciled the conformance tests/docs to the current local
  Memories app id and reran Server.Tests green.
- Verified focused and broad suites pass, then reran `Hexalith.Tenants.slnx` Release `-warnaserror`
  clean and `git diff --check` clean.

### Completion Notes

- The four persisted read models now expose `ProjectedAt` and `ProjectionVersion` through the shared
  EventStore freshness interface.
- Focused tests prove projection freshness is not confused with tenant domain `CreatedAt` or audit
  entry timestamps.
- Projection handlers now stamp the same `ProjectedAt` value on every model written for a projection
  request, including tenant detail, tenant audit, tenant index, global administrators, and system audit.
- Query handlers now classify freshness from persisted `ProjectedAt`; `Aging` maps to wire-current,
  `Stale` maps to `IsStale=true`, and missing timestamps map to `IsStale=null`.
- Defaults are intentionally conservative (`Aging=365d`, `Stale=3650d`) because `ProjectedAt` measures
  the last projection write, not event-store lag.
- UI snapshots, rows, gateway freshness resolution, and high-impact gates now consume the shared
  `ReadModelFreshnessState`; `Refreshing` no longer exists as durable state.
- D6 architecture, deferred-work routing, cross-aggregate timing docs, and test-summary evidence now
  reflect the implemented 3-state-on-wire adoption.
- Verification completed: focused Server 123/123, focused UI 95/95, full Contracts 106/106, Client
  48/48, Testing 181/181, Sample 39/39, Server 735/735, UI 761/761, Integration 223 passed / 1 skipped,
  and Release solution build 0 warnings / 0 errors.

## File List

- `_bmad-output/implementation-artifacts/cc-2026-06-25-tenant-read-model-freshness-adoption.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/planning-artifacts/architecture.md`
- `docs/cross-aggregate-timing.md`
- `src/Hexalith.Tenants.Server/Projections/GlobalAdministratorReadModel.cs`
- `src/Hexalith.Tenants.Server/Projections/TenantAuditReadModel.cs`
- `src/Hexalith.Tenants.Server/Projections/TenantIndexReadModel.cs`
- `src/Hexalith.Tenants.Server/Projections/TenantReadModel.cs`
- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor`
- `src/Hexalith.Tenants.UI/Components/Shared/TruthStateBadge.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/RemoveTenantConfigurationFlow.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/SetTenantConfigurationFlow.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleActionAvailability.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleCommandFlow.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/AddTenantMemberFlow.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/ChangeTenantMemberRoleFlow.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor`
- `src/Hexalith.Tenants.UI/Components/_Imports.razor`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorRow.cs`
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorsSnapshot.cs`
- `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditReceipt.cs`
- `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditRow.cs`
- `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditSnapshot.cs`
- `src/Hexalith.Tenants.UI/State/TenantAudit/TenantCorrectionStartIntent.cs`
- `src/Hexalith.Tenants.UI/State/TenantDetail/TenantDetailSnapshot.cs`
- `src/Hexalith.Tenants.UI/State/TenantDetail/TenantLifecycleAvailability.cs`
- `src/Hexalith.Tenants.UI/State/TenantList/TenantListRow.cs`
- `src/Hexalith.Tenants.UI/State/TenantList/TenantListSnapshot.cs`
- `src/Hexalith.Tenants.UI/State/TruthState/TenantFreshnessState.cs` (deleted)
- `src/Hexalith.Tenants.UI/State/UserTenants/UserTenantMembershipRow.cs`
- `src/Hexalith.Tenants.UI/State/UserTenants/UserTenantMembershipSnapshot.cs`
- `src/Hexalith.Tenants/Configuration/ReadModelFreshnessOptions.cs`
- `src/Hexalith.Tenants/Program.cs`
- `src/Hexalith.Tenants/Projections/GlobalAdministratorProjectionHandler.cs`
- `src/Hexalith.Tenants/Projections/ProjectionDispatcher.cs`
- `src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs`
- `src/Hexalith.Tenants/Queries/Handlers/GetGlobalAdministratorsQueryHandler.cs`
- `src/Hexalith.Tenants/Queries/Handlers/GetTenantAuditQueryHandler.cs`
- `src/Hexalith.Tenants/Queries/Handlers/GetTenantQueryHandler.cs`
- `src/Hexalith.Tenants/Queries/Handlers/GetTenantUsersQueryHandler.cs`
- `src/Hexalith.Tenants/Queries/Handlers/GetUserTenantsQueryHandler.cs`
- `src/Hexalith.Tenants/Queries/Handlers/ListTenantsQueryHandler.cs`
- `src/Hexalith.Tenants/Queries/Handlers/TenantQueryHandlerBase.cs`
- `src/Hexalith.Tenants/Queries/TenantQueryResult.cs`
- `src/Hexalith.Tenants/appsettings.json`
- `tests/Hexalith.Tenants.Server.Tests/Configuration/SnapshotConfigurationTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Configuration/EventPublicationConfigurationTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Documentation/CrossAggregateTimingDocumentationTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/GlobalAdministratorProjectionHandlerTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/ReadModelFreshnessTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantProjectionHandlerTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Queries/TenantQueryFreshnessTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Queries/TenantQueryHandlerETagTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Support/TenantQueryTestHarness.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/*.cs` (freshness enum migration across affected component tests)
- `tests/Hexalith.Tenants.UI.Tests/Components/TruthStateBadgeTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/UnavailableTenantQueryGatewayTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/State/*.cs` (freshness enum migration across affected state tests)
- `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs`
- `tests/test-summary.md`

## Change Log

- 2026-06-25T20:22:28+02:00 - Started story implementation, captured baseline commit, and added read-model freshness metadata.
- 2026-06-25T20:44:45+02:00 - Completed EventStore read-model freshness adoption, UI shared-state migration, documentation updates, and full verification; moved story to review.
