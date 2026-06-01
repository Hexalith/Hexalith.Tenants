---
created: 2026-06-01
source_story_key: 8-4-produce-the-reactive-access-aha-moment-demo
baseline_commit: 54384f8
---

# Story 8.4: Produce the Reactive Access "Aha Moment" Demo

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer evaluating Tenants,
I want a concise demo that shows access revocation propagating through subscribing services,
so that I can understand the event-driven value without reading the full architecture.

## Acceptance Criteria

1. Given the demo starts from a clean or documented local setup, when a tenant is created and a user is added with a tenant role, then the demo shows subscribing services receiving the add-user event, and each service updates its local access state from the tenant event stream.
2. Given the user is removed from the tenant, when the remove event is published, then the demo shows all subscribing services revoking or denying local access based on their projections, and no custom polling or manual synchronization job is used.
3. Given the demo presents the event history, when the viewer inspects the result, then it shows an audit trail of who acted, what changed, and when, and it avoids exposing raw payloads, tokens, secrets, or sensitive user data.
4. Given demo narration or written steps explain the behavior, when the viewer follows along, then the demo makes clear that subscribers are eventually consistent, and it references the planned synchronous authorization plugin only as a future option where appropriate.
5. Given demo assets are reviewed, when they are included in docs or README, then the asset length, commands, package names, and visual output support the 90-second proof goal, and stale or misleading demo steps are flagged for update.

## Tasks / Subtasks

- [x] Audit the existing demo artifacts and decide the minimal source-backed demo scope. (AC: 1, 2, 5)
  - [x] Treat `docs/demo.md`, `scripts/demo.sh`, and `scripts/demo.ps1` as prior repo state that must be verified, corrected, and validated; do not assume they satisfy Story 8.4 because sprint status still has this story in backlog at creation time.
  - [x] Preserve the current Epic 8 split: Story 8.4 owns demo production only; Story 8.5 owns deeper cross-aggregate timing docs; Story 8.6 owns compensating-command guidance.
  - [x] Resolve the plural-subscriber language honestly. The current runnable AppHost includes one sample subscriber resource named `sample`. Do not claim three live services unless implementation adds and validates additional subscriber resources. If the demo remains one runnable subscriber, phrase it as "the configured subscribing service" plus an architecture note that additional services subscribe the same way.
  - [x] Keep the first-run setup separate from the timed proof. The proof goal is about the add-user to remove-user access transition after AppHost, URLs, and auth are ready.

- [x] Correct and source-back `docs/demo.md`. (AC: 1, 2, 3, 4, 5)
  - [x] Keep the demo entry point at `docs/demo.md`, linked from `README.md`, and keep it focused on the 90-second reactive access proof.
  - [x] Align all command examples with the current EventStore command gateway route `POST /api/v1/commands` and status route `GET /api/v1/commands/status/{correlationId}`.
  - [x] Ensure command examples use concrete ULID-shaped `messageId` values and valid command domains: `BootstrapGlobalAdmin` uses domain and aggregate ID `global-administrators`; tenant commands use domain `tenants` with `aggregateId == payload.TenantId`.
  - [x] Keep `AddUserToTenant` role examples readable and deserializable with current contracts. `TenantRole` is serialized by name, so prefer `"TenantContributor"` where examples are parsed as JSON command payloads.
  - [x] Explain current local auth accurately: default AppHost uses Keycloak and the `hexalith-eventstore` audience; HMAC tokens are only for the intentional `EnableKeycloak=false` fallback.
  - [x] Make the observation path explicit: use Aspire dashboard resource endpoints, Sample service logs, `/access/{tenantId}/{userId}`, and command status/query endpoints as proof; raw logs are supporting evidence, not the only success signal.
  - [x] Show audit evidence through supported query surfaces: current tenant state via `GET /api/tenants/{tenantId}` and audit rows via `GET /api/tenants/{tenantId}/audit` when projection data is available. Do not imply the current details endpoint is raw event history.
  - [x] Avoid raw bearer tokens, decoded JWT payloads, secrets, full serialized event payloads, or real tenant/user data in docs or screenshots. Use redacted placeholders for tokens and stable synthetic IDs for tenants/users.
  - [x] Explain eventual consistency directly: EventStore is durable truth, the sample local projection catches up asynchronously from `tenants.events`, and `/access` reads local projection state without polling Tenants or EventStore.
  - [x] Mention the planned synchronous authorization plugin only as a future option for security-critical synchronous enforcement, not as current behavior.

- [x] Fix and harden the automated demo scripts. (AC: 1, 2, 3, 5)
  - [x] Keep `scripts/demo.sh` and `scripts/demo.ps1` as the automation entry points, but correct any command payload drift before promoting them.
  - [x] Fix the existing `BootstrapGlobalAdmin` script payloads: they currently send `domain: "tenants"` and must send `domain: "global-administrators"` with `aggregateId: "global-administrators"`.
  - [x] Replace non-ULID `messageId` values such as `demo-$TIMESTAMP-...` with valid 26-character ULIDs or another source-backed ULID generator. Invalid IDs fail EventStore validation.
  - [x] Do not generate an HMAC token unconditionally. Either accept a `--token`/`TOKEN` supplied from the quickstart Keycloak flow, add a `--keycloak-url` path that fetches the default local token, or clearly gate HMAC generation behind an explicit `--hmac-dev-token`/`EnableKeycloak=false` mode.
  - [x] Keep dynamic URL inputs required. Aspire assigns ports dynamically, so scripts must require EventStore and Sample base URLs or read them from documented environment variables.
  - [x] Add retry/wait behavior around the projection transition, not fixed sleeps alone. The script should poll `/access/{tenantId}/{userId}` until it sees granted after add and denied after remove, with bounded timeout and clear failure output.
  - [x] Print a compact, demo-friendly summary: commands accepted, correlation IDs/status URLs, access transition `granted -> denied`, and whether audit/query evidence was observed.
  - [x] Keep script output support-safe: do not print raw JWTs, full event payloads, secrets, or sensitive user data beyond synthetic demo IDs.

- [x] Add source-backed demo documentation tests. (AC: 1, 2, 3, 4, 5)
  - [x] Add `tests/Hexalith.Tenants.Server.Tests/Documentation/AhaMomentDemoDocumentationTests.cs` or equivalent focused documentation tests in the existing documentation-test area.
  - [x] Verify `docs/demo.md` and both scripts reference the current AppHost path, `eventstore`, `sample`, `POST /api/v1/commands`, `/api/v1/commands/status/{correlationId}`, `/access/{tenantId}/{userId}`, `tenants.events`, and `MapTenantEventSubscription`.
  - [x] Parse every fenced JSON command in `docs/demo.md`; assert each command has a concrete ULID-shaped `messageId`, valid `tenant`, `domain`, `aggregateId`, `commandType`, and payload shape.
  - [x] Assert `BootstrapGlobalAdmin` examples and scripts use `global-administrators`, and assert tenant command examples keep `aggregateId` aligned with `payload.TenantId`.
  - [x] Verify docs/scripts do not contain JWT-like raw token values, `Authorization: Bearer ` with a real token, `client_secret`, production secrets, or guidance to log full event payloads.
  - [x] Verify docs/scripts distinguish default Keycloak auth from the `EnableKeycloak=false` HMAC fallback.
  - [x] Verify docs/scripts use eventual-consistency language and do not claim synchronous revocation, custom polling, manual synchronization jobs, or guaranteed cross-service ordering.
  - [x] Verify `README.md` links to `docs/demo.md`, and `docs/demo.md` links to the quickstart, event contract reference, sample walkthrough, idempotent processing, and cross-aggregate timing docs without duplicating their scope.

- [x] Validate executable behavior as far as the environment allows. (AC: 1, 2, 3, 5)
  - [x] Run focused documentation tests through `dotnet test` or the direct xUnit runner fallback used in Stories 8.1 through 8.3 if VSTest hits the sandbox socket limitation.
  - [x] Run focused sample/client tests that cover projection-driven access and subscription behavior: `samples/Hexalith.Tenants.Sample.Tests`, `tests/Hexalith.Tenants.Client.Tests/Subscription/TenantEventProcessorTests.cs`, and `TenantEventSubscriptionEndpointsTests.cs`.
  - [x] If Docker, DAPR, Keycloak, and Aspire are available, run the AppHost and execute one full scripted demo cycle against actual dynamic URLs. Record command status, access transition, and audit/query evidence.
  - [x] If live infrastructure is unavailable, record the exact missing prerequisite and do not claim live execution. Source-backed tests remain required; live proof is additional evidence.
  - [x] Update `_bmad-output/implementation-artifacts/tests/test-summary.md` only if this repository continues recording documentation/demo validation evidence there.

## Dev Notes

### Source Context

- Epic 8 objective: developers can follow a validated quickstart, understand event contracts, see the reactive access demo, and design for timing, idempotency, and compensating commands. Story 8.4 owns the reactive access "aha moment" demo. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 8: Developers Can Adopt Through Documentation and Demo Evidence`]
- Story 8.4 requires a clean or documented local setup, add-user event observation, remove-user revocation observation, audit evidence, eventual-consistency explanation, and reviewed demo assets that support the 90-second proof goal. [Source: `_bmad-output/planning-artifacts/epics.md#Story 8.4: Produce the Reactive Access "Aha Moment" Demo`]
- PRD FR63 requires an "aha moment" screencast or video showing reactive cross-service access revocation. The PRD sequence is create tenant, add user, show subscribers receiving `UserAddedToTenant`, remove user, watch subscribers revoke access, then query event history/audit. [Source: `_bmad-output/planning-artifacts/prd.md#The "Aha Moment" Demo`; `_bmad-output/planning-artifacts/prd.md#Documentation & Adoption`]
- Architecture maps Epic 8 adoption work to `docs/`, `README.md`, and the sample project. [Source: `_bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping`]

### Current Repository State

- `docs/demo.md` already exists and is linked from `README.md`, but Story 8.4 is still `backlog` at creation time. Treat existing demo artifacts as unaccepted prior work that must be audited and validated.
- `scripts/demo.sh` and `scripts/demo.ps1` already exist. They currently require EventStore and Sample URLs, check `/health`, send the add/remove flow, and summarize the access transition.
- Known drift in the existing scripts at story creation:
  - Both scripts send `BootstrapGlobalAdmin` with `domain: "tenants"` instead of `global-administrators`.
  - Both scripts generate `messageId` values like `demo-$TIMESTAMP-...`, but EventStore validates `messageId` as a 26-character ULID.
  - Both scripts generate HMAC development tokens unconditionally, while the default AppHost enables Keycloak and configures EventStore/Tenants for OIDC with audience `hexalith-eventstore`.
- `docs/demo.md` already explains Aspire dynamic ports, the Sample `/access` endpoint, eventual consistency, and current-state versus audit-history distinction. Verify every statement against source before keeping it.
- The AppHost currently starts one sample subscriber resource named `sample`. It wires EventStore AppId `eventstore`, Tenants AppId `tenants`, the Sample AppId `sample`, Keycloak, Redis-backed DAPR components, and shared pub/sub. [Source: `src/Hexalith.Tenants.AppHost/Program.cs`]
- The Sample project registers `AddHexalithTenants()`, three custom logging handlers, `UseCloudEvents()`, `MapSubscribeHandler()`, `MapTenantEventSubscription()`, `/access/{tenantId}/{userId}`, `/configuration/{tenantId}/sample`, `/alive`, and `/health`. [Source: `samples/Hexalith.Tenants.Sample/Program.cs`]
- `SampleLoggingEventHandler` intentionally logs tenant ID plus message/correlation metadata and does not log the sample user ID or role. Preserve that support-safe behavior in demo docs and scripts. [Source: `samples/Hexalith.Tenants.Sample/Handlers/SampleLoggingEventHandler.cs`; `samples/Hexalith.Tenants.Sample.Tests/Handlers/SampleLoggingEventHandlerTests.cs`]
- `AccessCheckEndpoints.CheckAccessAsync` reads `ITenantProjectionStore` and fails closed for missing tenants, disabled/unknown status, non-members, `TenantRole.Unknown`, and out-of-range roles. It does not call Tenants, EventStore, DaprClient, or HttpClient synchronously. [Source: `samples/Hexalith.Tenants.Sample/Endpoints/AccessCheckEndpoints.cs`; `samples/Hexalith.Tenants.Sample.Tests/Endpoints/AccessCheckEndpointsTests.cs`]
- `TenantProjectionEventHandler` updates local projection state for tenant lifecycle, user add/remove/role-change, and configuration set/remove events. [Source: `src/Hexalith.Tenants.Client/Handlers/TenantProjectionEventHandler.cs`]
- `TenantEventProcessor` deduplicates by `MessageId`, rejects payload tenant ID mismatches, and removes failed message IDs so invalid/failed deliveries can retry. [Source: `src/Hexalith.Tenants.Client/Subscription/TenantEventProcessor.cs`]

### Technical Guardrails

- Use repo-pinned versions and package families from project context. Do not bump .NET, DAPR, Aspire, xUnit, Shouldly, or package references for this demo story. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`]
- Command submission goes through EventStore `POST /api/v1/commands`; do not invent Tenants-specific command endpoints. [Source: `docs/quickstart.md`; `tests/Hexalith.Tenants.Server.Tests/Documentation/QuickstartDocumentationTests.cs`; `Hexalith.EventStore/src/Hexalith.EventStore/Controllers/CommandsController.cs`]
- Command status proof uses `GET /api/v1/commands/status/{correlationId}`. [Source: `docs/quickstart.md`; `Hexalith.EventStore/src/Hexalith.EventStore/Controllers/CommandStatusController.cs`]
- `messageId` must be a concrete ULID-shaped idempotency key. Do not use ad hoc strings in docs or scripts. [Source: `docs/quickstart.md`; `tests/Hexalith.Tenants.Server.Tests/Documentation/QuickstartDocumentationTests.cs`; `Hexalith.EventStore/src/Hexalith.EventStore/Validation/SubmitCommandRequestValidator.cs`]
- Platform tenant is `system`; tenant domain is `tenants`; global administrator domain and aggregate ID are `global-administrators`. [Source: `src/Hexalith.Tenants.Contracts/Identity/TenantIdentity.cs`]
- DAPR component/topic names are contracts: pub/sub component `pubsub`, topic `tenants.events`, dead-letter topic `deadletter.tenants.events`, publisher AppId `eventstore`, subscriber AppId `sample`. [Source: `_bmad-output/project-context.md#DAPR`; `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml`; `deploy/dapr/pubsub.yaml`]
- Do not add direct Redis, broker, database, or synchronous Tenants query dependencies to the sample access path. The demo value is local projection state updated by event subscription.
- Keep auth guidance local-development focused. Production claim mapping belongs in `docs/production-auth-claim-contract.md` and `docs/production-auth-readiness.md`.
- Keep logs and demo output support-safe. Do not print raw JWTs, decoded token payloads, secrets, full event payloads, or real user data.

### Previous Story Intelligence

- Story 8.1 established the source-backed quickstart validation pattern and current command route/auth guidance. Reuse its route, token, ULID, and status-proof assumptions. [Source: `_bmad-output/implementation-artifacts/8-1-create-a-prerequisite-validated-quickstart.md`]
- Story 8.2 established reflection/source-backed documentation tests for contracts and warns not to duplicate cross-aggregate timing or compensating-command scope. [Source: `_bmad-output/implementation-artifacts/8-2-publish-the-event-contract-reference.md`]
- Story 8.3 established the sample walkthrough and validation-test pattern for `Program.cs`, `MapTenantEventSubscription()`, the under-20-lines registration target, projection events, `/access`, and support-safe logging. Use those docs as source links rather than restating their full content. [Source: `_bmad-output/implementation-artifacts/8-3-document-the-sample-consuming-service-walkthrough.md`]
- Stories 8.1 through 8.3 recorded that `dotnet test` can build but VSTest may abort in this sandbox with `SocketException (13): Permission denied`; direct xUnit runner execution worked for focused tests. Use the same fallback if needed and record it.
- Recent commits show Story 8.1, 8.2, and 8.3 landed immediately before this story, so quickstart, event contract reference, and sample walkthrough are current sources to link rather than duplicate. [Source: `git log --oneline -5`]

### Latest Technical Notes

- Current DAPR docs state pub/sub messages are wrapped in CloudEvents v1.0 and successful subscriber processing is acknowledged with HTTP 200. Non-200 responses or app crashes trigger redelivery with at-least-once semantics. [Source: DAPR Docs, Publish and subscribe how-to](https://docs.dapr.io/developing-applications/building-blocks/pubsub/howto-publish-subscribe/)
- Current DAPR docs identify programmatic subscriptions as application-code subscriptions that require an endpoint. Tenants Client wraps this through `MapTenantEventSubscription()` and DAPR topic metadata, so the demo should teach the repository API, not raw DAPR attributes. [Source: DAPR Docs, Subscription types](https://docs.dapr.io/developing-applications/building-blocks/pubsub/subscription-methods/)
- Current Aspire dashboard docs describe dashboard support for resource state, logs, traces, environment/configuration, and start/stop/restart actions. It is appropriate demo evidence for dynamic resource endpoints and Sample logs, but it can expose sensitive configuration, so docs should warn against recording secrets. [Source: Aspire dashboard overview](https://aspire.dev/dashboard/overview/)

### Existing Files Likely to Touch

- `docs/demo.md`: primary walkthrough/demo narration target.
- `scripts/demo.sh`: Bash automation target.
- `scripts/demo.ps1`: PowerShell automation target.
- `README.md`: update only if demo link/description drifts.
- `tests/Hexalith.Tenants.Server.Tests/Documentation/AhaMomentDemoDocumentationTests.cs`: likely new validation test.
- `samples/Hexalith.Tenants.Sample.Tests/` and `tests/Hexalith.Tenants.Client.Tests/Subscription/`: validation references; edit only if source/doc drift requires additional assertions.
- `src/Hexalith.Tenants.AppHost/Program.cs`, `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml`, and `deploy/dapr/pubsub.yaml`: touch only if the implementation intentionally adds additional runnable demo subscriber resources. Do not change AppHost topology merely to satisfy prose.
- `_bmad-output/implementation-artifacts/tests/test-summary.md`: update only if validation evidence continues to be recorded there.

### Project Structure Notes

- Alignment: Story 8.4 belongs in `docs/`, `scripts/`, README navigation if needed, and existing documentation/sample/client test areas. It should not change domain behavior, command contracts, projection semantics, package versions, or production deployment posture unless validation exposes a concrete drift bug.
- Detected conflict: archived legacy Story 8.3 bundled demo production with CHANGELOG and CONTRIBUTING work. Current sprint split makes Story 8.4 narrower: reactive access demo assets and validation only.
- Detected drift risk: existing demo docs/scripts came from old work and currently contain auth, domain, and message ID mismatches. Fix those before claiming the demo is reproducible.
- Dirty worktree note at story creation: `_bmad-output/story-automator/orchestration-7-20260601-143204.md` already had unrelated local modifications. Do not restore or rewrite that file during Story 8.4 implementation.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 8.4: Produce the Reactive Access "Aha Moment" Demo`]
- [Source: `_bmad-output/planning-artifacts/prd.md#The "Aha Moment" Demo`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping`]
- [Source: `_bmad-output/project-context.md#DAPR`]
- [Source: `_bmad-output/project-context.md#Testing Rules`]
- [Source: `docs/demo.md`]
- [Source: `scripts/demo.sh`]
- [Source: `scripts/demo.ps1`]
- [Source: `docs/quickstart.md`]
- [Source: `docs/event-contract-reference.md`]
- [Source: `docs/sample-consuming-service-walkthrough.md`]
- [Source: `docs/idempotent-event-processing.md`]
- [Source: `docs/cross-aggregate-timing.md`]
- [Source: `README.md`]
- [Source: `src/Hexalith.Tenants.AppHost/Program.cs`]
- [Source: `src/Hexalith.Tenants.AppHost/HexalithTenantsSample.cs`]
- [Source: `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml`]
- [Source: `deploy/dapr/pubsub.yaml`]
- [Source: `samples/Hexalith.Tenants.Sample/Program.cs`]
- [Source: `samples/Hexalith.Tenants.Sample/Handlers/SampleLoggingEventHandler.cs`]
- [Source: `samples/Hexalith.Tenants.Sample/Endpoints/AccessCheckEndpoints.cs`]
- [Source: `src/Hexalith.Tenants.Client/Subscription/TenantEventSubscriptionEndpoints.cs`]
- [Source: `src/Hexalith.Tenants.Client/Subscription/TenantEventProcessor.cs`]
- [Source: `src/Hexalith.Tenants.Client/Handlers/TenantProjectionEventHandler.cs`]
- [Source: `samples/Hexalith.Tenants.Sample.Tests/Registration/SampleRegistrationTests.cs`]
- [Source: `samples/Hexalith.Tenants.Sample.Tests/Endpoints/AccessCheckEndpointsTests.cs`]
- [Source: `tests/Hexalith.Tenants.Client.Tests/Subscription/TenantEventProcessorTests.cs`]
- [Source: `tests/Hexalith.Tenants.Client.Tests/Subscription/TenantEventSubscriptionEndpointsTests.cs`]
- [Source: `tests/Hexalith.Tenants.Server.Tests/Documentation/QuickstartDocumentationTests.cs`]
- [Source: `tests/Hexalith.Tenants.Server.Tests/Documentation/SampleConsumingServiceWalkthroughDocumentationTests.cs`]
- [Source: DAPR Docs, Publish and subscribe how-to](https://docs.dapr.io/developing-applications/building-blocks/pubsub/howto-publish-subscribe/)
- [Source: DAPR Docs, Subscription types](https://docs.dapr.io/developing-applications/building-blocks/pubsub/subscription-methods/)
- [Source: Aspire dashboard overview](https://aspire.dev/dashboard/overview/)

## Validation Checklist Results

- Story foundation: PASS. Story statement and all five Epic 8.4 acceptance criteria are preserved.
- Scope control: PASS. The story limits implementation to demo docs/scripts/assets and validation, with explicit boundaries around Stories 8.5 and 8.6.
- Architecture/source context: PASS. The story cites AppHost topology, EventStore command routes, Sample subscription/access behavior, DAPR topic naming, auth mode, and projection semantics.
- Reinvention prevention: PASS. The story directs the developer to audit and correct existing `docs/demo.md` and `scripts/demo.*` instead of creating parallel demo artifacts.
- Wrong-library/version prevention: PASS. The story keeps repository-pinned .NET/DAPR/Aspire/testing versions and treats external docs as conceptual confirmation only.
- File-location prevention: PASS. Expected changes are limited to `docs/`, `scripts/`, README navigation if needed, existing documentation tests, and optional evidence.
- Regression prevention: PASS. The story calls out known current drift in script auth, command domain, ULID message IDs, and plural-subscriber claims before implementation.
- Security/privacy prevention: PASS. The story forbids raw tokens, secrets, full payload logs, and sensitive tenant/user data in demo docs, scripts, output, screenshots, or narration.
- Validation evidence: PASS. The story requires source-backed documentation tests plus focused sample/client tests, and distinguishes live AppHost proof from source-backed validation when infrastructure is unavailable.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-01: Audited `docs/demo.md`, `scripts/demo.sh`, and `scripts/demo.ps1` against AppHost/sample source. Confirmed prior drift in script bootstrap domain, non-ULID message IDs, unconditional HMAC token generation, fixed-sleep projection checks, and plural-subscriber wording.
- 2026-06-01: `bash -n scripts/demo.sh` passed.
- 2026-06-01: `pwsh -NoProfile -Command '$ErrorActionPreference="Stop"; $null = [scriptblock]::Create((Get-Content -Raw scripts/demo.ps1)); "pwsh syntax ok"'` passed.
- 2026-06-01: `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --filter FullyQualifiedName~Documentation --no-restore` aborted before execution in this sandbox with `System.Net.Sockets.SocketException (13): Permission denied`; direct xUnit fallback used.
- 2026-06-01: Debug builds passed for `tests/Hexalith.Tenants.Server.Tests`, `samples/Hexalith.Tenants.Sample.Tests`, and `tests/Hexalith.Tenants.Client.Tests` with 0 warnings and 0 errors using single-node MSBuild flags.
- 2026-06-01: Direct xUnit `AhaMomentDemoDocumentationTests` passed: 6 total, 0 failed, 0 skipped.
- 2026-06-01: Direct xUnit related documentation tests passed: 18 total, 0 failed, 0 skipped.
- 2026-06-01: Full direct xUnit regressions passed: Server 704 total, Client 92 total, Sample 31 total, 0 failed, 0 skipped.
- 2026-06-01: Live AppHost demo not executed because `docker info --format '{{.ServerVersion}}'` failed with permission denied on `unix:///var/run/docker.sock`. DAPR CLI/runtime and Aspire CLI were installed, but Docker-backed Keycloak/AppHost execution was unavailable in this sandbox.
- 2026-06-01: Senior review found and fixed HMAC fallback drift: scripts and Aspire E2E token used the Tenants development audience/signing key while the demo submits commands to the EventStore gateway in `EnableKeycloak=false` mode.
- 2026-06-01: `bash -n scripts/demo.sh` passed after review fixes.
- 2026-06-01: `pwsh -NoProfile -Command '$ErrorActionPreference="Stop"; $null = [scriptblock]::Create((Get-Content -Raw scripts/demo.ps1)); "pwsh syntax ok"'` passed after review fixes.
- 2026-06-01: Single-node Debug builds passed for `tests/Hexalith.Tenants.Server.Tests` and `tests/Hexalith.Tenants.IntegrationTests` after review fixes.
- 2026-06-01: Direct xUnit `AhaMomentDemoDocumentationTests` passed after review fixes: 8 total, 0 failed, 0 skipped.
- 2026-06-01: Direct xUnit `AspireTopologyTests` passed with prerequisite skips after review fixes: 5 total, 0 failed, 5 skipped.
- 2026-06-01: Direct xUnit focused related Client and Sample tests passed after review fixes: Client 38 total, 0 failed, 0 skipped; Sample 24 total, 0 failed, 0 skipped.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Audited and corrected the reactive access demo scope around the current single `sample` subscriber while documenting how additional services subscribe through the same `tenants.events` path.
- Reworked `docs/demo.md` as the 90-second proof entry point with setup separated from timed proof, current EventStore command/status routes, Tenants current-state/audit query routes, support-safe evidence guidance, eventual-consistency wording, and future-only synchronous authorization plugin language.
- Hardened `scripts/demo.sh` and `scripts/demo.ps1` to require dynamic EventStore/Sample URLs, accept Keycloak tokens by default, gate HMAC token generation behind explicit fallback flags, generate ULID-shaped command IDs, poll command status, poll `/access` until `granted -> denied`, and summarize command/status/query evidence without printing tokens or payload dumps.
- Added source-backed `AhaMomentDemoDocumentationTests` covering demo topology references, JSON command payloads, script drift regressions, auth-mode separation, support-safe content, eventual-consistency wording, README/demo related links, and one-live-subscriber honesty.
- Senior review fixed the HMAC fallback token generation so the Bash/PowerShell scripts and Aspire E2E test target the EventStore command gateway's `EnableKeycloak=false` development auth settings.
- Live scripted AppHost execution was not claimed because Docker access is unavailable in this sandbox; the exact prerequisite failure is recorded in the debug log and test summary.

### File List

- `docs/demo.md`
- `scripts/demo.sh`
- `scripts/demo.ps1`
- `tests/Hexalith.Tenants.Server.Tests/Documentation/AhaMomentDemoDocumentationTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/AspireTopologyTests.cs`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `_bmad-output/implementation-artifacts/8-4-produce-the-reactive-access-aha-moment-demo.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

## Senior Developer Review (AI)

### Findings

- **High - fixed:** `scripts/demo.sh`, `scripts/demo.ps1`, and `tests/Hexalith.Tenants.IntegrationTests/AspireTopologyTests.cs` generated HMAC fallback tokens with the Tenants development audience/signing key, but the demo command flow submits to the EventStore gateway. In the `EnableKeycloak=false` AppHost path, EventStore uses its own development auth settings, so the fallback token would fail before command submission. Updated all three to use `hexalith-eventstore` with the EventStore development signing key and added documentation-test coverage.
- **Medium - fixed:** `tests/Hexalith.Tenants.IntegrationTests/AspireTopologyTests.cs` was modified by the implementation but missing from the story File List. Added it to the File List so the story record matches git reality.

### Review Validation

- `bash -n scripts/demo.sh` passed.
- `pwsh -NoProfile -Command '$ErrorActionPreference="Stop"; $null = [scriptblock]::Create((Get-Content -Raw scripts/demo.ps1)); "pwsh syntax ok"'` passed.
- `dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore -m:1 /nr:false /p:BuildInParallel=false /p:UseSharedCompilation=false` passed with 0 warnings and 0 errors.
- `dotnet build tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-restore -m:1 /nr:false /p:BuildInParallel=false /p:UseSharedCompilation=false` passed with 0 warnings and 0 errors.
- `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -class Hexalith.Tenants.Server.Tests.Documentation.AhaMomentDemoDocumentationTests -parallel none -noLogo -noColor` passed: 8 total, 0 failed, 0 skipped.
- `dotnet tests/Hexalith.Tenants.IntegrationTests/bin/Debug/net10.0/Hexalith.Tenants.IntegrationTests.dll -class Hexalith.Tenants.IntegrationTests.AspireTopologyTests -parallel none -noLogo -noColor` passed with prerequisite skips: 5 total, 0 failed, 5 skipped.
- Focused Client and Sample direct xUnit suites passed: Client 38 total; Sample 24 total; 0 failed.
- Aspire docs MCP lookup was unavailable during review; web fallback confirmed the Aspire dashboard surfaces resources, logs, traces, configuration, and sensitive runtime data that must be handled carefully. Source: <https://learn.microsoft.com/dotnet/aspire/fundamentals/dashboard/overview>

## Change Log

| Date | Version | Description | Author |
|------|---------|-------------|--------|
| 2026-06-01 | 0.1 | Initial story context created | GPT-5 Codex |
| 2026-06-01 | 1.0 | Produced source-backed reactive access demo docs/scripts, added documentation tests, validated with direct xUnit fallback, and recorded live AppHost Docker blocker | GPT-5 Codex |
| 2026-06-01 | 1.1 | Senior review fixed HMAC fallback auth drift, added regression coverage, documented Aspire E2E file, and marked story done | GPT-5 Codex |
