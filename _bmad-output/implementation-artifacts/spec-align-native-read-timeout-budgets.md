---
title: 'Align bounded native verification with its enclosing transports'
type: 'bugfix'
created: '2026-11-03'
status: 'in-review'
route: 'dispatch'
baseline_commit: 'cea24f9a2cc513beef36d42805634e21ffe217a4'
owning_eventstore_baseline: '9aa4e5d690db3cce734ae38942b05c23c8380bac'
gitlink_audit_ref: 'ae40c9657175ae5567cdd182384d3840af64d8a2'
review_head: '401d926eb7d51bdf332696ed3f0fe39a812b3501'
review_resumed: '2026-11-13'
context:
  - '/home/administrator/projects/hexalith/tenants/AGENTS.md'
  - '/home/administrator/projects/hexalith/tenants/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '/home/administrator/projects/hexalith/tenants/references/Hexalith.AI.Tools/hexalith-git-instructions.md'
---

<frozen-after-approval reason="User authorized all necessary CI repairs, dependency updates and unattended publication">

## Intent

Make the already-selected30s stream read plus8s final native observation usable through the actual enclosing transports. They currently cut off the sequential verification at30s; the production owner client also inherits a10s attempt omitted by the test helper. Preserve full trust, cancellation, retry isolation, bounded fail-soft UI behavior and every existing live test while qualifying successful valid work beyond the former30s cutoff.

## Boundaries & Constraints

Do not weaken authority, catalog admission, proof, native removal, receipt, projection, skip or error assertions. Preserve original producer defaults for unrelated consumers. No broad resilience default change, infinite timeout, raw fallback, source substitution for package proof, additional retries, unbounded wait or fake runtime authority. Preserve all concurrent work and inspect target paths immediately before editing. Do not edit package versions, shared Builds/Platform cohorts, McpCli, historical evidence or reference skills. No staging, commits, pushes, AppHost/private runtime launches. Root owns exact-source CI, publication and cohort synchronization; normal individual builds and focused tests are allowed. Secret material must never appear in logs or arguments.

</frozen-after-approval>

## Code Map

- Owning repositories are root Tenants and root-declared EventStore. Read their relevant tracked configuration/build/tests before changes; preserve actual baselines above. EventStore4.0.3 is actually published and all14 archives verified; root will determine/publish the next immutable producer patch after this required repair, then align consumers. Do not invent package availability or mutate published tags.
- `src/Hexalith.Tenants/Program.cs` intentionally sets stream30 and metadata8. `TenantsGlobalAdministratorAuthority` awaits these sequentially. Keep those inner values.
- Root `src/Hexalith.Tenants/Authorization/TenantOwnerSourceGatewayRegistration.cs` currently adds only Dapr transport and inherits ServiceDefaults attempt10/total30. Chain the existing SDK AddEventStoreBoundedInvocationResilience on this owned typed client to give it one30s attempt/total and remove inherited resilience; unrelated clients retain their defaults. Match the real production defaults in `NativeOwnerReadServiceComposition.CreateServices` using the existing AddServiceDefaults registration and supplied configuration; keep genuine issuer, key privacy, actual custody and native30/metadata8 unchanged.
- EventStore `src/Hexalith.EventStore/Extensions/DomainQueryRoutingServiceCollectionExtensions.cs` registers named domain-query-invocation client with existing bounded30s resilience. Existing Microsoft options support named override `DaprDomainQueryInvoker.HttpClientName + "-standard"`; producer DomainQueryResilienceTests already exercises that seam. Extend the host composition so its query pipeline can use the already validated DomainServices:InvocationTimeoutSeconds when greater than30, retaining30 as its minimum/default. Keep the shared library AddEventStoreBoundedInvocationResilience default30 unchanged. Resolve effective validated DomainServiceOptions through DI, or add an optional host-only configuration seam if needed; preserve existing no-argument callers. Set attempt/total to the resulting bound and circuit sampling at least twice attempt. Root AppHost selects45 through the existing DomainServices setting; unrelated SDK consumers/default host still get30. No new public SDK API or standalone config knob is needed.
- Root AppHost Program currently defaults DomainServices:InvocationTimeoutSeconds30; change default45 while preserving operator overrides. Its resource-model tests must verify both the effective command setting and the EventStore host query setting is capable of extending above30 using actual registration. SDK existing DomainQueryResilienceTests/QueryRouterComposedRegistrationTests are the primary producer seams; prove default30, shorter command override does not reduce query30, and configured45 gives owned query45 without changing unrelated clients.
- Root API Program has an owned gateway policy30/30, sampling60 and stale10+2 comment. Use attempt/total50 and sampling100, correctly enclosing domain45 and native30+8. Keep inherited-policy removal, forwarding and unrelated clients unchanged. Update existing TenantsApiGatewayResilienceTests to verify real transport completion beyond the old30s boundary plus caller cancellation; prefer existing fake-time seams, or one bounded delayed test if needed. Do not merely change an option assertion.
- Active local Dapr policy is `src/Hexalith.Tenants.AppHost/DaprComponents/resiliency.yaml`, explicitly loaded by UI/API sidecars. Set eventStoreInvocation55s and tenantsApiInvocation60s; keep noReadRetry zero, actor/pubsub/write policies and all other limits unchanged. Synchronize matching `deploy/dapr/resiliency.yaml` template and set its existing gateway-to-tenants target to50s so it also encloses30+8. Update existing configuration/retry qualification checks for the new durations; do not change expected backend call counts.
- Actual UI `Services/Gateways/TenantQueryGateway.cs` initially had MaximumEnrichmentDuration30. The first live 20-row CI run completed 17 count reads before the 60-second page deadline and degraded the remaining three; the direct detail reads returned HTTP 200. Set the explicit whole supplementary-page budget to90 seconds, preserving the injectable deadline, search hydration8, concurrent list enrichment4 from the parallel pressure repair, and fail-soft behavior. This is a page bound, not a promise each of20 rows receives a full per-call budget. Keep actual deadline/caller cancellation tests and update stale comments/default-duration assertions only where necessary.
- Final review also requires small root CI evidence fixes: generic `scripts/run-aspire-integration-tier.sh` has actual609 passing cases but minimum1; require609. Authenticated REST suite has8 cases; require8. Determine actual complete AppHostSecurityResourceGraphTests executed count and set that lane's floor to the real current count. Retain all24 relocated live cases. Add real uniquely named TRX output and always-upload per matrix suite without dropping browser/bootstrap evidence.
- In the actual Sample-enabled Aha source-profile branch, read existing real committed catalog custody and assert exact four expected required owner identities (eventstore,eventstore-admin,tenants,tenants-api), so the new manifest-only Sample test is not mistaken for live owner exclusion proof. Do not add Sample as a receiver/owner.
- `docs/development-route-catalog.md` has stale current default3.133.0 and unconditional Sample omission. Correct current guidance to selected public4.0.3 and optional Sample/worker-backed qualification; retain explicitly historical3.133.0 evidence. Root updates the selected version again only after next actual producer publication.

## Tasks & Acceptance

- [x] Configure the owned producer query budget from validated existing invocation options with30s default/minimum; keep other consumers unchanged and verify real transport/registration isolation.
- [x] Align root native HTTP, API, AppHost, active/deploy Dapr and UI bounds as specified, preserving cancellation and retry semantics. Given a stream finishes29s and metadata2s, enclosing transports must permit completion; caller cancellation and genuine deadline exhaustion must still reach the actual transport.
- [x] Match native fixture production defaults, retain real custody, and add the real committed-custody assertion that the Sample-enabled cohort has only its four owner hosts; actual live execution remains an external release prerequisite. Keep all existing removal/background-delivery assertions.
- [x] Tighten actual CI floors, retain uniquely named TRX evidence, and correct current profile guidance. No suite or security control is removed or skipped.
- [x] Build individual changed test graphs with warnings as errors and run only relevant existing/new focused tests. Retain exact commands/results/logs under `/tmp/tenants-release-20261103-07JciR/deadline-fix-*`. Report local source vs public-package proof separately and any incomplete external qualification. Return changes to root; perform no external operations.

## Implementation Notes

- This repairs independently confirmed final-review findings, not a hypothetical risk. Current named/query/API/Dapr30s limits are reached by the actual production call graph; existing tests only held11s and asserted the old10+2 assumption.
- Preserve already passing609 generic integration controls,20 native fixture controls,2 profile models and all SDK4.0.3 CI/publication evidence. A separate parallel session may prepare McpCli dependency pins; leave it untouched.

## File List

- `references/Hexalith.EventStore`
- `references/Hexalith.Builds`
- `references/Hexalith.Platform`
- `references/Hexalith.FrontComposer`
- `references/Hexalith.McpCli`
- `references/Hexalith.Memories`
- `tests/Hexalith.Tenants.IntegrationTests/Fixtures/AspireTopologyFixture.cs` — resumed review: give only the protected UI page client a 150-second finite allowance.
- `tests/integration/qualify-dapr-read-retries.py` — resumed review: bind reports to both original and loaded resiliency policy bytes.
- `docs/production-auth-readiness.md` — resumed review: correct the remaining API caller-budget paragraph to 50 seconds.
- `_bmad-output/implementation-artifacts/evidence/native-read-timeout-review-2026-11-13/verification.json` — current review commands, results and validation limitations.
- `_bmad-output/implementation-artifacts/evidence/native-read-timeout-review-2026-11-13/final-review.json` — second independent review, retained-evidence audit and supplementary reproductions.
- `_bmad-output/implementation-artifacts/deferred-work.md` — append the three unrelated or unverified review groups without modifying prior entries.

## Verification

- All141 focused cases passed with zero skips: producer27, root owner/API/custody53, AppHost50 (security36 plus source-budget14), UI deadlines9, Dapr policies2. All five individual Debug/source graphs built with -warnaserror and zero warnings/errors. Exact commands/logs/TRX: `/tmp/tenants-release-20261103-07JciR/deadline-fix-handoff.json`, `deadline-fix-result-summary.json`, `deadline-fix-security-test-count.json`. These local checks do not establish public repaired-package or live runtime acceptance.
- Actual SDK transport completed31 seconds once, released every fence, and still enforces genuine timeout/caller cancellation. Configured45 extends only the query-owned pipeline; defaults/minimum30 and invalid0/3601 refusals pass. Native HTTP now permits a single bounded30-second attempt instead of inheriting10 seconds. Root API50, Dapr55/60 and UI supplementary page60 remain finite; the page retains fail-soft behavior.
- Root actual relative TRX path was checked under TestResults/authenticated-routes and matches the always-upload path. Floors are actual generic609, REST8 and security36; browser/bootstrap evidence is retained. Sample owner readback assertion compiles, with actual live execution still required.
- Producer commit e1667d7cd2042c947fe80906ba5349991f99da58 was created from only the two verified producer paths after exact full message validation with the locked commitlint. Evidence: `/tmp/tenants-release-20261103-07JciR/eventstore-query-budget-commitlint.log`. Pushed main; exact-source CI41574175275 and separate Integration41574174697 are running, Commitlint41574175307 already passed. No new producer publication is claimed yet.
- Concurrent root f5f65fc0bb755fb4af31427e4f62727d44f578e9 preserves actual tenant authorization by signing UI tests in with state.Token and bounds list enrichment4 while leaving search8. Original live failures were bodies, not bootstrap: native-removal/sample/anonymous UI passed; two populated-list REST failures remain pressure/availability, and seeded UI token mismatch is independently confirmed. Do not claim every HTTP500 was a30-second timeout.

## Parent Review Corrections

| Finding | Verdict | Evidence and disposition |
| --- | --- | --- |
| Deployed tenants target still cuts verification off at30 | medium; patched | Raised only deploy tenantsInvocation to50s. Existing two-case policy test resolves the actual target, requires its bound>38s, and preserves retry/breaker checks plus local target absence. Both cases passed with zero skips. |
| AppHost comment retains10+2 beside45 default | low; patched | Corrected to actual30-second stream plus8-second final observation without behavioral change. |

- Follow-up policy build passed with zero warnings/errors; two tests passed with zero skips. All other141-case evidence remains applicable because only deployment policy/comment and their two existing assertions changed.

## Automated Review Triage

| Finding | Verdict | Evidence and disposition |
| --- | --- | --- |
| Blind1: producer HttpClient100 cuts larger valid query setting | medium; patch | The new host policy can exceed100 using validated existing settings. Extend only its physical HTTP timeout with finite headroom, preserving default100 and all unrelated clients; cover configured120 and extended failure paths. |
| Blind2: operator command override60 exceeds fixed API50 | false | The command override does not increase the unchanged native30+8 work. This consumer's50s bound still encloses the intended verification; command and client latency bounds are separate. Preserve existing explicit command overrides and document the actual default hierarchy rather than introducing unrelated dynamic Dapr configuration. |
| Blind3: deployment template and readiness docs remain30/10+2 | medium; patch | Synchronize current template45 and documented native30+8/domain45/API50/Dapr50,55,60 limits, preserving historical evidence and secret placeholders. |
| Blind4: OIDC landing request retains30 | medium; patch | Authentication follows the return page after the genuine callback, so it can fail on valid slow data. Finish after the valid local callback has issued the encrypted cookie, or use a compatible finite landing bound; preserve origin/redirect/cookie checks. |
| Blind5: browser waits25 reject supported valid reads | medium; patch | Align functional navigation/readiness bounds with the supported finite page flow and exercise one controlled31s successful page. Keep independent performance thresholds and all readiness/authorization assertions. |
| Blind6: configured45 cancellation/exhaustion only tested atdefault | low; patch | Extend existing actual producer timeout/caller-cancellation controls to configured45; preserve default controls and real transport cancellation. |
| Blind7: new owned native-client cancellation/exhaustion behavior | low; patch | Extend existing production-registration transport controls to prove30s exhaustion and caller cancellation reach the terminal once, preserving no replay after cancellation. |
| Blind8: fully composed29+2 native-authority test | low; rejected | The unchanged real reader/metadata/authority composition already passed the live native-removal control. Existing authority tests protect required metadata/proof checks; the changed parent HTTP policies are directly exercised by held31s sends. Additional protocol fixture scaffolding duplicates unchanged sequential logic rather than exposing a missing changed caller edge. |
| Blind9: real Dapr delayed success and finite exhaustion absent | medium; patch | Group with the independent verified gap. Add actual31s HTTPS success and actual configured timeout refusal with exact body/status/backend-call counts. Preserve all13 original controls and the4-call503 mutation. |
| Verification gap: sidecar can load stale30 policy unnoticed | medium; patch | Extend the existing isolated Dapr/Sentry/HTTPS loopback qualification; no business AppHost or durable runtime is needed. Retain direct evidence and suitable caller deadline; do not substitute scalar checks. |
| Verification Other1: deploy Dapr README still5/30 | low; patch | Group with Blind3 and correct current operator guidance to actual supplied policy. |

Edge-case layer returned no findings.

## Review Follow-up

- The existing owned Dapr/Sentry/HTTPS loopback test is permitted for targeted policy qualification; it uses unique listeners/namespaces and synthetic HTTP responses, with no business AppHost or private business backend. Keep cleanup limited to its own resources. Run new long-duration success/timeout controls in normal qualification only if necessary to retain the existing13-case mutation contract exactly; report that profile distinction truthfully.
- Preserve existing production defaults and actual partial-state refusal. An additional validated120s producer setting must have a compatible finite HTTP bound; root default remains45 and caller cancellation remains authoritative.

## Review Evidence Correction

- Blind1 is false after actual factory inspection: Microsoft standard resilience already changes the physical HttpClient.Timeout to Infinite and enforces the finite named pipeline. The e166 producer does not have a100-second transport cutoff. Remove only the unnecessary new physical-timeout patch and101-second test; preserve the existing bounded policy and its original physical-handler semantics. Keep configured120 option/handler assertions and configured45 real cancellation/exhaustion coverage. No unbounded transport is introduced: the named attempt/total policy remains finite. This supersedes the earlier provisional medium/patch disposition and100-second premise without changing the intended default hierarchy.

## Review Triage Log

Resumed 2026-11-13 at `401d926eb7d51bdf332696ed3f0fe39a812b3501`. All three independent reviewers completed before triage. The required full baseline diff contains later unrelated changes; each finding below was checked against its actual callers and history. Verification-gap review reported no gaps.

| Finding | Verdict | Route and evidence |
| --- | --- | --- |
| Blind 1: protected UI HTTP fixture still stops at 60 seconds | medium | patch: `GetMarkupAsync` uses `TenantsUiClient`, whose resource reused the command client's 60-second bound. Select a private 150-second UI allowance, enclosing the supported 75-second enrichment and 91-second composed prerender. Command, REST, Sample and health bounds retain their values. |
| Blind 2: authenticated live-route capture hashes net10 outputs | medium | defer: both binary paths are still `net10.0`; the later .NET 11 migration `dc33fda90beea8ee9352b15cb061bf01dbf5f178` makes clean runs fail or bind stale outputs. This is a pre-existing migration defect outside the native-budget repair. |
| Blind 3: status-label validator launches a net10 test executable | medium | defer: the invoked script hardcodes `net10.0` while its shell caller now selects net11. Same later framework-migration cause as Blind 2. |
| Blind 4: four standalone browser launchers retain net10 outputs | medium | defer: administrator-dialog, navigation-focus, detail/audit-return and audit-layout scripts still select old host/dependency/evidence paths. Same later framework-migration cause as Blind 2. |
| Blind 5: workspace-focus evidence hashes a net10 assembly | medium | defer: this independent evidence capture also retains the former output path. Same later framework-migration cause as Blind 2. |
| Blind 6: zero-poll release acquisition budget includes signature/comparison time | medium | defer: `verify-release-artifacts.sh` sets a five-second cohort deadline before interleaved package comparisons. Slow native verification can leave later packages no immediate probe. Introduced by the separate release-verification work recorded in `8eaf6f601d82bcb4a92ed89aa1c2dcdc8815baa8`, not this timeout repair. |
| Blind 7: authentication changes during independent user-context resolution | maybe-false | defer, medium if reproduced: the resolver checks its change flag before `UserId`; the production circuit accessor makes another provider call. A deterministic same-subject revocation during that second resolution is needed to establish the alleged stale privilege result. This resolver change belongs to the later .NET 11 work, not this repair. |
| Blind 8: every Dapr target/template needs separate long-delay controls | false | reject: the existing two-case configuration test resolves the actual local/deployment target mappings and asserts 50/55/60-second durations plus retries/breakers. The real sidecar test qualifies loading and applying that same policy schema, with delayed success and actual exhaustion. No distinct changed wiring or reachable bad outcome was demonstrated; duplicating long controls for duration constants does not establish another missing behavior. |
| Blind 9: Dapr evidence omits original/loaded policy hashes | low | patch: preserve SHA-256 of the exact deployment bytes captured once and of the actual isolated policy file. Both ordinary and mutation profiles must retain them; retry mutation still changes only its temporary copy. |
| Blind 10: historical spec prose says concurrency four/page budget 60 | low | reject: the proposed fix edits this build's spec, which the review workflow rejects. Current source uses concurrency five and a 90-second page budget. The resumed verification below explicitly distinguishes current source checks from the historical live evidence; it does not relabel historical runs. |
| Edge 1: operator invocation overrides above 50 outlive fixed enclosing transports | false | reject: producer option validation does not promise an end-to-end API allowance of that size. The frozen intent encloses the selected 30+8-second native work; the default host/API/Dapr hierarchy does so. Current deployment guidance explicitly requires coordinating outer bounds with operator-selected SDK budgets. Preserving a shorter caller SLA is intentional, and no arbitrary-override completion guarantee is claimed. |
| Parent 1: API caller-credentials paragraph still states 30 seconds | low | patch: correct current operator guidance to the actual 50-second attempt/total policy and reference the documented enclosing Dapr bounds. |

### Second independent pass, 2026-11-13

All three layers reviewed the full original-baseline diff, including the existing uncommitted fixes, before triage. Repeated findings retain their earlier verdicts and routes; they are not deferred again. This pass changes no timeout implementation.

| Finding | Verdict | Route and evidence |
| --- | --- | --- |
| Blind B1: same-subject revocation during principal corroboration | maybe-false | carried defer from Blind 7. The new linked-source runner demonstrates stale administrator evidence when a substitute accessor emits revocation in its getter. Preserve that boundary reproduction; the production provider plus actual ServerCircuitUserContextAccessor combination remains unqualified. |
| Blind B2: zero-poll acquisition loses later immediate probes | medium | carried defer from Blind 6. Additional existing-fixture reproduction with five ready packages and simulated validation elapsed time fails at Fixture.Package1 after only one request. Exact script, output and hashes are retained in final-review.json. |
| Blind B3: source integration dependency graph mixes net10/net11 | medium | carried defer with the existing migration group. The unchanged source-build log records NU1201/NU1202 in FrontComposer/Memories; this repair changes no framework or dependency selection. |
| Blind B4: package UI generator emits obsolete Validate() | medium | carried defer with the existing migration group. The unchanged serialized Release/package fallback records CS0618 in generated CreateTenantCommand code with warnings as errors retained. |
| Blind B5: authenticated-route evidence hashes net10 binaries | medium | carried defer from Blind 2. The same two paths and their normal C# caller remain as previously recorded. |
| Blind B6: status-label browser script launches net10 executable | medium | carried defer from Blind 3. The child executable selection still disagrees with its net11 shell caller. |
| Blind B7: four standalone browser launchers retain net10 paths | medium | carried defer from Blind 4. The administrator-dialog, navigation-focus, detail/audit-return and audit-layout scripts retain the previously recorded paths. |
| Blind B8: workspace-focus evidence hashes net10 assembly | medium | carried defer from Blind 5. Its independent capture still retains the previously recorded path. |
| Blind B9: enrichment waits directly for a noncooperating provider | medium | defer as pre-existing. The original cea24f9a2cc513beef36d42805634e21ffe217a4 already awaits the same detail tasks inside Task.WhenAll. Current FrontComposerGatewayAuthorizationHandler also awaits circuit principal resolution without a cancellation-aware wait, so a pending authentication task can outlive page cancellation. Qualify that composed case and safe handling of late tasks separately; changing the native timeout constants did not introduce it. |
| Blind B10: every Dapr target needs a separate delayed test | false | carried reject from Blind 8. Actual target mappings and their durations/retry/breaker selections are checked by the two-case configuration test; the real sidecar controls exercise policy loading, delayed success and finite exhaustion. No new wiring defect was demonstrated. |
| Edge E1: principal corroboration needs a post-getter generation check | maybe-false | carried defer from Blind 7, with the same boundary reproduction and outstanding production-accessor qualification as B1. The reviewer supplied a proposed guard, not evidence that the existing source contains it. |
| Verification V1: authenticated-browser capture retains net10 paths | medium | carried defer from Blind 2. The verification layer independently traced the normal caller and reported no native-timeout verification gaps. |

## Resumed Review Scope and Evidence

- The historical pointer audit ends at the actual story-carrying merge `ae40c9657175ae5567cdd182384d3840af64d8a2`. Direct last-edit history initially suggested `8a2d001ae3980afea5bd57e28ba5ca03a0febeec`, but the repository's merge-aware guard correctly refused that endpoint because it omitted the subsequent merge. Six pointers differ from the original baseline at the actual endpoint: the original three plus FrontComposer, McpCli and Memories inherited by that merge, now explicitly listed for historical audit. Those inherited pointers are not claimed as timeout implementation edits. The original baseline is preserved; `review_head` identifies current source/test review. Later pointer updates remain outside this historical interval and this review moves no pointer.
- Current source retains native stream 30 seconds plus metadata 8, domain default 45, API 50, Dapr 50/55/60 and supplementary page 90; current list concurrency is five. The protected UI fixture now has its own 150-second request allowance, matching the existing functional navigation bound. No broader client default or retry policy changes.
- Review-local commands/logs are under `/tmp/tenants-native-timeout-review-rtfo741u/`; the earlier `/tmp/tenants-release-20261103-07JciR/` files are unavailable in this session and are not treated as fresh verification. Current results and blockers are retained in the File List's verification artifact.
- Full source integration validation is blocked by existing .NET 10 FrontComposer/Memories projects consuming .NET 11 dependencies. Serialized Release/package fallback reaches a separate generated `EditContext.Validate()` obsolete-use error (CS0618). These gates remain intact; no package selection, framework override, warning suppression, business AppHost or private business runtime is used to conceal the blockers.

## Completion Notes List

- FrontComposer, McpCli and Memories are declared solely because the actual story-carrying merge `ae40c9657175ae5567cdd182384d3840af64d8a2` inherited their pointers alongside the timeout work. They belong to the historical merge audit, not to the timeout implementation footprint. This declaration preserves the baseline, audits all six pointers at the merge-aware endpoint and changes no submodule checkout. Current source review remains bound independently to `review_head`.
- The story remains `in-review` while broader integration/UI validation is blocked. Focused source/model/browser/isolated-sidecar evidence is reported separately from a full business-topology or release qualification.
- Fresh review verification passed 185 cases with zero failures/skips: producer source 30, root server source 49, AppHost models 50, existing native/API transport controls in the focused current-package runner 35, Chromium controls 6 and real Dapr HTTPS controls 15. The separate 13-case retry mutation failed exactly its one-call assertion with four backend calls, as required. The final Dapr runs verify both original and actually loaded policy hashes. All four successful focused builds retained warnings as errors and reported zero warnings/errors.
- The historical endpoint passes both the merge-aware endpoint eligibility check and the production pointer validator; current `git diff HEAD -- references` is empty. Exact build/test commands, result counts, source/package distinctions and the unresolved full-build errors are retained in the verification artifact. Focused transport evidence does not claim that the changed UI fixture or the blocked UI C# test graph was executed.
- The second pass audited the retained 185-case results, all six build-log hashes, the three changed-source hashes and the current deployment-policy hash; they match. Python syntax, diff whitespace and the historical gitlink guard pass. These are evidence-integrity checks, not another 185 test executions. Its one newly identified pre-existing enrichment limitation is appended to deferred work; existing deferred entries and historical evidence remain intact. Full integration/UI validation remains blocked, so status stays in-review.
