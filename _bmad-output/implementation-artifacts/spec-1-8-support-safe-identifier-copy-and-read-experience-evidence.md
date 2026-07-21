---
title: 'Story 1.8: Support-Safe Identifier Copy and Read-Experience Evidence'
type: 'feature'
created: '2026-07-21'
status: 'in-review'
baseline_revision: 'c0451deaa02a67c852a5222b75a3564795460d0b'
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

Hardened support-safe copy around explicit outer-surface approval, exact literal preservation, fail-closed defaults, race-safe Clipboard API interop, polite atomic EN/FR feedback, and current read-experience evidence. Configuration copy stays unavailable until Story 1.6 supplies its positive BFF-safe model; the report records that dependency and the unavailable browser/NVDA proof without converting them into passing claims.

### Files Changed

- `_bmad-output/implementation-artifacts/1-8-support-safe-identifier-copy-and-epic-1-readiness-evidence.md` -- points historical completion claims to the current superseding report.
- `_bmad-output/implementation-artifacts/spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md` -- captures intent, tasks, review triage, verification, and this result.
- `_bmad-output/implementation-artifacts/story-1-8-support-safe-identifier-copy-and-read-experience-evidence-2026-07-21.md` -- records current pins, test outcomes, evidence matrices, and blockers.
- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor` -- migrates the authorized identifier copy consumer to explicit approval.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor` -- explicitly approves the authorized detail identifier.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor.css` -- preserves significant identifier whitespace visibly.
- `src/Hexalith.Tenants.UI/Components/Shared/SupportSafeCopyButton.razor` -- implements fail-closed rendering, versioned interop, serialized activation, and polite atomic feedback.
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditDataGrid.razor` -- gates audit copy through centralized audit safety.
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceReceipt.razor` -- supplies explicit approval for sanitized receipt content.
- `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/LegacyConfigurationDisplaySanitizer.cs` -- isolates transitional display-only redaction from clipboard approval.
- `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/RemoveTenantConfigurationFlow.razor` -- preserves safe consequence-preview display behavior.
- `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/SetTenantConfigurationFlow.razor` -- preserves safe current-value preview behavior.
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor` -- explicitly approves authorized member identifiers.
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor.css` -- preserves member identifier whitespace.
- `src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor` -- removes copy affordances while retaining authorization-safe display until Story 1.6 lands.
- `src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor` -- explicitly approves authorized list identifiers.
- `src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor.css` -- preserves list identifier whitespace and wrapping.
- `src/Hexalith.Tenants.UI/Components/Users/MyTenantsDataGrid.razor` -- explicitly approves My Tenants and user-lookup identifiers.
- `src/Hexalith.Tenants.UI/Components/Users/MyTenantsDataGrid.razor.css` -- preserves My Tenants/user-lookup identifier whitespace.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` -- adds complete English recovery, including cancellation.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx` -- adds exact French recovery parity.
- `src/Hexalith.Tenants.UI/Services/SupportSafety/SupportSafeCopyClassifier.cs` -- enforces explicit approval and known-kind fail-closed classification.
- `src/Hexalith.Tenants.UI/Services/SupportSafety/SupportSafeCopyEligibility.cs` -- defines the documented fail-closed eligibility enum.
- `src/Hexalith.Tenants.UI/Services/SupportSafety/SupportSafeCopyValueKind.cs` -- defines copy contracts with `Unknown = 0`.
- `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditReceipt.cs` -- consumes centralized audit-field safety.
- `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditSupportSafety.cs` -- centralizes existing audit display/receipt sanitization.
- `tests/Hexalith.Tenants.UI.Tests/Components/AuditEvidenceReceiptTests.cs` -- proves incomplete receipts expose no copy state.
- `tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs` -- activates authorized global-administrator copy exactly.
- `tests/Hexalith.Tenants.UI.Tests/Components/MyTenantsSurfaceTests.cs` -- activates exact self-audit copy and guards whitespace CSS.
- `tests/Hexalith.Tenants.UI.Tests/Components/RemoveTenantConfigurationFlowTests.cs` -- guards restored remove-preview display.
- `tests/Hexalith.Tenants.UI.Tests/Components/SetTenantConfigurationFlowTests.cs` -- guards restored set-preview display.
- `tests/Hexalith.Tenants.UI.Tests/Components/SupportSafeCopyButtonTests.cs` -- covers exact literals, failures, races, overlap, defaults, accessibility, and source safety.
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs` -- covers safe audit copy and unsafe-reference omission.
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs` -- covers detail/member copy, configuration omission, and literal CSS.
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs` -- activates exact list copy and guards literal layout.
- `tests/Hexalith.Tenants.UI.Tests/Components/UserMembershipLookupSurfaceTests.cs` -- proves visible reserved/Unicode text equals copied text.
- `tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs` -- guards explicit approval, evidence blockers, and exact EN/FR bundles.
- `tests/test-summary.md` -- replaces obsolete Story 1.8 claims with current 237/970-test evidence.

### Review Findings

- Patches applied: 15 (high 5, medium 8, low 2).
- Items deferred: 0.
- Items rejected as noise or unsupported expansion: 4.
- Follow-up review recommendation: `true`; patched score = `3 × 8 medium + 2 low = 26`, and five patched findings were high severity.

### Verification

- UI Release test-project build: passed with 0 warnings and 0 errors.
- Seven-class focused runner: 237 passed, 0 failed, 0 skipped.
- Full UI runner: 970 passed, 0 failed, 0 skipped.
- Release solution build: passed with 0 warnings and 0 errors.
- I/O matrix audit: every row has an executed passing guard, including dependency-blocker record structure.
- `git diff --check`: passed.

### Residual Risks

- `CFG-1.6-SAFE-MODEL`: configuration copy remains intentionally unavailable until Story 1.6 provides positive pre-component approval.
- `BROWSER-COPY-1.8`: authenticated EN/FR Clipboard API, focus, responsive, forced-colors, and reduced-motion browser evidence remains open for Tenant UI QA.
- `AT-NVDA-1.8`: dated human NVDA/browser announcement and focus evidence remains open for Accessibility QA.
