---
title: 'DW-181: Qualify tenant-detail cancellation lifetime'
type: 'chore'
created: '2026-10-21'
status: 'done'
route: 'oneshot'
review_loop_iteration: 1
baseline_commit: '8ca1db8d5662d94272811951803974b8ed1c3e0d'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** DW-181 records a missing runtime regression for tenant-detail load cancellation. The page already assigns cancellation-source disposal to each completed load, but no focused test establishes that a superseded or disposed page keeps an outstanding read's token usable until that read finishes.

**Approach:** Drive real rendered detail loads through the existing refresh/parameter callbacks and page disposal with explicit completion gates. Prove cancellation occurs immediately, token wait-handle and registration access remain usable while held, disposal occurs after completion, and obsolete results cannot replace the current detail. Preserve existing runtime behavior, dependencies and unrelated deferred work. Close only DW-181 after focused/full UI checks and independent review; commit and push on main as requested.

</frozen-after-approval>

## Implementation Notes

- Clean main entry and full canonical baseline are recorded above; this freeform maintenance has no sprint story key. No unresolved user-visible choice, public API, migration or unrequested external action is needed. Small test/evidence change uses the oneshot route.
- Read root AGENTS, permitted LLM/Git/UX baseline, editor/attributes/build/test settings, architecture AD-7/8/11, ledger and maintenance index. Independent investigation confirms a coverage gap without an evident production defect. DW-180 stays uncertain and open.
- Code map: TenantDetailPage.razor BeginLoad / ReplaceActiveCancellation / RetireCancellationCore cancel superseded reads; PendingOperation.Dispose invokes CompleteCancellation after completion. DisposeAsync cancels active reads without owning their disposal. TenantDetailSurfaceTests.cs supplies RegisterServices, Detail, ReadyWithSafeConfiguration and real EditTenantMetadataFlow refresh callbacks; the fixture is already partial. TenantsWorkspaceTests.cs contains the equivalent held-read test.
- Given a held read, when replaced or the page is disposed, then cancellation is observed before release, wait-handle and registration access remain valid until completion, and wait-handle access throws ObjectDisposedException after the owning callback completes. Given a replacement completes first, when the held obsolete read completes, then its result does not overwrite the replacement.
- Baseline Debug source build passes with zero warnings/errors. Start isolated Aspire baseline and inspect resource state/telemetry before test edits; retain exact baseline outcome in validation evidence and stop the started app afterwards.
- Added three rendered cases in `TenantDetailSurfaceTests.CancellationLifetime.cs`, reusing the existing partial fixture and public metadata refresh callback. Explicit gates hold the gateway while refresh/route/unmount cancellation occurs; WaitHandle detects premature disposal, and post-callback WaitHandle rejection detects missing completion cleanup. The disposal case uses actual bUnit `DisposeComponentsAsync`, checks no subsequent renders and drains the dispatcher. Every gate releases in finally.
- Baseline and test Debug source builds pass with zero warnings/errors; initial focused 3/3 pass with zero failures/skips. Aspire begins with waiting resources but settles to all 23 Running/Healthy; the app started by this session stops successfully.
- Six root dependency checkouts advanced externally after clean entry. Preserve those changes without staging or reverting them. Qualification uses available source checkouts, not a claim about the original declared dependency pins; no dependency update command was run by this session. Record final commit range and exact external state separately in validation evidence.
- Concurrent external sync commit `ec10b2575c836d650a68ffcbe14a16ccc9257528` captured the initial test/spec and six dependency advances during qualification. Preserve it. Its source changes are included in this review; this session owns only subsequent completion corrections and stages no dependency paths. The true entry baseline remains unchanged above.

## File List

- `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.CancellationLifetime.cs` — rendered cancellation lifetime regressions.
- `_bmad-output/implementation-artifacts/spec-dw-181-detail-cancellation-lifetime.md` — workflow state and inherited-change attribution.
- `_bmad-output/implementation-artifacts/dw-181-validation-evidence.md` — exact checks, controls, review and limits.
- `_bmad-output/implementation-artifacts/deferred-work.md` — reconcile only DW-181 while preserving its history.
- `docs/maintenance-backlog.md` — remove only the completed DW-181 action row.
- `references/Hexalith.Builds`
- `references/Hexalith.EventStore`
- `references/Hexalith.FrontComposer`
- `references/Hexalith.McpCli`
- `references/Hexalith.Memories`
- `references/Hexalith.Platform`

## Completion Notes List

The external sync above captured these moves and the initial task sources. Declare the actual inherited targets for honest range validation; this does not attribute dependency updates to DW-181 or establish broader dependency/release qualification.

| Path | Entry pointer | Inherited target |
| --- | --- | --- |
| `references/Hexalith.Builds` | `fff4a212180d8cd1c5c0759655a9bc4fb6963c45` | `17d0c6ffec5fb3c8d8058e67cb85644460cb5256` |
| `references/Hexalith.EventStore` | `a07265f00994a3ad093184cec0f329d11184c3e9` | `ae917d3e4dbab20b8982c5222a13611c3e3faf35` |
| `references/Hexalith.FrontComposer` | `1a1cd1e89f59b05ed2621a31f157f9d940356ef8` | `247410b8b35050abf3f06ec72d6bcf3f1552b150` |
| `references/Hexalith.McpCli` | `9354dc08176744c75c2ddb61d924c0948b373b98` | `39f75e8c8e2bec0824e8f0b93f72bd94c9a73789` |
| `references/Hexalith.Memories` | `35df7e2080c369003cacac12f5b44117d7f3ed35` | `7d1e21f91ac58e427e1440ba34d1c8c141d7e98c` |
| `references/Hexalith.Platform` | `642b5bdbdf33ec1588eeef3b9a7e9f1a02aebc40` | `7b5548f0d308d27af9d5fda5b68c6b959c053185` |

## Review Triage Log

- Low / patch: unmount and dispatcher-drain waits were unbounded, so a teardown regression could hang before finally. Bound every harness teardown/dispatcher wait and release all held tasks in finally.
- Medium / patch: synchronous successor completion did not establish immediate predecessor cancellation. Hold the successor separately and observe cancellation while both owning tasks remain incomplete.
- Low / patch: route response identity was synthesized from case state rather than the query. Validate each actual request tenant and derive ordinary response identity from that request.
- Medium / patch: only metadata refresh owned held reads. Add parameter-load variants through public SetParametersAsync, sharing one captured token across held detail/member reads; release detail while members remain held and require token usability until the owning callback completes.
- Medium / patch: predecessor completion could detach a still-active successor without the original tests noticing. Keep the successor pending through predecessor completion, start another load, and require successor cancellation, usable token lifetime and eventual disposal while preserving the final projection marker.
- Low / patch (correction recheck): the fourth load could hide an incorrectly applied original obsolete snapshot. Assert no rendered configuration table contains the original superseded marker immediately after the first owner's completion, before starting that fourth load. The reviewer accepts the original five corrections.

- The accepted review expands the theory from three to six cases, covering both public metadata refresh and parameter lifecycle ownership, independently held successors and shared member reads. Correction recheck accepts all findings after restoring the first obsolete-result assertion before the fourth load. A local variable-name collision in that assertion is corrected without behavior changes.
- Final validation encountered a full disk: project and direct test entry points abort with exit 134/empty output. Remove only task-owned temporary clone/binaries/copied docs/superseded TRX files, retaining logs and recorded counters. The retry passes focused 6/6 and the pre-final-assert full run passes 6624/6624. Expanded early-disposal control rejects all six cases once runner entry symlinks are replaced with actual local files; the first symlink-entry attempt is invalid evidence. Initial missing-cleanup control rejects all three initial cases. No unexecuted control is claimed; exact failures/limits remain in validation evidence.
- Final corrected Debug source build passes with zero warnings/errors. Final focused 6/6 and full UI 6624/6624 pass with zero failures/skips, including the restored pre-fourth-load assertion; scoped formatting and unchanged own-spec gitlink audit pass. Independent recheck accepts all six review corrections with no remaining issue. Only DW-181 closes: 402 done/254 open across all 656 IDs and 229 exact-covering index rows; unrelated record bodies, archive and sprint remain preserved. The frozen intent is byte-identical to the inherited initial spec.
