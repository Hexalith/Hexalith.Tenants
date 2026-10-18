# DW-217/DW-219 validation evidence — 2026-10-18

Entry commit: `089b575a6c144902e7de871c4b21fd4fecfacb52`. Scope: align operator recovery guidance with the recorded authentic-tag-only policy and accurately explain registry/tag-floor failures. Validation logic, package inventory, dependency pins, floor values and protected source/approval gates are unchanged. No publication or tag mutation runs.

## Reproduction and acceptance

Existing real-Bash tests execute the actual publication-preflight wrapper and extracted workflow guard. The wrapper helper clears release inputs before execution, preventing destination probes. The workflow helper supplies isolated GitHub/registry fixtures; the added sparse-registry scenario verifies the exact read-only call sequence. The missing provenance-safe advice and guaranteed-collision claim reproduce as two failures against original production text.

- Debug source test-project build: exit **0**, zero warnings/errors.
- Original production text with updated assertions: exit **2**, **212 passed / 2 failed / 0 skipped**.
- Corrected focused governance suite: exit **0**, **214/214 passed**, zero failures/skips.
- Full owning Contracts suite: exit **0**, **333/333 passed**, zero failures/skips.

```bash
dotnet build tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Debug -m:1 -nr:false -p:UseHexalithProjectReferences=true > /tmp/dw-217-219/build.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Debug --no-build --no-restore --filter-method '*PackageGovernanceTests*' --minimum-expected-tests 100 --no-ansi --no-progress --results-directory /tmp/dw-217-219/red --report-xunit-trx --report-xunit-trx-filename focused.trx > /tmp/dw-217-219/red-tests.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Debug --no-build --no-restore --filter-method '*PackageGovernanceTests*' --minimum-expected-tests 214 --no-ansi --no-progress --results-directory /tmp/dw-217-219/green --report-xunit-trx --report-xunit-trx-filename focused.trx > /tmp/dw-217-219/green-tests.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 333 --no-ansi --no-progress --results-directory /tmp/dw-217-219/full --report-xunit-trx --report-xunit-trx-filename full.trx > /tmp/dw-217-219/full-tests.log 2>&1
actionlint .github/workflows/release.yml
bash -n scripts/validate-publication-preflight.sh
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-217-219-release-recovery-guidance.md
git diff --check
```

The lint, syntax, story-gitlink and whitespace-diff checks exit **0**. Existing boundary checks still prove that below-floor versions fail locally in verify/publish, while at/above-floor versions reach the original identity check; malformed versions retain their earlier format error. Workflow controls retain numeric/revision precedence, allowed equality, protected-source ordering, missing/malformed evidence and authentic-tag versus nonzero-revision recovery behavior. A sparse version index fails with lineage/risk language without asserting a specific proposal or collision.

## Runtime snapshot and limits

The required pre-code Aspire snapshot uses the Aspire skill. `aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json` exits **0** and starts the exact Tenants AppHost. The corresponding `aspire describe` exits **0**: security is unhealthy, domain resources waiting, and Memories resources healthy. `aspire stop` with the same exact AppHost exits **0** and cleans up only this task-owned instance. Logs are `/tmp/dw-217-219-aspire-start.json`, `/tmp/dw-217-219-aspire-describe.json` and `/tmp/dw-217-219-aspire-stop.log`. This snapshot is not distributed-runtime acceptance; isolated release guidance requires no browser session or healthy topology.

The authoritative policy comes from the completed `spec-release-version-floor-drift.md`, especially its approved iteration-three authentic-tag-only recovery decision, and the existing workflow recovery branches. The separately tracked stale release-line/trigger documentation remains DW-218. Existing release-readiness holds remain.

The contributor formatter check also ran:

```bash
dotnet format whitespace tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --no-restore --include tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs --verify-no-changes > /tmp/dw-217-219/format.log 2>&1
```

It exits **2** with **172 diagnostics on unchanged lines** and **zero diagnostics in added-line intervals**, checked against `git diff --unified=0`. Existing formatting is preserved; this file-wide baseline blocker remains separate from the passing build, all 333 tests and diff checks.

## Independent review and correction checks

Blind Hunter returned four findings; all are triaged in the spec. Complete conditional policy/fallback sentences replace fragment-only assertions, contributor checks normalize whitespace, and the recovery paragraph now names the evidence required and the repository-maintainer issue destination. The spec links this evidence artifact. Independent recheck confirms all four findings resolved with no residual concern.

The review Debug source build exits **0**, zero warnings/errors:

```bash
dotnet build tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Debug -m:1 -nr:false -p:UseHexalithProjectReferences=true > /tmp/dw-217-219/review-build.log 2>&1
```

Three temporary mutation probes each execute the following focused command and exit **2**, exactly one failed test and zero skips. Original file bytes are restored after each probe, including on exceptions.

```bash
dotnet test --project tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Debug --no-build --no-restore --filter-method '*PackageGovernanceTests.Publication_preflight_rejects_release_versions_below_the_published_floor' --minimum-expected-tests 1 --no-ansi --no-progress
```

| Probe | Temporary change | Captured output |
| --- | --- | --- |
| Preflight policy reversal | In the script, replace `only when it is the authentic tag` with `even when it is not the authentic tag` | `/tmp/dw-217-219/script-policy-reversal.log` |
| Contributor policy reversal | Make the same replacement in `CONTRIBUTING.md` | `/tmp/dw-217-219/docs-policy-reversal.log` |
| Reflowed obsolete advice | Append the single-line sentence `Restore the deleted tags until semantic-release resumes above the floor.` to `CONTRIBUTING.md` | `/tmp/dw-217-219/reflowed-obsolete-advice.log` |

`python3 /tmp/dw-217-219/check-mutations.py` orchestrates those probes and exits **0** after verifying all three expected failures and restoring bytes. The probes prove the strengthened checks reject the reviewer's concrete bypasses; they do not simulate a publication or authorize tag restoration.

## Final acceptance and maintenance accounting

The restored final tree passes the owning Contracts suite again: exit **0**, **333/333 passed**, zero failures/skips, including the 214 governance cases and strengthened review assertions.

```bash
dotnet test --project tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 333 --no-ansi --no-progress --results-directory /tmp/dw-217-219/final-full --report-xunit-trx --report-xunit-trx-filename full.trx > /tmp/dw-217-219/final-full-tests.log 2>&1
python3 /tmp/dw-217-219/check-ledger.py > /tmp/dw-217-219/ledger-check.log 2>&1
```

The accounting check exits **0**. It compares every ledger section against entry commit `089b575a6c144902e7de871c4b21fd4fecfacb52`, verifies that only DW-217/DW-219 bodies changed and all replaced original fields remain under their `pre-dw-*` names, then verifies disposition totals and exactly-once coverage of every open ID by the maintenance table. All **653 IDs** remain: **367 done / 286 open**, covered by **258 remaining index rows**. DW-218 and every unrelated ledger body remain byte-for-byte unchanged. Archive, sprint tracking, package inventory, dependency pointers and release-readiness holds remain preserved. This correction does not establish release readiness.

Pinned and installed commitlint both equal **21.2.3**. The exact candidate `fix(ci): align release recovery guidance with authentic lineage` validates with exit **0**, zero problems/warnings:

```bash
node_modules/.bin/commitlint --edit /tmp/dw-217-219/commit-message.txt --verbose > /tmp/dw-217-219/commitlint.log 2>&1
```

The invoked build workflow requires a local commit; no push, hosted release, tag mutation or publication is performed.

After review corrections, the same scoped formatter command was rerun with output `/tmp/dw-217-219/final-format.log`: exit **2**, again **172 unchanged-line diagnostics and zero on added lines**. The final build and all tests pass independently; existing file-wide formatting remains a reported baseline blocker.
