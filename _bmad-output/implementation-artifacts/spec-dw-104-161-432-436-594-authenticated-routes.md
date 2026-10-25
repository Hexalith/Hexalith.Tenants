---
title: 'Qualify authenticated Tenants routes and recovery through the live topology'
type: 'bugfix'
created: '2026-10-25'
status: 'in-review'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '8804c10b8a900860ea2394c81eff78d23bb3c0a4'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Complete DW-104, DW-161, DW-432, DW-436 and DW-594: qualify authenticated UI context, six REST reads, conditional responses and cursor recovery through the live topology. Repair prerequisite duplicate token registration and certificate trust. Preserve anonymous denial coverage. The requested five-item batch is the unit of work; commit and push reviewed changes to main.

## Boundaries & Constraints

**Always:** Validate caller/current-tenant scope. Use production controllers/client, command-seeded isolated state and persisted projection assertions. Preserve lifecycle distinctions, safe evidence, historical fields and unrelated records. Require zero acceptance skips; keep unfulfilled items open.

**Never:** Disable TLS validation, substitute canned responses for live proof, mutate stored events/projections, expose credentials, expand API contracts, change Dapr identities/policy, update dependencies or close unrelated decisions.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Startup | Helper already wires token | One parameter shared with own sidecar | Duplicate graph fails |
| Six reads | Authorized persisted tenant | Correct data and authority/freshness headers | Transport/auth errors fail |
| Conditional | Prior strong validator | Recognized 304 with coherent metadata | Missing authority fails closed |
| Cursor | Invalid protected cursor | Real sentinel and page-one recovery | Unrelated 400 stays distinct |
| UI routes | Valid caller/current tenant | Protected pages render | Anonymous/ambiguous scope denies |
| Audit Back | Detail/member launch | Target, safe return and focus preserved | External return rejected |
| Responsive | Phone/tablet/desktop; forced colors | Content/recovery usable | Assert actual dimensions/mode |

</frozen-after-approval>

## Code Map

- AppHost tasks below consume the pinned helper's existing run-mode token ownership.
- Integration tests reuse platform lifecycle handling, existing persisted-state/client helpers and six anonymous `TenantsUiRouteSmokeTests` cases. The existing audit adapter cannot qualify REST transport.
- UI host tests and `Browser/local-oidc-fixture.mjs` supply cookie/signed-token patterns. Standalone mocked upstreams cannot qualify this batch.

- Prerequisite qualification: the pinned optional Memories server also fails package mode with CS0234 for its own `TenantIngestionOwnership`. Keep the normal AppHost topology unchanged by default; add an owning `EnableMemories=false` opt-out used only by the focused read-route fixtures/CI. Omit its UI reference when disabled; do not rename existing Dapr identities or substitute responses. Assert both default inclusion and explicit exclusion in model tests. The REST endpoints under qualification do not require Memories search.

## Tasks & Acceptance

**Execution:**

- [x] `src/Hexalith.Tenants.AppHost/Program.cs`, `tests/Hexalith.Tenants.AppHost.Tests/AppHostSecurityResourceGraphTests.cs` — remove redundant token registration; assert unique ownership.
- [x] `tests/Hexalith.Tenants.IntegrationTests/Fixtures/AspireTopologyFixture.cs`, `tests/Hexalith.Tenants.IntegrationTests/AspireTopologyTests.cs` — configure trusted certificates and safe bounded TLS diagnostics.
- [ ] `tests/Hexalith.Tenants.IntegrationTests/AuthenticatedTenantsRouteTests.cs` — add live six-route, metadata, 304 and invalid-cursor proof.
- [x] `tests/Hexalith.Tenants.IntegrationTests/AuthenticatedTenantsUiRouteTests.cs` — add authenticated routes and contextual audit navigation.
- [x] `tests/Hexalith.Tenants.UI.Tests/Browser/validate-authenticated-live-routes.mjs` — exercise Aspire endpoints, measured widths, forced colors and Back.
- [x] `.github/workflows/ci.yml` — add focused Release/package qualification with certificate trust, minimum counts and skip rejection; preserve broad advisory tier.
- [ ] `_bmad-output/implementation-artifacts/dw-104-161-432-436-594-validation-evidence.md`, `_bmad-output/implementation-artifacts/deferred-work.md`, `docs/maintenance-backlog.md` — record reviewed evidence and conserved closure.

**Acceptance Criteria:**

- Given pinned dependencies, when startup runs, then resources are healthy and the token graph is unique.
- Given authenticated sockets, when REST/UI scenarios run, then every matrix row passes.
- Given hosted CI, when `Generated_tenants_api_get_tenant_reads_verified_redis_state_with_projection_authority` runs, then persisted authority passes without skips.
- Given reviewed evidence, when conservation runs, then only these five records close; unrelated history remains.

## Implementation Notes

Investigation only. Clean main baseline above; user requests main/push. No intent gaps, migration or live publication. Fixture design is an implementation choice.

2026-10-25: User approved the spec and requested continuation. Frozen intent is unchanged; implementation is authorized.

Baseline: `aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj` builds with zero warnings/errors, then CLI exits2/AppHost134: duplicate `eventstore-app-api-token` at Program.cs:79. OpenSSL also reports untrusted certificate/`SSL_CERT_DIR`. No Tenants AppHost remains running; unrelated hosts are preserved.

Implementation/validation checkpoint: unique token graph 8/8 passes; Debug/package IntegrationTests builds with zero warnings/errors, and generated browser/CI syntax checks pass. Debug/source builds retain the pinned Memories/EventStore CS0246 blocker. The live positive-cookie workspace assertion passes; state-seeded route/browser cases remain pending because completed member commands repeatedly leave the externally shared Redis projection at sequence 1. These are failures, not closure evidence. No numbered records are closed. Full acceptance remains required after independent review and clean hosted execution.

The conditional proof targets version-backed tenant detail. All six routes assert actual scoped payload and freshness/provenance; index/audit producer-version policy stays separately tracked in DW-568. No production metadata policy was changed.

Post-review patches are implemented. Focused model10/10 and TLS5/5 pass without skips; final IntegrationTests build has zero warnings/errors. Browser source is syntax-checked; live matrix acceptance remains pending. A temporary validation branch will obtain clean hosted execution before main is updated. The root also makes token-leak assertions boolean so failure messages cannot echo the actual fixture token.

## Spec Change Log

## Review Triage Log

| Finding | Verdict / evidence | Route |
| --- | --- | --- |
| Blind1: responsive checks only cover audit | medium: DW-104 requires the actual tenant-list grid at measured widths/forced colors. | patch list cases |
| Blind2: restricted administrator container also passes | medium: the same marker exists in the restricted branch; real authorized row/readiness must be checked. | patch |
| Blind3: cookie file is written before chmod | medium: default Unix umask permits a read window before chmod. | patch atomic private creation |
| Blind4: missing and token-inconsistent cookie scopes | low: maintained host tests already cover missing subject/current-tenant and ambiguous values; mismatching a forged trusted cookie with another token is outside the supported authentication fixture contract. No new cross-token policy is established. | reject extra policy/duplicated coverage |
| Blind5: Back checks lose nested return context | medium: origin/path alone cannot detect loss of the original workspace query. | patch complete context |
| Blind6: one recovered row does not establish page one | medium: compare against a cursorless baseline and paging shape without parsing cursors. | patch |
| Blind7: detail version is only nonempty | medium: compare with the persisted version and require valid served-at metadata. | patch |
| Blind8: evidence digests are captured only once | medium: recompute all source/dependency/binary captures before success. | patch |
| Blind9: circuit errors may only reach console | medium: capture bounded circuit failures and prove interactive operation. | patch |
| Edge1: focus target is a launcher sibling | medium: production restores to the source-specific audit launcher, not its containing return-focus landmark. | patch |
| Edge2: cookie file mode window | medium: confirms Blind3 with create-time file-mode evidence. | patch same root cause |
| Verification1: authorized admin page may be restricted | medium: preverified gap confirms Blind2. | patch same root cause |
| Verification2: TLS formatter redaction lacks failure tests | medium: successful readiness cannot detect arbitrary exception-message leakage. | patch focused nested-exception tests |

All three independent layers completed against the original 65,076-byte diff. Acceptance remains pending; the exact historical persisted-state regression subsequently passed locally 1/1, generated-controller44/44, anonymous6/6 and REST-client128/128 all pass without skips. The local member-seeding failure remains separately recorded.

## Verification

- Build affected projects individually: Debug/source, `-p:UseNuGetDeps=false -m:1 -nr:false`; run maintained MTP suites with minimum counts and `--fail-skips on`.
- Run browser validator against `aspire describe` endpoint; retain safe source/binary-bound results.
- Require hosted Release/package persisted-state evidence before DW-594 closure.
- Run story-gitlink validation against this spec, ledger/index conservation, `git diff --check` and pinned commitlint. Preserve exact commands/results. Fetch and inspect divergence before push.

## Continued live qualification (2026-10-25)

The first candidate is main commit `3b60914ace5201a711fb51c1036d3679e0512ec8`; PR55 was merged externally while its validation was running. Hosted run [40200821264](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/40200821264) passed the exact historical persisted-state regression with zero skips, as well as the model and anonymous suites. Conditional detail returned HTTP200 instead of304; UI/browser failures exposed fixture defects.

Positive UI qualification now performs genuine signed loopback OIDC discovery/code exchange through production callbacks, populating the framework token store. Only the ambiguous-scope negative case uses a protected test ticket. Chromium receives the public development certificate in its active NSS store; no certificate validation is bypassed. The live request-path profile disables only existing projection background workers (`EventStore__Projections__WorkersEnabled=false`), because clean hosted execution reproduced a completed-command/persisted-sequence stall with workers enabled. This isolates request-path delivery; it does not qualify or repair worker concurrency. Default application configuration and dependency pins remain unchanged.

Real runtime tracing exposed the canonical user-lookup SSR self-redirect. The panel now leaves an already-canonical URL in place; a focused component regression and the actual HTTP route protect it. Administrator authority intentionally remains restricted in HTTP prerender under the existing circuit-only resolver policy. HTTP coverage asserts that restriction; the browser still requires real authorized administrator rows. Local real-browser execution has reached all six authorized surfaces. Responsive/interaction/Back acceptance is still being completed.

DW-161 cannot close with the approved dependency freeze: pinned EventStore Gateway3.127 only recognizes SelfRoutingETag conditional validators, while the Tenants detail handler emits the opaque store ETag; its production304 client path also drops version/served-at metadata. The live conditional assertion remains blocking and unchanged in meaning. Six ordinary REST reads and conditional detail are separate cases so the upstream failure does not suppress other evidence.

### Continued review triage

| Finding | Verdict / evidence | Route |
| --- | --- | --- |
| Final Edge1: publish-mode token lost | high: AddHexalithEventStore only owns run-mode registration; removing the unconditional caller also removed the prior publish token. Restore that same wiring only in publish mode, preserving prior policy. | patch |
| Final Edge2: member roles unchecked | medium: identifier comparisons accept incorrect per-user roles. | patch with Final Blind2 |
| Final Blind1: fixed caller memberships exceed first page | medium: repeated seeds exceed My Tenants page size20; its URL needs the cursor containing this seed. | patch |
| Final Blind2: identical seed roles and unchecked roles | medium: both owners plus ID-only assertions miss role corruption; use distinct roles and compare each persisted value. | patch |
| Final Blind3: audit count alone | medium: three duplicated/wrong events can satisfy count and tenant checks. Require actual seeded types/targets and unique identities. | patch |
| Final Blind4: malformed cursor only | medium: malformed decoding is proven but an actually issued incompatible-scope cursor is not covered. Add that concrete negative without decoding or changing contracts. | patch |
| Final Blind5: pinned coordinates without scroll | medium: the wrong wrapper can leave scrollLeft zero and pass stable-position checks. Require measured scroll movement when overflow exists. | patch |
| Final Blind6: bounds do not prove exposed content/action | medium: stable pinned positions can still be clipped or occluded on phone widths. Add actual viewport/hit checks and expose failures honestly. | patch |
| Final Blind7: contextual Back accepts unavailable audit | medium: preserved URLs alone do not prove usable contextual evidence. Require ready rows and rendered context before Back. | patch |
| Final Blind8: evidence binding omits backend/dependency contents | medium: dirty relevant dependency implementations can change while gitlink pointers remain stable. Capture the relevant source and backend binaries or explicitly narrow each claim. | patch |
| Final Blind9: TLS diagnostics not in focused gate | medium: only broader lanes run the five redaction checks. Add a blocking minimum-count selection. | patch |
| Final Blind10: early failures lack safe artifact | medium: failures before Node or outer cancellation leave no retained packet. Create an initial private destination and fixed wrapper failure outcome. | patch |

The second verification-gap layer reported no gaps. All three layers completed before this triage; no finding was dropped. The original13 rows remain preserved.

The final feasibility audit found a technically possible MVC filter workaround, but no existing platform callback. It would duplicate shared conditional-response/authority policy locally. The required Hexalith baseline explicitly requires adding shared boilerplate to its technical module first, then consuming it (`references/Hexalith.AI.Tools/hexalith-llm-instructions.md:59`). Thus DW-161 is constrained by platform ownership plus the approved dependency freeze, not claimed to be technically impossible.

The live phone matrix exposed an actual Fluent5 migration defect: old CSS selected the removed `fluent-data-grid` element, leaving the native table narrower than its tracks, so sticky columns scrolled out. The component now uses the vendor's documented CSS-width parameters with inherited responsive variables, and isolated styling targets the real `.fluent-data-grid` table with `width:max-content`. Measured390px evidence confirms all three critical cells stay pinned and visible through750px horizontal movement. The next action-occlusion failure remains under repair; no responsive closure is claimed yet.

## Owning prerequisite corrections

Runtime proved that tenant-detail/member audit launchers correctly refuse a null audit producer version, even with a Current projection-backed payload and store ETag. The owning tenant projection now certifies its existing audit ProjectionVersion only from a complete canonical tenant event prefix and the actual evidence after each retried merge. This applies the already established singleton-audit complete-replay authority principle to the tenant-owned stream. It does not borrow detail authority, infer an index validator, expand wire fields, update dependencies or manually mutate persisted state. Unknown/malformed/skipped/gapped/conflicting/unmatched evidence remains unversioned; compatible older replay preserves a certified newer prefix. DW-568 stays open and its record is untouched. Focused producer54 and downstream22 tests pass without skips; full server/live proof and independent recheck remain required.

The phone list also needs actual multiline rows and its existing 2.35rem copy-control footprint. Fluent-owned copy-root selectors now cross CSS isolation with ::deep, retaining all size/focus values. Mobile audit action construction is shared with desktop and uses distinct selectors. After updating the existing identity tests to check both copies, full UI7,706 passes with zero failures/skips. A subsequent copy-selector correction is undergoing its focused/full UI recheck and the real physical browser matrix.

### Producer/phone independent recheck triage

| Finding | Verdict / evidence | Route |
| --- | --- | --- |
| Recheck Blind1: hidden mobile launcher wins Back focus | medium: tenantsFocus.js selects the first audit launcher without visibility filtering; the new hidden mobile copy precedes desktop. An isolated Node control reproduces fallback heading focus. | patch visible enabled selection and real list Back control |
| Recheck Edge1: malformed lifecycle statuses certify | high: the existing status converter maps object/null to Unknown, and the new guard checks only non-string property presence. A complete creation plus malformed rejection probe certifies tenant-sequence:2. Require the converter's canonical recognized string shape before certifying. | patch |
| Recheck Edge2: hidden launcher wins focus | medium: same verified querySelector root cause as Recheck Blind1. | patch same root |
| Recheck Verification1: copy control physical proof missing | medium: preverified gap; identity-container hit testing cannot establish the actual Fluent copy button's containment, pointer access or focus outline. | patch live checks |
| Recheck Verification2: role-change negative certificate absent | medium: preverified gap; the added-role negative does not cover the separate changed.NewRole validation. | patch numeric invalid and valid role-change controls |

All three focused independent rechecks completed before triage. Full server ran1495 with1494passes/1failure/0skips: the sole failure is unchanged DeploymentReadinessDocumentationTests opening missing pinned upstream scripts/validate-operational-evidence.py. No source/dependency change is made for that separate documentation gate. Full UI after the copy-selector correction passes7706/7706 with zero skips.

Live progression after producer/copy corrections now passes all12 measured layout/forced-colors combinations and detail audit Back/focus, totaling20 completed cases before the member-context assertion times out. The latter assertion incorrectly searched empty audit row-marker spans for target text; it must inspect the actual rendered target cell, preserving the real ready-data requirement.

## Final local qualification checkpoint

All review corrections are verified. The malformed lifecycle-status probe now returns null authority; complete valid controls and unsupported-role controls pass. The real Chromium focus harness passes phone/desktop/forced-colors and rejects the broken first-candidate mutation. Live authenticated UI3/3 passes with zero skips and all25 browser cases pass, including actual copy containment/hit/Tab/outline, all12 measured layouts, detail/member/list Back and visible responsive launcher focus. The support-safe packet is [live-browser-local.json](dw-104-161-432-436-594-live-browser-local.json); its source diff digest still matched the final source at handoff. Full UI7706/7706 passes, zero skips.

Final real REST4 has3passes/1failure/0skips. All six reads, distinct roles, exact audit events and actual audit producer version matching the persisted committed prefix pass; cursor recovery and anonymous denial pass. Only conditional detail still returns200 instead of304. Full server1505 has1504passes/1unchanged missing-upstream-validator failure/0skips. Corrected hosted Release/package execution and ledger closure are the remaining authorized steps; DW-161 stays open under the explicit dependency freeze. Spec stays in-review while the five-item intent remains incomplete.


### Cold-bootstrap independent recheck triage

| Finding | Verdict / evidence | Route |
| --- | --- | --- |
| Cold Blind1: diagnostic response buffered before bound | medium: default SendAsync buffers the complete response before LoadIntoBufferAsync applies its limit; existing buffered content also bypasses that limit. | patch streaming bounded read and full transport control |
| Cold Blind2: caller outside first administrator page | medium: a valid singleton may have more than100 members preceding the caller, while polling repeats the first page only. | patch follow protected cursors |
| Cold Blind3: liveness does not ensure bootstrap dependencies ready | medium: platform base promises process liveness, and a transient initial transport/503 failure currently aborts the fixture immediately. The hosted early bootstrap already exposed the readiness race. | patch bounded transient retry preserving one command identity |
| Cold Blind4: accepted command terminal outcome discarded | medium:202 alone cannot distinguish a committed bootstrap from later rejection/failure and otherwise degrades into an authority timeout. | patch consume actual terminal status and committed sequence |
| Cold Blind5: cold gate can reuse existing singleton | medium: synthetic controls and a live path that accepts already-bootstrapped state do not explicitly require a fresh bootstrap in clean CI. | patch require a newly completed sequence1 bootstrap in the first hosted live step |
| Cold Edge1: later administrator page | medium: same first-page-only defect as Cold Blind2. | patch same root |
| Cold Edge2: response buffered before size guard | medium: same default HTTP buffering defect as Cold Blind1. | patch same root |
| Cold Verification1: initialization exception boundary untested | medium: preverified gap; helper-only redaction tests do not detect preserving a credential-bearing inner exception at the outer initialization boundary. | patch fault injection through the actual boundary and cleanup |

All three cold-bootstrap review layers completed before triage. No finding is omitted. These corrections retain real production transport, authenticated commands and producer metadata; they add no application contract, dependency update or direct state mutation.

Cold-bootstrap review patches pass all72 focused controls, zero skips, with a warning-free build. The exact live historical persisted-state regression also passes locally1/1, zero skips. Hosted first-use sequence1 proof remains required before the four otherwise qualified records can close; the conditional DW-161 limitation stays blocking and unskipped.


### Hosted certificate recheck triage

| Finding | Verdict / evidence | Route |
| --- | --- | --- |
| Hosted TLS1: dev server certificate imported as CA | medium: hosted physical browser fails first navigation with ERR_CERT_INVALID. Local passing browser uses P,, in the preferred existing legacy NSS store; CI overwrites both stores with C,,. Chromium specifies P,, for a self-signed server leaf. | patch the import trust kind only; retain TLS validation |

Fresh hosted24fb584b qualifies cold-bootstrap sequence1, six ordinary REST reads/cursor recovery, all HTTP contracts, anonymous denial and the exact historical persisted-state regression with zero skips. Conditional detail still fails200vs304. The browser's first-route certificate failure prevents physical layout/Back acceptance, so no additional record closes yet.
