---
title: 'DW-186: Distinguish projection lifecycle availability reasons'
type: 'bugfix'
created: '2026-10-21'
status: 'done'
route: 'oneshot'
review_loop_iteration: 1
baseline_commit: '2ac783647336594787b1dc5cdde94dc4cd1b302c'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** DW-186 records an approved dedicated projection-lifecycle category, but lifecycle availability still groups non-current projection lifecycle with stale read freshness. Its rendered reason also loses the existing lifecycle-specific localized explanation.

**Approach:** Add a lifecycle-specific presentation category for non-current projection lifecycle when authority and read evidence otherwise qualify. Preserve the six canonical shared unavailable reasons, command eligibility, precedence, refresh recovery and existing enum values. Render the dedicated automation category and existing whole-string EN/FR lifecycle explanation; add matching category resources. Qualify both typed and legacy inputs, earlier blockers and current-state recovery. Close only DW-186 after focused/full UI verification and independent review, then commit and push main as requested.

</frozen-after-approval>

## Implementation Notes

- Clean main entry and full canonical baseline recorded above. This freeform maintenance has no sprint story key. DW-180 remains uncertain; earlier open records retain external dependencies or separate contracts. DW-186 is the next dependency-free actionable Tenants item after DW-181.
- No unresolved user-visible choice or migration is required. Small existing-state/resource/rendering change uses the oneshot route. Commit and push are explicitly authorized.
- Read root AGENTS, permitted LLM/Git/UX/state baseline, editor/attributes/build/test configuration, architecture AD-3/7/8/9/11, truth-state specification, ledger and index. Independent read-only investigation identifies both compatibility and actual rendered category paths. The canonical taxonomy stays unchanged; the dedicated category is a lifecycle-specific presentation subtype of StaleData.
- Code map: TenantLifecycleAvailability.FromEvidence derives compatibility category and SafeMessageKey; TenantLifecycleActionAvailability.ReasonCategory/HighImpactReason own rendered category/copy; TenantLifecycleUnavailableReasonCategory gets an appended member. Existing lifecycle resources already supply a whole-string specific explanation with tenant/operation placeholders. Preserve retained-attempt and temporary connectivity overrides.
- Tests reuse TenantLifecycleAvailabilityTests and the FluentBunitContext lifecycle fixture with real localization. Given qualified read/authority evidence and a non-current lifecycle, when evaluated/rendered, then actions remain disabled with ProjectionLifecycle category, specific EN/FR reason and Refresh focus. Given earlier permission/read/freshness blockers, when evaluated/rendered, then their original categories prevail. Given current lifecycle returns, then the intended action becomes available. Include undefined lifecycle and refreshing with/without baseline.
- Baseline Debug source build passes with zero warnings/errors. Isolated Aspire start succeeds; initial waiting resources settle to 23 Running/Healthy. Error telemetry sample contains Memories registry initialization errors, so resource readiness is not successful initialization or functional/release evidence. No AppHost/dependency changes are required.
- Added 65 rendered/evaluation cases: 56 non-current lifecycle EN/FR combinations across legacy and typed inputs (including aging/refreshing with baseline and undefined lifecycle), plus nine earlier-blocker controls. Entry-code replay builds cleanly and fails all 56 intended category cases while the nine controls pass. Production change appends only the compatibility enum member, derives the subtype from qualified evidence and reuses the existing specific whole-string reason in rendering. Shared evaluator and recovery remain unchanged.
- First production build reports CS0103 because the availability file lacks the ProjectionLifecycleState namespace import. Add the missing using; corrected Debug source build has zero warnings/errors. An intervening no-build focused invocation runs old binaries and is not fixed-code evidence. Corrected focused project runner passes 65/65 with zero failures/skips; full UI verification is running.
- Concurrent external archiving modifies deferred-work.md and deferred-work-archive.md after clean entry. Preserve it; no archiving or external ledger hunks belong to this change. Task-owned reconciliation will target only DW-186, with selective staging if external changes remain uncommitted.
- Initial full UI passes 6690/6690 with zero failures/skips; scoped formatting applies the required CRLF line endings and then passes. Independent review identifies five concrete fixture/presentation gaps. Accept all: supply real detail and prove blocked/allowed launch, derive compatibility reason key from the category predicate, apply the same presentation to an open confirmation, qualify legacy aging, and assert EN/FR earlier-blocker copy/recovery. Only lifecycle presentation extends to the existing command flow; shared eligibility/recovery remain untouched. An old open-flow assertion is updated to the now-specific lifecycle explanation.

## Review Triage Log

- Low / patch: negative launcher proof lacked Detail, allowing a separate render guard to hide an incorrectly opened action. Supply matching detail, assert no flow component while blocked and an actual flow after recovery.
- Low / patch: undefined freshness kept an old projection-lifecycle compatibility message under StaleData. Derive the compatibility key from the same category selection and assert earlier-blocker keys/copy.
- Low / patch: a retained open confirmation rendered generic stale copy after projection lifecycle changed. Apply the dedicated localized presentation to both current and captured preview evidence, preserving shared disable/recovery behavior; add actual open-form transitions in both directions/languages/input paths.
- Low / patch: legacy tests covered only current freshness because their fixture hardcoded it. Add aging rows and map supported aging freshness into actual component parameters.
- Low / patch: earlier-blocker controls lacked rendered reason/recovery checks. Add EN/FR exact shipped-copy checks for both and retain authority-first nondisclosure.

- Independent read-only correction recheck accepts all five fixes with no unresolved concrete issue. Revised Debug source build has zero warnings/errors; focused lifecycle 358/358 passes, including all 96 dedicated cases. Scoped formatter also repairs the existing changed fixture's CRLF and two conditional indentation diagnostics, then passes across all touched C# files. No shared evaluator or eligibility behavior changes.
- Full reviewed-source UI passes 6721/6721 with zero failures/skips. Only DW-186 closes; all 656 IDs remain, 403 done/253 open, indexed exactly once by 228 maintenance rows. Every unrelated record body and the concurrent external archive remain preserved. Selective staging uses a HEAD-based patch containing only the DW-186 closure heading/record, leaving the external archive maintenance uncommitted. No new work is deferred.

## File List

- `src/Hexalith.Tenants.UI/State/TenantDetail/TenantLifecycleUnavailableReasonCategory.cs` — append the compatibility category.
- `src/Hexalith.Tenants.UI/State/TenantDetail/TenantLifecycleAvailability.cs` — derive category and compatibility explanation together.
- `src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleActionAvailability.razor` — render dedicated category and whole-string reason.
- `src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleCommandFlow.razor` — keep open-confirmation presentation consistent.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` — English category label.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx` — French category label.
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantLifecycleProjectionCategoryTests.cs` — 96 focused rendered/state regressions.
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantLifecycleActionAvailabilityTests.cs` — update open-flow copy expectation and scoped whitespace.
- `tests/Hexalith.Tenants.UI.Tests/State/TenantLifecycleAvailabilityTests.cs` — update category and cover undefined lifecycle.
- `docs/tenants-ui-truth-state-and-action-availability-spec.md` — document the supplemental lifecycle presentation contract.
- `docs/maintenance-backlog.md` — remove only the completed action row.
- `_bmad-output/implementation-artifacts/deferred-work.md` — reconcile only DW-186.
- `_bmad-output/implementation-artifacts/dw-186-validation-evidence.md` — exact verification, review and limits.
- `_bmad-output/implementation-artifacts/spec-dw-186-projection-lifecycle-category.md` — workflow state and decisions.
