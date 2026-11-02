---
title: 'Restore integration contracts against the published canonical cohort'
type: 'bugfix'
created: '2026-11-02'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 1bcc27d48997981d486eee72d465d3026cec4d6b
context:
  - /tmp/tenants-published-ci-work-_0z70gqx/Hexalith.Tenants/AGENTS.md
  - /tmp/tenants-published-ci-work-_0z70gqx/Hexalith.Tenants/references/Hexalith.AI.Tools/hexalith-llm-instructions.md
  - /tmp/tenants-published-ci-work-_0z70gqx/Hexalith.Tenants/references/Hexalith.AI.Tools/hexalith-git-instructions.md
---

<frozen-after-approval reason="User authorized fixing all CI failures and explicitly waived human validation checkpoints">

## Intent

Restore meaningful Tenants integration coverage against published EventStore4.0.0 and McpCli.Abstractions2.0.0 so the existing release can proceed when its protected gates pass. Merging all branches is complete; current remaining failures involve obsolete public authorization fixtures and incomplete catalog leases.

## Boundaries & Constraints

Preserve current canonical platform scopes, exact permissions, cryptographically bound bootstrap, persisted administrator verification, native source proofs and retained internal system identities. Do not change production behavior merely to satisfy old test expectations, disable tests, bypass lease/authority checks, invent package versions, weaken CI or approve a protected environment. Fix genuine runtime defects only with explicit source evidence, reporting them before expanding beyond the listed fixture repair.

Work ONLY in the isolated repository `/tmp/tenants-published-ci-work-_0z70gqx/Hexalith.Tenants`. Its nine root-declared dependencies are frozen detached worktrees at the recorded baseline gitlinks; do not edit them or initialize their nested submodules. The shared original workspace has concurrent active work. The parent owns applying the patch, reviews, commits, pushes, CI monitoring and release dispatch. Do not commit, push, rebase, switch branches or alter the original workspace.

</frozen-after-approval>

## Code Map

- Implementation root is the isolated checkout above. All paths below are relative to it. Existing original `spec-synchronize-dependencies-and-release-2026-11-02.md` records completed merges; leave that historical artifact unchanged.
- `/tmp/tenants-all-branches-command-runtime-tests.log`: full real-package CommandApiRuntimeIntegrationTests run178 cases/78failures. `/tmp/tenants-all-branches-published-controls.log`: BoundBootstrap* and CommandStatusContract*31passed. Actual producer packages were fetched from nuget.org into `/tmp/tenants-published-cache-1eqjrydq/packages`; `/tmp/tenants-published-cache-1eqjrydq/fallback` links other cached packages but excludes EventStore/McpCli IDs, avoiding old locally manufactured4.0 candidates.
- `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs`: public envelope helpers near2770–2915 still use system; CreateJwt defaults system and lacks exact platform permissions. Use TenantPlatformScopes.Tenants or GlobalAdministrators by operation. Exact permissions come from `src/Hexalith.Tenants.Contracts/Routing/TenantCatalogDeclarations.cs`: tenants:create/update/disable/enable/add-user/remove-user/change-user-role/set-configuration/remove-configuration and global-administrators:set/remove. Bootstrap stays bound-only; generic public authentication tests should use an ordinary Update command.
- Public admission validates every grant and exactly one operation permission. Empty grants can be valid for a platform operation; reserved/duplicate/malformed grants cannot. Never silently repair intentional negative JWTs with new claims. Keep legacy command:submit/commands:*/CLR-type permissions as denial cases; add correct exact-permission acceptance and duplicate-exact denial. Source-claim positives use canonical source tenants and source alias permissions; any direct eventstore:* claim suppresses source transformation. Early admission denial is bare403, while downstream domain rejections keep ProblemDetails. Keep routed command/status/problem storage tenant system unchanged.
- `Fixtures/TenantOwnerCatalogTestAuthority.cs` returns manually constructed RouteCatalogDispatchLease with internal Activation null. This causes four Process and12 NativeAuthority failures before their fake native reader runs. Use actual SDK RouteCatalogAuthority.AcquireAsync with isolated CAS IRouteCatalogStateStore, non-null coordinates, committed activation, matching observer/trust validator. Do not remove RouteCatalogInvocationScope or relax ReadCount==1. Prefer SDK testing helpers if available; otherwise add minimal test-only types, one type per .cs file with internal documentation.
- Exact producer reference is published tag/source653124dced81830e932edb7738a78ee61104d55a. Its `tests/Hexalith.EventStore.Server.Tests/Routing/RouteCatalogTestFixture.cs`, RouteCatalogTestStateStore and NativeSourceReadScopeTests show genuine lease acquisition. Read through git show if the frozen checkout has later changes; do not borrow unpublished APIs. Existing TenantsApiGatewayResilience/TenantOwnerSourceRuntime snapshot helpers also return bare leases and alone are insufficient.
- `CommandApiRuntimeIntegrationTests.NativeAuthority.cs`: preserve internal /process and /query system envelopes, stored events and all500/redaction/ReadCount assertions. Public gateway legs separately need declared scope and exact read permission. `Fixtures/TenantGatewayTestHost.cs` already registers the catalog fixture in package mode; retain that registration.

## Tasks & Acceptance

- [x] Repair public request/JWT fixtures and tests, retaining each original business, authentication and no-dispatch purpose under the current contract.
- [x] Replace incomplete lease authority with genuine SDK-owned acquisition; add only the test support types needed and preserve native proof checks.
- [x] Build and run the complete CommandApiRuntimeIntegrationTests class, then relevant related catalog/gateway tests, with real published dependencies and no skips.
- [x] Append changed paths, exact commands/results and unresolved concerns below; report a concise implementation handoff without commits or original-workspace edits.

Given a correct public operation scope and exact permission, when the HTTP request is submitted, then the fixture reaches its intended handler or domain rejection; missing, duplicate or wrong authority fails before routing.

Given authenticated internal callback envelopes, when process/query executes, then retained system identity remains intact and current native verification runs.

Given unavailable native authority, when a protected operation executes, then existing fail-closed responses, redaction and read-count checks remain meaningful.

Given the real published producer packages, when the repaired class runs, then all cases pass without exclusions, skips or lowered minima.

## Implementation Notes

User authorization covers the CI repair and subsequent release work without another local approval. Enforced GitHub production approval remains binding. The isolated checkout prevents interference with concurrent main edits; root status was clean when its canonical baseline was captured.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Evidence and route |
| --- | --- | --- |
| Blind 1: source claim shapes can pass without tenant normalization | medium | The catalog filter accepts empty grants for a platform operation, and these HTTP cases only assert 202/internal system routing. A lost source tenant mapping therefore escapes them. Patch request-time transformed-claim assertions, preserving source alias and precedence purpose. |
| Blind 2: other valid platform namespace absent from refusal matrix | low | The updated matrix contains only storage/ordinary request scopes. A cross-platform namespace error has no direct case here although SDK GeneratedRouteCatalogFilter compares the declared namespace exactly. Add the missing matrix row as a direct coverage correction. |
| Blind 3: case/whitespace permission variants absent | low | New permission tests cover legacy aliases and duplicates, but no variant of the exact permission. The SDK uses Ordinal comparison; add denial data rows so the HTTP suite observes that boundary. |
| Blind 4: unrelated additional permissions lack positive control | low | Successful direct claims use one permission overall, so this matrix cannot detect a blanket single-permission restriction. Add one positive request with the required permission once and an unrelated permission. |
| Blind 5: distinct multiple direct grants lack positive control | low | The new grant matrix covers zero/one valid grant and invalid two-grant collections. Add a successful distinct two-grant request including normalized uppercase input; combine with Blind 4's positive collection control. |
| Blind 6: acquired snapshot replaces independent expected input | medium | ConfigureCatalogAuthorityControl overwrites selected before header assertions. Although endpoint/credentials remain independent, root/epoch/inventories can redefine their expected values. Assert retained input fields before adopting the SDK-generated activation binding. Patch. |
| Blind 7: auxiliary/projection-only participant omitted | false | All actual Create callers use the compiled owner routes and optional eventstore technical receiver; participant apps are already in route/idempotency/local/technical inventories. No reachable caller supplies a new auxiliary/projection-only owner. The claim that this change loses an owner in these proofs is not demonstrated; broadening a test fixture for hypothetical snapshots is unnecessary. |
| Blind 8: durable release can silently leak fences | false | Create returns the real SDK RouteCatalogAuthority, which persists acquisition and removes its fence in its SDK-owned release callback; no custom no-op release remains. Producer RouteCatalogActivationTests.PrepareFencesNewDispatchAndCannotCommitUntilDurableLeasesDrain and TerminalRefusalHttpTests.TerminalResponseRetainsLeaseThroughExecutionAndDrainsOnEveryExit already cover the state invariant and exceptional exits. No leak in this fixture was demonstrated. |
| Edge reviewer note: source tenant claims no longer observed | medium | Same verified defect as Blind 1; grouped into its request-time observation patch. The edge review's final edge-case list is empty. |

Verification-gap reviewer: no verification gaps found. All three layers completed before triage. No findings require a spec loopback or a deferred-work entry. Patch work is confined to test assertions/data.

## Verification

Restore/build the IntegrationTests project in the isolated checkout, Release package mode, with UseHexalithProjectReferences=false, UseNuGetDeps=true, HexalithEventStoreFromSource=false, HexalithFrontComposerFromSource=false and HexalithMemoriesFromSource=false. Use NUGET_PACKAGES and RestorePackagesPath `/tmp/tenants-published-cache-1eqjrydq/packages` plus RestoreFallbackFolders `/tmp/tenants-published-cache-1eqjrydq/fallback` on restore/build. Source imports must not satisfy package validation.

Invoke the built assembly directly with `-class Hexalith.Tenants.IntegrationTests.CommandApiRuntimeIntegrationTests`; inspect final failure/skip counts. Run only additional relevant classes required by the shared authority fixture change. Inspect the final diff and whitespace. Parent verifies artifact provenance, applies current-root-compatible changes, validates exact commit messages with pinned commitlint and preserves CI source gates.

## Implementation Handoff (2026-11-02)

All edits were confined to the isolated checkout. No production source, dependency gitlink, workflow gate or original-workspace file was changed. No commits, pushes, branch changes or submodule operations were performed.

Changed paths:

- `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs`: public request helpers now select the declared Tenants or GlobalAdministrators scope; authorized tokens carry explicit operation permissions and canonical grants. Explicit negative tokens remain literal. Generic authentication checks use UpdateTenant; early admission denials assert bare HTTP 403 and downstream management-scope/domain denials retain ProblemDetails. Source-claim positives use canonical source grants and source-alias permissions. Legacy wildcard/category/CLR permission tokens remain denial cases. Added ten cases proving empty/canonical grants with one exact permission, duplicate exact-permission rejection and invalid grant-collection rejection. Internal system envelopes and routed/status/problem storage assertions remain unchanged.
- `tests/Hexalith.Tenants.IntegrationTests/Fixtures/TenantOwnerCatalogTestAuthority.cs`: replaced fabricated leases with real SDK prepare/ready/commit and AcquireAsync ownership. Host observations, Development trust, authority coordinates, optional technical receivers and projection/auxiliary inventories match the committed activation.
- `tests/Hexalith.Tenants.IntegrationTests/Fixtures/TenantOwnerCatalogTestStateStore.cs`: new isolated serialized CAS state store, with one documented internal type and non-null coordinates.
- `tests/Hexalith.Tenants.IntegrationTests/TenantOwnerSourceGatewayTests.cs`: header proofs now compare against the snapshot acquired from the real SDK authority. Exact receiver, credential and independently selected endpoint/coordinate assertions remain intact.

`CommandApiRuntimeIntegrationTests.NativeAuthority.cs` and its system envelopes, persisted-state assertions, HTTP 500/redaction and ReadCount checks were unchanged. SDK fixture/source examples were read from producer commit `653124dced81830e932edb7738a78ee61104d55a`; no unpublished API was introduced.

Exact successful verification commands (working directory is the isolated checkout):

```bash
NUGET_PACKAGES=/tmp/tenants-published-cache-1eqjrydq/packages dotnet restore tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj -p:Configuration=Release -p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true -p:HexalithEventStoreFromSource=false -p:HexalithFrontComposerFromSource=false -p:HexalithMemoriesFromSource=false -p:RestorePackagesPath=/tmp/tenants-published-cache-1eqjrydq/packages -p:RestoreFallbackFolders=/tmp/tenants-published-cache-1eqjrydq/fallback
NUGET_PACKAGES=/tmp/tenants-published-cache-1eqjrydq/packages dotnet build tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Release --no-restore -m:1 -p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true -p:HexalithEventStoreFromSource=false -p:HexalithFrontComposerFromSource=false -p:HexalithMemoriesFromSource=false -p:RestorePackagesPath=/tmp/tenants-published-cache-1eqjrydq/packages -p:RestoreFallbackFolders=/tmp/tenants-published-cache-1eqjrydq/fallback
NUGET_PACKAGES=/tmp/tenants-published-cache-1eqjrydq/packages dotnet tests/Hexalith.Tenants.IntegrationTests/bin/Release/net10.0/Hexalith.Tenants.IntegrationTests.dll -class Hexalith.Tenants.IntegrationTests.CommandApiRuntimeIntegrationTests
NUGET_PACKAGES=/tmp/tenants-published-cache-1eqjrydq/packages dotnet tests/Hexalith.Tenants.IntegrationTests/bin/Release/net10.0/Hexalith.Tenants.IntegrationTests.dll -class Hexalith.Tenants.IntegrationTests.TenantsApiGeneratedControllerTests -class Hexalith.Tenants.IntegrationTests.TenantsApiGatewayResilienceTests -class Hexalith.Tenants.IntegrationTests.TenantOwnerSourceGatewayTests
git diff --check
```

Results: restore exit 0; Release build exit 0 with 0 warnings / 0 errors; full CommandApiRuntimeIntegrationTests: 188 passed / 0 failed / 0 skipped / 0 not-run (79.337s); related classes: 100 passed / 0 failed / 0 skipped / 0 not-run (75.597s); whitespace check exit 0. Logs: `/tmp/tenants-published-ci-restore.log`, `/tmp/tenants-published-ci-build-final.log`, `/tmp/tenants-published-ci-build-verified.log`, `/tmp/tenants-published-ci-runtime-tests-final.log`, `/tmp/tenants-published-ci-related-tests-final.log`. The final rebuild only follows whitespace formatting; no behavioral edits followed the passing test runs.

Package asset inspection confirms the EventStore runtime/client/contracts/gateway/server/ServiceDefaults/testing producers resolve as 4.0.0 and McpCli.Abstractions as 2.0.0, all as NuGet packages from the specified package/fallback roots. The deliberately frozen Testing.Integration harness remains 3.128.4 as configured before this change. The nine dependency worktrees remain unmodified and detached at their recorded baseline.

Earlier diagnostic runs found one obsolete bare HTTP 403 expectation and loss of optional technical receiver membership during the first fixture replacement. Both were corrected before the passing runs above; no runtime defect or production change was needed.

No implementation work remains incomplete. Parent-owned reviews, patch application, pinned commitlint validation, commits/pushes, GitHub package/source CI and release/protected-environment gates remain outside this handoff. These Development-only in-memory authority proofs do not claim deployed topology/production activation evidence.

Concurrent integration: another active session committed the exact implementation patch as `52522bd8f3e82a85b44de2b91ba427069c3e6eba` and pushed it before parent review. Reverse-application of the handed-off patch succeeds against current root. No duplicate patch was applied. Subsequent commit `bbaabb4db49a6d373abecdd929ce4859b08843c6` synchronizes Builds and Platform qualification fixes. Review covers the implementation code; dependency pointer updates remain separately attributed.

## File List

- `.github/workflows/ci.yml`
- `Directory.Build.targets`
- `scripts/prepare-source-catalog-profile.py`
- `scripts/tests/test_source_catalog_profile.py`
- `tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/SourceCatalogOwnerProfileTests.cs`
- `tests/scripts/test_bootstrap_compatibility.py`

- `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/Fixtures/TenantOwnerCatalogTestAuthority.cs`
- `tests/Hexalith.Tenants.IntegrationTests/Fixtures/TenantOwnerCatalogTestStateStore.cs`
- `tests/Hexalith.Tenants.IntegrationTests/TenantOwnerSourceGatewayTests.cs`
- `references/Hexalith.Builds`
- `references/Hexalith.Platform`

## Completion Notes List

Builds contains qualification corpus and readiness hash fixes; Platform removes a duplicate catalog consumer SDK reference. Both were committed separately in bbaabb4d. The baseline remains the original captured start. These separately attributed pointer changes occurred while the isolated implementation was running; they are included here to preserve exact repository provenance.

| Path | Baseline | Target |
| --- | --- | --- |
| `references/Hexalith.Builds` | `207dcd0d72b57d30fdc99105357a23f3b1483bd3` | `e7b8324e88f0d7f58bdc838c4a1fc9b7ca5faff6` |
| `references/Hexalith.Platform` | `932e93a9499f7ab74fb99ab29f431680e52ba874` | `28737966f2ec0cdbfc16e89a31c348f03a8058d2` |

The first provenance check rejected inline descriptions on the dependency File List entries because declarations must contain the path alone. The corrected entries and unchanged exact-target table pass the guard; no baseline was narrowed.

Final fresh fetch found Builds6585f5860e87c146c26aa066d3a52b94eb56f271, aligning the executable v1 schema and positive fixture with EventStore4.0.0. Fast-forwarded only the clean declared Builds checkout and recorded its pointer separately in b7edfc44. This final row supersedes the intermediate Builds target:

| Path | Baseline | Target |
| --- | --- | --- |
| `references/Hexalith.Builds` | `207dcd0d72b57d30fdc99105357a23f3b1483bd3` | `6585f5860e87c146c26aa066d3a52b94eb56f271` |

Merged the later three origin/test/story-2-14-dapr-readback commits in d83b4a9b. Its seven-file patch provisions package catalog material on a proven fresh Dapr backend, aligns package/source compilation symbols and retains all six canonical package bootstrap build checks. Applied that exact branch patch in the isolated checkout before final verification. All fetched root/submodule branches are now ancestors of their owning main branches.

Post-review verification in the isolated checkout: serialized Release build with -warnaserror and the same explicit package/cache/source flags succeeded with0 warnings/errors (38.67s), log `/tmp/tenants-final-canonical-build.log`. The exact complete CommandApiRuntimeIntegrationTests assembly invocation recorded above passed193/193, zero errors/failures/skips/not-run, exit0 (69.767s), log `/tmp/tenants-final-canonical-runtime.log`. The same assembly invocation for TenantsApiGeneratedControllerTests, TenantsApiGatewayResilienceTests and TenantOwnerSourceGatewayTests passed100/100, zero errors/failures/skips/not-run, exit0 (76.934s), log `/tmp/tenants-final-canonical-related.log`. Both include all review patches and the incoming branch configuration.

`python3 -m unittest discover -s scripts/tests -p test_source_catalog_profile.py -v` passed3, and `python3 -m unittest discover -s tests/scripts -p test_bootstrap_compatibility.py -v` passed12. Child checks also passed21 affected command cases and25 owner-source cases. Parent full verification supersedes those focused results. Existing isolated dependency worktrees remained frozen; later Builds changes only schema/fixture qualification and Platform only removes a duplicate SDK reference, with their own GitHub validation required.

All three independent reviews completed, all accepted findings were patched and verified, and none were deferred. Patches retain live transformed tenant grants and precedence, independent catalog expectations, cross-namespace refusal, exact permission spelling and multi-grant/multi-permission acceptance. GitHub release/publication still requires successful exact-source CI and the existing production environment rule.

The first root copy guard compared raw repository bytes to CRLF checkout bytes and safely refused before editing; normalized Git blob identities confirmed both paths were unchanged. The first final provenance check also rejected the obsolete intermediate Builds target; the final row above fixes that receipt without narrowing the baseline.
