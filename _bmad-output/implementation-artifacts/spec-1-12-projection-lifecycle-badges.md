---
title: 'Story 1.12: Projection Lifecycle Badges'
type: 'feature'
created: '2026-07-31'
status: 'done'
baseline_commit: '25bdff0'
final_revision: '33abe27'
review_loop_iteration: 1
followup_review_recommended: false
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-1-10-direct-tenants-reads-and-authoritative-freshness.md'
warnings:
  - authored-after-the-fact
---

<intent-contract>

## Intent

**Problem:** Projection lifecycle was carried in the read metadata and surfaced nowhere. `TruthStateBadge`
conflated freshness with lifecycle, so a projection that was rebuilding, degraded, unavailable or local-only
rendered indistinguishably from a current one, and the command surfaces that must fail closed on a
non-current projection had no lifecycle input at all.

**Approach:** Give lifecycle its own rendering primitive (`ProjectionLifecycleBadge`) and its own live-region
wrapper (`ProjectionLifecycleStatus`), separate from the freshness badge; thread `ProjectionLifecycleState`
from the read snapshots to every surface that displays or gates on it; and add a lifecycle clause to the
command availability gates. Status mounts use `ProjectionLifecycleStatus` as a sibling of any page live
region (never nested); grid/row cells keep the bare badge. Detail facts keep the `dt`/`dd` +
`role="alert"|"status"` pattern inside the definition list.

**Authored after the fact.** This story was split out of Story 1.10 by that story's code-review loop 9
(decision D4, 2026-07-31) because commit `33abe27` "feat(ui): add projection lifecycle badges" (39 files)
landed inside 1.10's range while being declared by no story. It is already published on `main`. This spec
exists so the work has an owner, a File List and its own review loop — it is a record of shipped work, not a
plan for work to be done. Loop 10 of Story 1.10 (decision D-E, 2026-07-31) required it to exist before
either story can close.

## Boundaries & Constraints

**Always:** Keep freshness and lifecycle as independent bindings with independent badges; render the
localized lifecycle label alongside any state class, never the class alone; keep EN/FR key parity; use
Fluent/FrontComposer primitives and stable `data-testid` selectors.

**Never:** Let a lifecycle badge assert a state the metadata does not carry; let `Unknown` render as a
positive claim; put a live region inside another live region; change public backend or query contracts.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
|----------|---------------|---------------------------|
| Current lifecycle | `ProjectionLifecycleState.Current` | Lifecycle badge reads "Current"; command gates unaffected |
| Non-current lifecycle | `Stale`, `Rebuilding`, `Degraded`, `Unavailable`, `LocalOnly` | Own badge, own label, own class; mutation gates fail closed |
| Absent lifecycle header | `Unknown` | Badge reads "Unknown"; nothing claims a rebuild is under way |
| Lifecycle vs freshness | Lifecycle `Stale`, freshness `Current` | Two badges, two states, neither overwriting the other |

</intent-contract>

## Code Map

- `src/Hexalith.Tenants.UI/Components/Shared/ProjectionLifecycleBadge.razor` — the lifecycle rendering primitive.
- `src/Hexalith.Tenants.UI/Components/Shared/ProjectionLifecycleStatus.razor` — its live-region wrapper.
- `src/Hexalith.Tenants.UI/Components/Shared/TruthStateBadge.razor` — rewritten to carry freshness only.
- `src/Hexalith.Tenants.UI/State/TenantDetail/TenantLifecycleAvailability.cs` — the availability evaluator that gained the lifecycle clause.
- The four command flows and the surfaces that display the badge — see the File List.

## Tasks & Acceptance

**Acceptance Criteria:**
- Given a read whose metadata carries a projection lifecycle, when any consuming surface renders, then the
  lifecycle is shown by its own badge with its own localized label, independently of the freshness badge.
- Given a non-current projection lifecycle, when a mutation gate evaluates, then the action is unavailable
  and the reason names the condition the operator can act on.
- Given EN and FR resources, when the badge renders, then both languages carry every
  `Tenants.ProjectionLifecycle.*` key with no one-sided entry.
- Given the badge tests, when they run, then they assert the localized label and not only the state class.

### Review Findings

- [x] [Review][Patch] DECLARE four undeclared `references/` gitlink moves (+ refresh declared endpoints) — applied (loop 1, 2026-08-08)
- [x] [Review][Patch] Standardize status mounts on `ProjectionLifecycleStatus` (bare badge in grid/row cells only) — applied
- [x] [Review][Patch] Lift workspace list lifecycle out of `tenants-list-notices` — applied [`TenantsWorkspace.razor`]
- [x] [Review][Patch] Lift audit lifecycle out of assertive state live region — applied [`TenantAuditPage.razor`]
- [x] [Review][Patch] Lift user-membership lookup lifecycle out of status live region — applied [`UserMembershipLookupPanel.razor`]
- [x] [Review][Patch] Fix `ProjectionLifecycleStatus` `role`/`aria-live` contradiction (`alert` when Unavailable) — applied
- [x] [Review][Patch] Add tests for `ProjectionLifecycleStatus` — applied [`ProjectionLifecycleStatusTests.cs`]
- [x] [Review][Patch] Close lifecycle confirm dialog when `TenantId`/detail identity changes — applied
- [x] [Review][Patch] Assert non-default My Tenants row lifecycle binding — applied
- [x] [Review][Patch] Assert user-lookup snapshot/row lifecycle selectors — applied
- [x] [Review][Patch] Assert Member Access header/row lifecycle independently of freshness — applied
- [x] [Review][Patch] Assert empty My Tenants snapshot still renders lifecycle status — applied
- [x] [Review][Patch] Assert lifecycle facts header badge — applied
- [x] [Review][Patch] Assert localized lifecycle labels on global-admins and audit status badges — applied
- [x] [Review][Patch] Assert localized label on empty-list snapshot lifecycle badge — applied
- [x] [Review][Patch] Add `ProjectionLifecycleBadge.razor.css` nowrap companion — applied
- [x] [Review][Defer] Coarse `StaleData` category for every non-Current projection lifecycle — deferred, pre-existing [`src/Hexalith.Tenants.UI/State/TenantDetail/TenantLifecycleAvailability.cs:64`]
- [x] [Review][Defer] Open Set/Remove/Edit flows do not reset when lifecycle flips mid-flight (only lifecycle-action re-evals) — deferred, pre-existing pattern beyond this story's badge split [`src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor`]

## File List

Declared by this story alone:

- `src/Hexalith.Tenants.UI/Components/Shared/ProjectionLifecycleBadge.razor`
- `src/Hexalith.Tenants.UI/Components/Shared/ProjectionLifecycleBadge.razor.css`
- `src/Hexalith.Tenants.UI/Components/Shared/ProjectionLifecycleStatus.razor`
- `src/Hexalith.Tenants.UI/Components/Shared/TruthStateBadge.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditDataGrid.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleActionAvailability.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor`
- `src/Hexalith.Tenants.UI/Components/Users/MyTenantsDataGrid.razor`
- `tests/Hexalith.Tenants.UI.Tests/Components/EditTenantMetadataFlowTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/ProjectionLifecycleStatusTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantLifecycleActionAvailabilityTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TruthStateBadgeTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/UserMembershipLookupSurfaceTests.cs`

Shared with Story 1.10 — both stories changed these files in the same range and both declare them.
Corrected by Story 1.10's review loop 12 (2026-08-01): seven paths below were previously listed above as
"declared by this story alone" or omitted entirely while Story 1.10's File List also declares them, which
contradicts decision D-B/D-E's rule that a genuinely shared file is declared by both with the overlap stated.
`TenantLifecycleAvailability.cs` was the sharpest case — it is the decision D-F clause-ordering file that
Story 1.10 changed in this very range. `TenantListSurfaceTests.cs` and `TenantConfigurationView.razor.css`
were in neither of this story's lists:

- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/RemoveTenantConfigurationFlow.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/SetTenantConfigurationFlow.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/TenantConfigurationManagement.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor.css`
- `src/Hexalith.Tenants.UI/Components/Users/MyTenantsPanel.razor`
- `src/Hexalith.Tenants.UI/Components/Users/UserMembershipLookupPanel.razor`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`
- `src/Hexalith.Tenants.UI/State/TenantDetail/TenantDetailSnapshot.cs`
- `src/Hexalith.Tenants.UI/State/TenantDetail/TenantLifecycleAvailability.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/AuditEvidenceEntryPointTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/MyTenantsSurfaceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/RemoveTenantConfigurationFlowTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/SetTenantConfigurationFlowTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/State/TenantLifecycleAvailabilityTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs`

Shared with Story 1.11 — declared by both:

- `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor`
- `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs`

`references/` pointers falling inside this story's window — **declared, not owned**:

- `references/Hexalith.AI.Tools`
- `references/Hexalith.Builds`
- `references/Hexalith.Commons`
- `references/Hexalith.EventStore`
- `references/Hexalith.FrontComposer`
- `references/Hexalith.Memories`
- `references/Hexalith.PolymorphicSerializations`

In-range endpoints (authoritative for the guard; last statement wins):
`references/Hexalith.AI.Tools 859d53b -> a19a69d`,
`references/Hexalith.Builds 61e43b1 -> a8a5085`,
`references/Hexalith.Commons f2b5f1b -> 74ccb96`,
`references/Hexalith.EventStore bb4c81d -> 37fdcd1`,
`references/Hexalith.FrontComposer b6efcad -> 138a550`,
`references/Hexalith.Memories 4a6f0d3 -> f78c80c`,
`references/Hexalith.PolymorphicSerializations f3b2330 -> 5dd6aa8`.

Story 1.12's own commit `33abe27` moves **no** submodule pointer — `git show --stat --name-only 33abe27`
returns no `references/` path. These pointers moved in later/other story ranges after baseline `25bdff0`.
They are declared here so `validate-story-gitlinks.py` (baseline → HEAD + working tree) reports a
deliberate, stated state rather than an undeclared one — this story asserts their provenance, not their
ownership. Review loop 1 (2026-08-08) expanded the declaration from three paths to all seven in-window
moves after the guard failed on AI.Tools, Commons, FrontComposer, and PolymorphicSerializations.

**Boundary note on `TenantListSurfaceTests.cs`.** Story 1.10's decision D-E (2026-07-31) settled this file
explicitly: its badge-era content belongs to this story, but the change made in Story 1.10's loop-10 range —
the `NotModifiedWithoutSnapshot` remark and the lifecycle-badge label assertions — is 1.10 transport and
evidence work and is declared there. The file therefore appears in **both** File Lists, and each story owns
the part it changed. `references/` gitlinks are declared by Story 1.10 for its range; this story moves none.

## Verification

Inherited from Story 1.10's loop-10 verification run against the same tree (2026-07-31):
`dotnet test tests/Hexalith.Tenants.UI.Tests` PASS; Release solution build with `-warnaserror` 0/0; EN/FR
parity 1,223 keys each. Loop 10 also added the localized-label assertions this story's AC4 requires, in
`TenantListSurfaceTests`, `GlobalAdministratorsPageTests` and `TenantDetailSurfaceTests`.

**Review loop 1 (2026-08-08) applied.** Status mounts now use `ProjectionLifecycleStatus` (sibling live
regions; `role="alert"` when Unavailable); verification gaps for My Tenants / user-lookup / members /
facts header / empty-list labels were closed; gitlinks declared for all seven in-window submodule moves.
Pre-existing Domain UI conformance failures (div/span budget 221>220; GlobalAdministrators
`display: inline-flex` without `fc-css-exception`) remain on `main` and are outside this story's patches.
Focused lifecycle tests PASS; full UI suite 1934/1936 with those two pre-existing governance failures.

## Review Findings — inherited from Story 1.10 review loop 8 (re-attributed 2026-07-31)

Two loop-8 items were raised against Story 1.10 before decision D4 (loop 9) and decision D-E (loop 10) split
the projection-lifecycle-badge work forward to this story key. Both name files this story declares, and both
describe behaviour `33abe27` introduced, so they are re-attributed here rather than closed under 1.10. They
are recorded unchecked: this story still owes its first review pass, and these are part of it.

- [x] [Review][Patch] Add a test file for `ProjectionLifecycleStatus.razor` — re-recorded unchecked under
  `### Review Findings` (loop 0, 2026-08-08) together with the `role`/`aria-live` fix.
  [src/Hexalith.Tenants.UI/Components/Shared/ProjectionLifecycleStatus.razor:24]
- [x] [Review][Patch] Lift the lifecycle badge out of the workspace live region — re-recorded unchecked under
  `### Review Findings` (loop 0, 2026-08-08); audit and user-lookup nesting added there as well. Global-admins
  nesting from `33abe27` is already lifted at HEAD.
  [src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:178]

Story 1.10 records both as re-attributed rather than resolved; neither is closed by 1.10's completion.
