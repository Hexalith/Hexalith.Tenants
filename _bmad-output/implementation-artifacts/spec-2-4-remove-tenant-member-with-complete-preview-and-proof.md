---
title: 'Remove Tenant Member with Complete Preview and Proof'
type: 'feature'
created: '2026-08-08'
status: 'done'
baseline_commit: '29c4aec965e9cba4165a8844a86edc67ba7d756b'
review_loop_iteration: 3
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-2-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Remove-member has a consequence preview and command path, but eligibility is not fully fail-closed, platform-standing/GA friction is unwired, and the flow is an inline section rather than a focus-trapped destructive dialog — so deliberate removal remains unsafe to operate.

**Approach (2.4a only):** Close eligibility, complete ten-item preview (incl. platform-standing), elevated last-owner/target-GA friction, focus-trapped destructive dialog, AggregateIdentity-locked dispatch, and existing Story 2.1 projection-confirmation lifecycle. WP-2A / `audit_available` deferred to 2.4b (`deferred-work.md`).

## Boundaries & Constraints

**Always:**
- BFF-only: `RemoveUserFromTenantAsync` → `POST /api/v1/commands`; no new endpoints.
- Ten-item preview complete before confirm; missing item blocks dispatch.
- Fail closed on stale/missing/unknown validation, freshness, auth, lifecycle, preview completeness, or narrow unsafe layout.
- Last-owner removal allowed with elevated friction; tenant removal never changes GA standing.
- Reuse `messageId` per attempt; AggregateIdentity lock; keep Story 2.1 non-collapse lifecycle.
- EN/FR parity; `data-testid="tenants-remove-member-*"`.

**Ask First:**
- Replacing Tenants `role="dialog"` + focus-sentinel pattern with a different Fluent/FrontComposer dialog primitive.

**Never:**
- WP-2A proof assembly, `audit_available`, or proof-capability gating (deferred 2.4b).
- New preview/receipt/status endpoints; browser-direct calls; reshaped remove contracts.
- Confirming from acceptance/SignalR alone; optimistic row removal; editing events/projections.
- Promising undo/rollback/`restore intended access`; inventing last-GA hard-stops on membership removal.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Eligible open | Fresh + authorized + preview complete | Focus-trapped destructive dialog; ten items + GA/platform-standing | Incomplete item blocks confirm |
| Last-owner / target-GA | OwnerCount==1 or target on GA list | Elevated friction + explicit risk; still allowed when authorized | GA standing unchanged |
| Confirm + dispatch | Complete preview; AggregateIdentity free | Submit once with retained messageId; lock held; Story 2.1 lifecycle | Surface down → fail closed |
| Overlap / fail-closed | In-flight sibling or stale/narrow layout | Unavailable with lock or localized reason | No dispatch |
| Cancel / Escape | Dialog open | No command; focus returns to launcher | N/A |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs` (+ interface/unavailable) -- remove submit + messageId reuse
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs` -- `GetGlobalAdministratorsAsync` for live target-GA standing
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs` -- `TenantRemoveMemberCommandSnapshot` (reuse ConfirmProjection; no WP-2A here)
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantAggregateCommandAdmissionGate.cs` -- AggregateIdentity lock via `TenantDetailPage.SetCommandActivityAsync`
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor` (+ `.css`) -- focus-trapped `role="dialog"`; ten preview items incl. platform-standing + consequences-versus-unknowns; elevated last-owner/GA friction; narrow form hide; dismiss/recovery outside form
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor` -- launch + focus return; resolves target GA friction from `GlobalAdministratorsSnapshot.IsCompleteEvidence`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor` -- loads GA snapshot with detail/members (soft-fail); passes to `MemberAccessReview`
- Reuse: `RemoveTenantConfigurationFlow.razor` dialog/focus-sentinel pattern
- Resources/tests: `TenantsResources*.resx` `Tenants.RemoveMember.*`; `RemoveTenantMemberFlowTests.cs`; `TenantDetailSurfaceTests` / gateway suites

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor` (+ `.css`) -- focus-trapped destructive dialog; ten preview items + platform-standing; elevated friction; Escape/cancel no-dispatch + focus return; dispatch via existing gateway/snapshot
- [x] `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor` (+ `MemberAccessReview.razor`) -- wire live target-GA standing; fail-closed on incomplete preview/narrow layout; keep AggregateIdentity lock / SignalR nudge-only
- [x] `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` (+ `.fr.resx`) -- friction/unavailable/dialog copy parity as needed
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/RemoveTenantMemberFlowTests.cs` (+ surface/gateway as needed) -- incomplete preview, friction, Escape/focus, lock, dispatch; do not assert WP-2A complete
- [x] `_bmad-output/implementation-artifacts/sprint-status.yaml` -- advance `2-4-remove-tenant-member-with-complete-preview-and-proof` with this slice (incomplete until 2.4b)

### Review Findings

- [x] [Review][Patch] Fail closed on live audit-proof capability rather than gateway registration alone [src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor:591]
- [x] [Review][Patch] Follow audit pagination when assembling WP-2A removal proof [src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor:801]
- [x] [Review][Patch] Preserve the original causal lower bound when retrying with the same message ID [src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs:728]
- [x] [Review][Patch] Promote audit available only from current lifecycle-backed evidence and a ready receipt [src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor:818]
- [x] [Review][Patch] Wire rendered audit recovery and receipt inspection actions to real behavior [src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor:173]
- [x] [Review][Patch] Resolve positive and paged global-administrator standing before suppressing elevated friction [src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor:436]
- [x] [Review][Patch] Keep the supplementary global-administrator read from blocking primary tenant detail [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:399]
- [x] [Review][Patch] Replace stale Epic 5 and unsupported access-restoration preview promises [src/Hexalith.Tenants.UI/Resources/TenantsResources.resx:2193]
- [x] [Review][Patch] Add component coverage for audit-provenance confirmation, later proof recovery, parent capability gating, and page-level GA wiring [tests/Hexalith.Tenants.UI.Tests/Components/RemoveTenantMemberFlowTests.cs:518]
- [x] [Review][Patch] Derive remove eligibility from a tenant-scoped current authoritative audit read, with generation-safe non-blocking failure [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:1470]
- [x] [Review][Patch] Bound and cancel supplementary GA pagination; degrade retained rows to incomplete unknown evidence after refresh faults [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:1349]
- [x] [Review][Patch] Bound and cancel removal-proof pagination while continuing past weak matches to later current projection-backed receipts [src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor:896]
- [x] [Review][Patch] Preserve a coalesced projection refresh requested during a status-only refresh [src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor:786]
- [x] [Review][Patch] Forward audit inspection distinctly and render only recovery actions backed by real delegates [src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceReceipt.razor:71]
- [x] [Review][Patch] Remove escalation/navigation semantic substitution and align EN/FR recovery copy with rendered actions [src/Hexalith.Tenants.UI/Resources/TenantsResources.resx:2208]
- [x] [Review][Patch] Cover stale, mismatched, missing, cyclic, capped, cancelled, coalesced, late-route, and callback fail-closed paths [tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs:285]
- [x] [Review][Defer] Default HEAD-based gitlink validation includes seven post-story dependency bumps [_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md:1] — deferred, pre-existing

#### Loop 3 (2026-08-21) — chunk A: removal-proof lifecycle & command state

- [x] [Review][Decision] **RESOLVED (2026-08-21): ASSIGN AN ORDERED READ-MODEL VERSION.** `TenantProjectionHandler` now persists `tenant-sequence:<n>` from the aggregate-local EventStore `SequenceNumber`, preserving a later stored version during older replay. `TenantQueryResult` therefore uses store ETags only for legacy read models. Verification also disproved the zero-padding sub-premise: the current parser consumes the complete trailing digit run, so `0009` → `0010` already compares correctly; GUID/hash ETags were the remaining portability defect. [src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs:88]
- [x] [Review][Decision] **RESOLVED (2026-08-21): WEAKEN THE RECEIPT CLAIM.** A time-window match still supplies authorized current removal evidence, but the receipt no longer stamps the current attempt's `MessageId` onto a row whose command causation is absent from the audit contract. Inspect-audit navigation retains the attempt reference as search context without presenting it as proof ownership. [src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor:1038]
- [x] [Review][Decision] **RESOLVED (2026-08-21): WIRE METADATA IN ITS OWNING STORY.** Story 3.2 now supplies `AuditEvidenceProvider` for same-value metadata attempts and passes the returned row into `TenantUpdateMetadataCommandSnapshot.ConfirmProjection`; no remove-member confirmation arm was added from the weaker time-window proof. [src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor:755]
- [x] [Review][Decision] **RESOLVED (2026-08-21): DECLARE THE EVENTSTORE POINTER.** The File List now declares the `references/Hexalith.EventStore` pointer moved by story commit `d3f74f58`; later unrelated root-submodule drift remains called out separately and is not attributed to this story. [_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md:1]

- [x] [Review][Patch] Refresh rendered on the WP-2A receipt regresses a confirmed removal to projection-pending and drops audit_available [src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor:200]
- [x] [Review][Patch] Receipt Close destroys the confirmed outcome, the proof, and the tracking identifiers [src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor:1101]
- [x] [Review][Patch] Ten-item preview completeness gate is a compile-time tautology, so "missing item blocks dispatch" is unenforced and the preview-blocked banner is unreachable [src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor:334]
- [x] [Review][Patch] Aggregate-lease refusal discards the retained messageId, letting a retry dispatch a second removal under a new identity [src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor:742]
- [x] [Review][Patch] AuditProofCapabilityAvailable defaults to true, so unknown proof capability fails open against 2.4b AC1 [src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor:296]
- [x] [Review][Patch] Escape and Cancel cannot dismiss the destructive dialog while a command is in flight, with no announcement and no alternative exit [src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor:1129]
- [x] [Review][Patch] A successful removal fires an assertive role="alert" target-absent unavailability alongside the confirmed lifecycle [src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor:364]
- [x] [Review][Patch] Gateway ULID-validation failure renders hard-coded English, breaking EN/FR parity [src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs:51]
- [x] [Review][Patch] Focus trap forwards into the display:none narrow form, and the px breakpoint removes the flow entirely at high zoom [src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor:1143]
- [x] [Review][Defer] Dispose never releases the command-activity lease [src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor:1159] — deferred, pre-existing. Attempted and reverted: `CommandFlowGuardConformanceTests.Command_flows_do_not_release_page_activity_directly` forbids a `*Flow.razor` releasing page-level activity, so the parent owns the release decision by design. `MemberAccessReview` already compensates on its own `DisposeAsync` and on the authorization-teardown path; any residual unmount gap belongs to the parent, not the flow.
- [x] [Review][Patch] Proof walk re-runs up to 50 pages x 50 rows on every refresh after it already succeeded, bounded by an unrelated cursor-history constant [src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor:907]
- [x] [Review][Patch] Whitespace-only tenant id still accepted on five gateway paths the same commit hardened elsewhere [src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs:182]
- [x] [Review][Patch] FindMatchingRemovalProof is exercised only by tests while production duplicates the selection inline, so the two can drift silently [src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs:1055]

- [x] [Review][Defer] ApplyProjectionEvidence is dead code across all eight command flows (zero call sites) [src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor:628] — deferred, pre-existing
- [x] [Review][Defer] CreateTenantFlow never adopts the new reusable messageId affordance, so an ambiguous failure re-dispatches under a fresh ULID [src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor:348] — deferred, pre-existing
- [x] [Review][Defer] Missing coverage: create/update messageId retention on indeterminate failure, lease-denied submit, and the tracking-lost branch [tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs:1137] — deferred, pre-existing
- [x] [Review][Defer] Legacy FAST token --neutral-stroke-rest survives and the Fluent conformance guard does not catch it [src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor.css:4] — deferred, pre-existing

#### Loop 3 (2026-08-21) — chunk B: surfaces, resources & shared services

- [x] [Review][Decision] **RESOLVED (2026-08-21, owner decision): RESTORE BOTH GUARDS.** Re-add the `HttpContext` discriminator so prerender/static SSR stays `Indeterminate` and renders the restricted surface, and restore `IUserContextAccessor` subject corroboration (`requireCorroboration: true`). Rationale: the 2026-08-01 owner decision (`spec-1-11-authorized-global-administrator-review.md:75`) already weighed and rejected the availability trade-off -- "the cancellation and non-blocking review patches address the availability cost **without switching identity sources**" -- so the loop-2 change re-litigated a settled decision from inside an unrelated story. The `AsyncLocal` nature of `IHttpContextAccessor.HttpContext` keeps the owner's stated harm (a stale request principal retaining privilege) reachable. Any residual loop-2 availability cost must be re-solved via the approved cancellation/non-blocking route, not an identity-source switch. Original finding: `TenantConfigurationPrincipalResolver` re-admits `HttpContext.User` as authoritative identity and drops subject corroboration, reversing the recorded 2026-08-01 owner decision — the baseline used HttpContext presence as a discriminator to *exclude* that source ("precisely the evidence source the 2026-08-01 owner decision removed"); `:45-52` now selects `httpContext.User` whenever no circuit provider is in scope, and `:89-92` passes `corroboratedSubject: null, requireCorroboration: false` after dropping `IUserContextAccessor`, leaving the corroboration branch in `TenantsGlobalAdministratorClaims.cs:49-52` unreachable. Tests were written for the new behaviour (`TenantConfigurationReadPolicyTests.cs:330-400`), so it is deliberate — but it is an authorization-evidence loosening landed under a removal-preview story, in a file in neither spec's Code Map, with no Ask-First and no recorded decision. Choose: CONFIRM the reversal and record it, RESTORE the HttpContext discriminator and corroboration, or keep the SSR path and restore corroboration only. [src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationPrincipalResolver.cs:45]
- [x] [Review][Decision] **RESOLVED (2026-08-21, owner decision): BOTH.** Bind `OnContinueReadOnly` in `AddTenantMemberFlow` and `ChangeTenantMemberRoleFlow`, where `ContinueReadOnlyAsync`/`CanContinueReadOnly` already exist (pure wiring, no new behaviour), AND derive `RecoveryCopySuffix` from the verbs `CanRenderRecovery` will actually render, so `CreateTenantFlow` and `EditTenantMetadataFlow` degrade honestly without inventing a read-only concept they do not have. This finding is a regression of the loop-2 resolution "align EN/FR recovery copy with rendered actions", so the fix is deliberately drift-proof. Original finding: `MissingSupport` renders zero recovery actions while its copy still names one — `CanRenderRecovery` gates `ContinueReadOnly` on `OnContinueReadOnly.HasDelegate` and `Escalate` on `OnEscalate.HasDelegate`, but the four call sites (`CreateTenantFlow.razor:102`, `AddTenantMemberFlow.razor:107`, `ChangeTenantMemberRoleFlow.razor:128`, `EditTenantMetadataFlow.razor:130`) bind only `OnRefresh` + `InspectAuditAction`. `MissingSupport`'s verb set is `[ContinueReadOnly, Escalate]`, so **no button renders at all**, yet `Tenants.Audit.Availability.Reason.MissingSupport.NoEscalation` reads "Continue read-only." `RecoveryCopySuffix` only compensates for a missing `OnEscalate`. Violates epic-2 "Every failure or uncertain state provides an applicable named recovery" and regresses the loop-2 resolution "align EN/FR recovery copy with rendered actions". Choose: bind `OnContinueReadOnly` at the four call sites, derive the copy from the actually-renderable verb set, or both. [src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditAvailabilityState.razor:89]
- [x] [Review][Decision] **RESOLVED (2026-08-21, owner decision): DECLARE IN FILE LIST.** Add each out-of-scope file to this spec's File List with a reason, mirroring the repository's gitlink-declaration policy. Splitting was rejected because it would require reopening `1-6-read-only-tenant-configuration` and `3-1-create-tenant-with-projection-confirmation` (both `done`) and would collide with a peer session actively rewriting `CreateTenantFlow.razor` (+152/-50 during this review); reverting was rejected as maximum churn against tested, wanted work. NOTE: `3-1` is marked `done` in sprint-status while its files are being actively rewritten -- status accuracy to be reconciled separately. Original finding: the committed range changes ~11 source files outside both specs' Code Maps and reshapes the command-gateway contract — `ITenantCommandGateway.CreateTenantAsync`/`UpdateTenantAsync` gained `string? messageId` (story 3.1/3.2 surfaces), and the tenant-detail 304 path was rewritten so every conditional hit costs a second unconditional read while `ContinueReadOnlyComposition` was deleted, so a reauthorization failure now drops approved configuration rows the removed comment said "AC5 requires we retain". Both behaviours are tested, so both are deliberate, but neither is covered by a spec entry. Choose: DECLARE the expanded scope in this spec's File List with reasons, SPLIT the create/metadata/query-gateway work into stories 3.1/3.2/1.6, or REVERT the out-of-scope parts. [src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs:7]

- [x] [Review][Patch] Converting four config layout wrappers from `div` to `FluentStack` voids every scoped CSS rule that styles them, losing horizontal scroll, the focus ring and both responsive grids [src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor:14]
- [x] [Review][Patch] A refused aggregate lease still dispatches for the metadata, lifecycle, set-configuration and remove-configuration flows [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:1662]
- [x] [Review][Patch] The configuration landmark shares one lease-owner token across its set and remove flows, re-creating the AD-12 multi-owner early-release the new lease code documents as fixed [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:230]
- [x] [Review][Patch] EditTenantMetadataFlow reuses a consumed messageId for a different payload — **NO CHANGE NEEDED (2026-08-21):** a peer session fixed this independently while this review was running; the landed guard at `EditTenantMetadataFlow.razor:524-529` is identical to the sibling rule (`State is Failed` + blank `CorrelationId` + non-blank `MessageId` + `Equals(_snapshot.Intent, request)`). Verified, not merely observed. [src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor:524]
- [x] [Review][Patch] Degraded global-administrator evidence still yields a positive "also a global administrator" preview claim; only the negative direction checks IsCompleteEvidence [src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor:859]
- [x] [Review][Patch] ChangeTenantMemberRoleFlow is non-exitable while a command is in flight — Cancel disabled and CloseAsync silently no-ops, with no announcement [src/Hexalith.Tenants.UI/Components/Tenants/Members/ChangeTenantMemberRoleFlow.razor:666]
- [x] [Review][Patch] The Wait recovery verb renders as a live enabled button wired to an empty switch arm [src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditAvailabilityState.razor:111]
- [x] [Review][Patch] AggregateLocked puts a two-sentence instruction into the terse always-visible reason-catalog legend [src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor:315]
- [x] [Review][Patch] CommandFlowGuardConformanceTests matches only the retired OnCommandActivityChanged release path, so it now passes vacuously for the live CommandActivityLease mechanism [tests/Hexalith.Tenants.UI.Tests/CommandFlowGuardConformanceTests.cs:8]
- [x] [Review][Patch] AuditAvailabilityState renders an empty labelled actions region when every verb is filtered out; AuditEvidenceReceipt guards the same case [src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditAvailabilityState.razor:21]
- [x] [Review][Patch] A Ready receipt with no inspect delegate falls through to offering Refresh, contradicting the state it reports [src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceReceipt.razor:192]
- [x] [Review][Patch] The lease acquire/release path and MemberAccessReview.DisposeAsync can throw ObjectDisposedException on teardown; sibling dispatches are guarded [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:1738]
- [x] [Review][Patch] The global-administrator page walk is bounded by the unrelated CursorHistory.DefaultMaximum, truncates indistinguishably from a gateway failure, and logs nothing [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:249]
- [x] [Review][Patch] StateGlyph returns the untranslated English literal "OK" for the Available state where every sibling returns punctuation [src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditAvailabilityState.razor:100]
- [x] [Review][Patch] A missing admission-gate registration disables every command surface with a null reason string — **NO CHANGE NEEDED (2026-08-21):** premise did not survive verification. A reason is already rendered; the child surfaces supply their own (`Missing_aggregate_admission_gate_fails_membership_dispatch_closed` asserts "command support is unavailable"). A page-level reason was implemented and then reverted because it *overrode* the more accurate child reason. [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:322]
- [x] [Review][Patch] UnavailableTenantQueryGateway throws synchronously from Task-returning members — **NO CHANGE NEEDED (2026-08-21):** premise did not survive verification. The finding claimed the two new members were inconsistent with the class; in fact the synchronous-throw idiom is the majority pattern there (6 of 10 members, including pre-existing ones such as `GetTenantAuditAsync`). This is a pre-existing class-wide style question, not a defect these members introduced, and changing it would alter fallback-gateway behaviour with no demonstrated consumer impact. [src/Hexalith.Tenants.UI/Services/Gateways/UnavailableTenantQueryGateway.cs:15]
- [x] [Review][Patch] No test covers change-role nudge coalescing or the projection-refresh re-entrancy guard; the add-flow equivalent is pinned with an exact call count [tests/Hexalith.Tenants.UI.Tests/Components/ChangeTenantMemberRoleFlowTests.cs:1]
- [x] [Review][Patch] No test covers the new MemberAccessReview.DisposeAsync lease release; deleting it orphans the aggregate lock for the life of the circuit [tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs:1235]
- [x] [Review][Patch] No assertion distinguishes the .NoEscalation copy variants, so recovery copy can name absent controls undetected [tests/Hexalith.Tenants.UI.Tests/Components/AuditAvailabilityStateTests.cs:24]
- [x] [Review][Patch] The AuditAvailable state is absent from the glyph theory, so a blank success glyph would ship green [tests/Hexalith.Tenants.UI.Tests/Components/AuditAvailabilityStateTests.cs:19]

- [x] [Review][Defer] Refresh coalescing downgrades a user-initiated projection refresh to status-only and re-enters recursively, duplicated verbatim across three flows [src/Hexalith.Tenants.UI/Components/Tenants/Members/AddTenantMemberFlow.razor:513] — deferred, pre-existing
- [x] [Review][Defer] AsyncLocal<bool> used for dispatcher-bound re-entrancy state where a plain field is correct and cheaper [src/Hexalith.Tenants.UI/Components/Tenants/Members/AddTenantMemberFlow.razor:160] — deferred, pre-existing
- [x] [Review][Defer] Coalescer, submit guard, lease plumbing and SafeMessageText are copy-pasted across two-to-three flow components [src/Hexalith.Tenants.UI/Components/Tenants/Members/ChangeTenantMemberRoleFlow.razor:452] — deferred, pre-existing
- [x] [Review][Defer] The same scoped-CSS-on-a-Fluent-host trap exists in five other components predating this change [src/Hexalith.Tenants.UI/Components/Users/UserMembershipLookupPanel.razor.css:1] — deferred, pre-existing
- [x] [Review][Defer] Inserting Available mid-enum shifts MissingSupport's numeric value for any future numeric persistence or interop [src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditAvailability.cs:5] — deferred, pre-existing
- [x] [Review][Defer] French resource additions are inconsistently accented against their immediate neighbours [src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx:3141] — deferred, pre-existing
- [x] [Review][Defer] TenantAggregateCommandAdmissionGate's public API changed shape: same-owner TryAcquire now returns false and Release silently no-ops on owner mismatch, with no [Obsolete] overload [src/Hexalith.Tenants.UI/State/TenantCommands/TenantAggregateCommandAdmissionGate.cs:26] — deferred, pre-existing
- [x] [Review][Defer] The audit-capability probe has no reconnect subscription, and every read refresh briefly flips Remove to MissingAuditProof [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:823] — deferred, pre-existing
- [x] [Review][Defer] messageId remains absent from six ITenantCommandGateway methods, leaving their duplicate-dispatch hazard open [src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs:32] — deferred, pre-existing
- [x] [Review][Defer] TenantQueryGateway dereferences Detail! with a null-forgiving operator inside the catch that exists to fail safe [src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs:2131] — deferred, pre-existing
- [x] [Review][Defer] HasSameTenantDetail newly compares ConfigurationManagement.TenantId, so a default-constructed value degrades instead of retaining [src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs:2166] — deferred, pre-existing
- [x] [Review][Defer] _commandInFlight is cleared on one lease-refusal path and left stale on the other in the same method [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:1681] — deferred, pre-existing
- [x] [Review][Defer] A TenantId change does not notify non-keyed command surfaces that their lease was revoked [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:406] — deferred, pre-existing
- [x] [Review][Defer] The GA aggregation loop uses ContainsKey+Add where TryAdd suffices, and silently drops duplicate or null UserId rows [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:1435] — deferred, pre-existing
- [x] [Review][Defer] Add and change-role retryMessageId exclude the Rejected state, so a rejected attempt re-dispatches under a fresh ULID [src/Hexalith.Tenants.UI/Components/Tenants/Members/AddTenantMemberFlow.razor:441] — deferred, pre-existing
- [x] [Review][Defer] MemberAccessReview sets child lease ownership after the await, so two concurrent callers can both be granted [src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor:754] — deferred, pre-existing
- [x] [Review][Defer] CreateTenantFlow and TenantsWorkspace findings (fail-open absence baseline, empty-string tracking ids, confirmed-to-UnableToVerify downgrade, stale-empty-list absence proof) were raised against files a peer session rewrote mid-review [src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor:435] — deferred, pre-existing
- [x] [Review][Defer] TenantsWorkspace resolves ITenantsBffComposition per render with the untyped overload and duplicates its own absence predicate [src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:418] — deferred, pre-existing
- [x] [Review][Defer] An eighth undeclared references/ pointer move (Hexalith.EventStore c890235 -> f8b514f) appeared in the working tree during this review, extending the open chunk-A gitlink decision [_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md:1] — deferred, pre-existing

#### Loop 3 (2026-08-21) — final adversarial verification

- [x] [Review][Patch] Reject persisted older/equal tenant-sequence replay before tenant state or `ProjectedAt` mutation, while preserving every event within one accepted incoming batch and its optimistic-concurrency retry [src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs:81]
- [x] [Review][Patch] Accept the one-way legacy ETag to valid `tenant-sequence:<n>` upgrade only with exact command-event evidence; keep malformed, missing, regressing, and sequence-to-opaque transitions fail-closed [src/Hexalith.Tenants.UI/State/TenantCommands/TenantMembershipCommandProvenance.cs:34]
- [x] [Review][Patch] Render both wired Ready-receipt actions so Inspect audit no longer makes dismissal unreachable [src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceReceipt.razor:189]
- [x] [Review][Patch] Reserve the membership child lease before awaiting parent admission and roll it back on refusal or exception [src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor:755]
- [x] [Review][Patch] Key change-role/removal flows by target identity and reject queued sibling switches while a child owns or is acquiring the lease [src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor:240]
- [x] [Review][Patch] Make Continue read-only release activity and close the removal dialog rather than immediately reconstructing the preview [src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor:1147]
- [x] [Review][Patch] Initialize pre-command removal audit evidence as NotStarted instead of falsely reporting MissingSupport [src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs:834]
- [x] [Review][Patch] Put initial dialog focus on visible Cancel and bind the narrow-layout source check to the Razor form class as well as its CSS rule [src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor:535]
- [x] [Review][Patch] Parse and assert every audit-inspection restoration query field independently [tests/Hexalith.Tenants.UI.Tests/Components/RemoveTenantMemberFlowTests.cs:691]
- [x] [Review][Patch] Disable member row launchers when the command surface is unavailable even when its optional reason string is blank [src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor:657]
- [x] [Review][Defer] The reviewed TEA hook assets have no executable in-repository scanner/CLI-mode verification [.agent/skills/bmad-testarch-framework/resources/hooks/tea-enforce.cjs:575] — deferred, cross-cutting tooling

**Acceptance Criteria:**
- Given remove eligibility is calculated, when freshness, auth, preview completeness, or layout safety is indeterminate, then the action fails closed with localized reason and named recovery.
- Given an eligible removal opens, when the preview renders, then all ten required items plus last-owner/target-GA risk appear in a focus-trapped destructive dialog; cancel/Escape never dispatch and focus returns to the launcher.
- Given the user confirms a current complete preview, when submit runs, then `RemoveUserFromTenant` is dispatched once with retained messageId under AggregateIdentity lock, using Story 2.1 confirmation rules without optimistic removal.
- Given EN/FR resources and focused tests run, when verification completes, then elevated-risk, fail-closed, dialog, lock, and dispatch scenarios pass without asserting WP-2A/`audit_available` complete.


## File List

_Declared scope expansion (2026-08-21, loop 3 chunk B decision)._

Files this story's committed range changed that are **outside** the Code Map above. Declared here rather
than split out, because splitting would require reopening `1-6-read-only-tenant-configuration` and
`3-1-create-tenant-with-projection-confirmation` (both `done`) and would collide with a peer session
actively rewriting `CreateTenantFlow.razor`. Each entry states why the change belongs to this range.

- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs` (+ `UnavailableTenantCommandGateway.cs`)
  — `CreateTenantAsync`/`UpdateTenantAsync` gained `string? messageId` so a retry of one logical attempt keeps
  its tracking identity. Same affordance the membership commands needed for WP-2A retry causality; the
  create/metadata call sites belong to stories 3.1/3.2.
- `src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor`,
  `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor` — create-flow lifecycle and absence-baseline
  work owned by story `3-1-create-tenant-with-projection-confirmation`.
- `src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor` — metadata command
  lifecycle owned by story `3-2-edit-tenant-metadata-with-recorded-updates`.
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/AddTenantMemberFlow.razor`,
  `src/Hexalith.Tenants.UI/Components/Tenants/Members/ChangeTenantMemberRoleFlow.razor` — refresh coalescing,
  message-id retention and the shared aggregate-lease contract the remove flow depends on.
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs` — tenant-detail 304 handling reworked so a
  conditional hit re-reads unconditionally, and `ContinueReadOnlyComposition` removed so a reauthorization
  failure reports configuration unavailable instead of retaining approved rows. Changes Story 1.6 read
  behaviour; both directions are covered by tests but neither was spec'd here.
- `src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationPrincipalResolver.cs` — the loop-2 change
  here was REVERSED by the loop-3 chunk B decision above; the file now matches the 2026-08-01 owner decision.
- `src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor` (+ `.css`) — configuration read
  surface: filter comparison made ordinal/case-sensitive (deliberate, tested), and the scoped-CSS regression
  introduced by the `div` → `FluentStack` conversion fixed via `::deep`.
- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor.css`,
  `src/Hexalith.Tenants.UI/Services/Gateways/UnavailableTenantQueryGateway.cs`,
  `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditAvailability.cs`,
  `src/Hexalith.Tenants.UI/State/TenantCommands/TenantAggregateCommandAdmissionGate.cs`,
  `src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj` — supporting changes for the audit-availability and
  aggregate-lease work in this range.
- `src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs` — persists the aggregate-local ordered
  projection version required by membership confirmation instead of depending on a store-specific ETag.
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantProjectionHandlerTests.cs`,
  `tests/Hexalith.Tenants.Server.Tests/Queries/TenantQueryFreshnessTests.cs`,
  `tests/Hexalith.Tenants.UI.Tests/State/TenantMembershipCommandProvenanceTests.cs` — pin sequence stamping,
  replay non-regression, query metadata precedence, legacy ETag boundaries, and the 9-to-10 transition.
- `references/Hexalith.EventStore`
  — story commit `d3f74f58` advanced this root dependency pointer while landing the membership
  command-provenance and lifecycle hardening. The pointer change is declared as committed story-range
  ownership; this follow-up does not edit EventStore submodule content.

**Still not declared here:** six later `references/` pointer moves differ from the baseline (`AI.Tools`,
`Builds`, `Commons`, `FrontComposer`, `Memories`, and `PolymorphicSerializations`). They are post-story
dependency drift and remain outside this story rather than being falsely attributed to it.

## Spec Change Log

- 2026-08-08: Scope split — regenerated for 2.4a; deferred 2.4b WP-2A proof/reconciliation to `deferred-work.md`.
- 2026-08-08: 2.4a implemented — dialog, ten-item preview with platform-standing, live GA wiring; adversarial review patches applied.
- 2026-08-20: Review patches implemented — live proof capability, paged/current proof and GA evidence, real recovery actions, non-blocking supplementary reads, corrected EN/FR copy, and focused regression coverage.
- 2026-08-21: Review loop 3 (chunk A) — 12 patches applied: confirmed-outcome retention across status polls, non-destructive receipt dismissal, real preview completeness, identity-preserving lease refusal, fail-closed proof capability, announced in-flight dismissal, no target-absent alert on success, localized gateway tracking failure, focus trap kept out of the hidden narrow form, bounded proof re-walk, whitespace tenant-id guards, shared proof ordering. Dispose lease release attempted and reverted (governance invariant). 4 decisions open.
- 2026-08-21: Review loop 3 (chunk B, surfaces/resources/shared services) — 3 decisions resolved by the owner (restore both principal-resolver guards per the 2026-08-01 decision; derive audit recovery copy from rendered verbs AND bind continue-read-only where it exists; declare the scope expansion in a File List) and 20 patch findings closed: 17 applied, 3 recorded NO CHANGE NEEDED after their premise failed verification. Headline fixes: scoped-CSS regression that voided the configuration card's overflow/focus/responsive rules, a refused aggregate lease that still dispatched for four command surfaces, and a configuration landmark sharing one lease token across two flows (AD-12). 19 items deferred. UI tests 2128/2128.
- 2026-08-20: Review follow-up implemented — authoritative live audit capability, fail-closed bounded/cancellable GA and proof walks, retained-evidence degradation, lossless refresh coalescing, delegate-accurate recovery actions/copy, and route-generation regressions.
- 2026-08-21: Review loop 3 (chunk A decisions) — all four resolved: persisted aggregate-sequence projection versions, removal receipts no longer overclaim command causation, Story 3.2 owns and wires its metadata audit arm, and the story-owned EventStore pointer is declared while later dependency drift stays external.
- 2026-08-21: Review loop 3 final adversarial verification — 10 product patches applied with focused regressions; broad server verification then exposed and closed a same-sequence batch replay regression missed by the narrow filter. One unrelated TEA-hook verification gap was appended to deferred work. Final suites: server 746/746, UI 2188/2188; Release solution build clean.

## Design Notes

Platform-standing is preview item #9; known GA also raises an elevated sibling risk banner. Incomplete GA evidence stays Unknown (never invents NotReflected). Destructive confirmation uses the existing Tenants `role="dialog"` + focus-sentinel pattern; Cancel/Refresh/Continue-read-only stay outside the CSS-hidden narrow form. Honest audit handoff (no WP-2A / `audit_available`) until 2.4b.

Projection confirmation now depends on an ordered aggregate-sequence marker rather than the state-store ETag: `TenantProjectionHandler` stamps `TenantReadModel.ProjectionVersion` as `tenant-sequence:<n>`, where `<n>` is the aggregate-local, monotonically increasing EventStore `SequenceNumber` (shared format constant: `TenantProjectionVersionFormat.SequencePrefix` in `Hexalith.Tenants.Contracts`). A persisted sequence rejects an older-or-equal replay before any state or `ProjectedAt` mutation, while every event within one accepted incoming batch (including same-sequence retries) still applies. `TenantMembershipCommandProvenance` treats a missing or non-`tenant-sequence:` marker as a legacy/opaque token and falls back to plain inequality; it accepts a one-way legacy-to-sequence upgrade only alongside exact command-event evidence, and fails closed on malformed, regressing, or sequence-to-opaque transitions.

The metadata audit-proof read (`TenantDetailPage.GetUpdateMetadataAuditEvidenceAsync`) walks paginated `GetTenantAuditAsync` results up to `MetadataAuditProofMaximumPageCount` (50) instead of reading a single page. It tracks the `ProjectionVersion` seen on the first page and requires every later page to report the same value, failing closed on drift; more than one qualifying row across the full walk (not just one page) is treated as an ambiguous match and also fails closed. Exhausting all 50 pages without a single, version-consistent match fails closed rather than confirming.

## Verification

**Commands:**
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --no-restore --filter "FullyQualifiedName~RemoveTenantMember|FullyQualifiedName~TenantRemoveMember|FullyQualifiedName~TenantDetailSurfaceTests"` -- 226 passed, 0 failed, 0 skipped
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName~RemoveTenantMember|FullyQualifiedName~TenantRemoveMember|FullyQualifiedName~TenantDetailSurfaceTests|FullyQualifiedName~AuditEvidenceReceiptTests|FullyQualifiedName~AuditAvailabilityStateTests"` -- 253 passed, 0 failed, 0 skipped
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --no-restore` -- 2077 passed, 0 failed, 0 skipped
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --no-restore` -- passed with 0 warnings and 0 errors
- Focused test result: 253 passed, 0 failed, 0 skipped
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --no-build --no-restore` (loop 3, after patches) -- 2102 passed, 0 failed, 0 skipped
- `dotnet build src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj -c Release --no-restore` (loop 3) -- passed with 0 warnings and 0 errors
- `dotnet build Hexalith.Tenants.slnx -c Release --no-restore` (loop 3) -- FAILS in `references/Hexalith.Memories` (CS0618/SER301). Reproduced with this story's work stashed, so pre-existing submodule breakage, not caused by these patches.
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj` (loop 3 chunk B, after patches) -- 2128 passed, 0 failed, 0 skipped (2121 baseline + 7 new: 4 coverage tests, 3 audit-availability tests). Confirmed stable across repeated runs.
- `dotnet build src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj -c Release` (loop 3 chunk B) -- passed with 0 warnings and 0 errors
- Mutation-verified the three new behavioural tests: removing `MemberAccessReview.DisposeAsync`'s release, bypassing the change-role refresh coalescing, and restoring the escalate-only recovery-copy rule each turn the corresponding test red. The change-role test was strengthened after its first form let the mutant survive -- coalescing and non-coalescing issue the same number of status lookups, so the assertion had to move from call count to maximum observed concurrency.
- BLOCKER (external, not caused by these patches): `dotnet test` cannot restore at the working-tree `references/Hexalith.Builds` pointer `744b282` ("build: merge origin/dependabot/nuget/Props/xunit.v3-4.0.0"), which sets `xunit.v3` to 4.0.0 while leaving `xunit.v3.assert` and `xunit.v3.extensibility.core` at 3.2.2 -- `error NU1107: Version conflict detected for xunit.v3.common`. The suite above was run with the submodule temporarily checked out at the HEAD-committed `eadddc7`, where the pins are coherent at 3.2.2; the submodule was then restored to `744b282` exactly. Either advance the sibling xunit pins in Builds or revert that gitlink before CI can pass.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md` (loop 3 before chunk-A decision resolution) -- failed with the story-owned EventStore pointer undeclared plus later dependency drift.
- `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --no-restore --filter "FullyQualifiedName~TenantProjectionHandlerTests|FullyQualifiedName~TenantQueryFreshnessTests"` (chunk A decisions) -- 41 passed, 0 failed, 0 skipped
- `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --no-restore` (chunk A decisions) -- 742 passed, 0 failed, 0 skipped
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --no-restore --filter "FullyQualifiedName~RemoveTenantMemberFlowTests|FullyQualifiedName~TenantMembershipCommandProvenanceTests"` (chunk A decisions) -- 56 passed, 0 failed, 0 skipped
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --no-restore --filter "FullyQualifiedName~RemoveTenantMember|FullyQualifiedName~TenantRemoveMember|FullyQualifiedName~TenantDetailSurfaceTests"` (chunk A decisions / matrix audit) -- 249 passed, 0 failed, 0 skipped
- `dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Release --no-restore` and `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-restore` (chunk A decisions) -- both passed with 0 warnings and 0 errors
- `dotnet build src/Hexalith.Tenants/Hexalith.Tenants.csproj --configuration Release --no-restore` and `dotnet build src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj --configuration Release --no-restore` (chunk A decisions) -- both passed with 0 warnings and 0 errors
- BROAD UI BLOCKER (unrelated to Story 2.4 files): `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --no-restore` -- 2167 passed, 1 failed; `GlobalAdministratorsPageTests.Grant_requery_does_not_confirm_from_a_superseded_snapshot` confirms from a superseded snapshot and fails identically in isolation.
- SOLUTION BLOCKER (pre-existing submodule): `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore` -- fails in `references/Hexalith.Memories` with three CS0618 errors and one SER301 error, followed by solution-level MSB4181; owned Tenants projects build successfully.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md` (chunk A decisions) -- EventStore is now declared; still fails for six post-story pointers (`AI.Tools`, `Builds`, `Commons`, `FrontComposer`, `Memories`, `PolymorphicSerializations`) intentionally not attributed to this story.
- `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --no-restore --filter "FullyQualifiedName~TenantProjectionHandlerTests|FullyQualifiedName~TenantQueryFreshnessTests|FullyQualifiedName~ProjectionWriteConformanceTests"` (final adversarial verification) -- 57 passed, 0 failed, 0 skipped
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --no-restore --filter "FullyQualifiedName~RemoveTenantMemberFlowTests|FullyQualifiedName~TenantRemoveMemberCommandSnapshotTests|FullyQualifiedName~TenantMembershipCommandProvenanceTests|FullyQualifiedName~AuditEvidenceReceiptTests|FullyQualifiedName~TenantDetailSurfaceTests"` (final adversarial verification) -- 300 passed, 0 failed, 0 skipped
- `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --no-restore` (final adversarial verification) -- 746 passed, 0 failed, 0 skipped
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --no-restore` (final adversarial verification) -- 2188 passed, 0 failed, 0 skipped
- Release builds for `src/Hexalith.Tenants`, `src/Hexalith.Tenants.UI`, both owned test projects, and `Hexalith.Tenants.slnx`, all with `--no-restore` (final adversarial verification) -- passed with 0 warnings and 0 errors. The solution-level Memories blocker recorded above was removed by a concurrent, unrelated solution-filter edit and is not attributed to Story 2.4.
- `git diff --check` (final adversarial verification) -- passed
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md` (final adversarial verification) -- expected fail remains limited to the six undeclared post-story pointers listed above; EventStore remains declared

## Suggested Review Order

**Removal command and proof lifecycle**

- Entry: retain command identity, acquire admission, then reconcile authoritative proof.
  [`RemoveTenantMemberFlow.razor:670`](../../src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor#L670)

- Preserve non-collapse lifecycle while requiring causal projection advancement.
  [`TenantCreateCommandModels.cs:979`](../../src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs#L979)

- Bound audit paging and promote only current projection-backed removal evidence.
  [`RemoveTenantMemberFlow.razor:941`](../../src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor#L941)

**Projection causality**

- Skip persisted replay without discarding accepted same-sequence batch events.
  [`TenantProjectionHandler.cs:81`](../../src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs#L81)

- Permit one-way legacy upgrades only alongside exact command-event evidence.
  [`TenantMembershipCommandProvenance.cs:34`](../../src/Hexalith.Tenants.UI/State/TenantCommands/TenantMembershipCommandProvenance.cs#L34)

**Admission and dialog safety**

- Reserve child ownership before awaiting the aggregate admission boundary.
  [`MemberAccessReview.razor:756`](../../src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor#L756)

- Key destructive flows by target so retargeting cannot retain stale intent.
  [`MemberAccessReview.razor:243`](../../src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor#L243)

- Focus visible recovery first and close after continuing read-only.
  [`RemoveTenantMemberFlow.razor:1148`](../../src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor#L1148)

**Eligibility and recovery evidence**

- Resolve complete GA and audit evidence without blocking primary detail.
  [`TenantDetailPage.razor:1406`](../../src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor#L1406)

- Render every wired Ready-receipt action without inventing unavailable recovery.
  [`AuditEvidenceReceipt.razor:189`](../../src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceReceipt.razor#L189)

**Verification**

- Pin same-sequence batches and older/equal persisted replay behavior.
  [`TenantProjectionHandlerTests.cs:226`](../../tests/Hexalith.Tenants.Server.Tests/Projections/TenantProjectionHandlerTests.cs#L226)

- Verify receipt navigation, responsive markup, and read-only exit behavior.
  [`RemoveTenantMemberFlowTests.cs:170`](../../tests/Hexalith.Tenants.UI.Tests/Components/RemoveTenantMemberFlowTests.cs#L170)

- Exercise retargeting, delayed admission, and blank-reason fail-closed launchers.
  [`TenantDetailSurfaceTests.cs:3377`](../../tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs#L3377)
