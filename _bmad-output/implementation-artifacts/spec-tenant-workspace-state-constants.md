---
title: 'Centralize tenant workspace state identifiers'
type: 'refactor'
created: '2026-09-06'
status: done
baseline_commit: 23b2a7691ae667fd160a48a5d7293f3cf38d48fe
baseline_revision: e1667e694acf6a2b35e21f90b7da73380d75f4ca
review_loop_iteration: 0
followup_review_recommended: false
context: []
warnings:
  - >-
      An external rebase during this run made the preserved baseline_commit a non-ancestor of HEAD. The repository-wide gitlink validator cannot assess that baseline; the bundle commit is limited to its spec and workspace test, and unrelated dependency/audit changes remain separately owned.
deferred:
  - summary: >-
      Concurrent Story 4.3 review edits appeared after this run's clean baseline and remain owned by that separate workflow.
    evidence: |-
      The repository was clean at this run's sanity gate. The Story 4.3 spec and deferred-work ledger were written at 10:58:58, before this bundle's implementation files at 11:00:34; the implementation subagent also reported leaving both paths untouched. Their transient status/deferred consistency can only be assessed after that other workflow finishes.
    location: >-
      _bmad-output/implementation-artifacts/deferred-work.md:2832; _bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md:129
    severity: medium
  - summary: >-
      Bootstrap token response bound
    evidence: >-
      B1/E2/V2: PostAsync buffers the response before the 64 KiB guard; introduced by the external authentication rebase, not this bundle.
    location: >-
      src/Hexalith.Tenants/Bootstrap/TenantBootstrapCredentialProvider.cs:86
    severity: medium
  - summary: >-
      Bootstrap authority HTTPS guard
    evidence: >-
      B2/E1: The authority credential flow does not enforce HTTPS outside Development; belongs to the external authentication work.
    location: >-
      src/Hexalith.Tenants/Bootstrap/TenantBootstrapCredentialProvider.cs:85
    severity: high
  - summary: >-
      Retained global correction visibility
    evidence: >-
      B3: Concurrent audit composition can hide a submitted global correction when the audit surface becomes stale.
    location: >-
      src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:252
    severity: medium
  - summary: >-
      Audit projection walk cancellation
    evidence: >-
      B5: Concurrent audit evidence requests use CancellationToken.None; generation checks prevent application but do not cancel obsolete I/O.
    location: >-
      src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1646
    severity: low
  - summary: >-
      Parent global projection evidence
    evidence: >-
      B6: The concurrent refresh callback returns new evidence without updating parent projection/authorization state.
    location: >-
      src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:2003
    severity: medium
  - summary: >-
      Global correction entry focus
    evidence: >-
      B7: The concurrent global preview-open success branch does not schedule entry focus.
    location: >-
      src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1666
    severity: medium
  - summary: >-
      Architecture Fluent pin reconciliation
    evidence: >-
      B8: Rebase-owned architecture passages disagree about the Fluent RC versus GA version.
    location: >-
      _bmad-output/planning-artifacts/architecture.md:371
    severity: low
  - summary: >-
      Production domain-service startup guidance
    evidence: >-
      B9: Rebase-owned readiness guidance omits SDK app-channel token and workload configuration prerequisites.
    location: >-
      docs/production-auth-readiness.md:84
    severity: medium
  - summary: >-
      Audit parent confirmation coverage
    evidence: >-
      V1: Concurrent audit tests cover preview but not submit-to-confirmation through the real parent projection refresh callback.
    location: >-
      tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs:1114
    severity: medium
---

<intent-contract>

## Intent

**Problem:** At the story's original pre-change baseline, `TenantsWorkspace` duplicated the `tenants`/`users` tab identifiers and `all`/`mine` scope identifiers already owned by `TenantWorkspaceState`. Equal values hid the split ownership, so changing either copy could silently disconnect normalized URL state from the rendered tab or scope.

**Approach:** Remove the Razor-local identifier constants and consume the public `TenantWorkspaceState` constants throughout the workspace. Add focused state-contract and bUnit routing coverage that jointly pins the canonical identifier vocabulary and proves each normalized identifier activates its intended surface.

## Boundaries & Constraints

**Always:** Preserve canonical `/tenants` behavior from architecture AD-2: tabs are `tenants|users`, scopes are `all|mine`, invalid inputs remain fail-safe, and existing cursor/state transitions are unchanged. Keep `TenantWorkspaceState` as the only production owner of all four identifiers and retain FrontComposer `FcPageTabs`/`FcPageTab` composition. Assess finalization cleanliness only over this bundle's owned paths: unrelated concurrent changes that are attributable to another workflow, identified explicitly, and left untouched do not block this bundle once its own changes are committed and verified; any unattributed or bundle-owned dirt remains blocking.

**Never:** Do not edit the deferred-work ledger or bundle intent, change route shapes or public identifier values, add aliases, alter unrelated workspace behavior, weaken existing tests, or modify FrontComposer/submodule code.

**Re-drive authority:** This `<intent-contract>` block is authoritative for every fresh development drive. Checked execution tasks and Review Triage Log entries are historical evidence only; they do not prove that the current `HEAD` working tree contains the implementation. Inspect the current `HEAD` working tree, preserve any conforming work already present, complete any missing work, and verify every acceptance criterion before reporting completion. When that inspection confirms the recorded baseline already satisfies every criterion, the re-drive is verification/finalization-only and must not manufacture an additional code diff.

**Verification responsibility:** Verify `TenantWorkspaceState`'s sole production ownership of the four canonical identifier literals through focused production-source inspection. Automated tests must pin the four values and route behavior, but they are not required to parse source or fail solely because an equal-value duplicate is introduced under a different symbol name.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Default tenants | `tab=tenants&scope=all` using state-owned identifiers | Tenants tab and all-tenants list surface render | No error expected |
| My tenants | `tab=tenants&scope=mine` using state-owned identifiers | Tenants tab and self-audit surface render | No error expected |
| Users | `tab=users&scope=all` using state-owned identifiers | Users tab and membership lookup surface render | Inapplicable scope remains normalized to `all` |
| Users with inapplicable scope | `tab=users&scope=mine` using state-owned identifiers | Normalize to `tab=users&scope=all`; users tab and membership lookup surface render | Treat `mine` as inapplicable to users, not as a retained ignored value |
| Identifier value and behavior contract | State constants and canonical routes are exercised | Values remain `tenants`, `users`, `all`, and `mine`, and each route activates its matching surface | State coverage fails on value drift and bUnit coverage fails on routing-behavior drift; sole production ownership is established by focused source inspection, not a source-parsing test |
| Already-satisfied re-drive baseline | Recorded baseline contains the production and test changes and satisfies source inspection | Verify the existing implementation and proceed to finalization without requiring an additional code diff | Block only when inspection or required verification identifies an unmet acceptance criterion |
| Concurrent repository changes | After a clean baseline, working-tree changes remain outside this bundle and every such path is explicitly attributable to another workflow | Leave those paths untouched and complete this bundle only after its owned paths are committed and clean and required verification passes | Record ownership as residual risk; do not stage, commit, revert, or edit those paths; unattributed or bundle-owned dirt remains blocking |

</intent-contract>

## Code Map

- `src/Hexalith.Tenants.UI/State/TenantList/TenantWorkspaceState.cs:48` -- Existing public constants `TenantsTab`, `UsersTab`, `AllScope`, and `MyScope` already drive normalization, transitions, and canonical URL serialization; this remains the single production owner.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:43` -- The recorded baseline consumes `TenantWorkspaceState` constants for tab IDs, scope option values, retained state, routing comparisons, normalization, and `ApplyWorkspaceState`; no Razor-local duplicate declarations remain.
- `tests/Hexalith.Tenants.UI.Tests/State/TenantWorkspaceStateTests.cs:8` -- Focused xUnit/Shouldly state coverage; add a contract test pinning all four public identifiers to architecture AD-2's canonical values.
- `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs:93` -- Existing bUnit route/surface tests and shared gateway stubs; add parameterized routing coverage constructed from the state-owned identifiers and assert the active FrontComposer tab plus all/mine/users outer surface.
- `_bmad-output/planning-artifacts/architecture.md:82` -- Read-only source of truth: AD-2 defines canonical tab/scope values and AD-11 requires focused bUnit/conformance guards.
- `.bmad-loop/runs/20260906-103947-b89c/bundles/tenant-workspace-state-constants/intent.md` -- Read-only DW-102 intent and verbatim ledger context; do not modify it or the ledger.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor` -- replace all `TenantsTabId`, `UsersTabId`, `AllTenantsScope`, and `MyTenantsScope` uses with the corresponding `TenantWorkspaceState` constants, then remove the four local declarations -- eliminates production drift without changing behavior.
- [x] `tests/Hexalith.Tenants.UI.Tests/State/TenantWorkspaceStateTests.cs` -- add a state vocabulary contract test for the four exact architecture-owned route values -- detects accidental public route drift.
- [x] `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs` -- add routing tests for all-tenants, my-tenants, and users URLs built from the state constants, asserting active tab and the corresponding rendered outer surface -- detects a component/state integration split. The 2026-10-08 re-drive also verifies users/mine normalization to all, canonical users navigation, and absence of the my-tenants surface.

**Acceptance Criteria:**
- Given the production UI source, when tab and scope identifiers are inspected, then only `TenantWorkspaceState` declares the four canonical literal values and `TenantsWorkspace` consumes those constants everywhere.
- Given canonical all-tenants, my-tenants, and users query state, when `TenantsWorkspace` renders, then the FrontComposer active tab and visible domain surface match the normalized state-owned identifiers.
- Given `tab=users&scope=mine`, when workspace state is normalized, then the result is `tab=users&scope=all`, and the users tab and membership lookup surface render.
- Given the state identifier contract, when any tab or scope constant drifts from `tenants`, `users`, `all`, or `mine`, then focused state coverage fails; when workspace routing behavior diverges from those state-owned identifiers, then focused bUnit routing coverage fails. Direct single ownership remains a source-inspection criterion rather than a source-parsing test requirement.
- Given the completed change, when the focused UI test project is built and the relevant state/routing tests run, then all existing and new checks pass with warnings treated as errors.
- Given finalization finds remaining working-tree changes, when this bundle's owned paths are committed and clean, required verification has passed, and every remaining dirty path is explicitly attributed to concurrent work and recorded as untouched and out of scope, then repository-wide dirtiness is reported as residual risk and does not block this bundle's completion; any unattributed or bundle-owned dirt does block completion.

## Spec Change Log

- 2026-10-08: Preserved the conforming production refactor and value contract; extended the existing routing theory with the missing users/mine normalization case and an explicit expected normalized scope. Existing route-backed navigation behavior is preserved.
- 2026-09-06: Resolved re-drive ambiguity by making the intent contract authoritative over historical task/review records, defining `users&mine` normalization, and separating focused source ownership inspection from automated value/behavior coverage.

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

### 2026-10-08 — Re-drive review

- All three required review layers completed against the aggregate baseline diff. The bundle-owned source diff is limited to the workspace routing theory; the production refactor and vocabulary contract were already present.
- Verdicts before grouping: 14 findings — high 2, medium 8, low 3, false 1. No intent-gap, bad-spec, or patch entry remains for this bundle.
- Each external finding is recorded here rather than appended to the deferred-work ledger, honoring the intent contract's explicit prohibition on editing that ledger. These records do not assign unrelated changes to this bundle.
- `B1` [medium] [defer] The authority response is buffered by PostAsync before the 64 KiB check. This is in the bootstrap implementation brought into the aggregate diff by the external rebase, not this bundle.
- `B2` [high] [defer] AcquireFromAuthorityAsync sends configured credentials without an environment-sensitive HTTPS check. The rebase-owned bootstrap flow is expressly outside this identifier-centralization intent.
- `B3` [medium] [defer] The audit parent displays the global panel only while authorized/current or while the tenant-only retained-attempt predicate succeeds; a stale audit can hide a submitted global correction. These are concurrent audit edits, not bundle changes.
- `B4` [false] [reject] The claim that global corrections have no resume path is disproved by GlobalAdministratorCorrectionPanel.TryAdoptEligibleReconciliation, called on initialization and parameter updates when the compatible audit correction is reopened. A dedicated parent Resume button is a separate enhancement.
- `B5` [low] [defer] The concurrent audit projection walks use CancellationToken.None. Generation/disposal checks prevent stale application, but obsolete requests can continue consuming resources; cancellation ownership belongs to that audit workflow.
- `B6` [medium] [defer] The concurrent parent refresh callback returns new projection evidence without replacing the parent snapshot/authorization. Child OnParametersSet preserves tracked/terminal snapshots, but parent eligibility still reads old evidence until another parent refresh.
- `B7` [medium] [defer] The successful concurrent global-correction open branch sets its preview without the tenant branch's entry-focus scheduling. Keyboard entry-focus verification belongs to the audit correction implementation.
- `B8` [low] [defer] Architecture line 371 names Fluent GA while older passages still name the RC pin and RC-to-GA actions. Those documentation changes were brought in by the external rebase and are read-only for this bundle.
- `B9` [medium] [defer] The production readiness document omits the new SDK app-channel token/workload settings; EventStoreDomainServiceSecurityStartupValidator checks APP_API_TOKEN and Authentication:Workload outside Development. This is part of the rebase-owned authentication migration.
- `B10` [low] [reject] The proposed universal DOM-absence assertions go beyond the intended visible-surface contract: FcPageTab intentionally retains previously activated panels. Active Fluent tab, normalized state, expected surface, and all/mine scope transitions already pin the contract; mutually exclusive scope branches are confirmed by production inspection. No regression requiring extra assertions was demonstrated.
- `E1` [high] [defer] The missing production HTTPS guard repeats B2; verified against the rebase-owned credential provider. Grouped with B2, without patching unrelated code.
- `E2` [medium] [defer] The post-buffering size check repeats B1; verified against the rebase-owned provider call order. Grouped with B1 and V2.
- `V1` [medium] [defer] The verification reviewer traced the real audit-parent refresh callback and found no rendered-parent submit-to-confirmation test; standalone panel callbacks/fallback tests cannot cover that caller path. This gap was added by concurrent audit work and is excluded by this bundle's intent.
- `V2` [medium] [defer] The reviewer's isolated SDK 10.0.401 probe accepted 71,680 bytes after LoadIntoBufferAsync(65,536), corroborating B1/E2. The bootstrap implementation is outside this bundle.

Grouped external follow-ups: bootstrap response bound (B1/E2/V2); bootstrap HTTPS (B2/E1); retained global panel (B3); projection-walk cancellation (B5); parent projection evidence (B6); correction entry focus (B7); architecture pin reconciliation (B8); production startup guidance (B9); audit-parent confirmation coverage (V1). All remain with their separate owners.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-restore --warnaserror` -- expected: build succeeds with zero warnings.
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-build --no-restore` -- expected: the complete UI test project passes, including new identifier state and routing guards.
- `rg -n 'TenantsTabId|UsersTabId|AllTenantsScope|MyTenantsScope' src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor` -- expected: no matches.


## Auto Run Result — 2026-10-08

- Production inspection confirms `TenantWorkspaceState` owns the four tab/scope constants and `TenantsWorkspace` consumes them. The existing vocabulary test already pins their canonical values. The missing users/mine render case was added to the existing theory with explicit normalized-scope, users-route, and surface assertions.
- `dotnet restore tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --property:Configuration=Release` — passed; all projects up to date, without changing tracked dependency configuration.
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-restore --warnaserror -m:1` — passed; 0 warnings, 0 errors, 2.89 seconds. This unmodified full project graph passed after concurrent compile errors and overwritten dependency assets were resolved.
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-build --no-restore` — passed; 3,972 succeeded, 0 failed, 0 skipped, 55.253 seconds, with every test file included. Earlier fallback runs are not used as full-lane acceptance evidence.
- `rg -n 'TenantsTabId|UsersTabId|AllTenantsScope|MyTenantsScope' src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor` — expected exit 1, no matches. Source inspection of the four literals and current render/transition call sites confirms single tab/scope ownership.
- Three independent review layers completed. Their 14 findings were individually triaged before grouping; no bundle-owned patch or unresolved intent/spec finding remained. Nine grouped external follow-ups are retained in frontmatter and the triage log. The deferred-work ledger remains outside this run.
- `./node_modules/.bin/commitlint --edit /tmp/tenant-workspace-state-constants-commit-9okn3x_j.txt --verbose` — passed using the installed and lockfile-pinned CLI 21.2.3; exact candidate `test(ui): cover normalized tenant workspace scope`; 0 problems, 0 warnings.
- `git diff --check -- _bmad-output/implementation-artifacts/spec-tenant-workspace-state-constants.md tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs` — passed for the owned changes.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-tenant-workspace-state-constants.md` — failed: `baseline_commit 23b2a76 is not an ancestor of HEAD (c3c2b54)`. Reflog established an external rebase from the original captured baseline to `3a90342da043877f189c09a8ed3e98dff44b90b8`; the original full `baseline_commit` is preserved per the workflow. This repository-wide provenance check remains a residual limitation, distinct from the passed UI checks and path-restricted bundle commit.
- Finalization parent observed directly from version control: `c3c2b546e0d80e5793645e6926e5169d821a4738`.

### Concurrent ownership

The remaining edits are outside this bundle. They are excluded from its commit; the intent contract allows completion once the owned paths are committed and clean and required UI verification passes.

- The separate global-administrator paging/audit workflow owns `spec-global-admin-projection-paging.md`, `TenantAuditPage.razor`, the audit grids/receipts/global correction panel, `GlobalAdministratorsProjectionLoader`, global correction snapshot/intent, and their associated audit/grid/panel/loader/state/conformance tests.
- The separate dependency-refresh workflow and pre-existing package edits own `spec-refresh-dependencies.md`, both dependency-refresh evidence JSON files, `_bmad-output/project-context.md`, `package.json`, `package-lock.json`, `src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj`, and the externally moved Builds/EventStore/Platform gitlinks. No part of those edits is staged or committed by this bundle.
- Badge accessibility and behavioral guard spec finalizations were committed by other workflows during the run; the historical bootstrap/authorization and architecture/readiness changes entered through the external rebase. They are not attributed to this bundle.

## File List

- `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs`
- `_bmad-output/implementation-artifacts/spec-tenant-workspace-state-constants.md`
