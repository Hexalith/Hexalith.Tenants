---
title: 'Resolve five deferred proof and validation records'
type: 'bugfix'
created: '2026-10-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: c157c394b8fca5f392b5f8a6a5ff1f51f4ac77a0
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Complete five deferred records: DW-225, DW-372, DW-382, DW-623 and DW-642, then push verified changes on main as requested. Exact command correlation and aggregate sequence must qualify removal/correction audit evidence independently of BFF clock skew. A verified completed correction whose event proof cannot identify its committed sequence must fail closed and release aggregate admission consistently across the correction panel and administrator page. Requalify the existing causal member-removal implementation and close the duplicate cold-source-build timing records using immutable hosted evidence. Preserve unrelated records and existing release/runtime limitations.

</frozen-after-approval>

## Implementation Notes

- Investigation: member removal already uses an unfiltered audit walk and exact command correlation/sequence (RemoveTenantMemberFlow.razor); the timestamp helper remains used by metadata updates and stays unchanged. GlobalAdministratorAuditProofLookup still filters From and matching rows by the independent submission clock. GlobalAdministratorsPage excludes missing sequences from permanent proof failure, while the panel already rejects them.
- Small local footprint: three existing UI implementation files, their focused regressions, and closure/evidence documentation. No new public API, dependencies, persisted data changes or deployment. The requested push to main is explicitly authorized.
- Verification: Debug/source UI build, focused lookup/member-removal/administrator tests, full UI project, original-code negative controls, immutable hosted four-case source run evidence and story gitlink guard. Retain exact failures and limitations.
- Status contract tracing at EventStore `f4df781cb7247c6a5f7549106b90d3748151edba`: normal verified event-producing completion emits an end sequence; legacy deserialization and terminal advisory repair permit completed records without one. The Tenants gateway verifies identity independently of usable sequence. No production malformed-status incident is claimed.
- The existing intermediate-progress regression caught an overbroad first implementation: EventsStored/EventsPublished also establish event evidence while awaiting completion. Permanent refusal now examines verified Completed status explicitly, preserving intermediate ownership, and maps only the correction-specific proof-failure key to terminal lease release. Both grant/removal and superseded completion paths use that outcome.
- Two focused development attempts failed: nullable theory inputs required explicit long literals; the first lease check incorrectly treated intermediate event evidence as terminal. Both were corrected; final acceptance is recorded separately.
- The lookup and snapshot both had submission-clock checks; both layers now permit exact verified evidence before or after the independent BFF submission clock. Original event ordering remains required.
- Independent Blind Hunter review returned eight findings. Patches preserve previously established sequence proof across compatible later statuses, replace the inherited 50-page ceiling with a ten-second cancellable complete walk, cover both event-progress statuses without completion-only event counts, add actual page replacement interleavings and qualify revoke as well as restore clock skew. Submission time remains a documented compatibility argument; sanitized hosted log evidence is retained alongside its public raw-log hash.

## Review Triage Log

| Finding | Verdict | Resolution and evidence |
| --- | --- | --- |
| Original-receipt window can exceed the inherited 50-page cap. | medium, patch | Replace the hard page ceiling with a ten-second linked deadline and cancellable wait; retain complete stable walk/uniqueness checks. A 60-page/3,000-row regression, stalled-reader deterministic deadline and caller-cancellation tests qualify the boundary. |
| Compatible later status can omit an established sequence. | medium, patch | Reuse retained same-attempt sequence when new status lacks usable proof. Grant/removal tests retain the lease at delayed projection, then confirm after a compatible sequence-less completion. |
| Submission-time parameter appears causal although unused. | low, patch | Document it as compatibility attempt metadata; it never qualifies proof. No public gateway signature changes. |
| Intermediate fixture incorrectly supplies completion-only event counts. | low, patch | Both EventsStored and EventsPublished now have null event counts across all unusable/valid sequence cases. |
| Superseded terminal status paths lack evidence. | medium, patch | Actual page replacement preserves the replacement owner; a held superseded response releases only its still-owned invalid attempt. Permanent refusal is processed before retaining reconciliation, on both grant/removal paths. |
| Clock-skew cases cover restore only. | low, patch | Lookup and snapshot theories now cover both GlobalAdministratorSet and GlobalAdministratorRemoved in both clock directions; gateway redaction evidence is retained. |
| Hash-only hosted evidence can expire with logs. | low, patch | Retain a sanitized timestamped runner/cache/build/test excerpt and hash alongside the original public log hash and immutable job URL. |
| Implementation inventory omits the snapshot gate. | low, patch | Correct the agent-owned notes to three production files, explicitly naming the snapshot gate; frozen intent remains unchanged. |
| Recheck: superseded terminal status releases its lease but leaves pending presentation. | medium, patch | After exact message/correlation/target/preview and ownership checks, publish UnableToVerify with release. Same-owner tests require terminal presentation, false in-flight state and an enabled new grant action; replacement-owner tests remain guarded. |

- Final acceptance: zero-warning/error Debug/source build; focused 1566/1566; maintained MTP full UI 7549/7549 with zero failures/skips. Earlier full runs include a stale gateway expectation and an unrelated cancellation/disposal race in an unchanged membership test; both remain qualified in the retained evidence. Final independent recheck confirms all eight initial findings and one follow-up are resolved, with no deferral.
- Formatting verification exits 2 on 100 existing whitespace diagnostics; none touches a changed line. Gitlink guard, diff check and exact ledger/index coverage pass. The exact full commit candidate validates with pinned/installed commitlint 21.2.3, zero problems/warnings. Source baseline and all evidence remain retained before publication.
