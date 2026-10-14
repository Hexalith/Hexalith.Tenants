# B7 validation evidence — 2026-10-14

B7 closure is proven for DW-195/DW-206/DW-235/DW-264. All required local implementation gates pass; full release remains **not ready**. Only these four findings close.

## Boundary, entry and inherited provenance

B7 owns DW-195/DW-206/DW-235/DW-264 only. Entry was clean `main`, full HEAD `7ad54431996f9f1f0d277005eb2a73b67b55fea6`, equal to `origin/main`. [Entry state](b7-entry-state.json) captures branch, complete HEAD, empty working/index diffs, remotes, recent history and all nine root dependency pointers. The complete tracked-file hash snapshot is retained locally at `/tmp/tenants-b7-20261014/entry-state.json`. Review compares new work against this entry, preserving committed B1–B6 changes.

The B7 standard gitlink check passes against its own full baseline. The historical B6 check remains blocked by four inherited, undeclared pointer movements over `57b881beebfbcdf0fe6d7bc4c23a0fc70903136d`: Builds, EventStore, Memories and Platform. These movements predate B7, including external commits `a8c18a6e435280eb56b866cc4693683716e81ee4` and `a5de755d204f5adeda60f883834fe8bf0075f261`. No pointer was reset, moved or falsely attributed to B7; the guard and historical evidence remain intact.

Required AGENTS/baseline, UX/state instructions, the four ledger findings, source specs, maintenance index, readiness/B6 evidence, architecture and tracked build/test configuration were read. Recorded August viewport/role decisions authorize these changes. Domain persistence, projections, circuit principal resolver, existing viewport effect/observer, support-safe command gateway and maintained CI remain unchanged. No new domain storage or migration is introduced.

## Implementation and regression boundary

- Removal uses the established scoped viewport observation and requires an actual Safe measurement. Missing observer, unmeasured/seeded Safe, measured Unknown and Phone refuse direct submit/retry; resize invalidates pending admission. Status, proof, receipt and retained exact attempt survive viewport changes. Scoped form selectors now target the actual Fluent/EditForm-generated form.
- Metadata uses current literal-tenant Owner/Contributor or GA reflection; membership uses Owner or GA for add, role and removal, including direct launchers. Default authority is Indeterminate/false; denied and indeterminate reasons are distinct EN/FR resources. No empty-membership UI inference bypasses the backend bootstrap history rule; resolved GA first-member admission is preserved.
- Create uses resolved current-caller GA reflection, preserving authoritative-empty first-tenant Unknown freshness. The exception affects freshness alone. Pending authority preserves circuit audit-return state until resolution; existing caller-change clearing remains.
- Fresh authority reads and post-lease reauthorization guard every dispatch; stale caller/tenant/target/evidence results cannot authorize. Changed current status/version/preview evidence invalidates the old submission; Unauthorized reads withdraw protected detail/member surfaces. Superseded render probes and cancelled/noncooperative reads cannot publish later. Provider waits are cancellable and bounded at 10 seconds. Retry captures exact attempt evidence before awaiting authority and refuses redelivery after tracking/proof resolves ambiguity.

Parent tests use the real BFF/principal resolver for scoped role success and current membership revocation. Direct submission/retained-retry cases assert command dispatch counts, identity, ownership and terminal proof, rather than accepting backend rejection as UI proof.

## Independent review

Three context-free layers reviewed the same B7 diff: blind review, edge cases and verification gaps. Their 16 findings, three correction follow-ups and two regression-discovered correction findings are individually classified in the [spec triage log](spec-b7-live-admission.md#review-triage-log). Demonstrated private guard/test corrections were applied without resetting authorized work. Source correction re-check reports no remaining concrete safety defect. No new finding is deferred and no unrelated ledger record is added.

The browser request for a fully authenticated live circuit remains an explicit historical release qualification, rather than a claim supplied by rendered fixtures. The maintained browser lane uses actual EN/FR rendered markup, real Chromium layout/focus and the build-selected shipped breakpoint asset. Parent/direct/effect bUnit checks establish .NET dispatch refusal. This combined evidence does not certify clipboard/denial journeys, human assistive technology, every physical viewport or production authorization.

## Commands, results and initial failures

Every exact root/test command, exit code, elapsed time, initial failed test name and retry is preserved in [durable command results](b7-command-results.json). Raw logs are under `/tmp/tenants-b7-20261014` and `/tmp/b7-*.log`; the durable JSON retains their results when temporary logs are unavailable. Aspire startup logs contain dashboard credentials and are not copied into tracked evidence.

Completed source-reference lane:

```text
dotnet build tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false --no-incremental
dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-build -p:UseHexalithProjectReferences=true --minimum-expected-tests 4 --filter-method Hexalith.Tenants.IntegrationTests.TenantsApiGeneratedControllerTests.GlobalAdministratorsRealHandlerMetadataSurvivesRouterRestClientAndUiGateway
```

Both exit0: graph build zero warnings/errors; generated-controller cases 4/4, zero failures/skips. Final candidate logs are `source-integration-build-final.log` and `source-generated-controller-final.log`; the earlier passing run is also retained. Final graph 35.74s, cases 1.89s.

### Final maintained results

| Lane | Exact outcome | Log |
| --- | --- | --- |
| Source-reference Debug graph / generated-controller path | Build: 0 warnings/errors;4/4, no failures/skips | `source-integration-build-final.log`, `source-generated-controller-final.log` |
| Final Debug UI build / focused matrix / full UI | Build: 0 warnings/errors;582/582 focused;4,939/4,939 full, zero errors/skips/not-run | `/tmp/b7-review-test-build4.log`, `/tmp/b7-review-focused2.log`, `/tmp/b7-review-ui-final2.log` |
| Release restore/build | Exit0; build: 0 warnings/errors | `release-restore-final.log`, `release-build-final.log` |
| Contracts / Client / Testing / Sample / AppHost / Server / UI |154 /50 /182 /40 /6 /926 /4,939 passed; **6,297 total**, no failures/skips | `release-{contracts,client,testing,sample,apphost,server,ui}.log` |
| Release integration excluding Performance |314/314, no failures/skips; **6,611 maintained Release cases combined** | `integration-release-final.log` |
| Maintained coverage |94.70% (1,233/1,302) package-scope lines;100% (150/150) required branches | `coverage-final.log` |
| Debug and Release Chromium | Both exit0; EN/FR,1024/767/768 measurement, computed form/focus/forced-colors, observer unsubscribe and all old/new mutation controls pass | `/tmp/b7-review-browser-final2.log`, `browser-release-final.log` |
| Local pack / package validation / isolated consumers | Five packages at `5.8.0-b7-validation`; all validations/builds pass; infrastructure-free consumer test 1/1 | `packages-pack-final.log`, `packages-validate-final.log`, `consumers-validate-final.log` |
| Python story-gitlink tests / B7 range |23/23; standard B7 guard PASS, no pointer movement | `python-guards.log`, `b7-gitlinks-final.log` |
| Historical B6 range | Exit1, four inherited UNDECLARED movements; preserved as a release provenance blocker | `historical-b6-gitlinks-final.log` |

All seven projects ran individually. Maintained floors remain154/50/182/40/6/884/4765, integration189, source-path4. The coverage command retains the exact80%/100% thresholds, four package line scopes and four isolation/auth inputs. UI coverage is collected but UI is outside the existing publishable-package line gate; no scope, gate or exclusion was changed. Release/default package mode and the configured FrontComposer UI source exception were preserved.

The Release breakpoint asset was selected from that build's static-web-assets manifest, `SourceId=Hexalith.FrontComposer.Shell`, `SourceType=Project`; SHA-256 `475b8f2e0e7de0e832b9e1d6b7f64916d35334ea747fd1835e9c6ac282bdaf0a`. Manifest SHA-256 `229995cb3d55a69dd30fc728dca52d4e7cab54b4bf4f7db292e4a6fe01fa844d`. Debug module bytes match. This is the unchanged UI source-reference exception, not a claim that a different packaged JS asset was tested. Package hashes and consumer evidence are in the durable JSON; no package was published.

### Initial failures and corrections

| Run | Failure / correction |
| --- | --- |
| Initial root Debug build | Exit1, three CS0118 uses of nonexistent membership Lifecycle tuple fields; removed invalid references. Subsequent builds clean. |
| Initial root method filter | Exit3; internal wildcard syntax unsupported by xUnit. Corrected executed class selectors passed351 cases; final matrix/full lanes use valid selectors. |
| First full Debug UI |4785 total /123 failed, no errors/skips; old permissive authorization and seed-only viewport fixtures. Explicit positive authority/fresh provider and actual measurement fixtures restored every retained assertion. |
| Test continuation build2/build4/build14 |11 signature-insertion errors /1 wrong fixture method /3 invalid byte-enum literal errors. Corrected helpers and invalid tier255; successful subsequent builds preserved in JSON. |
| Intermediate focused/fixture/full runs |231/1;260/10;213/6;219/2;4872/4;226/2. Record exact failed names in JSON. These isolated resource/fixture scope/setup assumptions and the real same-snapshot stale render race, which was fixed and tested. |
| First browser / next browser | Phone form remained visible: fixed generated EditForm CSS scoping. FR resize tier did not emit before paint: deterministic layout/RAF/compositor synchronization; no assertion weakened. |
| Review fixture build1 | One nullable caller-scope fixture argument; corrected fixture, build2 clean. |
| Review-focused1 |347 total /15 failed, no errors/skips. Reproduced outer form proof overwrite and pending audit-return marker consumption; fixed source and kept exact proof/identity/store assertions. Corrected prior-proof tracker seeding and obsolete authorized Add-slot minimum; category/key/value assertions preserved. |
| Review full UI initial |4939 total /1 failed: inherited nudge fixture switched detail on read2, now a fresh admission read. Keep initial detail for the first3 reads and introduce nudge data on read4; every original nudge/proof assertion remains. Narrow retry1/1 and final full4939/4939 pass. |
| Final preservation/whitespace initial | Count helper matched historical `status: done` inside DW-328 legacy text; anchored actual status fields prove316 done/327 open. Removed one trailing whitespace-only Razor line. Both checks pass on retry; no unrelated record changed. |
| Review browser initial | Inputs briefly missing during the final Debug no-incremental graph rebuild; no browser assertion executed. Serial retry after regenerated inputs passed. This environmental failure remains in JSON. |

The final correction re-check independently confirmed outer submission evidence guards and delayed marker cleanup, with no further concrete defect found. Nothing was deferred by the review. The earlier broad runs and all failures remain evidence rather than being relabeled final passes.

### Preservation and closure

Exactly DW-195/DW-206/DW-235/DW-264 change to done 2026-10-14. Original findings, source specs, legacy text, August decisions, owners/priorities/dependencies/related IDs and pre-fix verification/date/action remain in their entries. The maintenance index removes exactly four rows: **643 IDs,316 done /327 open;291 rows**. Reconciled counts:112 done,239 actionable,15 blocked,26 awaiting-decision,9 accepted-limitation,38 uncertain.

Final preservation/whitespace verification passed and is recorded in the durable command results. The JSON also embeds the preservation check source; it reads the captured entry and original ledger/readiness from the full baseline commit, so temporary baseline backups are not required. Dated B1–B6 readiness sections, B6 validation evidence, archive, sprint, build/coverage configuration, baseline/UX/state/principal/viewport/domain/projection safety seams, full HEAD, index and all nine dependency checkouts/pointers match entry. Only B7 source/tests and requested current documentation/evidence change. No staging, commit, push, dependency update, deployment, production replay or migration occurred.

## Remaining qualification and next bundle

B7 closure does not imply full release readiness. Historical story-gitlink provenance, seven product/governance holds (DW-540/DW-562/DW-584/DW-593/DW-599/DW-617/DW-625), hosted TLS/Chromium evidence, upstream qualification, current-candidate dedicated-runner NFR10/performance, production IdP/secret-provider/revocation and authenticated browser/human AT evidence remain.

No dedicated performance qualification ran: this WSL environment has 14 available CPUs and does not establish the approved 4-vCPU/8-GiB runner contract. No production replay, migration, deployment, dependency update, staging, commit or push occurred.

Next selected deferred bundle: EventStore command-recovery integrity DW-499/DW-502. Current pointer `21b34c01ec919769cfba9cfe0d47e50886b57ded` still returns exact terminal duplicates without repairing advisory status (`AggregateActor.cs:861`), writes terminal advisory status after saving idempotency/cleanup (`:6710`/`:6774`), and publishes resume events even when `rangeProofVerified` is false (`:4681`/`:4685`). Preserve original command ranges and persisted evidence; work in the owning EventStore repository after resolving its recovery/quarantine contract. DW-517 remains a separate platform qualification item. These records are selected, not closed or rewritten by B7.
