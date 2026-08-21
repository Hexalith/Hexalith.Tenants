# Epic 3 Context: Tenant Onboarding, Lifecycle, and Configuration

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Deliver tenant onboarding and ongoing metadata, lifecycle, and namespaced configuration control through projection-confirmed commands. Authorized users can create and maintain tenants and deliberately enable, disable, or configure them without false success, incomplete safety context, or sensitive-data exposure. Hard tenant deletion and generalized audit/recovery are outside this epic.

## Stories

- Story 3.1: Create Tenant with Projection Confirmation
- Story 3.2: Edit Tenant Metadata with Recorded Updates
- Story 3.3: Lifecycle and Configuration Availability Guardrail
- Story 3.4: Disable or Enable Tenant with Complete Preview
- Story 3.5: Set Namespaced Configuration with Complete Preview
- Story 3.6: Remove Configuration Key with Complete Preview

## Requirements & Constraints

- Creation and lifecycle changes are global-administrator-only; metadata edits also allow tenant contributors. Tenant ids remain literal case-sensitive strings, never normalized or parsed as GUIDs/ULIDs. Creation collision is `TenantAlreadyExists`; metadata updates always emit `TenantUpdated`, even for identical values.
- Disable/enable is reversible availability control, not deletion. Same-state requests reject as `TenantLifecycleStateAlreadySet`. Disabled is eventually consistent; confirmed-disabled tenants reject tenant-scoped mutations as `TenantDisabled`, while enable remains the recovery operation.
- Configuration is limited to server-reflected authorized namespaces. Every set/remove requires a complete preview in v1. Identical set key/value is `already applied`; a missing removal target is `ConfigurationKeyNotFound`; full-key and value maxima are 256 and 1024 characters, with excess mapped to `ConfigurationLimitExceeded`. Sensitive values must not leak outside input or into rendered/persisted feedback.
- Validation, authoritative freshness, authorization, lifecycle support, aggregate admission, preview completeness, namespace scope, and viewport safety must be eligible before high-impact submission. Missing or indeterminate inputs fail the affected action closed with a canonical inline reason and named recovery; server/API/domain authorization remains authoritative.
- Confirmation requires the expected authoritative postcondition plus projection-version advancement or safe command-specific audit provenance beyond the baseline. Status, SignalR, intent, optimistic state, pre-existing state, unrelated projection movement, or audit availability alone cannot confirm; insufficient evidence is `unable to verify`.
- Meet WCAG 2.1 AA, keyboard complete-or-exit, no-color-only, forced-colors/reduced-motion, English/French whole-string localization, stable-selector, and support-safety requirements. Never expose raw Problem Details, payloads, tokens, correlations, ETags, cursors, metadata, stack traces, or PII.

## Technical Decisions

- Compose the existing InteractiveServer UI with FrontComposer and Fluent UI Blazor V5. Browser components use only server-side query/command gateways. Existing contracts dispatch on fixed `POST /api/v1/commands`; each logical attempt has a ULID `messageId`. Add no preview, receipt, status, or lifecycle endpoint.
- Reconciliation reads go directly to Tenants REST with ETag, projection-version, and freshness metadata; commands/status remain on the EventStore client. Freshness-dependent readiness requires verified metadata propagation, split query/command service references, and removal of the generic EventStore query route.
- Shared typed immutable state separates last-confirmed data from intent and preserves canonical lifecycle, freshness, unavailable-reason, and audit vocabularies. Status polling and SignalR only nudge authoritative re-query.
- Lock one command per `(interactive circuit, AggregateIdentity)` through terminal evidence. Same-tenant metadata, membership, lifecycle, and configuration commands cannot overlap; unrelated aggregates may proceed. Refresh/reconnect reuses attempt identity without double dispatch.
- The BFF assembles and redacts previews/rejections from authorized read data. Use the approved structured inline-text consequence-preview fallback and canonical Tenants vocabulary with verified Fluent semantic/icon mappings until shared components exist.

## UX & Interaction Patterns

- Use custom Fluent command flows, not generated CRUD. Keep lifecycle feedback inline; never overwrite confirmed data or collapse into toast-only success. In-flight states are informative, while Success styling/copy/announcement is projection-proven only; every status uses text, icon, and semantic role.
- Lifecycle/configuration previews cover identity/scope, current and intended state, impact, freshness, recovery, audit expectation, authorization evidence, and known consequences versus unknowns. Missing content blocks confirmation. Disable/enable adds typed confirmation; focus is trapped, Escape/cancel dispatches nothing, and focus returns to the launcher.
- High-impact mutation remains unavailable with a visible reason on mobile or any width that cannot preserve complete safety context. Unavailable reasons are never hover-only.

## Cross-Story Dependencies

- Stories 3.1–3.2 reuse the earlier shared command/read foundation. Onboarding crosses epics: create, add the first owner through membership, then configure, confirming each step before continuing.
- Story 3.3 is the non-mutating availability gate for Stories 3.4–3.6; they depend on authoritative tenant/configuration reads, usable freshness, reflected authorization/scope, aggregate admission, and complete BFF preview inputs.
- Audit browsing and forward correction belong to the later audit/recovery epic. Audit handoff must not invent receipts, proof endpoints, or completed proof and is not required for truthful projection confirmation.
