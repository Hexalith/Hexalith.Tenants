# DW-227 / DW-241 / DW-242 / DW-299 / DW-317 validation

Completed 2026-10-23 against baseline `fc0a6959844f6afde5a5141fe46c36f5a0cbc0ad`.

Exactly five records close. Member Cancel and launchers now target actual Fluent controls, current missing/denied launchers recover to a stable heading, and unsafe viewport changes retain visible focus. Configuration and lifecycle form/label CSS matches generated child markup while equal-specificity narrow rules preserve the existing safety boundary. Command identity, admission, authorization and proof rules remain unchanged.

Validation commands and results:

```sh
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true
# Passed: 0 warnings, 0 errors.

tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests -method '*CommandFormExportsLocalizedBrowserMarkup' -method '*MemberDismissalFocus*' -method '*RemovalFocusUsesVisibleInteractiveTargets' -method '*OwnedRemovalFocusAvoidsDisabledCancelAndPreservesRetryCorrection' -method '*Initial_focus_targets_visible_cancel*' -method '*LifecycleRoundTripKeepsSetAdmissionBusyUntilOldLeaseCleanupCompletes'
# Passed: 20 tests, 0 failures, 0 skips.

dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build -p:UseHexalithProjectReferences=true
# Passed: 7,496 tests, 0 failures, 0 skips.

tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh
# Passed: Chrome 154.0.8037.57, actual rendered Fluent 5.0.1 assets and compiled CSS.
# EN/FR form/warning visibility, all set/lifecycle labels, width bounds at 390/767/768/1024/1366px.
# Actual removal Cancel and native forward/reverse Tab cycles plus Enter at 767/768px.
# Missing CSS scope and missing sentinel forwarding controls reject for the expected reasons.

python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-227-241-242-299-317-responsive-command-forms.md
# Passed: no references/ pointer changes.

./node_modules/.bin/commitlint --edit /tmp/tenants-responsive-forms-commit-message.txt
# Passed: exact full candidate; installed commitlint 21.2.3 matches package-lock.json.

git diff --check
# Passed.
```

Independent blind review returned seven findings. All were corrected and independently accepted: narrow CSS specificity, retained retry confirmation focus, resize recovery, complete keyboard cycles, all-label coverage, owned-command lifecycle fallback, and accurate localization claims. The additional test synchronization correction was independently accepted. No review finding was deferred.

Earlier attempts are not hidden: a draft test used an absent ViewportTier.Unknown enum value before modeling the unmeasured state correctly; intermediate builds encountered transient MSB3026 copy retries. The first full UI run was 7,493 passed/one failed because an old assertion still cast the removed Cancel wrapper. It now checks the actual Fluent button. A later full run was 7,495 passed/one failed in the existing configuration lease-cleanup test; its synchronous Submit wrapper could finish before the real async event. Awaiting SubmitAsync preserves the original cleanup assertions and passes final focused/full checks. Browser qualification caught and repaired the initial selector-specificity regression; native Enter input was corrected to include its character event. Final results above supersede those intermediate failures.

The browser fixtures use actual production-rendered markup, compiled isolated CSS and restored Fluent assets. Component regressions establish C# callback targets; static browser event bridges establish rendered-control focus/keyboard behavior. This does not claim an authenticated live Blazor callback session, full modal/inert accessibility, a human assistive-technology test, or healthy topology. Member launcher/heading browser fixtures are English; forms and removal fixtures use shipped EN/FR resources. Broader DW-194 and unrelated runtime/release holds remain open.

`aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --no-logo` succeeded. `aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json` reported Keycloak/security Running/Unhealthy and dependent resources Waiting. `aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj` stopped the inspection run successfully. No topology or dependency changes were made.

[Ledger audit](dw-227-241-242-299-317-ledger-validation.json) preserves all 657 IDs and every original field. Exactly DW-227/241/242/299/317 change; 652 other record bodies remain byte-equivalent, and 87 maintenance rows cover the remaining 97 open IDs exactly. Current counts: 560 done/97 open. Historical findings, archive/sprint files, unrelated records and dependency pointers are preserved.

[Raw logs](dw-227-241-242-299-317-raw-logs.zip) include final build/focused/full/browser output, the observed configuration test race, gitlink and commitlint validation. [Input digests](dw-227-241-242-299-317-input-sha256.json) bind changed implementation/tests/spec and built UI/CSS inputs.
