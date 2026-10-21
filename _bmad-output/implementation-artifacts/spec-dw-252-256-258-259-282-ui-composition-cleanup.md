---
title: 'Complete five deferred UI composition cleanup records'
type: 'refactor'
created: '2026-10-21'
status: 'done'
route: 'oneshot'
baseline_commit: 'c92cd4b15a80c3edb295093d3a1f426aa5a5636f'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Complete DW-252, DW-256, DW-258, DW-259 and DW-282 as one bounded UI composition cleanup. Retire unused internal projection-evidence entry points and unused create/metadata notification wrappers. Preserve the live authoritative status, projection and audit reconciliation paths, command identity, focus behavior and aggregate ownership. Resolve the workspace's scoped BFF composition consistently while reading current connectivity from that instance; reuse the existing authoritative-empty predicate.

Migrate tests that depend on retired seams without losing confirmation, late-status, receipt or foreign-lease coverage. Preserve historical ledger fields and unrelated records, validate the focused and full UI suites, independently review, and commit and push main as requested. Existing create/metadata notification feature gaps and release holds retain their separate scope.

</frozen-after-approval>

## Implementation Notes

- Planning: no unresolved intent choices, irreversible implementation work or new public API. Use the oneshot route for internal deletions, one cached scoped service reference, existing test migration and five-record ledger/index closeout. Commit/push is explicitly authorized.
- Code map: six `ApplyProjectionEvidence` declarations remain in CreateTenantFlow, AddTenantMemberFlow, ChangeTenantMemberRoleFlow, RemoveTenantMemberFlow, EditTenantMetadataFlow and SetTenantConfigurationFlow, with zero production calls. Remove-member tests and one retained-flow theory still invoke them; preserve their assertions through projection providers and the existing retained tracker advancement contract.
- TenantsWorkspace resolves ITenantsBffComposition in four places. Registration is scoped; cache the object during initialization, never connectivity/authorization values. The interface has no connectivity notification event. Live reads already re-evaluate connectivity on render/refresh; adding a notification contract belongs to separate work. The historical duplicate empty predicate is already centralized in IsAuthoritativeFirstTenantEmpty and remains in use.
- Read the Hexalith baseline, Git/UX guidance, repository build configuration, UI tests and architecture AD-1 through AD-7. Existing Aspire topology reports Running/Healthy before source changes. Raw configuration remains under /tmp.
- Removed six unused evidence wrappers and the unused create/metadata synchronous nudge wrappers. Preserved live member/configuration nudges, authoritative evidence providers and state-model confirmation methods.
- Migrated four removal cases to the live provider/nudge route; migrated the four-kind held-status theory and the removal receipt race to retained tracker advancement, preserving existing assertions. The receipt race invokes the existing private audit-query method through test reflection; no new production ingress was added.
- Cached the scoped composition object once in workspace initialization and reused it at all four sites. Expanded the existing command-connectivity test through disconnected/connected/disconnected Refresh events; the existing shared empty-state predicate is unchanged.
- Initial Debug builds passed with zero warnings/errors. Focused removal 240/240, retained attempts 207/207 and workspace 136/136 pass with zero failures/skips. Final accepted Debug rebuild passed with zero warnings/errors in 14.18s; full UI run and independent review remain in progress.
- Ledger/source audit passes: all 657 IDs and 652 unrelated record bodies preserved, five records closed, 440 done/217 open across 198 exact-covering maintenance rows. Historical mutable fields are retained with pre-ui-composition-cleanup prefixes. Exact evidence and the reproducible audit are in dw-252-256-258-259-282-validation-evidence.md.
- Accepted full UI result: 7116/7116 pass, zero failures/skips, 1m48.258s. Run provenance is captured in the companion manifest. Gitlink guard and whitespace checks pass; pinned commitlint 21.2.3 accepts the exact candidate message with zero problems/warnings. Fetched main has 0/0 divergence.
- Review scope decisions: retirement is DW-252's expressly allowed alternative; existing explicit metadata refresh remains the supported UX. Historical observations are preserved. Composition connectivity remains Refresh/rerender-driven under the existing event-free interface, with live command and read getters. Neither retirement claims new notification integration.

## Review Triage Log

Blind Hunter reviewed 60.790 kB and reported eight findings (N = min(floor(sqrt(60.790) + 1), 10) = 8). All findings were checked against source and tests.

| Finding | Verdict and disposition | Evidence / correction |
| --- | --- | --- |
| 1. Retained-flow race bypasses the provider | low; patched scope clarity | Its predecessor also bypassed the provider through the obsolete wrapper. Renamed to RetainedConfirmationPublicationSurvivesLateProcessingStatusAndProtectsForeignAdmission. Live provider coverage remains in the flow suites; this test explicitly qualifies concurrent retained-state publication. |
| 2. Private receipt invocation cannot detect missing automatic assembly | low; scope clarified, extra orchestration rejected | The controlled retained-publication race does not claim automatic orchestration; the same predecessor invoked assembly from an unused wrapper. Renamed to RetainedRemovalConfirmationPreservesQualifiedReceiptAcrossLateStatusAndReleasesOnlyItsLease. Existing RenderConfirmedRemoval cases (including ExactRemovalProofAcceptsClockSkewWithOnlyOpaqueETagMetadataAndRedactsProvenance and Pending_audit_refresh_can_recover_later_ready_proof) qualify automatic assembly. An additional provider-driven concurrent race would invent a production path across the serialized refresh drain. |
| 3. Retained tracker not rechecked after late status | low; patched | Assert exact retained terminal object plus message ID, correlation ID and intent identity after releasing held status. |
| 4. Reconnected create not proven enabled | medium; patched | Fill a valid draft while disconnected; after Refresh prove the submit control enables, then disconnect/Refresh and prove it disables and a captured form submit causes zero gateway calls. |
| 5. Audit read connectivity transition untested | medium; patched | Added rendered audit-link disconnected/connected/disconnected Refresh regression against the same scoped composition instance. |
| 6. Metadata notification disposition is lost | false as an outstanding requirement; clarified | DW-252 explicitly permits retiring the unused evidence seam. The selected retirement removes no production caller. The original notification observation remains in legacy detail and preserved verification; retirement-decision explicitly records existing explicit-refresh behavior and future-feature scope. DW-200 remains separately open for create. No metadata notification feature is claimed or required for this cleanup. |
| 7. Refresh-only connectivity decision unclear | low; clarified | Record the existing Refresh/rerender contract in the two selected workspace records. The interface exposes no connectivity event; this refactor preserves existing behavior and reads both current getters after Refresh, rather than introducing a new notification feature. |
| 8. Ledger dictionary hides duplicate headings | medium; patched | Verify numeric heading uniqueness before dictionary construction. A deliberate duplicate preceding its original must raise; the real ledger still passes full history/count/coverage checks. |

No new finding is deferred. No frozen intent was changed.

Post-review validation: Debug source build has zero warnings/errors (15.67s); focused 584/584 pass, zero failures/skips. Final post-review full suite and independent recheck remain in progress.

Independent recheck accepts findings 1–7. Finding 8 recheck identified a colonless duplicate bypass in the one-off audit: final parsing and uniqueness now share the same block set. Normal, colonless and zero-padded numeric duplicate controls all fail closed; the actual audit passes. No production change or additional deferred finding.

Final independent recheck clears all eight findings: embedded/executed checker contents match; normal, colonless and zero-padded duplicate controls reject; actual ledger/source audit passes and matches stored JSON. No remaining review finding and no new deferred item.

Completed validation: reviewed source build 0 warnings/errors; focused 584/584 and full UI 7117/7117 pass with zero failures/skips. Final independent review is clear, ledger/source/gitlink/whitespace checks pass, and no new finding is deferred. The exact candidate revalidates under pinned commitlint; fetched main has 0/0 divergence.
