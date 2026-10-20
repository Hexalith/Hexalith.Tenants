---
title: 'DW-155: Qualify member paging through rendered controls'
type: 'refactor'
created: '2026-10-20'
status: 'done'
route: 'oneshot'
review_loop_iteration: 1
baseline_commit: '630b072c3b48b1907705a7ed5b3c2360e2d94882'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** DW-155 records member-paging tests that invoke internal component handlers directly. They do not qualify the rendered buttons' event bindings or both directions during pending reads and open member commands, and require runtime visibility widened solely for tests. It is the oldest actionable Tenants record with no dependency; earlier open items need another owner or an unresolved contract.

**Approach:** Exercise paging through rendered Fluent buttons, including captured callbacks representing events already dispatched before a disabled re-render. Verify blocked events, boundary conditions and recovery without losing open command state or starting duplicate page reads. Replace all direct handler calls and restore private visibility while preserving existing guards and UI behavior. Close only DW-155 after focused and full UI verification, then commit and push on main as requested.

</frozen-after-approval>

## Implementation Notes

- Entry revision: `630b072c3b48b1907705a7ed5b3c2360e2d94882`, clean main. Freeform maintenance work; no epic story or sprint-status update applies.
- No unresolved user choices, migration, new API or dependency changes. Commit and push are explicitly authorized. Small footprint: `MemberAccessReview.razor`, `TenantDetailSurfaceTests.cs`, this spec, validation evidence and the ledger/index.
- Read repository baseline and UX/Git guidance, build configuration, current architecture constraints and existing paging tests. Independent read-only investigation confirms twelve direct handler calls in the same test file, existing Next-only pending-read coverage, and missing corresponding rendered callback coverage.
- Reuse the existing bUnit services and snapshot helpers. Capture `FluentButton.OnClick` from the rendered button by stable test id, as existing list paging tests do. A disabled DOM click alone cannot qualify an event already dispatched before disablement. Ordinary enabled page navigation uses DOM clicks. Keep all existing direction, refreshing and open-flow guards intact.
- Added eight rendered-control cases replacing the original direct-handler case: two open-flow/refresh cases, four direction boundaries and two held real-page reads with identity-preserving follow-up navigation. All eight pass against unchanged runtime guards (entry-runtime TRX). The initial build exposed a missing reference-type constraint on the generic bUnit helper; adding `class` fixes it, and the corrected Debug source build has zero warnings/errors.
- Replaced all twelve direct handler invocations, including four existing page-test callers, with rendered callbacks or enabled DOM clicks. Both handlers are private; their guard expressions are unchanged. The private-handler Debug source build has zero warnings/errors.
- Aspire baseline: 23 Running/Healthy resources, but bounded Error telemetry includes bootstrap ActorMethodInvocationException/HTTP 500 errors. The started AppHost was stopped successfully before rebuilding. The initial `aspire wait` attempt rejected unsupported `--format Json`; the corrected command succeeds. This is component qualification, not browser or fully healthy topology qualification.
- Four temporary harmful mutations build successfully and fail the eight-case lane: removing both refresh guards (2 failures), both open-flow guards (2), both boundary guards (3), or binding Next to Previous (7). The local driver restores intended source in `finally`; exact commands/results and scope limits are in [validation evidence](dw-155-validation-evidence.md). These are four separate controls, not six independently mutated predicates.
- Final restored Debug source build has zero warnings/errors; focused 8/8 and full UI 6571/6571 pass with zero failures/skips. Scoped formatting, own-spec gitlink and whitespace checks pass. The formatting tool emits only its generic workspace-load warning. Exact commands/results are in [validation evidence](dw-155-validation-evidence.md).
- The gated ledger closure changes only DW-155, retains every replaced historical field, and verifies 656 IDs, 397 done/259 open and 234 index rows covering every open ID exactly once. Archive, sprint, dependencies and release holds remain preserved. Pinned commitlint validates the exact full commit message with zero problems/warnings; origin fetch reports no divergence.
- Independent review requested edited-form preservation, actual pending-command coverage, boundary recovery, durable mutation reproduction, and final validation in the evidence. The first three are patched in the existing theories: role/confirmation values survive queued paging; held change-role/removal responses preserve RequestSent state and message ID with exactly one dispatch, then reject cleanly; both directions recover through freshly rendered controls after every boundary. Focused review qualification passes 10/10 with zero failures/skips. A review-build fixture error assigned the void Shouldly assertion to a string; separating capture and assertion fixes it, and the corrected source build has zero warnings/errors.
- The complete final mutation driver is embedded in the evidence; original eight-case controls and initial full-suite results remain as dated history. The final acceptance section was added while review was in progress, once the initial full suite finished. Review requalification is underway with the strengthened tests.

## Review Triage Log

- Low / patch: same child instance did not prove edited-form preservation. The change-role selection and removal confirmation are now edited through rendered controls and verified after queued events.
- Medium / patch: open-editor checks did not exercise an actual pending command. Both flows now hold a real submission response, reject both paging directions without additional dispatch, and retain RequestSent and the original message ID until a controlled rejection completes.
- Low / patch: boundary cases did not qualify reappearance/recovery. All four restore both available directions, then exercise freshly rendered controls, including after the pager disappeared.
- Low / patch: exact mutation reproduction depended on a temporary driver. The complete final driver is embedded in the committed evidence; it restores source in finally and records separate final controls.
- Low / patch (applied during review): the review began before final test evidence was appended. The evidence now includes exact initial build/focused/full commands/results and preserved initial failures; strengthened final results are recorded separately.
- Independent correction recheck confirms all five findings addressed and no remaining concrete code/test issue. The reviewer independently verifies all four final mutation inventories and ledger/index isolation. The final restored source rebuild has zero warnings/errors, focused 10/10 passes, and the scoped formatting recheck exits 0 with only the generic workspace-load warning; full UI acceptance is still running at this note.
- Final strengthened acceptance completes: restored Debug source build has zero warnings/errors; focused 10/10 and full UI 6573/6573 pass with zero failures/skips. Independent review confirms all five findings addressed and no remaining concrete issue. The final ledger/index refresh preserves all original fields/other records and verifies 656 IDs, 397 done/259 open and 234 exactly covering rows. No deferred follow-up is needed; existing release qualification holds remain preserved.
