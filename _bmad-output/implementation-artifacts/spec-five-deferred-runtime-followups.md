---
title: 'Investigate five deferred runtime and test follow-ups'
type: 'bugfix'
created: '2026-10-28'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '0b07d279aacce430b04b42ee9abd8aa5a6d6dafc'
context:
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — approved 2026-10-28">

## Intent

**Problem:** Four unnumbered runtime follow-ups plus DW-466 lack deterministic reproductions. Passing reruns do not explain the original failures.

**Approach:** Investigate credential redirects, grant Escape focus, removal-modal focus failure, grid disposal during navigation, and UI waits under load. Fix reproduced Tenants-owned defects, review, verify, and push completed changes to `main`.

**Approved decision (2026-10-28):** Proceed with these five investigations; external grid ownership or missing historical evidence may leave records open. Continue through implementation, review, verification and push.

## Boundaries & Constraints

**Always:** Preserve original evidence, authorization, admission ownership and zero-circuit-error acceptance. Use synthetic loopback credentials and simulated commands. Distinguish fixes, negative reproduction and blockers. This proposes five investigations; five defect closures are not established.

**Never:** Forward commands, mutate domain data, deploy, silently change redirect policy, suppress faults, inflate timeouts or upgrade dependencies speculatively. Preserve policy/assistive-technology holds and the recorded DW-137 deferral. Fix shared components only in their owning repository.

## I/O & Edge-Case Matrix

| Scenario | Controlled input | Required observation |
| --- | --- | --- |
| Credential redirect | Real handler; 301/302/303/307/308; same/cross origin | Exact destination, method and synthetic-body replay; bounded requests |
| Grant Escape | Foreground browser; held subscription/render completion | Dialog closes; enabled launcher owns focus; document focus is recorded |
| Removal focus fault | Connected circuit; rejected focus operation; held delivery | Native Escape/overlay containment and exact command ownership persist |
| Grid navigation | Held initialization; detail navigation and Back | Deterministic lifetime trace; zero circuit errors for acceptance |
| UI wait | Held authorization; controlled resource load | Named failing test and causal ordering; synchronized completion without larger timeouts |

</frozen-after-approval>

## Code Map

Paths below are repository-relative. Production component root: `src/Hexalith.Tenants.UI/Components/`. UI test root: `tests/Hexalith.Tenants.UI.Tests/`. Evidence root: `_bmad-output/implementation-artifacts/`.

- `src/Hexalith.Tenants/Bootstrap/`: `TenantBootstrapHostedService.cs` shares the default client between credentials and commands; `TenantBootstrapCredentialProvider.cs` submits credentials.
- `tests/Hexalith.Tenants.Server.Tests/Bootstrap/TenantBootstrapHostedServiceTests.cs`: currently synthetic handlers only.
- Component `Pages/GlobalAdministratorsPage.razor`: grant restoration; `Tenants/Members/RemoveTenantMemberFlow.razor`: focus preparation and admission.
- UI test `Browser/validate-authenticated-administrator-dialogs.mjs`: authenticated production components, native modals, create/detail/Back and standalone loopback OIDC.
- UI test `Components/TenantDetailSurfaceTests.cs`: named authorization-pending test; second historical metadata-confirmation test remains unnamed.
- Evidence `story-5-4-re-review-verification-2026-09-30.md`: timing history; `dw-268-328-391-434-489-raw-validation.zip`: grant diagnostics; `evidence/dw-194-201-221-267-609/raw-validation.zip`: grid stack/lifetime excerpt.

## Tasks & Acceptance

**Execution:**

- [x] Bootstrap/Server test paths above: qualify real-handler redirects; record any transport policy choice before production edits.
- [x] Browser runner/grant page: control foreground and subscription ordering; fix demonstrated restoration races.
- [x] Browser runner/removal flow: inject connected focus failure; hold delivery; assert containment and ownership.
- [x] Browser runner: control grid initialization/disposal; identify owner and retain reproduction. Keep open if no owning fix can be qualified.
- [x] Detail test/history: identify both DW-466 cases; reproduce under load; synchronize demonstrated races. Record missing historical identity as a blocker.
- [x] Evidence `deferred-work.md` and new batch report: record exact outcomes; preserve unrelated entries and original findings.

**Acceptance Criteria:**

- Given a controlled scenario, when run, then evidence identifies source, command, outcome and limits.
- Given a reproduced defect, when patched, then focused and affected full-project tests pass without weakened gates.
- Given external ownership or missing evidence, when reported, then the record remains open with its blocker.
- Given reviewed changes, when pushed, then pinned commitlint, diff and gitlink checks pass and remote divergence is inspected.

## Implementation Notes

Transport decision recorded before production edits (2026-10-28): the real-handler matrix reproduces cross-origin synthetic password-form replay for 307/308. Credential acquisition will use a dedicated named client with automatic redirects disabled for all 301/302/303/307/308 responses, including same-origin responses. Operators must configure the final token endpoint. Command dispatch retains its existing default-client behavior. This decision was explicitly directed during implementation; it is not an implicit change to shared HTTP policy. The original default-handler characterization remains as evidence.

The five investigations do not authorize closing unrelated policy or assistive-technology holds, including DW-137.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| B1 | medium | patch | Linked logs exist locally but are ignored by Git; preserve them in the committed evidence archive and update links. |
| B2 | medium | patch | The queue accepts any hub invocation; require the intended active global-administrator subscription scope. |
| B3 | medium | patch | The second focus wait observes the already-focused launcher without acknowledging the resumed subscription/render continuation. |
| B4 | medium | patch | Only the first lifecycle focus call fails; subsequent focus can repair the state before containment is checked. Hold recovery through the immediate containment checkpoint. |
| B5 | medium | patch | The exact-owned-command claim checks type/count/aggregate but omits payload and tenant/domain/status-message correlation. |
| B6 | medium | patch | Grid connected/detached prerequisites are recorded but not asserted before releasing the diagnostic continuation. |
| B7 | medium | patch | The load runner accepts exit zero even when the direct xUnit selector executes zero tests; require one passing unskipped case. |
| B8 | medium | patch | Worker output is discarded and no readiness, liveness or CPU activity is checked, so two-worker qualification is not established by the runner. |
| B9 | medium | patch | The expected grid exception bypasses the success-only final source capture; the failing diagnostic needs source stability through cleanup. |
| E1 | medium | patch | Cancellation/expiry does not retire queued callbacks; an obsolete invocation can satisfy the held-subscription prerequisite. |
| E2 | medium | patch | Sending completion frames is not acknowledgment that the component continuation/render finished; duplicate root cause of B3. |
| E3 | medium | patch | A worker may exit while a test still returns zero; duplicate root cause of B8. |
| V1 | medium | patch | Reviewer delayed completion frames and still observed a successful grant afterRelease claim; a real continuation acknowledgment is missing. Its later full-run focus failure is not claimed as a passing mutation. |

## Verification

- Build affected projects in Debug/source mode; run built xUnit assemblies using focused `-class`/`-method` selectors, then full affected projects. Keep broad blockers separate.
- Run the browser validator with `TENANTS_BROWSER_STANDALONE_OIDC=1` against freshly built production UI; require zero circuit errors.
- Run `scripts/validate-story-gitlinks.py` with this spec and `git diff --check`.
- Validate exact messages with owning pinned commitlint; fetch, inspect divergence, push `main`, and verify remote revision.

## Implementation Handoff

All five approved investigations completed. Two verified fixes close unnumbered records: dedicated credential redirect refusal and rejected-focus refusal before removal admission. Grant Escape's historical cause, the external pinned grid fix and DW-466's missing test identity remain open. No numbered status or dependency pointer changes.

Final Debug/source UI build passes with zero warnings/errors. Full UI **7,841/7,841** and Server **1,582/1,582** pass. Fresh focused browser **4/4** passes with zero circuit errors and source stability. Full browser fails on the known external grid fault after twelve completed cases; the separate grid diagnostic also fails its unchanged zero-error gate. Neither failure is hidden or claimed as acceptance. Five exact load cases and negative qualification controls pass. Three review lenses were triaged; final independent recheck finds no remaining concrete issues.

See [exact commands, outcomes, blockers and evidence](five-deferred-runtime-followups-validation-evidence.md).

## File List

- `../../src/Hexalith.Tenants/Bootstrap/TenantBootstrapHostedService.cs`: select separate credential and command clients.
- `../../src/Hexalith.Tenants/Bootstrap/TenantBootstrapServiceCollectionExtensions.cs`: register the credential-only redirect policy and bootstrap hosted service.
- `../../src/Hexalith.Tenants/Program.cs`: consume bootstrap registration.
- `../../docs/production-auth-readiness.md`: document redirect refusal and final trusted endpoint configuration.
- `../../tests/Hexalith.Tenants.Server.Tests/Bootstrap/TenantBootstrapHostedServiceTests.cs`: prove exact client selection.
- `../../tests/Hexalith.Tenants.Server.Tests/Bootstrap/TenantBootstrapRedirectTests.cs`: real-handler characterization and no-replay/direct-success matrix.
- `../../tests/Hexalith.Tenants.UI.Tests/Browser/validate-authenticated-administrator-dialogs.mjs`: foreground subscription ordering, connected native-focus rejection, and separate one-shot grid lifetime diagnostic.
- `deferred-work.md`: append outcomes while preserving original findings, all numbered statuses and unrelated records.
- `five-deferred-runtime-followups-validation-evidence.md`: batch outcome/verification report.
- `evidence/five-runtime-followups/`: exact logs, packets, preserved diagnostic attempts, load runner and conservation evidence.

### Historical harness review correction (superseded by final verification)

Exact active subscription tracking, held-completion negative control and scoped authoritative-read/DOM-render observation replace the earlier queue-only grant claim. Native removal containment is checked while recovery focus effects remain held; this exposes an open Escape defect, so the former negative qualification is withdrawn. Grid prerequisites are assertions and source equality is captured in cleanup even after failure. Load evidence requires exact single-case xUnit XML and observed worker activity. Ignored logs are preserved in a verified SHA-256 manifest archive; original packets remain as pre-review members. Targeted diagnostics only ran during this correction; final verification remains with the parent workflow.

Minimal focus correction: private focus success now gates preparation before admission. Direct regression 2/2 and edited component class 260/260 pass; patched targeted browser remains failing at subsequent gateway delivery before any loopback POST, so final browser/status qualification remains with the parent. File List additions: `../../src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor` and `../../tests/Hexalith.Tenants.UI.Tests/Components/RemoveTenantMemberFlowTests.cs`.

## Final Verification and Review Resolution

The historical handoff failures above remain preserved. Final fresh uninstrumented focused browser qualifies the corrected grant callback/render checkpoint, two no-dispatch focus refusals, safe dismissal, exact admitted removal envelope and repeated same-message status lookup. The broad browser result remains failed on the external grid owner; the focused result remains narrower qualification and does not set the runner's full-suite closure bit.

B1–B9, E1–E3 and V1 are resolved by archived logs, exact active subscription tracking/expiry, held negative control and rendered callback checkpoint, fail-closed focus preparation, full command/status identity checks, asserted grid ordering, cleanup source capture, exact xUnit results and observed worker activity. Strengthening B4 reproduced the original production gap; its minimal private guard and two regressions fix it. Independent read-only recheck reports no remaining concrete issues.

Final project tests: UI 7,841/7,841; Server 1,582/1,582; zero failed/skipped. Focused browser 4/4 with zero circuit errors. Load 5/5 plus rejecting controls. Original historical evidence and all unrelated numbered bodies are preserved. Pinned commitlint 21.2.3 accepts the exact full message with zero problems/warnings. The user's explicit push request governs final Git operations; no additional approval is needed.
