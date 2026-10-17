# DW-243/DW-262/DW-612 validation evidence

Entry: clean `main` at `a28e19330a3a97aaf48b4f77d8b66793e5d877a1`, 2026-10-17. Only these three ledger records close. Seven legacy token references become existing Fluent 2 roles in member-removal, metadata-edit and configuration-set CSS. Selectors, dimensions, system-color fallbacks and forced-colors rules are preserved. The conformance guard checks active component Razor and component/wwwroot CSS without exemptions, with 21 rejection/allowance controls. Original findings and pre-closure fields remain in the ledger.

## Build and test evidence

Commands ran from the repository root with the existing dependencies and pinned SDK. Builds use Debug source references; tests invoke the individual UI project through its maintained Microsoft.Testing.Platform lane.

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-method '*DomainUiFluentConformanceTests.DomainUiCssUsesCurrentFluentThemeTokens' --minimum-expected-tests 1 --no-ansi --no-progress --results-directory /tmp/tenants-dw-243-262-612/red --report-xunit-trx --report-xunit-trx-filename red.trx
```

The pre-migration build passes with zero warnings/errors in 28.52s. The guard then fails as intended: one failure, exit 2, listing exactly seven legacy references across the three stylesheets. The method was subsequently renamed `DomainUiUsesCurrentFluentThemeTokens` when review expanded its coverage to inline Razor. The first post-migration build passes and the following focused lane passes 375/375 before the review corrections.

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-class '*DomainUiFluentConformanceTests' --filter-class '*RemoveTenantMemberFlowTests' --filter-class '*EditTenantMetadataFlowTests' --filter-class '*SetTenantConfigurationFlowTests' --minimum-expected-tests 100 --no-ansi --no-progress --results-directory /tmp/tenants-dw-243-262-612/focused --report-xunit-trx --report-xunit-trx-filename focused.trx
```

After the review corrections, the same build command passes with zero warnings/errors in 16.22s. The expanded focused command uses `--minimum-expected-tests 383` and `/tmp/tenants-dw-243-262-612/focused-final`; 382/383 pass with zero skips, exit 2. The unchanged `PendingRemovalAuthorityReadReportsChangedSafety(cultureName: "en-US", changeConfirmation: False, isRetry: True)` expects the freshness refusal but briefly observes its preceding delivery-recovery message. No command implementation or existing assertion changed. Its immediate maintained-method recheck passes all eight cases, zero failures/skips, exit 0:

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-method '*RemoveTenantMemberFlowTests.PendingRemovalAuthorityReadReportsChangedSafety' --minimum-expected-tests 8 --no-ansi --no-progress --results-directory /tmp/tenants-dw-243-262-612/held-read-recheck --report-xunit-trx --report-xunit-trx-filename recheck.trx
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 6159 --no-ansi --no-progress --results-directory /tmp/tenants-dw-243-262-612/full --report-xunit-trx --report-xunit-trx-filename full.trx
```

Final full UI: **6159 passed, zero failures/skips, exit 0**, in 1m51.830s. This includes the 22 added guard cases and the previously failing case. The initial transient failure remains an observation; the passing reruns do not explain its scheduling cause. Exact TRX counters and reviewed source hashes are in [machine-readable validation](dw-243-262-612-ledger-validation.json).

## Browser and local topology

```bash
aspire ps --format Json
aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire logs security --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --tail 10
TENANTS_BROWSER_BUILD_CONFIGURATION=Debug CHROMIUM_BIN=/usr/bin/google-chrome-stable bash tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh
aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj
```

The AppHost starts successfully. Initially security initializes while Tenants/UI wait; subsequent inspection reports all three Running/Healthy. The exact Tenants AppHost is stopped after inspection. No topology or dependency changes ran.

The unchanged maintained Chromium runner passes actual rendered EN/FR removal/admission fixtures, measured 1024/767/768 transitions, focus/recovery, forced colors, identity checks and its negative controls; its included rendered status-label lane passes. Browser: Google Chrome 154.0.8037.57, restored Fluent 5.0.1. This is rendered-fixture evidence, not an authenticated command workflow or human assistive-technology assessment.

The Playwright CLI skill additionally checks isolated semantic selector fixtures carrying the generated Debug scope attributes and the three generated CSS assets. The fixture imports the restored Fluent module and calls its existing `beforeStart(document, 'web')`. A temporary server on port 62643 serves these inputs; `playwright-cli -s=dw243 open --browser=chrome`, `goto http://localhost:62643` and `run-code` drive light/dark media changes and forced colors, then the named browser is closed. Each border is compared against the resolved Fluent role and each keyboard-visible focus outline must remain solid and nonzero. No theme values are invented by the fixture.

| Role | Light computed value | Dark computed value |
| --- | --- | --- |
| `--colorNeutralStroke1` | `rgb(209, 209, 209)` | `rgb(102, 102, 102)` |
| `--colorPaletteRedBorder2` | `rgb(209, 52, 56)` | `rgb(227, 125, 128)` |
| `--colorStrokeFocus2` | `rgb(0, 0, 0)` | `rgb(255, 255, 255)` |

All seven changed properties match their roles in both themes. Forced colors matches CanvasText for four borders and Highlight for three focus outlines. Exact expected/actual values are preserved in the validation JSON. This isolated check does not claim complete rendered metadata/configuration workflows.

## Independent review, preservation and limits

The workflow's context-free blind review reports two guard bypasses: four unprefixed FAST primitives and inline Razor styles. Both are classified medium/patch, corrected with eight additional controls, and accepted by the independent correction recheck with no new concrete finding. No review item is deferred.

```bash
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-243-262-612-fluent-theme-tokens.md
git diff --check
node_modules/.bin/commitlint --edit /tmp/tenants-dw-243-262-612/commit-message.txt
```

Story-gitlink validation passes with no pointer changes; whitespace checking passes. The exact candidate `fix(ui): migrate command flow styles to Fluent theme tokens` passes the owning lockfile-pinned commitlint 21.2.3 with exit 0 before use. Its bytes and validation result are recorded in the machine-readable evidence. The skill requires a local commit; no push, branch or dependency update is performed.

All 647 IDs remain: **351 done, 296 open across 266 maintenance rows**. The other 644 ledger bodies and every unrelated action row are byte-equivalent as text; all selected pre-closure fields and original findings remain. Archive/sprint hashes, source execution hashes and gitlinks are checked against entry. The maintenance disposition table now explicitly counts all 647 records, correcting its inherited stale scope/counts while retaining historical closure narratives. Historical evidence, decisions and release holds are preserved. This CSS-only run does not rerun Release packaging, integration or release qualification. Release remains not ready.
