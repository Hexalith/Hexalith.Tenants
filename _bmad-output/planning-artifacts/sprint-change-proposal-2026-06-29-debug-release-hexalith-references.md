# Sprint Change Proposal: Debug Project References and Release NuGet References for Hexalith Libraries

Date: 2026-06-29
Requested by: Administrator
Status: Approved and implemented - Debug verified; Release package restore blocked by upstream package-feed gaps
Mode: Batch

## 1. Issue Summary

Administrator identified a build policy correction:

> Projects must use project references for Hexalith libraries for Debug and NuGet package references for Release.

The current Tenants repo partially satisfies this policy, but its source/package selector is still based on
whether a dependency submodule is present:

```xml
<HexalithEventStoreFromSource Condition="Exists('$(HexalithEventStoreRoot)\src\Hexalith.EventStore.Contracts\Hexalith.EventStore.Contracts.csproj')">true</HexalithEventStoreFromSource>
<HexalithMemoriesFromSource Condition="Exists('$(HexalithMemoriesRoot)\src\Hexalith.Memories.Contracts\Hexalith.Memories.Contracts.csproj')">true</HexalithMemoriesFromSource>
```

That means Release builds still use source `ProjectReference`s when the submodules are checked out. This conflicts
with the stricter policy: Debug should use source projects for local debugging, while Release should consume the
published Hexalith packages for package-only validation and release fidelity.

This supersedes the package-selection part of
`sprint-change-proposal-2026-06-29-apphost-project-references.md`. The AppHost Debug build-forcing references for
launched child host projects remain valid and should not be removed.

Evidence gathered:

- Existing project files use `HexalithEventStoreFromSource` / `HexalithMemoriesFromSource` for several dual
  ProjectReference/PackageReference pairs.
- `Hexalith.EventStore.Client` and `Hexalith.EventStore.Server` are published on nuget.org through `3.19.0`, but are
  still source-only in Tenants.
- `Hexalith.EventStore` is not published and is explicitly `IsPackable=false`; it is a web host, not a library
  package.
- `Hexalith.FrontComposer.Contracts` and `Hexalith.FrontComposer.Shell` are not currently available on nuget.org
  (404 from the NuGet flat-container API), even though the submodule projects have `PackageId` values.
- Release package restore reports missing upstream Commons packages from the configured feed, including
  `Hexalith.Commons.UniqueIds >= 3.19.0`, `Hexalith.Commons.Aspire`, and `Hexalith.Commons.ServiceDefaults`.
  Release package-only validation for EventStore remains blocked until the upstream feed publishes the full
  Hexalith 3.x package set or a root feed is configured.

## 2. Impact Analysis

### Epic Impact

No product epic scope changes are required. The PRD, UX design, command journeys, tenant contracts, aggregates,
events, projections, and user-facing flows remain unchanged.

Impacted planning and implementation areas:

- Build and release policy for all package-capable Hexalith library references.
- Package-only consumer validation and Release build fidelity.
- The prior June 29 AppHost proposal's Phase 2 selection rule.

### Story Impact

No existing UI/domain story needs acceptance criteria changes. This is a cross-cutting build-policy correction.

Implementation should be tracked as a small Developer task or a build-infra correction, not as a new product story.

### Artifact Conflicts

PRD: no conflict.

Epics: no story sequence or scope change.

Architecture: update the build/release convention from "source when submodule is present" to "Debug source,
Release package, for package-capable libraries."

UX: no impact.

Sprint status: no epic/story status update is required until implementation is approved and completed.

### Technical Impact

The change touches MSBuild configuration and project references:

- `Directory.Build.props`
- `Directory.Packages.props`
- Package-capable project files under `src/`, `samples/`, and `tests/`

Release validation will expose an upstream dependency gap until Commons 3.x is available from the configured feed.

## 3. Recommended Approach

Recommended path: Direct Adjustment with explicit exceptions.

Rationale:

- The policy is clear and local to build metadata.
- No rollback or MVP scope review is needed.
- Package-capable Hexalith libraries should follow the rule uniformly.
- Source-only host applications and unavailable packages must be documented as exceptions instead of hidden behind
  misleading Release source references.

Scope classification: Moderate.

The code edits are small, but full Release verification is blocked by upstream publishing state. The Developer can
wire the MSBuild graph now and record the validation blocker, but package-only Release cannot pass from nuget.org
alone until the EventStore transitive Commons 3.x dependency is available.

## 4. Detailed Change Proposals

### Proposal A: Replace submodule-presence selection with configuration-based selection

Artifact: `Directory.Build.props`

Current behavior:

```xml
<HexalithEventStoreFromSource Condition="Exists('$(HexalithEventStoreRoot)\src\Hexalith.EventStore.Contracts\Hexalith.EventStore.Contracts.csproj')">true</HexalithEventStoreFromSource>
<HexalithMemoriesFromSource Condition="Exists('$(HexalithMemoriesRoot)\src\Hexalith.Memories.Contracts\Hexalith.Memories.Contracts.csproj')">true</HexalithMemoriesFromSource>
```

Proposed behavior:

```xml
<HexalithEventStoreFromSource
  Condition="'$(Configuration)' == 'Debug' and Exists('$(HexalithEventStoreRoot)\src\Hexalith.EventStore.Contracts\Hexalith.EventStore.Contracts.csproj')">true</HexalithEventStoreFromSource>
<HexalithMemoriesFromSource
  Condition="'$(Configuration)' == 'Debug' and Exists('$(HexalithMemoriesRoot)\src\Hexalith.Memories.Contracts\Hexalith.Memories.Contracts.csproj')">true</HexalithMemoriesFromSource>
```

Rationale:

- Debug uses source projects when the submodule exists.
- Release leaves these properties empty/false, so the existing PackageReference conditions activate.
- The root path detection still supports standalone, child `references/`, and parent `references/` layouts.

### Proposal B: Add missing EventStore package pins

Artifact: `Directory.Packages.props`

Current package-capable EventStore references are missing package pins for:

- `Hexalith.EventStore.Client`
- `Hexalith.EventStore.Server`

Proposed additions:

```xml
<PackageVersion Include="Hexalith.EventStore.Client" Version="$(HexalithEventStoreVersion)" />
<PackageVersion Include="Hexalith.EventStore.Server" Version="$(HexalithEventStoreVersion)" />
```

Rationale:

- Both packages exist on nuget.org through `3.19.0`.
- Tenants currently consumes both libraries by source only.
- Central package management requires version pins in `Directory.Packages.props`, not project files.

### Proposal C: Convert remaining package-capable EventStore refs to dual Debug/Release refs

Artifacts:

- `src/Hexalith.Tenants.Client/Hexalith.Tenants.Client.csproj`
- `src/Hexalith.Tenants.Server/Hexalith.Tenants.Server.csproj`
- `samples/Hexalith.Tenants.Sample/Hexalith.Tenants.Sample.csproj`

Current examples:

```xml
<ProjectReference Include="$(HexalithEventStoreRoot)\src\Hexalith.EventStore.Client\Hexalith.EventStore.Client.csproj" />
<ProjectReference Include="$(HexalithEventStoreRoot)\src\Hexalith.EventStore.Server\Hexalith.EventStore.Server.csproj" />
<ProjectReference Include="$(HexalithEventStoreRoot)\src\Hexalith.EventStore.DomainService\Hexalith.EventStore.DomainService.csproj" />
```

Proposed pattern:

```xml
<ProjectReference Include="$(HexalithEventStoreRoot)\src\Hexalith.EventStore.Client\Hexalith.EventStore.Client.csproj"
                  Condition="'$(HexalithEventStoreFromSource)' == 'true'" />
<PackageReference Include="Hexalith.EventStore.Client"
                  Condition="'$(HexalithEventStoreFromSource)' != 'true'" />
```

Apply the same pattern to `Hexalith.EventStore.Server` and the unconditioned sample
`Hexalith.EventStore.DomainService` reference.

Rationale:

- These are package-capable libraries.
- Release builds should compile against published package contracts.
- Debug source builds preserve step-through debugging across Hexalith libraries.

### Proposal D: Keep source-only host references as documented exceptions

Artifacts:

- `src/Hexalith.Tenants/Hexalith.Tenants.csproj`
- `src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj`
- `src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj`
- `tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj`

Exceptions:

- `Hexalith.EventStore` web host: no NuGet package; `IsPackable=false`.
- Launched AppHost child projects: applications, not library packages; Debug build-forcing edges remain valid.
- `Hexalith.FrontComposer.Contracts` and `Hexalith.FrontComposer.Shell`: package IDs exist in source, but the
  packages are not on nuget.org today.

Rationale:

- The policy applies cleanly to package-capable libraries.
- Source-only application/host references cannot be converted to package references until the upstream project
  publishes the relevant package or refactors the dependency boundary.

Follow-up for FrontComposer:

- Publish `Hexalith.FrontComposer.Contracts` and `Hexalith.FrontComposer.Shell` packages, then add
  `HexalithFrontComposerVersion` and dual Debug/Release references in Tenants UI and UI tests.

Follow-up for EventStore host:

- Replace the Tenants host dependency on `Hexalith.EventStore` web host with package-capable library references
  where possible, or keep it as a documented source-only app dependency until EventStore provides a packageable
  composition surface.

### Proposal E: Update comments and the previous proposal's interpretation

Artifacts:

- `Directory.Build.props`
- project-file comments around dual source/package references
- optionally `sprint-change-proposal-2026-06-29-apphost-project-references.md`

Current wording says "source when the submodule is checked out."

Proposed wording:

"Debug uses source ProjectReference when the dependency source is available; Release uses the published NuGet
package. Source-only host/application dependencies are documented exceptions."

Rationale:

- Avoid preserving the old submodule-presence policy in comments after changing the actual behavior.

## 5. Validation Plan

After implementation:

1. Run a Debug build:

   ```bash
   dotnet build Hexalith.Tenants.slnx -c Debug
   ```

   Expected result: package-capable Hexalith refs select `ProjectReference` when submodules are present.

2. Inspect Release restore/build item selection:

   ```bash
   dotnet build Hexalith.Tenants.slnx -c Release -warnaserror
   ```

   Expected policy result: package-capable Hexalith refs select `PackageReference`.

   Current expected external blocker: EventStore package restore can fail on missing `Hexalith.Commons.UniqueIds`
   3.x from nuget.org unless a feed with the full Hexalith 3.x package set is configured.

3. Validate package-only consumers after the upstream package feed is complete:

   ```bash
   python3 scripts/validate-consumer-package-references.py
   ```

4. For AppHost freshness, keep the existing Debug-only child build edges and rerun the AppHost Debug build or
   `aspire run` smoke once normal build validation passes.

## 6. Correct-Course Checklist

- [x] 1.1 Triggering story identified: N/A - cross-cutting build policy correction from Administrator.
- [x] 1.2 Core problem defined: Release still uses source references when submodules are present.
- [x] 1.3 Supporting evidence gathered: MSBuild props/project files, NuGet package availability, upstream gaps.
- [x] 2.1 Current epic still viable: yes; no product epic changes.
- [x] 2.2 Epic-level changes needed: none.
- [x] 2.3 Remaining epics reviewed: no impact.
- [x] 2.4 New epics needed: no.
- [x] 2.5 Epic order/priority change: no.
- [x] 3.1 PRD conflicts checked: none.
- [x] 3.2 Architecture conflicts checked: build/release convention update needed.
- [x] 3.3 UI/UX conflicts checked: none.
- [x] 3.4 Other artifacts checked: project files, package pins, prior AppHost proposal, sprint status.
- [x] 4.1 Direct Adjustment: viable, with upstream package-feed risk.
- [x] 4.2 Potential Rollback: not viable; would preserve the incorrect Release behavior.
- [x] 4.3 PRD MVP Review: not applicable.
- [x] 4.4 Recommended path selected: Direct Adjustment with documented source-only exceptions.
- [x] 5.1 Issue summary created.
- [x] 5.2 Epic/artifact impact documented.
- [x] 5.3 Recommended path documented.
- [x] 5.4 MVP impact/action plan documented.
- [x] 5.5 Handoff plan established.
- [x] 6.1 Checklist completion reviewed.
- [x] 6.2 Proposal checked for consistency.
- [x] 6.3 User approval received.
- [N/A] 6.4 Sprint status update: no epic/story add/remove/renumbering.
- [x] 6.5 Next steps and handoff completed.

## 7. Implementation Handoff

Scope: Moderate build-policy correction.

Route to: Developer agent.

Developer responsibilities:

- Implement Proposals A, B, C, and E.
- Keep Proposal D exceptions documented and narrow.
- Do not modify submodule files.
- Do not overwrite existing unrelated `_bmad-output/implementation-artifacts/sprint-status.yaml` changes.
- Validate Debug build.
- Validate Release item selection and record the Commons 3.x/FrontComposer package blockers if Release restore cannot
  complete from nuget.org.

Upstream responsibilities:

- Publish `Hexalith.Commons.UniqueIds` 3.x, or configure the root Tenants restore to use the feed containing the
  full Hexalith 3.x package set.
- Publish FrontComposer packages if Release package-only policy must apply to UI host dependencies as well.

Success criteria:

- Debug builds use `ProjectReference` for package-capable Hexalith libraries when source is available.
- Release builds use `PackageReference` for package-capable Hexalith libraries.
- Source-only host/application dependencies are explicit exceptions, not accidental Release source references.
- Release package-only validation either passes or fails only on documented upstream package/feed gaps.
