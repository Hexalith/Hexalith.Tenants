# Post-Epic-1 R1-A1: Foundation Readiness Gates

Status: done

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

- [x] Task 1: Restore and test gate recovery (AC: 1)
  - [x] 1.1: Identify the source of `OpenTelemetry.Api` and `OpenTelemetry.Exporter.OpenTelemetryProtocol` 1.15.1 in the EventStore submodule dependency graph.
  - [x] 1.2: Prefer updating the root-level EventStore submodule to a safe revision with patched dependencies, if available.
  - [x] 1.3: If no upstream-safe revision is available, document and apply the narrowest mitigation that restores verification without hiding unrelated audit warnings.
  - [x] 1.4: Run `dotnet test Hexalith.Tenants.slnx --configuration Release` and capture the result in the Dev Agent Record.
- [x] Task 2: Story 1.3 release-language synchronization (AC: 2)
  - [x] 2.1: Update Story 1.3 acceptance criteria and notes to describe semantic-release on `main`.
  - [x] 2.2: Remove or correct stale tag-triggered release language unless it is explicitly describing historical context.
- [x] Task 3: Package-count validation gate (AC: 3)
  - [x] 3.1: Add a validation step to the semantic-release path that checks exactly these package IDs: `Hexalith.Tenants.Contracts`, `Hexalith.Tenants.Client`, `Hexalith.Tenants.Server`, `Hexalith.Tenants.Testing`, and `Hexalith.Tenants.Aspire`.
  - [x] 3.2: Ensure validation runs before any NuGet push.
- [x] Task 4: DAPR workflow alignment (AC: 4)
  - [x] 4.1: Determine whether Tier 2 and release tests currently require DAPR runtime initialization.
  - [x] 4.2: If DAPR is required, add explicit DAPR initialization before those tests.
  - [x] 4.3: If DAPR is not required, update story/workflow documentation to explain the current test boundary.
- [x] Task 5: Submodule policy alignment (AC: 5)
  - [x] 5.1: Replace recursive nested submodule initialization with root-level-only initialization where sufficient.
  - [x] 5.2: Document any remaining recursive usage with a concrete reason.
- [x] Task 6: Final tracking update (AC: 6)
  - [x] 6.1: Update this story status to `done` when all acceptance criteria are met.
  - [x] 6.2: Update `sprint-status.yaml` for `post-epic-1-r1a1-foundation-readiness-gates`.

## Dev Notes

- Triggering source: `_bmad-output/implementation-artifacts/epic-1-retro-2026-05-12.md`.
- Approved proposal: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-12.md`.
- Keep the correction narrow: restore health, release validation, CI alignment, story documentation, and submodule policy.
- Do not reopen Epic 1. This is an explicit post-epic carry-forward.
- Follow repository guidance: initialize/update only root-level submodules by default; do not initialize nested submodules recursively unless explicitly required.
- Commit messages must follow Conventional Commits.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Resolved BMAD customization: no prepend/append steps; persistent project context loaded from Commons and EventStore project-context files.
- OpenTelemetry source: `Hexalith.EventStore/src/Hexalith.EventStore.ServiceDefaults/Hexalith.EventStore.ServiceDefaults.csproj` directly references `OpenTelemetry.Exporter.OpenTelemetryProtocol`; current EventStore `Directory.Packages.props` pins `OpenTelemetry.Exporter.OpenTelemetryProtocol` and transitive `OpenTelemetry.Api` at 1.15.3, not 1.15.1.
- `dotnet list Hexalith.Tenants.slnx package --include-transitive` showed `OpenTelemetry.Api`, `OpenTelemetry.Api.ProviderBuilderExtensions`, and `OpenTelemetry.Exporter.OpenTelemetryProtocol` at 1.15.3.
- `dotnet test Hexalith.Tenants.slnx --configuration Release` reached test execution with no NuGet audit warnings promoted to errors. Result: failed after restore/build because local Aspire topology tests could not locate the DAPR CLI and `SnapshotPerformanceTests.ColdStartRehydration_CompletesWithin30Seconds_With500KEvents` hit `ConfigurationLimitExceededRejection` at event 301.
- Red validation: `python scripts\validate-nuget-packages.py nupkgs` failed because the stale local `nupkgs/` directory contained 6 packages, including unexpected `Hexalith.EventStore.Admin.Server`.
- Release pack validation: solution-level `dotnet pack --no-build --configuration Release --output <temp> -p:Version=1.2.3` also produced the unexpected EventStore Admin package, confirming the release path needed a targeted pack command.
- Green validation: `python scripts\pack-release-packages.py <temp> 1.2.3; python scripts\validate-nuget-packages.py <temp>` passed and validated exactly the five expected Tenants packages.
- Syntax validation: `python -m json.tool .releaserc.json` and `python -m py_compile scripts\pack-release-packages.py scripts\validate-nuget-packages.py` passed.
- Tier 1+2 release test slice passed: Contracts.Tests 34/34, Client.Tests 48/48, Testing.Tests 89/89, Sample.Tests 17/17, Server.Tests 261/261.

### Completion Notes List

- Restore/test gate recovered for the story's target failure: the full solution test reaches test execution without OpenTelemetry 1.15.1/NU190x audit warnings failing restore.
- The root-level EventStore submodule is at a safe revision whose OpenTelemetry exporter/API resolution is 1.15.3.
- Story 1.3 now describes semantic-release from `main` instead of tag-triggered release behavior.
- Semantic-release now packs only the five intended Tenants package projects and validates exact package IDs/count before NuGet push.
- CI and release workflows now initialize full DAPR before DAPR-backed Tier 2/Tier 3/performance test jobs.
- Workflow and developer setup guidance now uses root-level-only submodule initialization and avoids nested recursive submodule initialization by default.
- Full local solution tests still require local DAPR CLI/runtime readiness, and the existing performance test currently rejects after the configured tenant configuration limit; these are outside the OpenTelemetry restore gate corrected by this story.

### File List

- `.github/workflows/ci.yml` — UPDATED: root-level submodule checkout and DAPR initialization before DAPR-backed test jobs.
- `.github/workflows/release.yml` — UPDATED: root-level submodule checkout and DAPR initialization before release Tier 1+2 tests.
- `.releaserc.json` — UPDATED: semantic-release prepare command packs exact release packages and validates before publish.
- `scripts/pack-release-packages.py` — NEW: packs only the five publishable Tenants package projects.
- `scripts/validate-nuget-packages.py` — NEW: validates exact package count, IDs, version consistency, readme metadata, and license metadata.
- `README.md` — UPDATED: developer clone guidance initializes root-level submodules only.
- `CONTRIBUTING.md` — UPDATED: submodule setup policy avoids nested recursive initialization by default.
- `docs/quickstart.md` — UPDATED: quickstart clone/setup guidance initializes root-level submodules only.
- `_bmad-output/implementation-artifacts/1-3-ci-cd-pipeline.md` — UPDATED: release and workflow documentation synchronized with semantic-release, DAPR setup, and root-level submodule policy.
- `_bmad-output/implementation-artifacts/post-epic-1-r1a1-foundation-readiness-gates.md` — UPDATED: task tracking, Dev Agent Record, file list, change log, and status.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — UPDATED: carry-forward story status.
- `Hexalith.EventStore` — UPDATED: root-level submodule pointer currently advances from 668030f to f70e78d, retaining OpenTelemetry exporter/API resolution at 1.15.3.

## Change Log

- **2026-05-13**: Completed foundation readiness gates: validated OpenTelemetry restore recovery, synchronized Story 1.3 with semantic-release on `main`, added exact five-package release validation before NuGet push, initialized DAPR in DAPR-backed CI/release jobs, and replaced recursive submodule setup guidance with root-level-only initialization.
