---
created: 2026-07-19T17:39:19+02:00
baseline_commit: 088232a7255698e20105594d9e0ef12a0f09c73e
frontcomposer_source_commit_at_creation: 064b886d54b72975f8fdb061bd4ebbf630ddb374
frontcomposer_source_commit_verified: d3761fa08ce2f4bf004e8adc7f500822d04276f8
builds_source_commit_at_creation: cb8b2d412a937e09380387601c2682e080b66220
builds_source_commit_verified: 9ec0a032d785dd0abdc14276e8784d6fdd826fd0
frontcomposer_package_baseline: 4.0.1
fluent_ui_pin: 5.0.0-rc.4-26180.1
historical_evidence: _bmad-output/implementation-artifacts/story-1-0-spike-note-2026-06-05.md
superseded_by: _bmad-output/implementation-artifacts/story-1-0-frontcomposer-fluent-reverification-2026-07-19.md
# 2026-07-19 review patch: the create-time frontcomposer/builds commits (*_at_creation) are historical
# create-story observations; the *_verified commits are what the implementation-time evidence report
# actually inspected and tested. Both submodules advance continuously via an external, automated
# ~30min-to-few-hours `pull --tags origin main` cadence (confirmed via reflog spanning weeks) unrelated
# to any specific story session — see the evidence report's Immutable Baseline table.
---

# Story 1.0: Reverify FrontComposer Shell and Fluent Contracts

Status: done

<!-- Created by the BMAD create-story workflow. The 2026-06-05 spike remains immutable historical evidence. -->

## Story

As a Tenants UI maintainer,
I want the shared FrontComposer and Fluent contracts reverified against the corrected architecture and pinned dependencies,
so that subsequent stories build on demonstrated capabilities and honest fallback boundaries.

## Acceptance Criteria

1. **Shell integration and architecture boundary.** Given the current FrontComposer source/package baseline and the existing Story 1.0 evidence, when the shell integration contract is reverified, then the exact supported registration, single-module navigation, full-width operational layout, and constrained-inner-region APIs are documented with their source and version, and any divergence from AD-1 through AD-4 is recorded as a blocker rather than hidden by Tenants-owned replacement infrastructure.
2. **Command feedback and concurrency.** Given the shared command-feedback and concurrency capabilities, when their corrected behavior is reverified, then the evidence demonstrates distinct submitted, accepted, projection-pending, confirmed, audit-pending, and audit-available states, and confirms that SignalR is only a re-query nudge and locking is scoped by `(interactive circuit, AggregateIdentity)` while unrelated aggregates may proceed.
3. **Accessibility, localization, and documentation.** Given the FrontComposer accessibility, localization, and documentation contracts, when their available APIs and reference material are inspected, then `FC-A11Y`, `FC-L10N`, and `FC-DOC` are classified as verified, changed, or blocked with reproducible evidence, and story-specific keyboard, focus, live-region, localization, responsive, and documentation evidence remains mandatory rather than being waived by this verification.
4. **Generated-grid boundary.** Given the generated FrontComposer grid capability, when it is compared with Tenants' cursor pagination, safety-column pinning, stable action slots, and six-state requirements, then the Tenants-specific `TenantDataGrid` boundary remains explicit, and reusable generic grid capability is not reimplemented inside Tenants.
5. **Approved fallbacks and token posture.** Given `FC-AUD`, `FC-CNS`, and `FC-TOK` readiness, when the fallback posture is reviewed, then flat `AuditDataGrid` and inline full-content Consequence Preview remain the only approved local fallbacks, and missing shared token capability is handled through verified Fluent semantic/icon mappings without inventing token names.
6. **Exact Fluent package contract.** Given the centrally pinned Fluent UI Blazor version, when badge colors, Size20 status icons, grid pinning, MessageBar behavior, focus behavior, and ARIA parameters are checked against that exact version, then every relied-upon name and behavior is recorded as verified or blocked, and no assumption from an earlier release candidate is presented as current evidence.
7. **Dependency ownership.** Given the repository's dependency boundaries, when this reverification is performed, then no root-declared submodule source is modified without a separately authorized task, and any shared-platform gap is assigned to its owning module rather than implemented as Tenants boilerplate.
8. **Reproducible result.** Given the completed reverification, when the focused contract and conformance checks are run, then the exact commands and results are recorded, and any blocked check identifies its command, blocker, affected downstream stories, and approved conservative behavior.

## Tasks / Subtasks

- [x] Establish an immutable, reproducible baseline and successor evidence record. (AC: 1, 6, 7, 8)
  - [x] Create `_bmad-output/implementation-artifacts/story-1-0-frontcomposer-fluent-reverification-2026-07-19.md`; do not edit or overwrite `story-1-0-spike-note-2026-06-05.md`.
  - [x] Record the Tenants commit, root-declared FrontComposer and Builds submodule SHAs/descriptions, `HexalithFrontComposerVersion`, both Fluent package pins, and the actually resolved UI/UI-test package versions.
  - [x] Distinguish the published FrontComposer package baseline (`4.0.1`) from the source consumed by the unconditional project references (`064b886d...`, currently described as `v4.0.1-74-g064b886d`). Do not call either one “current” without naming which baseline was tested.
  - [x] Give every contract row one status: `verified`, `changed`, or `blocked`. For `changed` or `blocked`, include source evidence, owner, affected downstream stories, and the approved fail-closed/conservative behavior.

- [x] Reverify shell composition against AD-1 through AD-4. (AC: 1, 7, 8)
  - [x] Verify the supported bootstrap order and exact current APIs around `AddFluentUIComponents`, `AddHexalithFrontComposerQuickstart`, `AddHexalithDomain<T>`, optional server security, `AddHexalithEventStore`, request localization, and `FrontComposerShell`.
  - [x] Verify the Tenants manifest contributes exactly one `/tenants` shell entry, with Tenants sub-surfaces remaining page-local state or contextual routes rather than extra shell entries.
  - [x] Verify dense list/audit surfaces use the FrontComposer full-width contract and detail/form/lookup content uses the constrained contract or a FrontComposer-owned constrained inner region.
  - [x] Verify Tenants contributes domain composition only and has not recreated generic shell, navigation, layout, registration, theme, or platform plumbing.
  - [x] Run the focused composition and page-layout checks and attach their exact output to the successor evidence record.

- [x] Reverify FC-CMD and FC-CNC against the corrected truth and concurrency contracts. (AC: 2, 7, 8)
  - [x] Map the exact FrontComposer and Tenants representations for `submitted`, `accepted`, `projection-pending`, `confirmed`, `audit-pending`, and `audit-available`; do not infer a state from copy, color, or another state.
  - [x] Prove that accepted/status/SignalR evidence cannot directly establish projection confirmation or audit availability. SignalR may request an authoritative re-query only and must not itself advance lifecycle truth.
  - [x] Prove that command activity remains locked from submit through accepted/projection-pending until terminal evidence for the same `(interactive circuit, AggregateIdentity)`.
  - [x] Prove with two different aggregate identities in one circuit that unrelated aggregates can proceed, while two commands for the same aggregate cannot overlap. Include the fixed `global-administrators` aggregate identity in the matrix. **(2026-07-19 review patch: two of the four matrix rows — the differing-aggregate rows — could not be executed, because `CommandExecutionAdmissionRequest` carries no aggregate identity; those rows are recorded in the evidence report as currently infeasible and drive the FC-CNC `blocked` classification below, not as demonstrated proof.)**
  - [x] Inspect `CommandExecutionAdmissionRequest`, `CommandExecutionAdmissionGate`, pending-command state, generated command form emission, Tenants command snapshots, and page activity guards. If aggregate identity is absent or any pending command blocks every aggregate, classify FC-CNC as blocked and assign the shared gate gap to FrontComposer; do not add a second Tenants admission framework.
  - [x] If `SignalRNudge()` changes lifecycle state without a completed authoritative re-query, or if `audit-available` has no unambiguous typed representation, classify the affected contract as blocked and identify every command story that must remain unpromoted.

- [x] Reverify FC-A11Y, FC-L10N, and FC-DOC. (AC: 3, 7, 8)
  - [x] Verify shell-provided skip/focus/keyboard/live-region/reduced-motion/forced-colors primitives and HFC1050-HFC1055 diagnostics against current source and tests.
  - [x] Verify the current shell-owned `FcShellResources` versus Tenants-owned resource boundary, EN/FR parity evidence, navigation localization, and request-culture registration.
  - [x] Verify every cited current component/reference/testing/accessibility document exists; explicitly record that no Storybook evidence may be claimed unless a real current path is found.
  - [x] Record the consumer obligations that remain per story: keyboard path, focus entry/return/terminal behavior, announcement intent, localization parity and formatting, responsive/reflow and forced-colors behavior, and documentation/evidence updates.

- [x] Reverify FC-TBL while preserving the Tenants grid boundary. (AC: 4, 7, 8)
  - [x] Compare the current generated grid/page-loader implementation with opaque cursor pass-through, cursor invalidation recovery, safety-column pinning, stable row/action slots, and the six non-collapsing list states.
  - [x] Record whether current FrontComposer pagination remains offset-based (`skip`/`take`) or now has a genuine protected cursor contract; do not equate virtualization offsets with Tenants cursors.
  - [x] Verify `TenantDataGrid` and `AuditDataGrid` compose Fluent primitives, preserve Tenants-specific safety columns/actions/states, and do not become generic shared grid frameworks.
  - [x] If a reusable capability is still missing, assign it to FrontComposer enhancement work and retain the existing narrow Tenants boundary; do not duplicate it elsewhere in Tenants.

- [x] Reconcile FC-AUD, FC-CNS, and FC-TOK with current source. (AC: 5, 6, 7)
  - [x] Confirm the only approved local fallbacks remain the flat `AuditDataGrid` and inline full-content Consequence Preview; the historical global FC-CNC fallback is superseded by AD-12's aggregate-scoped form.
  - [x] Inspect current FrontComposer badge/token components before calling `FC-TOK` missing. If the current six-slot `BadgeSlot`/`FcStatusBadge` contract is only partial for Tenants' eight Fluent semantic roles and state-specific Size20 icons, classify it accurately as partial/changed rather than inventing shared token names.
  - [x] Verify Tenants mappings use only real `BadgeColor` values and real Fluent icon types. Preserve Success exclusively for proven current/active/confirmed/audit-available truth.

- [x] Verify the exact Fluent UI rc.4 API and rendering assumptions. (AC: 6, 8)
  - [x] Use assembly inspection against `Microsoft.FluentUI.AspNetCore.Components@5.0.0-rc.4-26180.1` and `Microsoft.FluentUI.AspNetCore.Components.Icons@5.0.0-rc.4-26180.1`; do not use rc.3 documentation or type names as proof.
  - [x] Verify all eight `BadgeColor` values, `FluentBadge` text/icon/ARIA usage, `ColumnBase<T>.Pin`, `DataGridColumnPin.Start`, the grid's keyboard/focus parameters and behavior, `FluentMessageBar`, `MessageBarLayout.Notification`, `AriaLive`, and the required `Icons.Regular.Size20.*` matrix in Dev Notes.
  - [x] Compare the verified API with actual Tenants rendering. Treat current 16px status factories, missing `IconStart`/accessible icon labeling, stale demo-site parameters, or missing notification layout as findings to classify—not as silently acceptable substitutes.
  - [x] Add or strengthen Tenants-owned conformance tests only when they guard a confirmed Tenants boundary. Do not weaken existing guards, add intentionally failing tests, or patch FrontComposer source under this story.

- [x] Run focused checks and issue the gate decision. (AC: 1-8)
  - [x] Run the package, source, composition, layout, Fluent, accessibility/localization/documentation, grid, command-state, SignalR, and aggregate-lock checks listed under Testing Standards.
  - [x] Record every exact command, exit code, pass count, and relevant output in the successor evidence report and the Dev Agent Record.
  - [x] For each blocked check, record the blocker, owning module, affected stories, and conservative behavior. A blocked shared contract still permits this verification story to conclude, but it does not clear affected downstream stories for implementation.
  - [x] Confirm `git diff --submodule=short` shows no submodule source change and that the unrelated untracked/working-tree files present before implementation remain untouched.

### Review Findings — BMAD Code Review (2026-07-19)

_Adversarial review (Blind Hunter + Edge Case Hunter + Acceptance Auditor) over the uncommitted working-tree diff (this story file, the evidence report, and the `sprint-status.yaml` status flip). The Acceptance Auditor independently reproduced most of the report's factual claims (arithmetic sums, absence/presence greps, doc-path existence, AD text) and confirmed them; the findings below are where the report still falls short of its own "reproducible evidence, honestly classified" bar._

- [x] [Review][Decision] **RESOLVED 2026-07-19 (reviewer investigation) — dismissed, AC7 not violated; downgraded to a documentation patch.** "Pre-existing" submodule pointer claim [`story-1-0-frontcomposer-fluent-reverification-2026-07-19.md:30`, `:235`] was flagged because the reflog showed the FrontComposer/Builds fast-forward at 17:50:27/28 on 2026-07-19, inside this story's implementation window. Investigation: `git -C references/Hexalith.FrontComposer reflog` and `git -C references/Hexalith.Builds reflog` (full history, clone-to-present) show a continuous, tightly-paired automated `pull --tags origin main` cadence for **both** submodules simultaneously (within 1-2 seconds of each other), recurring every ~30 minutes to a few hours going back to the initial clone on 2026-05-31 — dozens of instances, entirely independent of any specific story session. The 17:50:27/28 pull is one more instance of this same long-running background process, not something triggered by the Dev Agent's reverification work. AC7 (no submodule *source* modification) holds. Remaining patch: the evidence report should cite this reflog pattern to substantiate "pre-existing" with evidence rather than bare assertion — folded into the frontmatter/commit-tracking patch above.
- [x] [Review][Decision] **RESOLVED 2026-07-19 (reviewer independent re-run) — confirmed accurate, no further action.** Self-reported test evidence [`story-1-0-frontcomposer-fluent-reverification-2026-07-19.md:99-237`] was independently spot-checked: re-ran `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` against the existing Release binaries → **904/904 passed, 0 errors/failed/skipped**, exact match to the report. Also re-ran the FrontComposer `CommandExecutionAdmissionGateTests` class independently → **8/8 passed**, exact match. Both spot-checks confirm the report's headline claims are accurate, not merely asserted. (Full re-run of every cited class/assembly-inspection command was not performed — two high-value spot-checks were judged sufficient given both matched exactly.)
- [x] [Review][Patch] APPLIED — FC-CNC task checkbox overstates what was actually proven [`1-0-reverify-frontcomposer-shell-and-fluent-contracts.md:52`] — the subtask "Prove with two different aggregate identities in one circuit that unrelated aggregates can proceed... Include the fixed `global-administrators` aggregate identity in the matrix" is checked `[x]` complete, but the evidence report's own aggregate matrix states this "cannot be satisfied by the current gate" and that two of the four required rows "cannot be executed as such" because `CommandExecutionAdmissionRequest` carries no aggregate identity. Unlike sibling bullets in the same task group, which carry an explicit "if X is absent, classify as blocked" escape clause, this bullet has none — checking it complete blurs "we investigated and found it infeasible" with "the criterion was met." Fix: add a caveat/annotation (either in the checkbox text or the Debug Log) making clear the proof was demonstrated to be currently impossible, not executed.
- [x] [Review][Patch] APPLIED — Frontmatter commit-tracking is inconsistent between the two new files [`1-0-reverify-frontcomposer-shell-and-fluent-contracts.md:4`, `story-1-0-frontcomposer-fluent-reverification-2026-07-19.md:7-10`] — the story file's frontmatter still pins the stale create-story `frontcomposer_source_commit: 064b886d...` with no supersession marker and has no `builds_source_commit` field at all, while the evidence report's frontmatter instead records the implementation-time `d3761fa0...`/`9ec0a032...` values for the same run. A reader or tool that indexes/greps frontmatter across the two files would get two different values for what looks like the same field. Fix: update the story file's frontmatter to the implementation-time values (or add an explicit `superseded_by`/`see_evidence_report` marker) and add the missing `builds_source_commit` field. Also fold in the reflog evidence gathered while resolving the decision above: cite the paired FrontComposer/Builds reflog pattern (continuous ~30min-to-few-hours automated `pull --tags origin main` cadence since the 2026-05-31 clone) as the actual substantiation for "pre-existing root pointer differences," rather than leaving that claim as a bare assertion.
- [x] [Review][Patch] APPLIED — AD-4 paraphrase misattributes the DI/transport-boundary rule [`1-0-reverify-frontcomposer-shell-and-fluent-contracts.md:120`] — Dev Notes states "AD-4: Tenants composes domain UI only. Generic shell, navigation, layout, lifecycle, token, DI, or transport scaffolding does not belong in this module," but `architecture.md`'s actual AD-4 text only covers generic grids/tabs/shell layout/theme/command chrome; the DI/transport-boundary rule belongs to AD-5 ("Server-Side Gateways Are The Only Backend Egress") and AD-13 ("The UI Host Is Domain-Owned; Orchestration Is Platform-Owned"). A downstream story author who reads only this paraphrase (not the architecture doc) could misattribute the DI/transport rule to AD-4. Fix: correct the paraphrase or add AD-5/AD-13 references.
- [x] [Review][Patch] APPLIED — AD-12 paraphrase omits the confirmed/already-applied/unable-to-verify semantics [`1-0-reverify-frontcomposer-shell-and-fluent-contracts.md:122`] — the one-liner covers only lock scope ("lock key is `(interactive circuit, AggregateIdentity)`..."), but `architecture.md`'s AD-12 also rules that "confirmed" requires postcondition-plus-projection-version evidence, that a pre-existing expected state is "already applied" not "confirmed," and that unavailable provenance is "unable to verify" — clauses directly relevant to this story's own FC-CMD blocked finding. Fix: fold those clauses into the Dev Notes summary so a reader doesn't think AD-12 is locking-only.
- [x] [Review][Patch] APPLIED — Ambiguous test-count attribution in the 84/84 row [`story-1-0-frontcomposer-fluent-reverification-2026-07-19.md:201`] — the row names 5 test categories ("`TruthStateBadgeTests`, tenant preview/start intent, tenant correction panel, global correction snapshot/panel, and audit-grid correction") against 7 numeric counts `(1, 10, 14, 17, 24, 15, 3)`, with "tenant preview/start intent" and "global correction snapshot/panel" each silently bundling two counts. The 7 numbers do sum to 84, but a reader cannot attribute each number to its class to independently reproduce the row. Fix: list one count per named class.
- [x] [Review][Patch] APPLIED — Stale runner-incompatibility warning left unreconciled [`1-0-reverify-frontcomposer-shell-and-fluent-contracts.md:190`, `story-1-0-frontcomposer-fluent-reverification-2026-07-19.md:231`] — the story's Testing Standards still warns `dotnet test` "may hit the repository's known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility," while the evidence report's Final Quality Gate instead notes "the anticipated runner incompatibility did not occur," with no update to the story-file warning for future readers/agents who might still over-engineer a workaround. Fix: add a dated note to the Testing Standards warning (e.g., "did not reproduce as of 2026-07-19; monitor").
- [x] [Review][Defer] Zero test changes despite several identified Tenants-rendering gaps (Size16 vs required Size20 icons, missing `IconLabel`, unpinned freshness safety column, missing `MessageBarLayout.Notification`/`AriaLive` usage) [`story-1-0-frontcomposer-fluent-reverification-2026-07-19.md` FC-TOK/FC-TBL rows] — deferred, pre-existing. AC5's own wording is conditional ("add tests only when they guard a confirmed Tenants boundary"), so whether any of these gaps currently qualify is a judgment call for whichever story next touches badge/grid rendering, not a clear miss by this verification-only story.
- [x] [Review][Defer] No tracking ticket/issue exists for the gaps this story assigns to FrontComposer (FC-CMD, FC-CNC, FC-TBL, FC-TOK owner handoffs) [`story-1-0-frontcomposer-fluent-reverification-2026-07-19.md` Contract Gate Matrix] — deferred, pre-existing. "Assign to FrontComposer" has no actual assignment mechanism in this repo's process; matches the existing routing convention in `deferred-work.md`'s "Cross-Submodule Owner Handoffs" section, which this story should eventually feed.
- [x] [Review][Defer] `sprint-status.yaml`'s flat per-story status can't represent "review with 2 of 5 sub-contracts blocked" [`sprint-status.yaml:53`] — deferred, pre-existing. This is a schema limitation of a shared tracking file used across the whole project, not something this story's diff introduced or can fix alone.
- [x] [Review][Defer] `epic-1: done` while most of Epic 1's 12 stories (1-0, 1-1, 1-2, 1-4, 1-6, 1-8 through 1-11) remain `backlog`/`review` and only 3 (1-3, 1-5, 1-7) are `done` [`sprint-status.yaml:52-64`] — deferred, pre-existing. This violates the file's own documented rule ("done: All stories in epic completed") but predates this diff — the `epic-1`/`epic-1-retrospective` lines are unchanged by story 1.0's edit, which only touched the `1-0-...` status line. Likely stale from the epics.md renumbering during the 2026-07-19 sprint-change-proposal rollout; worth a sprint-planning resync, not a fix within this story.

## Dev Notes

### Outcome And Scope

- This story protects operator trust by proving that shell, lifecycle, concurrency, grid, accessibility, localization, and visual semantics mean what later stories claim they mean. It produces a gate decision and reproducible evidence; it does not implement a replacement platform capability.
- The June spike is useful historical context but is not a readiness waiver. It inspected an older FrontComposer/Fluent baseline and approved global one-at-a-time behavior that the 2026-07-15 architecture correction superseded. Preserve it unchanged and write a dated successor report.
- Completion can legitimately contain blocked contract rows. A blocker must remain visible and prevent the affected downstream stories from being promoted; it must not be “resolved” with local boilerplate under this story.

### Baseline Facts To Reproduce

| Baseline | Create-story observation | Implementation requirement |
|---|---|---|
| Tenants | `088232a7255698e20105594d9e0ef12a0f09c73e` | Re-record the implementation-time SHA. |
| FrontComposer source | `064b886d54b72975f8fdb061bd4ebbf630ddb374`, `v4.0.1-74-g064b886d` | This root-declared source submodule is what the UI project references; inspect but do not modify it. |
| Published FrontComposer baseline | central `HexalithFrontComposerVersion=4.0.1` | Record separately from the newer source SHA; do not claim package/source equivalence without evidence. |
| Builds source | `cb8b2d412a937e09380387601c2682e080b66220` | This root-declared submodule supplies the central package pins. |
| Fluent components/icons | `5.0.0-rc.4-26180.1` | Verify both the declared and resolved versions and inspect this exact assembly. |

The UI and UI-test project files contain unconditional FrontComposer project references, while the root build configuration defaults other cross-repository dependencies to packages. The verification report must state whether each result came from local source, a published package, or the resolved Fluent assembly. [Source: `Directory.Build.props`; `src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj`; `tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj`; `references/Hexalith.Builds/Props/Directory.Packages.props`]

### High-Risk Seams Found During Story Preparation

These are investigation leads, not pre-completed acceptance evidence. Re-run them, add tests or runtime evidence where appropriate, and classify the result.

- FrontComposer's current `CommandExecutionAdmissionRequest` carries command type and label but no aggregate identity. `CommandExecutionAdmissionGate` denies a second admission while any current admission or pending entry exists. That appears to preserve the superseded circuit-global policy rather than AD-12's aggregate-scoped lock. [Source: `references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/State/PendingCommands/CommandExecutionAdmissionRequest.cs`; `references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/State/PendingCommands/CommandExecutionAdmissionGate.cs`; `references/Hexalith.FrontComposer/tests/Hexalith.FrontComposer.Shell.Tests/State/PendingCommands/CommandExecutionAdmissionGateTests.cs`]
- Several Tenants command snapshots implement `SignalRNudge()` by moving `RequestSent`/`Accepted` to `ProjectionPending`. AD-7/architecture says SignalR requests re-query only and never advances lifecycle truth directly. Determine whether this is merely presentation wording or a real state-transition violation; fail closed if unproven. [Source: `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs`; `_bmad-output/planning-artifacts/architecture.md#AD-7 — Projection-confirmed truth is the shared runtime model`]
- `TenantCommandAuditState` currently contains `NotStarted`, `AuditPending`, `AuditDelayed`, `AuditUnavailable`, and `MissingSupport`, but no `AuditAvailable`. Audit readiness also appears in receipt/list state. Prove an unambiguous typed non-collapse contract or classify the gap. [Source: `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs`; `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditReceipt.cs`]
- FrontComposer now has `BadgeSlot`, `FcStatusBadge`, and status-icon tables, but the shared slots cover six meanings and the status icons are generic Size16 mappings. Tenants UX needs all eight Fluent roles plus state-specific Size20 icons. Treat this as current evidence that `FC-TOK` may be partial/changed, not permission to invent a shared vocabulary. [Source: `references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Contracts/Attributes/BadgeSlot.cs`; `references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Badges/SlotAppearanceTable.cs`; `references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Badges/StatusIconTable.cs`]
- Current Tenants `TenantDataGrid` pins identity and status, while the UX contract also identifies freshness as safety-critical; `AuditDataGrid` pins timestamp, actor, and outcome. Verify the intended responsive behavior and do not assume a compile-valid `Pin` attribute proves the complete safety behavior. [Source: `src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor`; `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditDataGrid.razor`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md#data-grid`]

### Shell And Architecture Invariants

- AD-1: one shell module entry at `/tenants`; contextual/detail/audit/global-administrator routes do not register additional shell entries.
- AD-2: workspace tab/scope/filter/sort/cursor state is page-local/canonical URL state, not shell navigation state.
- AD-3: use FrontComposer and Fluent first; a missing shared capability becomes an owned dependency or an explicitly approved fallback.
- AD-4: Tenants owns domain composition, not generic UI infrastructure — reusable grids, tabs, shell layout, theme primitives, or command chrome belong in FrontComposer or an approved fallback, not Tenants. **(2026-07-19 review patch: DI/transport-boundary rules are governed by AD-5 — server-side gateways are the only backend egress — and AD-13 — the UI host is domain-owned, orchestration is platform-owned — not AD-4.)**
- AD-5: UI components never call Tenants, EventStore, or Memories directly; all backend egress goes through the server-side command/query gateways.
- AD-7: command status and SignalR are nudges; confirmed truth comes from an authoritative projection re-query.
- AD-12: lock key is `(interactive circuit, AggregateIdentity)`, held through accepted/projection-pending until terminal evidence; unrelated aggregates proceed. `confirmed` requires the expected postcondition plus projection-version advancement or safe command-specific audit evidence beyond the pre-submit baseline; a pre-existing expected state or NoOp is `already applied`, never `confirmed`; unavailable provenance is `unable to verify`. **(2026-07-19 review patch: added the confirmed/already-applied/unable-to-verify clauses, which the original one-liner omitted despite being directly relevant to this story's own FC-CMD blocked finding.)**
- AD-13: `src/Hexalith.Tenants.UI` is a domain-owned, publishable app/container; distributed orchestration belongs to a platform/composing host, not this module.

[Source: `_bmad-output/planning-artifacts/architecture.md#Architecture Decision Records (ADRs)`; `_bmad-output/planning-artifacts/architecture.md#Core Architectural Decisions`]

### Current Source Map

- Host/bootstrap: `src/Hexalith.Tenants.UI/Program.cs`, `Components/Layout/MainLayout.razor`, `Composition/TenantsFrontComposerRegistration.cs`, and FrontComposer `Extensions/ServiceCollectionExtensions.cs` / `EventStoreServiceExtensions.cs`.
- Layout: FrontComposer `Components/Layout/FrontComposerShell.razor*`, `FcPageLayout.razor*`, `FcPageLayoutCoordinator.cs`, `FcAggregateListPage.razor*`, and `Contracts/Rendering/FcPageLayoutMode.cs`; Tenants page declarations and `PageLayoutDeclarationTests.cs`.
- Navigation: `TenantsFrontComposerRegistration.cs`, `TenantsWorkspace.razor`, `TenantsWorkspaceTests.cs`, and `TenantsUiCompositionTests.cs`.
- Lifecycle/concurrency: FrontComposer `Contracts/Lifecycle/CommandLifecycleState.cs`, `Components/Lifecycle/FcLifecycleWrapper.razor*`, `State/PendingCommands/*`, `Infrastructure/ProjectionConnection/*`, and generated `CommandFormEmitter.cs`; Tenants `State/TenantCommands/*`, command flow components, and command-state/component tests.
- Grid: FrontComposer `Components/DataGrid/*`, `State/DataGridNavigation/*`, and `IProjectionPageLoader`; Tenants `TenantDataGrid.razor`, `AuditDataGrid.razor`, `ListSurfaceStates.razor`, gateway cursor models, and grid/surface tests.
- A11Y/L10N/DOC: FrontComposer `wwwroot/js/fc-keyboard.js`, `fc-focus.js`, shell CSS, `CustomizationAccessibilityAnalyzer.cs`, `FcShellResources*.resx`, `docs/accessibility-verification/`, `docs/how-to/test-generated-components.md`, and `docs/reference/components/`.

### Fluent rc.4 Contract Matrix

Assembly inspection during story preparation found these exact rc.4 APIs. Re-run the commands and attach the implementation-time output; do not copy this table as sole proof.

- `BadgeColor`: `Brand`, `Danger`, `Important`, `Informative`, `Severe`, `Subtle`, `Success`, `Warning`.
- `DataGridColumnPin`: `None`, `Start`, `End`; `ColumnBase<T>.Pin` is typed as `DataGridColumnPin`.
- `FluentDataGrid<T>` exposes `AutoFocus`, `OnCellFocus`, `OnRowFocus`, keyboard handling, sorting, resizing, and item identity. Verify rendered/runtime behavior; parameter presence alone is insufficient.
- `FluentMessageBar` exposes `Layout`, `AriaLive`, `Intent`, `Title`, `ActionsTemplate`, `AllowDismiss`, and `Visible`; `MessageBarLayout.Notification` and `AriaLive.Polite`/`Assertive` exist.
- Required `Microsoft.FluentUI.AspNetCore.Components.Icons.Regular.Size20.*` types: `CheckmarkCircle`, `DocumentCheckmark`, `ArrowClockwise`, `Document`, `ClipboardClock`, `Clock`, `ClockDismiss`, `ClockWarning`, `Warning`, `ClockAlarm`, `Power`, `DocumentProhibited`, `ShieldError`, `Prohibited`, `DismissCircle`, `QuestionCircle`, `ShieldProhibited`, `ShieldQuestion`, `CheckmarkCircleHint`, `Shield`, and `ClockToolbox`.

The canonical state-to-role/icon mapping is in the UX design and must be verified as a whole. In particular, in-flight states use Informative/`ArrowClockwise`, audit pending uses Informative/`ClipboardClock`, confirmed uses Success/`CheckmarkCircle`, and audit available uses Success/`DocumentCheckmark`; those states must remain distinct. [Source: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md#status-badge`]

### Approved Fallback And Ownership Rules

- `FC-AUD`: flat Tenants `AuditDataGrid` is approved because no shared `<AuditTimeline>` contract exists.
- `FC-CNS`: inline full-content Consequence Preview is approved because no shared `<ConsequencePreview>` contract exists.
- `FC-CNC`: the approval remains relevant only as “one active command for the same aggregate”; its old circuit-global scope is superseded by AD-12.
- `FC-TOK`: use the canonical Tenants state vocabulary plus verified Fluent semantic roles/icons until a complete shared contract exists. Do not create names that look like FrontComposer or Fluent tokens.
- No FrontComposer, Builds, EventStore, or other root-declared submodule source may be changed in this story. A shared gap gets an owner and blocker record; remediation requires a separate authorized task in the owning repository.

[Source: `_bmad-output/planning-artifacts/fallback-approval-record-2026-06-03.md`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#B. FrontComposer dependency readiness`; `AGENTS.md`]

### Downstream Gate Impact

- Shell/navigation/layout failure blocks Stories 1.1-1.11 from claiming AD-1 through AD-4 conformance.
- FC-CMD, SignalR truth, audit-state, or FC-CNC failure blocks the command foundations and command/correction stories: 2.1-2.4, 3.1-3.6, 4.1-4.3, and 5.5-5.7.
- FC-TBL failure does not authorize a generic Tenants replacement; retain the explicit `TenantDataGrid` boundary and block only claims that depend on the missing reusable behavior.
- Fluent semantic/icon/focus/ARIA failure blocks any downstream story that renders the affected state or interaction until a verified mapping or separately approved conservative fallback exists.

### Testing Standards

Run from the Tenants repository root and record exact results. Keep test projects individual; do not run solution-level `dotnet test`.

```bash
git submodule status references/Hexalith.FrontComposer references/Hexalith.Builds
git -C references/Hexalith.FrontComposer describe --tags --always --dirty
git -C references/Hexalith.FrontComposer rev-parse HEAD
rg -n 'HexalithFrontComposerVersion|Microsoft\.FluentUI\.AspNetCore\.Components' references/Hexalith.Builds/Props/Directory.Packages.props
dotnet package list --project src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj --include-transitive --no-restore
dotnet package list --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --include-transitive --no-restore
dnx dotnet-inspect -y -- package Microsoft.FluentUI.AspNetCore.Components@5.0.0-rc.4-26180.1
dnx dotnet-inspect -y -- package Microsoft.FluentUI.AspNetCore.Components.Icons@5.0.0-rc.4-26180.1
dnx dotnet-inspect -y -- member Microsoft.FluentUI.AspNetCore.Components.BadgeColor --package Microsoft.FluentUI.AspNetCore.Components@5.0.0-rc.4-26180.1
dnx dotnet-inspect -y -- member Microsoft.FluentUI.AspNetCore.Components.DataGridColumnPin --package Microsoft.FluentUI.AspNetCore.Components@5.0.0-rc.4-26180.1
dnx dotnet-inspect -y -- member Microsoft.FluentUI.AspNetCore.Components.FluentMessageBar --package Microsoft.FluentUI.AspNetCore.Components@5.0.0-rc.4-26180.1
dnx dotnet-inspect -y -- member Microsoft.FluentUI.AspNetCore.Components.MessageBarLayout --package Microsoft.FluentUI.AspNetCore.Components@5.0.0-rc.4-26180.1
dnx dotnet-inspect -y -- member Microsoft.FluentUI.AspNetCore.Components.AriaLive --package Microsoft.FluentUI.AspNetCore.Components@5.0.0-rc.4-26180.1
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore
dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-build --no-restore
tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none
git diff --check
git diff --submodule=short -- references/Hexalith.FrontComposer references/Hexalith.Builds
```

- Run every required Size20 icon through `dotnet-inspect find <IconName> --package Microsoft.FluentUI.AspNetCore.Components.Icons@5.0.0-rc.4-26180.1 --table` and preserve the consolidated result.
- The `dotnet test` command may hit the repository's known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility. Record that exact failure and use the xUnit v3 executable fallback only after the Release test project builds successfully. **(2026-07-19 review patch: did not reproduce during the 2026-07-19 reverification run or the subsequent code-review spot-check re-run — `dotnet test ... --no-build --no-restore` and the xUnit v3 executable both passed 904/904 cleanly both times. Monitor for recurrence rather than assuming it is resolved.)**
- At minimum, report focused evidence for `TenantsUiCompositionTests`, `PageLayoutDeclarationTests`, `DomainUiFluentConformanceTests`, `CommandFlowGuardConformanceTests`, command snapshot tests, grid/surface tests, and the corresponding FrontComposer source tests inspected for bootstrap, lifecycle, admission, localization, layout, and accessibility.
- Do not weaken governance tests, broaden allowlists/budgets to hide failures, or mark a source inspection as a passing runtime test.

### Project Structure Notes

- New evidence belongs at `_bmad-output/implementation-artifacts/story-1-0-frontcomposer-fluent-reverification-2026-07-19.md`.
- If a confirmed Tenants boundary lacks an enduring guard, add the narrowest test under `tests/Hexalith.Tenants.UI.Tests/`; production changes are out of scope unless the story is separately corrected and authorized.
- Do not edit files under `references/Hexalith.FrontComposer`, `references/Hexalith.Builds`, or any other submodule. Do not initialize FrontComposer's nested submodules.
- Preserve unrelated working-tree changes. Re-run `git status --short` at implementation start and leave all pre-existing planning artifacts or other user-owned changes untouched.

### Latest Technical Information

- The central and resolved Fluent package version is `5.0.0-rc.4-26180.1`; NuGet lists that prerelease as published on 2026-06-29. Use the exact-version pages for [components](https://www.nuget.org/packages/Microsoft.FluentUI.AspNetCore.Components/5.0.0-rc.4-26180.1) and [icons](https://www.nuget.org/packages/Microsoft.FluentUI.AspNetCore.Components.Icons/5.0.0-rc.4-26180.1), plus assembly inspection, as version evidence.
- The package metadata points to official Fluent UI Blazor repository commit [`a6ec02a5d26b2c64c68180d8a662736b4cb18e4a`](https://github.com/microsoft/fluentui-blazor/tree/a6ec02a5d26b2c64c68180d8a662736b4cb18e4a). Use that commit or the installed assemblies for exact rc.4 API questions.
- The official [DataGrid documentation](https://fluentui-blazor.azurewebsites.net/datagrid) is useful for keyboard/resize behavior and cautions that rendered structure can change across versions. The official [MessageBar/MessageService documentation](https://fluentui-blazor.azurewebsites.net/MessageService) is useful for interactivity/provider behavior. Neither unversioned demo page supersedes rc.4 assembly evidence.

### References

- Story source: `_bmad-output/planning-artifacts/epics.md#Story 1.0: Reverify FrontComposer Shell and Fluent Contracts`
- Architecture: `_bmad-output/planning-artifacts/architecture.md#Architecture Decision Records (ADRs)`, `#Core Architectural Decisions`, `#Implementation Patterns & Consistency Rules`
- PRD: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md`
- PRD addendum: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#B. FrontComposer dependency readiness`
- UX design: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md`
- UX experience: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md`
- Historical spike: `_bmad-output/implementation-artifacts/story-1-0-spike-note-2026-06-05.md`
- Fallback approval: `_bmad-output/planning-artifacts/fallback-approval-record-2026-06-03.md`
- Readiness reconciliation: `_bmad-output/planning-artifacts/implementation-readiness-report-2026-07-19-v2.md`
- Project rules: `AGENTS.md`, `_bmad-output/project-context.md`, `references/Hexalith.FrontComposer/_bmad-output/project-context.md`

## Dev Agent Record

### Agent Model Used

OpenAI Codex (GPT-5)

### Debug Log References

- 2026-07-19 — Baseline red/green: confirmed the successor record was absent, captured the historical spike SHA-256, created the successor record, and validated all twelve gate rows.
- 2026-07-19 — Implementation-time baselines: Tenants `088232a`; FrontComposer source `d3761fa0` (`v4.0.1-76-gd3761fa0`); Builds source `9ec0a032` (`v4.21.7-10-g9ec0a03`); published FrontComposer package baseline `4.0.1`; resolved Fluent components/icons `5.0.0-rc.4-26180.1`.
- 2026-07-19 — Validation: Release UI-test project build succeeded with 0 warnings/0 errors; xUnit v3 executable completed 904/904 tests with 0 failed/skipped/not-run.
- 2026-07-19 — Shell/layout focus: Tenants composition 16/16 and layout 2/2 passed; FrontComposer bootstrap/layout classes passed 22/22, 4/4, 14/14, 7/7, and 12/12 after a pinned restore resolved the initial `NETSDK1004` assets prerequisite.
- 2026-07-19 — Command truth/concurrency: 124 Tenants focused checks and 50 FrontComposer pending-command checks passed; source checks proved aggregate identity and typed audit-available state are absent, so FC-CMD/FC-CNC remain blocked with circuit-global fail-closed serialization.
- 2026-07-19 — Accessibility/localization/docs: focused FrontComposer and Tenants classes passed (7, 70, 2, 5, 12, 30, 6, 51, and 16 tests); all cited current docs exist and no Storybook path was found.
- 2026-07-19 — Grid/fallback/token verification: 211 focused grid checks and 84 fallback/rendering checks passed. FC-TBL remains offset-based/changed; FC-AUD and FC-CNS fallbacks are verified; FC-TOK is a real but partial six-slot/Size16 contract.
- 2026-07-19 — Exact Fluent rc.4: assembly/public-surface inspection plus exact packaged DataGrid code verified the required colors, pinning, MessageBar/ARIA properties, 21 Size20 icons, autofocus, arrow navigation, resize/sort-reset, and reorder keyboard behavior.
- 2026-07-19 — Final gate: Release build succeeded with 0 warnings/errors; both `dotnet test` and the xUnit v3 executable passed all 904 tests; diff, historical-hash, and clean-submodule checks passed.

### Completion Notes List

- Created the dated successor evidence without modifying the June historical spike; recorded source/package distinctions, exact rc.4 package provenance, and explicit verified/changed/blocked decisions with owners, downstream impact, and conservative behavior.
- Reverified AD-1 through AD-4: supported bootstrap and optional security/EventStore order, exactly one `/tenants` shell entry, FrontComposer-owned shell/page measures, and no Tenants-owned generic shell/layout/registration replacement.
- Reverified AD-7/AD-12 command truth and locking. FC-CMD is blocked by SignalR-driven lifecycle advancement and missing typed audit availability; FC-CNC is blocked by FrontComposer's circuit-global admission gate and absent aggregate identity. Affected command stories remain unpromoted.
- Verified current accessibility primitives/diagnostics, shell-versus-domain localization ownership and EN/FR guards, and current documentation paths. No Storybook evidence is claimed.
- Reverified FC-TBL without broadening Tenants: protected cursor and non-collapsing state behavior remain local, while FrontComposer's reusable loader is offset-based and the tenant freshness safety column is not pinned.
- Confirmed only the flat audit grid and inline full-content consequence preview remain approved local fallbacks. Classified FC-TOK as changed/partial, with exact rc.4 API availability separated from current Size16/icon-label/MessageBar rendering findings.
- Issued a complete-with-blocked-contracts gate decision: verified contracts may be consumed subject to story-local evidence; FC-CMD and FC-CNC do not promote command stories, and FC-TBL/FC-TOK permit only their documented conservative boundaries.

### File List

- `_bmad-output/implementation-artifacts/1-0-reverify-frontcomposer-shell-and-fluent-contracts.md`
- `_bmad-output/implementation-artifacts/story-1-0-frontcomposer-fluent-reverification-2026-07-19.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Change Log

- 2026-07-19 - Created the corrected Story 1.0 reverification handoff against FrontComposer source `064b886d`, package baseline `4.0.1`, Fluent UI rc.4, AD-1 through AD-4, AD-7, and AD-12.
- 2026-07-19 - Established the implementation-time baseline and successor gate record against FrontComposer source `d3761fa0`, Builds source `9ec0a032`, and resolved Fluent UI rc.4 assemblies.
- 2026-07-19 - Completed contract reverification, recorded verified/changed/blocked promotion gates, and moved the story to review after all focused and full UI checks passed.
