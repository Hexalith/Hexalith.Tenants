---
title: 'DW-215: Match stable release-tag validation to semantic-release'
type: 'bugfix'
created: '2026-10-18'
status: 'done'
route: 'oneshot'
baseline_commit: '5efab750d471064c667e6d9e36cb222fa823bda5'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The unprotected release-floor guard accepts leading-zero and oversized numeric components in plain `vMAJOR.MINOR.PATCH` tags. Semantic-release rejects these versions, so the guard can select a floor the release engine will ignore and miss a registry collision.

**Approach:** Keep the existing plain three-component tag policy, admitting only canonical decimal components from zero through `9007199254740991`, inclusive, before numeric sorting or reachability requests. Prove invalid tags are ignored, an invalid-only set cannot establish a floor, valid boundaries still work, and the highest reachable valid tag still governs. Preserve the existing registry grammar, four-component comparison, source and approval gates, package inventory, dependency pins and authentic-tag recovery policy. Resolve only DW-215 after the relevant checks and independent review pass.

</frozen-after-approval>

## Implementation Notes

- Investigated DW-215 using its ledger record, the completed release-floor spec, project context, release configuration, lockfile and installed semantic-release/semver sources. No intent gap or irreversible operation is needed. The change uses the existing no-action Bash/jq guard and `PackageGovernanceTests.RunTagFloorGuardAsync`; registry grammar and broader tag-format policy remain outside the correction.
- `.github/workflows/release.yml` now checks canonical decimal components and their inclusive safe-integer bound as strings before `tonumber`, sorting or comparison requests. Arbitrarily long numeric components cannot round or overflow into a floor.
- `tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs` exercises the actual extracted Bash. Initial unchanged production: 24 failures/12 passes; corrected initial focused: 36 passes; initial full Contracts: 190 passes. Independent review added interior-boundary, padded-zero and plain-format exclusion controls and consolidated both invalid-tag paths through shared theory data. Final focused count is 49 cases.
- The actual jq selector matches installed semantic-release 25.0.9 / semver 7.8.5 across 5,832 generated plain tag candidates: 729 accepted, 5,103 rejected, zero admission/order mismatches. The complete runner and exact commands are in `dw-215-validation-evidence.md`.
- Aspire start/describe/stop completed for the pre-code baseline; the resource snapshot had waiting services/unhealthy security and is not distributed-runtime acceptance. The isolated release-guard checks do not use that topology. The initial story-gitlink check failed for missing baseline metadata and passed after recording the full canonical entry revision.

- Final source Debug build passes with zero warnings/errors. After review controls, focused tests pass 49/49 and the final fresh-built full Contracts suite passes 203/203, zero failures/skips; actionlint, whitespace, parser-parity and story-gitlink checks pass. Only DW-215 closes; all 653 ledger IDs remain, 364 done/289 open across 261 exact-covering maintenance rows. Exact prior fields, unrelated records, archive, sprint and pinned configuration are preserved. Independent recheck confirms every finding resolved; no review deferral was added.

## Review Triage Log

- Blind Hunter computed `min(floor(sqrt(9.136) + 1), 10) = 4` and returned four findings.
- **low / patch — interior 16-digit acceptance controls:** existing max-boundary tests did not individually exercise ordinary 16-digit major/minor values, although the parity probe covered max-minus-one. Added `1000000000000000` in each slot to catch an accidental equality-only bound.
- **low / patch — padded zeroes:** invalid fixtures lacked all-zero padding. Added `v00.0.0`, `v0.00.0` and `v0.0.00` to both mixed-fallback and invalid-only paths through shared theory data.
- **low / patch — existing plain-tag boundary:** existing exclusions covered prerelease/two-part names but not build metadata/four-part tags. Added `v99.0.0+build.1` and `v99.0.0.0`, preserving the established policy rather than broadening it.
- **low / patch — duplicated invalid matrix:** the two new methods had identical fifteen-case lists. Replaced both with `InvalidPlainReleaseTags`, ensuring each future invalid candidate exercises both behaviors. This changes test fixture maintenance only.
- **low / patch — recheck naming correction:** independent recheck accepted all four controls but noted that build metadata is valid semver while outside the existing plain-tag policy. Renamed the mixed-fallback test to `ReleaseWorkflowIgnoresTagsOutsidePlainReleasePolicy`, accurately describing both numeric rejection and deliberate format exclusions; no runtime correction was required.
