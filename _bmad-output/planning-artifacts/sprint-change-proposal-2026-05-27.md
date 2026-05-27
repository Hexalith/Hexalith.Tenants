# Sprint Change Proposal — Fail-Safe Defaults & Consumer-Contract Hardening (TEN-1 … TEN-5)

- **Project:** Hexalith.Tenants
- **Date:** 2026-05-27
- **Author:** Jerome (via Correct Course workflow)
- **Trigger origin:** Parties consuming-team review, cross-referencing Tenants stories 11-2 / 11-3 / 11-4 (now folded into the restructured 9-epic plan)
- **Mode:** Incremental (each edit proposal individually approved)
- **Scope classification:** Direct Adjustment — Minor/Moderate. Route to Developer agent.
- **Security note:** TEN-1 and TEN-2 are **fail-open** defaults and are treated as security-adjacent.

---

## Section 1 — Issue Summary

A downstream consumer (Parties) review surfaced five issues in `Hexalith.Tenants` that all share one root theme: **fail-safe defaults and consumer-facing contracts were under-specified.** All five were reproduced against current `main` before this proposal was written.

| ID | Problem (verified) | File:line evidence |
|----|--------------------|--------------------|
| **TEN-1** | `TenantRole.TenantOwner` is the first enum member ⇒ ordinal `0`. A `UserAddedToTenant` payload with a missing `Role` deserializes to `0` = `TenantOwner` (**fail-open** privilege grant). Enums currently serialize as **integers** on the event wire (default STJ; `JsonStringEnumConverter` is wired only in `TenantsProjectionActor.cs:35`). | `Contracts/Enums/TenantRole.cs:4`; `Events/UserAddedToTenant.cs:5` |
| **TEN-2** | `TenantStatus.Active` is the first enum member ⇒ ordinal `0`. A snapshot / read-model / query payload missing `Status` defaults to `Active` (**fail-open**). | `Contracts/Enums/TenantStatus.cs:4`; `Client/Projections/TenantLocalState.cs:48` |
| **TEN-3** | Membership maps compare keys with `StringComparer.Ordinal` (and the default `[]` is ordinal). JWT `sub` and `eventstore:tenant` casing varies by IdP, so benign casing differences fail closed (`UnknownTenant` / `MissingMember`) with no documented convention. | `Client/Projections/TenantLocalState.cs:26,53`; mirrored at ~14 sites incl. `Server/Aggregates/TenantState.cs` |
| **TEN-4** | `InMemoryTenantProjection.Apply` silently drops unknown event types via its `default:` arm. No test catches a new success event being dropped, so the fake can drift from the real projection. (The finding's `ProjectFromTenantsAsync` does not exist in this repo; the real site is the `default:` arm reached via `ApplyEvents`.) | `Testing/Projections/InMemoryTenantProjection.cs:72-74` |
| **TEN-5** | `Hexalith.Tenants.Testing` public helpers return `EventStore.Contracts.Results.DomainResult`, coupling that type into consuming-service (Parties) tests. | `Testing/Fakes/InMemoryTenantService.cs`; `Testing/Helpers/TenantTestHelpers.cs` |

**Discovery context:** raised during Parties↔Tenants integration; not caught earlier because the fail-open defaults only manifest on malformed/partial payloads and the casing convention was never written down.

---

## Section 2 — Impact Analysis

### Epic impact
No epic is invalidated; no new epic is required. Each correction maps to an existing story as an added acceptance criterion.

| Finding | Story (epics.md) | Disposition |
|---------|------------------|-------------|
| TEN-1 | 3.1 — Add Users to a Tenant with Explicit Roles | AC appended |
| TEN-2 | 2.5 — Disable and Re-Enable Tenants | AC appended |
| TEN-3 | 7.3 — Validate Production Authentication and EventStore Tenant Claims | AC appended |
| TEN-4 | 6.3 — Add Production/Fake Conformance Tests | AC appended |
| TEN-5 | 6.1 — Provide In-Memory Tenant Test Fakes | AC appended (decision recorded) |

### Artifact conflicts
- **PRD:** no conflict. These harden existing FRs (FR6/FR8/FR10 for roles; FR3/FR4 for status; FR46/FR47 for fakes; FR56/FR57 + auth for claims) — no requirement changes.
- **Architecture (`architecture.md`):** add two decision records — (a) enums serialize by name with ordinal-0 fail-closed sentinel; (b) identifier comparison is Ordinal with canonical casing as a boundary contract. Plus the Tenants.Testing `DomainResult` decision record.
- **`docs/event-contract-reference.md`:** update `Role` / `Status` representation to string + sentinel note.
- **`docs/production-auth-claim-contract.md`:** new "Identifier Casing Contract" section.
- **UX:** none.
- **Golden fixtures:** none exist yet (pre-v1.0), so nothing to regenerate.

### Technical impact
- **RBAC hierarchy is safe under the enum reorder** — `MeetsMinimumRole` (`TenantAggregate.cs:192-198`) switches on enum **names**, not ordinals, and already default-denies unknown roles.
- **Required consequence of the reorder:** once `Unknown=0` is a *defined* value, the existing `!Enum.IsDefined(command.Role)` guards (`TenantAggregate.cs:77,174`) no longer reject it. They must become an allowlist (`IsAssignableRole`).
- **Wire change:** enum representation moves int→name and ordinals shift. Acceptable under the Phase-1 (pre-v1.0) policy; flag in CHANGELOG via a `!` commit.

---

## Section 3 — Recommended Approach

**Selected path: Option 1 — Direct Adjustment.** Effort **Low–Medium**, Risk **Low**.

- *Option 2 (Rollback):* not viable / unnecessary — nothing to roll back; these are forward hardening fixes.
- *Option 3 (MVP review):* not needed — MVP scope and goals are unchanged.

**Chosen remediation courses (all confirmed with the user):**
- **TEN-1 / TEN-2:** sentinel `Unknown=0` **and** serialize-by-name (`JsonStringEnumConverter<T>`), with an allowlist guard for assignable roles. Strongest defense-in-depth; a missing field → `Unknown` (default-denied) and an unrecognized name → `JsonException` (fail-closed).
- **TEN-3:** documented normalization contract — Ordinal everywhere, canonical casing owned by the IdP/operator (because OIDC `sub` is case-sensitive per spec; case-folding could merge distinct subjects). Test-protect the Ordinal choice.
- **TEN-4:** reflection-driven projection-conformance test in the Tenants suite; production silent-default behavior preserved (real-service parity) but drift is now caught.
- **TEN-5:** accept `DomainResult` as the intentional public surface; record the decision; no wrapper, no Parties fitness test.

---

## Section 4 — Detailed Change Proposals

### TEN-1 + TEN-2 — fail-open enum defaults

**Contracts — `Enums/TenantRole.cs`**
```diff
+using System.Text.Json.Serialization;
 namespace Hexalith.Tenants.Contracts.Enums;
+/// <summary>Tenant membership role. <c>Unknown</c> (ordinal 0) is a non-privileged sentinel:
+/// a missing/defaulted Role deserializes here and is rejected by MeetsMinimumRole.</summary>
+[JsonConverter(typeof(JsonStringEnumConverter<TenantRole>))]
 public enum TenantRole {
+    Unknown = 0,
     TenantOwner,
     TenantContributor,
     TenantReader,
 }
```

**Contracts — `Enums/TenantStatus.cs`**
```diff
+using System.Text.Json.Serialization;
 namespace Hexalith.Tenants.Contracts.Enums;
+[JsonConverter(typeof(JsonStringEnumConverter<TenantStatus>))]
 public enum TenantStatus {
+    Unknown = 0,
     Active,
     Disabled,
 }
```

**Server — `Aggregates/TenantAggregate.cs`** (required guard fix)
```diff
+    // Only the three real roles are assignable; Unknown (sentinel) and out-of-range are rejected.
+    private static bool IsAssignableRole(TenantRole role)
+        => role is TenantRole.TenantOwner or TenantRole.TenantContributor or TenantRole.TenantReader;
```
```diff
-            _ when !Enum.IsDefined(command.Role)        // AddUserToTenant (line 77)
+            _ when !IsAssignableRole(command.Role)
-            _ when !Enum.IsDefined(command.NewRole)     // ChangeUserRole (line 174)
+            _ when !IsAssignableRole(command.NewRole)
```
`MeetsMinimumRole` needs no change (default arm already fail-closes `Unknown`).

**Client — `Projections/TenantLocalState.cs:48`**
```diff
-    public TenantStatus Status { get; set; } = TenantStatus.Active;
+    public TenantStatus Status { get; set; } = TenantStatus.Unknown;
```

**Tests**
- `Contracts.Tests/EnumFailSafeTests`: `UserAddedToTenant` JSON without `role` → `Unknown`; `"role":"Bogus"` → `JsonException`; round-trip emits `"role":"TenantOwner"` (string).
- `Server.Tests/Aggregates/TenantAggregateTests`: `AddUserToTenant`/`ChangeUserRole` with `Unknown` → `RoleEscalationRejection`; member holding `Unknown` fails `IsAuthorized`.
- Verify `EventSerializationTests` enum value picker does not select `Unknown`.

**Docs / commit**
- `architecture.md`: decision record — enums by name, ordinal-0 fail-closed sentinel.
- `docs/event-contract-reference.md`: `Role`/`Status` as string + sentinel note.
- Commit `fix(contracts)!:` with a CHANGELOG note (pre-v1.0 wire change).

**Parties follow-up satisfied:** authorization no longer needs to assume a non-zero owner role; status-gating no longer compensates for a defaulted `Active`.

### TEN-3 — userId/tenantId casing

- **`docs/production-auth-claim-contract.md`** — new "Identifier Casing Contract" section: Ordinal/case-sensitive comparison throughout; rationale (OIDC `sub` is case-sensitive); IdP/operator obligation to emit canonically-cased `sub` and reference tenant IDs with consistent casing (convention: lowercase kebab-case); mismatch is fail-closed by design — resolve at the source, not by relaxing the comparer.
- **`architecture.md`** — mirror decision record.
- **Code** — XML-doc notes on `TenantLocalState.Members` and `TenantEventContext.TenantId` cross-referencing the contract ("do not switch to OrdinalIgnoreCase"). **No comparer changes.**
- **Test** — `Client.Tests`: `"User-1"` and `"user-1"` are distinct entries; differently-cased lookup misses (locks in the Ordinal decision).

**Parties follow-up satisfied:** with the convention published, Parties can drop its claims case-folding compensation.

### TEN-4 — silent unknown-event drop

- **New `Testing.Tests/Projections/InMemoryTenantProjectionConformanceTests.cs`** (`[Trait("Category","Conformance")]`), reflection-driven over `Contracts.Events`:
  - `TenantCreated` → apply to empty projection; assert `GetTenant` becomes non-null.
  - global-admin events → apply to empty projection; assert `GetGlobalAdministrators()` reflects it.
  - all other tenant-scoped events → `Should.Throw<InvalidOperationException>` (proves they reach `GetOrThrow`; an unwired event hits `default:`, does not throw, and fails the test).
  - guard assertion on the discovered event count, with a message telling the dev to wire the new event and update the count.
  - reuse the reflection instance builder from `EventSerializationTests` (extract to a shared `TestEventFactory`).
- **`InMemoryTenantProjection.cs:73`** — keep the silent default; update the comment to cross-reference the new conformance guard.

Conformance home stays in the Tenants suite, exactly as the finding requests; Parties needs no projection-conformance test.

### TEN-5 — Testing exposes `DomainResult` (accept + document)

- **`architecture.md`** — decision record "Tenants.Testing result type": returning `DomainResult` is intentional; it is the canonical EventStore outcome type, in-tier for `Hexalith.Tenants.Testing`, and reused by consumer tests without new coupling.
- **`Testing/Fakes/InMemoryTenantService.cs`** — class-level XML-doc `<remarks>` pointing at the decision record ("Do not wrap").
- **No** wrapper type; **no** Parties architecture-fitness restriction.

---

## Section 5 — Implementation Handoff

**Scope:** Direct Adjustment (Minor/Moderate). **Recipient:** Developer agent.

**Already applied by this workflow (planning artifacts):**
- ✅ Five acceptance criteria appended to `epics.md` Stories 3.1, 2.5, 7.3, 6.3, 6.1.
- ✅ This Sprint Change Proposal.

**Developer agent deliverables (code + docs, to ship atomically):**
1. Contracts enum changes (TEN-1/TEN-2) + `TenantAggregate` allowlist guard + `TenantLocalState` default.
2. Tests: `EnumFailSafeTests`, `TenantAggregateTests` additions, casing test, `InMemoryTenantProjectionConformanceTests` (+ shared `TestEventFactory`).
3. Docs: `architecture.md` (3 decision records), `docs/production-auth-claim-contract.md` (casing section), `docs/event-contract-reference.md` (enum representation).
4. XML-doc cross-references on `TenantLocalState.Members`, `TenantEventContext.TenantId`, `InMemoryTenantService`, and the `InMemoryTenantProjection` default-arm comment.

**Verification gates (per project context):**
- `dotnet build` (TreatWarningsAsErrors) + Tier 1 (`Contracts.Tests`, `Client.Tests`, `Testing.Tests`) green before commit.
- Conformance, naming-convention, and serialization round-trip tests must stay enabled.
- Commit messages: `fix(contracts)!:` for the wire-affecting enum change; `test:` / `docs:` for the rest, split per Conventional Commits.

**Success criteria:**
- A `UserAddedToTenant` payload with a missing/unrecognized `Role` never yields owner access (deserializes to `Unknown`, default-denied).
- A payload missing `Status` never reads as `Active`.
- The casing contract is published and test-protected; Parties can drop its compensations.
- Adding an unwired success event fails the Tenants projection-conformance test.
- The `Tenants.Testing` `DomainResult` surface is documented as intentional.

**`sprint-status.yaml`:** no structural change — no epics/stories added, removed, or renumbered (corrections are added ACs on existing backlog stories).

---

## Appendix — Change Navigation Checklist Results

| § | Item | Status |
|---|------|--------|
| 1.1 | Triggering origin documented (Parties review; stories 11-2/3/4) | ✅ Done |
| 1.2 | Core problem categorized (under-specified fail-safe defaults / contracts) | ✅ Done |
| 1.3 | Evidence gathered (all five reproduced with file:line) | ✅ Done |
| 2.1–2.5 | Epic impact (no epic invalidated; AC-level adjustments; no resequencing) | ✅ Done |
| 3.1 | PRD conflict | [N/A] none |
| 3.2 | Architecture impact (3 decision records) | ✅ Action-needed → handed off |
| 3.3 | UI/UX impact | [N/A] none |
| 3.4 | Other artifacts (event-contract-reference, claim-contract docs, tests, CHANGELOG) | ✅ Action-needed → handed off |
| 4.1 | Option 1 Direct Adjustment | ✅ Viable (selected) |
| 4.2 | Option 2 Rollback | Not viable / unnecessary |
| 4.3 | Option 3 MVP review | Not needed |
| 4.4 | Recommended path | ✅ Option 1 |
| 5.1–5.5 | Proposal components | ✅ Done |
| 6.4 | sprint-status.yaml update | [N/A] no structural change |
