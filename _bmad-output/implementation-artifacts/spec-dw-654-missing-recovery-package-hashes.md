---
title: 'DW-654: Refuse recovery without expected package hashes'
type: 'bugfix'
created: '2026-10-18'
status: 'done'
route: 'oneshot'
baseline_commit: 'ff7014107a8fe0c53315e43d8ccd6838f8a3d010'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** DW-654 records a verified recovery-publisher defect: an existing NuGet package without its expected SHA256 record bypasses content comparison and allows publication. Missing evidence must prevent recovery from changing any destination.

**Approach:** Fail closed for an existing package whose expected hash is absent, before package pushes, container login/publication or GitHub Release creation. Exercise absent evidence at every existing-package position across the supported destination inventories, with matching-evidence and mismatched-content controls. Close only DW-654 after verification and independent review, preserving historical findings, unrelated work and release holds.

</frozen-after-approval>

## Implementation Notes

- Scope is one concrete Tenants-owned defect selected from the deferred ledger. The recorded next action and preflight producer settle intent; there are no human-only choices, irreversible operations or new public interfaces. Entry branch is main with a clean working tree and recent recovery-publisher work. No external publication is part of implementation or testing.
- Code map: `scripts/publish-partial-release.sh` currently continues on an empty per-ID hash lookup. `scripts/validate-partial-release-recovery.sh` produces one lowercase SHA256 plus package ID per existing package. Reuse the existing comparison and final source gate; missing files already fail under strict shell execution. Keep the manifest, workflow, preflight, dependencies and DW-620 asset qualification unchanged.
- Test footprint: extend `tests/Hexalith.Tenants.Contracts.Tests/PartialReleasePublicationTests.cs::RunPublisherAsync`, retaining distinct package contents, reversed hash records, stable source responses, exact intercepted command sequences and unchanged-state assertions. Qualify an omitted record, empty hash file, missing file and blank matching record. Require package-specific missing-evidence refusals for empty lookups; absent files must fail without publication. Existing matching inventories, no-existing-package recovery, mismatched hashes, source refusals and prerelease classification remain covered. Local copied-script controls should show original continuation is detected.
- Validation: repository-required pre-edit Aspire state/telemetry snapshot; shell syntax; Debug source build; focused direct-assembly publisher cases; maintained full Contracts test project; scoped C# formatting; whitespace; story-gitlink and full entry-revision dependency checks. Record exact commands, failures and limits in `dw-654-validation-evidence.md`. After independent review passes, update only this record and its maintenance index row, retain superseded fields under `pre-dw-654-*`, and verify all other records, archive and sprint tracking are preserved.
- The original local probe reaches all intercepted destinations without its expected hash; the supplied-hash control refuses differing bytes. Added 39 cases: nineteen omitted records across all existing-package positions and twenty unusable/missing-file cases. An initial three-error CA1062 build failure is corrected at the helper boundary; the Debug source build then has zero warnings/errors. Original production fails 35/115 focused cases; the one-line refusal repair passes 115/115 focused and the maintained Contracts lane 479/479, with zero failures/skips. Both historical probes now refuse. Formatting, syntax, whitespace and pointer checks pass. Exact commands and environment limits are recorded in `dw-654-validation-evidence.md`; independent review is pending.
- Independent review corrections add the no-existing-package/absent-file success control, preserve valid earlier records in wrong-ID/blank-record cases, and supply `dw-654-check-variants.py` for a fresh Debug source build and isolated current/baseline/mutation replay. Production passes 66/66 selected cases; the exact entry-revision publisher fails the intended 35 cases; an unconditional file requirement fails the added control. The optional truncated-inventory probe independently reproduces the pre-existing final-line omission and is deferred as DW-655. No inventory/parser expansion is included in the DW-654 fix.
- Final independent recheck reruns the tracked replay into a fresh directory with a zero-warning/error build, confirms all three corrections and finds no remaining issue. Focused 116/116 and maintained Contracts 480/480 pass with zero failures/skips. Only DW-654 closes; DW-655 is added with the confirmed truncated-inventory reproduction. All 654 original IDs/fields and unrelated records/rows, archive/sprint, earlier evidence and dependency pointers are preserved: 655 IDs, 373 done/282 open across 254 exact-covering maintenance rows. Final whitespace, syntax and story-gitlink checks pass. Pinned commitlint 21.2.3 validates the exact local candidate with zero problems/warnings. Acceptance is complete; no push or live publication is requested, and release holds remain.

## Review Triage Log

- **medium / defer — unterminated inventory:** confirmed through the tracked isolated probe: an unterminated final existing-package ID is skipped and current production reaches all intercepted destinations with empty expected hashes. The same loop predates this change, and preflight normally emits newlines. Record separately as DW-655 for malformed/truncated inventory handling.
- **low / patch — no-existing-package absent-file control:** added a success case with all packages missing and no hash file. Its current-production success and failure under an unconditional hash-file mutation qualify the intended boundary.
- **low / patch — late unusable records:** wrong-ID and blank-record cases now change only the last existing package's record and retain the preceding valid per-ID hashes, requiring the exact successful comparison prefix.
- **low / patch — historical replay reproducibility:** added a tracked runner that rebuilds current tests and reads the original publisher from the full entry revision into an isolated fixture. Fresh reports, exact cases/failure mechanisms, exit codes and content hashes distinguish the original defect from unrelated fixture or stale-result failures.
