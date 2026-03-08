# Story 1.3: CI/CD Pipeline

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer,
I want GitHub Actions workflows for continuous integration and release publishing,
so that every PR is validated automatically and tagged releases publish NuGet packages.

## Acceptance Criteria

1. **Given** a developer pushes a commit or opens a PR to main **When** the CI workflow (`ci.yml`) triggers **Then** it executes: restore, build (Release configuration), and runs Tier 1+2 tests

2. **Given** the CI workflow runs **When** all tests pass **Then** the workflow reports success and code coverage is collected via coverlet

3. **Given** a developer pushes a tag matching `v*` (e.g., `v0.1.0`) **When** the release workflow (`release.yml`) triggers **Then** it executes the full test suite, packs all 5 NuGet packages (Contracts, Client, Server, Testing, Aspire), validates the expected package count (5), and pushes to NuGet.org

4. **Given** the release workflow runs **When** the package count does not match the expected 5 **Then** the workflow fails before pushing to NuGet.org

5. **Given** the CI workflow exists **When** a developer inspects the workflow file **Then** it uses pinned action versions (commit SHAs), NuGet cache, concurrency groups with cancel-in-progress, and minimal permissions (`contents: read`)

6. **Given** the release workflow exists **When** a developer inspects the workflow file **Then** it uses `contents: write` permission (for GitHub Release creation), validates package version matches the git tag, and creates a GitHub Release with generated release notes

7. **Given** the CI workflow runs **When** any test fails **Then** test result artifacts (`.trx` files) are uploaded for debugging

8. **Given** the CI workflow exists **When** a developer inspects the Tier 3 (Aspire) test job **Then** it runs as a separate job with `continue-on-error: true` and `needs: build-and-test`, requiring full DAPR init (not slim)

## Tasks / Subtasks

- [ ] Task 0: Verify prerequisites (AC: all)
  - [ ] 0.1: Verify EventStore CI/CD reference files exist — confirm `Hexalith.EventStore/.github/workflows/ci.yml` and `release.yml` are present and read their full content
  - [ ] 0.2: Verify `.github/workflows/` directory exists or can be created in Hexalith.Tenants
  - [ ] 0.3: Verify the solution builds — run `dotnet build Hexalith.Tenants.slnx --configuration Release` and confirm zero errors

- [ ] Task 1: Create CI workflow (AC: #1, #2, #5, #7, #8)
  - [ ] 1.1: Create `.github/workflows/ci.yml` mirroring EventStore's CI workflow structure
  - [ ] 1.2: Configure triggers: push to main, pull_request to main
  - [ ] 1.3: Configure concurrency group `ci-${{ github.ref }}` with `cancel-in-progress: true`
  - [ ] 1.4: Set permissions to `contents: read`
  - [ ] 1.5: Job `build-and-test` on `ubuntu-latest` with `timeout-minutes: 15`
  - [ ] 1.6: Steps: checkout (fetch-depth: 0 for MinVer), setup-dotnet (auto-detects global.json), NuGet cache, restore, build (Release, --no-restore)
  - [ ] 1.7: Tier 1 Unit Tests — run each test project individually with `--no-build --configuration Release --logger "trx;LogFileName=test-results.trx"`: Contracts.Tests, Client.Tests, Testing.Tests, Sample.Tests (from samples/)
  - [ ] 1.8: Install DAPR CLI v1.16.0 and `dapr init`
  - [ ] 1.9: Tier 2 Integration Tests — run Server.Tests with `--no-build --configuration Release --logger "trx;LogFileName=integration-results.trx"`
  - [ ] 1.10: Upload test results artifact on failure
  - [ ] 1.11: Job `aspire-tests` — separate job with `needs: build-and-test`, `continue-on-error: true`, `timeout-minutes: 10`. Checkout, setup-dotnet, NuGet cache, full DAPR init (not slim — Aspire topology needs full runtime), run IntegrationTests, Test Summary step writing status to `$GITHUB_STEP_SUMMARY` (mirror EventStore), upload results on failure

- [ ] Task 2: Create Release workflow (AC: #3, #4, #6)
  - [ ] 2.1: Create `.github/workflows/release.yml` mirroring EventStore's release workflow structure
  - [ ] 2.2: Configure trigger: push tags `v*`
  - [ ] 2.3: Set permissions to `contents: write` (for GitHub Release creation)
  - [ ] 2.4: Single job `release` on `ubuntu-latest` with `timeout-minutes: 20`
  - [ ] 2.5: Steps: checkout (fetch-depth: 0), setup-dotnet, NuGet cache, restore, build (Release)
  - [ ] 2.6: Install DAPR CLI v1.16.0 with `dapr init --slim`
  - [ ] 2.7: Run all Tier 1+2 tests (Contracts.Tests, Client.Tests, Testing.Tests, Sample.Tests, Server.Tests) with individual test result loggers
  - [ ] 2.8: Pack NuGet: `dotnet pack --no-build --configuration Release --output ./nupkgs`
  - [ ] 2.9: Validate packages — Python script checking 5 expected package IDs (Hexalith.Tenants.Contracts, Client, Server, Testing, Aspire), version consistency, nuspec metadata (readme, license)
  - [ ] 2.10: Validate version matches tag — extract tag version, compare to package version
  - [ ] 2.11: Publish to NuGet.org using `NUGET_API_KEY` secret
  - [ ] 2.12: Create GitHub Release with `softprops/action-gh-release`, attach `.nupkg` files, generate release notes

- [ ] Task 3: Verification (AC: all)
  - [ ] 3.1: Validate YAML syntax of both workflow files (well-formed YAML)
  - [ ] 3.2: Verify all action references use pinned commit SHAs (not version tags)
  - [ ] 3.3: Verify all Tenants-specific project names/paths are correct (not EventStore names)
  - [ ] 3.4: Verify the solution still builds: `dotnet build Hexalith.Tenants.slnx --configuration Release`

## Dev Notes

### Architecture Requirements

- **Mirror EventStore CI/CD workflows exactly** — the reference implementation is at `Hexalith.EventStore/.github/workflows/ci.yml` and `release.yml`. Adapt project names and package IDs from EventStore to Tenants, but keep the same structure, action versions, and patterns.
- **Two workflows**: `ci.yml` (continuous integration) and `release.yml` (tag-triggered release). Do NOT combine them.
- **No `docs-validation.yml`** — EventStore has this for discussion template YAML validation. Tenants does not have discussion templates, so skip this workflow entirely.
- **No discussion template validation step in CI** — EventStore's CI includes a Python step to validate `.github/DISCUSSION_TEMPLATE/*.yml`. Tenants does not have these templates, so omit this step. Document the deviation in the Change Log.
- **GitHub Actions submodule checkout** — Tenants uses a git submodule (`Hexalith.EventStore`). The checkout step does NOT need `submodules: recursive` because the CI build uses NuGet package references in production, not submodule project references. However, since the current Story 1.1 scaffolding uses `ProjectReference` through the submodule, **the checkout MUST include `submodules: recursive`** until the project transitions to NuGet package references.
- **5 NuGet packages**: Hexalith.Tenants.Contracts, Hexalith.Tenants.Client, Hexalith.Tenants.Server, Hexalith.Tenants.Testing, Hexalith.Tenants.Aspire. All other projects have `IsPackable=false`.
- **DAPR init modes explained**: CI uses `dapr init` (full) for Tier 2 because Server.Tests may exercise DAPR runtime components (actors, state store). Release uses `dapr init --slim` because it only needs the DAPR SDK to compile, not actual runtime — the slim init provides enough for the build and Tier 1+2 tests to pass. This matches EventStore's pattern exactly.

### Operational Prerequisites

- **`NUGET_API_KEY` GitHub secret** — the release workflow requires a `NUGET_API_KEY` secret configured in the GitHub repository settings (Settings > Secrets and variables > Actions). This is a NuGet.org API key with push permissions for the `Hexalith.Tenants.*` packages. Without this secret, the "Publish to NuGet" step will fail.
- **Tag-from-any-branch risk** — the release workflow triggers on any `v*` tag regardless of which branch it's pushed from. This is consistent with EventStore's pattern. Mitigation: use GitHub branch protection rules and tag protection rules to restrict who can push tags. This is an operational concern, not a workflow code change.

### Test Tier Classification

| Tier | Test Projects | DAPR Requirement | CI Job |
|------|--------------|------------------|--------|
| Tier 1 (Unit) | Contracts.Tests, Client.Tests, Testing.Tests, Sample.Tests | None | `build-and-test` |
| Tier 2 (Integration) | Server.Tests | `dapr init` (full) | `build-and-test` |
| Tier 3 (Aspire) | IntegrationTests | `dapr init` (full) | `aspire-tests` (continue-on-error) |

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
      - checkout (fetch-depth: 0, submodules: recursive)
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
      - checkout (fetch-depth: 0, submodules: recursive)
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
    tags: ['v*']
permissions:
  contents: write
jobs:
  release:
    runs-on: ubuntu-latest
    timeout-minutes: 20
    steps:
      - checkout (fetch-depth: 0, submodules: recursive)
      - setup-dotnet
      - NuGet cache
      - restore
      - build (Release, --no-restore)
      - Install DAPR CLI v1.16.0 + dapr init --slim
      - Run all Tier 1+2 tests
      - dotnet pack (--no-build, Release, --output ./nupkgs)
      - Validate packages (Python: 5 expected IDs, version match, nuspec metadata)
      - Validate version matches tag
      - Push to NuGet.org (NUGET_API_KEY secret)
      - Create GitHub Release (softprops/action-gh-release)
```

### Pinned Action Versions (from EventStore Reference)

Use the EXACT same commit SHAs as EventStore:
| Action | SHA | Tag |
|--------|-----|-----|
| actions/checkout | `34e114876b0b11c390a56381ad16ebd13914f8d5` | v4.3.1 |
| actions/setup-dotnet | `67a3573c9a986a3f9c594539f4ab511d57bb3ce9` | v4.3.1 |
| actions/cache | `0057852bfaa89a56745cba8c7296529d2fc39830` | v4.3.0 |
| actions/upload-artifact | `ea165f8d65b6e75b540449e92b4886f43607fa02` | v4.6.2 |
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
- `MinVer 7.0.0` — version calculation from git tags (already in Directory.Packages.props)
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
- Do NOT add coverlet configuration — coverlet.collector works via its NuGet package integration, no additional config needed. Code coverage is collected automatically when `dotnet test` runs because coverlet.collector is referenced as a NuGet package in each test project. No extra `--collect "XPlat Code Coverage"` flags are needed in the workflow YAML.

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
- EventStore submodule requires `git submodule update --init --recursive` — CI checkout must include `submodules: recursive`
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
- MinVer versioning from git tags (requires `fetch-depth: 0` for full history)
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
- **DO NOT** forget `submodules: recursive` in checkout — the solution currently depends on the EventStore submodule via ProjectReference
- **DO NOT** forget `fetch-depth: 0` in checkout — MinVer needs full git history to calculate versions
- **DO NOT** use `dapr init --slim` for Tier 2 tests in CI — Server.Tests may exercise DAPR runtime components (actors, state store) that require full initialization. In the release workflow, use `--slim` because release only needs the DAPR SDK for compilation, not actual runtime execution (matching EventStore's pattern exactly)
- **DO NOT** include a concurrency block in `release.yml` — each release must complete independently
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
- [Source: _bmad-output/implementation-artifacts/1-1-solution-structure-and-build-configuration.md] — Previous story learnings: submodule verification, SDK pinning, MinVer, package versions
- [Source: _bmad-output/implementation-artifacts/1-2-dapr-component-configuration-and-servicedefaults.md] — Previous story learnings: DAPR component location, ServiceDefaults compilation
- [Source: Hexalith.EventStore/CLAUDE.md#CI/CD] — EventStore's CI/CD documentation summary

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

### File List
