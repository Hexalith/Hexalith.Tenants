# DW-308/DW-314 validation evidence

Entry: clean `main` at `e1d7ecbfaa87dabdf8a25c54fe05fb0f90ae6250`. Production composition already passed an explicit authority. The defect was the optional argument that granted `TenantOwner` or `GlobalAdministrator` when omitted, plus private resolvers that accepted `tenantStatus` and discarded it.

## Initial compile failure

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false --nologo -v:q
```

Exit 1. Log: `/tmp/tenants-dw-308/pre-test-update-build.log`. Every previous five-argument `Available` call failed with CS7036 for required `authorityState` (40 call sites; the log records each error twice, 80 `error CS` lines). No other error code appeared.

## Correction

`Available` requires `TenantConfigurationAuthorityState` and stores it unchanged. Each old call now passes `GlobalAdministrator` when its global flag is true and `TenantOwner` otherwise, which is the authority the removed default would have chosen. Private `ResolveAuthority` overloads no longer take `tenantStatus`. `tenantId` remains and is still rejected when blank. Lifecycle status is still stored on the context.

## Final execution

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false --nologo -v:q
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-class '*TenantConfigurationReadPolicyTests' --minimum-expected-tests 80 --no-ansi --no-progress --results-directory /tmp/tenants-dw-308/review-policy --report-xunit-trx --report-xunit-trx-filename policy.trx
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 6263 --no-ansi --no-progress --results-directory /tmp/tenants-dw-308/full-final --report-xunit-trx --report-xunit-trx-filename full.trx
```

Review build exit 0, zero warnings/errors (`/tmp/tenants-dw-308/review-build.log`). Policy tests: 80 passed, 0 failed, 0 skipped, exit 0. Full UI: 6263 passed, 0 failed, 0 skipped, exit 0, 2m36.440s. An earlier full run of 6263/6263, before the review expanded the same single fact, is retained at `/tmp/tenants-dw-308/full/full.trx`.

## Review

Blind review returned nine notes. The status test now also covers contributor membership, safe-model reauthorization under a global policy, and `Reauthorize(TenantDetail, ...)`. Resolver comments state that status is not an input. The detail-page stub that still passes `TenantOwner` for a non-global safe-model shape is DW-649; production already returns `MissingPermission`, and this change preserved the stub's previous meaning. No second review pass was run after the test expansion. The final policy and full UI runs above include that expansion.

## Limits

No production incident was reproduced. Production callers already supplied explicit authority, so operator-visible resolution is unchanged. No browser session, Release build, package audit, or Aspire run was required for this internal signature change. Release remains not ready. Only DW-308 and DW-314 close.
