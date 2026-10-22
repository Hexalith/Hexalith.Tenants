---
title: 'Qualify five deferred command status documentation records'
type: 'refactor'
created: '2026-10-22'
status: 'in-progress'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** DW-507, DW-509, DW-510, DW-513 and DW-518 describe confirmed documentation gaps in EventStore's command status and replay contract. Consumers encounter obsolete correlation-key status routes, missing recovery fields and incomplete descriptions of rejection outcomes.

**Approach:** Correct the owning EventStore reference and five related guides against current response models, controller lookup rules and actor status writers. Document optional recovery fields and their unknown/null behavior; recommend submitted message identity while preserving correlation compatibility, ambiguity and the actual replay route name; distinguish command tracing, rejection details, event counts and committed sequence proof. Record source and validation evidence, close exactly these five Tenants ledger entries, and push owning documentation plus the Tenants reference update to main as requested.

</frozen-after-approval>

## Implementation Notes

- Small documentation-only route: no unresolved intent choices, runtime/API changes, migrations or deployment. Six existing Markdown documents, this spec, a validation evidence document, the five ledger blocks and the EventStore gitlink form the planned footprint. No new callable interface.
- Inspected root AGENTS/baseline/Git rules, repository formatting/build/test/commitlint configuration, EventStore contributor and docs-validation guidance, and the canonical Tenants architecture constraints. Investigation was delegated as directed by the build workflow; unrelated API and release-recovery candidates remain open.
- Initial Tenants revision: `1ccdacd11acb27fa9b7055ea2c44c98828867489`. Original EventStore gitlink: `5ce5f1d84b88c808cc8a526b9decf149de977e55`. Owning documentation is unchanged between that gitlink and fetched EventStore main `70b2206b531a71c883fbe2e4749047b399d26447`.
- Reuse `CommandStatusResponse.FromRecord`, `CommandStatusController` tenant-scoped direct/indexed/legacy resolution, `ReplayController` distinct original IDs and `AggregateActor.CompleteTerminalAsync` as contract evidence. Keep DW-523 (polling/Retry-After behavior) separate; the present correction does not resolve its runtime gap.
- Corrected the six owning EventStore pages and the five ledger records. Added a stable status-lookup heading, recovery advisories, replay identity/409 guidance, rejection failure details and eventful rejection counts. Preserved old ledger evidence with pre-status-documentation fields.
- Focused Markdown lint and local links/anchors pass. Full docs validation stops on 13 existing lint issues in seven untouched pages; installed lychee also requires a temporary option-shape adaptation. Exact commands, outcomes and commitlint successes are retained in `dw-507-509-510-513-518-validation-evidence.md`.
- Concurrent-session coordination: another session committed and pushed the six owning docs as `92b22bac631cd27f67e81e48e800459bd91af77b` while review started. The reviewer now examines that exact range from the recorded owner base. Unrelated submodule pointer advances from that session are excluded from this task's parent commit.
- Review patched six source-verified consistency issues: tracked-but-unarmed recovery, linked nullable recovery semantics, required tutorial message identities, lifecycle diagram tracing/tracking, base-revision evidence and rejection markers retained on publication failure. The linked operations guide is the seventh touched owner page. No runtime/API change was added, and no finding was deferred.
