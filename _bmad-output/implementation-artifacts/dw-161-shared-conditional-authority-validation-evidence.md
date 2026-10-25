# DW-161 shared conditional authority validation

DW-161 is closed after clean hosted3.128.2 runtime acceptance and hosted Chromium verification of the responsive identity fixture. All five requested records are now done. The initial shared repair3.128.1 is published and passes the unchanged conditional contract locally; review found two remaining version-retention edges, corrected in EventStore935c21ac5ebc5e50738ae6ee79dc661acd2fd5a8. The four earlier closures remain unchanged. User approval explicitly authorized the EventStore repair and dependency update; no unrelated dependency, Dapr policy, TLS behavior or stored live state is changed.

The current reviewed shared source is EventStore `935c21ac5ebc5e50738ae6ee79dc661acd2fd5a8`. It extends the initial repair `656f2771761f48060657bb7485cd5f369f6b7b81`, which was based on `a5f1aaa08cf466f26a06cf8719138043f22cfde7` after incorporating upstream documentation/dependency commits. Six source/test file hashes remained byte-identical through that rebase. The original Tenants gitlink was `4e905980a58c9641d800be062667cac24a50d0b3`.

## Shared behavior and review

Authenticated routing and handler execution precede conditional comparison. Strong opaque validators can match only when supplied by the actual projection producer; legacy fallback-only opaque validators remain misses. Existing policy, payload, provenance, mixed-projection, weak/wildcard and ten-value guards remain. The complete bounded list is validated before decoding/equality, and normalized comma-containing SDK validators are reusable.

Bodyless304 responses preserve independent producer version, served-at, freshness and warnings using existing headers. Version is never inferred from ETag. Header bounds and canonical lifecycle remain authoritative; actual multiple header values are rejected, while one opaque version containing commas is preserved. The follow-up retains200 with unchanged payload/JSON whenever a nonempty version cannot round-trip losslessly through the existing ASCII header, including Unicode, controls, oversized values and boundary whitespace. Null/empty versions retain existing conditional behavior. Warnings retain the existing sixteen-token/4096-character carrier; unrepresentable warning evidence retains200 with the body.

All three independent review layers completed. The continuation spec records every one of nine findings and its disposition. Eight verified findings, including one duplicate, were corrected. The comma-as-duplicate suggestion was rejected because the established version contract is opaque. The verification-gap layer reported no gaps.

## Initial shared repair checks (historical checkpoint)

| Check | Result | Skips |
| --- | --- | --- |
| Controller/authority boundaries |103/103 pass|0|
| Real Kestrel persistence/router/HTTP |4/4 pass|0|
| Gateway typed/untyped client |91/91 pass|0|
| Full REST generator project |124/124 pass|0|

All three affected test projects build in Debug/source with zero warnings/errors. No package fallback was needed for the shared patch. The Kestrel cases cover ASCII, opaque comma and Unicode versions, actual header emission, unchanged persisted evidence, then a legitimate isolated-fixture writer update that requires old-validator200 and new-validator304. They are controlled component fixtures; final Tenants acceptance still requires real command-seeded live state. Documentation lint and diff whitespace checks pass.

Shared builds use `dotnet build <test-project> --configuration Debug -p:UseHexalithProjectReferences=true -m:1 -nr:false`. Maintained MTP selections use the discovered controller103, persistence4 and client91 minimum counts with `--fail-skips on`; root ran the complete generator project with the same skip rejection,124 cases.

## Initial exact-source CI and release (completed historical checkpoint)

[EventStore CI40235128075](https://github.com/Hexalith/Hexalith.EventStore/actions/runs/40235128075) passed all six required jobs for the exact current main SHA. [Commitlint40235128103](https://github.com/Hexalith/Hexalith.EventStore/actions/runs/40235128103) also passed; CodeQL, payload-protection and advisory checks passed. Optional performance/Aspire jobs in that CI workflow were skipped and are not claimed as acceptance passes. The separate [live-sidecar integration40235127782](https://github.com/Hexalith/Hexalith.EventStore/actions/runs/40235127782) subsequently passed.

[Release40236350167](https://github.com/Hexalith/Hexalith.EventStore/actions/runs/40236350167) passed the exact-main proof and reached the configured production review. The authenticated account was the configured reviewer, and approval used the ordinary review endpoint after source qualification. The workflow revalidates main/CI before publication. Its publication manifest covers all fourteen packages plus the container. The actual3.128.1 publication is recorded below; its follow-up release must pass the same gates.

## Consumer source-mode limit

The required local source attempt was:

```sh
dotnet build tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj -c Debug -p:UseHexalithProjectReferences=true -p:UseNuGetDeps=false -p:HexalithFrontComposerFromSource=false -p:HexalithMemoriesFromSource=false -m:1 -nr:false
```

It failed with zero warnings/one error, CS0246 at `references/Hexalith.Memories/src/Hexalith.Memories.Server/EventStoreIntegration/EventStoreMemoriesCommandStore.cs:22`: `IEventStoreGatewayBoundCommandProofSource` is missing. This reproduces the existing mandatory source-reference CI failure, not only an optional runtime resource limitation. Baseline [Source-reference integration40218898554](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/40218898554/job/121245959556) failed the unconditional source graph build on692c17b27d94677a3a4d065330d150b262cdb570 with the same CS0246 at Memories line22; its provenance test was skipped and is not claimed as a pass. The tracked disposition remains the [prior validation hold](dw-104-161-432-436-594-validation-evidence.md). The local current-source attempt reproduces it. No live consumer test used partial outputs; no Memories dependency/source or source CI gate is changed.

## Conservation

The continuation baseline has657 IDs,609 done/48 open across43 maintenance rows. All656 unselected record bodies and index rows are unchanged, and DW-161 history is retained. Only a successful published-package/live conditional contract can close DW-161. Existing broader package-consumer, mandatory Memories source-reference build, optional Memories host, operational-validator and background-worker holds remain.


## Historical initial published repair (3.128.1)

[EventStore3.128.1](https://github.com/Hexalith/Hexalith.EventStore/releases/tag/v3.128.1) published at2026-10-25T21:47:53Z. Release40236350167 completed successfully, including both source-proof and publication jobs. The fetched tag resolves exactly to656f2771761f48060657bb7485cd5f369f6b7b81, the reviewed source with green required CI/commitlint. Normal configured production review was satisfied; no release protection or failed gate was changed. Tenants now pins that actual version before its Builds imports so all consumed EventStore packages resolve consistently, and its declared gitlink targets the same source. Published-package consumer qualification is in progress; DW-161 remains open.


## Consumer compatibility and local package evidence

The published-package build first failed four CS0618 references in AuditStreamPersistenceTests. The upstream constant explicitly preserves actor-response-mismatch under the new ResponseMismatch name; replacing those four obsolete references yields a zero-warning/error build and audit117/117 plus generated HTTP44/44 passes.

The first full UI package run found285 fixture DI failures. The earlier7706 proof used source FrontComposer. Its published4.7.0 package exposes NavigationFailureNotifier and production registration already supplies it, while six bUnit setups gated it on source-only HFC_ROUTE_OPTIONS. Removing just those six guard pairs preserves lifetimes, localization, production behavior and actual route-options guards; the six classes pass832/832. This package profile legitimately has7705 tests because one route-options test remains source-only. The next full run had one NullReferenceException in the existing CancellationToken.WaitHandle disposal observer; an isolated rerun and the complete7705/7705 rerun passed, zero skips. The first isolated command set an incorrect minimum24 for six declared cases; its six tests passed but the policy correctly rejected the count. The corrected minimum6 passes. These failed attempts remain failures, not acceptance passes.

Local Debug/package live REST now passes4/4, including the unchanged conditional304 assertion with persisted producer version and valid served-at/lifecycle. Exact historical persisted-state passes1/1. Authenticated UI passes3/3 including browser25/25. One earlier UI invocation failed with FAIL_SKIP at prerequisite discovery; it did not qualify any scenario. Redis returned PONG and real Docker placement/scheduler ports50005/50006 were reachable. Using the harness's existing explicit endpoint variables produced the passing run, without restarting shared services or changing Dapr identity/policy. AppHost13/13 and TLS/bootstrap/private-evidence guards80/80 also pass. All acceptance runs use minimum counts and fail-skips. Hosted Release/package checks remain required before ledger closure.


## Follow-up review corrections

EventStore935c21ac5ebc5e50738ae6ee79dc661acd2fd5a8 fixes lossless producer-version retention. Controller111/111 and real Kestrel7/7 pass with zero skips; the Debug/source build has zero warnings/errors. Tests preserve persisted evidence and assert legitimate fixture state advancement (old tag200/new tag304). This is the reviewed follow-up to Consumer Blind1/2; the completed exact-source publication is recorded below.

The one UI observer NullReference was traced to a concurrent CancellationTokenSource.WaitHandle getter and Dispose: the getter accesses the kernel-event field after Dispose can clear it. See [the exact runtime source](https://github.com/dotnet/runtime/blob/v10.0.13/src/libraries/System.Private.CoreLib/src/System/Threading/CancellationTokenSource.cs#L90) and [the documented Dispose concurrency boundary](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtokensource?view=net-10.0#thread-safety). Both test helpers now poll IsClosed on a borrowed handle captured before the controlled fake can finish; all eight observations retain the original cancellation/authority/ownership/render/status assertions and bounded deadlines. No borrowed handle is disposed and no exception is swallowed. The affected class passes475/475 with zero skips after a zero-warning/error Debug/package build.


## Reproduction commands and evidence boundaries

Local builds use Debug; hosted consumer qualification uses the existing Release/package job. The package fallback flags are `-p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true -p:HexalithFrontComposerFromSource=false -p:HexalithMemoriesFromSource=false -warnaserror -m:1 -nr:false`. Build these projects individually with `dotnet build <project> --configuration Debug` and those flags: EventStore main, Admin.Server.Host, Admin.UI, Tenants.Sample, Tenants.AppHost.Tests, Tenants.IntegrationTests and Tenants.UI.Tests. For the three EventStore-host builds, also pass `-p:HexalithEventStoreVersion=<actual consumed version>` so their independent owning build properties agree with the Tenants pin.

After builds, invoke each maintained MTP selection as follows (project is IntegrationTests unless specified):

```sh
dotnet test <project.csproj> --configuration Debug --no-build --filter-class <fully-qualified-class> --minimum-expected-tests <count> --fail-skips on
```

| Selection (under Hexalith.Tenants.IntegrationTests) | Minimum |
| --- | --- |
| AuthenticatedTenantsRouteTests |4|
| AuthenticatedTenantsUiRouteTests |3|
| TenantsUiRouteSmokeTests |6|
| TenantsApiGeneratedControllerTests |44|
| AuditStreamPersistenceTests |117|
| AspireTopologyTlsDiagnosticsTests |5|
| AspireTopologyBootstrapTests |72|
| AuthenticatedRouteEvidenceTests |3|
| AppHostSecurityResourceGraphTests (Hexalith.Tenants.AppHost.Tests project/namespace) |12|

The historical selection uses `--filter-method Hexalith.Tenants.IntegrationTests.AspireTopologyTests.Generated_tenants_api_get_tenant_reads_verified_redis_state_with_projection_authority --minimum-expected-tests 1`. Full UI uses the UI.Tests project, no class filter and minimum7705. The package profile excludes the single source-only route-options test; actual route-options guards remain.

For live tests in this existing local topology, set `EventStore__Projections__WorkersEnabled=false`, `HEXALITH_EVENTSTORE_TEST_PLACEMENT_PORT=50005`, `HEXALITH_EVENTSTORE_TEST_SCHEDULER_PORT=50006` and `SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust:/usr/lib/ssl/certs"`. These are supported fixture endpoint/worker controls; the hosted cold-bootstrap job uses its own fresh topology and does not reuse local state. The browser tool dependency remains the tracked npm lockfile and Chromium installation in tests/performance/tenant-audit.

Safe browser packets bind the on-disk UI/Shell binaries, validator and Tenants/FrontComposer working sources. They do not claim to hash running backend binaries or all dependency working sources. Backend conditional/version/persistence acceptance is supplied independently by the actual command-seeded REST and historical integration results. Final published-package assets/deps hashes and retained safe packets will be recorded with those final results.


## Follow-up exact-source publication gate

[EventStore CI40240634704](https://github.com/Hexalith/Hexalith.EventStore/actions/runs/40240634704) passed all six required jobs for935c21ac5ebc5e50738ae6ee79dc661acd2fd5a8. [Commitlint40240634709](https://github.com/Hexalith/Hexalith.EventStore/actions/runs/40240634709) passed for that same source. Optional Aspire/performance CI jobs were skipped and are not acceptance evidence. The root dispatched [release40242204827](https://github.com/Hexalith/Hexalith.EventStore/actions/runs/40242204827) only after these results and verifying the live main tip still matched.


## Published final repair

[EventStore3.128.2](https://github.com/Hexalith/Hexalith.EventStore/releases/tag/v3.128.2) published at2026-10-25T23:03:01Z. [Release40242204827](https://github.com/Hexalith/Hexalith.EventStore/actions/runs/40242204827) passed both exact-main verification and full package/container publication. The fetched tag resolves exactly to935c21ac5ebc5e50738ae6ee79dc661acd2fd5a8, the reviewed follow-up source. The configured reviewer jpiquot used the ordinary production-review endpoint, deployment7320382201, after successful source CI/commitlint. No protection, freeze or failed release gate changed. Tenants now pins actual3.128.2 and the same source gitlink; unrelated pins remain unchanged.


### Registry-index delay and local published-asset fallback

The first final consumer build and explicit `dotnet restore samples/Hexalith.Tenants.Sample/Hexalith.Tenants.Sample.csproj --no-http-cache` with the package flags failed NU1102: nuget.org still advertised nearest3.128.1. The successful release log records all fourteen pushes accepted (HTTP Created / Your package was pushed at22:59Z), but the flat-container index and direct Contracts3.128.2 URL still returned the old list/404. These restore attempts are failures, not acceptance passes.

Local verification therefore uses only the actual public [3.128.2 GitHub release](https://github.com/Hexalith/Hexalith.EventStore/releases/tag/v3.128.2) package assets while NuGet indexes them. Exact preparation: `gh release download v3.128.2 -R Hexalith/Hexalith.EventStore --pattern '*.nupkg' --dir /tmp/tenants-five-routes.WK5uRZ/eventstore-3.128.2`; then add `-p:RestoreAdditionalProjectSources=/tmp/tenants-five-routes.WK5uRZ/eventstore-3.128.2` to the seven individual Debug/package builds above. This command-line-only source does not change repository feeds. The local receipt records the downloaded assets and resolved graph. Final hosted Release/package qualification must restore through the unchanged normal NuGet configuration on a clean runner; local cache/assets alone cannot close DW-161.


NuGet indexing subsequently completed. The retained [package-content comparison](dw-161-published-package-comparison.json) downloads all fourteen public flat-container packages and compares every non-signature archive entry byte for byte against the official release assets:14/14 equal. The [local build receipt](dw-161-consumer-binding-local.json) records all seven project graphs, actual package versions and output hashes; all resolved EventStore package references are3.128.2 (ten in IntegrationTests). The three EventStore-owned hosts compile their own internal projects from the released source rather than package references; the receipt states zero EventStore package references for those owner graphs and records their DLL hashes. The Tenants consumer projects use the released packages.

The final AppHost graph-only selection was initially invoked with minimum13, but that class contains12 tests; all12 passed while the policy correctly failed the invocation (exit9). The established full AppHost project contains13. The corrected final full-project invocation uses no class filter and minimum13; the hosted graph-only lane retains its existing minimum12. No test/count gate is lowered below its discovered selection.


## Final local3.128.2 consumer qualification

All seven Debug/package project builds pass with zero warnings/errors. The final maintained selections pass with zero failures/skips: REST4/4 (unchanged conditional304 plus independent persisted version/time/lifecycle), authenticated UI3/3, historical persisted state1/1, anonymous6/6, generated HTTP44/44, audit117/117, TLS5/5, bootstrap72/72, private evidence3/3, full AppHost13/13 and full UI7705/7705. The [retained local browser packet](dw-161-live-browser-local.json) passes all25 physical cases, started2026-10-25T23:08:22.933Z. Its source-diff, validator and UI/Shell binary hashes match the final local source/build receipt; source-diff hashing trims UTF-8 git output to match the browser and retains the raw-byte hash separately. It does not claim to hash running backend binaries.

All three consumer review layers completed before triage. Consumer Blind1/2 are fixed by the published lossless-version fallback,3 by retained safe evidence/commands/package binding,5 by the exact mandatory baseline/current source-build hold,6 by the diagnosed and verified observer repair, and7 by current evidence plus labeled historical checkpoints. Finding4 remains rejected as a premature-closure claim: DW-161 remains open and requires the reviewed candidate's hosted normal-NuGet Release/package results. No additional work is deferred and no unrelated record or dependency changes.


## Hosted candidate and broader-gate disposition

Consumer implementationa569b48531d1567a12baab4d423f5426ce5c9c25 is pushed to main. [Hosted CI40243563692](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/40243563692/job/121320312950) passes the complete normal-NuGet Release/package HTTP lane:44 generated-route,18 real status,35 protected project,31 replay and117 audit cases (245 total), all zero failures/skips. Commitlint40243563921 passes. The live job remains pending at this checkpoint.

The same candidate's mandatory [Source-reference integration40243563553](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/40243563553/job/121320312601) reproduces the exact baseline Memories CS0246 at line22; its provenance test is skipped and unqualified. The broad package-consumer job fails its existing Contracts range check: expected[5.10.1,), found[0.0.0-ci-test,). Its dependent broad Aspire/performance jobs are skipped and unqualified. No source/coverage/package gate is changed.

[Story Guards40243563628](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/40243563628) passes this change's declared gitlinks, but reports three failures. Release-artifact fixtures fail38 cases because the existing shared OCI validator requires --created; provenance fixtures error in two temporary-dependency Git commits (exit128). These same steps already fail on both the original8804 baseline [40191830627](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/40191830627) and continuation692c baseline [40218898564](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/40218898564). Both story files also pass the direct local validate-story-gitlinks.py check; a local changed-story wrapper without a GitHub event correctly refuses its missing GITHUB_EVENT_PATH prerequisite.

The Chromium failure requires correction: it passed at the original8804 baseline, then the responsive repair60f9 added a second tenant audit launcher per identity. The unchanged HTML still requires four raw launchers instead of eight, selects a hidden responsive launcher as its expected focus and can mutate to another copy of the same identity. This is a stale test fixture, not production link loss. The same implementation agent is correcting only the HTML while preserving raw/visible counts, both-copy equivalence, all12 audit returns, actual clicks, guards, detail cases and wrong-row rejection. The continuation692c baseline's matching failure is recorded historically, not treated as proof that the original batch did not cause it.


### Corrected responsive identity fixture

The HTML-only patch now requires eight raw tenant links/four raw membership links and four distinct visible identities at measured1024px. Each tenant's desktop/mobile pair must carry equal href/return identity. Clicks and expected focus use the visible launcher, and the negative control must select another visible identity. All12 audit returns,4 detail returns,33 exotic identity anchors, exact focus/rerender/lookup guards and wrong-row rejection pass. No production source, waits or deadlines changed.

Exact local command: `CHROMIUM_BIN=/usr/bin/google-chrome-stable TENANTS_BROWSER_BUILD_CONFIGURATION=Debug tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh`. Exit0, Google Chrome154.0.8037.57, HTML SHA-256 `acad9475bfb3a2e6ce265cbe88cdf725893cca35378ab3aa67cbaf1fdc661058`. The full existing guard, including its other forced-color/layout/control checks, passes. This is rendered Fluent fixture evidence; authenticated backend/browser acceptance remains the separate live packet. The original8804 Chromium success and subsequent failure are preserved above; this regression is repaired rather than called an unrelated pre-existing limitation.


## Hosted published-package acceptance

[Live job121320313059](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/40243563692/job/121320313059) passes on exact consumera569b48531d1567a12baab4d423f5426ce5c9c25. Its clean runner restores normal NuGet packages and builds Release outputs. Every selected suite passes with zero failures/skips: app-channel12, TLS5, bootstrap72, fresh cold-bootstrap REST4, private evidence3, historical persisted state1, anonymous6 and authenticated UI3 (106 total). The separate HTTP job passes245 cases, zero skips. This includes the unchanged conditional304 assertion with persisted independent producer version/time/lifecycle. The first live fixture explicitly requires newly committed cold bootstrap and its projected sequence1 authority.

The [retained hosted browser packet](dw-161-live-browser-hosted.json), started2026-10-25T23:23:44.635Z, passes all25 cases. Its sourceRevision is exactlya569b48531d1567a12baab4d423f5426ce5c9c25, validator hash matches the maintained script, and source, untracked source, FrontComposer source/untracked and dependency-gitlink captures are all clean. Binding remains limited to the stated UI/Shell on-disk artifacts and sources; real backend/persistence behavior comes from the separate integration tests. The only subsequent implementation edit is the HTML fixture correction above; no production/configuration/dependency file differs from this runtime-qualified consumer. Hosted Chrome153 confirmation of that fixture remains required before final batch closure.


## Final closure (2026-10-26)

[Hosted Chrome153 job121323924630](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/40244779370/job/121323924630) passes the corrected identity fixture at exact follow-up ec3fd76d7dc15a77414008b66a5bab5b7c2380f5. The full existing guard retains raw/visible counts, responsive-copy equivalence, all12 actual audit returns,4 detail returns and wrong-row rejection. Only HTML test markup and evidence differ from the already runtime-qualified a569 consumer; a Git comparison confirms production/configuration/dependencies are unchanged. The old Chromium regression is repaired. The other two Story Guard jobs retain the separately verified original-baseline failures and are not claimed green.

The separate [EventStore live-sidecar integration40240634580](https://github.com/Hexalith/Hexalith.EventStore/actions/runs/40240634580) also completed successfully for exact935c21ac5ebc5e50738ae6ee79dc661acd2fd5a8. All required shared release gates, consumer acceptance and new fixture checks are satisfied. DW-161 closes while DW-104/DW-432/DW-436/DW-594 remain closed. Both specs are done. Conservation checks require all657 IDs,610 done/47 open,42 exact open-index rows,656 unchanged other record bodies and all historical DW-161 fields. Broader baseline CI/runtime/release holds remain, and no gate or accepted limitation is removed.
