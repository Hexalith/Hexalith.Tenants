---
baseline_commit: 3c21b146f9cafc8e42d82729c7ad979ad1451df5
---

# Story 1.1: Establish EventStore-Native Solution Structure

Status: done

## Story

As a developer,
I want the Tenants repository to use the EventStore-native solution and project structure,
so that I can build and extend the tenant platform using the expected Hexalith package boundaries.

## Acceptance Criteria

1. Given the repository is checked out with root-level submodules initialized, when a developer opens the solution, then `Hexalith.Tenants.slnx` contains the expected source projects for Contracts, Client, Server, host, Aspire, AppHost, ServiceDefaults, and Testing, and matching test projects exist under `tests/`.
2. Given the solution structure is reviewed, when project references are inspected, then Contracts remains the public immutable API surface, Server owns aggregate, state, validator, projection, and read-model implementation, and Client, Testing, Aspire, AppHost, and host projects preserve their documented boundaries.
3. Given the repo uses Hexalith.EventStore as a submodule dependency, when dependency setup is documented or validated, then only root-level submodule initialization is required, and no recursive submodule initialization command is introduced.
4. Given a developer runs a focused restore/build for the solution, when project structure is valid, then the build uses `Hexalith.Tenants.slnx` and does not require a legacy `.sln` file.

## Tasks / Subtasks

- [x] Verify and repair the Tenants solution inventory (AC: 1, 4)
  - [x] Work from the actual repository root: `Hexalith.Tenants/`, not the parent BMAD workspace.
  - [x] Ensure `Hexalith.Tenants.slnx` lists these source projects under `src/`: `Hexalith.Tenants.Contracts`, `Hexalith.Tenants.Client`, `Hexalith.Tenants.Server`, `Hexalith.Tenants`, `Hexalith.Tenants.Aspire`, `Hexalith.Tenants.AppHost`, `Hexalith.Tenants.ServiceDefaults`, and `Hexalith.Tenants.Testing`.
  - [x] Ensure `Hexalith.Tenants.slnx` lists these matching test projects under `tests/`: `Hexalith.Tenants.Contracts.Tests`, `Hexalith.Tenants.Client.Tests`, `Hexalith.Tenants.Server.Tests`, `Hexalith.Tenants.Testing.Tests`, and `Hexalith.Tenants.IntegrationTests`.
  - [x] Keep sample projects in `samples/` if present, but do not use samples as substitutes for the required source/test projects.
  - [x] Do not create a root-level legacy `.sln` file.
- [x] Verify and repair project boundary references (AC: 2)
  - [x] `Contracts` must stay the public immutable API surface and must not reference any `Hexalith.Tenants.*` project.
  - [x] `Server` must own aggregates, state, validators, projections, and read models; EventStore-discovered aggregate/projection types must remain in `src/Hexalith.Tenants.Server`.
  - [x] `Client` must remain the consumer integration surface; do not add domain behavior, aggregate state, projection write policy, or host wiring to Client.
  - [x] `Testing` must reuse production domain logic from `Server` and `Contracts`; do not fork aggregate rules into test-only implementations.
  - [x] `Aspire` and `AppHost` must own orchestration/hosting composition only; do not add domain rules there.
  - [x] The host project `src/Hexalith.Tenants` owns HTTP hosting, authentication setup, command gateway integration, query controllers, domain processing route, health checks, and telemetry.
- [x] Validate EventStore submodule setup and solution dependency rules (AC: 3)
  - [x] Keep `Hexalith.EventStore` as a root-level git submodule dependency; do not replace it with a NuGet package reference.
  - [x] Document or preserve the setup command as `git submodule update --init`; do not add `--recursive`.
  - [x] Do not modify submodule code or submodule solution files unless a separate task explicitly targets that submodule.
  - [x] Preserve the root `HexalithEventStoreRoot` auto-detection behavior in `Directory.Build.props`.
- [x] Validate focused restore/build behavior (AC: 4)
  - [x] Run `dotnet restore Hexalith.Tenants.slnx` from `Hexalith.Tenants/`.
  - [x] Run `dotnet build Hexalith.Tenants.slnx --no-restore` from `Hexalith.Tenants/`.
  - [x] If either command fails, fix the structural cause or document the exact blocking diagnostic and the file causing it; do not leave a silent solution-level failure.
  - [x] Confirm no command path requires a legacy `.sln`.
- [x] Add or update structure guard tests/scripts only if needed to make this story durable (AC: 1, 2, 4)
  - [x] Prefer a focused test or validation script that checks solution project membership and dependency direction over broad unrelated build churn.
  - [x] If adding tests, follow xUnit v3 plus Shouldly conventions and place them in the test project that owns the checked behavior.
  - [x] Do not expand Story 1.1 into package metadata, CI, or publish validation work; those belong to Stories 1.2 and 1.3.

## Dev Notes

### Source Context

- Epic 1 objective: developers can clone, build, test, package, and reference the tenant platform with EventStore-native project structure, package boundaries, CI gates, and release foundation in place. [Source: _bmad-output/planning-artifacts/epics.md#Epic 1: Developers Can Build and Consume the Tenant Platform]
- Story 1.1 is the foundation story for the EventStore-native solution structure and must preserve consumer-facing build usability, not just rearrange infrastructure files. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.1: Establish EventStore-Native Solution Structure] [Source: _bmad-output/planning-artifacts/implementation-readiness-report-2026-05-31.md#Minor 1 - Epic 1 Is Infrastructure-Heavy but Acceptable for This Developer Tool]
- The PRD defines Tenants as a .NET developer tool distributed as five NuGet packages plus a deployable microservice: Contracts, Client, Server, Testing, and Aspire. [Source: _bmad-output/planning-artifacts/prd.md#NuGet Package Architecture]
- Phase 1 has no frontend implementation requirement. Do not pull Phase 2 UI/FrontComposer work into this story. [Source: _bmad-output/planning-artifacts/architecture.md#Frontend Architecture]

### Current Repository State

- The actual code repository root is `Hexalith.Tenants/`. BMAD planning and story files are in the parent workspace `_bmad-output/` and are untracked implementation artifacts.
- Existing required source projects found:
  - `src/Hexalith.Tenants.Contracts/Hexalith.Tenants.Contracts.csproj`
  - `src/Hexalith.Tenants.Client/Hexalith.Tenants.Client.csproj`
  - `src/Hexalith.Tenants.Server/Hexalith.Tenants.Server.csproj`
  - `src/Hexalith.Tenants/Hexalith.Tenants.csproj`
  - `src/Hexalith.Tenants.Aspire/Hexalith.Tenants.Aspire.csproj`
  - `src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj`
  - `src/Hexalith.Tenants.ServiceDefaults/Hexalith.Tenants.ServiceDefaults.csproj`
  - `src/Hexalith.Tenants.Testing/Hexalith.Tenants.Testing.csproj`
- Existing required test projects found:
  - `tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj`
  - `tests/Hexalith.Tenants.Client.Tests/Hexalith.Tenants.Client.Tests.csproj`
  - `tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj`
  - `tests/Hexalith.Tenants.Testing.Tests/Hexalith.Tenants.Testing.Tests.csproj`
  - `tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj`
- `Hexalith.Tenants.slnx` already exists and contains the required Tenants source/test projects plus sample projects and selected EventStore submodule projects.
- Local probe on 2026-05-31: `dotnet restore Hexalith.Tenants.slnx` and `dotnet build Hexalith.Tenants.slnx --no-restore` both exited with code 1 immediately after `ValidateSolutionConfiguration`, with zero warnings and zero errors. Treat this as an in-scope structure/build usability issue unless a later diagnostic proves it is environmental.
- `find . -name '*.sln' -o -name '*.slnx'` found no root-level `Hexalith.Tenants.sln`; legacy `.sln` files exist only inside submodules such as `Hexalith.Builds/` and `Hexalith.Commons/`. Do not edit submodule solution files for this story.
- The repo currently has tracked `*.csproj.lscache` files and `status.txt`; project context says not to add new local-cache files and that cleanup is separate unless explicitly assigned. Do not broaden this story into cache cleanup without a direct need.

### Architecture and Boundary Requirements

- Selected starter is not a CLI template. Preserve the manual Hexalith.EventStore structure mirror; do not run `aspire new` over the repository. [Source: _bmad-output/planning-artifacts/architecture.md#Selected Starter: Hexalith.EventStore Structure Mirror]
- Use the modern XML solution format: `Hexalith.Tenants.slnx`. Never create or require `.sln` for Tenants build commands. [Source: _bmad-output/project-context.md#Technology Stack & Versions] [Source: _bmad-output/planning-artifacts/architecture.md#Development Workflow Integration]
- Root-level shared build files own common settings: `global.json`, `Directory.Build.props`, `Directory.Build.targets`, and `Directory.Packages.props`. Runtime `appsettings*.json` stay in the owning host project. [Source: _bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries]
- Runtime and language are .NET 10 SDK `10.0.300`, C# latest, nullable references enabled, implicit usings enabled, and warnings as errors. Do not bump SDK or package versions in this story. [Source: _bmad-output/project-context.md#Technology Stack & Versions]
- Central package management is mandatory. Do not add inline `Version=` attributes to `PackageReference`. [Source: _bmad-output/project-context.md#Package Management]
- Dependency direction must not reverse toward `Contracts`. Cross-module references should stay same-tier with EventStore where applicable. [Source: _bmad-output/project-context.md#Project Dependency Direction (Hard Architectural Constraint)]
- Aggregates and projections must live in `Hexalith.Tenants.Server`, the assembly scanned by EventStore. Misplacing these types can create silent runtime registration failures. [Source: _bmad-output/project-context.md#Convention-Driven Wiring (Reflection)]

### File-Specific Guidance for Likely Touches

- `Hexalith.Tenants.slnx`: update only to correct solution membership/folders. Preserve required Tenants source/test projects. Keep submodule project entries only if they are needed by the focused restore/build experience; do not replace `.slnx` with `.sln`.
- `Directory.Build.props`: preserve `HexalithEventStoreRoot` auto-detection, `TargetFramework=net10.0`, nullable/implicit usings, `TreatWarningsAsErrors`, `LangVersion=latest`, and default package metadata. Do not remove the four-layout EventStore root detection.
- `.gitmodules`: preserve root-level submodules `Hexalith.EventStore`, `Hexalith.AI.Tools`, `Hexalith.Builds`, `Hexalith.Commons`, and `Hexalith.FrontComposer`. Do not introduce recursive submodule instructions.
- `src/*/*.csproj` and `tests/*/*.csproj`: check project references against the boundary table before changing them. Adding references just to make compile errors disappear is not acceptable if it reverses dependency direction.

### Testing Standards

- Use xUnit v3 and Shouldly for new or changed tests. Do not use `Assert.*`.
- Test projects inherit shared test settings from `tests/Directory.Build.props`; do not add per-file `using Xunit;`.
- Test class naming is `{TypeUnderTest}Tests.cs`; test method naming uses `snake_case_with_PascalCase_for_type_names`.
- For this story, minimum evidence is:
  - `dotnet restore Hexalith.Tenants.slnx`
  - `dotnet build Hexalith.Tenants.slnx --no-restore`
  - a solution/project membership check, either by focused automated test/script or by explicit command output in completion notes.
- Tier 2 DAPR or Tier 3 Aspire runtime tests are not required unless the implementation changes AppHost/DAPR runtime behavior beyond solution membership/reference repair.

### Out of Scope

- Package metadata and publishability governance beyond preserving existing boundaries; Story 1.2 owns central build/package governance.
- CI workflow, coverage gates, package validation, and artifact validation; Story 1.3 owns this.
- Consumer package reference verification; Story 1.4 owns this.
- Domain commands, events, projections, authorization behavior, query endpoints, or Phase 2 UI work unless required only to keep the existing solution buildable.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.1: Establish EventStore-Native Solution Structure]
- [Source: _bmad-output/planning-artifacts/prd.md#Solution & Project Structure]
- [Source: _bmad-output/planning-artifacts/prd.md#NuGet Package Architecture]
- [Source: _bmad-output/planning-artifacts/architecture.md#Selected Starter: Hexalith.EventStore Structure Mirror]
- [Source: _bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries]
- [Source: _bmad-output/project-context.md#Technology Stack & Versions]
- [Source: _bmad-output/project-context.md#Project Dependency Direction (Hard Architectural Constraint)]
- [Source: _bmad-output/project-context.md#Submodules]
- [Source: _bmad-output/planning-artifacts/implementation-readiness-report-2026-05-31.md#Minor 1 - Epic 1 Is Infrastructure-Heavy but Acceptable for This Developer Tool]

## Project Structure Notes

- Alignment: the current repository already contains the expected `src/`, `tests/`, `samples/`, root build files, root-level submodules, and `Hexalith.Tenants.slnx` shape described by the PRD and architecture.
- Variance to resolve: the focused `dotnet restore/build` probe against `Hexalith.Tenants.slnx` failed silently after solution configuration validation. Story 1.1 should make the solution usable enough that developers get a successful focused build or a concrete diagnostic tied to a specific project/configuration.
- Do not treat submodule `.sln` files as Tenants legacy solution files. The no-legacy-sln rule applies to the Tenants root build path.

## Previous Story Intelligence

- None. This is the first story in Epic 1.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `dotnet restore Hexalith.Tenants.slnx` from `Hexalith.Tenants/`: passed.
- `dotnet build Hexalith.Tenants.slnx --no-restore` from `Hexalith.Tenants/`: passed, 0 warnings, 0 errors.
- `dotnet tests/Hexalith.Tenants.Contracts.Tests/bin/Debug/net10.0/Hexalith.Tenants.Contracts.Tests.dll`: passed 44/44.
- `dotnet tests/Hexalith.Tenants.Client.Tests/bin/Debug/net10.0/Hexalith.Tenants.Client.Tests.dll`: passed 51/51.
- `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll`: passed 486/486.
- `dotnet tests/Hexalith.Tenants.Testing.Tests/bin/Debug/net10.0/Hexalith.Tenants.Testing.Tests.dll`: passed 93/93.
- `dotnet test Hexalith.Tenants.slnx --no-build`: blocked by sandbox/VSTest TCP listener creation with `System.Net.Sockets.SocketException (13): Permission denied`.
- `dotnet tests/Hexalith.Tenants.IntegrationTests/bin/Debug/net10.0/Hexalith.Tenants.IntegrationTests.dll`: DAPR fixture tests blocked by sandbox TCP listener creation in `TenantsDaprTestFixture.GetAvailablePorts`; non-DAPR integration tests ran, 10 failed due fixture initialization, 4 skipped due missing DAPR prerequisites.

### Completion Notes List

- Repaired the root `Hexalith.Tenants.slnx` inventory so it lists the required Tenants source/test projects without carrying EventStore submodule projects as solution members or requiring a legacy `.sln`.
- Added tracked root MSBuild solution defaults to make `dotnet restore Hexalith.Tenants.slnx` and `dotnet build Hexalith.Tenants.slnx --no-restore` deterministic under the current solution graph.
- Kept `Hexalith.EventStore` as a root-level submodule and preserved `HexalithEventStoreRoot` autodetection in `Directory.Build.props`.
- Removed AppHost project-resource references from the project file and replaced generated metadata with explicit Aspire project metadata so AppHost can keep orchestration references without forcing cross-boundary project references.
- Added xUnit v3/Shouldly solution-structure guard tests covering solution membership, boundary references, submodule setup, and no root legacy `.sln`.
- Full VSTest and DAPR integration execution is environment-blocked in this sandbox by socket creation restrictions; focused restore/build and in-process unit/guard assemblies pass.

### File List

- `Hexalith.Tenants/.gitignore`
- `Hexalith.Tenants/Directory.Solution.props`
- `Hexalith.Tenants/Directory.Solution.targets`
- `Hexalith.Tenants/MSBuild.rsp`
- `Hexalith.Tenants/Hexalith.Tenants.slnx`
- `Hexalith.Tenants/src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj`
- `Hexalith.Tenants/src/Hexalith.Tenants.AppHost/Hexalith_EventStore.cs`
- `Hexalith.Tenants/src/Hexalith.Tenants.AppHost/Hexalith_EventStore_Admin_Server_Host.cs`
- `Hexalith.Tenants/src/Hexalith.Tenants.AppHost/Hexalith_EventStore_Admin_UI.cs`
- `Hexalith.Tenants/src/Hexalith.Tenants.AppHost/Hexalith_Tenants.cs`
- `Hexalith.Tenants/src/Hexalith.Tenants.AppHost/Hexalith_Tenants_Sample.cs`
- `Hexalith.Tenants/src/Hexalith.Tenants.AppHost/ProjectMetadataPaths.cs`
- `Hexalith.Tenants/tests/Hexalith.Tenants.Contracts.Tests/SolutionStructureTests.cs`
- `_bmad-output/implementation-artifacts/1-1-establish-eventstore-native-solution-structure.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Senior Developer Review (AI)

Reviewer: Codex on 2026-05-31

Outcome: Approved after auto-fixes. No critical issues remain.

Findings fixed:

- MEDIUM: `Hexalith.Tenants.slnx` marked `src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj` with `<Build Project="false" />`, but `Directory.Solution.targets` still built AppHost through its serial project-reference target. Removed the contradictory solution build suppression and updated the guard test to verify the AppHost remains a normal solution member while its Aspire reference stays out of the project resource graph.
- MEDIUM: `MSBuild.rsp` disabled build parallelism but did not force single-node MSBuild execution. Added `-m:1` and expanded the guard test so focused solution builds stay deterministic in constrained environments.
- LOW: `EventStoreAdminProjectMetadata.cs` contained five public metadata classes plus duplicated path helpers, violating the one-type-per-file source convention. Split the metadata into per-type files and a shared `ProjectMetadataPaths` helper.

Validation:

- `dotnet restore Hexalith.Tenants.slnx`: passed.
- `dotnet build Hexalith.Tenants.slnx --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet tests/Hexalith.Tenants.Contracts.Tests/bin/Debug/net10.0/Hexalith.Tenants.Contracts.Tests.dll`: passed 47/47.
- `dotnet test tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --no-build`: blocked by sandbox/VSTest TCP listener creation with `System.Net.Sockets.SocketException (13): Permission denied`.
- MCP doc search: no MCP resources were configured in this session.

### Change Log

- 2026-05-31: Implemented EventStore-native solution structure repair and guard tests for Story 1.1.
- 2026-05-31: Senior developer review auto-fixed AppHost solution metadata, single-node MSBuild defaults, and AppHost metadata file structure; marked story done.
