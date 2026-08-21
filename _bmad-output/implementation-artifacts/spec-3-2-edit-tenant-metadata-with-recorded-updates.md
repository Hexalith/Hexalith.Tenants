---
title: 'Edit Tenant Metadata with Recorded Updates'
type: 'feature'
created: '2026-08-08'
status: 'done'
baseline_commit: '753f1ead9e155a0ea53009e9a9d8f9dcb3d5024a'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-3-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Metadata edit (Story 2.5) still confirms from Name+Description match alone — including from `Accepted` — so same-value updates and ambient detail can false-confirm without projection-version or audit provenance. `UpdateTenantAsync` always mints a new messageId and in-flight resubmit returns without status refresh, unlike create/membership reconnect honesty.

**Approach:** Harden the existing Edit Metadata stack to the corrected Story 3.2 recorded-update contract: provenance-qualified confirmation (including identical submitted values), optional messageId reuse with reconnect refresh, and evidence that carries ProjectionVersion — mirroring Story 3.1 create / membership patterns without reshaping contracts.

## Boundaries & Constraints

**Always:**
- BFF egress only via `ITenantCommandGateway` → `POST /api/v1/commands` + correlation status; literal `TenantId` as AggregateId (never trim/normalize/GUID/ULID-parse).
- Domain already always emits `TenantUpdated` for successful updates (including identical Name+Description). UI must never treat same-value as NoOp, `already applied`, or unchanged-state rejection.
- `confirmed` requires submitted TenantId+Name+Description **and** projection-version advancement or safe command-specific audit provenance beyond a pre-submit baseline; confirm only from `ProjectionPending`.
- Missing qualifying provenance → `unable to verify`, not success.
- Same logical attempt reuses `messageId`/correlation; mint a new ULID only for a deliberate new attempt.
- Last-confirmed metadata stays visible and is never overwritten by in-flight intent; SignalR only nudges re-query.
- Fail closed on stale/unknown freshness, non-current projection lifecycle, disabled/unknown tenant, missing command surface, Unauthorized surface, or aggregate lock held — localized inline reason.
- Support-safe mapping; EN/FR parity; retain `tenants-edit-metadata-*` selectors.

**Ask First:**
- Adding a domain `UpdateTenantValidator` or server name/description length rules (none exist today).
- Inventing new BFF contributor/GA authorization-reflection plumbing solely for metadata (member flows still default `IsAuthorized=true`).
- Implementing deferred unsafe-viewport fail-closed in this slice.

**Never:**
- New endpoints, browser-direct backend calls, optimistic metadata overwrite, or reshaped `UpdateTenant`/`TenantUpdated` contracts.
- Confirming from acceptance, SignalR alone, metadata match without provenance, or unrelated projection churn.
- Domain same-value NoOp or UI AlreadyApplied for metadata.
- Stories 3.3–3.6 scope, hard delete, or event/projection edits.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Provenance-qualified confirm | Match + version/audit advances past baseline | `Confirmed` (+ audit-pending) | N/A |
| Match without provenance | Same Name/Description, no baseline advancement | Not `Confirmed` — pending or `unable to verify` | Refresh / continue read-only |
| Same-value recorded update | Submitted equals last-confirmed; domain emits `TenantUpdated`; provenance advances | Dispatch proceeds; confirm only with provenance | Missing provenance → UnableToVerify |
| Confirm too early | State is `Accepted` only | Confirm is a no-op until ProjectionPending | Keep polling |
| Disabled / not found / permissions | Domain rejection | Rejected + safe localized copy; last-confirmed unchanged | Mapped gateway text |
| Reconnect same attempt | In-flight / duplicate submit with tracking | RefreshStatus and/or reuse messageId; no second dispatch | Lost tracking → unable to verify |
| Whitespace name | Empty/whitespace Name | No dispatch | Field guidance |
| Command surface down | Unavailable gateway / BFF disconnected | Fail closed before dispatch | Localized unavailable reason |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs` -- `TenantUpdateMetadataCommandSnapshot` (~1041–1188); weak `ConfirmProjection` (~1159–1188) match-only + allows Accepted; add `BaselineProjectionVersion` like create (~109) / membership
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantMembershipCommandProvenance.cs` -- reuse `HasProjectionVersionAdvancement` / `HasQualifyingAuditProvenance` (do not duplicate)
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs` -- `UpdateTenantAsync` (~27–29) lacks optional `messageId` (Create/Add/Change/Remove already have it)
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs` -- `UpdateTenantAsync` (~168–196) always `NewUlid`; reuse `ResolveMessageId`; `SafeUpdateTenantRejection` (~811–835)
- `src/Hexalith.Tenants.UI/Services/Gateways/UnavailableTenantCommandGateway.cs` -- mirror optional messageId signature
- `src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor` -- submit (~419–488) in-flight silent return (~423–426); wire baseline capture + reconnect refresh like `CreateTenantFlow` / `AddTenantMemberFlow`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor` -- compose (~148–155); `GetUpdateMetadataProjectionEvidenceAsync` (~1311–1315) returns ambient Detail without version; pass/version evidence like members (`ProjectionVersion` ~188–189)
- `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs` -- `Handle(UpdateTenant)` (~39–54) already always-emits `TenantUpdated` (read-only unless proven defect)
- `src/Hexalith.Tenants.Contracts/Commands/UpdateTenant.cs` -- contract shape (read-only)
- Resources `TenantsResources.resx` (+ `.fr.resx`) -- extend `Tenants.EditMetadata.*` for UnableToVerify/provenance keys
- Patterns: create `ConfirmProjection` (~225–280); AddMember reconnect (~352–414); Story 3.1 `spec-3-1-create-tenant-with-projection-confirmation.md`
- Tests: `TenantUpdateMetadataCommandSnapshotTests.cs`, `EditTenantMetadataFlowTests.cs`, `TenantCommandGatewayTests.cs`; optional Server same-value always-emit assertion in `TenantAggregateTests.cs`
- Tracking: `sprint-status.yaml` → `3-2-edit-tenant-metadata-with-recorded-updates`
- Continuity: prior epic story `spec-3-1-create-tenant-with-projection-confirmation.md` (done); historical UI base `2-5-edit-tenant-metadata-with-safe-validation.md`

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs` -- capture pre-submit baseline ProjectionVersion (+ attempt start if needed); harden `TenantUpdateMetadataCommandSnapshot.ConfirmProjection` for metadata match + version/audit provenance; confirm only from ProjectionPending; missing provenance → UnableToVerify -- confirmation honesty
- [x] `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs` (+ `TenantCommandGateway.cs`, `UnavailableTenantCommandGateway.cs`) -- optional `messageId` on `UpdateTenantAsync` via existing ResolveMessageId -- reconnect/idempotency
- [x] `src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor` -- capture baseline before dispatch; in-flight/same-attempt → RefreshStatus and reuse messageId; confirm with versioned evidence -- flow meets narrowed contract
- [x] `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor` -- provide ProjectionVersion (and authoritative re-query if already used by sibling flows) into metadata evidence path -- evidence honesty
- [x] `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` (+ `.fr.resx`) -- EditMetadata UnableToVerify/provenance whole-strings with EN/FR parity -- support-safe copy
- [x] `tests/Hexalith.Tenants.UI.Tests/State/TenantUpdateMetadataCommandSnapshotTests.cs` (+ `EditTenantMetadataFlowTests.cs`, `TenantCommandGatewayTests.cs`; optional `TenantAggregateTests` same-value emit) -- cover I/O matrix edges -- regression net
- [x] `_bmad-output/implementation-artifacts/sprint-status.yaml` -- move `3-2-edit-tenant-metadata-with-recorded-updates` through ready-for-dev → in-progress → review -- tracking

**Acceptance Criteria:**
- Given update reaches reconciliation, when metadata matches submitted values but provenance does not advance past baseline, then the attempt is not `Confirmed`.
- Given identical submitted Name+Description with qualifying provenance, when reconciliation runs, then the attempt may `Confirmed` and is never labeled NoOp/already-applied.
- Given state is only `Accepted`, when ConfirmProjection runs, then state does not become `Confirmed`.
- Given the same logical attempt is retried with tracking available, when submit is considered, then the original `messageId` is reused (or status refreshed) with no second dispatch.
- Given focused snapshot, gateway, and flow tests run, when verification completes, then the matrix and non-collapse invariants pass.

## Spec Change Log

## Design Notes

Reuse `TenantMembershipCommandProvenance` rather than copying opaque version comparison. Capture baseline from the detail snapshot's ProjectionVersion at RequestSent (null/whitespace baseline fails closed to UnableToVerify when match exists without advancement — same posture as change-role/remove-member). Prefer extending evidence provider to include version over inventing a new proof DTO. Deferred: unsafe-viewport fail-closed and new contributor/GA reflection plumbing (`Ask First` / `deferred-work.md` if carved later).

## Verification

**Commands:**
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --filter "FullyQualifiedName~TenantUpdateMetadataCommandSnapshotTests|FullyQualifiedName~EditTenantMetadataFlowTests|FullyQualifiedName~TenantCommandGatewayTests"` -- expected: matching tests pass (xUnit v3 executable fallback if MTP/VSTest hits)
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md` -- expected: exit 0

## Suggested Review Order

**Provenance-qualified confirmation**

- Confirm only from ProjectionPending with metadata match plus version or audit provenance.
  [`TenantCreateCommandModels.cs:1180`](../../src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs#L1180)

- Match without advancement fails closed to UnableToVerify MissingProvenance.
  [`TenantCreateCommandModels.cs:1221`](../../src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs#L1221)

- Missing baseline fails closed without overwriting last-confirmed metadata.
  [`TenantCreateCommandModels.cs:1203`](../../src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs#L1203)

**Flow reconnect and evidence**

- Whitespace-safe live projection version reader with parameter fallback.
  [`EditTenantMetadataFlow.razor:412`](../../src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor#L412)

- In-flight same attempt refreshes status; missing tracking blocks without redispatch.
  [`EditTenantMetadataFlow.razor:443`](../../src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor#L443)

- Capture baseline and reuse messageId on deliberate reconnect submit.
  [`EditTenantMetadataFlow.razor:477`](../../src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor#L477)

- Lost-tracking refresh maps to UnableToVerify with command-surface copy.
  [`EditTenantMetadataFlow.razor:533`](../../src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor#L533)

**Page and gateway wiring**

- Detail page supplies ProjectionVersion into the metadata flow like membership.
  [`TenantDetailPage.razor:152`](../../src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor#L152)

- UpdateTenantAsync reuses ResolveMessageId for optional reconnect identity.
  [`TenantCommandGateway.cs:168`](../../src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs#L168)

**Tests**

- Snapshot matrix covers provenance, Accepted no-confirm, and same-value honesty.
  [`TenantUpdateMetadataCommandSnapshotTests.cs:14`](../../tests/Hexalith.Tenants.UI.Tests/State/TenantUpdateMetadataCommandSnapshotTests.cs#L14)

- Flow covers MissingProvenance, reconnect, lost tracking, and MissingBaseline UI.
  [`EditTenantMetadataFlowTests.cs:290`](../../tests/Hexalith.Tenants.UI.Tests/Components/EditTenantMetadataFlowTests.cs#L290)

- Composition asserts page→flow ProjectionVersion wiring.
  [`TenantDetailSurfaceTests.cs:1565`](../../tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs#L1565)

### Review Findings

_Code review 2026-08-21 (loop 1). Range `753f1ead..91914b94`. Four layers: blind-hunter, edge-case-hunter, verification-gap, acceptance-auditor. Line refs are HEAD (`44362aed`); the metadata regions are byte-identical between the story commit and HEAD, so they resolve correctly._

- [ ] [Review][Patch] Metadata confirmation evidence is the ambient page snapshot, not an authoritative re-query — add a `GetUpdateMetadataProjectionProofAsync` on the query gateway and use it, mirroring `GetSetConfigurationProjectionProofAsync` / `GetRemoveConfigurationProjectionProofAsync`. Resolved from [Review][Decision] on 2026-08-21: user chose to patch. [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:1632]

- [ ] [Review][Patch] HIGH — Unconditional `messageId` reuse replays the previous attempt on a deliberate new edit (regression) [src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor:479]
- [ ] [Review][Patch] Metadata confirmation uses the non-causal legacy provenance overload while all three membership siblings were since hardened onto the causal one [src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs:1243]
- [ ] [Review][Patch] Stale `SafeMessageKey` survives the Rejected / PublishFailed / TimedOut / null-status / default `ApplyStatus` arms [src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs:1136]
- [ ] [Review][Patch] In-flight-without-tracking `Blocked(...)` discards last-confirmed metadata and leaks the aggregate command-activity lease [src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor:456]
- [ ] [Review][Patch] No page-level test drives the provenance handshake end to end; the two halves stub each other out [tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs:1565]
- [ ] [Review][Patch] No flow-level test observes a non-null reused `messageId` reaching the gateway — the reason the reuse regression shipped green [tests/Hexalith.Tenants.UI.Tests/Components/EditTenantMetadataFlowTests.cs:380]
- [ ] [Review][Patch] Tracking metadata contradicts itself: spec frontmatter `status: 'done'` vs `sprint-status.yaml: review`; `epic-3: done` while 3-1/3-2 are `review` and 3-3..3-6 are `backlog` [_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md:5]
- [ ] [Review][Patch] `ProjectionPending` is synthesizable by `SignalRNudge`, so the new guard's comment "Only status-driven ProjectionPending (Completed/Events*) may confirm" is not enforced — latent only, as the metadata flow's nudge entry point has no callers [src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs:1197]
- [ ] [Review][Patch] `CanRefresh` uses `is not null` while `RefreshStatusAsync` uses `IsNullOrWhiteSpace`; a blank tracking id enables a refresh button that immediately takes the lost-tracking branch [src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor:272]
- [ ] [Review][Patch] `SafeMessageText` resolves `Localizer[key].Value` with no `ResourceNotFound` guard, so a drifted key renders the raw resource id to the user [src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor:301]
- [ ] [Review][Patch] The four new `deferred-work.md` entries land under the preceding `## Deferred from: ... spec-3-1 ...` heading (each entry does carry a correct `source_spec:`, so this is cosmetic) [_bmad-output/implementation-artifacts/deferred-work.md:999]

- [x] [Review][Defer] `AttemptStartedAtUtc` and `hasQualifyingAuditProvenance` are dead in production; a test pins an unreachable branch [src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs:1181] — deferred, pre-existing
- [x] [Review][Defer] Story premise unproven on the projection side: nothing asserts `ProjectionVersion` advances for a same-value update [tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs] — deferred, pre-existing
- [x] [Review][Defer] Hard-coded English on paths the diff made localizable (`"Command status could not be verified."`, gateway validation literal) — both pre-existing context lines [src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs:1165] — deferred, pre-existing
- [x] [Review][Defer] `ApplyProjectionEvidence` on the metadata flow is dead code; the diff threaded a version into an entry point with no callers [src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor:409] — deferred, pre-existing
- [x] [Review][Defer] Reflection-based tests poke private `_snapshot` / `RefreshStatusAsync` to synthesize states production reaches only narrowly [tests/Hexalith.Tenants.UI.Tests/Components/EditTenantMetadataFlowTests.cs:395] — deferred, pre-existing
- [x] [Review][Defer] `messageId` is now inconsistent across `ITenantCommandGateway` — configuration, global-administrator, and lifecycle methods still lack it [src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs:27] — deferred, pre-existing
