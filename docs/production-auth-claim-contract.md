[Back to README](../README.md)

# Production Auth Claim Contract

Hexalith.Tenants uses EventStore authentication and authorization infrastructure for protected command and query requests. Production identity providers must emit claims that authorize the platform tenant context used by tenant-management operations.

## Required Downstream Claims

Tenant-management commands and queries run against the EventStore tenant `system` and domain `tenants`. The request tenant must be `system`; managed tenant IDs belong in the aggregate ID, route, query payload, or command payload depending on the operation. After EventStore claims transformation, the effective authenticated principal must include:

| Claim | Required value | Purpose |
| --- | --- | --- |
| `sub` | Stable subject ID | Authenticated user identifier used by EventStore and Tenants. Do not use `name` as the trusted subject. |
| `eventstore:tenant` | `system` | Authorizes access to the platform tenant used to manage tenant records. |
| `eventstore:domain` | `tenants` | Authorizes the Tenants domain when domain claims are present. |
| `eventstore:permission` (command path) | `command:submit`, `commands:*`, or an exact command type token (for example `BootstrapGlobalAdmin`) | Authorizes the command submission path. Listed in match precedence order (`commands:*` wildcard first, then `command:submit`, then exact type). |
| `eventstore:permission` (query path) | `query:read`, `queries:*`, the legacy `command:query`, or an exact query type token (for example `list-tenants`) | Authorizes the query routing path. `command:query` is the explicit `LegacyQueryPermission` constant in `Hexalith.EventStore/src/Hexalith.EventStore/Authorization/ClaimsRbacValidator.cs` (still accepted for back-compat with the local sample realm at `src/Hexalith.Tenants.AppHost/KeycloakRealms/hexalith-realm.json`). New IdP integrations should emit `query:read` rather than the legacy shape. |

For Tenants deployment, `eventstore:tenant=system` is the safest production contract. A non-global-admin token with a missing, blank, or wrong `eventstore:tenant` claim fails closed with `403 Forbidden` before command/query dispatch.

### Global Administrators

`GlobalAdministratorHelper` recognizes a principal as a global administrator when the token contains any of:

- `global_admin=true` or `is_global_admin=true` (boolean claims, parsed by `bool.TryParse`)
- `role` (or `ClaimTypes.Role`) equal to `GlobalAdministrator`, `global-administrator`, or `global-admin`
- `roles` JSON array or space/comma-delimited string containing any of those role values

EventStore's shared `ClaimsTenantValidator` and `ClaimsRbacValidator` still recognize those global-administrator shapes as a bypass for generic EventStore tenant/RBAC matching. The Tenants host adds a narrower production guard before that shared validator: protected Tenants command and query requests must still target the `system` request tenant and have an effective non-blank `eventstore:tenant=system` claim, including global-administrator requests. A global-admin token without that claim, or with a non-`system` request tenant, fails closed with `403 Forbidden` before command/query dispatch. The rate-limit fallback to the `anonymous` partition does NOT apply on the Tenants host today because EventStore rate limiting is not registered (see [Rate-Limit Boundary](#rate-limit-boundary)).

## Identifier Casing Contract

`sub` (the authenticated user identifier) and the managed `tenantId` are compared **case-sensitively** (`StringComparer.Ordinal`) throughout Hexalith.Tenants — membership keys (`TenantLocalState.Members`, `TenantState.Users`), projection lookups, and event deduplication.

- **Rationale:** OIDC `sub` is case-sensitive per spec. Case-folding identifiers could collapse two genuinely distinct subjects into one membership entry — a silent privilege merge. Tenants therefore does **not** normalize casing internally.
- **Identity-provider / operator obligation:** the IdP MUST emit a stable, canonically-cased `sub`, and the casing present when an administrator issues `AddUserToTenant` MUST match the casing consuming services later observe for that principal. Managed tenant IDs are operator-assigned and MUST be referenced with identical casing across EventStore registration, the `eventstore:tenant` claim, and event payloads. Convention for new tenant IDs: lowercase kebab-case (for example `acme-corp`).
- **Mismatch is fail-closed by design:** a casing difference surfaces as an unknown tenant or a missing member. Resolve it by aligning the IdP/operator casing at the source — **not** by relaxing the comparer to `OrdinalIgnoreCase`.
- **Consumer guidance:** consuming services (for example Parties) should rely on this published contract rather than compensating with claims case-folding.

## Supported Source Claims

An identity provider may emit the downstream `eventstore:tenant` claim directly (recommended for production — see [Keycloak Mapping](#keycloak-mapping-direct-claim-shape) below). EventStore can also normalize these source claim shapes into `eventstore:tenant`:

| Source claim | Supported value shape | Normalized claim |
| --- | --- | --- |
| `tenants` | JSON array, for example `["system"]` | One `eventstore:tenant` claim per non-empty entry |
| `tenants` | Space-delimited string, for example `system tenant-a` | One `eventstore:tenant` claim per non-empty part |
| `tenant_id` | Single tenant ID | One `eventstore:tenant` claim |
| `tid` | Single tenant ID, fallback when `tenant_id` is absent | One `eventstore:tenant` claim |

### Mixed Source-Claim Precedence

`EventStoreClaimsTransformation` is idempotent: when the token already carries any `eventstore:*` claim (including a blank or whitespace-only value), source claims are NOT merged afterward. For mixed-source tokens, the documented effective principal is:

- **`tenants` + `tenant_id` (or `tid`):** both contribute. `tenants` is processed first and emits one claim per non-empty part; `tenant_id` (or `tid` when `tenant_id` is absent) is then appended unless it duplicates a value already emitted.
- **`tenant_id` + `tid`:** only `tenant_id` is used. `tid` is silently dropped — it is a fallback for IdPs that emit only `tid`, not an additive claim.
- **Direct `eventstore:tenant` + any source claim:** all source claims are ignored. A blank or whitespace-only direct claim is NOT repaired by source aliases. Fix the IdP mapping at the source.

**Operators should emit exactly one source-claim shape per token.** Mixing shapes is supported but produces multi-tenant principals that are easy to misread.

Duplicate tenant values from a single source claim are retained on the principal but do not grant additional permissions. Whitespace-only tenant claims survive normalization (they are not filtered out) and remain on the principal until `ClaimsTenantValidator` rejects them at authorization (fail-closed). Operators inspecting the effective principal in diagnostics or audit logs will see the literal whitespace claim until validation runs.

## Keycloak Mapping (Direct-Claim Shape)

The local Aspire Keycloak realm is the authoritative local sample. It uses Keycloak attribute mappers to emit `eventstore:*` claims directly into the access token — no source-claim normalization happens at runtime.

| Keycloak user attribute (realm profile field) | Direct token claim emitted into the JWT |
| --- | --- |
| `tenants` user attribute | `eventstore:tenant` JWT claim |
| `domains` user attribute | `eventstore:domain` JWT claim |
| `permissions` user attribute | `eventstore:permission` JWT claim |

For a production realm, configure equivalent direct-claim mappers and verify that the access token includes `eventstore:tenant=system` for tenant-management operators. The sample realm under `src/Hexalith.Tenants.AppHost/KeycloakRealms/hexalith-realm.json` is local implementation evidence only; do not copy sample users, passwords, or environment details into production.

### Alternative: Source-Claim Shape

IdPs that cannot emit `eventstore:*` claims directly may emit the source claims listed in [Supported Source Claims](#supported-source-claims). `EventStoreClaimsTransformation` normalizes them into the downstream `eventstore:tenant` claim at request time. Choose one shape per IdP; do not mix direct and source mappings on the same token.

## Verification Steps

1. Decode an access token without sending it to external services. Confirm `iss`, `aud`, `sub`, and either the direct downstream `eventstore:tenant` claim or exactly one supported source tenant mapping.
2. Confirm the effective principal after EventStore claims transformation contains `eventstore:tenant=system`.
3. Confirm tenant-management command tokens include `eventstore:domain=tenants` and the needed command/query permission claims when your IdP emits domain or permission claims.
4. Run the focused contract tests:

```bash
dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantClaimContractTests
dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~TenantsQueryControllerIntegrationTests|FullyQualifiedName~CommandApiRuntimeIntegrationTests"
```

5. Verify negative cases. Tokens with missing, blank, wrong-cased, or wrong tenant claims, including global-administrator-shaped tokens, must return `403 Forbidden` and must not reach command/query dispatch. Requests using a non-`system` request tenant must also fail before dispatch. The ProblemDetails `reasonCode` extension distinguishes the failure mode: `principal_not_member` for missing/blank tenant claims, `tenant_mismatch` for wrong or wrong-cased tenant claims and non-`system` request tenants.

## Rate-Limit Boundary

**The Tenants host does NOT register the EventStore rate limiter today.** The Tenants host registers EventStore domain/client services directly and intentionally avoids the full EventStore server extension. The partitioning behavior described below applies to the EventStore host boundary only — it is included here for operators routing through both services.

EventStore rate limiting, where the full EventStore server extension is registered, partitions traffic by the first `eventstore:tenant` claim and falls back to `anonymous` when no tenant claim exists.

In Tenants, protected command and query requests fail closed through EventStore authorization before dispatch. Executable rate-limit partition coverage belongs at the EventStore host boundary or deployment smoke-test scope.
