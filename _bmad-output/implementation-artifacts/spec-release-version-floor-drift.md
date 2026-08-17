---
title: 'Restore the release version floor and guard registry/tag drift'
type: 'bugfix'
created: '2026-08-16'
status: 'done'
baseline_commit: 'f6ccee0c2c3e2d4eca55a66df434ab502e4d27fd'
review_loop_iteration: 3
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Deleted release tags once let semantic-release propose an already-published NuGet version. The pre-approval drift guard now prevents recurrence, but it truncates nonzero NuGet revision components and gives remediation that could encourage a provenance-invalid replacement tag.

**Approach:** Preserve the guard and legitimate 4.x-or-later history, compare all four stable NuGet version components numerically, and permit recovery only by restoring a reachable tag at the commit that produced that published version. If provenance or reachability cannot be established, stop for a separately reviewed release-lineage recovery procedure.

## Boundaries & Constraints

**Always:**
- Keep the guard in unprotected `verify-source`, before production approval and secrets.
- Keep `release.yml` free of third-party actions; use `gh api`, `curl`, and `jq`, reading `tools/release-packages.json` at the dispatched SHA.
- Compare stable versions numerically as `Major.Minor.Patch.Revision`; omitted or zero revision is equivalent to zero.
- Use the highest reachable `vMajor.Minor.Patch` tag. `compare/<tag>...<sha>` accepts `ahead` or `identical`; `behind` is an unreachable descendant.
- Fail closed on failed or malformed probes and when no reachable release tag exists.
- Keep `tools/release-packages.json` as the sole package inventory.

**Ask First:**
- Creating, deleting, moving, or re-pointing any tag.
- Changing package IDs, the container repository, or the expected package count.
- Every push to `origin` (branch or tag).

**Never:**
- Never recreate `v3.15.1` unless its actual producing commit is recovered and verified.
- Never use `feat!:`/`fix!:` for the major; this repository requires a `BREAKING CHANGE:` footer.
- Never reintroduce `--skip-duplicate`, edit `references/Hexalith.Builds`, or commit/push.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Covered floor | floor `v3.15.1`, published `3.15.1` or `3.15.1.0` | Pass | N/A |
| Nonzero revision drift | floor `v3.15.1`, published `3.15.1.1` | Exit 1 naming the exact version and floor | Fails pre-approval |
| Registry ahead | floor `v3.2.18`, published `3.15.1` | Exit 1 with reachable authentic-tag guidance and separate-review escalation when provenance cannot be established | Fails pre-approval |
| Unusable evidence | failed/malformed probe or no reachable tag | No release | Fails closed |

</frozen-after-approval>

## Code Map

- `.github/workflows/release.yml` -- `verify-source` guard; update `def parts` and authentic-tag-only remediation text only.
- `tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs` -- `Release_workflow_*tag_floor*` tests execute extracted Bash through `RunTagFloorGuardAsync`; use isolated revision and boundary-order fixtures.
- `tools/release-packages.json` -- read-only five-package inventory.
- `.releaserc.json` and `scripts/validate-publication-preflight.sh` -- read-only release/version policy and post-approval destination defense.

## Tasks & Acceptance

**Execution:**
- [x] `.github/workflows/release.yml` -- compare padded four-component stable versions and direct recovery only to a reachable authentic tag or separate lineage review.
- [x] `tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs` -- prove isolated `3.15.1.0 == 3.15.1`, `3.15.1.1 > 3.15.1`, numeric revision ordering, `3.15.0.999 < 3.15.1`, and the authentic-tag-only guidance.

**Acceptance Criteria:**
- Given the highest legitimate reachable tag covers every stable package version, when `verify-source` runs, then it passes without entering protected release state early.
- Given any stable package version exceeds the floor by full numeric precedence, when the guard runs, then it exits 1 with package, version, floor, and reachable authentic-tag remediation.
- Given authentic provenance or reachability cannot be established, when an operator reads the failure, then it directs them to stop for a separately reviewed lineage-recovery procedure rather than fabricate or bypass a tag.
- Given evidence cannot be proved, when the guard runs, then it exits 1 rather than assuming absence.

## Spec Change Log

- **2026-07-28 — review iteration 1 — frozen block contains a factually wrong mechanism.**
  All three review lenses independently found, and a live API call confirmed, that the
  `<frozen-after-approval>` line "take the first whose `compare/<tag>...<sha>` status is
  `behind` or `identical`" is inverted. `compare/{base}...{head}` reports head relative to
  base, so a reachable ancestor tag reports `ahead`; `behind` means the tag is a descendant
  Semantic Release will not see. Verified: `compare/v3.2.17...v3.2.18` →
  `{"status":"ahead","ahead_by":2}`.
  The frozen block's *intent* sentence — "Floor = highest `v<semver>` tag reachable from the
  dispatched SHA, matching semantic-release's `git tag --merged`" — is correct, and the
  implementation follows that intent. The frozen mechanism clause was NOT edited: only the
  human may change frozen content. It should be corrected to `ahead` or `identical`.
  Known-bad state avoided: the first cut implemented the frozen clause literally and would
  have failed every dispatch with "No release tag is reachable"; it passed CI only because
  `v4.0.0` sat on the main tip that day.
  KEEP: the reachability walk itself (highest-first, stop at first reachable) is correct and
  must survive re-derivation — only the accepted status values were wrong.

- **2026-07-28 — world state moved during implementation.** `825b98c` landed on `main` and
  fixed the original 3.x collision by declaring `minimum_release_version` in
  `validate-publication-preflight.sh`; run 30340676669 then published all five packages at
  4.0.0. The `v3.15.1` floor tag this spec called for was created, pushed, and then deleted
  again: `825b98c` had explicitly declined to create it because it would resolve to the
  3.2.18 tree rather than `5469a6b`, where the 3.15.1 packages were really built. The
  `BREAKING CHANGE:` footer was dropped for the same reason — 4.0.0 is already published, so
  it would now drive 5.0.0. The drift guard remains as the general-case protection.

- **2026-08-16 — review iteration 2 — human-approved intent correction and re-scope.**
  Review found the frozen reachability polarity still contradicted the tested API semantics,
  the recovery text could encourage fabricated provenance, and four-part versions were
  truncated. The human authorized the frozen correction. The spec now preserves the working
  highest-reachable-tag guard and real-shell harness while requiring provenance-safe recovery
  and full NuGet revision comparison. This avoids recreating the rejected `v3.15.1` tag and
  prevents `3.15.1.1` from passing a `v3.15.1` floor.

- **2026-08-16 — review iteration 3 — recovery policy narrowed by human decision.**
  Review proved a version-line advance was not executable because the guard blocks before
  semantic-release analyzes the proposed release. The human selected authentic-tag-only
  recovery. The spec now requires a reachable tag at the actual producing commit and a
  separate reviewed lineage procedure when that proof is unavailable. KEEP the four-slot
  comparator and real-shell harness; add isolated zero-revision and lower-core boundary tests.

## Design Notes

`v4.0.0` at `825b98c` authentically escaped the occupied 3.x band; later legitimate tags now provide the floor. A replacement tag is valid only when it points to the actual producing commit and is reachable from the dispatched SHA. NuGet treats a missing or zero fourth revision as zero, while a nonzero revision participates in ordering.

## Verification

**Commands:**
- `actionlint .github/workflows/release.yml` -- expected: no findings.
- `dotnet build tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Release -m:1 -nr:false -p:UseHexalithProjectReferences=false -p:NuGetAudit=false` -- expected: 0 warnings and errors.
- `tests/Hexalith.Tenants.Contracts.Tests/bin/Release/net10.0/Hexalith.Tenants.Contracts.Tests -method 'Hexalith.Tenants.Contracts.Tests.PackageGovernanceTests.Release_workflow_registry_drift_guard_fails_closed_on_every_unprovable_state' -method 'Hexalith.Tenants.Contracts.Tests.PackageGovernanceTests.Release_workflow_proves_the_tag_floor_covers_the_registry_before_approval'` -- expected: both pass.
- `git diff --check -- .github/workflows/release.yml tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs _bmad-output/implementation-artifacts/spec-release-version-floor-drift.md` -- expected: no findings.

## Suggested Review Order

**Version-floor logic**

- Four-slot numeric precedence closes nonzero NuGet revision drift.
  [`release.yml:229`](../../.github/workflows/release.yml#L229)

- Revision-aware recovery prevents impossible three-part tag advice.
  [`release.yml:253`](../../.github/workflows/release.yml#L253)

**Behavioral proof**

- Real-shell tests cover registry drift and fail-closed evidence handling.
  [`PackageGovernanceTests.cs:929`](../../tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs#L929)

- Boundary fixtures pin zero, nonzero, numeric, and lower-core revision ordering.
  [`PackageGovernanceTests.cs:1000`](../../tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs#L1000)

**Deferred follow-up**

- Pre-existing parser and operator-documentation gaps remain explicitly tracked.
  [`deferred-work.md:1048`](deferred-work.md#L1048)
