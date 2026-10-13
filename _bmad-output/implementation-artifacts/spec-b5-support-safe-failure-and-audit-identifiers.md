---
title: 'B5 support-safe failure and audit identifier closure'
type: 'bugfix'
created: '2026-10-13'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '8216f9927700e39d67b2235ed83a4dbcef03d35a'
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-ux-instructions.md'
---

<frozen-after-approval reason="user explicitly authorized complete B5 implementation, review and validation">

## Intent

**Problem:** DW-315 historically allowed arbitrary backend failure text into command messages. DW-441 documented phone, encoded PII and divider bypasses in typed audit user identifiers. Current HEAD already contains substantive fixes and regressions; the ledger still carries the old source verification, and negative boundary tests lack explicit EN/FR fallback assertions.

**Approach:** Reverify and complete the approved support-safety boundary, retaining exact code-to-key mappings and the D5 allowlist/confusable inspection. Strengthen executed regressions, independently review the result and record closure only when validation proves it.

## Boundaries & Constraints

**Always:** Unknown backend data becomes fixed safe localization keys/fallbacks. Match whole case-sensitive failure codes without trimming/substring trust. Inspect compatibility normalization before repeated percent decoding, then user character allowlist and confusable skeleton. Preserve literal spelling, case, spacing and Unicode in approved identifiers. D4/D5 restrict phone/@/character allowlist enforcement to typed UserId; preserve established TenantId, ConfigurationKey and ApprovedReference policies. Existing B1–B4 evidence, historical findings, unrelated records, archive, sprint and decisions remain intact.

**Never:** New D5 approval, dependencies, staging, commits, branches, push, deployment, unrelated cleanup or B6/B7 closure. This is a freeform release bundle; no sprint story key. Generate review diffs in temporary files without staging. Parent records ledger/index/readiness closure after final validation.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected behavior | Handling |
| --- | --- | --- | --- |
| Known code | ConcurrencyConflict, BackpressureExceeded, DomainRejected | Fixed EN/FR key and whole localized message; evidence unchanged | Exact match only |
| Unknown details | Arbitrary private text, secrets, email, encoded values, code with suffix/space/case changes | No raw value in messages, DOM, JSON, live region or clipboard | Fixed safe fallback |
| Unsafe user ID | Dotted/slashed/tel/leading-space phones, fullwidth percent email, nested encodings | Actor/target redacted; partial receipt or blocked summary | Localized EN/FR refusal |
| Divider/confusable | U+2503/U+275A/U+01C0/U+23D0/U+204F/U+061B, encoded forms and secret lookalikes | No unsafe display/copy/announcement | Fail closed |
| Safe literals | Unicode, mixed case, literal spaces/slash/dot, tenant@alpha and non-user phone references | Original value retained exactly in display and approved copy | No normalization of output |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`: SafeMessageForStatus/SafeFailureReason already replaces BoundSafeFailureReason with exact code/key map. Retain fallback text and evidence/proof semantics.
- `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditSupportSafety.cs`: existing typed UserId character allowlist, NFKC-before-percent decoding, skeleton and phone detection cover recorded D5 cases.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources{.fr,}.resx`: six failure resources already present with parity.
- `tests/Hexalith.Tenants.UI.Tests/Components/SupportSafetyBoundaryTests.cs`: existing real gateway/create+metadata, mapped/direct grid/receipt, JSON/live-region/clipboard observer controls. Strengthen negative theories to run both cultures and assert localized fallbacks, including copied/blocked outcomes. Keep deliberate disclosure controls.
- `tests/Hexalith.Tenants.UI.Tests/State/TenantAuditSupportSafetyTests.cs`: current encodings and positive non-user literal contracts. Retain all tests; supplement unlisted-symbol allowlist coverage if useful.
- `tests/Hexalith.Tenants.UI.Tests/State/TenantCommandSafeMessageTests.cs`: fresh key retention and stale-key clearing.
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs`: status/rejection/evidence compatibility.
- `_bmad-output/planning-artifacts/architecture.md`: AD-5, AD-9, AD-11 bind BFF safety, whole-string localization and tests.

## Tasks & Acceptance

**Execution:**
- [x] Reverify gateway and D5 source against historical findings; finish any proven source gap without changing supported literal contracts.
- [x] Strengthen boundary regressions in the test paths above for both cultures, exact mappings, unknown details, all listed D5 shapes, render/copy/live region and safe fallbacks.
- [x] Run focused Debug source-reference build/tests; report exact commands/results and failures.
- [x] Parent: complete independent review and maintained validation, update only DW-315/DW-441, maintenance counts/index and B5 readiness evidence.

**Acceptance Criteria:**
- Given backend details outside exact approved codes, when shared gateway status reaches consumers, then fixed safe EN/FR copy appears and no unknown text reaches support observers.
- Given a D5 unsafe user ID, when mapped or directly supplied to grid/receipt, then display/copy/live regions fail closed with localized safe feedback.
- Given an approved literal domain identifier, when displayed/copied, then its original literal remains unchanged.
- Given passing checks and review, when closure is recorded, then only DW-315/DW-441 change status and B6/B7/decisions stay open.

## Implementation Notes

Source reverification: both core fixes are committed at baseline; no need to recreate them. No intent gap, irreversible operation or policy approval remains. User authorization covers the complete bundle.

Focused verification: Debug source build has zero warnings/errors; 195 boundary, 54 audit-safety, 253 gateway and six safe-message cases pass. Initial boundary iterations failed 186, 36 and two cases due fixture ordering and assertions about existing state/JSON contracts; failure logs are preserved. Exact commands and final broad evidence will be recorded in release-readiness.md. Production fixes already exist at baseline; this implementation strengthens regressions only.

Review patches: encoded observer checks retain all decoded/normalized representations and deliberate disclosure controls; only asserted message-key fields are excluded from case-insensitive JSON scans. Four symbol shapes gain EN/FR consumer coverage; four non-default command-evidence cases compare against unchanged no-detail proof. The real-resource localizer now has a neutral constructor for the unchanged parity gate. Patched focused build passes with zero warnings/errors; boundary 227, gateway 257 and parity three pass. Initial patch CA1062 diagnostics remain recorded.

Final validation and closure: 6,270 maintained Release cases pass with zero failures/skips, including 4,765 UI and 189 runtime integration; browser, package/consumer, coverage (95.70% lines, 100% configured auth branches) and Python guards pass. Exactly DW-315/DW-441 close; 310 done / 333 open across 643 IDs, with 297 index rows. The preservation comparison passes; B1–B4 evidence, B6/B7, decisions, unrelated records, archive (308 baseline IDs), sprint, dependencies and staging remain unchanged. Initial AppHost unhealthy/waiting observation and broader hosted/production/NFR10/AT limits are retained in release-readiness.md. User instruction prohibits the workflow's local commit step; no commit was created.

## Spec Change Log

## Review Triage Log

| Finding | Verdict / evidence | Route |
| --- | --- | --- |
| BH-1 processing fallback is not rendered | false: TenantCreateCommandModels clears SafeMessage/SafeMessageKey in Processing for create and metadata; progress displays state copy intentionally. Resource resolution and terminal fallback rendering cover the supported distinction. | reject |
| BH-2 encoded observer checks omit decoded input | low: encoded cases check only their supplied literal outside exact actor/target guards; add original and inspected representations to actual observer checks to detect transformed disclosure. | patch |
| BH-3 case-sensitive JSON scan broadens exception | low: whole-scan case sensitivity can miss altered casing. Exact SafeMessageKey assertions permit removing only those trusted keys from scanned clones and retaining case-insensitive checks for remaining state. | patch |
| BH-4 unknown-detail evidence uses defaults | low: unknown-detail case uses only zero/null evidence; add valid Completed count/sequence and known rejection evidence to guard conditional clearing during safety mapping. | patch |
| BH-5 new symbols lack consumer/encoding coverage | low: four symbol cases are helper-only; add them to the same observer/encoding matrix so the new allowlist property is checked through typed consumers. | patch |
| EC / verification-gap | No findings: both independent lenses completed. | complete |
| Maintained UI parity failure | medium: CultureBoundTenantsLocalizer requires constructor arguments, while the maintained gate constructs every discovered localizer. Provide a neutral real-resource constructor so the fixture remains inspected; do not exempt it or change the gate. | patch |

## Verification

Use Debug `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -m:1 -nr:false`, then direct assembly single-dash class filters for SupportSafetyBoundaryTests, TenantAuditSupportSafetyTests, TenantCommandSafeMessageTests and TenantCommandGatewayTests. Parent runs final full UI, solution warnings-as-errors build, maintained per-project suites, integration, browser, package/consumer/coverage and preservation checks. Keep exact command logs under `/tmp/tenants-b5-20261013-current/`; no solution-level dotnet test. Temporary validation files are not product code.
