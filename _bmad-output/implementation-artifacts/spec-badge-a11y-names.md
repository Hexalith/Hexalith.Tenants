---
title: 'Remove duplicate accessible names from tenant badges'
type: 'bugfix'
created: '2026-09-06'
status: 'done'
baseline_revision: 'f75cdacc8eca458778c7109fd3f713f8907bed02'
baseline_commit: 'f75cdacc8eca458778c7109fd3f713f8907bed02'
review_loop_iteration: 0
followup_review_recommended: true
context:
  - 'references/Hexalith.AI.Tools/hexalith-ux-instructions.md'
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** The tenant-grid status and pending badges and the shared truth-state badge give their icon the same accessible label already supplied by the badge host, creating redundant accessible-name sources that can produce duplicate announcements if the host semantics change.

**Approach:** Remove `IconLabel` from only those three badge renderings. Keep each existing localized visible state string as the host `aria-label`, making it the single authoritative programmatic name while leaving the icon decorative.

## Boundaries & Constraints

**Always:** Preserve the existing localized state-only label, visible text, `aria-label`, roles, icons, colors, appearances, CSS classes, test IDs, and state selection logic. Prove on the rendered Fluent component that `IconLabel` is absent and that the non-empty host `aria-label` exactly equals trimmed visible text.

**Never:** Do not change `ProjectionLifecycleBadge` or any other badge type, localization resources, state vocabulary, styling, component dependencies, or the deferred-work ledger. Do not replace Fluent UI components or add custom markup/CSS/JavaScript.

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Tenant status badge | A rendered tenant row in any localized status | The visible localized status and host `aria-label` are identical and non-empty; `IconLabel` is unset | Existing fallback status mapping remains unchanged |
| Tenant pending badge | A rendered tenant row in any localized pending state | The visible localized pending state and host `aria-label` are identical and non-empty; `IconLabel` is unset | Existing unknown-state mapping remains unchanged |
| Truth-state badge | A durable freshness state or transient refreshing state | The visible localized freshness and host `aria-label` are identical and non-empty; `IconLabel` is unset; refreshing retains `role="status"` | Existing unknown freshness mapping remains unchanged |

</intent-contract>

## Code Map

- `src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor:60` -- Status badge; remove only the `IconLabel` assignment while retaining `StatusLabel(...)` for visible text and the host name.
- `src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor:93` -- Pending badge; apply the same single-name treatment without changing pending-state semantics.
- `src/Hexalith.Tenants.UI/Components/Shared/TruthStateBadge.razor:4` -- Shared freshness badge; remove only `IconLabel`; `Label` remains the localized visible and host-accessible value and the refreshing role remains conditional.
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs:69` -- Existing status/pending component assertions are the regression seam for icon decoration and host/text name equality.
- `tests/Hexalith.Tenants.UI.Tests/Components/TruthStateBadgeTests.cs:20` -- Existing durable and refreshing cases cover all truth-state label paths and can assert a null icon label plus authoritative host name.
- `src/Hexalith.Tenants.UI/Components/Shared/ProjectionLifecycleBadge.razor:4` -- Read-only out-of-scope control; retain its current `IconLabel` and existing tests.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` and `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx` -- Read-only localized values already used by visible text and host `aria-label`.
- `_bmad-output/implementation-artifacts/deferred-work.md` -- Read-only ledger; orchestration owns resolution recording.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor` -- Remove `IconLabel` from the status and pending badges only so their icons are decorative.
- [x] `src/Hexalith.Tenants.UI/Components/Shared/TruthStateBadge.razor` -- Remove `IconLabel` from the shared truth-state badge only.
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs` -- Replace duplicate-label expectations with assertions that both grid badge icons are unnamed and each localized host name is non-empty and equals visible text.
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/TruthStateBadgeTests.cs` -- Assert the same invariant for durable and refreshing truth-state cases while retaining role, icon, color, and lifecycle-boundary checks.

**Acceptance Criteria:**
- Given a rendered in-scope badge, when its accessibility properties are inspected, then `IconLabel` is null, its host `aria-label` is non-empty localized text, and that label equals its trimmed visible state text.
- Given any in-scope status, pending, freshness, or refreshing path, when the change is rendered, then the existing state-only string, role, icon, color, appearance, CSS class, and test ID remain unchanged.
- Given `ProjectionLifecycleBadge` and all other badge types, when the bundle is complete, then their implementation and established behavior remain unchanged.
- Given the deferred-work ledger, when the bundle is complete, then no ledger content has been edited.

## Spec Change Log

## Review Triage Log

### 2026-09-06 — Review pass
- verdicts: 17 findings — high 0, medium 6, low 2, false 9, maybe-false 0
- findings:
  - `[false]` `[reject]` Projection lifecycle retains the duplicate-label pattern — the invocation intent explicitly says not to extend this bundle to `ProjectionLifecycleBadge`, and its implementation remained unchanged.
  - `[medium]` `[patch]` Grid assertions no longer pinned exact status and pending labels — added exact localized-name assertions through the complete three-status by two-pending-state matrix.
  - `[medium]` `[patch]` Active, Unknown, and None grid paths lacked direct accessibility coverage — added six matrix cases covering every defined status/pending combination.
  - `[low]` `[reject]` Truth-state tests use only the default resource prefix and an English stub — prefix selection and shipped resource files are unchanged, existing surface/resource tests cover the alternate callers, and expanding every locale/prefix combination is disproportionate to this unconditional parameter deletion.
  - `[medium]` `[patch]` Null `IconLabel` alone did not prove the rendered icon is decorative — added rendered-SVG assertions for `aria-hidden="true"`, no `aria-label`, and no `<title>`.
  - `[false]` `[reject]` Appearance, classes, and test IDs were not newly asserted everywhere — the source diff deletes only `IconLabel`; those properties are visibly unchanged and existing selectors/color/icon/class assertions still exercise them.
  - `[false]` `[reject]` Verification should run the full UI assembly — the directly affected badge classes compile and their 121 focused cases exercise the shared component and every grid state combination; no downstream API or branching behavior changed.
  - `[false]` `[reject]` A global Fluent default could repopulate truth-state `IconLabel` — package inspection shows `FluentBadge.IconLabel` is a nullable ordinary parameter initialized to null, with no configuration-derived default.
  - `[false]` `[reject]` A global Fluent default could repopulate grid `IconLabel` — the same package implementation passes only the component's nullable parameter to its icon, so omission leaves it null.
  - `[false]` `[reject]` Blank or padded truth-state localization could violate the host/text invariant — all tracked in-scope resource values and test localizer values are non-blank and unpadded, and this change does not alter localization lookup.
  - `[false]` `[reject]` Blank or padded grid localization could violate the host/text invariant — the tracked status/pending resources are non-blank and unpadded, and both host and visible text retain the same unchanged lookup.
  - `[false]` `[reject]` An undefined freshness enum could expose a resource key — production gateways normalize freshness through closed switch mappings to Current, Stale, Aging, or Unknown; no reachable caller supplies an undefined value.
  - `[false]` `[reject]` An undefined tenant status could expose a resource key — `TenantStatusJsonConverter` maps absent, non-string, and unrecognized values to `Unknown`; no reachable grid caller supplies an undefined value.
  - `[medium]` `[patch]` A Disabled status could be mislabeled Active without the replacement test failing — the new matrix pins exact visible and host names for Unknown, Active, and Disabled.
  - `[medium]` `[patch]` Tests observed a component/DOM proxy instead of icon decoration — rendered icon assertions now verify the actual light-DOM SVG is hidden and unnamed for every in-scope badge.
  - `[medium]` `[patch]` Status and pending coverage was narrower than the intent — the new six-case matrix exhausts the defined status and pending state sets.
  - `[low]` `[reject]` Localization equality was not rerun across locale variants — localization resources and selection logic are unchanged, the structural invariant is locale-independent, and adding a cross-locale renderer matrix would not materially strengthen this narrow deletion.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release` -- expected: UI production and test code compile with warnings treated as errors.
- `dotnet tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests.dll -class Hexalith.Tenants.UI.Tests.Components.TruthStateBadgeTests` -- expected: all truth-state and lifecycle-boundary component tests pass.
- `dotnet tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests.dll -class Hexalith.Tenants.UI.Tests.Components.TenantListSurfaceTests` -- expected: tenant-grid component tests pass, including the status and pending badge accessibility assertions.
- `git diff --check` -- expected: no whitespace errors.

## Auto Run Result

Status: done

Summary: Removed redundant `IconLabel` values from the tenant-grid status and pending badges and the shared truth-state badge. Their existing localized visible text and identical host `aria-label` remain authoritative, while regression tests now verify the rendered icon is hidden and unnamed.

Files changed:
- `src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor` -- removed icon labels from the status and pending badges only.
- `src/Hexalith.Tenants.UI/Components/Shared/TruthStateBadge.razor` -- removed the truth-state icon label only.
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs` -- added exhaustive status/pending name coverage and rendered decorative-icon assertions.
- `tests/Hexalith.Tenants.UI.Tests/Components/TruthStateBadgeTests.cs` -- asserted authoritative host/text equality and decorative rendered SVGs for durable and refreshing states.
- `_bmad-output/implementation-artifacts/spec-badge-a11y-names.md` -- recorded the plan, verification, review triage, and result.

Review findings:
- Patches applied: 2 grouped entries — high 0, medium 2, low 0, maybe-false 0. Restored exact/exhaustive status-pending name coverage and added rendered-SVG decoration checks.
- Items deferred: 0.
- Rejected: `ProjectionLifecycleBadge` expansion because the intent explicitly excludes it; alternate-prefix and cross-locale matrices because unchanged resource selection plus existing resource/surface coverage make them disproportionate; exhaustive reassertion of unchanged appearance/classes/test IDs and a full-suite run because the deletion changes no related API or branch; global-default concerns because Fluent's nullable `IconLabel` has no configured default; blank/padded-resource concerns because tracked values are valid and unchanged; invalid freshness/status concerns because production mappings and converters normalize to closed known values.

Follow-up review recommendation: true. Two medium review entries were patched; a follow-up should independently verify that the new state matrix and rendered-SVG assertions faithfully exercise the final accessibility surface.

Verification performed:
- Release build succeeded with 0 warnings and 0 errors.
- `TruthStateBadgeTests`: 13 passed, 0 failed/skipped/not-run.
- `TenantListSurfaceTests`: 108 passed, 0 failed/skipped/not-run.
- Matrix audit: all tenant status/pending combinations plus every durable truth state and transient refreshing state ran and passed.
- `git diff --check` and `git diff --cached --check` passed.
- Package inspection confirmed non-focusable Fluent badge icons render `aria-hidden="true"` and omit `<title>` when `IconLabel` is null.

Residual risks: No known implementation defect remains. The follow-up recommendation covers the review-driven test changes themselves.
