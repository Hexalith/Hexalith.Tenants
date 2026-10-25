# Five deferred UI safety items — final verification

Approved baseline: `14133e96d2c5efb13270580f920133079707398b`. DW-194, DW-201, DW-221, DW-267 and DW-609 are implemented, independently reviewed and closed against the [approved frozen matrix](spec-dw-194-201-221-267-609-ui-safety.md). All 657 IDs remain: **605 done / 52 open across 47 exact-covering maintenance rows**. All 652 other numbered records, selected original fields, five accepted limitations, archive, sprint and dependency gitlinks remain preserved.

## Resulting behavior

- Removal presents the complete ten-fact preview in Fluent's native modal and reuses shared FrontComposer labeling/containment. Native Tab/Shift+Tab, inert background, safe focus return and pending Escape/overlay/resize safeguards are verified. Focus preparation retains exact lifetime/admission ownership before disabling Confirm.
- Create requires a measured safe viewport and retains intent/tracked work across resize. A matching conflict offers open-existing only after a bounded unconditional authorized detail read, rechecks the exact owner inside the renderer before navigation, and restores lifecycle focus when a click-time read refuses. Detail Back preserves context, opens the create region and focuses its visible lifecycle. Stale return imports and JavaScript locations cannot focus or consume replacement markers.
- Optional `CollectionShapes` declares array intent and refuses scalar, incomplete, nested or contradictory metadata. Metadata keys follow configuration case-insensitivity; values and policy identifiers retain exact comparisons. Existing deployments/defaults remain in legacy mode with documented flattened-provider ambiguity. See [deployment guidance](../../docs/tenant-configuration-read-policy.md).
- Member/metadata activity uses per-attempt/generation coordination; metadata refresh shares the existing drain; safe-message resolution preserves flow-specific fallback. Replacements acquire activity independently of held old callbacks, late outcomes cannot release new ownership, and disposed callback faults avoid resolving retired services.
- Retained facts/validators and in-flight publication bind to observed authorization revision plus caller scope. Real provider transitions clear mounted workspace/membership rows before resolving authority and trigger unconditional fresh reads. Retired, faulted, timed-out, disposed and overtaken work cannot restore old facts. Existing detail/audit/configuration reauthorization remains enforced. Platform revocation guarantees DW-562/DW-617 remain separate.

## Final gates

| Gate | Result | Archive member |
| --- | --- | --- |
| Debug/source UI test build | 0 warnings, 0 errors | `final/build.log` |
| Maintained UI project | **7,704 passed; 0 failed; 0 skipped**, 2m06.964s | `final/ui-tests.log` |
| Review regressions | 215/215: create 83, activity 5, policy 110, panel lifetime 8, mounted consumers 9 | `supporting/review-*-tests.log` |
| Complete static Chromium lane | Passed; all retained fixtures/mutations and new location/native-modal cases | `final/static-browser.log` |
| Authenticated production UI browser | **12/12**, fresh build, stable source, closure eligible | `final/browser.json`, `final/browser.log` |
| Ledger/index conservation | Exactly five status changes; all selected history recoverable; 652 other bodies unchanged | `final/ledger-conservation.json` |
| Story gitlinks / whitespace / pinned commitlint | Passed | `final/gitlinks.log`, `final/diff-check.log`, `final/commitlint.log` |

The [raw archive](evidence/dw-194-201-221-267-609/raw-validation.zip) preserves complete final and diagnostic packets. Its [manifest](evidence/dw-194-201-221-267-609/manifest.json) records every member's hash/size, counts and runtime identity. Exact commands are `final/commands.json`; `final/ui-test-binaries.sha256` binds the test, UI and Shell binaries. The final browser UI/Shell hashes match the full-suite copies, and before-build/after-build/after-browser source captures are identical.

## Frozen matrix coverage

| Frozen row | Executed coverage |
| --- | --- |
| Removal modal and pending safeguards | Browser `member-native-modal-keyboard`, `member-pending-native-dismissal`; retained held pre-submit focus and narrow pending-delivery component regressions. Static EN/FR fixtures additionally verify native modal opening, keyboard cycles and negative controls. |
| Measured create safety | `Unknown_or_unsafe_viewport_blocks_create_and_resize_preserves_tracked_work`, `Unsafe_resize_during_held_baseline_refuses_with_the_localized_viewport_reason`, first-tenant bootstrap regression; browser `create-measured-conflict-recovery`. |
| Authorized conflict and return | Fresh-read refusal/recheck, five queued navigation-owner cases, four held return-import cases; actual JavaScript mismatched-location focus/marker cases. Browser proves failed-click lifecycle focus, authorized navigation and visible focused Back return. Keyboard activation waits for the actual enabled control. |
| Policy discriminator/compatibility | Empty array/scalar, malformed root, case-insensitive metadata names with literal data, reload recovery and four nested-item cases (both collections × legacy/explicit root); retained layered-environment compatibility. |
| Exact shared callback ownership | Held/repeated activity, late failed/rejected and successful old callbacks with replacement admission/release, disposed fault, iterative metadata refresh, per-flow safe fallback and exact lease-owner cases. |
| Same-subject permission revision | Gateway 28 retained and 14 in-flight combinations plus disposal; nine mounted list/my/user ready/degraded/in-flight cases; eight membership fault/timeout/disposal/overtaking cases; retained audit boundaries and configuration reauthorization. |

## Independent review and preserved limitations

All three review layers ran independently before triage. Every finding and verdict appears in the spec. Eight initial patch groups and the later static/keyboard fixture corrections were verified; low/false findings were rejected with evidence. Two new unnumbered review follow-ups are recorded separately from the 657 numbered records: an **unverified medium** connected-browser pre-submit focus-failure scenario, and an **observed medium** pinned Fluent 5.0.1 grid-disposal continuation.

One post-review full browser run reported a disposed grid JavaScript reference and body-focus failure during create recovery. Its packet, log, sanitized stack and unchanged-library decompilation excerpt remain under `diagnostics/`; its identical original focus labels do not establish which checkpoint failed. Labelled checks did not reproduce that circuit fault. A separate diagnostic proved keyboard activation could occur while the visible recovery control was disabled; the fixture now awaits enabled state. No dependency workaround, assertion removal or circuit-error allowance was introduced. The final complete run passes both named focus checkpoints with **zero circuit errors**; this does not claim the separate dependency defect is fixed.

The final full invocation unsets selected-only/build-reuse flags and uses the repository's pinned Playwright 1.63.0. All nine retained administrator cases plus three safety cases execute: 40 authorized fixture reads, 15 administrator reads, exactly two local command simulations, zero unexpected commands and zero circuit errors. Selected-only diagnostics remain ineligible for closure. A retired temporary Playwright-path launch failure is also retained.

The test-only loopback OIDC issuer exercises production signed-token validation and Razor components. Deterministic HTTP/SignalR responses qualify UI behavior, not backend authorization, stored domain state or delivery. No command is forwarded. The existing AppHost baseline remains blocked by duplicate `eventstore-app-api-token` registration at Program.cs:79 (AppHost exit134 / CLI exit2); no healthy distributed topology or Release qualification is claimed.

All nine original static fixture export invocations remain intact (`final/static-fixture-conservation.json`). The runner resolves the shipped focus asset from the selected build manifest. Two older static fixtures now open/verify the native modal; the obsolete missing-sentinel mutation becomes a missing-native-modal mutation while layout, Tab/Shift+Tab, Enter, launcher/heading, measured viewport and stuck-measurement controls remain. The earlier byte-identical export-block claim belongs only to the preserved pre-review packet.

Earlier suite, fixture-selector/disposal, modal-opening/sentinel, focus and navigation failures remain diagnostics, never final qualification. `diagnostics/pre-review-raw-validation.zip` and its manifest preserve the entire earlier 29-member packet unchanged. No historical pass or closure bit qualifies later source by itself.
