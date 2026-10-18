---
title: 'DW-608: Qualify existing-package hash refusals during recovery'
type: 'refactor'
created: '2026-10-18'
status: 'done'
route: 'oneshot'
baseline_commit: '1dd9613e27abe935002d9d3af3a1f476bfdd73a6'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** DW-608 remains open for executable proof that an existing NuGet package with different bytes stops partial-release recovery before publication. Recent work covers one mismatch, but its fixture also rejects the subsequent source lookup if the hash comparison is bypassed, weakening the independent acceptance evidence.

**Approach:** Qualify the existing production guard with stable source evidence, distinct reviewed package contents and mismatches at every existing-package position across package, container, release and mixed recovery. Require the exact checked prefix, package-specific refusal and unchanged recovery state before any publication, and use isolated copied-script mutations to demonstrate detection. Close only DW-608 after the owning test suite and independent review pass, preserving historical findings, unrelated work and release holds.

</frozen-after-approval>

## Implementation Notes

- Selected one cohesive Tenants-owned acceptance gap from the deferred ledger. Its recorded next action and current publisher settle intent. There are no human-only choices, irreversible actions or new public APIs. The working tree was clean; the existing branch and latest commits concern this publisher.
- Code map and footprint: reuse `RunPublisherAsync` in `tests/Hexalith.Tenants.Contracts.Tests/PartialReleasePublicationTests.cs`, the five IDs from `tools/release-packages.json`, real SHA256 and strict intercepted publication commands. Keep `scripts/publish-partial-release.sh` unchanged unless execution demonstrates a defect. Change only this test class, this spec, scoped validation/replay evidence, `deferred-work.md` and its `docs/maintenance-backlog.md` index. Preserve DW-590's source gate and independent DW-620 asset work.
- Investigation confirms the prior mismatch test uses one existing package and couples mismatched content to an unsupported final-main fixture response. Supply stable API responses for hash-mismatch cases, support a chosen mismatch index, and qualify all five positions for container/release recovery and four existing packages plus one missing package for mixed recovery. Use distinct contents/hashes per ID and differently ordered hash records to detect incorrect lookup or partial scanning. Matching controls must still publish exact expected artifacts.
- Validation: Debug source build, focused executable publisher tests, maintained full Contracts lane, scoped formatter, shell syntax, whitespace and story-gitlink checks. Copied-script controls must pass pristine production and reject bypassed, first-only, wrong-ID and late comparisons; intercepted publication remains local. Capture exact results and limitations durably. The baseline requires an isolated pre-edit Aspire snapshot; lifecycle and inspection have succeeded and the exact AppHost is stopped. This is environment evidence, not release qualification. The invoked workflow requests independent review and a validated local Conventional Commit; no push or live publication is requested.
- Implementation keeps the publisher unchanged. Added fifteen mismatch cases and one matching mixed-inventory control, decoupled stable source responses, per-ID package contents and reversed hash-record order. The Debug source build passes with zero warnings/errors, focused publisher 71/71 and maintained Contracts 435/435 pass with zero failures/skips. Formatting, syntax, whitespace and pointer checks pass. The replay runner rejects four harmful copies; its initial late-hash expectation was corrected from 21 to 20 because the all-missing matching control has no existing hashes. Exact commands, outcomes and limits are in `dw-608-validation-evidence.md`. Independent review and scoped ledger closure remain pending.
- Independent review accepted four test/evidence corrections: fresh replay directories with prior-summary invalidation, an explicit source build and recorded hashes before copying assemblies, exact case/failure diagnostics with bypassed guards reaching intercepted publication, and a multi-existing-package package-only inventory. Added four mismatch cases and one matching control. A fifth finding reproduces the pre-existing missing-expected-hash continuation through all three intercepted destinations; retain it separately as DW-654 with a tracked local reproduction and supplied-hash refusal control. This task qualifies supplied-hash mismatches; it does not claim recovery-evidence completeness or release readiness.
- Final independent recheck ran the normal replay, both negative replay controls and both missing/present probes, and confirms all four corrections without a remaining finding. Focused 76/76 and maintained Contracts 440/440 pass with zero failures/skips; source builds have zero warnings/errors. Only DW-608 closes while DW-654 remains open: 654 preserved IDs, 372 done/282 open and 254 index rows with exact open-ID coverage. All unrelated original record bodies/rows and selected historical fields, archive/sprint, production publisher and dependency pointers pass preservation checks. Concurrent SDK/quickstart edits are preserved and excluded from this task's commit. Pinned commitlint 21.2.3 validates the exact local candidate with zero problems/warnings. Acceptance is complete; status is done.

## Review Triage Log

- **medium / patch — stale XML accepted:** the reviewer reproduced successful replay after mutant launches exited before creating reports. Each invocation now uses a fresh report directory and removes its prior summary first; the tracked negative checker requires stale-report refusal.
- **low / patch — stale assembly possible:** standalone replay previously relied on the documented prior build. It now performs the Debug source build itself, fails on build errors and records the command plus assembly/source/publisher SHA256 evidence.
- **medium / patch — aggregate failures insufficient:** expected totals could hide the unsupported-source fixture error. The runner now checks exact case identities, per-variant failure sets, the call-sequence assertion and intercepted publication in every bypass failure. The tracked negative checker injects the old fixture error and requires rejection despite the expected aggregate counts.
- **medium / patch — package-only multi-package gap:** added a package-only inventory with four existing packages and one missing package, all four mismatch positions and a matching success control.
- **medium / defer — missing expected hash:** the unchanged publisher skips existing packages absent from its hash file. The local tracked probe exits 0 and reaches intercepted package/container/release publication when the expected hash is missing; with that hash supplied, the same differing bytes exit 1 at the existing guard. Recorded separately as DW-654 because this is a pre-existing recovery-state completeness defect outside DW-608's supplied-hash acceptance repair.
