---
status: done
date: '2026-10-31'
baseline_commit: 9e5a991d0ba304bac7b1a5809cd71797b47d78ed
resume_baseline_commit: 2ec938397c5b330cf1e694e8cc442cb4ad022f2b
sdk_version: '5.1.0'
context:
  - AGENTS.md
  - references/Hexalith.AI.Tools/hexalith-llm-instructions.md
  - _bmad-output/project-context.md
  - .editorconfig
  - .gitattributes
  - global.json
  - Directory.Build.props
  - Directory.Build.targets
  - Directory.Packages.props
  - docs/development-route-catalog.md
---

# Qualify the latest stable SDK catalog selection

## Current Resume Scope — 2026-11-15

The user resumed this spec with **use latest SDK**. This section supersedes the
historical 3.130 implementation instructions and old branch/publication plans
below. Preserve those records as historical evidence. The original
`baseline_commit` remains unchanged; `resume_baseline_commit` identifies the
starting source for this invocation's changes and review.

NuGet.org currently lists `Hexalith.EventStore.Client` at stable **5.1.0**
(`https://www.nuget.org/packages/Hexalith.EventStore.Client`). The repository's
`Directory.Build.props` already defaults to 5.1.0, and `Directory.Build.targets`,
`PackageGovernanceTests.cs`, and the ordinary `published-catalog-http` CI job
already enroll that exact finite selection. The task is to qualify this current
selection, repair any actual gap, and leave durable local evidence. Do not
downgrade to 3.130 or make speculative future versions admissible.

### Constraints and Code Map

- Work in the Tenants superproject. Preserve the existing moved gitlinks for
  Builds, EventStore, FrontComposer, Memories, and Platform; do not modify their
  source, initialize nested submodules, stage, commit, branch, push, or dispatch
  remote workflows. Do not change historical release/source claims.
- Use `run-with-temp COMMAND [ARGUMENT ...]` for disposable verification jobs.
  Temporary repository copies, package caches/feeds and build output belong
  beneath its `$TMPDIR`. Keep useful logs and receipts outside that directory,
  under `_bmad-output/implementation-artifacts/evidence/latest-sdk-catalog-qualification-2026-11-15/`.
  Wait for all children before returning. Run jobs sequentially.
- Follow the `published-catalog-http` job in `.github/workflows/ci.yml`: actual
  package-mode Release builds of Contracts.Tests and IntegrationTests, normal
  public NuGet restore, compiled owner enrollment/catalog discovery, generated
  HTTP contracts, and fresh Contracts/Server pack-and-consume verification.
  Local package-mode Release qualification is explicitly within this task.
- `tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs` owns the
  finite selectors, legacy floor, source-owner boundaries and publish guards.
  Test actual evaluated MSBuild behavior, including ordinary/native selections.
- `tests/scripts/test_catalog_package_consumer.py`,
  `tests/scripts/test_catalog_artifact_guards.py`, and
  `tests/scripts/test_hosted_package_profile.py` own provenance, stale actual-PE
  packing and hosted-profile refusals. Use the real maintained tools and tests;
  do not substitute a local feed for published SDK archives or weaken gates.
- `scripts/verify-catalog-package-consumer.py` verifies actual freshly packed
  Tenants Contracts/Server archives and their loaded compiled declarations.
  A scratch feed containing those local Tenants candidates is permitted by this
  existing tool; all SDK dependencies must come from public NuGet.org.
- Allowed changes are this spec, the durable evidence directory, and the
  existing selector/configuration/governance files only if current qualification
  exposes a real defect. Existing coverage may prove correct behavior without
  redundant production edits or new tests. Runtime enrollment/trust, routing,
  authorization, activation and legacy compatibility policy stay unchanged.

## Tasks & Acceptance

- [x] Verify the current stable SDK from official public NuGet metadata; record
  package identities, exact selected versions, embedded producer source and
  archive hashes for the restored EventStore SDK cohort in a durable receipt.
  Given normal public restore, when the current default graph is evaluated,
  then its current runtime SDK packages select 5.1.0 with no SDK candidate feed.
  The explicitly frozen Testing.Integration 3.128.4 harness remains separate.
- [x] Build `tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj`
  and `tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj`
  in isolated package-mode Release with warnings as errors. Given the actual
  current published binaries, when both graphs compile, then there are zero
  warnings/errors; report exact commands and any genuine blocker separately.
- [x] Run the complete affected selector/floor/native/publication controls and
  maintained Python provenance/stale-pack/profile controls. Given qualified
  stable 5.1.0 and supported aliases, when evaluated, then they are accepted;
  prereleases, nonzero revisions, malformed metadata and future unknown cohorts
  are refused, while legacy/source ownership boundaries remain enforced.
- [x] Run `PublishedTenantCatalogTests`, `TenantCatalogTests`, and all
  `TenantsApiGeneratedControllerTests` cases through the maintained project-level
  test runner with fail-skips/minimum counts from CI. Pack and consume actual
  Contracts/Server archives through `scripts/verify-catalog-package-consumer.py`.
  Given current 5.1.0 binaries, when discovery and HTTP tests execute, then all
  expected cases pass without skips and candidate archive/DLL identity agrees.
- [x] Record exact commands, counts, results, source identifiers, changes and
  remaining limits here and in durable credential-free receipts. Given the
  pre-existing five moved gitlinks, when reviewing this invocation, then they
  remain untouched and are excluded from its claimed implementation changes.
  Local evidence does not claim exact-source remote CI, publication or live
  Production qualification.

## Current Local Qualification — 2026-11-15

The complete current package qualification passed. The existing 5.1.0 default,
finite selectors, aliases, publication guards and maintained tests required no
production, configuration or test edits. This invocation changes only the
current task checklist, this qualification record and the durable evidence
directory. Historical records below are retained unchanged.

Official public [Client metadata](https://api.nuget.org/v3-flatcontainer/hexalith.eventstore.client/index.json)
and the [NuGet gallery](https://www.nuget.org/packages/Hexalith.EventStore.Client)
confirm stable 5.1.0. The two actual Release graphs restored ten current
EventStore SDK package identities at exactly 5.1.0. Each restored archive was
independently compared with its public NuGet download and has an exact SHA256,
package ID/version and embedded repository/source receipt. All ten name producer
`49084f66f8d6545cae442875355a7066d5f5e73d`. The frozen
`Hexalith.EventStore.Testing.Integration` 3.128.4 harness remains separate, with
producer `717a06e16c3afd81b904eaf4de646b74b13262d2`; its public archive also
matches. No SDK candidate feed, cache injection or gate waiver was used.

The actual qualification ran once, sequentially, under:

```bash
run-with-temp python3 _bmad-output/implementation-artifacts/evidence/latest-sdk-catalog-qualification-2026-11-15/qualify.py > _bmad-output/implementation-artifacts/evidence/latest-sdk-catalog-qualification-2026-11-15/qualification.log 2>&1
```

The reproducible driver copies tracked superproject files and only the existing
root-declared Builds/Platform inputs into its private `$TMPDIR`, creates an
empty private NuGet package/HTTP cache, and restricts restore to
`https://api.nuget.org/v3/index.json`. It sets `RepositoryCommit` to
`2ec938397c5b330cf1e694e8cc442cb4ad022f2b` and uses the pinned .NET 11.0.100
SDK. Both project builds use the ordinary default without an SDK version
override:

```bash
catalog_args=(-p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true
  -p:HexalithEventStoreFromSource=false -p:HexalithFrontComposerFromSource=false
  -p:HexalithMemoriesFromSource=false)
for catalog_suite in IntegrationTests Contracts.Tests; do
  dotnet build "tests/Hexalith.Tenants.${catalog_suite}/Hexalith.Tenants.${catalog_suite}.csproj" \
    --configuration Release -warnaserror -m:1 -nr:false "${catalog_args[@]}" \
    --source https://api.nuget.org/v3/index.json
done
```

Each build reports **0 warnings and 0 errors**. All tests use the maintained
project-level Microsoft.Testing.Platform runner, with `--fail-skips on` and the
CI minimum counts for catalog, HTTP, owner-source and bootstrap selections.
The complete `PackageGovernanceTests` class also passes, including the real
MSBuild floor, source-owner, ordinary/native selection and publication controls.

| Verification | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| PackageGovernanceTests (complete class) | 556 | 0 | 0 |
| PublishedTenantCatalogTests | 1 | 0 | 0 |
| TenantCatalogTests | 2 | 0 | 0 |
| TenantsApiGeneratedControllerTests (minimum 66) | 67 | 0 | 0 |
| TenantOwnerSourceGatewayTests (minimum 25) | 27 | 0 | 0 |
| BoundBootstrap* (minimum 13) | 15 | 0 | 0 |
| AuthenticatedBootstrapCannotAssignDifferentTarget | 2 | 0 | 0 |
| Commands_endpoint_returns_problem_details_for_domain_rejection | 1 | 0 | 0 |
| Python archive provenance/consumer controls | 14 | 0 | 0 |
| Python stale actual-PE/foreign-owner pack controls | 2 | 0 | 0 |
| Python hosted package profile controls | 2 | 0 | 0 |

All **671 .NET cases and 18 Python controls** pass. Fresh Contracts and Server
archives at candidate `0.0.0-catalog-qualification.20261115` pass ordinary pack
validation and `scripts/verify-catalog-package-consumer.py`. Its separate
package-only restore uses only the two local Tenants archives plus public SDK
dependencies, verifies candidate source/archive/DLL identity, and discovers
**24 routes, 24 operations and 13 adapters**. No candidate was published.

Durable credential-free evidence is under
[`evidence/latest-sdk-catalog-qualification-2026-11-15/`](evidence/latest-sdk-catalog-qualification-2026-11-15/).
`commands.json` records every expanded command, working directory, UTC start/end
and exit code beside the individual logs. `public-archive-provenance.json`
records all eleven restored archives; `restored-graphs.json` records exact
graph selections and public restore sources; `catalog-package-consumer.json`
records fresh candidate hashes and compiled discovery. `source-before.json`,
`source-after.json` and `source-input-sha256.json` bind the actual source inputs.
All children finished, the wrapper exited successfully, and its scratch tree,
package caches and build outputs were removed.

The required historical story audit was also run exactly as follows:

```bash
run-with-temp python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-qualify-sdk-3-130-catalog-selection.md
```

It returns **1**, recorded in `story-gitlinks.log`: its unchanged historical
`baseline_commit` 9e5a991-to-current comparison finds nine pointer differences,
eight undeclared in the historical File List. That audit does not use
`resume_baseline_commit`. This pre-existing historical mismatch is retained as
a separate audit limitation; no historical pointer ownership was invented and
no historical declarations, baseline or gitlinks were changed to make it pass.
The before/after receipts independently prove all nine original submodule
indices, checked-out HEADs and working-tree statuses remained identical.
In particular, the five pre-existing moved Builds, EventStore, FrontComposer,
Memories and Platform gitlinks are preserved and excluded from this invocation's
implementation changes. Builds `73e60af8a1fcdf3f06726d5d1b6e1199cce778ee` and
Platform `f6f85032598865b9123f16364448bfd02c0ff5b0` are qualification inputs.

This is local working-tree qualification against the recorded source base and
existing dependency inputs. It does not establish exact-source remote CI,
release/publication acceptance, native distributed execution or live Production
trust. No branch, stage, commit, push, remote dispatch or nested initialization
was performed.

## Review Triage Log

Review covers this invocation against `resume_baseline_commit`; the complete
original-baseline diff is retained separately because it includes 42,193 lines
of intervening work. No review layer was skipped. All three independent layers
returned before triage. Findings below are graded individually before grouping.

| ID | Layer | Verdict | Evidence and route |
| --- | --- | --- | --- |
| B1 | Blind | medium | Root `.gitignore:110` excludes every retained `.log`, so logs would be missing from a recorded change. Patch: add an evidence-directory exception. |
| B2 | Blind | medium | The driver overwrites source/command receipts before network and process operations that may throw, leaving old success files beside a partial replay. Patch with B3/E1/V1: isolate each replay from the original results. |
| B3 | Blind | medium | The driver does not regenerate the original manually retained summary or cleanup assertion. Reusing its directory can mix that summary with new results. Patch with B2/E1/V1: separate invocation output and document the original snapshot versus replay receipts. |
| B4 | Blind | false | The historical audit is explicitly a separate command in Current Local Qualification, and its failure is separate from successful SDK qualification in the README and summary. The driver does not claim to automate that independent audit. |
| B5 | Blind | low | Commands record the package graph but not the actual CLI selection; the consumer's separate scratch directory lacks the repository's SDK policy. Patch: retain observed toolchain output and provide the same existing global.json policy above both scratch projects. This directly closes a provenance omission. |
| B6 | Blind | false | The receipt describes hashes of copied tracked source before the explicitly generated public-only NuGet configuration. Every build also carries `--source https://api.nuget.org/v3/index.json`, and all retained restored graphs record only that source. It does not claim the generated config was a tracked source input. |
| B7 | Blind | false | The Builds manifest digest includes every tracked file, including root properties, schemas and Tools inputs. The additional individual-hash map is selective; changing an omitted individual row's source still changes the complete manifest digest. |
| B8 | Blind | false | The command filters only by the complete class, with fail-skips enabled; the actual retained run executes all 556 cases. A minimum count of one prevents an empty discovery and does not truncate discovery. No removed or unexecuted current case was identified. |
| B9 | Blind | low | The driver has no outer process timeout, but it is a foreground manual reproduction under run-with-temp, whose signal handler terminates the job group, waits and cleans its workspace. No hanging current job exists. Reject: an additional timeout policy adds complexity for a hypothetical manual-run failure. |
| B10 | Blind | medium | `git -C` in an uninitialized submodule directory can discover the enclosing repository, falsely recording its HEAD/status as the module's. Patch: distinguish uninitialized paths using the actual repository root. |
| B11 | Blind | false | Both required default-path assets receipts are present, include their current 5.1.0 SDK identities, and follow successful builds and nonempty project-level tests. No alternate output path is passed and no file deletion occurs between those commands and scanning; no reachable false certification was shown. |
| E1 | Edge | medium | Metadata, archive or process-start failure can leave previous successful outcomes beside partial overwritten files. Same defect as B2/B3/V1; patch by writing replays to a new invocation directory. |
| V1 | Verification | medium | The manually retained successful summary is never invalidated by the old replay driver. Same defect as B2/B3/E1; isolate replay receipts and clarify the README links. |

The surviving findings form four patch groups: retain logs (B1), isolate replay
evidence (B2/B3/E1/V1), record actual toolchain policy (B5), and identify module
boundaries (B10). They add no product surface and do not alter SDK admission.

### Review Resolution

All four patch groups are resolved. A directory-local `.gitignore` retains the
credential-free logs without changing root ignore policy. Each replay now writes
to a fresh invocation directory with its own incomplete/failed/passed result;
the original successful receipts and manually assembled summary remain immutable.
Replay measurements record the actual toolchain, while the shared scratch parent
provides the same existing global.json policy to both project ancestry paths.
Optional module records check the actual Git repository root before reading HEAD
or status, and identify uninitialized paths explicitly.

The implementation agent and parent independently ran:

```bash
run-with-temp python3 -B _bmad-output/implementation-artifacts/evidence/latest-sdk-catalog-qualification-2026-11-15/test_qualify.py
```

All four focused safeguards pass. They cover failed/successful replay isolation,
interruption, measured policy inheritance and optional-module boundary handling.
The SDK graph was not rerun for evidence-only patches; its original build/test
inputs and receipts are unchanged. The README distinguishes that original
qualification snapshot from future replay output and does not retroactively
claim toolchain measurements during the original run. No finding was deferred;
the separately documented pre-existing historical gitlink audit still returns 1.

This invocation completes the current local latest-SDK qualification. No commit
or remote operation was made; the five pre-existing moved gitlinks remain intact.

## Historical Qualification Record

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
- `tests/Hexalith.Tenants.Contracts.Tests/SourceReferenceWorkflowGovernanceTests.cs`
- `_bmad-output/implementation-artifacts/spec-qualify-sdk-3-130-catalog-selection.md`

## Change

Extend the finite stable catalog API selection with only 3.130.0, 3.130.1 and the
pending 3.130.2 maintenance selection, with their supported
same-binary zero-revision/metadata variants. Retain 3.129.0 and 3.129.1 support,
the default 3.129.0 selection, explicit 3.128.4 compatibility, exact producer source
ownership, native unknown/prerelease refusal and actual-PE publication validation.
Keep 3.130.3, 3.131.0, nonzero revisions and prereleases unqualified.

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

- `references/Hexalith.EventStore`
- `.github/workflows/ci.yml`
- `.github/workflows/source-reference.yml`
- `.github/workflows/story-guards.yml`
- `.github/workflows/commitlint.yml`
- `.github/workflows/codeql.yml`
- `Directory.Build.targets`
- `tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs`
- `tests/Hexalith.Tenants.Contracts.Tests/SourceReferenceWorkflowGovernanceTests.cs`
- `_bmad-output/implementation-artifacts/spec-qualify-sdk-3-130-catalog-selection.md`

## Completion Notes List

The integrated owner qualification retains the EventStore source dependency move
from `e014af6c3f1c67732485f24012cdbe528a12b0f6` to
`316da6cf7c12582dc4cddeecb6f2f06f64588817`. This source revision supplies the
development catalog owner qualification consumed by the merged main graph; it
does not change the published 3.130.0/3.130.1 package selections or the frozen
3.128.4 bootstrap compatibility host.

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

## Qualify the finite workflow branch fixture

Exact4d CI41250098352 built and consumed packages successfully. Its Contracts
shard then exposed33 failures in SourceReferenceWorkflowGovernanceTests: the
existing branch-shape predicate accepted only the singleton main push list.
All failures shared that one baseline violation; the other six test shards
passed. Local execution of the whole affected class reproduces68cases with
33failures and zero skips before the correction.

Extend only that fixture's accepted push lists to `[main]` or
`[main, fix/qualify-sdk-3.130.1]`. Pull requests still require exactly main.
Keep main mandatory, refuse all additional or duplicate branches, and retain
every command, ordering, source-mode, floor, fail-skips, suppression-filter,
shell-override and dependency refusal. Add positive main-only/qualified-push
controls and negative arbitrary, wildcard, future, missing-main, duplicate,
extra, non-scalar and pull-request qualifier cases. No workflow job or product
source changes.

Run only the complete affected class after a warning-free public3.130.1 build.
The scope receipt must identify this intentional test-only difference while
proving all production/package/gitlink entries remain identical6f and every
workflow remains identical4d. Apply pinned commitlint, normal hooks and the
actual nonzero-before story guard, then push only the qualification branch.
Repeat its full exact remote checks; do not accept skipped downstream Aspire
from the failed4d unit gate as qualification.

Focused proof: the unchanged pre-fix class reproduces68total/33failed/0skipped.
After the finite fixture correction, its full84cases pass with0failures/skips
(901ms); its actual published3.130.1 Release build has0warnings/errors.
Logs are `/tmp/tenants-sdk1301-public-proof/workflow-governance-{red,build,green}.log`.
No broader local tests were run for this test-only correction.


## Pending SDK 3.130.2 maintenance qualification

Prepare this bounded draft from selected source8e785341 while the SDK owner
repairs method-specific technical reachability on its maintenance branch. At
preparation time 3.130.2 has no verified public archives or published source;
this draft is not qualified, committed, pushed or enrolled in remote CI.

Add only exact stable3.130.2 and its existing same-binary zero-revision/metadata
forms to the catalog API selector. Keep the default3.129.0, explicit3.128.4,
all previously qualified versions, exact source ownership and actual-PE pack
guards unchanged. Add equivalent selector positives and prerelease, nonzero
revision and next-patch3.130.3 refusal controls. Do not add declarations,
authorization, activation, package defaults or Production trust.

After the actual official fourteen archives and immutable SDK source are
verified, require normal public NuGet restore, warning-free builds, the existing
66 HTTP contracts and affected selector/floor/profile/pack/provenance controls.
Only then apply pinned commitlint, normal hooks and a normal push advancing the
existing exact qualification branch. Its full exact-source remote gates must
pass. Original checkout selection remains RootCI-coordinated. Published
Tenants6.0.0/source9e and its default release inputs remain unchanged.


## SDK 3.130.2 local public qualification completed

Official source is28e3f260bbaf4619d1a99d656fba04c0c532387b. All fourteen
actual NuGet.org archives are HTTP200 with exact IDs/version/source/repository
URL; receipt SHA256 is8e07e4b6be063dd23b60483e3dd920078dd78c1bcfc3b70e8341a0751a2ae312.
Normal public restore succeeds after using the supported no-http-cache option
to avoid a stale version index; the initial NU1102 evidence is retained. No
candidate feed, archive injection, global-cache cleanup or gate waiver is used.

Actual Release IntegrationTests, Contracts.Tests and published Tenants6 native
host builds have zero warnings/errors. All66 HTTP and83 selector/floor tests
pass, with zero failures/skips. Both ordinary stale/legacy pack guards, all five
archive provenance controls and both profile control groups pass. Restored
API/host SDK132 archives and output DLLs match the official source28e cohort;
the own public Contracts/Server pair still exactly matches Tenants6/source9e.

Proof is retained under /tmp/downstream-sdk132-drafts-wd24gzkq/proof/tenants132,
with qualified-public132-provenance.json beside it. The exact owner commit/push
and full remote checks remain required; local proof does not replace those
gates. Original selection remains coordinated with RootCI. No default package
or release input, production authority or previously qualified support changes.
