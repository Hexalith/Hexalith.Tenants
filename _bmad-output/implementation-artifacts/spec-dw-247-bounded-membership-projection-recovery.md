---
title: 'DW-247 bounded membership ProjectionPending recovery'
type: 'bugfix'
created: '2026-10-16'
status: 'done'
route: 'oneshot'
baseline_commit: 'a3f43655d68370389439c7d2c212ab7939bd164e'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="user-authorized settled August 25/26 maintenance contract">

## Intent

Implement only DW-247 in Hexalith.Tenants. The August 25/26 decision in
`deferred-work.md` requires a configured retry/time bound from unqualified
membership ProjectionPending to UnableToVerify, with status recovery and
support-safe guidance. Story 2.1's frozen requirements, acceptance and review
(`spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`),
Story 2.4's removal acceptance and review
(`spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md`), and canonical
architecture AD-7/AD-12 own projection confirmation, exact identity and admission.
The earlier review's reverted immediate UnableToVerify patch is historical;
the subsequent settled bound is authoritative. Current add/change/remove
snapshot confirmers and all three flow guards have no such bound. Existing
immediate-pending regressions remain valid before expiry.

Use one deployment-configured elapsed bound, `Tenants:Ui:MembershipProjectionTimeout`,
with a five-minute default. Accept positive finite invariant TimeSpan values within
.NET timer limits; reject invalid configuration. A retained membership attempt starts
its monotonic deadline once, on its first transition to ProjectionPending. Elapsed
time consumes it, including held status/parent/evidence calls, temporary status
failures and detached renderers. Requests, coalescing, SignalR, status recovery,
Continue read-only, same-instance replacement and remount never renew or accelerate
it. Accepted-only and delivery-ambiguous attempts are outside this bound.

Given any add/change/remove attempt remains unqualified, when the exact deadline
is reached even during held I/O, then its pending presentation becomes UnableToVerify
with localized retry-status/read-only/support guidance and no gateway call, projection
read or cancellation. Expiry is uncertainty, never rejection, failure, cancellation,
success or permission for another dispatch. Preserve intent, baseline, exact
message/correlation/domain/aggregate, caller binding, audit truth and admission.
UnableToVerify is unresolved: Continue read-only and surface disposal detach the
renderer while retaining the dispatched attempt and lease. Other aggregates remain
available. Terminal evidence cancels its timer; circuit/tracker disposal stops timers
and observers without fabricating outcomes or releasing unresolved admission.

Given qualifying evidence arrives before or after expiry, when the original exact
attempt is reconciled, then only its authoritative intended projection confirms.
Late status alone remains a nudge; verified terminal non-success stays truthful.
Expiry alone must not invalidate held-operation comparisons or suppress a late
qualified result. Other concurrent evidence/caller/generation changes still reject
obsolete publication. Recovery can query status/projection for that same attempt,
but unqualified results cannot restart pending waiting. Removal retains its committed
sequence, caller-scoped proof and separate audit lifecycle/budget.

Exercise real Fluent add/change/remove commands with deterministic time and held
operations: boundary, repeated/coalesced requests, qualifying evidence before/after,
unavailable/mismatched evidence, failed status, explicit recovery, Continue read-only,
replacement/remount, disposal and late outcomes. Assert exact dispatch/status/parent/
evidence counts, cancellation separation and admission behavior. Preserve completed
B1–B8 and DW bundles, particularly DW-245/DW-265/DW-290 queued projection intent.
Keep DW-248/DW-266/DW-267 and lifecycle poll-budget DW-323 open and separate.
No generic plumbing extraction, dependency/version/gitlink mutation, stage/commit/
push/deploy, replay, migration, nested initialization, gate/exclusion/coverage/timeout
relaxation or sprint/archive rewrite is authorized. Only close DW-247 after independent
review and applicable focused/full Debug/maintained Release UI/browser qualification.
Record initial failures, exact commands, preserved external provenance blockers,
counts/readiness and a next ledger bundle; never claim full release readiness.

</frozen-after-approval>

## Implementation Notes

- Entry branch main, canonical HEAD `a3f43655d68370389439c7d2c212ab7939bd164e`.
  `dw-247-entry-state.json` captures all ten repositories and inherited bytes.
  Explicit user preservation/no-commit authorization governs the dirty tree.
- Scope is one domain recovery behavior, using the existing retained-attempt lifetime,
  existing controls and test clock; no intent gap or irreversible action remains.
  Use the rendered small-change route. No sprint story transition applies.
- Code map: `TenantCommandAttemptTracker`/`TenantRetainedCommandAttempt` own the deadline
  independently of renderers; a membership-only policy validates configuration and
  transforms typed pending snapshots. Membership snapshot confirmers admit expired
  uncertainty only with retained command-event evidence and original authoritative
  provenance checks. Three flows publish/read normalized retained results and accept
  expiry-only comparisons, preserving all other exact continuation guards.
- The guard's historical uncertain-release comment must reflect the tracker-owned
  admission contract; its state-only behavior is unchanged.

- Initial Debug build passes (existing SignalR NU1903 warnings). Existing focused
  membership/proof/retained/guard suite passes 485/485, zero failures/skips.
  The pre-change isolated AppHost was Running/Healthy; exact-target stop passed.

- Production adds the membership-only clock/configuration policy and retained timer.
  Expiry preserves audit state, exact tracked evidence and admission. An exact
  expiry-source comparison admits held late results without relaxing evidence guards;
  the three renderers read the tracker-normalized result after publication.
  Status failures retain their existing semantics; only the expiry-specific Unable
  state can consume late projection evidence directly.
- Broad Release failed on the two existing SignalR NU1903 audit errors; strict
  package-only UI failed on the same seven pre-existing projection-scope API gaps.
  Commands and output are in dw-247-command-results.json; no gate was weakened.

## Review Triage Log

Independent context-free Blind Hunter reviewed entry-relative changes, calculated
N=min(floor(sqrt(83.877)+1),10)=10, and returned ten findings. No tests were executed
by the reviewer. Every finding is retained below.

| Finding | Verdict and disposition | Evidence |
| --- | --- | --- |
| Focus-only nudge clears expiry comparison source | medium, patched | Preserve the exact expiry source across HasSameEvidence focus-only updates. Three deterministic state regressions and held-parent Fluent cases pin late qualified publication; real subsequent evidence still invalidates it. |
| Transient status loses expired proof eligibility | medium, patched | Pending/retryable responses preserve expiry uncertainty and retained event evidence, matching their pre-expiry semantics. Unknown, unverified and malformed responses still block confirmation until verified recovery. Actual Fluent cases cover all three flows. |
| Module configuration argument ignored without IConfiguration in DI | medium, patched | Module factory now supplies provider configuration or its explicit argument plus TimeProvider. Six real module-composition tests cover fallback, invalid values and host precedence. |
| Initial refresh hides Continue while submit awaits evidence | medium, patched | CanContinueReadOnly admits expired UnableToVerify only after delivery settles. Detachment clears local submitting state and advances generation, retaining attempt/admission. Nine held initial-submit Fluent cases cover parent/evidence and late results. |
| Observer invocation list can outlive circuit disposal | low, patched and clarified | Disposal blocks state publication and new observer claims; per-observer lock checks skip later claims. An observer claimed before disposal may finish; component observers already guard disposal. Deterministic disposal between two observers verifies the second never runs and admission remains unresolved. No blocking join is introduced. |
| Missing operator setting documentation | low, patched | quickstart documents configuration/environment name, default, validation, first-pending clock and retained admission/recovery/disposal. Other documented policies remain unchanged. |
| Missing fractional/max/culture acceptance cases | low, patched | Deterministic tests accept one tick,125ms and maximum4294967294ms using invariant values under fr-FR, asserting the exact boundary. |
| Missing caller/authorization cross-product tests | low, rejected optional duplication | No new bypass found on independent trace: TenantDetailPage BeginLifecycleProof/CanApplyLifecycleProof, caller/version invalidation and CallerBoundTenantsRestQueryClient still bind production reads. Expiry-only comparison preserves full snapshot evidence and does not bypass tenant/caller/generation checks. Existing composed caller/authority regressions remain in the full UI gate. No unrelated plumbing change or new deferred defect is warranted. |
| Missing held removal-audit regression after late confirmation | low, patched | New actual Fluent removal case holds the audit query after late projection confirmation, advances beyond the former membership deadline, and asserts original sequence/caller, uncancelled audit token, no new I/O and truthful final audit state. Existing proof-walk page/cancellation/receipt regressions remain unchanged. |
| Missing rendered localization/accessibility assertions | low, patched | Actual Fluent expiry assertions cover EN/FR guidance, assertive live region, available recovery and absence of raw tracking identifiers. Resource parity remains gated. |

Independent read-only recheck found no unresolved concrete production issue; the
caller suggestion was explicitly reclassified as optional coverage after tracing
actual production proof boundaries. Final execution evidence is recorded separately.


## Completion review — 2026-10-16

Resumed the existing in-progress spec on clean entry
`6de661e129ff6f6f0c19795fcb1d032abee25d1d`. The implementation and initial
review corrections were already committed by the user. This pass preserves that
history, completes qualification, and corrects only the following review findings.
The frozen intent and original baseline remain unchanged; no sprint transition
or commit is authorized by this spec.

The skill's only configured review layer, context-free Blind Hunter, independently
compared the original DW-247 entry-file copies with current production/test files.
Changed content:103199 bytes/1024=100.780KiB; N=min(floor(sqrt(100.780)+1),10)=10.
The reviewer ran no tests. Its read-only correction recheck reports no concrete
regression and confirms all three applied corrections.

| Finding | Verdict and disposition | Evidence |
| --- | --- | --- |
| Expiry guidance asks for an unavailable command reference | low, patched | All three membership audit entry points omit SupportSafeCommandReference and intentionally hide raw tracking IDs. EN/FR expiry resources now say to contact support without asking for a missing reference. Recovery and uncertainty guidance remain whole-string localized resources. |
| Test click helper bypasses disabled controls | low, patched | Before invoking the Fluent callback, ClickAsync now asserts both FluentButton.Disabled=false and absence of the rendered disabled attribute. Existing held-operation cases therefore verify reachable recovery/Continue actions as well as callbacks. |
| Early timer callback/reschedule branch lacks dedicated coverage | low, rejected optional coverage | The branch compares monotonic elapsed time under the same lock and rearms only the positive remainder. Existing exact/fractional boundary cases and held operations exercise the deadline contract. No production defect was demonstrated; a specialized early-firing clock would add test machinery beyond a simple correction. |
| Queued callback after disposal lacks dedicated coverage | low, rejected optional coverage | ExpireMembershipWait rejects disposed trackers under _sync before state/timer publication. Existing disposal tests assert stopped timers, no notification, retained admission and no fabricated outcome. Injecting callbacks after disposal would require additional timer scaffolding without a demonstrated gap in behavior. |
| Queued original callback after replacement lacks dedicated coverage | low, rejected optional coverage | The same locked callback checks IsCurrent using exact retained-attempt reference identity. Terminal/replacement tests pin removal of the old timer and preservation of the successor deadline. No stale-publication defect was demonstrated; additional callback scaffolding is optional. |
| Separate-thread expiry/reconciliation boundary coverage | low, rejected optional coverage | Tracker updates and expiry serialize on _sync; held-I/O cases verify qualifying results both before and after expiry, and exact expiry-source comparisons reject other evidence changes. No lock-free write or concrete unsafe interleaving was found. Additional scheduling scaffolding would duplicate verified outcome orders. |
| Independent UTC-clock jump coverage | low, rejected optional coverage | Production explicitly uses TimeProvider.GetTimestamp/GetElapsedTime and never UTC subtraction. Deterministic elapsed-bound tests cover current behavior. A second independently adjustable clock is optional regression hardening beyond the current defect. |
| Actual DI-scope timer disposal coverage | low, rejected optional coverage | Module composition registers the IDisposable tracker as scoped; composition cases use real scopes and the tracker disposal cases assert timer/observer/admission behavior. No custom DI ownership path or failure was found. Additional combination coverage is optional duplication. |
| One-tick-above-maximum configuration rejection coverage | low, rejected optional coverage | The invariant TimeSpan parser rejects nonpositive and values above uint.MaxValue-1 milliseconds. Tests pin the exact accepted maximum and reject larger durations. No precision defect was demonstrated; the extra ceiling-adjacent configuration case has negligible normal-use impact. |
| Concrete maximum and setting-capture lifetime undocumented | low, patched | quickstart now specifies49.17:02:47.294 and construction-time capture by the circuit-scoped tracker. Existing trackers retain their original bound after configuration changes. |

The rendered resource/callback correction does not change dispatch, deadline,
identity, projection, caller, audit or admission semantics. Initial full Debug
passed5368/5368. The first full maintained Release run passed5367/5368 and failed
only RealChromiumFocusValidatorStopsWithinItsBoundWhenChromiumHangs at its unchanged
15-second outer wait while the isolated app was starting. Its focused recheck
passed1/1 in6.6s after exact-target app cleanup. No timeout or assertion was relaxed.
All initial commands/failures remain in dw-247-completion-command-results.json.
The pre-correction results are superseded by the corrected final qualification.

The permitted local Aspire skill supplied a fresh isolated start, state/telemetry
inspection, healthy tenants-ui wait, all 23 resources Running/Healthy, and exact-target
stop before final qualification. No AppHost or dependency source was edited.
Final input capture additionally covers RESX resources and browser HTML templates,
which the inherited source-only hash list omitted.


## Final qualification

Corrected Debug source and maintained Release each pass657 focused cases and
5368 full per-project MTP UI cases,zero failures/skips. Both unchanged Chromium
lanes pass. All 6753 final inputs,including RESX and browser templates,remain
stable; initial failures and all release/provenance limits remain recorded.
Only DW-247 closes. The original frozen intent/baseline,all unrelated ledger
records,archive/sprint and every repository HEAD/branch/dependency are preserved.
See [validation evidence](dw-247-validation-evidence.md) and its linked exact
commands,summary,hashes,preservation and ledger audit. Next selected bundle:
DW-234/DW-251/DW-316. Completion leaves changes uncommitted under the frozen scope.
