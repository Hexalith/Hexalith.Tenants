---
title: 'Resolve five deferred audit presentation and receipt contracts'
type: 'feature'
created: '2026-10-26'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '665068ec0791fd2ea0e00e1c04b25d60ed0b9fbb'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** DW-408, DW-440, DW-450, DW-460 and DW-474 leave audit compatibility, timestamps, badges and receipt contracts unsettled; historical specifications conflict.

**Approach:** Implement the approved decisions below, verify the resulting consistent contract, and close exactly these five records with preserved historical evidence. Commit and push completed changes to main as requested.

## Boundaries & Constraints

**Always:** Preserve authorization, support-safe mapping, authority checks, current callers, localized labels and pending/error states. Reuse Fluent V5 components. Preserve unrelated records, historical findings and release holds.

**Never:** Invent authority, expose raw payloads, rewrite historical evidence, change dependencies, publish a release, or close unverified work.

## Approved Decisions — 2026-10-26

The user approved all four recommended decisions:

1. DW-408: Retain whole-page rejection of unknown audit events; document coordinated server/UI rollout.
2. DW-440: Use invariant `yyyy-MM-dd HH:mm:ss UTC` in grid and receipt; localize surrounding labels.
3. DW-450/DW-474: Adopt DESIGN.md consistently: Rounded status badges, Filled for Danger/Severe and Tint otherwise, localized icon labels where icons exist. Preserve accessible names without duplicate live announcements.
4. DW-460: Add an explicit lifecycle/provenance receipt overload, preserving the existing overload's conservative Partial behavior.

</frozen-after-approval>

## Code Map

All production paths below are relative to `src/Hexalith.Tenants.UI/`; tests are relative to `tests/Hexalith.Tenants.UI.Tests/`.

- `Services/Gateways/TenantQueryGateway.cs`: event allowlist, page validation, caller/stream binding.
- `State/TenantAudit/TenantAuditReceipt.cs`: metadata-free factory, completeness/state precedence and timestamps.
- `State/TenantAudit/TenantAuditRow.cs`: safe mapping defaults authority to Unknown.
- `Components/Tenants/Audit/{AuditDataGrid,AuditAvailabilityState}.razor`: timestamps, badges and live announcements.
- `Components/Shared/{TruthStateBadge,ProjectionLifecycleBadge}.razor`, `Components/Pages/TenantDetailPage.razor`, `Components/Tenants/TenantDataGrid.razor`, `Components/Users/MyTenantsDataGrid.razor`: remaining badge sites.
- `State/TenantAuditReceiptTests.cs`, `Services/Gateways/TenantQueryGatewayTests.cs`, `Components/{AuditAvailabilityState,TruthStateBadge,ProjectionLifecycleStatus,TenantAuditPage}Tests.cs`: qualification seams. Brace notation lists existing files.
- `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md`: Filled/Rounded/IconLabel requirements conflict with the completed availability story's explicit Tint choice.

## Tasks & Acceptance

**Execution, after decisions:**

- [x] Gateway and gateway tests above — qualify the chosen unknown-event policy, mixed pages, category mismatches and both streams.
- [x] Receipt/row factories and receipt tests above — implement the chosen API; test complete evidence, absent authority, unsafe entries and overriding states.
- [x] Receipt, audit grid and audit-page tests above — align UTC formatting and verify EN/FR, offsets and missing timestamps.
- [x] Seven badge sites and component tests above — align appearance/anatomy using Fluent parameters, including accessible names.
- [x] `docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md`, DESIGN.md and `epics.md` in the planning directory — record approved precedence while preserving story history.
- [x] `deferred-work.md`, `docs/maintenance-backlog.md`, and new implementation artifact `dw-408-440-450-460-474-validation-evidence.md` — retain exact results; close only the five selected records after passing validation and independent review.

**Acceptance Criteria:**

- Given an unsupported event, when the gateway maps its page, then the approved compatibility result occurs without unsafe disclosure or fabricated proof.
- Given a complete entry with authoritative metadata, when a receipt is constructed through the selected supported API, then Ready requires Current freshness/lifecycle and ProjectionBacked provenance; missing authority remains incomplete.
- Given the same event under English and French cultures, when its grid and receipt render, then both follow the approved absolute UTC convention.
- Given each status badge state, when rendered, then appearance and anatomy follow one documented convention with an accessible state name.
- Given passing checks and review, when reconciliation runs, then exactly five records change from open to done, all 657 IDs remain, and numbered totals become 615 done/42 open across 38 maintenance rows.

## Implementation Notes

- Approval received on 2026-10-26. The root workflow handles final review, ledger closure, commit and push after the implementation handoff returns. The implementation agent prepares code, tests, policy documentation and validation evidence; it must not commit or push. Do not mark the spec done or ledger records closed before independent review.

- Parent acceptance audit read the complete 102,318-byte staged diff. Code, tests and policy documentation satisfy the first five tasks; final ledger closure remains conditional on the independent review below. No matrix exists in the approved intent.

## Spec Change Log

## Review Triage Log


| Finding | Verdict / route | Evidence and disposition |
| --- | --- | --- |
| Blind 1 | medium / patch | The new authority overload reaches existing IsKnownOutcome allowance for Administrative global events; an empty-user entry becomes Ready. Restrict these events to Access and test both operations. |
| Blind 2 | medium / patch | Configuration events with missing/unsafe keys map their target to tenant ID and can become Ready. Require a safe event-specific configuration target and qualify missing, unsafe and conflicting targets. |
| Blind 3 | medium / patch | The new fixture loads only Fluent CSS; production availability forced-color rules are absent. Load compiled production styles before asserting that presentation. |
| Blind 4 | medium / patch | Browser exports omit detail status and the None pending state; a minimum badge count does not bind those sites. Add explicit site/state coverage and export the detail statuses. |
| Blind 5 | medium / patch | Most new fixture labels are compared with values from the same rendered component. Add independent resource-derived expected EN/FR names for the exported states. |
| Blind 6 | medium / patch | Current visibility checks ignore hidden/transparent/zero-size icons and hidden text. Qualify actual icon/text visibility and dimensions. |
| Blind 7 | medium / patch | Static AX properties do not prove spoken transition announcements. Add renderer transition coverage and narrow evidence wording to verified DOM/AX semantics; no human speech claim. |
| Blind 8 | medium / patch | New validator lacks its own rejection controls. Exercise lost names, exposed descendants, wrong appearance and invisible icons without weakening positive checks. |
| Blind 9 | low / patch | Combined-read category mismatch and reversed mixed ordering are absent from the new matrix. Add those straightforward cases. |
| Blind 10 | false / reject | No divergent badge contract is demonstrated: all seven sites use the approved identical Fluent rule and are covered. Extracting a generic shared component would require a new technical-module API/dependency, excluded by the approved freeze; hypothetical future drift is not a current defect. |
| Edge 1 | medium / patch, grouped with Blind 1 | Tracing confirms the gateway rejects Administrative global events while receipt completeness currently permits them and skips the user check. |
| Verification 1 | medium / patch, grouped with Blind 4 | The detail test checks aria-label on the hidden child; deleting the outer label survives current tests and its browser site is absent. Assert the outer localized name for all statuses in EN/FR. |
| Parent broad 1 | medium / patch | The first Blind 2 repair over-restricted configuration targets to ConfigurationKey and broke the existing typed-user precedence when both safe fields are present. Retain the required safe key, but match the mapper-selected UserId when present, otherwise ConfigurationKey. Add valid both-field and mismatched-target controls for Set/Removed; preserve the existing page Ready assertion. |
| Parent broad 2 | test fixture / patch | Both localized configuration outcome cases used a generic row without the now-required key. Supply genuine event-specific configuration evidence and assert Ready; preserve the localized outcome/copy assertions and production missing-key rejection. |
| Parent acceptance 1 | medium / patch | First full post-review run found that requiring target==configuration key rejects the established typed-user target when both fields exist. Preserve mapper precedence while requiring the safe key and selected typed target; the existing page Ready assertion is retained. |
| Parent acceptance 2 | low / patch | Two generic localized copy fixtures used configuration outcomes without configuration keys. Supply complete event-specific evidence while retaining their copy and translation assertions. |
| Parent guidance | low / patch | The new fixture helper shared a C# file with its test class. Move it to BadgeContractFixture.cs under the one-type rule; no behavior change. |

Independent correction checks: the original Blind Hunter, Edge Case Hunter and Verification Gap reviewers each re-inspected their findings and found no concrete remaining defect. Category/configuration target rules, explicit site/state fixtures, localized outer names, production styles, visibility checks and rejection controls are repaired. Parent final build passes with zero warnings/errors after moving the fixture helper into its own C# file to satisfy the repository's one-type rule. Final broad verification and ledger reconciliation follow. No new work was deferred.

The subsequent parent full run exposed the two issues above: 7,759/7,762 passed, three failures, zero skipped, exit 2, 1m45.268s (`/tmp/dw-408-474-root-full-ui.log`). Failures were the existing typed-user configuration page test and the Set/Removed localized receipt cases. This correction preserves the mapper's established typed-target contract and does not relax the required safe configuration-key evidence. Focused correction build passed with zero warnings/errors; receipt state/component checks passed 143/143 and the unchanged typed-user page regression passed 1/1. Exact results are retained in the validation artifact; root performs the final broad rerun.

Final parent acceptance: the full maintained UI project passed 7,762/7,762 with no failed/skipped/not-run cases. The complete maintained Chromium launcher passed its existing scenarios, four EN/FR normal/forced-color badge cases and 16 targeted rejection controls. Scoped formatting passes after a line-ending-only correction. Exactly the selected five records close; ledger validation proves 657 preserved IDs, 615 done/42 open, 38 rows, unchanged other 652 bodies and preserved historical fields. No dependency/gitlink, archive or sprint change occurred. No new work was deferred.

## Verification

- Before code edits, inspect an Aspire-started AppHost; record exact blockers.
- Build UI tests in Debug with `-p:UseHexalithProjectReferences=true`; run focused classes, then maintained per-project `dotnet test` for the full UI suite.
- After visual changes, run maintained Chromium audit checks in EN/FR and forced colors.
- Run `python3 scripts/validate-story-gitlinks.py` against this spec, `git diff --check`, and ledger/index exact-coverage and unchanged-record checks.
- Validate the exact commit message with pinned commitlint, fetch/inspect divergence, then commit and push completed work to main. Push is already authorized.
