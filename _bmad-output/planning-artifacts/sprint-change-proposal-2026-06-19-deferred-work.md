# Sprint Change Proposal - Deferred Work Triage

Date: 2026-06-19
Workflow: bmad-correct-course
Mode: Batch
Status: Approved - routed for implementation
Owner: Administrator
Trigger: `_bmad-output/implementation-artifacts/deferred-work.md`
Approval: Administrator approved on 2026-06-19.

## 1. Issue Summary

`deferred-work.md` has become the active holding area for review-found work from the June 7 tenant query routing fix and the June 18 FrontComposer/Fluent conformance series. The file now mixes four different kinds of work:

- Tenants-owned hardening that should become implementation stories.
- Cross-submodule handoffs to FrontComposer or EventStore owners.
- Deployment/documentation cleanup.
- Stale or already-resolved review records.

This creates planning risk: future agents may either ignore real hardening items because they are buried in a mixed deferred list, or apply submodule-owned fixes inside the Tenants repository, violating the domain boundary and submodule policy.

The proposal below converts the deferred items into explicit backlog changes and handoffs without reopening completed Epics 1-5 or the completed June 18 conformance stories.

## 2. Change Analysis Checklist

| Item | Status | Finding |
| --- | --- | --- |
| 1.1 Triggering story | [x] | Multiple triggers: Story 3.5 tenant query REST routing, page-layout/page-header conformance sweep, structural/style conformance sweep, and shell/Admin.UI conformance audit. |
| 1.2 Core problem | [x] | Deferred work is not classified by owner or implementation route; some entries are stale, some need Tenants stories, and some need FrontComposer/EventStore handoff. |
| 1.3 Evidence | [x] | Evidence from `deferred-work.md`, `sprint-status.yaml`, the relevant implementation artifacts, current source scans, and current submodule source. |
| 2.1 Current epic impact | [x] | Epics 1, 2, 4, and 5 are done. Epic 3 has done stories plus optional retro. No epic feature objective is invalidated. |
| 2.2 Epic-level changes | [x] | Add cross-cutting maintenance stories instead of changing feature epics. |
| 2.3 Future epic impact | [N/A] | No future feature epic becomes obsolete. |
| 2.4 New epic need | [N/A] | A new epic is not justified; this is cross-cutting hardening and owner handoff. |
| 2.5 Priority/order | [x] | Prioritize Tenants runtime correctness before UI guard polish; route submodule ownership separately. |
| 3.1 PRD impact | [N/A] | PRD behavior remains valid. The changes enforce existing freshness, support-safety, accessibility, and Fluent governance commitments. |
| 3.2 Architecture impact | [!] | D6 freshness needs a concrete implementation decision because the current read-model store exposes value + ETag only, not persisted projection age. |
| 3.3 UI/UX impact | [!] | Page landmark/accessibility work is FrontComposer-owned. Tenants UI guard hardening needs re-approval because it changes frozen Section 5.3 governance behavior. |
| 3.4 Other artifacts | [!] | `sprint-status.yaml`, `deferred-work.md`, `docs/cross-aggregate-timing.md`, and DAPR component YAML need updates after approval. |
| 4.1 Direct adjustment | [x] | Viable. Add focused stories and handoffs. |
| 4.2 Rollback | [N/A] | No rollback simplifies the work. Completed behavior remains valuable. |
| 4.3 MVP review | [N/A] | MVP scope does not change. |
| 4.4 Recommended path | [x] | Direct Adjustment with Moderate scope. |
| 5.1 Issue summary | [x] | Covered here. |
| 5.2 Artifact adjustments | [x] | Specific story/status/deferred-work/documentation changes proposed below. |
| 5.3 Path rationale | [x] | Direct adjustment avoids re-opening completed stories and respects submodule ownership. |
| 5.4 MVP impact | [x] | None. |
| 5.5 Handoff plan | [x] | Developer agent for Tenants stories; FrontComposer/EventStore owners for submodule work; PO/DEV for backlog sync. |
| 6.1 Checklist completion | [x] | All applicable sections completed. |
| 6.2 Proposal accuracy | [x] | Current source was checked for drift before routing items. |
| 6.3 User approval | [x] | Administrator approved the proposal on 2026-06-19. |
| 6.4 Sprint status update | [x] | Three cross-cutting stories were added to `sprint-status.yaml` as `ready-for-dev`. |
| 6.5 Handoff confirmation | [x] | Tenants stories and FrontComposer/EventStore owner handoffs were routed in implementation artifacts. |

## 3. Impact Analysis

### Epic Impact

No functional epic needs redefinition. The delivered stories remain valid:

- Story 3.5 correctly moved Tenants UI reads to REST-backed domain endpoints and retired the Tenants projection actor path in this repository.
- The June 17-18 Fluent/FrontComposer conformance stories correctly moved Tenants UI toward FrontComposer/Fluent v5 governance.
- Epic 4 and Epic 5 implementation state is newer than some historical planning docs; do not treat older blocked/deferred notes as current unless a current story cites them.

The change should be tracked as cross-cutting maintenance, not as a feature-epic rewrite.

### Story Impact

Create three Tenants-owned cross-cutting stories:

1. `cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening`
2. `cc-2026-06-19-domain-ui-governance-and-accessibility-hardening`
3. `cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup`

Create two owner handoffs, not Tenants implementation stories:

1. FrontComposer shell/page-header accessibility and fail-open contract hardening.
2. EventStore/Admin.UI carve-out and non-semantic-clickable remediation already identified by the shell/Admin.UI audit, plus confirmation that the old retired actor-routing item is now stale.

### Artifact Conflicts

- `deferred-work.md` currently says the EventStore Admin service still assigns `ProjectionActorType: TenantProjectionRouting.ActorTypeName`. Current source no longer shows that assignment in `references/Hexalith.EventStore/src/Hexalith.EventStore.Admin.Server/Services/DaprTenantQueryService.cs`; the entry is stale and should be marked resolved/stale after approval.
- `docs/cross-aggregate-timing.md` correctly states that dead-lettering is application-level, but the Mermaid sequence still shows subscriber failure flowing to `deadletter.tenants.events` after retry/dead-letter policy. That diagram should be clarified.
- `deploy/dapr/pubsub.yaml` still contains `publishingScopes: "sample="` while comments say EventStore publishes and sample subscribes. DAPR topic scoping syntax grants app-id/topic pairs, so this should be verified and corrected to match the intended publisher/subscriber contract.

### Technical Impact

- D6 freshness cannot be made real from the current `IReadModelStore` contract alone: `ReadModelEntry<TValue>` exposes only `Value` and `ETag`. The current `TenantQueryResult.FromPayload` stamps `ServedAt = UtcNow`, which measures response time, not projection age. A real `aging`/`stale` signal needs either shared read-model metadata support in `Hexalith.EventStore` or Tenants-owned persisted projection metadata in each read model.
- ETag handling is mostly safe in today's topology because the server emits strong ETags and DAPR state-store ETags are simple opaque strings. Hardening is still worthwhile because `NormalizeIfNoneMatch` can throw `ArgumentException` and weak/proxy ETags are not mapped through the gateway's safe degraded path.
- UI governance hardening changes the approved Section 5.3 guard behavior. Because Administrator explicitly deferred those guard changes during review, they need this proposal approval before implementation.

## 4. Recommended Approach

Recommended path: **Direct Adjustment**.

Scope classification: **Moderate**.

Rationale:

- The deferred work is real but bounded. It does not require changing PRD scope or feature epics.
- Completed stories should stay complete. Reopening them would blur evidence and commit boundaries.
- Submodule-owned issues must be handed off, not fixed opportunistically in Tenants.
- The highest-risk Tenants-owned item is runtime truthfulness around freshness and ETags; address it before optional UI polish.

Rejected alternatives:

- **Potential rollback:** not useful. The completed REST query routing and conformance sweeps are directionally correct.
- **MVP review:** not needed. The deferred items enforce existing MVP and NFR commitments rather than changing product scope.
- **Single mega-story:** rejected. Query correctness, UI governance, and deployment/docs have different owners, test lanes, and blast radii.

## 5. Detailed Change Proposals

### 5.1 Sprint Status

Old:

```yaml
development_status:
  cc-frontcomposer-shell-and-adminui-fluent-conformance-audit: done
```

New, after approval:

```yaml
development_status:
  # --- Cross-cutting Correct Course 2026-06-19 ---
  cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening: ready-for-dev
  cc-2026-06-19-domain-ui-governance-and-accessibility-hardening: ready-for-dev
  cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup: ready-for-dev
```

Rationale: the June 19 work is not a new product feature epic; it is cross-cutting hardening.

### 5.2 Story: Tenant Query Freshness, ETag, and Coverage Hardening

Story: `cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening`

Section: Intent

OLD:

```markdown
Deferred work exists only as review bullets under Story 3.5 and deferred-work.md.
```

NEW:

```markdown
Harden the REST-backed Tenants query path after Story 3.5 by making freshness truthful,
making ETag behavior explicit and robust, restoring state-store reconstruction coverage
on the production REST/handler path, and adding support-safety assertions for live
gateway error mapping.
```

Acceptance criteria:

1. Given a successful tenant query response, when freshness metadata is emitted, then `ServedAt` is not used as a proxy for projection age unless it is backed by persisted projection metadata; otherwise the response reports freshness as `unknown` or uses an explicitly documented direct-read `current` rule.
2. Given D6 freshness states, when a real persisted projection timestamp/version is available, then `ResolveFreshness` can produce `current`, `aging`, `stale`, and `unknown` according to configurable thresholds with tests for each state.
3. Given the current `IReadModelStore` only exposes `Value` + `ETag`, when implementation needs generic read-model metadata, then the developer either consumes a shared EventStore capability or records an EventStore handoff instead of adding generic persistence scaffolding to Tenants.
4. Given a read-model ETag is null or whitespace, when a REST query succeeds, then the response behavior is explicit and tested: 200 with no ETag and no 304 support, and UI freshness fails closed unless a real projection marker exists.
5. Given `If-None-Match` contains weak tags, `*`, escaped strong tags, or unsupported multi-tag input, when the server/client normalizes it, then unsupported input maps to a safe non-leaking query state and supported strong tags compare consistently with the emitted strong ETag.
6. Given a gateway error response includes `correlationId`, `reasonCode`, raw payload text, stack traces, tokens, or ETags, when `TenantQueryGateway` maps it to UI snapshots, then rendered/user-facing copy excludes those values on the live populated-correlation path.
7. Given the retired actor path was removed, when integration coverage runs, then a REST/handler equivalent proves persisted read-model state survives a fresh service instance or handler/store boundary. Do not restore the retired projection actor test.
8. Given Tier 2 and Tier 3 evidence, when full suites remain blocked, then the story records exact current blockers; if the prior pubsub/health blockers are now resolved, `deferred-work.md` is updated to remove stale blocker text.

Affected files:

- `src/Hexalith.Tenants/Queries/TenantQueryResult.cs`
- `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantsQueryApiClient.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`
- `src/Hexalith.Tenants.Server/Projections/*ReadModel.cs`
- `tests/Hexalith.Tenants.Server.Tests/Queries/*`
- `tests/Hexalith.Tenants.IntegrationTests/StatelessRestartTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/*`

Rationale: this is the only deferred bucket that can affect user-visible truthfulness under normal operation.

### 5.3 Story: Domain UI Governance and Accessibility Hardening

Story: `cc-2026-06-19-domain-ui-governance-and-accessibility-hardening`

Section: Intent

OLD:

```markdown
Section 5.3 guard-hardening candidates are deferred in deferred-work.md because the
approved regexes were frozen and must not be changed mid-review.
```

NEW:

```markdown
Re-approve and harden the Tenants UI governance guards that were intentionally deferred
from the structural/style conformance sweep, then add small missing component tests for
current FluentStack migrations and cosmetic route-heading fallbacks.
```

Acceptance criteria:

1. Given component CSS contains compact non-zero spacing such as `margin:0.5rem` or `padding:0.5rem`, when `Domain_ui_component_css_does_not_own_layout_spacing_or_typography` scans it, then it is flagged unless covered by an approved `fc-css-exception`.
2. Given inline raw-element `style=` contains layout/spacing/measure declarations beyond the original narrow set, including `margin`, `padding`, `width`, `inline-size`, `justify-content`, or `align-items`, when governance scans `.razor` source, then it is flagged unless the story records an explicit exception.
3. Given `<div>`/`<span>` budget counting, when comments contain tag-like text, then comments are excluded before counting.
4. Given `fc-css-exception` markers, when a marker exempts a rule, then the story either preserves rule-level scoping with a documented rationale or introduces declaration-level scoping with updated tests.
5. Given `:focus-visible` is an approved exemption today, when this story reviews it, then the final behavior is explicitly approved: retain blanket exemption or narrow it with rationale.
6. Given `RemoveForcedColorsMediaBlocks` strips forced-colors blocks, when CSS contains braces in comments or strings, then the helper remains stable or is replaced with a safer parser.
7. Given `MemberAccessReview` opens change-role or remove-member regions, when bUnit renders the active region, then the `aria-controls` source button points to a rendered target `id` after the FluentStack migration.
8. Given `TenantAuditPage` receives a blank or whitespace `TenantId`, when the page header renders, then it uses a localized fallback rather than a dangling `Audit - ` heading. This is cosmetic, not a crash fix.

Affected files:

- `tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor`
- Tenants `.resx` files if an audit fallback string is added

Rationale: these are guard and test hardening items, not feature behavior changes. They need explicit approval because they alter a human-approved frozen governance contract.

### 5.4 Story: DAPR Deployment Docs and Deferred Record Cleanup

Story: `cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup`

Section: Intent

OLD:

```markdown
Deployment and review-record cleanup items are scattered through deferred-work.md.
```

NEW:

```markdown
Make deployment pub/sub scope documentation and deferred-work records truthful after the
June 18 DAPR dead-letter correction, and close stale entries that current source no
longer supports.
```

Acceptance criteria:

1. Given `deploy/dapr/pubsub.yaml` says EventStore publishes and sample subscribes, when topic scopes are configured, then `publishingScopes` and `subscriptionScopes` match that intent or are omitted with a documented reason. The current suspicious `publishingScopes: "sample="` must be verified against DAPR topic scoping and corrected if inert or misleading.
2. Given local and production DAPR pub/sub components are compared, when topic-scope policy differs, then the difference is intentional and documented.
3. Given `docs/cross-aggregate-timing.md` shows the propagation sequence, when subscriber failure is diagrammed, then it does not imply DAPR component dead-lettering to `deadletter.tenants.events`. The diagram should distinguish subscriber redelivery from EventStore's application-level dead-letter publisher.
4. Given `CrossAggregateTimingDocumentationTests` guards the guide, when docs/YAML change, then tests assert the truthful application-level dead-letter wording and topic-scope contract.
5. Given `deferred-work.md` still says EventStore Admin routes tenant queries through `TenantProjectionRouting.ActorTypeName`, when current EventStore source no longer does that, then the entry is marked stale/resolved with the verification command and date instead of being carried as open work.
6. Given `deferred-work.md` has contradictory June 18 review-record wording, when the cleanup runs, then entries are normalized to a current, non-contradictory status with source artifact references.

Affected files:

- `deploy/dapr/pubsub.yaml`
- `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml`
- `docs/cross-aggregate-timing.md`
- `tests/Hexalith.Tenants.Server.Tests/Documentation/CrossAggregateTimingDocumentationTests.cs`
- `_bmad-output/implementation-artifacts/deferred-work.md`

Rationale: this keeps deployment evidence and future-agent context accurate without reopening UI conformance stories.

### 5.5 FrontComposer Owner Handoff

Handoff: `frontcomposer-2026-06-19-page-header-landmarks-and-contract-hardening`

OLD:

```markdown
Tenants pages use FcPageHeader, and deferred-work records the duplicate banner,
orphaned aria-labelledby, unnamed shell main, and FcPageHeader fail-open contract.
```

NEW:

```markdown
Route to the FrontComposer shell/UX owner. Do not patch this in Tenants.
```

Requested outcomes:

1. `FrontComposerShell` exposes a shell content landmark contract that can be a native `<main>` or an equivalent role with an accessible-name parameter; Tenants page headings can name the shell main landmark without orphaned page-level `aria-labelledby`.
2. `FcPageHeader` no longer creates a competing global `banner` landmark on every route page. Options include non-landmark wrapper markup or scoping inside a sectioning element, decided by the FrontComposer UX owner.
3. `FcPageHeader` handles blank `Heading` fail-safely or documents a strict consumer contract with analyzable/tested guardrails. Current Tenants callers have consumer fallbacks, but the shared component remains brittle.
4. `FocusHeadingAsync()` either ensures the heading is focusable when used as a focus target or fails diagnostically when `HeadingTabIndex` is omitted.

Files to inspect in FrontComposer:

- `references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageHeader.razor`
- `references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageHeader.razor.cs`
- `references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor`
- FrontComposer Shell component/a11y tests

Rationale: this is a shared shell contract and should be solved once for all domains.

### 5.6 EventStore Owner Handoff

Handoff: `eventstore-2026-06-19-admin-ui-and-query-record-followup`

OLD:

```markdown
deferred-work says EventStore.Admin.Server still routes tenant queries through the retired actor.
```

NEW:

```markdown
Mark that specific actor-routing entry stale/resolved based on current source. Keep the
EventStore/Admin.UI Fluent audit handoffs from the June 18 audit as EventStore-owned work.
```

Requested outcomes:

1. Confirm `Hexalith.EventStore.Admin.Server/Services/DaprTenantQueryService.cs` no longer sets `ProjectionActorType` to a retired Tenants actor. If EventStore tests still encode the retired actor assumption elsewhere, update them under EventStore ownership.
2. Continue the already-recorded Admin.UI audit remediation handoffs: non-semantic clickable `Index.razor`, clickable-span class, `ActivityChart` a11y proof, `StorageTreemap` semantics/docs, and optional parity guards.

Rationale: the retired actor path should not stay as open Tenants deferred work if current EventStore source already removed it.

## 6. Implementation Handoff

Scope: **Moderate**.

Primary assignees:

- Developer agent: implement Tenants stories after approval.
- PO/Developer: update `sprint-status.yaml` and create story artifacts.
- FrontComposer owner: handle shell/page-header contract hardening.
- EventStore owner: handle Admin.UI and any remaining EventStore-side query assumptions.

Suggested order:

1. Create and implement `cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening`.
2. Create and implement `cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup`.
3. Create and implement `cc-2026-06-19-domain-ui-governance-and-accessibility-hardening`.
4. Route FrontComposer and EventStore handoffs.
5. Update `deferred-work.md` so only genuinely open deferred items remain.

Success criteria:

- `deferred-work.md` no longer mixes stale, owned, and handoff work without status.
- Tenants query freshness and ETag behavior are explicitly safe and tested.
- DAPR docs/YAML reflect the actual app-level dead-letter and topic-scope contract.
- UI governance guard changes are re-approved and covered by tests.
- FrontComposer/EventStore shared issues are routed to owners without submodule edits from this repository.

## 7. Approval Decision

Approved by Administrator on 2026-06-19.

Implementation routing completed:

- Created three Tenants cross-cutting story artifacts under `_bmad-output/implementation-artifacts/`.
- Updated `sprint-status.yaml` with the three approved `ready-for-dev` story keys.
- Reorganized `deferred-work.md` so open items point to either a Tenants story, a FrontComposer owner handoff, an EventStore owner handoff, or a stale/resolved record.
- Kept source code, submodule files, deployment YAML, and docs unchanged in this approval step.

## 8. Workflow Execution Log

- Issue addressed: deferred review work was unclassified across Tenants-owned hardening, submodule handoffs, deployment/docs cleanup, and stale records.
- Change scope: Moderate.
- Artifacts modified: this proposal, three new story artifacts, `sprint-status.yaml`, and `deferred-work.md`.
- Routed to: Product Owner / Developer agents for Tenants story execution; FrontComposer owner for shell/page-header contract work; EventStore owner for Admin.UI and any remaining EventStore-side query assumptions.
