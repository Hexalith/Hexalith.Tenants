# DW-540/DW-554/DW-570/DW-584/DW-593 validation evidence

Date: 2026-10-27. The user approved the five policies in the [implementation spec](spec-dw-540-554-570-584-593-release-governance.md) and explicitly requested a push to `main`. Exactly these five numbered records close. Fresh full generic Aspire qualification remains a separate unnumbered follow-up; broader runtime and release holds remain.

## Accepted behavior and review

| Record | Implemented and verified behavior |
| --- | --- |
| DW-540 | Tenants selects blocking generic Aspire tests with browser/TLS/Dapr prerequisites and skipped-test rejection. The TRX guard requires a successful summary and consistent passing counters. Other reusable callers retain their defaults. |
| DW-554 | Only `Hexalith.Tenants.Server` may consume the two exact approved technical ServiceDefaults IDs. Other known and unknown packages refuse them; dependency range and framework checks remain. |
| DW-570 | Standalone release/recovery evaluate the bounded static workflow/action/helper closure with the actual defining job identity and immutable Git blobs. Reusable identity enforcement and standalone Trusted Publishing jobs remain. |
| DW-584 | Release/recovery require the latest exact-source `main` push run and attempt for `ci.yml`, `story-guards.yml` and `source-reference.yml` to succeed. A final refresh refuses interleaved newer runs/attempts. Normal publication checks all three again at its final admission boundary. |
| DW-593 | Recovery resolves execution tooling from the immutable source publication declaration and checks checkout identity, helper bytes and retained provenance throughout preparation/publication. Source build props retain their source gitlink. Installed publisher changes and post-build helper changes are refused. |

Three independent reviewers examined the implementation through blind, edge-case and verification-gap lenses. The spec retains all 12 individual finding verdicts before duplicate grouping. Actionable findings were patched and rechecked: defining job identity, latest-run freshness, final normal publication admission, installed publisher bytes, post-publication-build helper drift, structural environment parsing, strict TRX results, safe API diagnostics and executable actual-workflow controls. The proposed interpreter-transitive shell closure was outside the approved bounded static closure contract. No policy changes or live publication were needed to resolve the findings.

## Revisions and concurrent work

The original comparison baseline remains `2cf3a9660f71f6877923772cd145aa6856907945`. A concurrent external `/pushall` session recorded the implementation, integrated upstream Builds changes and advanced unrelated pointers. Its public parent `ff8c9a6273f70754c50a53b46dd4b4ff6fc2828b` was followed by `dbd034be94d094c74ed2ee2d4cfe0c601cc18f70`, preserved as the closure parent. No history was rewritten by this task.

The final shared execution revision is **`35f185abdec37a3ae92d82f85a21e0f3c971d82d`**, already present on Builds `main`. All three publication execution declarations match it. The original local shared commit `ad395b72f0639951d4bb2af91a22eba7fdb95207` was externally integrated as `568417aebb4f517e9f46d9c60f8564b0d8f7503f`; the final shared head contains the review corrections and upstream changes. Original exact-message commitlint evidence is retained.

Only Builds implementation changes are attributed to this task. The other five changed pointers belong to concurrent work and remain intact. The spec declares all six baseline-to-target pointer changes; `final-preserved-state.json` records all nine current gitlinks. The late EventStore advance from `4dc256c4db7a0ba88efdbbe0f6bcbcf047c09d34` to `4e4a7e9b8fcc8cb6fb47107d916489ada7c08cfc` changes published-compatibility tests/story documentation, with no compiled production input changes to the passing source build.

Two inherited fixture assumptions were corrected during verification: the FrontComposer dependency-routing fixture now isolates unavailable Memories source, and the Tenants Dockerfile inventory excludes the Platform/McpCli root reference owners consistently with the other reference owners. Production behavior is unchanged by these fixture corrections.

## Final verification

Commands run from the Tenants root unless the table names Builds as the working directory. Raw final outputs, conservation snapshots and exact validated commit messages are included in [the validation archive](dw-540-554-570-584-593-raw-validation.zip). Negative-control diagnostics in passing Python suites are expected refusals.

| Command | Result | Raw log |
| --- | --- | --- |
| `dotnet build tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -p:UseNuGetDeps=false -m:1 -nr:false` | Exit 0; 0 warnings, 0 errors | `final-contracts-source-build.log` |
| `dotnet tests/Hexalith.Tenants.Contracts.Tests/bin/Debug/net10.0/Hexalith.Tenants.Contracts.Tests.dll` | 718 passed, 0 failed, 0 skipped | `final-contracts-full.log` |
| `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/scripts -p 'test_*release*.py'` | 70 passed; includes executable final normal-release admission, structural bootstrap and both actual workflow snapshots | `final-release-controls.log` |
| Builds: `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s Github/governed-provenance/tests -p 'test_*.py'` | 59 passed: 43 provenance, 11 workflow gates, 5 execution identity | `final-shared-provenance-controls.log` |
| Builds: `PYTHONDONTWRITEBYTECODE=1 python3 Github/publish-containers/tests/test_oci_registry_validator.py` | 12 passed | `final-shared-oci-controls.log` |
| Builds: `pwsh -NoProfile -File Tools/test-domain-workflow-test-platforms.ps1` | 371 assertions passed, including integrated upstream test hooks | `final-shared-test-platforms.log` |
| `PYTHONDONTWRITEBYTECODE=1 python3 tests/scripts/test_nuget_dependency_ranges.py` | 11 passed | `final-package-policy.log` |
| `actionlint .github/workflows/ci.yml .github/workflows/release.yml .github/workflows/recover-partial-release.yml` | Exit 0 | `final-parent-actionlint.log` |
| Builds: `actionlint .github/workflows/domain-ci.yml .github/workflows/domain-release.yml` | Exit 0 using the owning repository's existing tracked configuration | `final-shared-actionlint-owned.log` |
| `bash -n` individually for `partial-release-recovery-common.sh`, `validate-partial-release-recovery.sh`, `prepare-partial-release.sh`, `publish-partial-release.sh`, `validate-publication-preflight.sh` in `scripts/` | All five exit 0 | `final-shell-syntax.log` |
| `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-540-554-570-584-593-release-governance.md` | All six changed pointers declared; exit 0 | `final-story-gitlinks.log` |
| `python3 check-ledger.py` against the retained initial snapshots | All 657 IDs retained; 629 done/28 open across 24 index rows; all 652 unselected bodies and every original selected field preserved | `final-ledger-conservation.json` |
| `git diff --check` | Exit 0 | `final-whitespace.log` |
| `node node_modules/@commitlint/cli/cli.js --edit /tmp/tenants-release-governance-z5n_5u2_/tenants-commit-message.txt --verbose` | Exact full final candidate passes the pinned CLI with 0 problems/warnings | `tenants-commitlint.log` |

The ledger audit compares the original numbered records byte-for-byte for all 652 unselected IDs, checks historical selected lines and requires the maintenance rows to cover every remaining open ID exactly once. Original findings and dependency history are retained with `pre-release-governance-` fields. No archive or sprint-status changes are part of this closure.

## Remaining qualification limits

The following exact package-mode command exits 1:

```sh
dotnet build tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Release -p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true -p:HexalithFrontComposerFromSource=false -warnaserror -m:1 -nr:false
```

`final-contracts-package-build.log` retains 10 CS0234/CS0246 errors in `TenantCatalogDeclarations.cs`: the published EventStore contracts lack the routing namespace/types, including `CommandIntentDeclaration`, `RouteContractDeclaration` and `IdempotencyDescriptorField`. This existing source/published-package mismatch prevents a fresh full package-mode generic Aspire qualification. The separate unnumbered ledger/index follow-up remains open. The passing source contract suite and workflow/refusal controls do not claim a live generic CI run or establish broad release readiness.

Actionlint 1.7.12 lacks the supported defining-job workflow fields in its schema. Running the shared files from the parent without Builds' owning configuration reports 11 occurrences of those fields after upstream integration (`final-shared-actionlint.log`). Builds' already tracked `.github/actionlint.yaml` scopes the schema exception to those properties; the owning lint command above succeeds. No lint configuration was changed for this task.

No release/recovery workflow was dispatched, package/container published, credential policy changed or nested submodule initialized. Existing production approvals, current-main checks, artifact/receipt verification and credential sequencing remain required.

Validation archive SHA-256: `388fd11fb352f48fd4f9e83316522fb1324d62fe90926b7255db4a2d06bf2884`. Its `manifest.json` records the byte count and SHA-256 of each retained input/log.
