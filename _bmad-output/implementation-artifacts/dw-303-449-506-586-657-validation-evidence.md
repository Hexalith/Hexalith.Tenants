# DW-303 / DW-449 / DW-506 / DW-586 / DW-657 validation

Date: 2026-10-21. Clean entry baseline on main: `ab450987de52d3cb305e74da3039c9b0d2cc335b`.

## Behavior and boundaries

DW-303 adds dedicated EN/FR lifecycle message/recovery resources when missing projection-version evidence is the effective blocker. The compatibility adapter and both lifecycle renderers preserve the existing shared evaluator and enum values. Null/empty/whitespace versions, both operations, typed/legacy inputs, Aging and Refreshing with a current baseline, Refreshing without one, and earlier/later blockers are qualified. Restoring evidence permits one tracked dispatch using that version; it does not imply success.

DW-449 and DW-586 were already fixed at entry. The five type-named snapshots handle Pending and retryable lookup results before null-status failure. Twenty new cross-command cases independently verify raw/keyed pending and retryable feedback, exact attempt/identity/projection/audit retention, unknown/contradictory controls, later authoritative confirmation and terminal stability. Original-source replay passes these cases. No production command snapshot changes are needed.

DW-506 makes the correction button and recovery paragraph follow actual display eligibility. Competing reconciliation and held authorization use waiting guidance while retaining the command identity; eligible retry guidance returns afterward. Null-correlation retries still use the existing core predicate. Empty/whitespace correlation cannot enable a status lookup; captured refresh callbacks issue no status request or redispatch. EN/FR grant/removal tests cover both recovery phases. Compatibility blank-correlation snapshots are deliberately seeded; this is not evidence that a production gateway returns such tracking.

DW-657 normalizes absent external reasons at render and pre-dispatch refusal boundaries. Captured/repeated valid submits retain a localized explanation without dispatch. An exact-reference marker permits only a known unsent refusal to reset when support/admission recovers. Adopted, replaced, expired and ambiguous attempts retain their existing tracking gates. EN/FR tests cover unavailable support, refused admission and subsequent single-dispatch recovery.

This is component/state validation. It makes no browser, screen-reader, distributed mutation or release-readiness claim. Existing release holds, dependencies, archive, sprint state and unrelated ledger records remain intact.

## Environment and build

The existing AppHost was already running. Inspected with:

```bash
aspire ps --format Json
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
```

Tenants API/domain/UI, Memories and security report Running/Healthy at entry. Raw runtime configuration stays outside the repository. No AppHost restart or edit was needed.

All source builds use:

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 -v:q
```

Initial, corrected, original-source and reviewed builds pass with zero warnings/errors. The final acceptance build takes 16.40 seconds.

## Maintained project test lane

Base command, run from the repository root:

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --no-ansi --no-progress --report-xunit-trx --report-xunit-trx-filename results.trx
```

Append one row per invocation; the focused rows also use `--minimum-expected-tests 1`:

| Filter | Results directory | Reviewed result |
| --- | --- | --- |
| `--filter-class '*TenantCommandTemporaryLookupTests'` | `/tmp/tenants-feedback/review-TenantCommandTemporaryLookupTests` | 20/20 pass |
| `--filter-class '*MemberCommandExplanationTests'` | `/tmp/tenants-feedback/review-MemberCommandExplanationTests` | 58/58 pass |
| `--filter-class '*TenantLifecycleProjectionCategoryTests'` | `/tmp/tenants-feedback/review-TenantLifecycleProjectionCategoryTests` | 138/138 pass |
| `--filter-class '*GlobalAdministratorCorrectionPanelTests' --minimum-expected-tests 135` | `/tmp/tenants-feedback/correction-final` | 135/135 pass |
| `--filter-class '*TenantLifecycleActionAvailabilityTests'` | `/tmp/tenants-feedback/availability-final` | 113/113 pass |

These 464 focused tests have zero failures/skips/errors. The final correction run follows the last display-busy change and the adopted-delivery expectation update. The final full UI result is recorded below.

Initial failures are preserved here:

- A combined `--filter-class` value joining four globs with `|` selects zero tests and exits 5 under the minimum-count policy. Separate class filters replace it; no zero-test run counts as validation.
- Initial correction 133/135 fails the two new grant cases because the stub simulates a first-read preflight adding the grant target. This scenario disables that simulation and then passes 135/135.
- After review changes waiting copy, an existing adopted-grant test still expects retry instructions while its delivery task is held: 134/135. It now asserts waiting/disabled behavior while held and retry recovery after ambiguous completion, retaining its original exact-identity assertions. Final correction 135/135 passes.
- First full UI run is 7032/7033 with zero skips: the existing `Missing_projection_version_disables_lifecycle_with_visible_refresh_recovery` test expects the stale-data wording that DW-303 replaces. It is renamed to `MissingProjectionVersionDisablesLifecycleWithVisibleSupportRecovery`, its stub receives the new resource keys, and its assertions require the exact missing-version/support guidance while preserving disabled/zero-dispatch checks.
- Initial scoped formatter reports 15 new-file initializer whitespace diagnostics. Formatting only the new test file corrects them; final scoped formatting passes.

## Original-source controls

Before the review refinements, save the patched bytes of these five logic files, replace them with `git show ab450987de52d3cb305e74da3039c9b0d2cc335b:<path>`, build, run the controls, and restore every saved file in `finally` before the next reviewed build:

- `Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor`
- `Components/Tenants/Members/AddTenantMemberFlow.razor`
- `Components/Tenants/Lifecycle/TenantLifecycleActionAvailability.razor`
- `Components/Tenants/Lifecycle/TenantLifecycleCommandFlow.razor`
- `State/TenantDetail/TenantLifecycleAvailability.cs`

Paths above are under `src/Hexalith.Tenants.UI/`. Tests and the new resources remain present. The original-source build has zero warnings/errors. Each control uses the base maintained command with `--minimum-expected-tests 1`:

| Control filter | Result | Exit |
| --- | --- | --- |
| `--filter-method '*MissingProjectionVersionHasDedicated*'` | 0 pass / 24 fail / 0 skip | 2 |
| `--filter-class '*MemberCommandExplanationTests'` | 46 pass / 12 fail / 0 skip | 2 |
| `--filter-method '*AmbiguousCorrectionRetryLabelTracksActualEligibility'` | 0 pass / 4 fail / 0 skip | 2 |
| `--filter-class '*TenantCommandTemporaryLookupTests'` | 20 pass / 0 fail / 0 skip | 0 |

Results directories are `/tmp/tenants-feedback/original-lifecycle`, `original-member`, `original-correction` and `original-pending`. This replay demonstrates the initial defects and already-fixed pending behavior; the later recovery and held-authorization refinements are qualified by reviewed/final runs, not retroactively claimed as part of this replay.

## Review and repository checks

Blind Hunter computes `min(floor(sqrt(41.368) + 1), 10) = 7`. All seven findings receive in-scope corrections recorded in the owning spec: unsent refusal recovery, waiting paragraph, blank correlation refusal, extra freshness cases, blocker precedence, restored-version dispatch, and independent audit-state controls. Recheck identifies the remaining held-authorization display interval; the final predicate and test resolve it. Final independent recheck clears all seven findings and identifies no additional defect. Nothing is newly deferred.

```bash
dotnet format whitespace tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --no-restore --verify-no-changes --include src/Hexalith.Tenants.UI/State/TenantDetail/TenantLifecycleAvailability.cs tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorCorrectionPanelTests.cs tests/Hexalith.Tenants.UI.Tests/Components/MemberCommandExplanationTests.cs tests/Hexalith.Tenants.UI.Tests/Components/TenantLifecycleProjectionCategoryTests.cs tests/Hexalith.Tenants.UI.Tests/State/TenantCommandTemporaryLookupTests.cs --verbosity quiet
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-303-449-506-586-657-command-feedback.md
git -c core.safecrlf=false diff --check
```

All pass. The additional whitespace check includes `tests/Hexalith.Tenants.UI.Tests/Components/TenantLifecycleActionAvailabilityTests.cs` using the same formatter invocation and also passes. The gitlink validator reports no references pointer changes from the full entry baseline through HEAD plus working tree.

The installed lockfile-pinned commitlint CLI is `21.2.3`. Exact full message `fix(ui): complete deferred command feedback and recovery` plus its terminating newline is validated before commit:

```bash
node_modules/.bin/commitlint --verbose < /tmp/tenants-feedback-commit-message.txt
```

Exit 0, zero problems/warnings. Successful output is retained in `/tmp/tenants-feedback-commitlint.log`. Before push preparation, `git fetch origin main` succeeds and `git rev-list --left-right --count HEAD...origin/main` returns `0 0`; remote main is the full entry baseline above.

## Final full acceptance and ledger preservation

The base maintained test command with `--minimum-expected-tests 7000 --results-directory /tmp/tenants-feedback/full` passes 7033/7033, zero failures/skips/errors, in 1m 47.232s. The final source build has zero warnings/errors; final scoped formatter and gitlink/whitespace checks pass. The initial full failure remains recorded above and its TRX is retained separately at `/tmp/tenants-feedback/first-full-results.trx`.

`python3 /tmp/tenants-feedback-close-ledger.py` compares canonical entry-baseline records with the closeout, preserves the original values under `pre-command-feedback-*` keys before replacing status/disposition/verification fields, and verifies every unselected entry remains byte-identical. It removes exactly the five selected maintenance rows and checks unique exact coverage of all remaining open IDs. The resulting machine-readable evidence is [dw-303-449-506-586-657-ledger-validation.json](dw-303-449-506-586-657-ledger-validation.json): 657 IDs retained, 425 done, 232 open across 211 maintenance rows. No archive, sprint, dependency, governance gate or release-hold change is made.
