---
title: 'DW-590: Recheck the recovery source immediately before publication'
type: 'bugfix'
created: '2026-10-18'
status: 'done'
route: 'oneshot'
baseline_commit: '2d733caf0c989d3531d82bd9e27d40fef12fc1f4'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Partial-release recovery checks live main before build, packing and validation. If main advances during preparation, the publisher still pushes artifacts for the superseded source.

**Approach:** Recheck live main against the reviewed source after local preparation and existing-package hash checks, immediately before the first publication. Refuse changed or unavailable source evidence without package, container or GitHub Release publication. Preserve initial source/version/inventory gates, package hash refusals and supported package/container/release-only recovery. Add isolated executable-shell regressions and close only DW-590 after owning-suite acceptance and independent review, preserving historical findings, unrelated ledger records, dependency pointers and release holds.

</frozen-after-approval>

## Implementation Notes

- Selected one cohesive Tenants-owned actionable record from the deferred ledger. Its recorded next action and the existing exact-source workflow invariant settle intent; no human-only choice, external side effect or new public API is needed. Small footprint: the recovery publisher, one owning Contracts test class and scoped maintenance evidence.
- Entry revision: `2d733caf0c989d3531d82bd9e27d40fef12fc1f4`. `scripts/publish-partial-release.sh` checks the source before `dotnet build`, pack/validation scripts and existing-package hash comparisons. Insert the final check after that hash loop and before missing-package pushes; this also guards container-only and release-only recovery. Keep workflow authentication, source eligibility, package manifests and artifact/provenance policies unchanged.
- Reuse Contracts tests' Bash process conventions, isolated temporary state and stubbed external commands. Reproduce main advancement during preparation against original production, then verify stable source and negative source/API/hash controls. Run a Debug source build, focused executable cases, the maintained Contracts suite, shell syntax, scoped formatting, whitespace and story-gitlink checks. The required isolated Aspire snapshot is environment evidence only. No publication is performed; the build workflow requests a validated local Conventional Commit.
- Added 33 executable publisher cases: four stable inventories, twenty main-advancement cases across build/pack/package validation/consumer validation/hash stages, and nine failed/empty/malformed final-response cases. The first build failed CS1929 because the Shouldly collection overload could not infer the expected list; materializing the expected array corrected it. The corrected Debug source build has zero warnings/errors. Original production fails all 33 exact-sequence checks, including all 29 unsafe-source cases proceeding to publication. After the three-line final gate, the focused suite passes 33/33 with zero failures/skips. Recovery state is compared byte-for-byte after every case.
- Required Aspire discovery found an unrelated running FrontComposer AppHost, which was preserved. Isolated Tenants start, describe and telemetry capture succeeded; Memories was healthy, security unhealthy and dependent services waiting. The initial stop command rejected unsupported `--format Json`; the corrected exact-AppHost stop succeeded. This is environment evidence, not healthy-topology or release qualification.
- The first maintained full Contracts suite passes 384/384, zero failures/skips, and scoped formatting passes. Independent review found five test-fixture gaps, all accepted for correction: complete existing inventories, zero-existing inventories, executable hash refusal, initial source/version/inventory refusals and exact publication arguments. Expanded the suite to 46 cases, including main advancement during the last of five hash checks, truly empty existing inventories and strict source/package/container destination validation. Production remains the same two-command post-hash recheck; separate DW-608 is not closed by a preservation control in this task.
- Independent recheck resolves all five findings without a residual issue. Final zero-warning/error Debug source build, focused 46/46, maintained Contracts 397/397 and scoped formatting pass, with zero failures/skips. The actual compiled tests reject seven harmful copied-script mutations while a pristine copy passes 46/46. Only DW-590 closes: all 653 IDs remain, 370 done/283 open across 255 exactly covering maintenance rows. Every unrelated record/index row, archive/sprint and dependency pointer is preserved; shell syntax, whitespace, story-gitlink and direct pointer checks pass. [Exact commands, initial failures and limits](dw-590-validation-evidence.md) preserve the unhealthy pre-edit security snapshot and unchanged release holds. Pinned commitlint 21.2.3 validates the exact local commit candidate with zero problems/warnings before use. No release readiness is claimed.
- Final artifact review confirms scope, evidence and accounting. Clarification of the earlier historical “byte-for-byte” wording: state assertions compare filenames and decoded text contents, as the evidence document states. The spec was marked done after acceptance; the reviewer read the earlier in-progress version while that update was being written.

## Review Triage Log

- **medium / patch — incomplete existing inventory:** container/release-only fixtures omitted four existing IDs. Populate all five hashes and advance main only at the final comparison, proving the final gate follows every existing artifact check.
- **medium / patch — zero-existing path absent:** a gate inside the hash loop could escape detection. Add all-five-missing success, advancement and unusable-response controls with empty existing inventory/hash files.
- **medium / patch — hash refusal untested:** add changed package bytes and require the existing mismatch refusal before the final source read or any publication. This is preservation coverage, not a closure of the separate DW-608 acceptance record.
- **medium / patch — initial refusals untested:** add stored-source mismatch, version mismatch, absent inventory and initially stale main, requiring exact pre-preparation call sequences and unchanged persisted state.
- **medium / patch — permissive publication stubs:** validate exact package files and NuGet arguments, exact release tag/source/assets and exact container destination/version/options. Compare publication sequence and identities so duplicate or redirected operations fail. No finding is deferred.
- **low / patch — evidence wording:** clarify the earlier historical state-comparison wording to filenames and decoded text contents, matching the actual assertions and evidence document.
- **false — final spec status:** the reviewer observed the earlier in-progress version during finalization; current frontmatter is done after owning-suite acceptance and closure verification.
