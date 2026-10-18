# DW-218 validation evidence — 2026-10-18

Entry revision: `b39f1716177788b8e068711f9674560d51a94e0e`. Scope: correct stale release trigger, version-line and generated-notes guidance. The current workflow and configuration are the authority; this task does not publish or establish release readiness.

## Evidence and correction

- `.github/workflows/release.yml` declares only `workflow_dispatch`. Its unprotected `verify-source` job proves the current main tip has successful exact-SHA push CI and checks the registry/tag floor. The protected production job rechecks source before publication; `verify-publication` checks the release tag at the dispatched source. No release test tiers run.
- `.releaserc.json` has the release-notes generator and GitHub publisher, with no changelog/git plugins. `CHANGELOG.md` remains historical, through `3.2.18`; generated notes belong to GitHub Releases.
- Local tag `v5.8.0` resolves to `032573384d3df4bc7a5bc0e69945ecca4e00967f` (2026-10-08), an ancestor of the entry revision. Contributor and agent guidance date this repository snapshot explicitly and do not infer current registry contents, a next version or readiness from it. The historical 3.x/4.0 recovery and consumer comparisons remain labeled history.
- The shell preflight's executable lines and `minimum_release_version=4.0.0` remain unchanged. Its corrected comment separates the static minimum from reachable release history. Existing authentic-producing-commit/reachability recovery advice is retained.

Read-only source checks:

```bash
git rev-parse 'v5.8.0^{commit}'
git merge-base --is-ancestor 'v5.8.0^{commit}' b39f1716177788b8e068711f9674560d51a94e0e
git show --format=fuller --no-patch 6cc9eb3a44f45417aac76d7def9daba7544cd2fa
python3 /tmp/dw-218/check-guidance.py
```

All exit **0**. The last check normalizes contributor/context whitespace, checks manual dispatch, date-bound canonical tag evidence, static-minimum language, historical labels and absent changelog/git plugins. It compares every nonblank, noncomment shell line with entry source and verifies byte-for-byte preservation of release workflow/configuration, changelog, package manifest, release-readiness report, archive and sprint status. It resolves and checks the tag's ancestry independently. Local tag evidence is not a registry publication attestation.

## Tests and checks

The first full existing Contracts run after the documentation edit exits **2**: **332 passed / 1 failed / 0 skipped**. `Publication_preflight_rejects_release_versions_below_the_published_floor` requires the obsolete contributor sentence “Nothing at or below 4.0.0 can be released again.” Updating this existing assertion to require the unchanged static minimum and independent publishability checks corrects the fixture; all actual shell boundary/provenance controls remain. No new test is added.

The Debug source test-project build exits **0**, **zero warnings/errors**. The corrected full owning Contracts suite exits **0**, **333 passed / 0 failed / 0 skipped**.

```bash
dotnet test --project tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 333 --no-ansi --no-progress --results-directory /tmp/dw-218/contracts --report-xunit-trx --report-xunit-trx-filename contracts.trx > /tmp/dw-218/contracts.log 2>&1
dotnet build tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Debug -m:1 -nr:false -p:UseHexalithProjectReferences=true > /tmp/dw-218/build.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 333 --no-ansi --no-progress --results-directory /tmp/dw-218/final-contracts --report-xunit-trx --report-xunit-trx-filename contracts.trx > /tmp/dw-218/final-contracts.log 2>&1
git diff --check
bash -n scripts/validate-publication-preflight.sh
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-218-release-trigger-and-line-guidance.md
```

The whitespace and Bash syntax checks exit **0**. The first story-gitlink check refuses missing `baseline_commit` in the generated spec; after recording the canonical entry revision, it exits **0**, proving no reference pointer changes in this story range and working tree.

## Required runtime snapshot

Before the C# assertion correction, the Aspire skill was used for the required pre-code snapshot. `aspire ps --format Json` showed an unrelated Platform AppHost, so Tenants was started with isolation. Start, describe, telemetry capture and stop all exit **0**. The snapshot shows unhealthy security, waiting domain services and healthy Memories resources; this is not healthy-topology or distributed-runtime acceptance. Only the task-owned Tenants instance was stopped; the unrelated host was preserved.

```bash
aspire ps --format Json
aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --isolated --format Json > /tmp/dw-218/aspire-start.json 2>&1
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json > /tmp/dw-218/aspire-describe.json 2>&1
aspire otel logs --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json > /tmp/dw-218/aspire-otel-logs.json 2>&1
aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj > /tmp/dw-218/aspire-stop.log 2>&1
```

## Review and completion

Blind Hunter computed `min(floor(sqrt(11.874) + 1), 10) = 4` and returned four findings. All were patched and independently rechecked with no residual concern:

- Added the workflow's stale-source and no-releasable-commit operator outcomes.
- Narrowed the new preflight comment to ancestry/numeric coverage, explicitly excluding producing-commit proof.
- Reduced the existing fixture's coupling to complete prose sentences; it checks the floor token/static-minimum role and retains all actual shell boundary and provenance controls.
- Corrected the agent-owned predecessor shorthand in append-only notes to enumerate DW-215/DW-216/DW-217/DW-219.

The final Debug source build again exits **0**, zero warnings/errors. All **333 Contracts tests pass**, zero failures/skips, after review corrections:

```bash
dotnet build tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Debug -m:1 -nr:false -p:UseHexalithProjectReferences=true > /tmp/dw-218/review-build.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 333 --no-ansi --no-progress --results-directory /tmp/dw-218/review-contracts --report-xunit-trx --report-xunit-trx-filename contracts.trx > /tmp/dw-218/review-contracts.log 2>&1
```

No review finding is deferred. Scoped formatter verification ran before and after review corrections:

```bash
dotnet format whitespace tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --no-restore --include tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs --verify-no-changes > /tmp/dw-218/format.log 2>&1
dotnet format whitespace tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --no-restore --include tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs --verify-no-changes > /tmp/dw-218/review-format.log 2>&1
```

Both exit **2** with **172 diagnostics on unchanged lines and zero on added lines**, attributed against the corresponding `git diff --unified=0` intervals. Existing file-wide formatting is preserved as a separate baseline blocker; build, tests and whitespace-diff checks pass.

Maintenance validation exits **0**:

```bash
python3 /tmp/dw-218/check-ledger.py
python3 /tmp/dw-218/check-guidance.py
```

The accounting check compares all 653 ledger sections against the canonical entry revision, proves only DW-218 changed, and checks every replaced original field remains under `pre-dw-218-*`. It proves the only removed action row is DW-218, all other rows remain identical and in order, and every open ID appears exactly once. Current totals: **368 done / 285 open**, **257 maintenance rows**; dispositions are 198 actionable, 15 blocked, 26 awaiting-decision, nine accepted limitations and 37 uncertain. Archive, sprint, package inventory, dependencies, original findings and release-readiness holds remain preserved. This closes only DW-218 and does not qualify a release.

Pinned and installed commitlint both equal **21.2.3**. The exact candidate `docs(release): correct deferred trigger and version-line guidance` validates with exit **0**, zero problems/warnings:

```bash
node_modules/.bin/commitlint --edit /tmp/dw-218/commit-message.txt --verbose > /tmp/dw-218/commitlint.log 2>&1
```

The invoked build workflow requires a local commit. No push, hosted release, publication or tag mutation is performed.
