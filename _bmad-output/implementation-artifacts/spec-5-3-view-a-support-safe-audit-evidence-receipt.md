---
title: 'View a support-safe audit evidence receipt'
type: 'feature'
created: '2026-09-27'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 5
baseline_commit: 33d6dcca278952eaf65764e24b21000a9baa32d8
context:
  - _bmad-output/implementation-artifacts/epic-5-context.md
  - _bmad-output/planning-artifacts/epics.md
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The historical receipt exists, but it can mark incomplete provenance ready, uses category-specific target fallback, copies only an audit reference, and hides an unavailable requested receipt when the audit grid has no rows. It lacks the current contract's field selectors and focus handoff.

**Approach:** Reverify Story 5.3 against the current epic. Assemble an allow-listed receipt view model in the server-side BFF from an authorized loaded audit row, render seven support-safe facts with honest availability and recovery, and offer a bounded classified summary for copy.

## Boundaries & Constraints

**Always:** Use only the fixed tenant-scoped audit read and the current authorized loaded result; preserve Story 5.2 return/source context. Derive target in `userId` → `key` → `TenantId` order, then require valid actor, target, scope, outcome, absolute evidence time, safe projection marker, and audit reference for Ready. Keep command/projection status separate from audit proof. Preserve existing correction integration only where already safely gated by complete Ready evidence. Use Tenants EN/FR whole strings, semantic field pairs, Fluent/FrontComposer controls, stable selectors, keyboard focus, and the existing support-safe classifier.

**Never:** Add receipt endpoints, browser backend reads, hidden-page scans, raw narrative or payload in component inputs, unapproved identifiers in visible/copy/announcement/log state, fabricated proof or timestamps, or new correction behavior. Do not modify history, shared FrontComposer, or unrelated command flows.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Authorized row | Complete current row in tenant-scoped result | Seven labeled facts, optional safe command reference, bounded summary copy | Open and close return focus to row action |
| Missing reference | Reference absent from loaded authorized result, including empty/error page | Visible unavailable receipt and inspect/refresh or paging guidance | No scope substitution or proof claim |
| Incomplete evidence | Missing/unsafe field, unknown projection, or invalid evidence time | Partial or typed unavailable state | No Ready, copy, or correction action |
| Lifecycle signal | Pending/delayed/unavailable/missing support, stale/degraded/unauthorized/cursor error | Distinct localized state and applicable recovery | No command/SignalR inference of audit proof |
| Unsafe content | Narrative extras, credential/PII-shaped identifiers or references | Allow-listed safe fields only | Omit or block unsafe summary with feedback |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs` (`GetTenantAuditAsync`, `IsValidTenantAuditPayload`) -- fixed scoped read and BFF mapping; keep tenant/UTC/event validation.
- `src/Hexalith.Tenants.UI/State/TenantAudit/` (`TenantAuditNarrative`, `TenantAuditRow`, `TenantAuditReceipt`, `TenantAuditSupportSafety`) -- allow-list, target fallback, readiness, and safety policy.
- `src/Hexalith.Tenants.UI/Services/SupportSafety/SupportSafeCopyClassifier.cs` -- existing clipboard classifier.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor` -- loaded-row selection and focus; the receipt is currently inside the nonempty-grid branch.
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/` (`AuditDataGrid`, `AuditEvidenceReceipt`) -- open action, semantic fields, typed recovery, correction gate, and responsive hooks.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources{,.fr}.resx` -- whole-string copy and parity.
- `tests/Hexalith.Tenants.UI.Tests/` (`State/TenantAuditReceiptTests`, `Components/AuditEvidenceReceiptTests`, `Components/TenantAuditPageTests`, `Services/Gateways/TenantQueryGatewayTests`, `Browser/tenants-focus-browser-validation.html`) -- focused verification; old fallback and reference-only copy assertions need revision.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.Tenants.UI/State/TenantAudit/{TenantAuditNarrative,TenantAuditRow,TenantAuditReceipt}.cs`, `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs` -- derive target precedence and sanitized BFF receipt fields; reject missing time, unknown freshness, lifecycle other than Current, or provenance other than ProjectionBacked as Ready. Reject PII-shaped IDs, including plausible phone numbers of 7–15 digits with optional `+`, spaces, hyphens, or parentheses, and ASCII/Unicode field separators in the typed narrative mapping before `userId` → `key` → `TenantId` selects a grid or receipt target. Preserve separate InvalidCursor, Error, Unavailable, Stale, Degraded, and Unauthorized receipt states. Do not place a caller-supplied command hint in receipt proof without an authoritative row-to-command link.
- [x] `src/Hexalith.Tenants.UI/Components/Tenants/Audit/{AuditEvidenceReceipt,AuditAvailabilityState}.razor`, `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditSupportSafety.cs`, `src/Hexalith.Tenants.UI/Resources/TenantsResources{,.fr}.resx` -- render safe fields, a proven optional command reference, bounded classified summary, feedback, and field/copy/close/state/recovery selectors. Validate receipt targets under their BFF-approved typed policy, including safe user IDs that configuration-key rules would reject. Resolve known event/category outcomes to EN/FR whole-string values. Use named summary placeholders `{actor}`, `{target}`, `{scope}`, `{outcome}`, `{timestamp}`, `{projection}`, `{auditReference}` exactly once each; reject escaped, repeated, missing, or unknown names, unsafe field boundaries and Unicode separators before copy. Treat visual vertical-bar lookalikes, including U+FF5C, U+2223, and U+2758, as field separators in typed identifiers and copied summaries. Validate resource presence so missing outcome/projection keys block copy. Keep shared FrontComposer unchanged.
- [x] `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor`, `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditDataGrid.razor` -- render missing-reference state across page states and restore focus without extra reads. Reconcile an open receipt immediately when an authorized result becomes Loading, Error, or invalid, and when the URL receipt reference changes, including a typed interim state while its read is pending. Remove Ready/copy/correction affordances in nonready states. Focus a new deep link once; keep a dismissed deep link closed across refresh/viewport changes while resetting that dismissal on tenant change. Build receipt and correction intent from the matching current snapshot row even if a queued launcher callback supplies an older row. When the URL removes the selected receipt, return focus to its current launcher or the page heading. Return focus to the selected event's current launcher, or to the page heading when missing or when DOM focus fails after async import. Offer filter reset instead of ineffective refresh for an invalid-filter receipt.
- [x] `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceReceipt.razor.css` -- keep complete fields, visible focus, forced colors, reduced motion, and Fluent conformance.
- [x] `tests/Hexalith.Tenants.UI.Tests/{State,Components,Services/Gateways,Browser}/` -- update focused receipt, gateway, resource, and browser tests for every matrix row, safety, focus, live regions, and responsive behavior. Include Loading/invalid-filter invalidation and a clicked receipt Reset that clears filters and triggers a new read, uncorrelated command hint, URL-reference change while a read is pending, deep-link dismissal across tenants, focus after reorder and missing launcher, queued stale-row click, InvalidCursor/Error distinction, every localized outcome pair, named-summary slot swap/escape/omission, PII phone shape and Unicode separator rejection, typed target-policy consistency, resource-missing copy block, receipt recovery selectors, rendered launcher IDs after reorder, administrative both-field target precedence, and stale/degraded missing-reference resolution.
- [x] `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditSupportSafety.cs` -- inspect a Unicode compatibility-normalized value before approval so fullwidth `＠` and similar forms cannot bypass `@` or boundary checks. Reject visual vertical-bar lookalikes including U+2225. Treat Unicode decimal digits and Unicode spacing as phone-number digits and separators for the 7–15 digit PII shape. Keep ordinary safe Unicode identifiers usable when they do not match a prohibited shape. Apply the same boundary policy to copied summary values and labels.
- [x] `src/Hexalith.Tenants.UI/Components/Tenants/Audit/{AuditEvidenceReceipt,AuditAvailabilityState}.razor`, `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor`, and EN/FR resources -- name the copy button for the full summary. Give an unavailable audit read a receipt-specific retry label that describes an audit refresh, rather than command status lookup. Describe blocked copy without suggesting that refresh repairs a missing translation. When a requested reference is absent from a stale or degraded result, show explicit missing-row disclosure alongside the surface state and never present the query hint as verified proof.
- [x] `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor` -- return focus to the page heading when an unsafe URL receipt is removed; after an async focus-module import, validate that the focus request still belongs to the current receipt or close operation. When an automatic read removes a focused receipt control, hand focus to the still-rendered receipt heading or state. Invalidate an active correction panel when its source audit row disappears or ceases to be Ready so it cannot remount on later rows without renewed evidence.
- [x] `tests/Hexalith.Tenants.UI.Tests/{State,Components,Services/Gateways,Browser}/` -- add regression coverage for Unicode-digit/nonbreaking-space phone IDs, fullwidth at-sign email IDs, U+2225 and compatibility-normalized separator IDs; receipt-specific audit retry copy; missing-row disclosure in stale/degraded states; blocked-copy feedback; active correction invalidation; unsafe-query removal and async focus races; and the production receipt heading's `tabindex=-1`.
- [x] `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor` -- resolve a changed receipt URL from the same tenant's current loaded snapshot without forcing a new audit read or clearing the grid; preserve typed Loading only when a read is actually pending. Track whether the active receipt came from the URL or a grid launcher so refresh preserves a manually opened row B while the URL still requests A, and closing either receipt dismisses the outstanding URL request until URL or tenant changes.
- [x] `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditReceipt.cs`, receipt UI/resources, and page focus handling -- assert that a requested row is missing only when an authorized result actually contains a checked page; Error, Unavailable, Unauthorized, Loading, and InvalidCursor must say the row could not be verified. Hand focus from removed receipt controls on both Ready-to-nonready and nonready-to-nonready transitions, without stealing focus from an active audit filter.
- [x] `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditSupportSafety.cs` -- reject further visible vertical dividers U+2016, U+2502, and U+2759 in typed identifiers, references, and copied summary values. Treat malformed UTF-16, including an unpaired surrogate, as unsafe without throwing during route or audit mapping.
- [x] `tests/Hexalith.Tenants.UI.Tests/{State,Components,Browser}/` -- verify supported administrative outcomes at runtime as Ready and copyable with correct EN/FR meaning; safe URL A plus manually opened row B through refresh and Close; same-tenant receipt URL changes without a redundant read; unavailable/error/unauthorized missing-row wording; a missing launcher returning false and falling back to the page heading; nonready action replacement focus; focus retained in an active filter; added visual separators and malformed surrogate fail closed.

**Acceptance Criteria:**
- Given a complete authorized loaded row, when its receipt opens, then exactly seven required safe facts and an optional approved command reference are presented with absolute culture-aware time and a truthful projection marker.
- Given no matching loaded row or incomplete evidence, when receipt resolution runs, then it offers an honest typed state and recovery without copying or enabling correction.
- Given unsafe narrative or identifiers, when BFF mapping and copying run, then raw data never reaches receipt component state or clipboard, and safe values follow the specified fallback order.
- Given keyboard, screen-reader, narrow-width, or forced-colors use, when the receipt opens and closes, then field relationships, state announcements, focus, complete fields, and stable selectors remain usable.

## Implementation Notes

- Review loops 1 and 2 reverted their code revisions after independent review. Their earlier Release builds, full UI passes (3,214 and 3,232 tests), and browser harness passes are historical evidence for discarded versions, not verification of this revision.
- Keep the fixed server-side tenant-scoped read, typed narrative allow-list, target precedence, UTC/freshness readiness gate, no-extra-read selection, safe correction gate, distinct typed states, and semantic EN/FR field presentation while addressing the review findings above.
- Review loop 3 reverted its code revision after independent review. Its Release build, full UI pass (3,251 tests), and browser focus harness pass are historical evidence for the discarded revision, not verification of the next revision.
- Review loop 4 reverted its code revision after independent review. Its Release build, full UI pass (3,281 tests), and browser focus harness pass are historical evidence for the discarded revision, not verification of the next revision.
- Review loop 5's revision passed the Release UI test project build with zero warnings and errors, the full UI suite (3,256 passed; zero failed or skipped), the Chrome 154 focus and responsive harness, and `git diff --check`. It was reverted after independent review; these are historical results, not verification of the next revision.
- The final revision passed a zero-warning Release build, all 3,290 UI tests (zero failed or skipped), the Chrome 154 focus harness, and `git diff --check`. Review loop 6's receipt authority, focus, summary-boundary, localization, and invalid-route fixes have focused regressions. Each frozen matrix row has passing coverage in receipt, page, gateway, or browser checks. Two preexisting grid/reference safety concerns are recorded in the deferred-work ledger.

## Spec Change Log

- Review loop 1 (2026-09-27): Review found that the implementation plan did not specify how receipt authority changes during a load, how an uncorrelated command hint is excluded from proof, how deep-link dismissal and focus survive result changes, or how distinct error states, localized outcomes, safe summary boundaries, and recovery selectors are produced. Amend the non-frozen tasks and verification below to require those behaviors. Avoid the known-bad state in which a Ready receipt survives an invalidated result, a command hint appears as linked evidence, a dismissed URL receipt reopens, an invalid cursor appears to be a missing event, or a copied summary can spoof or omit a field. KEEP the server-side fixed tenant-scoped read, seven-field allow-list, target precedence, UTC/freshness readiness gate, no-extra-read receipt selection, existing safe correction gate, EN/FR field labels, and passing full UI/browser coverage.
- Review loop 2 (2026-09-27): Review found that numbered summary slots can be escaped or swapped in a translation, and that the narrative allow-list admits PII-shaped IDs before target precedence reaches the grid. Amend the non-frozen tasks to require named, exactly-once summary fields and PII rejection at the typed narrative mapping boundary, plus the recorded small safety and verification fixes. Avoid copied summaries with omitted or mislabeled facts and grid targets that expose an email-shaped ID. KEEP the fixed authorized read, distinct typed receipt states, full result-invalidation behavior, uncorrelated command-hint exclusion, EN/FR outcome mapping, deep-link dismissal and row-identity focus, existing correction gate, and passing full UI/browser coverage.
- Review loop 3 (2026-09-28): Review found that a phone-shaped `userId` and Unicode visual vertical bars still pass the typed boundary and can become grid targets or copied field-looking text. Amend the non-frozen safety and test tasks to define phone-number shapes and vertical-bar lookalikes explicitly; record the small row-authority, navigation, reset-test, and target-policy corrections found in the same review. Avoid PII-shaped targets, visual field spoofing, a Ready receipt with its target hidden by a stricter component recheck, and stale callback evidence. KEEP the fixed authorized read, seven safe facts, current lifecycle/projection-backed readiness, named summary slots, EN/FR outcome mapping, distinct typed states, read invalidation, correction gate, deep-link dismissal and focus, full UI suite, and browser focus coverage.
- Review loop 4 (2026-09-28): Review found that ASCII-only phone and `@` checks miss Unicode-digit, Unicode-space, and fullwidth forms; an unlisted visual pipe can still spoof a copied field. It also found an audit-read retry labeled as a command-status lookup, ambiguous missing-reference display on stale/degraded results, and focus/correction authority gaps during asynchronous changes. Amend the non-frozen tasks with compatibility-normalized safety inspection, Unicode phone and separator checks, receipt-specific recovery copy, explicit missing-row disclosure, and focus/correction invalidation tests. Avoid Unicode PII and field-boundary bypasses, stale correction panels, misleading recovery, and lost or stale focus. KEEP the fixed tenant-scoped read, typed target policy, seven semantic facts, current projection-backed readiness, named summary fields, EN/FR outcomes, distinct lifecycle states, reset recovery, row-identity focus, safe correction gate, and passing full UI/browser coverage.
- Review loop 5 (2026-09-28): Review found that receipt URL navigation unnecessarily clears the current result, an outstanding URL reference can override or reopen a manually selected receipt, missing-row copy is asserted without an authorized page, and focus changes can miss removed controls or steal focus from a filter. It also found more visual separators and malformed UTF-16 escaping the safety boundary, plus runtime verification gaps for administrative outcomes and missing-launcher fallback. Amend the non-frozen tasks to require source-aware receipt selection, checked-page wording, conditional focus handoff, broader Unicode rejection, and direct runtime tests. Avoid extra reads, an A/B receipt switch or reopened dismissal, unsupported absence claims, lost or stolen focus, field spoofing, and render exceptions. KEEP the fixed authorized tenant-scoped read, typed target policy, seven semantic facts, current projection-backed readiness, named summary fields, EN/FR outcomes, distinct lifecycle states, reset recovery, correction source-row invalidation, and passing full UI/browser coverage.

## Review Triage Log

| Finding | Verdict and evidence | Route |
| --- | --- | --- |
| BH1 Ready receipt survives invalidation | medium: `LoadAsync` can set Loading and filter validation can set Error while `_selectedReceipt` remains Ready until a later resolution; copy and correction remain rendered. | bad_spec |
| BH2 unrelated command hint | medium: `SupportSafeCommandReference` is a caller-controlled query value passed to `FromRow` without a row relationship check; the new summary copies it beside authorized audit facts. | bad_spec |
| BH3 dismissed URL receipt reopens | medium: `CloseReceiptAsync` clears selection while `ReceiptReference` remains set, and viewport or load reconciliation selects it again. | bad_spec |
| BH4 URL open has no heading focus | medium: `ResolveReceiptSelection` opens a query-selected receipt without setting `_pendingReceiptHeadingFocus`; keyboard focus stays at its prior location. | patch |
| BH5 row index focus drifts | medium: `_receiptLauncherIndex` is captured at open and reused after rows may be replaced or reordered; close can focus another event's launcher. | patch |
| BH6 missing inspect or paging button | false: the required recovery is inspect/unavailable **with refresh or paging**; `InvalidReference` offers refresh and the page pager remains available when the loaded result has pages. | reject |
| BH7 invalid cursor misreported | medium: `Unavailable` maps InvalidCursor through its default arm to InvalidReference, losing the page-one restart state. | bad_spec |
| BH8 Error and Unavailable collapsed | medium: both surface kinds map to receipt Unavailable despite the contract requiring distinct errored and unavailable states. | bad_spec |
| BH9 English outcome in French | medium: `SafeOutcome` returns event/category tokens directly and the receipt renders that raw English value in both cultures. | bad_spec |
| BH10 summary separator spoof | medium: the safety policy admits `|` and labels in identifiers, so an actor can inject a field-looking segment into the copied summary. | patch |
| BH11 missing placeholder can pass | medium: `SafeReceiptSummary` tests value containment after formatting; equal field values let a format missing one indexed slot pass silently. | patch |
| BH12 recovery selectors absent on shared state | medium: receipt pending/unavailable/missing-support recovery buttons come from `AuditAvailabilityState`, which renders no receipt-specific button selector. | bad_spec |
| EC1 Ready during refresh | medium: the Loading write does not reconcile `_selectedReceipt`, confirming BH1 for a manual refresh. | bad_spec |
| EC2 focus after reorder | medium: the saved row index is not reconciled to the selected event after a refreshed result, confirming BH5. | patch |
| EC3 deep-link dismissal | medium: the unchanged query parameter is read again by `ResolveReceiptSelection`, confirming BH3. | bad_spec |
| EC4 deep-link focus | medium: only button open sets the pending heading focus flag, confirming BH4. | patch |
| EC5 invalid cursor | medium: the `Unavailable` switch has no InvalidCursor arm, confirming BH7. | bad_spec |
| EC6 summary slot | medium: formatted value containment cannot prove the presence of each indexed placeholder, confirming BH11. | patch |
| VG1 rendered focus linkage | medium: page tests mock the JS call and browser tests hardcode the attribute, leaving the rendered two-row index-to-launcher linkage untested. | patch |
| VG2 stale missing reference coverage | medium: existing missing-reference page cases cover Empty, Error, and Unauthorized, while Stale and Degraded are not exercised through page resolution. | patch |

### Review loop 2

| Finding | Verdict and evidence | Route |
| --- | --- | --- |
| BH2-1 old receipt after query change | medium: `OnParametersSetAsync` clears the selected reference but leaves `_selectedReceipt` Ready while its awaited load runs, so the old URL selection stays copyable. | patch |
| BH2-2 unmatched requested reference shown as proof | false: the reference is presented in a receipt explicitly labeled not loaded, with no Ready, copy, or correction action; the requested hint is not asserted as verified proof. | reject |
| BH2-3 arbitrary target without narrative | false: every production `FromRow` caller receives rows mapped by `TenantAuditRow.FromEntry`, which always supplies a typed narrative; no live path supplies the reviewer’s synthetic null-narrative row. | reject |
| BH2-4 Empty surface with a row | false: Empty and FilteredEmpty snapshots have no rows to select, and the production page constructs an unavailable receipt when the requested row is absent. | reject |
| BH2-5 Unicode line separator spoof | medium: `char.IsControl` and the Format-category check admit U+2028/U+2029, allowing visual field breaks in a copied summary. | patch |
| BH2-6 escaped numeric slot | medium: `Split("{0}")` counts the substring inside `{{0}}`, while `string.Format` emits a literal and omits that field value. | bad_spec |
| BH2-7 missing resource key copied | medium: `IStringLocalizer` can return a key for a missing outcome or freshness resource; the summary gate accepts that safe-looking text as a field value. | patch |
| BH2-8 numbered slots | medium: the epic requires named summary placeholders; numeric slots allow a translator to swap actor and target labels without the safety gate noticing. | bad_spec |
| BH2-9 invalid-filter receipt retries invalid input | medium: the receipt's refresh action calls `RefreshAsync`, which reapplies unchanged invalid filters, while the page's reset action is the applicable recovery. | patch |
| BH2-10 administrative PII target | medium: `TenantAuditNarrative.SafeIdentifier` accepts email-shaped `userId`, and the new category-independent precedence puts it in the grid target even when a safe key exists. | bad_spec |
| BH2-11 machine outcome token crosses component boundary | false: the token is a fixed allow-listed outcome fact, one of the seven permitted receipt fields; the component maps it to a localized whole string before display or copy, and no raw narrative crosses. | reject |
| BH2-12 browser fixture is hand-built | false: bUnit renders the production receipt, grid, and page handoff, while the browser harness exercises the shipped focus module and compiled scoped CSS; the combined checks cover the cited mismatch. | reject |
| EC2-1 old receipt after query change | medium: resetting only `_selectedReceiptReference` leaves the old receipt rendered until load reconciliation, confirming BH2-1. | patch |
| EC2-2 administrative PII target | medium: the narrative allow-list admits `@`, and grid target uses that field first, confirming BH2-10. | bad_spec |
| EC2-3 escaped numeric slot | medium: `{{0}}` passes the substring count and formats to literal `{0}`, confirming BH2-6. | bad_spec |
| EC2-4 Empty surface with a row | false: production snapshots and selection cannot yield the stated Empty-plus-row condition; the public helper's synthetic combination is not an observed user path. | reject |
| EC2-5 launcher vanishes during focus import | low: `focusElementById` may return false after an async import if the selected launcher was removed; no heading fallback then runs. | patch |
| EC2-6 summary slot claim | medium: escaped numeric slots refute the spec's claimed slot verification, confirming BH2-6. | bad_spec |
| VG2-1 reordered launcher ID assertion | medium: the reordered-row test checks row text and a mocked JS request but never asserts the selected row's post-refresh launcher ID. | patch |
| VG2-2 administrative both-field target assertion | medium: the gateway fixture has `userId` and `key` but no Target assertion, so the old category-specific branch could return without test failure. | patch |
| VG2-3 outcome mapping assertions | medium: only one of eleven event/category mappings is rendered in tests; an opposite outcome resource mapping could pass unnoticed. | patch |

### Review loop 3

| Finding | Verdict and evidence | Route |
| --- | --- | --- |
| BH3-1 stale click row | medium: `OpenReceiptAsync` checks the current snapshot for the reference but passes the callback's older row to receipt and correction creation; a queued click after refresh can show stale proof. | patch |
| BH3-2 dismissal across tenants | medium: changing only `TenantId` leaves `_dismissedReceiptReference` intact, so the same requested reference stays closed in the next tenant. | patch |
| BH3-3 URL removal loses focus | medium: a query change clears the rendered receipt without scheduling focus return; focus inside its removed control can fall to the document body. | patch |
| BH3-4 target policy mismatch | medium: `FromRow` accepts `infrastructure-admin` as a typed user target, while the component's `ConfigurationKey` recheck hides it and blocks copy in a Ready receipt. | patch |
| BH3-5 translated labels could swap | false: the shipped EN/FR templates bind each label to its named field correctly; the proposed swapped template is not a reachable current resource value. | reject |
| BH3-6 Unicode lookalike separator | medium: U+FF5C in a typed identifier passes the present boundary checks and visually imitates a summary field separator. | bad_spec |
| BH3-7 classifier kind for summary | false: `ApprovedReference` is a broad explicit-approval kind in the existing classifier, which only checks nonempty approved text; the summary is additionally validated before it reaches that gate. No copy bypass follows from the name. | reject |
| BH3-8 partial copy feedback | false: an unsafe required field produces the localized Partial state that says complete proof cannot be cited; the Ready-only alert covers a separate failure of safe summary assembly. | reject |
| BH3-9 pending states unreachable on page | false: the historical page has no authoritative row-to-command link, so it must not infer command pending/delayed state from the uncorrelated navigation hint; the model and component preserve distinct states when an authoritative caller supplies one. | reject |
| BH3-10 permission recovery absent inside receipt | false: the page renders its configured permission link alongside an Unauthorized receipt, so the recovery is available without inventing a second receipt action. | reject |
| BH3-11 escalation absent inside receipt | false: the page renders its configured escalation link for Error and Unavailable, while receipt recovery offers refresh. | reject |
| BH3-12 phone-shaped user ID | medium: `TenantAuditNarrative.FromPayload` accepts a phone-shaped `userId` such as `+1-202-555-0100`, which new category-independent target precedence can expose in grid and receipt. | bad_spec |
| EC3-1 dismissal across tenants | medium: the tenant-change branch clears selection but leaves `_dismissedReceiptReference`, confirming BH3-2. | patch |
| EC3-2 stale click row | medium: the current-reference membership guard does not replace the callback row, confirming BH3-1. | patch |
| EC3-3 target policy mismatch | medium: typed user target and component configuration-key validation differ, confirming BH3-4. | patch |
| EC3-4 URL change while read pending | medium: `OnParametersSetAsync` clears the old receipt and awaits a same-tenant read before resolving the new reference, leaving no typed receipt during the pending read. | patch |
| VG3-1 reset action untested | medium: the receipt reset test asserts a button exists but never clicks it; changing its callback to retry would leave the test green while recovery fails. | patch |
| VG3-2 target policy mismatch | medium: existing tests use a target accepted by both policies, so the Ready-but-hidden target in BH3-4 is untested. | patch |

### Review loop 4

| Finding | Verdict and evidence | Route |
| --- | --- | --- |
| BH4-1 nonbreaking-space phone | medium: the phone detector permits only ASCII spaces, so a 10-digit phone separated by U+00A0 enters the typed user target and copied summary. | bad_spec |
| BH4-2 fullwidth at-sign email | medium: canonicalization only percent-decodes, so `person＠example.test` bypasses the ASCII `@` check and reaches the grid or receipt. | bad_spec |
| BH4-3 unlisted visual pipe | medium: U+2225 is absent from the separator set and can imitate a field boundary in an identifier and summary. | bad_spec |
| BH4-4 long or braced field Ready | false: Ready describes audit evidence completeness; bounded copy has a separate explicit safety gate and assertive blocked-copy feedback. A long or braced field does not make the underlying timestamp or projection proof false. | reject |
| BH4-5 missing translation while Ready | false: a missing resource does not invalidate the authorized audit row; the component shows a placeholder and blocks copy, while correction remains governed by row and projection evidence. | reject |
| BH4-6 copy accessible name | medium: the existing EN/FR accessible name says copy a reference, while the button writes all seven fields. | patch |
| BH4-7 audit retry mislabeled status lookup | medium: Unavailable routes through command-oriented `AuditAvailabilityState`, whose refresh button says “Retry status lookup” but invokes the audit read. | bad_spec |
| BH4-8 blocked-copy feedback suggests ineffective refresh | low: a missing translation produces blocked copy, and the current feedback tells users to refresh audit evidence even though a read cannot restore a missing resource. | patch |
| BH4-9 unsafe URL removal loses focus | medium: an unsafe requested reference renders an unavailable receipt but leaves `_selectedReceiptReference` null, so query removal skips the focus-return flag. | patch |
| BH4-10 delayed heading focus races close | medium: `OnAfterRenderAsync` clears the pending focus flag before awaiting module import; a later close can focus the launcher before the older open request focuses a stale heading. | bad_spec |
| BH4-11 automatic invalidation removes focused control | medium: refresh can replace a focused copy or correction control with unavailable state without handing focus to the still-rendered receipt heading or state. | bad_spec |
| BH4-12 alternate French punctuation or order | false: the shipped EN/FR templates satisfy the fixed field-label contract and copy; a hypothetical future translation that changes order or separators would fail closed with blocked-copy feedback. | reject |
| EC4-1 Unicode-digit phone | medium: `LooksLikePhoneNumber` counts only ASCII digits, so a 7–15 digit Unicode-number string can bypass the phone-shaped identifier block. | bad_spec |
| EC4-2 active correction outlives audit authority | medium: after an empty refresh removes its source row, `_activeCorrectionIntent` is retained; a later nonempty result can remount its panel without renewed audit evidence. | bad_spec |
| EC4-3 stale/degraded missing hint resembles proof | medium: `Unavailable` retains the requested reference for a missing row, while Stale/Degraded copy discusses audit evidence without saying the row is absent and Degraded says to use the reference with limitations. | bad_spec |
| VG4-1 rendered receipt heading focusability untested | medium: page JS mocks and browser fixture supply their own focusable heading; removing `tabindex=-1` from the production receipt would leave them passing. | patch |
| VG4-2 copy accessible name | medium: the EN/FR resource still describes reference-only copy, confirming BH4-6. | patch |

### Review loop 5

| Finding | Verdict and evidence | Route |
| --- | --- | --- |
| BH5-1 same-tenant URL forces read | medium: `OnParametersSetAsync` calls `LoadAsync` for a receipt-only URL change, and the new `retainConfirmed: !receiptRouteChanged` clears a still-authorized grid while a redundant read runs. | bad_spec |
| BH5-2 URL A overrides grid B | medium: `ResolveReceiptSelection` always prefers `ReceiptReference`, so a later refresh silently replaces manually opened B with URL A. | bad_spec |
| BH5-3 Close B reopens URL A | medium: Close stores B as dismissed and clears `_selectedReceiptReference`; the next reconciliation cannot match the outstanding URL A and opens it. | bad_spec |
| BH5-4 pending correction after receipt close | false: the operator already invoked Start correction, and Close receipt does not revoke the source row's Ready audit authority; the async completion rechecks that row and fresh projection. | reject |
| BH5-5 nonready action loses focus | medium: focus handoff is scheduled only for Ready-to-nonready; a nonready Restart or Reset control can disappear during another nonready state transition. | bad_spec |
| BH5-6 unverified row declared missing | medium: `Unavailable` sets the missing-row flag for Error, Unavailable, and Unauthorized even though those states provide no authorized checked page. | bad_spec |
| BH5-7 filtered result receipt refresh | low: receipt Refresh reapplies valid filters, but the page renders its Reset action beside the filtered result, so usable recovery is already present; adding a separate receipt policy branch would add state complexity for little gain. | reject |
| BH5-8 MissingSupport escalation copy | false: the page never derives MissingSupport from its historical audit read and thus never renders that receipt state; direct component state is not a reachable page path, while Error/Unavailable page states already render configured escalation recovery. | reject |
| BH5-9 further visual dividers | medium: U+2016 and U+2502 pass `ContainsFieldBoundary` and can visually split a typed grid target or copied field. | bad_spec |
| BH5-10 malformed UTF-16 normalization | medium: `Normalize(FormKC)` can throw on an unpaired surrogate after `ContainsInvisibleOrControl` accepts its replacement rune, turning an unsafe URL or audit identifier into a render/read failure. | bad_spec |
| BH5-11 outcome meaning untested | medium: the resource test checks presence and difference only; swapping two translations would pass while support reads the wrong event result. | patch |
| VG5-1 administrative runtime outcome gap | medium: receipt tests exercise Ready for an Access event only, so removing a supported administrative outcome from `IsKnownOutcome` would disable its copy without a failing runtime test. | patch |
| VG5-2 missing launcher page fallback gap | medium: browser helper and page tests never combine a safe disappeared launcher with a false JS focus result, so the page heading fallback could break unnoticed. | patch |
| EC5-1 dismissed deep link reopens | medium: after Close clears `_selectedReceiptReference`, the dismissal guard no longer matches the safe URL reference, confirming BH5-3. | bad_spec |
| EC5-2 error says row absent | medium: Error, Unavailable, and Unauthorized set `IsRequestedReferenceMissing` without a checked page, confirming BH5-6. | bad_spec |
| EC5-3 U+2759 separator | medium: U+2759 also passes the explicit divider set and can spoof a field boundary in grid or copied summary text. | bad_spec |
| EC5-4 filter focus stolen | medium: any Ready-to-nonready transition schedules receipt heading focus, including filter validation while focus remains in the filter; the handoff must depend on focus being in a removed receipt control. | bad_spec |

### Review loop 6

| Finding | Verdict and evidence | Route |
| --- | --- | --- |
| BH6-1 compatibility semicolon | medium: a fullwidth semicolon survives the pre-normalization identifier-only check and becomes an unchecked ASCII semicolon after FormKC normalization. | patch |
| BH6-2 remaining visual dividers | medium: U+00A6 and U+2551 pass the current explicit divider set and can visibly split copied fields. Adding these characters to the existing set is a direct correction. | patch |
| BH6-3 incidental secret substrings in event IDs | medium: the strict reference classifier can reject an otherwise valid opaque ID containing `jwt` or `eyj`; the same strict fragments and gateway row validation existed before this story. | defer |
| BH6-4 grid reference copy includes context | medium: the existing grid copy combines a reference with typed narrative context while labeling it a reference; this behavior predates this receipt story and the diff does not change that copy path. | defer |
| BH6-5 unsafe URL says not loaded | low: a rejected reference is indeed not in the loaded result, but the message does not distinguish invalid input from an absent row. Unsafe URL input is uncommon, and a distinct state/copy branch adds complexity without changing proof or recovery. | reject |
| BH6-6 unverified hint in evidence field | medium: `Unavailable` retains the URL hint and the component labels it “Audit reference”; degraded copy also says to use the reference. The missing-row notice helps but does not separate request from verified evidence. | patch |
| BH6-7 whole receipt live region | medium: the existing section-level live region now encloses the new fields and feedback plus nested live regions, so state changes can reannounce controls and duplicate feedback. A state-only live region is a direct markup fix. | patch |
| BH6-8 same-state control removal focus | medium: page-load projection enrichment can change correction action availability while receipt State remains Ready, and viewport changes can remove that action; the state-change-only focus probe misses both demonstrated paths. | patch |
| BH6-9 grid correction survives Loading | medium: Loading calls `ResolveReceiptSelection`, whose no-selection return precedes `CaptureCorrectionAuthority`; a grid-started correction panel can keep stale authority until a later result. | patch |
| BH6-10 FromEntry is Partial without provenance | false: `FromEntry` lacks lifecycle and provenance inputs, so Partial is the truthful result; a public helper cannot claim Ready without those required facts, and production receipt selection uses mapped rows. | reject |
| BH6-11 synthetic browser receipt | false: carried from BH2-12; bUnit checks production receipt/page markup and the browser harness checks shipped focus JavaScript, with their combined coverage exercising the stated handoff. | reject |
| EC6-1 URL hint and scope as evidence | medium: `Unavailable` puts requested route values into labeled receipt evidence fields without a matched row, confirming BH6-6. | patch |
| EC6-2 invalid tenant retains prior reference | medium: the invalid-tenant branch changes the snapshot but retains `_selectedReceiptReference`, so reconciliation can display a prior tenant's URL/reference on an invalid route. | patch |
| EC6-3 late focus probe after Close | medium: `CaptureReceiptFocusForReplacementAsync` awaits JS without rechecking its generation or selected receipt before scheduling heading focus, so it can override Close. | patch |
| EC6-4 Unauthorized wording | medium: the English and French Unauthorized receipt state text omits that the requested event could not be verified, contrary to the explicit task; adding that clause is a direct resource correction. | patch |
| VG6-1 French runtime copy gap | medium: parity and outcome tests read French resource values, but no component test executes the French seven-field template and clipboard path, so a broken French slot could hide Copy unnoticed. | patch |
| Root6-1 semicolon in copied summary | medium: the shared field-boundary set omits ASCII semicolon for approved references and summary values; a copied reference can carry a field-looking semicolon segment. | patch |

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release -m:1 --no-restore` -- zero errors and warnings.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -parallelMode none` -- all UI tests pass; use focused `-class` during development.
- `git diff --check` -- no whitespace errors.

### Review Findings

Code review 2026-09-28 of `33d6dcca..7ac52e8d`. Mid-review, a concurrent session rebased the range to `051f9f30..f8223524`. The rebase dropped only the two out-of-scope workspace test hunks, and the second commit is patch-identical. All four review layers completed. Independent verification:

- The Release UI test build had 0 warnings and 0 errors, and the UI suite passed 3,290/3,290 at both `7ac52e8d` and `f8223524`.
- `git diff --check` was clean.
- `validate-story-gitlinks.py` exited with 1 (4 UNDECLARED).
- A temporary bUnit probe, since removed, reproduced the prerender JS-interop call path.

- [ ] [Review][Patch] Declare the four submodule pointer moves forward-only (decision D1, following the 5.2 precedent): add a `## File List` whose bare list items are `references/Hexalith.AI.Tools`, `references/Hexalith.EventStore`, `references/Hexalith.FrontComposer` and `references/Hexalith.Memories`, with provenance notes that contain no `->` arrows, and a note explaining why they rode in `7ac52e8d`. Then rerun `validate-story-gitlinks.py` [_bmad-output/implementation-artifacts/spec-5-3-view-a-support-safe-audit-evidence-receipt.md:1]
- [ ] [Review][Patch] An Access-category receipt is Ready only when the row has a safe typed narrative `userId` (decision D2). The `userId` → `key` → `TenantId` fallback stays for display; otherwise the receipt is Partial, with no Copy or correction [src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditReceipt.cs:67]
- [ ] [Review][Patch] Render the grid row's narrative context as separate visible text, and make the reference copy button copy the approved event reference only (decision D3; this also resolves the deferred reference-only-copy item). Correct the false Story 5.3 deferred-work note [src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditDataGrid.razor:192]
- [ ] [Review][Patch] Limit the new `@` and phone-shape rejection to `SupportSafeCopyValueKind.UserId` (the narrative user ID and the actor). TenantId, ConfigurationKey and ApprovedReference return to the prior policy, which restores numeric tenant IDs and the removal and update proof matching for them (decision D4) [src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditSupportSafety.cs:116]
- [x] [Review][Defer] The deny-list safety policy still admits PII and separator lookalikes [src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditSupportSafety.cs:163] — deferred (decision D5): stop adding characters one at a time inside this story, and move typed user IDs to an allow-listed character set with a confusable-skeleton check as a design item. Known open bypasses: phone numbers written with `.`, `/`, `tel:` or a leading space; a fullwidth `％40`; and U+2503, U+275A, U+01C0, U+23D0, U+204F and U+061B.
- [ ] [Review][Patch] Receipt deep links crash prerender: `OnParametersSetAsync` → `LoadAsync` → `CaptureReceiptFocusForReplacementAsync` issues JS interop during static rendering, and the resulting `InvalidOperationException` is not caught. Return early when `!RendererInfo.IsInteractive`, and add a prerender regression test [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1561]
- [ ] [Review][Patch] Loading never paints while a receipt is open: the focus probe yields first and the Loading write has no `StateHasChanged`, so Refresh, Apply and paging leave the pre-read Ready receipt (with Copy and Start correction) interactive for the whole read. Render inside the Loading write, and add the pending-probe plus pending-read test [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:850]
- [ ] [Review][Patch] Start correction silently does nothing when the refreshed intent is unavailable. The receipt's cached `_selectedReceiptCorrectionIntent` stays enabled, so every click refreshes the projection and returns without showing a reason. Re-resolve the receipt intent and render the unavailable reason [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1443]
- [ ] [Review][Patch] The Loading receipt state is announced assertively (`role="alert"`) on every refresh with an open receipt, shows a `?` glyph, and offers a Refresh that restarts the in-flight read. Make Loading polite, give it the pending glyph, and offer no action [src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceReceipt.razor:312]
- [ ] [Review][Patch] Add the typed target-policy regression test claimed by task 57: a user target that the UserId policy accepts but the ConfigurationKey rules reject (for example `infrastructure-admin`) stays visible, Ready and copyable [src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceReceipt.razor:272]
- [ ] [Review][Patch] Test that removing a safe, loaded receipt from the URL returns focus to its launcher (`tenants-audit-receipt-launcher-1`), not to the page heading [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:731]
- [ ] [Review][Patch] Test the post-refresh guards in `OpenCorrectionAsync`: no panel opens when the source row is removed or stops being Ready, or when the refreshed intent becomes unavailable, while the projection read is pending [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1431]
- [ ] [Review][Patch] Test the correction focus hand-off after enrichment: with the same row, a detail refresh that makes the correction unavailable while focus is on Start correction moves focus to the receipt heading [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:936]
- [ ] [Review][Patch] A correction panel invalidated by `CaptureCorrectionAuthority` drops keyboard focus to `<body>`. Hand focus back the way `CloseCorrectionAsync` does [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1526]
- [ ] [Review][Patch] The receipt header grid has two columns but now three children, so Copy (or the copy-blocked alert) wraps under the title [src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceReceipt.razor.css:16]
- [ ] [Review][Patch] The member-removal flow shares the receipt, but its tests' stub localizer lacks the Summary, Outcome and Close keys. A Ready removal receipt therefore renders copy-blocked with a raw-key Close label, and no test notices. Add the keys and assert the summary copy and Close [tests/Hexalith.Tenants.UI.Tests/Components/RemoveTenantMemberFlowTests.cs:1731]
- [ ] [Review][Patch] Remove the discarded receipt parameters (`supportSafeCommandReference`, `requestedReference`, `tenantId`) and the page's pass-through of them, plus the unused `Tenants.Audit.Receipt.ReferenceLiteral` resource and its stub entries [src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditReceipt.cs:59]
- [ ] [Review][Patch] Assert exactly seven receipt fields (`ShouldBe(7)`) instead of at least seven [tests/Hexalith.Tenants.UI.Tests/Components/AuditEvidenceReceiptTests.cs:45]
- [ ] [Review][Patch] Read and clear `_pendingCorrectionFocusReference` before the receipt-focus `ConfigureAwait(false)` awaits in `OnAfterRenderAsync`, not off the dispatcher after them [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:799]
- [ ] [Review][Patch] Re-entering the same tenant's route no longer retries a failed read-refresh subscription. Call `EnsureReadRefreshSubscriptionAsync` before the early return [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:739]
- [ ] [Review][Patch] Assert that the production receipt renders `tenants-correction-start` inside `.audit-evidence-receipt__correction`, the class the JS focus probe depends on [src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceReceipt.razor:103]
- [ ] [Review][Patch] The Story 5.3 deferred-work entries sit under the Story 5.2 heading with relative `source_spec` paths. Give them their own heading [_bmad-output/implementation-artifacts/deferred-work.md:3218]
- [x] [Review][Defer] Unaccented French receipt strings (`prete a citer`, `perimee`, `Reference de commande`, …) and mixed apostrophes [src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx:3430] — deferred: pre-existing since 2026-06-06; this story accented only the strings it touched.
- [x] [Review][Defer] The receipt timestamp uses the fixed pattern `yyyy-MM-dd HH:mm:ss 'UTC'` rather than culture-aware formatting [src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditReceipt.cs:37] — deferred: pre-existing Story 5.1 UTC convention, shared with the grid.

#### Rejected

- Unrelated workspace-routing test rewrites (blind, auditor) — false: the rebase onto `051f9f30` dropped those hunks, and the story commits at `f8223524` no longer touch `TenantListSurfaceTests` or `TenantsWorkspaceTests`.
- Spec `status: 'done'` while sprint-status says `review` (auditor) — low: the review-completion step reconciles it.
- Receipt state never written to the URL (blind) — false: dismissal lasting until the URL or tenant changes is the specified behavior, and a deep link that reopens on reload is expected.
- Stale `OnParametersSetAsync` comment (blind) — false: the comment still correctly explains why a same-tenant re-entry must not re-read.
- Same-tenant query-only navigation no longer re-reads (auditor) — low: none of the six query parameters feed the request, so a re-read returned the same data; freshness comes from the notification subscription.
- Event-type allow-lists and checked-page lists duplicated in four places (blind) — low: no path where they currently diverge was shown; a consolidation refactor is not worth it now.
- Focus-module import races and the leaked `IJSObjectReference` (blind, edge) — low: at most one extra module reference per circuit, released when the circuit ends; the fix adds a cached-task field.
- Probe `TaskCanceledException` (JS interop timeout) aborts the read (edge) — low: needs a client unresponsive for 60 seconds; the fix is a guard for an undemonstrated state.
- `ReferenceEquals` probe skip when a concurrent resolve replaces the receipt (edge) — low: needs a second dispatcher event inside the probe round trip, and the fix changes the stale-probe guard's meaning.
- Index-based launcher focus after rows change between Close and render (edge) — low: needs a load to apply inside the focus import await; the fix means resolving the reference at focus time.
- Empty `?receiptReference=` opens an unavailable receipt (edge) — low: an empty requested reference is an invalid request, and reporting it as not loaded is honest (the same rule rejected BH6-5).
- Degraded/Stale wording beside the missing-row notice (auditor) — low: only degraded results with a missing row are affected, the notice already discloses the absence, and a distinct state adds a branch plus resources.
- Invalid filters reported as a failed read (auditor, edge) — low: the page's state section names the invalid filters, the receipt offers the correct Reset, and distinct copy would add a branch plus resources.
- Recovery selectors `-continue` vs `-continuereadonly` (auditor) — low: each is stable per state, and renaming the shared `AuditAvailabilityState` IDs would break the other flows' selectors.
- Receipt Unavailable copy mentions escalation without an Escalate button (edge) — low: the page renders its configured escalation link for Unavailable and Error.
- Summaries over 2,048 characters block copy with a misleading message (blind) — low: needs identifiers near the 256-character limit in every field.
- Browser harness leaves its receipt fixtures in the DOM (blind) — low: no later harness assertion queries those IDs, and the harness passes.
