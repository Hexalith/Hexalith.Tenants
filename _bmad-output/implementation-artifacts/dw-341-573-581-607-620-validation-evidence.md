# Partial-release recovery closure evidence — 2026-10-22

Closes **DW-341, DW-573, DW-581, DW-607 and DW-620** against the clean Tenants entry revision `7cfa1c38847f7f4340b1cc11a9137f28430bd6c6`. The user authorized committing and pushing the completed work on main. The bmad-build one-shot route performed investigation, implementation, independent review and final verification.

## Result

Recovery uses one validated, hash-bound manifest through preflight, packing, package validation, publication and final verification. The packer's existing project-path validation also runs during preflight, without building or touching artifacts. Invalid/duplicate inventories and option-like, outside, nonexistent or non-project paths reject before remote reads. Manifest changes after validation, including changes during asset downloads or the final main lookup, reject before publication.

Eligibility requires an existing package, container, matching source tag or consistent release. An entirely absent release fails without replacing previous recovery state or emitting credential output. Existing package/container evidence without a tag remains supported through full publication and verification.

Existing GitHub Releases are checked for exact tag, non-draft status and expected prerelease classification immediately before repair. Their complete asset metadata is validated before downloads. Assets are downloaded through the authenticated GitHub asset-ID API, without following asset-provided URLs, then SHA-256 compared with the reviewed local package bytes. Duplicate IDs/names, unknown names, missing downloads and conflicting content reject before any publication. Only missing manifest-listed assets upload, without replacement or `--clobber`; final verification checks the complete exact inventory and all bytes again.

The successful validation step emits `needs-nuget` from its missing-package inventory. Both the account guard and NuGet login require an explicit true value. Missing-account and whitespace-account package guards remain blocking. Existing-release asset repair, absent-release repair and container-only repair run without a NuGet key when all packages already exist.

## Baseline and build

The existing Aspire topology was inspected with the local Aspire skill:

```bash
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json | jq '[.resources[] | {name, state, healthStatus}]'
```

All 23 resources were Running/Healthy. No topology restart, resource modification or deployment was needed.

The first source-build command omitted explicit serialization:

```bash
dotnet build tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -p:NuGetAudit=false
```

It failed with MSB4018 in `GenerateDepsFile` for the shared `Hexalith.McpCli.Abstractions` output. The serialized retry and final reviewed build passed:

```bash
dotnet build tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Debug -m:1 -p:UseHexalithProjectReferences=true -p:NuGetAudit=false
```

Final reviewed build: **zero warnings/errors**, 24.44 seconds. Local builds use Debug project references; recovery's existing Release artifact-production commands remain intact. No dependency update was needed.

## Tests and behavior controls

```bash
dotnet test --project tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Debug --no-build --no-restore -p:UseHexalithProjectReferences=true

python3 tests/scripts/test_partial_release_recovery.py

python3 _bmad-output/implementation-artifacts/dw-341-573-581-607-620-mutation-controls.py
```

Final complete Contracts suite: **659/659 passed, zero failures/skips**, 26.970 seconds. This mandatory test lane includes the **18-case offline recovery pipeline**, parsed workflow credential-routing assertions and executed populated/empty/whitespace account guards. Existing publication/main-advancement/hash-refusal and prerelease-classification matrices remain green. Focused tests also qualify download-triggered inventory mutation, metadata changes after validation, exact overridden final NuGet requests, missing-asset-only repair, complete-asset no-op and full no-tag recovery.

Five isolated behavior controls copy production scripts into temporary fixture repositories; production source is never changed. Removing eligibility, manifest binding, asset hashing or release metadata qualification, or restoring default-inventory verification, causes the corresponding test to reject. All five final controls report rejected, with zero unexpected test errors. The metadata control rejects all three draft/tag/prerelease subcases.

An initial default-verifier control was accepted because the first fixture retained canonical IDs. Distinct fixture IDs alone still exposed a second gap: the canonical manifest was absent and Bash could skip a failed command-substitution loop. The final fixture supplies both manifests and asserts the exact five overridden NuGet URLs plus container URL; the same control now rejects explicitly. This failed control and its repair are retained rather than reported as an initial pass.

## Independent review

Blind Hunter reported six findings: release metadata qualification, an insufficient override fixture, the last manifest-check timing, inconsistent project preflight, incomplete no-tag end-to-end tests and credential-free existing-release repair evidence. All six were patched and triaged in the owning spec. Final independent recheck confirms all addressed and **no remaining concrete findings**, with nothing deferred. The reviewer independently proved final-main manifest mutation rejection and rejection of the canonical-loop regression.

## Ledger and tooling

```bash
python3 _bmad-output/implementation-artifacts/dw-341-573-581-607-620-verify.py

actionlint .github/workflows/recover-partial-release.yml

bash -n scripts/partial-release-recovery-common.sh scripts/validate-partial-release-recovery.sh scripts/publish-partial-release.sh scripts/verify-partial-release.sh

dotnet format whitespace tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --no-restore --include tests/Hexalith.Tenants.Contracts.Tests/PartialReleaseRecoveryWorkflowTests.cs --verify-no-changes --verbosity quiet

node_modules/.bin/commitlint --edit /tmp/tenants-dw-recovery-commit-message.txt --verbose

git -c core.safecrlf=false diff --check
```

The changed-story CI wrapper was also attempted locally as `python3 scripts/validate-changed-story-gitlinks.py`; it requires a GitHub event and returned `GITHUB_EVENT_PATH or --event-path is required.` The direct local equivalent passes: `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-341-573-581-607-620-partial-release-recovery.md --ref HEAD`. Its full canonical baseline is recorded in the spec; no dependency pointers moved. The committed event check can then use the exact before/after revisions.

The checks listed above pass. The exact candidate message was validated by lockfile-pinned commitlint **21.2.3**, with zero problems/warnings; its successful output is preserved in the [run manifest](dw-341-573-581-607-620-run-manifest.json). Installing pinned tools used `npm ci --ignore-scripts --no-audit --no-fund`; package manifests and locks are unchanged.

The [ledger verifier](dw-341-573-581-607-620-verify.py) preserves all **657 IDs**, all **652 unrelated record bodies**, the ledger preamble and selected historical fields under `pre-partial-recovery-`. Exactly five index rows are removed, leaving **500 done / 157 open across 147 rows**, each open record indexed exactly once. Duplicate-ID and immutable-field controls reject. [Structured ledger results](dw-341-573-581-607-620-ledger-validation.json) and source hashes are committed with the evidence.

No live NuGet/OIDC exchange, registry publication or protected recovery workflow was executed. These are isolated destination tests plus real script/packer execution, not proof of production publication. Dependency pointers, archive/sprint tracking and unrelated release/runtime/provenance holds remain preserved. Fetching origin showed no divergence before the final commit.
