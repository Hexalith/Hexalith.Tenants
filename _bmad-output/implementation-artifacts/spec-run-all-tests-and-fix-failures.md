---
title: 'Run all tests and fix failures'
type: 'bugfix'
created: '2026-07-14'
status: 'in-progress'
baseline_commit: '9624741d70abc15be19ff98736678a8da2806a8a'
context:
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The current repository state is unverified across every owned test lane, so unit, DAPR, Aspire, performance, or coverage regressions may be hidden.

**Approach:** Reproduce the CI-shaped Release build and all owned test lanes, fix each root cause narrowly in this repository, and repeat the full verification until green.

## Boundaries & Constraints

**Always:** Run projects individually with `DiffEngine_Disabled=true`; preserve serialized builds; use full DAPR/Docker prerequisites; keep warnings as errors; preserve architecture, contracts, tenant isolation, support safety, and test intent; record commands and pass/fail/skip totals.

**Ask First:** Any `references/` submodule edit, public/product behavior or architecture change, deployment, or release.

**Never:** Use solution-level `dotnet test`; weaken/exclude valid tests; lower gates; add blanket suppressions; bypass prerequisites; edit generated or nested-submodule files; or include unrelated cleanup.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Clean baseline | Release build and all lanes | All pass under CI-equivalent settings | Preserve evidence and continue |
| Repeatable failure | Build/test/gate fails in isolation | Smallest owned fix resolves its cause | Add focused coverage; rerun focused and full lanes |
| Environment blocker | Docker, DAPR, Aspire, or runner blocks execution | Repair prerequisite; tests execute | Use xUnit executable fallback; report irreducible blockers exactly |
| Performance | Scheduled benchmark opted in | Benchmark executes and passes | Use the harness opt-in and preserve its threshold |

</frozen-after-approval>

## Code Map

- `.github/workflows/ci.yml` -- authoritative lane and gate configuration.
- `references/Hexalith.Builds/.github/workflows/domain-ci.yml` -- read-only reusable CI command snapshot.
- `Hexalith.Tenants.slnx` -- restore and Release warning-as-error build boundary.
- `tests/Directory.Build.props` -- shared test and coverage defaults.
- `tests/Hexalith.Tenants.IntegrationTests/` -- DAPR/Aspire and scheduled performance tests.
- `scripts/validate-coverage.py` -- line and isolation/auth branch gate.

## Tasks & Acceptance

**Execution:**
- [ ] `Hexalith.Tenants.slnx` -- restore/build Release with warnings as errors; repair compilation failures first.
- [ ] `tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj`, `tests/Hexalith.Tenants.Client.Tests/Hexalith.Tenants.Client.Tests.csproj`, `tests/Hexalith.Tenants.Testing.Tests/Hexalith.Tenants.Testing.Tests.csproj`, `tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj`, `samples/Hexalith.Tenants.Sample.Tests/Hexalith.Tenants.Sample.Tests.csproj` -- run Tier 1 individually with coverage; fix every failure.
- [ ] `tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj` -- verify full DAPR, run Tier 2 with coverage, and fix failures.
- [ ] `tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj` -- run Aspire non-performance and opted-in performance lanes; treat prerequisite skips as missing evidence.
- [ ] `src/**`, `tests/**`, `samples/**`, `deploy/**`, `docs/**`, `.github/workflows/**`, `*.props`, `*.targets`, `*.json`, `*.yaml` -- change only failure-implicated paths and add focused regression assertions.
- [ ] `scripts/validate-coverage.py` -- require greater than 80% scoped lines and 100% branches for configured isolation/auth targets.
- [ ] `_bmad-output/implementation-artifacts/spec-run-all-tests-and-fix-failures.md` -- record changes, totals, skips, and blockers before review.

**Acceptance Criteria:**
- Given pinned dependencies, when Release builds, then it has zero warnings/errors.
- Given seven owned test projects, when lanes run individually, then all executable tests pass and prerequisite skips are not presented as execution evidence.
- Given Tier 1/2 coverage, when validation runs, then scoped lines exceed 80% and configured isolation/auth targets have 100% branches.
- Given a fix, when focused and full suites rerun, then the failure is gone without weakening tests, contracts, security, or unrelated behavior.

## Spec Change Log

## Design Notes

The performance comment names a Tenants opt-in, but the shared harness gates on `HEXALITH_EVENTSTORE_RUN_PERFORMANCE_TESTS=1`. Use the harness contract; do not change the benchmark unless the mismatch causes a verified failure.

## Verification

**Commands:**
- `dotnet restore Hexalith.Tenants.slnx && dotnet build Hexalith.Tenants.slnx --no-restore -c Release -warnaserror` -- zero warnings/errors.
- `for project in tests/Hexalith.Tenants.Contracts.Tests tests/Hexalith.Tenants.Client.Tests tests/Hexalith.Tenants.Testing.Tests tests/Hexalith.Tenants.UI.Tests samples/Hexalith.Tenants.Sample.Tests tests/Hexalith.Tenants.Server.Tests; do DiffEngine_Disabled=true dotnet test "$project" --no-build -c Release --collect:"XPlat Code Coverage" --results-directory "TestResults/$(basename "$project")"; done` -- Tier 1/2 pass with isolated coverage.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --no-build -c Release --filter "Category!=Performance"` -- non-performance passes live.
- `HEXALITH_EVENTSTORE_RUN_PERFORMANCE_TESTS=1 DiffEngine_Disabled=true dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --no-build -c Release --filter "Category=Performance"` -- performance executes and passes.
- `python3 scripts/validate-coverage.py --coverage-root TestResults --minimum-line-coverage 80 --required-branch-coverage 100 --isolation-auth-target src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs --isolation-auth-target src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsAggregate.cs --isolation-auth-target src/Hexalith.Tenants.Server/Validators/ChangeUserRoleValidator.cs --line-scope src/Hexalith.Tenants.Contracts/ --line-scope src/Hexalith.Tenants.Client/ --line-scope src/Hexalith.Tenants.Server/ --line-scope src/Hexalith.Tenants.Testing/` -- coverage gates pass.
