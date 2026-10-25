# Five deferred workflow and status diagnostics corrections

Date: 2026-10-25. Closes exactly DW-121, DW-456, DW-508, DW-511 and DW-522.

The existing main-only release contract is explicit in both shared input surfaces.
Evidence self-tests reject optimized execution and distinguish intended validation
diagnostics from crashes. Applicable v1/v2 packet tampering runs in accepted and
candidate modes, preserving logical counts and historical baseline bytes. V2
acceptance hashes are rebound after mutation so stale hashes cannot mask missing
semantic checks; pending-specific metadata controls remain candidate-only.

The existing status 409 now has generated ProblemDetails metadata and a restored
scope-aware OpenAPI description. Runtime, served catalog and linked problem
reference explain direct, indexed and legacy ambiguities without disclosing matched
records. Guidance distinguishes request diagnostics from command identity and
warns against inferring success or resubmitting business commands. Lookup precedence
and publication-recovery behavior remain governed by the separate DW-521/DW-523.

## Exact validation

Commands run from their owning repository roots. The focused source build and
tests use the actual production controller, generated OpenAPI and rendered problem page.

| Command | Result |
| --- | --- |
| `python3 Tools/test-runtime-toolchain-evidence-validator.py` | Pass: 30 logical packet scenarios per each of three baselines; 48 drift controls; 165 authority controls; two historical hash pins; current 103 logical controls; SDK import 3; evaluated catalog 47. Shared packet mutations exercise both modes. |
| `python3 -O Tools/test-runtime-toolchain-evidence-validator.py` | Expected exit 1 with `G6-EVIDENCE-TESTS-INVALID: optimized Python disables required assertions`. |
| `python3 -O Tools/test_runtime_toolchain_v2.py` | Same explicit refusal, independently confirmed by reviewer. |
| `python3 Tools/test-prepare-domain-release.py` | 9/9 pass. |
| `python3 -m unittest discover -s Github/publish-containers/tests` | 150/150 pass. |
| `actionlint .github/workflows/domain-release.yml` | Exit 0. |
| `dotnet build tests/Hexalith.EventStore.Server.Tests/Hexalith.EventStore.Server.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1` | Pass, zero warnings/errors after review corrections. |
| `dotnet tests/Hexalith.EventStore.Server.Tests/bin/Debug/net10.0/Hexalith.EventStore.Server.Tests.dll -class Hexalith.EventStore.Server.Tests.Commands.CommandStatusControllerTests -class Hexalith.EventStore.Server.Tests.OpenApi.OpenApiSpecTests -class Hexalith.EventStore.Server.Tests.OpenApi.ErrorReferenceEndpointTests -xml /tmp/tenants-dw-eventstore-tests-reviewed.xml` | 61 passed, zero failed/errors/skipped/not-run. |

Two intentional mode-refusal positive controls and eight crash/output/exit negative
controls qualify the diagnostic assertion. An in-memory legacy candidate-validation
bypass is rejected specifically for a stale artifact hash. An in-memory v2 accepted
integer-validity bypass is rejected specifically by the new shared mutation loop.
No tracked production validator was mutated for these controls.

[Machine-readable results](dw-121-456-508-511-522-test-results.json) bind the tested
owner commits and Git file blobs, parsed per-class XML results, control counts and
log bundle hash. [Saved logs](dw-121-456-508-511-522-validation-logs.zip) contain
build/self-test/regression output, the focused test summary and successful exact
commit-message validations with the owning pinned commitlint CLIs.

## Independent review

Blind Hunter identified four findings. All were checked and patched: current-v2
accepted-mode tampering coverage (medium); generated 409 response description
(low); served no-resubmission guidance (low); and diagnostic fields/support
instructions in the Markdown example (low). Recheck found no new or unresolved
issues. No review finding was deferred.

## History and limits

The initial Debug source restore failed with NU1506 duplicate cryptography package
versions. Concurrent user synchronization integrated remote EventStore history and
resolved that catalog overlap; this task did not change package versions. Its
intermediate heads were `749767072b8c2ea2abc8a23540ca65471d078c49` and
`115a9ee4e137ffc88370897cbc0adfde085e8d7b`. Initial test compilation also rejected
seven new `ConfigureAwait(false)` calls with xUnit1030; those calls were removed
before the passing build/tests. Earlier failures are not counted as passing evidence.

The required runtime baseline command was
`aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --isolated --non-interactive --format Json`.
It built with zero warnings/errors, then exited 2 because the AppHost registers
`eventstore-app-api-token` twice. This is an existing topology blocker, outside the
five scoped fixes. No healthy distributed-topology or production-release claim is
made. The passing API checks run in process through the maintained test host.

[Ledger verification](dw-121-456-508-511-522-ledger-validation.json) confirms all
657 IDs, exact preservation of unrelated records, prefixed original mutable fields,
unchanged archive/sprint status and exact maintenance-index coverage. Exactly five
records close: 595 done, 62 open across 57 rows. Broader runtime, policy and release
holds remain.
