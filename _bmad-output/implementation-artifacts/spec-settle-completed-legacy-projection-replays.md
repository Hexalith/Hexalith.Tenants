---
title: 'Settle completed legacy projection replay without blocking later events'
type: 'bugfix'
created: '2026-10-28'
status: 'done'
route: 'oneshot'
baseline_commit: '3d13880bceb43fbbb686ad5089170f9ce5f97eb3'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Tenants CI exposes a legacy EventStore delivery race: a duplicate activation at a completed head attempts another publication, which is correctly refused, but leaves a leased frozen intent that blocks subsequent events. Use the existing exact replay-completion proof to settle duplicates when the writer protocol is inactive, preserving source/candidate identity checks and the worker-enabled qualification.

</frozen-after-approval>

## Implementation Notes

- Owning implementation is Hexalith.EventStore, a root-declared submodule. Read its AGENTS, commitlint, build, editor, and replay-publication contracts; do not initialize nested submodules or load skills from references.
- Code map: ProjectionUpdateOrchestrator.LegacyReplay.cs, inactive-writer checkpoint branch; EventReplayProjectionActor.ReplayPublication.cs provides the existing read-only completion proof; ProjectionRebuildProductionPathTests.LegacyReplay.cs and LegacyReplayProductionHarness.cs provide deterministic production-path coverage.
- Plan: parameterize completed-trigger coverage for both writer modes and append a later head. Demonstrate the inactive-writer failure before adding the existing completion-proof call. Verify the full replay production-path class and failed live regression. Commit upstream changes separately, then record Tenants gitlink and this evidence.
- No intent gaps or new public API. Implementation is reversible; subsequent Git/publication operations remain the separately authorized release task.
- Deterministic red evidence: inactive-writer duplicate replay retained its leased head-2 intent; the active-writer control passed. The fix settles the existing exact completion proof under the lifecycle lease and retains the old recovery path when proof is absent.
- Added fail-closed mismatched-model coverage, original-operation recovery after finalization-save failure, later head-3 progress in both writer modes, and persisted model/receipt/checkpoint/save-count immutability assertions for duplicate delivery.
- Final validation: source build passed with zero warnings/errors; all 60 replay production-path tests passed without skips. Upstream source host remains consumed through the existing root gitlink; no test gate or runtime worker setting was weakened.

## Review Triage Log

- Medium, patched: false completed-proof path needed recovery coverage when the checkpoint persisted before finalization failed. The new injected-save-failure test resumes the original operation and settles its outbox without another handler preparation or notification.
- Low, patched: duplicate test previously asserted only notifications. It now compares committed model and receipt bytes, save count, and scoped checkpoint before and after duplicate delivery.

## File List

- `references/Hexalith.EventStore`

## Completion Notes List

- The listed root gitlinks declare the complete dependency movement from this record's starting commit through the release source, including independent synchronization commits. The scoped implementation and its validation remain recorded above.
