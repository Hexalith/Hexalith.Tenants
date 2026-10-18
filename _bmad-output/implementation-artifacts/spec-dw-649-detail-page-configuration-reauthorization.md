---
title: 'Align tenant detail configuration test with current preview authority'
type: 'bugfix'
created: '2026-10-17'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** DW-649 records a tenant detail page test stub that grants `TenantOwner` without checking the caller's membership. The page does not call that stub: it refreshes current preview evidence at submit, while the test's caller and member fixture do not match.

**Approach:** Align the successful test's caller and owner evidence, remove its unused unconditional-owner stub, and cover a submit-time preview that refuses dispatch after owner evidence is lost. Preserve the separate safe-model and member-bearing reauthorization contracts. Reconcile DW-649 using the executed result.

**Decision (2026-10-18):** The user chose the preview-only fixture repair. Do not wire the unused reauthorization callback into production as part of DW-649.

</frozen-after-approval>

## Implementation Notes

The first four bullets below preserve the original investigation plan. Its reauthorization instructions were disproved by the first focused run and are superseded by the 2026-10-18 decision correction.

- Route facts: No user-visible intent gap, irreversible operation, new public API, dependency, or production behavior change. This is a test-fixture and backlog-evidence repair.
- Code map: `TenantDetailPage.razor:2839` calls `ITenantsBffComposition.ReauthorizeConfigurationManagementAsync(snapshot.Detail, snapshot.Configuration)`. `TenantsBffComposition.cs:669` uses the member-bearing `TenantConfigurationSafeComposer.Reauthorize(TenantDetail, ...)`; the older safe-model-only overload deliberately returns `MissingPermission` for a non-global caller because it has no membership evidence. Preserve that distinction in the ledger explanation.
- Test map: `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs:3348` grants `TenantOwner` from a stub regardless of the detail; `Detail(...)` defaults to `owner-user` while `ReadyWithSafeConfiguration(...)` identifies caller `operator.alpha` and grants a render-time owner context. The nested `StubTenantsBffComposition` at about line 8945 currently reduces the detail overload to tenant ID/status. Add a narrowly scoped detail-aware test delegate or equivalent, and use the real composer with a validated `operator.alpha` billing policy. Make the successful detail contain `operator.alpha` as owner. Test that a newly missing owner at reauthorization yields `MissingPermission` and zero command dispatch while retaining the render-time grant as stale evidence.
- Existing `TenantConfigurationReadPolicyTests.Lifecycle_status_does_not_change_resolved_configuration_authority` already proves the composer behavior; `Detail_page_requires_fresh_safe_preview_evidence_on_set_submit` proves that page reauthorization runs. Preserve both and avoid broad changes to `ReadyWithSafeConfiguration`, which many unrelated scenarios use.
- Validation: run the focused page test class or methods and the complete UI test project under the pinned .NET 10 SDK; use the repository's Debug source-reference lane and warnings-as-errors. Record exact results. After independent review and passing gates, close only DW-649 in `_bmad-output/implementation-artifacts/deferred-work.md`, update its one index row in `docs/maintenance-backlog.md`, and preserve historical fields, unrelated records, archive, sprint, dependencies, and release holds.
- Replan trigger: The initial two-case test failed 0/2 because `ReauthorizeProvider` is declared by `TenantConfigurationManagement` but never invoked. The test edit was reverted. The baseline page success test depends on the submit-time `GetSetConfigurationPreviewAsync` result, whose production composer resolves current policy and owner membership from raw detail. A new page test should exercise that actual seam; wiring the unused callback would expand production behavior beyond the planned test repair. Exact first-failure command: `dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-method '*Detail_page_set_configuration_uses_current_member_authority_before_dispatch*' --minimum-expected-tests 2 --no-ansi --no-progress --results-directory /tmp/tenants-dw-649/focused --report-xunit-trx --report-xunit-trx-filename focused.trx` (exit 2, both cases waited for a callback count of one but observed zero). The Debug source build before that passed with zero warnings/errors.
- Decision correction (2026-10-18): The preceding pre-replan notes describe the disproved reauthorization plan and are retained for history. `TenantConfigurationManagement.razor:245` never invokes its callback. `SetTenantConfigurationFlow.razor:568` requests a fresh preview at submit, and `TenantQueryGateway.cs:418` plus `TenantsBffComposition.cs:708` resolve current detail, policy, and owner membership for that preview. Scope is now the successful fixture and a preview-refusal page regression; the user explicitly selected this path.
- Implementation: The page theory now uses `operator.alpha` as the initial owner and models a change to `owner-user` before submit; the preview provider returns unavailable after that change. The old page test's false reauthorization comment and unused delegate were removed; the nested composition stub now returns unavailable for its interface method. The initial preview-based build found CS0117 from naming a nonexistent `Blocked` lifecycle state; the assertion uses the `UnableToVerify` state produced by `TenantSetConfigurationCommandSnapshot.Blocked`.
- Final validation: Debug source-reference build passed with zero warnings/errors. The two new page cases passed 2/2, and the final full UI rerun after review corrections passed 6264/6264 with zero failures/skips. `dw-649-validation-evidence.md` records exact commands, initial failures, review, and limits. Only DW-649 closes; 649 ledger IDs remain, 358 done/291 open, with 263 index rows.

## Review Triage Log

- low, patch: the spec was still `in-progress` while the ledger described completion; the final spec now says `done` after review and the final test run.
- medium, patch: pre-replan notes still instructed the disproved reauthorization approach. They are explicitly labeled as superseded investigation, and the active implementation decision follows them.
- low, patch: closure was recorded before independent review completed. This review and its correction run are now in the evidence file, and the spec is finalized only after both.
- medium, patch: an older page test claimed the unused callback ran. Its name and comment now describe the fresh preview it actually tests; its unused callback setup and the nested stub's dead tracking were removed.
- low, patch: DW-649 verification links omitted the gateway and composition path. The record now preserves its previous links and names the actual preview path and production tests.
- false: the reviewer requested a query-gateway-to-composer integration case. The closure claim is limited to a controlled page preview refusal, with separate existing composition tests proving owner membership checks; the evidence explicitly states that no combined gateway execution was run. An additional integration fixture is outside this selected fixture repair.
