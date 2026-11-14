---
title: 'Repair current McpCli published-cohort CI'
type: 'bugfix'
created: '2026-11-14'
status: 'in-review'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '401d926eb7d51bdf332696ed3f0fe39a812b3501'
owning_repository_baseline_commit: '42d3c2fd48405e0f2c27f9b61f6d7025e4246934'
investigated_remote_head: '7fa3df6a6b827f85fba6e12ec22c5efa08034ac8'
context:
  - '/home/administrator/projects/hexalith/tenants/references/Hexalith.McpCli/AGENTS.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Restore current McpCli CI using its published EventStore 4.2.0, Tenants 7.0.7 and Parties 3.1.1 cohort. Preserve product .NET 10 support by overriding incompatible central package selections. Resolve two census rejections through specifically authorized classifications and the immutable approval chain.

## Boundaries & Constraints

Preserve analyzer netstandard2.0, current/historical vectors, source identities, native authorization, artifact integrity and every gate. No broad exclusions, fabricated approval, scanner weakening, business behavior changes, operator credentials or nested initialization. Preserve concurrent work and dependency checkouts. Staging, commits, pushes and publication require a later concrete publication decision; local evidence grants no hosted acceptance.

## Approved Decisions

On 2026-11-14 the requesting user answered “apply” and “approve” to the exact two census classifications and repair spec. This specifically authorizes PE06 external-developer-tool classification of Microsoft.DotNet.ApiCompat.Tool installation and PE09 qualification-evidence-tooling classification of Hexalith.EventStore.RuntimeIdentityProbe at EventStore d457ecd51a17b8ecc0c678b02d19d7ee92c13141, with the exact source bindings in evidence/mcpcli-current-ci-2026-11-14/investigation.json, including all eight probe compilation sources. No broader source, future surface or publication authorization is inferred.

</frozen-after-approval>

## Code Map

Implementation paths are relative to `references/Hexalith.McpCli`.

- `Directory.Build.props`: net10 product target; add PackageVersion overrides after the central import. Current catalog introduces three net11-only dependencies. Published module nuspecs permit compatible net10 versions.
- `tools/dependency-policy.json`: exact production/helper closure, verified by Manifest.Tests against fresh assets. Existing approved UniqueIds version is 2.30.10.
- `tools/proprietary-surfaces/v1/README.md`, `amendments.json`, `amendments/1.0.7.json`: mandatory specific owner authority, full source hashes and append-only lineage. Preserve all prior receipts.
- `.github/workflows/ci.yml`, current vector/plan/runtime selections and Manifest.Tests: authoritative qualification sequence. Existing net10 executable paths and current enrollment stay intact.

## Tasks & Acceptance

**Execution:**

- [x] `Directory.Build.props` — override UniqueIds to 2.30.10, JwtBearer and SignalR.StackExchangeRedis to 10.0.15. These public packages satisfy the selected module floors without changing runtime support.
- [x] `tools/dependency-policy.json` — fresh package-mode restore into a task-owned cache; reconcile only actual changed selections and run existing dependency/helper-boundary controls.
- [x] `tools/proprietary-surfaces/v1/amendments/` and `amendments.json` — after specific approval, append the next amendment and v2 receipt binding the packet's exact identities/sources; reject source drift and preserve prior lineage.
- [x] `Directory.Build.props` and `tests/Hexalith.McpCli.Analyzers.Tests/Hexalith.McpCli.Analyzers.Tests.csproj` — resolve subsequent current-catalog compiler failures while preserving product .NET 10 and analyzer netstandard2.0 support.
- [x] `tools/live-topology/test_ci.py` — bind the existing catalog identity check and its negative-control fixture to the already-recorded approved catalog, preserving executor identity and all refusal controls.
- [x] Owning evidence directory and this spec — retain local verification, independent review, corrected-helper controls and exact source hashes.
- [ ] Complete the additional specifically approved census actions and exact-source hosted/native qualification after the required owner and publication decisions.

**Acceptance Criteria:**

- Given current published modules, when fresh restore and Release warnings-as-errors build run, then NU1202 is absent, net10 support remains and dependency policy matches assets.
- Given specifically approved additions, when census fixtures and committed-tree scan run, then only those additions pass; incomplete approvals, source drift and unknown identities remain rejected.
- Given the fresh build, when ordinary tests, enrollment replay, strict benchmark, sample conformance and topology discovery run, then every required case remains enabled and passes.
- Given reviewed published source, when complete hosted CI runs, then every blocking job including native acceptance passes; otherwise completion remains pending.
- Given entry work, when changes are compared, then unrelated files, withdrawn historical drafts and all five completed dispositions remain preserved.

## Implementation Notes

Approved for local implementation on 2026-11-14. Footprint: compatibility and evidence byte-preservation configuration, amendment index, new amendment/receipt, census documentation and bounded evidence. No runtime/data side effects during preparation.

Fresh restore exposed two further compatibility failures after the three planned pins. The existing net10 gateway test host requires `Microsoft.AspNetCore.OpenApi` 10.0.15; its published dependency constraint requires `Microsoft.OpenApi` >=2.13.0 and <3.0.0, so both receive compatible post-import overrides (10.0.15 and 2.13.0). Official archives for all five overrides were independently downloaded and matched byte-for-byte to the task cache.

Roslyn 5.9's netstandard compilation closure selects System.Collections.Immutable 11, conflicting with the net10 analyzer test host's framework assembly 10. Only `Hexalith.McpCli.Analyzers.Tests` now targets net11.0, matching the pinned SDK/Roslyn host. The shipped analyzer remains netstandard2.0; all product, enrollment and topology net10.0 paths remain unchanged. The ordinary Release warnings-as-errors solution build and all six ordinary suites pass. Fresh production and Aspire-helper assets already match the approved dependency policy exactly, so no policy mutation is needed.

Amendment 1.0.8 adds only the two approved EventStore observations and binds all eight probe sources through the unchanged v2 authority chain. A candidate-policy overlay reads the exact owning HEAD source pins through existing root object stores without fetching, checking out or initializing nested submodules. Both additions pass; eight earlier allowance source drifts and four inspection errors remain refused. The read-only follow-up packet records each prior authority, approved/current source digest and bounded rationale; no additional approval is inferred. The later investigated remote commit has identical census policy and all the same pins except Platform, so it does not contain missing renewals for these failures.

Owning evidence: [verification packet](../../references/Hexalith.McpCli/evidence/mcpcli-current-ci-2026-11-14/verification-index.json), [candidate scan summary](../../references/Hexalith.McpCli/evidence/mcpcli-current-ci-2026-11-14/candidate-census-summary.json), and [remaining census follow-up](../../references/Hexalith.McpCli/evidence/mcpcli-current-ci-2026-11-14/remaining-census-followup.json). Full raw output is retained byte for byte under the owning packet raw/ directory with relative paths and SHA-256 bindings; original scratch paths are historical context. Disk exhaustion during isolated replay was resolved with a task-owned memory-backed cache/scratch directory and a fresh restore; no unrelated data or system defaults changed.

The non-live topology suite passed 245 of 246 cases before exposing one stale test expectation: it still required catalog `6ad5854aa14a9950e1fbfc32d7e27953a1c4b82b`, while the approved source packet and unchanged committed gitlink select `05a75b626fb73c6e386d16c41e885f193d3cff16`. The exact expected catalog identity and its negative-control fixture now bind that existing selection. The separately approved reusable executor references remain `6ad5854aa14a9950e1fbfc32d7e27953a1c4b82b`; no gitlink, workflow reference, native selection or source identity moves. The catalog/executor checks remain exact and all eleven mutation refusal controls remain enabled. Focused identity and negative-control tests verify the correction without rerunning the eleven-minute unchanged suite.

## Verification

From the owning repository, use fresh restore and Release build with `-warnaserror`; run the six ordinary projects individually. Follow owning guidance for enrollment coverage, strict benchmark, `check_enrolled.py`, sample validation/loopback and subsequent fresh restore. Run topology fixtures/preflight and exact built-test discovery; census fixtures/committed scan; evidence secret scanning; whitespace checks; independent review; eventual exact-source hosted CI.

Local results: fresh restore and final production Release warnings-as-errors build pass; all six ordinary projects pass (2,038 successful tests, six existing platform/probe skips). Enrollment coverage, strict full-v1 benchmark, both-module CLI/MCP replay (21 operations, six shapes), sample validation and complete sample loopback pass. All 222 census fixtures pass. Topology preflight and exact discovery of 21 vectors plus smoke/boundary pass; 245 topology fixtures pass and the single stale catalog expectation is corrected with both focused identity/refusal-control cases passing. After sample loopback, a fresh restore, production rebuild and three real dependency/helper/integration closure checks pass.

The candidate census approves exactly the two new entries and retains eight allowance source drifts plus four inspection errors. The direct owning-workspace scan reports `git-object-unavailable` because its nested references remain uninitialized; the labeled read-only object-store overlay supplies the committed-source evidence without changing them. Newer Platform context is separately hash-bound in the follow-up packet. Independent review, additional specific census authority and complete exact-source hosted/native qualification remain pending; no staging, commit, push or publication occurred.

## Investigation Evidence

[Packet](evidence/mcpcli-current-ci-2026-11-14/investigation.json) records public package hashes, source closure and [failed current CI](https://github.com/Hexalith/Hexalith.McpCli/actions/runs/43536045450): six restore/conformance/discovery failures plus census, with native/performance skipped.

Original 7.0.0 enrollment work was superseded and delivered. DW-117/118/133/365/367 are done and archived, with zero open maintenance rows. Their withdrawn draft and successor evidence remain unchanged. The full probe closure was successfully computed using permitted top-level object stores; no nested checkout or fetch was needed.

## Spec Change Log

## Review Triage Log

All three independent layers completed before triage. Verification-gap review found no gaps. The following rows retain every final finding separately; repeated edge findings are grouped with their blind-review counterparts only for implementation.

| Finding | Verdict | Route and evidence |
| --- | --- | --- |
| B1: receipt paths used before validation | high | patch: the helper joins unvalidated index/receipt paths and copies before unchanged policy validation; parent traversal can escape temporary storage. Validate paths before any access/copy. |
| B2: receipt symlinks become regular overlay files | medium | patch: copyfile follows the source symlink and loses the checker's leaf-symlink refusal. Reject those sources before copying. |
| B3: stdout bypasses redaction | medium | patch: the JSON report file is sanitized, but the summary prints raw source-derived error/identity values. Sanitize the console summary too. |
| B4: exceptional scans leave no failure receipt | medium | patch: output is written only after scan returns; a failed rerun can leave a stale prior report. Emit a sanitized failed result and provenance for supported prerequisite/policy/decoding failures. |
| B5: optimization removes preservation assertion | medium | patch: assert guards metadata preservation and disappears under -O. Use an unconditional runtime refusal. |
| B6: supplemental receipt bindings are not validator predicates | low | patch: document that the extra authoring/provenance bindings are supplementary; actual authority, authorization and allowance source closure are enforced by the unchanged checker. Current supplementary hashes independently match. |
| B7: raw verification artifacts are temporary | low | patch: retain sanitized task-owned raw evidence in the owning packet with durable relative references, preserving hashes and rescanning the expanded packet. No publication is implied. |
| B8: command index lacks a final implementation manifest | medium | patch: add exact final source/configuration hashes and bind relevant final verification records to that manifest; keep failed and historical command contexts distinct. |
| B9: full census still rejects earlier sources | medium | defer: unchanged source pins/policy already produce eight allowance drifts and four inspection errors. Additional specific authority and exact-source hosted/native acceptance remain pending; no completion claim is made. Retain an explicit ledger follow-up. |
| B10: archived approval snapshot packet link is broken | low | patch: adapt that nonfrozen snapshot link to its stored location and refresh the new receipt/index hashes; preserve the approved frozen intent and every prior receipt. |
| E1: traversal during pre-validation receipt copy | high | patch with B1: direct path join and filesystem mutation precede safe_path validation; the counterexample is reachable with malformed candidate metadata. |
| E2: symlink evidence normalized by copy | medium | patch with B2: unchanged loader refuses symlink evidence, while the helper currently converts it to a regular overlay file. |
| E3: credentials in diagnostic identities/targets | medium | patch with B3: the helper's final print bypasses the existing redactor despite sanitizing its file output. |
| B11 (intermediate): metadata attestation scope is too broad | low | patch: identify the measured checkout metadata explicitly (HEAD/index/packed-refs/refs), rather than implying config/reflog/common-directory coverage. |

- Implementation starts from the clean owning checkout at the recorded local baseline. The investigated later remote revision remains context; no fetch, branch movement, stage, commit, push or remote workflow operation is authorized in this implementation phase. Existing root and other-submodule changes are preserved.


## Review Corrections Verified

All three independent layers completed before triage. The implementation agent corrected the helper and evidence findings; root independently repeated all seven targeted regressions and verified all 42 source hashes, all 173 files in the review-fix scan, all 40 command logs and the exact new amendment/receipt bindings. The edge reviewer independently rechecked the three original counterexamples and found no residual defect; the verification reviewer independently confirmed the regression quality and all hash bindings with no residual gap. Ordinary .NET, enrollment and long topology/sample suites were not repeated for evidence-only corrections.

All 120 original raw files are now retained durably, with historical and final command contexts distinguished. The new snapshot link is correct and only new1.0.8 linkage hashes changed; every prior receipt and the approved frozen intent remain unchanged. The final review record is in ../../references/Hexalith.McpCli/evidence/mcpcli-current-ci-2026-11-14/review-result.json. A final unchanged-scanner receipt covers the post-review status record and packet documentation.

B9 remains an explicitly recorded pre-existing follow-up in the root deferred ledger. The twelve proposed source renewals/classifications/exclusions are inactive and bound to the prospective current source context, including the full86-source newer Platform closure. Root independently recomputed all proposed bindings from the already-available committed object stores; none is approved or activated by this work.

Status stays in-review: additional specific census authority, committed-source hosted/native acceptance and publication are incomplete. The presentation workflow's automatic done/commit actions are withheld because the approved scope explicitly reserves staging/commits/pushes/publication for a later concrete decision and the overall CI acceptance remains unsatisfied. No existing work, branch, checkout or prior approval was overwritten.

- Parent final new-file whitespace verification found four verbatim log/patch captures with trailing/CRLF whitespace. Four exact .gitattributes data entries follow existing repository capture conventions; no raw byte or executable-code whitespace rule changed. The final source/index links and final unchanged-scanner receipt include this metadata correction.
