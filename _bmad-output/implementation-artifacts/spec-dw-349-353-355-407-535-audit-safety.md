---
title: 'Validate audit filters and narrative timestamps and qualify caller-safe recovery'
type: 'bugfix'
created: '2026-10-21'
status: 'done'
route: 'oneshot'
baseline_commit: 'be14af75cbabe05c9500475c7163f55606dd618d'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Complete five audit deferred records: DW-349's renderer-bound filter validation, DW-353's support-safe ISO timestamp narrative parsing, DW-355's named category validation, DW-407's caller-change protection during invalid-cursor retry failures, and DW-535's assertive Unauthorized announcement coverage. Keep existing projection truth, support safety, opaque cursors and authorization boundaries. Accept narrative timestamps with explicit UTC Z or numeric offsets and zero through seven fractional digits; reject missing zones, malformed or culture-dependent dates. Keep displayed timestamps in canonical UTC round-trip form. Preserve historical ledger fields and close only these five records after passing relevant validation and independent review. The user requests committing and pushing the completed changes to main.

</frozen-after-approval>

## Implementation Notes

- This is one small audit-safety maintenance batch: two private implementation fixes, one private timestamp parser extension and existing-test coverage, with no new public API, intent gaps, data changes or deployment. Use the one-shot route. The requested five related records share the audit evidence surface.
- Read root guidance, Hexalith baseline/Git/UX rules, project context, contributor/build/test configuration, architecture AD-6 through AD-12 and the audit evidence specification. The initial tree is clean on main at the full baseline above; user push-to-main authorization governs branch choice.
- Code map: `Components/Pages/TenantAuditPage.razor` `LoadAsync` must capture/validate request on the dispatcher and explicitly render local refusal. `TryCreateRequest` must accept exact named categories only. Preserve generation/cancellation and existing filter-edit semantics. `State/TenantAudit/TenantAuditNarrative.cs` `SafeTimestamp` owns the bounded explicit-zone grammar. Existing `TenantAuditPageTests` and `TenantQueryGatewayTests` provide real notifier, Fluent filter and held REST-response fixtures. Add focused narrative tests in a single type-named file.
- DW-407 already has ordinary-fault and 304 caller-switch coverage; qualify the remaining held invalid-cursor retry catch with caller replacement and sign-out, including retained rows, ETag and scope checks. Production gateway behavior should need no change if the new regression passes.
- Aspire startup succeeds. Initial resource inspection shows Memories running/healthy, security running/unhealthy and downstream resources waiting. This is component/gateway work; no distributed-health acceptance is claimed. Raw configuration remains outside the repository.
- Validation will use a Debug source UI-test project build and maintained MTP focused/full UI lanes, scoped formatting, story-gitlink and whitespace checks. Ledger closeout will compare parsed baseline/current records and exact open-ID index coverage. Keep dependency pointers, archive, sprint state and existing release holds intact.
- Implemented renderer-bound request capture/validation and exact category names; three delayed-authentication tests seed raw filters only to isolate the load-path render, eight malformed category cases and three valid recovery controls qualify behavior. The existing ten accessible-state cases now assert exact role/live pairs. Gateway adds six held invalid-cursor retry failure cases with caller replacement, sign-out and stable-caller controls.
- Initial Debug source build succeeds with zero warnings/errors. Focused page 246/246 and gateway 714/714 pass. Initial narrative run fails 23/44: mixed mandatory/optional fraction tokens reject valid fractions and the runtime accepts shortened numeric offsets. Replaced the mixed token with optional fractions plus bounded explicit-zone/nonempty-fraction shape checks; corrected build has zero warnings/errors and narrative 44/44 passes.
- Scoped formatter exits 2 on 22 diagnostics, all mapped to exact unchanged baseline lines in TenantAuditPageTests; none touches added/changed lines. No unrelated formatting is rewritten. Story-gitlink and whitespace checks pass.
- Review clarification: DW-407 qualifies the end-to-end caller-safe invalid-cursor recovery result. The caller-bound transport and outer publication guard already protect this path; these tests do not prove each inner retry catch guard independently necessary. The narrative grammar is uppercase `T`/`Z`, required seconds, an optional dot followed by 1–7 digits, and explicit `Z` or `±HH:mm` offsets. Compact/shortened offsets are outside this intentionally bounded UI grammar.
- Independent review requested a queued-dispatch ownership control, a nonnull stream selector in retry assertions, and canonical `O` timestamp compatibility cases with numeric UTC/trailing zeros. These are small validation/evidence corrections within the existing scope.

- Original-code replay builds the exact baseline page/parser with final tests. Expected controls fail: timestamp 22/46, delayed authentication 3/3, queued dispatcher 1/1 and category grammar 7/8 (all exit 2, zero skips); patched source bytes are restored before the final build. The one lowercase category rejection and canonical original timestamp controls remain passing.

- Final reviewed Debug build has zero warnings/errors; focused page 247/247, gateway 714/714 and narrative 46/46 (1,007 total) pass, and full UI 6,949/6,949 passes with zero failures/skips/errors. Independent recheck resolves all five findings; nothing is newly deferred. Only the selected five records close: 657 IDs retained, 420 done/237 open across 216 exact-covering maintenance rows, with historical field values and all unrelated records preserved. Complete commands, initial failures, controls and formatting limits are in `dw-349-353-355-407-535-validation-evidence.md`; machine-readable preservation is retained in the accompanying ledger JSON.

## Review Triage Log

Blind Hunter calculated its floor from 22.97 kB: `min(floor(sqrt(22.97) + 1), 10) = 5`. All five findings receive small in-scope validation/evidence corrections:

1. **medium / patch:** The delayed-auth test proved repainting but could not distinguish off-renderer validation followed by an explicit render. Add a deterministic held-dispatch regression that observes unchanged state until queued renderer work executes.
2. **low / patch:** The retry-catch wording overstates which internal guard the test qualifies. An appended implementation clarification records the transport/outer-guard behavior and end-to-end coverage limit without altering frozen intent or historical notes.
3. **low / patch:** A null stream made retry stream preservation vacuous. Set the request stream to `tenant` and preserve the exact received-argument assertion.
4. **low / patch:** The positive timestamp matrix omitted numeric UTC and trailing-zero canonical server forms. Add exact `.0000000+00:00` and `.1200000+00:00` compatibility cases for both existing narrative keys and display.
5. **low / patch:** The bounded timestamp grammar needed explicit syntax. An appended note specifies uppercase T/Z, optional 1–7 fractional digits and ±HH:mm offsets; compact ISO variants remain outside that grammar. No public API or user-facing contract is broadened beyond the recorded intent.
