---
title: 'DW-245/DW-265/DW-290 queued membership projection-refresh intent'
type: 'bugfix'
created: '2026-10-16'
status: 'done'
route: 'oneshot'
baseline_commit: 'a3f43655d68370389439c7d2c212ab7939bd164e'
review_loop_iteration: 0
---

<frozen-after-approval reason="user-authorized bounded bundle; settled owning acceptance">

## Intent

A manual projection refresh queued behind a status-only refresh in AddTenantMemberFlow
or ChangeTenantMemberRoleFlow loses its parent reload intent. Preserve that intent
with OR semantics for every coalesced batch, including requests arriving during a
replay. Consume each batch once; a later status nudge cannot downgrade it.

The owning contract is Story 2.1's frozen constraints, matrix, acceptance and review;
Story 2.4's queued-removal acceptance and Loop 3 chunk B explicitly defer this sibling
gap. DW-290 points to Story 3.2's review but describes the same membership defect.
The settled Story 3.4 review also requires unconditional consumption (short-circuit
OR previously produced redundant replay). No new product decision is required.

Given held status-only work, when a user requests projection refresh, then its
awaitable completes after a serialized replay invokes the parent reload. Given mixed
manual and nudge requests, when they coalesce, then one batch invokes the parent at
most once and independent requests during replay become the next batch. Given a
failure, when explicit pending work exists, then consume that work without inventing
an automatic retry; report thrown failures to awaiters after draining. Given disposal,
replacement or terminal evidence, when old work completes, then existing continuation,
retained identity, caller, lease and ownership guards still apply.

Status/notifications remain nudges. Only qualified authoritative projection evidence
can confirm. Preserve exact tracking/domain/aggregate, caller binding, retained
attempts, admission, CancellationToken.None for dispatched tracking, truthful lifecycle
and disposal ownership. Never dispatch another command to service refresh.

DW-248/DW-266/DW-267 remain separately tracked; no generic plumbing extraction or
reentrancy-primitive change. Preserve all B1–B8 and earlier DW bundle contracts,
inherited work, historical findings, archive/sprint and unresolved decisions. Change
no dependencies, gitlinks, gates, exclusions, coverage floors or timeout policies.
No stage/commit/push/deploy, production replay or data migration.

</frozen-after-approval>

## Implementation Notes

Plan established before production edits. Small local change, no intent gaps,
new public API or irreversible action: the rendered build workflow's oneshot route
applies. Explicit user preservation/no-commit instructions authorize working over
the inherited dirty tree and override the workflow's default clean-tree/commit steps.
This maintenance bundle has no sprint story-key transition.

Code map: the two Members/*.razor RefreshCommandStatusAsync methods own the queue;
RefreshCommandStatusCoreAsync and PublishResult retain lifecycle/proof/ownership
semantics. Existing Add/Change tests cover merged audit recovery, concurrent lookup
and callback self-nudges; TenantRetainedCommandFlowTests covers retained leases and
late/disposed work. New MembershipRefreshIntentTests will render actual Fluent UI,
submit actual commands, and hold status/parent/evidence operations with causal task
barriers. Count status lookups, parent reloads and evidence calls separately: status
nudges may read cached authoritative evidence without requesting a parent reload.

Implement local atomic enqueue/consume/idle handoff, without awaiting under a lock.
One batch has a pending-work bit and projection-intent bit; OR requests and consume
both together before awaiting. One owner drains; merged manual callers await that
owner. Do not retry failed batches without another request. Preserve explicit pending
requests on failure; retire the gate and fault awaiters deliberately. Keep the
AsyncLocal callback guard and all core continuation checks unchanged.

Validation: prove the defect red before production changes; focused actual Fluent
cases across both flows; existing member/proof/retained/guard regressions; Debug
source and maintained Release builds, unfiltered per-project UI lanes and unchanged
Chromium runner in both configurations. Independent context-free Blind Hunter review
and triage precede closure. Capture exact commands, failures, existing skips, blockers,
validation input hashes and preservation. Append readiness evidence and update only
the three canonical ledger records plus counts/index after acceptance passes.

Implementation/review notes (append-only):

- Current source reproduced the lost parent reload with held actual Fluent requests.
  A local lock now makes enqueue, OR/consume and idle handoff atomic; no await occurs
  under it. The existing AsyncLocal and core status/projection/ownership guards remain.
- Independent review exposed generation replacement loss in the first drain. Four
  same-instance tenant replacement cases failed with two reads instead of three.
  Pending batches now carry their attempt generation; new generations supersede old
  queued intent and are served by the existing owner after old work retires.
- Failure tests invoke the actual Fluent OnClick callback to observe flow exceptions;
  FluentButton's DOM event path handles exceptions itself. Happy-path cases still use
  actual DOM clicks. Callback fixtures capture their exact rendering flow.
- Actual unmount uses a rendered CascadingValue host with empty child content, then
  mounts a replacement. Disposing bUnit's child handle alone did not unmount the flow.
  Qualified late evidence may legitimately complete the retained attempt and be
  adopted by its replacement; late owner execution cannot drain disposed queued work.

## Review Triage Log

Independent context-free Blind Hunter: 41,097 bytes / 40.134 KiB;
N=min(floor(sqrt(40.134)+1),10)=7. All seven findings retained:

| Finding | Verdict / disposition | Evidence and resolution |
| --- | --- | --- |
| Current generation lost behind old owner | high / patched | Four failing held replacement cases; generation-tagged batch consumption keeps exact alpha/beta attempts distinct. |
| Add fixture submits existing member | medium / patched | Add starts with absent target; change starts with reader; matches TenantAggregate admission. |
| Status maximum misses downstream overlap | medium / patched | Replay tests assert exact status/reload/evidence counts after additional requests, before releasing each held downstream operation. |
| Maximum counter can overwrite overlap | medium / patched | Monotonic Interlocked overlap flag cannot regress from detected overlap. |
| No failure during replay regression | medium / patched | Initial and replay status/reload/evidence faults queue more manual work; assert exact dispatch/lookup/reload/evidence counts and failure observation. |
| Disposal does not actually unmount | medium / patched | Rendered host removes child and asserts controls absent before replacement; original handle-disposal fixture failure retained. |
| Idle handoff has no execution evidence | medium / patched | Sixteen concurrent release/arrival rounds per flow hold the successor too, enqueue more work and assert every exact batch count plus completion. |

No new product decision or generic extraction is introduced. DW-248/DW-266/DW-267
stay open and unchanged; the local iterative drain does not claim to complete their
shared three-flow design/extraction recommendations.

- Independent read-only recheck confirms all seven findings are addressed, with no
  unresolved concrete issue. It did not run tests; command evidence supplies execution.
- The narrow dotnet whitespace formatter emitted brace placement inconsistent with
  the required Allman baseline. Normalized only the new test file through the SDK's
  existing Roslyn parser; no dependency/configuration change. Final qualification
  rebuilds/tests bind the resulting bytes. Existing task guards and test timeouts
  remain unchanged.

- Final focused462/full UI5196 pass in both configurations with zero failures/skips;
  both maintained Chromium lanes pass. Exact commands, initial failures, blockers
  and preservation/count audits are linked from dw-245-validation-evidence.md.
