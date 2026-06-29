---
title: "Sprint Change Proposal - Epic 3 Projection Actor and FR15 Drift Audit"
date: 2026-06-29T16:58:15+02:00
status: approved-applied
trigger: "Epic 3 retrospective action item: audit Epic 3 planning and evidence docs for retired projection-actor and FR15 hard-delete drift."
mode: batch
scope: minor-documentation-correction
---

# Sprint Change Proposal: Epic 3 Projection Actor and FR15 Drift Audit

## 1. Issue Summary

The trigger is action item 6 from `_bmad-output/implementation-artifacts/epic-3-retro-2026-06-29.md`: audit Epic 3 planning and evidence docs for stale retired projection-actor language and FR15 hard-delete drift.

Two corrections are already canonical:

- Story 3.5 retired the failed `TenantsProjectionActor` / generic query-gateway read path. Tenants UI reads now use Tenants REST endpoints (`GET /api/tenants*`) through the server-side BFF and in-process query handlers, preserving ETag/freshness behavior.
- FR15 disable/enable is reversible lifecycle soft-delete / availability control, not hard destructive tenant deletion. Hard tenant deletion remains out of scope for Tenants UI and is reserved for future administrators-only CLI tooling.

The audit found the core Epic 3 planning direction is mostly aligned, but several implementation evidence and historical docs still contain contradictory or stale wording that can mislead future story creation, review, or documentation updates.

## 2. Change Analysis Checklist

- [x] 1.1 Trigger identified: Epic 3 retro action item 6, with Story 3.5 and Story 3.2 as the current correction evidence.
- [x] 1.2 Core problem defined: documentation drift after two course corrections, not a product scope or code behavior change.
- [x] 1.3 Evidence collected: affected artifacts and line references are listed below.
- [x] 2.1 Current epic impact assessed: Epic 3 remains done; no feature reopening is needed.
- [x] 2.2 Epic-level changes assessed: no new epic or story required; apply direct documentation corrections.
- [x] 2.3 Future epic impact assessed: Epic 4, Epic 5, and future correction/high-impact flows consume these docs, so stale wording has downstream risk.
- [x] 2.4 Obsolescence/new epic assessment: no planned epic is invalidated.
- [x] 2.5 Priority/order assessment: no sequencing change required.
- [x] 3.1 PRD conflicts checked: PRD main text is aligned; PRD addendum still names the caching projection actor as freshness source.
- [x] 3.2 Architecture conflicts checked: architecture is aligned and explicitly says the projection actor is retired.
- [x] 3.3 UX conflicts checked: current UX spine is aligned; older docs specs still name `CachingProjectionActor`.
- [x] 3.4 Other artifacts checked: Story 3.1, Story 3.3, test summary, and published docs need corrections.
- [x] 4.1 Direct adjustment evaluated: viable, low-risk documentation-only correction.
- [N/A] 4.2 Rollback evaluated: no implementation rollback is useful.
- [N/A] 4.3 MVP review evaluated: MVP/product scope is unchanged.
- [x] 4.4 Recommended path selected: direct documentation adjustment.
- [x] 5.1 Issue summary created.
- [x] 5.2 Epic impact and artifact adjustment needs documented.
- [x] 5.3 Recommended path documented.
- [x] 5.4 PRD/MVP impact documented: no MVP impact.
- [x] 5.5 Handoff plan established.
- [x] 6.1 Checklist reviewed.
- [x] 6.2 Proposal reviewed for consistency.
- [x] 6.3 User approval received on 2026-06-29.
- [x] 6.4 Sprint status updated after the accepted doc edits landed; `epic-3-retro-2026-06-29-doc-drift` is marked done.
- [x] 6.5 Next steps defined.

## 3. Impact Analysis

### Epic Impact

Epic 3 remains complete. The audit does not reopen Stories 3.1 through 3.5 and does not change FR15, FR16, or FR17 scope.

The impact is interpretive: stale text can cause a future agent to route reads through the retired actor path or treat FR15 as still blocked or hard-delete-related. That creates risk in later command, audit, and correction work.

### Story Impact

- Story 3.1 evidence needs a historical qualifier around the pre-correction governance-block wording.
- Story 3.3 evidence needs one stale FR15 sentence corrected; the same file already contains the correct later statement.
- Story 3.5 evidence is directionally correct; the test-summary addendum should match the actual guard-test scan roots.
- No implementation story acceptance criteria need to change.

### Artifact Conflicts

Current aligned artifacts:

- `_bmad-output/planning-artifacts/epics.md` lines 147 and 1095-1098 correctly state FR15 is reversible lifecycle soft-delete / availability control and hard deletion is out of scope.
- `_bmad-output/implementation-artifacts/3-2-disable-or-enable-tenant-with-high-impact-confirmation.md` consistently scopes Story 3.2 to reversible lifecycle control and excludes hard delete.
- `_bmad-output/implementation-artifacts/3-5-tenant-query-gateway-rest-routing.md` correctly records the retired actor failure and REST-backed replacement.
- `_bmad-output/planning-artifacts/architecture.md` lines 335-340 correctly prohibit `POST /api/v1/queries` / generic query routing for Tenants UI reads.

Drift requiring edits:

- `_bmad-output/implementation-artifacts/3-1-tenant-lifecycle-command-availability-and-blocked-state-guardrail.md` line 79 still says Story 3.2 remains gated.
- `_bmad-output/implementation-artifacts/3-3-set-tenant-configuration-key-value-with-consequence-preview.md` line 84 still says FR15 remains categorically blocked.
- `_bmad-output/implementation-artifacts/tests/test-summary.md` line 235 understates the projection-routing guard as UI + Contracts only, while the actual test scans `src/Hexalith.Tenants.Contracts`, `src/Hexalith.Tenants.UI`, and `src/Hexalith.Tenants`.
- `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md` line 65 still says freshness comes via the caching projection actor.
- `docs/tenants-ui-operations-shell-spec.md`, `docs/tenants-ui-truth-state-and-action-availability-spec.md`, `docs/tenants-ui-remove-user-from-tenant-journey-spec.md`, and `docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md` still name `CachingProjectionActor`.
- `docs/event-contract-reference.md` line 784 still says query endpoints dispatch to the projection actor.
- `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/reconcile-audit-recovery.md` lines 70 and 72 are historical process notes that repeat the old projection-actor freshness source; either update or mark superseded if the file is still treated as active guidance.

### Technical Impact

No code or tests are required to change. The existing source guard confirms no retired query-routing terms in the relevant product source roots. The proposal is documentation-only, plus a sprint-status action-item status update after approval.

## 4. Recommended Approach

Use direct adjustment.

Rationale:

- The current product and architecture decisions are settled and already implemented.
- The needed corrections are localized text edits.
- No rollback, replan, or scope reduction is justified.
- Keeping the old text creates avoidable risk for future agents because the stale wording appears in story evidence and published docs.

Effort: Low.

Risk: Low. The main risk is accidentally rewriting historical notes as if they were never true. Preserve historical sequence where useful, but make current status explicit.

MVP impact: None.

## 5. Detailed Change Proposals

### Story 3.1 Evidence

Artifact: `_bmad-output/implementation-artifacts/3-1-tenant-lifecycle-command-availability-and-blocked-state-guardrail.md`

Section: Dev Notes / Story Source And Epic Context

OLD:

```markdown
- Implementation readiness explicitly states that platform-wide destructive actions remain categorically blocked pending governance/contract confirmation; Story 3.1 is buildable because it renders the readiness/blocked state, while Story 3.2 remains gated. [Source: `_bmad-output/planning-artifacts/implementation-readiness-report-2026-06-05-v2.md#External Build-Start Gates`]
```

NEW:

```markdown
- Historical note: at Story 3.1 creation time, implementation readiness treated platform-wide destructive actions as categorically blocked pending governance/contract confirmation, so Story 3.1 rendered the honest readiness/blocked state while Story 3.2 remained gated. This was later superseded by the approved 2026-06-06 FR15 correction: disable/enable is reversible lifecycle soft-delete / availability control, not hard destructive tenant deletion. [Source: `_bmad-output/planning-artifacts/implementation-readiness-report-2026-06-05-v2.md#External Build-Start Gates`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-06.md`]
```

Optional nearby clarification for line 87:

```markdown
Lifecycle enable remained UI-blocked by governance in Story 3.1 only; Story 3.2 later enabled the approved reversible lifecycle flow after the 2026-06-06 correction.
```

Rationale: Preserve the historical reason for Story 3.1 without implying the current Epic 3 state is still gated.

### Story 3.3 Evidence

Artifact: `_bmad-output/implementation-artifacts/3-3-set-tenant-configuration-key-value-with-consequence-preview.md`

Section: Dev Notes / Story Source And Epic Context

OLD:

```markdown
- Tenant-scoped destructive/configuration flows are fallback-eligible. The Product/UX-approved `FC-CNS` inline consequence fallback applies to FR16/FR17; FR15 lifecycle disable/enable remains categorically blocked and should not be used as a reason to block Story 3.3. [Source: `_bmad-output/planning-artifacts/fallback-approval-record-2026-06-03.md#The three approved fallbacks`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-03.md#Section 2 - Impact Analysis`; `_bmad-output/implementation-artifacts/3-2-disable-or-enable-tenant-with-high-impact-confirmation.md`]
```

NEW:

```markdown
- Tenant-scoped destructive/configuration flows are fallback-eligible. The Product/UX-approved `FC-CNS` inline consequence fallback applies to FR16/FR17. Earlier planning treated FR15 lifecycle disable/enable as categorically blocked, but the approved 2026-06-06 correction later reclassified FR15 as reversible lifecycle soft-delete / availability control eligible under approved fallbacks; that correction does not change Story 3.3's configuration-specific safeguards or imply hard-delete UI scope. [Source: `_bmad-output/planning-artifacts/fallback-approval-record-2026-06-03.md#The three approved fallbacks`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-06.md`; `_bmad-output/implementation-artifacts/3-2-disable-or-enable-tenant-with-high-impact-confirmation.md`]
```

Rationale: Remove the direct contradiction with the file's later "Previous Story Intelligence" section.

### Story 3.5 Test Evidence Summary

Artifact: `_bmad-output/implementation-artifacts/tests/test-summary.md`

Section: Story 3.5 Evidence Addendum

OLD:

```markdown
- [x] `tests/Hexalith.Tenants.Contracts.Tests/SolutionStructureTests.cs` - Adds a guard that UI and Contracts source no longer reference tenant projection-actor routing symbols.
```

NEW:

```markdown
- [x] `tests/Hexalith.Tenants.Contracts.Tests/SolutionStructureTests.cs` - Adds a guard that `src/Hexalith.Tenants.Contracts`, `src/Hexalith.Tenants.UI`, and `src/Hexalith.Tenants` no longer reference tenant projection-actor routing symbols.
```

Rationale: Match the actual guard test roots in `Tenant_ui_contracts_and_host_do_not_use_projection_actor_query_routing`.

### PRD Addendum Freshness Primitive

Artifact: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md`

Section: D. Mechanism decisions, rejection/NoOp matrix and rationale

OLD:

```markdown
- **Freshness primitive:** conditional requests (`If-None-Match` -> `304`) via the caching projection actor; the Truth State Badge derives `current/refreshing/aging/stale/unknown` from this. Numeric thresholds deferred to implementation.
```

NEW:

```markdown
- **Freshness primitive:** server-side conditional requests (`If-None-Match` -> `304`) served by `TenantsQueryController` over the Tenants REST read endpoints, using read-model ETag/freshness metadata surfaced by the in-process query handlers. The Truth State Badge derives `current/refreshing/aging/stale/unknown` from this. Numeric thresholds deferred to implementation.
```

Rationale: Align the PRD addendum with Story 3.5 and architecture D6.

### UI Docs Freshness Source

Artifacts:

- `docs/tenants-ui-operations-shell-spec.md`
- `docs/tenants-ui-truth-state-and-action-availability-spec.md`
- `docs/tenants-ui-remove-user-from-tenant-journey-spec.md`
- `docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md`

Replace stale `CachingProjectionActor` freshness-source wording with:

```markdown
ETag `If-None-Match` -> `304 Not Modified` is the freshness primitive served by the Tenants REST read endpoints through `TenantsQueryController` and in-process query handlers, using read-model ETag/freshness metadata.
```

For `docs/tenants-ui-truth-state-and-action-availability-spec.md`, also update "Two fixed claims" so the fixed claim is the ETag/304 REST read-model evidence contract, not the old actor implementation.

Rationale: These docs are still useful UX/spec references, but their freshness implementation source is obsolete.

### Event Contract Query Reference

Artifact: `docs/event-contract-reference.md`

Section: Query API Reference

OLD:

```markdown
Tenant query endpoints are protected REST adapters over EventStore `SubmitQuery`. Controllers validate route/query input, derive the authenticated user from JWT `sub`, validate signed opaque cursors, then dispatch to the projection actor. Query authorization and row filtering are handled by the projection/query path.
```

NEW:

```markdown
Tenant query endpoints are protected REST read adapters over in-process domain query handlers. Controllers validate route/query input, derive the authenticated user from JWT `sub`, validate signed opaque cursors, then dispatch through the Tenants query dispatcher to the relevant `IDomainQueryHandler`. Query authorization and row filtering are handled by the query handler path.
```

Rationale: Remove the retired projection-actor implication from the public contract reference.

### Historical PRD Reconciliation Note

Artifact: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/reconcile-audit-recovery.md`

Sections: lines 70 and 72

Proposal: Either update the freshness-source phrase to match the PRD addendum edit above, or add a short supersession note:

```markdown
Superseded by Story 3.5: ETag -> 304 freshness is now served by Tenants REST read endpoints through `TenantsQueryController` and in-process query handlers, not by `CachingProjectionActor`.
```

Rationale: This is a process artifact, but the current stale wording can be copied into active docs.

### Sprint Status Action Item

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

After the approved edits above are applied, update:

```yaml
id: epic-3-retro-2026-06-29-doc-drift
status: done
```

Rationale: The action item is currently open and should only be closed once the actual drift corrections are applied, not merely because this audit proposal exists.

## 6. Implementation Handoff

Scope classification: Minor documentation correction.

Handoff recipients:

- Paige (Technical Writer): apply wording corrections to planning, docs, and evidence artifacts.
- Winston (System Architect): verify the new query-routing wording preserves the architecture boundary.
- Amelia (Developer): optionally run the source guard test if any wording change touches guard-test evidence.

Success criteria:

- No active planning/evidence/docs artifact implies Tenants UI reads route through `TenantsProjectionActor`, `TenantProjectionRouting`, `ProjectionActorType`, `CachingProjectionActor`, or the EventStore generic query gateway.
- No active Epic 3 evidence artifact implies FR15 is still categorically blocked or implements hard destructive tenant deletion.
- Historical notes remain clear where the old status was true at the time.
- `sprint-status.yaml` marks `epic-3-retro-2026-06-29-doc-drift` done only after the edits land.

## 7. Approval

Approved by Administrator on 2026-06-29 and applied as a minor documentation correction.

Applied artifacts:

- `_bmad-output/implementation-artifacts/3-1-tenant-lifecycle-command-availability-and-blocked-state-guardrail.md`
- `_bmad-output/implementation-artifacts/3-3-set-tenant-configuration-key-value-with-consequence-preview.md`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md`
- `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/reconcile-audit-recovery.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `docs/tenants-ui-operations-shell-spec.md`
- `docs/tenants-ui-truth-state-and-action-availability-spec.md`
- `docs/tenants-ui-remove-user-from-tenant-journey-spec.md`
- `docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md`
- `docs/event-contract-reference.md`

Quoted OLD blocks in this proposal remain historical audit evidence, not current implementation guidance.
