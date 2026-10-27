# Read and recovery contract qualification — 2026-10-26

Implementation baseline: `cc0efc7df3399f1ea0e3789033ee75926bd06b35`.
Selected records: DW-114, DW-137, DW-144, DW-514 and DW-523.
This is an implementation/validation record; the DW-137 identity-policy decision, independent review and ledger acceptance are pending. No commits or pushes have run.

[Raw validation records](evidence/dw-114-137-144-514-523/validation-records.tar.gz) retain the exact command metadata, outputs, initial failures and controls; the [manifest](evidence/dw-114-137-144-514-523/manifest.json) binds their bytes. AppHost startup logs containing dashboard access tokens and private certificate material are excluded.

## Implemented contracts

- Memories reports the raw Redis match total and applies the requested window before omitting invalid documents. The owning Redis integration theory removes `sourceUri` or `sourceType` from a complete first window and verifies later windows, exhausted paging and unchanged totals.
- The explicit `X-Hexalith-Route-Encoding: base64url-v1` discriminator enables strict canonical UTF-8/base64url route segments. Generated queries decode before tenant resolution; missing headers preserve literal identifiers. Invalid encodings and duplicate/unknown discriminators fail before gateway dispatch. The BFF encodes the four parameterized reads.
- The BFF chooses the injected sidecar endpoint ahead of legacy configuration, preserves bearer/conditional headers and response authority, and uses the shared innermost Dapr routing handler. Dedicated API and UI configurations retain caller/verb boundaries. The named zero-retry app target bypasses built-in service retries. Platform's existing Sentry/control-plane bootstrap now supplies the connected local graph through a reusable generated-sidecar binding and deferred event subscriber.
- Recovery uses the same MessageId fallback for null/blank causation as admission. Persisted drain range, idempotency identity, publication index, reminder ownership, pending count and eventual completion are asserted.
- Status endpoints advertise `Retry-After: 1` for `PublishFailed` only when `Retryable == true`. False/null retain no advertised interval; null remains unknown. Enum/replay compatibility and command/projection/audit truth stay distinct.

## Completed focused checks

| Lane | Result | Scope |
| --- | --- | --- |
| Memories original Redis search class | 19/19, zero skipped | Disposable baseline source mirror with the two owned file overlays; real pinned Redis fixture |
| EventStore codec, maintained MTP | 25/25, zero skipped | Canonicality, Unicode, separators, dots, percent strings and literal collisions |
| EventStore generator, maintained MTP | 139/139, zero skipped | Generated runtime behavior, decoding order, invalid/multiple headers and literal compatibility |
| BFF original REST client class | 129/129 | Changed production source, original test file and stable unchanged DTO outputs |
| BFF composition/authentication | 6/6 | Changed production registration, routing/token ownership, precedence and no direct fallback |
| AppHost resource model and shared identity binding | 15/15, zero skipped | Debug/package model; includes missing-runtime refusal and generated mTLS binding |
| EventStore recovery/status/provenance | 123/123, zero skipped | Isolated final source artifacts |
| Full EventStore Server | 5,871 passed, zero failed, 25 existing skips | 5,896 total; identical final isolated outputs placed at the standard repository-relative depth |
| Controlled real Dapr mTLS | 11/11 | Six GETs, bearer forwarding, 304 metadata, other caller/verb/path denial and one backend call on 503 |
| Ordinary local AppHost mTLS chain | 9/9 | Six literal-compatible live reads through the real sidecar/API/backend; wrong caller403, wrong verb403, missing bearer401 |

The controlled Dapr lane uses Dapr/Sentry 1.18.7 and HTTPS app-channel transport. It resolves the templates with the actual self-hosted workload trust domain `public` and a test namespace. The synthetic receiver has a unique discovery alias to avoid other AppHosts; the authorized caller remains `tenants-ui`. This qualifies transport and policy behavior, not a Kubernetes deployment or a live persisted-domain read.

The retry rejection control changes only the temporary named policy from zero to three retries. The same 503 check observes four backend calls and fails; the remaining ten checks pass. Neither tracked policy file is mutated. Dapr's actual trie requires middle `/**` segments for the membership route. Matching path prefixes also admit descendants; the generated API owns its action inventory, and no stronger Dapr-only path guarantee is claimed.

DW-161 was already accepted against published EventStore3.128.2 at this baseline. Its record and prior hosted acceptance are preserved; controlled 304 transport evidence does not requalify that historical packet.

The ordinary AppHost check first established literal compatibility. The actual current generated API then built successfully from real source in a disposable lane excluding only the unrelated, uncompilable Contracts/Routing directory. Its four encoded parameterized reads return200 for ordinary valid identifiers, and detail returns the exact persisted tenant identity; malformed discriminator/encoding controls return400. This socket proof supplements the generator, BFF and controlled-header checks.

**DW-137 remains incomplete:** live slash, backslash, dot-only and Unicode identifiers encounter the existing downstream `SubmitQueryRequestValidator`/`AggregateIdentity` security grammar. Valid encoding preserves their bytes but does not override that policy. Independent Unicode/percent probes returned400 validation refusals; an unstable follow-up also observed401/500 and is retained without attributing it to a codec defect. A user question is pending about extending shared identity semantics with collision-safe actor/key addressing versus preserving the restriction and substituting another item. No shared identity regex or physical key contract has been broadened, and this record does not claim end-to-end special-ID acceptance.

The shared Platform prerequisite is necessary because deny-default local ACLs cannot identify callers without mTLS. Its owning baseline is `eecea3fa467d27226ef6173c19f853add36bb26c`. The implementation links the existing shared helper into Tenants and preserves the existing Platform subscriber's diagnostics. It does not duplicate an independent Sentry implementation in the domain module. Ordinary startup exposed an event-ordering failure when binding happened before generated executables existed; the shared deferred subscriber resolves that ordering.

## Exact parent-run commands

Executed from the owning EventStore repository:

```bash
dotnet build tests/Hexalith.EventStore.Contracts.Tests/Hexalith.EventStore.Contracts.Tests.csproj -c Debug -m:1 --no-restore -p:UseHexalithProjectReferences=true -p:HexalithEventStoreVersion=3.128.2 -p:HexalithTenantsFromSource=false -v:q
dotnet test --project tests/Hexalith.EventStore.Contracts.Tests/Hexalith.EventStore.Contracts.Tests.csproj -c Debug --no-build --no-restore --filter-class '*RestRouteIdentifierCodecTests'
dotnet build tests/Hexalith.EventStore.RestApi.Generators.Tests/Hexalith.EventStore.RestApi.Generators.Tests.csproj -c Debug -m:1 --artifacts-path /tmp/dw-eventstore-generator-final-artifacts -p:UseHexalithProjectReferences=false -p:Version=3.128.2 -p:HexalithEventStoreVersion=3.128.2 -p:HexalithTenantsFromSource=false -v:q
dotnet test --project tests/Hexalith.EventStore.RestApi.Generators.Tests/Hexalith.EventStore.RestApi.Generators.Tests.csproj -c Debug --no-build --no-restore --artifacts-path /tmp/dw-eventstore-generator-final-artifacts
```

The generator's final isolated lane compiles the changed EventStore projects from source while consuming unchanged shared libraries as packages. This avoids another session changing common project-output identities during validation.

Executed against the disposable Memories mirror:

```bash
dotnet build /tmp/dw114-memories-validation/mirror/tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj -c Debug -m:1 --no-restore -p:UseHexalithProjectReferences=false -p:MinVerVersionOverride=1.0.0 -p:CustomAfterMicrosoftCommonTargets=/tmp/dw114-memories-validation/focus.targets -v:q
dotnet test --project tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj -c Debug --no-build --no-restore --filter-class '*SyntacticSearchIntegrationTests'
python3 /tmp/dw144-dapr-validation/qualify.py
DW144_MUTATE_RETRIES=1 python3 /tmp/dw144-dapr-validation/qualify.py
```

The last command intentionally exits 1 and rejects the retry mutation. The Redis mirror starts at Memories `cdd73d7361f67781a523f63f04853491a687f426`, overlays the two owned files, and makes one explicit baseline-only compilation repair: fully qualify `TenantIngestionOwnership` in the disposable provisioning activity. It excludes the unrelated uncompilable `InventoryWirePaginationIntegrationTests.cs` from compilation. The actual search class, production search/indexing code, factory and Redis fixture are unchanged. The maintained project-level command runs all 19 search cases; this is focused evidence, not a passing broad Integration gate.

## Initial failures and broader limits

- Original Memories source build: `EventStoreMemoriesCommandStore.cs` CS0246, missing `IEventStoreGatewayBoundCommandProofSource` due mixed source/package dependencies. Debug/package build then exposes the existing `ProvisionRediSearchActivity.cs:44` namespace collision. After its external qualification repair, the broad Integration project exposes `InventoryWirePaginationIntegrationTests.cs:56` CS7036 for the changed MemoriesClient constructor. No tracked source, dependency or test gate is weakened to hide those failures.
- Full Tenants UI/authenticated integration builds are blocked by baseline `TenantCatalogDeclarations.cs` referencing absent EventStore routing contracts, including `RouteContractDeclaration`, `CommandIntentDeclaration` and `IdempotencyDescriptorField`. Current adopted EventStore source has no corresponding definitions. Focused BFF lanes use disclosed stable DTO dependencies; they do not establish a green broad build or package publication readiness.
- Initial codec test compilation rejected a pattern expression inside Shouldly's expression tree; its predicate was corrected and all 25 cases pass.
- Concurrent builds changed shared assembly identities. Initial recovery/status execution had 51 failures; a consistent rebuild passes 113 and the final extended isolated lane passes 123. A later generator build encountered duplicate Commons.UniqueIds identities; the final isolated package-dependency build passes all 139 generator cases.
- Full EventStore Contracts MTP run: 1,268 total, 1,265 passed, one failed, two skipped. The existing packaging governance test cannot establish ancestry from approved Builds execution `22a578b576a515d2af214fe81859447fffc97981` to gitlink `db897eac9f2819220b32d237109d7255218b46a9` (Git exit128). No nested submodule was initialized to clear this hold.
- Initial full Server run: 5,893 total, 5,861 passed, seven port5000 bind failures, 25 skipped. The seven cases subsequently pass in the 123-case focused lane. Full out-of-tree artifacts then expose fixture-root assumptions (197 failures), and the first root-aware copy had one fewer directory level than the standard output layout (103 failures). Copying byte-identical isolated outputs to `tests/Hexalith.EventStore.Server.Tests/bin/dw-five-output/net10.0` restores both ascent-based and fixed-depth fixture discovery. Final full execution passes 5,871, fails zero and retains 25 pre-existing skips across 5,896 cases (`/tmp/dw-eventstore-server-root-depth-tests.log`).
- A diagnostic MTP `-- --help` invocation hit a CLI help/IPC failure. The supported `--filter-class` invocation runs successfully and supplies the acceptance evidence.
- The owning Platform.Composition Debug/package build reaches two pre-existing CA1062 errors in unchanged `PlatformHostedPublishing.cs`. The linked shared helper/subscriber compiles in the AppHost, its15 model/binding checks pass, and the real mTLS topology passes its read/denial controls. The broad Platform gate remains visibly blocked.

Existing runtime, historical provenance, assistive-technology and release holds remain. No release workflow, package publication, business-data mutation or deployment is performed by this task.
