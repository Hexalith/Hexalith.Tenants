---
title: 'DW-112: Consistent scope-aware status-filter labeling'
type: 'bugfix'
created: '2026-10-15'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_revision: '51f404513f2657e98f949f58f0df43b99b68a7d0'
baseline_commit: '51f404513f2657e98f949f58f0df43b99b68a7d0'
---

<frozen-after-approval reason="user-owned implementation request">

## Intent

Make the tenant workspace status selector's visible label and accessible name
describe the same filtering scope, using its existing localized resource contract.
Prove ordinary and authoritative modes, transitions, and EN/FR rendering including
the browser-computed accessible name. Close only DW-112 after independent review
and applicable UI/browser validation; preserve inherited work and release blockers.

</frozen-after-approval>

## Implementation Notes

- Pre-implementation ownership: Hexalith.Tenants, `main`, full HEAD
  `51f404513f2657e98f949f58f0df43b99b68a7d0`. The user's explicit preservation
  instructions authorize continuing in the inherited dirty tree and override
  workflow dirty-tree/commit steps. No sprint/story status changes are needed.
  Entry branches, HEADs, diffs, indexes, remotes, history, root gitlinks and nested
  initialization states are in `dw-112-entry-state.json`; exact entry bytes are
  retained for preservation comparison. External EventStore checkout remains
  `241652627a52e47c189b2b052db407cec172f813`, recorded pointer
  `24d719292b06d8a9d09771d837f8dd4d8d9b8997`.
- Reverified DW-112 before implementation: `TenantsWorkspace.razor:114/116`
  already selects `Localizer[StatusFilterLabelKey]` for `Label`, but hardcodes
  `Tenants.List.StatusFilterLabel` for `aria-label`.
- Owning contract: Story 1.9
  `spec-1-9-authoritative-memories-search-with-protected-paging.md`, Intent,
  Boundaries, tasks and review triage require snapshot-driven scope copy.
  `_snapshot.IsAuthoritativeSearch`, not query presence, decides the label.
  Nonempty-query fallback still uses ordinary page-local filtering. Authoritative
  search filters indexed candidates then hydrates/authorizes/rechecks Tenants
  truth; sorting remains within the protected page. Story 1.2 current-page
  semantics remain valid outside authoritative mode. Story 1.9's final review
  explicitly deferred this pre-existing label mismatch; its separate empty-page
  announcement and dropped-identity/privacy findings remain outside this bundle.
- Existing `StatusFilterLabelKey` at workspace:597-600 owns both labels:
  `Tenants.List.StatusFilterLabel.Authoritative` gives **Status across indexed
  candidates** / **Statut parmi les candidats indexés**; otherwise
  `Tenants.List.StatusFilterLabel` gives **Status on current page** / **Statut sur
  la page actuelle**. Both shipped resx contracts remain unchanged. No intent
  gaps, irreversible work, new API or dependency changes are needed.
- Small implementation: change only the selector's `aria-label` expression.
  Add actual workspace/Fluent rendering regressions using the existing
  `CultureBoundTenantsLocalizer` over shipped resources; check visible label
  markup and aria-label, ordinary blank/nonempty fallback, authoritative ready,
  partial and empty results, and same-component mode transitions. Export actual
  rendered selector snapshots for Chromium's computed accessibility tree.
- Validation: retain existing Fluent/localization/accessibility/browser gates,
  full per-project UI tests, warnings, exclusions, floors and timeouts. Run Debug
  source-reference and maintained Release UI lanes; record broad solution and
  strict package-mode blockers separately. Historical B6/B8/DW-517 and inherited
  gitlink failures remain evidence, without resets or false ownership declarations.
  Do not initialize nested dependencies or change production state.
- Runtime investigation refined the initial one-expression implementation: the
  consumed Fluent 5.0.0 `FluentSelect.OnAfterRenderAsync` invokes
  `Microsoft.FluentUI.Blazor.Components.Select.Initialize` only on first render.
  Its shipped JS sets the actual light-DOM combobox button's name from the label
  once; changing the dropdown host attribute does not rename that button.
  Chromium proved that the one-expression fix retains the old computed name on
  `en/transition-1`. The brief keyed-control approach passed naming checks, but
  independent review demonstrated loss of keyboard focus during a held status
  change. Both attempts and their evidence remain historical, not final behavior.
- Final implementation keeps the original Fluent control. Its existing host name
  uses `StatusFilterLabelKey`; workspace `OnAfterRenderAsync` synchronizes the
  existing native button only when the localized scope label changes or the
  selector reappears. `tenantsStatusFilter.js` reads that owning host name and
  updates the button in place. The consumed library provides no equivalent
  dynamic-name operation: `Initialize` preserves existing names, `UpdateValue`
  changes only values, and `copyToShadow` cannot reach this light-DOM button.
  This limited custom-JS fallback adds no new control, observer, timer, focus
  transfer, component API or dependency. A disconnected/cancelled circuit is
  handled like the workspace's existing browser operations.
- Actual bUnit callbacks cover delayed first load and a held authoritative status
  change through Loading and completion, including unchanged selector identity,
  selected Active/Disabled state and exact gateway request state. Fixture metadata
  records actual label-sync invocations. Chromium replays shipped Fluent startup
  and initialization plus the shipped label helper against actual successive
  render output. It checks computed names and focused native-button identity
  through held status changes, and rejects a deliberately stale native name.

## Review Triage Log

Independent context-free Blind Hunter review used the oneshot route; other
review layers were skipped by that route. Finding floor was
`min(floor(sqrt(21.4) + 1), 10) = 5`; six initial findings and one correction
finding were investigated individually. No unrelated work is deferred or closed.

| Finding | Verdict and disposition | Evidence / correction |
| --- | --- | --- |
| Host attribute alone leaves Fluent's native name stale | medium / patch | Inspected consumed assembly/JS; independent and parent Chromium runs failed ordinary-to-authoritative transition. Final stable-control synchronization fixes this without replacing Fluent. |
| Host mutation does not test the computed-name assertion | medium / patch | Independent runner failed its old mutation assertion. EN/FR controls now mutate the actual native combobox name; host-expression sensitivity is separately established by the pre-fix bUnit eight failures/four passes. |
| Synchronous gateway fixtures miss first-loading capture | medium / patch | Added held initial gateway completion EN/FR and actual loading/resolved snapshots for Chromium. |
| Computed-name validator lacks maintained caller | medium / patch | Appended it to the existing browser runner, preserving the exact original runner prefix and all scenarios/bounds. |
| Fixture subprocess is unbounded before browser timer | medium / patch | Bound fixture export using the existing browser bound and SIGKILL. No existing timeout was raised. |
| Browser spawn failure can bypass cleanup | low / patch | Await spawn/error and guard PID cleanup; independent nonexistent-browser test exited1 and left zero harness directories. |
| Keyed selector loses native focus during status reload | medium / patch | Independent EN/FR DOM check proved focused control removal and BODY focus. Removed key; added held status-change snapshots, stable element/request/value assertions and native focus/identity browser checks. |


Final independent correction recheck:16 actual-rendering cases and30 Chromium
EN/FR states pass, with native control/focus retained. No unresolved finding remains.
Final parent-focused runs pass131/131 in Debug and Release; full maintained MTP
project runs pass4987/4987 in each configuration with zero failures/skips. Both
maintained browser lanes pass. Exact commands, initial failures, unchanged gates,
preservation and remaining release blockers are recorded in
`dw-112-validation-evidence.md` and its linked machine-readable evidence. Only
DW-112 closes; next Tenants-owned bundle is DW-150. No commit or Git mutation ran.
