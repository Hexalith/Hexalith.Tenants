# DW-621 validation evidence — 2026-10-18

Entry revision: `2994c279bc1c291c69e3d11f6bb6290c5504a13f`. Scope: classify newly created recovery GitHub Releases as prereleases for versions with a prerelease suffix and verify final classification metadata. Every publication tool in the executable fixture is intercepted locally; no package/container publication, GitHub Release creation or remote edit is performed.

## Behavior and reproduction

`validate-partial-release-recovery.sh` already admits three decimal components and an optional hyphen suffix; it does not admit build metadata. At the publisher's existing missing-release boundary, an initialized optional Bash argument array now adds `--prerelease` for a suffix-bearing version. Stable creation preserves its previous arguments, including the exact tag, target source, title, generated notes and five package assets. Existing releases follow the same no-edit path. The flag's behavior is documented in the [official GitHub CLI manual](https://cli.github.com/manual/gh_release_create).

The existing `PartialReleasePublicationTests` executable fixture now takes the reviewed version. Expected classification is explicit theory data, separate from the production predicate. Nine new cases cover stable creation, `alpha`, `rc.1`, `preview-build.7`, numeric `0`, mixed/all-missing inventories and existing-release package/container-only recovery. Exact external command arguments, source-read order, package identities and retained recovery-state filenames/text contents remain asserted.

The Debug source build exits 0 with **zero warnings/errors**. Running the new tests against unchanged production exits 1: **6 failed / 49 passed / 0 skipped**. All six failures are prerelease creations missing the required flag; stable creation, existing-release cases and earlier refusal controls pass. After the narrow publisher repair, focused execution exits 0: **55 passed / 0 failed / 0 skipped**. The maintained full Contracts lane exits 0: **406 passed / 0 failed / 0 skipped**. Scoped formatter validation exits 0.

```bash
mkdir -p /tmp/dw-621
dotnet build tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Debug -m:1 -nr:false -p:UseHexalithProjectReferences=true > /tmp/dw-621/baseline-build.log 2>&1
dotnet tests/Hexalith.Tenants.Contracts.Tests/bin/Debug/net10.0/Hexalith.Tenants.Contracts.Tests.dll -class 'Hexalith.Tenants.Contracts.Tests.PartialReleasePublicationTests' -noColor -result-xml /tmp/dw-621/baseline-focused.xml > /tmp/dw-621/baseline-focused.log 2>&1
dotnet tests/Hexalith.Tenants.Contracts.Tests/bin/Debug/net10.0/Hexalith.Tenants.Contracts.Tests.dll -class 'Hexalith.Tenants.Contracts.Tests.PartialReleasePublicationTests' -noColor -result-xml /tmp/dw-621/focused.xml > /tmp/dw-621/focused.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 406 --no-ansi --no-progress --results-directory /tmp/dw-621/contracts --report-xunit-trx --report-xunit-trx-filename contracts.trx > /tmp/dw-621/contracts.log 2>&1
dotnet format whitespace tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --no-restore --include tests/Hexalith.Tenants.Contracts.Tests/PartialReleasePublicationTests.cs --verify-no-changes > /tmp/dw-621/format.log 2>&1
```

The original-production command above was run before changing the publisher. Running that command in the completed worktree exercises corrected production. The executable baseline replay below instead selects scripts from the full entry revision without overwriting the worktree or relying on an environment override that the test helper replaces.

## Independent review corrections and retained results

Review identified four improvements, all patched: verify final GitHub metadata, compare individual release argument boundaries, provide executable original-production replay and retain compact failure detail. The final verifier requires the JSON Boolean `prerelease` to match the reviewed version after its existing tag/draft checks. Thirteen new executable cases use real `jq` and intercepted network commands: five valid stable/varied prerelease responses pass; wrong Boolean, absent, null and string metadata are refused for both stable and prerelease versions. The original verifier accepts all eight invalid cases (**8 failed / 5 passed / 0 skipped**). Corrected combined focused execution passes **68/68**, zero failures/skips. The review Debug source build has **zero warnings/errors**.

The six original publisher failures are the release-only versions `1.2.3-alpha`, `1.2.3-rc.1`, `1.2.3-preview-build.7`, `1.2.3-0`, plus mixed and all-missing inventories at `1.2.3-rc.1`. The eight original verifier failures use each of `1.2.3` and `1.2.3-rc.1` with a wrong Boolean, missing property, JSON null or string-encoded expected Boolean. Both original-production failure sets are reproduced by the tracked runner, which copies the compiled test output, marker solution, manifest and selected scripts into an isolated temporary repository. No tests or production files in the worktree are rewritten. Replay and copied-script mutation checks exit 0 with these observed summaries:

| Selected copied script | Passed | Failed | Skipped | Test process exit |
| --- | ---: | ---: | ---: | ---: |
| Corrected publisher | 55 | 0 | 0 | 0 |
| Entry-revision publisher | 49 | 6 | 0 | 1 |
| Always request prerelease | 51 | 4 | 0 | 1 |
| Collapse all asset paths into one argument | 45 | 10 | 0 | 1 |
| Corrected verifier | 13 | 0 | 0 | 0 |
| Entry-revision verifier | 5 | 8 | 0 | 1 |
| Invert expected classification | 6 | 7 | 0 | 1 |

After building the tests, these commands reproduce the controls and review checks:

```bash
mkdir -p /tmp/dw-621
dotnet build tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Debug -m:1 -nr:false -p:UseHexalithProjectReferences=true > /tmp/dw-621/review-build.log 2>&1
python3 _bmad-output/implementation-artifacts/dw-621-check-variants.py > /tmp/dw-621/replayed-variants.log 2>&1
dotnet tests/Hexalith.Tenants.Contracts.Tests/bin/Debug/net10.0/Hexalith.Tenants.Contracts.Tests.dll -class 'Hexalith.Tenants.Contracts.Tests.PartialReleaseVerificationTests' -class 'Hexalith.Tenants.Contracts.Tests.PartialReleasePublicationTests' -noColor -result-xml /tmp/dw-621/review-focused.xml > /tmp/dw-621/review-focused.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 419 --no-ansi --no-progress --results-directory /tmp/dw-621/contracts-final --report-xunit-trx --report-xunit-trx-filename contracts.trx > /tmp/dw-621/contracts-final.log 2>&1
dotnet format whitespace tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --no-restore --include tests/Hexalith.Tenants.Contracts.Tests/PartialReleasePublicationTests.cs tests/Hexalith.Tenants.Contracts.Tests/PartialReleaseVerificationTests.cs --verify-no-changes > /tmp/dw-621/format-final.log 2>&1
```

The initial verifier regression ran the same direct class command with only `PartialReleaseVerificationTests`, saving `/tmp/dw-621/baseline-verification.xml` before patching the verifier; the tracked variant runner reproduces its original-production result from the completed worktree. Final maintained Contracts acceptance passes **419/419**, zero failures/skips, and scoped formatting passes. The runner asserts exact failure totals and exits if any control unexpectedly changes its outcome.

Independent recheck executed the tracked runner and resolved the four original findings. It then demonstrated a Windows portability issue: platform-default CRLF in the expected argument file survives Bash `mapfile -t`. The fixture now writes explicit LF separators. A further independent recheck confirms that correction and reports no remaining issue. After this final edit, the Debug source build exits 0 with **zero warnings/errors**, focused execution passes **68/68**, maintained Contracts passes **419/419**, both with **zero failures/skips**, and scoped formatting passes. Windows suite execution is not claimed; the portability finding was demonstrated with CRLF input to Bash and corrected with explicit platform-independent delimiters.

```bash
dotnet build tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Debug -m:1 -nr:false -p:UseHexalithProjectReferences=true > /tmp/dw-621/final-build.log 2>&1
dotnet tests/Hexalith.Tenants.Contracts.Tests/bin/Debug/net10.0/Hexalith.Tenants.Contracts.Tests.dll -class 'Hexalith.Tenants.Contracts.Tests.PartialReleaseVerificationTests' -class 'Hexalith.Tenants.Contracts.Tests.PartialReleasePublicationTests' -noColor -result-xml /tmp/dw-621/accepted-focused.xml > /tmp/dw-621/accepted-focused.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 419 --no-ansi --no-progress --results-directory /tmp/dw-621/contracts-accepted --report-xunit-trx --report-xunit-trx-filename contracts.trx > /tmp/dw-621/contracts-accepted.log 2>&1
dotnet format whitespace tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --no-restore --include tests/Hexalith.Tenants.Contracts.Tests/PartialReleasePublicationTests.cs tests/Hexalith.Tenants.Contracts.Tests/PartialReleaseVerificationTests.cs --verify-no-changes > /tmp/dw-621/format-accepted.log 2>&1
```

## Required environment snapshot

Discovery found an unrelated running FrontComposer AppHost and left it running. The isolated Tenants start, resource/telemetry capture and exact-AppHost stop all exit 0. Memories resources were healthy; security was starting and dependent services were waiting. This records the pre-edit environment, not healthy-topology or release qualification.

```bash
aspire ps --format Json
aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --isolated --format Json > /tmp/dw-621/aspire-start.json 2>&1
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json > /tmp/dw-621/aspire-describe.json 2>&1
aspire otel logs --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json > /tmp/dw-621/aspire-otel-logs.json 2>&1
aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --non-interactive > /tmp/dw-621/aspire-stop.log 2>&1
```

## Scope and limits

Shell syntax, whitespace, story-gitlink validation and the direct baseline pointer comparison pass. The existing release/source/artifact guards remain covered by the publisher class. The actual verifier consumes controlled release JSON in the isolated tests; no live remote metadata is read back. Existing releases are not edited, and incorrect final classification is refused. Existing-release asset/provenance work and every unrelated release qualification hold remain separately scoped.

```bash
bash -n scripts/publish-partial-release.sh scripts/verify-partial-release.sh
git diff --check
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-621-recovery-prerelease-classification.md > /tmp/dw-621/story-gitlinks.log 2>&1
git diff --exit-code 2994c279bc1c291c69e3d11f6bb6290c5504a13f -- references/
```

Only DW-621 closes. `python3 /tmp/dw-621/validate-closure.py > /tmp/dw-621/closure.json` exits 0, comparing every original record with the full entry revision: **653 IDs, 371 done / 282 open**, including **195 actionable**, across **254 index rows** with exact open-ID coverage. Every unrelated record and index row is unchanged; all original DW-621 fields remain under `pre-dw-621-*` where replaced. Archive, sprint tracking and dependency pointers remain unchanged. The direct story-gitlink and baseline pointer checks above are reproducible without the temporary closure checker.

Pinned commitlint **21.2.3** validates the exact local commit candidate with zero problems/warnings before use. Evidence is retained in `/tmp/dw-621/commitlint.log`:

```bash
node node_modules/@commitlint/cli/cli.js --edit /tmp/dw-621/commit-message.txt --verbose > /tmp/dw-621/commitlint.log 2>&1
```

Independent review and correction rechecks are complete. No finding is deferred, and no release readiness is claimed.
