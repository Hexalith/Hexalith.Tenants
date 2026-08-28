# Epic 4 Context: Global Administrator Control

<!-- Generated from planning artifacts. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Enable authorized operators to grant and remove platform-wide global-administrator authority while isolating governance from tenant membership, preserving the fixed aggregate scope, and preventing removal of the final administrator. Actions fail closed when authority, freshness, count completeness, command support, or safety context is indeterminate; no change is successful before authoritative projection confirmation.

## Stories

- Story 4.1: Fixed-Scope Global Administrator Action Availability
- Story 4.2: Grant Global Administrator with Projection Confirmation
- Story 4.3: Remove Global Administrator with Last-Administrator Hard Stop

## Requirements & Constraints

- Only authorized global administrators may use mutations. API and domain authorization remain the enforcement boundary; the UI reflects server-derived authority and leaks no administrator identity, count, route, or existence signal to unauthorized callers.
- Evaluate grant and removal independently using authoritative platform authority, direct-read freshness/provenance, target visibility, complete count, lifecycle support, preview readiness, aggregate admission, and viewport safety. Unknown, stale, degraded, incomplete, or unsupported inputs block the affected action with a visible canonical reason and recovery.
- Last-administrator protection is a hard invariant. Removal requires authoritative proof of a complete count greater than one; partial or paged counts are insufficient. The final removal is unavailable before preview and domain-rejected as `LastGlobalAdministrator` if a race occurs—never an override, warning-only flow, NoOp, or completable friction.
- These are high-impact actions requiring a complete, support-safe preview of fixed scope, target, count impact, authority changed, freshness, recovery, audit expectation, caller/target context, and known consequences versus unknowns. Missing content blocks submission. Confirmation is deliberate; bulk or concurrent governance mutations are prohibited.
- Treat UserIds as literal, case-sensitive caller input: require non-whitespace but never generate, normalize, or parse them as GUIDs or ULIDs. Existing grant targets and absent removal targets are safe rejections, not success or `already applied`.
- Keep submitted, accepted, projection pending, confirmed, rejected, audit availability, missing support, and unable-to-verify states distinct. Reconnects and retries preserve last-confirmed rows and attempt identity without double dispatch or optimistic mutation.
- Use Tenants-owned whole-string EN/FR resources. Rendered or observable data excludes tokens, claims, payloads, raw Problem Details, correlations, ETags, cursors, metadata, stack traces, and PII. Stable selectors cannot depend on row text, color, or generated Fluent markup.
- Completion evidence covers routing, authorization isolation, freshness/incomplete-count refusal, last-administrator pre-block and race rejection, complete preview, aggregate locking, qualified confirmation, reconnect/idempotency, localization, accessibility, responsive fail-closed behavior, support safety, and end-to-end outcomes.

## Technical Decisions

- Reads use direct `GET /api/global-administrators` through the server-side BFF and preserve ETag, projection version, authorization, and `ReadModelFreshnessState`. `ServedAt`, client time, tenant freshness, command status, and SignalR are not proof.
- Commands use `POST /api/v1/commands` with tenant `system`, domain and aggregate id `global-administrators`. Dispatch `SetGlobalAdministrator(UserId)` or `RemoveGlobalAdministrator(UserId)` with a client-generated ULID `messageId`. Components never call backends directly, and no new endpoint is added.
- Lock one command per `(interactive circuit, AggregateIdentity)` through terminal evidence. Since all mutations share the fixed aggregate, one grant/removal locks all governance mutations while unrelated tenant aggregates may proceed.
- Confirmation comes from authoritative re-query: grant requires target presence; removal requires absence after a baseline proving presence. The postcondition also needs projection-version advancement or safe command-specific audit provenance. Acceptance, pre-existing state, unrelated changes, and optimistic intent never confirm.
- Shared immutable truth, freshness, lifecycle, audit, and authorization state keeps last-confirmed projection separate from intent. The BFF owns authorization reflection, preview assembly, rejection mapping, and redaction.

## UX & Interaction Patterns

Global Administrators is a contextual, policy-gated surface within the single Tenants FrontComposer module, not a separate shell entry or tenant-membership view. Compose with FrontComposer and Fluent UI Blazor V5 first. Unavailable actions retain a visible, hover-independent, programmatically associated reason and recovery; disabled semantics, tooltip, color, icon, position, or animation alone are insufficient.

Grant and removal use anchored high-impact flows with structured previews and focus-trapped confirmation. Cancel/Escape dispatch nothing; focus returns to the launcher or relevant failure region. Removal is low-emphasis and destructive; self-removal warns of possible future governance loss without claiming unproven session effects. Unsafe viewports leave mutation visibly unavailable while authorized review remains usable. Announce routine progress politely, blockers assertively, and success only after qualified confirmation.

## Cross-Story Dependencies

Story 4.1 owns the fixed-scope availability guardrail consumed by grant and removal and depends on Story 1.11's authorized review/direct read. Stories 4.2–4.3 share aggregate locking, lifecycle, authoritative re-query, support-safety, localization, and accessibility foundations; removal additionally requires a complete count. Epic 5 consumes these commands for audit-linked forward correction. Epic 4 exposes only evidence actually available and never fabricates audit completion.
