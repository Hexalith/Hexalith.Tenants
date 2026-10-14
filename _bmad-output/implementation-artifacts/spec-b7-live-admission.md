---
title: 'B7 live authority and measured viewport admission'
type: 'bugfix'
created: '2026-10-14'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '7ad54431996f9f1f0d277005eb2a73b67b55fea6'
context:
  - 'AGENTS.md'
  - '_bmad-output/project-context.md'
  - 'references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - 'references/Hexalith.AI.Tools/hexalith-ux-instructions.md'
  - 'references/Hexalith.AI.Tools/hexalith-state-instructions.md'
---

<frozen-after-approval reason="User explicitly authorizes complete B7 implementation, independent review and validation">

## Intent

DW-195/DW-206/DW-235/DW-264 are reverified: remove-member relies on CSS; metadata/member flags default true without parent authority; member launchers ignore flags; create trusts list kind. Implement live, fail-closed admission using existing BFF reflection and measured viewport observation. August decisions settle the scope; no new permission checkpoint is necessary for reversible local work.

## Boundaries & Constraints

Always preserve B1–B6 proof, retention, leases, audit and support-safety behavior, authoritative requery, literal tenant/caller binding and localization. Metadata permits current tenant Owner/Contributor or GA; every membership action permits Owner or GA; create requires resolved GA. Distinguish denied from indeterminate. Submission and ambiguous-delivery retry must reauthorize and reject stale results after tenant/caller changes and awaits. Remove requires an actual Safe viewport measurement, including direct calls and resizing after preview/during admission.

Preserve backend first-member bootstrap: never-populated history is tracked only in TenantState; empty UI rows cannot invent an authorization bypass. Authorized GA first-member paths remain available. Preserve create's documented authoritative-empty first-tenant Unknown freshness exception; it never substitutes for GA authority.

Never update dependencies, stage, commit, push, deploy, replay production or migrate data. Preserve external commits/pointers and historical gitlink provenance blocker. Change only four ledger entries after closure proof, maintenance rows/counts and B7 readiness evidence; archive, sprint, decisions and B1–B6 evidence remain unchanged.

## I/O & Edge-Case Matrix

| Given | When | Then |
| --- | --- | --- |
| Denied or indeterminate metadata/member/create authority | Parent renders, direct submit or retry runs | Specific unavailable reason; zero command dispatch |
| Owner/Contributor/Reader/nonmember/GA in literal tenant scope | BFF reflects metadata/membership | Correct role admission; malformed/wrong-tenant evidence fails closed |
| Delayed authority response | Caller, tenant or authority changes | Superseded response cannot enable action or dispatch |
| Authorized preview | Authority changes or admission awaits | Fresh authorization checked; invalidated intent never dispatches |
| Unavailable, Unknown, seeded Safe without measurement, or Phone viewport | Remove direct submission | Zero dispatch |
| Safe measured preview | Resize before/during submit | Unknown/narrow refuses dispatch; status/proof retention survives |
| Initial Unknown then measured Tablet/Desktop | Authorized remove submitted | Successful command dispatch with existing exact attempt/proof semantics |
| Authorized GA and authoritative first-tenant Unknown | Create submitted | Existing freshness exception and successful dispatch preserved |

</frozen-after-approval>

## Code Map

- `UI/Services/Gateways/{ITenantsBffComposition,TenantsBffComposition}.cs`: async global reflection and `ComposeTenantCorrectionProjectionsAsync` validate current principal before membership. Extend role reflection; avoid synchronous HttpContext properties.
- `UI/Services/Configuration/TenantConfigurationPrincipalResolver.cs`: established circuit principal resolution; preserve identity safeguards.
- `UI/Components/Pages/TenantDetailPage.razor`: generation/cancellation/caller-version lifecycle reflection, `ReauthorizeLifecycleAsync`, current caller snapshots. Bind metadata/members and submission callbacks; invalidate on refreshed detail/auth/tenant transitions.
- `UI/Components/Pages/TenantsWorkspace.razor`: `ResolveGlobalAdministratorsEntryAsync` has superseded-result guard; reuse resolved GA, preserve `IsAuthoritativeFirstTenantEmpty`.
- `UI/Components/Tenants/{CreateTenantFlow,Metadata/EditTenantMetadataFlow,Members/*}.razor`: eligibility plus submission/retry/post-lease guards, MemberAccessReview action slots/direct launchers. Fail-closed default authority. Preserve retained ownership and status/proof paths.
- `UI/State/TenantDetail/TenantHighImpactViewportObservation.cs`: production starts unmeasured Unknown; Safe requires HasMeasurement. Scoped Fluxor observer already tracks 768px boundary. Reuse in remove; handle Unknown→Safe idle preview and unsubscribe disposal.
- Source paths above relative to `src/Hexalith.Tenants.` where indicated by UI; test seams in `tests/Hexalith.Tenants.UI.Tests/{Components,Services,TenantsWorkspaceTests.cs}`. Existing parent fixtures need explicit authority/measurements, not permissive production defaults.

## Tasks & Acceptance

- [x] BFF and parent components: scoped reflection and stale-response guards; all live bindings and localized denied/indeterminate reasons.
- [x] Flow components: fresh submission/retry authorization and measured remove viewport, with post-await identity/evidence checks; preserve existing ownership/proof paths.
- [x] UI tests and browser harness: meaningful matrix regressions through parents and direct submissions, zero dispatch on refusal, successful authorized paths, stale/caller/tenant/authority/resize races. Update authorized fixtures explicitly without weakening assertions.
- [x] Independent review, focused Debug source checks and required maintained individual-project/browser/package-consumer/unchanged coverage gates; record all exact commands, initial failures and limitations.
- [x] Root closure: only four finding records/maintenance rows and counts, B7 spec/evidence/readiness; protected files and dependency pointers match entry snapshot.

Given current source, when all matrix regressions, review fixes and required validation pass, then only these four findings close; full release remains qualified.

## Implementation Notes

Entry: clean main, full baseline above. Snapshot including file hashes and all nine dependency pointers: `/tmp/tenants-b7-20261014/entry-state.json`. Local Aspire baseline started and all23 described resources Running/Healthy before code edits. No intent gaps or irreversible actions. Implementation agent owns code/tests/browser and focused validation; root owns maintained validation, independent review, and ledger/readiness closure. Read source specs referenced by the four findings and relevant architecture before edits; reuse existing seams.

Production continuation: initial implementation worker hit capacity after writing source; root preserved its edits and completed compilation/guards. First Debug build failed on three nonexistent membership Lifecycle tuple members; removing those references produced a clean build. Root also serialized initial authority lookups, rejected stale lookup completion without poisoning newer state, refreshed scoped tenant detail for submission reflection, and abandoned only unsent create admission on authority refusal.

Sequential test continuation agent owns only UI regression/authorized fixture updates and browser harness work now; production source is established. Implement every matrix scenario, through real parent pages and direct submission/ambiguous retry paths. Existing positive fixtures must explicitly supply IsAuthorized=true, Authorized reflection and an Authorized fresh provider; removal fixtures must Observe(Tablet/Desktop), not seed Safe. BFF tests should exercise the real new role-aware seam. Run focused builds/tests then the full Debug UI executable to find inherited fixture assumptions. Preserve every existing assertion and gate; report production failures to root rather than changing expected behavior. Root will resolve source findings, run maintained validation, review and closure. Do not edit ledger, readiness, protected configuration, dependencies or production source in this test continuation.

Root continuation fixes: all post-lease authority awaits recheck disposal/attempt ownership; fresh submission denial/indeterminate updates sibling parent availability with current-scope guards; Unauthorized reads map to specific denial. Create captures the exact snapshot before fresh dispatch authority, rejects newer evidence, and abandons only undispatched admission on refusal. Pending lookup disables submit. Debug build4 is clean; six existing/new flow/BFF classes pass351/351. One attempted wildcard method filter exited3 because xUnit permits wildcards only at filter ends; class selectors supplied the executed proof instead. Tests/browser continuation completed: full Debug UI 4,887/4,887, focused live matrix 238/238, EN/FR browser observer/CSS/mutation checks passed. Independent review and maintained gates follow.

## Spec Change Log

## Review Triage Log

| Finding | Verdict / route | Verification and disposition |
| --- | --- | --- |
| Blind 1 | high / patch | Equal Indeterminate did not retire a pending older render probe. Fresh publication now invalidates an outstanding probe even when values match; ordinary stable submissions avoid generation churn. |
| Blind 2 | high / patch | Fresh role evidence could retain an obsolete Active status/version/preview. Compare the complete admission evidence and publish changed detail as a new snapshot before refusing the old submission. |
| Blind 3 | high / patch | Fresh Unauthorized previously left protected detail/member content displayed. Publish the authorization-safe read state, clear member rows and cancel superseded reads. |
| Blind 4 | high / patch | Pending create authority cleared audit-return evidence before resolution. Defer taking/clearing it while Indeterminate, restore on Authorized and clear on proven denial; existing caller-change clearing remains. |
| Blind 5 | high / patch | Role/remove admission tuples omitted Member. Include target evidence and cancel invalidated lookup on target replacement. |
| Blind 6 | medium / patch | Metadata predispatch Cancel refused while authority awaited; None tokens could leave admission held indefinitely. Cancel initial admission on close/parameter change/disposal and bound each provider wait to10 seconds independently of cooperation. Dispatched work retains existing recovery semantics. |
| Blind 7 | high / patch | Four retry paths captured attempt evidence after the await. Capture before authority resolution and require unchanged snapshot/attachment/generation and current retry eligibility before redelivery. |
| Blind 8 | low / patch | Unsupported per-row Add slot always reported missing permission even for authorized callers. Derive only the authority reason from reflection, preserving the separate unsupported-entry reason. |
| Blind 9 | false / reject | The browser harness explicitly qualifies rendered-fixture/real-JS evidence, not a running circuit. .NET dispatch is proven through parent/direct bUnit; established Fluxor observer wiring has component/state coverage. Add an effect→removal admission regression to make that production seam explicit. Full authenticated browser qualification remains the historical release hold. |
| Blind 10 | low / patch | Hardcoded reference JS could diverge from selected build assets. Resolve the unique breakpoint asset from that build manifest and record identity/source/hash. |
| Edge 1 | high / patch | Same demonstrated fresh Disabled/status/projection evidence defect as Blind2; publish/invalidate new evidence before dispatch. |
| Edge 2 | high / patch | Same demonstrated Member-only replacement as Blind5; include target in admission parameters. |
| Edge 3 | high / patch | Same demonstrated equal-Indeterminate stale render race as Blind1; retire outstanding probes. |
| Edge 4 | medium / patch | A noncompleting provider can hold admission/unsent lease. Bounded cancellable wait returns Indeterminate and lets the existing refusal release unsent ownership. |
| Edge 5 (claim) | high / patch | Independently verified duplicate target-transition claim; patched with Edge2/Blind5, without changing the frozen user intent. |
| Edge follow-up 1 | high / patch | Render CTS is cleared before queued publication. Keep the pending marker (`_mutationAuthorizationResolved is null`) in the supersession condition through publication, without changing stable Authorized child revisions. |
| Edge follow-up 2 | high / patch | A deadline firing after an Authorized assignment could leave it in the catch. Reset the result to Indeterminate on every provider exception/cancellation before the dispatch guard. |
| Edge follow-up 3 | high / patch | Noncooperative reads could complete after admission cancellation and publish changed/denied evidence on the old snapshot. Check cancellation after the read and inside both queued publication callbacks; late cancelled completion cannot replace newer state. |
| Regression follow-up 1 | high / patch | Form submission also awaited authority before deciding retry/new intent. Capture exact attempt/snapshot/generation at that first wait, and preserve tracking/terminal proof that arrives while it runs. Apply to all five command families. |
| Regression follow-up 2 | high / patch | Workspace consumed the create audit-return marker before delayed authority resolved, causing the child to clear pending state. Delay marker/focus consumption until authority resolves; retain existing marker cleanup after restoration. |
| Verification gap 1 | high / patch | Real BFF parent tests returned unchanged membership; stub denial did not prove the fresh-detail handoff. Add real current-principal/changed-membership refusal and no-attempt assertions. |

All three independent reviewers ran context-free against the same full B7 source/test diff and all reported before triage. Twenty-one findings were classified individually, then shared causes grouped. No new work was deferred and no ledger record added. The implementation worker could not continue after its capacity error; root applies demonstrated private-guard corrections and the test continuation worker adds their regressions. User instructions require preserving authorized work, so no reset/revert/dependency movement is used.

## Verification

Focused: Debug UI project build with `-p:UseHexalithProjectReferences=true -m:1 -nr:false`, execute built xUnit assembly with `-class`/`-method`. Required maintained lanes/floors, package/browser/coverage match current tracked release configuration and B6 evidence, never lower gates. Exact commands/results recorded in `b7-validation-evidence.md`. Root standard B7 gitlinks check plus historical B6 gitlinks check distinguish clean B7 pointers from inherited failure. No solution-level tests.

Final handoff: all 21 findings triaged, no review deferrals; source correction re-check clean. Final Debug 4,939/4,939 and maintained Release 6,611 cases, source 4, browser, package-consumer and unchanged coverage gates pass. Four finding records close only; exact commands/initial failures/limits in [B7 evidence](b7-validation-evidence.md). Explicit user no-Git-mutation instructions override workflow commit/sprint presentation actions; no commit or sprint synchronization is performed.
