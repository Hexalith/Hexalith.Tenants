---
title: 'Resolve five deferred evidence and lookup policies'
type: 'bugfix'
created: '2026-10-28'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '983ba9d51027a287db0efa6860058b56dcb8ff44'
context:
  - '/home/administrator/projects/hexalith/tenants/AGENTS.md'
  - '/home/administrator/projects/hexalith/tenants/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '/home/administrator/projects/hexalith/tenants/references/Hexalith.AI.Tools/hexalith-ux-instructions.md'
  - '/home/administrator/projects/hexalith/tenants/references/Hexalith.AI.Tools/hexalith-git-instructions.md'
---

<frozen-after-approval reason="human-owned intent — approved 2026-10-28">

## Intent

Complete the requested five-item batch: DW-438/457/498/521/600. Review, verify, commit and push to main.

## Boundaries & Constraints

Preserve authorization, redaction, ordinal identity, bounded reads and independent lifecycle evidence. Push owning technical-module commits before separate parent gitlinks; validate messages with owning pinned commitlint.

Preserve historical evidence, unrelated records and release holds. No migrations, deployment, manual package publication, invented approvals, contact or attempt transfer. Source evidence cannot qualify package adoption.

## Approved Decisions

The user approved all recommendations on 2026-10-28:

- DW-521: Exact MessageId first across distinct authorized tenants; one wins, multiple give 409; compatibility only with none.
- DW-600: Any ordinal duplicate within/across pages makes evidence incomplete; preserve case distinction.
- DW-438: Canonical uppercase ULID ApprovedReferences may contain incidental `jwt`/`eyj`; retain other gates. Shape cannot prove arbitrary data nonsecret.
- DW-498: Keep caller redaction; another current owner/global administrator inspects independently, with no attempt transfer.

</frozen-after-approval>

## Code Map

Owning-repository paths. Tenants source: `src/Hexalith.Tenants.UI/`; tests: `tests/Hexalith.Tenants.UI.Tests/`.

- Tenants `State/TenantAudit/TenantAuditSupportSafety.cs`: shared audit/reference gate; NUlid already available. Preserve safe non-ULID literals.
- Tenants `Services/Gateways/GlobalAdministratorsProjectionLoader.cs`: ignores failed TryAdd; preserve cancellation, scope/version/freshness/cursor checks and 50-page cap. Explicitly supersede deduplication in `spec-global-admin-projection-paging.md`.
- Tenants `Services/Gateways/TenantsBffComposition.cs::ComposeTenantCorrectionProjectionsAsync`: redacts nonowners. UI: `Components/Tenants/Audit/CorrectionStartPanel.razor`; copy: `Resources/TenantsResources*.resx`.
- EventStore `src/Hexalith.EventStore/Controllers/CommandStatusController.cs::GetStatus`: mixes exact and correlation matches.
- Builds `Tools/runtime-toolchain-baseline-2026-09-27.json`: immutable superseded packet with pinned SHA-256; preserve bytes/approval/containment.

## Tasks & Acceptance

Approved execution:

- [x] EventStore controller above, `tests/Hexalith.EventStore.Server.Tests/Commands/CommandStatusControllerTests.cs`, `docs/reference/command-api.md`: implement precedence; test tenant ordering, ambiguous indexes, duplicate claims, direct/direct and compatibility collisions, unauthorized scopes.
- [x] Builds `Tools/runtime-toolchain-disposition-2026-09-27.json`, `Tools/validate-runtime-toolchain-disposition.py`, `Tools/test-runtime-toolchain-disposition.py`, `Tools/README.md`: add digest-bound superseded/unqualified SDK-split disposition; distinguish `dapr` support metadata from `Dapr` catalog meaning. Test digest and false-qualification rejection.
- [x] Tenants reference gate and loader above: implement approved policies; qualify via test `State/TenantAuditSupportSafetyTests.cs`, `Services/Gateways/TenantQueryGatewayTests.cs`, `Services/Gateways/GlobalAdministratorsProjectionLoaderTests.cs` and receipt/navigation/copy consumers. Preserve encoding/Unicode/boundary gates and reject other fragments, including canonical ULIDs containing `SECRET`.
- [x] Tenants recovery UI/translations above, tests `Services/Gateways/TenantsBffCompositionTests.cs`, `Components/CorrectionStartPanelTests.cs`, and `docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md`: qualify self-demotion, independent inspection and EN/FR guidance without inferred success.
- [x] Tenants `_bmad-output/implementation-artifacts/deferred-work.md`, `docs/maintenance-backlog.md`, colocated validation report: record outcomes, exact revisions and independent review; preserve unselected records.

Acceptance:

- Given colliding correlation evidence, when exact-message lookup runs, then the selected precedence holds without unauthorized probing.
- Given overlapping pages, when strict completeness runs, then correction cannot treat their aggregate as complete.
- Given authorized references, when classified/rendered/copied, then only the approved canonical exception changes and literals remain exact.
- Given lost authority after self-demotion, when refreshed, then the submitting circuit stays redacted and another inspector cannot inherit its attempt.
- Given historical bytes, when disposition validates, then digest drift or invented qualification fails.

## Implementation Notes

Parent retains the rendered bmad-build orchestration, independent review and final commits/pushes. Implement sequentially from this spec; do not rerender workflows, spawn other agents or commit/push. Load owning guidance before edits; never load skills under references/. Report actual test evidence and remaining blockers. Follow owning repository attributes: Tenants C# uses CRLF; EventStore C# uses LF.

Implementation verification: UI 7,872/7,872; final focused UI 1,309/1,309; EventStore Commands 239/239; new Builds validator 3/3 with negative controls; historical validator suite passed. Both source builds have zero warnings/errors. Initial selector failures remain retained. Final ledger acceptance and Git revisions follow independent review.

Final acceptance: all recorded review corrections are applied and independently rechecked; no concrete issue remains and no review finding is deferred. Post-rebase builds have zero warnings/errors; full UI 7,925/7,925, Server 1,584/1,584, EventStore Commands 239/239, Builds disposition 4/4 and historical/current controls pass. Exactly five records close: 644 done/13 open across 657 IDs; all 652 unselected numbered bodies and remaining maintenance rows are preserved. Owning main commits are published at the declared targets. Published-package adoption and broader runtime/release/assistive-technology holds remain separate.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Route | Evidence / required correction |
| --- | --- | --- | --- |
| B1 | medium | patch | Hidden capture replaces CurrentProjection but leaves rendered currentRole/currentProjectionSnapshot inputs; clear derived current facts while preserving causal baseline/identity. |
| B2 | medium | patch | Pending, retryable and unverified early returns skip current authority capture; reauthorize retained display independently of status completion. |
| B3 | medium | patch | Receipt summary decorates the reference with a colon before canonical classification, so approved JWT/EYJ ULIDs yield null; qualify actual rendered summary/copy. |
| B4 | medium | patch | Configured recovery paths decode before reference classification and inadvertently acquire the audit-only exception; preserve their former strict policy. |
| B5 | low | patch | Dedicated status documentation retains combined ambiguity and replay claims identical semantics; correct both descriptions without changing replay behavior. |
| B6 | medium | patch | Builds CI does not invoke the new disposition controls; add them to its existing Python job. |
| B7 | low | patch | Recovery instruction is outside the focused lifecycle region without a description association; associate the rendered instruction and test the DOM relationship. |
| B8 | medium | patch | GetTenantAuditQueryHandler requires global authority, whereas new copy offers owners state and audit inspection together. Distinguish authorized state inspection from global-only audit inspection and qualify that boundary. |
| B9 | low | patch | Final ledger, maintenance index and command/result report remain parent finalization work; complete them before Git acceptance. No closure or push is claimed yet. |
| E1 | medium | patch | Independently confirms B1: RequiredPreviewInputs still expose cached current facts after hidden capture. |
| E2 | medium | patch | Independently confirms B2: noncompleted status branches bypass capture-based redaction. |
| V1 | medium | patch | Preverified broken-verification gap independently confirms B6: new negative controls are absent from normal Builds CI. |
| V2 | medium | patch | Actual built receipt probe independently confirms B3: ordinary reference yields summary, canonical JWT/EYJ references yield null. |
| P1 | medium | patch | CRLF-equivalent historical bytes fail the new raw digest check, while existing authoritative sha256 normalizes CRLF to LF; match that established digest contract and retain substantive drift rejection. |
| P2 | low | patch | Newly added EventStore control-flow braces use same-line opening braces despite the baseline Allman requirement; correct only newly added blocks. |

Resolution: B1/B2/E1/E2 clear derived facts and refresh authority across status outcomes while preserving baseline identity; B3/V2 qualify real EN/FR receipt summary/clipboard output; B4 preserves strict recovery destinations; B5 corrects status/replay docs; B6/V1 registers negative controls in CI; B7 associates focused lifecycle guidance; B8 preserves the production audit authority boundary; B9 completes exact ledger/report conservation; P1 matches normalized-LF historical binding and P2 corrects new block formatting. Independent recheck is clear. See [commands, retained failures and final evidence](dw-438-457-498-521-600-validation-evidence.md).

## Verification

Run owning Debug/source projects individually, full UI, affected EventStore suites, and new/existing Builds validator suites with negative controls. Separate package-adoption blockers. Check ledger conservation, gitlinks and whitespace; fetch/inspect divergence before main pushes.

## File List

- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/dw-438-457-498-521-600-validation-evidence.md`
- `_bmad-output/implementation-artifacts/evidence/dw-438-457-498-521-600/manifest.json`
- `_bmad-output/implementation-artifacts/evidence/dw-438-457-498-521-600/validation-records.tar.gz`
- `_bmad-output/implementation-artifacts/spec-dw-438-457-498-521-600-deferred-policies.md`
- `docs/maintenance-backlog.md`
- `docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md`
- `references/Hexalith.Builds`
- `references/Hexalith.EventStore`
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceReceipt.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`
- `src/Hexalith.Tenants.UI/Services/Gateways/GlobalAdministratorsProjectionLoader.cs`
- `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditNavigationSafety.cs`
- `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditSupportSafety.cs`
- `tests/Hexalith.Tenants.Server.Tests/Queries/GetTenantAuditQueryHandlerTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/AuditEvidenceReceiptTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/CorrectionStartPanelTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/SupportSafeCopyButtonTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/GlobalAdministratorsProjectionLoaderTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantsBffCompositionTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/State/TenantAuditNavigationSafetyTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/State/TenantAuditSupportSafetyTests.cs`

| Reference | Baseline | Target |
| --- | --- | --- |
| `references/Hexalith.EventStore` | `717a06e16c3afd81b904eaf4de646b74b13262d2` | `be4aba873d09736abb4caacf1fae8ad870f0b3b6` |
| `references/Hexalith.Builds` | `2574bf2f3da5367d8801f0d7661d66155e5d90b0` | `505230a24339070aa2dbf5ac927cd430050bb902` |
