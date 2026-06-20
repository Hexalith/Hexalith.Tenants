# Deferred Work

Updated: 2026-06-19 by Correct Course approval.
Source proposal: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-19-deferred-work.md`.

This file is now a routing index. Original review detail remains in the source story/spec artifacts; open items here must point to a Tenants story, a FrontComposer owner handoff, an EventStore owner handoff, or a stale/resolved record.

## Tenants-Owned Work Routed to Ready-for-Dev Stories

### `cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening`

Status: `review` after implementation on 2026-06-19.
Story artifact: `_bmad-output/implementation-artifacts/cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening.md`.
Primary source: code review of `3-5-tenant-query-gateway-rest-routing` on 2026-06-07.

Resolution summary:

- Freshness is no longer derived from response `ServedAt`. The implemented direct-read rule treats a real read-model ETag/projection version as `current`; absent markers resolve to `unknown`.
- Generic projection age/version metadata is not available from the current `IReadModelStore` contract. Do not add Tenants-owned generic persistence scaffolding; the remaining threshold-based age metadata need is routed to the EventStore owner handoff below.
- Null/empty read-model ETag behavior is explicit and tested: successful REST reads return 200 with no ETag, no projection-version header, no served-at header, and no 304 support.
- ETag handling is hardened and tested for weak tags, `*`, escaped strong tags, and unsupported multi-tag input.
- REST/handler read-model reconstruction coverage now proves a recreated controller factory can serve the persisted read model from the shared store and honor 304 through the production REST/handler path.
- Live populated-correlation gateway error coverage now asserts that `correlationId`, `reasonCode`, raw payload text, stack traces, tokens, cursors, and ETags do not reach user-facing copy.
- Current full-suite evidence: `Server.Tests` remains blocked by 3 DAPR component expectation tests that still assert removed `enableDeadLetter` / `deadLetterTopic` metadata; `IntegrationTests` passes with DAPR/Aspire/performance skips. The old health-readiness blocker wording is no longer current evidence.

### `cc-2026-06-19-domain-ui-governance-and-accessibility-hardening`

Status: `review` after implementation on 2026-06-19.
Story artifact: `_bmad-output/implementation-artifacts/cc-2026-06-19-domain-ui-governance-and-accessibility-hardening.md`.
Primary sources:

- Code review of `spec-frontcomposer-fluent-structural-and-style-conformance-sweep` on 2026-06-18.
- Code review of `spec-frontcomposer-shell-and-adminui-fluent-conformance-audit` on 2026-06-18.

Resolution summary (2026-06-19):

- Compact non-zero spacing (e.g. `margin:0.5rem`/`padding:0.5rem`) is now flagged by the styling-ownership guard. The `(?!0)` zero-skip was replaced with a zero-token matcher that still skips genuine resets (`0`, `0 0 0 0`, `0px`, `0 !important`). No real component CSS regressed.
- The inline-style guard was widened beyond flex/grid/gap to also cover spacing (margin/padding), sizing (width/inline-size), and alignment (justify-content/align-items), and now scans both quote styles. No `.razor` carries inline `style=`.
- The `<div>`/`<span>` budget now excludes Razor (`@* *@`) and HTML (`<!-- -->`) comments before counting.
- `fc-css-exception` scoping decision: kept RULE-level with documented rationale; a unit test proves a marker exempts only its own rule and does not leak to the next rule.
- `:focus-visible` exemption decision: NARROWED. The blanket exemption was removed; focus-ring affordances (outline/outline-offset/outline-color) are untracked so genuine focus rules still pass, but a `:focus-visible` rule that owns layout/spacing/typography is now flagged unless documented.
- `RemoveForcedColorsMediaBlocks` now skips braces inside CSS comments and quoted strings so a stray brace cannot leak the block tail back into the scan.
- `MemberAccessReview` gained bUnit coverage proving the change-role and remove-member `aria-controls` resolve to a rendered active-region `id` after the FluentStack migration.
- `TenantAuditPage` renders a localized fallback (`Tenants.Audit.UnknownTenant`) for a blank/whitespace `TenantId` instead of a dangling heading.

Still-open sibling candidates (not regressions, future hardening): route pages with `<FcPageLayout>` but no `Mode`, logical longhand `-start`/`-end` spacing that the ownership regex never tracked, pseudo-class root selectors, undocumented new structural tags, and unclosed forced-colors blocks. Single-quoted inline layout `style` was closed as part of AC2.

Dismissed record retained:

- The claim that the styling scan is blind to declarations inside `@media` blocks was verified as a false positive in the structural/style story and is not open work.

### `cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup`

Status: `review` after implementation on 2026-06-20.
Story artifact: `_bmad-output/implementation-artifacts/cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup.md`.
Primary sources:

- Code review of `spec-frontcomposer-fluent-structural-and-style-conformance-sweep` on 2026-06-18.
- Code review of `spec-frontcomposer-shell-and-adminui-fluent-conformance-audit` on 2026-06-18.
- Current deployment docs/YAML scan on 2026-06-19.
- DAPR v1.17 topic-scoping documentation checked on 2026-06-20.

Current resolution summary:

- Production `deploy/dapr/pubsub.yaml` now explicitly scopes `eventstore` to publish `tenants.events` and `deadletter.tenants.events`, denies `sample` publishing, and allows `sample` to subscribe to `tenants.events`.
- Local AppHost pub/sub intentionally omits topic-level scopes while retaining component-level `eventstore` and `sample` scopes; the difference is documented in the component YAML and timing guide.
- `docs/cross-aggregate-timing.md` distinguishes subscriber redelivery on `tenants.events` from EventStore's application-level dead-letter publisher for `deadletter.tenants.events`.
- `CrossAggregateTimingDocumentationTests` guards the production topic-scope contract, local topic-scope omission, application-level dead-letter wording, and the absence of DAPR subscriber-failure-to-dead-letter wording.
- June 18 review-record contradictions are kept as routed, stale/resolved, or future-owner handoff entries instead of open Tenants implementation work.

## Cross-Submodule Owner Handoffs

### FrontComposer owner: `frontcomposer-2026-06-19-page-header-landmarks-and-contract-hardening`

Status: routed to FrontComposer owner; do not patch in Tenants.
Source proposal section: `5.5 FrontComposer Owner Handoff`.

Requested outcomes:

- `FrontComposerShell` exposes a shell content landmark contract that can be a native `<main>` or an equivalent role with an accessible-name parameter.
- Tenants page headings can name the shell main landmark without orphaned page-level `aria-labelledby`.
- `FcPageHeader` no longer creates a competing global `banner` landmark on every route page.
- `FcPageHeader` handles blank `Heading` fail-safely or documents a strict consumer contract with analyzable/tested guardrails.
- `FocusHeadingAsync()` ensures the heading is focusable when used as a focus target or fails diagnostically when `HeadingTabIndex` is omitted.

Related prior audit handoffs:

- FrontComposer H-FC-1: rework or re-justify `FcHomeCard` against pinned `FluentCard` support.
- FrontComposer H-FC-2: consider parity guards for structural/style governance.

### EventStore owner: `eventstore-2026-06-19-admin-ui-and-query-record-followup`

Status: routed to EventStore owner; do not patch in Tenants.
Source proposal section: `5.6 EventStore Owner Handoff`.

Requested outcomes:

- Continue the Admin.UI audit remediation handoffs from `audit-frontcomposer-shell-adminui-fluent-2026-06-18.md`: `Index.razor` non-semantic clickable semantics, clickable-span remediation, `ActivityChart` a11y proof, `StorageTreemap` semantics/docs, and optional parity guards.
- If EventStore tests still encode the retired Tenants actor-routing assumption, update them under EventStore ownership.

### EventStore owner: `eventstore-2026-06-19-read-model-freshness-metadata`

Status: routed to EventStore owner; do not patch in Tenants.
Source story: `cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening`.

Requested outcomes:

- Add or expose shared read-model metadata for persisted projection timestamp/version if D6 threshold-based `aging` and `stale` states need to be computed generically.
- Keep the capability in `Hexalith.EventStore` (`IReadModelStore` / query metadata path) rather than adding Tenants-specific persistence scaffolding.
- Once available, Tenants can map real persisted projection age/version through configurable thresholds; until then Tenants uses the direct-read ETag/version `current` rule and fails unmarked responses closed to `unknown`.

## Stale or Resolved Records

### EventStore Admin retired actor-routing entry

Status: stale/resolved as of 2026-06-19; re-verified on 2026-06-20.

Previous record said `Hexalith.EventStore/src/Hexalith.EventStore.Admin.Server/Services/DaprTenantQueryService.cs` still assigned `ProjectionActorType: TenantProjectionRouting.ActorTypeName`.

Verification command:

```bash
rg -n "ProjectionActorType|TenantProjectionRouting|TenantsProjectionActor" Hexalith.EventStore/src/Hexalith.EventStore.Admin.Server/Services/DaprTenantQueryService.cs
```

Result on 2026-06-19 and 2026-06-20: no matches. Do not carry this as open Tenants work.

### Inert DAPR component dead-letter metadata

Status: resolved on 2026-06-18; only the timing-diagram wording remains open under `cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup`.

The misleading Redis pub/sub component keys `enableDeadLetter` and `deadLetterTopic` were removed from local and production component files. EventStore's application-level dead-letter publisher remains the documented mechanism.

### Per-commit history and commit-scope hygiene

Status: not scheduled as implementation work.

Records about intermediate non-building commits, co-mingled story diffs, and bundled DAPR/health changes are history hygiene notes. The current approved path is to keep completed story states intact and create focused follow-up stories for real runtime, governance, deployment, and documentation work.

## Deferred from: code review of cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening (2026-06-19)

- ETag special-character (quote/comma) robustness — latent, non-exploitable. `NormalizeETagToken`/`Trim('"')` unquote any value that starts and ends with `"` (asymmetric vs raw store tokens) in `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs:87-107`; the client and server both reject commas with a substring check, dropping a single quoted strong tag whose content legitimately contains a comma (`src/Hexalith.Tenants.UI/Services/Gateways/TenantsQueryApiClient.cs:25-29`); and client/server normalization disagree on quoted-whitespace/`"*"` edge inputs. These do not bite while DAPR/Redis read-model ETags remain opaque numeric strings without quotes or commas, and the emit→submit→compare round-trip is internally symmetric. Revisit if the EventStore read-model store contract ever emits special-character ETags (ties into the `eventstore-2026-06-19-read-model-freshness-metadata` handoff above).

## Deferred from: code review of cc-2026-06-19-domain-ui-governance-and-accessibility-hardening (2026-06-19)

- CSS ownership guard logical longhand spacing — `DomainUiFluentConformanceTests` still does not catch `margin-inline-start`, `margin-inline-end`, `padding-block-start`, or `padding-block-end`. This was already recorded as a still-open sibling candidate, not a regression from this story.
- Forced-colors malformed block handling — `RemoveForcedColorsMediaBlocks` now ignores braces inside comments and strings, but an unclosed forced-colors block can still remove the rest of a CSS file from the scan. Keep as a future hardening candidate.
- Sibling query ETag special-character robustness — quote/comma ETag edge cases surfaced again because the working-tree diff includes the completed tenant-query hardening story. Keep routed under the tenant-query review / EventStore read-model freshness handoff; it is outside the domain UI governance story.
