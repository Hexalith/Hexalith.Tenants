# Sprint Change Proposal — Extract EventStore AppHost Project-Metadata Boilerplate to `Hexalith.EventStore.Aspire`

Date: 2026-06-22
Project: tenants
Workflow: bmad-correct-course
Mode: Batch
Status: APPROVED (2026-06-22 by Administrator / Jérôme Piquot) — routed for implementation
Related precedent: `sprint-change-proposal-2026-06-20-eventstore-test-harness-extraction.md` (same
platform-boundary pattern: move domain-agnostic technical code out of Tenants into the EventStore
platform). This proposal follows the same cross-submodule sequencing.

## 1. Issue Summary

**Trigger (user request):** "This class is a common need for all domain modules. Move it to the
EventStore module. Create an Aspire helper in EventStore."

The "class" is the **cross-repo AppHost project-metadata boilerplate** that the Tenants AppHost
hand-rolls to host its services on the shared EventStore platform:

- `src/Hexalith.Tenants.AppHost/ProjectMetadataPaths.cs` — repository-root resolver
  (`AppContext.BaseDirectory` + 5 levels up) used to locate cross-repo project files.
- `src/Hexalith.Tenants.AppHost/HexalithEventStore.cs` — `IProjectMetadata` for the EventStore
  command-gateway project (`SuppressBuild => true`).
- `src/Hexalith.Tenants.AppHost/HexalithEventStoreAdminServerHost.cs` — `IProjectMetadata` for the
  Admin.Server.Host project.
- `src/Hexalith.Tenants.AppHost/HexalithEventStoreAdminUI.cs` — `IProjectMetadata` for the Admin.UI
  project.

This is a **boundary violation**, confirmed by this repo's own rules:

- **Tenants CLAUDE.md** — "Do not add boilerplate code that is common to domain modules here. Reuse
  existing shared implementations from the technical modules, or move the boilerplate into the
  appropriate technical module before consuming it from Tenants… Typical homes… include
  `Hexalith.EventStore`…"
- **`project-context.md`** — Tenants is "a domain plugin that runs ON `Hexalith.EventStore`."
- **`epics.md:367` (Story 1.1 AC)** — "no generic shell, DI, serialization, or event-store
  boilerplate is duplicated inside Hexalith.Tenants."

Adding the three EventStore platform projects (command gateway + Admin.Server.Host + Admin.UI) to an
AppHost is exactly such a common need: **every** domain module built on EventStore needs the same
three `IProjectMetadata` classes and the same repository-path resolver.

### Evidence — the boilerplate is already duplicated

The identical files exist verbatim in a second domain module's AppHost:

```
Hexalith.FrontComposer/src/Hexalith.FrontComposer.AppHost/
  HexalithEventStore.cs
  HexalithEventStoreAdminServerHost.cs
  HexalithEventStoreAdminUI.cs
  ProjectMetadataPaths.cs        ← same repo-root resolver, copy-pasted
```

`ProjectMetadataPaths` is the single class the user pointed at; the three `IProjectMetadata` classes
that depend on it are the rest of the same coherent unit. Together they are copy-pasted into every
domain module today and would be copy-pasted into every future one.

### Target

**`Hexalith.EventStore.Aspire`** (existing published package in the EventStore submodule). It is the
correct home: it already owns the platform's Aspire wiring extensions
(`AddHexalithEventStore`, `AddEventStoreDomainModule`, `HexalithEventStoreResources`) and the Tenants
AppHost already references it by `ProjectReference` (`IsAspireProjectResource="false"`). No new package
is created — the change adds public API to an existing one.

## 2. Impact Analysis

### Epic Impact

No product/PRD epic scope changes. The PRD, epics, architecture, and UX artifacts describe
tenant-management behavior and remain valid. This is a **platform-boundary / build-infrastructure**
correction, not a feature change.

- Epics 1–5: no acceptance-criteria changes to product behavior. The change *reinforces*
  Story 1.1 AC ("no … event-store boilerplate is duplicated inside Hexalith.Tenants") and the
  `epics.md:330` "shared capability, not Tenants-local boilerplate" principle.
- No epic added, removed, reordered, or re-prioritized.

### Story Impact

- No story acceptance criteria change. All Tenants UI/integration tests reference Aspire resources by
  name (`eventstore`, `eventstore-admin`, `eventstore-admin-ui`, `tenants`, …), which are unchanged.
- New rule for future stories that add a domain-module AppHost:

  ```text
  EventStore platform-projects rule: a domain-module AppHost must NOT hand-roll IProjectMetadata
  classes for the EventStore command gateway / Admin.Server.Host / Admin.UI, nor a repository-root
  resolver. Call builder.AddHexalithEventStorePlatformProjects() and resolve any other cross-repo
  project file via Hexalith.EventStore.Aspire.RepositoryProjectPaths.
  ```

### Artifact Conflicts

- **EventStore `Hexalith.EventStore.Aspire` public surface:** three new public types are added
  (`RepositoryProjectPaths`, `HexalithEventStorePlatformExtensions`, `HexalithEventStorePlatformProjects`).
  Adding public API to a published package is a **minor (feat) version bump for EventStore** and an
  **EventStore-repo architecture decision** — must be approved by the EventStore repo owner.
- **EventStore generated API docs** (`docs/reference/api/Hexalith.EventStore.Aspire/*`) become stale
  and should be regenerated (ApiReferenceBuild) as part of the EventStore commit.
- **Tenants AppHost:** four files deleted, five files edited (no `.csproj`/package changes — the
  AppHost is `IsPackable=false`).
- **No release-list change** — `Hexalith.EventStore.Aspire` is already a published package.

### Technical Impact

- **EventStore submodule (separate repo):** three new source files in
  `src/Hexalith.EventStore.Aspire/`. No new project, no `.slnx` change, no new package.
- **Tenants repo:** delete four AppHost files, rewire `Program.cs` + four metadata classes, bump the
  EventStore submodule pointer.
- No backend domain behavior, commands, events, projections, UI contracts, DAPR topology, or Aspire
  **resource graph** change. Resource names, sidecars, and env wiring are byte-for-byte equivalent.

> ⚠️ **Cross-repo / submodule constraint.** This cannot be a single in-place edit. The platform code
> lands and is committed+pushed in the `Hexalith.EventStore` submodule first; the submodule pointer is
> then bumped in Tenants. Because the AppHost consumes the library by `ProjectReference` (source, not
> NuGet), local builds work before the EventStore package is published — but the pointer must
> reference a commit reachable on the EventStore remote (see
> `submodule-pointer-push-consistency`). Sequencing is in §5.

## 3. Recommended Approach

**Recommended path: Direct Adjustment (Hybrid — platform extraction + single reusable helper).**

Rationale:

- It is the explicitly documented "move boilerplate to the technical module" path from CLAUDE.md and
  satisfies the existing Story 1.1 / `epics.md:330` AC instead of leaving a known boundary violation.
- Extract once; Tenants and the already-duplicating FrontComposer (and every future domain) reuse it.
- No product scope, MVP, or architecture replan is touched. No rollback is justified (nothing to
  revert; this removes duplication rather than changing behavior).

Effort estimate: **Low–Medium** (cross-repo, new public API on an existing package; the extraction is
a mechanical move with one namespace fix — `IProjectMetadata` lives in `Aspire.Hosting`, not
`Aspire.Hosting.ApplicationModel`).

Risk level: **Low.** Risks: (a) the submodule-pointer bump must precede / accompany the Tenants commit
or a fresh clone / CI won't resolve the EventStore commit; (b) the new public API must build clean
under EventStore's `TreatWarningsAsErrors` (verified — see §6); (c) the repo-root resolver keeps the
exact 5-levels-up semantics, so the resolved project paths are unchanged for the standard
`<root>/src/<Module>.AppHost/` layout.

## 4. Detailed Change Proposals

### 4.1 What moves vs. what stays

| Move to `Hexalith.EventStore.Aspire` (domain-agnostic platform glue) | Keep in Tenants AppHost (domain-specific) |
|---|---|
| `ProjectMetadataPaths` repo-root + project-path resolver | `HexalithTenants` / `HexalithTenantsUI` / `HexalithTenantsSample` (Tenants' own projects) |
| `IProjectMetadata` for EventStore command gateway | `HexalithMemoriesServer` (cross-repo Memories project metadata) |
| `IProjectMetadata` for Admin.Server.Host | `system\|tenants\|v1` + `system\|global-administrators\|v1` domain-service registrations |
| `IProjectMetadata` for Admin.UI | `tenants.events` topic override; Keycloak/OIDC + `Authentication:JwtBearer:*` wiring |
| The repeated `AddProject<…>("eventstore"/"eventstore-admin"/"eventstore-admin-ui")` calls | Memories inline topology (FalkorDB, secretstore, llm, routing) + `AddHexalithEventStore`/`AddEventStoreDomainModule` calls |

The four "keep" metadata classes are re-pointed at the moved resolver
(`RepositoryProjectPaths`) so the repo keeps a single path-resolution implementation.

### 4.2 New public API — `Hexalith.EventStore.Aspire` (EventStore submodule)

Three new files in `src/Hexalith.EventStore.Aspire/`:

- **`RepositoryProjectPaths.cs`** (public) — replaces `ProjectMetadataPaths`:

  ```csharp
  public static class RepositoryProjectPaths {
      public static string GetProjectPath(params string[] path)
          => Path.Combine(GetRepositoryRoot(), Path.Combine(path));
      public static string GetRepositoryRoot()
          => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
  }
  ```

  (`GetRepositoryRoot` made public — additive; useful for resolving any cross-repo project file.)

- **`EventStorePlatformProjectMetadata.cs`** — three `internal sealed` `IProjectMetadata` classes
  (`EventStoreProjectMetadata`, `EventStoreAdminServerHostProjectMetadata`,
  `EventStoreAdminUIProjectMetadata`), each `SuppressBuild => true`, paths resolved via
  `RepositoryProjectPaths` against the consuming repo's `Hexalith.EventStore` submodule.
  **Note:** `IProjectMetadata` is in the `Aspire.Hosting` namespace (not `Aspire.Hosting.ApplicationModel`).

- **`HexalithEventStorePlatformExtensions.cs`** — the Aspire helper + result record:

  ```csharp
  public sealed record HexalithEventStorePlatformProjects(
      IResourceBuilder<ProjectResource> EventStore,
      IResourceBuilder<ProjectResource> AdminServer,
      IResourceBuilder<ProjectResource> AdminUI);

  public static class HexalithEventStorePlatformExtensions {
      public static HexalithEventStorePlatformProjects AddHexalithEventStorePlatformProjects(
          this IDistributedApplicationBuilder builder,
          string eventStoreName = "eventstore",
          string adminServerName = "eventstore-admin",
          string adminUiName = "eventstore-admin-ui");
  }
  ```

  It only *adds* the three projects (returning their builders for further customization). The
  consumer still calls the existing `AddHexalithEventStore(...)` to wire the DAPR topology — the
  helper composes with, and does not replace, that method.

> Scope note: the EventStore's **own** AppHost is unchanged — it builds those projects directly via
> source-generated `Projects.*` (`ProjectReference`), which is correct for the owning repo. The helper
> is **consumer-only** (cross-repo, `SuppressBuild`).

### 4.3 Tenants AppHost — delete the boilerplate

Delete: `HexalithEventStore.cs`, `HexalithEventStoreAdminServerHost.cs`,
`HexalithEventStoreAdminUI.cs`, `ProjectMetadataPaths.cs`.

### 4.4 Tenants AppHost — `Program.cs` consumes the helper

```csharp
HexalithEventStorePlatformProjects eventStorePlatform = builder.AddHexalithEventStorePlatformProjects();
IResourceBuilder<ProjectResource> eventStore   = eventStorePlatform.EventStore;
IResourceBuilder<ProjectResource> adminServer  = eventStorePlatform.AdminServer;
IResourceBuilder<ProjectResource> adminUI      = eventStorePlatform.AdminUI;
// … existing eventStore.WithEnvironment(domain-service registrations) and AddHexalithEventStore(...) unchanged
```

### 4.5 Tenants AppHost — re-point the four remaining metadata classes

`HexalithTenants` / `HexalithTenantsUI` / `HexalithTenantsSample` / `HexalithMemoriesServer`: swap
`using Projects;` → `using Hexalith.EventStore.Aspire;` and
`ProjectMetadataPaths.GetProjectPath(...)` → `RepositoryProjectPaths.GetProjectPath(...)`.
(`using Projects;` must be removed — after deleting `ProjectMetadataPaths.cs` the `Projects` namespace
no longer exists in this AppHost, since its only `ProjectReference` is `IsAspireProjectResource="false"`.)

### 4.6 Follow-up (out of scope here) — FrontComposer adoption

`Hexalith.FrontComposer.AppHost` has the identical four files and can adopt the same helper. That is a
**separate FrontComposer-repo change** with its own sprint/PR; this proposal only notes the
opportunity and does not require it.

## 5. Implementation Handoff

**Scope classification: Moderate** — cross-repo, a public-API addition to a published platform
package, and a submodule-pointer bump with ordered sequencing. (Not Minor: crosses the submodule
boundary, not a single in-place edit. Not Major: no PRD/architecture/product replan.)

### Sequencing (must be in order)

1. **EventStore repo** — repo owner approves the public-API addition to `Hexalith.EventStore.Aspire`;
   review the three new files; build `Hexalith.EventStore.Aspire` Release clean under
   `TreatWarningsAsErrors`; regenerate the API reference docs.
2. **EventStore repo** — commit (Conventional Commits, e.g.
   `feat(aspire): add EventStore platform-projects AppHost helper + RepositoryProjectPaths`); **push**
   so the commit is reachable on the remote.
3. **Tenants repo** — bump the `Hexalith.EventStore` submodule pointer to that commit; verify the
   gitlink is reachable on the submodule remote.
4. **Tenants repo** — the AppHost is already rewired (§4.3–4.5); build the AppHost Release clean;
   commit (`refactor(apphost): consume EventStore platform-projects helper, drop duplicated metadata`).
   **`refactor` (not `feat`)** — the AppHost is `IsPackable=false`, so this triggers no version bump
   or NuGet publish of the 5 Tenants packages.
5. **Tenants repo** — confirm Tier-2/Tier-3 unaffected (Aspire resource graph unchanged); push the
   branch.

### Handoff recipients

- **EventStore repo owner / Architect** — approve and own the public-API addition (steps 1–2). Use
  the EventStore-scoped BMAD skills (`Hexalith.EventStore:bmad-*`) for work under that submodule.
- **Developer agent (Tenants)** — steps 3–5 after the submodule pointer is available.

### Success criteria

- No EventStore-platform AppHost boilerplate remains in `src/Hexalith.Tenants.AppHost/` — only
  Tenants/Memories-specific metadata + domain wiring.
- `Hexalith.EventStore.Aspire` builds clean under `TreatWarningsAsErrors` with the new public API.
- Tenants AppHost builds clean; the Aspire resource graph (names, sidecars, env) is unchanged.
- The duplicating FrontComposer AppHost (and future domains) can consume the same helper.

## 6. Implementation Log (2026-06-22 — implemented locally, uncommitted, pending approval)

Implemented end-to-end this session; **no commits made** in either repo, pending this approval.

EventStore submodule (uncommitted — three new files):

- `src/Hexalith.EventStore.Aspire/RepositoryProjectPaths.cs` (public).
- `src/Hexalith.EventStore.Aspire/EventStorePlatformProjectMetadata.cs` (three internal
  `IProjectMetadata` classes; `using Aspire.Hosting;` for `IProjectMetadata`).
- `src/Hexalith.EventStore.Aspire/HexalithEventStorePlatformExtensions.cs`
  (`AddHexalithEventStorePlatformProjects` + `HexalithEventStorePlatformProjects` record).

Tenants AppHost (uncommitted):

- Deleted `HexalithEventStore.cs`, `HexalithEventStoreAdminServerHost.cs`,
  `HexalithEventStoreAdminUI.cs`, `ProjectMetadataPaths.cs`.
- `Program.cs` calls `AddHexalithEventStorePlatformProjects()`; `HexalithTenants` /
  `HexalithTenantsUI` / `HexalithTenantsSample` / `HexalithMemoriesServer` use `RepositoryProjectPaths`.

Validation:

- `Hexalith.EventStore.Aspire` — Release build clean: **0 warnings / 0 errors** under
  `TreatWarningsAsErrors`.
- `Hexalith.Tenants.AppHost` — Release build clean: **0 warnings / 0 errors**.
- `rg` sweep over `src`/`tests`/`samples` — **no lingering references** to the deleted types.

Remaining (owner / commit-time): EventStore repo-owner approval for the public-API addition; API-doc
regeneration; commit+push EventStore (`feat`); bump the Tenants submodule pointer; commit Tenants
(`refactor`).

## 7. Checklist Results

- [x] 1.1 Trigger identified: user request — move the common AppHost project-metadata boilerplate to EventStore + add an Aspire helper.
- [x] 1.2 Core problem: misplaced platform boundary (domain module hand-rolls reusable EventStore platform-project wiring). Type: technical/architectural duplication.
- [x] 1.3 Evidence: Tenants CLAUDE.md + project-context.md boundary rules; `epics.md:330/367` ACs; identical files duplicated in `Hexalith.FrontComposer.AppHost`.
- [x] 2.1–2.5 Epic assessment: product epics valid; reinforces Story 1.1 AC; no epic add/remove/reorder/re-prioritize.
- [x] 3.1 PRD: no conflict; MVP unaffected.
- [x] 3.2 Architecture: public-API addition in the EventStore platform package; no Tenants domain-architecture change; AppHost source-tree annotations remain valid.
- [x] 3.3 UX: none.
- [x] 3.4 Other artifacts: EventStore `Hexalith.EventStore.Aspire` public surface + generated API docs; Tenants AppHost files. No release-list, `.slnx`, or Tenants `.csproj`/package change.
- [x] 4.1 Direct Adjustment (Hybrid): viable, Low–Medium effort, Low risk — selected.
- [N/A] 4.2 Rollback: not viable (no completed feature to revert; this removes duplication).
- [N/A] 4.3 MVP review: not viable (MVP unchanged).
- [x] 4.4 Recommended path: Direct Adjustment (platform extraction + single reusable helper).
- [x] 5.1–5.5 Issue/impact/recommendation/action plan/handoff: included.
- [x] 6.1 Checklist complete.
- [x] 6.2 Proposal reviewed against discovered files (build-verified).
- [x] 6.3 User approval: obtained 2026-06-22 (Administrator / Jérôme Piquot).
- [x] 6.4 Sprint-status update: added tracking entry `cc-2026-06-22-eventstore-aspire-platform-projects-helper` (status `review`; no epic/story add/remove/reorder).
- [x] 6.5 Next steps/handoff: EventStore repo owner (steps 1–2), Tenants Developer agent (steps 3–5).

## 8. Approval Request

Approve this Sprint Change Proposal for implementation?

- `yes` — record approval, add the sprint-status tracking entry, route to the EventStore repo owner
  (public-API commit) then the Tenants Developer agent (submodule-pointer bump + AppHost commit).
- `revise` — adjust the target, the extraction surface, the naming, or the sequencing.
- `no` — stop this correction.
