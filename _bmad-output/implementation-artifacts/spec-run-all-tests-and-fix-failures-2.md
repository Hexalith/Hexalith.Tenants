---
title: 'Run all tests and fix failures'
type: 'bugfix'
created: '2026-07-20'
status: 'done'
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
- [x] `scripts/validate-nuget-packages.py` -- add `Microsoft.Extensions.Diagnostics.Abstractions` and `OpenTelemetry` to the `Hexalith.Tenants.Aspire` expected-dependency set, and `Microsoft.Extensions.Diagnostics.Abstractions` to `Hexalith.Tenants.Server`/`Hexalith.Tenants.Testing` -- unblocks the CI gate that has failed for 10+ pushes.
- [x] `tests/Hexalith.Tenants.Contracts.Tests/CiQualityGateScriptTests.cs` -- sync the `ExpectedDependencies` fixture mirror (comment-documented as mirroring the script) with the same 3 additions so its own regression test keeps isolating license/version/dependency behavior correctly.
- [x] `python3 scripts/pack-release-packages.py` + `validate-nuget-packages.py` + `validate-consumer-package-references.py` -- re-run locally to confirm the package-validation lane is green.
- [x] `tests/Hexalith.Tenants.Contracts.Tests`, `tests/Hexalith.Tenants.Client.Tests`, `tests/Hexalith.Tenants.Testing.Tests`, `tests/Hexalith.Tenants.UI.Tests`, `samples/Hexalith.Tenants.Sample.Tests` -- run Tier 1 individually with coverage; fix every failure (untested by CI since 2026-07-19).
- [x] `tests/Hexalith.Tenants.Server.Tests` -- verify full DAPR, run Tier 2 with coverage, and fix failures.
- [x] `tests/Hexalith.Tenants.IntegrationTests` -- run Aspire non-performance and opted-in performance lanes; fix every failure. Initially blocked by an EventStore submodule/package drift (see Spec Change Log); resolved once the user published `Hexalith.EventStore.Server`/`.Aspire` 3.78.0 and bumped the pins in commit `9166f7e`.
- [x] `src/**`, `tests/**`, `samples/**`, `deploy/**`, `docs/**`, `.github/workflows/**`, `*.props`, `*.targets`, `*.json`, `*.yaml` -- change only failure-implicated paths and add focused regression assertions.
- [x] `scripts/validate-coverage.py` -- require greater than 80% scoped lines and 100% branches for configured isolation/auth targets.
- [x] `_bmad-output/implementation-artifacts/spec-run-all-tests-and-fix-failures-2.md` -- record changes, totals, skips, and blockers before review.

**Acceptance Criteria:**
- Given pinned dependencies, when Release builds, then it has zero warnings/errors.
- Given the packed NuGet packages, when package/dependency-boundary/consumer validation runs, then it passes without weakening any expected-dependency check beyond the verified upstream footprint change.
- Given seven owned test projects, when lanes run individually, then all executable tests pass and prerequisite skips are not presented as execution evidence.
- Given Tier 1/2 coverage, when validation runs, then scoped lines exceed 80% and configured isolation/auth targets have 100% branches.
- Given a fix, when focused and full suites rerun, then the failure is gone without weakening tests, contracts, security, or unrelated behavior.

## Spec Change Log

- 2026-07-20: Confirmed the Aspire allowlist drift also applies to `Hexalith.Tenants.Server`/`Hexalith.Tenants.Testing` (both flatten `Hexalith.EventStore.Server`'s new `Microsoft.Extensions.Diagnostics.Abstractions` dependency too) — extended the fix beyond the originally-scoped Aspire-only allowlist entry.
- 2026-07-20: Discovered Tier 3 `IntegrationTests` were blocked by an EventStore submodule/package version drift unrelated to the package-allowlist fix (submodule bumped past a `SubmitCommand` constructor change with no matching published package). Asked the user how to resolve (revert submodule vs. wait for a new package); user chose to keep the current submodule pointer and get a new package published. The user then independently published `Hexalith.EventStore.Server`/`.Aspire` 3.78.0 and committed updated `Hexalith.Builds`/`Hexalith.EventStore`/`Hexalith.Memories` submodule pins directly (commit `9166f7e`, which also picked up this session's in-flight allowlist/spec edits from the working tree). Re-verified against the new pins: the blocker is resolved, all lanes pass.

## Design Notes

The package-validation gate flattens `Hexalith.EventStore.Aspire`'s and `Hexalith.EventStore.Server`'s own transitive `PackageReference`s directly into the consuming Tenants packages' nuspecs (confirmed by extracting all nuspecs involved) — this is pre-existing, accepted behavior already reflected in most of the current allowlist. The drift was 3 new entries that appeared in the pinned `3.77.2` packages' own dependency lists (`Microsoft.Extensions.Diagnostics.Abstractions` in both `Hexalith.EventStore.Aspire` and `.Server`; `OpenTelemetry` in `.Aspire` only). `HexalithEventStoreVersion` (3.77.2, pinned in `references/Hexalith.Builds/Props/Directory.Packages.props`) was not touched by the most recent local commit — the package-validation gate has been broken since at least `fc4c5eb` (2026-07-19), independent of any uncommitted work.

Separately (and unrelated to the package-allowlist fix), Tenants builds two copies of `Hexalith.EventStore.Server` side by side: the domain-service host (`src/Hexalith.Tenants`) references it via NuGet package, while the AppHost unconditionally source-builds the real EventStore host from `references/Hexalith.EventStore` (ProjectReference, no `HexalithEventStoreFromSource` gate). `Hexalith.Tenants.IntegrationTests` links both. The submodule bump in `a2f58d0` had advanced past a `SubmitCommand` constructor change (commit `6945714b`) with no matching published package at the time, so the two copies disagreed at runtime — confirmed via a throwaway diagnostic test (`AppDomain.CurrentDomain.FirstChanceException` capture, not committed) that surfaced the exact `MissingMethodException` the support-safe `GlobalExceptionHandler` otherwise redacts from logs/responses by design. This resolved itself mid-session: the user published `3.78.0` (which includes `6945714b`) and bumped `HexalithEventStoreVersion` plus the `Hexalith.EventStore`/`Hexalith.Builds` submodule pins to match (commit `9166f7e`), closing the drift to 3 commits instead of 33. This is the same class of drift as `[[ci-restore-nu1107-submodule-drift-fix]]` (submodule pointer ahead of its paired published package) — the general lesson holds: advance the package pin to match the submodule, don't paper over it with `UseHexalithProjectReferences`.

## Verification

**Commands:**
- `dotnet restore Hexalith.Tenants.slnx && dotnet build Hexalith.Tenants.slnx --no-restore -c Release -warnaserror` -- zero warnings/errors.
- `rm -rf ./nupkgs && python3 scripts/pack-release-packages.py ./nupkgs 0.0.0-ci-test && python3 scripts/validate-nuget-packages.py ./nupkgs && python3 scripts/validate-consumer-package-references.py ./nupkgs` -- package/dependency-boundary/consumer validation passes.
- `for project in tests/Hexalith.Tenants.Contracts.Tests tests/Hexalith.Tenants.Client.Tests tests/Hexalith.Tenants.Testing.Tests tests/Hexalith.Tenants.UI.Tests samples/Hexalith.Tenants.Sample.Tests tests/Hexalith.Tenants.Server.Tests; do DiffEngine_Disabled=true dotnet test "$project" --no-build -c Release --collect:"XPlat Code Coverage" --results-directory "TestResults/$(basename "$project")"; done` -- Tier 1/2 pass with isolated coverage.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --no-build -c Release --filter "Category!=Performance"` -- non-performance passes live.
- `HEXALITH_EVENTSTORE_RUN_PERFORMANCE_TESTS=1 DiffEngine_Disabled=true dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --no-build -c Release --filter "Category=Performance"` -- performance executes and passes.
- `python3 scripts/validate-coverage.py --coverage-root TestResults --minimum-line-coverage 80 --required-branch-coverage 100 --isolation-auth-target src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs --isolation-auth-target src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsAggregate.cs --isolation-auth-target src/Hexalith.Tenants.Server/Validators/ChangeUserRoleValidator.cs --line-scope src/Hexalith.Tenants.Contracts/ --line-scope src/Hexalith.Tenants.Client/ --line-scope src/Hexalith.Tenants.Server/ --line-scope src/Hexalith.Tenants.Testing/` -- coverage gates pass.

**Results:**
- Release build (against `Hexalith.EventStore`/`.Builds`/`.Memories` pins as of commit `9166f7e`): 0 warnings, 0 errors.
- Package validation (pack + dependency-boundary + consumer-reference): all 5 packages pass after the allowlist fix, re-verified against `Hexalith.EventStore.Server`/`.Aspire` 3.78.0.
- Tier 1: Contracts 112, Client 50, Testing 181, UI 942, Sample 39 -- 1,324 passed, 0 failed, 0 skipped.
- Tier 2 Server: 738 passed, 0 failed, 0 skipped.
- Coverage gates: 94.33% scoped line coverage (1,165/1,235) and 100% configured isolation/auth branch coverage (132/132).
- Tier 3 IntegrationTests: initial run against the pre-3.78.0 pins failed all 68 command-path tests (`MissingMethodException`, documented above); re-run after the user published 3.78.0 and updated the pins: `Category!=Performance` -- 166 passed, 0 failed, 0 skipped. Opt-in `Category=Performance` (500,000-event benchmark): 1 passed, 0 failed, 0 skipped in 16m 3s.
- Total executable evidence (final, post-fix run): 2,229 passed, 0 failed, 0 skipped across seven owned projects (excludes the superseded pre-fix Tier 3 run).
- No remaining blockers.

## Suggested Review Order

- Source-of-truth allowlist: the 3 dependencies verified present in the real, pinned upstream nuspecs (`Microsoft.Extensions.Diagnostics.Abstractions` in Server/Testing/Aspire; `OpenTelemetry` in Aspire only).
  [`validate-nuget-packages.py:55`](../../scripts/validate-nuget-packages.py#L55)

- Same addition applied to the `Hexalith.Tenants.Testing` set (which also flattens `Hexalith.EventStore.Server`'s dependencies).
  [`validate-nuget-packages.py:85`](../../scripts/validate-nuget-packages.py#L85)

- `Hexalith.Tenants.Aspire` gets both new entries — it flattens `Hexalith.EventStore.Aspire`, which pulls in `OpenTelemetry` directly (Server/Testing don't).
  [`validate-nuget-packages.py:114`](../../scripts/validate-nuget-packages.py#L114)

- `OpenTelemetry` (bare package, distinct from `.Extensions.Hosting`/`.Exporter.OpenTelemetryProtocol`) — Aspire-only, confirmed via direct nuspec extraction.
  [`validate-nuget-packages.py:122`](../../scripts/validate-nuget-packages.py#L122)

- Mirrored fixture (test file's own comment documents it as an intentional mirror of the script's allowlist, kept in sync so its license/version regression tests stay isolated from dependency-boundary noise).
  [`CiQualityGateScriptTests.cs:307`](../../tests/Hexalith.Tenants.Contracts.Tests/CiQualityGateScriptTests.cs#L307)
  [`CiQualityGateScriptTests.cs:338`](../../tests/Hexalith.Tenants.Contracts.Tests/CiQualityGateScriptTests.cs#L338)
  [`CiQualityGateScriptTests.cs:368`](../../tests/Hexalith.Tenants.Contracts.Tests/CiQualityGateScriptTests.cs#L368)
  [`CiQualityGateScriptTests.cs:376`](../../tests/Hexalith.Tenants.Contracts.Tests/CiQualityGateScriptTests.cs#L376)
