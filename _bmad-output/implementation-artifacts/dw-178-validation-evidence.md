# DW-178 validation evidence — 2026-10-20

Entry revision: `54bfac8da66b60aa8aa19a77d23676a7662113b4`, clean main. The user requested the next deferred work and authorized commit/push on main. DW-178 is the next dependency-free actionable Tenants record after DW-177; lower open records retain their existing ownership/dependencies/dispositions.

## Decision and scope

The ledger offers two resolutions: short-circuit unchanged authority scope, or document why every parameter update requires a fresh resolve. This change takes the latter. `OnParametersSetAsync` reloads projection evidence even when only the return context changes. Tenant ID and authentication epoch alone do not prove that the composition's current authority answer is unchanged. Preserve the existing fresh read, its pending Indeterminate state, replaceable cancellation/generation protection, and independent submit-time reauthorization. A source comment records that relationship. No runtime behavior, public API, localization, topology or dependency change is introduced by this work.

Six new rendered component cases exercise repeated same-tenant parameters and actual query-string navigation, granted/revoked/indeterminate completion, pending fail-closed behavior, follow-up recovery and existing superseded-resolution coverage. They reuse the existing fixture and assert the child authority parameter plus rendered Fluent action availability. The pre-existing authentication/supersession and command-time reauthorization regressions remain part of the full suite. This is component evidence; no new browser, assistive-technology, deployed-authorization or revocation guarantee is claimed.

## Commands and initial results

Run from the owning repository root. Logs are under `/tmp/dw178/` except the initial baseline/Aspire logs, which use `/tmp/dw178-` prefixes.

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 -v:q
```

The initial build (`/tmp/dw178-build-baseline.log`) exits 1, zero warnings/one error: `TenantReadAuthorityResult.cs(12,5): CS0246 NativeSourceProof could not be found`. This is the existing unrelated source dependency mismatch from `75627d267296d26a9f4fee05fa0495deb110fc11`. During investigation another actor advanced root dependency checkouts. The next three ordinary source builds (`build-tests.log`, `build-focused.log`, `build-corrected.log`) and the qualified-test build (`build-qualified.log`) each exit 0 with zero warnings/errors. These are source-checkout results, not qualification of the originally declared dependency pins. This work does not stage or commit those pointer changes.

All test commands use this maintained project-runner prefix:

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --no-ansi --no-progress --report-xunit-trx --report-xunit-trx-filename results.trx
```

Append `--results-directory /tmp/dw178/<directory>` and the filters below. Read counts from the resulting TRX, rather than relying only on exit status.

| Directory | Extra arguments | Result |
| --- | --- | --- |
| `focused` | `--filter-method '*SameTenantParameterLoad*' --minimum-expected-tests 7` | Exit 2: one passed/six failed/zero skipped. bUnit refuses direct assignment of SupplyParameterFromQuery; use actual NavigationManager query navigation. |
| `focused-corrected` | Same filter/minimum | Exit 2: one passed/six failed/zero skipped. The ready fixture lacked the lifecycle projection version required for actionable state. Supply the existing `tenant-sequence:41` marker. |
| `focused-qualified` | Same filter/minimum | Exit 0: seven passed/zero failed/skipped. |

## Aspire baseline

```bash
aspire ps --format Json
aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --isolated --format Json --non-interactive
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire otel logs --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --severity Error --limit 5 --format Json
aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --non-interactive
```

Initial ps is empty. Start succeeds; initial describe includes Waiting resources and unhealthy security. The final describe reports all 23 resources Running/Healthy. Inspect the bounded error sample locally; do not copy login credentials or tokens into tracked evidence. The started app stops successfully. This establishes the resource baseline, without claiming distributed functional qualification.

## Git ownership

The initial required default own-spec gitlink check exits 1 because another actor advanced dependency checkouts after entry. Its exact output is retained in `/tmp/dw178/gitlinks-initial.log`. Do not restore, stage or attribute those changes to DW-178. The committed-range check and staged-path inspection must separately establish that the DW-178 commit does not move dependency pointers. Existing release qualification holds remain in force.

## Final qualification

Pending the restored build, full UI suite, scoped formatting, independent review and exact maintenance reconciliation. No closure is claimed by this initial evidence draft.

The experimental seventh same-tenant late-result test failed to reject removal of the generation guard (control exit 0). A direct-substitute experiment then produced an unrelated null composition fault; after repairing that fixture, the guard-removal control again passed. Remove the unproven test and experimental fixture hook rather than claiming qualification. The existing supersession regression is preserved. The cache-control replay rejected all six retained cases (plus the now-removed seventh); this is diagnostic evidence, not a final maintained seven-case result. Final qualification below uses exactly the six retained tests.

Concurrent external commit `a83021d9efa259c1afa014078d2c5fcf61c4d42c` subsequently recorded all six checkout moves (including Platform) and captured the in-progress DW-178 files. Preserve the commit. The final source validation therefore uses the now-committed updated dependency pins; the initial missing-type failure remains historical evidence. The spec File List and full target table declare all six inherited moves from the true story-entry baseline. The follow-up completion commit must contain only the owned remaining files and no gitlink changes.
