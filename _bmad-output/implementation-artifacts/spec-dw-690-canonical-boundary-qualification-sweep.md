---
title: 'Deferred canonical-boundary and package-cohort qualification sweep (DW-690)'
type: 'bugfix'
created: '2026-11-27'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '5511006e0b691de5f6fca33823b0a06c0ec08ec1'
context: ['{project-root}/AGENTS.md']
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** After the prior UI runtime qualification sweep (`spec-deferred-ui-runtime-qualification-sweep.md`), the highest-priority eligible open deferred-work item is `DW-690` (`medium`), anchoring the sibling parent subject of Tenants canonical-boundary, package-cohort, and release qualification (`DW-668`, `DW-686`, `DW-690`, `DW-691`, `DW-705`, `DW-710`, `DW-672`, `DW-677`, `DW-678`, `DW-715`). Production Tenants compilation has never been verified against the real older advertised EventStore package cohorts (`5.3.0`, `5.3.1`, `5.4.0`, `5.5.1`, `5.5.2`, `5.6.0`) beyond synthetic marker assemblies, and `RefreshTenantAccess` (`DW-710`) currently emits unvalidated historical status, roles, or blank keys that `TenantProjectionHandler.IsCompleteAuditPayload` refuses.

**Approach:** Work through the 10-item sibling group in priority order (`medium` before unspecified, tie-broken by DW ID). Record and skip already-resolved (`DW-668`, `DW-686`, `DW-691`, `DW-705`, `DW-672`, `DW-715`), blocked (`DW-677`), and obsolete/unrecoverable (`DW-678`) entries without counting them toward ten. Assign a dedicated subagent to implement and verify each eligible item (`DW-690` and `DW-710`), committing each implementation separately with pinned commitlint and its DW ID.

**Approved Decisions:**
- **DW-710 policy (Fail-closed rejection):** `TenantAggregate.Handle(RefreshTenantAccess)` must reject malformed historical authoritative `TenantState` (status other than `Active`/`Disabled`, blank or non-assignable member roles, or blank configuration keys) before emitting `TenantAccessRefreshed`, matching `TenantProjectionHandler.IsCompleteAuditPayload`.
- **Shortfall finalization:** Even though this sibling group exhausts after 2 new implementations (short of 10), run the full solution build and individual test suites (fixing and separately committing any failures until both pass), mark the newly verified items (`DW-690`, `DW-710`) as `done` in `deferred-work.md`, commit the ledger update, push directly to `main` (which also pushes existing local commits `420bc97d` and `5511006e`), and report the shortfall and open count.

## Boundaries & Constraints

**Always:**
- Work from current `HEAD` (`5511006e0b691de5f6fca33823b0a06c0ec08ec1`) on `main` and preserve existing local commits (`420bc97d`, `5511006e`), untracked `spec-deferred-ui-runtime-qualification-sweep.md`, and all ten modified `references/*` submodule pointers untouched.
- Validate every commit message with the repository's pinned commitlint CLI (`npx commitlint --edit <file>`) before committing, including the DW ID.
- Qualify real production Tenants sources (`Hexalith.Tenants`, `Contracts`, `Server`, `Client`, `Testing`, `Aspire`) in Release/package mode (`UseHexalithProjectReferences=false`, `UseNuGetDeps=true`, `-warnaserror`) against actual published EventStore packages — never synthetic marker assemblies.
- Run test projects individually (not solution-level `dotnet test`) and keep every `.cs` file focused on a single type with XML documentation, Allman braces, and support-safe rejection payloads (no raw secret values).

**Never:**
- Never revert, stage, or bump unrelated `references/*` submodules or initialize nested submodules.
- Never weaken compiler warnings, NuGet audits, package-cohort guards, or audit-certification checks.
- Never fabricate or retroactively attribute fresh reports to the missing 2026-11-13 `/tmp/tenants-net11-repair-ng54_u4z/` run (`DW-678`).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Real EventStore cohort compilation (`DW-690`) | Advertised EventStore version in `Directory.Build.targets` (`5.3.0..5.6.0`), Release/package mode | All production Tenants projects (`Contracts`, `Server`, `Client`, `Testing`, `Aspire`, `Hexalith.Tenants`) compile with 0 warnings/errors | Any advertised version that fails real compilation is removed from `ValidateCanonicalBoundaryPackageCohort` and rejected by guard tests |
| Malformed historical `TenantStatus` (`DW-710`) | `TenantState.Status` is `TenantStatus.Unknown` or undefined enum value | `RefreshTenantAccess` rejects without emitting `TenantAccessRefreshed` | Emits support-safe rejection (`status` field) |
| Malformed historical member (`DW-710`) | Blank/whitespace member key or `TenantRole.Unknown` / undefined role | `RefreshTenantAccess` rejects without emitting `TenantAccessRefreshed` | Emits support-safe rejection (`members` field), no raw key/secret echoed |
| Malformed historical config key (`DW-710`) | Blank/whitespace key in `TenantState.Configuration` | `RefreshTenantAccess` rejects without emitting `TenantAccessRefreshed` | Emits support-safe rejection (`configurationKeys` field), no config values leaked |
| Valid `Active`/`Disabled` state (`DW-710`) | Valid status, assignable roles, non-blank config keys within limits | Emits sorted `TenantAccessRefreshed` accepted by `IsCompleteAuditPayload` | N/A |

</frozen-after-approval>

## Code Map

- `Directory.Build.targets:49-55` -- `ValidateCanonicalBoundaryPackageCohort` advertises EventStore `5.3.0`, `5.3.1`, `5.4.0`, `5.5.1`, `5.5.2`, and `5.6.0` for current production Tenants projects (`Hexalith.Tenants`, `Contracts`, `Server`, `Client`, `Testing`, `Aspire`).
- `Directory.Build.props:4-6,69-97` -- Default package-mode settings (`UseHexalithProjectReferences=false`, `UseNuGetDeps=true`, `HexalithMcpCliVersion=3.0.0`).
- `tests/scripts/test_catalog_artifact_guards.py:17-135` -- Existing guard tests; builds only synthetic marker assemblies (`PackFixtures.slnx`), not real production Tenants sources.
- `src/Hexalith.Tenants.Contracts/Events/Rejections/TenantAccessSnapshotLimitExceededRejection.cs` -- Pattern for recovery rejection contracts (`IRejectionEvent`).
- `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs:253-303` -- `Handle(RefreshTenantAccess, TenantState?, CommandEnvelope)`; add fail-closed historical-state validation before snapshot creation/validation.
- `src/Hexalith.Tenants.Server/Aggregates/TenantState.cs:145` -- `Apply(TenantAccessSnapshotLimitExceededRejection)` pattern; add no-op `Apply` overload for the new rejection event.
- `src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs:52,357-374` -- Known event registration list and `IsCompleteAuditPayload` parity rules (`Active`/`Disabled`, non-blank assignable member roles, non-blank unique configuration keys).
- `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAccessRecoveryTests.cs`, `tests/Hexalith.Tenants.Client.Tests/Projections/TenantProjectionTransitionTests.cs`, `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs` -- Recovery, projection transition, and rejection HTTP-mapping tests.
- `_bmad-output/implementation-artifacts/deferred-work.md` -- Ledger to update for newly verified items (`DW-690`, `DW-710`) after full build and test suites pass.

## Tasks & Acceptance

**Execution:**
- [x] `DW-690`: `tests/scripts/test_eventstore_cohort_compilation.py` (and `Directory.Build.targets` + `tests/scripts/test_catalog_artifact_guards.py` if any advertised older cohort fails real compilation) -- Dedicated subagent compiles real production Tenants projects (`Contracts`, `Server`, `Client`, `Testing`, `Aspire`, `Hexalith.Tenants`) in Release/package mode (`-warnaserror`) against every advertised EventStore package cohort (`5.3.0`, `5.3.1`, `5.4.0`, `5.5.1`, `5.5.2`, `5.6.0`) using isolated intermediate/output directories; trims any failing cohort from `Directory.Build.targets` or verifies all passing cohorts with an automated regression test, then commits with `(DW-690)` -- Replaces synthetic-only proof with real production compilation evidence.
- [x] `DW-710`: `src/Hexalith.Tenants.Contracts/Events/Rejections/TenantAccessStateMalformedRejection.cs`, `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`, `src/Hexalith.Tenants.Server/Aggregates/TenantState.cs`, `src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs`, and unit/client/integration rejection tests -- Dedicated subagent implements fail-closed rejection for malformed historical status, blank/non-assignable member roles, and blank configuration keys before snapshot emission, wires projection/API rejection awareness, verifies all affected test suites, and commits with `(DW-710)` -- Aligns recovery emission with `IsCompleteAuditPayload` fail-closed certification.
- [x] Finalization: Run full solution build (`Hexalith.Tenants.slnx`) and all individual test projects + maintained Python guard suites; fix and separately commit any failures until green; mark `DW-690` and `DW-710` as `done 2026-11-27` with resolution lines in `_bmad-output/implementation-artifacts/deferred-work.md`, commit the ledger update, and push directly to `main` -- Satisfies post-sweep verification and shortfall finalization.

**Acceptance Criteria:**
- Given every EventStore version advertised by `ValidateCanonicalBoundaryPackageCohort`, when production Tenants projects build in Release/package mode with `-warnaserror`, then every advertised version succeeds and incompatible older versions remain rejected.
- Given malformed historical `TenantState` (invalid status, blank/unknown/undefined member role, or blank configuration key), when `RefreshTenantAccess` executes, then it returns `TenantAccessStateMalformedRejection` without emitting `TenantAccessRefreshed` or leaking secrets, and audit/projection/API rejection handling remains consistent.
- Given the full solution build and individual test suites, all pass with zero failures before `DW-690` and `DW-710` are marked `done` in `deferred-work.md` and pushed to `main`.

## Implementation Notes

- **DW-690** (`96958cbdd7fd4b16518dee17c0f93344128d9217`): Added `tests/scripts/test_eventstore_cohort_compilation.py`. All six advertised EventStore cohorts (`5.3.0`, `5.3.1`, `5.4.0`, `5.5.1`, `5.5.2`, `5.6.0`) compiled all six real production Tenants projects (`Contracts`, `Server`, `Client`, `Testing`, `Aspire`, `Hexalith.Tenants`) in Release/package mode (`-warnaserror`, isolated `ArtifactsPath`) with exit code `0`, `0 Error(s)`, exact package-only `project.assets.json` resolution, and zero workspace `obj/` mutation. Incompatible older and non-canonical alias versions (`4.2.0`, `5.1.1`, `5.2.0`, `5.3.0.0`, `5.6.0+sha`) remain rejected on real production projects. `python3 -m unittest -v tests/scripts/test_eventstore_cohort_compilation.py tests/scripts/test_catalog_artifact_guards.py` passed 6/6.
- **DW-710** (`00c7b01923849501be72da7949db1f7b42a1601a`): Added `TenantAccessStateMalformedRejection` (`TenantId`, `Field`) in `Hexalith.Tenants.Contracts`, added fail-closed historical-state validation in `TenantAggregate.Handle(RefreshTenantAccess)` (status, null/blank/non-assignable members, null/blank configuration keys) and null-safe `IsAuthorized`/`GetActorRole`, added no-op `TenantState.Apply`, wired `TenantProjectionHandler.s_tenantAuditContracts`, documented the rejection in `docs/event-contract-reference.md`, and added Contracts/Server/Client/Integration tests. Release build succeeded with 0 errors; Contracts 861/861, Server 1956/1956, Client 124/124, Testing 199/199, UI 8151/8151, AppHost 80/80, Sample 43/43, and `CommandApiRuntimeIntegrationTests` 198/198 passed with 0 failures/skips.
- **Review hardening**: `bfe3f8fa9f0d223bea333e82ffa4cce735e58503` (DW-710: `Field` allowlist in `IsCompleteAuditPayload`, `GetActorRole` reuse across five sibling handlers, null-collection/precedence/Quick Reference docs, aggregate replay/precedence/JSON serialization tests) and `25a549e628bd614f5f8282b3410ee544be8a556e` (DW-690: wired `test_eventstore_cohort_compilation.py` into `scripts/run-product-unit-checks.sh`, strict workspace isolation, ambient MSBuild/CLI env scrubbing, exact direct EventStore package checks, parsed McpCli guard, prerelease/newer/McpCli rejections).
- **Post-review full verification**: Release build of `Hexalith.Tenants.slnx` succeeded (`0` errors; 6 pre-existing Aspire `CS0618` warnings). Individual .NET test projects: Contracts `861/861`, Server `1963/1963`, Client `124/124`, Testing `199/199`, UI `8151/8151`, AppHost `80/80`, Sample `43/43` (total `11,421/11,421`, 0 failed, 0 skipped). CI in-process integration classes: `TenantsApiGeneratedControllerTests` `80/80`, `CommandApiRuntimeIntegrationTests` `198/198` (after 1 non-reproducing intermittent `NullReferenceException` in unrelated `AuthenticatedBootstrapCannotAssignDifferentTarget`), `TenantOwnerSourceGatewayTests` `27/27`, `ProjectionReplayIntegrityTests` `32/32`, `AuditStreamPersistenceTests` `119/119` (total `456/456`, 0 failed, 0 skipped; the 3 Aspire live-topology fixtures require live Dapr + `prepare-source-catalog-profile.py` host identity provisioning). Node browser unit suites: `147/147`. Python product suites (`scripts/tests` + all 9 `tests/scripts` suites including final `test_eventstore_cohort_compilation.py`): `134/134`.
- **Sibling group shortfall**: 2 of 10 target items implemented (`DW-690`, `DW-710`); 8 siblings skipped (`DW-668`, `DW-686`, `DW-691`, `DW-705`, `DW-672`, `DW-715` already resolved; `DW-677` blocked on `DW-676`/hosted governance; `DW-678` obsolete/unrecoverable missing `/tmp/tenants-net11-repair-ng54_u4z/`). Open items remaining in `deferred-work.md`: `26`.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Route and evidence |
| --- | --- | --- |
| E1 / B1: `TenantProjectionHandler.IsCompleteAuditPayload` does not allowlist `TenantAccessStateMalformedRejection.Field` | medium | Patch. Verified at `TenantProjectionHandler.cs:386-391`: only non-whitespace string is checked, allowing arbitrary `Field` values to certify `ProjectionVersion`. Add explicit allowlist (`status`, `members`, `configurationKeys`) and negative tests. |
| E2 / B2: Five other `TenantAggregate` handlers dereference `state.Users.TryGetValue` directly despite null-safe `IsAuthorized` | medium | Patch. Verified at `TenantAggregate.cs:139,161,183,219,240`: `IsAuthorized` returns `false` on `Users == null`, then inline `state.Users.TryGetValue` throws `NullReferenceException`. Reuse `GetActorRole(state, envelope.UserId)`. |
| E3 / B7: Workspace isolation check in `test_eventstore_cohort_compilation.py` is a no-op on clean checkouts and absent from rejection test | medium | Patch. Verified at `test_eventstore_cohort_compilation.py:164-171,173-220`: missing pre-existing `project.assets.json` skips post-build assertion. Assert `not assets_file.exists()` when absent beforehand and run check in both tests. |
| E4 / B8 / E6: Ambient MSBuild/CLI env vars and localized/terminal-logger output can bypass cohort guard or break `'0 Error(s)'` check | medium | Patch. Verified against `Directory.Build.props:81` and `Directory.Build.targets:51`: scrub `Hexalith*FromSource`, `HexalithCatalogEventStoreVersion`, `HexalithMcpCliVersion`, `HexalithUsePublishedSdkForHostedTests`, `MSBUILDTERMINALLOGGER`, pin `DOTNET_CLI_UI_LANGUAGE=en-US`, and pass `-tl:off` plus explicit `FromSource=false` properties. |
| E5 / B9: Positional `errors[0]` parsing, unchecked split delimiter, hardcoded McpCli `3.0.0`, and missing prerelease/newer/McpCli rejections | medium | Patch. Verified at `test_eventstore_cohort_compilation.py:54-63,156-163,173-220`: select EventStore and McpCli `<Error>` elements by condition, verify delimiter, assert parsed McpCli version, and add prerelease/newer EventStore and invalid McpCli rejection checks. |
| B10: Transitive `Hexalith.EventStore.` prefix check misses root package and per-project direct package verification | medium | Patch. Verified at `test_eventstore_cohort_compilation.py:134-155`: match exact `Hexalith.EventStore` or `Hexalith.EventStore.*` prefix, assert expected direct EventStore packages per production project, and note non-packable `Api`/`UI` exclusion. |
| V1 / B6: `test_eventstore_cohort_compilation.py` is not invoked by `scripts/run-product-unit-checks.sh` or any CI workflow | high | Patch. Pre-verified by Verification Gap Reviewer: add `test_eventstore_cohort_compilation.py` to `scripts/run-product-unit-checks.sh` so normal product unit checks run it automatically. |
| B3: `docs/event-contract-reference.md` omits `null` collections, fault precedence, and `RefreshTenantAccess` in Quick Reference | low | Patch. Verified at `docs/event-contract-reference.md:69-73,927-944`: document null-collection rejection, entries-budget precedence, and add `RefreshTenantAccess` row to Quick Reference. |
| B4: `TenantAccessRecoveryTests.cs` lacks aggregate replay via `ProcessAsync`, `ActorRole == null`, non-null malformed unprivileged caller, and budget-precedence cases | medium | Patch. Verified at `TenantAccessRecoveryTests.cs:228-346`: add aggregate replay, `ActorRole == null`, `Unknown`-role/unprivileged non-null caller, and entries-limit precedence assertions. |
| B5: `EventSerializationTests.cs` generates `"test-Field"` and checks CLR properties without serialized JSON keys | low | Patch. Verified at `EventSerializationTests.cs:89-130`: map `"Field" => "status"` in `GetTestValue` and assert exact serialized JSON property names (`Field`, `TenantId`). |

## Verification

**Commands:**
- `python3 -m unittest tests/scripts/test_eventstore_cohort_compilation.py tests/scripts/test_catalog_artifact_guards.py` -- expected: all real-cohort compilation and guard tests pass.
- `dotnet build Hexalith.Tenants.slnx --configuration Release` -- expected: zero warnings, zero errors.
- Individual test projects under `tests/*/*.csproj` + Python script suites under `tests/scripts/` -- expected: all pass with zero failures.
- `npx commitlint --edit <temp-commit-msg-file>` -- expected: exit code 0 for every commit message.
