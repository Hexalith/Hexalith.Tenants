---
project: Hexalith.Tenants
date: 2026-05-26
workflow: bmad-correct-course
trigger: _bmad-output/planning-artifacts/implementation-readiness-report-2026-05-26.md
readinessStatus: NEEDS WORK
mode: Batch
scopeClassification: Moderate
approvalStatus: Approved by Jerome on 2026-05-26
approvedTopic: tenants.events
implementationStatus: Applied
---

# Sprint Change Proposal: Implementation Readiness Corrections

## 1. Issue Summary

The Implementation Readiness Assessment completed on 2026-05-26 found that the Hexalith.Tenants planning set has full FR traceability but is not ready for unqualified implementation handoff. The report identified 15 issues or concerns: 3 critical, 6 major, 3 minor, and 3 UX alignment clarifications.

The core problem is artifact consistency and story readiness, not missing functional coverage. Several source-of-truth documents and story specs disagree on aggregate names, DAPR topic naming, story sizing, style/release conventions, and Phase 2 UI readiness boundaries. Proceeding without correction would create avoidable implementation ambiguity around EventStore reflection discovery, DAPR pub/sub subscriptions, sprint slicing, and UI dependency gating.

Evidence reviewed:

- `_bmad-output/planning-artifacts/implementation-readiness-report-2026-05-26.md`
- `_bmad-output/planning-artifacts/prd.md`
- `_bmad-output/planning-artifacts/architecture.md`
- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/ux-design-specification.md`
- `_bmad-output/project-context.md`
- Current source/docs/tests references for the DAPR topic and global administrator naming

## 2. Change Analysis Checklist Results

| Item | Status | Finding |
| --- | --- | --- |
| 1.1 Triggering story | [N/A] | The trigger is not a single implementation story. It is the 2026-05-26 readiness report. |
| 1.2 Core problem | [x] | Issue type: artifact inconsistency and story-readiness failure discovered during planning validation. |
| 1.3 Supporting evidence | [x] | Readiness report names the critical and major findings; exact references were confirmed in PRD, architecture, epics, UX, source, docs, and tests. |
| 2.1 Current epic viability | [!] | Epics remain viable, but Epic 1, Epic 2, Epic 4, Epic 6, Epic 10, and Epic 12 need clarification before future handoff. |
| 2.2 Epic-level changes | [x] | No new epic is needed. Existing epics need wording corrections, readiness gates, and backlog classification cleanup. |
| 2.3 Future epic impact | [x] | Epic 12 must remain Phase 2 planning/dependency governance until converted into concrete UI stories with explicit blockers. |
| 2.4 New/obsolete epics | [x] | No epic becomes obsolete. Story 2.4 must be treated as historical only; 2.4A-2.4E are the future implementation work packages. |
| 2.5 Order/priority | [!] | Fix critical naming/topic conflicts before assigning more implementation work. Consider moving pagination utilities before or into Story 9.1. |
| 3.1 PRD conflicts | [!] | PRD still contains Allman brace and `v*` tag release wording; PRD NFR24 says WCAG 2.1 AA while UX targets WCAG 2.2 AA. |
| 3.2 Architecture conflicts | [x] | Architecture already defines the canonical aggregate name and topic: `GlobalAdministratorsAggregate` and `tenants.events`. |
| 3.3 UX conflicts | [!] | UX needs clarification on WCAG target and whether user lookup is exact-ID lookup or broader user search. |
| 3.4 Other artifacts | [!] | Current source/docs/tests still reference `system.tenants.events`; several implementation story files retain old Allman/topic/naming assumptions. |
| 4.1 Direct adjustment | [x] | Viable. Estimated effort: medium. Risk: low to medium, depending on whether topic correction changes code and tests. |
| 4.2 Rollback | [N/A] | Rollback does not address artifact conflicts and would add churn. |
| 4.3 MVP review | [N/A] | MVP scope remains achievable. No requirement reduction is needed. |
| 4.4 Recommended path | [x] | Direct Adjustment plus backlog gating. Correct artifacts, align topic defaults/docs/tests if approved, then rerun readiness. |
| 5.1 Issue summary | [x] | Included in this proposal. |
| 5.2 Epic/artifact impact | [x] | Included in Sections 3 and 5. |
| 5.3 Path forward | [x] | Included in Section 4. |
| 5.4 MVP impact/action plan | [x] | MVP unaffected; implementation handoff should stay gated until corrections are complete. |
| 5.5 Agent handoff | [x] | Included in Section 6. |
| 6.1 Checklist completion | [x] | Applicable items are addressed; approval-dependent items remain marked action-needed. |
| 6.2 Proposal accuracy | [x] | Proposal is grounded in the reviewed artifacts and source references. |
| 6.3 User approval | [!] | Pending Jerome's explicit approval. |
| 6.4 sprint-status.yaml update | [N/A] | No `_bmad-output/implementation-artifacts/sprint-status.yaml` file was found. Update is not applicable unless sprint tracking is generated later. |
| 6.5 Handoff confirmation | [!] | Pending approval. |

## 3. Impact Analysis

### Epic Impact

- Epic 1 needs convention cleanup: Story 1.1 must stop preserving outdated EventStore/Allman style assumptions, and Story 1.3 must describe semantic-release on merge to `main`, not tagged releases.
- Epic 2 needs the critical corrections: normalize `GlobalAdministratorsAggregate`/`GlobalAdministratorsState`, use `tenants.events`, and keep Story 2.4 split into 2.4A-2.4E for future work.
- Epic 3 remains viable, but Story 2.1/2.3 scaffolding decisions affect Epic 3 contract/state timing and should be explicitly marked as intentional historical scaffolding, not a future slicing model.
- Epic 4 event integration depends on the canonical DAPR topic. Consumer subscription examples must use the same topic as producer defaults.
- Epic 5 naming impact may apply to global-administrator projection references if the architecture's plural projection name is chosen as canonical. This is lower priority than aggregate naming and should be handled deliberately because current code uses singular `GlobalAdministratorProjection`.
- Epic 6 needs isolation criteria moved: aggregate/event isolation belongs in Story 6.1; projection-level isolation belongs in Story 6.2.
- Epic 7 has minor wording cleanup in Story 7.1.
- Epic 9 can proceed, but shared pagination utility work should be moved before or into Story 9.1 if not already completed.
- Epic 10 Story 10.3B must remain blocked until Story 10.3A names the exact EventStore APIs and submodule commit/version.
- Epic 12 remains Phase 2 planning/dependency governance, not shippable Admin UI product implementation.

### Story Impact

Affected stories:

- 1.1 Solution Structure & Build Configuration
- 1.2 DAPR Component Configuration & ServiceDefaults
- 1.3 CI/CD Pipeline
- 2.1 Tenant Domain Contracts
- 2.2 Global Administrator Aggregate
- 2.3 Tenant Aggregate Lifecycle
- 2.4 Tenant Service, Bootstrap & Event Publishing
- 4.1 Client DI Registration
- 4.2 Event Subscription & Local Projection Pattern
- 5.1 Per-Tenant & Global Admin Projections, if projection naming is aligned
- 6.1 In-Memory Tenant Service & Test Helpers
- 6.2 In-Memory Projection & Conformance Tests
- 7.1 Aspire Hosting & AppHost
- 8.2 Event Contract Reference & Technical Documentation
- 8.3 Aha Moment Demo & Project Documentation
- 9.1 Opaque Signed Query Cursors
- 9.5 Shared Pagination Bounds and Cursor Utilities
- 10.3A EventStore Projection Cancellation API Prerequisite
- 10.3B Cancellation Token Threading for Tenant Projection Queries
- 12.1-12.4 Phase 2 Admin UI dependency stories

### Artifact Conflicts

- PRD:
  - Code style section still says Allman braces.
  - Release section still says releases are triggered by `v*` tags.
  - NFR24 says Phase 2 Admin UI must address WCAG 2.1 AA, while UX says WCAG 2.2 AA.
- Architecture:
  - Already supports canonical aggregate name and topic.
  - Should stay the source of truth unless a deliberate topic decision changes it.
- Epics:
  - Contains stale singular aggregate references.
  - Contains `system.tenants.events` in additional requirements and Story 2.4/4.2.
  - Contains Story 2.4 split guidance, but that split must be treated as binding for future work.
- UX:
  - Uses `user lookup` and `user search` language without an explicit backend capability boundary.
  - Targets WCAG 2.2 AA while PRD says WCAG 2.1 AA.
- Source/docs/tests:
  - Current default topic and tests reference `system.tenants.events`.
  - Docs/event contract reference and demo references still use `system.tenants.events`.

### Technical Impact

If the canonical DAPR topic remains `tenants.events` as stated in project context, PRD, and architecture, implementation must update:

- `src/Hexalith.Tenants.Client/Configuration/HexalithTenantsOptions.cs`
- Topic assertions in client tests and integration tests
- `docs/event-contract-reference.md`
- `docs/demo.md`
- Implementation story files that cite `system.tenants.events`
- Any DAPR subscription examples or sample consuming service registration

This is an implementation-visible behavior change for consumers subscribing to the old topic. Because the project is still pre-1.0, this should be handled as an alignment fix rather than a post-1.0 breaking contract change. The change still needs explicit approval because it affects code defaults and documentation examples, not only planning text.

## 4. Recommended Approach

Recommended path: Direct Adjustment with backlog gating.

Rationale:

- The MVP scope remains valid and fully traced.
- The critical issues are correctable through artifact normalization and small code/docs alignment work.
- Rollback would not solve the source-of-truth conflicts.
- A PRD MVP review would be disproportionate because no requirement category has become infeasible.
- Story 2.4 already has a usable split, so the corrective action is to make the split authoritative for future work rather than inventing a new epic.

Effort estimate:

- Planning artifact corrections: 0.5 day.
- Source/docs/tests topic alignment, if approved: 0.5 to 1 day plus focused validation.
- Readiness rerun: 0.25 day.

Risk:

- Aggregate naming cleanup risk is low because current code already uses `GlobalAdministratorsAggregate`.
- Topic cleanup risk is medium because current code/docs/tests use `system.tenants.events`; changing it affects consumers and examples.
- UX clarifications are low risk if captured as Phase 2 planning decisions.

Timeline impact:

- Pause new implementation handoff until critical naming/topic/story-split corrections are applied.
- Existing completed stories do not need rollback.
- Rerun readiness after corrections, focusing on changed artifacts.

## 5. Detailed Change Proposals

### Proposal A: Normalize Global Administrators Aggregate Naming

Artifacts:

- `_bmad-output/planning-artifacts/epics.md`
- Relevant implementation story files under `_bmad-output/implementation-artifacts/`
- `docs/event-contract-reference.md`, if it is treated as implementation documentation for future consumers

Old:

```text
GlobalAdministratorAggregate
GlobalAdministratorState
```

New:

```text
GlobalAdministratorsAggregate
GlobalAdministratorsState
```

Specific planning edits:

```text
OLD:
- Two aggregates: TenantAggregate (...) and GlobalAdministratorAggregate (...)
- Snapshot strategy: 50-event interval for tenant domain, default 100 for GlobalAdministratorAggregate
- Bootstrap mechanism: ... GlobalAdministratorAggregate rejects if any GlobalAdministratorSet event exists

NEW:
- Two aggregates: TenantAggregate (...) and GlobalAdministratorsAggregate (...)
- Snapshot strategy: 50-event interval for tenant domain, default 100 for GlobalAdministratorsAggregate
- Bootstrap mechanism: ... GlobalAdministratorsAggregate rejects if any GlobalAdministratorSet event exists
```

```text
OLD:
**Then** `actors.yaml` configures TenantAggregate and GlobalAdministratorAggregate actor types

NEW:
**Then** `actors.yaml` configures TenantAggregate and GlobalAdministratorsAggregate actor types
```

```text
OLD:
**Given** the GlobalAdministratorAggregate Handle methods
**Given** the GlobalAdministratorState class

NEW:
**Given** the GlobalAdministratorsAggregate Handle methods
**Given** the GlobalAdministratorsState class
```

Do not rename command or event contracts such as `SetGlobalAdministrator`, `RemoveGlobalAdministrator`, `GlobalAdministratorSet`, or `GlobalAdministratorRemoved`. Those names describe the target user designation and are not the aggregate type name.

Rationale:

EventStore discovery and DAPR actor registration depend on exact type names. The canonical aggregate in architecture, project context, and current source is `GlobalAdministratorsAggregate`.

### Proposal B: Resolve DAPR Topic Naming to `tenants.events`

Artifacts:

- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/implementation-artifacts/2-4-tenant-service-bootstrap-and-event-publishing.md`
- `_bmad-output/implementation-artifacts/4-1-client-di-registration.md`
- `_bmad-output/implementation-artifacts/4-2-event-subscription-and-local-projection-pattern.md`
- `_bmad-output/implementation-artifacts/8-2-event-contract-reference-and-technical-documentation.md`
- `_bmad-output/implementation-artifacts/8-3-aha-moment-demo-and-project-documentation.md`
- `src/Hexalith.Tenants.Client/Configuration/HexalithTenantsOptions.cs`
- `tests/Hexalith.Tenants.Client.Tests/Registration/TenantServiceCollectionExtensionsTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/GracefulDegradationTests.cs`
- `docs/event-contract-reference.md`
- `docs/demo.md`

Old:

```text
system.tenants.events
```

New:

```text
tenants.events
```

Specific planning edits:

```text
OLD:
- Pub/Sub topic: `system.tenants.events` -- single topic for all tenant events; consumers filter by event type

NEW:
- Pub/Sub topic: `tenants.events` -- single topic for all tenant events; consumers filter by event type
```

```text
OLD:
**Then** events are published to DAPR pub/sub topic `system.tenants.events` as CloudEvents 1.0

NEW:
**Then** events are published to DAPR pub/sub topic `tenants.events` as CloudEvents 1.0
```

```text
OLD:
**Given** a consuming service is subscribed to the `system.tenants.events` DAPR pub/sub topic

NEW:
**Given** a consuming service is subscribed to the `tenants.events` DAPR pub/sub topic
```

Specific code/default edit, after approval:

```csharp
// OLD
public string TopicName { get; set; } = "system.tenants.events";

// NEW
public string TopicName { get; set; } = "tenants.events";
```

Dead letter topic remains:

```text
deadletter.tenants.events
```

Rationale:

Project context, architecture, and PRD FR36 already identify `tenants.events` as the canonical topic. Keeping `system.tenants.events` in code/docs creates producer/consumer subscription drift.

### Proposal C: Make Story 2.4A-2.4E the Authoritative Future Split

Artifact:

- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/implementation-artifacts/2-4-tenant-service-bootstrap-and-event-publishing.md`
- Sprint/backlog tracking, if regenerated later

Old:

```text
Story 2.4: Tenant Service, Bootstrap & Event Publishing
```

New:

```text
Story 2.4 remains historical only.

Future implementation, evidence review, or rework must use:
- 2.4A Command API and EventStore processing endpoint wiring
- 2.4B Bootstrap hosted service and multi-instance idempotency
- 2.4C DAPR pub/sub publication and recovery behavior
- 2.4D API error and authentication response mapping
- 2.4E Tier 2 command pipeline verification
```

Implementation artifact action:

- Mark the existing `2-4-tenant-service-bootstrap-and-event-publishing.md` as historical/completed.
- If any 2.4 area is reopened, create a focused 2.4A-2.4E story file instead of reassigning the broad 2.4 story.

Rationale:

The readiness report correctly identifies Story 2.4 as too broad for future sprint execution. The epics document already contains a useful split; the correction is to make that split operational.

### Proposal D: Mark Story 2.1 and 2.3 as Historical Scaffolding Exceptions

Artifacts:

- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/implementation-artifacts/2-1-tenant-domain-contracts.md`
- `_bmad-output/implementation-artifacts/2-3-tenant-aggregate-lifecycle.md`

Story 2.1 old:

```text
**Then** it contains all 12 command records: ... membership and configuration commands ...
**Then** it contains all 11 event records: ... membership and configuration events ...
```

Story 2.1 new:

```text
**Then** it contains the initial public contract set needed by Epic 2 and known Epic 3 behavior.

Scaffolding exception: membership and configuration contracts are included early because the project is pre-1.0 and already uses conformance/serialization/naming tests to prevent unused or malformed public contracts from drifting. Future stories should prefer vertical contract creation by first behavioral use unless the Product Owner explicitly approves a scaffolding exception.
```

Story 2.3 old:

```text
- Note: TenantState includes Users/Configuration Apply methods for completeness -- those Handle methods are implemented in Epic 3 (Stories 3.1, 3.3) but the state class is created here with all Apply methods
```

Story 2.3 new:

```text
- Scaffolding exception: TenantState includes Users/Configuration Apply methods because related event contracts already exist and projection/testing conformance depends on stable replay behavior. This does not mean membership/configuration command behavior is complete; that behavior remains owned by Epic 3 Stories 3.1 and 3.3. Future stories should avoid adding future-facing Apply methods before their behavior slice unless explicitly approved.
```

Rationale:

These stories appear to have been completed historically. Re-slicing completed work would be churn. Marking them as intentional exceptions preserves traceability while preventing future stories from copying the upfront-surface pattern.

### Proposal E: Move Projection Isolation Criteria from Story 6.1 to Story 6.2

Artifacts:

- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/implementation-artifacts/6-1-in-memory-tenant-service-and-test-helpers.md`
- `_bmad-output/implementation-artifacts/6-2-in-memory-projection-and-conformance-tests.md`

Story 6.1 old:

```text
**Given** the InMemoryTenantService
**When** two tenants are created and users are added to each
**Then** projections for tenant A never contain data from tenant B (aggregate-level isolation guarantee)
```

Story 6.1 new:

```text
**Given** the InMemoryTenantService
**When** two tenants are created and users are added to each
**Then** aggregate state and produced events for tenant A never include tenant B membership or configuration data
```

Story 6.2 add:

```text
**Given** the InMemoryTenantProjection
**When** events for tenant A and tenant B are applied in the same test run
**Then** projected query results for tenant A never contain tenant B data, and projected query results for tenant B never contain tenant A data
```

Rationale:

Story 6.1 introduces the in-memory service, not the in-memory projection. Projection isolation is valid but belongs in Story 6.2.

### Proposal F: Keep Story 10.3B Blocked Until EventStore Dependency Evidence Exists

Artifacts:

- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/implementation-artifacts/10-3a-eventstore-projection-cancellation-api-prerequisite.md`
- `_bmad-output/implementation-artifacts/10-3b-cancellation-token-threading-for-tenant-projection-queries.md`

Old:

```text
Story 10.3B can be assigned after Story 10.3A in normal sequence.
```

New:

```text
Story 10.3B remains blocked until Story 10.3A names the exact EventStore cancellation-aware APIs and the EventStore submodule commit/version available to Tenants. If the prerequisite is already satisfied, update 10.3A and 10.3B with the concrete API names and submodule commit before treating 10.3B as implementation-ready.
```

Rationale:

Tenants should not invent a Tenants-only projection cancellation path if the dependency belongs in EventStore.

### Proposal G: Keep Epic 12 Planning-Only Until Converted to UI Stories

Artifacts:

- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/implementation-artifacts/12-*.md`
- Future Phase 2 UI backlog

Old risk:

```text
Epic 12 is scheduled as if it delivers shippable Admin UI behavior.
```

New:

```text
Epic 12 is planning/dependency governance only. It produces dependency maps and readiness decisions. Phase 2 Admin UI implementation must be represented by separate UI stories with explicit `blockedBy` entries for FrontComposer, command lifecycle, consequence preview, audit evidence, accessibility, localization, and status reconciliation dependencies.
```

Rationale:

Epic 12 can remain useful planning work, but it should not be counted as delivered UI product behavior.

### Proposal H: Update Style and Release Wording

Artifacts:

- `_bmad-output/planning-artifacts/prd.md`
- `_bmad-output/planning-artifacts/epics.md`
- Relevant implementation story files

PRD style old:

```text
Inherited from EventStore's `.editorconfig`: file-scoped namespaces, Allman braces, `_camelCase` private fields, `I` prefix for interfaces, `Async` suffix for async methods, 4-space indentation, CRLF, UTF-8, warnings as errors.
```

PRD style new:

```text
Follow the current Hexalith.Tenants `.editorconfig` and project context: file-scoped namespaces, K&R brace style in Tenants code, `_camelCase` private fields, `I` prefix for interfaces, `Async` suffix for async methods, 4-space indentation, CRLF, UTF-8, nullable references, and warnings as errors.
```

PRD release old:

```text
- Release: Triggered by `v*` tags -- full test suite, pack, validate 5 packages, push to NuGet.org
```

PRD release new:

```text
- Release: Triggered on merge to `main` through semantic-release -- determines SemVer from Conventional Commits, runs tests, packs and validates 5 packages, publishes to NuGet.org, creates a GitHub Release, and updates CHANGELOG.md
```

Epics Story 1.1 old:

```text
**Then** it contains centralized NuGet package versions for all dependencies (..., MediatR, MinVer)
**Then** it enforces EventStore conventions (file-scoped namespaces, Allman braces, `_camelCase` private fields, 4-space indentation)
```

Epics Story 1.1 new:

```text
**Then** it contains centralized NuGet package versions for all dependencies without inline `Version=` attributes
**Then** it enforces current Hexalith.Tenants conventions (file-scoped namespaces, K&R brace style where applicable, `_camelCase` private fields, 4-space indentation, warnings as errors)
```

Epics Story 1.3 old:

```text
So that every PR is validated automatically and tagged releases publish NuGet packages.
```

Epics Story 1.3 new:

```text
So that every PR is validated automatically and semantic-release publishes NuGet packages after qualifying merges to `main`.
```

Rationale:

Project context and current repository policy supersede inherited EventStore wording. Leaving the old wording invites style churn and release pipeline mistakes.

### Proposal I: Clean Up Minor Story Wording and Sequencing

Artifacts:

- `_bmad-output/planning-artifacts/epics.md`
- Relevant implementation story files

Story 7.1 old:

```text
**Then** the Aspire dashboard launches and the Aspire dashboard launches with Hexalith.Tenants ...
```

Story 7.1 new:

```text
**Then** the Aspire dashboard launches with Hexalith.Tenants ...
```

Story 9 sequencing:

```text
If Story 9.1 and Story 9.2 are not already complete, move Story 9.5 shared pagination/cursor utility work before Story 9.1 or fold the shared utility acceptance criteria into Story 9.1. If the work is already complete, record this as a retrospective note and avoid rework.
```

Rationale:

These are minor cleanup items, but fixing them reduces avoidable reviewer friction.

### Proposal J: Clarify UX Alignment Decisions

Artifacts:

- `_bmad-output/planning-artifacts/prd.md`
- `_bmad-output/planning-artifacts/ux-design-specification.md`
- Future Phase 2 UI backlog

WCAG old:

```text
PRD: Phase 2 Admin UI must address WCAG 2.1 AA.
UX: Target WCAG 2.2 AA.
```

WCAG proposed new:

```text
Phase 2 Admin UI accessibility baseline is WCAG 2.1 AA, with WCAG 2.2 AA as the design and implementation target where supported by the selected Fluent UI Blazor and FrontComposer stack.
```

User lookup old:

```text
User lookup / user search is referenced as a UI path without defining whether it is exact-ID lookup or broader directory-backed search.
```

User lookup proposed new:

```text
Phase 2 first slice supports exact user ID lookup using existing user-tenants query capability. Broader user search/discovery requires an external directory integration or a new backend requirement and is not implied by the current PRD.
```

Command-capable UI gating proposed new:

```text
Read-only UI surfaces may mature first. Command-capable UI stories remain blocked unless they explicitly declare available or approved-fallback dependencies for command lifecycle feedback, projection confirmation, consequence preview, audit evidence, accessibility, localization, and status reconciliation.
```

Rationale:

This keeps Phase 2 UI ambitious but prevents the backend MVP from inheriting hidden UI/search requirements.

## 6. Implementation Handoff

Scope classification: Moderate.

Reason:

- The change does not require a fundamental PRD or architecture replan.
- It does require backlog/story reorganization and at least one implementation-visible topic alignment decision.
- It should be coordinated between Product Owner, Developer, and Architect roles before further implementation handoff.

Recommended routing:

- Product Owner / Developer: update epics and implementation story artifacts, classify Story 2.4 as historical, and ensure 2.4A-2.4E are the future units of work.
- Developer: if topic normalization is approved, update source defaults, tests, docs, and story references from `system.tenants.events` to `tenants.events`.
- Architect: confirm `tenants.events` remains the canonical topic and decide whether projection naming should stay as current code (`GlobalAdministratorProjection`) or be normalized to architecture wording (`GlobalAdministratorsProjection`) in a separate change.
- UX / Product: confirm WCAG baseline/target and exact-ID user lookup scope for Phase 2.

Success criteria:

- No planning/story reference uses `GlobalAdministratorAggregate` or `GlobalAdministratorState` for the aggregate/state type.
- Command/event names such as `GlobalAdministratorSet` remain unchanged.
- Exactly one canonical DAPR tenant event topic is used in planning, source defaults, tests, and docs: `tenants.events`, unless Architect explicitly overrides the project context and updates every source-of-truth artifact together.
- Story 2.4 is not assignable as one future implementation story; reopened work uses 2.4A-2.4E.
- Story 6.1 covers aggregate/event isolation only; Story 6.2 covers projection isolation.
- Story 10.3B either remains blocked or names concrete EventStore APIs and submodule commit/version evidence.
- Epic 12 remains planning/dependency governance until converted into real Phase 2 UI implementation stories with explicit `blockedBy` fields.
- PRD and epics match current style/release policy: K&R for Tenants, semantic-release on merge to `main`, no MinVer assumption.
- Readiness rerun reports zero critical issues and no unaccepted major issues.

Validation plan after approved implementation:

- Run targeted text scans for stale terms:
  - `GlobalAdministratorAggregate`
  - `GlobalAdministratorState`
  - `system.tenants.events`
  - `Allman braces`
  - `tagged releases`
- Run focused tests if code topic defaults change:
  - Client registration/options tests
  - Event publication/degradation tests that assert topic names
  - Any sample or documentation validation available in the repo
- Rerun implementation readiness against corrected artifacts.

## 7. Approval

This proposal is ready for review.

Approved by Jerome on 2026-05-26:

```text
yes. topic is tenants.events
```

Implementation is authorized to apply the artifact corrections and align topic defaults, docs, and tests to `tenants.events`.

## 8. Implementation Outcome

Applied on 2026-05-26 after approval.

Artifacts and code updated:

- Planning artifacts: PRD style/release/accessibility wording, product brief topic wording, epics naming/topic/story-readiness corrections, UX accessibility and exact user ID lookup clarification.
- Story artifacts: corrected stale topic, aggregate naming, K&R style, DAPR version, and semantic-release references in relevant implementation story files.
- Tenants source/docs/tests: default client topic, topic assertions, event contract reference, demo documentation, and graceful-degradation topic check now use `tenants.events`.
- EventStore submodule: `AggregateIdentity.PubSubTopic` and `NamingConventionEngine.GetPubSubTopic` now publish platform tenant `system` events to `{domain}.events`, preserving `{tenant}.{domain}.events` for non-system tenants. Dead letter derivation now follows the resolved pub/sub topic, so Tenants uses `deadletter.tenants.events`.

Validation completed:

- Stale-term scans for `system.tenants.events`, `GlobalAdministratorAggregate`, `GlobalAdministratorState`, `Allman braces`, `tagged releases`, and `DAPR 1.17.3` across corrected Tenants paths returned no actionable hits.
- `dotnet test .\tests\Hexalith.Tenants.Client.Tests\Hexalith.Tenants.Client.Tests.csproj --configuration Release --filter FullyQualifiedName~TenantServiceCollectionExtensionsTests --no-restore`
- `dotnet test .\tests\Hexalith.Tenants.IntegrationTests\Hexalith.Tenants.IntegrationTests.csproj --configuration Release --filter FullyQualifiedName~GracefulDegradationTests --no-restore`
- `dotnet test .\tests\Hexalith.Tenants.IntegrationTests\Hexalith.Tenants.IntegrationTests.csproj --configuration Release --filter FullyQualifiedName~DaprEndToEndTests --no-restore`
- `dotnet test .\Hexalith.EventStore\tests\Hexalith.EventStore.Contracts.Tests\Hexalith.EventStore.Contracts.Tests.csproj --configuration Release --filter FullyQualifiedName~AggregateIdentityTests --no-restore`
- `dotnet test .\Hexalith.EventStore\tests\Hexalith.EventStore.Client.Tests\Hexalith.EventStore.Client.Tests.csproj --configuration Release --filter FullyQualifiedName~NamingConventionEngineTests --no-restore`
- `dotnet test .\Hexalith.EventStore\tests\Hexalith.EventStore.Server.Tests\Hexalith.EventStore.Server.Tests.csproj --configuration Release --filter FullyQualifiedName~EventPublisherOptionsTests --no-restore`

One initial parallel EventStore client test attempt failed with a transient file lock while other tests were building the same project. The same test passed when rerun sequentially.
