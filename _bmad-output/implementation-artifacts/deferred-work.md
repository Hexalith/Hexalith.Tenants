# Deferred Work

Updated: 2026-06-19 by Correct Course approval.
Source proposal: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-19-deferred-work.md`.

This file is now a routing index. Original review detail remains in the source story/spec artifacts; open items here must point to a Tenants story, a FrontComposer owner handoff, an EventStore owner handoff, or a stale/resolved record.

## Tenants-Owned Work Routed to Ready-for-Dev Stories

### `cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening`

Status: `ready-for-dev`.
Story artifact: `_bmad-output/implementation-artifacts/cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening.md`.
Primary source: code review of `3-5-tenant-query-gateway-rest-routing` on 2026-06-07.

Routed items:

- Freshness ladder reports `current` on every successful read. Follow-up is to make D6 freshness truthful from real projection metadata or explicitly document the direct-read/unknown rule.
- Full `Server.Tests` and `IntegrationTests` blockers must be re-baselined with current evidence; old pub/sub and health-readiness blocker wording must not be carried if resolved.
- Null/empty read-model ETag behavior must be explicit and tested: 200 with no ETag and no 304 support.
- ETag quoting, weak ETag, `*`, escaped strong tag, and unsupported multi-tag handling must be hardened or safely mapped.
- The deleted actor-based state-store reconstruction test must be replaced with REST/handler production-path reconstruction coverage.
- A live populated-correlation gateway error path must assert that `correlationId`, `reasonCode`, raw payloads, stack traces, tokens, cursors, and ETags do not reach user-facing copy.

### `cc-2026-06-19-domain-ui-governance-and-accessibility-hardening`

Status: `ready-for-dev`.
Story artifact: `_bmad-output/implementation-artifacts/cc-2026-06-19-domain-ui-governance-and-accessibility-hardening.md`.
Primary sources:

- Code review of `spec-frontcomposer-fluent-structural-and-style-conformance-sweep` on 2026-06-18.
- Code review of `spec-frontcomposer-shell-and-adminui-fluent-conformance-audit` on 2026-06-18.

Routed items:

- Section 5.3 governance guard bypasses that were explicitly deferred by Administrator: compact non-zero spacing such as `padding:0.5rem`, broader inline layout/spacing/sizing declarations, comment-counting in the `<div>`/`<span>` budget, `fc-css-exception` scoping, `:focus-visible` exemption breadth, and `RemoveForcedColorsMediaBlocks` brace robustness.
- Sibling structural-governance hardening candidates: route pages with `<FcPageLayout>` but no `Mode`, single-quoted inline layout `style`, fractional non-zero spacing beginning with `0`, pseudo-class root selectors, undocumented new structural tags, and unclosed forced-colors blocks.
- Blank/whitespace `TenantId` on the audit route renders a dangling `Audit - ` heading. Cosmetic only; route through localized fallback if implemented.
- `MemberAccessReview` has button-side `aria-controls` coverage, but should also assert the active target region `id` is rendered after the FluentStack migration.

Dismissed record retained:

- The claim that the styling scan is blind to declarations inside `@media` blocks was verified as a false positive in the structural/style story and is not open work.

### `cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup`

Status: `ready-for-dev`.
Story artifact: `_bmad-output/implementation-artifacts/cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup.md`.
Primary sources:

- Code review of `spec-frontcomposer-fluent-structural-and-style-conformance-sweep` on 2026-06-18.
- Code review of `spec-frontcomposer-shell-and-adminui-fluent-conformance-audit` on 2026-06-18.
- Current deployment docs/YAML scan on 2026-06-19.

Routed items:

- `deploy/dapr/pubsub.yaml` contains `publishingScopes: "sample="` while the component comment says EventStore publishes and sample subscribes. Verify against DAPR topic-scoping syntax and correct or document the intended mapping.
- Local AppHost and production DAPR pub/sub component scope policy must be compared and documented if intentionally different.
- `docs/cross-aggregate-timing.md` still diagrams subscriber failure flowing to `deadletter.tenants.events` after retry/dead-letter policy. The prose correctly credits EventStore's application-level dead-letter publisher; the diagram should stop implying DAPR component dead-lettering.
- `CrossAggregateTimingDocumentationTests` should assert the truthful application-level dead-letter wording and the pub/sub scope contract after YAML/docs changes.
- June 18 review-record contradictions and old DAPR/health bundling text should stay normalized to current facts.

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

## Stale or Resolved Records

### EventStore Admin retired actor-routing entry

Status: stale/resolved as of 2026-06-19.

Previous record said `Hexalith.EventStore/src/Hexalith.EventStore.Admin.Server/Services/DaprTenantQueryService.cs` still assigned `ProjectionActorType: TenantProjectionRouting.ActorTypeName`.

Verification command:

```bash
rg -n "ProjectionActorType|TenantProjectionRouting|TenantsProjectionActor" Hexalith.EventStore/src/Hexalith.EventStore.Admin.Server/Services/DaprTenantQueryService.cs
```

Result on 2026-06-19: no matches. Do not carry this as open Tenants work.

### Inert DAPR component dead-letter metadata

Status: resolved on 2026-06-18; only the timing-diagram wording remains open under `cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup`.

The misleading Redis pub/sub component keys `enableDeadLetter` and `deadLetterTopic` were removed from local and production component files. EventStore's application-level dead-letter publisher remains the documented mechanism.

### Per-commit history and commit-scope hygiene

Status: not scheduled as implementation work.

Records about intermediate non-building commits, co-mingled story diffs, and bundled DAPR/health changes are history hygiene notes. The current approved path is to keep completed story states intact and create focused follow-up stories for real runtime, governance, deployment, and documentation work.
