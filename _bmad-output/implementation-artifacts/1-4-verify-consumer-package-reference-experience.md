---
baseline_commit: 6ce94b8
external_research:
  - https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-pack
  - https://learn.microsoft.com/nuget/consume-packages/package-restore
  - https://learn.microsoft.com/en-us/nuget/reference/nuspec
  - https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/components-overview
---

# Story 1.4: Verify Consumer Package Reference Experience

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a consuming developer,
I want the tenant packages to restore and expose the expected integration surface,
so that I can adopt Hexalith.Tenants without understanding the repository internals.

## Acceptance Criteria

1. Given the five package projects are packed locally or through CI, when the package artifacts are inspected, then Contracts, Client, Server, Testing, and Aspire packages are produced with expected package IDs, and package metadata is consistent with the repository release conventions.
2. Given a sample or verification consumer project references the Contracts and Client packages, when the consumer project restores and builds, then command, event, query, and client registration types are available through the public package surface, and no source-project reference is required for consumer code.
3. Given a test consumer references the Testing package, when the test project restores and builds, then in-memory tenant testing helpers are available to the consumer, and the package does not require live DAPR, EventStore, or Aspire infrastructure for unit-test usage.
4. Given a deployment-oriented consumer references the Aspire package, when the AppHost integration is compiled, then the tenant hosting extension is available through a single documented registration path, and the consumer does not need to duplicate Tenants AppHost wiring manually.
5. Given package dependency metadata is reviewed, when transitive dependencies are inspected, then package dependencies follow the documented Contracts, Client, Server, Testing, and Aspire boundaries, and no package introduces an unexpected dependency on host-only projects.

## Tasks / Subtasks

- [x] Extend package artifact validation for consumer-facing metadata (AC: 1, 5)
  - [x] Reuse `scripts/pack-release-packages.py ./nupkgs 0.0.0-ci-test`; do not create a second package list or hand-pack broad globs.
  - [x] Reuse or extend `scripts/validate-nuget-packages.py` so it still verifies exactly five package IDs: `Hexalith.Tenants.Contracts`, `.Client`, `.Server`, `.Testing`, `.Aspire`.
  - [x] Add dependency-boundary checks against each `.nuspec`: package IDs, one shared version, readme metadata, license metadata, expected package dependencies, and absence of host-only projects.
  - [x] Verify `.snupkg`, symbol packages, stale packages, host projects, AppHost, ServiceDefaults, tests, samples, and submodule packages are excluded from package counts and publish/consumer feeds.

- [x] Add an isolated consumer restore/build smoke test for Contracts and Client (AC: 2, 5)
  - [x] Create a deterministic verification harness that builds a temporary SDK-style consumer project against local `.nupkg` output, not `ProjectReference`.
  - [x] Use an isolated local package source such as `./nupkgs` plus normal external feeds for third-party dependencies; keep generated consumer files under a git-ignored temp/artifact path or create/delete them inside the test.
  - [x] The consumer code must compile usage of `CreateTenant`, at least one success event such as `TenantCreated`, at least one query contract such as `ListTenantsQuery`, and `services.AddHexalithTenants(...)`.
  - [x] Assert the generated consumer project file contains `PackageReference` entries for `Hexalith.Tenants.Contracts` and `Hexalith.Tenants.Client` and contains no `ProjectReference`.

- [x] Add an isolated Testing package consumer smoke test (AC: 3, 5)
  - [x] Compile a temporary test consumer using `Hexalith.Tenants.Testing` from local package output.
  - [x] Prove public access to `InMemoryTenantService`, `TenantTestHelpers`, and `InMemoryTenantProjection`.
  - [x] Run at least one infrastructure-free unit test that creates a tenant through the fake and asserts with Shouldly; do not require DAPR, Docker, Aspire, Redis, or EventStore sidecars.
  - [x] Assert the test consumer uses package references only and does not reach into `src/` or the EventStore submodule through source references.

- [x] Add an isolated Aspire package consumer compile smoke test (AC: 4, 5)
  - [x] Compile a temporary AppHost-style consumer that references `Hexalith.Tenants.Aspire` from local package output.
  - [x] Prove public access to `HexalithTenantsExtensions.AddHexalithTenants(...)` and `HexalithTenantsResources`.
  - [x] Keep the test to compile-time validation unless the implementation deliberately proves a full Aspire run is stable in CI; Story 1.3 keeps Tier 3 runtime tests non-blocking.
  - [x] Assert the consumer does not duplicate Tenants AppHost wiring such as manually creating the `tenants` sidecar, `statestore`, or `pubsub` components outside the public Aspire extension.

- [x] Wire the consumer package-reference evidence into governance tests or scripts (AC: 1-5)
  - [x] Prefer extending `tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs` for deterministic metadata and workflow invariants.
  - [x] Add a focused script only if a test would become unreadable for temporary project generation, package feed creation, restore/build, or `.nuspec` parsing.
  - [x] Ensure CI runs the package-consumer validation after Release build and local package creation, before publish.
  - [x] Keep generated `nupkgs`, temporary consumer directories, `bin`, `obj`, `TestResults`, coverage files, and NuGet cache artifacts ignored and uncommitted.

- [x] Run implementation evidence (AC: 1-5)
  - [x] From `Hexalith.Tenants/`, run `dotnet restore Hexalith.Tenants.slnx`.
  - [x] Run `dotnet build Hexalith.Tenants.slnx --no-restore --configuration Release -warnaserror`.
  - [x] Run focused package governance / consumer package-reference tests.
  - [x] Run `python3 scripts/pack-release-packages.py ./nupkgs 0.0.0-ci-test` and `python3 scripts/validate-nuget-packages.py ./nupkgs`.
  - [x] Run the temporary Contracts+Client consumer restore/build, Testing consumer test, and Aspire consumer compile validation.
  - [x] Record exact blocked diagnostics if network, NuGet feed, DAPR, Docker, Aspire, or VSTest restrictions prevent a command; do not mark blocked commands as passed.

## Dev Notes

### Source Context

- Epic 1 objective: developers can clone, build, test, package, and reference the tenant platform with EventStore-native structure, package boundaries, CI gates, and release foundation. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 1: Developers Can Build and Consume the Tenant Platform`]
- Story 1.4 owns package consumption from produced NuGet artifacts. It must not rework Story 1.2 central package governance or Story 1.3 CI/release gates except to add consumer-reference evidence after package creation. [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.4: Verify Consumer Package Reference Experience`]
- PRD FR43 requires developers to install Hexalith.Tenants through five NuGet packages; FR44, FR46, and FR48 require Client DI registration, infrastructure-free Testing helpers, and Aspire hosting extensions. [Source: `_bmad-output/planning-artifacts/prd.md#Developer Experience & Packaging`]
- The package architecture is: Contracts = commands/events/result/identity surface; Client = DI and event handling; Server = domain service; Testing = in-memory fakes; Aspire = hosting extensions. [Source: `_bmad-output/planning-artifacts/prd.md#NuGet Package Architecture`]
- Architecture maps Epic 1 to root build files, workflows, `src/*`, and `tests/*`; package versions come from `Directory.Packages.props`; published packages are Contracts, Client, Server, Aspire, and Testing. [Source: `_bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping`]

### Current Repository State

- Actual source repository root is `Hexalith.Tenants/`; this story file lives in the parent `_bmad-output/implementation-artifacts/`.
- The source repo was clean when this story was created. Last source commit: `6ce94b8 feat(story-1.3): Add CI Quality Gates for Build, Test, Coverage, and Package Validation`.
- Existing release package scripts:
  - `scripts/pack-release-packages.py` packs exactly the five intended package project paths into a supplied output directory and clears stale `.nupkg` / `.snupkg` files first.
  - `scripts/validate-nuget-packages.py` validates exact package IDs, single shared version, readme metadata, and license metadata, excluding `.snupkg` and symbol packages.
- Existing CI/release workflows already restore/build `Hexalith.Tenants.slnx`, run blocking Tier 1+2 tests, validate coverage, and run semantic-release packaging. Add consumer-reference validation without weakening those gates.
- Existing sample projects under `samples/` still use source `ProjectReference` entries. That is fine for in-repo sample development, but Story 1.4's consumer smoke must prove package-reference usage separately.

### Public Package Surfaces to Verify

- `Hexalith.Tenants.Contracts` currently exposes command records such as `CreateTenant`, event records such as `TenantCreated`, enums such as `TenantRole` and `TenantStatus`, identities, and query contracts such as `ListTenantsQuery`, `GetTenantQuery`, `GetTenantUsersQuery`, `GetUserTenantsQuery`, and `GetTenantAuditQuery`. [Source: `Hexalith.Tenants/src/Hexalith.Tenants.Contracts`]
- `Hexalith.Tenants.Client` exposes `TenantServiceCollectionExtensions.AddHexalithTenants(...)`, `ITenantEventHandler<TEvent>`, `TenantEventProcessor`, `TenantEventEnvelope`, `TenantEventSubscriptionEndpoints.MapTenantEventSubscription()`, `ITenantProjectionStore`, and the in-memory local projection store. [Source: `Hexalith.Tenants/src/Hexalith.Tenants.Client`]
- `Hexalith.Tenants.Testing` exposes `InMemoryTenantService`, `TenantTestHelpers`, and `InMemoryTenantProjection`. These wrap production aggregate/read-model logic and must remain usable without DAPR, Docker, Aspire, Redis, or running EventStore. [Source: `Hexalith.Tenants/src/Hexalith.Tenants.Testing`]
- `Hexalith.Tenants.Aspire` exposes `HexalithTenantsExtensions.AddHexalithTenants(...)` and `HexalithTenantsResources`. The extension currently creates DAPR `statestore` and `pubsub` components and wires the Tenants sidecar with AppId `tenants`. [Source: `Hexalith.Tenants/src/Hexalith.Tenants.Aspire/HexalithTenantsExtensions.cs`]
- `Hexalith.Tenants.Server` is a published package too. Its public aggregate, state, projection, and read-model types are consumer-visible and semver-sensitive, but Story 1.4 should mainly validate dependency metadata and absence of host-only dependencies unless a consumer scenario explicitly needs Server. [Source: `_bmad-output/project-context.md#Public API Surface (NuGet Packages)`]

### Dependency Boundary Expectations

- Expected package dependency shape from PRD:
  - Contracts depends minimally on `Hexalith.EventStore.Contracts`.
  - Client depends on Contracts plus DAPR ASP.NET Core / framework references needed for DI and pub/sub endpoints.
  - Server depends on Contracts, Client/EventStore Server as applicable, DAPR actor/client packages, MediatR, and FluentValidation.
  - Testing depends on Contracts and Server to reuse production domain logic.
  - Aspire depends on Aspire hosting packages and CommunityToolkit DAPR integration.
- Current generated `.nuspec` evidence from prior package output shows ProjectReferences are converted to package dependencies, for example `Hexalith.Tenants.Client` depends on `Hexalith.Tenants.Contracts`, and `Hexalith.Tenants.Testing` depends on `Hexalith.Tenants.Contracts` and `Hexalith.Tenants.Server`.
- Host-only projects must never appear as package dependencies: `Hexalith.Tenants`, `Hexalith.Tenants.AppHost`, `Hexalith.Tenants.ServiceDefaults`, tests, samples, or submodule test/sample projects.
- EventStore remains a source submodule inside this repo, but NuGet consumers restore EventStore package dependencies through package metadata. Do not add consumer `ProjectReference` entries to the EventStore submodule to make the smoke test pass.

### Recommended Implementation Shape

- Add or extend a script such as `scripts/validate-consumer-package-references.py` if temporary project generation is too procedural for xUnit. Keep it deterministic and callable by CI after `pack-release-packages.py`.
- The script/test should:
  - create a temporary directory outside tracked source, preferably under `TestResults/consumer-package-smoke` or `/tmp`;
  - write a minimal `NuGet.Config` or pass restore source arguments that include `./nupkgs` and normal configured sources;
  - create one minimal web/worker consumer for Contracts+Client, one test consumer for Testing, and one AppHost-style compile consumer for Aspire;
  - add `PackageReference` entries with the exact local package version, such as `0.0.0-ci-test`;
  - run `dotnet restore` and `dotnet build` / `dotnet test` against those consumers;
  - inspect generated project files and fail if any `ProjectReference` exists.
- If external dependencies cannot be restored in a local sandbox, record the exact NuGet diagnostics. CI/release environments should still run the canonical restore/build/pack/consumer validation commands.
- Keep consumer smoke code minimal and compile-oriented. This story is proving package installability and public API reachability, not full runtime behavior, domain correctness, or Aspire topology execution.

### Architecture and Technical Guardrails

- Use `Hexalith.Tenants.slnx`; do not create or use a legacy `.sln`.
- Keep central package management. No inline `Version=` or `VersionOverride` on Tenants-owned `PackageReference` entries.
- Do not bump SDK/package versions as part of this story unless a real restore/build blocker proves the current pins are invalid. Current pins include .NET SDK `10.0.300`, DAPR SDK `1.17.9`, Aspire `13.3.5`, CommunityToolkit Aspire DAPR `13.3.0-preview.1.260514-0647`, MediatR `14.1.0`, FluentValidation `12.1.1`, xUnit v3 `3.2.2`, and Shouldly `4.3.0`. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`]
- Do not add Dockerfiles, compose files, or new local infrastructure requirements for package-reference smoke tests.
- Do not commit generated package output, temporary consumer projects, or NuGet cache artifacts.
- Use System.Text.Json, nullable reference types, K&R braces in Tenants code, xUnit v3, and Shouldly for any new tests.
- Temporary consumer tests may use package references and code snippets that differ from in-repo project references by design; that difference is the purpose of this story.

### Previous Story Intelligence

- Story 1.1 established the EventStore-native solution structure, repaired `Hexalith.Tenants.slnx`, kept EventStore submodule projects out of Tenants solution membership, and added structure guard tests.
- Story 1.2 added package/build/container governance tests and reinforced that `Directory.Build.props`, `Directory.Packages.props`, `Directory.Build.targets`, `tests/Directory.Build.props`, and root submodule detection are central governance points.
- Story 1.2 also showed local restore/build/pack can be blocked by NuGet network restrictions and VSTest socket restrictions; blocked commands must be recorded as blocked, not passed.
- Story 1.3 implemented CI/release gates and successfully packed/validated the exact five expected packages in a capable environment. Reuse its scripts/workflow hooks instead of creating a competing release path.
- Story 1.3 recorded one unrelated non-blocking Tier 3 Aspire test failure: `CommandApiRuntimeIntegrationTests.Commands_endpoint_returns_problem_details_for_domain_rejection`. Do not let Story 1.4 absorb that runtime bug; this story is package-reference smoke validation.

### Git Intelligence

- Recent source commits:
  - `6ce94b8 feat(story-1.3): Add CI Quality Gates for Build, Test, Coverage, and Package Validation`
  - `76065d4 feat(story-1.2): Configure Central Build and Package Governance`
  - `fff8fda feat(story-1.1): Establish EventStore-Native Solution Structure`
  - `3c21b14 chore: update sprint status generation date and fix typo in pub-sub validation`
  - `42b03e4 docs: update BMAD planning artifacts`
- Story 1.3 source changes touched `.github/workflows/ci.yml`, `.github/workflows/release.yml`, `release.config.cjs`, `scripts/pack-release-packages.py`, `scripts/validate-nuget-packages.py`, `scripts/validate-coverage.py`, and package governance tests. Build on those files; do not undo pinned actions, bounded artifacts, root-only submodules, or exact package ID validation.

### Latest Technical Notes

- `dotnet pack` creates NuGet packages and supports output-directory and MSBuild property inputs; keep using the existing pack script rather than broad ad hoc `dotnet pack` commands. [Source: Microsoft Learn `dotnet pack`]
- `dotnet restore` restores packages listed by `PackageReference`; a consumer smoke test must therefore use package references and a package source that can resolve local Tenants artifacts plus external dependencies. [Source: Microsoft Learn NuGet package restore]
- `.nuspec` metadata is the package manifest source for dependencies and metadata such as id, version, license, repository, readme, and dependency groups. Validate dependency boundaries from package metadata, not from assumptions in `.csproj` files alone. [Source: Microsoft Learn `.nuspec` reference]
- Aspire hosting integrations extend `IDistributedApplicationBuilder` so AppHost code can express resources in the application model. Story 1.4 only needs compile proof that the Tenants Aspire extension is consumable through that public surface. [Source: Microsoft Learn Aspire integrations overview]

### Testing Standards

- Use xUnit v3 and Shouldly for new tests. Do not add `Assert.*` in new code.
- Tests inherit global `using Xunit` from `tests/Directory.Build.props`; do not add per-file `using Xunit;` in normal test projects.
- Test methods use `snake_case_with_PascalCase_for_type_names`.
- If adding script tests, cover them from `tests/Hexalith.Tenants.Contracts.Tests` the same way Story 1.3 covered coverage/package governance scripts.
- Minimum evidence:
  - package metadata validation still passes for exactly five packages;
  - consumer project with Contracts+Client package refs restores/builds and compiles public command/event/query/DI usage;
  - test consumer with Testing package ref restores/builds/runs without live infrastructure;
  - AppHost compile consumer with Aspire package ref restores/builds and compiles `AddHexalithTenants`;
  - no generated consumer uses source `ProjectReference`.

### Out of Scope

- Domain command/event/projection implementation.
- Changing public API names just to make a smoke test prettier.
- Refactoring `samples/Hexalith.Tenants.Sample` away from project references unless the implementation chooses that as the verification harness and preserves in-repo developer ergonomics.
- Full Aspire runtime execution, DAPR sidecar startup, Docker/Redis/EventStore runtime validation, or Tier 3 test repairs.
- Release tool replacement, semantic-release redesign, package version bumping, or source-link/repository metadata overhaul beyond missing validation.
- Documentation/adoption stories from Epic 8, including full quickstart and event contract reference updates, unless a very small README note is needed to explain the consumer smoke path.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.4: Verify Consumer Package Reference Experience`]
- [Source: `_bmad-output/planning-artifacts/prd.md#NuGet Package Architecture`]
- [Source: `_bmad-output/planning-artifacts/prd.md#Developer Experience & Packaging`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Infrastructure & Deployment`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping`]
- [Source: `_bmad-output/project-context.md#Technology Stack & Versions`]
- [Source: `_bmad-output/project-context.md#Package Management`]
- [Source: `_bmad-output/project-context.md#Publishing`]
- [Source: `_bmad-output/project-context.md#Public API Surface (NuGet Packages)`]
- [Source: `_bmad-output/implementation-artifacts/1-2-configure-central-build-and-package-governance.md#Previous Story Intelligence`]
- [Source: `_bmad-output/implementation-artifacts/1-3-add-ci-quality-gates-for-build-test-coverage-and-package-validation.md#Previous Story Intelligence`]
- [Source: `Hexalith.Tenants/scripts/pack-release-packages.py`]
- [Source: `Hexalith.Tenants/scripts/validate-nuget-packages.py`]
- [Source: `Hexalith.Tenants/tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs`]
- [Source: `Hexalith.Tenants/src/Hexalith.Tenants.Client/Registration/TenantServiceCollectionExtensions.cs`]
- [Source: `Hexalith.Tenants/src/Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs`]
- [Source: `Hexalith.Tenants/src/Hexalith.Tenants.Aspire/HexalithTenantsExtensions.cs`]
- [Source: Microsoft Learn `dotnet pack`](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-pack)
- [Source: Microsoft Learn NuGet package restore](https://learn.microsoft.com/nuget/consume-packages/package-restore)
- [Source: Microsoft Learn `.nuspec` reference](https://learn.microsoft.com/en-us/nuget/reference/nuspec)
- [Source: Microsoft Learn Aspire integrations overview](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/components-overview)

## Project Structure Notes

- Alignment: the source repo already contains the five package projects, package validation scripts, and package governance tests required for this story.
- Likely implementation touches:
  - `Hexalith.Tenants/scripts/validate-nuget-packages.py`
  - possibly a new `Hexalith.Tenants/scripts/validate-consumer-package-references.py`
  - `Hexalith.Tenants/tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs` or a sibling test file
  - `Hexalith.Tenants/.github/workflows/ci.yml`
  - `Hexalith.Tenants/.github/workflows/release.yml`
  - possibly `.gitignore` if a new temp/artifact output path is introduced
- Avoid touching domain aggregates, projections, UI docs, submodule source, generated package outputs, or unrelated BMAD/GitHub assistant files.

## Validation Checklist Results

- Story foundation extracted from Epic 1 and Story 1.4 acceptance criteria.
- PRD and architecture package requirements incorporated: five NuGet packages, Client DI registration, Testing fakes, Aspire extension, and package dependency boundaries.
- Current repository files inspected for likely touches: package project files, package scripts, workflows, release config, package governance tests, Client registration, Testing fakes, Aspire extension, sample projects, and generated prior `.nuspec` evidence.
- Previous Story 1.1, 1.2, and 1.3 learnings incorporated, including central governance tests, exact package validation, CI package gates, and local environment blockers.
- External technical research checked against Microsoft Learn for `dotnet pack`, NuGet restore, `.nuspec` metadata, and Aspire hosting integrations; no version bump is required by this story.
- Anti-reinvention guidance added: reuse existing package scripts/governance tests, generate isolated temporary consumer projects, and avoid source references in consumer smoke validation.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-31: `dotnet restore Hexalith.Tenants.slnx /m:1 /nr:false` passed with all projects up-to-date.
- 2026-05-31: `dotnet build Hexalith.Tenants.slnx --no-restore --configuration Release -warnaserror /m:1 /nr:false` passed with 0 warnings and 0 errors.
- 2026-05-31: `python3 -m py_compile scripts/pack-release-packages.py scripts/validate-nuget-packages.py scripts/validate-consumer-package-references.py` passed.
- 2026-05-31: `python3 scripts/pack-release-packages.py ./nupkgs 0.0.0-ci-test` passed after making the existing pack script pass `/m:1 /nr:false` to avoid MSBuild node socket reuse in restricted environments.
- 2026-05-31: `python3 scripts/validate-nuget-packages.py ./nupkgs` passed against exactly five actual packages at version `0.0.0-ci-test` with expected dependency boundaries.
- 2026-05-31: Focused governance tests and full `dotnet test Hexalith.Tenants.slnx --no-build --configuration Release` were blocked by VSTest socket restrictions: `System.Net.Sockets.SocketException (13): Permission denied` at `Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.SocketServer.Start`.
- 2026-05-31: `python3 scripts/validate-consumer-package-references.py ./nupkgs` generated package-only consumers, then restore was blocked by external NuGet feed access: `NU1301: Unable to load the service index for source https://api.nuget.org/v3/index.json` and `Permission denied (api.nuget.org:443)`.
- 2026-05-31 (Claude Opus 4.8 evidence pass, network + VSTest available): `dotnet test tests/Hexalith.Tenants.Contracts.Tests --configuration Release --filter PackageGovernanceTests` passed 10/10; VSTest sockets work in this environment, so the prior socket block no longer applies.
- 2026-05-31: First `validate-consumer-package-references.py ./nupkgs` run surfaced a real harness defect: the Contracts+Client consumer build failed under `-warnaserror` on `NU1603` because the local `0.0.0-ci-test` pack stamps that placeholder version onto the source-submodule EventStore dependency lower bound, and restore legitimately resolves up to the published `Hexalith.EventStore.Contracts 1.2.1`. Fixed by adding `-p:WarningsNotAsErrors=NU1603` to the consumer build so genuine compiler `-warnaserror` stays on but the benign placeholder-version restore substitution does not fail the package-reference smoke. Re-run passed: Contracts+Client build, Testing infra-free unit test (1 passed), Aspire compile (0 warnings).
- 2026-05-31: Full unit suites passed: Contracts 64/64, Client 51/51, Server 488/488, Testing 93/93.
- 2026-05-31: Running the full Contracts.Tests surfaced 2 regressions introduced by this story's `validate-nuget-packages.py` dependency-boundary check — Story 1.3 `CiQualityGateScriptTests` synthetic packages carried no `<dependencies>`, so the validator failed them on boundary mismatch instead of the license/version behavior they assert. Fixed the fixtures to emit the expected dependency metadata; Contracts.Tests now 64/64.
- 2026-05-31: `dotnet build Hexalith.Tenants.slnx --no-restore --configuration Release -warnaserror` passed with 0 warnings / 0 errors after the fixture fix; re-pack + `validate-nuget-packages.py` + `validate-consumer-package-references.py` all green end-to-end.
- 2026-05-31: `IntegrationTests` (Tier 3): 58 passed, 13 skipped (DAPR/Aspire infra unavailable → `SkipIfUnavailable`/`[DaprFact]`), 1 failed — `CommandApiRuntimeIntegrationTests.Commands_endpoint_returns_problem_details_for_domain_rejection`. This is the documented pre-existing Story 1.3 non-blocking Tier-3 runtime failure (asserts `ProblemDetails.Title` "Conflict" vs runtime "Global Admin Already Bootstrapped Rejection"); it is unrelated to packaging and explicitly out of Story 1.4 scope, so it is not absorbed here.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Extended NuGet package validation to parse `.nuspec` dependencies, enforce exact package dependency boundaries for Contracts, Client, Server, Testing, and Aspire, and reject host/sample/test dependency leaks.
- Added deterministic isolated consumer package-reference validation that generates Contracts+Client, Testing, and Aspire consumers under `/tmp`, asserts package-only project files, compiles public API usage, and runs an infrastructure-free Testing consumer unit test when NuGet restore is available.
- Wired package consumer validation into CI after Release build/local pack and into semantic-release before NuGet publish.
- Added governance tests pinning the package validator, consumer smoke script, and CI/release workflow hooks.
- Evidence/verification pass completed by Claude Opus 4.8 in an environment with working NuGet network and VSTest sockets (the conditions the prior agent recorded as blocked). All implementation-evidence subtasks now run green; no commands remain blocked.
- Fixed a verification-harness defect: `validate-consumer-package-references.py` failed the consumer build under `-warnaserror` on the benign `NU1603` placeholder-version restore substitution for the EventStore source-submodule dependency. Added `-p:WarningsNotAsErrors=NU1603` so real compiler warnings still fail the smoke build but the expected transitive version resolution does not. This was a real CI-gate-reddening bug, since CI packs with `0.0.0-ci-test`.
- Fixed a regression this story introduced into Story 1.3's `CiQualityGateScriptTests`: the new dependency-boundary enforcement in `validate-nuget-packages.py` rejected the tests' synthetic packages (which had no `<dependencies>`). Updated the synthetic-package fixtures to emit the expected dependency metadata so they again isolate license/symbol/version behavior; `Hexalith.Tenants.Contracts.Tests` is now 64/64.
- ⚠️ Follow-up note for reviewers (out of Story 1.4 scope): the packed Tenants packages declare their EventStore dependency lower bound equal to the pack version (e.g. `Hexalith.EventStore.Contracts >= 0.0.0-ci-test`) because EventStore is referenced via source `ProjectReference` and the global `-p:Version` applies across the build graph. With the CI smoke version this resolves up to the published EventStore version, but real releases must ensure a satisfying published EventStore version exists (otherwise restore would hard-fail with NU1605/NU1102, not NU1603). Version/release-machinery changes are out of scope here.
- Verified pre-existing Tier-3 `CommandApiRuntimeIntegrationTests.Commands_endpoint_returns_problem_details_for_domain_rejection` failure is a runtime/domain issue unrelated to packaging and not introduced by this story; left untouched per story scope.
- Senior review auto-fix completed: `validate-consumer-package-references.py` now writes a temporary `NuGet.Config` that adds the local package output while preserving inherited NuGet configuration sources, instead of overriding feeds with hard-coded `--source` arguments. This keeps the smoke compatible with normal developer, CI, and mirrored/private feed setups.

### Senior Developer Review (AI)

Reviewer: Codex on 2026-05-31

Outcome: Approved after auto-fix.

Findings:

- [MEDIUM] Fixed consumer smoke restore feed handling in `Hexalith.Tenants/scripts/validate-consumer-package-references.py`. The script previously restored with explicit `--source ./nupkgs --source https://api.nuget.org/v3/index.json`, which overrides normal NuGet configuration sources and conflicts with the story requirement to use local package output plus normal external feeds. It now writes a temporary `NuGet.Config` containing the local feed without clearing inherited sources, and supports repeatable `--nuget-source` entries for additional feeds.

Validation performed:

- MCP resource discovery attempted; no MCP resources were configured in this environment. Existing story references to Microsoft Learn package/restore/nuspec/Aspire documentation were used as the captured external reference set.
- `python3 -m py_compile scripts/pack-release-packages.py scripts/validate-nuget-packages.py scripts/validate-consumer-package-references.py` passed.
- `dotnet build Hexalith.Tenants.slnx --no-restore --configuration Release -warnaserror /m:1 /nr:false` passed with 0 warnings and 0 errors.
- `dotnet tests/Hexalith.Tenants.Contracts.Tests/bin/Release/net10.0/Hexalith.Tenants.Contracts.Tests.dll -parallel none -noLogo` passed 64/64.
- `dotnet tests/Hexalith.Tenants.Client.Tests/bin/Release/net10.0/Hexalith.Tenants.Client.Tests.dll -parallel none -noLogo` passed 51/51.
- `dotnet tests/Hexalith.Tenants.Testing.Tests/bin/Release/net10.0/Hexalith.Tenants.Testing.Tests.dll -parallel none -noLogo` passed 93/93.
- `python3 scripts/pack-release-packages.py ./nupkgs 0.0.0-ci-test && python3 scripts/validate-nuget-packages.py ./nupkgs && python3 scripts/validate-consumer-package-references.py ./nupkgs` passed end-to-end.
- `dotnet test tests/Hexalith.Tenants.Contracts.Tests/ --no-build --configuration Release --filter "PackageGovernanceTests|CiQualityGateScriptTests"` remains blocked in this sandbox by VSTest socket permissions: `System.Net.Sockets.SocketException (13): Permission denied`. The xUnit v3 assembly runner path was used for local verification.

### File List

- `Hexalith.Tenants/.github/workflows/ci.yml`
- `Hexalith.Tenants/release.config.cjs`
- `Hexalith.Tenants/scripts/pack-release-packages.py`
- `Hexalith.Tenants/scripts/validate-consumer-package-references.py`
- `Hexalith.Tenants/scripts/validate-nuget-packages.py`
- `Hexalith.Tenants/tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs`
- `Hexalith.Tenants/tests/Hexalith.Tenants.Contracts.Tests/CiQualityGateScriptTests.cs`
- `_bmad-output/implementation-artifacts/1-4-verify-consumer-package-reference-experience.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Change Log

- 2026-05-31: Implemented consumer package-reference validation and dependency-boundary governance for Story 1.4; validation partially blocked by sandbox VSTest socket and NuGet network restrictions.
- 2026-05-31: Completed implementation-evidence pass with working network/VSTest. Fixed consumer-smoke `NU1603` `-warnaserror` failure (`-p:WarningsNotAsErrors=NU1603`) and repaired the `CiQualityGateScriptTests` synthetic-package fixtures broken by the new dependency-boundary check. All in-scope tests, package validation, and consumer smoke now pass (Contracts 64/64, Client 51/51, Server 488/488, Testing 93/93; pack/validate/consumer green; full Release build clean). One pre-existing out-of-scope Tier-3 runtime integration failure remains and is documented. Status → review.
- 2026-05-31: Senior review auto-fix applied for consumer restore source handling; local package source is now added through temporary NuGet.Config while preserving inherited configured feeds. Revalidated build, package metadata, consumer smoke, and in-scope xUnit v3 assemblies. Status → done.
