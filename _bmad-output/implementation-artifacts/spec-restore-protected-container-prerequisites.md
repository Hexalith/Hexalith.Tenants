---
title: 'Restore protected container publication prerequisites'
type: 'bugfix'
created: '2026-10-28'
status: 'done'
route: 'oneshot'
baseline_commit: '0a2adfcdc843cca34e1aad65b4324253a4642b19'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

The split release workflow prepares NuGet packages on one runner, but its protected publisher still builds the container and executes an arm64 smoke test on a separate runner. Restore the publisher's root-declared source build prerequisites and arm64 emulation before issuing its temporary NuGet credential, preserving exact-source admission, workflow provenance, protected publication, and credential-free package preparation.

</frozen-after-approval>

## Implementation Notes

- Investigation: Directory.Packages.props requires the root Builds catalog; Directory.Build.targets unconditionally imports Builds ContainerProvenance.targets. The protected checkout deliberately disables automatic submodules, and the candidate archive carries nupkgs, publisher helpers, identity and node_modules only. Setup on the separate preparation runner cannot supply files or binfmt registrations to the publisher.
- Independent investigation confirmed the shared PublishContainer helper restores and builds its own runtime packs; no bin/obj transfer, solution restore, buildx setup or new package dependency is needed. Ubuntu supplies Docker/curl/Python/jq; existing setup-dotnet and setup-node cover explicit toolchains.
- Scope: move the existing immutable QEMU setup to the protected job and reuse the exact approved Builds initialize-build action there, after prepared closure and latest-source gates and before OIDC issuance. Add mandatory workflow contract tests and negative mutations for missing, late, prematurely executed, conditional or continued-failure setup.
- Route: no intent gaps, no irreversible implementation operation, no new API, small workflow/test footprint. Publication is separately authorized by the user. Release attempt 40623819184 was cancelled before approval/publication rather than allowing predictable post-NuGet failure.

- Review patches: the QEMU action now uses the immutable Docker Hub index digest 400a4873b838d1b89194d982c45e5fb3cda4593fbfd7e08a02e76b03b21166f0 (verified amd64 and arm64 children). A mandatory credential-free arm64 container execution probe runs before OIDC login; comments now distinguish NuGet preparation from the protected container build.
- Companion synchronization: Memories advanced from fecd80ed0b01994b6ede45bbb59cdef35325bb3b to 86011d4dc1e16d0d8222e07e6a492faf8e2347c0 with its own bounded, read-only workflow-worker readiness fix and regression tests for the observed provisioning setup failure. This upstream test-only fix is included before the final release source is frozen; no nested submodules are initialized.

## File List

- .github/workflows/release.yml
- tests/Hexalith.Tenants.Contracts.Tests/PartialReleaseRecoveryWorkflowTests.cs
- references/Hexalith.Memories

## Review Triage Log

- High, patched: moving a privileged QEMU installer into the protected runner must not retain a mutable image tag. Both setup and the runtime probe use the verified immutable multiarchitecture digest.
- Medium, patched: successful registration alone does not prove arm64 execution. The actual pinned arm64 binary now runs before NuGet login; mandatory-step mutations and executable success/failure controls reject missing or bypassable checks.
- Low, patched: comments incorrectly claimed all builds remained in preparation. They now distinguish credential-free NuGet preparation from protected container publishing and runner setup.
- Medium, patched: the new shell fixture inherited CRLF from its C# raw string. Explicit LF normalization prevents an invalid Linux shebang; rebuilt executable controls pass on the CRLF checkout.
- Verification: initial focused release-governance suite passed 309 tests; final rebuilt runner passed all 18 prerequisite/probe controls with no failures or skips. Six executable workflow-closure tests passed on the final workflow. Debug source-reference build passed with zero warnings/errors. Clean-workspace MSBuild evaluation failed with MSB4019 without Builds and passed after supplying its Props. Real native/arm64 Docker runs verified the pinned image, failure without emulation, and success after installation; the original emulator registrations were restored afterward. No further concrete review defect remained.
