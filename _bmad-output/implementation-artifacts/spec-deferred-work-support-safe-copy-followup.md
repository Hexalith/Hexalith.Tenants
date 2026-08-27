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
deferred:
  - summary: >-
      French audit resource values spell "Reference" without its accent, so a French operator
      reads unaccented labels where the rest of the file is correctly accented.
    evidence: |-
      TenantsResources.fr.resx uses accented French throughout ("Non connecte" is spelled
      "Non connecté" at line 19, "Périmé" at line 85), and line 3707 already carries
      "Référence d'audit d'origine". The audit block is the exception: line 3374
      "Reference d'audit : {0}", line 3380 "Reference de commande", line 3389
      "Reference d'audit", line 3410 "Reference indisponible". Lines 3380 and 3389 predate
      this story, so the cluster is pre-existing rather than introduced here.
    location: >-
      src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx:3374
    severity: low
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

### 2026-08-27 -- Follow-up review pass
- intent_gap: 0
- bad_spec: 0
- patch: 7: (high 1, medium 4, low 2)
- defer: 1: (high 0, medium 0, low 1)
- reject: 15: (high 0, medium 5, low 10)
- addressed_findings:
  - `[high]` `[patch]` Restored circuit-loss tolerance in clipboard teardown: `IsKnownTeardownException` matched only `JSException or OperationCanceledException`, but `JSDisconnectedException` and `ObjectDisposedException` derive from `Exception` directly, so the one teardown race the pre-change `catch (JSDisconnectedException)` existed for now faulted the cached disposal task and escaped into `Renderer.HandleAsyncExceptions`. Both types are matched again.
  - `[medium]` `[patch]` Covered the regression above with `disconnected` and `object-disposed` cases on `Known_module_teardown_failures_complete_without_disclosing_details`; reverting the source fix fails exactly those two.
  - `[medium]` `[patch]` Gated the grid reference literal on a support-safe bare `EventReference` before composing it with the context: a composed `" - {context}"` literal passed the policy when the reference was empty, rendering and copying a literal that carried no audit reference and leaving the row without its `data-audit-reference` anchor. Added `Tenant_audit_page_omits_reference_surfaces_when_only_the_context_is_present`.
  - `[medium]` `[patch]` Refused any localized receipt literal that does not contain the audit reference: a missing or placeholder-less resource makes `IStringLocalizer` echo the key, which the safety policy accepts, so both the visible field and the clipboard would have carried a reference-less resource key. Added `Receipt_component_rejects_a_localized_literal_that_drops_the_audit_reference`.
  - `[medium]` `[patch]` Added `Disposal_started_on_the_renderer_dispatcher_drains_pending_write_once`: every existing disposal-race test called `DisposeAsync` from the free xUnit thread, so the renderer-dispatcher ordering that navigation actually produces was unexercised.
  - `[medium]` `[patch]` Restored PII regression coverage lost when `[InlineData("person@example.test")]` was dropped, retargeted to where disclosure is possible. Actor and target are caller-supplied user identifiers whose policy deliberately admits an e-mail shape (which is why the original row could not be restored as-is); the approved-reference policy must still refuse it, now proven at the model (`Receipt_blocks_a_pii_shaped_command_reference`) and on the rendered receipt (`Receipt_component_never_renders_a_pii_shaped_command_reference`).
  - `[low]` `[patch]` Restored attribute alignment on `Width="190px"` in the `audit-correction` `TemplateColumn`.
  - `[low]` `[patch]` Corrected this spec's Verification commands: the spec is tracked as of `ead00b0c`, and the previous whitespace probe (`git diff --no-index --check /dev/null <spec>`) exited `1` for "files differ" regardless of whitespace defects, so it could never fail. The read-only assertion over `deferred-work.md` was dropped because the ledger is orchestrator-owned.

## Design Notes

The component-local receipt literal is the localization boundary; the state model must not synthesize presentation copy. A disposal barrier must cover the entire module acquisition/write critical section so an import that resolves after disposal starts is still disposed exactly once.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-restore -warnaserror -m:1 -nr:false` -- expected: zero warnings/errors.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.Components.SupportSafeCopyButtonTests -class Hexalith.Tenants.UI.Tests.Components.TenantAuditPageTests -class Hexalith.Tenants.UI.Tests.Components.AuditEvidenceReceiptTests -class Hexalith.Tenants.UI.Tests.State.TenantAuditReceiptTests` -- expected: all focused tests pass.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` -- expected: full UI suite passes.
- `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -warnaserror -m:1 -nr:false` -- expected: zero warnings/errors.
- `git diff --check` -- expected: no whitespace defects. (The spec is tracked as of `ead00b0c`, so the earlier `--no-index` probe against `/dev/null` no longer applies; that probe also exited `1` for "files differ" whether or not whitespace defects existed, so it could not distinguish pass from fail.)
- `git diff --exit-code -- .bmad-loop/runs/20260827-213738-29ba/bundles/support-safe-copy-followup/intent.md` -- expected: the read-only bundle intent remains unchanged. `_bmad-output/implementation-artifacts/deferred-work.md` is deliberately excluded: the ledger is orchestrator-owned and the orchestrator records entry resolutions there outside this workflow.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-deferred-work-support-safe-copy-followup.md` -- expected: PASS with no `references/` pointer changes in range.

## Auto Run Result

Status: done

Summary: Independent four-layer follow-up review of the hardened support-safe copy bundle. The review found one high-severity regression introduced by the previous pass -- the clipboard disposal path had stopped tolerating `JSDisconnectedException`, the exact circuit-loss race the pre-change code handled -- plus two fail-open gaps where a support-safe literal could render and be copied without carrying any audit reference. All seven accepted findings were patched and the three behavioural fixes were mutation-verified. The deferred-work ledger and the bundle intent were not edited.

Files changed in this pass:
- `src/Hexalith.Tenants.UI/Components/Shared/SupportSafeCopyButton.razor` -- matches `JSDisconnectedException` and `ObjectDisposedException` as known teardown exceptions again.
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditDataGrid.razor` -- requires a support-safe bare event reference before composing the rendered/copied literal; restores attribute alignment.
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceReceipt.razor` -- rejects a localized literal that does not contain the audit reference.
- `tests/Hexalith.Tenants.UI.Tests/Components/SupportSafeCopyButtonTests.cs` -- adds disconnected/object-disposed teardown cases and a renderer-dispatcher disposal regression.
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs` -- adds the context-only (reference-less) row regression.
- `tests/Hexalith.Tenants.UI.Tests/Components/AuditEvidenceReceiptTests.cs` -- adds the reference-less localized literal and rendered-PII regressions.
- `tests/Hexalith.Tenants.UI.Tests/State/TenantAuditReceiptTests.cs` -- adds the PII-shaped command-reference model regression.
- `_bmad-output/implementation-artifacts/spec-deferred-work-support-safe-copy-followup.md` -- records this triage pass, the deferred item, and corrected verification commands.

Carried forward from the implementation pass (unchanged here): `State/TenantAudit/TenantAuditReceipt.cs`, both `TenantsResources` resource files, `RemoveTenantMemberFlowTests.cs`, and `GlobalAdministratorsProjectionLoaderTests.cs`.

Review findings: 7 patches applied (high 1, medium 4, low 2), 1 item deferred (low), 15 items rejected (medium 5, low 10) as duplicate, pre-existing, intent-implementing, or factually incorrect. Two reviewer claims were checked and found wrong: the unsafe-value theory's parameter is still load-bearing (`CommandReference.ShouldBeNull()` fails if the policy admits the input), and no `references/` gitlink moved in range.

Follow-up review recommendation: true -- patched findings were high 1, medium 4, low 2; a high-severity patch independently requires follow-up, and the weighted medium/low score is 14.

Verification performed:
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-restore -warnaserror -m:1 -nr:false` -- 0 warnings, 0 errors.
- Focused clipboard/grid/receipt/model tests -- 143/143 passed (was 139 before this pass).
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` -- 2,491/2,491 passed (was 2,484).
- `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -warnaserror -m:1 -nr:false` -- 0 warnings, 0 errors.
- `git diff --check` -- clean. Read-only bundle intent unchanged. `python3 scripts/validate-story-gitlinks.py` -- PASS, no `references/` pointer changes in range.
- Mutation verification: reverting the teardown predicate fails the two new teardown cases (stack trace confirms the fault reaching `Renderer.HandleAsyncExceptions`); reverting the grid gate fails the context-only row regression; reverting the receipt containment guard fails the reference-less literal regression.
- An interim attempt to assert PII non-disclosure via `receipt.ToString()` was rejected by the repository's own `SupportSafetyEvidenceGateTests`; the evidence was moved to rendered markup, which is the surface where disclosure is possible.

Residual risks: The renderer-dispatcher disposal regression starts disposal on the dispatcher but does not await it there, so a hypothetical deadlock in real circuit teardown (where the framework awaits the returned task) remains represented by component-level tests rather than an end-to-end browser scenario. The French audit resource block keeps its pre-existing unaccented "Reference" spellings; that is recorded in `deferred`.
