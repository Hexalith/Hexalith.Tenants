---
title: 'Scope live list hydration tokens to the actual isolated test corpus'
type: 'bugfix'
created: '2026-11-03'
status: 'in-review'
route: 'dispatch'
baseline_commit: 'f5f65fc0bb755fb4af31427e4f62727d44f578e9'
context:
  - '/home/administrator/projects/hexalith/tenants/AGENTS.md'
  - '/home/administrator/projects/hexalith/tenants/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '/home/administrator/projects/hexalith/tenants/references/Hexalith.AI.Tools/hexalith-git-instructions.md'
---

<frozen-after-approval reason="User authorized all necessary CI repairs and unattended release">

## Intent

Restore the live cursor recovery qualification when the isolated backend contains several command-seeded tenants. A native global administrator can list every tenant, while detail hydration requires exact eventstore:tenant grants. The cursor test currently grants only its newest tenant and then hydrates page one, which contains a previously seeded tenant. The Ready assertion therefore fails independently of transport timing. Authenticated UI tests also share earlier rows; give those sessions the same exact corpus visibility without changing production authorization.

## Boundaries & Constraints

Test-only change. Preserve all production authorization, current-tenant selection, native ownership, admission, proof, projection, denial, cursor, performance and ready-content assertions. No wildcard/system detail grant, extra retries, warm-up of the measured populated-page control, moving baseline pagination to the newest row, reduced case floors, skips or fabricated evidence. Preserve concurrent changes, inspect targets immediately before writing, and do not change dependency versions. No staging, commits, pushes, remote operations, AppHost or private business runtime launches. Root owns publication and actual fresh CI runtime proof. Local individual builds and focused synthetic unit tests are permitted. Never print tokens or private keys.

</frozen-after-approval>

## Code Map

- Owning repository is root Tenants. Read tracked .editorconfig, .gitattributes, Directory.Build.props/targets, relevant project files and tests before editing. Current working tree also contains authorized deadline/CI repairs; preserve them. Root EventStore 4.0.4 package pin may temporarily block full restore until publication. Distinguish source/local compilation evidence from eventual public-package/live acceptance, and report the exact blocker without weakening package admission.
- tests/Hexalith.Tenants.IntegrationTests/Fixtures/AuthenticatedRouteState.cs holds the narrow per-seed Token. Keep it narrow and retain existing direct route assertions. Add a separate instance helper for producing an exact corpus read token. Use the existing production TenantsRestQueryClient and isolated HttpClient, with the existing global administrator CreateDemoJwt() platform list caller. Global-admin authority already allows index enumeration. Enumerate ListTenantsQuery PageSize100 through every returned protected cursor; do not assume page one is complete. Require successful response/payload, safe nonblank exact tenant IDs, no repeated cursor, nonempty corpus including this seeded TenantId and its persisted current name/status. Do not log IDs/tokens in refusal diagnostics. Deduplicate exact IDs and mint AspireTopologyTests.CreateDemoJwt(exactIds). Caller cancellation must terminate enumeration. Reject incomplete or malformed pagination rather than returning partial grants. Prefer an overload accepting a typed list-page delegate for deterministic controls if needed; keep each new C# type in its own matching file.
- Existing WaitForListPageAsync polls the actual index until this seed is visible and returns its cursor. Keep that API and pagination behavior. Run corpus token enumeration only after existing seed projection/list-readiness checks and on this fixture's already-qualified fresh backend; the helper must not bootstrap or modify authority.
- AuthenticatedTenantsRouteTests.InvalidProtectedCursorHasRealSentinelAndRecoversPageOneThroughProductionGateway currently creates a provider with state.Token before WaitForListPageAsync. Move provider creation after readiness and use the separate exact corpus token. Keep real page-one baseline and next-page comparisons, invalid and incompatible cursor 400 controls, membership recovery, unrelated InvalidRequest 400 and all authority assertions unchanged. Other route tests retain narrow tokens. PopulatedTwentyRowPageHydratesWithoutActorQuotaFailure already grants its actual20 IDs and measures one load; do not alter it.
- AuthenticatedTenantsUiRouteTests.ProtectedRoutesRenderForValidatedCallerAndCurrentTenant and LiveBrowserMeasuresResponsiveForcedColorsAndRestoresContextualAuditBackFocus should obtain their corpus tokens after command seed/list readiness and sign in using them. Preserve the initial empty-corpus sign-in in the first test, genuine OIDC callback/cookie checks, unambiguous current tenant, seeded ready-content and browser focus/performance assertions. The UI's current tenant comes from validated OIDC identity, independently of access-token read grants. Keep ambiguous-current-tenant denial untouched.
- Add focused tests beside AuthenticatedRouteFixtureIsolationTests (a new matching type/file if clearer). Synthetic returned pages can validate grant issuance without a running backend. Parse the token in-process; assertions must not leak it on failure. Required controls: multiple pages/duplicates yield only exact distinct observed grants including older rows; incomplete response, missing/repeating cursor, unsafe/empty identifier, missing or stale seed refuse; cancellation propagates. Retain no-wildcard checks and the original narrow Token. Reuse existing response/page model construction rather than a second pagination implementation.

## Tasks & Acceptance

- [x] Given a fresh qualified backend and complete paginated index with this current seed and older tenants, when corpus credentials are issued, then only those exact distinct IDs receive read claims and the original state.Token remains narrow.
- [x] Given incomplete, malformed, cyclic, missing-seed or stale-seed pages, when issuance runs, then it fails closed without a token; caller cancellation propagates.
- [x] Given the existing cursor test corpus, when production gateway page one and recovery are requested, then both may hydrate every listed row while all existing recovery/denial/metadata assertions remain present. Given either authenticated UI seed, its genuine session receives the exact corpus token while current-tenant selection remains unchanged.
- [x] Build the changed individual test graph with warnings as errors and run the focused fixture/token controls without starting a business runtime. Record exact commands, results and limitations in /tmp/tenants-release-20261103-07JciR/corpus-token-handoff.json. Root must still prove the complete live suite on fresh public-package CI before release.

## File List

- references/Hexalith.EventStore
- references/Hexalith.Builds
- references/Hexalith.Platform

## Verification

Both individual Debug/source builds passed with zero warnings/errors; direct assembly focused execution passed65 cases (40 corpus controls and25 existing fixture controls), zero skips. Exact commands, input hashes, logs and TRX are retained in /tmp/tenants-release-20261103-07JciR/corpus-token-handoff.json. Root read the implementation diff and confirmed original seed creation, pagination, measured20-row control and denial assertions remain. This establishes local composition only; fresh public-package CI and actual live cursor/UI acceptance remain required before release. The spec does not claim every UI failure was caused by the corpus mismatch.


## Review Triage Log

| Finding | Verdict | Evidence and route |
| --- | --- | --- |
| Blind1: newer SDK helper breaks retained3.129 build | false | The pre-existing ValidateCanonicalBoundaryPackageCohort target runs before CollectPackageReferences and PrepareForBuild for every current Contracts/Server dependency and already rejects every package selection except4.0.3. Retained catalog flags are historical API qualifications, not a supported complete current-package build. This change creates no reachable previously valid3.129 build regression. |
| Blind2: corpus credentials carry mutation permissions | medium; patch | CreateDemoJwt includes command:submit and tenant/admin writes. A read-only corpus credential should retain only query:read and the two existing read permissions; preserve all ordinary command tokens and exact tenant claims. |
| Blind3: corpus issuance accepts stale/degraded metadata | medium; patch | The real typed list client exposes authoritative provenance/lifecycle and degradation separately from HTTP success. Require the same current projection evidence already asserted by the live route tests, with focused refusal controls. |
| Blind4: unbounded corpus or token size | low; rejected | Every actual caller has a five/eight-minute cancellation bound and a fixture-owned fresh backend with a fixed small corpus (20 populated seeds plus individual route seeds; UI only its own seeds). Unique cursor growth and oversized legitimate corpora are not reachable in this qualification. Additional arbitrary limits add unrelated branches without a demonstrated current state. |
| Blind5: stalled or late readiness predicate exceeds deadline | medium; patch | The helper awaits the predicate before rechecking elapsed time; late true can pass after the advertised deadline. Bound that await by remaining monotonic time and preserve the original failure label. |
| Blind6: backwards wall-clock changes extend polling | low; patch | The helper uses Date.now for an elapsed-time deadline. Use monotonic time as part of the same finite helper correction and retain deterministic tests. |
| Blind7: navigation/networkidle get independent allowances | low; patch | Both await separate default timeouts. Give open one overall deadline rather than two allowances, preserving navigation/readiness behavior. |
| Blind8: browser90 cuts valid50+60 composition | medium; patch | Initial list awaits before enrichment starts, and prerender awaits both. Set functional bound150 seconds to enclose their110-second maximum with headroom; keep independent performance thresholds and test actual composed delayed rendering. |
| Blind9: long Dapr control exceptions discard report | medium; patch | New invoke/JSON operations can throw before report serialization. Record fixed failure categories for each long control and preserve earlier results on those exception paths. |
| Blind10: delayed success omits stale/served metadata assertions | low; patch | The controlled backend emits these exact headers and documentation claims metadata preservation. Add their exact assertions to the new successful long control; no new production API or test mode is required. |
| Edge1: pending readiness predicate never returns | medium; patch | Same demonstrated await as Blind5; include pending and late-success controls in the helper correction. |
| Verification gap: successful enrichment after30 unprotected | medium; patch | Existing injected50ms and virtual60s exhaustion controls pass if the effective default timer regresses30. Add actual default-deadline virtual31s successful detail completion with authoritative known counts and no degradation. |
| Verification Other1: legal36+55 render fails90 navigation | medium; patch | Independently traced actual workspace prerender; group with Blind8 and exercise that real cumulative delay on the owned synthetic browser loopback. |

These are trivial changes to internal/test-only seams or their assertions; no public surface, production authority or unknown state is introduced. All original acceptance checks and actual live CI remain required. The existing Dapr/Sentry/HTTPS synthetic loopback is permitted targeted policy qualification, with unique task-owned resources and cleanup; no business AppHost or private business backend is authorized for local launch.


## Review Fix Verification

- All retained patch findings are fixed. Corpus credentials now include only query:read and the two catalog read permissions; all ordinary command/narrow tokens are preserved. Every observed page requires the existing live authority metadata contract. Focused corpus53 and UI deadline19 cases pass with zero skips; edited graphs compile without warnings/errors.
- Browser helper uses a finite150-second monotonic bound shared across each operation's awaits, rejects stalled/late predicates and preserves failure labels. All4 browser controls pass, including actual36+55-second held prerender and existing31-second responses/denial. Dapr normal15 and8 injected invoke/JSON/header controls pass; retry mutation retains13 cases and fails only the intended503 guard with4 backend calls. Exact commands/hashes/results: /tmp/tenants-release-20261103-07JciR/corpus-token-review-fixes-handoff.json.
- EventStore4.0.4 Release41580970775 succeeded at326bfef884543cdbcaf96de7f4c4ad378dbad6d7; all14 actual public archives verified at2026-11-03T05:12:16.427447+00:00. Identity/version, ZIP integrity, repository-signature presence, hashes and nuspec source all match. Signature presence is not independent cryptographic signature validation. Proof: /tmp/tenants-release-20261103-07JciR/eventstore-4.0.4-root-agent/verification.json.
- Root fresh public-package restore succeeded. All five changed individual Release graphs built with -warnaserror and zero warnings/errors. Public-package governance184, offline AppHost50 and UI deadlines19 cases pass, zero skips. Commands/logs: tenants404-builds.json and tenants404-focused-public-tests.json in that same evidence directory. Ten restored SDK archives were independently identical to the downloaded public archives.
- Direct complete generic discovery now contains669 cases (609 prior cases plus changed fixture/token controls). Its blocking minimum is updated669. Complete generic execution and final exact-source live CI remain pending; neither discovery nor offline model evidence establishes business runtime acceptance.
