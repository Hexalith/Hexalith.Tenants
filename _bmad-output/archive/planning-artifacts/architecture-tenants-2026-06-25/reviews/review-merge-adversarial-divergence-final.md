# Final Merge Adversarial Divergence Recheck

**Verdict:** FAIL — no critical finding remains, and the fixes materially improve the spine, but five high-severity divergence/contradiction holes remain before the canonical merge is safe for independent implementation.

**Recheck scope:** updated AD-2, AD-7/AD-12, AD-10, AD-13/AD-14, plus retained canonical prose and persistent project guidance. The attack requires two independently built units to obey the decisions literally and still fail to interoperate.

## Remaining high findings

### H1 — AD-2 identifies canonical routes but not canonical workspace-state encoding or compatibility behavior

AD-2 now fixes the route set and makes `/tenants` plus tab/scope/query state canonical. It still does not define the query keys/values, default omission rules, encoding, or whether `/tenants/my` and `/tenants/users` redirect versus render then replace/canonicalize the URL.

- Unit A emits `/tenants?tab=tenants&scope=mine&q=abc` and redirects compatibility routes.
- Unit B emits `/tenants?view=my-tenants&query=abc` and renders compatibility routes in place while only return links use `/tenants`.

Both obey AD-1/AD-2, expose one shell entry, use page-local switching, and canonicalize returns. Their bookmarks, cross-component links, restored filters, browser history, and route-smoke tests are incompatible.

**Required closure:** add the exact canonical workspace-state schema (keys, legal values, defaults, escaping, ordering if tests require it) and explicit compatibility-route behavior, including whether redirects preserve query/fragment state.

### H2 — AD-7/AD-12 still allow pre-existing state or NoOp to be classified incompatibly

AD-12 now fixes lock scope and requires command-specific expected postcondition evidence from an authoritative re-query. It does not say whether the postcondition must be causally newer than the pre-submit baseline. Retained D2 prose separately maps NoOp to `already applied` (`architecture.md:496-497`).

- Unit A submits a role-change command, re-queries, sees the requested role already present, and marks `confirmed` because the expected postcondition exists.
- Unit B compares the pre-submit baseline, sees no projection advancement, and terminates as `already applied`; it reserves `confirmed` for evidence newer than the accepted attempt.

Both use the same aggregate, authoritative projection evidence, shared lifecycle, and no optimistic success. They disagree on lifecycle, receipt wording, audit expectation, and when the aggregate lock releases.

**Required closure:** specify baseline capture and causality/version requirements, or explicitly state that an already-satisfied postcondition confirms. Define precedence among `confirmed`, `already applied`/NoOp, rejection, timeout, and unverifiable evidence, and require the shared confirmation contract to encode it.

### H3 — AD-10 does not define how the opaque search cursor advances across dropped hits

AD-10 now fixes result-window ordering, deduplication, authorization filtering, visible sort, no backfill, degradation, and outage fallback. It does not define whether the cursor advances over the raw Memories window or the hydrated visible rows, nor whether the cursor is bound to query, sort, actor/authorization scope, and index generation.

- Unit A advances by the raw Memories offset/window length, so duplicates, deleted ids, and forbidden hits are consumed even when no row is rendered.
- Unit B advances by the number of deduplicated hydrated rows, causing a later request to revisit raw hits from the previous window.

Both drop without backfill and expose only an opaque cursor. They can produce duplicates, skips, or loops across pages and disagree when sort/query/auth changes.

**Required closure:** define cursor payload semantics without exposing the payload: raw-window next offset, query/index/sort/auth scope binding, expiry/invalidation, and behavior after partial hydration or authorization changes.

### H4 — canonical prose still contains incompatible component-location maps

The spine's Consistency Conventions make `Components/Tenants`, `Components/Users`, `Components/Pages`, and `Components/Shared` canonical. Retained prose still directs agents to sibling `Components/Audit` and `Components/GlobalAdministrators` (`architecture.md:609`, `architecture.md:626`), maps audit implementation to `Components/Tenants/Audit` (`architecture.md:784-785`), and later claims it is homed in `Components/Audit` (`architecture.md:849`). These are all imperative or validation-style sections, not clearly marked historical.

- Unit A follows the canonical spine/complete tree and places route pages in `Pages` and domain audit components in `Tenants/Audit`.
- Unit B follows Naming/Structure Patterns and creates sibling `Audit` and `GlobalAdministrators` namespaces.

The AD precedence sentence technically favors the spine, but the retained rules still cause duplicate component names, namespace/resource divergence, and mismatched mirrored tests for agents consuming the full canonical document.

**Required closure:** normalize every tree, mapping, naming rule, and validation statement to one component layout, or explicitly mark the conflicting blocks as non-normative history.

### H5 — AD-13 now aligns with repository policy, but persistent root project context still contradicts it

AD-13 correctly makes orchestration platform-owned and the repository AppHost transitional. The always-loaded root project context still says: "AppHost is the allowed repository-specific technical component" and directs agents to modify/restart it (`_bmad-output/project-context.md:77-78`, `:148-151`). That guidance is newer-looking, imperative, and foundational to implementation agents.

- Unit A follows AD-13 and refuses to expand the repository AppHost, preparing migration to a composing host.
- Unit B follows persistent project context and adds new Tenants UI/Memories/auth/DAPR wiring to the repository AppHost as the explicitly allowed repository technical component.

Both can claim compliance with a canonical project source; their deployment topology and ownership diverge.

**Required closure:** update project context with the AD-13 transitional rule and migration boundary before implementation readiness. Do not rely only on the architecture precedence sentence because project context is loaded independently by implementation workflows.

## Rechecked areas now closed at critical/high severity

- The prior AD-9 contradiction is closed: retained overview/constraint text now says receipts and previews are server-side BFF-assembled.
- AD-12 now fixes the concurrency key to `(interactive circuit, AggregateIdentity)` and permits unrelated aggregates to proceed.
- AD-10 now fixes returned-id ordering, hydration/authorization filtering, visible per-page sort, no-backfill behavior, partial degradation, and outage fallback; only cursor advancement/scope remains open.
- AD-13 now resolves UI-host versus orchestration ownership and treats the repository AppHost as migration debt rather than a pattern to expand.
- AD-14 supplies a clear one-replica ceiling until DataProtection, routing/session, and cursor-durability conditions are verified; no additional critical/high divergence was found in its rule.

## Gate recommendation

Do not mark the architecture merge fully divergence-safe yet. Close H1-H3 in the precedence-layer decisions/contracts and repair H4-H5 across the canonical prose and persistent project context. After those changes, this adversarial lens should be rerun only as a focused confirmation; the previously critical safety, concurrency-scope, and orchestration-ownership issues do not need to be reopened unless the fixes change.
