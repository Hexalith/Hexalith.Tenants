---
title: 'DW-653: Contain malformed Unicode standard pagination cursors'
type: 'bugfix'
created: '2026-10-18'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
baseline_commit: '57d564b4b38a05ba3444ed7093d8330a1a597bfa'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Malformed Unicode in standard-query cursor strings throws from JSON string decoding and escapes pagination validation (DW-653), instead of returning a query failure.

**Approach:** Return bare `InvalidEnvelope` when a cursor cannot be decoded as a string, before invoking the cursor codec. Cover all four standard pagination handlers and preserve their existing identity, route, read and authorization ordering. Preserve successfully decoded cursors, protected continuation and scope validation, final duplicate-property selection, page-size clamping and existing malformed-JSON/default-page compatibility. Reproduce the failure through production handlers before repair; add direct parser and handler regressions. Scope is DW-653 only.

</frozen-after-approval>

## Implementation Notes

- Planning: no unresolved intent gaps, irreversible actions or public contract changes. Small cohesive footprint: `src/Hexalith.Tenants/Queries/TenantQueryPaginationPayloadParser.cs`, its pagination-result record moved to `TenantQueryPaginationPayload.cs` to follow the single-type rule, the base pagination adapter and its four handler consumers, focused parser/handler tests, this spec, validation evidence and DW-653 ledger/index bookkeeping.
- Investigation: catch only `InvalidOperationException` around cursor `GetString()`; carry an optional error through the existing pagination result and base adapter. Each handler consumes the error at its existing parsing point. Shared missing-user rejection and global wrong-route rejection still precede parsing; get-user-tenants still reads its index and evaluates self/admin visibility before parsing. No handler-wide catch or audit-parser changes.
- Tests reuse `TenantQueryTestHarness.ExecuteAsync`, read-model substitutes and the real protected cursor codec. Cover unpaired/embedded/reversed surrogates, raw invalid UTF-8, final duplicates, escaped property names and valid Unicode/escaped strings, identity/route precedence, exact existing read boundaries, protected continuation/scope rejection and page-size defaults/bounds.
- Verification: Debug Server test-project source build, focused red/green parser and production-handler cases, then full Server suite. Record exact commands, counters, independent review and limits in `dw-653-validation-evidence.md`; run whitespace and story-gitlink checks. Preserve unrelated ledger entries, archive, sprint status, dependency pointers and existing release holds. Leave changes uncommitted under the user-provided Git safeguard.

- Runtime baseline: Aspire start/describe succeeded; the final snapshot reports all application and Dapr resources Running/Healthy. Security logs were captured. Stopped only the instance started for this pre-edit check. No live query qualification is claimed.

- Initial reproduction: source Debug build succeeds with two transient MSB3026 copy-retry warnings and zero errors. The 64-case production-handler run exits 2: 54 failed, 10 passed, zero skipped. Failures throw InvalidOperationException through the original cursor accessor; the identity/route and valid-pagination controls pass.
- Implementation: the local cursor decoder now returns an optional validation error, consumed by all four standard handlers at their existing parsing points. The result record has its own file. No audit, authorization, cursor-codec or page-size policy changes.

- Initial repaired verification: source Debug build has zero warnings/errors; focused parser/handler 92/92 and full Server 1296/1296 pass, zero failures/skips. There are 78 new test cases. Whitespace and story-gitlink checks pass with no dependency pointer changes. Independent review and completion bookkeeping remain pending.

- Independent review found six missing compatibility controls and no new production defect. Added handler checks for decoded Unicode reaching invalid-cursor validation, malformed-document defaults versus ignored Unicode fields, a valid cursor superseded by a malformed final duplicate, present/filtered cross-user targets and propagation of the exact downstream codec exception; added direct malformed-document parser controls. Production code is unchanged after review.

- Final verification after review: source Debug build exits 0 with one transient MSB3026 copy-retry warning and zero errors. Focused parser/handler 112/112 and full Server 1316/1316 pass, zero failures/skips; there are 98 new test cases total. Only DW-653 closes and its maintenance row is removed, with original findings/prior fields and unrelated records preserved. No Git mutation, dependency update, archive move, sprint change or release approval ran. User Git safeguards take precedence over the workflow's automatic commit step; changes remain uncommitted.

## Review Triage Log

- Low / patch — Valid Unicode was only checked directly in the parser. Added a valid escaped surrogate-pair cursor through every production consumer, preserving `invalid-cursor` rather than `InvalidEnvelope`.
- Low / patch — Malformed JSON with an undecodable cursor lacked boundary coverage. Added parser and all-handler controls proving document-parse failure still defaults to the first page.
- Low / patch — Duplicate controls started with malformed cursors. Added the reverse order with a real protected first token, malformed final token, bare `InvalidEnvelope`, no codec calls and unchanged read boundaries.
- Low / patch — Cross-user cases covered only an absent target. Added present memberships for both the administrator and filtered outsider, preserving the same ordered index/admin reads and validation result.
- Low / patch — Unrelated malformed Unicode was not covered. Added all-handler controls proving ignored string fields remain ignored and the requested first-page size survives.
- Low / patch — Downstream exceptions lacked an explicit boundary control. Added all-handler codec-fault cases proving the same InvalidOperationException instance propagates, with existing pre-parsing read boundaries intact.

- Independent reviewer recheck: all six findings resolved, no unresolved findings or new deferrals. Latest focused 112/112 and full Server 1316/1316 pass with zero failures/skips; latest source build succeeds with one transient MSB3026 warning and zero errors. Reviewer also confirmed DW-653-only closure, preserved original evidence, exact maintenance-row removal, archive/sprint hashes and unchanged dependency gitlinks.
