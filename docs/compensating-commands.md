[Back to README](../README.md)

# Compensating Commands

Compensating commands are deliberate follow-up commands that correct current tenant state while preserving the original event history. Submit them through the EventStore command gateway at `POST /api/v1/commands`, then verify the command outcome with `GET /api/v1/commands/status/{correlationId}`.

Compensation is not hidden undo, event deletion, event mutation, projection editing, direct state-store repair, rollback, or a shortcut around normal authorization. A corrective command is handled by the same aggregate rules as any other command. If it succeeds, a new event is appended. If it is rejected, EventStore records the rejected command outcome, but the tenant audit projection does not show a successful corrective audit event.

## Source-Backed Rules

These rules are anchored in the current Tenants contracts and aggregate behavior:

- `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs` defines the command success, rejection, and `NoOp` behavior.
- `src/Hexalith.Tenants.Server/Aggregates/TenantState.cs` applies stored events to current tenant state.
- `src/Hexalith.Tenants.Server/Projections/TenantAuditReadModel.cs` builds audit rows from successful tenant events.
- `src/Hexalith.Tenants.Contracts/Queries/TenantAuditEntry.cs` defines the audit query row shape.
- `docs/event-contract-reference.md` is the command, event, enum, and rejection reference.

Event-sourced tenant events are immutable facts. When a tenant access or configuration mistake is corrected, the original event remains in history and the correction appends a new event only if the corrective command succeeds. Tenant query and audit rows are projection-backed, so command status is the immediate proof of the command outcome; tenant audit query rows are proof that the successful corrective event has projected after catch-up. For projection timing, see [Cross-Aggregate Timing](cross-aggregate-timing.md).

## Worked Example: Wrong User Removed

Sofia is a trusted global administrator for the `acme-corp` tenant. She intended to remove `contractor-b`, but she submitted `RemoveUserFromTenant` for `contractor-a`.

### Mistake: RemoveUserFromTenant

```json
{
    "messageId": "01ARZ3NDEKTSV4RRFFQ69G5FAW",
    "tenant": "system",
    "domain": "tenants",
    "aggregateId": "acme-corp",
    "commandType": "RemoveUserFromTenant",
    "payload": {
        "TenantId": "acme-corp",
        "UserId": "contractor-a"
    }
}
```

If accepted, this produces `UserRemovedFromTenant` for `contractor-a`. That event is not removed or rewritten.

### Correction: AddUserToTenant With Explicit TenantRole

Sofia restores `contractor-a` by issuing a new `AddUserToTenant` command with the intended current role:

```json
{
    "messageId": "01ARZ3NDEKTSV4RRFFQ69G5FAX",
    "tenant": "system",
    "domain": "tenants",
    "aggregateId": "acme-corp",
    "commandType": "AddUserToTenant",
    "payload": {
        "TenantId": "acme-corp",
        "UserId": "contractor-a",
        "Role": "TenantContributor"
    }
}
```

If accepted, this produces `UserAddedToTenant` with `TenantRole.TenantContributor`.

The role is explicit for three reasons:

- `UserRemovedFromTenant` carries `TenantId` and `UserId`, but it does not carry the removed user's role.
- Earlier `UserAddedToTenant` and `UserRoleChanged` events can help the operator choose a role, but intervening business decisions can make an old role stale.
- The correction must state the intended current role. The system does not automatically restore historical roles without a new command.

If the original task still requires removing `contractor-b`, Sofia submits the intended removal separately:

```json
{
    "messageId": "01ARZ3NDEKTSV4RRFFQ69G5FAY",
    "tenant": "system",
    "domain": "tenants",
    "aggregateId": "acme-corp",
    "commandType": "RemoveUserFromTenant",
    "payload": {
        "TenantId": "acme-corp",
        "UserId": "contractor-b"
    }
}
```

The durable history now shows the mistake, the correction, and the intended removal as separate facts:

1. `UserRemovedFromTenant` for `contractor-a`.
2. `UserAddedToTenant` for `contractor-a` with `TenantContributor`.
3. `UserRemovedFromTenant` for `contractor-b`.

## Common Correction Scenarios

### Mistaken User Removal

Safe command path:

- Use `AddUserToTenant` with explicit `TenantRole.TenantOwner`, `TenantRole.TenantContributor`, or `TenantRole.TenantReader` to restore the wrongly removed user.
- Use `RemoveUserFromTenant` for the intended user only if that removal is still required.

Expected rejection cases:

- `TenantNotFoundRejection` if the tenant aggregate does not exist.
- `TenantDisabledRejection` if the tenant is disabled.
- `UserAlreadyInTenantRejection` if the restore target is already a member.
- `UserNotInTenantRejection` if the intended removal target is not a member.
- `RoleEscalationRejection` if the submitted role is not assignable, including `TenantRole.Unknown`.
- `InsufficientPermissionsRejection` if the actor is not a tenant owner or trusted global administrator.

### Wrong Role Assignment

Safe command path:

```json
{
    "messageId": "01ARZ3NDEKTSV4RRFFQ69G5FAZ",
    "tenant": "system",
    "domain": "tenants",
    "aggregateId": "acme-corp",
    "commandType": "ChangeUserRole",
    "payload": {
        "TenantId": "acme-corp",
        "UserId": "contractor-a",
        "NewRole": "TenantReader"
    }
}
```

Use `ChangeUserRole` with explicit `NewRole`. If `NewRole` equals the user's current role, the aggregate returns `NoOp`; no new correction event is produced.

Expected rejection or no-op cases:

- `TenantNotFoundRejection`, `TenantDisabledRejection`, `UserNotInTenantRejection`, `RoleEscalationRejection`, or `InsufficientPermissionsRejection`.
- `NoOp` for same-role requests.

### Configuration Mistake

Safe command path:

```json
{
    "messageId": "01ARZ3NDEKTSV4RRFFQ69G5FB0",
    "tenant": "system",
    "domain": "tenants",
    "aggregateId": "acme-corp",
    "commandType": "SetTenantConfiguration",
    "payload": {
        "TenantId": "acme-corp",
        "Key": "sample.access.mode",
        "Value": "read-only"
    }
}
```

Use `SetTenantConfiguration` to overwrite a key with the intended value. Use `RemoveTenantConfiguration` if the key should no longer exist:

```json
{
    "messageId": "01ARZ3NDEKTSV4RRFFQ69G5FB1",
    "tenant": "system",
    "domain": "tenants",
    "aggregateId": "acme-corp",
    "commandType": "RemoveTenantConfiguration",
    "payload": {
        "TenantId": "acme-corp",
        "Key": "sample.access.mode"
    }
}
```

Expected rejection or no-op cases:

- `TenantNotFoundRejection`, `TenantDisabledRejection`, `ConfigurationLimitExceededRejection`, `ConfigurationKeyNotFoundRejection`, or `InsufficientPermissionsRejection`.
- `NoOp` when `SetTenantConfiguration` submits the same key and same value already stored.

### Tenant Lifecycle Correction

Safe command path:

```json
{
    "messageId": "01ARZ3NDEKTSV4RRFFQ69G5FB2",
    "tenant": "system",
    "domain": "tenants",
    "aggregateId": "acme-corp",
    "commandType": "EnableTenant",
    "payload": {
        "TenantId": "acme-corp"
    }
}
```

Use `EnableTenant` after an accidental `DisableTenant`, or `DisableTenant` after accidental enablement. Both are trusted global administrator operations. If a correction requires a member or configuration command, enable the tenant first; most member and configuration commands reject disabled tenants with `TenantDisabledRejection`.

Expected rejection cases:

- `TenantNotFoundRejection`.
- `TenantLifecycleStateAlreadySetRejection` if the tenant is already in the requested lifecycle state.
- `InsufficientPermissionsRejection` if the actor is not a trusted global administrator.

Lifecycle duplicate-state rejections serialize `TenantStatus` values by enum name:

```json
{
    "TenantId": "acme-corp",
    "CurrentStatus": "Disabled",
    "RequestedStatus": "Disabled",
    "CommandName": "DisableTenant"
}
```

## Audit and Verification

Use two evidence sources:

1. EventStore command status proves the submitted command outcome. A successful correction reaches a success terminal status with the corrective event stored and published. A rejected correction reaches a rejected command outcome and names the rejection event.
2. Tenant audit query rows prove successful corrective events after projections catch up. `TenantAuditReadModel` creates rows for `UserAddedToTenant`, `UserRemovedFromTenant`, `UserRoleChanged`, `TenantConfigurationSet`, `TenantConfigurationRemoved`, `TenantDisabled`, and `TenantEnabled`.

Rejected compensating commands do not produce successful corrective audit events. Treat the rejection as feedback to choose a valid explicit command, role, value, lifecycle operation, or actor authority.

Keep correction examples support-safe. Do not capture or paste raw bearer tokens, decoded JWT payloads, secrets, real tenant or user data, full serialized event payload dumps, or stack traces in tickets, docs, or runbooks.

## Drift Checks

When command names, role names, lifecycle statuses, rejection behavior, or EventStore command-envelope fields change, update this guide and the related documentation tests in the same change. Re-check the command contracts, `TenantAggregate`, `TenantState`, `TenantAuditReadModel`, `TenantAuditEntry`, and [Event Contract Reference](event-contract-reference.md) before publishing new compensating-command snippets.

Related guides:

- [Quickstart Guide](quickstart.md)
- [Event Contract Reference](event-contract-reference.md)
- [Cross-Aggregate Timing](cross-aggregate-timing.md)
- [Sample Consuming Service Walkthrough](sample-consuming-service-walkthrough.md)
- ["Aha Moment" Demo](demo.md)
