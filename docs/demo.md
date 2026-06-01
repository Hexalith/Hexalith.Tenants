[Back to README](../README.md)

# "Aha Moment" Demo — Reactive Cross-Service Access Revocation

This demo proves the core value of event-sourced tenant management: when you remove a user from a tenant, every subscribing service revokes access automatically — no custom integration code, no polling, no webhooks. Just a DAPR pub/sub event subscription.

The demo sequence (Steps 1–6) takes **under 2 minutes** once the topology is running. Initial one-time setup (AppHost startup, JWT generation) is separate preparation — allow 5–10 minutes on first run.

## Prerequisites

Before you begin, verify:

- **.NET 10 SDK** — `dotnet --version` should show `10.x.xxx`
- **DAPR CLI + Runtime** — `dapr --version` should show both CLI and runtime versions. Run `dapr init` (full init, **not** `--slim` — the Aspire topology requires the full DAPR runtime with placement service)
- **Docker** — `docker info` should show engine details. Docker Desktop must be running

These are the same prerequisites as the [Quickstart](quickstart.md#prerequisites).

## Start the Topology

Start the Aspire AppHost, which launches the EventStore command gateway, the Tenants domain service, the Sample consuming service, Keycloak, DAPR sidecars, and Redis:

```bash
dotnet run --project src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj
```

> **Note:** The first run pulls Docker images — allow 5–10 minutes. Subsequent starts are much faster.

In the terminal output, look for a line like:

```
Login to the dashboard at https://localhost:17225/login?t=...
```

Open this URL in your browser. This is the **Aspire dashboard**. You will use it throughout the demo to find service URLs and view logs.

### Find Your Service URLs

In the Aspire dashboard:

1. Click **eventstore** to find the EventStore command gateway base URL (e.g., `https://localhost:{port}`)
2. Click **sample** to find the Sample service base URL

Note these URLs for the remaining steps.

> **Aspire assigns ports dynamically** — your ports will differ from the examples below. Replace `{eventstore-url}` and `{sample-url}` with your actual URLs throughout this guide.

## Get a JWT Token

Generate a JWT token following the [Quickstart JWT section](quickstart.md#get-an-access-token). Copy the output token — you need it for the demo commands.

## Demo Sequence

### Set Up Swagger UI

Open `{eventstore-url}/swagger` in your browser. Click the **Authorize** button (top right), paste your JWT token in the **Value** field (do not include the `Bearer` prefix — Swagger adds it automatically), and click **Authorize**, then **Close**.

You are now authenticated for all subsequent requests.

### Step 1: Bootstrap Global Admin

Expand **POST /api/v1/commands**, click **Try it out**, and submit:

```json
{
    "messageId": "01JQK000000000000000000011",
    "tenant": "system",
    "domain": "global-administrators",
    "aggregateId": "global-administrators",
    "commandType": "BootstrapGlobalAdmin",
    "payload": {
        "UserId": "admin-user"
    }
}
```

**Observe:** The EventStore command gateway returns `202 Accepted`. No Sample service log entry — GlobalAdmin events don't trigger sample handlers.

> **Important:** The `UserId` in the payload must match the `sub` claim in your JWT token. The quickstart JWT scripts use `"admin-user"` as the `sub` claim.

### Step 2: Create a Tenant

```json
{
    "messageId": "01JQK000000000000000000012",
    "tenant": "system",
    "domain": "tenants",
    "aggregateId": "acme-demo",
    "commandType": "CreateTenant",
    "payload": {
        "TenantId": "acme-demo",
        "Name": "Acme Demo Corp",
        "Description": "Demo tenant for aha moment"
    }
}
```

**Observe:** The EventStore command gateway returns `202 Accepted`.

> **Re-running the demo?** If you've run this before, `BootstrapGlobalAdmin` will return `GlobalAdminAlreadyBootstrappedRejection` (safe to ignore — bootstrap already done) and `CreateTenant` will return `TenantAlreadyExistsRejection`. Use a different tenant ID (e.g., `acme-demo-2`) and matching `aggregateId`.

### Step 3: Add a User with TenantContributor Role

```json
{
    "messageId": "01JQK000000000000000000013",
    "tenant": "system",
    "domain": "tenants",
    "aggregateId": "acme-demo",
    "commandType": "AddUserToTenant",
    "payload": {
        "TenantId": "acme-demo",
        "UserId": "jane-doe",
        "Role": "TenantContributor"
    }
}
```

> **Roles:** Use the enum names `TenantOwner`, `TenantContributor`, or `TenantReader`. `Unknown` is the fail-closed sentinel and is rejected by the aggregate.

**Observe** (Aspire dashboard → `sample` → Logs): `[Sample] UserAddedToTenant processed for tenant acme-demo; message ...; correlation ...`

### Step 4: Verify Access in Sample Service

**Wait for the log:** Before checking the endpoint, confirm you see the `[Sample] UserAddedToTenant processed for tenant acme-demo...` log message in the Aspire dashboard → `sample` → Logs. This confirms the event has been processed by the local projection.

Open `{sample-url}/access/acme-demo/jane-doe` in your browser (find `{sample-url}` from the Aspire dashboard → `sample` → Endpoints).

**Observe:**

```json
{
    "tenantId": "acme-demo",
    "userId": "jane-doe",
    "access": "granted",
    "role": "TenantContributor"
}
```

### Step 5: Remove the User — THE AHA MOMENT

```json
{
    "messageId": "01JQK000000000000000000014",
    "tenant": "system",
    "domain": "tenants",
    "aggregateId": "acme-demo",
    "commandType": "RemoveUserFromTenant",
    "payload": {
        "TenantId": "acme-demo",
        "UserId": "jane-doe"
    }
}
```

**Observe** (Aspire dashboard → `sample` → Logs): `[Sample] UserRemovedFromTenant processed for tenant acme-demo; revoking projected access; message ...; correlation ...`

**Observe:** `{sample-url}/access/acme-demo/jane-doe` now returns:

```json
{
    "tenantId": "acme-demo",
    "userId": "jane-doe",
    "access": "denied",
    "reason": "User is not a member"
}
```

**THIS IS THE AHA MOMENT:** The consuming service automatically revoked access — no custom integration code, no polling, no webhook. Just a DAPR pub/sub event subscription.

### Step 6: Verify Current State and Understand the Audit Trail

**Wait for the log:** Before checking access, confirm you see the `[Sample] UserRemovedFromTenant processed for tenant acme-demo...` log message in the Aspire dashboard → `sample` → Logs. This confirms the revocation event has been processed by the local projection.

Open `{eventstore-url}/api/tenants/acme-demo` in the Swagger UI (authenticated) or append `?access_token={token}` for browser access.

**Observe:** Tenant details showing the current state — the tenant exists with an empty members list (jane-doe was added then removed).

**Audit trail note:** The query endpoint above shows the CURRENT projection state, not the event history. The full audit trail — `TenantCreated` → `UserAddedToTenant` → `UserRemovedFromTenant` with timestamps and actor IDs — lives in the event store. In the event-sourced model, no state change is ever lost: the add, the remove, who did it, and when are all preserved as immutable events.

> **Audit query endpoint:** The API exposes `GET /api/tenants/{tenantId}/audit` for date-range audit queries. It is restricted to global administrators and returns projection-backed audit rows when the tenant audit projection has processed matching events. For event schema details and temporal auditability patterns, see [Event Contract Reference](event-contract-reference.md).

## What Just Happened?

Here's the architecture behind the demo:

1. The **EventStore command gateway** processed each command and stored events atomically in the event store
2. Events were **published asynchronously** via DAPR pub/sub to the `tenants.events` topic
3. The **Sample service** received each event via its subscription endpoint
4. The Sample's `SampleLoggingEventHandler` logged the event
5. The Sample's local projection (`ITenantProjectionStore`) was updated automatically
6. The `/access` endpoint reads from the **local projection** — no synchronous calls back to Tenants or EventStore

**Multi-service note:** This demo shows one subscribing service for simplicity. In production, any number of services can subscribe to the same `tenants.events` topic — each would independently receive the `UserRemovedFromTenant` event and revoke access in its own local projection simultaneously. The architecture supports this with zero additional configuration — each new subscriber just adds `AddHexalithTenants()` and a DAPR pub/sub subscription.

## Next Steps

- **[Quickstart Guide](quickstart.md)** — Full setup and first tenant creation walkthrough
- **[Event Contract Reference](event-contract-reference.md)** — Complete event schemas, field semantics, and temporal audit patterns
- **[Sample Consuming Service Walkthrough](sample-consuming-service-walkthrough.md)** — How the sample subscription, projection, access check, and configuration endpoint are implemented
- **[Sample Service Source](../samples/Hexalith.Tenants.Sample/)** — See how the consuming service stays under 20 meaningful tenant registration lines

## Troubleshooting

### HTTPS Certificate Errors

Aspire assigns HTTPS URLs with development certificates that are not trusted by default.

- **curl:** Add `-k` or `--insecure` flag
- **PowerShell `Invoke-RestMethod`:** Add `-SkipCertificateCheck`
- **Browsers:** Click through the certificate warning
- Alternatively, check if the Aspire dashboard shows HTTP endpoints alongside HTTPS

### `TenantAlreadyExists` on Re-run

If running the demo a second time, use a different tenant ID (e.g., `acme-demo-2`) or expect `TenantAlreadyExistsRejection` and `GlobalAdminAlreadyBootstrappedRejection` — these are correct behavior.

### `401 Unauthorized`

JWT token expired or claims mismatch — regenerate using the [quickstart instructions](quickstart.md#get-an-access-token).

### Access Endpoint Returns "not a member" Immediately After AddUserToTenant

Event propagation is asynchronous — wait 1–2 seconds for the event to reach the Sample service's local projection before checking `/access`.

### Access Endpoint Returns 500 Error

The sample uses the Client package's default in-memory `ITenantProjectionStore`. Restarting the sample clears projected state until events are replayed or delivered again. Check the `sample` logs for invalid payload or handler failures, and restart the AppHost if the sample process is unhealthy.

### First Command Fails With Connection Error (not 401/4xx)

DAPR sidecars take a few seconds to initialize after the AppHost starts. Wait 10–15 seconds and retry. Check sidecar status in the Aspire dashboard — each service should show its sidecar as "Running".
