# Administrator grant surface validation

Date: 2026-10-22. Entry revision: `f0792c44f98b4dd9492020e1bb6b108e84dc12e7`.

The scoped batch resolves DW-346, DW-369 and DW-374, and closes DW-377/DW-402 as preserved historical grant-scope notes. The latter two were explicitly accepted attribution notes with no requested maintenance fix. Their original fields remain under `pre-grant-surface-`; the approved grant CSS and current 108 required localization keys are unchanged.

Grant preview Cancel now uses the existing Fluent V5 `FluentButton`, `OnClick`, outline appearance and its `.Element` focus target. The existing component test still drives the actual sentinel callback, Escape, Cancel and launcher restoration with zero commands. New EN/FR actual-control exports feed a Chromium fixture checking visible focus on the rendered host and computed border-box chrome at measured 768px and 1024px. The browser fixture simulates the sentinel focus forwarding; actual Blazor callback/dismissal is covered by the component suite. It does not qualify real keyboard Tab trapping, authenticated topology, screen readers or other browsers; those existing deferred gates remain open.

The final redispatch-refusal regression starts with an actually submitted ambiguous grant and retained dispatch-marked lease. It reuses the test gateway's capability provider to model success at the outer read followed by failure at the final renderer boundary. Direct private-boundary invocation deliberately avoids an event-triggered rerender that could conceal a silent return through `OnAfterRenderAsync` repair. The test requires withdrawal, assertive recovery, the same preview/message identity and lease, no dispatch in flight, and no extra gateway submission. Removing only the inner `SurfaceAmbiguousGrantRetryUnavailable` call makes that one test fail: `after.IsDeliveryRetryWithdrawn should be True but was False`. Original source bytes are restored in a `finally` block before rebuilding.

Actual EN/FR replacement pages adopt a previously withdrawn ambiguous command while preview readiness remains blocked. They require the disabled retry and its associated localized reason/recovery, the same target, baseline, message ID and lease, no automatic delivery, and same-ID manual retry after readiness recovers. This qualifies recomputation of the presentation-only flag; no durable presentation field or production retry policy was added.

## Validation commands and results

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 -v:q
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --filter-class '*GlobalAdministratorsPageTests' --minimum-expected-tests 1 --results-directory /tmp/dw-grant-focused
dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -method '*GlobalAdministratorsPageTests.InnerGrantRedispatchRefusalSurfacesRecoveryWithoutAnEventRender'
```

The focused maintained lane passes **281/281**, zero failures/skips, 22.537s. Initial Debug build passes with zero warnings/errors (17.90s); the complete fixture build passes with zero warnings/errors (11.70s). The direct-assembly invocation above is the deliberately failing silent-return control, not acceptance evidence. Its mutation build passes, test exits 1 with exactly one assertion failure, then the restored Debug build passes with zero warnings/errors (13.48s).

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --minimum-expected-tests 1 --results-directory /tmp/dw-grant-full
bash tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh
python3 _bmad-output/implementation-artifacts/dw-346-369-374-377-402-verify.py
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-346-369-374-377-402-grant-surface.md
bash -n tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh
git -c core.safecrlf=false diff --check
```

The first story-gitlink invocation correctly failed because the new spec omitted `baseline_commit`; adding the canonical entry revision resolves it, with no references pointer changes. The first conservation run exposed an inherited maintenance-index inconsistency: DW-507/DW-509/DW-510/DW-513/DW-518 were already done at entry, but their rows and the current counters still treated them as open. This batch removes those five stale rows without modifying their ledger records. Exactly the selected five record bodies change; all 657 IDs and all 652 unrelated bodies remain identical. Current counts are **490 done / 167 open across 157 index rows**. The checker enforces selected-field historical preservation, exact current dispositions, historical preamble/index narrative preservation, exact open-row coverage and unchanged dependency pointers.

The maintained full UI suite passes **7,163/7,163**, zero failures/skips, 1m47.967s. The maintained Chromium lane exits 0 using Chrome 154.0.8037.57 and restored Fluent 5.0.1. All four new grant-control culture/width checks pass, alongside the existing focus, identity, responsive, forced-color and accessibility-name checks and harmful controls. Independent-review corrections and recheck are recorded below. This evidence does not accept deployment or release readiness.


The exact LF-terminated Conventional Commit candidate passes the installed pinned commitlint CLI with zero problems/warnings:

```text
fix(ui): resolve five deferred administrator grant records
```

```bash
node_modules/.bin/commitlint --verbose < /tmp/dw-grant-commit-message.txt
git fetch origin main
git rev-list --left-right --count HEAD...origin/main
```

The first fetched comparison is `0 0` before commit. The local AppHost was already running; pre-change Aspire inspection found all 23 resources healthy. This is a runtime baseline observation, not an authenticated browser acceptance claim.

The tracked [transcript](dw-346-369-374-377-402-validation-transcript.txt) preserves command output, with repository/NuGet prefixes normalized and trailing whitespace removed. The [run manifest](dw-346-369-374-377-402-run-manifest.json) records accepted source/assembly identities and original local log hashes. These checks use substituted component services and make no live authentication or release-readiness claim.


## Independent review corrections

All nine findings are triaged in the spec and patched. The selected-record checker now reconstructs the exact original block and rejects four injected immutable-field changes. The replacement regression removes the real page through renderer-owned child content and remounts a distinct page instance. It pins both submitted command records and requires zero correlationless status calls across adoption, rearm and retry. Final refusal pins rendered assertiveness and recovery. Browser assertions now require opacity/CSS visibility and containment of the focused control in the modal/viewport, and the runner rejects omitted focus forwarding and transparent controls. Actual EN/FR grant markup, harness, page CSS, Fluent JS/CSS and UI runtime assembly identities are emitted with the browser results.

The reviewed Debug build passes with zero warnings/errors (12.73s); the reviewed maintained administrator-page lane passes **281/281**, zero failures/skips, 25.644s. The exact reproducible control is:

```bash
python3 _bmad-output/implementation-artifacts/dw-346-369-374-377-402-check-refusal.py --log-directory /tmp/dw-grant-review-control
```

The script builds the unique inner-call deletion, requires the intended withdrawal assertion to fail, restores identical source bytes and rebuilds the maintained assembly even if validation raises. Its command logs preserve which assembly was tested. The earlier standalone test command is only the test phase of this complete procedure.


The tracked refusal-control script exits 0: its one-case mutated run fails the intended assertion, source SHA-256 restores to `3470a0ea06cedca07570a99c6763be60ebf7f8d48f817c8a69e3c74688356279`, and the restored build has zero warnings/errors (12.00s). The earlier source/assembly map remains historical and will be kept separately from reviewed-run identities.

Final reviewed maintained full UI run: **7,163/7,163 pass**, zero failures/skips, **1m52.522s**, using the full command above with `--results-directory /tmp/dw-grant-review-full`. The initial full run remains historical evidence from before the strengthened test assertions. Independent recheck accepts all nine corrections and finds no remaining concrete problem, subject to final browser execution and evidence capture.


Final strengthened Chromium lane exits 0: actual EN/FR grant Cancel focus and visible bounds pass at measured 768px/1024px; suppressed forwarding and transparent-control mutations fail the required focus assertion. Existing removal, identity, correction, responsive, forced-color and accessible-name scenarios continue to pass. All actual grant input hashes appear in the [reviewed transcript](dw-346-369-374-377-402-reviewed-validation-transcript.txt). The run manifest preserves initial acceptance identities and their original transcript separately from the final reviewed source/assembly identities and transcript.

The final conservation checker passes its four immutable-field controls and all 657-record/index checks; story gitlinks, shell syntax, whitespace and pinned commitlint 21.2.3 pass. Final pre-commit fetch still reports `0 0` divergence. All nine review findings are addressed; no new work is deferred by this batch. Historical browser, runtime and release limitations remain in force.

Final evidence recheck reports no remaining concrete issue: all nine findings resolve, reviewed browser input/source/assembly/log/transcript hashes match, historical evidence remains distinct, and the conservation controls pass.
