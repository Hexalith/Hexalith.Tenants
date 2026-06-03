[Back to README](../README.md)

# Sample Consuming Service Walkthrough

This guide explains the existing sample consuming service in
[`samples/Hexalith.Tenants.Sample/`](../samples/Hexalith.Tenants.Sample/).
Use it as the source-backed path for copying tenant event subscription and
local projection access checks into another service. The sample is not a new
integration path; it uses the same `Hexalith.Tenants.Client` APIs that package
consumers use.

## File map

| Walkthrough step | Source file |
| ---------------- | ----------- |
| Package/project references | `samples/Hexalith.Tenants.Sample/Hexalith.Tenants.Sample.csproj` |
| DI/subscription setup | `samples/Hexalith.Tenants.Sample/Program.cs` |
| Custom logging handler | `samples/Hexalith.Tenants.Sample/Handlers/SampleLoggingEventHandler.cs` |
| Local projection access endpoint | `samples/Hexalith.Tenants.Sample/Endpoints/AccessCheckEndpoints.cs` |
| Configuration endpoint | `samples/Hexalith.Tenants.Sample/Endpoints/TenantConfigurationEndpoints.cs` |
| AppHost sample registration | `src/Hexalith.Tenants.AppHost/Program.cs` and `src/Hexalith.Tenants.AppHost/HexalithTenantsSample.cs` |
| Sample tests | `samples/Hexalith.Tenants.Sample.Tests/Registration/SampleRegistrationTests.cs`, `samples/Hexalith.Tenants.Sample.Tests/Endpoints/AccessCheckEndpointsTests.cs`, `samples/Hexalith.Tenants.Sample.Tests/Endpoints/TenantConfigurationEndpointsTests.cs`, and `samples/Hexalith.Tenants.Sample.Tests/Handlers/SampleLoggingEventHandlerTests.cs` |

## Package references

The sample project references the local source projects:

- `src/Hexalith.Tenants.Client/Hexalith.Tenants.Client.csproj`
- `src/Hexalith.Tenants.Contracts/Hexalith.Tenants.Contracts.csproj`

That is deliberate for this repository. A normal consuming service installs the
matching NuGet packages instead:

```bash
dotnet add package Hexalith.Tenants.Contracts
dotnet add package Hexalith.Tenants.Client
```

`Hexalith.Tenants.Contracts` supplies event payloads such as
`UserAddedToTenant`, `TenantDisabled`, and `TenantConfigurationSet`.
`Hexalith.Tenants.Client` supplies DI registration, typed handler dispatch,
the built-in local projection handler, DAPR subscription mapping, and
`ITenantProjectionStore`.

## Subscription setup

The real setup in `samples/Hexalith.Tenants.Sample/Program.cs` is intentionally
small. `samples/Hexalith.Tenants.Sample.Tests/Registration/SampleRegistrationTests.cs`
counts the meaningful tenant registration lines with the same predicate used by
documentation validation: non-empty, non-comment lines containing
`AddHexalithTenants`, `AddEventStoreDomainEventHandler`, `UseCloudEvents`,
`MapSubscribeHandler`, or `MapEventStoreDomainEvents`. The current target stays
under 20 meaningful lines.

```csharp
builder.Services
    .AddHexalithTenants()
    .AddEventStoreDomainEventHandler<UserAddedToTenant, SampleLoggingEventHandler>()
    .AddEventStoreDomainEventHandler<UserRemovedFromTenant, SampleLoggingEventHandler>()
    .AddEventStoreDomainEventHandler<TenantDisabled, SampleLoggingEventHandler>();

WebApplication app = builder.Build();

app.UseCloudEvents();
app.MapSubscribeHandler();
app.MapEventStoreDomainEvents();
```

Reusable package setup:

- `AddHexalithTenants()` registers tenant client services, default options, the
  DAPR client, `EventStoreDomainEventProcessor`, the built-in
  `TenantProjectionEventHandler`, and the default `InMemoryTenantProjectionStore`
  when no `ITenantProjectionStore` is already registered.
- `AddEventStoreDomainEventHandler<TEvent, THandler>()` registers custom typed handlers
  for event payloads your service cares about.
- `UseCloudEvents()` enables CloudEvents request handling for DAPR pub/sub.
- `MapSubscribeHandler()` exposes DAPR's subscription discovery endpoint.
- `MapEventStoreDomainEvents()` maps the Tenants event subscription endpoint.

Sample-specific teaching surfaces:

- `SampleLoggingEventHandler` logs a small set of events for demo visibility.
- `/access/{tenantId}/{userId}` shows one projection-backed authorization check.
- `/configuration/{tenantId}/sample` shows namespace-filtered configuration
  reads from the local projection.

`EventStoreDomainEventsOptions` supplies these subscription defaults:

- DAPR pub/sub component: `pubsub`
- Shared topic: `tenants.events`

`MapEventStoreDomainEvents()` maps the programmatic subscription endpoint at
`/tenants/events`.

Consumers filter by event type through typed handlers. Consumers must not create one DAPR topic per tenant event type. All tenant events flow on the shared
`tenants.events` topic, and each service decides what to handle locally.

## Projection updates

`AddHexalithTenants()` registers `TenantProjectionEventHandler`, the built-in
handler that applies Tenants events to `TenantLocalState` through
`ITenantProjectionStore`. The handler updates these local projection fields:

| Event | Local projection effect |
| ----- | ----------------------- |
| `TenantCreated` | Creates or updates the local tenant row, sets name, description, and active status. |
| `TenantUpdated` | Updates name and description metadata. |
| `TenantDisabled` | Sets status to disabled. |
| `TenantEnabled` | Sets status to active. |
| `UserAddedToTenant` | Sets the user's role in `TenantLocalState.Members`. |
| `UserRemovedFromTenant` | Removes the user from `TenantLocalState.Members`. |
| `UserRoleChanged` | Replaces the user's role in `TenantLocalState.Members`. |
| `TenantConfigurationSet` | Sets a key/value entry in `TenantLocalState.Configuration`. |
| `TenantConfigurationRemoved` | Removes a key from `TenantLocalState.Configuration`. |

These operations are naturally idempotent for local state: dictionary set writes
the latest value for a key, and dictionary remove leaves the projection in the
same state when repeated. Side effects outside the projection, such as sending
notifications or writing an outbox record, still need explicit deduplication.
Use event `MessageId` for that deduplication, not `SequenceNumber`, which is
aggregate-local metadata.

EventStore remains the durable source of truth. The sample projection catches up
asynchronously from `tenants.events`, so endpoint responses are local projection state and show eventual consistency. Do not describe these endpoints as
synchronous Tenants truth, and do not add per-request synchronous lookups back to
Tenants or EventStore for this sample pattern.

For full schemas and envelope metadata, see
[`docs/event-contract-reference.md`](event-contract-reference.md). For duplicate
delivery and projection patterns, see
[`docs/idempotent-event-processing.md`](idempotent-event-processing.md). For
propagation timing and cross-aggregate ordering boundaries, see
[`docs/cross-aggregate-timing.md`](cross-aggregate-timing.md).

## Access endpoint behavior

`samples/Hexalith.Tenants.Sample/Endpoints/AccessCheckEndpoints.cs` maps
`/access/{tenantId}/{userId}`. The endpoint depends on `ITenantProjectionStore`;
it does not use `HttpClient`, `DaprClient`, or a synchronous Tenants query.

The access check:

- returns `400` when `tenantId` or `userId` is blank;
- returns `404` when the tenant is not present in the local projection;
- denies disabled tenants;
- denies tenants with unknown or non-active status;
- denies users missing from `TenantLocalState.Members`;
- denies `TenantRole.Unknown` and out-of-range role values;
- grants known active members with `TenantOwner`, `TenantContributor`, or
  `TenantReader`.

That is sample policy, not a package-level authorization rule. Your service owns
the mapping from tenant roles to capabilities.

## Configuration endpoint behavior

`samples/Hexalith.Tenants.Sample/Endpoints/TenantConfigurationEndpoints.cs` maps
`/configuration/{tenantId}/sample`. It also reads only `ITenantProjectionStore`.

The endpoint filters `TenantLocalState.Configuration` to keys beginning with the
`sample.` prefix, strips that prefix in the response, and ignores unrelated
namespaces such as `billing.plan`. The point is the namespace boundary: a real
service should read only keys it owns, for example `billing.` or `notifications.`
when those namespaces belong to that service.

## AppHost registration

The AppHost wires the sample as a subscriber in
`src/Hexalith.Tenants.AppHost/Program.cs` and locates the project through
`src/Hexalith.Tenants.AppHost/HexalithTenantsSample.cs`.

The sample uses DAPR AppId `sample` and receives a reference to the Tenants
pub/sub component. It does not receive the Tenants actor state-store reference.
That boundary matters: the consuming service reacts to tenant events and stores
its own projection; it does not read Tenants actor state directly.

## Safe to copy

These pieces are safe starting points for a real consuming service:

- package references for `Hexalith.Tenants.Contracts` and
  `Hexalith.Tenants.Client`;
- `AddHexalithTenants()`;
- typed handler registration with `AddEventStoreDomainEventHandler<TEvent, THandler>()`;
- CloudEvents middleware, `MapSubscribeHandler()`, and
  `MapEventStoreDomainEvents()`;
- the `IEventStoreDomainEventHandler<TEvent>` handler shape;
- reads from the `ITenantProjectionStore` abstraction;
- idempotent dictionary set/remove projection operations for local state.

## Application-specific

These choices belong to each consuming service:

- custom event handlers and which event types they handle;
- local endpoint routes;
- role-to-capability policy;
- configuration namespace prefix;
- durable projection store choice;
- durable deduplication store choice;
- logging levels;
- side effects such as notifications, outbox writes, cache invalidation, or
  downstream workflow triggers.

The sample custom logger handles only `UserAddedToTenant`,
`UserRemovedFromTenant`, and `TenantDisabled`. Role changes, tenant enablement,
and configuration changes still update the local projection through the built-in
`TenantProjectionEventHandler`; the sample logger simply does not log those
events.

## Deployment supplied

Your deployment supplies environment-specific values:

- DAPR AppId for the consuming service;
- pub/sub component name, usually `pubsub` unless explicitly overridden;
- access to the `tenants.events` topic;
- OIDC/JWT tokens and identity provider configuration;
- tenant IDs;
- user IDs;
- configuration keys and owned prefixes;
- secrets;
- production storage connection strings.

Use placeholders in docs, scripts, tickets, and logs. Do not paste raw bearer
tokens, decoded JWT payloads, secrets, full event payload logs, or sensitive
tenant/user data. Do not log full event payloads. The current
`SampleLoggingEventHandler` logs tenant ID plus message and correlation metadata,
and intentionally does not log the sample user ID or role.

## Production store guidance

The default `InMemoryTenantProjectionStore` is suitable for local development,
tests, and single-instance samples. It is not a scaled-out production store
because each instance has its own memory. Production consumers should register a
durable `ITenantProjectionStore` before `AddHexalithTenants()` so the default is
not used, and should use bounded/shared deduplication for side effects.

Keep the service eventually consistent: process tenant events, update the local
projection, and make local decisions with clear fallback behavior. For unknown
tenants, disabled tenants, unknown status, and unknown roles, fail closed.
