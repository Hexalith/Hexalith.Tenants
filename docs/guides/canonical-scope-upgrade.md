# Canonical scope upgrade (7.0.0)

Tenants administration and cross-tenant catalog operations now use declared platform scopes. The public `system` tenant is reserved; existing `system` stream identities remain an internal storage detail behind the authenticated EventStore workload route.

The 7.0.0 package cohort requires EventStore 4.0.3 and McpCli.Abstractions 2.0.0. Upgrade those producers first, then upgrade all Tenants packages together. Update callers that supplied `system` as a public tenant or expected a per-call tenant for a platform-scoped operation. The catalog includes the global administrator audit query as a separate platform operation.
