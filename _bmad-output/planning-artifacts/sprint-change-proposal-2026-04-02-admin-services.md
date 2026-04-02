# Sprint Change Proposal — Add EventStore Admin Server & UI to Aspire Topology

**Date:** 2026-04-02
**Triggered by:** Developer experience — need EventStore admin tooling in Tenants local development topology
**Scope classification:** Minor — direct implementation by development team
**Approved edit proposals:** 3/3

---

## Section 1: Issue Summary

The Hexalith.Tenants AppHost lacks EventStore admin tooling. During local development, inspecting event store internals (aggregates, events, projections) requires running the EventStore AppHost separately. The EventStore submodule already ships `Hexalith.EventStore.Admin.Server.Host` (REST API) and `Hexalith.EventStore.Admin.UI` (Blazor Server with Fluent UI) — they just need wiring into the Tenants AppHost topology.

This is not a bug or a missing MVP feature. All 8 epics are **done**. This is a post-MVP developer experience enhancement that makes the Tenants development topology self-contained.

---

## Section 2: Impact Analysis

### Epic Impact
- **No epic changes.** All epics remain **done**. This falls within Epic 7 (Deployment & Observability) scope but doesn't alter any story or acceptance criteria.

### Story Impact
- **No story changes.** Story 7.1 (Aspire Hosting & AppHost) acceptance criteria say the AppHost launches "Hexalith.Tenants, EventStore server, and Keycloak." Adding admin services extends the topology without changing the existing criteria.

### Artifact Conflicts

| Artifact | Change Type | Details |
|----------|------------|---------|
| `src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj` | Modify | Add 2 project references: Admin.Server.Host, Admin.UI |
| `src/Hexalith.Tenants.AppHost/DaprComponents/accesscontrol.eventstore-admin.yaml` | Create | DAPR access control config for admin server sidecar |
| `src/Hexalith.Tenants.AppHost/Program.cs` | Modify | Add admin server + UI resources, DAPR sidecar wiring, Keycloak auth, refactor config path resolution to helper function |

### Technical Impact
- **Domain code:** Zero changes
- **Test code:** Zero changes
- **Package APIs:** Zero changes
- **NuGet packages:** Zero changes
- **CI/CD:** Zero changes — admin projects are from the EventStore submodule, not built/published by Tenants

---

## Section 3: Recommended Approach

**Selected path:** Direct Adjustment — straightforward AppHost wiring following the established EventStore AppHost pattern.

**Rationale:**
- The EventStore AppHost already has the complete wiring pattern for admin services
- The Tenants AppHost just mirrors this pattern, reusing the same DAPR app IDs, env var names, and service references
- No new packages, no new domain concepts, no architectural changes
- Effort: **Low** (3 files, ~50 lines of new code)
- Risk: **Low** (no domain logic changes, pattern is proven in EventStore)

---

## Section 4: Detailed Change Proposals

### 4.1: AppHost `.csproj` — Add project references

**File:** `src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj`

Add two project references to the EventStore admin projects in the submodule:
```xml
<ProjectReference Include="..\..\Hexalith.EventStore\src\Hexalith.EventStore.Admin.Server.Host\Hexalith.EventStore.Admin.Server.Host.csproj" />
<ProjectReference Include="..\..\Hexalith.EventStore\src\Hexalith.EventStore.Admin.UI\Hexalith.EventStore.Admin.UI.csproj" />
```

### 4.2: DaprComponents — Admin access control config

**File:** `src/Hexalith.Tenants.AppHost/DaprComponents/accesscontrol.eventstore-admin.yaml` (new)

Create DAPR access control config for the admin server sidecar, copied from EventStore's equivalent. Allow-by-default for local development.

### 4.3: AppHost `Program.cs` — Full admin wiring

**File:** `src/Hexalith.Tenants.AppHost/Program.cs`

Key changes:
1. **Config path resolution** — refactored to `ResolveDaprConfigPath()` helper function + added admin config path
2. **EventStore sidecar** — added fixed `DaprHttpPort = 3501` so Admin.Server can query the sidecar metadata endpoint
3. **Admin.Server** — added project resource with DAPR sidecar (state store access, EventStore service invocation)
4. **Admin.UI** — added project resource with Admin.Server reference, Swagger URL, external HTTP endpoints
5. **Keycloak** — extended auth wiring for Admin.Server (JWT Bearer) and Admin.UI (OIDC client credentials)
6. **Non-Keycloak fallback** — provides Swagger URL to Admin.UI when Keycloak is disabled

Pattern mirrors EventStore AppHost exactly: same DAPR app IDs (`eventstore-admin`, `eventstore-admin-ui`), same env var names, same service reference topology.

---

## Section 5: Implementation Handoff

**Change scope:** Minor — direct implementation by development team.

**Handoff:** Developer (Jerome) implements all 3 file changes.

**Implementation sequence:**
1. Create `accesscontrol.eventstore-admin.yaml` in DaprComponents
2. Add project references to `.csproj`
3. Update `Program.cs` with admin wiring
4. Verify with `dotnet build` on the AppHost
5. Verify with `dotnet run` on the AppHost — confirm admin services appear in Aspire dashboard

**Success criteria:**
- AppHost builds without errors
- Aspire dashboard shows `eventstore-admin` and `eventstore-admin-ui` resources
- Admin UI is accessible via external HTTP endpoint
- Admin Server can reach EventStore via DAPR service invocation
- Keycloak auth works for both admin services when enabled
