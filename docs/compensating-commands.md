[Back to README](../README.md)

# Compensating Commands

How to correct mistakes in an event-sourced system where events are immutable.

## What Are Compensating Commands?

Compensating commands are commands that undo or correct a previous operation by issuing a **new command** that moves state to the desired outcome. They do not erase history — they add to it.

## Why There Is No "Undo"

In event sourcing, events are **immutable facts**. Once `UserRemovedFromTenant` is stored, it cannot be deleted, modified, or rolled back. The event stream is an append-only log.

Corrections are always **new events** that represent the corrective action. The original event remains in the stream as a permanent record of what happened.

## Worked Example: Removing the Wrong User

Sofia is a GlobalAdministrator managing the `acme-corp` tenant. She needs to remove a contractor (`jdoe-consulting`) but accidentally removes the wrong user (`jdoe-contractor`).

### Step 1: The Mistake

Sofia removes `jdoe-contractor` from `acme-corp`:

```json
{
    "messageId": "01JNQV7K8M0001-remove-wrong",
    "tenant": "system",
    "domain": "tenants",
    "aggregateId": "acme-corp",
    "commandType": "RemoveUserFromTenant",
    "payload": {
        "TenantId": "acme-corp",
        "UserId": "jdoe-contractor"
    }
}
```

This produces `UserRemovedFromTenant` — the user is now removed from the tenant.

### Step 2: Sofia Realizes the Mistake

She meant to remove `jdoe-consulting`, not `jdoe-contractor`. The `UserRemovedFromTenant` event is already persisted and cannot be undone.

### Step 3: Compensate — Re-add the Correct User

Sofia issues `AddUserToTenant` to restore `jdoe-contractor`:

```json
{
    "messageId": "01JNQV8R4N0002-compensate",
    "tenant": "system",
    "domain": "tenants",
    "aggregateId": "acme-corp",
    "commandType": "AddUserToTenant",
    "payload": {
        "TenantId": "acme-corp",
        "UserId": "jdoe-contractor",
        "Role": 1
    }
}
```

This produces `UserAddedToTenant`, restoring the user with the `TenantContributor` role (value `1`).

### Step 4: Remove the Intended User

Sofia now removes the correct user:

```json
{
    "messageId": "01JNQV9F7P0003-remove-correct",
    "tenant": "system",
    "domain": "tenants",
    "aggregateId": "acme-corp",
    "commandType": "RemoveUserFromTenant",
    "payload": {
        "TenantId": "acme-corp",
        "UserId": "jdoe-consulting"
    }
}
```

This produces `UserRemovedFromTenant` for the intended user.

## Why the Role Must Be Explicitly Specified

In Step 3, Sofia must explicitly specify `"Role": 1` (TenantContributor) in the compensating `AddUserToTenant` command. The system does **not** auto-restore the previous role. Here's why:

1. **`UserRemovedFromTenant` does not carry role information.** The removal event records only which user was removed, not what role they had. The previous role exists only in earlier events (`UserAddedToTenant` or the last `UserRoleChanged`).

2. **Auto-restore could assign a stale role.** If the user's role changed between when they were originally added and when they were removed, auto-restoring the "previous" role could assign a role that no longer reflects business intent.

3. **The decision must be explicit.** The human (or calling service) must decide which role to assign based on **current business context**, not historical state. The event history provides the information needed to make this decision, but the decision itself is deliberate.

## The Audit Trail

The event stream after this correction contains three events in order:

1. `UserRemovedFromTenant` — jdoe-contractor removed (the mistake)
2. `UserAddedToTenant` — jdoe-contractor re-added with TenantContributor role (the correction)
3. `UserRemovedFromTenant` — jdoe-consulting removed (the intended action)

Each event records the timestamp and the actor (`userId`) who performed it. The complete sequence is preserved — the mistake, the correction, when each happened, and who performed each action.

This is an advantage of event sourcing over CRUD: in a CRUD system, corrections overwrite state and the history of the mistake is lost. In event sourcing, the full audit trail is permanent and queryable.
