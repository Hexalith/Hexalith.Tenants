# Domain-Fidelity Review — Tenants Management UI PRD

**Scope:** PRD (`prd.md`) + Addendum (`addendum.md`) checked against the authoritative backend rules in `_bmad-output/project-context.md`, `CLAUDE.md`, and the actual source (`src/Hexalith.Tenants.Server/Aggregates/*`, `src/Hexalith.Tenants.Contracts/*`).

**Overall verdict:** The PRD is *largely* faithful and, in several places, deliberately *more* correct than its own source UI specs (notably the ULID issue). However there is **one critical contradiction** (last-global-administrator treated as a soft warning when the backend hard-rejects it) and **two high-severity misstatements** about NoOp semantics (re-adding a user and editing metadata are *not* NoOps — they are rejections / always-emit). Several medium/low omissions are worth closing before the command phases.

Strengths the PRD got *right* (do not "fix"):
- ID-scheme: PRD §4, §12 R-6, §16.12, addendum §D/§E correctly state tenant/user ids are caller-supplied strings, NOT ULIDs; only envelope `MessageId` may be a ULID. This contradicts (and supersedes) the source UI specs, which is the desired behavior. Confirmed against `TenantIdentity`, `CreateTenant(string TenantId,…)`, `AddUserToTenant(string TenantId, string UserId,…)`.
- Business failures = rejection events, not exceptions (PRD §4 "Rejection", CP-3); compensating commands never edit/delete events (CP-7, NFR-5, §13); command endpoint `POST /api/v1/commands` (addendum §C, §16.1) — all confirmed in source.
- Two aggregates / `global-administrators` as a single fixed-identity aggregate (PRD §4, FR-18): confirmed (`GlobalAdministratorsAggregate`, domain `global-administrators`).
- Empty-tenant bootstrap (`AddUserToTenant` skips owner-only RBAC when `HasMembershipHistory == false`): addendum §D matches source exactly.
- Last-*owner* removal allowed by design (no "≥1 owner" invariant): PRD CP-6 correct — `RemoveUserFromTenant` has no last-owner check.

---

## Finding 1 — Last-global-administrator is HARD-REJECTED by the backend, not a soft warning
**Severity: CRITICAL**

**PRD location:** §6 CP-6 ("The **last-owner** and **last-global-administrator** cases are warnings with extra friction, **never backend-prohibited**"); §7.7 FR-19 ("removing the last global administrator is **warned with elevated friction, not hard-blocked** (CP-6)"); §4 Glossary ("The **last global administrator** is a protected case" — ambiguous but read alongside CP-6 implies soft).

**Authoritative rule it conflicts with:** `GlobalAdministratorsAggregate.Handle(RemoveGlobalAdministrator,…)` returns a **hard rejection** when the count is 1:
```
_ when state.Administrators.Count == 1 => DomainResult.Rejection([new LastGlobalAdministratorRejection(...)])
```
`LastGlobalAdministratorRejection` is one of the 14 rejection events. The backend **will not** remove the last global administrator under any friction/override. This is the *opposite* of the last-*owner* case (`RemoveUserFromTenant` has no such guard and succeeds to zero owners). `project-context.md` lists `LastGlobalAdministrator` as a rejection and separately notes last-owner removal is allowed — the two cases are deliberately asymmetric.

**Why it matters:** CP-6 conflates the two cases and instructs downstream UX/architecture to build "elevated friction, not a hard block" for an operation the server **always rejects**. A UI built to CP-6 would let the user push through friction and then surface a backend rejection it told them wouldn't happen — directly violating the product's own "never report success/availability it can't back" trust thesis.

**Suggested fix:** Split CP-6 and FR-19. State explicitly: *last-owner removal* is allowed by design (elevated friction, never blocked) — keep as-is; *last-global-administrator removal* is **backend-prohibited** (`LastGlobalAdministratorRejection`) and the UI must present it as a **hard-blocked action with an Unavailable Action Reason** ("cannot remove the last global administrator"), not friction-then-submit. Update §4 Glossary to make "protected" mean hard-blocked for global-admin.

---

## Finding 2 — Re-adding an existing user is a REJECTION, not a NoOp "already applied"
**Severity: HIGH**

**PRD location:** §7.4 FR-10 ("same-state requests reflect 'already applied' (NoOp)"); §7.9 FR-24 / addendum §D ("Same-state requests return NoOp (no event) — surfaced as 'already applied'", stated generally and for the compensating "restore intended access" = `AddUserToTenant`).

**Authoritative rule it conflicts with:** `TenantAggregate.Handle(AddUserToTenant,…)`:
```
_ when state.Users.TryGetValue(command.UserId, out TenantRole existingRole)
    => DomainResult.Rejection([new UserAlreadyInTenantRejection(tenantId, command.UserId, existingRole)])
```
Adding a user who is already a member is a **hard rejection** (`UserAlreadyInTenantRejection`), **not** a NoOp. NoOp applies only to (a) `ChangeUserRole` to the *current* role and (b) `SetTenantConfiguration` with identical key+value — confirmed in source.

**Why it matters:** FR-10's acceptance criterion ("same-state add → already applied / NoOp") is untestable/wrong; an idempotent re-add will surface a rejection. This is also load-bearing for **compensating recovery** (FR-24 "restore intended access" via `AddUserToTenant`): if the user already exists, the correction is rejected, not a silent success — the UI must handle `UserAlreadyInTenant` as a distinct outcome.

**Suggested fix:** In FR-10, remove the "(NoOp)" for add; state that re-adding an existing member returns a `UserAlreadyInTenant` rejection shown as safe localized text (and, for a corrective add where the goal is satisfied, may be presented as "already applied" *in the UI* — but it is a rejection, not a domain NoOp). Narrow the addendum §D "Same-state requests return NoOp" statement to *role-unchanged* and *identical config* only, with an explicit note that add-existing and lifecycle-already-set are **rejections**.

---

## Finding 3 — Editing tenant metadata is NOT a NoOp on identical values, and its RBAC is Contributor-or-global-admin (not "owner")
**Severity: HIGH**

**PRD location:** §7.5 FR-14 ("no-op edits reflect 'already applied'"; "An authorized user can edit a tenant's metadata").

**Authoritative rule it conflicts with:** `TenantAggregate.Handle(UpdateTenant,…)` always emits `TenantUpdated` on the authorized path — there is **no same-value NoOp branch**:
```
_ => DomainResult.Success([new TenantUpdated(tenantId, command.Name, command.Description, …)])
```
Also, authorization is **`TenantContributor`-or-higher OR global admin** (`!IsAuthorized(state, …, TenantRole.TenantContributor)`), which is *looser* than owner — a Contributor can edit metadata.

**Why it matters:** (a) FR-14's "no-op edits reflect 'already applied'" is false — re-submitting identical metadata produces a new `TenantUpdated` event every time (relevant to audit noise and to the UI's "already applied" promise). (b) The PRD's general authorization framing ("tenant owner OR global admin") understates who can edit metadata; downstream action-availability logic must allow Contributors for FR-14.

**Suggested fix:** Remove the NoOp claim from FR-14 (metadata edit always emits an event; if "no-op edit" UX is desired it is a client-side diff suppression, not a domain NoOp — say so). Note FR-14's RBAC is Contributor-or-higher / global-admin, distinct from the owner-gated commands.

---

## Finding 4 — Disable/Enable already-in-state is a REJECTION (TenantLifecycleStateAlreadySet), not a NoOp
**Severity: MEDIUM**

**PRD location:** §7.5 FR-15 (doesn't claim NoOp explicitly — partial pass), but interacts with the PRD-wide NoOp framing (FR-14, addendum §D) and the §4 "NoOp" glossary applied "everywhere".

**Authoritative rule it conflicts with:** `DisableTenant` on an already-`Disabled` tenant and `EnableTenant` on an already-`Active` tenant both return `TenantLifecycleStateAlreadySetRejection` (a hard rejection), not NoOp. Confirmed in source. Also both commands are **global-admin-only** (`IsGlobalAdmin` gate; a tenant owner cannot disable/enable).

**Why it matters:** If the UI's lifecycle toggle treats "already disabled/enabled" as a benign NoOp ("already applied") per the global NoOp glossary, it will mis-handle the actual `TenantLifecycleStateAlreadySet` rejection. And FR-15 should state disable/enable is global-admin-only (a tenant owner sees it unavailable).

**Suggested fix:** In FR-15, state that re-issuing the current lifecycle state returns `TenantLifecycleStateAlreadySet` (rejection, shown as safe text), and that disable/enable authorization is **global-administrator-only**. Scope the §4 NoOp glossary to role-unchanged + identical-config; explicitly exclude lifecycle.

---

## Finding 5 — PRD omits that commands to a DISABLED tenant are rejected at the source
**Severity: MEDIUM**

**PRD location:** §4 Glossary "Tenant lifecycle status / disabled" and §7.5 FR-15 describe *disabled* only as "an eventually-consistent **availability signal**" (for consumers). No FR notes the *write-side* effect.

**Authoritative rule it conflicts with:** Every mutating tenant command (`UpdateTenant`, `AddUserToTenant`, `RemoveUserFromTenant`, `ChangeUserRole`, `SetTenantConfiguration`, `RemoveTenantConfiguration`) short-circuits with `TenantDisabledRejection` when `Status != Active`:
```
{ Status: not TenantStatus.Active } => DomainResult.Rejection([new TenantDisabledRejection(tenantId)])
```
So a disabled tenant is not merely an availability signal to downstream consumers — it **blocks member/role/config edits at the aggregate** (only `EnableTenant` is accepted).

**Why it matters:** The UI's action-availability (FR-9 reasons) and command flows (FR-10/11/12/16/17) should reflect that member/config commands on a disabled tenant will be rejected — a likely real operator scenario (triaging a disabled tenant). Omitting this means the UI may offer actions the backend will reject.

**Suggested fix:** Add a note to §4 (disabled status) and FR-9/§6 that, beyond the consumer-facing availability signal, **a disabled tenant rejects member/role/config commands** (`TenantDisabled`); only Enable is accepted. This is a distinct Unavailable-Action-Reason candidate (currently the PRD's reason categories don't include "tenant disabled").

---

## Finding 6 — Global-administrator grant/remove rejection vocabulary is missing from the PRD
**Severity: LOW**

**PRD location:** §7.7 FR-19 (grant/remove global admin) lists only the last-admin case.

**Authoritative rule:** `SetGlobalAdministrator` returns `GlobalAdministratorAlreadyExistsRejection` (granting an existing admin), and `RemoveGlobalAdministrator` returns `GlobalAdministratorNotFoundRejection` (removing a non-admin) and `LastGlobalAdministratorRejection`. Caller must themselves be a global admin (else `InsufficientPermissions`). None of these (except last-admin, see Finding 1) appear in FR-19.

**Why it matters:** Minor — but FR-19 consequences are "testable" per §7 and currently under-specify the rejection outcomes the UI must render safely. Note also: granting an already-existing global admin is a *rejection*, not a NoOp (parallels Finding 2).

**Suggested fix:** Add to FR-19 consequences: granting an existing admin → `GlobalAdministratorAlreadyExists` (rejection, not NoOp); removing a non-admin → `GlobalAdministratorNotFound`; caller must be a global administrator. Keep last-admin as hard-blocked per Finding 1.

---

## Finding 7 — "TenantId" is used as the audit Target fallback; PRD/addendum Target rule is slightly off
**Severity: LOW**

**PRD location:** addendum §D ("Target resolution rule is `userId` → `key` → `TenantId`").

**Authoritative rule:** `TenantAuditEntry` resolves the target by `userId` then `key`, **falling back to `TenantId`** — confirmed in source. The addendum is **correct** here. (Listed only to confirm it checks out; no change needed.) The PRD body never restates this, which is fine.

**Suggested fix:** None — addendum §D matches source. (Recorded for completeness.)

---

## Finding 8 — "NarrativePayload" is a PRD-coined name, not a backend type
**Severity: LOW (clarity)**

**PRD location:** §4 Glossary, FR-22, §10, addendum §D — "structured **NarrativePayload**".

**Authoritative rule:** No type named `NarrativePayload` exists in the contracts. The audit entry carries a structured `IReadOnlyDictionary<string,string>` (narrative/metadata) from which the receipt is assembled client-side. The addendum already states the receipt is "assembled client-side … never the raw event payload" and "no new backend receipt endpoint" — consistent with reality.

**Why it matters:** Negligible, but downstream engineers may search for a `NarrativePayload` type and not find it. The concept (client-side assembly from the existing structured narrative dictionary, never the raw payload) is correct.

**Suggested fix:** Add one line in addendum §D clarifying `NarrativePayload` is the PRD's name for the existing structured narrative metadata (an `IReadOnlyDictionary<string,string>` on the audit read model), not a distinct backend contract — so no one expects a new type/endpoint.

---

## Cross-cutting confirmations (no change needed)
- **Command endpoint:** `POST /api/v1/commands` + `GET /api/v1/commands/status/{correlationId}` confirmed in source/tests; PRD §16.1/addendum §C correctly flag the unversioned `/api/commands` only as an *alias to confirm against the gateway*, not the authoritative route.
- **Authorization enforced server-side, UI reflects only:** PRD CP-9, NFR-2, §13 match the aggregate-level RBAC + server-populated `actor:globalAdmin` extension (`CommandsController` strips client-provided reserved extensions). Accurate.
- **Domain-boundary policy:** PRD §11 boundary policy + addendum §F ("build missing components in FrontComposer, not Tenants") matches `CLAUDE.md` / `project-context.md`. Accurate.
- **Query routes** (`/api/tenants`, `/api/tenants/{id}`, `/api/tenants/{id}/users`, `/api/users/{userId}/tenants`, `/api/tenants/{id}/audit`) in addendum §C match `TenantsQueryController`. Accurate.
- **TenantStatus** is binary `Active`/`Disabled` (+ `Unknown` sentinel) and **TenantRole** = `TenantOwner`/`TenantContributor`/`TenantReader` (+ `Unknown`): PRD §4 (owner-level role; `Unknown` never a valid target) accurate.
- **AuditEventCategory** = `Access` | `Administrative`: PRD §4 / addendum §D accurate.

---

## Priority summary
| # | Severity | One-line |
|---|---|---|
| 1 | CRITICAL | Last-global-administrator is hard-rejected (`LastGlobalAdministratorRejection`), not soft-friction — CP-6/FR-19 wrong; asymmetric with last-owner. |
| 2 | HIGH | Re-adding an existing user → `UserAlreadyInTenant` rejection, NOT a NoOp (FR-10, addendum §D, affects FR-24 recovery). |
| 3 | HIGH | `UpdateTenant` never NoOps on identical values (always emits) and is Contributor-or-global-admin, not owner (FR-14). |
| 4 | MEDIUM | Disable/Enable already-in-state → `TenantLifecycleStateAlreadySet` rejection (not NoOp); both are global-admin-only (FR-15). |
| 5 | MEDIUM | Commands to a disabled tenant are rejected (`TenantDisabled`); PRD treats "disabled" only as a consumer availability signal. |
| 6 | LOW | FR-19 omits `GlobalAdministratorAlreadyExists`/`GlobalAdministratorNotFound`; grant-existing is a rejection, not NoOp. |
| 7 | LOW | Audit Target fallback to `TenantId` — addendum correct, recorded only. |
| 8 | LOW | `NarrativePayload` is a PRD-coined name, not a backend type — clarify. |
