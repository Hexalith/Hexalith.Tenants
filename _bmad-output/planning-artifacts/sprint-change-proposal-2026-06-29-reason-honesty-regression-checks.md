---
title: "Sprint Change Proposal - Command-Surface Reason-Honesty Regression Checks"
date: "2026-06-29"
project_name: "Hexalith.Tenants"
author: "Correct Course workflow"
trigger: "epic-3-retro-2026-06-29-reason-honesty"
mode: "Batch"
scope_classification: "Minor"
status: "APPROVED"
approval:
  approved_by: "Administrator"
  approved_at: "2026-06-29T17:01:25+02:00"
  approval_note: "Approved with 'approve'."
---

# Sprint Change Proposal: Command-Surface Reason-Honesty Regression Checks

## 1. Issue Summary

The active change trigger is the Epic 3 retrospective action item:

```text
Add reason-honesty regression checks for degraded/unavailable/unknown projection states on command surfaces.
```

The issue is not a missing product requirement. The PRD, UX, architecture, and story records already
require fail-closed command surfaces, visible unavailable reasons, projection-confirmed success, and
honest separation between data availability, authorization, command lifecycle, and audit proof.

The gap is regression durability. Epic 3 found that degraded, unavailable, and unknown projection
states were sometimes mapped to permission failures. Story 3.1 and Story 3.3 fixed that locally, but
the open retrospective action item shows the lesson has not yet been promoted into a command-surface
regression sweep.

Evidence reviewed:

- `_bmad-output/implementation-artifacts/sprint-status.yaml` keeps
  `epic-3-retro-2026-06-29-reason-honesty` open.
- `_bmad-output/implementation-artifacts/epic-3-retro-2026-06-29.md` records that Story 3.1 and
  Story 3.3 both needed reason-honesty fixes.
- Story 3.1 review fixed degraded/unavailable/unknown detail surfaces that were incorrectly
  surfaced as missing permission.
- Story 3.3 review fixed degraded projection-state copy for set-configuration.
- Current PRD and architecture evidence already name reason-honesty for degraded, unavailable, and
  unknown projection states as part of UI acceptance evidence.

Problem statement:

Command surfaces must not tell users they lack permission when the actual blocking condition is data
availability, projection degradation, unknown freshness, unavailable read support, or unverifiable
projection evidence. Missing-permission copy is correct only when authorization is actually the
failing gate.

## 2. Impact Analysis

### Epic Impact

Epic 3 remains done. This is retrospective follow-through, not reopened FR15/FR16/FR17 scope.

The regression checks are cross-epic because the same command-surface honesty invariant applies to:

- Epic 2 tenant create, member, and metadata command flows.
- Epic 3 lifecycle and configuration command flows.
- Epic 4 global-administrator grant/remove command flows.
- Epic 5 correction command flows where current projection evidence gates correction availability.

No new epic is required.

### Story Impact

Affected completed story areas:

- Story 2.1 create tenant with projection-confirmed lifecycle.
- Story 2.2 add user to tenant.
- Story 2.3 change member role.
- Story 2.4 remove member.
- Story 2.5 edit tenant metadata.
- Story 3.1 lifecycle action availability.
- Story 3.2 lifecycle enable/disable.
- Story 3.3 set configuration.
- Story 3.4 remove configuration.
- Story 4.3 grant global administrator.
- Story 4.4 remove global administrator.
- Story 5.5 through Story 5.8 correction surfaces where correction availability depends on current
  projection evidence.

No completed story needs product redefinition. The implementation should be a focused follow-up
artifact tied to the Epic 3 retrospective action item.

### Artifact Conflicts

PRD: no change required. It already lists reason-honesty for degraded, unavailable, and unknown
projection states in acceptance evidence.

Architecture: no change required. It already lists "data unavailable but not authorization-denied"
and command-surface proof expectations in pattern enforcement.

UX: no change required. `EXPERIENCE.md` already requires fail-closed gating, inline-visible reasons,
recovery mapping, and no false success.

Epics/story guardrails: no broad rewrite required. The existing story guardrails are sufficient, but
the implementation handoff should add a focused regression artifact or test task so the open action
item can close with executable evidence.

Sprint status: keep the action item open until regression checks are added and pass. Mark it `done`
only after validation evidence names the relevant test files.

### Technical Impact

UI test and possibly small UI copy/state mapping change. No backend endpoint, command contract,
projection, AppHost, DAPR, FrontComposer shared module, package, data persistence, or submodule edit
is proposed.

Expected implementation impact:

- Add or extend focused bUnit/state tests under `tests/Hexalith.Tenants.UI.Tests`.
- Prefer existing command-flow test files when they already own the surface; add a small
  Tenants-specific matrix/helper only if it reduces real duplication.
- Patch any command-surface mapping that still uses missing-permission copy for data unavailable,
  degraded, unknown, or unverifiable projection states.
- Update `tests/test-summary.md` only if the repository continues current evidence-summary practice.
- Mark `epic-3-retro-2026-06-29-reason-honesty` done only after the checks pass.

## 3. Recommended Approach

Recommended path: **Direct Adjustment**.

Rationale:

- The product and architecture requirements already exist and are correct.
- Rollback would not help; this is a regression-guard gap.
- MVP scope and command behavior do not change.
- The work is bounded to test hardening plus any uncovered local mapping fixes.
- The change belongs in Tenants UI command-surface tests, not in shared technical modules.

Effort estimate: Medium-low. The work touches several test files, but each assertion is small.

Risk level: Low to medium. The main risk is over-normalizing reasons and accidentally hiding true
authorization failures. The regression matrix must include both "data unavailable" and "actual
permission missing" controls.

Timeline impact: Minimal. Route directly to Developer agent with Test Architect review.

## 4. Detailed Change Proposals

### Proposal A - Add a Focused Reason-Honesty Regression Matrix

Artifacts:

- `tests/Hexalith.Tenants.UI.Tests/Components/*FlowTests.cs`
- optional focused file: `tests/Hexalith.Tenants.UI.Tests/Components/CommandSurfaceReasonHonestyTests.cs`

OLD:

```text
Reason-honesty coverage exists mostly as story-local tests. Story 3.1 and Story 3.3 fixed known
incorrect mappings, but there is no command-surface regression sweep that proves degraded,
unavailable, and unknown projection states stay distinct from missing-permission failures.
```

NEW:

```text
Add a focused regression matrix covering representative command surfaces:

- tenant/member command surfaces: add, change role, remove, edit metadata;
- lifecycle/configuration command surfaces: enable/disable, set configuration, remove configuration;
- global-administrator command surfaces: grant/remove;
- correction command surfaces where current projection evidence gates correction availability.

For each applicable surface, assert:

- degraded projection/read state shows degraded or data-unavailable copy;
- unavailable projection/read support shows unavailable or continue-read-only copy;
- unknown freshness/projection state shows refresh/unknown-data copy;
- missing permission copy appears only when authorization is the failing gate;
- the command trigger remains unavailable and no command is submitted;
- no state is rendered as Success, confirmed, or audit available.
```

Rationale: The repeated Epic 3 failures were not isolated to one component type. A matrix makes the
distinction executable across command surfaces.

### Proposal B - Keep Canonical Reasons Honest

Artifacts:

- `src/Hexalith.Tenants.UI/State/**`
- `src/Hexalith.Tenants.UI/Components/**`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx`

OLD:

```text
Some existing flows already map data-availability failures correctly. Past review evidence shows
other flows previously mapped degraded/unavailable/unknown projection states to permission copy.
```

NEW:

```text
Where tests expose a remaining mismatch, map causes as follows:

- authorization denied or indeterminate authorization -> `missing permission`;
- stale or unknown freshness -> `stale data` / refresh-first copy;
- degraded projection/read surface -> degraded/read-degraded copy;
- unavailable read/projection support -> unavailable/support copy;
- command gateway unavailable -> missing lifecycle/command support copy;
- unverifiable projection evidence after command status -> unable-to-verify recovery copy.

Keep EN/FR resources in parity and keep all copy support-safe.
```

Rationale: Reason categories must guide recovery. A data-availability problem sends the user to
refresh, retry, continue read-only, or escalate; a permission problem sends the user to request
permission or escalate.

### Proposal C - Add Negative Controls for True Authorization Failures

Artifacts:

- the same test files touched by Proposal A.

OLD:

```text
Tests often cover stale/unknown or unauthorized cases independently, but the reason-honesty action
requires proving the two classes are not conflated.
```

NEW:

```text
For each representative surface, pair at least one data-availability case with a true authorization
case:

- data degraded/unavailable/unknown must not include missing-permission copy;
- true missing permission must still include missing-permission copy;
- tests must not assert only generic "unavailable" text.
```

Rationale: This prevents a bad fix that removes permission copy entirely or maps everything to stale
data.

### Proposal D - Sprint Status Closure Criteria

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

OLD:

```yaml
- id: epic-3-retro-2026-06-29-reason-honesty
  epic: 3
  action: "Add reason-honesty regression checks for degraded/unavailable/unknown projection states on command surfaces."
  owner: "Murat (Test Architect) and Amelia (Developer)"
  status: open
  source: "_bmad-output/implementation-artifacts/epic-3-retro-2026-06-29.md"
```

NEW after implementation and validation:

```yaml
- id: epic-3-retro-2026-06-29-reason-honesty
  epic: 3
  action: "Add reason-honesty regression checks for degraded/unavailable/unknown projection states on command surfaces."
  owner: "Murat (Test Architect) and Amelia (Developer)"
  status: done
  source: "_bmad-output/implementation-artifacts/epic-3-retro-2026-06-29.md"
```

Do not mark this item done merely because this proposal exists. Close it only after:

- focused tests are committed;
- any exposed mapping fixes are committed;
- the UI test project builds cleanly;
- the xUnit v3 UI executable or equivalent focused test run passes;
- validation evidence names the exact test files.

Rationale: The action item asks for regression checks, not planning text.

## 5. Checklist Results

- [x] 1.1 Triggering context identified: Epic 3 retrospective action item
  `epic-3-retro-2026-06-29-reason-honesty`.
- [x] 1.2 Core problem defined: regression durability gap for unavailable-reason honesty on command
  surfaces.
- [x] 1.3 Evidence gathered from sprint status, Epic 3 retrospective, PRD, epics, architecture, UX,
  story records, and current test-summary evidence.
- [x] 2.1 Current epic assessed: Epic 3 can remain done.
- [x] 2.2 Epic-level change identified: focused cross-command regression checks, no new epic.
- [x] 2.3 Remaining epics reviewed: Epics 2, 4, and 5 command/correction surfaces consume the same
  invariant.
- [N/A] 2.4 No future epic invalidated and no new epic required.
- [N/A] 2.5 No epic resequencing required.
- [x] 3.1 PRD checked: requirement already present; no PRD update required.
- [x] 3.2 Architecture checked: pattern enforcement already present; no architecture update required.
- [x] 3.3 UX checked: fail-closed reason behavior already present; no UX update required.
- [!] 3.4 Secondary artifact action: add tests and close sprint-status item only after validation.
- [x] 4.1 Direct Adjustment viable: medium-low effort, low-to-medium risk.
- [N/A] 4.2 Rollback not useful.
- [N/A] 4.3 MVP review not needed.
- [x] 4.4 Recommended path selected: Direct Adjustment.
- [x] 5.1-5.5 Issue summary, impact, recommendation, detailed proposals, and handoff defined.
- [x] 6.3 User approval received from Administrator on 2026-06-29T17:01:25+02:00.
- [N/A] 6.4 Sprint status update deferred until implementation evidence exists.

## 6. Implementation Handoff

Scope classification: **Minor**.

Route to: Developer agent with Test Architect review.

Developer responsibilities:

- Add focused reason-honesty tests in existing command-flow test files or one small
  Tenants-specific matrix test.
- Fix any command-surface mapping still conflating data-availability failures with missing
  permission.
- Preserve support-safe copy, EN/FR resource parity, stable selectors, no-color-only status, and
  fail-closed command admission.
- Run the focused UI test project build and xUnit v3 UI executable fallback if `dotnet test` hits the
  known .NET 10 Microsoft.Testing.Platform/VSTest issue.
- Update validation evidence with exact commands and test counts.

Test Architect responsibilities:

- Verify the regression matrix includes both data-availability cases and true authorization negative
  controls.
- Confirm tests do not rely on row text, color only, or incidental Fluent-generated markup.
- Confirm no command surface shows Success, confirmed, or audit available for degraded,
  unavailable, unknown, or unable-to-verify projection states.

Success criteria:

- Degraded/unavailable/unknown projection states on command surfaces render data-availability or
  refresh/retry/escalation reasons, not missing-permission reasons.
- Missing-permission reasons still render for true authorization failures.
- No command submission occurs while the action is blocked by data availability.
- The UI test project passes the focused validation run.
- `epic-3-retro-2026-06-29-reason-honesty` can be marked `done` with validation evidence.

## 7. Review Prompt

Review this proposal. Continue to implementation with this direct-adjustment plan, or revise the
regression matrix before handoff.
