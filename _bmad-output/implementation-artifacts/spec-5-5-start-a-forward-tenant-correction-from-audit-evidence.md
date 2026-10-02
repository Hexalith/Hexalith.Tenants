---
title: 'Start a forward tenant correction from audit evidence'
type: 'feature'
created: '2026-10-01'
status: 'done'
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

All six execution tasks and four acceptance criteria were checked against the complete baseline diff (including untracked files), component behavior and verification results. No backend contract or persistence changes. The committed implementation includes the two source-reference pointer updates recorded below; this resumed review preserves those existing build inputs and performs no submodule updates.

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
| R-B1 | high | patch | The new grid/receipt role callback clears the mounted preview even after it holds a command tracking handle. Read the child's existing snapshot and retain submitted previews when another start or role selection is attempted. |
| R-B2 | medium | patch | A pre-submit blocked snapshot created from refreshed intent has no SafeMessageKey, so the recovery predicate cannot accept later current evidence. Allow recovery of that specific untracked, intent-blocked snapshot while retaining submitted terminal states. |
| R-B3 | medium | patch | Redacted already-applied evidence clears the command but FromIntent maps it to UnableToVerify. Set the existing AlreadyApplied lifecycle for that verified reason at preview composition. |
| R-B4 | medium | patch | Handoff removes the start panel before its asynchronous read completes, losing its focus and cancellation controls. Keep the panel mounted during handoff and expose localized progress with cancellation. |
| R-B5 | medium | patch (R-E2 group) | A notification can supersede activation's shared projection generation after it clears the panel. Report whether activation installed an available intent and hand off only that result; retain the start surface during handoff. |
| R-B6 | high | defer | Final SubmitAsync already dispatches from cached preview facts in the baseline, without an immediate authority recheck. This unchanged Story 5.6 confirmation seam needs separate current-authority validation; 5.5 itself never dispatches. |
| R-B7 | medium | defer | The baseline preview and capture do not carry owner-count facts or an explicit last-owner warning. Domain command validation remains the last-owner enforcement boundary; the existing Story 5.6 consequence-preview work needs a separate redacted safety decision. |
| R-B8 | false | reject | The approved matrix explicitly blocks an absent UserRoleChanged target. Complete membership proves current absence, but does not establish the supported correction transition; CurrentStateIndeterminate is the required conservative result. |
| R-B9 | medium | patch | The tenant start page always blocks global commands, and the global projection field now has no rendered consumer. Stop its redundant load-time enrichment while preserving existing correction components and APIs. |
| R-B10 | false | reject | carried B9: the browser fixture is explicitly rendered-markup/focus-helper evidence, with actual callback behavior verified by bUnit. It is not described as an authenticated application E2E. |
| R-E1 | medium | patch | With no panel open, unsafe viewport handling schedules correction focus with a null reference and clears the receipt-heading focus just captured. Schedule launcher focus only when a panel reference exists. |
| R-E2 | medium | patch (R-B5 group) | A retained notification supersedes a pending handoff projection read; the old code has neither a surviving start surface nor an installed preview. Keep the start mounted and require a successfully installed activation result before handoff. |
| R-V1 | medium | patch | carried V1: the raw-tenant mismatch remains absent from composition tests. Preserve this prior verdict and complete the outstanding scope-boundary verification task. |
| R-V2 | medium | patch | The receipt-origin scenario selects in the grid first and bypasses the receipt callback. Select the role through the receipt component and verify its start intent. |
| R-V3 | high | patch | Initial handoff assertions do not protect retained preview refresh. Deliver notification captures after handoff and assert revoked/stale evidence disables Confirm while preserving the mounted preview and original reference. |
| R-V4 | medium | patch | carried V2: browser controls lack a competing picker/reason. Preserve this prior verdict and complete the outstanding exact-focus verification task. |
| BH1 | medium | patch | The now-reachable preview has a second button labeled as a preview handoff but wired to `SubmitAsync`; remove that misleading duplicate action. |
| BH2 | high | defer | carried R-B6: the pre-existing Story 5.6 final dispatch uses cached authority; confirmation-time revalidation remains deferred. |
| BH3 | high | defer | Page refresh, source-row loss, and viewport narrowing can unmount a submitted preview and lose its tracking handle; this existed in the Story 5.6 panel before this change and is recorded below. |
| BH4 | medium | defer | carried R-B7: the pre-existing preview has no redacted owner-count consequence fact. |
| BH5 | high | defer | The existing Story 5.6 proof lookup matches event type, scope, target, and time without attempt-specific provenance, so an unrelated event can be mistaken for proof. |
| BH6 | medium | defer | The existing Story 5.6 proof href targets an `audit-` fragment that the grid does not render as an id, so the link has no destination. |
| BH7 | low | defer | The existing preview carries `originalTimestamp` in its intent but filters it from visible preview data; showing it is Story 5.6 review content. |
| BH8 | low | reject | Two quick Start clicks can issue redundant read-only projection requests, but the generation guard discards the older result and keeps the launcher focused; adding a pending control for this uncommon case adds state without protecting a mutation. |
| BH9 | low | reject | A notification can supersede an initial Start read and leave no panel, but the latest audit refresh updates the available action or reason while the launcher remains; a retry is possible and no command is sent. The pending-state change is larger than this transient case warrants. |
| BH10 | medium | patch | `IsCommandSurfaceConnected` alone does not prove `SupportsCommandStatusLookup`; the preview can enable Confirm with a gateway unable to track the submitted command. Gate Confirm on that capability. |
| BH11 | false | reject | carried B9/R-B10: the fixture claims rendered markup and shipped focus-helper checks; component tests execute Blazor callbacks, and no authenticated live-app claim is made. |
| BH12 | maybe-false | defer | The isolated browser fixture does not prove the composed grid/receipt/preview layout at narrow widths, but no actual overflow is demonstrated. An authenticated composed application browser run would settle this medium-if-true gap; the existing read-auth condition limits that run. |
| EH1 | high | defer | The same pre-existing submitted-preview unmounting as BH3 occurs on refresh, row loss, and viewport narrowing. |
| EH2 | low | reject | The same generation race as BH9 can discard an initial Start read; the retained launcher and latest refresh permit retry without mutation. |
| VG1 | medium | patch | Gateway tests cover current responses and unusable responses, while evaluator tests fabricate noncurrent captures. A successful stale or handler-computed REST response has no direct gateway assertion that its capture remains unavailable. |

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

- `.github/workflows/story-guards.yml`
- `references/Hexalith.EventStore`
- `references/Hexalith.FrontComposer`
- `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/deferred-work.md`
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

## Completion Notes List

The resumed review initially ran `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`, which returned `RESULT: FAIL` for two undeclared pointers. Both were already committed in the implementation, rather than introduced by this review. They are recorded as the source build inputs shipped with the story so the complete baseline diff remains reviewable; no pointer was reverted or updated.

| Existing source input | Baseline → committed target | Context |
|---|---|---|
| `references/Hexalith.EventStore` | 4339eb6aa4d52b83adc558d2c03687b7ac7d43f2 → 2c58ffda41759e895ace4b9625c9bd931a217672 | Inherited implementation build input; upstream source-structure changes and published archive qualification evidence. |
| `references/Hexalith.FrontComposer` | 3b1584d923a9a7dac163050b924285a1206605b2 → 4cedcdc216980664f561084ea7b91946c37260c9 | Inherited implementation build input; upstream approved runtime qualification prerequisites. |

### Resumed verification — 2026-10-02

- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1`: passed, zero warnings/errors (`/tmp/story55-resume-build.log`).
- `dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false --no-build --no-restore`: 3,729 passed, zero failed/skipped (`/tmp/story55-resume-ui.log`).
- `TENANTS_BROWSER_BUILD_CONFIGURATION=Debug bash tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh`: passed rendered EN/FR start fixtures at desktop/narrow widths and forced colors, shipped focus checks, and negative mutations (`/tmp/story55-resume-browser.log`). Browser fixtures verify rendered components and shipped focus helpers; Blazor callback behavior is covered by component tests.
- `aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json`: all twenty resources Running/Healthy. This is resource-health evidence; it does not resolve the previously recorded live authenticated-read limitation.

### Final review-fix verification — 2026-10-02

- Same Debug/source-reference build command: passed, zero warnings/errors (`/tmp/story55-review-fixes-build.log`).
- `tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests -class '*TenantAuditPageTests' -class '*CorrectionStartPanelTests' -class '*TenantsBffCompositionTests'`: 265 passed, zero errors/failures/skips/not-run (`/tmp/story55-review-fixes-focused.log`).
- Same per-project Microsoft.Testing.Platform command: 3,740 passed, zero failed/skipped (`/tmp/story55-review-fixes-ui.log`).
- Same Debug Chromium harness command: passed, now including a picker before each launcher and exact unavailable-reason focus for both origins (`/tmp/story55-review-fixes-browser.log`).
- `git diff --check` and the story gitlink validator: passed. The final unified review diff is `/tmp/story55-review-vte6q9yt/changes.diff`.
- All three review layers completed. New retained-handoff, command-tracking, live-recovery, already-applied and focus findings were patched. Prior V1/V2 verification tasks were completed; receipt selection and notification refresh also have executable regression coverage. R-B6 and R-B7 are recorded in `deferred-work.md` as pre-existing Story 5.6 confirmation/consequence work.
- The exact local commit message passed pinned commitlint 21.2.2 with `node_modules/.bin/commitlint --edit /tmp/story55-review-vte6q9yt/commit-message.txt --verbose`; zero errors/warnings, evidence preserved in `/tmp/story55-review-vte6q9yt/commitlint-validation.log`. Spec status is `done`; sprint status is `review`, as required by the build workflow.

### Final build review — 2026-10-02

- Debug source-reference build passed with zero warnings/errors; the full UI test project passed 3,761/3,761 with zero skips. The Debug Chromium harness passed EN/FR rendered start fixtures at desktop and narrow widths, forced colors, exact focus and zero browser egress.
- `git diff --check` and `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md` passed. The final baseline diff was staged at `/tmp/story55-audit-zMSkkV/changes.diff` for review.
- Blind, edge-case and verification-gap reviews completed. Three findings were patched (the misleading duplicate submit action, missing status-lookup confirmation gate, and gateway metadata regression coverage). Pre-existing Story 5.6 and an unverified composed-layout check were recorded in `deferred-work.md`; the remaining low or false findings were rejected with evidence in the triage log.

### Review Findings

Code review 2026-10-02 (bmad-code-review, full mode). Diff `bcfc0788..f44e19e7` covers the two story commits `cefefa26` and `f44e19e7`: 36 files, +2,361/−533, reviewed as one pass. The non-story AppHost commit `bcfc0788` is excluded. PR #51 is already merged as `09c90f08`, whose tree is identical to `f44e19e7`.

Inputs and baseline checks:
- **Layers:** blind hunter, edge-case hunter, verification gap and acceptance auditor; none failed.
- **Gitlink validator:** `python3 scripts/validate-story-gitlinks.py` on this spec → PASS. Both pointer moves are declared, and both targets are reachable on their submodule `origin/main`.
- **Build:** `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1` → 0 warnings, 0 errors.
- **Tests:** `dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false --no-build --no-restore` → 3,740/3,740 passed.

Checks on the merged PR (context, not story findings): Story Guards, Commitlint and CI were all red.
- **Commitlint:** the PR title "Fix/story 5 5 correction start review" is not a Conventional Commit (`subject-empty`, `type-empty`).
- **CI:** red from the already-tracked package-boundary failure.
- **Story Guards:** see the first patch below.

- [x] [Review][Patch] (resolved decision → option (a), a panel-owned `HasSubmitted` flag) A submitted preview that ends Failed or Rejected is discarded by a new role selection or Start, which opens a second dispatch attempt [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1457].
  - **The gap:** `HasSubmittedCorrection` (`TenantAuditPage.razor:1457`) only protects previews with `HasCommandTracking` (MessageId and CorrelationId) or in `RequestSent`. `TenantCorrectionPreviewSnapshot.ApplySubmissionFailure` never copies the MessageId, yet `TenantCommandGateway` attaches one to Failed results raised after the POST. A possibly dispatched Failed attempt therefore counts as "not submitted".
  - **The result:** `SelectCorrectionRoleAsync` and `OpenCorrectionAsync` set `_previewCorrectionIntent = null`. The ambiguous outcome disappears without the operator closing it, and a fresh start → handoff → Confirm dispatches a second attempt with a new MessageId. Domain idempotency limits the state impact, but this breaks the single-retained-attempt rule.
  - **Before 5.5:** a reopen kept the same mounted panel and preserved its terminal state.
  - **Options:**
    - (a) a panel-owned `HasSubmitted` flag, set in `SubmitAsync` and never cleared, read by `HasSubmittedCorrection`;
    - (b) `ApplySubmissionFailure` keeps a returned MessageId and CorrelationId ("dispatched = MessageId present", the Story 5.4 rule), which also changes `HasCommandTracking` consumers;
    - (c) a lifecycle-based test, which cannot tell pre-submit AlreadyApplied/UnableToVerify from post-submit.
- [x] [Review][Patch] (resolved decision → option (b), one recovery message with the current redaction kept, plus Owner-only empty-membership copy for global administrators) Losing authority, and empty-membership recovery for a non-global operator, show a stack of generic reasons and never the "request permission" recovery [src/Hexalith.Tenants.UI/State/TenantAudit/TenantCorrectionStartIntent.cs:86].
  - **Unauthorized operator:** an unauthorized principal gets the fully unavailable capture (`TenantsBffComposition.cs:562-567`). The evaluator then shows, together: AuthorizationIndeterminate, CurrentProjectionUnavailable, TenantLifecycleUnknown (for an active tenant) and CurrentStateIndeterminate. None of these strings carries a recovery step.
  - **Global administrator with empty membership:** only global administrators can reach `EmptyMembershipRequiresOwner`, yet its copy tells them to "request current global administrator permission". The matrix requires the canonical recovery ("Choose Owner/request permission"; lost authority → applicable recovery).
  - **Options:**
    - (a) a non-leaking authority-denied capture state (no membership, lifecycle or role disclosed) that yields one PermissionRequired-style reason with request-permission copy, plus Owner-only copy for global administrators;
    - (b) keep the redaction and only collapse the unavailable-capture reasons to one localized "refresh or request permission" message;
    - (c) accept the current copy for 5.5 and defer recovery wording to Story 5.6.
- [x] [Review][Patch] The Story Guards Chromium lane fails deterministically ("Focus validator inputs are missing.") [tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh:7]. The script default changed to `Debug`, while `.github/workflows/story-guards.yml:60` builds Release and sets no `TENANTS_BROWSER_BUILD_CONFIGURATION` (job 110722075272, run 36970094779 on `09c90f08`). Set the variable to `Release` on the workflow step, or default the script to Release. The baseline Chromium exit-134 abort still needs its own fix after that.
- [x] [Review][Patch] A failed fixture export exits silently and its log is deleted [tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh:150]. Under `set -euo pipefail` the test-executable output goes only to `$validation_tmp/start-fixture-tests.log`, which the EXIT trap removes. On failure, print the log and exit; also check that `tenant-correction-start-{en,fr}.html` exist before launching Chromium.
- [x] [Review][Patch] A stale `_activeCorrectionFocusReference` defeats the R-E1 fix [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:648]. `OpenCorrectionAsync` sets the reference even when activation fails, and `SelectCorrectionRoleAsync` closes panels without clearing it. Narrowing the viewport then calls `ScheduleCorrectionFocus`, whose `InvalidateCorrectionFocus` wipes the receipt-heading focus just captured and sends focus to a stale grid row. Guard on `_activeCorrectionIntent is not null || _previewCorrectionIntent is not null` instead of the reference.
- [x] [Review][Patch] Dead code left by the refactor [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1924]. These members have no callers: `RefreshGlobalAdministratorsProjectionAsync`, `RefreshGlobalAdministratorsProjectionForLoadAsync` (`:1943`), `IsGlobalAdministratorRow` (`:1965`), `IsGlobalAdministratorAuthorized` (`:1968`), the `_globalAdministratorsSnapshot`/`_globalAdministratorsProjectionGeneration` fields that only they use, `ReplaceUnavailableReceiptCorrection` (`:1536`), `IsFocusInsideReceiptCorrectionAsync` (`:1547`) and `AuditDataGrid.RequiresRoleSelection` (`AuditDataGrid.razor:267`). The comment at `:962` still mentions "the global-administrator refresh below", which no longer exists. Keep `GlobalAdministratorCorrectionPanel.razor` for Story 5.7.
- [x] [Review][Patch] Uncorrectable outcomes render every unavailable reason, which misstates complete evidence [src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditDataGrid.razor:258]. The UnsupportedOutcome exclusion was dropped from `ShouldRenderUnavailableReason`, `IsSupportedCorrection` and `AuditEvidenceReceipt.HasRenderableUnavailableReason` (`AuditEvidenceReceipt.razor:358`). On the real page a `TenantConfigurationSet` row or receipt then reads "Audit evidence is not ready for correction start. Authorization evidence is indeterminate. This audit outcome is not supported for correction start." The matrix asks only for "Localized unsupported".
  - Fix: render only `Tenants.Correction.Unavailable.UnsupportedOutcome` when that reason is present, in both the grid and the receipt.
  - Remove the orphan `;` lines.
  - Update the receipt comment at `AuditEvidenceReceipt.razor:209-211`.
  - Rename `Audit_grid_does_not_render_correction_copy_for_unsupported_rows` and `Receipt_component_omits_correction_copy_for_uncorrectable_outcomes`, and make them assert the exact unsupported text and the absence of the other reasons. No test asserts that copy today.
- [x] [Review][Patch] The GlobalNotReady branch is asserted only as non-empty text [tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs:1126]. No test anywhere pins `Tenants.Correction.Start.GlobalNotReady`. Assert the exact localized text in the global read-only page test.
- [x] [Review][Patch] The start-panel timestamp adds a redundant suffix ("… UTC +00:00"), while the receipt shows "… UTC" [src/Hexalith.Tenants.UI/Components/Tenants/Audit/TenantCorrectionStartPanel.razor:79]. Use the receipt's `yyyy-MM-dd HH:mm:ss 'UTC'` format so the same evidence timestamp reads the same on both surfaces.
- [x] [Review][Patch] Every grid role picker has the same accessible name [src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditDataGrid.razor:123]. Each row's `FluentSelect` uses `aria-label="Choose intended role"`, while the Start button beside it names the audit reference. Add a reference-qualified EN/FR key, for example "choose intended role for audit evidence {0}".
- [x] [Review][Patch] The no-op `await Task.CompletedTask.ConfigureAwait(false)` [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1825]. Make `SelectCorrectionRoleAsync` non-async and return `Task.CompletedTask`.
- [x] [Review][Patch] The new deferred-work section breaks the ledger convention [_bmad-output/implementation-artifacts/deferred-work.md:3419]. It is an H1 (`# Story 5.5 resumed review — 2026-10-02`) with no blank line before it, while every other section is `## Deferred from: code review of <spec> (<date>)`. Rename it to that heading and add the blank line.
- [x] [Review][Patch] The French start-panel title drops "forward" [src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx:4159]. "Démarrer une correction de locataire" loses the forward-correction meaning. Use the established term "correction en avant", for example "Démarrer une correction en avant du locataire".
- [x] [Review][Patch] On phone or unmeasured widths, global-administrator rows claim a supported correction [src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditDataGrid.razor:262]. `IsSupportedCorrection` accepts the GlobalAdministrators domain, so a global row shows "This supported correction is read-only on a phone. Use … desktop viewport to continue", while desktop shows GlobalNotReady. Require `IntendedCommandDomain is TenantCorrectionCommandDomain.Tenants`.
- [x] [Review][Patch] The owner-only authority rule is not tested against a non-owner member [tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantsBffCompositionTests.cs:61]. Dropping `member.Role is TenantRole.TenantOwner &&` at `TenantsBffComposition.cs:562` passes every test. Add a theory where the principal is a TenantContributor or TenantReader member, asserting an unavailable capture.
- [x] [Review][Patch] The gateway catch-all is untested, and it is the only guard on Start and handoff [src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs:87]. Add gateway tests where `GetTenantAsync` throws `HttpRequestException` and where the principal resolver throws, both asserting an unavailable capture.
- [x] [Review][Patch] Re-selecting the role inside the preview during empty-membership recovery is unpinned [src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:456]. Add a `CorrectionStartPanelTests` case: an empty-membership global `StartProjection`, role changed from Owner to Reader in the preview, asserting Confirm is disabled.
- [x] [Review][Patch] Focus return to a receipt launcher is unpinned [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1454]. Add a page test that closes a receipt-launched panel and asserts `focusCorrectionLauncher(reference, "receipt")` under strict JS, plus `data-correction-origin` markup assertions.
- [x] [Review][Patch] Preview-heading focus after handoff (B2) is unpinned [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1528]. Extend `DeliberateStartAndSeparatePreviewHandoffNeverSubmitOrPoll` with a strict `focusElementById("tenants-correction-title")` assertion.
- [x] [Review][Patch] The command-surface term of the support gate is unpinned [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1789]. Add a page theory row with `StubBffComposition(commandConnected: false)`, asserting no Start and no handoff.
- [x] [Review][Patch] Ignoring a stale or scope-conflicting capture for "already applied" (B4) is unpinned [src/Hexalith.Tenants.UI/State/TenantAudit/TenantCorrectionStartIntent.cs:126]. Add an evaluator theory and a `CorrectionStartPanelTests` case where a non-current or mismatched capture's `CurrentRole` equals the intended role, asserting no AlreadyApplied reason and no `currentRole` input.
- [x] [Review][Patch] Nothing checks that the preview hides machine tokens [src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:672]. After handoff, assert that the preview's visible text contains neither `tenant-projection-current` nor a bare `true` for empty recovery.
- [x] [Review][Patch] Hiding the pickers on unsafe viewports (B5) is unpinned [src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditDataGrid.razor:113]. Add `tenants-correction-role` absence assertions to the existing phone and unmeasured grid tests, and a receipt test with `IsCorrectionViewportSafe=false` (`AuditEvidenceReceipt.razor:107`).
- [x] [Review][Defer] A submitted, tracked preview is still unmounted by refresh, list-refresh, row loss and viewport narrowing [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1653] — deferred: pre-existing.
  - These paths clear `_previewCorrectionIntent` without checking `HasSubmittedCorrection`: `CaptureCorrectionAuthority` (source row absent or not Ready, for example after the corrective event pushes it off page 1), `ClearPaging` (`ListRefreshed`/`InvalidCursor`, `:1407`) and the viewport handler (`:645`).
  - The preview is also inside the `ShouldRenderRows` block (`:214`), so a Loading snapshot unmounts it.
  - The same paths unmounted the submitting `CorrectionStartPanel` at baseline `bcfc0788`. The commit's "Retain submitted correction tracking" covers only the new role and start paths.
  - Story 5.6 owns preview lifecycle retention, and the resolution of the first decision above determines its "submitted" signal.
- [x] [Review][Defer] CI still runs no Tenants test tier [.github/workflows/ci.yml] — deferred: pre-existing and already tracked (`deferred-work.md:3335`). It was reconfirmed red on `09c90f08`: run 36970095241 fails `ci / build-and-test` at "Validate package consumer references", so every UI test cited for this story is local evidence only.

#### Rejected

- BH3/AA9, "the browser harness checks its own listeners": low. Already adjudicated as B9/R-B10, and correcting the recorded browser claims would be a spec edit.
- AA2, "unrelated submodule bumps bundled into `cefefa26`": low. Both moves are declared in the File List, the validator passes, both targets are on `origin/main` and the suite passes. Splitting them now would mean rewriting merged history.
- BH10/AA3, "global-administrator correction withdrawn, and the copy points to a missing path": false. The spec's Never-rule forbids global correction in this flow, Story 5.7 is backlog, the component file is kept, and the Global Administrators page is the supported path the copy refers to.
- BH6/E10, "pickers and Start are silently inert while a submitted preview is tracked": low. It needs the tracked-preview condition, and the fix needs new disabled-state parameters.
- BH8, "gateway catch-all does not log": low. It matches the gateway's existing silent-catch convention (15 other blocks).
- BH9, "activation and handoff reads use `CancellationToken.None`": low. Generation guards discard superseded results, so the cost is one wasted read; the fix needs CTS plumbing.
- BH13/AA8, "the page stub returns captures production cannot produce": low. The evaluator, BFF and panel tests cover those matrix rows, and the fix is a stub redesign. It may be revisited with the authority decision.
- BH14 (residual), "Indeterminate principal, blank subject, duplicate or unsafe ids untested": low. Each has a second guard (blank-subject check, page `Distinct`, narrative mapper null-out). The non-owner and catch-all gaps are the patches above.
- BH15, "focus priority keyed on `data-testid`": low. It breaks only if a test id is renamed.
- BH16a, "assignable roles duplicated across files": low. It causes harm only if the role set changes.
- BH18, "`Evaluate` ignores the `CurrentRole`/`TenantStatus` compatibility fields": low. No production caller sets them without a `Projection`, and the result is fail-closed.
- BH19a, "`_isHandoffPending` reset off-dispatcher": false. No render or EventCallback runs off-dispatcher there; ComponentBase re-renders on the dispatcher after the handler task completes.
- BH19c, "`OnAfterRenderAsync` reads the focus generation after `ConfigureAwait(false)`": low. It is the existing pattern, the window is tiny, and the fix is complex.
- BH20b, "spec status `done` vs sprint `review`, `review_loop_iteration: 0`, `/tmp` evidence paths": the split is intentional per the Completion Notes, and the rest are spec edits.
- AA4/E3, "handoff does not compare the command type or current role": false. The preview re-displays current state and intended command and needs explicit Confirm. `PreviewHandoffRetainsFreshCaptureWhenEarlierDetailDisagrees` pins this deliberately.
- AA7/E11, "activation shares the projection generation and discards a load's batch capture": low. Start re-reads before any intent arms, the window is narrow, and the fix adds counters.
