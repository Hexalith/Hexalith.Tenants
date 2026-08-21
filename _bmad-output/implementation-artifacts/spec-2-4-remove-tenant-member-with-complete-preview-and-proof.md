---
title: 'Remove Tenant Member with Complete Preview and Proof'
type: 'feature'
created: '2026-08-08'
status: 'in-progress'
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

- [ ] [Review][Decision] Ordered projection-version confirmation silently depends on the state-store ETag format — `TenantReadModel.ProjectionVersion` is never assigned (`src/Hexalith.Tenants.Server/Projections/TenantReadModel.cs:16`), so `TenantQueryResult.cs:53` falls back to the raw state-store ETag. `TrySplitOrderedVersion` then requires a stable prefix plus a monotone trailing digit run: zero-padded ETags break deterministically at every power-of-ten boundary (`…0009` → prefix `…000` vs `…0010` → prefix `…001`), and GUID/hash ETags never confirm at all. Fails closed, so no false success — but combined with the non-dismissible dialog below the operator is stranded. Choose: assign a real ordered `ProjectionVersion` in the projection handler, document a store ETag contract, or give remove the audit-provenance confirm arm 2.4b actually specified. [src/Hexalith.Tenants.UI/State/TenantCommands/TenantMembershipCommandProvenance.cs:41]
- [ ] [Review][Decision] WP-2A proof cannot be tied to this specific command — the match predicate is (EventType, TenantId, Target, Timestamp >= attemptStart) with no upper bound, and `TenantAuditRow` carries no message/correlation id, so another operator's removal of the same user in the same window qualifies and is then stamped with this attempt's `MessageId` as the support reference. The 2.4b Always clause only demands "tenant + target + causal lower bound", so the code matches the spec letter; the question is whether that bar is sufficient for a support artifact. Choose: add an upper time bound, extend the audit row contract with a command reference, or weaken the receipt's claim. [src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs:1044]
- [ ] [Review][Decision] 2.4b's audit-provenance confirm arm landed on metadata instead of remove, and the metadata parameter is dead — `TenantUpdateMetadataCommandSnapshot.ConfirmProjection` gained `hasQualifyingAuditProvenance` but its only caller (`EditTenantMetadataFlow.razor:410,573`) never supplies it, while `TenantRemoveMemberCommandSnapshot.ConfirmProjection` has no such arm at all. 2.4b "Ask First" explicitly covers extending audit-provenance confirmation beyond remove-member. Choose: drop the metadata parameter, wire it, or move the arm to remove. [src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs:1205]
- [ ] [Review][Decision] Story commit `d3f74f58` moved `references/Hexalith.EventStore` undeclared, and this spec's existing deferred gitlink entry misattributes all seven pointer moves to "post-story dependency bumps" — `python3 scripts/validate-story-gitlinks.py` exits 1 for both 2.4 specs. At least one bump is this story's own commit, not external drift. Choose: DECLARE the EventStore pointer as a File List entry with a reason, or REVERT it and land it separately as `build(deps)`. [_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md:1]

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

- [ ] [Review][Patch] Converting four config layout wrappers from `div` to `FluentStack` voids every scoped CSS rule that styles them, losing horizontal scroll, the focus ring and both responsive grids [src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor:14]
- [ ] [Review][Patch] A refused aggregate lease still dispatches for the metadata, lifecycle, set-configuration and remove-configuration flows [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:1662]
- [ ] [Review][Patch] The configuration landmark shares one lease-owner token across its set and remove flows, re-creating the AD-12 multi-owner early-release the new lease code documents as fixed [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:230]
- [ ] [Review][Patch] EditTenantMetadataFlow reuses a consumed messageId for a different payload; the sibling flows added Intent/CorrelationId/Failed guards in the same commit [src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor:480]
- [ ] [Review][Patch] Degraded global-administrator evidence still yields a positive "also a global administrator" preview claim; only the negative direction checks IsCompleteEvidence [src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor:859]
- [ ] [Review][Patch] ChangeTenantMemberRoleFlow is non-exitable while a command is in flight — Cancel disabled and CloseAsync silently no-ops, with no announcement [src/Hexalith.Tenants.UI/Components/Tenants/Members/ChangeTenantMemberRoleFlow.razor:666]
- [ ] [Review][Patch] The Wait recovery verb renders as a live enabled button wired to an empty switch arm [src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditAvailabilityState.razor:111]
- [ ] [Review][Patch] AggregateLocked puts a two-sentence instruction into the terse always-visible reason-catalog legend [src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor:315]
- [ ] [Review][Patch] CommandFlowGuardConformanceTests matches only the retired OnCommandActivityChanged release path, so it now passes vacuously for the live CommandActivityLease mechanism [tests/Hexalith.Tenants.UI.Tests/CommandFlowGuardConformanceTests.cs:8]
- [ ] [Review][Patch] AuditAvailabilityState renders an empty labelled actions region when every verb is filtered out; AuditEvidenceReceipt guards the same case [src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditAvailabilityState.razor:21]
- [ ] [Review][Patch] A Ready receipt with no inspect delegate falls through to offering Refresh, contradicting the state it reports [src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceReceipt.razor:192]
- [ ] [Review][Patch] The lease acquire/release path and MemberAccessReview.DisposeAsync can throw ObjectDisposedException on teardown; sibling dispatches are guarded [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:1738]
- [ ] [Review][Patch] The global-administrator page walk is bounded by the unrelated CursorHistory.DefaultMaximum, truncates indistinguishably from a gateway failure, and logs nothing [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:249]
- [ ] [Review][Patch] StateGlyph returns the untranslated English literal "OK" for the Available state where every sibling returns punctuation [src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditAvailabilityState.razor:100]
- [ ] [Review][Patch] A missing admission-gate registration disables every command surface with a null reason string [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:322]
- [ ] [Review][Patch] UnavailableTenantQueryGateway throws synchronously from Task-returning members instead of returning a faulted task [src/Hexalith.Tenants.UI/Services/Gateways/UnavailableTenantQueryGateway.cs:15]
- [ ] [Review][Patch] No test covers change-role nudge coalescing or the projection-refresh re-entrancy guard; the add-flow equivalent is pinned with an exact call count [tests/Hexalith.Tenants.UI.Tests/Components/ChangeTenantMemberRoleFlowTests.cs:1]
- [ ] [Review][Patch] No test covers the new MemberAccessReview.DisposeAsync lease release; deleting it orphans the aggregate lock for the life of the circuit [tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs:1235]
- [ ] [Review][Patch] No assertion distinguishes the .NoEscalation copy variants, so recovery copy can name absent controls undetected [tests/Hexalith.Tenants.UI.Tests/Components/AuditAvailabilityStateTests.cs:24]
- [ ] [Review][Patch] The AuditAvailable state is absent from the glyph theory, so a blank success glyph would ship green [tests/Hexalith.Tenants.UI.Tests/Components/AuditAvailabilityStateTests.cs:19]

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

**Acceptance Criteria:**
- Given remove eligibility is calculated, when freshness, auth, preview completeness, or layout safety is indeterminate, then the action fails closed with localized reason and named recovery.
- Given an eligible removal opens, when the preview renders, then all ten required items plus last-owner/target-GA risk appear in a focus-trapped destructive dialog; cancel/Escape never dispatch and focus returns to the launcher.
- Given the user confirms a current complete preview, when submit runs, then `RemoveUserFromTenant` is dispatched once with retained messageId under AggregateIdentity lock, using Story 2.1 confirmation rules without optimistic removal.
- Given EN/FR resources and focused tests run, when verification completes, then elevated-risk, fail-closed, dialog, lock, and dispatch scenarios pass without asserting WP-2A/`audit_available` complete.

## Spec Change Log

- 2026-08-08: Scope split — regenerated for 2.4a; deferred 2.4b WP-2A proof/reconciliation to `deferred-work.md`.
- 2026-08-08: 2.4a implemented — dialog, ten-item preview with platform-standing, live GA wiring; adversarial review patches applied.
- 2026-08-20: Review patches implemented — live proof capability, paged/current proof and GA evidence, real recovery actions, non-blocking supplementary reads, corrected EN/FR copy, and focused regression coverage.
- 2026-08-21: Review loop 3 (chunk A) — 12 patches applied: confirmed-outcome retention across status polls, non-destructive receipt dismissal, real preview completeness, identity-preserving lease refusal, fail-closed proof capability, announced in-flight dismissal, no target-absent alert on success, localized gateway tracking failure, focus trap kept out of the hidden narrow form, bounded proof re-walk, whitespace tenant-id guards, shared proof ordering. Dispose lease release attempted and reverted (governance invariant). 4 decisions open.
- 2026-08-20: Review follow-up implemented — authoritative live audit capability, fail-closed bounded/cancellable GA and proof walks, retained-evidence degradation, lossless refresh coalescing, delegate-accurate recovery actions/copy, and route-generation regressions.

## Design Notes

Platform-standing is preview item #9; known GA also raises an elevated sibling risk banner. Incomplete GA evidence stays Unknown (never invents NotReflected). Destructive confirmation uses the existing Tenants `role="dialog"` + focus-sentinel pattern; Cancel/Refresh/Continue-read-only stay outside the CSS-hidden narrow form. Honest audit handoff (no WP-2A / `audit_available`) until 2.4b.

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
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md` -- fails only on the seven pre-existing post-baseline dependency bumps recorded in the deferred review finding above

## Suggested Review Order

**Removal proof lifecycle**

- Entry: coalesce status work without losing authoritative projection-refresh intent.
  [`RemoveTenantMemberFlow.razor:786`](../../src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor#L786)

- Bound and cancel audit paging while continuing from weak to strong evidence.
  [`RemoveTenantMemberFlow.razor:896`](../../src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor#L896)

- Preserve retry causality and require current, receipt-ready proof before promotion.
  [`TenantCreateCommandModels.cs:988`](../../src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs#L988)

- Route receipt inspection with support-safe command context.
  [`RemoveTenantMemberFlow.razor:1098`](../../src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor#L1098)

**Authoritative eligibility evidence**

- Launch supplementary GA and audit reads without blocking primary tenant detail.
  [`TenantDetailPage.razor:453`](../../src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor#L453)

- Aggregate GA pages with cancellation, bounds, and projection-version consistency.
  [`TenantDetailPage.razor:1406`](../../src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor#L1406)

- Prove tenant-scoped audit capability only from current authoritative responses.
  [`TenantDetailPage.razor:1529`](../../src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor#L1529)

- Feed proven capability into fail-closed member action slots.
  [`MemberAccessReview.razor:611`](../../src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor#L611)

**Honest recovery actions**

- Render only recovery verbs backed by real delegates.
  [`AuditAvailabilityState.razor:108`](../../src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditAvailabilityState.razor#L108)

- Forward inspection distinctly and hide inoperative receipt actions.
  [`AuditEvidenceReceipt.razor:189`](../../src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceReceipt.razor#L189)

- Localize no-escalation recovery variants in English and French.
  [`TenantsResources.resx:3138`](../../src/Hexalith.Tenants.UI/Resources/TenantsResources.resx#L3138)

**Verification**

- Exercise bounded proof walks, cancellation, coalescing, and callback semantics.
  [`RemoveTenantMemberFlowTests.cs:784`](../../tests/Hexalith.Tenants.UI.Tests/Components/RemoveTenantMemberFlowTests.cs#L784)

- Exercise incomplete GA evidence, page caps, refresh faults, and route races.
  [`TenantDetailSurfaceTests.cs:295`](../../tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs#L295)
