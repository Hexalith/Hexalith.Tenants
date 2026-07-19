# Input Reconciliation — Sprint Change Proposal 2026-07-19

**Input:** `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-19.md`  
**Compared artifacts:** `prd.md`, `addendum.md`  
**Reconciliation date:** 2026-07-19  
**Verdict:** **SATISFIED WITH AUTHORITATIVE SOURCE CORRECTION** — the approved actor/authorization intent is fully preserved; one rejection-vocabulary defect in the input is deliberately not propagated.

## 1. Authorized input contract

The approved proposal authorizes one companion PRD erratum arising from Story 3.1 actor reconciliation:

- Tenant creation is domain-enforced as global-administrator-only (proposal lines 16, 106–126). The proposal calls the rejection `GlobalAdminRequired`, but source verification shows that name is only the private aggregate helper invoked by `CreateTenant` (`TenantAggregate.cs:33,229`); the helper emits `InsufficientPermissionsRejection` (`TenantAggregate.cs:234`).
- FR-13 must retain the “authorized operator” framing while adding the explicit clarification “domain-enforced as global-administrator-only” (proposal lines 120 and 124).
- The UI reflects a non-global-administrator caller as `missing permission`; if a request is dispatched anyway, the API/domain boundary rejects it and the UI surfaces safe localized rejection text (proposal lines 112–118).
- The erratum is documentation-only. It does not alter goals, MVP, phases, sequencing, architecture, UX, story identifiers, or the already-applied `epics.md` edits (proposal lines 24–33, 156–170).

### Authoritative source correction

The proposal's `GlobalAdminRequired` token is a source-vocabulary defect, not an approved requirement to expose that private helper name:

- Domain event: `InsufficientPermissionsRejection` (`src/Hexalith.Tenants.Contracts/Events/Rejections/InsufficientPermissionsRejection.cs:5`; emitted at `TenantAggregate.cs:234`).
- UI rejection mapping: `InsufficientPermissions` with safe authorization text (`TenantCommandGateway.cs:427`; generic normalization also at lines 640–641).
- Product-facing availability reason remains the canonical `missing permission` category.

The updated PRD/addendum therefore follow code truth while retaining every substantive element of the approved change: global-administrator-only creation, UI-reflects/server-enforces behavior, support-safe rejection handling, and no scope or phase expansion.

## 2. Reconciliation evidence

| Check | Updated artifact evidence | Result |
|---|---|---|
| Qualitative actor wording | `prd.md:265` uses “An authorized operator (**domain-enforced as global-administrator-only**)”, preserving the proposal's requested operator framing and adding its exact enforcement clarification. | Satisfied |
| Journey actor consistency | `prd.md:113–116` keeps Elena's onboarding journey intact and narrows its entry state to “authenticated as a global administrator” before tenant creation. | Satisfied |
| UI reflects / server enforces | `prd.md:266` makes the action unavailable with `missing permission`, then states that a dispatched request is domain-rejected. Existing CP-9/NFR-2 continue to establish the UI-reflects/server-enforces boundary. | Satisfied |
| Rejection vocabulary | `prd.md:266` and `addendum.md:63` correctly use domain `InsufficientPermissionsRejection` and UI mapping `InsufficientPermissions`, overriding the proposal's erroneous private-helper token. `prd.md:266` and `addendum.md:64` retain exact `TenantAlreadyExists`. | Satisfied with authoritative correction |
| Support-safe behavior | `prd.md:266` and `addendum.md:63` require safe localized rejection text when an unauthorized request is dispatched. | Satisfied |
| Existing success semantics | `prd.md:266` retains projection-confirmed success; no optimistic-success behavior was introduced. | Satisfied |
| Technical-how placement | `addendum.md:63–64` records the RBAC rejection separately from the existing-id rejection in the aggregate-verified rejection matrix. | Satisfied |
| Scope containment | The updated text adds only the approved FR-13 actor/RBAC clarification and its journey/mechanism consistency consequences. It introduces no FR, endpoint, component, story, phase, architecture decision, UX behavior, or backend change beyond the approved contract. | Satisfied |

## 3. Qualitative preservation scan

- UJ-6 remains a complete onboarding journey: create tenant, add first owner, set initial configuration, and confirm each step. The update narrows only who may perform the creation step.
- “Authorized operator” remains the product-facing umbrella term in FR-13, as the proposal explicitly instructed; the parenthetical prevents that umbrella wording from broadening domain authorization.
- General dual-audience and owner-self-service language remains intact. Nothing implies that tenant owners or non-global platform operators can create tenants.
- The update does not promote tenant creation into a high-impact/Consequence Preview flow, change Phase 2b placement, or borrow FR-15/FR-19 governance requirements.

## 4. Gaps and disposition

- **Input defect, corrected:** the proposal incorrectly presents private helper `GlobalAdminRequired` as the emitted rejection name. The artifacts correctly use `InsufficientPermissionsRejection` → `InsufficientPermissions`.
- **Artifact gaps:** none.

The proposal's substantive authorization intent remains fully satisfied, with no accidental scope expansion.
