# DW-371 / DW-386 / DW-396 / DW-406 / DW-414 validation

Date: 2026-10-21. Clean entry on main: `406114bb131792e8411e16b9aac6885b3132444e`.

## Behavior and boundaries

DW-371 adds removal-preview localization reason/recovery properties following the existing grant contract. The administrator page consumes only complete nonblank pairs for the effective missing-preview blocker. Correction preview and tracked recovery distinguish localization failure from transport support, keeping permission and viewport precedence. EN/FR tests cover both actions, null/empty/whitespace pair fallbacks, simultaneous transport/viewport failures, and an absent explicit French entry whose indexer resolves an English fallback.

DW-386 reuses the existing per-composition immutable fixed removal-localization result. Two positive controls initialize through readiness and composition: 100 valid previews plus repeated readiness/diagnostic reads each require exactly two resource-table enumerations. Before this change, each composition repeats both enumerations (202 including readiness). Negative controls qualify both initialization orders for missing EN and FR resources. Target absence and last-administrator evidence remain per-preview checks. This measures eliminated work; it does not claim a user-visible latency improvement.

DW-396 exercises missing tracked grant/removal capability, enabled captured confirmations invoked after capability loss, and captured recovery callbacks after ambiguous or accepted/correlated submissions. Unsupported paths dispatch nothing further, perform no additional status reads and preserve exact message/correlation evidence and aggregate ownership.

DW-406 retains recognized platform-governance rejection reasons using shipped command-neutral EN/FR resources for grant submission and fixed-aggregate rejected status. Unknown rejection codes retain generic safe feedback. Gateway and page tests qualify exact keys, terminal classification, raw-payload suppression, preserved rows and assertive messages. All four consumed rejection keys now participate in fixed grant readiness; eight EN/FR missing-value cases qualify fail-closed behavior.

DW-414 replaces the existing removal rejection test's echoed expected/raw text with a distinct raw sentinel and exact real EN/FR resource assertions for last-administrator and target-not-found rejection. The sentinel must be absent while rows, assertive feedback and the original no-success/no-member-copy assertions remain.

These are component, gateway and localization checks. No browser, assistive-technology, distributed mutation, deployment or release-readiness evidence is asserted. No dependency or AppHost change is required.

## Environment and build

The existing AppHost is running. Entry inspection:

```bash
aspire ps --format Json
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
```

Tenants UI/API/domain, EventStore, Memories and security report Running/Healthy. Raw runtime configuration is retained outside the repository.

The Debug source-reference build command is:

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 -v:q
```

Final acceptance build passes with zero warnings/errors in 14.99 seconds. Log: `/tmp/tenants-admin-feedback-build-final.log`.

## Tests and retained initial failures

Base maintained project command:

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --no-ansi --no-progress --report-xunit-trx --report-xunit-trx-filename results.trx
```

Each focused invocation adds `--filter-class '*CLASS' --minimum-expected-tests 1 --results-directory DIRECTORY`. The final BFF run instead requires `--minimum-expected-tests 112`.

| Class | Results directory | Result |
| --- | --- | --- |
| `GlobalAdministratorCorrectionPanelTests` | `/tmp/tenants-admin-feedback/review-GlobalAdministratorCorrectionPanelTests` | 183/183 pass |
| `GlobalAdministratorsPageTests` | `/tmp/tenants-admin-feedback/recheck-GlobalAdministratorsPageTests` | 274/274 pass |
| `TenantCommandGatewayTests` | `/tmp/tenants-admin-feedback/recheck-TenantCommandGatewayTests` | 774/774 pass |
| `TenantsBffCompositionTests` | `/tmp/tenants-admin-feedback/accepted-TenantsBffCompositionTests` | 112/112 pass |

These 1343 focused tests pass with zero failures/skips.

Initial failures remain disclosed:

- The page-test accepted-result fixture initially omitted the required message ID; the build fails with CS7036. Supply both IDs.
- Initial correction tests pass 143/151: eight cases try to click a removed DOM node. Capture and invoke the actual Fluent callback instead.
- Initial page tests pass 270/274: four cases expect disabled removal buttons, whereas the established UI renders reason/recovery slots for unavailable actions. Assert those rendered slots and zero dispatch.
- Initial gateway tests pass 772/774: two old unauthorized/forbidden controls expect generic grant feedback. Update only the expected resource keys to the specific permission reason.
- Expanding review-test parameters initially modifies the wrong adjacent theory, producing 50 compilation/analyzer errors. Correct method-bound targeting; the next source build has zero warnings/errors.
- Reviewed BFF tests pass 111/112: adding four required grant keys invalidates the existing exact inventory count of 104. Update it to 108; retain the exact inventory assertion. The first final BFF/full runs then expose the adjacent distinct-key count still expecting 104: BFF 111/112 and full UI 7114/7115, zero skips. Updating that expectation to 108 preserves the uniqueness assertion. The first full TRX/log are retained separately at `/tmp/tenants-admin-feedback/first-full-results.trx` and `/tmp/tenants-admin-feedback-first-full.log`.

Initial TRX/logs remain under `/tmp/tenants-admin-feedback/` and `/tmp/tenants-admin-feedback-*Tests.log`. No failed or zero-test run counts as acceptance.

## Review and repository checks

Blind Hunter computes `min(floor(sqrt(37.435 kB) + 1), 10) = 7`. The owning spec records all seven findings and their in-scope patches. Independent recheck accepts each correction and finds no concrete introduced defect. No finding is newly deferred.

```bash
git -c core.safecrlf=false diff --check
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-371-386-396-406-414-administrator-feedback.md
```

Both pass; no dependency pointer moves are present.

The scoped formatter command is:

```bash
dotnet format whitespace tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --no-restore --verify-no-changes --include tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorCorrectionPanelTests.cs tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantsBffCompositionTests.cs --verbosity quiet
```

It exits 2 on 112 inherited diagnostics in unchanged page-test lines. A mapping of each reported line to `git show HEAD:<path>`/current changed ranges confirms zero diagnostics on introduced lines after correcting one moved initializer. Initial and final logs are `/tmp/tenants-admin-feedback-format.log` and `/tmp/tenants-admin-feedback-format-final.log`. This inherited formatting blocker is separate from the passing build/tests; no build/format policy is weakened.

## Commit validation and synchronization

The installed lockfile-pinned commitlint CLI is 21.2.3. Exact full message, including terminating newline, is stored in `/tmp/tenants-admin-feedback-commit-message.txt` and validated before use:

```bash
node_modules/.bin/commitlint --verbose < /tmp/tenants-admin-feedback-commit-message.txt
```

Exit 0, zero problems/warnings; successful output is retained in `/tmp/tenants-admin-feedback-commitlint.log`. The validated message is `fix(ui): complete five administrator feedback follow-ups`.

`git fetch origin main` succeeds; `git rev-list --left-right --count HEAD...origin/main` returns `0 0` during push preparation, with remote main at the entry baseline above.


## Final acceptance and ledger preservation

The base project command with `--minimum-expected-tests 7115 --results-directory /tmp/tenants-admin-feedback/full` passes 7115/7115 with zero failures/skips/errors in 2m00.756s. Final BFF 112/112, the other focused classes and the zero-warning/error final source build qualify the reviewed changes. The initial full failure is retained separately; only the later passing run counts as acceptance.

`python3 /tmp/tenants-admin-feedback-close-ledger.py` compares the entry-baseline ledger and maintenance index with the closeout. It preserves changed historical values under `pre-administrator-feedback-*` keys, asserts that every unselected record body is byte-identical, removes exactly the five selected index rows and verifies unique exact coverage of every remaining open ID. Machine-readable evidence is [dw-371-386-396-406-414-ledger-validation.json](dw-371-386-396-406-414-ledger-validation.json): all 657 IDs retained, 430 done/227 open across 206 maintenance rows. No archive, sprint, dependency, governance gate or release-hold change is made.
