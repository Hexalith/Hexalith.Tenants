---
title: 'Close five source and package provenance verification gaps'
type: 'bugfix'
created: '2026-10-21'
status: 'done'
route: 'oneshot'
baseline_commit: '2cd3cc270e88d3cdbb47a1a938fd6a0951b6f0d4'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Five deferred records leave source and package provenance checks weaker than their claims. A source-reference four-case floor may count skipped tests, workflow substring checks cannot bind flags to executable commands, build-only EventStore edges lack metadata regression checks, and the Memories model test does not establish the mode of the AppHost it inspects.

**Approach:** Resolve exactly DW-344, DW-381, DW-393, DW-394 and DW-395 as one provenance verification batch. Qualify the current skipped-test behavior with an isolated forced-skip control and enforce all four provenance passes; parse the workflow structure and validate actual commands; qualify build-only edge metadata; and bind the Memories model result to explicit package-mode evidence. Preserve unrelated ledger records, dependency pins and release holds. Run focused checks and independent review, record reproducible evidence, validate the Conventional Commit, commit and push to main as requested.

</frozen-after-approval>

## Implementation Notes

- No intent gaps or new public API. Changes are confined to existing repository-specific tests, their build metadata, the source-reference workflow and closeout evidence. All edits are reversible; commit/push are explicitly authorized. This qualifies for the skill's oneshot path.
- DW-393 duplicates DW-381; both require executed forced-skip evidence with the pinned runner.
- Code map: `.github/workflows/source-reference.yml` contains the Debug source build and four-case MTP invocation; `tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs` has the substring check, EventStore reference enumeration and XML metadata helpers. `src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj` owns three build-only EventStore host edges and one compile-time Aspire helper edge. `tests/Hexalith.Tenants.IntegrationTests/MemoriesSecretStoreResourceGraphTests.cs` inspects the model without starting resources.
- Authoritative constraints: root/project guidance requires serialized builds, Debug source validation locally, explicit package validation when qualifying that graph, warnings as errors, no nested submodule operations and no shared-platform source edits.
- Given four passing provenance cases, the source lane succeeds; given one skipped case or misplaced/commented/wrong command flags, it fails. Given any build-only edge with leaked assembly/private/resource metadata, governance fails. Given a source-mode AppHost, the package claim must fail rather than silently count as package evidence.

- Validation exposed the pre-existing package IntegrationTests build failure: the pinned EventStore package lacks `NativeSourceProof` used by Tenants.Contracts. No dependency pins or product contracts change. The Memories model test moves from IntegrationTests to the existing blocking AppHost.Tests project, whose graph only needs AppHost and can independently qualify the required package model. This is the focused fallback required by repository guidance; the broad blocker remains explicit.
- Forced-skip reproduction: original four-case floor exits 0 with three succeeded/one skipped. The pinned MTP adapter needs `--fail-skips on`; the same control then exits 2 with one failed skip. The restored source's bytes match its entry snapshot, and the final workflow rebuild passes with zero warnings/errors and 4/4 tests.
- Model qualification: the original model regression moves into the existing blocking AppHost.Tests project. All nine package-mode tests pass; final source-mode negative control fails the package assertion with all three source-mode values recorded. AppHost metadata changed without changing the resource model; the skill restarted Aspire and all 23 resources report Running.
- Contracts verification: focused YAML 52/52 and package governance 234/234 are included in full Contracts 630/630, zero failures/skips. A string-assertion overload compile error was corrected; the first stale-assembly filter and initial invalid bare skip switch are retained as non-acceptance evidence. Relevant full-project/source cases total 643 successes.
- Ledger replay proves exactly the selected five bodies changed, all historical fields and all 652 unrelated bodies remain preserved, and the 207 open IDs are covered exactly once by 189 index rows. Archive, sprint and dependency pointers remain unchanged. Evidence and raw logs are colocated under the DW-344-381-393-394-395 prefix.

## Review Triage Log

Blind Hunter inspected 201,535 changed bytes: 201.535 kB; N = min(floor(sqrt(201.535) + 1), 10) = 10. Every finding was checked against the cited rule or saved evidence. All ten are bounded patches; none is deferred.

1. **medium / patch:** Later authored ProjectReference Update can override a checked edge. The AppHost declaration contract now rejects Update operations, with direct metadata-override controls.
2. **medium / patch:** Later Remove can erase a counted edge. The same authored-operation rule rejects Remove, with independent removal controls. Findings 1 and 2 share the ignored item-operation root cause.
3. **medium / patch:** A blank metadata child overrides an attribute but the old reader discarded it. The rule now counts declarations before interpreting values; independent blank-override controls cover all three keys.
4. **medium / patch:** Self-consistent Include/Exists could point at the wrong repository root. Expected Includes now derive independently from the canonical EventStore root and exact project identity; wrong-root controls fail.
5. **medium / patch:** Manual-only workflow triggers could suppress the gate. This existing gap is exposed by the new workflow validation; it now requires both main push and main pull-request mappings without suppressing filters. Additional manual dispatch may coexist.
6. **medium / patch:** A skipped job prerequisite could skip provenance. The current independent-job contract now rejects needs, including scalar and sequence prerequisite controls.
7. **medium / patch:** Exact lowercase comparisons rejected supported MSBuild Boolean casing. Actual assembly-mode checks now use ordinal case-insensitive equality; a real uppercase package build/full suite and source refusal qualify the boundary.
8. **medium / patch:** The initial full package command had a seven-case floor for a nine-case suite. That historical command remains recorded. Final acceptance raises the floor to nine and independently requires the named Memories proof with a one-case minimum.
9. **medium / patch:** Log hashes lacked invocation attribution. Each log entry now records the command vector, working directory and exit code. New review acceptance captures those fields automatically during execution; older invocations are explicitly transcribed from tool history and superseded by final runs where applicable.
10. **medium / patch:** Saved acceptance lacked changed-source hashes. Final acceptance captures raw and normalized-LF source hashes and test-binary hashes before/after execution, binding current workflow/validators/AppHost files to the saved results. The read-only replay checks the current normalized source hashes as well as the raw archive and TRX evidence.

- Reviewed acceptance supersedes the initial suite: full Contracts 654/654 includes YAML 68/package governance 242; full uppercase package AppHost 9/9, exact named package proof 1/1 and source provenance 4/4 pass, with source AppHost refusal still expected. Final distinct acceptance total is 667. Exact invocations, exits and source/binary hashes are captured in the raw evidence; the all-ten-findings independent recheck is the remaining completion step.

- Final independent recheck clears all ten findings: original documents pass, every demonstrated bypass fails, uppercase package properties and exact named proof qualify the model, and all saved command/source/binary/TRX bindings replay successfully. No finding is deferred. The unrelated broad package IntegrationTests NativeSourceProof blocker remains documented. Pinned commitlint validates the exact candidate before the authorized commit/push.
