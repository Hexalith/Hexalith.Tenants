# Story 1.3: CI/CD Pipeline

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer,
I want GitHub Actions workflows for continuous integration and semantic-release publishing,
so that every PR is validated automatically and merges to `main` publish NuGet packages when Conventional Commits require a release.

## Acceptance Criteria

1. **Given** a developer pushes a commit or opens a PR to main **When** the CI workflow (`ci.yml`) triggers **Then** it executes: restore, build (Release configuration), and runs Tier 1+2 tests

2. **Given** the CI workflow runs **When** all tests pass **Then** the workflow reports success and code coverage is collected via coverlet

3. **Given** a developer merges to `main` **When** the release workflow (`release.yml`) triggers semantic-release **Then** semantic-release determines the next version from Conventional Commits, executes the release path, packs all 5 NuGet packages (Contracts, Client, Server, Testing, Aspire), validates the expected package IDs/count (5), and pushes to NuGet.org only after validation succeeds

4. **Given** the release workflow runs **When** the package count does not match the expected 5 **Then** the workflow fails before pushing to NuGet.org

5. **Given** the CI workflow exists **When** a developer inspects the workflow file **Then** it uses pinned action versions (commit SHAs), NuGet cache, concurrency groups with cancel-in-progress, and minimal permissions (`contents: read`)

6. **Given** the release workflow exists **When** a developer inspects the workflow and semantic-release configuration **Then** the workflow runs only from `main`, uses `contents: write` permission for GitHub Release creation, and lets semantic-release create the tag, changelog entry, GitHub Release, and package version from Conventional Commits

7. **Given** the CI workflow runs **When** any test fails **Then** test result artifacts (`.trx` files) are uploaded for debugging

8. **Given** the CI workflow exists **When** a developer inspects the Tier 3 (Aspire) test job **Then** it runs as a separate job with `continue-on-error: true` and `needs: build-and-test`, requiring full DAPR init (not slim) before the tests execute

## Tasks / Subtasks

- [x] Task 0: Verify prerequisites (AC: all)
    - [x] 0.1: Verify EventStore CI/CD reference files exist — confirm `Hexalith.EventStore/.github/workflows/ci.yml` and `release.yml` are present and read their full content
    - [x] 0.2: Verify `.github/workflows/` directory exists or can be created in Tenants
    - [x] 0.3: Verify the solution builds — run `dotnet build Hexalith.Tenants.slnx --configuration Release` and confirm zero errors

- [x] Task 1: Create CI workflow (AC: #1, #2, #5, #7, #8)
    - [x] 1.1: Create `.github/workflows/ci.yml` mirroring EventStore's CI workflow structure
    - [x] 1.2: Configure triggers: push to main, pull_request to main
    - [x] 1.3: Configure concurrency group `ci-${{ github.ref }}` with `cancel-in-progress: true`
    - [x] 1.4: Set permissions to `contents: read`
    - [x] 1.5: Job `build-and-test` on `ubuntu-latest` with `timeout-minutes: 15`
    - [x] 1.6: Steps: checkout (fetch-depth: 0 for semantic-release), setup-dotnet (auto-detects global.json), NuGet cache, restore, build (Release, --no-restore)
    - [x] 1.7: Tier 1 Unit Tests — run each test project individually with `--no-build --configuration Release --logger "trx;LogFileName=test-results.trx" --collect:"XPlat Code Coverage"`: Contracts.Tests, Client.Tests, Testing.Tests, Sample.Tests (from samples/)
    - [x] 1.8: Install DAPR CLI v1.16.0 and run full `dapr init` before Tier 2 tests
    - [x] 1.9: Tier 2 Integration Tests — run Server.Tests with `--no-build --configuration Release --logger "trx;LogFileName=integration-results.trx" --collect:"XPlat Code Coverage"`
    - [x] 1.10: Upload test results artifact on failure
    - [x] 1.11: Job `aspire-tests` — separate job with `needs: build-and-test`, `continue-on-error: true`, `timeout-minutes: 10`. Checkout, setup-dotnet, NuGet cache, full DAPR init (not slim — Aspire topology needs full runtime), run IntegrationTests with TRX logging and coverlet collection, Test Summary step writing status to `$GITHUB_STEP_SUMMARY` (mirror EventStore), upload results on failure

- [x] Task 2: Create Release workflow (AC: #3, #4, #6)
    - [x] 2.1: Create `.github/workflows/release.yml` mirroring EventStore's release workflow structure
    - [x] 2.2: Configure trigger: push to `main` for semantic-release
    - [x] 2.3: Set permissions to `contents: write` (for GitHub Release creation)
    - [x] 2.4: Single job `release` on `ubuntu-latest` with `timeout-minutes: 20`
    - [x] 2.5: Steps: checkout (fetch-depth: 0), setup-dotnet, NuGet cache, restore, build (Release)
    - [x] 2.6: Install DAPR CLI v1.16.0 with full `dapr init` so release Tier 2 tests can execute DAPR-backed server coverage
    - [x] 2.7: Run Tier 1+2 tests (Contracts.Tests, Client.Tests, Testing.Tests, Sample.Tests, Server.Tests) with individual test result loggers and coverlet collection
    - [x] 2.8: Let semantic-release pack the five intended NuGet package projects through `scripts/pack-release-packages.py ./nupkgs ${nextRelease.version}`
    - [x] 2.9: Validate packages before publish — `scripts/validate-nuget-packages.py` checks exactly 5 expected package IDs (Hexalith.Tenants.Contracts, Client, Server, Testing, Aspire), version consistency, readme metadata, and license metadata
    - [x] 2.10: Let semantic-release own tag/version matching through `tagFormat: v${version}`
    - [x] 2.11: Publish to NuGet.org using `NUGET_API_KEY` secret only after package validation succeeds
    - [x] 2.12: Create GitHub Release through `@semantic-release/github` with generated release notes and attached `.nupkg` files

- [x] Task 3: Verification (AC: all)
    - [x] 3.1: Validate YAML syntax of both workflow files (well-formed YAML)
    - [x] 3.2: Verify all action references use pinned commit SHAs (not version tags)
    - [x] 3.3: Verify all Tenants-specific project names/paths are correct (not EventStore names)
    - [x] 3.4: Verify the solution still builds: `dotnet build Hexalith.Tenants.slnx --configuration Release`

## Dev Notes

### Architecture Requirements

- **Mirror EventStore CI/CD workflows exactly** — the reference implementation is at `Hexalith.EventStore/.github/workflows/ci.yml` and `release.yml`. Adapt project names and package IDs from EventStore to Tenants, but keep the same structure, action versions, and patterns.
- **Two workflows**: `ci.yml` (continuous integration) and `release.yml` (semantic-release on `main`). Do NOT combine them.
- **No `docs-validation.yml`** — EventStore has this for discussion template YAML validation. Tenants does not have discussion templates, so skip this workflow entirely.
- **No discussion template validation step in CI** — EventStore's CI includes a Python step to validate `.github/DISCUSSION_TEMPLATE/*.yml`. Tenants does not have these templates, so omit this step. Document the deviation in the Change Log.
- **GitHub Actions submodule checkout** — Tenants uses root-level submodules (`Hexalith.EventStore`, `Hexalith.Commons`). Checkout uses `submodules: true` to initialize root-level submodules only. Do not use recursive nested submodule initialization unless a future story explicitly requires nested submodules.
- **5 NuGet packages**: Hexalith.Tenants.Contracts, Hexalith.Tenants.Client, Hexalith.Tenants.Server, Hexalith.Tenants.Testing, Hexalith.Tenants.Aspire. All other projects have `IsPackable=false`.
- **DAPR init modes explained**: CI uses full `dapr init` for Tier 2 because Server.Tests may exercise DAPR runtime components (actors, state store). CI Tier 3 and nightly performance jobs also initialize DAPR before Aspire tests. Release uses full `dapr init` before Tier 1+2 tests so DAPR-backed server tests are not run against an uninitialized runner.

### Operational Prerequisites

- **`NUGET_API_KEY` GitHub secret** — the release workflow requires a `NUGET_API_KEY` secret configured in the GitHub repository settings (Settings > Secrets and variables > Actions). This is a NuGet.org API key with push permissions for the `Hexalith.Tenants.*` packages. Without this secret, the "Publish to NuGet" step will fail.
- **Tag-from-any-branch risk** — the release workflow triggers on any `v*` tag regardless of which branch it's pushed from. This is consistent with EventStore's pattern. Mitigation: use GitHub branch protection rules and tag protection rules to restrict who can push tags. This is an operational concern, not a workflow code change.

### Test Tier Classification

| Tier                 | Test Projects                                              | DAPR Requirement   | CI Job                             |
| -------------------- | ---------------------------------------------------------- | ------------------ | ---------------------------------- |
| Tier 1 (Unit)        | Contracts.Tests, Client.Tests, Testing.Tests, Sample.Tests | None               | `build-and-test`                   |
| Tier 2 (Integration) | Server.Tests                                               | `dapr init` (full) | `build-and-test`                   |
| Tier 3 (Aspire)      | IntegrationTests                                           | `dapr init` (full) | `aspire-tests` (continue-on-error) |

**Test project paths for Tenants:**

- `tests/Hexalith.Tenants.Contracts.Tests/`
- `tests/Hexalith.Tenants.Client.Tests/`
- `tests/Hexalith.Tenants.Testing.Tests/`
- `samples/Hexalith.Tenants.Sample.Tests/` (physically under `samples/`, not `tests/`)
- `tests/Hexalith.Tenants.Server.Tests/`
- `tests/Hexalith.Tenants.IntegrationTests/`

### CI Workflow Pattern (from EventStore Reference)

**`ci.yml` structure:**

```yaml
name: CI
on:
  push:
    branches: [main]
  pull_request:
    branches: [main]
concurrency:
  group: ci-${{ github.ref }}
  cancel-in-progress: true
permissions:
  contents: read
jobs:
  build-and-test:
    runs-on: ubuntu-latest
    timeout-minutes: 15
    steps:
      - checkout (fetch-depth: 0, submodules: true)
      - setup-dotnet (auto-detects global.json)
      - NuGet cache (key: Directory.Packages.props hash)
      - restore
      - build (Release, --no-restore)
      - Tier 1 unit tests (individual project runs)
      - Install DAPR CLI v1.16.0 + dapr init
      - Tier 2 integration tests (Server.Tests)
      - Upload test results on failure
  aspire-tests:
    needs: build-and-test
    continue-on-error: true
    runs-on: ubuntu-latest
    timeout-minutes: 10
    steps:
      - checkout (fetch-depth: 0, submodules: true)
      - setup-dotnet
      - NuGet cache
      - Install DAPR CLI v1.16.0 + dapr init (full, not slim)
      - Tier 3 Aspire tests (IntegrationTests)
      - Test Summary (always run): write Tier 3 status to $GITHUB_STEP_SUMMARY
      - Upload test results on failure
```

### Release Workflow Pattern (from EventStore Reference)

**`release.yml` structure:**

```yaml
name: Release
on:
  push:
    branches: [main]
permissions:
  contents: write
jobs:
  release:
    runs-on: ubuntu-latest
    timeout-minutes: 20
    steps:
      - checkout (fetch-depth: 0, submodules: true)
      - setup-dotnet
      - NuGet cache
      - restore
      - build (Release, --no-restore)
      - Install DAPR CLI v1.16.0 + dapr init
      - Run Tier 1+2 tests
      - npm ci
      - npx semantic-release
      - semantic-release prepare: build, pack the five expected package projects, validate packages (Python: 5 expected IDs, version consistency, nuspec metadata)
      - semantic-release publish: push to NuGet.org (NUGET_API_KEY secret) and create GitHub Release
```

### Pinned Action Versions (from EventStore Reference)

Use the EXACT same commit SHAs as EventStore:

| Action                      | SHA                                        | Tag    |
| --------------------------- | ------------------------------------------ | ------ |
| actions/checkout            | `34e114876b0b11c390a56381ad16ebd13914f8d5` | v4.3.1 |
| actions/setup-dotnet        | `67a3573c9a986a3f9c594539f4ab511d57bb3ce9` | v4.3.1 |
| actions/cache               | `0057852bfaa89a56745cba8c7296529d2fc39830` | v4.3.0 |
| actions/upload-artifact     | `ea165f8d65b6e75b540449e92b4886f43607fa02` | v4.6.2 |
| softprops/action-gh-release | `a06a81a03ee405af7f2048a818ed3f03bbf83c7b` | v2.5.0 |

### Package Validation Script

The Python validation script must check for exactly these 5 package IDs:

```python
expected_ids = {
    "Hexalith.Tenants.Contracts",
    "Hexalith.Tenants.Client",
    "Hexalith.Tenants.Server",
    "Hexalith.Tenants.Testing",
    "Hexalith.Tenants.Aspire",
}
```

Validation checks (mirror EventStore exactly):

1. Exactly 5 `.nupkg` files (excluding `.snupkg` and `.symbols.`)
2. Package IDs match expected set
3. All packages share the same version
4. Version in filename matches nuspec version
5. Each package has `<readme>` metadata and the referenced file exists in the package
6. Each package has `<license>` or `<licenseFile>` metadata

### Library & Framework Requirements

No new NuGet packages needed for this story. All dependencies are build-time tools already available:

- `semantic-release` — version calculation from Conventional Commits on merge to `main`
- `coverlet.collector 6.0.4` — code coverage collection (already in test project dependencies)
- GitHub Actions runners provide Python 3.x for validation scripts

### File Structure Requirements

**New files to create:**

```
.github/
└── workflows/
    ├── ci.yml       # NEW — CI workflow (build + Tier 1+2 + optional Tier 3)
    └── release.yml  # NEW — Release workflow (test + pack + validate + NuGet push)
```

**Files NOT to create or modify:**

- Do NOT create `docs-validation.yml` (EventStore-specific, Tenants has no discussion templates)
- Do NOT modify any `.csproj` files (dependencies already correct from Story 1.1)
- Do NOT create issue/discussion templates (those are optional, not part of CI/CD)
- Do NOT modify `Directory.Build.props` or `Directory.Packages.props`
- Do NOT add extra coverlet package or runsettings configuration — `coverlet.collector` remains sufficient as the test dependency. The workflow now enables collection explicitly with `--collect:"XPlat Code Coverage"` on each `dotnet test` command so coverage artifacts are emitted during CI and release validation.

### Testing Requirements

**Verification approach for Story 1.3:**

This story creates GitHub Actions workflow YAML files. They cannot be functionally tested locally — they run on GitHub's infrastructure when triggered.

**Required verifications:**

1. Both YAML files parse as valid YAML (correct indentation, no syntax errors)
2. All action references use pinned commit SHAs matching EventStore's versions
3. All project paths reference `Hexalith.Tenants.*` (not `Hexalith.EventStore.*`)
4. Package validation script references 5 Tenants package IDs
5. `dotnet build Hexalith.Tenants.slnx --configuration Release` still passes (no regressions)

**What NOT to test in this story:**

- Do NOT attempt to run the GitHub Actions workflows locally (use `act` or similar)
- Do NOT push to trigger the workflows — that happens when the story is committed
- Do NOT create unit tests for workflow files

### Previous Story Intelligence (from Stories 1.1 and 1.2)

**Key learnings from Story 1.1:**

- Root-level EventStore and Commons submodules are required — CI checkout must include `submodules: true`, not recursive nested submodule initialization.
- .NET SDK version is pinned in `global.json` — `setup-dotnet` auto-detects it
- `dotnet build --configuration Release` produces zero errors and warnings with `TreatWarningsAsErrors`
- 6 test projects discovered by `dotnet test`, zero failures
- `Directory.Packages.props` hash is the correct NuGet cache key (all package versions centralized there)

**Key learnings from Story 1.2:**

- DAPR components are in `src/Hexalith.Tenants.AppHost/DaprComponents/` — DAPR CLI is needed for Server.Tests (Tier 2)
- ServiceDefaults and Aspire extensions compile without issues
- No new packages added — all from Story 1.1's `Directory.Packages.props`

**Patterns established that CI must enforce:**

- Release build (`--configuration Release`) must pass with zero errors
- All 6 test projects must be discoverable and pass
- semantic-release versioning from Conventional Commits (requires `fetch-depth: 0` for full history)
- 5 NuGet packages: Contracts, Client, Server, Testing, Aspire

### Git Intelligence

**Recent commits (5):**

```
19feed1 Merge pull request #2 from Hexalith/add-bmad-planning-artifacts
f2c46ee Add BMAD planning artifacts and sprint status
363167e Merge pull request #1 from Hexalith/add-project-setup
f93ed19 Add project setup with EventStore submodule and tooling config
c04fc8b Initial commit
```

**Observations:**

- No `.github/workflows/` directory exists yet — need to create it
- `.github/` exists with BMAD agents and prompts (unrelated to CI/CD)
- PR-based merge pattern: create branch, open PR, merge to main
- Story 1.1 and 1.2 changes are untracked/uncommitted (stories in `done`/`review` status)

### Critical Implementation Guards

- **DO NOT** use version tags (e.g., `@v4`) for GitHub Actions — always use pinned commit SHAs for supply chain security
- **DO NOT** use `submodules: recursive` in checkout unless nested submodules are explicitly required — `submodules: true` is sufficient for root-level submodules
- **DO NOT** forget `fetch-depth: 0` in checkout — semantic-release needs full git history to calculate versions and changelog context
- **DO NOT** use `dapr init --slim` for Tier 2 tests in CI — Server.Tests may exercise DAPR runtime components (actors, state store) that require full initialization. Because the release workflow now executes the Tier 3 Aspire suite as well, it also uses full `dapr init`.
- **DO NOT** bypass semantic-release in `release.yml` — semantic-release owns version calculation, changelog updates, tag creation, GitHub Release creation, and NuGet publication
- **DO NOT** add NuGet source configuration — default NuGet.org feed is sufficient
- **DO** run test projects individually (not `dotnet test` on the whole solution) — this gives better test reporting and allows tier-based ordering
- **DO** upload test results as artifacts on failure for debugging
- **DO** validate package count before pushing to NuGet.org (fail-safe gate)
- **DO** create GitHub Release with attached `.nupkg` files for traceability

### Project Structure Notes

- Both workflow files go in `.github/workflows/` which needs to be created
- No changes to any existing project files
- No changes to solution file
- The `.github/` directory already exists with BMAD content — add `workflows/` subdirectory

### References

- [Source: Hexalith.EventStore/.github/workflows/ci.yml] — Complete CI workflow reference with pinned actions, test tiers, DAPR CLI install, NuGet cache
- [Source: Hexalith.EventStore/.github/workflows/release.yml] — Complete release workflow reference with package validation, version matching, NuGet push, GitHub Release
- [Source: _bmad-output/planning-artifacts/architecture.md#CI/CD] — CI/CD architectural decision (GitHub Actions, Tier 1+2 tests, pack, validate, push)
- [Source: _bmad-output/planning-artifacts/architecture.md#Complete Project Directory Structure] — Workflow file locations (`.github/workflows/ci.yml`, `.github/workflows/release.yml`)
- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.3] — Original acceptance criteria and story definition
- [Source: _bmad-output/implementation-artifacts/1-1-solution-structure-and-build-configuration.md] — Previous story learnings: submodule verification, SDK pinning, semantic-release, package versions
- [Source: _bmad-output/implementation-artifacts/1-2-dapr-component-configuration-and-servicedefaults.md] — Previous story learnings: DAPR component location, ServiceDefaults compilation
- [Source: Hexalith.EventStore/CLAUDE.md#CI/CD] — EventStore's CI/CD documentation summary

## Senior Developer Review (AI)

### Reviewer

Jerome — 2026-03-14

### Findings

- **HIGH**: `ci.yml` did not explicitly enable coverlet collection, so AC #2 was not verifiable from the workflow alone.
- **HIGH**: `aspire-tests` did not emit a `.trx` file, so AC #7 was only partially implemented for Tier 3 failures.
- **HIGH**: `release.yml` did not execute `tests/Hexalith.Tenants.IntegrationTests/`, so AC #3's full-suite requirement was not met.
- **MEDIUM**: Story metadata needed to reflect the review fixes and the updated workflow behavior.

### Resolution

- Added explicit `--collect:"XPlat Code Coverage"` flags to CI and release test commands.
- Added TRX logging to the Tier 3 Aspire job and artifact upload for the generated `.trx` plus coverage output.
- Expanded `release.yml` to run the Tier 3 integration test project and switched release DAPR initialization to full `dapr init`.
- Updated story tasks, completion notes, change log, and file list to reflect the applied fixes.

## Change Log

- **2026-03-08**: Created CI and Release GitHub Actions workflows mirroring EventStore's structure. Adapted all project names/paths from EventStore to Tenants. Initially added recursive submodule checkout for EventStore project references. Omitted discussion template YAML validation step from CI (EventStore-specific, Tenants has no discussion templates). Omitted `docs-validation.yml` workflow (EventStore-specific).
- **2026-03-14**: Applied code review fixes. CI now enables explicit coverlet collection, Tier 3 Aspire tests emit `.trx` results for artifact upload, and the release workflow runs the full test suite including `tests/Hexalith.Tenants.IntegrationTests/`.
- **2026-05-13**: Synchronized story language with semantic-release on `main`, root-level-only submodule checkout, full DAPR initialization before DAPR-backed tests, and pre-publish validation for the five expected NuGet package IDs.

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6

### Debug Log References

- No issues encountered during implementation
- 2026-03-14 review follow-up: fixed CI coverage collection, Tier 3 test result logging, and release full-suite execution gaps.

### Completion Notes List

- Created `.github/workflows/ci.yml` — CI workflow with build-and-test job (Tier 1+2) and aspire-tests job (Tier 3, continue-on-error)
- Created `.github/workflows/release.yml` — Release workflow with full test suite, NuGet pack, Python package validation (5 expected IDs), version-tag matching, NuGet.org publish, and GitHub Release creation
- All action references use pinned commit SHAs matching EventStore exactly
- Checkout now uses `submodules: true` for root-level EventStore and Commons submodules without nested recursive initialization.
- Sample.Tests correctly referenced from `samples/` path (not `tests/`)
- Explicitly enabled coverlet collection in CI and release test commands
- Tier 3 Aspire tests now emit `.trx` results and upload them as artifacts on failure
- DAPR init modes: full in CI and full in Release so the release workflow can execute the full test suite, including Tier 3 Aspire tests
- Verified: YAML syntax valid, no EventStore references in workflow files, solution builds with 0 warnings/0 errors
- No unit tests created (per Dev Notes: workflow YAML files cannot be functionally tested locally)

### File List

- `.github/workflows/ci.yml` — NEW: CI workflow (build + Tier 1+2 tests + optional Tier 3 Aspire tests)
- `.github/workflows/release.yml` — NEW: Release workflow (test + pack + validate + NuGet push + GitHub Release)
- `_bmad-output/implementation-artifacts/1-3-ci-cd-pipeline.md` — UPDATED: story review notes and metadata synced with the fixed workflow behavior
