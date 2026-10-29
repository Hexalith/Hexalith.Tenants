---
title: 'Qualify five deferred items and retain their evidence gates'
type: 'chore'
created: '2026-10-29'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '95e9a6cce041c30311baf6703e06289beafca2c6'
context:
  - 'AGENTS.md'
  - '_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — user approved recommendations on 2026-10-29">

## Intent

Advance DW-116/137/446/459/466 with current browser qualification, an actionable AT handoff and precise retained blockers. The user approved the recommended scope after comparing alternatives. All five records remain open. Push reviewed, verified changes to main after the build workflow finishes.

**Approved decision, 2026-10-29:** Qualify browser behavior and prepare the manual AT handoff; keep DW-116/446/459 open pending actual speech evidence.

**Approved follow-up decisions:** Preserve identifier restrictions and leave DW-137 open. Keep DW-466's unidentified historical failure open, using existing per-test reporting without speculative timeout changes. User-only identifier support is a possible separate future feature, not authorized implementation here.

## Boundaries & Constraints

Preserve authorization, durable identities, opaque cursors, command/projection/audit separation, localization and release holds. Reuse FrontComposer/Fluent V5 and platform capabilities. Record exact revisions and commands.

DOM/AX observations do not prove speech. Passing tests do not explain historical failures. Route encoding cannot override downstream identity validation. No speculative timeout increase, dependency update, migration, waiver or unsupported closure is authorized.

Do not change production behavior, shared modules, dependencies, timeouts or parallelism. Read `.agents/skills/aspire/SKILL.md` and establish Aspire resource state before code edits; discover authenticated browser endpoints through Aspire. Existing fixture processes may control read-only upstreams; no domain data mutation. Keep tokens/credentials out of retained output and bind evidence to source, browser, Fluent, validator and assembly identities.

## I/O & Edge-Case Matrix

| Scenario | Expected evidence or behavior |
| --- | --- |
| Search notices in current Fluent 5.0.1 | Existing outer polite/status/atomic region and notice containment hold; browser captures composed live semantics without asserting speech count |
| First audit Pending/Unavailable transition | Existing component behavior passes; manual authenticated Create protocol specifies first speech and interaction with its separate lifecycle region |
| Slash/backslash/all-dot identifiers | Until an approved shared contract exists, current pre-transport refusal and durable identity rules remain intact |
| Held lifecycle authorization | Dependency entry precedes pending-state assertion; release permits enabled rendering; historical uncertainty remains explicit |

</frozen-after-approval>

## Code Map

UI paths below are relative to `src/Hexalith.Tenants.UI/`; UI test paths to `tests/Hexalith.Tenants.UI.Tests/`.

- UI `Components/Pages/TenantsWorkspace.razor`; test `Components/TenantListSurfaceTests.cs`: notices and light-DOM helper.
- UI `Components/Tenants/CreateTenantFlow.razor`, `Components/Tenants/Audit/AuditAvailabilityState.razor`; test `Components/AuditAvailabilityStateTests.cs`: first insertion and lifecycle region; DW-446/459 duplicate risk.
- Test `Browser/validate-authenticated-navigation-focus.mjs`: reusable authenticated fixture.
- UI `Services/Gateways/TenantsRestQueryClient.cs` and matching tests: pre-HTTP refusal.
- `spec-dw-114-137-144-514-523-read-and-recovery-contracts.md`: prior identity decision/patch. `src/Hexalith.Tenants.Contracts/Queries/GetUserTenantsQuery.cs` targets fixed index/EntityId: user lookup need not change durable aggregate addresses.
- Test `Components/TenantDetailSurfaceTests.cs`: held authorization. `evidence/five-runtime-followups/authorization-load.py`: five passing contention iterations. `story-5-4-re-review-verification-2026-09-30.md`: unnamed failure. Searches of 1,589 archive members and four empty CI log retrievals found no method. Reuse existing TRX reporting.

## Tasks & Acceptance

**Execution:**

- [x] Test `Components/TenantListSurfaceTests.cs` — rename/reword helper; preserve assertions/scenarios.
- [x] Test `Browser/validate-authenticated-navigation-focus.mjs` — capture/assert sanitized composed live semantics including Fluent accessibility internals; preserve prior focus and zero-command/circuit-error gates; run authenticated.
- [x] `_bmad-output/implementation-artifacts/evidence/dw-116-137-446-459-466/` and `dw-116-137-446-459-466-validation-evidence.md` — retain commands/results, failed attempts, exact source/version binding and limitations.
- [x] `docs/tenants-ui-deferred-at-handoff.md` — authenticated search/first Create audit protocol: EN/FR resources, versions, expected/actual speech, omissions/duplicates and closure criteria.
- [x] `deferred-work.md` and `docs/maintenance-backlog.md` — append evidence/decisions to only the selected five records/rows, preserving original findings, unselected body bytes, all 657 IDs and 652 done/5 open totals; record the tenant/user distinction and exact missing timeout evidence.
- [x] This spec — record execution; parent owns review, staging, commitlint and main push. Implementer must not stage/commit/push.

**Acceptance Criteria:**

- Given search and first audit transitions, when qualified AT evidence is evaluated, then each claimed closure has actual speech evidence; browser-only results leave AT acceptance open.
- Given unsupported identities, when no replacement shared contract is approved, then no transport or durable addressing policy changes.
- Given a historical timeout without its method identity, when only current passing runs are available, then no causal fix or closure is claimed.
- Given five selected records, when outcomes are published, then original findings, numbered counts and remaining blockers are accurate and independently reviewed.

## Implementation Notes

- Loaded both frontmatter context files in full before implementation, the permitted root-declared AI.Tools baseline and UX rules, repository build/style/test guidance, architecture/accessibility guidance, and the required local Aspire skill. Parent owns the already-rendered build workflow.
- Started the exact existing AppHost through Aspire and established resource state before code edits. Discovered the registered authenticated UI endpoint and process via `aspire describe`; private ephemeral realm credentials stay outside retained artifacts. The fixture uses a separate rebuilt UI and controlled read-only upstreams; it rejects domain commands.
- Renamed/reworded the bUnit notice helper and one test to limit their claim to outer-region attributes, containment and duplicate-bar suppression. Existing assertions and scenarios are preserved.
- Expanded the authenticated validator with empty, one-notice and two-notice composed DOM/ElementInternals plus computed Chromium AX checks; all original 24 focus cases remain. Fluent 5.0.1 exposes status via ElementInternals, rather than the historical internal-dialog shape. Current full packet passes 28/28 with zero command attempts/circuit errors; speech remains unobserved.
- The existing source-build command initially fails because two McpCli source projects select their own EventStore 3.128.3 default below Tenants' 3.128.4 floor. The explicit build property selects the already pinned root version, 3.128.4, without editing dependencies. Fresh Debug/source test build passes with zero warnings/errors. Maintained MTP passes 803 relevant wildcard-selected cases (including five state-model cases) plus 83 Create flow cases, zero failures/skips, with per-test TRX/XML.
- Added the EN/FR actual-speech handoff and append-only qualification/decision fields to exactly five records/rows. All five stay open: 657 IDs, 652 done/5 open. Conservation validation proves all 652 unselected numbered bodies and every unrelated ledger/index byte unchanged.
- Retained failed browser/build attempts and exact current source/version bindings in the evidence directory. Passing runs do not explain historical timeouts, replace actual speech, or qualify production backend authorization, release readiness or stored domain data.
- Parent must complete independent review, staging, pinned commitlint, divergence inspection and authorized publication. Implementer performed no staging, commit or push.
- Post-review completion: all ten independently filed findings were addressed and rechecked by the parent. The current accepted browser packet is attempt 6, with 29/29 passing cases, host-bound readable AX semantics, same-document outer-region survival, zero commands and zero circuit errors. Earlier packets and hash-matched validator versions remain retained. The unchanged test/source bindings preserve the 803 plus 83 passing maintained MTP cases. Final conservation, gitlink, syntax, manifest/source-hash and whitespace checks pass; all five records remain open. The qualification-owned AppHost was stopped through Aspire after runtime verification.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Route | Verification and required correction |
| --- | --- | --- | --- |
| BH1 | medium | patch | Create calls GetStatusAsync and projection reads, not a separate audit-record probe. C1/C2 must control the actual command-status results and projection evidence. |
| BH2 | medium | patch | TenantCommandAuditStates maps a delivered EventsStored status to Pending immediately. First Unavailable requires that no earlier result mounted Pending; distinguish backend storage from evidence delivered to the UI. |
| BH3 | medium | patch | The unattended fixture's resetList flag has no manual control surface. State this missing prerequisite explicitly and describe required backend behavior; do not imply an executable cursor-expiry control exists. |
| BH4 | low | patch | Create's lifecycle Refresh and shared availability Refresh have distinct callbacks/retry bookkeeping. Identify their actual scoped selectors and cover each separately. |
| BH5 | low | patch | The protocol lacks queue-settling and observation-end records, allowing delayed speech to be confused with omission. Define repeatable measurement boundaries without inventing a product latency SLA. |
| BH6 | medium | patch | The introduction requests a second supported pairing but closure criteria do not bind cases to both pairings. Make the evidence matrix explicit for each affected record. |
| BH7 | low | patch | The browser gate counts polite statuses but does not reject extra alerts or pin child atomic semantics. Tighten assertions against the currently observed status/polite/atomic structure. |
| BH8 | low | patch | Computed status records omit host association/readability, so a count alone cannot attribute each AX status to its expected notice. Bind current notice hosts to computed nodes and retain sanitized readable-content booleans. Shared root with BH7. |
| BH9 | low | patch | Empty and one-notice observations use different documents, leaving browser node persistence untested. Exercise the existing Refresh path in one document and verify the outer node survives. |
| BH10 | low | patch | Failed attempts retain validator hashes without reconstructable attempted source, and generic labels limit diagnosis. Preserve hash-matched attempted validators where recoverable and state diagnostic limits accurately; retain original packets unchanged. |

Edge Case Hunter and Verification Gap Reviewer returned no findings. Every Blind Hunter finding was verified separately before grouping. All corrections are local documentation or qualification-tool patches; production behavior and approved open-record decisions remain unchanged.

Resolution: BH1–BH6 are corrected in the source-backed status/projection protocol, explicit blocked manual prerequisite, scoped Refresh selectors, observation windows and pairing matrix. BH7–BH9 are corrected by host-bound readable/unignored polite/atomic AX assertions, unexpected-live-role rejection and same-document node-survival checks; accepted attempt 6 passes 29/29. BH10 is corrected by exact hash-matched validator snapshots and bounded failure diagnoses. Parent artifact/source/conservation checks passed after the patches. No review finding remains unresolved and no new work was deferred; the five pre-existing records retain their approved open dispositions.

## Verification

Implementation results are retained in [validation evidence](dw-116-137-446-459-466-validation-evidence.md), including exact commands, the failed attempts, per-test artifacts and source/runtime hashes. The parent records independent-review outcomes in the triage log before completion.

Run relevant Debug/source UI classes through maintained MTP with per-test results, expanded authenticated browser checks and `node --check`. Build before direct-assembly fallback. Browser evidence must retain prior focus/zero-command/zero-circuit-error checks.

Run `python3 scripts/validate-story-gitlinks.py` against this spec, `git diff --check` and baseline ledger-body/count comparisons. Parent validates pinned commitlint and fetches/inspects divergence before pushing.
