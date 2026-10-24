---
title: 'Resolve five deferred runtime authentication and HTTP contract records'
type: 'bugfix'
created: '2026-10-23'
status: 'done'
route: 'dispatch'
baseline_commit: 'a95d1039db3fb1004a93cee871e0358dd41c7b9d'
review_loop_iteration: 0
context:
  - 'AGENTS.md'
  - 'references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
---

<frozen-after-approval reason="user delegated five deferred work items and publication to main">

## Intent

**Problem:** The ledger retains missing runtime proof for Keycloak workload credentials/project scope (DW-569, DW-592), missing EventStore app-channel protection (DW-571), and generated-route ETag/status-proof assertions confined to advisory CI (DW-572, DW-577).

**Approach:** Complete these five related records with the existing platform token helper, real Keycloak authentication evidence, and blocking in-process HTTP contract coverage. Commit and push to main after verification and independent review, as requested.

## Boundaries & Constraints

**Always:** Reuse EventStore hosting/authentication infrastructure. Preserve the advisory Aspire tier and HTTPS contract. Keep user bearer authentication separate from Dapr app-channel authentication and workload authorization. Prove production HTTP producers agree with production UI consumers. Retain original ledger findings and exact canonical revisions. Use Debug source references locally and the existing Release/package CI contract. Keep secrets and tokens out of tracked evidence and tool output.

**Never:** Add a proprietary test authentication bypass to production; initialize nested submodules; change dependency pointers or publication policy; dispatch business commands for live evidence; overwrite user data; claim a CI execution or runtime assertion that did not run. Unrelated DW-594 and policy decisions remain open.

## I/O & Edge-Case Matrix

| Scenario | Expected behavior |
| --- | --- |
| EventStore, Tenants and sample resources, either auth mode | Each app shares a secret token with its own sidecar; tokens differ between apps |
| Internal EventStore channel request without/wrong token | Rejected; matching token reaches non-mutating validation on the existing protected notification route |
| Actual Keycloak client_credentials grant | Token grants the intended workload caller, audience and project operation |
| Protected Tenants project request with valid workload assertion and channel | Authorized valid request succeeds without changing business state |
| Missing/wrong channel or missing workload assertion | Same project request rejected before projection execution |
| Generated API response and conditional read | HTTP ETag and freshness agree; supported conditional behavior preserved |
| Completed command status through real producer and UI gateway | Message identity, aggregate/domain, event count and committed sequence survive; malformed responses withhold committed-sequence proof |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Tenants.AppHost/Program.cs`: AddHexalithEventStore attaches the sidecar; then reuse WithGeneratedEventStoreAppChannelToken, already used by tenants/sample. Keycloak gets the same generated workload-client-secret as EventStore.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.Aspire/HexalithEventStoreAppChannelExtensions.cs`: existing app/sidecar token primitive, read only.
- `tests/Hexalith.Tenants.AppHost.Tests/AppHostSecurityResourceGraphTests.cs`: evaluate environments and assert secret parameter identity in both authentication modes.
- `tests/Hexalith.Tenants.IntegrationTests/TenantsApiGeneratedControllerTests.cs`: real API WebApplicationFactory, authenticated clients, generated ETag/304 and REST-client cases; reusable in-process coverage currently advisory.
- `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs`, `Fixtures/MessageIdentifyingCommandStatusStore.cs`, `AspireTopologyTests.cs`: status producer fixtures and existing UI-gateway proof assertions.
- `.github/workflows/ci.yml`: blocking unit/integration project lists; topology is advisory. Prefer reuse or an explicitly filtered blocking lane to unnecessary test duplication.
- `src/Hexalith.Tenants/Program.cs`: authenticated bespoke /project; use a valid empty tenant-domain projection with an isolated aggregate identity only if it performs no persistence. Inspect dispatcher before selecting payload.

## Tasks & Acceptance

**Execution:**

- [x] `src/Hexalith.Tenants.AppHost/Program.cs` and `tests/Hexalith.Tenants.AppHost.Tests/AppHostSecurityResourceGraphTests.cs` — wire EventStore token after sidecar creation; prove sharing and isolation in both auth modes.
- [x] `tests/Hexalith.Tenants.IntegrationTests/` and `.github/workflows/ci.yml` — make real generated HTTP ETag cases and real command-status-producer/UI-gateway proof run in a blocking topology-free lane with minimum-test/skip guards. Add missing boundary tests without copying platform boilerplate.
- [x] `tests/Hexalith.Tenants.IntegrationTests/` or `scripts/` — add a maintained secret-safe runtime validator/fixture using actual local Keycloak credentials, effective project authorization and internal channel positive/negative controls; run it against rebuilt healthy services. Prefer consuming Aspire state privately over logging environments. Do not persist business state.
- [x] `_bmad-output/implementation-artifacts/deferred-work.md`, `docs/maintenance-backlog.md`, `tests/test-summary.md` and colocated validation evidence — close exactly the five verified records and reconcile coverage; preserve unrelated entries.

**Acceptance Criteria:**

- Given a healthy Keycloak topology, when real workload credentials are exchanged and the project route is called, then the positive case succeeds and missing-credential controls fail without storing business data or credentials.
- Given either auth mode, when app-channel wiring is evaluated, then EventStore has its own secret shared with its sidecar, and the protected notification callback rejects missing/wrong tokens.
- Given generated ETag or status-proof regressions, when the configured blocking contract lane runs, then assertions fail independently of advisory Aspire results.
- Given passing relevant checks and review, when publishing, then five ledger records close with reproducible evidence and the changes reach main.

## Implementation Notes

The user's end-to-end instruction authorizes implementation and publication. No unresolved product choice, migration or deployment. Existing AppHost was already running; Aspire describe reports healthy resources including security, eventstore, tenants and tenants-api. Private baseline state: `/tmp/tenants-deferred-five-aspire-state.json`; telemetry capture attempted at `/tmp/tenants-deferred-five-baseline-otel.json`. No submodule mutations authorized or needed. Implement sequentially; leave commit/push and live AppHost restart to the parent after source verification.

## Spec Change Log

Planning correction: replaced the matrix's unsupported discovery wording with protected notification request validation. EventStore discovery is anonymous; the existing protected callback rejects an empty notification before side effects. This implementation detail preserves the delegated channel-authentication goal and the no-business-write constraint.

## Review Triage Log

Blind Hunter floor: 87,551 / 1,000 = 87.551 kB; min(floor(sqrt(87.551) + 1), 10) = 10. All three layers reported before triage. Verification Gap Reviewer reports no gaps. Findings are retained individually, including duplicate claims.

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| B1 | medium | patch | A null committed sequence withholds one proof field; existing create/member flows have other projection evidence policies. Narrow the new test and all closure claims to the exact committed-sequence proof asserted, preserving the positive producer/consumer contract. |
| B2 | medium | patch | Successful control-token issuance alone does not prove which operation/audience the mapper emitted. Inspect each token before the negative route call to substantiate the specific missing/query/sample control. |
| B3 | medium | patch | The promoted ProjectContract filter currently covers scope/channel but would miss signature/lifetime/issuer/caller regressions. Add four narrow rejection cases using the existing fixture and update its minimum count. |
| B4 | false | reject | The production mapped handler directly invokes ProjectionDispatcher; for this valid tenant-domain empty request its only path returns 200. HTTP 401/403 is produced by authorization before that handler. Status assertions plus the actual route/handler trace establish admission rejection; unchanged persisted contents independently prove no business writes. No test-only dispatcher seam is needed. |
| B5 | medium | patch | Manual app-header controls prove the app policy, while graph evaluation proves configuration sharing. Add actual EventStore sidecar delivery: parent independently POSTed the same empty notification through its local Dapr invoke API without supplying the app token and observed 400 validation. |
| B6 | false | reject | Runtime evidence explicitly identifies the healthy Keycloak topology and actual client_credentials grant. Both-mode claims concern evaluated AppHost token sharing/isolation, covered by the two model tests. No live signing-key-mode run is claimed; make that qualification explicit in evidence. |
| B7 | low | reject | The operator supplies a fresh owner-only snapshot of a running local Development AppHost. Reused-PID confusion requires stale input contrary to that documented precondition; adding process lifecycle authority to this validator is disproportionate for that unlikely use. Final accepted evidence refreshes resource state and binds loaded assembly identities separately. |
| B8 | medium | patch | AppHost/test hashes do not identify the live domain/authentication assemblies. Refresh/restart after builds and record current mapped EventStore/Tenants assembly hashes, source roots, exact dependency gitlinks and dirty candidate provenance without credentials. |
| B9 | low | patch | Separate stat/read opens can inspect and consume different files. A single no-follow descriptor with fstat is a direct correction of the existing owner-only check; retain safe failure output and test unreadable permissions/symlinks. |
| B10 | low | patch | Acceptance retained the obsolete discovery label even though the matrix and implementation notes accurately describe protected notification validation. Correct that label; no production endpoint or security-policy change is implied. |
| E1 | low | patch | Duplicate of B10 from the independent claims pass. Discovery remains anonymous in EventStore; the protected callback is the route actually qualified. Correct the recorded acceptance wording consistently. |


## Verification

- Build targeted test projects in Debug/source mode; run every added/selected contract and model case with zero skips and the existing minimum-test safeguards.
- Rebuild/restart the exact AppHost via Aspire, inspect health and run the maintained runtime validator; report safe booleans/statuses only.
- Run relevant existing suites, workflow/script syntax and repository guards. Use the baseline fallback ladder if a broad gate is blocked and preserve the exact command/result.
- Stage/read the complete diff including new files, audit every matrix row, independently review and correct findings before closure.
- Validate the exact full commit message using local pinned commitlint, fetch/inspect divergence, commit and push main. User has explicitly authorized these Git operations.

Runtime route investigation: EventStore subscription discovery is currently anonymous, so it cannot prove the app-channel boundary. The existing protected POST /projections/changed rejects an empty notification before actor calls or broadcasting: use its HTTP 400 request-validation result as the positive channel-admission control, paired with missing/wrong-token HTTP 401/403. This refines the matrix's read-only-discovery wording to a non-mutating protected route; it introduces no production endpoint or notification side effect. TenantProjectionHandler returns immediately without writes for an empty Events array, so an isolated valid tenant /project request can prove workload authorization with HTTP 200.

Baseline runtime control: on the healthy unmodified AppHost, EventStore has no APP_API_TOKEN and POST /projections/changed with empty tenant/type and no channel token returns HTTP 400 (validation reached). Safe raw observation: `/tmp/tenants-deferred-five-baseline-channel.json`. After wiring, this exact no-token request must instead fail channel authorization.

Initial pre-review source checks: Debug/source full CommandApiRuntimeIntegrationTests 113/113; generated class 44/44; security model 8/8. Release/package generated44 + status18 + project7 and full AppHost9/9 pass, zero skips. Both source and package builds have zero warnings/errors. Full command/result evidence and initial corrected attempts are retained in the colocated source-checks JSON. Parent live validator passes all actual Keycloak, operation, audience, bearer and channel controls with verified TLS. Exactly five records close and the index covers the remaining 82 open IDs exactly once.

Review correction verification: parent normal Debug/source build and complete affected class 117/117 pass. The exact Release/package build and filters 44+18+11 pass, all zero skips and build warnings/errors. Changes narrow sequence-proof wording, verify negative token claims, add four cryptographic/issuer/caller controls, prove actual own-sidecar delivery, and read private state from one no-follow descriptor. No production authorization or lifecycle evidence policy changes.

Final runtime correction recheck: a full fresh AppHost graph passes all real Keycloak and own-sidecar controls after one rejected project-only restart attempt (ERR_DIRECT_INVOKE/500). The rejection is retained; the accepted packet binds 20 current mapped assemblies, canonical clean dependency checkouts, and the dirty candidate source. No TLS/authentication policy was changed. Independent review rechecks follow before publication.

Final review recheck: Edge Case Hunter reports no findings; Verification Gap Reviewer reports no gaps; Blind Hunter confirms all accepted fixes resolved and all remaining dispositions adequate, independently verifying 28 source/binary fingerprints, accepted token/sidecar results and nine unchanged clean dependencies. No new deferred work was added. Exact-message pinned commitlint 21.2.3 passes with zero problems/warnings; the authorized main commit/push follows.
