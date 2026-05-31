---
workflow: bmad-correct-course
project: Tenants
date: 2026-05-31
status: approved
mode: batch
change_scope: moderate
trigger: implementation-readiness-assessment
source_report: _bmad-output/planning-artifacts/implementation-readiness-report-2026-05-31.md
source_artifacts:
  - _bmad-output/planning-artifacts/prd.md
  - _bmad-output/planning-artifacts/architecture.md
  - _bmad-output/planning-artifacts/epics.md
  - _bmad-output/planning-artifacts/ux-design-specification.md
  - _bmad-output/project-context.md
approval:
  approver: Jerome
  decision: approved
  approved_at: 2026-05-31
applied_artifacts:
  - _bmad-output/planning-artifacts/epics.md
  - _bmad-output/implementation-artifacts/sprint-status.yaml
---

# Sprint Change Proposal: Implementation Readiness Corrections

## 1. Issue Summary

The 2026-05-31 implementation readiness assessment completed with status `NEEDS WORK`.
The assessment found complete PRD coverage and sound UX/architecture alignment, but identified
execution-shaping problems that block broad Developer-agent handoff.

The primary issue is not missing scope. It is that several stories are currently shaped in a way
that can lead to unsafe implementation order, oversized handoff, or routing planning work as
product delivery.

Evidence from the assessment:

- Epic 5 has a forward dependency: endpoint stories 5.1-5.4 are listed before projection write safety and query authorization work, even though those safety concerns are prerequisites for shippable query endpoints.
- Story 5.6 explicitly says it must be split before Developer-agent handoff.
- Story 7.6 explicitly says it must be split before Developer-agent handoff.
- Epic 9 explicitly says it is readiness/planning-only and not shippable Admin UI implementation.
- Story 7.4 telemetry evidence, Story 7.5 startup reconstruction evidence, NFR11, and NFR13 need validation-lane classification before handoff.

## 2. Impact Analysis

### Epic Impact

| Epic | Impact | Required Action |
| --- | --- | --- |
| Epic 1 | Minor quality watch only | Preserve developer-facing acceptance evidence when creating story files. |
| Epic 5 | Critical sequencing correction | Resequence and split enabling projection, authorization, and pagination stories before endpoint delivery. |
| Epic 7 | Major handoff correction | Split Story 7.6 and classify telemetry/startup benchmark evidence lanes. |
| Epic 9 | Routing guard | Keep as planning-only until Phase 2 implementation stories are created. |

Epics 2, 3, 4, 6, and 8 remain implementation-ready according to the assessment.

### Story Impact

Affected current stories:

- `5.1-query-a-paginated-tenant-list`
- `5.2-query-tenant-details-and-tenant-users`
- `5.3-query-the-tenants-a-user-belongs-to`
- `5.4-query-tenant-access-audit-history`
- `5.5-provide-safe-cursor-based-pagination-for-query-endpoints`
- `5.6-persist-tenant-projections-without-silent-write-loss`
- `5.7-enforce-query-side-authorization-and-isolation`
- `7.4-expose-tenant-command-and-event-metrics-with-opentelemetry`
- `7.5-prove-stateless-operation-health-and-startup-reconstruction`
- `7.6-validate-deployment-readiness-with-smoke-tests-and-recovery-checks`
- Epic 9 stories `9.1` through `9.7`

### Artifact Conflicts

PRD:

- No MVP scope change is required.
- Phase 1 remains backend/package/documentation.
- Phase 2 Admin UI remains post-MVP unless separately promoted by a future scope decision.

Architecture:

- No architecture reversal is required.
- The architecture already supports projection safety, query-side authorization, and Phase 2 UI boundaries.
- Handoff must preserve the architecture rule that query controllers stay thin and authorization/filtering belongs in projection/query handling.

UX:

- No UX rewrite is required.
- UX remains Phase 2 planning input.
- Command-capable UI flows must stay gated until truth-state, freshness, lifecycle, audit, accessibility, localization, and FrontComposer dependencies are converted into implementation-ready stories.

Sprint status:

- `_bmad-output/implementation-artifacts/sprint-status.yaml` should be updated only after this proposal is approved.
- Epic 5 story keys should be replaced or augmented with split/resequenced story keys.
- Epic 7 story key `7-6-validate-deployment-readiness-with-smoke-tests-and-recovery-checks` should be replaced by split `7-6A` through `7-6E` keys.
- Epic 9 should remain `backlog` but should be labelled or moved out of product implementation routing if the tracking format supports that distinction.

### Technical Impact

- Query endpoints must not be implemented before minimum projection durability, query authorization, and cursor security are complete.
- Projection write paths need separate evidence for per-tenant detail projections, shared tenant index projection, audit projection, and conflict diagnostics.
- Deployment smoke tests need focused slices so auth, DAPR service invocation, health readiness, pub/sub recovery, and documentation evidence can fail independently.
- Benchmark and performance evidence must not be confused with ordinary PR-blocking unit tests.

## 3. Recommended Approach

Recommended path: Direct Adjustment.

Do not reduce PRD scope and do not roll back completed work. Instead, reorganize the backlog and story handoff surface before implementation begins.

Rationale:

- PRD FR coverage is complete: 65 of 65 FRs are mapped.
- UX and architecture are aligned.
- The blockers are sequencing, story size, and routing clarity.
- Direct adjustment preserves the MVP while reducing implementation risk.

Effort estimate: Medium.

Risk level after correction: Medium-low for planning handoff, with high-risk implementation areas still correctly isolated into projection, authorization, and deployment evidence stories.

Timeline impact:

- Short-term planning work increases because Epic 5 and Story 7.6 need reshaping.
- Implementation risk decreases because Developer agents receive smaller, prerequisite-correct stories.

## 4. Detailed Change Proposals

### Proposal A: Resequence and Split Epic 5

Artifact: `_bmad-output/planning-artifacts/epics.md`

Section: `## Epic 5: Operators and Developers Can Query Tenant State and Audit Access`

OLD:

```text
### Story 5.1: Query a Paginated Tenant List
### Story 5.2: Query Tenant Details and Tenant Users
### Story 5.3: Query the Tenants a User Belongs To
### Story 5.4: Query Tenant Access Audit History
### Story 5.5: Provide Safe Cursor-Based Pagination for Query Endpoints
### Story 5.6: Persist Tenant Projections Without Silent Write Loss
### Story 5.7: Enforce Query-Side Authorization and Isolation
```

NEW:

```text
### Story 5.1: Persist Per-Tenant Detail Projections Without Silent Write Loss
### Story 5.2: Persist the Shared Tenant Index Projection Without Silent Write Loss
### Story 5.3: Persist the Tenant Audit Projection Without Silent Write Loss
### Story 5.4: Expose Projection Write Conflict Diagnostics and Recovery Evidence
### Story 5.5: Enforce Query-Side Authorization and Isolation
### Story 5.6: Provide Safe Cursor-Based Pagination for Query Endpoints
### Story 5.7: Query a Paginated Tenant List
### Story 5.8: Query Tenant Details and Tenant Users
### Story 5.9: Query the Tenants a User Belongs To
### Story 5.10: Query Tenant Access Audit History
```

Rationale:

Projection durability, query-side authorization, and cursor security are safety prerequisites for endpoint delivery. This removes the forward dependency by putting enabling work first.

Minimum acceptance criteria to preserve in every endpoint story:

- Returned rows are scoped to the requester.
- Cursor tokens are opaque, signed, endpoint-bound, and scope-bound.
- Error bodies and pagination metadata do not leak hidden tenant or user existence.
- Projection freshness and eventual-consistency behavior are documented.
- Tests include authorized, unauthorized, partially authorized, empty, invalid cursor, and cross-tenant cases as applicable.

Sprint status proposal:

```yaml
  # Epic 5: Operators and Developers Can Query Tenant State and Audit Access
  epic-5: backlog
  5-1-persist-per-tenant-detail-projections-without-silent-write-loss: backlog
  5-2-persist-the-shared-tenant-index-projection-without-silent-write-loss: backlog
  5-3-persist-the-tenant-audit-projection-without-silent-write-loss: backlog
  5-4-expose-projection-write-conflict-diagnostics-and-recovery-evidence: backlog
  5-5-enforce-query-side-authorization-and-isolation: backlog
  5-6-provide-safe-cursor-based-pagination-for-query-endpoints: backlog
  5-7-query-a-paginated-tenant-list: backlog
  5-8-query-tenant-details-and-tenant-users: backlog
  5-9-query-the-tenants-a-user-belongs-to: backlog
  5-10-query-tenant-access-audit-history: backlog
  epic-5-retrospective: optional
```

### Proposal B: Split Story 7.6 Before Handoff

Artifact: `_bmad-output/planning-artifacts/epics.md`

Section: `### Story 7.6: Validate Deployment Readiness with Smoke Tests and Recovery Checks`

OLD:

```text
### Story 7.6: Validate Deployment Readiness with Smoke Tests and Recovery Checks
```

NEW:

```text
### Story 7.6A: Validate Production Auth Smoke Tests
### Story 7.6B: Validate DAPR Component and Service Invocation Smoke Tests
### Story 7.6C: Validate Health and Dependency Readiness Smoke Tests
### Story 7.6D: Validate Pub/Sub Recovery and Catch-Up Evidence
### Story 7.6E: Publish the Deployment Readiness Checklist and Evidence Template
```

Rationale:

The current story spans unrelated failure modes. Splitting it keeps auth, DAPR topology, health readiness, pub/sub recovery, and operator evidence independently testable and diagnosable.

Sprint status proposal:

```yaml
  7-6a-validate-production-auth-smoke-tests: backlog
  7-6b-validate-dapr-component-and-service-invocation-smoke-tests: backlog
  7-6c-validate-health-and-dependency-readiness-smoke-tests: backlog
  7-6d-validate-pubsub-recovery-and-catch-up-evidence: backlog
  7-6e-publish-the-deployment-readiness-checklist-and-evidence-template: backlog
```

### Proposal C: Classify Evidence and Benchmark Lanes in Epic 7

Artifact: `_bmad-output/planning-artifacts/epics.md`

Sections:

- `### Story 7.4: Expose Tenant Command and Event Metrics with OpenTelemetry`
- `### Story 7.5: Prove Stateless Operation, Health, and Startup Reconstruction`

OLD:

```text
Given tenant commands are submitted
When command processing completes, rejects, or fails
Then OpenTelemetry spans or metrics record command latency
And p95 command duration can be measured against the 50ms target.

Given startup reconstruction performance tests run with the target scale data set
When 1,000 tenants with an assumed average of 500 events each are seeded
Then ready-state reconstruction completes within the 30-second target or reports a documented failure
And test evidence is categorized appropriately for CI or scheduled performance lanes.
```

NEW:

```text
Given telemetry evidence is collected for command, event, and query paths
When story verification is performed
Then smoke-level telemetry presence checks run in the normal implementation lane
And p95 latency threshold evidence is classified as release evidence or scheduled performance evidence, not an ordinary unit-test gate unless explicitly approved.

Given startup reconstruction performance tests run with the target scale data set
When 1,000 tenants with an assumed average of 500 events each are seeded
Then ready-state reconstruction completes within the 30-second target or reports a documented failure
And the 500,000-event benchmark is classified as scheduled performance evidence, while ordinary readiness and health checks remain in the implementation lane.
```

Rationale:

This keeps high-cost benchmark/load proof visible without turning every PR into a scale-test requirement.

### Proposal D: Guard Epic 9 from Product Implementation Routing

Artifact: `_bmad-output/planning-artifacts/epics.md`

Section: `## Epic 9: Administrators Can Plan Phase 2 UI Access Operations Safely`

OLD:

```text
**Readiness status:** readiness/planning-only. This epic produces Phase 2 UI dependency maps, specifications, and acceptance-evidence requirements. It is not a shippable Admin UI implementation epic and must not be routed to Developer agents as product delivery work until separate implementation stories are created.
```

NEW:

```text
**Readiness status:** readiness/planning-only. This epic produces Phase 2 UI dependency maps, specifications, and acceptance-evidence requirements. It is not a shippable Admin UI implementation epic and must not be routed to Developer agents as product delivery work until separate implementation stories are created.

**Routing rule:** Product implementation must not create Developer-agent story files directly from Epic 9 stories. Before Phase 2 UI implementation starts, Product/UX/Architecture must convert these planning outputs into implementation stories with explicit source projections, FrontComposer/Fluent UI dependencies, adapter architecture, command lifecycle behavior, accessibility/localization evidence, and test artifacts.
```

Rationale:

The current document already states the planning-only boundary. The routing rule makes the handoff consequence explicit.

Sprint status proposal:

- Leave Epic 9 and stories 9.1-9.7 in backlog until planning outputs are needed.
- Do not mark them `ready-for-dev`.
- If the tracking system supports labels, mark Epic 9 as `planning-only` or `not-dev-ready`.

### Proposal E: Story File Creation Rules for Broad Acceptance Criteria

Artifact: Future story files under `_bmad-output/implementation-artifacts`

OLD:

```text
Broad evidence-oriented acceptance criteria can be copied directly from epics into story files.
```

NEW:

```text
When creating story files, every broad evidence-oriented acceptance criterion must name:

- Verification command or review procedure.
- Expected evidence artifact path.
- Pass/fail threshold.
- Validation lane: normal CI, Tier 2, Tier 3, scheduled performance, manual release evidence, or documentation review.
```

Rationale:

The readiness assessment found several broad validation claims. They are acceptable at epic level, but story handoff needs concrete verification instructions.

## 5. Checklist Execution Notes

| Checklist Item | Status | Notes |
| --- | --- | --- |
| 1.1 Triggering issue identified | Done | Trigger is the 2026-05-31 implementation readiness assessment. |
| 1.2 Core problem defined | Done | Readiness/story-shaping problem, not PRD coverage failure. |
| 1.3 Supporting evidence gathered | Done | Evidence from readiness report and planning artifacts loaded. |
| 2.1 Current epic impact evaluated | Done | Epic 5 has critical sequencing risk. |
| 2.2 Required epic-level changes determined | Done | Epic 5 resequence, Story 5.6 split, Story 7.6 split, Epic 9 guard. |
| 2.3 Remaining epics reviewed | Done | Epics 2-4, 6, 8 remain implementation-ready; Epic 7 needs split/lane work. |
| 2.4 New/obsolete epics considered | Done | No new epic required; Epic 9 remains planning-only. |
| 2.5 Priority/order changes considered | Done | Epic 5 story order must change. |
| 3.1 PRD conflicts checked | Done | No MVP scope change required. |
| 3.2 Architecture conflicts checked | Done | Architecture supports the correction. |
| 3.3 UX conflicts checked | Done | UX remains Phase 2 input; not Phase 1 implementation. |
| 3.4 Secondary artifacts checked | Done | Sprint status was updated after approval. |
| 4.1 Direct adjustment evaluated | Viable | Recommended. |
| 4.2 Rollback evaluated | Not viable | No completed work needs reverting. |
| 4.3 MVP review evaluated | Not viable | MVP scope remains achievable. |
| 4.4 Path selected | Done | Direct backlog adjustment. |
| 5.1 Issue summary created | Done | Included in this proposal. |
| 5.2 Impact documented | Done | Included above. |
| 5.3 Recommended path documented | Done | Direct adjustment. |
| 5.4 MVP impact/action plan defined | Done | No MVP scope change; backlog reshaping required. |
| 5.5 Handoff plan established | Done | Product Owner/Developer coordination. |
| 6.1 Checklist completion reviewed | Done | Approval and artifact updates are complete. |
| 6.2 Proposal accuracy verified | Done | Cross-checked against report and artifacts. |
| 6.3 User approval | Done | Approved by Jerome on 2026-05-31. |
| 6.4 Sprint status update | Done | `_bmad-output/implementation-artifacts/sprint-status.yaml` updated with split/resequenced story keys. |
| 6.5 Handoff plan confirmation | Done | Handoff is Product Owner / Developer for backlog reorganization, with Architect/Test Architect involvement for Phase 2 UI conversion and evidence-lane classification. |

## 6. Implementation Handoff

Change scope classification: Moderate.

Route to:

- Product Owner / Developer agents for backlog reorganization.
- Developer agent only after Epic 5 and Story 7.6 split stories exist and sprint status is updated.
- Product Manager / Architect later only if Epic 9 is promoted into real Phase 2 UI implementation.

Responsibilities:

- Product Owner: approve the story resequencing and split plan; keep Epic 9 out of implementation routing.
- Developer agent: implement only stories marked ready after split/resequence.
- Architect: review any future Phase 2 UI implementation conversion for FrontComposer adapter architecture and query/projection truth-state boundaries.
- Test Architect: classify validation lanes for telemetry, startup reconstruction, smoke, Tier 2, Tier 3, scheduled performance, and release evidence.

Success criteria:

- Epic 5 no longer has endpoint stories preceding their projection, authorization, and cursor prerequisites.
- Story 5.6 is replaced by four implementation-ready slices.
- Story 7.6 is replaced by five implementation-ready slices.
- Epic 9 cannot be accidentally routed as product implementation work.
- Sprint status reflects approved story keys before any Developer-agent story creation.
- Future story files include concrete commands, evidence paths, thresholds, and validation lanes.

## 7. Workflow Execution Log

- 2026-05-31: Correct-course workflow activated for readiness assessment status `NEEDS WORK`.
- 2026-05-31: Planning artifacts and project context loaded.
- 2026-05-31: Sprint Change Proposal drafted in batch mode.
- 2026-05-31: Jerome approved the proposal.
- 2026-05-31: Epic 5 resequenced and split in `_bmad-output/planning-artifacts/epics.md`.
- 2026-05-31: Story 7.6 split and Epic 7 evidence lanes clarified in `_bmad-output/planning-artifacts/epics.md`.
- 2026-05-31: Epic 9 routing guard added in `_bmad-output/planning-artifacts/epics.md`.
- 2026-05-31: Sprint status updated in `_bmad-output/implementation-artifacts/sprint-status.yaml`.
- 2026-05-31: Handoff route set to Product Owner / Developer agents, with Architect/Test Architect involvement for Phase 2 UI conversion and evidence-lane classification.

## 8. Applied Updates and Next Steps

1. Completed: Updated `_bmad-output/planning-artifacts/epics.md` with the approved Epic 5 resequence, Story 7.6 split, Epic 9 routing rule, and Epic 7 evidence-lane wording.
2. Completed: Updated `_bmad-output/implementation-artifacts/sprint-status.yaml` to match the approved story keys.
3. Next: Re-run implementation readiness assessment.
4. Next: If the status becomes ready, begin story creation from Epic 1 or the selected implementation order.

Approval:

```text
Approved by Jerome on 2026-05-31.
```
