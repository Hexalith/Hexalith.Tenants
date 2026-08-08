# Epic 3 Context: Tenant Onboarding, Lifecycle, and Configuration

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Deliver complete tenant onboarding and ongoing metadata, lifecycle, and namespaced configuration control through the shared projection-confirmed command posture. Authorized users must create tenants, maintain records, and safely enable, disable, or configure them without false success, incomplete previews, or silent same-state shortcuts. Hard tenant deletion and broader audit generalization are outside this epic.

## Stories

- Story 3.1: Create Tenant with Projection Confirmation
- Story 3.2: Edit Tenant Metadata with Recorded Updates
- Story 3.3: Lifecycle and Configuration Availability Guardrail
- Story 3.4: Disable or Enable Tenant with Complete Preview
- Story 3.5: Set Namespaced Configuration with Complete Preview
- Story 3.6: Remove Configuration Key with Complete Preview

## Requirements & Constraints

- Create is global-administrator-only. The caller-supplied tenant id is a literal case-sensitive string: never generated, trimmed, normalized, slugified, or parsed as a GUID or ULID. Collision is a safe `TenantAlreadyExists` rejection, never success or `already applied`.
- Metadata edit is available to tenant contributors and global administrators. Successful edits always record an update; identical submitted values still expect a recorded update and never become a NoOp or unchanged-state rejection.
- Disable/enable is global-administrator-only and reversible availability control, not hard deletion. Same-state requests are rejected as `TenantLifecycleStateAlreadySet`. Disabled status is an eventually-consistent availability signal; confirmed-disabled tenants reject tenant-scoped mutations as `TenantDisabled`, while enable remains the recovery path after high-impact gates pass.
- Configuration is namespaced by authorized prefix. Every eligible set/remove in v1 requires a complete consequence preview; there is no low-risk-key bypass. Identical set key+value is `already applied`; missing remove targets are safe `ConfigurationKeyNotFound` rejections, not NoOps. Full-key length max 256 and value length max 1024; over-limit results map to safe `ConfigurationLimitExceeded` guidance. Raw or sensitive values must never appear in preview, lifecycle, audit handoff, recovery, announcements, logs, or copied feedback.
- Action availability fails closed when authorization, freshness, lifecycle support, aggregate admission, preview completeness, proof prerequisites, viewport safety, namespace scope, or other readiness inputs are stale, missing, unknown, or indeterminate. Use the canonical unavailable reasons: `missing permission`, `stale data`, `missing lifecycle support`, `missing consequence preview`, `missing audit proof`, and `high-impact flow not ready`. Server/API/domain authorization remains the enforcement boundary.
- Success requires authoritative projection reconciliation: expected postcondition plus projection-version advancement or safe command-specific audit provenance beyond the pre-submit baseline. Status completion, SignalR, submitted intent, optimistic UI, unrelated projection changes, or audit availability alone cannot confirm. Missing qualifying provenance is `unable to verify`.
- First-tenant bootstrap: when the authorization-scoped tenant list is authoritatively empty and freshness is `unknown` only because no first write timestamp exists, create may remain available for an otherwise eligible global administrator; `TenantAlreadyExists` remains the collision backstop. Non-empty, ambiguous, unauthorized, or unproven lists do not get this exception.
- Support-safe surfaces never expose Problem Details, payloads, tokens, correlations, ETags, cursors, stack traces, or PII. English and French Tenants-owned whole-string resources stay parity-checked with named placeholders. Workflows must be keyboard-operable, safely exitable, usable under forced colors and reduced motion, and unavailable at widths that cannot preserve complete safety context.

## Technical Decisions

- InteractiveServer components egress only through server-side gateways. Mutations dispatch existing contracts on fixed `POST /api/v1/commands` with a client-generated ULID attempt/`messageId` as the idempotency key. No browser backend calls, invitation/bootstrap aliases, reshaped contracts, or new preview/receipt/status endpoints.
- Commands for this epic: `CreateTenant(TenantId, Name, Description)`, `UpdateTenant(TenantId, Name, Description)`, `DisableTenant(TenantId)`, `EnableTenant(TenantId)`, `SetTenantConfiguration(TenantId, Key, Value)`, and `RemoveTenantConfiguration(TenantId, Key)`.
- Lock scope is `(interactive circuit, AggregateIdentity)` from submit through terminal evidence. Same-tenant metadata, membership, lifecycle, and configuration commands cannot overlap; unrelated aggregates may proceed. Bulk create, concurrent same-tenant commands, and toast batching remain prohibited. Refresh, reconnect, and retry of one logical attempt reuse its identity without double-dispatch.
- Shared typed immutable state keeps last-confirmed projection separate from in-flight intent and preserves distinct lifecycle states including submitted, accepted, projection-pending, confirmed, already-applied, rejected, unable-to-verify, and audit handoff states where applicable. Status polling and SignalR only nudge authoritative re-query.
- The BFF assembles and redacts consequence previews and rejection view models from existing read-model fields before render. Story 3.3 evaluates eligibility only and never submits lifecycle or configuration commands.

## UX & Interaction Patterns

- These are custom Fluent command flows, not generated CRUD. Lifecycle feedback stays inline and anchored to the affected form, detail, or configuration surface; it never overwrites last-confirmed data or collapses into toast-only success.
- High-impact enable, disable, set, and remove open only after Story 3.3 reports eligibility, then present a complete support-safe ten-item consequence preview. Lifecycle previews cover identity, requested and current lifecycle state, membership/operational impact, freshness, recovery, audit expectation, caller/platform scope, and known consequences versus unknowns. Configuration previews cover identity, authorized namespace/prefix, key, current known state, intended effect, freshness, recovery, audit expectation, authorization/scope evidence, and known consequences versus unknowns. Any missing item blocks confirmation and names the gap.
- Disable/enable uses elevated typed confirmation (exact tenant identity or approved operation phrase), focus trapping, and non-committing Escape/cancel. Lifecycle and configuration mutations stay unavailable on mobile or other unsafe narrow layouts that cannot preserve full safety context. Unavailable actions keep a visible hover-independent reason and named recovery. Success is announced only after qualified projection confirmation.

## Cross-Story Dependencies

- Stories 3.1 and 3.2 reuse the shared command gateway, aggregate lock, reconnect, and provenance-qualified confirmation posture established for earlier membership command work; they depend on Tenants workspace create entry and eligible tenant-detail metadata surfaces from the discovery epic.
- Story 3.3 is the fail-closed availability gate for Stories 3.4–3.6 and performs no mutation itself. Stories 3.4–3.6 depend on authoritative tenant detail and configuration reads with usable freshness, plus complete BFF-assembled preview inputs.
- Audit browsing and forward correction remain a later epic; this epic may hand off to existing authorized audit paths when evidence becomes available but must not invent receipts, proof endpoints, or completed audit state. Hard destructive tenant deletion remains future administrators-only tooling outside this UI scope.
