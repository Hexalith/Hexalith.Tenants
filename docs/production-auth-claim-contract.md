[Back to README](../README.md)

# Production Auth Claim Contract

Hexalith.Tenants uses EventStore authentication and authorization infrastructure for protected command and query requests. Production identity providers must emit claims that authorize the platform tenant context used by tenant-management operations.

## Required Downstream Claims

Tenant-management commands and queries run against the EventStore tenant `system` and domain `tenants`. After EventStore claims transformation, the effective authenticated principal must include:

| Claim | Required value | Purpose |
| --- | --- | --- |
| `sub` | Stable subject ID | Authenticated user identifier used by EventStore and Tenants. Do not use `name` as the trusted subject. |
| `eventstore:tenant` | `system` | Authorizes access to the platform tenant used to manage tenant records. |
| `eventstore:domain` | `tenants` | Authorizes the Tenants domain when domain claims are present. |
| `eventstore:permission` | `command:submit`, `command:query`, `command:replay`, `commands:*`, `queries:*`, `query:read`, or a specific command/query type as needed | Authorizes command and query categories when permission claims are present. |

For Tenants deployment, `eventstore:tenant=system` is the safest production contract. A non-global-admin token with a missing, blank, or wrong `eventstore:tenant` claim fails closed with `403 Forbidden` before command/query dispatch.

## Supported Source Claims

An identity provider may emit the downstream `eventstore:tenant` claim directly. EventStore can also normalize these source claim shapes into `eventstore:tenant`:

| Source claim | Supported value shape | Normalized claim |
| --- | --- | --- |
| `tenants` | JSON array, for example `["system"]` | One `eventstore:tenant` claim per non-empty entry |
| `tenants` | Space-delimited string, for example `system tenant-a` | One `eventstore:tenant` claim per token |
| `tenant_id` | Single tenant ID | One `eventstore:tenant` claim |
| `tid` | Single tenant ID fallback when `tenant_id` is absent | One `eventstore:tenant` claim |

Use one authoritative mapping style per token. Current `EventStoreClaimsTransformation` treats any existing `eventstore:*` claim as evidence that the principal is already normalized, so source claims are not merged afterward. For example, a token with `eventstore:tenant=" "` and `tenants=["system"]` still fails tenant authorization because the blank downstream claim is not repaired by the source alias.

Duplicate tenant values are tolerated, but they do not add permissions. Whitespace-only tenant values are ignored by tenant authorization and fail closed when no non-empty matching tenant remains.

## Keycloak Mapping

The local Aspire Keycloak realm maps user attributes directly to EventStore downstream claims:

| Keycloak user attribute | Token claim |
| --- | --- |
| `tenants` | `eventstore:tenant` |
| `domains` | `eventstore:domain` |
| `permissions` | `eventstore:permission` |

For a production realm, configure equivalent mappers and verify that the access token includes `eventstore:tenant=system` for tenant-management operators. The sample realm under `src/Hexalith.Tenants.AppHost/KeycloakRealms/hexalith-realm.json` is local implementation evidence only; do not copy sample users, passwords, or environment details into production.

## Verification Steps

1. Decode an access token without sending it to external services. Confirm `iss`, `aud`, `sub`, and either the direct downstream `eventstore:tenant` claim or exactly one supported source tenant mapping.
2. Confirm the effective principal after EventStore claims transformation contains `eventstore:tenant=system`.
3. Confirm tenant-management command tokens include `eventstore:domain=tenants` and the needed command/query permission claims when your IdP emits domain or permission claims.
4. Run the focused contract tests:

```bash
dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantClaimContractTests
dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~TenantsQueryControllerIntegrationTests|FullyQualifiedName~CommandApiRuntimeIntegrationTests"
```

5. Verify negative cases: non-global-admin tokens with missing, blank, or wrong tenant claims must return `403 Forbidden` and must not reach command/query dispatch.

Global administrators may bypass tenant matching in EventStore authorization. Do not use global-admin bypass tests as proof that ordinary tenant-management operator tokens have the correct tenant partition claim.

## Rate-Limit Boundary

EventStore rate limiting, where the full EventStore server extension is registered, partitions traffic by the first `eventstore:tenant` claim and falls back to `anonymous` when no tenant claim exists.

The Tenants host intentionally registers EventStore domain/client services directly and does not register the full EventStore server extension or global rate limiter. In Tenants, protected command and query requests fail closed through EventStore authorization before dispatch. Executable rate-limit partition coverage belongs at the EventStore host boundary or deployment smoke-test scope.
