---
title: 'DW-648: Qualify ambiguous removal retry admission ordering'
type: 'bugfix'
created: '2026-10-17'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: '18b0f54578982cb16b4170013885a8632f8f3726'
context:
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Address the next focused deferred-work item, DW-648. A member removal with an ambiguous delivery remains `RequestSent`, retains its message identity, and can retry the same intent. Its separate retry admission path lacks controlled coverage of a successful authority result waiting while safety evidence changes and restores. Establish that coverage and repair runtime ordering only when the regression reproduces.

Given an actual ambiguous `RequestSent` removal, when a successful retry authority result is pending or queued and confirmation or freshness changes, then the obsolete admission must not dispatch. Restoring safe values does not validate the obsolete generation; deliberate recovery must reauthorize and reuse the original intent, message identity, and attempt start time. Given unchanged current safety, when deliberate retry succeeds, then normal accepted/status/projection behavior remains available. Denied, indeterminate, disposed, or replaced authority/attempt evidence must continue to fail closed. Preserve retained ownership and the distinction between accepted, confirmed, and audit available.

Keep changes within the existing removal flow, its regression tests, and this item's evidence/ledger/index. Use existing Fluent controls, tracker/gate, provider and localization patterns. Preserve unrelated records, historical evidence, archive, sprint status, dependencies, and release qualification holds. No version-control mutation or deployment is required.

</frozen-after-approval>

## Implementation Notes

- Route facts: no unresolved user-visible intent choice; no irreversible action; small footprint of one private retry method and its existing test class, plus evidence and bookkeeping. No new public API or dependency.
- Entry revision: `18b0f54578982cb16b4170013885a8632f8f3726`; branch `main`, clean entry tree. Selecting the latest recorded Tenants-owned continuation gives this run one coherent goal rather than the entire remaining backlog.
- Code map: `src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor`, `CanRetryDelivery`, `SubmitAsync`, `RetryDeliveryAsync`, and `ReauthorizeInitialAdmissionAsync`. The outer submit resumes on the renderer and compares admission generation; the retry's separate second read currently uses `ConfigureAwait(false)` and omits the caller-side admission-generation comparison. Reuse all existing attempt evidence, confirmation, availability and tracker delivery guards.
- Test map: `tests/Hexalith.Tenants.UI.Tests/Components/RemoveTenantMemberFlowTests.cs`, `QueuedInitialRemovalAuthorityCannotAdmitChangedGeneration`, `RenderLeaseRecoveryFlow`, and `StubTenantCommandGateway`. Reuse the controlled renderer blocker and authority task fixture, but establish a genuine `IsAmbiguousFailure=true`, null-correlation `RequestSent` attempt and target its retry's second authority read.
- Validation: reproduce using the new focused maintained MTP test lane before any runtime fix; run the focused removal class and full UI project after correction. Build Debug with source references, warnings as errors, and serialized MSBuild. Run story-gitlink/whitespace checks and verify exact ledger/index coverage. Record exact commands, initial failures and limits in `dw-648-validation-evidence.md`; close only DW-648 after the relevant full test project passes and independent review findings are resolved.
- Baseline: isolated task-owned Aspire started before code changes; initial resources were 13 Waiting/10 Running, then 16 Running/seven Finished with security readiness Healthy. This is a runtime baseline, not a live-browser reproduction. The baseline Debug source build passed with zero warnings/errors. New four-case ordering theory against unchanged runtime: two pending-authority cases passed; both successful queued-authority cases failed because an obsolete retry dispatched a second removal. The gateway resolver barrier prevents the old off-renderer path racing ahead of the controlled change-and-restore transition.
- Minimal correction: the retry's authority await resumes on the renderer with `ConfigureAwait(true)` and compares the captured admission generation before the existing delivery guards. All other await, gateway, cancellation, identity, tracker and localization behavior is retained. The initial four-case recheck passes. Added four real ambiguous-retry authority-refusal/recovery cases covering denied, indeterminate, faulted and canceled results, exact retained identity/ownership, safe feedback and Accepted without premature Confirmed.
- Expanded Debug source build passes with zero warnings/errors; the entire removal flow class passes 229/229 with zero failures/skips. Initial story-gitlink validation rejected the spec because the template omitted `baseline_commit`; added the exact canonical entry revision to frontmatter and retained the failed command in validation evidence.
- Initial full UI passes 6251/6251, zero failures/skips. Blind Hunter returned five qualification gaps: successful suspended retry; disposal; provider/version replacement; tenant/member/attempt replacement; and full status/projection recovery. Added eleven regression cases: suspended unchanged-authority recovery through Accepted, ProjectionPending and Confirmed with separate audit evidence; pending/queued provider/version replacement with restoration; tenant/member replacement preserving replacement snapshots and old ownership; and disposal/remount preserving same-ID recovery. All changes remain in the original two source/test files; runtime correction is unchanged.
- Initial expanded review suite: 238/240 passed, two disposal-fixture failures, zero skips. Calling bUnit wrapper cleanup inside the held renderer did not establish component disposal before remount. Corrected the fixture to invoke the component's `IDisposable` boundary explicitly before replacement, matching existing repository disposal tests. No additional runtime change was made. Independent read-only recheck accepts all five review additions; execution recheck remains required.
- Final acceptance: corrected Debug source build has zero warnings/errors, the complete removal class passes 240/240, and the final full UI project passes 6262/6262 with zero failures/skips. All nineteen new cases pass. Only DW-648 closes after independent review/recheck; 648 IDs remain with 355 done/293 open and 264 exact-covering maintenance rows. Preservation validation confirms every unrelated ledger body/index row, archive/sprint, dependency pointers, entry HEAD, empty staging and required source line endings. Task-owned Aspire stopped successfully. Exact commands, initial failures and qualification limits are retained in `dw-648-validation-evidence.md`; release remains not ready. No version-control mutation was authorized or performed.

## Review Triage Log

- Medium, patch: successful recovery used completed authority tasks and could miss blanket refusal of suspended retries. Added a genuinely suspended second authority read with unchanged safety and successful same-ID delivery; independent recheck accepts it.
- Medium, patch: disposal during this separate retry path lacked coverage. Added pending/queued disposal plus remount, no late dispatch, retained ownership and deliberate same-ID recovery. The initial fixture failures and corrected explicit disposal are retained above and in validation evidence; independent source recheck accepts the scenarios.
- Medium, patch: confirmation/freshness alone did not qualify provider/version replacement. Added pending/queued provider and version transitions, restoring original values before consumption, with no obsolete dispatch and deliberate recovery; independent recheck accepts them.
- Medium, patch: retained target/attempt context remained unchanged in the original tests. Added pending/queued tenant/member replacement and disposed-owner adoption, asserting replacement snapshots and ownership remain unchanged by late results; independent recheck accepts them.
- Medium, patch: successful same-ID recovery stopped at Accepted. Added verified completion with insufficient projection (ProjectionPending), then matching projection (Confirmed), aggregate ownership release, and no invented AuditAvailable; independent recheck accepts it.
