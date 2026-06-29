---
title: 'Debug project references and Release package references for Hexalith libraries'
type: 'chore'
created: '2026-06-29'
status: 'done'
baseline_commit: 'cda11955a446a9699c89484d20ad0e52b075f57c'
context:
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-29-debug-release-hexalith-references.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Hexalith dependency selection is currently based on whether submodule source is present, so Release builds still use `ProjectReference` when submodules are checked out. That defeats package-only release validation and hides NuGet packaging or feed problems until later.

**Approach:** Make Debug builds use source `ProjectReference` for package-capable Hexalith libraries when source exists, and make Release builds select `PackageReference`. Document and preserve narrow exceptions for source-only host/application projects and packages that are not published yet.

## Boundaries & Constraints

**Always:** Keep all package versions in `Directory.Packages.props`. Use existing `$(Hexalith*Root)` path detection; do not hardcode dependency paths. Preserve AppHost Debug build-forcing references for launched child applications. Leave unrelated sprint-status and retro files untouched.

**Ask First:** Halt before modifying submodule files, adding a root `NuGet.config`, changing package versions away from the approved EventStore `3.19.0` / Memories `1.31.1` pins, or refactoring the Tenants host away from the EventStore web-host dependency.

**Never:** Do not initialize submodules, use recursive submodule commands, add package versions to `.csproj` files, remove source-only application references without a published package, or create `.sln` files.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Debug with dependency source present | `Configuration=Debug`; EventStore/Memories submodule projects exist | Package-capable Hexalith refs select `ProjectReference` | If source is absent, existing package fallback remains available |
| Release with dependency source present | `Configuration=Release`; submodule projects exist | Package-capable Hexalith refs select `PackageReference` | Release restore may fail on upstream package-feed gaps; document the exact missing package |
| Source-only dependency | EventStore web host or unpublished FrontComposer packages | Keep `ProjectReference` and comments explaining exception | Do not fabricate package fallback |
| Published package-capable dependency currently source-only | EventStore Client/Server/DomainService source refs | Add central package pin if needed and dual ProjectReference/PackageReference conditions | No version attributes in `.csproj` |

</frozen-after-approval>

## Code Map

- `Directory.Build.props` -- defines Hexalith roots and `HexalithEventStoreFromSource` / `HexalithMemoriesFromSource` selectors.
- `Directory.Packages.props` -- central package versions and missing EventStore Client/Server pins.
- `src/Hexalith.Tenants.Client/Hexalith.Tenants.Client.csproj` -- source-only EventStore Client reference to dual-wire.
- `src/Hexalith.Tenants.Server/Hexalith.Tenants.Server.csproj` -- source-only EventStore Server reference to dual-wire.
- `samples/Hexalith.Tenants.Sample/Hexalith.Tenants.Sample.csproj` -- source-only EventStore DomainService reference to dual-wire.
- `src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj` and `tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj` -- FrontComposer source-only exceptions.
- `src/Hexalith.Tenants/Hexalith.Tenants.csproj` -- EventStore web-host source-only exception plus DomainService dual reference.

## Tasks & Acceptance

**Execution:**
- [x] `Directory.Build.props` -- make `HexalithEventStoreFromSource` and `HexalithMemoriesFromSource` true only for Debug builds with source present -- enforces Debug-source / Release-package policy.
- [x] `Directory.Packages.props` -- add central versions for `Hexalith.EventStore.Client` and `Hexalith.EventStore.Server` -- enables Release package refs.
- [x] `src/Hexalith.Tenants.Client/Hexalith.Tenants.Client.csproj` -- convert EventStore Client to conditional ProjectReference/PackageReference -- aligns client library with policy.
- [x] `src/Hexalith.Tenants.Server/Hexalith.Tenants.Server.csproj` -- convert EventStore Server to conditional ProjectReference/PackageReference -- aligns server package with policy.
- [x] `samples/Hexalith.Tenants.Sample/Hexalith.Tenants.Sample.csproj` -- convert EventStore DomainService to conditional ProjectReference/PackageReference -- aligns sample app with policy where package exists.
- [x] Relevant comments -- update wording from "source when submodule is checked out" to "Debug source, Release package" and document source-only exceptions -- prevents stale guidance.

**Acceptance Criteria:**
- Given Debug configuration and available Hexalith source, when project files evaluate, then package-capable Hexalith dependencies select project references.
- Given Release configuration and available Hexalith source, when project files evaluate, then package-capable Hexalith dependencies select package references.
- Given EventStore web host or FrontComposer dependencies, when evaluating references, then they remain documented source-only exceptions until upstream packages exist.
- Given Release restore fails because a transitive Hexalith package is unavailable from the configured feed, when reporting verification, then the missing upstream package is named without treating it as a Tenants implementation defect.

## Spec Change Log

## Design Notes

The selector should remain path-presence guarded so Debug only opts into source when the dependency project actually exists:

```xml
<HexalithEventStoreFromSource
  Condition="'$(Configuration)' == 'Debug' and Exists('$(HexalithEventStoreRoot)\src\Hexalith.EventStore.Contracts\Hexalith.EventStore.Contracts.csproj')">true</HexalithEventStoreFromSource>
```

## Verification

**Commands:**
- `dotnet build Hexalith.Tenants.slnx -c Debug` -- expected: succeeds with package-capable Hexalith source refs selected where source exists.
- `dotnet build Hexalith.Tenants.slnx -c Release -warnaserror` -- expected: selects package refs; may stop on documented upstream feed gap for EventStore/Commons 3.x.
- `dotnet msbuild <project> -c Release -t:Restore -v:minimal` on edited package-capable projects as needed -- expected: no accidental source refs in Release item selection.

**Results:**
- `dotnet build Hexalith.Tenants.slnx -c Debug` -- passed, 0 warnings / 0 errors; source projects were built for EventStore Client, Server, DomainService, and Memories.
- `dotnet msbuild Directory.Build.props -p:Configuration=Debug -getProperty:HexalithEventStoreFromSource -getProperty:HexalithMemoriesFromSource` -- both properties evaluated to `true`.
- `dotnet msbuild Directory.Build.props -p:Configuration=Release -getProperty:HexalithEventStoreFromSource -getProperty:HexalithMemoriesFromSource` -- both properties evaluated empty.
- Release item probes for Tenants Client, Server, Contracts, UI, Sample, Tenants host, Tenants.Aspire, AppHost, IntegrationTests, and UI.Tests showed package-capable Hexalith dependencies selecting `PackageReference`; EventStore web host and FrontComposer remained documented source-only exceptions.
- `dotnet build Hexalith.Tenants.slnx -c Release -warnaserror` -- restore failed on upstream package/feed gaps: `Hexalith.Commons.UniqueIds >= 3.19.0`, `Hexalith.Commons.Aspire`, and `Hexalith.Commons.ServiceDefaults` are not available from the configured `nuget.org` source.
- `git diff --check` -- passed.

## Suggested Review Order

**Selector Policy**

- Central flag gates source references to Debug when source exists.
  [Directory.Build.props:34](../../Directory.Build.props#L34)

- Central package pins define the published Hexalith package versions.
  [Directory.Packages.props:64](../../Directory.Packages.props#L64)

**Converted Package-Capable References**

- Client switches EventStore Client source to package outside Debug.
  [Hexalith.Tenants.Client.csproj:13](../../src/Hexalith.Tenants.Client/Hexalith.Tenants.Client.csproj#L13)

- Server switches EventStore Server source to package outside Debug.
  [Hexalith.Tenants.Server.csproj:5](../../src/Hexalith.Tenants.Server/Hexalith.Tenants.Server.csproj#L5)

- Sample switches EventStore DomainService and Memories contracts by configuration.
  [Hexalith.Tenants.Sample.csproj:11](../../samples/Hexalith.Tenants.Sample/Hexalith.Tenants.Sample.csproj#L11)

- Aspire package helper follows the same EventStore selector.
  [Hexalith.Tenants.Aspire.csproj:16](../../src/Hexalith.Tenants.Aspire/Hexalith.Tenants.Aspire.csproj#L16)

- AppHost helper packages use Release packages while Debug build edges stay separate.
  [Hexalith.Tenants.AppHost.csproj:10](../../src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj#L10)

- Integration tests package EventStore harnesses outside Debug.
  [Hexalith.Tenants.IntegrationTests.csproj:10](../../tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj#L10)

**Source-Only Exceptions**

- Tenants host keeps EventStore web host source because no package exists.
  [Hexalith.Tenants.csproj:23](../../src/Hexalith.Tenants/Hexalith.Tenants.csproj#L23)

- UI host documents unpublished FrontComposer as a source-only exception.
  [Hexalith.Tenants.UI.csproj:16](../../src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj#L16)

- UI tests mirror the FrontComposer exception for shell coverage.
  [Hexalith.Tenants.UI.Tests.csproj:9](../../tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj#L9)

**Verification And Artifacts**

- Spec records Debug success, Release item selection, and feed blocker.
  [spec-debug-release-hexalith-references.md:75](spec-debug-release-hexalith-references.md#L75)

- Proposal captures approved exceptions and upstream package gaps.
  [sprint-change-proposal-2026-06-29-debug-release-hexalith-references.md:40](../planning-artifacts/sprint-change-proposal-2026-06-29-debug-release-hexalith-references.md#L40)
