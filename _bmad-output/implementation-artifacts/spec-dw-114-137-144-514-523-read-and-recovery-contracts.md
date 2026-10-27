---
title: 'Complete four deferred contracts and subscription cleanup synchronization'
type: 'bugfix'
created: '2026-10-26'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'cc0efc7df3399f1ea0e3789033ee75926bd06b35'
resumed_revision: '20100fc33b837fb5d85688afe3e3c6529c535080'
context: []
---

<frozen-after-approval reason="human-owned intent — user renegotiated scope on 2026-10-27">

## Intent

Complete DW-114, DW-144, DW-514 and DW-523 plus the unnumbered navigation-verification subscription-cleanup test follow-up, then commit and push the reviewed changes to main. The user selected this recommendation on 2026-10-27. These are five deferred items; four have numbered DW records.

Preserve the existing shared identity rules. Leave DW-137 open with its verified downstream blocker. Remove the unfinished route-encoding feature from this batch, retaining its patch/evidence for later work. The Dapr BFF uses the existing literal routes and client refusal of unsupported route identifiers.

## Boundaries & Constraints

Preserve authorization, opaque cursors, existing actor/storage identities, bounded reads and independent command/projection/audit truth. Reuse technical-module plumbing. Commit shared changes before parent gitlinks and validate every message with owning pinned commitlint. Never migrate data, deploy, manually publish packages, rewrite history, add silent read retries, widen the domain receiver's POST-only ACL or close unrelated records.

Keep intervening commits and unrelated staged/worktree changes. The retained original baseline covers this workflow's history; resumed_revision records the already-advanced workspace. Remove only this workflow's codec changes, never restore whole repositories or older dependency pointers.

## I/O & Edge-Case Matrix

| Input / state | Expected behavior |
| --- | --- |
| Indexed document missing a required field | Filter after raw paging; preserve raw total and reach later valid windows |
| Existing supported literal BFF route | Correct authorized GET, conditional/authority headers, no silent retry or direct fallback |
| Unsupported slash/backslash/dot-only route identifier | Existing client refusal before transport; shared identity policy unchanged |
| Cancellation followed by late backend setup | Await an explicit unsubscribe signal before assertions; exactly one unsubscribe and no retained callback |
| Null/empty/whitespace causation | Admission's MessageId fallback persists through recovery |
| PublishFailed, Retryable true/false/null | Retry-After only for armed recovery, without implying success |

</frozen-after-approval>

## Code Map

Paths are relative to owning Tenants, references/Hexalith.EventStore, references/Hexalith.Memories or references/Hexalith.Platform.

- Memories `tests/Hexalith.Memories.IntegrationTests/Search/SyntacticSearchIntegrationTests.cs` and `src/Hexalith.Memories.Contracts/V1/SearchResult.cs`: retained real-Redis window/total regression and contract documentation.
- EventStore `src/Hexalith.EventStore.Server/Actors/AggregateActor.cs` and `src/Hexalith.EventStore/Controllers/CommandStatusController.cs`: retained normalization and record-aware polling, with owning tests/docs.
- Tenants `src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryClient.cs` and its tests: remove only prior codec changes, retain original literal routing and guards. EventStore codec/generator additions are removed after saving a patch.
- Tenants `tests/Hexalith.Tenants.UI.Tests/Services/TenantReadRefreshSubscriptionTests.cs`: cancellation-after-late-setup test races background cleanup. Inspect actual detach/unsubscribe ordering in `src/Hexalith.Tenants.UI/Services/TenantReadRefreshSubscription.cs`; synchronize the test without changing production cleanup.
- Tenants `src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs`, AppHost, Dapr templates, model tests and integration fixture: retain tested invocation routing, HTTPS, zero-retry policy and identity bootstrap.
- Platform `DaprSelfHostedMtls.cs`, `DaprSelfHostedMtlsSubscriber.cs` and composition subscriber: retained shared local identity prerequisite, linked by Tenants.

## Tasks & Acceptance

- [x] Save the DW-137 codec patch/evidence, remove its production/test additions only, and retain literal-route compatibility. Correct deployment text describing this batch.
- [x] Stabilize the late-setup cancellation test with an explicit unsubscribe signal and bounded wait; preserve exactly-once and no-retained-callback assertions.
- [x] Verify retained Memories paging, EventStore recovery/status and Dapr/Platform implementation against current workspace revisions.
- [x] Update the evidence and four selected numbered ledger records after review; resolve the original unnumbered follow-up without deleting its historical finding. Preserve DW-137 open and every unrelated record.

Given late subscription cleanup, when cancellation has returned but cleanup is held, then the test waits for the unsubscribe signal and verifies callback retirement rather than racing scheduling.

Given a supported literal identifier, when the BFF reads through Dapr, then the original request/auth/authority contract holds; given an unsupported identifier, when the client validates it, then no ambiguous request is sent.

Given invalid indexed entries, when raw paging advances, then later valid results retain the original raw total.

Given blank causation or armed publication recovery, when recovery/status executes, then the retained identity and polling contracts remain consistent.

## Implementation Notes

The parent already rendered bmad-build and retains orchestration, independent review, ledger acceptance, commits and pushes. Implement directly from this spec; do not render another workflow. Original investigations/tests remain useful historical evidence, but current revisions advanced through external commits and require current relevant verification.

The shared Platform bootstrap is necessary for local deny-default peer policies. Preserve its canonical source-link reuse and production authorization boundary. Existing baseline broad build/provenance failures are recorded separately from focused evidence; no unrun test is acceptance evidence.

## Spec Change Log

- 2026-10-27: User applied the recommendation to preserve shared identity restrictions and substitute the recorded cleanup-test synchronization follow-up for DW-137. Keep the four verified implementations, Platform prerequisite, historical evidence and intervening commits. Remove unfinished encoding plumbing; no identity-policy expansion or data migration.

## Review Triage Log

| Finding | Verdict | Route | Evidence and required correction |
| --- | --- | --- | --- |
| BH1 | medium | patch | API/UI ResourcesPaths include unscoped llm.memories and secretstore.memories; unlike the scoped Redis components these can initialize for the new readers. Scope the two Memories components to their owning app. |
| BH2 | medium | patch | The new Compile/ProjectReference paths are fixed under references, while Directory.Build.props/targets support sibling Builds/module layouts. Resolve the canonical helper/tooling roots consistently; source-linking itself is the approved shared prerequisite, not a package-mode defect. |
| BH3 | medium | patch | AddLocalApplicationInfrastructure unconditionally replaces annotation.Options.RuntimePath, so an explicitly selected valid installation is ignored. Preserve its value before applying the default. |
| BH4 | medium | patch | Program still states explicit placement/scheduler configuration wins, but the local identity bootstrap replaces it. Isolated local mTLS requires its owned control plane: reject incompatible explicit settings diagnostically and correct that stale contract rather than silently ignore them. |
| BH5 | low | patch | A configured certificate parent lies outside the run directory. Failure after AddSentry currently cleans only the latter; delete both owned paths on failed composition. |
| BH6 | medium | patch | SyntacticSearchService normalizes MaxResults and HTTP normalizes separately, so newly documented requested-window increments can skip results above the caps. Document effective normalized windows and both limits. |
| BH7 | medium | patch | The retained live probe sets allSixPresent but allPass checks only recorded rows and process exit is always success. Empty lists can omit five reads and failures do not fail the command. |
| BH8 | medium | patch | The retained live probe hardcodes a checkout and requires an unretained temporary Aspire JSON file. Collect fresh state or accept explicit inputs so the maintained qualification is reproducible. |
| BH9 | medium | patch | The live six-read test directly constructs the client and supplies routing headers, bypassing production DI registration. Resolve the registered typed client with live transport and caller authorization. |
| BH10 | medium | patch | ConditionalDetailReadRecognizesPersistedAuthority still targets the direct API endpoint. Exercise the current Dapr read transport so actual generated 304 authority is covered. |
| BH11 | medium | patch | LocalDaprIdentityTests invokes generated arguments but no generated environment callbacks; removing credential/trust bindings would leave it green. Assert the executable receives the expected model-only credentials and identity environment. |
| EC1 | low | patch | Confirmed independently of BH5: the certificate-parent configuration can create a private child outside runDirectory; the catch path leaves that child behind. |
| EC2 | medium | patch | Confirmed independently of BH6: SearchPaginationOptions.NormalizeCandidateSize caps direct reads, while public endpoints cap their requested size; guidance must use the effective size. |
| EC3 | medium | patch | Confirmed independently of BH7: allSixPresent does not govern allPass or exit status. Fail incomplete and negative qualification. |
| VG1 | medium | patch | Pre-verified regression gap: maintained live route tests cover allowed requests, while caller/verb denials exist only in retained probes. Add the two real-sidecar negative controls to the blocking route lane. |
| VG2 | medium | patch | Pre-verified broken-verification gap: recording HttpMessageHandler counts cannot observe Dapr internal retries. Promote the existing controlled real-sidecar 503/count qualification to a maintained blocking CI check, retaining the retry mutation rejection control. |
| P1 | low | patch | Parent staged diff check flags the required blank context lines in preserved text patches. Preserve their exact bytes in a compressed archive so source whitespace checks remain meaningful without broad exemptions. |

| P2 | medium | patch | The source harness fix is absent from published 3.128.2. Narrow legacy standard loopback hints are now accepted with explicit supersession diagnostics; custom/external endpoints still reject. Twelve compatibility/rejection rows and all 30 package-mode model tests pass. |
| P3 | low | patch | A newly added test parsed the normalized metadata tag as raw quoted HTTP syntax. Removed that incorrect assertion; the production client already rejects weak wire tags before populating metadata, and the full live suite passes 6/6. |

All findings were classified separately before grouping. Shared roots: BH5/EC1, BH6/EC2, BH7/EC3. No finding is deferred or discarded. The corrections preserve approved identity, transport and asynchronous cleanup intent and add no runtime public API.

## Verification

Use current relevant owning Debug/source test projects and maintained MTP commands. If broad builds remain blocked, climb the baseline fallback ladder, preserve exact commands/blockers and qualify focused unchanged source without hiding the broad gate. The cleanup test must run successfully; use a held-unsubscribe control to prove it waits deterministically.

Rerun the literal BFF client/composition and AppHost/shared-binding checks after codec removal; qualify ordinary mTLS Dapr reads/denials as needed via Aspire. Existing full Server and Redis evidence may be reused only when relevant source/artifact hashes still match; otherwise rerun their focused owning lanes.

Record all root-declared dependency movements and reasons. Validate the story gitlink guard and exact commit messages, fetch/inspect divergence, and complete the authorized main pushes after independent review.

## File List

- `tests/integration/qualify-dapr-read-retries.py`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `docs/maintenance-backlog.md`
- `Directory.Build.props`
- `.github/workflows/ci.yml`
- `_bmad-output/implementation-artifacts/dw-114-137-144-514-523-validation-evidence.md`
- `_bmad-output/implementation-artifacts/evidence/dw-114-137-144-514-523/manifest.json`
- `_bmad-output/implementation-artifacts/evidence/dw-114-137-144-514-523/validation-records.tar.gz`
- `_bmad-output/implementation-artifacts/spec-dw-114-137-144-514-523-read-and-recovery-contracts.md`
- `deploy/dapr/README.md`
- `deploy/dapr/accesscontrol.tenants-api.yaml`
- `deploy/dapr/accesscontrol.tenants-ui.yaml`
- `deploy/dapr/resiliency.yaml`
- `references/Hexalith.EventStore`
- `references/Hexalith.Memories`
- `references/Hexalith.Platform`
- `src/Hexalith.Tenants.AppHost/DaprComponents/accesscontrol.tenants-api.yaml`
- `src/Hexalith.Tenants.AppHost/DaprComponents/accesscontrol.tenants-ui.yaml`
- `src/Hexalith.Tenants.AppHost/DaprComponents/resiliency.yaml`
- `src/Hexalith.Tenants.AppHost/DaprComponents/sentry.yaml`
- `src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj`
- `src/Hexalith.Tenants.AppHost/Program.cs`
- `src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryClient.cs`
- `tests/Hexalith.Tenants.AppHost.Tests/AppHostSecurityResourceGraphTests.cs`
- `tests/Hexalith.Tenants.AppHost.Tests/LocalDaprIdentityTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/AuthenticatedTenantsRouteTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/Fixtures/AspireTopologyFixture.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/TenantReadRefreshSubscriptionTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs`
- `_bmad-output/implementation-artifacts/evidence/dw-114-137-144-514-523/recommended-resume/`: preserved codec patches and current qualification records.

## Completion Notes

- `references/Hexalith.EventStore`: retain external movement from `935c21ac5ebc5e50738ae6ee79dc661acd2fd5a8` to `29f9ec2d2214292a5abb71370bb24adfbe063189`, then the owning recovery/status fix.
- `references/Hexalith.Memories`: retain external movement from `cdd73d7361f67781a523f63f04853491a687f426` to `22b792cf4f4a7aa5c9b6f129e6a491821169a8bf`, then the owning search regression/documentation.
- `references/Hexalith.Platform`: retain external movement from `eecea3fa467d27226ef6173c19f853add36bb26c` to `73bebc0a336ebf68cee77ab0114d96dde66c386d`, then the shared local mTLS prerequisite.
- Existing unrelated Catalog test commits remain intact and outside this implementation review.

## Final Acceptance

All review corrections are applied. Current unchanged UI source: 7,839/7,839; current real Redis: 19/19; rebased EventStore recovery/status/provenance: 123/123; shared harness defaults: 4/4; restored generator: 124/124; complete package-mode AppHost: 30/30; maintained production-DI real-sidecar routes: 6/6; controlled Dapr: 13/13. Every listed passing run has zero skipped/failed cases. The retry mutation fails as required with four backend calls. Earlier failed attempts and broader gates remain explicit in the evidence.

Exactly four numbered records and the original unnumbered cleanup follow-up close. The ledger audit proves all 657 IDs remain, 624 done/33 open, 29 index rows, and all 653 unselected numbered bodies (including DW-137) remain byte-identical. Original findings/fields remain retained. No new review follow-up is deferred.

Concurrent EventStore content and Platform story commits are preserved through clean rebases; all three owning commits were validated with their pinned commitlint and pushed before parent pointer updates.

- `references/Hexalith.EventStore` final target: `b1dd186ad49aba1c0c21d452c75fc7769feba1da`.

- `references/Hexalith.Memories` final target: `2bfc735600ed153bce9069570a43ba93fe77ac0f`.

- `references/Hexalith.Platform` final target: `f187f346bb2a1136f4be7221af82136f779c2928`.
