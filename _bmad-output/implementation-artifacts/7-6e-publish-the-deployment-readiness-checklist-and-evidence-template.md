---
baseline_commit: de520a9
---

# Story 7.6E: Publish the Deployment Readiness Checklist and Evidence Template

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a platform operator,
I want a deployment readiness checklist and evidence template,
so that production readiness proof is repeatable across environments.

## Acceptance Criteria

1. Given a deployment readiness checklist is followed, when an operator verifies Tenants in an environment, then the checklist covers issuer, audience, `eventstore:tenant`, HTTPS metadata, signing/authority source, DAPR components, service invocation, and health endpoints, and development token guidance is clearly separated from production IdP setup.
2. Given deployment readiness documentation and smoke tests are reviewed, when operators prepare a production deployment, then required environment variables, IdP claim mappings, DAPR prerequisites, AppHost overrides, and verification commands are documented, and smoke-test evidence can be used as release or deployment readiness proof.

## Tasks / Subtasks

- [x] Reconcile existing deployment-readiness evidence before changing docs. (AC: 1, 2)
  - [x] Read Story 7.6A, 7.6B, 7.6C, and 7.6D completion notes; treat their smoke-test lanes as source evidence, not work to recreate.
  - [x] Confirm `_bmad-output/implementation-artifacts/tests/test-summary.md` still contains separate Story 7.6A-D evidence sections with pass/fail/skip counts and live-evidence boundaries.
  - [x] Confirm existing docs remain the source material: `docs/production-auth-readiness.md`, `docs/production-auth-claim-contract.md`, `docs/quickstart.md`, `deploy/dapr/README.md`, `docs/event-contract-reference.md`, `docs/cross-aggregate-timing.md`, and `docs/idempotent-event-processing.md`.
  - [x] Confirm `Hexalith.EventStore/scripts/validate-operational-evidence.py` currently supports only query and SignalR operational-evidence schemas; do not claim Tenants deployment evidence is validated by that script unless this story explicitly adds a supported Tenants schema.
  - [x] Do not duplicate auth smoke tests, DAPR topology tests, health/readiness tests, or pub/sub recovery tests already owned by Stories 7.6A-D.

- [x] Publish a consolidated deployment readiness guide. (AC: 1, 2)
  - [x] Create `docs/deployment-readiness.md` as the operator-facing checklist and evidence guide for Tenants production-like deployment readiness.
  - [x] Link it from `README.md` near the existing quickstart, production auth, and DAPR deployment documentation links.
  - [x] Keep `docs/production-auth-readiness.md` auth-specific; the new guide should consolidate and reference it rather than moving all auth content or creating conflicting checklists.
  - [x] Separate local development token guidance from production IdP setup. Local HMAC and local Keycloak examples belong in `docs/quickstart.md`; production evidence must use OIDC authority-based JWT validation and redacted production tokens.
  - [x] Include a clear "what this proves / what it does not prove" section so static documentation/config checks are not mistaken for live deployment proof.

- [x] Cover the required readiness checklist controls. (AC: 1, 2)
  - [x] Auth controls: issuer, audience, token expiration, subject, effective `eventstore:tenant=system`, HTTPS metadata, production signing source, direct/source claim mapping, global-administrator tenant-claim behavior, and fail-closed 401/403 outcomes.
- [x] Configuration controls: required environment variables for `Authentication__JwtBearer__Authority`, `Authentication__JwtBearer__Issuer`, `Authentication__JwtBearer__Audience`, `Authentication__JwtBearer__RequireHttpsMetadata`, and `Authentication__JwtBearer__SigningKey` absence in production.
  - [x] DAPR controls: AppIds `eventstore`, `tenants`, `eventstore-admin`, `eventstore-admin-ui`, and `sample`; components `statestore` and `pubsub`; topic `tenants.events`; dead letter `deadletter.tenants.events`; placement; scheduler; state-store scopes; pub/sub scopes; and receiver-specific deny-by-default access control.
  - [x] Service-invocation controls: `eventstore` is the only production caller allowed to Tenants `POST /process` and `POST /project`; domain-service registrations route `system|tenants|v1` and `system|global-administrators|v1` to AppId `tenants` method `process`.
  - [x] Health/readiness controls: `/alive` is process liveness, `/ready` is dependency readiness, `/health` is diagnostic health, unhealthy readiness returns HTTP 503, and live command/query path evidence is separate from `/ready`.
  - [x] Pub/sub recovery controls: EventStore is source of truth, `PublishFailed` means stored-but-not-yet-published, drain recovery republishes persisted events, DAPR delivery is at-least-once, and subscriber catch-up must be backed by live or documented idempotency evidence.
- [x] AppHost/operator controls: required DAPR full-init or slim-mode prerequisites, Docker/AppHost prerequisites for local proof, local `EnableKeycloak=false` fallback boundaries, production override locations, no fixed DAPR sidecar ports, and no recursive submodule initialization.

- [x] Add a reusable evidence template. (AC: 2)
  - [x] Add a copyable evidence template in `docs/deployment-readiness.md` or a companion `docs/deployment-readiness-evidence-template.md`.
  - [x] Template fields must include environment alias, run date/time, commit SHA or package version, operator/reviewer alias, run profile (`deterministic-local`, `prepared-apphost`, `production-like`, or `production`), final classification, evidence source links, redaction statement, and reviewer verdict.
  - [x] Include per-control rows for auth, DAPR components, service invocation, health/readiness, command path, query path, pub/sub recovery, and evidence boundaries.
  - [x] Use classifications that separate proof from blockers: `pass`, `environment-blocker`, `product-failure`, `configuration-gap`, `instrumentation-gap`, `documentation-gap`, and `not-claimable`.
  - [x] Include explicit live-evidence boundary fields for skipped DAPR/AppHost tests; skipped live tests are not passing deployment proof.
  - [x] Include a redaction checklist forbidding compact JWTs, bearer tokens, signing keys, decoded token payloads, raw command/event payloads, private hosts, concrete connection strings, real tenant/user identifiers, and PII.

- [x] Add deterministic documentation tests for the published guide and template. (AC: 1, 2)
  - [x] Add `tests/Hexalith.Tenants.Server.Tests/Documentation/DeploymentReadinessDocumentationTests.cs` or extend the nearest existing documentation/configuration test only if that keeps the assertions cohesive.
  - [x] Assert `docs/deployment-readiness.md` exists and links to `production-auth-readiness.md`, `production-auth-claim-contract.md`, `quickstart.md`, `deploy/dapr/README.md`, and the Story 7.6A-D evidence source in `_bmad-output/implementation-artifacts/tests/test-summary.md`.
  - [x] Assert the guide contains the AC-required terms: issuer, audience, `eventstore:tenant`, HTTPS metadata, signing/authority source, DAPR components, service invocation, health endpoints, environment variables, IdP claim mappings, DAPR prerequisites, AppHost overrides, and verification commands.
  - [x] Assert local development token guidance is visibly separated from production IdP setup and does not present local HMAC or sample Keycloak credentials as production readiness evidence.
  - [x] Assert the evidence template contains required metadata, classifications, control rows, redaction statement, reviewer verdict, and live-evidence boundary rows.
  - [x] Assert published docs and template do not contain compact JWTs, bearer tokens, raw signing keys, concrete production connection strings, private network addresses, real issuer hosts, real tenant/user identifiers, or PII. Local-only quickstart placeholders and documented local ports remain allowed only in quickstart/DAPR prerequisite context.

- [x] Capture Story 7.6E implementation evidence. (AC: 1, 2)
  - [x] Add a Story 7.6E section to `_bmad-output/implementation-artifacts/tests/test-summary.md`.
  - [x] Record documentation tests, any focused build/test commands, pass/fail/skip counts, and safe evidence categories.
  - [x] If VSTest socket restrictions occur in this sandbox, use the repository's direct xUnit executable fallback pattern and record the environment limitation separately from product failures.
  - [x] Do not record raw production evidence in the story file or test summary. Use safe aliases and placeholders only.

- [x] Run focused validation and record evidence accurately. (AC: 1, 2)
  - [x] Run `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~DeploymentReadinessDocumentationTests|FullyQualifiedName~QuickstartDocumentationTests|FullyQualifiedName~EventPublicationConfigurationTests|FullyQualifiedName~AuthenticationConfigurationTests"`.
  - [x] Run `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~HealthEndpointsTests|FullyQualifiedName~TenantsQueryControllerIntegrationTests|FullyQualifiedName~CommandApiRuntimeIntegrationTests|FullyQualifiedName~DaprTestPrerequisiteDiagnosticsTests"` if docs reference those classes as readiness evidence.
  - [x] Use direct xUnit fallback if `dotnet test` is blocked by sandbox socket permissions, matching the Story 7.6A-D pattern.
  - [x] Run `dotnet build Hexalith.Tenants.slnx --configuration Debug --no-restore` if production code, project files, shared test infrastructure, README, or docs beyond the focused guide/template change.
  - [x] Do not mark ACs complete from manually inspected docs alone; the published guide/template must be pinned by deterministic tests.

## Dev Notes

### Source Context

- Epic 7 objective: operators can deploy Tenants alongside EventStore with Aspire/DAPR, production auth validation, telemetry, health, stateless operation, and recovery behavior. Story 7.6E is the final consolidation story for the corrected deployment-readiness split. [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.6E: Publish the Deployment Readiness Checklist and Evidence Template`]
- The 2026-05-31 sprint correction split the old oversized Story 7.6 into independently diagnosable auth, DAPR service invocation, health readiness, pub/sub recovery, and final evidence-template stories. 7.6E must preserve that separation while making the final operator proof repeatable. [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-31.md#Proposal B: Split Story 7.6 Before Handoff`]
- PRD requirements relevant to this story include FR48, FR53-FR57, FR60, NFR9, NFR15-NFR17, NFR20, NFR22, and NFR23. The guide must turn those into operator controls without adding new runtime behavior. [Source: `_bmad-output/planning-artifacts/prd.md#Functional Requirements`; `_bmad-output/planning-artifacts/prd.md#Non-Functional Requirements`]
- Architecture maps Epic 7 work to `AppHost`, `Aspire`, `ServiceDefaults`, `Telemetry`, `Health`, `Configuration`, `Validation`, auth tests, production auth docs, and deployment smoke tests. [Source: `_bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping`]

### Current Repository State

- `docs/production-auth-readiness.md` already contains auth-specific production settings, token content expectations, IdP claim contract, an auth deployment readiness checklist, deterministic auth smoke-test commands, manual auth smoke checks, failure triage, evidence map, and deployment boundaries.
- `docs/production-auth-claim-contract.md` documents direct and source tenant-claim mapping, source-claim precedence, global-administrator behavior, exact `system` tenant requirements, case-sensitive ID comparison, and EventStore host boundaries.
- `docs/quickstart.md` owns local developer setup: .NET SDK, Docker, full `dapr init`, root-level submodules, AppHost startup, local Keycloak token retrieval, HMAC fallback for `EnableKeycloak=false`, and first EventStore command submission.
- `src/Hexalith.Tenants.AppHost/Program.cs` wires local Keycloak-derived auth overrides through `Authentication__JwtBearer__Authority`, `Issuer`, `Audience`, `RequireHttpsMetadata=false`, and empty `SigningKey` for local AppHost services. Production guidance must not copy the local `RequireHttpsMetadata=false` value; production uses `true`.
- `deploy/dapr/README.md` documents local, slim, and production DAPR deployment contracts, including AppIds, `statestore`, `pubsub`, `tenants.events`, `deadletter.tenants.events`, production receiver-specific access control, pub/sub recovery evidence, and failure triage.
- `_bmad-output/implementation-artifacts/tests/test-summary.md` currently stores Story 7.6A-D evidence sections. 7.6E should append a new section instead of rewriting prior evidence.
- `tests/Hexalith.Tenants.Server.Tests/Documentation/QuickstartDocumentationTests.cs` demonstrates the preferred source-backed documentation-test style: read docs with `File.ReadAllText`, assert required terms/routes/source references, and parse JSON examples when structure matters.
- `tests/Hexalith.Tenants.Server.Tests/Configuration/EventPublicationConfigurationTests.cs` already pins DAPR component names, scopes, resiliency targets, production access control, DAPR docs terms, support-safe DAPR evidence, local Keycloak quickstart claims, and no provider-specific package references.
- `Hexalith.EventStore/scripts/validate-operational-evidence.py` and `validate-evidence.sh` exist in the submodule, but the validator explicitly supports only `query-operational-evidence/v1` and `signalr-operational-evidence/v1`. Treat it as vocabulary inspiration only unless this story adds a Tenants-specific schema and tests.

### Previous Story Intelligence

- Story 7.6A completed production auth smoke evidence. It validated protected command/query auth with real JWT bearer middleware or production-like local seams, recorded VSTest socket denial as an environment limitation, and used direct xUnit fallback. Preserve its fail-closed `eventstore:tenant=system` and support-safe evidence boundaries. [Source: `_bmad-output/implementation-artifacts/7-6a-validate-production-auth-smoke-tests.md`]
- Story 7.6B completed DAPR component and service-invocation smoke evidence. Static YAML/config/docs validation is deterministic evidence only; live DAPR/AppHost tests are prerequisite-gated and skipped live tests are not passing proof. [Source: `_bmad-output/implementation-artifacts/7-6b-validate-dapr-component-and-service-invocation-smoke-tests.md`]
- Story 7.6C completed health/readiness smoke evidence. It preserved `/alive` as process liveness, `/ready` as bounded DAPR state-store readiness, and command/query path checks as separate evidence from `/ready`. [Source: `_bmad-output/implementation-artifacts/7-6c-validate-health-and-dependency-readiness-smoke-tests.md`]
- Story 7.6D completed pub/sub recovery and catch-up evidence. It distinguishes persisted source-of-truth events from publication success, records `PublishFailed` as stored-but-not-published, preserves at-least-once subscriber semantics, and does not claim live subscriber catch-up without a live assertion. [Source: `_bmad-output/implementation-artifacts/7-6d-validate-pub-sub-recovery-and-catch-up-evidence.md`]
- Recent commits before story creation are `de520a9 feat(story-7.6D): Validate Pub/Sub Recovery and Catch-Up Evidence`, `910fa23 feat(story-7.6C): Validate Health and Dependency Readiness Smoke Tests`, `d20a990 feat(story-7.6B): Validate DAPR Component and Service Invocation Smoke Tests`, and `4db3ca7 feat(story-7.6A): Validate Production Auth Smoke Tests`.

### Latest Technical Information

- ASP.NET Core JWT bearer guidance for .NET 10 requires validating token signature, issuer, audience, and expiration; incorrect required claims or values should fail authentication with `401`. Keep the production auth checklist aligned with those controls. [External: Microsoft Learn, Configure JWT bearer authentication in ASP.NET Core, `https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0`]
- Aspire health checks distinguish AppHost resource checks from service endpoint checks. Do not let the final checklist treat AppHost resource liveness as equivalent to the Tenants `/ready` endpoint. [External: Aspire Docs, Health checks, `https://aspire.dev/fundamentals/health-checks/`]
- Aspire DAPR integration uses the CommunityToolkit Dapr integration to add sidecars and wire state store/pub-sub/component resources. Keep repository-pinned packages and current AppHost patterns; do not add obsolete Aspire DAPR workload assumptions. [External: Aspire Docs, Dapr framework integration, `https://aspire.dev/integrations/frameworks/dapr/`]
- DAPR resiliency specs are colocated with components and applied when the sidecar starts; targets can include apps, components, and actors. Keep checklist verification tied to `deploy/dapr/resiliency.yaml` and AppHost `DaprComponents/resiliency.yaml`, not product code. [External: DAPR Docs, Resiliency overview, `https://docs.dapr.io/operations/resiliency/resiliency-overview/`]
- DAPR app health checks are disabled by default; when enabled, the sidecar can stop pub/sub subscriptions, input bindings, and service-invocation forwarding until the app health check succeeds. Do not add DAPR app-health configuration in this docs story unless separately approved. [External: DAPR Docs, App health checks, `https://docs.dapr.io/operations/resiliency/health-checks/app-health/`]
- DAPR sidecar `/healthz` is intended for infrastructure health checks; application code should not depend on it. The Tenants readiness checklist should keep `/ready` as the application endpoint and DAPR sidecar health as infrastructure evidence. [External: DAPR Docs, Sidecar health, `https://docs.dapr.io/operations/resiliency/health-checks/sidecar-health/`]
- DAPR pub/sub provides at-least-once delivery, and dead-letter topics should be paired with retry resiliency policies before classifying delivery failures. Keep evidence wording away from exactly-once or global-ordering claims. [External: DAPR Docs, Pub/sub overview, `https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/`; DAPR Docs, Dead Letter Topics, `https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-deadletter/`]

### Technical Guardrails

- This is a documentation and deterministic validation story. Do not change tenant domain behavior, EventStore command pipeline behavior, DAPR topology, authentication policy, health endpoint semantics, projection logic, package versions, or AppHost resource names unless a direct doc/test contradiction is found and recorded.
- Prefer a published docs artifact under `docs/` plus source-backed documentation tests. Do not create only an implementation-artifact template that operators cannot discover from README.
- Use existing local docs instead of inventing new contracts: `production-auth-readiness.md` for auth, `deploy/dapr/README.md` for DAPR, `quickstart.md` for local development, and Story 7.6A-D sections in `test-summary.md` for evidence examples.
- Keep static and live evidence separate. Static tests can prove documentation/configuration contracts; live deployment proof requires prepared DAPR/AppHost/IdP infrastructure and must be recorded with prerequisite availability.
- Preserve support-safe evidence. Published docs, test assertions, story notes, and evidence templates must not contain compact JWTs, bearer tokens, signing keys, decoded token payloads, raw command/event payloads, private hosts, concrete connection strings, real tenant/user identifiers, or PII.
- Use ULID-shaped placeholders for command `messageId`, `correlationId`, and related EventStore identifiers. Do not use GUID-shaped examples for EventStore command envelopes.
- Do not edit the `Hexalith.EventStore` submodule for this story. Refer to its validator scripts as existing context only.

### Existing Files Likely to Touch

- `docs/deployment-readiness.md`: likely new consolidated checklist and evidence-template guide.
- `docs/deployment-readiness-evidence-template.md`: optional companion template if the guide would become too dense.
- `README.md`: add a link to the new deployment readiness guide near existing docs links.
- `tests/Hexalith.Tenants.Server.Tests/Documentation/DeploymentReadinessDocumentationTests.cs`: likely new deterministic source-backed tests for the guide/template.
- `tests/Hexalith.Tenants.Server.Tests/Configuration/EventPublicationConfigurationTests.cs`: update only if shared DAPR documentation assertions need to include the new guide.
- `tests/Hexalith.Tenants.Server.Tests/Documentation/QuickstartDocumentationTests.cs`: update only if new links or quickstart separation checks belong there.
- `_bmad-output/implementation-artifacts/tests/test-summary.md`: append Story 7.6E evidence.
- `docs/production-auth-readiness.md`, `docs/production-auth-claim-contract.md`, `docs/quickstart.md`, and `deploy/dapr/README.md`: update only to fix stale cross-links or avoid duplicated/conflicting readiness guidance.

### Preserve Existing Behavior

- Preserve `POST /api/v1/commands`, command status route expectations, and EventStore command envelope shape.
- Preserve `eventstore:tenant=system`, direct/source tenant-claim precedence, case-sensitive ID comparison, and fail-closed global-administrator behavior.
- Preserve DAPR names: AppId `tenants`, state store `statestore`, pub/sub `pubsub`, topic `tenants.events`, and dead letter `deadletter.tenants.events`.
- Preserve production deny-by-default DAPR access control, with `eventstore` as the allowed internal caller for Tenants `/process` and `/project`.
- Preserve `/alive` and `/ready` semantics from Story 7.6C.
- Preserve EventStore source-of-truth and DAPR at-least-once semantics from Story 7.6D.
- Preserve direct xUnit fallback reporting for sandbox VSTest socket denial.

### Out of Scope

- New auth policy, IdP provisioning automation, Keycloak or Entra configuration beyond documenting expected mappings, Helm/Kubernetes/Azure Container Apps manifests, DAPR app-health adoption, OpenTelemetry dashboards, alert rules, live production smoke execution, and new evidence validator schema tooling.
- Reworking Story 7.6A-D tests or docs unless a stale link, contradictory wording, or support-safety issue directly blocks the consolidated guide.
- Phase 2 Admin UI evidence, browser E2E tests, frontend changes, or FrontComposer work.
- Editing `Hexalith.EventStore` submodule files.

### Testing Standards

- Use xUnit v3 and Shouldly; do not use `Assert.*`.
- Keep documentation tests deterministic and infrastructure-free.
- Prefer exact source-backed assertions over broad prose checks where possible: route names, environment variable keys, DAPR component names, classification values, file links, and redaction-forbidden patterns.
- If parsing template metadata or tables, use structured parsing where practical; otherwise keep string assertions narrow and intentional.
- If `dotnet test` cannot run because of VSTest socket restrictions, build first if needed and use the direct xUnit executable fallback pattern already recorded in Stories 7.6A-D.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.6E: Publish the Deployment Readiness Checklist and Evidence Template`]
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-31.md#Proposal B: Split Story 7.6 Before Handoff`]
- [Source: `_bmad-output/planning-artifacts/prd.md#Functional Requirements`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping`]
- [Source: `_bmad-output/project-context.md#Technology Stack & Versions`]
- [Source: `_bmad-output/project-context.md#DAPR`]
- [Source: `_bmad-output/project-context.md#API Surface`]
- [Source: `_bmad-output/project-context.md#Testing Rules`]
- [Source: `_bmad-output/implementation-artifacts/7-6a-validate-production-auth-smoke-tests.md`]
- [Source: `_bmad-output/implementation-artifacts/7-6b-validate-dapr-component-and-service-invocation-smoke-tests.md`]
- [Source: `_bmad-output/implementation-artifacts/7-6c-validate-health-and-dependency-readiness-smoke-tests.md`]
- [Source: `_bmad-output/implementation-artifacts/7-6d-validate-pub-sub-recovery-and-catch-up-evidence.md`]
- [Source: `_bmad-output/implementation-artifacts/tests/test-summary.md`]
- [Source: `docs/production-auth-readiness.md`]
- [Source: `docs/production-auth-claim-contract.md`]
- [Source: `docs/quickstart.md`]
- [Source: `deploy/dapr/README.md`]
- [Source: `tests/Hexalith.Tenants.Server.Tests/Documentation/QuickstartDocumentationTests.cs`]
- [Source: `tests/Hexalith.Tenants.Server.Tests/Configuration/EventPublicationConfigurationTests.cs`]
- [Source: `Hexalith.EventStore/scripts/validate-operational-evidence.py`]
- [External: Microsoft Learn, Configure JWT bearer authentication in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0)
- [External: Aspire Docs, Health checks](https://aspire.dev/fundamentals/health-checks/)
- [External: Aspire Docs, Dapr framework integration](https://aspire.dev/integrations/frameworks/dapr/)
- [External: DAPR Docs, Resiliency overview](https://docs.dapr.io/operations/resiliency/resiliency-overview/)
- [External: DAPR Docs, App health checks](https://docs.dapr.io/operations/resiliency/health-checks/app-health/)
- [External: DAPR Docs, Sidecar health](https://docs.dapr.io/operations/resiliency/health-checks/sidecar-health/)
- [External: DAPR Docs, Pub/sub overview](https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/)
- [External: DAPR Docs, Dead Letter Topics](https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-deadletter/)

## Project Structure Notes

- Alignment: Story 7.6E belongs in published docs, deterministic documentation tests, README navigation, and the shared implementation evidence summary.
- Detected baseline: Story 7.6A-D already provide the smoke-test evidence lanes. The likely implementation is a consolidated operator guide, template, docs tests, and evidence summary entry.
- Detected risk: `docs/production-auth-readiness.md` already has an auth-only readiness checklist. The new guide must not create contradictory auth instructions; it should reference and summarize that document.
- Detected risk: EventStore has operational evidence validator tooling, but it is not currently a Tenants deployment-readiness schema. Do not falsely claim validator coverage.
- No UI/frontend changes are required for this story.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References
- 2026-06-02: Reconciled Story 7.6A-D completion notes and `_bmad-output/implementation-artifacts/tests/test-summary.md`; confirmed existing Story 7.6A-D evidence sections remain separate and preserve pass/fail/skip counts plus live-evidence boundaries.
- 2026-06-02: Confirmed existing source docs remain the source material: production auth readiness, production auth claim contract, quickstart, DAPR deployment README, event contract reference, cross-aggregate timing, and idempotent event processing.
- 2026-06-02: Confirmed `Hexalith.EventStore/scripts/validate-operational-evidence.py` supports only `query-operational-evidence/v1` and `signalr-operational-evidence/v1`; the new Tenants deployment readiness template is not claimed as validator-supported.
- 2026-06-02: Added red-phase `DeploymentReadinessDocumentationTests`; initial direct xUnit run failed because `docs/deployment-readiness.md` did not exist.
- 2026-06-02: Published `docs/deployment-readiness.md`, linked it from `README.md`, and reran `DeploymentReadinessDocumentationTests`: 5 total, 0 failed, 0 skipped.
- 2026-06-02: Required focused Server.Tests and IntegrationTests `dotnet test` commands aborted before execution with sandbox MSBuild/VSTest socket denial: `System.Net.Sockets.SocketException (13): Permission denied`. Treated as environment limitation, not product failure.
- 2026-06-02: Direct xUnit focused Server.Tests fallback passed: 54 total, 0 failed, 0 skipped.
- 2026-06-02: Direct xUnit focused IntegrationTests fallback passed: 184 total, 0 failed, 0 skipped.
- 2026-06-02: Debug solution build passed with 0 warnings and 0 errors using `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0`, `--configuration Debug`, `--no-restore`, `-m:1`, `/nr:false`, `/p:BuildInParallel=false`, and `/p:UseSharedCompilation=false`.
- 2026-06-02: Full direct xUnit regression sweep passed: Contracts 105/0 failed, Client 92/0 failed, Testing 181/0 failed, Sample 31/0 failed, Server 733/0 failed, Integration 232 total with 0 failed and 28 DAPR/performance prerequisite-gated skips.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Published `docs/deployment-readiness.md` as the consolidated operator checklist and copyable evidence template for Tenants deployment readiness.
- Kept production auth details in `docs/production-auth-readiness.md` and `docs/production-auth-claim-contract.md`; the new guide consolidates and references those sources instead of creating a conflicting auth checklist.
- Separated production OIDC authority-based evidence from local HMAC/local Keycloak development guidance and made static documentation checks distinct from live deployment proof.
- Added deterministic source-backed documentation tests covering required links, checklist controls, evidence-template metadata/classifications/control rows, redaction rules, and the EventStore validator boundary.
- Captured Story 7.6E evidence in `_bmad-output/implementation-artifacts/tests/test-summary.md`, including VSTest socket limitation, direct xUnit fallback results, solution build, and full regression sweep.

### File List
- `README.md`
- `docs/deployment-readiness.md`
- `tests/Hexalith.Tenants.Server.Tests/Documentation/DeploymentReadinessDocumentationTests.cs`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/7-6e-publish-the-deployment-readiness-checklist-and-evidence-template.md`

## Senior Developer Review (AI)

**Reviewer:** GPT-5 Codex  
**Date:** 2026-06-02  
**Outcome:** Approved

### Review Summary

- Verified Acceptance Criteria 1 and 2 against `docs/deployment-readiness.md`, `README.md`, `tests/Hexalith.Tenants.Server.Tests/Documentation/DeploymentReadinessDocumentationTests.cs`, and `_bmad-output/implementation-artifacts/tests/test-summary.md`.
- Confirmed the guide covers issuer, audience, `eventstore:tenant`, HTTPS metadata, signing/authority source, DAPR components, service invocation, health endpoints, environment variables, IdP claim mappings, DAPR prerequisites, AppHost overrides, verification commands, and the production/local token boundary.
- Confirmed the evidence template includes required metadata, run profiles, classifications, per-control rows, live-evidence boundaries, redaction statement, reviewer verdict, and support-safe redaction checklist.
- Confirmed the EventStore operational evidence validator boundary is documented and tested; the guide does not claim Tenants deployment readiness evidence is validator-supported.

### Findings

- No CRITICAL, HIGH, or MEDIUM implementation issues remain.
- No auto-fix code changes were required beyond review status and sprint-status synchronization.

### Validation

- `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore -m:1 /nr:false /p:BuildInParallel=false /p:UseSharedCompilation=false` - passed with 0 warnings and 0 errors.
- `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -noLogo -noColor -parallel none -class Hexalith.Tenants.Server.Tests.Documentation.DeploymentReadinessDocumentationTests` - passed, 6 total, 0 errors, 0 failed, 0 skipped.
- `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -noLogo -noColor -parallel none -class Hexalith.Tenants.Server.Tests.Documentation.DeploymentReadinessDocumentationTests -class Hexalith.Tenants.Server.Tests.Documentation.QuickstartDocumentationTests -class Hexalith.Tenants.Server.Tests.Configuration.EventPublicationConfigurationTests -class Hexalith.Tenants.Server.Tests.Configuration.AuthenticationConfigurationTests` - passed, 55 total, 0 errors, 0 failed, 0 skipped.
- `dotnet tests/Hexalith.Tenants.IntegrationTests/bin/Debug/net10.0/Hexalith.Tenants.IntegrationTests.dll -noLogo -noColor -parallel none -class Hexalith.Tenants.IntegrationTests.HealthEndpointsTests -class Hexalith.Tenants.IntegrationTests.TenantsQueryControllerIntegrationTests -class Hexalith.Tenants.IntegrationTests.CommandApiRuntimeIntegrationTests -class Hexalith.Tenants.IntegrationTests.Fixtures.DaprTestPrerequisiteDiagnosticsTests` - passed, 184 total, 0 errors, 0 failed, 0 skipped.

## Change Log

| Date       | Version | Description | Author |
|------------|---------|-------------|--------|
| 2026-06-02 | 0.1     | Created Story 7.6E implementation context for deployment readiness checklist and evidence template publishing. | GPT-5 Codex |
| 2026-06-02 | 1.0     | Published deployment readiness guide and evidence template, added deterministic documentation tests, captured validation evidence, and moved story to review. | GPT-5 Codex |
| 2026-06-02 | 1.1     | Completed senior developer review, approved implementation, and moved story to done. | GPT-5 Codex |
