[Back to README](../README.md)

# Event Contract Reference

Comprehensive reference for all tenant domain commands, events, and rejection events in Hexalith.Tenants. Use this document to design integrations, build event handlers, and understand the contract between the Tenant service and consuming services.

## Table of Contents

- [Event Delivery Model](#event-delivery-model)
- [Identity Scheme](#identity-scheme)
- [Three-Outcome Model](#three-outcome-model)
- [Event Envelope Metadata](#event-envelope-metadata)
- [Contract Stability](#contract-stability)
- [Enums](#enums)
    - [TenantRole](#tenantrole)
    - [TenantStatus](#tenantstatus)
- [TenantAggregate](#tenantaggregate)
    - [Tenant Lifecycle](#tenant-lifecycle)
    - [User-Role Management](#user-role-management)
    - [Tenant Configuration](#tenant-configuration)
- [GlobalAdministratorsAggregate](#globaladministratorsaggregate)
- [Rejection Events](#rejection-events)
    - [Rejection Table](#rejection-table)
    - [InsufficientPermissionsRejection Detail](#insufficientpermissionsrejection-detail)
    - [RFC 7807 Problem Details](#rfc-7807-problem-details)
- [Quick Reference](#quick-reference)
- [Idempotency](#idempotency)

---

## Event Delivery Model

All events are published via DAPR pub/sub as [CloudEvents 1.0](https://cloudevents.io/) on topic **`tenants.events`**. Consumers filter by event type to receive only the events they need.

Commands that encounter infrastructure failures during processing (e.g., state rehydration errors, event persistence failures) produce events routed to the dead letter topic **`deadletter.tenants.events`**. Operators should monitor this topic for processing failures. Note: DAPR pub/sub may also have its own dead letter behavior for subscriber delivery failures, configured at the DAPR component level.

Commands are submitted via the CommandApi. See the [Quickstart Guide](quickstart.md) for command submission details.

## Identity Scheme

All tenant domain events use the following identity components:

| Field           | Value                                                            | Description                                          |
| --------------- | ---------------------------------------------------------------- | ---------------------------------------------------- |
| Platform tenant | `system`                                                         | All tenant management runs under the platform tenant |
| Domain          | `tenants`                                                        | The domain service namespace                         |
| Aggregate ID    | Managed tenant ID (e.g., `acme-corp`) or `global-administrators` | Identifies the specific aggregate instance           |

The canonical composite identity is `system:tenants:{aggregateId}`.

## Three-Outcome Model

Every command produces exactly one of three outcomes:

| Outcome       | Description                                 | Events Produced                                     |
| ------------- | ------------------------------------------- | --------------------------------------------------- |
| **Success**   | Command accepted, state changed             | One or more domain events                           |
| **Rejection** | Business rule violated                      | A rejection event (e.g., `TenantNotFoundRejection`) |
| **NoOp**      | Command is valid but redundant (idempotent) | No events produced                                  |

## Event Envelope Metadata

All events are wrapped in EventStore's event envelope, which provides CloudEvents 1.0 compliance. Each envelope includes metadata fields: `MessageId`, `SequenceNumber`, `Timestamp`, `CorrelationId`, `CausationId`, `UserId`, and more.

This document covers the **payload fields** — the domain-specific content inside each event. For the full envelope schema, see the [EventStore Event Envelope documentation](../Hexalith.EventStore/docs/concepts/event-envelope.md) ([GitHub link](https://github.com/Hexalith/Hexalith.EventStore/blob/main/docs/concepts/event-envelope.md)).

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

---

## TenantAggregate

Commands and events for managing individual tenants. Each tenant is an aggregate instance identified by its tenant ID (e.g., `acme-corp`).

### Tenant Lifecycle

#### CreateTenant

Creates a new tenant. No domain-level permission check — any authenticated user can create a tenant. The Phase 2 auth plugin will enforce authorization at the pipeline level.

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

**Rejections:** `TenantAlreadyExistsRejection`

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

**Rejections:** `TenantNotFoundRejection`, `TenantLifecycleStateAlreadySetRejection`
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

**Rejections:** `TenantNotFoundRejection`, `TenantLifecycleStateAlreadySetRejection`
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
    "Role": 1
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
    "OldRole": 2,
    "NewRole": 1
}
```

</details>

**Rejections:** `TenantNotFoundRejection`, `TenantDisabledRejection`, `UserNotInTenantRejection`, `RoleEscalationRejection`, `InsufficientPermissionsRejection`
**NoOp:** If `NewRole` equals the user's current role, no event is produced.

---

### Tenant Configuration

#### SetTenantConfiguration

Sets a configuration key-value pair on a tenant. Keys follow a dot-delimited namespace convention (e.g., `billing.plan`, `parties.maxContacts`). Subscribing services should filter by key prefix to process only their own namespace — for example, `key.startsWith("billing.")` for the Billing service.

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

---

#### RemoveTenantConfiguration

Removes a configuration key from a tenant.

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

**Rejections:** `TenantNotFoundRejection`, `TenantDisabledRejection`, `InsufficientPermissionsRejection`
**NoOp:** If the key does not exist in the tenant's configuration, no event is produced.

---

## GlobalAdministratorsAggregate

Commands and events for managing global administrators. The GlobalAdministratorsAggregate is a **singleton** using aggregate ID `global-administrators`.

> **Note:** GlobalAdmin commands do **not** include a `TenantId` field. The `TenantId` field in GlobalAdmin events is always `"system"` (the platform tenant context).

> **Authorization:** GlobalAdmin commands have no domain-level permission checks in Phase 1. The Phase 2 auth plugin will enforce that only existing global administrators can call `SetGlobalAdministrator` and `RemoveGlobalAdministrator`.

#### BootstrapGlobalAdmin

Bootstraps the first global administrator. Can only be called once.

**Command fields:**

| Field    | Type   | Description                             |
| -------- | ------ | --------------------------------------- |
| `UserId` | string | User to designate as first global admin |

**Success event:** `GlobalAdministratorSet`

| Field      | Type   | Description                  |
| ---------- | ------ | ---------------------------- |
| `TenantId` | string | Always `"system"`            |
| `UserId`   | string | The designated administrator |

Published on topic: `tenants.events`

<details>
<summary>JSON example</summary>

```json
{
    "TenantId": "system",
    "UserId": "admin-user"
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

| Field      | Type   | Description                  |
| ---------- | ------ | ---------------------------- |
| `TenantId` | string | Always `"system"`            |
| `UserId`   | string | The designated administrator |

Published on topic: `tenants.events`

<details>
<summary>JSON example</summary>

```json
{
    "TenantId": "system",
    "UserId": "sofia-admin"
}
```

</details>

**NoOp:** If the user is already a global administrator, no event is produced.

---

#### RemoveGlobalAdministrator

Removes a user from the global administrator list.

**Command fields:**

| Field    | Type   | Description                           |
| -------- | ------ | ------------------------------------- |
| `UserId` | string | User to remove from global admin role |

**Success event:** `GlobalAdministratorRemoved`

| Field      | Type   | Description               |
| ---------- | ------ | ------------------------- |
| `TenantId` | string | Always `"system"`         |
| `UserId`   | string | The removed administrator |

Published on topic: `tenants.events`

<details>
<summary>JSON example</summary>

```json
{
    "TenantId": "system",
    "UserId": "sofia-admin"
}
```

</details>

**Rejections:** `LastGlobalAdministratorRejection`
**NoOp:** If the user is not a global administrator, no event is produced.

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

---

## Quick Reference

| Command                     | Success Event                | Possible Rejections                                                                                                                                 |
| --------------------------- | ---------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------- |
| `CreateTenant`              | `TenantCreated`              | `TenantAlreadyExistsRejection`                                                                                                                      |
| `UpdateTenant`              | `TenantUpdated`              | `TenantNotFoundRejection`, `TenantDisabledRejection`, `InsufficientPermissionsRejection`                                                            |
| `DisableTenant`             | `TenantDisabled`             | `TenantNotFoundRejection`, `TenantLifecycleStateAlreadySetRejection`                                                                                |
| `EnableTenant`              | `TenantEnabled`              | `TenantNotFoundRejection`, `TenantLifecycleStateAlreadySetRejection`                                                                                |
| `AddUserToTenant`           | `UserAddedToTenant`          | `TenantNotFoundRejection`, `TenantDisabledRejection`, `UserAlreadyInTenantRejection`, `RoleEscalationRejection`, `InsufficientPermissionsRejection` |
| `RemoveUserFromTenant`      | `UserRemovedFromTenant`      | `TenantNotFoundRejection`, `TenantDisabledRejection`, `UserNotInTenantRejection`, `InsufficientPermissionsRejection`                                |
| `ChangeUserRole`            | `UserRoleChanged`            | `TenantNotFoundRejection`, `TenantDisabledRejection`, `UserNotInTenantRejection`, `RoleEscalationRejection`, `InsufficientPermissionsRejection`     |
| `SetTenantConfiguration`    | `TenantConfigurationSet`     | `TenantNotFoundRejection`, `TenantDisabledRejection`, `ConfigurationLimitExceededRejection`, `InsufficientPermissionsRejection`                     |
| `RemoveTenantConfiguration` | `TenantConfigurationRemoved` | `TenantNotFoundRejection`, `TenantDisabledRejection`, `InsufficientPermissionsRejection`                                                            |
| `BootstrapGlobalAdmin`      | `GlobalAdministratorSet`     | `GlobalAdminAlreadyBootstrappedRejection`                                                                                                           |
| `SetGlobalAdministrator`    | `GlobalAdministratorSet`     | `GlobalAdministratorAlreadyExistsRejection`                                                                                                         |
| `RemoveGlobalAdministrator` | `GlobalAdministratorRemoved` | `GlobalAdministratorNotFoundRejection`, `LastGlobalAdministratorRejection`                                                                          |

## Idempotency

All events include `MessageId` and `SequenceNumber` in the event envelope. Consumers should use these fields for deduplication.

DAPR pub/sub guarantees **at-least-once delivery**, not exactly-once. Network retries, sidecar restarts, and redelivery can cause the same event to arrive multiple times. Without deduplication, this can lead to incorrect state.

For detailed idempotent processing patterns, including message-level deduplication and handler-level idempotency, see [Idempotent Event Processing](idempotent-event-processing.md).
