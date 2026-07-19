# Focused FR-13 Authorization Consistency Review

- **Reviewed:** the approved 2026-07-19 sprint-change proposal; updated `prd.md`, `addendum.md`, `.memlog.md`, and reconciliation report; `TenantAggregate.cs`; and the already-applied FR13 / Story 3.1 passages in `epics.md`
- **Review date:** 2026-07-19
- **Scope:** the FR-13 tenant-creation authorization correction only

## Verdict

**CHANGES REQUIRED — 0 CRITICAL, 1 HIGH, 0 MEDIUM, 0 LOW.** The correction accurately narrows tenant creation to global administrators and preserves the UI-reflects/server-enforces boundary without expanding scope. However, every changed artifact names `GlobalAdminRequired` as the domain rejection even though the domain emits `InsufficientPermissionsRejection` and the UI exposes the safe code `InsufficientPermissions`. This contract mismatch must be corrected before the update can pass its reviewer gate.

## HIGH

### H1. `GlobalAdminRequired` is a helper method, not the domain rejection contract

The applied correction propagates an identifier from the approved proposal as though it were the exact rejection name:

- `prd.md:266` says the domain rejects an unauthorized create with `GlobalAdminRequired`.
- `addendum.md:63` records `GlobalAdminRequired` as the backend rejection in the aggregate-verified matrix.
- `epics.md:1307` repeats `GlobalAdminRequired` in Story 3.1's server-enforcement AC.
- `.memlog.md:28` records “GlobalAdminRequired behavior.”
- `reconcile-scp-2026-07-19.md:12,24` calls `GlobalAdminRequired` the rejection and declares the vocabulary exact.
- The source proposal itself introduces the same mistaken name at lines 16 and 118; faithful propagation does not make it domain-accurate.

The implementation proves a different contract:

- `TenantAggregate.cs:32-33` calls the private helper `GlobalAdminRequired(...)` when the trusted global-administrator marker is absent.
- `TenantAggregate.cs:229-238` shows that helper returning `new InsufficientPermissionsRejection(...)`.
- `InsufficientPermissionsRejection.cs:5-9` defines the emitted rejection event type.
- `TenantAggregateTests.cs:88-102` specifically verifies that unauthorized `CreateTenant` emits `InsufficientPermissionsRejection` with `CommandName == CreateTenant`.
- `TenantCommandGateway.cs:425-427` maps unauthorized/forbidden create submission to the support-safe UI code `InsufficientPermissions`; shared status mapping does the same at lines 640-642.

**Impact:** downstream implementation and acceptance tests could wait for or assert a nonexistent `GlobalAdminRequired` rejection, while the reconciliation report incorrectly certifies exact domain vocabulary. The safety policy remains correct, but its observable failure contract is false.

**Required correction:** describe the backend outcome as domain event `InsufficientPermissionsRejection` and, where product-facing shorthand is appropriate, safe code `InsufficientPermissions`; retain `missing permission` as the canonical UI availability reason. Update the PRD, addendum, and Story 3.1 passage, correct/regenerate reconciliation, and append a corrective memlog entry rather than rewriting the append-only history. The approved proposal may remain immutable if it is treated as the superseded change signal, but current artifacts must not present its helper-method name as the rejection contract.

## Verified clean

- **Authorization precision:** `prd.md:265`, UJ-6 at `prd.md:113-118`, `epics.md:45,218`, and Story 3.1 at `epics.md:1288-1311` consistently make tenant creation global-administrator-only while preserving “authorized operator” as the PRD's product-facing umbrella.
- **UI reflects / server enforces:** `prd.md:192`, FR-13, and Story 3.1 keep the UI fail-closed with `missing permission`; `TenantAggregate.cs:28-36,265-269` remains the authoritative enforcement boundary using a trusted server-populated extension.
- **Duplicate and success semantics:** `TenantAlreadyExists` rejection and projection-confirmed success remain unchanged and consistent across PRD, addendum, epics, and aggregate behavior.
- **Scope containment:** no new feature, endpoint, phase, story, journey step, authorization role, preview requirement, or implementation mechanism was introduced by the correction.

## Severity counts

| Severity | Count |
|---|---:|
| Critical | 0 |
| High | 1 |
| Medium | 0 |
| Low | 0 |

---

## Resolution Re-review — 2026-07-19

### Final gate verdict

**PASS — the original HIGH finding is resolved in the scoped PRD update artifacts. No in-scope residual findings remain.**

### Resolution evidence

- `prd.md:266` now distinguishes the observable contracts correctly: the domain emits `InsufficientPermissionsRejection`, the UI maps it to `InsufficientPermissions`, and action availability remains `missing permission`.
- `addendum.md:63` carries the same domain-event → UI-code → availability-reason mapping in the aggregate-verified rejection matrix.
- UJ-6 (`prd.md:113-118`) and FR-13 (`prd.md:264-266`) remain aligned on global-administrator-only creation; duplicate rejection and projection-confirmed success semantics are unchanged.
- `.memlog.md:30` appends an explicit override that supersedes the earlier helper-name shorthand without rewriting append-only history.
- `reconcile-scp-2026-07-19.md:6,12,17-25,34,47-52` records the proposal's vocabulary defect, the authoritative source correction, preserved authorization intent, and zero artifact gaps.
- No endpoint, feature, phase, journey step, role, preview, architecture, or implementation scope was added.

### Downstream source residual — non-gating for this PRD update

The immutable approved change signal (`sprint-change-proposal-2026-07-19.md:16,118`) and the already-user-modified downstream Story 3.1 AC (`epics.md:1307`) still use `GlobalAdminRequired` as shorthand. They are outside this PRD update's write scope and therefore do not reopen the PRD gate. The reconciliation and memlog now prevent that shorthand from governing the corrected PRD/addendum contract. The `epics.md` wording should be tracked for the next workflow authorized to modify downstream epic/story artifacts; the approved proposal can remain as historical input with its defect explicitly recorded.

### Final severity counts

| Severity | Open in scoped PRD artifacts |
|---|---:|
| Critical | 0 |
| High | 0 |
| Medium | 0 |
| Low | 0 |

Non-gating downstream source residuals: **1** (proposal + already-modified Story 3.1 share the same helper-name shorthand).
