---
title: 'Complete DW-161 through shared conditional query authority'
type: 'bugfix'
created: '2026-10-25'
status: 'in-review'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '692c17b27d94677a3a4d065330d150b262cdb570'
eventstore_baseline_commit: 'ee56b15ac0b0a787e9c5fb9dc46cad96a59b46ab'
context:
  - '/home/administrator/projects/hexalith/tenants/references/Hexalith.EventStore/AGENTS.md'
  - '/home/administrator/projects/hexalith/tenants/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '/home/administrator/projects/hexalith/tenants/references/Hexalith.AI.Tools/hexalith-git-instructions.md'
---

<frozen-after-approval reason="user explicitly approved the shared EventStore fix and dependency update">

## Intent

Complete DW-161: repair shared EventStore conditional matching/304 metadata, publish through existing gates, consume the released repair in Tenants, and push reviewed main changes. The other four records are closed.

## Boundaries & Constraints

**Always:** Execute authenticated routing before comparison; keep version distinct from ETag. Preserve policy/payload, mixed-projection and provenance/lifecycle guards, header bounds and absent-evidence behavior. Final acceptance uses command-seeded persisted state. Only EventStore dependency/gitlink advances; preserve other656 records and DW-161 history.

**Never:** Bypass TLS/auth/release gates; initialize nested submodules; infer version from ETag; expand wire fields; encode private validators in Tenants; mutate live persisted state; weaken/skip acceptance; close other decisions.

## I/O & Edge-Case Matrix

| Scenario | Input | Expected | Failure |
| --- | --- | --- | --- |
| Producer match | Exact strong opaque ETag | Routed304, independent version/time retained | No preflight shortcut |
| Miss/weak/wildcard/limits | Unsupported validator | Existing200 behavior | Bounded parsing |
| Policy/payload/mixed | Existing veto | No304 | Preserve guards |
| Fallback | Legacy service-only opaque ETag | Existing200 | Producer required |
| Nonprojection | Handler/unknown provenance | No304/projection authority | Fail closed |
| Metadata | Valid/missing/malformed/duplicate/oversized/conflicting headers | Preserve valid fields; invalid evidence cannot create authority | Canonical lifecycle wins |
| Consumer | Released packages, live state | REST4/4, historical1/1, UI3/3, zero skips | Keep unfulfilled record open |

</frozen-after-approval>

## Code Map

Code paths are relative to owner `references/Hexalith.EventStore`.

- `src/Hexalith.EventStore/Controllers/QueriesController.cs`: dispatch already precedes comparison. Permit strong opaque equality only for the actual producer ETag. Preserve fallback misses, policy/payload vetoes, wildcard rejection, ten-value limit, weak rejection and encoded mixed-projection safeguards. Emit existing version/time/freshness headers before304.
- `src/Hexalith.EventStore.Client/Gateway/EventStoreGatewayClient.cs`:304 drops version/time. Add bounded single-value header parsing; typed results already forward Metadata.
- `src/Hexalith.EventStore.RestApi.Generators/RestApiControllerEmitter.cs`: existing AppendQueryMetadataHeaders/TryGetBoundedHeaderValue define names,4096-character/control rules and invariant round-trip time. No new wire fields are needed.
- `tests/Hexalith.EventStore.Server.Tests/Controllers/QueriesControllerTests.cs` and `Integration/QueryResponseProvenancePersistenceTests.cs`: controller/router/HTTP seams. Strengthen the mixed-type test so the server validator actually matches a candidate.
- `tests/Hexalith.EventStore.Client.Tests/Gateway/EventStoreGatewayClientTests.cs`: typed/untyped304 and malformed/duplicate metadata controls. Generator error-semantics tests already exercise forwarding; the unsafe-version case needs ProjectionBacked provenance to reach its bound check.
- Gateway links controller source into the published Gateway package. Release publishes14 packages together and requires exact-main CI/commitlint plus production protection. Root handles release and actual version discovery.
- Consumer Tenants `Directory.Build.props`: actual published HexalithEventStoreVersion goes before Builds imports, consistently overriding3.127.0 without changing Builds. Root updates the EventStore gitlink and ledger.

## Tasks & Acceptance

- [x] Shared controller/client — repair matching and304 transport.
- [x] Focused controller/client/persistence/generator tests — execute every matrix row.
- [x] EventStore `docs/reference/query-api.md` — document producer validators/bodyless metadata.
- [x] Tenants `Directory.Build.props`/EventStore gitlink — consume published repair and pass unchanged live conditional authority.
- [ ] Ledger/index/parent spec/evidence — close only DW-161 after proof.

Given reviewed shared source, tests must pass without weakening guards. Given exact-main CI and release approval, consume its actual published package. Given the live topology, unchanged304 assertions must pass. Given conservation, all657 IDs remain and only DW-161 changes status.

## Implementation Notes

The subagent owns shared source/tests/query docs, sequentially, from EventStore. Root owns remotes/release, Tenants and ledger. Subagent: no commits/push/dispatch/publication or consumer edits; no skills under references. Prior fresh hosted topology supplies baseline; no AppHost change.

## Spec Change Log

User approval resolves the original dependency-freeze blocker. EventStore was clean-fast-forwarded from4e905980a58c9641d800be062667cac24a50d0b3 to the recorded current main baseline; no nested submodule was initialized/updated. Current release3.128.0 is historical: consume the actual eventual repair release.

## Review Triage Log

## Verification

Build each affected EventStore test project in Debug/source (`-p:UseHexalithProjectReferences=true -m:1 -nr:false`); record source blockers before package fallback. Run maintained MTP class selections with discovered minimum counts and `--fail-skips on`. Root runs all three independent review layers, validates exact messages using each owner's pinned commitlint, follows release gates, and reruns Tenants HTTP/live suites and hosted Release/package CI. Conserve all656 unrelated ledger bodies, historical DW-161 fields and exact index coverage. Only the approved EventStore pointer/package pin may advance.

Shared-source phase: all three Debug/source builds pass without warnings/errors; maintained MTP controller92, persisted HTTP2, gateway client87 and generator50 pass (231 total), zero skips. Both changed docs pass lint. Review this source before the required package publication; consumer/release tasks remain explicitly pending and will receive their own final verification/review before closure.


### Shared-source review triage

| Finding | Verdict / evidence | Route |
| --- | --- | --- |
| Shared Blind1: Unicode version reaches Kestrel header writer | medium: bounded/control-only guard accepts non-ASCII; Kestrel's default header validation requires ASCII and throws. The duplicate edge finding confirms the same reachable200/304 failure. | patch safe header emission and HTTP regression |
| Shared Blind2: comma ETag result is not reusable | medium: GetETag returns unquoted opaque,validator, but NormalizeIfNoneMatch drops that returned value. The caller cannot reuse the SDK's own validator. | patch atomic normalized-token round trip |
| Shared Blind3: invalid trailing list members ignored after a match | medium: matching returns before later garbage, unmatched quotes or wildcard members are validated. These are reachable unsupported headers. | patch complete bounded validation before matching |
| Shared Blind4: coalesced version text called a duplicate | false: ProjectionVersion is an opaque equality token; the existing emitter permits a single comma-containing value. A single version-42, version-43 field is indistinguishable from a legitimate opaque producer version and must not be split or rejected without changing that contract. Actual multiple header values are already rejected. Clarify documentation and retain opaque-value coverage. | reject comma-based version inference |
| Shared Blind5: newly conditional producer warning evidence lost | medium:200 metadata retains WarningCodes, while the new opaque304 path drops them. The existing generated surface already defines a bounded Warning-Codes carrier. | patch existing warning carrier or preserve full-body evidence |
| Shared Blind6: count limit follows projection decoding | low: production analyzes/decodes the complete header before applying its ten-value match limit. HTTP transport bounds total bytes, but parsing the supported list before decoding directly removes this unnecessary work. | patch as part of complete-list validation |
| Shared Blind7: fallback mixed-type regression removed | medium: replacing the weak old fallback test with a producer case leaves hasProducerETag=false without a meaningful mixed-type veto control. Both states must match a candidate before asserting200. | patch both producer and fallback cases |
| Shared Blind8: persisted conditional test does not advance state | medium: unchanged-state304 controls cannot detect a stale validator after a producer update. Use the isolated fixture's legitimate writer, then require old-tag200 and new-tag304; final live acceptance still uses real commands. | patch persistence/HTTP advancement proof |
| Shared Edge1: Unicode version reaches default header encoding | medium: same verified non-ASCII header failure as Shared Blind1; official ASP.NET Core10 HttpHeaders.ValidateHeaderValueCharacters requires ASCII for the default encoding selector. | patch same root |

All three review layers completed before triage. Verification-gap review reported no gaps. No finding is dropped; the one rejected claim preserves the established opaque-version contract. Existing shared release and consumer tasks remain pending.

Post-review corrections pass controller103/103, real Kestrel persistence4/4, gateway client91/91 and full generator124/124, all zero skips (322 checks). Server/client/generator Debug/source builds have zero warnings/errors. Query documentation lint and diff whitespace pass. The rejected coalesced-version interpretation remains rejected, with a single comma-containing opaque version covered explicitly. No review finding is unresolved in the shared source. Publication and consumer acceptance remain pending.


## File List

- `references/Hexalith.EventStore`
- `Directory.Build.props`
- `tests/Hexalith.Tenants.IntegrationTests/AuditStreamPersistenceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.EarlyMembershipNudge.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.EarlyMembershipNudgeBoundaries.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/MyTenantsSurfaceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantDomIdentityRenderingTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/StatusFilterLabelRenderingTests.cs`
- `_bmad-output/implementation-artifacts/dw-161-shared-conditional-authority-validation-evidence.md`
- `_bmad-output/implementation-artifacts/dw-161-consumer-binding-local.json`
- `_bmad-output/implementation-artifacts/dw-161-published-package-comparison.json`
- `_bmad-output/implementation-artifacts/dw-161-live-browser-local.json`

## Completion Notes List

Historical initial repair checkpoint: the approved shared fix is `656f2771761f48060657bb7485cd5f369f6b7b81`, based on current main `a5f1aaa08cf466f26a06cf8719138043f22cfde7` after a clean rebase over five upstream documentation/dependency commits. All six reviewed source/test file byte hashes remained identical. The source push was accepted through the authenticated main-push override; publication still requires exact-source CI/commitlint and the existing protected production review. Consumer package integration and live closure remain pending.

| Path | Baseline | Current target |
| --- | --- | --- |
| `references/Hexalith.EventStore` | `4e905980a58c9641d800be062667cac24a50d0b3` | `935c21ac5ebc5e50738ae6ee79dc661acd2fd5a8` |

Consumer compatibility: published3.128.1 marks ActorResponseMismatch obsolete in favor of identical wire-value ResponseMismatch; four audit-test references migrated without suppression (117/117 pass). Six existing scoped NavigationFailureNotifier fixture registrations were incorrectly gated on source-only HFC_ROUTE_OPTIONS although published FrontComposer4.7.0 exposes the service; only those guards were removed (832 focused cases pass). Actual route-options guards remain. Package-profile full UI discovers7705 because one genuine route-options case is source-only; full package UI7705/7705 now passes, zero skips. Initial full package UI had285 DI failures, followed by one CancellationToken.WaitHandle disposal-helper NullReference; isolated6/6 and the complete rerun passed. An initial isolated minimum24 was incorrect for six declared cases; corrected minimum6 passed. No production test expectation or failure/skip gate was weakened.

Local published-package checks now pass REST4/4 (including unchanged304 authority), historical1/1, authenticated UI3/3 with browser25/25, generated HTTP44/44, audit117/117, AppHost13/13 and fixture guards80/80, all zero skips. One UI invocation failed prerequisite discovery before scenarios; direct Redis PING and real placement/scheduler socket checks succeeded, and the documented existing endpoint overrides yielded the passing UI run. No Dapr service was restarted or policy/identity changed. Hosted Release/package qualification and closure remain pending.


### Consumer review triage

| Finding | Verdict / evidence | Route |
| --- | --- | --- |
| Consumer Blind1: Unicode producer version lost on304 | medium: the new ASCII header guard avoids Kestrel failure but still allows a bodyless response that loses a valid JSON equality token. Preserve its full-body evidence when the existing carrier cannot retain it. | patch shared fallback before final publication |
| Consumer Blind2: surrounding spaces change version during HTTP parsing | medium: HTTP field parsing trims boundary whitespace, so emitting that opaque value changes its equality token. The same round-trip eligibility guard must retain200 JSON. | patch same round-trip guard |
| Consumer Blind3: new local proof is not retained/reproducible | medium: current prose lists successful runs but has not yet retained the new safe packet, canonical commands or resolved package/binary binding. | patch evidence packet and commands |
| Consumer Blind4: hosted qualification pending | false as a premature-closure claim: DW-161 is still open, the ledger task is unchecked and the documents explicitly require the candidate's hosted Release/package run before closure. That run necessarily follows the reviewed consumer push. | retain required final verification |
| Consumer Blind5: source-reference CI blocker understated | medium: the known Memories source failure affects a mandatory source-reference workflow build, not merely an optional runtime resource. Record its exact baseline/current disposition without weakening or repairing unrelated gates. | patch accurate evidence and tracked hold |
| Consumer Blind6: cancellation-disposal observer exception unexplained | medium: one full run failed in the existing WaitHandle polling helper; rerun success alone does not establish whether this is a fixture race. Diagnose the exact runtime/fixture boundary and preserve the initial failure. | patch/track from demonstrated cause |
| Consumer Blind7: current publication statements stale | low: new evidence still leads with publication pending despite its appended successful3.128.1 release. Historical checkpoints must be marked clearly and current evidence updated. Spec-only editing portions are rejected under the review workflow rule; normal final state tracking remains required. | patch evidence current-state prose |

All three consumer layers completed before triage; edge-case and verification-gap reviewers reported no findings. The unchanged domain conditional test already passes4/4 locally, but these shared round-trip defects require a follow-up repair release before final consumer qualification. The approved shared-repair scope already covers this correction; no new wire fields or broader dependency updates are authorized.


### Consumer review patch verification

Consumer Blind1 and2 are fixed in EventStore935c21ac5ebc5e50738ae6ee79dc661acd2fd5a8: a single lossless header check controls both emission and304 eligibility. Unicode, control, oversized and boundary-whitespace versions retain200 with unchanged payload/JSON evidence. Null/empty, bounded ASCII, commas and internal spaces preserve304. Debug/source build has zero warnings/errors; controller111/111 and real Kestrel7/7 pass with zero skips, and query documentation lint passes. The follow-up main push used the authenticated main-push override; release still requires the ordinary exact-main CI/commitlint and production-review gates.

Consumer Blind6 is a demonstrated test-observer race: .NET10.0.13 CancellationTokenSource.WaitHandle can read the kernel-event field concurrently with Dispose clearing it. Both test helpers now receive a SafeWaitHandle captured while each controlled fake is held, assert it initially open and poll IsClosed within the original deadline. All eight callers preserve cancellation, authority, disposal, ownership, rendering and status assertions; borrowed handles are never disposed. Debug/package build has zero warnings/errors and the affected class passes475/475, zero skips. Initial failed runs remain failure evidence.

The root complete UI recheck after the observer patch passes7705/7705 with zero skips. The shared client91/91 and full generator124/124 also pass after the retention patch, bringing shared focused checks to333 (111+7+91+124), all zero skips. An initial generator invocation used a nonexistent Server.RestApi.Generators.Tests project path and failed before tests; the corrected maintained RestApi.Generators.Tests project passes.


Current publication: EventStore3.128.2, tag/source935c21ac5ebc5e50738ae6ee79dc661acd2fd5a8, released2026-10-25T23:03:01Z through successful source CI40240634704, commitlint40240634709 and protected release40242204827. Tenants consumes that actual version. Final hosted consumer qualification remains pending; the initial3.128.1 statements above are historical checkpoints.


Final local consumer verification on actual3.128.2 passes seven Debug/package builds with zero warnings/errors; REST4/4, UI3/3 with browser25/25, historical1/1, anonymous6/6, HTTP44/44, audit117/117, fixture guards80/80, full AppHost13/13 and full UI7705/7705, all zero skips. Safe packet, resolved-package/output receipt, all14 registry-versus-release package comparisons and canonical reproduction commands are retained in the continuation evidence. Mandatory source-reference Memories compilation remains a separately tracked baseline/current failure. All consumer review patches are verified; final hosted normal-NuGet Release/package qualification remains required before DW-161 closes.
