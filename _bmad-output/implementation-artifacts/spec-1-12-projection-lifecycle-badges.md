---
title: 'Story 1.12: Projection Lifecycle Badges'
type: 'feature'
created: '2026-07-31'
status: 'review'
baseline_commit: '25bdff0'
final_revision: '33abe27'
review_loop_iteration: 0
followup_review_recommended: true
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
command availability gates.

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

## File List

Declared by this story alone:

- `src/Hexalith.Tenants.UI/Components/Shared/ProjectionLifecycleBadge.razor`
- `src/Hexalith.Tenants.UI/Components/Shared/ProjectionLifecycleStatus.razor`
- `src/Hexalith.Tenants.UI/Components/Shared/TruthStateBadge.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditDataGrid.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleActionAvailability.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor`
- `src/Hexalith.Tenants.UI/Components/Users/MyTenantsDataGrid.razor`
- `tests/Hexalith.Tenants.UI.Tests/Components/EditTenantMetadataFlowTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantLifecycleActionAvailabilityTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TruthStateBadgeTests.cs`

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

- `references/Hexalith.Builds`
- `references/Hexalith.EventStore`
- `references/Hexalith.Memories`

Their in-range endpoints are `Builds 61e43b1 -> b529b66`, `EventStore bb4c81d -> e4618d9` and
`Memories 4a6f0d3 -> a1f64d5`.

Story 1.12's own commit `33abe27` moves **no** submodule pointer — `git show --stat --name-only 33abe27`
returns no `references/` path. These three moved in `b045129` ("build(deps): bump Builds and EventStore
submodules") and `a49d793`, both of which belong to Story 1.10's range and are declared and justified in
Story 1.10's File List. They appear here only because `validate-story-gitlinks.py` compares a story's
`baseline_commit` against HEAD plus the working tree, and this story's baseline (`25bdff0`) predates those
commits. They are declared here so the guard reports a deliberate, stated state rather than an undeclared
one — this story asserts their provenance, not their ownership. Verified: the validator exits 0 with these
entries present, and Story 1.10's own run also exits 0 with all six of its pointers declared.

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

**This story has not had its own review loop.** It carries the open decision recorded against Story 1.10 as
`spec:869` — resolved there by decision D-F (2026-07-31), which reversed D6, upheld the strict lifecycle gate
at all five sites and reordered the clauses so the accurate failure reason wins. The clause ordering in
`TenantLifecycleAvailability.cs` and in the four command flows now reflects that decision; a first review pass
over this story should confirm the rest of `33abe27` against these acceptance criteria.

## Review Findings — inherited from Story 1.10 review loop 8 (re-attributed 2026-07-31)

Two loop-8 items were raised against Story 1.10 before decision D4 (loop 9) and decision D-E (loop 10) split
the projection-lifecycle-badge work forward to this story key. Both name files this story declares, and both
describe behaviour `33abe27` introduced, so they are re-attributed here rather than closed under 1.10. They
are recorded unchecked: this story still owes its first review pass, and these are part of it.

- [ ] [Review][Patch] Add a test file for `ProjectionLifecycleStatus.razor` — mutating `LiveRegion` to always
  return `"polite"` survived the full suite; the component has no test file and a repo-wide grep for its name
  and for the six test-ids its call sites pass returns zero hits under `tests/`. Its sibling
  `ProjectionLifecycleBadge` is thoroughly covered, so the gap is specific to the status wrapper. Note while
  covering it that the component pairs `role="status"` with `aria-live="assertive"` — the same contradiction
  Story 1.10's loop-10 item resolved in `TenantConfigurationView` and `TenantDetailPage` by switching to
  `role="alert"`; resolve it the same way here or record why this site differs.
  [src/Hexalith.Tenants.UI/Components/Shared/ProjectionLifecycleStatus.razor:24]
- [ ] [Review][Patch] Lift the lifecycle badge out of the workspace live region — `ProjectionLifecycleBadge`
  sits inside the `role="status" aria-live="polite" aria-atomic="true"` notices stack, so any lifecycle
  transition, and any tab switch that inserts or removes it, re-announces the whole notices block verbatim.
  The same diff removes this defect from `GlobalAdministratorsPage` and `TenantAuditPage`, where the badges
  were deliberately lifted out as siblings — except that in both of those the badge was then placed inside the
  `role="@StatusRole" aria-live="@StatusLive"` state section, which is `alert` when the surface is stale, so a
  rebuild re-announces the whole state block assertively.
  [src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:178]

Story 1.10 records both as re-attributed rather than resolved; neither is closed by 1.10's completion.
