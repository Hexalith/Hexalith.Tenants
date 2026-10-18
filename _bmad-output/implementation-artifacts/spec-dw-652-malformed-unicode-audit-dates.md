---
title: 'DW-652: Contain malformed Unicode audit dates'
type: 'bugfix'
created: '2026-10-18'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
baseline_commit: '2143c8e51bdf37babb4f29dd5953d24fd25e754c'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Embedded or reversed unpaired surrogates in audit `from` and `to` strings can throw from `JsonElement.TryGetDateTimeOffset`, escaping the existing payload validation boundary (DW-652).

**Approach:** Return bare `InvalidEnvelope` for these malformed bounds before cursor decoding or audit reads. Preserve authorization precedence, missing/null dates, existing invalid-date rejection, inclusive date/offset filtering, final duplicate-property selection and protected cursor scope. Demonstrate the defect through the production audit handler, then verify the repaired behavior and valid controls. Scope is DW-652; standard pagination cursor validation remains DW-653.

</frozen-after-approval>

## Implementation Notes

- Planning: no unresolved intent gaps, irreversible operations or public API changes. Small footprint: the local date accessor in `src/Hexalith.Tenants/Queries/Handlers/TenantQueryHandlerBase.cs`, its existing handler test class in `tests/Hexalith.Tenants.Server.Tests/Queries/GetTenantAuditQueryHandlerTests.cs`, this spec, validation evidence and DW-652 bookkeeping in the ledger and maintenance index.
- Investigation: catch only `InvalidOperationException` at the date accessor. Its existing false result already becomes bare `InvalidEnvelope`; retain all surrounding parsing and handler ordering. Reuse `ExecuteAsync`, `AssertOnlyAuthorityWasReadAsync`, `CreateStore` and `CreateCursorCodec` for malformed dates, authorization/cursor precedence, equivalent escaped date bounds and duplicate-member controls.
- Verification: Debug source build of the Server test project, maintained MTP focused audit-handler run (initial failing evidence before repair, final pass) and full Server project; `git diff --check` and story-gitlink validation. Preserve existing findings and unrelated ledger records, archive, sprint status and release holds. Record exact commands/results in `dw-652-validation-evidence.md`.

- Initial reproduction: source Debug build passes with one transient copy-retry warning; maintained focused audit-handler run exits 2, with 44 failed/217 passed/zero skipped (261 total). All failures throw `InvalidOperationException` from the original date accessor, including final malformed duplicate dates.
- Runtime baseline: Aspire start/describe succeeded; application projects reached Running, security readiness was Healthy, and security logs showed startup completion. Dapr CLI resources finished; no live audit endpoint validation is claimed. Stopped only the AppHost started for this pre-edit check.
- Implementation: wrap the existing date conversion in a local `InvalidOperationException` catch, returning false with the nullable output still null. Existing payload validation produces bare `InvalidEnvelope`; no handler-wide catch or cursor behavior changes. Added 106 handler cases covering both date bounds, embedded/reversed/separated surrogate sequences, raw malformed UTF-8, authorization/cursor precedence, escaped-date protected continuation, inclusive offsets and final duplicate-member semantics.

- Independent review found no new production defect. Added six compatibility controls: mixed valid/malformed bounds under both authorization states and cursor continuation across equivalent UTC/offset representations in both escape directions. Final addition count is 112 handler cases. Review also requested durable initial-failure evidence, baseline reproduction guidance and completion bookkeeping.

- Final verification after review: Debug source build exits 0 with zero warnings/errors; focused audit handler 267/267 and full Server 1218/1218 pass, zero failures/skips. Only DW-652 closes with its historical fields preserved; the maintenance index removes only that row. No Git mutation, dependency update, archive move, sprint change or release-readiness approval ran. User Git safeguards take precedence over the workflow's automatic commit step; changes remain uncommitted.

## Review Triage Log

- Low / patch — Completion evidence still pending: accurate during review. Finalized this spec, DW-652's ledger fields and its maintenance row only after relevant tests and independent review passed.
- Low / patch — Reproduction commands used the current implementation: added disposable-copy guidance using the canonical baseline revision and current tests; distinguished expanded final tests from the historical 261-case run.
- Low / patch — Temporary raw evidence may disappear: preserved an actual initial exception excerpt and red/green TRX counters in the repository evidence document, including final counters.
- Low / patch — Equivalent UTC/offset continuation absent: kept the original escaping controls and added both escape directions with equivalent representations of both bounds. Platform QueryCursorScope formats instants as UTC; production scope behavior is unchanged.
- Low / patch — Mixed valid/malformed bounds absent: added both malformed-bound directions for administrators and non-administrators, asserting exact failure, no payload, no codec calls and authority-only access.

- Independent reviewer recheck: all five findings resolved; reviewer verified six added compatibility cases, durable evidence/reproduction guidance, spec/ledger/index reconciliation, zero-warning/error final build and TRX counts (267 focused; 1218 full Server; zero failures/skips).
