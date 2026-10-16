---
title: 'DW-247 bounded membership ProjectionPending recovery'
type: 'bugfix'
created: '2026-10-16'
status: 'in-progress'
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
