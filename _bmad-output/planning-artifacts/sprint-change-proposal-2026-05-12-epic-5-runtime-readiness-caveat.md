# Sprint Change Proposal: Epic 5 Runtime Readiness Caveat

Date: 2026-05-12
Project: Hexalith.Tenants
Prepared for: Jerome
Mode: Batch
Approval status: Existing corrective action already approved in SCP-2026-05-04; this note confirms no new scope.

## 1. Issue Summary

Epic 5, "Tenant Discovery & Query," is story-complete: Stories 5.1, 5.2, and 5.3 are all marked done, and the Epic 5 retrospective is complete. The retro also calls out a release-readiness caveat plainly: protected `/api/tenants/*` runtime readiness is blocked until `post-epic-5-r5a1-tenants-jwt-auth-wiring` is implemented and verified.

The blocker is not new. It was discovered during MCP-observed end-to-end testing on 2026-05-04 and captured in `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-04-tenants-defect-carry-forward.md`. The existing defect story remains ready for development in `_bmad-output/implementation-artifacts/sprint-status.yaml`.

Core problem statement: Epic 5 can be treated as implementation-complete, but it must not be used as evidence of protected HTTP runtime readiness until the JWT authentication pipeline wiring story is complete.

## 2. Checklist Findings

| Checklist Item | Status | Finding |
| --- | --- | --- |
| 1.1 Triggering story identified | Done | Trigger is the Epic 5 retrospective, tied to Story 5.3 and carry-forward story `post-epic-5-r5a1-tenants-jwt-auth-wiring`. |
| 1.2 Core problem defined | Done | Runtime host authentication wiring is missing for `[Authorize]` tenant query routes. |
| 1.3 Evidence gathered | Done | Evidence is in SCP-2026-05-04, the post-Epic-5 story, sprint status, and Epic 5 retro. |
| 2.1 Current epic viability | Done | Epic 5 remains story-complete. Runtime readiness is conditional. |
| 2.2 Epic-level changes | Done | No new epic changes required; existing carry-forward story is sufficient. |
| 2.3 Remaining epics reviewed | Done | Epic 6 can proceed, but docs must state in-memory parity does not prove ASP.NET host auth wiring. |
| 2.4 Future epic invalidation | Done | No future epic is invalidated. |
| 2.5 Epic order or priority | Action-needed | Resolve R5-A1 before claiming protected query endpoint release readiness. |
| 3.1 PRD conflicts | Done | No PRD change; FR25-FR30 remain valid. |
| 3.2 Architecture conflicts | Done | Architecture already requires JWT validation at the Hexalith.Tenants entry point. |
| 3.3 UI/UX conflicts | N/A | No current Admin UI impact. |
| 3.4 Other artifacts | Done | Sprint status and defect story already track the blocker. Epic 5 retro now reinforces it. |
| 4.1 Direct adjustment | Viable | Use the existing post-Epic-5 defect story. Effort remains low to medium; risk is critical because all protected query routes are affected. |
| 4.2 Rollback | Not viable | Query implementation is useful and covered; rollback would not fix host middleware wiring. |
| 4.3 MVP review | Not viable | MVP scope remains valid; audit completeness is separately deferred. |
| 4.4 Recommended path | Done | Direct Adjustment using existing story R5-A1. |
| 5.1-5.5 Proposal components | Done | This document confirms the existing approved route. |
| 6.1-6.5 Final review/handoff | Action-needed | Development and Tier 2 verification still required for R5-A1. |

## 3. Impact Analysis

### Epic Impact

Epic 5 remains done from a story implementation perspective. Its release-readiness language must stay conditional: query contracts, projections, controllers, and actor authorization are complete, but protected HTTP execution is not release-ready while the host lacks JWT authentication registration and `UseAuthentication()` / `UseAuthorization()` middleware ordering.

Epic 6 remains viable. Its documentation should keep the boundary explicit: in-memory and conformance tests prove domain/projection parity, not host authentication pipeline readiness.

### Story Impact

No new story is required.

Existing story to execute:

- `_bmad-output/implementation-artifacts/post-epic-5-r5a1-tenants-jwt-auth-wiring.md`

Success criteria remain:

- Unauthenticated `GET /api/tenants` returns 401 instead of 500.
- Authenticated admin request to `GET /api/tenants` returns 200 when the query path is otherwise healthy.
- Authenticated request to unknown tenant detail returns 404 instead of host-level auth failure.
- Existing Tier 1 and Tier 2 coverage remains green.

### Artifact Impact

PRD: No change.

Architecture: No change. Existing architecture already documents JWT validation at the service entry point and query authorization through the EventStore/MediatR pipeline plus query-side RBAC.

Sprint status: No change. `post-epic-5-r5a1-tenants-jwt-auth-wiring` is already `ready-for-dev`.

Retrospective: Already updated. It correctly states that Epic 5 is story-complete but not release-clean for protected HTTP query routes.

## 4. Recommended Approach

Recommended path: Direct Adjustment.

Do not reopen Epic 5 and do not add a duplicate story. Keep Epic 5 marked done, keep the retrospective complete, and route the existing R5-A1 carry-forward story to development before using protected `/api/tenants/*` as release evidence.

Rationale:

- The corrective action was already approved on 2026-05-04.
- The sprint tracker already contains the carry-forward story.
- The retro now makes the readiness caveat visible enough for planning and handoff.
- Creating another story would split ownership of the same defect.

Change scope classification: Minor.

## 5. Detailed Change Proposals

### Proposal A: Preserve Existing Sprint Status

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

OLD and NEW remain:

```yaml
  # Post-Epic-5 Defect Carry-Forward (SCP-2026-05-04 §A)
  # R5-A1: Tenants Program.cs missing AddJwtAuthentication / UseAuthentication wiring;
  # all /api/tenants/* requests returning 500.
  post-epic-5-r5a1-tenants-jwt-auth-wiring: ready-for-dev
```

Rationale: The status is already correct and should not be duplicated.

### Proposal B: Treat Epic 5 Readiness As Conditional

Artifact: Epic 5 handoff/status language

OLD:

```markdown
Epic 5 is complete.
```

NEW:

```markdown
Epic 5 is story-complete. Protected `/api/tenants/*` runtime readiness remains blocked until `post-epic-5-r5a1-tenants-jwt-auth-wiring` is implemented and verified.
```

Rationale: This keeps implementation status and release readiness separate.

## 6. Implementation Handoff

Scope: Minor.

Routed to: Developer agent for `post-epic-5-r5a1-tenants-jwt-auth-wiring`, with QA verification on the protected tenant query HTTP matrix.

Responsibilities:

- Developer: add JWT Bearer authentication registration and middleware ordering in `Program.cs`.
- QA/Test Architect: add or run Tier 2 authorization coverage for `/api/tenants`, `/api/tenants/{id}`, and `/api/tenants/{id}/users`.
- Product Owner/Scrum Master: keep the story visible as a release blocker until verified.

Success criteria:

- Protected tenant query routes return 401/403/404/200 as appropriate, not 500.
- Story `post-epic-5-r5a1-tenants-jwt-auth-wiring` moves from `ready-for-dev` to `done`.
- Epic 5 can then be cited as runtime-ready for protected query endpoints.

## 7. Verification

Tests were not run for this proposal. This was documentation/status-only and did not change source code or sprint tracker state.

