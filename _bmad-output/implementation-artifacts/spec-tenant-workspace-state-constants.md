---
title: 'Centralize tenant workspace state identifiers'
type: 'refactor'
created: '2026-09-06'
status: 'done'
baseline_revision: '7dc9482f517de8bccd6619311c4914a29923275d'
review_loop_iteration: 0
followup_review_recommended: false
context: []
warnings: []
deferred:
  - summary: >-
      Concurrent Story 4.3 review edits appeared after this run's clean baseline and remain owned by that separate workflow.
    evidence: |-
      The repository was clean at this run's sanity gate. The Story 4.3 spec and deferred-work ledger were written at 10:58:58, before this bundle's implementation files at 11:00:34; the implementation subagent also reported leaving both paths untouched. Their transient status/deferred consistency can only be assessed after that other workflow finishes.
    location: >-
      _bmad-output/implementation-artifacts/deferred-work.md:2832; _bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md:129
    severity: medium
---

<intent-contract>

## Intent

**Problem:** `TenantsWorkspace` duplicates the `tenants`/`users` tab identifiers and `all`/`mine` scope identifiers already owned by `TenantWorkspaceState`. Equal values currently hide the split ownership, so changing either copy can silently disconnect normalized URL state from the rendered tab or scope.

**Approach:** Remove the Razor-local identifier constants and consume the public `TenantWorkspaceState` constants throughout the workspace. Add focused state-contract and bUnit routing coverage that jointly pins the canonical identifier vocabulary and proves each normalized identifier activates its intended surface.

## Boundaries & Constraints

**Always:** Preserve canonical `/tenants` behavior from architecture AD-2: tabs are `tenants|users`, scopes are `all|mine`, invalid inputs remain fail-safe, and existing cursor/state transitions are unchanged. Keep `TenantWorkspaceState` as the only production owner of all four identifiers and retain FrontComposer `FcPageTabs`/`FcPageTab` composition.

**Never:** Do not edit the deferred-work ledger or bundle intent, change route shapes or public identifier values, add aliases, alter unrelated workspace behavior, weaken existing tests, or modify FrontComposer/submodule code.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Default tenants | `tab=tenants&scope=all` using state-owned identifiers | Tenants tab and all-tenants list surface render | No error expected |
| My tenants | `tab=tenants&scope=mine` using state-owned identifiers | Tenants tab and self-audit surface render | No error expected |
| Users | `tab=users&scope=all` using state-owned identifiers | Users tab and membership lookup surface render | Inapplicable scope remains normalized to `all` |
| Identifier contract | State constants are evaluated | Values remain `tenants`, `users`, `all`, and `mine` | Test fails on route-vocabulary drift |

</intent-contract>

## Code Map

- `src/Hexalith.Tenants.UI/State/TenantList/TenantWorkspaceState.cs:48` -- Existing public constants `TenantsTab`, `UsersTab`, `AllScope`, and `MyScope` already drive normalization, transitions, and canonical URL serialization; this remains the single production owner.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:43` -- Tab IDs, scope option values, retained state, routing comparisons, normalization, and `ApplyWorkspaceState` currently consume four private duplicate constants declared near line 285; replace every duplicate reference with `TenantWorkspaceState` constants and delete the private declarations.
- `tests/Hexalith.Tenants.UI.Tests/State/TenantWorkspaceStateTests.cs:8` -- Focused xUnit/Shouldly state coverage; add a contract test pinning all four public identifiers to architecture AD-2's canonical values.
- `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs:93` -- Existing bUnit route/surface tests and shared gateway stubs; add parameterized routing coverage constructed from the state-owned identifiers and assert the active FrontComposer tab plus all/mine/users outer surface.
- `_bmad-output/planning-artifacts/architecture.md:82` -- Read-only source of truth: AD-2 defines canonical tab/scope values and AD-11 requires focused bUnit/conformance guards.
- `.bmad-loop/runs/20260906-103947-b89c/bundles/tenant-workspace-state-constants/intent.md` -- Read-only DW-102 intent and verbatim ledger context; do not modify it or the ledger.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor` -- replace all `TenantsTabId`, `UsersTabId`, `AllTenantsScope`, and `MyTenantsScope` uses with the corresponding `TenantWorkspaceState` constants, then remove the four local declarations -- eliminates production drift without changing behavior.
- [x] `tests/Hexalith.Tenants.UI.Tests/State/TenantWorkspaceStateTests.cs` -- add a state vocabulary contract test for the four exact architecture-owned route values -- detects accidental public route drift.
- [x] `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs` -- add routing tests for all-tenants, my-tenants, and users URLs built from the state constants, asserting active tab and the corresponding rendered outer surface -- detects a component/state integration split.

**Acceptance Criteria:**
- Given the production UI source, when tab and scope identifiers are inspected, then only `TenantWorkspaceState` declares the four canonical literal values and `TenantsWorkspace` consumes those constants everywhere.
- Given canonical all-tenants, my-tenants, and users query state, when `TenantsWorkspace` renders, then the FrontComposer active tab and visible domain surface match the normalized state-owned identifiers.
- Given the state identifier contract, when any tab or scope constant drifts from `tenants`, `users`, `all`, or `mine`, then focused state coverage fails; when workspace routing stops consuming/matching those constants, then focused bUnit routing coverage fails.
- Given the completed change, when the focused UI test project is built and the relevant state/routing tests run, then all existing and new checks pass with warnings treated as errors.

## Spec Change Log

## Review Triage Log

### 2026-09-06 — Review pass
- verdicts: 14 findings — high 0, medium 5, low 4, false 3, maybe-false 2
- findings:
  - `[medium]` `[defer]` The review diff contains ledger and unrelated Story 4.3 edits forbidden by this bundle — the clean pre-run status, file timestamps, and implementation report establish these as concurrent changes owned by another workflow; they were not patched, staged, or otherwise incorporated here.
  - `[medium]` `[defer]` The deferred-work ledger appears contaminated by DW-346/DW-347 changes — the file was written before this bundle's implementation files and after the clean sanity gate, so it remains untouched as concurrent work.
  - `[medium]` `[defer]` The Story 4.3 spec contains unrelated review material — the file has the same earlier concurrent write time and is outside DW-102, so it remains untouched.
  - `[maybe-false]` `[defer]` Story 4.3 is still `done` while ten new patch findings are unchecked — that file is under an active separate workflow; its final state after that workflow completes would settle whether this transient inconsistency persists.
  - `[maybe-false]` `[defer]` Story 4.3 still has `deferred: []` while review text records deferrals — completion of the separate Story 4.3 workflow would settle whether its frontmatter is ultimately inconsistent.
  - `[false]` `[reject]` DW-102 remains open because its ledger handoff is missing — the invocation explicitly assigns ledger resolution to the orchestrator and forbids this build from editing it.
  - `[false]` `[reject]` The implementation spec lacks persisted verification results — review precedes the workflow's required `Auto Run Result`, where executed outcomes are recorded during finalization.
  - `[false]` `[reject]` Verification is invalid without an in-command restore — these are narrow checks inside an already restored build workspace, and both the Release build and full UI test command executed successfully as written.
  - `[low]` `[reject]` A renamed or inline duplicate literal could evade the ownership grep and runtime tests — the present production diff has one owner, equal duplicates do not yet create identifier divergence, and a brittle source-text guard is not justified for an unlikely reintroduction.
  - `[low]` `[patch]` The two new test methods used underscore-separated names — renamed them to `IdentifierVocabularyMatchesTheCanonicalWorkspaceRouteContract` and `WorkspaceCanonicalStateIdentifiersActivateTheMatchingTabAndSurface`.
  - `[low]` `[reject]` Tests do not enforce the strongest possible source-level single-owner invariant — current source inspection proves one production owner, while adding a source-text conformance test for a hypothetical equal-value duplication would add brittle complexity without an everyday behavior failure.
  - `[medium]` `[patch]` Initial-route coverage omitted rendered scope-option values and interactive scope changes — extended the routing theory to assert `all`/`mine` option values and exercise both all-to-mine and mine-to-all callbacks, URLs, state, and rendered surfaces; existing coverage already exercises interactive tab changes.
  - `[medium]` `[defer]` The combined diff diverges from intent through concurrent ledger and Story 4.3 edits — temporal evidence attributes those files to another workflow, so they remain separately owned and untouched.
  - `[low]` `[reject]` The bUnit theory proves value equality but not constant ownership against future equal-value duplication — the intent requires failures on identifier divergence, which the state-value, rendered-option, interactive-routing, and surface assertions cover; a source parser/grep test would be disproportionate.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-restore --warnaserror` -- expected: build succeeds with zero warnings.
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-build --no-restore` -- expected: the complete UI test project passes, including new identifier state and routing guards.
- `rg -n 'TenantsTabId|UsersTabId|AllTenantsScope|MyTenantsScope' src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor` -- expected: no matches.

## Auto Run Result

Status: done

Summary: `TenantWorkspaceState` is now the sole production owner of the `tenants`/`users` tab identifiers and `all`/`mine` scope identifiers. `TenantsWorkspace` consumes those constants across rendered tab and option IDs, defaults, state comparisons, normalization, state application, and canonical navigation. Focused state and bUnit coverage pins the public vocabulary and exercises initial plus interactive routing.

Files changed:
- `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor` -- removed four duplicate local constants and replaced every use with the state-owned constants.
- `tests/Hexalith.Tenants.UI.Tests/State/TenantWorkspaceStateTests.cs` -- added the exact canonical identifier-vocabulary contract.
- `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs` -- added all/mine/users route coverage, rendered scope-option assertions, and interactive scope round trips.
- `_bmad-output/implementation-artifacts/spec-tenant-workspace-state-constants.md` -- recorded the plan, review triage, verification, and final result.

Review findings breakdown:
- Patches applied: high 0, medium 1, low 1. The medium patch added rendered scope-value and interactive scope-routing assertions; the low patch renamed the two new tests to PascalCase.
- Items deferred: one grouped concurrent-work item covers five review observations about Story 4.3 and ledger edits created by another workflow after this run's clean baseline. Those files were left untouched.
- Rejected: the request to close DW-102 in the ledger was rejected because the caller explicitly assigns that write to the orchestrator; the missing-results claim was rejected because finalization records them here; the missing-restore claim was rejected because verification ran successfully in the already restored workspace; three variants demanding a source-text ownership guard were rejected because current inspection proves one owner, the behavioral contract is identifier divergence, and a brittle guard against hypothetical equal-value duplication is disproportionate.

Follow-up review recommendation: false. This first pass patched high 0, medium 1, and low 1 entries, below the follow-up threshold; no specific unverified bundle risk remains.

Verification performed:
- Release UI test-project build with `--no-restore --warnaserror`: passed with 0 warnings and 0 errors.
- Full UI test project: passed 2,870/2,870 with 0 failed and 0 skipped.
- Focused matrix run: passed 4/4 identifier/routing cases with 0 failed, skipped, or not run.
- Duplicate-name search: returned no matches for the four removed Razor-local identifiers.
- `git diff --check` over this bundle's files: passed.

Residual risks: the shared working tree contains concurrent, separately owned Story 4.3 spec and deferred-work ledger edits. They are excluded from this bundle and may keep the repository dirty until their owning workflow finalizes.
