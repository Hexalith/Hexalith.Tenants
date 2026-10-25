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

- [ ] `src/Hexalith.Tenants.AppHost/Program.cs`, `tests/Hexalith.Tenants.AppHost.Tests/AppHostSecurityResourceGraphTests.cs` — remove redundant token registration; assert unique ownership.
- [ ] `tests/Hexalith.Tenants.IntegrationTests/Fixtures/AspireTopologyFixture.cs`, `tests/Hexalith.Tenants.IntegrationTests/AspireTopologyTests.cs` — configure trusted certificates and safe bounded TLS diagnostics.
- [ ] `tests/Hexalith.Tenants.IntegrationTests/AuthenticatedTenantsRouteTests.cs` — add live six-route, metadata, 304 and invalid-cursor proof.
- [ ] `tests/Hexalith.Tenants.IntegrationTests/AuthenticatedTenantsUiRouteTests.cs` — add authenticated routes and contextual audit navigation.
- [ ] `tests/Hexalith.Tenants.UI.Tests/Browser/validate-authenticated-live-routes.mjs` — exercise Aspire endpoints, measured widths, forced colors and Back.
- [ ] `.github/workflows/ci.yml` — add focused Release/package qualification with certificate trust, minimum counts and skip rejection; preserve broad advisory tier.
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
