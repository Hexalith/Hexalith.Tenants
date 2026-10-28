# DW-122/DW-334/DW-358/DW-579/DW-595 qualification evidence

Date: 2026-10-28. Approved scope: [frozen spec](spec-dw-122-334-358-579-595-search-and-dependency-qualification.md). Implementation, dependency reconciliation, selected ledger updates and independent review are complete. The final results below supersede preserved earlier-pass history; validated owning publication is recorded after the checks.

## Final accepted checks after review patches

The final service preserves deterministic BM25/key windows, rejects incomplete raw identities before
filtering malformed fields, and uses existing recognized timeout/protocol exception types so plain,
scoped and hybrid callers retain HTTP 503, error-code and Retry-After behavior. Final real controls
cover selected-hash deletion with persisted-state assertions, advertised-total/returned-row
truncation, hydration-only warnings, RESP3 count-only paths and bounded real timeout establishment.
Normal queries on the same two 5,000-document corpora completed in 11 ms each during focused
verification; the existing concurrent latency smoke also passes. No new performance SLA is asserted.

| Final lane | Result | Exact output |
| --- | --- | --- |
| Debug/source integration build and self-contained standard fixtures | 82/82, zero failed/skipped/not-run; build zero warnings/errors | [Build](evidence/dw-122-334-358-579-595/final-integration-build.log), [run](evidence/dw-122-334-358-579-595/final-integration-test.log) |
| Debug/source Search namespace and complete changed endpoint class | 296/296, zero failed/skipped/not-run; build zero warnings/errors | [Build](evidence/dw-122-334-358-579-595/final-unit-build.log), [run](evidence/dw-122-334-358-579-595/final-unit-test.log) |
| Actual package AppHost model | 1/1; build zero warnings/errors; all required mode metadata correct | [Build](evidence/dw-122-334-358-579-595/final-apphost-build.log), [model](evidence/dw-122-334-358-579-595/final-apphost-model-test.log) |
| Owning server package-dependency build | Zero warnings/errors | [Build](evidence/dw-122-334-358-579-595/final-package-server-build.log) |

Commands are the documented project/assembly commands below. The final integration run omits the
native override and provisions its own pinned infrastructure. The final unit run adds
`-class Hexalith.Memories.Server.Tests.Endpoints.SearchEndpointContractTests` to the existing Search
namespace filter, covering all changed endpoint-file tests rather than only the six new cases.
[Focused patch results](evidence/dw-122-334-358-579-595/search-review-fixes-validation.json) and raw
`search-review-fixes-*` logs retain the earlier narrow checks separately.

[Final source/test identities](evidence/dw-122-334-358-579-595/final-source-test-identities.json)
bind the executed assemblies to source SHA-256
`dda55f1f015a168f2d23682f0e04a8dd7d1329ca4ad5090256598fe06f6f2f14`.
[Final runtime identities](evidence/dw-122-334-358-579-595/final-runtime-identities.json) bind that
same source to the actual loaded server DLL and package dependency manifests. Final live evidence:
[start](evidence/dw-122-334-358-579-595/final-aspire-start.txt),
[resource state](evidence/dw-122-334-358-579-595/final-resource-state.json),
[Memories wait](evidence/dw-122-334-358-579-595/final-memories-wait.txt),
[sidecar wait](evidence/dw-122-334-358-579-595/final-sidecar-wait.txt),
[provider loaded](evidence/dw-122-334-358-579-595/final-provider-binding.json),
[sidecar initialized](evidence/dw-122-334-358-579-595/final-sidecar-initialized.json),
[actual component metadata](evidence/dw-122-334-358-579-595/final-component-metadata.json),
[HTTP health/alive/ready](evidence/dw-122-334-358-579-595/final-http-health.json), and
[owned stop](evidence/dw-122-334-358-579-595/final-aspire-stop.txt).
Both resources are Healthy; all three HTTP endpoints return 200/Healthy; the selected provider is
`secretstores.local.file/v1`. Dashboard credentials and environment values are excluded.

[Independent scoped rechecks](evidence/dw-122-334-358-579-595/independent-review-recheck.txt)
confirm the fixes. Broad release/production-provider holds remain; old cancelled/failed runs and
superseded binary packets are historical evidence, not final acceptance claims.

## Exact starting revisions

- Tenants: `db83309146816602033320270f10b1cea28d3100` (`main`); the approved spec was the only pre-existing untracked file.
- Memories: `72c36c7113c750c8b809f9d72b945a2a8c2d4555` (`main`), initially clean. Five owning code/test files plus a companion scope artifact are prepared; the final owning commit will be recorded after review.
- Builds: `2574bf2f3da5367d8801f0d7661d66155e5d90b0`.
- Adopted EventStore: `717a06e16c3afd81b904eaf4de646b74b13262d2`. Earlier `36c78f3435d2d492a73f1b0707cf865c8735d740` evidence is historical and cannot replace qualification of this adopted pin.

## Iteration-1 search evidence (superseded by final direct patches)

Final source selects BM25 score descending and unique Redis key ascending before raw offsets, then hydrates selected keys in the same order. Both selection and hydration execute through bounded `FT.PROFILE ... LIMITED QUERY`; verified warning metadata rejects timeout-return partial results in RESP2 and RESP3. Exact raw cardinality also rejects configured aggregate-cap truncation instead of returning an apparently exhausted page. Successful windows retain raw offsets/totals, graph scope, highlights, summaries, stale-field filtering and source identities. Whitespace-only case IDs again normalize to null. A literal query term `warning` remains searchable because warning traversal inspects profile keys rather than query-term values.

A mismatched aggregate cap produces an explicit Redis failure for an unservable window. It does not silently trim results or extend server limits. No global Redis configuration or timeout bound is changed. Complete successful windows are deterministic for an unchanged index; this is not snapshot isolation across mutations. The curated adapter upsert contract proves one deterministic key per tenant aggregate and preserved `tenant:` sourceUri identities across pages.

| Final check | Result | Evidence |
| --- | --- | --- |
| Expanded controls against original source | 36 run, 18 expected failures, zero skips | [Original run](evidence/dw-122-334-358-579-595/search-iteration1-original-test.log) |
| Debug/source integration build | Zero warnings/errors | [Final build](evidence/dw-122-334-358-579-595/search-iteration1-final-ready-build.log) |
| Real syntactic and graph integration | 66/66, zero failures/skips/not-run | [Final run](evidence/dw-122-334-358-579-595/search-iteration1-final-ready-test.log) |
| Debug/source Search unit namespace | 271/271, zero failures/skips/not-run | [Final build](evidence/dw-122-334-358-579-595/search-iteration1-final-unit-build.log), [run](evidence/dw-122-334-358-579-595/search-iteration1-final-unit-test.log) |
| Actual package AppHost model | 1/1, zero failures/skips | [Build](evidence/dw-122-334-358-579-595/apphost-package-iteration1-build.log), [model](evidence/dw-122-334-358-579-595/apphost-package-iteration1-model-test.log) |

[Final source/test binary identities](evidence/dw-122-334-358-579-595/search-iteration1-final-identities.json) bind source SHA-256 `e55cae67ec268d872ad980f051fb3ee4a82549da5279fc517193ee60705e2b8f` to the executed source-mode assemblies. The final 66-case lane uses the same commands as the preserved first-pass run, with the owned native endpoint `127.0.0.1:56818`. The shared fixture retains its intentional FAIL policy; default RETURN and unequal-cap controls use owned endpoints/dedicated disposable servers. Final timeout controls directly observe partial original aggregate counts of 1,799/50,000 (RESP2) and 1,899/50,000 (RESP3), while the corrected service refuses incomplete execution. All four cap graph/protocol controls observe total 23 with zero raw rows at a valid deeper offset and prove explicit refusal, while shallow/exhausted requests behave correctly.

The first profile traversal implementation mistakenly inspected a query-term value as metadata; the final four real `warning` term cases pass. One pre-timeout disposable-server connection-readiness failure is retained in `search-iteration1-final-qualified-test.log`; internal/external TCP readiness and PING corrected it, and the final same 66-case lane passes. Earlier build/probe logs remain in the evidence directory rather than being reclassified as passing.

### Preserved first-pass search history (superseded by iteration 1)

Memories now selects raw positions using `FT.AGGREGATE` with `ADDSCORES SCORER BM25STD`, sorting `@__score DESC @__key ASC` before `LIMIT`. Optional validated graph keys are applied with `INKEYS` before sorting/window selection. `SORTBY MAX` retains only the required prefix; the page is hydrated using the existing `FT.SEARCH` scorer, highlighting, summaries and required-field filtering, then restored to the selected key order. Malformed documents still consume raw positions and remain included in totals. Existing missing-index/query-syntax classification, visibility filters, pagination normalization, scoped-key validation and stale-entry logging remain.

No Tenants gateway, cursor, authorization or cross-window deduplication change is needed. The upstream order gives its existing per-window authority hydration and opaque scoped raw-offset cursors a stable boundary. Ordering is guaranteed for an unchanged query/index; index mutation is not a snapshot-paging guarantee.

The added real Redis contract uses three relevance groups containing tied scores across five-item page boundaries, reverse insertion order, graph exclusions and repeated windows under both RESP2 and RESP3. It checks explicit identity order, adjacent-window completeness/uniqueness, descending BM25 relevance and raw totals. Existing malformed-document tests now force ties and verify persisted field removal, empty first raw positions, unchanged later windows and exhausted totals.

| Check | Result | Retained output |
| --- | --- | --- |
| New contracts against original search implementation | Expected failure: 21 run, 4 failed, 17 passed, zero skipped | [Original failure](evidence/dw-122-334-358-579-595/search-original-contract-test.log) |
| Initial Debug/source integration build | Exit 0, zero warnings/errors | [Build](evidence/dw-122-334-358-579-595/search-final-build.log) |
| Initial syntactic + graph integration classes | 43/43 passed, zero skipped/not-run | [Real infrastructure run](evidence/dw-122-334-358-579-595/search-final-contract-test.log) |
| Initial Debug/source Server unit build | Exit 0, zero warnings/errors | [Build](evidence/dw-122-334-358-579-595/search-unit-final-build.log) |
| Entire Server Search unit namespace | 271/271 passed, zero skipped/not-run | [Unit run](evidence/dw-122-334-358-579-595/search-unit-final-test.log) |

The integration fixture uses the checked-in Redis Stack digest `sha256:880df9c228597cb0d15b585f39a4327d6ee2d8b0d0f155e3f75dba9a761d4ec3` (Search `2.10.20`). The focused owned container binds only `127.0.0.1:59893`; `FT.CONFIG SET ON_TIMEOUT FAIL` was selected before first-pass verification. The existing graph fixture separately starts/disposes its pinned Redis/FalkorDB containers.

Commands, from `references/Hexalith.Memories`:

```bash
dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj -c Debug -m:1 -p:UseHexalithProjectReferences=true -p:UseNuGetDeps=false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0
MEMORIES_TEST_OWNED_NATIVE=1 MEMORIES_TEST_REDISSTACK_CONNECTION=127.0.0.1:59893 dotnet tests/Hexalith.Memories.IntegrationTests/bin/Debug/net10.0/Hexalith.Memories.IntegrationTests.dll -class Hexalith.Memories.IntegrationTests.Search.SyntacticSearchIntegrationTests -class Hexalith.Memories.IntegrationTests.Search.GraphScopedSearchIntegrationTests
dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj -c Debug -m:1 -p:UseHexalithProjectReferences=true -p:UseNuGetDeps=false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0
dotnet tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class 'Hexalith.Memories.Server.Tests.Search.*'
```

The original-code control used the first build command and only the syntactic class, before production code edits. It failed both tied-score cases and both tied malformed-document cases at the explicit tie-order assertion. Subsequent expanded coverage is retained, too: [initial object/string unit assertion mismatch](evidence/dw-122-334-358-579-595/search-unit-initial-failure.log), corrected by comparing the Redis key's string identity, and [overstrict repeated numeric-score equality failure](evidence/dw-122-334-358-579-595/search-repeated-score-equality-failure.log). The latter kept identical identities/snippets but observed changing BM25 magnitudes; the contract now checks repeated identity order and descending relevance, rather than requiring identical score magnitudes. An initial compile attempt also reported CS0030 at the two RedisValue-to-RedisKey casts; conversion through `ToString()` corrected it before any successful integration run. These first-pass results are preserved history; iteration 1 below binds the corrected final source.

The Redis aggregation options used here are documented by Redis in [FT.AGGREGATE](https://redis.io/docs/latest/commands/ft.aggregate/) and were directly exercised against the pinned image before implementation. No dependency version, Redis schema, timeout validator or production provider was changed.

## DW-334/DW-358: actual package model and live local provider

From the Tenants root, the actual AppHost test project was built with explicit package-dependency settings, then its built xUnit assembly was invoked with the required single-dash class filter:

```bash
dotnet build tests/Hexalith.Tenants.AppHost.Tests/Hexalith.Tenants.AppHost.Tests.csproj -c Debug -m:1 -p:UseNuGetDeps=true -p:UseHexalithProjectReferences=false -p:HexalithMemoriesFromSource=false -p:HexalithFrontComposerFromSource=false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0
dotnet tests/Hexalith.Tenants.AppHost.Tests/bin/Debug/net10.0/Hexalith.Tenants.AppHost.Tests.dll -class Hexalith.Tenants.AppHost.Tests.MemoriesSecretStoreResourceGraphTests -showLiveOutput
```

Both commands exit 0: build zero warnings/errors; test 1/1 passed. [Build output](evidence/dw-122-334-358-579-595/apphost-package-build.log), [actual assembly metadata/model output](evidence/dw-122-334-358-579-595/apphost-package-model-test.log). Inspected assembly metadata is `UseNuGetDeps=true`, `UseHexalithProjectReferences=false`, `HexalithMemoriesFromSource=false`. The existing model assertions prove the unique consumer-owned resource/component path and both server/sidecar references plus wait relationship.

The exact Tenants AppHost was then started through Aspire in isolated mode before production edits. Baseline [start](evidence/dw-122-334-358-579-595/apphost-start-baseline.log), [server wait](evidence/dw-122-334-358-579-595/apphost-memories-wait-baseline.log), [sidecar wait](evidence/dw-122-334-358-579-595/apphost-sidecar-wait-baseline.log), [provider binding](evidence/dw-122-334-358-579-595/memories-secretstore-live-baseline.json), [initialization](evidence/dw-122-334-358-579-595/memories-initialized-live-baseline.json), and [owned stop](evidence/dw-122-334-358-579-595/apphost-stop-baseline.log) are retained. This first baseline used the existing launched server output; it is separate from final explicit package-dependency server qualification.

To remove any stale source-dependency server ambiguity, Memories Server was explicitly rebuilt in Debug with package dependencies before the package live run, and rebuilt again after the final source adjustments:

```bash
# From references/Hexalith.Memories:
dotnet build src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj -c Debug -m:1 -p:UseNuGetDeps=true -p:UseHexalithProjectReferences=false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0
# From the Tenants root:
aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --isolated --no-build --format Json --non-interactive
aspire wait memories --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --timeout 45
aspire wait memories-dapr-cli --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --timeout 45
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --include-hidden --format Json
aspire logs memories-dapr-cli --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --search 'secretstore' --format Json
aspire logs memories-dapr-cli --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --search 'initialized' --format Json
```

Preserved first-pass live packet passes: [final package server build](evidence/dw-122-334-358-579-595/memories-server-package-final-build.log) exits 0 with zero warnings/errors; [exact binary/package dependency identities](evidence/dw-122-334-358-579-595/package-runtime-identities.json), [Aspire start](evidence/dw-122-334-358-579-595/apphost-start-final.log), [server healthy](evidence/dw-122-334-358-579-595/apphost-memories-wait-final.log), [sidecar healthy](evidence/dw-122-334-358-579-595/apphost-sidecar-wait-final.log), [resource/launch state](evidence/dw-122-334-358-579-595/memories-resource-state-final.json), [actual local-file provider loaded](evidence/dw-122-334-358-579-595/memories-secretstore-live-final.json), [Dapr initialized Running](evidence/dw-122-334-358-579-595/memories-initialized-live-final.json), [sidecar component metadata](evidence/dw-122-334-358-579-595/memories-dapr-component-metadata-final.json), and [HTTP health/alive/ready](evidence/dw-122-334-358-579-595/memories-http-health-final.json), all three status 200/Healthy. The server runtime manifest resolves EventStore Client/Contracts/DomainService/ServiceDefaults 3.128.4 as packages; the AppHost manifest resolves Memories.Aspire 3.0.3 as a package. The immediately preceding package run remains retained with `package-pass1` names. All retained packets exclude secret values.

The selected checked-in provider remains `secretstores.local.file/v1` in `src/Hexalith.Tenants.AppHost/DaprComponents/secretstore.memories.yaml`; no provider repair or AppHost code change was necessary. Dashboard login tokens are redacted from retained startup output. No secret retrieval endpoint was called and no secret values are recorded. The package mode refers to the genuine AppHost helper and published external runtime dependencies; Memories Server is the locally built owning server, not an unpublished NuGet package claim.

Limits: isolated Aspire randomizes host state/ports but the checked-in state-store/pubsub YAML still names existing local Redis. Legacy queued events caused `TENANT_NOT_FOUND` retries for the unprovisioned search partition; this does not negate component initialization/health, and no ingestion or end-to-end search deployment claim is made. Production-provider qualification and package publication remain outside scope. Broader runtime/release holds remain.

### Iteration-1 corrected-source live qualification (historical binary)

After re-derivation, the owning server was rebuilt with the same explicit package-dependency command and the exact Tenants AppHost restarted in isolated mode. [Final package build](evidence/dw-122-334-358-579-595/memories-server-package-iteration1-final-build.log), [AppHost start](evidence/dw-122-334-358-579-595/apphost-start-iteration1-final.log), [server wait](evidence/dw-122-334-358-579-595/apphost-memories-wait-iteration1-final.log), [sidecar wait](evidence/dw-122-334-358-579-595/apphost-sidecar-wait-iteration1-final.log), [actual provider](evidence/dw-122-334-358-579-595/memories-secretstore-live-iteration1-final.json), [Dapr initialization](evidence/dw-122-334-358-579-595/memories-initialized-live-iteration1-final.json), [component metadata](evidence/dw-122-334-358-579-595/memories-dapr-component-metadata-iteration1-final.json), and [health/alive/ready](evidence/dw-122-334-358-579-595/memories-http-health-iteration1-final.json) pass; all three HTTP checks return 200/Healthy.

[Final runtime identities](evidence/dw-122-334-358-579-595/package-runtime-identities-iteration1-final.json) bind the corrected source hash to the actual loaded server DLL `4e85fa5158f328043d20c3a9253cac231ccd1d0b6cfe99441f522f6f150e05eb`, identified through the child process module mappings. AppHost external helpers and launched server external EventStore libraries resolve as genuine packages; this remains a locally built owning server rather than an unpublished server-package claim. Owned Aspire/container resources were stopped; cleanup logs are retained.

## DW-579: current Builds audit reconciliation

Executed from Builds `2574bf2f3da5367d8801f0d7661d66155e5d90b0`:

```bash
pwsh -NoProfile -File Tools/validate-package-version-audit.ps1
```

Exit 0: `Package version audit validation passed for 307 packages, 151 families, and 1 source(s).` [Complete validator output](evidence/dw-122-334-358-579-595/builds-current-audit-validation.log). [Independent origin reconciliation](evidence/dw-122-334-358-579-595/builds-audit-reconciliation.json) records the current audit SHA-256 and confirms all 67 distinct generated origin revisions resolve in the owning repository. The historical repair is ancestor `85c5fb3bff144be5dc184c478a49235a5e17b4bb`; it removed unavailable historical context entries without relaxing the validator.

This accepted current audit supersedes the current blocker. It does not accept the October 10 snapshot retrospectively: historical origins `200a01334b97bc1f254be0be22575b52960abac5` and `9566bafbc93118bd717cc610e11fc6584362ad18` remain unavailable. Original failed evidence and its source fields remain unchanged.

## DW-595: adopted EventStore hosted qualification

Exact adopted EventStore `717a06e16c3afd81b904eaf4de646b74b13262d2` now has successful [CI run 40653941080](https://github.com/Hexalith/Hexalith.EventStore/actions/runs/40653941080), including `ci / contracts` job `122700333968`, `ci / build-and-test` job `122700334031`, folded-snapshot compatibility, governance, checkpointed process packages and Tenants source-mode jobs. Payload Protection, lifecycle, CodeQL and Advisory Tests workflows also succeed at that exact SHA. [Raw workflow conclusions](evidence/dw-122-334-358-579-595/eventstore-exact-pin-runs.json), [CI job conclusions](evidence/dw-122-334-358-579-595/eventstore-exact-pin-ci-jobs.json).

The separate exact-pin [Integration Tests run 40653940590](https://github.com/Hexalith/Hexalith.EventStore/actions/runs/40653940590) succeeds with `live-sidecar` and `published-compatibility` jobs ([raw jobs](evidence/dw-122-334-358-579-595/eventstore-exact-pin-integration-jobs.json)). The CI workflow itself skips its advisory performance/Aspire jobs; those skips are not claimed as passing. Earlier cancelled Integration run `40644656405` at the previous pin remains historical, not a success. This settles the original upstream red unit/contracts blocker and distinguishes independent current runtime evidence from broader Tenants release readiness.

```bash
gh run list -R Hexalith/Hexalith.EventStore --commit 717a06e16c3afd81b904eaf4de646b74b13262d2 --limit 30 --json databaseId,name,status,conclusion,headSha,url
gh run view 40653941080 -R Hexalith/Hexalith.EventStore --json jobs,headSha,conclusion,url
gh run view 40653940590 -R Hexalith/Hexalith.EventStore --json jobs,headSha,conclusion,url
```

## Independent review, ledger preservation and publication

First independent review reported twelve findings across blind/edge layers; verification-gap review found none. Every finding is individually triaged in the approved spec. Re-derivation fixes timeout/cap completeness and caseId compatibility and expands exhausted/malformed/curated identity coverage. Final scoped blind and verification rechecks found no unresolved issue; the final edge layer reported none. Every kept finding is fixed; no new deferred record was created.

The ledger now closes exactly DW-122/DW-334/DW-358/DW-579/DW-595. [Preservation result](evidence/dw-122-334-358-579-595/ledger-preservation.json) verifies all 657 IDs, all 652 unselected numbered bodies, selected original fields, 639 done/18 open and exact coverage by 17 maintenance rows. Original historical failures and policy holds remain. Both exact commit candidates pass their owning pinned commitlint tools ([Tenants](evidence/dw-122-334-358-579-595/tenants-commitlint-validation.log), [Memories](evidence/dw-122-334-358-579-595/memories-commitlint-validation.log)). All intentionally retained `.log` files are explicitly included despite the root ignore pattern; no ignore rule is changed. Final owning commits are validated before publication.


## Self-contained reproduction and ledger verification

The owned native endpoints above are historical captures and were removed after the runs.
For a fresh Docker-capable checkout, omit the native override so the standard fixtures provision
their pinned containers. The timeout/cap controls independently create and clean up their own
RETURN-policy servers, preserving the shared fixture's established FAIL policy:

```bash
# From references/Hexalith.Memories, after the documented Debug/source integration build:
dotnet tests/Hexalith.Memories.IntegrationTests/bin/Debug/net10.0/Hexalith.Memories.IntegrationTests.dll -class Hexalith.Memories.IntegrationTests.Search.SyntacticSearchIntegrationTests -class Hexalith.Memories.IntegrationTests.Search.GraphScopedSearchIntegrationTests
```

The exact read-only preservation computation is retained as
[verify-ledger-preservation.py](evidence/dw-122-334-358-579-595/verify-ledger-preservation.py).
From the Tenants root:

```bash
python3 _bmad-output/implementation-artifacts/evidence/dw-122-334-358-579-595/verify-ledger-preservation.py
```

It loads the recorded Git baseline, verifies all 652 unselected bodies byte-for-byte as decoded
text, preserves every original selected non-status field/body line and its prior status/action,
and checks exact open-ID/index coverage and the disposition table. Its output reproduces the
retained JSON. It performs no writes or Git mutations.

## Owning source publication

Memories main now contains [`35ef40e3fdcc7a490e04cf49e6a532fd5bf32285`](https://github.com/Hexalith/Hexalith.Memories/commit/35ef40e3fdcc7a490e04cf49e6a532fd5bf32285). [Publication/source identity](evidence/dw-122-334-358-579-595/memories-publication.json) verifies that the committed materialized source exactly matches the tested DDA hash. A non-fast-forward push race was resolved by rebasing onto the independent tooling commit 6249e6bafc577934be959a3f248fbb27c8951ffc; no search source/test path changed. Updated scope/readiness and post-commit pinned commitlint checks pass. The root commit adopts this exact declared pointer and retains every qualification artifact.


## Retained log bytes and whitespace gate

`git diff --cached --check` initially rejected nine Testcontainers `Labels: ` log lines for trailing
whitespace. The gate was preserved. [Original retained command logs](evidence/dw-122-334-358-579-595/raw-command-logs.zip)
archive every `.log` file before formatting; [hash manifest](evidence/dw-122-334-358-579-595/log-normalization.json)
verifies each archived original and published text. Individually linked text logs use UTF-8/LF and
trim trailing spaces/tabs only. Test outcomes and all other content are preserved; credential
redactions already applied at capture remain applied. The archive preserves original retained
line endings and whitespace for exact inspection. No source/validator ignore rule was weakened.
