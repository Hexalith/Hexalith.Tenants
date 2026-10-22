# DW-552/DW-553/DW-557/DW-561/DW-566 validation

Date: 2026-10-22. Owning repository: Hexalith.Tenants, main. Baseline: `1e60af449c1fdc3f9801cf57e8847e00faab2394`.

Recovery now builds, packs, validates and compares existing package hashes before NuGet login. Preparation requires validated state, records its hash before work, refuses changes during work, and binds prepared package bytes plus source/version/manifest/inventories/destination context. Publication checks this evidence before client use and again immediately before publication, without building or packing packages. Missing package credentials fail before any client call. Inventories with no missing packages keep their supported no-key path.

All external action invocations in both release workflows use immutable commit identities. The six newly pinned actions retain their previously declared versions. [Upstream resolution evidence](dw-552-553-557-561-566-action-pins.json) records exact Git tag refs, commit identities, primary action manifest URLs and manifest SHA-256 values. Parsed workflow tests require those exact pins and version comments. This records immutable source selection, not a claim that every transitive action implementation is audited.

## Commands and results

- `dotnet build tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Debug -m:1 -p:UseHexalithProjectReferences=true -p:NuGetAudit=false`: final source build succeeded, zero warnings/errors. This environment pin disables restore-time auditing for the local check; tracked audit/build policy is unchanged.
- `dotnet tests/Hexalith.Tenants.Contracts.Tests/bin/Debug/net10.0/Hexalith.Tenants.Contracts.Tests.dll -noLogo`: 687/687 passed; zero errors, failures, skipped or not-run cases (48.984 seconds).
- The Contracts suite executes `python3 tests/scripts/test_partial_release_recovery.py`. Independent review also ran the updated pipeline: 27/27 passed. It uses disposable stub destinations and the real validator, manifest packer, preparation, publisher and verifier. Package/consumer validator routing is checked; package formats have separate existing contract tests.
- `python3 _bmad-output/implementation-artifacts/dw-552-553-557-561-566-mutation-controls.py`: all four original behavioral controls passed and all four harmful variants rejected through assertions: omitted prepared proof, omitted final proof, rebuild with live key and omitted credential admission. [Replay script](dw-552-553-557-561-566-mutation-controls.py) changes only disposable copied scripts.
- Workflow tests execute both actual account guards for valid/unset/empty/whitespace accounts; parsed conditions reject disabled guard/login and empty output-binding mutations. Both actual normal-release `verifyReleaseCmd` and recovery publication run commands reject unset/empty/whitespace keys before any forbidden client. Preparation environment removal/change and continued-failure mutations reject.
- `actionlint .github/workflows/release.yml .github/workflows/recover-partial-release.yml`: passed.
- `bash -n scripts/prepare-partial-release.sh scripts/publish-partial-release.sh scripts/partial-release-recovery-common.sh`: passed.
- `dotnet format whitespace tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --no-restore --verify-no-changes --include tests/Hexalith.Tenants.Contracts.Tests/PartialReleasePublicationTests.cs tests/Hexalith.Tenants.Contracts.Tests/PartialReleaseRecoveryWorkflowTests.cs`: exit 0; formatter emitted a workspace-loading warning, no scoped formatting errors.
- `git diff --check`: passed. The C# worktree uses the baseline-required CRLF; Git reports its existing automatic LF index normalization.
- Exact full commit message was written to `/tmp/tenants-release-credentials-commit.txt` and checked using lockfile-installed `@commitlint/cli@21.2.3`: `node_modules/.bin/commitlint --edit /tmp/tenants-release-credentials-commit.txt --verbose`, exit 0, zero problems/warnings. Candidate: `fix(release): resolve five deferred credential records`.
- `git fetch origin main` and `git rev-list --left-right --count HEAD...origin/main`: fetched successfully, `0 0` before commit.

## Review and correction evidence

The configured Blind Hunter reviewed the actual changed files independently; no configured layer was skipped. All seven findings were patched and the independent recheck reports no remaining concrete defects and no deferrals:

1. Required state files and the container-state enum now reject missing/unknown evidence before build.
2. The pre-build state snapshot rejects inventory changes during preparation. A build fixture empties the missing-package list and must fail without accepted proof or publication.
3. Repository, registry and container-repository changes after preparation reject before a publication client runs.
4. Both normal and recovery missing-login outputs now execute their real shell guards, alongside the successful account and recovery publication controls.
5. Actual preparation environment bindings and default failure propagation have parsed mutation checks; actual preparation runs through the full offline fixture.
6. Build, pack and both validator failure cases revoke stale proof and refuse later publication. Independent probes deleted each of the seven state files during build; every case refused preparation and subsequent publication without changing destinations.
7. Action pin tests bind actual workflow values and retained comments to the upstream evidence, beyond checking SHA syntax.

The first new pipeline run had three fixture failures: the supposed existing-inventory mutation preserved its initial empty value, so publication correctly succeeded and affected later shared subcases. The fixture now starts from one existing package, resets call evidence and asserts real changes. Subsequent initial Contracts run passed 677/677. The final reviewed version adds normal-path and environment-mutation controls and the preparation-boundary corrections above; no failing result is concealed or described as a production reproduction.

## Ledger scope and practical limits

Exactly DW-552/DW-553/DW-557/DW-561/DW-566 close. [Ledger validation](dw-552-553-557-561-566-ledger-validation.json) proves all 657 IDs remain, the 652 unrelated record bodies are unchanged, and all 152 remaining open records have exact coverage across 142 maintenance rows. Status totals are 505 done/152 open. Earlier selected field values remain under `pre-release-credentials-`; historical findings, dependency pointers, archives and sprint tracking are preserved.

No real release, credential exchange, container publication or deployment ran. The existing Aspire app was already running and resource state was inspected before edits. These local checks do not prove a live Trusted Publishing exchange or GitHub runner behavior. DW-560 and DW-563 remain open: normal release still performs dependency/build work in the OIDC-enabled job, and broader normal/container build separation under publication credentials remains deferred. Existing runtime, provenance and release holds remain; this batch does not establish release readiness.
