---
status: in-progress
date: '2026-10-31'
baseline_commit: 9e5a991d0ba304bac7b1a5809cd71797b47d78ed
---

# Qualify the exact SDK 3.130 catalog selections

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

- `.github/workflows/ci.yml`
- `.github/workflows/source-reference.yml`
- `.github/workflows/story-guards.yml`
- `.github/workflows/commitlint.yml`
- `.github/workflows/codeql.yml`
- `Directory.Build.targets`
- `tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs`
- `_bmad-output/implementation-artifacts/spec-qualify-sdk-3-130-catalog-selection.md`

## Change

Extend the finite stable catalog API selection with only 3.130.0, 3.130.1 and their supported
same-binary zero-revision/metadata variants. Retain 3.129.0 and 3.129.1 support,
the default 3.129.0 selection, explicit 3.128.4 compatibility, exact producer source
ownership, native unknown/prerelease refusal and actual-PE publication validation.
Keep 3.130.2, 3.131.0, nonzero revisions and prereleases unqualified.

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

- `.github/workflows/ci.yml`
- `.github/workflows/source-reference.yml`
- `.github/workflows/story-guards.yml`
- `.github/workflows/commitlint.yml`
- `.github/workflows/codeql.yml`
- `Directory.Build.targets`
- `tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs`
- `_bmad-output/implementation-artifacts/spec-qualify-sdk-3-130-catalog-selection.md`

## Qualified stable patch 3.130.1

SDK 3.130.1 was published concurrently at exact source
`1de42b706f52af0da6513f0b7663002606d17156`. All fourteen official archives
returned HTTP200 and matched their IDs, versions and embedded source. Runtime,
catalog and authentication sources are unchanged from 3.130.0. The added test
stream helper is unused by this qualification; no broader Works feature is adopted.

Extend only the finite selection to exact 3.130.1 and its same-binary aliases.
Retain 3.130.0 and add independent patch-prerelease, nonzero-revision and future
3.130.2 refusal rows in both native and packable evaluation controls. Defaults,
legacy support, public constructors, publishing and archive policy are unchanged.

The original 3.130.0 owner source af2802f6 completed exact CI41244858357,
Source41244858103, Story41244858090, Commitlint41244858432 and CodeQL41244858326
successfully, including the full Aspire job124671477034. That proof remains retained
in `/tmp/tenants-sdk130-public-proof/green-ci.json` and adjacent receipts.

Fresh public-only restore into `/tmp/tenants-sdk1301-qualification-nuget` succeeds.
Actual 3.130.1 IntegrationTests, Contracts.Tests and published Tenants6 peer-host
builds pass with zero warnings/errors. All66HTTP and71selector/floor cases pass
with zero failures/skips; pack2, provenance5 and profile2 controls also pass.
Runtime assets select only3.130.1; normal downloaded cache archives match official
source1de bytes, while published Tenants6 Contracts/Server remain exact source9e.

Durable patch proof:
`/tmp/tenants-sdk1301-public-proof/qualified-public-provenance.json` and adjacent
logs. Original checkout synchronization follows RootCI's explicit diagnostic
cleanup/thaw receipt. Exact new owner CI must pass before final ledger/release
acceptance; the prior130 result does not replace that requirement.

## Isolated exact-source workflow qualification

A separate session advanced main to 9fcc5e8b with new bootstrap and source-catalog
features, cancelling the original 6f34ac67 main CI41248178028 before completion.
Those features are outside the selected prerequisite scope. Preserve main and the
original selected checkout at6f; use a dedicated qualification branch rather than
a pull-request merge which would include that additional source.

Seed `fix/qualify-sdk-3.130.1` at exact6f, then add that one exact branch to the
existing push triggers for CI, Source-reference integration, Story Guards,
Commitlint and CodeQL. No job, condition, command, timeout, dependency, checkout
setting, release trigger or release-main gate changes. Push-context checkouts in
local jobs and shared workflows select this branch's actual event commit, and the
existing ref-specific concurrency leaves main runs independent. Seeding before
the CI-only push provides the real nonzero before-SHA required by Story Guards.

Validate the complete workflow files with actionlint, assert only those five
push-trigger lines and this spec differ from6f, and prove every other tracked
tree entry (including all source, tests, packages and gitlinks) is identical.
Run the actual changed-story guard on the real6f-to-qualification event. The
normal pinned commitlint and commit hooks apply; push only the isolated branch.
The full exact branch CI, Source-reference, Story Guards, Commitlint and CodeQL
must finish successfully before the selected-source remote gate is accepted.
This qualification does not publish from a side branch or alter existing
Tenants6.0.0/source9e publication claims.

## Recover the qualification ref after external pruning

The isolated qualification commit3dc758ed preserves all4,814 non-workflow/spec
entries from6f and passed actionlint, the actual nonzero-before changed-story
event, pinned commitlint and normal hooks. Its blocking HTTP job passed270cases
with zero failures/skips, published-catalog HTTP passed, and source-reference
integration passed.

A competing external sync then merged the branch into newer main60cc86e2 and
deleted its remote ref. CodeQL completed analysis but could not upload because
that exact qualification ref no longer existed. Restoring the same3dc ref
created a branch-creation push and superseded unfinished same-ref workflows.
Preserve those results and failures as evidence; do not weaken the strict
changed-story requirement for a real, nonzero before-SHA.

After the user confirmed the external sync stopped, add only this recovery note
to the now-seeded branch and perform a normal push from3dc. Every other tracked
entry, including all five workflow files, remains byte/mode-identical to3dc;
production, tests, package selectors and gitlinks remain identical to6f. Run
pinned commitlint and normal hooks and the actual new push-context story guard.
The complete CI, Source-reference, Story Guards, Commitlint and CodeQL workflows
on this exact recovery commit are the final selected-source remote gate.
Preserve newer main without merging, rewinding or pruning it; leave original
producer checkout selection and fresh runtime preparation to RootCI.
