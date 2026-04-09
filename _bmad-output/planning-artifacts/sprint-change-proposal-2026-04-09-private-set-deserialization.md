# Sprint Change Proposal — Private Set Deserialization Fix on All Models

**Date:** 2026-04-09
**Triggered by:** Story 16-5 (Tenant Management — Admin Web UI) via Hexalith.EventStore integration
**Scope Classification:** Minor
**Status:** Approved & Implemented
**Related:** Hexalith.EventStore sprint-change-proposal-2026-04-09-tenant-index-deserialization.md (umbrella)

---

## Section 1: Issue Summary

All state and projection model classes in `Hexalith.Tenants.Server` use `private set` on their properties. When these models are round-tripped through JSON serialization — either via DAPR's `GetStateAsync<T>()` or via the EventStore's `DomainServiceCurrentState` payload — `System.Text.Json` cannot populate `private set` properties. This causes:

1. **TenantIndexReadModel:** Tenant index loses all previously stored tenants on each projection update. Only the last created tenant is visible.
2. **TenantState:** Aggregate state cannot be rehydrated from serialized `DomainServiceCurrentState`, causing the domain service `/process` endpoint to crash with 500 for any command that requires existing state (DisableTenant, AddUserToTenant, etc.).
3. **TenantReadModel:** Per-tenant projection data cannot be deserialized when read by the projection actor.
4. **GlobalAdministratorReadModel:** Global admin set cannot be deserialized, breaking admin authorization checks.

### Verification

Confirmed with a .NET 10 serialization test:
```
public set    -> Tenants.Count = 2   ✓
private set   -> Tenants.Count = 0   ✗
```

---

## Section 2: Impact Analysis

| Area | Impact |
|------|--------|
| Hexalith.EventStore Admin UI | All tenant operations broken (list, disable, add user) |
| Hexalith.Tenants domain service | `/process` endpoint returns 500 for commands on existing aggregates |
| Other consumers | Any service reading these projections from DAPR state store gets empty data |

---

## Section 3: Changes Applied

### Change: `private set` → `set` on all model properties

**4 files changed:**

#### 1. `src/Hexalith.Tenants.Server/Projections/TenantIndexReadModel.cs`

```csharp
// BEFORE
public Dictionary<string, TenantIndexEntry> Tenants { get; private set; } = [];
public Dictionary<string, Dictionary<string, TenantRole>> UserTenants { get; private set; } = [];

// AFTER
public Dictionary<string, TenantIndexEntry> Tenants { get; set; } = [];
public Dictionary<string, Dictionary<string, TenantRole>> UserTenants { get; set; } = [];
```

#### 2. `src/Hexalith.Tenants.Server/Aggregates/TenantState.cs`

All 8 properties: `TenantId`, `Name`, `Description`, `Status`, `Users`, `HasMembershipHistory`, `Configuration`, `CreatedAt` — `private set` → `set`.

#### 3. `src/Hexalith.Tenants.Server/Projections/TenantReadModel.cs`

All 7 properties: `TenantId`, `Name`, `Description`, `Status`, `Members`, `Configuration`, `CreatedAt` — `private set` → `set`.

#### 4. `src/Hexalith.Tenants.Server/Projections/GlobalAdministratorReadModel.cs`

```csharp
// BEFORE
public HashSet<string> Administrators { get; private set; } = [];

// AFTER
public HashSet<string> Administrators { get; set; } = [];
```

### Design Note

`private set` was originally used to enforce encapsulation — state should only be modified via `Apply()` methods. However, `System.Text.Json`'s default behavior does not populate `private set` properties during deserialization. Alternatives considered:

| Option | Viable? | Why not chosen |
|--------|---------|----------------|
| `[JsonInclude]` attribute | Yes | Requires extra import, less obvious than `set` |
| `init` setter | No | `Apply()` methods need to modify properties after construction |
| `JsonObjectCreationHandling.Populate` | Yes | Requires global opt-in, harder to reason about |
| `public set` | **Chosen** | Simplest, most explicit, compatible with all serializers |

---

## Section 4: Validation

### Post-deployment
- Redis FLUSHALL required (existing state contains data serialized with the old model)
- Recreate tenants and verify full lifecycle

### Success Criteria
- [x] Multiple tenants persist in the index across projection updates
- [x] DisableTenant and EnableTenant commands succeed
- [x] AddUserToTenant commands succeed for all roles
- [x] Projection data correctly round-trips through DAPR state store
- [ ] All Tier 1 tests pass
