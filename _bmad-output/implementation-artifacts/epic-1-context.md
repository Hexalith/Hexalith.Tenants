# Epic 1 Context: Trustworthy Tenant Discovery and Access Review

<!-- Generated from planning artifacts. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Deliver the complete read-only tenant-management experience so operators, owners, and members can find only authorized tenants, judge data trustworthiness, inspect tenant details, configuration, memberships, and global-administrator access, and understand unavailable actions without leaking hidden data or presenting indexed, stale, or merely requested state as authoritative truth.

## Stories

- Story 1.0: Reverify FrontComposer Shell and Fluent Contracts
- Story 1.1: Reverify UI Host Bootstrap and Canonical Workspace
- Story 1.2: Tenant List Triage and Cursor Foundation
- Story 1.3: Tenant Detail Navigation and Overview
- Story 1.4: My Tenants Self-Audit
- Story 1.5: User Membership Lookup
- Story 1.6: Read-Only Tenant Configuration
- Story 1.7: Tenant Member Table and Action Availability
- Story 1.8: Support-Safe Identifier Copy and Read-Experience Evidence
- Story 1.9: Authoritative Memories Search with Protected Paging
- Story 1.10: Direct Tenants Reads and Authoritative Freshness
- Story 1.11: Authorized Global Administrator Review

## Requirements & Constraints

- Cover tenant triage, deep-linked overview, My Tenants, user-membership lookup, authorized configuration, member/access review, and global-administrator review. User lookup is not an exhaustive directory; platform authority remains distinct from tenant membership.
- Lists use opaque cursors, deterministic sorting, exact status filtering, and authorization-safe results. `loading`, `empty`, `filtered-empty`, `error`, `stale`, and `degraded` remain distinct; invalid or stale cursor state restarts at page 1 with an honest localized notice; search failure must not block the normal list.
- Freshness requires authoritative read-model provenance. Missing provenance is `unknown`; HTTP success, request/cache time, indexed data, command status, and live notifications are not proof. Safety-sensitive availability fails closed when freshness or authorization is indeterminate.
- APIs and domain logic enforce authorization; the UI only reflects it. Routes, errors, empty states, and component state must not reveal hidden tenants, memberships, configuration namespaces, administrator identities, counts, or existence.
- Sensitive configuration values and unsafe internals must never reach rendered component state, DOM, announcements, clipboard, logs, or telemetry. Only explicitly support-safe identifiers may be copied, preserving the caller-supplied literal. Tokens, protected cursors, ETags, payloads, internal correlations, raw metadata, stack traces, and PII remain absent from every output channel.
- Typical warm list/detail/member reads target roughly one-second interaction. Each surface needs focused authorization, support-safety, localization, responsive, accessibility, and conformance evidence with selectors independent of text, color, row data, or generated markup.

## Technical Decisions

- Use the domain-owned .NET 10 Blazor InteractiveServer host through FrontComposer and Fluent UI Blazor V5. Tenants owns domain behavior and copy; reusable UI infrastructure remains platform-owned or uses an approved fallback.
- Components use server-side BFF contracts only. Six reads go directly to Tenants REST; commands and status lookup remain on EventStore. Query and command service references remain separate, each failing closed on its own side without falling back to the other. Do not use the generic query route or add backend endpoints.
- Preserve ETag, projection-version, and freshness metadata inside the BFF. Use the shared EventStore freshness model, separate last-confirmed data from refresh intent, and reject metadata-deficient responses as proof. SignalR only triggers re-query.
- Treat TenantId and UserId as case-sensitive caller-supplied strings, not GUIDs, ULIDs, or emails. Encode routes safely and copy authorized values literally.
- Memories supplies ordered tenant-id candidates only. The BFF deduplicates, authorization-filters, and hydrates them through authoritative reads; indexed content is never row truth. Bind protected cursors to user and search scope; invalidation restarts at page 1 honestly.
- The UI owns no datastore. Orchestration, health, telemetry, secrets, DataProtection, and production scaling remain platform responsibilities; do not expand transitional repository orchestration. Multi-replica InteractiveServer remains unapproved until shared key protection, session routing, and cursor durability are verified.

## UX & Interaction Patterns

- Register one `/tenants` shell entry. Use page-local Tenants and Users tabs with canonical scope, lookup, search, filter, sort, and cursor state; scope changes reset paging. Contextual routes restore valid list context on return.
- Keep identity, status, freshness, role, and risk available through pinned columns or horizontal overflow. Tablet may collapse/stack; mobile is read-only.
- Pair localized status text with verified Fluent icon and semantic role, never color alone. Distinguish pending, unknown, and proven states; provide table semantics, absolute times, visible focus, keyboard order, and polite copy feedback.
- Use parity-checked English/French whole strings. Unavailable actions show an inline, programmatically associated canonical reason; a tooltip alone is insufficient.

## Cross-Story Dependencies

- Stories 1.0–1.1 establish the reverified UI boundary, host, BFF, workspace, state, and localization foundations. Historical completion is evidence to reverify, not a readiness waiver.
- Story 1.2 supplies shared list/cursor behavior; Stories 1.3–1.8 complete the core reads and evidence; Story 1.9 adds protected Memories search.
- Story 1.10 corrects transport and freshness for earlier reads. Until platform metadata, split host references, direct routing, protected paging, and index support are verified, surfaces report `unknown`, fail closed, or use the normal-list fallback. Story 1.11 depends on this posture and underpins later global-administrator control.
