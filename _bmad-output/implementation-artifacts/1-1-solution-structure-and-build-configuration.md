# Story 1.1: Solution Structure & Build Configuration

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer,
I want to clone the Hexalith.Tenants repository and have a fully buildable solution with all project shells and correct dependency chains,
so that I can begin implementing domain logic on a proven, consistent project structure mirroring EventStore conventions.

## Acceptance Criteria

1. **Given** the repository is cloned with the EventStore submodule initialized **When** the developer opens the solution **Then** `Hexalith.Tenants.slnx` contains all 15 projects (8 src, 5 test, 2 sample)

2. **Given** the solution structure exists **When** `dotnet build` is executed **Then** all projects compile successfully with zero errors and warnings-as-errors enabled

3. **Given** the solution structure exists **When** `dotnet test` is executed **Then** the test runner discovers all 6 test projects (5 under tests/ + Sample.Tests under samples/), executes smoke tests in each project, and reports zero failures

4. **Given** the solution is built **When** a developer inspects `global.json` **Then** it specifies SDK version 10.0.103 with `rollForward: latestPatch`

5. **Given** the solution is built **When** a developer inspects `Directory.Build.props` **Then** it contains shared project properties including NuGet metadata, nullable references enabled, implicit usings enabled, and warnings as errors

6. **Given** the solution is built **When** a developer inspects `Directory.Packages.props` **Then** it contains centralized NuGet package versions for all dependencies (EventStore, DAPR SDK, Aspire, xUnit, Shouldly, NSubstitute, coverlet, FluentValidation, MediatR, MinVer)

7. **Given** the solution is built **When** a developer inspects `.editorconfig` **Then** it enforces EventStore conventions (file-scoped namespaces, Allman braces, `_camelCase` private fields, 4-space indentation)

8. **Given** the solution is built **When** a developer inspects project dependencies **Then** Contracts depends on EventStore.Contracts; Client depends on Contracts; Server depends on Contracts and EventStore.Server; Testing depends on Server and Contracts; Hexalith.Tenants depends on Server, Contracts, and ServiceDefaults; Aspire has NO project references (only NuGet: Aspire.Hosting, CommunityToolkit.Aspire.Hosting.Dapr — matches EventStore's actual pattern, not the architecture prose); test projects reference their corresponding src projects plus xUnit, Shouldly, NSubstitute, and coverlet

## Tasks / Subtasks

- [x] Task 0: Verify prerequisites (AC: all)
  - [x] 0.1: Verify EventStore submodule is initialized — run `git submodule update --init --recursive` and confirm `Hexalith.EventStore/src/` contains project directories
  - [x] 0.2: Verify .NET SDK version available — run `dotnet --version`. Architecture specifies 10.0.103; if unavailable, use latest 10.0.x patch and document deviation in Dev Agent Record.
- [x] Task 1: Create root build configuration files (AC: #4, #5, #6, #7)
  - [x] 1.1: Create `global.json` with SDK 10.0.103 and `rollForward: latestPatch`. If SDK 10.0.103 is not available (verified in Task 0.2), use latest available 10.0.x and document in Dev Agent Record.
  - [x] 1.2: Create `Directory.Build.props` mirroring EventStore's pattern (TargetFramework net10.0, Nullable enable, ImplicitUsings enable, TreatWarningsAsErrors true, NuGet metadata for Hexalith.Tenants, MinVer configuration)
  - [x] 1.3: Create `Directory.Packages.props` with centralized package versions — copy ALL packages from EventStore's `Directory.Packages.props` matching versions exactly (including Aspire.Hosting.*, Testcontainers, etc.). Tenants inherits the full ecosystem; unused packages cause no harm in centralized management and will be needed in later stories.
  - [x] 1.4: Create `.editorconfig` matching EventStore's conventions exactly (copy from EventStore)
- [x] Task 2: Create solution file and source project shells (AC: #1, #8)
  - [x] 2.1: Create `Hexalith.Tenants.slnx` with all 15 projects organized in /src/, /tests/, /samples/ folders
  - [x] 2.2: Create `src/Hexalith.Tenants.Contracts/Hexalith.Tenants.Contracts.csproj` — empty shell, depends on EventStore.Contracts via ProjectReference
  - [x] 2.3: Create `src/Hexalith.Tenants.Client/Hexalith.Tenants.Client.csproj` — depends on Tenants.Contracts, Dapr.Client, Microsoft.Extensions packages. Add `<InternalsVisibleTo Include="Hexalith.Tenants.Client.Tests" />`
  - [x] 2.4: Create `src/Hexalith.Tenants.Server/Hexalith.Tenants.Server.csproj` — depends on Tenants.Contracts, EventStore.Server via ProjectReference, Dapr packages, MediatR
  - [x] 2.5: Create `src/Hexalith.Tenants/Hexalith.Tenants.csproj` — Web SDK, IsPackable=false, IsPublishable=true, depends on Server, Contracts, ServiceDefaults, Dapr.AspNetCore, MediatR, FluentValidation, JWT, OpenApi. Add `<InternalsVisibleTo Include="Hexalith.Tenants.Server.Tests" />`
  - [x] 2.6: Create `src/Hexalith.Tenants.Aspire/Hexalith.Tenants.Aspire.csproj` — depends on Aspire.Hosting, CommunityToolkit.Aspire.Hosting.Dapr
  - [x] 2.7: Create `src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj` — Aspire.AppHost.Sdk, OutputType Exe, IsPackable=false, IsPublishable=true, depends on Hexalith.Tenants, Sample, Aspire (IsAspireProjectResource=false)
  - [x] 2.8: Create `src/Hexalith.Tenants.ServiceDefaults/Hexalith.Tenants.ServiceDefaults.csproj` — IsPackable=false, IsAspireSharedProject=true, FrameworkReference Microsoft.AspNetCore.App, OpenTelemetry packages
  - [x] 2.9: Create `src/Hexalith.Tenants.Testing/Hexalith.Tenants.Testing.csproj` — depends on Tenants.Server, Tenants.Contracts, Shouldly, NSubstitute, xunit.assert
- [x] Task 3: Create test project shells (AC: #1, #3, #8)
  - [x] 3.1: Create `tests/Directory.Build.props` — imports root props, sets IsPackable=false, IsPublishable=false, IsTestProject=true
  - [x] 3.2: Create `tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj` — depends on Tenants.Contracts, Tenants.Testing, xUnit packages, coverlet. Include `<Using Include="Xunit" />` global using.
  - [x] 3.3: Create `tests/Hexalith.Tenants.Client.Tests/Hexalith.Tenants.Client.Tests.csproj` — depends on Tenants.Client, Tenants.Testing, xUnit packages, coverlet. Include `<Using Include="Xunit" />` global using.
  - [x] 3.4: Create `tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj` — depends on Hexalith.Tenants, Tenants.Server, Tenants.Testing, Sample (intentional: tests sample domain service registration patterns); FrameworkReference Microsoft.AspNetCore.App; xUnit, Shouldly, NSubstitute, YamlDotNet, coverlet. Include `<Using Include="Xunit" />`.
  - [x] 3.5: Create `tests/Hexalith.Tenants.Testing.Tests/Hexalith.Tenants.Testing.Tests.csproj` — depends on Tenants.Testing, Tenants.Contracts, xUnit packages, coverlet. Include `<Using Include="Xunit" />` global using.
  - [x] 3.6: Create `tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj` — depends on Hexalith.Tenants, Aspire.Hosting.Testing, xUnit packages, coverlet. Include `<Using Include="Xunit" />` global using.
- [x] Task 4: Create sample project shells (AC: #1, #8)
  - [x] 4.1: Create `samples/Hexalith.Tenants.Sample/Hexalith.Tenants.Sample.csproj` — Web SDK, IsPackable=false, depends on Tenants.Client, Tenants.Contracts
  - [x] 4.2: Create `samples/Hexalith.Tenants.Sample.Tests/Hexalith.Tenants.Sample.Tests.csproj` — PHYSICALLY under `samples/` but listed under `/samples/` folder in `.slnx`. Must explicitly set `IsTestProject=true`, `IsPackable=false` (does NOT inherit from `tests/Directory.Build.props`). Depends on Sample, Tenants.Testing, xUnit packages, coverlet. Include `<Using Include="Xunit" />`.
- [x] Task 5: Add minimal source files for compilation (AC: #2)
  - [x] 5.1: Library SDK projects (Contracts, Client, Server, Testing, Aspire) compile empty — NO placeholder needed. Only Web SDK and Exe projects need entry points.
  - [x] 5.2: Add `Program.cs` stub for Hexalith.Tenants (minimal ASP.NET host: `var builder = WebApplication.CreateBuilder(args); var app = builder.Build(); app.Run();`)
  - [x] 5.3: Add `Program.cs` stub for AppHost (initial minimal Aspire host; later expanded in Story 1.2 with DAPR component wiring)
  - [x] 5.4: Add `Extensions.cs` stub for ServiceDefaults (initial placeholder extension method; later expanded in Story 1.2 with OpenTelemetry and health endpoint setup)
  - [x] 5.5: Add `Program.cs` stub for Sample (minimal web app: `var builder = WebApplication.CreateBuilder(args); var app = builder.Build(); app.Run();`)
- [x] Task 6: Verify build and test (AC: #2, #3)
  - [x] 6.1: Run `dotnet restore Hexalith.Tenants.slnx` — verify all packages resolve
  - [x] 6.2: Run `dotnet build Hexalith.Tenants.slnx --configuration Release` — verify zero errors
  - [x] 6.3: Run `dotnet test Hexalith.Tenants.slnx` — verify 6 test projects discovered, one smoke test per project executed, and zero failures

## Dev Notes

### Architecture Requirements

- **Mirror EventStore structure exactly** — the PRD and architecture specify that Hexalith.Tenants follows "the same project structure, conventions, and documentation approach as Hexalith.EventStore." The EventStore submodule at `Hexalith.EventStore/` IS the reference.
- **Modern XML solution format** (`Hexalith.Tenants.slnx`) — NOT the classic `.sln` format.
- **15 projects total**: 8 src + 5 test + 2 sample (see complete directory structure below).
- **.NET 10 SDK pinned** at version 10.0.103 (architecture specifies this; EventStore uses 10.0.102 — Tenants explicitly uses 10.0.103).
- **NuGet package versions** must match EventStore's `Directory.Packages.props` exactly — centralized package management via `ManagePackageVersionsCentrally`.
- **MinVer versioning** with `v` tag prefix for git tag-based SemVer.

### Project Dependency Graph

```
Hexalith.EventStore.Contracts <── Hexalith.Tenants.Contracts
                                     ├── Hexalith.Tenants.Client
                                     │     └── (Dapr.Client, Microsoft.Extensions)
                                     ├── Hexalith.Tenants.Server
Hexalith.EventStore.Server <────────┘     └── (Dapr, MediatR)
                                     ├── Hexalith.Tenants.Testing
                                     │     └── (Shouldly, NSubstitute, xunit.assert)
                                     ├── Hexalith.Tenants [Web SDK]
                                     │     └── (Server, ServiceDefaults, Dapr, MediatR, FluentValidation, JWT, OpenApi)
                                     ├── Hexalith.Tenants.Aspire
                                     │     └── (Aspire.Hosting, CommunityToolkit.Aspire.Hosting.Dapr)
                                     ├── Hexalith.Tenants.AppHost [Aspire.AppHost.Sdk]
                                     │     └── (Hexalith.Tenants, Sample, Aspire)
                                     └── Hexalith.Tenants.ServiceDefaults
                                           └── (OpenTelemetry, Resilience, ServiceDiscovery)
```

### SDK Patterns from EventStore Reference

**Directory.Build.props pattern:**
- `TargetFramework`: `net10.0`
- `Nullable`: `enable`
- `ImplicitUsings`: `enable`
- `TreatWarningsAsErrors`: `true`
- `IsPackable`: `true` (default, overridden by host/test projects)
- `IsPublishable`: `false` (default, overridden by deployable projects)
- NuGet metadata: Authors, Company, PackageLicenseExpression (MIT), URLs pointing to Hexalith/Hexalith.Tenants
- MinVer: `MinVerTagPrefix` = `v`, `MinVerDefaultPreReleaseIdentifiers` = `preview.0`
- README pack item for packable projects

**Test Directory.Build.props:**
- Imports root props via `<Import Project="$([MSBuild]::GetPathOfFileAbove(...))" />`
- Sets `IsPackable=false`, `IsPublishable=false`, `IsTestProject=true`

**Hexalith.Tenants pattern:** Uses `Microsoft.NET.Sdk.Web`, `IsPackable=false`, `IsPublishable=true`

**AppHost pattern:** Uses `Aspire.AppHost.Sdk/13.1.2`, `OutputType=Exe`, `IsPackable=false`, `IsPublishable=true`, `UserSecretsId`

**ServiceDefaults pattern:** Uses `Microsoft.NET.Sdk`, `IsPackable=false`, `IsAspireSharedProject=true`, `FrameworkReference Microsoft.AspNetCore.App`

**Testing (NuGet) pattern:** Uses `Microsoft.NET.Sdk`, packable (default from root), references Server + Contracts + test assertion libraries

### .editorconfig Conventions

Copy directly from `Hexalith.EventStore/.editorconfig`:
- File-scoped namespaces (`csharp_style_namespace_declarations = file_scoped:warning`)
- Allman braces (`csharp_new_line_before_open_brace = all:warning`)
- `_camelCase` private fields
- `I` prefix for interfaces
- `Async` suffix for async methods
- 4-space indentation, CRLF line endings, UTF-8
- Analyzer severities set to warning for scaffolding phase

### Complete Target Directory Structure

```
Hexalith.Tenants/
├── .editorconfig
├── global.json
├── Directory.Build.props
├── Directory.Packages.props
├── Hexalith.Tenants.slnx
├── Hexalith.EventStore/                   # Git submodule (already exists)
├── src/
│   ├── Hexalith.Tenants.Contracts/
│   │   └── Hexalith.Tenants.Contracts.csproj
│   ├── Hexalith.Tenants.Client/
│   │   └── Hexalith.Tenants.Client.csproj
│   ├── Hexalith.Tenants.Server/
│   │   └── Hexalith.Tenants.Server.csproj
│   ├── Hexalith.Tenants/
│   │   ├── Hexalith.Tenants.csproj
│   │   └── Program.cs
│   ├── Hexalith.Tenants.Aspire/
│   │   └── Hexalith.Tenants.Aspire.csproj
│   ├── Hexalith.Tenants.AppHost/
│   │   ├── Hexalith.Tenants.AppHost.csproj
│   │   └── Program.cs
│   ├── Hexalith.Tenants.ServiceDefaults/
│   │   ├── Hexalith.Tenants.ServiceDefaults.csproj
│   │   └── Extensions.cs
│   └── Hexalith.Tenants.Testing/
│       └── Hexalith.Tenants.Testing.csproj
├── tests/
│   ├── Directory.Build.props
│   ├── Hexalith.Tenants.Contracts.Tests/
│   │   └── Hexalith.Tenants.Contracts.Tests.csproj
│   ├── Hexalith.Tenants.Client.Tests/
│   │   └── Hexalith.Tenants.Client.Tests.csproj
│   ├── Hexalith.Tenants.Server.Tests/
│   │   └── Hexalith.Tenants.Server.Tests.csproj
│   ├── Hexalith.Tenants.Testing.Tests/
│   │   └── Hexalith.Tenants.Testing.Tests.csproj
│   └── Hexalith.Tenants.IntegrationTests/
│       └── Hexalith.Tenants.IntegrationTests.csproj
└── samples/
    ├── Hexalith.Tenants.Sample/
    │   ├── Hexalith.Tenants.Sample.csproj
    │   └── Program.cs
    └── Hexalith.Tenants.Sample.Tests/
        └── Hexalith.Tenants.Sample.Tests.csproj
```

### Critical Implementation Guards

- **DO NOT** use the classic `.sln` format — only `.slnx` (modern XML solution format)
- **DO NOT** add domain logic files (commands, events, aggregates) — this story is ONLY scaffolding
- **DO NOT** create `dapr/components/` directory yet — that's Story 1.2
- **DO NOT** create CI/CD workflow files yet — that's Story 1.3
- **DO** reference EventStore projects via `ProjectReference` with relative paths through the submodule (e.g., `..\..\Hexalith.EventStore\src\Hexalith.EventStore.Contracts\Hexalith.EventStore.Contracts.csproj`)
- **DO** ensure every src project without source files has at minimum an empty placeholder to avoid build warnings
- **DO** match EventStore's NuGet package versions exactly from its `Directory.Packages.props`
- **DO** set `InternalsVisibleTo` attributes matching the same pattern as EventStore (Hexalith.Tenants → Server.Tests, Client → Client.Tests)

### NuGet Package Versions (from EventStore Directory.Packages.props)

| Category | Package | Version |
|----------|---------|---------|
| Build | MinVer | 7.0.0 |
| DAPR | Dapr.Client | 1.16.1 |
| DAPR | Dapr.AspNetCore | 1.16.1 |
| DAPR | Dapr.Actors | 1.16.1 |
| DAPR | Dapr.Actors.AspNetCore | 1.16.1 |
| Aspire | Aspire.Hosting | 13.1.2 |
| Aspire | Aspire.Hosting.Testing | 13.1.1 |
| Aspire | CommunityToolkit.Aspire.Hosting.Dapr | 13.0.0 |
| Aspire ServiceDefaults | Microsoft.Extensions.Http.Resilience | 10.3.0 |
| Aspire ServiceDefaults | Microsoft.Extensions.ServiceDiscovery | 10.3.0 |
| Aspire ServiceDefaults | OpenTelemetry.Exporter.OpenTelemetryProtocol | 1.15.0 |
| Aspire ServiceDefaults | OpenTelemetry.Extensions.Hosting | 1.15.0 |
| Aspire ServiceDefaults | OpenTelemetry.Instrumentation.AspNetCore | 1.15.0 |
| Aspire ServiceDefaults | OpenTelemetry.Instrumentation.Http | 1.15.0 |
| Aspire ServiceDefaults | OpenTelemetry.Instrumentation.Runtime | 1.15.0 |
| Microsoft.Extensions | Microsoft.Extensions.Configuration.Binder | 10.0.0 |
| Microsoft.Extensions | Microsoft.Extensions.Hosting.Abstractions | 10.0.0 |
| Microsoft.Extensions | Microsoft.Extensions.Hosting | 10.0.0 |
| Application | MediatR | 14.0.0 |
| Application | Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.0 |
| Application | Microsoft.AspNetCore.OpenApi | 10.0.3 |
| Application | Swashbuckle.AspNetCore.SwaggerUI | 10.1.2 |
| Application | FluentValidation | 12.1.1 |
| Application | FluentValidation.DependencyInjectionExtensions | 12.1.1 |
| Testing | coverlet.collector | 6.0.4 |
| Testing | Microsoft.AspNetCore.Mvc.Testing | 10.0.0 |
| Testing | Microsoft.NET.Test.Sdk | 18.0.1 |
| Testing | xunit | 2.9.3 |
| Testing | xunit.assert | 2.9.3 |
| Testing | xunit.runner.visualstudio | 3.1.5 |
| Testing | Shouldly | 4.3.0 |
| Testing | NSubstitute | 5.3.0 |
| Testing | Testcontainers | 4.10.0 |
| Testing | YamlDotNet | 16.3.0 |

### Project Structure Notes

- Solution mirrors EventStore's structure exactly, replacing `EventStore` with `Tenants` in all names
- EventStore submodule is at `Hexalith.EventStore/` — project references use relative paths through this submodule
- No `nuget.config` needed initially (EventStore has one, but Tenants can inherit default NuGet feeds)
- The `samples/` folder follows EventStore's pattern with `Hexalith.EventStore.Sample.Tests` placed under `tests/` in the solution file but physically under `samples/`

### References

- [Source: _bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries] — Complete directory structure, dependency graph, type location rules
- [Source: _bmad-output/planning-artifacts/architecture.md#Starter Template Evaluation] — Rationale for mirroring EventStore structure
- [Source: _bmad-output/planning-artifacts/architecture.md#Implementation Patterns & Consistency Rules] — Naming conventions, code style rules
- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.1] — Acceptance criteria and story definition
- [Source: Hexalith.EventStore/Directory.Build.props] — Root build properties reference
- [Source: Hexalith.EventStore/Directory.Packages.props] — Package version reference
- [Source: Hexalith.EventStore/Hexalith.EventStore.slnx] — Solution file format reference
- [Source: Hexalith.EventStore/.editorconfig] — Code style conventions reference
- [Source: Hexalith.EventStore/tests/Directory.Build.props] — Test project build properties reference

## Change Log

- 2026-03-08: Implemented full solution scaffolding — 15 projects, root build configs, all build and test verification passed.
- 2026-03-08: Senior developer review fixes applied — added smoke tests to all 6 test projects, corrected Story 1.1 verification wording, and documented that AppHost/ServiceDefaults were later expanded by Story 1.2.

## Dev Agent Record

### Senior Developer Review (AI)

- **Review Outcome**: Approved after fixes.
- **Reviewer Model**: GPT-5.4.
- **Issues Fixed**:
  - Added one smoke test to each of the 6 test projects so `dotnet test` executes real tests instead of emitting `No test is available` warnings.
  - Corrected Story 1.1 verification language to reflect 6 discovered test projects with smoke tests and zero failures.
  - Documented that `src/Hexalith.Tenants.AppHost/Program.cs` and `src/Hexalith.Tenants.ServiceDefaults/Extensions.cs` were later expanded by Story 1.2 and no longer match the original minimal stub wording.
  - Reconciled the Story 1.1 planning text with the implemented solution by correcting the epic-level acceptance criterion to 6 test projects.
- **Story Status**: Confirmed as `done` after review fixes.
- **Sprint Status**: Confirmed as `done`.

### Agent Model Used

Claude Opus 4.6 (claude-opus-4-6)

### Debug Log References

- ServiceDefaults Extensions.cs initially failed to compile — `IHostApplicationBuilder` required explicit `using Microsoft.Extensions.Hosting;` since plain SDK projects don't include it in implicit usings (Web SDK does). Fixed by adding the using directive.

### Completion Notes List

- Task 0: EventStore submodule verified (8 src project directories present). SDK 10.0.103 confirmed available (also 10.0.102, 10.0.200-preview installed).
- Task 1: Created global.json (SDK 10.0.103), Directory.Build.props (mirroring EventStore with Tenants URLs/metadata), Directory.Packages.props (exact copy of EventStore's centralized versions), .editorconfig (exact copy of EventStore conventions).
- Task 2: Created Hexalith.Tenants.slnx with 15 projects in /src/, /tests/, /samples/ folders. All 8 src .csproj files created with correct dependency chains including cross-submodule ProjectReferences to EventStore.Contracts and EventStore.Server.
- Task 3: Created tests/Directory.Build.props importing root props. All 5 test .csproj files with correct project references, xUnit, coverlet, and global `<Using Include="Xunit" />`.
- Added one smoke test class per test project to prove test discovery and eliminate `No test is available` warnings during solution-level test runs.
- Task 4: Created Sample and Sample.Tests under samples/. Sample.Tests explicitly sets IsTestProject=true, IsPackable=false since it doesn't inherit tests/Directory.Build.props.
- Task 5: Added minimal Program.cs stubs for Hexalith.Tenants and Sample. Story 1.2 later expanded AppHost and ServiceDefaults beyond the original scaffolding placeholders while preserving Story 1.1 buildability goals.
- Task 6: `dotnet restore` — all 17 projects restored (15 Tenants + 2 EventStore). `dotnet build --configuration Release` — zero errors, zero warnings. `dotnet test` — 6 test projects discovered, 6 smoke tests executed, zero failures, zero warnings.

### File List

- global.json (new)
- Directory.Build.props (new)
- Directory.Packages.props (new)
- .editorconfig (new)
- Hexalith.Tenants.slnx (new)
- src/Hexalith.Tenants.Contracts/Hexalith.Tenants.Contracts.csproj (new)
- src/Hexalith.Tenants.Client/Hexalith.Tenants.Client.csproj (new)
- src/Hexalith.Tenants.Server/Hexalith.Tenants.Server.csproj (new)
- src/Hexalith.Tenants/Hexalith.Tenants.csproj (new)
- src/Hexalith.Tenants/Program.cs (new)
- src/Hexalith.Tenants.Aspire/Hexalith.Tenants.Aspire.csproj (new)
- src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj (new)
- src/Hexalith.Tenants.AppHost/Program.cs (new)
- src/Hexalith.Tenants.ServiceDefaults/Hexalith.Tenants.ServiceDefaults.csproj (new)
- src/Hexalith.Tenants.ServiceDefaults/Extensions.cs (new)
- src/Hexalith.Tenants.Testing/Hexalith.Tenants.Testing.csproj (new)
- tests/Directory.Build.props (new)
- tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj (new)
- tests/Hexalith.Tenants.Contracts.Tests/ScaffoldingSmokeTests.cs (new)
- tests/Hexalith.Tenants.Client.Tests/Hexalith.Tenants.Client.Tests.csproj (new)
- tests/Hexalith.Tenants.Client.Tests/ScaffoldingSmokeTests.cs (new)
- tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj (new)
- tests/Hexalith.Tenants.Server.Tests/ScaffoldingSmokeTests.cs (new)
- tests/Hexalith.Tenants.Testing.Tests/Hexalith.Tenants.Testing.Tests.csproj (new)
- tests/Hexalith.Tenants.Testing.Tests/ScaffoldingSmokeTests.cs (new)
- tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj (new)
- tests/Hexalith.Tenants.IntegrationTests/ScaffoldingSmokeTests.cs (new)
- samples/Hexalith.Tenants.Sample/Hexalith.Tenants.Sample.csproj (new)
- samples/Hexalith.Tenants.Sample/Program.cs (new)
- samples/Hexalith.Tenants.Sample.Tests/Hexalith.Tenants.Sample.Tests.csproj (new)
- samples/Hexalith.Tenants.Sample.Tests/ScaffoldingSmokeTests.cs (new)
