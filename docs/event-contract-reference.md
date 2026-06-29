[Back to README](../README.md)

# Event Contract Reference

Comprehensive reference for all tenant domain commands, events, and rejection events in Hexalith.Tenants. Use this document to design integrations, build event handlers, and understand the contract between the Tenant service and consuming services.

## Table of Contents

- [Event Delivery Model](#event-delivery-model)
- [Identity Scheme](#identity-scheme)
- [Contract Inventory](#contract-inventory)
- [Three-Outcome Model](#three-outcome-model)
- [Event Envelope Metadata](#event-envelope-metadata)
- [Serialization Shape](#serialization-shape)
- [Contract Stability](#contract-stability)
- [Enums](#enums)
    - [TenantRole](#tenantrole)
    - [TenantStatus](#tenantstatus)
    - [AuditEventCategory](#auditeventcategory)
- [TenantAggregate](#tenantaggregate)
    - [Tenant Lifecycle](#tenant-lifecycle)
    - [User-Role Management](#user-role-management)
    - [Tenant Configuration](#tenant-configuration)
- [GlobalAdministratorsAggregate](#globaladministratorsaggregate)
- [Rejection Events](#rejection-events)
    - [Rejection Table](#rejection-table)
    - [InsufficientPermissionsRejection Detail](#insufficientpermissionsrejection-detail)
    - [RFC 7807 Problem Details](#rfc-7807-problem-details)
- [Query API Reference](#query-api-reference)
- [Quick Reference](#quick-reference)
- [Idempotency](#idempotency)

---

## Event Delivery Model

All events are published via DAPR pub/sub as [CloudEvents 1.0](https://cloudevents.io/) on the shared topic **`tenants.events`**. Consumers filter by event type by registering typed handlers such as `AddEventStoreDomainEventHandler<UserAddedToTenant, MyHandler>()`; do not create one topic per event type.

The durable EventStore stream is the source of truth. Pub/sub publication happens after event storage and is asynchronous. If pub/sub is temporarily unavailable after a tenant event is stored, the command can still be accepted, the event remains committed, and EventStore drain recovery republishes the stored sequence range when the channel recovers. Operators should monitor `PublishFailed` command status transitions and related structured logs/metrics as delivery diagnostics, not as evidence that the source event was rolled back.

For command-status timing, subscriber lag, stale reads, and recovery guidance, see [Cross-Aggregate Timing](cross-aggregate-timing.md). For explicit correction workflows after mistaken access, role, configuration, or lifecycle changes, see [Compensating Commands](compensating-commands.md).

Commands that encounter infrastructure failures during processing (e.g., state rehydration errors, event persistence failures) produce events routed to the dead letter topic **`deadletter.tenants.events`**. Operators should monitor this topic for processing failures. Note: DAPR pub/sub may also have its own dead letter behavior for subscriber delivery failures, configured at the DAPR component level.

DAPR pub/sub is at-least-once delivery. Consumers must be idempotent and may see duplicate deliveries after retry or recovery. Do not depend on exactly-once publication or cross-service subscriber delivery order; use `MessageId` for duplicate detection and use `SequenceNumber` only as aggregate-local ordering metadata within one aggregate stream.

The Client package's built-in local projection is runtime state for the consuming service. It lets the service answer tenant-aware access, lifecycle, and configuration behavior checks from its own process/store instead of synchronously querying Tenants for every decision. `UserAddedToTenant`, `UserRoleChanged`, `UserRemovedFromTenant`, `TenantDisabled`, `TenantEnabled`, `TenantConfigurationSet`, and `TenantConfigurationRemoved` are applied by `TenantProjectionEventHandler` to `TenantLocalState`; the projection also keeps bounded `LastEvent` metadata for diagnostics: last message ID, aggregate-local sequence number, timestamp, and correlation ID. EventStore remains the durable source of truth, and each consuming service processes `tenants.events` independently; lifecycle/configuration reactions are eventually consistent with the tenant event stream, so do not assume immediate read-after-write visibility or matching observation time across services. Scaled-out services should use a bounded shared deduplication store and durable projection store when duplicate suppression or projection state must survive process restarts or coordinate across instances.

Commands are submitted through the EventStore command gateway. See the [Quickstart Guide](quickstart.md) for command submission details.

## Identity Scheme

All tenant domain events use the following identity components:

| Field           | Value                                                            | Description                                          |
| --------------- | ---------------------------------------------------------------- | ---------------------------------------------------- |
| Platform tenant | `system`                                                         | All tenant management runs under the platform tenant |
| Domain          | `tenants` or `global-administrators`                             | The aggregate domain; both publish to `tenants.events` |
| Aggregate ID    | Managed tenant ID (e.g., `acme-corp`) or `global-administrators` | Identifies the specific aggregate instance           |

The canonical composite identity is `system:tenants:{managedTenantId}` for managed tenant aggregates and `system:global-administrators:global-administrators` for the global administrator aggregate. Both aggregate families publish on the shared `tenants.events` topic; consumers filter by event type rather than by topic.

## Contract Inventory

All public contracts in this reference are owned by package `Hexalith.Tenants.Contracts`. The tables below are the source-backed index for commands, success events, rejections, queries, DTOs, and enums. Detailed field tables appear in the aggregate, rejection, query, and enum sections that follow.

### Command Contracts

| Contract | Package | Owning aggregate/domain | Fields | Intended caller | Success or rejection outcome |
| --- | --- | --- | --- | --- | --- |
| `CreateTenant` | `Hexalith.Tenants.Contracts` | `TenantAggregate` / `tenants` | `TenantId`, `Name`, `Description?` | Trusted global administrator through the command gateway | `TenantCreated`; `TenantAlreadyExistsRejection`, `InsufficientPermissionsRejection` |
| `UpdateTenant` | `Hexalith.Tenants.Contracts` | `TenantAggregate` / `tenants` | `TenantId`, `Name`, `Description?` | Tenant contributor/owner or trusted global administrator | `TenantUpdated`; `TenantNotFoundRejection`, `TenantDisabledRejection`, `InsufficientPermissionsRejection` |
| `DisableTenant` | `Hexalith.Tenants.Contracts` | `TenantAggregate` / `tenants` | `TenantId` | Trusted global administrator | `TenantDisabled`; `TenantNotFoundRejection`, `TenantLifecycleStateAlreadySetRejection`, `InsufficientPermissionsRejection` |
| `EnableTenant` | `Hexalith.Tenants.Contracts` | `TenantAggregate` / `tenants` | `TenantId` | Trusted global administrator | `TenantEnabled`; `TenantNotFoundRejection`, `TenantLifecycleStateAlreadySetRejection`, `InsufficientPermissionsRejection` |
| `AddUserToTenant` | `Hexalith.Tenants.Contracts` | `TenantAggregate` / `tenants` | `TenantId`, `UserId`, `Role` | Tenant owner or trusted global administrator; first tenant member bootstrap is allowed | `UserAddedToTenant`; `TenantNotFoundRejection`, `TenantDisabledRejection`, `UserAlreadyInTenantRejection`, `RoleEscalationRejection`, `InsufficientPermissionsRejection` |
| `RemoveUserFromTenant` | `Hexalith.Tenants.Contracts` | `TenantAggregate` / `tenants` | `TenantId`, `UserId` | Tenant owner or trusted global administrator | `UserRemovedFromTenant`; `TenantNotFoundRejection`, `TenantDisabledRejection`, `UserNotInTenantRejection`, `InsufficientPermissionsRejection` |
| `ChangeUserRole` | `Hexalith.Tenants.Contracts` | `TenantAggregate` / `tenants` | `TenantId`, `UserId`, `NewRole` | Tenant owner or trusted global administrator | `UserRoleChanged`; `TenantNotFoundRejection`, `TenantDisabledRejection`, `UserNotInTenantRejection`, `RoleEscalationRejection`, `InsufficientPermissionsRejection` |
| `SetTenantConfiguration` | `Hexalith.Tenants.Contracts` | `TenantAggregate` / `tenants` | `TenantId`, `Key`, `Value` | Tenant owner or trusted global administrator | `TenantConfigurationSet`; `TenantNotFoundRejection`, `TenantDisabledRejection`, `ConfigurationLimitExceededRejection`, `InsufficientPermissionsRejection` |
| `RemoveTenantConfiguration` | `Hexalith.Tenants.Contracts` | `TenantAggregate` / `tenants` | `TenantId`, `Key` | Tenant owner or trusted global administrator | `TenantConfigurationRemoved`; `TenantNotFoundRejection`, `TenantDisabledRejection`, `ConfigurationKeyNotFoundRejection`, `InsufficientPermissionsRejection` |
| `BootstrapGlobalAdmin` | `Hexalith.Tenants.Contracts` | `GlobalAdministratorsAggregate` / `global-administrators` | `UserId` | Startup/bootstrap host configuration or operator bootstrap path, not a public REST shortcut | `GlobalAdministratorSet`; `GlobalAdminAlreadyBootstrappedRejection` |
| `SetGlobalAdministrator` | `Hexalith.Tenants.Contracts` | `GlobalAdministratorsAggregate` / `global-administrators` | `UserId` | Existing global administrator | `GlobalAdministratorSet`; `InsufficientPermissionsRejection`, `GlobalAdministratorAlreadyExistsRejection` |
| `RemoveGlobalAdministrator` | `Hexalith.Tenants.Contracts` | `GlobalAdministratorsAggregate` / `global-administrators` | `UserId` | Existing global administrator | `GlobalAdministratorRemoved`; `InsufficientPermissionsRejection`, `GlobalAdministratorNotFoundRejection`, `LastGlobalAdministratorRejection` |

### Success Event Contracts

Every success event below is published to `tenants.events`. The EventStore envelope contains the event identity and version metadata; the payload contains the managed tenant identity as top-level `TenantId`. For global-administrator events that managed tenant identity is the platform tenant `system`.

| Contract | Package | Producing command(s) | Payload fields | Timestamp fields | Intended consumer use |
| --- | --- | --- | --- | --- | --- |
| `TenantCreated` | `Hexalith.Tenants.Contracts` | `CreateTenant` | `TenantId`, `Name`, `Description?`, `CreatedAt` | `CreatedAt` | Create or refresh local tenant lifecycle/configuration projections |
| `TenantUpdated` | `Hexalith.Tenants.Contracts` | `UpdateTenant` | `TenantId`, `Name`, `Description?`, `UpdatedAt` | `UpdatedAt` | Refresh tenant display metadata |
| `TenantDisabled` | `Hexalith.Tenants.Contracts` | `DisableTenant` | `TenantId`, `DisabledAt` | `DisabledAt` | Stop tenant-scoped access or background work |
| `TenantEnabled` | `Hexalith.Tenants.Contracts` | `EnableTenant` | `TenantId`, `EnabledAt` | `EnabledAt` | Resume tenant-scoped access or background work |
| `UserAddedToTenant` | `Hexalith.Tenants.Contracts` | `AddUserToTenant` | `TenantId`, `UserId`, `Role` | None | Grant local access for the user/tenant pair |
| `UserRemovedFromTenant` | `Hexalith.Tenants.Contracts` | `RemoveUserFromTenant` | `TenantId`, `UserId` | None | Revoke local access for the user/tenant pair |
| `UserRoleChanged` | `Hexalith.Tenants.Contracts` | `ChangeUserRole` | `TenantId`, `UserId`, `OldRole`, `NewRole` | None | Update local authorization decisions |
| `TenantConfigurationSet` | `Hexalith.Tenants.Contracts` | `SetTenantConfiguration` | `TenantId`, `Key`, `Value` | None | Apply namespaced tenant configuration values |
| `TenantConfigurationRemoved` | `Hexalith.Tenants.Contracts` | `RemoveTenantConfiguration` | `TenantId`, `Key` | None | Remove local configuration values |
| `GlobalAdministratorSet` | `Hexalith.Tenants.Contracts` | `BootstrapGlobalAdmin`, `SetGlobalAdministrator` | `TenantId`, `UserId`, `ActorUserId`, `SetAt` | `SetAt` | Update support/admin authorization projections |
| `GlobalAdministratorRemoved` | `Hexalith.Tenants.Contracts` | `RemoveGlobalAdministrator` | `TenantId`, `UserId`, `ActorUserId`, `RemovedAt` | `RemovedAt` | Update support/admin authorization projections |

### Query and DTO Contracts

All query contracts implement `IQueryContract`; controllers are REST adapters that dispatch through EventStore `SubmitQuery`. Query response DTOs are public contracts in the same package and are safe for consumers to deserialize by property name.

| Query contract | Package | `QueryType` | `Domain` | `ProjectionType` | Response shape | Intended REST adapter | Intended consumer |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `ListTenantsQuery` | `Hexalith.Tenants.Contracts` | `list-tenants` | `tenants` | `tenant-index` | `PaginatedResult<TenantSummary>` | `GET /api/tenants` | Administrative tenant list screens and service inventory jobs |
| `GetTenantQuery` | `Hexalith.Tenants.Contracts` | `get-tenant` | `tenants` | `tenants` | `TenantDetail` | `GET /api/tenants/{tenantId}` | Tenant details, lifecycle, membership, and configuration readers |
| `GetTenantUsersQuery` | `Hexalith.Tenants.Contracts` | `get-tenant-users` | `tenants` | `tenants` | `PaginatedResult<TenantMember>` | `GET /api/tenants/{tenantId}/users` | Tenant access review screens and owner tooling |
| `GetUserTenantsQuery` | `Hexalith.Tenants.Contracts` | `get-user-tenants` | `tenants` | `tenant-index` | `PaginatedResult<UserTenantMembership>` | `GET /api/users/{userId}/tenants` | User access review screens and self-service access views |
| `GetTenantAuditQuery` | `Hexalith.Tenants.Contracts` | `get-tenant-audit` | `tenants` | `tenants` | `PaginatedResult<TenantAuditEntry>` | `GET /api/tenants/{tenantId}/audit` | Support, audit, and compliance evidence workflows |
| `GetGlobalAdministratorsQuery` | `Hexalith.Tenants.Contracts` | `get-global-administrators` | `global-administrators` | `global-administrators` | `PaginatedResult<GlobalAdministratorSummary>` | `GET /api/global-administrators` | Platform governance review screens |

| DTO | Fields |
| --- | --- |
| `PaginatedResult<T>` | `Items`, `Cursor?`, `HasMore` |
| `TenantSummary` | `TenantId`, `Name`, `Status` |
| `TenantDetail` | `TenantId`, `Name`, `Description?`, `Status`, `Members`, `Configuration`, `CreatedAt` |
| `TenantMember` | `UserId`, `Role` |
| `UserTenantMembership` | `TenantId`, `Name`, `Status`, `Role` |
| `TenantAuditEntry` | `EventId`, `EventType`, `Category`, `ActorId`, `Timestamp`, `TenantId`, `NarrativePayload`, computed `Target`, computed `Scope`, computed `Outcome` |
| `GlobalAdministratorSummary` | `UserId` |

### Rejection and Enum Contracts

The rejection table later in this document lists all 14 public rejection contracts, their structured fields, and HTTP boundary mappings. Public enum contracts are `TenantRole`, `TenantStatus`, and `AuditEventCategory`; their values and serialization behavior are documented in [Enums](#enums).

## Three-Outcome Model

Every command produces exactly one of three outcomes:

| Outcome       | Description                                 | Events Produced                                     |
| ------------- | ------------------------------------------- | --------------------------------------------------- |
| **Success**   | Command accepted, state changed             | One or more domain events                           |
| **Rejection** | Business rule violated                      | A rejection event (e.g., `TenantNotFoundRejection`) |
| **NoOp**      | Command is valid but redundant (idempotent) | No events produced                                  |

## Event Envelope Metadata

All events are wrapped in EventStore's event envelope, which provides CloudEvents 1.0 compliance. Each envelope includes metadata fields: `MessageId`, `SequenceNumber`, `Timestamp`, `CorrelationId`, `CausationId`, `UserId`, and more.

Consumers commonly need these envelope fields:

| Metadata | Meaning for consumers |
| --- | --- |
| CloudEvents `id` | DAPR CloudEvents event identifier. Tenants publication stamps it from the command correlation ID plus aggregate-local sequence number. |
| CloudEvents `source` | Event source, currently `hexalith-eventstore/{tenantId}/{domain}`. |
| CloudEvents `type` | Fully-qualified event payload type name. Use it for event type filtering. |
| CloudEvents `specversion` | CloudEvents version; DAPR pub/sub uses CloudEvents 1.0. |
| EventStore `MessageId` | Stable persisted event identifier. Prefer this for deduplication. |
| EventStore `SequenceNumber` | Aggregate-local stream sequence/aggregate version. Use only within one aggregate stream. |
| EventStore `Timestamp` | Server persistence timestamp. |
| EventStore `CorrelationId` | Request/trace correlation ID from the command pipeline. |
| EventStore `CausationId` | Originating command message ID/idempotency key. |
| EventStore `UserId` | Authenticated actor user ID captured by EventStore. |

This document covers the **payload fields** — the domain-specific content inside each event. For the full envelope schema, see the [EventStore Event Envelope documentation](../references/Hexalith.EventStore/docs/concepts/event-envelope.md) ([GitHub link](https://github.com/Hexalith/Hexalith.EventStore/blob/main/docs/concepts/event-envelope.md)).

## Serialization Shape

Event payload bytes persisted by EventStore use `System.Text.Json` with default `System.Text.Json` options, so the domain payload examples in this reference use PascalCase contract property names such as `TenantId`, `CreatedAt`, `ActorUserId`, and `UpdatedAt` rather than REST/web camelCase. EventStore gateway HTTP command requests and some REST responses use web defaults at their own boundary; do not infer event payload casing from command-request examples.

Tenant contract enums add explicit converters:

- `TenantRole` uses `[JsonConverter(typeof(JsonStringEnumConverter<TenantRole>))]`, so values serialize by name, for example `"TenantContributor"`.
- `TenantStatus` uses `TenantStatusJsonConverter`, so values serialize by name and unknown or non-string input reads as `TenantStatus.Unknown`.
- `AuditEventCategory` appears in query DTOs and API responses by enum name at the HTTP boundary.

Timestamp fields are `DateTimeOffset`. Examples use an explicit offset such as `"2026-03-19T14:30:00+00:00"` so subscribers preserve timezone information.

## Contract Stability

> **Pre-v1.0 notice:** Schemas may change before v1.0. After v1.0, only additive changes (new fields with defaults) will be made.

**Example of a backward-compatible change:** In a future v1.1, a new optional field `tags` could be added to `TenantCreated`. Existing subscribers continue working because System.Text.Json ignores unknown properties by default (`JsonSerializerOptions.UnmappedMemberHandling` defaults to `Skip`).

**Forward-compatible enum handling (fail-closed):** Enums serialize **by name** and reserve ordinal `0` as a non-privileged `Unknown` sentinel (TEN-1/TEN-2). A payload with a missing role/status deserializes to `Unknown`; an unrecognized role name throws `JsonException`, while an unrecognized tenant status name materializes as `TenantStatus.Unknown`. Subscribers must treat `Unknown` (and any unrecognized value) as **fail-closed** — deny access and do not treat the tenant as active — never as a usable role or as `Active`. Do **not** map unknown roles to `TenantReader`. Phase 2 may add roles; new names are additive and old payloads remain decodable by name.

---

## Enums

### TenantRole

Defines the permission level of a user within a tenant. Serialized **by name** (e.g. `"TenantOwner"`) in both event payloads and query responses via `[JsonConverter(typeof(JsonStringEnumConverter<TenantRole>))]`. Ordinal `0` is the non-privileged `Unknown` sentinel: a missing or unrecognized role fails closed rather than mapping to a privileged role (TEN-1).

| Ordinal | Name                | Description                                                        |
| ------- | ------------------- | ------------------------------------------------------------------ |
| `0`     | `Unknown`           | Non-privileged sentinel — rejected by the aggregate, never granted |
| `1`     | `TenantOwner`       | Full administrative control over the tenant                        |
| `2`     | `TenantContributor` | Can perform operations within the tenant                           |
| `3`     | `TenantReader`      | Read-only access to tenant data                                    |

### TenantStatus

Defines the operational state of a tenant. Serialized **by name** (e.g. `"Active"`) via `TenantStatusJsonConverter`. Ordinal `0` is the non-active `Unknown` sentinel: an absent or unrecognized status never defaults to `Active` (TEN-2).

| Ordinal | Name       | Description                                                          |
| ------- | ---------- | -------------------------------------------------------------------- |
| `0`     | `Unknown`  | Non-active sentinel — absent/unrecognized status is never active     |
| `1`     | `Active`   | Tenant is operational                                                |
| `2`     | `Disabled` | Tenant is suspended — commands that modify tenant state are rejected |

### AuditEventCategory

Categorizes audit query rows. It is used by `TenantAuditEntry.Category` and the `GetTenantAuditQuery` REST adapter filter.

| Name | Description |
| --- | --- |
| `Access` | Access and role management event |
| `Administrative` | Tenant administration and configuration event |

---

## TenantAggregate

Commands and events for managing individual tenants. Each tenant is an aggregate instance identified by its tenant ID (e.g., `acme-corp`).

### Tenant Lifecycle

#### CreateTenant

Creates a new tenant. Requires trusted global administrator authority from the server-populated command envelope. The aggregate uses the envelope aggregate ID as the canonical managed tenant ID; command payload fields cannot retarget the operation.

**Command fields:**

| Field         | Type    | Description                      |
| ------------- | ------- | -------------------------------- |
| `TenantId`    | string  | Unique identifier for the tenant |
| `Name`        | string  | Display name                     |
| `Description` | string? | Optional description             |

**Success event:** `TenantCreated`

| Field         | Type           | Description                         |
| ------------- | -------------- | ----------------------------------- |
| `TenantId`    | string         | The created tenant's ID             |
| `Name`        | string         | Display name                        |
| `Description` | string?        | Optional description                |
| `CreatedAt`   | DateTimeOffset | Server-generated creation timestamp |

Published on topic: `tenants.events`

<details>
<summary>JSON example</summary>

```json
{
    "TenantId": "acme-corp",
    "Name": "Acme Corporation",
    "Description": "Primary business tenant",
    "CreatedAt": "2026-03-19T14:30:00+00:00"
}
```

</details>

**Rejections:** `TenantAlreadyExistsRejection`, `InsufficientPermissionsRejection`

---

#### UpdateTenant

Updates a tenant's name and description.

**Command fields:**

| Field         | Type    | Description      |
| ------------- | ------- | ---------------- |
| `TenantId`    | string  | Target tenant ID |
| `Name`        | string  | New display name |
| `Description` | string? | New description  |

**Success event:** `TenantUpdated`

| Field         | Type    | Description             |
| ------------- | ------- | ----------------------- |
| `TenantId`    | string  | The updated tenant's ID |
| `Name`        | string  | New display name        |
| `Description` | string? | New description         |
| `UpdatedAt`   | DateTimeOffset | Server-generated update timestamp |

Published on topic: `tenants.events`

<details>
<summary>JSON example</summary>

```json
{
    "TenantId": "acme-corp",
    "Name": "Acme Corporation International",
    "Description": "Updated business tenant",
    "UpdatedAt": "2026-03-19T15:45:00+00:00"
}
```

</details>

**Rejections:** `TenantNotFoundRejection`, `TenantDisabledRejection`, `InsufficientPermissionsRejection` (minimum role: `TenantContributor`)

---

#### DisableTenant

Disables a tenant. Disabled tenants reject modification commands.

**Command fields:**

| Field      | Type   | Description      |
| ---------- | ------ | ---------------- |
| `TenantId` | string | Target tenant ID |

**Success event:** `TenantDisabled`

| Field        | Type           | Description                        |
| ------------ | -------------- | ---------------------------------- |
| `TenantId`   | string         | The disabled tenant's ID           |
| `DisabledAt` | DateTimeOffset | Server-generated disable timestamp |

Published on topic: `tenants.events`

<details>
<summary>JSON example</summary>

```json
{
    "TenantId": "acme-corp",
    "DisabledAt": "2026-03-19T15:00:00+00:00"
}
```

</details>

**Rejections:** `TenantNotFoundRejection`, `TenantLifecycleStateAlreadySetRejection`, `InsufficientPermissionsRejection`
**Duplicate lifecycle state:** If the tenant is already disabled, `TenantLifecycleStateAlreadySetRejection` is produced with the current and requested status.

> Requires trusted global administrator authority.

---

#### EnableTenant

Re-enables a previously disabled tenant.

**Command fields:**

| Field      | Type   | Description      |
| ---------- | ------ | ---------------- |
| `TenantId` | string | Target tenant ID |

**Success event:** `TenantEnabled`

| Field       | Type           | Description                       |
| ----------- | -------------- | --------------------------------- |
| `TenantId`  | string         | The enabled tenant's ID           |
| `EnabledAt` | DateTimeOffset | Server-generated enable timestamp |

Published on topic: `tenants.events`

<details>
<summary>JSON example</summary>

```json
{
    "TenantId": "acme-corp",
    "EnabledAt": "2026-03-19T15:30:00+00:00"
}
```

</details>

**Rejections:** `TenantNotFoundRejection`, `TenantLifecycleStateAlreadySetRejection`, `InsufficientPermissionsRejection`
**Duplicate lifecycle state:** If the tenant is already active, `TenantLifecycleStateAlreadySetRejection` is produced with the current and requested status.

> Requires trusted global administrator authority.

---

### User-Role Management

#### AddUserToTenant

Adds a user to a tenant with a specified role.

**Command fields:**

| Field      | Type             | Description                                             |
| ---------- | ---------------- | ------------------------------------------------------- |
| `TenantId` | string           | Target tenant ID                                        |
| `UserId`   | string           | User to add                                             |
| `Role`     | TenantRole (string) | Role to assign: `"TenantOwner"`, `"TenantContributor"`, or `"TenantReader"` |

**Success event:** `UserAddedToTenant`

| Field      | Type             | Description    |
| ---------- | ---------------- | -------------- |
| `TenantId` | string           | The tenant ID  |
| `UserId`   | string           | The added user |
| `Role`     | TenantRole (string) | Assigned role  |

Published on topic: `tenants.events`

<details>
<summary>JSON example</summary>

```json
{
    "TenantId": "acme-corp",
    "UserId": "jane-doe",
    "Role": "TenantContributor"
}
```

</details>

**Rejections:** `TenantNotFoundRejection`, `TenantDisabledRejection`, `UserAlreadyInTenantRejection`, `RoleEscalationRejection`, `InsufficientPermissionsRejection`

---

#### RemoveUserFromTenant

Removes a user from a tenant.

**Command fields:**

| Field      | Type   | Description      |
| ---------- | ------ | ---------------- |
| `TenantId` | string | Target tenant ID |
| `UserId`   | string | User to remove   |

**Success event:** `UserRemovedFromTenant`

| Field      | Type   | Description      |
| ---------- | ------ | ---------------- |
| `TenantId` | string | The tenant ID    |
| `UserId`   | string | The removed user |

Published on topic: `tenants.events`

<details>
<summary>JSON example</summary>

```json
{
    "TenantId": "acme-corp",
    "UserId": "jane-doe"
}
```

</details>

**Rejections:** `TenantNotFoundRejection`, `TenantDisabledRejection`, `UserNotInTenantRejection`, `InsufficientPermissionsRejection`

---

#### ChangeUserRole

Changes a user's role within a tenant.

**Command fields:**

| Field      | Type             | Description        |
| ---------- | ---------------- | ------------------ |
| `TenantId` | string           | Target tenant ID   |
| `UserId`   | string           | Target user        |
| `NewRole`  | TenantRole (string) | New role to assign |

**Success event:** `UserRoleChanged`

| Field      | Type             | Description                          |
| ---------- | ---------------- | ------------------------------------ |
| `TenantId` | string           | The tenant ID                        |
| `UserId`   | string           | The user whose role changed          |
| `OldRole`  | TenantRole (string) | Previous role (from aggregate state) |
| `NewRole`  | TenantRole (string) | New role                             |

Published on topic: `tenants.events`

<details>
<summary>JSON example</summary>

```json
{
    "TenantId": "acme-corp",
    "UserId": "jane-doe",
    "OldRole": "TenantContributor",
    "NewRole": "TenantOwner"
}
```

</details>

**Rejections:** `TenantNotFoundRejection`, `TenantDisabledRejection`, `UserNotInTenantRejection`, `RoleEscalationRejection`, `InsufficientPermissionsRejection`
**NoOp:** If `NewRole` equals the user's current role, no event is produced.

---

### Tenant Configuration

#### SetTenantConfiguration

Sets a configuration key-value pair on a tenant. Requires a tenant owner or trusted global administrator; tenant contributors cannot change tenant configuration. Keys follow a dot-delimited namespace convention (e.g., `billing.plan`, `parties.maxContacts`). The namespace shape is a convention, not a regex-enforced contract; the service preserves accepted key text exactly. Keys must be present and non-empty, but whitespace-only keys are currently accepted for backward compatibility. Subscribing services should filter their local projection reads by owned prefix to process only their own namespace - for example, `key.StartsWith("billing.", StringComparison.Ordinal)` for the Billing service. The sample consumer uses the same pattern for `sample.` keys and ignores unrelated namespaces without polling, sync jobs, or per-request Tenants API calls.

**Command fields:**

| Field      | Type   | Description                                 |
| ---------- | ------ | ------------------------------------------- |
| `TenantId` | string | Target tenant ID                            |
| `Key`      | string | Configuration key (dot-delimited namespace) |
| `Value`    | string | Configuration value                         |

**Success event:** `TenantConfigurationSet`

| Field      | Type   | Description         |
| ---------- | ------ | ------------------- |
| `TenantId` | string | The tenant ID       |
| `Key`      | string | Configuration key   |
| `Value`    | string | Configuration value |

Published on topic: `tenants.events`

<details>
<summary>JSON example</summary>

```json
{
    "TenantId": "acme-corp",
    "Key": "billing.plan",
    "Value": "enterprise"
}
```

</details>

**Rejections:** `TenantNotFoundRejection`, `TenantDisabledRejection`, `ConfigurationLimitExceededRejection`, `InsufficientPermissionsRejection`
**NoOp:** If the key already exists with the same value, no event is produced.

**Configuration limits:** A tenant can store up to 100 configuration keys. A key can contain up to 256 characters. A value can contain up to 1024 `string.Length` characters. `ConfigurationLimitExceededRejection.LimitType` uses `KeyCount`, `KeyLength`, or `ValueSize`; `CurrentCount` reports the current key count or submitted key/value length, and `MaxAllowed` reports the configured limit. Oversized values are not stored in the rejection payload.

---

#### RemoveTenantConfiguration

Removes a configuration key from a tenant. Requires a tenant owner or trusted global administrator; tenant contributors cannot remove tenant configuration.

**Command fields:**

| Field      | Type   | Description                 |
| ---------- | ------ | --------------------------- |
| `TenantId` | string | Target tenant ID            |
| `Key`      | string | Configuration key to remove |

**Success event:** `TenantConfigurationRemoved`

| Field      | Type   | Description               |
| ---------- | ------ | ------------------------- |
| `TenantId` | string | The tenant ID             |
| `Key`      | string | Removed configuration key |

Published on topic: `tenants.events`

<details>
<summary>JSON example</summary>

```json
{
    "TenantId": "acme-corp",
    "Key": "billing.plan"
}
```

</details>

**Rejections:** `TenantNotFoundRejection`, `TenantDisabledRejection`, `ConfigurationKeyNotFoundRejection`, `InsufficientPermissionsRejection`
**Missing key:** If the key does not exist in the tenant's configuration, `ConfigurationKeyNotFoundRejection` is produced and no `TenantConfigurationRemoved` event is produced.

---

## GlobalAdministratorsAggregate

Commands and events for managing global administrators. The GlobalAdministratorsAggregate is a **singleton** using aggregate ID `global-administrators`.

> **Note:** GlobalAdmin commands do **not** include a `TenantId` field. The `TenantId` field in GlobalAdmin events is always `"system"` (the platform tenant context).

> **Authorization:** `BootstrapGlobalAdmin` is the only first-administrator path. `SetGlobalAdministrator` and `RemoveGlobalAdministrator` require the actor in the trusted command envelope to already be present in `GlobalAdministratorsState`.

#### BootstrapGlobalAdmin

Bootstraps the first global administrator. Can only be called once.

**Command fields:**

| Field    | Type   | Description                             |
| -------- | ------ | --------------------------------------- |
| `UserId` | string | User to designate as first global admin |

**Success event:** `GlobalAdministratorSet`

| Field         | Type           | Description                                                |
| ------------- | -------------- | ---------------------------------------------------------- |
| `TenantId`    | string         | Always `"system"`                                          |
| `UserId`      | string         | The designated administrator                               |
| `ActorUserId` | string         | Actor recorded for audit; for bootstrap this is `UserId`   |
| `SetAt`       | DateTimeOffset | Server-generated timestamp for the assignment              |

Published on topic: `tenants.events`

<details>
<summary>JSON example</summary>

```json
{
    "TenantId": "system",
    "UserId": "admin-user",
    "ActorUserId": "admin-user",
    "SetAt": "2026-03-19T14:30:00+00:00"
}
```

</details>

**Rejections:** `GlobalAdminAlreadyBootstrappedRejection`

> **Note:** `BootstrapGlobalAdmin` and `SetGlobalAdministrator` produce the same event type (`GlobalAdministratorSet`).

---

#### SetGlobalAdministrator

Designates a user as a global administrator.

**Command fields:**

| Field    | Type   | Description                       |
| -------- | ------ | --------------------------------- |
| `UserId` | string | User to designate as global admin |

**Success event:** `GlobalAdministratorSet`

| Field         | Type           | Description                                   |
| ------------- | -------------- | --------------------------------------------- |
| `TenantId`    | string         | Always `"system"`                             |
| `UserId`      | string         | The designated administrator                  |
| `ActorUserId` | string         | Existing global administrator making the call |
| `SetAt`       | DateTimeOffset | Server-generated timestamp for the assignment |

Published on topic: `tenants.events`

<details>
<summary>JSON example</summary>

```json
{
    "TenantId": "system",
    "UserId": "sofia-admin",
    "ActorUserId": "admin-user",
    "SetAt": "2026-03-19T15:00:00+00:00"
}
```

</details>

**Rejections:** `InsufficientPermissionsRejection`, `GlobalAdministratorAlreadyExistsRejection`

---

#### RemoveGlobalAdministrator

Removes a user from the global administrator list.

**Command fields:**

| Field    | Type   | Description                           |
| -------- | ------ | ------------------------------------- |
| `UserId` | string | User to remove from global admin role |

**Success event:** `GlobalAdministratorRemoved`

| Field         | Type           | Description                                   |
| ------------- | -------------- | --------------------------------------------- |
| `TenantId`    | string         | Always `"system"`                             |
| `UserId`      | string         | The removed administrator                     |
| `ActorUserId` | string         | Existing global administrator making the call |
| `RemovedAt`   | DateTimeOffset | Server-generated timestamp for the removal    |

Published on topic: `tenants.events`

<details>
<summary>JSON example</summary>

```json
{
    "TenantId": "system",
    "UserId": "sofia-admin",
    "ActorUserId": "admin-user",
    "RemovedAt": "2026-03-19T15:30:00+00:00"
}
```

</details>

**Rejections:** `InsufficientPermissionsRejection`, `GlobalAdministratorNotFoundRejection`, `LastGlobalAdministratorRejection`

---

## Rejection Events

Rejection events are produced when a command violates a business rule. All rejections implement `IRejectionEvent` and are persisted in the event store alongside success events, providing a complete audit trail.

### Rejection Table

| Rejection                                      | Fields                                                       | HTTP Status | HTTP Boundary Corrective Action                                                                 |
| ---------------------------------------------- | ------------------------------------------------------------ | ----------- | ----------------------------------------------------------------------------------------------- |
| `TenantAlreadyExistsRejection`                 | `TenantId`                                                   | 409         | Use a different identifier or treat the existing resource as the current state.                  |
| `TenantNotFoundRejection`                      | `TenantId`                                                   | 404         | Verify the identifier and tenant context, then retry with an existing resource.                  |
| `TenantDisabledRejection`                      | `TenantId`                                                   | 422         | Review the rejection detail, correct the request, and retry when appropriate.                    |
| `TenantLifecycleStateAlreadySetRejection`      | `TenantId`, `CurrentStatus`, `RequestedStatus`, `CommandName` | 409         | Use a different identifier or treat the existing resource as the current state.                  |
| `GlobalAdminAlreadyBootstrappedRejection`      | `TenantId`                                                   | 409         | Use a different identifier or treat the existing resource as the current state.                  |
| `GlobalAdministratorAlreadyExistsRejection`    | `TenantId`, `UserId`                                         | 409         | Use a different identifier or treat the existing resource as the current state.                  |
| `GlobalAdministratorNotFoundRejection`         | `TenantId`, `UserId`                                         | 404         | Verify the identifier and tenant context, then retry with an existing resource.                  |
| `LastGlobalAdministratorRejection`             | `TenantId`, `UserId`                                         | 422         | Review the rejection detail, correct the request, and retry when appropriate.                    |
| `UserAlreadyInTenantRejection`                 | `TenantId`, `UserId`, `ExistingRole`                         | 409         | Use a different identifier or treat the existing resource as the current state.                  |
| `UserNotInTenantRejection`                     | `TenantId`, `UserId`                                         | 422         | Review the rejection detail, correct the request, and retry when appropriate.                    |
| `RoleEscalationRejection`                      | `TenantId`, `UserId`, `AttemptedRole`                        | 422         | Review the rejection detail, correct the request, and retry when appropriate.                    |
| `InsufficientPermissionsRejection`             | `TenantId`, `ActorUserId`, `ActorRole?`, `CommandName`        | 422         | Review the rejection detail, correct the request, and retry when appropriate.                    |
| `ConfigurationLimitExceededRejection`          | `TenantId`, `LimitType`, `CurrentCount`, `MaxAllowed`         | 422         | Review the rejection detail, correct the request, and retry when appropriate.                    |
| `ConfigurationKeyNotFoundRejection`            | `TenantId`, `Key`                                             | 404         | Verify the configuration key and tenant context, then retry with an existing key.                |

<details>
<summary>Structured rejection JSON example</summary>

```json
{
    "TenantId": "acme-corp",
    "CurrentStatus": "Disabled",
    "RequestedStatus": "Disabled",
    "CommandName": "DisableTenant"
}
```

</details>

### InsufficientPermissionsRejection Detail

The `ActorRole` field is **nullable**:

- **`null`** — the actor is not a member of the tenant at all. Corrective action: add the user to the tenant first with `AddUserToTenant`.
- **Non-null** — the actor has the specified role but it is insufficient. Corrective action: the user has role `{ActorRole}` but needs `TenantOwner` or `GlobalAdministrator` for this command.

### RFC 7807 Problem Details

Domain command rejections returned from `POST /api/v1/commands` use [RFC 7807 Problem Details](https://www.rfc-editor.org/rfc/rfc7807). The `type` field is a stable domain-rejection URI, not just the event type name. The URI suffix is the `reasonCode`, derived from the rejection event type.

```json
{
    "type": "https://hexalith.io/problems/domain-rejections/tenant-not-found-rejection",
    "title": "Tenant Not Found Rejection",
    "detail": "The command referenced a domain resource that does not exist in the requested tenant context.",
    "status": 404,
    "instance": "/api/v1/commands",
    "correlationId": "abc-123",
    "tenantId": "system",
    "reasonCode": "tenant-not-found-rejection",
    "rejectionType": "Hexalith.Tenants.Contracts.Events.Rejections.TenantNotFoundRejection",
    "correctiveAction": "Verify the identifier and tenant context, then retry with an existing resource."
}
```

The `title`, `detail`, HTTP status, and `correctiveAction` are composed by EventStore's HTTP boundary/catalog. Persisted rejection events remain structured data only. Problem Details responses must not echo raw command payload JSON, serialized rejection event payload JSON, bearer tokens, stack traces, local paths, or sensitive tenant/user values.

### Optimistic Concurrency Conflicts

Persistence-level optimistic concurrency conflicts are EventStore command-pipeline outcomes, not Tenants domain rejection events. EventStore retries state-store conflicts that occur before a successful `EventsStored` checkpoint up to `EventStore:CommandConcurrency:MaxPersistenceConflictRetries` times; the default is `1`.

Each retry rehydrates the latest aggregate state and invokes Tenants domain logic again, so membership, role, and configuration commands evaluate against the ordered event sequence already committed by the winning command. If the retry limit is exhausted, command status is `Rejected` with `FailureReason == "ConcurrencyConflict"`, and the public command endpoint returns sanitized HTTP `409` ProblemDetails with `Retry-After: 1` and the request correlation ID. The response does not expose aggregate IDs, tenant IDs, state-store keys, ETags, payloads, stack traces, tokens, or local paths.

Idempotency records are written only after a terminal command result is known. Replaying a duplicate causation ID after success, domain rejection, no-op, publish-failed, or terminal concurrency conflict returns the cached terminal result and does not append duplicate tenant events.

---

## Query API Reference

Tenant query endpoints are protected REST read adapters over in-process domain query handlers. Controllers validate route/query input, derive the authenticated user from JWT `sub`, validate signed opaque cursors, then dispatch through the Tenants query dispatcher to the relevant `IDomainQueryHandler`. Query authorization and row filtering are handled by the query handler path.

| Endpoint | Query contract | Response |
| --- | --- | --- |
| `GET /api/tenants` | `ListTenantsQuery` | `PaginatedResult<TenantSummary>` |
| `GET /api/tenants/{tenantId}` | `GetTenantQuery` | `TenantDetail` |
| `GET /api/tenants/{tenantId}/users` | `GetTenantUsersQuery` | `PaginatedResult<TenantMember>` |
| `GET /api/users/{userId}/tenants` | `GetUserTenantsQuery` | `PaginatedResult<UserTenantMembership>` |
| `GET /api/tenants/{tenantId}/audit` | `GetTenantAuditQuery` | `PaginatedResult<TenantAuditEntry>` |
| `GET /api/global-administrators` | `GetGlobalAdministratorsQuery` | `PaginatedResult<GlobalAdministratorSummary>` |

Paginated responses use the standard shape `{ "items": [...], "cursor": "...", "hasMore": true }`. Standard query endpoints default to page size `20` and clamp at `100`. Audit queries default to page size `100` and clamp at `1000`.

Cursors are signed, opaque, and bound to the query type and authorization scope. A cursor generated for a different tenant, target user, requester, date range, category, or query shape is rejected with a safe validation error and must not reveal embedded tenant IDs, user IDs, filters, or internal state.

`GET /api/tenants/{tenantId}/audit` accepts optional `from`, `to`, `category`, `cursor`, and `pageSize` query parameters. Audit rows are projection-backed and include:

| Field | Meaning |
| --- | --- |
| `eventId` | Event reference used for stable ordering and support correlation |
| `eventType` | Tenant event type that produced the audit row |
| `category` | Audit category, serialized by enum name |
| `actorId` | Support-safe actor identifier from event metadata |
| `timestamp` | Event timestamp |
| `tenantId` | Tenant scope for the audit row |
| `target` | Best-effort target derived from narrative payload (`userId`, `key`, or tenant ID) |
| `scope` | Tenant scope for the row |
| `outcome` | Event type outcome |
| `narrativePayload` | Support-safe key/value summary, not a raw event payload dump |

Global-administrator events are also projected into system-scoped audit state under `audit:system`, so `GlobalAdministratorSet` and `GlobalAdministratorRemoved` can be queried through the same audit contract when the system tenant scope is requested.

---

## Quick Reference

| Command                     | Success Event                | Possible Rejections                                                                                                                                 |
| --------------------------- | ---------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------- |
| `CreateTenant`              | `TenantCreated`              | `TenantAlreadyExistsRejection`, `InsufficientPermissionsRejection`                                                                                  |
| `UpdateTenant`              | `TenantUpdated`              | `TenantNotFoundRejection`, `TenantDisabledRejection`, `InsufficientPermissionsRejection`                                                            |
| `DisableTenant`             | `TenantDisabled`             | `TenantNotFoundRejection`, `TenantLifecycleStateAlreadySetRejection`, `InsufficientPermissionsRejection`                                            |
| `EnableTenant`              | `TenantEnabled`              | `TenantNotFoundRejection`, `TenantLifecycleStateAlreadySetRejection`, `InsufficientPermissionsRejection`                                            |
| `AddUserToTenant`           | `UserAddedToTenant`          | `TenantNotFoundRejection`, `TenantDisabledRejection`, `UserAlreadyInTenantRejection`, `RoleEscalationRejection`, `InsufficientPermissionsRejection` |
| `RemoveUserFromTenant`      | `UserRemovedFromTenant`      | `TenantNotFoundRejection`, `TenantDisabledRejection`, `UserNotInTenantRejection`, `InsufficientPermissionsRejection`                                |
| `ChangeUserRole`            | `UserRoleChanged`            | `TenantNotFoundRejection`, `TenantDisabledRejection`, `UserNotInTenantRejection`, `RoleEscalationRejection`, `InsufficientPermissionsRejection`     |
| `SetTenantConfiguration`    | `TenantConfigurationSet`     | `TenantNotFoundRejection`, `TenantDisabledRejection`, `ConfigurationLimitExceededRejection`, `InsufficientPermissionsRejection`                     |
| `RemoveTenantConfiguration` | `TenantConfigurationRemoved` | `TenantNotFoundRejection`, `TenantDisabledRejection`, `ConfigurationKeyNotFoundRejection`, `InsufficientPermissionsRejection`                       |
| `BootstrapGlobalAdmin`      | `GlobalAdministratorSet`     | `GlobalAdminAlreadyBootstrappedRejection`                                                                                                           |
| `SetGlobalAdministrator`    | `GlobalAdministratorSet`     | `InsufficientPermissionsRejection`, `GlobalAdministratorAlreadyExistsRejection`                                                                      |
| `RemoveGlobalAdministrator` | `GlobalAdministratorRemoved` | `InsufficientPermissionsRejection`, `GlobalAdministratorNotFoundRejection`, `LastGlobalAdministratorRejection`                                      |

## Idempotency

All events include `MessageId` and `SequenceNumber` in the event envelope. Consumers should use `MessageId` for deduplication.

DAPR pub/sub guarantees **at-least-once delivery**, not exactly-once. Network retries, sidecar restarts, and redelivery can cause the same event to arrive multiple times. Without deduplication, this can lead to incorrect state.

`SequenceNumber` can help reason about ordering inside one aggregate stream, such as one managed tenant aggregate. It must not be treated as global ordering across services, tenants, aggregate types, subscriber instances, or redelivery attempts. The sample-consuming-service and Client projection rely on idempotent set/remove operations and bounded last-event metadata rather than global ordering.

For detailed idempotent processing patterns, including message-level deduplication and handler-level idempotency, see [Idempotent Event Processing](idempotent-event-processing.md).
