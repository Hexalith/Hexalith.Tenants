# Deferred Work

**DW-647 closure (2026-10-17):** Held removal admission now explains current freshness, exact confirmation and authority refusals accurately in EN/FR, including retry announcements, changes during cleanup and explicit reconfirmation after safety recovers. Independent review/rechecks, 50 new cases, zero-warning/error Debug source build, focused 430 and full UI 6124 pass with zero failures/skips. Only DW-647 closes: 647 IDs remain, 346 done/301 open across 269 exact-covering maintenance rows. Original findings/pre-closure fields, all unrelated records, prior evidence, archive/sprint, dependencies and release holds are preserved. See [exact commands, initial failures and limits](dw-647-validation-evidence.md). Release remains not ready.

**DW-646 closure (2026-10-17):** Only DW-646 closes after failed-removal identity retention across refused/faulted retry admission, cleanup, disposal and real parent cancel/reopen passes independent review/correction rechecks. The final Debug source build has zero warnings/errors; focused 380 and full UI 6074 pass with zero failures/skips, including 32 new cases. All 647 IDs remain:345 done/302 open across 270 exact-covering maintenance rows. Original findings/pre-closure evidence, DW-647, every unrelated ledger body, prior evidence, archive/sprint, dependency pointers and release holds are preserved. See [complete validation and initial failures](dw-646-validation-evidence.md). Release remains not ready.

Reconciliation date: 2026-10-12. Verified against Tenants HEAD `7a2a126f` and the existing checked-out root dependencies. Scope: all 152 entries marked open and all 287 unnumbered findings/restatements (including 254 `source_spec` records). Unnumbered records now have DW-357 through DW-643; their original multiline text and resolution notes are preserved as a JSON string in the single-line `legacy-detail` field. Existing DW identifiers, findings, decisions, closed stubs and archive content are preserved. No BMAD artifact was moved.

This ledger remains the source of truth; [the maintenance backlog](../../docs/maintenance-backlog.md) is its concise remaining-work index. `status` remains `open` or `done <ISO date>` for existing readers. Supplementary single-line fields record `disposition`, verification date/evidence, evidence kind, priority, owner, dependencies, next action and exact duplicate relationships. Accepted limitations and uncertain findings remain open; acceptance records an existing intentional boundary, not completion. `verification-files` resolves abbreviated filenames where unambiguous. `legacy-detail` on migrated records decodes to the original text, including earlier resolutions.

Evidence distinguishes inspected source/tests/artifacts from executed results. No .NET tests or browser sessions were rerun for this documentation task. Read-only story-gitlink validators and existing hosted CI results were inspected: [current Tenants CI](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/38269521776) has a successful build/test job and an advisory Aspire lane with 183/184 passed, zero skipped, and the generated API TLS test failing; [current Story Guards](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/38269520326) still times out the shipped Chromium profile. These results do not establish an entirely green runtime environment.

Original reconciliation counts for the 439 records: 86 done, 265 actionable, 15 blocked, 26 awaiting-decision, 9 accepted-limitation, 38 uncertain (353 open). After validated B1 closure of DW-550/DW-567/DW-598: 89 done, 262 actionable, 15 blocked, 26 awaiting-decision, 9 accepted-limitation, 38 uncertain; 350 open. Earlier closed records are outside these reconciliation counts. The three B1 closures retain their historical findings here; no archive move or unrelated disposition change ran.

After validated B2 closure of DW-142/DW-580/DW-351/DW-366: 93 done, 258 actionable, 15 blocked, 26 awaiting-decision, 9 accepted-limitation, 38 uncertain; 346 open. These four closures retain pre-fix verification and action history in their records; the archive and every unrelated record remain unchanged. Exact commands and results are in [B2 remediation evidence](release-readiness.md#b2-remediation-evidence-2026-10-12).

After validated B3 closure of DW-222/DW-286/DW-300 (2026-10-13): 96 done, 255 actionable, 15 blocked, 26 awaiting-decision, 9 accepted-limitation, 38 uncertain; 343 open. All 643 unique ledger IDs remain: 300 done and 343 open. The ten B1/B2/B3 closures retain their historical findings in the active ledger; the archive remains at its historical 290 records. Only these three B3 records change in this pass. Exact commands, results, review and limits are in [B3 remediation evidence](release-readiness.md#b3-remediation-evidence-2026-10-13).

After validated B4 closure of DW-199/DW-288/DW-287/DW-458/DW-471/DW-472/DW-500/DW-421 (2026-10-13): 104 reconciled done, 247 actionable, 15 blocked, 26 awaiting-decision, nine accepted limitations and 38 uncertain; 335 open. All 643 unique ledger IDs remain: 308 done and 335 open. Historical findings and pre-fix verification/actions are retained in these eight records; B1–B3 evidence, unrelated records, archive and sprint tracking are preserved. Exact commands, results, initial failures, review and limits are in [B4 remediation evidence](release-readiness.md#b4-remediation-evidence-2026-10-13).

After validated B5 closure of DW-315/DW-441 (2026-10-13): 106 reconciled done, 245 actionable, 15 blocked, 26 awaiting-decision, nine accepted limitations and 38 uncertain; 333 open. All 643 unique IDs remain: 310 done and 333 open. The two historical findings, legacy text and pre-B5 verification/actions remain in their records. B1–B4 evidence, unrelated records, archive and sprint are preserved. The current baseline archive contains 308 IDs and is unchanged; these two closures remain in the active ledger. Exact commands, initial failures, review and limits are in [B5 remediation evidence](release-readiness.md#b5-remediation-evidence-2026-10-13).

After validated B6 closure of DW-626/DW-628 (2026-10-13): 108 done, 243 actionable, 15 blocked, 26 awaiting-decision, 9 accepted-limitation, 38 uncertain; 331 open. All 643 IDs remain: 312 done / 331 open, indexed by 295 maintenance rows. Original findings and pre-B6 verification/actions remain in the two records; B1–B5 evidence, B7, unresolved decisions, archive and sprint tracking are preserved. See [B6 remediation evidence](release-readiness.md#b6-remediation-evidence-2026-10-13) and [exact validation, initial failures and provenance limits](b6-validation-evidence.md).

After validated B7 closure of DW-195/DW-206/DW-235/DW-264 (2026-10-14): 112 reconciled done, 239 actionable, 15 blocked, 26 awaiting-decision, 9 accepted-limitation and 38 uncertain; 327 open. All 643 IDs remain: 316 done / 327 open, indexed by 291 maintenance rows. The four original findings, August decisions and pre-B7 verification/actions remain in place; B1–B6 evidence, unrelated records, archive, sprint tracking, pointers and unresolved decisions are preserved. See [B7 readiness](release-readiness.md#b7-remediation-evidence-2026-10-14) and [exact commands, initial failures, review and limits](b7-validation-evidence.md). B7 closure does not establish full release readiness.

After validated B8 closure of DW-499/DW-502 (2026-10-14): 114 reconciled done, 237 actionable, 15 blocked, 26 awaiting-decision, 9 accepted-limitation and 38 uncertain; 325 open. All 643 IDs remain: 318 done / 325 open, indexed by 289 maintenance rows. Original findings, legacy detail and pre-B8 verification/actions are retained; B1–B7 evidence, unrelated records, archive, sprint tracking and unresolved decisions are preserved. External commits/pointer movements and historical story-gitlink provenance blockers remain visible without assistant Git/dependency mutation. See [B8 evidence](b8-validation-evidence.md) and [exact commands/results/initial failures/limits](b8-command-results.json). Next selected bundle: DW-517; B8 closure does not establish release readiness.

DW-517 repair/qualification update (2026-10-14): the content-blind exclusions are repaired in EventStore and independent review/new ingestion controls pass, but owning security qualification still reports nine preserved historical findings without a verified retirement/recognition disposition. DW-517 remains open/actionable; no closure or unrelated disposition change ran. Recomputed counts remain 114 reconciled done /237 actionable /15 blocked /26 awaiting-decision /9 accepted-limitation /38 uncertain; 643 unique IDs,318 done/325 open,289 index rows. [Exact DW-517 validation, policy alternatives and release limits](dw-517-validation-evidence.md) retain failures, B1–B8 history, external movements and historical provenance blockers.

After validated DW-517 closure (2026-10-14):115 reconciled done,236 actionable,15 blocked,26 awaiting-decision,9 accepted-limitation and38 uncertain;324 open. All643 IDs remain:319 done/324 open, indexed by288 maintenance rows. Only DW-517 closes; original findings, all prior evidence, B1–B8, external movements, archive/sprint and unresolved decisions remain. See [final DW-517 evidence](dw-517-validation-evidence.md). Release remains not ready; next selected bundle is DW-105/DW-108, shared DOM-safe identity encoding.

After validated DW-105/DW-108 closure (2026-10-14):117 reconciled done,234 actionable,15 blocked,26 awaiting-decision,nine accepted limitations and38 uncertain;322 open. All643 IDs remain:321 done/322 open, with287 exact-covering maintenance rows. Only these two records close; original findings/pre-bundle evidence, B1–B8, DW-517, external work, archive/sprint and decisions remain. See [DOM identity evidence](dw-105-108-validation-evidence.md). Release remains not ready; next selected bundle is DW-112.

DW-112 closure update — 2026-10-15: only DW-112 closes after actual Fluent EN/FR labels, computed native names, held loading/status transitions and focus/selection preservation pass independent review and final Debug/maintained Release UI4987 each (zero failures/skips). All643 IDs remain:322 done/321 open,286 index rows; reconciled118 done/233 actionable/15 blocked/26 awaiting-decision/nine accepted limitations/38 uncertain. Original findings, B1–B8, DW-517, DW-105/DW-108, external work/pointers, archive/sprint, decisions and historical blockers are preserved. See [complete evidence](dw-112-validation-evidence.md). Next Tenants-owned bundle is DW-150 alone; DW-121 remains Builds-owned. Release remains not ready.

DW-150 closure update — 2026-10-15: only DW-150 closes after combined member-read/detail-refresh coordination passes independent review, seventeen actual Fluent held-read cases, final focused332 and full UI5004 in Debug/maintained Release (zero failures/skips), and both unchanged Chromium lanes. All643 IDs remain:323 done/320 open,285 index rows; reconciled119 done/232 actionable/15 blocked/26 awaiting-decision/nine accepted limitations/38 uncertain. Original findings, all prior evidence/B1–B8/DW-517/DW-105/DW-108/DW-112, inherited/external work and pointers, archive/sprint, decisions and release/provenance blockers remain. See [complete evidence](dw-150-validation-evidence.md). Next selected bundle is DW-164/DW-183; DW-121 remains Builds-owned. Release remains not ready.

DW-164/DW-183 closure update — 2026-10-15: only these duplicate records close after completion-driven latest-route setup, route/caller callback binding and lease cleanup pass independent review,32 actual Fluent cases, final focused364/full UI5036 in Debug/maintained Release (zero failures/skips), and both unchanged Chromium lanes. All643 IDs remain:325 done/318 open,284 index rows; reconciled121 done/230 actionable/15 blocked/26 awaiting-decision/nine accepted limitations/38 uncertain. Original findings, B1–B8, DW-517, DW-105/DW-108, DW-112, DW-150, inherited/external work/pointers, archive/sprint, gates and unresolved decisions remain preserved. See [complete evidence](dw-164-validation-evidence.md). Next separate bundle: DW-166/DW-173; their timeout/cancellation policy was not changed here. Release remains not ready.

DW-166/DW-173 closure update — 2026-10-15: only these two records close after shared finite authorization/subscription policy, replaceable caller/route cancellation, exact scoped setup/cleanup ownership and three-failure recovery pass independent review, focused/full UI and maintained Chromium gates. All643 IDs remain:327 done/316 open,282 index rows; reconciled123 done/228 actionable/15 blocked/26 awaiting-decision/nine accepted limitations/38 uncertain. DW-176/DW-184 receive necessary consumer wiring but remain open for separate acceptance reconciliation. Prior findings/evidence, B1–B8, DW-517, DW-105/DW-108, DW-112, DW-150, DW-164/DW-183, inherited work and externally moved commits/pointers remain preserved; historical provenance blockers are retained without resets or false attribution. See [complete evidence](dw-166-validation-evidence.md). Next selected bundle: DW-176/DW-184 consumer acceptance reconciliation; release remains not ready.

DW-176/DW-184 closure update — 2026-10-15: only these two records close after separately proving workspace consumer acceptance against the shared DW-173 policy. Current-caller entry/audit binding, active create-view/actual alias-route invalidation, pending/expired explicit authentication recovery, final admission release, dispatched tracking and late/disposal safety pass independent review, focused849/full UI5134 in both configurations and both unchanged Chromium lanes. All643 IDs remain:329 done/314 open,280 index rows; reconciled125 done/226 actionable/15 blocked/26 awaiting-decision/nine accepted limitations/38 uncertain. Prior findings, decisions, B1–B8 and all earlier bundle evidence, inherited/external work, archive/sprint, gates and historical provenance blockers remain preserved. See [complete consumer evidence](dw-176-validation-evidence.md). Next selected bundle: DW-245/DW-265/DW-290, queued membership projection-refresh intent; DW-121 remains Builds-owned. Release remains not ready.

DW-245/DW-265/DW-290 closure update — 2026-10-16: only these three duplicate queued membership projection-refresh records close. Both flows retain OR-combined intent in generation-tagged batches, serialize replay and idle handoff, consume explicit pending work across failure and preserve exact retained proof/identity/admission/disposal semantics. Independent review/recheck,62 new actual Fluent cases, focused462/full UI5196 in Debug/maintained Release and both unchanged Chromium lanes pass. All643 IDs remain:332 done/311 open,279 index rows; reconciled128 done/223 actionable/15 blocked/26 awaiting-decision/nine accepted limitations/38 uncertain. DW-248/DW-266/DW-267, all earlier B1–B8/DW evidence, unrelated findings/decisions, inherited/external work and dependency pointers, archive/sprint, gates and historical provenance/release blockers remain preserved. See [complete evidence](dw-245-validation-evidence.md). Next selected bundle: DW-247 bounded membership ProjectionPending recovery under its settled August25/26 decision; DW-323 remains separately scoped and DW-121 remains Builds-owned. Release remains not ready.

After validated DW-247 closure (2026-10-16): 129 reconciled done,222 actionable,15 blocked,26 awaiting-decision,nine accepted limitations and38 uncertain;310 open. All 643 IDs remain:333 done/310 open across278 exact-covering maintenance rows. One configured retained membership projection deadline and exact late reconciliation pass independent review/recheck,172 dedicated cases, focused 657/full UI 5368 in Debug/maintained Release and both unchanged Chromium lanes. Initial failures and broad release/provenance blockers remain in [evidence](dw-247-validation-evidence.md). Only DW-247 changes; prior history,decisions,B1–B8/all earlier bundles,DW-248/DW-266/DW-267/DW-323,archive and sprint are preserved. Next selected bundle:DW-234/DW-251/DW-316 shared failure/status localization. Release remains not ready.

After verified DW-234/DW-251/DW-316 command-localization closure (2026-10-16): 132 reconciled done, 219 actionable, 15 blocked, 26 awaiting-decision, nine accepted limitations and38 uncertain; 307 open. All 643 IDs remain: 336 done/307 open across 275 exact-covering maintenance rows. Whole-string gateway/default/unavailable/status and snapshot EN/FR copy passes final focused 1,825/full UI 5,532 in both configurations and unchanged Chromium lanes. Original findings and prior evidence, archive/sprint, dependencies, decisions, historical blockers and separately scoped DW-323 remain preserved. See [complete evidence](dw-234-251-316-validation-evidence.md). Independent workflow review is recorded in the owning spec; release remains not ready.

After accepted DW-337 tracked-dispatch closure (2026-10-16): 133 reconciled done, 218 actionable, 15 blocked, 26 awaiting-decision, nine accepted limitations and 38 uncertain; 306 open. All 643 IDs remain: 337 done/306 open across 274 exact-covering maintenance rows. Six tracked operations share the existing global transport classifier, with exact same-ID retries, caller cancellation, permanent mapping and shipped EN/FR copy verified by 768 focused/5954 full UI tests in both configurations and both unchanged Chromium lanes. Only DW-337 changes; original findings/decisions, all earlier closures, archive/sprint, dependencies, unrelated records and release holds remain preserved. See [complete evidence](dw-337-validation-evidence.md) and [workflow review](spec-dw-337-tracked-dispatch-ambiguity.md). Release remains not ready.

After verified DW-285 acceptance reconciliation (2026-10-16): 134 reconciled done, 217 actionable, 15 blocked, 26 awaiting-decision, nine accepted limitations and 38 uncertain; 305 open. All 643 IDs remain: 338 done/305 open across 273 exact-covering maintenance rows. The existing runtime reentry fix is qualified by 12 new held-dispatch cases, independent review/recheck and final Debug focused195/full UI5967, zero failures/skips. Only DW-285 closes; original findings, earlier evidence/decisions, DW-470, archive/sprint, dependencies and release holds remain preserved. See [complete evidence](dw-285-validation-evidence.md). Release remains not ready.

After verified DW-470 acceptance reconciliation (2026-10-16): 135 reconciled done, 216 actionable, 15 blocked, 26 awaiting-decision, nine accepted limitations and 38 uncertain; 304 open. All 643 IDs remain: 339 done/304 open across 272 exact-covering maintenance rows. Existing runtime key propagation is qualified by 12 new actual Fluent EN/FR keyed-only failure cases, independent review/recheck and final Debug focused207/full UI5979, zero failures/skips. Only DW-470 closes; original findings and pre-closure verification/actions, all earlier evidence/decisions, archive/sprint, dependencies and release holds remain preserved. See [complete evidence](dw-470-validation-evidence.md). Release remains not ready.

After validated DW-331 closure (2026-10-16): 136 reconciled done, 215 actionable, 15 blocked, 26 awaiting-decision, nine accepted limitations and 38 uncertain; 303 open. All 643 IDs remain: 340 done/303 open across 271 exact-covering maintenance rows. The pure removal evaluator rejects contradictory complete-population scope with the existing IncompletePopulation outcome; nine new behavioral cases preserve grant independence and refusal precedence. Independent review/recheck, final Debug focused33/full UI5988 pass with zero failures/skips. One intermittent unchanged correction-test failure and its passing isolated/full rechecks are preserved in [complete evidence](dw-331-validation-evidence.md). Only DW-331 closes; original findings/pre-closure verification/actions, prior evidence/decisions, archive/sprint, dependencies and release holds remain preserved. Release remains not ready.

After validated DW-468 closure (2026-10-16): 137 reconciled done, 215 actionable, 15 blocked, 26 awaiting-decision, nine accepted limitations and 37 uncertain; 302 open. All 643 IDs remain: 341 done/302 open across 270 exact-covering maintenance rows. The formatter now uses the supported Allman value, and contributor guidance agrees. Independent review corrections, separate owning-project checks and pinned-SDK positive/negative/autofix controls qualify the policy repair; the historical three-file formatting failures and workspace limitations remain in [complete evidence](dw-468-validation-evidence.md). Only DW-468 closes; original fields/legacy detail, all earlier evidence, source files, archive/sprint, dependency pointers and release holds are preserved. Release remains not ready.

**DW-480 closure and DW-644 review deferral (2026-10-16):** A first member-removal activity-lease refusal now keeps an undispatched resubmittable preview and meaningful localized refusal feedback. Independent review/recheck, final Debug focused94/full UI5999 pass with zero failures/skips. [Exact commands, initial fixture failures and limits](dw-480-validation-evidence.md) retain all results. Only DW-480 closes; the separate existing stale-to-current pre-dispatch recovery defect is newly recorded as open DW-644. There are now 644 unique IDs: 342 done/302 open across 270 maintenance rows; the original 439-record reconciliation has 138 done/214 actionable, plus one newly actionable DW-644. Historical findings, unrelated record bodies, archive/sprint, dependencies and release holds remain preserved. No Git/dependency mutation ran. Release remains not ready.

**DW-644 closure and DW-645 review deferral (2026-10-17):** Explicitly undispatched evidence refusals now recover through deliberate resubmission with existing safety checks, accurate EN/FR feedback and renewed confirmation after any tenant/target identity replacement. Independent review and correction rechecks, Debug removal121/shared207/full UI6026 pass with zero failures/skips. [Exact commands, initial failures and runtime limits](dw-644-validation-evidence.md) retain all evidence. Only DW-644 closes; the older throwing-release cleanup defect is newly open as source-verified DW-645. All 645 IDs remain:343 done/302 open across270 maintenance rows. The original439-record reconciliation remains138 done/214 actionable; new DW-645 contributes one additional actionable record. All643 unrelated original record bodies, earlier history/decisions, archive/sprint, dependency pointers and release holds remain preserved. Release remains not ready.

### DW-1: Follow-up review still recommended for 1-8-support-safe-identifier-copy-and-read-experience-evidence after the damping cap was spent

status: done 2026-08-27
resolution: resolved by sweep bundle dw-support-safe-copy-followup
resolution-undo: e32f9ec5cfa74f6e713b0d8ce939f3393f91d53d4b83c40ea3313864db2fc699 2026-08-27 7374617475733a206f70656e
origin: review-budget-followup
source_spec: `spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md`
severity: low
archived: 2026-10-12

### DW-2: Global-administrator pagination >20 admins — fail-OPEN CLOSED (full paging redesign still routed)

status: done 2026-08-27
origin: migrated from legacy ledger ("2026-07-01 Correct Course — Deferred Work (pagination fail-closed + submodule doc handoffs)"), 2026-08-25
archived: 2026-10-12

### DW-3: FrontComposer `FcContentLabel` single-writer dispose-clobber + server first-paint — DOCUMENTED

status: done 2026-08-25
origin: migrated from legacy ledger ("2026-07-01 Correct Course — Deferred Work (pagination fail-closed + submodule doc handoffs)"), 2026-08-25
archived: 2026-10-12

### DW-4: FrontComposer `FcPageHeader.FocusHeadingAsync` no-op→throw — DOCUMENTED

status: done 2026-08-25
origin: migrated from legacy ledger ("2026-07-01 Correct Course — Deferred Work (pagination fail-closed + submodule doc handoffs)"), 2026-08-25
archived: 2026-10-12

### DW-5: EventStore `StorageTreemap` SVG `<g tabindex>` cross-browser — DOCUMENTED

status: done 2026-08-25
origin: migrated from legacy ledger ("2026-07-01 Correct Course — Deferred Work (pagination fail-closed + submodule doc handoffs)"), 2026-08-25
archived: 2026-10-12

### DW-6: JSDisconnectedException guard on panel focus

status: done 2026-08-25
origin: migrated from legacy ledger ("2026-06-30 Correct Course — Deferred Work (Tenants-Owned, Actionable) Implemented"), 2026-08-25
archived: 2026-10-12

### DW-7: Page-load global-admin query unguarded

status: done 2026-08-25
origin: migrated from legacy ledger ("2026-06-30 Correct Course — Deferred Work (Tenants-Owned, Actionable) Implemented"), 2026-08-25
archived: 2026-10-12

### DW-8: Tenant panel terminal-state focus parity

status: done 2026-08-25
origin: migrated from legacy ledger ("2026-06-30 Correct Course — Deferred Work (Tenants-Owned, Actionable) Implemented"), 2026-08-25
archived: 2026-10-12

### DW-9: Tenant confirm fail-closed on stale/degraded

status: done 2026-08-25
origin: migrated from legacy ledger ("2026-06-30 Correct Course — Deferred Work (Tenants-Owned, Actionable) Implemented"), 2026-08-25
archived: 2026-10-12

### DW-10: Tenant corrective-proof time tie-back + invariant culture

status: done 2026-08-25
origin: migrated from legacy ledger ("2026-06-30 Correct Course — Deferred Work (Tenants-Owned, Actionable) Implemented"), 2026-08-25
archived: 2026-10-12

### DW-11: Concurrent correction opens out of order

status: done 2026-08-25
origin: migrated from legacy ledger ("2026-06-30 Correct Course — Deferred Work (Tenants-Owned, Actionable) Implemented"), 2026-08-25
archived: 2026-10-12

### DW-12: No story-specific 5.7 gateway-routing test

status: done 2026-08-25
origin: migrated from legacy ledger ("2026-06-30 Correct Course — Deferred Work (Tenants-Owned, Actionable) Implemented"), 2026-08-25
archived: 2026-10-12

### DW-13: Create-tenant freshness gate narrowed `Current or Unknown → Current`

status: done 2026-08-25
origin: migrated from legacy ledger ("2026-06-30 Correct Course — Deferred Work (Tenants-Owned, Actionable) Implemented"), 2026-08-25
archived: 2026-10-12

### DW-14: FrontComposer owner handoff

status: done 2026-08-25
origin: migrated from legacy ledger ("2026-06-21 Correct Course — Deferred + Pending Work Implemented"), 2026-08-25
archived: 2026-10-12

### DW-15: EventStore owner handoff

status: done 2026-08-25
origin: migrated from legacy ledger ("2026-06-21 Correct Course — Deferred + Pending Work Implemented"), 2026-08-25
archived: 2026-10-12

### DW-16: EventStore owner handoff

status: done 2026-08-25
origin: migrated from legacy ledger ("2026-06-21 Correct Course — Deferred + Pending Work Implemented"), 2026-08-25
archived: 2026-10-12

### DW-17: Epic 11 — Production Authorization Readiness (persisted DataProtection key ring)

status: done 2026-08-25
origin: migrated from legacy ledger ("2026-06-21 Correct Course — Deferred + Pending Work Implemented"), 2026-08-25
archived: 2026-10-12

### DW-18: Pending (newly discovered) — Memories-integration doc/test drift

status: done 2026-06-21
origin: migrated from legacy ledger ("2026-06-21 Correct Course — Deferred + Pending Work Implemented"), 2026-08-25
archived: 2026-10-12

### DW-19: Freshness is no longer derived from response `ServedAt`. The implemented direct-read rule treats a real read-model ETag/projection version as `current`; absent markers resolve to `unknown`

status: done 2026-08-25
origin: migrated from legacy ledger ("`cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening`"), 2026-08-25
archived: 2026-10-12

### DW-20: Generic projection age/version metadata is not available from the current `IReadModelStore` contract. Do not add Tenants-owned generic persistence scaffolding; the remaining threshold-based age metadata need is routed to the EventStore owner handoff below

status: done 2026-08-25
origin: migrated from legacy ledger ("`cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening`"), 2026-08-25
archived: 2026-10-12

### DW-21: Null/empty read-model ETag behavior is explicit and tested: successful REST reads return 200 with no ETag, no projection-version header, no served-at header, and no 304 support

status: done 2026-08-25
origin: migrated from legacy ledger ("`cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening`"), 2026-08-25
archived: 2026-10-12

### DW-22: ETag handling is hardened and tested for weak tags, `*`, escaped strong tags, and unsupported multi-tag input

status: done 2026-08-25
origin: migrated from legacy ledger ("`cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening`"), 2026-08-25
archived: 2026-10-12

### DW-23: REST/handler read-model reconstruction coverage now proves a recreated controller factory can serve the persisted read model from the shared store and honor 304 through the production REST/handler path

status: done 2026-08-25
origin: migrated from legacy ledger ("`cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening`"), 2026-08-25
archived: 2026-10-12

### DW-24: Live populated-correlation gateway error coverage now asserts that `correlationId`, `reasonCode`, raw payload text, stack traces, tokens, cursors, and ETags do not reach user-facing copy

status: done 2026-08-25
origin: migrated from legacy ledger ("`cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening`"), 2026-08-25
archived: 2026-10-12

### DW-25: Current full-suite evidence (corrected 2026-06-21): the earlier `Server.Tests` blocker — 3 DAPR component expectation tests asserting removed `enableDeadLetter` / `deadLetterTopic` metadata — was resolved on 2026-06-20 by `cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup`; full `Server.Tests` now passes 700/700. `IntegrationTests` passes with DAPR/Aspire/performance skips. The old health-readiness blocker wording is no longer current evidence

status: done 2026-08-25
origin: migrated from legacy ledger ("`cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening`"), 2026-08-25
archived: 2026-10-12

### DW-26: Code review of `spec-frontcomposer-fluent-structural-and-style-conformance-sweep` on 2026-06-18

status: done 2026-08-27
origin: migrated from legacy ledger ("`cc-2026-06-19-domain-ui-governance-and-accessibility-hardening`"), 2026-08-25
archived: 2026-10-12

### DW-27: Code review of `spec-frontcomposer-shell-and-adminui-fluent-conformance-audit` on 2026-06-18

status: done 2026-08-27
origin: migrated from legacy ledger ("`cc-2026-06-19-domain-ui-governance-and-accessibility-hardening`"), 2026-08-25
archived: 2026-10-12

### DW-28: Compact non-zero spacing (e.g. `margin:0.5rem`/`padding:0.5rem`) is now flagged by the styling-ownership guard. The `(?!0)` zero-skip was replaced with a zero-token matcher that still skips genuine resets (`0`, `0 0 0 0`, `0px`, `0 !important`). No real component CSS regressed

status: done 2026-08-25
origin: migrated from legacy ledger ("`cc-2026-06-19-domain-ui-governance-and-accessibility-hardening`"), 2026-08-25
archived: 2026-10-12

### DW-29: The inline-style guard was widened beyond flex/grid/gap to also cover spacing (margin/padding), sizing (width/inline-size), and alignment (justify-content/align-items), and now scans both quote styles. No `.razor` carries inline `style=`

status: done 2026-08-25
origin: migrated from legacy ledger ("`cc-2026-06-19-domain-ui-governance-and-accessibility-hardening`"), 2026-08-25
archived: 2026-10-12

### DW-30: The `<div>`/`<span>` budget now excludes Razor (`@* *@`) and HTML (`<!-- -->`) comments before counting

status: done 2026-08-25
origin: migrated from legacy ledger ("`cc-2026-06-19-domain-ui-governance-and-accessibility-hardening`"), 2026-08-25
archived: 2026-10-12

### DW-31: `fc-css-exception` scoping decision: kept RULE-level with documented rationale; a unit test proves a marker exempts only its own rule and does not leak to the next rule

status: done 2026-08-25
origin: migrated from legacy ledger ("`cc-2026-06-19-domain-ui-governance-and-accessibility-hardening`"), 2026-08-25
archived: 2026-10-12

### DW-32: `:focus-visible` exemption decision: NARROWED. The blanket exemption was removed; focus-ring affordances (outline/outline-offset/outline-color) are untracked so genuine focus rules still pass, but a `:focus-visible` rule that owns layout/spacing/typography is now flagged unless documented

status: done 2026-08-25
origin: migrated from legacy ledger ("`cc-2026-06-19-domain-ui-governance-and-accessibility-hardening`"), 2026-08-25
archived: 2026-10-12

### DW-33: `RemoveForcedColorsMediaBlocks` now skips braces inside CSS comments and quoted strings so a stray brace cannot leak the block tail back into the scan

status: done 2026-08-25
origin: migrated from legacy ledger ("`cc-2026-06-19-domain-ui-governance-and-accessibility-hardening`"), 2026-08-25
archived: 2026-10-12

### DW-34: `MemberAccessReview` gained bUnit coverage proving the change-role and remove-member `aria-controls` resolve to a rendered active-region `id` after the FluentStack migration

status: done 2026-08-25
origin: migrated from legacy ledger ("`cc-2026-06-19-domain-ui-governance-and-accessibility-hardening`"), 2026-08-25
archived: 2026-10-12

### DW-35: `TenantAuditPage` renders a localized fallback (`Tenants.Audit.UnknownTenant`) for a blank/whitespace `TenantId` instead of a dangling heading

status: done 2026-08-25
origin: migrated from legacy ledger ("`cc-2026-06-19-domain-ui-governance-and-accessibility-hardening`"), 2026-08-25
archived: 2026-10-12

### DW-36: The claim that the styling scan is blind to declarations inside `@media` blocks was verified as a false positive in the structural/style story and is not open work

status: done 2026-08-27
origin: migrated from legacy ledger ("`cc-2026-06-19-domain-ui-governance-and-accessibility-hardening`"), 2026-08-25
archived: 2026-10-12

### DW-37: Code review of `spec-frontcomposer-fluent-structural-and-style-conformance-sweep` on 2026-06-18

status: done 2026-08-27
origin: migrated from legacy ledger ("`cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup`"), 2026-08-25
archived: 2026-10-12

### DW-38: Code review of `spec-frontcomposer-shell-and-adminui-fluent-conformance-audit` on 2026-06-18

status: done 2026-08-27
origin: migrated from legacy ledger ("`cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup`"), 2026-08-25
archived: 2026-10-12

### DW-39: Current deployment docs/YAML scan on 2026-06-19

status: done 2026-08-27
origin: migrated from legacy ledger ("`cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup`"), 2026-08-25
archived: 2026-10-12

### DW-40: DAPR v1.17 topic-scoping documentation checked on 2026-06-20

status: done 2026-08-27
origin: migrated from legacy ledger ("`cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup`"), 2026-08-25
archived: 2026-10-12

### DW-41: Production `deploy/dapr/pubsub.yaml` denies `sample` publishing with an empty topic list (`publishingScopes: "sample="`) and allows `sample` to subscribe to `tenants.events`, while leaving `eventstore` unlisted so it keeps unrestricted publish access (required for EventStore dynamic per-tenant topic provisioning, NFR20 — listing `eventstore` is the documented anti-pattern)

status: done 2026-08-25
origin: migrated from legacy ledger ("`cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup`"), 2026-08-25
archived: 2026-10-12

### DW-42: Local AppHost pub/sub intentionally omits topic-level scopes while retaining component-level `eventstore` and `sample` scopes; the difference is documented in the component YAML and timing guide

status: done 2026-08-25
origin: migrated from legacy ledger ("`cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup`"), 2026-08-25
archived: 2026-10-12

### DW-43: `docs/cross-aggregate-timing.md` distinguishes subscriber redelivery on `tenants.events` from EventStore's application-level dead-letter publisher for `deadletter.tenants.events`

status: done 2026-08-25
origin: migrated from legacy ledger ("`cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup`"), 2026-08-25
archived: 2026-10-12

### DW-44: `CrossAggregateTimingDocumentationTests` guards the production topic-scope contract, local topic-scope omission, application-level dead-letter wording, and the absence of DAPR subscriber-failure-to-dead-letter wording

status: done 2026-08-25
origin: migrated from legacy ledger ("`cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup`"), 2026-08-25
archived: 2026-10-12

### DW-45: June 18 review-record contradictions are kept as routed, stale/resolved, or future-owner handoff entries instead of open Tenants implementation work

status: done 2026-08-27
origin: migrated from legacy ledger ("`cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup`"), 2026-08-25
archived: 2026-10-12

### DW-46: `FrontComposerShell` exposes a shell content landmark contract that can be a native `<main>` or an equivalent role with an accessible-name parameter

status: done 2026-08-25
origin: migrated from legacy ledger ("FrontComposer owner: `frontcomposer-2026-06-19-page-header-landmarks-and-contract-hardening`"), 2026-08-25
archived: 2026-10-12

### DW-47: Tenants page headings can name the shell main landmark without orphaned page-level `aria-labelledby`

status: done 2026-08-25
origin: migrated from legacy ledger ("FrontComposer owner: `frontcomposer-2026-06-19-page-header-landmarks-and-contract-hardening`"), 2026-08-25
archived: 2026-10-12

### DW-48: `FcPageHeader` no longer creates a competing global `banner` landmark on every route page

status: done 2026-08-25
origin: migrated from legacy ledger ("FrontComposer owner: `frontcomposer-2026-06-19-page-header-landmarks-and-contract-hardening`"), 2026-08-25
archived: 2026-10-12

### DW-49: `FcPageHeader` handles blank `Heading` fail-safely or documents a strict consumer contract with analyzable/tested guardrails

status: done 2026-08-25
origin: migrated from legacy ledger ("FrontComposer owner: `frontcomposer-2026-06-19-page-header-landmarks-and-contract-hardening`"), 2026-08-25
archived: 2026-10-12

### DW-50: `FocusHeadingAsync()` ensures the heading is focusable when used as a focus target or fails diagnostically when `HeadingTabIndex` is omitted

status: done 2026-08-25
origin: migrated from legacy ledger ("FrontComposer owner: `frontcomposer-2026-06-19-page-header-landmarks-and-contract-hardening`"), 2026-08-25
archived: 2026-10-12

### DW-51: FrontComposer H-FC-1: rework or re-justify `FcHomeCard` against pinned `FluentCard` support

status: done 2026-08-25
origin: migrated from legacy ledger ("FrontComposer owner: `frontcomposer-2026-06-19-page-header-landmarks-and-contract-hardening`"), 2026-08-25
archived: 2026-10-12

### DW-52: FrontComposer H-FC-2: consider parity guards for structural/style governance

status: done 2026-08-25
origin: migrated from legacy ledger ("FrontComposer owner: `frontcomposer-2026-06-19-page-header-landmarks-and-contract-hardening`"), 2026-08-25
archived: 2026-10-12

### DW-53: Continue the Admin.UI audit remediation handoffs from `audit-frontcomposer-shell-adminui-fluent-2026-06-18.md`: `Index.razor` non-semantic clickable semantics, clickable-span remediation, `ActivityChart` a11y proof, `StorageTreemap` semantics/docs, and optional parity guards

status: done 2026-08-25
origin: migrated from legacy ledger ("EventStore owner: `eventstore-2026-06-19-admin-ui-and-query-record-followup`"), 2026-08-25
archived: 2026-10-12

### DW-54: If EventStore tests still encode the retired Tenants actor-routing assumption, update them under EventStore ownership

status: done 2026-08-26
origin: migrated from legacy ledger ("EventStore owner: `eventstore-2026-06-19-admin-ui-and-query-record-followup`"), 2026-08-25
archived: 2026-10-12

### DW-55: Add or expose shared read-model metadata for persisted projection timestamp/version if D6 threshold-based `aging` and `stale` states need to be computed generically

status: done 2026-08-25
origin: migrated from legacy ledger ("EventStore owner: `eventstore-2026-06-19-read-model-freshness-metadata`"), 2026-08-25
archived: 2026-10-12

### DW-56: Keep the capability in `Hexalith.EventStore` (`IReadModelStore` / query metadata path) rather than adding Tenants-specific persistence scaffolding

status: done 2026-08-25
origin: migrated from legacy ledger ("EventStore owner: `eventstore-2026-06-19-read-model-freshness-metadata`"), 2026-08-25
archived: 2026-10-12

### DW-57: Once available, Tenants can map real persisted projection age/version through configurable thresholds; until then Tenants uses the direct-read ETag/version `current` rule and fails unmarked responses closed to `unknown`

status: done 2026-08-25
origin: migrated from legacy ledger ("EventStore owner: `eventstore-2026-06-19-read-model-freshness-metadata`"), 2026-08-25
archived: 2026-10-12

### DW-58: ETag special-character (quote/comma) robustness — latent, non-exploitable. `NormalizeETagToken`/`Trim('"')` unquote any value that starts and ends with `"` (asymmetric vs raw store tokens) in `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs:87-107`; the client and server both reject commas with a substring check, dropping a single quoted strong tag whose content legitimately contains a comma (`src/Hexalith.Tenants.UI/Services/Gateways/TenantsQueryApiClient.cs:25-29`); and client/server normalization disagree on quoted-whitespace/`"*"` edge inputs. These do not bite while DAPR/Redis read-model ETags remain opaque numeric strings without quotes or commas, and the emit→submit→compare round-trip is internally symmetric. Revisit if the EventStore read-model store contract ever emits special-character ETags (ties into the `eventstore-2026-06-19-read-model-freshness-metadata` handoff above)

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening (2026-06-19)"), 2026-08-25
archived: 2026-10-12

### DW-59: CSS ownership guard logical longhand spacing — RESOLVED (2026-06-21 hardening). `DomainUiFluentConformanceTests` now tracks the logical longhands (`margin-inline-start/-end`, `padding-block-start/-end`, etc.) alongside the physical longhands and shorthand, with `[InlineData]` coverage for both flagged and zero-reset cases

status: done 2026-06-21
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-19-domain-ui-governance-and-accessibility-hardening (2026-06-19)"), 2026-08-25
archived: 2026-10-12

### DW-60: Forced-colors malformed block handling — RESOLVED (2026-06-21 hardening). `RemoveForcedColorsMediaBlocks` plus a dedicated `Forced_colors_unterminated_block_does_not_hide_trailing_ownership` test now ensure an unterminated forced-colors block cannot hide trailing ownership declarations from the scan

status: done 2026-06-21
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-19-domain-ui-governance-and-accessibility-hardening (2026-06-19)"), 2026-08-25
archived: 2026-10-12

### DW-61: Sibling query ETag special-character robustness — quote/comma ETag edge cases surfaced again because the working-tree diff includes the completed tenant-query hardening story. Keep routed under the tenant-query review / EventStore read-model freshness handoff; it is outside the domain UI governance story

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-19-domain-ui-governance-and-accessibility-hardening (2026-06-19)"), 2026-08-25
archived: 2026-10-12

### DW-62: Application-level vs native dead-letter framing for operators — RESOLVED (2026-06-21 hardening). `deploy/dapr/README.md:53` now carries an explicit operator note scoping the "no native dead-letter" claim to the `pubsub` component shipped here and warning that an EventStore-provided component may set its own `enableDeadLetter`/`deadLetterTopic`

status: done 2026-06-21
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup (2026-06-20)"), 2026-08-25
archived: 2026-10-12

### DW-63: Stale Server.Tests evidence line in `test-summary.md` — RESOLVED (2026-06-21 hardening). A correction note was added (`tests/test-summary.md:246`) recording that the 3-test Server.Tests blocker was resolved on 2026-06-20 and that Server.Tests passes; the old line is retained only as dated historical evidence

status: done 2026-06-21
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup (2026-06-20)"), 2026-08-25
archived: 2026-10-12

### DW-64: `<FcContentLabel>` single-writer dispose-clobber

status: done 2026-07-01
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-21-frontcomposer-page-header-landmarks-and-contract-hardening (2026-06-25)"), 2026-08-25
archived: 2026-10-12

### DW-65: Page-driven `<FcContentLabel>` accessible name absent on server first paint

status: done 2026-07-01
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-21-frontcomposer-page-header-landmarks-and-contract-hardening (2026-06-25)"), 2026-08-25
archived: 2026-10-12

### DW-66: `FocusHeadingAsync()` no-op → throw is an undisclosed API behavior change

status: done 2026-07-01
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-21-frontcomposer-page-header-landmarks-and-contract-hardening (2026-06-25)"), 2026-08-25
archived: 2026-10-12

### DW-67: SVG `<g tabindex="0">` focusability not guaranteed cross-browser

status: done 2026-07-01
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-21-eventstore-admin-ui-a11y-remediation (2026-06-25)"), 2026-08-25
archived: 2026-10-12

### DW-68: Global Administrators / Audit discoverability after nav de-listing

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-27-tenants-module-tabbed-workspace (2026-06-27)"), 2026-08-25
archived: 2026-10-12

### DW-69: GlobalAdministratorPolicy now registered but unconsumed

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-27-tenants-module-tabbed-workspace — Group 1 re-review (2026-06-27, chunked)"), 2026-08-25
archived: 2026-10-12

### DW-70: Create-tenant freshness gate narrowed `Current or Unknown` → `Current` — RESOLVED 2026-06-30 (CC deferred-work, verify-only)

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-27-tenants-module-tabbed-workspace — Group 1 re-review (2026-06-27, chunked)"), 2026-08-25
archived: 2026-10-12

### DW-71: Page-local tabs render empty tabpanels

status: done 2026-09-02
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-27-tenants-module-tabbed-workspace — Group 1 re-review (2026-06-27, chunked)"), 2026-08-25
archived: 2026-10-12

### DW-72: Page-local tabs a11y — empty tabpanels + missing Tenants-owned bUnit assertion

status: done 2026-09-02
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-27-tenants-module-tabbed-workspace — Full review (2026-06-28)"), 2026-08-25
archived: 2026-10-12

### DW-73: Global-administrator projection pagination ignored (>20 admins)

status: done 2026-08-27
origin: migrated from legacy ledger ("Deferred from: code review of 5-7-global-administrator-correction-verification (2026-06-29)"), 2026-08-25
archived: 2026-10-12

### DW-74: No story-specific gateway-routing test — CLOSED 2026-06-30 (CC deferred-work) as already-covered

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of 5-7-global-administrator-correction-verification (2026-06-29)"), 2026-08-25
archived: 2026-10-12

### DW-75: Terminal failure states reset to a fresh submittable preview on parent re-render (HIGH) — RESOLVED 2026-06-30

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of 5-8-correction-projection-refresh-cleanup (2026-06-29)"), 2026-08-25
archived: 2026-10-12

### DW-76: `ConfirmProjection` confirms off a known-Stale projection — RESOLVED 2026-06-30

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of 5-8-correction-projection-refresh-cleanup (2026-06-29)"), 2026-08-25
archived: 2026-10-12

### DW-77: Corrective-proof lookup may link the wrong historical audit row

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of 5-8-correction-projection-refresh-cleanup (2026-06-29)"), 2026-08-25
archived: 2026-10-12

### DW-78: Focus call lacks `JSDisconnectedException` guard — RESOLVED 2026-06-30 (CC deferred-work, Edit A)

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of 5-8-correction-projection-refresh-cleanup (2026-06-29)"), 2026-08-25
archived: 2026-10-12

### DW-79: Global-admin projection query unguarded in the page-load critical path — RESOLVED 2026-06-30 (CC deferred-work, Edit B)

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of 5-8-correction-projection-refresh-cleanup (2026-06-29)"), 2026-08-25
archived: 2026-10-12

### DW-80: Corrective-proof timestamp uses `CurrentCulture` instead of `InvariantCulture` — RESOLVED 2026-06-30

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of 5-8-correction-projection-refresh-cleanup (2026-06-29)"), 2026-08-25
archived: 2026-10-12

### DW-81: EventCallback→Func drops the parent re-render after confirm refresh (intentional, benign)

status: done 2026-09-06
origin: migrated from legacy ledger ("Deferred from: code review of 5-8-correction-projection-refresh-cleanup (2026-06-29)"), 2026-08-25
archived: 2026-10-12

### DW-82: `CorrectionStartPanel` terminal-state focus parity (story 5.6) — RESOLVED 2026-06-30 (CC deferred-work, Edit C)

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of 5-7-global-administrator-correction-verification — committed bundle re-review (2026-06-30)"), 2026-08-25
archived: 2026-10-12

### DW-83: Already-logged (2026-06-29), re-confirmed: RESOLVED 2026-07-01 (CC deferred-work):

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of 5-7-global-administrator-correction-verification — committed bundle re-review (2026-06-30)"), 2026-08-25
archived: 2026-10-12

### DW-84: Already-logged (2026-06-29), re-confirmed:

status: done 2026-08-27
origin: migrated from legacy ledger ("Deferred from: code review of 5-7-global-administrator-correction-verification — committed bundle re-review (2026-06-30)"), 2026-08-25
archived: 2026-10-12

### DW-85: Ledger-hygiene (see 5.7 patch P-9) — CLOSED 2026-06-30:

status: done 2026-08-27
origin: migrated from legacy ledger ("Deferred from: code review of 5-7-global-administrator-correction-verification — committed bundle re-review (2026-06-30)"), 2026-08-25
archived: 2026-10-12

### DW-86: Concurrent correction opens can finish projection refresh out of order — RESOLVED 2026-06-30 (CC deferred-work, Edit D)

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of 5-8-correction-projection-refresh-cleanup (2026-06-30)"), 2026-08-25
archived: 2026-10-12

### DW-87: Tenant-domain correction can still confirm from stale/degraded tenant detail — RESOLVED 2026-06-30 (CC deferred-work, Edit E)

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of 5-8-correction-projection-refresh-cleanup (2026-06-30)"), 2026-08-25
archived: 2026-10-12

### DW-88: Tenant-domain corrective proof lookup can link unrelated historical rows — RESOLVED 2026-06-30 (CC deferred-work, Edit F)

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of 5-8-correction-projection-refresh-cleanup (2026-06-30)"), 2026-08-25
archived: 2026-10-12

### DW-89: Scheduled performance workflow lacks the EventStore opt-in — the shared `domain-ci.yml` performance job invokes the `Category=Performance` lane without `HEXALITH_EVENTSTORE_RUN_PERFORMANCE_TESTS=1`, while `DaprPerformanceFactAttribute` requires that variable

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: run-all-tests-and-fix-failures review (2026-07-14)"), 2026-08-25
archived: 2026-10-12

### DW-90: Zero test changes despite several identified Tenants-rendering gaps

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of 1-0-reverify-frontcomposer-shell-and-fluent-contracts (2026-07-19)"), 2026-08-25
archived: 2026-10-12

### DW-91: No tracking ticket/issue for the FrontComposer-owned gaps

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of 1-0-reverify-frontcomposer-shell-and-fluent-contracts (2026-07-19)"), 2026-08-25
archived: 2026-10-12

### DW-92: `sprint-status.yaml`'s flat per-story status can't represent "review with 2 of 5 sub-contracts blocked"

status: done 2026-08-26
origin: migrated from legacy ledger ("Deferred from: code review of 1-0-reverify-frontcomposer-shell-and-fluent-contracts (2026-07-19)"), 2026-08-25
archived: 2026-10-12

### DW-93: Epic 1 is marked done while most child stories remain backlog or review

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of 1-0-reverify-frontcomposer-shell-and-fluent-contracts (2026-07-19)"), 2026-08-25
archived: 2026-10-12

### DW-94: Reusable release caller omits required publication-authority inputs

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of 1-1-reverify-ui-host-bootstrap-and-canonical-workspace (2026-07-19)"), 2026-08-25
archived: 2026-10-12

### DW-95: Submodule pointer upgrades require their own review

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of 1-1-reverify-ui-host-bootstrap-and-canonical-workspace (2026-07-19)"), 2026-08-25
archived: 2026-10-12

### DW-96: Epic 1 aggregate status conflicts with child stories

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of 1-1-reverify-ui-host-bootstrap-and-canonical-workspace (2026-07-19)"), 2026-08-25
archived: 2026-10-12

### DW-97: `EXPECTED_DEPENDENCIES` is hand-duplicated between `scripts/validate-nuget-packages.py` and its test mirror in `CiQualityGateScriptTests.cs`

status: done 2026-09-02
resolution: resolved by sweep bundle dw-package-boundary-source
resolution-undo: 3446eef21b1b160c45f226f960c64918cc4cf6db292ace5f1d0586f9c9e868d0 2026-09-02 7374617475733a206f70656e
origin: migrated from legacy ledger ("Deferred from: code review of run-all-tests-and-fix-failures-2 (2026-07-20)"), 2026-08-25
archived: 2026-10-12

### DW-98: Dependency-boundary validation is a hardcoded per-package allowlist rather than derived from actual restore/lock output

status: done 2026-09-02
resolution: resolved by sweep bundle dw-package-boundary-source
resolution-undo: 3446eef21b1b160c45f226f960c64918cc4cf6db292ace5f1d0586f9c9e868d0 2026-09-02 7374617475733a206f70656e
origin: migrated from legacy ledger ("Deferred from: code review of run-all-tests-and-fix-failures-2 (2026-07-20)"), 2026-08-25
archived: 2026-10-12

### DW-99: `NextPageAsync` unguarded `NextCursor==null`

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of 1-2-tenant-list-triage-and-cursor-foundation (2026-07-20)"), 2026-08-25
archived: 2026-10-12

### DW-100: Grid cannot return to the default `TenantId` ordering except via toolbar Reset

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of 1-2-tenant-list-triage-and-cursor-foundation (2026-07-20)"), 2026-08-25
archived: 2026-10-12

### DW-101: Brittle source-text "guard" tests + stale resource stub

status: done 2026-09-05
origin: migrated from legacy ledger ("Deferred from: code review of 1-2-tenant-list-triage-and-cursor-foundation (2026-07-20)"), 2026-08-25
archived: 2026-10-12

### DW-102: Duplicated tab/scope literal constants across two files

status: done 2026-09-06
origin: migrated from legacy ledger ("Deferred from: code review of 1-2-tenant-list-triage-and-cursor-foundation (2026-07-20)"), 2026-08-25
archived: 2026-10-12

### DW-103: Redundant double a11y labeling on badges

status: done 2026-10-12
origin: migrated from legacy ledger ("Deferred from: code review of 1-2-tenant-list-triage-and-cursor-foundation (2026-07-20)"), 2026-08-25
priority: P3
archived: 2026-10-12

### DW-104: Disclosed runtime-verification gaps
origin: migrated from legacy ledger ("Deferred from: code review of 1-2-tenant-list-triage-and-cursor-foundation (2026-07-20)"), 2026-08-25
location: reasonCode
reason: The legacy ledger defers this issue: Disclosed runtime-verification gaps. Original context is preserved in legacy-detail.
legacy-detail: - **Disclosed runtime-verification gaps** — (1) the invalid-cursor page-one recovery wire-path (that the `list-tenants` query actually populates the `reasonCode` problem-details extension the gateway matches on, rather than only `detail`) rests on unit doubles; (2) AC8 per-width and forced-colors behavior is proven by grid-scoped CSS + bUnit/forced-colors conformance rather than full browser emulation, because the local Chrome lane exposes a fixed 1235px virtual viewport (window resize is a no-op). Both are disclosed in the story Debug Log and do not gate this read-only UI story per the Epic 1 convention. (`story evidence`)
status: open
disposition: blocked
verification-date: 2026-10-12
verification: tests/Hexalith.Tenants.IntegrationTests/TenantsApiGeneratedControllerTests.cs covers real generated reads, but the original invalid-cursor reasonCode wire proof and authenticated per-width/forced-colors acceptance packet are not established by component/source assertions.
verification-files: tests/Hexalith.Tenants.IntegrationTests/TenantsApiGeneratedControllerTests.cs
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: healthy authenticated browser/API fixture
next-action: Capture the invalid-cursor problem-details response and browser width/forced-colors traces.
related: DW-116, DW-161

### DW-105: Degenerate/exotic tenant ids on the shared detail-nav path

status: done 2026-10-14
origin: migrated from legacy ledger ("Deferred from: code review of 1-4-my-tenants-self-audit (2026-07-21)"), 2026-08-25
priority: P2
archived: 2026-10-15

### DW-106: Audit-grid unsafe references are omitted from copying but remain visible in the rendered reference label

status: done 2026-08-27
resolution: resolved by sweep bundle dw-support-safe-copy-followup
resolution-undo: e32f9ec5cfa74f6e713b0d8ce939f3393f91d53d4b83c40ea3313864db2fc699 2026-08-27 7374617475733a206f70656e
origin: migrated from legacy ledger ("Deferred from: code review of 1-4-my-tenants-self-audit (2026-07-21)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md`
archived: 2026-10-12

### DW-107: Clipboard module import and write operations are not coordinated with component disposal

status: done 2026-08-27
resolution: resolved by sweep bundle dw-support-safe-copy-followup
resolution-undo: e32f9ec5cfa74f6e713b0d8ce939f3393f91d53d4b83c40ea3313864db2fc699 2026-08-27 7374617475733a206f70656e
origin: migrated from legacy ledger ("Deferred from: code review of 1-4-my-tenants-self-audit (2026-07-21)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md`
archived: 2026-10-12

### DW-108: Caller-supplied tenant identifiers are reused as raw DOM ids and return-focus anchors

status: done 2026-10-14
origin: migrated from legacy ledger ("Deferred from: code review of 1-4-my-tenants-self-audit (2026-07-21)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md`
priority: P2
archived: 2026-10-15

### DW-109: The audit receipt copies a hidden synthesized English composite rather than one exact visible localized literal

status: done 2026-08-27
resolution: resolved by sweep bundle dw-support-safe-copy-followup
resolution-undo: e32f9ec5cfa74f6e713b0d8ce939f3393f91d53d4b83c40ea3313864db2fc699 2026-08-27 7374617475733a206f70656e
origin: migrated from legacy ledger ("Deferred from: code review of 1-4-my-tenants-self-audit (2026-07-21)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md`
archived: 2026-10-12

### DW-110: Legacy configuration display safety remains a deny-list that can miss unrecognized secret formats

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of 1-4-my-tenants-self-audit (2026-07-21)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md`
archived: 2026-10-12

### DW-111: Configuration keys that fail the legacy display-safety policy remain visible even while their paired values are redacted

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of 1-4-my-tenants-self-audit (2026-07-21)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md`
archived: 2026-10-12

### DW-112: The authoritative-search status filter's visible label and accessible name can describe different scopes

status: done 2026-10-15
origin: migrated from legacy ledger ("Deferred from: code review of 1-4-my-tenants-self-audit (2026-07-21)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md`
priority: P2
archived: 2026-10-15

### DW-113: An unmapped list reason renders its raw resource key as user-visible copy in the shared list-state surface
origin: migrated from legacy ledger ("Deferred from: code review of 1-4-my-tenants-self-audit (2026-07-21)"), 2026-08-25
location: ListSurfaceStates.razor; TenantsResources.resx
source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md`
reason: The legacy ledger defers this issue: An unmapped list reason renders its raw resource key as user-visible copy in the shared list-state surface. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md` summary: An unmapped list reason renders its raw resource key as user-visible copy in the shared list-state surface. evidence: `ListSurfaceStates.razor` resolves `Localizer["Tenants.List.Reason.{Reason}"]` for any non-`None` reason, but only 5 of the 10 `TenantListReason` members have a `Tenants.List.Reason.*` key in `TenantsResources.resx`; an unmapped reason therefore renders the literal key in EN and FR. No currently reachable call site passes an unmapped reason, so this is a latent pre-existing trap in the shared component rather than a live defect of this story.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Shared/ListSurfaceStates.razor Message interpolates Tenants.List.Reason.{Reason}; Resources/TenantsResources.resx has five reason keys while State/TenantList/TenantListReason.cs has eleven non-None members. Unsupported inputs still echo keys; no current reachable caller was established.
verification-files: src/Hexalith.Tenants.UI/Components/Shared/ListSurfaceStates.razor; src/Hexalith.Tenants.UI/State/TenantList/TenantListReason.cs
evidence-kind: verified-defect
priority: P3
owner: unassigned
dependencies: none
next-action: Add explicit fallback/mapping and EN/FR coverage for every accepted reason.
related: none

### DW-114: The advance-by-requested-window paging rule rests on a Memories server premise that no test in this repository can observe
origin: migrated from legacy ledger ("Deferred from: code review of 1-4-my-tenants-self-audit (2026-07-21)"), 2026-08-25
location: references/
source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md`
reason: The legacy ledger defers this issue: The advance-by-requested-window paging rule rests on a Memories server premise that no test in this repository can observe. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md` summary: The advance-by-requested-window paging rule rests on a Memories server premise that no test in this repository can observe. evidence: Correctness of `nextOffset = min(rawOffset + PageSize, TotalCount)` requires the Memories search server to apply `Offset` before dropping entries that fail its required-field check and to report the untrimmed total. That is true of `SyntacticSearchService` in the consumed submodule today, but `SearchResult.TotalCount` documents only "may exceed returned results", every gateway test stubs `MemoriesClient.SearchAsync`, and the intent's Block-If bars editing anything under `references/`. Closing this needs a contract test in the Memories repository or a Tenants integration test against a live index.
status: open
disposition: blocked
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs advances rawOffset by requested PageSize; references/Hexalith.Memories/src/Hexalith.Memories.Server/Search/SyntacticSearchService.cs reports raw totals and filters required fields. Tenants gateway tests still substitute SearchAsync, so no owning contract regression proves that premise.
verification-files: src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs; references/Hexalith.Memories/src/Hexalith.Memories.Server/Search/SyntacticSearchService.cs
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Memories owning contract/integration lane
next-action: Obtain a Memories server contract test for offset-before-filtering and untrimmed totals.
related: DW-122

### DW-115: The tenant-detail read path does not adopt the shared null-member guard, so a malformed member element crashes the detail page while both list surfaces degrade safely

status: done 2026-09-05
origin: migrated from legacy ledger ("Deferred from: code review of 1-4-my-tenants-self-audit (2026-07-21)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md`
archived: 2026-10-12

### DW-116: The "exactly one polite live region" proof is an artefact of bUnit's missing shadow DOM
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-9-authoritative-memories-search-with-protected-paging (2026-07-27)"), 2026-08-25
location: TenantListSurfaceTests.cs:1866
source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md`
reason: The legacy ledger defers this issue: The "exactly one polite live region" proof is an artefact of bUnit's missing shadow DOM. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md` summary: The "exactly one polite live region" proof is an artefact of bUnit's missing shadow DOM. evidence: `TenantListSurfaceTests.cs:1866` counts live regions in markup where `<fluent-message-bar>` renders as an inert custom element. The shipped Fluent v5 module sets `role="status" aria-live="polite"` on each bar's internal dialog at runtime, so a real browser nests a live region per bar inside the workspace's outer one. The helper degenerates to `0.ShouldBe(0)` on the empty-notice call. status: open — blocked on BROWSER-SEARCH-1.9 and AT-NVDA-1.9, both already open.
status: open
disposition: blocked
verification-date: 2026-10-12
verification: tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs counts light-DOM aria-live markup; Components/Shared/ListSurfaceStates.razor uses Fluent messages. This cannot establish the real shadow-DOM or screen-reader announcement count.
verification-files: tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs; src/Hexalith.Tenants.UI/Components/Shared/ListSurfaceStates.razor
evidence-kind: unverified-risk
priority: P2
owner: unassigned
dependencies: BROWSER-SEARCH-1.9 and AT-NVDA-1.9
next-action: Capture authenticated search live-region and NVDA evidence with the shipped Fluent runtime.
related: DW-104

### DW-117: Surfacing codec exceptions escape the gateway and reach an unguarded LoadAsync, tearing down the Blazor circuit
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-9-authoritative-memories-search-with-protected-paging (2026-07-27)"), 2026-08-25
location: TenantQueryGateway.cs:1019; TenantsWorkspace.razor:632
source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md`
reason: The legacy ledger defers this issue: Surfacing codec exceptions escape the gateway and reach an unguarded LoadAsync, tearing down the Blazor circuit. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md` summary: Surfacing codec exceptions escape the gateway and reach an unguarded LoadAsync, tearing down the Blazor circuit. evidence: `TenantQueryGateway.cs:1019` deliberately re-raises `ObjectDisposedException`, `NullReferenceException`, `ArgumentNullException`, `OutOfMemoryException`; `TenantsWorkspace.razor:632` catches only `OperationCanceledException`. A disposed Data Protection provider during host shutdown therefore kills the circuit where every other cursor-protection failure degrades to the ordinary list. Documented as deliberate in the source comments, and shutdown-time disposal is benign, so recorded rather than patched. status: open
status: open
disposition: accepted-limitation
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs IsSurfacingDefect explicitly rethrows fatal/programming/disposal exceptions; Components/Pages/TenantsWorkspace.razor LoadAsync only contains cancellation. Source remarks explicitly choose diagnostic surfacing rather than classifying programming failures as malformed cursors.
verification-files: src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs; src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor
evidence-kind: accepted-limitation
priority: P3
owner: unassigned
dependencies: none
next-action: Revisit containment only if the documented surfacing policy changes.
related: none

### DW-118: The codec argument guard straddles the contained/surfacing partition, so its null and empty halves produce opposite outcomes
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-9-authoritative-memories-search-with-protected-paging (2026-07-27)"), 2026-08-25
location: TenantQueryGateway.cs:1025
source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md`
reason: The legacy ledger defers this issue: The codec argument guard straddles the contained/surfacing partition, so its null and empty halves produce opposite outcomes. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md` summary: The codec argument guard straddles the contained/surfacing partition, so its null and empty halves produce opposite outcomes. evidence: `QueryCursorCodec` uses `ArgumentException.ThrowIfNullOrWhiteSpace`, which throws `ArgumentNullException` for null and `ArgumentException` for empty. `TenantQueryGateway.cs:1025` excludes `ArgumentNullException` before the `ArgumentException` base match, so null escapes to circuit teardown while empty degrades to the ordinary list. Not reachable today: `TenantSearchCursorScopes.Create` never returns null and `TenantSearchCursorPosition.Format` never returns empty. status: open
status: open
disposition: accepted-limitation
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs excludes ArgumentNullException before ArgumentException; TenantSearchCursorScopes.Create and TenantSearchCursorPosition.Format produce closed nonblank inputs. The asymmetry remains deliberate for programming defects and is not a demonstrated production input path.
verification-files: src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs
evidence-kind: accepted-limitation
priority: P3
owner: unassigned
dependencies: none
next-action: Revisit if codec callers begin accepting nullable scope or position inputs.
related: DW-117

### DW-119: The pager is unmounted on every load, dropping keyboard focus from the button the operator just pressed
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-9-authoritative-memories-search-with-protected-paging (2026-07-27)"), 2026-08-25
location: TenantsWorkspace.razor:534
source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md`
reason: The legacy ledger defers this issue: The pager is unmounted on every load, dropping keyboard focus from the button the operator just pressed. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md` summary: The pager is unmounted on every load, dropping keyboard focus from the button the operator just pressed. evidence: `TenantsWorkspace.razor:534` sets `_snapshot = TenantListSnapshot.Loading()`, making `ShowList`, `HasMore` and `HasPreviousPage` all false, so `ShowPager` (`:416`) removes the whole `<nav>` for the duration of every load. Needs the authenticated browser lane to confirm the focus consequence. status: open — needs BROWSER-SEARCH-1.9.
status: open
disposition: uncertain
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor LoadAsync assigns Loading and ShowPager rejects Loading. The removal is verified, but current active-element behavior after Previous/Next has no authenticated browser proof.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor
evidence-kind: unverified-risk
priority: P2
owner: unassigned
dependencies: authenticated browser search fixture
next-action: Record document.activeElement during paging and determine whether restoration is required.
related: DW-177

### DW-120: The CI package-boundary gate asserts the fixture it generates from its own allowlist

status: done 2026-09-02
resolution: resolved by sweep bundle dw-package-boundary-source
resolution-undo: 3446eef21b1b160c45f226f960c64918cc4cf6db292ace5f1d0586f9c9e868d0 2026-09-02 7374617475733a206f70656e
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-9-authoritative-memories-search-with-protected-paging (2026-07-27)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md`
archived: 2026-10-12

### DW-121: The shared release workflow documents `source-branch` as configurable even though the established publication policy accepts only `main`
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-9-authoritative-memories-search-with-protected-paging (2026-07-27)"), 2026-08-25
location: domain-release.yml
source_spec: `_bmad-output/implementation-artifacts/spec-gh-actions-30240946791-89897853390.md`
reason: The legacy ledger defers this issue: The shared release workflow documents `source-branch` as configurable even though the established publication policy accepts only `main`. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-gh-actions-30240946791-89897853390.md` summary: The shared release workflow documents `source-branch` as configurable even though the established publication policy accepts only `main`. evidence: `domain-release.yml` and the pre-existing publication preflight reject every source branch except `main`, while the reusable-workflow input description says only that it is an exact protected source branch; resolving that public contract is broader than the stale-release race fix.
status: open
decision: 2026-08-26 Main-only contract — Remove configurable-branch ambiguity and enforce and document main across the workflow and callers.
decision: 2026-08-25 Main-only contract — Remove configurable-branch ambiguity and enforce and document main across shared workflow and callers.
disposition: actionable
verification-date: 2026-10-12
verification: references/Hexalith.Builds/.github/workflows/domain-release.yml source-branch description remains configurable while publication boundaries reject anything except main. Existing 2026-08-25/26 main-only decisions remain settled.
verification-files: references/Hexalith.Builds/.github/workflows/domain-release.yml
evidence-kind: verified-defect
priority: P2
owner: Hexalith.Builds
dependencies: Hexalith.Builds owning workflow change
next-action: Align the input description/contract and callers with the recorded main-only policy.
related: none

### DW-122: Per-page candidate dedup lets one tenant render on two consecutive authoritative search pages
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-9-authoritative-memories-search-with-protected-paging (2026-07-27, pass 3)"), 2026-08-25
location: spec-1-9-…-paging.md:238
source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md`
reason: The legacy ledger defers this issue: Per-page candidate dedup lets one tenant render on two consecutive authoritative search pages. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md` summary: Per-page candidate dedup lets one tenant render on two consecutive authoritative search pages. evidence: `TenantQueryGateway.BuildAuthoritativeSearchSnapshotAsync` builds its `seen` set per raw window, so a tenant the index returns in two overlapping windows is rendered twice across consecutive pages. Closing it needs either an index uniqueness guarantee (upstream, barred by this spec's Block-If) or a cross-page seen-set carried in the protected cursor, which would place reconstructable index material into protected state and violate this story's own cursor constraints. Reclassified from patch to deferred during the 2026-07-27 pass-2 application and marked `[x]` at `spec-1-9-…-paging.md:238`, but never entered this ledger — the pass-3 review found it invisible to ledger triage. Recorded here now. status: open — blocked on an upstream index uniqueness guarantee or a cursor design that carries no reconstructable index material.
status: open
decision: 2026-08-26 Upstream uniqueness — Make Memories guarantee stable unique tenant candidates and add an owning contract test.
decision: 2026-08-25 Upstream uniqueness — Make Memories guarantee stable unique tenant candidates and add a contract test.
disposition: blocked
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs BuildAuthoritativeSearchSnapshotAsync uses a per-window seen set. No cross-page uniqueness contract/test was found; the recorded 2026-08-25/26 decision chooses upstream Memories uniqueness.
verification-files: src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs
evidence-kind: verified-gap
priority: P2
owner: Memories
dependencies: Memories index uniqueness guarantee
next-action: Add an owning Memories uniqueness contract test across raw windows.
related: DW-114

### DW-123: A partially hidden search window still advertises, through a live Next control beside its surviving rows, that the window held more than it rendered

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-9-authoritative-memories-search-with-protected-paging (2026-07-27, pass 3)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md`
archived: 2026-10-12

### DW-124: The pass-2 finding "Seven new Lifecycle bindings unverified end-to-end on any real surface" was checked off after closing 1 of 13 binding sites

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-9-authoritative-memories-search-with-protected-paging (2026-07-27, pass 3)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md`
archived: 2026-10-12

### DW-125: A third divergent global-administrator claim parser now coexists with the existing two, so the same signed-in user can be a proven administrator for configuration and Indeterminate for tenant lifecycle

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of 1-6-read-only-tenant-configuration.md (2026-07-27)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/1-6-read-only-tenant-configuration.md`
archived: 2026-10-12

### DW-126: Lifecycle and global-administrator authorization reflections still read `HttpContext.User` with no circuit fallback, while the new configuration path has one

status: done 2026-10-12
origin: migrated from legacy ledger ("Deferred from: code review of 1-6-read-only-tenant-configuration.md (2026-07-27)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/1-6-read-only-tenant-configuration.md`
priority: P2
archived: 2026-10-12

### DW-127: Partially hidden authoritative-search windows disclose hidden candidates through paging state
origin: migrated from legacy ledger ("Deferred from: code review of 1-6-read-only-tenant-configuration (2026-07-28)"), 2026-08-25
location: TenantQueryGateway.cs:890
reason: The legacy ledger defers this issue: Partially hidden authoritative-search windows disclose hidden candidates through paging state. Original context is preserved in legacy-detail.
legacy-detail: - **Partially hidden authoritative-search windows disclose hidden candidates through paging state.** This is the already-recorded Story 1.9 `PARTIAL-WINDOW-DISCLOSURE-1.9` residual: `TenantQueryGateway.cs:890` renders surviving rows while retaining a `HasMore` value derived from the raw pre-authorization total. It is pre-existing relative to the Story 1.6 trust-boundary chunk and remains owned by Story 1.9.
status: open
decision: 2026-08-28 Await stronger requirement
decision: 2026-08-28 Await stronger requirement
disposition: awaiting-decision
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs still derives partially hidden-window HasMore from raw TotalCount and explicitly documents the residual disclosure channel. Both 2026-08-28 decisions say await a stronger requirement.
verification-files: src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs
evidence-kind: unverified-risk
priority: P2
owner: Story 1.9
dependencies: product/security requirement
next-action: Set the privacy/completeness acceptance requirement for partially hidden search windows.
related: DW-128, DW-618

### DW-128: Search hydration conflates forbidden and missing candidates when deciding whether to end paging
origin: migrated from legacy ledger ("Deferred from: code review of 1-6-read-only-tenant-configuration (2026-07-28)"), 2026-08-25
location: TenantQueryGateway.cs:1013
reason: The legacy ledger defers this issue: Search hydration conflates forbidden and missing candidates when deciding whether to end paging. Original context is preserved in legacy-detail.
legacy-detail: - **Search hydration conflates forbidden and missing candidates when deciding whether to end paging.** `TenantQueryGateway.cs:1013` classifies both 403 and 404 as `HiddenOrAbsent`; an all-404 stale-index window can therefore collapse paging and make later authorized matches unreachable. This is pre-existing relative to Story 1.6 and should be resolved with the Story 1.9 paging contract so anti-enumeration behavior remains coherent.
status: open
decision: 2026-08-26 Distinct internal results — Keep external responses identical but let only forbidden candidates terminate hidden-window paging.
decision: 2026-08-25 Distinct internal results — Keep external responses identical but let only forbidden candidates terminate hidden-window paging.
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs HydrateSearchCandidateAsync still merges Forbidden and NotFound into HiddenOrAbsent, so an all-404 stale window terminates paging. Existing 2026-08-25/26 decisions require distinct internal results without external enumeration.
verification-files: src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: coordinate DW-127 privacy contract
next-action: Separate internal absent/forbidden outcomes and test all-404 windows followed by authorized matches.
related: DW-127, DW-618

### DW-129: The release tag floor guard probes NuGet only; the container tag in registry.hexalith.com is never checked

status: done 2026-08-26
origin: migrated from legacy ledger ("Deferred from: code review of 1-6-read-only-tenant-configuration (2026-07-28)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-release-version-floor-drift.md`
archived: 2026-10-12

### DW-130: The tag floor is proved in verify-source but never re-proved after the production approval gate

status: done 2026-08-26
origin: migrated from legacy ledger ("Deferred from: code review of 1-6-read-only-tenant-configuration (2026-07-28)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-release-version-floor-drift.md`
archived: 2026-10-12

### DW-131: The guard fails on any published version above the floor, even when the version semantic-release would actually propose is free, with no override

status: done 2026-08-27
origin: migrated from legacy ledger ("Deferred from: code review of 1-6-read-only-tenant-configuration (2026-07-28)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-release-version-floor-drift.md`
archived: 2026-10-12

### DW-132: The release-published tenants container image fails to start under Production defaults, failing the container smoke test and aborting every release after packages are already pushed

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of 1-6-read-only-tenant-configuration (2026-07-28)"), 2026-08-25
archived: 2026-10-12

### DW-133: BMAD workflow render files (`_bmad/render/bmad-quick-dev/step-05-present.md`, `step-oneshot.md`, `workflow.md`) were modified inside the Story 1.10 diff, adding gitlink-validator instructions. Real and probably desirable, but it is tooling maintenance unrelated to Story 1.10's acceptance criteria and outside the spec's authorized doc outputs (evidence file + `tests/test-summary.md`). Should land as its own `docs` commit rather than inside a feature story (Hexalith commitlint forbids `chore`)
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-28)"), 2026-08-25
location: tests/test-summary.md; _bmad/render/bmad-quick-dev/step-05-present.md
reason: The legacy ledger defers this issue: BMAD workflow render files (`_bmad/render/bmad-quick-dev/step-05-present.md`, `step-oneshot.md`, `workflow.md`) were modified inside the Story 1.10 diff, adding gitlink-validator instructions. Original context is preserved in legacy-detail.
legacy-detail: - BMAD workflow render files (`_bmad/render/bmad-quick-dev/step-05-present.md`, `step-oneshot.md`, `workflow.md`) were modified inside the Story 1.10 diff, adding gitlink-validator instructions. Real and probably desirable, but it is tooling maintenance unrelated to Story 1.10's acceptance criteria and outside the spec's authorized doc outputs (evidence file + `tests/test-summary.md`). Should land as its own `docs` commit rather than inside a feature story (Hexalith commitlint forbids `chore`).
status: open
disposition: accepted-limitation
verification-date: 2026-10-12
verification: _bmad/render/bmad-quick-dev/step-05-present.md, step-oneshot.md and workflow.md still contain the gitlink instructions. This records historical scope attribution in the already-landed Story 1.10 diff, not a current functional defect; separating that old commit would require rewriting history. Original attribution is preserved.
evidence-kind: accepted-limitation
priority: P3
owner: unassigned
dependencies: none
next-action: Keep the historical attribution when archiving; use separately scoped tooling work for future changes.
related: none

### DW-134: Deferred to Story 1.11

status: done 2026-08-08
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-28)"), 2026-08-25
archived: 2026-10-12

### DW-135: Deferred to Story 1.11

status: done 2026-08-08
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-28)"), 2026-08-25
archived: 2026-10-12

### DW-136: `EventStore:BaseAddress` already accepted Aspire compound schemes before Story 1.10, but neither the EventStore gateway client nor the tenant command client attaches `.AddServiceDiscovery()`. A compound address can therefore be marked connected and fail when sent. This is real command/status transport debt, but it predates the active direct-read change and remains outside the chunk-1 patch set

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-29)"), 2026-08-25
archived: 2026-10-12

### DW-137: Future feature — reversible route identifiers:
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-29)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryClient.cs:530
reason: The legacy ledger defers this issue: Future feature — reversible route identifiers:. Original context is preserved in legacy-detail.
legacy-detail: - **Future feature — reversible route identifiers:** Define an explicit backend route contract for literal tenant/user identifiers containing `/`, then update the six direct-read endpoints and clients to round-trip that representation. Until this is delivered, the direct-read client must fail closed for this identifier class rather than issue an ambiguous encoded-slash request. Owner: future Tenants API route-contract work. Reason: future feature. [src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryClient.cs:530]
status: open
decision: 2026-08-27 Define reversible routes — Define and implement a reversible identifier route contract across all six API endpoints and clients, with slash and dot round-trip and traversal-safety tests.
decision: 2026-08-27 Define reversible routes — Define and implement a reversible identifier route contract across all six API endpoints and clients, with slash and dot round-trip and traversal-safety tests.
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryClient.cs still rejects ambiguous slash/all-dot route identifiers. The 2026-08-27 decision approves defining reversible routes, but there is no implemented six-route round-trip contract.
verification-files: src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryClient.cs
evidence-kind: recommendation
priority: P2
owner: future Tenants API route-contract work
dependencies: Tenants API route-contract design
next-action: Specify the reversible route representation and slash/dot/traversal tests before changing client/controller behavior.
related: none

### DW-138: Deferred to Story 1.11

status: done 2026-08-08
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-29, review loop 4)"), 2026-08-25
archived: 2026-10-12

### DW-139: Deferred to Story 1.11

status: done 2026-08-08
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-29, review loop 4)"), 2026-08-25
archived: 2026-10-12

### DW-140: Deferred to Story 1.11

status: done 2026-08-08
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-29, review loop 4)"), 2026-08-25
archived: 2026-10-12

### DW-141: Deferred to Story 1.11

status: done 2026-08-08
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-29, review loop 4)"), 2026-08-25
archived: 2026-10-12

### DW-142: Retained direct-read snapshots are scoped by entity, filter and paging inputs but not by the authenticated subject, so a principal change inside one scoped circuit can expose the previous subject's authorized rows during a failure or an insensitive `304` response

status: done 2026-10-12
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-29, core transport/state follow-up)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-1-10-direct-tenants-reads-and-authoritative-freshness.md`
priority: P1
archived: 2026-10-13

### DW-143: `EventStore:BaseAddress` is accepted with compound service-discovery schemes (e.g. `https+http://eventstore`) by the same `TryGetHttpBaseAddress` gate used for the read side, but no service discovery is attached to the command/status clients, so such a value can only fail at send time

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-30, chunk A+B transport/gateway)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-1-10-direct-tenants-reads-and-authoritative-freshness.md`
archived: 2026-10-12

### DW-144: Decide whether the Tenants UI BFF's six canonical reads should move from direct HTTPS to DAPR service invocation. Raised by the owner during the 1.10 review: DAPR is the intended discovery mechanism for services with sidecars. Not actionable inside 1.10 — it is a topology and security-posture change, not a base-address swap
origin: migrated from legacy ledger ("Architectural decision recorded by code review of spec-1-10 (2026-07-30) — BFF read transport vs DAPR service invocation"), 2026-08-25
location: src/Hexalith.Tenants.AppHost/Program.cs:104-106; src/Hexalith.Tenants.UI
source_spec: `_bmad-output/implementation-artifacts/spec-1-10-direct-tenants-reads-and-authoritative-freshness.md`
reason: The legacy ledger defers this issue: Decide whether the Tenants UI BFF's six canonical reads should move from direct HTTPS to DAPR service invocation. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-10-direct-tenants-reads-and-authoritative-freshness.md` summary: Decide whether the Tenants UI BFF's six canonical reads should move from direct HTTPS to DAPR service invocation. Raised by the owner during the 1.10 review: DAPR is the intended discovery mechanism for services with sidecars. Not actionable inside 1.10 — it is a topology and security-posture change, not a base-address swap. evidence: | Current topology (verified 2026-07-30): - `tenants-api` HAS a DAPR sidecar, `AppId = "tenants-api"` (src/Hexalith.Tenants.AppHost/Program.cs:104-106). - `tenants-ui` has NO sidecar, no app-id, and no Dapr package references anywhere in src/Hexalith.Tenants.UI. It is not a DAPR app; it is a Blazor front end / BFF. - `deploy/dapr/accesscontrol.tenants.yaml` is `defaultAction: deny` and allows exactly one caller, `appId: eventstore`, on five POST operations (/process, /project, /query, /replay-state, /admin/operational-index-metadata). None of the six `GET /api/tenants*` read routes are allowed and `tenants-ui` has no policy entry. - Because the reads go direct HTTPS to `tenants-api` (which is `.WithExternalHttpEndpoints()`), they bypass the DAPR access-control plane and mTLS entirely. That is a deviation from the documented deny-by-default posture and was not recorded anywhere in Story 1.10. What a move to DAPR invoke would require: 1. A sidecar + app-id for `tenants-ui`. 2. A `tenants-ui` policy in accesscontrol.tenants.yaml allowing GET on the six read routes, plus the route tests project-context.md requires to change alongside any app-id/topic change. 3. Base address becomes `http://localhost:{daprHttpPort}/v1.0/invoke/tenants-api/method/` + route. Route identity at the API is preserved so the six-path acceptance criterion survives, but the client's URI building, base-path retention and scheme gate all assume a direct service address. 4. Reconciling `deploy/dapr/resiliency.yaml`, which applies `defaultRetry` (constant, 3 retries), a 5s `daprSidecar` timeout and a circuit breaker to invoke targets, with Story 1.10's deliberate transport semantics: the hand-built linked deadline, the fixed support-safe failure categories, and the explicit never-silently-retry invariant (notably the invalid-cursor rule). A retried conditional GET re-sends `If-None-Match`, so 304/ETag behaviour through the sidecar must be verified, not assumed. Not verified, flagged for that work: how `%2E%2E` behaves through a DAPR invoke path. If anything it is worse than direct HTTP — a resolved `..` could traverse out of the `/v1.0/invoke/{appId}/method/` prefix — so the reject-all-dot route-value patch is required regardless of the transport chosen. status: open — owner-raised architectural decision for its own story. Story 1.10 proceeds with the resolved option (c): no discovery mechanism, consuming the AppHost-injected resolved endpoint URL, matching the EventStore command/status and Memories clients.
status: open
decision: 2026-08-26 Use Dapr invocation — Add a tenants-ui sidecar and app ID, deny-by-default GET policy, resilience reconciliation, and route and ETag tests.
decision: 2026-08-25 Use Dapr invocation — Add a tenants-ui sidecar and app-id, deny-by-default GET policy, resilience reconciliation, and route and ETag tests.
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.AppHost/Program.cs gives tenants-api a Dapr sidecar, while tenants-ui remains a ProjectResource without one and receives Tenants__BaseAddress from the HTTPS endpoint. Recorded 2026-08-25/26 decisions select Dapr invocation; migration is still absent.
evidence-kind: recommendation
priority: P2
owner: unassigned
dependencies: topology/security contract and DW-137
next-action: Implement the already-selected topology in a dedicated owning change with deny-default GET policy, retry and ETag/route checks.
related: DW-161

### DW-145: Deferred to Story 1.11

status: done 2026-08-08
origin: migrated from legacy ledger ("Architectural decision recorded by code review of spec-1-10 (2026-07-30) — BFF read transport vs DAPR service invocation"), 2026-08-25
archived: 2026-10-12

### DW-146: Member evidence gate collapses lifecycle and permission reasons into "stale data"

status: done 2026-09-02
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-30)"), 2026-08-25
archived: 2026-10-12

### DW-147: Mobile read-only for platform-authority mutations is enforced only by CSS

status: done 2026-08-28
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-30)"), 2026-08-25
archived: 2026-10-12

### DW-148: Deferred to Story 1.11

status: done 2026-08-08
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-30)"), 2026-08-25
archived: 2026-10-12

### DW-149: Read-refresh lease retry is unverified on the tenant detail page

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of 1-6-read-only-tenant-configuration (2026-07-31)"), 2026-08-25
archived: 2026-10-12

### DW-150: An in-flight `RefreshTenantReadsAsync` is aborted silently by a concurrent detail refresh

status: done 2026-10-15
origin: migrated from legacy ledger ("Deferred from: code review of 1-6-read-only-tenant-configuration (2026-07-31)"), 2026-08-25
priority: P2
archived: 2026-10-15

### DW-151: Undeclared `references/Hexalith.EventStore` gitlink bump in the working tree

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of 1-6-read-only-tenant-configuration (2026-07-31)"), 2026-08-25
archived: 2026-10-12

### DW-152: Composition availability-pair guard is logically asymmetric

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-31)"), 2026-08-25
archived: 2026-10-12

### DW-153: Member mutation flows are outside the projection-lifecycle policy

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-31)"), 2026-08-25
archived: 2026-10-12

### DW-154: Two global-administrator teardown paths are knowingly unverified
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-31)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:997
reason: The legacy ledger defers this issue: Two global-administrator teardown paths are knowingly unverified. Original context is preserved in legacy-detail.
legacy-detail: - **Two global-administrator teardown paths are knowingly unverified** — un-marshalling the `ResetPagingAsync` cursor-history clear off the dispatcher, and neutering the `ObjectDisposedException` catch filter on the notification-refresh teardown, both survived the full UI suite. Reason for deferral: bUnit's single-threaded renderer cannot reproduce either race, so these are untestable at the current harness level rather than merely uncovered. Revisit if: a concurrency-capable component harness lands, or either path produces a live defect. Until then the code comments should say "unverified" rather than implying coverage. [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:997]
status: open
disposition: blocked
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor retains dispatcher-sensitive reset/notification teardown paths. Original mutation evidence says bUnit remains green without those guards; no concurrent teardown proof supersedes it.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: concurrent teardown harness
next-action: Use a concurrency-capable renderer/browser harness to pin reset and disposed-notification behavior.
related: none

### DW-155: Paging guards widened to `internal` for direct test access
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-31)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor:585
reason: The legacy ledger defers this issue: Paging guards widened to `internal` for direct test access. Original context is preserved in legacy-detail.
legacy-detail: - **Paging guards widened to `internal` for direct test access** — `MemberAccessReview`'s paging guards were promoted from private to internal so the test project could invoke them directly; the accompanying comment concedes every guard could be deleted with the suite still green. Reason for deferral: pre-existing test-design debt, not introduced behaviour. The guards remain unobservable through the rendered affordance, so the coverage they now have does not prove the control behaves correctly — but narrowing them again without a rendered-affordance test would lose coverage. Revisit if: the member pager gains bUnit tests that drive it through its rendered controls. [src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor:585]
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor RequestNextPageAsync and RequestPreviousPageAsync remain internal, with comments explaining pre-render click guards. Direct invocation still does not prove the rendered disabled-control behavior.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor
evidence-kind: verified-gap
priority: P3
owner: unassigned
dependencies: none
next-action: Drive member paging through rendered controls while a command/page request is pending.
related: DW-610

### DW-156: Command `SafeMessage` values are hardcoded English literals rather than `TenantsResources.resx` entries

status: done 2026-08-27
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-31)"), 2026-08-25
archived: 2026-10-12

### DW-157: `RestQueryClientAdapter` carries 13 lines of dead freshness computation that re-implement `TenantsRestQueryClient.ResolveFreshness`, discard the result, and omit the `IsDegraded == true` collapse both production implementations perform — so it will drift silently while reading as if it models the client

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-31)"), 2026-08-25
archived: 2026-10-12

### DW-158: `WaitForAsync` reports a slow agent as a raw `TaskCanceledException` from `Task.Delay` rather than as a named unmet condition, so a genuinely flaky subscription test reports as an infrastructure error
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-31)"), 2026-08-25
location: tests/Hexalith.Tenants.UI.Tests/Services/TenantReadRefreshSubscriptionTests.cs:317
reason: The legacy ledger defers this issue: `WaitForAsync` reports a slow agent as a raw `TaskCanceledException` from `Task.Delay` rather than as a named unmet condition, so a genuinely flaky subscription test reports as an infrastructure error. Original context is preserved in legacy-detail.
legacy-detail: - `WaitForAsync` reports a slow agent as a raw `TaskCanceledException` from `Task.Delay` rather than as a named unmet condition, so a genuinely flaky subscription test reports as an infrastructure error. Reason for deferral: diagnostics-only. No production behaviour is left unverified by it. Revisit if: the subscription tests start failing intermittently in CI and the cause needs to be readable from the failure message alone. [tests/Hexalith.Tenants.UI.Tests/Services/TenantReadRefreshSubscriptionTests.cs:317]
status: open
disposition: actionable
verification-date: 2026-10-12
verification: tests/Hexalith.Tenants.UI.Tests/Services/TenantReadRefreshSubscriptionTests.cs WaitForAsync still uses Task.Delay(10, timeout.Token) without translating expiry into a named unmet-condition assertion.
verification-files: tests/Hexalith.Tenants.UI.Tests/Services/TenantReadRefreshSubscriptionTests.cs
evidence-kind: verified-defect
priority: P3
owner: unassigned
dependencies: none
next-action: Return a diagnostic assertion naming the condition when the two-second wait expires.
related: none

### DW-159: `TenantConfigurationView.StateResourcePrefix` has no arm for `TenantDetailSurfaceKind.NotFound` or `Unauthorized`, so both fall through to `Tenants.Configuration.State.Ready` ("Configuration evidence is current") and are announced assertively, because `!CanInspect` puts `LivePoliteness` in the escalated set, over a surface that has no rows
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-31, loop 10 never-reviewed delta)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor:220
reason: The legacy ledger defers this issue: `TenantConfigurationView.StateResourcePrefix` has no arm for `TenantDetailSurfaceKind.NotFound` or `Unauthorized`, so both fall through to `Tenants.Configuration.State.Ready` ("Configuration evidence is current") and are announced assertively, because `!CanInspect` puts `LivePoliteness` in the escalated set, over a surface that has no rows. Original context is preserved in legacy-detail.
legacy-detail: - `TenantConfigurationView.StateResourcePrefix` has no arm for `TenantDetailSurfaceKind.NotFound` or `Unauthorized`, so both fall through to `Tenants.Configuration.State.Ready` ("Configuration evidence is current") and are announced **assertively**, because `!CanInspect` puts `LivePoliteness` in the escalated set, over a surface that has no rows. Reason for deferral: pre-existing arms, not introduced by this range, and unreachable through the only current consumer — `TenantDetailSnapshot.NotFound`/`Unauthorized` route through `Empty(...)`, which yields an unavailable safe model, so the Unavailable arm wins before the fall-through is reached. Revisit if: the second consumer the file's own comment anticipates arrives, or any caller passes those surface kinds with an available configuration model. [src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor:220]
status: open
disposition: accepted-limitation
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor StateResourcePrefix still omits explicit NotFound/Unauthorized arms; State/TenantDetail/TenantDetailSnapshot.cs Empty supplies an unavailable safe model, so current consumers select Unavailable before Ready. The original latent second-consumer condition remains.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor; src/Hexalith.Tenants.UI/State/TenantDetail/TenantDetailSnapshot.cs
evidence-kind: accepted-limitation
priority: P3
owner: unassigned
dependencies: none
next-action: Require explicit state mapping if a second consumer passes an available model with a denied/not-found surface.
related: none

### DW-160: Replace `CapturingGatewayClient` + `RestQueryClientAdapter` in `TenantQueryGatewayTests` with a substitute
origin: migrated from legacy ledger ("Deferred from: review repair loop 11 of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-31)"), 2026-08-25
location: tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs:6093
reason: The legacy ledger defers this issue: Replace `CapturingGatewayClient` + `RestQueryClientAdapter` in `TenantQueryGatewayTests` with a substitute. Original context is preserved in legacy-detail.
legacy-detail: - Replace `CapturingGatewayClient` + `RestQueryClientAdapter` in `TenantQueryGatewayTests` with a substitute of the real `ITenantsRestQueryClient`. The adapter still re-implements the generic `SubmitQueryRequest` transport Story 1.10 deleted, so failures can only be injected as `EventStoreGatewayException` — a type the real client never throws — and the roughly sixty tests it drives exercise only the success arm of `ToEventStoreResult`. Reason for deferral: this is the reason review loop 10 itself gave when it reopened the item. Replacing the harness rewrites the fixture of about sixty tests in one change, which deserves its own pass and its own review rather than riding along with unrelated repairs. The *misleading* half is already closed: the 23 inert `Request.*` assertions and the adapter's dead freshness ladder are gone, and every failure-kind mapping repaired in loops 9–11 was driven through the production seam with `FixedFailureRestQueryClient` or an `ITenantsRestQueryClient` substitute. What remains is structural test debt, not a false claim. Revisit if: a further failure-mapping change is needed at the gateway seam, or the adapter drifts from `ResolveFreshness` again. [tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs:6093]
status: open
disposition: actionable
verification-date: 2026-10-12
verification: tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs still defines CapturingGatewayClient and RestQueryClientAdapter, alongside real REST substitutes for failure mapping. The structural fixture debt remains, without invalidating the production-seam failure tests.
verification-files: tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs
evidence-kind: recommendation
priority: P3
owner: unassigned
dependencies: none
next-action: Replace adapter-backed cases with ITenantsRestQueryClient substitutes when the gateway fixture is next maintained.
related: none

### DW-161: Live socket-level proof that all six direct Tenants REST routes answer through the deployed topology
origin: migrated from legacy ledger ("Deferred from: review repair loop 11 of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-31)"), 2026-08-25
location: tests/Hexalith.Tenants.IntegrationTests/AspireTopologyTests.cs:421
reason: The legacy ledger defers this issue: Live socket-level proof that all six direct Tenants REST routes answer through the deployed topology. Original context is preserved in legacy-detail.
legacy-detail: - Live socket-level proof that all six direct Tenants REST routes answer through the deployed topology. Reason for deferral: recorded as an owned limitation under decision `spec:854`, option (b). The routes are proven in process against the real generated controllers — paths, query strings, metadata headers and the conditional `304` path — by `TenantsApiGeneratedControllerTests.Direct_rest_client_routes_match_the_generated_controllers_and_parse_their_real_headers`. A live probe driving the production `TenantsRestQueryClient` against the `tenants-api` Aspire resource was written for loop 11 and every read times out at the client's 60 s bound in the local slim-mode topology, which also intermittently fails the pre-existing command-status wait in `AspireTopologyTests`. A lane that cannot separate "the routes are broken" from "the topology is unhealthy" is not evidence, so it was not shipped. Revisit if: a reliable Aspire topology lane exists (CI or local) that can serve as an oracle for `tenants-api`. [tests/Hexalith.Tenants.IntegrationTests/AspireTopologyTests.cs:421]
status: open
disposition: blocked
verification-date: 2026-10-12
verification: tests/Hexalith.Tenants.IntegrationTests/TenantsApiGeneratedControllerTests.cs Direct_rest_client_routes_match_the_generated_controllers_and_parse_their_real_headers proves six routes in process. AspireTopologyTests.cs does not supply the requested healthy live six-route production-client packet.
verification-files: tests/Hexalith.Tenants.IntegrationTests/TenantsApiGeneratedControllerTests.cs; tests/Hexalith.Tenants.IntegrationTests/AspireTopologyTests.cs
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: healthy live topology and endpoint/auth evidence
next-action: Capture all six production-client routes, conditional 304 and headers through a healthy authenticated deployed topology.
related: DW-104, DW-144

### DW-162: `scripts/validate-story-gitlinks.py` keeps no automated test and no CI wiring

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-08-01)"), 2026-08-25
archived: 2026-10-12

### DW-163: A tenant configuration key written by a producer other than this UI, carrying an invisible separator or other untypeable character, remains permanently unremovable through the UI

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-08-01)"), 2026-08-25
archived: 2026-10-12

### DW-164: A route change while the prior tenant's refresh subscription is pending can leave the new tenant without projection auto-refresh

status: done 2026-10-15
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review (2026-08-01)"), 2026-08-25
priority: P2
archived: 2026-10-15

### DW-165: `TenantAuditPage` is the last production consumer of the synchronous `HttpContext`-only

status: done 2026-10-12
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review, loop 2 (2026-08-01)"), 2026-08-25
priority: P2
archived: 2026-10-12

### DW-166: `EnsureReadRefreshLeaseAsync` calls `SubscribeAsync` with `CancellationToken.None` and no timeout

status: done 2026-10-15
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review, loop 2 (2026-08-01)"), 2026-08-25
priority: P2
archived: 2026-10-15

### DW-167: The grant and remove submit buttons are never disabled while a mutation is in flight

status: done 2026-08-01
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review, loop 2 (2026-08-01)"), 2026-08-25
archived: 2026-10-12

### DW-168: `TenantDetailPage.IsSafeReturnUrl` accepts any string with a `/tenants` prefix — including

status: done 2026-10-12
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review, loop 2 (2026-08-01)"), 2026-08-25
priority: P2
archived: 2026-10-12

### DW-169: On narrow viewports the per-row Remove launcher is hidden by CSS with no per-row localized reason; the actions

status: done 2026-08-08
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review, loop 2 (2026-08-01)"), 2026-08-25
archived: 2026-10-12

### DW-170: Multi-page populations can permanently land grant/remove confirmation in page-scoped `UnableToVerify`

status: done 2026-09-02
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review (2026-08-08, loop 5 chunk 2)"), 2026-08-25
archived: 2026-10-12

### DW-171: UnableToVerify copy mentions confirming via the tenant audit trail without an in-page navigation link

status: done 2026-09-02
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review (2026-08-08, loop 5 chunk 2)"), 2026-08-25
archived: 2026-10-12

### DW-172: A `Ready` snapshot reporting `HasMore == true` with a blank `NextCursor` is a silent dead end. Loop 2 correctly

status: done 2026-08-08
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review (2026-08-01, loop 3)"), 2026-08-25
archived: 2026-10-12

### DW-173: Authorization resolution is uncancellable from both consuming pages. The loop-2 patch made

status: done 2026-10-15
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review (2026-08-01, loop 3)"), 2026-08-25
priority: P2
archived: 2026-10-15

### DW-174: AC5 remains partially unmet while the story sits in `review`: the narrow-viewport per-row Remove reason gap

status: done 2026-08-08
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review (2026-08-01, loop 3)"), 2026-08-25
archived: 2026-10-12

### DW-175: TenantsWorkspace nests ProjectionLifecycleBadge inside polite atomic status region — deferred, pre-existing lifecycle-badge composition (not core 1.11 auth)

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review.md (2026-08-08, loop 6 chunk 3)"), 2026-08-25
archived: 2026-10-12

### DW-176: Workspace GA entry resolve calls ResolveGlobalAdministratorsAuthorizationAsync without CancellationToken — deferred, pre-existing fire-and-forget entry path; version/_disposed still gate apply

status: done 2026-10-15
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review.md (2026-08-08, loop 6 chunk 3)"), 2026-08-25
priority: P2
archived: 2026-10-16

### DW-177: Soft `RefreshAsync` blanks the tenant list via Loading/ShowList — deferred, pre-existing UX; workspace never had retainConfirmed
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review.md (2026-08-08, loop 6 chunk 3)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:679
reason: The legacy ledger defers this issue: Soft `RefreshAsync` blanks the tenant list via Loading/ShowList — deferred, pre-existing UX; workspace never had retainConfirmed. Original context is preserved in legacy-detail.
legacy-detail: - Soft `RefreshAsync` blanks the tenant list via Loading/ShowList — deferred, pre-existing UX; workspace never had retainConfirmed. Distinct from the LoadAsync version-bump stale-apply window below (same approximate line region, different method). [src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:679]
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor RefreshAsync calls LoadAsync, which assigns TenantListSnapshot.Loading and clears visible rows; retained evidence exists but soft refresh still blanks the list.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor
evidence-kind: recommendation
priority: P2
owner: unassigned
dependencies: none
next-action: Choose/pin retained-list refresh behavior and preserve visible evidence during a same-scope soft refresh.
related: DW-119

### DW-178: StartLifecycleAuthorizationResolution runs on every OnParametersSetAsync without TenantId short-circuit — deferred, mitigated by generation + CTS replace
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review.md (2026-08-08, loop 6 chunk 3)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:339
reason: The legacy ledger defers this issue: StartLifecycleAuthorizationResolution runs on every OnParametersSetAsync without TenantId short-circuit — deferred, mitigated by generation + CTS replace. Original context is preserved in legacy-detail.
legacy-detail: - StartLifecycleAuthorizationResolution runs on every OnParametersSetAsync without TenantId short-circuit — deferred, mitigated by generation + CTS replace [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:339]
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor OnParametersSetAsync still starts lifecycle authority resolution on same-tenant parameter updates; replaceable CTS/generation guards protect stale application.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor
evidence-kind: recommendation
priority: P3
owner: unassigned
dependencies: none
next-action: Short-circuit unchanged authority scope or document why each parameter update requires a fresh resolve.
related: none

### DW-179: IsSafeReturnUrl accepts any /tenants-prefixed path — deferred, already deferred earlier; same-origin relative only

status: done 2026-10-12
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review.md (2026-08-08, loop 6 chunk 3)"), 2026-08-25
priority: P2
archived: 2026-10-12

### DW-180: `LoadAsync` version bump after `BeginLoad` leaves a stale-apply window — deferred, pre-existing workspace load pattern
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review.md (2026-08-08, loop 6 chunk 3)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:679
reason: The legacy ledger defers this issue: `LoadAsync` version bump after `BeginLoad` leaves a stale-apply window — deferred, pre-existing workspace load pattern. Original context is preserved in legacy-detail.
legacy-detail: - `LoadAsync` version bump after `BeginLoad` leaves a stale-apply window — deferred, pre-existing workspace load pattern. Distinct from soft `RefreshAsync` blanking above (same approximate line region, different method). [src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:679]
status: open
disposition: uncertain
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor LoadAsync still calls BeginLoad before incrementing _loadVersion; the ordering is confirmed, but no held cancellation/dispatcher reproduction establishes a stale snapshot application in that interval.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor
evidence-kind: unverified-risk
priority: P2
owner: unassigned
dependencies: none
next-action: Create a deterministic cancellation callback interleaving and assert the older load cannot apply.
related: none

### DW-181: TenantDetailPage BeginLoad deferred-CTS disposal lacks a workspace-equivalent runtime test — deferred, coverage gap only
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review.md (2026-08-08, loop 6 chunk 3)"), 2026-08-25
location: tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs
reason: The legacy ledger defers this issue: TenantDetailPage BeginLoad deferred-CTS disposal lacks a workspace-equivalent runtime test — deferred, coverage gap only. Original context is preserved in legacy-detail.
legacy-detail: - TenantDetailPage BeginLoad deferred-CTS disposal lacks a workspace-equivalent runtime test — deferred, coverage gap only [tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs]
status: open
disposition: actionable
verification-date: 2026-10-12
verification: tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs exercises route/load cancellation, but the explicit workspace-equivalent outstanding-token deferred-disposal assertion is not established for TenantDetailPage BeginLoad.
verification-files: tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs
evidence-kind: verified-gap
priority: P3
owner: unassigned
dependencies: none
next-action: Drive a real held detail read through supersession/disposal and assert token lifetime through completion.
related: none

### DW-182: EditTenantMetadataFlow Lifecycle wiring not asserted via tenants-edit-metadata-open on the detail page — deferred, covered by EditTenantMetadataFlowTests

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review.md (2026-08-08, loop 6 chunk 3)"), 2026-08-25
archived: 2026-10-12

### DW-183: Route change during in-flight prior-tenant subscribe can briefly miss auto-refresh — deferred, previously deferred; OnAfterRender retry partially mitigates

status: done 2026-10-15
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review.md (2026-08-08, loop 6 chunk 3)"), 2026-08-25
priority: P2
archived: 2026-10-15

### DW-184: ApplyAuthenticationStateChangedAsync awaits authenticationStateTask with no timeout — deferred, pre-existing; fail-closed hides entry until auth completes

status: done 2026-10-15
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review.md (2026-08-08, loop 6 chunk 3)"), 2026-08-25
priority: P2
archived: 2026-10-16

### DW-185: Member Next stays enabled when HasMore has blank NextCursor — deferred, MemberAccessReview not in this story File List; page already no-ops; pre-existing pager honesty gap

status: done 2026-08-27
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review.md (2026-08-08, loop 6 chunk 3)"), 2026-08-25
archived: 2026-10-12

### DW-186: Coarse `StaleData` category for every non-Current projection lifecycle — deferred, pre-existing; message key is specific (`ProjectionLifecycle`) but category chip stays StaleData
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-12-projection-lifecycle-badges.md (2026-08-08)"), 2026-08-25
location: src/Hexalith.Tenants.UI/State/TenantDetail/TenantLifecycleAvailability.cs:64
reason: The legacy ledger defers this issue: Coarse `StaleData` category for every non-Current projection lifecycle — deferred, pre-existing; message key is specific (`ProjectionLifecycle`) but category chip stays StaleData. Original context is preserved in legacy-detail.
legacy-detail: - Coarse `StaleData` category for every non-Current projection lifecycle — deferred, pre-existing; message key is specific (`ProjectionLifecycle`) but category chip stays StaleData [`src/Hexalith.Tenants.UI/State/TenantDetail/TenantLifecycleAvailability.cs:64`]
status: open
decision: 2026-09-02 Add lifecycle category — Add a dedicated projection-lifecycle category and migrate mappings, chips, resources, and tests compatibly.
decision: 2026-09-02 Add lifecycle category — Add a dedicated projection-lifecycle category and migrate mappings, chips, resources, and tests compatibly.
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/State/TenantDetail/TenantLifecycleAvailability.cs still maps shared StaleData to TenantLifecycleUnavailableReasonCategory.StaleData even for ProjectionLifecycle-specific copy. Existing 2026-09-02 decision selects a dedicated lifecycle category.
verification-files: src/Hexalith.Tenants.UI/State/TenantDetail/TenantLifecycleAvailability.cs
evidence-kind: verified-defect
priority: P3
owner: unassigned
dependencies: none
next-action: Add the recorded lifecycle reason category with compatible resource/chip mappings.
related: none

### DW-187: Open Set/Remove/Edit flows do not reset when lifecycle flips mid-flight (only lifecycle-action re-evals) — deferred, pre-existing command-flow pattern beyond this story's badge split
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-12-projection-lifecycle-badges.md (2026-08-08)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor
reason: The legacy ledger defers this issue: Open Set/Remove/Edit flows do not reset when lifecycle flips mid-flight (only lifecycle-action re-evals) — deferred, pre-existing command-flow pattern beyond this story's badge split. Original context is preserved in legacy-detail.
legacy-detail: - Open Set/Remove/Edit flows do not reset when lifecycle flips mid-flight (only lifecycle-action re-evals) — deferred, pre-existing command-flow pattern beyond this story's badge split [`src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor`]
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor and Configuration/SetTenantConfigurationFlow.razor retain open editor intent through lifecycle updates; submit gates recheck availability, but no common editor invalidation contract resets those open flows.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor; src/Hexalith.Tenants.UI/Components/Tenants/Configuration/SetTenantConfigurationFlow.razor
evidence-kind: recommendation
priority: P2
owner: unassigned
dependencies: shared open-editor lifecycle contract
next-action: Specify and test mid-edit lifecycle-change handling across metadata/set/remove without discarding an accepted attempt.
related: none

### DW-188: CorrectionStartPanel still submits membership corrections without reusable messageId tracking

status: done 2026-10-12
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-1-reverify-projection-confirmed-membership-command-foundation.md (2026-08-08)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
priority: P2
archived: 2026-10-12

### DW-189: Projection-version advancement is opaque inequality only, with no causal/audit-qualified branch

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-1-reverify-projection-confirmed-membership-command-foundation.md (2026-08-08)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
archived: 2026-10-12

### DW-190: AggregateAdmissionGate falls back to a page-private instance when DI resolution fails

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-1-reverify-projection-confirmed-membership-command-foundation.md (2026-08-08)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
archived: 2026-10-12

### DW-191: SignalR nudge is skipped when MemberAccessReview ref is still null
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-1-reverify-projection-confirmed-membership-command-foundation.md (2026-08-08)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
reason: The legacy ledger defers this issue: SignalR nudge is skipped when MemberAccessReview ref is still null. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md` summary: SignalR nudge is skipped when MemberAccessReview ref is still null. evidence: TenantDetailPage forwards only when `_memberAccessReview is not null`; early refresh before child attach can miss an in-flight nudge.
status: open
disposition: uncertain
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor forwards member nudges only when _memberAccessReview is nonnull; current key/lease timing is not covered by an early-nudge-before-child-attach reproduction.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor
evidence-kind: unverified-risk
priority: P2
owner: unassigned
dependencies: none
next-action: Hold child attachment, deliver a nudge during a retained attempt, and show whether the subsequent authoritative read reconciles it.
related: none

### DW-192: The mandatory story gitlink validator fails against current HEAD because seven unrelated submodule pointer bumps landed after the isolated Story 2.1 commit

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-1-reverify-projection-confirmed-membership-command-foundation.md (2026-08-19)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
archived: 2026-10-12

### DW-193: Story 2.4b — provenance reconciliation refinements, WP-2A removal-proof assembly (`audit_available`), proof-capability fail-closed gating, and proof-state recovery/tests

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: bmad-build scope split of Story 2.4 (2026-08-08)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md`
archived: 2026-10-12

### DW-194: Document-level Tab trapping / inert backdrop beyond sentinel pattern for remove-member dialog
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md (2026-08-08)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md`
reason: The legacy ledger defers this issue: Document-level Tab trapping / inert backdrop beyond sentinel pattern for remove-member dialog. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md` summary: Document-level Tab trapping / inert backdrop beyond sentinel pattern for remove-member dialog. evidence: Spec Ask First prefers Tenants role=dialog + focus-sentinel pattern; full Fluent/FrontComposer modal primitive was not authorized in this slice.
status: open
decision: 2026-08-26 Adopt shared modal — Adopt or create a Fluent or FrontComposer modal with document-level trapping, inert backdrop, and browser tests.
decision: 2026-08-25 Adopt shared modal — Adopt or create a Fluent or FrontComposer modal with document-level trapping, inert backdrop, and browser tests.
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor still uses focus sentinels and role=dialog, without a shared document trap/inert backdrop. Existing 2026-08-25/26 decisions select a shared Fluent/FrontComposer modal.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor
evidence-kind: recommendation
priority: P2
owner: unassigned
dependencies: FrontComposer modal primitive and browser fixture
next-action: Adopt the recorded shared modal contract and obtain browser Tab/Escape/inert evidence.
related: DW-227, DW-241, DW-242

### DW-195: JS/viewport submit-time narrow-layout guard beyond CSS fail-closed for remove-member
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md (2026-08-08)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md`
reason: The legacy ledger defers this issue: JS/viewport submit-time narrow-layout guard beyond CSS fail-closed for remove-member. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md` summary: JS/viewport submit-time narrow-layout guard beyond CSS fail-closed for remove-member. evidence: Matches established RemoveTenantConfigurationFlow CSS-only narrow gate; changing to a runtime viewport gate needs an explicit Ask First decision.
pre-b7-verification-date: 2026-10-12
pre-b7-verification: src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor still has no measured viewport gate at submit; its stylesheet alone hides the form. Existing 2026-08-25/26 decisions require fail-closed runtime measurement.
pre-b7-next-action: Use the selected viewport observation gate and refuse unknown/narrow submissions.
status: done 2026-10-14 (B7 closure)
decision: 2026-08-26 Runtime viewport gate — Reuse viewport observation and refuse submit whenever measured safety is unknown or narrow.
decision: 2026-08-25 Runtime viewport gate — Reuse the high-impact viewport observation pattern and refuse submit whenever measured safety is unknown or narrow.
disposition: done
verification-date: 2026-10-14
verification: B7 reuses TenantHighImpactViewportObservation and requires HasMeasurement plus Safe for direct submit/retry and after awaited admission. Missing, unmeasured, measured Unknown, Phone and resize cases prove zero dispatch; safe measured submissions and retained proof pass. See b7-validation-evidence.md.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor
evidence-kind: verified-closure
priority: P2
owner: unassigned
dependencies: shared viewport observation
next-action: None for this finding; validated B7 closure does not imply release readiness.
closure-evidence: `_bmad-output/implementation-artifacts/b7-validation-evidence.md`; `_bmad-output/implementation-artifacts/spec-b7-live-admission.md`
related: DW-242

### DW-196: Remove-member WP-2A assembly only inspects the first audit page; matching UserRemovedFromTenant rows on later pages can leave proof pending

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4b-wp-2a-removal-proof-and-audit-available.md (2026-08-08)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md`
archived: 2026-10-12

### DW-197: Proof-capability fail-closed detects only null/UnavailableTenantQueryGateway, not a live stale/unknown audit-capability probe before open

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4b-wp-2a-removal-proof-and-audit-available.md (2026-08-08)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md`
archived: 2026-10-12

### DW-198: Default `validate-story-gitlinks.py` execution compares the Story 2.4 baseline to the later repository `HEAD` and reports seven undeclared `references/` pointer moves made by post-story dependency commits. Exact story-range validation with `--ref fa5ca559` passes for both 2.4 specifications with no gitlink changes. Keep the later dependency bumps outside Story 2.4 review scope; make story-end range selection durable if old stories must remain independently re-reviewable after `main` advances
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md (2026-08-19)"), 2026-08-25
location: validate-story-gitlinks.py; references/
reason: The legacy ledger defers this issue: Default `validate-story-gitlinks.py` execution compares the Story 2.4 baseline to the later repository `HEAD` and reports seven undeclared `references/` pointer moves made by post-story dependency commits. Original context is preserved in legacy-detail.
legacy-detail: - Default `validate-story-gitlinks.py` execution compares the Story 2.4 baseline to the later repository `HEAD` and reports seven undeclared `references/` pointer moves made by post-story dependency commits. Exact story-range validation with `--ref fa5ca559` passes for both 2.4 specifications with no gitlink changes. Keep the later dependency bumps outside Story 2.4 review scope; make story-end range selection durable if old stories must remain independently re-reviewable after `main` advances.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: scripts/validate-story-gitlinks.py defaults legacy stories to HEAD, while --ref permits exact historical ranges. The recorded-commit mode added later does not automatically assign an end revision to these older 2.4 records.
verification-files: scripts/validate-story-gitlinks.py
evidence-kind: recommendation
priority: P3
owner: unassigned
dependencies: historical story-range attribution
next-action: Record an immutable story-end range or migrate the historical artifact to owned-commit provenance; keep later dependency moves separate.
related: DW-246

### DW-199: AggregateIdentity-scoped create admission lock through terminal evidence (unrelated aggregates may proceed; replace create-local submitting flag with TenantAggregateCommandAdmissionGate)

status: done 2026-10-13
origin: migrated from legacy ledger ("Deferred from: bmad-build scope split of Story 3.1 (2026-08-08)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md`
priority: P1
archived: 2026-10-13

### DW-200: Workspace SignalR / read-refresh nudge wiring into CreateTenantFlow (ApplySignalRNudge + authoritative re-query; never notify-alone confirm)
origin: migrated from legacy ledger ("Deferred from: bmad-build scope split of Story 3.1 (2026-08-08)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md`
reason: The legacy ledger defers this issue: Workspace SignalR / read-refresh nudge wiring into CreateTenantFlow (ApplySignalRNudge + authoritative re-query; never notify-alone confirm). Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md` summary: Workspace SignalR / read-refresh nudge wiring into CreateTenantFlow (ApplySignalRNudge + authoritative re-query; never notify-alone confirm). evidence: Split from Story 3.1; membership already has the pattern, and create confirmation honesty can ship before host nudge plumbing.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor declares ApplySignalRNudge, but Components/Pages/TenantsWorkspace.razor has no production caller or create read-refresh subscription. Manual status refresh remains the live proof path.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor; src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: none
next-action: Wire workspace notification nudges to create authoritative requery without notify-only confirmation.
related: DW-256

### DW-201: Create-tenant mobile/unsafe-viewport fail-closed and open-existing recovery CTA affordances beyond localized Rejected copy
origin: migrated from legacy ledger ("Deferred from: bmad-build scope split of Story 3.1 (2026-08-08)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md`
reason: The legacy ledger defers this issue: Create-tenant mobile/unsafe-viewport fail-closed and open-existing recovery CTA affordances beyond localized Rejected copy. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md` summary: Create-tenant mobile/unsafe-viewport fail-closed and open-existing recovery CTA affordances beyond localized Rejected copy. evidence: Split from Story 3.1; confirmation/freshness/idempotency core does not require viewport gating or interactive open-existing navigation in the first slice.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor has no observed viewport safety gate or open-existing recovery CTA; rejected text remains the recovery affordance. These original split goals remain undelivered.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor
evidence-kind: recommendation
priority: P2
owner: unassigned
dependencies: create UX acceptance contract
next-action: Specify safe create viewport and open-existing navigation behavior, then cover the visible actions.
related: none

### DW-202: Create confirmation cannot correlate projection evidence to this attempt's messageId; concurrent same-id creates or unrelated list ProjectionVersion churn can still satisfy absence-then-presence + version rules

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-1-create-tenant-with-projection-confirmation.md (2026-08-08)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md`
archived: 2026-10-12

### DW-203: Workspace IsCommandSurfaceConnected is a render-time service lookup with no subscription, so BFF disconnect may not refresh create availability until an unrelated rerender

status: done 2026-08-28
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-1-create-tenant-with-projection-confirmation.md (2026-08-08)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md`
archived: 2026-10-12

### DW-204: SignalR-elevated ProjectionPending can still confirm after unrelated projection-version advancement on metadata (and sibling create/membership) flows

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: implementation of spec-3-2-edit-tenant-metadata-with-recorded-updates.md (2026-08-08)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md`
archived: 2026-10-12

### DW-205: Edit metadata confirmation does not pass live audit-row provenance into ConfirmProjection (version advancement only on the live path)

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: implementation of spec-3-2-edit-tenant-metadata-with-recorded-updates.md (2026-08-08)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md`
archived: 2026-10-12

### DW-206: Metadata IsAuthorized still defaults true with no contributor/global-admin BFF authorization reflection wired from TenantDetailPage
origin: migrated from legacy ledger ("Deferred from: implementation of spec-3-2-edit-tenant-metadata-with-recorded-updates.md (2026-08-08)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md`
reason: The legacy ledger defers this issue: Metadata IsAuthorized still defaults true with no contributor/global-admin BFF authorization reflection wired from TenantDetailPage. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md` summary: Metadata IsAuthorized still defaults true with no contributor/global-admin BFF authorization reflection wired from TenantDetailPage. evidence: Story 3.2 blind-hunter review and Ask First deferral; member flows share the same default-true pattern, so inventing metadata-only reflection was out of this slice.
pre-b7-verification-date: 2026-10-12
pre-b7-verification: src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor still defaults IsAuthorized=true; Components/Pages/TenantDetailPage.razor does not bind it. Recorded 2026-08-25/26 contributor/global-administrator reflection decisions are not implemented for metadata.
pre-b7-next-action: Bind tenant-scoped contributor/global-administrator evidence and fail closed while authority is indeterminate.
status: done 2026-10-14 (B7 closure)
decision: 2026-08-26 Role-aware BFF reflection — Add tenant-scoped contributor or global-administrator reflection and fail closed while authority is indeterminate.
decision: 2026-08-25 Role-aware BFF reflection — Extend BFF composition with tenant-scoped role-aware reflection and fail closed while contributor authority is indeterminate.
disposition: done
verification-date: 2026-10-14
verification: B7 binds metadata to the current literal-tenant Owner/Contributor or global-administrator BFF reflection; defaults and failed/pending authority are closed. Real parent/fresh membership, stale caller/tenant/response, preview revocation, post-lease and successful submission regressions pass. See b7-validation-evidence.md.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor; src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor
evidence-kind: verified-closure
priority: P2
owner: unassigned
dependencies: role-aware BFF seam
next-action: None for this finding; validated B7 closure does not imply release readiness.
closure-evidence: `_bmad-output/implementation-artifacts/b7-validation-evidence.md`; `_bmad-output/implementation-artifacts/spec-b7-live-admission.md`
related: DW-235, DW-264

### DW-207: After Confirmed/Rejected/Failed, retained MessageId can be reused on a deliberate new metadata edit instead of minting a new ULID

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: implementation of spec-3-2-edit-tenant-metadata-with-recorded-updates.md (2026-08-08)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md`
archived: 2026-10-12

### DW-208: Configuration filter comment claims Ordinal matching while FilteredRows uses OrdinalIgnoreCase

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-6-read-only-tenant-configuration.md (2026-08-09)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-1-6-read-only-tenant-configuration.md`
archived: 2026-10-12

### DW-209: Extend PageLayoutDeclarationTests runtime shell coverage beyond TenantsWorkspace/UserMembershipLookup to MyTenants, TenantAudit, and GlobalAdministrators full-width measure
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md`
reason: The legacy ledger defers this issue: Extend PageLayoutDeclarationTests runtime shell coverage beyond TenantsWorkspace/UserMembershipLookup to MyTenants, TenantAudit, and GlobalAdministrators full-width measure. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md` summary: Extend PageLayoutDeclarationTests runtime shell coverage beyond TenantsWorkspace/UserMembershipLookup to MyTenants, TenantAudit, and GlobalAdministrators full-width measure. evidence: Those three dense pages are source-scanned for FullWidth but have no executable data-fc-page-layout assertion; a Constrained regression would fail only governance text scan.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: tests/Hexalith.Tenants.UI.Tests/Components/PageLayoutDeclarationTests.cs still renders only workspace and user lookup in FrontComposerShell; MyTenants, TenantAudit and GlobalAdministrators lack runtime data-fc-page-layout coverage there.
verification-files: tests/Hexalith.Tenants.UI.Tests/Components/PageLayoutDeclarationTests.cs
evidence-kind: verified-gap
priority: P3
owner: unassigned
dependencies: none
next-action: Add runtime shell layout assertions for the three remaining dense pages.
related: none

### DW-210: Add runtime FcPageHeader assertions for MyTenants and UserMembershipLookup page chrome

status: done 2026-10-12
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md`
priority: P3
archived: 2026-10-12

### DW-211: Assert TenantsWorkspace post-detail FocusHeadingAsync actually moves focus to tenants-list-heading
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md`
reason: The legacy ledger defers this issue: Assert TenantsWorkspace post-detail FocusHeadingAsync actually moves focus to tenants-list-heading. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md` summary: Assert TenantsWorkspace post-detail FocusHeadingAsync actually moves focus to tenants-list-heading. evidence: Existing tests only assert tabindex=-1 after return query; removing FocusHeadingAsync would not fail the suite.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor has post-detail heading/focus handling, but a tabindex/source assertion does not establish document.activeElement at tenants-list-heading; no targeted authenticated post-detail heading trace was established.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor
evidence-kind: verified-gap
priority: P3
owner: unassigned
dependencies: authenticated browser navigation fixture
next-action: Capture browser activeElement after a missing-row detail return and assert the list heading is focused.
related: none

### DW-212: Harden FcPageHeader when PageTitle and Heading are both blank/whitespace so document title cannot resolve empty
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md`
reason: The legacy ledger defers this issue: Harden FcPageHeader when PageTitle and Heading are both blank/whitespace so document title cannot resolve empty. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md` summary: Harden FcPageHeader when PageTitle and Heading are both blank/whitespace so document title cannot resolve empty. evidence: FrontComposer-owned contract; Tenants callers currently supply localized titles, but the primitive still admits an empty DocumentTitle path.
status: open
decision: 2026-09-02 Reject blank titles — Throw a diagnostic parameter-contract exception when both title inputs are blank and add shared component tests.
decision: 2026-09-02 Reject blank titles — Throw a diagnostic parameter-contract exception when both title inputs are blank and add shared component tests.
disposition: actionable
verification-date: 2026-10-12
verification: references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageHeader.razor.cs ResolvedPageTitle still falls back to blank Heading when both inputs are blank. The recorded 2026-09-02 reject-blank-title decision remains unimplemented.
verification-files: references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageHeader.razor.cs
evidence-kind: verified-defect
priority: P3
owner: FrontComposer
dependencies: FrontComposer owning change
next-action: Implement the recorded shared parameter diagnostic and its component regression.
related: none

### DW-213: Make FocusHeadingAsync fail closed when the heading element is not yet rendered instead of focusing a default ElementReference
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md`
reason: The legacy ledger defers this issue: Make FocusHeadingAsync fail closed when the heading element is not yet rendered instead of focusing a default ElementReference. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md` summary: Make FocusHeadingAsync fail closed when the heading element is not yet rendered instead of focusing a default ElementReference. evidence: FrontComposer-owned timing contract; Tenants workspace path relies on OnAfterRenderAsync ordering.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageHeader.razor.cs FocusHeadingAsync checks heading text/tabindex but not whether _headingElement was rendered before calling FocusAsync.
verification-files: references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageHeader.razor.cs
evidence-kind: verified-defect
priority: P3
owner: FrontComposer
dependencies: FrontComposer owning change
next-action: Add a rendered-element readiness guard and an explicit pre-render focus test.
related: none

### DW-214: Align RemoveTenantMemberFlowTests StubTenantsLocalizer keys/values with shipped TenantsResources (EN/FR) so LocalizerDoubleParityTests passes

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md`
archived: 2026-10-12

### DW-215: Align release-tag validation with semantic-release SemVer parsing
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-release-version-floor-drift.md`
reason: The legacy ledger defers this issue: Align release-tag validation with semantic-release SemVer parsing. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-release-version-floor-drift.md` summary: Align release-tag validation with semantic-release SemVer parsing. evidence: The pre-existing tag filter accepts leading-zero or oversized numeric tags that semantic-release may ignore, allowing the guard and release engine to select different floors.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: .github/workflows/release.yml release_tags still accepts ^v[0-9]+\.[0-9]+\.[0-9]+$, including leading zeros and arbitrary numeric width, before selecting a floor; .releaserc.json uses semantic-release tag semantics.
verification-files: .github/workflows/release.yml
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: none
next-action: Validate candidate tags with the same SemVer parser/range as semantic-release and test rejected numeric forms.
related: none

### DW-216: Validate normalized NuGet version grammar before excluding prereleases from the registry floor
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-release-version-floor-drift.md`
reason: The legacy ledger defers this issue: Validate normalized NuGet version grammar before excluding prereleases from the registry floor. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-release-version-floor-drift.md` summary: Validate normalized NuGet version grammar before excluding prereleases from the registry floor. evidence: The pre-existing registry parser accepts malformed prerelease and non-normalized stable strings, which can turn unusable evidence into a passing drift check.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: .github/workflows/release.yml registry grammar permits leading-zero stable forms and malformed prerelease identifiers under [0-9A-Za-z.-]+, then excludes any hyphenated value. Invalid evidence can still pass the floor guard.
verification-files: .github/workflows/release.yml
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: none
next-action: Validate normalized NuGet grammar before stable/prerelease classification and cover malformed suffixes.
related: none

### DW-217: Reconcile publication-preflight and contributor recovery guidance with authentic-tag-only provenance policy
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-release-version-floor-drift.md`
reason: The legacy ledger defers this issue: Reconcile publication-preflight and contributor recovery guidance with authentic-tag-only provenance policy. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-release-version-floor-drift.md` summary: Reconcile publication-preflight and contributor recovery guidance with authentic-tag-only provenance policy. evidence: Existing script and CONTRIBUTING guidance still recommends restoring deleted tags or advancing via a breaking footer without the producing-commit and reachability safeguards adopted by this spec.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: scripts/validate-publication-preflight.sh floor failure text and CONTRIBUTING.md still suggest restoring deleted tags or a breaking footer without the authentic producing-commit/reachability safeguards in .github/workflows/release.yml.
verification-files: scripts/validate-publication-preflight.sh; .github/workflows/release.yml
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: release-lineage policy
next-action: Align recovery guidance with authentic-tag-only lineage and reviewed recovery.
related: DW-218, DW-219

### DW-218: Correct stale release trigger and current release-line documentation
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-release-version-floor-drift.md`
reason: The legacy ledger defers this issue: Correct stale release trigger and current release-line documentation. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-release-version-floor-drift.md` summary: Correct stale release trigger and current release-line documentation. evidence: Existing project context and contributor docs describe automatic workflow-run publication and a 4.x current line, while release is manually dispatched and reachable history extends through 5.x.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: _bmad-output/project-context.md still says workflow_run publication; .github/workflows/release.yml actually uses workflow_dispatch. CONTRIBUTING.md still calls 4.x current, while later release artifacts describe newer lines.
verification-files: .github/workflows/release.yml
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: verified current release-line evidence
next-action: Correct current trigger/line guidance using committed release evidence; preserve historical observations.
related: DW-217

### DW-219: Replace absolute collision wording with an accurate unproven-lineage risk statement
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-release-version-floor-drift.md`
reason: The legacy ledger defers this issue: Replace absolute collision wording with an accurate unproven-lineage risk statement. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-release-version-floor-drift.md` summary: Replace absolute collision wording with an accurate unproven-lineage risk statement. evidence: The existing guard message says semantic-release would certainly propose an occupied version even though it does not compute the proposal and the registry range may contain gaps.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: .github/workflows/release.yml still says semantic-release would certainly propose an occupied version, although the guard compares registry/tag floors without computing the proposed version.
verification-files: .github/workflows/release.yml
evidence-kind: verified-defect
priority: P3
owner: unassigned
dependencies: none
next-action: Describe the proven lineage/floor gap and collision risk without claiming an uncomputed collision.
related: DW-217

### DW-220: Remove default set/remove projection-proof implementations from ITenantQueryGateway so every gateway and decorator must implement the security-sensitive proof contract explicitly
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-1-6-read-only-tenant-configuration-2.md`
reason: The legacy ledger defers this issue: Remove default set/remove projection-proof implementations from ITenantQueryGateway so every gateway and decorator must implement the security-sensitive proof contract explicitly. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-6-read-only-tenant-configuration-2.md` summary: Remove default set/remove projection-proof implementations from ITenantQueryGateway so every gateway and decorator must implement the security-sensitive proof contract explicitly. evidence: The pre-existing interface defaults allow a future implementation to omit both methods, compile successfully, and silently return unavailable proof; the concrete unavailable gateway is now covered directly, but the interface design debt remains outside this review diff.
status: open
decision: 2026-08-28 Require explicit methods — Remove the default implementations and update every gateway and stub to implement the proof contracts explicitly.
decision: 2026-08-28 Require explicit methods — Remove the default implementations and update every gateway and stub to implement the proof contracts explicitly.
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Services/Gateways/ITenantQueryGateway.cs still supplies unavailable default bodies for set/remove projection proof, including tracked request overloads. Existing 2026-08-28 decision requires explicit methods.
verification-files: src/Hexalith.Tenants.UI/Services/Gateways/ITenantQueryGateway.cs
evidence-kind: recommendation
priority: P2
owner: unassigned
dependencies: none
next-action: Remove the recorded default proof implementations and make every adapter/stub implement them explicitly.
related: none

### DW-221: Introduce a shape-preserving configuration schema or discriminator that distinguishes valid empty policy arrays from empty scalar declarations
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-1-6-read-only-tenant-configuration-2.md`
reason: The legacy ledger defers this issue: Introduce a shape-preserving configuration schema or discriminator that distinguishes valid empty policy arrays from empty scalar declarations. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-6-read-only-tenant-configuration-2.md` summary: Introduce a shape-preserving configuration schema or discriminator that distinguishes valid empty policy arrays from empty scalar declarations. evidence: Standard IConfiguration flattening makes JSON [] and scalar "" observationally identical, so the current provider safely withholds all approval but cannot render the scalar form as policy-unavailable without also rejecting the repository's valid-empty default.
status: open
decision: 2026-08-26 Add shape metadata — Add backward-compatible raw-shape or discriminator metadata alongside existing configuration keys.
decision: 2026-08-25 Shape metadata — Add backward-compatible raw-shape or discriminator metadata alongside existing IConfiguration keys.
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationReadPolicyProvider.cs IsCollectionScalar still documents IConfiguration ambiguity between [] and empty scalar; no raw-shape discriminator is present. Existing 2026-08-25/26 shape-metadata decision remains pending.
verification-files: src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationReadPolicyProvider.cs
evidence-kind: recommendation
priority: P2
owner: unassigned
dependencies: configuration schema compatibility
next-action: Define backward-compatible shape metadata and provider tests distinguishing empty arrays from scalar declarations.
related: none

### DW-222: Bind remove-member audit receipts to the exact submitted command before presenting them as attempt-specific proof

status: done 2026-10-13
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
priority: P1
archived: 2026-10-13

### DW-223: Page through bounded tenant-audit results while assembling remove-member proof

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
archived: 2026-10-12

### DW-224: Require current projection lifecycle, freshness, and projection-backed provenance before promoting removal audit evidence to available proof

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
archived: 2026-10-12

### DW-225: Replace client/server wall-clock matching for removal proof with a causally stable boundary
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
reason: The legacy ledger defers this issue: Replace client/server wall-clock matching for removal proof with a causally stable boundary. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md` summary: Replace client/server wall-clock matching for removal proof with a causally stable boundary. evidence: AttemptStartedAtUtc is captured on the UI clock and compared directly with server event timestamps, so ordinary clock skew can hide a legitimate event or admit an equal-time event despite strict-advancement wording.
status: open
decision: 2026-08-26 Use causation identity — Bind proof to command and event causation identity in coordination with DW-222.
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/State/TenantCommands/TenantMembershipCommandProvenance.cs HasQualifyingAuditProvenance still compares audit timestamps >= AttemptStartedAtUtc from the UI clock; RemoveTenantMemberFlow.razor uses that window. Recorded causation-identity decision is pending.
verification-files: src/Hexalith.Tenants.UI/State/TenantCommands/TenantMembershipCommandProvenance.cs; src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: DW-222
next-action: Replace wall-clock qualification with exact command causation/sequence in coordination with DW-222.
related: DW-222

### DW-226: Downgrade retained global-administrator evidence when a tenant-detail supplementary refresh fails

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
archived: 2026-10-12

### DW-227: Focus the actual remove-member controls rather than tabindex wrappers during dialog trapping and restoration
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
reason: The legacy ledger defers this issue: Focus the actual remove-member controls rather than tabindex wrappers during dialog trapping and restoration. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md` summary: Focus the actual remove-member controls rather than tabindex wrappers during dialog trapping and restoration. evidence: The existing focus sentinels and close restoration target noninteractive span wrappers, which can move keyboard focus away from the intended confirm, cancel, or launch button.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor now focuses FluentTextInput.Element for initial confirmation, but Cancel still uses a tabindex=-1 span; Members/MemberAccessReview.razor still restores focus to span wrappers.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor; src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: shared modal/focus contract
next-action: Focus the native/Fluent interactive Cancel and launcher controls and assert browser keyboard behavior.
related: DW-194, DW-241

### DW-228: Refresh remove-member audit guidance now that the flow queries and renders audit receipts

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
archived: 2026-10-12

### DW-229: Verify the tenant-detail global-administrator evidence bridge at the page boundary

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
archived: 2026-10-12

### DW-230: Pre-existing flaky false-success in the global-administrator grant re-query

status: done 2026-09-02
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-1-reverify-projection-confirmed-membership-command-foundation (2026-08-20)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
archived: 2026-10-12

### DW-231: Global-administrator command surface is not covered by the new AggregateIdentity admission gate

status: done 2026-08-28
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-1-reverify-projection-confirmed-membership-command-foundation (2026-08-20)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
archived: 2026-10-12

### DW-232: Optional `messageId` reached create-tenant and update-tenant beyond the declared membership scope

status: done 2026-10-12
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-1-reverify-projection-confirmed-membership-command-foundation (2026-08-20)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
priority: P3
archived: 2026-10-12

### DW-233: A missing admission-gate registration disables Epic 3 command surfaces as well as membership

status: done 2026-08-28
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-1-reverify-projection-confirmed-membership-command-foundation (2026-08-20)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
archived: 2026-10-12

### DW-234: Gateway and snapshot safe-message strings are still hard-coded English

status: done 2026-10-16
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-1-reverify-projection-confirmed-membership-command-foundation (2026-08-20)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
priority: P2
archived: 2026-10-16

### DW-235: Fail membership action availability closed against live authorization reflection
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof (2026-08-20)"), 2026-08-25
location: MemberAccessReview
source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md`
reason: The legacy ledger defers this issue: Fail membership action availability closed against live authorization reflection. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md` summary: Fail membership action availability closed against live authorization reflection. evidence: `MemberAccessReview` defaults add/change/remove authorization parameters to true, `TenantDetailPage` does not bind membership authorization evidence, and `BuildActionSlots` ignores the change-role and remove-member authorization values when deciding whether to render launch buttons; denial is enforced only after a flow is opened.
pre-b7-verification-date: 2026-10-12
pre-b7-verification: src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor still defaults authorization flags true and BuildActionSlots ignores change/remove authority; Components/Pages/TenantDetailPage.razor still omits bindings. Recorded owner/global-administrator reflection decisions remain pending; backend enforcement is separate.
pre-b7-next-action: Bind live tenant-scoped authority to every membership launcher and fail closed with specific reasons.
status: done 2026-10-14 (B7 closure)
decision: 2026-08-26 Bind role reflection — Add tenant-scoped owner or global-administrator reflection, bind all flags, and show denial or indeterminate reasons.
decision: 2026-08-25 Role-aware BFF reflection — Add tenant-scoped owner or global-administrator reflection, bind all flags, and include denial or indeterminate reasons.
disposition: done
verification-date: 2026-10-14
verification: B7 binds add/change/remove availability and direct launchers to current tenant Owner or global-administrator reflection with distinct denied/indeterminate reasons. Fresh role changes and target/caller/tenant/preview/retry races prove zero unauthorized dispatch, preserving bootstrap, exact attempts, proof and leases. See b7-validation-evidence.md.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor; src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor
evidence-kind: verified-closure
priority: P2
owner: unassigned
dependencies: role-aware BFF reflection
next-action: None for this finding; validated B7 closure does not imply release readiness.
closure-evidence: `_bmad-output/implementation-artifacts/b7-validation-evidence.md`; `_bmad-output/implementation-artifacts/spec-b7-live-admission.md`
related: DW-206, DW-264

### DW-236: Serialize child membership-command lease acquisition

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof (2026-08-20)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md`
archived: 2026-10-12

### DW-237: Keep an open membership command flow keyed to its captured target

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof (2026-08-20)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md`
archived: 2026-10-12

### DW-238: Give Continue read-only a stable dialog lifecycle

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof (2026-08-20)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md`
archived: 2026-10-12

### DW-239: Do not initialize a pre-command removal preview as missing audit support

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof (2026-08-20)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md`
archived: 2026-10-12

### DW-240: Map command-status HTTP timeouts to a support-safe unknown result

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof (2026-08-20)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md`
archived: 2026-10-12

### DW-241: Restore focus safely when a successful removal deletes the launcher row
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof (2026-08-20)"), 2026-08-25
location: MemberAccessReview.OnAfterRenderAsync
source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md`
reason: The legacy ledger defers this issue: Restore focus safely when a successful removal deletes the launcher row. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md` summary: Restore focus safely when a successful removal deletes the launcher row. evidence: `MemberAccessReview.OnAfterRenderAsync` focuses a retained row wrapper without verifying that the target still exists or providing a fallback, so a stale `ElementReference` can fault instead of returning focus after the row disappears.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor OnAfterRenderAsync still directly focuses retained row ElementReferences without checking existence or providing a fallback when removal deletes the row.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: DW-194 focus contract
next-action: Restore to an existing launcher or stable member heading, containing stale-reference failures.
related: DW-227

### DW-242: Make remove-member focus trapping visibility-aware and verify it at the responsive breakpoint
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof (2026-08-20)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md`
reason: The legacy ledger defers this issue: Make remove-member focus trapping visibility-aware and verify it at the responsive breakpoint. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md` summary: Make remove-member focus trapping visibility-aware and verify it at the responsive breakpoint. evidence: The narrow-layout CSS hides the confirmation form, but initial focus and the end sentinel can still target controls inside that hidden form; current tests only inspect CSS/source structure and do not exercise computed visibility or an actual keyboard focus cycle.
status: open
disposition: uncertain
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor now wraps the end sentinel to always-visible Cancel, partially addressing hidden-confirm focus. Initial confirmation still uses the form control; no computed-visibility Tab-cycle proof exists at the narrow breakpoint.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor
evidence-kind: unverified-risk
priority: P2
owner: unassigned
dependencies: authenticated browser fixture
next-action: Measure form visibility and initial/Tab focus at 767px and 768px before closing the remaining risk.
related: DW-195, DW-227, DW-317

### DW-243: Replace legacy Fluent/FAST CSS custom properties in the remove-member dialog
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof (2026-08-20)"), 2026-08-25
location: RemoveTenantMemberFlow.razor.css
source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md`
reason: The legacy ledger defers this issue: Replace legacy Fluent/FAST CSS custom properties in the remove-member dialog. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md` summary: Replace legacy Fluent/FAST CSS custom properties in the remove-member dialog. evidence: `RemoveTenantMemberFlow.razor.css` still uses `--neutral-stroke-rest`, `--error-fill-rest`, and `--focus-stroke-outer`, contrary to the repository's Fluent UI v5 token guidance.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor.css still uses --neutral-stroke-rest, --error-fill-rest and --focus-stroke-outer. tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs does not prove removal of these legacy variables.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor.css; tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: none
next-action: Migrate the residual CSS tokens to Fluent 2 roles and pin the legacy-token guard.
related: DW-243, DW-262, DW-612

### DW-244: Align rejected remove-member recovery copy with actions the surface actually provides
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof (2026-08-20)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md`
reason: The legacy ledger defers this issue: Align rejected remove-member recovery copy with actions the surface actually provides. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md` summary: Align rejected remove-member recovery copy with actions the surface actually provides. evidence: The EN/FR rejected-state recovery text tells the operator to request permission, but the rejected flow renders no permission-request action or delegate.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Resources/TenantsResources.resx Tenants.RemoveMember.Recovery.Rejected still says request permission; Components/Tenants/Members/RemoveTenantMemberFlow.razor supplies no permission-request destination/action.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor
evidence-kind: verified-defect
priority: P3
owner: unassigned
dependencies: none
next-action: Align rejected recovery copy with rendered actions or provide the explicitly governed destination.
related: none

### DW-245: Preserve queued projection-refresh intent in add-member and change-role flows

status: done 2026-10-16
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof (2026-08-20)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md`
priority: P2
archived: 2026-10-16

### DW-246: Reconcile story gitlink validation with the seven post-baseline dependency pointer changes
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof (2026-08-20)"), 2026-08-25
location: Hexalith.Builds
source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md`
reason: The legacy ledger defers this issue: Reconcile story gitlink validation with the seven post-baseline dependency pointer changes. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md` summary: Reconcile story gitlink validation with the seven post-baseline dependency pointer changes. evidence: The story validator reports only `Hexalith.AI.Tools`, `Hexalith.Builds`, `Hexalith.Commons`, `Hexalith.EventStore`, `Hexalith.FrontComposer`, `Hexalith.Memories`, and `Hexalith.PolymorphicSerializations`, all introduced after the preserved story baseline and outside this patch.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: scripts/validate-story-gitlinks.py legacy-mode default still compares the old 2.4 baseline with current HEAD; the exact original --ref range remains the supported way to exclude later dependency commits. No durable story-end attribution has been added to this historical artifact.
verification-files: scripts/validate-story-gitlinks.py
evidence-kind: recommendation
priority: P3
owner: unassigned
dependencies: DW-198
next-action: Record exact story end/provenance, retaining later dependency changes outside the old story.
related: DW-198

### DW-247: No-advancement ProjectionPending has no in-place terminal escape

status: done 2026-10-16
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof (2026-08-20)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
priority: P2
archived: 2026-10-16

### DW-248: Refresh-coalescing re-enters recursively instead of iterating
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof (2026-08-20)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
reason: The legacy ledger defers this issue: Refresh-coalescing re-enters recursively instead of iterating. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md` summary: Refresh-coalescing re-enters recursively instead of iterating. evidence: RefreshCommandStatusAsync calls itself after the finally block when a request arrived during the in-flight release window. Under a sustained nudge stream this grows the async frame chain without bound. The dropped-request windows reported by review did not reproduce on inspection. Duplicated verbatim across AddTenantMemberFlow, ChangeTenantMemberRoleFlow and RemoveTenantMemberFlow, so any rewrite should extract a shared helper.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Tenants/Members/AddTenantMemberFlow.razor and ChangeTenantMemberRoleFlow.razor still recursively re-enter RefreshCommandStatusAsync after the finally release window; continuous nudges can grow the async chain.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Members/AddTenantMemberFlow.razor; src/Hexalith.Tenants.UI/Components/Tenants/Members/ChangeTenantMemberRoleFlow.razor
evidence-kind: recommendation
priority: P3
owner: unassigned
dependencies: membership coalescer design
next-action: Replace recursive tail replay with an iterative shared drain without dropping refresh intent.
related: DW-245, DW-265, DW-290

### DW-249: AttemptStartedAtUtc and hasQualifyingAuditProvenance are dead in production for metadata; the snapshot test pins a branch no production call site can reach

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-2-edit-tenant-metadata-with-recorded-updates.md (2026-08-21)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md`
archived: 2026-10-12

### DW-250: source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md` summary: Nothing proves the tenant-detail read model's ProjectionVersion actually advances for a same-value update, which is the premise the whole confirmation path now rests on

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-2-edit-tenant-metadata-with-recorded-updates.md (2026-08-21)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md`
archived: 2026-10-12

### DW-251: Hard-coded English strings remain on paths this story made localizable

status: done 2026-10-16
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-2-edit-tenant-metadata-with-recorded-updates.md (2026-08-21)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md`
priority: P2
archived: 2026-10-16

### DW-252: EditTenantMetadataFlow.ApplyProjectionEvidence and ApplySignalRNudge have no callers; the story threaded a projection version into a dead entry point
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-2-edit-tenant-metadata-with-recorded-updates.md (2026-08-21)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md`
reason: The legacy ledger defers this issue: EditTenantMetadataFlow.ApplyProjectionEvidence and ApplySignalRNudge have no callers; the story threaded a projection version into a dead entry point. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md` summary: EditTenantMetadataFlow.ApplyProjectionEvidence and ApplySignalRNudge have no callers; the story threaded a projection version into a dead entry point. evidence: grep over src/ finds only the declarations; TenantDetailPage holds an @ref to _memberAccessReview only and nudges only that component. Consequence: SignalR nudges never reach the metadata flow today.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor still declares ApplySignalRNudge and ApplyProjectionEvidence without production invocations; TenantDetailPage.razor only nudges member and lifecycle children.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor; src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor
evidence-kind: recommendation
priority: P3
owner: unassigned
dependencies: none
next-action: Wire metadata nudges to authoritative reconciliation or retire the dead evidence seam.
related: DW-259

### DW-253: New defensive branches are covered only by reflection-poking private fields, so they will break silently on rename and do not exercise the real gateway path
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-2-edit-tenant-metadata-with-recorded-updates.md (2026-08-21)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md`
reason: The legacy ledger defers this issue: New defensive branches are covered only by reflection-poking private fields, so they will break silently on rename and do not exercise the real gateway path. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md` summary: New defensive branches are covered only by reflection-poking private fields, so they will break silently on rename and do not exercise the real gateway path. evidence: EditTenantMetadataFlowTests.cs sets the private _snapshot field and invokes private RefreshStatusAsync to build an Accepted snapshot with null tracking ids. The only realistic production route to that state is a gateway returning Accepted with a blank CorrelationId, which no test drives.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: tests/Hexalith.Tenants.UI.Tests/Components/EditTenantMetadataFlowTests.cs still sets _snapshot via GetField for tracking-loss branches. A source rename can invalidate the fixture and it does not establish the real Accepted/blank-correlation gateway route.
verification-files: tests/Hexalith.Tenants.UI.Tests/Components/EditTenantMetadataFlowTests.cs
evidence-kind: verified-gap
priority: P3
owner: unassigned
dependencies: none
next-action: Drive the defensive branches through a gateway substitute returning the malformed Accepted response.
related: none

### DW-254: Optional messageId is now inconsistent across ITenantCommandGateway
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-2-edit-tenant-metadata-with-recorded-updates.md (2026-08-21)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md`
reason: The legacy ledger defers this issue: Optional messageId is now inconsistent across ITenantCommandGateway. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md` summary: Optional messageId is now inconsistent across ITenantCommandGateway. evidence: Create, Add, Change, Remove and Update carry it; SetTenantConfigurationAsync, RemoveTenantConfigurationAsync, SetGlobalAdministratorAsync, RemoveGlobalAdministratorAsync, EnableTenantAsync and DisableTenantAsync do not. The reconnect/idempotency contract is therefore partial. Hexalith.Tenants.UI is not a published package, so there is no external consumer break.
status: open
decision: 2026-08-26 Unify optional identity — Add optional messageId to every command method and update implementations, flows, and tests consistently.
decision: 2026-08-25 Unify optional identity — Add optional messageId to every command method and update implementations, flows, and tests with consistent retry semantics.
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs now has explicit tracked variants for configuration/lifecycle/global commands, but old untracked members still lack optional messageId. The recorded uniform optional-identity decision is only partially delivered by the new split API.
verification-files: src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs
evidence-kind: recommendation
priority: P2
owner: unassigned
dependencies: compatibility of tracked APIs
next-action: Reconcile the settled identity contract with tracked/untracked APIs and migrate or deprecate legacy methods consistently.
related: DW-319, DW-327

### DW-255: `AttemptStartedAtUtc` ships on the public `TenantCreateCommandSnapshot` record but is never read, and defaults via `DateTimeOffset.UtcNow` instead of an injected clock
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-1-create-tenant-with-projection-confirmation.md (2026-08-21)"), 2026-08-25
location: AttemptStartedAtUtc
source_spec: `_bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md`
reason: The legacy ledger defers this issue: `AttemptStartedAtUtc` ships on the public `TenantCreateCommandSnapshot` record but is never read, and defaults via `DateTimeOffset.UtcNow` instead of an injected clock. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md` summary: `AttemptStartedAtUtc` ships on the public `TenantCreateCommandSnapshot` record but is never read, and defaults via `DateTimeOffset.UtcNow` instead of an injected clock. evidence: The audit-provenance branch it feeds (`HasQualifyingAuditProvenance`) is never called for create and is already recorded as deferred work from the 2026-08-08 review; removing or wiring the field belongs with that slice.
status: open
decision: 2026-08-27 Wire injected timing — Inject a clock, stamp create attempts deterministically, and consume AttemptStartedAtUtc in bounded retained-attempt or provenance behavior while preserving the public member.
decision: 2026-08-27 Wire injected timing — Inject a clock, stamp create attempts deterministically, and consume AttemptStartedAtUtc in bounded retained-attempt or provenance behavior while preserving the public member.
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs TenantCreateCommandSnapshot still stamps AttemptStartedAtUtc with DateTimeOffset.UtcNow; CreateTenantFlow.razor has no injected clock or consumption of that field. Existing 2026-08-27 injected-timing decision remains pending.
verification-files: src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs; src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor
evidence-kind: recommendation
priority: P3
owner: unassigned
dependencies: none
next-action: Inject the chosen clock and consume timing in bounded retained create behavior while preserving the public member.
related: DW-313

### DW-256: `CreateTenantFlow.ApplyProjectionEvidence` has no callers in `src/` or `tests/` and bypasses `SetSnapshot`, so it would not honour the assertive-focus rule if wired
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-1-create-tenant-with-projection-confirmation.md (2026-08-21)"), 2026-08-25
location: src/; tests/
source_spec: `_bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md`
reason: The legacy ledger defers this issue: `CreateTenantFlow.ApplyProjectionEvidence` has no callers in `src/` or `tests/` and bypasses `SetSnapshot`, so it would not honour the assertive-focus rule if wired. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md` summary: `CreateTenantFlow.ApplyProjectionEvidence` has no callers in `src/` or `tests/` and bypasses `SetSnapshot`, so it would not honour the assertive-focus rule if wired. evidence: Its signature was updated for the tuple change, but the SignalR nudge wiring that would call it is itself deferred; fixing the seam in isolation has no observable effect.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor ApplyProjectionEvidence now calls SetSnapshot, fixing the focus bypass, but repository search still finds no invocations; the nudge integration is still missing.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor
evidence-kind: recommendation
priority: P3
owner: unassigned
dependencies: DW-200
next-action: Complete the create nudge/evidence wiring or remove the dead seam.
related: DW-200, DW-259

### DW-257: Baseline and evidence projection versions are read from different snapshot lineages -- baseline from `_snapshot.ProjectionVersion`, evidence from `_lastConfirmedSnapshot ?? _snapshot` -- so a failed post-create reload makes a genuinely successful create report `UnableToVerify`

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-1-create-tenant-with-projection-confirmation.md (2026-08-21)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md`
archived: 2026-10-12

### DW-258: `TenantsWorkspace.IsCommandSurfaceConnected` is a render-time `Services.GetService` lookup with no subscription, duplicating an existing resolution in the same component and using the non-generic overload
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-1-create-tenant-with-projection-confirmation.md (2026-08-21)"), 2026-08-25
location: TenantsWorkspace.IsCommandSurfaceConnected
source_spec: `_bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md`
reason: The legacy ledger defers this issue: `TenantsWorkspace.IsCommandSurfaceConnected` is a render-time `Services.GetService` lookup with no subscription, duplicating an existing resolution in the same component and using the non-generic overload. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md` summary: `TenantsWorkspace.IsCommandSurfaceConnected` is a render-time `Services.GetService` lookup with no subscription, duplicating an existing resolution in the same component and using the non-generic overload. evidence: Pre-existing composition pattern; the no-subscription half is already recorded in this ledger from the 2026-08-08 review. Story 3.1 added the workspace-side call site but not the pattern.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor IsCommandSurfaceConnected still resolves ITenantsBffComposition via Services.GetService at render time, with duplicated disconnected/authorization-empty predicates and no reconnect subscription.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor
evidence-kind: recommendation
priority: P3
owner: unassigned
dependencies: none
next-action: Resolve composition consistently and centralize the absence predicate; specify whether reconnect needs notification.
related: DW-258, DW-282

### DW-259: `ApplyProjectionEvidence` is dead code across all eight command flows
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md (2026-08-21)"), 2026-08-25
location: src/; tests/
reason: The legacy ledger defers this issue: `ApplyProjectionEvidence` is dead code across all eight command flows. Original context is preserved in legacy-detail.
legacy-detail: - **`ApplyProjectionEvidence` is dead code across all eight command flows.** A repo-wide search finds eight `internal void ApplyProjectionEvidence` declarations in `src/` and zero invocations in `src/` or `tests/`. Loop 2 rewrote the remove-member copy (`RemoveTenantMemberFlow.razor:628`) to fire a discarded `InvokeAsync` that calls `TryAssembleRemovalProofAsync` and `UpdateCommandActivityForSnapshotAsync` without `StateHasChanged` and outside any try/catch. The live proof path is `HandleAuthoritativeRefreshNudgeAsync` → `TryAssembleRemovalProofAsync` (`:902`), reached from `MemberAccessReview.razor:804`, so WP-2A still works — the rewritten method is simply unreachable. Pre-existing pattern, spans seven files outside story 2.4.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor, Members/AddTenantMemberFlow.razor, ChangeTenantMemberRoleFlow.razor, RemoveTenantMemberFlow.razor, Metadata/EditTenantMetadataFlow.razor and Configuration/SetTenantConfigurationFlow.razor still declare ApplyProjectionEvidence with no call sites in src/tests. The historical count eight changed, but the dead-seam issue persists.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor; src/Hexalith.Tenants.UI/Components/Tenants/Members/AddTenantMemberFlow.razor; src/Hexalith.Tenants.UI/Components/Tenants/Members/ChangeTenantMemberRoleFlow.razor; src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor; src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor; src/Hexalith.Tenants.UI/Components/Tenants/Configuration/SetTenantConfigurationFlow.razor
evidence-kind: recommendation
priority: P3
owner: unassigned
dependencies: none
next-action: Remove unused evidence entry points or wire one authoritative live seam with focus/fault containment.
related: DW-252, DW-256

### DW-260: `CreateTenantFlow` never adopts the reusable `messageId` affordance this story added

status: done 2026-08-27
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md (2026-08-21)"), 2026-08-25
archived: 2026-10-12

### DW-261: Missing test coverage for three branches this story introduced
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md (2026-08-21)"), 2026-08-25
location: TenantCommandGatewayTests.cs:1137-1184; tests/
reason: The legacy ledger defers this issue: Missing test coverage for three branches this story introduced. Original context is preserved in legacy-detail.
legacy-detail: - **Missing test coverage for three branches this story introduced.** No test asserts `CreateTenantAsync`/`UpdateTenantAsync` retain the minted ULID on indeterminate failure (the three membership equivalents exist at `TenantCommandGatewayTests.cs:1137-1184`); no test hands any flow a denying `CommandActivityLease` (every stub returns `Task.FromResult(true)`, and `FromResult(false)` appears nowhere in `tests/`), so the pre-dispatch lease guard at `RemoveTenantMemberFlow.razor:742-750` can be deleted with the suite still green; and no test observes the tracking-lost submit branch at `:669-690`. Test files are chunk C of this review.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: tests/Hexalith.Tenants.UI.Tests/Components/RemoveTenantMemberFlowTests.cs ActivityLeaseRefusalReportsARetryAsUnverifiableAndAFirstAttemptAsNotStarted now covers denial, and Services/Gateways/TenantCommandGatewayTests.cs Update_tenant_gateway_exception_retains_resolved_message_id_for_exact_retry covers update identity retention. A create indeterminate minted-ID case and the original no-tracking submission branch still lack demonstrated behavior coverage; the original three-part gap is only partially resolved.
verification-files: tests/Hexalith.Tenants.UI.Tests/Components/RemoveTenantMemberFlowTests.cs; tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs
evidence-kind: verified-gap
priority: P3
owner: unassigned
dependencies: shared flow test seam
next-action: Add create minted-ID retention on an indeterminate result and a gateway-driven accepted-with-lost-tracking submission regression.
related: none

### DW-262: Legacy FAST token `--neutral-stroke-rest` survives in `RemoveTenantMemberFlow.razor.css:4`
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md (2026-08-21)"), 2026-08-25
location: RemoveTenantMemberFlow.razor.css:4; project-context.md
reason: The legacy ledger defers this issue: Legacy FAST token `--neutral-stroke-rest` survives in `RemoveTenantMemberFlow.razor.css:4`. Original context is preserved in legacy-detail.
legacy-detail: - **Legacy FAST token `--neutral-stroke-rest` survives in `RemoveTenantMemberFlow.razor.css:4`.** `project-context.md` bans `--neutral-*` outright, yet `DomainUiFluentConformanceTests` passes — the guard does not cover custom-property names. Noted rather than patched because this story's diff moved *off* a banned token (`--accent-fill-rest` → `--error-fill-rest` at `.css:36`), i.e. it improved the file; the residual token and the guard gap are pre-existing.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor.css still uses --neutral-stroke-rest, --error-fill-rest and --focus-stroke-outer. tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs does not prove removal of these legacy variables.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor.css; tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: none
next-action: Migrate the residual CSS tokens to Fluent 2 roles and pin the legacy-token guard.
related: DW-243, DW-262, DW-612
duplicate-of: DW-243

### DW-263: `RemoveTenantMemberFlow.Dispose` does not release the command-activity lease — attempted and reverted

status: done 2026-08-28
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md (2026-08-21)"), 2026-08-25
archived: 2026-10-12

### DW-264: Create availability derives `IsAuthorized` from the tenant-list surface kind rather than `ITenantsBffComposition.GlobalAdministratorsAuthorizationReflection`, so an `Indeterminate` authorization reflection still leaves create enabled -- against the Always fail-closed clause
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md (2026-08-21)"), 2026-08-25
location: IsAuthorized
source_spec: `_bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md`
reason: The legacy ledger defers this issue: Create availability derives `IsAuthorized` from the tenant-list surface kind rather than `ITenantsBffComposition.GlobalAdministratorsAuthorizationReflection`, so an `Indeterminate` authorization reflection still leaves create enabled -- against the Always fail-closed clause. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md` summary: Create availability derives `IsAuthorized` from the tenant-list surface kind rather than `ITenantsBffComposition.GlobalAdministratorsAuthorizationReflection`, so an `Indeterminate` authorization reflection still leaves create enabled -- against the Always fail-closed clause. evidence: Deferred to Story 3.3 by code-review decision D5 (2026-08-21). Story 3.3 is scoped exactly as the fail-closed availability guardrail for lifecycle and configuration; server/API/domain authorization remains the enforcement boundary, so this is UI honesty rather than a security hole. Story 3.3 must cover create availability, not only lifecycle and configuration.
pre-b7-verification-date: 2026-10-12
pre-b7-verification: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor still binds CreateTenantFlow.IsAuthorized from _snapshot.Kind excluding Error/Unauthorized, rather than the async global authority reflection. Indeterminate authority can still leave create enabled.
pre-b7-next-action: Bind create availability to resolved authority while preserving the documented first-tenant freshness exception.
status: done 2026-10-14 (B7 closure)
disposition: done
verification-date: 2026-10-14
verification: B7 derives create availability from resolved current-caller global-administrator reflection, reauthorizes before dispatch and refuses stale/pending/denied results. Authorized first-tenant Unknown freshness and delayed audit return remain supported; denied/indeterminate cases prove zero dispatch. See b7-validation-evidence.md.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor
evidence-kind: verified-closure
priority: P2
owner: unassigned
dependencies: create authority reflection
next-action: None for this finding; validated B7 closure does not imply release readiness.
closure-evidence: `_bmad-output/implementation-artifacts/b7-validation-evidence.md`; `_bmad-output/implementation-artifacts/spec-b7-live-admission.md`
related: DW-206, DW-235

### DW-265: Refresh coalescing downgrades a user-initiated projection refresh to a status-only refresh and re-enters recursively

status: done 2026-10-16
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
priority: P2
archived: 2026-10-16

### DW-266: `AsyncLocal<bool>` is the wrong primitive for dispatcher-bound re-entrancy state
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: AddTenantMemberFlow.razor:160; ChangeTenantMemberRoleFlow.razor:178
reason: The legacy ledger defers this issue: `AsyncLocal<bool>` is the wrong primitive for dispatcher-bound re-entrancy state. Original context is preserved in legacy-detail.
legacy-detail: - summary: `AsyncLocal<bool>` is the wrong primitive for dispatcher-bound re-entrancy state. evidence: `AddTenantMemberFlow.razor:160`, `ChangeTenantMemberRoleFlow.razor:178`. Blazor components already run serialized on the renderer dispatcher, so a plain field is correct and avoids an ExecutionContext copy-on-write per set; the AsyncLocal also fails to flow into callbacks invoked from a context it was not captured on.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Tenants/Members/AddTenantMemberFlow.razor and ChangeTenantMemberRoleFlow.razor still use AsyncLocal<bool> _isProjectionRefreshCallback. A simpler dispatcher-state primitive is recommended, but no distinct production failure is demonstrated.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Members/AddTenantMemberFlow.razor; src/Hexalith.Tenants.UI/Components/Tenants/Members/ChangeTenantMemberRoleFlow.razor
evidence-kind: recommendation
priority: P3
owner: unassigned
dependencies: none
next-action: Evaluate dispatcher/reentrancy semantics with callback tests before replacing the primitive.
related: DW-267

### DW-267: Coalescer, submit guard, lease plumbing and `SafeMessageText` are copy-pasted across flow components
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: AddTenantMemberFlow.razor:397-416; ChangeTenantMemberRoleFlow.razor:452-471
reason: The legacy ledger defers this issue: Coalescer, submit guard, lease plumbing and `SafeMessageText` are copy-pasted across flow components. Original context is preserved in legacy-detail.
legacy-detail: - summary: Coalescer, submit guard, lease plumbing and `SafeMessageText` are copy-pasted across flow components. evidence: `AddTenantMemberFlow.razor:397-416` is byte-identical to `ChangeTenantMemberRoleFlow.razor:452-471` including its six-line comment; `SetCommandActivityRaisedAsync` duplicated at `:315-341`/`:374-400`; `SafeMessageText` duplicated at `CreateTenantFlow.razor:213-218` and `EditTenantMetadataFlow.razor:301-306`.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Tenants/Members/AddTenantMemberFlow.razor and ChangeTenantMemberRoleFlow.razor still duplicate coalescing/lease/safe-message plumbing; CreateTenantFlow.razor and Metadata/EditTenantMetadataFlow.razor retain related copies.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Members/AddTenantMemberFlow.razor; src/Hexalith.Tenants.UI/Components/Tenants/Members/ChangeTenantMemberRoleFlow.razor; src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor; src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor
evidence-kind: recommendation
priority: P3
owner: unassigned
dependencies: DW-245 and DW-300
next-action: Extract shared plumbing only after fixing/pinning the meaningful identity and refresh semantics.
related: DW-248, DW-266

### DW-268: The scoped-CSS-on-a-Fluent-host trap predates this change in five other components
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: UserMembershipLookupPanel.razor.css; TenantAuditPage.razor.css
reason: The legacy ledger defers this issue: The scoped-CSS-on-a-Fluent-host trap predates this change in five other components. Original context is preserved in legacy-detail.
legacy-detail: - summary: The scoped-CSS-on-a-Fluent-host trap predates this change in five other components. evidence: Plain scoped selectors are applied to classes placed on Fluent components in `UserMembershipLookupPanel.razor.css`, `TenantAuditPage.razor.css`, `GlobalAdministratorsPage.razor.css`, `AuditEvidenceReceipt.razor.css` and `AuditEvidenceEntryPoint.razor.css`. Per Microsoft's CSS-isolation contract, scoped CSS applies to HTML elements only, so these selectors cannot match. Only the four `TenantConfigurationView` wrappers are a regression introduced by this story.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Users/UserMembershipLookupPanel.razor.css and Pages/TenantAuditPage.razor.css, GlobalAdministratorsPage.razor.css plus Tenants/Audit styles still contain plain scoped selectors for classes placed on child Fluent hosts. Some rules cannot match CSS-isolated child markup; each target needs inspection.
verification-files: src/Hexalith.Tenants.UI/Components/Users/UserMembershipLookupPanel.razor.css; src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor.css; src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor.css
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: browser style evidence
next-action: Audit each host selector, repair ownership/::deep where needed and confirm computed styles at every supported width.
related: DW-317

### DW-269: Inserting `Available` mid-enum shifts `MissingSupport`'s numeric value
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: TenantAuditAvailability.cs:5-11
reason: The legacy ledger defers this issue: Inserting `Available` mid-enum shifts `MissingSupport`'s numeric value. Original context is preserved in legacy-detail.
legacy-detail: - summary: Inserting `Available` mid-enum shifts `MissingSupport`'s numeric value. evidence: `TenantAuditAvailability.cs:5-11`. Harmless today (no numeric persistence or interop), but it makes the enum unsafe to serialize by value later.
status: open
disposition: accepted-limitation
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditAvailabilityState.cs still places Available before MissingSupport with implicit values. No numeric persistence/interoperability usage is established; the original finding explicitly records a harmless historical shift rather than an active serialized contract.
verification-files: src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditAvailabilityState.cs
evidence-kind: accepted-limitation
priority: P3
owner: unassigned
dependencies: none
next-action: Assign/pin values before introducing numeric serialization; retain the historical observation.
related: none

### DW-270: French resource additions are inconsistently accented against their neighbours
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: TenantsResources.fr.resx:3141
reason: The legacy ledger defers this issue: French resource additions are inconsistently accented against their neighbours. Original context is preserved in legacy-detail.
legacy-detail: - summary: French resource additions are inconsistently accented against their neighbours. evidence: `TenantsResources.fr.resx:3141` is fully accented while `:3138` and `:3147` are deliberately unaccented; the same split appears at `:2562-2567` versus `:2134`. The same screen can render both conventions.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx still mixes accented labels with Identite/Disponibilite/Categorie/Resultat/Reference entries on the same surfaces.
evidence-kind: verified-defect
priority: P3
owner: unassigned
dependencies: none
next-action: Run a focused French copy pass with resource parity preserved.
related: DW-306, DW-324, DW-439

### DW-271: `TenantAggregateCommandAdmissionGate`'s public API changed shape without an obsolete overload

status: done 2026-08-28
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
archived: 2026-10-12

### DW-272: The audit-capability probe has no reconnect subscription, and every read refresh briefly blocks removal
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: TenantDetailPage.razor:823; MemberAccessReview.razor:611-613
reason: The legacy ledger defers this issue: The audit-capability probe has no reconnect subscription, and every read refresh briefly blocks removal. Original context is preserved in legacy-detail.
legacy-detail: - summary: The audit-capability probe has no reconnect subscription, and every read refresh briefly blocks removal. evidence: `TenantDetailPage.razor:823` clears `_auditProofCapabilityAvailable` before restarting the probe, and `MemberAccessReview.razor:611-613` turns that into `UnavailableReason.MissingAuditProof`, so every refresh (including SignalR-nudged ones) flips Remove to unavailable with a misleading reason until the extra round trip lands. A `BffComposition` reconnect never re-probes.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor clears _auditProofCapabilityAvailable at every detail/combined refresh before probing; Members/MemberAccessReview.razor maps false to MissingAuditProof. No composition reconnect event triggers an immediate re-probe.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor; src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: BFF connectivity signal
next-action: Distinguish pending probe from missing support and refresh capability on a governed reconnect signal.
related: none

### DW-273: `messageId` remains absent from six `ITenantCommandGateway` methods

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
archived: 2026-10-12

### DW-274: `TenantQueryGateway` dereferences `Detail!` inside the catch that exists to fail safe

status: done 2026-08-26
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
archived: 2026-10-12

### DW-275: `HasSameTenantDetail` newly compares `ConfigurationManagement.TenantId`

status: done 2026-09-06
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
archived: 2026-10-12

### DW-276: `_commandInFlight` is handled inconsistently across the two lease-refusal paths in one method
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: TenantDetailPage.razor:1681-1686
reason: The legacy ledger defers this issue: `_commandInFlight` is handled inconsistently across the two lease-refusal paths in one method. Original context is preserved in legacy-detail.
legacy-detail: - summary: `_commandInFlight` is handled inconsistently across the two lease-refusal paths in one method. evidence: `TenantDetailPage.razor:1681-1686` returns `false` leaving a stale `true`; `:1705-1709` explicitly clears it first. The removed code carried a comment explaining the no-lockable-identity path; it was dropped rather than preserved or refuted.
status: open
disposition: uncertain
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor SetCommandActivityLeaseAsync still returns false for missing identity/gate without clearing _commandInFlight, while contention clears it. The owning-lease invariants may make unconditional clearing incorrect; no reproduced stale-true path is established.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor
evidence-kind: unverified-risk
priority: P3
owner: unassigned
dependencies: none
next-action: Hold an existing lease and exercise each refusal path to establish the intended flag/owner behavior.
related: none

### DW-277: A `TenantId` change does not notify non-keyed command surfaces that their lease was revoked

status: done 2026-08-28
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
archived: 2026-10-12

### DW-278: The global-administrator aggregation loop uses `ContainsKey`+`Add` and silently drops duplicate or null `UserId` rows

status: done 2026-08-28
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
archived: 2026-10-12

### DW-279: Add and change-role `retryMessageId` exclude the `Rejected` state

status: done 2026-09-06
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
archived: 2026-10-12

### DW-280: `MemberAccessReview` sets child lease ownership after the await

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
archived: 2026-10-12

### DW-281: `CreateTenantFlow` and `TenantsWorkspace` findings were raised against files a peer session rewrote mid-review

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
archived: 2026-10-12

### DW-282: `TenantsWorkspace` resolves `ITenantsBffComposition` per render and duplicates its own absence predicate
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: TenantsWorkspace
reason: The legacy ledger defers this issue: `TenantsWorkspace` resolves `ITenantsBffComposition` per render and duplicates its own absence predicate. Original context is preserved in legacy-detail.
legacy-detail: - summary: `TenantsWorkspace` resolves `ITenantsBffComposition` per render and duplicates its own absence predicate. evidence: `:418-420` uses the untyped `Services.GetService(typeof(...))` inside a per-render property with no caching, where `TenantDetailPage` caches the equivalent in a field; the `Empty && IsAuthorizationScopedEmpty` predicate appears at both `:413-414` and `:159` and must not be allowed to drift.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor IsCommandSurfaceConnected still resolves ITenantsBffComposition via Services.GetService at render time, with duplicated disconnected/authorization-empty predicates and no reconnect subscription.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor
evidence-kind: recommendation
priority: P3
owner: unassigned
dependencies: none
next-action: Resolve composition consistently and centralize the absence predicate; specify whether reconnect needs notification.
related: DW-258, DW-282
duplicate-of: DW-258

### DW-283: An eighth undeclared `references/` pointer move appeared during this review

status: done 2026-08-27
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
archived: 2026-10-12

### DW-284: Exercise the production create-attempt tracker across a real component remount
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: Remember
source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md`
reason: The legacy ledger defers this issue: Exercise the production create-attempt tracker across a real component remount. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md` summary: Exercise the production create-attempt tracker across a real component remount. evidence: Existing remount coverage pre-seeds a tracker manually and does not prove the scoped registration plus production `Remember` call preserve the original intent and baseline from the first dispatched flow.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: tests/Hexalith.Tenants.UI.Tests/Components/CreateTenantFlowTests.cs retained-create cases seed a tracker; State/TenantCommands/TenantCreateAttemptTracker.cs stores only a handle. No production-dispatch-to-remount proof preserves original intent/baseline through registration plus Remember.
verification-files: tests/Hexalith.Tenants.UI.Tests/Components/CreateTenantFlowTests.cs; src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateAttemptTracker.cs
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: none
next-action: Dispatch through the first component, remount using real scoped services, and assert no redispatch plus correct baseline retention.
related: none

### DW-285: Guard create and membership submit flows against re-entrant snapshot replacement while a dispatch is awaiting completion

status: done 2026-10-16
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md`
priority: P2
archived: 2026-10-16

### DW-286: Require current authoritative projection state for create and membership command confirmation evidence

status: done 2026-10-13
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md`
priority: P1
archived: 2026-10-13

### DW-287: Retain aggregate command admission across tenant route changes and disposal until the old command is terminal

status: done 2026-10-13
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md`
priority: P1
archived: 2026-10-13

### DW-288: Preserve the resolved create-command message ID after an indeterminate submission so an exact retry cannot mint a duplicate identity

status: done 2026-10-13
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md`
priority: P1
archived: 2026-10-13

### DW-289: Define causal projection-change handling for valid opaque or content-hash version tokens across command flows

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md`
archived: 2026-10-12

### DW-290: Preserve a queued manual projection refresh when add-member or change-role reconciliation is already processing a status-only nudge

status: done 2026-10-16
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md`
priority: P2
archived: 2026-10-16

### DW-291: Reconcile Epic 3 tracker state so an epic and retrospective are not marked done while Story 3.2 is in review and Stories 3.3 through 3.6 remain backlog

status: done 2026-08-28
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md`
archived: 2026-10-12

### DW-292: Add executable in-repository verification for the TEA enforcement hook assets included in the reviewed range
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof — loop 3 final verification (2026-08-21)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md`
reason: The legacy ledger defers this issue: Add executable in-repository verification for the TEA enforcement hook assets included in the reviewed range. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md` summary: Add executable in-repository verification for the TEA enforcement hook assets included in the reviewed range. evidence: The mirrored `tea-enforce.cjs` assets advertise focused-test and registry-completeness enforcement, but no repository test imports the scanner or executes its pre/post/stop modes, so a disabled rule or an always-successful entry point would remain green.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: .claude/skills/bmad-testarch-framework/resources/hooks/tea-enforce.cjs and .agents/skills/bmad-testarch-framework/resources/hooks/tea-enforce.cjs remain; repository tests do not import/execute their pre/post/stop scanner contract.
evidence-kind: verified-gap
priority: P3
owner: unassigned
dependencies: TEA hook test harness
next-action: Add focused scanner mode/registry/failure regressions in owning tooling tests.
related: none

### DW-293: Blank `CommandSurfaceUnavailableReason` maps to `UnavailableReason.AggregateLocked`'s copy, which asserts a specific ("another command is already in progress") cause that may not be true
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4b-wp-2a-removal-proof-and-audit-available (2026-08-22)"), 2026-08-25
location: MemberAccessReview.razor:660
source_spec: `_bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md`
reason: The legacy ledger defers this issue: Blank `CommandSurfaceUnavailableReason` maps to `UnavailableReason.AggregateLocked`'s copy, which asserts a specific ("another command is already in progress") cause that may not be true. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md` summary: Blank `CommandSurfaceUnavailableReason` maps to `UnavailableReason.AggregateLocked`'s copy, which asserts a specific ("another command is already in progress") cause that may not be true. evidence: `ResolveFailClosedReasons` (`MemberAccessReview.razor:660`) returns `AggregateLocked` whenever `!IsCommandSurfaceAvailable`, even with an empty reason string (e.g. a missing admission-gate registration with no actual contention); there is no generic "support unavailable" `UnavailableReason` value. Fail-closed behavior is correct; only the specific wording is inaccurate. Needs a dedicated wording/UX pass (new enum value + EN/FR copy) rather than a fold-in fix.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor ResolveFailClosedReasons still maps blank unavailable support and its own _childCommandLeaseOwner to AggregateLocked; ReasonLabel can therefore say another command when no other command is established.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor
evidence-kind: verified-defect
priority: P3
owner: unassigned
dependencies: none
next-action: Introduce reason-honest unavailable/self-owned copy and make the ledger the decision authority.
related: DW-293, DW-294, DW-298

### DW-294: Self-lock reason text — a row whose own flow currently holds `_childCommandLeaseOwner` also renders its own launcher buttons as `AggregateLocked` ("another command is already in progress"), which is imprecise for its own open flow
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4b-wp-2a-removal-proof-and-audit-available (2026-08-22, pass 2)"), 2026-08-25
location: MemberAccessReview.razor:663
source_spec: `_bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md`
reason: The legacy ledger defers this issue: Self-lock reason text — a row whose own flow currently holds `_childCommandLeaseOwner` also renders its own launcher buttons as `AggregateLocked` ("another command is already in progress"), which is imprecise for its own open flow. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md` summary: Self-lock reason text — a row whose own flow currently holds `_childCommandLeaseOwner` also renders its own launcher buttons as `AggregateLocked` ("another command is already in progress"), which is imprecise for its own open flow. evidence: `ResolveFailClosedReasons` (`MemberAccessReview.razor:663`) checks `_childCommandLeaseOwner is not null` unconditionally for every row, including the row whose own flow holds the lease; not newly introduced by this diff (the row was already gated unavailable via `IsCommandSurfaceAvailable` once any child flow raises the parent lease) — same class of issue as the item above; fold into that dedicated wording/UX pass rather than treating separately.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor ResolveFailClosedReasons still maps blank unavailable support and its own _childCommandLeaseOwner to AggregateLocked; ReasonLabel can therefore say another command when no other command is established.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor
evidence-kind: verified-defect
priority: P3
owner: unassigned
dependencies: none
next-action: Introduce reason-honest unavailable/self-owned copy and make the ledger the decision authority.
related: DW-293, DW-294, DW-298

### DW-295: New `EventId(2001, "TenantProjectionNullEventSkipped")` is an unregistered magic literal with no cross-project collision check

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4b-wp-2a-removal-proof-and-audit-available (2026-08-22, pass 2)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md`
archived: 2026-10-12

### DW-296: New `TenantProjectionVersionFormat` type deliberately sits outside the namespaces `EventContractReferenceDocumentationTests` sweeps, setting a precedent for future non-contract public types to bypass the assembly's only doc-completeness governance check

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4b-wp-2a-removal-proof-and-audit-available (2026-08-22, pass 2)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md`
archived: 2026-10-12

### DW-297: The Release-build `.slnx` topology revert is validated only in review-findings prose, not captured as a reproducible command in the spec's formal Verification section
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4b-wp-2a-removal-proof-and-audit-available (2026-08-22, pass 2)"), 2026-08-25
location: dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore; spec-2-4b-wp-2a-removal-proof-and-audit-available.md:123
source_spec: `_bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md`
reason: The legacy ledger defers this issue: The Release-build `.slnx` topology revert is validated only in review-findings prose, not captured as a reproducible command in the spec's formal Verification section. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md` summary: The Release-build `.slnx` topology revert is validated only in review-findings prose, not captured as a reproducible command in the spec's formal Verification section. evidence: `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore` (0 Warning(s), 0 Error(s)) is recorded only inside a Review Findings bullet; `## Verification` at `spec-2-4b-wp-2a-removal-proof-and-audit-available.md:123` lists only a filtered `dotnet test` command, so a reader relying on Verification alone would miss the full-solution Release build check.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: _bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md records the successful Release build in Review Findings, but formal Verification still lists only its filtered test command.
verification-files: _bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md
evidence-kind: verified-gap
priority: P3
owner: unassigned
dependencies: none
next-action: Add the existing exact Release validation command/result to formal verification without rerunning historical work.
related: none

### DW-298: The "blank `CommandSurfaceUnavailableReason` → `AggregateLocked` copy" deferred decision is now recorded independently, with drifting prose, in both `deferred-work.md` and the spec's own Review Findings section
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4b-wp-2a-removal-proof-and-audit-available (2026-08-22, pass 2)"), 2026-08-25
location: deferred-work.md; spec-2-4b-wp-2a-removal-proof-and-audit-available.md
source_spec: `_bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md`
reason: The legacy ledger defers this issue: The "blank `CommandSurfaceUnavailableReason` → `AggregateLocked` copy" deferred decision is now recorded independently, with drifting prose, in both `deferred-work.md` and the spec's own Review Findings section. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md` summary: The "blank `CommandSurfaceUnavailableReason` → `AggregateLocked` copy" deferred decision is now recorded independently, with drifting prose, in both `deferred-work.md` and the spec's own Review Findings section. evidence: Compare this file's entry above (2026-08-22) against `spec-2-4b-wp-2a-removal-proof-and-audit-available.md`'s Review Findings "DEFERRED (2026-08-22): ACCEPT CURRENT COPY FOR THIS PASS" bullet — same decision, different prose framing. Two sources of truth for one decision invite silent divergence on the next edit.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: _bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md still records ACCEPT CURRENT COPY separately from DW-293/294. This reconciliation declares deferred-work.md authoritative, but the spec lacks a DW cross-reference and may drift.
verification-files: _bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md; _bmad-output/implementation-artifacts/deferred-work.md
evidence-kind: recommendation
priority: P3
owner: unassigned
dependencies: DW-293 copy decision
next-action: Replace the spec decision restatement with a historical note linking DW-293/294.
related: DW-293, DW-294

### DW-299: Add browser-level computed-visibility coverage for the narrow configuration set and remove forms
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4b-wp-2a-removal-proof-and-audit-available (2026-08-22, pass 2)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-3-4-disable-or-enable-tenant-with-complete-preview.md`
reason: The legacy ledger defers this issue: Add browser-level computed-visibility coverage for the narrow configuration set and remove forms. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-4-disable-or-enable-tenant-with-complete-preview.md` summary: Add browser-level computed-visibility coverage for the narrow configuration set and remove forms. evidence: Existing tests inspect stylesheet text only; they do not prove that the forms are hidden and the safety warning is visible at 767px, then available again at 768px. This is real configuration-flow work owned by Stories 3.5 and 3.6, not the Story 3.4 lifecycle flow.
status: open
disposition: blocked
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Tenants/Configuration/SetTenantConfigurationFlow.razor.css and RemoveTenantConfigurationFlow.razor.css use ::deep for narrow hide rules, but component stylesheet-text tests do not establish computed visibility at 767/768px.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Configuration/SetTenantConfigurationFlow.razor.css; src/Hexalith.Tenants.UI/Components/Tenants/Configuration/RemoveTenantConfigurationFlow.razor.css
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: authenticated browser configuration fixture
next-action: Capture real set/remove form and warning visibility at both sides of the breakpoint.
related: DW-317

### DW-300: Nine other command flows (create, add/remove/change member, metadata, set/remove configuration, global-admin grant/remove) still construct a 2-arg `TenantCommandTrackingHandle` with no aggregate id, so they keep accepting a status response for a different command and keep treating propagation lag as terminal. Story 3.4's Boundaries fence this under "Ask First: broadening shared command infrastructure beyond the focused lifecycle seam"

status: done 2026-10-13
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-25, loop 2)"), 2026-08-25
priority: P1
archived: 2026-10-13

### DW-301: `TenantDetailPage`'s `ResetLifecycleProofScope`/`BeginLifecycleProof`/`CanApplyLifecycleProof`/`CompleteLifecycleProof` quartet is a verbatim copy of the metadata quartet, and `TenantQueryGateway.GetLifecycleProjectionProofAsync` is byte-identical to `GetMetadataProjectionProofAsync`. Extract a keyed `ProofScope` helper before a third command surface needs one
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-25, loop 2)"), 2026-08-25
location: TenantDetailPage
reason: The legacy ledger defers this issue: `TenantDetailPage`'s `ResetLifecycleProofScope`/`BeginLifecycleProof`/`CanApplyLifecycleProof`/`CompleteLifecycleProof` quartet is a verbatim copy of the metadata quartet, and `TenantQueryGateway.GetLifecycleProjectionProofAsync` is byte-identical to `GetMetadataProjectionProofAsync`. Original context is preserved in legacy-detail.
legacy-detail: - `TenantDetailPage`'s `ResetLifecycleProofScope`/`BeginLifecycleProof`/`CanApplyLifecycleProof`/`CompleteLifecycleProof` quartet is a verbatim copy of the metadata quartet, and `TenantQueryGateway.GetLifecycleProjectionProofAsync` is byte-identical to `GetMetadataProjectionProofAsync`. Extract a keyed `ProofScope` helper before a third command surface needs one.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor still has separate metadata/lifecycle proof-generation/CTS blocks; Services/Gateways/TenantQueryGateway.cs has parallel unconditional proof seams.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor; src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs
evidence-kind: recommendation
priority: P3
owner: unassigned
dependencies: none
next-action: Extract a keyed proof-scope helper with cancellation and route-generation behavior pinned first.
related: none

### DW-302: `ProjectionVersion` is threaded through four carriers (page parameter, `HighImpactEvidence`, `ResolveAvailability` override, `TenantLifecycleAvailabilityInput`). In production all four agree, so the override is untestable no-op logic that diverges only under test doubles. Pick one carrier
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-25, loop 2)"), 2026-08-25
location: ProjectionVersion
reason: The legacy ledger defers this issue: `ProjectionVersion` is threaded through four carriers (page parameter, `HighImpactEvidence`, `ResolveAvailability` override, `TenantLifecycleAvailabilityInput`). Original context is preserved in legacy-detail.
legacy-detail: - `ProjectionVersion` is threaded through four carriers (page parameter, `HighImpactEvidence`, `ResolveAvailability` override, `TenantLifecycleAvailabilityInput`). In production all four agree, so the override is untestable no-op logic that diverges only under test doubles. Pick one carrier.
status: open
decision: 2026-08-27 Evidence is canonical — Make TenantHighImpactActionEvidence the canonical version source, remove the override path, and retain compatibility shims for public component parameters during migration.
decision: 2026-08-27 Evidence is canonical — Make TenantHighImpactActionEvidence the canonical version source, remove the override path, and retain compatibility shims for public component parameters during migration.
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleActionAvailability.razor ResolveAvailability still overlays ProjectionVersion onto supplied evidence; TenantLifecycleCommandFlow.razor carries its own override. Existing 2026-08-27 evidence-canonical decision is not implemented.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleActionAvailability.razor; src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleCommandFlow.razor
evidence-kind: recommendation
priority: P3
owner: unassigned
dependencies: public component compatibility
next-action: Make high-impact evidence canonical and retain only explicit compatibility shims for public parameters.
related: none

### DW-303: A blank `ProjectionVersion` fails closed as `UnavailableReason.StaleData`, bricking both lifecycle buttons behind "authoritative data is not current" — a cause no refresh can fix and which misdirects support. Needs a distinct reason or recovery key
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-25, loop 2)"), 2026-08-25
location: ProjectionVersion
reason: The legacy ledger defers this issue: A blank `ProjectionVersion` fails closed as `UnavailableReason.StaleData`, bricking both lifecycle buttons behind "authoritative data is not current" — a cause no refresh can fix and which misdirects support. Original context is preserved in legacy-detail.
legacy-detail: - A blank `ProjectionVersion` fails closed as `UnavailableReason.StaleData`, bricking both lifecycle buttons behind "authoritative data is not current" — a cause no refresh can fix and which misdirects support. Needs a distinct reason or recovery key.
status: open
decision: 2026-08-27 Specific recovery key — Keep the existing unavailable-reason enum stable, detect the missing-version branch explicitly at the lifecycle component boundary, and emit dedicated EN/FR message and recovery keys with focused tests.
decision: 2026-08-27 Specific recovery key — Keep the existing unavailable-reason enum stable, detect the missing-version branch explicitly at the lifecycle component boundary, and emit dedicated EN/FR message and recovery keys with focused tests.
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/State/TenantDetail/TenantHighImpactActionAvailabilityEvaluator.cs maps missing ProjectionVersion to StaleData; TenantLifecycleAvailability.cs still has no dedicated missing-version recovery mapping. The recorded 2026-08-27 specific-recovery-key decision remains pending.
verification-files: src/Hexalith.Tenants.UI/State/TenantDetail/TenantHighImpactActionAvailabilityEvaluator.cs; src/Hexalith.Tenants.UI/State/TenantDetail/TenantLifecycleAvailability.cs
evidence-kind: verified-defect
priority: P3
owner: unassigned
dependencies: none
next-action: Implement the recorded lifecycle-boundary EN/FR missing-version message/recovery without renumbering the enum.
related: none

### DW-304: The ten-item preview renders from `_snapshot.LastConfirmedProjection ?? Detail` while the eligibility gate validates `Detail`/`ResolvedEvidence`; after an in-flow Refresh the user can be shown facts no gate validated. Fails closed at submit, so consistency rather than correctness. Fix is ambiguous (which source wins)

status: done 2026-08-26
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-25, loop 2)"), 2026-08-25
archived: 2026-10-12

### DW-305: `tenants-lifecycle-unavailable-reason` is emitted by both the launcher (per action) and the open flow; with the flow open, up to three elements share the testid with different semantics

status: done 2026-08-26
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-25, loop 2)"), 2026-08-25
archived: 2026-10-12

### DW-306: The French accent repair is partial: ~30 entries fixed, but `Tenants.GlobalAdministrators.Column.Identity`/`.Availability` and `Tenants.Audit.Column.Category`/`.Outcome` remain unaccented. Key parity is clean (1346/1346). Finish in a dedicated pass
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-25, loop 2)"), 2026-08-25
location: Availability
reason: The legacy ledger defers this issue: The French accent repair is partial: ~30 entries fixed, but `Tenants.GlobalAdministrators.Column.Identity`/`.Availability` and `Tenants.Audit.Column.Category`/`.Outcome` remain unaccented. Original context is preserved in legacy-detail.
legacy-detail: - The French accent repair is partial: ~30 entries fixed, but `Tenants.GlobalAdministrators.Column.Identity`/`.Availability` and `Tenants.Audit.Column.Category`/`.Outcome` remain unaccented. Key parity is clean (1346/1346). Finish in a dedicated pass.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx still contains Disponibilite accorder/retirer, Identite administrateur, Categorie and Resultat for the cited global/audit column keys.
evidence-kind: verified-defect
priority: P3
owner: unassigned
dependencies: none
next-action: Correct the four cited labels in the French copy pass.
related: DW-270, DW-324

### DW-307: `TenantLifecycleAttemptTracker` has no attempt expiry and never prunes `_terminalMessageByTenantId`/`_terminalAttemptStartedAtByTenantId`; both grow for the circuit's lifetime. Subsumed by the open decision on bounding a wedged attempt

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-25, loop 2)"), 2026-08-25
archived: 2026-10-12

### DW-308: `TenantConfigurationManagementContext`'s null `authorityState` default (implicit `TenantOwner` grant) and `TenantConfigurationSafeComposer`'s `_ = tenantStatus;` discard were documented with comments rather than removed
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-25, loop 2)"), 2026-08-25
location: TenantConfigurationManagementContext
reason: The legacy ledger defers this issue: `TenantConfigurationManagementContext`'s null `authorityState` default (implicit `TenantOwner` grant) and `TenantConfigurationSafeComposer`'s `_ = tenantStatus;` discard were documented with comments rather than removed. Original context is preserved in legacy-detail.
legacy-detail: - `TenantConfigurationManagementContext`'s null `authorityState` default (implicit `TenantOwner` grant) and `TenantConfigurationSafeComposer`'s `_ = tenantStatus;` discard were documented with comments rather than removed.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/State/TenantDetail/TenantConfigurationManagementContext.cs Available still accepts authorityState=null and grants TenantOwner for nonglobal callers; Services/Configuration/TenantConfigurationSafeComposer.cs still discards tenantStatus in authority helpers. Production supplies explicit evidence, but the public implicit-grant trap remains.
verification-files: src/Hexalith.Tenants.UI/State/TenantDetail/TenantConfigurationManagementContext.cs; src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationSafeComposer.cs
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: none
next-action: Require explicit authority at construction and reconcile unused status parameters with compatibility tests.
related: DW-308, DW-314

### DW-309: `_hasAdoptedRetainedAttempt` is latched before the tracker lookup, and a `Detail.TenantId` change on a mounted flow is never re-adopted. Latent only — the parent renders the flow solely for a loaded tenant

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-25, loop 2)"), 2026-08-25
archived: 2026-10-12

### DW-310: `TenantLifecycleAttemptTracker.Remember` compares `AttemptStartedAtUtc` with `<=`, so two attempts within one clock tick collapse. Compare `(AttemptStartedAtUtc, MessageId)` or use a monotonic sequence

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-25, loop 2)"), 2026-08-25
archived: 2026-10-12

### DW-311: `Remember` mixes contracts: `SetSnapshot` treats `false` as a tracking mismatch, but the method throws `ArgumentException` for shape violations, which escape unhandled from a UI event handler

status: done 2026-08-25
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-25, loop 2)"), 2026-08-25
archived: 2026-10-12

### DW-312: Preserve a visible exit when the set-configuration flow is opened wide and the viewport is then narrowed

status: done 2026-08-26
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-25, loop 2)"), 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-3-4-disable-or-enable-tenant-with-complete-preview.md`
archived: 2026-10-12

### DW-313: `TenantLifecycleAttemptTracker` reimplements `TenantCreateAttemptTracker` without sharing
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-25)"), 2026-08-25
location: src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateAttemptTracker.cs:24-70; TenantsUiServiceCollectionExtensions.cs:98
reason: The legacy ledger defers this issue: `TenantLifecycleAttemptTracker` reimplements `TenantCreateAttemptTracker` without sharing. Original context is preserved in legacy-detail.
legacy-detail: - **`TenantLifecycleAttemptTracker` reimplements `TenantCreateAttemptTracker` without sharing** — `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateAttemptTracker.cs:24-70` is the same circuit-scoped, `StringComparer.Ordinal`-keyed, lock-guarded per-tenant retention concept, but with no expiry, no terminal tombstones, and a `Forget(tenantId)` carrying exactly the late-completion race the new tracker's docs warn about. Both are registered side by side (`TenantsUiServiceCollectionExtensions.cs:98`). Pre-existing; the create flow is Story 3.1 territory and sharing a generic base is a cross-story refactor. Either share a base or file the create-flow gap explicitly — two divergent answers to one question is the worse outcome.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateAttemptTracker.cs still stores only handles and Forget(tenantId), without expiry/tombstones/intent; TenantLifecycleAttemptTracker.cs has bounded snapshots and dispatch-identity ownership. The trackers remain divergent.
verification-files: src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateAttemptTracker.cs; src/Hexalith.Tenants.UI/State/TenantCommands/TenantLifecycleAttemptTracker.cs
evidence-kind: recommendation
priority: P2
owner: unassigned
dependencies: create identity/ownership contract
next-action: Address create retention/late completion before extracting reusable technical retention behavior.
related: DW-255, DW-284, DW-288

### DW-314: `TenantConfigurationManagementContext.Available` documents the `authorityState = null` landmine instead of removing it
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-25)"), 2026-08-25
location: src/Hexalith.Tenants.UI/State/TenantDetail/TenantConfigurationManagementContext.cs:85-92
reason: The legacy ledger defers this issue: `TenantConfigurationManagementContext.Available` documents the `authorityState = null` landmine instead of removing it. Original context is preserved in legacy-detail.
legacy-detail: - **`TenantConfigurationManagementContext.Available` documents the `authorityState = null` landmine instead of removing it** — `src/Hexalith.Tenants.UI/State/TenantDetail/TenantConfigurationManagementContext.cs:85-92`. The `<remarks>` explains at length that the null default silently grants `TenantOwner`, exists only for tests predating the authority distinction, and "a new test should not rely on" it. A comment cannot stop the next test from taking the 5-argument overload. Second occurrence — also deferred in Loop 2. Deferred again because configuration authority is Story 3.5/3.6 territory, which Story 3.4's "Never" list excludes.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/State/TenantDetail/TenantConfigurationManagementContext.cs Available still accepts authorityState=null and grants TenantOwner for nonglobal callers; Services/Configuration/TenantConfigurationSafeComposer.cs still discards tenantStatus in authority helpers. Production supplies explicit evidence, but the public implicit-grant trap remains.
verification-files: src/Hexalith.Tenants.UI/State/TenantDetail/TenantConfigurationManagementContext.cs; src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationSafeComposer.cs
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: none
next-action: Require explicit authority at construction and reconcile unused status parameters with compatibility tests.
related: DW-308, DW-314
duplicate-of: DW-308

### DW-315: `TenantCommandGateway.BoundSafeFailureReason` returns most backend failure text verbatim

status: done 2026-10-13
origin: deferred from code review of spec-3-4-disable-or-enable-tenant-with-complete-preview, 2026-08-25
source_spec: `_bmad-output/implementation-artifacts/spec-3-4-disable-or-enable-tenant-with-complete-preview.md`
priority: P1
archived: 2026-10-14

### DW-316: Localize all `UnavailableTenantCommandGateway` failure results

status: done 2026-10-16
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview.md (2026-08-25)"), 2026-08-26
priority: P2
archived: 2026-10-16

### DW-317: Repair inert `__form` scoped CSS selectors on configuration and lifecycle flows
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-26)"), 2026-08-26
location: src/Hexalith.Tenants.UI/Components/Tenants/Configuration/SetTenantConfigurationFlow.razor.css:33,41; src/Hexalith.Tenants.UI/Components/Tenants/Configuration/RemoveTenantConfigurationFlow.razor.css:33,41; src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleCommandFlow.razor.css:48-54
reason: The selectors target a class placed on an `<EditForm>` component, which does not receive the CSS-isolation scope attribute, so these layout rules have never applied. Repair scope stamping across all three flows and visually verify every width because the change affects form layout.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Tenants/Configuration/SetTenantConfigurationFlow.razor.css and RemoveTenantConfigurationFlow.razor.css now fix the narrow hide selector with ::deep, but ordinary __form layout/label selectors and Lifecycle/TenantLifecycleCommandFlow.razor.css still target EditForm child output without scope.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Configuration/SetTenantConfigurationFlow.razor.css; src/Hexalith.Tenants.UI/Components/Tenants/Configuration/RemoveTenantConfigurationFlow.razor.css; src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleCommandFlow.razor.css
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: browser layout fixture
next-action: Repair the remaining form/label scope stamping and verify computed layout at supported widths.
related: DW-268, DW-299

### DW-318: Replace the legacy Fluent v4 token in `SetTenantConfigurationFlow`

status: done 2026-08-27
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-26)"), 2026-08-26
archived: 2026-10-12

### DW-319: Retire or deprecate untracked lifecycle dispatch methods
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-26)"), 2026-08-26
location: src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs (`EnableTenantAsync` and `DisableTenantAsync`)
reason: The public, undeprecated methods have zero production callers after the tracked pair took over, but a future caller could use them to bypass `TenantLifecycleAttemptTracker`. Removal is breaking, so retire or deprecate the methods with an explicit compatibility plan.
status: open
decision: 2026-08-26 Deprecate with guidance — Mark methods obsolete, document tracked replacements, and add compatibility tests before later removal.
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs EnableTenantAsync/DisableTenantAsync remain public and undeprecated alongside tracked methods; no production flow uses the untracked pair. Recorded 2026-08-26 deprecate-with-guidance decision is pending.
verification-files: src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs
evidence-kind: recommendation
priority: P3
owner: unassigned
dependencies: none
next-action: Mark the old methods obsolete with tracked replacements and pin compatibility behavior.
related: DW-254, DW-327

### DW-320: Remove the dead lifecycle `DuplicatePrevented` state path
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-26)"), 2026-08-26
location: src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs; src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleCommandFlow.razor
reason: Lifecycle `SubmitAsync` uses `BlockedWithTracking`, which never produces `TenantLifecycleCommandSnapshot.DuplicatePrevented`, while `HasTerminalOwnership` and the lifecycle icon switch still carry that unreachable state path. Remove the lifecycle-only dead handling without disturbing command flows that still use `DuplicatePrevented`.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs still defines lifecycle DuplicatePrevented; Lifecycle/TenantLifecycleCommandFlow.razor SubmitAsync uses BlockedWithTracking while its icon switch still handles DuplicatePrevented.
verification-files: src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs; src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleCommandFlow.razor
evidence-kind: recommendation
priority: P3
owner: unassigned
dependencies: none
next-action: Remove the unused lifecycle-only state path while preserving live duplicate handling in other flows.
related: none

### DW-321: Extract shared lifecycle lease-reclamation and focus helpers
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-26)"), 2026-08-26
location: src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:560,576,593,2336; src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleCommandFlow.razor; src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleActionAvailability.razor
reason: Four admission-owner, retained-lease, and release blocks repeat the same logic with small identity and ownership-polarity differences, while `FocusSafelyAsync` is byte-identical across the two lifecycle components. Extract shared helpers that preserve those deliberate differences.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor retains repeated lifecycle/set/remove-config reclamation and release blocks; Lifecycle/TenantLifecycleCommandFlow.razor and TenantLifecycleActionAvailability.razor duplicate FocusSafelyAsync.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor; src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleCommandFlow.razor; src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleActionAvailability.razor
evidence-kind: recommendation
priority: P3
owner: unassigned
dependencies: lease/focus regression coverage
next-action: Extract helpers after explicitly pinning surface owner identity and retention polarity.
related: none

### DW-322: Move `TenantLifecycleAttemptTracker` pruning off the render path
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-26)"), 2026-08-26
location: src/Hexalith.Tenants.UI/State/TenantCommands/TenantLifecycleAttemptTracker.cs; TenantLifecycleCommandFlow.OnParametersSet; TenantLifecycleActionAvailability.OnParametersSet
reason: `Find` prunes under lock and allocates `ToArray` snapshots of three dictionaries, and both lifecycle components invoke it unconditionally during parameter rendering. Prune on mutation or on a timer so routine renders do not pay the repeated lock and allocation cost.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/State/TenantCommands/TenantLifecycleAttemptTracker.cs Find invokes PruneExpiredLocked under lock, which ToArray-copies three dictionaries; both lifecycle components call Find from parameter rendering.
verification-files: src/Hexalith.Tenants.UI/State/TenantCommands/TenantLifecycleAttemptTracker.cs
evidence-kind: recommendation
priority: P3
owner: unassigned
dependencies: none
next-action: Measure render cost and move pruning to a controlled mutation/expiry seam without losing expiration semantics.
related: none

### DW-323: Enforce or remove `PendingStatusPollCount`
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-26)"), 2026-08-26
location: src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs; src/Hexalith.Tenants.UI/State/TenantCommands/TenantLifecycleAttemptTracker.cs
reason: `PendingStatusPollCount` is incremented, saturated, and merged across attempts but never read as a budget; only the five-minute deadline bounds polling. Enforce a poll cap or remove the unused counter and its `MergeSameAttempt` plumbing.
status: open
decision: 2026-08-27 Enforce poll budget — Define a tested maximum pending-poll count, transition safely to UnableToVerify when exhausted, and keep the existing public field operational.
decision: 2026-08-27 Enforce poll budget — Define a tested maximum pending-poll count, transition safely to UnableToVerify when exhausted, and keep the existing public field operational.
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs lifecycle pending polls increment PendingStatusPollCount, but only elapsed five-minute expiry bounds them; TenantLifecycleAttemptTracker.cs only merges the count. Existing 2026-08-27 poll-budget decision remains pending.
verification-files: src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs; src/Hexalith.Tenants.UI/State/TenantCommands/TenantLifecycleAttemptTracker.cs
evidence-kind: recommendation
priority: P3
owner: unassigned
dependencies: configured poll budget
next-action: Implement the recorded tested poll cap and safe UnableToVerify transition.
related: DW-247

### DW-324: French audit resource values spell "Reference" without its accent, so a French operator reads unaccented labels where the rest of the file is correctly accented.
origin: spec-deferred 1be4f24f2760
location: src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx:3374
source_spec: `spec-deferred-work-support-safe-copy-followup.md`
severity: low
reason: TenantsResources.fr.resx uses accented French throughout ("Non connecte" is spelled "Non connecté" at line 19, "Périmé" at line 85), and line 3707 already carries "Référence d'audit d'origine". The audit block is the exception: line 3374 "Reference d'audit : {0}", line 3380 "Reference de commande", line 3389 "Reference d'audit", line 3410 "Reference indisponible". Lines 3380 and 3389 predate this story, so the cluster is pre-existing rather than introduced here.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx retains Reference de commande and Reference indisponible in the audit block despite later accented receipt strings.
evidence-kind: verified-defect
priority: P3
owner: unassigned
dependencies: none
next-action: Finish the cited audit accent/apostrophe pass with EN/FR parity.
related: DW-270, DW-306, DW-439

### DW-325: TenantConfigurationManagement latches _removeCommandInFlight from a retained attempt with no reset branch, so the flag survives the tracker's autonomous expiry when the flow is unmounted.
origin: spec-deferred 1af6159500d9
location: src/Hexalith.Tenants.UI/Components/Tenants/Configuration/TenantConfigurationManagement.razor:363
source_spec: `spec-3-6-remove-configuration-key-with-complete-preview.md`
severity: low
reason: OnParametersSet sets _removeCommandInFlight = true whenever RemoveAttemptTracker.Find returns a retained attempt, and only the flow's own lease callback lowers it. Tracker expiry raised while the flow is unmounted never runs that callback. The obvious else-reset was implemented and reverted: it lowers ChildCommandInFlight at the instant of confirmation, and the management landmark then replaces both flows with the unavailable paragraph before the operator can see the terminal state (Matching_signalr_notification_reconciles_retained_remove_without_redispatch_or_nudge_success fails). Impact is limited: the page clears its own _commandInFlight on expiry, so IsCommandSurfaceAvailable still recovers. A correct fix needs a distinct "flow owns the lease" signal.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Tenants/Configuration/TenantConfigurationManagement.razor OnParametersSet still only raises _removeCommandInFlight for retained attempts; there is no reset when a tracker expires while the child is unmounted. Original terminal-unmount regression prevents a naive else-reset.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Configuration/TenantConfigurationManagement.razor
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: retained remove activity ownership
next-action: Represent flow-owned activity separately from retained-attempt activity and pin expiry plus visible confirmation.
related: DW-587

### DW-326: One of the ten mandated preview facts is a constant, and roughly two dozen enum-keyed EN/FR strings can never render.
origin: spec-deferred 3352ed6ca1f0
location: src/Hexalith.Tenants.UI/State/TenantCommands/TenantRemoveConfigurationPreview.cs:50
source_spec: `spec-3-6-remove-configuration-key-with-complete-preview.md`
severity: low
reason: TenantRemoveConfigurationPreview.IsComplete requires IsAuthoritative, which requires Freshness == Current and Lifecycle == Current. PreviewItems returns [] unless IsComplete, so the merged "Read model: {0}; projection lifecycle: {1}." fact always reads Current/Current, and only one of the Remove.Freshness.*, Remove.Lifecycle.* and Preview.CurrentState.* values is ever reachable. A degraded-freshness operator sees a block message instead of a degraded-freshness fact.
status: open
decision: 2026-08-28 Keep ten-fact contract — Keep the complete-preview contract, document Current and Current as an explicit proof fact, prune unreachable resources, and pin the intentional constant in tests.
decision: 2026-08-28 Keep ten-fact contract — Keep the complete-preview contract, document Current and Current as an explicit proof fact, prune unreachable resources, and pin the intentional constant in tests.
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/State/TenantCommands/TenantRemoveConfigurationPreview.cs IsComplete requires Current freshness/lifecycle; Configuration/RemoveTenantConfigurationFlow.razor renders the combined fact only for complete previews. Recorded 2026-08-28 decision intentionally keeps this proof fact, but unreachable resource cleanup/pinning remains.
verification-files: src/Hexalith.Tenants.UI/State/TenantCommands/TenantRemoveConfigurationPreview.cs; src/Hexalith.Tenants.UI/Components/Tenants/Configuration/RemoveTenantConfigurationFlow.razor
evidence-kind: recommendation
priority: P3
owner: unassigned
dependencies: existing ten-fact acceptance contract
next-action: Document/pin the intentional Current/Current proof fact and prune unreachable resource variants.
related: none

### DW-327: The untracked RemoveTenantConfigurationAsync overload silently changed its failure contract to keyed, SafeMessage-null results.
origin: spec-deferred ee1c75c4860f
location: src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs:273
source_spec: `spec-3-6-remove-configuration-key-with-complete-preview.md`
severity: low
reason: It now delegates to RemoveTenantConfigurationTrackedAsync, so it can return Ambiguous or FailedWithKey("Tenants.Commands.Unavailable.InvalidTrackingReference") with SafeMessage null. No production caller remains, and no test pins the overload, so a future caller rendering SafeMessage would show empty text.
status: open
decision: 2026-08-28 Deprecate compatibly — Mark the overload obsolete, document the tracked replacement, and preserve safe compatibility behavior until a later removal.
decision: 2026-08-28 Deprecate compatibly — Mark the overload obsolete, document the tracked replacement, and preserve safe compatibility behavior until a later removal.
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs untracked RemoveTenantConfigurationAsync still delegates to tracked dispatch and may return keyed SafeMessage-null failures; ITenantCommandGateway.cs does not mark it obsolete. Existing compatible-deprecation decision remains pending.
verification-files: src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs; src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs
evidence-kind: recommendation
priority: P3
owner: unassigned
dependencies: none
next-action: Apply the recorded deprecation and compatibility guidance with a direct overload regression.
related: DW-319, DW-254

### DW-328: Follow-up review still recommended for 3-6-remove-configuration-key-with-complete-preview after the damping cap was spent
origin: review-budget-followup
location: n/a
source_spec: `spec-3-6-remove-configuration-key-with-complete-preview.md`
severity: low
reason: The follow-up-review damping cap (limits.max_followup_reviews = 1) was spent with the story finalized (status: done, verify green) while the review pass still recommended an independent follow-up. The work was committed by bmad-loop run 20260827-234608-260c; this entry preserves the lingering recommendation for a deliberate later review.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: _bmad-output/implementation-artifacts/spec-3-6-remove-configuration-key-with-complete-preview.md remains done, but the damping-cap record still calls for an independent follow-up; no artifact explicitly closes this review recommendation. This does not establish a product defect.
verification-files: _bmad-output/implementation-artifacts/spec-3-6-remove-configuration-key-with-complete-preview.md
evidence-kind: recommendation
priority: P3
owner: unassigned
dependencies: independent review capacity
next-action: Run a separately scoped independent follow-up on retained removal expiry and proof behavior.
related: DW-325, DW-326, DW-327

### DW-339: Builds `package-version-audit.json` still records EventStore `3.99.0` while the catalog pin is `3.100.0`.

status: done 2026-09-02
origin: migrated from legacy ledger (""), 2026-09-02
source_spec: `_bmad-output/implementation-artifacts/spec-eventstore-3-100-0.md`
archived: 2026-10-12

### DW-340: In-progress `spec-refresh-dependencies.md` can overwrite the new EventStore `3.100.0` planning facts.

status: done 2026-10-12
origin: migrated from legacy ledger (""), 2026-09-02
source_spec: `_bmad-output/implementation-artifacts/spec-eventstore-3-100-0.md`
priority: P2
archived: 2026-10-12

### DW-329: Global-administrator command retries do not retain a caller-owned message id across ambiguous transport failures.

status: done 2026-09-02
origin: spec-deferred 9acd809c21e3
source_spec: `spec-4-1-fixed-scope-global-administrator-action-availability.md`
severity: high
archived: 2026-10-12

### DW-330: Existing grant/remove projection confirmation does not require baseline projection-version advancement or command-specific audit provenance.

status: done 2026-09-02
origin: spec-deferred b69ce19c6d80
source_spec: `spec-4-1-fixed-scope-global-administrator-action-availability.md`
severity: high
archived: 2026-10-12

### DW-331: A contradictory complete-read authorization-scope flag is not revalidated by the pure removal evaluator.

status: done 2026-10-16
origin: spec-deferred b78a2689221d
source_spec: `spec-4-1-fixed-scope-global-administrator-action-availability.md`
severity: medium
priority: P2
archived: 2026-10-17

### DW-332: Repeated availability evaluation during one Razor render could theoretically produce mismatched reason and recovery associations.
origin: spec-deferred cb19669a12d0
location: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:821
source_spec: `spec-4-1-fixed-scope-global-administrator-action-availability.md`
reason: Grant and removal availability are recomputed independently for visible copy, disabled state, and aria-describedby. Blazor serializes renderer callbacks, but the admission gate's state can change before its notification callback is rendered; a deterministic test that changes gate or viewport evidence between those synchronous property reads is needed to establish whether an inconsistent render is reachable. The evaluation pattern predates this re-drive.
status: open
disposition: uncertain
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor still recomputes availability across rendered attributes; no deterministic gate/viewport transition between synchronous reads establishes mismatched copy or associations.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor
evidence-kind: unverified-risk
priority: P3
owner: unassigned
dependencies: none
next-action: Drive a controlled evidence transition during a render and capture the resulting associations before choosing a fix.
related: none

### DW-333: The concurrently introduced tenant-workspace tab migration lacks browser-level proof that inactive panels are not visible or focusable.
origin: spec-deferred 55356cb8b10a
location: tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs:420
source_spec: `spec-4-2-grant-global-administrator-with-projection-confirmation.md`
severity: medium
reason: The workspace-tab change arrived in a separate concurrent commit and is outside Story 4.2. Existing component assertions cover attributes, but an authenticated browser active-element and visibility trace would settle the remaining interaction risk.
status: open
disposition: blocked
verification-date: 2026-10-12
verification: tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs retained-tab assertions establish role/attributes, not computed inactive-panel visibility, focusability or accessibility-tree exclusion under the shipped Fluent runtime. Original commit attribution is preserved separately.
verification-files: tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: authenticated real-browser workspace fixture
next-action: Capture authenticated tab switches with computed visibility, activeElement and accessibility-tree evidence.
related: DW-333, DW-335, DW-357

### DW-334: The concurrently introduced Memories secret-store topology lacks a deployed Aspire model and health verification.
origin: spec-deferred dd7b16301f77
location: src/Hexalith.Tenants.AppHost/Program.cs:117
source_spec: `spec-4-2-grant-global-administrator-with-projection-confirmation.md`
severity: medium
reason: The AppHost secret-store change arrived in a separate concurrent commit and is outside Story 4.2. An Aspire resource-model inspection plus a healthy deployed startup using the intended secret provider would settle the topology risk.
status: open
disposition: blocked
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.AppHost/Program.cs supplies the Memories secret store and tests/Hexalith.Tenants.IntegrationTests/MemoriesSecretStoreResourceGraphTests.cs inspects its model; no current deployed-startup health packet using the intended provider settles the original full requirement.
verification-files: tests/Hexalith.Tenants.IntegrationTests/MemoriesSecretStoreResourceGraphTests.cs
evidence-kind: verified-gap
priority: P2
owner: operator
dependencies: operator-owned live Aspire/secret-provider evidence
next-action: Capture deployed model, provider binding and healthy startup in the intended package mode.
related: DW-344, DW-358

### DW-335: The tenant-workspace tab migration removed every inactive-panel visibility assertion inside this story's own commit, not a concurrent one.
origin: spec-deferred 36063b3ad145
location: tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs:426
source_spec: `spec-4-2-grant-global-administrator-with-projection-confirmation.md`
severity: medium
reason: Commit 8da765ad -- the same commit that carries the Story 4.2 grant work -- replaced the `hidden`/`aria-hidden` assertions on `#tenants-retained-panel`/`#users-retained-panel` with `role="tabpanel"` checks that hold for the active and inactive panel alike. Fluent UI v5 owns the panel flip client-side, so bUnit cannot observe it and no assertion anywhere distinguishes the two states. An authenticated browser trace showing the inactive panel is neither visible, focusable, nor exposed to assistive technology would settle it.
status: open
disposition: blocked
verification-date: 2026-10-12
verification: tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs retained-tab assertions establish role/attributes, not computed inactive-panel visibility, focusability or accessibility-tree exclusion under the shipped Fluent runtime. Original commit attribution is preserved separately.
verification-files: tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: authenticated real-browser workspace fixture
next-action: Capture authenticated tab switches with computed visibility, activeElement and accessibility-tree evidence.
related: DW-333, DW-335, DW-357
duplicate-of: DW-333

### DW-336: Focus containment, focus restoration, and viewport measurement are proven at the interop layer rather than in a real browser.
origin: spec-deferred 36c5b71ef493
location: tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs
source_spec: `spec-4-2-grant-global-administrator-with-projection-confirmation.md`
severity: medium
reason: The component tests assert which ElementReference the page asked the runtime to focus and drive the viewport by calling Observe on the observation singleton. Neither reaches document.activeElement, a real Tab cycle, `inert` semantics, or a real JS measurement, and there is no browser-driven lane in this repository. An authenticated browser trace over the grant preview -- open, Tab cycle, Escape, restore -- and a real viewport measurement would settle it.
status: open
disposition: blocked
verification-date: 2026-10-12
verification: tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs asserts interop focus ids and simulated viewport observations; browser harnesses exist now, but no authenticated rendered Blazor grant-preview Tab/Escape/inert and measurement packet closes this entry.
verification-files: tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: authenticated browser grant fixture
next-action: Run the real grant-preview keyboard/focus/viewport sequence and preserve its trace.
related: DW-345, DW-357, DW-359

### DW-337: Identical transport conditions are classified two different ways depending on which tracked command was dispatched.

status: done 2026-10-16
origin: spec-deferred 61e82d34cb29
source_spec: `spec-4-2-grant-global-administrator-with-projection-confirmation.md`
severity: low
priority: P2
archived: 2026-10-16

### DW-338: Nothing in this repository pins EventStore's command-status contract, which the grant lifecycle reasons over directly.

status: done 2026-09-05
origin: spec-deferred 0ab333618257
source_spec: `spec-4-2-grant-global-administrator-with-projection-confirmation.md`
severity: low
archived: 2026-10-12

### DW-341: `scripts/publish-partial-release.sh` reads `HEXALITH_RELEASE_PACKAGE_MANIFEST` into a `manifest` variable that no command ever consumes, so an operator override is silently ignored.
origin: spec-deferred 6dcaa0451f25
location: scripts/publish-partial-release.sh:6
source_spec: `spec-package-boundary-source.md`
severity: low
reason: `scripts/publish-partial-release.sh:6` assigns `manifest="${HEXALITH_RELEASE_PACKAGE_MANIFEST:-tools/release-packages.json}"` and no later line references `$manifest`; `grep -n manifest scripts/publish-partial-release.sh` returns line 6 only. The variable was already dead at `bbb0b11a` (the file is untouched by this change), and `scripts/pack-release-packages.py` accepts no manifest argument either, so threading the new `--manifest` flag alone would not make the override effective. Pre-existing, not caused by this story.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: scripts/publish-partial-release.sh sets manifest from HEXALITH_RELEASE_PACKAGE_MANIFEST but never consumes it; scripts/pack-release-packages.py fixes MANIFEST to tools/release-packages.json. Overrides are still silently ignored.
verification-files: scripts/publish-partial-release.sh; scripts/pack-release-packages.py
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: release recovery inventory contract
next-action: Use one validated manifest consistently through recovery validation, packing and publication.
related: none

### DW-342: Sprint tracking marks Story 4.3 in progress while Epic 4 remains done.

status: done 2026-09-06
origin: migrated from legacy ledger (""), 2026-09-05
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-refresh-dependencies-2.md`
archived: 2026-10-12

### DW-343: The dependency-refresh spec File List omits EventStore, FrontComposer, and Memories gitlinks plus Aspire topology test edits that sit in the same baseline range.

origin: migrated from legacy ledger (""), 2026-09-05
location: _bmad-output/implementation-artifacts/spec-refresh-dependencies-2.md; references/Hexalith.EventStore; references/Hexalith.FrontComposer; references/Hexalith.Memories; tests/Hexalith.Tenants.IntegrationTests/AspireTopologyTests.cs
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
reason: These files changed after Story 4.3's baseline in later dependency and topology work rather than in the removal command surface; a dedicated refresh-spec File List update would settle the ledger without changing removal behavior.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: _bmad-output/implementation-artifacts/spec-refresh-dependencies-2.md File List still lists only Builds, AppHost files, secret-store test and itself; AspireTopologyTests.cs is documented in tasks but absent from File List, and the later EventStore/FrontComposer/Memories gitlinks lack range attribution.
verification-files: _bmad-output/implementation-artifacts/spec-refresh-dependencies-2.md; tests/Hexalith.Tenants.IntegrationTests/AspireTopologyTests.cs
evidence-kind: verified-gap
priority: P3
owner: unassigned
dependencies: historical refresh commit range
next-action: Reconcile exact baseline/end attribution in the refresh artifact without reverting current pointers.
related: none

### DW-344: `MemoriesSecretStoreResourceGraphTests` does not pin package-mode secret-store properties, so a default source-reference run may not prove the NuGet graph.

origin: migrated from legacy ledger (""), 2026-09-05
location: tests/Hexalith.Tenants.IntegrationTests/MemoriesSecretStoreResourceGraphTests.cs
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
reason: The test was added by later dependency work in the baseline range and is outside Story 4.3's removal intent; it does not pin the package-mode properties needed to distinguish the NuGet resource graph from a default source-reference run.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: tests/Hexalith.Tenants.IntegrationTests/MemoriesSecretStoreResourceGraphTests.cs builds the AppHost model but does not assert evaluated package/source properties. Current root defaults prefer NuGet except UI, while an explicit source build can still run this same test without proving package mode.
verification-files: tests/Hexalith.Tenants.IntegrationTests/MemoriesSecretStoreResourceGraphTests.cs
evidence-kind: verified-gap
priority: P3
owner: unassigned
dependencies: AppHost build-mode fixture
next-action: Pin/record evaluated Memories package-mode properties in the model test lane.
related: DW-334

### DW-345: `focusElementById` may report a false failure when FluentButton focuses an inner native control instead of the host that owns the Cancel id.

origin: migrated from legacy ledger (""), 2026-09-05
location: src/Hexalith.Tenants.UI/wwwroot/js/tenantsFocus.js:23-35
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
reason: `tenantsFocus.js` returns `document.activeElement === target` after `target.focus()`. A real-browser trace of `document.activeElement` after focusing the rendered Cancel host would settle whether a successful inner-control focus is reported as false and wrap fallback incorrectly skips Cancel.
status: open
disposition: uncertain
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/wwwroot/js/tenantsFocus.js focusElementById still checks document.activeElement===target. The host/inner Fluent-control behavior is not settled by raw custom-element or bUnit interop tests; a successful real control trace is missing.
evidence-kind: unverified-risk
priority: P2
owner: unassigned
dependencies: authenticated rendered-control browser fixture
next-action: Trace document.activeElement/shadow activeElement after focusing the actual Blazor Fluent Cancel control.
related: DW-336, DW-362

### DW-346: Grant preview Cancel remains a raw lowercase fluent-button.

origin: code review chunk A 2026-09-06
location: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:633
source_spec: `_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
severity: low
reason: Story 4.3 required Fluent V5 actions on the removal dialog. The grant preview Cancel control is pre-existing KEEP grant chrome (`<fluent-button appearance="outline" @ref>`), not introduced by this removal pass.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor grant-preview Cancel remains a lowercase fluent-button with @onclick/@ref (around line 666); removal Cancel is already FluentButton.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor
evidence-kind: verified-defect
priority: P3
owner: unassigned
dependencies: grant chrome ownership
next-action: Migrate the grant Cancel to the existing Fluent V5 component and verify focus behavior.
related: none

### DW-347: Story 4.3 File List gitlink SHAs do not match the current tree.

status: done 2026-10-12
origin: code review chunk A 2026-09-06
source_spec: `_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
severity: medium
priority: P2
archived: 2026-10-12

### DW-348: Spec 5.1 remains in-review with its last operator-handoff task unchecked.

status: done 2026-09-24
origin: migrated from legacy ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-06)"), 2026-09-06
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
archived: 2026-10-12

### DW-349: Tenant audit filter validation can be written off the renderer and never painted.

origin: migrated from legacy ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-06)"), 2026-09-06
location: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
reason: TenantAuditPage.TryCreateRequest mutates _fromValidationKey, _toValidationKey, and _categoryValidationKey before InvokeAsync, including from notification LoadAsync continuations that already use ConfigureAwait(false), so validation state can be written off the renderer and never painted.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor LoadAsync still calls TryCreateRequest before InvokeAsync; that method mutates _fromValidationKey/_toValidationKey/_categoryValidationKey and may be reached after ConfigureAwait(false) notification continuations.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: none
next-action: Marshal request validation state onto the renderer and prove notification-triggered invalid fields repaint.
related: none

### DW-350: Audit list return URLs that contain a cursor are rejected and replaced with the tenant path.

status: done 2026-10-12
origin: migrated from legacy ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-06)"), 2026-09-06
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
priority: P2
archived: 2026-10-12

### DW-351: Tenant detail still matches audit/capability scope without the caller binding used by Story 5.1 reads.

status: done 2026-10-12
origin: migrated from legacy ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-06)"), 2026-09-06
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
priority: P2
archived: 2026-10-13

### DW-352: Audit unauthorized and unavailable recovery both navigate to the same BackHref.

status: done 2026-10-12
origin: migrated from legacy ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-06)"), 2026-09-06
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
priority: P2
archived: 2026-10-12

### DW-353: Audit timestamps that omit fractional seconds or use Z without seven digits are dropped from display.

origin: migrated from legacy ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-06)"), 2026-09-06
location: src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditNarrative.cs
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
reason: TenantAuditNarrative.SafeTimestamp accepts only the round-trip O format with seven fractional digits, so otherwise valid timestamps that omit fractional seconds or use Z without seven digits are dropped from display.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditNarrative.cs SafeTimestamp still accepts only O. Canonical server output uses O; acceptance of additional ISO 8601 input forms remains an enhancement rather than a demonstrated current transport defect.
verification-files: src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditNarrative.cs
evidence-kind: recommendation
priority: P3
owner: unassigned
dependencies: timestamp input contract
next-action: Specify accepted narrative timestamp forms and add missing-fraction/Z cases before expanding the parser.
related: none

### DW-354: A rejected audit EventId becomes an empty receipt/grid identity instead of failing closed.

status: done 2026-10-12
origin: migrated from legacy ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-06)"), 2026-09-06
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
priority: P2
archived: 2026-10-12

### DW-355: Numeric audit category text "0" is coerced to Access without a field error.

origin: migrated from legacy ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-06)"), 2026-09-06
location: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
reason: TenantAuditPage combines int.TryParse with Enum.IsDefined when parsing category text, so numeric enum value zero is accepted as Access instead of producing a field validation error.
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor TryCreateRequest now uses Enum.TryParse+Enum.IsDefined, which still admits numeric text 0 as Access; there is no named-value grammar check.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor
evidence-kind: verified-defect
priority: P3
owner: unassigned
dependencies: none
next-action: Require supported category names and assert numeric URL/form values produce the field error.
related: none

### DW-356: A reversed audit From/To range has no page test for field-associated Range validation.

status: done 2026-10-12
origin: migrated from legacy ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-06)"), 2026-09-06
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
priority: P3
archived: 2026-10-12

## Deferred from: code review of spec-4-2-grant-global-administrator-with-projection-confirmation.md (2026-09-06)

### DW-357: Focus containment remains interop-ID proof (`FocusedElementIds` / captured `ElementReference` ids), not `document.activeElement`, a real Tab/Shift+Tab cycle, or browser `inert`. Already DW-336; an authenticated browser trace of grant-preview open, Tab, Shift+Tab, Escape, and launcher restoration would settle it.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-2-grant-global-administrator-with-projection-confirmation.md (2026-09-06)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- Focus containment remains interop-ID proof (`FocusedElementIds` / captured `ElementReference` ids), not `document.activeElement`, a real Tab/Shift+Tab cycle, or browser `inert`. Already DW-336; an authenticated browser trace of grant-preview open, Tab, Shift+Tab, Escape, and launcher restoration would settle it."
status: open
disposition: uncertain
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor renders the grant modal/inert background and focus sentinels; component tests inspect interop targets. Browser fixtures in tests/Hexalith.Tenants.UI.Tests/Browser exercise shipped JS and constructed Fluent controls, but no retained authenticated grant-page trace proves a real Tab/Shift+Tab cycle, Escape, inert exclusion, and launcher restoration.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor
evidence-kind: unverified-risk
priority: P2
owner: unassigned
dependencies: none
next-action: Capture an authenticated grant-page browser trace covering open, Tab, Shift+Tab, Escape, activeElement, and inert background.
related: DW-336, DW-391
duplicate-of: DW-336


### DW-358: Package-reference Memories secret-store AC9 is outside this grant-core chunk. Already DW-334 / operator-owned; AppHost and the published `Hexalith.Memories.Aspire` pin belong to group 3 of this review, not this pass.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-2-grant-global-administrator-with-projection-confirmation.md (2026-09-06)"), 2026-10-12
location: n/a
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- Package-reference Memories secret-store AC9 is outside this grant-core chunk. Already DW-334 / operator-owned; AppHost and the published `Hexalith.Memories.Aspire` pin belong to group 3 of this review, not this pass."
status: open
disposition: blocked
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.AppHost/Program.cs:119-135 now passes the explicit memories-secretstore resource to the hosting recipe, and the source/package signature prerequisite is documented as available (2.25.0). The retained evidence does not establish a deployed package-mode Memories resource model and healthy startup using the intended secret provider; fixture health targets in tests/Hexalith.Tenants.IntegrationTests/Fixtures/AspireTopologyFixture.cs omit Memories.
evidence-kind: verified-gap
priority: P2
owner: operator
dependencies: Operator environment and intended secret-provider configuration
next-action: Operator: inspect the intended package-mode Aspire resource model and retain healthy Memories startup/provider evidence.
related: DW-334
duplicate-of: DW-334


## Deferred from: code review of spec-4-2-grant-global-administrator-with-projection-confirmation.md (2026-09-07)

Grant-core follow-up (`7e88a571..d0c534ff`, 14 files). No new DW ids; later-mainline gitlinks and same-turn focus race stay out of this chunk.

### DW-359: Focus containment remains interop-ID proof, including Escape from Fluent/FAST shadow DOM. Already DW-336; an authenticated browser trace of open, Tab, Shift+Tab, Escape, and launcher restoration would settle it.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-2-grant-global-administrator-with-projection-confirmation.md (2026-09-07)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- Focus containment remains interop-ID proof, including Escape from Fluent/FAST shadow DOM. Already DW-336; an authenticated browser trace of open, Tab, Shift+Tab, Escape, and launcher restoration would settle it."
status: open
disposition: uncertain
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor renders the grant modal/inert background and focus sentinels; component tests inspect interop targets. Browser fixtures in tests/Hexalith.Tenants.UI.Tests/Browser exercise shipped JS and constructed Fluent controls, but no retained authenticated grant-page trace proves a real Tab/Shift+Tab cycle, Escape, inert exclusion, and launcher restoration.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor
evidence-kind: unverified-risk
priority: P2
owner: unassigned
dependencies: none
next-action: Capture an authenticated grant-page browser trace covering open, Tab, Shift+Tab, Escape, activeElement, and inert background.
related: DW-336, DW-391
duplicate-of: DW-336


### DW-360: `validate-story-gitlinks.py` FAILs against HEAD because later mainline moved `references/` after grant completion `d0c534ff` (UNDECLARED `Hexalith.AI.Tools` / `Hexalith.Memories`; MISSTATED Builds/Commons/EventStore/FrontComposer vs the File List). Not caused by this 14-file chunk; belongs to group 3 / later `build(deps)`.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-2-grant-global-administrator-with-projection-confirmation.md (2026-09-07)"), 2026-10-12
location: scripts/validate-story-gitlinks.py; _bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- `validate-story-gitlinks.py` FAILs against HEAD because later mainline moved `references/` after grant completion `d0c534ff` (UNDECLARED `Hexalith.AI.Tools` / `Hexalith.Memories`; MISSTATED Builds/Commons/EventStore/FrontComposer vs the File List). Not caused by this 14-file chunk; belongs to group 3 / later `build(deps)`."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: Executed python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md on 2026-10-12: exit 1. Current undeclared McpCli/Platform and seven misstated target SHAs show the long legacy baseline-to-HEAD record still incorporates later dependency work.
verification-files: scripts/validate-story-gitlinks.py; _bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Story-owned commit attribution
next-action: Record the actual story-owned gitlink commits and historical endpoints without reverting later dependency updates.
related: DW-384


### DW-361: Opening preview while `_focusGrantLauncherPending` is still set can focus the inert launcher. Unverified medium: settle with a same-turn cancel-then-reopen test that leaves both focus flags set before `OnAfterRenderAsync`. [`src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1335`]

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-2-grant-global-administrator-with-projection-confirmation.md (2026-09-07)"), 2026-10-12
priority: P2
archived: 2026-10-12

## Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-06)

Chunk 1 (state / gateway / admission) restated two open items; no new ledger rows.

### DW-362: `focusElementById` may report a false failure when FluentButton focuses an inner native control instead of the host that owns the Cancel id. Already DW-345; a real-browser `document.activeElement` trace after focusing the rendered Cancel host would settle it.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-06)"), 2026-10-12
location: _bmad-output/implementation-artifacts/story-4-3-iteration-6-focus-browser-evidence-2026-09-22.md
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- `focusElementById` may report a false failure when FluentButton focuses an inner native control instead of the host that owns the Cancel id. Already DW-345; a real-browser `document.activeElement` trace after focusing the rendered Cancel host would settle it."
status: open
disposition: uncertain
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/wwwroot/js/tenantsFocus.js:39-53 still checks document.activeElement === target. story-4-3-iteration-6-focus-browser-evidence-2026-09-22.md and tenants-focus-browser-validation.html prove focus for a constructed fluent-button host; the page renders Blazor FluentButton for Cancel. Missing evidence is an activeElement/shadowRoot trace for that actual rendered Blazor host, not the raw-element fixture.
verification-files: _bmad-output/implementation-artifacts/story-4-3-iteration-6-focus-browser-evidence-2026-09-22.md
evidence-kind: unverified-risk
priority: P2
owner: unassigned
dependencies: none
next-action: Capture document.activeElement and shadow-root activeElement after focusing the actual rendered remove Cancel FluentButton.
related: DW-345, DW-391
duplicate-of: DW-345


### DW-363: Story 4.3 File List gitlink SHAs still do not match the current tree. Already DW-347; `validate-story-gitlinks.py` now reports Builds `6daad3d`, EventStore `9a20c05`, FrontComposer `f0c3b6f` versus File List `39debe9` / `7b7f876` / `0a4c4ad`. Updating the record edits this spec; restoring the tree reverts later `build(deps)` work.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-06)"), 2026-10-12
priority: P2
archived: 2026-10-12

### DW-364: Restore-access correction cannot retain an ambiguous UnableToVerify grant on the aggregate lease.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-06)"), 2026-10-12
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
priority: P2
archived: 2026-10-12

### DW-365: The pushall skill copies in the baseline range stage everything and commit with a build subject.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-06)"), 2026-10-12
location: _bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md; .agents/skills/pushall/SKILL.md
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`\n  summary: The pushall skill copies in the baseline range stage everything and commit with a build subject.\n  evidence: Identical .agents/.claude/.github pushall skills arrived with later tooling commits in this story's baseline range. They are not the last-administrator removal command; leaving them in this File List would mix unrelated work."
status: open
disposition: accepted-limitation
verification-date: 2026-10-12
verification: This original record is an out-of-scope tooling attribution note rather than a removal defect. spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md separately carries pushall findings, while .agents/skills/pushall/SKILL.md explicitly scopes whole-repository synchronization to a user invocation of /pushall. Actual procedure defects remain independently tracked.
verification-files: _bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md; .agents/skills/pushall/SKILL.md
evidence-kind: accepted-limitation
priority: P3
owner: unassigned
dependencies: none
next-action: Keep this scope note as history; use the separate pushall safeguards entries for maintenance.
related: DW-417, DW-418, DW-428, DW-429


## Deferred from: code review of spec-5-1-browse-tenant-audit-trail.md (2026-09-06)

### DW-366: Tenant detail still uses caller-free `MatchesScope(request)` for audit/capability evidence while Story 5.1 binds retained audit reads with `MatchesScope(request, callerScope)`. Already DW-351; TenantDetailPage is outside this story's File List.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-1-browse-tenant-audit-trail.md (2026-09-06)"), 2026-10-12
priority: P1
archived: 2026-10-13

### DW-367: `UserRemovedFromTenant` typed `PreviousRole`/`Role` cannot be produced because the event and audit projection emit only `userId`. Restore-access therefore cannot take an intended role from typed narrative; that is later correction-story/projection contract work, not browse-grid fail-closed.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-1-browse-tenant-audit-trail.md (2026-09-06)"), 2026-10-12
location: src/Hexalith.Tenants.Contracts/Events/UserRemovedFromTenant.cs; _bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md; src/Hexalith.Tenants.UI/State/TenantAudit/TenantCorrectionStartIntent.cs
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- `UserRemovedFromTenant` typed `PreviousRole`/`Role` cannot be produced because the event and audit projection emit only `userId`. Restore-access therefore cannot take an intended role from typed narrative; that is later correction-story/projection contract work, not browse-grid fail-closed."
status: open
disposition: accepted-limitation
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.Contracts/Events/UserRemovedFromTenant.cs still carries only TenantId/UserId, so historical role is unavailable. The completed spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md:21-27 expressly requires deliberate role selection and says never infer roles from history or add contract/history fields. TenantCorrectionStartIntent.cs uses context.IntendedRole. The current approved correction path intentionally avoids this missing historical field.
verification-files: src/Hexalith.Tenants.Contracts/Events/UserRemovedFromTenant.cs; _bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md; src/Hexalith.Tenants.UI/State/TenantAudit/TenantCorrectionStartIntent.cs
evidence-kind: accepted-limitation
priority: P3
owner: unassigned
dependencies: Explicit contract/product change approval
next-action: Keep deliberate role selection; revisit the event schema only through a separately approved contract change.
related: none


### DW-368: After projection refresh, `OpenCorrectionAsync` may assign a re-derived intent without re-checking `IsAvailable`. Unverified medium: settle by showing whether `CorrectionStartPanel` / `GlobalAdministratorCorrectionPanel` can submit when the parent intent is unavailable after refresh.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-1-browse-tenant-audit-trail.md (2026-09-06)"), 2026-10-12
priority: P2
archived: 2026-10-12

## Deferred from: code review of spec-4-2-grant-global-administrator-with-projection-confirmation.md (2026-09-13)

Verification pass over the uncommitted fix round that closes the 2026-09-07 patch list.

### DW-369: The `DispatchGrantAsync` redispatch-guard call site has no test. Filed by the verification-gap layer with a defer disposition: the window sits between an off-dispatcher `AreGrantDispatchCapabilitiesCurrent()` read and the on-dispatcher `livePrerequisites` re-read, with no injectable seam in the bUnit harness, and the deterministic sibling guard is now covered. Reverting the call to a bare `return;` fails nothing. [`src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:3316`]

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-2-grant-global-administrator-with-projection-confirmation.md (2026-09-13)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- The `DispatchGrantAsync` redispatch-guard call site has no test. Filed by the verification-gap layer with a defer disposition: the window sits between an off-dispatcher `AreGrantDispatchCapabilitiesCurrent()` read and the on-dispatcher `livePrerequisites` re-read, with no injectable seam in the bUnit harness, and the deterministic sibling guard is now covered. Reverting the call to a bare `return;` fails nothing. [`src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:3316`]"
status: open
disposition: actionable
verification-date: 2026-10-12
verification: GlobalAdministratorsPage.razor::DispatchGrantAsync still has a redispatch livePrerequisites refusal branch that calls SurfaceAmbiguousGrantRetryUnavailable. Existing AmbiguousGrantDeliveryRetryPrerequisiteRaceSurfacesRecovery and FinalRendererPrerequisiteLossReleasesExactUndispatchedLeaseAndRequiresFreshReview cover outer retry/initial-dispatch boundaries, not a deterministic capability flip at this inner redispatch branch.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: A controllable final-dispatch capability boundary
next-action: Add a deterministic dispatch-capability read seam or equivalent test that fails when this inner refusal silently returns.
related: DW-396


### DW-370: Grant status refresh lacks the in-flight gate the remove side has. Pre-existing, not touched by this change: `CanRefreshRemoveStatus` opens with `Volatile.Read(ref _removeSubmissionInFlight) == 0 && _removeAdmissionLease?.IsReconciliationDispatchInFlight != true`, while `CanRefreshGrantStatus` has no equivalent, so a second retry click during an in-flight redispatch re-enters and can divert the returning submission result. [`src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1124`]

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-2-grant-global-administrator-with-projection-confirmation.md (2026-09-13)"), 2026-10-12
priority: P2
archived: 2026-10-12

### DW-371: The sibling grant consumers did not adopt this round's pattern. Pre-existing and outside the grant-page file group: `GlobalAdministratorCorrectionPanel.RetryAmbiguousDeliveryAsync` still returns silently for the same grant command, and neither the correction panel nor the remove surface consumes a localization-specific unavailable reason although `Remove.Preview.Unavailable.Localization` already exists in both resx files and in `RequiredRemoveFactKeys`. [`src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor:1173`]

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-2-grant-global-administrator-with-projection-confirmation.md (2026-09-13)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- The sibling grant consumers did not adopt this round's pattern. Pre-existing and outside the grant-page file group: `GlobalAdministratorCorrectionPanel.RetryAmbiguousDeliveryAsync` still returns silently for the same grant command, and neither the correction panel nor the remove surface consumes a localization-specific unavailable reason although `Remove.Preview.Unavailable.Localization` already exists in both resx files and in `RequiredRemoveFactKeys`. [`src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor:1173`]"
status: open
disposition: actionable
verification-date: 2026-10-12
verification: GlobalAdministratorCorrectionPanel.razor::HasReconciliationSupport folds failed grant/remove preview localization readiness into MissingLifecycleSupport, and the page removal availability similarly consumes readiness without the grant-specific localization override. RetryAmbiguousDeliveryAsync now preserves busy/preflight diagnostics, partially resolving the silent-return concern, but localization failures still have no specific unavailable reason/recovery on these sibling surfaces.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: BFF readiness reason contract
next-action: Expose the localization-specific unavailable reason and recovery on removal and correction consumers, with missing-EN/FR regressions.
related: DW-385


## Deferred from: code review of spec-4-2-grant-global-administrator-with-projection-confirmation-2.md (2026-09-21)

### DW-372: Source-reference job may time out on a cold restore. Unverified medium: the new workflow has no NuGet cache, `timeout-minutes: 15`, and builds the full Debug source graph with `-m:1 -nr:false --no-incremental`. Settle by timing a cold CI run of that build plus the four-case execution; if it finishes under 15 minutes the timeout is adequate. [`.github/workflows/source-reference.yml:20`]

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-2-grant-global-administrator-with-projection-confirmation-2.md (2026-09-21)"), 2026-10-12
location: .github/workflows/source-reference.yml
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- Source-reference job may time out on a cold restore. Unverified medium: the new workflow has no NuGet cache, `timeout-minutes: 15`, and builds the full Debug source graph with `-m:1 -nr:false --no-incremental`. Settle by timing a cold CI run of that build plus the four-case execution; if it finishes under 15 minutes the timeout is adequate. [`.github/workflows/source-reference.yml:20`]"
status: open
disposition: uncertain
verification-date: 2026-10-12
verification: .github/workflows/source-reference.yml:20 still sets timeout-minutes: 15, has no NuGet-cache step, and builds the full Debug source-reference graph with -m:1 -nr:false --no-incremental. No retained timed cold CI run establishes that restore/build plus the four provenance cases fits that budget.
verification-files: .github/workflows/source-reference.yml
evidence-kind: unverified-risk
priority: P2
owner: unassigned
dependencies: none
next-action: Retain wall-clock timing for a genuinely cold CI build and four-case execution before changing cache or timeout.
related: DW-372, DW-382


## Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-22)

Chunk A (declared production source). No new DW ids; grant KEEP and later gitlink drift stay out of this removal patch list.

### DW-373: Restore-access correction still arms with `TryMarkDispatched` and cannot retain correlationless grant `RequestSent` the way removal now does. KEEP grant; this story tokenizes removal only. Already recorded as Story 4.2 restore-access lease territory. [`src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor:913`]

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-22)"), 2026-10-12
priority: P2
archived: 2026-10-12

### DW-374: Grant withdrawn-retry is a snapshot bit that cannot survive adoption. Grant KEEP / Story 4.2 chrome, not last-administrator removal. [`src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorGrantCommandSnapshot.cs:24`]

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-22)"), 2026-10-12
location: src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorGrantCommandSnapshot.cs; src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- Grant withdrawn-retry is a snapshot bit that cannot survive adoption. Grant KEEP / Story 4.2 chrome, not last-administrator removal. [`src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorGrantCommandSnapshot.cs:24`]"
status: open
disposition: uncertain
verification-date: 2026-10-12
verification: State/GlobalAdministrators/GlobalAdministratorGrantCommandSnapshot.cs still stores IsDeliveryRetryWithdrawn only in the page snapshot; CreateGrantSnapshot in GlobalAdministratorsPage.razor omits it during reconciliation adoption. OnAfterRenderAsync recomputes live withdrawal/rearm state, so a lost bit alone is not proof of incorrect recovery. Missing evidence is a replacement/adoption regression specifying whether a still-blocked retry retains its withdrawal reason and label.
verification-files: src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorGrantCommandSnapshot.cs; src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor
evidence-kind: unverified-risk
priority: P2
owner: unassigned
dependencies: none
next-action: Verify adoption of a withdrawn ambiguous grant under unchanged blocked prerequisites and assert the reason, label, and disabled state.
related: DW-370


### DW-375: Grant `EditForm OnSubmit` plus `fluent-button @onclick` can double-enter preview. Grant KEEP launcher, not the removal dialog. [`src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:241`]

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-22)"), 2026-10-12
priority: P2
archived: 2026-10-12

### DW-376: Grant completion apply has no snapshot fallback when the intent/message/preview `Equals` guard fails. Grant KEEP dispatch; removal already uses `CompleteRemoveSingleFlightAsync`. [`src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:3535`]

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-22)"), 2026-10-12
priority: P2
archived: 2026-10-12

### DW-377: `RequiredGrantFactKeys` grew to 89 keys and grant-preview gained `box-sizing: border-box` on this removal story. KEEP grant localization/chrome; belongs with Story 4.2. [`src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs:25`]

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-22)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor.css; _bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- `RequiredGrantFactKeys` grew to 89 keys and grant-preview gained `box-sizing: border-box` on this removal story. KEEP grant localization/chrome; belongs with Story 4.2. [`src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs:25`]"
status: open
disposition: accepted-limitation
verification-date: 2026-10-12
verification: The original entry is a KEEP grant-scope note. TenantsBffComposition.RequiredGrantFactKeys now contains 104 grant-surface keys; GlobalAdministratorsPage.razor.css retains the intentional grant-preview border-box rule. spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md explicitly rejects treating grant modal box-sizing as a removal defect and identifies the 4.2 commits.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor.css; _bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md
evidence-kind: accepted-limitation
priority: P3
owner: unassigned
dependencies: none
next-action: Retain the historical grant attribution; no removal fix is needed.
related: DW-402, DW-405


### DW-378: `focusElementById` may report a false failure when FluentButton focuses an inner native control instead of the Cancel host id. Already DW-345; a real-browser `document.activeElement` trace after host `.focus()` would settle it. [`src/Hexalith.Tenants.UI/wwwroot/js/tenantsFocus.js:23`]

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-22)"), 2026-10-12
location: _bmad-output/implementation-artifacts/story-4-3-iteration-6-focus-browser-evidence-2026-09-22.md
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- `focusElementById` may report a false failure when FluentButton focuses an inner native control instead of the Cancel host id. Already DW-345; a real-browser `document.activeElement` trace after host `.focus()` would settle it. [`src/Hexalith.Tenants.UI/wwwroot/js/tenantsFocus.js:23`]"
status: open
disposition: uncertain
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/wwwroot/js/tenantsFocus.js:39-53 still checks document.activeElement === target. story-4-3-iteration-6-focus-browser-evidence-2026-09-22.md and tenants-focus-browser-validation.html prove focus for a constructed fluent-button host; the page renders Blazor FluentButton for Cancel. Missing evidence is an activeElement/shadowRoot trace for that actual rendered Blazor host, not the raw-element fixture.
verification-files: _bmad-output/implementation-artifacts/story-4-3-iteration-6-focus-browser-evidence-2026-09-22.md
evidence-kind: unverified-risk
priority: P2
owner: unassigned
dependencies: none
next-action: Capture document.activeElement and shadow-root activeElement after focusing the actual rendered remove Cancel FluentButton.
related: DW-345, DW-391
duplicate-of: DW-345


### DW-379: Correction outer catch may overlay Ambiguous after Accepted delivery. Unverified medium: settle by showing `RefreshStatusCoreAsync` throwing after `TryCompleteReconciliationDispatch` already published Accepted. [`src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor:1082`]

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-22)"), 2026-10-12
priority: P2
archived: 2026-10-12

### DW-380: `validate-story-gitlinks.py` FAILs against HEAD: UNDECLARED Commons `6da79ae -> 9f4809d` and PolymorphicSerializations `8aeed1d -> 7e95556`; MISSTATED Builds/EventStore/FrontComposer/Memories vs File List `39debe9` / `7b7f876` / `0a4c4ad` / `f174f9c`. Already DW-347; updating the record edits this spec, restoring the tree reverts later `build(deps)` (Chunk C).

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-22)"), 2026-10-12
priority: P2
archived: 2026-10-12

## Deferred from: oneshot review of spec-4-2-grant-global-administrator-with-projection-confirmation-2.md (2026-09-22)

### DW-381: MTP skipped results may satisfy the source-reference four-test floor.

origin: migrated from unnumbered ledger ("Deferred from: oneshot review of spec-4-2-grant-global-administrator-with-projection-confirmation-2.md (2026-09-22)"), 2026-10-12
location: .github/workflows/source-reference.yml
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation-2.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation-2.md`\n  summary: MTP skipped results may satisfy the source-reference four-test floor.\n  evidence: maybe-false; prove it by running `--filter-method Hexalith.Tenants.IntegrationTests.TenantsApiGeneratedControllerTests.GlobalAdministratorsRealHandlerMetadataSurvivesRouterRestClientAndUiGateway` with one forced skip and `--minimum-expected-tests 4`. If that run still exits 0, add `--fail-skips`."
status: open
disposition: uncertain
verification-date: 2026-10-12
verification: .github/workflows/source-reference.yml still uses --minimum-expected-tests 4 without --fail-skips. PackageGovernanceTests::Source_reference_workflow_executes_real_handler_projection_provenance checks those command fragments only. No retained forced-skip run establishes whether the current pinned MTP/xUnit implementation counts skips toward this floor.
verification-files: .github/workflows/source-reference.yml
evidence-kind: unverified-risk
priority: P2
owner: unassigned
dependencies: none
next-action: Run an isolated forced-skip probe of one provenance case with the current four-case MTP command; record exit code and passed/skipped counts.
related: DW-381, DW-393


### DW-382: Source-reference job may time out on a cold restore.

origin: migrated from unnumbered ledger ("Deferred from: oneshot review of spec-4-2-grant-global-administrator-with-projection-confirmation-2.md (2026-09-22)"), 2026-10-12
location: .github/workflows/source-reference.yml
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation-2.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation-2.md`\n  summary: Source-reference job may time out on a cold restore.\n  evidence: maybe-false; settle by timing a cold CI run of `dotnet build tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -m:1 -nr:false --no-incremental` plus the four-case execution. If it finishes under 15 minutes, the timeout is adequate; if not, add the domain-ci NuGet cache and/or raise the timeout."
status: open
disposition: uncertain
verification-date: 2026-10-12
verification: .github/workflows/source-reference.yml:20 still sets timeout-minutes: 15, has no NuGet-cache step, and builds the full Debug source-reference graph with -m:1 -nr:false --no-incremental. No retained timed cold CI run establishes that restore/build plus the four provenance cases fits that budget.
verification-files: .github/workflows/source-reference.yml
evidence-kind: unverified-risk
priority: P2
owner: unassigned
dependencies: none
next-action: Retain wall-clock timing for a genuinely cold CI build and four-case execution before changing cache or timeout.
related: DW-372
duplicate-of: DW-372


## Deferred from: code review of spec-4-2-grant-global-administrator-with-projection-confirmation.md (2026-09-22)

### DW-383: `focusElementById` may treat a successful Fluent button focus as failure. maybe-false, high if true. Settle with a browser trace of `document.activeElement` after focusing the remove-cancel Fluent button host. [`src/Hexalith.Tenants.UI/wwwroot/js/tenantsFocus.js:34`]

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-2-grant-global-administrator-with-projection-confirmation.md (2026-09-22)"), 2026-10-12
location: _bmad-output/implementation-artifacts/story-4-3-iteration-6-focus-browser-evidence-2026-09-22.md
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- `focusElementById` may treat a successful Fluent button focus as failure. maybe-false, high if true. Settle with a browser trace of `document.activeElement` after focusing the remove-cancel Fluent button host. [`src/Hexalith.Tenants.UI/wwwroot/js/tenantsFocus.js:34`]"
status: open
disposition: uncertain
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/wwwroot/js/tenantsFocus.js:39-53 still checks document.activeElement === target. story-4-3-iteration-6-focus-browser-evidence-2026-09-22.md and tenants-focus-browser-validation.html prove focus for a constructed fluent-button host; the page renders Blazor FluentButton for Cancel. Missing evidence is an activeElement/shadowRoot trace for that actual rendered Blazor host, not the raw-element fixture.
verification-files: _bmad-output/implementation-artifacts/story-4-3-iteration-6-focus-browser-evidence-2026-09-22.md
evidence-kind: unverified-risk
priority: P1
owner: unassigned
dependencies: none
next-action: Capture document.activeElement and shadow-root activeElement after focusing the actual rendered remove Cancel FluentButton.
related: DW-345, DW-391
duplicate-of: DW-345


### DW-384: `validate-story-gitlinks.py` FAILs against HEAD. This grant-core chunk excluded gitlinks. UNDECLARED `references/Hexalith.AI.Tools de38f78 -> 5f93d2e` and `references/Hexalith.Memories d1b95ab -> 8884933`. MISSTATED Builds `2fba349` (story says `9d77ed7`), Commons `9f4809d` (`372d715`), EventStore `66cb4ed` (`e38c125`), FrontComposer `0276424` (`c6fe14c`), PolymorphicSerializations `7e95556` (`8aeed1d`). Declare or revert in the gitlink chunk. [`scripts/validate-story-gitlinks.py`]

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-2-grant-global-administrator-with-projection-confirmation.md (2026-09-22)"), 2026-10-12
location: n/a
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- `validate-story-gitlinks.py` FAILs against HEAD. This grant-core chunk excluded gitlinks. UNDECLARED `references/Hexalith.AI.Tools de38f78 -> 5f93d2e` and `references/Hexalith.Memories d1b95ab -> 8884933`. MISSTATED Builds `2fba349` (story says `9d77ed7`), Commons `9f4809d` (`372d715`), EventStore `66cb4ed` (`e38c125`), FrontComposer `0276424` (`c6fe14c`), PolymorphicSerializations `7e95556` (`8aeed1d`). Declare or revert in the gitlink chunk. [`scripts/validate-story-gitlinks.py`]"
status: open
disposition: actionable
verification-date: 2026-10-12
verification: Executed the Story 4.2 gitlink guard on 2026-10-12: exit 1. Its legacy baseline 7e88a57 is compared with HEAD 7a2a126; McpCli/Platform are undeclared and seven older File List targets are misstated after later dependency updates. The individual historical hashes changed, but the attribution problem remains.
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Story-owned gitlink attribution
next-action: Document actual story-owned pointer commits/endpoints, without copying current unrelated dependency bumps into grant scope.
related: DW-360
duplicate-of: DW-360


### DW-385: Add removal availability reason and recovery keys to the removal localization-readiness manifest.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-2-grant-global-administrator-with-projection-confirmation.md (2026-09-22)"), 2026-10-12
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md`
priority: P2
archived: 2026-10-12

### DW-386: Reuse the cached fixed removal localization result during removal preview composition.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-2-grant-global-administrator-with-projection-confirmation.md (2026-09-22)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md`\n  summary: Reuse the cached fixed removal localization result during removal preview composition.\n  evidence: `ComposeGlobalAdministratorRemovePreviewAsync` repeats the two-culture resource scan instead of using the existing per-circuit cache; this is pre-existing removal-only performance work."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: TenantsBffComposition.cs:290 caches _hasCompleteFixedRemoveLocalization for IsGlobalAdministratorRemovePreviewReady, but ComposeGlobalAdministratorRemovePreviewAsync:372 still calls HasCompleteLocalization(RequiredRemoveFactKeys) and scans both cultures again. This is a verified redundant scan, with no retained performance measurement showing user-visible impact.
verification-files: src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs
evidence-kind: recommendation
priority: P3
owner: unassigned
dependencies: Localization immutability/cache-lifetime contract
next-action: Assess whether the existing per-circuit fixed-localization cache can safely serve removal composition; change only if the measured cost warrants it.
related: none


### DW-387: Correct ambiguous removal retry preflight and its blocked-state copy.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-2-grant-global-administrator-with-projection-confirmation.md (2026-09-22)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md`\n  summary: Correct ambiguous removal retry preflight and its blocked-state copy.\n  evidence: A response-lost successful removal can make the target absent, causing normal preview validation to strand same-id redispatch; other mismatch branches can display affirmative availability copy as the failure reason."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: GlobalAdministratorCorrectionPanel.razor::RetryAmbiguousDeliveryAsync re-composes normal removal preview and requires the old preview and current population to match (:1501-1527). A response-lost successful removal makes the target absent and fails these checks, retaining RequestSent/UnableToVerify before a same-id retry can reach server idempotency. Some failure branches now use explicit evidence copy, but the retry-preflight availability problem remains.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: Same-id idempotency/recovery contract
next-action: Specify and test same-id removal recovery when the post-dispatch target is absent, preserving original ownership and complete last-administrator safeguards.
related: DW-373


### DW-388: Make removal projection requery cancellable across supersession and disposal.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-2-grant-global-administrator-with-projection-confirmation.md (2026-09-22)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md`\n  summary: Make removal projection requery cancellable across supersession and disposal.\n  evidence: `RequeryRemoveProjectionAsync` starts the bounded population walk with `CancellationToken.None`, so a replaced operation cannot stop its I/O."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: GlobalAdministratorsPage.razor::RequeryRemoveProjectionAsync:4809 still calls LoadCompletePopulationSafelyAsync(CancellationToken.None); supersession/disposal guards reject stale writes but cannot stop the already-running multi-page I/O.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: Removal operation cancellation lifetime
next-action: Pass an attempt-owned cancellation token through removal projection requery and verify supersession/disposal cancels the bounded walk.
related: none


### DW-389: Determine whether remove-dialog JS interop can surface cancellation exceptions during browser teardown.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-2-grant-global-administrator-with-projection-confirmation.md (2026-09-22)"), 2026-10-12
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md`
priority: P2
archived: 2026-10-12

### DW-390: Verify visual distinctness for accepted identities containing non-ASCII whitespace.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-2-grant-global-administrator-with-projection-confirmation.md (2026-09-22)"), 2026-10-12
location: n/a
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md`\n  summary: Verify visual distinctness for accepted identities containing non-ASCII whitespace.\n  evidence: This is maybe-false; compare accepted whitespace identities in supported browser/font combinations to determine whether literal visual rendering collides even though accessible names tokenize whitespace."
status: open
disposition: uncertain
verification-date: 2026-10-12
verification: GlobalAdministratorIdentityDisplay.Encode preserves literal whitespace while EncodeAccessible tokenizes it; GlobalAdministratorIdentityDisplayTests::Encode_PreservesLiteralWhitespaceAndDistinguishesFormatScalars and ::AccessibleEncodingTokenizesRepeatedAndEdgeWhitespaceWithoutChangingVisualEncoding confirm this intentional distinction. No retained supported-browser/font screenshot comparison proves whether distinct non-ASCII whitespace identities collide visually.
evidence-kind: unverified-risk
priority: P2
owner: unassigned
dependencies: none
next-action: Capture side-by-side supported-browser/font rendering of accepted whitespace identities and compare visible distinction with accessible names.
related: DW-391


### DW-391: Run authenticated browser evidence for modal focus, inert background, responsive layout, and long identity visibility.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-2-grant-global-administrator-with-projection-confirmation.md (2026-09-22)"), 2026-10-12
location: _bmad-output/implementation-artifacts/story-4-3-iteration-6-focus-browser-evidence-2026-09-22.md
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md`\n  summary: Run authenticated browser evidence for modal focus, inert background, responsive layout, and long identity visibility.\n  evidence: bUnit mocks DOM focus and does not compute CSS; a browser trace must cover both Tab directions, Escape restoration, short/mobile viewports, removal preview columns, and unclipped long identities."
status: open
disposition: uncertain
verification-date: 2026-10-12
verification: Browser fixtures and story-4-3-iteration-6-focus-browser-evidence-2026-09-22.md prove shipped JS focus on constructed Fluent controls. They do not host the authenticated grant/removal Blazor modal or exercise actual Tab input, inert background, computed short/mobile layout, preview column clipping, and long identity visibility together. Missing evidence is the complete authenticated page trace requested in the original finding.
verification-files: _bmad-output/implementation-artifacts/story-4-3-iteration-6-focus-browser-evidence-2026-09-22.md
evidence-kind: unverified-risk
priority: P2
owner: unassigned
dependencies: none
next-action: Capture authenticated grant and removal page keyboard/viewport/long-identity evidence at desktop, mobile, and short heights.
related: DW-336, DW-345, DW-357, DW-359, DW-390


### DW-392: Update the quickstart for generated local Keycloak credentials.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-2-grant-global-administrator-with-projection-confirmation.md (2026-09-22)"), 2026-10-12
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md`
priority: P3
archived: 2026-10-12

### DW-393: Prove whether skipped source-reference cases satisfy the minimum expected test floor.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-2-grant-global-administrator-with-projection-confirmation.md (2026-09-22)"), 2026-10-12
location: .github/workflows/source-reference.yml
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md`\n  summary: Prove whether skipped source-reference cases satisfy the minimum expected test floor.\n  evidence: This is maybe-false; force one of the four provenance cases to skip and run the current Microsoft.Testing.Platform command to determine whether `--fail-skips` is required."
status: open
disposition: uncertain
verification-date: 2026-10-12
verification: .github/workflows/source-reference.yml still uses --minimum-expected-tests 4 without --fail-skips. PackageGovernanceTests::Source_reference_workflow_executes_real_handler_projection_provenance checks those command fragments only. No retained forced-skip run establishes whether the current pinned MTP/xUnit implementation counts skips toward this floor.
verification-files: .github/workflows/source-reference.yml
evidence-kind: unverified-risk
priority: P2
owner: unassigned
dependencies: none
next-action: Run an isolated forced-skip probe of one provenance case with the current four-case MTP command; record exit code and passed/skipped counts.
related: DW-381
duplicate-of: DW-381


### DW-394: Pin build-only EventStore project-reference metadata in package governance tests.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-2-grant-global-administrator-with-projection-confirmation.md (2026-09-22)"), 2026-10-12
location: tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md`\n  summary: Pin build-only EventStore project-reference metadata in package governance tests.\n  evidence: Current tests verify edge presence and version isolation but not `Condition`, `ReferenceOutputAssembly`, `Private`, or `IsAspireProjectResource`, allowing dependency or Aspire-resource leakage to regress unnoticed."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: Contracts.Tests/PackageGovernanceTests.cs pins the four AppHost EventStore edges and Version isolation, and its generic No_EventStore_project_reference_is_reachable_in_package_mode now checks source conditions. No assertion pins ReferenceOutputAssembly, Private, or IsAspireProjectResource on the build-only host edges; the original metadata regression gap remains partially unresolved.
verification-files: tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Authoritative build-only metadata policy
next-action: Add governance coverage for all three build-only edge metadata values and make mutations of each fail.
related: DW-395


### DW-395: Validate source-reference workflow commands structurally rather than with independent substrings.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-2-grant-global-administrator-with-projection-confirmation.md (2026-09-22)"), 2026-10-12
location: tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md`\n  summary: Validate source-reference workflow commands structurally rather than with independent substrings.\n  evidence: The current governance test can pass when required flags move to comments or unrelated steps, configurations diverge, or the YAML structure is invalid."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: PackageGovernanceTests.cs::Source_reference_workflow_executes_real_handler_projection_provenance (:254-268) still uses independent workflow.ShouldContain substrings. It does not parse the build/test run steps or bind matching configuration/source-mode/test-floor flags to their actual commands, although structural helper methods now exist for release jobs elsewhere in this class.
verification-files: tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: YAML/command structural validation helper
next-action: Validate the actual source-reference YAML build/test step commands and bind every required flag to its command; cover comment/misplaced-flag mutations.
related: DW-394


### DW-396: Cover correction-panel grant and removal behavior when tracked dispatch capability is false.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-2-grant-global-administrator-with-projection-confirmation.md (2026-09-22)"), 2026-10-12
location: n/a
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md`\n  summary: Cover correction-panel grant and removal behavior when tracked dispatch capability is false.\n  evidence: The pure evaluator tests false capabilities, but correction-panel stubs always return true; panel wiring can regress without disabling high-impact correction actions."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: GlobalAdministratorCorrectionPanelTests.StubTenantCommandGateway now has settable SupportsTrackedGlobalAdministratorDispatch/RemoveDispatch properties, but no test sets either false. UnavailableQueryGatewayBlocksBothCorrectionActionsWithoutDispatch covers requery absence, not the dispatch-capability wiring named in this finding.
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: none
next-action: Render restore and revoke correction panels with their tracked-dispatch capability false and verify confirm/retry remain blocked and no dispatch occurs.
related: DW-369


### DW-397: Add real explicit-project-root tests for the BMAD customization resolver.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-2-grant-global-administrator-with-projection-confirmation.md (2026-09-22)"), 2026-10-12
location: n/a
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md`\n  summary: Add real explicit-project-root tests for the BMAD customization resolver.\n  evidence: Wrapper tests only inspect mocked argv; execute the resolver from a conflicting working directory and cover explicit-root precedence, fallback precedence, and warnings in the Python guard lane."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: _bmad/scripts/resolve_customization.py implements --project-root, candidate_project_roots precedence, and warn_on_masked_override. The tracked Python tests under tests/scripts only contain the gitlink regression lane; no direct resolver test executes conflicting cwd/explicit-root precedence and warnings.
evidence-kind: verified-gap
priority: P3
owner: unassigned
dependencies: Python 3.11+ and temporary fixture roots
next-action: Add isolated temporary-project resolver tests for explicit root, cwd/script/skill fallback ordering, and masked-override diagnostics.
related: none


### DW-398: Update the quickstart for generated local Keycloak credentials.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-2-grant-global-administrator-with-projection-confirmation.md (2026-09-22)"), 2026-10-12
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
priority: P3
archived: 2026-10-12

### DW-399: Reject blank and repeated continuation cursors in tenant-audit payload validation.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-2-grant-global-administrator-with-projection-confirmation.md (2026-09-22)"), 2026-10-12
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
priority: P2
archived: 2026-10-12

### DW-400: Close the global-administrator read-refresh subscription race across authorization collapse.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-2-grant-global-administrator-with-projection-confirmation.md (2026-09-22)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`\n  summary: Close the global-administrator read-refresh subscription race across authorization collapse.\n  evidence: Authorization can become false after the post-subscribe guard and before `_readRefreshLease` assignment; the sign-out path can then miss and leave that callback subscribed because the post-assignment guard only rechecks disposal."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: GlobalAdministratorsPage.razor::EnsureReadRefreshLeaseAsync checks _disposed/IsAuthorized before assignment (:2335), then assigns _readRefreshLease (:2352), and its post-assignment guard checks only _disposed (:2359). Authorization collapse between those points can still miss the assigned lease. The dispose-race test covers _disposed only.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: Captured authorization transition generation
next-action: Guard subscription assignment against authorization generation/collapse and add the same interleaving with sign-out rather than disposal.
related: DW-421


## Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-22)

Page and focus slice (`91d2335...c3a11fd0`). No new DW ids.

### DW-401: Story gitlink guard still fails against HEAD. Already DW-347. Current tree is Builds `2fba349`, Commons `9f4809d` undeclared, EventStore `db1e9d7`, FrontComposer `00d4da4`, Memories `8884933`, PolymorphicSerializations `7e95556` undeclared, while the File List still states Builds `39debe9`, EventStore `7b7f876`, FrontComposer `0a4c4ad`, and Memories `f174f9c`. This slice does not move `references/`. Declaring edits the spec; restoring the tree reverts later `build(deps)` work.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-22)"), 2026-10-12
priority: P2
archived: 2026-10-12

### DW-402: Grant preview `box-sizing: border-box` is in `.global-admins__grant-preview`. Already deferred as KEEP grant chrome for Story 4.2, not last-administrator removal.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-22)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor.css
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- Grant preview `box-sizing: border-box` is in `.global-admins__grant-preview`. Already deferred as KEEP grant chrome for Story 4.2, not last-administrator removal."
status: open
disposition: accepted-limitation
verification-date: 2026-10-12
verification: GlobalAdministratorsPage.razor.css retains .global-admins__grant-preview box-sizing: border-box. The removal spec review explicitly records that this is approved KEEP grant chrome from Story 4.2, not a removal change needing correction.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor.css
evidence-kind: accepted-limitation
priority: P3
owner: unassigned
dependencies: none
next-action: Preserve the scope attribution and style; no maintenance fix follows from this note.
related: DW-377


### DW-403: Validate the Release/NuGet package graph in CI rather than relying on the Debug source-reference lane.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-22)"), 2026-10-12
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
priority: P3
archived: 2026-10-12

## Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-22, Group A removal core)

Group A removal-core source slice (`91d2335..3a98efe`). Already-recorded items are listed without new DW ids.

### DW-404: Story gitlink guard still fails against HEAD (`scripts/validate-story-gitlinks.py` exit 1). Already DW-347.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-22, Group A removal core)"), 2026-10-12
priority: P2
archived: 2026-10-12

### DW-405: `RequiredGrantFactKeys` has no guard proving rendered grant keys are listed and present in EN/FR. Already deferred as KEEP grant localization for Story 4.2.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-22, Group A removal core)"), 2026-10-12
priority: P3
archived: 2026-10-12

### DW-406: Preserve specific grant rejection reasons instead of collapsing every code to the generic rejected key.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-22, Group A removal core)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`\n  summary: Preserve specific grant rejection reasons instead of collapsing every code to the generic rejected key.\n  evidence: `TenantCommandGateway.MapSetGlobalAdministratorGatewayException` maps `GlobalAdministratorAlreadyExists`, `InsufficientPermissions`, and other recognised codes to `Tenants.GlobalAdministrators.Grant.Submission.Rejected`, `GetStatusAsync` maps every rejected status to `Grant.Status.Rejected`, and the page never renders `RejectionCode`; introduced by Story 4.2 commit `cf0e420b`."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: TenantCommandGateway.cs::MapSetGlobalAdministratorGatewayException:904-916 still maps recognised rejection codes and HTTP permission failures to the generic Grant.Submission.Rejected key. GetStatusAsync uses Grant.Status.Rejected, and GlobalAdministratorsPage renders the safe key without RejectionCode-specific mapping. Removal retains differentiated reasons.
verification-files: src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: Approved rejection-code-to-copy map
next-action: Add support-safe EN/FR grant rejection reasons keyed by recognised platform-governance codes for submission and status, with page/gateway regressions.
related: none


### DW-407: Cover the tenant-audit caller-change guard on faulting, invalid-cursor retry, and not-modified refetch paths.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-22, Group A removal core)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`\n  summary: Cover the tenant-audit caller-change guard on faulting, invalid-cursor retry, and not-modified refetch paths.\n  evidence: Only `Get_tenant_audit_discards_an_awaited_response_when_the_caller_changes` switches the caller mid-call, and it completes successfully with no retained snapshot; removing the `AuditIdentityFailure` check in the exception catches would return the original caller's retained rows with every test green. Story 5.1 code (`281e3c3c`)."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: TenantQueryGatewayTests now includes Get_tenant_audit_discards_a_refetched_304_response_when_the_caller_changes_during_the_second_read and Get_tenant_audit_discards_retained_rows_when_the_caller_changes_during_an_unexpected_fault, closing two parts of the old gap. The invalid-cursor first-page retry catch at TenantQueryGateway.cs:1168-1175 still has no held-retry/caller-switch regression.
verification-files: src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Held invalid-cursor recovery response
next-action: Add a caller-switch test during invalid-cursor recovery whose retry faults, and verify no old caller rows can be retained.
related: none


### DW-408: Decide how the tenant-audit payload allowlist handles server event types it does not know.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-22, Group A removal core)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`\n  summary: Decide how the tenant-audit payload allowlist handles server event types it does not know.\n  evidence: `TenantQueryGateway.IsSupportedAuditEvent` hard-codes eleven event types, so one new server event type fails `IsValidTenantAuditPayload` for the whole page and signals `InvalidPayload`, the same signal as tampering. Story 5.1 code (`281e3c3c`)."
status: open
disposition: awaiting-decision
verification-date: 2026-10-12
verification: TenantQueryGateway.cs::IsSupportedAuditEvent:2825 retains an eleven-event/category allowlist and IsValidTenantAuditPayload rejects the entire page when an event is unknown. The server currently projects those supported tenant events; no new unsupported production event is established. A versioning/product decision is needed on fail-closed unknown-event handling versus safe forward-compatible placeholders.
verification-files: src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs
evidence-kind: recommendation
priority: P2
owner: unassigned
dependencies: Audit contract/versioning decision
next-action: Decide the unknown audit event compatibility contract, then update gateway behavior/tests and documentation to that single policy.
related: none


### DW-409: Verify grant completion is applied when the grant generation is superseded after the lease completion is published (unverified, would be medium).

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-22, Group A removal core)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`\n  summary: Verify grant completion is applied when the grant generation is superseded after the lease completion is published (unverified, would be medium).\n  evidence: `DispatchGrantAsync` syncs the published completion only while `CanApplyGrantMutation(generation)` holds, and the gate handler skips grant sync while `_isGrantSubmitting` is true; settle with a test that invalidates the generation between `TryCompleteReconciliationDispatch` and the renderer sync."
status: open
disposition: uncertain
verification-date: 2026-10-12
verification: GlobalAdministratorsPage.razor::DispatchGrantAsync still applies a published lease completion only when CanApplyGrantMutation(generation), while ApplyFixedAggregateAdmissionChangeAsync skips grant sync during _isGrantSubmitting. Superseded_accepted_completion_is_immediately_adopted_and_resumed exists, but its test gates I/O/rendering rather than proving the exact ordering of lease publication, skipped notification, and generation supersession. Missing evidence is that exact same-owner publication-to-sync interleaving.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor
evidence-kind: unverified-risk
priority: P2
owner: unassigned
dependencies: none
next-action: Add or run a deterministic test that invalidates grant generation after durable completion publication but before renderer sync, and assert UI plus lease completion.
related: DW-376, DW-421


## Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-22, Group A production File List)

Production File List slice (`91d2335...HEAD`, 12 files). Already-recorded items are listed without new DW ids.

### DW-410: Story gitlink guard still fails against HEAD (`scripts/validate-story-gitlinks.py` exit 1: Commons and PolymorphicSerializations undeclared; Builds/EventStore/FrontComposer/Memories SHAs misstated). Already DW-347.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-22, Group A production File List)"), 2026-10-12
priority: P2
archived: 2026-10-12

### DW-411: Grant delivery uses the removal lease-token API, correction restore still uses `TryMarkDispatched`, and `RequiredGrantFactKeys` expanded. KEEP grant / Story 4.2; already recorded on the earlier 2026-09-22 Group A pass and the iteration-6 restore-access deferral.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-22, Group A production File List)"), 2026-10-12
priority: P2
archived: 2026-10-12

### DW-412: `focusElementById` may report failure when focus lands on an inner Fluent control, and the Chromium harness mounts a raw `<fluent-button>` rather than the Blazor `FluentButton` host. Already DW-345; a real-browser `activeElement` trace of the rendered Cancel host would settle it.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-22, Group A production File List)"), 2026-10-12
location: _bmad-output/implementation-artifacts/story-4-3-iteration-6-focus-browser-evidence-2026-09-22.md
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- `focusElementById` may report failure when focus lands on an inner Fluent control, and the Chromium harness mounts a raw `<fluent-button>` rather than the Blazor `FluentButton` host. Already DW-345; a real-browser `activeElement` trace of the rendered Cancel host would settle it."
status: open
disposition: uncertain
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/wwwroot/js/tenantsFocus.js:39-53 still checks document.activeElement === target. story-4-3-iteration-6-focus-browser-evidence-2026-09-22.md and tenants-focus-browser-validation.html prove focus for a constructed fluent-button host; the page renders Blazor FluentButton for Cancel. Missing evidence is an activeElement/shadowRoot trace for that actual rendered Blazor host, not the raw-element fixture.
verification-files: _bmad-output/implementation-artifacts/story-4-3-iteration-6-focus-browser-evidence-2026-09-22.md
evidence-kind: unverified-risk
priority: P2
owner: unassigned
dependencies: none
next-action: Capture document.activeElement and shadow-root activeElement after focusing the actual rendered remove Cancel FluentButton.
related: DW-345, DW-391
duplicate-of: DW-345


### DW-413: Correction outer catch can overlay Ambiguous after Accepted if `RefreshStatusCoreAsync` throws. Maybe-false; settle by showing that throw after `TryCompleteReconciliationDispatch` already published Accepted. Already recorded on iteration 6.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-22, Group A production File List)"), 2026-10-12
priority: P2
archived: 2026-10-12

## Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-23, Group B1 page tests)

Page-test slice (`91d2335..0f490cb`, `GlobalAdministratorsPageTests.cs`). Already-recorded items are listed without new DW ids.

### DW-414: Make the removal rejection page test prove localization instead of echoing the gateway's raw text.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-23, Group B1 page tests)"), 2026-10-12
location: tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`\n  summary: Make the removal rejection page test prove localization instead of echoing the gateway's raw text.\n  evidence: `Remove_rejection_keeps_last_confirmed_rows_without_success_or_member_copy` (`GlobalAdministratorsPageTests.cs:2332`) passes `Rejected(expectedText, code)`, and the stub localizer text also contains `expectedText`. `RemoveSafeMessage` falls back to raw `SafeMessage` when the key is missing, so a broken `Remove.Status.Rejected.*` mapping still passes. Pre-existing; not changed in this range."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: GlobalAdministratorsPageTests.cs::Remove_rejection_keeps_last_confirmed_rows_without_success_or_member_copy:2681 still supplies TenantCommandSubmissionResult.Rejected(expectedText, rejectionCode) and asserts the same expectedText returned by the stub. RemoveSafeMessage falls back to raw safe text when no key exists. The test still does not distinguish correct localisation from raw gateway echo.
verification-files: tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: none
next-action: Use distinct raw gateway text and real/localizer-only expected EN/FR rejection copy, and make a broken rejection key mapping fail.
related: DW-406


### DW-415: Verify the removal requery mismatch branch cannot release a live same-attempt lease from a stale confirming basis (unverified, would be medium).

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-23, Group B1 page tests)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`\n  summary: Verify the removal requery mismatch branch cannot release a live same-attempt lease from a stale confirming basis (unverified, would be medium).\n  evidence: In `RequeryRemoveProjectionAsync` (`GlobalAdministratorsPage.razor:4765-4773`), if `_removeSnapshot` advanced to the same attempt's next state during the held load, the lease still matches, so `RetainOrReleaseRemoveCompletion` can call `TryReleaseTerminal` for a terminal stale-basis projection while the UI keeps a non-terminal snapshot. Settle with a page test that advances the same attempt during a held confirming requery and asserts the lease and UI state together."
status: open
disposition: uncertain
verification-date: 2026-10-12
verification: GlobalAdministratorsPage.razor::RequeryRemoveProjectionAsync still calls RetainOrReleaseRemoveCompletion(projectionSnapshot, projectionLease, snapshot) when its captured snapshot is no longer current. Same-lease advancement can therefore reach terminal-release handling from an old confirming basis. Existing superseded projection tests inspect replacement attempts, but no retained test proves this exact same-attempt/nonterminal UI plus terminal stale-basis release ordering.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor
evidence-kind: unverified-risk
priority: P2
owner: unassigned
dependencies: none
next-action: Hold a confirming removal projection read, advance the same attempt snapshot, release the read, and assert retained UI state and lease ownership together.
related: DW-388, DW-421


### DW-416: Story gitlink guard still fails (`scripts/validate-story-gitlinks.py` exit 1). Already DW-347.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-23, Group B1 page tests)"), 2026-10-12
priority: P2
archived: 2026-10-12

### DW-417: Stop the pushall per-repository procedure when its initial fetch fails.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-23, Group B1 page tests)"), 2026-10-12
location: .agents/skills/pushall/SKILL.md
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`\n  summary: Stop the pushall per-repository procedure when its initial fetch fails.\n  evidence: `.agents/skills/pushall/SKILL.md` step 1 runs `fetch --all --prune` but does not branch on failure; later merge and pruning steps can use stale remote-tracking refs."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: .agents/skills/pushall/SKILL.md per-repo step 1 runs fetch --all --prune with no failure branch; later merge/prune steps assume refreshed remote-tracking refs. The initial fetch prerequisite is still not enforced in the procedure.
verification-files: .agents/skills/pushall/SKILL.md
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: Future authorized pushall skill change
next-action: Add an explicit per-repository fetch-failure stop/skip path before merge, push, or pruning, and synchronize tracked skill copies.
related: DW-418, DW-428, DW-429


### DW-418: Handle pushall checkout failure before continuing merge and final-branch steps.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-23, Group B1 page tests)"), 2026-10-12
location: .agents/skills/pushall/SKILL.md
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`\n  summary: Handle pushall checkout failure before continuing merge and final-branch steps.\n  evidence: `.agents/skills/pushall/SKILL.md` step 4 checks out the chosen default without a failure path, while steps 5–11 assume the checkout succeeded."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: .agents/skills/pushall/SKILL.md step 4 checks out the chosen default branch without checking its result; steps 5-11 proceed and the final-branch guarantee can fail.
verification-files: .agents/skills/pushall/SKILL.md
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: Future authorized pushall skill change
next-action: Require successful checkout and verify the current branch before continuing merge/push/prune steps; retain per-repo failure outcome.
related: DW-417, DW-428, DW-429


### DW-419: Fail the legacy story gitlink guard when its baseline already contains story work.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-23, Group B1 page tests)"), 2026-10-12
location: scripts/validate-story-gitlinks.py
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`\n  summary: Fail the legacy story gitlink guard when its baseline already contains story work.\n  evidence: `scripts/validate-story-gitlinks.py` currently emits only a warning from `baseline_is_mid_story`; pointer moves before that baseline are excluded from the baseline-to-tree verdict. The current Story 4.3 baseline does not meet this condition."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: scripts/validate-story-gitlinks.py:406-410 still records baseline_is_mid_story as a warning only, then returns PASS when no in-range pointer violations are found. Story work contained before that baseline can remain outside its verdict.
verification-files: scripts/validate-story-gitlinks.py
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: Story baseline ownership policy
next-action: Fail the legacy guard when the baseline already contains this story work, or require an explicit verified true-start override with regression coverage.
related: DW-420, DW-437


### DW-420: Read unchanged legacy-mode gitlinks from the superproject index or tree when a submodule is uninitialized.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-23, Group B1 page tests)"), 2026-10-12
location: scripts/validate-story-gitlinks.py
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`\n  summary: Read unchanged legacy-mode gitlinks from the superproject index or tree when a submodule is uninitialized.\n  evidence: `current_pointer(path, None)` runs `git -C <path> rev-parse HEAD`; in an uninitialized submodule directory Git can resolve the superproject HEAD and report a false pointer value. Recorded-commit mode uses `git ls-tree` and is unaffected."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: scripts/validate-story-gitlinks.py::current_pointer(path, None):327-336 still uses git -C path rev-parse HEAD without verifying the repository root. In an uninitialized submodule directory this resolves the enclosing superproject. Recorded-ref mode correctly uses ls-tree.
verification-files: scripts/validate-story-gitlinks.py
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: none
next-action: Read unchanged legacy-mode gitlinks from the superproject index/tree and test an uninitialized declared submodule directory.
related: DW-419


### DW-421: Release a terminal grant lease adopted after ownerless delivery completion.

status: done 2026-10-13
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-23, Group B1 page tests)"), 2026-10-12
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
priority: P1
archived: 2026-10-13

### DW-422: Keep tenant-audit Continue read-only linked to a rendered grid heading when filter validation is active.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-23, Group B1 page tests)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`\n  summary: Keep tenant-audit Continue read-only linked to a rendered grid heading when filter validation is active.\n  evidence: `TenantAuditPage.CanContinueReadOnlyForState` checks rows and stale/degraded/list-refreshed state while `ShouldRenderRows` also requires no filter validation, so the recovery link can target a heading omitted from the DOM. The audit surface was changed by later Story 5.1 work in the baseline range."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: TenantAuditPage.razor::CanContinueReadOnlyForState:613 only checks retained rows and stale/degraded/list-refreshed kind, whereas ShouldRenderRows:554 also requires !HasFilterValidation. The recovery anchor at :180 can still target #tenant-audit-grid-heading when validation omits the grid.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: none
next-action: Gate Continue read-only on the actual rendered grid/heading condition and cover stale retained rows with invalid filter input.
related: DW-444


### DW-423: Sanitize URL-supplied user and command context before showing it on the tenant audit page.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-23, Group B1 page tests)"), 2026-10-12
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-1-browse-tenant-audit-trail.md`
priority: P2
archived: 2026-10-12

### DW-424: Reject malformed audit date and non-string category fields in direct server query payloads.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-23, Group B1 page tests)"), 2026-10-12
location: src/Hexalith.Tenants/Queries/Handlers/TenantQueryHandlerBase.cs
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-1-browse-tenant-audit-trail.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-1-browse-tenant-audit-trail.md`\n  summary: Reject malformed audit date and non-string category fields in direct server query payloads.\n  evidence: `TenantQueryHandlerBase.DeserializeAuditPayload` converts malformed `from`/`to` and non-string `category` to null filters; the method is unchanged from the Story 5.1 baseline, so a direct authorized query can broaden its tenant-scoped result."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants/Queries/Handlers/TenantQueryHandlerBase.cs::DeserializeAuditPayload:301-327 still converts invalid from/to through TryGetDateTimeOffset to null and only parses category when its JSON kind is String. Non-string category and malformed date fields can silently become missing filters and broaden an authorized direct read.
verification-files: src/Hexalith.Tenants/Queries/Handlers/TenantQueryHandlerBase.cs
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: none
next-action: Reject supplied-but-malformed date/category filter fields with InvalidEnvelope and add direct query-handler tests for each malformed JSON kind/date.
related: DW-408


### DW-425: Recheck audit load generation inside the queued tenant-detail projection write.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-23, Group B1 page tests)"), 2026-10-12
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-1-browse-tenant-audit-trail.md`
priority: P2
archived: 2026-10-12

### DW-426: Invalidate an in-flight correction open when the audit page changes tenant routes.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-23, Group B1 page tests)"), 2026-10-12
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-1-browse-tenant-audit-trail.md`
priority: P2
archived: 2026-10-12

### DW-427: Revoke or refresh an open correction intent when its audit evidence degrades.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-23, Group B1 page tests)"), 2026-10-12
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-1-browse-tenant-audit-trail.md`
priority: P2
archived: 2026-10-12

### DW-428: Keep dirty detached-HEAD submodule commits reachable from the branch that pushall pushes.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-23, Group B1 page tests)"), 2026-10-12
location: .agents/skills/pushall/SKILL.md
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-1-browse-tenant-audit-trail.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-1-browse-tenant-audit-trail.md`\n  summary: Keep dirty detached-HEAD submodule commits reachable from the branch that pushall pushes.\n  evidence: `.agents/skills/pushall/SKILL.md` commits before checking out the default branch, so a detached-HEAD commit can be left behind. This separate Git skill is unrelated to tenant audit."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: .agents/skills/pushall/SKILL.md commits a dirty tree in step 3, before step 4 checks out the default branch. It has no detached-HEAD guard or step merging that newly created detached commit into the branch pushed in step 8, so the local changes can be left behind.
verification-files: .agents/skills/pushall/SKILL.md
evidence-kind: verified-defect
priority: P1
owner: unassigned
dependencies: Future authorized pushall skill change
next-action: Require an attached intended branch or retain/merge the exact detached commit before default-branch push; add a procedure fixture for dirty detached HEAD.
related: DW-417, DW-418, DW-429


### DW-429: Make pushall honor the remote's actual default branch when both main and master exist.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-23, Group B1 page tests)"), 2026-10-12
location: .agents/skills/pushall/SKILL.md
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-1-browse-tenant-audit-trail.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-1-browse-tenant-audit-trail.md`\n  summary: Make pushall honor the remote's actual default branch when both main and master exist.\n  evidence: `.agents/skills/pushall/SKILL.md` prefers local `main` ahead of `origin/HEAD`, so it can merge and prune against the wrong default. This separate Git skill is unrelated to tenant audit."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: .agents/skills/pushall/SKILL.md step 2 still prefers local main, then master, and asks origin only when neither exists. This can choose a stale local branch instead of the actual remote default and drive merges/pruning against the wrong branch.
verification-files: .agents/skills/pushall/SKILL.md
evidence-kind: verified-defect
priority: P1
owner: unassigned
dependencies: Future authorized pushall skill change
next-action: Resolve the remote default first, validate its branch/ref, and handle missing or ambiguous remote-default evidence explicitly.
related: DW-417, DW-418, DW-428


### DW-430: Resolve whether the historical Story 5.1 performance guest used modified dependency source.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-23, Group B1 page tests)"), 2026-10-12
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-1-browse-tenant-audit-trail-2.md`
priority: P3
archived: 2026-10-12

## Deferred from: code review of spec-5-2-reach-scoped-audit-evidence-from-context.md (2026-09-26)

Story range `fc147e3e..f17027ac`.

### DW-431: Story 5.2 gitlink guard fails against HEAD because later commits bump submodules.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-2-reach-scoped-audit-evidence-from-context.md (2026-09-26)"), 2026-10-12
location: scripts/validate-story-gitlinks.py; _bmad-output/implementation-artifacts/spec-5-2-reach-scoped-audit-evidence-from-context.md
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-2-reach-scoped-audit-evidence-from-context.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-2-reach-scoped-audit-evidence-from-context.md`\n  summary: Story 5.2 gitlink guard fails against HEAD because later commits bump submodules.\n  evidence: `scripts/validate-story-gitlinks.py` exits 1 for undeclared Builds `754d2b4→2326f98`, EventStore `04682ea→4fafcb9`, and FrontComposer `2e33757→07bfc22`. The bumps come from `8431d992` (Story 5.1 closure), `04f68c60`, and `dfa78e10`. `--ref f17027ac` passes, with no `references/` change in the story range."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: Executed python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-5-2-reach-scoped-audit-evidence-from-context.md on 2026-10-12: exit 1. Legacy baseline fc147e3-to-HEAD includes later AI.Tools/McpCli/Memories/Platform/PolymorphicSerializations pointer changes not declared by the story; historical three-pointer list changed but drift still applies.
verification-files: scripts/validate-story-gitlinks.py; _bmad-output/implementation-artifacts/spec-5-2-reach-scoped-audit-evidence-from-context.md
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Story-owned pointer attribution
next-action: Record precise story-owned pointer commits/endpoints and keep later dependency moves out of historical story scope.
related: DW-437


### DW-432: Run the hosted Story 5.2 route smoke once the Aspire EventStore fixture is healthy.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-2-reach-scoped-audit-evidence-from-context.md (2026-09-26)"), 2026-10-12
location: n/a
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-2-reach-scoped-audit-evidence-from-context.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-2-reach-scoped-audit-evidence-from-context.md`\n  summary: Run the hosted Story 5.2 route smoke once the Aspire EventStore fixture is healthy.\n  evidence: `TenantsUiRouteSmokeTests` audit-context assertions (`:110-111`) only compiled. In every review pass, fixture setup timed out on EventStore `/alive` before any assertion ran."
status: open
disposition: blocked
verification-date: 2026-10-12
verification: TenantsUiRouteSmokeTests now asserts anonymous FrontComposer fc-scope-blocked states and no protected page content; the old hosted audit-context assertions were replaced rather than demonstrated. No authenticated hosted route fixture currently proves the Story 5.2 contextual links/returns. A healthy anonymous startup alone cannot settle this originally requested protected-route evidence.
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: DW-436 authenticated caller/scope fixture and healthy Aspire topology
next-action: Establish the authenticated route fixture from DW-436, then retain the hosted audit-context route assertions and results.
related: DW-436


### DW-433: Verify focus restoration does not report a false missing origin when the page replaces its own URL mid-wait (unverified, would be medium).

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-2-reach-scoped-audit-evidence-from-context.md (2026-09-26)"), 2026-10-12
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-2-reach-scoped-audit-evidence-from-context.md`
priority: P2
archived: 2026-10-12

### DW-434: Restore in-flight detail command panels after an audit round trip by expanding their accordion item (Option C follow-up).

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-2-reach-scoped-audit-evidence-from-context.md (2026-09-26)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor; _bmad-output/implementation-artifacts/spec-5-2-reach-scoped-audit-evidence-from-context.md
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-2-reach-scoped-audit-evidence-from-context.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-2-reach-scoped-audit-evidence-from-context.md`\n  summary: Restore in-flight detail command panels after an audit round trip by expanding their accordion item (Option C follow-up).\n  evidence: Decision 2026-09-26 (Option A) accepted heading focus plus notice for detail command-flow and cursor returns. `TenantLifecycleCommandFlow`, `SetTenantConfigurationFlow`, and `RemoveTenantConfigurationFlow` already re-adopt a `RetainsAttempt` (RequestSent/Accepted) snapshot from their circuit `*AttemptTracker` on re-init. Their `FluentAccordionItem` in `TenantDetailPage.razor` may be collapsed on return, so the launcher cannot take focus. Expand the matching item when `auditFocus` names that flow, as `CreateTenantFlow` does, then browser-verify. Circuit-local state for terminal results, the member and metadata flows, and the list cursor was rejected as over-engineering, and it conflicts with the paging no-silent-reactivation invariant."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: TenantDetailPage.razor lifecycle/configuration accordion items currently start Expanded=true, but are not specifically expanded/re-adopted from auditFocus for a previously collapsed flow. spec-5-2-reach-scoped-audit-evidence-from-context.md:712 preserves approved Option A heading-plus-notice and the optional Option C in-flight restoration follow-up; Option B circuit state/cursor persistence was rejected. Exact flow-accordion browser restoration remains unproved.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor; _bmad-output/implementation-artifacts/spec-5-2-reach-scoped-audit-evidence-from-context.md
evidence-kind: recommendation
priority: P3
owner: unassigned
dependencies: Approved Option A fallback remains valid; preserve rejected Option B
next-action: For the approved Option C follow-up, browser-check a collapsed in-flight lifecycle/configuration audit round trip and expand its matching item only if the launcher is unreachable.
related: none


### DW-435: Invalidate retained tenant audit rows and an open receipt when the caller changes in the same UI circuit.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-2-reach-scoped-audit-evidence-from-context.md (2026-09-26)"), 2026-10-12
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-2-reach-scoped-audit-evidence-from-context.md`
priority: P2
archived: 2026-10-12

### DW-436: Replace the historical unauthenticated hosted route smoke with an authenticated FrontComposer route fixture.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-2-reach-scoped-audit-evidence-from-context.md (2026-09-26)"), 2026-10-12
location: tests/Hexalith.Tenants.IntegrationTests/TenantsUiRouteSmokeTests.cs
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-2-reach-scoped-audit-evidence-from-context.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-2-reach-scoped-audit-evidence-from-context.md`\n  summary: Replace the historical unauthenticated hosted route smoke with an authenticated FrontComposer route fixture.\n  evidence: With the repository development JWT issuer, audience, and key supplied temporarily, Aspire EventStore starts and all six `TenantsUiRouteSmokeTests` reach their assertions, but each fails because the unauthenticated shell prerenders `data-testid=\"fc-scope-blocked\"` instead of the expected page marker. Without those temporary settings, the fixture times out at EventStore `/alive` before assertions. The route tests need a valid caller and scope to inspect the pages."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: IntegrationTests/TenantsUiRouteSmokeTests.cs now explicitly documents and tests anonymous routes behind the FrontComposer scope boundary (six *BlocksAnonymousScope cases). This resolves the old incorrect anonymous expected-page markers, but no authenticated FrontComposer caller/scope route fixture is present, so the requested protected-page contextual route coverage remains absent.
verification-files: tests/Hexalith.Tenants.IntegrationTests/TenantsUiRouteSmokeTests.cs
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Validated caller/tenant scope and healthy Aspire topology
next-action: Add an authenticated FrontComposer route fixture and preserve the useful anonymous fail-closed tests as a separate lane.
related: DW-432


## Deferred from: code review of spec-5-2-reach-scoped-audit-evidence-from-context.md (2026-09-27)

Story range `fc147e3e..HEAD`.

### DW-437: Un-defer the CI story gitlink gate so `validate-story-gitlinks.py` runs against changed story files, not only its own regression suite.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-2-reach-scoped-audit-evidence-from-context.md (2026-09-27)"), 2026-10-12
location: .github/workflows/story-guards.yml; tests/scripts/test_validate_story_gitlinks.py; scripts/validate-story-gitlinks.py
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-2-reach-scoped-audit-evidence-from-context.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-2-reach-scoped-audit-evidence-from-context.md`\n  summary: Un-defer the CI story gitlink gate so `validate-story-gitlinks.py` runs against changed story files, not only its own regression suite.\n  evidence: Decision 2026-09-27 (\"Declare + enable CI gate\"). Story 5.2 is the fifth undeclared-bump occurrence; story commit `3ba48896` moved EventStore and FrontComposer without saying so. The fourth occurrence (Story 3.4, 2026-08-25) was the pre-agreed trigger. `.github/workflows/story-guards.yml` currently runs only `tests/scripts/test_validate_story_gitlinks.py`. The gate must compare each story's `baseline_commit..HEAD` range, because bumps often land in a later `build(deps)` commit."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: .github/workflows/story-guards.yml::gitlink-guard still runs only python3 tests/scripts/test_validate_story_gitlinks.py. It never invokes scripts/validate-story-gitlinks.py on changed stories. Current Story 5.2 and 5.4 guards still exit 1. The 2026-09-27 Declare + enable CI gate decision is already approved and needs implementation, not a new policy question.
verification-files: .github/workflows/story-guards.yml; tests/scripts/test_validate_story_gitlinks.py; scripts/validate-story-gitlinks.py
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Story attribution must avoid including unrelated later pointer updates
next-action: Enable a changed-story CI validation step with full history and deliberate story baseline/owned-commit handling.
related: DW-431, DW-451, DW-419, DW-420


## Deferred from: code review of spec-5-3-view-a-support-safe-audit-evidence-receipt.md (2026-09-27)

Story range `33d6dcca..7ac52e8d` (rebased to `051f9f30..f8223524`).

### DW-438: Revisit strict opaque audit reference classification for incidental secret-shaped substrings.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-3-view-a-support-safe-audit-evidence-receipt.md (2026-09-27)"), 2026-10-12
location: src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditSupportSafety.cs
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-3-view-a-support-safe-audit-evidence-receipt.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-3-view-a-support-safe-audit-evidence-receipt.md`\n  summary: Revisit strict opaque audit reference classification for incidental secret-shaped substrings.\n  evidence: The pre-existing gateway and reference classifier reject an otherwise valid opaque event ID when it happens to contain `jwt` or `eyj`; this story did not introduce that policy."
status: open
disposition: awaiting-decision
verification-date: 2026-10-12
verification: TenantAuditSupportSafety.cs::StrictUnsafeFragments still includes jwt/eyj, and IsSafe compares normalized secret fragments as substrings for ApprovedReference. An opaque ID containing those fragments is rejected. This is conservative established support-safety policy; no actual legitimate generated reference rejected in production is established.
verification-files: src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditSupportSafety.cs
evidence-kind: recommendation
priority: P3
owner: unassigned
dependencies: Support-safety policy/opaque-reference contract decision
next-action: Decide whether opaque references need a shape-based exemption from substring rejection and define examples before changing the shared safety policy.
related: DW-441


## Deferred from: code review of spec-5-3-view-a-support-safe-audit-evidence-receipt.md (2026-09-28)

Story range `33d6dcca..7ac52e8d` (rebased to `051f9f30..f8223524`).

### DW-439: Add accents to the remaining unaccented French receipt strings and make apostrophes consistent.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-3-view-a-support-safe-audit-evidence-receipt.md (2026-09-28)"), 2026-10-12
location: n/a
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-3-view-a-support-safe-audit-evidence-receipt.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-3-view-a-support-safe-audit-evidence-receipt.md`\n  summary: Add accents to the remaining unaccented French receipt strings and make apostrophes consistent.\n  evidence: `TenantsResources.fr.resx` still carries unaccented receipt copy from 2026-06-06 (`La preuve d'audit est prete a citer.`, `perimee`, `reessayez`, `Reference de commande`). Story 5.3 accented only the strings it rewrote, so the file now mixes `’` with ASCII `'`."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx still contains Reference de commande (:3284), La preuve d'audit est prete a citer (:3332), perimee (:3335), and reessayez (:3341), beside newer accented/curly-apostrophe strings. The localisation inconsistency remains visible in receipt copy.
evidence-kind: verified-defect
priority: P3
owner: unassigned
dependencies: French editorial convention
next-action: Correct the remaining French receipt accents and adopt the established apostrophe convention, checking EN/FR key parity.
related: none


### DW-440: Decide whether the absolute audit timestamp should be culture-formatted, as the epic's "culture-aware" wording suggests.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-3-view-a-support-safe-audit-evidence-receipt.md (2026-09-28)"), 2026-10-12
location: src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditReceipt.cs
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-3-view-a-support-safe-audit-evidence-receipt.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-3-view-a-support-safe-audit-evidence-receipt.md`\n  summary: Decide whether the absolute audit timestamp should be culture-formatted, as the epic's \"culture-aware\" wording suggests.\n  evidence: `TenantAuditReceipt.TimestampLabel` and the grid both use the fixed `yyyy-MM-dd HH:mm:ss 'UTC'` pattern from the Story 5.1 UTC correction; the French receipt renders the same string."
status: open
disposition: awaiting-decision
verification-date: 2026-10-12
verification: State/TenantAudit/TenantAuditReceipt.cs::TimestampLabel and AuditDataGrid use ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.CurrentCulture). The timezone is honest UTC, but this fixed numeric pattern retains the same arrangement under French culture. The requested culture-aware date arrangement versus stable support timestamp policy remains unspecified.
verification-files: src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditReceipt.cs
evidence-kind: recommendation
priority: P3
owner: unassigned
dependencies: UX/support timestamp formatting decision
next-action: Choose culture-formatted absolute dates or the stable UTC pattern for both grid and receipt, then align the UX wording/tests.
related: DW-450


### DW-441: Move typed audit identifiers from a deny-list to an allow-listed character set with a confusable-skeleton check.

status: done 2026-10-13
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-3-view-a-support-safe-audit-evidence-receipt.md (2026-09-28)"), 2026-10-12
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-3-view-a-support-safe-audit-evidence-receipt.md`
priority: P1
archived: 2026-10-14

### DW-442: Bind an in-flight correction open to the tenant route that initiated it.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-3-view-a-support-safe-audit-evidence-receipt.md (2026-09-28)"), 2026-10-12
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-3-view-a-support-safe-audit-evidence-receipt.md`
priority: P2
archived: 2026-10-12

### DW-443: Restore visible focus when a viewport downgrade removes an active correction panel.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-3-view-a-support-safe-audit-evidence-receipt.md (2026-09-28)"), 2026-10-12
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-3-view-a-support-safe-audit-evidence-receipt.md`
priority: P2
archived: 2026-10-12

## Deferred from: code review of spec-5-3-view-a-support-safe-audit-evidence-receipt.md (2026-09-29)

Re-review range `f8223524..38da46a6`.

### DW-444: Return focus when `ClearPaging()` or a viewport downgrade removes an active correction panel.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-3-view-a-support-safe-audit-evidence-receipt.md (2026-09-29)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-3-view-a-support-safe-audit-evidence-receipt.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-3-view-a-support-safe-audit-evidence-receipt.md`\n  summary: Return focus when `ClearPaging()` or a viewport downgrade removes an active correction panel.\n  evidence: On a `ListRefreshed` or `InvalidCursor` result, `LoadAsync` runs `CaptureCorrectionAuthority` (which keeps a still-Ready source) and then `ClearPaging()`. `ClearPaging()` nulls `_activeCorrectionIntent` and `_activeCorrectionFocusReference` without setting `_pendingCorrectionFocusReference`, so focus falls to `<body>` (`TenantAuditPage.razor:914-918`, `:1379-1388`). `RenderViewportChangeAsync` has the same gap, already recorded in the 2026-09-28 viewport entry. Both predate baseline `33d6dcca`. Each needs the pending-focus assignment that `CaptureCorrectionAuthority` now uses, gated on focus actually being lost."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: Viewport removal now schedules origin-specific focus (DW-443 resolved), but TenantAuditPage.razor::ClearPaging:1597-1609 still clears _activeCorrectionIntent/_previewCorrectionIntent and calls InvalidateCorrectionFocus without scheduling a launcher/heading return. On a current-ready ListRefreshed/InvalidCursor recovery CaptureCorrectionAuthority can keep the source immediately before ClearPaging removes the panel, leaving the paging-removal focus gap.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: Focus only when the removed panel actually owned focus
next-action: Schedule origin-aware focus before ClearPaging removes a still-active unsubmitted correction, while preserving focus on any surviving control; cover ListRefreshed recovery.
related: DW-443, DW-445, DW-422


### DW-445: Return correction focus to the control that launched the panel, not the first matching reference in document order.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-3-view-a-support-safe-audit-evidence-receipt.md (2026-09-29)"), 2026-10-12
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-3-view-a-support-safe-audit-evidence-receipt.md`
priority: P2
archived: 2026-10-12

## Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery.md (2026-09-29)

Review range `55f3dc63..working tree`.

### DW-446: Confirm that a screen reader announces the first audit availability state when the shared control mounts.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery.md (2026-09-29)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor; src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditAvailabilityState.razor
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`\n  summary: Confirm that a screen reader announces the first audit availability state when the shared control mounts.\n  evidence: Unverified (maybe-false); medium if true. The flows mount `AuditAvailabilityState` on the NotStarted → Pending/Unavailable transition, so its `aria-live` container is inserted together with its first content, the same pattern as the baseline control. Some screen readers skip content inserted with a new live region. The flows keep their own lifecycle live regions. Settle with an NVDA and browser check of a Create command reaching events-stored; if it is silent, keep the live container mounted empty while the state is NotStarted."
status: open
disposition: uncertain
verification-date: 2026-10-12
verification: CreateTenantFlow.razor mounts AuditAvailabilityState only when AuditState != NotStarted, and AuditAvailabilityState.razor inserts its aria-live container with initial content. Flows still retain separate lifecycle live regions, so static markup cannot prove the first availability message is missed. Missing evidence is an NVDA/supported-browser announcement trace at NotStarted-to-Pending/Unavailable.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor; src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditAvailabilityState.razor
evidence-kind: unverified-risk
priority: P2
owner: unassigned
dependencies: none
next-action: Run NVDA with a supported browser through the first events-stored availability transition and record whether its first message is announced.
related: none


### DW-447: Stop flow-level lifecycle recovery copy from naming recovery actions that the shared availability control may not render.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery.md (2026-09-29)"), 2026-10-12
location: n/a
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`\n  summary: Stop flow-level lifecycle recovery copy from naming recovery actions that the shared availability control may not render.\n  evidence: Strings such as `Tenants.EditMetadata.Recovery.Degraded`, `Tenants.Configuration.Set.Recovery.*`, `Tenants.Configuration.Remove.Recovery.*` and `Tenants.RemoveMember.Recovery.*` read \"Wait, retry status lookup, inspect audit when available, or escalate.\" They render beside the control (for example `EditTenantMetadataFlow.razor:153`) whether or not Escalate, Inspect audit or Continue read-only exist. This flow lifecycle copy predates Story 5.4, which removed the same promise from the shared control."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: TenantsResources.resx still tells Configuration.Set/Remove and EditMetadata degraded states to wait, retry status lookup, inspect audit when available, or escalate (:1487, :1714, :2644). Their flow-level RecoveryText renders independently of AuditAvailabilityState action callbacks/retry budgets. The shared availability control now filters actions, so this adjacent copy can promise an absent action.
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: Current per-flow callback/recovery availability
next-action: Make flow-level recovery text describe only currently rendered actions, with no-callback and exhausted-retry component coverage in EN/FR.
related: DW-448, DW-449


## Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery.md (2026-09-29, review 2)

Review range `55f3dc63..1cdcc0a9`.

### DW-448: Stop an unmatched RemoveMember proof walk from leaving the audit state pending indefinitely.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery.md (2026-09-29, review 2)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor; src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`\n  summary: Stop an unmatched RemoveMember proof walk from leaving the audit state pending indefinitely.\n  evidence: `ApplyRemovalProofMatch(matched: false)` keeps `AuditPending` (`TenantCreateCommandModels.cs:1155`) when the walk reaches the last page (`!audit.HasMore`, `RemoveTenantMemberFlow.razor:1135`) without any match, weak or strong. Running out of the page budget is already Delayed (`:1157`), and so is a last page with only weak matches. The Pending recovery set has no Escalate, so once the three-retry limit withdraws Refresh, Inspect audit is the only recovery left, and the state stays pending until a host refresh changes it. The walk and its matching predate Story 5.4, and the frozen spec keeps them. RemoveMember has no retention or expiry window, so a fix would move an unmatched attempt to Delayed after a bounded number of unmatched walks, or would first add an attempt deadline to the flow."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: RemoveTenantMemberFlow.razor::RefreshRemovalProofAsync reaches !audit.HasMore and calls ApplyRemovalProofMatch(matched:false) when no weak/strong match exists. TenantCreateCommandModels.cs::TenantRemoveMemberCommandSnapshot.ApplyRemovalProofMatch preserves available/delayed but otherwise returns AuditPending with no attempt deadline. Exhausting pages/weak matches becomes Delayed, while repeated complete unmatched walks can remain pending indefinitely.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor; src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: Attempt wait/deadline threshold
next-action: Choose a bounded unmatched-walk/deadline threshold within the existing recovery contract and transition exhausted waiting to Delayed with tests.
related: DW-447, DW-449


### DW-449: Treat a 404 status lag as a wait in the command dimension, not an assertive unable-to-verify.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery.md (2026-09-29, review 2)"), 2026-10-12
location: src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`\n  summary: Treat a 404 status lag as a wait in the command dimension, not an assertive unable-to-verify.\n  evidence: `TenantCommandGateway.GetStatusAsync` maps a 404 to `TenantCommandStatusResult.Pending` (null `Status`, `IsPending`). The `ApplyStatus` methods for Create, AddMember, ChangeRole, RemoveMember and Metadata still send every null status to UnableToVerify, announced assertively (`TenantCreateCommandModels.cs:255` and siblings). The baseline did the same. Narrowed by the Story 5.4 Review 3 decision (option b): the audit dimension now keeps its state on the lag through `TenantCommandAuditStates.FromStatusLookup`, so only the command dimension remains. Lifecycle and the configuration snapshots already handle `IsPending` and `IsRetryableFailure` as waits in both dimensions."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: TenantCommandGateway.GetStatusAsync maps 404 to TenantCommandStatusResult.Pending. TenantCreateCommandModels.cs Create/AddMember/ChangeRole/RemoveMember/Metadata ApplyStatus still maps every null Status to UnableToVerify with Assertive politeness; only AuditState uses FromStatusLookup and retains lag correctly. Lifecycle/configuration snapshots already handle IsPending as waiting. The recorded option-b decision narrowed this to command dimension and must be preserved.
verification-files: src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: Preserve Story 5.4 option-b command/audit separation
next-action: Handle IsPending in the five command lifecycle snapshots without assertive unverifiable feedback, preserving their already-correct independent audit state.
related: DW-447, DW-448


### DW-450: Decide once for the whole repo whether Severe (and Danger) status badges use `BadgeAppearance.Filled`, as DESIGN.md specifies.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery.md (2026-09-29, review 2)"), 2026-10-12
location: _bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md; src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditAvailabilityState.razor; src/Hexalith.Tenants.UI/Components/Shared/TruthStateBadge.razor
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`\n  summary: Decide once for the whole repo whether Severe (and Danger) status badges use `BadgeAppearance.Filled`, as DESIGN.md specifies.\n  evidence: DESIGN.md `truth-state-badge` says \"Tint (default) · Filled (Danger + Severe)\". `AuditAvailabilityState.razor:27` and `TruthStateBadge.razor` both render Severe with `Tint`, and Story 5.4's spec asked for Tint. Changing only the audit badge would make it diverge from the freshness badge."
status: open
disposition: awaiting-decision
verification-date: 2026-10-12
verification: _bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md:58 specifies Filled for Danger/Severe. Components/Tenants/Audit/AuditAvailabilityState.razor and Components/Shared/TruthStateBadge.razor hard-code Tint; the completed availability story specifically requested Tint. A repo-wide design precedence decision remains needed before treating one badge as a defect.
verification-files: _bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md; src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditAvailabilityState.razor; src/Hexalith.Tenants.UI/Components/Shared/TruthStateBadge.razor
evidence-kind: recommendation
priority: P3
owner: unassigned
dependencies: Repo-wide UX/design precedence decision
next-action: Decide the shared Danger/Severe badge appearance, then align both components and DESIGN.md together.
related: DW-440


## Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery.md (2026-09-29, review 3)

Review range `1cdcc0a9..e077e65e` (delta only).

### DW-451: Seventh undeclared-bump occurrence for the CI story gitlink gate tracked above (Story 5.2 entry, "Un-defer the CI story gitlink gate").

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery.md (2026-09-29, review 3)"), 2026-10-12
location: .github/workflows/story-guards.yml
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`\n  summary: Seventh undeclared-bump occurrence for the CI story gitlink gate tracked above (Story 5.2 entry, \"Un-defer the CI story gitlink gate\").\n  evidence: Commit `e077e65e` (\"update audit availability and recovery specifications and sprint status\") moved `references/Hexalith.Builds` `0610f783`→`85ca19bc`, `references/Hexalith.EventStore` `801f3e52`→`cc79c03a` and `references/Hexalith.FrontComposer` `7d3af61e`→`f86fb730` without a declaration. `validate-story-gitlinks.py` on the 5.4 spec exits 1, while `.github/workflows/story-guards.yml:35` runs only the guard's own regression suite, so CI stayed green."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: The historical e077e65e occurrence remains valid history. .github/workflows/story-guards.yml still runs only the guard regression suite, and the current Story 5.4 validator executed 2026-10-12 exits 1 for later undeclared McpCli/Memories/Platform/PolymorphicSerializations pointers. The missing changed-story enforcement identified by the seventh occurrence is still present.
verification-files: .github/workflows/story-guards.yml
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: DW-437 CI gate and story attribution
next-action: Implement the already-approved changed-story CI gate from DW-437 and preserve this recurrence as supporting history.
related: DW-437
duplicate-of: DW-437


### DW-452: Hand off to Hexalith.Builds. The G-6 validator tuple pins CommunityToolkit.Aspire.Hosting.Dapr `13.5.1-beta.767`, while the central catalog pins `.770`.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery.md (2026-09-29, review 3)"), 2026-10-12
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
priority: P3
archived: 2026-10-12

### DW-453: Hand off to Hexalith.Builds. Correct the false and stale claims in the 6-1 G-4/G-6 story record.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery.md (2026-09-29, review 3)"), 2026-10-12
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
priority: P3
archived: 2026-10-12

### DW-454: Hand off to Hexalith.Builds. The historical 2026-09-06 G-6 baseline and packet no longer validate, and the README still points at them.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery.md (2026-09-29, review 3)"), 2026-10-12
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
priority: P3
archived: 2026-10-12

### DW-455: Hand off to Hexalith.Builds. G-6 candidate and accepted evidence cannot be told apart.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery.md (2026-09-29, review 3)"), 2026-10-12
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
priority: P3
archived: 2026-10-12

### DW-456: Hand off to Hexalith.Builds. Strengthen the G-6 self-test mode checks.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery.md (2026-09-29, review 3)"), 2026-10-12
location: references/Hexalith.Builds/Tools/test-runtime-toolchain-evidence-validator.py
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`\n  summary: Hand off to Hexalith.Builds. Strengthen the G-6 self-test mode checks.\n  evidence: `test-runtime-toolchain-evidence-validator.py:289-291` asserts only `returncode == 1`, so an uncaught-exception mutant passes. The checks are bare `assert` statements, which `python -O` strips, and `:382` prints a literal \"22 scenarios\". No tampering scenario runs with `candidate=True`."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: Builds/Tools/test-runtime-toolchain-evidence-validator.py now derives result counts and has expanded authority/current-v2 mutation controls, closing stale literal-count coverage. However :471-473 still asserts only returncode == 1 for cross-mode negatives, and the suite still uses bare assert statements without an optimization guard; Python -O strips those checks. Legacy packet mutation negatives still use the default accepted-mode helper.
verification-files: references/Hexalith.Builds/Tools/test-runtime-toolchain-evidence-validator.py
evidence-kind: verified-gap
priority: P2
owner: Hexalith.Builds
dependencies: Hexalith.Builds-owned test harness change
next-action: Require expected validation diagnostics for mode negatives and reject optimized execution or replace bare asserts; run applicable packet tampering in both candidate and accepted modes.
related: DW-452, DW-455


### DW-457: Hand off to Hexalith.Builds. Record a disposition for the Aspire SDK split in the 2026-09-27 baseline.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery.md (2026-09-29, review 3)"), 2026-10-12
location: references/Hexalith.Builds/Tools/runtime-toolchain-baseline-2026-09-27.json
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`\n  summary: Hand off to Hexalith.Builds. Record a disposition for the Aspire SDK split in the 2026-09-27 baseline.\n  evidence: `runtime-toolchain-baseline-2026-09-27.json:12` certifies `aspireSdk` `13.5.4` while `pinAudit.appHostProjects` lists five AppHosts at `13.5.3`, including `Hexalith.Tenants.AppHost`, with no `aspireSdk` disposition. `dispositions` also has the case-duplicate keys `dapr` (object) and `Dapr` (string)."
status: open
disposition: awaiting-decision
verification-date: 2026-10-12
verification: references/Hexalith.Builds/Tools/runtime-toolchain-baseline-2026-09-27.json:12,22-34,59-66 still records aspireSdk 13.5.4 against five AppHosts at 13.5.3 and no Aspire SDK disposition; dapr and Dapr remain case-distinct keys. This is the retained historical qualification record, not evidence that current pins are all 13.5.3.
verification-files: references/Hexalith.Builds/Tools/runtime-toolchain-baseline-2026-09-27.json
evidence-kind: verified-gap
priority: P2
owner: Hexalith.Builds
dependencies: Builds toolchain qualification decision
next-action: Builds owners must annotate the historical Aspire split disposition and disambiguate the two Dapr meanings.
related: none


### DW-458: Withhold the shared control's Continue read-only in the member flows while an owned attempt is still in flight.

status: done 2026-10-13
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery.md (2026-09-29, review 3)"), 2026-10-12
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
priority: P1
archived: 2026-10-13

## Deferred from: code review of 5-4-audit-availability-state-recovery.md (2026-09-30)

### DW-459: Confirm that the first mounted availability state is announced by assistive technology.

origin: migrated from unnumbered ledger ("Deferred from: code review of 5-4-audit-availability-state-recovery.md (2026-09-30)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditAvailabilityState.razor
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/5-4-audit-availability-state-recovery.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/5-4-audit-availability-state-recovery.md`\n  summary: Confirm that the first mounted availability state is announced by assistive technology.\n  evidence: Unverified and medium if true. `AuditAvailabilityState` inserts its `aria-live` container together with the first `Pending` or `Unavailable` content. Settle with an NVDA/VoiceOver browser check of a `NotStarted` transition; if silent, keep the live container mounted empty before the first state."
status: open
disposition: uncertain
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditAvailabilityState.razor:7-21 still mounts its aria-live container together with the first state. Source and Chromium DOM fixtures cannot establish first-insertion announcements; no NVDA/VoiceOver NotStarted-to-Pending/Unavailable trace was found.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditAvailabilityState.razor
evidence-kind: unverified-risk
priority: P2
owner: unassigned
dependencies: NVDA or VoiceOver authenticated browser session
next-action: Capture an assistive-technology browser trace of the first mounted state before deciding whether an always-mounted empty live container is required.
related: none


### DW-460: Decide whether `TenantAuditReceipt.FromEntry` should accept authoritative lifecycle/provenance or be removed.

origin: migrated from unnumbered ledger ("Deferred from: code review of 5-4-audit-availability-state-recovery.md (2026-09-30)"), 2026-10-12
location: src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditReceipt.cs; src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditRow.cs
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/5-4-audit-availability-state-recovery.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/5-4-audit-availability-state-recovery.md`\n  summary: Decide whether `TenantAuditReceipt.FromEntry` should accept authoritative lifecycle/provenance or be removed.\n  evidence: Pre-existing to the current Story 5.4 refinement. `TenantAuditRow.FromEntry` defaults lifecycle and provenance to `Unknown`, while receipt completeness requires `Current` and `ProjectionBacked`, so the public factory cannot produce `Ready`. Resolution requires deciding whether callers may supply authoritative metadata or whether the misleading factory should be retired."
status: open
disposition: awaiting-decision
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditReceipt.cs:39-46 FromEntry still delegates to TenantAuditRow.FromEntry; TenantAuditRow.cs:38-39 defaults Lifecycle/Provenance to Unknown, while receipt completeness requires Current/ProjectionBacked at TenantAuditReceipt.cs:73-76. The public factory still cannot produce Ready.
verification-files: src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditReceipt.cs; src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditRow.cs
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Public receipt API decision
next-action: Choose an authoritative-metadata overload or deprecate the misleading FromEntry factory, preserving provenance checks.
related: none


## Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery-2.md (2026-09-30)

### DW-461: Give the legacy Story 5.4 artifact a baseline that resolves, or formally retire its guard role.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery-2.md (2026-09-30)"), 2026-10-12
location: scripts/validate-story-gitlinks.py; _bmad-output/implementation-artifacts/5-4-audit-availability-state-recovery.md
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery-2.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery-2.md`\n  summary: Give the legacy Story 5.4 artifact a baseline that resolves, or formally retire its guard role.\n  evidence: `5-4-audit-availability-state-recovery.md:3` pins `baseline_commit: a5ca6e3f548e89b28a37826be721d9ef9f7cd51a`, which is not a commit, so `scripts/validate-story-gitlinks.py` fails closed on it. The intended `a5ca6e38` (Story 5.3, 2026-06-06) predates the `references/` layout, and correcting it gives 7 \"absent at baseline\" failures. The work range is audited by `spec-5-4-understand-audit-availability-and-recovery.md` (`55f3dc63..HEAD`, which passes with Builds, EventStore and FrontComposer declared) and by `spec-…-2.md` (`d728ff46..HEAD`, which passes). The 2026-09-30 completion note in the legacy story says the story guard \"reported no pointer changes\"; that result came from the follow-up spec. The owner should choose between repointing the baseline to `55f3dc63` and marking the legacy artifact exempt from the guard."
status: open
disposition: awaiting-decision
verification-date: 2026-10-12
verification: Ran PYTHONDONTWRITEBYTECODE=1 python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/5-4-audit-availability-state-recovery.md: RESULT FAIL because baseline_commit a5ca6e3f548e89b28a37826be721d9ef9f7cd51a is not a commit. The historical baseline remains unchanged; do not substitute later HEAD history for the story range.
verification-files: scripts/validate-story-gitlinks.py; _bmad-output/implementation-artifacts/5-4-audit-availability-state-recovery.md
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Historical artifact validation policy
next-action: Choose a resolvable audited range for the legacy artifact or explicitly retire its executable guard role.
related: DW-467,DW-478


### DW-462: Refresh the stale parts of the legacy Story 5.4 record.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery-2.md (2026-09-30)"), 2026-10-12
location: _bmad-output/implementation-artifacts/5-4-audit-availability-state-recovery.md; _bmad-output/implementation-artifacts/story-5-4-re-review-verification-2026-09-30.md
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery-2.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery-2.md`\n  summary: Refresh the stale parts of the legacy Story 5.4 record.\n  evidence: `5-4-audit-availability-state-recovery.md:211` still reads \"Outcome: Approve (status → done)\" under a `Status: review` story. The ticked 2026-09-30 patch anchors point at `AuditAvailabilityState.razor:299/:220/:51`; the current locations are `:287-376`, `:236-243` and `:56`. The 2026-09-30 hardening has no Debug Log References with exact commands, and the \"three mutation rejections\" are never named, so the evidence cannot be reproduced."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: _bmad-output/implementation-artifacts/5-4-audit-availability-state-recovery.md now says Status: done, so the status mismatch is resolved. Retained 2026-09-30 review patches still use obsolete source-line anchors, and the addendum says exact commands and mutation names were not recorded; story-5-4-re-review-verification-2026-09-30.md remains the evidence pointer.
verification-files: _bmad-output/implementation-artifacts/5-4-audit-availability-state-recovery.md; _bmad-output/implementation-artifacts/story-5-4-re-review-verification-2026-09-30.md
evidence-kind: verified-gap
priority: P3
owner: unassigned
dependencies: DW-461 historical guard disposition
next-action: Add a dated historical-evidence clarification with current symbol anchors and recover exact commands/mutation identities where available; preserve the original notes.
related: DW-461


## Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery-3.md (2026-09-30)

Review range `55fc6f91..a4a1ce13`.

### DW-463: Restore CI test execution. `ci / build-and-test` fails at "Validate package consumer references", so no Tenants test tier has run in CI since at least 2026-09-29.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery-3.md (2026-09-30)"), 2026-10-12
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery-3.md`
priority: P1
archived: 2026-10-12

### DW-464: Repair `validate-remove-focus-in-chromium`, which aborts in CI with Chrome 153 (core dump, exit 134) on every `main` push.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery-3.md (2026-09-30)"), 2026-10-12
location: tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery-3.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery-3.md`\n  summary: Repair `validate-remove-focus-in-chromium`, which aborts in CI with Chrome 153 (core dump, exit 134) on every `main` push.\n  evidence: `validate-tenants-focus-browser.sh` gets \"Aborted (core dumped)\" from `--headless=new --disable-dev-shm-usage --disable-gpu` in runs `36559824612` (`55f3dc63`) through `36747918865` (`a4a1ce13`). The workflow last passed on 2026-09-22 (`35782893330`), and local Chrome 154 passes. A runner sandbox restriction is suspected but unverified; settle it by uploading the `${output_path}.stderr` Chrome writes."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: Verified current-head hosted failure: HEAD 7a2a126f, Story Guards run 38269520326, job 114880028087 (https://github.com/Hexalith/Hexalith.Tenants/actions/runs/38269520326/job/114880028087) logs Chrome 153.0.8010.52 and profile-shipped exit 124 after the 30-second bound. /tmp/tenants-ledger-current-chromium-job.log preserves that evidence. Current validate-tenants-focus-browser.sh has --no-sandbox, timeout and stderr reporting, but the hosted launch still times out before later preview scenarios.
verification-files: tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: Hosted runner Chromium launch diagnosis
next-action: Diagnose the bounded profile-shipped timeout with the exact pinned Chrome 153 hosted runner configuration, then rerun all preview/negative-control scenarios.
related: DW-496,DW-555


### DW-465: Let the browser harness run against the local Debug build, so local UI evidence can be reproduced from the repository.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery-3.md (2026-09-30)"), 2026-10-12
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery-3.md`
priority: P2
archived: 2026-10-12

### DW-466: Some bUnit `WaitForAssertion` checks in the UI suite time out under load.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery-3.md (2026-09-30)"), 2026-10-12
location: tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery-3.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery-3.md`\n  summary: Some bUnit `WaitForAssertion` checks in the UI suite time out under load.\n  evidence: `TenantDetailSurfaceTests.Detail_lifecycle_actions_fail_closed_while_authorization_is_pending` (`:1924`) failed with check count 85 and render count 86 in a full MTP run (3,585/3,586) while four review agents were running; it passes 3 of 3 runs on its own. Story 5.4's verification report also records an unnamed \"metadata-confirmation\" timeout (3,579/3,580) that passed on rerun. Neither test is in the reviewed diff. Name the tests, then raise their per-call timeouts or make their waits event-driven."
status: open
disposition: uncertain
verification-date: 2026-10-12
verification: tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs still contains Detail_lifecycle_actions_fail_closed_while_authorization_is_pending with render-based WaitForAssertion. The 2026-09-30 evidence reports load-dependent failures and an unnamed metadata-confirmation timeout; no current controlled load reproduction or precise identity for the second test is available.
verification-files: tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs
evidence-kind: unverified-risk
priority: P2
owner: unassigned
dependencies: Historical full-suite raw logs; controlled load reproduction
next-action: Identify the metadata test from raw historical logs and reproduce both waits under controlled load before changing timeout or synchronization.
related: none


### DW-467: `validate-story-gitlinks.py` still exits 1 on the legacy Story 5.4 artifact. This recurs the 2026-09-30 entry above; add no separate action.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery-3.md (2026-09-30)"), 2026-10-12
location: scripts/validate-story-gitlinks.py; _bmad-output/implementation-artifacts/5-4-audit-availability-state-recovery.md
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery-3.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery-3.md`\n  summary: `validate-story-gitlinks.py` still exits 1 on the legacy Story 5.4 artifact. This recurs the 2026-09-30 entry above; add no separate action.\n  evidence: `5-4-audit-availability-state-recovery.md:3` `baseline_commit a5ca6e3f…` is not a commit. The spec-3 range (`55fc6f9..HEAD`, no pointer changes) and the primary spec range (`55f3dc6..HEAD`, Builds, EventStore and FrontComposer declared) both pass."
status: open
disposition: awaiting-decision
verification-date: 2026-10-12
verification: Ran PYTHONDONTWRITEBYTECODE=1 python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/5-4-audit-availability-state-recovery.md: RESULT FAIL because baseline_commit a5ca6e3f548e89b28a37826be721d9ef9f7cd51a is not a commit. The historical baseline remains unchanged; do not substitute later HEAD history for the story range.
verification-files: scripts/validate-story-gitlinks.py; _bmad-output/implementation-artifacts/5-4-audit-availability-state-recovery.md
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Historical artifact validation policy
next-action: Choose a resolvable audited range for the legacy artifact or explicitly retire its executable guard role.
related: DW-461,DW-478
duplicate-of: DW-461


### DW-468: Reconcile dotnet formatter same-line-brace expectations with the Hexalith baseline requirement for Allman braces.

status: done 2026-10-16
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery-3.md (2026-09-30)"), 2026-10-12
source_spec: `_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery-4.md`
priority: P3
archived: 2026-10-17

## Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery.md (2026-10-01, review 5)

Review range `54ceb3e1..697697f9`.

### DW-469: Use the retry-aware `TenantCommandAuditStates.FromSubmission(result, retriedMessageId)` in Lifecycle, Set configuration and Remove configuration.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery.md (2026-10-01, review 5)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleCommandFlow.razor; src/Hexalith.Tenants.UI/Components/Tenants/Configuration/SetTenantConfigurationFlow.razor; src/Hexalith.Tenants.UI/Components/Tenants/Configuration/RemoveTenantConfigurationFlow.razor
source_spec: `_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`\n  summary: Use the retry-aware `TenantCommandAuditStates.FromSubmission(result, retriedMessageId)` in Lifecycle, Set configuration and Remove configuration.\n  evidence: Pre-existing; the 2026-10-01 fix pass wired it in AddMember, ChangeRole, RemoveMember and Metadata only. `TenantLifecycleCommandFlow.razor:1315`, `SetTenantConfigurationFlow.razor:698` and `RemoveTenantConfigurationFlow.razor:937` still call `FromSubmission(result)`. Their tracked gateways return `FailedWithKey` with no MessageId for failures before dispatch (`TenantCommandGateway.cs`, `UnavailableTenantCommandGateway.cs`), so a re-dispatch of a retained, possibly delivered identity that fails that way reports NotStarted and hides the control. Lifecycle's re-dispatch site (`:1550`) is explicit. The configuration flows first need the tracker to report whether `BeginDispatch` resumed a retained identity."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleCommandFlow.razor:1315, Configuration/SetTenantConfigurationFlow.razor:702 and RemoveTenantConfigurationFlow.razor:937 still call TenantCommandAuditStates.FromSubmission(result) without a retained retry MessageId. Gateway FailedWithKey validation failures omit MessageId, allowing resumed ambiguous attempts to map to NotStarted.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleCommandFlow.razor; src/Hexalith.Tenants.UI/Components/Tenants/Configuration/SetTenantConfigurationFlow.razor; src/Hexalith.Tenants.UI/Components/Tenants/Configuration/RemoveTenantConfigurationFlow.razor
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: Configuration tracker resumed-identity result
next-action: Pass retry identity to lifecycle audit mapping and expose whether configuration BeginDispatch resumed a retained identity; test pre-dispatch failure on retry.
related: none


### DW-470: Keep the localized reason of a keyed failed submission in AddMember and ChangeRole.

status: done 2026-10-16
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery.md (2026-10-01, review 5)"), 2026-10-12
source_spec: `_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
priority: P2
archived: 2026-10-17

## Deferred from: build review of spec-5-4-understand-audit-availability-and-recovery-5.md (2026-10-01, review 1)

Review range `c9cd9045..` working tree (the Review 5 patch closure).

### DW-471: Drop a late status lookup in AddMember, ChangeRole and RemoveMember when Continue read-only has reset the attempt, as Create now does.

status: done 2026-10-13
origin: migrated from unnumbered ledger ("Deferred from: build review of spec-5-4-understand-audit-availability-and-recovery-5.md (2026-10-01, review 1)"), 2026-10-12
source_spec: `_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery-5.md`
priority: P1
archived: 2026-10-13

### DW-472: Do not re-apply a status lookup to a confirmed attempt in AddMember, ChangeRole and Create.

status: done 2026-10-13
origin: migrated from unnumbered ledger ("Deferred from: build review of spec-5-4-understand-audit-availability-and-recovery-5.md (2026-10-01, review 1)"), 2026-10-12
source_spec: `_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery-5.md`
priority: P1
archived: 2026-10-13

### DW-473: Unverified (would be medium): Create's projection reconcile may write UnableToVerify onto the panel that Continue read-only reset.

origin: migrated from unnumbered ledger ("Deferred from: build review of spec-5-4-understand-audit-availability-and-recovery-5.md (2026-10-01, review 1)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor
source_spec: `_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery-5.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery-5.md`\n  summary: Unverified (would be medium): Create's projection reconcile may write UnableToVerify onto the panel that Continue read-only reset.\n  evidence: The message-id guard covers only the status apply. If Continue read-only lands during the reconcile's `ProjectionEvidenceProvider` await, the next `_snapshot.Intent.TenantId` throws, and the catch arm writes an Assertive `UnableToVerify` from the Idle snapshot (`CreateTenantFlow.razor:590-609`). In the only host, `TenantsWorkspace.razor:188-189`, `OnProjectionRefreshRequested` re-renders the flow to AuditPending (no Continue read-only verb) before the reconcile reads, so the path looks unreachable there. To settle it, write a host-level test, or a bUnit test without the callback, that clicks Continue read-only during a held `ProjectionEvidenceProvider` and asserts the panel stays Idle."
status: open
disposition: uncertain
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor:591-613 reads _snapshot.Intent after ProjectionEvidenceProvider awaits and catches errors by writing UnableToVerify. The production OnProjectionRefreshRequested callback changes the mounted host state first; no controlled test demonstrates that Continue read-only can remain reachable during that await in the actual host.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor
evidence-kind: unverified-risk
priority: P2
owner: unassigned
dependencies: Held projection-provider continuation and host callback scheduling evidence
next-action: Hold ProjectionEvidenceProvider across Continue read-only in a host-level test and a callback-free component test; determine reachability and assert no stale reconciliation overwrites Idle.
related: none


## Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery.md (2026-10-01, review 7)

Review range `55f3dc63..fa489329`, chunk 1 (`src` minus `State/` and `Services/`).

### DW-474: Give the audit availability badge `Shape="BadgeShape.Rounded"` and an `IconLabel`, as DESIGN.md's truth-state-badge anatomy requires, as part of the repo-wide badge alignment decision.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery.md (2026-10-01, review 7)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditAvailabilityState.razor; _bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md
source_spec: `_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`\n  summary: Give the audit availability badge `Shape=\"BadgeShape.Rounded\"` and an `IconLabel`, as DESIGN.md's truth-state-badge anatomy requires, as part of the repo-wide badge alignment decision.\n  evidence: `AuditAvailabilityState.razor:27-34` sets neither, so the Fluent default shape applies. It follows `Components/Shared/TruthStateBadge.razor`, as the spec's Code Map directs, and no badge in the repo sets a shape (DESIGN.md:59-60, 178, 188). The visible text and `aria-label` already carry the state name, so this is visual conformance only. Decide it together with the open Review 2 Tint vs Filled deferral, so every badge changes at once."
status: open
disposition: awaiting-decision
verification-date: 2026-10-12
verification: AuditAvailabilityState.razor:27-34 still has Tint with no Shape or IconLabel. _bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md:58-60,178,188 specifies Rounded/IconLabel and Filled for Danger/Severe. Visible state text and aria-label already provide the accessible name; this is a visual/conformance recommendation, not a demonstrated accessibility defect.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditAvailabilityState.razor; _bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md
evidence-kind: recommendation
priority: P3
owner: unassigned
dependencies: Existing Tint-versus-Filled badge decision
next-action: Settle the existing repo-wide badge anatomy/appearance decision and apply the resulting shared convention together.
related: none


### DW-475: Replace the source-text pin of the seven flows' denied-read Inspect-audit wiring with a rendered theory.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery.md (2026-10-01, review 7)"), 2026-10-12
location: tests/Hexalith.Tenants.UI.Tests/Components/AuditAvailabilityStateTests.cs
source_spec: `_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`\n  summary: Replace the source-text pin of the seven flows' denied-read Inspect-audit wiring with a rendered theory.\n  evidence: `AuditAvailabilityStateTests.cs:924-966` asserts `=> AuditReadDenied ? null : CommandAuditEntryPointTemplate;` through `File.ReadAllText`. Only Create has rendered coverage (`CreateTenantFlowTests.cs:226-256`), and `RemoveTenantMemberFlowTests.cs:67-77` checks only that the entry point is absent, which an empty `[data-recovery-verb='inspectaudit']` shell would also pass. Add one theory across AddMember, ChangeRole, RemoveMember, Metadata, Lifecycle, Set configuration and Remove configuration that asserts no `inspectaudit` recovery renders when the audit read is denied."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: tests/Hexalith.Tenants.UI.Tests/Components/AuditAvailabilityStateTests.cs:969-988 still reads source to pin AuditReadDenied entry-point wiring. Member rendered denial coverage checks the handoff entry point but does not provide the proposed seven-flow rendered theory excluding the inspectaudit recovery shell.
verification-files: tests/Hexalith.Tenants.UI.Tests/Components/AuditAvailabilityStateTests.cs
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Reusable rendered flow fixtures
next-action: Add a rendered denied-audit theory across member, metadata, lifecycle and configuration flows and assert no inspectaudit recovery control or shell appears.
related: none


### DW-476: Let the existing browser validator consume the selected local build configuration instead of hardcoding Release CSS paths.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery.md (2026-10-01, review 7)"), 2026-10-12
source_spec: `_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
priority: P2
archived: 2026-10-12

### DW-477: Preserve and print Chromium stderr when the browser validator exits before its validation-result checks.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery.md (2026-10-01, review 7)"), 2026-10-12
source_spec: `_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
priority: P2
archived: 2026-10-12

### DW-478: Resolve or explicitly retire the legacy Story 5.4 artifact's non-resolving validation baseline.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery.md (2026-10-01, review 7)"), 2026-10-12
location: scripts/validate-story-gitlinks.py; _bmad-output/implementation-artifacts/5-4-audit-availability-state-recovery.md
source_spec: `_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`\n  summary: Resolve or explicitly retire the legacy Story 5.4 artifact's non-resolving validation baseline.\n  evidence: Build completion review BH10 verified that python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/5-4-audit-availability-state-recovery.md exits 1 because a5ca6e3f548e89b28a37826be721d9ef9f7cd51a is not a commit. The historical baseline is preserved; the current owning spec's canonical baseline passes its guard."
status: open
disposition: awaiting-decision
verification-date: 2026-10-12
verification: Ran PYTHONDONTWRITEBYTECODE=1 python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/5-4-audit-availability-state-recovery.md: RESULT FAIL because baseline_commit a5ca6e3f548e89b28a37826be721d9ef9f7cd51a is not a commit. The historical baseline remains unchanged; do not substitute later HEAD history for the story range.
verification-files: scripts/validate-story-gitlinks.py; _bmad-output/implementation-artifacts/5-4-audit-availability-state-recovery.md
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Historical artifact validation policy
next-action: Choose a resolvable audited range for the legacy artifact or explicitly retire its executable guard role.
related: DW-461,DW-467
duplicate-of: DW-461


### DW-479: Split the fourteen pre-existing public command-model types into their own named C# files.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery.md (2026-10-01, review 7)"), 2026-10-12
location: src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs
source_spec: `_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`\n  summary: Split the fourteen pre-existing public command-model types into their own named C# files.\n  evidence: Build completion review BH11 compared TenantCreateCommandModels.cs with baseline 55f3dc63b6ce10bb0afdf929d026fa07cffd9105 and found all fourteen public types already present. The monolithic file remains contrary to the Hexalith one-type-per-file rule; Review 7's fixes did not edit that file."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs still declares fourteen public types (records/enums at lines 23,27,36,51,60,74,79,134,171,432,614,857,1241,1524), contrary to the required Hexalith one-type-per-file authoring rule.
verification-files: src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs
evidence-kind: verified-gap
priority: P3
owner: unassigned
dependencies: none
next-action: Split existing command-model types into their matching type-named files in a separate mechanical maintenance change.
related: DW-530


## Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery.md (2026-10-01, review 8)

Review chunk: `fa489329..78e09184` for `src` and `tests`, plus the full-story diff of the browser harness and of the composition, workspace and generated-surface tests.

### DW-480: After a first-attempt activity-lease refusal in RemoveMember, a second submit shows "This attempt can no longer be tracked", although nothing was dispatched.

status: done 2026-10-16
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery.md (2026-10-01, review 8)"), 2026-10-12
source_spec: `_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
priority: P2
archived: 2026-10-17

## Deferred from: code review of spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md (2026-10-02)

### DW-481: Recheck current circuit authority and membership immediately before tenant correction confirmation dispatch (high).

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md (2026-10-02)"), 2026-10-12
source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`
priority: P1
archived: 2026-10-12

### DW-482: Decide and expose redacted last-owner consequence facts in tenant correction previews (medium).

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md (2026-10-02)"), 2026-10-12
source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`
priority: P2
archived: 2026-10-12

## Deferred from: code review of spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md (2026-10-02)

Review diff: `bcfc0788..f44e19e7` (story commits `cefefa26` and `f44e19e7`, merged as PR #51 `09c90f08`).

### DW-483: A submitted, tracked tenant-correction preview is still unmounted by refresh, list-refresh, row loss and viewport narrowing (pre-existing).

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md (2026-10-02)"), 2026-10-12
source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`
priority: P1
archived: 2026-10-12

### DW-484: CI still runs no Tenants test tier (already tracked above; reconfirmed).

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md (2026-10-02)"), 2026-10-12
source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`
priority: P1
archived: 2026-10-12

## Deferred from: build review of spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md (2026-10-02)

Review range: story baseline `4f426e59` to the fix pass later committed as `31c2d2e1`. Finding IDs refer to that build review's triage log.

### DW-485: Retain a submitted correction preview across audit refresh, row loss, and viewport changes (already tracked above).

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: build review of spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md (2026-10-02)"), 2026-10-12
source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`
priority: P1
archived: 2026-10-12

### DW-486: Require attempt-specific provenance before linking a corrective audit event as proof.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: build review of spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md (2026-10-02)"), 2026-10-12
source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`
priority: P1
archived: 2026-10-12

### DW-487: Give the corrective audit proof link a matching destination in the audit grid.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: build review of spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md (2026-10-02)"), 2026-10-12
source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`
priority: P2
archived: 2026-10-12

### DW-488: Show the original evidence timestamp in the Story 5.6 final preview.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: build review of spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md (2026-10-02)"), 2026-10-12
source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`
priority: P2
archived: 2026-10-12

### DW-489: Verify the composed audit grid, receipt, start, and preview at narrow widths in an authenticated browser run.

origin: migrated from unnumbered ledger ("Deferred from: build review of spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md (2026-10-02)"), 2026-10-12
location: tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh; _bmad-output/implementation-artifacts/story-5-6-reverification-2026-10-08.json
source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`\n  summary: Verify the composed audit grid, receipt, start, and preview at narrow widths in an authenticated browser run.\n  evidence: BH12; the isolated browser fixture cannot prove the composed layout. This is a medium-impact possibility without demonstrated overflow; a live authenticated browser run across measured widths would settle it when the existing query-gateway authentication limitation is resolved."
status: open
disposition: uncertain
verification-date: 2026-10-12
verification: tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh validates isolated start/preview/global fixtures, not the authenticated composed audit grid+receipt+start+preview. story-5-6-reverification-2026-10-08.json expressly excludes authenticated live-command assertions. No measured composed overflow or current live narrow-width trace is available.
verification-files: tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh; _bmad-output/implementation-artifacts/story-5-6-reverification-2026-10-08.json
evidence-kind: unverified-risk
priority: P2
owner: unassigned
dependencies: Authenticated live Aspire/browser environment
next-action: Run the authenticated composed audit/correction surface at measured narrow widths and record whether required fields and focus remain usable.
related: none


## Deferred from: code review of spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md (2026-10-02)

Review diff: `9bad98d9..31c2d2e1` (re-review of the review-fix commit).

### DW-490: Cancelling during an in-flight correction submission loses the panel's `HasSubmitted` flag, so a new attempt can be sent (pre-existing).

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md (2026-10-02)"), 2026-10-12
source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`
priority: P1
archived: 2026-10-12

### DW-491: Ambiguous, FailedWithKey and RejectedWithKey correction submissions show no localized reason and offer no retry with the same message ID (pre-existing).

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md (2026-10-02)"), 2026-10-12
source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`
priority: P2
archived: 2026-10-12

### DW-492: `TenantAuditPageTests.Loading_receipt_renders_while_focus_probe_and_authoritative_read_are_pending` fails when run alone and is flaky in the full suite (pre-existing, Story 5.3 `38da46a6`).

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md (2026-10-02)"), 2026-10-12
source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`
priority: P2
archived: 2026-10-12

### DW-493: Verify whether viewport observation can change concurrently between the final safety check and correction attempt admission.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md (2026-10-02)"), 2026-10-12
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
priority: P2
archived: 2026-10-12

## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md (2026-10-03)

Review diff: `11e65e37..17538e07` (story commit `17538e07`).

### DW-494: The global-administrator correction still links corrective proof by event type plus a later timestamp (pre-existing; Story 5.7).

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md (2026-10-03)"), 2026-10-12
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
priority: P1
archived: 2026-10-12

### DW-495: CI still runs no Tenants test tier (already tracked; reconfirmed).

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md (2026-10-03)"), 2026-10-12
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
priority: P1
archived: 2026-10-12

### DW-496: The Story 5.6 preview browser validation never runs in CI (pre-existing).

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md (2026-10-03)"), 2026-10-12
location: tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: The Story 5.6 preview browser validation never runs in CI (pre-existing).\n  evidence: Story Guards aborts with exit 134 (\"Aborted (core dumped)\") at the first Chromium launch in `validate-tenants-focus-browser.sh` (`:266`), before the preview fixture steps (`:330-360`). The rendered EN/FR preview, focus, forced-colors and egress checks therefore run locally only."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: Verified current-head hosted failure: HEAD 7a2a126f, Story Guards run 38269520326, job 114880028087 (https://github.com/Hexalith/Hexalith.Tenants/actions/runs/38269520326/job/114880028087) logs Chrome 153.0.8010.52 and profile-shipped exit 124 after the 30-second bound. /tmp/tenants-ledger-current-chromium-job.log preserves that evidence. Current validate-tenants-focus-browser.sh has --no-sandbox, timeout and stderr reporting, but the hosted launch still times out before later preview scenarios.
verification-files: tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: Hosted runner Chromium launch diagnosis
next-action: Diagnose the bounded profile-shipped timeout with the exact pinned Chrome 153 hosted runner configuration, then rerun all preview/negative-control scenarios.
related: DW-464,DW-555
duplicate-of: DW-464


## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 2 (2026-10-03)

Review diff: `17538e07..93b0b96d` (fix pass `93b0b96d`).

### DW-497: The story gitlink validator exits 1 on the working tree because `references/Hexalith.Builds` and `references/Hexalith.FrontComposer` were moved outside this story.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 2 (2026-10-03)"), 2026-10-12
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
priority: P2
archived: 2026-10-12

### DW-498: Decide how an authorized correction result can be inspected after the submitting owner demotes themselves.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 2 (2026-10-03)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: Decide how an authorized correction result can be inspected after the submitting owner demotes themselves.\n  evidence: TenantsBffComposition.ComposeTenantCorrectionProjectionsAsync returns unavailable captures for nonowners; its owner/global authorization predicate is unchanged from baseline 11e65e3. Correction confirmation properly refuses redacted postconditions, leaving self-demotion unconfirmed until another authorized actor inspects it; a result-read policy must preserve authorization."
status: open
disposition: awaiting-decision
verification-date: 2026-10-12
verification: TenantsBffComposition.cs:562-567 still returns unavailable correction captures to a caller who is neither current owner nor global administrator. Self-demotion therefore removes authorized confirmation evidence; this follows the existing authorization boundary rather than proving a security bug. No approved result-read exception or authorized alternate-actor handoff contract is recorded.
verification-files: src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs
evidence-kind: recommendation
priority: P2
owner: unassigned
dependencies: Product/authorization result-read policy
next-action: Choose and document an authorized post-self-demotion inspection/handoff policy; retain current redaction until that decision.
related: none


### DW-499: Make terminal EventStore advisory status publication recoverable after a failed write or crash.

status: done 2026-10-14
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 2 (2026-10-03)"), 2026-10-12
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
priority: P1
archived: 2026-10-15

### DW-500: Preserve ambiguous delivery identity and aggregate admission in the ordinary add-member and change-role flows.

status: done 2026-10-13
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 2 (2026-10-03)"), 2026-10-12
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
priority: P1
archived: 2026-10-13

### DW-501: Pin FrontComposer deferred editor-focus preservation when an abandonment warning reopens before its queued callback runs.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 2 (2026-10-03)"), 2026-10-12
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
priority: P2
archived: 2026-10-12

### DW-502: Validate or quarantine malformed persisted envelope ranges before EventStore command-resume publication.

status: done 2026-10-14
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 2 (2026-10-03)"), 2026-10-12
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
priority: P1
archived: 2026-10-15

### DW-503: Verify whether correction delivery/status continuations outside the renderer can corrupt visible state during remount or expiry (medium, unverified).

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 2 (2026-10-03)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: Verify whether correction delivery/status continuations outside the renderer can corrupt visible state during remount or expiry (medium, unverified).\n  evidence: Final BH4 observes ConfigureAwait(false) continuations assigning component fields. The actual page unmounts on cleared intent and the tracker rejects mismatched or terminal regression, so user-visible corruption was not established. A controlled late status response overlapped with parameter change, expiry notification, and remount must demonstrate or refute a concrete outcome before any state-management patch."
status: open
disposition: uncertain
verification-date: 2026-10-12
verification: CorrectionStartPanel.razor delivery/status continuations still use ConfigureAwait(false) followed by SetRetainedSnapshot; that helper writes the component snapshot directly at :1063-1071. Tracker identity/terminal checks and actual remounts may contain visible effects. No controlled late status+expiry+parameter-change/remount outcome establishes corruption.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor
evidence-kind: unverified-risk
priority: P2
owner: unassigned
dependencies: Controlled renderer/continuation scheduling evidence
next-action: Hold a status response across expiry, parameter replacement and remount, then establish or refute a concrete visible state regression before changing threading.
related: none


## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 3 (2026-10-04)

Diff reviewed: Tenants `c0afce2e..af69f36d` (code only) and EventStore `b51978dd..b0464255`, `ff2fcc9f`, `865cd9e4`.

### DW-504: Mark the older Story 5.6 record as superseded on audit proof linking.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 3 (2026-10-04)"), 2026-10-12
location: _bmad-output/implementation-artifacts/5-6-preview-and-confirm-correction-with-linked-proof.md; src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: Mark the older Story 5.6 record as superseded on audit proof linking.\n  evidence: `5-6-preview-and-confirm-correction-with-linked-proof.md` is still `Status: done`, and its AC 8 and Completion Notes (`:188-194`) claim audit proof linking and support-safe proof links. The current implementation always applies `WithCorrectiveProof(null)` (`CorrectionStartPanel.razor:1031`), and the tenant confirmation copy says the audit association is unavailable. A reader of the older record gets a false picture of what shipped."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: _bmad-output/implementation-artifacts/5-6-preview-and-confirm-correction-with-linked-proof.md remains Status: done and Completion Notes :188-194 claim tenant audit proof lookup/linking. Current CorrectionStartPanel.razor:1044 deliberately uses WithCorrectiveProof(null), and the newer spec says audit association support is missing. The older artifact has no superseding clarification for this claim.
verification-files: _bmad-output/implementation-artifacts/5-6-preview-and-confirm-correction-with-linked-proof.md; src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor
evidence-kind: verified-gap
priority: P3
owner: unassigned
dependencies: Artifact historical-claim annotation
next-action: Add a dated superseded-behavior note linking the current 5.6 spec and its honest tenant missing-association state, preserving historical acceptance text.
related: DW-486


### DW-505: CI still runs none of Story 5.6's pass-3 tests (already tracked; reconfirmed).

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 3 (2026-10-04)"), 2026-10-12
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: CI still runs none of Story 5.6's pass-3 tests (already tracked; reconfirmed).\n  evidence: CI at `b6a271ed` fails at package validation (`Hexalith.Tenants.Server` → forbidden `Hexalith.EventStore.ServiceDefaults`) before Tier 1. Story Guards run 37223763857 aborts at the first Chromium launch (`validate-tenants-focus-browser.sh:226`, exit 134). That is before the new recovery-focus, forced-colors and missing-tabindex checks (`:330-375`). This extends the 2026-09-29 \"Restore CI test execution\" entry and the 2026-10-03 Story 5.6 entries."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: The package-boundary blocker is resolved: current-head CI run 38269521776 build-and-test job 114880391875 succeeds. The independent browser defect remains verified: HEAD 7a2a126f Story Guards run 38269520326 job 114880028087 times out in profile-shipped with Chrome 153.0.8010.52, exit 124 after 30 seconds, before preview scenarios. Original observations are preserved but current applicability is now limited to hosted browser validation.
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: DW-464 hosted Chromium repair
next-action: Repair and rerun the pinned hosted Chromium launch and subsequent correction preview/negative controls; no further package-boundary repair is needed for this record.
related: DW-463,DW-464,DW-496,DW-555


## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 4 (2026-10-05)

Diff reviewed: Tenants `e3e3af7d..560ac28f` and EventStore `865cd9e4..979de6f3` (anchored at root `88602b9b`, EventStore `f9d7dde4`).

### DW-506: Show the global-administrator correction panel's delivery-retry label only when a retry is actually possible.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 4 (2026-10-05)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: Show the global-administrator correction panel's delivery-retry label only when a retry is actually possible.\n  evidence: `GlobalAdministratorCorrectionPanel.razor:469` (`RefreshActionText`) picks the delivery-retry text whenever `IsSubmissionAmbiguous && CorrelationId: null`, even when `CanRetryAmbiguousCorrectionDelivery` is false, and uses a null-only pattern. Story 5.6 pass 3 fixed the same defect in `CorrectionStartPanel` (`CanRefresh && no correlation`). Global-administrator correction belongs to Story 5.7."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: GlobalAdministratorCorrectionPanel.razor:524-531 RefreshActionText still chooses delivery-retry solely for IsSubmissionAmbiguous && CorrelationId:null, without CanRetryAmbiguousCorrectionDelivery or CanRefreshSnapshot. Admission/authority/viewport refusal can disable actual retry while copy offers it.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: Global correction recovery-label contract
next-action: Use the actual retry-eligibility predicate for retry-specific text and cover ambiguous-but-ineligible and blank-correlation states.
related: none


### DW-507: Document `retryable`, `recoveryReasonCode` and `drainAttemptCount` in the EventStore status API reference.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 4 (2026-10-05)"), 2026-10-12
location: references/Hexalith.EventStore/src/Hexalith.EventStore/Models/CommandStatusResponse.cs
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: Document `retryable`, `recoveryReasonCode` and `drainAttemptCount` in the EventStore status API reference.\n  evidence: `CommandStatusResponse` (`src/Hexalith.EventStore/Models/CommandStatusResponse.cs`) serializes all three, but `docs/reference/command-api.md:308-322` lists none of them. A consumer cannot tell that an automatic retry is still armed on `PublishFailed`. The fields arrived in EventStore `86308550`, before the story baseline."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: references/Hexalith.EventStore/src/Hexalith.EventStore/Models/CommandStatusResponse.cs:21-23 serializes Retryable, RecoveryReasonCode and DrainAttemptCount; docs/reference/command-api.md status-response field table at :317-325 still omits them.
verification-files: references/Hexalith.EventStore/src/Hexalith.EventStore/Models/CommandStatusResponse.cs
evidence-kind: verified-gap
priority: P2
owner: Hexalith.EventStore
dependencies: EventStore status documentation
next-action: Document all three advisory recovery fields, null/legacy behavior and polling implications in the status response reference.
related: DW-523


### DW-508: Declare the command-status endpoint's 409 in its OpenAPI metadata.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 4 (2026-10-05)"), 2026-10-12
location: references/Hexalith.EventStore/src/Hexalith.EventStore/Controllers/CommandStatusController.cs
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: Declare the command-status endpoint's 409 in its OpenAPI metadata.\n  evidence: `CommandStatusController.cs:58-64` declares 200/400/401/403/404/429 but not 409, although `CreateAmbiguityProblemDetails` returns 409 (`:133`, `:152-153`). `CommandDocumentationTransformer` does not add it either, so the generated OpenAPI disagrees with `command-api.md:389`."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: references/Hexalith.EventStore/src/Hexalith.EventStore/Controllers/CommandStatusController.cs:58-66 declares 200/400/401/403/404/429 response metadata but no 409. GetStatus returns CreateAmbiguityProblemDetails on tenant/correlation ambiguity and runtime returns 409.
verification-files: references/Hexalith.EventStore/src/Hexalith.EventStore/Controllers/CommandStatusController.cs
evidence-kind: verified-gap
priority: P2
owner: Hexalith.EventStore
dependencies: EventStore API metadata owner
next-action: Declare and verify the status endpoint’s 409 ProblemDetails response in generated OpenAPI.
related: none


### DW-509: Move the remaining EventStore docs from `/status/{correlationId}` to the messageId route.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 4 (2026-10-05)"), 2026-10-12
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: Move the remaining EventStore docs from `/status/{correlationId}` to the messageId route.\n  evidence: `docs/guides/security-model.md:209-210,280`, `docs/concepts/command-lifecycle.md:210`, `docs/getting-started/first-domain-service.md:277` and the brownfield docs still name the correlation ID as the status key. `CommandStatusController` routes `{messageId}`, and correlation lookup is a bounded compatibility path that can return 409."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: references/Hexalith.EventStore/docs/guides/security-model.md:209, docs/concepts/command-lifecycle.md:210, docs/getting-started/first-domain-service.md:277 and docs/brownfield/{api-contracts,architecture}.md still document status/{correlationId}; CommandStatusController routes {messageId} and correlation fallback can be ambiguous.
evidence-kind: verified-gap
priority: P2
owner: Hexalith.EventStore
dependencies: EventStore documentation owner
next-action: Update each remaining status-route example to MessageId-first guidance and explain bounded correlation compatibility.
related: none


## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 6 (2026-10-05)

Diff reviewed: Tenants `2306feba..2c04af17` and EventStore `f9d7dde4..7c3243e1` (rebased mid-review to `7dcc4756` on EventStore `origin/main`; root still pins `f9d7dde4`).

### DW-510: Finish converting the EventStore replay reference to messageId semantics.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 6 (2026-10-05)"), 2026-10-12
location: references/Hexalith.EventStore/src/Hexalith.EventStore/Controllers/ReplayController.cs
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: Finish converting the EventStore replay reference to messageId semantics.\n  evidence: `docs/reference/command-api.md:425` says `originalCorrelationId` \"matches the path parameter\", but `ReplayController` resolves the path value as a messageId first and returns `archivedCommand.CorrelationId ?? correlationId`. So they differ when a messageId is passed. The replay error table (`:448`) omits the correlation-ambiguity 409 that `ReplayController` returns. These rows predate the story; pass 4 converted only the polling guidance. Pass-12 BH7 reconfirmed this and notes that `OriginalCorrelationId` and `OriginalMessageId` are separate response fields; pass 13 folds that duplicate entry into this work item."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: references/Hexalith.EventStore/docs/reference/command-api.md:427 still says originalCorrelationId matches the replay path, and :450 lacks correlation-ambiguity 409. ReplayController.cs:264-266 returns distinct OriginalCorrelationId and OriginalMessageId after message-first lookup.
verification-files: references/Hexalith.EventStore/src/Hexalith.EventStore/Controllers/ReplayController.cs
evidence-kind: verified-gap
priority: P2
owner: Hexalith.EventStore
dependencies: EventStore replay API reference
next-action: Document separate replay MessageId/correlation fields and the identifier-ambiguity 409 outcome.
related: none


### DW-511: Make the command-status 409 ProblemDetails detail match every ambiguity it reports.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 6 (2026-10-05)"), 2026-10-12
location: references/Hexalith.EventStore/src/Hexalith.EventStore/Controllers/CommandStatusController.cs
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: Make the command-status 409 ProblemDetails detail match every ambiguity it reports.\n  evidence: `CommandStatusController.CreateAmbiguityProblemDetails` (`:209`) always says \"The correlation identifier maps to multiple commands. Query again using the command MessageId.\" It also serves the case where one messageId matches records in several authorized tenants or several legacy records (`:152-153`), where a messageId retry cannot help. Pass-12 BH9 additionally traces a direct message match in one tenant colliding with another tenant's correlation index; pass 13 folds that duplicate entry into this work item. The updated `command-api.md` 409 row already gives the general remediation; the runtime copy is pre-existing and EventStore-owned."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: references/Hexalith.EventStore/src/Hexalith.EventStore/Controllers/CommandStatusController.cs:241-248 always tells users to retry with MessageId although GetStatus also invokes it for cross-tenant direct-message collisions. Retrying the same MessageId cannot resolve every ambiguity.
verification-files: references/Hexalith.EventStore/src/Hexalith.EventStore/Controllers/CommandStatusController.cs
evidence-kind: verified-defect
priority: P2
owner: Hexalith.EventStore
dependencies: EventStore ambiguity contract
next-action: Replace runtime remediation with scope-aware general guidance and cover direct/direct and direct/correlation cross-tenant collisions.
related: DW-521,DW-522


## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 7 (2026-10-05)

Diff reviewed: Tenants `c0b6f16d..50fc6257` and EventStore `d48e1aeb..ad8fe3ba` (root `50fc6257` was pushed mid-review).

### DW-512: Pin the value, not just the presence, of the tenant correction delivery outcome passed to `EndDelivery`.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 7 (2026-10-05)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: Pin the value, not just the presence, of the tenant correction delivery outcome passed to `EndDelivery`.\n  evidence: `TenantCorrectionAttemptTracker.EndDelivery` (`src/Hexalith.Tenants.UI/State/TenantCommands/TenantCorrectionAttemptTracker.cs:126`) now requires an outcome, but passing `request` or `attempt.Snapshot` instead of `next`/`unavailable` at `CorrectionStartPanel.razor:868`/`:933` still compiles and passes. `SetRetainedSnapshot` re-applies the right snapshot one statement later and bUnit is single-threaded, so no test can see the window in which another panel's `TryStartRetry` could send a duplicate same-id delivery. Closing it needs a production change, such as having `SetRetainedSnapshot` render only what `EndDelivery` retained."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: TenantCorrectionAttemptTracker.EndDelivery retains the supplied outcome atomically, and current CorrectionStartPanel.razor:881,946 passes unavailable/next correctly. Immediately following SetRetainedSnapshot still calls TryUpdate(candidate), so a wrong EndDelivery argument can be repaired before rendered tests notice; no exact retained-outcome window test was found. This is a mutation/concurrency verification gap, not a reproduced current duplicate dispatch.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Controllable concurrent retry test seam
next-action: Add a deterministic tracker/consumer regression that observes the EndDelivery outcome before subsequent rendering/update; decide whether rendering should consume only the atomically retained result.
related: none


## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 9 (2026-10-05)

Diff reviewed: Tenants `e2c297b2..680bee32` and EventStore `738da5c9..0c6bb5c3`. During the review a peer rebased `0c6bb5c3` to `2f7e044b` (identical patch) and re-pinned the root in `c3234b10`, then `6b6338f0`.

### DW-513: Correct the EventStore `failureReason` row, which says "PublishFailed status only".

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 9 (2026-10-05)"), 2026-10-12
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: Correct the EventStore `failureReason` row, which says \"PublishFailed status only\".\n  evidence: `docs/reference/command-api.md:325` dates from EventStore `6a901adf` (2026-03-01). `ConcurrencyConflictExceptionHandler.cs:51-60` and the `SubmitCommandHandler` coordinated-conflict writer put `FailureReason: \"ConcurrencyConflict\"` on `Rejected` records. Pre-existing; not caused by Story 5.6."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: references/Hexalith.EventStore/docs/reference/command-api.md:325 still describes failureReason as PublishFailed-only, while current concurrency-conflict paths write Rejected with FailureReason ConcurrencyConflict (reference docs :207 already acknowledge it).
evidence-kind: verified-gap
priority: P2
owner: Hexalith.EventStore
dependencies: EventStore status API reference
next-action: Correct the status field table to include concurrency-conflict rejection reasons and distinguish domain rejection markers.
related: none


### DW-514: Normalize whitespace causation in `AggregateActor.CreateCommandProcessingIdentity` (medium, unverified).

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 9 (2026-10-05)"), 2026-10-12
location: references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/AggregateActor.cs
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: Normalize whitespace causation in `AggregateActor.CreateCommandProcessingIdentity` (medium, unverified).\n  evidence: `CreateCommandProcessingIdentity` (`AggregateActor.cs:4704-4710`) maps only `null` to `MessageId`, while admission (`:652`) also maps whitespace. On the resume publish-failure path (`:4346`), a whitespace `CausationId` makes `IdempotencyChecker` → `identity.Validate()` throw before the drain record and reminder are written. The same call already existed at the EventStore story baseline `2c58ffda`. HTTP always sets causation = messageId. To settle it, list every non-HTTP entry point that builds a `CommandEnvelope` with a caller-supplied `CausationId`."
status: open
disposition: uncertain
verification-date: 2026-10-12
verification: references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/AggregateActor.cs:5151-5158 CreateCommandProcessingIdentity still maps only null CausationId to MessageId; admission at :669 normalizes whitespace too. HTTP initializes causation from MessageId, and no complete inventory/test proves a supported non-HTTP whitespace causation can reach failing resume recovery.
verification-files: references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/AggregateActor.cs
evidence-kind: unverified-risk
priority: P2
owner: Hexalith.EventStore
dependencies: Non-HTTP command-entry-point reachability evidence
next-action: Inventory non-HTTP CommandEnvelope producers and reproduce a supported whitespace-causation resume before deciding normalization/rejection behavior.
related: none


### DW-515: Give the EventStore shared-consumer authority Contracts test more headroom, or speed it up.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 9 (2026-10-05)"), 2026-10-12
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
priority: P2
archived: 2026-10-12

## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 11 (2026-10-05)

Diff reviewed: the uncommitted Tenants working tree against root `d8558263`, and EventStore `46d7b2eb..8f34b395` (story files only).

### DW-516: Carried, with no new work item; see "Restore CI test execution" above. Tenants CI never runs the strengthened same-row correction regression `RoleSelectionAndAnotherStartPreserveASubmittedPreviewAndItsTrackingHandle`.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 11 (2026-10-05)"), 2026-10-12
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
priority: P1
archived: 2026-10-12

## Deferred from: pass 12 workflow review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md (2026-10-05)

Diff reviewed: `/tmp/tenants-56-pass11-review-content-kr6d6_ah.diff` (699,840 bytes), containing the original Tenants story-baseline diff and the EventStore proof-path delta from its preserved `b51978dd` baseline. The claims spec was excluded and supplied separately only to the edge layer. Pass-12 BH7 and BH9 extend the pass-6 replay-reference and runtime-ambiguity work items above; pass 13 removed their duplicate entries here.

### DW-517: Restore content scanning or governed, content-bound retirement for EventStore evidence paths currently skipped by the secret guard (high).

status: done 2026-10-14
origin: migrated from unnumbered ledger ("Deferred from: pass 12 workflow review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md (2026-10-05)"), 2026-10-12
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
priority: P1
archived: 2026-10-15

### DW-518: Correct EventStore command-status documentation to include positive event counts on eventful rejections.

origin: migrated from unnumbered ledger ("Deferred from: pass 12 workflow review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md (2026-10-05)"), 2026-10-12
location: n/a
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: Correct EventStore command-status documentation to include positive event counts on eventful rejections.\n  evidence: Pass-12 BH6: command-api.md says eventCount is Completed-only, but CompleteTerminalAsync passes a positive count for Rejected. Both wording and behavior predate the preserved EventStore baseline; correction confirmation remains gated on Completed and verified proof."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: references/Hexalith.EventStore/docs/reference/command-api.md:322 still says eventCount is Completed-only; AggregateActor.CompleteTerminalAsync at :6725-6729 passes positive counts on eventful Rejected outcomes as well.
evidence-kind: verified-gap
priority: P2
owner: Hexalith.EventStore
dependencies: EventStore status API reference
next-action: Document positive event counts on eventful rejections and preserve the separate Completed-only correction-proof contract.
related: DW-507,DW-513


### DW-519: Add real AggregateActor.GetEventsAsync count and cumulative-payload budget boundary tests.

origin: migrated from unnumbered ledger ("Deferred from: pass 12 workflow review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md (2026-10-05)"), 2026-10-12
location: references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/AggregateActor.cs
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: Add real AggregateActor.GetEventsAsync count and cumulative-payload budget boundary tests.\n  evidence: Pass-12 VG1: pre-verified gap in independent EventStore evolution commit 974a7fa33d70fdfb9dabb32c7d2416e4f0095853. AggregateActorGetEventsTests uses small arrays, projection tests mock the actor, and the only LegacyArrayLimit assertion calls EventStreamReader.RehydrateAsync. Invoke the real actor for excessive count and cumulative 64-MiB payload cases and require LegacyArrayLimit, so deleting arrayBudget.Add is detected."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/AggregateActor.cs:2072 applies arrayBudget.Add; AggregateActorGetEventsTests tests metadata failures and small arrays (32/33-event batch boundary cases) but no excessive-count or cumulative-64-MiB actor-boundary case. Legacy reader boundary coverage does not exercise this actor loop.
verification-files: references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/AggregateActor.cs
evidence-kind: verified-gap
priority: P2
owner: Hexalith.EventStore
dependencies: EventStore actor test fixtures
next-action: Invoke real AggregateActor.GetEventsAsync at count/payload boundaries and assert LegacyArrayLimit, including an arrayBudget.Add removal control.
related: none


## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 13 (2026-10-05)

Diff reviewed: Tenants `d8558263..5a519cd7`, and EventStore `8f34b395..55b2982e` (story file `docs/reference/command-api.md` only).

### DW-520: Teach `scripts/validate-story-gitlinks.py` to bind three-column dependency-table rows, or document that tables are non-binding.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 13 (2026-10-05)"), 2026-10-12
location: scripts/validate-story-gitlinks.py
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: Teach `scripts/validate-story-gitlinks.py` to bind three-column dependency-table rows, or document that tables are non-binding.\n  evidence: `stated_targets()` (`:208`) matches only `ARROW_CHAIN` (`X -> Y`). For the 5.6 spec at `5a519cd7` it returns `{}`, so the run PASSes while four `| references/X | base | target |` rows are stale (EventStore `8f34b395` vs shipped `55b2982e`, FrontComposer, McpCli, Memories). The docstring says a stale pointer table is exactly what the guard catches. `tests/scripts/test_validate_story_gitlinks.py` has no pipe-table fixture. Add one that expects `[MISSTATED]`. Pre-existing; not caused by Story 5.6."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: Executed an import-only parser probe: stated_targets on a File List row “| references/Hexalith.Builds | 1111111 | 2222222 |” returned {}; the arrow form returned target 2222222. scripts/validate-story-gitlinks.py:208-256 remains ARROW_CHAIN-only, so stale three-column targets are not bound.
verification-files: scripts/validate-story-gitlinks.py
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: Gitlink declaration format policy
next-action: Support three-column base/target rows with a negative MISSTATED fixture, or explicitly declare and enforce their nonbinding status.
related: DW-534


### DW-521: A caller-chosen correlation ID in one tenant can force a 409 on another tenant's `messageId` status lookup.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 13 (2026-10-05)"), 2026-10-12
location: references/Hexalith.EventStore/src/Hexalith.EventStore/Controllers/CommandStatusController.cs
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: A caller-chosen correlation ID in one tenant can force a 409 on another tenant's `messageId` status lookup.\n  evidence: `CommandStatusController.GetStatus` (`:114-153`) counts a direct match in tenant A and an indexed correlation match in tenant B together, so `matches.Count == 2` returns 409. The body `correlationId` is caller-chosen, and the endpoint has no tenant selector. Tenants correction confirmation fails closed (unable to verify), so the impact is availability for multi-tenant callers. The `:3576` entry tracks only the wording, not the lookup behaviour. Pre-existing (`ddccb9b1`). EventStore-owned."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: references/Hexalith.EventStore/src/Hexalith.EventStore/Controllers/CommandStatusController.cs:114-156 counts exact MessageId in one authorized tenant and correlation-index resolution in another together, then returns 409 for multiple matches. docs/reference/command-api.md now documents this behavior, but its availability consequence for caller-chosen correlations remains.
verification-files: references/Hexalith.EventStore/src/Hexalith.EventStore/Controllers/CommandStatusController.cs
evidence-kind: verified-defect
priority: P2
owner: Hexalith.EventStore
dependencies: EventStore lookup compatibility decision
next-action: Choose explicit message-primary versus compatibility lookup semantics across authorized tenants and test caller-chosen cross-tenant collisions.
related: DW-511,DW-522


### DW-522: Extends the pass-6 entry "Make the command-status 409 ProblemDetails detail match every ambiguity it reports": the served error catalog has the same wrong copy.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 13 (2026-10-05)"), 2026-10-12
location: references/Hexalith.EventStore/src/Hexalith.EventStore/OpenApi/ErrorReferenceEndpoints.cs
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: Extends the pass-6 entry \"Make the command-status 409 ProblemDetails detail match every ambiguity it reports\": the served error catalog has the same wrong copy.\n  evidence: `src/Hexalith.EventStore/OpenApi/ErrorReferenceEndpoints.cs:94-97` describes `command-correlation-ambiguous` as \"The tenant-scoped correlation identifier maps to multiple live commands\" and remediates with \"Use the MessageId returned by command submission\". Neither covers a cross-tenant `messageId` collision. `docs/reference/problems/` has no page for this type. Pre-existing. EventStore-owned."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: references/Hexalith.EventStore/src/Hexalith.EventStore/OpenApi/ErrorReferenceEndpoints.cs:94-97 still describes tenant-scoped correlation collisions only and says use MessageId. The shared 409 also covers direct MessageId collisions across authorized tenants; no specific docs/reference/problems page was found.
verification-files: references/Hexalith.EventStore/src/Hexalith.EventStore/OpenApi/ErrorReferenceEndpoints.cs
evidence-kind: verified-defect
priority: P2
owner: Hexalith.EventStore
dependencies: DW-511 general ambiguity wording
next-action: Align the served ambiguity catalog and problem reference with the general scope-aware runtime remediation.
related: DW-511,DW-521


## Deferred from: pass 14 workflow review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md (2026-10-05)

Diff reviewed: `/tmp/tenants-56-review-content-3sylhkc4.diff`, 626,873 bytes; root `aeb23ad1` = `f9dc75c1`; EventStore `55b2982e` story paths.

### DW-523: Correct EventStore polling guidance that treats recoverable PublishFailed as terminal.

origin: migrated from unnumbered ledger ("Deferred from: pass 14 workflow review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md (2026-10-05)"), 2026-10-12
location: references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Commands/CommandStatusExtensions.cs; references/Hexalith.EventStore/src/Hexalith.EventStore/Controllers/CommandStatusController.cs
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: Correct EventStore polling guidance that treats recoverable PublishFailed as terminal.\n  evidence: Pass-14 BH10, extended by pass 17 at EventStore `07d1e23a6c5b06bbbb1fc8ddb5174cc3382d3d93`: command-api.md labels PublishFailed terminal at :380, :383, :400 (replay of terminal failure states), :470, :473, :475, and :477; docs/brownfield/architecture.md:152 and docs/brownfield/api-contracts.md:23 make the same claim. The runtime CommandStatusExtensions.IsTerminal also classifies PublishFailed as terminal, and CommandStatusController.cs:201-202 therefore omits Retry-After. Line :477 tells callers to stop polling, although a successful publication drain can later write Completed with command-specific sequence proof. Correcting :477 alone leaves the other documentation sites and runtime polling behavior inconsistent. Story 5.6's consumer maps PublishFailed to Degraded with refresh recovery, so it remains pending for verification rather than claiming correction success. The identical terminal-state sentence exists at preserved EventStore baseline b51978dd1d2a3721ad239db2623e1560377c7583:454, so this is pre-existing EventStore documentation work. Distinguish recoverable publication failure from exhausted recovery without duplicating the existing recovery-field documentation follow-up."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: references/Hexalith.EventStore/docs/reference/command-api.md:380,383,400,470,473,475,477 and brownfield docs still call PublishFailed terminal. CommandStatusExtensions.cs IsTerminal returns status >= Completed, and CommandStatusController.cs:231-235 omits Retry-After for it despite recoverable publication drain later reaching Completed.
verification-files: references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Commands/CommandStatusExtensions.cs; references/Hexalith.EventStore/src/Hexalith.EventStore/Controllers/CommandStatusController.cs
evidence-kind: verified-defect
priority: P2
owner: Hexalith.EventStore
dependencies: EventStore publication recovery status contract
next-action: Define recoverable versus exhausted PublishFailed polling semantics and align runtime Retry-After plus all lifecycle/replay documentation.
related: DW-507


## Deferred from: spec-refresh-dependencies.md and spec-eventstore-3-117-1.md (2026-10-08)

Diff reviewed: the dependency refresh from its recorded baseline `8f6f8cb813255cc94ada95cda1a5e224c3b6bed0`, and the EventStore 3.117.1 continuation from `1846c7128cf0f6b16f9e62f38032bcab78e26e38`, with each spec's separate evidence/triage supplement. Both final evidence reports record root `1846c7128cf0f6b16f9e62f38032bcab78e26e38`; concurrent owning dependency checkouts are identified in those reports. This heading groups those existing findings, not a new combined review.

### DW-524: medium; tenant index replay can overwrite newer index values while detail skips older sequences.

origin: migrated from unnumbered ledger ("Deferred from: spec-refresh-dependencies.md and spec-eventstore-3-117-1.md (2026-10-08)"), 2026-10-12
location: src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs; src/Hexalith.Tenants.Server/Projections/TenantIndexReadModel.cs
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-refresh-dependencies.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-refresh-dependencies.md`\n  summary: medium; tenant index replay can overwrite newer index values while detail skips older sequences.\n  evidence: BH1: TenantProjectionHandler.cs:156 has no per-aggregate sequence watermark; TenantIndexReadModel.Apply(TenantUpdated) overwrites Name. The older-replay test checks only detail against an empty index. This predates the EventStore upgrade; add persisted-index replay coverage when addressing the index watermark."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs:154-168 applies every replayed event to the singleton index without an aggregate watermark; TenantIndexReadModel.cs:24-29 overwrites Name. The detail path checks persisted sequence at :102-132, but the older-replay test does not seed and assert a newer persisted index.
verification-files: src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs; src/Hexalith.Tenants.Server/Projections/TenantIndexReadModel.cs
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: Shared singleton-index sequence contract
next-action: Add per-aggregate index replay ordering and persisted-index end-state regression covering older TenantUpdated and membership/lifecycle events.
related: none


### DW-525: medium; the custom Tenants /project route bypasses legacy projection evolution admission.

origin: migrated from unnumbered ledger ("Deferred from: spec-refresh-dependencies.md and spec-eventstore-3-117-1.md (2026-10-08)"), 2026-10-12
location: src/Hexalith.Tenants/Projections/ProjectionDispatcher.cs
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-refresh-dependencies.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-refresh-dependencies.md`\n  summary: medium; the custom Tenants /project route bypasses legacy projection evolution admission.\n  evidence: BH2: Program.cs:173 calls the Tenants ProjectionDispatcher, which lacks admission checks before persistence. Published EventStore 3.115.0 already includes RequireLegacy in source revision 283b07a52c9c70e1c940164a7011ee8c3ad98b2d, so this is pre-existing. Restore equivalent rejection of unverified hints/versioned event metadata with route-level coverage."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants/Program.cs:166-173 uses the domain ProjectionDispatcher for /project; src/Hexalith.Tenants/Projections/ProjectionDispatcher.cs:50-107 invokes handlers without RequireLegacy/evolution admission. It validates global identity and authentication separately but lacks the platform’s unverified-hint/versioned-event rejection before persistence.
verification-files: src/Hexalith.Tenants/Projections/ProjectionDispatcher.cs
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: EventStore legacy projection admission contract
next-action: Add equivalent fail-closed projection evolution admission at this custom route and route-level no-persistence rejection coverage.
related: none


### DW-526: medium; recorded gitlink provenance rejects a valid new submodule addition.

origin: migrated from unnumbered ledger ("Deferred from: spec-refresh-dependencies.md and spec-eventstore-3-117-1.md (2026-10-08)"), 2026-10-12
location: scripts/validate-story-gitlinks.py
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-refresh-dependencies.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-refresh-dependencies.md`\n  summary: medium; recorded gitlink provenance rejects a valid new submodule addition.\n  evidence: BH4: validate-story-gitlinks.py:428 compares current_pointer(..., baseline), which returns None for an absent pointer, to the raw addition old SHA of forty zeroes. Normalize missing pointers and cover additions/removals; this branch predates the dependency continuation."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: scripts/validate-story-gitlinks.py:428-429 compares current_pointer(path, baseline), which is None when absent, directly with the raw old SHA for an added gitlink (all zeroes). The recorded-story-owned path therefore reports CHAIN GAP on a valid new submodule addition.
verification-files: scripts/validate-story-gitlinks.py
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: Gitlink recorded-provenance tests
next-action: Normalize absent/null pointer representations and add valid addition/removal CLI cases.
related: DW-531


### DW-527: medium; NuGet boundary validation cannot detect mismatched dependency versions or framework groups.

origin: migrated from unnumbered ledger ("Deferred from: spec-refresh-dependencies.md and spec-eventstore-3-117-1.md (2026-10-08)"), 2026-10-12
location: scripts/validate-nuget-packages.py
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-refresh-dependencies.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-refresh-dependencies.md`\n  summary: medium; NuGet boundary validation cannot detect mismatched dependency versions or framework groups.\n  evidence: BH5: scripts/validate-nuget-packages.py reduces restore and nuspec dependencies to ID sets. An isolated nuspec containing EventStore [3.115.0] in net9.0 passes with the expected package ID. Preserve framework/version evidence and add negative fixtures in the owning release-validator work; the implementation predates this upgrade."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: scripts/validate-nuget-packages.py PackageMetadata.dependencies and load_restore_dependencies return frozenset IDs; nuspec comparison similarly flattens dependency IDs and drops versions/framework grouping. Restore structure is validated, but wrong nuspec version/framework can still share the accepted ID boundary.
verification-files: scripts/validate-nuget-packages.py
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Release consumer-boundary schema
next-action: Preserve and compare dependency version ranges and target-framework groups with negative isolated nuspec fixtures.
related: DW-544,DW-554


### DW-528: medium; bootstrap token responses are buffered before the intended 64 KiB bound is enforced.

origin: migrated from unnumbered ledger ("Deferred from: spec-refresh-dependencies.md and spec-eventstore-3-117-1.md (2026-10-08)"), 2026-10-12
location: src/Hexalith.Tenants/Bootstrap/TenantBootstrapCredentialProvider.cs; src/Hexalith.Tenants/Bootstrap/TenantBootstrapHostedService.cs
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-refresh-dependencies.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-refresh-dependencies.md`\n  summary: medium; bootstrap token responses are buffered before the intended 64 KiB bound is enforced.\n  evidence: BH7: TenantBootstrapCredentialProvider.cs:86 uses buffered PostAsync, then calls LoadIntoBufferAsync with 64 KiB; TenantBootstrapHostedService creates a default HttpClient. An oversized authority response consumes memory before this cap. Use headers-first bounded reading and an oversized-response test in the pre-existing bootstrap flow."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants/Bootstrap/TenantBootstrapCredentialProvider.cs:85-93 uses buffered HttpClient.PostAsync before LoadIntoBufferAsync(MaxTokenResponseBytes). TenantBootstrapHostedService.cs:95 creates a default client, so oversized response buffering precedes the intended 64-KiB check; current bootstrap tests lack oversized authority responses.
verification-files: src/Hexalith.Tenants/Bootstrap/TenantBootstrapCredentialProvider.cs; src/Hexalith.Tenants/Bootstrap/TenantBootstrapHostedService.cs
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: Bootstrap authority response contract
next-action: Use headers-first bounded token response reading and add an oversized authority-response test.
related: DW-542


### DW-529: medium; performance evidence records root HEAD while measuring an unrestricted working tree.

origin: migrated from unnumbered ledger ("Deferred from: spec-refresh-dependencies.md and spec-eventstore-3-117-1.md (2026-10-08)"), 2026-10-12
location: scripts/run-tenant-audit-performance.sh
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-refresh-dependencies.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-refresh-dependencies.md`\n  summary: medium; performance evidence records root HEAD while measuring an unrestricted working tree.\n  evidence: BH8: scripts/run-tenant-audit-performance.sh:114 records gitRevision, then builds local files without a clean-tree guard or source/submodule snapshot. Distinct measured candidates can share the same revision. Capture exact source provenance or require cleanliness; this is pre-existing and does not invalidate the separately evidenced clean historical run."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: scripts/run-tenant-audit-performance.sh:114 records git rev-parse HEAD while :157-160 builds the working tree; no clean-tree guard, diff digest or submodule/source snapshot is collected. Uncommitted measured candidates can retain the same recorded revision.
verification-files: scripts/run-tenant-audit-performance.sh
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Performance evidence provenance contract
next-action: Capture exact root/submodule source provenance or fail preflight on a dirty tree before claiming a revision-bound performance result.
related: none


### DW-530: low; command models violate the required one-C#-type-per-file authoring rule.

origin: migrated from unnumbered ledger ("Deferred from: spec-refresh-dependencies.md and spec-eventstore-3-117-1.md (2026-10-08)"), 2026-10-12
location: src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-refresh-dependencies.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-refresh-dependencies.md`\n  summary: low; command models violate the required one-C#-type-per-file authoring rule.\n  evidence: BH10: TenantCreateCommandModels.cs contains fourteen public records/enums for create, membership, metadata and lifecycle flows. The required Hexalith baseline explicitly requires separate type-named files. Split mechanically in the owning command-flow change; no new product code was introduced by this dependency upgrade."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs still declares fourteen public types (records/enums at lines 23,27,36,51,60,74,79,134,171,432,614,857,1241,1524), contrary to the required Hexalith one-type-per-file authoring rule.
verification-files: src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs
evidence-kind: verified-gap
priority: P3
owner: unassigned
dependencies: none
next-action: Split existing command-model types into their matching type-named files in a separate mechanical maintenance change.
related: DW-479
duplicate-of: DW-479


### DW-531: medium; recorded gitlink continuity lacks a regression that detects removal of the chain check.

origin: migrated from unnumbered ledger ("Deferred from: spec-refresh-dependencies.md and spec-eventstore-3-117-1.md (2026-10-08)"), 2026-10-12
location: tests/scripts/test_validate_story_gitlinks.py; scripts/validate-story-gitlinks.py
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-refresh-dependencies.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-refresh-dependencies.md`\n  summary: medium; recorded gitlink continuity lacks a regression that detects removal of the chain check.\n  evidence: VG1, pre-verified: the reviewer disabled only [CHAIN GAP] in memory and all 23 guard tests still passed. Add CLI cases for a valid two-commit pointer chain and an omitted intermediate pointer-changing commit. The guard/tests predate this dependency continuation."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: tests/scripts/test_validate_story_gitlinks.py covers parser and synthetic default CLI behavior but no recorded Story-Owned Gitlink Commits two-commit continuity or missing intermediate commit fixture; the CHAIN GAP branch at scripts/validate-story-gitlinks.py:428-432 remains unpinned. The historic mutation result is retained, not rerun here.
verification-files: tests/scripts/test_validate_story_gitlinks.py; scripts/validate-story-gitlinks.py
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Recorded-provenance synthetic repo fixture
next-action: Add CLI cases proving a valid recorded pointer chain passes and omitting an intermediate pointer-changing commit fails CHAIN GAP.
related: DW-526


### DW-532: Clarify the non-EventStore preservation statement as structural equality of parsed rows rather than byte equality.

origin: migrated from unnumbered ledger ("Deferred from: spec-refresh-dependencies.md and spec-eventstore-3-117-1.md (2026-10-08)"), 2026-10-12
location: _bmad-output/implementation-artifacts/spec-eventstore-3-117-1.md
source_spec: `_bmad-output/implementation-artifacts/spec-eventstore-3-117-1.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-eventstore-3-117-1.md`\n  summary: Clarify the non-EventStore preservation statement as structural equality of parsed rows rather than byte equality.\n  evidence: The refresh verification compares JSON package/family lists with Python equality; separate catalog byte/hash checks prove file-byte preservation. The focused review's R7 records this distinction and the one-shot workflow routes fixes to specs into deferred work."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: _bmad-output/implementation-artifacts/spec-eventstore-3-117-1.md:46 still says non-EventStore rows are “byte-for-byte equivalent as parsed data”; R7 at :90 explains actual Python structural equality and separate catalog byte hashes. The historical sentence remains ambiguous despite the preserved review clarification.
verification-files: _bmad-output/implementation-artifacts/spec-eventstore-3-117-1.md
evidence-kind: verified-gap
priority: P3
owner: unassigned
dependencies: Historical evidence wording annotation
next-action: Add a concise dated annotation distinguishing parsed-row structural equality from the independent catalog byte/hash guarantee.
related: none


## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 15 (2026-10-08)

Diff reviewed: Tenants `bfaa770e` + `5bfe0715` (story files only; HEAD `5bfe0715` = `origin/main`).

### DW-533: medium; restore the Epic 5 rules that the 2026-10-08 regeneration dropped from `epic-5-context.md`, before Story 5.7 is specified.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 15 (2026-10-08)"), 2026-10-12
location: _bmad-output/implementation-artifacts/epic-5-context.md
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: medium; restore the Epic 5 rules that the 2026-10-08 regeneration dropped from `epic-5-context.md`, before Story 5.7 is specified.\n  evidence: `5bfe0715` rewrote the context 9,595 → 6,688 bytes. It removed \"never announce success before projection confirm\" (`architecture.md:691`), McpCli-only CLI/MCP exposure (`architecture.md:66`), \"5.7a/5.7b, complete only as a whole\" (`epics.md:2671`), the `LastGlobalAdministrator` hard stop (`epics.md:2730`), the exclusive Set/RemoveGlobalAdministrator mapping, the `/api/global-administrators` read, the availability severity styling and \"seven fields only\" receipts, and it vaguened the recovery-verb, origin-state and entry-point lists. The R1–R4 additions come from the 5.5/5.6 specs, not the planning docs, so the next `compile-epic-context` run drops them again. Restore the `1cdcc0a9` content and add R1–R4 with source citations."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: _bmad-output/implementation-artifacts/epic-5-context.md still omits McpCli-only exposure, explicit Set/Remove command naming, global-administrator read route, named LastGlobalAdministrator rejection and 5.7a/5.7b whole-story rule; it retains generic last-administrator blocking and seven receipt facts. Story 5.7 is now done, so this no longer blocks specification, but the generated context still incompletely carries authoritative planning constraints.
verification-files: _bmad-output/implementation-artifacts/epic-5-context.md
evidence-kind: verified-gap
priority: P3
owner: unassigned
dependencies: Authoritative planning context sources
next-action: Annotate the context’s historical/current scope and reconcile omitted authoritative constraints and citations before future regeneration or reuse.
related: none


### DW-534: Carried, with no new work item; see DW-162. CI still never runs `validate-story-gitlinks.py` against real story files.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 15 (2026-10-08)"), 2026-10-12
location: .github/workflows/story-guards.yml; tests/scripts/test_validate_story_gitlinks.py
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: Carried, with no new work item; see DW-162. CI still never runs `validate-story-gitlinks.py` against real story files.\n  evidence: `.github/workflows/story-guards.yml:34` runs only `tests/scripts/test_validate_story_gitlinks.py` on synthetic repositories. `5bfe0715` reached `main` green while the guard exits 1 on both 5.6 specs (Memories `c4697367`, Platform `495d1d0d`). 10th recurrence of the silent-bump pattern."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: .github/workflows/story-guards.yml:34 runs only tests/scripts/test_validate_story_gitlinks.py; no step invokes the production guard against real selected story artifacts. Current historical 5.6-2 guard execution still reports undeclared/misstated pointers, demonstrating synthetic regression success is not actual artifact validation.
verification-files: .github/workflows/story-guards.yml; tests/scripts/test_validate_story_gitlinks.py
evidence-kind: verified-gap
priority: P1
owner: unassigned
dependencies: DW-198 historical story-range selection; DW-520 declaration format
next-action: Wire production story-range validation for the actual change scope, using durable story-end/provenance rules that avoid treating later unrelated dependency changes as story-owned.
related: DW-162,DW-198,DW-520,DW-549
duplicate-of: DW-437


## Deferred from: code review of spec-gh-actions-28953291798-85906522208.md (2026-10-08)

Diff reviewed: `tests/Hexalith.Tenants.IntegrationTests/TenantsUiRouteSmokeTests.cs` at `3d96d0aa..HEAD` (`5bfe0715`). The spec's own change is `2e82d0d6`; these entries come from later commits to the same file or from pre-existing CI state.

### DW-535: medium; no blocking test asserts that the audit Unauthorized state is announced with `role="alert"` and `aria-live="assertive"`.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-gh-actions-28953291798-85906522208.md (2026-10-08)"), 2026-10-12
location: tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs
source_spec: `_bmad-output/implementation-artifacts/spec-gh-actions-28953291798-85906522208.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-gh-actions-28953291798-85906522208.md`\n  summary: medium; no blocking test asserts that the audit Unauthorized state is announced with `role=\"alert\"` and `aria-live=\"assertive\"`.\n  evidence: `96bdfd8a` removed `markup.ShouldContain(\"role=\\\"alert\\\"\")` from the audit route smoke test. The only Tier 1 check, `TenantAuditPageTests.Tenant_audit_page_renders_distinct_accessible_states` (`tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs:2108-2129`), asserts only `GetAttribute(\"role\").ShouldNotBeNull()`, so a regression of `TenantAuditPage.razor:632` `StatusRole` to `status`/`polite` passes everywhere. Fix: mirror `TenantDetailSurfaceTests.cs:2945-2947` and assert `alert` plus `assertive` for Unauthorized."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs:2767-2779 Tenant_audit_page_renders_distinct_accessible_states still asserts only nonnull role and nonempty live text, including Unauthorized. Current hosted route smoke asserts outer fc-scope-blocked and cannot cover page-level alert/assertive semantics.
verification-files: tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Audit component localizer/authorization fixture
next-action: Pin Unauthorized role=alert and aria-live=assertive in the blocking rendered component lane.
related: none


### DW-536: low; the localized detail Unauthorized title "Tenant detail unauthorized" is not pinned exactly by any test.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-gh-actions-28953291798-85906522208.md (2026-10-08)"), 2026-10-12
location: tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs
source_spec: `_bmad-output/implementation-artifacts/spec-gh-actions-28953291798-85906522208.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-gh-actions-28953291798-85906522208.md`\n  summary: low; the localized detail Unauthorized title \"Tenant detail unauthorized\" is not pinned exactly by any test.\n  evidence: The hosted smoke assertion was removed around `2e61f57b`/`62eb3607`. Tier 1 `Detail_page_renders_distinct_safe_states` (`TenantDetailSurfaceTests.cs:2923`) matches only the case-insensitive substring `\"authorized\"`, which an unresolved resx key name (`Tenants.Detail.State.Unauthorized.Title`) would also satisfy."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs:2920-2938 still uses expectedText “authorized” for Unauthorized rather than the exact localized title. The rewritten TenantsUiRouteSmokeTests only checks shell scope denial, so it provides no title fallback detection.
verification-files: tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs
evidence-kind: verified-gap
priority: P3
owner: unassigned
dependencies: Blocking detail component resource fixture
next-action: Add exact localized Unauthorized-title coverage with the real resources or a parity-aware localizer.
related: none


### DW-537: low; the detail smoke test asserts the unauthorized marker twice, and the comment above the duplicate describes a source-order form that never existed in this file.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-gh-actions-28953291798-85906522208.md (2026-10-08)"), 2026-10-12
source_spec: `_bmad-output/implementation-artifacts/spec-gh-actions-28953291798-85906522208.md`
priority: P3
archived: 2026-10-12

### DW-538: low; the audit smoke assertion "Return to tenant detail" cannot tell the focus-return hint from the NoFocus variant, and no test asserts the hint text.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-gh-actions-28953291798-85906522208.md (2026-10-08)"), 2026-10-12
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-gh-actions-28953291798-85906522208.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-gh-actions-28953291798-85906522208.md`\n  summary: low; the audit smoke assertion \"Return to tenant detail\" cannot tell the focus-return hint from the NoFocus variant, and no test asserts the hint text.\n  evidence: `TenantsResources.resx:3547-3552`: `Tenants.Audit.ReturnContext` = \"Return to {0}. The originating control will receive focus when available.\"; `.NoFocus` = \"Return to {0}.\" Both start with the asserted prefix. The test passes a `returnFocus`, so it should assert the full sentence. A search of `tests/Hexalith.Tenants.UI.Tests` finds the sentence only in a resource dictionary (`AuditEvidenceEntryPointTests.cs:514`), never in an assertion."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: The old hosted prefix assertion was removed by the shell-denial smoke rewrite. TenantAuditPageTests.Matching_list_anchor_and_explicit_return_focus_restore_the_audit_launcher now asserts the context contains “focus” (:2924), and Focusless_detail_return_preserves_a_stripped_cursor_notice asserts no “focus” (:2941), so variant differentiation exists. Exact full localized hint copy is still not pinned; this is a residual test-strength recommendation, not evidence of wrong current behavior.
evidence-kind: recommendation
priority: P3
owner: unassigned
dependencies: UX exact-copy test policy
next-action: Strengthen component assertions to the full approved localized focus-return sentence if exact copy is a maintained contract.
related: none


### DW-539: low; the user-lookup smoke comment says the compatibility route "redirects before issuing the lookup", but the code issues the lookup first.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-gh-actions-28953291798-85906522208.md (2026-10-08)"), 2026-10-12
source_spec: `_bmad-output/implementation-artifacts/spec-gh-actions-28953291798-85906522208.md`
priority: P3
archived: 2026-10-12

### DW-540: medium; Aspire-lane failures are hidden because the reusable `domain-ci.yml` defaults `aspire-continue-on-error` to true.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-gh-actions-28953291798-85906522208.md (2026-10-08)"), 2026-10-12
location: references/Hexalith.Builds/.github/workflows/domain-ci.yml
source_spec: `_bmad-output/implementation-artifacts/spec-gh-actions-28953291798-85906522208.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-gh-actions-28953291798-85906522208.md`\n  summary: medium; Aspire-lane failures are hidden because the reusable `domain-ci.yml` defaults `aspire-continue-on-error` to true.\n  evidence: Run 28953291798 concluded `success` although `ci / aspire-tests` failed; Tenants `ci.yml` does not override the default. A red hosted smoke test therefore never blocks a merge. Changing this is a workflow-definition decision (Ask First in the spec)."
status: open
disposition: awaiting-decision
verification-date: 2026-10-12
verification: references/Hexalith.Builds/.github/workflows/domain-ci.yml:109-114 still defaults aspire-continue-on-error to true and applies it to aspire-tests at :669; .github/workflows/ci.yml does not override the input. The lane remains advisory by workflow definition, independent of its latest health.
verification-files: references/Hexalith.Builds/.github/workflows/domain-ci.yml
evidence-kind: verified-gap
priority: P2
owner: Hexalith.Builds
dependencies: Maintainer CI gating decision
next-action: Record whether the existing Aspire lane must become blocking, and define the exact prerequisites/skip behavior before changing the workflow.
related: none


### DW-541: high; on `main` (`5bfe0715`, run 37782396626) `ci / build-and-test` fails, so `ci / aspire-tests` is skipped and the hosted smoke class never runs in CI.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-gh-actions-28953291798-85906522208.md (2026-10-08)"), 2026-10-12
source_spec: `_bmad-output/implementation-artifacts/spec-gh-actions-28953291798-85906522208.md`
priority: P1
archived: 2026-10-12

## Deferred from: pass 16 review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md (2026-10-08)

Diff reviewed: baseline `11e65e37f0fbf6649642a512052eebd37d50166d` through the pass-15 patches committed in `2b91b05be9c927319fd207b02552d1213997776b`; the spec retains the immutable review-input path and the 15-finding triage tally.

### DW-542: medium; bootstrap token retrieval buffers the response before applying its 64 KiB cap.

origin: migrated from unnumbered ledger ("Deferred from: pass 16 review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md (2026-10-08)"), 2026-10-12
location: src/Hexalith.Tenants/Bootstrap/TenantBootstrapCredentialProvider.cs; src/Hexalith.Tenants/Bootstrap/TenantBootstrapHostedService.cs
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: medium; bootstrap token retrieval buffers the response before applying its 64 KiB cap.\n  evidence: `TenantBootstrapCredentialProvider` calls `HttpClient.PostAsync` and only then `LoadIntoBufferAsync`. The password grant and subject check sit on that same path. This is outside the tenant-correction story."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants/Bootstrap/TenantBootstrapCredentialProvider.cs:85-93 uses buffered HttpClient.PostAsync before LoadIntoBufferAsync(MaxTokenResponseBytes). TenantBootstrapHostedService.cs:95 creates a default client, so oversized response buffering precedes the intended 64-KiB check; current bootstrap tests lack oversized authority responses.
verification-files: src/Hexalith.Tenants/Bootstrap/TenantBootstrapCredentialProvider.cs; src/Hexalith.Tenants/Bootstrap/TenantBootstrapHostedService.cs
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: Bootstrap authority response contract
next-action: Use headers-first bounded token response reading and add an oversized authority-response test.
related: DW-528
duplicate-of: DW-528


### DW-543: high claim superseded by pass-17 gateway tracing; see the medium command-API test-expectation defer below.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: pass 16 review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md (2026-10-08)"), 2026-10-12
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
priority: P1
archived: 2026-10-12

### DW-544: medium; NuGet validation allowlists two ServiceDefaults package ids at any version.

origin: migrated from unnumbered ledger ("Deferred from: pass 16 review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md (2026-10-08)"), 2026-10-12
location: scripts/validate-nuget-packages.py
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: medium; NuGet validation allowlists two ServiceDefaults package ids at any version.\n  evidence: `scripts/validate-nuget-packages.py` names `Hexalith.EventStore.ServiceDefaults` and `Hexalith.Commons.ServiceDefaults` before the `.ServiceDefaults` fragment check. A wrong version of those ids is a dependency-governance gap, not a correction-flow change."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: scripts/validate-nuget-packages.py:35-38 allows the two exact technical ServiceDefaults package IDs by name; PackageMetadata.dependencies stores IDs only. No version constraint attaches to those exceptions, so version mismatches remain undetectable by this validator.
verification-files: scripts/validate-nuget-packages.py
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: DW-527 version-aware dependency validation
next-action: Validate exception dependency versions against owning restore/catalog evidence and add wrong-version fixtures.
related: DW-527,DW-554


### DW-545: medium; duplicate global-administrator ids are dropped and the page is still marked complete.

origin: migrated from unnumbered ledger ("Deferred from: pass 16 review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md (2026-10-08)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Services/Gateways/GlobalAdministratorsProjectionLoader.cs
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: medium; duplicate global-administrator ids are dropped and the page is still marked complete.\n  evidence: `GlobalAdministratorsProjectionLoader` ignores a failed `TryAdd` and sets `IsCompleteEvidence`. Conflicting administrator rows can disappear from a ready page. That loader is the global-administrator read, not this correction."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Services/Gateways/GlobalAdministratorsProjectionLoader.cs:67 discards rows.TryAdd failure and :82 sets IsCompleteEvidence=true after page exhaustion. Conflicting duplicate UserId rows across stable pages are silently discarded; no duplicate-conflict fail-closed test was found.
verification-files: src/Hexalith.Tenants.UI/Services/Gateways/GlobalAdministratorsProjectionLoader.cs
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: Global-administrator projection completeness contract
next-action: Reject conflicting duplicate administrator rows as incomplete evidence and cover same/conflicting duplicates across cursor pages.
related: none


### DW-546: Story 5.7; loading may drop an in-flight global-administrator correction when that story mounts the panel. Not reachable on the tenant audit page since `10c9f6f6`; unverified, high if true.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: pass 16 review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md (2026-10-08)"), 2026-10-12
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
priority: P1
archived: 2026-10-12

### DW-547: resolved by `10c9f6f6`; the tenant audit page no longer opens or submits a global-administrator correction.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: pass 16 review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md (2026-10-08)"), 2026-10-12
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
priority: P2
archived: 2026-10-12

## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 17 (2026-10-08)

Diff reviewed: Tenants `fe5f6aa2..0ac7c126`, story files only (HEAD `0ac7c126` = `origin/main`), plus EventStore `9542d3c9..07d1e23a` story paths.

### DW-548: medium; `CommandApiRuntimeIntegrationTests.Commands_endpoint_rejects_client_supplied_globalAdmin_extension_metadata` expects `400`, but EventStore deliberately ignores the reserved key and returns `202`. The test is red in the masked `ci / aspire-tests` lane.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 17 (2026-10-08)"), 2026-10-12
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
priority: P2
archived: 2026-10-12

### DW-549: Carried, with no new work item; see DW-198. `validate-story-gitlinks.py` exits 1 on the historical reverification `-2.md` at HEAD.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 17 (2026-10-08)"), 2026-10-12
location: scripts/validate-story-gitlinks.py; _bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction-2.md
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction-2.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction-2.md`\n  summary: Carried, with no new work item; see DW-198. `validate-story-gitlinks.py` exits 1 on the historical reverification `-2.md` at HEAD.\n  evidence: At `0ac7c126` it reports 4× `[UNDECLARED]` (Builds, Commons, EventStore, McpCli) and 2× `[MISSTATED]` (Memories `906bc07a`, Platform `f5a0d72f`). The `-2.md` File List records `5bfe0715` values. `-3.md` calls `-2.md` historical, but the guard has no story-end ref, and `-3.md` will fail the same way at the next bump."
status: open
disposition: awaiting-decision
verification-date: 2026-10-12
verification: Executed PYTHONDONTWRITEBYTECODE=1 python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction-2.md: RESULT FAIL at current HEAD with five UNDECLARED later pointers and two MISSTATED targets (Memories f4c7d68 versus c469736; Platform 977f2e0 versus 495d1d0). The historical -2 artifact still lacks durable story-end selection, so later main changes contaminate revalidation.
verification-files: scripts/validate-story-gitlinks.py; _bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction-2.md
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: DW-198 historical story-range policy
next-action: Set a durable story-end/provenance convention for historical artifacts and have the guard validate that range without rewriting their original findings.
related: DW-198,DW-534
duplicate-of: DW-198


## Deferred from: Story 5.6 remediation workflow review, pass 18 (2026-10-08)

Diff reviewed: baseline `11e65e37` to the pre-patch working tree over root `5289b86b` (committed with its patches in `10c9f6f6`); input `/tmp/tenants-56-pass17-2kj37k80/pass18-reviewed-input.diff`, 2,657,085 bytes, SHA-256 `4692e95462d7000e9a3d850158858974fc05aef53e28434611ab4d14585330d3`, not archived. Triage: 13 findings, 2 patches, 6 defers (5 new, 1 carried), 5 rejected.

### DW-550: high; bootstrap administrator credential acquisition permits an HTTP authority outside Development.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: Story 5.6 remediation workflow review, pass 18 (2026-10-08)"), 2026-10-12
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
priority: P1
archived: 2026-10-13

### DW-551: medium; bootstrap authority mode hardcodes the Keycloak token endpoint despite its generic authority contract.

origin: migrated from unnumbered ledger ("Deferred from: Story 5.6 remediation workflow review, pass 18 (2026-10-08)"), 2026-10-12
location: src/Hexalith.Tenants/Bootstrap/TenantBootstrapCredentialProvider.cs
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: medium; bootstrap authority mode hardcodes the Keycloak token endpoint despite its generic authority contract.\n  evidence: `TenantBootstrapCredentialProvider` appends `/protocol/openid-connect/token`; an OIDC authority with a different advertised endpoint cannot bootstrap. The provider documentation does not restrict authority mode to Keycloak. Resolve or explicitly configure and validate the endpoint in separate bootstrap compatibility work."
status: open
disposition: awaiting-decision
verification-date: 2026-10-12
verification: TenantBootstrapCredentialProvider.cs:24-27 documents generic authority credential mode but :85 always appends /protocol/openid-connect/token; no discovery or configured token endpoint exists. A non-Keycloak authority cannot satisfy this undocumented endpoint assumption.
verification-files: src/Hexalith.Tenants/Bootstrap/TenantBootstrapCredentialProvider.cs
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Bootstrap authority compatibility decision
next-action: Choose a Keycloak-only documented contract or configurable/discovered token endpoint with validation and compatibility tests.
related: none


### DW-552: medium; partial-release recovery consumes temporary NuGet credential lifetime while preparing artifacts.

origin: migrated from unnumbered ledger ("Deferred from: Story 5.6 remediation workflow review, pass 18 (2026-10-08)"), 2026-10-12
location: .github/workflows/recover-partial-release.yml; scripts/publish-partial-release.sh
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: medium; partial-release recovery consumes temporary NuGet credential lifetime while preparing artifacts.\n  evidence: `recover-partial-release.yml` exchanges the documented one-hour key before `publish-partial-release.sh` builds, packs, validates, and compares package hashes. A long preparation leaves an expired key at publication. Prepare artifacts before exchanging credentials in the release recovery owner's work."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: .github/workflows/recover-partial-release.yml:70-85 obtains the temporary NuGet key before scripts/publish-partial-release.sh:14-26 builds, packs, validates and compares hashes. Unlike release.yml’s explicit late exchange, recovery spends credential lifetime on preparation. Actual expiry has not been reproduced, but the timing/provenance gap is present.
verification-files: .github/workflows/recover-partial-release.yml; scripts/publish-partial-release.sh
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Release recovery artifact-preparation boundary
next-action: Separate artifact preparation/hash verification from publication and exchange the temporary credential immediately before push.
related: none


### DW-553: medium; release credential exchange executes a mutable NuGet action tag with OIDC issuance permission.

origin: migrated from unnumbered ledger ("Deferred from: Story 5.6 remediation workflow review, pass 18 (2026-10-08)"), 2026-10-12
location: .github/workflows/release.yml; .github/workflows/recover-partial-release.yml
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: medium; release credential exchange executes a mutable NuGet action tag with OIDC issuance permission.\n  evidence: `release.yml` and `recover-partial-release.yml` invoke `NuGet/login@v1.2.0` in jobs with `id-token: write`. Moving that tag changes credential-exchange code independently of the reviewed source and Builds checkout identity. Pin the credential action to a reviewed commit in separate release governance work."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: .github/workflows/release.yml:425 and recover-partial-release.yml:72 still use mutable NuGet/login@v1.2.0 with id-token:write in their jobs. This is verified unpinned credential-exchange code; no compromised action or credential leak is established.
verification-files: .github/workflows/release.yml; .github/workflows/recover-partial-release.yml
evidence-kind: recommendation
priority: P2
owner: unassigned
dependencies: Reviewed credential-action revision
next-action: Resolve the reviewed NuGet/login action revision and pin both invocations to that commit under release governance.
related: none


### DW-554: medium; technical ServiceDefaults package exceptions are not scoped to published package layers.

origin: migrated from unnumbered ledger ("Deferred from: Story 5.6 remediation workflow review, pass 18 (2026-10-08)"), 2026-10-12
location: scripts/validate-nuget-packages.py
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: medium; technical ServiceDefaults package exceptions are not scoped to published package layers.\n  evidence: `scripts/validate-nuget-packages.py` allows both exact technical ServiceDefaults IDs for every package whose restore evidence includes them, including Contracts, Client, and Testing. Define and enforce the intended dependency layers in package architecture work; Story 5.6 adds executable exact-ID boundary coverage without changing that policy."
status: open
disposition: awaiting-decision
verification-date: 2026-10-12
verification: scripts/validate-nuget-packages.py applies exact ServiceDefaults ID allowlisting globally; load_dependency_boundaries keys packages but no exception policy distinguishes Server from Contracts/Client/Testing. The intended technical dependency layers are not specified by this validator.
verification-files: scripts/validate-nuget-packages.py
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Published package dependency-layer decision
next-action: Approve allowed published layers for each technical ServiceDefaults dependency, then enforce that policy with isolated package fixtures.
related: DW-527,DW-544


## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 19 (2026-10-08)

Diff reviewed: Tenants `5289b86b..10c9f6f6` (the pass-17 fix pass), HEAD `10c9f6f6` = `origin/main`. Triage: 39 raw findings → 1 decision (resolved into the first patch), 6 patches, 1 defer, 19 rejected.

### DW-555: medium; carried, updates the Chromium entry at `:3338`. Story Guards still fails at HEAD, but the 30-second bound now ends it at once instead of a 15-minute hang.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 19 (2026-10-08)"), 2026-10-12
location: tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: medium; carried, updates the Chromium entry at `:3338`. Story Guards still fails at HEAD, but the 30-second bound now ends it at once instead of a 15-minute hang.\n  evidence: Run 37843324360, job 113537923481, at `10c9f6f6`: \"Chromium scenario profile-shipped failed (exit 124; bound 30s)\" with Chrome for Testing 153.0.8010.52, preceded only by dbus connection errors. Since `9ee73062` added `--no-sandbox`, Chrome 153 hangs on its first DOM dump instead of aborting with exit 134, so the `:3338` and `:3508` descriptions are out of date. Repairing the CI Chromium launch remains runner work; the story's EN/FR browser evidence is local-only (Chrome 154)."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: Verified current-head hosted failure: HEAD 7a2a126f, Story Guards run 38269520326, job 114880028087 (https://github.com/Hexalith/Hexalith.Tenants/actions/runs/38269520326/job/114880028087) logs Chrome 153.0.8010.52 and profile-shipped exit 124 after the 30-second bound. /tmp/tenants-ledger-current-chromium-job.log preserves that evidence. Current validate-tenants-focus-browser.sh has --no-sandbox, timeout and stderr reporting, but the hosted launch still times out before later preview scenarios.
verification-files: tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: Hosted runner Chromium launch diagnosis
next-action: Diagnose the bounded profile-shipped timeout with the exact pinned Chrome 153 hosted runner configuration, then rerun all preview/negative-control scenarios.
related: DW-464,DW-496
duplicate-of: DW-464


## Deferred from: Story 5.6 pass-20 independent review (2026-10-09)

Diff reviewed: baseline `11e65e37` through the working tree over root `a0576d83` (committed with the pass-19 patches in `ded413df`); input `/tmp/tenants-56-diff-yWc71L.patch`, 3,161,925 bytes, SHA-256 `927090f9…`, not archived. Triage: 15 findings, 0 patches, 11 defers (2 new, 9 carried), 4 rejected.

### DW-556: medium; bootstrap does not retry transient credential or command failures until the host restarts.

origin: migrated from unnumbered ledger ("Deferred from: Story 5.6 pass-20 independent review (2026-10-09)"), 2026-10-12
location: src/Hexalith.Tenants/Bootstrap/TenantBootstrapHostedService.cs
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: medium; bootstrap does not retry transient credential or command failures until the host restarts.\n  evidence: `TenantBootstrapHostedService.StartAsync` schedules `RunBootstrapAsync` once on `ApplicationStarted`; the run returns after a credential failure, unexpected response, or caught exception. Its log says it retries on next restart. Define a bounded in-process retry or explicit operator recovery in the bootstrap owner's work."
status: open
disposition: awaiting-decision
verification-date: 2026-10-12
verification: TenantBootstrapHostedService.cs:62-63 schedules RunBootstrapAsync once after ApplicationStarted; credential/unexpected-response/exception paths return without in-process retry. Its failure log explicitly says retry on next restart. This is documented one-shot behavior, and no approved bounded retry or operator command policy was found.
verification-files: src/Hexalith.Tenants/Bootstrap/TenantBootstrapHostedService.cs
evidence-kind: recommendation
priority: P2
owner: unassigned
dependencies: Bootstrap recovery product/operator policy
next-action: Choose bounded in-process retry with identity-preserving delivery semantics or document an explicit operator recovery path.
related: none


### DW-557: medium; manually dispatched release tests pass when the NuGet login step is disabled.

origin: migrated from unnumbered ledger ("Deferred from: Story 5.6 pass-20 independent review (2026-10-09)"), 2026-10-12
location: .github/workflows/release.yml; .github/workflows/recover-partial-release.yml
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: medium; manually dispatched release tests pass when the NuGet login step is disabled.\n  evidence: `PackageGovernanceTests.Release_workflows_publish_to_nuget_through_trusted_publishing` checks login text and step order but not a disabling condition. A login step with `if: ${{ false }}` still passes the test; the dependent publish step receives an empty `NUGET_API_KEY` and aborts. Validate the executable login-to-publish handoff in release-owner work with the protected GitHub environment."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: PackageGovernanceTests.AssertTrustedPublishingJob still inspects login text/order without evaluating login-step if conditions; release.yml and recover-partial-release.yml use that handoff. Inspected, not mutation-tested this triage.
verification-files: .github/workflows/release.yml; .github/workflows/recover-partial-release.yml
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Protected publication test environment
next-action: Exercise enabled and disabled login-to-publish handoffs without publishing.
related: DW-566


## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 21 (2026-10-09)

Diff reviewed: Tenants `a0576d83..ded413df` (the pass-19 fix pass), HEAD `ded413df` = `origin/main`; 548-line scratch diff, SHA-256 `ce074e60…`, not archived. Triage: 40 normalized findings → 0 decisions, 4 patches, 2 defers, 23 rejected.

### DW-558: low; the paging spec still lists its reverted tenant-audit-page work as delivered.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 21 (2026-10-09)"), 2026-10-12
location: _bmad-output/implementation-artifacts/spec-global-admin-projection-paging.md; src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor
source_spec: `_bmad-output/implementation-artifacts/spec-global-admin-projection-paging.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-global-admin-projection-paging.md`\n  summary: low; the paging spec still lists its reverted tenant-audit-page work as delivered.\n  evidence: Task `:64` (`TenantAuditPage.razor` complete reads for initial enrichment, correction-open refresh and confirmation, plus refreshed-intent re-derivation) is `[x]`, and AC4 (`:75`) describes correction-open re-evaluation. Story 5.6 removed the audit-page branch in `10c9f6f6` and the enrichment in `ded413df`. The spec's own 2026-10-08 triage note explains this. Qualify `:64` and AC4 as reverted until Story 5.7 in that spec's review."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: spec-global-admin-projection-paging.md:64/75 still claims page delivery while its 2026-10-08 triage note says reverted/in-review. Story 5.7 now independently supplies the integration in TenantAuditPage.razor.
verification-files: _bmad-output/implementation-artifacts/spec-global-admin-projection-paging.md; src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor
evidence-kind: verified-gap
priority: P3
owner: unassigned
dependencies: Story 5.7 completion evidence
next-action: Reconcile the paging spec historical reversal with the final Story 5.7 ownership and verification.
related: DW-559


### DW-559: low; Story 5.7 handoff. Story 5.6 reverted the tenant audit page's global-administrator integration, and Story 5.7 must re-derive it.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 21 (2026-10-09)"), 2026-10-12
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
priority: P3
archived: 2026-10-12

## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 22 (2026-10-09)

Diff reviewed: baseline `11e65e37f0fbf6649642a512052eebd37d50166d` through the working tree over root `84c4e8b3dfd2fe651bac57bf89e2e988d998a7d2`; input `/tmp/tenants-56-baseline-k7kwjdc7.diff`, 3,178,732 bytes, SHA-256 `1f8675b41f9b4c0b6e2998083354297fe810c63485ab2220e5abb8a56d65d05c`, not archived. Triage: 14 findings, 0 patches, 3 new defers, 6 carried defers, 5 rejections.

### DW-560: medium; the release publish job grants OIDC issuance while dependency and build code runs before credential exchange.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 22 (2026-10-09)"), 2026-10-12
location: .github/workflows/release.yml
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: medium; the release publish job grants OIDC issuance while dependency and build code runs before credential exchange.\n  evidence: `release.yml` grants `id-token: write` at job scope (`:285`). Before `NuGet/login` (`:425`), the same job runs `npm ci` (`:369`), `dotnet restore` and `dotnet build` (`:372-375`, which execute restored packages' MSBuild targets), and the container-publisher preparation (`:381`). All of that code therefore executes with OIDC token-request permission. Separate dependency installation and build from publication authority in release-governance work."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: release.yml release job still grants id-token: write before npm ci, restore/build and container preparation. This verifies the authority boundary, not execution of malicious dependency code.
verification-files: .github/workflows/release.yml
evidence-kind: recommendation
priority: P2
owner: unassigned
dependencies: Release workflow ownership
next-action: Separate build/dependency execution from the job allowed to request publication OIDC tokens.
related: DW-563


### DW-561: medium; release jobs execute movable action tags beyond the previously recorded NuGet login action.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 22 (2026-10-09)"), 2026-10-12
location: .github/workflows/release.yml; .github/workflows/recover-partial-release.yml
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: medium; release jobs execute movable action tags beyond the previously recorded NuGet login action.\n  evidence: `release.yml` runs `actions/checkout@v7.0.1` (`:312`, `:318`), `actions/setup-dotnet@v6.0.0` (`:340`), `actions/setup-node@v7.0.0` (`:343`), `actions/cache@v6.1.0` (`:347`) and `actions/upload-artifact@v7.0.1` (`:438`) in a job with publication authority. `recover-partial-release.yml` grants `id-token: write` (`:32`) and runs `actions/checkout@v7.0.1` (`:44`) and `actions/setup-dotnet@v6.0.0` (`:50`). Version tags can move without a reviewed workflow change. Pin reviewed action revisions in release-governance work; the prior ledger entry covers `NuGet/login` specifically."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: release.yml and recover-partial-release.yml still use movable checkout/setup-dotnet/setup-node/cache/upload-artifact tags in publication-authorized jobs; no exploit is established.
verification-files: .github/workflows/release.yml; .github/workflows/recover-partial-release.yml
evidence-kind: recommendation
priority: P2
owner: unassigned
dependencies: Review approved action revisions
next-action: Pin reviewed immutable action SHAs in publication jobs.
related: DW-560


### DW-562: high; global-administrator revocation can remain authorized for every global-administrator command and query, including tenant correction, while its authority channels lag.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 22 (2026-10-09)"), 2026-10-12
location: docs/production-auth-readiness.md
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: high; global-administrator revocation can remain authorized for every global-administrator command and query, including tenant correction, while its authority channels lag.\n  evidence: `81144734` added `TenantsGlobalAdministratorVerifier` for EventStore Story 5.5 FR28 within the Story 5.6 range. Correction dispatch using global-administrator authority needs both the corroborated UI principal-claims gate (`src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationPrincipalResolver.cs:92`, reached from `src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs:542,561`) and the server verifier. At the `/process` boundary, the verifier admits `actor:globalAdmin` for tenant commands (`src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs:265-272`); at the `/query` boundary, it admits `QueryEnvelope.IsGlobalAdmin` for global-administrator queries (`src/Hexalith.Tenants/Authorization/TenantsGlobalAdministratorVerifier.cs:11-18`). Both use the persisted `GlobalAdministratorReadModel`. Revoking either authority channel stops its path once observed: identity-provider revocation takes effect after token or circuit renewal, and `GlobalAdministratorRemoved` takes effect after projection, with no bound if projection stalls. The global-administrator authority owner should define an authoritative or version-bounded revocation check for all such commands and queries."
status: open
disposition: awaiting-decision
verification-date: 2026-10-12
verification: TenantsGlobalAdministratorVerifier.IsCurrentGlobalAdministratorAsync still checks only persisted model.Administrators.Contains(claim.UserId), without sequence/freshness comparison. Identity and projection revocation remain eventually consistent; production-auth-readiness.md documents grant consistency only.
verification-files: docs/production-auth-readiness.md
evidence-kind: verified-gap
priority: P1
owner: global-administrator authority owner
dependencies: Platform authorization and revocation contract
next-action: Define the authoritative/version-bounded revocation guarantee and responsible authority before changing command/query gates.
related: DW-617,DW-564


## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 23 (2026-10-09)

Diff reviewed: Tenants `84c4e8b3..ee4a81f3` (the pass-21 fix pass, local and unpushed; `origin/main` = `84c4e8b3`); input 266-line scratch diff, 29,357 bytes, SHA-256 `6b2037560cd12aa6c63d5e6cd20d785153d042be2541afc4541672d2bb82a1b0`, not archived. Triage: 28 normalized findings → 0 decisions, 3 patches, 1 defer (folded into the pass-22 entry), 20 rejected.

## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 24 (2026-10-09)

Diff reviewed: Tenants `ee4a81f3`..working tree over root `0d8cc8d8` (= `origin/main`; the user's commit carrying the pass-23 records and patches); input 158-line scratch diff, 23,870 bytes, SHA-256 `e8087f4f487ebba254d54c17eb237c0518f0ae2dd32007608a30a499f5d651ab`, not archived. Triage: 24 normalized findings → 0 decisions, 2 patches (9 findings), 1 defer (3 findings), 12 rejected.

### DW-563: medium; release builds after `NuGet/login` run restored package code with the live NuGet key in their environment.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 24 (2026-10-09)"), 2026-10-12
location: .github/workflows/release.yml; scripts/publish-partial-release.sh
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: medium; release builds after `NuGet/login` run restored package code with the live NuGet key in their environment.\n  evidence: `release.yml` mints `NUGET_API_KEY` at `:425` and passes it to the Semantic Release step (`:431`), whose `prepareCmd` (`.releaserc.json:11`) runs `dotnet build Hexalith.Tenants.slnx` with implicit restore. Restored packages' MSBuild targets therefore execute with the key, contrary to the workflow comment that the key is \"minted only after the build\" (`release.yml:421-422`). `recover-partial-release.yml` logs in at `:72`, then `scripts/publish-partial-release.sh:14` builds with `NUGET_API_KEY`, `HEXALITH_ZOT_API_KEY` and `GH_TOKEN` in the environment (`recover-partial-release.yml:77-83`). The pass-22 OIDC entry covers pre-login code and `:3788` covers key expiry. In release-governance work, build and pack before credential exchange, and give the publish step only prebuilt artifacts."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: release.yml supplies NUGET_API_KEY to Semantic Release while .releaserc.json prepareCmd rebuilds/restores; recovery logs in before publish-partial-release.sh builds. Package targets execute in the credential-bearing environment; malicious execution is unverified.
verification-files: .github/workflows/release.yml; scripts/publish-partial-release.sh
evidence-kind: recommendation
priority: P2
owner: unassigned
dependencies: Release build/publication separation
next-action: Build and validate immutable packages before credential exchange; limit credential-bearing work to publication.
related: DW-560


### DW-564: low; Tenants production authentication guidance documents global-administrator grants but omits revocation lag.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 24 (2026-10-09)"), 2026-10-12
location: docs/production-auth-readiness.md
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: low; Tenants production authentication guidance documents global-administrator grants but omits revocation lag.\n  evidence: `docs/production-auth-readiness.md:76` says `GlobalAdministratorSet` takes effect after projection, but does not say that `GlobalAdministratorRemoved` also takes effect after projection and identity-provider revocation after token or circuit renewal. Update that Tenants-owned guidance separately from the platform authority check."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: docs/production-auth-readiness.md global-administrator bootstrap paragraph explains GlobalAdministratorSet projection lag but still omits removal lag/token or circuit renewal.
verification-files: docs/production-auth-readiness.md
evidence-kind: verified-gap
priority: P3
owner: unassigned
dependencies: DW-562 revocation contract
next-action: Document current revocation lag and the eventual-consistency boundary without claiming a bound.
related: DW-562,DW-617


## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 25 (2026-10-09)

Diff reviewed: baseline `11e65e37` through the working tree over root `cc17b071`; input `/tmp/story-56-diff-J8LkoHyp.patch`, 3,208,703 bytes, SHA-256 `8b551d774cd7fab5a6acbb34be921fda1e2f620f39c4518acdb15db34496a302`, not archived. Triage: 17 findings, 0 patches, 14 defers (2 new, 12 carried), 3 rejected.

### DW-565: medium; global-administrator correction proof needs a complete, current audit walk before pairing receipts.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 25 (2026-10-09)"), 2026-10-12
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
priority: P2
archived: 2026-10-12

### DW-566: medium; Trusted Publishing account guards lack executable success and failure tests.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 25 (2026-10-09)"), 2026-10-12
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: medium; Trusted Publishing account guards lack executable success and failure tests.\n  evidence: `PackageGovernanceTests.AssertTrustedPublishingJob` (`tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs:2209-2219`, called at `:904-905`) checks the two workflow guard blocks (`.github/workflows/release.yml:308`, `.github/workflows/recover-partial-release.yml:40`) for the `NUGET_USER` expansion text but does not execute their Bash conditions. Replacing `-z` with `-n` preserves that text and makes a configured account fail before publication. Release-governance work should execute both guards with populated and blank account values and assert the respective exit statuses."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: PackageGovernanceTests.AssertTrustedPublishingJob still asserts NUGET_USER expansion text, not Bash guard execution; both release workflows retain the populated/blank account check.
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Isolated workflow guard fixture
next-action: Execute each account guard with populated, blank and whitespace values and assert exits.
related: DW-557


## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 27 (2026-10-09)

Diff reviewed: baseline `11e65e37` through root `6e6a7f5b`; input `/tmp/story-5-6-baseline-OsK7WiL3.diff`, 1,815,498 bytes, not archived. Triage: 15 findings, 1 patch, 7 defers (1 new, 6 carried), 7 rejected.

### DW-567: high; the first global-administrator bootstrap command can name a different administrator than its authenticated actor.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 27 (2026-10-09)"), 2026-10-12
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
priority: P1
archived: 2026-10-13

## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 31 (2026-10-09)

Diff reviewed: Tenants `96cc6f11..cdd0c80c` (the pass-29 fix pass); input `/tmp/tenants-56-pass31-diff-IbVNh1.patch`, 89,787 bytes, SHA-256 `f726d22b3611a2146a432c1924104405e3279ab02e80d5612b3dd25ad585b923`, not archived. Triage: 41 findings, 2 decisions (both resolved to option (a), adding 2 patches), 6 patches, 1 defer (new), 14 rejected.

### DW-568: medium if true (unverified, maybe-false); the version-derived producer ETag cannot reach audit, index or global-administrator reads, because only `TenantReadModel` is stamped with a `ProjectionVersion`.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 31 (2026-10-09)"), 2026-10-12
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: medium if true (unverified, maybe-false); the version-derived producer ETag cannot reach audit, index or global-administrator reads, because only `TenantReadModel` is stamped with a `ProjectionVersion`.\n  evidence: `src/Hexalith.Tenants/Queries/TenantQueryResult.cs:50` falls back to `readModel?.ProjectionVersion` when the store ETag is absent. Only `TenantProjectionHandler.cs:236-250` assigns a version (`tenant-sequence:N`). `TenantIndexReadModel`, `TenantAuditReadModel` and `GlobalAdministratorReadModel` declare `ProjectionVersion`, but nothing in Tenants or EventStore assigns it. When their store ETag is absent, metadata is null and those surfaces fail closed under Story 4.2's rule, which blocks audit evidence and global-administrator grant/remove previews. Pass 29's rationale (\"all four read models carry a `ProjectionVersion`\") was wrong. To settle: probe whether live audit or global-administrator reads ever lack a store ETag. Pass 27 reported none for tenant detail; pass 30 observed `1`; the live audit test asserts `ProjectionBacked`. If they do, stamp sequence versions in those three projections."
status: open
disposition: uncertain
verification-date: 2026-10-12
verification: Original blanket no-version assertion is partly superseded: GlobalAdministratorProjectionHandler now stamps singleton and audit:system sequence versions. TenantIndexReadModel and tenant audit merge still lack producer versions. Missing evidence: live index/tenant-audit reads without store ETags and the intended cross-aggregate index validator.
evidence-kind: unverified-risk
priority: P2
owner: unassigned
dependencies: Live store ETag behavior and index-version contract
next-action: Capture safe metadata for index and tenant-audit reads with absent store ETags before deciding remaining stamping work.
related: DW-624


## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 32 (2026-10-10)

Diff reviewed: Tenants `d88bed1e` plus the uncommitted pass-31 fix pass, later committed as `3fab3094`. Triage: 14 findings, 2 patches, 6 defers, 6 rejected.

### DW-569: medium; Keycloak workload client authentication lacks a runtime token and authorized domain-call test.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 32 (2026-10-10)"), 2026-10-12
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: medium; Keycloak workload client authentication lacks a runtime token and authorized domain-call test.\n  evidence: The AppHost model test verifies shared secret parameter wiring, and the realm declares `clientAuthenticatorType: client-secret`, but no Keycloak-enabled test obtains a `client_credentials` token with that secret and completes an authorized domain-service call. The existing Aspire fixture runs with `EnableKeycloak=false`. Add a live Keycloak test in the identity/integration owner before relying on this mode's end-to-end operation."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: AppHostSecurityResourceGraphTests evaluates shared client-secret wiring and EventPublicationConfigurationTests parses realm scopes; AspireTopologyFixture still passes EnableKeycloak=false. No client_credentials plus protected domain-call test was found.
evidence-kind: verified-gap
priority: P2
owner: identity/integration owner
dependencies: Keycloak-enabled integration topology
next-action: Obtain a workload client_credentials token and assert an authorized domain-service call without retaining tokens.
related: DW-592


### DW-570: medium; the standalone release workflow omits the reusable Builds workflow's governed-provenance evaluation.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 32 (2026-10-10)"), 2026-10-12
location: .github/workflows/release.yml; references/Hexalith.Builds/.github/workflows/domain-release.yml
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: medium; the standalone release workflow omits the reusable Builds workflow's governed-provenance evaluation.\n  evidence: `.github/workflows/release.yml` checks the approved Builds checkout SHA but does not run the `governed-provenance` closure evaluation used by `references/Hexalith.Builds/.github/workflows/domain-release.yml`. This pre-existing release-governance difference should be resolved in the release workflow owner before publication relies on equivalent closure evidence."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: release.yml validates the Builds checkout SHA but contains no governed-provenance closure evaluation; shared Builds domain-release.yml supplies that evaluation. The parity recommendation remains unimplemented.
verification-files: .github/workflows/release.yml; references/Hexalith.Builds/.github/workflows/domain-release.yml
evidence-kind: recommendation
priority: P2
owner: release workflow owner
dependencies: Approved release-provenance policy
next-action: Add the owning workflow closure gate or document an approved equivalent closure procedure.
related: DW-593


## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 33 (2026-10-10)

Diff reviewed: Tenants `d88bed1e..3fab3094` (the pass-31 fix pass, local and unpushed); 60,432-byte scratch diff, SHA-256 `6174a862c240e8cc9c38cf85eb8adbc00ec60412d56a59a7d72ef7cc2b91ce58`, not archived. Triage: 38 findings, 0 decisions, 8 patches, 2 defers (both new), 18 rejected.

### DW-571: low; the Tenants AppHost gives the EventStore server no Dapr app-channel token, so in Development its internal channel admits callers without `dapr-api-token`.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 33 (2026-10-10)"), 2026-10-12
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: low; the Tenants AppHost gives the EventStore server no Dapr app-channel token, so in Development its internal channel admits callers without `dapr-api-token`.\n  evidence: `src/Hexalith.Tenants.AppHost/Program.cs:89` and `:168` wire `WithGeneratedEventStoreAppChannelToken()` for `tenants` and `sample` only; `eventstore` (added through `AddHexalithEventStore` at `:65`) gets none. The EventStore server validates the same `APP_API_TOKEN` through `DaprAppChannelTokenValidator` and `DaprAppChannelTokenHealthCheck` (`references/Hexalith.EventStore/src/Hexalith.EventStore/Authentication/DaprAppChannelTokenValidator.cs:27-39`), and EventStore's own AppHost wires `eventStore.WithGeneratedEventStoreAppChannelToken()` in run mode (`references/Hexalith.EventStore/src/Hexalith.EventStore.AppHost/Program.cs:421-427`). Pass 32's reason for skipping it (\"not shown to use this domain-service validator\") missed that validator. Pre-existing (pass 29 BH6). To close: wire the token after `AddHexalithEventStore`, assert the eventstore app/sidecar share it, and re-run the live topology lane."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: AppHost Program.cs wires WithGeneratedEventStoreAppChannelToken only on tenants/sample. AppHostSecurityResourceGraphTests.AssertAppChannelTokensAsync likewise checks only those two; eventstore receives no app-channel token.
evidence-kind: verified-defect
priority: P3
owner: unassigned
dependencies: EventStore hosting helper/runtime token contract
next-action: Wire and evaluate the EventStore app/sidecar token pair, then verify missing-token denial.
related: DW-569


### DW-572: medium; no blocking CI lane checks the ETag header of the generated `tenants-api` route.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 33 (2026-10-10)"), 2026-10-12
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: medium; no blocking CI lane checks the ETag header of the generated `tenants-api` route.\n  evidence: The mandatory live ETag agreement (`tests/Hexalith.Tenants.IntegrationTests/AspireTopologyTests.cs:220-221`, `:257-259`) and the in-process generated-controller ETag cases (`tests/Hexalith.Tenants.IntegrationTests/TenantsApiGeneratedControllerTests.cs:115`, `:321`) all live in IntegrationTests. CI runs that project only as the `continue-on-error` aspire tier (`.github/workflows/ci.yml:35`; `references/Hexalith.Builds/.github/workflows/domain-ci.yml:109-113`, `:669`). The blocking `TenantQueryResultTests` cover how metadata is built, not the HTTP header, so a generated API that dropped or rewrote the ETag (Story 4.2's freshness validator) leaves the release gate green. Pass 31 decision 2 kept the aspire tier advisory. To close: move the in-process generated-controller ETag cases into a blocking test project, as was done for the AppHost model checks."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: ci.yml keeps generated-controller and live HTTP ETag tests in IntegrationTests, solely the advisory Aspire project; blocking projects lack a generated-route HTTP ETag assertion. Existing decision to keep Aspire advisory is preserved.
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Separate blocking in-process route lane
next-action: Place generated-route ETag agreement coverage in a blocking test lane without changing the accepted advisory topology tier.
related: DW-577,DW-594


## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 34 (2026-10-10)

Diff reviewed: Tenants baseline `11e65e37` to `176d0670` plus the uncommitted pass-33 fix pass, later committed as `b00d6d1b`; `git diff --binary` input, 3,398,875 bytes, SHA-256 `17c0ce1ac274f1094b6b57ae990bb695deb540c019ae3a9da0e84e7287b1df48`, not archived. Triage: 17 findings, 0 patches, 16 defers (1 new, 15 carried), 1 rejected.

### DW-573: medium; container- or GitHub-Release-only partial release recovery depends on a successful NuGet account guard and login even when no package needs publication.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 34 (2026-10-10)"), 2026-10-12
location: .github/workflows/recover-partial-release.yml; scripts/publish-partial-release.sh
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: medium; container- or GitHub-Release-only partial release recovery depends on a successful NuGet account guard and login even when no package needs publication.\n  evidence: Commit `390da330` introduced the `NUGET_USER` guard and `NuGet/login` step within the Story 5.6 baseline range; neither exists at baseline `11e65e37`. `.github/workflows/recover-partial-release.yml:34-43,70-81` unconditionally requires the account and runs login before the recovery script. `scripts/publish-partial-release.sh:28-33` reads `NUGET_API_KEY` only while pushing entries from `missing-packages`, then handles an absent container at `:35` and a missing GitHub Release at `:46-52`. A NuGet account or login failure therefore blocks container- or GitHub-Release-only recovery. The related pass-25 entry above (Trusted Publishing account guards lack executable tests) covers guard verification; the release owner should condition the account guard and login on missing packages while preserving the package-publish gate."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: recover-partial-release.yml still unconditionally requires NUGET_USER and runs NuGet/login, while publish-partial-release.sh only reads NUGET_API_KEY for missing-packages. Container/release-only repair still depends on NuGet authentication.
verification-files: .github/workflows/recover-partial-release.yml; scripts/publish-partial-release.sh
evidence-kind: verified-defect
priority: P2
owner: release owner
dependencies: Validated recovery missing-package inventory
next-action: Condition account validation/login on packages needing publication while retaining their publication gate.
related: DW-566


## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 36 (2026-10-10)

Diff reviewed: Tenants baseline `11e65e37` through `c21bf7be` plus the uncommitted pass-35 fix pass (spec `in-review`, sprint `in-progress`), later committed as `c3577147`; `git diff --binary` input, 3,417,703 bytes, SHA-256 `bc733579eb218a3645d688392546a9d00daf96cd50899c55e443822784e7d784`, not archived. Triage: 16 findings, 0 patches, 12 defers (3 new, 9 carried), 4 rejected.

### DW-574: medium if rendered facts are exposed; a principal change within one Blazor circuit retains the previous principal's correction attempt. Owner: Story 5.6 tracker in Tenants UI.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 36 (2026-10-10)"), 2026-10-12
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: medium if rendered facts are exposed; a principal change within one Blazor circuit retains the previous principal's correction attempt. Owner: Story 5.6 tracker in Tenants UI.\n  evidence: `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCorrectionAttemptTracker.cs:9` keys attempts by tenant, and `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1513` looks up the retained attempt by `TenantId`. The tracker is circuit-scoped (`src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs:102`). A SignalR reconnect after the cookie changes can replace the principal in that live circuit: `ComponentHub.ConnectCircuit` calls `SetCircuitUser`, which calls `SetAuthenticationState`. New gateway reads use the new principal because `FrontComposerGatewayAuthorizationHandler` resolves `sub` per request. Neither `TenantAuditPage` nor `CorrectionStartPanel` subscribes to `AuthenticationStateChanged`, unlike `TenantDetailPage.razor:713,940`, `GlobalAdministratorCorrectionPanel.razor:548`, and `TenantCreateAuditReturnState.cs:24`. A bUnit authentication-state transition and rendered-content assertion, on the same page and after in-circuit navigation back to the tenant, must establish whether attempt-specific facts such as an uncommitted intent are visible to the new audit-authorized principal."
status: open
disposition: uncertain
verification-date: 2026-10-12
verification: TenantAuditPage now subscribes to authentication changes and clears displayed state, superseding part of the finding. TenantCorrectionAttemptTracker still keys only tenant and ResumeCorrection restores retained intent once any caller has an authorized audit snapshot. Missing: same-circuit new-principal rendered/navigation test proving attempt facts are withheld.
evidence-kind: unverified-risk
priority: P2
owner: Story 5.6 tracker in Tenants UI
dependencies: Caller-bound retained tenant attempts
next-action: Add a principal-transition/remount rendered regression; bind or reauthorize retained attempt provenance if previous-user intent is exposed.
related: DW-580,DW-609


### DW-575: medium; the optional quickstart bootstrap check can install the wrong administrator after automatic bootstrap fails. Owners: Tenants bootstrap and quickstart documentation.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 36 (2026-10-10)"), 2026-10-12
location: docs/quickstart.md
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: medium; the optional quickstart bootstrap check can install the wrong administrator after automatic bootstrap fails. Owners: Tenants bootstrap and quickstart documentation.\n  evidence: `docs/quickstart.md:118,191-208` submits `BootstrapGlobalAdmin` for `admin-user` as an expected-rejection check, while the AppHost uses a generated service subject. The related pass-27 entry above (`deferred-work.md:3880`) explains that the aggregate accepts `command.UserId` without binding it to the caller. If automatic bootstrap fails without stopping the healthy service, the check can succeed for `admin-user`. Bootstrap is one-time (`src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsAggregate.cs:14`), and the hosted service treats `GlobalAdminAlreadyBootstrappedRejection` as success (`src/Hexalith.Tenants/Bootstrap/TenantBootstrapHostedService.cs:114-119`). Every restart can then report bootstrap complete while the generated service subject remains unauthorized; with the realm's UUID subjects, nobody can act as `admin-user` until state is wiped. The quickstart should require verification of the configured administrator before permitting the optional check."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: quickstart.md still presents BootstrapGlobalAdmin admin-user as an optional expected-rejection check without first proving the configured service subject owns bootstrap; GlobalAdministratorsAggregate bootstrap trusts its supplied target.
verification-files: docs/quickstart.md
evidence-kind: verified-defect
priority: P2
owner: Tenants bootstrap and quickstart documentation
dependencies: DW-567 bootstrap identity boundary
next-action: Require configured administrator verification before optional bootstrap checks, and replace the wrong-subject command example.
related: DW-567,DW-576


### DW-576: medium; the manual quickstart has no concrete path to grant a newly provisioned local user global-administrator authority. Owner: Tenants quickstart documentation.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 36 (2026-10-10)"), 2026-10-12
location: docs/quickstart.md
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: medium; the manual quickstart has no concrete path to grant a newly provisioned local user global-administrator authority. Owner: Tenants quickstart documentation.\n  evidence: `docs/quickstart.md:120` instructs readers to obtain an existing administrator's grant but does not explain how to authenticate as that administrator or issue the grant in the default generated-credential topology. A local development runbook should provide the executable grant path."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: quickstart.md says obtain an existing administrator grant but provides no concrete authentication/SetGlobalAdministrator path for the generated-credential default topology.
verification-files: docs/quickstart.md
evidence-kind: verified-gap
priority: P2
owner: Tenants quickstart documentation
dependencies: Local human-account and administrator handoff contract
next-action: Document an executable local administrator-to-human grant runbook.
related: DW-575


## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 39 (2026-10-10)

Diff reviewed: Tenants `b14bf146..2b91a52f` (the pass-37 fix pass with the pass-38 test patches, local and unpushed); 26,970-byte scratch diff, SHA-256 `93739b129fd547bda106a89c038fe693087a3c62dae7d36324315db719da9257`, not archived. Triage: 36 findings, 0 decisions, 5 patches, 2 defers (both new), 18 rejected.

### DW-577: medium; the live command-status proof assertions added in pass 38 run only in the advisory, already-red aspire tier. Owner: Tenants CI test tiering.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 39 (2026-10-10)"), 2026-10-12
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: medium; the live command-status proof assertions added in pass 38 run only in the advisory, already-red aspire tier. Owner: Tenants CI test tiering.\n  evidence: `tests/Hexalith.Tenants.IntegrationTests/AspireTopologyTests.cs:158-164` and `:357-375` assert the live create and add status proof, and the pass-39 patches 1 and 2 add the UI-gateway and projection-sequence checks there. CI runs IntegrationTests only as `aspire-test-project` (`.github/workflows/ci.yml:35`), which is `continue-on-error` by default (`references/Hexalith.Builds/.github/workflows/domain-ci.yml:109-113`, `:669`). At `c3577147` (run 38039239192, aspire job 114176854705) that job failed 9 of 178 tests while the run succeeded, and `Generated_tenants_api_get_tenant_reads_verified_redis_state_with_projection_authority` failed at `:122` on the `tenants-api` `/alive` SSL timeout, before its new assertions. Related: the `aspire-continue-on-error` entry (`:3726`) and the pass-33 ETag entry (`:3912`); pass-31 decision 2 kept the tier advisory. To close: a blocking lane that checks live or contract-level command-status proof through the UI gateway."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: ci.yml still runs live UI-gateway command-status/projection-sequence assertions only in advisory IntegrationTests. Current CI run 38269521776 passes overall with the Aspire job failing; the accepted advisory-tier decision is unchanged.
evidence-kind: verified-gap
priority: P2
owner: Tenants CI test tiering
dependencies: Blocking status-proof contract lane; DW-594 for live proof
next-action: Add a blocking UI-gateway status-proof lane without relying on topology-tier success.
related: DW-572,DW-594


### DW-578: low; `_bmad-output/project-context.md` omits `AppHost.Tests` from blocking CI Tier 2. Owner: agent-context maintenance.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 39 (2026-10-10)"), 2026-10-12
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: low; `_bmad-output/project-context.md` omits `AppHost.Tests` from blocking CI Tier 2. Owner: agent-context maintenance.\n  evidence: `_bmad-output/project-context.md:112` lists only `Server.Tests`, while `.github/workflows/ci.yml:32-34` runs `tests/Hexalith.Tenants.AppHost.Tests` next to it in `integration-test-projects` (added by Story 5.6 pass 31). Agents that read the context file will leave out the blocking AppHost model checks."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: _bmad-output/project-context.md:112 still names Server.Tests alone in Tier 2; ci.yml integration-test-projects includes AppHost.Tests and Server.Tests.
evidence-kind: verified-gap
priority: P3
owner: agent-context maintenance
dependencies: Current CI configuration
next-action: Update Tier 2 guidance to include the blocking AppHost model project.
related: DW-596


## Deferred from: spec-eventstore-3-117-1.md (2026-10-10)

### DW-579: Restore or reconcile missing historical Git revisions referenced by the Builds package audit before accepting the EventStore 3.120.0 audit.

origin: migrated from unnumbered ledger ("Deferred from: spec-eventstore-3-117-1.md (2026-10-10)"), 2026-10-12
location: _bmad-output/implementation-artifacts/dependency-refresh-eventstore-3.120.0-evidence-2026-10-10.json; references/Hexalith.Builds/Tools/package-version-audit.json
source_spec: `_bmad-output/implementation-artifacts/spec-eventstore-3-117-1.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-eventstore-3-117-1.md`\n  summary: Restore or reconcile missing historical Git revisions referenced by the Builds package audit before accepting the EventStore 3.120.0 audit.\n  evidence: The supported full pre-edit audit was generated against committed Builds `2cf00028bbe563d80d4d12b5fb2054914f14fcb6`, but deterministic validation of its saved snapshot exits 1 with 50 unavailable historical origin-revision errors in addition to a temporary catalog-path mismatch. The checked-in audit validator exits 1 with 1,563 errors, including unavailable historical revisions; logs and exact commands are archived in `dependency-refresh-eventstore-3.120.0-raw-logs-2026-10-10.zip`. This inherited provenance gap blocks accepted audit evidence even after a governed catalog commit."
status: open
disposition: blocked
verification-date: 2026-10-12
verification: dependency-refresh-eventstore-3.120.0-evidence-2026-10-10.json preserves failed supported audit acceptance (50 unavailable historical origins). Current Builds package-version-audit.json still references locally missing commits 200a0133 and 9566bafb (read-only cat-file check); no replacement accepted audit is present.
verification-files: _bmad-output/implementation-artifacts/dependency-refresh-eventstore-3.120.0-evidence-2026-10-10.json; references/Hexalith.Builds/Tools/package-version-audit.json
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Builds-maintainer historical provenance repair
next-action: Reconcile missing owning-repository origin revisions and regenerate/validate supported audit evidence.
related: DW-596


## Deferred from: spec-gh-actions-28944933021-85877006572.md (2026-10-10)

### DW-580: Bind retained Tenants UI query snapshots to the authenticated caller.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: spec-gh-actions-28944933021-85877006572.md (2026-10-10)"), 2026-10-12
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`
priority: P2
archived: 2026-10-13

### DW-581: Recover missing package assets on an existing GitHub Release.

origin: migrated from unnumbered ledger ("Deferred from: spec-gh-actions-28944933021-85877006572.md (2026-10-10)"), 2026-10-12
location: scripts/publish-partial-release.sh; scripts/verify-partial-release.sh
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`\n  summary: Recover missing package assets on an existing GitHub Release.\n  evidence: `publish-partial-release.sh` uploads assets only when creating a release, leaving a 200 release with missing assets incomplete."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: publish-partial-release.sh creates/uploads nupkg assets only for release HTTP 404 and does nothing for existing HTTP 200 releases; verify-partial-release.sh then requires five assets.
verification-files: scripts/publish-partial-release.sh; scripts/verify-partial-release.sh
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: Release asset inventory
next-action: Upload verified missing assets to existing releases without replacing matching assets.
related: DW-620


### DW-582: Verify existing recovery container content against the reviewed source.

origin: migrated from unnumbered ledger ("Deferred from: spec-gh-actions-28944933021-85877006572.md (2026-10-10)"), 2026-10-12
location: scripts/validate-partial-release-recovery.sh; scripts/verify-partial-release.sh
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`\n  summary: Verify existing recovery container content against the reviewed source.\n  evidence: `validate-partial-release-recovery.sh` records `present` from HTTP 200 without digest or provenance comparison."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: validate-partial-release-recovery.sh classifies container HTTP 200 as present; verify-partial-release.sh repeats presence only. No reviewed-source digest/provenance comparison exists.
verification-files: scripts/validate-partial-release-recovery.sh; scripts/verify-partial-release.sh
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Reviewed container digest/provenance
next-action: Compare existing OCI content/provenance with reviewed immutable source before accepting recovery.
related: DW-583,DW-589


### DW-583: Verify release artifacts and destinations after publication.

origin: migrated from unnumbered ledger ("Deferred from: spec-gh-actions-28944933021-85877006572.md (2026-10-10)"), 2026-10-12
location: .github/workflows/release.yml
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`\n  summary: Verify release artifacts and destinations after publication.\n  evidence: The `verify-publication` job checks the release tag but does not independently inspect NuGet packages, the container, or release assets."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: release.yml verify-publication validates a published tag resolving to dispatched source only; it does not inspect NuGet content, OCI content or GitHub package assets. Recovery verifier is separate and mostly checks presence.
verification-files: .github/workflows/release.yml
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Immutable release-artifact manifest
next-action: Verify every expected destination and artifact content after normal publication.
related: DW-582,DW-620


### DW-584: Decide whether separate story and source-reference workflows must gate releases.

origin: migrated from unnumbered ledger ("Deferred from: spec-gh-actions-28944933021-85877006572.md (2026-10-10)"), 2026-10-12
location: .github/workflows/release.yml
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`\n  summary: Decide whether separate story and source-reference workflows must gate releases.\n  evidence: Unverified medium-severity policy question; `release.yml` queries only `ci.yml`, and an explicit release-gate policy would establish whether failed separate workflows must block publication."
status: open
disposition: awaiting-decision
verification-date: 2026-10-12
verification: release.yml verify-source queries only successful ci.yml runs; separate story-guards.yml/source-reference checks are not included. Existing separate workflow design alone does not establish release-blocking policy.
verification-files: .github/workflows/release.yml
evidence-kind: recommendation
priority: P2
owner: unassigned
dependencies: Release-gate policy owner
next-action: Decide which separate exact-source workflow outcomes must block publication and document the gate.
related: DW-585


### DW-585: Run the story gitlink validator against changed story artifacts in CI.

origin: migrated from unnumbered ledger ("Deferred from: spec-gh-actions-28944933021-85877006572.md (2026-10-10)"), 2026-10-12
location: .github/workflows/story-guards.yml; tests/scripts/test_validate_story_gitlinks.py; scripts/validate-story-gitlinks.py
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`\n  summary: Run the story gitlink validator against changed story artifacts in CI.\n  evidence: `story-guards.yml` runs regression tests for the validator, and workflow searches found no invocation on changed stories."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: story-guards.yml invokes tests/scripts/test_validate_story_gitlinks.py but no workflow invokes validate-story-gitlinks.py on actual changed story artifacts.
verification-files: .github/workflows/story-guards.yml; tests/scripts/test_validate_story_gitlinks.py; scripts/validate-story-gitlinks.py
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Changed-story selection/base revision
next-action: Add actual changed-story validation alongside the validator regression lane.
related: DW-643,DW-584


### DW-586: Keep pending create-command status in a waiting state.

origin: migrated from unnumbered ledger ("Deferred from: spec-gh-actions-28944933021-85877006572.md (2026-10-10)"), 2026-10-12
location: src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`\n  summary: Keep pending create-command status in a waiting state.\n  evidence: The gateway maps status 404 to `Pending`, while `TenantCreateCommandSnapshot.ApplyStatus` treats the resulting null status as assertive `UnableToVerify`."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: TenantCreateCommandModels.cs TenantCreateCommandSnapshot.ApplyStatus sets any null Status to UnableToVerify/assertive even when TenantCommandStatusResult.IsPending=true; status 404 maps to Pending in the gateway.
verification-files: src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: Pending-status UX contract
next-action: Preserve a polite waiting lifecycle for Pending status and cover 404-before-first-status.
related: none


### DW-587: Clear a retained remove-configuration activity latch when its attempt expires without a mounted child.

origin: migrated from unnumbered ledger ("Deferred from: spec-gh-actions-28944933021-85877006572.md (2026-10-10)"), 2026-10-12
location: n/a
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`\n  summary: Clear a retained remove-configuration activity latch when its attempt expires without a mounted child.\n  evidence: `TenantConfigurationManagement` sets `_removeCommandInFlight` from a retained attempt and clears it on context change or child callback, which an unmounted child cannot send."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: TenantConfigurationManagement.OnParametersSet sets _removeCommandInFlight=true from a retained removal; it has no inverse reset when Find returns null and no tracker expiry subscription. Same-tenant expiry with no mounted child can retain the latch.
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: Removal tracker expiry notification
next-action: Clear/recompute the parent activity latch when the retained attempt expires without a mounted child.
related: none
duplicate-of: DW-325


### DW-588: Reject a recovery state with an existing GitHub Release and missing tag.

origin: migrated from unnumbered ledger ("Deferred from: spec-gh-actions-28944933021-85877006572.md (2026-10-10)"), 2026-10-12
location: scripts/validate-partial-release-recovery.sh
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`\n  summary: Reject a recovery state with an existing GitHub Release and missing tag.\n  evidence: The recovery validator independently permits a 404 tag and a 200 release, then later tag verification cannot succeed."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: validate-partial-release-recovery.sh permits tag=404 independently of release=200; publisher leaves an existing release untouched and verifier requires a resolving tag. The inconsistent state remains admissible.
verification-files: scripts/validate-partial-release-recovery.sh
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: Validated GitHub tag/release inventory
next-action: Reject existing-release/missing-tag recovery before any publication.
related: DW-581,DW-607


### DW-589: Check OCI index handling in the recovery container manifest probe.

origin: migrated from unnumbered ledger ("Deferred from: spec-gh-actions-28944933021-85877006572.md (2026-10-10)"), 2026-10-12
location: n/a
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`\n  summary: Check OCI index handling in the recovery container manifest probe.\n  evidence: Unverified medium-severity compatibility issue; the probe advertises only an image manifest media type, and a live Zot probe with an OCI index would settle whether it returns 200 or 406."
status: open
disposition: uncertain
verification-date: 2026-10-12
verification: Recovery validator/verifier still advertise only application/vnd.oci.image.manifest.v1+json. Missing evidence: deployed Zot negotiation behavior for a multiarchitecture OCI index under this Accept header.
evidence-kind: unverified-risk
priority: P2
owner: unassigned
dependencies: Live Zot OCI-index negotiation fixture
next-action: Probe an existing OCI index safely or emulate registry negotiation and document accepted media types.
related: DW-582


### DW-590: Recheck the live main SHA immediately before partial release publication.

origin: migrated from unnumbered ledger ("Deferred from: spec-gh-actions-28944933021-85877006572.md (2026-10-10)"), 2026-10-12
location: scripts/publish-partial-release.sh
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`\n  summary: Recheck the live main SHA immediately before partial release publication.\n  evidence: `publish-partial-release.sh` checks main before building and packing, then publishes without a second check after those operations."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: publish-partial-release.sh compares live main before build/pack/hash validation; no second source check precedes the first push/publication. Main may advance during those operations.
verification-files: scripts/publish-partial-release.sh
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Exact-source publication invariant
next-action: Recheck live main after all build/validation work and immediately before publication.
related: DW-563


### DW-591: Add a rendered document-language test for English and French UI cultures.

origin: migrated from unnumbered ledger ("Deferred from: spec-gh-actions-28944933021-85877006572.md (2026-10-10)"), 2026-10-12
location: n/a
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`\n  summary: Add a rendered document-language test for English and French UI cultures.\n  evidence: `App.razor` derives `html lang` from UI culture, while existing tests assert only source text."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: App.razor derives html lang from UI culture, while TenantsUiCompositionTests.Document_language_uses_the_active_request_culture still reads source text only. No rendered EN/FR document assertion was found.
evidence-kind: verified-gap
priority: P3
owner: unassigned
dependencies: Rendered host fixture
next-action: Render actual host documents under en/fr cultures and assert html lang.
related: DW-614


### DW-592: Verify the imported Keycloak project scope through an issued workload token and protected endpoint.

origin: migrated from unnumbered ledger ("Deferred from: spec-gh-actions-28944933021-85877006572.md (2026-10-10)"), 2026-10-12
location: n/a
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`\n  summary: Verify the imported Keycloak project scope through an issued workload token and protected endpoint.\n  evidence: The realm test parses JSON, and the Aspire fixture disables Keycloak, leaving the effective `domain-service:project` token claim unverified."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: EventPublicationConfigurationTests.LocalKeycloakRealm_DeclaresScopedEventStoreWorkloadClientAndDefaultHumanScopes parses the imported JSON only; AspireTopologyFixture disables Keycloak and no issued-token/protected-project test exists.
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Keycloak-enabled workload token fixture
next-action: Issue the configured workload token and prove its domain-service:project scope through the protected endpoint.
related: DW-569
duplicate-of: DW-569


## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 40 (2026-10-10)

Diff reviewed: Tenants baseline `11e65e37` to `01602592` plus the then-current working tree; 4,329,888-byte diff, SHA-256 `b238fc55a1be2be5066869148d3a62e1a90d920c19837cec75dc7a57d48194e5`, not archived (spec "Pass-40 review triage"). Triage: 12 findings, 9 defers (one new entry below; the rest were already in the ledger), 3 rejected. Heading and severity added in pass 41.

### DW-593: medium; reconcile the Builds execution revision used by partial release recovery with the publishing workflow. Owner: release recovery (`recover-partial-release.yml`).

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 40 (2026-10-10)"), 2026-10-12
location: .github/workflows/release.yml; .github/workflows/recover-partial-release.yml; scripts/validate-partial-release-recovery.sh
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: medium; reconcile the Builds execution revision used by partial release recovery with the publishing workflow. Owner: release recovery (`recover-partial-release.yml`).\n  evidence: `release.yml` declares `53d53ae42abf7c87d385a078ab260531480bbf8a` for publication, but `recover-partial-release.yml` declares `2eac6955a812465a4308d194ae38fee35b766b05` for recovery. `validate-partial-release-recovery.sh` verifies only that the recovery value has SHA syntax. Recovery builds with the source tree's Builds gitlink, so matching publication-tool provenance is not established. The release recovery owner should decide and enforce the exact revision relationship before relying on rebuilt artifacts."
status: open
disposition: awaiting-decision
verification-date: 2026-10-12
verification: release.yml declares Builds execution 53d53ae4 while recover-partial-release.yml declares 2eac6955; validate-partial-release-recovery.sh checks SHA syntax only and recovery builds use source gitlink. Approved relationship between publication and recovery tooling remains unspecified.
verification-files: .github/workflows/release.yml; .github/workflows/recover-partial-release.yml; scripts/validate-partial-release-recovery.sh
evidence-kind: verified-gap
priority: P2
owner: release recovery (recover-partial-release.yml)
dependencies: Release-owner exact execution provenance decision
next-action: Define and enforce the approved publication/recovery Builds revision relationship.
related: DW-570


## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 41 (2026-10-10)

Diff reviewed: Tenants `2b91a52f..f6e7d7a5`, Story 5.6 paths only (the pass-39 fix pass `ef4b7f2f..01602592` plus the pass-40 records committed as `f6e7d7a5`); 43,644-byte scratch diff, SHA-256 `a21d3a98d4291f0d2aec2c8bfe0553145c768e4840e7cdff9ae37da944d69c12`, not archived. Triage: 30 findings, 1 decision, 4 patches, 3 defers (all new), 13 rejected.

### DW-594: medium; `Generated_tenants_api_get_tenant_reads_verified_redis_state_with_projection_authority` has never passed in CI, because `tenants-api` `/alive` fails TLS on the runner, and no entry tracks that failure. Owner: Tenants integration-test fixture.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 41 (2026-10-10)"), 2026-10-12
location: tests/Hexalith.Tenants.IntegrationTests/AspireTopologyTests.cs
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: medium; `Generated_tenants_api_get_tenant_reads_verified_redis_state_with_projection_authority` has never passed in CI, because `tenants-api` `/alive` fails TLS on the runner, and no entry tracks that failure. Owner: Tenants integration-test fixture.\n  evidence: In CI runs 37966289093, 38034556707, 38035646675, 38039239192 and 38053398801 (aspire job 114217781085 at `01602592`), the test failed at `tests/Hexalith.Tenants.IntegrationTests/AspireTopologyTests.cs:125` with \"tenants-api /alive did not return HTTP 200 within 00:04:00 … The SSL connection could not be established\". That is before any command is submitted, so its persisted-state and projection-version assertions never run. `WaitForTenantsApiAliveAsync` keeps only `ex.Message` (`:846-847`) and drops the inner exception that would name the certificate fault. The fixture's HTTPS `tenants-api` client is in `tests/Hexalith.Tenants.IntegrationTests/Fixtures/AspireTopologyFixture.cs`. The pass-39 tiering entry above mentions the failure only in passing; making that lane blocking would still fail here. To close: surface the inner exception, fix the runner TLS trust or use the HTTP endpoint, and see the test pass in CI."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: Current HEAD CI run 38269521776 job 114882247420 (2026-10-12) still fails Generated_tenants_api_get_tenant_reads_verified_redis_state_with_projection_authority at AspireTopologyTests.cs:125: /alive TLS timeout before command/projection assertions. Existing CI log inspected; no local tests run.
verification-files: tests/Hexalith.Tenants.IntegrationTests/AspireTopologyTests.cs
evidence-kind: verified-defect
priority: P2
owner: Tenants integration-test fixture
dependencies: Runner TLS trust or selected HTTP endpoint
next-action: Expose safe inner TLS cause, repair endpoint/trust, then require this exact live test to pass in CI.
related: DW-572,DW-577


### DW-595: medium; EventStore's own CI is red at the pinned `a5405602` (secret scan and a contracts packaging test). Owner: EventStore (Story 6.1-P1R evidence, contracts packaging).

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 41 (2026-10-10)"), 2026-10-12
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: medium; EventStore's own CI is red at the pinned `a5405602` (secret scan and a contracts packaging test). Owner: EventStore (Story 6.1-P1R evidence, contracts packaging).\n  evidence: EventStore CI run 38050087596 at `a5405602`: `build-and-test` job 114207290957 fails `SecretsProtectionTests.TrackedReusableContent_DoesNotContainUsableSecrets` with 152 violations, all under `_bmad-output/implementation-artifacts/evidence/6-1-p1r-31190-published-run/`. The previously adopted pin `37451b52` (run 37965432920, job 113938609179) already failed it with 19 violations under `6-1-p1r-current-source-2026-10-09/`, so the failure predates this range. These are not Story 5.6's four historical locations. The `contracts` job 114207290744 newly fails `ContractsPackageDependencyTests.SharedConsumerAuthorityValidatorPassesForEveryTrackedMsBuildSurfaceAsync` (it passed at `37451b52`). Tenants `build-and-test` stays green on the 3.120.0 package lane."
status: open
disposition: blocked
verification-date: 2026-10-12
verification: Pinned source is now 3d0b738d. CI run 38266659178 still fails build-and-test/contracts, but current logs no longer show the original secret-scan or SharedConsumerAuthorityValidator failures. Remaining failures are SharedProjectionChunkedStage/CopyOnWrite, DomainServiceIsolation, ReleasePackageManifest and ProjectionDispatchContract tests. Historical failures retained; current upstream CI blocker differs.
evidence-kind: verified-defect
priority: P2
owner: EventStore (Story 6.1-P1R evidence, contracts packaging)
dependencies: EventStore-maintainer fixes at adopted source revision
next-action: Route current failing upstream tests to EventStore and capture a green exact-pin CI result.
related: none


### DW-596: low; `_bmad-output/project-context.md` is stale about the EventStore 3.120.0 catalog commit. Owner: EventStore 3.120.0 dependency refresh (`spec-eventstore-3-117-1.md`) and agent-context maintenance.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 41 (2026-10-10)"), 2026-10-12
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: low; `_bmad-output/project-context.md` is stale about the EventStore 3.120.0 catalog commit. Owner: EventStore 3.120.0 dependency refresh (`spec-eventstore-3-117-1.md`) and agent-context maintenance.\n  evidence: `_bmad-output/project-context.md:34` says the 3.120.0 catalog \"requires a governed owning-repository commit … no commit is authorized in this task\". Builds `9e6b6ca3` (\"fix: update HexalithEventStoreVersion to 3.120.0\") is that commit, and root `b3878f14` pins it, so agents reading the file will think the package lane is still 3.119.0. `:112` also still omits `AppHost.Tests` from Tier 2 (the pass-39 entry above)."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: project-context.md:34 still says the edited 3.120.0 catalog needs a commit; current Builds catalog/gitlink already contains 3.120.0. Its Tier 2 guidance also still omits AppHost.Tests. Supported audit acceptance remains a separate blocker.
evidence-kind: verified-gap
priority: P3
owner: EventStore 3.120.0 dependency refresh (spec-eventstore-3-117-1.md) and agent-context maintenance
dependencies: Current dependency/provenance facts
next-action: Refresh the dependency narrative and CI project list without claiming unresolved audit acceptance.
related: DW-578,DW-579


## Deferred from: spec-gh-actions-28944933021-85877006572.md, continued (2026-10-10)

### DW-597: Preserve system-scope authorization for the Keycloak administrator's multivalued tenant claim.

origin: migrated from unnumbered ledger ("Deferred from: spec-gh-actions-28944933021-85877006572.md, continued (2026-10-10)"), 2026-10-12
location: n/a
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`\n  summary: Preserve system-scope authorization for the Keycloak administrator's multivalued tenant claim.\n  evidence: The realm maps system, tenant-a, and tenant-b to eventstore:tenant, while TenantsGlobalAdministratorClaims accepts only one distinct literal scope and cannot establish system scope for that principal."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: TenantsGlobalAdministratorClaims.ResolveSystemScopeEvidence rejects more than one distinct eventstore:tenant value; the sample Keycloak administrator has system/tenant-a/tenant-b. The shipped principal cannot corroborate system scope through this resolver.
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: Multitenant claim normalization contract
next-action: Allow explicit unambiguous system membership within valid multivalued claims and add the shipped-realm principal regression.
related: DW-614


### DW-598: Require a secure authority before posting bootstrap administrator credentials.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: spec-gh-actions-28944933021-85877006572.md, continued (2026-10-10)"), 2026-10-12
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`
priority: P2
archived: 2026-10-13

### DW-599: Enforce tenant configuration read policy at the direct API boundary.

origin: migrated from unnumbered ledger ("Deferred from: spec-gh-actions-28944933021-85877006572.md, continued (2026-10-10)"), 2026-10-12
location: n/a
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`\n  summary: Enforce tenant configuration read policy at the direct API boundary.\n  evidence: GetTenantQueryHandler returns all configuration values to an authorized tenant member, while prefix and display-safe filtering runs only in the UI composer, leaving direct REST callers outside the FR-6 display policy."
status: open
disposition: awaiting-decision
verification-date: 2026-10-12
verification: GetTenantQueryHandler.ExecuteCoreAsync still returns model.Configuration wholesale after membership authorization; display-safe/prefix policy lives in UI composition. FR-6 API applicability needs an explicit boundary decision; current REST disclosure is verified, but policy violation is not established.
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Configuration read-policy scope decision
next-action: Decide whether FR-6 filtering protects API disclosure as well as UI display; specify the owning boundary.
related: none


### DW-600: Treat duplicate administrator identities across cursor pages as incomplete projection evidence.

origin: migrated from unnumbered ledger ("Deferred from: spec-gh-actions-28944933021-85877006572.md, continued (2026-10-10)"), 2026-10-12
location: n/a
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`\n  summary: Treat duplicate administrator identities across cursor pages as incomplete projection evidence.\n  evidence: GlobalAdministratorsProjectionLoader silently drops repeated user IDs and marks the final aggregate IsCompleteEvidence=true even when pages overlap."
status: open
disposition: awaiting-decision
verification-date: 2026-10-12
verification: GlobalAdministratorsProjectionLoader.TryAdd still silently deduplicates across pages and marks terminal evidence complete; LoadAsyncDeduplicatesIdentitiesWithinAndAcrossPagesWithOrdinalComparison explicitly pins this behavior. The established paging spec requires ordinal aggregation, so rejection is a proposed policy change, not a proven regression.
evidence-kind: recommendation
priority: P2
owner: unassigned
dependencies: Complete-evidence duplicate-page policy
next-action: Decide whether cross-page overlap must invalidate completeness; reconcile any new rule with the existing spec/test contract.
related: none


### DW-601: Preserve unique method numbers when elicitation catalog overrides replace shipped methods.

origin: migrated from unnumbered ledger ("Deferred from: spec-gh-actions-28944933021-85877006572.md, continued (2026-10-10)"), 2026-10-12
location: n/a
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`\n  summary: Preserve unique method numbers when elicitation catalog overrides replace shipped methods.\n  evidence: merge_extra accepts a replacement num that can collide with an existing method, while numeric lookup keeps the first collision and makes the other method unreachable by number."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: .agents/skills/bmad-advanced-elicitation/scripts/pick_methods.py merge_extra accepts replacement num unchanged; numeric lookup uses setdefault, so duplicate numbers leave later methods unreachable. Existing tests cover inherited numbering, not explicit collisions.
evidence-kind: verified-defect
priority: P3
owner: unassigned
dependencies: Local elicitation catalog ownership
next-action: Reject or allocate unique explicit replacement method numbers and add collision coverage.
related: none


### DW-602: Bound tenant list payload count to the requested page size before enrichment.

origin: migrated from unnumbered ledger ("Deferred from: spec-gh-actions-28944933021-85877006572.md, continued (2026-10-10)"), 2026-10-12
location: n/a
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`\n  summary: Bound tenant list payload count to the requested page size before enrichment.\n  evidence: TenantsRestQueryClient validates item shape but not count, and TenantQueryGateway schedules one detail read per returned summary, expanding memory and read work for malformed large pages."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: TenantsRestQueryClient.IsValidPayload validates item shape but not requested page count; TenantQueryGateway enriches every returned tenant summary. The 16 MiB body cap does not enforce the requested page-size bound.
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Requested page-size propagation to payload validator
next-action: Reject oversized item counts before hydration and test bounded detail-call fanout.
related: none


### DW-603: Prevent one stalled tenant refresh callback from blocking later notifications.

origin: migrated from unnumbered ledger ("Deferred from: spec-gh-actions-28944933021-85877006572.md, continued (2026-10-10)"), 2026-10-12
location: n/a
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`\n  summary: Prevent one stalled tenant refresh callback from blocking later notifications.\n  evidence: TenantReadRefreshSubscription awaits callbacks serially without a deadline and retains the projection key in _running until the stalled callback returns."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: TenantReadRefreshSubscription.RunRefreshLoopAsync awaits callbacks sequentially while _running retains the projection key; no deadline isolates a callback that never completes. Subsequent notifications cannot start an independent loop.
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: Refresh callback cancellation/deadline contract
next-action: Bound or isolate callbacks so a stalled subscriber cannot block later refresh notifications.
related: none


### DW-604: Define a projection schema rebuild path for historical tenant events.

origin: migrated from unnumbered ledger ("Deferred from: spec-gh-actions-28944933021-85877006572.md, continued (2026-10-10)"), 2026-10-12
location: docs/event-contract-reference.md
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`\n  summary: Define a projection schema rebuild path for historical tenant events.\n  evidence: TenantProjectionHandler skips replayed events at or below the persisted aggregate sequence, so new historical projection logic cannot update an existing read model without resetting or versioning it."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: TenantProjectionHandler skips events at/below persisted aggregate sequence. event-contract-reference.md now explains global singleton replay only; no tenant projection schema-reset/versioned rebuild path was found for new historical tenant logic.
verification-files: docs/event-contract-reference.md
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Platform projection rebuild/versioning contract
next-action: Specify a safe tenant projection schema rebuild/version path and its rollout verification.
related: DW-636


### DW-605: Split tenant command model types into files named for each C# type.

origin: migrated from unnumbered ledger ("Deferred from: spec-gh-actions-28944933021-85877006572.md, continued (2026-10-10)"), 2026-10-12
location: src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`\n  summary: Split tenant command model types into files named for each C# type.\n  evidence: TenantCreateCommandModels.cs contains fourteen public records and enums, making individual models harder to navigate and violating the Hexalith one-type-per-file rule."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: TenantCreateCommandModels.cs still defines multiple public enums/records/snapshots in one file, contrary to the required Hexalith one-type-per-file rule.
verification-files: src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs
evidence-kind: verified-gap
priority: P3
owner: unassigned
dependencies: none
next-action: Plan a mechanical split into files named after each C# type without behavior changes.
related: none


### DW-606: Normalize affected C# files to the repository's CRLF line ending policy.

origin: migrated from unnumbered ledger ("Deferred from: spec-gh-actions-28944933021-85877006572.md, continued (2026-10-10)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryClient.cs; src/Hexalith.Tenants/Bootstrap/TenantBootstrapCredentialProvider.cs
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`\n  summary: Normalize affected C# files to the repository's CRLF line ending policy.\n  evidence: TenantsRestQueryClient.cs and TenantBootstrapCredentialProvider.cs are LF-only despite .editorconfig requiring CRLF, so editor normalization can create noisy diffs."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: Read-only byte inspection on 2026-10-12 found TenantsRestQueryClient.cs has 765 LF/0 CRLF, TenantBootstrapCredentialProvider.cs 179 LF/0 CRLF; .editorconfig requires CRLF for C#.
verification-files: src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryClient.cs; src/Hexalith.Tenants/Bootstrap/TenantBootstrapCredentialProvider.cs
evidence-kind: verified-gap
priority: P3
owner: unassigned
dependencies: Owning source formatting work
next-action: Normalize the affected C# files in a separate formatting-only change.
related: DW-605


### DW-607: Require partial-release evidence before recovery can publish a version.

origin: migrated from unnumbered ledger ("Deferred from: spec-gh-actions-28944933021-85877006572.md, continued (2026-10-10)"), 2026-10-12
location: scripts/validate-partial-release-recovery.sh
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`\n  summary: Require partial-release evidence before recovery can publish a version.\n  evidence: validate-partial-release-recovery.sh checks the source and CI run but has no guard requiring an existing package, container, tag, or release, so an entirely absent release can enter recovery."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: validate-partial-release-recovery.sh still permits all packages/container/tag/release absent after only source/CI checks; existing=0 merely writes package-state=partial. Recovery can publish an arbitrary absent version.
verification-files: scripts/validate-partial-release-recovery.sh
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: Partial-release eligibility policy
next-action: Require concrete existing publication evidence before admitting recovery, with an all-destinations-absent rejection case.
related: DW-588


### DW-608: Execute a mismatched existing-package hash test for partial-release recovery.

origin: migrated from unnumbered ledger ("Deferred from: spec-gh-actions-28944933021-85877006572.md, continued (2026-10-10)"), 2026-10-12
location: scripts/publish-partial-release.sh
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`\n  summary: Execute a mismatched existing-package hash test for partial-release recovery.\n  evidence: Existing tests do not run publish-partial-release.sh with a mismatched package hash, so removal of its comparison could let a mixed-source release publish without test failure."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: publish-partial-release.sh compares existing NuGet SHA256 hashes, but PackageGovernanceTests.Partial_release_recovery_is_protected_and_never_skips_duplicate_packages only inspects script text. No executable hash-mismatch case was found.
verification-files: scripts/publish-partial-release.sh
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Mock publication tools/state fixture
next-action: Execute the mismatch path and assert exit before any package/container/release publication.
related: DW-620


### DW-609: Clear or reauthorize retained tenant, audit, and membership snapshots when permissions change within the same user and tenant scope.

origin: migrated from unnumbered ledger ("Deferred from: spec-gh-actions-28944933021-85877006572.md, continued (2026-10-10)"), 2026-10-12
location: n/a
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`\n  summary: Clear or reauthorize retained tenant, audit, and membership snapshots when permissions change within the same user and tenant scope.\n  evidence: FrontComposer remounts on tenant or user identity transitions only; audit, detail, workspace, and membership components can retain read snapshots after a same-scope permission change."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: FrontComposer ScopeBoundaryService.SameScope compares only tenant/user identity; TenantQueryGateway audit CallerScope is user-derived, and list/membership retention remains unbound. Same-user permission changes do not universally invalidate all retained reads.
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Permission-change invalidation/reauthorization contract
next-action: Cover same-identity permission loss across detail/audit/list/membership and invalidate or reauthorize retained facts.
related: DW-580,DW-574


### DW-610: Guard membership paging against repeated Next events while a page read is in flight.

origin: migrated from unnumbered ledger ("Deferred from: spec-gh-actions-28944933021-85877006572.md, continued (2026-10-10)"), 2026-10-12
location: n/a
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`\n  summary: Guard membership paging against repeated Next events while a page read is in flight.\n  evidence: Both membership panels advance cursor history from the current snapshot without checking an in-flight read or HasMore in the event handler; a repeated event can push a loading cursor."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: MyTenantsPanel.NextPageAsync unconditionally pushes cursor; UserMembershipLookupPanel.NextPageAsync checks only snapshot non-null. Neither handler rejects loading/repeated Next or missing HasMore before advancing history.
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: Membership paging in-flight state
next-action: Guard Next with arrived-page/HasMore and an in-flight gate; cover repeated clicks during a delayed read.
related: none


### DW-611: Make user-membership lookup sorting consistent across cursor pages.

origin: migrated from unnumbered ledger ("Deferred from: spec-gh-actions-28944933021-85877006572.md, continued (2026-10-10)"), 2026-10-12
location: n/a
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`\n  summary: Make user-membership lookup sorting consistent across cursor pages.\n  evidence: The UI sorts only the current 20 rows while GetUserTenantsQuery carries no sort field, so page boundaries do not follow the selected order."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: UserMembershipLookupPanel.VisibleRows orders only current rows; GetUserTenantsQuery contains UserId/Cursor/PageSize and no sort contract. Selected order therefore does not span cursor boundaries.
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: Server/authorized membership ordering contract
next-action: Define globally ordered protected paging or explicitly constrain the UI sort promise to the current page.
related: DW-619


### DW-612: Replace legacy Fluent border variables in tenant metadata and member-removal CSS.

origin: migrated from unnumbered ledger ("Deferred from: spec-gh-actions-28944933021-85877006572.md, continued (2026-10-10)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor.css; src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor.css
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`\n  summary: Replace legacy Fluent border variables in tenant metadata and member-removal CSS.\n  evidence: Two components use --neutral-stroke-rest with CanvasText fallback while V5 components use --colorNeutralStroke1, so their borders can fail to follow the theme."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: EditTenantMetadataFlow.razor.css:6 and RemoveTenantMemberFlow.razor.css:4 still use --neutral-stroke-rest with CanvasText fallback; Hexalith UX and current Fluent components use V5 tokens.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor.css; src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor.css
evidence-kind: verified-gap
priority: P3
owner: unassigned
dependencies: Fluent V5 theme token mapping
next-action: Replace the two legacy border tokens with the appropriate Fluent V5 token.
related: none


### DW-613: Render a recovery surface for unmatched interactive UI routes.

origin: migrated from unnumbered ledger ("Deferred from: spec-gh-actions-28944933021-85877006572.md, continued (2026-10-10)"), 2026-10-12
location: n/a
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`\n  summary: Render a recovery surface for unmatched interactive UI routes.\n  evidence: Routes.razor defines only Found content; an unmatched interactive route has no in-app not-found message or navigation link."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: UI Components/Routes.razor still defines Router Found only and no NotFound recovery surface. Interactive unmatched-route recovery remains undocumented/unrendered.
evidence-kind: verified-gap
priority: P3
owner: unassigned
dependencies: Existing FrontComposer not-found conventions
next-action: Add a framework-conforming in-app unmatched-route message and safe recovery link.
related: DW-616


### DW-614: Verify the signed-in tenant claim alias through the real Tenants host.

origin: migrated from unnumbered ledger ("Deferred from: spec-gh-actions-28944933021-85877006572.md, continued (2026-10-10)"), 2026-10-12
location: n/a
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`\n  summary: Verify the signed-in tenant claim alias through the real Tenants host.\n  evidence: Program.cs selects eventstore:current-tenant, but current tests use anonymous requests, inspect endpoint metadata, or substitute the user context; none verifies a multi-tenant authenticated principal."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: UI Program.cs configures tenantClaimType eventstore:current-tenant; current host smoke tests are anonymous and other tests inspect source or substitute user context. No real-host multitenant authenticated alias assertion was found.
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Authenticated real Tenants host fixture
next-action: Issue a safe multi-tenant test principal and prove the selected current-tenant alias through the rendered host.
related: DW-597


### DW-615: Add a blocking anonymous Tenants-host scope-boundary test.

origin: migrated from unnumbered ledger ("Deferred from: spec-gh-actions-28944933021-85877006572.md, continued (2026-10-10)"), 2026-10-12
location: n/a
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`\n  summary: Add a blocking anonymous Tenants-host scope-boundary test.\n  evidence: The hosted smoke assertions use DaprFact and can skip, while the Aspire CI lane is nonblocking; component and source checks do not render an anonymous request through the Tenants host."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: TenantsUiRouteSmokeTests does render anonymous routes but remains DaprFact/IntegrationTests in advisory Aspire CI; current CI run 38269521776 executed it with zero skips, yet it is still not a blocking regression boundary.
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Blocking in-process host fixture
next-action: Add a mandatory anonymous host scope-denial lane without depending on the advisory topology tier.
related: DW-614,DW-572


### DW-616: Verify FrontComposer-owned routes through interactive Tenants router navigation.

origin: migrated from unnumbered ledger ("Deferred from: spec-gh-actions-28944933021-85877006572.md, continued (2026-10-10)"), 2026-10-12
location: n/a
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`\n  summary: Verify FrontComposer-owned routes through interactive Tenants router navigation.\n  evidence: Routes.razor includes the FrontComposer assembly, but existing tests do not navigate the interactive router to /home; fresh HTTP GET tests exercise separate endpoint mapping."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: Routes.razor includes the FrontComposer shell assembly; existing HTTP smoke checks do not perform interactive-router navigation to /home. No actual interactive Tenants router navigation coverage was found.
evidence-kind: verified-gap
priority: P3
owner: unassigned
dependencies: Interactive host/browser fixture
next-action: Navigate the running interactive router to a FrontComposer-owned route and assert correct shell content.
related: DW-613


### DW-617: Reconcile global administrator revocation with the freshness of the authorization read model.

origin: migrated from unnumbered ledger ("Deferred from: spec-gh-actions-28944933021-85877006572.md, continued (2026-10-10)"), 2026-10-12
location: n/a
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`\n  summary: Reconcile global administrator revocation with the freshness of the authorization read model.\n  evidence: TenantsGlobalAdministratorVerifier accepts membership from the global-administrator read model without a sequence or freshness check, so a projection lag can retain revoked command authority."
status: open
disposition: awaiting-decision
verification-date: 2026-10-12
verification: TenantsGlobalAdministratorVerifier still checks read-model membership with no version/freshness boundary. Projection lag can retain authorization after removal; this duplicates the broader command/query authority concern.
evidence-kind: verified-gap
priority: P1
owner: unassigned
dependencies: DW-562 revocation contract decision
next-action: Resolve the shared authoritative/version-bounded revocation contract through DW-562.
related: DW-562,DW-564
duplicate-of: DW-562


### DW-618: Make authorized search paging both complete and private when index windows contain hidden tenants.

origin: migrated from unnumbered ledger ("Deferred from: spec-gh-actions-28944933021-85877006572.md, continued (2026-10-10)"), 2026-10-12
location: n/a
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`\n  summary: Make authorized search paging both complete and private when index windows contain hidden tenants.\n  evidence: A fully forbidden or absent window ends paging before later authorized hits, while a partly hidden window derives HasMore from the raw index total and can reveal additional raw results."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: TenantQueryGateway.BuildAuthoritativeSearchSnapshotAsync explicitly ends paging for an entirely hidden window and derives partial-window HasMore from raw index total. Later authorized hits can be inaccessible, and partial windows retain the recorded paging disclosure.
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: Authorization-safe index paging design
next-action: Design bounded authorized paging that continues through hidden windows without exposing raw-result pagination facts.
related: DW-619


### DW-619: Make tenant search sorting consistent across page boundaries.

origin: migrated from unnumbered ledger ("Deferred from: spec-gh-actions-28944933021-85877006572.md, continued (2026-10-10)"), 2026-10-12
location: n/a
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`\n  summary: Make tenant search sorting consistent across page boundaries.\n  evidence: SortSearchRows orders each hydrated index window after page selection, so later pages can contain names or statuses that should precede rows on earlier pages."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: TenantQueryGateway.SortSearchRows sorts only the hydrated raw window after Memories selects page boundaries; name/status ordering is not global across protected search pages.
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: Authorized search ordering contract
next-action: Move ordering before page selection or specify an honest current-window sorting UX.
related: DW-611,DW-618


### DW-620: Verify the contents of existing GitHub Release package assets during partial-release recovery.

origin: migrated from unnumbered ledger ("Deferred from: spec-gh-actions-28944933021-85877006572.md, continued (2026-10-10)"), 2026-10-12
location: scripts/verify-partial-release.sh; scripts/publish-partial-release.sh
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`\n  summary: Verify the contents of existing GitHub Release package assets during partial-release recovery.\n  evidence: Final verification checks release asset names and count but not hashes, and the publisher leaves existing release assets untouched, so different binaries can pass."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: verify-partial-release.sh validates exactly five expected release asset names but never their hashes; publish-partial-release.sh skips assets on existing releases. Matching names can conceal different binaries.
verification-files: scripts/verify-partial-release.sh; scripts/publish-partial-release.sh
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Immutable expected package hashes; recovery asset inventory
next-action: Download existing release assets safely and compare each content hash with the reviewed package manifest.
related: DW-581,DW-608,DW-583


### DW-621: Mark recovered prerelease versions as prereleases in GitHub Releases.

origin: migrated from unnumbered ledger ("Deferred from: spec-gh-actions-28944933021-85877006572.md, continued (2026-10-10)"), 2026-10-12
location: scripts/publish-partial-release.sh
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`\n  summary: Mark recovered prerelease versions as prereleases in GitHub Releases.\n  evidence: The recovery publisher calls gh release create without --prerelease for a version containing a prerelease suffix, leaving GitHub to mark it stable."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: publish-partial-release.sh gh release create has no --prerelease flag, while validator accepts SemVer prerelease suffixes. Recovery can mark a prerelease GitHub version stable.
verification-files: scripts/publish-partial-release.sh
evidence-kind: verified-defect
priority: P2
owner: unassigned
dependencies: SemVer prerelease classification
next-action: Set and verify GitHub prerelease metadata for suffix-bearing versions.
related: none


### DW-622: Test the effective Tenants UI read endpoint in the AppHost resource model.

origin: migrated from unnumbered ledger ("Deferred from: spec-gh-actions-28944933021-85877006572.md, continued (2026-10-10)"), 2026-10-12
location: n/a
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md`\n  summary: Test the effective Tenants UI read endpoint in the AppHost resource model.\n  evidence: Existing tests inspect source text for Tenants__BaseAddress but do not evaluate the UI resource environment; a later override could redirect authorized reads to EventStore while the source-text checks pass."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: AppHost Program.cs sets UI Tenants__BaseAddress to tenantsApi HTTPS, but AppHostSecurityResourceGraphTests evaluates only authentication/token environments, not the UI effective read endpoint. Source-text wiring checks cannot catch later overrides.
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Evaluated AppHost environment fixture
next-action: Assert the effective tenants-ui Tenants__BaseAddress resolves to the expected tenants-api endpoint.
related: none


### DW-623: Verify whether BFF submit-time and EventStore event-time clock skew can hide an exact global-administrator correction audit proof.

origin: migrated from unnumbered ledger ("Deferred from: spec-gh-actions-28944933021-85877006572.md, continued (2026-10-10)"), 2026-10-12
location: n/a
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md`\n  summary: Verify whether BFF submit-time and EventStore event-time clock skew can hide an exact global-administrator correction audit proof.\n  evidence: The BFF records SubmittedAtUtc while EventStore stamps the event independently; measured production skew or a shared timestamp ordering guarantee is needed to establish whether the current post-submit boundary can exclude a valid exact-correlation event."
status: open
disposition: uncertain
verification-date: 2026-10-12
verification: GlobalAdministratorAuditProofLookup.FindAsync queries From=submittedAtUtc and requires row.Timestamp>=submittedAtUtc; BFF submit time and EventStore event time use independent clocks. Missing evidence: supported clock-order guarantee or measured deployment skew bounds.
evidence-kind: unverified-risk
priority: P2
owner: unassigned
dependencies: Timestamp/provenance contract; clock synchronization evidence
next-action: Establish the clock boundary or select a safe proof lookup policy grounded in exact correlation/sequence.
related: DW-565


## Deferred from: code review of spec-5-7-correct-global-administrator-authority-from-audit-evidence.md (2026-10-11)

Diff reviewed: Tenants `66072cc4..362a957d`. The diff was 4,351 lines (260,201 bytes; SHA-256 `80873fa0559d09b80c66f547286074324c610c366b9f7f069dcb05e37c6a000b`) and is not archived. Triage produced 1 decision, 21 patches, 3 defers and 20 rejected. Two of the defers are new entries below. The third, BFF and EventStore clock skew, is already recorded above.

### DW-624: unverified medium; `GlobalAdministratorProjectionHandler` takes the projection and `audit:system` version from the last event iterated, not the highest sequence. Owner: Tenants global-administrator projection.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-7-correct-global-administrator-authority-from-audit-evidence.md (2026-10-11)"), 2026-10-12
source_spec: `_bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md`
priority: P2
archived: 2026-10-12

### DW-625: medium; an ambiguous global-administrator delivery that actually committed can never be redelivered or resolved in the same circuit. Owner: global-administrator command lifecycle (Stories 4.2/4.3 policy, reused by 5.7).

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-7-correct-global-administrator-authority-from-audit-evidence.md (2026-10-11)"), 2026-10-12
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md`\n  summary: medium; an ambiguous global-administrator delivery that actually committed can never be redelivered or resolved in the same circuit. Owner: global-administrator command lifecycle (Stories 4.2/4.3 policy, reused by 5.7).\n  evidence:\n  - **Code:** `GlobalAdministratorCorrectionPanel.RetryAmbiguousDeliveryAsync` (`:1349-1383` restore, `:1384-1420` removal) and `GlobalAdministratorsPage` `WithdrawDeliveryRetry` withdraw same-ULID redelivery once the projection no longer matches the preview.\n  - **Consequence:** a POST that committed but returned an ambiguous result changes that projection. The attempt then stays ambiguous without a correlation, and the fixed-aggregate admission lease stays held until a new circuit.\n  - **To settle:** choose between two fixes: (a) idempotent same-MessageId redelivery that is allowed after preconditions change, or (b) a MessageId-keyed status lookup. Either must keep the \"never dispatch already-applied work\" boundary."
status: open
disposition: awaiting-decision
verification-date: 2026-10-12
verification: GlobalAdministratorCorrectionPanel.RetryAmbiguousDeliveryAsync still requires the original grant/removal preview to match current projection; GlobalAdministratorsPage similarly withdraws delivery retries. An actually committed ambiguous request can change that projection while lacking correlation. Existing no-already-applied-dispatch policy is preserved.
evidence-kind: verified-defect
priority: P2
owner: global-administrator command lifecycle (Stories 4.2/4.3 policy, reused by 5.7)
dependencies: Platform MessageId lookup/idempotent redelivery capability
next-action: Choose an authorized MessageId status lookup or compatible same-ID resolution route that preserves the settled dispatch boundary.
related: none


## Deferred from: Story 5.7 review pass 3 (2026-10-11)

### DW-626: Separate or safely merge the system tenant audit stream and global-administrator audit stream.

status: done 2026-10-13
origin: migrated from unnumbered ledger ("Deferred from: Story 5.7 review pass 3 (2026-10-11)"), 2026-10-12
source_spec: `_bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md`
priority: P2
archived: 2026-10-14

### DW-627: Establish whether concurrent global-administrator projection rebuilds can overwrite newer state.

origin: migrated from unnumbered ledger ("Deferred from: Story 5.7 review pass 3 (2026-10-11)"), 2026-10-12
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md`\n  summary: Establish whether concurrent global-administrator projection rebuilds can overwrite newer state.\n  evidence: The existing full-rebuild handler uses `SaveAsync` without a version guard. If EventStore allows concurrent projection requests for this aggregate, an older request finishing last can replace newer state; an upstream serialization guarantee would settle reachability."
status: open
disposition: uncertain
verification-date: 2026-10-12
verification: GlobalAdministratorProjectionHandler uses unguarded SaveAsync. EventStore ProjectionUpdateOrchestrator has per-aggregate static keyed locks, proving in-process serialization only. Missing evidence: cross-replica/rebuild delivery serialization for the global singleton and domain /project requests.
evidence-kind: unverified-risk
priority: P2
owner: unassigned
dependencies: Platform distributed delivery/serialization guarantee
next-action: Trace all supported delivery/rebuild routes across replicas and document a guarantee or reproduce stale overwrite.
related: DW-624


## Deferred from: code review of spec-5-7-correct-global-administrator-authority-from-audit-evidence.md, review-fix range `362a957d..4f0abcae` (2026-10-11)

### DW-628: unverified medium; the documented global-administrator projection replay is the trigger for the existing `audit:system` collision entry above ("Separate or safely merge the system tenant audit stream…"). Owner: Tenants global-administrator projection and rollout docs.

status: done 2026-10-13
origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-7-correct-global-administrator-authority-from-audit-evidence.md, review-fix range `362a957d..4f0abcae` (2026-10-11)"), 2026-10-12
source_spec: `_bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md`
priority: P2
archived: 2026-10-14

## Deferred from: independent module manifest review of `711e100858e59cf8de2b626ea21790d59f969023` (Review 6, 2026-10-11)

### DW-629: Verify the independently committed module manifest's `tenants-events` topic against the deployed `tenants.events` topic.

origin: migrated from unnumbered ledger ("Deferred from: independent module manifest review of `711e100858e59cf8de2b626ea21790d59f969023` (Review 6, 2026-10-11)"), 2026-10-12
location: module/hexalith.module-manifest.v2.json
source_spec: `_bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md`\n  summary: Verify the independently committed module manifest's `tenants-events` topic against the deployed `tenants.events` topic.\n  evidence: Review 6 found that the manifest added by `711e100858e59cf8de2b626ea21790d59f969023` names `tenants-events`, while AppHost publishes `tenants.events`; the manifest schema's topic naming semantics need confirmation before changing it."
status: open
disposition: uncertain
verification-date: 2026-10-12
verification: module/hexalith.module-manifest.v2.json integration.topics names tenants-events while AppHost routing uses tenants.events. Missing evidence: owning v2 schema/consumer definition specifying whether name is a literal pubsub topic or an inventory identifier.
verification-files: module/hexalith.module-manifest.v2.json
evidence-kind: unverified-risk
priority: P2
owner: unassigned
dependencies: Module manifest v2 semantics owner
next-action: Obtain the schema topic-name contract and compare runtime wiring before changing manifest content.
related: DW-630


### DW-630: Audit the independently committed module manifest's tenant API operation inventory.

origin: migrated from unnumbered ledger ("Deferred from: independent module manifest review of `711e100858e59cf8de2b626ea21790d59f969023` (Review 6, 2026-10-11)"), 2026-10-12
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md`\n  summary: Audit the independently committed module manifest's tenant API operation inventory.\n  evidence: Review 6 found existing audit, member, lifecycle, and configuration endpoints absent from the manifest added by `711e100858e59cf8de2b626ea21790d59f969023`; confirm whether the schema requires exhaustive operations."
status: open
disposition: uncertain
verification-date: 2026-10-12
verification: Manifest tenant-api operations still lists only create/list/get/update despite decorated audit/member/configuration/lifecycle contracts. Missing evidence: the v2 schema/consumer requirement for exhaustive versus representative operation inventory.
evidence-kind: recommendation
priority: P2
owner: unassigned
dependencies: Module manifest v2 inventory contract
next-action: Confirm required inventory completeness, then map every required decorated operation.
related: DW-629,DW-631


### DW-631: Decide whether the manifest's global-administrator critical flow should include removal.

origin: migrated from unnumbered ledger ("Deferred from: independent module manifest review of `711e100858e59cf8de2b626ea21790d59f969023` (Review 6, 2026-10-11)"), 2026-10-12
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md`\n  summary: Decide whether the manifest's global-administrator critical flow should include removal.\n  evidence: Review 6 found removal declared as an operation but omitted from the critical flow in the independent `711e100858e59cf8de2b626ea21790d59f969023` manifest; determine whether that flow is intended as a representative subset."
status: open
disposition: awaiting-decision
verification-date: 2026-10-12
verification: Manifest declares remove-global-administrator as an operation but global-administrator-management criticalFlows includes only list and set. No local decision says whether a critical flow is representative or lifecycle-complete.
evidence-kind: recommendation
priority: P3
owner: unassigned
dependencies: Manifest critical-flow scope decision
next-action: Decide whether removal belongs in the critical flow and record the chosen inventory rule.
related: DW-630


### DW-632: Add authentication tests for the independently committed manifest configuration fallbacks.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: independent module manifest review of `711e100858e59cf8de2b626ea21790d59f969023` (Review 6, 2026-10-11)"), 2026-10-12
source_spec: `_bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md`
priority: P2
archived: 2026-10-12

## Deferred from: Story 5.7 Review 8 fix-pass triage (2026-10-11)

### DW-633: Reconcile the independent authentication fallback deferred entry with its now-present alias-only API tests.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: Story 5.7 Review 8 fix-pass triage (2026-10-11)"), 2026-10-12
source_spec: `_bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md`
priority: P3
archived: 2026-10-12

## Deferred from: Story 5.7 Review 9 triage (2026-10-12)

### DW-634: medium; make the disabled-bootstrap test deterministically prove no deferred command is registered.

origin: migrated from unnumbered ledger ("Deferred from: Story 5.7 Review 9 triage (2026-10-12)"), 2026-10-12
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md`\n  summary: medium; make the disabled-bootstrap test deterministically prove no deferred command is registered.\n  evidence: `StartAsyncWithDisabledBootstrapPreservesConfiguredAdministrator` in `tests/Hexalith.Tenants.Server.Tests/Bootstrap/TenantBootstrapHostedServiceTests.cs:30`, independently committed in `6b6a35f0`, waits with `Task.Delay(50)` at `:52` for absence of HTTP traffic, but the enabled path schedules its HTTP work with `Task.Run` after `ApplicationStarted`; a late task could escape the assertion."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: TenantBootstrapHostedServiceTests.StartAsyncWithDisabledBootstrapPreservesConfiguredAdministrator still asserts no HTTP after Task.Delay(50), while enabled service work is deferred after ApplicationStarted. The timing-based negative proof remains.
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Deterministic host-lifetime/callback fixture
next-action: Assert no deferred callback/work registration for disabled bootstrap rather than waiting for absent traffic.
related: none


### DW-635: medium; record current global-administrator membership in the independent module manifest authorization inventory.

origin: migrated from unnumbered ledger ("Deferred from: Story 5.7 Review 9 triage (2026-10-12)"), 2026-10-12
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md`\n  summary: medium; record current global-administrator membership in the independent module manifest authorization inventory.\n  evidence: `module/hexalith.module-manifest.v2.json`, as changed by `6b6a35f0`, lists `global-administrators-api` authorization policy `authenticated` at `:239` and operation `authorizationPolicy` `authenticated` at `:247`, `:252` and `:257`, while `GetGlobalAdministratorsQueryHandler` rejects authenticated users absent from the singleton administrator projection."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: Manifest global-administrators-api surface/operations still declare authenticated only, while GetGlobalAdministratorsQueryHandler requires current singleton administrator membership. Manifest authorization inventory omits this additional runtime gate.
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Manifest authorization inventory semantics
next-action: Record the current-global-administrator condition using the owning schema authorization representation.
related: DW-630


### DW-636: low; decide how the independent module manifest should expose the singleton projection replay prerequisite.

origin: migrated from unnumbered ledger ("Deferred from: Story 5.7 Review 9 triage (2026-10-12)"), 2026-10-12
location: docs/event-contract-reference.md
source_spec: `_bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md`\n  summary: low; decide how the independent module manifest should expose the singleton projection replay prerequisite.\n  evidence: `module/hexalith.module-manifest.v2.json`, as changed by `6b6a35f0`, leaves `recoveryHooks` (`:417`) and `recoveryInventory` (`:446`) empty even though the replay prerequisite at `docs/event-contract-reference.md:127` requires replay of persisted singleton projections before correction becomes available; the appropriate manifest hook or inventory entry needs an owning manifest follow-up."
status: open
disposition: awaiting-decision
verification-date: 2026-10-12
verification: Manifest recoveryHooks and recoveryInventory remain empty while event-contract-reference.md:127 requires persisted singleton replay before audit correction. The appropriate supported manifest recovery representation is unspecified.
verification-files: docs/event-contract-reference.md
evidence-kind: recommendation
priority: P3
owner: unassigned
dependencies: Manifest recovery-hook/inventory contract
next-action: Choose the supported representation for the documented singleton replay prerequisite.
related: DW-604,DW-628


## Deferred from: code review of spec-5-7-correct-global-administrator-authority-from-audit-evidence.md, fix-pass range `44878a58..2315aad9` (2026-10-12)

### DW-637: medium; pin the authentication-state unsubscribe on disposal of `GlobalAdministratorCorrectionPanel` and `GlobalAdministratorsPage`.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-7-correct-global-administrator-authority-from-audit-evidence.md, fix-pass range `44878a58..2315aad9` (2026-10-12)"), 2026-10-12
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md`\n  summary: medium; pin the authentication-state unsubscribe on disposal of `GlobalAdministratorCorrectionPanel` and `GlobalAdministratorsPage`.\n  evidence: Pre-existing test gap: both subscriptions exist at Story 5.7 baseline `66072cc4`, and fix pass `2315aad9` closed only the `TenantAuditPage` equivalent. In an isolated checkout of `2315aad9`, deleting `AuthenticationStateChanged -= OnAuthenticationStateChanged` at `src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor:2367`, or at `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:2397`, leaves the UI suite at 4,075/4,075 green. A regression would keep disposed components referenced by the circuit's provider, and the panel handler (`:2026`) would run `RetainReconciliationForReplacement()` and `InvalidateOperation()` after disposal. Suggested shape: the `TenantAuditPage` dispose test from `2315aad9` — after disposal raise a principal change and assert the handler's counter is unchanged (`_authorizationGeneration` on the panel, `_authorizationTransitionVersion` on the page)."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: GlobalAdministratorCorrectionPanel.Dispose and GlobalAdministratorsPage.DisposeAsync unsubscribe AuthenticationStateChanged, but inspected component tests lack a post-disposal principal-change assertion for their counters. TenantAuditPageTests has the equivalent explicit disposal regression. Original mutation evidence is historical, not rerun.
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Observable authentication test provider
next-action: Add post-disposal provider-notification regressions for both components that fail if unsubscription is removed.
related: none


## Deferred from: Story 5.7 Review 10 triage (2026-10-12)

### DW-638: unverified medium; a global-administrator event persisted at sequence 0 cannot be paired from audit evidence.

status: done 2026-10-12
origin: migrated from unnumbered ledger ("Deferred from: Story 5.7 Review 10 triage (2026-10-12)"), 2026-10-12
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md`
priority: P2
archived: 2026-10-12

### DW-639: low; the missing signing-key exception names only the legacy EventStore setting.

origin: migrated from unnumbered ledger ("Deferred from: Story 5.7 Review 10 triage (2026-10-12)"), 2026-10-12
location: n/a
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md`\n  summary: low; the missing signing-key exception names only the legacy EventStore setting.\n  evidence: `src/Hexalith.Tenants.Api/Program.cs:60` throws that `EventStore:Authentication:SigningKey` is required when neither that value nor `Jwt:SigningKey` is configured. `FirstConfigured` was introduced by the independent bootstrap commit `6b6a35f0`."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: API Program.cs missing-key exception still names only EventStore:Authentication:SigningKey even though FirstConfigured also accepts Jwt:SigningKey; generated-controller invalid-key tests pin the legacy-only wording.
evidence-kind: verified-gap
priority: P3
owner: unassigned
dependencies: Supported auth configuration aliases
next-action: Name both accepted configuration alternatives in the safe exception and align message tests.
related: DW-632


### DW-640: medium; the public API readiness registration does not prove its three-second gateway timeout.

origin: migrated from unnumbered ledger ("Deferred from: Story 5.7 Review 10 triage (2026-10-12)"), 2026-10-12
location: n/a
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md`\n  summary: medium; the public API readiness registration does not prove its three-second gateway timeout.\n  evidence: `src/Hexalith.Tenants.Api/Program.cs:83` registers `eventstore-gateway` with `TimeSpan.FromSeconds(3)`, and `tests/Hexalith.Tenants.IntegrationTests/TenantsApiReadinessTests.cs` returns a completed response without reading `HealthCheckRegistration.Timeout`. Dropping the timeout argument leaves that test green. The registration comes from the independent API commit `6b6a35f0`."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: API Program.cs registers eventstore-gateway timeout=3 seconds; TenantsApiReadinessTests uses completed stub responses and never inspects HealthCheckRegistration.Timeout or deadline behavior. Timeout removal is not covered.
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Health-check registration/test fixture
next-action: Assert the effective registration timeout or exercise a cancellable stalled gateway health check.
related: none


### DW-641: medium; legacy authentication settings are not tested against a different non-blank alias.

origin: migrated from unnumbered ledger ("Deferred from: Story 5.7 Review 10 triage (2026-10-12)"), 2026-10-12
location: n/a
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md`\n  summary: medium; legacy authentication settings are not tested against a different non-blank alias.\n  evidence: `FirstConfigured` in `src/Hexalith.Tenants.Api/Program.cs:104` keeps a non-blank legacy value over `Oidc:Authority` or `Jwt:SigningKey`. The alias tests in `tests/Hexalith.Tenants.IntegrationTests/TenantsApiGeneratedControllerTests.cs` set only the alias, or a blank legacy value. Those settings come from `6b6a35f0`."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: Program.FirstConfigured preserves nonblank legacy value; generated-controller tests cover alias-only and blank-legacy fallback but no different nonblank legacy and alias values. Alias coverage added earlier does not cover precedence.
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Conflicting auth configuration fixture
next-action: Test distinct nonblank legacy/alias authority and signing keys and assert legacy precedence.
related: DW-632


## Deferred from: code review of spec-5-7-correct-global-administrator-authority-from-audit-evidence.md, range `2315aad9..9345d9d0` (2026-10-12)

### DW-642: unverified medium; the Global Administrators page holds the fixed-aggregate lease for a `Completed` correction with no valid committed sequence, while the correction panel now releases the same status.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-7-correct-global-administrator-authority-from-audit-evidence.md, range `2315aad9..9345d9d0` (2026-10-12)"), 2026-10-12
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md`\n  summary: unverified medium; the Global Administrators page holds the fixed-aggregate lease for a `Completed` correction with no valid committed sequence, while the correction panel now releases the same status.\n  evidence: Pre-existing on the page: `IsPermanentCorrectionBaselineFailure` and `CanConfirmCorrectionProjection` in `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor` (`:5256`, `:5292`) both require `CommittedEventSequence is > 0`, so an adopted correction whose status is `Completed` with events but no sequence stays `ProjectionPending` with the lock held. The panel's key-only `IsPermanentCommandProofFailure` (`src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor:893`), added in `9345d9d0`, releases that state. `TenantCommandGateway.cs:710-718` returns a null sequence with verified identity only when the status record breaks its own invariants. What would settle it: whether EventStore can return `Completed` with events but no valid `CommittedEventSequence` for the global-administrators aggregate."
status: open
disposition: uncertain
verification-date: 2026-10-12
verification: GlobalAdministratorsPage.IsPermanentCorrectionBaselineFailure/CanConfirmCorrectionProjection still require committed sequence>0; panel now releases malformed completed proof. EventStore status writing conditionally emits sequence only with verified range proof. Missing evidence: whether Completed+events+null sequence is reachable in supported global command/recovery states rather than corrupted status.
evidence-kind: unverified-risk
priority: P2
owner: unassigned
dependencies: Platform completed-status invariant/recovery proof
next-action: Trace verified terminal/recovery producers and add a reachable malformed-status regression only after establishing the contract.
related: none


## Deferred from: code review of spec-5-7-correct-global-administrator-authority-from-audit-evidence.md, range `9345d9d0..6a81aee1` (2026-10-12)

### DW-643: medium; the story gitlink guard ignores pointer moves in unrecorded commits once a story records a Story-Owned Gitlink Commits section.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-7-correct-global-administrator-authority-from-audit-evidence.md, range `9345d9d0..6a81aee1` (2026-10-12)"), 2026-10-12
location: scripts/validate-story-gitlinks.py
source_spec: `_bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md`\n  summary: medium; the story gitlink guard ignores pointer moves in unrecorded commits once a story records a Story-Owned Gitlink Commits section.\n  evidence: In that branch, `check()` in `scripts/validate-story-gitlinks.py` (`:412-457`) validates only the listed commits' chain, their stated targets and working-tree movement, and never compares baseline pointers with HEAD. A later unrecorded pointer-moving commit therefore passes, which is how Story 5.7's `6869ec4a` and `3440ff01` passed the `2315aad9..9345d9d0` review. Pre-existing tooling, not changed by the reviewed range; at `6a81aee1` all five pointer-moving commits are recorded and net endpoints match. Fix: in that branch, also fail when `gitlink_changes(baseline, head)` has a path whose HEAD pointer differs from its last owned target, or from the baseline when the path has no owned target."
status: open
disposition: actionable
verification-date: 2026-10-12
verification: validate-story-gitlinks.py check owned_commits branch still verifies listed commit chains/claims/worktree only and explicitly excludes later committed moves; it does not compare baseline-to-HEAD endpoints to final owned targets. Historical regression results do not prove unrecorded moves are rejected.
verification-files: scripts/validate-story-gitlinks.py
evidence-kind: verified-gap
priority: P2
owner: unassigned
dependencies: Story-owned commit guard policy; later independent-pointer attribution
next-action: Cover an unrecorded later pointer-moving commit and enforce the approved endpoint/attribution rule.
related: DW-585
### DW-644: A non-lease pre-dispatch freshness refusal cannot resume after freshness becomes current.

status: done 2026-10-17
priority: P2
archived: 2026-10-17

### DW-645: A throwing pre-dispatch activity-release callback leaves removal recovery without retained ownership.

status: done 2026-10-17
priority: P2
archived: 2026-10-17

### DW-646: A refused failed-delivery removal retry retains its prior message identity only in the live renderer.

status: done 2026-10-17
priority: P2
archived: 2026-10-17

### DW-647: A freshness or confirmation change during removal admission can announce unavailable authority instead of its actual refusal reason.

- source_spec: `_bmad-output/implementation-artifacts/spec-dw-645-predispatch-release-recovery.md`
  summary: Preserve accurate localized refusal feedback when current safety changes invalidate a held removal admission.
  evidence: Independent DW-645 review traced freshness/confirmation invalidation of admission generation to ReauthorizeAdmissionAsync returning false and SubmitAsync selecting AuthorizationUnavailableReason even while authority remains authorized. The reason-selection logic is unchanged from canonical entry e667d5391dae306e631dec23acd191863e54130c.
pre-dw-647-status: open
status: done 2026-10-17
pre-dw-647-disposition: actionable
disposition: resolved
pre-dw-647-verification-date: 2026-10-17
verification-date: 2026-10-17
pre-dw-647-verification: Source-verified generation invalidation and reason selection at entry and current source. Existing held-admission regressions prove no dispatch and recoverability but do not pin the exact explanation. No wrong-copy EN/FR execution or correction was performed; preserve that limit.
verification: Executed 50 new EN/FR held-admission, held-authority, held-cleanup and same-ID retry cases; final Debug source build has zero warnings/errors, focused 430/430 and full UI 6124/6124 pass with zero failures/skips. Independent review and both correction rechecks accept all five findings. Exact commands, initial failures and limits are retained in dw-647-validation-evidence.md.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor; tests/Hexalith.Tenants.UI.Tests/Components/RemoveTenantMemberFlowTests.cs
pre-dw-647-evidence-kind: verified-defect
evidence-kind: executed-regression
priority: P2
owner: Tenants UI
dependencies: Current safety reason versus invalidated admission-generation feedback
pre-dw-647-next-action: Assert shipped EN/FR freshness/confirmation refusal feedback during held admission and distinguish changed safety evidence from genuinely denied or indeterminate authority without weakening reauthorization.
next-action: Closed within DW-647; existing release qualification holds and unrelated deferred work remain unchanged.
related: DW-645, DW-644
resolution-evidence: [Exact validation, initial failures and independent review](dw-647-validation-evidence.md)
resolution-spec: spec-dw-647-removal-admission-feedback.md
