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
| `Authentication:JwtBearer:AllowedAlgorithms:0` | `Authentication__JwtBearer__AllowedAlgorithms__0` | Explicit supported asymmetric JWT algorithm, such as `RS256`, matching the IdP signing keys | Startup validation rejects a missing, blank, symmetric, or unsupported algorithm. | Record the algorithm name only; do not store signing material. |
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

## Global Administrator Bootstrap Credential

When `Tenants:BootstrapGlobalAdminUserId` is set, the Tenants host submits `BootstrapGlobalAdmin` to EventStore once it is listening. EventStore no longer admits any internal caller by its Dapr application id (EventStore Story 5.5), so the command is authorized only by the configured administrator's own delegated credential:

| Mode | Credential | Settings |
| --- | --- | --- |
| Authority (production) | The administrator's own access token, obtained with the resource-owner password grant. It is sent only when its `sub` equals `Tenants:BootstrapGlobalAdminUserId`. | `EventStore:Authentication:Authority`, `EventStore:Authentication:ClientId`, `EventStore:Authentication:Username`, `EventStore:Authentication:Password`, optional `EventStore:Authentication:TokenEndpoint` |
| Symmetric (`Development` only) | A 120-second token for the configured administrator, signed with the shared development key. | `Authentication:JwtBearer` symmetric contract |

`EventStore:Authentication:Authority` remains required for authority mode and must be an absolute HTTP(S) URI with a host and no userinfo, query or fragment. HTTPS is required in every environment except `Development`.

The optional `EventStore:Authentication:TokenEndpoint` (`EventStore__Authentication__TokenEndpoint`) selects the password-grant endpoint for providers whose endpoint differs from Keycloak's. If absent, bootstrap uses `<authority>/protocol/openid-connect/token`; there is no OIDC endpoint discovery. A configured endpoint must be an absolute, well-formed HTTP(S) URI with a host and no userinfo, query or fragment; HTTPS is required outside `Development`. Blank/invalid/insecure configured overrides fail closed without a request or default-endpoint fallback. An override cannot bypass an invalid authority, missing credentials or subject mismatch, and cannot activate symmetric Development token fallback.

**The configured endpoint is an operator-trusted credential destination.** It may have a different host/path from the authority. Verify that destination before supplying credentials: the provider sends the configured username and password there. The access token must name exactly `Tenants:BootstrapGlobalAdminUserId`; EventStore still validates token authentication, and the aggregate binds bootstrap target to authenticated actor. Response size remains bounded to 64 KiB, body reads share the request timeout/cancellation, and diagnostics contain safe reason/status categories only.

Credential acquisition never follows HTTP redirects, including same-origin redirects. A 301, 302, 303, 307 or 308 response fails the credential attempt without a request to its destination or a bootstrap command. Configure the final trusted token URL explicitly; do not rely on redirect aliases. This credential-only client policy does not change the HTTP client used for EventStore command dispatch.

`Tenants:BootstrapGlobalAdminEnabled` defaults to `true`. When enabled with a configured user ID, the hosted service makes **one startup attempt** after listening; it does not retry in process. Set `Tenants__BootstrapGlobalAdminEnabled=false` to suppress another startup submission while recovering uncertain delivery. This preserves the configured user ID and existing state. A later restart with bootstrap enabled makes one new attempt, so do not restart blindly after failure.

In production, supply the administrator password from a secret store for the bootstrap run only and remove it afterwards. If the IdP does not permit password grants, disable hosted bootstrap and have the designated administrator submit the one-time command with their own IdP-issued token, correct system/domain/permission claims and a supported global-administrator claim. The target must equal that authenticated subject. Never fabricate a token or change the administrator target to work around failed credentials.

Global-administrator authority is verified from the `global-administrators` read model after `GlobalAdministratorSet` has been projected. Wire administrator hints alone do not grant domain authority. Use the [quickstart membership and human handoff commands](quickstart.md#get-an-access-token) against `tenants-api`; a successful full-page membership lookup under the configured subject is the authoritative handoff check.

### Unexpected-response diagnostics

Bootstrap warning `2003` records the actual `StatusCode` plus `ResponseKind`,
`ResponseCode` and `ValidationField`. These are closed diagnostic classifications, never delivery,
non-delivery, authorization, or administrator-membership authority. Keep the
attempt IDs from event `2008` and follow the status and membership recovery
checks below; a classification does not authorize another submission or restart.

`ResponseKind` identifies a recognized SDK `problem-*` family or `dapr`. Unknown
problem types use `problem-other`; ordinary JSON objects and other JSON values
use `json-object` and `json-other`. Empty, whitespace, unreadable or oversized
probe input uses `unavailable`; malformed JSON, escaped scalar data or excessive
nesting uses `non-json`. Only the existing diagnostic probe is limited to 8192
UTF-8 bytes; HTTP transport completion policy is unchanged. An oversized 409 body
is not scanned for the already-bootstrapped marker; ordinary bounded marker
responses retain event `2004` and still require the membership check.

`ResponseCode` is a fixed recognized SDK/Dapr code. Dapr `errorCode` selects the
Dapr classification; otherwise `reasonCode` takes precedence over canonical
`code`, including when its value is unrecognized. `none` means no code was
available; `unrecognized` means a supplied code was unknown or had the wrong
shape. Raw body text, type URLs, unknown codes, details, credentials and returned
identities are excluded from these fields. A recognized value remains an
untrusted response description and does not replace command-status or membership
evidence.

For the SDK's `problem-validation-error` response only, `ValidationField` labels
recognized command/request fields such as `message-id`, `payload`,
`payload-user-id`, `extensions` or `principal-id`. It never copies raw validation
keys, messages or values. Equivalent wrapper paths share one label; disagreeing
labels use `multiple`, unknown or malformed keys use `unrecognized`, and absent
or empty dictionaries use `none`. Other response families always use `none`.
This field is also diagnostic-only and does not prove which request was delivered
or whether administrator membership exists.

### Explicit bootstrap recovery

1. Stop the handoff after token failure, HTTP failure, unexpected response, ambiguous delivery, command-status failure or projection timeout. Resource health is insufficient. Logs `2005`/`2006` indicate credential acquisition did not yield dispatch; `2001` indicates acceptance only, `2003` an unexpected response, `2002` a safe phase/category, and `2004` the already-bootstrapped response only. None prove the configured subject's membership.
2. Preserve the configured subject and authenticate it through the configured IdP. Verify exact token `sub`, authority and endpoint contract without displaying tokens or credentials. Define all shared helpers from the quickstart before its initial membership check; a failed check keeps this authenticated shell open. For persistent local Keycloak, changing Aspire parameters does not update an imported user. Repair that same identity rather than deleting state or substituting its username for the fixed subject.
3. Check qualifying projected membership with that identity. A fresh, non-degraded, version-consistent result containing the configured subject establishes projected membership under the existing contract. If delivery may have occurred, use **Tenants event `2008`**, emitted immediately before dispatch: its `MessageId` and `CorrelationId` are exactly the attempted payload's ULIDs and contain no subject/token/credential data. This event records an attempt, not delivery or success. Retain those same IDs in this shell (if manual recovery already retained IDs, keep those instead):

```bash
retain_startup_attempt() {
  [[ -z "${BOOTSTRAP_MESSAGE_ID:-}" && -z "${BOOTSTRAP_CORRELATION_ID:-}" ]] || return 1
  read -r -p "MessageId from bootstrap event 2008: " BOOTSTRAP_MESSAGE_ID
  read -r -p "CorrelationId from the same event 2008: " BOOTSTRAP_CORRELATION_ID
  [[ "$BOOTSTRAP_MESSAGE_ID" =~ ^[0-7][0-9A-HJKMNP-TV-Z]{25}$ &&
     "$BOOTSTRAP_CORRELATION_ID" =~ ^[0-7][0-9A-HJKMNP-TV-Z]{25}$ ]]
}
retain_startup_attempt
resume_bootstrap
```

`resume_bootstrap` only queries the retained correlation status and qualifying projected membership; it never dispatches. A `404`, missing IDs, `403`, stale/degraded/missing/mixed-version projection evidence or timeout does not establish non-delivery. If identity/status cannot be established, investigate without submitting a fresh command or restarting with bootstrap enabled. An already-bootstrapped rejection with the configured subject absent requires an existing administrator's investigation.

4. Only after proving that no bootstrap was delivered and the state is unbootstrapped, correct configuration and explicitly perform the following **state-changing recovery** as the same configured identity. `BOOTSTRAP_UNBOOTSTRAPPED_CONFIRMED` is an operator evidence gate: set it only after that proof, never because a membership read failed. Existing retained handles prohibit a new submission even if this flag is set. Initial bootstrap needs the configured identity; subsequent human grants additionally require qualifying administrator membership.

```bash
recover_bootstrap() {
  [[ -z "${BOOTSTRAP_MESSAGE_ID:-}" && -z "${BOOTSTRAP_CORRELATION_ID:-}" ]] || return 1
  verify_configured_identity || return 1
  [[ "${BOOTSTRAP_UNBOOTSTRAPPED_CONFIRMED:-false}" == true ]] || return 1
  if has_admin_membership "$ADMIN_TOKEN" "$ADMIN_SUBJECT"; then return 1; fi
  BOOTSTRAP_PROJECTED=false
  BOOTSTRAP_MESSAGE_ID=$(new_ulid)
  BOOTSTRAP_CORRELATION_ID=$(new_ulid)
  BOOTSTRAP_BODY=$(jq -nc --arg messageId "$BOOTSTRAP_MESSAGE_ID" --arg correlationId "$BOOTSTRAP_CORRELATION_ID" \
    --arg subject "$ADMIN_SUBJECT" \
    '{messageId:$messageId,correlationId:$correlationId,tenant:"system",domain:"global-administrators",aggregateId:"global-administrators",commandType:"BootstrapGlobalAdmin",payload:{UserId:$subject}}')
  BOOTSTRAP_HTTP=$(platform_api "$ADMIN_TOKEN" -sS -o /dev/null -w '%{http_code}' \
    -H 'Content-Type: application/json' --data-binary @/dev/fd/3 \
    "$EVENTSTORE_URL/api/v1/commands" 3<<< "$BOOTSTRAP_BODY") || return 1
  [[ "$BOOTSTRAP_HTTP" == 202 ]] || return 1
  resume_bootstrap
}
# Set true only after the confirmed non-delivery/unbootstrapped checks above.
BOOTSTRAP_UNBOOTSTRAPPED_CONFIRMED=false
recover_bootstrap
```

A nonzero result preserves IDs and stops unresolved. After repair, use the same handles:

```bash
resume_bootstrap
```

`PublishFailed` stops unresolved because events were persisted but publication failed; those events forbid blind replacement. Apply the existing EventStore publication-recovery policy and then query this same status/projection again. `Rejected`, `TimedOut`, unknown status and polling expiry also retain the handles. These examples do not redefine publication recovery, automatically clear handles, or authorize a fresh restart after ambiguous delivery. Only qualifying status and membership evidence completes recovery before the administrator grants human access. Record safe status/reason, pass/fail and timestamp only.

## Global Administrator Revocation Boundary

`GlobalAdministratorRemoved` governs each new global-only command and every query gate through `ITenantsGlobalAdministratorAuthority`. The request-scoped service reads only `system/global-administrators/global-administrators` through the authenticated native stream reader, folds known events, and compares native incarnation/prefix proof with a final metadata observation. Projection age and projection membership do not grant authority. Verified nonmembership removes the global grant; unavailable, malformed, changed or over-bound evidence fails closed with an authority-unavailable failure. Independent tenant permissions retain their existing behavior. An operation admitted at its final verified source observation may finish after a later removal.

Global-administrator management commands retain their existing membership check against the global aggregate's locked, replayed state. Their SDK wire administrator hint is always stripped, granting no additional permission. A native read of that same actor while it is awaiting `/process` would deadlock because actor reentrancy is disabled; refusing the unused hint avoids that self-call. Global-domain queries still require the query handler's independent native-source check, and tenant-domain commands use the shared authority service.

At the SDK query boundary, Tenants discards the unused administrator hint only for the matched canonical `/query` POST endpoint with its server-owned query authorization policy. Request paths or caller claims alone cannot select this behavior. All six handlers still establish their own native grants, and independent detail/users membership can succeed when the global source is unavailable. Tenant commands still verify native membership. The delegated native-read authority endpoint preserves its canonical wire-hint refusal by checking the SDK's original verified query as well as its rebuilt query.

A successfully verified administrator set may be reused only within the current request. There is no cross-request authority cache. The shared reader bounds a replay to 10,000 events, 16 MiB and a ten-second authenticated read; the final native metadata adapter has its own existing two-second bound. The recorded 10,000-event fold control measures CPU replay only, not end-to-end transport latency. Long-lived streams exceeding a bound need an approved platform solution; they never fall back to the projection.

Use bounded backpressure for native verification bursts: the Tenants AppHost defaults `EventStore:RateLimiting:QueueLimit` to `64`, while an explicit operator value (including `0`) wins. Configure the same queue on the deployment's EventStore host. The existing tenant/actor and consumer permit ceilings and windows remain unchanged, as do source freshness, request-only reuse, and retry policy. Queueing still observes request cancellation and the existing read deadlines; sustained load can exhaust those bounds and must fail closed. A real 20-row control returned all current count pairs within its deadlines with this setting; it is not a general capacity SLA.

Enroll the `tenants` workload for exactly the `eventstore` audience and `eventstore:stream-read` operation. Supply its private PKCS#8 RSA scope key through `Authentication:BoundStreamRead:PrivateKey`; give EventStore the corresponding SPKI public key at `Authentication:BoundStreamRead:PublicKeys:tenants`, the existing owner-domain list `tenants,global-administrators` at `Authentication:BoundStreamRead:AllowedDomains:tenants`, and the workload in `Authentication:DaprInternal:AllowedCallers`. The Tenants transport restricts the new domain to the fixed aggregate above; another global aggregate or tenant cannot be signed. Keep secrets outside checked-in configuration. The local AppHost supplies per-run keys and enrolled credentials; deployment values are shown in [the source-authority template](../deploy/source-authority-settings.example.json).

For an existing persistent Keycloak realm, startup realm import does not update that realm. Keycloak skips an already existing realm to preserve its state; see [Keycloak realm import and export](https://www.keycloak.org/server/importExport#importing-a-realm-during-startup). Changing the checked-in realm JSON or regenerating Aspire parameters therefore does not enroll a missing `tenants` client or synchronize an existing client's secret.

Use the realm's authenticated Admin Console or Admin REST API for a targeted enrollment: create or update only the confidential `tenants` service-account client, the `eventstore-audience.eventstore` scope with its `eventstore` audience mapper, and the `eventstore-operation.eventstore.stream-read` scope with its `eventstore:stream-read` operation mapper. Assign those two optional client scopes to `tenants` as in the realm template; grant no audience or operation by default. Preserve existing realm settings, users, roles and unrelated clients. Obtain or rotate that client's secret through the identity administrator's normal secret-management process, then configure the identical value in Tenants' `Authentication:WorkloadIssuer:ClientSecret` (the local AppHost's existing `tenants-workload-client-secret` parameter supplies it). A newly generated local parameter does not rotate the persisted Keycloak secret. Coordinate both sides before restarting the workload; never delete the realm or users to force another startup import.

Verify enrollment with a client-credentials token acquisition requesting `eventstore-audience.eventstore eventstore-operation.eventstore.stream-read` using the synchronized secret, without printing or saving the secret or token. Check the resulting token's signature/issuer, `eventstore` audience and `eventstore:operation=eventstore:stream-read` claim locally, then perform a native source read and final metadata verification through the enrolled workload. Record only acquisition/read pass or fail, HTTP status and safe reason code. Confirm an unassigned scope/client fails closed. Publishing supplies no ephemeral source key or public-key binding: deploy the externally managed matching private/public pair and the explicit enrollment settings above. These are operator instructions; implementation tests do not change an identity deployment.

Existing legacy global streams whose native inspection returns Unsupported or has no native proof cannot satisfy this policy. They remain unavailable even if their projection grants membership. Such deployments need a separately approved platform recovery plan; this change never backfills proof, resets state, migrates events or falls back to projection authority. A pre-existing local stream with 172 events and no proof was observed during qualification and retained.

Identity-provider revocation remains a separate channel: an issued token or retained Blazor circuit may contain older claims until renewal. Those claims cannot override the native source check. Operator verification must cover the committed native removal and renewed identity/session evidence, without treating command acceptance, a projected audit row or a SignalR notification as revocation proof. The evidence artifact records the actual acceptance status of DW-562 and DW-617.

## Deployment Readiness Checklist

Run these checks before release. Store only the pass/fail result, test name, HTTP status, and safe reason code.

| Check | Pass condition | Fail condition | Evidence | Redaction rule |
| --- | --- | --- | --- | --- |
| Production startup placeholders | Deployment overrides provide `Authority`, `Issuer`, `Audience`, an explicit supported asymmetric `AllowedAlgorithms` entry, and `RequireHttpsMetadata=true`. | Startup/options validation names a missing or invalid `Authentication:JwtBearer` key. | `AuthenticationConfigurationTests` or deployment startup log shows the named key. | Do not include signing-key values, bearer tokens, or full authority hosts. |
| HTTPS authority | `Authority` is absolute HTTPS. | Empty, whitespace, relative, malformed, or HTTP authority fails before OIDC discovery. | Startup validation names `Authentication:JwtBearer:Authority`. | Record only the category, not the exact internal URL. |
| JWT signing algorithm | `AllowedAlgorithms:0` names a supported asymmetric algorithm used by the IdP, such as `RS256`. | Missing, blank, symmetric, or unknown algorithms fail startup validation. | Startup validation names `Authentication:JwtBearer:AllowedAlgorithms`. | Record the algorithm name only, never keys or tokens. |
| Signing source | Production uses OIDC `Authority`; `SigningKey` is empty or unset. | Any production signing key, or `Authority` plus `SigningKey`, fails as ambiguous. | Startup validation names `Authentication:JwtBearer:SigningKey`. | Never print the signing key. |
| HTTPS metadata | `RequireHttpsMetadata=true`. | `false` fails in `Production`. | Startup validation names `Authentication:JwtBearer:RequireHttpsMetadata`. | No secret data. |
| Token issuer | Token `iss` equals configured `Issuer`. | Wrong issuer returns `401 Unauthorized`. | Smoke test or manual request returns 401 at authentication. | Do not commit decoded production tokens. |
| Token audience | Token `aud` equals configured `Audience`. | Wrong audience returns `401 Unauthorized`. | Smoke test or manual request returns 401 at authentication. | Do not commit decoded production tokens. |
| Token subject | Token has non-empty `sub`. | Missing subject fails authentication or authorization before dispatch. | ProblemDetails reason code, when present, is safe. | Redact subject IDs. |
| Tenant claim | Effective principal contains `eventstore:tenant=system`, including global-administrator operators, and protected requests use the `system` request tenant. | Missing, blank, wrong-cased, wrong tenant claim, or non-`system` request tenant returns `403 Forbidden` before dispatch. | ProblemDetails `reasonCode` is `principal_not_member` or `tenant_mismatch`. | Redact real tenant and user identifiers. |
| Query endpoint | A valid token can call one protected Tenants query endpoint, such as `GET /api/tenants`. | Invalid token returns 401; wrong tenant returns 403; query router is not invoked for denied calls. | `TenantsQueryControllerIntegrationTests` or manual HTTP status. | Do not store bearer tokens or full response data from production tenants. |
| Command endpoint | A valid token can reach `POST /api/v1/commands` when EventStore command infrastructure is available. | Missing/wrong auth returns 401 or 403 before command routing. | `CommandApiRuntimeIntegrationTests` or operator-run deployment test. | Use placeholder command IDs and non-production tenant data in transcripts. |
| Global-administrator revocation | A committed removal is included in the verified native prefix, and a new global-only operation refuses the removed caller even while projections or claims still grant. Renewed token/session evidence checks the separate IdP channel. | A stale projection or claim restores a global grant, a global-only operation remains allowed after verified removal, or unavailable source evidence grants authority. | Deployment/operator verification of native availability/proof match and safe refusal status/reason; projection catch-up and session renewal are separate checks. An operation independently permitted to a tenant owner does not prove global authority was revoked. This is a required deployment check, not a test run claimed here. | Record only pass/fail, safe status/reason and redacted identities; never tokens, proof values or model contents. |
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

Expected evidence: production startup/options validation fails safely for missing placeholders, invalid or missing `AllowedAlgorithms`, whitespace values, non-HTTPS authority, `RequireHttpsMetadata=false`, and production `SigningKey`, and succeeds for valid OIDC-style overrides.

Redaction rule: validation assertions must name configuration keys only. They must not echo signing keys, bearer tokens, decoded payloads, or secret values.

## Manual Deployment Smoke Checks

Use these only against an environment approved for release verification.

```bash
curl -i -H "Authorization: Bearer <redacted-access-token>" https://<tenants-host>/api/tenants
```

Expected evidence: `200 OK` for an authorized operator token, `401 Unauthorized` for invalid authentication, or `403 Forbidden` for a valid token without `eventstore:tenant=system`. This proves the protected query path.

Redaction rule: do not store the bearer token, full host name, returned tenant data, or full response body in committed artifacts. Store only status code, safe reason code, and test timestamp.

For bootstrap and administrator handoff, use the authenticated membership reads and status checks in [Explicit bootstrap recovery](#explicit-bootstrap-recovery) and the [Quickstart](quickstart.md#get-an-access-token). Do not submit `BootstrapGlobalAdmin` as a smoke probe. A command path check must use an approved operation and the **EventStore** gateway URL, retain its IDs, and verify status plus the resulting projection; acceptance alone is not business success.

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

| Story 7.6A acceptance criterion | Evidence source |
| --- | --- |
| AC1 valid and invalid tokens on protected tenant command/query endpoints | [Deterministic Local Smoke Tests](#deterministic-local-smoke-tests), `TenantsQueryControllerIntegrationTests`, `CommandApiRuntimeIntegrationTests`, and [Failure Triage](#failure-triage) |
| AC2 support-safe production auth evidence for issuer, audience, `eventstore:tenant`, HTTPS metadata, signing/authority source, and development-token separation | [Required Production Settings](#required-production-settings), [Required Token Contents](#required-token-contents), [IdP Claim Contract](#idp-claim-contract), [Deployment Readiness Checklist](#deployment-readiness-checklist), [Manual Deployment Smoke Checks](#manual-deployment-smoke-checks), and [Quickstart](quickstart.md) |

## Recorded Deployment Boundaries

- The deterministic smoke tests use real ASP.NET Core JWT bearer middleware but a local production-like signing seam to avoid a live IdP dependency.
- The command endpoint can be tested through a mocked command router locally. Full command processing still requires EventStore/DAPR infrastructure and belongs to deployment smoke verification.
- The Tenants host does not register the EventStore rate limiter today. Executable rate-limit partition evidence belongs at the EventStore host boundary or later deployment automation.
- Vendor-specific Keycloak, Entra ID, Helm, Aspire publish, and cloud deployment manifests remain deferred deployment automation. This story records the contract they must satisfy.
