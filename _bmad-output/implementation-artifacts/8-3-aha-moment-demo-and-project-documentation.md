# Story 8.3: "Aha Moment" Demo & Project Documentation

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer or decision-maker evaluating Hexalith.Tenants,
I want a compelling demo showing reactive cross-service access revocation and complete project documentation,
So that I can see the value of event-sourced tenant management in under 2 minutes and understand how to contribute.

## Acceptance Criteria

1. **Given** the "aha moment" demo artifact exists (reproducible script + step-by-step walkthrough)
   **When** a viewer runs the demo
   **Then** it demonstrates in under 2 minutes: create a tenant, add a user with TenantContributor role, show the Sample subscribing service receiving the UserAddedToTenant event, remove the user, watch the Sample service log access revocation automatically, and query the event history showing the full audit trail

2. **Given** the demo
   **When** a developer wants to reproduce it locally
   **Then** a `docs/demo.md` walkthrough and a `scripts/demo.ps1` / `scripts/demo.sh` script are provided to automate the multi-service scenario using the AppHost

3. **Given** CHANGELOG.md exists
   **When** a developer inspects it
   **Then** it follows Keep a Changelog format with an initial release entry documenting MVP capabilities

4. **Given** CONTRIBUTING.md exists
   **When** a developer reads it
   **Then** it includes: development setup instructions, branch naming conventions (`feat/`, `fix/`, `docs/`), PR process, test requirements (Tier 1+2 must pass), and code style reference (`.editorconfig`)

## Tasks / Subtasks

- [x] Task 0: PREREQUISITE — Verify command payload field casing and enum format (MUST complete before writing ANY JSON examples)
  - [x] 0.1: **CRITICAL GATE**: The quickstart (`docs/quickstart.md`) uses PascalCase payload fields (`"TenantId"`, `"UserId"`) and **integer enums** (`"Role": 1` for TenantContributor). However, Story 8.2 notes suggest `JsonStringEnumConverter` may cause string serialization in events. The dev agent MUST verify: (a) What casing does the command payload deserializer accept? (b) Does `AddUserToTenant` accept `"Role": "TenantContributor"` (string) or `"Role": 1` (integer) or both? Inspect the `CommandsController` deserialization path and test against the running AppHost. Match the quickstart's verified format exactly. This determination affects EVERY JSON example in the demo and scripts. Do NOT proceed to Task 1.5+ until resolved.

- [x] Task 1: Create `docs/demo.md` — "Aha Moment" demo walkthrough (AC: #1, #2)
  - [x] 1.1: Write introduction explaining the demo's purpose: prove reactive cross-service access revocation. State that this demo uses the AppHost topology which launches Hexalith.Tenants + Sample consuming service + DAPR sidecars + Redis. Clarify timing: "The demo sequence (Steps 1-6) takes under 2 minutes once the topology is running. Initial one-time setup (AppHost startup, JWT generation) is separate preparation — allow 5-10 minutes on first run."
  - [x] 1.2: Write prerequisites section (same prerequisites as quickstart: .NET 10, DAPR CLI + `dapr init`, Docker running)
  - [x] 1.3: Write "Start the Topology" section: `dotnet run --project src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj`. Note: first run pulls Docker images (allow 5-10 minutes). Wait for Aspire dashboard to show both `commandapi` and `sample` as running. **Include explicit instructions**: "In the terminal output, look for a line like `Login to the dashboard at https://localhost:17225/login?t=...` — open this URL in your browser. This is the Aspire dashboard. You will use it throughout the demo to find service URLs and view logs."
  - [x] 1.3a: Write a "Find Your Service URLs" sub-section immediately after startup: instruct the developer to click `commandapi` in the Aspire dashboard to find Hexalith.Tenants base URL (e.g., `https://localhost:{port}`), then click `sample` to find the Sample service base URL. Note these URLs for the remaining steps. Explain: "Aspire assigns ports dynamically — your ports will differ from the examples below. Replace `{commandapi-url}` and `{sample-url}` with your actual URLs throughout this guide."
  - [x] 1.4: Write JWT token acquisition step — reuse the exact same approach from `docs/quickstart.md` (dev signing key scripts for PowerShell and bash). Link to quickstart rather than duplicating the full JWT generation instructions — just include a brief "Generate a JWT token following the [Quickstart JWT section](quickstart.md#step-3-get-a-jwt-token)" reference and show the resulting `$TOKEN` / `TOKEN` variable. **Verify**: the JWT claims structure in the demo scripts must produce the same token format that the quickstart scripts produce — any mismatch causes `401 Unauthorized`
  - [x] 1.5: Write the 6-step demo sequence as a numbered walkthrough with command payloads. Use the casing and enum format verified in Task 0.
    **Before Step 1**, include a Swagger UI setup instruction: "Open `{commandapi-url}/swagger` in your browser. Click the **Authorize** button (top right), enter `Bearer {your-JWT-token}` (the token from the previous step), and click **Authorize**. You are now authenticated for all subsequent requests."
    For each step, include:
    - The JSON command body for `POST /api/v1/commands` — paste into the Swagger UI request body (do NOT include curl examples in demo.md; the automation scripts in `scripts/` handle programmatic HTTP)
    - What to observe in the Sample service logs — always specify: "In the Aspire dashboard, click `sample` → Logs tab"
    - What to observe in Hexalith.Tenants response

    **Step 1: Bootstrap Global Admin**
    ```json
    {
        "tenant": "system",
        "domain": "tenants",
        "aggregateId": "global-administrators",
        "commandType": "BootstrapGlobalAdmin",
        "payload": { "UserId": "demo-admin" }
    }
    ```
    - Observe: Hexalith.Tenants returns `202 Accepted`. No Sample service log entry (GlobalAdmin events don't trigger sample handlers)

    **Step 2: Create a Tenant**
    ```json
    {
        "tenant": "system",
        "domain": "tenants",
        "aggregateId": "acme-demo",
        "commandType": "CreateTenant",
        "payload": { "TenantId": "acme-demo", "Name": "Acme Demo Corp", "Description": "Demo tenant for aha moment" }
    }
    ```
    - Observe: Hexalith.Tenants returns `202 Accepted` with TenantCreated event
    > **Re-running the demo?** If you've run this before, `BootstrapGlobalAdmin` will return `GlobalAdminAlreadyBootstrappedRejection` (safe to ignore — bootstrap already done) and `CreateTenant` will return `TenantAlreadyExistsRejection`. Use a different tenant ID (e.g., `acme-demo-2`) and matching `aggregateId`.

    **Step 3: Add a User with TenantContributor Role**
    Use the enum format verified in Task 0 (integer or string).
    ```json
    {
        "tenant": "system",
        "domain": "tenants",
        "aggregateId": "acme-demo",
        "commandType": "AddUserToTenant",
        "payload": { "TenantId": "acme-demo", "UserId": "jane-doe", "Role": 1 }
    }
    ```
    > **Note**: Use the enum format verified in Task 0. The quickstart uses `"Role": 1` (integer: 0=TenantOwner, 1=TenantContributor, 2=TenantReader). If Task 0 confirms string format also works, prefer string for readability.
    - Observe (Aspire dashboard → `sample` → Logs): `[Sample] User jane-doe added to tenant acme-demo with role TenantContributor`

    **Step 4: Verify Access in Sample Service**
    - **Wait for the log**: Before checking the endpoint, confirm you see the `[Sample] User jane-doe added to tenant acme-demo...` log message in the Aspire dashboard → `sample` → Logs. This confirms the event has been processed by the local projection
    - Open `{sample-url}/access/acme-demo/jane-doe` in your browser (find `{sample-url}` from the Aspire dashboard → `sample` → Endpoints)
    - Observe: `{ "tenantId": "acme-demo", "userId": "jane-doe", "access": "granted", "role": "TenantContributor" }`

    **Step 5: Remove the User — THE AHA MOMENT**
    ```json
    {
        "tenant": "system",
        "domain": "tenants",
        "aggregateId": "acme-demo",
        "commandType": "RemoveUserFromTenant",
        "payload": { "TenantId": "acme-demo", "UserId": "jane-doe" }
    }
    ```
    - Observe (Aspire dashboard → `sample` → Logs): `[Sample] User jane-doe REMOVED from tenant acme-demo — revoking access`
    - Observe: `{sample-url}/access/acme-demo/jane-doe` now returns `{ "access": "denied", "reason": "User is not a member" }`
    - **THIS IS THE AHA MOMENT**: The consuming service automatically revoked access — no custom integration code, no polling, no webhook. Just a DAPR pub/sub event subscription

    **Step 6: Verify Current State and Understand the Audit Trail**
    - Open `{commandapi-url}/api/tenants/acme-demo` in the browser or via Swagger UI
    - Observe: Tenant details showing the current state — the tenant exists with an empty members list (jane-doe was added then removed)
    - **Audit trail note**: The query endpoint shows the CURRENT projection state, not the event history. The full audit trail — `TenantCreated` → `UserAddedToTenant` → `UserRemovedFromTenant` with timestamps and actor IDs — lives in the event store. In the event-sourced model, no state change is ever lost: the add, the remove, who did it, and when are all preserved as immutable events. For audit queries by date range, see `GET /api/tenants/{tenantId}/audit` (FR29). For more on temporal auditability, see [Event Contract Reference](event-contract-reference.md)

  - [x] 1.6: Write a "What Just Happened?" section explaining the architecture behind the demo:
    - Hexalith.Tenants processed the command and stored events atomically
    - Events were published asynchronously via DAPR pub/sub to the `system.tenants.events` topic
    - The Sample service received the event via its subscription endpoint
    - The Sample's `SampleLoggingEventHandler` logged the event
    - The Sample's local projection (`ITenantProjectionStore`) was updated automatically
    - The `/access` endpoint reads from the local projection — no calls back to Hexalith.Tenants
    - **Multi-service note**: This demo shows one subscribing service for simplicity. In production, any number of services can subscribe to the same `system.tenants.events` topic — each would independently receive the `UserRemovedFromTenant` event and revoke access in its own local projection simultaneously. The PRD envisions this with Parties, Billing, and Reporting services all reacting to the same event. The architecture supports this with zero additional configuration — each new subscriber just adds `AddHexalithTenants()` and a DAPR pub/sub subscription
  - [x] 1.7: Write a "Next Steps" section linking to: quickstart.md, event-contract-reference.md, the sample source code at `samples/Hexalith.Tenants.Sample/`
  - [x] 1.8: Write a "Troubleshooting" section at the end of demo.md covering:
    - **HTTPS certificate errors**: Aspire assigns HTTPS URLs with development certificates that are not trusted by default. For `curl`, add `-k` or `--insecure` flag. For PowerShell `Invoke-RestMethod`, add `-SkipCertificateCheck`. For browsers, click through the certificate warning. Alternatively, check if the Aspire dashboard shows HTTP endpoints alongside HTTPS
    - **`TenantAlreadyExists` on re-run**: If running the demo a second time, use a different tenant ID (e.g., `acme-demo-2`) or expect `TenantAlreadyExistsRejection` and `GlobalAdminAlreadyBootstrappedRejection` — these are correct behavior
    - **`401 Unauthorized`**: JWT token expired or claims mismatch — regenerate using the quickstart instructions
    - **Access endpoint returns "not a member" immediately after AddUserToTenant**: Event propagation is asynchronous — wait 1-2 seconds for the event to reach the Sample service's local projection before checking `/access`
    - **Access endpoint returns 500 error**: The local projection uses DAPR state store (Redis). Verify Redis is running: `docker ps | grep redis`. If Redis crashed, restart the AppHost
    - **First command fails with connection error (not 401/4xx)**: DAPR sidecars take a few seconds to initialize after the AppHost starts. Wait 10-15 seconds and retry. Check sidecar status in the Aspire dashboard — each service should show its sidecar as "Running"

- [x] Task 2: Create demo automation scripts (AC: #2)
  - [x] 2.1: Create `scripts/demo.ps1` (PowerShell) — automated demo script that:
    - Generates a JWT token using the dev signing key (same approach as quickstart — verify the claims structure produces the same token that the quickstart scripts produce)
    - Sends the 6 commands sequentially via `Invoke-RestMethod` to Hexalith.Tenants
    - Includes `Start-Sleep -Seconds 2` between steps to allow event propagation and visual observation
    - Queries the Sample service `/access` endpoint to show access grant then revocation
    - Prints clear step headers and colored output so the terminal itself IS the demo
    - **IMPORTANT**: The script assumes the AppHost is already running (it does not start it). Print a prerequisite check at the start: attempt to reach Hexalith.Tenants health endpoint and fail fast with a clear message if unreachable
    - Use realistic unique IDs to avoid conflicts on re-run (append timestamp or GUID suffix to tenant ID and user ID, e.g., `acme-demo-{timestamp}`)
    - Use the enum format verified in Task 0 for the `Role` field
    - **HTTPS handling**: Use `-SkipCertificateCheck` on all `Invoke-RestMethod` calls — Aspire dev certificates are not trusted by default
  - [x] 2.2: Create `scripts/demo.sh` (bash) — same automated demo script for bash/macOS/Linux:
    - Uses `curl` and `jq` (note jq as optional — graceful fallback to raw JSON)
    - **HTTPS handling**: Use `curl -k` (or `--insecure`) on all requests — Aspire dev certificates are not trusted by default
    - Same sequential flow with `sleep 2` between steps
    - Same prerequisite check and unique ID generation
    - Use the enum format verified in Task 0
  - [x] 2.3: Both scripts **MUST** accept `--base-url` (or `$COMMANDAPI_URL` env var) and `--sample-url` (or `$SAMPLE_URL` env var) as **required parameters with no defaults**. Aspire assigns ports dynamically — hardcoded defaults WILL break. If the user runs the script without providing URLs, print: "ERROR: --base-url and --sample-url are required. Find your service URLs in the Aspire dashboard (typically https://localhost:17225). Example: ./demo.sh --base-url https://localhost:7234 --sample-url https://localhost:7235"
  - [x] 2.4: Both scripts print a summary at the end showing: number of commands sent, access state transitions verified (granted → denied), demo cycle completed

- [x] Task 3: Update README.md with demo link (AC: #1)
  - [x] 3.1: Replace the `<!-- TODO: Story 8.3 — Add "aha moment" demo GIF/link here -->` placeholder with a "See It In Action" section linking to `docs/demo.md` and mentioning the automation scripts in `scripts/`
  - [x] 3.2: Write a brief 3-4 sentence description of what the demo shows (reactive access revocation across services)

- [x] Task 4: Create CHANGELOG.md (AC: #3)
  - [x] 4.1: Create `CHANGELOG.md` following [Keep a Changelog](https://keepachangelog.com/) format
  - [x] 4.2: Add an `[Unreleased]` section at the top (standard practice for pre-release)
  - [x] 4.3: Add a `[0.1.0] - YYYY-MM-DD` initial release entry (use the actual date when the first `v0.1.0` tag is created — do NOT hardcode today's date, as the release may happen later) with categorized items:
    **Added:**
    - Tenant lifecycle management (Create, Update, Disable, Enable) via event-sourced TenantAggregate
    - User-role management with three roles (TenantOwner, TenantContributor, TenantReader)
    - Global administrator management with bootstrap mechanism
    - Tenant key-value configuration with namespace conventions and limits
    - Event-driven integration via DAPR pub/sub (CloudEvents 1.0)
    - Tenant discovery and query endpoints with cursor-based pagination
    - In-memory testing fakes with production-parity domain logic
    - .NET Aspire hosting extensions and AppHost topology
    - OpenTelemetry instrumentation for command and event processing
    - Comprehensive documentation: quickstart guide, event contract reference, cross-aggregate timing, compensating commands
    - CI/CD pipeline with GitHub Actions (build, test, NuGet publish)
    - Sample consuming service demonstrating event subscription and access enforcement
  - [x] 4.4: Add footer links section: `[Unreleased]` and `[0.1.0]` linking to GitHub compare/tag URLs using the `Hexalith/Hexalith.Tenants` repository

- [x] Task 5: Create CONTRIBUTING.md (AC: #4)
  - [x] 5.1: Write "Getting Started" section with development setup:
    - Prerequisites: .NET 10 SDK, DAPR CLI + `dapr init` (**full init, NOT `--slim`** — the Aspire topology requires the full DAPR runtime with placement service for actors), Docker
    - Clone with `--recurse-submodules` (EventStore submodule)
    - Build: `dotnet build Hexalith.Tenants.slnx --configuration Release`
    - Test: `dotnet test Hexalith.Tenants.slnx`
    - Run locally: `dotnet run --project src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj`
    - Windows long path note: `git config --system core.longpaths true`
  - [x] 5.2: Write "Branch Naming Conventions" section:
    - `feat/<description>` — New features
    - `fix/<description>` — Bug fixes
    - `docs/<description>` — Documentation changes
    - `refactor/<description>` — Code refactoring
    - `test/<description>` — Test additions or changes
  - [x] 5.3: Write "Pull Request Process" section:
    - Create a branch from `main`
    - Make changes and commit with descriptive messages
    - Ensure all Tier 1 and Tier 2 tests pass locally before submitting
    - Open a PR against `main` with a description of changes
    - CI will run automatically — PR must pass before merge
    - PRs require at least one approval
  - [x] 5.4: Write "Test Requirements" section:
    - All PRs must pass Tier 1 (unit) and Tier 2 (DAPR integration) tests
    - New domain logic requires Tier 1 tests with 100% branch coverage on authorization paths
    - Test framework: xUnit + Shouldly + NSubstitute
    - Coverage collected via coverlet (> 80% line coverage target)
  - [x] 5.5: Write "Code Style" section:
    - Code style enforced via `.editorconfig` (inherited from EventStore conventions)
    - Key conventions: file-scoped namespaces, Allman braces, `_camelCase` private fields, 4-space indentation, warnings as errors
    - Run `dotnet format` before committing to auto-fix formatting issues
  - [x] 5.6: Write "Submodule Management" section — contributors need to know how to work with the `Hexalith.EventStore` git submodule:
    - Initial clone: `git clone --recurse-submodules` (already in Getting Started)
    - Pulling submodule updates: `git submodule update --init --recursive` after pulling main
    - When the submodule reference changes in a PR: `git submodule update` to sync
    - Note: Do NOT modify files inside `Hexalith.EventStore/` directly — changes to the submodule must go through the EventStore repository
  - [x] 5.7: Write "Project Structure" section — brief overview referencing the README structure table
  - [x] 5.8: Write "Reporting Issues" section — link to GitHub Issues

- [x] Task 6: Validation
  - [x] 6.1: Verify `docs/demo.md` is well-formed markdown with language-annotated code blocks
  - [x] 6.2: Verify all command payloads in demo.md use the same format as `docs/quickstart.md` — same field names, same casing, same envelope structure, same enum format (integer vs string for Role). Any mismatch = 400 Bad Request for the developer following the demo
  - [x] 6.3: Verify `scripts/demo.ps1` and `scripts/demo.sh` produce JWT tokens with the exact same claims structure as the quickstart scripts. Specifically verify: `sub`, `iss` (`hexalith-dev`), `aud` (`hexalith-tenants`), `tenants` array (`["system"]`), `exp` — any mismatch causes `401 Unauthorized`
  - [x] 6.4: Verify CHANGELOG.md follows Keep a Changelog format (https://keepachangelog.com/). `[Unreleased]` section should be empty (staging area for future changes)
  - [x] 6.5: Verify CONTRIBUTING.md references correct paths, test commands, and tools
  - [x] 6.6: Verify README.md demo placeholder is replaced with actual content
  - [x] 6.7: Verify cross-references between docs are valid (demo.md → quickstart.md, CONTRIBUTING.md → .editorconfig)
  - [x] 6.8: Verify `scripts/` directory is created (new directory — does not exist yet)

## Dev Notes

### Architecture Context

This is a **documentation + scripting** story — no C# code changes. The deliverables are:
- `docs/demo.md` — Step-by-step "aha moment" demo walkthrough
- `scripts/demo.ps1` — PowerShell automation script
- `scripts/demo.sh` — Bash automation script
- `CHANGELOG.md` — Keep a Changelog format
- `CONTRIBUTING.md` — Developer contribution guide
- `README.md` update — Replace demo placeholder with link

The demo leverages the existing AppHost topology which already includes both Hexalith.Tenants and the Sample consuming service with DAPR sidecars. No new services or code changes are needed — the demo simply drives the existing system through its paces.

### What Already Exists (DO NOT Recreate)

| Component | Path | Relevance |
|-----------|------|-----------|
| AppHost topology | `src/Hexalith.Tenants.AppHost/Program.cs` | Launches Hexalith.Tenants + Sample + DAPR sidecars + Redis — already includes the Sample service in the topology |
| Sample consuming service | `samples/Hexalith.Tenants.Sample/` | Already handles `UserAddedToTenant`, `UserRemovedFromTenant`, `TenantDisabled` events via `SampleLoggingEventHandler` |
| Sample access check endpoint | `samples/Hexalith.Tenants.Sample/Endpoints/AccessCheckEndpoints.cs` | `GET /access/{tenantId}/{userId}` — queries local projection for access enforcement |
| Sample logging handler | `samples/Hexalith.Tenants.Sample/Handlers/SampleLoggingEventHandler.cs` | Logs `[Sample] User {UserId} added/REMOVED...` messages visible in Aspire dashboard |
| Quickstart guide | `docs/quickstart.md` | JWT generation approach, command format, troubleshooting — link to it, do NOT duplicate |
| Event contract reference | `docs/event-contract-reference.md` | Event schemas — link to it from demo doc |
| README with placeholder | `README.md:24` | Contains `<!-- TODO: Story 8.3 -->` placeholder to replace |
| Dev JWT config | `src/Hexalith.Tenants/appsettings.Development.json` | Issuer: `hexalith-dev`, Audience: `hexalith-tenants`, HMAC-SHA256 signing key: `this-is-a-development-signing-key-minimum-32-chars` |
| Command endpoint | `POST /api/v1/commands` | EventStore's CommandsController route (verified in Story 8.1) |
| Query endpoint | `GET /api/tenants/{tenantId}` | TenantsQueryController (verified in Story 8.1) |
| CI workflow | `.github/workflows/ci.yml` | Existing CI pipeline |
| Release workflow | `.github/workflows/release.yml` | Existing release pipeline |
| License | `LICENSE` | MIT license file exists |
| .editorconfig | `.editorconfig` | Code style conventions exist |
| Hexalith.Tenants health endpoint | Built into Hexalith.Tenants via `MapDefaultEndpoints()` | Typically `/health` or `/alive` — verify by inspecting ServiceDefaults or hitting the endpoint. Used by demo scripts for prerequisite check |
| Sample health endpoint | `samples/Hexalith.Tenants.Sample/Program.cs:34` | Explicit `GET /health` returning `"healthy"` |

### Critical Patterns to Follow

**Command Payload Format** (must match quickstart exactly):
```json
{
    "tenant": "system",
    "domain": "tenants",
    "aggregateId": "<target-aggregate-id>",
    "commandType": "<CommandTypeName>",
    "payload": { /* command-specific fields */ }
}
```

**JWT Token Generation** (dev signing key approach from quickstart):
- Issuer: `hexalith-dev`
- Audience: `hexalith-tenants`
- Signing key: `this-is-a-development-signing-key-minimum-32-chars` (HMAC-SHA256)
- Claims must include `tenants` array with `"system"` for EventStore's `ClaimsTenantValidator`
- Claims must include `sub` claim for the user ID (used as actor in command processing)
- **CRITICAL**: The JWT `sub` claim and the `BootstrapGlobalAdmin.UserId` MUST match. If `sub` is `"demo-admin"`, then `BootstrapGlobalAdmin.payload.UserId` must also be `"demo-admin"`. Otherwise the actor won't be recognized as a GlobalAdmin and subsequent commands will fail authorization
- The quickstart has working PowerShell and bash scripts for generating valid JWTs — the demo scripts should reuse or reference the same approach

**Sample Service Observable Behavior:**
- `SampleLoggingEventHandler` logs to `ILogger` at `Information` level for adds and `Warning` level for removals/disables
- Log format: `[Sample] User {UserId} added to tenant {TenantId} with role {Role}` / `[Sample] User {UserId} REMOVED from tenant {TenantId} — revoking access`
- These logs are visible in the Aspire dashboard under the `sample` resource → Logs tab
- The `/access/{tenantId}/{userId}` endpoint queries `ITenantProjectionStore` for real-time access checks

**AppHost Topology:**
- Hexalith.Tenants runs as `commandapi` with full DAPR sidecar (state store + pub/sub + actors)
- Sample runs as `sample` with DAPR sidecar (pub/sub only — no state store, subscriber only)
- Both services are started by `dotnet run --project src/Hexalith.Tenants.AppHost/`
- Service URLs are assigned dynamically by Aspire — check the dashboard for actual ports

### Dev Agent MUST-VERIFY Checklist (Before Writing JSON Examples)

These items have ambiguity that CANNOT be resolved from planning artifacts alone:

1. **Command payload field casing**: The quickstart uses PascalCase for payload fields (`"TenantId"`, `"UserId"`, `"Role"`) while the envelope uses camelCase (`"tenant"`, `"domain"`, `"aggregateId"`, `"commandType"`). Verify this is correct by inspecting the `CommandsController` deserialization path or testing against the running AppHost. The payload is deserialized into C# record properties which are PascalCase, but `System.Text.Json` with `camelCase` policy might expect camelCase. Match whatever the quickstart verified.

2. **Enum format in command payloads**: The quickstart (`docs/quickstart.md:232`) uses `"Role": 1` (integer) with a note: `0=TenantOwner, 1=TenantContributor, 2=TenantReader`. Story 8.2 notes that events serialize enums as strings due to `JsonStringEnumConverter`. But command DESERIALIZATION (input) may differ from event SERIALIZATION (output). Verify: does `AddUserToTenant` accept `"Role": "TenantContributor"` (string), `"Role": 1` (integer), or both? Test against the running AppHost. If both work, prefer string for demo readability but document integer as alternative. If only integer works, use integer (matching quickstart).

3. **Aspire dashboard URL**: The Aspire dashboard URL is printed in the terminal when the AppHost starts (typically `https://localhost:17225` but may vary). Verify the actual URL by running the AppHost and noting what's printed.

4. **`sub` claim ↔ BootstrapGlobalAdmin.UserId alignment**: Verify that when the JWT `sub` claim is `"demo-admin"` and `BootstrapGlobalAdmin.payload.UserId` is `"demo-admin"`, subsequent commands (CreateTenant, AddUserToTenant) succeed authorization. If the `sub` claim must match the bootstrapped admin's UserId for the actor to be recognized as GlobalAdmin, this must be documented explicitly. Test by bootstrapping with UserId `"demo-admin"` and sending CreateTenant with a JWT where `sub` = `"demo-admin"`.

5. **Hexalith.Tenants health endpoint path**: The demo scripts use a health check for prerequisite verification. Verify the actual health endpoint path — inspect `ServiceDefaults/Extensions.cs` for `MapDefaultEndpoints()` or `MapHealthChecks()`. Common paths: `/health`, `/healthz`, `/alive`. The Sample service uses `/health` explicitly.

### Anti-Patterns to Avoid

- **DO NOT** create a separate demo service or modify any C# code — the demo drives the EXISTING AppHost topology
- **DO NOT** duplicate JWT generation instructions from quickstart — link to it and reuse the approach
- **DO NOT** hardcode service ports in docs — they are dynamically assigned by Aspire. Use placeholders like `{commandapi-url}` and `{sample-url}` in the walkthrough, and note that actual URLs are visible in the Aspire dashboard. Scripts should accept URL parameters
- **DO NOT** make the demo dependent on Story 8.2 completion — the demo.md should not link to event-contract-reference.md as a prerequisite, only as a "learn more" reference
- **DO NOT** add a demo GIF to the repo — just document the reproducible walkthrough. GIF/video can be added later
- **DO NOT** include `curl -X POST` shell commands in demo.md for the demo steps — show the JSON body and reference Swagger UI as the primary path (matching quickstart pattern). The scripts handle the curl/Invoke-RestMethod automation separately
- **DO NOT** reference DAPR component YAML files — Aspire handles DAPR configuration automatically

### Project Structure Notes

Files to create:
- **CREATE**: `docs/demo.md` — "Aha moment" demo walkthrough (FR63)
- **CREATE DIRECTORY**: `scripts/` — New directory (does not exist yet)
- **CREATE**: `scripts/demo.ps1` — PowerShell demo automation script
- **CREATE**: `scripts/demo.sh` — Bash demo automation script (make executable: `chmod +x`)
- **CREATE**: `CHANGELOG.md` — Keep a Changelog format
- **CREATE**: `CONTRIBUTING.md` — Developer contribution guide

Files to modify:
- **MODIFY**: `README.md` — Replace line 24 demo placeholder with actual demo link

Directory structure after this story:
```
Hexalith.Tenants/
  CHANGELOG.md          (to create — this story)
  CONTRIBUTING.md       (to create — this story)
  README.md             (to modify — replace placeholder)
  docs/
    demo.md             (to create — this story)
    quickstart.md       (exists — Story 8.1)
    event-contract-reference.md  (exists — Story 8.2)
    cross-aggregate-timing.md    (exists — Story 8.2)
    compensating-commands.md     (exists — Story 8.2)
    idempotent-event-processing.md (exists — Epic 4)
  scripts/
    demo.ps1            (to create — this story)
    demo.sh             (to create — this story)
```

### Technology Stack Reference

| Technology | Version | Relevance |
|-----------|---------|-----------|
| .NET SDK | 10.0.103 | Runtime for AppHost and services |
| DAPR SDK | 1.17.3 | Pub/sub event delivery between Hexalith.Tenants and Sample |
| .NET Aspire | 13.1.x | AppHost topology orchestration |
| System.Text.Json | .NET 10 built-in | JSON serialization for command payloads |
| PowerShell | 7.x | Demo script (cross-platform) |
| bash | 4.x+ | Demo script (Linux/macOS) |

### Previous Story Intelligence

**Story 8.1 (Quickstart Guide & README)** — Done. Key learnings:
- JWT uses dev signing key (not Keycloak) — issuer `hexalith-dev`, audience `hexalith-tenants`, HMAC-SHA256
- Command endpoint: `POST /api/v1/commands`
- Query endpoint: `GET /api/tenants/{tenantId}`
- BootstrapGlobalAdmin must precede tenant commands
- The quickstart includes both PowerShell and bash JWT generation scripts that produce valid tokens
- README has a `<!-- TODO: Story 8.3 -->` placeholder at line 24

**Story 8.2 (Event Contract Reference & Technical Documentation)** — In progress (tasks 5-9 remaining). Key learnings:
- Three docs created: `event-contract-reference.md`, `cross-aggregate-timing.md`, `compensating-commands.md`
- Docs are partially complete but exist as files
- The demo should not depend on 8.2 completion — link as "learn more" references only

**AppHost Topology** — The AppHost already includes the Sample consuming service in its topology (added during Epic 4 / Story 4.3). The Sample service:
- Registers `SampleLoggingEventHandler` for `UserAddedToTenant`, `UserRemovedFromTenant`, `TenantDisabled`
- Has an `/access/{tenantId}/{userId}` endpoint that queries the local projection
- Uses `AddHexalithTenants()` for DI registration (12 lines of DI config in Program.cs)
- The topology works end-to-end — events flow from Hexalith.Tenants through DAPR pub/sub to Sample

### Git Intelligence

Recent commits show:
- Story 8.1 created quickstart.md and updated README.md (commit `5810018`)
- Story 8.2 is in progress with docs partially created (commit `e8becc9`)
- The codebase is stable with all Epics 1-7 done
- No code changes are needed for this documentation story
- The AppHost already includes Sample in its topology

### Demo Script Technical Notes

All technical details for script implementation are covered in the **MUST-VERIFY Checklist** (items 1-5), **Critical Patterns to Follow** (JWT, command format), and **Task 2** subtasks. Key cross-references:
- JWT claims structure → "Critical Patterns to Follow" → JWT Token Generation
- Service URL discovery → Task 2.3 (required params, no defaults)
- Enum format → Task 0 + MUST-VERIFY #2
- HTTPS certificate handling → Tasks 2.1/2.2

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 8.3] — Acceptance criteria
- [Source: _bmad-output/planning-artifacts/prd.md#FR63] — "Aha moment" demo requirement
- [Source: _bmad-output/planning-artifacts/prd.md#Innovation — The "Aha Moment" Demo] — 6-step demo sequence specification
- [Source: _bmad-output/planning-artifacts/prd.md#Journey 1] — Alex's evaluation journey and peer validation
- [Source: _bmad-output/planning-artifacts/architecture.md#API & Communication Patterns] — Command endpoint format
- [Source: _bmad-output/planning-artifacts/architecture.md#Project Structure] — Complete directory layout
- [Source: src/Hexalith.Tenants.AppHost/Program.cs] — AppHost topology with Sample service
- [Source: samples/Hexalith.Tenants.Sample/Program.cs] — Sample DI registration and endpoint mapping
- [Source: samples/Hexalith.Tenants.Sample/Handlers/SampleLoggingEventHandler.cs] — Event handler log messages
- [Source: samples/Hexalith.Tenants.Sample/Endpoints/AccessCheckEndpoints.cs] — Access check endpoint implementation
- [Source: docs/quickstart.md] — JWT generation approach, command format
- [Source: _bmad-output/implementation-artifacts/8-1-quickstart-guide-and-readme.md] — Story 8.1 dev notes and verified paths
- [Source: _bmad-output/implementation-artifacts/8-2-event-contract-reference-and-technical-documentation.md] — Story 8.2 verified record definitions

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (1M context)

### Debug Log References

- Task 0 verification: Inspected `EventStoreAggregate.DispatchCommandAsync` (line 142) — uses `JsonSerializer.Deserialize(command.Payload, handleInfo.CommandType)` with default options (PascalCase, integer enums)
- `TenantRole` enum at `src/Hexalith.Tenants.Contracts/Enums/TenantRole.cs` — no `JsonStringEnumConverter` attribute
- Health endpoint confirmed at `/health` via `src/Hexalith.Tenants.ServiceDefaults/Extensions.cs:21`

### Completion Notes List

- Task 0: Verified command payload uses PascalCase for payload fields (`TenantId`, `UserId`, `Role`) and integer enum format (`"Role": 1`). Default `JsonSerializerOptions` used for deserialization. Matches quickstart exactly.
- Task 1: Created `docs/demo.md` — complete "aha moment" walkthrough with 6-step demo sequence, Swagger UI instructions, dynamic URL placeholders, architecture explanation ("What Just Happened?"), next steps, and comprehensive troubleshooting section.
- Task 2: Created `scripts/demo.ps1` (PowerShell) and `scripts/demo.sh` (bash). Both scripts: generate JWT tokens matching quickstart claims, require `--base-url`/`--sample-url` with no defaults, use unique timestamp-suffixed IDs, include health check prerequisites, print colored output, handle HTTPS certificates (`-SkipCertificateCheck`/`-k`), and show summary with command count and access transitions.
- Task 3: Updated `README.md` — replaced `<!-- TODO: Story 8.3 -->` placeholder with "See It In Action" section linking to demo.md and scripts/.
- Task 4: Created `CHANGELOG.md` — Keep a Changelog format with `[Unreleased]` section and `[0.1.0] - YYYY-MM-DD` initial release entry documenting all MVP capabilities. Footer links to GitHub compare/tag URLs.
- Task 5: Created `CONTRIBUTING.md` — Getting Started (prerequisites, clone with submodules, build, test, run), Branch Naming Conventions, PR Process, Test Requirements, Code Style (.editorconfig reference), Submodule Management, Project Structure reference, Reporting Issues.
- Task 6: All validations pass — well-formed markdown, payload formats match quickstart, JWT claims identical across all files, CHANGELOG follows Keep a Changelog format, cross-references valid, README placeholder replaced, scripts/ directory created.

### File List

- `docs/demo.md` (NEW) — "Aha Moment" demo walkthrough
- `scripts/demo.ps1` (NEW) — PowerShell demo automation script
- `scripts/demo.sh` (NEW) — Bash demo automation script
- `CHANGELOG.md` (NEW) — Keep a Changelog format
- `CONTRIBUTING.md` (NEW) — Developer contribution guide
- `README.md` (MODIFIED) — Replaced demo placeholder with "See It In Action" section

### Change Log

- 2026-03-19: Implemented Story 8.3 — created demo walkthrough, automation scripts, CHANGELOG, CONTRIBUTING, and updated README (all 6 tasks + validation complete)
