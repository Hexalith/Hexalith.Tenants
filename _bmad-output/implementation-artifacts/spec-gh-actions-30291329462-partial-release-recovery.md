---
title: 'Recover a partially published NuGet release without republishing immutable artifacts'
type: 'bugfix'
created: '2026-07-27'
status: 'in-review'
baseline_commit: '578770679b9d3bc3fdf2a8a78190f24cdad8576e'
review_loop_iteration: 0
context:
  - `{project-root}/.github/workflows/release.yml`
  - `{project-root}/scripts/validate-publication-preflight.sh`
  - `{project-root}/tools/release-packages.json`
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** GitHub Actions run `30291329462` passed source verification, restore, and build, but Semantic Release stopped before publication because it calculated `3.3.0` while `Hexalith.Tenants.Contracts`, `Hexalith.Tenants.Server`, and `Hexalith.Tenants.Aspire` already exist at that immutable NuGet version. The failure issue could not be created because the repository also lacks the `semantic-release` label. The existing release path has no safe way to complete the remaining package/container/GitHub Release work.

**Approach:** Add an explicitly operator-triggered partial-release recovery path that accepts only a reviewed existing version and exact main source, proves the release unit is recoverable, publishes only missing immutable artifacts, and completes the GitHub Release/tag evidence without weakening ordinary release collision checks. Make failure reporting independent of a pre-existing GitHub label.

## Boundaries & Constraints

**Always:** Keep normal Semantic Release fail-closed on any destination collision. Recovery must require protected production approval, exact lowercase source SHA on `main`, successful exact-source CI, the five-package manifest, and the approved Builds execution identity. Probe all five NuGet packages and the container tag before doing work; publish only absent package IDs; verify every package, container, tag, and GitHub Release resolves to the reviewed source/version. Diagnostics must not expose secrets.

**Ask First:** Changing the shared `Hexalith.Builds` reusable workflow, deleting/unlisting packages or tags, republishing an existing package, changing repository secrets/environments, or allowing recovery from a source other than the live `main` tip.

**Never:** Do not use `--skip-duplicate` to hide collisions, silently advance or override Semantic Release's version, rerun ordinary release as recovery, or create a recovery path that can publish a different source than the reviewed SHA.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Complete partial release | Existing version has some packages, missing packages, and no conflicting container/tag | Only missing packages and remaining release artifacts are published; final evidence proves the complete release unit | Stop before publication if any identity/source/probe is ambiguous |
| Already complete | All five packages, container tag, and GitHub Release/tag exist for the exact source | No publication occurs; recovery reports success after verification | Fail if any artifact resolves to another source |
| Full collision | All destinations exist but source/tag/release evidence is incomplete or mismatched | No publication occurs | State the exact recovery blocker without deleting or republishing |
| Invalid request | Version is absent/invalid, source is not live `main`, or CI is not green | No publication occurs | Fail closed with support-safe validation details |

</frozen-after-approval>

## Code Map

- `.github/workflows/recover-partial-release.yml` -- manually dispatched, protected recovery entry point and exact-source gates.
- `scripts/validate-partial-release-recovery.sh` -- support-safe validation of version, source, CI, package inventory, destination state, and approved execution identity.
- `scripts/publish-partial-release.sh` -- publishes only absent immutable package artifacts and the remaining container/release artifacts in a verified order.
- `scripts/verify-partial-release.sh` -- verifies all five packages, container, tag, GitHub Release, and source/version evidence after recovery.
- `tools/release-packages.json` -- authoritative package inventory consumed by recovery; no duplicate package list is introduced.
- `tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs` -- tests workflow/script invariants, collision safety, and no-skip-duplicate behavior.

## Tasks & Acceptance

**Execution:**
- [x] `.github/workflows/recover-partial-release.yml` -- add a protected `workflow_dispatch` recovery job with explicit version/source inputs and exact-main/source-CI gates -- prevent accidental recovery of a different release.
- [x] `scripts/validate-partial-release-recovery.sh` -- validate identities, manifest count, destination probes, and recoverable partial state -- keep ordinary release collision protection intact.
- [x] `scripts/publish-partial-release.sh` -- pack the reviewed source and publish only absent package IDs, then remaining immutable artifacts -- avoid duplicate pushes and partial ordering hazards.
- [x] `scripts/verify-partial-release.sh` -- verify package, container, tag, release, and source parity -- make completion auditable and fail closed.
- [x] `tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs` -- cover workflow inputs, protected gates, exact-source checks, and absence of duplicate-suppression flags -- prevent regression.

**Acceptance Criteria:**
- Given a reviewed `3.3.0` source with three existing packages and two absent packages, when recovery runs, then only the two absent packages are submitted and the completed release is verified against the exact source SHA.
- Given any existing artifact resolves to a different source or the container/tag state conflicts, when recovery is requested, then no publish command runs and the job fails with a support-safe reason.
- Given ordinary Semantic Release sees an existing destination version, when its preflight runs, then it still fails closed and directs operators to the recovery workflow.
- Given the GitHub `semantic-release` label is absent, when Semantic Release reports a failure, then failure reporting does not replace the primary release error with a label-validation error.

## Spec Change Log

## Design Notes

Recovery is intentionally separate from Semantic Release because the normal version calculation cannot safely reuse a version whose immutable destinations are partially populated. The recovery workflow must not infer a version from tags or silently choose a successor; the operator supplies the version and the workflow proves that the existing artifacts belong to the same release unit before completing it.

## Verification

**Commands:**
- `dotnet test tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Release --no-restore --filter FullyQualifiedName~PackageGovernanceTests` -- expected: governance tests pass.
- `bash -n scripts/validate-partial-release-recovery.sh scripts/publish-partial-release.sh scripts/verify-partial-release.sh` -- expected: shell syntax passes.
- `git diff --check` -- expected: no whitespace errors.
