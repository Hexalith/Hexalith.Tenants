[Back to README](../README.md)

# Production Auth Readiness

Use this guide before promoting Hexalith.Tenants to a production-like environment. It turns the Story 11.1 JWT validation contract and the Story 11.2 tenant-claim contract into operator checks and smoke-test evidence.

This page is not a production IdP provisioning guide. It assumes your deployment platform supplies configuration overrides and your IdP issues access tokens. Keep local development token generation in [Quickstart](quickstart.md), and keep the production claim mapping detail in [Production Auth Claim Contract](production-auth-claim-contract.md).

## Required Production Settings

Production authentication uses OIDC discovery. The committed `src/Hexalith.Tenants/appsettings.json` contains placeholders and is expected to fail startup validation until deployment overrides supply real values.

| .NET key | Environment variable | Production expectation | Evidence | Redaction rule |
| --- | --- | --- | --- | --- |
| `Authentication:JwtBearer:Authority` | `Authentication__JwtBearer__Authority` | Absolute HTTPS OIDC authority, for example `<https-oidc-authority>` | Startup validation succeeds only when the value is present and HTTPS. | Do not commit real internal authorities in transcripts. Replace with `<https-oidc-authority>`. |
| `Authentication:JwtBearer:Issuer` | `Authentication__JwtBearer__Issuer` | Exact token `iss` value expected from the IdP | Token inspection shows `iss` equals the configured value. | Redact environment-specific issuer hosts in committed evidence. |
| `Authentication:JwtBearer:Audience` | `Authentication__JwtBearer__Audience` | Exact token `aud` value accepted by Tenants | Token inspection shows `aud` equals the configured value. | Redact customer-specific audience values when they identify a private deployment. |
| `Authentication:JwtBearer:RequireHttpsMetadata` | `Authentication__JwtBearer__RequireHttpsMetadata` | `true` | Startup validation rejects `false` in `Production`. | No secret data. Record only pass/fail. |
| `Authentication:JwtBearer:SigningKey` | `Authentication__JwtBearer__SigningKey` | Empty or unset | Startup validation rejects any production signing key. | Never print, log, or commit signing-key values. |

ASP.NET Core configuration providers are applied in order, so later providers override earlier `appsettings` values. Use double underscores in environment variables because they are the portable hierarchical-key separator across shells and container platforms.

## Required Token Contents

Inspect access-token header and payload only in a trusted local tool. Do not upload production tokens to external decoders, paste full bearer tokens into logs, or commit decoded token output.

Expected decoded payload shape:

```json
{
  "iss": "<expected-issuer>",
  "aud": "hexalith-tenants",
  "sub": "<redacted-subject>",
  "exp": "<future-unix-timestamp>",
  "eventstore:tenant": "system",
  "eventstore:domain": "tenants",
  "eventstore:permission": "command:submit"
}
```

`exp` is a Unix timestamp (seconds since epoch) in real tokens; the placeholder is shown as a string so it cannot accidentally read as expired evidence in committed docs.

Expected evidence: the payload includes `iss`, `aud`, `sub`, `exp`, and either direct `eventstore:tenant=system` or one supported source tenant claim that EventStore normalizes into `eventstore:tenant=system`.

Redaction rule: replace the subject, issuer host, token IDs, and any organization-specific tenant/user values with placeholders before storing evidence. Never store the full compact JWT.

Tenant-management operations run in the platform tenant context `system`. The managed tenant ID in a command payload is separate from the `eventstore:tenant=system` authorization context.

## IdP Claim Contract

Production IdPs should emit direct EventStore claims when possible:

| Claim | Expected value for tenant-management operators | What it proves |
| --- | --- | --- |
| `sub` | Stable non-empty subject ID | EventStore and Tenants have a trusted caller identity. |
| `eventstore:tenant` | `system` | The caller can access the platform tenant context. |
| `eventstore:domain` | `tenants` | The caller can access the Tenants domain when domain claims are enforced. |
| `eventstore:permission` | `command:submit`, `commands:*`, `query:read`, `queries:*`, or an exact command/query type accepted by EventStore | The caller has the required command or query permission. |

If your IdP cannot emit `eventstore:*` claims directly, EventStore can normalize supported source claims such as `tenants`, `tenant_id`, or `tid`. Choose one mapping style per IdP. Direct `eventstore:*` claims are treated as already normalized and source claims are not merged afterward.

The local Aspire Keycloak realm at `src/Hexalith.Tenants.AppHost/KeycloakRealms/hexalith-realm.json` is sample evidence only. It maps local sample attributes to direct `eventstore:tenant`, `eventstore:domain`, and `eventstore:permission` claims. Do not treat the sample users, passwords, realm settings, or `sslRequired` value as production guidance.

## Deployment Readiness Checklist

Run these checks before release. Store only the pass/fail result, test name, HTTP status, and safe reason code.

| Check | Pass condition | Fail condition | Evidence | Redaction rule |
| --- | --- | --- | --- | --- |
| Production startup placeholders | Deployment overrides provide `Authority`, `Issuer`, `Audience`, and `RequireHttpsMetadata=true`. | Startup/options validation names a missing or invalid `Authentication:JwtBearer` key. | `AuthenticationConfigurationTests` or deployment startup log shows the named key. | Do not include signing-key values, bearer tokens, or full authority hosts. |
| HTTPS authority | `Authority` is absolute HTTPS. | Empty, whitespace, relative, malformed, or HTTP authority fails before OIDC discovery. | Startup validation names `Authentication:JwtBearer:Authority`. | Record only the category, not the exact internal URL. |
| Signing source | Production uses OIDC `Authority`; `SigningKey` is empty or unset. | Any production signing key, or `Authority` plus `SigningKey`, fails as ambiguous. | Startup validation names `Authentication:JwtBearer:SigningKey`. | Never print the signing key. |
| HTTPS metadata | `RequireHttpsMetadata=true`. | `false` fails in `Production`. | Startup validation names `Authentication:JwtBearer:RequireHttpsMetadata`. | No secret data. |
| Token issuer | Token `iss` equals configured `Issuer`. | Wrong issuer returns `401 Unauthorized`. | Smoke test or manual request returns 401 at authentication. | Do not commit decoded production tokens. |
| Token audience | Token `aud` equals configured `Audience`. | Wrong audience returns `401 Unauthorized`. | Smoke test or manual request returns 401 at authentication. | Do not commit decoded production tokens. |
| Token subject | Token has non-empty `sub`. | Missing subject fails authentication or authorization before dispatch. | ProblemDetails reason code, when present, is safe. | Redact subject IDs. |
| Tenant claim | Effective principal contains `eventstore:tenant=system`, including global-administrator operators, and protected requests use the `system` request tenant. | Missing, blank, wrong-cased, wrong tenant claim, or non-`system` request tenant returns `403 Forbidden` before dispatch. | ProblemDetails `reasonCode` is `principal_not_member` or `tenant_mismatch`. | Redact real tenant and user identifiers. |
| Query endpoint | A valid token can call one protected Tenants query endpoint, such as `GET /api/tenants`. | Invalid token returns 401; wrong tenant returns 403; query router is not invoked for denied calls. | `TenantsQueryControllerIntegrationTests` or manual HTTP status. | Do not store bearer tokens or full response data from production tenants. |
| Command endpoint | A valid token can reach `POST /api/v1/commands` when EventStore command infrastructure is available. | Missing/wrong auth returns 401 or 403 before command routing. | `CommandApiRuntimeIntegrationTests` or operator-run deployment test. | Use placeholder command IDs and non-production tenant data in transcripts. |
| Rate-limit partition | If routed through a host that registers EventStore rate limiting, partitioning uses normalized subject/client and tenant context and does not log tokens. | Tenants host alone cannot prove this today because it does not register the EventStore rate limiter. | Record EventStore-host evidence or note the Tenants boundary deferral. | Do not log token contents or raw tenant/user identifiers. |

## Deterministic Local Smoke Tests

These tests are deterministic local evidence. They do not call Keycloak, Entra ID, OIDC discovery, DAPR sidecars, Redis, Docker, or Aspire orchestration.

```bash
dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantsQueryControllerIntegrationTests
```

Expected evidence: valid production-like smoke tokens reach protected query endpoints; missing, malformed, invalid signature, wrong issuer, wrong audience, expired, missing tenant, blank tenant, and wrong tenant cases fail with 401 or 403 as appropriate.

Redaction rule: keep the test transcript limited to command, test names, pass/fail count, and safe status/reason-code assertions. Do not print generated JWTs or signing material.

```bash
dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~CommandApiRuntimeIntegrationTests
```

Expected evidence: valid real-JWT command requests reach the mocked command router, while missing, blank, wrong-cased, wrong, global-administrator missing-tenant, or non-`system` request-tenant cases fail before routing.

Redaction rule: test command IDs are generated; do not replace them with production correlation IDs or payloads in committed evidence.

```bash
dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~AuthenticationConfigurationTests
```

Expected evidence: production startup/options validation fails safely for missing placeholders, whitespace values, non-HTTPS authority, `RequireHttpsMetadata=false`, and production `SigningKey`, and succeeds for valid OIDC-style overrides.

Redaction rule: validation assertions must name configuration keys only. They must not echo signing keys, bearer tokens, decoded payloads, or secret values.

## Manual Deployment Smoke Checks

Use these only against an environment approved for release verification.

```bash
curl -i -H "Authorization: Bearer <redacted-access-token>" https://<tenants-host>/api/tenants
```

Expected evidence: `200 OK` for an authorized operator token, `401 Unauthorized` for invalid authentication, or `403 Forbidden` for a valid token without `eventstore:tenant=system`. This proves the protected query path.

Redaction rule: do not store the bearer token, full host name, returned tenant data, or full response body in committed artifacts. Store only status code, safe reason code, and test timestamp.

```bash
curl -i \
  -H "Authorization: Bearer <redacted-access-token>" \
  -H "Content-Type: application/json" \
  -d '{"messageId":"<ulid>","tenant":"system","domain":"global-administrators","aggregateId":"global-administrators","commandType":"BootstrapGlobalAdmin","payload":{"userId":"<redacted-subject>"}}' \
  https://<tenants-host>/api/v1/commands
```

`<ulid>` is a placeholder. Replace it with the output of `Ulid.NewUlid().ToString()` (or any other compliant ULID generator) before sending — the EventStore controller rejects the literal placeholder because `messageId` must parse as a ULID, not the surrounding shape.

Expected evidence: `202 Accepted` when command infrastructure is available and the token has command authorization, or a safe `401`/`403` before routing when authentication or authorization is wrong. This proves the command gateway path, not domain business success.

Redaction rule: use non-production subjects and placeholder IDs in stored examples. Do not store bearer tokens, production user identifiers, or command payloads containing real tenant data.

## Failure Triage

| Symptom | Likely layer | Safe next check |
| --- | --- | --- |
| Startup fails before listening | Options validation | Check named `Authentication:JwtBearer` key. |
| `401 Unauthorized` with `WWW-Authenticate: Bearer error="invalid_token", error_description="The token expired ..."` | JWT authentication (lifetime) | Check token `exp`, the host's `ClockSkew`, and the IdP's clock. |
| `401 Unauthorized` with `WWW-Authenticate: Bearer error="invalid_token"` (no expiration error_description) | JWT authentication (signature/issuer/audience) | Check signing source, `iss`, `aud`, and signature algorithm. |
| `401 Unauthorized` with `WWW-Authenticate: Bearer` (no error attribute) or no header | JWT authentication (missing/malformed token) | Confirm the `Authorization: Bearer …` header is present and the bearer value parses as a JWT. |
| `403 Forbidden` with `principal_not_member` | Tenant authorization | Check effective `eventstore:tenant=system` after claims transformation. |
| `403 Forbidden` with `tenant_mismatch` | Tenant authorization | Align request tenant with the effective tenant claim. |
| Command smoke cannot prove routing | Deployment boundary | Confirm EventStore/DAPR command infrastructure is running, or record this as infrastructure evidence outside deterministic smoke tests. |
| Rate-limit partition cannot be observed in Tenants | Host boundary | Tenants does not register the EventStore rate limiter today; prove partitioning at the EventStore host boundary. |

## Evidence Map

| Acceptance criterion | Evidence source |
| --- | --- |
| AC1 required JWT settings, IdP claims, environment variables, AppHost/deployment overrides | [Required Production Settings](#required-production-settings), [Required Token Contents](#required-token-contents), [IdP Claim Contract](#idp-claim-contract), README and quickstart links |
| AC2 issuer, audience, `eventstore:tenant`, HTTPS metadata, signing/authority source, rate-limit partitioning | [Deployment Readiness Checklist](#deployment-readiness-checklist) |
| AC3 valid and invalid token smoke tests | `TenantsQueryControllerIntegrationTests` and `CommandApiRuntimeIntegrationTests` |
| AC4 missing or invalid overrides fail safely | `AuthenticationConfigurationTests` and [Failure Triage](#failure-triage) |
| AC5 local development docs remain separate | [Quickstart](quickstart.md) remains local HMAC-only; this page references it without moving production OIDC setup into it |

## Recorded Deployment Boundaries

- The deterministic smoke tests use real ASP.NET Core JWT bearer middleware but a local production-like signing seam to avoid a live IdP dependency.
- The command endpoint can be tested through a mocked command router locally. Full command processing still requires EventStore/DAPR infrastructure and belongs to deployment smoke verification.
- The Tenants host does not register the EventStore rate limiter today. Executable rate-limit partition evidence belongs at the EventStore host boundary or later deployment automation.
- Vendor-specific Keycloak, Entra ID, Helm, Aspire publish, and cloud deployment manifests remain deferred deployment automation. This story records the contract they must satisfy.
