# Async ownership validation — 2026-10-23

Entry revision: `907029802377ab8e80c20e894f27c6ca84360478`. Scope: exactly DW-180, DW-266, DW-473, DW-503 and DW-574.

## Behavior and review

- DW-180: replacement retires the previous list-load generation before synchronous cancellation callbacks. The regression reads generation at that boundary, drains the old operation while replacement is held, and checks retained confirmed evidence.
- DW-266: the existing context-local callback flag suppresses causal reentry and preserves independent catch-up. A component-wide boolean is rejected by both add/change negative controls. The intentional existing policy is documented rather than replaced.
- DW-473: existing captured create intent/generation publication passes callback-free list/detail dismissal with late success/failure. The real workspace callback withholds dismissal during held proof, preserves unresolved admission, and never claims confirmation from missing proof.
- DW-503: status and delivery capture original message, intent and projection target. Renderer publication consumes retained truth; exact update notifications reach remounts. Unique guards are acquired with renderer ownership checks and retired per generation, so obsolete delivery/status cannot claim or clear successor busy state. Query-gateway fallback and independent successor lookup are qualified.
- DW-574: the real module factory binds retention to literal authenticated subjects. Pending/faulted identity, case changes, authorized replacement and sign-out withhold private prior intent. Hidden completion/expiry preserve exact lease policy. Held delivery/status and navigation/remount cases remain private; original-owner and delayed same-caller recovery keep exact identities.

The independent reviewer returned nine findings, individually triaged in the spec. Eight verification/implementation gaps were patched. The claimed parameterless-constructor bypass is false for the shipped host, whose Program uses the explicit module factory; legacy isolated-fixture constructors remain compatible. The production registration has its own regression. Rechecks caught and cleared the status-guard reacquisition window and confirmed the final fixture corrections. No new deferred record was created.

## Verification

The final Debug source build (`build-verified-final`) passes with zero warnings/errors. The final focused run passes **680/680**, and the complete UI project passes **7,434/7,434**, with zero failures, skips or not-executed cases. All 680 focused case names occur in the passing full TRX. All 15 changed source/test hashes and both assembly hashes remain unchanged since this final build.

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -m:1 -nr:false

dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -class '*TenantsWorkspaceTests' -class '*AddTenantMemberFlowTests' -class '*ChangeTenantMemberRoleFlowTests' -class '*CreateTenantFlowTests' -class '*CorrectionStartPanelTests' -class '*TenantAuditPageTests' -class '*TenantCorrectionAttemptCallerTests' -class '*TenantCorrectionAttemptTrackerTests' -parallelMode none -result-trx /tmp/tenants-async-ownership/focused-verified.trx

dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug --no-build --no-restore --parallel none --minimum-expected-tests 7434 --report-xunit-trx --report-xunit-trx-filename results.trx --results-directory /tmp/tenants-async-ownership/full-final

python3 _bmad-output/implementation-artifacts/dw-180-266-473-503-574-verify.py
```

Three negative controls fail as required: the original load ordering observes generation 2 inside cancellation instead of a retired generation; component-wide add/change callback booleans perform one status lookup instead of the required two. Their build passes, all three cases fail, and exact candidate source bytes are restored before the final positive build.

Initial evidence is retained separately: CS1061 from two obsolete bUnit API calls; CS0103 from an overly broad fixture edit; CS7036 from a missing fixture constructor argument; the initial 671-case runs with 3 and 1 fixture failures; one stale-binary 671-case run started before its build completed and is invalid as final evidence; and the first 680-case run with four fixture failures. Those four were a scheduler-dependent EventCallback completion proxy, incompletely updated replacement identity inputs, an immediate post-click component lookup, and an old expectation that remounted state remains stale. Corrections retain generation, exact identity, rendered recovery and dispatch/status-count assertions. No production source was weakened to make those fixtures pass.

## Preservation and limits

Aspire start/describe found all 23 resources Running/Healthy before edits; the AppHost stopped successfully. Only sanitized name/state/health data is retained. This is application-baseline evidence, not new authenticated browser or assistive-technology acceptance.

The closure verifier requires all 657 IDs, exactly five selected record changes, lossless reconstruction of selected historical fields, and unchanged bodies for all 652 unrelated records. The maintenance index exactly covers 122 open IDs across 112 rows: 535 done, 60 actionable, 15 blocked, 23 awaiting-decision, 6 accepted-limitation and 18 uncertain. Archive, sprint, dependency pointers and release-readiness holds remain unchanged.

Story gitlink and whitespace checks pass. The exact full commit message passes pinned commitlint 21.2.3 with zero problems/warnings. The pre-commit fetch reports zero divergence from origin/main. [Raw command arrays, logs, negative controls, TRX reports, sanitized Aspire state, hashes and validated commit-message bytes](dw-180-266-473-503-574-raw-logs.zip) preserve the executed evidence.
