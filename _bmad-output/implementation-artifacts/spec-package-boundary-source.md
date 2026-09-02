---
title: 'Derive Package Boundaries from Restore Evidence'
type: 'refactor'
created: '2026-09-02'
status: 'in-review'
baseline_revision: 'bbb0b11ad98d6b5462f05c2e303220b870a73711'
baseline_commit: 'bbb0b11ad98d6b5462f05c2e303220b870a73711'
review_loop_iteration: 0
followup_review_recommended: true
context: ['{project-root}/AGENTS.md']
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** The NuGet package validator and its C# fixture duplicate a large hand-maintained dependency allowlist. Because the tests synthesize packages from that mirror, legitimate restore drift and accidental boundary widening can both escape meaningful regression coverage.

**Approach:** Make the release inventory plus each declared project's NuGet restore assets the governed source for expected dependency groups. Exercise the validator through independent positive and negative package/evidence fixtures so package contents are never generated from the validator's expectations.

## Boundaries & Constraints

**Always:** Keep `tools/release-packages.json` authoritative for publishable package IDs and project paths; derive each expected dependency set from package-mode `obj/project.assets.json` by combining direct `projectFileDependencyGroups` IDs with `centralTransitiveDependencyGroups` keys across target frameworks; fail closed on missing, malformed, ambiguous, or inconsistent manifest/restore evidence; preserve exact dependency equality and the independent forbidden host/sample/test policy; keep the existing CI invocation valid after its Restore and Release Build steps; use only standard-library Python and isolated test directories.

**Never:** Edit the deferred-work ledger; hard-code or persist a generated dependency allowlist; derive expected dependencies from the `.nupkg` being validated; let tests generate package dependencies from the same map/evidence the validator consumes; weaken package inventory, license, readme, shared-version, symbols-package, mismatch, or forbidden-dependency checks; modify root-declared submodules or dependency pins.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Current release output | Manifest-declared packages, matching package-mode restore assets, plus symbols packages | Validator accepts the five release packages and ignores symbols packages | No error expected |
| Dependency drift | A package omits an evidence-backed dependency or adds one absent from restore evidence | Validator rejects the package and reports sorted missing/unexpected IDs | Exit 1 with the package-specific boundary mismatch |
| Invalid evidence | Manifest/project/assets file is missing, malformed, duplicated, outside its root, or structurally unusable | Validator stops before claiming package validity | Exit 1 with a concise evidence/manifest diagnostic |
| Forbidden dependency | Restore evidence and package both include a host, sample, test, AppHost, or ServiceDefaults dependency | Independent policy still rejects the package | Exit 1 naming the forbidden IDs |

</intent-contract>

## Code Map

- `scripts/validate-nuget-packages.py:14-136` -- remove both hard-coded release IDs and `EXPECTED_DEPENDENCIES`; load the release manifest and per-project `obj/project.assets.json` instead.
- `scripts/validate-nuget-packages.py:206-270` -- thread evidence-derived sets through the exact boundary comparison, inventory check, and success report; keep forbidden-dependency enforcement independent.
- `scripts/pack-release-packages.py:14-64` -- read-only reuse reference for manifest location, root confinement, duplicate rejection, and project validation semantics.
- `tools/release-packages.json:1-24` -- read-only authoritative package ID-to-project map; each project path anchors its restore asset file.
- `tests/Hexalith.Tenants.Contracts.Tests/CiQualityGateScriptTests.cs:228-384` -- replace the mirrored dependency dictionary with focused CLI cases backed by isolated manifest/assets fixtures and independently authored `.nupkg` dependencies.
- `tests/Hexalith.Tenants.Contracts.Tests/CiQualityGateScriptTests.cs:419-451` -- extend fixture helpers to accept explicit dependency IDs and write minimal valid NuGet restore evidence without coupling package generation to it.
- `.github/workflows/ci.yml:11-32` and `references/Hexalith.Builds/.github/workflows/domain-ci.yml:337-353` -- read-only execution evidence: CI restores and builds before invoking the unchanged validator command.
- `src/Hexalith.Tenants.{Contracts,Client,Server,Testing,Aspire}/obj/project.assets.json` -- generated, untracked evidence only; fresh package-mode restore confirms packed dependencies equal direct plus centrally promoted transitive groups.

## Tasks & Acceptance

**Execution:**
- [x] `scripts/validate-nuget-packages.py` -- load and validate the release manifest, derive dependency boundaries from each declared project's restore assets, and use those boundaries for inventory, comparison, and reporting while retaining the forbidden policy.
- [x] `tests/Hexalith.Tenants.Contracts.Tests/CiQualityGateScriptTests.cs` -- remove the allowlist mirror; add isolated manifest/assets helpers plus independent passing, unexpected-dependency, and invalid-evidence CLI regressions without generating package dependencies from expected evidence.
- [x] `_bmad-output/implementation-artifacts/spec-package-boundary-source.md` -- record implementation, verification evidence, and review changes without touching the orchestrator-owned ledger.

**Acceptance Criteria:**
- Given a package-mode restore and packed release inventory in `./nupkgs`, when `python3 scripts/validate-nuget-packages.py ./nupkgs` runs, then it derives all five exact dependency boundaries from restore assets and exits 0 without any dependency allowlist in source or tests.
- Given package metadata with a dependency absent from independent restore evidence, when the CLI runs, then it exits 1 and names that dependency as unexpected.
- Given package metadata missing a dependency present in independent restore evidence, when the CLI runs, then it exits 1 and names that dependency as missing.
- Given absent or malformed restore evidence for a manifest-declared project, when the CLI runs, then it exits 1 before reporting successful validation.
- Given a forbidden dependency that is also present in restore evidence, when the CLI runs, then the independent forbidden-boundary policy still exits 1 and names it.
- Given the Contracts test project, when its focused CI-gate class executes, then all metadata, symbols, positive-boundary, negative-boundary, and evidence-failure tests pass without reading the production package dependency set into package fixtures.

## Spec Change Log

- 2026-09-02: Implemented restore-evidence-derived package boundaries, isolated CLI regression fixtures, and fail-closed manifest/assets validation. No deferred-work or root-submodule files were changed.

## Review Triage Log

### 2026-09-02 — Review pass
- verdicts: 33 findings — high 5, medium 5, low 4, false 19, maybe-false 0
- findings:
  - `[false]` `[reject]` Restore-derived expectations cannot detect correlated source-and-package growth — DW-98 explicitly requests dynamic restore-derived validation to eliminate allowlist drift; a reviewed project/package change is not a package-versus-restore mismatch, while forbidden architecture dependencies remain independently rejected.
  - `[medium]` `[patch]` Restore-evidence identity checks lacked negative coverage — added `PackageValidatorRejectsRestoreEvidenceAttributedToAnotherPackage`, proving valid JSON attributed to another package exits 1.
  - `[high]` `[patch]` The full Contracts suite was red after the literal allowlists were removed — replaced the two stale governance assertions and verified all 132 tests pass.
  - `[false]` `[reject]` Correlated package growth defeats the removed static allowlist — the claimed static-policy interpretation conflicts with DW-98's requested dynamic source; CI now verifies restored dependency truth plus the separate forbidden policy.
  - `[false]` `[reject]` Valid release assets can omit `centralTransitiveDependencyGroups` — every manifest-declared project inherits central transitive pinning and its fresh schema-v4 assets contain that group; the hypothetical dependency-free external project is not a current release input.
  - `[false]` `[reject]` Multi-target restore aliases make the framework-group equality invalid — all five release projects target only `net10.0`, and their three relevant schema-v4 group maps use the same alias.
  - `[false]` `[reject]` Unioning target frameworks permits dependencies in the wrong nuspec group — the release inventory is single-target, so no second framework exists on which the described misplacement can occur.
  - `[low]` `[reject]` A future private dependency could appear in restore evidence but be omitted from packing — none of the five release projects declares `PrivateAssets`; supporting a hypothetical future manifest shape needs new parser branches and tests, so this unlikely case is not worth expanding this change.
  - `[false]` `[reject]` Pack-specific suppression or custom nuspec behavior is unmodeled — no declared release project uses `SuppressDependenciesWhenPacking`, `NuspecFile`, or equivalent custom packing behavior.
  - `[false]` `[reject]` Stale or source-mode assets can be accepted — the production caller restores package mode and builds immediately before packing/validation; its generated package and assets are from the same CI graph.
  - `[false]` `[reject]` Assets with restore errors can be treated as authority — CI's preceding `dotnet restore` is fail-fast, and the inspected release assets contain no restore logs.
  - `[low]` `[patch]` Synthetic assets declared obsolete schema version 3 — updated the fixture to the SDK-emitted schema version 4.
  - `[false]` `[reject]` No automated path exercises real restore and the default manifest — the blocking CI gate itself performs real solution restore, Release build, pack, and default-manifest validation; the same production-shaped chain also passed locally.
  - `[high]` `[patch]` `Release_workflow_packs_validates_and_publishes_only_expected_packages` still parsed `EXPECTED_PACKAGE_IDS` — replaced that obsolete assertion with default-manifest and restore-source governance checks.
  - `[high]` `[patch]` `NuGet_package_validator_enforces_dependency_boundaries` still required package/dependency literals — rewrote it to assert manifest/assets derivation and absence of `EXPECTED_DEPENDENCIES`.
  - `[medium]` `[patch]` The old `EXPECTED_DEPENDENCIES` assertion accidentally matched a local variable — replaced it with an exact uppercase-constant absence assertion.
  - `[high]` `[reject]` The spec's focused verification command omitted the full Contracts suite — review rules reject spec-only fixes; the actual gap was closed by running the full assembly after repairing its two failures, with 132/132 passing.
  - `[low]` `[reject]` Several fail-closed manifest branches lack one test per branch — missing file, malformed JSON, attribution, dependency mismatch, casing, and forbidden-policy surfaces are covered; exhaustive malformed-input expansion adds complexity without a demonstrated production defect.
  - `[false]` `[reject]` One-package unit fixtures miss retained multi-package inventory/version branches — those branches predate this change and the real five-package CI gate exercises the multi-package success surface on every run.
  - `[false]` `[reject]` Missing-readme negative coverage is absent — readme validation is unchanged by this bundle, so this is not a regression caused by the reviewed diff.
  - `[medium]` `[patch]` NuGet package and dependency IDs were compared case-sensitively — normalized manifest lookup, inventory, and dependency equality with `casefold()` while retaining canonical evidence spellings in output; added a passing mixed-case CLI test.
  - `[false]` `[reject]` The governed-boundary reading requires a durable static approval snapshot — DW-98 calls for actual dynamic restore/lock output specifically to remove recurring static-list maintenance; the diff implements that defensible reading at the CI package surface.
  - `[false]` `[reject]` Independent tests must themselves run real restore and pack — the real CI gate is the end-to-end restore/pack test, while isolated tests deliberately author package and evidence inputs independently to discriminate positive and negative comparison paths.
  - `[low]` `[reject]` `PrivateAssets=all` can cause a false boundary mismatch — no current release project uses it, and adding suppression-aware modeling plus new state coverage is disproportionate for a hypothetical future package edit.
  - `[false]` `[reject]` Source-reference restore evidence can become release truth — the only production caller performs package-mode restore and Release build before validation, as required by the intent contract.
  - `[false]` `[reject]` Mutually stale package/assets can pass after a project edit — the fail-fast CI sequence regenerates assets and build outputs before packing, so the cited stale pairing is not reachable there.
  - `[false]` `[reject]` The same dependency can appear in direct and central-transitive groups — NuGet central transitive pinning does not classify an already-direct dependency as centrally transitive, and none of the real assets has such overlap.
  - `[false]` `[reject]` Duplicate original target frameworks are silently collapsed — NuGet generated each inspected `originalTargetFrameworks` list; the hand-corrupted duplicate has no dependency-boundary consequence.
  - `[false]` `[reject]` A custom manifest under a directory named `tools` gets the wrong root — that inference intentionally models the production layout; isolated manifests are supported at their project root and all such tests pass.
  - `[medium]` `[patch]` Restore `projectName` matching was case-sensitive — made restore identity matching follow NuGet's case-insensitive ID semantics and covered mixed-case package validation.
  - `[medium]` `[patch]` Package-output inventory matching was case-sensitive — normalized package IDs and duplicate detection while preserving canonical manifest output.
  - `[false]` `[reject]` Non-object `project.frameworks` values can pass — only the framework keys are consumed to establish group consistency; unused framework payload fields cannot alter the derived boundary.
  - `[high]` `[patch]` Lowercase forbidden host/sample/test IDs could pass — normalized both exact forbidden IDs and forbidden fragments and proved a lowercase AppHost dependency is rejected even when restore evidence contains it.

## Design Notes

NuGet's package-mode assets expose the exact two inputs used here: direct/project dependencies under `projectFileDependencyGroups`, and centrally promoted transitive pins under root-level `centralTransitiveDependencyGroups`. Their union matched every dependency in freshly packed Contracts, Client, Server, Testing, and Aspire nuspecs. The optional manifest path should exist only to make the CLI testable against an isolated root; production continues to default to `tools/release-packages.json`.

The validator loads all declared projects and restore assets before inspecting package output. It rejects duplicate JSON properties and inventory entries, paths escaping the inferred project root, mismatched restore project/package identities, non-`PackageReference` evidence, inconsistent target-framework groups, and unusable dependency entries. Custom manifests placed at an isolated project root keep project paths relative to that directory; a manifest under `tools/` uses its parent project root, matching production layout.

## Verification

**Commands:**
- `python3 -m py_compile scripts/validate-nuget-packages.py` -- expected: no syntax errors.
- `dotnet restore tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -p:UseHexalithProjectReferences=false -m:1 -nr:false --force && dotnet build tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Release --no-restore -p:UseHexalithProjectReferences=false -warnaserror -m:1 -nr:false` -- expected: 0 warnings and errors.
- `tests/Hexalith.Tenants.Contracts.Tests/bin/Release/net10.0/Hexalith.Tenants.Contracts.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.Contracts.Tests.CiQualityGateScriptTests` -- expected: all focused tests pass.
- `dotnet restore Hexalith.Tenants.slnx -p:UseHexalithProjectReferences=false && for project in src/Hexalith.Tenants.Contracts/Hexalith.Tenants.Contracts.csproj src/Hexalith.Tenants.Client/Hexalith.Tenants.Client.csproj src/Hexalith.Tenants.Server/Hexalith.Tenants.Server.csproj src/Hexalith.Tenants.Testing/Hexalith.Tenants.Testing.csproj src/Hexalith.Tenants.Aspire/Hexalith.Tenants.Aspire.csproj; do dotnet build "$project" --no-restore --configuration Release -p:UseHexalithProjectReferences=false -warnaserror -m:1 -nr:false || exit 1; done && package_gate_dir=$(mktemp -d) && python3 scripts/pack-release-packages.py "$package_gate_dir" 0.0.0-ci-test && python3 scripts/validate-nuget-packages.py "$package_gate_dir"` -- expected: all five real packages pass against freshly restored evidence.
- `git diff --check -- scripts/validate-nuget-packages.py tests/Hexalith.Tenants.Contracts.Tests/CiQualityGateScriptTests.cs _bmad-output/implementation-artifacts/spec-package-boundary-source.md` -- expected: no findings.

**Observed 2026-09-02:**
- `python3 -m py_compile scripts/validate-nuget-packages.py` completed with no output or errors.
- The focused restore and Release build completed with 0 warnings and 0 errors.
- The final focused CI-gate class completed 15 tests with 0 errors, failures, skips, or tests not run.
- The full Contracts test assembly completed 132 tests with 0 errors, failures, skips, or tests not run.
- The package-mode solution restore, five Release project builds, fresh five-package pack, and default-manifest validator completed successfully; all five derived dependency sets exactly matched their nuspec metadata.
- `git diff --check` completed with no findings after the review patches.

