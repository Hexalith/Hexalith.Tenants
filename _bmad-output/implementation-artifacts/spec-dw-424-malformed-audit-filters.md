---
title: 'Reject malformed direct audit query filters'
type: 'bugfix'
created: '2026-10-18'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: '20a395af8086db9e62ec64fd9ce5f4b1e6e3a09e'
context:
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** DW-424 records direct audit queries whose malformed timestamp strings or non-string categories silently remove filters and broaden results. Non-string timestamp values can also escape as exceptions.

**Approach:** Reject malformed supplied `from`, `to`, and `category` values with `InvalidEnvelope` before cursor decoding or audit reads. Preserve global-administrator authorization precedence, omitted/null optional filters, blank category strings, valid date parsing and inclusive bounds, and existing string-category parsing. Prove these behaviors through the real query handler and reconcile DW-424 only after successful checks and independent review.

</frozen-after-approval>

## Implementation Notes

- Route facts: no unresolved user-visible intent gap, irreversible action, new public API, dependency change or data mutation. One production parser file and one focused handler test file, plus spec, validation evidence and the selected backlog closure.
- Code map: `src/Hexalith.Tenants/Queries/Handlers/TenantQueryHandlerBase.cs::DeserializeAuditPayload` owns audit parsing; replace its lossy timestamp helper with explicit parse success and check category JSON kind. `GetTenantAuditQueryHandler.ExecuteCoreAsync` already authorizes first and returns parser failures before cursor decoding or audit reads; reuse that ordering without changing the handler or transport.
- Tests: add `tests/Hexalith.Tenants.Server.Tests/Queries/GetTenantAuditQueryHandlerTests.cs`, using `TenantQueryTestHarness`, `IReadModelStore` substitutes and existing real protected-cursor conventions. Existing `Projections/TenantsProjectionActorTests.cs` dispatches real handlers and covers inclusive boundaries, reversed ranges, string categories, pagination, cross-tenant filtering and malformed JSON.
- Acceptance: Given each date field supplied as an invalid/empty/whitespace/impossible date string or number/boolean/object/array, when an authorized handler executes, then it returns `invalid-envelope`, no payload and no audit read. Given non-string, non-null category values, when queried, then the same rejection occurs. Given absent/null optional fields or blank category strings, when queried, then valid audit results retain their existing scope. Given an unauthorized caller with malformed filters, when queried, then Forbidden wins. Given valid date offsets and category names, when queried, then only the expected matching rows are returned.
- Preserve existing numeric-string category semantics; changing enum policy is outside DW-424. Do not modify pagination, stream selection, persistence, UI, dependencies, unrelated ledger records, archive or sprint status.
- Validation plan: Debug source-reference build of the Server test project; focused new handler cases first against the original parser, then against the repair; full Server project; `git diff --check`; `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-424-malformed-audit-filters.md`. Record exact commands, outcomes, review and limits in `dw-424-validation-evidence.md`.
- Runtime baseline: the exact-target Aspire start succeeded; initial describe found security TLS-readiness failure and dependent services waiting. This is runtime-baseline evidence, not successful live audit validation. Another workspace's Folders AppHost was already running and must be left alone.
- Runtime follow-up: a second describe showed security Healthy, all other non-parameter resources Running except the finished Memories Dapr command. The exact-target stop succeeded before production editing; no live audit query is claimed.
- Implementation: the timestamp helper now distinguishes optional absence/null from failed parsing and checks JSON kind before calling `TryGetDateTimeOffset`. Category parsing rejects non-string/non-null values. The existing handler returns both failures before cursor decoding or audit reads; no changes to authorization, contracts or transport were needed.
- Initial verification: the first new-test build failed because C# 14 reserves `field` in property accessors (CS9273/CS9258 and consequent CS1503); rename to `propertyName` fixed the fixture. Initial and recheck builds also logged transient MSB3026 copy retries. These are retained in the evidence, not treated as parser failures.
- Original-parser run: 39 total, 25 failed/14 passed/zero skipped; ten failures were wrong-type timestamp exceptions, fifteen reached cursor decoding instead of returning InvalidEnvelope. The final production build passed with zero warnings/errors; repaired focused run passed 39/39 with zero failures/skips. Full Server verification and independent review remain pending at this point.
- Version-control scope: leave changes uncommitted under the user's explicit AGENTS.md rule against committing unless the task requires it; this implementation request does not require a commit. No staging, branching, dependency update or push is needed.
- Review corrections: add numeric-string category compatibility, six one-sided/active-filter combinations, malformed filters with nonempty cursors, escaped lone-surrogate dates and both duplicate-member orderings. The existing JSON parser selects the final occurrence of duplicate properties; the repair validates that effective value and does not introduce a duplicate-rejection policy. The expanded suite has 59 cases. A separate existing lone-surrogate category decoding exception will be recorded as DW-650 rather than changing preserved string-category semantics in DW-424.
- Completion: independent review accepted all coverage corrections with no new issue. The first expanded build found two CA1062 fixture errors, corrected with parameter null guards. Final Debug source build passes with one transient MSB3026 copy retry and zero errors; final focused 59/59 and full Server 1010/1010 pass with zero failures/skips. DW-424 closes and the separate existing category exception is newly open as DW-650. All original IDs, unrelated record fields, archive/sprint, dependency pointers and release holds remain preserved; current totals are 650 IDs, 359 done/291 open across 263 index rows. `dw-424-validation-evidence.md` preserves the exact commands, initial failures, review and limits.

## Review Triage Log

- False, reject: duplicate members bypass a final invalid-value check. The parser has always selected the final member; no unique-member policy is required by DW-424. Added compatibility cases prove that final null retains optional semantics and final malformed values are rejected in both date/category cases. This documents existing behavior rather than imposing a new duplicate-member policy.
- Medium, defer: escaped lone high/low surrogate category strings throw `InvalidOperationException` at the unchanged `GetString()` accessor. Independent original-accessor and built-handler reproductions confirm a pre-existing string-decoding issue; record DW-650 with this evidence. The repaired date path already rejects these inputs and now has regression coverage.
- Medium, patch: preserving numeric-string category parsing lacked behavioral coverage. Added exact-row tests for `"0"`, `"1"` and undefined `"99"`, alongside the separate numeric-JSON rejection cases.
- Medium, patch: optional-only tests did not prove that remaining active filters survive. Added lower-only, upper-only and mixed null/blank/active date/category cases with exact row IDs.
- Medium, patch: negative tests lacked supplied cursors. Added three malformed-filter requests with a nonempty invalid cursor, asserting InvalidEnvelope, no payload, no codec invocation and only the authority read.
