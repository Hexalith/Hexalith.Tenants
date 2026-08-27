---
title: 'Deferred Work: Support-Safe Copy Follow-up'
type: 'bugfix'
created: '2026-08-27'
status: 'done'
baseline_revision: 'c6a722cb94813d233a72a86ace26d36d4ac10d42'
baseline_commit: 'c6a722cb94813d233a72a86ace26d36d4ac10d42'
review_loop_iteration: 0
followup_review_recommended: true
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/.bmad-loop/runs/20260827-213738-29ba/bundles/support-safe-copy-followup/intent.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md'
warnings:
  - multiple-goals
deferred: []
---

<intent-contract>

## Intent

**Problem:** Story 1.8 left audit copy surfaces with three support-safety gaps: an unsafe or alternate audit reference can remain visible, the receipt copies a hidden English composite instead of its visible localized reference, and clipboard import/write operations can race component disposal. The exhausted follow-up-review recommendation also requires a fresh independent review of the hardened result.

**Approach:** Make each audit copy value identical to one support-safe visible literal, assemble receipt copy text at the localized rendering boundary, and serialize clipboard-module lifetime with import/write/disposal before running focused and independent review evidence.

## Boundaries & Constraints

**Always:** Preserve exact approved literals without normalization; fail closed for unsafe audit values; use the same literal for rendered text and clipboard input; keep EN/FR resource parity, existing Fluent controls, stable selectors, truthful feedback, and support-safe exception handling; coordinate disposal so no module is disposed during import/write and no interop or feedback starts after disposal.

**Block If:** Correct behavior requires a backend/public contract, a new support-safety policy decision, changes inside `references/`, or editing the deferred-work ledger.

**Never:** Render, copy, announce, log, or retain raw unsafe references, hidden alternate composites, clipboard exceptions, tokens, payloads, metadata, correlations, cursors, ETags, stack traces, or PII; add browser storage/backend calls, legacy clipboard fallback, dependency changes, or ledger edits.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Safe audit grid reference | Approved event reference plus approved context | Grid renders one exact approved reference literal and copy writes that identical literal | Omit alternate/raw content |
| Unsafe audit grid reference | Reference or composed label fails support-safety | Raw value is absent from visible/DOM copy surfaces and no copy control renders | Show a neutral unavailable placeholder |
| Localized receipt reference | Ready receipt under EN or FR | One localized visible reference literal is passed unchanged to clipboard | Partial/unsafe receipt renders no copy control |
| Disposal during import/write | Clipboard operation is suspended when navigation disposes the component | Disposal invalidates new work, waits for in-flight interop, then disposes the resolved module once | Suppress post-disposal feedback; tolerate circuit disconnection |

</intent-contract>

## Code Map

- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditDataGrid.razor:63` -- currently sanitizes only the copy argument while `ReferenceLabel` and row marker retain raw input; derive one approved rendered/copied literal here.
- `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditReceipt.cs:24` -- owns the hidden `CopyableReferenceText` constructor field and hard-coded English multiline builder; keep only support-safe receipt data and remove hidden presentation assembly.
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceReceipt.razor:24` -- localized outer surface; compose one whole-string reference literal and use it for both visible reference and `SupportSafeCopyButton.Value`.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` and `.fr.resx` -- add parity-checked whole-string receipt-reference copy with a placeholder.
- `src/Hexalith.Tenants.UI/Components/Shared/SupportSafeCopyButton.razor:92` -- import/write are serialized per activation but disposal neither invalidates nor waits for them; add a disposal-aware interop lifetime barrier.
- `tests/Hexalith.Tenants.UI.Tests/Components/SupportSafeCopyButtonTests.cs:253` -- reuse controllable import/write runtime to prove both disposal races and single module disposal.
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs:248`, `Components/AuditEvidenceReceiptTests.cs:24`, and `State/TenantAuditReceiptTests.cs:17` -- outer grid, localized receipt, and model regressions; assert exact visible-to-clipboard equality and absence of hidden/raw content.
- `.bmad-loop/runs/20260827-213738-29ba/bundles/support-safe-copy-followup/intent.md` and `_bmad-output/implementation-artifacts/deferred-work.md` -- read-only intent/evidence; never edit either file.

## Tasks & Acceptance

**Execution:**
- [x] `AuditDataGrid.razor` and `TenantAuditPageTests.cs` -- gate the composed rendered reference and row marker through the existing audit safety policy, copy exactly the rendered literal, and cover safe/unsafe DOM behavior.
- [x] `TenantAuditReceipt.cs`, `AuditEvidenceReceipt.razor`, both resource files, `TenantAuditReceiptTests.cs`, and `AuditEvidenceReceiptTests.cs` -- remove hidden English copy assembly, create one visible EN/FR whole-string reference literal, and prove the clipboard argument equals it exactly.
- [x] `SupportSafeCopyButton.razor` and `SupportSafeCopyButtonTests.cs` -- invalidate new activation on disposal, coordinate module import/write/disposal, suppress late feedback, and test disposal while import and write are each pending.
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs`, `Components/AuditEvidenceReceiptTests.cs`, `Components/SupportSafeCopyButtonTests.cs`, and `State/TenantAuditReceiptTests.cs` -- retain support-safety and prohibited-recovery terminology guards; the workflow then runs the independent review required by DW-1 without altering the ledger.

**Acceptance Criteria:**
- Given a safe or unsafe audit row, when the audit grid renders and copy is attempted, then the outer DOM contains only one approved visible reference literal, the clipboard receives that exact literal when eligible, and unsafe raw text produces neither a reference/copy surface nor hidden marker value.
- Given a ready receipt in English or French, when its reference renders and copy is activated, then the visible whole-string localized literal and clipboard argument are ordinally identical and no synthesized multiline English summary exists in component state.
- Given clipboard import or write is in flight, when the component is disposed, then disposal blocks until that operation exits, disposes the resolved module exactly once, permits no later interop or result publication, and completes without leaking exception/value detail.
- Given the completed bundle, when focused tests, full UI tests, solution build, support-safety scans, and independent review run, then all pass with no unresolved in-scope finding and neither the bundle intent nor deferred-work ledger has changed.

## Spec Change Log

## Review Triage Log

### 2026-08-27 — Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 9: (high 3, medium 5, low 1)
- defer: 0
- reject: 8: (high 0, medium 3, low 5)
- addressed_findings:
  - `[high]` `[patch]` Made clipboard import/write initiation atomic with disposal, drained in-flight interop before one shared module disposal, and gave concurrent disposal callers one consistent outcome.
  - `[high]` `[patch]` Suppressed audit row markers and correction-focus attributes whenever the composed event-reference/context literal fails support-safety.
  - `[high]` `[patch]` Sanitized directly constructed receipt fallback values so unsafe references cannot remain visible when no approved localized literal exists.
  - `[medium]` `[patch]` Restricted receipt copy controls to `Ready` receipts even when a non-ready receipt carries an otherwise safe reference.
  - `[medium]` `[patch]` Revalidated the final localized receipt literal through the approved-reference policy before rendering or copying it.
  - `[medium]` `[patch]` Strengthened grid regressions to assert ordinal visible-to-clipboard equality and safe-event/unsafe-context DOM omission.
  - `[medium]` `[patch]` Added receipt regressions for partial, invalid, directly unsafe, and unsafe-localization states.
  - `[medium]` `[patch]` Added clipboard lifecycle regressions for post-disposal activation, concurrent disposal, known teardown faults, and consistent unexpected-fault propagation.
  - `[low]` `[patch]` Extended verification to whitespace-check the new untracked specification as well as tracked diffs.

## Design Notes

The component-local receipt literal is the localization boundary; the state model must not synthesize presentation copy. A disposal barrier must cover the entire module acquisition/write critical section so an import that resolves after disposal starts is still disposed exactly once.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-restore -warnaserror -m:1 -nr:false` -- expected: zero warnings/errors.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.Components.SupportSafeCopyButtonTests -class Hexalith.Tenants.UI.Tests.Components.TenantAuditPageTests -class Hexalith.Tenants.UI.Tests.Components.AuditEvidenceReceiptTests -class Hexalith.Tenants.UI.Tests.State.TenantAuditReceiptTests` -- expected: all focused tests pass.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` -- expected: full UI suite passes.
- `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -warnaserror -m:1 -nr:false` -- expected: zero warnings/errors.
- `git diff --check && (git diff --no-index --check /dev/null _bmad-output/implementation-artifacts/spec-deferred-work-support-safe-copy-followup.md; spec_diff_status=$?; test "$spec_diff_status" -eq 1) && git diff --exit-code -- .bmad-loop/runs/20260827-213738-29ba/bundles/support-safe-copy-followup/intent.md _bmad-output/implementation-artifacts/deferred-work.md` -- expected: no whitespace defects, the untracked spec itself passes the whitespace check, and both read-only files remain unchanged.

## Auto Run Result

Status: done

Summary: The audit grid now renders and copies one approved composed reference literal, receipts render and copy one policy-approved localized reference literal only when ready, and clipboard interop disposal is coordinated with import/write operations. An independent four-layer review was completed and all accepted findings were patched without editing the deferred-work ledger.

Files changed:
- `src/Hexalith.Tenants.UI/Components/Shared/SupportSafeCopyButton.razor` — coordinates activation, import, write, and shared module disposal while suppressing late feedback.
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditDataGrid.razor` — derives one support-safe reference literal and omits unsafe copy, row-marker, and correction metadata.
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceReceipt.razor` — creates, revalidates, renders, and copies one localized receipt reference literal for ready receipts.
- `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditReceipt.cs` — removes the hidden English presentation-copy composite from receipt state.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` — adds English localized reference and unavailable-placeholder literals.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx` — adds matching French localized literals.
- `tests/Hexalith.Tenants.UI.Tests/Components/SupportSafeCopyButtonTests.cs` — covers pending import/write disposal, repeated disposal, teardown faults, and post-disposal activation.
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs` — covers exact grid literal copying and unsafe DOM omission; refreshes the global-admin fixture for current lifecycle contracts.
- `tests/Hexalith.Tenants.UI.Tests/Components/AuditEvidenceReceiptTests.cs` — covers EN/FR visible-to-clipboard equality and unsafe/non-ready receipt states.
- `tests/Hexalith.Tenants.UI.Tests/Components/RemoveTenantMemberFlowTests.cs` — supplies the new receipt-localization key to the flow localizer fixture.
- `tests/Hexalith.Tenants.UI.Tests/State/TenantAuditReceiptTests.cs` — verifies support-safe receipt fields and absence of hidden presentation copy.
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/GlobalAdministratorsProjectionLoaderTests.cs` — repairs the continuation-cycle fixture call counter exposed by the full-suite run.
- `_bmad-output/implementation-artifacts/spec-deferred-work-support-safe-copy-followup.md` — records the implementation contract, review triage, and verification evidence.

Review findings: 9 patches applied (high 3, medium 5, low 1), 0 items deferred, and 8 items rejected as duplicate, pre-existing, out-of-intent, or non-actionable review noise (high 0, medium 3, low 5).

Follow-up review recommendation: true — patched findings were high 3, medium 5, low 1; the weighted medium/low score is 16 and high-severity patches independently require follow-up.

Verification performed:
- UI test project Release build: passed with 0 warnings and 0 errors.
- Focused support-safe grid, receipt, model, and clipboard tests: 139/139 passed.
- Full UI test suite: 2,484/2,484 passed.
- Solution Release build: passed with 0 warnings and 0 errors.
- Whitespace, untracked-spec, read-only intent/ledger, and story-gitlink checks: passed; no `references/` pointer changes were detected.

Residual risks: No known unresolved in-scope defect remains. Browser navigation teardown is represented by deterministic component-level import/write suspension tests rather than a separate end-to-end browser scenario.
