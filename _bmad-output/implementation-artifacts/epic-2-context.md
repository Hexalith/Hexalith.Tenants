# Epic 2 Context: Safe Tenant Membership Management

<!-- Generated from planning artifacts. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Enable authorized users to add tenant members, change their roles, and remove their access without presenting an accepted command or live notification as completed work. Every mutation must fail closed when its prerequisites are uncertain, reconcile against authoritative projection truth, preserve last-owner safety semantics, and provide honest, supportable recovery and proof.

## Stories

- Story 2.1: Reverify Projection-Confirmed Membership Command Foundation
- Story 2.2: Add User to Tenant with Explicit Role
- Story 2.3: Change Tenant Member Role
- Story 2.4: Remove Tenant Member with Complete Preview and Proof

## Requirements & Constraints

- Membership changes are custom command flows, not generated CRUD or invitation workflows. Adding is direct by literal caller-supplied UserId with an explicit supported role; UserId values remain meaningful strings and are not parsed as GUIDs or ULIDs. `TenantRole.Unknown` is never a valid target.
- Action eligibility combines validation, authoritative freshness, reflected authorization, tenant lifecycle, and required lifecycle/proof capability. Any stale, unknown, missing, or indeterminate prerequisite blocks dispatch and produces a localized inline reason. The API and domain remain the authorization boundary.
- Adding an existing member is a safe `UserAlreadyInTenant` rejection, not a NoOp. Changing to the authoritative current role and removing a member already absent before the attempt are `already applied`, not newly confirmed successes. Unsafe escalation, disabled-tenant, lost-authorization, concurrency, and other domain failures must retain their distinct safe outcomes.
- A removal requires a complete support-safe consequence preview before confirmation. It must cover tenant and target identity, current role, owner-count impact, access removed, freshness, recovery, audit expectation, platform standing, and known consequences versus unknowns. Missing preview input blocks the action. Removing the last tenant owner is permitted with elevated friction; removing tenant membership never changes global-administrator standing.
- Removal completion keeps access confirmation separate from audit proof. Audit may be pending, delayed, unavailable, or available after projection confirmation, and missing proof is never silently upgraded to success. Available proof is limited to support-safe actor, target, tenant, outcome, absolute timestamp, projection marker, and reference data.
- Every failure or uncertain state provides an applicable named recovery such as refresh, wait, retry status lookup, inspect audit, request permission, continue read-only, or escalate. Recovery is forward-only; no event, projection, or read model is edited and no unsupported undo or rollback is promised.
- Completion evidence must cover focused gateway, validation, authorization, freshness, idempotency, locking, lifecycle, projection confirmation, rejection, reconnect, localization, accessibility, responsive, support-safety, conformance, and end-to-end scenarios. English and French whole-string resources must remain in parity. Workflows must be keyboard-operable, safely exitable, usable in forced colors and reduced motion, and unavailable at widths that cannot preserve complete safety context.

## Technical Decisions

- InteractiveServer components use server-side gateways as the only backend egress. Commands use the existing contracts and fixed `POST /api/v1/commands`; no browser backend calls, unversioned aliases, reshaped contracts, or new preview, receipt, proof, or status endpoints are introduced.
- Each deliberate attempt receives a client-generated ULID `messageId`; refresh, reconnect, and retry of that logical attempt reuse its identity. The lock scope is `(interactive circuit, AggregateIdentity)` from submission through terminal evidence. Commands for the same tenant cannot overlap, while unrelated aggregates remain usable; bulk and multi-row command submission are prohibited.
- Shared typed immutable state keeps last-confirmed membership separate from in-flight intent and preserves distinct `submitted`, `accepted`, `projection_pending`, `confirmed`, `audit_pending`, and `audit_available` states. Rejection, duplicate, `already applied`, timeout, degraded, and `unable to verify` retain their canonical meanings and never receive unearned success treatment.
- Status polling and SignalR only trigger authoritative re-query. Confirmation requires the command-specific postcondition in the direct Tenants member projection plus projection-version advancement or safe command-specific audit provenance newer than the pre-submit baseline. A pre-existing matching state, unrelated projection change, acceptance response, or live signal cannot confirm an attempt; absent qualifying provenance yields `unable to verify`.
- The BFF assembles and redacts rejection models, previews, and removal proof before rendering. Raw Problem Details, payloads, tokens, internal correlations, metadata, ETags, protected cursors, stack traces, and PII must not enter component state, rendered output, copy actions, announcements, logs, or telemetry.
- Compose command surfaces from FrontComposer and Fluent UI Blazor V5 primitives. Tenants owns domain-specific truth, safety, and copy; reusable command infrastructure remains platform-owned. Domain strings use Tenants-owned culture-aware `.resx` resources with named placeholders, and stable `data-testid="tenants-{surface}-{element}"` contracts must not depend on localized text, identity values, color, or generated Fluent markup.

## UX & Interaction Patterns

Command lifecycle feedback stays inline and anchored to the affected member row or panel; it never replaces confirmed row data or collapses into toast-only feedback. Status uses icon and text as well as semantic color, with success announced only after projection confirmation. Unavailable actions retain their action-slot footprint and show a plain-language reason without relying on hover.

Removal uses a low-emphasis destructive entry point followed by the complete preview and a focus-trapped confirmation dialog. Cancel and Escape never dispatch, focus returns to the launcher after exit or terminal handling, and last-owner or global-administrator context adds explicit elevated friction without conflating tenant and platform authority. Announcement intent is dedicated: progress is polite, while rejection, failure, destructive blockers, degraded state, and `unable to verify` are assertive.

## Cross-Story Dependencies

Story 2.1 is the shared gateway, idempotency, lifecycle, reconciliation, reconnect, and aggregate-lock foundation for Stories 2.2–2.4. All mutation stories depend on authoritative direct tenant-member reads with usable freshness and projection provenance plus the existing member-table action-availability surface. Story 2.4 additionally depends on the minimum removal-proof capability assembled from the authorized audit read path, but it does not depend on the later general audit and correction epic. Platform-owned read freshness, separated query/command service references, and direct-read prerequisites must remain fail-closed or `unable to verify` until available rather than weakening confirmation rules.
