---
title: 'Correct global administrator authority from audit evidence'
type: 'feature'
created: '2026-10-10'
status: 'done'
route: 'dispatch'
review_loop_iteration: 3
baseline_commit: '66072cc410834e2c34f7d30f121e447940ea6fc6'
context:
  - _bmad-output/project-context.md
  - _bmad-output/implementation-artifacts/epic-5-context.md
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Revised Story 5.7 remains backlog: the audit page blocks correction, while its dormant panel can link unrelated audit events.

**Approach:** Connect authorized system evidence to the fixed-scope panel; complete preview, command and proof checks.

## Boundaries & Constraints

**Always:** Map `GlobalAdministratorRemoved` to `SetGlobalAdministrator` and `GlobalAdministratorSet` to `RemoveGlobalAdministrator`, in fixed `system` / `global-administrators` / `global-administrators` scope. Require current complete paging, authorized evidence, safe width and ten preview facts. Recheck state, count, authority and admission before dispatch. Retain one ULID attempt/lease across sibling actions, retry and reconnect within the same Blazor circuit. A hard browser reload or new circuit has no attempt recovery in this story and must be disclosed as such. Confirm only a fresh complete postcondition reaching verified command progress beyond the baseline. Keep command, projection and audit states distinct; preserve original evidence, last-confirmed projection, last-admin safety, focus and EN/FR safe copy. Where one audit receipt belongs to successive corrections, show every independently verified paired link so neither relationship is lost.

**Never:** Use tenant roles as authority; dispatch already-applied work; convert rejection to success; link by target/type/time; mutate history; render raw tracking data; add endpoints or dependencies.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Restore/remove | Complete system evidence | Ten facts; one command; causal confirmation | Audit delay separate |
| Already applied/unsafe | Intended state, uncertain/one-admin count | No dispatch; self-removal warns if safe | Recover or block |
| Concurrent/ambiguous | Rejection, duplicate, refresh, partial page | Retain attempt/lease/evidence | Typed recovery |
| Unproven audit row | Similar later event | No paired link | Honest availability |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor` — closed gate; dormant panel links coincidental proof.
- `src/Hexalith.Tenants.UI/Services/Gateways/GlobalAdministratorsProjectionLoader.cs` — complete paging; command gateway omits global sequence.
- `src/Hexalith.Tenants.Server/Projections/TenantAuditReadModel.cs` — audit lacks command provenance.
- `src/Hexalith.Tenants/Projections/GlobalAdministratorProjectionHandler.cs` — full-history rebuild must emit an ordered sequence version. Existing persisted models have no such version and need operator replay before this correction is available.
- `tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh` — existing browser lane uses actual scoped CSS but lacks rendered global correction facts and paired receipt controls.
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorReconciliationState.cs` and `src/Hexalith.Tenants.UI/State/TenantCommands/TenantAggregateCommandAdmissionGate.cs` — reconciliation currently lacks its source audit identity, so action/target adoption is ambiguous.
- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor` and `src/Hexalith.Tenants.UI/State/TenantAudit/GlobalAdministratorCorrectionProofRegistry.cs` — sibling completion currently releases the correction attempt before its original evidence can be paired.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor` — enable proven system rows; refresh complete projection; mount panel with one provider, protected refresh/reconnect attempt recovery, receipt and focus.
- [x] `src/Hexalith.Tenants.UI/State/TenantAudit/GlobalAdministratorCorrectionSnapshot.cs` — model already-applied, self-removal, causal confirmation and fail-closed proof.
- [x] `src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor` — show ten items: original reference/time, scope, target, presence, complete/resulting count, provenance, authority/admission, recovery, audit expectation, consequences/unknowns; recheck once; preserve focus and safe width.
- [x] `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs` and `src/Hexalith.Tenants.UI/State/TenantCommands/TenantAggregateCommandAdmissionGate.cs` — validate global sequence; retain one attempt across sibling actions/reconnect.
- [x] `src/Hexalith.Tenants.Server/Projections/TenantAuditReadModel.cs`, `src/Hexalith.Tenants.Contracts/Queries/TenantAuditEntry.cs`, `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs` — carry minimal authorized provenance; match attempt/scope/target/sequence/time across protected pages; emit safe paired references or missing support.
- [x] `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` and `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx` — add whole-string platform copy with parity.
- [x] `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditDataGrid.razor` and `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceReceipt.razor` — expose eligible start and bidirectional safe receipt navigation.
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorCorrectionPanelTests.cs`, `tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs`, `tests/Hexalith.Tenants.UI.Tests/State/GlobalAdministratorCorrectionSnapshotTests.cs`, `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs`, `tests/Hexalith.Tenants.Server.Tests/Projections/TenantAuditReadModelTests.cs` — cover matrix, paging, reconnect, causal negatives, routing and accessibility.
- [x] `docs/event-contract-reference.md` — state the rollout gate: rebuild the existing singleton projection from durable event history after deploying the sequence-writing handler; keep correction unavailable until a complete read returns a valid ordered version. Do not synthesize a baseline from wall time or unrelated audit rows.
- [x] `tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh` and its fixture/test files — export real EN/FR global correction panel and paired receipt markup; check facts, controls, focus, narrow width and forced colors in Chromium.
- [x] `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorReconciliationState.cs`, `src/Hexalith.Tenants.UI/State/TenantCommands/TenantAggregateCommandAdmissionGate.cs`, and both global action surfaces — retain the source audit reference and UTC timestamp with the command identity across lease writers/adoption; a correction panel may adopt only its exact source, while the global-administrator page can adopt and carry that source without rewriting it.
- [x] `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor`, `src/Hexalith.Tenants.UI/State/TenantAudit/GlobalAdministratorCorrectionProofRegistry.cs`, and `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor` — retain a correction-origin completion in the circuit when the sibling page finishes it; on audit return, reauthorize and recheck current complete projection and both protected audit rows before publishing a paired link. Do not pair a different same-target audit row or discard an independently verified neighbor.
- [x] `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor` — subscribe to authentication-state changes; immediately invalidate cached off-page rows, paired links, global correction authorization and pending proof-walk generations, then reload/re-authorize before rendering protected evidence for the new principal; unsubscribe on disposal.
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorCorrectionPanelTests.cs`, `tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs`, `tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs`, and `tests/Hexalith.Tenants.UI.Tests/State/TenantAggregateCommandAdmissionGateTests.cs` — prove wrong-origin same-target adoption is rejected, sibling-page completion can produce only its original verified pair on audit return, and a principal change hides cached protected evidence before reauthorization.
- [x] `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor` and `tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs` — require Access category and ordered projection version on original-row rechecks, and prove a valid original on page two still yields the paired link.
- [x] `src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor` and its component tests — render neutral count/presence after an unavailable revoke projection even if the remove preview changes the message key; submission stays blocked.
- [x] `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/GlobalAdministratorAuditProofLookupTests.cs` — prove duplicate exact corrective rows split across pages return no proof and `Complete == false`.
- [x] Retain the complete projection that confirmed a correction before the sibling page or panel releases its lease; on audit return, require a fresh complete current projection and exact protected audit rows while preserving a previously confirmed pair after a later correction reverses the target state.
- [x] Retain committed sequence from stale sibling status callbacks and a confirmed panel completion whose audit proof is delayed; exercise both paths and rendered grant handoff.

**Acceptance Criteria:**
- Given complete authorized system evidence, when correction opens, then the fixed command and all ten facts render; stale, partial or unsupported evidence blocks with recovery.
- Given current authority, when Confirm runs, then already-applied and last-admin cases send nothing; an eligible action sends exactly one fixed-scope ULID attempt.
- Given rejection or uncertain delivery, when status/projection refresh, then distinct states retain the original evidence, projection and attempt without false success or duplicate dispatch.
- Given eventful completion, when one fresh complete projection reaches this command's verified sequence, then only the matching postcondition confirms.
- Given a possible corrective audit row, when provenance is checked, then only re-derivable causal association creates both safe receipt links; otherwise audit availability stays truthful.
- Given EN/FR, keyboard, mobile and forced-colors use, when rendered, then safety facts, selectors, focus and live regions work; narrow widths stay read-only.
- Given a persisted singleton projection without ordered sequence, when correction opens, then it stays unavailable until the existing projection is replayed from durable history and the new sequence version is observed.
- Given two system audit rows for the same target and action, when one correction is retained and another panel opens, then the second row cannot adopt or pair the first row's command.
- Given a correction-origin command completes after the global-administrator page adopts its lease, when the operator returns to audit in the same circuit, then the original source is retained and only a fresh, authorized, causal recheck may create the paired receipt link.
- Given the signed-in principal changes while an audit page is open, when authentication-state notification arrives, then cached off-page receipts and paired links disappear before any protected evidence is shown for the new principal.

### Review Findings

Code review 2026-10-11 of `66072cc4..362a957d`. The review diff was 4,351 lines (260,201 bytes; SHA-256 `80873fa0559d09b80c66f547286074324c610c366b9f7f069dcb05e37c6a000b`) and is not archived. Four layers ran: Blind Hunter, Edge Case Hunter, Verification Gap and Acceptance Auditor; none failed. Triage produced 1 decision-needed (resolved to a patch on 2026-10-11), 22 patch, 3 defer and 20 rejected. The gitlink guard passed: `validate-story-gitlinks.py` exited 0 with no `references/` pointer changes in range. Finding 1 was confirmed in a clean checkout pinned to `362a957d`:
- **Reproduction:** a copy of `Mounted_global_panel_notifies_page_when_a_new_proven_pair_becomes_available` makes the proof lookup complete off the dispatcher (`Task.Run`, as real HTTP does) and asserts `tenants-correction-proof-link`. It fails.
- **Fix check:** after the callback is wrapped in `InvokeAsync(() => OnProofAvailable.InvokeAsync())`, it passes. The original synchronous test passes both before and after.

- [x] [Review][Patch] (low) Stamp command provenance only on `system` global-administrator audit rows [src/Hexalith.Tenants.Server/Projections/TenantAuditReadModel.cs:136] — decision resolved 2026-10-11 (option a, restrict). `CreateEntry` currently sets `CommandCorrelationId` and `AggregateSequence` on every entry, so every `GET /api/tenants/{id}/audit` response and the published `TenantAuditEntry` contract carry command correlation IDs, although only `GlobalAdministratorAuditProofLookup` reads them. Set both fields only for `GlobalAdministratorSet`/`GlobalAdministratorRemoved` entries on tenant `system`; leave them null elsewhere. Update `docs/event-contract-reference.md` to state that scope, and add a server test that a tenant-scoped entry carries neither field. [blind+auditor]
- [x] [Review][Patch] (high) `OnProofAvailable` is invoked off the Blazor dispatcher [src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor:1746] — this also applies at `:1810`. Both calls `await OnProofAvailable.InvokeAsync().ConfigureAwait(false)` after the protected proof lookup's `ConfigureAwait(false)` continuation. The page's `HandleEventAsync` then calls `StateHasChanged()` off the dispatcher and throws. On Confirm, the throw is swallowed by `SubmitAsync`'s catch: the Confirmed snapshot and `RetainOrReleaseCompletion` never run, and a completed revoke becomes an ambiguous delivery. On Refresh, the exception escapes the click handler and ends the circuit. Fix: `await InvokeAsync(() => OnProofAvailable.InvokeAsync()).ConfigureAwait(false)`, as `OnClose` does at `:2222`. Add a page test whose proof lookup completes off the dispatcher. [blind+edge+auditor+verification]
- [x] [Review][Patch] (medium) A reload or page change unmounts an open global preview and does not restore focus [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1857] — `RefreshGlobalCorrectionEvidenceForLoadAsync` sets `_globalCorrectionIntent = null` whenever the loaded page has global rows but not the original row. On `/tenants/system/audit`, almost every page has global rows. Review loop 1 rejected a paging finding because "retaining the active panel across paging is required". Keep the existing intent when the row is absent; re-evaluate it only when the row is present. [edge+verification]
- [x] [Review][Patch] (medium) Every paired-receipt recheck makes an off-page receipt flash "Unavailable" [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1564] — `RefreshPairedGlobalReceiptAsync` clears `_verifiedGlobalProofs` and `_verifiedGlobalRows` and re-resolves the selection before it re-verifies. A paired receipt that is not on the loaded page therefore renders Unavailable for the duration of two or more protected walks, every time it is opened or navigated to, and the scheduled heading focus lands on the Unavailable state. Replace the verified sets atomically in the final dispatcher apply, so a failed recheck removes only the affected links. [blind]
- [x] [Review][Patch] (medium) Sibling-surface writers drop the hidden proof inputs, and missing inputs are then reported as "audit delayed" with a Refresh that never ends [src/Hexalith.Tenants.UI/State/TenantCommands/TenantAggregateCommandAdmissionGate.cs:462]
  - **Inputs dropped:** `GlobalAdministratorsPage.CreateReconciliation` builds the shared lease reconciliation with `new(...)`, which carries neither `SubmittedAtUtc` nor `CommittedEventSequence`. `TryAdvanceReconciliation` and `TryRetainReconciliation` (`:490`) store that record as is.
  - **Adoption:** the panel adopts any retained lease with the same action and target.
  - **Delayed state:** `QueryCorrectiveProofAsync` (`GlobalAdministratorCorrectionPanel.razor:2175`) returns `(null, false)`, which the panel maps to `AuditDelayed`. The Confirmed branch of `CanRefreshSnapshot` keeps Refresh enabled, but refreshing can never succeed.
  - **Fix part 1:** when the command identity is unchanged, carry forward the existing `SubmittedAtUtc` and `CommittedEventSequence`.
  - **Fix part 2:** report a confirmed attempt that lacks proof inputs as `MissingSupport`, not `AuditDelayed`. [auditor+edge]
- [x] [Review][Patch] (medium) Every parent re-render resets the revoke preview [src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor:719]
  - **Cause:** with an unchanged intent, `ShouldLiveUpdatePreview(Previewed)` rebuilds the snapshot through `FromIntent`, which sets `RemovePreview = null`. `OnParametersSetAsync` then composes the preview again over HTTP.
  - **Effect:** after any page re-render, Confirm is disabled until that call returns, and the self-removal warning briefly reverts to the generic last-admin copy.
  - **Fix:** keep the existing complete `RemovePreview` while the projection version is unchanged. [edge]
- [x] [Review][Patch] (medium) A projection that has reached the command's sequence but contradicts the postcondition stays pending forever [src/Hexalith.Tenants.UI/State/TenantAudit/GlobalAdministratorCorrectionSnapshot.cs:674]
  - **Cause:** once `HasReached(CommittedEventSequence, projection.ProjectionVersion)` and `Advanced` both hold, a failing `projectionProvesCorrection` check returns `ProjectionPending`. That state means a later change superseded the command.
  - **Effect:** Refresh can never resolve it, and the lease for the fixed aggregate stays held for the rest of the circuit, so it blocks every global-administrator action.
  - **Fix:** return `UnableToVerify`, reusing the existing per-action copy that the deleted revoke delegation used (still-present / evidence-required). [auditor+edge]
- [x] [Review][Patch] (low) The authority/admission fact can say "ready" next to a disabled Confirm [src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor:552] — `AuthorityAdmissionLabel` leaves out `(IsRestoreAccessAction || RemovePreview?.IsComplete == true)` and `!_isSubmitting`. `CreateCorrectionDecision` requires both. Review loop 1 recorded this as patched. Derive the label from `CreateCorrectionDecision()`. [blind+edge]
- [x] [Review][Patch] (low) The in-flight guard misses the `RequestSent` window [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1760] — `HasCommandTracking` requires `CorrelationId` or an ambiguous delivery. While the POST is in flight (`MessageId` set, no correlation yet), a second Start or a load refresh (`:1853`) can replace or unmount the panel, and the operator loses sight of the attempt. Guard on `Snapshot?.MessageId is not null`. Review loop 1 recorded "second intent hides active attempt" as patched. [edge]
- [x] [Review][Patch] (low) An off-page verified paired receipt offers a Start correction that does nothing [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:2038]
  - **Cause:** `ResolveReceiptSelection` builds a correction intent from a row taken from `_verifiedGlobalRows`, but `OpenGlobalCorrectionAsync` looks the row up only in `_snapshot.Rows`.
  - **Effect:** clicking Start performs two protected reads, then silently focuses a row that does not exist.
  - **Fix:** offer correction only for rows on the loaded page. [edge]
- [x] [Review][Patch] (low) Dead EN/FR resource keys [src/Hexalith.Tenants.UI/Resources/TenantsResources.resx:4290] — `Tenants.Audit.Receipt.Paired.Open` is unused. `Tenants.Correction.Start.GlobalNotReady` (`:4213`) is orphaned by this diff and survives only in three test-stub localizers. Delete both from EN/FR and the stubs. [blind+auditor+edge]
- [x] [Review][Patch] (low) The receipt test now asserts an echoed key instead of the localized copy [tests/Hexalith.Tenants.UI.Tests/Components/AuditEvidenceReceiptTests.cs:632] — `.ShouldContain("GlobalAdministratorCommandSupportUnavailable")` passes only because the stub localizer echoes missing keys, so it would not catch a raw key leaking into the UI. Assert the exact EN copy of `Tenants.Correction.Unavailable.GlobalAdministratorCommandSupportUnavailable`. [blind]
- [x] [Review][Patch] (low) Internal planning label in the public contract document [docs/event-contract-reference.md:127] — "For Story 5.7 …" puts a sprint artifact name into the published contract reference. Name the feature instead (audit-based global-administrator correction). [blind]
- [x] [Review][Patch] (medium) The tenant-switch part of the paired-receipt test cannot detect a cross-tenant cached receipt [tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs:1607] — the cache is already empty before the switch. Navigate to `tenant.alpha?receiptReference=event-corrective` while the pair is still verified, then assert an unavailable receipt and no paired control. [verification]
- [x] [Review][Patch] (medium) The route-driven paired refresh is never exercised [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:828] — this also covers `:1078-1081`. Add a page test that navigates a loaded system page to `?receiptReference=event-corrective`, with an off-page corrective row, and asserts a Ready receipt with its back-link. [verification]
- [x] [Review][Patch] (medium) The restore pre-dispatch recheck has no negative test [src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor:955] — no test drives `original?.Matches(preview)` to false. Add a panel test where the confirm-time read has moved on (count and version changed), and assert no Set request, `UnableToVerify` and a released lease. [verification]
- [x] [Review][Patch] (medium) The proof link is never asserted for adopted or replacement attempts [tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorCorrectionPanelTests.cs:2552] — this also covers `:1912-1965`. Extend both replacement tests: seed `SubmittedAtUtc`, then assert `AuditAvailable` and `tenants-correction-proof-link`. [verification]
- [x] [Review][Patch] (medium) No test serializes the provenance fields [tests/Hexalith.Tenants.Contracts.Tests/Queries/QueryDtoSerializationTests.cs:222] — a `[JsonIgnore]` or a non-public accessor would silently turn every proof into `MissingSupport`. Extend the round trip (server camelCase options, then Web defaults) with both fields set. [blind+verification]
- [x] [Review][Patch] (medium) The new global-authority and projection read during system-page load has no failure-path test [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1047] — render the system page with a global row plus `GlobalAdminFault` (and an authorization fault), then assert that the grid and receipt still render and correction is unavailable. [verification]
- [x] [Review][Patch] (medium) Three new preview facts are checked only for being present [tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorCorrectionPanelTests.cs:41] — assert the exact values: authority/admission Ready versus Blocked (already applied, last admin, narrow), provenance Current versus Unverified, and original time `2026-06-01 10:00:00 UTC`. [verification]
- [x] [Review][Patch] (medium) Protection of an in-flight global correction is untested [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1760] — add a page test that starts a second global correction and triggers a reload while an attempt is tracked (including during `RequestSent`), then asserts the original intent and `MessageId` are kept. [verification]
- [x] [Review][Patch] (medium) The page proof stub ignores the arguments it receives [tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs:3995] — it keys only on `correlationId`, so swapping `proof.OriginalTimestamp` and `submittedAtUtc` at the call site leaves every page test green. Delegate to `GlobalAdministratorAuditProofLookup.FindAsync`, as the panel stub does. [verification+blind+auditor]
- [x] [Review][Defer] (unverified medium) Global projection version comes from the last event iterated, not the highest [src/Hexalith.Tenants/Projections/GlobalAdministratorProjectionHandler.cs:71] — deferred: an out-of-order batch would understate the version and keep confirmation pending. What would settle it: whether EventStore guarantees that `ProjectionRequest.Events` arrives in sequence order. `TenantProjectionHandler.StampAggregateProjectionVersion` uses a monotonic maximum. The `>= 0` guard matches that handler, so it is not a defect.
- [x] [Review][Defer] (medium) After an ambiguous restore POST that actually committed, redelivery is withdrawn [src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor:1369] — deferred: this follows the existing policy, not a fault introduced here. The new restore branch mirrors the removal path and `GlobalAdministratorsPage` `WithdrawDeliveryRetry`: a changed projection fails `Matches`, the attempt stays ambiguous, and the fixed-aggregate lease is held until a new circuit. A fix needs an idempotent same-MessageId redelivery policy or a MessageId status lookup, and that conflicts with "never dispatch already-applied work".
- [x] [Review][Defer] (unverified medium) BFF and EventStore clock skew could hide an exact proof [src/Hexalith.Tenants.UI/Services/Gateways/GlobalAdministratorAuditProofLookup.cs:62] — deferred: already in `deferred-work.md` (added in `362a957d`). This review adds one point: exact correlation plus sequence already prove causality, so the submit-time bound could be relaxed if the skew is ever confirmed.

**Rejected** (one line each):
- No resume control for a global attempt after close — low: reopening the same row adopts the retained lease, the fix would add UI branches, and the case is rare.
- Pre-dispatch drift leaves restore stuck with Refresh disabled — low: it needs a concurrent global-administrator change between preview and Confirm, and close/reopen recovers.
- Legacy-projection copy is generic, and a null projection shows count "0" — low: the AC7 gate holds, and the dedicated key and the "0" path are reachable only with a null projection, which the page never mounts; the fix would add branches.
- `CommandProofUnavailable` says "tenant evidence" — low: the key is shared with tenant correction, so the fix needs a new key, and the path is an edge case. Separately, the claim that MissingSupport after a complete walk is wrong is false: the walk is complete only after the audit has reached the committed sequence.
- Each system load reads the complete projection — false: the task requires that start-gate evidence, and the pair recheck short-circuits when no registered pair is selected.
- Original-row walk duplicates the proof lookup with weaker checks — low: it re-matches the exact reference, target, time and type, and the pair is already proven by the ordered corrective walk; the refactor is not a direct fix.
- Paired buttons do not say which side of the correction they lead to — low: the references differ, and chains are rare.
- Browser lane misses paired-button focus/forced-colors and revoke copy — low: the buttons are the same `FluentButton` whose focus and forced-colors behaviour is asserted on Cancel, and a page-wide `scrollWidth` check covers receipt overflow.
- Panel stub rewrites projections — false: no harm named beyond the restore-recheck test gap above.
- Registry keeps every pair with no bound — false: unbounded retention within the circuit is the clarified requirement, and corrections are rare.
- Rollout docs lack a runnable replay procedure and omit the system audit — low/false: it is a deployment-time step whose fix depends on EventStore replay tooling, and one `ProjectAsync` run writes both the singleton and `audit:system`.
- Spec says `done` but sprint status says `review` — rejected: the fix edits the spec under review; this review's status sync resolves it.
- Second Start during a tracked attempt is silently ignored — low: the active panel keeps its typed status and recovery, a notice would add branches, and the case is rare.
- Paired link depends on circuit memory and is not rechecked against the baseline — false: the baseline was checked at confirmation and the retained snapshot cannot change; the circuit scope is the approved frozen intent.
- No negative domain test for the gateway; paired tests select buttons by text — low: a wrong-domain status with a matching identity is not realistic, and text selection only tells apart two buttons that share a test id.
- Tenant change during `OpenGlobalCorrectionAsync` applies stale authority — false: those fields are read only for global rows on `system`, which reloads them, and the stale continuation cannot match a row.
- Disposed renderer or off-dispatcher reads in the paired refresh — false: the final apply re-checks reference and generation on the dispatcher, and disposal-time throws match the existing `LoadAsync` pattern with no user-visible effect.
- `EventCount > 1` breaks sequence matching — false: `GlobalAdministratorsAggregate` emits exactly one event for each Set or Remove.
- The deleted revoke "pending" copy now shows a projection read failure as pending — low: it takes a read failure in mid-confirmation, Refresh still works, and the fix needs new state semantics.

### Review Findings — review-fix range `362a957d..4f0abcae` (2026-10-11)

This review covered only the review-fix commits: 19 files, 1,534 diff lines (116,486 bytes). Four layers ran: Blind Hunter, Edge Case Hunter, Verification Gap and Acceptance Auditor. None failed. They raised 34 findings, which triage reduced to 1 decision-needed, 13 patch, 1 defer and 11 rejected entries. The human resolved the decision as a patch and left the patches as action items. The review's status sync fixed the sprint-date patch, leaving 13 open. The gitlink guard passed: `python3 scripts/validate-story-gitlinks.py` exited 0, with no `references/` pointer changes since `66072cc4`.

- [x] [Review][Patch] (medium) The authority/admission fact mirrors Confirm but gives the wrong cause [src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor:552]
  - **Problem:** `AuthorityAdmissionLabel` is `!CreateCorrectionDecision().IsConfirmDisabled`. Whenever Confirm is disabled, for any reason, the fact reads "Authority, command admission, or safe viewport cannot be verified. Refresh before confirming." That covers last administrator, already applied, submitting, and every tracked or terminal state, including Confirmed.
  - **Decision (2026-10-11):** keep the derivation. Review loops 1 and 2 established that the fact never says Ready next to a disabled Confirm. Every disabled branch already renders its specific reason and recovery in the alert at `:42`.
  - **Fix:** reword `Tenants.Correction.GlobalAdmin.Preview.AuthorityAdmission.Blocked` to "Confirm is unavailable for the reason shown above." (EN) and « La confirmation est indisponible pour la raison indiquée ci-dessus. » (FR). Update the test dictionary at `GlobalAdministratorCorrectionPanelTests.cs:3283` and the Blocked assertions. Add one assertion for the Confirmed state.
  [blind+auditor]
- [x] [Review][Patch] (medium) Carrying proof inputs forward breaks the Global Administrators page's unchanged-lease guard [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:5035] — this also applies at `:5109`.
  - **Cause:** `CarryProofInputs` (`TenantAggregateCommandAdmissionGate.cs:500`) keeps `SubmittedAtUtc` and `CommittedEventSequence` on a lease the page adopted from the correction panel. Both are record members, so `Equals(CreateReconciliation(_grantSnapshot|_removeSnapshot), reconciliation)` can never match again.
  - **Effect:** every later gate change rebuilds the page snapshot. The rebuild drops `IsDeliveryRetryWithdrawn` or `LastConfirmedProjection`, refocuses the lifecycle and nudges status.
  - **Fix:** compare against `reconciliation with { SubmittedAtUtc = null, CommittedEventSequence = null }`, and add a page test that adopts a panel lease with proof inputs.
  [edge+auditor]
- [x] [Review][Patch] (medium) A verified contradiction claims that another attempt is active and that audit evidence is delayed [src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor:276]
  - **Cause:** after `IsVerifiedContradiction` releases the lease, the snapshot still has `HasCommandTracking`, so `TryGetTrackedLiveBlock` returns `AggregateBusy` (`:459`). Separately, `ConfirmProjection` routes through `UnableToVerify` (`GlobalAdministratorCorrectionSnapshot.cs:674`, helper `:766`), which sets `AuditDelayed` and a Refresh recovery key even though Refresh is disabled and no proof lookup will run.
  - **Effect:** the assertive alert says "unavailable while another global-administrator attempt is active", and the lifecycle says "Corrective audit evidence is delayed."
  - **Fix:** check `IsVerifiedContradiction` before the tracked live-block check and return the Unavailable recovery. Give the contradiction branch `AuditUnavailable` and no Refresh recovery. Assert both in `Reached_sequence_with_wrong_postcondition_releases_global_lease`.
  [edge+auditor]
- [x] [Review][Patch] (medium) The regression test for the off-dispatcher `OnProofAvailable` fix passes without the fix, and the Refresh call site is untested [tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs:1789]
  - **Why the test passes unfixed:** without the `InvokeAsync` wrap, the page's background re-verification still renders the paired button and `Remember` runs first. Only the panel's Confirmed state and `tenants-correction-proof-link` are lost, and the test asserts neither.
  - **Fix:** assert `LifecycleState == Confirmed` and the proof link. Add a page test that confirms with an empty audit and makes the proof available off the dispatcher. That test should click `tenants-correction-refresh` (`GlobalAdministratorCorrectionPanel.razor:1834`).
  [verification+auditor]
- [x] [Review][Patch] (medium) The route-driven paired-refresh test resolves the receipt from a pre-filled cache [tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs:1673] — before navigating to `?receiptReference=event-corrective`, the test clicks both paired buttons, so `_verifiedGlobalRows` already holds the off-page row. Deleting `RefreshPairedGlobalReceiptAsync()` at `TenantAuditPage.razor:830` leaves it green. Navigate on a cold cache instead, then assert a Ready receipt with its back-link. [auditor]
- [x] [Review][Patch] (medium) Proof-input carry-forward is tested at only one of five gate write sites [tests/Hexalith.Tenants.UI.Tests/State/TenantAggregateCommandAdmissionGateTests.cs:135] — the test reaches only `TryRetainReconciliation` (`:490`). Reverting `:299`, `:332`, `:363` or `:462` to plain assignment would stay green. Turn the test into a theory over `TryAdvanceReconciliation`, `TryBeginReconciliationDispatch` and `TryCompleteReconciliationDispatch`. Add a case proving that a different `MessageId` or action does not inherit the inputs. [verification+blind]
- [x] [Review][Patch] (medium) A confirmed attempt without proof inputs keeps an enabled Refresh that does nothing, and the guard is untested [src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor:383]
  - **Behaviour:** the Confirmed branch of `CanRefreshSnapshot` enables Refresh for every state except `AuditAvailable`. Each click therefore reruns a lookup that is guaranteed to return `(null, false)` and refocuses the lifecycle.
  - **Untested guard:** no test reaches the new `HasCorrectiveProofInputs` guard at `:1826`.
  - **Claim gap:** this leaves the review-loop item "a Refresh that never ends" only partly fixed.
  - **Fix:** require `HasCorrectiveProofInputs(snapshot)` in that branch. Extend `Malformed_original_timestamp_confirms_projection_but_does_not_link_corrective_proof` to assert that Refresh is disabled and the state stays `MissingSupport`.
  [edge+auditor+verification]
- [x] [Review][Patch] (low) No test separates the authority/admission label's new derivation from the old one [tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorCorrectionPanelTests.cs:66] — the old and new expressions differ only while the remove preview is still being composed, and every compose stub completes synchronously. Add an async compose stub. Assert the label is not Ready while the compose is pending and Confirm is disabled, and Ready afterwards. Apply this patch after the authority/admission copy patch above. [verification]
- [x] [Review][Patch] (low) Keeping the remove preview can overwrite a fail-closed projection state at the same version [src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor:714] — when a stale or non-current projection keeps its version, `FromIntent` returns `CurrentProjectionUnavailable`. `WithRemovePreview` then resets it to Previewed and clears the message, so count and state render as verified. Confirm stays disabled by availability. Re-attach the preview only when `refreshed.LifecycleState is Previewed`. [verification]
- [x] [Review][Patch] (low) The proof notification makes Confirm wait for the page's protected re-walk, and page exceptions now reach the panel [src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor:1770] — this also applies at `:1834`.
  - **Delay:** the panel awaits the page's whole `RefreshPairedGlobalReceiptAsync` before it applies Confirmed and settles the lease. For a receipt-launched correction, that is an authorization read plus two protected walks.
  - **Lost isolation:** that method's final apply now runs outside its `try/catch` (`TenantAuditPage.razor:1629`).
  - **Fix:** notify after the panel's own apply, and guard the call.
  [blind]
- [x] [Review][Patch] (low) The in-flight guard also holds Failed attempts that have no correlation [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1747] — this also applies at `:1840`. `MessageId is not null` now also blocks a second Start after a deterministic POST failure. The guard needs only the `RequestSent` window: use `LifecycleState is RequestSent || HasCommandTracking`. [blind]
- [x] [Review][Patch] (low) The server provenance tests miss `system` non-global events and the Removed positive case [tests/Hexalith.Tenants.Server.Tests/Projections/TenantAuditReadModelTests.cs:121] — the only negative case is a `tenant-1` event, which fails both halves of the predicate. Dropping the event-type condition would leave every test green. Add a `system` tenant-aggregate event (expect null) and `GlobalAdministratorRemoved` (expect provenance). [blind+auditor]
- [x] [Review][Patch] (low) `deferred-work.md` records three existing defers a second time [_bmad-output/implementation-artifacts/deferred-work.md:4126] — the "Story 5.7 review pass 3" section repeats the projection-ordering, ambiguous-delivery and clock-skew entries already recorded above it. Delete the three duplicates. [blind]
- [x] [Review][Patch] (low) The sprint-status `last_updated` value reads as November 10 [_bmad-output/implementation-artifacts/sprint-status.yaml:32] — the file uses MM-DD-YYYY, so `11-10-2026` should be `10-11-2026`. Fixed during the review's status sync. [blind]
- [x] [Review][Defer] (unverified medium) The documented global-projection replay can overwrite tenant rows in `audit:system` [docs/event-contract-reference.md:127] — deferred: this is pre-existing and already in the ledger ("Separate or safely merge the system tenant audit stream…"). The new point is that the rollout replay is the trigger, so the paragraph needs a precondition warning. What would settle it: whether a tenant aggregate can be named `system`.

**Rejected** (one line each):
- Spec `done` versus sprint `review` — rejected: the fix edits the spec under review; this review's sprint sync updates the status.
- Triage tally mismatch (22 patches, 20 rejected, "Finding 1") — rejected: the fix edits the spec under review. The ledger's "1 decision, 21 patches" matches the 22 checklist items, so that half is false.
- Stale verification counts (4,026 versus 4,029) and no range recorded for pass 3 — rejected: the fix edits the spec under review.
- A correction superseded by a later change shows as unverified, with no proof link — low: it needs a concurrent change from another circuit between commit and projection read, the copy is accurate for the current projection, and a superseded state is new scope.
- Contradiction detected through message keys, and the lease released as Failed — false: `TryReleaseTerminal` uses the state only for `IsTerminal`, sibling snapshots use those keys only with ProjectionPending, and the snapshot and panel tests catch a key rename.
- The page's sequence theory does not test `FindAsync`'s sequence equality — false: the `("tracking-safe", 3L, 5, false)` case in `GlobalAdministratorAuditProofLookupTests` fails if that check is removed. The page case pins the retained sequence the page passes.
- Duplicated `system` + global-event predicate in `CreateEntry` — low: if the two copies diverge, the lookup needs both fields and so fails closed.
- Audit rows persisted before this patch keep correlation IDs — maybe-false, low: it depends on deployment and rebuild. Exposure is limited to the authorized REST API, and the BFF strips both fields.
- Keeping the remove preview relies on `null == null` version equality, and the test does not prove the compose was skipped — false: an unordered version never reaches Previewed, and the synchronous compose stub would fail `ShouldBeSameAs`.
- The `RequestSent` page test does not assert how the attempt ends — low: it pins the guarded behaviour, and the panel tests cover acceptance and confirmation.
- Relative versus absolute `source_spec` paths in the ledger — maybe-false, low: there is no evidence that the sweep requires absolute paths.

### Review Findings — fix-pass range `4f0abcae..94faa46b` (2026-10-11)

Code review 2026-10-11 of the commits since the previous review: `d54b6d51`, `b9e9cc92`, `85a5a6cc` and `94faa46b`.
- **Diff:** 22 files, 3,297 lines, 223,048 bytes (SHA-256 `f15a5379f218a1b690575c44d5b7d57c4c320f6472dcdc9aa6e5a32bcd661b5f`). It is not archived.
- **Excluded:** the `references/` gitlinks, which the guard checks, and the four `711e1008` `/pushall` files (module manifest, Api `Program.cs`, readiness health check and its test).
- **Layers:** Blind Hunter, Edge Case Hunter, Verification Gap and Acceptance Auditor all ran, and none failed.
- **Triage:** 40 findings became 1 decision-needed, 10 patch, 0 defer and 18 rejected entries. The decision was resolved on 2026-10-11 (option a), which made it a patch and made the guard-test patch moot, so the final tally is 10 patch and 19 rejected.
- **Gitlink guard:**
  - `python3 scripts/validate-story-gitlinks.py <spec>` passes (exit 0) only through the `Non-Story Gitlink Commits` mode that `94faa46b` added.
  - The guard as it was before `94faa46b` (`94faa46b^:scripts/validate-story-gitlinks.py`), run with `--ref 94faa46b`, exits 1. It reports 6 `[UNDECLARED]` pointers: Builds, EventStore, FrontComposer, McpCli, Memories and Platform.
  - All six target SHAs are reachable on their remotes' `origin/main`.

- [x] [Review][Patch] (medium) Story commits moved six submodule pointers, and the story changed the gitlink guard to exempt them [scripts/validate-story-gitlinks.py:432]
  - **Resolution (2026-10-11, option a):** declare the moves the existing way and remove the new mode.
    - Rename `## Non-Story Gitlink Commits` to `## Story-Owned Gitlink Commits`, keeping the same two full commit IDs.
    - Add a `## File List` that lists each of the six paths. Each path gets an `old -> new -- reason` line, in story 4.3's format. The reason states that the move was workspace drift carried by the story commit, and that no story code depends on it.
    - Restore `scripts/validate-story-gitlinks.py` and `tests/scripts/test_validate_story_gitlinks.py` from `94faa46b^`.
    - Correct the "six independent pointer updates" prose.
  - **Dry run:** a scratch copy of this spec, modified this way, passes both the current guard and the guard from before `94faa46b`.
  - **Evidence:**
    - The story code adds no imports from any moved submodule.
    - CI runs only the guard's self-tests (`story-guards.yml`).
    - No other file uses the `Non-Story` mode.
  - **Original finding:**
  - **What happened:**
    - Story commits `b9e9cc92` ("feat: enhance global administrator correction functionality") and `85a5a6cc` move 8 pointers across 6 submodules. The `85a5a6cc` message itself says "Updated submodule references for Hexalith.EventStore and Hexalith.Platform".
    - `94faa46b` then added a repo-wide `Non-Story Gitlink Commits` mode to the guard. In that mode a commit ID with no File List entry and no per-pointer reason is enough.
    - The story lists its own two commits under that heading and calls them "six independent `references/` pointer updates". There are eight moves, and they are not independent.
  - **Why it matters:**
    - The project-context rule requires either a File List entry with a reason for each moved pointer, or a revert plus a separate `build(deps)` commit.
    - FrontComposer builds from source by default, and Builds owns the package catalog. So the recorded 4,062/4,062 UI run used the moved pointers.
    - Both commits are already on `origin/main`, so reverting them would mean new pointer-reverting commits, not a history rewrite.
  - **Options:**
    - (a) Declare the pointers under `Story-Owned Gitlink Commits` with File List entries and per-pointer reasons, and remove the `Non-Story` mode from the guard and its tests. Recommended.
    - (b) Keep the `Non-Story` mode as repo policy, but require a reason and owner for each excluded commit, correct the "six independent" claim, and update the project-context gitlink rule.
    - (c) Accept as is.

  [auditor+blind+edge]
- [x] [Review][Patch] (medium) Count, presence and provenance render as verified next to "Current projection evidence is unavailable." [src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor:531]
  - **Cause:** this diff made `IsProjectionStateVerifiable` depend only on `LastConfirmedProjectionEvidence` and dropped the `CurrentProjectionUnavailable` marker check. Only the revoke-confirm branch clears the evidence on a failed read.
  - **Where it shows:**
    - Restore Confirm with a null projection read (`:980`).
    - The grant retry preflight (`RetainAmbiguousPreflight`, `:1447`), which also keeps old evidence when the rebuilt preview differs.
    - A changed revoke preview at Confirm (`:1065`), where the evidence is current but the state says unavailable.
  - **Effect:** in all three cases the panel shows the earlier count, Present/Absent and "Complete, current system projection…" beside the unavailable alert.
  - **Fix:** require both the readable complete evidence and `SafeMessageKey != "Tenants.Correction.Unavailable.CurrentProjectionUnavailable"`. Update the stale comment above the property. Assert the neutral rendered facts for the restore null-read and changed-revoke paths.

  [edge+auditor+verification]
- [x] [Review][Patch] (medium) Every load of the `system` audit page re-walks every pending completion, even proven ones and even with no receipt selected [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1685]
  - **Cause:**
    - `LoadAsync` (`:1190`) calls `RefreshPairedGlobalReceiptAsync` on every system load, including read-refresh notifications.
    - The new condition dropped `selectedReference is not null`. That short-circuit is what the earlier rejection "Each system load reads the complete projection — false" relied on.
    - `GlobalAdministratorCorrectionProofRegistry` never removes a pending completion, even after `Remember` succeeds.
  - **Effect:** after one correction, each load, page change or notification costs an authorization read, a complete global-administrator projection walk, and two protected audit walks of up to 50 pages for each completion in the circuit.
  - **Fix:** run the pending walk only when a receipt is selected. Remove a completion from the pending list once its pair is remembered. This cannot lose a delayed pair, because only proven entries are removed.

  [blind]
- [x] [Review][Patch] (medium) The contradiction-alert regression assertions cannot fail [tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorCorrectionPanelTests.cs:800]
  - **Problem:**
    - `Reached_sequence_with_wrong_postcondition_releases_global_lease` checks "another global-administrator attempt" on `tenants-correction-safe-message`, which never shows the decision reason.
    - It checks "audit evidence is delayed" on `tenants-correction-state`, which never shows audit copy.
    - The stub localizer has no AggregateBusy entry.
  - **Effect:** deleting the `IsVerifiedContradiction` check at `GlobalAdministratorCorrectionPanel.razor:276` brings the misleading busy alert back, and the test stays green.
  - **Fix:** assert the exact text of `tenants-correction-unavailable-recovery` ("Restore the blocked authorization, lifecycle, viewport, or projection evidence before retrying.") and `tenants-correction-audit-state` ("Corrective audit evidence is unavailable.").

  [verification]
- [x] [Review][Patch] (medium) Nothing tests that the Global Administrators page records a completion on its out-of-date path [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:4105]
  - **Scope:** this also covers `:4922`.
  - **Gap:** deleting either `RememberCorrectionCompletion` call in `RetainOrReleaseGrantCompletion`/`RetainOrReleaseRemoveCompletion` leaves every test green. Those calls are reached when the projection read that confirms the command is superseded.
  - **Fix:** add a grant/remove theory with a correction-origin lease (source, sequence and submit time) whose confirming projection read is superseded. Assert one pending completion that carries the source and `ConfirmedProjectionEvidence`, and assert the lease is released.

  [verification]
- [x] [Review][Patch] (medium) Re-subscribing after a principal change and unsubscribing on dispose are both untested [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:759]
  - **Scope:** this also covers `:718` and `:2596`.
  - **Gap:** the principal-change tests register no `TenantReadRefreshSubscription`, so the subscribe step returns early. Deleting the re-subscribe, the lease disposal or the unsubscribe leaves every test green.
  - **Fix:** add a test with a substitute `IProjectionSubscription`. Assert the first lease is disposed and `SubscribeAsync(..., "system", ...)` is received twice. Add another test asserting that after `DisposeAsync` the page no longer reacts to an authentication change.

  [verification]
- [x] [Review][Patch] (low) A failed grant dispatch start leaves the fixed global-administrators lease held and the dispatch token open [src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor:1122]
  - **Lease held:**
    - The grant branch calls `TryMarkDispatched` before `ToCompletionReconciliation()` and `TryBeginReconciliationDispatch`.
    - If either of those fails, `TryAbandonBeforeDispatch` refuses the dispatch-marked lease (`TenantAggregateCommandAdmissionGate.cs:556`), and `_admissionLease` is never set.
    - The lease therefore stays locked for the whole circuit.
  - **Token open:** the grant completion is written only after the renderer apply (`:1289`, `:1354`), and the `completedIoSnapshot` catch branch neither completes nor aborts the token.
  - **Fix:** use `TryBeginInitialReconciliationDispatch` as the remove branch does. Then complete the token before the renderer apply, or abort it when completion is refused or the catch branch runs.

  [blind+edge]
- [x] [Review][Patch] (low) The authentication transition reads `TenantId` and writes `_initialLoadStartedTenantId` off the dispatcher [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:746]
  - **Problem:** after `await authenticationStateTask.ConfigureAwait(false)`, lines `:746` and `:756` run on a thread-pool thread. `OnParametersSetAsync` reads and writes the same field on the dispatcher (`:951`).
  - **Fix:** move both into the `InvokeAsync` block that clears `_authenticationTransitionPending`, following the page's own "Marshalled for the same reason" convention at `:1178`.

  [blind]
- [x] [Review][Patch] (low) The list of circuit-only proof-input fields is maintained by hand in three places [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:5061]
  - **Where:** the unchanged-lease guard resets the same five init-only fields at `:5061` and `:5142`. `CarryProofInputs` (`TenantAggregateCommandAdmissionGate.cs:547`) carries the same list.
  - **Risk:** review 2 already found that adding these fields broke the guard. The next init-only member added to `GlobalAdministratorReconciliationState` will break it again silently.
  - **Fix:** centralize the list in one helper on the record and use it in all three places.

  [blind]
- [x] [Review][Patch] (low) The new deferred-work entries are filed under the wrong review [_bmad-output/implementation-artifacts/deferred-work.md:4128]
  - **Misfiled:** the four module-manifest entries came from the dev loop's "Review 6" of the independent commit `711e100858e59cf8de2b626ea21790d59f969023`, but they sit under the heading for the `362a957d..4f0abcae` code review.
  - **Absolute path:** the first entry uses the machine-specific `source_spec` path `/home/administrator/…`.
  - **Fix:** move the manifest entries under a heading that names their real source, and make the path relative like its neighbours.

  [blind+auditor]

**Rejected** (one line each):
- The guard's `Non-Story` mode has no failing test for a bogus exclusion or an off-path baseline — moot: decision option (a) removes the mode and its tests. The finding was valid: the Verification Gap layer removed both checks and the suite still passed 28/28. [verification+blind]
- The off-dispatcher regression test can pass without the `InvokeAsync` wrap — low: the panel now applies Confirmed and the proof link before notifying, and the page's handler renders through its own `InvokeAsync`. Removing the wrap therefore has no user-visible effect left to detect, and an off-dispatcher affinity probe would be a new test harness.
- A projection-loader throw in the pending walk clears pairs that were already verified — low: the production `TenantQueryGateway` catches every exception and returns an unreadable snapshot, which the readable check skips. A guard would protect a state that was never demonstrated. [auditor+edge]
- A contradiction reports `AuditUnavailable` without reading the audit — low: review 2's approved patch prescribed exactly this. The panel offers no corrective evidence, and a separate "not checked" state needs new copy.
- The proof-input carry-forward test misses the initial-dispatch site — low: a fresh lease has no earlier reconciliation, so the carry at that site has nothing to carry.
- Spec `done` versus sprint `review`, the stale "13 open" narrative, and undated verification counts — rejected: the fix edits the spec under review, and this review's status sync sets both statuses. [auditor+blind+edge]
- XML documentation missing on the new internal registry and snapshot members — low: the existing internal members of those files (`Remember`, `Find`, `FindAll`, `WithReconciliation`, `ToReconciliation`) are also undocumented, and no analyzer enforces it.
- A principal change leaves the circuit-wide admission gate and proof registry in place — low:
  - **Reachability:** a same-circuit principal change needs an in-circuit identity switch, because sign-out normally ends the circuit.
  - **No exposure:** the next principal's walks reauthorize as that principal, and adoption exposes only an attempt that principal could start from the same row.
  - **Cost of a fix:** binding attempts to a principal would add new state.

  [auditor+blind]
- A correction command adopted by the Global Administrators page can stay pending forever — false:
  - **Missing reconciliation:** page leases always carry reconciliation through `TryBeginInitialReconciliationDispatch` (`:3589`, `:4346`).
  - **Missing sequence:** staying pending is the intended fail-closed policy (review 3 rejection).
  - **Sequence at or below the baseline:** this is unreachable, because the baseline is read before dispatch.
- A contradiction is declared final when a later change supersedes it — low, carried: this needs a concurrent change from another circuit, and the copy is accurate for the current projection.
- "Confirm is unavailable for the reason shown above." can point at "Preview is ready…" — low: it happens only while the revoke preview is still being composed. The claim that sometimes no reason is shown is false, because `DisabledDecision` always produces one.
- A changed revoke preview uses the wrong copy, focuses the disabled Refresh and offers no re-preview — low, carried from the review 1 restore rejection: it needs a concurrent change between preview and Confirm, and close/reopen recovers. The verified-facts part is in the patch above.
- Recovering from a faulted authentication change does not re-subscribe to live updates — low: the authentication task has to fault, and the fix adds re-subscription to `RefreshAsync`.
- The new catches swallow exceptions without logging — low: neither component has a logger, and bare fail-closed catches are already the page pattern (10 instances).
- A retained page-origin removal blocks the audit surface with only the generic AggregateBusy recovery — low: it needs a disposed Global Administrators page with an ambiguous removal, adoption is restricted by the loop-2 spec, and the fix needs new copy.
- An unparseable `originalTimestamp` stops a replacement panel from adopting — false: production intents format it from the row's `DateTimeOffset` with `"O"` (`TenantCorrectionStartIntent.cs:61`), so it is always parseable.
- An exception thrown by `LoadAsync` after an authentication change leaves the page in Loading — low: `LoadAsync` already catches the gateway exception types, and the normal `OnParametersSetAsync` path has the same exposure.
- The second-Start guard blocks correlated terminal attempts but allows an uncorrelated Failed one — low: terminal blocking predates this diff (the old `MessageId` guard), and the review 2 patch prescribed the new condition.
- Moving the proof notification after the apply means a panel disposed mid-walk never notifies the page — false: during the submit walk `CanClose` is false (the lease is dispatch-marked and the state is not terminal), and the Refresh walk, where Close is allowed, still notifies after a failed apply.

### Review Findings — fix-pass range `c3f46c12..1ac82206` (2026-10-11)

Code review 2026-10-11 of `1ac82206`, the fix pass for the ten patches above. `c3f46c12` only recorded those findings.
- **History note:** after the previous review, `94faa46b` was rebased unchanged as `db87514f` (`git range-diff` shows `=`), on top of the independent commit `6b6a35f0` (bootstrap option). `6b6a35f0` is outside this story and this review.
- **Diff:** 16 files, +442/−193, 1,119 lines, 78,900 bytes (SHA-256 `07e1bd8bb311cd7341b28b5845c993efebeb0221e54cb07cd182b9d5c25b6714`). It is not archived. It has no `references/` changes.
- **Layers:** Blind Hunter, Edge Case Hunter, Verification Gap and Acceptance Auditor all ran, and none failed.
- **Triage:** 38 findings became 0 decision-needed, 7 patch, 0 defer and 14 rejected entries.
- **Isolated checkout at `1ac82206`:** the full UI suite passes 4,072/4,072. Every mutation cited below was run against that checkout.
- **Gitlink guard:**
  - `python3 scripts/validate-story-gitlinks.py <spec>` passes (exit 0) on the working tree and with `--ref 1ac82206`.
  - The guard and its tests are byte-identical to `94faa46b^`. The `Non-Story` mode is gone, and the eight moves are declared under `Story-Owned Gitlink Commits` with per-pointer File List reasons.

- [x] [Review][Patch] (medium) The dispose test cannot fail if the authentication unsubscribe is removed [tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs:2067]
  - **Why it can't fail:** `DisposeAsync` sets `_disposed` before unsubscribing (`TenantAuditPage.razor:2595`). `ApplyAuthenticationStateChangedAsync` returns on `_disposed` before any read or subscribe. So `Disposed_audit_page_does_not_react_to_principal_change` passes even with a live handler.
  - **Mutation:** deleting `AuthenticationStateChanged -= OnAuthenticationStateChanged` (`TenantAuditPage.razor:2598`) leaves 4,072/4,072 green. A missing unsubscribe would keep every disposed audit page referenced by the circuit's provider.
  - **Fix:** after `DisposeAsync`, read `_authenticationTransitionVersion` by reflection. Raise `ChangePrincipal`, then assert the value is unchanged. `OnAuthenticationStateChanged` increments it unconditionally.

  [verification+blind+auditor+edge]
- [x] [Review][Patch] (medium) Nothing pins the confirming projection passed by the superseded-read call sites [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:3926]
  - **Scope:** this also covers `:4803`.
  - **Gap:** `Superseded_confirmed_projection_completion_records_correction_source_and_releases_lease` (`GlobalAdministratorsPageTests.cs:6172`) invokes `RetainOrRelease{Grant,Remove}Completion` by reflection and passes `after` itself. It never runs the call sites that forward `snapshot` as `confirmedProjection`.
  - **Mutation:** dropping that argument at both call sites leaves 4,072/4,072 green.
  - **Effect:** `RememberCompletion` then receives a null `ConfirmedProjectionEvidence` and silently rejects it (`GlobalAdministratorCorrectionProofRegistry.cs:20`). A correction confirmed on the page's superseded path would then never produce its paired receipt (acceptance criterion: sibling-page completion).
  - **Fix:** drive the real superseded projection read for grant and remove. Advance the page's mutation generation while the confirming read is pending. Assert that the pending completion carries that read as `ConfirmedProjectionEvidence`.

  [blind+auditor+edge]
- [x] [Review][Patch] (medium) Nothing tests resuming a superseded grant whose POST throws [src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor:947]
  - **Gap:** this diff added the `if (resumeAdopted)` resume block to `PublishCorrectionDeliveryCompletionAsync`. It also moved the grant failure path to complete or abort the token before publishing (`:1362-1378`).
  - **Mutation:** deleting the block, or restoring the old failure-path order, each leaves 4,072/4,072 green.
  - **Probe:** a copy of `Superseded_accepted_correction_is_immediately_adopted_resumed_and_confirmed` (`GlobalAdministratorCorrectionPanelTests.cs:1846`) whose pending POST faults with `HttpRequestException` passes at HEAD and fails under both mutations.
  - **Effect of a regression:** the superseded grant stays retained, the same-ID ambiguous resend never starts, and the global-administrators lock stays held until an unrelated event.
  - **Fix:** add that test. Assert Confirmed, `TrackedMessageIds` equal to `[id, id]`, and `IsLocked == false`.

  [verification]
- [x] [Review][Patch] (low) The grant dispatch-start source pin matches the remove branch [tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorCorrectionPanelTests.cs:2166]
  - **Problem:** the needle `acquiredLease.TryBeginInitialReconciliationDispatch(` first matches the remove branch (`GlobalAdministratorCorrectionPanel.razor:1121`). The grant call is at `:1133`.
  - **Effect:** reverting the grant branch to `TryMarkDispatched` plus `TryBeginReconciliationDispatch` leaves the suite green. The pass-3 lease-held fix is then unpinned.
  - **Fix:** assert that the needle occurs twice inside the callback range, and that `SubmitAsync` no longer contains `TryMarkDispatched(_admissionOwner)`.

  [blind+auditor+edge]
- [x] [Review][Patch] (low) Nothing tests that the live region returns to polite once a readable projection clears the unavailable marker [tests/Hexalith.Tenants.UI.Tests/State/GlobalAdministratorCorrectionSnapshotTests.cs:491]
  - **Mutation:** deleting the Polite reset (`GlobalAdministratorCorrectionSnapshot.cs:669-670`) leaves 4,072/4,072 green.
  - **Effect of a regression:** a readable-but-lagging refresh would keep announcing the pending state assertively.
  - **Fix:** in the existing theory, assert `unavailable.LiveRegionPoliteness` is Assertive and `readableButLagging.LiveRegionPoliteness` is Polite.

  [verification]
- [x] [Review][Patch] (low) The command-identity predicate now exists twice [src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorReconciliationState.cs:58]
  - **Problem:** `WithCarriedProofInputs` repeats `HasSameCommandIdentity` (`TenantAggregateCommandAdmissionGate.cs:696`). The gate still uses its own copy at `:398`, `:498`, `:526` and `:690`.
  - **Risk:** `TryCompleteReconciliationDispatch` validates with one copy and carries with the other. If only one is tightened, a validated completion silently drops `SourceAuditReference` and `ConfirmedProjectionEvidence`, so the pair is lost.
  - **Fix:** make the predicate one internal method on the record, called by both the gate and `WithCarriedProofInputs`.

  [blind+auditor]
- [x] [Review][Patch] (low) The new deferral is filed under the wrong heading again [_bmad-output/implementation-artifacts/deferred-work.md:4148]
  - **Problem:** "Reconcile the independent authentication fallback deferred entry…" comes from the dev loop's Review 8 triage ("Review 8 blind: deferred alias-test entry is stale"). It sits under the `711e1008` "(Review 6)" heading.
  - **Fix:** give it its own heading that names the Review 8 fix-pass source.

  [blind+auditor+edge]

**Rejected** (one line each):
- Spec `done` versus sprint `review` — rejected: this review's status sync sets both. [blind+edge+verification]
- The spec still cites `94faa46b`, which no branch contains, and the closeout block still says "28/28" and "non-story" — rejected: the fix edits the spec under review. The history note above maps `94faa46b` to `db87514f`. [blind+auditor]
- The "no story code depends on this pointer move" reasons are untested, and the File List names only gitlinks — rejected: that wording and format are the human's option (a) resolution, and the fix edits the spec. [blind]
- No Release or package-mode build is recorded — low: CI `build-and-test` passed on `6b6a35f0`, which carries all eight pointer moves. This diff changes no package or project references, and recording evidence is a spec edit. [blind]
- The `_isSubmitting` guard drops a loss of authorization during submit — false: authorization changes go through the panel's `OnAuthenticationStateChanged` (`:2026`). It retains for replacement and calls `InvalidateOperation`, which clears `_isSubmitting`. Three tests fail when the guard is removed. [blind]
- The failed-read marker lives in `SafeMessageKey`. It is lost when a null-status, unverified-identity or sequence-guard transition leaves ProjectionPending, and it reaches the Global Administrators page through reconciliation — low:
  - **Lost marker:** those transitions end in UnableToVerify. Its existing presentation shows the last-confirmed facts without the contradicting alert that the pass-3 patch removed.
  - **Global Administrators page:** the copy it carries there is accurate.
  - **Cost of a fix:** a dedicated flag adds state, and it needs a clearing rule outside ProjectionPending.

  [blind+edge]
- Two new adoption calls run after `ConfigureAwait(false)` — low:
  - **Existing pattern:** five sites already do the same (`:1304`, `:1653`, `:1727`, `:1778`, `:1871`).
  - **Effect:** the method only writes fields and calls the locked gate. The gate's `StateChanged` re-renders through `InvokeAsync`, so any inconsistency lasts one frame.
  - **Cost of a fix:** a consistent fix means refactoring all seven sites.

  [blind+edge]
- No mutation checks are recorded — rejected: the fix is a spec edit, and this review mutation-verified each test gap. [blind]
- The commit message hides the guard revert — low: the message passes the pinned commitlint, the revert is the recorded option (a) decision, and changing it means rewriting history. [blind]
- The new ProjectionPending unavailable state has no browser or EN/FR fixture — low: the resource key and its EN/FR copy predate this diff, and the markup reuses the existing fact and provenance fields. The live-region part is the patch above. [blind]
- A second `RetainOrReleaseCompletion` runs when the callback throws after retaining — low: it needs the gate or renderer to throw after retention, and the fix adds an "entered" flag. [edge]
- A grant apply refused without disposal leaves the lease owned — false: the renderer refuses only during teardown. `Dispose` (`:2350`) retains through `RetainReconciliationForReplacement`, and `TryCompleteReconciliationDispatch` publishes onto an unowned lease. [edge]
- An unprovable pending completion is re-walked on every load with a receipt selected — low: it is limited to this circuit's own corrections and to loads with a receipt selected, and eviction needs attempt tracking. [edge]
- `Story-Owned` mode ignores a later unlisted pointer commit — low and by design: the guard's docstring says owned mode excludes later committed dependency updates, and restoring exactly this guard was option (a). [edge]

## Implementation Notes

- Correction is available from authorized `system` audit rows after a complete current platform projection read. Both commands use the fixed global-administrators aggregate and retain one circuit-local attempt across component replacement and reconnect.
- The BFF alone checks audit correlation, aggregate sequence, event, target, scope, and original/submit time boundaries. Public audit rows are redacted. Paired receipts reauthorize and re-query both events on each opening; failed rechecks remove only the affected links. The circuit retains every proven pair for its lifetime, including both independently verified neighbors in a correction chain.
- A hard browser reload creates a new InteractiveServer circuit, so its earlier in-flight attempt and proven-pair registry cannot be restored. A durable protected causal store would be required for that stronger recovery behavior; none was added under the approved no-dependency/no-new-endpoint boundary.
- Review pass 3 refreshed open previews across system audit pages without global rows and added page tests with fixed proof provenance and unavailable original evidence. The earlier 22 review patches and these three follow-up findings are resolved; pre-existing projection and ambiguous-delivery risks are recorded in `deferred-work.md`.
- Matrix coverage: restore/remove and paging in `GlobalAdministratorCorrectionPanelTests`; already-applied, last-admin, and self-removal in the panel/snapshot tests; duplicate/reconnect/ambiguous reconciliation in panel tests; unrelated and post-submit audit negatives in `GlobalAdministratorAuditProofLookupTests` and receipt recheck in `TenantAuditPageTests`. All covering tests ran in the full passing suites below.
- Confirmed corrections may retry delayed audit proof without redispatch. Stored/published progress stays pending until Completed supplies the exact end sequence. A proof found while the panel closes is retained by the circuit registry before renderer application.
- Review loop 3 retains the complete projection that actually confirmed each circuit-local completion. Audit return requires a fresh complete current projection at or beyond that version and exact protected original/corrective rows, so a later valid reversal does not erase the earlier command's receipt pair. The panel records a confirmed completion before waiting for delayed audit proof; stale sibling status callbacks keep the committed sequence before lease handoff.

## Spec Change Log

- Review loop 1: successive corrections lost one reverse link, so the human clarified that a receipt shows every independently verified pair. The frozen intent now also scopes retry/reconnect recovery to the same Blazor circuit and discloses hard reload limits. KEEP the complete projection/authority/admission checks, fixed-scope command lease, and BFF-only proof validation.
- Review loop 1: legacy global projections have no ordered version. Added an explicit full-history replay rollout gate and fail-closed acceptance. This avoids treating a legacy null version, wall time, or an unrelated audit row as a causal baseline. KEEP the new handler's ordered version and exact command-sequence postcondition.
- Review loop 1: prior browser validation did not render the new global correction controls. Added real EN/FR panel and paired receipt fixture coverage to the task and verification plan. This avoids claiming accessibility coverage from unrelated tenant correction fixtures. KEEP existing browser lanes and bUnit behavior tests.
- Review loop 2: review found that the shared lease loses the source audit identity on sibling-page adoption and completion, and that cached off-page proof survives a same-circuit principal change. Added explicit origin retention, sibling-completion recovery, and authentication invalidation tasks and acceptance cases. This avoids pairing a command to a different same-target row or showing a prior principal's cached receipt. KEEP fixed-scope ULID locking, exact correlation/sequence proof, fail-closed projection and audit checks, EN/FR and browser evidence, all verified prior review patches, and the existing user-owned frozen intent. The committed implementation since the baseline is preserved; resetting it would violate the repository instruction to preserve existing changes.
- Review loop 3: a sibling-confirmed correction can lose its pair if a later valid correction reverses the target before audit return. Retain the complete confirmation projection in circuit reconciliation and recheck a fresh complete current projection plus both protected audit rows. This avoids reconstructing an earlier postcondition from a later state. KEEP the frozen intent, exact command provenance, current authorization, and all existing implementation; the user explicitly requires preserving it, so this correction is applied in place.

## Review Triage Log

| Finding | Verdict and evidence | Route |
| --- | --- | --- |
| Verification gap: production gateway proof path untested | medium — helper and panel tests bypass `TenantQueryGateway.FindGlobalAdministratorCorrectionProofAsync`; a call through the redacted public read would silently lose proof. | patch |
| Verification gap: page proof callback untested | medium — the page test seeds `GlobalAdministratorCorrectionProofRegistry` directly, so removing the mounted panel's `OnProofAvailable` binding leaves existing tests green. | patch |
| Edge: audit version reach guard blocks lagging proof | false — `GlobalAdministratorAuditProofLookup` has no `HasReached` check on audit projection version at the cited lines. Its delayed-proof retry problem is separately confirmed below. | reject |
| Edge: cross-service submit clock may reject a valid event | maybe-false — the BFF clock supplies `SubmittedAtUtc` and EventStore supplies the event timestamp; evidence of production skew or a timestamp ordering guarantee is needed to decide reachability. If true, proof availability is medium harm. | defer (unverified medium) |
| Edge: closing panel during proof lookup loses pair | medium — disposal invalidates the operation before `OnProofAvailable` runs, although the gateway may already have returned a verified proof. | patch |
| Edge: a receipt in two successive corrections loses its first reverse link | medium — `Remember` overwrites the shared reference key, so A→B remains but B→A becomes B→C. Human clarification chose every independently verified paired link. | intent_gap; resolved in frozen intent |
| Edge: existing persisted global projection lacks ordered version | medium — the old handler stored `ProjectionVersion = null`; the new preview requires an ordered version and the read handler returns the stored value. Existing deployments need a replay or migration path not specified in the plan. | bad_spec |
| Blind: backend audit DTO exposes provenance directly | false — the approved task explicitly adds minimal authorized provenance to `TenantAuditEntry`; the BFF public row path removes both fields before rendering. | reject |
| Blind: same-page direct proof route skips lookup | medium — `OnParametersSetAsync` resolves the new receipt then returns on `!needsInitialLoad` without `RefreshPairedGlobalReceiptAsync`, so an off-page corrective receipt stays unavailable. | patch |
| Blind: confirmed audit delay cannot retry proof | medium — `CanRefreshSnapshot` excludes `Confirmed`, while proof lookup runs only in the status-to-confirmed transition; a later audit catch-up cannot surface a link in the panel. | patch |
| Blind: stored/published progress becomes unable to verify | medium — those statuses set `ProjectionPending` without a committed sequence, then `ConfirmProjection` treats the missing sequence as a terminal verification failure. | patch |
| Blind: paging away leaves unsafe old authority | false — retaining the active panel across paging is required; the submit path reauthorizes and re-queries a complete projection and admission before dispatch. | reject |
| Blind: corrective search starts before submit and exceeds page cap | medium — the lookup scans every page from the original event and returns incomplete at 50 pages even when it found a match; starting at the submit boundary removes unrelated earlier history. | patch |
| Blind: original receipt recheck scans later history | medium — the search starts at the original timestamp with no upper bound, so more than 50 pages of later events hides an otherwise valid original. | patch |
| Blind: second intent hides active attempt | medium — `OpenGlobalCorrectionAsync` replaces the intent while the first panel tracks a command; the fixed aggregate stays locked, but the operator loses its visible status and recovery. | patch |
| Blind: authority/admission label can falsely say ready | low — the label ignores `CanSubmit` and action support, so an already-applied or last-admin preview can show readiness beside a disabled Confirm. | patch |
| Blind: browser run does not render new global correction controls | medium — the cited script exercises tenant correction and existing global removal fixtures, while no browser fixture asserts the newly activated panel facts or paired control. The verification plan omitted this specific lane. | bad_spec |
| Review 2 verification: resulting count lacks rendered assertion | medium — the panel renders a calculated high-impact preview number, but existing component and browser checks only assert a nonempty field. | patch |
| Review 2 verification: self-removal warning lacks rendered assertion | medium — the panel selects specific self-removal copy, while only the underlying preview flag is tested. | patch |
| Review 2 blind: lagging audit version reported as missing | medium — the proof walk returns complete absence without checking the ordered audit version against the committed command sequence. | patch |
| Review 2 blind: submit/event clock skew can hide exact proof | maybe-false — BFF and EventStore clocks are independent; actual production skew and timestamp-order guarantees are unknown. If present, proof availability is medium harm. | defer (unverified medium) |
| Review 2 blind: more than 50 corrective audit pages hides proof | low — after 2,500 later matching-time-window rows, the bounded complete walk fails closed even if it saw the target; removing the bound or uniqueness check requires a different query contract forbidden by intent. Unlikely during a correction session. | reject |
| Review 2 blind: more than 50 original audit pages share a timestamp | low — a valid original could exceed the bounded walk only with more than 2,500 events at the exact same timestamp; an exact-reference endpoint is forbidden and the case is unlikely in everyday use. | reject |
| Review 2 blind: one pair recheck exception aborts remaining pairs | medium — the single outer catch discards every independently verified neighbor when one protected read throws. | patch |
| Review 2 blind: legacy projection can open correction preview | medium — the start gate accepts a complete opaque projection version, then the panel rejects it; this misleads the operator before the documented replay gate. | patch |
| Review 2 blind: unrelated command support blocks selected action | medium — the page requires both grant and remove tracking even though the panel and selected intent need only one; a partially supported installation loses its working correction action. | patch |
| Review 2 blind: corrective proof category unchecked | low — the current server projection deterministically maps global events to Access, but a mismapped row accepted by the BFF would weaken the same category invariant used at correction start. One direct guard suffices. | patch |
| Review 2 blind: 20-pair eviction drops older links | medium — the registry removes proven pairs after 20, contradicting the clarified requirement to show every independently verified neighbor on a receipt within its circuit. | patch |
| Review 2 blind: hard-reload recovery limit absent from operator copy | medium — the spec discloses the circuit limit, but the panel recovery text omits it for the operator running the correction. | patch |
| Review 2 blind: browser fixture does not run live callbacks | low — live start, submit, refresh and navigation callbacks are exercised by bUnit/page tests; Chromium verifies rendered EN/FR markup, focus, widths and colors. Adding a live hosted browser stack is disproportionate. | reject |
| Review 2 edge: cached system receipt may render under another tenant | high — a tenant route change leaves `_verifiedGlobalRows` populated, and `ResolveReceiptSelection` can use a cached system row after the new tenant snapshot loads. | patch |
| Review 2 edge: cross-service clock skew | maybe-false — same clock-order question as the blind finding; no deployment timestamp guarantee or measured skew in this repository. | defer (unverified medium) |
| Review 2 edge: lagging audit version appears as missing | medium — same missing sequence-reach guard as the blind finding; audit catch-up must remain delayed. | patch |
| Review 3 blind: global replay can replace `audit:system` tenant history | medium — both projection handlers write that key and a tenant aggregate named `system` is not rejected here; the collision predates this story. | defer |
| Review 3 blind: out-of-order global event batch regresses state/version | maybe-false — the loop stamps the last sequence, but the EventStore delivery-order guarantee has not been established; this was already identified in the review findings. | defer (unverified medium) |
| Review 3 blind: concurrent global rebuilds can overwrite newer state | maybe-false — `SaveAsync` has no compare-and-swap, but whether EventStore serializes projections for this aggregate needs confirmation; full-rebuild saving predates this story. | defer (unverified medium) |
| Review 3 blind: ambiguous committed restore cannot redeliver | medium — the changed projection can fail preview matching before a correlationless retry; the existing global action policy has the same restriction and the frozen intent bars dispatch of already-applied work. | defer |
| Review 3 blind: positive Completed status without sequence remains pending | false — a missing sequence leaves causal proof uncertain, so the snapshot keeps Refresh and the lease rather than claiming success; later status repair can still supply the sequence. | reject |
| Review 3 blind: submit/event clock skew hides exact proof | maybe-false — separate BFF/EventStore clocks can be skewed, but no measured deployment skew or timestamp-order contract shows the event can predate submit. | defer (unverified medium) |
| Review 3 blind: fifty corrective pages can hide a match | low — after more than 2,500 later rows the bounded complete walk fails closed; removing the cap needs a different query contract, while this volume in a correction session is unlikely. | reject |
| Review 3 blind: fifty same-time original pages can hide a match | low — the exact-time bounded walk fails closed only beyond 2,500 rows at one timestamp; an exact-reference endpoint is outside the approved boundary. | reject |
| Review 3 blind: cached off-page receipt stays visible during recheck | low — same-circuit evidence can remain visible briefly while a new protected check runs; failed rechecks remove links, and hiding them immediately would restore the proven Unavailable flash. | reject |
| Review 3 blind: grant panel remains open after pre-submit evidence loss | low — the recheck blocks dispatch and renders UnableToVerify; collapsing the panel would add a branch without improving command safety. | reject |
| Review 3 blind: open preview stays stale on page without global rows | medium — load enrichment skips the open panel when the new page has no global rows, leaving displayed count and authority stale until Confirm. | patch |
| Review 3 edge: out-of-order global event batch | maybe-false — same ordering question as the blind finding; no upstream guarantee was established. | defer (unverified medium) |
| Review 3 edge: cross-service submit clock | maybe-false — same timing question as the blind finding; deployment clock evidence would settle it. | defer (unverified medium) |
| Review 3 edge: paging away leaves open preview stale | medium — same missing refresh condition as the blind finding. | patch |
| Review 3 verification: page stub manufactures the requested sequence | medium — the test stub stamps the caller's sequence onto the returned row, so passing the wrong committed sequence leaves paired-link tests green. | patch |
| Review 3 verification: original evidence recheck lacks a negative case | medium — page tests only return a matching original while negative paths remove corrective proof; skipping the original recheck would go undetected. | patch |
| Review 4 verification: duplicate corrective proof across pages | medium — pre-verified gap: existing duplicate test puts both rows on one page, so an early return after page one could pair ambiguous evidence. | patch |
| Review 4 verification: original receipt on a later page | medium — pre-verified gap: every page test supplies the original on page one, so stopping the protected walk early would hide a valid pair. | patch |
| Review 4 edge: fifty corrective pages | low — carried: the bounded walk fails closed after more than 2,500 later rows; removing the bound needs a different query contract and is unlikely during one correction. | reject |
| Review 4 edge: BFF/EventStore submit clock skew | maybe-false — carried: separate clocks may skew, but production measurements or a timestamp-order contract are needed to show reachability. | defer (unverified medium) |
| Review 4 edge: ambiguous committed grant cannot redeliver | medium — carried: the pre-existing fixed-scope delivery policy withdraws redelivery after the projection changes, and no MessageId status lookup is available. | defer |
| Review 4 blind: same-target audit intent adopts a different correction attempt | medium — the retained lease and panel matcher compare action and target without source audit reference; a second historical row for that target can claim the first row's command and mispair its proof. | bad_spec |
| Review 4 blind: sibling page completes correction without its proof pair | medium — the global-administrator page adopts and releases the shared lease, but has no source audit reference or proof-registry call, so a same-circuit correction loses its causal receipt link. Same missing-origin root as the prior finding. | bad_spec |
| Review 4 blind: replay can replace `audit:system` tenant history | medium — carried: both projection handlers write the key, and a `system` tenant aggregate may collide; the storage issue predates this story and is already deferred. | defer |
| Review 4 blind: original receipt recheck omits Access category | medium — `FindOriginalGlobalAuditRowAsync` accepts a Ready `Administrative` global row while `TenantCorrectionStartIntent.Evaluate` rejects that category, so a paired receipt can use ineligible source evidence. | patch |
| Review 4 blind: cached off-page receipt survives principal change | medium — the page caches verified rows, has no authentication-state subscription, and its snapshot-based authorization flag can remain from the previous principal after a same-circuit change. | bad_spec |
| Review 4 blind: unavailable revoke projection renders count as verified | medium — submit-time `EvaluateCurrentProjection` can set zero and `WithRemovePreview` replaces the key used by `IsProjectionStateVerifiable`; Confirm is blocked but the preview can claim `0` and `Absent`. | patch |
| Review 4 blind: BFF/EventStore submit clock skew | maybe-false — carried: exact correlation could be excluded by skew, but no measured skew or ordering guarantee establishes the trigger. | defer (unverified medium) |
| Review 4 blind: bounded proof walks hide rows past page fifty | low — carried: the walk fails closed, and the earlier triage rejected an unbounded or new-query-contract fix for an unlikely volume in one correction session. | reject |
| Review 4 blind: original recheck accepts opaque projection version | low — the original walk checks only nonblank version even though the corrective walk requires an ordered version; a regressed/legacy audit projection could be treated as current during receipt revalidation. | patch |
| Review 4 blind: Chromium fixture has no live callbacks | low — carried: bUnit/page tests exercise Start, Confirm, Refresh and receipt navigation; Chromium checks rendered focus, layout and safe copy, so adding a hosted browser stack is disproportionate. | reject |
| Review 4 blind: no full EventStore-to-browser integration test | low — the server projection, DTO serialization, gateway proof lookup, panel, page and browser fixture each have passing focused lanes; a live distributed test would add confidence but the claim does not identify an uncovered failed seam. | reject |
| Review 4 blind: final paired-receipt dispatcher apply can throw on disposal | false — carried: the generation and disposed checks prevent stale apply, and the previously reviewed disposal-time behavior matches the page's existing load pattern without a user-visible failure. | reject |
| Review 4 blind: replay paragraph lacks executable runbook | low — the document states the replay scope, required ordered header and fail-closed gate; the `audit:system` collision is already deferred, and a new operator procedure needs deployment-specific tooling outside this feature. | reject |
| Review 5 blind: faulted authentication task leaves audit hidden | medium — `ApplyAuthenticationStateChangedAsync` returns from its catch with the transition flag set and a Loading snapshot, so the page has no visible recovery. | patch |
| Review 5 blind: restore RequestSent cannot be adopted during POST | medium — the grant path marks dispatch without reconciliation, while `ToReconciliation` rejects an ordinary correlationless grant RequestSent; disposal cannot retain that attempt. | patch |
| Review 5 blind: corrective proof beyond fifty pages | low — carried: the bounded walk fails closed after more than 2,500 later rows; the previously reviewed fix needs a different query contract and is unlikely during one correction. | reject |
| Review 5 blind: original proof beyond fifty same-time pages | low — carried: the exact-time walk fails closed only beyond more than 2,500 rows sharing one timestamp; an exact-reference endpoint is outside the approved boundary. | reject |
| Review 5 blind: submit and event clock skew | maybe-false — carried: BFF and EventStore clocks are separate, but measured deployment skew or a timestamp-order contract is needed to show a valid exact event can precede submit time. | defer (unverified medium) |
| Review 5 blind: out-of-order projection batch | maybe-false — carried: the handler stamps the last iterated sequence, and the EventStore sequence-order guarantee has not been established. | defer (unverified medium) |
| Review 5 blind: overlapping projection rebuilds | maybe-false — carried: the full-rebuild save has no version guard, but whether EventStore serializes this aggregate's projection requests remains unverified. | defer (unverified medium) |
| Review 5 blind: global replay can replace system tenant audit | medium — carried: both projection handlers write `audit:system`; this storage collision predates the story and is already in the deferred ledger. | defer |
| Review 5 blind: pending completions cause repeated protected walks | low — pending completions are rechecked on each receipt refresh, but one circuit normally has few corrections; filtering or pruning adds state and could lose a delayed pair. | reject |
| Review 5 blind: browser fixture has no live callbacks | low — carried: page and panel tests exercise callbacks, while Chromium verifies rendered EN/FR focus, layout, and safe copy. | reject |
| Review 5 edge: principal change races panel opening | medium — the open callback checks only its local generation after awaiting and can apply old authorization/projection while an authentication transition is pending. | patch |
| Review 5 edge: event timestamp before BFF submit time | maybe-false — carried: an actual clock-order violation requires deployment evidence; the exact proof boundary is already recorded. | defer (unverified medium) |
| Review 5 edge: post-submit audit exceeds fifty pages | low — carried: the bounded search fails closed and needs a different query contract to lift safely. | reject |
| Review 5 edge: pending completion list grows in circuit | low — the list can increase repeat work, but ordinary per-circuit volume is small and removal must preserve delayed proof recovery. | reject |
| Review 5 verification: rendered revoke sibling handoff is untested | medium — the grant handoff renders through the page, while the revoke test invokes the completion helper directly; removing the revoke call site would leave tests green. | patch |
| Review 5 verification: server audit query provenance is untested | medium — model and DTO tests cover the fields separately, but the stored system audit query test does not assert both survive the query response. | patch |
| Review 6 blind: sibling-confirmed pair lost after a later reversal | medium — `RebuildVerifiedCompletion` confirms the first command against the latest target presence, so a later valid opposite correction hides its earlier pair. | bad_spec; amend completion evidence in place under user preservation instruction |
| Review 6 blind: submit/event clock skew | maybe-false — BFF and EventStore clocks differ; production clock measurements or a timestamp-order guarantee are still needed. | carried defer |
| Review 6 blind: global replay may replace system tenant audit | medium — both projection handlers can write `audit:system`; this predates the story and remains deferred. | carried defer |
| Review 6 blind: out-of-order global batch | maybe-false — ordered delivery by EventStore has not been established; this remains deferred. | carried defer |
| Review 6 blind: concurrent global rebuild | maybe-false — aggregate projection serialization is unverified; this remains deferred. | carried defer |
| Review 6 blind: ambiguous committed POST | medium — same existing fixed-scope delivery limit; the prior triage deferred it because no MessageId status lookup exists. | carried defer |
| Review 6 blind: proof beyond 50 pages | low — the bounded complete walk fails closed beyond 2,500 rows; the previously considered query contract is outside frozen intent. | carried reject |
| Review 6 blind: repeated pending proof walks | low — a circuit normally has few corrections; pruning can lose delayed recovery, as previously triaged. | carried reject |
| Review 6 blind: manifest event topic mismatch | medium — the independently committed module manifest says `tenants-events` while AppHost uses `tenants.events`; this manifest change came from `711e1008`, outside Story 5.7. | defer |
| Review 6 blind: manifest tenant API inventory incomplete | low — the independently committed manifest omits existing API operations; the manifest came from `711e1008`, outside Story 5.7. | defer |
| Review 6 blind: manifest critical flow omits removal | low — its flow omits an already declared removal operation; the manifest came from `711e1008`, outside Story 5.7. | defer |
| Review 6 blind: no live distributed smoke | low — the passing component, gateway, server and rendered browser lanes cover the changed seams; no deployable live stack was available for this review. | carried reject |
| Review 6 edge: stale grant status loses committed sequence | medium — the stale callback calls `RetainOrReleaseGrantCompletion` before storing `status.CommittedEventSequence` in the lease. | patch |
| Review 6 edge: stale removal status loses committed sequence | medium — the removal stale callback has the same ordering gap. | patch |
| Review 6 edge: panel closes before delayed audit proof | medium — only a verified proof is remembered; a confirmed completion with missing proof leaves no pending registry entry. | patch |
| Review 6 edge: retained pair lacks a new projection read | false — a retained pair was already projection-confirmed; receipt reopening reauthorizes and rechecks both protected audit rows. Fresh projection is required for a pending sibling completion. | reject |
| Review 6 verification: manifest authentication fallbacks untested | medium — new fallback keys are absent from API authentication tests, but the `711e1008` manifest/API change is independent of Story 5.7. | defer |
| Review 6 verification: rendered grant handoff misses sequence assertion | medium — the direct helper test prepopulates sequence and the rendered test covers removal only, so the grant call site can regress. | patch |
| Review 7 blind: removal preview changes during Confirm | medium — removal replaces its reviewed preview before dispatch and checks only the new preview, so a changed count or version can bypass the operator's acknowledgment. | patch |
| Review 7 blind: ambiguous committed POST | medium — carried: the same fixed-scope retry cannot resolve a committed correlationless delivery after projection change without a MessageId status lookup. | carried defer |
| Review 7 blind: BFF/EventStore clock skew | maybe-false — carried: deployment clock ordering evidence is still absent. | carried defer |
| Review 7 blind: audit proof past 50 pages | low — carried: the bounded complete walk fails closed beyond 2,500 rows and a new exact-reference query is outside frozen intent. | carried reject |
| Review 7 blind: repeated pending proof walks | low — carried: circuits normally hold few completions, and pruning can lose delayed recovery. | carried reject |
| Review 7 blind: replay may replace system tenant audit | medium — carried: both handlers can write `audit:system`; this predates the story. | carried defer |
| Review 7 blind: out-of-order global batch | maybe-false — carried: EventStore batch ordering is unverified. | carried defer |
| Review 7 blind: concurrent global rebuild | maybe-false — carried: upstream serialization is unverified. | carried defer |
| Review 7 blind: manifest topic mismatch | medium — carried: independently committed manifest topic naming still needs schema confirmation. | carried defer |
| Review 7 blind: manifest API inventory | low — carried: independent manifest operation inventory remains deferred. | carried defer |
| Review 7 blind: manifest critical flow | low — carried: independent manifest flow membership remains deferred. | carried defer |
| Review 7 blind: manifest authentication fallback tests | medium — carried: independent API and manifest change has no fallback authentication tests. | carried defer |
| Review 7 edge: sibling confirms before sequence | medium — a correction-origin page can confirm on EventsStored, release the lease, then lose the later Completed sequence needed for causal pairing. | patch |
| Review 7 edge: source fields reset sibling presentation | medium — `CreateReconciliation` omits the retained source fields, so the unchanged-lease comparison fails and can rebuild a withdrawn retry or removal presentation. | patch |
| Review 7 edge: submit/event clock order | maybe-false — carried: production clock evidence remains missing. | carried defer |
| Review 7 edge: retained pair skips new projection read | false — carried: retained pairs were already projection-confirmed; reopening reauthorizes and rechecks both protected audit rows, while pending completions require a fresh complete projection. | carried reject |
| Review 7 verification: global scope/category guard lacks isolated negative | medium — the existing invalid-global test has an empty target, so removing the new scope/category guard would leave it green. | patch |
| Review 7 verification: OIDC fallback test | medium — carried: the independent API change has no test with only `Oidc:Authority`. | carried defer |
| Review 7 verification: signing-key fallback test | medium — carried: the independent API change has no test with only `Jwt:SigningKey`. | carried defer |
| Review 8 blind: cached off-page receipt stays visible during recheck | low — carried: the prior same-circuit recheck decision applies; failed protected rechecks replace the cached rows atomically, while authentication changes clear them immediately. | reject |
| Review 8 blind: corrective proof past 50 pages | low — carried: the bounded complete walk fails closed beyond 2,500 rows; the query-contract change needed to remove this limit was previously rejected. | reject |
| Review 8 blind: original proof past 50 same-time pages | low — carried: the exact-time walk fails closed beyond 2,500 rows at one timestamp; the prior query-contract decision still applies. | reject |
| Review 8 blind: submit/event clock skew | maybe-false — carried: a deployment timestamp-order guarantee or measured skew is still needed to establish whether an exact event can precede BFF submit time. | defer (unverified medium) |
| Review 8 blind: ambiguous committed POST | medium — carried: the existing fixed-scope retry policy withdraws correlationless redelivery after the projection changes, and there is no MessageId status lookup. | defer |
| Review 8 blind: global replay can replace system tenant audit | medium — carried: both projection handlers write `audit:system`; the collision predates this story and remains in the deferred ledger. | defer |
| Review 8 blind: out-of-order global batch | maybe-false — carried: EventStore's batch-order guarantee is still unverified, while the handler stamps the last iterated sequence. | defer (unverified medium) |
| Review 8 blind: concurrent global rebuild | maybe-false — carried: whether EventStore serializes this aggregate's projection requests remains unverified; the full-rebuild save predates this story. | defer (unverified medium) |
| Review 8 blind: separate six gitlink moves | false — the human-selected Story-Owned Gitlink Commits route records both full pointer-bearing commits and all six exact old/new paths; the restored guard passes and there is no working-tree pointer movement. Rewriting mixed committed history is not needed to reproduce the baseline. | reject |
| Review 8 blind: no live Aspire smoke | low — carried: the component, gateway, server, and rendered browser lanes cover the changed seams; the prior review rejected a live hosted stack for this story. | reject |
| Review 8 blind: deferred alias-test entry is stale | low — the independently committed `TenantsApiGeneratedControllerTests` now include both alias-only tests, but the Review 6 deferred entry still asks to add them. The independent API work and ledger reconciliation are outside this correction intent. | defer |
| Review 8 blind: manifest API inventory incomplete | low — carried: the independently committed manifest omits existing operations; its schema inventory requirement remains a separate deferred decision. | defer |
| Review 8 edge: post-command unreadable projection leaves old facts current | medium — `ConfirmProjection(null)` returns the pending snapshot with only a changed focus, so retained precommand count and presence still render as current after a failed post-command refresh. | patch |
| Review 8 edge: event timestamp before submit | maybe-false — carried: separate BFF and EventStore clocks may violate the time bound, but production clock evidence or an ordering contract is still needed. | defer (unverified medium) |
| Review 9 verification: disabled bootstrap test uses timed absence | medium — the independently committed disabled-bootstrap test waits 50 ms to assert no HTTP call, while the enabled path queues work with `Task.Run`; a late task could evade the assertion. | defer |
| Review 9 edge: retained pair skips current projection read | false — carried: a retained pair already has complete confirmation projection evidence; receipt reopening reauthorizes and rechecks both protected audit rows. Fresh projection is required for pending completions. | carried reject |
| Review 9 edge: BFF/EventStore clock skew | maybe-false — carried: measured skew or a deployment timestamp-order contract is still needed to establish whether an exact event can predate BFF submission. | carried defer (unverified medium) |
| Review 9 edge: out-of-order global projection batch | maybe-false — carried: the handler stamps the last iterated sequence, but EventStore batch-order guarantees remain unverified. | carried defer (unverified medium) |
| Review 9 blind: retained pair skips current projection read | false — carried: retained proof includes the confirming projection and both protected rows are rechecked under current authorization; this same claim was rejected in Reviews 6–8. | carried reject |
| Review 9 blind: corrective proof beyond fifty pages | low — carried: the bounded walk fails closed after 2,500 rows, and removing the cap needs a different query contract; ordinary correction sessions are unlikely to reach it. | carried reject |
| Review 9 blind: submit/event clock skew | maybe-false — carried: no measured skew or timestamp-order contract establishes the trigger. | carried defer (unverified medium) |
| Review 9 blind: out-of-order global projection batch | maybe-false — carried: EventStore batch ordering remains unverified. | carried defer (unverified medium) |
| Review 9 blind: global replay may replace system tenant audit | medium — carried: both projection handlers write `audit:system`; this predates the story and is already deferred. | carried defer |
| Review 9 blind: overlapping global rebuilds | maybe-false — carried: the full-rebuild save has no version guard, but upstream projection serialization remains unverified. | carried defer (unverified medium) |
| Review 9 blind: manifest event topic mismatch | medium — carried: the independent manifest says `tenants-events` while AppHost, clients and deployment docs use `tenants.events`. | carried defer |
| Review 9 blind: manifest tenant API inventory | low — carried: the independent manifest omits existing API operations; the inventory decision remains deferred. | carried defer |
| Review 9 blind: manifest global-admin authorization | medium — the independent manifest says `authenticated`, while `GetGlobalAdministratorsQueryHandler` also requires current membership in the singleton administrator projection. | defer |
| Review 9 blind: manifest recovery inventory | low — the independent manifest leaves recovery hooks and inventory empty despite the documented singleton replay prerequisite, leaving a manifest consumer without that operational dependency. | defer |
| Review 9 blind: manifest critical flow omits removal | low — carried: the independent manifest omits its already declared removal operation from the critical flow. | carried defer |
| Review 9 blind: ambiguous committed POST | medium — carried: a changed projection can withdraw correlationless redelivery and keep the circuit lease; MessageId status lookup is unavailable, so this existing limitation remains deferred. | carried defer |

## File List

These pointer moves were workspace drift carried by the two Story 5.7 commits below. No Story 5.7 code depends on the moved versions. The guard validates their exact committed chain and checks for new working-tree movement.

- `references/Hexalith.Builds`
  - `references/Hexalith.Builds` 9e6b6ca3e8b4e22e6642d4f0d70d25edbf2db08b -> 1bde4d92bb9a9ce48b19a6ca17236075ea28a78b -- workspace drift carried by the story commit; no story code depends on this pointer move.
- `references/Hexalith.EventStore`
  - `references/Hexalith.EventStore` 423dfc55c272df33e35b231dba02e65aad31bd06 -> b57310fb44ef2231815f3c3cfea09714d942334d -- workspace drift carried by the story commits; no story code depends on this pointer move.
- `references/Hexalith.FrontComposer`
  - `references/Hexalith.FrontComposer` 0e114214007c22f5cdbac21a6853cff4208340ee -> 19f4839231ae6ec7cc6318842eb329c2d04caaa9 -- workspace drift carried by the story commit; no story code depends on this pointer move.
- `references/Hexalith.McpCli`
  - `references/Hexalith.McpCli` b2820d21ca34ee7a873772b312ac16569bb8a8ce -> 7e99305a621e1bdf7119b6e924348857e2b5a5a4 -- workspace drift carried by the story commit; no story code depends on this pointer move.
- `references/Hexalith.Memories`
  - `references/Hexalith.Memories` 26d69d4f111d341e02c24d5c2c597cf128e40bf9 -> 499b7a838a36a4cd500ab517d86acf07b1d74ad4 -- workspace drift carried by the story commit; no story code depends on this pointer move.
- `references/Hexalith.Platform`
  - `references/Hexalith.Platform` 72ba792574926897758bf474021336a114c705ca -> 053ee05abff2d60d06c62734fd9d9e0e8953df43 -- workspace drift carried by the story commits; no story code depends on this pointer move.

## Story-Owned Gitlink Commits

These commits mix Story 5.7 code edits with eight pointer moves across six root submodules. Their code edits remain in the review diff. The full IDs pin pointer provenance after the preserved baseline.

- `b9e9cc926360c802b9de94dc3dd3a8c56e4caba6`
- `85a5a6cc18ef6a0f2da69e4fd2c2a1a408f32359`

## Verification

**Current review patch verification (2026-10-12):**
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 --no-restore -v:q` — passed with zero warnings/errors.
- `dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false --no-build --no-restore` — passed 4,075/4,075, none skipped. The two new superseded projection-read cases and the superseded transport-exception case also passed with direct xUnit method runs.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md` — passed with no working-tree gitlink movement. `git diff --check` passed; `git diff HEAD -- references` was empty.

**Review 8 patch verification (2026-10-11):**
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 --no-restore -v:q` — passed, zero warnings/errors; `dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false --no-build --no-restore` — passed 4,072/4,072, none skipped.
- `dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 --no-restore -v:q` initially failed with CS1704 because stale restore assets mixed `Hexalith.Memories.Contracts` source and package references. `dotnet restore tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -p:UseNuGetDeps=false -m:1 --ignore-failed-sources` followed by the same build passed with zero warnings/errors; the server test project passed 834/834.
- The contracts test project source-mode Debug build passed with zero warnings/errors; its test project passed 154/154.
- `TENANTS_BROWSER_BUILD_CONFIGURATION=Debug bash tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh` — passed EN/FR rendered correction and paired receipts, narrow width, forced colors, and focus checks.
- `python3 tests/scripts/test_validate_story_gitlinks.py` — passed 23/23; `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md` — passed. `git diff --check` passed and `git diff HEAD -- references` was empty.
- Commit message `feat(audit): complete global administrator correction from evidence` passed the owning repository's installed pinned `@commitlint/cli` 21.2.3 via `node_modules/.bin/commitlint --edit /tmp/story-5-7-commit-message.txt`; exact message file SHA-256: `31ea7c73f5aca747c81522b10fae41f385fcad4cd7e79cb5155b363547d3309d`.

**Review patch continuation (2026-10-11):**
- `dotnet restore tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -p:UseNuGetDeps=false -m:1 --ignore-failed-sources` and `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 --no-restore -v:q` — passed, zero warnings/errors.
- `dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false --no-build --no-restore` — passed 4,067/4,067, none skipped.
- `TENANTS_BROWSER_BUILD_CONFIGURATION=Debug bash tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh` — passed rendered EN/FR global correction and paired receipt lanes, narrow width, forced colors and focus.
- `python3 tests/scripts/test_validate_story_gitlinks.py` — passed 23/23 after restoring the earlier guard. `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md` — passed against the two declared story-owned pointer commits and no working-tree pointer movement.
- `git diff --check` and `git diff HEAD -- references` — passed; the latter is empty.

**Closeout gate investigation (2026-10-11):**
- `dotnet restore tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -p:UseNuGetDeps=false -m:1 --ignore-failed-sources` followed by `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 --no-restore` — passed, zero warnings/errors after the final patch. The prior CS1704 run used package-mode restore assets with a source-mode `--no-restore` build; the matching source restore selects the Commons UniqueIds project instead of the 2.30.1 package in `EventStore.Contracts`.
- `dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore` — passed 4,062/4,062 after the final review patches, none skipped. The four final patch paths also passed eight focused cases before the full suite.
- `python3 tests/scripts/test_validate_story_gitlinks.py` — passed 28/28. `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md` — passed, with the two exact non-story pointer-bearing commits recorded above and no working-tree pointer movement. The previous default guard failure remains historical evidence of why explicit provenance was needed.
- `git diff --check` and `git diff HEAD -- references` — passed; the latter is empty.

**Commands:**
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1` — passed, zero warnings/errors.
- `dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false --no-build --no-restore` — passed, 4,029/4,029, none skipped.
- `dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1` — passed, zero warnings/errors.
- `dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug -p:UseNuGetDeps=false --no-build --no-restore` — passed, 830/830, none skipped.
- `dotnet test --project tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Debug -p:UseNuGetDeps=false --no-build --no-restore` — passed, 154/154, none skipped.
- `TENANTS_BROWSER_BUILD_CONFIGURATION=Debug bash tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh` — passed, EN/FR desktop/narrow/forced-colors lanes including rendered global panel and paired receipt fixtures.
- `git diff --check` — passed.

**Review 5 patch verification (2026-10-11):**
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -m:1 --no-restore` — passed, zero warnings/errors.
- `dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore` — passed, 4,056/4,056, none skipped.
- `dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug -m:1 --no-restore` — passed, zero warnings/errors; `dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug --no-build --no-restore` — passed, 832/832.
- `dotnet build tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Debug -m:1 --no-restore` — passed, zero warnings/errors; `dotnet test --project tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Debug --no-build --no-restore` — passed, 154/154.
- `TENANTS_BROWSER_BUILD_CONFIGURATION=Debug bash tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh` — passed EN/FR, narrow-width, and forced-colors lanes.
- `git diff --check` — passed; `git diff HEAD -- references` — empty.
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 --no-restore` — exit 1, CS1704 duplicate `Hexalith.Commons.UniqueIds` 3.120.0 source and 2.30.1 NuGet in `references/Hexalith.EventStore.Contracts`.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md` — exit 1: the preserved `66072cc4` baseline and current HEAD differ at six committed `references/` gitlinks. This working tree changes no gitlinks.

**Implementation continuation (2026-10-11):**
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -m:1 --no-restore` — passed, zero warnings/errors.
- `dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore` — passed, 4,052/4,052, none skipped; independently rerun after the six-file patch. This includes restore/remove, already-applied/last-admin/self-removal, ambiguous and duplicate attempts, protected paging, origin-specific adoption, sibling completion, principal change, and duplicate corrective rows across pages.
- `git diff --check` — passed.
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 --no-restore` — blocked by CS1704 in `references/Hexalith.EventStore.Contracts`: `Hexalith.Commons.UniqueIds` was imported from both 3.120.0 source and 2.30.1 NuGet. The standard Debug package lane above passed.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md` — exit 1 because the preserved `66072cc4` story baseline predates six already committed `references/` pointer changes in HEAD. `git diff HEAD -- references` is empty; this continuation changed no pointers.
- `python scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md` — passed against HEAD plus working tree, no `references/` pointer changes.
- No live Aspire distributed smoke was run.
- Review patch verification: `dotnet build tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 --no-restore` — passed, zero warnings/errors.
- Review patch verification: `dotnet test --project tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Debug -p:UseNuGetDeps=false --no-build --no-restore` — passed, 154/154.
- Review patch verification: `dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 --no-restore` — passed, zero warnings/errors.
- Review patch verification: `dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug -p:UseNuGetDeps=false --no-build --no-restore` — passed, 830/830.
- Review patch verification: `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 --no-restore` — passed, zero warnings/errors.
- Review patch verification: `dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false --no-build --no-restore` — passed, 4,026/4,026, none skipped.
- Review patch verification: `TENANTS_BROWSER_BUILD_CONFIGURATION=Debug bash tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh` — passed, EN/FR global panel and paired receipt plus desktop/narrow/forced-colors lanes.

**Review-fix range `362a957d..4f0abcae` patch verification (2026-10-11):**
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 --no-restore` — passed, zero warnings/errors.
- `dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false --no-build --no-restore` — passed, 4,040/4,040, none skipped.
- `dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -class Hexalith.Tenants.UI.Tests.State.TenantAggregateCommandAdmissionGateTests` — passed, 18/18 after the final test strengthening.
- `dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 --no-restore` — passed, zero warnings/errors.
- `dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug -p:UseNuGetDeps=false --no-build --no-restore` — passed, 832/832, none skipped.
- `TENANTS_BROWSER_BUILD_CONFIGURATION=Debug bash tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh` — passed, including rendered EN/FR global correction and paired receipt, narrow and forced-colors lanes.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md` — passed; no `references/` pointer changes.
- `git diff --check` — passed.
