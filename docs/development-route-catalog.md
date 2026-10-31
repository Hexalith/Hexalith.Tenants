# Development routing catalog owner profile

This source profile uses the selected EventStore SDK's compiled schema 3 catalog
and complete activation binding v2. Its source prerequisite is pinned by
`references/Hexalith.EventStore`; a source result does not qualify a released
package floor.

The selected authority peers are the Gateway (`eventstore`), Admin.Server
(`eventstore-admin`), Tenants domain service (`tenants`), and generated REST host
(`tenants-api`). Only the compiled Tenants Contracts and Server adapters declare
business ownership. Gateway and Admin.Server observe their actual installed SDK
technical actions. Admin.Server's owner-state reader uses the actual selected
Gateway sidecar coordinates and owner credential.

Tenants.UI and Admin.UI retain their authenticated proxy transports outside the
authority cohort. The six Tenants REST reads use Dapr; Tenants.UI command and
status calls retain direct Gateway HTTPS. The Tenants `sample` remains its
existing pub/sub consumer and supplies no SDK Counter operations. Optional
Memories REST traffic is not qualified by this focused profile; an undeclared
workload path remains refused rather than receiving an inferred grant.

## Retained backend and key custody

A source AppHost run requires the `Tenants:DevelopmentRouteCatalog` section to
bind `DevelopmentRouteCatalogCompositionOptions`: `CustodyDirectory`,
`AuthorityComponentName`, `AuthorityStateKey`, `ActiveDigestKeyGeneration`,
`DigestKeys`, and `SecretMapGeneration`. Keep the custody directory and key ring
private and retained together with the actual business backend. An empty routing
cell does not establish that the business backend is new. Missing custody inputs
refuse startup; this profile never silently replaces persistent digest keys.

For an existing backend, supply its existing ring and retained predecessor
custody. Provision new material only together with an independently established
new isolated business backend. Never commit credentials, private custody, or raw
host observations. The SDK checks complete predecessor readbacks, current backend
coordinates, authority component membership, and full activation identity before
accepting a successor.

## Existing delegated bootstrap

`BootstrapGlobalAdmin` stays internal. The source profile enables the existing
Tenants startup service to cryptographically validate the configured
administrator's bearer with the installed authentication handler, then preserve
that subject in the existing bound workload command proof. The Gateway verifies
the actual workload assertion, source key, allowed domain, command payload, and
idempotency identity. The configured subject or an unvalidated JWT decode never
supplies authority. The legacy bootstrap wire remains unchanged outside this
explicit profile.

The private `tenants.bootstrap.json` delivery receipt records the one actual
hosted attempt and its response identity. It is workflow evidence, not a domain
projection or an authorization source. Runtime qualification follows the real
command status and persisted administrator projection rather than submitting a
replacement human internal command.

The Keycloak `hexalith-tenants-ui` authorization-code client alone issues one
`eventstore:surface=ui` access-token claim. Service clients, client-credential
tokens, proxies, and receivers do not acquire that claim through a shared mapper.

## Standalone source runtime qualification

The `development-catalog` job in `source-reference.yml` initializes only this
standalone repository's root-declared dependencies, nonrecursively. It records
the exact SDK source and evaluated producer version without overriding another
owner's numeric package catalog.

Before provisioning private keys, the job proves that Dapr initialization created
its actual Redis container and volumes after the job's recorded boundary, no
volume existed in the pre-initialization inventory, the selected localhost
listener belongs to that container, and the actual database is empty. Refusal
leaves prior evidence and environment inputs unchanged. The retained material is
scoped to that disposable job's genuinely new backend.

The mandatory `SourceCatalogOwnerProfileTests` case then requires all four actual
hosts to commit the complete v2 identity, the hosted bound bootstrap to complete
and project membership, a public tenant command to persist, all six real REST
reads and a conditional read to succeed, the real Admin UI issuer/proxy/owner
reader to load its infrastructure page, an undeclared UI descendant to refuse,
and the actual authority to have no outstanding leases. Missing prerequisites or
skipped cases cannot qualify this lane. Only the credential-free backend and
result records are uploaded; raw observations and key material remain private.
