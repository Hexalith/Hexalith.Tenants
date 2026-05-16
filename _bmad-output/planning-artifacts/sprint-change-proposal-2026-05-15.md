# Sprint Change Proposal: Story Readiness Cleanup

**Date:** 2026-05-15
**Project:** Hexalith.Tenants
**Trigger:** Implementation readiness assessment `implementation-readiness-report-2026-05-15.md`
**Status:** Approved and implemented

## 1. Issue Summary

The implementation readiness assessment found that the core planning artifacts are present, the Phase 1 backend/package/documentation scope is clear, and all 65 PRD functional requirements are mapped to epics. The remaining readiness risk is story execution quality: several completed or planned stories either front-load future capability or combine independently verifiable outcomes into one assignment.

The assessment identified three major story-quality issues:

1. Story 2.1 defines the full tenant contract surface, including Epic 3 user-role and configuration contracts.
2. Story 2.3 includes `TenantState.Users` and `TenantState.Configuration` Apply behavior before the Epic 3 Handle stories own those capabilities.
3. Story 7.3 combines snapshot configuration, restart reconstruction, pub/sub outage behavior, integration recovery, and performance benchmarking.

It also identified minor planning hygiene issues around Story 8.3, Story 2.4 DAPR version alignment, Epic 1 naming, and Phase 2 FrontShell sequencing.

## 2. Impact Analysis

### Epic Impact

**Epic 2: Core Tenant Management & Global Administration**

Epic 2 can remain complete, but Story 2.1 and Story 2.3 need explicit rationale. Since these stories are already implemented, removing contracts or state Apply methods would create churn and potential regressions. The better correction is to mark these as deliberate foundation decisions and add guardrails for future story creation.

**Epic 3: Tenant Membership, Roles & Configuration**

Epic 3 remains the owning epic for user-role Handle behavior, configuration Handle behavior, role enforcement, and configuration limits. The correction should clarify that Epic 2 contract/state preparation did not transfer Epic 3 behavior ownership.

**Epic 7: Deployment & Observability**

Epic 7 remains valid, but Story 7.3 should be treated as three logical work packages for review and future maintenance:

1. Snapshot interval configuration and stateless restart reconstruction.
2. Pub/sub outage graceful degradation and drain recovery verification.
3. Snapshot performance benchmark and nightly CI gate.

**Epic 8: Documentation & Adoption**

Story 8.3 is acceptable after completion, but future planning should split adoption/demo work from repository contribution documentation when these have different owners or review paths.

### Artifact Conflicts

**PRD**

No PRD requirement changes are needed. The MVP remains achievable and Phase 1 remains backend/package/documentation only. The PRD already states that event contracts may evolve before v1.0 and that Admin UI / FrontShell is Phase 2 unless explicitly promoted.

**Architecture**

No architecture change is required. The architecture already supports complete aggregate replay, snapshot configuration, stateless service reconstruction, and DAPR pub/sub degradation behavior.

**UX**

No Phase 1 UX implementation change is required. The UX specification remains authoritative for Phase 2 Admin UI / FrontShell work only. Phase 2 UI stories should later include explicit blocked-by relationships for FrontShell component and hook dependencies.

**Sprint Status**

Do not change `sprint-status.yaml` until this proposal is approved. If approved, add a post-readiness correction entry rather than renumbering completed historical stories.

## 3. Recommended Approach

**Selected path:** Direct Adjustment

**Scope classification:** Moderate

**Rationale:** The readiness issues are planning and story-slicing issues, not evidence of missing PRD coverage or broken architecture. Because the affected stories are already marked done, reopening and renumbering historical work would create more confusion than it removes. The cleanest correction is to add explicit post-readiness notes to the affected stories, capture a reusable story-slicing rule, and create focused follow-up validation work only where evidence is incomplete.

**Effort estimate:** Low to medium

**Risk level:** Low

**Timeline impact:** Minimal. This should not reopen completed source implementation unless validation reveals that a claimed acceptance criterion lacks evidence.

## 4. Detailed Change Proposals

### Story 2.1: Tenant Domain Contracts

**Section:** Story and Acceptance Criteria

**OLD:**

```markdown
As a developer,
I want all tenant commands, events, enums, and identity types defined in the Contracts package,
So that consuming services and all other packages have a stable, shared API surface to reference.

1. Given the Contracts project exists When a developer inspects the Commands folder Then it contains all 12 command records...
2. Given the Contracts project exists When a developer inspects the Events folder Then it contains all 11 event records...
```

**NEW:**

```markdown
As a developer,
I want the public tenant contract baseline defined in the Contracts package,
So that consuming services and all packages can compile against a known pre-1.0 API surface while implementation behavior remains owned by the appropriate feature stories.

Post-readiness note, 2026-05-15:
This story intentionally established the full pre-1.0 public contract baseline. User-role and configuration command/event records are contract declarations only in Story 2.1; their executable aggregate behavior remains owned by Epic 3 Stories 3.1 and 3.3. Future contract-baseline stories must state this explicitly and must not expose unimplemented operations through runtime endpoints until the owning behavior story is complete.

Acceptance Criteria Addendum:
- Given a command or event belongs to a later behavior story, when Story 2.1 is reviewed, then the story identifies it as a contract-only declaration and links to the owning behavior story.
- Given a declared command has no completed behavior story, when runtime endpoints are reviewed, then the command is not documented as a usable operation.
```

**Rationale:** Preserve the completed contract baseline while removing ambiguity about behavior ownership.

### Story 2.3: Tenant Aggregate Lifecycle

**Section:** Tasks / Subtasks and Dev Notes

**OLD:**

```markdown
Task 1.1: Create TenantState ... state class with ALL 9 Apply methods (4 lifecycle + 3 user-role + 2 configuration). User-role and configuration Apply methods are needed now for state completeness; their Handle methods come in Epic 3

TenantState includes ALL Apply methods now...
```

**NEW:**

```markdown
Post-readiness note, 2026-05-15:
Story 2.3 is accepted as an aggregate replay foundation story for `TenantState`, not as ownership of user-role or configuration behavior. The non-lifecycle Apply methods exist so a tenant aggregate can replay its full event stream once Epic 3 events exist. The Handle methods, validation, authorization, duplicate checks, and configuration limits remain owned by Stories 3.1 and 3.3.

Acceptance Criteria Addendum:
- Given Story 2.3 includes non-lifecycle Apply methods, when the story is reviewed, then it verifies only replay/state mutation behavior for historical events and does not claim user-role or configuration command behavior.
- Given Epic 3 owns user-role and configuration commands, when Stories 3.1 and 3.3 are reviewed, then they verify the corresponding Handle methods and business rules.
```

**Rationale:** Makes the foundation decision explicit and prevents future agents from treating state replay support as completed behavior.

### Story 7.3: Stateless Scaling & Snapshot Configuration

**Section:** Story, Acceptance Criteria, and Tasks

**OLD:**

```markdown
As a platform engineer,
I want the tenant service to be stateless with configurable snapshot intervals and graceful degradation,
So that I can scale horizontally, restart without data loss, and maintain operations during infrastructure partial failures.

Acceptance criteria include restart reconstruction, tenant snapshot interval, global admin snapshot interval, pub/sub outage behavior, Tier 3 outage recovery, and 500,000-event performance testing.
```

**NEW:**

```markdown
Post-readiness note, 2026-05-15:
Story 7.3 is oversized and should not be used as a model for future story slicing. For evidence review and future maintenance, treat it as three logical packages:

7.3A Snapshot Configuration and Restart Reconstruction
- Owns AC 1, AC 2, and AC 3.
- Evidence: appsettings snapshot interval, SnapshotConfigurationTests, stateless restart verification.

7.3B Pub/Sub Outage Graceful Degradation
- Owns AC 4 and AC 5.
- Evidence: graceful degradation integration tests and drain recovery verification.

7.3C Snapshot Performance Benchmark
- Owns AC 6.
- Evidence: performance test scaffold and nightly CI schedule/gate.

Future reliability/performance stories must carry one independently testable outcome unless a single runtime change cannot be verified safely in isolation.
```

**Rationale:** Avoids renumbering completed stories while giving reviewers and future agents clear ownership boundaries.

### Story 8.3: Aha Moment Demo & Project Documentation

**Section:** Future Story-Slicing Guidance

**OLD:**

```markdown
Story 8.3 bundles demo walkthrough/scripts, README update, CHANGELOG, and CONTRIBUTING documentation.
```

**NEW:**

```markdown
Planning note:
If similar adoption work is planned again, split demo/adoption collateral from repository contributor documentation when they have different owners, review criteria, or release timing. Demo work should own user-facing evaluation flow. Repository documentation should own contributor setup, changelog, PR, branch, and test policy.
```

**Rationale:** This is a minor concern and does not require reopening completed documentation unless ownership or release timing changes.

### Story 2.4: DAPR Version Alignment

**Section:** Acceptance Criteria Addendum

**NEW:**

```markdown
Acceptance Criteria Addendum:
- Given Story 2.4 depends on DAPR package behavior, when the story is reviewed, then `Directory.Packages.props` and related DAPR package references are verified to use the intended compatible version set.
- Given a dependency version alignment issue is discovered, when the story is completed, then the exact version alignment is recorded in completion notes and build/test validation.
```

**Rationale:** Converts required dependency alignment from implementation guidance into reviewable acceptance evidence.

### Phase 2 UI Sequencing

**Section:** Future Phase 2 Planning Guidance

**NEW:**

```markdown
Phase 2 Admin UI / FrontShell stories must include explicit blocked-by relationships for required FrontShell components and hooks, including AuditTimeline, ConsequencePreview, useCommand pending/concurrent command support, toast consolidation, layout variants, tokens, and Storybook references.
```

**Rationale:** Preserves the Phase 1 backend scope boundary and prevents UI dependencies from silently becoming Phase 1 blockers.

## 5. Checklist Results

| Item | Status | Notes |
| --- | --- | --- |
| 1.1 Triggering story identified | Done | Trigger is readiness report; impacted stories are 2.1, 2.3, 7.3, with minor concerns for 8.3 and 2.4. |
| 1.2 Core problem defined | Done | Story slicing and ownership clarity issue, not missing requirements. |
| 1.3 Supporting evidence gathered | Done | Evidence from readiness report and affected story files. |
| 2.1 Current epic impact | Done | Epics remain valid; corrections are story-level. |
| 2.2 Required epic-level changes | Done | No new epic required. Add post-readiness correction notes. |
| 2.3 Remaining epics reviewed | Done | Epic 8 minor split guidance; Phase 2 UI sequencing noted. |
| 2.4 Future epic invalidation | Done | No planned epic is invalidated. |
| 2.5 Epic order/priority | Done | No resequencing needed. |
| 3.1 PRD conflict check | Done | No PRD change needed. |
| 3.2 Architecture conflict check | Done | No architecture change needed. |
| 3.3 UX conflict check | Done | No Phase 1 UX blocker; Phase 2 dependency guidance needed later. |
| 3.4 Other artifacts | Action-needed | Update affected story notes and sprint-status only after approval. |
| 4.1 Direct adjustment | Viable | Recommended path. |
| 4.2 Rollback | Not viable | Reverting completed contracts/state/tests would add risk without improving product behavior. |
| 4.3 MVP review | Not viable | MVP scope remains achievable. |
| 4.4 Recommended path selected | Done | Direct adjustment with post-readiness notes and optional follow-up validation. |
| 5.1 Issue summary | Done | Included above. |
| 5.2 Impact and adjustments | Done | Included above. |
| 5.3 Recommended path | Done | Included above. |
| 5.4 MVP impact and action plan | Done | No MVP reduction. |
| 5.5 Handoff plan | Done | Product Owner/Developer for artifact updates; Developer for evidence validation. |
| 6.1 Checklist completion | Done | All applicable sections addressed. |
| 6.2 Proposal accuracy | Done | Consistent with readiness report and story status. |
| 6.3 User approval | Action-needed | Pending Jerome approval. |
| 6.4 sprint-status update | Action-needed | Only after approval. |
| 6.5 Next steps and handoff | Action-needed | Pending approval. |

## 6. Implementation Handoff

**Recommended handoff recipients:** Product Owner / Developer

**Responsibilities:**

- Product Owner: Approve whether completed stories should receive post-readiness notes or whether planning artifacts should be left untouched and this proposal should stand as the correction record.
- Developer: If approved, update the affected story files with the proposed notes and add a single post-readiness correction entry to `sprint-status.yaml`.
- Developer: Verify that Story 7.3 evidence exists for the three logical packages before closing the correction.

**Success criteria:**

1. The readiness report issues have an approved correction path.
2. Story 2.1 explicitly distinguishes contract baseline from behavior ownership.
3. Story 2.3 explicitly distinguishes state replay foundation from Epic 3 behavior ownership.
4. Story 7.3 has clear logical evidence boundaries or is split if reopened.
5. Phase 1 remains backend/package/documentation only.
6. No completed source implementation is reverted solely for planning cleanliness.

## 7. Approval Request

Implemented on 2026-05-15 after Jerome requested the deferred work be applied.

Implemented decision:

**Approved Direct Adjustment:** Added post-readiness notes to Stories 2.1, 2.3, 7.3, and 8.3, added an AC addendum to Story 2.4, and updated `sprint-status.yaml` with one post-readiness correction entry.
