---
title: 'Create Tenant with Projection Confirmation'
type: 'feature'
created: '2026-08-08'
status: 'done'
baseline_commit: 'fa5ca5596f4a627713dce9ea712a0de81cf670bf'
review_loop_iteration: 1
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

### Review Findings

Code review 2026-08-21 (loop 1). Layers: blind-hunter, edge-case-hunter, verification-gap, acceptance-auditor — all four completed. Reviewed diff `fa5ca55..753f1ead`; every finding below re-verified against `HEAD` (44362ae) source, not the diff hunk alone.

**Decision needed** — all 5 resolved 2026-08-21; each is now a patch or a deferral below.

| # | Decision | Resolution |
|---|----------|------------|
| D1 | Vacuous provenance gate (detail-vs-list version, inequality overload) | **Adopt full membership posture** — add `HasCommandEventEvidence`, set it from `ApplyStatus`, switch to the causal 3-arg overload with same-projection baselines → patch |
| D2 | No deliberate-new-attempt path; `Blocked` wipes tracking | **Auto-reset on terminal state** — a differing submit after Confirmed/Rejected/Failed starts a fresh attempt; only a non-terminal in-flight attempt blocks → patch |
| D3 | `messageId` reuse is dead code; reconnect double-dispatches | **Persist tracking and reuse on retry** — store attempt tracking and pass `_snapshot.MessageId` on re-dispatch → patch |
| D4 | First-tenant / stale copy authored but never rendered | **Wire both keys into `UnavailableReason`** and update the three workspace tests to assert the specific copy → patch |
| D5 | Authorization reflection not threaded into create availability | **Deferred to Story 3.3** — 3.3 is scoped exactly as the fail-closed availability guardrail; server/API remains the enforcement boundary, so this is UI honesty rather than a security hole |

**Patch**

- [x] [Review][Patch] [D1] Adopt the membership provenance posture for create: add `HasCommandEventEvidence`, set it in `ApplyStatus` (EventsStored/EventsPublished, or Completed with `EventCount > 0`), and switch `ConfirmProjection` to the causal 3-arg `HasProjectionVersionAdvancement` with same-projection baselines [src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs:258]
- [x] [Review][Patch] [D2] Auto-reset tracking once the attempt is terminal so a differing submit starts a fresh attempt; only a non-terminal in-flight attempt blocks [src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor:308]
- [x] [Review][Patch] [D3] Persist attempt tracking across the circuit and pass `_snapshot.MessageId` on re-dispatch; a lost-tracking retry resolves to `unable to verify` rather than dispatching again [src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor:349]
- [x] [Review][Patch] [D4] Wire `Tenants.Create.Availability.Stale` and `.FirstTenantUnknown` into `UnavailableReason` and update the three workspace tests to assert the specific copy [src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor:178]
- [x] [Review][Patch] Indeterminate baseline-absence is recorded as "tenant present" and the command is dispatched anyway [src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor:428]
- [x] [Review][Patch] Detail evidence is trusted without checking its surface kind or freshness [src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs:247]
- [x] [Review][Patch] A failed evidence read collapses terminal Rejected/Degraded into UnableToVerify; the evidence block also runs on every refresh [src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor:401]
- [x] [Review][Patch] `Blocked` is a static factory, so the tracking guard destroys the tracking it protects and disables the recovery its copy names [src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs:121]
- [x] [Review][Patch] Missing test: the workspace→flow provenance wiring is unpinned — deleting `BaselineProjectionVersion` keeps the whole suite green [tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs:563]
- [x] [Review][Patch] A page past the end of a non-empty list reads as an authoritatively empty first-tenant list (`RequestCursor` never consulted) [src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:411]
- [x] [Review][Patch] First-tenant exception ignores `TenantListSnapshot.Lifecycle`, so Rebuilding/Unavailable/LocalOnly still opens create [src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:411]
- [x] [Review][Patch] An empty-string description can never confirm — a successful create stays ProjectionPending forever [src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs:243]
- [x] [Review][Patch] `SignalRNudge` still promotes RequestSent/Accepted into ProjectionPending — the only state the new gate trusts (membership siblings had this removed) [src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs:217]
- [x] [Review][Patch] `ApplyStatus` never clears `SafeMessageKey`, so a stale provenance reason renders under a later state [src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs:164]
- [x] [Review][Patch] The `UnableToVerify` path writes unproven evidence into the `LastConfirmed*` fields [src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs:280]
- [x] [Review][Patch] A `TenantAlreadyExists` rejection sets FocusTarget=Refresh while `CanRefresh` is false, moving focus to a disabled control [src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor:366]
- [x] [Review][Patch] The new provenance message renders outside the aria-live region, which announces only the state text [src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor:118]
- [x] [Review][Patch] Missing test: Accepted + matching evidence + advanced version must not confirm — restoring the old gate keeps the suite green [tests/Hexalith.Tenants.UI.Tests/State/TenantCreateCommandSnapshotTests.cs:39]
- [x] [Review][Patch] The retry test asserts `StatusCallCount` outside `WaitForAssertion` and never asserts messageId reuse — the AC4 test does not test AC4 [tests/Hexalith.Tenants.UI.Tests/Components/CreateTenantFlowTests.cs:280]
- [x] [Review][Patch] `Localizer[key].Value` has no `ResourceNotFound` guard, so a raw resource key can render on a support-safe surface [src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor:216]
- [x] [Review][Patch] Spec metadata is self-contradictory: frontmatter `status: 'done'` vs sprint-status `review`, `review_loop_iteration: 0` with an empty Spec Change Log despite a recorded 2026-08-08 review, and Verification command 2 is not reproducible as written (exit 1, not 0 — the 7 UNDECLARED bumps belong to later `build(deps)` commits, not to 3.1, whose own commit is gitlink-clean) [_bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md:5]
- [x] [Review][Patch] A deferred-work evidence line misstates provenance — this story introduced the workspace-side `IsCommandSurfaceConnected` lookup and the parameter binding, not just "threaded the existing flag" [_bmad-output/implementation-artifacts/deferred-work.md:31]
- [x] [Review][Patch] The create stub declares two all-optional `CreateTenantAsync` overloads; the non-interface one is dead and makes a single-argument call ambiguous [tests/Hexalith.Tenants.UI.Tests/Components/CreateTenantFlowTests.cs:398]

**Deferred**

- [x] [Review][Defer] [D5] Create availability derives `IsAuthorized` from the list surface kind, not from `GlobalAdministratorsAuthorizationReflection`, so an `Indeterminate` reflection leaves create enabled [src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:155] — deferred to Story 3.3: 3.3 is scoped exactly as the fail-closed availability guardrail; server/API remains the enforcement boundary, so this is UI honesty rather than a security hole
- [x] [Review][Defer] `AttemptStartedAtUtc` ships on a public record but is never read, and defaults via `DateTimeOffset.UtcNow` rather than an injected clock [src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs:111] — deferred, the audit-provenance branch it feeds is already recorded as deferred work
- [x] [Review][Defer] `ApplyProjectionEvidence` has no callers and bypasses `SetSnapshot`, so it would not honour the assertive-focus rule if wired [src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor:268] — deferred, belongs with the deferred SignalR nudge wiring
- [x] [Review][Defer] Baseline and evidence versions are read from different snapshot lineages (`_snapshot` vs `_lastConfirmedSnapshot`), so a failed post-create reload makes a genuinely successful create report UnableToVerify [src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:965] — deferred, fail-closed direction and entangled with the provenance-gate decision above
- [x] [Review][Defer] `IsCommandSurfaceConnected` is a render-time service lookup with no subscription, duplicating an existing resolution in the same component [src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:418] — deferred, pre-existing composition pattern already recorded in deferred-work.md

**Dismissed as noise (2)**

- Missing ULID validation on a caller-supplied `messageId` — true at `753f1ea`, already fixed at HEAD by `TryResolveMessageId` (`TenantCommandGateway.cs:944`, landed in `d3f74f5`).
- `epic-3-context.md` numeric constraints lack a traceable source — out of scope for a code review of this story's implementation.

## Spec Change Log

- 2026-08-21 -- Code review loop 1. Four layers ran (blind-hunter, edge-case-hunter, verification-gap,
  acceptance-auditor); 30 findings triaged to 5 decision-needed, 23 patch, 5 defer, 2 dismissed. All five
  decisions were resolved by the story owner and all 23 patches applied. The provenance gate was rebuilt on
  the membership posture (`HasCommandEventEvidence` + causal advancement + like-for-like projection
  baselines), the tracking guard now preserves its handle and auto-resets on a terminal attempt, attempt
  handles are retained per circuit, and the first-tenant/stale copy this story authored is now rendered.
  D5 (authorization reflection in create availability) was deferred to Story 3.3. UI suite 2117/2117.

## Design Notes

Create usually has no pre-submit tenant. Capture `BaselineTenantAbsent` + list `ProjectionVersion` (may be null on true first-tenant empty). Confirm only when authoritative evidence shows literal TenantId + submitted Name (+ Description via `GetTenant` when verifying full metadata) **and** either list/detail version advances past a non-empty baseline, or null-baseline first appearance from proven-absent baseline with a non-empty post-create version / qualifying audit provenance. If the tenant was present at baseline, never Confirm.

Prefer extending `TenantMembershipCommandProvenance` over duplicating opaque comparison. Do not add domain max-length validators unless Ask First approves. Deferred: AggregateIdentity lock, SignalR nudge wiring, mobile fail-closed / open-existing CTAs (`deferred-work.md`).

## Verification

**Commands:**
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --filter "FullyQualifiedName~TenantCreateCommandSnapshotTests|FullyQualifiedName~CreateTenantFlowTests|FullyQualifiedName~TenantCommandGatewayTests"` -- expected: matching tests pass (xUnit v3 executable fallback if MTP/VSTest hits)
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md` -- actual: exit 1, 7 UNDECLARED `references/` pointer moves. Not attributable to this story: `git diff --stat fa5ca55 753f1ead -- references/` is empty, so the story's own commit is gitlink-clean. The bumps belong to later commits in the widening `baseline_commit..HEAD` window -- `f5249d8`, `da06403`, `dd521d9`, `020b099a`, `de0881b4` are proper `build(deps)` commits; `d3f74f5` (`test:`), `cff62ce`/`5a2b90d` (`fix:`) and `acab0b5`/`40b14f8` (`build: sync`) bundled bumps into non-`build(deps)` commits. Re-run against `fa5ca55..753f1ead` to verify this story in isolation.

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
