# Sprint Change Proposal — Collapse Duplicate UI Command-Request DTOs

- **Date:** 2026-06-25
- **Author:** Administrator (via Correct Course workflow)
- **Status:** Approved 2026-06-25 (Administrator) — implemented & verified; uncommitted, pending Developer handoff
- **Change scope classification:** Minor (code-quality refactor; no requirement, scope, or contract change)
- **Triggering context:** Code review of `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs`

---

## Section 1 — Issue Summary

While reviewing `TenantCreateCommandModels.cs`, the UI request records were found to be **field-for-field duplicates** of the domain command records in `Hexalith.Tenants.Contracts.Commands`. The `TenantCommandGateway` mapped each UI record onto its twin with a redundant 1:1 construction (e.g. `var command = new CreateTenant(request.TenantId, request.Name, request.Description)`).

Nine UI request records duplicated a contract command with identical positional fields:

| UI record (deleted) | Contract command (reused) |
|---|---|
| `CreateTenantCommandRequest(TenantId, Name, Description?)` | `CreateTenant(TenantId, Name, Description?)` |
| `AddUserToTenantCommandRequest(TenantId, UserId, Role)` | `AddUserToTenant(TenantId, UserId, Role)` |
| `ChangeUserRoleCommandRequest(TenantId, UserId, NewRole)` | `ChangeUserRole(TenantId, UserId, NewRole)` |
| `RemoveUserFromTenantCommandRequest(TenantId, UserId)` | `RemoveUserFromTenant(TenantId, UserId)` |
| `UpdateTenantCommandRequest(TenantId, Name, Description?)` | `UpdateTenant(TenantId, Name, Description?)` |
| `SetTenantConfigurationCommandRequest(TenantId, Key, Value)` | `SetTenantConfiguration(TenantId, Key, Value)` |
| `RemoveTenantConfigurationCommandRequest(TenantId, Key)` | `RemoveTenantConfiguration(TenantId, Key)` |
| `SetGlobalAdministratorCommandRequest(UserId)` | `SetGlobalAdministrator(UserId)` |
| `RemoveGlobalAdministratorCommandRequest(UserId)` | `RemoveGlobalAdministrator(UserId)` |

**Evidence the duplication bought nothing:**
- The UI project already references `Hexalith.Tenants.Contracts` (enums, queries) and the gateway already imported `Hexalith.Tenants.Contracts.Commands` — so the records gave no decoupling.
- `project-context.md` line 45 documents the contract shape with the literal example `public record AddUserToTenant(string TenantId, string UserId, TenantRole Role);`.
- `CLAUDE.md` **Domain Implementation Boundary** explicitly forbids carrying boilerplate that duplicates shared/contract types.
- Story `2-2` task line already constructed `new AddUserToTenant(tenantId, userId, role)` directly for the command payload — the UI wrapper was redundant from the start.

**Retained on purpose:** `TenantLifecycleCommandRequest(TenantId, Operation)` — the one genuine UI-owned composite; the gateway resolves its `TenantLifecycleOperation` to either `EnableTenant` or `DisableTenant`, so it is *not* a 1:1 clone.

---

## Section 2 — Impact Analysis

| Artifact | Impact | Detail |
|---|---|---|
| **PRD** (`prds/`) | **None** | No reference to the DTOs; goals/requirements/MVP unaffected. |
| **Epics** (`epics.md`) | **None** | No reference to the DTOs. |
| **Architecture** (`architecture.md`, `architecture/`) | **None** | The UI-BFF server-side gateway design is unchanged — it still translates UI intents into EventStore command envelopes; only a redundant intermediate DTO layer was removed. No component, pattern, contract, or data-model change. |
| **UX Designs** (`ux-designs/`) | **None** | No user flow, screen, or interaction change. |
| **Implementation-artifacts (story docs)** | **9 files, 15 references** | Stale type names in `done` stories — see Section 4. |
| **Code (`src` + `tests`)** | **Implemented** | 43 files, +301/−316; 9 records deleted (2 as standalone files). |
| **CI / build / coverage** | **None (verified)** | No new analyzer, package, or coverage-gate surface. |

**Epic impact:** Epics 1/2/4/5 `done`, Epic 3 `in-progress` (all its stories `done`). No epic needs modification, addition, removal, resequencing, or re-prioritization. No in-flight story depends on the removed DTOs.

---

## Section 3 — Recommended Approach

**Selected path: Option 1 — Direct Adjustment (Hybrid: implement + retroactive doc sync).**

This is a pure internal refactor with no behavioural change, so neither rollback (Option 2) nor MVP review (Option 3) applies.

- **Option 1 — Direct Adjustment:** Viable. Effort **Low**, Risk **Low**. Compiler-guided type substitution; behaviour preserved (records keep value equality, so snapshot `with`/matching logic is identical).
- **Option 2 — Rollback:** Not viable / unnecessary. Nothing to revert; the change *removes* duplication rather than introducing risk.
- **Option 3 — MVP Review:** N/A. MVP scope and goals are untouched.

**Rationale:** removes ~270 lines of duplicate ceremony, aligns the UI with the documented contract pattern and the CLAUDE.md anti-duplication boundary, and improves maintainability (one source of truth for command shape) at negligible risk.

---

## Section 4 — Detailed Change Proposals

### 4a. Code (implemented)

**Deleted** 9 duplicate request records:
- 7 from `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs` (replaced with an explanatory comment).
- 2 standalone files: `SetGlobalAdministratorCommandRequest.cs`, `RemoveGlobalAdministratorCommandRequest.cs`.

**Rewired** to the contract commands:
- `ITenantCommandGateway`, `TenantCommandGateway`, `UnavailableTenantCommandGateway` — parameter types are now the contract records; the 9 collapsed gateway methods serialize `request` directly (the redundant `var command = new …(request.…)` lines were removed). The two lifecycle methods keep their `Enable/DisableTenant` mapping.
- Snapshot `Intent` fields (`Tenant*CommandSnapshot`, `GlobalAdministrator*CommandSnapshot`) retyped to the contract records.
- `Components/_Imports.razor` + affected test files import `Hexalith.Tenants.Contracts.Commands`.

Example (gateway, before → after):
```csharp
// OLD
public async Task<…> CreateTenantAsync(CreateTenantCommandRequest request, …) {
    var command = new CreateTenant(request.TenantId, request.Name, request.Description);
    var submit = new SubmitCommandRequest(…, JsonSerializer.SerializeToElement(command));

// NEW
public async Task<…> CreateTenantAsync(CreateTenant request, …) {
    var submit = new SubmitCommandRequest(…, JsonSerializer.SerializeToElement(request));
```

### 4b. Story documentation (implemented — literal type-name replacement)

The old type names were replaced in place across **9 `done` story docs**; the two GA stories also had their now-deleted per-type file entries struck through with a pointer to this proposal:

- `2-2`, `2-3`, `2-4`, `2-5` (Epic 2) — member/metadata command intent types.
- `3-3`, `3-4` (Epic 3) — configuration command intent types.
- `4-3`, `4-4` (Epic 4) — global-administrator command intent types (+ struck file entries).
- `5-6` (Epic 5) — correction-flow command intent types.

`SubmitCommandRequest` references (the EventStore envelope) were correctly left untouched.

---

## Section 5 — Implementation Handoff

- **Scope:** **Minor** — implemented directly; no backlog reorganization or replan required.
- **Status:** Complete and verified.
  - Release build with `-warnaserror`: **0 warnings, 0 errors**.
  - `Hexalith.Tenants.UI.Tests`: **761 passed, 0 failed**.
- **Handoff recipient:** Developer agent — commit on a `refactor/…` branch (Conventional Commits: `refactor` → no version bump, avoids a false NuGet publish of the 5 packages). Per CLAUDE.md, no direct commit to `main`.
- **Success criteria:** build clean under `-warnaserror`, full UI suite green, no remaining `*CommandRequest` duplicate type (only `TenantLifecycleCommandRequest` retained), story docs reference current type names.

### Optional follow-up (not in this change)
None outstanding — the global-administrator pair, originally flagged as a follow-up, was folded into this proposal at the user's direction.
