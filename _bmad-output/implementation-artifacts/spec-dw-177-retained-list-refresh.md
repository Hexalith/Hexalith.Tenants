---
title: 'DW-177: Retain tenant rows during same-scope soft refresh'
type: 'bugfix'
created: '2026-10-20'
status: 'done'
route: 'oneshot'
review_loop_iteration: 1
baseline_commit: '3ae9db014dfe52bdee4f6731465cc2a7b65701b7'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** DW-177 is the next dependency-free actionable Tenants item after DW-160. A same-scope soft list refresh replaces the rendered tenant rows and pager with an empty loading snapshot, interrupting inspection while the gateway read is pending.

**Approach:** Keep the current caller's already displayed rows and paging context mounted during soft refresh, alongside the existing localized loading message. Treat the transient snapshot as Loading with unknown freshness so paging, create and audit actions fail closed. Preserve initial/hard-load clearing, caller isolation, request validators and last-confirmed gateway evidence, and apply the authoritative completion through existing generation/disposal guards. Close only DW-177 after focused and full UI checks and independent review, then commit and push to main as requested.

</frozen-after-approval>

## Implementation Notes

- Clean main at entry revision above. This is freeform maintenance without a sprint story key. The user authorizes completion and pushing main. No intent decision, migration, new public API or dependency change is needed; the small local implementation uses the oneshot route.
- Read baseline/Git/UX instructions, build/test settings, architecture AD-5/7/8/10/11 and the maintenance entry. Independent investigation confirms the unconditional Loading replacement in TenantsWorkspace.LoadAsync and existing explicit caller binding through TenantListSnapshot.CallerScope / TenantQueryGateway.AuditCallerScope.
- Change only TenantsWorkspace.razor and a focused partial TenantsWorkspaceTests file, plus this spec, evidence, ledger and maintenance index. Retain the current snapshot only for reuseETag with nonempty rows and a nonblank matching caller scope; never resurrect _lastConfirmedSnapshot after a hard load or denial. Preserve search ordering and metadata, display the existing loading surface, and guard both rendered pager buttons and queued callbacks while Loading. Keep the gateway previous argument unchanged.
- Exercise held reads through actual Fluent controls: ordinary and search rows/order, successful and denied/error completion, repeated refresh and late results, initial/hard load, caller mismatch/missing scope, validators, action gates and pager recovery. Use existing production EN/FR resources and deterministic TaskCompletionSource barriers. Run Debug source build, focused class and full UI, scoped formatting and own-spec gitlink checks. Review the complete worktree with the required independent blind reviewer before closure.
- Aspire baseline: isolated Tenants AppHost starts; final describe reports 23 Running resources and security Healthy. A bounded Error sample contains existing Memories/EventStore 500/authentication retry failures. No clean end-to-end topology claim is made; the started AppHost is stopped for cleanup and the pre-existing Folders AppHost is preserved.
- Initial Debug source build with the first 18 new cases succeeds with zero warnings/errors. Running those cases against original production exits 2: 11 fail on missing grid/pager, seven pass, zero skip. The production fix preserves the current snapshot only with matching nonblank caller evidence, changes its transient kind/freshness, retains mounting and guards both pager enablement and callbacks.
- Four additional cases require matching snapshot caller evidence and pin initial-load behavior. The hard-load case now actually resets an active search scope. Stale/degraded fixtures carry their corresponding freshness rather than a contradictory Current page. Final acceptance is pending; no closure is claimed yet.
- First corrected acceptance passes all 22 dedicated cases and the 128-case workspace class with zero failures/skips. Debug source build has zero warnings/errors. The first scoped formatting check reports only the new test file's 390 LF line endings; apply the configured CRLF formatting before final qualification. Independent blind review is running.
- Independent blind review returns six findings. Three require small production corrections: preserve Stale/Degraded warnings and zero-filter-result mounting while Loading, and clear retained loading evidence as soon as an authentication notification starts (including unresolved replacement authority). A private nullable original-kind field preserves the warning without changing the public snapshot. The remaining findings strengthen delayed terminal-response assertions, repeated-refresh prior/ETag evidence, native grid identity and rendered rows/create/audit controls. Six new cases cover filtered EN/FR warnings and resolved/pending authentication transitions. No new public API or intent gap is introduced.
- The first full UI run on pre-review production executes 6604 cases: 6603 pass, one unrelated GlobalAdministratorsPageTests.OrdinaryRefreshAndProjectionNotificationShareCallerCancellationWithoutCancelingSiblingAuthority case fails its PendingTimerCount=0 assertion (observed 1), zero skips. Preserve that result and recheck the exact case after rebuilding the reviewed source; final full acceptance remains required.

- Final independent recheck accepts every correction, including Ready/zero-filter pager retention. The isolated entry-revision source build with actual dependency roots has zero warnings/errors; byte-identical final overlays pass focused 30/30 and full UI 6612/6612, zero failures/skips. The earlier timer assertion passes its exact one-case recheck. Reviewed scoped formatting and own-spec gitlink checks pass. Current-main build remains explicitly blocked by the preserved external NativeSourceProof dependency; it is not reported as a clean current-main build.
- Gated ledger integrity closes only DW-177: 656 IDs remain, 400 done/256 open and 231 exact-covering index rows. Every unrelated record body, archive/sprint, historical fields and dependency pointer remains preserved. Pinned commitlint 21.2.3 validates the exact full commit message with zero problems/warnings. See [complete evidence](dw-177-validation-evidence.md).


## Review Triage Log

- Medium / patch: zero-match page-local filters hid a previously mounted Stale/Degraded grid/pager during Loading. Retain its original surface kind for the display decision; four EN/FR held-read cases pin empty visible rows, warning, mounting and pager.
- Medium / patch: caller evidence was checked only at refresh admission; a later authentication event left retained loading rows visible. Clear them before awaiting replacement authentication, including pending identity; two held-read cases cover resolved and unresolved transitions.
- Medium / patch: Loading suppressed the existing Stale/Degraded warning and reason alongside retained rows. Render the original warning beside loading and clear the private original-kind field only on current completion, hard load or authority invalidation.
- Medium / patch: terminal outcome tests skipped the pending retained interval. Hold each Error/Unauthorized/Empty second response, assert grid/loading first, release it and then verify a subsequent refresh cannot resurrect old rows.
- Medium / patch: repeated refresh lacked gateway evidence assertions. Capture each request and assert both pending refreshes receive the original confirmed object and exact ETag.
- Low / patch: wrapper/parameter checks did not qualify rendered action wiring or native grid continuity. Add native Fluent grid identity, rendered row order/detail hrefs, actual disabled create submit and unavailable audit button assertions. Browser DOM focus remains outside this component evidence.
- Review recheck accepts five findings and identifies the remaining Ready/zero-filter-match pager case. Separate logical history from button enablement so the mounted pager survives independently of grid visibility; extend filtered coverage to Ready.
- Concurrent external commit 75627d267296d26a9f4fee05fa0495deb110fc11 advanced main after entry, adding six unrelated authority source/test files without changing declared pointers. The reviewed ordinary Debug source build exits 1 with CS0246 at TenantReadAuthorityResult.cs:12: NativeSourceProof is absent from the declared EventStore source checkout f6b545cd02632f662136d0b4b8e6952baa7afec3. Preserve the external commit and dependency pointers. Use an isolated entry-revision checkout with exactly the DW-177 production/test overlay for focused/full qualification, and record the separate current-main build blocker; do not weaken its build gate.
- Initial isolated validation through symlinked project paths fails with 329 analyzer/metadata errors because compiler paths resolve to the real dependency locations while configuration is discovered through symlink aliases. Retry with explicit absolute Hexalith dependency-root properties, preserving the same source project references, analyzers and warning gates. No suppression or dependency update is applied.
