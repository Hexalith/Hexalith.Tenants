---
title: 'DW-187: Invalidate undispatched editors when lifecycle changes'
type: 'bugfix'
created: '2026-10-21'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: '7b33d851d5637dfaf6f310f5cfc2f57479317d9b'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Metadata and configuration editors preserve obsolete unsent intent when tenant or projection lifecycle changes. A later recovery can silently reuse that draft or destructive confirmation.

**Approach:** Invalidate undispatched metadata/set editors and removal confirmation when the value pair of tenant status and projection lifecycle changes. Metadata/set editors close and discard their draft, requiring a deliberate reopen; removal requires fresh confirmation. Invalidate pending unsent continuations so a lifecycle round trip cannot dispatch an old preview. Same-lifecycle refreshes preserve drafts. Preserve sent, retained and accepted attempts, their tracking identity, late-result processing and terminal evidence. Keep existing availability, localization, focus recovery and causal confirmation rules. Cover rendered transitions, unchanged-lifecycle controls, delayed pre-dispatch work and in-flight/accepted retention. Close only DW-187 and update its maintenance evidence. No new API, dependency, persistence or topology change.

</frozen-after-approval>

## Implementation Notes

- Entry revision: `7b33d851d5637dfaf6f310f5cfc2f57479317d9b`; clean `main`. User authorizes commit/push on main.
- Small reversible change: three existing flow components, their existing tests, spec and deferred-work/maintenance evidence. No unresolved intent gap or irreversible implementation action. Earlier open items retain privacy, route, topology and owning-module dependencies.
- Reuse per-flow generation and retention guards. Compare lifecycle values rather than refreshed Detail/Context references. Remove already invalidates preview scope but retains matching confirmation text.
- Baseline Debug source build passes with zero warnings/errors. Aspire reports 23 Running/Healthy resources; telemetry does not establish distributed functional acceptance.
- Initial 19-case reproduction: corrected fixtures fail ten intended cases against entry production; fixed production passes all 19. Initial full UI passes 6740/6740, zero failures/skips.
- Independent Blind Hunter review identifies provisional-admission, prior-terminal-evidence and focus/uncertain-retention gaps. Corrections preserve busy admission until cleanup, invalidate unsent drafts independently of settled command evidence and reset provisional metadata only after its refusal releases ownership. Additional held-boundary and evidence/focus tests qualify these corrections.

## Review Triage Log

- Medium / patch: metadata's provisional RequestSent precedes the activity lease and second authority lookup. Lifecycle revision tracking now closes and clears the first undispatched draft after invalidated admission unwinds; actual sent/uncertain attempts retain ownership. New held-lease and held-second-lookup cases cover both boundaries.
- Medium / patch: set reset prematurely cleared submit ownership before an awaited lease returned. Preserve the active submit until its existing finally releases provisional activity, and prevent opening/queued opening while busy. New held-lease cleanup case qualifies ordering.
- Medium / patch: prior terminal set MessageId skipped invalidation of a newly edited draft. Clear inputs and invalidate preview independently, preserving terminal snapshot/message/audit evidence. New held post-rejection preview case qualifies late completion.
- Medium / patch: prior terminal removal MessageId retained old destructive confirmation. Clear confirmation on lifecycle changes whenever no unresolved attempt remains; preserve terminal evidence. New settled removal case requires fresh confirmation before another dispatch.
- Low / patch: reset/reopen focus was unqualified. Existing transition cases now assert focus to the persistent lifecycle section and reopened Fluent input through focus interop. This is component evidence, not native browser/assistive-technology qualification.
- Medium / patch: uncertainty and terminal retention lacked transition coverage. New actual ambiguous/settled submissions compare full snapshots and retained tracker ownership through lifecycle changes, preserving identity, intent and audit evidence.
- Review correction recheck accepts all six original findings, then identifies a liveness regression: waiting for every obsolete preview would prevent recovery if a provider never returned. Keep busy specifically from lease acquisition through cleanup; preview-only work invalidates and allows immediate reopening. Existing held-preview cases now reopen and enter a fresh draft before releasing the old provider, then verify the late result cannot replace that draft.
- Medium / patch (correction recheck): distinguish preview-only work from provisional lease acquisition. Track the latter per active submit; tenant replacement resets the local marker. Recover/reopen immediately before any lease, serialize cleanup after lease acquisition, and retain actual dispatched attempts.

## Final Verification

Final Debug source build: zero warnings/errors. Dedicated lifecycle cases: 28/28; final full UI: 6749/6749, zero failures/skips. Independent review and final correction recheck accepted; no new deferral. See [exact evidence](dw-187-validation-evidence.md).
