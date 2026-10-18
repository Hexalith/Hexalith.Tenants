---
title: 'DW-216: Validate registry versions before prerelease exclusion'
type: 'bugfix'
created: '2026-10-18'
status: 'done'
route: 'oneshot'
baseline_commit: '43234d937ae0b3bc32c2837db38ea403b64b2742'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The unprotected release-floor guard accepts non-normalized numeric cores and malformed prerelease identifiers, then discards all hyphenated versions. Invalid registry evidence can therefore produce a passing floor check.

**Approach:** Validate every registry version before stable/prerelease classification: canonical decimal core components within NuGet's inclusive Int32 range, and nonempty ASCII alphanumeric/hyphen prerelease identifiers with no leading zeros in purely numeric identifiers. Preserve existing two-to-four-component compatibility, explicit zero-revision equivalence, case-insensitive prerelease compatibility, valid prerelease exclusion, four-component numeric comparison, release-tag selection, source/approval gates, inventory, dependency pins and authentic-tag recovery. Prove malformed entries fail closed in mixed and prerelease-only indexes while valid prereleases and stable boundary comparisons still work. Resolve only DW-216 after focused checks, the owning Contracts suite and independent review pass.

</frozen-after-approval>

## Implementation Notes

- Selected the next dependency-free release-floor defect after DW-215. Repository ledger, completed release-floor spec, existing real-Bash/jq harness, build rules and official NuGet grammar/source settle the intended correction. No intent gap or irreversible action is needed; the runtime footprint is one existing jq validator plus tests and maintenance evidence.
- Reuse `PackageGovernanceTests.RunTagFloorGuardAsync` and its exact-invocation assertions. Keep the existing two-component and explicit-zero-revision aliases even though flat-container output normally normalizes them further; this is a compatibility-preserving validator, not a general NuGet parser. Numeric prerelease identifiers have no Int32 bound. Do not change the independent semantic-release tag bound.
- The pre-code Aspire baseline started and described the exact Tenants AppHost, then stopped that task-owned instance. Security was unhealthy and domain services were waiting; this does not establish distributed-runtime acceptance and does not block isolated release-guard verification.
- Added 116 real-shell cases before changing the workflow. The zero-warning/error Debug source build succeeded; original-production execution failed 64 cases and passed 52. Complete string/core/suffix validation before prerelease filtering makes all 116 pass, and the full Contracts suite passes 319/319 with zero failures/skips. Actionlint, whitespace and story-gitlink checks pass. Independent review remains pending; DW-216 is still open.
- Independent review corrections add the documented 64-character version bound and fourteen test cases. The expanded matrix initially fails four overlong cases and passes 126; after correction, final focused 130/130 and full Contracts 333/333 pass with zero failures/skips. The review build has zero warnings/errors. Independent recheck confirms all four findings resolved and separately reports 17/17 jq probes passing; no review deferral remains.
- Only DW-216 closes. All 653 ledger IDs remain, 365 done/288 open across 260 exact-covering maintenance rows. Original fields are retained under `pre-dw-216-*`; unrelated ledger bodies, historical findings, archive, sprint, dependency pins and release holds remain preserved. Exact commands, earlier failures, review and limits are in `dw-216-validation-evidence.md`.
- The contributor's scoped whitespace formatter exits 2 with 172 diagnostics on unchanged lines and zero diagnostics in added lines, verified against the added-line intervals. Existing formatting is preserved; this separate file-wide blocker is recorded in the evidence alongside the passing source build, tests, actionlint and story-gitlink check.

## Review Triage Log

- Blind Hunter computed `min(floor(sqrt(11.376) + 1), 10) = 4` and returned four findings.
- **medium / patch — overlong prereleases:** the guard accepts a 65-character ASCII prerelease, although NuGet.org's documented version limit is 64 characters. Add the overall length bound and both alphabetic/numeric 64/65-character controls; no new public interface or release policy is required.
- **low / patch — non-string values:** the new type branch lacks direct real-shell controls. Add null, number, boolean, object and array entries, alone and mixed with valid evidence, requiring validation failure rather than success or drift classification.
- **low / patch — invalid/above-floor ordering:** existing invalid matrices pair with a covered stable version. Add both orders of malformed prerelease plus above-floor stable evidence, also with valid companions, proving complete validation precedes drift classification.
- **low / patch — genuine mixed stable drift:** add a mixed covered stable, above-floor stable and valid higher prerelease index. Require the actual stable version in the drift diagnostic and exclude the prerelease from it.
