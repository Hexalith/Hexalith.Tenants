# DW-649 validation evidence

Entry: clean `main` at `b0c1e696c396ade9d59276115ca94f5917633adb`; the only initial worktree change was the draft DW-649 spec. The user selected a preview-only fixture repair on 2026-10-18.

## Finding and correction

The original DW-649 finding compared an unconditional `TenantOwner` test callback with the safe-model-only reauthorization overload, which returns `MissingPermission` for a non-global principal because a safe model has no membership evidence. The detail page passes the member-bearing overload to `TenantConfigurationManagement`, but that component declares `ReauthorizeProvider` without invoking it. Set submission instead requests a fresh preview. The production preview path reads current tenant detail and `TenantsBffComposition.ComposeSetConfigurationPreviewAsync` checks current policy, namespace, and owner membership before reporting a complete authorized preview. Existing `TenantsBffCompositionTests.Set_preview_requires_owner_membership_before_configuration_lookup` and its current-subject test cover that production boundary.

The former positive page test named `operator.alpha` as caller but put `owner-user` in its detail; its unused callback granted `TenantOwner` regardless. The revised test supplies `operator.alpha` as owner and removes the unused callback. A second theory case starts from the same eligible page, changes the preview's owner evidence before submit, and asserts a refused submit-time preview, `UnableToVerify` before dispatch, and zero command/status/projection-proof calls. An older page test's false reauthorization name/comment and unused callback setup were also corrected, with dead tracking removed from its nested test stub. No production source or authorization behavior changed.

## Initial investigation and failures

The initial reauthorization-based two-case attempt built with zero warnings/errors, then failed 0/2 with both cases waiting for a callback count of one while observing zero:

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-method '*Detail_page_set_configuration_uses_current_member_authority_before_dispatch*' --minimum-expected-tests 2 --no-ansi --no-progress --results-directory /tmp/tenants-dw-649/focused --report-xunit-trx --report-xunit-trx-filename focused.trx
```

Exit 2; `/tmp/tenants-dw-649/focused/focused.trx` retains both failures. That exploratory test edit was reverted. The first preview-based build then failed with CS0117 because the test used nonexistent `TenantCommandLifecycleState.Blocked`; `TenantSetConfigurationCommandSnapshot.Blocked` actually creates `UnableToVerify`. The assertion was corrected.

## Final execution

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false --nologo -v:q
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-method '*Detail_page_set_configuration_requires_current_authorized_preview_before_dispatch*' --minimum-expected-tests 2 --no-ansi --no-progress --results-directory /tmp/tenants-dw-649/focused-preview --report-xunit-trx --report-xunit-trx-filename focused.trx
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 6264 --no-ansi --no-progress --results-directory /tmp/tenants-dw-649/full-ui --report-xunit-trx --report-xunit-trx-filename full.trx
```

Final build exit 0, zero warnings/errors. Focused page cases: 2 passed, zero failed/skipped, exit 0. Full UI project: 6264 passed, zero failed/skipped, exit 0. The full run includes the existing production preview-composition and configuration-policy tests. `git diff --check` passed.

After the review correction, the same Debug build command passed again with zero warnings/errors. The final complete UI rerun used the same test command with `--results-directory /tmp/tenants-dw-649/full-ui-final`; it passed 6264/6264, zero failed/skipped, exit 0 (`/tmp/tenants-dw-649/full-ui-final/full.trx`).

## Review

The independent Blind Hunter review computed a five-finding floor from 24.691 kB of changed content and returned six findings. The final spec status/review evidence, superseded planning notes, an older false test comment and unused stub setup, and ledger verification links were corrected. The suggestion for a combined query-gateway-to-composer test was rejected for this selected fixture repair: the page case tests consumption of a controlled refused preview, existing composition tests test owner membership, and the limit below expressly avoids claiming an integrated gateway run. The final Debug build and full UI rerun followed these corrections.

## Limits

The page test uses a controlled preview provider to prove the page and set flow consume submit-time refusal; it does not independently rerun the server or browser. Existing production-composer tests supply the membership-boundary evidence. The unused `ReauthorizeProvider` is documented as an existing separate seam and was not wired into production by the user's explicit scope decision. Release remains not ready.

## Commit-message validation

The exact full candidate in `/tmp/tenants-dw-649-commit-message.txt` is `test(ui): align configuration preview authority fixture`. The owning repository's installed `@commitlint/cli` is pinned at `21.2.3` in `package-lock.json`. `node_modules/.bin/commitlint --edit /tmp/tenants-dw-649-commit-message.txt` exited 0 with no violations before the message was used.
