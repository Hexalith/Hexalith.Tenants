---
title: 'Resolve five deferred release artifact verification gaps'
type: 'bugfix'
created: '2026-10-23'
status: 'done'
route: 'dispatch'
baseline_commit: '25e3bf827a7963ec79b57b4918290b3fed728706'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Resolve exactly DW-527, DW-544, DW-582, DW-583 and DW-589. Validate NuGet dependency versions/frameworks against restore evidence, verify recovery containers through shared OCI verification, and check destination content after normal publication. Independently review, verify, commit and push the changes to `main`.

## Boundaries & Constraints

**Always:** Preserve the five-package inventory, source/tag gates, technical ServiceDefaults exceptions, two-platform OCI contract, historical findings, unrelated records and existing ledger edits. Reuse Builds technical code.

**Never:** Publish a release during this task, change dependencies/gitlinks, replace artifacts or weaken gates. Leave credential separation (DW-560/DW-563) and execution-pin reconciliation (DW-593) open. OCI proof establishes immutable content with exact source provenance, not equality with an independently rebuilt image.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| NuGet boundary | Same IDs, wrong range/framework | Reject, including ServiceDefaults exceptions | Fail validation |
| Equivalent ranges | `1.0.0`, `>= 1.0.0`, `[1.0.0, )` | Equal minimums; distinguish exact `[1.0.0]` | Reject malformed/ambiguous evidence |
| Existing OCI index | Reviewed source and two platforms | Verify immutable descendants, provenance and retained digest | Reject mismatch or unresolved content |
| Published destinations | Packages and OCI receipt | Match NuGet/GitHub bytes and OCI digest | Reject missing/conflicting content |

</frozen-after-approval>

## Code Map

- `scripts/validate-nuget-packages.py`: restore loading and nuspec parsing discard ranges/frameworks; retain both through boundary comparison.
- `tests/Hexalith.Tenants.Contracts.Tests/CiQualityGateScriptTests.cs`: executable package fixtures; preserve equivalent minimum ranges and packages without dependencies.
- `scripts/partial-release-recovery-common.sh`: reuse inventory, preparation and GitHub asset hashing. Recovery scripts currently accept container HTTP 200 without content proof.
- `references/Hexalith.Builds/Github/publish-containers/oci_registry_validator.py`: reuse index negotiation, immutable digest/size/platform and source/version/repository checks. Evidence paths must stay below the workspace.
- `references/Hexalith.Builds/Props/ContainerProvenance.targets`: existing imported provenance properties repair multi-RID labels.
- `.github/workflows/release.yml`: add destination verification after Semantic Release in the existing publication job; packages and publisher OCI receipt are available there. Retain the separate tag/source gate.

## Tasks & Acceptance

**Execution:**
- [x] `scripts/validate-nuget-packages.py` — retain per-framework direct/central ranges; normalize supported equivalents; reject missing, duplicate, conflicting or swapped groups. Apply ordinary checks to both technical exceptions.
- [x] `scripts/partial-release-recovery-common.sh` and `scripts/{validate-partial-release-recovery,prepare-partial-release,publish-partial-release,verify-partial-release}.sh` — verify OCI provenance/content, bind its digest to preparation, recheck before writes and after completion. Supply shared provenance properties for missing images; retain evidence in workspace-confined storage.
- [x] `scripts/verify-release-artifacts.sh` (new), recovery verifier and `.github/workflows/release.yml` — share read-only destination checks against reviewed package bytes and OCI receipt. Derive the actual release version, require exact inventory/source, and verify normal publication after Semantic Release.
- [x] `tests/scripts/test_partial_release_recovery.py`, new `tests/scripts/test_release_artifact_verification.py`, affected Contracts package/workflow tests and `.github/workflows/story-guards.yml` — qualify real validators and caller wiring with isolated negative fixtures in mandatory CI.
- [x] `_bmad-output/implementation-artifacts/deferred-work.md` and batch evidence — close only the selected five after validation, preserve original fields, and maintain exact index coverage. Preserve archive edits unstaged.

**Acceptance Criteria:**
- Given accepted dependency IDs, when versions/frameworks disagree with restore evidence, then package validation fails.
- Given prepared recovery, when existing OCI content or provenance changes, then recovery refuses before publication.
- Given normal publication, when any destination differs from reviewed content, then the workflow fails and retains evidence.
- Given the original ledger, when closure completes, then exactly five open records become done and unrelated records/user edits remain preserved.

## Implementation Notes

- User approved continuing with dirty ledger/archive files while preserving their edits and committing only task changes.
- No intent gaps. Only irreversible action: authorized Git push. Footprint: validators, workflow wiring, tests and evidence; no domain API changes.
- Recovery uses the tracked root Builds helper, whose provenance CLI is newer than its declared execution pin. Record this distinction; do not claim DW-593 closure. Normal release retains its approved copied helper.
- Baseline recovery suite passes 27/27 in 43.342 seconds.

- Pre-existing user ledger/archive bytes and patch are preserved at `/tmp/tenants-dw-527-544-582-583-589-0r_75odq`. Use those entry bytes for preservation checks and exclude their existing diff from the task commit.

- Parent verified the staged task diff, excluding the pre-existing ledger/archive cleanup. All five execution tasks are complete. Matrix audit: the executed NuGet range suite covers wrong range/framework, equivalent minimums and technical exceptions; recovery fixtures cover OCI index/provenance/digest refusal; normal-publication fixtures cover all destination bytes and receipts. Final implementation evidence is 257 Python and 484 affected Contracts cases with zero failures/skips.

## Spec Change Log

## Review Triage Log

| Finding | Verdict and evidence | Resolution |
| --- | --- | --- |
| Blind 1 | high: NuGet.org automatically adds repository signatures; the whole-ZIP comparison rejects unchanged reviewed payloads. Microsoft signed-packages documentation and the reviewer signature-only fixture confirm it. | Patch the private payload comparison and add acceptance/rejection controls; preserve raw destination hashes. |
| Blind 2 | high: Local packages are hashed after each destination read, so a mutation during GitHub reads can validate different bytes against a moving baseline. The fixture reproduces a false pass. | Patch a frozen reviewed-input snapshot and final unchanged-input check. |
| Blind 3 | high: The configured evidence directory can equal nupkgs, overwriting the local comparison source before hashing and producing a false pass. | Patch evidence/input overlap rejection before writes. |
| Blind 4 | medium: The new OCI network read occurs after the final live-main and preparation checks, allowing source/state drift before publication. | Patch source/preparation checks after OCI and before the first write. |
| Blind 5 | medium: Receipt discovery can fail before the previous success marker is removed; a rejected retry retains pass evidence. | Patch early attempt invalidation, including admission failures. |
| Blind 6 | medium: NuGet and GitHub response bytes use identical paths; a conflicting GitHub download destroys the NuGet body needed to diagnose the mismatch. | Patch distinct destination storage. |
| Blind 7 | medium: Three curl retries allow roughly seven seconds after fast 404s, while Microsoft documents validation/indexing taking minutes. A normal successful publication can be misreported failed. | Patch bounded publication readiness waiting and delayed-availability controls. |
| Blind 8 | medium: The failed validation trap removes temporary package observations before the unconditional evidence upload; failed recovery cannot retain its observed package context. | Patch failure evidence retention separately from accepted state. |
| Blind 9 | medium: Version bounds retain spelling differences: 1.0 and 1.0.0 compare differently despite NuGet normalization. The new comparator exposes a false rejection. | Patch canonical numeric/prerelease forms before range comparison. |
| Blind 10 | low: The advertised .NETCoreApp/.NETStandard families map aliases inconsistently; .NETCoreApp3.1 becomes net3.1. | Patch the direct alias mapping and paired fixtures. |
| Edge 1 | medium: Same reproduced source drift after the added OCI read as Blind 4. | Group with Blind 4; one source/preparation recheck patch. |
| Edge 2 | medium: Same reproduced stale success marker after missing-receipt rejection as Blind 5. | Group with Blind 5; one early invalidation patch. |
| Edge 3 | medium: Same reproduced destination body overwrite as Blind 6. | Group with Blind 6; one separate-storage patch. |
| Verification Other 1 | high: The signature-only archive mutation is rejected despite unchanged package files; confirmed with the new fixture and Microsoft primary docs. | Group with Blind 1; one signature-aware payload comparison patch. |


| Implementation follow-up | high: the same repository-signing mismatch exists in recovery preparation, which compares a downloaded signed ZIP against a newly packed unsigned ZIP. | Patched with the same payload comparer after validating the retained destination archive against its accepted raw hash; signed-existing-package and tamper controls pass. |
| Independent recheck | medium: arbitrary root signature bytes pass payload comparison, but native NuGet verification rejects them with NU3005. The parent verified a real cached repository-signed package passes and signature-only corruption fails. | Patched native signature verification for every signed destination, including identical archives. Tool failure/unavailability/timeout and concurrent mutation reject. The real native positive/negative controls pass, and independent recheck confirms resolution. |

## Verification

- Run all `tests/scripts` through Python unittest discovery, plus shell syntax, whitespace and changed-story checks.
- Build the Contracts test project in Debug with source references; invoke its built assembly with single-dash `-class` filters for affected package/recovery/workflow tests.
- Negative fixtures reject wrong ranges/frameworks, exception versions, OCI labels/digests/platforms, negotiation failures, changed preparation and destination bytes. Exercise the real shared OCI validator.
- Prove exact-five ledger preservation. Validate the full commit message with pinned commitlint; fetch/inspect divergence, commit only task-owned changes, push and verify remote `main`.


Review routing: all ten distinct causes are local corrections to the implemented comparisons, ordering, normalization or retained evidence. Each uses the existing private script interface and demonstrated states, so they route to patch; no intent choice or public API change is needed. All three layers completed before triage.

Final review recheck: all original findings and the native-signature follow-up are resolved. The frozen intent is unchanged; no new deferred record is needed. Parent verification additionally exercised the full payload comparer against a real repository-signed NuGet package and its signature-only corrupted copy (accepted/rejected respectively).

Final parent gates: 273/273 Python tooling cases and 484/484 affected Contracts cases pass with zero failures/skips; source build has zero warnings/errors. Native signed-package positive and signature-corruption negative controls pass through the complete helper. Actionlint, shell syntax, whitespace, exact-five preservation and the worktree story guard pass. Independent rechecks clear every finding, with no new deferred work. Pinned commitlint 21.2.3 validates the exact commit message with zero problems/warnings. The final push to main is explicitly user-authorized.
