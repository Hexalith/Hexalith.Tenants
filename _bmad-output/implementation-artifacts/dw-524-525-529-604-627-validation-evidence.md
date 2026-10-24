# DW-524 / DW-525 / DW-529 / DW-604 / DW-627 validation evidence

Date: 2026-10-24. Owning repository: Hexalith.Tenants. Implementation validation
is recorded below; independent review, final staged-diff inspection and publication
are parent-owned and must be recorded before the spec is complete.

## Review-fix validation — current implementation

The earlier 24/30-case and full-suite results later in this document describe the
initial implementation candidate. They remain historical evidence. The following
focused results qualify the review fixes; parent-owned broad Debug, package-mode,
final review, staging and publication checks remain pending.

- Singleton authority/version now reuses the existing `IsCompleteGlobalReplay`
  decision. Incomplete higher histories preserve existing ordered authority and
  cannot prevent later canonical recovery. Populated initialization without a
  complete proof is refused before audit/authority writes, including negative,
  zero and positive suffix sequences. Existing audit completeness and exact
  duplicate-event proof rules remain unchanged.
- Tenant event input is normalized once with stable aggregate-sequence ordering,
  shared by detail and index folds. Nonpositive persisted owner watermarks fail
  preflight before writes. Added persisted unordered-history and zero/negative
  watermark controls. Fixed only newly added Allman indentation/blank lines.
- Ordinary `AuditStreamPersistenceTests.AuthorizedStore` now explicitly represents
  previously projected ordered authority (`tenant-sequence:1`). Its audit, owner
  isolation and canonical-proof assertions/data remain unchanged. Dedicated legacy
  refusal regressions remain in the replay and authenticated HTTP lanes. The unit
  null-event case now expects unsafe populated initialization to refuse before
  writes; an existing ordered authority remains usable for incomplete requests.
- Provenance refuses assume-unchanged/skip-worktree flags in root and declared
  checkouts and rechecks every captured dependency revision, index flags and
  cleanliness before output. Nineteen hermetic tests include four hidden-index
  controls, four late-mutation controls and a controlled clean runner. The latter
  checks exact helper root/dependency proof in `environment.json` and stops at a
  stubbed certificate command before any build, seed or measurement.
- Global authenticated route controls now prove successful ordinary legacy metadata,
  persisted authority/audit/version and response agreement; populated unknown-version
  authority fails without changing persisted data or ETags.
- Story Guards invokes the provenance suite explicitly. The existing blocking
  topology-free contract job now runs replay31, audit117 unique MTP cases and
  protected ProjectContract35, each with minimum counts and zero-skip guards.
  The advisory topology job and release workflow configuration remain unchanged.
- The current maintenance Remaining sentence is corrected to 77 open/71 rows;
  historical summaries and unrelated records remain preserved.

Exact focused commands (working directory remains the owning repository root):

```bash
dotnet build tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj -c Debug -p:UseHexalithProjectReferences=true -m:1
dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj -c Debug --no-build -p:UseHexalithProjectReferences=true --filter-class Hexalith.Tenants.IntegrationTests.ProjectionReplayIntegrityTests --minimum-expected-tests 31 --fail-skips on
dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj -c Debug --no-build -p:UseHexalithProjectReferences=true --filter-class Hexalith.Tenants.IntegrationTests.AuditStreamPersistenceTests --minimum-expected-tests 117 --fail-skips on
dotnet tests/Hexalith.Tenants.IntegrationTests/bin/Debug/net10.0/Hexalith.Tenants.IntegrationTests.dll -class Hexalith.Tenants.IntegrationTests.AuditStreamPersistenceTests
dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj -c Debug --no-build -p:UseHexalithProjectReferences=true --filter-method 'Hexalith.Tenants.IntegrationTests.CommandApiRuntimeIntegrationTests.ProjectContract*' --minimum-expected-tests 35 --fail-skips on
dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -m:1
dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug --no-build -p:UseHexalithProjectReferences=true --filter-class Hexalith.Tenants.Server.Tests.Projections.GlobalAdministratorProjectionHandlerTests --fail-skips on
dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug --no-build -p:UseHexalithProjectReferences=true --filter-class Hexalith.Tenants.Server.Tests.Projections.TenantProjectionHandlerTests --fail-skips on
python3 tests/scripts/test_tenant_audit_source_provenance.py
```

Both source builds pass with zero warnings/errors. Replay: **31/31**; protected
route: **35/35**; global handler: **12/12**; tenant handler: **34/34**; provenance:
**19/19**, all zero failures/skips. Audit persistence: **117/117 unique MTP cases**
and separately **119/119 direct xUnit cases**, zero failures/skips. Logs are under
`/tmp/tenants-five-review-fix/`: `integration-debug-build.log`, `replay-debug-tests.log`,
`audit-stream-mtp-debug-tests.log`, `audit-stream-direct-debug-tests.log`,
`project-route-debug-tests.log`, `server-debug-build.log`, `global-handler-debug-tests.log`,
`tenant-handler-debug-tests.log` and `provenance-tests.log`.

Initial review failures are preserved:

- Parent's initial complete audit-class reproduction reported 119 total /97 failed
  /22 passed, zero skipped because ordinary fixtures had populated unversioned
  authority. Log: `/tmp/tenants-five-validation/audit-stream-debug-before-review-fix.log`.
- The maintained audit command was first invoked with `--minimum-expected-tests 119`
  and otherwise the same arguments shown above. It executed **117 passing bodies**,
  zero failed/skipped, and exited **9** for the minimum-count policy. Log:
  `/tmp/tenants-five-review-fix/audit-stream-debug-tests.log`.
  `RejectionHistories` generates identical malformed `{}` rows twice for the
  one-field `GlobalAdminAlreadyBootstrappedRejection`, once for each full/simple
  discriminator. MTP collapses those two duplicate data identities; direct xUnit
  executes both, yielding119. No rows or assertions were deleted/altered. Parent
  accepted the verified discovery distinction, so CI guards the actual MTP total117.

Read-only YAML parsing and command assertions pass: all three blocking lanes have
the actual minimum and `--fail-skips on`, the explicit provenance step exists,
`jobs.ci` equals the prior advisory/shared configuration, and release.yml has no diff.
No broader suite or Release/package rebuild was rerun by the implementation agent
after this review-fix request; those final gates remain parent-owned.

## Scope and decisions

- DW-524: tenant index watermarks belong to ordinal aggregate identities and commit
  with index state through `ReadModelWritePolicy.UpdateAsync`. A frozen persisted
  watermark permits all new same-sequence siblings together; older/equal delivery
  preserves current names, lifecycle and memberships. Retries reload both other
  tenants and newer same-owner state. Initial admission runs before detail/audit
  writes and repeats inside the guarded transform.
- Initialization requires a contiguous prefix from sequence one. Persisted
  `TenantNotFoundRejection` / `InsufficientPermissionsRejection` events can precede
  creation; rejection-only prefixes establish empty owner slices without tenant
  rows. Successful changes require creation first. A populated legacy owner slice
  without a watermark is refused even with a contiguous prefix: that prefix could
  be older than its existing unwatermarked state. Other watermarked owners continue.
- DW-525: the custom authenticated `/project` route mirrors the SDK's internal
  legacy admission using existing public DTO fields. Both request hint families,
  metadata versions, stored identity/format/digest/provenance/adaptation families,
  invalid stored payload versions and every effective payload family are refused
  before store reads/writes. Null/legacy metadata and payload versions 1..1024
  remain supported. No transport or persistence infrastructure was copied.
- DW-627: global singleton authority uses the shared guarded policy. Newer/equal
  persisted ordered authority wins against older/equal/unversioned requests and
  competing commits; the response is serialized from the selected persisted value.
  A populated unknown/invalid-version legacy singleton is refused before audit and
  authority writes and rechecked on retries; missing/empty initialization is distinct.
  Audit ownership, canonical completion proof and existing merge logic are preserved.
- DW-529: source preflight runs before logs, results, preparation, build, seed or
  measurement. Root and every root-declared dependency must be clean, initialized
  and match the root commit's gitlink. Canonical root/dependency identifiers feed
  `environment.json`; rejected sources emit no revision-bound output. The helper
  binds gitlinks to the captured root revision and rechecks root identity/cleanliness.
- DW-604: the linked tenant schema runbook inventories exact current keys and
  describes complete authoritative histories, writer fencing, isolated generation
  or projection-only store selection, persisted validation, catch-up, cutover and
  rollback. Fixed-key intake supplies no automatic generation migration capability.
  Production execution remains a separately authorized operator action.

All frontmatter context files were loaded before implementation. Root build/test,
editor, attribute, architecture, event-contract and audit-rollout guidance was read.
Implementation was sequential. No production replay, deletion, source-event change,
dependency content/pointer change, nested initialization or release-policy change
was performed. The parent had inspected the existing Aspire graph healthy; it was
left running without restart. Runtime projection checks use isolated in-memory
platform stores and authenticated ASP.NET production-route fixtures.

## Validation commands and results

Working directory for every command below:
`/home/administrator/projects/hexalith/tenants`.

```bash
dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -m:1
dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug --no-build -p:UseHexalithProjectReferences=true --fail-skips on
```

Final Server source build: zero warnings/errors. Final full Server suite:
**1475 passed, zero failed/skipped**. Logs:
`/tmp/tenants-five-validation/server-debug-build-5.log` and
`/tmp/tenants-five-validation/server-debug-tests-4.log`.

```bash
python3 tests/scripts/test_tenant_audit_source_provenance.py
bash -n scripts/run-tenant-audit-performance.sh
npm run test:mode --prefix tests/performance/tenant-audit
```

Source provenance: **10/10 pass**, covering clean canonical identifiers, detached
matching dependency, root/dependency tracked, untracked and staged dirtiness,
mismatched clean checkout and missing checkout. Rejected runner invocations prove
preparation/results do not start. Shell syntax: pass. Existing preparation mode:
**4/4 pass**. Mode log: `/tmp/tenants-five-validation/performance-mode-tests.log`.
The provenance lane was rerun after binding gitlinks to the captured root revision.

Parent independently ran `python3 scripts/tenant-audit-source-provenance.py .`:
expected exit 1, `Root source is dirty; commit or isolate the intended source before
measuring.` No performance measurement was executed or claimed for this dirty
implementation tree.

```bash
dotnet build tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj -c Debug -p:UseHexalithProjectReferences=true -m:1
dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj -c Debug --no-build -p:UseHexalithProjectReferences=true --filter-class Hexalith.Tenants.IntegrationTests.ProjectionReplayIntegrityTests --minimum-expected-tests 24 --fail-skips on
dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj -c Debug --no-build -p:UseHexalithProjectReferences=true --filter-method 'Hexalith.Tenants.IntegrationTests.CommandApiRuntimeIntegrationTests.ProjectContract*' --minimum-expected-tests 30 --fail-skips on
```

Final Integration Debug/source build: zero warnings/errors. Persisted replay:
**24/24 pass**; protected real route: **30/30 pass**, zero failures/skips. The
route cases include both domains for all 16 rejection families, three legacy
metadata positive controls and the existing 11 workload/channel controls. Logs:
`/tmp/tenants-five-validation/integration-debug-build-3.log`,
`replay-debug-tests-2.log` and `project-route-debug-tests-2.log`.

```bash
dotnet build tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj -c Release -p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true -p:HexalithFrontComposerFromSource=false -m:1
dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj -c Release --no-build -p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true -p:HexalithFrontComposerFromSource=false --filter-class Hexalith.Tenants.IntegrationTests.ProjectionReplayIntegrityTests --minimum-expected-tests 24 --fail-skips on
dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj -c Release --no-build -p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true -p:HexalithFrontComposerFromSource=false --filter-method 'Hexalith.Tenants.IntegrationTests.CommandApiRuntimeIntegrationTests.ProjectContract*' --minimum-expected-tests 30 --fail-skips on
```

Release/package build: zero warnings/errors. Persisted replay: **24/24 pass**.
Protected real route: **30/30 pass**, zero failures/skips. Logs:
`/tmp/tenants-five-validation/integration-release-package-build.log`,
`replay-release-package-tests.log` and `project-route-release-package-tests.log`.

```bash
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-524-525-529-604-627-projection-integrity.md
git -c core.safecrlf=false diff --check
```

Gitlink guard: **PASS**, no `references/` pointer changes against the full baseline
above. Whitespace check: **PASS**; C# working files retain required CRLF. Source
identity inspection confirms all nine full dependency commits match the root gitlinks.
The final ledger/index reconciliation and independent-review results are recorded below.

## Initial failures and corrections

- Initial Integration Debug build failed with four CS7036 errors in the new replay
  tests: omitted required `UpdatedAt`/`DisabledAt` constructor arguments. Added
  explicit event timestamps. The next source build passed zero warnings/errors;
  its initial replay lane passed 20/20 and authenticated `/project` lane 30/30.
  The final lane includes additional rejection-history, ordinal-identity and legacy
  authority checks. Initial log: `/tmp/tenants-five-validation/integration-debug-build.log`.
- Initial full Server run: 1448 passed / 27 failed / zero skipped. Existing fixtures
  omitted new index admission reads, assumed missing indexes accepted suffixes,
  expected unguarded global `SaveAsync`, or counted only write-policy reads.
  Fixtures now explicitly supply previously established index state for suffix-only
  detail/audit tests, script admission snapshots, assert guarded writes and count
  the preflight. Genuine legacy incompatibility is covered by persisted-state tests;
  legacy data was not silently stamped with inferred detail watermarks.
- A subsequent Server run passed 1474 / failed 1: the cancellation test expected a
  single read instead of index preflight followed by detail. Updated that exact
  assertion. A repeated preliminary invocation still used the previous built
  assertion while a build completed; it also passed 1474 / failed 1. The final
  invocation awaited completed build and passed 1475/1475. Initial logs are
  `/tmp/tenants-five-validation/server-debug-tests.log`, `server-debug-tests-2.log`
  and `server-debug-tests-3.log`.
- Parent implementation inspection identified legitimate pre-creation rejections,
  unsafe suffix initialization, legacy global authority ambiguity and incorrect
  draft audit-key names. All were corrected before final validation. Current docs
  use `TenantAuditStreams`' actual `audit-tenants:system` and
  `audit-global-administrators:singleton` keys. This inspection is distinct from
  the required independent review.

## Canonical baseline and dependencies

Baseline root commit: `8767377dba1ed0d67dde4e8279bb7c5a2fe5a49e`.
Each checked-out dependency equals its root baseline gitlink:

| Root-declared path | Full canonical commit |
| --- | --- |
| `references/Hexalith.EventStore` | `f4df781cb7247c6a5f7549106b90d3748151edba` |
| `references/Hexalith.Commons` | `1663028fb6d9f46a0bf17919edeb21bb99be1587` |
| `references/Hexalith.AI.Tools` | `3f194e17174994d308ec84af9ee2b5aa68674d0d` |
| `references/Hexalith.FrontComposer` | `adc0dbe2228d6291df8b1fe5ba1dc8fb0080f631` |
| `references/Hexalith.Builds` | `5e54e892e69c31f346d69cb79f19f04bce8b79cc` |
| `references/Hexalith.PolymorphicSerializations` | `221ac4592ba908ae7661ba1fa9e2d1fcc23baee0` |
| `references/Hexalith.Memories` | `63327920814a293e0168986be212deb10b81dc00` |
| `references/Hexalith.McpCli` | `6b01b3dc57a241ebe5b3096be635469ff00d8221` |
| `references/Hexalith.Platform` | `4ebe6798fe8bd6c22258f20c90d6861b7137c927` |

The implementation candidate is a root working-tree change against this baseline;
these identifiers do not claim the uncommitted candidate is a clean performance
source. Parent-owned final inspection/commit identifies the finished source.

## Limits and pending finalization

Populated legacy index slices and populated unknown-version global authority require
a separately authorized coordinated rebuild. Shared multi-key writes remain independently
guarded rather than transactional; failures can leave partial successful writes and
must be retried from authoritative histories. A retry rechecks current shared state.
The runbook requires platform-supported isolation and selection; it supplies no
production reset command or unsupported evolution-proof bypass.

Unresolved revocation freshness, search, status, production provider/authority and
release qualification decisions remain unchanged. Production data was not mutated.
Independent review and publication evidence remain to be added by the parent.

## Ledger/index integrity and handoff

Read-only Python comparison with `git show HEAD:_bmad-output/implementation-artifacts/deferred-work.md`
confirms exactly DW-524, DW-525, DW-529, DW-604 and DW-627 changed among ledger
record blocks. All 657 unique IDs and the 652 unrelated blocks remain identical.
Original selected fields and prior verification/action values are preserved under
`pre-projection-integrity-*`; source specs, owners, dependency and related pointers
remain unchanged. The index has 71 rows, covering all 77 open identifiers exactly
once. Current dispositions are 580 done, 28 actionable, 11 blocked, 21 awaiting
decisions, 5 accepted limitations and 12 uncertain. The stale prior table was
reconciled from actual ledger contents. Test summary, index and ledger link this evidence.

The story gitlink guard and whitespace check were repeated after reconciliation and
passed. The spec's implementation task checkboxes are complete; its status remains
in-progress pending required independent review, final staged-diff inspection and
parent-owned publication. No broad release acceptance is claimed.

The exact hermetic fixture messages `test: create provenance fixture`,
`test: add fixture dependency` and `test: advance fixture dependency` were validated
with the owning root's pinned `@commitlint/cli` 21.2.3 before the final regression run:

```bash
./node_modules/.bin/commitlint --edit /tmp/tenants-five-fixture-commit-0.txt --verbose
./node_modules/.bin/commitlint --edit /tmp/tenants-five-fixture-commit-1.txt --verbose
./node_modules/.bin/commitlint --edit /tmp/tenants-five-fixture-commit-2.txt --verbose
```

All exited zero, with zero problems/warnings. Parent preserves validation evidence
at `/tmp/tenants-five-fixture-commitlint-evidence.txt`. The final root commit's exact
message validation and fetch/push evidence remain parent-owned.

## Parent independent review and final verification

The parent staged and inspected the complete intended diff, audited every acceptance/matrix row, and ran Blind Hunter, Edge Case Hunter and Verification Gap Reviewer concurrently. Every finding and disposition is in the spec. Accepted fixes include canonical singleton admission, nonpositive owner-watermark refusal, shared fold ordering, explicit ordinary audit authority, protected global success/refusal controls, hidden Git index flags and dependency rechecks, caller-level environment provenance and blocking CI wiring. Verification Gap confirmed all accepted findings resolved. Edge Case confirmed all accepted fixes and independently withdrew gap/split-group concerns after tracing the supported immutable complete-prefix transport. No new deferred work was added.

The final parent executed all 17 commands below; every command exited zero. The Debug full-class direct runner reported Server1475 via MTP, HTTP141, audit119, generated44 and replay31; package MTP reported replay31/project35/audit117/generated44/status18, with zero failures/skips throughout. Provenance19 and preparation4 passed. Both builds reported zero warnings/errors; actionlint, bash syntax and the gitlink guard passed. The final effective ledger audit confirms exactly the five selected blocks changed, 652 unrelated blocks unchanged, 657 IDs, 580 done/77 open and 71 exact-covering rows.

- `dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -m:1 -nr:false -warnaserror` — exit 0; log `/tmp/tenants-five-validation/parent-final-server-debug-build.log`.
- `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug --no-build -p:UseHexalithProjectReferences=true --fail-skips on` — exit 0; log `/tmp/tenants-five-validation/parent-final-server-debug-full.log`.
- `dotnet tests/Hexalith.Tenants.IntegrationTests/bin/Debug/net10.0/Hexalith.Tenants.IntegrationTests.dll -class Hexalith.Tenants.IntegrationTests.CommandApiRuntimeIntegrationTests` — exit 0; log `/tmp/tenants-five-validation/parent-final-command-debug-full.log`.
- `dotnet tests/Hexalith.Tenants.IntegrationTests/bin/Debug/net10.0/Hexalith.Tenants.IntegrationTests.dll -class Hexalith.Tenants.IntegrationTests.AuditStreamPersistenceTests` — exit 0; log `/tmp/tenants-five-validation/parent-final-audit-debug-full.log`.
- `dotnet tests/Hexalith.Tenants.IntegrationTests/bin/Debug/net10.0/Hexalith.Tenants.IntegrationTests.dll -class Hexalith.Tenants.IntegrationTests.TenantsApiGeneratedControllerTests` — exit 0; log `/tmp/tenants-five-validation/parent-final-generated-debug-full.log`.
- `dotnet tests/Hexalith.Tenants.IntegrationTests/bin/Debug/net10.0/Hexalith.Tenants.IntegrationTests.dll -class Hexalith.Tenants.IntegrationTests.ProjectionReplayIntegrityTests` — exit 0; log `/tmp/tenants-five-validation/parent-final-replay-debug-full.log`.
- `dotnet build tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj -c Release -p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true -p:HexalithFrontComposerFromSource=false -m:1 -nr:false -warnaserror` — exit 0; log `/tmp/tenants-five-validation/parent-final-integration-package-build.log`.
- `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj -c Release --no-build -p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true -p:HexalithFrontComposerFromSource=false --filter-class Hexalith.Tenants.IntegrationTests.ProjectionReplayIntegrityTests --minimum-expected-tests 31 --fail-skips on` — exit 0; log `/tmp/tenants-five-validation/parent-final-replay-package.log`.
- `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj -c Release --no-build -p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true -p:HexalithFrontComposerFromSource=false --filter-method Hexalith.Tenants.IntegrationTests.CommandApiRuntimeIntegrationTests.ProjectContract* --minimum-expected-tests 35 --fail-skips on` — exit 0; log `/tmp/tenants-five-validation/parent-final-project-package.log`.
- `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj -c Release --no-build -p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true -p:HexalithFrontComposerFromSource=false --filter-class Hexalith.Tenants.IntegrationTests.AuditStreamPersistenceTests --minimum-expected-tests 117 --fail-skips on` — exit 0; log `/tmp/tenants-five-validation/parent-final-audit-package.log`.
- `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj -c Release --no-build -p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true -p:HexalithFrontComposerFromSource=false --filter-class Hexalith.Tenants.IntegrationTests.TenantsApiGeneratedControllerTests --minimum-expected-tests 44 --fail-skips on` — exit 0; log `/tmp/tenants-five-validation/parent-final-generated-package.log`.
- `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj -c Release --no-build -p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true -p:HexalithFrontComposerFromSource=false --filter-method Hexalith.Tenants.IntegrationTests.CommandApiRuntimeIntegrationTests.CommandStatusContract* --minimum-expected-tests 18 --fail-skips on` — exit 0; log `/tmp/tenants-five-validation/parent-final-status-package.log`.
- `python3 tests/scripts/test_tenant_audit_source_provenance.py` — exit 0; log `/tmp/tenants-five-validation/parent-final-provenance.log`.
- `npm run test:mode --prefix tests/performance/tenant-audit` — exit 0; log `/tmp/tenants-five-validation/parent-final-performance-mode.log`.
- `bash -n scripts/run-tenant-audit-performance.sh` — exit 0; log `/tmp/tenants-five-validation/parent-final-shell-syntax.log`.
- `actionlint .github/workflows/ci.yml .github/workflows/story-guards.yml` — exit 0; log `/tmp/tenants-five-validation/parent-final-workflow-lint.log`.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-524-525-529-604-627-projection-integrity.md` — exit 0; log `/tmp/tenants-five-validation/parent-final-gitlink-guard.log`.

Exact final message validation:

```bash
./node_modules/.bin/commitlint --edit /tmp/tenants-five-final-commit-message.txt --verbose
```

Pinned CLI21.2.3 returned zero problems/warnings. The validated full message is `fix(projections): resolve five deferred integrity and provenance records`. Fixture-message validations are retained above.

Before publication, fetch found upstream `2ea496168635284301f75c05e53b32bec4583e80` one commit ahead of the original baseline. Its three-file query failure-reason compatibility correction is independent user work and will be preserved by rebase. Combined-source validation and the authorized push follow; the initial baseline is intentionally preserved.

## Upstream integration and final combined-source acceptance

Remote commit `2ea496168635284301f75c05e53b32bec4583e80` was preserved in history by a conflict-free rebase. Its attempted failure-category rename exposed a real build break in both modes: the exact Debug/source Server command and the Release/package Server command each exited1 with four CS0117 errors because the pinned `QueryAdapterFailureReason` supplies neither `ResponseMismatch` nor `QueryExecutionFailure`. Failing logs are `/tmp/tenants-five-validation/combined-server-build.log` and `combined-package-final-server-build.log`; no successful combined-source claim is based on those attempts.

Direct source, cached package XML and the pinned package API were inspected:

```bash
dnx dotnet-inspect -y -- member QueryAdapterFailureReason --package Hexalith.EventStore.Contracts@3.126.0 --all -S Fields -n 25
```

The inspection succeeded and confirmed `ActorResponseMismatch` and `ActorException`. The initial skill example using `--oneline` exited1 because this installed tool version does not accept that switch; `member --help` supplied the supported syntax. The exact corrected inspection is retained in `/tmp/tenants-five-validation/query-failure-api-inspection-fields.log`.

Eight affected symbol references in the two query handlers and audit expectations were corrected to the actual pinned public contract. This preserves the upstream commit in history, restores build compatibility in both modes, and changes no dependency content or pointer. Git independently confirms the resulting C# source tree equals the fully reviewed, successfully tested pre-rebase implementation `e365f143bef6d2b24b919e7d831dc4dcd92f05fa`; final combined validation was nevertheless rerun.

All 20 combined commands exited zero. They repeat the 17 full parent commands listed above, plus these three commands:

```bash
dotnet build tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj -c Debug -p:UseHexalithProjectReferences=true -m:1 -nr:false -warnaserror
dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Release -p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true -p:HexalithFrontComposerFromSource=false -m:1 -nr:false -warnaserror
dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Release --no-build -p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true -p:HexalithFrontComposerFromSource=false --fail-skips on
```

Final results: Server1475 in each mode; source HTTP141/audit119/generated44/replay31; package replay31/project35/audit117 unique MTP/generated44/status18; provenance19; preparation4. All tests have zero failures/skips. All four builds report zero warnings/errors. Shell syntax, actionlint and gitlink validation pass. The published tree has no remaining build blocker from the upstream rename.

| Combined check | Exit | Exact log |
| --- | ---: | --- |
| server-debug-build | 0 | `/tmp/tenants-five-validation/combined-repaired-server-debug-build.log` |
| server-debug-full | 0 | `/tmp/tenants-five-validation/combined-repaired-server-debug-full.log` |
| integration-debug-build | 0 | `/tmp/tenants-five-validation/combined-repaired-integration-debug-build.log` |
| command-debug-full | 0 | `/tmp/tenants-five-validation/combined-repaired-command-debug-full.log` |
| audit-debug-full | 0 | `/tmp/tenants-five-validation/combined-repaired-audit-debug-full.log` |
| generated-debug-full | 0 | `/tmp/tenants-five-validation/combined-repaired-generated-debug-full.log` |
| replay-debug-full | 0 | `/tmp/tenants-five-validation/combined-repaired-replay-debug-full.log` |
| integration-package-build | 0 | `/tmp/tenants-five-validation/combined-repaired-integration-package-build.log` |
| server-package-build | 0 | `/tmp/tenants-five-validation/combined-repaired-server-package-build.log` |
| server-package-full | 0 | `/tmp/tenants-five-validation/combined-repaired-server-package-full.log` |
| replay-package | 0 | `/tmp/tenants-five-validation/combined-repaired-replay-package.log` |
| project-package | 0 | `/tmp/tenants-five-validation/combined-repaired-project-package.log` |
| audit-package | 0 | `/tmp/tenants-five-validation/combined-repaired-audit-package.log` |
| generated-package | 0 | `/tmp/tenants-five-validation/combined-repaired-generated-package.log` |
| status-package | 0 | `/tmp/tenants-five-validation/combined-repaired-status-package.log` |
| provenance | 0 | `/tmp/tenants-five-validation/combined-repaired-provenance.log` |
| performance-mode | 0 | `/tmp/tenants-five-validation/combined-repaired-performance-mode.log` |
| shell-syntax | 0 | `/tmp/tenants-five-validation/combined-repaired-shell-syntax.log` |
| workflow-lint | 0 | `/tmp/tenants-five-validation/combined-repaired-workflow-lint.log` |
| gitlink-guard | 0 | `/tmp/tenants-five-validation/combined-repaired-gitlink-guard.log` |

The final result manifest is `/tmp/tenants-five-validation/combined-repaired-results.json`. The actual clean rebased checkout also passed source preflight with all nine matching dependencies before the compatibility correction; no performance measurement is claimed. Final publication follows with the previously validated message and no force push. Populated legacy projection records still require the documented coordinated rebuild; production migration was not executed.
