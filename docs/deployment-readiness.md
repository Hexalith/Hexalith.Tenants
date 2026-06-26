[Back to README](../README.md)

# Deployment Readiness

Use this guide when preparing Hexalith.Tenants for a production-like or production deployment beside EventStore. It consolidates operator checks from [Production Auth Readiness](production-auth-readiness.md), [Production Auth Claim Contract](production-auth-claim-contract.md), [Quickstart](quickstart.md), [DAPR deployment templates](../deploy/dapr/README.md), the [Event Contract Reference](event-contract-reference.md), [Cross-Aggregate Timing](cross-aggregate-timing.md), and [Idempotent Event Processing](idempotent-event-processing.md).

Story 7.6A-D evidence source: [_bmad-output/implementation-artifacts/tests/test-summary.md](../_bmad-output/implementation-artifacts/tests/test-summary.md). Treat those sections as smoke-test evidence lanes to reference, not work to duplicate.

## What This Proves

When completed with live environment evidence, this checklist proves that a specific Tenants deployment profile has the required authentication, DAPR, service-invocation, health, command, query, and pub/sub recovery controls available for release review.

For deterministic-local runs, this guide and its tests prove that the published documentation and configuration contracts are internally consistent, support-safe, and tied to the existing Story 7.6A-D evidence sources.

## What This Does Not Prove

Static documentation checks do not prove live deployment readiness. A skipped DAPR/AppHost test is an evidence boundary, not a pass. Live proof still requires a prepared environment with the selected IdP, EventStore, Tenants host, DAPR sidecars, state store, pub/sub, placement, scheduler, and operator-approved networking.

The EventStore operational evidence validator currently supports query and SignalR operational-evidence schemas only. Do not claim this Tenants deployment readiness template is validated by that script.

## Production IdP Readiness

Production readiness evidence uses OIDC authority-based JWT validation. Local HMAC tokens and local Keycloak examples are development-only and belong in [Quickstart](quickstart.md). Production proof must use redacted production tokens, the configured OIDC authority, and support-safe evidence.

Auth controls:

| Control | Required production evidence | Failure classification |
| --- | --- | --- |
| issuer | Token `iss` matches `Authentication__JwtBearer__Issuer`; wrong issuer returns `401`. | configuration-gap or product-failure |
| audience | Token `aud` matches `Authentication__JwtBearer__Audience`; wrong audience returns `401`. | configuration-gap or product-failure |
| token expiration | Token `exp` is future at run time; expired token returns `401`. | configuration-gap |
| subject | Token has stable non-empty `sub`; evidence stores only a redacted subject alias. | configuration-gap |
| effective tenant | Effective principal contains `eventstore:tenant=system`, including global-administrator operators. | configuration-gap or product-failure |
| HTTPS metadata | `Authentication__JwtBearer__RequireHttpsMetadata=true` in production. | configuration-gap |
| signing/authority source | Production uses OIDC `Authentication__JwtBearer__Authority`; `Authentication__JwtBearer__SigningKey` is absent or empty. | configuration-gap |
| IdP claim mappings | Direct `eventstore:*` claims or one documented source-claim mapping produce the effective principal. | configuration-gap |
| global administrator behavior | Global-administrator-shaped principals still fail closed without effective `eventstore:tenant=system`. | product-failure |
| fail-closed outcomes | Invalid authentication returns `401`; invalid authorization returns `403` before command/query dispatch. | product-failure |

Required environment variables:

| Environment variable | Production expectation |
| --- | --- |
| `Authentication__JwtBearer__Authority` | Absolute HTTPS OIDC authority. |
| `Authentication__JwtBearer__Issuer` | Exact issuer expected in the token. |
| `Authentication__JwtBearer__Audience` | Exact accepted audience for Tenants. |
| `Authentication__JwtBearer__RequireHttpsMetadata` | `true`. |
| `Authentication__JwtBearer__SigningKey` | Unset or empty in production. |

Verification commands:

```bash
dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~AuthenticationConfigurationTests
dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~TenantsQueryControllerIntegrationTests|FullyQualifiedName~CommandApiRuntimeIntegrationTests"
```

Manual production-like checks may use:

```bash
curl -i -H "Authorization: Bearer <redacted-access-token>" https://<tenants-host>/api/tenants
```

Record only status code, safe reason code, timestamp, and redacted operator aliases. Do not store token text, decoded token payloads, private hosts, raw command/event payloads, or returned tenant data.

## Local Development Token Boundary

Local HMAC and local Keycloak examples are valid developer setup aids, not production readiness proof. Use them only to bootstrap AppHost or deterministic-local checks. The production evidence record must say whether the run used `deterministic-local`, `prepared-apphost`, `production-like`, or `production`, and it must not classify local token success as production IdP readiness.

## DAPR Components

Required DAPR prerequisites are documented in [deploy/dapr](../deploy/dapr/README.md). Local proof requires Docker when using the AppHost defaults. Full local init provides Redis, placement, and scheduler. Slim mode is acceptable only when the operator supplies equivalent services before actor flows start.

Required DAPR controls:

| Control | Expected value or behavior |
| --- | --- |
| AppIds | `eventstore`, `tenants`, `eventstore-admin`, `eventstore-admin-ui`, and `sample`. |
| components | `statestore` for actor state and `pubsub` for tenant events. |
| topic | `tenants.events`. |
| dead letter | `deadletter.tenants.events`. |
| placement | Available before actor command flows are claimed live. |
| scheduler | Available before DAPR actor reminders and recovery flows are claimed live. |
| state-store scopes | Include `eventstore`, `eventstore-admin`, and `tenants` where required by the deployment template. |
| pub/sub scopes | Include `eventstore` publisher and `sample` subscriber where required by the deployment template. |
| access control | Production uses receiver-specific deny-by-default access control. |
| sidecar ports | Use dynamic sidecar ports; no fixed DAPR sidecar ports are part of the Tenants readiness contract. |

Verification commands:

```bash
dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~EventPublicationConfigurationTests
dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~DaprTestPrerequisiteDiagnosticsTests|FullyQualifiedName~DaprEndToEndTests"
```

If live DAPR prerequisites are missing, classify the evidence as `environment-blocker` or `not-claimable` for the live row. Do not convert prerequisite skips into passing deployment proof.

## Service Invocation

Production service invocation controls:

| Control | Expected value or behavior |
| --- | --- |
| Tenants receiver | `eventstore` is the only production caller allowed to Tenants `POST /process` and `POST /project`. |
| domain service registration | `system|tenants|v1` routes to AppId `tenants` method `process`. |
| global administrator registration | `system|global-administrators|v1` routes to AppId `tenants` method `process`. |
| denied caller behavior | Unexpected DAPR callers are denied by the receiving service access-control policy. |

Evidence for this section can reference Story 7.6B deterministic configuration tests and any same-run live DAPR/AppHost smoke tests that actually executed.

## Health Endpoints

Required health/readiness controls:

| Endpoint | Meaning | Evidence boundary |
| --- | --- | --- |
| `/alive` | Process liveness. | Does not prove dependency readiness. |
| `/ready` | Dependency readiness for the ready-tagged DAPR state-store check. | Unhealthy readiness returns HTTP 503. |
| `/health` | Diagnostic health output. | Output must remain support-safe. |

Live command/query path evidence is separate from `/ready`. A healthy `/ready` response does not prove protected command submission, protected query routes, EventStore command processing, or pub/sub delivery.

Verification commands:

```bash
dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~HealthEndpointsTests|FullyQualifiedName~TenantsQueryControllerIntegrationTests|FullyQualifiedName~CommandApiRuntimeIntegrationTests"
```

## Pub/Sub Recovery

Required recovery controls:

| Control | Expected value or behavior |
| --- | --- |
| source of truth | EventStore remains the source of truth for accepted commands and persisted tenant events. |
| publication failure | `PublishFailed` means events were stored but not yet published. |
| drain recovery | Recovery republishes persisted events after publication becomes available. |
| DAPR delivery | DAPR pub/sub is at-least-once, not exactly-once. |
| subscriber catch-up | Subscriber catch-up must be backed by live evidence or documented idempotency evidence. |
| idempotency | Consumers use message identifiers and stored progress to tolerate redelivery. |

Use [Event Contract Reference](event-contract-reference.md), [Cross-Aggregate Timing](cross-aggregate-timing.md), [Idempotent Event Processing](idempotent-event-processing.md), and Story 7.6D evidence as the source material. Do not claim live subscriber catch-up unless a live assertion ran in the target environment.

## AppHost and Operator Controls

Operator controls:

| Control | Required check |
| --- | --- |
| DAPR init mode | Full init or equivalent slim-mode prerequisites are available before live actor/readiness proof. |
| Docker/AppHost prerequisites | Local proof records Docker and AppHost prerequisite availability. |
| local fallback boundary | `EnableKeycloak=false` is a local development fallback only. |
| production override locations | Production values are supplied through deployment environment variables, secret providers, or platform-specific AppHost overrides. |
| sidecar ports | Tenants readiness does not require fixed DAPR sidecar ports. |
| submodules | Initialize only root-declared submodules under `references/`; no recursive submodule initialization. |

## Evidence Template

Copy this template into the deployment evidence record for each environment. Replace placeholders before using it as release evidence.

```yaml
template_version: tenants-deployment-readiness-template/v1
environment_alias: <environment-alias>
run_datetime_utc: <yyyy-mm-ddThh:mm:ssZ>
commit_sha_or_package_version: <commit-sha-or-package-version>
operator_alias: <operator-alias>
reviewer_alias: <reviewer-alias>
run_profile: deterministic-local | prepared-apphost | production-like | production
final_classification: pass | environment-blocker | product-failure | configuration-gap | instrumentation-gap | documentation-gap | not-claimable
evidence_source_links:
  - <link-to-redacted-test-summary-or-release-evidence>
redaction_statement: Evidence was reviewed and redacted before storage.
reviewer_verdict: approved | rejected | needs-follow-up
```

Per-control evidence rows:

| Control row | Classification | Evidence source | Live evidence boundary | Reviewer notes |
| --- | --- | --- | --- | --- |
| auth | pass / environment-blocker / product-failure / configuration-gap / instrumentation-gap / documentation-gap / not-claimable | <link-or-command> | <what was live, skipped, or deterministic> | <safe note> |
| DAPR components | pass / environment-blocker / product-failure / configuration-gap / instrumentation-gap / documentation-gap / not-claimable | <link-or-command> | <what was live, skipped, or deterministic> | <safe note> |
| service invocation | pass / environment-blocker / product-failure / configuration-gap / instrumentation-gap / documentation-gap / not-claimable | <link-or-command> | <what was live, skipped, or deterministic> | <safe note> |
| health/readiness | pass / environment-blocker / product-failure / configuration-gap / instrumentation-gap / documentation-gap / not-claimable | <link-or-command> | <what was live, skipped, or deterministic> | <safe note> |
| command path | pass / environment-blocker / product-failure / configuration-gap / instrumentation-gap / documentation-gap / not-claimable | <link-or-command> | <what was live, skipped, or deterministic> | <safe note> |
| query path | pass / environment-blocker / product-failure / configuration-gap / instrumentation-gap / documentation-gap / not-claimable | <link-or-command> | <what was live, skipped, or deterministic> | <safe note> |
| pub/sub recovery | pass / environment-blocker / product-failure / configuration-gap / instrumentation-gap / documentation-gap / not-claimable | <link-or-command> | <what was live, skipped, or deterministic> | <safe note> |
| evidence boundaries | pass / environment-blocker / product-failure / configuration-gap / instrumentation-gap / documentation-gap / not-claimable | <link-or-command> | live_evidence_boundary: skipped DAPR/AppHost tests are not passing deployment proof | <safe note> |

Redaction checklist:

- [ ] No compact JWTs.
- [ ] No bearer tokens.
- [ ] No signing keys.
- [ ] No decoded token payloads.
- [ ] No raw command/event payloads.
- [ ] No private hosts.
- [ ] No concrete connection strings.
- [ ] No real tenant/user identifiers.
- [ ] No PII.

## Final Review

The reviewer verdict should state whether the deployment is approved, rejected, or needs follow-up. A final `pass` classification requires every required control row to have same-run evidence, no unresolved blockers, and no skipped live checks being counted as proof.
