---
status: in-review
date: '2026-10-31'
baseline_commit: fff6a5cb556ee0012974550c708eb7a871a25001
---

# Publish the existing compiled Tenants catalog inputs

## Intent

The operator requests latest packages, working CI and publication without another
human checkpoint. EventStore3.129.0 publishes the catalog APIs previously limited
to source builds and requires activated catalog admission. Tenants5.11.2 lacks its
compiled declaration-source/adapter-source enrollment and Operations property.
Ship the already implemented owner declarations and adapters through the existing
Contracts and Server packages, with no new declaration, grant or trust policy.
The composition owner remains responsible for real SDK activation/readback.

## Scope and implementation

- Set the normal EventStore default to3.129.0 before the shared catalog import.
  Explicit3.128.4 compatibility selections and exact source mappings retain their
  existing authority and the completion-recovery floor remains enforced.
- Compile the existing catalog branches for source builds and exact qualified
  package3.129.0. This selects API availability only; project-reference selection
  conditions remain unchanged. Existing Contracts and Server are already two of
  the five published packages, so the package inventory does not change.
- Refuse NuGet packing when catalog artifacts are unavailable. Frozen3.128.4
  compatibility builds cannot become a release candidate.
- The existing disposable compatibility selector already freezes EventStore host
  source717a06e16c3afd81b904eaf4de646b74b13262d2 in three named topology jobs.
  Export the same SDK3.128.4 through GITHUB_ENV to all subsequent runtime, domain
  and test builds in those jobs. The normal build/package and source jobs retain
  their current source/default SDK. Reject a missing environment channel before
  checkout. All frozen test counts, authentication, persistence and skip gates
  remain enabled.
- Execute actual SDK declaration discovery and adapter inventory tests in normal
  package builds. An unconditional metadata test inspects the actual SDK surface
  independently of the compilation symbol, so deleting the symbol cannot hide
  the missing owner enrollment. Preserve legacy metadata absence for3.128.4.

## File Scope

Allowed files for this story:

- `.github/workflows/ci.yml`
- `.github/workflows/source-reference.yml`
- `Directory.Build.props`
- `Directory.Build.targets`
- `README.md`
- `_bmad-output/implementation-artifacts/spec-publish-compiled-catalog-inputs.md`
- `references/Hexalith.Builds`
- `scripts/build-release-candidate.sh`
- `scripts/prepare-bootstrap-compatibility.sh`
- `scripts/prepare-partial-release.sh`
- `scripts/verify-catalog-package-consumer.py`
- `src/Hexalith.Tenants.Api/Program.cs`
- `src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj`
- `tests/Hexalith.Tenants.Contracts.Tests/CiQualityGateScriptTests.cs`
- `tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj`
- `tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs`
- `tests/Hexalith.Tenants.Contracts.Tests/PublishedTenantCatalogTests.cs`
- `tests/Hexalith.Tenants.Contracts.Tests/TenantCatalogTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/TenantsApiGeneratedControllerTests.cs`
- `tests/scripts/test_bootstrap_compatibility.py`
- `tests/scripts/test_candidate_version.py`
- `tests/scripts/test_catalog_artifact_guards.py`
- `tests/scripts/test_catalog_package_consumer.py`
- `tests/scripts/test_partial_release_recovery.py`
- `tools/CatalogArtifactVerifier/CatalogArtifactVerifier.csproj`
- `tools/CatalogArtifactVerifier/Program.cs`
- `tools/catalog-package-consumer/Program.cs`
- `tools/catalog-package-consumer/approved-route-ids.txt`
- `src/Hexalith.Tenants/Hexalith.Tenants.csproj`
- `tests/scripts/test_hosted_package_profile.py`
- `tests/Hexalith.Tenants.Contracts.Tests/PartialReleasePublicationTests.cs`

No declaration body, canonical intent normalization, grant or Production trust implementation changes.

## Verification

Use a disposable ordinary source copy and the exact owning Builds6cc96b4ff23c40c77651e1e4f85ed3170c25c7a4
catalog, without initializing nested submodules or changing original producer
outputs. Build Contracts.Tests and both native hosts Release/package with warnings
as errors. Run compiled catalog and metadata tests, version-selection/packability
governance and compatibility selector fixtures. Pack the unchanged five-package
manifest; validate actual nuspec SDK dependencies and embedded Contracts/Server
PE metadata. Run a legacy3.128.4 compile/test probe and require publication refusal.
The parent owns three independent reviews, hosted exact-source CI and Release.

## Evidence

- Public API inspection: `dnx dotnet-inspect -y -- member Hexalith.Tenants.Contracts.Routing.TenantCatalogDeclarations --package Hexalith.Tenants.Contracts@5.11.2`
  confirms Operations absent. SDK3.129.0 exposes Read and ReadOperations. Logs
  `/tmp/tenants-5112-catalog-api.log`, `/tmp/tenants-3129-catalog-reader-api.log`.
- `python3 tests/scripts/test_bootstrap_compatibility.py`:8tests pass, including
  all three topology jobs and refusals for non-CI, unknown job, missing environment
  channel, dirty source and undeclared source; ordinary build and local check keep
  their existing source/SDK. No real checkout is changed by the fixtures.
- Disposable source workspace `/tmp/tenants-catalog-qualification-q5CetSxR`, exact
  owning Builds6cc96b4f; no nested submodule initialization. Contracts.Tests Release
  package build: zero warnings/errors. Actual catalog discovery/metadata tests:
  3passed; version default/floor, publication refusal, packability and script
  bridge:32passed. Both native domain/API hosts and Client/Aspire projects build
  Release/package with zero warnings/errors. Logs `/tmp/tenants-published-catalog-*-build.log`,
  `/tmp/tenants-published-catalog-runtime-tests.log` and
  `/tmp/tenants-published-catalog-governance-tests.log`.
- `python3 scripts/pack-release-packages.py <temporary nupkgs> 5.12.0-qualification`
  and `python3 scripts/validate-nuget-packages.py <temporary nupkgs>`: PASS, all five
  existing package IDs. No publication is performed. A separate PEReader probe
  reads the DLL bytes embedded in these actual archives: Contracts contains the
  exact declaration-source attribute and public static Operations getter; Server
  contains the exact adapter-source attribute. Every EventStore dependency in all
  five nuspecs is3.129.0. Receipt `/tmp/tenants-published-catalog-package-pe.json`;
  probe source `/tmp/tenants-catalog-package-probe/Program.cs`.
- Independent fresh3.128.4 copy: Contracts.Tests Release build zero warnings/errors;
  metadata test1/1passes with the historical absent enrollment. Actual
  `dotnet pack --no-build` refuses this profile with the new explicit publication
  diagnostic. Logs `/tmp/tenants-published-catalog-legacy-{build,tests,pack-rejection}.log`.
- Negative compiler-symbol mutation in another disposable copy: removing the
  catalog DefineConstants entry still builds3.129.0 but the unconditional
  metadata test fails exactly1/1, proving the guard cannot silently remove its
  own verification. Original source/output directories remain unchanged.
- The first combined class/method-filter command selected zero tests; it is not
  counted as verification. Separate executed runs above provide the3+32 results.
- Independent parent reviews and hosted exact-source CI remain outstanding. No
  commit or push is authorized before parent review.


## File List

- `.github/workflows/ci.yml`
- `.github/workflows/source-reference.yml`
- `Directory.Build.props`
- `Directory.Build.targets`
- `README.md`
- `_bmad-output/implementation-artifacts/spec-publish-compiled-catalog-inputs.md`
- `references/Hexalith.Builds`
- `scripts/build-release-candidate.sh`
- `scripts/prepare-bootstrap-compatibility.sh`
- `scripts/prepare-partial-release.sh`
- `scripts/verify-catalog-package-consumer.py`
- `src/Hexalith.Tenants.Api/Program.cs`
- `src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj`
- `tests/Hexalith.Tenants.Contracts.Tests/CiQualityGateScriptTests.cs`
- `tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj`
- `tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs`
- `tests/Hexalith.Tenants.Contracts.Tests/PublishedTenantCatalogTests.cs`
- `tests/Hexalith.Tenants.Contracts.Tests/TenantCatalogTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/TenantsApiGeneratedControllerTests.cs`
- `tests/scripts/test_bootstrap_compatibility.py`
- `tests/scripts/test_candidate_version.py`
- `tests/scripts/test_catalog_artifact_guards.py`
- `tests/scripts/test_catalog_package_consumer.py`
- `tests/scripts/test_partial_release_recovery.py`
- `tools/CatalogArtifactVerifier/CatalogArtifactVerifier.csproj`
- `tools/CatalogArtifactVerifier/Program.cs`
- `tools/catalog-package-consumer/Program.cs`
- `tools/catalog-package-consumer/approved-route-ids.txt`
- `src/Hexalith.Tenants/Hexalith.Tenants.csproj`
- `tests/scripts/test_hosted_package_profile.py`
- `tests/Hexalith.Tenants.Contracts.Tests/PartialReleasePublicationTests.cs`

## Completion Notes

The operator authorizes dependency synchronization. SDK Testing3.129.0 requires
JsonSchema.Net>=9.6; original owner Builds6cc pins9.5 and the IntegrationTests
restore concretely refuses NU1109. Adopt the already qualified workspace
Buildsa22 catalog (JsonSchema9.6, AngleSharp1.9, FrontComposer4.7.2 and
Tenants5.11.2); ContainerProvenance.targets is unchanged. Only the gitlink moves,
with no nested checkout initialization. The proposed pointer is not staged until
parent independent review finishes.

| path | baseline | target |
| --- | --- | --- |
| `references/Hexalith.Builds` | `2186b9a4bc507a116a457c590c80945c2e232cbd` | `a22b55f0a4f6d2aaa3a5ccc505896064ac595364` |


### Coherent central catalog continuation

The original6cc catalog produced NU1109 because JsonSchema.Net9.5 is below
EventStore.Testing3.129's9.6 requirement. The approveda22 archive resolves it.
The first disposable integration build then exposed missing linked Platform
source files (CS2001), a copy prerequisite rather than a product defect. Reading
the two unchanged helper files at the exact owner-selected Platform
667e5a02a523cf8764ca54d6aa1b4b75db5dca64 into a sibling ordinary source directory
resolves that prerequisite, without initializing any submodule. IntegrationTests
Release/package graph now builds with zero warnings/errors in10.95s, including
current domain/API/UI hosts. Existing frozen Aspire fixture helper3.128.4 remains
explicitly separate; runtime library package dependencies use3.129.0.


### Additional HTTP fixture gap (parent triage required)

Full `TenantsApiGeneratedControllerTests` on the new package graph executes47
cases:24pass,23fail, zero skips. Failing cases receive503 before the capturing
gateway is called because the new generated action requires a committed catalog
lease. Existing source-mode factory enrollment supplies a canonical query-only
lease only when the gateway is HandlerRoutedEventStoreGatewayClient; the older
CapturingEventStoreGatewayClient transport fixtures have no catalog. This was
hidden by the older package profile and the source lane's four selected real
handler proofs. Log `/tmp/tenants-published-catalog-api-http-tests.log`.

No HTTP fixture, catalog authority, authentication expectation or production
policy was changed to conceal the failure. Parent must triage the bounded test
enrollment correction, preserving real generated-host declaration/lease checks
and all existing gateway payload/status assertions before owner CI/release.


Parent bounded continuation: extend only the existing generated HTTP transport
fixture to its capturing gateway. Reuse actual owner commands/queries, the
public Development idempotency binder and canonical schema-two snapshot/lease
pattern already used for real handlers. Keep all47 existing assertions unchanged;
add missing-acquisition and undeclared-command HTTP refusals before any stub
submission. Distributed activation/schema-three surface policy remain with the
Platform runner, not simulated by these transport fixtures. No production
authority, filter, authentication or operation policy changes.


### Generated HTTP fixture correction

The bounded correction reuses the existing canonical schema-two snapshot and
leased authority helper. Capturing-gateway tests now include exact owner commands
and queries plus all12 metadata-only Development idempotency bindings from the
public SDK. The real GeneratedRouteCatalogFilter still resolves and compares
compiled declarations and requires command admission before reaching the stub.
The existing real-handler fixtures retain their query-only snapshot. No old
assertion changes. Two new actual HTTP controls refuse unavailable acquisition
and a command missing from a valid query-only snapshot before any gateway call.
Full class now passes49/49 (all47 earlier cases plus both refusal controls),
zero failures/skips, in2.884s.

The new `published-catalog-http` CI job builds the same minimal package graph in
Release against explicit EventStore3.129.0, using only root-declared Builds and
Platform inputs. It executes this complete class with minimum49 and fail-skips.
Existing source-reference and three frozen compatibility jobs remain enabled.
The normal package artifact tests remain unconditional in the existing unit lane.


Parent runtime identity continuation: the API observation callback previously
passed logical `tenants-api`, which differs from a run-scoped physical Dapr ID.
When private bootstrap is explicitly configured, require the runner's nonempty
EventStore:DomainService:AppId and pass that exact value to SDK capture. Without
a private directory the existing inert behavior remains. Add missing/blank
configuration refusals and a real SDK pre-activation observation proving logical
HostId and physical AppId remain distinct; this does not claim catalog activation
or validate a fabricated production trust policy. The published HTTP CI class
now requires53 total cases.


Final identity/HTTP proof: the actual API assembly is launched in four owned child
processes, using immutable startup arguments and the built test runtime/dependency
files. Missing, empty and whitespace physical app IDs each exit with the explicit
configuration diagnostic before any observation. A distinct suffixed app ID is
retained in the real SDK's pre-activation observation alongside logical HostId.
That process waits for a coordinator activation which is never supplied; no
activation/readiness/commit or backend persistence is claimed. All child processes
are reaped and their temporary directories deleted. Late WebApplicationFactory
configuration cannot drive these startup callbacks, so the real child process
proof replaces the initial four unsuccessful late-config attempts.

The complete new package-mode class passes53/53, zero failures/skips, in5.479s.
The legacy3.128.4 build and complete existing HTTP class pass47/47, zero skips.
Normal catalog3/3, governance32/32 and five-package PE/SDK-dependency verification
remain passing on the coherenta22 catalog. The workflow's exact Microsoft.Testing.Platform
command is separately checked with minimum53/fail-skips before review handoff.

The exact new workflow MTP command passes53total/53succeeded/0failed/0skipped
in8.023s. Log `/tmp/tenants-published-catalog-ci-http-tests.log`. Final legacy
HTTP class passes47/47 in2.231s after the identity change. No code remains
unverified by its affected focused checks; independent reviews and owner hosted
CI/release remain pending.


## Review Triage Log

All three independent layers completed before this triage. Edge returned no findings.

| Finding | Verdict | Evidence / route |
|---|---|---|
| Blind1 ordinary catalog services/activation | medium | API currently registers common authority/filter/readiness only through private Development bootstrap. With no directory, new generated callbacks refuse while normal readiness can remain green. Register common SDK catalog services when available and test the actual unenrolled host refuses and reports unready; document the mandatory trusted-generation requirement. Existing public SDK enrollment seams remain authoritative; do not invent Production trust or auto-activate. Patch. |
| Blind2 pack property vs actual PE | high | A legacy build followed by normal-SDK `pack --no-build` can satisfy evaluated availability while reusing unenrolled DLLs. Verify the actual Contracts/Server pack-target PE metadata before archive publication, with a build-legacy/pack-normal rejection control. Patch. |
| Blind3 equivalent NuGet version disables capability | medium | Existing package-floor tests support metadata suffixes, but the new exact string condition omits those same-binary selections. Normalize the finite supported SDK selections while retaining prerelease/unknown refusal; qualify the forthcoming SDK bug-fix patch separately as needed. Patch. |
| Blind4 actual package consumer absent | medium | Current unconditional metadata test follows project references; manual five-archive PE proof exists, but checked CI does not restore and discover declarations/adapters from those actual archives. Add the bounded package-only offline consumer to the release/CI artifact path. Patch. |
| Blind5 admission control removes route too | medium | Query-only snapshot proves undeclared command refusal before admission lookup. Keep/name that control accurately and separately remove/mismatch command admission with the command route present, requiring refusal before the capturing transport. Patch. |
| Blind6 schema-three ingress/eligibility HTTP gap | medium | Existing transport fixtures deliberately use schema two; the new public owner operation metadata is therefore not exercised by their callback. Add actual owner-operation schema-three positive and internal/ineligible negative HTTP cases, retaining legacy transport assertions. Patch. |
| Blind7 self-derived complete inventory | medium | Reflected source and expected values both derive from the same provider; removing a computed internal query can escape current public-operation counts. Compare with an independent approved full route inventory, including computed queries/projections. Patch. |
| Gap1 pack lifecycle hookup | medium | Preverified: direct-target tests survive deletion of BeforeTargets=GenerateNuspec. Add ordinary pack lifecycle refusal and require no archive output; covered with Blind2's actual-PE guard controls. Patch. |
| Gap2 emitted API operation payload | medium | Preverified: current actual-worker observation asserts identities only; dropping Operations from the callback leaves it passing although schema-three activation must refuse. Assert emitted declarations/operations against exact compiled inventories/digests. Patch. |

Scope extension for these bounded corrections: common API SDK registration/readiness tests, actual compiled-artifact pack guard/tool and actual package-only metadata consumer, version normalization, independent full route identities and additional existing HTTP/worker/publishing-governance tests. Preserve real filters/leases, all existing assertions and legacy profiles. No new Production trust policy or broad eligibility grant. The latest SDK's mandatory catalog is a runtime migration requirement; artifact publication must not imply an unenrolled Production deployment is ready.


### Bounded review patches and focused evidence

- Blind1: API common SDK catalog authority/filter/readiness is registered before
  optional activation. Both actual unconfigured Development and Production
  processes report503 readiness and refuse generated queries. No Production
  trust implementation or automatic activation was added. README and breaking
  release semantics describe this migration requirement.
- Blind2/Gap1: ordinary pack lifecycle executes a Framework-only PE reader on
  the actual pack-target DLL and required sibling Contracts/Server DLLs. Actual
  legacy build followed by normal `pack --no-build --no-restore` fails for both
  core packages, leaving no archive. The guard does not rely on evaluated flags.
  `test_catalog_artifact_guards.py`:1actual lifecycle test/2package controlsPASS,
  `/tmp/tenants-review-fixes-pack-lifecycle-tests.log`.
- Blind3: only stable SDK3.129.0 and the known3.129.1 patch normalize as catalog
  capable, including metadata suffixes and fourth-zero aliases. Unknown and
  prerelease catalog SDKs refuse native build/publication.19focused version
  selection/publication controlsPASS; actual3.129.1 package qualification waits
  for its public archives.
- Blind4: CI and credential-free release/recovery preparation restore actual
  Contracts/Server archives from an exact local source mapping into a fresh
  package cache. Verify source/version/SDK metadata, original/restored archive
  hashes, actual consumer output DLL hashes, then discover all22approved routes,
  all22operation schemas/digests and12adapters through the public SDK. Actual
  five-package pack/validation and isolated consumerPASS, receipt
  `/tmp/tenants-review-fixes-package-consumer.json` (local shape proof; no reviewed
  source SHA claimed until committed release preparation). Five early metadata
  source/version/SDK refusal testsPASS. Candidate script24and recovery43fixtures
  PASS with new required consumer routing.
- Blind5: accurately named undeclared-command control remains. Two separate
  malformed admission candidates keep the command route present but remove its
  admission or change its joined domain. The real canonical snapshot constructor
  rejects the idempotency facet during acquisition; actual HTTP returns503 and
  the capturing gateway receives no call.
- Blind6: actual owner schema-three operation contracts/digests admit signed
  agent/UI eligible queries and reject absent/agent/service callers from UI-only
  queries before forwarding. Internal bootstrap metadata remains ineligible
  under the shared policy, is absent from generated endpoint inventory and is
  rejected by HTTP404 without a gateway call. Legacy schema-two assertions stay.
- Blind7: complete catalog route IDs now compare with a separate approved22-ID
  inventory including both computed internal queries and both projections; the
  package-only consumer checks this same independently authored inventory.
- Gap2: the real API worker observation now compares every serialized declaration
  and operation digest with compiled owners, and independently rehashes each
  emitted operation. Dropping callback Operations cannot pass.
- Revised SDK3.129.0 generated HTTP class:63/63PASS, zero errors/skips,6.277s.
  `/tmp/tenants-review-fixes-http-tests.log`. Catalog3/3 and finite version19/19
  PASS. Builds have zero warnings/errors. No original producer output build or
  index mutation occurred. Parent owns full verification; no automatic new review
  loop is requested after these bounded patches.


Final explicit-selection qualification adds the AppHost Builds.Tooling edge to
File Scope. Its own authoritative catalog must not receive a Tenants runtime
SDK override. Preserve the removal on the direct reference and on the SDK's
metadata-free promoted transitive reference. Every runtime/test edge retains its
chosen SDK. The earlier actual override probe failed the tooling catalog; after
this bounded correction both full Integration builds succeed with zero warnings
and errors and both full63-case HTTP runs pass with zero skips:
`/tmp/tenants-review-fixes-explicit-sdk-build.log`,
`/tmp/tenants-review-fixes-explicit-sdk-http.log`,
`/tmp/tenants-review-fixes-metadata-variant-build.log`, and
`/tmp/tenants-review-fixes-metadata-variant-http.log`. The latter really rebuilds
with SDK3.129.0+metadata-with-dash before executing; it supersedes the initial
failed variant probe and its uncounted stale-DLL test run. A temporary recovery
fixture symlink caused duplicate lexical Builds identities; it was removed only
from the disposable copy. Original nested checkouts remain uninitialized.


### Concurrent upstream reconciliation

Before committing, origin advanced from original3aa to
fff6a5cb556ee0012974550c708eb7a871a25001 through four independent owner
commits. Fast-forwarded, preserved all unrelated API-image/release verification,
bootstrap diagnostics, source-lane improvements and owner schema-three authority.
Recorded the integration baseline above so this story's actual gitlink drift is
2186→a22; original6cc qualification history remains evidence of the initial
restore failure. The frozen legacy3.128.4 override remains explicit and cannot
publish artifacts. Preserve the upstream nested-owner floor boundary. The SDK
capability and actual artifact guards remain strict and finite.

The combined generated HTTP class retains all upstream cases, defaults to its
actual compiled schema-three owner authority and adds the reviewed malformed
admission/unenrolled-host/operation/observation controls. It passes66/66, zero
failures/skips,10.259s. Legacy schema-two helper is retained for focused route
and malformed-admission controls; all existing status/payload/auth assertions
remain. CI now requires at least66 cases with fail-skips enabled. Both affected
builds have zero warnings/errors. Final log:
`/tmp/tenants-review-fixes-concurrent-http-tests.log`.

All three independent reviews and all9triage rows are complete. Parent explicitly
authorized the bounded patches, upstream reconciliation and owner commit/push
after affected checks; no further automatic review loop is required. The exact
breaking candidate passed pinned commitlint with zero problems/warnings; log
`/tmp/tenants-published-catalog-commitlint.log`. Full hosted verification and
publication remain parent-owned.


Final bounded merge proof:59selected governance cases (including source identity,
legacy floor, finite SDK/publication selection, native/transitive tooling boundary
and retained release mappings) PASS, plus3catalog/metadata and24candidate
script cases. Logs `/tmp/tenants-review-fixes-concurrent-governance-tests.log`,
`/tmp/tenants-review-fixes-concurrent-catalog-tests.log`, and
`/tmp/tenants-review-fixes-concurrent-candidate-tests.log`. Ordinary stale-PE
pack lifecycle rerun passes2package refusal controls with no archive;5archive
metadata refusals also pass. Final proposed gitlink audit PASS. No new broad
verification run was performed; parent owns hosted CI and publication.


### Hosted CI follow-up: keep foreign generated packages owner-controlled

Exactb992 CI41200328508 job124534690515 and source run41200328264
job124534689801 exposed the publication guard applying to foreign Commons
Aspire/ServiceDefaults/UniqueIds packages. Those projects inherit Tenants targets
through the ordinary nested build/solution graph and correctly retain their own
SDK3.128.4 catalog. Scope the automatic GenerateNuspec guard to the same finite
five Tenants package names already used by its actual PE verifier. Extract only
the SDK availability predicate to an explicit internal target for its existing
selector controls. The five owned package floors, actual Contracts/Server PE
enrollment guard, explicit legacy build support and source support remain.

Reproduced using exact Commons0604bcf5304fc09a569f7abbd8e5d1b9049c02b7
and qualified Buildsa22 ordinary archives inside a disposable nested layout,
with the actual Directory.Solution.targets forcing serial native builds. Original
b992 targets refuse all3 foreign build-generated packages. With only this guard
correction, the identical solution builds with zero warnings/errors and produces
all3 Commons archives. No foreign source changes or nested initialization. Logs:
`/tmp/tenants-real-commons-pack-boundary-before.log`,
`/tmp/tenants-real-commons-pack-boundary-after.log`, and
`/tmp/tenants-real-commons-pack-boundary-proof.log`.

The checked regression suite builds actual SDK fixture projects through the
solution target and verifies all3 foreign generated archives retain SDK3.128.4.
It also builds all5 owned legacy outputs, requires ordinary no-build packing to
refuse every legacy package with no archive, and retains both actual stale-PE
Contracts/Server refusals. Two tests/10 bounded package controls pass in15.581s,
log `/tmp/tenants-owner-pack-boundary-tests.log`. Parent authorized this precise
hosted-CI repair with no new review loop. No production artifact body or public
package inventory changes.


Final repair proof:19 SDK/publication selector cases pass with zero skips, log
`/tmp/tenants-owner-pack-boundary-selector-tests.log`. All5 normal SDK3.129.0
packages pass their unchanged actual-PE guards and archive validation, logs
`/tmp/tenants-owner-pack-boundary-normal-five-pack.log` and
`/tmp/tenants-owner-pack-boundary-normal-five-validation.log`. The affected
Contracts.Tests build has zero warnings/errors. Exact fix candidate passes the
pinned owner commitlint, log `/tmp/tenants-owner-pack-boundary-commitlint.log`.
No wider test run was performed for this bounded guard correction.


### Source-only assembly identity follow-up

Exact8a27567 source41201634020 job124538416728 now builds successfully
with zero warnings/errors. All4 retained real-handler cases then refuse to load
EventStore.Client3.129.0.0. The Tenants Client source edge exports its selected
SDK as Version, while other source edges retain the pinned SDK producer's own
3.128.4 catalog, rebuilding the shared output with a different assembly identity.
This is a source-only cohort mismatch, not a handler assertion failure.

Before source building, evaluate Version from the actual checked-out EventStore
Client producer and bind that value through GITHUB_ENV for every remaining
source build/test step. Do not invent a source version or change a package pin.
Normal package qualification stays SDK3.129.0 and frozen public compatibility
remains its explicit separate job profile. Existing four provenance and compiled
operation checks, counts, serial builds and fail-skips gates remain unchanged.
File Scope/List now include the source workflow.

Local exact source qualification uses all nine top-level root-declared repositories
at the owning Tenants gitlink SHAs in genuine detached disposable worktrees,
recorded in `/tmp/tenants-source-identity-detached-worktrees.tsv`. No nested
submodule initialization/update or original producer output build. Ordinary
archives first hit Memories' existing genuine-Git check; no fake Git markers or
guard bypass was used. Both resulting source builds have zero warnings/errors;
actual EventStore.Client PE identity is3.128.4.0, matching the evaluated pinned
producer. The exact4 real-handler cases pass4/4 (zero skips) in1.910s, and
compiled catalog cases pass2/2 (zero skips) in0.783s. Logs:
`/tmp/tenants-source-producer-coherent-build.log`,
`/tmp/tenants-source-producer-four-handler-tests.log`,
`/tmp/tenants-source-producer-catalog-build.log`,
`/tmp/tenants-source-producer-catalog-tests.log`, and
`/tmp/tenants-source-producer-actual-client-identity.txt`. The exact workflow
selection shell passes syntax validation and actually supplies the tested
producer binding. No wider tests were run for this workflow-only correction.


### Root B7 hosted public-artifact profile

Parent's combined artifact review found the native domain host's own source
Contracts/Server PE identities colliding with the Root's actual published owner
artifacts in the default AssemblyLoadContext. At the parent's explicit bounded
request, mirror the SDK hosted-test opt-in: only when
HexalithUsePublishedSdkForHostedTests=true, replace the nonpackable native
Tenants host's two existing own Contracts/Server project references with their
existing public CPM package references. HexalithTenantsVersion remains the exact
version authority. Existing SDK edges, normal native/default/publish behavior,
all public package projects and all production code bodies remain unchanged.
No API-host, trust, activation, grant or artifact reader changes.

Actual MSBuild evaluation covers default/false, package/source intent and
publishing configurations: all retain the native owner project references. The
explicit true profile removes exactly those two source edges, selects both exact
CPM6.0.0 packages without local versions, keeps the host nonpackable and leaves
every other SDK/dependency edge unchanged. Checked published-catalog CI runs the
new2-test/10-configuration regression suite; log
`/tmp/tenants-hosted-package-profile-tests.log` (PASS in4.507s).

A disposable host build against the actual current public Contracts/Server5.11.2
and SDK3.129.0 succeeds with zero warnings/errors in1.31s. Its assets prove
both owner dependencies are packages, with no owner project dependency. Logs
`/tmp/tenants-hosted-public-profile-build.log` and
`/tmp/tenants-hosted-public-profile-assets.json`. This is ABI/profile proof only;
5.11.2 lacks compiled catalog enrollment and cannot qualify Root's actual6.0.0
catalog. Parent owns the mandatory future6.0.0 public-PE runtime qualification.


### Complete recovery-publication harness routing

Final67d9 CI41203800517 has green published-catalog, blocking HTTP,
authenticated live routes, source-reference41203800132, CodeQL and story guards.
The shared job builds successfully and every shard except Contracts.Tests passes.
Its138 failures all come from the unchanged strict unexpected-command assertion
in PartialReleasePublicationTests: this C# recovery harness was omitted when the
new required actual catalog-consumer command was added to recovery preparation.
Python candidate/recovery harness adapters had been updated; this separate C#
harness adapter was still missing. Production validation correctly remained
mandatory and did not fail open.

Update only the isolated C# publication tool adapter to accept exactly the new
consumer script/path, six arguments, expected reviewed source and receipt output,
and require RepositoryCommit to match that reviewed source. Record the new gate
in exact expected call ordering and the receipt in exact state-file inventories.
All existing source/hash/main/destination/refusal assertions remain. Add5 cases
for main advancement during the new preparation gate and7 cases requiring catalog
qualification failure to stop all recovery publication inventories before effects.
Actual production scripts, validators, public artifacts and runtime code are
unchanged.

All142 existing class cases plus12 new cases pass154/154, zero errors/failures/
skips, in148.600s. Affected Contracts.Tests build has zero warnings/errors. Logs
`/tmp/tenants-recovery-consumer-fixture-build.log` and
`/tmp/tenants-recovery-consumer-fixture-tests.log`. The first archive-only fixture
probe lacked the existing helper's required Builds checkout; final proof uses
the already recorded genuine detached exact source worktrees and bypasses no
guard. Only this affected class was executed; no broader rerun.
