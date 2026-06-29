# Sprint Change Proposal: AppHost Project References — Build Child Assets Fresh, Consistent Paths in Every Layout

Date: 2026-06-29
Requested by: Administrator
Status: Phase 1 Implemented & verified · Phase 2 Implemented & verified (with one documented upstream limitation)
Mode: Incremental

## 1. Issue Summary

The `Hexalith.Tenants` AppHost adds its seven launched children
(`eventstore`, `eventstore-admin`, `eventstore-admin-ui`, `tenants`, `tenants-ui`, `sample`, `memories`)
to the Aspire app model via **path-only `IProjectMetadata` with `SuppressBuild = true`**. Aspire then launches
each child with `dotnet run … --no-build`. Because the children are **not** `<ProjectReference>`s of the AppHost,
nothing on the `aspire run` path compiles them:

- Building/running the AppHost never builds the children (they are outside its MSBuild graph).
- `SuppressBuild = true` makes Aspire launch them `--no-build`.

Net effect: a stale `bin/Debug` runs **silently** — the documented 2026-06-15 incident where previously-fixed bugs
reappeared at runtime purely from stale binaries. Discovered again during an `aspire run` session on 2026-06-29.

Administrator's directive: *"Use project references. We need to be sure that when we launch AppHost, the assets are
built and up to date. Debug builds use project references and release builds use NuGet packages for Hexalith
projects. Be sure the paths to projects are consistent in all situations. The child project can be in a child
references folder or in a parent references folder."*

Category: **technical limitation discovered during operation** (build/orchestration, not product scope).

## 2. Epic & Artifact Impact

**Epic / product impact: none.** No PRD requirement, epic, story, UX behavior, domain contract, command, event,
projection, or tenant-facing flow changes. (Same shape as the 2026-06-26 submodule-references correction.)

**Impacted artifacts (build/infra only):**

- `src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj` — Debug build-forcing child references.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.Aspire/RepositoryProjectPaths.cs` (submodule) — new resolver.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.Aspire/EventStorePlatformProjectMetadata.cs` (submodule) — repoint 3 metadata classes.
- `references/Hexalith.Memories/src/Hexalith.Memories.Aspire/RepositoryProjectPaths.cs` (submodule) — mirror resolver.
- `references/Hexalith.Memories/src/Hexalith.Memories.Aspire/MemoriesServerProjectMetadata.cs` (submodule) — repoint metadata.
- *(Phase 2 only)* the three `.Aspire` csproj files + `Directory.Packages.props` (Tenants + EventStore submodule).

**Non-impacted:** Tenants domain contracts/aggregates, runtime command/query semantics, UI screens/UX, package
identity/URLs, the nested-submodule policy.

## 3. Recommended Path

**Direct Adjustment, phased.** No rollback or MVP review needed. The explicit request is treated as approval.

A key constraint refines the literal ask:

- The launched children are **host applications**, not NuGet library packages — only the 5 Tenants libraries +
  the `.Aspire` helpers ship as packages. So "Release = NuGet" can apply to **library** references but cannot
  apply to the AppHost launching host apps.
- The `.Aspire` helpers are **packable libraries**; they deliberately use path-only `IProjectMetadata` so a
  packable library never `<ProjectReference>`s a host app (which would pull apps into the package graph). The
  build-forcing references therefore belong in the **AppHost** (`IsPackable=false`, dev-only), not the libraries.

Decisions (confirmed with Administrator):

- **Phase 1 now** (low-risk, fully solves the primary requirement); **Phase 2 after** (higher-risk package swap).
- Phase 2 dual-mode keying = **submodule-presence** (`Condition="'$(HexalithXRoot)' != ''"`), generalizing the
  idiom already present in `Hexalith.EventStore.Aspire.csproj` for `Hexalith.Commons.Aspire`.

## 4. Change Proposal

### Phase 1 — IMPLEMENTED & verified

**4.1 AppHost build-forcing references (Debug).**
`src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj` gains a `Condition="'$(Configuration)' == 'Debug'"`
ItemGroup with a `<ProjectReference>` to each of the 7 launched host projects, each:

- pathed via the existing multi-layout `$(HexalithEventStoreRoot)` / `$(HexalithMemoriesRoot)` properties
  (cross-repo) or a relative path (Tenants-own projects);
- `ReferenceOutputAssembly="false" Private="false" IsAspireProjectResource="false"` — a **pure build edge**: it
  forces MSBuild to compile the child with the AppHost without adding the child assembly to the AppHost closure
  or creating a duplicate Aspire resource (the `IProjectMetadata` classes remain the single source of launched
  resources);
- individually `Exists`-guarded so a missing dependency submodule never breaks the AppHost build.

Result: `aspire run` (which builds the AppHost) now recompiles every child before launch; `SuppressBuild`/`--no-build`
is retained for fast launch of the just-built binaries.

**4.2 Launch path == build path in every layout.**
The build path resolves via `$(Hexalith*Root)` (already multi-layout). The launch path resolved via
`RepositoryProjectPaths` only ever appended `references/<module>` under *this* repo's root and enforced a
"must resolve under repository root" guard — which **broke** the parent-`references` layout. Added
`RepositoryProjectPaths.GetReferencedModuleProjectPath(moduleDirectory, …moduleRelativePath)` that probes the same
candidate locations, in the same precedence, as `Directory.Build.props`'s `$(Hexalith*Root)`:

1. `<root>/../…` (this repo nested directly inside the dependency repo)
2. `<root>/../../…`
3. `<root>/references/<module>/…` (standalone dev — common case)
4. `<root>/../<module>/…` (sibling — e.g. both under a parent's `references/`)
5. `<root>/../references/<module>/…` (dependency under the parent's `references/`)

…returning the first that `File.Exists`, else the standalone path. Added to **both** `RepositoryProjectPaths`
copies (EventStore + the intentionally-duplicated Memories one). The 3 EventStore metadata classes and the
Memories metadata class now call it; stale "AppHost build never compiles it" doc comments were corrected.

**Files changed (Phase 1):**

| File | Repo | Change |
|------|------|--------|
| `src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj` | Tenants | Debug build-forcing ItemGroup (7 refs) |
| `…/Hexalith.EventStore.Aspire/RepositoryProjectPaths.cs` | EventStore (submodule) | `GetReferencedModuleProjectPath` |
| `…/Hexalith.EventStore.Aspire/EventStorePlatformProjectMetadata.cs` | EventStore (submodule) | 3 classes → new resolver + doc |
| `…/Hexalith.Memories.Aspire/RepositoryProjectPaths.cs` | Memories (submodule) | mirror resolver |
| `…/Hexalith.Memories.Aspire/MemoriesServerProjectMetadata.cs` | Memories (submodule) | → new resolver + doc |

### Phase 2 — IMPLEMENTED & verified (Tenants repo only; no submodule edits)

Dual-mode the cross-repo **library** references: build from the source project when the dependency submodule is
checked out, else from the published NuGet package. **Keying correction:** `$(Hexalith*Root)` is *always* set
(`Directory.Build.props` has a fallback), so `… != ''` would never be false. Presence is therefore detected via
`Exists(<canonical csproj>)`, surfaced as two new flags in `Directory.Build.props`:
`HexalithEventStoreFromSource` / `HexalithMemoriesFromSource`. Each cross-repo reference to a **published** package
became:

```xml
<ProjectReference Include="$(HexalithEventStoreRoot)\…\X.csproj" Condition="'$(HexalithEventStoreFromSource)' == 'true'" />
<PackageReference Include="Hexalith.EventStore.X"               Condition="'$(HexalithEventStoreFromSource)' != 'true'" />
```

Versions pinned centrally in `Directory.Packages.props` via `$(HexalithEventStoreVersion)=3.19.0` /
`$(HexalithMemoriesVersion)=1.31.1` — both matching the checked-out submodule tags (`v3.19.0` / `v1.31.1`).

**Coverage** — fallbacks added for the references whose project is actually published:
EventStore `Contracts`, `DomainService`, `Aspire`, `Testing`, `Testing.Integration`; Memories `Contracts`,
`Client.Rest`, `Aspire`. **Left source-only (no published package):** the EventStore gateway `Hexalith.EventStore`
(`IsPackable=false`) and **FrontComposer** `Contracts`/`Shell` (absent from nuget.org). Their consumers
(`Hexalith.Tenants` host, `Hexalith.Tenants.UI`, `UI.Tests`) are apps/tests — not published packages — so requiring
the submodule there is acceptable.

**Files changed (Phase 2, Tenants repo only):** `Directory.Build.props` (presence flags), `Directory.Packages.props`
(version props + 8 `PackageVersion`s), and `Hexalith.Tenants.Contracts`, `Hexalith.Tenants`, `Hexalith.Tenants.Aspire`,
`Hexalith.Tenants.AppHost`, `Hexalith.Tenants.UI`, `Hexalith.Tenants.Sample`, `Hexalith.Tenants.IntegrationTests` csproj.

**Verification (2026-06-29):**

- Dev path (submodules present): `slnx` Debug 0/0 and Release `-warnaserror` 0/0 — ProjectReference path unchanged,
  zero regression.
- **Memories fallback proven:** `Hexalith.Tenants.Sample` built with `-p:HexalithMemoriesFromSource=false` →
  restored `Hexalith.Memories.Contracts` 1.31.1 from nuget.org, **build succeeded 0/0**.
- **EventStore fallback — wired correctly, blocked upstream:** building `Hexalith.Tenants.Contracts` with
  `-p:HexalithEventStoreFromSource=false` selected and restored `Hexalith.EventStore.Contracts` 3.19.0, but failed on
  the transitive dependency `Hexalith.Commons.UniqueIds (>= 3.19.0)` — **nuget.org publishes Commons only up to 2.18.0**.
  This is an upstream Hexalith **publishing gap** (EventStore 3.x packages reference Commons 3.x that isn't on
  nuget.org), not a Tenants defect. The EventStore package-only path will work once Commons 3.x is published (or via a
  feed carrying the full Hexalith 3.x set). The **dev/source path is unaffected** (Commons resolved via submodule).

**Net:** the requested Debug-source / Release-package policy is fully wired and dev-safe; the package path is
end-to-end-proven for Memories and one transitive-publish step away for EventStore.

## 5. Validation (Phase 1 — performed 2026-06-29)

- **Build-forcing proven:** touched one `.cs` in each of EventStore / Tenants.UI / Memories.Server, then built
  **only** the AppHost (`-c Debug`) → all three child DLLs recompiled (e.g. EventStore 20:21→09:30). 0/0.
- **Debug solution build:** `dotnet build Hexalith.Tenants.slnx -c Debug` → 0 warnings / 0 errors.
- **Release/CI gate:** `dotnet build Hexalith.Tenants.slnx -c Release -warnaserror` → 0 / 0 (Debug-gated ItemGroup
  does not apply in Release; resolver compiles clean).
- **Live `aspire run`:** full topology came up — **21/21 service resources Running + Healthy**, 7 `*-rebuilder`
  NotStarted, 6 Dapr meta-resources null; zero problem-state resources.
- **Path consistency:** every child launched from its correctly-resolved path
  (`references/Hexalith.EventStore/…`, `references/Hexalith.Memories/…`, `src/Hexalith.Tenants…`) — launch path now
  equals build path. (Parent-`references` layout verified by candidate-ordering logic mirroring
  `Directory.Build.props`; not physically present in the standalone checkout to integration-test.)

## 6. Handoff

- **Scope: Minor (build/infra), Developer-executed** — Phase 1 implemented directly in this session.
- **Uncommitted.** Phase 1 spans the Tenants repo + EventStore + Memories submodules (owner handoff). Phase 2 is
  **Tenants-repo only** (no submodule edits). No commit/push performed.
- **Follow-ups:**
  1. **Upstream:** publish `Hexalith.Commons` 3.x to nuget.org (or document the feed that carries the full Hexalith
     3.x set) so the EventStore package-only fallback can fully restore. Tracked outside this repo.
  2. Optional unit tests for `GetReferencedModuleProjectPath` candidate ordering (EventStore + Memories test projects).
  3. `project-startup-issues` memory updated: `aspire run` now builds children fresh in Debug — manual pre-build no
     longer required.

## 7. Correct-Course Checklist

- [x] Trigger and issue identified (stale-binary risk on `aspire run`).
- [x] Product scope impact assessed (none).
- [x] Architecture / build impact assessed.
- [x] Recommended path selected (Direct Adjustment, phased).
- [x] Phase 1 implemented against the approved path.
- [x] Phase 1 validated (Debug + Release builds, live `aspire run` health, path consistency).
- [x] Phase 2 implemented (submodule-presence keyed via `Exists`, package versions pinned).
- [x] Phase 2 validated (dev path 0/0; Memories fallback proven; EventStore fallback wired, upstream-blocked & documented).
- [x] Implementation handoff defined (upstream Commons publish + optional tests).
