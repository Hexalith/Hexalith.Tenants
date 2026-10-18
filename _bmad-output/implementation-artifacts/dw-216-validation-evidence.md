# DW-216 validation evidence — 2026-10-18

Entry commit: `43234d937ae0b3bc32c2837db38ea403b64b2742`. Scope: registry version validation in the existing unprotected release-floor guard. No registry publication or hosted release runs as part of this correction.

## Contract and compatibility

The [flat-container API contract](https://learn.microsoft.com/en-us/nuget/api/package-base-address-resource#response) defines normalized version strings without build metadata. [NuGet normalization](https://learn.microsoft.com/en-us/nuget/concepts/package-versioning#normalized-version-numbers) removes numeric leading zeros and zero revisions. This repository's previously approved floor spec deliberately accepts explicit zero revisions, and the existing validator also accepts two-component versions and uppercase prerelease identifiers. Those compatibility aliases remain supported; this correction does not claim strict equality with every canonical flat-container spelling.

[NuGet core parsing](https://github.com/NuGet/NuGet.Client/blob/dev/src/NuGet.Core/NuGet.Versioning/NuGetVersionFactory.cs) rejects core components above `2147483647`. [NuGet prerelease validation](https://github.com/NuGet/NuGet.Client/blob/dev/src/NuGet.Core/NuGet.Versioning/SemanticVersionFactory.cs) requires nonempty ASCII alphanumeric/hyphen identifiers and forbids leading zeros only in entirely numeric identifiers. Numeric prerelease identifiers have no Int32 bound. Valid `rc-001`, `01a`, hyphen-only labels, uppercase labels and long canonical numeric labels remain excluded as prereleases. The independent semantic-release tag bound is unchanged.

## Reproduction and focused verification

The new tests execute the actual workflow Bash/jq through `RunTagFloorGuardAsync`, using stubbed GitHub and registry responses. Malformed values are exercised both alone and alongside a covered stable version and a valid prerelease. Invalid-evidence assertions require the existing safe-validation diagnostic, no success output and no drift misclassification. This reproduces local guard behavior, not a production release incident.

The initial Debug source build exits **0**, zero warnings/errors. Against unchanged production, the new tests exit **2**: **116 total, 64 failed, 52 passed, zero skipped**. With complete validation before prerelease exclusion, the same tests exit **0**: **116/116 passed, zero failures/skips**.

```bash
dotnet build tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Debug -m:1 -nr:false -p:UseHexalithProjectReferences=true > /tmp/dw-216/red-build.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Debug --no-build --no-restore --filter-method '*PackageGovernanceTests.ReleaseRegistry*' --minimum-expected-tests 116 --no-ansi --no-progress --results-directory /tmp/dw-216/red --report-xunit-trx --report-xunit-trx-filename focused.trx > /tmp/dw-216/red-tests.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Debug --no-build --no-restore --filter-method '*PackageGovernanceTests.ReleaseRegistry*' --minimum-expected-tests 116 --no-ansi --no-progress --results-directory /tmp/dw-216/green --report-xunit-trx --report-xunit-trx-filename focused.trx > /tmp/dw-216/green-tests.log 2>&1
actionlint .github/workflows/release.yml
git diff --check
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-216-registry-version-validation.md
```

Actionlint, whitespace and story-gitlink checks pass. Git emits its existing CRLF-to-LF checkout advisory for the C# file; the file retains CRLF, while YAML/Markdown retain LF. No gitlink changes are present.

## Aspire baseline and limits

`aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json` starts the exact Tenants AppHost. `aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json` exits **0**: security is unhealthy and the domain services are waiting; Memories resources are healthy. `aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj` exits **0**, cleaning up only the instance started for this check. This establishes a pre-code resource snapshot, not green distributed-runtime acceptance. No browser check is required for the isolated workflow validator.

## Final acceptance

The full owning Contracts suite exits **0**: **319/319 passed, zero failures/skips**, including all 116 focused cases and the prior release-tag/four-component floor regressions.

```bash
dotnet test --project tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 319 --no-ansi --no-progress --results-directory /tmp/dw-216/full --report-xunit-trx --report-xunit-trx-filename full.trx > /tmp/dw-216/full-tests.log 2>&1
```

Independent Blind Hunter review returned four findings, all triaged in the spec. The remaining runtime gap was a missing overall version-length check: [NuGet.org limits the version field to 64 characters](https://learn.microsoft.com/en-us/nuget/reference/nuspec#version). Added alphabetic/numeric 64-character positive controls and 65-character isolated/mixed negative controls, plus non-string JSON values, invalid/above-floor ordering and genuine stable drift alongside a valid prerelease.

The review build exits **0**, zero warnings/errors. Before the length correction, the expanded focused matrix exits **2**: **130 total, four failed, 126 passed, zero skipped**. All four failures are the 65-character cases; the other review controls pass. After the length correction, focused tests exit **0**, **130/130 passed**, zero failures/skips. Numeric suffixes remain free of an Int32 component bound within the overall NuGet.org length limit.

```bash
dotnet build tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Debug -m:1 -nr:false -p:UseHexalithProjectReferences=true > /tmp/dw-216/review-build.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Debug --no-build --no-restore --filter-method '*PackageGovernanceTests.ReleaseRegistry*' --minimum-expected-tests 130 --no-ansi --no-progress --results-directory /tmp/dw-216/review-red --report-xunit-trx --report-xunit-trx-filename focused.trx > /tmp/dw-216/review-red-tests.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Debug --no-build --no-restore --filter-method '*PackageGovernanceTests.ReleaseRegistry*' --minimum-expected-tests 130 --no-ansi --no-progress --results-directory /tmp/dw-216/final-focused --report-xunit-trx --report-xunit-trx-filename focused.trx > /tmp/dw-216/final-focused-tests.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 333 --no-ansi --no-progress --results-directory /tmp/dw-216/final-full --report-xunit-trx --report-xunit-trx-filename full.trx > /tmp/dw-216/final-full-tests.log 2>&1
```

The final full Contracts suite exits **0**, **333/333 passed**, zero failures/skips. Independent recheck confirms all four findings resolved and reports a separate **17/17 jq probe** pass; that report supplements the locally recorded real-shell suite rather than replacing it. No review deferral remains.

Only DW-216 closes. All 653 ledger IDs remain: **365 done, 288 open**, represented by **260 maintenance rows** that cover every open ID exactly once. Every replaced DW-216 field is retained under `pre-dw-216-*`. Unrelated ledger bodies and historical closure notes, archive/sprint, package manifests/lockfile, release configuration, inventory, dependencies and existing release readiness holds remain preserved. This correction does not establish release readiness.

The repository-pinned and installed commitlint CLI versions both equal **21.2.3**. `node_modules/.bin/commitlint --edit /tmp/dw-216/commit-message.txt --verbose` exits **0**, zero problems/warnings, validating the exact full candidate `fix(ci): validate registry versions before prerelease exclusion`. The successful output is retained in `/tmp/dw-216/commitlint.log`. The invoked workflow requires a local commit; no push or publication is performed.

The contributor formatter check was also attempted:

```bash
dotnet format whitespace tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --no-restore --include tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs --verify-no-changes > /tmp/dw-216/format.log 2>&1
```

It exits **2**, reporting **172 WHITESPACE diagnostics on unchanged lines**, primarily existing brace placement. Comparing each reported line with the added-line intervals from `git diff --unified=0` finds **zero diagnostics in added lines**. This file-wide formatting blocker remains visible; unrelated existing formatting was preserved. The source build, all 333 owning tests, actionlint, whitespace-diff check and story-gitlink check pass independently.
