# DW-113 validation evidence — 2026-10-20

Entry revision: `6c5e3e10204c82fd22226755e5c13e33fede1953`, clean main.

## Scope and behavior

DW-113 is the oldest open actionable record with no dependency. `ListSurfaceStates` previously looked up `Tenants.List.Reason.{Reason}` for all eleven non-None enum members, while both resource bundles provide only five reason keys. The six remaining members have notice-specific vocabulary elsewhere; reusing those notices on an error surface could incorrectly promise an available list. No current production caller has been shown to send an unsupported reason to this surface; this is a reproduced latent component defect.

The component now accepts a reason translation only when `LocalizedString.ResourceNotFound` is false. Otherwise it uses the existing surface-kind message, including the distinct `SearchPageEmpty.MoreMessage` when `HasMore` is true. All resource values, component parameters, markup, control conditions and live-region semantics are retained.

The new `ListSurfaceStatesTests` class renders the component against real shipped EN/FR resources through the existing `CultureBoundTenantsLocalizer`. Its 280 cases cover all twelve defined reason values, two invalid casts (-1 and 999), all nine surface enum values, and the additional search-empty continuation variant. After review, expected reason availability comes from a direct ResourceManager query independent of the component's localizer metadata, with explicit nonblank controls for the five established reason resources. Missing reasons use state copy. Every case asserts exact rendered paragraph/title text and announcement semantics; no internal `Tenants.List.*` key may appear as the message. Applicable cases also click the rendered reset/refresh Fluent controls and assert their callback delivery. The Ready enum value retains its existing Empty surface fallback.

## Original-production reproduction

The tests were added before changing the component. From the repository root:

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 -v:q
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build -p:UseNuGetDeps=false -- --filter-class '*ListSurfaceStatesTests'
```

The Debug source build exits 0 with zero warnings/errors (`/tmp/dw113-baseline-build.log`). Original production exits 2: 280 total, 160 failed, 120 passed, zero skipped (`/tmp/dw113-baseline-tests.log`). The eight unsupported/invalid reasons fail in both cultures across ten surface variants, exposing raw reason keys. The None and five supported-reason controls pass. This supplies both defect reproduction and controls against replacing every reason with generic copy.

## Changed-production checks

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 -v:q
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-class '*ListSurfaceStatesTests' --minimum-expected-tests 280 --no-ansi --no-progress --results-directory /tmp/dw113/focused --report-xunit-trx --report-xunit-trx-filename results.trx
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 6564 --no-ansi --no-progress --results-directory /tmp/dw113/full --report-xunit-trx --report-xunit-trx-filename results.trx
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-113-list-reason-fallback.md
git diff --check
```

The changed-production Debug source build exits 0 with zero warnings/errors (`/tmp/dw113-build.log`). The focused class exits 0 with 280/280 passed and zero failures/skips (`/tmp/dw113-focused.log`, `/tmp/dw113/focused/results.trx`). The full UI suite exits 0 with 6,564/6,564 passed and zero failures/skips (`/tmp/dw113-full.log`, `/tmp/dw113/full/results.trx`). Own-spec gitlink validation passes against the full entry baseline, with no dependency pointer movement. The whitespace check passes.

## Runtime baseline and limits

```bash
aspire ps --format Json
aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --no-build --format Json
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire otel logs --severity Error --limit 3 --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json --non-interactive
aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --non-interactive
```

All commands exit 0. No AppHost was initially running. The started topology reported 23 Running/Healthy resources, including the UI; the bounded telemetry query still returned three Memories Error entries, two naming `InitializeTenantRegistryActivity`. The started AppHost was stopped successfully before rebuilding changed production. This records the existing runtime baseline, not an entirely healthy topology or a browser/assistive-technology qualification. No dependency, topology, style or focus behavior changes are part of DW-113.

## Ledger integrity and commit validation

`python3 /tmp/dw113/close-ledger.py` gates closure on the exact all-passing 280-case focused and 6,564-case full TRX inventories, verifies no external ledger/index edits, and changes only DW-113 among all 656 records. Every replaced field is retained as `pre-dw-113-*`. It verifies 396 done/260 open IDs, and that the remaining 235 maintenance rows cover all 260 open IDs exactly once. Only the DW-113 index row is removed; a dated closure summary is added to each document. The archive, sprint status, dependencies and unrelated records remain unchanged. Existing release qualification holds remain in force.

Pinned `@commitlint/cli@21.2.3`, invoked with `node_modules/.bin/commitlint --edit /tmp/dw113/commit-message.txt --verbose`, exits 0 and reports zero problems or warnings. Successful validation is retained in `/tmp/dw113/commitlint.txt`. The exact full message is:

```text
fix(ui): fall back to localized tenant list state messages
```

`git fetch origin` succeeds and `git rev-list --left-right --count main...origin/main` reports `0 0` at the entry revision. The user authorized the final push to main. No release workflow dispatch is part of this work.

## Independent review

The context-free blind reviewer found no functional regression and returned three test/documentation improvements. All are triaged low and patched: replace a fixed expected-key allowlist with availability from the shipped ResourceManager while retaining explicit supported-reason controls; verify rendered reset/refresh callback delivery with unsupported reasons; and record exact reproduction commands/configuration/logs directly in the spec. No frozen-intent change, production follow-up or new deferred item was needed. The original reproduction and first all-passing full run above remain valid historical results; final strengthened-test rechecks are recorded separately below.

The same reviewer independently rechecked the corrections and ledger/index integrity, confirmed all three findings addressed, and found no concrete remaining issue. This was a read-only inspection; executed test evidence belongs to the commands below.

## Strengthened-test final acceptance

After the review corrections:

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 -v:q
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-class '*ListSurfaceStatesTests' --minimum-expected-tests 280 --no-ansi --no-progress --results-directory /tmp/dw113/focused-final --report-xunit-trx --report-xunit-trx-filename results.trx
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 6564 --no-ansi --no-progress --results-directory /tmp/dw113/full-final --report-xunit-trx --report-xunit-trx-filename results.trx
```

The final Debug source rebuild exits 0 with zero warnings/errors (`/tmp/dw113-build-final.log`), and the strengthened focused class exits 0 with 280/280 passed and zero failures/skips (`/tmp/dw113-focused-final.log`, `/tmp/dw113/focused-final/results.trx`).

The strengthened full UI suite also exits 0 with 6,564/6,564 passed and zero failures/skips (`/tmp/dw113-full-final.log`, `/tmp/dw113/full-final/results.trx`). Both final TRX inventories are verified to contain exactly the expected passing results. The subsequent PascalCase test-method naming adjustment changes no test behavior and receives its own compile/focused recheck; no third full-suite run is needed for that identifier-only change.

That final compile uses the same Debug source-build command and exits 0 with zero warnings/errors (`/tmp/dw113-build-final-name.log`). The same focused command, with results directory `/tmp/dw113/focused-final-name`, passes 280/280 with zero failures/skips (`/tmp/dw113-focused-final-name.log`). Its TRX report is `/tmp/dw113/focused-final-name/results.trx`. The final ledger integrity recheck confirms all replaced historical fields are retained, only DW-113 changes, and the 235 remaining rows cover the 260 open records exactly once.

A first ancillary resource-integrity probe compared raw worktree bytes to Git blob bytes and rejected the unchanged neutral resource because its CRLF worktree content is normalized in Git. The corrected check compares `git hash-object --path=<path> <path>` with `git rev-parse <entry>:<path>`, honoring tracked clean filters. It passes for both resource bundles, deferred archive and sprint status. No resource content was changed by either check. The targeted ledger equality check still reports only DW-113 changed, and own-spec gitlink/whitespace checks pass.

The contributor formatting requirement is checked narrowly with `dotnet format whitespace tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --include /home/administrator/projects/hexalith/tenants/tests/Hexalith.Tenants.UI.Tests/Components/ListSurfaceStatesTests.cs --verify-no-changes --no-restore`. It exits 0 with no formatting diagnostic, while printing the generic workspace-load warning (`/tmp/dw113-format.log`); this is a scoped file-format check, not a claim that the entire repository formats cleanly. A final origin fetch again reports `0 0` divergence before committing.
