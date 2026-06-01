[Back to README](../README.md)

# "Aha Moment" Demo - Reactive Access Revocation

This demo proves the event-driven value of Tenants in a short, repeatable flow: create a tenant, add a user, watch the configured subscribing service grant local access, remove the user, then watch that same service deny access from its local projection. The timed proof starts after the AppHost, service URLs, and auth token are ready.

The current runnable AppHost includes one sample subscriber resource named `sample`. Additional services subscribe the same way: register `AddHexalithTenants()`, map `MapTenantEventSubscription()`, and subscribe to `tenants.events`.

## First-Run Setup

Use the [Quickstart](quickstart.md) for prerequisites, submodule setup, DAPR initialization, and the first local auth token. Start the AppHost:

```bash
dotnet run --project src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj
```

Open the Aspire dashboard URL printed by the AppHost. In the dashboard, collect these dynamic endpoints:

- `eventstore`: EventStore command gateway base URL for `POST /api/v1/commands` and `GET /api/v1/commands/status/{correlationId}`
- `tenants`: Tenants query API base URL for `GET /api/tenants/{tenantId}` and `GET /api/tenants/{tenantId}/audit`
- `sample`: Sample subscribing service base URL for `/access/{tenantId}/{userId}`
- `keycloak`: local identity provider base URL, unless you intentionally started with `EnableKeycloak=false`

Default local auth uses Keycloak and the `hexalith-eventstore` audience. Get a token with the [Quickstart token flow](quickstart.md#get-an-access-token). HMAC development tokens are only for the explicit `EnableKeycloak=false` fallback.

## 90-Second Proof

Use stable synthetic IDs for the narrated proof:

- Tenant: `acme-demo`
- User: `jane-doe`
- Actor: `admin-user`

Submit each command to `POST {eventstore-url}/api/v1/commands`. The response is `202 Accepted` with a `correlationId` and a `Location` header. Poll `GET {eventstore-url}/api/v1/commands/status/{correlationId}` until the command reaches a terminal status such as `Completed` or `Rejected`.

### 1. Bootstrap Global Admin

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

The payload `UserId` must match the authenticated JWT `sub` claim. On reruns, `GlobalAdminAlreadyBootstrappedRejection` is expected and safe to continue past.

### 2. Create Tenant

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

`aggregateId` must match `payload.TenantId`. On reruns, use a fresh tenant ID.

### 3. Add User

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

Watch Aspire dashboard -> `sample` -> logs for `UserAddedToTenant processed for tenant acme-demo`. The logging handler records tenant and message/correlation metadata; it does not print full event payloads.

Then call:

```text
GET {sample-url}/access/acme-demo/jane-doe
```

Expected local projection result:

```json
{
    "tenantId": "acme-demo",
    "userId": "jane-doe",
    "access": "granted",
    "role": "TenantContributor"
}
```

### 4. Remove User

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

Watch Aspire dashboard -> `sample` -> logs for `UserRemovedFromTenant processed for tenant acme-demo`.

Call the same local access endpoint again:

```text
GET {sample-url}/access/acme-demo/jane-doe
```

Expected result:

```json
{
    "tenantId": "acme-demo",
    "userId": "jane-doe",
    "access": "denied",
    "reason": "User is not a member"
}
```

This is the proof: the configured subscribing service denies access from local projection state after it receives the remove event. The `/access` endpoint does not call Tenants or EventStore synchronously and no custom polling or manual synchronization job is used.

### 5. Verify Current State and Audit Evidence

Query current tenant state:

```text
GET {tenants-url}/api/tenants/acme-demo
```

The current projection should show the tenant without `jane-doe` as an active member.

Query audit rows when the audit projection has processed the events:

```text
GET {tenants-url}/api/tenants/acme-demo/audit
```

The audit query returns projection-backed rows such as `TenantCreated`, `UserAddedToTenant`, and `UserRemovedFromTenant`, including actor and timestamp metadata. It is not a raw event payload dump. Avoid recording raw bearer tokens, decoded JWT payloads, secrets, full serialized event payloads, or real tenant/user data in screenshots or narration.

## Automated Script

The scripts run the add-user to remove-user proof once the topology and token are ready:

```bash
TOKEN="<redacted>" ./scripts/demo.sh \
  --base-url "{eventstore-url}" \
  --sample-url "{sample-url}" \
  --tenants-url "{tenants-url}"
```

```powershell
$env:TOKEN = "<redacted>"
./scripts/demo.ps1 -BaseUrl "{eventstore-url}" -SampleUrl "{sample-url}" -TenantsUrl "{tenants-url}"
```

For the intentional `EnableKeycloak=false` fallback only, pass `--hmac-dev-token` or `-HmacDevToken`. The generated fallback token targets the EventStore command gateway's local development auth settings. The scripts require dynamic URLs because Aspire assigns ports at runtime, generate valid ULID-shaped command IDs, poll command status, poll `/access/{tenantId}/{userId}` until `granted -> denied`, and print a compact summary without raw tokens or full event payloads.

## What Happened

1. Commands were accepted by the EventStore command gateway at `POST /api/v1/commands`.
2. EventStore stored tenant events and published them asynchronously to DAPR pub/sub topic `tenants.events`.
3. The `sample` service received events through `MapTenantEventSubscription()`.
4. `TenantProjectionEventHandler` updated `ITenantProjectionStore`.
5. `/access/{tenantId}/{userId}` read local projection state and failed closed after `UserRemovedFromTenant`.

This behavior is eventually consistent. EventStore is the durable source of truth; subscribers catch up asynchronously. Security-critical synchronous enforcement remains future work for the planned synchronous authorization plugin.

For implementation details, see the [Sample Consuming Service Walkthrough](sample-consuming-service-walkthrough.md), which shows the under 20 meaningful tenant registration lines, and the [Idempotent Event Processing](idempotent-event-processing.md) guidance for duplicate delivery handling.

## Related Guides

- [Quickstart Guide](quickstart.md)
- [Event Contract Reference](event-contract-reference.md)
- [Sample Consuming Service Walkthrough](sample-consuming-service-walkthrough.md)
- [Idempotent Event Processing](idempotent-event-processing.md)
- [Cross-Aggregate Timing](cross-aggregate-timing.md)

## Troubleshooting

- `401 Unauthorized`: refresh the Keycloak token from the quickstart flow, or confirm you intentionally started with `EnableKeycloak=false` before using the HMAC fallback.
- `TenantAlreadyExistsRejection`: use a fresh tenant ID and matching `aggregateId`.
- `/access` still denied after add: wait for the sample projection to catch up and verify command status is `Completed`.
- `/access` still granted after remove: verify `UserRemovedFromTenant` reached `Completed`, then check `sample` logs and DAPR sidecar health.
- Connection errors: confirm Docker, DAPR, Keycloak, Redis, `eventstore`, `tenants`, `sample`, and sidecars are healthy in the Aspire dashboard.
