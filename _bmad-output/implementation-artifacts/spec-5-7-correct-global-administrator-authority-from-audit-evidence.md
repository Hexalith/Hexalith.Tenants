---
title: 'Correct global administrator authority from audit evidence'
type: 'feature'
created: '2026-10-10'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 1
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

**Acceptance Criteria:**
- Given complete authorized system evidence, when correction opens, then the fixed command and all ten facts render; stale, partial or unsupported evidence blocks with recovery.
- Given current authority, when Confirm runs, then already-applied and last-admin cases send nothing; an eligible action sends exactly one fixed-scope ULID attempt.
- Given rejection or uncertain delivery, when status/projection refresh, then distinct states retain the original evidence, projection and attempt without false success or duplicate dispatch.
- Given eventful completion, when one fresh complete projection reaches this command's verified sequence, then only the matching postcondition confirms.
- Given a possible corrective audit row, when provenance is checked, then only re-derivable causal association creates both safe receipt links; otherwise audit availability stays truthful.
- Given EN/FR, keyboard, mobile and forced-colors use, when rendered, then safety facts, selectors, focus and live regions work; narrow widths stay read-only.
- Given a persisted singleton projection without ordered sequence, when correction opens, then it stays unavailable until the existing projection is replayed from durable history and the new sequence version is observed.

### Review Findings

Code review 2026-10-11 of `66072cc4..362a957d`. The review diff was 4,351 lines (260,201 bytes; SHA-256 `80873fa0559d09b80c66f547286074324c610c366b9f7f069dcb05e37c6a000b`) and is not archived. Four layers ran: Blind Hunter, Edge Case Hunter, Verification Gap and Acceptance Auditor; none failed. Triage produced 1 decision-needed (resolved to a patch on 2026-10-11), 22 patch, 3 defer and 20 rejected. The gitlink guard passed: `validate-story-gitlinks.py` exited 0 with no `references/` pointer changes in range. Finding 1 was confirmed in a clean checkout pinned to `362a957d`:
- **Reproduction:** a copy of `Mounted_global_panel_notifies_page_when_a_new_proven_pair_becomes_available` makes the proof lookup complete off the dispatcher (`Task.Run`, as real HTTP does) and asserts `tenants-correction-proof-link`. It fails.
- **Fix check:** after the callback is wrapped in `InvokeAsync(() => OnProofAvailable.InvokeAsync())`, it passes. The original synchronous test passes both before and after.

- [ ] [Review][Patch] (low) Stamp command provenance only on `system` global-administrator audit rows [src/Hexalith.Tenants.Server/Projections/TenantAuditReadModel.cs:136] — decision resolved 2026-10-11 (option a, restrict). `CreateEntry` currently sets `CommandCorrelationId` and `AggregateSequence` on every entry, so every `GET /api/tenants/{id}/audit` response and the published `TenantAuditEntry` contract carry command correlation IDs, although only `GlobalAdministratorAuditProofLookup` reads them. Set both fields only for `GlobalAdministratorSet`/`GlobalAdministratorRemoved` entries on tenant `system`; leave them null elsewhere. Update `docs/event-contract-reference.md` to state that scope, and add a server test that a tenant-scoped entry carries neither field. [blind+auditor]
- [ ] [Review][Patch] (high) `OnProofAvailable` is invoked off the Blazor dispatcher [src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor:1746] — this also applies at `:1810`. Both calls `await OnProofAvailable.InvokeAsync().ConfigureAwait(false)` after the protected proof lookup's `ConfigureAwait(false)` continuation. The page's `HandleEventAsync` then calls `StateHasChanged()` off the dispatcher and throws. On Confirm, the throw is swallowed by `SubmitAsync`'s catch: the Confirmed snapshot and `RetainOrReleaseCompletion` never run, and a completed revoke becomes an ambiguous delivery. On Refresh, the exception escapes the click handler and ends the circuit. Fix: `await InvokeAsync(() => OnProofAvailable.InvokeAsync()).ConfigureAwait(false)`, as `OnClose` does at `:2222`. Add a page test whose proof lookup completes off the dispatcher. [blind+edge+auditor+verification]
- [ ] [Review][Patch] (medium) A reload or page change unmounts an open global preview and does not restore focus [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1857] — `RefreshGlobalCorrectionEvidenceForLoadAsync` sets `_globalCorrectionIntent = null` whenever the loaded page has global rows but not the original row. On `/tenants/system/audit`, almost every page has global rows. Review loop 1 rejected a paging finding because "retaining the active panel across paging is required". Keep the existing intent when the row is absent; re-evaluate it only when the row is present. [edge+verification]
- [ ] [Review][Patch] (medium) Every paired-receipt recheck makes an off-page receipt flash "Unavailable" [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1564] — `RefreshPairedGlobalReceiptAsync` clears `_verifiedGlobalProofs` and `_verifiedGlobalRows` and re-resolves the selection before it re-verifies. A paired receipt that is not on the loaded page therefore renders Unavailable for the duration of two or more protected walks, every time it is opened or navigated to, and the scheduled heading focus lands on the Unavailable state. Replace the verified sets atomically in the final dispatcher apply, so a failed recheck removes only the affected links. [blind]
- [ ] [Review][Patch] (medium) Sibling-surface writers drop the hidden proof inputs, and missing inputs are then reported as "audit delayed" with a Refresh that never ends [src/Hexalith.Tenants.UI/State/TenantCommands/TenantAggregateCommandAdmissionGate.cs:462]
  - **Inputs dropped:** `GlobalAdministratorsPage.CreateReconciliation` builds the shared lease reconciliation with `new(...)`, which carries neither `SubmittedAtUtc` nor `CommittedEventSequence`. `TryAdvanceReconciliation` and `TryRetainReconciliation` (`:490`) store that record as is.
  - **Adoption:** the panel adopts any retained lease with the same action and target.
  - **Delayed state:** `QueryCorrectiveProofAsync` (`GlobalAdministratorCorrectionPanel.razor:2175`) returns `(null, false)`, which the panel maps to `AuditDelayed`. The Confirmed branch of `CanRefreshSnapshot` keeps Refresh enabled, but refreshing can never succeed.
  - **Fix part 1:** when the command identity is unchanged, carry forward the existing `SubmittedAtUtc` and `CommittedEventSequence`.
  - **Fix part 2:** report a confirmed attempt that lacks proof inputs as `MissingSupport`, not `AuditDelayed`. [auditor+edge]
- [ ] [Review][Patch] (medium) Every parent re-render resets the revoke preview [src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor:719]
  - **Cause:** with an unchanged intent, `ShouldLiveUpdatePreview(Previewed)` rebuilds the snapshot through `FromIntent`, which sets `RemovePreview = null`. `OnParametersSetAsync` then composes the preview again over HTTP.
  - **Effect:** after any page re-render, Confirm is disabled until that call returns, and the self-removal warning briefly reverts to the generic last-admin copy.
  - **Fix:** keep the existing complete `RemovePreview` while the projection version is unchanged. [edge]
- [ ] [Review][Patch] (medium) A projection that has reached the command's sequence but contradicts the postcondition stays pending forever [src/Hexalith.Tenants.UI/State/TenantAudit/GlobalAdministratorCorrectionSnapshot.cs:674]
  - **Cause:** once `HasReached(CommittedEventSequence, projection.ProjectionVersion)` and `Advanced` both hold, a failing `projectionProvesCorrection` check returns `ProjectionPending`. That state means a later change superseded the command.
  - **Effect:** Refresh can never resolve it, and the lease for the fixed aggregate stays held for the rest of the circuit, so it blocks every global-administrator action.
  - **Fix:** return `UnableToVerify`, reusing the existing per-action copy that the deleted revoke delegation used (still-present / evidence-required). [auditor+edge]
- [ ] [Review][Patch] (low) The authority/admission fact can say "ready" next to a disabled Confirm [src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor:552] — `AuthorityAdmissionLabel` leaves out `(IsRestoreAccessAction || RemovePreview?.IsComplete == true)` and `!_isSubmitting`. `CreateCorrectionDecision` requires both. Review loop 1 recorded this as patched. Derive the label from `CreateCorrectionDecision()`. [blind+edge]
- [ ] [Review][Patch] (low) The in-flight guard misses the `RequestSent` window [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1760] — `HasCommandTracking` requires `CorrelationId` or an ambiguous delivery. While the POST is in flight (`MessageId` set, no correlation yet), a second Start or a load refresh (`:1853`) can replace or unmount the panel, and the operator loses sight of the attempt. Guard on `Snapshot?.MessageId is not null`. Review loop 1 recorded "second intent hides active attempt" as patched. [edge]
- [ ] [Review][Patch] (low) An off-page verified paired receipt offers a Start correction that does nothing [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:2038]
  - **Cause:** `ResolveReceiptSelection` builds a correction intent from a row taken from `_verifiedGlobalRows`, but `OpenGlobalCorrectionAsync` looks the row up only in `_snapshot.Rows`.
  - **Effect:** clicking Start performs two protected reads, then silently focuses a row that does not exist.
  - **Fix:** offer correction only for rows on the loaded page. [edge]
- [ ] [Review][Patch] (low) Dead EN/FR resource keys [src/Hexalith.Tenants.UI/Resources/TenantsResources.resx:4290] — `Tenants.Audit.Receipt.Paired.Open` is unused. `Tenants.Correction.Start.GlobalNotReady` (`:4213`) is orphaned by this diff and survives only in three test-stub localizers. Delete both from EN/FR and the stubs. [blind+auditor+edge]
- [ ] [Review][Patch] (low) The receipt test now asserts an echoed key instead of the localized copy [tests/Hexalith.Tenants.UI.Tests/Components/AuditEvidenceReceiptTests.cs:632] — `.ShouldContain("GlobalAdministratorCommandSupportUnavailable")` passes only because the stub localizer echoes missing keys, so it would not catch a raw key leaking into the UI. Assert the exact EN copy of `Tenants.Correction.Unavailable.GlobalAdministratorCommandSupportUnavailable`. [blind]
- [ ] [Review][Patch] (low) Internal planning label in the public contract document [docs/event-contract-reference.md:127] — "For Story 5.7 …" puts a sprint artifact name into the published contract reference. Name the feature instead (audit-based global-administrator correction). [blind]
- [ ] [Review][Patch] (medium) The tenant-switch part of the paired-receipt test cannot detect a cross-tenant cached receipt [tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs:1607] — the cache is already empty before the switch. Navigate to `tenant.alpha?receiptReference=event-corrective` while the pair is still verified, then assert an unavailable receipt and no paired control. [verification]
- [ ] [Review][Patch] (medium) The route-driven paired refresh is never exercised [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:828] — this also covers `:1078-1081`. Add a page test that navigates a loaded system page to `?receiptReference=event-corrective`, with an off-page corrective row, and asserts a Ready receipt with its back-link. [verification]
- [ ] [Review][Patch] (medium) The restore pre-dispatch recheck has no negative test [src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor:955] — no test drives `original?.Matches(preview)` to false. Add a panel test where the confirm-time read has moved on (count and version changed), and assert no Set request, `UnableToVerify` and a released lease. [verification]
- [ ] [Review][Patch] (medium) The proof link is never asserted for adopted or replacement attempts [tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorCorrectionPanelTests.cs:2552] — this also covers `:1912-1965`. Extend both replacement tests: seed `SubmittedAtUtc`, then assert `AuditAvailable` and `tenants-correction-proof-link`. [verification]
- [ ] [Review][Patch] (medium) No test serializes the provenance fields [tests/Hexalith.Tenants.Contracts.Tests/Queries/QueryDtoSerializationTests.cs:222] — a `[JsonIgnore]` or a non-public accessor would silently turn every proof into `MissingSupport`. Extend the round trip (server camelCase options, then Web defaults) with both fields set. [blind+verification]
- [ ] [Review][Patch] (medium) The new global-authority and projection read during system-page load has no failure-path test [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1047] — render the system page with a global row plus `GlobalAdminFault` (and an authorization fault), then assert that the grid and receipt still render and correction is unavailable. [verification]
- [ ] [Review][Patch] (medium) Three new preview facts are checked only for being present [tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorCorrectionPanelTests.cs:41] — assert the exact values: authority/admission Ready versus Blocked (already applied, last admin, narrow), provenance Current versus Unverified, and original time `2026-06-01 10:00:00 UTC`. [verification]
- [ ] [Review][Patch] (medium) Protection of an in-flight global correction is untested [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1760] — add a page test that starts a second global correction and triggers a reload while an attempt is tracked (including during `RequestSent`), then asserts the original intent and `MessageId` are kept. [verification]
- [ ] [Review][Patch] (medium) The page proof stub ignores the arguments it receives [tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs:3995] — it keys only on `correlationId`, so swapping `proof.OriginalTimestamp` and `submittedAtUtc` at the call site leaves every page test green. Delegate to `GlobalAdministratorAuditProofLookup.FindAsync`, as the panel stub does. [verification+blind+auditor]
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

## Implementation Notes

- Correction is available from authorized `system` audit rows after a complete current platform projection read. Both commands use the fixed global-administrators aggregate and retain one circuit-local attempt across component replacement and reconnect.
- The BFF alone checks audit correlation, aggregate sequence, event, target, scope, and original/submit time boundaries. Public audit rows are redacted. Paired receipts reauthorize and re-query both events on each opening; failed rechecks remove only the affected links. The circuit retains every proven pair for its lifetime, including both independently verified neighbors in a correction chain.
- A hard browser reload creates a new InteractiveServer circuit, so its earlier in-flight attempt and proven-pair registry cannot be restored. A durable protected causal store would be required for that stronger recovery behavior; none was added under the approved no-dependency/no-new-endpoint boundary.
- Matrix coverage: restore/remove and paging in `GlobalAdministratorCorrectionPanelTests`; already-applied, last-admin, and self-removal in the panel/snapshot tests; duplicate/reconnect/ambiguous reconciliation in panel tests; unrelated and post-submit audit negatives in `GlobalAdministratorAuditProofLookupTests` and receipt recheck in `TenantAuditPageTests`. All covering tests ran in the full passing suites below.
- Confirmed corrections may retry delayed audit proof without redispatch. Stored/published progress stays pending until Completed supplies the exact end sequence. A proof found while the panel closes is retained by the circuit registry before renderer application.

## Spec Change Log

- Review loop 1: successive corrections lost one reverse link, so the human clarified that a receipt shows every independently verified pair. The frozen intent now also scopes retry/reconnect recovery to the same Blazor circuit and discloses hard reload limits. KEEP the complete projection/authority/admission checks, fixed-scope command lease, and BFF-only proof validation.
- Review loop 1: legacy global projections have no ordered version. Added an explicit full-history replay rollout gate and fail-closed acceptance. This avoids treating a legacy null version, wall time, or an unrelated audit row as a causal baseline. KEEP the new handler's ordered version and exact command-sequence postcondition.
- Review loop 1: prior browser validation did not render the new global correction controls. Added real EN/FR panel and paired receipt fixture coverage to the task and verification plan. This avoids claiming accessibility coverage from unrelated tenant correction fixtures. KEEP existing browser lanes and bUnit behavior tests.

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

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1` — passed, zero warnings/errors.
- `dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false --no-build --no-restore` — passed, 4,015/4,015, none skipped.
- `dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1` — passed, zero warnings/errors.
- `dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug -p:UseNuGetDeps=false --no-build --no-restore` — passed, 829/829, none skipped.
- `TENANTS_BROWSER_BUILD_CONFIGURATION=Debug bash tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh` — passed, EN/FR desktop/narrow/forced-colors lanes including rendered global panel and paired receipt fixtures.
- `git diff --check` — passed.
