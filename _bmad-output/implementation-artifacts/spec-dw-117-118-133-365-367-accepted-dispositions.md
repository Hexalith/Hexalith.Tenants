---
title: 'Close five settled deferred-work dispositions'
type: 'chore'
created: '2026-10-25'
status: 'draft'
route: 'dispatch'
superseded_by: 'spec-dw-194-201-221-267-609-ui-safety.md'
review_loop_iteration: 0
baseline_commit: '332362168abab554e51de36a49dc404966699435'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Complete exactly DW-117, DW-118, DW-133, DW-365 and DW-367 as accepted dispositions, then commit and push the reviewed changes to main as requested. These five records already document intentional exception surfacing, historical tooling attribution, and deliberate correction-role selection; their approved policies leave no current implementation task. Preserve those limitations and their future reopening conditions without claiming runtime fixes, historical role recovery, or rewritten commit attribution.

Reconcile the deferred ledger and maintenance index, preserve every original field and unrelated record, and retain concise evidence from the owning specifications and current source. Verify exact ID/status/index conservation, unchanged application code and gitlinks, documentation links and whitespace. Obtain independent review before finalizing. This documentation-only batch does not qualify runtime, deployment or release readiness.

</frozen-after-approval>

## Implementation Notes

- The explicit five-item request overrides the default single-goal proposal. All five dispositions are already settled; no new product decision or public contract is introduced. The small documentation footprint qualifies for the oneshot route. Commit and push to main are explicitly authorized.
- Entry main was clean with one local ledger-sweep commit and two remote query fixes. Fetched origin and rebased the local commit without conflicts; canonical integrated baseline is recorded above. Preserve all existing work.
- Evidence: Story 1.9 records the deliberate codec exception partition; `TenantQueryGateway.IsSurfacingDefect` and existing decode/encode tests preserve it. Story 1.10's boundary corrections already attribute the three render files to tooling. Story 4.3 separates pushall provenance, with safeguards independently closed by DW-417/418/428/429. Story 5.5 requires explicit role selection and forbids history inference; the removal event still has no previous-role field.
- Change only the ledger, its maintenance index, this spec and companion validation evidence. Preserve old mutable fields with a `pre-accepted-close-` prefix; keep original disposition and dependency fields and state each conditional reopening trigger explicitly. Leave archive and sprint tracking unchanged.
- Replanning discovery: the maintenance index explicitly requires accepted limitations to stay open for visibility. The proposed closure would change that tracking policy and is withdrawn. No selected record or maintenance row was changed. This draft is superseded by the actionable five-item spec linked in frontmatter; its frozen proposal is retained solely as planning history, not approved implementation scope. An independent concurrent `/pushall` session committed this initially untracked draft in `14133e96d2c5efb13270580f920133079707398b`; preserve that published history.
