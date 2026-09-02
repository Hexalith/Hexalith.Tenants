---
title: 'Derive Package Boundaries from Restore Evidence'
type: 'refactor'
created: '2026-09-02'
status: 'done'
baseline_revision: 'bbb0b11ad98d6b5462f05c2e303220b870a73711'
baseline_commit: 'bbb0b11ad98d6b5462f05c2e303220b870a73711'
review_loop_iteration: 0
followup_review_recommended: false
context: ['{project-root}/AGENTS.md']
warnings: []
deferred:
  - summary: >-
      `scripts/publish-partial-release.sh` reads `HEXALITH_RELEASE_PACKAGE_MANIFEST` into a
      `manifest` variable that no command ever consumes, so an operator override is silently ignored.
    evidence: |-
      `scripts/publish-partial-release.sh:6` assigns `manifest="${HEXALITH_RELEASE_PACKAGE_MANIFEST:-tools/release-packages.json}"`
      and no later line references `$manifest`; `grep -n manifest scripts/publish-partial-release.sh` returns line 6 only.
      The variable was already dead at `bbb0b11a` (the file is untouched by this change), and
      `scripts/pack-release-packages.py` accepts no manifest argument either, so threading the new
      `--manifest` flag alone would not make the override effective. Pre-existing, not caused by this story.
    location: >-
      scripts/publish-partial-release.sh:6
    severity: low
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

### 2026-09-02 — Review pass

- verdicts: 49 findings — high 0, medium 5, low 36, false 8, maybe-false 0
- findings:
  - `[medium]` `[reject]` DW-118 closed although restore-derived expectations still let correlated source+package growth pass — real (grouped with the verification-gap entry below), but out of scope: the intent's Always clause mandates deriving each expected set from `obj/project.assets.json` and its Never clause forbids hard-coding or persisting a generated allowlist, so the only proposed fix contradicts the contract. Ledger status itself is orchestrator-owned.
  - `[low]` `[reject]` The comment explaining why `Microsoft.Extensions.Http.Resilience` is upstream-owned was deleted with the allowlist — real knowledge loss, but the fix is new prose rather than a direct correction, and the derived set now shows the dependency's provenance implicitly.
  - `[low]` `[reject]` Manifest parsing is now duplicated between `validate-nuget-packages.py:205-265` and `pack-release-packages.py:17-70`, with divergent duplicate-detection casing — verified real, but the fix is extracting a shared module (new surface, not a direct correction), and both copies read the same production manifest.
  - `[low]` `[reject]` `centralTransitiveDependencyGroups` is hard-required at `validate-nuget-packages.py:97-101` — verified: all five release projects' schema-v4 assets contain it (`CentralPackageTransitivePinningEnabled` at `Directory.Packages.props:8`). Absent-section handling would also need relaxing the group-consistency check at `:131`; that is added branches for a state no release project reaches, and the Always clause explicitly asks for fail-closed evidence handling.
  - `[low]` `[reject]` A `PrivateAssets="all"` reference would produce a false `Missing:` failure — real semantic gap, but no manifest-declared release project uses `PrivateAssets` (only `src/Hexalith.Tenants.Api`, which is not published), and the fix needs suppression-aware parsing plus new coverage.
  - `[false]` `[reject]` The assets path `<project>/obj/project.assets.json` is hard-coded and breaks under a relocated intermediate path — `grep -rn 'BaseIntermediateOutputPath|MSBuildProjectExtensionsPath|UseArtifactsOutput'` over `Directory.Build.props`, `Directory.Build.targets`, `global.json`, `nuget.config`, and the five `src/*.csproj` returns nothing, so no reachable state relocates it.
  - `[low]` `[reject]` Package-mode and source-mode restore evidence are indistinguishable (`projectStyle` is `PackageReference` in both) — `Directory.Build.props:56-59` defaults `UseHexalithProjectReferences` to `false`, so both production callers restore package mode, and the deleted literal allowlist was equally blind to a source-mode pack. Detecting it needs a new heuristic.
  - `[low]` `[defer]` The new `--manifest` flag is not threaded from `HEXALITH_RELEASE_PACKAGE_MANIFEST`, and the default manifest path has no unit test — the unused variable is pre-existing at `bbb0b11a` and recorded under `deferred`; the default path is exercised by the blocking CI gate on every run.
  - `[low]` `[reject]` The `tools`-basename project-root inference at `validate-nuget-packages.py:211-217` is implicit and load-bearing — real, but the fix is a new `--project-root` CLI argument, i.e. added public surface.
  - `[low]` `[reject]` Eleven new fail-closed branches (duplicate manifest id/project, root escape, non-`.csproj`, duplicate JSON property, non-`PackageReference` style, inconsistent framework groups, ambiguous casing, undeclared package id, duplicate package output) have no test — each is a single `raise` on a manifest the repository itself owns; the covered surfaces already include missing, malformed, attributed-elsewhere, mismatched, and forbidden evidence.
  - `[low]` `[reject]` Positive coverage regressed from a five-package fixture to single-package fixtures — the multi-package inventory path runs in the real CI gate on every push (verified: `Validated 5 NuGet packages`), and the shared-version branch had no unit coverage before this change either. A multi-package fixture needs a new helper signature.
  - `[low]` `[reject]` Test naming switched to PascalCase for only the new methods, leaving two conventions in one file — cosmetic; renaming the untouched coverage-gate tests is churn beyond this change.
  - `[low]` `[patch]` `PackageGovernanceTests` pinned private Python identifiers (`load_dependency_boundaries`, `load_restore_dependencies`) and the exact text of the `DEFAULT_MANIFEST` assignment, so renaming a helper or reformatting one line fails governance with no behavior change — replaced with `DEFAULT_MANIFEST` plus `"release-packages.json"` token assertions and dropped the two private-helper greps.
  - `[low]` `[reject]` `PackageGovernanceTests.cs` is changed by the diff but absent from the spec's Code Map, Tasks, and `git diff --check` command — the fix edits this build's spec, which the review rules reject.
  - `[false]` `[reject]` The spec claims no deferred-work file was changed while the diff modifies `deferred-work.md` — `git show --stat 9e7ac900` lists exactly four files and none is the ledger; the ledger edit is an uncommitted orchestrator sweep write (`resolution: resolved by sweep bundle dw-package-boundary-source`) made outside this build.
  - `[low]` `[reject]` DW-118 carries both `status: open` (inside `legacy-detail`) and `status: done 2026-09-02` — orchestrator-owned ledger formatting; the intent's Never clause forbids editing it.
  - `[low]` `[reject]` The three ledger resolutions share one `resolution-undo` token and cite no commit — orchestrator-owned ledger bookkeeping, excluded by the intent's Never clause.
  - `[low]` `[reject]` Spec frontmatter is stale (`review_loop_iteration`, `followup_review_recommended`, duplicated baseline keys, unresolved `{project-root}` placeholder) — the fix edits this build's spec.
  - `[low]` `[reject]` `deferred: []` although the triage log rejected real out-of-scope risks — the fix edits this build's spec; this pass records the one pre-existing item that qualifies.
  - `[low]` `[reject]` Code Map cites pre-change line ranges — the fix edits this build's spec.
  - `[low]` `[reject]` Acceptance criteria never absorbed the review-pass patches (casing, attribution, forbidden normalization) — the fix edits this build's spec.
  - `[low]` `[reject]` `references/Hexalith.Commons/scripts/validate-nuget-packages.py` still uses `EXPECTED_PACKAGE_IDS`, so sibling repos diverge — the intent's Never clause forbids modifying root-declared submodules.
  - `[false]` `[reject]` The success report prints derived expectations rather than observed package contents — `validate_dependency_boundaries` has already proven the two sets equal (case-insensitively) for every package before the report runs, so the printed set is the observed set in canonical spelling.
  - `[false]` `[reject]` `sorted(dependency_boundaries.values())` can compare `frozenset` second elements non-deterministically — the dict is keyed by casefolded package id, so two entries can never share an identical canonical `package_id`; the tuple comparison always decides on the first element.
  - `[low]` `[reject]` `actual_by_normalized_id` silently collapses a package that spells one dependency id two ways, while restore evidence raises on the same ambiguity — real asymmetry, but a single-TFM nuspec emits one dependency group and NuGet treats the spellings as one package, so the harm is negligible and the fix adds a branch.
  - `[low]` `[reject]` Absent `centralTransitiveDependencyGroups` rejects a legitimate restore — grouped with the earlier `centralTransitiveDependencyGroups` entry; same refutation and same route.
  - `[false]` `[reject]` Long-moniker versus short-alias framework keys make the `:131` equality invalid — inspected all five real assets: `projectFileDependencyGroups`, `centralTransitiveDependencyGroups`, `project.frameworks`, and `originalTargetFrameworks` all use the identical `net10.0` alias in schema v4.
  - `[low]` `[reject]` A platform-specific TFM (`net10.0-windows`) would fail the `originalTargetFrameworks` equality — no release project targets a platform TFM; all five are `net10.0` only.
  - `[low]` `[reject]` Multi-target restore groups are unioned into one flat set, so a dependency could sit in the wrong nuspec group — the release inventory is single-target, so no second framework exists.
  - `[low]` `[reject]` Ambiguous dependency casing raises fatally although the comparator casefolds — fail-closed on inconsistent evidence is what the Always clause asks for; relaxing it adds a canonicalization rule for a state NuGet does not emit.
  - `[low]` `[reject]` The assets schema `version` field is never checked — a future schema would surface as a structural failure through the existing `require_object` guards rather than a silent wrong boundary; adding a version gate is a new branch for an unobserved state.
  - `[low]` `[reject]` Source-referenced restore evidence can become release truth — grouped with the earlier package-mode/source-mode entry; same refutation and route.
  - `[low]` `[reject]` The `tools` basename heuristic misroutes an isolated manifest — grouped with the earlier project-root inference entry.
  - `[low]` `[reject]` `PrivateAssets`/`DevelopmentDependency` dependencies are not excluded from the derived set — grouped with the earlier `PrivateAssets` entry.
  - `[medium]` `[patch]` `PackageGovernanceTests` deleted the only assertions that the validator's forbidden surface still names `Hexalith.Tenants.AppHost`, `Hexalith.Tenants.ServiceDefaults`, and the sample/test fragments; only `.AppHost` had runtime coverage, so removing `ServiceDefaults`, `.Tests`, `.Test`, `.Sample`, or `.Samples` from the policy would have passed every test — re-added membership assertions for the host, composition, sample, and test entries, closing the weakening the intent's Never clause forbids.
  - `[low]` `[reject]` Single-package fixtures lose multi-package inventory and shared-version coverage — grouped with the earlier five-to-one fixture entry.
  - `[false]` `[reject]` The spec's task claims no ledger was touched while the diff flips DW-97/98/120 — grouped with the earlier ledger-claim entry; `9e7ac900` contains no ledger change.
  - `[medium]` `[reject]` The gate no longer detects unreviewed growth of the published dependency surface, because expectation and package now move together from the same `.csproj` edit — verified real (adding a `PackageReference` to a release project now passes where it previously failed), but out of scope: the Always clause prescribes deriving each expected set from restore assets and the Never clause forbids hard-coding or persisting a generated allowlist, so the only proposed fix contradicts the intent contract. Recorded as a residual risk instead.
  - `[low]` `[patch]` `validate-nuget-packages.py:402-411` (`Package id mismatch. Missing/unexpected`) is unreachable: the count guard at `:379`, the duplicate guard at `:391`, and the undeclared-id guard at `:398` together force set equality before it runs — removed the dead block; the specific diagnostics it promised can never print, and the reachable guards already reject every case it covered.
  - `[low]` `[reject]` The manifest contract is re-implemented rather than shared with the packer — grouped with the earlier manifest-duplication entry.
  - `[low]` `[reject]` `PrivateAssets` suppression is unmodeled — grouped with the earlier `PrivateAssets` entry.
  - `[low]` `[reject]` `centralTransitiveDependencyGroups` is hard-required with no well-formed-but-absent test — grouped with the earlier `centralTransitiveDependencyGroups` entry.
  - `[medium]` `[reject]` Intent divergence (a): the Problem statement's expectation lives at the PR-review surface while the diff's check lives at the build-output surface — grouped with the verification-gap entry above; the intent's own I/O matrix is written at the diff's surface, and the Always/Never clauses exclude the policy-surface fix.
  - `[medium]` `[reject]` Intent divergence (b): the reference artifact moved from a tracked literal to untracked `obj/project.assets.json` — grouped with the same entry; this is precisely the mechanism the Always clause prescribes.
  - `[low]` `[reject]` Intent divergence (c): the duplication moved from dependency data to a hand-written model of the assets schema in `WriteRestoreEvidence` — real, but the Never clause bars tests from generating package dependencies from the validator's evidence, which is what a shared model would reintroduce; the real end-to-end shape is covered by the CI gate.
  - `[low]` `[patch]` Intent divergence (d): governance tests assert implementation spelling instead of governed behavior — grouped with the private-identifier patch above; same fix.
  - `[low]` `[reject]` Intent divergence (e): the validator is no longer an independent second witness to the package inventory — the Always clause explicitly makes `tools/release-packages.json` authoritative, and `release.yml`'s `expected-package-count: 5` plus the C# `ExpectedPackageIds` assertion still witness the count independently.
  - `[false]` `[reject]` Intent divergence (f): `casefold()` comparison contradicts "preserve exact dependency equality" — set exactness is preserved; only the ID relation was aligned with NuGet's case-insensitive semantics, and the same normalization strengthened the forbidden check (a lowercase `hexalith.tenants.apphost` is now rejected).
  - `[false]` `[reject]` Intent divergence (g): hunk 1 edits the ledger the Never clause forbids touching — grouped with the earlier ledger-claim entry; the build's commit `9e7ac900` contains no ledger change.

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


## Auto Run Result

Status: done

### Summary

Follow-up review pass over the committed change (`9e7ac900`) that replaced the validator's hard-coded package and dependency inventories with `tools/release-packages.json` plus each declared project's package-mode `obj/project.assets.json`. Four independent review layers reported 49 findings; three entries were patched, one pre-existing item was deferred, and the remainder were refuted or rejected as out of scope. No spec re-derivation was required: no finding routed to intent_gap or bad_spec, and `review_loop_iteration` stayed at 0.

### Files Changed

- `scripts/validate-nuget-packages.py` — derives package inventory and dependency boundaries from the release manifest plus restore assets; this pass removed the unreachable `Package id mismatch` block that the count, duplicate, and undeclared-id guards make impossible to reach.
- `tests/Hexalith.Tenants.Contracts.Tests/CiQualityGateScriptTests.cs` — independent manifest/assets/package CLI fixtures with positive, missing, unexpected, malformed, attribution, casing, and forbidden coverage (unchanged this pass).
- `tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs` — this pass restored the deleted forbidden-dependency membership assertions and replaced private-identifier / exact-source-text greps with `DEFAULT_MANIFEST` and `"release-packages.json"` token assertions.
- `_bmad-output/implementation-artifacts/spec-package-boundary-source.md` — records this review pass, the deferred item, and the terminal result.

### Review Findings

Patches applied: 4 finding rows across 3 root-cause entries — medium 1, low 2.

- Restored the forbidden-dependency governance assertions (`Hexalith.Tenants.AppHost`, `Hexalith.Tenants.ServiceDefaults`, `Hexalith.Tenants.Sample`, `".Tests"`, `".Samples"`). Only `.AppHost` had runtime coverage, so deleting any other entry from the policy would previously have passed the whole suite — a weakening the intent's Never clause forbids.
- Replaced brittle governance assertions that pinned private Python helper names and the exact formatting of the `DEFAULT_MANIFEST` assignment with formatting-independent token assertions.
- Removed the unreachable `Package id mismatch. Missing/unexpected` block at the end of `main()`; the count guard, duplicate guard, and undeclared-id guard together force set equality before it can run.

Items deferred: 1 — `scripts/publish-partial-release.sh:6` reads `HEXALITH_RELEASE_PACKAGE_MANIFEST` into a variable no command consumes (pre-existing at the baseline; `pack-release-packages.py` accepts no manifest argument either).

Rejected findings, each with its recorded reason, are enumerated one row per finding in the `## Review Triage Log` entry for this pass. The substantive groups are: the restore-derived expectation moving with the change it judges (real, but the Always/Never clauses mandate the mechanism and forbid a persisted allowlist — recorded as a residual risk); hypothetical restore shapes no release project produces (`PrivateAssets`, absent `centralTransitiveDependencyGroups`, multi-TFM or platform TFMs, long framework monikers, non-v4 schema, relocated `obj/`); breadth-of-coverage requests for single-`raise` fail-closed branches; findings whose only fix edits this build's spec; and findings whose fix edits the orchestrator-owned deferred-work ledger or a root-declared submodule.

Follow-up review recommendation: false — patched entries by verdict were medium 1, low 2, with no high entry and fewer than two medium entries.

### Verification

Re-ran every command in the `## Verification` section after the patches:

- `python3 -m py_compile scripts/validate-nuget-packages.py` — no output, no errors.
- Package-mode Contracts test restore and Release build — 0 warnings, 0 errors.
- Focused `CiQualityGateScriptTests` — 15 total, 0 errors, 0 failed, 0 skipped, 0 not run.
- Full `Hexalith.Tenants.Contracts.Tests` assembly — 132 total, 0 errors, 0 failed, 0 skipped, 0 not run.
- Package-mode `Hexalith.Tenants.slnx` restore, five Release project builds (0 warnings / 0 errors each), fresh pack, and default-manifest validation — `Validated 5 NuGet packages at version 0.0.0-ci-test`, all five derived dependency sets exactly matching their nuspec metadata.
- `git diff --check` — no findings.

### Residual Risks

- The gate now proves that the packed nuspec agrees with the restore output of the same build; it no longer pins the published dependency surface to a human-reviewed value. Adding a `PackageReference` to a release project updates both sides at once and passes. This is the mechanism the intent contract prescribes and the trade-off DW-98 asked for, and the independent forbidden host/sample/test policy still rejects architectural leaks — but unreviewed growth of an otherwise-legitimate dependency surface no longer has a gate. Raising it again requires a change of intent, not a patch.
- `centralTransitiveDependencyGroups` is required per target framework. Every current release project emits it, but a future packable project with no centrally promoted transitives would fail the gate rather than pass it (fail-closed, and diagnosable from the message).
- A `PrivateAssets="all"` reference on a release project would appear in restore evidence but not in the packed nuspec, producing a `Missing:` failure that names the dependency rather than the suppression. No release project uses `PrivateAssets` today.
- The synthetic `WriteRestoreEvidence` fixture models the assets schema by hand; only the CI gate exercises a real NuGet-emitted assets file.
- The pre-existing broad Release solution build remains blocked outside this bundle at `src/Hexalith.Tenants.AppHost/Program.cs:132` (CS1503). Every package project, the owning Contracts test assembly, and the production package gate pass.
