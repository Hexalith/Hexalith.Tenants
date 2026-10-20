---
title: 'DW-158: Name subscription wait timeout failures'
type: 'chore'
created: '2026-10-20'
status: 'done'
route: 'oneshot'
review_loop_iteration: 1
baseline_commit: 'b8470b6aa0568270576811b13674cea91a6c086b'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** DW-158 records a private subscription-test wait helper whose two-second expiry escapes as a raw TaskCanceledException. The failure does not identify which expected condition failed, making slow subscription-test failures difficult to diagnose. This is the next dependency-free actionable Tenants item after DW-155; earlier open work requires another owner or an unresolved contract.

**Approach:** Give the existing wait an explicit condition description and translate only its polling-delay expiry into a named assertion failure. Preserve its two-second deadline, successful polling and original predicate exceptions. Qualify timeout diagnostics and immediate/eventual success, run focused and full UI tests, close only DW-158, and commit and push on main as requested.

</frozen-after-approval>

## Implementation Notes

- Entry revision: `b8470b6aa0568270576811b13674cea91a6c086b`, clean main. This is freeform maintenance work without a sprint story key. The user explicitly authorizes the main commit and push.
- Read the repository baseline, Git and UX guidance, tracked build/test configuration, architecture constraints and the relevant subscription tests. Independent read-only investigation confirms one helper and one caller in `tests/Hexalith.Tenants.UI.Tests/Services/TenantReadRefreshSubscriptionTests.cs`.
- No unresolved intent choice, migration, production change, public API or dependency update. Small footprint: the existing test file, this spec, validation evidence, deferred ledger and maintenance index.
- Keep the helper private and require a readable condition name. Scope expiry translation to the cancellable delay so failures evaluating the predicate retain their original exception type and identity. Preserve the current timeout and polling interval; avoid introducing shared test infrastructure for this local diagnostic.
- Verify the named timeout fails against the original helper, then passes after repair, alongside immediate/eventual success and predicate-exception controls. Build Debug with source references and use the maintained per-project Microsoft.Testing.Platform lane. Review independently before final closure; preserve historical ledger fields, unrelated records and existing release qualification holds.
- Aspire baseline: the isolated Tenants AppHost starts, the UI becomes healthy and all 23 resources report Running/Healthy. A bounded Error query still contains InitializeTenantRegistryActivity and HTTP 500 failures. The started Tenants AppHost stops successfully; an already-running Parties AppHost is left alone. This is runtime-baseline evidence, not complete topology qualification.
- The initial Debug source build passes with zero warnings/errors. The first five-case original-helper run has the intended timeout failure and one fixture failure: Shouldly's Task overload substitutes a TaskCanceledException, defeating a predicate-exception identity assertion. The corrected test observes a direct await instead and now checks two distinct timeout condition names (six new cases total). Initial results remain in validation evidence.
- The first repaired-helper focused run has 27/29 passing: Shouldly also rethrows its own ShouldAssertException inside ThrowAsync, so both timeout tests need direct typed exception capture. After that fixture correction, the final six-case original-helper replay builds with zero warnings/errors and fails exactly the two timeout cases (4 passed, zero skipped); the success and predicate-identity controls pass. The replay restores the intended source in finally, and a fresh restored build is required before acceptance.
- The restored intended-source build has zero warnings/errors, and the maintained subscription class passes 29/29 with zero failures/skips, including all six new cases. The final scoped whitespace-format check exits 0 with only the generic workspace-load warning. The own-spec gitlink guard passes without reference pointer changes. Final full UI acceptance and independent Blind Hunter review are in progress.
- Initial full UI acceptance passes 6579/6579 with zero failures/skips. Independent review requests four simple qualification patches: normalize replay matching for LF while preserving entry bytes, assert a tolerant elapsed-time lower bound, cover predicate cancellation after deadline expiry, and inspect recorded exception details in the replay. All four are accepted; the revised seven-case helper lane adds one deliberate three-second synchronous predicate to cross the unchanged deadline. The deadline cases now require at least 1.9 seconds without imposing a scheduler-sensitive upper bound. Final focused/full acceptance will be rerun after these test/documentation changes.

## Review Triage Log

- Low / patch: the replay assumed CRLF. It now normalizes both the source and match to LF and restores the exact entry bytes in finally, supporting either checkout style.
- Low / patch: timeout-message checks did not independently constrain elapsed time. Both descriptions now require more than 1.9 seconds, a tolerant lower bound for the unchanged two-second deadline.
- Low / patch: predicate cancellation was only qualified before expiry. A third predicate-exception case deliberately remains in synchronous evaluation past the helper deadline, then requires the exact original exception instance.
- Low / patch: the negative-control driver checked only failure counts/names. It now also requires TaskCanceledException in each failed result's recorded exception information.
- Independent correction recheck accepts all four patches and reports no new concrete issue. The reviewed seven-case original-helper replay builds with zero warnings/errors and has exactly the two named-timeout failures, both recording TaskCanceledException; all five compatibility controls pass. The final restored reviewed-source build also has zero warnings/errors. The helper retains the original two-second timeout and ten-millisecond poll.
- Final reviewed acceptance passes: restored Debug source build has zero warnings/errors, focused 30/30 and full UI 6580/6580 pass with zero failures/skips. Independent correction review accepts all four findings with no remaining concrete issue or deferred follow-up. Exact commands, initial fixture failures, original-helper controls and limits are in [validation evidence](dw-158-validation-evidence.md).
- Gated closure changes only DW-158 and retains all replaced historical fields: 656 IDs, 398 done/258 open and 233 maintenance rows uniquely covering every open ID. Archive, sprint, production sources, dependency pointers and existing release holds remain preserved. Pinned commitlint 21.2.3 accepts the exact full message with zero problems/warnings; origin fetch reports zero divergence before commit.
- Final pre-commit fetch supersedes the earlier zero-divergence snapshot: origin/main is two commits ahead at `9a94131a26d0c5d80d5540706e267ee123d54b68` through `18e73d5b43abd687199db3313467b5a60c0d2691`. The only upstream tree change is an unrelated Parties caller/GET-ready operation in the module manifest; tested UI sources/tests and dependency pointers are identical. Preserve it by rebasing the tested local commit onto current origin/main before push. The recorded full UI qualification remains entry-source evidence; no combined-tree full rerun is claimed.
