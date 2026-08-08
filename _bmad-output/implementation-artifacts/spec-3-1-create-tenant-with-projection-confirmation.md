---
title: 'Create Tenant with Projection Confirmation'
type: 'feature'
created: '2026-08-08'
status: 'done'
baseline_commit: 'fa5ca5596f4a627713dce9ea712a0de81cf670bf'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-3-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Create-tenant confirms from TenantId existence alone, treats all Unknown list freshness as creatable, and mints a new messageId on every submit — so acceptance, a pre-existing row, or a reconnect retry can be mistaken for proven creation.

**Approach:** Harden the existing Story 2.1 create flow for confirmation honesty: empty-list-only first-tenant freshness exception, provenance-qualified confirmation with submitted metadata, and messageId reuse — without inventing endpoints or absorbing deferred lock/nudge/mobile work.

## Boundaries & Constraints

**Always:**
- BFF egress only via `ITenantCommandGateway` → `POST /api/v1/commands` + correlation status; literal TenantId as AggregateId (never trim/normalize/GUID/ULID-parse/generate).
- Distinct lifecycle vocabulary; never collapse accepted / projection-pending / confirmed / rejected / unable-to-verify / audit handoff.
- `confirmed` requires literal tenant + submitted metadata **and** projection-version advancement or safe command-specific audit provenance beyond a pre-submit baseline.
- `TenantAlreadyExists` is always Rejected — never NoOp, already-applied, or success.
- First-tenant exception only for an authoritatively empty list whose freshness is `unknown` solely for missing first write timestamp; non-empty/ambiguous Unknown fails closed.
- Same logical attempt reuses `messageId`/correlation; mint a new ULID only for a deliberate new attempt.
- Fail closed on invalid/indeterminate validation, freshness, authorization reflection, or command-surface readiness with localized inline reason.
- Support-safe mapping; EN/FR parity; stable `data-testid` contracts.

**Ask First:**
- Adding a domain `CreateTenantValidator` or changing aggregate field rules beyond UI/gateway empty/whitespace guards.
- Implementing deferred AggregateIdentity lock, SignalR nudge wiring, or mobile fail-closed in this slice.

**Never:**
- New endpoints, browser-direct backend calls, optimistic tenant rows, or reshaped contracts.
- Confirming from acceptance, SignalR alone, unrelated projection churn, or pre-submit existence without provenance.
- Stories 3.2–3.6 scope, event/projection edits, or undo/rollback.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Provenance-qualified confirm | Tenant + submitted metadata **and** version/audit advances past baseline | `Confirmed` (+ audit-pending) | N/A |
| Existence without provenance | Match without baseline advancement / missing usable baseline when pre-existing | Not `Confirmed` — pending or `unable to verify` | Refresh / retry status / continue read-only |
| Pre-existing id | Backend `TenantAlreadyExists` | `Rejected` (never AlreadyApplied/success) | Refresh / open-existing copy when authorized |
| First-tenant Unknown | Empty authoritative list + Unknown | Create remains available | N/A |
| Non-empty Unknown | Rows/ambiguous + Unknown | Create unavailable (`stale data`) | Localized reason |
| Reconnect same attempt | Retry with stored tracking | Reuse `messageId`; no second dispatch | Lost tracking → `unable to verify` |
| Whitespace input | Empty/whitespace id or name | No dispatch | Field guidance |
| Command surface down | Unavailable gateway / BFF disconnected | Fail closed before dispatch | Localized unavailable reason |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor` -- form/lifecycle; existence-only confirm; always-new messageId; `IsNullOrEmpty` validation
- `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor` -- blanket `IsFresh = Current or Unknown` (~156); no BFF surface pass; list-only `FindTenantProjectionEvidenceAsync`
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs` -- `TenantCreateCommandSnapshot.ConfirmProjection` (~212–242) TenantId-only; membership snapshots already use baseline provenance
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantMembershipCommandProvenance.cs` -- reuse opaque version/audit advancement helpers
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs` / `TenantCommandGateway.cs` -- `CreateTenantAsync` lacks optional `messageId`; `TenantAlreadyExists` safe map present
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs` -- `IsCommandSurfaceConnected` to thread into create
- `src/Hexalith.Tenants.UI/State/TenantList/TenantListSnapshot.cs` -- list `ProjectionVersion` + freshness/kind/items for baseline + first-tenant gate
- `src/Hexalith.Tenants.Contracts/Queries/TenantSummary.cs` -- Id+Name+Status; Description needs `GetTenant` when verifying full metadata
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantQueryGateway.cs` -- `GetTenantAsync` unused by create today
- Resources `TenantsResources.resx` (+ `.fr.resx`) -- extend `Tenants.Create.*` for provenance/first-tenant/stale keys
- Tests: `TenantCreateCommandSnapshotTests.cs`, `CreateTenantFlowTests.cs`, `TenantCommandGatewayTests.cs`
- Patterns only: membership confirm in same models file; `spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
- Tracking: `sprint-status.yaml` → `3-1-create-tenant-with-projection-confirmation`

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs` -- capture pre-submit baseline (list ProjectionVersion, tenant-absent, attempt start); harden `ConfirmProjection` for metadata match + version/audit provenance; never Confirm on pre-existing/missing provenance; keep AlreadyExists as Rejected-only -- confirmation honesty
- [x] `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs` (+ `TenantCommandGateway.cs`) -- optional `messageId` on `CreateTenantAsync` -- reconnect/idempotency
- [x] `src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor` -- whitespace-safe validation; persist attempt tracking + reuse messageId; capture baseline; confirm via list and/or `GetTenant` detail with provenance -- flow meets narrowed contract
- [x] `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor` -- empty-list-only Unknown exception; pass `BffComposition.IsCommandSurfaceConnected` -- host freshness/surface honesty
- [x] `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` (+ `.fr.resx`) -- Create provenance/first-tenant/stale/unable-to-verify whole-strings with EN/FR parity -- support-safe copy
- [x] `tests/Hexalith.Tenants.UI.Tests/State/TenantCreateCommandSnapshotTests.cs` (+ `CreateTenantFlowTests.cs`, `TenantCommandGatewayTests.cs`) -- cover I/O matrix edges -- regression net
- [x] `_bmad-output/implementation-artifacts/sprint-status.yaml` -- move `3-1-create-tenant-with-projection-confirmation` through ready-for-dev → in-progress → review -- tracking

**Acceptance Criteria:**
- Given create reaches reconciliation, when the tenant matches submitted metadata but provenance does not advance past baseline, then the attempt is not `Confirmed`.
- Given an authoritatively empty Unknown list (missing first write only), when an otherwise eligible caller opens create, then it remains available; given non-empty/ambiguous Unknown, when availability is evaluated, then create fails closed as stale.
- Given `TenantAlreadyExists`, when lifecycle renders, then state is Rejected, never already-applied or success.
- Given the same logical attempt is retried with tracking available, when submit is considered, then the original `messageId` is reused with no second dispatch.
- Given focused snapshot, gateway, flow, and workspace tests run, when verification completes, then the matrix and non-collapse invariants pass.

## Spec Change Log

## Design Notes

Create usually has no pre-submit tenant. Capture `BaselineTenantAbsent` + list `ProjectionVersion` (may be null on true first-tenant empty). Confirm only when authoritative evidence shows literal TenantId + submitted Name (+ Description via `GetTenant` when verifying full metadata) **and** either list/detail version advances past a non-empty baseline, or null-baseline first appearance from proven-absent baseline with a non-empty post-create version / qualifying audit provenance. If the tenant was present at baseline, never Confirm.

Prefer extending `TenantMembershipCommandProvenance` over duplicating opaque comparison. Do not add domain max-length validators unless Ask First approves. Deferred: AggregateIdentity lock, SignalR nudge wiring, mobile fail-closed / open-existing CTAs (`deferred-work.md`).

## Verification

**Commands:**
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --filter "FullyQualifiedName~TenantCreateCommandSnapshotTests|FullyQualifiedName~CreateTenantFlowTests|FullyQualifiedName~TenantCommandGatewayTests"` -- expected: matching tests pass (xUnit v3 executable fallback if MTP/VSTest hits)
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md` -- expected: exit 0

## Suggested Review Order

**Provenance-qualified confirmation**

- Confirm only after ProjectionPending with metadata match plus version advancement.
  [`TenantCreateCommandModels.cs:225`](../../src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs#L225)

- Missing provenance maps to UnableToVerify with localized SafeMessageKey.
  [`TenantCreateCommandModels.cs:280`](../../src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs#L280)

**Create flow admission and reconciliation**

- Reuse tracked attempts via status refresh; admit submit before baseline capture.
  [`CreateTenantFlow.razor:300`](../../src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor#L300)

- Fail-closed baseline absence capture and detail reconciliation paths.
  [`CreateTenantFlow.razor:428`](../../src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor#L428)

**Workspace gating and evidence**

- Empty-list-only Unknown freshness exception plus BFF surface wiring.
  [`TenantsWorkspace.razor:411`](../../src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor#L411)

- Evidence rows and ProjectionVersion come from the same list snapshot.
  [`TenantsWorkspace.razor:965`](../../src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor#L965)

**Gateway idempotency**

- Single CreateTenantAsync contract honors optional messageId reuse.
  [`ITenantCommandGateway.cs:7`](../../src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs#L7)

- ResolveMessageId mint-or-reuse plus whitespace reject before submit.
  [`TenantCommandGateway.cs:40`](../../src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs#L40)

**Tests**

- Snapshot provenance and pre-existing baseline non-confirm cases.
  [`TenantCreateCommandSnapshotTests.cs:113`](../../tests/Hexalith.Tenants.UI.Tests/State/TenantCreateCommandSnapshotTests.cs#L113)

- Flow whitespace, description/detail confirm, retry, and provenance copy.
  [`CreateTenantFlowTests.cs:95`](../../tests/Hexalith.Tenants.UI.Tests/Components/CreateTenantFlowTests.cs#L95)

- Workspace first-tenant, non-empty Unknown, and disconnected BFF gating.
  [`TenantsWorkspaceTests.cs:563`](../../tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs#L563)
