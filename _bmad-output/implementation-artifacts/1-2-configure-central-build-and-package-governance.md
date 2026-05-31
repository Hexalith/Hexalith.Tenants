---
baseline_commit: fff8fda
external_research:
  - https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management
  - https://learn.microsoft.com/en-au/nuget/reference/errors-and-warnings/nu1008
  - https://learn.microsoft.com/en-us/nuget/reference/msbuild-targets
  - https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-pack
  - https://learn.microsoft.com/en-us/dotnet/core/containers/publish-configuration
---

# Story 1.2: Configure Central Build and Package Governance

Status: done

## Story

As a package maintainer,
I want build and package settings governed centrally,
so that every Hexalith.Tenants package follows consistent versioning, warnings, metadata, and dependency rules.

## Acceptance Criteria

1. Given package references are reviewed across source and test projects, when a project references a NuGet package, then the project uses central package management through `Directory.Packages.props`, and no project-level `PackageReference` contains an inline `Version=` attribute.
2. Given shared build settings are reviewed, when a developer builds the solution, then nullable references, implicit usings, latest C# language version, and warnings-as-errors are applied consistently from shared build configuration.
3. Given published package projects are inspected, when package metadata and pack settings are evaluated, then Contracts, Client, Server, Testing, and Aspire are configured as publishable packages, and host projects, AppHost, and ServiceDefaults are not packable.
4. Given container or publish settings are reviewed, when host projects are prepared for deployment, then container defaults come from shared build targets or documented host configuration, and no Dockerfile or ad hoc publish convention is introduced for Phase 1 foundation work.
5. Given package governance tests or validation scripts run, when a project violates central versioning or packability expectations, then validation fails with enough detail for a developer to identify the offending project.

## Tasks / Subtasks

- [x] Audit central package management and remove package-version drift (AC: 1, 5)
  - [x] Verify `Hexalith.Tenants/Directory.Packages.props` has `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>`.
  - [x] Scan all Tenants-owned `.csproj` files under `src/`, `tests/`, and `samples/` for `<PackageReference ... Version=...>` and move any versions to `Directory.Packages.props`.
  - [x] Treat `<PackageReference Update="...">` metadata-only entries as valid only when they do not contain `Version` or `VersionOverride`.
  - [x] Do not edit submodule package files under `Hexalith.EventStore`, `Hexalith.Commons`, `Hexalith.Builds`, or `Hexalith.FrontComposer`.
- [x] Harden shared build configuration (AC: 2, 5)
  - [x] Preserve `TargetFramework=net10.0`, `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`, and `LangVersion=latest` in root `Directory.Build.props`.
  - [x] Do not add StyleCop, SonarAnalyzer, Roslynator, or default `GenerateDocumentationFile=true`; project context explicitly says Tenants does not use those as global defaults.
  - [x] Preserve `HexalithEventStoreRoot` four-layout auto-detection and the root-submodule reference model.
  - [x] Add or extend a focused governance test/script that reports the exact project and offending XML node when central-build rules are violated.
- [x] Verify packability and NuGet metadata for the five package projects (AC: 3, 5)
  - [x] Confirm these projects are packable through shared defaults unless a project-specific override is justified: `src/Hexalith.Tenants.Contracts`, `.Client`, `.Server`, `.Testing`, and `.Aspire`.
  - [x] Confirm these projects are explicitly non-packable: `src/Hexalith.Tenants`, `src/Hexalith.Tenants.AppHost`, `src/Hexalith.Tenants.ServiceDefaults`, all `tests/*`, and sample host/test projects.
  - [x] Verify shared package metadata remains centralized in `Directory.Build.props`: authors, company, MIT license, project URL, repository URL/type, description, tags, and README packaging.
  - [x] If package-specific descriptions are added, keep them scoped to the package project and do not duplicate every shared metadata property.
- [x] Verify deployment publish/container governance (AC: 4, 5)
  - [x] Preserve .NET SDK container publishing defaults in `Directory.Build.targets`, including base image, registry default, non-root user, port, and OCI labels.
  - [x] Confirm host project `src/Hexalith.Tenants/Hexalith.Tenants.csproj` opts into container publishing with `EnableContainer=true` and `ContainerRepository=tenants`.
  - [x] Do not introduce a Dockerfile, compose file, or custom publish script for Phase 1 foundation work unless a failing validation proves the existing .NET SDK path cannot satisfy deployment requirements.
- [x] Validate build, pack, and governance evidence (AC: 1-5)
  - [x] Run `dotnet restore Hexalith.Tenants.slnx` from `Hexalith.Tenants/`.
  - [x] Run `dotnet build Hexalith.Tenants.slnx --no-restore` from `Hexalith.Tenants/`.
  - [x] Run the focused governance tests or script added/updated by this story.
  - [x] Run `dotnet pack` for the five publishable package projects, preferably `--configuration Release --no-build` after a Release build if build output is current.
  - [x] Record exact blocked diagnostics if sandbox, DAPR, or VSTest socket restrictions prevent any command; do not mark blocked commands as passed.

## Dev Notes

### Source Context

- Epic 1 objective: developers can clone, build, test, package, and reference the tenant platform with EventStore-native project structure, package boundaries, CI gates, and release foundation in place. [Source: _bmad-output/planning-artifacts/epics.md#Epic 1: Developers Can Build and Consume the Tenant Platform]
- Story 1.2 owns central build and package governance. It should not absorb CI workflow gates from Story 1.3 or consumer reference verification from Story 1.4. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.2: Configure Central Build and Package Governance]
- The PRD defines five NuGet packages: `Hexalith.Tenants.Contracts`, `.Client`, `.Server`, `.Testing`, and `.Aspire`. Host projects are deployable/runtime infrastructure, not packages. [Source: _bmad-output/planning-artifacts/prd.md#NuGet Package Architecture]
- Package quality standards include Source Link, deterministic builds, XML documentation expectations where applicable, semantic-release, centralized package management, and CI validation of expected package count before NuGet push. This story should establish local governance evidence; CI enforcement belongs to Story 1.3. [Source: _bmad-output/planning-artifacts/prd.md#NuGet Package Architecture]
- Development workflow architecture says build uses `Hexalith.Tenants.slnx`, package versions come only from `Directory.Packages.props`, published packages are Contracts, Client, Server, Aspire, and Testing, host projects are not NuGet packages, and container publishing uses .NET SDK container support. [Source: _bmad-output/planning-artifacts/architecture.md#Development Workflow Integration]

### Current Repository State

- Actual code repository root is `Hexalith.Tenants/`. BMAD story files live in the parent `_bmad-output/implementation-artifacts/`.
- Story 1.1 left `Hexalith.Tenants.slnx` buildable and added structure guard tests in `tests/Hexalith.Tenants.Contracts.Tests/SolutionStructureTests.cs`.
- Current root governance files:
  - `Directory.Packages.props` enables central package management and pins DAPR `1.17.9`, Aspire `13.x`, MediatR `14.1.0`, FluentValidation `12.1.1`, xUnit v3 `3.2.2`, Shouldly `4.3.0`, NSubstitute `6.0.0-rc.1`, Testcontainers `4.10.0`, and related packages.
  - `Directory.Build.props` centralizes `net10.0`, nullable, implicit usings, warnings-as-errors, `LangVersion=latest`, default `IsPackable=true`, default package metadata, README packing, and EventStore submodule root detection.
  - `Directory.Build.targets` centralizes .NET SDK container defaults for opt-in host projects.
  - `tests/Directory.Build.props` imports the root props, sets tests non-packable, and adds common test package references.
- Current scan found no inline `Version=` attributes on Tenants-owned `PackageReference` entries in `src/`, `tests/`, or `samples/`; re-verify during implementation because this story should make that invariant durable.
- Existing `.csproj.lscache`, `bin`, and `obj` files are present locally. Do not add or depend on generated/cache artifacts, and avoid turning this story into unrelated cleanup unless a governance test explicitly needs to ignore them.

### File-Specific Guidance for Likely Touches

- `Hexalith.Tenants/Directory.Packages.props`: add missing `<PackageVersion>` entries here only. Keep versions centralized and grouped. Do not use `VersionOverride` unless a story explicitly justifies a per-project exception.
- `Hexalith.Tenants/Directory.Build.props`: preserve shared language/build defaults and NuGet metadata. Be careful with `IsPackable=true` as the source default: it intentionally makes package projects packable while host/test projects opt out.
- `Hexalith.Tenants/Directory.Build.targets`: preserve the `.NET SDK container` opt-in model. Keep host container settings centralized here rather than adding Dockerfiles.
- `Hexalith.Tenants/tests/Directory.Build.props`: common test packages belong here when all test projects need them; project files should only carry project-specific package references or metadata updates.
- `Hexalith.Tenants/tests/Hexalith.Tenants.Contracts.Tests/SolutionStructureTests.cs`: likely place to add package governance assertions because Story 1.1 already uses it for repository structure guardrails. If the test grows too broad, split a new `PackageGovernanceTests.cs` in the same test project.
- Package projects expected packable: `src/Hexalith.Tenants.Contracts/Hexalith.Tenants.Contracts.csproj`, `src/Hexalith.Tenants.Client/Hexalith.Tenants.Client.csproj`, `src/Hexalith.Tenants.Server/Hexalith.Tenants.Server.csproj`, `src/Hexalith.Tenants.Testing/Hexalith.Tenants.Testing.csproj`, `src/Hexalith.Tenants.Aspire/Hexalith.Tenants.Aspire.csproj`.
- Projects expected non-packable: `src/Hexalith.Tenants/Hexalith.Tenants.csproj`, `src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj`, `src/Hexalith.Tenants.ServiceDefaults/Hexalith.Tenants.ServiceDefaults.csproj`, `tests/*`, and sample projects.

### Architecture and Technical Guardrails

- Use `Hexalith.Tenants.slnx` only; do not introduce a legacy `.sln`.
- Keep EventStore as a root-level submodule through `HexalithEventStoreRoot`; do not replace it with NuGet package references.
- Central package management means project `PackageReference` items carry package identity and metadata, while `Directory.Packages.props` carries versions. NuGet emits NU1008 when CPM is enabled and a `PackageReference` uses a `Version` attribute; turn that into a pre-build/test guard with a clearer project path if practical. [Source: Microsoft Learn NuGet Central Package Management] [Source: Microsoft Learn NU1008]
- Do not bump library versions as part of this story unless the implementation uncovers a build/security blocker. Version upgrades affect package governance and should be explicit.
- Do not add global XML documentation generation. Project context says Tenants does not set `GenerateDocumentationFile=true` globally; package metadata can still be valid without changing the warning surface.
- Do not add container infrastructure outside .NET SDK container publishing. Microsoft documents SDK container publish configuration through MSBuild properties such as container base image/repository; that matches the current `Directory.Build.targets` model. [Source: Microsoft Learn .NET SDK container publish configuration]

### Previous Story Intelligence

- Story 1.1 repaired `Hexalith.Tenants.slnx` and added repository guard tests. Reuse that pattern: focused, deterministic tests over XML/project files are preferred for governance invariants.
- Story 1.1 validation showed `dotnet restore Hexalith.Tenants.slnx` and `dotnet build Hexalith.Tenants.slnx --no-restore` pass with 0 warnings and 0 errors after repair.
- Full `dotnet test Hexalith.Tenants.slnx --no-build` may be blocked in this sandbox by VSTest TCP listener permissions. When that happens, run built test assemblies directly where possible and record the socket diagnostic instead of hiding it.
- Story 1.1 intentionally removed EventStore submodule projects from the Tenants solution membership and kept AppHost project-resource metadata explicit. Do not undo this while adding package governance.
- Story 1.1 file list included `Directory.Solution.props`, `Directory.Solution.targets`, and `MSBuild.rsp`; preserve their single-node/serial build defaults unless this story proves they are unnecessary.

### Testing Standards

- Use xUnit v3 and Shouldly for new or changed tests. Do not use `Assert.*`.
- Tests inherit global `using Xunit` from `tests/Directory.Build.props`; do not add per-file `using Xunit;`.
- Test methods use `snake_case_with_PascalCase_for_type_names`.
- Governance test candidates:
  - every Tenants-owned `PackageReference` has no `Version` or `VersionOverride`;
  - every `PackageReference Include="..."` has a matching `PackageVersion` unless it is SDK/framework-provided or intentionally metadata-only;
  - `Directory.Packages.props` enables CPM;
  - root build defaults set nullable, implicit usings, warnings-as-errors, and latest language version;
  - exactly five source package projects are packable and runtime/test/sample projects are not packable;
  - no Tenants-owned Dockerfile or compose file is introduced.
- Minimum command evidence:
  - `dotnet restore Hexalith.Tenants.slnx`
  - `dotnet build Hexalith.Tenants.slnx --no-restore`
  - focused governance tests or direct assembly execution if VSTest is sandbox-blocked
  - `dotnet pack` for the five package projects, or exact diagnostics if pack is blocked

### Out of Scope

- GitHub Actions CI gates, coverage thresholds, artifact upload, and package-count checks in CI; Story 1.3 owns that.
- Consumer package reference smoke tests; Story 1.4 owns that.
- Domain command/event/projection work, authorization behavior, query endpoints, or UI work.
- Updating submodule package governance files.
- Broad cleanup of generated `bin`, `obj`, `.lscache`, or historical BMAD artifacts unless needed to keep governance scans deterministic.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.2: Configure Central Build and Package Governance]
- [Source: _bmad-output/planning-artifacts/prd.md#NuGet Package Architecture]
- [Source: _bmad-output/planning-artifacts/prd.md#Solution & Project Structure]
- [Source: _bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries]
- [Source: _bmad-output/planning-artifacts/architecture.md#Development Workflow Integration]
- [Source: _bmad-output/project-context.md#Technology Stack & Versions]
- [Source: _bmad-output/project-context.md#Package Management]
- [Source: _bmad-output/project-context.md#Publishing]
- [Source: _bmad-output/implementation-artifacts/1-1-establish-eventstore-native-solution-structure.md#Previous Story Intelligence]
- [Source: Microsoft Learn Central Package Management](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management)
- [Source: Microsoft Learn NU1008](https://learn.microsoft.com/en-au/nuget/reference/errors-and-warnings/nu1008)
- [Source: Microsoft Learn MSBuild pack targets](https://learn.microsoft.com/en-us/nuget/reference/msbuild-targets)
- [Source: Microsoft Learn dotnet pack](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-pack)
- [Source: Microsoft Learn .NET SDK container publish configuration](https://learn.microsoft.com/en-us/dotnet/core/containers/publish-configuration)

## Project Structure Notes

- Alignment: the repository already follows the intended EventStore-native package topology, with root central package/build files and source/test project structure in place.
- Main implementation risk: accidentally validating or editing submodule package files. Governance scans must scope to Tenants-owned source, test, and sample projects unless they explicitly exclude submodules.
- Secondary risk: default `IsPackable=true` is intentional for package projects, so tests should evaluate effective expectations from known project groups, not simply fail every project that lacks an explicit `IsPackable` property.
- Container governance should stay MSBuild-based. `src/Hexalith.Tenants` is the host container opt-in; AppHost is publishable orchestration but not a NuGet package.

## Validation Checklist Results

- Story foundation extracted from Epic 1 and Story 1.2 acceptance criteria.
- Architecture guardrails extracted from PRD, architecture, project context, and Story 1.1 completion notes.
- Current repository files inspected for likely touches: root build/package files, all Tenants-owned project files, test shared props, and existing structure guard tests.
- Previous story intelligence incorporated from Story 1.1, including build evidence and sandbox test limitations.
- External technical research checked against Microsoft Learn for CPM, NU1008, MSBuild pack, `dotnet pack`, and SDK container publish configuration; no version bump was added to this story.
- Scope boundaries stated for Story 1.3 CI work, Story 1.4 consumer verification, submodules, and generated artifacts.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Red phase: `dotnet test tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --no-restore --filter PackageGovernanceTests -m:1 -nr:false` initially failed compilation in `PackageGovernanceTests.cs` with CS8604 nullable assertion warnings and CS1503 for `ShouldNotContain` overload usage; fixed test code and reran.
- Green phase: `dotnet test tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --no-restore --filter PackageGovernanceTests -m:1 -nr:false` compiled successfully, then VSTest aborted with `System.Net.Sockets.SocketException (13): Permission denied` in `Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.SocketServer.Start`.
- Focused governance validation passed through direct xUnit v3 runner: `dotnet tests/Hexalith.Tenants.Contracts.Tests/bin/Debug/net10.0/Hexalith.Tenants.Contracts.Tests.dll -class Hexalith.Tenants.Contracts.Tests.PackageGovernanceTests -parallel none -noLogo -noColor` => Total: 4, Errors: 0, Failed: 0, Skipped: 0.
- Regression validation passed for the built Contracts test assembly through direct xUnit v3 runner: `dotnet tests/Hexalith.Tenants.Contracts.Tests/bin/Debug/net10.0/Hexalith.Tenants.Contracts.Tests.dll -parallel none -noLogo -noColor` => Total: 51, Errors: 0, Failed: 0, Skipped: 0.
- Review validation: `rg -n '<(TargetFramework|Nullable|ImplicitUsings|TreatWarningsAsErrors|LangVersion)>' src tests samples -g '*.csproj'` returned no project-level central build overrides after review fixes.
- Review validation: `rg -n '<PackageReference[^>]*(Version=|VersionOverride=)|<VersionOverride>|<PackageReference[^>]*Include="[^"]+"[^>]*Version=' src tests samples Directory.Build.props Directory.Build.targets -g '*.csproj' -g '*.props' -g '*.targets'` returned no inline package versions or overrides after review fixes.
- Review rerun of `dotnet test tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --no-restore --filter PackageGovernanceTests -m:1 -nr:false` remained blocked by restricted NuGet access: repeated `NU1801` for `https://api.nuget.org/v3/index.json` and `NU1101` missing-package diagnostics for locally unavailable packages.
- Required `dotnet restore Hexalith.Tenants.slnx` was blocked by restricted network access to NuGet: `NU1301: Unable to load the service index for source https://api.nuget.org/v3/index.json` with `Permission denied (api.nuget.org:443)`. Retrying with `--ignore-failed-sources` converted feed access warnings into errors because `TreatWarningsAsErrors=true` and also reported missing packages not available locally, including `Aspire.Hosting`, `CommunityToolkit.Aspire.Hosting.Dapr`, `Dapr.*`, `MediatR`, `Shouldly`, and `Hexalith.Commons.UniqueIds`.
- Required `dotnet build Hexalith.Tenants.slnx --no-restore` was blocked after the failed restore state by NuGet/Aspire SDK resolution: `MSB4236: The SDK 'Aspire.AppHost.Sdk/13.3.3' specified could not be found` and `NU1301: Permission denied (api.nuget.org:443)`.
- Required `dotnet pack` for the five publishable package projects was attempted with `--configuration Release --no-build --no-restore`; each project was blocked by NuGet feed/package availability, for example `NU1801: Warning As Error: Unable to load the service index for source https://api.nuget.org/v3/index.json` and `NU1101: Unable to find package ... No packages exist with this id in source(s): nuget.org`.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Added focused package/build governance tests for central package management, inline version drift, metadata-only `PackageReference Update` entries, shared build defaults, EventStore root detection, packability boundaries, NuGet metadata, and SDK container publishing defaults.
- Senior review tightened package governance coverage to include shared `Directory.Build.*` files that can add project package references, not only `.csproj` files.
- Senior review removed the redundant host-level `LangVersion` override so language version governance comes only from root `Directory.Build.props`.
- Confirmed Tenants-owned `src/`, `tests/`, and `samples/` projects have no inline `PackageReference Version` or `VersionOverride` entries, and no Dockerfile/compose publish convention was introduced.
- Preserved existing central package/build/container configuration; no package versions, shared metadata, submodule references, or container defaults were changed.
- Validation evidence is mixed due sandbox restrictions: direct xUnit governance/regression tests pass, while solution restore/build and package creation are blocked by NuGet network access and VSTest socket restrictions. Blocked commands are recorded above and are not reported as passed.

### File List

- Hexalith.Tenants/src/Hexalith.Tenants/Hexalith.Tenants.csproj
- Hexalith.Tenants/tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs
- _bmad-output/implementation-artifacts/1-2-configure-central-build-and-package-governance.md
- _bmad-output/implementation-artifacts/sprint-status.yaml

### Change Log

- 2026-05-31: Added package/build/container governance tests and recorded sandbox-blocked restore/build/pack diagnostics.
- 2026-05-31: Senior review auto-fixed governance test coverage for shared build files and removed redundant host `LangVersion` override.

## Senior Developer Review (AI)

### Review Summary

- Outcome: Approved after auto-fixes.
- Critical issues remaining: 0.
- High/medium issues fixed: 2.
- Git/story discrepancy: source repo shows `tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs` as untracked and `src/Hexalith.Tenants/Hexalith.Tenants.csproj` as modified after review fixes; parent BMAD artifacts are outside the source repository status surface.

### Findings Fixed

- Medium: Package governance validation scanned Tenants-owned `.csproj` files but did not scan shared MSBuild files such as `tests/Directory.Build.props`, even though those files can add `PackageReference` items to projects. Fixed by including owned `Directory.Build.*` files and root build props/targets in package-reference governance scanning, with offending XML nodes in missing-version diagnostics.
- Medium: `src/Hexalith.Tenants/Hexalith.Tenants.csproj` duplicated `<LangVersion>latest</LangVersion>`, weakening the central-build-governance rule that language settings come from root `Directory.Build.props`. Fixed by removing the project-level override and tightening the governance test to fail on any project-level central build property override.

### Review Validation Checklist

- Story file loaded and status verified as reviewable.
- Acceptance criteria and completed tasks cross-checked against central package/build/container files and the governance test implementation.
- File List reviewed and updated for the review-time `Hexalith.Tenants.csproj` change.
- Security review: no credential, injection, or authorization surface introduced; changes are local MSBuild XML and deterministic test code.
- Test quality review: governance tests use xUnit v3/Shouldly and assert concrete XML/build invariants.
- Validation performed: static package/build override searches passed; `dotnet test --no-restore --filter PackageGovernanceTests` remained blocked by NuGet feed access and missing local packages as recorded above.
