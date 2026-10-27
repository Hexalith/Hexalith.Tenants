# Read and recovery contract qualification — 2026-10-26

Implementation baseline: `cc0efc7df3399f1ea0e3789033ee75926bd06b35`.
Selected records: DW-114, DW-137, DW-144, DW-514 and DW-523.
The original selection and qualification below are historical. The 2026-10-27 approved scope, current verification and final acceptance are recorded in the resumed sections; DW-137 remains deferred.

[Raw validation records](evidence/dw-114-137-144-514-523/validation-records.tar.gz) retain the exact command metadata, outputs, initial failures and controls; the [manifest](evidence/dw-114-137-144-514-523/manifest.json) binds their bytes. AppHost startup logs containing dashboard access tokens and private certificate material are excluded.

## Final acceptance — 2026-10-27

The approved five items are complete: DW-114, DW-144, DW-514, DW-523 and the original unnumbered cleanup-test follow-up. DW-137 remains open with the downstream identity-policy blocker; its exact removed patch bytes remain archived. Three independent review layers returned sixteen findings; all were classified separately and corrected, with two additional compatibility/assertion corrections. No review finding is deferred.

Current qualification passes: full unchanged UI **7,839/7,839**, real Redis **19/19**, rebased Server recovery/status/provenance **123/123**, restored generator **124/124**, shared harness defaults **4/4**, complete AppHost in its required package mode **30/30**, maintained real-sidecar route class **6/6**, and controlled Dapr **13/13**. All passing runs have zero skips/failures. The maintained retry mutation is rejected with four backend calls; its other twelve controls pass. The blocking CI lane now executes this check and mutation, and requires all six route cases.

The route suite resolves the production registered client and token relay. It qualifies all six persisted reads, real generated 304/authority metadata, protected cursor recovery, missing bearer401, authenticated wrong caller403 and disallowed verb403. Model tests cover generated credential environment, explicit runtime retention, failed-start directory cleanup and legacy published-harness loopback argument compatibility. Only the known standard local prerequisite hints are superseded, with diagnostics; custom/external endpoints reject. Memories components are scoped to their owner and absent from both reader sidecars' runtime metadata.

The current Integration Debug/source build passes with the supported Memories helper package edge (`HexalithMemoriesFromSource=false`), without tracked source exclusions. Initial new live tests passed four and failed two because a test parsed normalized unquoted ETag metadata as raw header syntax; the production client was already correct, and the corrected complete rerun passes six. The existing package-only AppHost test correctly failed when the full assembly was run in source mode; rebuilding in package mode passes all thirty. These attempts remain recorded, not hidden.

The numbered ledger now has **624 done / 33 open across 657 IDs and 29 maintenance rows**. The unnumbered resolution is excluded from numbered totals. A byte audit preserves all 653 unselected numbered bodies, including the complete open DW-137 record. Selected historical fields and the original unnumbered finding are retained.

[Final commands](evidence/dw-114-137-144-514-523/recommended-resume/final-command-metadata.json), [sanitized validation outputs](evidence/dw-114-137-144-514-523/recommended-resume/final-validation-records.tar.gz), [pinned commitlint evidence](evidence/dw-114-137-144-514-523/recommended-resume/commitlint-records.tar.gz), [owning pushed commits](evidence/dw-114-137-144-514-523/recommended-resume/completed-module-commits.json), [source hashes](evidence/dw-114-137-144-514-523/recommended-resume/final-source-hashes.json), and [ledger audit](evidence/dw-114-137-144-514-523/recommended-resume/ledger-preservation-audit.json) retain the outcome. Dashboard access tokens, bearer values and private key material are redacted from retained live logs. Earlier phase archives remain historical.

The prior broad Platform analyzer and package/provenance/release holds are unresolved; no green whole-Platform, Release or hosted CI result is claimed. No production data migration, manual publication or deployment was performed. Owning fixes are pushed to main before the parent gitlinks; concurrent upstream work is preserved.

## Resumed scope and verification — 2026-10-27

The approved scope is DW-114, DW-144, DW-514 and DW-523 plus the original unnumbered navigation subscription-cleanup follow-up. DW-137 remains open with its verified downstream shared-identity blocker. The original 2026-10-26 investigation below is historical evidence; its pending identity-policy question is resolved by the approved decision to retain existing restrictions and defer encoding.

The unfinished codec, generated-controller decoding, BFF encoding and their encoding-only tests are removed. The original patches and SHA-256 manifest are retained in [recommended-resume](evidence/dw-114-137-144-514-523/recommended-resume/manifest.json). The current BFF retains literal routes, percent-escaping, and refusal of slash, backslash and dot-only identifiers before transport. Deployment guidance now describes this behavior. Intervening commits, retained recovery/paging/mTLS changes and dependency pointers are preserved.

The late-setup cancellation regression now waits for an explicit bounded unsubscribe-completion signal. It first observes caller cancellation before releasing backend setup, holds unsubscribe, verifies the callback is already detached during that hold, releases cleanup in a finally block, and then verifies exactly one unsubscribe and no refresh after cleanup. Production cleanup is unchanged.

Current ordinary UI Debug/source build succeeds with zero warnings/errors. The actual owning cleanup, REST client and composition classes pass **260/260**, zero skipped. The restored generator Debug build succeeds with zero warnings/errors and its maintained MTP suite passes **124/124**, zero skipped. The earlier MTP pipe-union class filter ran zero tests (exit 5); the supported repeated `-class` assembly invocation supplies the focused evidence. The existing healthy ordinary mTLS AppHost rerun passes **9/9** (six literal reads, wrong caller/verb denials, missing bearer). Its retained Platform helper/subscriber source hashes exactly match the earlier qualified bytes; source decoder removal is independently covered by the rebuilt generator suite.

The current all-source AppHost test build is blocked at unchanged Memories `EventStoreMemoriesCommandStore.cs:22`, CS0246 for `IEventStoreGatewayBoundCommandProofSource`. Its exact command/output is retained separately from passing focused evidence. The full owning UI run now passes **7,839/7,839**, zero skipped. The AppHost package-dependency fallback builds with zero warnings/errors and its **15/15** model/shared identity checks pass. Current Memories Integration Debug/package builds without source repairs or compile exclusions; its maintained real-Redis search class passes **19/19**. Current EventStore Server Debug/source builds with the current Tenants source edge, zero warnings/errors; recovery/status/provenance passes **123/123**. The narrower package-Tenants edge fails new CatalogConstruction routing references and is superseded by the complete source lane. Prior broad historical results are retained separately, without implying that all-source AppHost, Contracts governance or Platform package gates pass.

[Resume command metadata](evidence/dw-114-137-144-514-523/recommended-resume/command-metadata.json), [source hashes](evidence/dw-114-137-144-514-523/recommended-resume/source-hashes.json), original codec patches, passing outputs and exact blocker are bound by the resume manifest. Raw Aspire environment, dashboard tokens and private certificate material are excluded.

[Parent validation archive](evidence/dw-114-137-144-514-523/recommended-resume/parent-validation-records.tar.gz) retains completed full UI, AppHost, current Redis and Server outputs. The resume metadata records exact commands and current repository heads. The three dependency checkouts advanced externally during the discussion; their current commits and all intervening work are preserved. Independent review and ledger acceptance follow these checks.

### Independent-review corrections — 2026-10-27

Memories echo/secret-store components now admit only app ID `memories`; both read sidecars' real Dapr metadata excludes them. Canonical Platform/Builds source prerequisites resolve standalone, sibling and enclosing-Platform layouts in both dependency modes. Explicit native runtime selection is preserved; failed composition cleans both private run state and a separately configured owned credential child. Local runs diagnostically reject explicit external placement/scheduler settings. The shared harness retains explicit endpoint arguments, including empty values, so Tenants selects its owned private plane without changing other consumers' defaults. Search remarks now specify the normalized raw window and HTTP 1..100 versus direct-service 1..1000 caps.

Live REST tests resolve the production typed client and real bearer-forwarding/routing handlers against the UI sidecar, including conditional and cursor cases, and add authenticated wrong-peer and disallowed-verb 403 controls. Their final live execution remains parent-owned. The portable ordinary-AppHost probe collects fresh Aspire state (or explicit inputs), requires all nine checks, and exits nonzero for missing/failed checks. Current rerun passes **9/9**; injected empty-list/read-failure controls verify the failure exits.

The maintained real-Dapr HTTPS qualification is `tests/integration/qualify-dapr-read-retries.py`, promoted from the original controlled script using repository-relative templates, the installed runtime/matching Sentry image, isolated temporary credentials/ports and cleanup. Its current baseline passes **13/13**, including one backend call on 503 and two component-scope controls. The independent `--mutate-retries` run exits 1, observes exactly four calls, and passes the other twelve checks. The blocking live-route CI lane runs both and validates the expected mutation failure.

Focused current-source AppHost build (Memories package edge only) and shared-harness build have zero warnings/errors. Changed AppHost model/identity classes pass **19/19** and shared-harness endpoint-default tests pass **4/4**, zero skipped. Six direct MSBuild layout/mode evaluations pass; CI YAML parses. No wider suite ran in this correction pass. Commands, outputs, controls and current source hashes are retained in [review-fix evidence](evidence/dw-114-137-144-514-523/recommended-resume/review-fixes/manifest.json).

The exact original DW-137 patch bytes are now compressed in [the preserved archive](evidence/dw-114-137-144-514-523/recommended-resume/dw-137-encoding-patches.tar.gz); extraction matches the original per-member hashes. Only the two newly added raw patch copies were removed; no whitespace exemption was introduced. The [patch manifest](evidence/dw-114-137-144-514-523/recommended-resume/dw-137-preserved-patch-manifest.json) binds the archive and member bytes.

The initially reported published-harness compatibility gap is addressed by a narrow allowance for its standard loopback prerequisite hints. Local runs accept only `localhost`/`127.0.0.1` with placement `50005`/`6050` or scheduler `50006`/`6060`, print a diagnostic that private allocated listeners supersede each legacy hint, and retain the private mTLS endpoints on every sidecar. External hosts, custom ports and cross-service port selections are rejected. Four model rows reproduce the unchanged package's appended arguments after empty fixture settings, and eight rejection rows cover the boundary: **12/12**, zero failed/skipped. The captured execution contains all eight supersession diagnostics. The focused source build has zero warnings/errors; no public mode flag, source-only Release exception or duplicate harness was added. [Exact commands and bytes](evidence/dw-114-137-144-514-523/recommended-resume/review-fixes/legacy-loopback-compatibility.json) retain this follow-up. Full package/live qualification remains parent-owned.

The parent live run also exposed an incorrect added assertion parsing normalized metadata ETags as quoted wire headers. That assertion is removed; nonempty authority metadata and the unchanged client's strong wire-tag acceptance remain. The parent owns the six-case live rerun. No wider suite ran in this follow-up.

## Original qualification — historical record

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
