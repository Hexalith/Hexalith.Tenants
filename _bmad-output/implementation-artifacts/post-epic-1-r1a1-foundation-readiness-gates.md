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

## Tasks / Subtasks

- [ ] Task 1: Restore and test gate recovery (AC: 1)
  - [ ] 1.1: Identify the source of `OpenTelemetry.Api` and `OpenTelemetry.Exporter.OpenTelemetryProtocol` 1.15.1 in the EventStore submodule dependency graph.
  - [ ] 1.2: Prefer updating the root-level EventStore submodule to a safe revision with patched dependencies, if available.
  - [ ] 1.3: If no upstream-safe revision is available, document and apply the narrowest mitigation that restores verification without hiding unrelated audit warnings.
  - [ ] 1.4: Run `dotnet test Hexalith.Tenants.slnx --configuration Release` and capture the result in the Dev Agent Record.
- [ ] Task 2: Story 1.3 release-language synchronization (AC: 2)
  - [ ] 2.1: Update Story 1.3 acceptance criteria and notes to describe semantic-release on `main`.
  - [ ] 2.2: Remove or correct stale tag-triggered release language unless it is explicitly describing historical context.
- [ ] Task 3: Package-count validation gate (AC: 3)
  - [ ] 3.1: Add a validation step to the semantic-release path that checks exactly these package IDs: `Hexalith.Tenants.Contracts`, `Hexalith.Tenants.Client`, `Hexalith.Tenants.Server`, `Hexalith.Tenants.Testing`, and `Hexalith.Tenants.Aspire`.
  - [ ] 3.2: Ensure validation runs before any NuGet push.
- [ ] Task 4: DAPR workflow alignment (AC: 4)
  - [ ] 4.1: Determine whether Tier 2 and release tests currently require DAPR runtime initialization.
  - [ ] 4.2: If DAPR is required, add explicit DAPR initialization before those tests.
  - [ ] 4.3: If DAPR is not required, update story/workflow documentation to explain the current test boundary.
- [ ] Task 5: Submodule policy alignment (AC: 5)
  - [ ] 5.1: Replace recursive nested submodule initialization with root-level-only initialization where sufficient.
  - [ ] 5.2: Document any remaining recursive usage with a concrete reason.
- [ ] Task 6: Final tracking update (AC: 6)
  - [ ] 6.1: Update this story status to `done` when all acceptance criteria are met.
  - [ ] 6.2: Update `sprint-status.yaml` for `post-epic-1-r1a1-foundation-readiness-gates`.

## Dev Notes

- Triggering source: `_bmad-output/implementation-artifacts/epic-1-retro-2026-05-12.md`.
- Approved proposal: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-12.md`.
- Keep the correction narrow: restore health, release validation, CI alignment, story documentation, and submodule policy.
- Do not reopen Epic 1. This is an explicit post-epic carry-forward.
- Follow repository guidance: initialize/update only root-level submodules by default; do not initialize nested submodules recursively unless explicitly required.
- Commit messages must follow Conventional Commits.

## Dev Agent Record

### Agent Model Used

TBD

### Debug Log References

TBD

### Completion Notes List

TBD

### File List

TBD
