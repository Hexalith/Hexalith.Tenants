---
title: 'Story 1.8: Support-Safe Identifier Copy and Read-Experience Evidence'
type: 'feature'
created: '2026-07-21'
status: 'done'
baseline_revision: 'c0451deaa02a67c852a5222b75a3564795460d0b'
final_revision: 'f08da0f8ffa52d1d6cc4eade585c32edb52ab92a'
review_loop_iteration: 0
followup_review_recommended: true
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-1-6-read-only-tenant-configuration.md'
  - '{project-root}/_bmad-output/implementation-artifacts/1-8-support-safe-identifier-copy-and-epic-1-readiness-evidence.md'
warnings:
  - oversized
---

<intent-contract>

## Intent

**Problem:** The historical Story 1.8 added copy controls, but approval is implicit, identifier safety relies on content deny-lists, failure announcements are over-assertive, outer-surface interaction coverage is incomplete, and its readiness report no longer proves the revised Stories 1.2-1.8 read experience.

**Approach:** Make copy eligibility explicit and fail-closed at each authorized read surface, preserve approved caller-supplied literals exactly, harden localized clipboard feedback, and issue current source, component, browser, accessibility, localization, responsive, and support-safety evidence without claiming unavailable proof.

## Boundaries & Constraints

**Always:** Treat tenant/user ids as case-sensitive caller-supplied strings with no invented GUID, ULID, email, or length contract; approve copy from server-authorized read-model provenance before rendering an affordance; pass the same visible literal to the clipboard; keep focus on the trigger; use Fluent/FrontComposer, stable selectors, EN/FR whole strings, polite atomic result announcements, and fail-closed configuration policy from Story 1.6.

**Block If:** Correct behavior would require a new backend/public contract, an invented identifier limit, approval of raw configuration data, or changes inside `references/`. Missing human assistive-technology proof is recorded as blocked evidence with owner, consequence, and reopen trigger; it is never fabricated.

**Never:** Copy or expose hidden/alternate/decoded/route-encoded values, bearer/JWT data, payloads, EventStore metadata, internal correlation ids, cursors, ETags, stack traces, secrets, or PII; infer configuration safety from a blacklist; add browser backend calls/storage, legacy clipboard fallbacks, command/audit/search/global-admin scope, or generic FrontComposer clipboard infrastructure.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Approved identifier | Authorized tenant/user literal, including Unicode, reserved characters, significant surrounding whitespace, or long content | One labeled affordance copies the exact visible string without normalization or truncation | No component-imposed length or format rejection |
| Unapproved value | Empty value, hidden value, unsafe reference, or configuration entry without Story 1.6 positive approval | No copy affordance and the value never enters copy-component state | Render only the authorization-safe read state |
| Clipboard outcome | Success, insecure/missing API, denied permission, disconnection, or generic JS failure | Localized polite atomic feedback; focus stays on the trigger; only actual success says copied | No value or exception detail is logged, rendered, announced, or persisted |
| Evidence dependency | Browser/AT lane or Story 1.6 safe model is unavailable | Other evidence runs; affected matrix cell is honestly `blocked` | Record exact blocker, owner, consequence, and measurable reopen trigger |

</intent-contract>

## Code Map

- `src/Hexalith.Tenants.UI/Components/Shared/SupportSafeCopyButton.razor` -- current shared Fluent copy control and live feedback.
- `src/Hexalith.Tenants.UI/Services/SupportSafety/SupportSafeCopyClassifier.cs` -- current implicit deny-list eligibility and co-located public enums.
- `src/Hexalith.Tenants.UI/wwwroot/js/tenantsClipboard.js` -- isolated Clipboard API adapter.
- `src/Hexalith.Tenants.UI/Components/Tenants/` and `Components/Users/` -- list, detail, configuration, member, My Tenants, and user-lookup composition surfaces.
- `tests/Hexalith.Tenants.UI.Tests/` -- component, composition, localization, CSS, and Fluent governance evidence.

## Tasks & Acceptance

**Execution:**
- `src/Hexalith.Tenants.UI/Services/SupportSafety/SupportSafeCopyClassifier.cs`, new `SupportSafeCopyValueKind.cs`, and new `SupportSafeCopyEligibility.cs` -- split public types; add `Unknown = 0`; classify whitespace-only values as empty, explicit non-approval/unknown kinds as unsafe, and explicitly approved values as allowed without content normalization or identifier deny-lists.
- `src/Hexalith.Tenants.UI/Components/Shared/SupportSafeCopyButton.razor`, `.razor.css`, and `wwwroot/js/tenantsClipboard.js` -- require a non-default kind plus explicit `IsApproved`, prevent interop for non-approved values, preserve exact text/focus, use one polite atomic status channel with localized recovery, and keep interop free of storage, logging, backend calls, or legacy fallbacks.
- `src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor`, `Components/Pages/TenantDetailPage.razor`, `Components/Users/MyTenantsDataGrid.razor`, and `Components/Tenants/Members/MemberAccessReview.razor` -- approve only authorized projection literals before composing list, detail, My Tenants, user-lookup, and member copy controls; retain surface-specific selectors and read behavior.
- `src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor` -- consume only Story 1.6's positively approved safe model; if that model is absent, omit configuration copy affordances and record the dependency instead of reintroducing blacklist safety.
- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor`, `Components/Tenants/Audit/AuditDataGrid.razor`, and `Components/Tenants/Audit/AuditEvidenceReceipt.razor` -- migrate shared-component calls to explicit approval without changing their product behavior; full UI regression evidence guards this compatibility-only edit.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` and `.fr.resx` -- keep exact key parity and add complete result/recovery strings without raw values or failure internals.
- `tests/Hexalith.Tenants.UI.Tests/Components/SupportSafeCopyButtonTests.cs`, `TenantListSurfaceTests.cs`, `TenantDetailSurfaceTests.cs`, `MyTenantsSurfaceTests.cs`, `UserMembershipLookupSurfaceTests.cs`, `DomainUiFluentConformanceTests.cs`, and `TenantsUiCompositionTests.cs` -- cover the edge matrix, activate copy through every outer surface, prove exact/alternate-value behavior, source safety, selectors, focus/live-region semantics, EN/FR parity, responsive/forced-colors/reduced-motion hooks, and shared-consumer compatibility.
- `_bmad-output/implementation-artifacts/story-1-8-support-safe-identifier-copy-and-read-experience-evidence-2026-07-21.md`, the historical Story 1.8 artifact, and `tests/test-summary.md` -- retain history, add a supersession pointer, and record immutable pins, exact commands/results, per-AC and per-surface/channel evidence, browser artifacts, and honest blocked cells.

**Acceptance Criteria:**
- Given an approved identifier on any Stories 1.2-1.8 read surface, when copy is activated, then the clipboard receives exactly the complete visible caller-supplied literal and focus remains on the same control.
- Given a value lacks explicit surface or Story 1.6 approval, when the surface renders, then no copy affordance, copy state, announcement, clipboard call, log, telemetry tag, or serialized state contains it.
- Given each clipboard success or failure outcome, when feedback renders, then EN/FR polite atomic copy names the true outcome, offers safe recovery for non-success, and never reports false success or exception detail.
- Given tenant list, detail, My Tenants, user lookup, configuration, and member review at supported widths and accessibility modes, when inspected and exercised, then literal access, stable selectors, keyboard order, focus visibility, overflow/wrapping, forced-colors meaning, and reduced-motion-independent behavior remain usable.
- Given current verification and retained evidence, when the report is reviewed, then every revised criterion and surface/channel has dated evidence or an exact blocker with owner, consequence, and reopen trigger; historical test counts and obsolete Server/AppHost failures are not presented as current.

## Spec Change Log

## Review Triage Log

### 2026-07-21 — Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 15: (high 5, medium 8, low 2)
- defer: 0
- reject: 4: (high 0, medium 4, low 0)
- addressed_findings:
  - `[high]` `[patch]` Made copy-kind classification reject undefined/future enum values and added regression coverage.
  - `[high]` `[patch]` Captured and versioned copy inputs across async interop so parameter changes cannot substitute an unapproved literal or publish stale feedback.
  - `[medium]` `[patch]` Reset feedback when component input identity changes.
  - `[medium]` `[patch]` Republished repeated identical outcomes through a cleared live region and covered repeated success/failure.
  - `[high]` `[patch]` Centralized audit-field sanitization so unsafe event references cannot bypass receipt safety through the grid.
  - `[high]` `[patch]` Restored configuration command-preview behavior through an isolated display-only compatibility sanitizer without granting clipboard approval.
  - `[medium]` `[patch]` Required a non-empty accessible name before a copy affordance can render.
  - `[low]` `[patch]` Strengthened the Razor consumer audit so non-self-closing copy controls cannot evade explicit-approval checks.
  - `[medium]` `[patch]` Added exact real-bundle EN/FR assertions for every changed clipboard outcome.
  - `[high]` `[patch]` Preserved significant identifier whitespace visibly with `white-space: break-spaces` across the five copyable read surfaces.
  - `[medium]` `[patch]` Corrected the evidence verdict so six-surface usability remains completion-blocked while configuration/browser/AT proof is unavailable.
  - `[medium]` `[patch]` Added audit and global-administrator outer-surface activation/omission compatibility coverage.
  - `[low]` `[patch]` Proved the user-lookup visible reserved/Unicode literal equals the exact clipboard argument.
  - `[medium]` `[patch]` Mapped canceled JS interop to localized non-success recovery.
  - `[medium]` `[patch]` Serialized overlapping activations so duplicate or out-of-order writes cannot occur.

### 2026-07-21 — Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 7: (high 4, medium 2, low 1)
- defer: 5: (high 2, medium 3, low 0)
- reject: 5: (high 1, medium 2, low 2)
- addressed_findings:
  - `[high]` `[patch]` Stopped unapproved literals from being duplicated into the component's observed-value state while preserving fail-closed invalidation.
  - `[high]` `[patch]` Serialized the final version and eligibility check with clipboard-write initiation on the renderer dispatcher, closing the pre-write revocation race.
  - `[medium]` `[patch]` Cleared prior result feedback at the start of a new eligible activation so an old success cannot remain visible during a pending attempt.
  - `[high]` `[patch]` Added pending-import coverage for approval revocation, an unknown value kind, and loss of the accessible name.
  - `[medium]` `[patch]` Exercised significant whitespace, reserved characters, Unicode, and exact clipboard arguments through list, detail, My Tenants, user-lookup, and member surfaces.
  - `[high]` `[patch]` Added set/remove preview regressions for a sensitive configuration key paired with an otherwise safe value.
  - `[low]` `[patch]` Corrected stale single-line/ellipsis CSS comments to describe the intentional exact-literal wrapping behavior.

### 2026-07-21 — Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 3: (high 0, medium 3, low 0)
- defer: 6: (high 3, medium 3, low 0)
- reject: 7: (high 0, medium 6, low 1)
- addressed_findings:
  - `[medium]` `[patch]` Reworded canceled or timed-out interop feedback in EN/FR so it states only that copy could not complete, without inventing user cancellation.
  - `[medium]` `[patch]` Renamed the bUnit evidence section so component/workflow tests are not mislabeled as browser end-to-end proof.
  - `[medium]` `[patch]` Separated current Story 1.8 validation from superseded repository validation retained only for provenance.

## Design Notes

`ValueKind` identifies the data contract; it must not silently confer approval through a default. The outer BFF-backed surface owns identifier approval. Configuration approval remains the positive pre-component Story 1.6 contract. The shared component enforces that decision immediately before interop as defense in depth. Audit/global-admin calls receive only a compatibility migration; their product behavior and story evidence remain out of scope.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-restore -warnaserror -m:1 -nr:false` -- expected: zero warnings/errors.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.Components.SupportSafeCopyButtonTests -class Hexalith.Tenants.UI.Tests.Components.TenantListSurfaceTests -class Hexalith.Tenants.UI.Tests.Components.TenantDetailSurfaceTests -class Hexalith.Tenants.UI.Tests.Components.MyTenantsSurfaceTests -class Hexalith.Tenants.UI.Tests.Components.UserMembershipLookupSurfaceTests -class Hexalith.Tenants.UI.Tests.DomainUiFluentConformanceTests -class Hexalith.Tenants.UI.Tests.TenantsUiCompositionTests` -- expected: all focused tests pass.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` -- expected: full UI suite passes.
- `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -warnaserror -m:1 -nr:false` -- expected: zero warnings/errors.

**Manual checks (if no CLI):**
- Retain authenticated EN/FR browser evidence for success/failure, focus before/after, all six surfaces, responsive widths, forced colors, reduced motion, and clean console/log output; record NVDA/browser evidence as blocked unless a dated human session is available.

## Auto Run Result

Status: done

### Summary

Completed a fresh four-layer review of the support-safe copy implementation. The pass closed the remaining current-change races and evidence gaps, retained exact caller-supplied literals through every principal read surface, and appended five newly deferred pre-existing findings without changing any existing ledger entry.

### Files Changed

- `_bmad-output/implementation-artifacts/1-8-support-safe-identifier-copy-and-epic-1-readiness-evidence.md` -- points historical completion claims to the superseding evidence report.
- `_bmad-output/implementation-artifacts/deferred-work.md` -- appends five new pre-existing review findings without altering existing entries.
- `_bmad-output/implementation-artifacts/spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md` -- records the fresh review triage, verification, recommendation, and result.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` -- records Story 1.8 as done.
- `_bmad-output/implementation-artifacts/story-1-8-support-safe-identifier-copy-and-read-experience-evidence-2026-07-21.md` -- records current pins, matrices, test evidence, and honest blockers.
- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor` -- migrates the authorized identifier copy consumer to explicit approval.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor` -- explicitly approves the authorized detail identifier.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor.css` -- preserves significant identifier whitespace visibly.
- `src/Hexalith.Tenants.UI/Components/Shared/SupportSafeCopyButton.razor` -- enforces fail-closed rendering, renderer-serialized pre-write validation, non-retained unapproved observations, and truthful feedback transitions.
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditDataGrid.razor` -- gates audit copy through the existing audit safety policy.
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceReceipt.razor` -- supplies explicit approval for the existing sanitized receipt content.
- `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/LegacyConfigurationDisplaySanitizer.cs` -- isolates transitional display-only redaction from clipboard approval.
- `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/RemoveTenantConfigurationFlow.razor` -- preserves command-preview display behavior while withholding copy approval.
- `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/SetTenantConfigurationFlow.razor` -- preserves current-value preview behavior while withholding copy approval.
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor` -- explicitly approves authorized member identifiers.
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor.css` -- preserves member identifier whitespace.
- `src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor` -- omits configuration copy until Story 1.6 supplies a positive safe model.
- `src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor` -- explicitly approves authorized list identifiers.
- `src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor.css` -- preserves exact whitespace/wrapping and accurately documents the layout.
- `src/Hexalith.Tenants.UI/Components/Users/MyTenantsDataGrid.razor` -- explicitly approves My Tenants and user-lookup identifiers.
- `src/Hexalith.Tenants.UI/Components/Users/MyTenantsDataGrid.razor.css` -- preserves exact My Tenants/user-lookup whitespace and accurately documents the layout.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` -- supplies complete English clipboard recovery copy.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx` -- supplies exact French clipboard recovery parity.
- `src/Hexalith.Tenants.UI/Services/SupportSafety/SupportSafeCopyClassifier.cs` -- requires explicit approval and a known copy-value kind.
- `src/Hexalith.Tenants.UI/Services/SupportSafety/SupportSafeCopyEligibility.cs` -- defines fail-closed eligibility values.
- `src/Hexalith.Tenants.UI/Services/SupportSafety/SupportSafeCopyValueKind.cs` -- defines copy contracts with `Unknown = 0`.
- `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditReceipt.cs` -- consumes centralized audit-field safety.
- `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditSupportSafety.cs` -- centralizes the existing audit display/receipt policy.
- `tests/Hexalith.Tenants.UI.Tests/Components/AuditEvidenceReceiptTests.cs` -- proves incomplete receipts omit copy state.
- `tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs` -- activates authorized global-administrator copy exactly.
- `tests/Hexalith.Tenants.UI.Tests/Components/MyTenantsSurfaceTests.cs` -- proves exact whitespace/Unicode/reserved-character copy through My Tenants.
- `tests/Hexalith.Tenants.UI.Tests/Components/RemoveTenantConfigurationFlowTests.cs` -- covers safe preview retention and sensitive-key redaction.
- `tests/Hexalith.Tenants.UI.Tests/Components/SetTenantConfigurationFlowTests.cs` -- covers safe preview retention and sensitive-key redaction.
- `tests/Hexalith.Tenants.UI.Tests/Components/SupportSafeCopyButtonTests.cs` -- covers exact literals, failure outcomes, revocation races, overlapping activation, fresh feedback, defaults, and accessibility.
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs` -- covers safe audit copy and unsafe-reference copy omission.
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs` -- proves exact detail/member copy and configuration-copy omission.
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs` -- proves exact list copy for significant whitespace, reserved characters, and Unicode.
- `tests/Hexalith.Tenants.UI.Tests/Components/UserMembershipLookupSurfaceTests.cs` -- proves exact visible-to-clipboard equality for significant whitespace, reserved characters, and Unicode.
- `tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs` -- guards explicit approval, evidence-blocker structure, and EN/FR bundle parity.
- `tests/test-summary.md` -- records the story's superseding verification evidence.

### Review Findings

- Patches applied this pass: 7 (high 4, medium 2, low 1).
- Items newly deferred this pass: 5 (high 2, medium 3, low 0); each was appended as a new ledger entry only.
- Items rejected this pass: 5 (high 1, medium 2, low 2).
- Follow-up review recommendation: `true`; patched score = `3 × 2 medium + 1 low = 7`, and four patched findings were high severity.

### Verification

- UI Release test-project build: passed with 0 warnings and 0 errors.
- Focused runner including the changed component/surface/configuration classes: 279 passed, 0 failed, 0 skipped.
- Full UI runner: 976 passed, 0 failed, 0 skipped.
- Release solution build: passed with 0 warnings and 0 errors.
- `git diff --check`: passed.
- Browser/NVDA/manual proof was not fabricated; the existing exact blockers remain authoritative.

### Residual Risks

- Five pre-existing findings were appended to `deferred-work.md`: audit raw-display safety, disposal/in-flight interop coordination, DOM-safe focus anchors, hidden audit-receipt copy representation, and deny-list-based legacy configuration display.
- `CFG-1.6-SAFE-MODEL`, `BROWSER-COPY-1.8`, and `AT-NVDA-1.8` remain honestly blocked evidence cells with their recorded owners, consequences, and reopen triggers.
- After the reviewed diff is committed, this spec's required `status` and `final_revision` write-back remains as an unstaged workflow result artifact.

### Follow-up Review — 2026-07-21

Summary: Completed a fresh four-layer follow-up review of the Story 1.8 baseline diff. Hardened cancellation feedback so it does not invent user intent, clarified the evidence taxonomy and current-versus-historical validation record, and recorded one newly surfaced pre-existing configuration-key exposure without changing any existing deferred-work entry.

Files changed in this review pass:
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` -- made the English canceled/timeout recovery copy causally neutral.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx` -- kept the French recovery copy equivalent and causally neutral.
- `tests/Hexalith.Tenants.UI.Tests/Components/SupportSafeCopyButtonTests.cs` -- updated cancellation outcome coverage for the truthful recovery copy.
- `tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs` -- updated exact EN/FR resource assertions.
- `tests/test-summary.md` -- distinguished component/workflow evidence from browser E2E evidence and separated current validation from retained history.
- `_bmad-output/implementation-artifacts/story-1-8-support-safe-identifier-copy-and-read-experience-evidence-2026-07-21.md` -- refreshed authoritative focused, component, and full-suite totals.
- `_bmad-output/implementation-artifacts/deferred-work.md` -- appended one new configuration-key exposure entry; existing entries were not modified, reopened, or rewritten.
- `_bmad-output/implementation-artifacts/spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md` -- recorded this review pass, verification, recommendation, and result.

Review findings: 3 medium patches applied; 6 pre-existing items deferred, of which 5 were already present in the ledger and 1 was appended as new; 7 findings rejected as design preference, workflow-transient state, out-of-scope compatibility coverage, or already disclosed external evidence gaps.

Follow-up review recommendation: `true` -- patched findings were high 0, medium 3, low 0; score = `3 × 3 + 1 × 0 = 9`.

Verification performed:
- Release UI test-project build passed with 0 warnings and 0 errors.
- Seven-class focused UI executable passed 241/241.
- `SupportSafeCopyButtonTests` passed 43/43.
- Full UI executable passed 976/976.
- Release solution build passed with 0 warnings and 0 errors.
- `git diff --check` passed.

Residual risks: Story 1.6 positive configuration approval, authenticated browser focus/responsive proof, and human NVDA evidence remain explicitly blocked in the dated report. Six pre-existing issues remain deferred; only the newly identified configuration-key exposure was appended during this pass.
