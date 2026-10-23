# Runtime authentication and HTTP contract qualification — 2026-10-23

This batch resolves **DW-569, DW-571, DW-572, DW-577 and DW-592** from Tenants baseline `a95d1039db3fb1004a93cee871e0358dd41c7b9d`. DW-592 retains its duplicate relationship to DW-569. No dependency pointer, release policy or advisory Aspire-tier decision changes.

The original ledger fields remain verbatim under `pre-runtime-contracts-*` where current verification supersedes them. All 657 numbered records remain: **575 done / 82 open**, covered exactly once by **76 maintenance rows**. No unrelated record changes.

## Runtime evidence

The existing AppHost initially reported healthy Keycloak, EventStore and Tenants resources. EventStore had no configured app-channel token. An empty POST to its protected `/projections/changed` route **without a token returned 400**, demonstrating that it reached request validation.

After building the Debug source graph, the exact AppHost was restarted through:

```bash
SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust:/etc/ssl/certs" aspire start --no-build --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json --non-interactive
```

`aspire wait` confirmed healthy `security`, `eventstore` and `tenants`. A fresh private `aspire describe --format Json` supplied endpoint/process identity. The maintained validator privately reads generated credentials from these local Development processes; neither their environments nor tokens are retained in tracked evidence.

```bash
SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust:/etc/ssl/certs" python3 scripts/validate-runtime-auth-contracts.py --state /tmp/tenants-deferred-five-aspire-state.json
```

TLS certificate verification stays enabled, redirects are refused, and credential grants and direct app requests require local HTTPS; the separate fixed-path Dapr invocation uses only the SDK-configured local loopback endpoint and optional sidecar API token. Actual Keycloak `client_credentials` exchange returns **200**. Its issued assertion identifies the EventStore workload, has Tenants audience, and grants exactly `domain-service:project`. The valid protected Tenants project request returns **200**; invalid client credentials return **401**.

| Protected route control | Observed HTTP status |
| --- | --- |
| Tenants project: valid workload assertion and own channel | 200 |
| Missing, wrong or other-app channel | 401 |
| Missing assertion or bearer used in its place | 401 |
| Issued token missing project operation | 401 |
| Issued token granting query instead of project | 403 |
| Issued token with sample audience | 401 |
| EventStore notification: own channel, empty invalid notification | 400 |
| EventStore notification: missing, wrong or other-app channel | 401 |

The EventStore **400** is a positive channel-admission control, not a successful notification: the empty identity is rejected before provenance verification, actor calls or broadcasting. The tenant-domain projection uses an empty event array and an isolated aggregate identity; the handler returns before store access. Eleven in-process route controls also verify unchanged administrator sentinel contents/ETag and absent tenant/index/audit records. No business command, global-singleton replay or domain-state mutation is used for this qualification.

Validator failure controls confirm that group/world-readable state files and nonlocal endpoints fail with exit **1** and fixed safe diagnostics. The safe runtime observations and these controls are in [runtime results](dw-569-571-572-577-592-runtime-results.json).

## Blocking contract coverage

The new `Blocking HTTP contracts` job in `.github/workflows/ci.yml` builds the integration test assembly in **Release/package mode**, explicitly disabling FrontComposer source fallback. It starts no Aspire topology, grants only inherited read permissions, and fails on test skips or fewer than the expected **44 + 18 + 11** cases. The existing advisory Aspire tier is unchanged.

- **44 generated HTTP/REST cases** qualify the real generated API's ETag, freshness, conditional responses and REST-consumer agreement.
- **18 command-status cases** traverse the real production status controller and `TenantCommandGateway`: valid tenant/global-administrator proof preserves message/correlation identity, tenant/domain/aggregate, event count and sequence. Sixteen malformed identity/scope/count/sequence controls withhold committed-sequence proof. This assertion does not redefine the other evidence policies used by create/member lifecycle confirmation. The test fixture bypasses its legacy identity-fabricating adapter only for these tests.
- **Eleven project controls** qualify the real route's workload assertion/channel boundary and persisted store end state without writes, including forged signature, expiry, wrong issuer and wrong caller.

Each group passes in both Debug/source and Release/package modes, with **zero skips**. Both builds have **zero warnings/errors**. The complete affected `CommandApiRuntimeIntegrationTests` class passes **117/117** after fixture changes. The AppHost security class passes **8/8**, including token sharing and pairwise token isolation in both authentication modes. The complete AppHost suite passes **9/9** in its intended Release/package mode. Exact commands, results, corrected initial attempts, and source/binary fingerprints are in [source checks](dw-569-571-572-577-592-source-checks.json).

The first generated-class run used an estimated minimum of 50: all 44 discovered assertions passed, but the runner correctly exited **9** for insufficient count. The committed guard uses the observed complete count of 44, which passes. A broad Debug/source AppHost run passed security 8/8 but failed the existing Memories model test because it explicitly requires package mode; the unchanged intended package-mode suite passes 9/9 separately, without weakening or skipping the assertion.

## Verification and scope

`actionlint .github/workflows/ci.yml`, Python compilation, the story gitlink guard, whitespace checks and exact ledger/index coverage checks qualify the changed artifacts. Final independent-review findings and any corrections are recorded in the colocated spec; publication follows those checks and exact-message pinned commitlint validation.

These are local executions against the rebuilt application and production in-process contracts. No new remote CI pass, full distributed Aspire-suite pass, live generated-route ETag qualification, or release-readiness approval is claimed. DW-594 and the remaining platform/product decisions stay open. The new blocking job's configured contract coverage is distinct from those retained live-topology gaps.

The preliminary no-restore build failed with NETSDK1004 because required assets were missing; the normal restoring build passed. The exact failed command and subsequent passing commands are preserved in the source-check packet.

Runtime qualification here covers Keycloak mode. The both-authentication-mode evidence is the evaluated AppHost app/sidecar model, not a claim of a second live signing-key topology run. Independent review corrections qualify the control tokens, actual sidecar delivery and live assembly provenance; the complete triage is retained in the spec.

After the review corrections, the parent reran the normal Debug source build and complete runtime class (117/117), then the exact Release/package build and all blocking filters (44/44, 18/18 and 11/11), with zero warnings/errors/skips. Validator failure controls now include no-follow symlink rejection as well as owner-only permissions and nonlocal endpoint rejection.

## Final runtime requalification

After final builds, restarting just the EventStore/Tenants project resources left direct calls healthy but own-sidecar delivery returned `ERR_DIRECT_INVOKE` / HTTP 500. The validator correctly failed; that rejected packet is retained separately in runtime results. A full `aspire start --no-build` restart of the exact AppHost restored healthy resources and the complete validator passed unchanged, including own-sidecar HTTP 400, all negative-token claim assertions and all direct channel controls. No endpoint, TLS or authorization policy was relaxed.

The accepted runtime packet records the baseline root revision, the dirty candidate file list, all nine canonical unchanged root dependency gitlinks with clean checkouts, and SHA-256 for 20 actually mapped Hexalith assemblies across EventStore and Tenants. The expected child executable identities and each mapped/current file inode were checked after restart, with no deleted mappings accepted. This binds the live authentication implementation independently of AppHost/test fingerprints. Initial and final source checks remain separate; only final corrected source/binary fingerprints describe the 117-case and 73-case acceptance runs.

All three independent correction rechecks are clear. Blind Hunter independently verified 28 current source/binary fingerprints, token/own-sidecar results and all nine unchanged clean dependency checkouts; Edge Case Hunter reports no findings and Verification Gap Reviewer reports no gaps. No new work is deferred. The exact complete Conventional Commit message passes the owning pinned commitlint 21.2.3 with zero problems/warnings.
