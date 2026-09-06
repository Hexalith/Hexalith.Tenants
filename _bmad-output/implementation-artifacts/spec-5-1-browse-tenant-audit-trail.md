---
title: 'Browse Tenant Audit Trail'
type: 'feature'
created: '2026-09-05'
status: 'in-progress'
baseline_revision: '0ca32a5cf6448f35b67f29f0ddcbce44d144b05e'
baseline_commit: '0ca32a5cf6448f35b67f29f0ddcbce44d144b05e'
review_loop_iteration: 1
followup_review_recommended: false
context:
  - '_bmad-output/implementation-artifacts/epic-5-context.md'
warnings:
  - 'oversized'
deferred: []
operator_actions:
  - 'Approve and record the Product/Operations audit-performance contract, including the representative dataset shape, page size and filter mix, reference environment and network assumptions, initial-render and interaction percentile budgets, authoritative test tier and repeatability method, and fallback trigger.'
---

<intent-contract>

## Intent

**Problem:** The existing tenant audit page satisfies the historical Story 5.1 contract but not the corrected contract: malformed filters can broaden queries, retained rows are not caller-bound, unsafe actor/context values can render, recovery states are incomplete, mobile exposes correction controls, and audit CSS uses legacy Fluent tokens. Historical performance evidence also lacks the required Product/Operations authority.

**Approach:** Harden the existing InteractiveServer BFF, state, grid, localization, and tests without changing the fixed REST route or server ordering. Complete every repository-controlled requirement, preserve later-story features on safe viewports, and leave only the explicit performance approval/evidence action to the operator.

## Boundaries & Constraints

**Always:** Preserve tenant/caller/filter/cursor scope, server ordering, opaque paging, absolute culture-aware timestamps, authoritative freshness/projection provenance, safe retained rows, EN/FR parity, stable selectors, accessible state/recovery semantics, and FrontComposer/Fluent UI v5 composition. Keep `sprint-status.yaml` read-only.

**Never:** Add a browser backend call, token storage, generic EventStore route, endpoint, global audit inventory, client-side resort, offset paging, raw payload/metadata/PII output, fabricated receipt/proof, inferred performance budget, generic audit timeline, or high-impact correction control on a phone viewport.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Filters | Valid UTC bounds and allowed category | Clear cursor history and query page 1 with exact server filters | Localized validation and no query for malformed/reversed bounds or unknown category |
| Cursor invalidation | Protected cursor rejected | Clear paging state, reload page 1, and announce a localized list-refreshed notice | Never expose the cursor or collapse to generic failure |
| Retained evidence | Refresh fails after caller/scope change | Retain only same-caller, same-tenant/filter/cursor rows with downgraded provenance | Otherwise discard rows and render the applicable failure state |
| Unsafe audit fields | Actor, context, or narrative resembles secrets/PII/internal data | Emit only approved support-safe values | Blank/reject unsafe presentation fields without logging their values |
| Phone viewport | Audit rows include later correction affordances | Preserve read-only critical context and references | Suppress high-impact correction controls while keeping recovery/navigation usable |

</intent-contract>

## Code Map

- `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:50` -- filter controls, state/recovery UI, cursor history, viewport observation, and grid action composition.
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditDataGrid.razor:5` -- authoritative-order Fluent grid, safety-critical columns, row selectors, receipts, and correction affordances.
- `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditRow.cs:52` -- contract-to-support-safe row mapping; actor currently bypasses the established classifier.
- `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditSnapshot.cs:199` -- retained-snapshot scope match; caller identity is currently absent.
- `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditSupportSafety.cs:49` -- reusable identifier/reference classifier.
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs:1020` -- authenticated BFF read, invalid-cursor retry, retention, freshness, and direct-client mapping.
- `src/Hexalith.Tenants/Queries/TenantQueryCursorScopes.cs` and `src/Hexalith.Tenants/Queries/Handlers/GetTenantAuditQueryHandler.cs` -- protected server cursor scope; bind audit cursors to `QueryEnvelope.UserId` in addition to tenant and filters.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor.css:17` -- two legacy token usages plus forced-color/focus layout hooks.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` and `TenantsResources.fr.resx` -- whole-string validation, recovery, mobile-read-only, and state copy.
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs:255` -- page/filter/paging/state/responsive/component-boundary coverage.
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs:3138` -- exact query, caller-bound retention, invalid cursor, provenance, and support-safety coverage.
- `tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs` -- Fluent v5 and prohibited legacy-token guard.
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs` and `tests/Hexalith.Tenants.Server.Tests/Queries/TenantQueryCursorCodecTests.cs` -- caller/tenant/filter cursor isolation and authoritative audit paging.
- `tests/Hexalith.Tenants.IntegrationTests/TenantsUiRouteSmokeTests.cs` -- hosted route, context, recovery, and secret-absence smoke evidence; complement it with blocking authorized component coverage because this lane can self-skip.

## Tasks & Acceptance

**Execution:**
- [ ] `src/Hexalith.Tenants/Queries/TenantQueryCursorScopes.cs`, `src/Hexalith.Tenants/Queries/Handlers/GetTenantAuditQueryHandler.cs`, and focused server tests -- include the authenticated `QueryEnvelope.UserId` in audit cursor protection and prove a cursor issued to one caller is rejected for another even when tenant and filters match.
- [ ] `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor` and `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor.css` -- validate filters before loading, clear incompatible paging, use field-specific `aria-invalid` plus an associated validation description, label absolute filter input as UTC, canonicalize/reject encoded unsafe return URLs, guard queued viewport callbacks after disposal, add state-specific localized recoveries, keep mobile read-only, and replace legacy tokens.
- [ ] `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditDataGrid.razor` -- preserve authoritative row order and unique stable item keys, expose phone-width suppression only for rows that otherwise have a supported correction, distinguish unmeasured viewport evidence from a measured phone, and retain accessible critical fields/selectors.
- [ ] `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditRow.cs` and `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditSupportSafety.cs` -- validate the complete supported event-type/category matrix; reject missing, unsafe, or duplicate event references; preserve allow-listed narrative as typed sanitized fields for correction logic instead of reparsing display text; and cover encoded/general token, authorization, secret, password, credential, and connection-string markers without disclosing rejected values.
- [ ] `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditSnapshot.cs` and `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs` -- bind retained evidence to caller, tenant, filters, cursor, page size, and validator; validate response count, non-default bounded timestamps, category/filter agreement, event/category coherence, tenant identity, and stable references; after every awaited audit read re-read the current caller and discard the result on identity change (Unauthorized when absent, otherwise Unavailable bound to the new caller, with no retained rows).
- [ ] `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` and `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx` -- add parity-checked whole-string UTC validation, recovery, viewport, and mobile-read-only copy with idiomatic accented French.
- [ ] Focused UI/server/integration tests -- cover changed-caller 304 refetch and unexpected-exception retention, identity change during an awaited response, invalid-cursor recovery after page-two history, malformed and non-UTC `To`, the full event allowlist/category matrix, typed role narrative mapping, exact recovery affordance sets and refresh invocation, encoded unsafe return URLs, duplicate/unsafe references, response filter/count/timestamp violations, viewport-disposal races, and authorized populated-grid semantics. Keep the hosted smoke test supplementary because it can self-skip.
- [ ] `_bmad-output/implementation-artifacts/spec-5-1-browse-tenant-audit-trail.md` -- record exact verification evidence and finish as `awaiting-operator` with the unchanged operator action after all agent-controlled work is committed.

**Acceptance Criteria:**
- Given an authorized tenant route, when audit loads, then only the fixed direct REST client and approved flat Fluent grid are used and server order is preserved.
- Given filters or cursor navigation, when inputs change or invalidate, then page-one restart, opaque caller/tenant/filter-bound paging, localized field-associated validation, explicit UTC input semantics, and no-query failure behavior match the matrix.
- Given loading, empty, filtered-empty, stale, degraded, unauthorized, invalid-cursor, unavailable, or error, when rendered, then each localized accessible state offers its applicable reset, refresh, continue-read-only, request-permission, or escalation recovery without success semantics.
- Given retained rows or an in-flight read, when freshness degrades or identity/scope changes, then only same-caller compatible evidence remains, an old-caller completion is discarded, and any retained provenance is visibly non-current.
- Given structured audit data, when mapped/rendered/copied/announced, then response shape, timestamps, filters, event/category pairs, unique references, and tenant identity are validated; correction logic consumes typed sanitized narrative rather than display text; and no cursor, validator, raw narrative, internal correlation, token, metadata, stack trace, claim, or unapproved PII escapes.
- Given desktop, tablet, phone, keyboard, screen-reader, forced-color, high-contrast, or reduced-motion use, when the surface renders, then critical columns, table semantics, focus, paging, references, and return navigation remain usable while phones expose no high-impact correction controls.
- Given English or French culture, when all audit states and actions render, then resource keys remain in parity and stable selectors do not depend on localized text or color.
- Given no approved audit-performance decision, when repository work completes, then no numeric claim or inferred fallback is recorded and the story finishes `awaiting-operator` with the specified action.

## Spec Change Log

- 2026-09-05 -- Implemented the repository-controlled audit hardening, responsive/accessibility behavior, localized recovery states, and focused regression coverage. Added exact verification evidence below. Status remains `in-progress` until the change is reviewed and committed; the Product/Operations performance action is unchanged.
- 2026-09-05 -- Review pass 1 found that the implementation contract did not make server-side caller-bound cursors, post-await caller revalidation, response-shape/filter/event validation, or typed narrative isolation explicit enough. Amended the Code Map, tasks, acceptance criteria, design notes, and verification targets; the known-bad state to avoid is a caller-replayable cursor or a Ready grid/correction intent built from mismatched, malformed, duplicated, or presentation-reparsed evidence. KEEP: retain the fixed direct REST route, authoritative ordering, localized distinct states and recovery, page-one invalid-cursor recovery, hashed caller binding for retained snapshots, support-safe allowlists, phone read-only behavior, Fluent v5 token cleanup, EN/FR parity, and focused regression style that passed in the superseded attempt.

## Review Triage Log

### 2026-09-05 — Review pass
- verdicts: 38 findings — high 4, medium 20, low 8, false 6, maybe-false 0
- findings:
  - `[high]` `[bad_spec]` Server audit cursors were not caller-bound — `TenantQueryCursorScopes.GetTenantAudit` omitted `QueryEnvelope.UserId`; the server cursor task now requires caller binding and cross-caller replay tests.
  - `[false]` `[reject]` The page passed the raw route tenant ID to its gateway and refresh subscription — functional query/subscription scope must use the literal route identifier, while display and diagnostics are separately sanitized; no logging or rendering path was shown.
  - `[medium]` `[patch]` Identifier safety omitted `access_token`, `authorization`, and broader credential markers — the direct classifier correction is carried into re-derivation and must remain value-silent.
  - `[high]` `[bad_spec]` Audit responses were not validated against requested date/category filters — mismatched rows could disclose out-of-filter evidence; response-contract checks are now explicit.
  - `[medium]` `[bad_spec]` Supported event types were not checked against their authoritative category — a contradictory row could be presented as valid; the complete event/category matrix is now required.
  - `[medium]` `[bad_spec]` Unsafe event references collapsed to duplicate empty DataGrid keys — multiple rows could be conflated and target the wrong receipt/action; unsafe, missing, and duplicate references must now reject the response.
  - `[medium]` `[bad_spec]` Unknown event types became blank Ready rows — the allowlist is now a response-validity boundary rather than display-only blanking.
  - `[low]` `[patch]` Phone-width copy appeared on intrinsically unsupported rows — the direct rendering correction must evaluate intrinsic support before showing viewport suppression.
  - `[low]` `[patch]` Unmeasured viewport evidence used the measured-phone “use wider” message — re-derivation now requires neutral pending evidence distinct from measured phone state.
  - `[false]` `[reject]` Viewport change removed an active correction panel after submission — removing high-impact controls when phone evidence becomes authoritative is required, and no lost command or proof state was demonstrated.
  - `[false]` `[reject]` Phone transition failed to return focus to the correction launcher — that launcher is intentionally absent on phone width, so focusing it would violate the responsive contract.
  - `[medium]` `[patch]` Every filter received `aria-invalid` for a single-field error and none referenced the warning — field-specific invalid state and `aria-describedby` are now direct UI requirements.
  - `[medium]` `[patch]` `datetime-local` values were interpreted as UTC without telling the operator — the direct localization/UI correction must label the absolute UTC contract.
  - `[low]` `[reject]` The spec lacked a live populated-grid browser capture — the isolated authentication/timeout limitation is recorded and the proposed correction is verification/spec evidence; blocking authorized component coverage and supplementary hosted smoke coverage remain required.
  - `[low]` `[reject]` The spec named the wrong conformance test and summarized a combined run without its exact command — this is a spec-only correction, so it is rejected under the review rule; the Code Map and next verification record are corrected separately.
  - `[low]` `[patch]` New French resources omitted idiomatic accents — the direct translation correction is included in re-derivation.
  - `[medium]` `[bad_spec]` Percent-encoded forbidden fragments could bypass return-URL classification — canonicalized unsafe-return rejection and regression tests are now explicit.
  - `[low]` `[patch]` Unmeasured viewport evidence used measured-phone guidance — same verified rendering defect as the ninth blind-hunter finding; the pending-state correction is carried into re-derivation.
  - `[medium]` `[patch]` A queued viewport callback could run after disposal — the outer check does not protect an already queued `InvokeAsync`; add an inner disposed guard.
  - `[low]` `[patch]` Unsupported rows received viewport-unavailable copy — same verified rendering defect as the eighth blind-hunter finding; intrinsic support must be evaluated first.
  - `[medium]` `[bad_spec]` Multiple unsafe references could produce duplicate empty item keys — same verified identity defect as the sixth blind-hunter finding; reject the response before row composition.
  - `[high]` `[bad_spec]` Semicolon-delimited display context was reparsed as correction input — an allow-listed `userId` value could inject role-like segments; typed sanitized narrative is now mandatory for behavior.
  - `[high]` `[bad_spec]` Rows outside requested filters could be accepted — same verified response-boundary defect as the fourth blind-hunter finding; validate each row against the canonical request.
  - `[medium]` `[bad_spec]` A default year-0001 timestamp could be presented as audit evidence — non-default absolute timestamps within requested bounds are now required.
  - `[medium]` `[bad_spec]` Event/category contradictions could be accepted — same verified coherence defect as the fifth blind-hunter finding; enforce the authoritative matrix.
  - `[medium]` `[bad_spec]` An audit response could contain more rows than the normalized page size — page-shape validation is now required before rendering or retention.
  - `[false]` `[reject]` Whitespace date input broadened the query — native empty filter input is intentionally the unfiltered state and cannot emit arbitrary whitespace; malformed non-empty values still fail locally.
  - `[high]` `[bad_spec]` Caller identity was captured only before the awaited read — `ServerCircuitUserContextAccessor.UserId` resolves the current circuit principal on every access, so an identity transition is observable and the old completion must be discarded; post-await revalidation is now explicit.
  - `[medium]` `[patch]` Identifier safety omitted general secret/password/token/credential markers — same verified classifier gap as the third blind-hunter finding; expand the direct classifier coverage without logging rejected text.
  - `[low]` `[reject]` The claims document named the wrong conformance test — same spec-only issue as the fifteenth blind-hunter finding; review policy rejects spec-edit findings.
  - `[medium]` `[patch]` Caller-isolation tests omitted changed-caller 304 refetch and unexpected-exception paths — the verification-gap evidence is trusted; focused tests are now required.
  - `[medium]` `[patch]` Invalid-cursor recovery was not tested after page-two history existed — add assertions that history is cleared, Previous is disabled, and the recovery request has a null cursor.
  - `[medium]` `[patch]` Filter coverage omitted malformed `To` and non-UTC `To` gateway input — add both focused cases to prevent one-sided parser/validation gaps.
  - `[medium]` `[patch]` The supported-event allowlist lacked a discriminating full matrix — add every supported type/category plus a safe-looking unknown type.
  - `[medium]` `[patch]` Typed role narrative mapping lacked valid/invalid role and `UserRoleChanged` page coverage — add focused behavior tests against typed sanitized evidence.
  - `[medium]` `[patch]` Recovery tests asserted only one affordance per state — assert the exact complete action sets and that refresh invokes a request.
  - `[false]` `[reject]` The in-review diff had not yet reached terminal `awaiting-operator` status — review necessarily occurs before commit/finalization; the operator status remains the terminal workflow action.
  - `[false]` `[reject]` The diff did not prove skill invocation, subagent use, commit, or final response — those are workflow events outside the reviewed product diff and were not an implementation divergence.

## Design Notes

Reuse `TenantHighImpactViewportObservation` as the authoritative FrontComposer viewport signal. Unknown/unsafe viewport evidence fails closed for mutation affordances but must not hide read-only audit evidence or recovery; pending measurement gets neutral copy, while measured phone copy appears only where an otherwise supported correction was suppressed. Caller binding belongs both in the server protected-cursor scope and in the retained snapshot/query-gateway seam. Re-read the accessor after each awaited audit response and discard an old-caller completion; do not place raw principal claims in rendered state or diagnostics. Validate the whole response before composing rows. Keep sanitized narrative structured for behavior and format a separate display string so delimiters can never create correction inputs.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release -m:1` -- expected: zero warnings and errors.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -class Hexalith.Tenants.UI.Tests.Components.TenantAuditPageTests -parallelMode none` -- expected: all audit page tests pass.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -class Hexalith.Tenants.UI.Tests.Services.Gateways.TenantQueryGatewayTests -parallelMode none` -- expected: all gateway tests pass.
- `dotnet build Hexalith.Tenants.slnx --configuration Release -m:1` -- expected: solution build passes without weakening gates.
- Browser verification through the running Aspire AppHost -- expected: desktop/tablet/phone, keyboard, localized state/recovery, forced-colors/high-contrast, and reduced-motion evidence is recorded; no performance claim is made before operator approval.

**Superseded attempt results recorded 2026-09-05 (not final verification):**

- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-restore -m:1` -- passed with zero warnings and zero errors.
- Audit-focused test run (`TenantAuditPageTests`, `AuditDataGridCorrectionTests`, `TenantQueryGatewayTests`, and `DomainUiFluentConformanceTests`) -- passed 530/530.
- `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -m:1` -- passed with zero warnings and zero errors.
- Adjacent contract/integration coverage -- `TenantsRestQueryClientTests` passed 118/118, `TenantsProjectionActorTests` passed 143/143, and `TenantsApiGeneratedControllerTests` passed 28/28.
- Aspire/Playwright -- `tenants-ui` was healthy. At 1024x768, 768x1024, and 390x844, verified localized recovery/state output, invalid-range validation and reset, stable read-only navigation, phone removal of correction controls, keyboard focus progression, forced-colors, and reduced-motion. English and French document/state output were exercised. Browser-normalized `datetime-local` seconds and fractional seconds exposed a parser-integration defect during this pass; the accepted exact formats were corrected and covered by tests.
- Authorized browser limit -- the isolated Aspire profile could not authenticate because its randomized proxy URL was not registered as a Keycloak `redirect_uri`. The normal profile authenticated after bypassing the local development certificate warning and reached `/tenants`; navigation to `/tenants/system/audit` did not complete within 30 seconds, so a live populated authorized grid could not be recorded. Authorized grid content, selectors, accessible semantics, caller-bound retention, and viewport-gated correction behavior are covered by the passing component/gateway tests above.
- No numeric audit-performance budget, percentile, or fallback claim was inferred or recorded. The unchanged Product/Operations operator action remains required.
