---
title: 'DW-621: Classify recovered prerelease GitHub Releases'
type: 'bugfix'
created: '2026-10-18'
status: 'done'
route: 'oneshot'
baseline_commit: '2994c279bc1c291c69e3d11f6bb6290c5504a13f'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Partial-release recovery accepts prerelease version suffixes but creates missing GitHub Releases without marking them as prereleases. Consumers can mistake a recovered preview for a stable release.

**Approach:** Pass GitHub CLI's prerelease flag when creating a missing release for a suffix-bearing version. Keep stable-release classification, exact source and artifact identities, existing-release handling and recovery refusals intact. Exercise the actual publisher with isolated publication tools, qualify stable and varied prerelease versions across recovery inventories, then close only DW-621 after owning-suite acceptance and independent review.

</frozen-after-approval>

## Implementation Notes

- Selected one cohesive, Tenants-owned actionable record from the deferred ledger. The recorded next action and the validator's existing version contract settle intent. No human-only choice, irreversible action or new public API is required. The existing branch owns the same recovery publisher and its latest commits harden this path; its working tree was clean on entry.
- Scope: `scripts/publish-partial-release.sh`, `tests/Hexalith.Tenants.Contracts.Tests/PartialReleasePublicationTests.cs`, this spec, scoped validation evidence, `deferred-work.md` and its `docs/maintenance-backlog.md` index. Preserve historical findings and all unrelated records, dependency pointers, archive, sprint and release holds.
- The preflight accepts three decimal version components with an optional hyphen suffix and no build metadata. `gh release create --prerelease` sets the new release classification (official manual: https://cli.github.com/manual/gh_release_create). Use an optional Bash argument array at the existing creation boundary; retain the original tag, target, title, generated notes and package assets. Existing releases are not edited by this repair.
- Generalize the existing executable fixture's version while retaining exact command/sequence and recovery-state assertions. Expected classification must be explicit test data rather than duplicating the production suffix predicate. Reproduce failures against unchanged production, then run the Debug source build, focused publisher class, maintained full Contracts lane, scoped formatting, shell syntax, whitespace and story-gitlink checks. The baseline's required isolated Aspire snapshot supplies environment evidence only. The build workflow includes independent review and a validated local Conventional Commit; no push or publication is performed.
- The isolated pre-edit Aspire start, resource/telemetry capture and exact-AppHost stop succeeded. Memories resources were healthy, security was starting and dependent services were waiting; this is environment evidence, not a healthy-topology or release qualification. The unrelated running FrontComposer AppHost was preserved.
- Added nine explicit classification cases by generalizing the existing executable fixture's version. The Debug source build passes with zero warnings/errors. Unchanged production fails all six prerelease-creation cases (6 failed / 49 passed / 0 skipped); stable creation, existing-release no-op paths and all earlier recovery guards pass. Added the optional prerelease argument array only at the existing missing-release creation boundary.
- Corrected production passes focused 55/55 and maintained full Contracts 406/406, zero failures/skips. Scoped formatter, shell syntax, whitespace, story-gitlink and direct baseline pointer checks pass. Exact commands, initial failures and environment/remote-evidence limits are retained in [validation evidence](dw-621-validation-evidence.md). Independent review and scoped ledger closure remain pending.
- Independent review identified missing final metadata verification, flattened release argument assertions, non-executable baseline replay guidance and insufficiently durable initial-failure detail. Accepted all for a small patch or evidence improvement. `scripts/verify-partial-release.sh` now requires the actual Boolean prerelease metadata to match the reviewed version; this refuses mismatches without editing existing releases. Added `PartialReleaseVerificationTests.cs` with thirteen real-verifier cases for stable/varied prereleases, wrong/missing/null/string metadata. The unchanged verifier passes five cases but incorrectly accepts all eight invalid-metadata cases. The corrected Debug source build has zero warnings/errors. Publisher assertions now compare each argument boundary, and baseline replay will use an isolated copy of the built assembly, marker solution, package manifest and selected scripts.
- Review corrections pass focused 68/68 and final maintained Contracts 419/419 with zero failures/skips. Scoped formatting and both shell syntax checks pass. The tracked `dw-621-check-variants.py` runner reproduces both original failure sets and rejects always-prerelease, collapsed-assets and inverted-verifier mutations; pristine controls pass. Only DW-621 closes, with 653 preserved IDs, 371 done/282 open and 254 exactly covering index rows. The closure checker preserves every unrelated record/row, original selected fields, archive/sprint and dependency pointers. Pinned commitlint 21.2.3 validates the exact local candidate with zero problems/warnings. Final independent recheck remains pending.
- Final independent recheck confirms all five findings resolved with no remaining issue or deferral. After the LF correction, the source build again passes with zero warnings/errors, focused 68/68 and maintained Contracts 419/419 pass with zero failures/skips, and scoped formatting, shell syntax and closure preservation pass. The reviewer also executed the tracked original/mutation replay successfully. Exact final commands and limits are retained in the evidence. Acceptance is complete; status is done.

## Review Triage Log

- **medium / patch — final metadata unverified:** the final workflow verifier accepted stable metadata for a prerelease and the inverse. Added a strict Boolean classification check and thirteen executable metadata cases; the original verifier fails eight cases, corrected production passes all thirteen. Existing-release objects are refused when inconsistent rather than edited.
- **low / patch — baseline replay unavailable:** the initial commands reflected a genuine pre-edit run but could not recreate it after the patch, and an environment override is replaced by the helper. Added a tracked isolated runner that selects the entry-revision scripts and documents historical versus current commands.
- **medium / patch — flattened arguments:** `$*` could hide a regression joining the five assets into one argument. Compare argument counts and each token; a copied joined-assets mutation now fails all ten release-creation cases.
- **low / patch — durable failure details:** aggregate summaries already existed, but the exact failing input sets and replay/mutation results were absent. Retained both failure sets and observed control results in the evidence, with the executable runner supporting re-audit. No finding is deferred.
- **medium / patch — Windows argument-file line endings:** independent recheck resolved all four original findings and reproduced the tracked controls, then demonstrated that platform-default CRLF would remain in Bash `mapfile` arguments. Write explicit LF separators for the argument vector, matching the shell fixture. Final validation follows this test-fixture correction.
