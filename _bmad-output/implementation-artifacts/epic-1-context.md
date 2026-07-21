# Epic 1 Context: Trustworthy Tenant Discovery and Access Review

<!-- Generated from planning artifacts. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Deliver the complete read-only tenant-management experience so operators, owners, and members can find only the tenants they are authorized to see, judge how trustworthy the displayed data is, inspect tenant details, configuration, memberships, and global-administrator access, and understand why an action is unavailable. This matters because access questions must be answerable without leaking hidden data or presenting stale, indexed, or merely requested state as authoritative truth.

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

- The read-only MVP must cover tenant list triage, deep-linkable tenant overview, self-audit through My Tenants, operator-backed user membership lookup, authorized configuration namespaces, member/access review, and authorized fixed-scope global-administrator review. Users lookup is not an exhaustive user directory, and global-administrator authority must remain distinct from tenant membership.
- Lists use opaque cursor pagination, deterministic sorting, exact status filtering, and authorization-safe empty/error handling. Loading, empty, filtered-empty, error, stale, and degraded states remain distinct; invalid or stale cursor state restarts at page 1 with an honest localized notice.
- Freshness claims require authoritative read-model provenance. Missing or invalid provenance renders as `unknown`; request time, `ServedAt`, successful HTTP status, cached age, Memories data, command status, and SignalR notifications are not proof of freshness. Safety-sensitive availability fails closed when freshness or authorization is indeterminate.
- Authorization is enforced by APIs and the domain. The UI only reflects it, and must not reveal hidden tenants, memberships, configuration namespaces, administrator identities, counts, or existence through routes, state copy, errors, or empty results.
- Sensitive configuration values and unsafe internals must never reach rendered component state, DOM, announcements, clipboard, logs, or telemetry. Only explicitly support-safe identifiers may be copied, preserving the complete literal caller-supplied value.
- Target roughly one-second interactive rendering for typical warm tenant list/detail/member reads, while preserving safety and freshness behavior. Every surface requires focused authorization, support-safety, localization, responsive, accessibility, gateway, and conformance evidence with stable selectors that do not depend on text, color, row data, or incidental Fluent markup.

## Technical Decisions

- Compose a .NET 10 Blazor InteractiveServer application through FrontComposer and Fluent UI Blazor V5. Tenants owns domain surfaces, domain copy, server-side composition, and tenant-specific safety behavior; reusable shell, layout, grid, theme, and command infrastructure belongs to FrontComposer.
- Browser components use injected server-side BFF contracts only. Tenant reads call the direct Tenants REST read surface with server-side bearer relay and service discovery; they must not use the generic EventStore query route or a browser-side client. Query and command service references remain separate.
- Preserve supported ETag, projection-version, and read-model freshness metadata inside the BFF without exposing those internals. Use the shared EventStore freshness model; retain last-confirmed data separately from refresh intent, and treat a metadata-deficient `304` as insufficient proof of recovery.
- Treat TenantId and UserId as case-sensitive, caller-supplied strings. Do not impose GUID, ULID, email, or invitation semantics; safely encode route values and copy authorized values literally.
- Whole-set tenant search uses Memories only to obtain ordered tenant-id candidates. The BFF drops malformed and duplicate candidates, authorization-filters and hydrates survivors through authoritative Tenants reads, and never renders indexed content as row truth. Search cursors are protected and bound to user, normalized query, status, sort, direction, and page size; search failure degrades to the normal cursor list.
- The UI host is domain-owned, but orchestration, shared hosting, health, telemetry, secrets, DataProtection, and production scaling are platform responsibilities. Do not expand transitional repository orchestration or implement shared-platform gaps inside Tenants. Multi-replica InteractiveServer remains unapproved until shared key protection, session routing, and cursor durability are verified.

## UX & Interaction Patterns

- Register exactly one Tenants shell entry at `/tenants`. Use page-local Tenants and Users tabs with canonical workspace state for tab, scope, lookup, filters, sorting, and cursor; changes reset the cursor. Tenant detail and global-administrator review are contextual routes with safe return state that restores filters, selection, and scroll where valid.
- Use full-width operational grids with stable row/action footprints. Identity, status, freshness, role, and risk context must remain available at every width through pinning or horizontal overflow, never by dropping safety-critical columns. Mobile is a safe read-only experience; tablet navigation may collapse and regions may stack.
- Status meaning uses localized text plus verified Fluent icon/semantic roles, never color alone. Pending or unknown state must not look or sound successful. Tables expose headers, sort state, and row relationships; timestamps are absolute; focus is visible and logical; copy feedback uses polite announcements and retains focus.
- Tenants-owned domain text uses parity-checked English/French whole-string resources with culture-aware formatting and no runtime fragment assembly. Unavailable actions show one canonical inline, programmatically associated reason rather than a tooltip-only explanation.

## Cross-Story Dependencies

- Story 1.0 establishes the verified FrontComposer/Fluent contracts and conservative fallback boundaries used by every later UI story. Story 1.1 establishes the host, BFF trust boundary, canonical workspace, routing, and localization foundation.
- Story 1.2 supplies the reusable tenant-list state and cursor behavior used by tenant detail return, My Tenants, and search. Stories 1.3 through 1.8 build the shared read surfaces and evidence baseline; Story 1.9 extends the list through protected Memories search.
- Story 1.10 is the explicit transport-and-provenance correction for reads already introduced by Stories 1.2 through 1.9, not a silently completed prerequisite. Until direct freshness metadata and separate query/command host references are available, affected surfaces must report `unknown` or fail closed. Story 1.11 depends on that direct-read/freshness posture for fixed-scope global-administrator review.
- Whole-set search depends on Memories index ingestion/filtering and protected scoped paging. Authoritative freshness depends on platform read metadata and composing-host service separation. These are external gates; their absence must produce the documented conservative behavior, not Tenants-owned replacement infrastructure.
