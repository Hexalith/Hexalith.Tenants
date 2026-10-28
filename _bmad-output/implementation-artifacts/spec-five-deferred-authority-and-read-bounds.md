---
title: 'Five deferred authority, read-boundary and CI items'
type: 'bugfix'
created: '2026-10-28'
status: 'in-review'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '8127cb3b47dcfcf4086d07e4af0730c2f4920f9e'
context: []
---

<frozen-after-approval reason="human-owned intent — recommendations approved by the user on 2026-10-28">

## Intent

Complete five deferred records, independently review and verify the changes, then commit and push main as requested. Selected: DW-562, its duplicate DW-617, and three unnumbered follow-ups: fatal failure during caller retirement; slow authorized-search backfill; fresh generic Aspire CI qualification. Two records share one revocation implementation. The explicit five-item request retains this batch scope.

## Boundaries & Constraints

Preserve caller binding, tenant-owner authorization, protected cursors, event-sourced persistence, domain/platform ownership and all unrelated records. Never substitute projection write age for source lag or return a retired caller's data. Use existing authenticated native-stream readers; no direct Dapr persistence, authority cache fallback, data migration, deployment, manual release or identity-grammar expansion. DW-137 and assistive-technology holds remain separate.

## I/O & Edge-Case Matrix

| State | Required behavior |
| --- | --- |
| Removal committed; projection still grants | Apply the explicitly approved revocation policy at commands and all query gates. |
| Unknown/gapped/changing source or proof failure | Follow the approved authority-unavailability policy; never guess membership. |
| Fatal dependency error and caller/cancellation transition | Apply the approved precedence without returning protected results. |
| Slow windows, hydration or fallback | Enforce the approved deadline boundary and duration; discard incomplete evidence. |
| Fresh CI qualification | Run the complete configured non-Performance tier with no skips; preserve exact provenance and failures. |


## Approved Decisions

The user instructed “apply recommendation” after the detailed tradeoff discussion; all recommended policies and this five-record implementation are approved.

- Revocation uses authenticated event-source membership at its final verified source observation. Fail closed on unavailable, malformed or over-bound evidence; operations already admitted before removal may finish. Narrowly enroll only the fixed global-administrator source. Preserve independent tenant-owner permissions. Reuse verified results within a request when appropriate, with no cross-request authority cache. Validate representative replay cost before acceptance.
- Preserve original fatal failures and their wrappers across concurrent caller retirement/cancellation. Keep the existing narrow fatal classification and reject all retired-caller results. Pre-cancelled work still stops before dependency execution.
- Use one configurable ten-second budget for the complete search load, including index windows, hydration and any fallback. Exhaustion returns safe Error/Retry and cancels outstanding work; late evidence cannot update current state. Measure controlled slow windows and cancellation to validate the initial value; no general latency SLA is claimed.
- Qualify the existing complete generic Aspire lane with workers enabled and no skips. Keep generic source exceptions and explicit published-package evidence distinct. Only actual passing complete CI evidence closes its follow-up.

</frozen-after-approval>

## Code Map

Paths below are repository-relative. Existing source seams settle implementation ownership; the policies above are approved.

- `src/Hexalith.Tenants/Authorization/TenantsGlobalAdministratorVerifier.cs`, `TenantOwnerSourceGateway.cs`: current membership-only verifier and tenant-only native transport.
- `src/Hexalith.Tenants/Queries/Handlers/TenantQueryHandlerBase.cs`, `GetGlobalAdministratorsQueryHandler.cs`: independent projection-based grants; changing the verifier alone leaves a bypass. Existing `TenantOwnerAuthorityQueryHandler.cs` and `TenantReadAuthorityQueryHandler.cs` demonstrate folding/final proof.
- EventStore `IAuthoritativeEventStreamReader` and `INativeSourceMetadataReader`: reuse complete bounded authenticated reads and native incarnation/digest checks. The fixed identity is system/global-administrators/global-administrators.
- `src/Hexalith.Tenants.UI/Services/Gateways/CallerBoundTenantsRestQueryClient.cs`, `TenantQueryGateway.cs`, `TenantWorkspaceReadDiagnostics.cs`: finally-check masking, outer transition containment, sequential search windows and recursive fatal classifier.
- `.github/workflows/ci.yml`: whole generic tier via Builds; defaults retain the available FrontComposer/Memories source exception. Generic and all-package results need distinct provenance.

## Tasks & Acceptance

- [x] `src/Hexalith.Tenants/Authorization/TenantsGlobalAdministratorAuthority.cs` and its interface: implement the approved authority contract using shared readers; fold known global-administrator events and verify final native proof.
- [x] Existing verifier, query base/derived constructors, administrator-list gate, `Program.cs` and native gateway: consume one authority service and narrowly configure fixed-source transport. Update `docs/production-auth-readiness.md` and deployment/configuration templates accordingly.
- [x] UI gateway/client and configuration registration: implement the approved exception and deadline policies; record per-window/load timing before selecting final settings.
- [x] Owning Server/UI test files: hold stale projection, source changes, fatal completions, caller transitions and slow windows deterministically; preserve tenant-owner paths and exactly identify original fatal instances.
- [x] Integration project and `.github/workflows/ci.yml`: freshly restore/build required hosts; qualify the entire generic tier with workers enabled and existing strict TRX/no-skip gates. Record package/source graphs separately and retain exact blockers.
- [x] Prepare the validation-evidence artifact, source/assembly hashes and complete qualification records for independent review; preserve original findings and all unrelated ledger bodies for orchestrator acceptance.

Given removed source membership and a stale granting projection, when a new global-only command or any query is authorized, then the approved source contract governs refusal; an independent tenant-owner permission remains effective.

Given held fatal reads or slow windows, when authority changes or time expires, then the approved precedence/deadline applies without old-caller evidence, incomplete search truth or an unbounded replacement recovery attempt.

Given the final implementation, when qualification runs, then actual executed results and immutable source/package identities support each claimed closure; failed or skipped qualification remains open.

## Implementation Notes

2026-10-28: The user approved applying the detailed recommendations and continuing the implementation. The implementation handoff owns code and local verification, proceeds sequentially and does not stage, commit, push or perform remote operations; the orchestrator owns independent review, final ledger acceptance and the authorized main push. Report any whole-tier blockers with exact commands and results rather than changing gates. Record local evidence under evidence/five-deferred-authority-and-read-bounds/ and the corresponding validation-evidence Markdown file.

2026-10-28 implementation finding: EventStore disables actor reentrancy and native stream reads call the source actor. The orchestrator approved always refusing the unused administrator hint in the global-administrators domain: management commands retain their existing locked/replayed-state membership gate, eliminating a same-actor `/process` cycle. Global queries still independently read native authority and tenant commands still use the shared service. Tests cover current, removed and spoofed actors plus zero source self-calls.

2026-10-28 qualification finding: the first complete generic tier executed 510 tests (488 passed, 22 failed, zero skipped); bootstrap authority timed out against a pre-existing local global stream whose native inspection reported Unsupported at sequence 172 with no proof. The orchestrator approved paired trusted component-path overrides through the existing platform helper and a fresh isolated Redis qualification run. Defaults and legacy data stay intact; no migration, reset, proof backfill or projection fallback is authorized.

2026-10-28 populated-load finding: the isolated complete tier ran 510 tests (508 passed, two failed, zero skipped). A clean UI-only route control passed all six protected HTTP surfaces, but one normal production 20-row list reproduced verified global-actor quota saturation and returned 20 rows with only eight known count pairs. Isolation is not a remedy for this realistic fan-out. Authority remains fail closed and ceilings/retries are unchanged. The orchestrator requested a diagnostic bounded queue64 control, with the same 1000/60s tenant and 100/1s consumer permits; the control alone does not authorize a permanent configuration change.

2026-10-28 tuning approval: after queue64 returned all 20 current count pairs in 624.98ms with unchanged permit ceilings/windows, the orchestrator approved that existing setting as the AppHost default while preserving explicit operator overrides. Deployment guidance recommends the same bounded queue. Authority freshness, request-only reuse and retry policies remain unchanged. Run/publish model controls and a complete generic rerun without diagnostic queue environment pins qualify the accepted default.

2026-10-28 owner-permission finding: real HTTP detail/users requests with independent owner membership returned503 when an untrusted administrator hint forced SDK native verification against unavailable global evidence. The orchestrator approved stripping only the unused hint on the server-matched canonical query POST/policy metadata, retaining all handler-native gates and tenant-command checks. Delegated native-read queries retain their original canonical wire-hint refusal using the SDK OriginalQuery. HTTP unavailable-source/spoofed-hint controls cover all six gates and independent owner reads.

2026-10-28 final local qualification: Server1617, UI7997, exact gateway820, AppHost39 and focused real HTTP21 all passed with zero skips. The final strict complete generic tier executed528 tests,528 passed, zero failed/skipped, in2m56.593s, with workers enabled and queue64 from the accepted AppHost default. Earlier failed whole-tier reports remain retained; actual post-push CI and final ledger acceptance remain orchestrator work. Sanitized bulk evidence is bundled with raw/sanitized hash receipts; raw captures remain outside the repository.

2026-10-28 post-review verification: all three independent layers completed and each finding was triaged before grouping. The callback-only Aspire parameter overload disproved the original run-key mismatch claim; its ignored-configuration gap and the separate publish defect were corrected and distinguished. Accepted corrections use existing seams. Final owning Server1624, UI8011 and AppHost42 suites pass without skips; the fresh complete generic rerun passed529/529 with zero failures/skips, and actual post-push CI remains pending. All six explicit published prerequisite builds pass after whitespace-only formatting of five test files; production runtime inputs remain unchanged. Four selected implementation records close; all655 unrelated numbered bodies are byte-for-byte unchanged.

2026-10-29 CI qualification follow-up: implementation commit22644726 was pushed. CI's generic Aspire job was skipped after the reproduced dependency-probe failure and a parallel shared-directory cleanup assertion; the browser job separately timed out after16 successful cases. Source-reference CI retains its pre-existing McpCli floor blocker. The two test-only corrections pass Release UI8011 and AppHost42 without an inherited EventStore version pin. A mistakenly selected stale Debug verification was stopped after2921 passing tests and is excluded from acceptance. Fresh exact-source generic CI remains required; no fifth-record closure or runtime cause is claimed.

## Spec Change Log

## Review Triage Log

All three independent layers completed before triage. Findings are recorded individually below, before grouping by root cause. The frozen approval remains unchanged. Each accepted correction uses existing configuration, transport or diagnostic seams; no new endpoint, authority policy, cache, retry or public contract is required.

| Finding | Verdict | Verification and route |
| --- | --- | --- |
| Blind 1 — overridden source private key disagrees with generated public key | false | Rechecked after the focused override control failed: installed Aspire.Hosting 13.6.4 documents the Func overload as callback-only, while the ParameterDefault overload explicitly reads configuration first. The original Func callback ignores the configured override, so both original run bindings still use the same generated pair; this specific mismatch claim is rejected. The distinct ignored-configuration gap is recorded separately below. |
| Blind 2 — persistent Keycloak realm does not acquire the new client | medium | The existing KeycloakPersistent path reuses its database; Keycloak startup import skips existing realms. The new client and generated secret therefore need explicit enrollment. Patch the existing operator guidance with a targeted client/scope update and safe verification procedure. Performing a production realm upgrade remains excluded by the approved no-deployment boundary. |
| Blind 3 — fatal cancellation wrapper swallowed in enrichment | high | EnrichRowsAsync catches OperationCanceledException without the fatal exclusion used by its sibling catch. An active parent token permits an OCE with a fatal inner cause to become degraded counts, including search fallback. Patch the filter and cover that exact path. |
| Blind 4 — optional timing logger replaces provider failure | medium | Both new timing helpers invoke the logger from finally with no containment. A nonfatal logger exception can replace an original fatal provider exception. Patch only optional timing diagnostics to tolerate nonfatal logger failure, preserving fatal classification. |
| Blind 5 — reflected fatal Apply failure becomes unavailable | high | The new authority catches every TargetInvocationException from MethodInfo.Invoke, including wrapped allocation failures in HashSet.Add. Patch by removing that broad reflection-wrapper translation; malformed JSON/argument handling remains. |
| Blind 6 — malformed rejection payload accepted | medium | The three user-bearing rejection records require UserId, but the missing-field condition only names set/remove. Missing required rejection fields deserialize as null and replay as a no-op. Patch required-field validation for the known record shapes and add malformed-rejection controls. |
| Blind 7 — deadline drops discovered cursor invalidation | medium | cursorRecovered is local to the inner search method; the outer timeout returns a bare Error. Workspace clears protected search history only from PagingRecovered/SearchRefreshed, so the discovered invalidation is lost. Patch by sharing the existing per-load flag across this boundary and test invalid-cursor expiry. |
| Blind 8 — no same-host grant/removal/refusal regression | medium | NativeRemovalRefusesTheRealProcessAndQueryBoundaries appends removal before constructing its factory. The separate instance-level authority test cannot reject a cross-request DI cache. Patch that HTTP test to prove grant, mutate the source, then refuse on the same host. |
| Blind 9 — native revocation transport lacks live Dapr control | medium | The removal boundary fixture replaces both native readers; the signed replay fixture uses a capture handler. Existing live Dapr controls establish current grants but do not mutate a grant and retain stale projection evidence. Patch the existing live fixture with a persisted-removal control through the production readers; preserve strict no-skip qualification. |
| Blind 10 — production logs cannot identify budget expiry | medium | The budget boundary returns generic Error and emits only Debug timing. Normal Warning logging cannot separate an exhausted configured bound from transport failure. Patch a fixed identifier-free expiry diagnostic using the existing logger, without logging caller cancellation as expiry. |
| Edge 1 — throwing cancellation callback escapes timer | high | The timed CTS executes linked callbacks synchronously; the reviewer reproduced an unhandled AggregateException on TimerQueueTimer.Fire using the exact structure. A provider callback is reachable through the supplied token. Patch the timer to observe CancelAsync completion/failure rather than allowing exceptions to escape its thread, and cover the demonstrated callback. |
| Patch validation — sibling enrichment timer shares unsafe callback execution | high | The read-boundary callback regression also reaches pageDeadline.CancelAfter during ordinary/fallback enrichment. That timer can independently execute the same throwing provider registration before the whole-load deadline. Reuse the observed asynchronous cancellation helper for this existing per-page bound and verify the demonstrated timer path; no bound duration or public surface changes. |
| Edge 2 — publish discards matching private key | high | The reviewer generated the real manifest: the random public key is a literal while the secret parameter has no matching persisted/default value. Patch publish mode to rely on existing operator key enrollment, with no ephemeral key bindings in the manifest. |
| Verification 1 — configured nondefault search duration not exercised | medium | Pre-verified gap: current deadline tests construct options directly, while the composition test neither sets a budget nor searches. Removing Bind would pass both. Patch a production-DI/manual-clock regression with a nondefault setting. |
| Verification 2 — non-search fatal containment not exercised | medium | Pre-verified gap: existing administrator tests supply ordinary failures; fatal tests use search or replace the whole gateway. Removing the administrator-read fatal filters would pass. Patch direct and wrapped fatal transport tests asserting original instance identity. |
| Focused implementation validation — configured run key ignored | medium | The new override control fails against the callback-only AddParameter overload because it bypasses configuration. An operator's enrolled existing parameter is silently ignored. Patch using the same parameter name with configuration-aware value selection, then derive the run verifier from that effective key. Installed API inspection and the focused red/green control distinguish this gap from Blind 1's rejected mismatch claim. |
| CI qualification — dependency-closure source probe blocks Tier 3 | medium | Exact-source CI40783107965 ran UI8011 with one failure: the true-source restore probe selected McpCli producer EventStore3.128.3 below the unchanged3.128.4 floor. The exact case reproduced without environment pins. Patch only that restore subprocess with the already supported McpCli package exception; all closure assertions and the separate source-reference workflow remain unchanged. |
| CI qualification — shared-directory cleanup assertion races model tests | medium | CI's LocalDaprIdentityTests compared every new shared /tmp/hexalith-dapr directory to its before snapshot while other model classes create independent runs. Its assertions can count those unrelated directories. Patch the filesystem-sensitive collection to disable parallelization against other collections, preserving the real owned cleanup assertions. |
| CI qualification — browser timeout does not identify its awaited boundary | low | The retained failure phase tenant-list-layout-1440-none covers both open/network-idle and seeded Current readiness. Two fixed phase assignments directly correct this diagnostic ambiguity without changing any wait, timeout or assertion. Patch; node syntax verification passes. |
| CI qualification — cause of published-profile browser timeout | maybe-false | Actual CI passed16 browser cases, then timed out before the1440 list check. Captured native transport reads returned200; this does not establish a product regression, rate saturation or a pre-existing cause. Settle with the finer boundary labels, fresh exact-source qualification and a same-corpus baseline/control if it recurs. Defer as an unverified medium runtime risk; preserve the original result and no claimed runtime fix. |

After these individual verdicts, Blind 1 is rejected. The sibling enrichment timer and Edge 1 share synchronous timer-thread callback execution and form one cancellation group. Edge 2 and the separately observed configuration gap have distinct causes and remain separate patch entries, as do the other accepted findings. No finding requires a frozen-intent change or new public surface. The initial three-layer review deferred no finding. The later published-profile CI browser timeout is separately retained as an unverified medium follow-up; the two demonstrated CI test-boundary defects and diagnostic ambiguity use the small patches above.

## Verification

Run owning Debug/source Server and UI projects individually, focused deterministic regressions followed by relevant full suites. Qualify real process/query boundaries with stale projections and persisted end-state checks.

Fresh generic tier: mirror the configured Release restore/build graph, then run the Integration project with `--no-build --configuration Release --filter-not-trait 'Category=Performance' --minimum-expected-tests 1 --fail-skips on` and TRX reporting. Keep workers enabled; install browser/TLS/Dapr prerequisites. Record a separate explicit published-package prerequisite build. Observe the final push's actual CI attempt before claiming fresh blocking CI acceptance. Never weaken gates or substitute focused routes for the full tier.

## Orchestrator Completion

- [x] Independently review and resolve findings against the staged implementation and matrix coverage.
- [x] Accept the two numbered revocation records and two unnumbered read-boundary records, preserving all other numbered bodies.
- [x] Validate the exact commit message with pinned commitlint, fetch/inspect divergence and push the reviewed implementation to main.
- [ ] Observe actual exact-source CI, then close the unnumbered generic-CI follow-up and push the final acceptance evidence.

These ordered completion actions remain part of the original intent; they follow implementation verification and independent review rather than being claimed complete in the implementation handoff.
