# Sprint Change Proposal: Post-Epic-1 Foundation Readiness Fix

Date: 2026-05-12
Project: Hexalith.Tenants
Prepared by: Bob (Scrum Master)
Mode: Batch
Approval status: Approved

## 1. Issue Summary

Epic 1 completed the project foundation, but the Epic 1 retrospective found that the current foundation quality gate is not green. A fresh verification run:

```powershell
dotnet test Hexalith.Tenants.slnx --configuration Release
```

fails during restore because EventStore submodule projects treat NuGet vulnerability warnings as errors for:

- `OpenTelemetry.Api` 1.15.1
- `OpenTelemetry.Exporter.OpenTelemetryProtocol` 1.15.1

The same retrospective also found alignment issues in the foundation artifacts:

- Story 1.3 still contains stale tag-release language, while the actual release workflow runs semantic-release on pushes to `main`.
- `.releaserc.json` publishes packages, but the release path does not visibly enforce the expected package count of 5 before publishing.
- Story 1.3 says DAPR initialization is required for Tier 2/release validation, but `.github/workflows/ci.yml` and `.github/workflows/release.yml` do not initialize DAPR before Server.Tests.
- Local AGENTS.md requires avoiding nested recursive submodule initialization unless explicitly requested, but current story/workflow guidance uses recursive submodule language.

### Trigger

Triggering artifact: `epic-1-retro-2026-05-12.md`

Triggering issue type: Technical limitation discovered during verification, plus artifact drift between story records, workflows, and repository policy.

Core problem statement: Epic 1 is complete in sprint tracking, but the foundation is not currently verifiable and the CI/release documentation does not fully match the actual implementation.

## 2. Checklist Findings

| Checklist Item | Status | Finding |
| --- | --- | --- |
| 1.1 Triggering story identified | Done | Trigger emerged from Epic 1 retrospective, primarily affecting Story 1.3 and foundation readiness. |
| 1.2 Core problem defined | Done | Restore is blocked by dependency audit warnings; CI/release/submodule guidance has drifted. |
| 1.3 Evidence gathered | Done | `dotnet test` restore failure; current `ci.yml`, `release.yml`, `.releaserc.json`, sprint status, and retro reviewed. |
| 2.1 Current epic viability | Done | Epic 1 remains complete, but needs a post-epic carry-forward fix. |
| 2.2 Epic-level changes | Action-needed | Add a post-Epic-1 foundation readiness correction story. |
| 2.3 Future epic impact | Done | Epic 2 and later work depend on restore/test/release gates being trustworthy. |
| 2.4 Future epic invalidation | Done | No future epic is invalidated. |
| 2.5 Epic order or priority | Action-needed | Correction should be handled before further domain implementation relies on the foundation gate. |
| 3.1 PRD conflicts | Done | MVP remains achievable; FR58 quality gates need implementation alignment. |
| 3.2 Architecture conflicts | Action-needed | CI/CD and EventStore dependency guidance need small clarifications. |
| 3.3 UI/UX conflicts | N/A | No UI/UX impact. |
| 3.4 Other artifacts | Action-needed | Update workflows, semantic-release package validation, story records, and sprint status after approval. |
| 4.1 Direct adjustment | Viable | One carry-forward story can resolve the issue. Effort: Medium. Risk: Medium. |
| 4.2 Rollback | Not viable | Reverting Epic 1 would remove useful foundation work and would not resolve upstream dependency health. |
| 4.3 MVP review | Not viable | Scope does not require MVP reduction. |
| 4.4 Recommended path | Done | Direct Adjustment. |
| 5.1-5.5 Proposal components | Done | Included below. |
| 6.1-6.5 Final review/handoff | Action-needed | User approval required before sprint-status or story files are changed. |

## 3. Impact Analysis

### Epic Impact

Epic 1: Completed, but requires a post-epic correction to restore foundation readiness.

Epic 2: Affected as a dependency consumer. Contracts, aggregates, bootstrap, and event publishing work should not proceed on a red restore/test gate.

Epics 3-8: No direct scope change. They benefit from the corrected foundation and release gates.

### Story Impact

Story 1.3 requires documentation synchronization with actual semantic-release behavior and validation gates.

A new carry-forward story should be added:

`post-epic-1-r1a1-foundation-readiness-gates`

### Artifact Conflicts

PRD: No requirement changes needed. FR58 still applies and is strengthened by this correction.

Architecture: Small clarification needed around CI/CD, EventStore dependency health, and root-level submodule initialization policy.

UX Design: No changes.

CI/CD: Workflow behavior must be reconciled with story requirements.

Semantic Release: Package-count validation should be added or documented as an explicit release gate.

Sprint Status: Add a post-Epic-1 carry-forward story after approval.

## 4. Recommended Approach

Recommended path: Direct Adjustment

Rationale:

- The issue is real and blocks current verification, but it does not change the product goal or domain model.
- The fix is isolated to foundation readiness: dependency health, workflow alignment, release validation, and documentation synchronization.
- A single carry-forward story preserves Epic 1 as complete while making the unresolved work visible and trackable.
- Rollback would discard useful foundation work and still leave the dependency vulnerability problem unresolved.
- MVP review is unnecessary because no functional requirement is invalidated.

Change scope classification: Moderate

Handoff:

- Bob (Scrum Master): update story/sprint artifacts after approval.
- Charlie (Senior Dev): resolve restore failure and workflow implementation.
- Dana (QA Engineer): verify release/package validation and test evidence.
- Winston (Architect): confirm DAPR and submodule policy alignment.

## 5. Detailed Change Proposals

### Proposal A: Add Post-Epic-1 Carry-Forward Story

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

Section: after Epic 1 retrospective entry

OLD:

```yaml
  epic-1-retrospective: done
```

NEW:

```yaml
  epic-1-retrospective: done

  # Post-Epic-1 Foundation Readiness Correction (SCP-2026-05-12)
  # R1-A1: Restore/test gate blocked by EventStore OpenTelemetry vulnerability warnings;
  # CI/release/story artifacts need foundation alignment before further dependency-heavy work.
  post-epic-1-r1a1-foundation-readiness-gates: ready-for-dev
```

Rationale: The correction is important enough to be tracked explicitly without reopening Epic 1.

### Proposal B: Create Story File For Carry-Forward

New artifact: `_bmad-output/implementation-artifacts/post-epic-1-r1a1-foundation-readiness-gates.md`

Proposed story:

```markdown
# Post-Epic-1 R1-A1: Foundation Readiness Gates

Status: ready-for-dev

## Story

As a developer,
I want restore, CI, release, and submodule guidance to match the current repository reality,
so that Epic 2 and later work can rely on a trustworthy foundation.

## Acceptance Criteria

1. Given the current solution, when `dotnet test Hexalith.Tenants.slnx --configuration Release` is executed, then restore reaches test execution without NuGet vulnerability warnings being promoted to errors by inherited EventStore dependencies.
2. Given the release path uses semantic-release on `main`, when Story 1.3 and release documentation are inspected, then they describe semantic-release behavior instead of stale tag-triggered release behavior.
3. Given semantic-release publishes NuGet packages, when the release path runs, then exactly 5 expected package IDs are validated before any NuGet push occurs.
4. Given Tier 2 or release tests require DAPR runtime support, when CI/release workflows run those tests, then DAPR is initialized before the tests; otherwise the story documentation explicitly states why DAPR is not required.
5. Given the repository uses a root-level EventStore submodule, when workflow and developer setup guidance initializes submodules, then it avoids nested recursive submodule initialization unless explicitly required.
6. Given the correction is complete, when sprint-status is inspected, then this carry-forward story is marked `done`.

## Implementation Notes

- Prefer updating the EventStore submodule to a safe root-level revision with patched OpenTelemetry dependencies if available.
- If no upstream-safe revision is available, document the chosen mitigation explicitly and keep warning suppression narrowly scoped.
- Prefer `submodules: true` over recursive nested submodule checkout if only the root-level EventStore submodule is required.
- Keep all commit messages Conventional Commits compliant.
```

Rationale: The story gives development and QA a concrete unit of work with verifiable acceptance criteria.

### Proposal C: Align Story 1.3 Release Language

Artifact: `_bmad-output/implementation-artifacts/1-3-ci-cd-pipeline.md`

Section: Acceptance Criteria and release workflow notes

OLD:

```markdown
Given a developer pushes a tag matching `v*` ...
When the release workflow (`release.yml`) triggers ...
Then it executes the full test suite, packs all 5 NuGet packages ...
```

NEW:

```markdown
Given a developer merges releasable Conventional Commits to `main`
When the release workflow (`release.yml`) runs semantic-release
Then semantic-release determines the next SemVer version, runs the configured validation gates, packs all 5 NuGet packages, validates the expected package count, publishes to NuGet.org, creates a GitHub Release, and updates CHANGELOG.md.
```

Rationale: The repository now uses semantic-release on `main`; story language must match the actual delivery mechanism.

### Proposal D: Add Package Count Validation To Semantic Release Path

Artifact: `.releaserc.json` or a repository script invoked by `.releaserc.json`

Section: `@semantic-release/exec`

OLD:

```json
"prepareCmd": "dotnet build --configuration Release -p:Version=${nextRelease.version} && dotnet pack --no-build --configuration Release --output ./nupkgs -p:Version=${nextRelease.version}",
"publishCmd": "dotnet nuget push ./nupkgs/*.nupkg --source https://api.nuget.org/v3/index.json --api-key $NUGET_API_KEY --skip-duplicate --verbosity quiet"
```

NEW:

```json
"prepareCmd": "dotnet build --configuration Release -p:Version=${nextRelease.version} && dotnet pack --no-build --configuration Release --output ./nupkgs -p:Version=${nextRelease.version} && <validate 5 expected Hexalith.Tenants packages>",
"publishCmd": "dotnet nuget push ./nupkgs/*.nupkg --source https://api.nuget.org/v3/index.json --api-key $NUGET_API_KEY --skip-duplicate --verbosity quiet"
```

Rationale: FR58 and Story 1.3 expect package validation before publishing. The release path should fail closed if package output is wrong.

### Proposal E: Reconcile DAPR Requirement

Artifact: `.github/workflows/ci.yml`, `.github/workflows/release.yml`, and Story 1.3

OLD:

```markdown
DAPR init is required for Tier 2 and release validation.
```

Current implementation:

```yaml
- name: Integration Tests (Tier 2)
  run: >
    dotnet test
    tests/Hexalith.Tenants.Server.Tests/
```

NEW option 1:

```yaml
- name: Install DAPR CLI and initialize runtime
  run: |
    <install dapr cli>
    dapr init

- name: Integration Tests (Tier 2)
  run: >
    dotnet test
    tests/Hexalith.Tenants.Server.Tests/
```

NEW option 2:

```markdown
Tier 2 tests do not currently require DAPR initialization because they do not start DAPR-dependent runtime paths. DAPR initialization remains required only for tests that exercise DAPR sidecars or Aspire topology.
```

Rationale: The team should pick one truth and make workflow behavior and story criteria agree.

### Proposal F: Reconcile Submodule Policy

Artifacts: `.github/workflows/*.yml`, Story 1.3, AGENTS.md guidance

OLD:

```yaml
submodules: recursive
```

NEW:

```yaml
submodules: true
```

Rationale: The repository needs the root-level EventStore submodule, but local policy explicitly avoids nested recursive submodules unless requested. If nested submodules become required, that requirement should be stated explicitly in the story and workflow comments.

## 6. Implementation Handoff

Scope: Moderate

Recommended routing:

- Development team: implement restore/dependency fix, package validation, workflow alignment.
- Scrum Master: add the carry-forward story and update sprint status once approved.
- QA Engineer: re-run restore/test, verify package-count validation, verify workflow/stories are synchronized.
- Architect: review the chosen DAPR and submodule policy decision.

Success criteria:

- `dotnet test Hexalith.Tenants.slnx --configuration Release` reaches test execution and either passes or fails on test assertions rather than restore/audit infrastructure.
- Release path validates exactly 5 expected packages before pushing to NuGet.
- Story 1.3 no longer contradicts the semantic-release workflow.
- CI/release DAPR behavior is intentional and documented.
- Submodule initialization follows root-level-only policy unless nested submodules are explicitly justified.

## 7. Approval Request

Bob (Scrum Master): "Jerome approved this proposal. The correction is contained, visible, and ready for implementation handoff."

Approval options:

- Approved: add the post-Epic-1 story and route to development.
- Implementation routing: development team, with QA and architecture review on the verification gates.
