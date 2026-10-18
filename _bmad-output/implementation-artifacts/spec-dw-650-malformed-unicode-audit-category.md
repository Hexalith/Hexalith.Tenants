---
title: 'Reject malformed Unicode audit category strings'
type: 'bugfix'
created: '2026-10-18'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: 'c8f3222af1ba5d10fe2c54e41b6f6be5c84eeeb4'
context:
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** DW-650 records escaped lone-surrogate category strings that throw during direct audit query payload decoding instead of returning a validation failure.

**Approach:** Return `InvalidEnvelope` for malformed Unicode category decoding before cursor decoding or audit reads. Preserve authorization precedence and all existing successfully decoded category semantics, including optional/blank values, case-insensitive names, numeric strings, unknown-category diagnostics and final-occurrence duplicate properties. Prove the rejection through the production query handler and close only DW-650 after verification and independent review.

</frozen-after-approval>

## Implementation Notes

- Route facts: investigation and prior DW-424 evidence settle the behavior; no user-visible intent gap or irreversible operation remains. The footprint is one private parser block and the existing focused test class, plus this spec, evidence and the selected backlog reconciliation. No new public API, dependency or persistence change is needed.
- Code map: `src/Hexalith.Tenants/Queries/Handlers/TenantQueryHandlerBase.cs::DeserializeAuditPayload` calls `categoryEl.GetString()` twice and catches only `JsonException` around the payload. Decode the category once and narrowly contain its `InvalidOperationException`; keep enum parsing unchanged. `GetTenantAuditQueryHandler.ExecuteCoreAsync` already authorizes first and returns parser failures before cursor decoding/read-model access; reuse it unchanged.
- Tests: extend `tests/Hexalith.Tenants.Server.Tests/Queries/GetTenantAuditQueryHandlerTests.cs`. Reuse `ExecuteAsync`, `AssertOnlyAuthorityWasReadAsync`, the substituted cursor codec, the real protected cursor and mixed-category/foreign-tenant fixture. Existing cases cover date bounds, optional filters, case-insensitive names and numeric-string categories.
- Acceptance: Given an authorized caller and an escaped lone high/low surrogate category, including malformed embedded sequences and a supplied invalid cursor, when the real handler executes, then it returns exactly `invalid-envelope`, no payload, no codec calls and only the authority read. Given a non-administrator with the same malformed payload, when queried, then Forbidden still wins without audit reads. Given valid escaped category names or a valid surrogate pair, when queried, then existing filtering or ordinary unknown-category diagnostics are preserved. Given duplicate category properties, when queried, then the final occurrence retains its existing meaning, rejecting only an effective malformed value.
- Implementation order: add focused regressions and execute them against unchanged production; repair only category decoding; rebuild and rerun the focused class and full Server project; independently review and recheck corrections; then reconcile DW-650 and the maintenance index while preserving all unrelated record bytes and historical evidence.
- Validation: use `dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true --no-restore --nologo -v:q`, then repository MTP `dotnet test --project ... -c Debug --no-build --no-restore` for focused/full Server lanes. Run `git diff --check` and `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-650-malformed-unicode-audit-category.md`. Record exact commands/outcomes and limitations in `dw-650-validation-evidence.md`.
- Scope constraints: preserve cursor opacity, stream selection, tenant isolation, all DW-424 behavior, archive/sprint, dependency pointers and existing release holds. Leave changes uncommitted and unstaged under the explicit user AGENTS.md rule; the task does not require staging, branching, committing or pushing.
- Runtime baseline: Aspire discovery returned no running AppHosts, and exact-target `aspire start` succeeded. Initial `aspire describe` found security TLS readiness unhealthy and dependent services waiting; structured logs were captured locally. This is baseline inspection, not live audit request qualification.
- Runtime follow-up: the second exact-target describe reported every resource Running/Healthy. Exact-target stop succeeded before the first test/source build. No live audit HTTP behavior is claimed.
- Original-production control: the 42 new cases expand the focused suite to 101. Debug source build passed with three transient MSB3026 copy-retry warnings and zero errors; focused execution exited 2 with 18 failed/83 passed/zero skipped. All failures are category decoding exceptions; unauthorized and compatibility controls pass.
- Implementation: category decoding now occurs once inside a narrow InvalidOperationException catch; only that decoding failure becomes InvalidEnvelope. JSON-kind validation, optional/blank behavior, enum parsing and handler ordering remain unchanged.
- Repaired verification: Debug source build exits 0 with one transient MSB3026 copy-retry warning and zero errors; focused 101/101 and full Server 1052/1052 pass with zero failures/skips, confirmed from TRX counters. Initial diff-whitespace and story-gitlink checks pass. Independent Blind Hunter review is running; backlog closure remains pending its triage.
- Review corrections: add eight raw malformed UTF-8 administrator/non-administrator cases with supplied invalid cursors, eight effective-final null/blank/numeric duplicate cases and two real protected-cursor continuation controls across equivalent escaped/plain category names. The focused suite grows from 101 to 119; production remains unchanged after the narrow decoding repair.
- Acceptance qualification: supplied-invalid-cursor tests use validly decoded opaque strings. A separately malformed Unicode cursor can throw before category validation; a malformed Unicode stream also throws with an otherwise valid category. These unchanged accessors are one pre-existing decoding root cause, newly recorded as DW-651 rather than broadened into this repair. The original comparison is source/accessor evidence, not a rebuilt baseline handler.

- Initial review-correction build failed with CS0029 because ShouldNotBeNullOrWhiteSpace returns void; separating the non-null value extraction and whitespace assertion fixes the fixture without changing the assertions or runtime.

- Completion: the independent correction recheck accepted all three coverage patches and the DW-651 deferral with no new issue. Final focused 119/119 and full Server 1070/1070 pass with zero failures/skips; final Debug source build passes with two transient MSB3026 retries and zero errors. Only DW-650 closes; DW-651 records unchanged cursor/stream Unicode exceptions. All 650 entry IDs remain plus DW-651: 651 IDs, 360 done/291 open across 263 exact-covering index rows. Exact commands, original failures and limits remain in dw-650-validation-evidence.md.

## Review Triage Log

- Medium, defer (DW-651): malformed Unicode cursor decoding throws before the category guard. Built-parser and production-handler reproduction plus unchanged entry-source/accessor comparison prove this pre-existing path; the selected ordinary invalid-cursor precedence remains covered.
- Medium, defer (same DW-651 root cause): malformed Unicode stream decoding throws with a valid category. The built handler and unchanged accessor reproduce it; malformed category alone or with a malformed stream is rejected by the repaired category path.
- Medium, patch: escaped-surrogate-only cases omitted raw malformed UTF-8. Add byte-preserving handler tests for isolated continuation, overlong, encoded-surrogate and out-of-range sequences, with authorization and no-cursor/no-audit assertions.
- Medium, patch: duplicate Unicode cases omitted effective-final null, blank and numeric strings. Add exact-row controls for both surrogate forms and numeric values 0/99 without changing final-occurrence parsing.
- Medium, patch: escaped positive cases omitted pagination. Add two protected-cursor continuation cases swapping plain/escaped spellings in both directions, proving exact filtered rows and terminal paging state.
