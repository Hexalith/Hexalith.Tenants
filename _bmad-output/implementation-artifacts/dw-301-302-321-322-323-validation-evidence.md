# Lifecycle maintenance validation — 2026-10-22

Entry revision: `4354b0e0687766c0995383f630ec54f9b2a29730`. Scope: exactly DW-301, DW-302, DW-321, DW-322 and DW-323.

## Behavior

- DW-301: metadata/configuration and lifecycle/membership proof reads use independent keyed scopes. Literal tenant and caller identity, generation retirement, multiple simultaneous reads, captured caller proof and operation-owned cancellation disposal remain enforced. Cancellation snapshots tolerate reentrant completion and a new-generation read. Both command-specific gateway proof methods share one unconditional detail read, without a previous snapshot or ETag.
- DW-302: supplied high-impact evidence determines lifecycle versions, including missing versions. Public scalar parameters adapt legacy callers only when typed evidence is absent. Preview, retained remount and submit fingerprints use that precedence. Authoritative proof rebasing remains separate.
- DW-321: page helpers share admission owner resolution, nullable tracker retention, exact-surface reclamation and bookkeeping cleanup. Route-only reclamation and lifecycle's active-surface exclusion remain explicit. Lifecycle focus shares the existing JS exception handling; unsupported exceptions propagate.
- DW-322: retained reads check only the requested tenant's expiry, including terminal tombstones, exact deadlines and regressed clocks. Existing mutation seams perform global cleanup. Against the original tracker, 3,000 reads across 256 retained tenants allocated **13,176,000 bytes** and the new allocation regression correctly failed. The replacement is required to allocate **zero bytes** for the same measured reads.
- DW-323: the existing count limits inconclusive status observations to **30**, alongside the existing five-minute deadline. Missing/transient and verified Received/Processing observations share the budget. Remounts preserve the count and command identity. Exhaustion exposes localized uncertainty under the existing lifecycle terminal-ownership policy. Stored-event evidence still permits the final authoritative projection read; readable terminal evidence determines its own outcome.

## Commands and initial results

The initial Debug source build passed with zero warnings/errors. The first state run passed **123/123**, zero failures/skips/not-run.

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -m:1 -nr:false

dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -class '*TenantLifecycleMaintenanceTests' -class '*TenantLifecycleAttemptTrackerTests' -class '*TenantLifecycleCommandSnapshotTests' -parallelMode none -result-trx /tmp/tenants-lifecycle-maintenance/state-focused-initial.trx

python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-301-302-321-322-323-lifecycle-maintenance.md
git diff --check
node_modules/.bin/commitlint --edit /tmp/tenants-lifecycle-maintenance/commit-message.txt --verbose
```

The final combined source build before independent review passed with zero warnings/errors, exit 0. Scoped `dotnet format whitespace` completed successfully. The exact full commit-message file passes the pinned `@commitlint/cli` **21.2.3**, zero problems/warnings. Fetch of origin/main found zero divergence from the entry revision.

## Initial failures and runtime limits

The first allocation-baseline build exposed a stale `_lifecycleProofTenantId` reference after helper extraction (CS0103). It was replaced with the helper's locked ordinal tenant comparison and a regression for caller changes and explicit invalidation. The corrected baseline build passed; the original tracker then failed the allocation assertion as intended, exit 1, measuring 13,176,000 bytes. Only this batch's tracker file was temporarily restored for that negative control, then the replacement was restored before normal validation.

The first expanded focused command combined class and method filters, selected zero tests, and misleadingly returned exit 0. The corrected selection uses class filters alone and checks parsed TRX test counts. The original zero-test log is retained. The initial story guard refused a missing `baseline_commit`; the spec now records the full entry revision and the unchanged-pointer guard passes.

Before editing, `aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --non-interactive` succeeded. `aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json` reported security Running/Unhealthy and dependent Tenants/EventStore/UI resources Waiting; Memories resources were Healthy. `aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj` succeeded. No healthy distributed-runtime baseline, authenticated-browser acceptance or release readiness is claimed.

## Independent review and corrections

The configured Blind Hunter reviewed 112,130 bytes of changed/untracked content and returned ten findings. Each is individually triaged in the spec. All were patched within this batch: dispatch-only read expiry; allocation measurement with 256 snapshot, dispatch and tombstone entries each; shared per-key expiry policy for reads and mutation sweeps; isolated count/deadline exemptions; complete non-success terminal-status boundary coverage; a rendered held final projection proof; absent-tracker lease polarity; competing-thread cancellation/disposal; actual EN/FR budget copy and recovery; and substantive, existing-path closure evidence validation. Independent source recheck accepts all ten corrections and identifies no new concrete regression. No new deferred record was created.

The corrected pre-review component/page selection executed **855 cases: 853 passed, two failed, zero skipped**. Both failures were new missing-version remount fixtures without a projection reader. The existing unsupported-reader behavior correctly terminalized tracking; those fixtures now provide a null-proof reader, exercising retained version gating without changing production terminal semantics. Initial failures remain in the raw evidence.

The original allocation negative control measured the snapshot-only workload described above. The final strengthened workload also populates 256 dispatch identities and 256 terminal tombstones; its measured reads must still allocate zero bytes. Only mutations enumerate collections, and both reads and sweeps apply one shared expiry policy.

The reviewed Debug source build passes with zero warnings/errors. The expanded focused component/page/state run passes **875/875**, zero failures/skips/not-run, and unconditional gateway proof checks pass **2/2**. Parsed TRX counters are checked, preventing another zero-selection success. The first complete run catches a divergent new `PollBudget` string in the component localizer double; the stub is corrected to the exact shipped value. The production copy and the suite-wide parity gate are unchanged by that correction.

The first complete UI run executed **7,398 cases: 7,396 passed, two failed, zero skipped**, exit 2, 1m55.178s. In addition to the corrected localizer stub, the unchanged `GlobalAdministratorsPageTests.OrdinaryRefreshAndProjectionNotificationShareCallerCancellationWithoutCancelingSiblingAuthority` hit its one-second bUnit wait at `GlobalAdministratorsPageTests.FiniteBounds.cs:381` (`PendingTimerCount` remained 1; the wait checked once). No production/test source for that case changed in this batch. This initial failure remains recorded separately from the final complete run; no timeout or test gate is weakened.

## Final verification and closure

The final Debug source build passes with **zero warnings/errors**, exit 0. Localizer parity passes **3/3**. The final complete UI run passes **7,398/7,398**, zero errors/failures/skips/not-executed, **exit 0**, 1m57.261s. Parsed final TRX confirms all **875 focused regressions and both gateway proof cases** pass in that same complete run, including the unchanged global-administrator timer-cleanup test. All 15 changed source/test hashes remain unchanged since the final build. The expanded 3,000-read allocation regression measures **zero bytes** with 256 snapshots, 256 dispatch identities and 256 terminal tombstones present.

```bash
dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -class '*LocalizerDoubleParityTests' -parallelMode none -result-trx /tmp/tenants-lifecycle-maintenance/localizer-parity-final.trx

dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug --no-build --no-restore --parallel none --minimum-expected-tests 7380 --report-xunit-trx --report-xunit-trx-filename results.trx --results-directory /tmp/tenants-lifecycle-maintenance/full-final

python3 _bmad-output/implementation-artifacts/dw-301-302-321-322-323-verify.py
```

The executed MTP floor is 7,380; final post-run validation requires **exactly 7,398 passed cases** and checks the identities of the focused and gateway cases. No excluded or skipped tests are accepted.

The preservation verifier passes: all **657 IDs** remain, exactly DW-301/DW-302/DW-321/DW-322/DW-323 close, all **652 unrelated record bodies** and preceding narrative remain unchanged. Selected historical fields reconstruct exactly from `pre-lifecycle-maintenance-` fields. The index exactly covers **127 open IDs across 117 rows**; totals are **530 done, 61 actionable, 15 blocked, 23 awaiting-decision, 6 accepted-limitation and 22 uncertain**. Archive, sprint, release-readiness and dependency pointers remain unchanged.

[Raw build/test logs, initial failures and negative control, full TRX reports, sanitized Aspire state, exact command arrays, source/assembly hashes, ledger/story checks and validated commit-message bytes](dw-301-302-321-322-323-raw-logs.zip) preserve the executed evidence. The archive uses this batch's explicit temporary evidence directory and excludes the original unsanitized Aspire output. The final fetch finds zero divergence from origin/main before committing. The exact full commit message passes pinned commitlint 21.2.3 with zero problems/warnings; whitespace and story gitlink checks pass.
