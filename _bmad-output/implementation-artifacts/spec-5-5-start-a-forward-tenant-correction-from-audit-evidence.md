---
title: 'Start a forward tenant correction from audit evidence'
type: 'feature'
created: '2026-10-01'
status: 'in-review'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 4f426e591c85825eb41ec4b289a43e91f2085004
context:
  - AGENTS.md
  - _bmad-output/project-context.md
  - _bmad-output/implementation-artifacts/epic-5-context.md
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The UI infers roles, assumes authority, blocks valid recovery, and submits on preview handoff. Story 5.5 needs a non-submitting start.

**Approach:** Reuse the receipt/grid entry points, refresh current projection and circuit authority through the BFF, require deliberate role selection, and open a separate start panel carrying safe evidence and an intent for Story 5.6.

## Boundaries & Constraints

**Always:** Require matching complete safe receipt, literal tenant/target, direct current projection provenance/lifecycle, current owner/global authority, command support and measured safe width. Preserve Story 5.4 recovery, original evidence, paging/order, generation ownership, focus and EN/FR localization. Empty recovery requires global authority and explicit Owner without asserting absent history. Preserve existing 5.6/5.7 APIs/components.

**Never:** Infer roles from history; treat empty membership as a bootstrap authorization bypass; enable global-administrator correction through this tenant start flow; submit, poll status, confirm success, or manufacture proof. No new backend endpoints, contract/history fields, dependencies, persistence edits, browser egress/storage, raw detail/member collections, configuration, claims, payloads, ETags, cursors, internal versions/correlations, or machine-token copy in new rendered state. No `undo`, `rollback`, or `hidden edit` wording.

## I/O & Edge-Case Matrix

| State | Result after explicit role selection | Recovery |
|---|---|---|
| Removal; target absent | AddUserToTenant intent | Review handoff |
| Removal; target present, different role | ChangeUserRole intent; deliberate current/intended review | Review handoff |
| Role-change outcome; target present, different role | ChangeUserRole intent | Review handoff |
| Same current/intended role | Already applied; no command intent | Inspect audit/read-only |
| Empty current membership | Owner-only recovery; current global authority required | Choose Owner/request permission |
| Absent role-change target; Unknown role; disabled/unknown lifecycle; conflicting scope | Block with localized reason | Refresh/supported path/read-only |
| Incomplete audit; stale/degraded/unknown projection; lost authority/support; unsafe width | Block; retain original receipt | Applicable canonical recovery |
| Global authority/unsupported outcome | Localized unsupported/high-impact-not-ready | Read-only; Story 5.7 owns correction |

</frozen-after-approval>

## Code Map

Paths below are relative to `src/Hexalith.Tenants.UI/`; tests are under `tests/Hexalith.Tenants.UI.Tests/`.

- `Components/Pages/TenantAuditPage.razor`: reuse generations, viewport and receipt recovery; replace `IntendedRoleFromNarrative` and assumed eligibility.
- `State/TenantAudit/TenantCorrectionStartIntent.cs`: retain evaluator compatibility for 5.7; split touched co-located types into named files.
- `Services/Gateways/{TenantQueryGateway,TenantsBffComposition}.cs`: reuse typed reads/metadata, principal resolver and support-safe classifier.
- `Components/Tenants/Audit/{AuditDataGrid,AuditEvidenceReceipt}.razor`: existing grid picker/start. Retain `CorrectionStartPanel.razor` for 5.6; new `TenantCorrectionStartPanel.razor` hands off by callback to preview without submitting.
- `wwwroot/js/tenantsFocus.js`: existing focus helpers; distinguish receipt/grid origin. `Browser/validate-tenants-focus-browser.sh` provides Chromium checks.

## Tasks & Acceptance

**Execution:**
- [x] `Services/Gateways/{ITenantQueryGateway,TenantQueryGateway,ITenantsBffComposition,TenantsBffComposition}.cs`, new `State/TenantAudit/TenantCorrectionProjection.cs` -- safe capture without ETag/previous snapshot; resolve current authority and redact before returning. Malformed/null/duplicate members never imply absence.
- [x] `State/TenantAudit/TenantCorrectionStartIntent.cs` and named type files -- derive add/change/already-applied and owner-only empty recovery from verified current state; require explicit valid role and carry safe original timestamp/provenance/preview inputs.
- [x] `Components/Pages/TenantAuditPage.razor` -- remove historical role fallback; wire selection, authoritative activation refresh, conflicts and no-submit handoff; isolate global outcomes and invalidate selection on scope change.
- [x] `Components/Tenants/Audit/{AuditDataGrid,AuditEvidenceReceipt,TenantCorrectionStartPanel}.razor` and necessary scoped CSS, `wwwroot/js/tenantsFocus.js` -- expose deliberate role/current-state review, handoff, cancel/Escape and exact focus return, with stable start/role/reference/projection/command/handoff/reason selectors.
- [x] `Resources/TenantsResources{,.fr}.resx` -- localized fields, absolute timestamps with UTC offset, role choices, recovery/bootstrap context and reasons; no visible enum/provenance tokens.
- [x] `State/TenantCorrectionStartIntentTests.cs`, `Components/{TenantAuditPage,AuditEvidenceReceipt,AuditDataGridCorrection,TenantCorrectionStartPanel}Tests.cs`, `Services/Gateways/{TenantQueryGateway,TenantsBffComposition}Tests.cs`, and `Browser/` harness -- cover the matrix, races/permission loss, unsafe data, parity and real keyboard/responsive/focus behavior.

**Acceptance Criteria:**
- Given supported evidence and current authorized projection, when the operator deliberately selects a valid role and starts correction, then the matrix determines an intent and a reachable non-submitting handoff carrying original reference/timestamp, literal scope/target, current/intended role, safe projection provenance and required preview data.
- Given incomplete safety or global scope, when start is evaluated/refreshed, then localized reason/recovery and original evidence remain visible, without a stale intent.
- Given either launcher, when selection changes, start blocks or the panel closes/cancels/Escapes, then focus returns to that launcher or its visible reason/receipt fallback; accessible names, forced colors, reduced motion and responsive safety remain valid.
- Given start/handoff interactions, when tested with command spies and support-safety guards, then zero submissions/status polls/history mutations occur, and neither success nor linked proof is claimed.

## Implementation Notes

Implemented a redacted, unconditional BFF projection capture with one tenant read and one current principal resolution per audit page. Explicit selection determines add/change/already-applied; empty membership requires global authority and Owner. Grid and receipt open a separate non-submitting panel. Both activation and preview handoff refresh current authority/state, reject superseded generations and retain safe original evidence. The existing preview accepts the same redacted capture so an older cached detail cannot override it.

All six execution tasks and four acceptance criteria were checked against the complete baseline diff (including untracked files), component behavior and verification results. No backend contract, persistence, dependency or reference gitlink changes.

## Spec Change Log

## Review Triage Log

| ID | Verdict | Route | Evidence |
|---|---|---|---|
| B1 | high | patch | Retained notification refresh updates only the start intent. An open preview keeps its authorized `StartProjection`; pass the refreshed intent/capture into that preview while preserving its submitted lifecycle. |
| B2 | medium | patch | Handoff removes the focused start heading without scheduling preview focus; the existing preview focuses only terminal lifecycle changes. Schedule its focusable heading through the page's existing focus mechanism. |
| B3 | false | reject | Production rows are created by `TenantAuditRow.FromEntry`, whose narrative mapper converts unsafe user identifiers to null. The batch excludes those empty targets, so an unsafe raw audit payload cannot poison valid rows as claimed. |
| B4 | medium | patch | The evaluator can append AlreadyApplied or empty-recovery facts from a stale or scope-conflicting capture. Guard those conclusions with matching, current projection evidence; apply the same rule in the redacted preview resolver. |
| B5 | medium | patch | The receipt picker remains interactive when the page supplies NarrowViewportUnavailable, unlike the grid. Gate the receipt picker and role event on the same measured safety decision. |
| B6 | medium | patch | The origin filter also rejects receipt-open fallback buttons, which have only `data-receipt-focus-reference`. Restrict origin matching to correction controls and retain the existing receipt-launcher fallback. |
| B7 | medium | patch | The pending reference is captured before asynchronous import while origin is read afterward; a newer close can pair unrelated origins/references. Capture both together and reject superseded focus generations. |
| B8 | medium | patch | `WillRemoveReceiptCorrectionControl` reads mutable role/projection dictionaries after a ConfigureAwait(false) continuation. Make that decision in the generation-guarded dispatcher callback that installs captures. |
| B9 | false | reject | The browser artifact intentionally verifies rendered markup and shipped focus helpers; actual Blazor callbacks and viewport blocking are executed by `DeliberateStartAndSeparatePreviewHandoffNeverSubmitOrPoll`, cancel/Escape and page viewport tests. It is not claimed as an authorized live application E2E. The separate preview-focus and competing-picker gaps are retained as B2 and V2. Live application authorization remains an explicitly recorded preexisting limitation. |
| B10 | false | reject | The only production panel caller receives `Evaluate` output after authoritative refresh. That evaluator always writes currentRole for a present target and derives Change; the manually forged present-target/Add intent has no reachable production caller. |
| E1 | high | patch (B1 group) | Independent tracing confirms the same retained-preview defect as B1: newly revoked/stale/conflicting captures do not update the preview. |
| V1 | medium | patch | Pre-verified regression gap: no correction BFF test supplies a mismatched raw tenant, so removing the raw-detail scope guard escapes coverage. Add unavailable/authority/membership assertions for that input. |
| V2 | medium | patch | Pre-verified browser gap: synthetic Start-only launchers cannot detect loss of picker/reason priority. Include competing controls for both origins and assert exact focused controls. |

## Verification

- Build UI tests: `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1`; zero warnings/errors.
- Run focused new/affected classes through the built xUnit executable, then the full UI project with `dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false --no-build --no-restore`; all pass.
- Run the extended Chromium harness with Debug assets and actual browser start/handoff checks in EN/FR, narrow widths and forced colors; record results/blockers.
- `git diff --check` and `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`; clean. Record File List and sprint transition at implementation/review.

### Recorded results

- Debug/source-reference build: 0 warnings, 0 errors (`/tmp/story55-build-handoff.log`).
- Focused affected classes: 895 passed, 0 failed/skipped/not run (`/tmp/story55-focused-handoff.log`).
- Full UI project: 3,729 passed, 0 failed/skipped (`/tmp/story55-full-ui-handoff.log`).
- Actual rendered EN/FR Chromium fixtures: desktop, narrow width, forced colors, handoff/cancel/Escape, exact grid/receipt focus and zero browser egress passed. Existing focus and negative mutation checks passed (`/tmp/story55-browser-final.log`). Reduced-motion styling contains no animation and disables scrolling motion.
- Live Aspire baseline: resources healthy; authenticated tenant reads were rejected by the existing server-side query gateway. Live application validation remains limited by this preexisting authentication condition; rendered component/browser fixtures cover the correction interactions.

### Matrix audit

All covering tests below ran in the full UI project (zero skips), with affected classes included in the focused run.

| Matrix row | Passing coverage |
|---|---|
| Removal, absent | `CurrentMembershipDeterminesTheForwardCommand` (absent case) |
| Removal, present/different | `CurrentMembershipDeterminesTheForwardCommand` (present case), `PreviewHandoffRetainsFreshCaptureWhenEarlierDetailDisagrees` |
| Role-change, present/different | `CurrentMembershipDeterminesTheForwardCommand` (role-change case), `RoleChangeEvidenceRequiresDeliberateSelectionEvenWhenHistoricalRoleExists` |
| Same role | `MatchingCurrentAndIntendedRoleCarriesNoCommand` (both outcomes) |
| Empty membership | `EmptyMembershipRequiresExplicitOwnerAndCurrentGlobalAuthority`, `EmptyMembershipRequiresCurrentGlobalAuthorityAtTheBffBoundary`, `EmptyRecoveryDisclosesOnlyCurrentMembershipAndAuthority` |
| Absent/unknown target, lifecycle, scope | `RoleChangeRequiresAKnownCurrentTargetRole`, `Member_correction_blocks_when_tenant_lifecycle_is_not_active`, `ReceiptMismatchAndProjectionScopeMismatchFailClosed` |
| Incomplete/stale/lost gates | `ReceiptMismatchAndProjectionScopeMismatchFailClosed`, `DirectProjectionEvidenceMustBeCurrent`, `AuthoritySupportAndViewportLossBlockWithoutLosingOriginalEvidence`, `StartAndHandoffRecheckAuthorityAndCommandSupport`, existing lifecycle/viewport suites |
| Global/unsupported | `TenantAuditPageKeepsGlobalAdministratorCorrectionReadOnlyEvenWithAuthority`, `Global_admin_outcome_blocks_when_command_support_is_absent`, `Unsupported_outcome_fails_closed_without_command_selection` |

### File List

- `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditDataGrid.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceReceipt.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/TenantCorrectionStartPanel.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/TenantCorrectionStartPanel.razor.css`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantQueryGateway.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantsBffComposition.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs`
- `src/Hexalith.Tenants.UI/State/TenantAudit/TenantCorrectionCommandDomain.cs`
- `src/Hexalith.Tenants.UI/State/TenantAudit/TenantCorrectionCommandType.cs`
- `src/Hexalith.Tenants.UI/State/TenantAudit/TenantCorrectionProjection.cs`
- `src/Hexalith.Tenants.UI/State/TenantAudit/TenantCorrectionRoleSelection.cs`
- `src/Hexalith.Tenants.UI/State/TenantAudit/TenantCorrectionStartContext.cs`
- `src/Hexalith.Tenants.UI/State/TenantAudit/TenantCorrectionStartIntent.cs`
- `src/Hexalith.Tenants.UI/State/TenantAudit/TenantCorrectionUnavailableReason.cs`
- `src/Hexalith.Tenants.UI/wwwroot/js/tenantsFocus.js`
- `tests/Hexalith.Tenants.UI.Tests/Browser/tenant-correction-start-browser-validation.html`
- `tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh`
- `tests/Hexalith.Tenants.UI.Tests/Components/AuditDataGridCorrectionTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/AuditEvidenceReceiptTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/CorrectionStartPanelTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantCorrectionStartPanelTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantsBffCompositionTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/State/TenantCorrectionPreviewSnapshotTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/State/TenantCorrectionStartIntentTests.cs`
