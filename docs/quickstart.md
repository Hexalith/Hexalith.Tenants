[Back to README](../README.md)

# Quickstart

Clone the repository, run the application with .NET Aspire, send your first tenant management command through the EventStore command gateway, and inspect the command outcome. This guide follows the same developer experience pattern as the [EventStore quickstart](../Hexalith.EventStore/docs/getting-started/quickstart.md).

> **Time estimate:** within 30 minutes from clone to first command when the prerequisites below are already installed. Installing .NET, Docker, or DAPR for the first time is outside that clock and depends on your workstation.

## Prerequisites

Before you begin, verify that the following tools are installed and working. Run each check command and confirm the expected output.

### .NET 10 SDK

```bash
dotnet --version
```

Expected: `10.0.300` or a later `10.0.xxx` patch version. The repository pins SDK `10.0.300` in [`global.json`](../global.json) with `rollForward: latestPatch`.

If not installed, download from [https://dot.net](https://dot.net/download).

### DAPR CLI and Runtime

```bash
dapr --version
```

Expected: CLI version and runtime version both present.

```bash
dapr init
```

> **Note:** Run `dapr init` (full init, not `--slim`) for this local quickstart. Full init provides Redis, actor placement, and scheduler. `dapr init --slim` excludes those local services; use slim mode only when you provide placement, scheduler, `statestore`, and `pubsub` separately. Deployment-specific DAPR details live in [`deploy/dapr`](../deploy/dapr/README.md).

The existing local tests expect Redis on `localhost:6379`, placement on `50005` on Linux or `6050` on Windows, and scheduler on `50006` on Linux or `6060` on Windows.

If not installed, follow the [DAPR Getting Started guide](https://docs.dapr.io/getting-started/).

### Docker

```bash
docker info
```

Expected: Docker daemon information (Engine version, etc.).

Docker Desktop must be running. The Aspire AppHost launches containers for Redis, Keycloak, and DAPR sidecars.

> **Tip:** Allocate at least 4 GB of memory to Docker Desktop. The full topology (EventStore command gateway + Tenants + Sample + DAPR sidecars + Redis + Keycloak) can exceed lower memory limits.

If not installed, download Docker Desktop from [https://docs.docker.com/get-started/get-docker/](https://docs.docker.com/get-started/get-docker/).

### Root-Level Submodules

Only initialize the root-level submodules used by this repository:

```bash
git submodule update --init Hexalith.EventStore Hexalith.Commons Hexalith.AI.Tools Hexalith.Builds Hexalith.FrontComposer
git submodule status Hexalith.EventStore Hexalith.Commons Hexalith.AI.Tools Hexalith.Builds Hexalith.FrontComposer
```

Expected: each line starts with a commit hash or a leading space. A leading `-` means the submodule is not initialized; rerun the command above.

> **Do not use recursive initialization.** Do not run `git submodule update --init --recursive`; nested submodules are intentionally left alone for this repository.

### About the `system` Tenant

Hexalith.Tenants operates as a platform-level service within EventStore's multi-tenant model. All tenant management commands run under the `system` tenant context — this is a platform tenant that manages other tenants, not a user-facing tenant.

For local development, the Aspire AppHost topology handles the `system` tenant configuration automatically. By default it starts a local Keycloak realm that emits `eventstore:tenant=system` for the sample administrator. You do not need to manually deploy EventStore or configure JWT tenant claims.

## Clone and Build

Clone the repository, then initialize the root-level submodules:

```bash
git clone https://github.com/Hexalith/Hexalith.Tenants.git
cd Hexalith.Tenants
git submodule update --init Hexalith.EventStore Hexalith.Commons Hexalith.AI.Tools Hexalith.Builds Hexalith.FrontComposer
```

Do not add `--recursive`.

> **Windows users:** The repository contains submodule paths such as `Hexalith.Tenants/Hexalith.EventStore/src/...`. If the build fails with path-too-long errors, run `git config --system core.longpaths true` and re-clone.

Verify the build:

```bash
dotnet build Hexalith.Tenants.slnx --configuration Release
```

## Run the Application

Start the Aspire AppHost, which launches the EventStore command gateway, the Tenants domain service, the Tenants UI host, Keycloak, Redis, DAPR sidecars, and the sample consuming service:

```bash
dotnet run --project src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj
```

> **Note:** The first run takes longer than usual because .NET restores NuGet packages and Docker pulls container images for Redis, Keycloak, and the DAPR sidecars.

Once the application starts, the terminal output includes the Aspire dashboard URL. Open it in your browser — the dashboard shows all running services and their endpoints.

Before sending a command, confirm the dashboard shows these local resources as running or healthy:

- `eventstore`: EventStore command gateway, including `POST /api/v1/commands`
- `tenants`: Tenants domain processor for `/process` and query endpoints
- `tenants-ui`: Blazor InteractiveServer Tenants workspace composed through FrontComposer
- `keycloak`: local identity provider, unless you explicitly set `EnableKeycloak=false`
- `redis`: local state store backing DAPR actor and projection state
- `sample`: consuming service subscribed to tenant events
- DAPR sidecars for `eventstore`, `tenants`, and `sample`

If `eventstore` or `tenants` is missing or unhealthy, do not submit the first command yet. Check the AppHost resource details first; common causes are Docker not running, DAPR not initialized, port conflicts from old sidecars, or uninitialized submodules.

## Get an Access Token

The EventStore command gateway requires a JWT token for authentication. The default local AppHost starts Keycloak with a sample realm and user.

Find the `keycloak` base URL in the Aspire dashboard, then request a token with the local sample credentials:

```bash
curl -s -X POST "{keycloak-url}/realms/hexalith/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password" \
  -d "client_id=hexalith-eventstore" \
  -d "username=admin-user" \
  -d "password=admin-pass" \
  | jq -r .access_token
```

If `jq` is not installed, copy the `access_token` value from the JSON response. The token includes the direct `eventstore:tenant=system`, `eventstore:domain=global-administrators`, `eventstore:domain=tenants`, and `eventstore:permission=command:submit` claims required for the two quickstart commands.

If you intentionally run the AppHost with `EnableKeycloak=false`, generate a development HMAC token instead. The quickstart submits commands to the EventStore command gateway, so that fallback uses the development issuer, audience, and signing key from `Hexalith.EventStore/src/Hexalith.EventStore/appsettings.Development.json`.

The compact payload produced by the examples includes `"aud":"hexalith-eventstore"`.

**PowerShell:**

```powershell
$header = @{alg="HS256";typ="JWT"} | ConvertTo-Json -Compress
$exp = [int](Get-Date -Date (Get-Date).AddHours(8).ToUniversalTime() -UFormat %s)
$payload = @{sub="admin-user";iss="hexalith-dev";aud="hexalith-eventstore";tenants=@("system");exp=$exp} | ConvertTo-Json -Compress

function ConvertTo-Base64Url($bytes) { [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+','-').Replace('/','_') }

$headerB64 = ConvertTo-Base64Url([System.Text.Encoding]::UTF8.GetBytes($header))
$payloadB64 = ConvertTo-Base64Url([System.Text.Encoding]::UTF8.GetBytes($payload))
$signingInput = "$headerB64.$payloadB64"

$key = [System.Text.Encoding]::UTF8.GetBytes("DevOnlySigningKey-AtLeast32Chars!")
$hmac = New-Object System.Security.Cryptography.HMACSHA256(,$key)
$sig = ConvertTo-Base64Url($hmac.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($signingInput)))

$token = "$signingInput.$sig"
Write-Output $token
```

**bash (requires openssl):**

```bash
header=$(echo -n '{"alg":"HS256","typ":"JWT"}' | openssl base64 -A | tr '+/' '-_' | tr -d '=')
exp=$(($(date +%s) + 28800))
payload=$(echo -n "{\"sub\":\"admin-user\",\"iss\":\"hexalith-dev\",\"aud\":\"hexalith-eventstore\",\"tenants\":[\"system\"],\"exp\":$exp}" | openssl base64 -A | tr '+/' '-_' | tr -d '=')
sig=$(echo -n "$header.$payload" | openssl dgst -sha256 -hmac "DevOnlySigningKey-AtLeast32Chars!" -binary | openssl base64 -A | tr '+/' '-_' | tr -d '=')
echo "$header.$payload.$sig"
```

Copy the output token — you need it in the next step.

> **How it works:** The default local Keycloak realm is imported from `src/Hexalith.Tenants.AppHost/KeycloakRealms/hexalith-realm.json` and is configured for the `hexalith-eventstore` audience used by the AppHost. The `EnableKeycloak=false` fallback uses EventStore's local HMAC-SHA256 settings with issuer `hexalith-dev` and audience `hexalith-eventstore`; the `tenants: ["system"]` source claim is normalized by EventStore into `eventstore:tenant=system`. For production IdP mappings, see [Production Auth Claim Contract](production-auth-claim-contract.md).
>
> **Production note:** Production deployments use OIDC authority-based JWT validation, not the local HMAC signing key. Before release, run the [Production Auth Readiness](production-auth-readiness.md) checklist and smoke tests.

## Validate Before the First Command

Do these checks before you submit `BootstrapGlobalAdmin`. They catch the common local setup failures while the system is still idle.

### Verify the EventStore command gateway

Find the `eventstore` service URL in the Aspire dashboard, then confirm the OpenAPI document exposes the command gateway route:

```bash
curl -fsS "{eventstore-url}/swagger/v1/swagger.json" | rg '"/api/v1/commands"'
```

Expected: a match for `/api/v1/commands`. If the request fails, the EventStore resource is not reachable yet; check AppHost health, Docker, DAPR initialization, and port conflicts before continuing. Do not switch to a Tenants-specific command URL; tenant commands are submitted through the EventStore-owned `POST /api/v1/commands` route.

### Verify the token and `system` tenant claim

Use the token from the previous step to call the status endpoint with a correlation ID that has not been used:

```bash
TOKEN="{paste-token-here}"
curl -i -H "Authorization: Bearer $TOKEN" \
  "{eventstore-url}/api/v1/commands/status/01JQK000000000000000009999"
```

Expected: `404 Not Found` with a problem-details body saying no command status exists for that correlation ID. That proves the gateway accepted the token and searched the authorized tenant scope.

Fix these outcomes before submitting a tenant command:

| Result | Meaning | Fix |
| ------ | ------- | --- |
| `401 Unauthorized` | Missing, expired, malformed, wrong issuer, wrong audience, or wrong signing key token | Re-acquire the Keycloak token or regenerate the local HMAC token after confirming the AppHost auth mode. |
| `403 Forbidden` with no tenant authorization claims | Token lacks an effective `eventstore:tenant=system` claim | Use the `admin-user` local Keycloak account or a local HMAC token with `tenants: ["system"]`. |
| `403 Forbidden` with tenant mismatch | Token tenant is not exactly `system` for the platform command path | Use the local platform administrator token; do not submit tenant-management commands under `tenant-a`, `tenant-b`, or differently cased `System`. |
| Connection failure | EventStore is not reachable | Wait for the AppHost resource, then check Docker, DAPR full init, and AppHost resource details. |

## Send Your First Commands

### Open Swagger UI

Find the `eventstore` service in the Aspire dashboard and open its URL. Append `/swagger` to the URL to open the Swagger UI.

1. Click the **Authorize** button at the top of the page
2. In the **Value** field, paste the token you generated — do not include the `Bearer` prefix, Swagger adds it automatically
3. Click **Authorize**, then **Close**

### Step 1: Bootstrap the Global Administrator

Before creating tenants, you must authorize an administrator. In the EventStore Swagger UI, expand the **POST /api/v1/commands** endpoint, click **Try it out**, and submit:

```json
{
    "messageId": "01JQK7YQ7YQ7YQ7YQ7YQ7YQ7Y1",
    "tenant": "system",
    "domain": "global-administrators",
    "aggregateId": "global-administrators",
    "commandType": "BootstrapGlobalAdmin",
    "payload": {
        "UserId": "admin-user"
    }
}
```

> **`messageId`** is required — it is the idempotency key. Generate a unique value per command (e.g., a ULID). Resubmitting the same `messageId` is safely deduplicated.

Click **Execute**. The API returns `202 Accepted` with a correlation ID. This registers `admin-user` as a global administrator who can create and manage tenants.

### Step 2: Create Your First Tenant

Now create a tenant. In the same **POST /api/v1/commands** endpoint, submit:

```json
{
    "messageId": "01JQK7YQ7YQ7YQ7YQ7YQ7YQ7Y2",
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

> **Important:** `aggregateId` and `payload.TenantId` must match — the aggregate ID is the managed tenant ID per the identity scheme (`system:tenants:{aggregateId}`). If they don't match, the command will be rejected with a validation error.

Click **Execute**. The API returns `202 Accepted`. The response body contains a correlation ID:

```json
{ "correlationId": "01JQK7YQ7YQ7YQ7YQ7YQ7YQ7Y2" }
```

The `Location` header points to the status polling endpoint (`/api/v1/commands/status/{correlationId}`). You can poll this endpoint until you see a terminal status:

```json
{
    "correlationId": "01JQK7YQ7YQ7YQ7YQ7YQ7YQ7Y2",
    "status": "Completed",
    "statusCode": 4,
    "timestamp": "2026-03-19T12:00:01Z",
    "aggregateId": "my-first-tenant",
    "eventCount": 1,
    "rejectionEventType": null,
    "failureReason": null
}
```

`status: "Completed"` with `eventCount: 1` confirms the `TenantCreated` event was stored and published. A `status: "Rejected"` response means a business rule rejected the command; check `rejectionEventType` for the reason and use the corrective actions below. Infrastructure failures use `failureReason` instead.

### Verify the Event

Verify the tenant was created by querying the read model. Expand the **GET /api/tenants/{tenantId}** endpoint, enter `my-first-tenant` as the tenant ID, and execute.

The response should contain the tenant details including the name and description you provided.

> **Note:** If the query returns 404, retry after 3–5 seconds. Projections are eventually consistent — the read model processes events asynchronously. If 404 persists beyond 30 seconds, check the Aspire dashboard for service errors and verify the command reached `status: "Completed"` via the status endpoint.

You can also check the command status via the URL in the `Location` header from the previous response, or query it directly: `GET /api/v1/commands/status/{correlationId}`.

### Running the Quickstart Again

If you've run this before:

- **BootstrapGlobalAdmin** may reach `status: "Rejected"` with `rejectionEventType` ending in `GlobalAdminAlreadyBootstrappedRejection`. This is correct behavior: the admin was already created. Continue to `CreateTenant`.
- **CreateTenant** with the same ID may reach `status: "Rejected"` with `rejectionEventType` ending in `TenantAlreadyExistsRejection`. Use a different `aggregateId` and matching `payload.TenantId`, e.g., `my-second-tenant`.

### Try More Commands

Create a multi-step workflow — add a user to your new tenant:

**1. Add a user to the tenant:**

```json
{
    "messageId": "01JQK7YQ7YQ7YQ7YQ7YQ7YQ7Y3",
    "tenant": "system",
    "domain": "tenants",
    "aggregateId": "my-first-tenant",
    "commandType": "AddUserToTenant",
    "payload": {
        "TenantId": "my-first-tenant",
        "UserId": "jane-doe",
        "Role": "TenantContributor"
    }
}
```

> **Roles:** Use the enum names `TenantOwner`, `TenantContributor`, or `TenantReader`. `Unknown` is the fail-closed sentinel and is rejected by the aggregate.

**2. Verify the user was added:**

Expand `GET /api/tenants/{tenantId}/users`, enter `my-first-tenant`, and execute. The response lists users and their roles in the tenant.

## Next Steps

> **Note:** Everything below is optional follow-up — you've already completed the core quickstart by creating your first tenant.

### Consume Tenant Events in Your Service

Install the NuGet packages for event-driven integration:

```bash
dotnet add package Hexalith.Tenants.Contracts
dotnet add package Hexalith.Tenants.Client
```

The sample consuming service shows the complete pattern: package references,
DI registration, typed tenant event handlers, DAPR subscription mapping, local
projection updates, access checks, configuration reads, and production adaptation
boundaries.

Use the [Sample Consuming Service Walkthrough](sample-consuming-service-walkthrough.md)
as the source-backed guide for copying the pattern from
[`samples/Hexalith.Tenants.Sample/`](../samples/Hexalith.Tenants.Sample/).

For event envelope fields, delivery semantics, and ordering limits, see [Event Contract Reference](event-contract-reference.md). For event handling patterns and idempotent processing, see [Idempotent Event Processing](idempotent-event-processing.md).

### Test Tenant Isolation Without Infrastructure

Install the Testing package in your consuming service test project when you need fast tenant setup and event replay without DAPR, Docker, Aspire, HTTP, or a live EventStore:

```bash
dotnet add package Hexalith.Tenants.Testing
```

Use `TenantIsolationTestHelpers` to create independent tenant contexts, replay only the events for the tenant under test, simulate duplicate delivery, and assert local grant/revoke behavior in your own projection:

```csharp
InMemoryTenantService tenants = TenantIsolationTestHelpers.CreateServiceWithTenants(
    new Dictionary<string, IReadOnlyDictionary<string, TenantRole>> {
        ["tenant-a"] = new Dictionary<string, TenantRole> {
            ["shared-user"] = TenantRole.TenantOwner,
            ["reader"] = TenantRole.TenantReader,
        },
        ["tenant-b"] = new Dictionary<string, TenantRole> {
            ["shared-user"] = TenantRole.TenantReader,
        },
    });

IReadOnlyList<IEventPayload> tenantAEvents = TenantIsolationTestHelpers.GetTenantEvents(tenants, "tenant-a");
IReadOnlyList<IEventPayload> duplicateTenantAEvents = TenantIsolationTestHelpers.DuplicateDelivery(tenantAEvents);

consumerProjection.ApplyEvents(duplicateTenantAEvents);
consumerProjection.IsAuthorized("tenant-a", "shared-user", TenantRole.TenantOwner).ShouldBeTrue();
consumerProjection.IsAuthorized("tenant-b", "shared-user", TenantRole.TenantOwner).ShouldBeFalse();

_ = TenantIsolationTestHelpers.RemoveUser(tenants, "tenant-a", "reader");
consumerProjection.ApplyEvents(TenantIsolationTestHelpers.GetTenantEvents(tenants, "tenant-a"));
consumerProjection.IsAuthorized("tenant-a", "reader", TenantRole.TenantReader).ShouldBeFalse();
```

`Hexalith.Tenants.Testing` provides aggregate-level fake parity: command validation, successful event production, and state transitions execute through the same aggregate logic used by the service. Consuming services are still responsible for testing their own projection-level and query-level isolation, including deduplication behavior and tenant-scoped reads. Keep idempotency assertions aligned with the guidance in [Idempotent Event Processing](idempotent-event-processing.md) rather than duplicating those patterns in each test.

## Troubleshooting

### AppHost Startup Failures

**Port conflict — DAPR sidecar port 3500 already in use**

Stop other DAPR instances and retry:

```bash
dapr stop --all
```

Or change the port in the AppHost configuration.

**Docker resource limits**

The topology (EventStore command gateway + Tenants + Sample + DAPR sidecars + Redis + Keycloak) can exceed default Docker Desktop memory allocation. Increase Docker memory to 4 GB or more in Docker Desktop Settings > Resources.

**DAPR not initialized**

If you see DAPR-related errors, ensure you've run the full initialization:

```bash
dapr init
```

Use `dapr init` (not `--slim`) — the Aspire topology requires Redis, placement, and scheduler. Slim self-hosted mode (`dapr init --slim`) is for operators who provide placement, scheduler, and the `statestore`/`pubsub` components themselves before actor flows start.

Expected local ports used by existing tests:

| Dependency | Linux | Windows |
| ---------- | ----- | ------- |
| Redis | `localhost:6379` | `localhost:6379` |
| Placement | `50005` | `6050` |
| Scheduler | `50006` | `6060` |

### DAPR Configuration Triage

| Symptom | Likely issue | Action |
| ------- | ------------ | ------ |
| Actor startup fails before command processing | missing placement | Confirm full `dapr init` ran or provide placement for slim mode |
| Actor reminder/scheduler errors appear | missing scheduler | Confirm scheduler is reachable on the expected port |
| State calls report a missing component | missing state store or wrong component name | Confirm the component is named `statestore` and scoped to the calling AppId |
| Event publishing/subscription fails | missing pub/sub or wrong component name | Confirm the component is named `pubsub` and scoped to `eventstore` and subscriber AppIds |
| Tenants `/process` or `/project` invocation fails | wrong AppId or denied service invocation | Confirm EventStore uses AppId `eventstore`, Tenants uses AppId `tenants`, and the receiver access-control template allows the route |
| Component exists but a sidecar cannot use it | wrong component scope | Add only the required AppId to the component `scopes` list |
| Sidecar logs show access-control denial | denied service invocation | Inspect the called sidecar's DAPR `Configuration`, not the caller's |

Production DAPR templates and additional triage guidance live in [`deploy/dapr`](../deploy/dapr/README.md).

**Build fails on Windows with path-too-long**

Enable long paths and re-clone:

```bash
git config --system core.longpaths true
```

### Common Errors

| Error                            | Meaning                      | Action                                       |
| -------------------------------- | ---------------------------- | -------------------------------------------- |
| `GlobalAdminAlreadyBootstrappedRejection` | Bootstrap already ran        | Safe to proceed — the admin exists           |
| `TenantAlreadyExistsRejection`            | Tenant ID already used       | Use a different `aggregateId` and `TenantId` |
| `401 Unauthorized`               | JWT token expired or invalid | Re-generate the token using the script above |
| `403 Forbidden`                  | Token lacks the effective `eventstore:tenant=system` authorization | Confirm the token has `tenants: ["system"]` locally or a production mapping that normalizes to `eventstore:tenant=system` |
