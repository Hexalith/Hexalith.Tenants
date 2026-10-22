# Tenants module manifest inventory

`module/hexalith.module-manifest.v2.json` declares the existing module surfaces
using the Builds v2 schema. Its inventory describes behavior; it does not
register ASP.NET authorization policies, grant permissions, execute recovery,
or prove that a deployed environment is ready. Platform enrollment and runtime
qualification remain separate.

## Topic inventory and physical routing

`integration.topics[].name` is a kebab-case inventory identifier. The existing
`tenants-events` entry has no `subscription`; the Platform Dapr composer skips
unsubscribed inventory entries when generating subscriptions. It does not create
or rename a publisher topic. The current physical publisher topic is
`tenants.events`, as declared by the AppHost routing and
[event contract](event-contract-reference.md#event-delivery-model).

For a future declared subscriber, v2 supports the physical topic in
`subscription.topic`, alongside the receiving `server` and `route`. Without that
override the consumer uses the inventory name. Do not add a subscription merely
to document a publisher: it would change deployment behavior. The entry's
`deadLetter.strategy: none` describes its lack of a declarative dead-letter
subscription; EventStore application-level dead-letter publication remains the
separate contract documented in the event reference.

## Public operation inventory

The public interfaces inventory every current REST-decorated command and query
in `Hexalith.Tenants.Contracts`, grouped by their effective route. Operations are
semantic names, not literal relative paths. Preserve these sets when updating
the contracts:

| Interface | Operations | Contract ownership |
| --- | --- | --- |
| `tenant-api` | Nine commands: create/update, enable/disable, add/remove user, change role, set/remove configuration; four queries: list tenants, get tenant, get tenant users, get tenant audit | Relative `RestRoute` declarations in the `tenants` API scope |
| `global-administrators-api` | List, set and remove administrator | The three absolute `/api/global-administrators` route declarations |
| `user-tenants-api` | Get user tenants | Absolute `/api/users/{userId}/tenants` route declaration |

`BootstrapGlobalAdmin` has no public REST route decoration and is not an
additional public operation. Domain dispatch and administration remain described
by the separate internal `tenant-domain` interface. Delegated domain queries
without public REST decoration do not expand this public inventory.

The `global-administrator-management` critical flow includes list, set **and
remove**. This is the complete existing administrator-management operation set;
it does not imply that a smoke test has run or authorize removing anyone. The
tenant-management flow remains its existing representative create/get scenario;
critical flows are selected operation references, while public interfaces carry
the complete decorated-operation inventory.

## Administrator authorization inventory

The descriptive label `authenticated-current-global-administrator` means
authentication **and** current administrator membership. It is not a newly
registered ASP.NET policy or a permission claim to issue in a token.

`GetGlobalAdministratorsQueryHandler` first requires the fixed `system` tenant
and singleton aggregate scope, then requires the authenticated requester's user
ID in the persisted singleton administrator read model. An untrusted wire
administrator hint does not replace that check. Set/remove commands independently
check the actor against the authoritative `GlobalAdministratorsState`; removal
also preserves the last administrator. The manifest label describes the common
membership condition, not every operation-specific rejection.

The query read model and other authorization channels can lag; this inventory
does not establish immediate revocation or close
[DW-562 and DW-617](../_bmad-output/implementation-artifacts/deferred-work.md).

## Recovery inventory and replay prerequisites

The manifest records three `rebuild-only` read models owned by `tenants`, each
dependent on authoritative `eventstore` history:

| Inventory entry | Evidence to rebuild and verify |
| --- | --- |
| `global-administrator-authority` | Singleton membership and its ordered projection provenance at `projection:global-administrators:singleton` (`TenantQueryHandlerBase.GlobalAdminProjectionKey`) |
| `global-administrator-audit` | Canonical singleton audit at `audit-global-administrators:singleton` |
| `system-tenant-audit` | The distinct literal `system` tenant audit at `audit-tenants:system` |

These are disposable projections, not backups of durable event histories.
No `restoreHook` is declared: the repository supplies no executable production
replay command, and its `tasks` and `recoveryHooks` remain empty. Inventory
declaration neither executes nor authorizes replay and does not claim that a
complete automated recovery procedure exists.

Before a separately authorized replay, follow the
[audit rollout and replay runbook](audit-stream-rollout.md): stop old writers,
preserve backups and immutable legacy evidence, establish ownership and
serialization, and verify both independent histories. Replaying the singleton
alone cannot restore lost tenant evidence. Distributed singleton replay
serialization remains open as DW-627. Keep correction unavailable until complete
authorized administrator and explicit global audit reads provide ordered
provenance covering the committed sequence; successful replay HTTP responses or
inventory validation alone are insufficient.
