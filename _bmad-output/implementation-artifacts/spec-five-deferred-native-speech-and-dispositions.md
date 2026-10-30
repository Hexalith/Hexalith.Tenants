---
title: 'Resolve five deferred records with native speech evidence and explicit dispositions'
type: 'chore'
created: '2026-10-30'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '4fa78baa94f939a2b752bce52d0bac77214ddd83'
context: ['AGENTS.md']
---

<frozen-after-approval reason="human-owned intent — user approved 2026-10-30">

## Intent

Five numbered records remain: DW-116/137/446/459/466. Qualify the three speech risks using actual native Windows NVDA output, and resolve the contract/history dispositions through explicit user decisions. Review, verify, commit and push authorized changes to main. The user approved including existing ledger/archive edits.

Use the existing authenticated disposable fixture and its EN/FR S0–S3/C1–C3 handoff. Declare NVDA/Chrome as primary and propose NVDA/Firefox as the second qualification target, recording the approved support basis. If actual speech exposes a Tenants-owned announcement defect, make a narrow fix and repeat qualification. Failed or unavailable evidence leaves the affected record open.

## Boundaries & Constraints

Preserve authorization, command identity, projection truth, localization, durable identifiers, opaque cursors and unrelated release holds. Preserve original findings, all 657 numbered IDs, unselected record bodies and the approved pre-existing edits. Use temporary native test/config directories; preserve the user's NVDA settings and desktop session. Start and inspect the exact AppHost through the local Aspire skill before UI code changes.

Never substitute DOM/AX data or expected strings for actual NVDA output. Distinguish queued Speech Viewer output from audible completion; retain focus, interruption, quiet-window and observation-end evidence. No credentials, tokens, raw requests or unrelated desktop speech enter committed artifacts. No dependency upgrades, shared-module modifications, migrations, timeout inflation, parallelism changes or unsupported causal claims. AT evidence qualifies the disposable production UI composition, not backend persistence or broad accessibility/release readiness.

## I/O & Edge-Case Matrix

| Scenario | Required outcome |
| --- | --- |
| S0–S3, EN/FR, both pairings | Timestamped actual search speech, focus and assessed omissions/duplicates; empty initial region produces no stale announcement |
| First C1 Pending / C2 Unavailable | Capture first insertion and separate lifecycle speech; no projection-confirmed success while projection is held |
| Both C3 Refresh controls | Correct keyboard target, one simulated command, increased status reads, actual speech and unchanged attempt |
| Speech failure or external Fluent defect | Retain failure; fix only owned behavior within this scope; otherwise report blocker and keep affected record open |
| Unsupported identifiers | Existing pre-HTTP refusal and shared durable identity restrictions remain unless separately scoped contract work is approved |
| Historical timeout | Current passing controls never imply identification or explanation of the missing September failure |

**Approved decisions, 2026-10-30:** Withdraw reversible-identifier support and close DW-137 as an accepted contract limitation; preserve current refusals and reopen only through approved shared-identity or user-only contract work. Close DW-466 as an accepted unresolved historical incident, retaining the missing method/root-cause evidence and reopening on an identified failure or causal reproduction. Neither closure claims an implemented feature or causal fix. Approve native NVDA/Chrome and NVDA/Firefox as the two qualification targets for this bounded run, without establishing a general browser-support policy. User: "approve and continue".

</frozen-after-approval>

## Code Map

- `docs/tenants-ui-deferred-at-handoff.md`: existing complete speech protocol, settings, controls, observation and closure gates; reuse.
- `tests/Hexalith.Tenants.UI.Tests/Browser/validate-authenticated-administrator-dialogs.mjs`: native standalone fixture/build, OIDC, headed browser, disposable responses and evidence identities.
- Same directory: `validate-deferred-handoff.mjs`, `deferred-handoff-fixture.mjs`: terminal parser, independent status/projection controls and automated matrix.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor`, `Components/Shared/ListSurfaceStates.razor`, `Components/Tenants/CreateTenantFlow.razor`, `Components/Tenants/Audit/AuditAvailabilityState.razor`: owned announcement composition; inspect before any proven-defect patch.
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs`, `AuditAvailabilityStateTests.cs`, `CreateTenantFlowTests.cs`, `TenantDetailSurfaceTests.cs`: focused regressions; the authorization case already waits for dependency entry.
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryClient.cs`: route refusal; EventStore durable identity validation prevents an encoding-only fix. User lookup instead targets fixed aggregate index with UserId as EntityId.
- `story-5-4-re-review-verification-2026-09-30.md` and existing contention evidence under implementation artifacts: local historical report lacks metadata method identity. Recovered September CI ZIPs contain build logs without executed tests.

## Tasks & Acceptance

- [x] `tests/Hexalith.Tenants.UI.Tests/Browser/capture-deferred-nvda-speech.ps1` and existing fixture modules if needed — add opt-in native capture/orchestration, reuse controls, preserve browser foreground, bind transcript to NVDA-owned viewer and clean up only owned processes.
- [x] Owned announcement components and matching tests above — patch only a reproduced owned defect; preserve shared composition and existing safety assertions.
- [x] `_bmad-output/implementation-artifacts/evidence/five-deferred-native-speech/` and `five-deferred-native-speech-validation-evidence.md` — retain sanitized actual transcripts, native/fixture/source identities, failed attempts, controls, historical-log inventory, independent review and limitations.
- [x] `docs/tenants-ui-deferred-at-handoff.md`, `deferred-work.md`, `deferred-work-archive.md`, `docs/maintenance-backlog.md` — record approved dispositions and evidence-backed outcomes with exact counts and preservation verification.
- [x] This spec — append execution/review results; validate exact commit message with pinned commitlint, fetch/inspect divergence and push main.

Acceptance:

- Given both declared pairings and cultures, when all applicable trials pass with independently accepted actual speech evidence, then only their qualified AT records close; blocked cells remain open.
- Given user-approved contract/history dispositions, when tracking closes, then original evidence, accepted limitations and reopening conditions remain explicit without claiming a feature or causal fix.
- Given the approved dirty starting tree, when final changes are verified and published, then all original IDs and unrelated findings survive, and the commit includes the pre-existing ledger edits.

## Implementation Notes

- Parent verified the staged unified diff against the approved entry, normal/optimized conservation results and exact executed test manifests. The first four implementation tasks are complete. Full native AT closure is intentionally blocked under the approved conditional outcome; the three AT IDs remain open. Matrix coverage combines the maintained browser controls, structural regressions, real speech/foreground-failure controls, unchanged route-refusal cases and explicit historical-evidence limits. Final publication remains parent-owned.

- User approved the complete proposed spec and both explicit tracking dispositions on 2026-10-30. Existing ledger/archive edits are authorized for inclusion. Entry copies are preserved in `/tmp/tenants-five-deferred-entry-j969k2zn/` for conservation checks.
- This implementation phase performs no remote operations or pushes. The parent owns staging, independent workflow review, commitlint, commit, remote divergence checks and authorized publication. Leave the final publication task for the parent.


## Spec Change Log

- 2026-10-30: Implemented opt-in native collector/orchestration and preserved actual failed/clean Speech Viewer packets. Reproduced first-Pending omission in four EN/FR baseline trials and added persistent Create-owned text hosts using shared state/localization; shared visual recovery lifetime is preserved.

## Review Triage Log

| Finding | Verdict | Route | Verified evidence and required correction |
| --- | --- | --- | --- |
| BH1 | medium | patch | S0 arms after authenticated workspace/network-idle completion, so entry speech precedes capture. Capture an authenticated entry from a controlled blank document without recording sign-in. |
| BH2 | medium | patch | Isolated actual-driver mocks accept mixed requested cultures/groups while observing only a subset. Refuse incompatible selectors and require every requested trial in completed coverage. |
| BH3 | medium | patch | Collector stdout polling and driver acknowledgement waits have no protocol progress bound; an alive silent child cannot reach finally. Add private bounded progress/acknowledgement waits and preserve failure cleanup. |
| BH4 | medium | patch | An unstarted Process has no usable redirected streams; the exact PowerShell cleanup branch reaches StandardInput.Close() on null and throws before artifact/NVDA cleanup (independently reproduced under strict mode). Track successful startup and make cleanup independent of that invalid process state. |
| BH5 | medium | patch | Collector-side exceptions bypass normal-path stderr persistence. Drain/save the owned runner's stderr after shutdown from finally so first-failure ordering remains diagnosable. |
| BH6 | medium | patch | Native Control-key requests are logged after the final sample and then discarded by the next interruption baseline. Existing packets prove a key request, not its observer response; label them unverified and retain/assess a bounded actual response in the current collector. |
| BH7 | medium | patch | Any focus-control speech sample currently passes, including delayed product output. Require the intended Search input focus and matching localized control speech; retain other output separately. |
| BH8 | medium | patch | Mode snapshots lack generation time and missing/stale snapshots do not block collection. Timestamp snapshots and reject unavailable/stale mode observation with retained diagnostics. |
| BH9 | medium | patch | sourceSettingsUnchanged can be false while passed stays true. Fail collection on mismatch without restoring or writing the user's original settings. |
| BH10 | medium | patch | Exit code zero alone passes even without complete or an observed/control pair. Require protocol completion and matching nonempty completed observations/controls. |
| BH11 | low | patch | The evidence omits an assessment of the in-flight alert. Source shows UnavailableReason emits the existing InFlight resource whenever _isSubmitting is true; this is independent of a second submit. Attempt 20 has one Create activation and command; attempt 17 has one activation per each of two fresh Create sessions. Explain this source/counter boundary without claiming exact OnSubmit callback counts. |
| EC1 | medium | patch | The actual-driver mock reproduces omitted requested cultures; same selector-coverage root cause as BH2, verified independently before grouping. |
| EC2 | medium | patch | The collector loop permits an alive silent runner indefinitely; same unbounded-progress root cause as BH3, verified independently before grouping. |
| VG1 | medium | patch | Preverified regression gap: shared announcement ownership moved for all audit states, but only initial Pending/Unavailable owner hosts are asserted. Extend held projection-confirmation coverage to prove Pending polite content clears and MissingSupport label/reason populates the assertive host. |

All three independent layers completed before triage. EC1/BH2 and EC2/BH3 are grouped only after their individual verdicts. These are local test/capture or evidence corrections; the frozen intent and production announcement approach remain valid. No intent gap, re-derivation or new deferred work is required. Earlier native packets remain immutable and their original control/freshness limitations must remain explicit.

- Implementation handoff: independent workflow review and publication remain parent-owned. No assistive-technology record is closed: the complete native matrix is blocked by repeated external foreground changes and contaminated queued output. Bounded retries preserve clean attainable cells and all failures.

## Execution Results

- The [execution evidence](five-deferred-native-speech-validation-evidence.md) binds 21 native attempts, source snapshots, actual sanitized speech, controls, historical-log inventory and limitations. Clean post-fix first Pending is captured on Firefox EN/FR and Chrome FR; clean S3 EN is captured on both pairings. Full C1–C3/S0–S3 qualification remains incomplete.
- Final Debug/source build: zero warnings/errors. Maintained MTP focused audit/Create: 159/159; full UI project: 8,055/8,055, zero failures/skips. Maintained authenticated handoff controls: 28/28, including real host continuity, with zero unexpected commands/circuit errors.
- Approved dispositions close only DW-137 and DW-466. All 657 IDs remain: 654 done/3 open. Current maintenance counts are 654 done, 0 actionable, 1 blocked, 2 uncertain; exact three-row coverage and original body/archive conservation pass in normal and optimized Python. Historical preambles remain preserved.
- No dependency/gitlink, timeout, parallelism, authorization, durable-identity or projection-truth change. Native user settings remain unchanged. Parent owns final review, staging, commitlint, commit, divergence check and push.

## Verification

Build Debug/source with the existing fixture's pinned EventStore 3.128.4 and packaged McpCli settings. Run relevant UI classes through maintained MTP with per-test results; run the full UI project if production behavior changes. Native real-speech trials must cover both pairings/cultures, first insertion, both Refresh paths, zero unexpected commands and zero circuit errors. Record effective NVDA voice/language/mode/live-region settings and browser foreground, plus positive capture and omission/interruption controls.

Run PowerShell/Node syntax checks for changed helpers, `git diff --check`, the maintained story-gitlink validator and exact ledger/body/count preservation checks. Preserve native blockers and commitlint evidence.


## Parent completion

All 14 review findings were addressed, and all three original reviewers cleared their corrections. Final parent verification: full UI 8,055/8,055, six Node mocks, PowerShell/Python syntax, normal/optimized conservation, artifact integrity and scoped formatting pass. Two unchanged baseline whitespace findings were corrected without semantic changes after the full run. Production source and original native packets are unchanged by review patches; the strengthened collector has no new native acceptance run. The first four tasks and all conditional outcomes are complete; publication follows the validated local commit under the user's explicit main-push instruction. Exact results and commitlint evidence are in the linked execution evidence. DW-116/446/459 remain open; no new work was deferred.
