# DW-408 / DW-440 / DW-450 / DW-460 / DW-474 validation evidence

Date: 2026-10-26. Implementation baseline: `665068ec0791fd2ea0e00e1c04b25d60ed0b9fbb`.

The approved [implementation spec](spec-dw-408-440-450-460-474-audit-contracts.md) is the source of intent. This implementation handoff does not itself close ledger records or declare the spec done; independent review and root reconciliation follow.

## Implemented contract

- DW-408 preserves whole-page rejection of unknown events/category mismatches, including mixed pages and both explicit streams. Qualification verifies that rejected pages retain no earlier rows, paging metadata or projection proof. Policy documents coordinated UI-before-server event rollout.
- DW-440 uses invariant `yyyy-MM-dd HH:mm:ss UTC` for grid and receipt labels. EN/FR surrounding labels remain localized; a non-Gregorian culture checks invariant formatting. Grid presentation converts offsets and leaves absent timestamps empty; receipt/gateway authority validation still rejects noncanonical timestamps.
- DW-450/DW-474 align all seven production component files to Rounded badges, Filled for Danger/Severe, Tint otherwise, and localized icon labels. A FluentText named-image wrapper exposes one state name while its inner visual FluentBadge subtree is aria-hidden; the pinned FluentBadge exposes no icon-only aria-hidden parameter. Chromium checks computed accessibility structure, not spoken screen-reader output. Availability/lifecycle parents retain live-region ownership; refreshing truth badges explicitly retain polite atomic live-region properties. The maintained Chromium launcher checks actual exported EN/FR badge markup in ordinary and forced colors and queries Chromium's accessibility tree.
- DW-460 adds explicit lifecycle/provenance overloads to the receipt and row entry factories. Gateway mapping consumes the row overload. The existing metadata-free overloads retain Unknown authority and conservative Partial receipts. Required evidence, support safety, surface precedence and command-audit precedence remain enforced.
- Additive precedence notes in the audit evidence specification, DESIGN.md and epics.md preserve historical specifications and release holds.

## Pre-edit runtime baseline

Commands:

```text
aspire ps --format Json --non-interactive
aspire start --no-build --isolated --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json --non-interactive
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json --non-interactive
aspire wait tenants-ui --timeout 35 --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --non-interactive
aspire otel logs --severity Error --limit 5 --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json --non-interactive
aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --non-interactive
```

All commands exited 0. Initially no AppHost was running. The isolated task-owned app reported 24 resources: 23 Running/Healthy and Memories Finished/None. `tenants-ui` was healthy immediately at the wait call; the captured error query returned zero records. The task-owned AppHost was stopped after inspection. This is a pre-edit snapshot using existing Debug assets, not release/runtime acceptance of these edits.

## Validation attempts and final results

Initial `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true` failed with CS8602 at the new receipt assertion (`TenantAuditPageTests.cs:2646`). The assertion now validates non-null before access. A subsequent export helper compile failed CS1929 because its generic constraint lacked `class`; fixed. Two source-graph builds also recorded transient MSB3026 copy warnings for `Hexalith.EventStore.Contracts.dll`.

An explicit serialized build (`--no-restore -m:1 -p:BuildInParallel=false`) passed with zero warnings/errors. Its focused run had 1,315 cases, 930 failures, zero skips: most failed loading `Hexalith.EventStore.Contracts, Version=3.128.2.0` because source project edges built different assembly versions to the same output. The same run exposed two incorrect new test assumptions, both corrected: tenant IDs retain their existing meaningful-string policy (the unsafe test now uses a prohibited bearer fragment), and grid header labels are checked through `TemplateColumn.Title` rather than unavailable bUnit DOM header roles. No production safety or build gate was relaxed.

The final local source-build command pins the existing EventStore version as a command-line build property to align every project edge; no tracked dependency/version configuration changes:

```text
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -p:Version=3.128.2 --no-restore -m:1 -p:BuildInParallel=false -nr:false
```

After alignment, the focused selection executed 1,776 cases with two failures (the new detail test incorrectly expected list colors; the existing detail semantics are Danger for Disabled and Subtle for Unknown). The initial Chromium badge check rejected unignored icon/text child nodes under a named badge; it does not claim a spoken screen-reader duplicate. The pinned Fluent V5 `IconStart` / `IconEnd` / `IconLabel` API has no icon-only aria-hidden parameter. A FluentText named-image wrapper now owns the state name and optional live semantics; the visual inner badge is aria-hidden. The wrapper rerun executed 1,776 cases with 17 failures in the new assertions: component-scoped DOM omits outer ancestors, and FluentText renders a host plus its optional span. Assertions now use the complete rendered surface and the named ancestor selector instead of assuming an immediate parent. The production wrapper was unchanged by those test corrections. These failed attempts are not acceptance evidence.

One focused invocation combined class and method filters and selected zero cases; it was discarded and corrected to class-only filters. Final source build: **passed, 0 warnings / 0 errors**, 40.47 seconds. Focused classes: **1,776 / 1,776 passed**, zero errors, failures, skipped or not-run cases, 48.222 seconds.

The maintained browser launcher passed its assertion scenarios (including all four new EN/FR × normal/forced-color badge cases), then exited 1 in an existing status-label validator cleanup: `ENOTEMPTY: directory not empty, rmdir '/tmp/tenants-status-label-HQty60/profile/Default'`. That attempt is retained as failed overall. The identical launcher then exited **0**, including all four new badge cases and the existing audit/focus/forced-color checks; the unrelated validator was not changed. Browser evidence identifies Chrome `154.0.8037.57` and Fluent UI `5.0.1`.

The initial maintained full UI run executed **7,747** cases: 7,745 passed, two failed, zero skipped (2m 08s 203ms). One failure was the existing `CorrectionStartPanelTests.LateDeliveryReconciliationCannotAcquireTheReplacementStatusGuard` five-second timeout while Chromium was also active. The other was `SupportSafetyEvidenceGateTests.No_test_offers_a_ToString_substring_check_as_support_safety_evidence`, which correctly rejected two new receipt `ToString()` substring assertions. Those assertions now inspect the sanitized actor/target/scope fields directly at the factory boundary; the governance guard remains unchanged. After the test-only rebuild, receipt/support-safety tests passed **67/67** (0.650s), and the previously timed-out existing test passed **1/1** alone (0.618s). The final maintained full UI project run **passed 7,747/7,747**, zero failed/skipped, exit 0, duration **1m 53s 058ms**, without concurrent browser validation. The browser-tested UI DLL is unchanged after that test-only rebuild: SHA-256 `5597850c5fc60bb868f0729a9b4ffeff9540995229d5ff1e0a64f4405ac7c68b`.

## Reproduction commands

Focused class selection (1,776 cases):

```bash
tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests \
  -class Hexalith.Tenants.UI.Tests.State.TenantAuditReceiptTests \
  -class Hexalith.Tenants.UI.Tests.Services.Gateways.TenantQueryGatewayTests \
  -class Hexalith.Tenants.UI.Tests.Components.AuditAvailabilityStateTests \
  -class Hexalith.Tenants.UI.Tests.Components.TruthStateBadgeTests \
  -class Hexalith.Tenants.UI.Tests.Components.ProjectionLifecycleStatusTests \
  -class Hexalith.Tenants.UI.Tests.Components.TenantAuditPageTests \
  -class Hexalith.Tenants.UI.Tests.Components.StatusBadgeContractTests \
  -class Hexalith.Tenants.UI.Tests.Components.TenantListSurfaceTests \
  -class Hexalith.Tenants.UI.Tests.Components.TenantDetailSurfaceTests
```

Post-correction factory/governance checks and timing-test retry:

```bash
tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests \
  -class Hexalith.Tenants.UI.Tests.State.TenantAuditReceiptTests \
  -class Hexalith.Tenants.UI.Tests.SupportSafetyEvidenceGateTests
tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests \
  -method '*CorrectionStartPanelTests.LateDeliveryReconciliationCannotAcquireTheReplacementStatusGuard'
```

Maintained broad gates:

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore -p:UseHexalithProjectReferences=true -p:Version=3.128.2 -- --report-xunit-trx --results-directory /tmp/dw-408-474-full-ui-final
env CHROMIUM_BIN=/usr/bin/google-chrome-stable TENANTS_BROWSER_BUILD_CONFIGURATION=Debug bash tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh
```

Browser scope: rendered production Fluent component fixtures, real Chromium accessibility/layout/forced-colors checks, and existing mutation controls. This does not qualify authenticated backend authorization, real domain data, human assistive-technology speech, package/Release assets, or release readiness. The selected work does not change those boundaries.

## Review repairs and focused verification

The preceding full-suite results qualify the initial implementation, before these review repairs. Root review owns the subsequent full verification; this repair leg ran only affected test classes/methods and the badge browser lane.

- Global administrator set/remove outcomes now require the Access category, with both operations tested with/without user targets. Configuration set/remove receipts require a safe typed configuration key matching the target. Missing/unsafe keys and conflicting targets remain Partial; existing safe fallback presentation is retained without granting evidence authority.
- Gateway rejection cases now include combined-stream category mismatches and unsupported-first mixed pages, preserving the earlier-proof/metadata rejection assertions.
- Badge exports bind 34 explicit site/state cases per language, including all three detail statuses and both pending states. Visible labels, icon labels and outer accessible names are checked against independent state-selected resource expectations. Availability markup retains its production scoped root, and the browser loads the compiled scoped CSS for all seven component sites.
- Chromium verifies icon/glyph and text dimensions, visibility and paint, plus the production availability forced-color outline. Four mutation controls (missing name, exposed descendant, wrong appearance and invisible icon) run for both languages in normal and forced colors: 16 intended rejections.
- Current → refreshing → current renderer assertions verify one named owner, hidden visual descendants, polite/atomic attributes only during refreshing, and restoration of the static state. Browser results establish computed accessibility properties, not human speech or dynamic assistive-technology announcements; human qualification remains outstanding.

The first repair build failed with two CS1061 errors because the new transition test used the old bUnit `SetParametersAndRender` API. Using the repository's `Render` API fixed it. The first badge-only run rejected the validator because the duplicate-descendant mutation was incorrectly accepted: the partial AX tree check did not traverse an ignored intermediary host. The opt-in validator now traverses the full subtree and rejects unignored exposed descendants while disregarding ignored raw nodes. The final browser rerun passed all positive cases and all 16 rejection controls.

Final repair build used the same source-reference, serialized, `Version=3.128.2` command above: **passed, 0 warnings / 0 errors**, 27.81 seconds. The four affected receipt/gateway/truth/export classes passed **852/852** (2.877s); the localized detail method passed **6/6** (0.994s). No full test suite or unrelated browser scenario was rerun in this repair leg.

```bash
tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests \
  -class Hexalith.Tenants.UI.Tests.State.TenantAuditReceiptTests \
  -class Hexalith.Tenants.UI.Tests.Services.Gateways.TenantQueryGatewayTests \
  -class Hexalith.Tenants.UI.Tests.Components.StatusBadgeContractTests \
  -class Hexalith.Tenants.UI.Tests.Components.TruthStateBadgeTests
tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests \
  -method '*TenantDetailSurfaceTests.DetailStatusUsesTheSharedBadgeAnatomy'
env CHROMIUM_BIN=/usr/bin/google-chrome-stable TENANTS_BROWSER_BUILD_CONFIGURATION=Debug \
  bash tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh --badges-only
```

The badge lane also runs by default in the maintained full launcher. Shell/JavaScript syntax checks and `git diff --check` pass.

| Repair result | Local log | SHA-256 |
| --- | --- | --- |
| review-build.log | `/tmp/dw-408-474-review-build.log` | `55ad8a1543b9f6641a7df37a75907215225f7bb865ba2c34f0f31492d2288804` |
| review-build-final.log | `/tmp/dw-408-474-review-build-final.log` | `273f9c03c106721f67e247aaad25c921d61ea2314a816b17217720a7c29f98d0` |
| review-focused.log | `/tmp/dw-408-474-review-focused.log` | `0b0782b3c64ba7d1e032cc2c1373e69c4d0dd5a4de14d4ad8ef96fcc96193563` |
| review-detail.log | `/tmp/dw-408-474-review-detail.log` | `98cc55fd5655af0536ba18f4c9a71b1c27548252356ae424c5462a5bba271dce` |
| review-browser.log | `/tmp/dw-408-474-review-browser.log` | `5ab45af849e811f1614f4a00b38275c7f21518ef97474ddb53861a851019c8fa` |
| review-browser-retry.log | `/tmp/dw-408-474-review-browser-retry.log` | `26c59266fa5565bb407096a473b9cf75a0854a72300e2013b65163935815a076` |

## Typed-target compatibility correction

The parent full run after the first review repair finished with **7,759/7,762 passed**, three failures, zero skipped, exit 2, **1m45.268s** (`/tmp/dw-408-474-root-full-ui.log`, TRX under `/tmp/dw-408-474-root-full-ui`). It exposed an over-restriction in the first configuration completeness repair: requiring Target == ConfigurationKey invalidated the existing mapper contract that selects a safe typed UserId before a configuration key when both exist. The existing `Administrative_entry_with_user_and_configuration_key_uses_the_typed_user_as_receipt_target` Ready expectation remains unchanged.

Configuration Set/Removed still require a safe configuration key. Target must now match the mapper's selected typed user when present, otherwise the key. Both operations have positive both-field controls and mismatched-target controls; absent/unsafe keys remain Partial even when a safe user target exists. The localized `AuditEvidenceReceiptTests` fixture now supplies a real configuration key for both configuration operations and explicitly checks Ready while retaining its localized label and copy assertions. The two prior outcome failures came from incomplete generic fixtures, not a need to relax production missing-key validation. The independently split `BadgeContractFixture.cs` is unchanged.

No build started until the parent reported that its full test process had finished. The first correction build failed CA1062 because moving receipt construction outside the render lambda made the existing test's unguarded eventType parameter visible to analysis. An explicit null guard fixed it. The final build used the same source-reference serialized `Version=3.128.2` command recorded above: **passed, 0 warnings / 0 errors**, **29.96s**.

Only directly affected checks ran: receipt state/component classes **143/143 passed** (1.048s), and the unchanged typed-user page regression **1/1 passed** (0.742s), all with zero errors/failures/skips/not-run cases. No browser or full-suite rerun was performed in this correction leg. `git diff --check` passed.

```bash
tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests \
  -class Hexalith.Tenants.UI.Tests.State.TenantAuditReceiptTests \
  -class Hexalith.Tenants.UI.Tests.Components.AuditEvidenceReceiptTests
tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests \
  -method '*TenantAuditPageTests.Administrative_entry_with_user_and_configuration_key_uses_the_typed_user_as_receipt_target'
```

| Correction result | Local log | SHA-256 |
| --- | --- | --- |
| typed-target-build.log | `/tmp/dw-408-474-typed-target-build.log` | `482c145f2e8f2ec638987b59404747a8000bb1f51cf8f70ccec41700dbdd3d7a` |
| typed-target-build-final.log | `/tmp/dw-408-474-typed-target-build-final.log` | `380b7c3e87d8c96c6b33d1965bf7a819a20ac47bce626052d50801a4b06f45a5` |
| typed-target-focused.log | `/tmp/dw-408-474-typed-target-focused.log` | `45cfdb3712407b2241822065df6c0d979c7d47c6a45c2edd90038fdd50b0d8cd` |
| typed-target-page.log | `/tmp/dw-408-474-typed-target-page.log` | `1ffac2b05d5ef4c3a0cb6df1fe611d9271d8569d1af0f8afe26dc7ed7084135d` |

## Raw result locations and hashes

Local raw logs are retained for review; the exact commands, counts, failed attempts and acceptance results above are preserved in this tracked artifact.

| Result | Local log | SHA-256 |
| --- | --- | --- |
| ui-build-final.log | `/tmp/dw-408-474-ui-build-final.log` | `eff60b9fe81c2af887925f22af22893de94d2cb45b740cc2dd5c50e5b946157d` |
| focused-final.log | `/tmp/dw-408-474-focused-final.log` | `271f23ca7a1a15119b413430accf08edfaab09d51f8021073a1259a2f934654f` |
| final-guard-focused.log | `/tmp/dw-408-474-final-guard-focused.log` | `4b66edca83ef24d77627460e29f1f1b55164c20b9d777d4ec9655603e745fc77` |
| timeout-retry.log | `/tmp/dw-408-474-timeout-retry.log` | `5be84511cbb646a6f95ff6f44295c86848929b511d96bf3820c2ece1db98ccad` |
| full-ui-final.log | `/tmp/dw-408-474-full-ui-final.log` | `8e97b1933a87006890db81620522190e76e3282421eb42b7f2e8f1b6b176367b` |
| browser-final.log | `/tmp/dw-408-474-browser-final.log` | `2097a6f64a61947752bad8bcc0a7d0be9e85c555d3ae716170fabdbb746a01a1` |
| gitlinks.log | `/tmp/dw-408-474-gitlinks.log` | `92a02a3cec987f68f99bb40890e1fdc4e85ab65af4a21d8fefa61ebbdb14d380` |
| diff-check.log | `/tmp/dw-408-474-diff-check.log` | `61e177977089a4c9a5a047c6f5a903524a903c616fcef02ac719f188c15beb45` |

Maintained full-run TRX: `/tmp/dw-408-474-full-ui-final/administrator_DESKTOP-VIOG240_2026-10-26_06_08_42.671600.trx`.

## Repository reconciliation checkpoint

`python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-408-440-450-460-474-audit-contracts.md` passed: no `references/` pointer changes from the spec baseline through the worktree. `git diff --check` passed (Git emitted existing CRLF-normalization notices).

Before review, the ledger and maintenance index are byte-for-byte unchanged from HEAD. All 657 numbered IDs are present and unique; the five selected IDs remain open. The root workflow must perform independent review before closing exactly these five records and reconciling to 615 done / 42 open across 38 maintenance rows. No unrelated record, historical finding or release hold is authorized to change.

## Final parent acceptance and closure — 2026-10-26

All three independent reviewers rechecked their corrections and found no concrete remaining defect. The final parent full run also qualified the preserved typed-user configuration target and complete localized-copy fixtures. No new work was deferred.

Final maintained command:

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore -p:UseHexalithProjectReferences=true -p:Version=3.128.2 -- --report-xunit-trx --results-directory /tmp/dw-408-474-root-full-ui-final
env CHROMIUM_BIN=/usr/bin/google-chrome-stable TENANTS_BROWSER_BUILD_CONFIGURATION=Debug bash tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh
```

Both commands exited **0**. The full UI suite passed **7,762/7,762**, zero failures/errors/skips/not-run cases, **1m47.023s**. The complete browser launcher passed the existing focus/audit/form scenarios, four badge EN/FR × normal/forced-color positive cases and all **16** targeted mutation rejections. This remains rendered-component/Chromium property evidence, without a human speech or new authenticated backend claim.

The parent moved `BadgeContractFixture` into its own C# file under the repository's one-type rule. The final scoped `dotnet format whitespace Hexalith.Tenants.slnx --no-restore --verify-no-changes --include tests/Hexalith.Tenants.UI.Tests/Components/BadgeContractFixture.cs tests/Hexalith.Tenants.UI.Tests/Components/StatusBadgeContractTests.cs tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.BadgeContract.cs` exited **0**. The first scoped attempt exited2 with ENDOFLINE errors in the new detail fixture; changed C# working files were normalized to required CRLF without changing their normalized Git content. The workspace loader still emits its existing unclassified warning; the scoped formatter reports no source formatting errors. No executable code changed after the successful broad gates.

[Ledger validation](dw-408-440-450-460-474-ledger-validation.json) verifies exactly these five closures: **657 unique IDs**, **615 done/42 open**, **38** exact-covering maintenance rows, all **652** unrelated record bodies unchanged and every prior selected field value preserved. The archive, sprint, dependency pins and broader release/AT holds remain unchanged. Historical checkpoints above retain their original pre-closure findings and results.

Commit-message validation used the lockfile-matching commitlint21.2.3 CLI and passed with exit0; its exact command and candidate hash are retained locally below. Push to main is explicitly authorized by the user.

| Final parent result | Local log | SHA-256 |
| --- | --- | --- |
| root-final-build | `/tmp/dw-408-474-root-final-build.log` | `e65df821d5dfef50482ab28cff653b582d9445cefabd7ce4b8fef1ce6e46037e` |
| root-full-ui | `/tmp/dw-408-474-root-full-ui.log` | `a6b39892eb610924b1bdfdd7625ca8a1e9148d6f3fbd0c750916f38089e942f5` |
| root-full-ui-final | `/tmp/dw-408-474-root-full-ui-final.log` | `a4c14af095fd6d9d57c4ed0d86c4876ec1a95c699767673f6dcc1af8fc856efb` |
| root-browser-final | `/tmp/dw-408-474-root-browser-final.log` | `ed9213e59bd1c79375c9c1d24d8db5c3ce1caabf20c0af3e928b0775e499d4ac` |
| root-format | `/tmp/dw-408-474-root-format.log` | `4fde081b85b00e3ee32c91dd4b18a8fb3875ede702f077500806c3c3247771f1` |
| root-format-final | `/tmp/dw-408-474-root-format-final.log` | `239c224f01960886368326d9ef5ebdcce071dcd0996d157bef6a963fc1eaa49c` |
| commitlint | `/tmp/dw-408-474-commitlint-evidence.txt` | `6855e2074777f6adf68e128e258fe05ccf86737c2dabf172bf1b4afa7141f764` |

Final built `src/Hexalith.Tenants.UI/bin/Debug/net10.0/Hexalith.Tenants.UI.dll` SHA-256: `5f5b29df53f38b9253b771e37929a7ee33fe96efa0cc1a62ba63f3e6d56f411d`.

Final built `tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll` SHA-256: `5d77af62d02bd4c318110e8a8e4991a3bfa31a3ae05d51050f55966edc1b3ee2`.
