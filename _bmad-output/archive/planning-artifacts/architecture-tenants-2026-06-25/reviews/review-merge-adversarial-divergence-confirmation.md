# Merge Adversarial Divergence Confirmation

**Verdict:** PASS — the canonical architecture is archive-ready. The four previously blocking high-severity divergence holes are closed. The project-context AppHost contradiction is explicitly recorded as a separately authorized source-reconciliation follow-up and does not prevent archiving the legacy architecture set.

## Confirmation results

### Canonical workspace state and compatibility routes — closed

AD-2 now defines the canonical `/tenants` state keys and legal values: `tab=tenants|users`, `scope=all|mine`, `userId`, `search`, `status`, `sort`, `desc`, and `cursor`. It specifies fail-safe normalization, cursor reset when tab/scope/filter/sort changes, renderable `/tenants/my` and `/tenants/users` compatibility routes, canonical generated/return URLs, and the contextual detail/audit/global-administrator routes. The prior redirect-versus-render and incompatible-query-schema attack no longer survives the rule.

### NoOp/pre-existing state versus confirmed — closed

AD-12 now requires expected postcondition evidence plus projection-version advancement or safe command-specific audit evidence beyond the pre-submit baseline. It explicitly classifies a pre-existing expected state or NoOp as `already applied`, never `confirmed`, and missing provenance as `unable to verify`. The fuller Communication Pattern repeats the same precedence. Independent command units can no longer classify the same evidence differently while obeying the architecture.

### Search cursor advancement and scope — closed

AD-10 now advances the next offset by raw Memories hits consumed, including malformed, duplicate, unauthorized, and unhydrated hits; dropped hits are not backfilled. It binds the opaque cursor to authenticated user plus normalized query/status/sort/page-size scope and resets mismatches to page 1 with an honest notice. This closes the duplicate/skip/loop attack and the cross-query/cross-user cursor-reuse ambiguity.

### Component paths — closed and reality-aligned

The canonical conventions, Naming Patterns, Structure Patterns, requirements mapping, and validation text now agree:

- route surfaces: `Components/Pages/`
- tenant and audit-domain surfaces: `Components/Tenants/`, including `Components/Tenants/Audit/`
- user lookup/self-audit: `Components/Users/`
- reusable domain views: `Components/Shared/`
- global-administrator state: `State/GlobalAdministrators/`
- audit state: `State/TenantAudit/`

Filesystem inspection confirms these are the actual implementation paths, including `Components/Pages/GlobalAdministratorsPage.razor`, `Components/Pages/TenantAuditPage.razor`, `Components/Tenants/Audit/*`, `State/GlobalAdministrators/*`, and `State/TenantAudit/*`. No sibling `Components/Audit/` or `Components/GlobalAdministrators/` implementation path remains in the canonical guidance.

## Authorized open reconciliation

The root `_bmad-output/project-context.md` AppHost exception still conflicts with AD-13, but the architecture now records that exact source reconciliation in both canonical Deferred Decisions and the spine's Deferred table. Per the explicit authorization for this confirmation, it is a tracked follow-up rather than an architecture-archive blocker. AD-13 itself is unambiguous: the UI host is domain-owned, distributed orchestration is platform/composing-host owned, and the repository AppHost is transitional legacy that must not expand.

## Archive gate

No critical or high adversarial-divergence finding remains within the architecture spine or canonical merged architecture. The legacy architecture folder may be archived while preserving the separately authorized project-context reconciliation as open follow-up work.
