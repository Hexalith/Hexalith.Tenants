---
title: 'DW-641: Reconcile verified authentication alias precedence coverage'
type: 'chore'
created: '2026-10-18'
status: 'done'
route: 'oneshot'
baseline_commit: '3e20b0372203fa31f93d60dad9a07c65b4d905df'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** DW-641 still requests tests for conflicting nonblank legacy and alias authentication settings, although committed generated-controller tests already assert exact legacy signing-key and authority precedence. The stale maintenance record invites duplicate work.

**Approach:** Verify the existing coverage against a fresh Debug source build, then close only DW-641 with reproducible evidence and independent review. Preserve its historical finding and verification, every unrelated deferred record, dependency pointers, archive, sprint tracking and release holds. No production or test changes are required.

</frozen-after-approval>

## Implementation Notes

- No unresolved intent gaps, irreversible operations or public API changes. The footprint is this specification, DW-641 validation evidence, the DW-641 ledger record and its maintenance index row. Work started on clean `main`; the invoked workflow requires a validated local Conventional Commit and no push.
- Code map: `src/Hexalith.Tenants.Api/Program.cs::FirstConfigured` selects a nonblank legacy value first. `tests/Hexalith.Tenants.IntegrationTests/TenantsApiGeneratedControllerTests.cs::Legacy_signing_key_wins_over_alias` and `Legacy_authority_wins_over_alias` supply distinct nonblank values and resolve real host authentication options. The factory clears configuration sources before applying fixture settings. Reuse these tests and neighboring alias/fallback controls; do not add duplicate tests or change authentication semantics.
- Validation plan: fresh IntegrationTests Debug build with `UseHexalithProjectReferences=true`; direct-assembly precedence cases and the generated-controller class; compare ledger IDs and unrelated record bodies with the entry revision, verify maintenance-row coverage, links, whitespace and unchanged dependencies/archive/sprint. Record exact results and scope limits. This is documentation reconciliation; no application source is changed or live topology qualification claimed.
- Completed: both exact precedence tests pass (2/2); the complete generated-controller class passes 44/44 with zero failures/skips/errors. The normal source build is blocked by Commons NU1902 for MessagePack 2.5.302; the documented local `NuGetAudit=false` fallback succeeds with zero warnings/errors. An initial unsupported interior-wildcard filter exits 3 without tests; corrected exact method arguments pass. [Exact commands, durable run receipts and reproducible preservation checks](dw-641-validation-evidence.md) distinguish the normal gate blocker from successful focused verification.
- Only DW-641 closes. Its original fields survive under `pre-dw-641-*`; all 655 IDs and unrelated record bodies remain, with 375 done/280 open, 193 actionable and 252 exactly covering index rows. Production/tests, root dependency revisions, archive/sprint, DW-639 and release holds remain unchanged. The reproducible closure check exits 0. Independent review requested four documentation improvements, all addressed below; no finding is deferred. `review_loop_iteration` stays zero because this oneshot did not enter a dispatch review loop.

- The independent correction recheck confirms all four findings resolved and no remaining issues. Pinned commitlint 21.2.3 accepts the exact nonempty local candidate with zero problems/warnings after a transient disk-full write failure. Full validation receipts are retained in the linked evidence; no push is requested.

## Review Triage Log

- **low / patch — missing reproducible reconciliation command:** the initial evidence described the checks but omitted their executable form. The evidence now embeds the complete checker and successful output; execution confirms exact counts/coverage, preserved historical fields and unchanged unrelated content.
- **low / patch — temporary-only execution receipts:** the initial totals referenced ephemeral XML/log files. Durable XML summaries retain counts, UTC start/finish times and SHA256 values for the build/test logs, XML and executed assembly. Rerun commands remain available; the 44-case class total includes the two focused cases.
- **low / patch — source/dependency provenance receipts:** evidence now preserves pre-build working-file hashes matching final values, normalized source comparisons against the canonical entry revision and all nine clean nonrecursive root-submodule revisions. No dependency or application/test content changed.
- **low / patch — incomplete final specification metadata:** final status is now `done`, with evidence link and this review disposition. No new `review_loop_iteration` is warranted: the workflow increments it only for dispatch review loopbacks, which did not occur here.
