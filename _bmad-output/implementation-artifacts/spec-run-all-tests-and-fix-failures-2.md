---
title: 'Run all tests and fix failures'
type: 'bugfix'
created: '2026-07-20'
status: 'in-progress'
baseline_commit: 'a2f58d02584236fa9a8f6cbb9dec515b8ea76b8d'
context:
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** `ci / build-and-test` on `main` has failed for 10+ consecutive pushes (back to at least `fc4c5eb`, 2026-07-19) at the "Validate package consumer references" gate, which blocks every later lane (Tier 1/2 tests, DAPR, Aspire, coverage) from ever executing in CI — so the true pass/fail state of the owned test suite is unverified.

**Approach:** Reproduce the CI-shaped Release build and package/test pipeline locally, fix each root cause narrowly, and re-run the full verification chain (build → package validation → Tier 1 → Tier 2 → Tier 3 Aspire → coverage gates) until green.

## Boundaries & Constraints

**Always:** Run projects individually with `DiffEngine_Disabled=true`; preserve serialized builds; use full DAPR/Docker prerequisites; keep warnings as errors; preserve architecture, contracts, tenant isolation, support safety, and test intent; record commands and pass/fail/skip totals.

**Ask First:** Any `references/` submodule edit, public/product behavior or architecture change, deployment, or release.

**Never:** Use solution-level `dotnet test`; weaken/exclude valid tests; lower gates; add blanket suppressions; bypass prerequisites; edit generated or nested-submodule files; or include unrelated cleanup.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Confirmed root cause | `Hexalith.EventStore.Aspire` 3.77.2's own nuspec now declares 2 extra flattened deps (`Microsoft.Extensions.Diagnostics.Abstractions`, `OpenTelemetry`) not yet in Tenants' allowlist | `validate-nuget-packages.py` boundary list matches the verified, legitimate footprint; pack validation passes | If any *other* unexpected dependency appears, treat as a real regression, not an allowlist update |
| Repeatable failure | Build/test/gate fails in isolation | Smallest owned fix resolves its cause | Add focused coverage; rerun focused and full lanes |
| Environment blocker | Docker, DAPR, Aspire, or runner blocks execution | Repair prerequisite; tests execute | Use xUnit executable fallback; report irreducible blockers exactly |
| Performance | Scheduled benchmark opted in | Benchmark executes and passes | Use the harness opt-in and preserve its threshold |

</frozen-after-approval>

## Code Map

- `scripts/validate-nuget-packages.py` -- `EXPECTED_DEPENDENCIES["Hexalith.Tenants.Aspire"]` is the confirmed first fix: add the 2 newly-legitimate flattened deps from the pinned `Hexalith.EventStore.Aspire` 3.77.2 package.
- `.github/workflows/ci.yml` -- authoritative lane and gate configuration (read-only reference).
- `references/Hexalith.Builds/.github/workflows/domain-ci.yml` -- read-only reusable CI command snapshot.
- `Hexalith.Tenants.slnx` -- restore and Release warning-as-error build boundary.
- `tests/Directory.Build.props` -- shared test and coverage defaults.
- `tests/Hexalith.Tenants.IntegrationTests/` -- DAPR/Aspire and scheduled performance tests.
- `scripts/validate-coverage.py` -- line and isolation/auth branch gate.

## Tasks & Acceptance

**Execution:**
- [ ] `scripts/validate-nuget-packages.py` -- add `Microsoft.Extensions.Diagnostics.Abstractions` and `OpenTelemetry` to the `Hexalith.Tenants.Aspire` expected-dependency set -- unblocks the CI gate that has failed for 10+ pushes.
- [ ] `python3 scripts/pack-release-packages.py` + `validate-nuget-packages.py` + `validate-consumer-package-references.py` -- re-run locally to confirm the package-validation lane is green.
- [ ] `tests/Hexalith.Tenants.Contracts.Tests`, `tests/Hexalith.Tenants.Client.Tests`, `tests/Hexalith.Tenants.Testing.Tests`, `tests/Hexalith.Tenants.UI.Tests`, `samples/Hexalith.Tenants.Sample.Tests` -- run Tier 1 individually with coverage; fix every failure (untested by CI since 2026-07-19).
- [ ] `tests/Hexalith.Tenants.Server.Tests` -- verify full DAPR, run Tier 2 with coverage, and fix failures.
- [ ] `tests/Hexalith.Tenants.IntegrationTests` -- run Aspire non-performance and opted-in performance lanes; treat prerequisite skips as missing evidence.
- [ ] `src/**`, `tests/**`, `samples/**`, `deploy/**`, `docs/**`, `.github/workflows/**`, `*.props`, `*.targets`, `*.json`, `*.yaml` -- change only failure-implicated paths and add focused regression assertions.
- [ ] `scripts/validate-coverage.py` -- require greater than 80% scoped lines and 100% branches for configured isolation/auth targets.
- [ ] `_bmad-output/implementation-artifacts/spec-run-all-tests-and-fix-failures-2.md` -- record changes, totals, skips, and blockers before review.

**Acceptance Criteria:**
- Given pinned dependencies, when Release builds, then it has zero warnings/errors.
- Given the packed NuGet packages, when package/dependency-boundary/consumer validation runs, then it passes without weakening any expected-dependency check beyond the verified upstream footprint change.
- Given seven owned test projects, when lanes run individually, then all executable tests pass and prerequisite skips are not presented as execution evidence.
- Given Tier 1/2 coverage, when validation runs, then scoped lines exceed 80% and configured isolation/auth targets have 100% branches.
- Given a fix, when focused and full suites rerun, then the failure is gone without weakening tests, contracts, security, or unrelated behavior.

## Spec Change Log

## Design Notes

The package-validation gate flattens `Hexalith.EventStore.Aspire`'s own transitive `PackageReference`s directly into `Hexalith.Tenants.Aspire`'s nuspec (confirmed by extracting both nuspecs) — this is pre-existing, accepted behavior already reflected in most of the current allowlist. The only drift is the 2 new entries that appeared in `Hexalith.EventStore.Aspire` 3.77.2's own dependency list. `HexalithEventStoreVersion` (3.77.2, pinned in `references/Hexalith.Builds/Props/Directory.Packages.props`) was not touched by the most recent local commit — this has been broken since at least `fc4c5eb` (2026-07-19), independent of any uncommitted work.

## Verification

**Commands:**
- `dotnet restore Hexalith.Tenants.slnx && dotnet build Hexalith.Tenants.slnx --no-restore -c Release -warnaserror` -- zero warnings/errors.
- `rm -rf ./nupkgs && python3 scripts/pack-release-packages.py ./nupkgs 0.0.0-ci-test && python3 scripts/validate-nuget-packages.py ./nupkgs && python3 scripts/validate-consumer-package-references.py ./nupkgs` -- package/dependency-boundary/consumer validation passes.
- `for project in tests/Hexalith.Tenants.Contracts.Tests tests/Hexalith.Tenants.Client.Tests tests/Hexalith.Tenants.Testing.Tests tests/Hexalith.Tenants.UI.Tests samples/Hexalith.Tenants.Sample.Tests tests/Hexalith.Tenants.Server.Tests; do DiffEngine_Disabled=true dotnet test "$project" --no-build -c Release --collect:"XPlat Code Coverage" --results-directory "TestResults/$(basename "$project")"; done` -- Tier 1/2 pass with isolated coverage.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --no-build -c Release --filter "Category!=Performance"` -- non-performance passes live.
- `HEXALITH_EVENTSTORE_RUN_PERFORMANCE_TESTS=1 DiffEngine_Disabled=true dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --no-build -c Release --filter "Category=Performance"` -- performance executes and passes.
- `python3 scripts/validate-coverage.py --coverage-root TestResults --minimum-line-coverage 80 --required-branch-coverage 100 --isolation-auth-target src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs --isolation-auth-target src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsAggregate.cs --isolation-auth-target src/Hexalith.Tenants.Server/Validators/ChangeUserRoleValidator.cs --line-scope src/Hexalith.Tenants.Contracts/ --line-scope src/Hexalith.Tenants.Client/ --line-scope src/Hexalith.Tenants.Server/ --line-scope src/Hexalith.Tenants.Testing/` -- coverage gates pass.
