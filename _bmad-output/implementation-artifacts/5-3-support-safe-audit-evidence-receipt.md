---
created: 2026-06-06T16:56:22+02:00
baseline_commit: 77bb935ea1c04cf258077bb510d0dc0c50ca6c79
---

# Story 5.3: Support-Safe Audit Evidence Receipt

Status: done

<!-- Note: Created by the BMAD create-story workflow for Story 5.3. -->

## Story

As an authorized user,
I want to view a support-safe Audit Evidence Receipt for a recorded action,
so that I can cite what happened without exposing raw event data or fabricating proof.

## Acceptance Criteria

1. Given a user opens an audit evidence receipt for a recorded action, when structured audit data is available, then the receipt shows actor, target, tenant scope, outcome, absolute timestamp, projection marker, and audit or command reference, and all labels and values use Tenants-owned localized copy and support-safe formatting.
2. Given receipt data is assembled from `TenantAuditEntry`, when fields are resolved, then actor comes from `ActorId`, target comes from `NarrativePayload.userId` then `key` then `TenantId`, scope comes from `TenantId`, outcome comes from event type and `AuditEventCategory`, timestamp comes from the audit entry, projection marker comes from freshness, and reference comes from audit or command reference, and `NarrativePayload` is treated as structured narrative metadata, not raw event body.
3. Given receipt evidence is partial, pending, delayed, unavailable, or unsupported, when the receipt renders, then the actual state is shown with wait, retry, inspect-audit, continue-read-only, or escalate options where appropriate, and the UI does not fabricate proof or show Success.
4. Given receipt values include identifiers or references, when the user copies receipt content, then only approved support-safe references are copied, and tokens, decoded JWT contents, raw metadata, serialized payloads, internal correlation ids, stack traces, and PII are blocked or redacted.
5. Given the receipt is used with keyboard, screen reader, forced-colors mode, or narrow viewport, when it renders, then field labels, value relationships, focus order, visible focus, no-color-only state, and responsive safety are preserved, and stable selectors such as `data-testid="tenants-audit-receipt"` and `data-testid="tenants-audit-receipt-reference"` are present.
6. Given this story is complete, when verification is run, then unit/component tests cover receipt field derivation, `NarrativePayload` fallback ordering, partial/unavailable states, safe-copy eligibility, redaction, localization, and stable selectors.
7. Given accessibility or E2E verification is run, then keyboard operation, screen-reader field relationships, live-region politeness, forced-colors rendering, responsive safety, and no raw event payload exposure are verified.

## Tasks / Subtasks

- [x] Add a Tenants-owned audit receipt model and builder (AC: 1, 2, 3, 4)
  - [x] Add a small pure model under `src/Hexalith.Tenants.UI/State/TenantAudit/`, for example `TenantAuditReceipt`, to hold actor, target, scope, outcome, timestamp, projection marker, audit reference, optional support-safe command reference, state, and copyable reference text.
  - [x] Build receipts from the existing `TenantAuditRow`/`TenantAuditEntry` shape. Do not add or call a backend receipt endpoint.
  - [x] Use the existing source derivation order: `ActorId` for actor; `NarrativePayload.userId`, then `key`, then `TenantId` for target; `TenantId` for scope; `EventType` plus `AuditEventCategory` for outcome; `Timestamp` for timestamp; `TenantFreshnessState` for projection marker; `EventReference` and optional `supportSafeCommandReference` for references.
  - [x] Treat `NarrativePayload` as the PRD name for the existing `IReadOnlyDictionary<string, string>` on `TenantAuditEntry`; do not create a separate backend contract, DTO, or endpoint for it.
  - [x] Treat `NarrativePayload` as structured narrative metadata only. Do not render or copy dictionary dumps, raw persisted event payloads, serialized command payloads, raw EventStore metadata, protected cursors, ETags, stack traces, internal correlation ids, MessageIds, or access tokens.
  - [x] If the implementation needs additional approved narrative keys, keep the allow-list local and explicit. Do not pass arbitrary narrative keys into visible copy or copied receipt text.

- [x] Add the receipt component and row open behavior (AC: 1, 2, 5)
  - [x] Add `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceReceipt.razor` and `.razor.css` or an equivalent local Tenants audit component. Keep it Tenants-owned; do not add generic FrontComposer, timeline, or cross-domain receipt scaffolding.
  - [x] Extend `AuditDataGrid` with an accessible row action such as `View receipt`, using a stable selector like `data-testid="tenants-audit-receipt-open"` and preserving existing grid columns, support-safe copy button, pinned critical columns, and row markers.
  - [x] Render the receipt from `TenantAuditPage` for the selected loaded row, or from a safe `receiptReference` query parameter only when the matching row exists in the current tenant-scoped audit result. If the requested reference is not loaded, show an honest unavailable/inspect-audit state instead of querying a separate source.
  - [x] Keep `GET /api/tenants/{tenantId}/audit` through `ITenantQueryGateway.GetTenantAuditAsync` as the only audit data source.
  - [x] Preserve Story 5.2 context query parameters (`targetUserId`, `supportSafeCommandReference`, `returnUrl`, `returnFocus`, and `source`) and do not break return navigation or focus context.

- [x] Render receipt evidence states without false Success (AC: 3, 5)
  - [x] Render a ready receipt only when a loaded audit row supplies the required support-safe fields.
  - [x] Map pending or delayed command/audit context from existing `TenantCommandAuditState` values (`AuditPending`, `AuditDelayed`, `AuditUnavailable`, `MissingSupport`) and existing `TenantAuditSurfaceKind` values without inventing stringly typed state tokens.
  - [x] Show partial/unavailable states with visible localized next actions: wait or refresh for pending, retry/inspect audit for delayed, continue-read-only or retry for unavailable, and escalate with a support-safe reference for missing implementation support.
  - [x] Never use Success styling or copy for `audit pending`, `audit delayed`, `audit unavailable`, `missing implementation support`, stale, degraded, unauthorized, invalid-cursor, or error states.
  - [x] SignalR or command lifecycle context may nudge the user to inspect or refresh audit, but must never advance a receipt to proven/ready by itself.

- [x] Implement support-safe receipt copy (AC: 1, 4, 5)
  - [x] Reuse `SupportSafeCopyButton` and `SupportSafeCopyClassifier`; do not create a second clipboard, redaction, or JS interop mechanism.
  - [x] Copy only an approved receipt reference summary assembled from classified safe values: audit event reference, optional support-safe command reference, tenant/user/key reference when safe, projection freshness marker, and absolute timestamp.
  - [x] Classify tenant ids as `TenantId`, user ids as `UserId`, configuration keys as `ConfigurationKey`, and audit/command references as `ApprovedReference`.
  - [x] If any field is unsafe or empty, omit it or block copy with the existing unsafe/empty feedback. Do not silently copy unsafe values.
  - [x] Add selectors including `data-testid="tenants-audit-receipt-copy"` and `data-testid="tenants-audit-receipt-reference"`.

- [x] Add localization, accessibility, responsive, and styling evidence (AC: 1, 3, 5, 7)
  - [x] Add EN/FR `Tenants.Audit.Receipt.*` resources for title, field labels, field accessible names, receipt states, recovery actions, copy labels, unavailable reasons, and live-region text.
  - [x] Use whole localized strings with named placeholders. Do not assemble sentences from fragments or leak raw machine tokens such as `audit_pending` into visible copy.
  - [x] Render field/value relationships with semantic structure (`dl`/`dt`/`dd`, table-like labels, or equivalent accessible associations) and stable focus order.
  - [x] Render absolute UTC/culture-aware timestamps consistently with the Story 5.1 UTC audit timestamp fix.
  - [x] Add focus-visible, forced-colors, reduced-motion-safe, and responsive CSS hooks. At mobile widths, preserve receipt safety fields or fail closed with a visible localized reason.
  - [x] Keep routine receipt open/close announcements polite; use assertive only for blocking, unavailable, degraded, unsafe-copy, or unable-to-verify states.

- [x] Add focused tests and validation (AC: 1-7)
  - [x] Add component tests, likely in `TenantAuditPageTests` and a new `AuditEvidenceReceiptTests`, for opening a receipt from an audit row and rendering actor, target, scope, outcome, timestamp, projection marker, and audit/command reference.
  - [x] Add unit tests for receipt field derivation and `NarrativePayload` fallback ordering: `userId` wins over `key`, `key` wins over `TenantId`, and missing narrative falls back safely.
  - [x] Add tests for partial, pending, delayed, unavailable, missing-support, stale, degraded, unauthorized, and invalid-reference receipt states with no false Success.
  - [x] Add safe-copy tests proving copied receipt text omits or blocks tokens, decoded JWT contents, raw metadata, serialized payloads, internal correlation ids, stack traces, cursors, ETags, MessageIds, and PII-like unsafe values.
  - [x] Add EN/FR resource parity tests for every `Tenants.Audit.Receipt.*` key.
  - [x] Add static/component tests for stable selectors, no browser backend calls from receipt components, no `localStorage`/`sessionStorage`, no raw payload strings, focus-visible hooks, forced-colors hooks, and responsive safety.
  - [x] Run focused validation first: `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore`.
  - [x] Run the xUnit v3 executable fallback for UI tests if the known .NET 10 Microsoft.Testing.Platform/VSTest issue appears.

## Dev Notes

### Story Source And Epic Context

- Story source is Epic 5 Story 5.3. Epic 5 covers audit evidence and forward recovery; this story adds support-safe receipts for already recorded audit actions. [Source: `_bmad-output/planning-artifacts/epics.md#Story 5.3: Support-Safe Audit Evidence Receipt`]
- FR22 requires a support-safe Audit Evidence Receipt with actor, target, tenant scope, outcome, absolute timestamp, projection marker, and audit or command reference. It must be assembled from structured `NarrativePayload`, never raw event payloads. [Source: `_bmad-output/planning-artifacts/epics.md#FR22`; `docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md#3. Audit Evidence Receipt Content and Copy-Safety`]
- FR23 requires `audit pending`, `audit delayed`, `audit unavailable`, and `missing implementation support` to stay distinct, with wait, retry, continue-read-only, inspect-audit, or escalate paths. [Source: `_bmad-output/planning-artifacts/epics.md#FR23`; `docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md#4. Delayed/Unavailable Audit-Proof States and the No-False-Success Rule`]
- Story 5.4 owns broader audit availability state recovery; Story 5.5 owns starting forward corrections; Story 5.6 owns preview/confirmation and original/corrective proof links. Do not implement correction actions, correction previews, command dispatch, grouped timeline mode, anomaly scoring, analytics, or cross-record linking here. [Source: `_bmad-output/planning-artifacts/epics.md#Story 5.4: Audit Availability State Recovery`; `_bmad-output/planning-artifacts/epics.md#Story 5.5: Start Forward Correction from Audit Evidence`; `_bmad-output/planning-artifacts/epics.md#Story 5.6: Preview and Confirm Correction with Linked Proof`]
- The approved `FC-AUD` fallback remains the flat Audit DataGrid. Do not claim or add a FrontComposer `<AuditTimeline>`. [Source: `_bmad-output/planning-artifacts/fallback-approval-record-2026-06-03.md#1. FC-AUD -> flat audit DataGrid`; `docs/tenants-ui-frontcomposer-dependency-map.md#FC-AUD`]

### Existing Implementation To Extend

- Story 5.1 added `/tenants/{TenantId}/audit`, `TenantAuditRequest`, `TenantAuditRow`, `TenantAuditSnapshot`, `TenantAuditSurfaceKind`, `AuditDataGrid`, resources, and `ITenantQueryGateway.GetTenantAuditAsync`. Reuse this work. [Source: `_bmad-output/implementation-artifacts/5-1-tenant-audit-trail-datagrid.md#File List`]
- Story 5.2 added scoped audit entry points and optional audit-page context query parameters: `targetUserId`, `supportSafeCommandReference`, `returnUrl`, `returnFocus`, and `source`. Preserve these while adding receipt behavior. [Source: `_bmad-output/implementation-artifacts/5-2-scoped-audit-evidence-entry-points.md#Completion Notes List`; `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor`]
- `TenantAuditEntry` already exposes the backend read DTO: `EventId`, `EventType`, `Category`, `ActorId`, `Timestamp`, `TenantId`, `NarrativePayload`, plus computed `Target`, `Scope`, and `Outcome`. Do not redeclare this DTO or re-case wire fields. [Source: `src/Hexalith.Tenants.Contracts/Queries/TenantAuditEntry.cs`]
- `NarrativePayload` is not a separate backend type. The PRD uses that name for the structured narrative dictionary already present on `TenantAuditEntry`. [Source: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/review-domain-fidelity.md#Finding 8`; `src/Hexalith.Tenants.Contracts/Queries/TenantAuditEntry.cs`]
- `TenantAuditRow.FromEntry` already maps audit entries into support-safe row fields and filters narrative reference context through an explicit allow-list. Extend or reuse it; do not bypass it with arbitrary narrative rendering. [Source: `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditRow.cs`]
- `AuditDataGrid` currently renders timestamp, actor, outcome, target, scope, category, freshness, reference context, row markers, and a support-safe event-reference copy button. Add receipt open behavior without removing safety-critical columns or changing existing selectors. [Source: `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditDataGrid.razor`]
- `TenantAuditPage` owns tenant scope, filters, cursor paging, state rendering, context banners, safe return URL handling, and UTC date parsing. Receipt selection should remain inside this page or a local child component unless a direct route is unavoidable. [Source: `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor`]
- `SupportSafeCopyButton` and `SupportSafeCopyClassifier` already provide the approved copy path. Reuse `SupportSafeCopyValueKind.ApprovedReference` for receipt/audit references and the identifier-specific kinds for tenant/user/configuration values. [Source: `src/Hexalith.Tenants.UI/Components/Shared/SupportSafeCopyButton.razor`; `src/Hexalith.Tenants.UI/Services/SupportSafety/SupportSafeCopyClassifier.cs`]
- There is no `src/Hexalith.Tenants.UI/Vocabulary/` directory in the current implementation. Current state tokens live under `State/TenantCommands` and `State/TruthState`; use those existing enums unless the implementation deliberately introduces the missing vocabulary in scope. [Source: `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs`; `src/Hexalith.Tenants.UI/State/TruthState/TenantFreshnessState.cs`]

### Backend And Data Boundary

- The consume-only backend surface is fixed. For audit, keep using `GET /api/tenants/{tenantId}/audit` through the server-side BFF and `GetTenantAuditQuery`. Do not add backend audit, receipt, consequence, preview, command-specific evidence, correction, or return-context endpoints. [Source: `_bmad-output/planning-artifacts/architecture.md#Technical Constraints & Dependencies`; `_bmad-output/implementation-artifacts/5-2-scoped-audit-evidence-entry-points.md#Backend And Data Boundary`]
- Browser-side components must not call backend routes directly and must not store backend tokens. All backend egress stays through `ITenantQueryGateway`. [Source: `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`; `tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs`]
- Treat `TenantId` and `UserId` as meaningful caller-supplied strings. Preserve literal values and URI-escape only for navigation; never parse as GUIDs or ULIDs. [Source: `_bmad-output/project-context.md#Identity Rules`]
- `SequenceNumber` is aggregate-local only and must not be used as a global receipt order or proof marker. The receipt projection marker for this story should be the support-safe freshness marker already present on `TenantAuditRow`; do not expose raw ETag/protected cursor values as receipt proof. [Source: `_bmad-output/project-context.md#DAPR, Eventing & Consumer Rules`; `docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md#3.2 The receipt composes from the existing read model`]
- Authorization remains server-enforced. The UI may show unavailable states, but it must not loosen server authorization or infer permission from client-only state. [Source: `_bmad-output/project-context.md#Event-Sourcing & Domain Rules`; `_bmad-output/planning-artifacts/epics.md#NFR2`]

### UX, Accessibility, And Localization Guardrails

- `AuditEvidenceReceipt` is one of the named domain components. In this repo, keep the first implementation local to `Components/Tenants/Audit/` unless there is already a shared Tenants UI pattern to reuse. [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR11`; `_bmad-output/planning-artifacts/architecture.md#Naming Patterns`]
- Reserve Success for projection-proven truth or audit-available evidence only. Pending, delayed, unavailable, unsupported, stale, degraded, or unable-to-verify states are not Success. [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR3`; `docs/tenants-ui-truth-state-and-action-availability-spec.md#5.2`]
- Use calm, precise copy and avoid prohibited terms: `undo`, `rollback`, and `hidden edit`. Correction language belongs to Stories 5.5 and 5.6. [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR22`; `docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md#5. Compensating-Recovery Language and Flow`]
- Use Tenants-owned `.resx` resources under `Tenants.Audit.Receipt.*`, whole strings with named placeholders, and EN/FR parity. [Source: `_bmad-output/planning-artifacts/epics.md#NFR7`; `_bmad-output/planning-artifacts/architecture.md#Localization keys`]
- Render every receipt status with text plus icon/shape, visible focus, forced-colors support, stable dimensions, and selectors. Color alone is not sufficient. [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR5`; `_bmad-output/planning-artifacts/epics.md#NFR4`]
- Absolute timestamps are required. Follow the Story 5.1 UTC rendering correction; do not use server-local `ToLocalTime()` or relative-only labels. [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR29`; `_bmad-output/implementation-artifacts/5-1-tenant-audit-trail-datagrid.md#Senior Developer Review (AI)`]

### Previous Story Intelligence

- Story 5.1 fixed server-local timezone dependence in audit timestamp rendering and date-filter parsing after review. Preserve UTC timestamp behavior. [Source: `_bmad-output/implementation-artifacts/5-1-tenant-audit-trail-datagrid.md#Senior Developer Review (AI)`]
- Story 5.2 fixed a raw source-kind token leak by mapping machine source tokens to localized whole phrases. Do not leak machine tokens from receipt state or source context into visible copy or accessible names. [Source: `_bmad-output/implementation-artifacts/5-2-scoped-audit-evidence-entry-points.md#Senior Developer Review (AI)`]
- Story 5.2 confirmed UI tests passed via the xUnit v3 executable fallback because `dotnet test` can hit a known .NET 10 Microsoft.Testing.Platform/VSTest issue. Use the same fallback if needed. [Source: `_bmad-output/implementation-artifacts/5-2-scoped-audit-evidence-entry-points.md#Debug Log References`]
- Recent commits are story-scoped Conventional Commits: `77bb935 feat(story-5.2): Scoped Audit Evidence Entry Points` and `497a4ac feat(story-5.1): Tenant Audit Trail DataGrid`. A compatible implementation commit for this story would be `feat(story-5.3): add support-safe audit evidence receipt`. [Source: `git log --oneline -5`]
- Current dirty work before creating this story included only `_bmad-output/story-automator/orchestration-1-20260605-153745.md`. It is unrelated and must not be reverted. [Source: `git status --short`]

### Latest Technical Information

- Use the repo-pinned stack and existing APIs: .NET 10, Blazor InteractiveServer, Fluent UI Blazor, EventStore query/command gateways, FrontComposer shell, xUnit v3, Shouldly, bUnit, and NSubstitute. Do not add packages or package versions. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`; `Directory.Packages.props`]
- External package research is not required for this story because implementation relies on existing repo-pinned components and local contracts. The primary risks are support-safety, receipt field derivation, no-false-success state rendering, localization, accessibility, and avoiding backend scope creep.

### Project Structure Notes

- Expected UI changes: `src/Hexalith.Tenants.UI/State/TenantAudit/`, `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceReceipt.razor`, `AuditDataGrid.razor`, `TenantAuditPage.razor`, corresponding CSS, and `TenantsResources.resx` / `.fr.resx`.
- Expected tests: `tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs`, a new `AuditEvidenceReceiptTests.cs` if useful, `TenantQueryGatewayTests.cs` or focused state tests for derivation/redaction, resource parity tests, static safety tests, and CSS/accessibility hook tests.
- Avoid backend contract/projection changes, `TenantAuditEntry` wire-shape changes, `GetTenantAuditQueryHandler`, audit projection storage, EventStore server registration, AppHost/Aspire plumbing, package metadata, shared FrontComposer code, and submodule changes unless a compile-time break proves a direct integration need.
- Do not add generic receipt infrastructure, a timeline component, a grouped audit mode, correction actions, correction preview, command submission, or cross-domain support-safety scaffolding to Tenants.

### References

- Story source: `_bmad-output/planning-artifacts/epics.md#Story 5.3: Support-Safe Audit Evidence Receipt`
- Epic context: `_bmad-output/planning-artifacts/epics.md#Epic 5: Audit Evidence and Forward Recovery`
- Audit/recovery spec: `docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md#3. Audit Evidence Receipt Content and Copy-Safety`; `docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md#4. Delayed/Unavailable Audit-Proof States and the No-False-Success Rule`
- FrontComposer fallback: `_bmad-output/planning-artifacts/fallback-approval-record-2026-06-03.md#1. FC-AUD -> flat audit DataGrid`; `docs/tenants-ui-frontcomposer-dependency-map.md#FC-AUD`
- Architecture: `_bmad-output/planning-artifacts/architecture.md#Technical Constraints & Dependencies`; `_bmad-output/planning-artifacts/architecture.md#Implementation Patterns & Consistency Rules`; `_bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries`
- Existing code: `src/Hexalith.Tenants.Contracts/Queries/TenantAuditEntry.cs`; `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditRow.cs`; `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor`; `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditDataGrid.razor`; `src/Hexalith.Tenants.UI/Components/Shared/SupportSafeCopyButton.razor`; `src/Hexalith.Tenants.UI/Services/SupportSafety/SupportSafeCopyClassifier.cs`
- Existing tests: `tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs`; `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs`; `tests/Hexalith.Tenants.UI.Tests/Components/SupportSafeCopyButtonTests.cs`
- Project rules: `_bmad-output/project-context.md`; `AGENTS.md`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-06T17:06:00+02:00 - Red-phase UI test build failed on missing `TenantAuditReceiptState`/receipt model types as expected before implementation.
- 2026-06-06T17:10:00+02:00 - `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-build --no-restore` hit the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility; used xUnit v3 executable fallback.
- 2026-06-06T17:12:00+02:00 - `tests/Hexalith.Tenants.Server.Tests/bin/Release/net10.0/Hexalith.Tenants.Server.Tests -noLogo -parallel none -reporter quiet` was attempted and failed on known pre-existing AppHost/docs evidence checks for missing `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml` and Story 7.6A deployment-readiness expectations; no failures touched Story 5.3 UI files.
- 2026-06-06T17:13:00+02:00 - `tests/Hexalith.Tenants.IntegrationTests/bin/Release/net10.0/Hexalith.Tenants.IntegrationTests -noLogo -parallel none -reporter quiet` was attempted; DAPR prerequisite tests were skipped and remaining failures were DAPR/InternalServerError infrastructure behavior unrelated to Story 5.3 UI changes.

### Completion Notes List

- Added a Tenants-owned `TenantAuditReceipt` model/builder that composes receipts from `TenantAuditEntry`/`TenantAuditRow`, preserves the required `NarrativePayload.userId -> key -> TenantId` target fallback, maps existing command/audit surface states without Success, and builds copy text only from classified support-safe values.
- Added local `AuditEvidenceReceipt` UI with semantic field/value relationships, stable selectors, localized state/action copy, support-safe copy via `SupportSafeCopyButton`, visible non-color state indicators, forced-colors/reduced-motion/responsive CSS hooks, and no fabricated timestamp for unavailable/unloaded receipt references.
- Extended `AuditDataGrid` with an accessible `View receipt` row action and `TenantAuditPage` with loaded-row receipt rendering plus fail-closed `receiptReference` query handling that does not call any new backend endpoint.
- Added EN/FR `Tenants.Audit.Receipt.*` resources and focused model/component/page tests for field derivation, fallback ordering, partial/unavailable states, safe-copy redaction, localization parity, selectors, no browser storage/backend calls, and CSS/accessibility hooks.
- Validation passed: `dotnet build Hexalith.Tenants.slnx -c Release -m:1 --no-restore`; xUnit executables passed for Contracts 105/105, Client 47/47, Testing 181/181, Sample 31/31, and UI 552/552.
- Broader Server and Integration executable suites were attempted and still have known pre-existing AppHost/DAPR documentation/infrastructure failures outside the Story 5.3 UI boundary.

### File List

- `_bmad-output/implementation-artifacts/5-3-support-safe-audit-evidence-receipt.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditDataGrid.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditDataGrid.razor.css`
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceReceipt.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceReceipt.razor.css`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`
- `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditReceipt.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/AuditEvidenceReceiptTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/State/TenantAuditReceiptTests.cs`

### Change Log

- 2026-06-06T16:56:22+02:00 - Created Story 5.3 context and marked it ready for development.
- 2026-06-06T17:14:41+02:00 - Implemented support-safe audit evidence receipt model, UI rendering, safe-copy behavior, localization, accessibility/responsive hooks, and focused tests; marked story ready for review.
- 2026-06-06 - Senior Developer Review (AI) completed: build clean (0 warnings), UI suite re-run green (552/552), corrected stale UI test count in Completion Notes (548 -> 552), recorded non-blocking observations; 0 critical issues; status set to done.

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot (automated adversarial review)
**Date:** 2026-06-06
**Outcome:** Approve — 0 critical, 0 high. Status moved `review` -> `done`.

### Verification performed

- Rebuilt `tests/Hexalith.Tenants.UI.Tests` in Release: **0 warnings, 0 errors**.
- Re-ran the xUnit v3 executable (`-noLogo -noColor -parallel none`): **552 total, 0 failed, 0 skipped**. The story's Completion Notes claimed `UI 548/548`; verified actual is 552 and corrected the note (the `test-summary.md` addendum already said 552).
- Cross-checked git working tree against the story File List: every application source/test file in the File List matches git reality, and no application source file changed outside the File List (the only extra git change, `_bmad-output/.../test-summary.md`, is an excluded BMAD artifact updated by the QA flow).
- EN/FR resource parity confirmed: 28 `Tenants.Audit.Receipt.*` keys each, all 10 receipt-state keys and all 5 freshness keys present in both cultures; FR values are genuine translations, not English copies.

### Acceptance Criteria

- **AC1–AC2 (fields & derivation):** Implemented. `TenantAuditReceipt.FromRow/FromEntry` derive actor from `ActorId`, target via the `userId -> key -> TenantId` fallback (inherited from `TenantAuditEntry.ResolveTarget`), scope from `TenantId`, outcome as `EventType (Category)`, timestamp from the row, projection marker from freshness, and references from `EventReference` + optional command reference. `NarrativePayload` is consumed only through the approved-key allow-list in `TenantAuditRow`; no dictionary/raw-body dump reaches the receipt.
- **AC3 (no false Success):** Implemented for surface-derived states. `ResolveState` maps surface kinds and freshness to `Stale/Degraded/Unauthorized/Unavailable/InvalidReference/Partial`, never `Ready`, and the component never emits Success styling/copy for non-ready states (covered by tests). See observation O4 on the command-audit-state path.
- **AC4 (safe copy / redaction):** Implemented with defense-in-depth — approved-key allow-list at the row, per-field classification, and a whole-string `ApprovedReference` gate that **fails closed** (returns empty) if any unsafe fragment or `@`/PII is present. Tokens, JWT-like text, metadata, cursors, ETags, correlation ids, stack traces, MessageIds, and email PII are all blocked (parameterized tests confirm).
- **AC5 (a11y/responsive/selectors):** Implemented. Semantic `dl/dt/dd`, `role="region"` labelled by the heading, non-color state glyphs, `:focus-visible`, `forced-colors`, `prefers-reduced-motion`, and responsive single-column fallback; required selectors `tenants-audit-receipt` and `tenants-audit-receipt-reference` (plus `-open`, `-copy`, `-state`) present.
- **AC6 (tests):** Implemented — model, component, and page tests cover derivation, fallback ordering, partial/unavailable states, redaction, parity, and selectors.
- **AC7 (a11y/E2E evidence):** CSS/selector hooks and static no-storage/no-backend checks present; full screen-reader/forced-colors E2E remains manual per epic.

### Findings

- **MEDIUM (fixed):** Completion Notes UI test count was stale (`548/548`); corrected to `552/552` to match the verified run.
- **LOW / observation O1:** A `?receiptReference=` deep-link computes an honest `InvalidReference` receipt, but it is only rendered inside `@if (ShouldRenderRows)`. When the whole audit surface yields no rows (Unauthorized/Unavailable/Empty/FilteredEmpty), the receipt-level state is suppressed. Not user-misleading because the page-level state section is itself honest and no second backend query is made (verified). Left as-is to avoid a confusing double-state.
- **LOW / observation O2:** The clipboard summary in `BuildCopyableReferenceText` uses fixed English labels (`Audit reference:`, …) and the raw freshness enum (`Projection marker: Current`). The rendered receipt is fully localized; the copied summary is a canonical machine-readable support format, so this is defensible and intentionally not localized.
- **LOW / observation O3:** The `Escalate` and `Wait` recovery actions both route through `OnRetry` (refresh) since no dedicated escalation hook exists yet; the distinct escalation/correction flows are owned by Stories 5.5/5.6.
- **LOW / observation O4:** `TenantAuditReceipt` supports command-lifecycle states (`Pending/Delayed/MissingSupport`) but `TenantAuditPage` always passes `auditState: NotStarted` because the read-only audit query (`GetTenantAuditAsync`) carries no command-audit state. These states are therefore exercised only by the model/component tests, not the live page. This matches the fixed consume-only data boundary; wiring a transient command-audit state into the audit page would be out of scope.

No findings rise to High/Critical: no AC is unimplemented, no `[x]` task is falsely claimed, and no support-safety/security gap was found.
