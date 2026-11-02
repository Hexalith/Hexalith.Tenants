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

## Published SDK qualification

The default package selection is stable EventStore `3.132.1`. The finite qualified
binary set is `3.129.0`, `3.129.1`, `3.130.0`, `3.130.1`, `3.130.2`, `3.131.0`, `3.132.0` and `3.132.1`;
zero fourth revisions and build metadata normalize to the same stable cohort.
Catalog qualification refuses prereleases, nonzero fourth revisions, unknown
versions and mixed SDK cohorts. The legacy build floor can still accept
`3.128.x` selections without catalog enrollment. Explicit `3.128.4`
compatibility builds retain the frozen host harness and cannot publish Tenants
packages.

The normal `published-catalog-http` CI matrix tests the actual default without a
version override and the explicit `3.129.0` floor. Both lanes require compiled
enrollment, the complete owner catalog, all generated HTTP cases, ordinary stale
artifact refusals, and a fresh packed Contracts/Server consumer. The consumer
checks source metadata and candidate archive/DLL hashes before discovering all
22 routes, 22 operations and 12 command adapters through the public SDK APIs.
Binary qualification enables `HEXALITH_DEVELOPMENT_CATALOG_SOURCE`; only an
actual source graph enables `HEXALITH_DEVELOPMENT_CATALOG_COHORT`.

Reproduce the public package checks from this repository with .NET pinned by
`global.json`, using the root-declared Builds and Platform inputs (no nested
initialization):

```bash
export RepositoryCommit="$(git rev-parse HEAD)"
catalog_args=(-p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true
  -p:HexalithEventStoreFromSource=false -p:HexalithFrontComposerFromSource=false
  -p:HexalithMemoriesFromSource=false)
# Append -p:HexalithEventStoreVersion=3.129.0 to repeat the explicit floor lane.
for catalog_suite in IntegrationTests Contracts.Tests; do
  dotnet build "tests/Hexalith.Tenants.${catalog_suite}/Hexalith.Tenants.${catalog_suite}.csproj" \
    -c Release -warnaserror -m:1 -nr:false "${catalog_args[@]}"
done
dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj \
  -c Release --no-build "${catalog_args[@]}" \
  --filter-class Hexalith.Tenants.IntegrationTests.TenantsApiGeneratedControllerTests \
  --minimum-expected-tests 66 --fail-skips on
dotnet test tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj \
  -c Release --no-build "${catalog_args[@]}" \
  --filter-class Hexalith.Tenants.Contracts.Tests.PublishedTenantCatalogTests \
  --minimum-expected-tests 1 --fail-skips on
dotnet test tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj \
  -c Release --no-build "${catalog_args[@]}" \
  --filter-class Hexalith.Tenants.Contracts.Tests.TenantCatalogTests \
  --minimum-expected-tests 2 --fail-skips on
python3 tests/scripts/test_catalog_package_consumer.py
python3 tests/scripts/test_catalog_artifact_guards.py
```

To verify fresh candidates, set `RepositoryCommit` to the exact source SHA when
building and packing. Use an empty temporary package directory and one unique
candidate version for both packages:

```bash
catalog_packages="$(mktemp -d)"
for catalog_package in Contracts Server; do
  dotnet pack "src/Hexalith.Tenants.${catalog_package}/Hexalith.Tenants.${catalog_package}.csproj" \
    -c Release --no-build --output "$catalog_packages" \
    -p:Version=0.0.0-catalog-qualification "${catalog_args[@]}"
done
python3 scripts/verify-catalog-package-consumer.py "$catalog_packages" \
  --expected-source-sha "$RepositoryCommit" --output "$catalog_packages/consumer.json"
```

The [3.132.1 qualification record](eventstore-3.132.1-qualification.json) records
all fourteen verified public SDK archives from producer
`0ace1545c433bf234f54cef3a57ac77d62b4175f`, focused local results and their limits.
The dated [3.132.0 qualification record](eventstore-3.132.0-qualification.json) and
[3.131.0 qualification record](eventstore-3.131.0-qualification.json) remain historical evidence. Public JWT positives exercise the declared
`UpdateTenant` operation. Bootstrap conflict and wrong-target controls use the
existing internal bound-workload endpoint with a cryptographically validated
human subject, a separately issued workload assertion, RSA command proof and
application-channel token. A mismatched target still reaches the real domain
rejection; the bootstrap creator retains its earlier refusal before sending.
These in-process transports do not qualify native Dapr or container execution. Local
candidates identify the working-tree source base and include the uncommitted
implementation; they are qualification inputs, not published releases. Exact committed-source
hosted bootstrap and release qualification remains a separate gate.
