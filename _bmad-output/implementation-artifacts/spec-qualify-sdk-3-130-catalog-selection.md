---
status: in-progress
date: '2026-10-31'
baseline_commit: 9e5a991d0ba304bac7b1a5809cd71797b47d78ed
---

# Qualify the exact SDK 3.130.0 catalog selection

## Intent

Official SDK 3.130.0 is published at source
`484f4bd82fd2ef4b6c2653fdde57feeeb6d85a3c`. Its fourteen manifest archives
have been independently downloaded from NuGet.org and verified. Qualify one finite
source-build compatibility selection without guessing future API support.

The existing Tenants 6.0.0 release remains source
`9e5a991d0ba304bac7b1a5809cd71797b47d78ed`. Its five archives and retained
server/API images are unchanged and independently verified.

## File Scope

Allowed files for this story:

- `Directory.Build.targets`
- `tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs`
- `_bmad-output/implementation-artifacts/spec-qualify-sdk-3-130-catalog-selection.md`

## Change

Extend the finite stable catalog API selection with only 3.130.0 and its supported
same-binary zero-revision/metadata variants. Retain 3.129.0 and 3.129.1 support,
the default 3.129.0 selection, explicit 3.128.4 compatibility, exact producer source
ownership, native unknown/prerelease refusal and actual-PE publication validation.
Keep 3.130.1, 3.131.0, nonzero revisions and prereleases unqualified.

The alias adds no catalog declarations, authorization, activation, route, package
inventory or runtime environment settings. Existing publication/archive validation
and its selected default SDK cohort are unchanged. Unconfigured Production remains
unready until its owner enrolls and activates trust; no Production trust is invented.

## Qualification

Use a disposable checkout with the exact ordinary Builds a22 archive and existing
Platform hosting inputs, with nested submodules uninitialized. Restore solely from
`https://api.nuget.org/v3/index.json` into the isolated
`/tmp/tenants-sdk130-qualification-nuget` cache. Normal NuGet restore succeeds;
no candidate feed, mirror, global-cache cleanup or gate waiver is used.

Actual Release builds of the IntegrationTests graph, Contracts.Tests and the
published-peer native host succeed with zero warnings/errors against SDK 3.130.0.
Generated API and published-peer host assets select only SDK 3.130.0 packages.
The downloaded cache archives match the official source484 receipt byte-for-byte.
The native host's explicit hosted-test profile resolves published Contracts/Server
6.0.0 archives at source9e, with no own public project references or inline versions.
Default, native, source and publishing profile controls remain unchanged.

Focused results:

- All 66 existing generated HTTP/catalog contracts pass, zero failures/skips.
- All 59 selector, floor, source, native and publication-selection controls pass,
  including exact130 variants and future/prerelease refusals.
- Both ordinary legacy/stale-PE pack lifecycle and foreign-owner controls pass.
- All five archive provenance/refusal controls pass.
- Both default/publishing/hosted profile control groups pass.

The selector's earlier preparation proof against SDK 3.129.0 also passed all59
controls. The actual 3.130 qualification above now replaces the pending binary
qualification; source and archive claims for already published6.0.0 remain9e.

Durable proof: `/tmp/tenants-sdk130-public-proof/qualified-public-provenance.json`
and adjacent restore, build, HTTP, selector and refusal logs. Full owning CI must
pass at the exact pushed source before runtime freeze and final release acceptance.

## File List

- `Directory.Build.targets`
- `tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs`
- `_bmad-output/implementation-artifacts/spec-qualify-sdk-3-130-catalog-selection.md`
