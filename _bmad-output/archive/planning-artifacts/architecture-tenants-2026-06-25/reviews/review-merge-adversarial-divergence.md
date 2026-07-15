# Merge Adversarial Divergence Review

**Verdict:** FAIL — the merged architecture is directionally coherent, but it is not yet divergence-tight. Independent teams can obey AD-1 through AD-13 literally and still produce incompatible routes, command coordination, confirmation behavior, search result sets, and shared-state APIs. The merged prose also retains direct contradictions and multiple structural maps that weaken the declared AD precedence layer.

**Lens:** construct two independently built units one level below the architecture, require both to obey every AD, and treat any incompatible result as a missing or insufficiently precise decision. Separately, scan the merged `architecture.md` for retained guidance that can override, contradict, or confuse AD-1 through AD-13.

## Divergence attacks

### 1. Critical — AD-1/AD-2 do not define canonical route identity or alias behavior

Evidence: AD-1 allows sub-surfaces to be "page-local tabs, scope modes, aliases, or contextual links"; AD-2 says old routes remain "aliases or canonical links". The Global Administrators diagram only calls its route "contextual or policy-gated". No canonical route table, redirect rule, selected-tab encoding, or return-location contract exists.

- Unit A makes `/tenants/my` and `/tenants/users` redirects to `/tenants?scope=my` and `/tenants?tab=users`, and uses `/tenants/global-administrators`.
- Unit B renders the workspace directly at the legacy paths, preserves the old URL, and uses `/global-administrators` as a contextual deep link.

Both expose exactly one shell entry, keep Tenants/Users switching page-local, avoid an all-users inventory, and use aliases/contextual links as permitted. They nevertheless disagree on bookmark identity, tab selection, history/back behavior, return links, route parameters, and route-smoke-test expectations.

**Required closure:** tighten AD-2 with a canonical route/alias table for Tenants, My Tenants, Users, tenant detail, audit, and Global Administrators. Specify redirect versus in-place alias behavior, tab/scope encoding, parameter names, and return-location preservation.

### 2. Critical — AD-12 leaves the one-at-a-time lock scope undefined

Evidence: AD-12 and the Commands convention require the approved "one-at-a-time" fallback, but neither identifies the serialization key or lifetime.

- Unit A uses one semaphore per command-flow component. Two open flows for the same tenant can submit concurrently.
- Unit B serializes by `(actor, tenantId)` across the server circuit; a third implementation could use one circuit-global lock and block unrelated tenants.

Each unit is one-at-a-time within its chosen scope, uses the shared gateway/lifecycle posture, previews where required, re-queries projection truth, and avoids bulk/toast batching. The units are operationally incompatible and produce different conflict, cancellation, and sibling-action behavior.

**Required closure:** amend AD-12 with the serialization key, owner, acquisition point, release conditions, reconnect behavior, and whether Global Administrator commands share or use a separate lock domain. The conformance test must open sibling flows, not merely double-click one component.

### 3. High — AD-7/AD-12 do not define what projection evidence confirms a command

Evidence: the rules say an authoritative projection re-query confirms, but do not define the confirmation predicate or how concurrent writers are distinguished.

- Unit A marks a command confirmed when re-queried business fields equal the requested values.
- Unit B requires a post-acceptance projection revision/change plus the target state; another unit could require terminal command status before evaluating the same projection.

All variants treat SignalR/status as nudges, re-query authoritatively, keep accepted/confirmed/audit distinct, and avoid optimistic success. Under NoOp, already-applied state, a concurrent writer producing the same value, or a correction racing the original command, they reach different lifecycle outcomes.

**Required closure:** define a shared confirmation contract: command-specific evidence predicate, baseline/revision handling, NoOp/already-applied semantics, timeout/unverifiable terminal state, and the relationship between terminal command status and projection evidence. Put it in one shared type/test fixture used by every command flow.

### 4. High — AD-10 leaves search ordering and pagination semantics open

Evidence: AD-10 fixes Memories as id-only and requires authoritative hydration/fallback, but says nothing about rank preservation, cursor interaction, caps, deduplication, or missing/unauthorized ids.

- Unit A hydrates the complete Memories match set, preserves Memories score order, drops missing ids, then page-slices the hydrated results.
- Unit B intersects the match set with the current cursor page and preserves authoritative cursor-list order; it never retrieves matches outside that page.

Both use only ids from `tenants-index`, render row truth only from Tenants reads, and fall back to the cursor list on outage. Users get incompatible result sets, ordering, counts, and next-page behavior; authorization and performance characteristics also differ.

**Required closure:** add a search result contract covering order, page/cursor semantics, maximum match set, deduplication, partial hydration, deleted/forbidden ids, errors, and fallback-state labeling.

### 5. High — AD-7/AD-8 name shared typed state but not a shared shape or action-safety matrix

Evidence: AD-7 requires typed shared truth/freshness/lifecycle/audit/authorization state; AD-8 says stale/unknown fails closed "where the safety contract requires it." The spine does not name the shared contract, its owning assembly, transition API, or which actions the qualifier covers.

- Unit A passes a single `TruthSnapshot` to every actionable component and blocks every command while freshness is stale/unknown.
- Unit B uses separate shared enum/value objects inside surface-specific snapshots and blocks only destructive/high-risk flows, leaving lower-risk commands enabled.

Both use typed shared state and fail closed where their interpretation of the safety contract requires it. Their component signatures and action availability disagree. The fuller architecture partially narrows this with a canonical Vocabulary and gating order, but still allows "no specific state framework" and supplies no authoritative action-to-risk matrix.

**Required closure:** nominate the canonical state contract and owner, required fields and transition invariants, and an action-safety matrix defining how `current`, `aging`, `stale`, `unknown`, and transient `refreshing` affect each command/read action.

### 6. Medium — AD-3/AD-4 do not define who may approve and register a fallback

AD-3 permits custom markup/CSS for a "documented gap"; AD-4 permits an "approved fallback". Neither defines approval authority, the registry/allowlist, expiration, or the test proving a FrontComposer/Fluent equivalent is absent. One unit can record a story-local gap and conformance exception while another accepts only the existing FC-AUD/FC-CNS/FC-CNC approval record. Both can claim literal compliance, but their guards and ownership boundaries differ.

**Required closure:** define one fallback registry and approving role, require a named platform gap, scope each exception to exact components/markup/styles, and prohibit per-test local allowlists.

## Post-merge contradiction and override audit

### 7. Critical — retained "client-assembled" safety guidance contradicts AD-9

AD-9 requires the BFF to assemble and redact receipts, consequence previews, and rejection text before anything reaches the DOM (`architecture.md:128`). Later security guidance repeats that server-side rule (`architecture.md:464-467`). However, the retained Requirements Overview says previews and receipts are "client-assembled" (`architecture.md:181-183`), and Technical Constraints says receipts/previews/status are "assembled client-side" (`architecture.md:227-228`). In an InteractiveServer system, "client" is sometimes used loosely, but these sentences explicitly invite a Razor/component implementation that assembles output from raw fields—the boundary AD-9 prevents.

**Required closure:** replace both retained client-side statements with unambiguous server-side BFF assembly language. If "client" means the server-side UI host rather than the browser/component layer, name that boundary explicitly.

### 8. High — the merged document contains three incompatible component-location maps

The spine seed places domain surfaces under `Components/Tenants`, `Components/Users`, `Components/Pages`, and `Components/Shared`. Retained naming/structure guidance introduces sibling `Components/Audit` and `Components/GlobalAdministrators` (`architecture.md:593`, `architecture.md:614`). Requirements mapping instead places audit components under `Components/Tenants/Audit` (`architecture.md:768-769`), while validation again claims `Components/Audit` (`architecture.md:833`). Independent stories can follow different normative-looking sections and create duplicate names, namespaces, resources, and mirrored tests.

**Required closure:** choose one canonical tree, align every mapping and example to it, and label superseded historical trees non-normative or remove them from the canonical document.

### 9. High — AD-13 conflicts with the foundational domain-module AppHost rule

AD-13 requires `src/Hexalith.Tenants.AppHost`. The merged prose attempts to resolve this by asserting that the no-AppHost policy applies only to domain-service modules and that a presentation host is distinct (`architecture.md:527-528`). The repository instruction actually says a domain module must not ship its own `*.AppHost`, `*.Aspire`, or `*.ServiceDefaults` project without that exception (`references/Hexalith.AI.Tools/hexalith-llm-instructions.md:126-129`). Root project context separately says this repository's AppHost is allowed, so two foundational sources conflict. One team can follow AD-13/root project context; another can follow repository-wide policy and move topology to a platform/host repository.

**Required closure:** record an explicit repository-policy exception or amend the foundational instruction; architectural prose cannot silently narrow the higher-level rule.

### 10. Medium — retained FrontComposer command-dispatch ownership can bypass AD-5

Starter/history sections state that the FrontComposer Shell "provides ... command dispatch" (`architecture.md:303-305`, `architecture.md:362-364`), while AD-5 makes `ITenantCommandGateway` and server-side collaborators the only backend egress and AD-12 assigns the shared gateway/lifecycle posture. The document does not say whether FrontComposer's dispatcher is UI chrome, a contract behind `ITenantCommandGateway`, or an alternative transport. The same section still says exact API names are "to be confirmed" despite later claims that FC-CMD readiness is closed.

**Required closure:** state the dependency direction explicitly: components/FrontComposer command UI -> Tenants command composition -> `ITenantCommandGateway` -> EventStore. Remove or label the unconfirmed historical integration wording so it cannot be read as a second egress path.

## Gate recommendation

Do not call the merged architecture divergence-safe yet. At minimum, close Findings 1-5 and remove the AD-9 contradiction before implementation readiness is marked ready. Resolve the AppHost policy conflict before treating AD-13 as binding across repositories. Findings 6, 8, and 10 are editorially repairable but should be repaired in the canonical document before the legacy architecture is archived.
