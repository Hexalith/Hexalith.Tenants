---
title: 'Align bounded native verification with its enclosing transports'
type: 'bugfix'
created: '2026-11-03'
status: 'in-review'
route: 'dispatch'
baseline_commit: 'cea24f9a2cc513beef36d42805634e21ffe217a4'
owning_eventstore_baseline: '9aa4e5d690db3cce734ae38942b05c23c8380bac'
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
- Actual UI `Services/Gateways/TenantQueryGateway.cs` currently has MaximumEnrichmentDuration30, not45. Set its explicit whole supplementary-page budget60, preserving injectable deadline, search hydration8, concurrent list enrichment4 from the parallel pressure repair, and fail-soft behavior. This is a page bound, not a promise each of20 rows receives a full per-call budget. Keep actual deadline/caller cancellation tests and update stale comments/default-duration assertions only where necessary.
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
