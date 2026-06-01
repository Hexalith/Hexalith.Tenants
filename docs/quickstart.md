[Back to README](../README.md)

# Quickstart

Clone the repository, run the application with .NET Aspire, send your first tenant management command, and see the resulting event. This guide follows the same developer experience pattern as the [EventStore quickstart](../Hexalith.EventStore/docs/getting-started/quickstart.md).

> **Time estimate:** ~15 minutes with prerequisites installed. The 30-minute target assumes prerequisites are already set up — the clock starts at "clone".

## Prerequisites

Before you begin, verify that the following tools are installed and working. Run each check command and confirm the expected output.

### .NET 10 SDK

```bash
dotnet --version
```

Expected: `10.x.xxx` (any 10.x version)

If not installed, download from [https://dot.net](https://dot.net/download).

### DAPR CLI and Runtime

```bash
dapr --version
```

Expected: CLI version and runtime version both present.

```bash
dapr init
```

> **Note:** Run `dapr init` (full init, not `--slim`) — the Aspire topology requires the full DAPR runtime with placement service and Redis.

If not installed, follow the [DAPR Getting Started guide](https://docs.dapr.io/getting-started/).

### Docker

```bash
docker info
```

Expected: Docker daemon information (Engine version, etc.).

Docker Desktop must be running. The Aspire AppHost launches containers for Redis, Keycloak, and DAPR sidecars.

> **Tip:** Allocate at least 4 GB of memory to Docker Desktop. The full topology (EventStore command gateway + Tenants + Sample + DAPR sidecars + Redis + Keycloak) can exceed lower memory limits.

If not installed, download Docker Desktop from [https://docs.docker.com/get-started/get-docker/](https://docs.docker.com/get-started/get-docker/).

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

> **Windows users:** The repository contains submodule paths such as `Hexalith.Tenants/Hexalith.EventStore/src/...`. If the build fails with path-too-long errors, run `git config --system core.longpaths true` and re-clone.

Verify the build:

```bash
dotnet build Hexalith.Tenants.slnx --configuration Release
```

## Run the Application

Start the Aspire AppHost, which launches the EventStore command gateway, the Tenants domain service, Keycloak, Redis, DAPR sidecars, and the sample consuming service:

```bash
dotnet run --project src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj
```

> **Note:** The first run takes longer than usual because .NET restores NuGet packages and Docker pulls container images for Redis, Keycloak, and the DAPR sidecars.

Once the application starts, the terminal output includes the Aspire dashboard URL. Open it in your browser — the dashboard shows all running services and their endpoints.

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

If `jq` is not installed, copy the `access_token` value from the JSON response. The token includes the direct `eventstore:tenant=system`, `eventstore:domain=tenants`, and `eventstore:permission=command:submit` claims required for tenant-management commands.

If you intentionally run the AppHost with `EnableKeycloak=false`, generate a development HMAC token instead. That fallback uses the development signing key in `src/Hexalith.Tenants/appsettings.Development.json`.

**PowerShell:**

```powershell
$header = @{alg="HS256";typ="JWT"} | ConvertTo-Json -Compress
$exp = [int](Get-Date -Date (Get-Date).AddHours(8).ToUniversalTime() -UFormat %s)
$payload = @{sub="admin-user";iss="hexalith-dev";aud="hexalith-tenants";tenants=@("system");exp=$exp} | ConvertTo-Json -Compress

function ConvertTo-Base64Url($bytes) { [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+','-').Replace('/','_') }

$headerB64 = ConvertTo-Base64Url([System.Text.Encoding]::UTF8.GetBytes($header))
$payloadB64 = ConvertTo-Base64Url([System.Text.Encoding]::UTF8.GetBytes($payload))
$signingInput = "$headerB64.$payloadB64"

$key = [System.Text.Encoding]::UTF8.GetBytes("this-is-a-development-signing-key-minimum-32-chars")
$hmac = New-Object System.Security.Cryptography.HMACSHA256(,$key)
$sig = ConvertTo-Base64Url($hmac.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($signingInput)))

$token = "$signingInput.$sig"
Write-Output $token
```

**bash (requires openssl):**

```bash
header=$(echo -n '{"alg":"HS256","typ":"JWT"}' | openssl base64 -A | tr '+/' '-_' | tr -d '=')
exp=$(($(date +%s) + 28800))
payload=$(echo -n "{\"sub\":\"admin-user\",\"iss\":\"hexalith-dev\",\"aud\":\"hexalith-tenants\",\"tenants\":[\"system\"],\"exp\":$exp}" | openssl base64 -A | tr '+/' '-_' | tr -d '=')
sig=$(echo -n "$header.$payload" | openssl dgst -sha256 -hmac "this-is-a-development-signing-key-minimum-32-chars" -binary | openssl base64 -A | tr '+/' '-_' | tr -d '=')
echo "$header.$payload.$sig"
```

Copy the output token — you need it in the next step.

> **How it works:** The default local Keycloak realm is imported from `src/Hexalith.Tenants.AppHost/KeycloakRealms/hexalith-realm.json` and is configured for the `hexalith-eventstore` audience used by the AppHost. The `EnableKeycloak=false` fallback uses a hardcoded HMAC-SHA256 signing key with issuer `hexalith-dev` and audience `hexalith-tenants`; the `tenants: ["system"]` source claim is normalized by EventStore into `eventstore:tenant=system`. For production IdP mappings, see [Production Auth Claim Contract](production-auth-claim-contract.md).
>
> **Production note:** Production deployments use OIDC authority-based JWT validation, not the local HMAC signing key. Before release, run the [Production Auth Readiness](production-auth-readiness.md) checklist and smoke tests.

## Send Your First Commands

### Open Swagger UI

Find the `eventstore` service in the Aspire dashboard and open its URL. Append `/swagger` to the URL to open the Swagger UI.

1. Click the **Authorize** button at the top of the page
2. In the **Value** field, paste the token you generated — do not include the `Bearer` prefix, Swagger adds it automatically
3. Click **Authorize**, then **Close**

### Step 1: Bootstrap the Global Administrator

Before creating tenants, you must authorize an administrator. Expand the **POST /api/v1/commands** endpoint, click **Try it out**, and submit:

```json
{
    "messageId": "01JQK000000000000000000001",
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
    "messageId": "01JQK000000000000000000002",
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
{ "correlationId": "01JQK000000000000000000002" }
```

The `Location` header points to the status polling endpoint (`/api/v1/commands/status/{correlationId}`). You can poll this endpoint until you see a terminal status:

```json
{
    "correlationId": "01JQK000000000000000000002",
    "status": "Completed",
    "statusCode": 5,
    "timestamp": "2026-03-19T12:00:01Z",
    "aggregateId": "my-first-tenant",
    "eventCount": 1,
    "rejectionEventType": null,
    "failureReason": null
}
```

`status: "Completed"` with `eventCount: 1` confirms the `TenantCreated` event was stored and published. A `status: "Rejected"` response means a business rule rejected the command — check `rejectionEventType` for the reason.

### Verify the Event

Verify the tenant was created by querying the read model. Expand the **GET /api/tenants/{tenantId}** endpoint, enter `my-first-tenant` as the tenant ID, and execute.

The response should contain the tenant details including the name and description you provided.

> **Note:** If the query returns 404, retry after 3–5 seconds. Projections are eventually consistent — the read model processes events asynchronously. If 404 persists beyond 30 seconds, check the Aspire dashboard for service errors and verify the command reached `status: "Completed"` via the status endpoint.

You can also check the command status via the URL in the `Location` header from the previous response, or query it directly: `GET /api/v1/commands/status/{correlationId}`.

### Running the Quickstart Again

If you've run this before:

- **BootstrapGlobalAdmin** will return a rejection (`GlobalAdminAlreadyBootstrappedRejection`) — this is correct behavior, the admin was already created.
- **CreateTenant** with the same ID will return a rejection (`TenantAlreadyExistsRejection`) — use a different `aggregateId` and `TenantId`, e.g., `my-second-tenant`.

### Try More Commands

Create a multi-step workflow — add a user to your new tenant:

**1. Add a user to the tenant:**

```json
{
    "messageId": "01JQK000000000000000000003",
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

Register tenant client services and the event types your service handles in your DI container:

```csharp
builder.Services
    .AddHexalithTenants()
    .AddTenantEventHandler<UserAddedToTenant, SampleLoggingEventHandler>()
    .AddTenantEventHandler<UserRemovedFromTenant, SampleLoggingEventHandler>()
    .AddTenantEventHandler<TenantDisabled, SampleLoggingEventHandler>();

WebApplication app = builder.Build();

app.UseCloudEvents();
app.MapSubscribeHandler();
app.MapTenantEventSubscription();
```

This is the same registration path used by [`samples/Hexalith.Tenants.Sample/Program.cs`](../samples/Hexalith.Tenants.Sample/Program.cs). It registers projection stores, options validation, DAPR client integration, and selected typed handlers. By default, the Client package binds the `Tenants` configuration section and subscribes through DAPR pub/sub component `pubsub` on the shared topic `tenants.events`. Consumers select event types by registering `ITenantEventHandler<TEvent>` implementations through `AddTenantEventHandler<TEvent, THandler>()`; do not create one DAPR topic per event type. DAPR pub/sub is at-least-once delivery, so handlers must be idempotent and must not assume cross-service ordering.

The built-in local projection updates `TenantLocalState` through `ITenantProjectionStore` so a consuming service can make tenant-aware runtime decisions without synchronously querying Tenants on every request. Add/remove/role/lifecycle/configuration reactions come from this local projection: membership events grant or revoke local capability, lifecycle events block or resume local operations after projection catch-up, and configuration events set or remove locally cached keys. The default store is in-memory and suitable for local or single-instance samples; scaled-out consumers should register a durable `ITenantProjectionStore` before calling `AddHexalithTenants()`. EventStore remains the durable source of truth, and local projection reads are eventually consistent with tenant commands.

Configuration keys are dot-delimited by convention. Consumers should read only keys under their owned namespace/prefix, such as `sample.` or `billing.`, and ignore unrelated keys unless that namespace is explicitly handled. The sample reaction path uses event subscription plus the local projection; it does not require polling, sync jobs, or per-request Tenants API calls.

Troubleshooting should use bounded metadata such as message ID, event type, tenant ID, and correlation ID. Do not log full event payloads; tenant configuration and user identifiers may be sensitive.

For event envelope fields, delivery semantics, and ordering limits, see [Event Contract Reference](event-contract-reference.md). For event handling patterns and idempotent processing, see [Idempotent Event Processing](idempotent-event-processing.md).

For a complete working example of a consuming service, see the sample at [`samples/Hexalith.Tenants.Sample/`](../samples/Hexalith.Tenants.Sample/).

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

Use `dapr init` (not `--slim`) — the Aspire topology requires the placement service.

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
