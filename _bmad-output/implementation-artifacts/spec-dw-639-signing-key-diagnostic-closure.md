---
title: 'DW-639: Reconcile verified signing-key configuration diagnostics'
type: 'chore'
created: '2026-10-18'
status: 'done'
route: 'oneshot'
baseline_commit: 'd93ba9ab613d9c31638390ed99c898c72ef3bfc2'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** DW-639 requests that missing signing-key diagnostics name both supported configuration alternatives, although committed production code and real-host tests already do so. Its obsolete verification leaves completed work in the maintenance queue.

**Approach:** Verify the existing missing/blank/short-key diagnostics with a fresh Debug source build and focused tests, then reconcile only DW-639 and its maintenance index with reproducible evidence and independent review. Preserve historical findings, unrelated records, application/tests, dependencies, archive, sprint tracking and release qualification holds.

</frozen-after-approval>

## Implementation Notes

- No unresolved intent gaps, irreversible operations or public API changes. Footprint: this specification, DW-639 validation evidence, the DW-639 ledger record and its maintenance index. The invoked oneshot workflow requires a validated local Conventional Commit; no push is requested.
- Code map: `src/Hexalith.Tenants.Api/Program.cs` already names `EventStore:Authentication:SigningKey` and `Jwt:SigningKey` in missing/short-key exceptions, plus both accepted authority alternatives for missing keys. `tests/Hexalith.Tenants.IntegrationTests/TenantsApiGeneratedControllerTests.cs::Symmetric_key_mode_rejects_missing_blank_or_short_signing_key` has four exact-message cases; `Short_alias_signing_key_names_both_key_aliases` covers the alias short-key case. The factory clears configuration sources. Reuse those cases and adjacent authentication controls without changing behavior or adding duplicate tests.
- Investigation independently confirms this is a stale record. Diagnostic messages contain configuration names and static guidance, not supplied values. Current behavior and tests originated in commit `b185ae80de474bedc6c60af38bacc5550d4e8cd7`.
- Validation plan: Debug IntegrationTests build with `UseHexalithProjectReferences=true`, recording any ordinary gate blocker separately from local environment fallback; direct-assembly execution of the five diagnostic cases and containing generated-controller class. Check ledger ID/status/disposition counts, exact index coverage, historical field preservation, unchanged unrelated records/dependency pointers, local links and whitespace. Documentation reconciliation does not require starting or modifying an Aspire topology.
- Completed validation uses the maintained project runner instead of the planned direct-assembly fallback: the ordinary audit-enabled Debug source build passes with zero warnings/errors, and the generated-controller class passes 44/44 with zero failures/skips. Its TRX includes all five diagnostic cases. No fallback invocation was needed. [Exact commands, execution receipts and executable preservation checks](dw-639-validation-evidence.md) retain the evidence and scope limits.
- Reconciled only DW-639, preserving its historical fields under `pre-dw-639-*` and removing only its maintenance row. All 655 IDs remain: 377 done/278 open, 191 actionable and 250 exactly covering index rows. The preservation checker, story-gitlink guard and whitespace check pass; normalized application/test content, protected file hashes and nine clean root dependency revisions remain unchanged. An initial metadata-count inspection failed because archived records omit disposition; the corrected inspection and final checker account for their done status. It made no edits.
- Independent review produced five documentation findings, all corrected below; no production defect or new deferral was identified. The evidence now separates historical receipt verification from portable reproduction, records durable execution excerpts and guards stale/duplicate/incomplete results. Both checker modes pass on the retained original artifacts. Pinned commitlint 21.2.3 validates the exact local candidate with zero problems/warnings.
- Independent correction recheck confirms all five findings resolved and no concrete remaining issue. Isolated temporary evidence controls reject missing TRX, duplicate test identity, a substituted diagnostic input and an altered original log receipt. The exact embedded checker, story-gitlink guard and final whitespace checks pass. No finding is deferred.

## Review Triage Log

- **low / patch — reused reproduction results:** a failed launch could leave an older TRX in the fixed directory. Preserve the actual historical commands, and provide a separate fail-fast Bash recipe using a unique results directory; the checker requires that directory explicitly.
- **low / patch — unchecked historical artifact hashes:** the checker originally recorded artifact hashes without verifying them. Add an explicit original-receipts mode checking all recorded build/test log, TRX, assembly and protected-file hashes, and distinguish a fresh run from historical verification.
- **low / patch — incomplete test identity checks:** aggregate/name counts alone could admit duplicate results or omit one theory input. Require 44 unique test IDs, names and execution IDs, matching definitions, and exactly the null/empty/whitespace/short-legacy/short-alias matrix.
- **low / patch — checkout-dependent byte checks:** original CRLF working-file hashes should not gate a portable LF checkout. Keep byte checks in original-receipts mode and require normalized Git content in both modes.
- **low / patch — temporary-only raw execution artifacts:** the original logs/TRX remain temporary. Preserve copied build/test log excerpts and parsed counters, timestamps, case identities and outcomes directly in the evidence, while explaining the limits of hashes after full-artifact deletion.
