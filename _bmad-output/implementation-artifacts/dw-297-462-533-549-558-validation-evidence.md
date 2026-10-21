# Deferred verification and planning reconciliation — 2026-10-21

Scope: DW-297, DW-462, DW-533, DW-549 and DW-558. Entry revision:
`0c10ef637171f9ac2639d4b5ac42f513ea9ef133`. This is documentation evidence;
no application build, browser test or runtime command lane was rerun. Historical
test results retain their recorded source, configuration, count and limitations.

## Repairs

| Record | Current evidence |
| --- | --- |
| DW-297 | The [removal-proof spec's formal Verification](spec-2-4b-wp-2a-removal-proof-and-audit-available.md#verification) now carries its already recorded 2026-08-22 Release solution command and zero-warning/error result, explicitly historical. |
| DW-462 | The [Story 5.4 report's reading guide](story-5-4-re-review-verification-2026-09-30.md#reading-the-historical-evidence-2026-10-21-dw-462) qualifies stale line anchors/statuses, identifies the exact later commands and nine mutation rows, and discloses the unrecovered earlier three mutations. The legacy story already links this report and remains byte-identical. |
| DW-533 | [Epic 5 context](epic-5-context.md#scope-and-source-precedence-reconciled-2026-10-21-dw-533) restores cited planning constraints and separately identifies the four retained implementation refinements. It is a handoff, not current runtime acceptance. |
| DW-549 | The [Story 5.6 reverification artifact](spec-5-6-preview-confirm-and-link-a-tenant-correction-2.md) preserves its baseline/frozen intent, inventories all three historical targets and binds final endpoint `2b91b05be9c927319fd207b02552d1213997776b`. |
| DW-558 | The dated ownership qualification below supersedes the historical paging record's delivered-page claims for current interpretation. The ledger links here as its current evidence; the blocked historical spec is preserved byte-for-byte. |

## DW-558: paging implementation versus Story 5.7 ownership

This is the current qualification of
[spec-global-admin-projection-paging.md](spec-global-admin-projection-paging.md),
whose frozen intent, checked tasks, ACs, passing pre-reversal test counts,
`in-review` status and non-ancestor baseline remain historical evidence.
It does not independently establish delivery of today's audit-page integration.

The historical sequence is:

1. `5a3bc6dd4c1aa77cec50d0a960e986aa89c08cbe` enabled the audit-page global
   correction branch while implementing complete projection loading.
2. `10c9f6f66632f6861fe13fb9f41ab05b90c37f67` removed that branch under Story
   5.6's pass-17 boundary decision.
3. `ded413dfd4786983585c7f56a399825cc1afbae3` removed its initial complete-read
   enrichment and restored the page's read-only correction gate. Its retained
   paging review note explicitly says enrichment waits for Story 5.7.
4. `362a957de11ce141f48d73d9ed4301f31a1c870e` subsequently restored the panel
   as part of Story 5.7. The [owning Story 5.7 spec](spec-5-7-correct-global-administrator-authority-from-audit-evidence.md)
   is recorded as done and owns its fixed scope, eligibility, confirmation and
   deterministic proof contract. That status does not clear independent release,
   runtime or authorization-lag holds.

| Paging record claim | Current qualification |
| --- | --- |
| Checked `TenantAuditPage.razor` task: initial enrichment, correction-open refresh, confirmation and refreshed-intent derivation | Implemented at the earlier checkpoint, then reverted in the two commits above. Current page integration belongs to Story 5.7, not to completion of the paging spec. |
| Checked combined component-test task including `TenantAuditPageTests` | The earlier page tests were evidence for the subsequently removed branch. Current global correction page tests qualify the later Story 5.7 integration; the old passing counts are not fresh evidence. |
| AC1–AC3: complete presence/count, confirmation and incomplete-evidence refusal | The reusable loader/panel/snapshot work survives; audit-page acceptance is owned by Story 5.7's later contract and evidence. |
| AC4: re-evaluate a correction intent after correction-open refresh | The audit-page claim was reverted with the branch. It can be assessed today only through Story 5.7's current integration. |
| Final verification: audit-page submission through the complete-read provider | Historical pre-reversal evidence. It cannot authorize re-enablement or change the paging artifact's in-review status. |

At the entry revision, the current source inventory is
[`TenantAuditPage.razor`](../../src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor):
`GlobalAdministratorCorrectionPanel` with `ProjectionRefreshProvider`, and
`RefreshGlobalAdministratorsProjectionAsync` using
`GlobalAdministratorsProjectionLoader.LoadAsync`. Existing
[`TenantAuditPageTests`](../../tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs)
include `Open_global_correction_refreshes_preview_when_paging_past_global_audit_rows`
and `Tenant_audit_page_keeps_global_administrator_correction_fail_closed_when_unauthorized`.
These are inspected source anchors, not a new test-run claim.

The paging guard still exits 1 because baseline
`23b2a7691ae667fd160a48a5d7293f3cf38d48fe` is not an ancestor of entry HEAD.
The changed-story CI policy requires a valid historical baseline before that
artifact can itself be edited. This report resolves its ownership ambiguity
without bypassing or weakening that separate history hold.

## Validation results

- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction-2.md --ref 2b91b05be9c927319fd207b02552d1213997776b` — exit 0, three exact declared targets. The initial pre-correction check failed with an undeclared Builds target and stale Memories/Platform targets. The first draft placed the inventory outside the parser's recognized sections and also failed; moving the declaration into File List and the final table into Completion Notes corrected that draft.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md --ref b2b80941df874c2ee6772ca316841c480e0e493b` — exit 0, seven exact declared targets.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-297-462-533-549-558-evidence-reconciliation.md` — exit 0, no pointer changes.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/5-4-audit-availability-state-recovery.md` — exit 1, retained invalid baseline `a5ca6e3f548e89b28a37826be721d9ef9f7cd51a` is not a commit. DW-461/DW-467/DW-478 remain open; this artifact is unchanged.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-global-admin-projection-paging.md` — exit 1, retained non-ancestor baseline as described above. Artifact/status unchanged.

The historical guard reads supplied current Markdown against the selected Git
tree. It does not load the old Markdown. The explicit endpoint is checked by
the committed-event runner before the authorized push; no historical endpoint
is used to hide new dependency work.

- `python3 tests/scripts/test_validate_story_gitlinks.py` — exit 0, **77/77**.
- `python3 tests/scripts/test_validate_changed_story_gitlinks.py` — exit 0, **49/49**.
- Read-only calls to the current CI runner's `audit_ref` passed for both repaired
  historical specs and this batch's spec. Five temporary-text controls were
  rejected with the expected diagnostics: each of three wrong final targets,
  the missing Builds declaration, and the original checkpoint with final targets.
  No production document or dependency checkout was mutated by those controls.
- Inline repository comparisons against entry revision verified all **657**
  ledger IDs survive, exactly the five selected records change, and every old
  selected-record field survives either verbatim or under a
  `pre-evidence-reconciliation-` key. Final counts are **460 done/197 open**;
  **181** maintenance rows cover all open IDs exactly once.
- Both edited historical specs retain their exact frozen intent. The selected
  Story 5.6 endpoint equals its last historical file edit, all final targets equal
  that Git tree, and its baseline is unchanged. The untouched legacy Story 5.4
  artifact has SHA-256 `b515eba5599bd66d262fe123f6224c9cef176da2e385cd2e49a0c8f79a88b082`;
  the untouched paging artifact has SHA-256
  `5805754f3fbd37f64b8effffa8d81ab5b8b10f6eb269ea0644a1be03c7d4a40e`.
- `git diff --check` — exit 0. No application, test, build configuration,
  dependency gitlink, archive, sprint or shared instruction file is changed.

## Reproduce the snapshot checks

```bash
python3 _bmad-output/implementation-artifacts/dw-297-462-533-549-558-verify.py
```

Exit 0. The [retained checker](dw-297-462-533-549-558-verify.py) contains the
exact calls, comparison arguments and controls formerly run inline. It compares
against entry revision `0c10ef637171f9ac2639d4b5ac42f513ea9ef133`, not moving
HEAD. It checks unique ledger records from each `### DW-N` header to the next
Markdown section, expands explicit grouped DW IDs in the first maintenance
table cell, and requires exact coverage without duplicates. It also verifies
historical document/intent preservation, the last file edit, every table
baseline/target, and the complete three-commit first-parent pointer chain.

The three CI preflights invoke `audit_ref` with each exact path as its singleton
identity, `base=head=0c10ef637171f9ac2639d4b5ac42f513ea9ef133`, and `moved=False`.
Their observed endpoints are `b2b80941df874c2ee6772ca316841c480e0e493b` for
the removal-proof spec, `2b91b05be9c927319fd207b02552d1213997776b` for Story
5.6 reverification, and the entry revision for this batch's spec.

Each control writes only a temporary copy with the original artifact basename
and calls the production guard with explicit `--ref`; the checker preserves
the exact invocation and requires both the failure exit and diagnostic.

| Control | Supplied change/ref | Expected | Observed |
| --- | --- | --- | --- |
| Builds target | Replace its final table target with 40 `f` characters; ref `2b91b05be9c927319fd207b02552d1213997776b` | exit 1, `[MISSTATED]` | matches |
| Memories target | Same replacement/ref for Memories | exit 1, `[MISSTATED]` | matches |
| Platform target | Same replacement/ref for Platform | exit 1, `[MISSTATED]` | matches |
| Missing Builds declaration | Delete only `- references/Hexalith.Builds` from File List; same ref | exit 1, `[UNDECLARED]` | matches |
| Superseded checkpoint | Current final inventory; ref `5bfe0715591ecae8790472b29bb0e7d8986da4ae` | exit 1, `[MISSTATED]` | matches |

This is deliberately a fixed-snapshot checker: future unrelated ledger changes
fail its exact expectations. To replay later, locate its introducing commit
with `git log --diff-filter=A -1 --format=%H -- _bmad-output/implementation-artifacts/dw-297-462-533-549-558-verify.py`
and run in a detached worktree of that commit. It reads current files from that
worktree and immutable historical trees; it never edits the ledger or sources.

## Independent review

Blind Hunter computed `N = min(floor(sqrt(36.137) + 1), 10) = 7` and returned
seven findings. All are addressed within this evidence-reconciliation scope:
receipt-source precedence, distinct historical guard checkpoints, explicit grid
fields, global correction proof, the scoped tenant expiry exception, and the
two missing reproducibility sources. The global proof correction preserves the
approved Story 5.7 committed-sequence requirement; the older planning shorthand
does not weaken it. The workflow spec retains the full verdict/evidence log.

Independent recheck confirms all seven are resolved, the retained checker exits
0, and no remaining defect was found in the corrections. All 24 new local links
and heading anchors resolve.

During recheck, unrelated external work changed the EventStore checkout from
`5ce5f1d84b88c808cc8a526b9decf149de977e55` to
`c91f520270388080b950feb869c68b43964285f3` with dirty contents. This work is
preserved outside the documentation commit. Earlier default-HEAD worktree
guard results above were obtained before that change; final event validation
uses committed snapshots and must show no gitlink changes in this batch.

## Commit validation

`node_modules/.bin/commitlint --edit /tmp/dw-297-462-533-549-558-commit-message.txt --verbose`
using repository-pinned `@commitlint/cli@21.2.3` exits 0 with zero problems and
warnings. Successful output is retained in
`/tmp/dw-297-462-533-549-558-commitlint.log`; the exact full candidate below has
SHA-256 `c2c2e5656dd60c03fc5babb3f930a13575bc025918c0df674f98bc7e7287abba`
(UTF-8, LF, final newline):

```text
docs: reconcile five deferred verification and planning records

Qualify historical validation, restore cited Epic 5 constraints, and bind the
Story 5.6 reverification to its final historical dependency inventory.

Close DW-297, DW-462, DW-533, DW-549, and DW-558 while preserving separate
history blockers, original evidence, and unrelated deferred work.
```
