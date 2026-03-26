# Sprint Change Proposal — 2026-03-26

**Project:** Hexalith.Tenants
**Author:** Jerome
**Date:** 2026-03-26
**Change Scope:** Minor

---

## Section 1: Issue Summary

Three related refinements identified after all 8 epics completed:

1. **Aspire AppHost missing infrastructure dependencies** — The AppHost orchestrates only the Tenants service and Sample consuming service, but doesn't provision EventStore server or Keycloak (identity provider) — both are runtime prerequisites documented in the PRD (FR59-60) and architecture (JWT Bearer auth, EventStore dependency).

2. **Project naming misalignment** — The deployable project is named `Hexalith.Tenants.CommandApi`, an internal architectural label. The service should be named `Hexalith.Tenants` to match the domain and deployment identity.

3. **DAPR AppId inconsistency** — The DAPR sidecar AppId is `"commandapi"` but all other DAPR resource names use the domain name `tenants` (`tenants-eventstore`, `tenants.events`, `deadletter.tenants.events`). AppId should be `"tenants"`.

**Trigger:** Post-completion alignment — no specific story revealed this; it's a deployment clarity refinement.

---

## Section 2: Impact Analysis

### Epic Impact

| Epic | Status | Impact |
|------|--------|--------|
| Epic 1 (Foundation) | done | Solution file, dependency chain descriptions — naming only |
| Epic 2 (Core Tenant Management) | done | Story 2.4 title and references — naming only |
| Epic 3 (Membership & Config) | done | Story references — naming only |
| Epic 4 (Integration) | done | Story references — naming only |
| Epic 5 (Query) | done | Story references — naming only |
| Epic 6 (Testing) | done | No impact |
| Epic 7 (Deployment) | done | Story 7.1 — Aspire topology change (EventStore + Keycloak) + naming |
| Epic 8 (Documentation) | done | Story references — naming only |

No epics added, removed, or resequenced.

### Artifact Conflicts

| Artifact | Impact | Severity |
|----------|--------|----------|
| Architecture doc | ~15 `CommandApi` references → `Hexalith.Tenants`; Aspire topology; DAPR AppId | Medium |
| Epics doc | Story 2.4 title + ~10 references; Story 7.1 acceptance criteria | Medium |
| PRD | 2 references | Low |
| Implementation story files | 22 files with `CommandApi` references; Story 2.4 filename | Low |
| Sprint status YAML | 1 story ID rename | Low |
| CI/CD pipelines | No changes needed | None |
| UX Design Specification | No changes needed | None |

### Technical Impact

| Component | Change |
|-----------|--------|
| `src/Hexalith.Tenants.CommandApi/` | Rename directory → `src/Hexalith.Tenants/` |
| `Hexalith.Tenants.CommandApi.csproj` | Rename → `Hexalith.Tenants.csproj` |
| All `.cs` files in project | Namespace `Hexalith.Tenants.CommandApi` → `Hexalith.Tenants` |
| `Hexalith.Tenants.slnx` | Update project path |
| `Hexalith.Tenants.AppHost.csproj` | Update `ProjectReference` path |
| `HexalithTenantsExtensions.cs` | AppId `"commandapi"` → `"tenants"` |
| `AppHost/Program.cs` | Add EventStore server + Keycloak; rename project reference |
| Any other `.csproj` with `CommandApi` reference | Update `ProjectReference` paths |

---

## Section 3: Recommended Approach

**Selected:** Direct Adjustment

**Rationale:**
- All changes are naming/topology refinements — no domain logic, event contracts, or test infrastructure affected
- The rename aligns the deployable name with ecosystem conventions (DAPR AppId matches domain naming)
- Adding EventStore + Keycloak to AppHost fulfills PRD prerequisites and improves developer onboarding
- Effort: **Low** — mechanical renames + small Aspire topology addition
- Risk: **Low** — no behavioral changes; build verification catches any missed references
- Timeline impact: **None** — can be implemented in a single session

**Trade-offs considered:**
- Could skip artifact updates and only change code → rejected (artifacts become misleading for future agents)
- Could create a new epic → rejected (scope is too small; this is a post-completion refinement)

---

## Section 4: Detailed Change Proposals

### 4.1 Code Changes

#### 4.1.1 Project Rename
- Directory: `src/Hexalith.Tenants.CommandApi/` → `src/Hexalith.Tenants/`
- File: `Hexalith.Tenants.CommandApi.csproj` → `Hexalith.Tenants.csproj`
- Namespace: `Hexalith.Tenants.CommandApi` → `Hexalith.Tenants` (all `.cs` files)
- Solution file: Update project path in `Hexalith.Tenants.slnx`
- ProjectReferences: Update in `Hexalith.Tenants.AppHost.csproj` and any other referencing `.csproj`

#### 4.1.2 DAPR AppId
- `HexalithTenantsExtensions.cs`: `AppId = "commandapi"` → `AppId = "tenants"`

#### 4.1.3 Aspire Topology (AppHost/Program.cs)
- Add Keycloak as container resource
- Add EventStore as project resource
- Update project reference from `Hexalith_Tenants_CommandApi` to `Hexalith_Tenants`
- Update resource name from `"commandapi"` to `"tenants"`
- Note: Exact Keycloak/EventStore Aspire APIs to be validated during implementation

### 4.2 Architecture Document Updates
- Project structure diagram: `CommandApi` → `Hexalith.Tenants`
- Component list: Update 8-project listing
- Dependencies section: Update cross-component dependency descriptions
- Infrastructure & Deployment: Add Aspire topology detail, add DAPR AppId `tenants`
- DAPR Resource Naming: Add explicit AppId entry
- ~15 total `CommandApi` references → `Hexalith.Tenants`

### 4.3 Epics Document Updates
- Story 2.4 title: "CommandApi, Bootstrap & Event Publishing" → "Tenant Service, Bootstrap & Event Publishing"
- Story 7.1 acceptance criteria: Add EventStore + Keycloak to topology description
- ~10 `CommandApi` references across stories → `Hexalith.Tenants`

### 4.4 PRD Updates
- 2 `CommandApi` references → `Hexalith.Tenants`

### 4.5 Implementation Story Files
- Global rename across 22 files: `Hexalith.Tenants.CommandApi` → `Hexalith.Tenants`
- Story 2.4 filename: `2-4-commandapi-bootstrap-and-event-publishing.md` → `2-4-tenant-service-bootstrap-and-event-publishing.md`
- Sprint status: Update story ID to match

### 4.6 No Changes Required
- CI/CD pipelines (`ci.yml`, `release.yml`) — no `CommandApi` references in paths
- UX Design Specification — references API endpoints, not project names
- NuGet package validation — `CommandApi`/`Hexalith.Tenants` is `IsPackable=false`

---

## Section 5: Implementation Handoff

**Change Scope Classification:** Minor — direct implementation by development team.

**Implementation sequence:**
1. Rename project directory and files (physical rename)
2. Update `.slnx` and all `ProjectReference` entries
3. Update namespaces in all `.cs` files
4. Update `HexalithTenantsExtensions.cs` AppId
5. Update `AppHost/Program.cs` (topology + references)
6. Build and run tests to verify
7. Update planning artifacts (architecture, epics, PRD)
8. Update implementation story files and sprint status

**Success criteria:**
- `dotnet build` succeeds with zero errors
- `dotnet test` passes all tiers
- DAPR sidecar starts with AppId `tenants`
- Aspire dashboard shows EventStore server + Keycloak + Tenants service
- All artifact references consistent (no remaining `CommandApi` references)

**Handoff:** Development team (Jerome) for direct implementation.
