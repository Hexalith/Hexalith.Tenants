# Tenants DAPR Deployment Templates

This folder contains production-oriented DAPR templates for deploying Hexalith.Tenants beside Hexalith.EventStore. The application-facing contract is stable across local and production modes:

- AppIds: `eventstore`, `tenants`, `eventstore-admin`, `eventstore-admin-ui`, `sample`, `tenants-api`, `tenants-ui`
- State store component: `statestore`
- Pub/sub component: `pubsub`
- Event topic: `tenants.events`
- Application-level dead-letter topic: `deadletter.tenants.events`

The templates intentionally keep infrastructure choice in DAPR YAML. Do not add Redis, broker, database, or cloud-provider SDK references to Tenants domain packages.

## Data Protection Key Ring (cursor durability)

The opaque query pagination cursor codec is backed by an ASP.NET Core Data Protection key ring. In production the key ring is persisted to the shared `statestore` DAPR component (via a `DaprClient`-backed `IXmlRepository` provided by `Hexalith.EventStore.DomainService.AddEventStoreDataProtection`), so:

- a cursor sealed by one Tenants replica can be unprotected by any other replica, and
- outstanding cursors survive pod restarts and rolling deploys (no intermittent 400s from a regenerated ephemeral ring).

The backing infrastructure is whatever `statestore.yaml` selects (Redis here) — no infrastructure SDK is added to the Tenants domain assembly. The selected DAPR state store must support ETag / first-write concurrency because key writes use compare-and-swap retries to merge concurrent key generation during rollout. Configuration lives under `EventStore:DataProtection`:

| Key | Default | Notes |
| --- | --- | --- |
| `EventStore:DataProtection:PersistToStateStore` | `true` (prod `appsettings.json`), `false` (`appsettings.Development.json`) | When `false`, an explicit ephemeral per-host key ring is used (safe for single-replica local/dev); the host then starts without a state store. |
| `EventStore:DataProtection:StateStoreName` | `statestore` | The DAPR state-store component that persists the ring. Must be scoped to `tenants` (it already is in `statestore.yaml`) and should not be scoped to untrusted app IDs. |
| `EventStore:DataProtection:StateKey` | `hexalith-tenants-dataprotection-keys` | Application-specific state key under which the key-ring elements are stored. The component is shared with EventStore/Admin for platform state, so key namespacing prevents accidental ring commingling; the DAPR state store remains a trusted boundary. |
| `EventStore:DataProtection:OperationTimeout` | `00:00:30` | Maximum duration allowed for each synchronous key-ring state-store operation. |

Operators running multi-replica Tenants must keep `PersistToStateStore: true` and ensure `statestore` is reachable; otherwise each replica mints cursors against its own ephemeral ring.

## Local Development Mode

Normal local development should run full `dapr init` before the Aspire AppHost starts. Full init provides Redis, actor placement, and scheduler services used by EventStore aggregate actors and Tenants projection flows.

Expected local ports used by existing tests:

- Redis: `localhost:6379`
- Placement: `50005` on Linux, `6050` on Windows
- Scheduler: `50006` on Linux, `6060` on Windows

Local AppHost components live in `src/Hexalith.Tenants.AppHost/DaprComponents`. The AppHost reuses the Platform-owned Sentry, mTLS placement/scheduler and generated-sidecar bindings, using private per-run credentials outside the checkout and allocated listeners. All connected sidecars use mTLS so API-to-EventStore and actor flows retain the same identity boundary. The historical domain access-control file remains local-only and allow-by-default. The generated API receiver uses its dedicated deny-by-default `accesscontrol.tenants-api.yaml`, allowing only the `tenants-ui` sidecar to invoke the six GET reads.

## Slim Self-Hosted Mode

`dapr init --slim` does not install Redis, placement, scheduler, or Zipkin. Operators using slim mode must provide these prerequisites before actor flows can work:

- A state store component named `statestore` with `actorStateStore: "true"`
- A pub/sub component named `pubsub`
- A placement service reachable by all actor-hosting sidecars
- A scheduler service reachable by DAPR sidecars

Actor startup failures in slim mode usually mean placement, scheduler, or the actor state store is missing. Check those prerequisites before changing application code.

## Production Mode

Apply the templates in this folder after replacing the placeholders with environment or secret-store values suitable for your platform:

- `statestore.yaml` is scoped to `eventstore`, `tenants`, and `eventstore-admin`.
- `pubsub.yaml` is scoped to `eventstore` and `sample`; `eventstore` is left unlisted in `publishingScopes` so it keeps unrestricted publish access (required for EventStore dynamic per-tenant topic provisioning, NFR20), `sample` is denied publishing via an empty topic list (`sample=`), and `sample` subscribes to `tenants.events` in demo deployments.
- `accesscontrol.tenants.yaml` is bound only to the Tenants sidecar. It uses `defaultAction: deny` and allows only `eventstore` to call `POST /process` and `POST /project`.
- `accesscontrol.eventstore.yaml` is bound only to the EventStore sidecar. It allows `eventstore-admin` GET/POST/PUT `/**`, `tenants-api` GET `/ready` and POST `/api/v1/queries` or `/api/v1/commands`, and `tenants` POST `/api/v1/commands` for configured bootstrap. Every other caller and route is denied; EventStore still authorizes the command itself.
- `accesscontrol.tenants-ui.yaml` is bound only to the UI caller sidecar. It denies inbound peer invocation and selects the same trust domain used by the API receiver; both sidecars must use the same `DAPR_TRUST_DOMAIN`, matching the SPIFFE trust domain actually issued by the deployment's identity provider. Self-hosted Dapr/Sentry can issue `public` identities even when configured otherwise; verify the issued identity and resolve this template accordingly.
- `accesscontrol.tenants-api.yaml` is bound only to the generated API receiver. It allows `tenants-ui` GET for list, detail, users, memberships, audit and global administrators; other callers and verbs are denied. Dapr trie matching also admits descendants of matching path prefixes; generated API routing enforces the declared read actions. The domain receiver remains POST-only.
- `accesscontrol.eventstore-admin.yaml` is bound only to Admin.Server and exposes no peer DAPR invocation policies.
- `resiliency.yaml` preserves the local retry and timeout intent for state-store and pub/sub operations. Its explicit `tenants-api` app target sets `noReadRetry` (`maxRetries: 0`) and a five-second timeout; the explicit target also bypasses Dapr built-in service retries. Load this Resiliency on the UI caller sidecar.

## BFF read transport and identifiers

Run the UI with its own `tenants-ui` sidecar and bind the API receiver's dedicated ACL. Configure the API app channel as HTTPS (`--app-protocol https`, with the application's HTTPS port). `DAPR_HTTP_ENDPOINT` selects the BFF's local sidecar and takes precedence over `Tenants:BaseAddress`; an explicitly configured `Tenants:BaseAddress` must also address that sidecar when no endpoint is injected. Sidecar failures fail closed without trying a direct API address. User bearer forwarding runs before the platform-owned invocation handler, which authoritatively sets `dapr-app-id: tenants-api` and the optional `DAPR_API_TOKEN`. Commands and status retain their independent EventStore address.

The four parameterized reads send `X-Hexalith-Route-Encoding: base64url-v1` and canonical unpadded UTF-8 base64url segments. The API decodes identifiers before tenant-scope resolution and query authorization. This preserves slash, backslash, dots, Unicode and literal percent text without traversal. Missing discriminator preserves existing literal-route clients; unknown or duplicate discriminators, padding, noncanonical trailing bits and invalid UTF-8 produce 400. List and global-administrator reads carry no route-identifier discriminator. Cursors remain opaque query parameters, and conditional headers and response metadata pass through the sidecars. The generated API's prior conditional-response acceptance remains unchanged; the controlled 304 check qualifies the additional Dapr transport.

## Pub/Sub Recovery Evidence

EventStore remains the source of truth for tenant events. Pub/sub publication happens after the event has been stored, so a `PublishFailed` command status means publication failed after persistence; it is not evidence that the tenant event was lost or rolled back. EventStore drain recovery republishes the persisted sequence range to `tenants.events` after the pub/sub path recovers.

DAPR pub/sub delivery is at-least-once. Subscriber redelivery and duplicate deliveries are expected during retries, sidecar restarts, and recovery. Consumers must deduplicate by `MessageId`, treat `SequenceNumber` as aggregate-local metadata only, and avoid exactly-once or global-ordering assumptions. Subscriber redelivery remains on `tenants.events`; this repository does not configure a DAPR native dead-letter subscription on the `pubsub` component. The `deadletter.tenants.events` topic is produced by EventStore's application-level dead-letter publisher for command-processing infrastructure failures, not by Redis pub/sub component metadata; keep the pub/sub component and `resiliency.yaml` retry settings reviewed together.

> **Operator note — scope of the "no native dead-letter" statement.** The claim that this repository configures no DAPR native dead-letter subscription is scoped to the `pubsub` component shipped here (`deploy/dapr/pubsub.yaml`). If you instead bind the Tenants services to an EventStore-provided pub/sub component, that component may set its own `enableDeadLetter` / `deadLetterTopic` metadata (for example `references/Hexalith.EventStore/samples/dapr-components/redis/pubsub.yaml`) and enable native dead-lettering that this repo's template does not. Confirm which pub/sub component your deployment actually binds before relying on the "no native DLQ" behavior.

Operator support-safe evidence should record command-status states such as `EventsStored`, `PublishFailed`, `EventsPublished`, and `Completed`; topic names; event type names; aggregate-local sequence categories; correlation/message identifier categories; prerequisite availability; and pass/fail/skip counts. Do not record raw event payloads, compact JWTs, bearer tokens, signing keys, decoded payloads, concrete connection strings, production hosts, real tenant/user identifiers, or PII.

## Failure Triage

| Symptom | Likely issue | What to check |
| --- | --- | --- |
| Actor calls fail at startup | missing placement | Verify placement is reachable on the expected port and sidecars use the correct placement address. |
| Actor reminders or scheduled actor work fail | missing scheduler | Verify scheduler is reachable on the expected port and sidecars use the correct scheduler address. |
| State operations fail with component not found | missing state store or wrong component name | Confirm a component named `statestore` is loaded and scoped to the calling AppId. |
| Events do not publish or subscribe | missing pub/sub or wrong component name | Confirm a component named `pubsub` is loaded and scoped to `eventstore` and subscriber AppIds. |
| EventStore cannot call Tenants domain processing | wrong AppId or denied service invocation | Confirm EventStore sidecar AppId is `eventstore`, Tenants sidecar AppId is `tenants`, and `accesscontrol.tenants.yaml` allows `POST /process`. |
| EventStore cannot call Tenants projection dispatch | wrong AppId or denied service invocation | Confirm `accesscontrol.tenants.yaml` allows `POST /project` only from `eventstore`. |
| Component exists but calls still fail | wrong component scope | Confirm `scopes` contains the app ID that uses the component. |
| Access-control failures appear in sidecar logs | denied service invocation | Check the receiver-specific `Configuration` file bound to the called sidecar, not the caller sidecar. |

Do not treat static YAML validation as live deployment proof. Live smoke tests still need a prepared environment with DAPR, Redis or the selected production backing services, placement, scheduler, mTLS, and the selected orchestration platform.
