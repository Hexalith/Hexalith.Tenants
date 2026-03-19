# Story 8.1: Quickstart Guide & README

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer evaluating Hexalith.Tenants,
I want a quickstart guide with prerequisite validation that gets me to my first tenant command within 30 minutes,
So that I can evaluate the system quickly and confidently with clear guidance at every step.

## Acceptance Criteria

1. **Given** a developer reads `docs/quickstart.md`
   **When** they follow the guide from the beginning
   **Then** the guide starts with a prerequisite validation section checking: DAPR sidecar is running, EventStore is deployed, `system` tenant is configured in EventStore's domain service registration, and JWT claims include `eventstore:tenant` = `system`

2. **Given** a prerequisite check fails
   **When** the developer reads the validation output
   **Then** the guide provides a specific remediation step with a link to the relevant DAPR or EventStore documentation

3. **Given** all prerequisites pass
   **When** the developer follows the remaining steps
   **Then** they can send a CreateTenant command and see the TenantCreated event within 30 minutes of starting the guide

4. **Given** the quickstart guide
   **When** a developer inspects its content
   **Then** it includes: NuGet package installation, DI configuration, DAPR component setup reference, first command execution, and verification of the produced event

5. **Given** the project README.md
   **When** a developer visits the repository
   **Then** the README includes: project description, badges (build status, NuGet version, license), a link to the quickstart guide, and a placeholder link for the "aha moment" demo (created in Story 8.3)

## Tasks / Subtasks

- [ ] Task 1: Create `docs/quickstart.md` — prerequisite validation section (AC: #1, #2)
  - [ ] 1.1: Write prerequisite list with validation commands for each (only 3 prerequisites needed with Aspire — EventStore deployment and `system` tenant are handled automatically by the AppHost topology):
    - .NET 10 SDK (`dotnet --version` → 10.x)
    - DAPR CLI and runtime (`dapr --version`, `dapr init` status)
    - Docker running (`docker info`)
  - [ ] 1.2: Write remediation steps for each prerequisite failure with links to official docs (DAPR: https://docs.dapr.io/getting-started/, .NET: https://dot.net)
  - [ ] 1.3: Document the `system` tenant requirement — explain that Hexalith.Tenants operates as a platform-level service within EventStore's multi-tenant model using the `system` tenant context, and that the Aspire AppHost topology handles this configuration automatically for local development (no manual EventStore deployment or JWT claim setup needed)
  - [ ] 1.4: State explicitly that the 30-minute target (FR59) assumes prerequisites are already installed — the clock starts at "clone"

- [ ] Task 2: Create `docs/quickstart.md` — clone, build, and run section (AC: #3, #4)
  - [ ] 2.1: Write clone instructions with `--recurse-submodules` flag (EventStore submodule required)
  - [ ] 2.2: Write build verification step: `dotnet build Hexalith.Tenants.slnx --configuration Release`
  - [ ] 2.3: Write Aspire AppHost launch instructions: `dotnet run --project src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj` — explain that this starts the full topology (CommandApi + DAPR sidecar + Redis state store + Keycloak)
  - [ ] 2.4: Note that first run takes longer due to NuGet restore and Docker image pulls
  - [ ] 2.5: **Windows users**: Note that the clone with submodules creates deep nesting (`Hexalith.Tenants/Hexalith.EventStore/src/...`). If the build fails with path-too-long errors, run `git config --system core.longpaths true` and re-clone

- [ ] Task 3: Create `docs/quickstart.md` — first command section (AC: #3, #4)
  - [ ] 3.1: Write JWT token acquisition step using Keycloak (follow EventStore quickstart pattern — `curl` for bash/Zsh, `Invoke-RestMethod` for PowerShell)
  - [ ] 3.2: Write Swagger UI instructions: open CommandApi URL from Aspire dashboard, append `/swagger`, authorize with JWT token
  - [ ] 3.3: **IMPORTANT — Bootstrap before CreateTenant.** The quickstart must establish the correct command sequence: (1) BootstrapGlobalAdmin first (to authorize the actor), then (2) CreateTenant. Without bootstrap, CreateTenant may fail authorization. Document sending BootstrapGlobalAdmin via Swagger UI as the first command (simpler for quickstart than config-file approach):
    ```json
    {
      "tenant": "system",
      "domain": "tenants",
      "aggregateId": "global-administrators",
      "commandType": "BootstrapGlobalAdmin",
      "payload": { "UserId": "admin-user" }
    }
    ```
  - [ ] 3.4: Write CreateTenant command payload example for `POST /api/v1/commands`. Note: `aggregateId` and `payload.TenantId` MUST match — the aggregateId IS the managed tenant ID per the identity scheme (`system:tenants:{aggregateId}`). Verify the actual `CreateTenant` record fields by inspecting `src/Hexalith.Tenants.Contracts/Commands/CreateTenant.cs` — it takes `(string TenantId, string Name, string? Description)`:
    ```json
    {
      "tenant": "system",
      "domain": "tenants",
      "aggregateId": "my-first-tenant",
      "commandType": "CreateTenant",
      "payload": {
        "TenantId": "my-first-tenant",
        "Name": "My First Tenant",
        "Description": "Created via quickstart guide"
      }
    }
    ```
  - [ ] 3.5: Write event verification — show the expected successful response shape (`DomainServiceWireResult` with `IsRejection: false` and `Events` array containing a `TenantCreated` event). Then show how to verify via the query endpoint `GET /api/tenants/my-first-tenant`. Include a note: "If the query returns 404, wait a moment and retry — projections are eventually consistent. The read model may take a few seconds to process the event."
  - [ ] 3.6: Add a note for developers running the quickstart a second time: "If you've run this before, BootstrapGlobalAdmin will return a rejection (GlobalAdminAlreadyBootstrapped) — this is correct behavior. Use a different aggregateId/TenantId for CreateTenant, or expect a TenantAlreadyExists rejection."
  - [ ] 3.7: Write a follow-up "try more commands" section with AddUserToTenant example showing multi-step workflow (create tenant → add user → verify user list)

- [ ] Task 4: Create `docs/quickstart.md` — "Next Steps" section (AC: #4)
  - [ ] **IMPORTANT**: This section is a clearly separated "Next Steps" appendix AFTER the core quickstart flow, not part of the 30-minute path. The developer's first milestone is seeing TenantCreated — everything after that is optional follow-up.
  - [ ] 4.1: Write NuGet package installation steps for consuming service integration:
    - `dotnet add package Hexalith.Tenants.Contracts` (event types)
    - `dotnet add package Hexalith.Tenants.Client` (DI registration and event handling)
  - [ ] 4.2: Write DI configuration snippet using `AddHexalithTenants()` extension method
  - [ ] 4.3: Reference existing `docs/idempotent-event-processing.md` for event handling patterns
  - [ ] 4.4: Reference the sample consuming service at `samples/Hexalith.Tenants.Sample/` as a complete working example

- [ ] Task 5: Create `docs/quickstart.md` — troubleshooting section
  - [ ] 5.1: Add a "Troubleshooting" section at the end of the quickstart covering common AppHost startup failures:
    - **Port conflict**: DAPR sidecar port 3500 already in use → stop other DAPR instances (`dapr stop --all`) or change the port in AppHost configuration
    - **Docker resource limits**: Keycloak + Redis + DAPR + CommandApi can exceed default Docker Desktop memory allocation → increase Docker memory to 4GB+ in Docker Desktop settings
    - **DAPR not initialized**: `dapr init` not run → run `dapr init` (full init, not `--slim`) for the Aspire topology
    - **Build fails on Windows with path-too-long**: Enable long paths with `git config --system core.longpaths true`
  - [ ] 5.2: Add a "Common Errors" sub-section with expected error responses and what they mean:
    - `GlobalAdminAlreadyBootstrapped` → bootstrap already ran, safe to proceed
    - `TenantAlreadyExists` → tenant ID already used, pick a different one
    - `401 Unauthorized` → JWT token expired or invalid, re-acquire from Keycloak

- [ ] Task 6: Update `README.md` with full project documentation (AC: #5)
  - [ ] 6.1: Write project description — multi-tenant management for the Hexalith ecosystem using event sourcing, DAPR, and .NET Aspire
  - [ ] 6.2: Add badges (only badges that resolve to real endpoints — no placeholders, no coverage badge until coverage reporting is wired up):
    - Build status: `[![CI](https://github.com/Hexalith/Hexalith.Tenants/actions/workflows/ci.yml/badge.svg)](https://github.com/Hexalith/Hexalith.Tenants/actions/workflows/ci.yml)`
    - NuGet version (Contracts package as representative): `[![NuGet](https://img.shields.io/nuget/v/Hexalith.Tenants.Contracts)](https://www.nuget.org/packages/Hexalith.Tenants.Contracts)`
    - License: `[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)`
  - [ ] 6.3: Write features list (tenant lifecycle, user-role management, global admin, configuration, event-driven integration, in-memory testing fakes)
  - [ ] 6.4: Write NuGet packages table listing all 5 packages with descriptions
  - [ ] 6.5: Add quickstart link pointing to `docs/quickstart.md` — include time estimate: "~15 minutes with prerequisites installed, ~45 minutes including first-time prerequisite setup"
  - [ ] 6.6: Add placeholder for "aha moment" demo link/GIF (created in Story 8.3)
  - [ ] 6.7: Write project structure overview (src/, tests/, samples/, docs/)
  - [ ] 6.8: Write contributing section (branch naming conventions, PR process, test requirements, link to `.editorconfig`)

- [ ] Task 7: Validation
  - [ ] 7.1: Verify `docs/quickstart.md` file is well-formed markdown, all code blocks have language annotations, all links are valid relative paths
  - [ ] 7.2: Verify `README.md` renders correctly (no broken badge URLs, proper heading hierarchy, no orphaned links)
  - [ ] 7.3: Verify all command examples use the correct tenant/domain/aggregateId format matching the actual `appsettings.json` configuration (`system|tenants|v1` domain service registration)

## Dev Notes

### Architecture Context

This is a **documentation-only** story — no C# code changes, no tests to write. The deliverables are two markdown files: `docs/quickstart.md` and an updated `README.md`.

The documentation must accurately reflect the current state of the implemented system (Epics 1-7 are complete or in-progress). All code, configuration, and infrastructure referenced in the docs must already exist.

### What Already Exists (DO NOT Recreate)

| Component | Path | Relevance |
|-----------|------|-----------|
| CommandApi Program.cs | `src/Hexalith.Tenants.CommandApi/Program.cs` | Shows actual DI registration, middleware pipeline, and endpoint mapping — docs must match this |
| appsettings.json | `src/Hexalith.Tenants.CommandApi/appsettings.json` | Shows domain service registration (`system\|tenants\|v1`), snapshot config, auth config, bootstrap config |
| appsettings.Development.json | `src/Hexalith.Tenants.CommandApi/appsettings.Development.json` | Shows dev JWT config (signing key, issuer, audience) |
| AppHost | `src/Hexalith.Tenants.AppHost/` | Aspire topology — launches CommandApi + DAPR sidecar + dependencies |
| Sample consuming service | `samples/Hexalith.Tenants.Sample/` | Reference implementation for event subscription |
| Idempotent processing doc | `docs/idempotent-event-processing.md` | Already written — link to it, do not duplicate content |
| Client DI extension | `src/Hexalith.Tenants.Client/` | `AddHexalithTenants()` extension method for consuming services |
| CI workflow | `.github/workflows/ci.yml` | Build status badge URL source |
| Release workflow | `.github/workflows/release.yml` | NuGet publishing pipeline |
| EventStore quickstart | `Hexalith.EventStore/docs/getting-started/quickstart.md` | Pattern to follow — Aspire AppHost launch, Keycloak JWT acquisition, Swagger UI command submission |
| Health check endpoint | Built into CommandApi via `MapDefaultEndpoints()` | Verify service is running |

### Critical Patterns to Follow

**EventStore Quickstart Pattern**: The EventStore quickstart (`Hexalith.EventStore/docs/getting-started/quickstart.md`) establishes the developer experience pattern:
1. Clone with submodules
2. Start Aspire AppHost (`dotnet run --project` or `aspire run --project`)
3. Get JWT token from Keycloak using curl/PowerShell
4. Submit command via Swagger UI (`POST /api/v1/commands`)
5. Verify the event/result

The Tenants quickstart MUST follow this same pattern for ecosystem consistency. Do NOT invent a different workflow.

**Command Payload Format**: Commands go through EventStore's `CommandsController` at `POST /api/v1/commands`. The request body uses:
```json
{
  "tenant": "system",
  "domain": "tenants",
  "aggregateId": "<tenant-id>",
  "commandType": "<CommandTypeName>",
  "payload": { /* command-specific fields */ }
}
```
The `tenant` is always `system` (platform tenant context). The `domain` is always `tenants`. The `aggregateId` is the managed tenant ID for TenantAggregate commands or `global-administrators` for GlobalAdmin commands. **IMPORTANT**: For TenantAggregate commands, `aggregateId` and `payload.TenantId` MUST match — the aggregateId IS the managed tenant ID per the identity scheme. Verified: `CreateTenant` record is `(string TenantId, string Name, string? Description)` — see `src/Hexalith.Tenants.Contracts/Commands/CreateTenant.cs`.

**Bootstrap Mechanism and Command Ordering**: When the AppHost starts, `TenantBootstrapHostedService` reads `Tenants:BootstrapGlobalAdminUserId` from configuration and sends a `BootstrapGlobalAdmin` command. If the value is empty (default in appsettings.json), no bootstrap occurs. For the quickstart, the developer either:
- Configures `BootstrapGlobalAdminUserId` in appsettings.Development.json before starting, OR
- Sends a manual `BootstrapGlobalAdmin` command via Swagger UI as the first command

The Swagger UI approach is simpler for a quickstart — use it as the primary path. **CRITICAL: The quickstart must establish the correct command sequence: (1) BootstrapGlobalAdmin → (2) CreateTenant.** Without an authorized GlobalAdmin, CreateTenant will fail. The BootstrapGlobalAdmin command uses `aggregateId: "global-administrators"`, NOT a tenant ID.

**Eventual Consistency on Query Verification**: After submitting a command, the query endpoint (`GET /api/tenants/{id}`) reads from projections which are eventually consistent. If the developer queries immediately after command submission, the projection may not have processed the event yet. The docs must mention: "If the query returns 404, wait a moment and retry."

**NuGet Packages (5 published)**:

| Package | Purpose |
|---------|---------|
| `Hexalith.Tenants.Contracts` | Commands, events, enums, identity types — shared API surface |
| `Hexalith.Tenants.Client` | DI registration, event handlers, client abstractions |
| `Hexalith.Tenants.Server` | Aggregates, projections, domain processing |
| `Hexalith.Tenants.Testing` | In-memory fakes, test helpers (production-parity domain logic) |
| `Hexalith.Tenants.Aspire` | .NET Aspire hosting extensions for consuming AppHosts |

### Dev Agent MUST-VERIFY Checklist (Before Writing Docs)

These items have ambiguity that CANNOT be resolved from planning artifacts alone. The dev agent must inspect the running system or source code to get the correct answer:

1. **Command endpoint path**: Is it `/api/v1/commands` or `/api/commands`? Grep for the actual route registration in EventStore's `CommandsController` or inspect Swagger UI after launching the AppHost. The story references `/api/v1/commands` from the EventStore quickstart, but verify this is the actual path for the Tenants CommandApi.

2. **JWT token acquisition path**: The Tenants `appsettings.Development.json` uses `Issuer: "hexalith-dev"` and a hardcoded signing key, while the EventStore quickstart uses Keycloak on port 8180 with `Issuer: "hexalith"`. These configurations differ. Inspect `src/Hexalith.Tenants.AppHost/Program.cs` to determine:
   - Does the Tenants AppHost include Keycloak in its topology?
   - If yes, what realm/client configuration does it use?
   - If no, does it use the dev signing key for local JWT generation? If so, document a `curl` command or script that generates a valid JWT using the dev signing key instead of Keycloak.

3. **BootstrapGlobalAdmin via Swagger UI**: Verify this command works when submitted through the public API (not just the hosted service). The authorization pipeline may reject it if the JWT claims don't grant sufficient permissions for the `global-administrators` aggregate. Test by:
   - Start AppHost with empty `BootstrapGlobalAdminUserId` (default)
   - Submit BootstrapGlobalAdmin via Swagger UI with a valid JWT
   - If rejected by auth → switch the quickstart to the config-based approach (set `BootstrapGlobalAdminUserId` in appsettings.Development.json before starting)

4. **Query endpoint URL**: Verify the exact route for tenant detail queries. Is it `GET /api/tenants/{tenantId}` or a different pattern? Grep for route group registration in the CommandApi source or check Swagger UI.

### Anti-Patterns to Avoid

- **DO NOT** reference DAPR component YAML files in `dapr/components/` — the Aspire AppHost handles DAPR configuration automatically. No manual DAPR component setup is needed for the quickstart path.
- **DO NOT** write steps that require `dapr run` directly — the Aspire AppHost orchestrates DAPR sidecars. Use `dotnet run --project src/Hexalith.Tenants.AppHost/` only.
- **DO NOT** duplicate content from `docs/idempotent-event-processing.md` — link to it.
- **DO NOT** document features from Story 8.2 (event contract reference) or Story 8.3 (aha moment demo) — those are separate stories. Use placeholder links where the README references them.
- **DO NOT** add coverage badge — no coverage reporting endpoint exists yet. A broken or placeholder badge looks worse than no badge.
- **DO NOT** hardcode version numbers for NuGet packages in installation instructions — use latest or specify `dotnet add package Hexalith.Tenants.Contracts` without version.
- **DO NOT** skip the BootstrapGlobalAdmin step before CreateTenant — without an authorized GlobalAdmin, CreateTenant will fail authorization. The quickstart MUST establish: bootstrap first, then create tenant.

### Project Structure Notes

Files to create/modify:
- **CREATE**: `docs/quickstart.md` — new file
- **MODIFY**: `README.md` — replace minimal content with full project README

Existing docs structure:
```
docs/
  idempotent-event-processing.md  (exists — Epic 4)
  quickstart.md                   (to create — this story)
```

The README.md currently contains only `# Hexalith.Tenants` — full replacement is expected.

### Technology Stack Reference

| Technology | Version | Purpose |
|-----------|---------|---------|
| .NET SDK | 10.0.103 | Runtime and build |
| DAPR SDK | 1.17.3 | State store, pub/sub, actors |
| .NET Aspire | 13.1.x | Local development topology |
| Keycloak | (via Aspire) | JWT authentication for development |
| Redis | (via Aspire) | DAPR state store backend |
| xUnit | 2.9.3 | Testing framework |
| FluentValidation | 12.1.1 | Command validation |
| MediatR | 14.0.0 | Command/query pipeline |
| OpenTelemetry | 1.15.x | Observability |

### Git Intelligence

Recent commits show:
- Story 7.2 (telemetry) is done — health checks and OpenTelemetry are implemented
- Story 7.1 (Aspire AppHost) is done — full topology working
- EventStore submodule was recently updated
- All prior epics (1-7) have established the complete codebase

The documentation should reflect the fully implemented system state.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Epic 8] — Story 8.1 acceptance criteria and FR mapping
- [Source: _bmad-output/planning-artifacts/architecture.md#API & Communication Patterns] — Command API endpoint format, query endpoints
- [Source: _bmad-output/planning-artifacts/architecture.md#Authentication & Security] — JWT Bearer auth, bootstrap mechanism
- [Source: _bmad-output/planning-artifacts/architecture.md#Cross-Cutting Concerns] — `system` tenant context, identity mapping
- [Source: _bmad-output/planning-artifacts/prd.md#FR59-FR60] — Quickstart guide requirements and prerequisite validation
- [Source: Hexalith.EventStore/docs/getting-started/quickstart.md] — EventStore quickstart pattern to follow
- [Source: src/Hexalith.Tenants.CommandApi/Program.cs] — Actual DI registration and pipeline
- [Source: src/Hexalith.Tenants.CommandApi/appsettings.json] — Domain service registration and config structure

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
