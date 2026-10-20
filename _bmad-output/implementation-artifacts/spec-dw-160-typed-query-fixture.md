---
title: 'DW-160: Replace the retired gateway test transport'
type: 'refactor'
created: '2026-10-20'
status: 'done'
route: 'oneshot'
review_loop_iteration: 1
baseline_commit: '84f2e8c7da6f2d7ae05412c478a94d21f83ab5c5'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** DW-160 is the next dependency-free actionable Tenants maintenance item after DW-158. The gateway test fixture still translates typed REST reads into a retired generic EventStore transport, fabricates request envelopes and injects transport exceptions that bypass native failure normalization.

**Approach:** Replace the obsolete capturing client and adapter with NSubstitute instances of ITenantsRestQueryClient. Assert actual typed requests and validators, return native response/failure contracts, preserve the existing behavioral cases and precise metadata, and close only DW-160 after focused/full UI qualification and independent review. Commit and push to main as requested.

</frozen-after-approval>

## Implementation Notes

- Entry revision: `84f2e8c7da6f2d7ae05412c478a94d21f83ab5c5`, clean main. Freeform maintenance without a sprint story key. User authorizes completion, commit and push on main.
- Read baseline/Git/UX guidance, build/test configuration and architecture AD-6/AD-11. Independent investigation finds about 175 affected test methods and 85 recorded-call uses in `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs`.
- No unresolved intent decision, production change, public API, dependency update or migration. The footprint is one existing test file plus this spec, validation evidence, ledger and index; repetitive local fixture edits use the oneshot route.
- Reuse the existing native CreateGateway overload. Private queue helpers may configure the six substitute methods; they must return native response objects and observe NSubstitute's actual calls, without serialization, fabricated request records or failure-status translation. Preserve existing success defaults (unknown lifecycle, absent projection version and etag), explicit/missing metadata, supported/invalid 304 inputs, all safety assertions, and concurrency/cancellation synchronization.
- Convert failure injections to explicit native failure kinds/statuses, retaining genuine unexpected-exception containment cases. Keep existing direct failure-mapping tests and actual HTTP client tests. Compare test method inventory before/after; run Debug source build, gateway class and complete UI project, scoped formatting and story-gitlink checks. Have a context-free reviewer inspect the complete change before closure.
- The isolated Aspire baseline reports all 23 resources Running/Healthy. The bounded Error sample includes existing Memories/EventStore HTTP 500 retry failures, so no clean end-to-end topology claim is made. The started Tenants AppHost stops successfully; pre-existing AppHosts are preserved.
- The migration removes both obsolete classes, the synthetic SubmittedQuery record and generic gateway overloads. Local helpers configure actual NSubstitute methods and return native response contracts; call assertions inspect NSubstitute's received arguments. Native failure kinds are explicit theory inputs, not reconstructed HTTP transport failures. All 247 async test methods remain.
- Initial build attempts expose four migration compile errors, followed by one correction-script signature error. The corrected Debug source build succeeds with zero warnings/errors. Initial focused acceptance runs 708 cases: 705 pass and three fail. Two negative-cursor inputs were mistakenly converted to InvalidCursor; independent review also identifies both. The third expects only the search fallback event, but the native response correctly raises the direct-read failure event too. Both inputs and the complete event/message assertions are corrected before final acceptance.
- Corrected focused acceptance passes 708/708 with zero failures/skips. Independent review also identifies hostile-body and unexpected-exception redaction coverage that must survive at their actual seams, a positive member 304 with mismatched validator, and two malformed-metadata tests relying on queue exhaustion for failed refetch. These corrections extend the test-only footprint to `TenantsRestQueryClientTests.cs`.
- Two HTTP cases preserve unrecognized-reason versus unsafe-detail behavior for list/global-administrator reads; the existing HTTP redaction theory carries the original hostile body. The existing foreign-prior exception case now carries hostile text and asserts safe error copy. Positive member retention uses matching validators. Deliberately malformed gateway contracts remain explicit defense tests; missing-metadata cases now arrange native 503 fallback and verify exact refetch count, null validator and consumed responses.

## Review Triage Log

- Medium / patch: list negative-cursor fixture accidentally supplied InvalidCursor. Restore native InvalidRequest; initial focused run reproduces the unwanted second read.
- Medium / patch: global-administrator negative-cursor fixture has the same mistake. Restore InvalidRequest; initial focused run reproduces the unwanted second read.
- Low / patch: native read failures also emit DirectTenantsReadFailed before fallback diagnostics. Assert both ordered events/messages and null logged exceptions.
- Medium / patch: dropping raw Problem Details removed the hostile non-cursor reason/detail scenario. Move it to two actual HTTP client cases, retaining native negative-cursor gateway assertions.
- Medium / patch: status-theory redaction assertions had no hostile input after migration. Remove those vacuous assertions; preserve hostile-body proof in HTTP fixed-category tests and hostile exception redaction in an existing gateway test.
- Low / patch: positive member 304 retention used a different response validator that the real client rejects. Use matching members-old; the existing real HTTP exact-validator theory retains mismatch refusal coverage.
- Medium / patch: two missing-metadata 304 cases could treat queue exhaustion as an operational refetch failure. Arrange an explicit native Unavailable/503 fallback and assert call count, null validator and queue consumption.

- Recheck correction: executed reviewed acceptance passes 706/708; both new two-call assertions fail because the missing-metadata rows make exactly one read. Source confirms matching retained evidence returns at !HasSupportedProjectionVersion without refetch. The reviewer's seventh claim was incorrect. Remove the unused fallback and instead assert one actual call and consumed responses. The earlier acceptance/recheck statement is superseded for finding seven; preserve the history. The HTTP client class passes 122/122, including both added hostile-body cases.
- Finding seven revised verdict: false. ListTenantsCoreAsync/GetGlobalAdministratorsCoreAsync return retained degradation before refetch for matching snapshots without supported projection version. Executed one-call observations disprove queue-exhaustion causation; exact one-call and queue-consumption assertions now qualify this path.
- Final reviewed acceptance passes: Debug source build has zero warnings/errors; gateway 708/708 and full UI 6582/6582 pass with zero failures/skips. The HTTP client class also passed 122/122, and the final full run includes both new HTTP cases. Scoped formatting and own-spec gitlink checks pass. The independent reviewer accepts the source/runtime disproof of finding seven, all other corrections and the mechanical indentation changes, with no remaining concrete issue.
- Gated closure retains all 656 IDs and every unrelated record body: only DW-160 closes, yielding 399 done/257 open and 232 exact-covering maintenance rows. Archive, sprint and dependency pointers remain unchanged. Complete commands, initial failures and review are in [validation evidence](dw-160-validation-evidence.md).
- Pinned commitlint 21.2.3 validates the exact full commit message with zero problems/warnings. Final pre-commit fetch reports zero divergence from origin/main at `84f2e8c7da6f2d7ae05412c478a94d21f83ab5c5`.
