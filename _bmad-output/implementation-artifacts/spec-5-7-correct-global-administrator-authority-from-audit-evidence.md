---
title: 'Correct global administrator authority from audit evidence'
type: 'feature'
created: '2026-10-10'
status: 'done'
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
