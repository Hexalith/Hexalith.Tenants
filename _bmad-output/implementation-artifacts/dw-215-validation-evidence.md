# DW-215 validation evidence — 2026-10-18

Entry commit: `5efab750d471064c667e6d9e36cb222fa823bda5`. Scope: the existing plain `vMAJOR.MINOR.PATCH` tag selector in the unprotected release-floor guard. No publication or hosted release was executed.

## Reproduction and checks

Before changing the workflow, 36 new cases exercised the actual extracted Bash through the existing GitHub/registry fixture. The initial Debug source build succeeded with zero warnings/errors. The test run exited **2**: **24 failed, 12 passed, zero skipped**. All 15 invalid-only cases and nine mixed-fallback invocation checks failed; the latter showed invalid candidates reaching the comparison API. Six lower-valued invalid minor/patch candidates sorted below the valid fallback in mixed cases; their invalid-only controls still failed. This is local guard reproduction, not an assertion that a production release collided.

```bash
dotnet build tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Debug -m:1 -nr:false -p:UseHexalithProjectReferences=true > /tmp/dw-215-initial-build.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Debug --no-build --no-restore --filter-method '*PackageGovernanceTests.ReleaseWorkflow*' --minimum-expected-tests 36 --no-ansi --no-progress --results-directory /tmp/dw-215-red --report-xunit-trx --report-xunit-trx-filename focused.trx > /tmp/dw-215-red-tests.log 2>&1
```

The corrected selector validates canonical decimal strings and the inclusive `9007199254740991` bound before numeric conversion. Focused tests then pass **36/36**, and full Contracts passes **190/190**, zero failures/skips, with exit **0** for each command:

```bash
dotnet test --project tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Debug --no-build --no-restore --filter-method '*PackageGovernanceTests.ReleaseWorkflow*' --minimum-expected-tests 36 --no-ansi --no-progress --results-directory /tmp/dw-215-green --report-xunit-trx --report-xunit-trx-filename focused.trx > /tmp/dw-215-green-tests.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 36 --no-ansi --no-progress --results-directory /tmp/dw-215-full --report-xunit-trx --report-xunit-trx-filename full.trx > /tmp/dw-215-full-tests.log 2>&1
actionlint .github/workflows/release.yml
git diff --check
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-215-release-tag-semver-validation.md
```

Actionlint and whitespace checks pass. The initial story-gitlink check exited **1** because the new spec lacked `baseline_commit`; adding the canonical entry commit made it pass, with no root pointer changes. No submodule, dependency or history rewrite was needed.

## Parser parity

The installed, lockfile-pinned semantic-release **25.0.9** uses semver **7.8.5**. Its tag admission calls `semver.valid(semver.clean(version))`, then its stable last-release selection excludes prereleases and orders with `semver.rcompare`. For the existing canonical plain-tag policy, the selector matches these numeric rules. Supporting other semver tag forms is outside this record's established scope.

`node /tmp/dw-215-parity.mjs` exits **0**: **5,832 candidates, 729 accepted, 5,103 rejected, zero admission or ordering mismatches**. The runner below extracts the actual jq selector, rather than restating its algorithm, and resolves semver from semantic-release. Run it from the repository root after installing the pinned npm dependencies; save as `/tmp/dw-215-parity.mjs`:

```javascript
import {createRequire} from 'node:module';
import {readFileSync} from 'node:fs';
import {spawnSync} from 'node:child_process';
const releaseRequire=createRequire(`${process.cwd()}/node_modules/semantic-release/index.js`);
const semver=releaseRequire('semver');
const workflow=readFileSync('.github/workflows/release.yml','utf8');
const selector=workflow.match(/release_tags="\$\(printf[^\n]+jq -r '\n([\s\S]*?)'\)"/)[1];
const numbers=['0','1','2','9','10','99','100','9007199254740990','9007199254740991','9007199254740992','9007199254740993','9999999999999999','10000000000000000','01','00','0009','9'.repeat(100),'9'.repeat(400)];
const tags=[];
for (const major of numbers) for (const minor of numbers) for (const patch of numbers) tags.push(`v${major}.${minor}.${patch}`);
const expected=tags.filter(t=>semver.valid(semver.clean(t.slice(1))));
expected.sort((a,b)=>semver.rcompare(a.slice(1),b.slice(1)));
const jq=spawnSync('jq',['-r',selector],{input:JSON.stringify([tags.map(t=>({ref:`refs/tags/${t}`}))]),encoding:'utf8',maxBuffer:16*1024*1024});
if (jq.status!==0) throw new Error(jq.stderr);
const actual=jq.stdout.trimEnd().split('\n');
if (JSON.stringify(expected)!==JSON.stringify(actual)) throw new Error('Mismatch in admission or numeric order');
console.log(JSON.stringify({semanticRelease:releaseRequire('./package.json').version,semver:releaseRequire('semver/package.json').version,cases:tags.length,accepted:actual.length,rejected:tags.length-actual.length,mismatches:0}));
```

## Aspire baseline and limits

The baseline requirement was checked using `aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj` (exit **0**), followed by `aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json` (exit **0**). The resource snapshot had waiting domain services, unhealthy security and healthy Memories resources; it does not establish a green distributed runtime. `aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj` exited **0** and stopped the app started for this check. No browser or distributed runtime acceptance is claimed or required for the isolated release-guard correction.

Existing release readiness holds remain. Registry version grammar (DW-216), broader tag-format policy, recovery lineage choices, package inventory, dependency pins, archive, sprint and unrelated records remain outside this correction.

## Independent review and final acceptance

Blind Hunter returned four test-control findings (interior 16-digit acceptance, padded-zero rejection, metadata/four-component exclusion and shared invalid fixtures). Each was classified **low / patch** and corrected. Independent recheck accepted all four, requested an accurate plain-policy test name because metadata is valid SemVer, and confirmed that naming correction. No finding remains unresolved and no review deferral was added. The spec records each verdict and its evidence.

The final source Debug build exits **0**, zero warnings/errors. After the added review controls, focused tests pass **49/49**; following the naming-only correction, the full fresh-built Contracts project passes **203/203**, including all 49 focused cases, zero failures/skips. The exact final commands are:

```bash
dotnet build tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Debug -m:1 -nr:false -p:UseHexalithProjectReferences=true > /tmp/dw-215-final-build.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Debug --no-build --no-restore --filter-method '*PackageGovernanceTests.ReleaseWorkflow*' --minimum-expected-tests 49 --no-ansi --no-progress --results-directory /tmp/dw-215-final-focused --report-xunit-trx --report-xunit-trx-filename focused.trx > /tmp/dw-215-final-focused-tests.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 203 --no-ansi --no-progress --results-directory /tmp/dw-215-final-full --report-xunit-trx --report-xunit-trx-filename full.trx > /tmp/dw-215-final-full-tests.log 2>&1
```

Only DW-215 closes. `/tmp/dw-215-close-ledger.py` compares all 653 ledger record bodies against `/tmp/dw-215-entry-state.json`: only DW-215 differs, with each replaced field retained as `pre-dw-215-*`. It verifies **364 done, 289 open**, and **261 index rows** that cover each open ID exactly once. The baseline hashes of the archive, sprint, npm manifest/lockfile, release configuration and package inventory remain identical; root gitlinks and canonical entry HEAD remain unchanged before the workflow's local commit. The maintenance table changes only its current counts, completion marker, DW-215 action row and new closure note; historical summaries remain.

The repository-pinned and installed commitlint CLI versions both equal **21.2.3**. `node_modules/.bin/commitlint --edit /tmp/dw-215-commit-message.txt --verbose` exits **0**, zero problems/warnings, validating the exact complete message `fix(ci): align release tag validation with semantic-release`. Successful evidence is preserved in `/tmp/dw-215-commitlint.log`. The invoked workflow requires the local commit; no push or publication is performed. Existing release qualification holds are unchanged.
