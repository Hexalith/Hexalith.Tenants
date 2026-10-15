# Release readiness verification

**Current assessment: 2026-10-14, after B8. Conclusion: not ready for release.** Only DW-499/DW-502 close in this bundle: durable exact terminal advisory repair and persisted range verification before publication. Independent review, fault/end-state regressions, final Server4664 passed/0failed/25 unchanged historical skips, package consumers, maintained Tenants tests and unchanged coverage support bounded local closure. External story-gitlink provenance, blocked nested-layout/topology lanes and existing qualification/decision holds remain. See [B8 evidence](b8-validation-evidence.md); epics and sprint tracking are unchanged.

**Historical pre-B8 assessment (retained):** **Assessment date:** 2026-10-12; B3/B4/B5/B6 remediation verified 2026-10-13; B7 verified 2026-10-14. **Conclusion: not ready for release.** The planned scope has completion records, and the local build and maintained test lanes pass. B1 bootstrap identity/credential transport, B2 caller-bound retained reads, B3 exact command/authoritative projection/removal proof, B4 admission/retained-attempt ownership, B5 support-safe failure/identifier policy and B6 audit-stream separation/replay compatibility are closed in the validated working tree (see their remediation evidence below). B7 live-authority/measured-viewport admission is now closed by the evidence below. External story-gitlink attribution drift and existing qualification holds still prevent an unqualified release decision. Several acceptance and deployment proofs also remain incomplete. This assessment does not reopen epics or change sprint tracking.

## Assessment boundary and provenance

Verified Tenants commit `7a2a126f09274e9324d72fe20d1c086a4a6b7276` on `main`. The working copy already contained changes to `.claude/settings.json`, `.codex/hooks.json`, and `deferred-work.md`, plus untracked `deferred-work-archive.md` and `docs/maintenance-backlog.md`. The working-copy ledger and backlog were assessed as supplied; they are not yet committed release evidence. Their content and sprint tracking were preserved.

Read [AGENTS.md](../../AGENTS.md) and the required [Hexalith baseline](../../references/Hexalith.AI.Tools/hexalith-llm-instructions.md), after confirming the root `.gitmodules` declares `references/Hexalith.AI.Tools`. Read owning build/test configuration, repository guidance, [architecture](../planning-artifacts/architecture.md), [epic requirements](../planning-artifacts/epics.md), completion records, retrospectives, [maintenance backlog](../../docs/maintenance-backlog.md), and [deferred ledger](deferred-work.md). Used the repository-local `bmad-sprint-planning` validate workflow; no tracking generation or repair ran.

Root dependency commits used by the source checks:

| Dependency | Commit |
| --- | --- |
| AI.Tools | `3f194e17174994d308ec84af9ee2b5aa68674d0d` |
| Builds | `608c164c3637240fed2dd92dbfdaaf581ba9846d` |
| Commons | `b247ed116c6523f8c596ec0a933eff8973d11568` |
| EventStore | `3d0b738d23e51ff765004e3f76ee7863e83b5ee6` |
| FrontComposer | `985d3e70ac8e9d5492fa857bc3a31208a9543670` |
| McpCli | `7e99305a621e1bdf7119b6e924348857e2b5a5a4` |
| Memories | `f4c7d68f619f376debca440387428139a61080cc` |
| Platform | `977f2e01347226ac727b707aee4f140f5dc8b010` |
| PolymorphicSerializations | `98de6e013840ece9f0fa7c68ab7dcdf2bba3b375` |

The package catalog pins EventStore `3.120.0` and FrontComposer `4.6.0`. Source-pin qualification and published-package qualification are separate evidence. Local environment: Ubuntu 26.04, .NET SDK `10.0.401`, Dapr CLI `1.18.2` / runtime `1.18.4`, Docker `29.8.2`; the browser harness selected Chrome `154.0.8037.57`. Existing Dapr infrastructure was available. No dependency/submodule updates, source fixes, artifact moves, staging, commits or publication were performed.

## Epic completion findings

[Sprint status](sprint-status.yaml) is valid: five epics, 32 stories, five retrospective statuses and 29 tracked action items are all `done`. Comparison with the current epic headings found exactly 32 planned story numbers, with no missing or additional tracked story numbers: Epic 1 has 12, Epic 2 has 4, Epic 3 has 6, Epic 4 has 3, and Epic 5 has 7. The shortened `4-3-...hard-sto` tracking key still identifies planned Story 4.3; it is not a missing story.

**Historical findings:** the story qualifications below preserve the 2026-10-12 assessment and its B1/B2 reconciliation. The B3/B4 references in those historical rows are superseded only by the validated [B3 evidence](#b3-remediation-evidence-2026-10-13) and [B4 evidence](#b4-remediation-evidence-2026-10-13); historical B5 references are superseded only by [B5 evidence](#b5-remediation-evidence-2026-10-13). Historical B6 references are superseded only by [B6 evidence](#b6-remediation-evidence-2026-10-13). Historical B7 references are superseded only by [B7 evidence](#b7-remediation-evidence-2026-10-14). All other qualifications remain open.

**Completion evidence exists for every planned story; full current acceptance cannot be certified for every story.** The tables identify delivered behavior, the actual completion artifact and the remaining limitation. Passing existing tests demonstrates the covered behavior; it does not close contradictory source findings or supply missing browser, authority, recovery or production evidence. The shared NFR10 gate in `epics.md` applies to all 32 stories.

### Epic 1 — Trustworthy Tenant Discovery and Access Review

FR1–FR9 and FR18 have implementation evidence. Direct reads, canonical navigation, configuration display filtering, protected search paging and fixed-scope review are present. Caller-bound retention B2 is closed in the validated working tree; some required accessibility/runtime evidence remains a release concern.

| Story | Completion evidence and acceptance coverage | Qualification |
| --- | --- | --- |
| 1.0 | [Reverification](1-0-reverify-frontcomposer-shell-and-fluent-contracts.md), [dated contract matrix](story-1-0-frontcomposer-fluent-reverification-2026-07-19.md): shell, Fluent APIs, localization/docs and fallback boundaries classified. | Correctly completed as **complete-with-blocked-contracts**. Its FC-CMD/FC-CNC findings do not themselves clear downstream commands. Later command evidence must establish the corrected behavior. |
| 1.1 | [Host/workspace story](1-1-reverify-ui-host-bootstrap-and-canonical-workspace.md), [runtime evidence](story-1-1-ui-host-bootstrap-and-canonical-workspace-evidence-2026-07-19.md): InteractiveServer, one module entry, canonical tabs/routes, EN/FR and container configuration. | Local host evidence exists; `PLATFORM-OPS-1` production liveness/publication authority and hosting policy remain external boundaries. Local startup is not production deployment proof. |
| 1.2 | [Cursor/list story](1-2-tenant-list-triage-and-cursor-foundation.md), [dated evidence](story-1-2-tenant-list-triage-and-cursor-foundation-evidence-2026-07-20.md): safety columns, six states, opaque paging, reset/recovery and warm-load evidence. | Caller retention B2 is closed by the remediation evidence below; historical fixed-viewport limitations do not establish every supported physical viewport or assistive-technology behavior. |
| 1.3 | [Detail story](1-3-tenant-detail-navigation-and-overview.md): deep links, overview, safe return context, freshness and authorization-safe absence; current detail tests pass. | Caller-bound detail evidence B2 is closed; shared NFR10 remains qualified. Historical read-route assumptions must be read with Story 1.10. |
| 1.4 | [Current self-audit story](1-4-my-tenants-self-audit.md), [dated evidence](story-1-4-my-tenants-self-audit-evidence-2026-07-21.md): principal-derived self scope, roles, empty states and canonical return. | Evidence retains the Role-aware `MyTenantsDataGrid` composition decision; do not claim literal reuse of `TenantDataGrid`. Historical direct-read/freshness limitations require the later 1.10 evidence. |
| 1.5 | [Lookup story](1-5-user-membership-lookup.md): literal user IDs, scoped memberships, safe empty/error states, validation and navigation; current lookup tests pass. | Same-target snapshot caller binding B2 is closed in the validated working tree. Composed authenticated lookup/AT evidence is narrower than the current epic's full acceptance wording. |
| 1.6 | [Current configuration spec](spec-1-6-read-only-tenant-configuration.md), [story evidence](1-6-read-only-tenant-configuration.md), [follow-up](spec-1-6-read-only-tenant-configuration-2.md): namespaced read-only display, redaction, state mapping and EN/FR. | UI filtering is implemented. Direct REST configuration filtering is unresolved decision DW-599; do not assume the UI policy protects every API consumer. |
| 1.7 | [Member review story](1-7-tenant-member-table-and-action-availability.md): member/role/risk context, canonical unavailable reasons and read-only review. | Current membership launchers default authorization flags to true without live parent binding: B7 / DW-235. Historical read-only completion does not establish current mutation availability safety. |
| 1.8 | [Current spec](spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md), [successor evidence](story-1-8-support-safe-identifier-copy-and-read-experience-evidence-2026-07-21.md): literal safe copy, refusal, localization, focus/ARIA and surface coverage. | Its `BROWSER-COPY-1.8` and `AT-NVDA-1.8` records explicitly withhold authenticated clipboard/denial and human screen-reader proof. The local focus harness does not supply that evidence. |
| 1.9 | [Search spec](spec-1-9-authoritative-memories-search-with-protected-paging.md), [corrected evidence](story-1-9-authoritative-memories-search-with-protected-paging-evidence-2026-07-27.md): Memories as index only, authoritative hydration, raw-hit advancement and caller/query-bound protected cursors. | Memories runtime/window guarantees and authenticated browser/AT evidence remain qualified. Preserve the existing partial-window disclosure boundary and DW-127 decision; DW-618 still tracks privacy/completeness work. |
| 1.10 | [Transport spec](spec-1-10-direct-tenants-reads-and-authoritative-freshness.md), [successor evidence](story-1-10-direct-tenants-reads-and-authoritative-freshness-evidence-2026-07-28.md): direct REST, token relay, metadata, 304 and nudge-only read behavior. | The non-audit retained-snapshot B2 finding is closed by the working-tree remediation evidence below. The local persisted generated-tenant route and four global-handler cases pass; they do not independently prove all six authenticated live routes together (DW-161). The final spec deliberately supersedes earlier service-discovery assumptions. |
| 1.11 | [Fixed-scope review spec](spec-1-11-authorized-global-administrator-review.md), [historical read artifact](4-2-review-global-administrators-from-fixed-aggregate.md): authorization reflection, direct fixed read, complete paging and distinct trust states. | Caller-bound global snapshot retention B2 is closed; revocation guarantee DW-562/DW-617 is unresolved. Complete UI paging does not make authorization projection lag bounded. |

### Epic 2 — Safe Tenant Membership Management

FR10–FR12 have forms, domain outcomes, previews and lifecycle evidence. Current freshness/identity proof and retention defects contradict the command-foundation, reconnect and removal-proof criteria.

| Story | Completion evidence and acceptance coverage | Qualification |
| --- | --- | --- |
| 2.1 | [Foundation spec](spec-2-1-reverify-projection-confirmed-membership-command-foundation.md): fixed command route, ULID idempotency, non-collapse states, authoritative requery, admission and rejection mapping. | B3/B4 remain in the actual create/member/metadata consumers. A shared guard's existence does not prove every consumer retains ownership or exact identity. |
| 2.2 | [Add-member story](2-2-add-user-to-tenant-with-explicit-role.md): explicit role, validation, orphan bootstrap, duplicate/rejection behavior and projection matching. | B3/B4: two-argument tracking, unqualified parent projection payloads, ambiguous delivery and teardown/late-status retention. |
| 2.3 | [Role-change story](2-3-change-tenant-member-role.md): intended/current role, NoOp honesty, escalation rejection and authoritative matching. | B3/B4 and live availability B7. DW-498 preserves the unresolved authorized inspection/handoff policy after self-demotion. |
| 2.4 | [Current removal spec](spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md), [WP-2A evidence](spec-2-4b-wp-2a-removal-proof-and-audit-available.md), [historical story](2-4-remove-tenant-member-with-consequence-preview.md): ten-item preview, friction, absence/provenance and distinct proof state. | B3: removal proof still selects target/event/time rather than exact attempt causation. B4 retention and B7 runtime viewport admission also remain. FR12 cannot be certified from a concurrent same-target receipt. |

### Epic 3 — Tenant Onboarding, Lifecycle and Configuration

FR13–FR17 are implemented, including mandatory configuration previews and reversible lifecycle control. Create/metadata admission and identity evidence remain deficient; stronger later lifecycle/configuration trackers do not repair those earlier consumers.

| Story | Completion evidence and acceptance coverage | Qualification |
| --- | --- | --- |
| 3.1 | [Current create spec](spec-3-1-create-tenant-with-projection-confirmation.md), [historical create evidence](2-1-create-tenant-with-projection-confirmed-command-lifecycle.md): validation, first-tenant unknown-freshness exception, duplicate handling and projection/version comparison. | B3/B4/B7: no aggregate admission lease, lost ambiguous result ID, two-argument tracking, cached proof without current metadata and list-kind-based authority. Preserve the legitimate first-tenant exception. |
| 3.2 | [Metadata spec](spec-3-2-edit-tenant-metadata-with-recorded-updates.md), [historical evidence](2-5-edit-tenant-metadata-with-safe-validation.md): validation, always-emitted update semantics and refreshed metadata proof. | B3/B4/B7: missing aggregate identity in status handle, admission released on route/disposal and unbound `IsAuthorized=true`. |
| 3.3 | [Availability spec](spec-3-3-lifecycle-and-configuration-availability-guardrail.md), [historical guardrail](3-1-tenant-lifecycle-command-availability-and-blocked-state-guardrail.md): separate lifecycle/config actions, freshness, scope, disabled behavior and canonical refusal reasons. | Focused guard coverage passes. Broader sibling admission and current authority bindings require the B4/B7 closure rather than assuming uniform wiring. |
| 3.4 | [Lifecycle spec](spec-3-4-disable-or-enable-tenant-with-complete-preview.md), [historical flow](3-2-disable-or-enable-tenant-with-high-impact-confirmation.md): ten facts, friction, NoOp/rejection honesty, scoped status and retained lifecycle attempts. | Support-safe failure mapping B5 remains shared. Pending-status semantics, retry recovery and current composed browser evidence remain qualification work. No hard tenant deletion is delivered or required. |
| 3.5 | [Set-configuration spec](spec-3-5-set-namespaced-configuration-with-complete-preview.md), [historical set flow](3-3-set-tenant-configuration-key-value-with-consequence-preview.md): namespace/limits, all-key preview, identical-value NoOp, exact value matching and retained tracking. | Approved all-preview policy remains settled. B5 and missing real form/breakpoint evidence DW-299 remain; historical Story 3.5 query-routing evidence is a separate artifact. |
| 3.6 | [Remove-configuration spec](spec-3-6-remove-configuration-key-with-complete-preview.md), [historical remove flow](3-4-remove-tenant-configuration-key-with-consequence-preview.md): ten facts, absence/provenance, safe refusal and scoped retained tracking. | B5 and DW-299 remain. Existing tracker/lease coverage is stronger than membership/metadata and must not be generalized to those flows. |

### Epic 4 — Global Administrator Control

FR19 has fixed-scope grant/removal, full projection paging, ten-item previews, self-removal consequences and last-administrator refusal evidence. Backend last-administrator enforcement is present. Bootstrap identity B1 and caller-bound retention B2 are closed; release qualification still requires revocation policy, ambiguous recovery and admission closure.

| Story | Completion evidence and acceptance coverage | Qualification |
| --- | --- | --- |
| 4.1 | [Availability spec](spec-4-1-fixed-scope-global-administrator-action-availability.md), [historical readiness](4-1-global-administrators-navigation-and-read-contract-readiness.md): independent action gates, fixed identity, complete counts, freshness and last-admin block. | B1/B2 are closed in the validated working tree; B5 and revocation decision DW-562/DW-617 remain. DW-331's contradictory public evaluator input remains a gap; current complete loaders reject it, so no reachable bypass is asserted here. |
| 4.2 | [Grant spec](spec-4-2-grant-global-administrator-with-projection-confirmation.md), [later revisions](spec-4-2-grant-global-administrator-with-projection-confirmation-3.md), [historical grant](4-3-grant-global-administrator-with-projection-confirmation.md): fixed routing, preview/friction, count and projection reconciliation. | Ownerless terminal-grant adoption can retain its lease (DW-421 / B4). Ambiguous committed delivery recovery DW-625 and authenticated grant-browser evidence remain unresolved. |
| 4.3 | [Removal spec](spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md), [follow-up](spec-4-3-remove-global-administrator-with-last-administrator-hard-stop-2.md), [historical removal](4-4-remove-global-administrator-with-last-admin-hard-stop.md): complete count, typed confirmation, last-admin pre-block/race and fixed scoped status. | Current declared gitlink check passes. Revocation semantics, ambiguous recovery, hosted browser qualification and remaining composed focus/count evidence stay qualified; a passed declaration check does not establish runtime readiness. |

### Epic 5 — Audit Evidence and Corrective Recovery

FR20–FR25 have substantial implementation and reviewed correction evidence. Global correction now has exact committed-sequence and audit-correlation proof. Tenant correction intentionally withholds paired links without causation. Audit retention, identifiers, replay collision and current acceptance evidence remain concerns.

| Story | Completion evidence and acceptance coverage | Qualification |
| --- | --- | --- |
| 5.1 | [Audit spec](spec-5-1-browse-tenant-audit-trail.md), [approved performance contract](story-5-1-performance-decision.md), [accepted evidence](story-5-1-performance-evidence.md): authorized flat grid, protected filters/paging, redaction and measured load. | Historical v4 measurements are valid and rechecked below. They bind September source, not this October candidate; the contract requires requalification after relevant query/BFF/grid/paging changes. B5/B6 also affect evidence integrity. |
| 5.2 | [Context-entry spec](spec-5-2-reach-scoped-audit-evidence-from-context.md), [historical story](5-2-scoped-audit-evidence-entry-points.md): scoped links, safe return context, target hints, focus and typed command handoff. | Caller-bound detail capability B2 is closed; authenticated composed route evidence DW-432/DW-436 remains. Anonymous fail-closed smoke results do not establish authorized contextual journeys. |
| 5.3 | [Receipt spec](spec-5-3-view-a-support-safe-audit-evidence-receipt.md), [historical receipt](5-3-support-safe-audit-evidence-receipt.md): seven safe fields, BFF mapping, refusal, copy and distinct proof availability. | Identifier policy implementation B5 remains. DW-440 preserves the absolute-date culture-format decision; no new timestamp policy is selected. |
| 5.4 | [Availability spec](spec-5-4-understand-audit-availability-and-recovery.md), [later reverification](spec-5-4-understand-audit-availability-and-recovery-6.md), [story](5-4-audit-availability-state-recovery.md): shared typed pending/delayed/unavailable/missing-support states and named recovery. | B4: Continue read-only and late status can abandon/revive member attempts. DW-447/DW-449/DW-586 still qualify actual recovery/pending semantics. Shared control tests do not establish every delegate is safe. |
| 5.5 | [Current start spec](spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md), [historical start](5-5-start-forward-correction-from-audit-evidence.md): current-state handoff, explicit role, orphan bootstrap, add/change selection and already-applied refusal. | Older unchecked enablement/focus items must be read with the subsequent 5.6 implementation. No historical role inference or broad recovery bypass is authorized. Current authority and support-safety qualification remain. |
| 5.6 | [Current correction spec](spec-5-6-preview-confirm-and-link-a-tenant-correction.md), [later reverification](spec-5-6-preview-confirm-and-link-a-tenant-correction-3.md), [historical preview](5-6-preview-and-confirm-correction-with-linked-proof.md): ten current facts, one attempt, exact completed-status sequence and projection reaching that sequence. | Approved frozen matrix permits truthful missing association. `TenantCorrectionPreviewSnapshot.WithCorrectiveProof` always leaves `ProofLink=null` / `MissingSupport`; the final 5.7 decision limits audit causation fields to global events. **Tenant bidirectional paired proof is not delivered**, and must not be advertised from the historical title. DW-574 caller-transition retention remains unverified. |
| 5.7 | [Current global correction spec](spec-5-7-correct-global-administrator-authority-from-audit-evidence.md), [historical verification](5-7-global-administrator-correction-verification.md): fixed system scope, complete paging, ten facts, no already-applied dispatch, last-admin refusal, exact sequence/correlation proof, paired links and sibling adoption. | Local UI, rendered browser harness and source-route checks pass. Replay prerequisites B6, revocation/recovery decisions and hosted browser/integration qualification remain. DW-642's malformed completed-status lease risk is unverified, not a confirmed reachable defect. |

### Retrospective action follow-through

Read all six retrospective records: [Epic 1](epic-1-retro-2026-06-06.md), [Epic 2](epic-2-retro-2026-06-06.md), [Epic 3](epic-3-retro-2026-06-29.md), [Epic 4](epic-4-retro-2026-06-07.md), [Epic 5 original](epic-5-retro-2026-06-06.md), and [Epic 5 refresh](epic-5-retro-2026-06-29.md). The original Epic 5 actions recur in the refresh; they are not a second independent set of untracked work.

| Action group | Closure evidence | Remaining work explicitly carried |
| --- | --- | --- |
| Epic 1: docs, command foundation, UI checklist, environment ownership, safe copy | [June 30 follow-through](../planning-artifacts/sprint-change-proposal-2026-06-30-epic-1-retro-follow-through.md), current command-foundation spec and conformance tests. All five are tracked done. | Runtime/AT and retention gaps remain visible in the maintenance index. Done means the follow-through was recorded, not that every future safety regression is impossible. |
| Epic 2: docs, Epic 3 readiness, neutral status copy, durable feedback, lock symmetry | [June 30 follow-through](../planning-artifacts/sprint-change-proposal-2026-06-30-epic-2-retro-follow-through.md), historical configuration-removal feedback and current gateway/guard tests. All five are tracked done. | DW-199/DW-287/DW-458/DW-471/DW-472/DW-500 retain the unresolved admission/retention work; DW-234/DW-251/DW-316 retain localization gaps. |
| Epic 3: tracking, lock retention, focus checklist, reason honesty, preview policy, doc drift | [Lock closure](../planning-artifacts/sprint-change-proposal-2026-06-30-command-lock-retention-guard-closure.md), [reason closure](../planning-artifacts/sprint-change-proposal-2026-06-30-reason-honesty-regression-checks-closure.md), [preview-policy closure](../planning-artifacts/sprint-change-proposal-2026-06-30-configuration-preview-scope-closure.md) and focus/conformance tests. All six are tracked done. | The shared guard exists, but create and member/metadata wiring remains incomplete. Current Story 3.5 is configuration set; historical `3-5-tenant-query-gateway-rest-routing.md` remains discoverable and its transport behavior is owned by current 1.10. |
| Epic 4: FR19 docs, correction gate, governance rejection copy, validation blockers | [June 30 follow-through](../planning-artifacts/sprint-change-proposal-2026-06-30-epic-4-retro-follow-through.md), current 5.7 and gateway tests. All four are tracked done. | DW-406 preserves remaining grant copy gaps; DW-562/DW-617 and DW-625 preserve authority/recovery decisions. No missing grant/remove primitive is inferred from the older retro. |
| Epic 5: global correction verification, terminal focus, one projection refresh, docs, support-safety guards | [Approved follow-through](../planning-artifacts/sprint-change-proposal-2026-06-29-epic-5-retro-follow-through.md), current 5.6/5.7 and [historical cleanup](5-8-correction-projection-refresh-cleanup.md), current component/browser tests. All five refreshed actions are tracked done. | The June refresh's four `Open` labels are historical snapshots. Current support-safety DW-315/DW-441 and composed AT/runtime gaps remain carried, rather than being hidden by those done action items. |
| Four additional tracked items | Gitlink guard/checklist, June 30 document-control closure and June 30/July 1 deferred-work follow-through. | Guard attribution/coverage gaps DW-198/DW-437/DW-451/DW-520/DW-534/DW-549/DW-643 remain explicit. |

All retrospective action families have a tracked closure or a named maintenance carry-forward. Several closure claims have a narrower scope than their original success criteria: specifically aggregate admission, support-safety and complete browser evidence. This prevents treating the 29 done actions as release approval.

## Archived evidence and discoverability

- At the original assessment, the ledger contained **643 unique DW IDs: 353 open and 290 done**. All 290 done IDs have corresponding full records in [deferred-work-archive.md](deferred-work-archive.md); no closed ID is missing from the archive. All 353 open IDs occur in the maintenance index. The index's 314 rows merge duplicate references; they are not 314 independent defects.
- Resolved **525 `source_spec` references** across active and archived records, using repository-root paths, same-directory bare filenames, or the recorded current workspace prefix. No referenced source spec was missing. Bare filenames and absolute workspace paths remain less portable than repository-relative links.
- The active ledger and maintenance index do not provide a direct `deferred-work-archive.md` link. Closed IDs remain searchable, but the archive's discovery route is implicit. This report supplies the direct link; adding a permanent index pointer is documentation follow-up, not missing evidence or permission to move the archive.
- The canonical architecture explicitly references the existing [archived architecture spine](../archive/planning-artifacts/architecture-tenants-2026-06-25/ARCHITECTURE-SPINE.md). Canonical AD-1–AD-14 govern over historical descriptive sections.
- Current numbers differ from retained June story filenames: create 2.1 → current 3.1; metadata 2.5 → 3.2; lifecycle/configuration historical 3.1–3.4 → current 3.3–3.6; fixed review 4.2 → 1.11; grant/removal 4.3/4.4 → current 4.2/4.3. Historical 5.8's single-refresh work is folded into current 5.6/5.7. The tables above make these records discoverable without renaming or counting them as extra planned stories.
- June unchecked review items in the old 1.2 and 5.5 records, and the settled pointer decision retained in 1.4, are historical evidence. Conversely, a done spec is not sufficient reason to discard an unresolved DW record. Story 5.6's old linked-proof language is qualified by its frozen matrix and current implementation (DW-504).
- Local Markdown-link inspection found no genuine missing file among the selected planning, retrospective, story and evidence links; one apparent match was a regex example inside code. This is a file-existence check, not verification of every fragment or historical statement.
- Executable historical attribution is still incomplete: 23 of 25 selected spec gitlink validations failed at current HEAD; 4.3 and 5.7 passed. Most failures compare an old baseline to today's unrelated dependency moves; two specs lack a baseline. They do **not** prove those later pointers belonged to the old story. Preserve DW-198/DW-461/DW-467/DW-478/DW-549 and choose immutable story-end/owned-commit provenance before claiming complete historical revalidation. Do not follow the validator's revert suggestions in this verification task.

## Validation evidence and gaps

### Executed local checks

All commands ran from the repository root. Raw logs, command manifests, coverage reports and TRX files are in `/tmp/tenants-release-readiness-20261012/`; these are session artifacts. The results and commands are recorded here so the assessment does not depend on that temporary directory remaining available.

| Check | Exact command | Result |
| --- | --- | --- |
| Skill customization | `uv run _bmad/scripts/resolve_customization.py --skill .agents/skills/bmad-sprint-planning --project-root . --key workflow` | Exit 0; no prepend/append facts or completion actions. |
| Skill configuration | `uv run _bmad/scripts/resolve_config.py --project-root .` | Exit 0; English, existing planning/implementation locations. |
| Sprint format | `uv run .agents/skills/bmad-sprint-planning/scripts/sprint_plan.py validate --status-file _bmad-output/implementation-artifacts/sprint-status.yaml` | Exit 0; `valid=true`, `problems=[]`, `legacy_mapped=[]`; no write. |
| Restore | `dotnet restore Hexalith.Tenants.slnx -m:1 -nr:false` | Exit 0. No dependency-version changes. |
| Release build | `dotnet build Hexalith.Tenants.slnx --no-restore --configuration Release -warnaserror -m:1 -nr:false` | Exit 0; 0 warnings, 0 errors; 32.48 s. |
| Python guard regressions | `python3 tests/scripts/test_validate_story_gitlinks.py` | Exit 0; 23 tests passed. DW-643 still identifies an untested guard-policy gap. |
| Browser harness | `TENANTS_BROWSER_BUILD_CONFIGURATION=Release tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh` | Exit 0 on Chrome 154. Rendered EN/FR tenant/global preview, paired receipt, narrow/desktop, forced colors and focus passed; negative tabindex/focus/CSS controls were rejected. This is the repository harness, not every authenticated composed application journey or human AT proof. |
| Source integration build | `dotnet build tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false --no-incremental` | Exit 0; 0 warnings/errors; 42.27 s. |
| Real-handler provenance | `dotnet test --project tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-build --no-restore -p:UseHexalithProjectReferences=true --minimum-expected-tests 4 --filter-method Hexalith.Tenants.IntegrationTests.TenantsApiGeneratedControllerTests.GlobalAdministratorsRealHandlerMetadataSurvivesRouterRestClientAndUiGateway --results-directory /tmp/tenants-release-readiness-20261012/integration-source --report-xunit-trx --report-xunit-trx-filename provenance.trx` | Exit 0; 4 passed, 0 failed/skipped. Real handler metadata survives router, REST client and UI gateway. |
| Live generated API / persisted Redis | `dotnet test --project tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Release --no-build --no-restore --filter-method '*Generated_tenants_api_get_tenant_reads_verified_redis_state_with_projection_authority' --minimum-expected-tests 1 --results-directory /tmp/tenants-release-readiness-20261012/integration-live --report-xunit-trx --report-xunit-trx-filename live-generated-api.trx` | Exit 0; 1 passed, 0 failed/skipped; 27.28 s. Includes command/projection identity and persisted Redis authority beyond a health response. |
| Full non-performance integration | `dotnet test --project tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Release --no-build --no-restore --filter-not-trait 'Category=Performance' --minimum-expected-tests 184 --results-directory /tmp/tenants-release-readiness-20261012/integration-full --report-xunit-trx --report-xunit-trx-filename non-performance.trx` | Exit 0; 184 passed, 0 failed/skipped; 72.78 s. Includes the live generated API case. |

The seven blocking test projects ran individually through the selected Microsoft.Testing.Platform runner, with coverage. No solution-level test command or direct-assembly substitute was used.

| Project | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| Contracts | 154 | 0 | 0 |
| Client | 50 | 0 | 0 |
| Testing | 181 | 0 | 0 |
| Sample | 40 | 0 | 0 |
| AppHost | 6 | 0 | 0 |
| Server | 834 | 0 | 0 |
| UI | 4,085 | 0 | 0 |
| **Blocking total** | **5,350** | **0** | **0** |

Exact blocking project commands (each exited 0):

```bash
dotnet test --project tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Release --no-build --no-restore --results-directory /tmp/tenants-release-readiness-20261012/test-results/Contracts --report-xunit-trx --report-xunit-trx-filename Contracts.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml
dotnet test --project tests/Hexalith.Tenants.Client.Tests/Hexalith.Tenants.Client.Tests.csproj --configuration Release --no-build --no-restore --results-directory /tmp/tenants-release-readiness-20261012/test-results/Client --report-xunit-trx --report-xunit-trx-filename Client.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml
dotnet test --project tests/Hexalith.Tenants.Testing.Tests/Hexalith.Tenants.Testing.Tests.csproj --configuration Release --no-build --no-restore --results-directory /tmp/tenants-release-readiness-20261012/test-results/Testing --report-xunit-trx --report-xunit-trx-filename Testing.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml
dotnet test --project samples/Hexalith.Tenants.Sample.Tests/Hexalith.Tenants.Sample.Tests.csproj --configuration Release --no-build --no-restore --results-directory /tmp/tenants-release-readiness-20261012/test-results/Sample --report-xunit-trx --report-xunit-trx-filename Sample.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml
dotnet test --project tests/Hexalith.Tenants.AppHost.Tests/Hexalith.Tenants.AppHost.Tests.csproj --configuration Release --no-build --no-restore --results-directory /tmp/tenants-release-readiness-20261012/test-results/AppHost --report-xunit-trx --report-xunit-trx-filename AppHost.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml
dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Release --no-build --no-restore --results-directory /tmp/tenants-release-readiness-20261012/test-results/Server --report-xunit-trx --report-xunit-trx-filename Server.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-build --no-restore --results-directory /tmp/tenants-release-readiness-20261012/test-results/UI --report-xunit-trx --report-xunit-trx-filename UI.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml
```

Together with the full non-performance integration suite, **5,534 Release test cases passed**. The extra one-case live and four-case Debug runs are focused corroboration, not five additional unique Release cases.

Coverage command:

```bash
python3 scripts/validate-coverage.py --coverage-root /tmp/tenants-release-readiness-20261012/test-results --minimum-line-coverage 80 --required-branch-coverage 100 --line-scope src/Hexalith.Tenants.Contracts/ --line-scope src/Hexalith.Tenants.Client/ --line-scope src/Hexalith.Tenants.Server/ --line-scope src/Hexalith.Tenants.Testing/ --isolation-auth-target src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs --isolation-auth-target src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsAggregate.cs --isolation-auth-target src/Hexalith.Tenants.Server/Validators/ChangeUserRoleValidator.cs --summary-file /tmp/tenants-release-readiness-20261012/coverage-summary.md
```

Exit 0: scoped line coverage **95.66% (1,168/1,221)**; configured isolation/auth branch coverage **100% (116/116)**. Coverage measures exercised code, not adequacy of the bootstrap actor contract, caller identity binding or every adversarial interleaving.

### Exact-source hosted evidence

Read GitHub Actions job metadata and downloaded TRX artifacts for the assessed SHA; did not infer job health from the aggregate workflow badge.

| Hosted evidence | Observed result | Readiness implication |
| --- | --- | --- |
| [Tenants CI 38269521776](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/38269521776) | Build-and-test job `114880391875` succeeds; all seven TRX counts match the local 5,350 cases. Consumer-reference validation and coverage steps succeed. Aspire job `114882247420` fails: 183/184, zero skipped. | Aggregate CI succeeds because the Aspire job is advisory. It is not an entirely green integration result. No package publication or production deployment was rehearsed here. |
| Same run's failed generated API case | TRX: `tenants-api /alive did not return HTTP 200 within 00:04:00`; last error is SSL connection establishment failure. The command/projection assertions are not reached on that runner. | DW-594 remains a hosted environment/evidence blocker. Local 184/184 establishes a passing local environment; it does not fix or waive the hosted failure. |
| [Story Guards 38269520326](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/38269520326) | Python guard lane succeeds; Chromium job `114880028087` fails `profile-shipped`, exit 124 at its 30-second bound on pinned Chrome `153.0.8010.52`. | DW-464/DW-496/DW-505/DW-555 remain hosted-browser qualification gaps. Local Chrome 154 passing is useful evidence with a different browser/environment. |
| [Source-reference 38269520259](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/38269520259) | Job `114880029466` succeeds; four real-handler provenance cases pass. | Confirms the selected source-route seam, not all upstream or live topology invariants. |
| [EventStore CI 38266659178](https://github.com/Hexalith/Hexalith.EventStore/actions/runs/38266659178) at adopted source `3d0b738d` | Build-and-test and Contracts jobs fail. Downloaded TRX shows two Client projection retirement/stage failures, one `DomainServiceIsolationTests` failure, four package-validator failures and one `ProjectionDispatchContractTests` failure. Other source-mode/integration workflows pass. | DW-595 remains upstream qualification work. This is not a claim that all EventStore lanes or the published 3.120.0 package fail; adoption of this source still lacks a wholly green owning CI record. Historical secret-scan failures are not asserted as current failures. |

Read-only retrieval commands included:

```bash
gh api 'repos/Hexalith/Hexalith.Tenants/actions/runs?head_sha=7a2a126f09274e9324d72fe20d1c086a4a6b7276&per_page=15'
gh api repos/Hexalith/Hexalith.Tenants/actions/runs/38269521776/jobs
gh api repos/Hexalith/Hexalith.Tenants/actions/runs/38269520326/jobs
gh api repos/Hexalith/Hexalith.Tenants/actions/runs/38269520259/jobs
gh run download 38269521776 --repo Hexalith/Hexalith.Tenants --name aspire-test-results --dir /tmp/tenants-release-readiness-20261012/ci-aspire-results
gh run download 38269521776 --repo Hexalith/Hexalith.Tenants --name blocking-test-results --dir /tmp/tenants-release-readiness-20261012/ci-blocking-results
gh run view 38269520326 --repo Hexalith/Hexalith.Tenants --job 114880028087 --log
gh api 'repos/Hexalith/Hexalith.EventStore/actions/runs?head_sha=3d0b738d23e51ff765004e3f76ee7863e83b5ee6&per_page=10'
gh run download 38266659178 --repo Hexalith/Hexalith.EventStore --name blocking-test-results --dir /tmp/tenants-release-readiness-20261012/eventstore-blocking-results
gh run download 38266659178 --repo Hexalith/Hexalith.EventStore --name contracts-test-results --dir /tmp/tenants-release-readiness-20261012/eventstore-contracts-results
```

All these retrievals succeeded. `gh run view` returned empty logs for the Tenants CI build/Aspire jobs; their results above were verified from job metadata and downloaded TRX instead.

### Story gitlink verification commands and outcomes

For each artifact in the table, executed exactly `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/<filename>` with `<filename>` replaced by the displayed filename, without `--head` or any mutation. This selected scan includes related historical 1.12 and WP-2A specs; it is not a claim of 25 additional planned stories. The full commands and output are retained in the session guard logs.

| Filename | Exit | Result |
| --- | ---: | --- |
| [spec-1-10-direct-tenants-reads-and-authoritative-freshness.md](spec-1-10-direct-tenants-reads-and-authoritative-freshness.md) | 1 | UNDECLARED, MISSTATED |
| [spec-1-11-authorized-global-administrator-review.md](spec-1-11-authorized-global-administrator-review.md) | 1 | UNDECLARED |
| [spec-1-12-projection-lifecycle-badges.md](spec-1-12-projection-lifecycle-badges.md) | 1 | UNDECLARED, MISSTATED |
| [spec-1-6-read-only-tenant-configuration.md](spec-1-6-read-only-tenant-configuration.md) | 1 | UNDECLARED |
| [spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md](spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md) | 1 | Missing baseline |
| [spec-1-9-authoritative-memories-search-with-protected-paging.md](spec-1-9-authoritative-memories-search-with-protected-paging.md) | 1 | Missing baseline |
| [spec-2-1-reverify-projection-confirmed-membership-command-foundation.md](spec-2-1-reverify-projection-confirmed-membership-command-foundation.md) | 1 | UNDECLARED |
| [spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md](spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md) | 1 | UNDECLARED |
| [spec-2-4b-wp-2a-removal-proof-and-audit-available.md](spec-2-4b-wp-2a-removal-proof-and-audit-available.md) | 1 | UNDECLARED |
| [spec-3-1-create-tenant-with-projection-confirmation.md](spec-3-1-create-tenant-with-projection-confirmation.md) | 1 | UNDECLARED |
| [spec-3-2-edit-tenant-metadata-with-recorded-updates.md](spec-3-2-edit-tenant-metadata-with-recorded-updates.md) | 1 | UNDECLARED |
| [spec-3-3-lifecycle-and-configuration-availability-guardrail.md](spec-3-3-lifecycle-and-configuration-availability-guardrail.md) | 1 | UNDECLARED |
| [spec-3-4-disable-or-enable-tenant-with-complete-preview.md](spec-3-4-disable-or-enable-tenant-with-complete-preview.md) | 1 | UNDECLARED |
| [spec-3-5-set-namespaced-configuration-with-complete-preview.md](spec-3-5-set-namespaced-configuration-with-complete-preview.md) | 1 | UNDECLARED |
| [spec-3-6-remove-configuration-key-with-complete-preview.md](spec-3-6-remove-configuration-key-with-complete-preview.md) | 1 | UNDECLARED |
| [spec-4-1-fixed-scope-global-administrator-action-availability.md](spec-4-1-fixed-scope-global-administrator-action-availability.md) | 1 | UNDECLARED |
| [spec-4-2-grant-global-administrator-with-projection-confirmation.md](spec-4-2-grant-global-administrator-with-projection-confirmation.md) | 1 | UNDECLARED, MISSTATED |
| [spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md](spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md) | 0 | PASS |
| [spec-5-1-browse-tenant-audit-trail.md](spec-5-1-browse-tenant-audit-trail.md) | 1 | UNDECLARED |
| [spec-5-2-reach-scoped-audit-evidence-from-context.md](spec-5-2-reach-scoped-audit-evidence-from-context.md) | 1 | UNDECLARED |
| [spec-5-3-view-a-support-safe-audit-evidence-receipt.md](spec-5-3-view-a-support-safe-audit-evidence-receipt.md) | 1 | UNDECLARED |
| [spec-5-4-understand-audit-availability-and-recovery.md](spec-5-4-understand-audit-availability-and-recovery.md) | 1 | UNDECLARED |
| [spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md](spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md) | 1 | UNDECLARED, MISSTATED |
| [spec-5-6-preview-confirm-and-link-a-tenant-correction.md](spec-5-6-preview-confirm-and-link-a-tenant-correction.md) | 1 | MISSTATED |
| [spec-5-7-correct-global-administrator-authority-from-audit-evidence.md](spec-5-7-correct-global-administrator-authority-from-audit-evidence.md) | 0 | PASS |

Both missing-baseline outcomes are validation blockers. UNDECLARED/MISSTATED results require historical attribution, not a dependency reset. The passing owned-commit branch remains subject to DW-643, so its success is not proof that every later pointer movement is governed.

### Performance and unexecuted acceptance evidence

Recomputed all six raw v4 batch files under [accepted performance artifacts](story-5-1-performance-v4-authoritative-2026-09-24/summary.json): **5,040 samples, 126 groups, 40 samples per group, zero failures or percentile discrepancies**, ten passing functional gates. Both compressed and uncompressed source-patch hashes match the recorded evidence. This validates the archived measurement record at clean source `697cdb58c33ddeb94690d8e5a5716da407dae4bf`; it does not rerun its browser workload or transfer those timings to current HEAD.

The authoritative runner command is `scripts/run-tenant-audit-performance.sh`, with `AUDIT_PERF_DEDICATED_RUNNER=1` and an isolated result directory, as recorded in [performance evidence](story-5-1-performance-evidence.md). **Not rerun here:** the current host reports 14 available CPUs, rather than the approved dedicated 4-vCPU/8-GiB environment. The runner also provisions browser dependencies and measurement data. No smoke timing, skipped nightly job, existing fast unit test or relaxed budget substitutes for candidate qualification. The approved 50-row policy and conditional 25-row fallback remain unchanged.

Also not established here: production IdP/secret-provider startup and revocation evidence, the complete six-route authenticated live read matrix, authenticated copy/denial journeys on all read surfaces, and human NVDA/VoiceOver behavior. Exact fixtures, owners and missing checks remain in the story evidence and DW records (including DW-104/DW-114/DW-116/DW-122/DW-161/DW-299/DW-333/DW-334/DW-335/DW-336/DW-357/DW-358/DW-359/DW-432/DW-436/DW-446/DW-459/DW-489). Existing component/harness evidence covers parts of those contracts; no row-specific Product/UX waiver is invented to close the rest.

## Confirmed release blockers

All seven B1–B7 source bundles are closed in the validated working tree. Their rows retain the original source findings and closure requirements as history; B7 adds current authority, measured viewport, stale-result and submission/retention regressions. Confirmation here means inspected implementation, consistent with the reconciled ledger; it does not imply a newly reproduced production incident. The original green tests lacked the negative cases needed to reject these behaviors; B2 has caller-transition and retained-proof regressions; B3 adds exact-identity, current-authority and exact-attempt removal regressions; B4 adds held-continuation, exact retained-attempt/lease and same-ID recovery regressions. Owners remain those recorded in the ledger; `unassigned` is not replaced with invented accountability.

| ID | Historical source finding and violated requirement | Closure evidence and original requirement |
| --- | --- | --- |
| **B1 — Bootstrap identity and credential transport (closed in working tree)** | DW-567: [GlobalAdministratorsAggregate](../../src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsAggregate.cs), lines 11–19, handles bootstrap without `CommandEnvelope` and records payload `UserId` as target and actor. DW-550/DW-598: [credential provider](../../src/Hexalith.Tenants/Bootstrap/TenantBootstrapCredentialProvider.cs), `AcquireFromAuthorityAsync`, posts username/password to the configured authority without an HTTPS/environment gate. Subject checking in the hosted provider does not bind arbitrary authenticated bootstrap command payloads. Production JWT validation applies to another setting. | **Closed 2026-10-12:** [remediation evidence](#b1-remediation-evidence-2026-10-12). Original requirement: bind authenticated bootstrap actor/target at the owning domain boundary; reject insecure production credential transport before I/O. Require mismatch and production-HTTP no-request regressions. Preserve the Development exception. |
| **B2 — Caller-bound retained reads (closed in working tree)** | DW-142/DW-580: [TenantQueryGateway](../../src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs), `CanRetainUserTenants`, `CanRetainGlobalAdministrators` and `CanRetainTenantList` compare entity/page/validator without caller provenance. Audit retention has the stronger caller-bound contract. DW-351/DW-366: [TenantDetailPage](../../src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor), lines 2170/2474, uses caller-free `MatchesScope`. | **Closed 2026-10-12:** [remediation evidence](#b2-remediation-evidence-2026-10-12). Original requirement: bind retained data/validators and detail proof capabilities to current caller; prove authentication-transition, failure and 304 behavior. FrontComposer remount containment is relevant but does not establish the gateway contract; no live cross-caller disclosure incident is claimed. |
| **B3 — Exact command and authoritative projection/removal proof (closed in working tree)** | DW-300: create, add/change/remove member and metadata still construct two-argument handles with `AggregateId=null`; gateway mismatch refusal is conditional on an aggregate ID. DW-286: parent member providers at detail lines 2359–2375 and workspace `FindTenantProjectionEvidenceAsync` return cached model/version without current freshness/lifecycle qualification. DW-222: [removal flow](../../src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor), `TryAssembleRemovalProofAsync`, calls target/event/time matching without command correlation/committed sequence. Its comment expressly permits a concurrent operator's removal evidence. | **Closed 2026-10-13:** [remediation evidence](#b3-remediation-evidence-2026-10-13). Original requirement: complete the already-selected aggregate-aware migration, propagate current authoritative proof metadata, and require removal causation tied to the submitted attempt. Negative cases must reject wrong command/aggregate, stale matching state and concurrent same-target removal. |
| **B4 — Admission and retained-attempt ownership (closed in working tree)** | DW-199/DW-288: [CreateTenantFlow](../../src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor) uses `_isSubmitting`/handle retention without acquiring the aggregate gate; its nonaccepted branch drops a returned MessageId. DW-287: detail route/disposal retains lifecycle/configuration leases but releases member/metadata leases while work can continue with `CancellationToken.None`. DW-458/DW-471/DW-472/DW-500: member recovery delegates can reset owned work, apply late status or lose ambiguous add/change ownership. DW-421: terminal adopted grant handling does not consistently release the lease. | **Closed 2026-10-13:** [remediation evidence](#b4-remediation-evidence-2026-10-13). Original requirement: Retain exact attempt and admission through ambiguous/accepted/pending states, route changes and disposal; guard recovery and late continuations by attempt generation/terminal state. Prove same-ID recovery, no competing dispatch and unrelated-aggregate availability. A retained handle alone is not an admission lease. |
| **B5 — Support-safe failure and identifier policy (closed in working tree)** | DW-315: [TenantCommandGateway](../../src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs), `BoundSafeFailureReason`, returns up to 160 backend characters unless a small denylist matches. DW-441: [TenantAuditSupportSafety](../../src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditSupportSafety.cs) still uses denylist/normalization rules that omit documented dotted-phone and confusable-divider shapes. Approved D5 already selected allowlisting/confusable handling. | **Closed 2026-10-13:** [remediation evidence](#b5-remediation-evidence-2026-10-13). Original requirement: implement the settled safe code/key and identifier policy; reject unknown backend text and the documented confusable/PII shapes across rendering, copying and announcements. Do not request a new policy decision for already-approved D5. |
| **B6 — Audit-stream collision during projection/replay (closed in working tree)** | DW-626/DW-628: [global handler](../../src/Hexalith.Tenants/Projections/GlobalAdministratorProjectionHandler.cs) replaces `audit:system`; [tenant handler](../../src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs) merges `audit:` + AggregateId. `CreateTenant` permits literal aggregate ID `system`. The rollout docs prescribe singleton replay, which can overwrite that tenant's audit evidence. | **Closed 2026-10-13:** [B6 evidence](#b6-remediation-evidence-2026-10-13). Original requirement: separate or safely merge the supported streams with compatibility/replay end-state proof; document safe rollout prerequisites. Existing ordered global projection versions do not protect the colliding audit key. No replay was performed by this assessment. |
| **B7 — Fail-closed live authority and safe-width admission (closed in working tree)** | DW-206/DW-235: metadata/member authorization flags default true and their parent omits live bindings. DW-264: workspace create authority follows list surface kind rather than resolved global authority. DW-195: removal relies on CSS hiding without the already-selected submit-time measured viewport gate. | **Closed 2026-10-14:** [B7 evidence](#b7-remediation-evidence-2026-10-14). Original requirement: wire the established live authority and safe-width contract; test indeterminate/denied authority and unknown/narrow viewport with zero dispatch. Backend rejection remains a separate protection and does not satisfy the UI's pre-dispatch acceptance criteria. |

The original assessment represented 26 open ledger records across B1–B7: DW-142, DW-195, DW-199, DW-206, DW-222, DW-235, DW-264, DW-286, DW-287, DW-288, DW-300, DW-315, DW-351, DW-366, DW-421, DW-441, DW-458, DW-471, DW-472, DW-500, DW-550, DW-567, DW-580, DW-598, DW-626 and DW-628. Duplicates are included in the record count, not counted as extra independent blockers. Following B1 validation, DW-550/DW-567/DW-598 are done; **23 records across B2–B7 remained open at B1 closure**. Following B2 validation, DW-142/DW-580/DW-351/DW-366 are also done; **19 records across B3–B7 remained open at B2 closure**. Following B3 validation, DW-222/DW-286/DW-300 are also done; **16 records across B4–B7 remained open at B3 closure**. Following B4 validation, its eight records are also done; **eight records across B5–B7 remained open at B4 closure**: DW-315/DW-441, DW-626/DW-628 and DW-195/DW-206/DW-235/DW-264. Following B5 validation, DW-315/DW-441 are also done; **six records across B6/B7 remained open at B5 closure**: DW-626/DW-628 and DW-195/DW-206/DW-235/DW-264. Following B6 validation, DW-626/DW-628 are also done; **four B7 records remained open at B6 closure**: DW-195/DW-206/DW-235/DW-264. Following B7 validation, those four are also done; all 26 original B1–B7 source-blocker records are closed. Release qualification and policy holds remain.

Hosted API TLS and Chrome qualification, current performance/NFR10 evidence and upstream qualification are **release evidence gaps**, distinct from B1–B7's source defects. DW-499/DW-502/DW-517 also retain platform-owned terminal-status repair, unverifiable publication-resume and secret-scan coverage work; current local Tenants tests do not close their owning acceptance contracts. The framework's advisory-status design must not be silently rewritten into a new guarantee.

## Follow-up work and decisions

### Release disposition of all remaining records

The original supplied index reconciled **265 actionable, 15 blocked, 26 awaiting decision, 9 accepted limitations and 38 uncertain = 353 open records**. Its 86 newly closed records plus 204 earlier closed records account for the archive's 290. B1 remediation changes only DW-550/DW-567/DW-598 to done: **262 actionable + 15 blocked + 26 awaiting decision + 9 accepted limitations + 38 uncertain = 350 open**. These three closures remain in the ledger with their historical findings; the archive and all other dispositions are unchanged. B2 remediation closes four additional actionable records: **258 actionable + 15 blocked + 26 awaiting decision + 9 accepted limitations + 38 uncertain = 346 open**. At B2 closure, ledger totals were 643 unique IDs, 297 done and 346 open. B3 remediation closes three additional actionable records: **255 actionable + 15 blocked + 26 awaiting decision + 9 accepted limitations + 38 uncertain = 343 open**. At B3 closure, ledger totals were 643 unique IDs, 300 done and 343 open. B4 closes eight additional actionable records: **247 actionable + 15 blocked + 26 awaiting decision + nine accepted limitations + 38 uncertain = 335 open**. At B4 closure, ledger totals were 643 unique IDs, 308 done and 335 open; the archive remains at its historical 290 records, with all 18 B1–B4 closures retained in the active ledger. B5 closes two additional actionable records: **245 actionable + 15 blocked + 26 awaiting decision + nine accepted limitations + 38 uncertain = 333 open**. At B5 closure, ledger totals were 643 unique IDs, 310 done and 333 open. The current baseline archive has 308 IDs and is unchanged in this pass; B1–B4 archive counts above remain historical. The two B5 closures retain their full findings in the active ledger.

B6 closed two and B7 closes four further actionable records: **239 actionable + 15 blocked + 26 awaiting-decision + nine accepted limitations + 38 uncertain = 327 open**. Current totals are 643 unique IDs, 316 done and 327 open across 291 maintenance rows. Only the four B7 records change in this pass.

Apply the following mutually exclusive assessment classification, in order. It covers every open record without declaring unverified risks to be confirmed defects:

| Release classification | Records | Rule and consequence |
| --- | ---: | --- |
| Confirmed B1–B7 source blockers | 0 | All 26 assessed records are closed by their dated evidence. This does not close policy, provenance or release qualification holds. |
| Product/governance decision holds | 7 | DW-540/DW-562/DW-584/DW-593/DW-599/DW-617/DW-625. Resolve or explicitly constrain the release contract before readiness can be approved; implementation and policy are not inferred here. |
| Follow-up after release within existing boundaries | 119 | Remaining P3 records, accepted limitations, remaining awaiting-decision records, and the tooling/historical attribution IDs listed below. These do not require feature expansion to ship the already-approved conservative scope. Dependencies on a blocker remain dependencies. |
| Unresolved release qualification/triage | 201 | Every other open record. These include 88 recorded source defects, 62 proof gaps, 35 unverified risks and 16 recommendations. Determine release applicability, reproduce/close relevant gaps, or record an explicit justified follow-up disposition. **This is not a declaration that all 201 are defects or must all be implemented. It is also not permission to silently ship unresolved acceptance work.** |

The tooling/historical follow-up IDs used by this classification are DW-133/DW-198/DW-343/DW-360/DW-365/DW-384/DW-417/DW-428/DW-429/DW-430/DW-431/DW-437/DW-451/DW-461/DW-462/DW-467/DW-478/DW-520/DW-526/DW-531/DW-534/DW-549/DW-601/DW-643, where still open. They concern historical attribution, skill/tooling behavior or future guard maintenance. In particular, pushall procedure findings are not exercised by this release verification. Whether separate guard workflows must block publication remains DW-584, rather than being decided implicitly from these priorities.

Examples that can follow afterward: French editorial cleanup, dead resources, type/file splits, current-context annotations, manifest description polish, latent second-consumer/numeric-enum boundaries and historical pointers. Accepted limitations remain open for visibility. Reusable audit timeline/grouped mode, generic consequence components, shared token work, RTL shipping and independent hard-delete tooling are outside these five epics' approved delivered scope; do not invent an Epic 6 or use them to reopen completed conservative fallbacks.

### Unresolved product and governance decisions

| Decision records | Preserved question and release boundary |
| --- | --- |
| DW-562/DW-617 | Who owns authoritative/version-bounded global-administrator revocation? The verifier checks projected membership without a freshness/sequence bound. Self-removal copy must not promise immediate token/session invalidation. This is a release authority decision hold, not approval to weaken or bypass authorization. |
| DW-599 | Does FR6's configuration filtering govern direct API disclosure as well as UI display? The API returns authorized tenant configuration wholesale; a policy violation is not established without the boundary decision. Resolve before claiming production confidentiality for that surface. |
| DW-625 | How can an ambiguous global command that actually committed be resolved by authorized MessageId lookup or safe same-ID redelivery when current state invalidates the original preview? Preserve the settled no-already-applied-dispatch rule. |
| DW-540/DW-584 | Must advisory Aspire and separate Story Guards/source-reference outcomes gate publication, and under what prerequisite/skip contract? Current release source validation queries successful `ci.yml` only. It can admit a SHA with the failed hosted cases recorded above; release-policy approval is still missing. |
| DW-593 | What approved relationship must publication/recovery Builds execution revisions satisfy? Current release uses `53d53ae42abf7c87d385a078ab260531480bbf8a`; recovery declares `2eac6955a812465a4308d194ae38fee35b766b05`. SHA syntax alone is not that governance decision. |
| DW-127 | Stronger privacy/completeness for partially hidden search windows. Preserve the recorded conservative/out-of-scope decision until Product/Security changes it; do not invent a stricter requirement or certify full completeness. |
| DW-408/DW-438/DW-440/DW-450/DW-474 | Unknown audit-event compatibility, opaque-reference exceptions, culture-formatted absolute dates and shared danger-badge appearance remain product/shared-policy follow-up. Retain safe refusal and current approved rendering until settled. |
| DW-457/DW-461/DW-467/DW-478/DW-549 | Builds historical Aspire disposition and immutable historical story validation ranges remain governance/evidence questions. Do not erase old failures or attribute unrelated later dependency moves to those stories. |
| DW-460/DW-498 | Public receipt factory metadata contract and authorized post-self-demotion inspection/handoff policy remain unresolved. No result-read authorization exception is assumed. |
| DW-551/DW-554/DW-556/DW-600 | Bootstrap provider endpoint contract, permitted ServiceDefaults package layers, bootstrap retry/operator recovery and duplicate global paging policy remain follow-up decisions. Preserve current fail-closed/one-shot/ordinal-aggregation behavior until approved changes exist. |
| DW-631/DW-636 | Manifest critical-flow completeness and replay recovery-hook representation remain owning-schema decisions. Empty inventories do not establish a supported recovery hook. |

Previously resolved choices remain resolved: all eligible configuration edits require complete preview; literal domain identifiers are not GUIDs/ULIDs; corrections are forward commands; exact committed-sequence proof is required by the current correction specs; unsafe paired proof is withheld; D5 identifier policy, aggregate-aware tracking and the selected viewport/authority reflection work are implementation follow-through, not new approval requests.

## Readiness conclusion

**Planned completion:** all five epics and 32 planned stories have discoverable completion records, and retrospective actions have closure/carry-forward evidence. Historical renumbering, superseded evidence and conservative fallbacks explain apparent extra stories or old blocked cells. This verifies the delivered scope and the tracking structure, with the qualifications above.

**Release readiness:** **not ready**. The original local build, 5,534 Release tests, focused source provenance, browser harness and coverage results coexisted with B1–B7 defects. The B1 and B2 remediation checks below pass, with the final B2 Release build, 5,812 Release cases, source provenance, browser harness and configured coverage gates green. The B3 checks also pass: 5,885 maintained Release cases, source provenance, browser, coverage and package gates are green; the B4 checks pass with 6,011 maintained Release cases and source provenance/browser/coverage/package gates. The B5 checks pass with 6,270 maintained Release cases, EN/FR support observers, browser, coverage and package/consumer gates. B6 and B7 checks also pass as recorded below. Readiness still fails on unresolved authority/recovery/governance decisions, inherited story-gitlink attribution, historically failed hosted critical checks awaiting current qualification, upstream qualification gaps and incomplete current NFR10/performance/production evidence. Whitespace or done statuses are not release proof.

A later readiness decision must retain B1–B7 closure evidence and needs an explicit disposition of the seven decision holds and release-relevant qualification work, passing critical checks in the intended release environment, and current candidate acceptance evidence. No release approval, tracking reset or publication is implied by this report. This B7 pass changes only the four requested findings and their implementation/evidence/index records; historical B1–B6 evidence and sprint tracking are preserved.


## B1 remediation evidence — 2026-10-12

**Disposition: B1 closed in the validated working tree; overall release remains not ready.** Implemented over clean Tenants `2638d7f8e5347f5cba6aba123bc95f5b13c6f2a1` on `main`, using the existing dependency pins. This is uncommitted implementation evidence, not a new immutable release revision. The earlier assessment, hosted failures, provenance, acceptance gaps and unresolved decisions above remain historical evidence. Only DW-567, DW-550 and its duplicate DW-598 changed disposition; no sprint status or archive edit, dependency/submodule update, staging, commit, push, deployment or publication ran.

[Bootstrap aggregate](../../src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsAggregate.cs) now requires `CommandEnvelope`, compares payload target to authenticated `envelope.UserId` with `StringComparison.Ordinal`, and rejects mismatch with `InsufficientPermissionsRejection` before checking bootstrap state. Successful audit attribution and rejection actor attribution use the envelope actor. The existing domain rejection mapping remains HTTP **422**; authentication/RBAC refusals remain separate. [Global state](../../src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsState.cs) replays permission rejections without membership mutation. The Testing fake preserves same-target convenience and adds an explicit actor overload; it calls the same secured handler.

[Credential acquisition](../../src/Hexalith.Tenants/Bootstrap/TenantBootstrapCredentialProvider.cs) validates the independent `EventStore:Authentication:Authority` before constructing the credential form or making a request. Authorities must be well-formed absolute HTTP(S) with a host and no userinfo, query or fragment; HTTPS is required in **every environment other than Development**, including Staging and custom environment names. Invalid/insecure configurations return no credential, so the hosted service sends neither a token request nor a bootstrap command. Local Development HTTP and the existing 120-second symmetric credential remain supported. The Keycloak realm token-path convention, subject check, one-shot retry behavior and unresolved provider-compatibility choices remain unchanged.

[Hosted diagnostics](../../src/Hexalith.Tenants/Bootstrap/TenantBootstrapHostedService.cs) retain stable reason, phase and exception type, without passing a raw exception to logging. [Server regressions](../../tests/Hexalith.Tenants.Server.Tests/Bootstrap/TenantBootstrapHostedServiceTests.cs) assert zero requests for 28 insecure/invalid configurations (including malformed spaces, escapes and backslashes), valid HTTPS and local HTTP paths, exact subject matching, safe token-response handling and redaction across rendered messages, structured state and exception text. Exceptions at authority connectivity, token JSON parsing and command dispatch carry sensitive sentinels in the tests. A real `IHttpClientFactory` logging pipeline additionally proves credential forms and bearer tokens are absent from diagnostics, including trace headers. [Aggregate cases](../../tests/Hexalith.Tenants.Server.Tests/Aggregates/GlobalAdministratorsAggregateTests.cs) cover ordinal/case/whitespace mismatch even with a global-admin hint, fresh/existing state and rejection replay.

[Authenticated JWT integration](../../tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs) proves the command actor comes from `sub`, despite a different `name`/payload target, and reaches the real aggregate's permission rejection with no administrator event. [Dapr integration](../../tests/Hexalith.Tenants.IntegrationTests/DaprEndToEndTests.cs) reads the persisted event stream: mismatch yields only an actor-attributed permission rejection, the same authenticated actor can then bootstrap, and a second matched actor gets `GlobalAdminAlreadyBootstrappedRejection`. Folding the persisted events leaves exactly the authenticated first administrator. Existing valid-bootstrap and duplicate cases also assert persisted event types and attribution.

### Commands and results

Commands ran from the repository root. Logs, TRX and coverage reports are session artifacts under `/tmp/tenants-b1-20261012/`; the results here do not depend on that directory remaining available. No installed package versions changed during restore.

| Check | Exact command | Result |
| --- | --- | --- |
| bmad-build activation | `uv run --no-cache "/home/administrator/projects/hexalith/tenants/_bmad/scripts/render_skill.py" --project-root "/home/administrator/projects/hexalith/tenants" --skill "/home/administrator/projects/hexalith/tenants/.agents/skills/bmad-build"` | Exit 0; rendered workflow read once; one-shot implementation/review. Temporary spec/triage notes kept outside the repository to honor the file scope. |
| Aspire discovery | `aspire ps --format Json` | Exit 0; no pre-existing AppHost session. |
| Baseline start | `aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --no-build` | Exit 0; started the local baseline from existing Debug assets. No dashboard credential copied into evidence. |
| Identity readiness | `aspire wait security-vwawtvwp --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --timeout 15` | Exit 0; healthy. |
| Baseline resource state | `aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json` | Exit 0; Tenants, generated API, UI, EventStore and identity resources reached Running/Healthy. |
| Baseline cleanup | `aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj` | Exit 0; stopped only the session started here before maintained checks. |
| Debug Server source build | `dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false` | Final exit 0; 0 warnings/errors, 17.85 s. Initial compile error recorded below. |
| Debug Server source tests | `dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-build --no-restore -p:UseHexalithProjectReferences=true --minimum-expected-tests 834 --results-directory /tmp/tenants-b1-20261012/server-debug --report-xunit-trx --report-xunit-trx-filename server.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` | Exit 0; 871 passed, 0 failed/skipped. Supplemental source run before the ten additional diagnostic/URI cases; final Release Server run covers all 881. |
| Maintained source integration build | `dotnet build tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false --no-incremental` | Final exit 0; 0 warnings/errors, 44.00 s. Initial compile error recorded below. |
| Source integration assertion rebuild | `dotnet build tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false` | Exit 0; 0 warnings/errors, 27.89 s; corrected the new test's expected existing HTTP mapping. |
| Focused source bootstrap integration | `dotnet test --project tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-build --no-restore -p:UseHexalithProjectReferences=true --filter-method '*Bootstrap*' --minimum-expected-tests 6 --results-directory /tmp/tenants-b1-20261012/integration-bootstrap-source-final --report-xunit-trx --report-xunit-trx-filename bootstrap.trx` | Exit 0; 6 passed, 0 failed/skipped, 6.38 s; includes JWT mismatch and persisted Dapr cases. |
| Maintained source provenance | `dotnet test --project tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-build --no-restore -p:UseHexalithProjectReferences=true --minimum-expected-tests 4 --filter-method Hexalith.Tenants.IntegrationTests.TenantsApiGeneratedControllerTests.GlobalAdministratorsRealHandlerMetadataSurvivesRouterRestClientAndUiGateway --results-directory /tmp/tenants-b1-20261012/integration-provenance-source --report-xunit-trx --report-xunit-trx-filename provenance.trx` | Exit 0; 4 passed, 0 failed/skipped, 1.64 s. |
| Restore | `dotnet restore Hexalith.Tenants.slnx -m:1 -nr:false` | Exit 0. |
| Maintained Release build | `dotnet build Hexalith.Tenants.slnx --no-restore --configuration Release -warnaserror -m:1 -nr:false` | Final exit 0; 0 warnings/errors, 6.80 s after the strict URI audit. Initial compile error recorded below. |
| Full non-performance integration | `dotnet test --project tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Release --no-build --no-restore --filter-not-trait 'Category=Performance' --minimum-expected-tests 187 --results-directory /tmp/tenants-b1-20261012/integration-release --report-xunit-trx --report-xunit-trx-filename non-performance.trx` | Exit 0; 187 passed, 0 failed/skipped, 68.62 s; includes live generated API/Redis evidence. |

All seven blocking projects ran individually through the maintained Microsoft.Testing.Platform runner, with coverage:

| Project | Exact command | Passed / failed / skipped |
| --- | --- | --- |
| Contracts | `dotnet test --project tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Release --no-build --no-restore --results-directory /tmp/tenants-b1-20261012/test-results/Contracts --report-xunit-trx --report-xunit-trx-filename Contracts.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` | Exit 0; 154 / 0 / 0 |
| Client | `dotnet test --project tests/Hexalith.Tenants.Client.Tests/Hexalith.Tenants.Client.Tests.csproj --configuration Release --no-build --no-restore --results-directory /tmp/tenants-b1-20261012/test-results/Client --report-xunit-trx --report-xunit-trx-filename Client.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` | Exit 0; 50 / 0 / 0 |
| Testing | `dotnet test --project tests/Hexalith.Tenants.Testing.Tests/Hexalith.Tenants.Testing.Tests.csproj --configuration Release --no-build --no-restore --results-directory /tmp/tenants-b1-20261012/test-results/Testing --report-xunit-trx --report-xunit-trx-filename Testing.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` | Exit 0; 182 / 0 / 0 |
| Sample | `dotnet test --project samples/Hexalith.Tenants.Sample.Tests/Hexalith.Tenants.Sample.Tests.csproj --configuration Release --no-build --no-restore --results-directory /tmp/tenants-b1-20261012/test-results/Sample --report-xunit-trx --report-xunit-trx-filename Sample.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` | Exit 0; 40 / 0 / 0 |
| AppHost | `dotnet test --project tests/Hexalith.Tenants.AppHost.Tests/Hexalith.Tenants.AppHost.Tests.csproj --configuration Release --no-build --no-restore --results-directory /tmp/tenants-b1-20261012/test-results/AppHost --report-xunit-trx --report-xunit-trx-filename AppHost.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` | Exit 0; 6 / 0 / 0 |
| Server | `dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Release --no-build --no-restore --results-directory /tmp/tenants-b1-20261012/test-results/Server --report-xunit-trx --report-xunit-trx-filename Server.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` | Exit 0; 881 / 0 / 0 |
| UI | `dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-build --no-restore --results-directory /tmp/tenants-b1-20261012/test-results/UI --report-xunit-trx --report-xunit-trx-filename UI.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` | Exit 0; 4085 / 0 / 0 |

**Final Release total: 5,398 blocking + 187 integration = 5,585 passed, zero failed/skipped** (51 more cases than the original 5,534). Debug source runs corroborate these cases and are not added to the unique Release total.

Configured coverage gate command:

```bash
python3 scripts/validate-coverage.py --coverage-root /tmp/tenants-b1-20261012/test-results --minimum-line-coverage 80 --required-branch-coverage 100 --line-scope src/Hexalith.Tenants.Contracts/ --line-scope src/Hexalith.Tenants.Client/ --line-scope src/Hexalith.Tenants.Server/ --line-scope src/Hexalith.Tenants.Testing/ --isolation-auth-target src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs --isolation-auth-target src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsAggregate.cs --isolation-auth-target src/Hexalith.Tenants.Server/Validators/ChangeUserRoleValidator.cs --summary-file /tmp/tenants-b1-20261012/coverage-summary.md
```

Exit 0: scoped package line coverage **95.69% (1,178/1,231)**; configured isolation/auth branch coverage **100% (118/118)**, including both outcomes of the new actor equality guard.

Initial candidate checks and corrections (historical; **no unresolved validation blocker**):

| Exact command | Initial result and correction |
| --- | --- |
| `dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false` | Exit 1, CS8122 at `TenantBootstrapHostedServiceTests.cs:542`: expression trees cannot contain the new `is` pattern. Replaced it with equality; same command passed. |
| `dotnet build tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false --no-incremental` | Exit 1, CS0104 at `CommandApiRuntimeIntegrationTests.cs:2204`: ambiguous `SubmitCommandRequest`. Qualified the owning contract type; same command passed. |
| `dotnet test --project tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-build --no-restore -p:UseHexalithProjectReferences=true --filter-method '*Bootstrap*' --minimum-expected-tests 5 --results-directory /tmp/tenants-b1-20261012/integration-bootstrap-source --report-xunit-trx --report-xunit-trx-filename bootstrap.trx` | Exit 2; 4 passed/2 failed/0 skipped. New JWT tests expected 403 but received the existing 422 domain-rejection mapping. Corrected the assertion to 422 and the exact `insufficient-permissions-rejection` reason code; final focused and full Release lanes passed. |
| `dotnet build Hexalith.Tenants.slnx --no-restore --configuration Release -warnaserror -m:1 -nr:false` | Exit 1, three CS0103 errors at `TenantConformanceTests.cs:701/919/1544` for the newly referenced `TenantIdentity`, plus propagated MSB4181. Added the owning namespace import; same command passed. |

Final scope/whitespace checks: `git -c core.safecrlf=false diff --check` and `git diff --exit-code HEAD -- references _bmad-output/implementation-artifacts/sprint-status.yaml Directory.Packages.props global.json` both exited 0. A record comparison verifies only DW-550/DW-567/DW-598 changed, original `legacy-detail` and pre-fix verification are retained, and counts now reconcile to 350 open. No historical finding was erased.

### Review disposition and remaining limits

bmad-build's configured independent Blind Hunter reviewed the source/test diff (six findings). Safe phase diagnostics and parser/dispatch exception regressions were patched; real factory diagnostic coverage was added and passed. Final validity inspection added `IsWellFormedOriginalString`, six malformed-path zero-request cases and a valid encoded-path case; the Release build, full Server and full non-performance integration checks were rerun successfully. The existing quickstart mismatch example is still DW-575, and the event-contract reference's bootstrap rejection list needs its separate documentation follow-up; neither document nor unrelated ledger record was changed under the requested scope. The behavior and added rejection are documented in this B1 evidence.

Cross-origin HTTPS redirect restriction was a separate hardening recommendation, not an approved B1 authority-origin/endpoint policy; no endpoint-discovery or redirect-policy redesign ran. HTTPS downgrade is refused by the current .NET handler according to [Microsoft's redirect contract](https://learn.microsoft.com/dotnet/api/system.net.http.httpclienthandler.allowautoredirect). The default factory redacts header values according to [Microsoft's logging contract](https://learn.microsoft.com/en-us/dotnet/core/compatibility/networking/9.0/redact-headers); the current factory regression checks bearer-header redaction. Ordinary authority paths may appear in framework HTTP diagnostics; configured URL credentials/query/fragment are refused before any request, and form credentials/tokens do not appear in the exercised logs. This does not certify custom telemetry configured to record request bodies.

Production IdP/secret-provider qualification, provider endpoint compatibility (DW-551), credential response bounds (DW-528/DW-542), bootstrap operator recovery (DW-556), hosted critical checks and current performance/NFR evidence remain outside B1. No performance/browser/production suite was newly run for this change. **B2–B7, the seven decision holds and remaining release qualifications are preserved; release remains not ready.**

## B2 remediation evidence 2026-10-12

**B2 is closed in the validated working tree over Tenants `b4b65dce017bcb3ea0a676f14b3156d908c1e9c0` on `main`.** Only DW-142/DW-580 and DW-351/DW-366 change disposition. Original findings and pre-fix verification/action text remain in those records. B1's evidence section is preserved verbatim. No commit, dependency/submodule update, staging, push or deployment ran; this closure is local working-tree evidence, not release approval.

Applied the repository-local `bmad-build` workflow after reading AGENTS.md, the root-declared AI.Tools baseline and UX instructions, relevant architecture AD-5–AD-8/D6/D7, authentication/subject-casing and Story 1.10 retained-read/freshness contracts, owning build configuration, source and tests. The workflow spec and triage log remain in the ignored `_bmad/render/` directory. Exact skill activation was `uv run --no-cache "/home/administrator/projects/hexalith/tenants/_bmad/scripts/render_skill.py" --project-root "/home/administrator/projects/hexalith/tenants" --skill "/home/administrator/projects/hexalith/tenants/.agents/skills/bmad-build"` (exit 0). The user's explicit implementation authorization and prohibition on commits governed the workflow checkpoints and completion. Only this report, the four corresponding ledger records/count summary and maintenance index were updated after validation.

`TenantQueryGateway` uses the established ordinal subject hash (`AuditCallerScope`) on retained list, user/self memberships, global-administrator, tenant-member and sanitized detail snapshots. Both provenance and entity/page scope must match before previous data or its validator enters a read; an explicit validator must match that retained snapshot. A scoped core uses one captured subject for self-targets and protected search cursor scope. The shared guard checks current identity before and after dependency awaits and final composition, remembers observed rejection, and prevents 304/cursor/search recovery after identity changes. When circuit/provider evidence is available it must corroborate the user context; an active circuit with no provider, pending/faulted state, anonymous identity or disagreement fails closed. Direct request-context reads remain supported when no circuit/provider exists. Observed authentication events reject active work even if the subject returns to its original value. Fresh configuration previews/proofs and Memories awaits share this guard. Complete administrator page walks require one caller throughout.

`TenantDetailPage` clears retained detail/member/global evidence, configuration, ETags, versions, paging history and capabilities on authentication events before awaiting new identity. Current caller provenance gates display/action evidence and member/metadata/lifecycle proofs; both audit capability and metadata-audit matching use `MatchesScope(request, currentCallerScope)`. Operation generations, cancellation and caller-version checks reject old completions, including ignored cancellation and suspended lifecycle reauthorization across a same-subject event. Initial/faulted identity has an accessible localized Fluent refresh action. Sanitized detail/configuration, supported same-caller retention, the detail 304 unconditional re-fetch policy and existing freshness/projection semantics are preserved.

Regressions exercise different callers at identical entity/page scope; populated and authorization-scoped empty evidence; unbound or mismatched validators; valid same-caller 304 and transient retention; 200/304/fault/cursor recovery transitions; sign-out, pending/faulted/malformed identity; HTTP-context/circuit disagreement; mutable A/B/A target/scope and provider events; late BFF composition/retained reauthorization; fresh configuration/search awaits; and obsolete detail/audit/lifecycle/member evidence. Real cancellation assertions verify cancellation at composition entry and no retry. Three independent review layers were triaged; applicable caller-boundary and verification findings were fixed. Existing permission-revision, command-attempt and other release work remains in its owning records.

All commands below ran from the repository root. Raw logs, command/exit manifests, TRX and coverage are session artifacts under `/tmp/tenants-b2-20261012/`; commands and outcomes are recorded here so evidence does not depend on that directory surviving.

| Check | Exact command | Result |
| --- | --- | --- |
| Aspire discovery | `aspire ps --format Json` | Exit 0; no pre-existing session. |
| Aspire baseline start | `aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --isolated` | Exit 0; started only a local baseline. No dashboard credential is copied here. |
| Initial state | `aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json` | Exit 0; identity initially Unhealthy and dependent resources Waiting. |
| Identity readiness | `aspire wait security-yacbnvkj --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --timeout 15` | Exit 0; identity Healthy. |
| Ready state | `aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json` | Exit 0; identity, EventStore, Tenants host/API/UI Running/Healthy. |
| Baseline cleanup | `aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj` | Exit 0; stopped only the session started here. |
| Python guard regressions | `python3 tests/scripts/test_validate_story_gitlinks.py` | Exit 0; 23 passed. Existing historical attribution gaps are not closed. |
| Debug source UI build | `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -m:1 --no-restore` | Exit 0; 0 warnings/errors; 11.95 s. |
| Focused source gateway/detail | `dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -class Hexalith.Tenants.UI.Tests.Services.Gateways.TenantQueryGatewayTests -class Hexalith.Tenants.UI.Tests.Components.TenantDetailSurfaceTests -parallelMode none` | Exit 0; 951 passed; zero failed/errors/skipped; 14.46 s. |
| Restore | `dotnet restore Hexalith.Tenants.slnx -m:1 -nr:false` | Exit 0; No dependency-version edits. |
| Maintained Release build | `dotnet build Hexalith.Tenants.slnx --no-restore --configuration Release -warnaserror -m:1 -nr:false` | Exit 0; 0 warnings/errors; 13.05 s. |
| Maintained source integration build | `dotnet build tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false --no-incremental` | Exit 0; 0 warnings/errors; 33.49 s. |
| Real-handler source provenance | `dotnet test --project tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-build --no-restore -p:UseHexalithProjectReferences=true --minimum-expected-tests 4 --filter-method Hexalith.Tenants.IntegrationTests.TenantsApiGeneratedControllerTests.GlobalAdministratorsRealHandlerMetadataSurvivesRouterRestClientAndUiGateway --results-directory /tmp/tenants-b2-20261012/integration-source --report-xunit-trx --report-xunit-trx-filename provenance.trx` | Exit 0; 4 passed; zero failed/skipped; 1.82 s. |
| Full non-performance integration | `dotnet test --project tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Release --no-build --no-restore --filter-not-trait Category=Performance --minimum-expected-tests 187 --results-directory /tmp/tenants-b2-20261012/integration-release --report-xunit-trx --report-xunit-trx-filename non-performance.trx` | Exit 0; 187 passed; zero failed/skipped; 69.51 s. Includes generated API and persisted Redis end-state. |
| Serialized Chromium harness | `env TENANTS_BROWSER_BUILD_CONFIGURATION=Release tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh` | Exit 0; Chrome 154.0.8037.57; EN/FR, desktop/narrow, forced colors, focus and negative tabindex/focus/CSS controls passed; 18.27 s. |
| Configured coverage gate | `python3 scripts/validate-coverage.py --coverage-root /tmp/tenants-b2-20261012/test-results --minimum-line-coverage 80 --required-branch-coverage 100 --line-scope src/Hexalith.Tenants.Contracts/ --line-scope src/Hexalith.Tenants.Client/ --line-scope src/Hexalith.Tenants.Server/ --line-scope src/Hexalith.Tenants.Testing/ --isolation-auth-target src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs --isolation-auth-target src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsAggregate.cs --isolation-auth-target src/Hexalith.Tenants.Server/Validators/ChangeUserRoleValidator.cs --summary-file /tmp/tenants-b2-20261012/coverage-summary.md` | Exit 0; 95.69% scoped package line coverage (1,178/1,231); 100% configured isolation/auth branches (118/118). |

The seven blocking projects ran individually through the maintained Microsoft.Testing.Platform runner with coverage. The six unaffected package/host suites below were green before the final UI-only review fixes; their source/dependencies did not change. Final Release UI and integration were rerun after all code fixes.

| Project | Exact command | Passed / failed / skipped |
| --- | --- | --- |
| Contracts | `dotnet test --project tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Release --no-build --no-restore --minimum-expected-tests 154 --results-directory /tmp/tenants-b2-20261012/test-results/Contracts --report-xunit-trx --report-xunit-trx-filename Contracts.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` | Exit 0; 154 / 0 / 0 |
| Client | `dotnet test --project tests/Hexalith.Tenants.Client.Tests/Hexalith.Tenants.Client.Tests.csproj --configuration Release --no-build --no-restore --minimum-expected-tests 50 --results-directory /tmp/tenants-b2-20261012/test-results/Client --report-xunit-trx --report-xunit-trx-filename Client.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` | Exit 0; 50 / 0 / 0 |
| Testing | `dotnet test --project tests/Hexalith.Tenants.Testing.Tests/Hexalith.Tenants.Testing.Tests.csproj --configuration Release --no-build --no-restore --minimum-expected-tests 182 --results-directory /tmp/tenants-b2-20261012/test-results/Testing --report-xunit-trx --report-xunit-trx-filename Testing.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` | Exit 0; 182 / 0 / 0 |
| Sample | `dotnet test --project samples/Hexalith.Tenants.Sample.Tests/Hexalith.Tenants.Sample.Tests.csproj --configuration Release --no-build --no-restore --minimum-expected-tests 40 --results-directory /tmp/tenants-b2-20261012/test-results/Sample --report-xunit-trx --report-xunit-trx-filename Sample.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` | Exit 0; 40 / 0 / 0 |
| AppHost | `dotnet test --project tests/Hexalith.Tenants.AppHost.Tests/Hexalith.Tenants.AppHost.Tests.csproj --configuration Release --no-build --no-restore --minimum-expected-tests 6 --results-directory /tmp/tenants-b2-20261012/test-results/AppHost --report-xunit-trx --report-xunit-trx-filename AppHost.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` | Exit 0; 6 / 0 / 0 |
| Server | `dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Release --no-build --no-restore --minimum-expected-tests 881 --results-directory /tmp/tenants-b2-20261012/test-results/Server --report-xunit-trx --report-xunit-trx-filename Server.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` | Exit 0; 881 / 0 / 0 |
| UI | `dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-build --no-restore --minimum-expected-tests 4310 --results-directory /tmp/tenants-b2-20261012/test-results/UI --report-xunit-trx --report-xunit-trx-filename UI.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` | Exit 0; 4312 / 0 / 0 |

**Final Release total: 5,625 blocking + 187 integration = 5,812 passed, zero failed/skipped.** Release UI grew from B1's 4,085 to 4,312 cases: 227 additional cases. Focused Debug/source runs corroborate covered behavior and are not added to the unique Release total. The configured coverage gate scopes publishable packages and does not itself establish UI caller-boundary adequacy; the new behavioral regressions supply that evidence.

Initial candidate failures and resolved validation blockers are retained as history:

| Exact command | Observed result and correction |
| --- | --- |
| `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -m:1 --no-restore` | Initial exit 1: CS1704, current Commons.UniqueIds source conflicted with stale package 2.30.1 assets. Rerun with restore resolved it. Subsequent candidate iterations of this exact command reported CS0246 (new principal-evidence type spelling), CS0117 (unsupported lifecycle enum value), and CS8506/CS8131 (test tuple/switch inference); each was corrected. Final result above is exit 0, zero warnings/errors. Raw iteration logs/manifests and the initial tool-output summary are retained. |
| `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -m:1` | Exit 0 after restore; no dependency versions changed. |
| `dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -class Hexalith.Tenants.UI.Tests.Services.Gateways.TenantQueryGatewayTests -class Hexalith.Tenants.UI.Tests.Components.TenantDetailSurfaceTests -class Hexalith.Tenants.UI.Tests.Services.Gateways.GlobalAdministratorsProjectionLoaderTests -parallelMode none` | Initial exit 1: 751 total/28 failures. Later 908 total/3 failures; then 910, 928 and 929 all passed. Fixed legitimate provenance fixtures, invalid unbound-validator expectations, initial authorization sequencing, search exception propagation and visible identity-recovery behavior; final maintained UI covers all cases. |
| `dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -parallelMode none` | Superseded full run exposed shared positive fixtures without scope and was interrupted with exit 58. Rerun: exit 0, 4,262 passed/zero failed/skipped before the final concurrency/review additions. Final Release UI above covers the complete 4,312-case tree. |
| `dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -class Hexalith.Tenants.UI.Tests.Services.Gateways.TenantQueryGatewayTests -class Hexalith.Tenants.UI.Tests.Components.TenantDetailSurfaceTests -parallelMode none` | Review-fix candidate: exit 1, 945 total/1 failure because DynamicProxy could not proxy the internal cursor codec. Used the existing test codec helper without widening production visibility. Final exit 0: 951 passed/zero failed/errors/skipped. |
| `env TENANTS_BROWSER_BUILD_CONFIGURATION=Release tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh` | First pre-review run passed. Post-review concurrent run exited 1: two paired-receipt export tests hit `BadImageFormatException: Index not found` while the coverage run used the same Release assets. A transient asset/instrumentation interaction is inferred, not conclusively diagnosed. The identical command ran serially after coverage/build activity and passed all browser controls; no source or gate change was needed. |

**No unresolved local B2 validation blocker remains.** Final source, whitespace, ledger/index consistency and preservation checks are recorded below. The browser harness is maintained fixture coverage, not a full authenticated composed cross-caller browser/AT journey. Package consumer/publication checks were not rerun because B2 changes only the non-packable UI and tests; no package/public backend contract changed. Production IdP/secret-provider and provider compatibility, source-specific hosted critical checks, candidate performance/NFR10 and full human assistive-technology evidence remain outside this closure.

Same-subject retained permission revisions (DW-609), retained command attempts (including DW-574/B4), hidden-search policy and all seven product/governance holds remain unresolved in their existing records. B2's current-caller read/proof guarantees do not close those separate contracts. **B3–B7 and remaining release qualifications are preserved; release remains not ready.**

Final preservation checks (all exit 0):

| Check | Exact command | Result |
| --- | --- | --- |
| Whitespace | `git -c core.safecrlf=false diff --check` | No whitespace errors. |
| Protected repository files | `git diff --exit-code HEAD -- references _bmad-output/implementation-artifacts/sprint-status.yaml _bmad-output/implementation-artifacts/deferred-work-archive.md Directory.Packages.props Directory.Build.props Directory.Build.targets global.json .editorconfig .gitattributes .gitmodules` | No changes to references, sprint tracking, archive, dependency/build configuration or shared formatting/submodule configuration. |
| Ledger/index and historical evidence | `python3 /tmp/tenants-b2-20261012/verify-b2-evidence.py` | Session comparison helper verifies the same 643 ledger IDs; only DW-142/DW-351/DW-366/DW-580 changed; original record fields and pre-fix verification/action text preserved; 297 done and 346 open; 258 actionable, 15 blocked, 26 awaiting decision, 9 accepted limitation and 38 uncertain. The maintenance index has 310 rows covering exactly those 346 open IDs; only the two B2 grouped rows were removed. Archive, sprint tracking and B1 evidence remain byte-identical to their pre-B2 copies. |
| Staging | `git diff --cached --exit-code HEAD` | No staged changes. |

## B3 remediation evidence — 2026-10-13

**B3 is closed in the validated working tree. Release remains not ready because B4–B7 and the existing qualification/decision holds remain open.** This pass closes only DW-222, DW-286 and DW-300 over Tenants `facc0a41e495c8a66b7797f49d61d2db7d8d6b49` on `main`. It preserves the historical assessment, original B1/B2 closure and every unrelated ledger record. No dependency/submodule update, staging, commit, push or deployment ran. Local validation packages are unpublished test artifacts.

Read the owning AGENTS.md, required Hexalith baseline and state/UX/Git guidance; architecture AD-5–AD-12; command foundation, authoritative freshness, first-tenant creation and exact removal/correction proof contracts; build/format/package/CI configuration; and affected source/tests before editing. Used the repository-local `bmad-build` workflow with one context-free implementer and its three required independent review lenses. Workflow/spec/log artifacts remain outside tracked documentation and outside the immutable generated skill snapshot.

The first activation command, `uv run --no-cache "/home/administrator/projects/hexalith/tenants/_bmad/scripts/render_skill.py" --project-root "/home/administrator/projects/hexalith/tenants" --skill "/home/administrator/projects/hexalith/tenants/.agents/skills/bmad-build"`, exited 1 with `HALT: generation contains unexpected or missing files` for generation `204d3a3662dc0bb249e9`. All manifest entries and hashes were valid; one extra B2 spec caused the integrity failure. Following the user's `fix`, that extra artifact was preserved at `_bmad/render/recovered-artifacts/204d3a3662dc0bb249e9/spec-b2-caller-bound-retained-reads.md` (SHA-256 `ac0142587cd984452babc16f8a6ffbf3d44390f490e806c6dad9886e7baf79c5`). The identical activation command then exited 0. Renderer source and integrity requirements were unchanged.

### Resulting behavior and regression evidence

| Record | Change and demonstrated boundary |
| --- | --- |
| DW-300 | All five remaining create/add/change/remove-member/metadata handles carry the submitted aggregate and explicit tenants domain. Existing message/correlation/aggregate/domain status verification is required before status can authorize success, rejection or NoOp. Wrong command/aggregate and wrong retained create handles fail closed, including the literal tenant ID `global-administrators`. Pending/retryable lookup retains identity and established event truth; delayed Received/Processing cannot erase stored-event proof. |
| DW-286 | Detail/workspace providers perform unconditional current reads and carry the existing full snapshot with payload, lifecycle, freshness and version together. Matching stale/cached/degraded/unavailable payloads, wrong tenant, absent or malformed version and noncurrent row evidence cannot confirm success. Metadata's mutable version side channel was removed. Held caller-transition and late dismissed-create proofs are discarded. Valid exact-identity/current evidence confirms supported success; legitimate first-tenant creation and B2 retention/detail capability behavior remain correct. |
| DW-222 | Removal retains the verified committed sequence, rejects contradictory positive completions and requires ordered current detail evidence to reach the exact attempt commit beyond baseline. The audit producer and BFF preserve existing internal removal correlation/sequence evidence. One exact tenant/target/event/correlation/sequence receipt is required after a complete bounded current caller-bound consistent audit walk. Concurrent same-target removal, unrelated evidence, mismatched causation/sequence, duplicates and inconsistent pagination/version/caller evidence cannot prove the submitted removal. UI time is not causation and clock skew cannot exclude valid exact evidence. |

The actual tenant audit producer has no ordered ProjectionVersion. Receipt walking uses its established normalized strong ETag fallback, requiring the same marker source and token on every page. An opaque audit marker never substitutes for the ordered tenant detail committed-sequence proof. Internal removal causation remains excluded from UI-state JSON, receipts and rendered diagnostics; unrelated/global audit rows retain existing redaction. Integration exposed and verified both the producer ETag contract and the BFF's prior removal-provenance stripping.

`PersistedRemovalProofSurvivesRealProjectionHandlerQueryRouterRestAndUiGateway` passed through the real persisted read model, query handler/router, REST controller/client and BFF. The live `Aha_moment_demo_revokes_sample_access_from_tenant_events` passed with the submitted remove handle, verified command commit, exact persisted tenant sequence/member absence and matching persisted Redis/API audit correlation/sequence/event reference. The maintained live generated-tenant authoritative read case also passed. These supplement the negative/positive state and component regressions; a fabricated matching payload is not the end-to-end proof.

### Maintained checks: exact commands and final results

The required pre-edit Aspire baseline used only this session's own AppHost. Each command exited 0: `aspire ps --format Json` returned `[]`; `aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj` started it; `aspire wait security-sssgqrhh --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --timeout 15` observed healthy identity infrastructure; `aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json` showed the identity and EventStore/Tenants/API/UI resources running; `aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj` stopped that session before builds. The final integration lane below supplies runtime evidence after the code changes. Dashboard credentials are excluded from this report.

Final blocking suites: **5,697 passed** (154 Contracts, 50 Client, 182 Testing, 40 Sample, six AppHost, 884 Server, 4,381 UI). Non-performance integration: **188 passed**. Combined maintained Release result: **5,885 passed, zero failures, zero skips**. The six suites unaffected by review patches retain their original passing evidence; full UI/integration were rerun after the final patches. Browser ran serially after UI instrumentation completed. Logs, TRX and coverage artifacts are under `/tmp/tenants-b3-20261013/`; `commands.jsonl` records exact argv, exits, timing and log paths.

| Check | Exact command | Result |
| --- | --- | --- |
| source-integration-build | `dotnet build tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false --no-incremental` | Exit 0; source-reference Debug integration graph, zero warnings/errors. Log: `source-integration-build.log`. |
| source-provenance | `dotnet test --project tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-build --no-restore -p:UseHexalithProjectReferences=true --minimum-expected-tests 4 --filter-method Hexalith.Tenants.IntegrationTests.TenantsApiGeneratedControllerTests.GlobalAdministratorsRealHandlerMetadataSurvivesRouterRestClientAndUiGateway --results-directory /tmp/tenants-b3-20261013/integration-source --report-xunit-trx --report-xunit-trx-filename provenance.trx` | Exit 0; four maintained real global-handler/router/REST/BFF provenance cases passed, zero skips. Log: `source-provenance.log`. |
| final-restore | `dotnet restore Hexalith.Tenants.slnx -m:1 -nr:false` | Exit 0; unchanged dependency versions and checked-out root gitlinks. Log: `final-restore.log`. |
| final-release-build | `dotnet build Hexalith.Tenants.slnx --no-restore --configuration Release -warnaserror -m:1 -nr:false` | Exit 0; final maintained Release solution build, zero warnings/errors. Log: `final-release-build.log`. |
| release-contracts | `dotnet test --project tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Release --no-build --no-restore --minimum-expected-tests 154 --results-directory /tmp/tenants-b3-20261013/test-results/Contracts --report-xunit-trx --report-xunit-trx-filename Contracts.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` | Exit 0; 154 passed, zero failures/skips. Log: `release-contracts.log`. |
| release-client | `dotnet test --project tests/Hexalith.Tenants.Client.Tests/Hexalith.Tenants.Client.Tests.csproj --configuration Release --no-build --no-restore --minimum-expected-tests 50 --results-directory /tmp/tenants-b3-20261013/test-results/Client --report-xunit-trx --report-xunit-trx-filename Client.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` | Exit 0; 50 passed, zero failures/skips. Log: `release-client.log`. |
| release-testing | `dotnet test --project tests/Hexalith.Tenants.Testing.Tests/Hexalith.Tenants.Testing.Tests.csproj --configuration Release --no-build --no-restore --minimum-expected-tests 182 --results-directory /tmp/tenants-b3-20261013/test-results/Testing --report-xunit-trx --report-xunit-trx-filename Testing.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` | Exit 0; 182 passed, zero failures/skips. Log: `release-testing.log`. |
| release-sample | `dotnet test --project samples/Hexalith.Tenants.Sample.Tests/Hexalith.Tenants.Sample.Tests.csproj --configuration Release --no-build --no-restore --minimum-expected-tests 40 --results-directory /tmp/tenants-b3-20261013/test-results/Sample --report-xunit-trx --report-xunit-trx-filename Sample.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` | Exit 0; 40 passed, zero failures/skips. Log: `release-sample.log`. |
| release-apphost | `dotnet test --project tests/Hexalith.Tenants.AppHost.Tests/Hexalith.Tenants.AppHost.Tests.csproj --configuration Release --no-build --no-restore --minimum-expected-tests 6 --results-directory /tmp/tenants-b3-20261013/test-results/AppHost --report-xunit-trx --report-xunit-trx-filename AppHost.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` | Exit 0; six passed, zero failures/skips. Log: `release-apphost.log`. |
| release-server | `dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Release --no-build --no-restore --minimum-expected-tests 884 --results-directory /tmp/tenants-b3-20261013/test-results/Server --report-xunit-trx --report-xunit-trx-filename Server.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` | Exit 0; 884 passed, zero failures/skips, including removal audit stamping/persisted projection regressions. Log: `release-server.log`. |
| final-release-ui | `dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-build --no-restore --minimum-expected-tests 4312 --results-directory /tmp/tenants-b3-20261013/test-results/UI --report-xunit-trx --report-xunit-trx-filename UI.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` | Exit 0; 4,381 passed, zero failures/skips; unchanged support-safety gate and B2 retention/detail-proof regressions passed. Log: `final-release-ui.log`. |
| final-release-integration | `dotnet test --project tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Release --no-build --no-restore --filter-not-trait Category=Performance --minimum-expected-tests 188 --results-directory /tmp/tenants-b3-20261013/integration-release-final --report-xunit-trx --report-xunit-trx-filename non-performance.trx` | Exit 0; 188 passed, zero failures/skips. Includes actual Aspire command/status/Redis projection/API removal evidence and persisted real-handler/router/REST/BFF proof. Log: `final-release-integration.log`. |
| browser | `env TENANTS_BROWSER_BUILD_CONFIGURATION=Release tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh` | Exit 0; Chrome 154.0.8037.57. EN/FR desktop/narrow/forced-colors start, preview, recovery, paired receipt and focus checks passed; return-true, in-flow/hidden CSS, unstacked recovery and missing-tabindex controls correctly rejected. Log: `browser.log`. |
| coverage | `python3 scripts/validate-coverage.py --coverage-root /tmp/tenants-b3-20261013/test-results --minimum-line-coverage 80 --required-branch-coverage 100 --line-scope src/Hexalith.Tenants.Contracts/ --line-scope src/Hexalith.Tenants.Client/ --line-scope src/Hexalith.Tenants.Server/ --line-scope src/Hexalith.Tenants.Testing/ --isolation-auth-target src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs --isolation-auth-target src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsAggregate.cs --isolation-auth-target src/Hexalith.Tenants.Server/Validators/ChangeUserRoleValidator.cs --summary-file /tmp/tenants-b3-20261013/coverage-summary.md` | Exit 0; publishable-package line coverage 95.70% (1180/1233), isolation/auth branches 100% (118/118), meeting configured 80%/100% gates. Log: `coverage.log`. |
| python-guards | `python3 tests/scripts/test_validate_story_gitlinks.py` | Exit 0; 23 story-gitlink guard regression cases passed. Log: `python-guards.log`. |
| local-pack | `python3 scripts/pack-release-packages.py /tmp/tenants-b3-20261013/packages 5.8.0-b3-validation` | Exit 0; five local validation packages; no publication or repository version edit. Log: `local-pack.log`. |
| package-validation | `python3 scripts/validate-nuget-packages.py /tmp/tenants-b3-20261013/packages` | Exit 0; all five packages/dependency inventories validated. Log: `package-validation.log`. |
| package-consumers | `python3 scripts/validate-consumer-package-references.py /tmp/tenants-b3-20261013/packages --work-directory /tmp/tenants-b3-20261013/consumers` | Exit 0; isolated Contracts + Client consumer built, Testing consumer built and one infrastructure-free test passed; no ProjectReference fallback. Log: `package-consumers.log`. |
| protected-scope | `git diff --exit-code HEAD -- references _bmad-output/implementation-artifacts/sprint-status.yaml _bmad-output/implementation-artifacts/deferred-work-archive.md Directory.Packages.props Directory.Build.props Directory.Build.targets global.json` | Exit 0; no dependency/gitlink, archive, sprint or protected build configuration changes. Log: `protected-scope.log`. |
| final-whitespace | `git -c core.safecrlf=false diff --check` | Exit 0; no whitespace errors. Log: `final-whitespace.log`. |
| final-document-whitespace | `git -c core.safecrlf=false diff --check` | Exit 0 after evidence updates; no whitespace errors. Log: `final-document-whitespace.log`. |
| staging-preserved | `git diff --cached --exit-code HEAD` | Exit 0; no staged changes. Log: `staging-preserved.log`. |

### Focused implementation/review checks

| Exact command | Result |
| --- | --- |
| `dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -class '*TenantCreateCommandSnapshotTests' -class '*TenantAddMemberCommandSnapshotTests' -class '*TenantChangeRoleCommandSnapshotTests' -class '*TenantRemoveMemberCommandSnapshotTests' -class '*TenantUpdateMetadataCommandSnapshotTests' -class '*TenantCommandAuthoritativeProofTests' -class '*TenantCommandAuditStatesTests' -class '*CreateTenantFlowTests' -class '*AddTenantMemberFlowTests' -class '*ChangeTenantMemberRoleFlowTests' -class '*RemoveTenantMemberFlowTests' -class '*EditTenantMetadataFlowTests' -class '*TenantDetailSurfaceTests' -class '*TenantsWorkspaceTests' -class '*TenantCommandGatewayTests' -class '*TenantQueryGatewayTests' > /tmp/tenants-b3-20261013/ui-focused-4.log 2>&1` | Exit 0; 1767 passed, zero errors/failures/skips. |
| `dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -class '*RemoveTenantMemberFlowTests' -class '*TenantQueryGatewayTests' > /tmp/tenants-b3-20261013/ui-focused-5.log 2>&1` | Exit 0; 778 passed, zero errors/failures/skips. |
| `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -class '*TenantAuditProjectionTests' -class '*TenantProjectionHandlerTests' > /tmp/tenants-b3-20261013/server-focused-1.log 2>&1` | Exit 0; 43 passed, zero errors/failures/skips. |
| `dotnet tests/Hexalith.Tenants.IntegrationTests/bin/Debug/net10.0/Hexalith.Tenants.IntegrationTests.dll -class '*TenantsApiGeneratedControllerTests' > /tmp/tenants-b3-20261013/integration-focused-4.log 2>&1` | Exit 0; 37 passed, zero errors/failures/skips. |
| `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -m:1 -nr:false > /tmp/tenants-b3-20261013/review-fix-build.log 2>&1` | Exit 0; zero build warnings/errors. |
| `dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -class '*CreateTenantFlowTests' -class '*TenantRemoveMemberCommandSnapshotTests' -class '*TenantCommandAuthoritativeProofTests' -class '*TenantMembershipCommandProvenanceTests' -class '*TenantQueryGatewayTests' -class '*SupportSafetyEvidenceGateTests' > /tmp/tenants-b3-20261013/review-fix-focused.log 2>&1` | Exit 0; 849 passed, zero errors/failures/skips. |

### Initial failures, review triage and remaining limits

The initial maintained UI command `dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-build --no-restore --minimum-expected-tests 4312 --results-directory /tmp/tenants-b3-20261013/test-results/UI --report-xunit-trx --report-xunit-trx-filename UI.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` exited 2: 4,367 of 4,368 passed, with `SupportSafetyEvidenceGateTests.No_test_offers_a_ToString_substring_check_as_support_safety_evidence` rejecting three new substring assertions. They were replaced by actual serialized row/snapshot/receipt evidence with deliberate disclosure controls. The gate was unchanged; its focused rerun and the final full 4,381-case UI run passed.

The initial maintained integration command `dotnet test --project tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Release --no-build --no-restore --filter-not-trait Category=Performance --minimum-expected-tests 188 --results-directory /tmp/tenants-b3-20261013/integration-release --report-xunit-trx --report-xunit-trx-filename non-performance.trx` exited 0 with **175 passed / 13 skipped**, reporting Docker prerequisite failure; that run was not treated as full runtime proof. `docker info --format '{{.ServerVersion}}'` subsequently exited 0 in 0.65 seconds and existing Dapr containers were healthy. The final exact integration command above passed **188/188 with zero skips**, including the affected live removal path. No fixture gate, timeout or dependency was weakened.

Initial Debug compilation/fixture failures were corrected before the passing checks: `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -m:1 -nr:false` initially exited 1 (five analyzer/nullability errors, then six outdated proof signatures, later two fixture errors); final builds exited 0 with zero warnings/errors. The integration counterpart initially exited 1 for one test fixture compile error, then built cleanly. The first affected UI class run had 36 failures from formerly unqualified positive fixtures; a later 1,767-case run had one audit-marker fixture failure, followed by the passing 1,767 and 778-case runs above. The generated-controller command shown above initially ran 37 cases with one failure, sequentially exposing missing authoritative admin fixture state, header evidence, and BFF removal-provenance stripping; all 37 then passed. Exact per-run commands, exits and counts remain in `implementation-command-results.jsonl`, with initial logs preserved rather than erased.

All three configured independent reviews completed before triage. Five B3 patch groups addressed the duplicated late-create proof race, signed sequence bounds, conflicting removal commit evidence, the Received/Processing preservation gap and the maintained support-safety assertion failure. The focused 849-case check above and final maintained gates passed. Bounded-history/concurrent-write findings correctly require withholding unavailable audit proof; transport faults already return unavailable through the production gateway. Duplicate display/proof refresh was retained to preserve independent authoritative evidence; no performance-budget failure was established. Existing caller ownership of retained attempts, reconstruction/admission and terminal-status ownership remain in B4/DW-574/DW-472 and their existing records. No new deferred record or unrelated closure was added.

The existing 50-page bound remains: an incomplete or version-changing walk cannot establish audit availability and stays delayed/indeterminate. Valid access confirmation and audit availability remain separate; no exact-lookup endpoint or unconditional liveness guarantee under continuous writes is invented. This closure does not imply B4 admission/retention ownership, B5 support policy, B6 collision/replay, B7 live authority/viewport admission, production identity/revocation or complete authenticated browser/AT qualification.

The accepted audit performance contract still requires its dedicated candidate workload after relevant BFF/query changes. `scripts/run-tenant-audit-performance.sh` with `AUDIT_PERF_DEDICATED_RUNNER=1` was not run on this 14-CPU host: the approved environment is dedicated 4-vCPU/8-GiB. Historical measurements, approved budgets, current candidate performance/NFR10 qualification gaps and hosted Chrome/TLS evidence remain in their existing scope; passing local tests/browser checks do not close them. All product/governance decisions are preserved.

### Evidence and scope preservation

`python3 /tmp/tenants-b3-20261013/verify-b3-evidence.py` exited 0 and verifies the same 643 ledger IDs; only DW-222/DW-286/DW-300 changed, with original findings/decisions and pre-fix verification/actions preserved. Current counts: 300 done / 343 open (reconciled 96 done, 255 actionable, 15 blocked, 26 awaiting decision, nine accepted limitations, 38 uncertain). The maintenance index has 307 unchanged remaining rows covering exactly those 343 open IDs; only three B3 rows were removed. Archive, sprint tracking, original B1/B2 evidence, B4–B7 blocker rows and governance decision text remain byte-identical to their pre-B3 copies. The comparison result is recorded in `preservation-check.json`. No release approval or publication is implied.

## B4 remediation evidence — 2026-10-13

**B4 is closed in the validated working tree. Release remains not ready because B5–B7 and the existing qualification/decision holds remain open.** This pass closes only DW-199/DW-288/DW-287/DW-458/DW-471/DW-472/DW-500/DW-421 over Tenants `2156db8c59e71bbaf6edfc6a6bebc7026c191631` on `main`. Original findings and pre-fix verification/action/date text remain in those records. Historical B1–B3 evidence remains byte-identical. No dependency/submodule update, staging, commit, push or deployment ran; local packages are unpublished validation artifacts.

Followed the owning AGENTS.md and root-declared Hexalith LLM/UX baseline; read architecture AD-5/7/8/12, aggregate admission, retained-attempt ownership, idempotency/recovery and terminal-state contracts, current authoritative/removal proof contracts, build/format/CI configuration and affected source/tests before editing. Each source finding was reverified in the current working tree. Used repository-local `bmad-build`, its implementation agent and three independent review lenses. Workflow artifacts are ignored and outside the immutable generated skill snapshot; exact argv/exits/logs/TRX/coverage are session artifacts under `/tmp/tenants-b4-20261013/`. The durable results below do not depend on those temporary files remaining available.

### Resulting behavior and meaningful regressions

| Records | Demonstrated boundary |
| --- | --- |
| DW-199/DW-288 | Create uses the existing aggregate gate and shared circuit attempt store before gateway I/O. Original resolved ULID, intent, baseline and start/tracking evidence survive unresolved and ambiguous delivery and accepted/pending reconciliation. Supported same-ID retry cannot mint a duplicate or silently change payload. Competing command surfaces refuse the owned aggregate; unrelated aggregates remain available. The legitimate first-tenant authoritative-absence exception still passes. |
| DW-287 | Typed member/metadata attempts and exact leases survive changed routes, disposal, remount and ownerless completion. A replacement renderer adopts full evidence; page/landmark restoration renders original change/removal targets even when their row is absent. Callback faults, pending parent approval and old refusals cannot grant a replacement early or detach its new attempt. Ownerless terminal evidence updates parent sibling availability. Host-provided shared-store composition remains consistent. |
| DW-458/DW-471/DW-472 | Continue's rendered delegates and direct handlers apply owned-attempt safety. Supported read-only detachment retains dispatched ownership. Held submission/status/projection/proof results bind to exact attempt and generation, cannot resurrect a dismissed panel, overwrite replacement evidence, regress terminal truth or release a foreign lease. All five terminal snapshots reject late status shapes; pending, unavailable and indeterminate evidence remain truthful and fail closed. Supported removal terminal proof enrichment remains intact. |
| DW-500 | Create/add/change gateway regressions cover established 408/429/5xx and transport ambiguity, exact identity and retry payload equality. Ambiguous member status nudges preserve the correlationless recovery basis. Existing authorization/freshness/form and removal preview/typed-confirmation guards still apply; blocked recovery retains ownership. The Dapr actor regression retries the same original create/add/change/metadata envelope and verifies the exact persisted values, original correlation and one domain event/sequence. |
| DW-421 | Adopted grant snapshots use the existing release-aware setter. Terminal rejected/failed delivery completed while ownerless releases the exact grant lease on adoption; pending/indeterminate and replacement lease safety stay protected. |
| B1–B3 preservation | Existing bootstrap, caller-bound query retention, exact command/domain/aggregate, current authoritative projection and exact removal correlation/committed-sequence proof remain correct. Same-caller ready removal receipt remount preserves the audit reference and support-safe copyable evidence. New retained receipt/metadata presentation retains current caller provenance and capability checks, withholding previous-caller or pending/denied read facts while preserving command intent/identity/baseline/tracking/lease. Broader retained correction-attempt ownership DW-574 remains open. |

Regression consumers include `TenantRetainedCommandFlowTests`, `TenantRetainedCommandAttemptTests`, `TenantDetailSurfaceTests`, `MemberAccessReviewTests`, create/member/metadata and global-administrator flow tests, gateway/composition/workspace tests and `DaprEndToEndTests.SameIdentityRecoveryPersistsTheOriginalDomainEventExactlyOnce`. Tests hold real async boundaries and exercise rendered flows and ownerless completion; preserved B3 receipt/authoritative negatives and the unchanged support-safety gate pass in the full suite.

### Maintained checks: exact commands and final results

Final maintained Release result: **6,011 passed, zero failures, zero skips**: Contracts 154, Client 50, Testing 182, Sample 40, AppHost six, Server 884, UI 4,506, non-performance integration 189. Four real-handler source-provenance cases also pass. Full UI/integration and builds ran after the review patches. The six unchanged unit lanes and unchanged publishable-package outputs retain their passing evidence without redundant reruns. Browser ran serially after UI coverage instrumentation. Aspire's pre-edit baseline described all 23 resources Running and this session's AppHost was stopped before builds.

| Check | Exact command | Result |
| --- | --- | --- |
| apphost-start | `aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj` | Exit 0; Started this session’s AppHost; tool exit 0 was captured. Observed tool result recorded in `command-results.jsonl`. |
| apphost-describe | `aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json` | Exit 0; All 23 resources Running; retained in apphost-baseline.json. Observed tool result recorded in `command-results.jsonl`. |
| apphost-stop | `aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj` | Exit 0; Stopped this session’s exact AppHost before builds; tool exit 0 was captured. Observed tool result recorded in `command-results.jsonl`. |
| review-build-11 | `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -m:1 -nr:false` | Exit 0; Build succeeded; zero warnings/errors. Log: `review-build-11.log`. |
| review-focused-04 | `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore -p:UseHexalithProjectReferences=true --filter 'FullyQualifiedName~TenantRetainedCommandFlowTests\|FullyQualifiedName~RemoveTenantMemberFlowTests\|FullyQualifiedName~EditTenantMetadataFlowTests\|FullyQualifiedName~LandmarkReplacementRestores\|FullyQualifiedName~LandmarkChildReplacement\|FullyQualifiedName~PageReplacementRendersSibling\|FullyQualifiedName~Host_supplied_attempt_store\|FullyQualifiedName~AmbiguousTenantDeliveryKeepsTheOriginalUlidAndPayload\|FullyQualifiedName~TenantsWorkspaceTests'` | Exit 0; 302 total, 302 passed, 0 failed, 0 skipped. Log: `review-focused-04.log`. |
| source-integration-build | `dotnet build tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false --no-incremental` | Exit 0; Build succeeded; zero warnings/errors. Log: `source-integration-build.log`. |
| source-id-recovery | `dotnet tests/Hexalith.Tenants.IntegrationTests/bin/Debug/net10.0/Hexalith.Tenants.IntegrationTests.dll -method '*SameIdentityRecoveryPersistsTheOriginalDomainEventExactlyOnce'` | Exit 0; 1 total, 0 errors, 0 failed, 0 skipped. Log: `source-id-recovery.log`. |
| source-provenance | `dotnet test --project tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-build --no-restore -p:UseHexalithProjectReferences=true --minimum-expected-tests 4 --filter-method Hexalith.Tenants.IntegrationTests.TenantsApiGeneratedControllerTests.GlobalAdministratorsRealHandlerMetadataSurvivesRouterRestClientAndUiGateway --results-directory /tmp/tenants-b4-20261013/integration-source --report-xunit-trx --report-xunit-trx-filename provenance.trx` | Exit 0; 4 total, 4 passed, 0 failed, 0 skipped. Log: `source-provenance.log`. |
| final-restore | `dotnet restore Hexalith.Tenants.slnx -m:1 -nr:false` | Exit 0; Passed; see named log and recorded evidence. Log: `final-restore.log`. |
| final-release-build | `dotnet build Hexalith.Tenants.slnx --no-restore --configuration Release -warnaserror -m:1 -nr:false` | Exit 0; Build succeeded; zero warnings/errors. Log: `final-release-build.log`. |
| maintained-release-contracts | `dotnet test --project tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Release --no-build --no-restore --minimum-expected-tests 154 --results-directory /tmp/tenants-b4-20261013/test-results/Contracts --report-xunit-trx --report-xunit-trx-filename Contracts.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` | Exit 0; 154 total, 154 passed, 0 failed, 0 skipped. Log: `maintained-release-contracts.log`. |
| maintained-release-client | `dotnet test --project tests/Hexalith.Tenants.Client.Tests/Hexalith.Tenants.Client.Tests.csproj --configuration Release --no-build --no-restore --minimum-expected-tests 50 --results-directory /tmp/tenants-b4-20261013/test-results/Client --report-xunit-trx --report-xunit-trx-filename Client.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` | Exit 0; 50 total, 50 passed, 0 failed, 0 skipped. Log: `maintained-release-client.log`. |
| maintained-release-testing | `dotnet test --project tests/Hexalith.Tenants.Testing.Tests/Hexalith.Tenants.Testing.Tests.csproj --configuration Release --no-build --no-restore --minimum-expected-tests 182 --results-directory /tmp/tenants-b4-20261013/test-results/Testing --report-xunit-trx --report-xunit-trx-filename Testing.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` | Exit 0; 182 total, 182 passed, 0 failed, 0 skipped. Log: `maintained-release-testing.log`. |
| maintained-release-sample | `dotnet test --project samples/Hexalith.Tenants.Sample.Tests/Hexalith.Tenants.Sample.Tests.csproj --configuration Release --no-build --no-restore --minimum-expected-tests 40 --results-directory /tmp/tenants-b4-20261013/test-results/Sample --report-xunit-trx --report-xunit-trx-filename Sample.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` | Exit 0; 40 total, 40 passed, 0 failed, 0 skipped. Log: `maintained-release-sample.log`. |
| maintained-release-apphost | `dotnet test --project tests/Hexalith.Tenants.AppHost.Tests/Hexalith.Tenants.AppHost.Tests.csproj --configuration Release --no-build --no-restore --minimum-expected-tests 6 --results-directory /tmp/tenants-b4-20261013/test-results/AppHost --report-xunit-trx --report-xunit-trx-filename AppHost.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` | Exit 0; 6 total, 6 passed, 0 failed, 0 skipped. Log: `maintained-release-apphost.log`. |
| maintained-release-server | `dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Release --no-build --no-restore --minimum-expected-tests 884 --results-directory /tmp/tenants-b4-20261013/test-results/Server --report-xunit-trx --report-xunit-trx-filename Server.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` | Exit 0; 884 total, 884 passed, 0 failed, 0 skipped. Log: `maintained-release-server.log`. |
| final-release-ui | `dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-build --no-restore --minimum-expected-tests 4464 --results-directory /tmp/tenants-b4-20261013/test-results/UI --report-xunit-trx --report-xunit-trx-filename UI.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` | Exit 0; 4506 total, 4506 passed, 0 failed, 0 skipped. Log: `final-release-ui.log`. |
| final-release-integration | `dotnet test --project tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Release --no-build --no-restore --filter-not-trait Category=Performance --minimum-expected-tests 189 --results-directory /tmp/tenants-b4-20261013/integration-release-final --report-xunit-trx --report-xunit-trx-filename non-performance.trx` | Exit 0; 189 total, 189 passed, 0 failed, 0 skipped. Log: `final-release-integration.log`. |
| browser-final | `env TENANTS_BROWSER_BUILD_CONFIGURATION=Release tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh` | Exit 0; Chrome 154.0.8037.57; EN/FR desktop, narrow, forced-colors and focus/receipt checks passed; all deliberate DOM/CSS/focus mutations rejected. Log: `browser-final.log`. |
| coverage-final | `python3 scripts/validate-coverage.py --coverage-root /tmp/tenants-b4-20261013/test-results --minimum-line-coverage 80 --required-branch-coverage 100 --line-scope src/Hexalith.Tenants.Contracts/ --line-scope src/Hexalith.Tenants.Client/ --line-scope src/Hexalith.Tenants.Server/ --line-scope src/Hexalith.Tenants.Testing/ --isolation-auth-target src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs --isolation-auth-target src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsAggregate.cs --isolation-auth-target src/Hexalith.Tenants.Server/Validators/ChangeUserRoleValidator.cs --summary-file /tmp/tenants-b4-20261013/coverage-summary.md` | Exit 0; 95.70% scoped package line coverage (1180/1233); configured isolation/auth branches 100% (118/118). Log: `coverage-final.log`. |
| python-guards | `python3 tests/scripts/test_validate_story_gitlinks.py` | Exit 0; 23 story-gitlink guard regression cases passed. Log: `python-guards.log`. |
| local-pack | `python3 scripts/pack-release-packages.py /tmp/tenants-b4-20261013/packages 5.8.0-b4-validation` | Exit 0; Five local unpublished validation packages. Log: `local-pack.log`. |
| package-validation | `python3 scripts/validate-nuget-packages.py /tmp/tenants-b4-20261013/packages` | Exit 0; All five package/dependency inventories validated. Log: `package-validation.log`. |
| package-consumers | `python3 scripts/validate-consumer-package-references.py /tmp/tenants-b4-20261013/packages --work-directory /tmp/tenants-b4-20261013/consumers` | Exit 0; Isolated Contracts + Client and Testing consumers built; one infrastructure-free test passed without ProjectReference fallback. Log: `package-consumers.log`. |
| protected-scope | `git diff --exit-code HEAD -- references _bmad-output/implementation-artifacts/sprint-status.yaml _bmad-output/implementation-artifacts/deferred-work-archive.md Directory.Packages.props Directory.Build.props Directory.Build.targets global.json .editorconfig .gitattributes .gitmodules` | Exit 0; No protected dependency/gitlink/build/format, archive or sprint changes. Log: `protected-scope.log`. |
| final-whitespace | `git -c core.safecrlf=false diff --check` | Exit 0; No whitespace errors. Log: `final-whitespace.log`. |
| staging-preserved | `git diff --cached --exit-code HEAD` | Exit 0; No staged changes. Log: `staging-preserved.log`. |
| evidence-preservation | `python3 /tmp/tenants-b4-20261013/verify-preservation.py` | Exit 0; 643 ledger IDs; exactly eight B4 records changed; 308 done / 335 open; 299 remaining index rows. B1–B3 history, B5–B7, governance, archive, sprint, dependencies and staging preserved. Log: `evidence-preservation.log`. |
| final-document-whitespace | `git -c core.safecrlf=false diff --check` | Exit 0; No whitespace errors. Log: `final-document-whitespace.log`. |

### Initial failures and review corrections

Every nonzero initial/iteration result remains recorded; none is silently replaced by a later pass. The original focused failures included fixtures missing circuit services, fixed fake IDs incompatible with exact supplied IDs, expected teardown release behavior, and implementation/continuation assertions; fixtures and source were corrected without weakening admission or proof guards. The first full UI run passed 4,462/4,464 with only `Workspace_allows_create_flow_only_for_authoritatively_empty_unknown_list` and `Workspace_names_the_first_tenant_bootstrap_exception_when_create_stays_available` failing because their plain bUnit fixture lacked the required shared services. Registering the real graph preserved both first-tenant assertions; the final full UI run passes. Compile iterations exposed missing `IRenderedFragment`, nullable `SafeMessage` assertion and nonexistent `DaprTheory` references; each was corrected without dependency changes.

| Check | Exact command | Initial result |
| --- | --- | --- |
| apphost-telemetry-resource-name | `aspire otel logs tenants-ui --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --severity Warning --limit 10 --format Json` | Independent exit not captured; Output: Resource 'tenants-ui' not found. Command ran before stop in one shell; independent exit code was not captured. Observed tool result recorded in `command-results.jsonl`. |
| debug-focused-initial | `dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -class '*CreateTenantFlowTests' -class '*AddTenantMemberFlowTests' -class '*ChangeTenantMemberRoleFlowTests' -class '*RemoveTenantMemberFlowTests' -class '*EditTenantMetadataFlowTests' -class '*TenantCreateCommandSnapshotTests' -class '*TenantAddMemberCommandSnapshotTests' -class '*TenantChangeRoleCommandSnapshotTests' -class '*TenantRemoveMemberCommandSnapshotTests' -class '*TenantUpdateMetadataCommandSnapshotTests' -class '*TenantCommandGatewayTests' -class '*TenantDetailSurfaceTests' -class '*MemberAccessReviewTests' -class '*GlobalAdministratorsPageTests'` | Exit 1; 1083 total, 0 errors, 59 failed, 0 skipped. Log: `debug-focused-initial.log`. |
| debug-focused-02 | `dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -class '*CreateTenantFlowTests' -class '*AddTenantMemberFlowTests' -class '*ChangeTenantMemberRoleFlowTests' -class '*RemoveTenantMemberFlowTests' -class '*EditTenantMetadataFlowTests' -class '*TenantDetailSurfaceTests'` | Exit 1; 515 total, 0 errors, 33 failed, 0 skipped. Log: `debug-focused-02.log`. |
| debug-build-03 | `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -m:1 -nr:false` | Exit 1; CS0246: The type or namespace name 'IRenderedFragment' could not be found (are you missing a using directive or an assembly reference?). Log: `debug-build-03.log`. |
| debug-focused-04 | `dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -class '*TenantRetainedCommandFlowTests' -class '*CreateTenantFlowTests' -class '*AddTenantMemberFlowTests' -class '*ChangeTenantMemberRoleFlowTests' -class '*RemoveTenantMemberFlowTests' -class '*EditTenantMetadataFlowTests' -class '*TenantDetailSurfaceTests'` | Exit 1; 538 total, 0 errors, 18 failed, 0 skipped. Log: `debug-focused-04.log`. |
| debug-focused-05 | `dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -class '*TenantRetainedCommandFlowTests' -class '*CreateTenantFlowTests' -class '*AddTenantMemberFlowTests' -class '*ChangeTenantMemberRoleFlowTests' -class '*RemoveTenantMemberFlowTests' -class '*EditTenantMetadataFlowTests' -class '*TenantDetailSurfaceTests'` | Exit 1; 538 total, 0 errors, 6 failed, 0 skipped. Log: `debug-focused-05.log`. |
| debug-build-06 | `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -m:1 -nr:false` | Exit 1; CS8604: Possible null reference argument for parameter 'actual' in 'void ShouldBeStringTestExtensions.ShouldNotContain(string actual, string expected, Case caseSensitivity = Case.Insensitive, string? customMessage = null)'.. Log: `debug-build-06.log`. |
| debug-integration-build-01 | `dotnet build tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj -c Debug -p:UseHexalithProjectReferences=true -m:1 -nr:false` | Exit 1; CS0246: The type or namespace name 'DaprTheoryAttribute' could not be found (are you missing a using directive or an assembly reference?); CS0246: The type or namespace name 'DaprTheory' could not be found (are you missing a using directive or an assembly reference?). Log: `debug-integration-build-01.log`. |
| debug-focused-final | `dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -class '*TenantRetainedCommandFlowTests' -class '*TenantRetainedCommandAttemptTests' -class '*CreateTenantFlowTests' -class '*AddTenantMemberFlowTests' -class '*ChangeTenantMemberRoleFlowTests' -class '*RemoveTenantMemberFlowTests' -class '*EditTenantMetadataFlowTests' -class '*TenantDetailSurfaceTests' -class '*TenantCommandGatewayTests' -class '*GlobalAdministratorsPageTests' -class '*TenantCreateCommandSnapshotTests' -class '*TenantAddMemberCommandSnapshotTests' -class '*TenantChangeRoleCommandSnapshotTests' -class '*TenantRemoveMemberCommandSnapshotTests' -class '*TenantUpdateMetadataCommandSnapshotTests'` | Exit 1; 1150 total, 0 errors, 6 failed, 0 skipped. Log: `debug-focused-final.log`. |
| maintained-release-ui | `dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-build --no-restore --minimum-expected-tests 4381 --results-directory /tmp/tenants-b4-20261013/test-results/UI --report-xunit-trx --report-xunit-trx-filename UI.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` | Exit 2; 4464 total, 4462 passed, 2 failed, 0 skipped. Log: `maintained-release-ui.log`. |
| review-build-02 | `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -m:1 -nr:false` | Exit 1; CS1026: ) expected. Log: `review-build-02.log`. |
| review-build-03 | `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -m:1 -nr:false` | Exit 1; CS8602: Dereference of a possibly null reference.; xUnit1031: Test methods should not use blocking task operations, as they can cause deadlocks. Use an async test method and await instead. (https://xunit.net/xunit.analyzers/rules/xUnit1031). Log: `review-build-03.log`. |
| review-build-04 | `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -m:1 -nr:false` | Exit 1; CS7036: There is no argument given that corresponds to the required parameter 'targetGlobalAdministratorFriction' of 'TenantRemoveMemberCommandSnapshot.Previewed(RemoveUserFromTenant, TenantRole, int, bool, TenantDetail)'; CS1061: 'IRenderedComponent<ChangeTenantMemberRoleFlow>' does not contain a definition for 'ChangeFluentSelect' and no accessible extension method 'ChangeFluentSelect' accepting a first argument of type 'IRenderedComponent<ChangeTenantMemberRoleFlow>' could be found (are you missing a using directive or an assembly reference?); CS1061: 'IRenderedComponent<AddTenantMemberFlow>' does not contain a definition for 'ChangeFluentSelect' and no accessible extension method 'ChangeFluentSelect' accepting a first argument of type 'IRenderedComponent<AddTenantMemberFlow>' could be found (are you missing a using directive or an assembly reference?). Log: `review-build-04.log`. |
| review-build-06 | `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -m:1 -nr:false` | Exit 1; CS7036: There is no argument given that corresponds to the required parameter 'tenantId' of 'TenantDetailSnapshot.NotFound(string)'. Log: `review-build-06.log`. |
| review-focused-01 | `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore -p:UseHexalithProjectReferences=true --filter 'FullyQualifiedName~TenantRetainedCommandFlowTests\|FullyQualifiedName~RemoveTenantMemberFlowTests\|FullyQualifiedName~EditTenantMetadataFlowTests\|FullyQualifiedName~LandmarkReplacementRestores\|FullyQualifiedName~LandmarkChildReplacement\|FullyQualifiedName~PageReplacementRendersSibling\|FullyQualifiedName~Host_supplied_attempt_store\|FullyQualifiedName~AmbiguousTenantDeliveryKeepsTheOriginalUlidAndPayload\|FullyQualifiedName~TenantsWorkspaceTests'` | Exit 2; 298 total, 286 passed, 12 failed, 0 skipped. Log: `review-focused-01.log`. |
| review-build-08 | `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -m:1 -nr:false` | Exit 1; CS0103: The name 'RefuseDispatch' does not exist in the current context. Log: `review-build-08.log`. |
| review-focused-02 | `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore -p:UseHexalithProjectReferences=true --filter 'FullyQualifiedName~TenantRetainedCommandFlowTests\|FullyQualifiedName~RemoveTenantMemberFlowTests\|FullyQualifiedName~EditTenantMetadataFlowTests\|FullyQualifiedName~LandmarkReplacementRestores\|FullyQualifiedName~LandmarkChildReplacement\|FullyQualifiedName~PageReplacementRendersSibling\|FullyQualifiedName~Host_supplied_attempt_store\|FullyQualifiedName~AmbiguousTenantDeliveryKeepsTheOriginalUlidAndPayload\|FullyQualifiedName~TenantsWorkspaceTests'` | Exit 2; 302 total, 301 passed, 1 failed, 0 skipped. Log: `review-focused-02.log`. |
| review-focused-03 | `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore -p:UseHexalithProjectReferences=true --filter 'FullyQualifiedName~TenantRetainedCommandFlowTests\|FullyQualifiedName~RemoveTenantMemberFlowTests\|FullyQualifiedName~EditTenantMetadataFlowTests\|FullyQualifiedName~LandmarkReplacementRestores\|FullyQualifiedName~LandmarkChildReplacement\|FullyQualifiedName~PageReplacementRendersSibling\|FullyQualifiedName~Host_supplied_attempt_store\|FullyQualifiedName~AmbiguousTenantDeliveryKeepsTheOriginalUlidAndPayload\|FullyQualifiedName~TenantsWorkspaceTests'` | Exit 2; 302 total, 301 passed, 1 failed, 0 skipped. Log: `review-focused-03.log`. |

The telemetry lookup used the display name `tenants-ui`, while Aspire had generated hashed resource names, and reported `Resource 'tenants-ui' not found`. Its independent exit was not captured; it is not presented as successful telemetry evidence. AppHost start/describe/stop succeeded and the final integration lane provides post-change runtime evidence.

All three required reviews completed before per-finding triage. Verified patches corrected removal's local-only projection update, correlationless ambiguous recovery after nudges, incomplete removal recovery gates, changed denied routes during suspended admission, callback-only acquisition faults, same-kind landmark acquisition before parent approval, parent availability after ownerless terminal release and split DI stores. Verification gaps added page/landmark remount and retained ready-receipt assertions; persisted recovery assertions now verify original values. A targeted follow-up caught retained removal receipt and metadata read-fact presentation crossing caller provenance; the existing B2 read contract now applies to that new retained path. Claims that safe renderer detachment lost dispatched admission or that serialized exact-attempt delivery could overwrite a newer identity were refuted against the current callers. No everyday resource failure justified a new eviction policy. No new deferred record, unrelated closure or product/governance decision was added.

### Preservation and remaining release limits

Only the eight B4 ledger records, this evidence and the maintenance index changed documentation. The ledger still contains 643 unique IDs: **308 done / 335 open** (reconciled 104 done, 247 actionable, 15 blocked, 26 awaiting decision, nine accepted limitations and 38 uncertain). Eight removed rows leave **299 maintenance rows** covering the same remaining 335 open IDs. Original finding/legacy fields and pre-fix verification/action/date are preserved. B1–B3 evidence, B5–B7 blocker findings, governance questions, unrelated records, archive and sprint tracking remain unchanged. Protected-scope, whitespace, staging and the session preservation comparison pass.

No unresolved local B4 validation blocker remains. The existing bounded/version-consistent removal walk still withholds unavailable or indeterminate proof; command success and audit availability remain distinct. This work adds no timeout, expiry, authorization or recovery policy and does not close DW-387's broader absent-target removal recovery, DW-574's correction caller ownership, B5 support policy, B6 collision/replay, B7 live authority/viewport admission, or any unrelated deferred work. It preserves the existing circuit retention boundary; no cross-circuit recovery guarantee is invented.

Hosted critical-check/upstream qualification, current candidate performance/NFR10 on the approved dedicated 4-vCPU/8-GiB environment, production IdP/secret-provider/revocation and complete authenticated browser/human AT journeys remain the existing release qualifications. The local browser harness and runtime integration do not stand in for those decisions or environments. **Release remains not ready.**


## B5 remediation evidence 2026-10-13

**Disposition: B5 closed in the validated working tree; overall release remains not ready.** Reverified clean Tenants `8216f9927700e39d67b2235ed83a4dbcef03d35a` on `main` and its existing root dependency pins. Both core production fixes and initial regressions were already committed at this baseline: the recorded DW-315/DW-441 source descriptions were historical rather than current reproductions. This pass completes their regression/validation/closure evidence. It changes four test/fixture files, the B5 build record, exactly two ledger records, this readiness evidence and the maintenance index. No production mapping rewrite was necessary.

`TenantCommandGateway.SafeFailureReason` matches only complete case-sensitive `ConcurrencyConflict`, `BackpressureExceeded` and `DomainRejected` codes to fixed whole-string localization keys/fallbacks. Unknown backend details cannot become presentation copy; PublishFailed/TimedOut use fixed status copy. The non-default evidence regressions preserve verified identity, event counts, rejection code and valid committed sequence. Broader shared rejection/validation localization work (DW-234/DW-251/DW-316) remains open.

`TenantAuditSupportSafety` implements the settled D4/D5 typed UserId character allowlist, compatibility normalization before repeated percent decoding and confusable inspection. Regression inputs include dotted/slashed/tel-prefixed/leading-space phones, fullwidth `％40`, nested encodings, U+2503/U+275A/U+01C0/U+23D0/U+204F/U+061B, secret lookalikes and unlisted symbols. EN/FR mapped and direct grid/receipt tests observe DOM, JSON, live regions and real clipboard JS calls, including deliberate disclosure controls for each original/decoded/normalized representation. Refusal, partial-state and copied feedback use shipped resources. Approved literal Unicode/case/spacing/slashes/dots remain exact; the existing non-user tenant/configuration/reference contract is preserved. Processing clears previous failure copy intentionally; it does not announce a backend support detail.

### Exact commands and results

Commands ran from the repository root. Fresh logs, exact command manifests, diffs, initial failures, TRX and coverage are under `/tmp/tenants-b5-20261013-current/`; older `/tmp/tenants-b5-20261013/` artifacts are not reused as current validation. The commands and results below remain usable if temporary files disappear. Tests ran per project, with focused xUnit single-dash assembly filters; no solution-level test, gate weakening, skipped-case substitution or dependency update ran.

| Check | Exact command | Result |
| --- | --- | --- |
| build-skill | `uv run --no-cache "/home/administrator/projects/hexalith/tenants/_bmad/scripts/render_skill.py" --project-root "/home/administrator/projects/hexalith/tenants" --skill "/home/administrator/projects/hexalith/tenants/.agents/skills/bmad-build"` | Exit 0; rendered workflow read and followed; invoked once. |
| apphost-discovery | `aspire ps --format Json` | Exit 0; no running AppHost before this session. |
| apphost-start | `aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json --non-interactive` | Exit 0; background start succeeded. Initial describe found security Running/Unhealthy and dependent services Waiting; this is not a healthy authenticated topology qualification. Log: `apphost-start.log`. |
| focused-source-build | `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -m:1 -nr:false` | Four initial builds exit 0, zero warnings/errors. Logs: focused-ui-build{,-rerun,-final,-verified}.log. |
| focused-boundary | `dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -class Hexalith.Tenants.UI.Tests.Components.SupportSafetyBoundaryTests` | Initial exits 1 with 186, 36 and two failures out of 195; corrected runs exit 0 with 195/195. Logs: focused-support-boundary{,-rerun,-final,-verified}.log. |
| focused-audit-safety | `dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -class Hexalith.Tenants.UI.Tests.State.TenantAuditSupportSafetyTests` | Exit 0; 54 passed, zero errors/failures/skips. Log: focused-audit-safety.log. |
| focused-safe-message | `dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -class Hexalith.Tenants.UI.Tests.State.TenantCommandSafeMessageTests` | Exit 0; six passed, zero errors/failures/skips. Log: focused-command-safe-message.log. |
| focused-gateway | `dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -class Hexalith.Tenants.UI.Tests.Services.Gateways.TenantCommandGatewayTests` | Exit 0; 253 passed, zero errors/failures/skips. Log: focused-command-gateway.log. |
| patched-boundary | `dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -class Hexalith.Tenants.UI.Tests.Components.SupportSafetyBoundaryTests` | Exit 0; 227 passed, zero errors/failures/skips. Log: patch-support-boundary.log. |
| patched-gateway | `dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -class Hexalith.Tenants.UI.Tests.Services.Gateways.TenantCommandGatewayTests` | Exit 0; 257 passed, zero errors/failures/skips. Log: patch-command-gateway.log. |
| patched-parity | `dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -class Hexalith.Tenants.UI.Tests.LocalizerDoubleParityTests` | Exit 0; three passed through unchanged maintained gate. Log: patch-localizer-parity.log. |
| patch-source-build | `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -m:1 -nr:false` | Initial exit 1, two CA1062 errors; rerun exit 0, zero warnings/errors. Logs: patch-ui-build{,-rerun}.log. |
| apphost-describe | `aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json` | Exit 0; Initial resources inspected; security Running/Unhealthy and dependent services Waiting. Log: `apphost-describe.log`. |
| apphost-stop | `aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --non-interactive` | Exit 0; This session AppHost stopped successfully. Log: `apphost-stop.log`. |
| python-guards | `python3 tests/scripts/test_validate_story_gitlinks.py` | Exit 0; 23 guard regressions passed. Log: `python-guards.log`. |
| maintained-restore | `dotnet restore Hexalith.Tenants.slnx -m:1 -nr:false` | Exit 0; Restore succeeded without version changes. Log: `maintained-restore.log`. |
| maintained-release-build | `dotnet build Hexalith.Tenants.slnx --no-restore --configuration Release -warnaserror -m:1 -nr:false` | Exit 0; Zero warnings/errors. Log: `maintained-release-build.log`. |
| release-contracts | `dotnet test --project tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Release --no-build --no-restore --minimum-expected-tests 154 --results-directory /tmp/tenants-b5-20261013-current/test-results/Contracts --report-xunit-trx --report-xunit-trx-filename Contracts.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` | Exit 0; 154 total, 154 passed, 0 failed, 0 skipped. Log: `release-contracts.log`. |
| release-client | `dotnet test --project tests/Hexalith.Tenants.Client.Tests/Hexalith.Tenants.Client.Tests.csproj --configuration Release --no-build --no-restore --minimum-expected-tests 50 --results-directory /tmp/tenants-b5-20261013-current/test-results/Client --report-xunit-trx --report-xunit-trx-filename Client.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` | Exit 0; 50 total, 50 passed, 0 failed, 0 skipped. Log: `release-client.log`. |
| release-testing | `dotnet test --project tests/Hexalith.Tenants.Testing.Tests/Hexalith.Tenants.Testing.Tests.csproj --configuration Release --no-build --no-restore --minimum-expected-tests 182 --results-directory /tmp/tenants-b5-20261013-current/test-results/Testing --report-xunit-trx --report-xunit-trx-filename Testing.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` | Exit 0; 182 total, 182 passed, 0 failed, 0 skipped. Log: `release-testing.log`. |
| release-sample | `dotnet test --project samples/Hexalith.Tenants.Sample.Tests/Hexalith.Tenants.Sample.Tests.csproj --configuration Release --no-build --no-restore --minimum-expected-tests 40 --results-directory /tmp/tenants-b5-20261013-current/test-results/Sample --report-xunit-trx --report-xunit-trx-filename Sample.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` | Exit 0; 40 total, 40 passed, 0 failed, 0 skipped. Log: `release-sample.log`. |
| release-apphost | `dotnet test --project tests/Hexalith.Tenants.AppHost.Tests/Hexalith.Tenants.AppHost.Tests.csproj --configuration Release --no-build --no-restore --minimum-expected-tests 6 --results-directory /tmp/tenants-b5-20261013-current/test-results/AppHost --report-xunit-trx --report-xunit-trx-filename AppHost.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` | Exit 0; 6 total, 6 passed, 0 failed, 0 skipped. Log: `release-apphost.log`. |
| release-server | `dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Release --no-build --no-restore --minimum-expected-tests 884 --results-directory /tmp/tenants-b5-20261013-current/test-results/Server --report-xunit-trx --report-xunit-trx-filename Server.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` | Exit 0; 884 total, 884 passed, 0 failed, 0 skipped. Log: `release-server.log`. |
| release-ui | `dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-build --no-restore --minimum-expected-tests 4506 --results-directory /tmp/tenants-b5-20261013-current/test-results/UI --report-xunit-trx --report-xunit-trx-filename UI.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` | Exit 2; 4729 total, 4728 passed, 1 failed, 0 skipped. Log: `release-ui.log`. |
| browser | `env TENANTS_BROWSER_BUILD_CONFIGURATION=Release tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh` | Exit 0; Chrome 154.0.8037.57; EN/FR desktop/narrow/forced-colors/focus/receipt checks passed; all deliberate mutations rejected. Log: `browser.log`. |
| release-integration | `dotnet test --project tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Release --no-build --no-restore --filter-not-trait Category=Performance --minimum-expected-tests 189 --results-directory /tmp/tenants-b5-20261013-current/integration-release --report-xunit-trx --report-xunit-trx-filename non-performance.trx` | Exit 0; 189 total, 189 passed, 0 failed, 0 skipped. Log: `release-integration.log`. |
| coverage | `python3 scripts/validate-coverage.py --coverage-root /tmp/tenants-b5-20261013-current/test-results --minimum-line-coverage 80 --required-branch-coverage 100 --line-scope src/Hexalith.Tenants.Contracts/ --line-scope src/Hexalith.Tenants.Client/ --line-scope src/Hexalith.Tenants.Server/ --line-scope src/Hexalith.Tenants.Testing/ --isolation-auth-target src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs --isolation-auth-target src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsAggregate.cs --isolation-auth-target src/Hexalith.Tenants.Server/Validators/ChangeUserRoleValidator.cs --summary-file /tmp/tenants-b5-20261013-current/coverage-summary.md` | Exit 0; 95.70% scoped package line coverage (1180/1233); isolation/auth branches 100% (118/118). Log: `coverage.log`. |
| final-release-build | `dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false` | Exit 0; Zero warnings/errors after patches. Log: `final-release-build.log`. |
| local-pack | `python3 scripts/pack-release-packages.py /tmp/tenants-b5-20261013-current/packages 5.8.0-b5-validation` | Exit 0; Five local unpublished validation packages. Log: `local-pack.log`. |
| final-release-ui | `dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-build --no-restore --minimum-expected-tests 4765 --results-directory /tmp/tenants-b5-20261013-current/test-results/UI --report-xunit-trx --report-xunit-trx-filename UI.trx --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` | Exit 0; 4765 total, 4765 passed, 0 failed, 0 skipped. Log: `final-release-ui.log`. |
| package-validation | `python3 scripts/validate-nuget-packages.py /tmp/tenants-b5-20261013-current/packages` | Exit 0; Five package/dependency inventories validated. Log: `package-validation.log`. |
| package-consumers | `python3 scripts/validate-consumer-package-references.py /tmp/tenants-b5-20261013-current/packages --work-directory /tmp/tenants-b5-20261013-current/consumers` | Exit 0; Package-only Contracts+Client and Testing consumers built; one infrastructure-free case passed, zero failed/skipped; no ProjectReference fallback. Log: `package-consumers.log`. |
| staging-preserved | `git diff --cached --exit-code HEAD` | Exit 0; No staged changes. Log: `staging-preserved.log`. |
| protected-scope | `git diff --exit-code HEAD -- references _bmad-output/implementation-artifacts/sprint-status.yaml _bmad-output/implementation-artifacts/deferred-work-archive.md Directory.Packages.props Directory.Build.props Directory.Build.targets global.json .editorconfig .gitattributes .gitmodules` | Exit 0; No protected dependency/gitlink/build/format, archive or sprint changes. Log: `protected-scope.log`. |
| code-whitespace | `git -c core.safecrlf=false diff --check` | Exit 0; No whitespace errors. Log: `code-whitespace.log`. |
| coverage-final | `python3 scripts/validate-coverage.py --coverage-root /tmp/tenants-b5-20261013-current/test-results --minimum-line-coverage 80 --required-branch-coverage 100 --line-scope src/Hexalith.Tenants.Contracts/ --line-scope src/Hexalith.Tenants.Client/ --line-scope src/Hexalith.Tenants.Server/ --line-scope src/Hexalith.Tenants.Testing/ --isolation-auth-target src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs --isolation-auth-target src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsAggregate.cs --isolation-auth-target src/Hexalith.Tenants.Server/Validators/ChangeUserRoleValidator.cs --summary-file /tmp/tenants-b5-20261013-current/coverage-summary-final.md` | Exit 0; Final reports: 95.70% scoped package line coverage (1180/1233); isolation/auth branches 100% (118/118). Log: `coverage-final.log`. |
| evidence-preservation | `python3 /tmp/tenants-b5-20261013-current/verify-preservation.py` | Exit 0; 643 IDs; exactly DW-315/DW-441 changed; 310 done / 333 open; 297 rows cover all open IDs; historical fields, B1–B4 evidence, B6/B7, decisions, protected files and staging preserved. Log: `evidence-preservation.log`. |
| final-document-whitespace | `git -c core.safecrlf=false diff --check` | Exit 0; no whitespace errors after closure documentation. Log: `final-document-whitespace.log`. |
| final-evidence-preservation | `python3 /tmp/tenants-b5-20261013-current/verify-preservation.py` | Exit 0; exactly two B5 closures, counts/index/historical evidence/protected paths and staging verified after documentation. Log: `final-evidence-preservation.log`. |

### Initial failures, review and corrections

Every nonzero iteration remains recorded. The first boundary run failed 186/195 because services were resolved before fixture registration and assertions expected detailed metadata announcements and an empty mapped target. Fixtures were ordered correctly; assertions now check the shipped metadata state announcement and the approved `tenant.alpha` display fallback while still rejecting unsafe user values. The next 36 failures reflected a mistaken `IsPending=true` expectation for a present Processing response; the flag describes status-store propagation lag, and the established false value is preserved. The next two failures were case-insensitive scans matching the legitimate fixed DomainRejected resource key to lowercase backend input. Review restored case-insensitive scanning of the remaining evidence, excluding only message-key fields that each test explicitly asserts. The patch build's two CA1062 diagnostics were fixed with null guards.

The first maintained UI run passed 4,728/4,729, with `LocalizerDoubleParityTests.Every_localizer_double_returns_the_exact_shipped_resource_value_for_every_stubbed_key` failing because the existing `CultureBoundTenantsLocalizer` fixture lacked a parameterless constructor. The fixture now provides a neutral constructor using the real resource factory; injected EN/FR behavior remains unchanged, and neither parity nor support-evidence gate is weakened. The final maintained UI run passes 4,765/4,765.

All three independent review lenses completed before triage. Edge-case and verification-gap reviewers returned no findings. The blind reviewer filed five suggestions: the requested Processing-detail display was refuted by existing intentional message clearing; the other four strengthened decoded/normalized observation, case-insensitive JSON checks, non-default proof evidence and symbol consumer coverage. Each finding and disposition is recorded in `spec-b5-support-safe-failure-and-audit-identifiers.md`. The patched diff was inspected and targeted checks rerun. No unresolved B5 review finding or new policy decision remains.

### Preservation and remaining release limits

Maintained Release evidence totals **6,270 passed, zero failed/skipped**: Contracts 154, Client 50, Testing 182, Sample 40, AppHost six, Server 884, UI 4,765 and non-performance integration 189. Final focused source evidence covers 227 boundary, 54 audit-safety, six key-transition, 257 gateway and three parity cases. Browser, package/consumer, coverage and Python guards pass. These local checks do not qualify upstream source pins, hosted critical workflows, dedicated candidate NFR10/performance, production IdP/secret-provider/revocation, every authenticated clipboard/denial journey or human assistive technology. The initial AppHost resource snapshot remains an unhealthy/waiting topology observation, not a passed authenticated journey; this session AppHost was stopped.

Only DW-315/DW-441 change ledger disposition: **643 IDs, 310 done / 333 open**; reconciled 106 done, 245 actionable, 15 blocked, 26 awaiting decision, nine accepted limitations and 38 uncertain. **297 maintenance rows** index exactly those 333 open IDs. Their original findings, lossless legacy text and pre-B5 verification/actions remain. B1–B4 evidence is unchanged; B6/B7, unresolved decisions, unrelated ledger records, archive, sprint, dependencies and staging are preserved. The current baseline archive contains 308 IDs and remains byte-for-byte unchanged; historical earlier archive counts remain historical, and no B5 archive move ran.

No local B5 validation blocker remains. This closure preserves typed UserId policy and existing literal non-user contracts; it does not claim universal Unicode confusable classification or all broader localization work. **Release remains not ready:** B6/DW-626/DW-628 audit-stream collision/replay and B7/DW-195/DW-206/DW-235/DW-264 live authority/viewport admission stay open, alongside the existing qualification and governance holds. **Next deferred-work bundle: B6, DW-626/DW-628**, requiring stream compatibility and persisted replay end-state proof before rollout. No dependency update, staging, commit, push or deployment ran.


## B6 remediation evidence 2026-10-13

**Disposition: DW-626/DW-628 closed after source, persisted end-state, independent review and maintained validation proof. Overall release remains not ready.** Full inherited B5 baseline is `57b881beebfbcdf0fe6d7bc4c23a0fc70903136d`; entry branch `main`, reported clean. Its B5 code/fixtures/evidence and already recorded gitlinks are inherited, excluded from B6 attribution. The initial hashes, status, full HEAD and B5 commit stat were captured before edits. Historical B1–B5 evidence below remains byte-identical to the entry copy.

Reverified both source findings: global replay replaced `audit:system`, while the tenant projector merged `audit:{AggregateId}` and `CreateTenant` accepted literal `system`. Tenant `system` now writes `audit-tenants:system`; global audit writes `audit-global-administrators:singleton`; ordinary tenant keys remain compatible. Both separated writers use EventStore guarded merge policy, preserving stored evidence through duplicate/full/shorter replay. Exact known ownership is validated before incoming writes. Legacy `audit:system` is never rewritten.

The compatible reader unions recognized immutable legacy subsets with the selected dedicated records, rejects unknown/inconsistent ownership and conflicting evidence, and preserves original IDs and rows. Default system reads remain combined when references are unambiguous, with opaque composite provenance and validators. Repeated EventIds across owners make the existing receipt selection ambiguous, so the entire combined source fails before filters/pagination; explicit owner reads retain access to each owner's evidence. Optional `Stream` binds REST transport, signed cursors, cache/snapshot scope and proof callers. Global proof/original-receipt recovery explicitly selects the global stream. Only validated canonical global history can provide ordered global provenance; legacy sequences, malformed/conflicting histories or nonpositive/reused persisted row sequences cannot. Existing legacy rows missing additive provenance can agree with canonical core evidence without being mutated, and redundant legacy timestamps do not keep a canonical replay stale forever.

Global replay folds by sequence, accepts valid recognized rejection contracts in simple/full-name forms, and compares complete duplicate envelope evidence. Rejections do not become success audit rows, but their valid sequences contribute to canonical ordering. Typed dispatcher logging now carries guarded-policy retry diagnostics. New audit ownership policy is included in both CI and local 100% branch gates. No threshold or safety assertion was weakened.

### Validation and independent review

[Exact commands, results, every initial failed validation and provenance limits](b6-validation-evidence.md) are retained as a standalone B6 record; raw logs, command manifest, diffs, TRX and coverage are under `/tmp/tenants-b6-20261013/`. The evidence remains understandable if temporary artifacts disappear. Builds use `.slnx`; tests run per project; focused source tests use built xUnit assemblies and single-dash filters. No solution-level test ran.

| Maintained lane | Final result |
| --- | --- |
| Restore and Release solution build with warnings as errors | Pass; zero warnings/errors |
| Contracts / Client / Testing / Sample / AppHost | 154 / 50 / 182 / 40 / 6 passed |
| Server | 926 passed |
| UI | 4,785 passed |
| Non-performance integration | 314 passed |
| Total maintained tests | **6,457 passed; zero failures/skips** |
| Browser | EN/FR, desktop/narrow/forced-colors/focus/receipt harness and deliberate negative controls pass |
| Scoped package line coverage | **94.70% (1233/1302)** |
| Isolation/auth branches, including new ownership policy | **100% (150/150)** |
| Local unpublished package inventory and package-only consumers | Five packages validated; consumers build and one infrastructure-free case passes |
| Python story-guard regressions | 23 passed |

Focused revised source evidence passes **119 serialized persisted audit cases**, **one actual SDK HTTP500→REST→UI failure-boundary case**, **three gateway selector/proof cases**, and **74 server policy/projection/diagnostic cases**. Earlier 282 server and 360 UI scope/page/proof cases pass as recorded; final maintained suites cover all current cases. The new persistence seam is the platform's JSON round-trip store, not a mock call-count substitute or production replay. Real local Dapr/Aspire integration also passes in the maintained 314-case lane. The startup baseline showed all 23 resources Running/Healthy; this session AppHost was stopped.

All three independent review lenses completed before triage. Every finding has a verdict/evidence in [the B6 spec](spec-b6-audit-stream-separation.md); substantive findings were fixed, and edge/verification rechecks reported no remaining B6 defects or gaps. The requested new unavailable taxonomy was rejected as unnecessary platform work: the final refusal uses the existing SDK `ActorResponseMismatch` sentinel, sanitized HTTP500 and UI `Error`, returning no rows, validators or authoritative metadata. That full boundary is tested. A final independent check approved the two Bash fixture line-ending corrections, with endpoint/exit/output/fail-closed assertions preserved.

Initial failures are retained: CA1062 null guards; CS9107 constructor capture; nullable Shouldly and missing namespace compilation errors; initial global mock/ordinary persisted-duplicate expectations; two legacy selected-source expectations; nine page fixtures and 17 global-panel fixtures missing Stream; the public Stream documentation gate; initial ownership string-switch branch shortfall; and two embedded Bash tests after required CRLF normalization. The latter now normalize script text to LF at execution. Final relevant checks all pass; no fixture expectation was relaxed to hide a production failure.

### External Git movement and remaining limits

During this run, an external workspace action committed the intermediate B6 files as `a8c18a6e435280eb56b866cc4693683716e81ee4` at 20:50:54 +0200, after external Builds/Memories pulls at 20:50:40–41. The baseline remains the original B5 commit; B6 review/closure compares all B6 files against it, with the external gitlinks captured separately. This run did not perform those commits/pulls, nor stage, push, deploy or change dependency declarations. Their preserved final pointers are Builds `e3be1d4e997f5f81c7d67c87d5857f8be3a8c69e` and Memories `42b4dc4d58731d4e9ee777285ba1f65f26e4a0c0`; entry pointers were `01dfb167754ca408c4e00645bb4d1a5b03f16e7e` and `7034b839791fd8dbe0c6aff50b907fbecd90701a`.

**The required story-gitlink attribution check remains failed:** `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-b6-audit-stream-separation.md` exits 1 with those two UNDECLARED movements from the original baseline. No dependency reset, false B6 ownership declaration or validator weakening was made. Passing source/test checks do not resolve this mixed commit's release provenance or qualify external dependency moves. The failure and exact command remain visible in the validation record.

Only DW-626/DW-628 change ledger disposition. **643 IDs: 312 done / 331 open; 295 maintenance rows** cover all remaining open IDs. Reconciled counts are 108 done, 243 actionable, 15 blocked, 26 awaiting decision, nine accepted limitations and 38 uncertain. Original findings/legacy text and pre-B6 verification/actions remain in both records. Historical B1–B5 evidence, B7, unresolved decisions, unrelated ledger/index rows, archive and sprint tracking are preserved; no archive move ran.

[Safe rollout and replay prerequisites](../../docs/audit-stream-rollout.md) require old writers stopped/drained, immutable evidence backups, complete separate owner histories, ownership reconciliation where ambiguous, and serialized replay routes before any separately authorized replay. No production replay or migration ran. This code cannot restore evidence already lost under the old collision. DW-627 distributed singleton authority-rebuild serialization, production provider/revocation proof, candidate NFR10/performance qualification, hosted critical flows, full authenticated browser/clipboard/denial and human assistive-technology evidence remain unresolved. Combined-source and owner-specific completeness are distinct; no cross-aggregate ordering or unconditional replay safety is claimed.

**Next deferred-work bundle: B7, DW-195/DW-206/DW-235/DW-264.** B7 and every existing decision remain open. Overall release remains not ready, including the external story-gitlink attribution blocker.

## B7 remediation evidence 2026-10-14

**B7 is closed; release remains not ready.** DW-195/DW-206/DW-235/DW-264 are reverified and implemented over clean `main` entry `7ad54431996f9f1f0d277005eb2a73b67b55fea6`. [Entry state](b7-entry-state.json) records empty entry/index changes and all nine full dependency pointers, distinguishing inherited B1–B6/external work from new B7 changes. [Spec and individual review triage](spec-b7-live-admission.md), [exact validation/initial failures/limits](b7-validation-evidence.md) and [durable commands/results](b7-command-results.json) provide closure proof.

Metadata binds current literal-tenant Owner/Contributor or GA authority; every membership action and launcher binds Owner or GA; create binds resolved current-caller GA. Defaults, denied/indeterminate states, stale responses and direct invocation fail closed before command dispatch. Removal requires actual measured Safe width through the existing observer. Fresh submission/post-lease reads and current caller/tenant/target/evidence guards prevent revocation/resize races; exact retained attempts, terminal proof and audit-return state survive. Authoritative-empty first-tenant freshness and proven GA first-member paths remain supported; backend bootstrap history semantics are untouched.

Three independent review layers and correction re-checks are complete; all 21 findings are individually triaged and no new work deferred. Parent/real-BFF/direct/effect/retry regressions demonstrate zero refused dispatch and successful current authority. Final Debug UI 4,939/4,939, focused 582/582, source handler/gateway 4/4 and maintained Release 6,611 cases pass with zero errors/failures/skips. Release build has zero warnings/errors; EN/FR Chromium old/new mutation controls, five local package/consumer checks and unchanged 94.70% line /100% required-branch gates pass. Initial compile, fixture, race, CSS/paint and temporary browser-input failures remain recorded.

Only four entries close; all 643 IDs remain (316 done /327 open), indexed by 291 rows. Reconciled 112 done /239 actionable /15 blocked /26 awaiting-decision /9 accepted-limitation /38 uncertain. Original findings/decisions/pre-B7 verification remain; historical B1–B6 evidence, unrelated records, archive and sprint are preserved.

Standard B7 gitlink validation passes with no pointer change. Historical B6 validation still fails on four inherited Builds/EventStore/Memories/Platform movements; no reset, undeclared ownership claim or guard bypass occurred. Full release still needs provenance resolution, the seven decision holds and applicable hosted TLS/browser/upstream, current-candidate dedicated NFR10/performance, production authority/provider/revocation, authenticated browser and human-AT qualification. The browser harness proves rendered fixtures and shipped JS; .NET effect/command admission is proven separately, not asserted as an authenticated live circuit.

Next selected ledger bundle is EventStore command-recovery integrity DW-499/DW-502, rechecked against its current pointer. It requires the owning durable status-repair and invalid-range quarantine/recovery contracts; DW-517 remains separate. These records and unresolved decisions are preserved. No dependency update, staging, commit, push, deployment, production replay or migration ran. B7 closure is not a release approval.

## B8 remediation evidence 2026-10-14

**DW-499/DW-502 are closed; release remains not ready.** The [owning contract](../../references/Hexalith.EventStore/docs/architecture/command-recovery-integrity.md), [spec and individual review triage](spec-b8-eventstore-command-recovery.md), [full entry/external movement records](b8-entry-state.json), [validation and limits](b8-validation-evidence.md) and [exact commands/initial failures/results](b8-command-results.json) support bounded local closure. Both findings were reverified at EventStore `173ad7f4e56a72f44621c7d3d7be86a5ec19dc03`; external EventStore `34c97acf9812c30ff610398baf3e00c9c9ed6c19` and Tenants `d6c82d667d3448f2111c15c0c0e8a2bcba95e399`/`5792099d432df44eb84aecee4b7b629b53f60957` remain preserved. Initial source ownership is distinguished from the external commits recording it. No assistant dependency/gitlink update, staging, commit, push, deployment, production replay or migration ran.

Terminal intent, exact original status/range and discovery ownership are saved atomically with terminal idempotency and successful cleanup. Failed writes/crashes leave registered-reminder/activation/exact-duplicate recovery; acknowledgement cleanup follows existing ambiguity inspection. Invalid persisted command/envelope identity, scope, sequence, count, causation or duplicate event identity refuses before publication and retains evidence. Verified ordinary and legacy rejection ranges resume; exact restoration is separately authorized through existing recovery seams. Other command/aggregate evidence and established advisory non-authority/idempotency behavior are preserved. Independent reviews and focused final rechecks resolved the corrections without inventing new policy guarantees.

Final Server project: 4,664 passed/0failed/25 unchanged skipped ATDD placeholders (4,689 total). Contracts2,333 plus two supplied-inventory package cases pass; Client1,497, Testing154, QueryRouting14, PayloadProtection635, Sample186 and Testing.Integration48 pass. Fourteen manifest packages and13 library/one tool consumers pass; final Server package/consumer rechecks use corrected source. Tenants maintained6,297 tests and four package integration cases pass; Debug source compatibility passes4 real-handler and308 UI cases. Existing coverage80/100 gates pass94.39% line and100% required branches, with no scope/exclusion/threshold/floor change. Intermediate faults and the original unobserved full-run completion remain explicit in evidence.

Attempted DomainService full lane remains qualified:870/872 pass, nested-submodule boundary case fails, and a separate unchanged timeout passes focused rerun. AppHost134/149 pass;15 fail through the prohibited/unavailable nested Tenants layout. No nested dependency initialization or replacement of their test subject occurred. Isolated Aspire startup never established running target resources, so topology/provider readiness remains unproven. These results are blockers, not passing lanes. Historical B6 gitlink guard initially failed on four inherited movements and now fails on five; B8 initially passed before external commits and now fails on five external baseline-to-current movements. No pointer reset, attribution rewrite or validator bypass resolves that provenance.

Only two records change disposition: all643 IDs remain (318 done/325 open), indexed by289 rows. Reconciled counts:114 done/237 actionable/15 blocked/26 awaiting-decision/9 accepted-limitation/38 uncertain. Original findings, legacy detail and pre-B8 actions/verification remain; every B1–B7 section, unrelated record, unresolved decision, archive and sprint record is preserved.

Remaining release holds include the existing seven decision records DW-540/DW-562/DW-584/DW-593/DW-599/DW-617/DW-625, provenance, DW-517 secret-scan qualification, applicable hosted TLS/browser/upstream, current-candidate dedicated NFR10/performance, production provider/authority/revocation, authenticated clipboard/denial/browser and human-AT evidence. In-memory tests do not prove multi-host/provider atomicity, cryptographic at-rest or second-writer guarantees, flat-index byte bounds, large-range capacity or recovery deadlines during scheduler outage. Missing legacy evidence can require exact trusted restore; automatic production rewriting/replay is not provided.

**Next selected ledger bundle: DW-517**, the separate EventStore secret-scan qualification item. B8 closure is not release approval.


## DW-517 repair and qualification evidence — 2026-10-14

**DW-517 remains open; overall release is not ready.** The owning evidence
reader's broad pre-content exclusions are removed over EventStore
`24d719292b06d8a9d09771d837f8dd4d8d9b8997`. The exact sealed capture, SSH identity,
binary/generated handling and existing approvals are preserved. [Owning contract](../../references/Hexalith.EventStore/docs/architecture/evidence-secret-scanning.md),
[validation and policy alternatives](dw-517-validation-evidence.md),
[exact commands/failures/results](dw-517-command-results.json),
[entry/external observations](dw-517-entry-state.json),
[independent review triage](spec-dw-517-evidence-content-scanning.md) and
[negative controls](dw-517-mutation-evidence.json) record the bounded repair.

Debug source and Release package builds pass with zero warnings/errors;
290 real-ingestion cases pass in each. Independent review's five findings are
patched and rechecked. Exact-source temporary control passes290/290; deliberately
bypassing ingestion/scanning/retirement-content/approval/seal produces
256/138/2/3/1 failures. Final focused security is491 passed/one failed/zero skipped
(492 total). Full owning Server is4,953 passed/one failed/25 existing DW1 ATDD
skips (4,979 total),exit2. Both fail only on the restored tracked-content guard's
nine preserved historical findings in three files. No source-expression/synthetic
recognition policy or retirement approval covers those files yet. No evidence
rewrite, new registry exemption or re-pin was used to pass validation.

Pinned Gitleaks8.21.2 reports1,561 findings in both entry/current tracked-tree
exports with identical path/rule/redacted-match multisets; no new findings,
but both raw checks remain red. Final changed-file check reports the unchanged
curl scanner fixture. Historical config comments reference a secret-scan job,
but current inspected EventStore/Builds workflow/composite YAML has no executing
Gitleaks reference. Existing CI wiring is preserved; no hosted execution of this
uncommitted source is claimed. Aspire startup times out without target resources.

Original entry Tenants `23aeb38f9784cd67b3c2fa66fe1c007799df6826` and external
`51f404513f2657e98f949f58f0df43b99b68a7d0` remain distinguished. All recorded and
checked-out dependency pointers, B1–B8 work and1,371 historical artifact hashes
are preserved. DW-517's own gitlink check passes without pointer movement;
historical B6/B8 checks still fail on five inherited movements. No assistant
Git/dependency mutation or production operation ran. B8 nested-layout/provider
limits and the seven existing authority/recovery/release-governance decision
holds remain, alongside applicable hosted, NFR10/performance, production and
human/authenticated browser qualification requirements. Passing new regressions
does not establish release readiness or govern historical-content exceptions.

Only DW-517 evidence/action fields change; no record closes. Counts recompute
unchanged:643 IDs,318 done/325 open,289 exact-covering maintenance rows;
114 reconciled done,237 actionable,15 blocked,26 awaiting-decision,nine accepted
limitations,38 uncertain. Historical B1–B8 evidence, findings, archive, sprint,
unrelated records and decisions stay in place. **Next independent implementation
bundle:DW-105/DW-108**, shared DOM-safe identity encoding, selected from the current
ledger. DW-517 remains P1 pending its owning contract and passing qualification;
that next selection is not a waiver. The user's request to compare contract
options grants no approval to alter recognition or retire evidence.


Final external movement: EventStore merge `241652627a52e47c189b2b052db407cec172f813`
arrived after validation, preserving parent `24d719292b06d8a9d09771d837f8dd4d8d9b8997`.
It changes owning sprint tracking and recorded Builds/FrontComposer/Memories/
Platform/Tenants pointers; nested dependencies remain uninitialized. This is
external work, not an assistant DW-517 mutation. Original entry and all full
movement identifiers remain in `dw-517-entry-state.json`. Final security source
hashes exactly match tested/mutated bytes; the external diff changes no owning
`src/` or `tests/` files, and effective initialized build dependencies are
unchanged. Prior full/focused results qualify that tested source, without
qualifying external provenance. DW-517's gitlink check initially passed and now
exits1 on the external EventStore checkout movement. No reset, new baseline,
false ownership declaration or attribution bypass was used. Historical B6/B8
failures remain. Tenants B1–B8 artifacts, archive/sprint and1,371 historical
artifact hashes are preserved; the external owning sprint edit is retained.


## Final DW-517 closure evidence — 2026-10-14

**DW-517 is closed in the validated working tree. Release remains not ready.**
The user's `apply recommended` resolved the owning decision: every textual
evidence path is ingested, with compact semantic recognition of bounded runtime
expressions and exact inert probes carrying rejection/redaction proof. Unknown
proof annotations, broken boundaries, duplicates/decorators/shadows, changed
markers, literal operands and credential-bearing URIs receive ordinary scanning.
No new path exclusion, retirement, hash re-pin or historical rewrite was created.
The original exact SSH identity, seal, binary/UI handling and both verified
content-bound Story5.3 approvals remain intact.

Final Debug source and Release package builds pass with zero warnings/errors.
Real owning ingestion passes576/576 in each configuration, with zero skips.
Final security is778/778 passed, zero failures/skips (`dw517-closure-security`).
Final unfiltered owning Server is5240 passed/zero failed/25 unchanged DW1 ATDD
skips,5265 total,exit0 (`dw517-closure-server`); all25 skip identities match the
original full-project result. The two changed maintained Python unit methods pass
with the same invalid-packet assertions and runtime-generated fixture values.
[Exact argv, cwd, outputs and all intermediate failures](dw-517-command-results.json)
and [full-project counters/skip identities](dw-517-full-server-summary.json) are
durable. The final commands use the unchanged build/warnings/gates and existing
package versions; no nested submodule was initialized.

The final exact-source control passes576/576. Eleven deliberately weakened
variants all compile and fail assertions: ingestion318, scanning337, retirement
content2, approval3, seal1, interpolation boundary12, probe verification10,
adjacent-tail boundary8, owning list closure1, here-document ownership4 and
disabled recognition92 failures. Final source/variant digests, setup scripts,
exact command indices and earlier mutation phases remain in
[mutation evidence](dw-517-mutation-evidence.json). This also closes the
independently demonstrated boundary mutation that previously survived all244
semantic cases; its initial survival is retained rather than hidden.

Independent Blind/Edge/VerificationGap review and subsequent correction rechecks
resolve every29 individually triaged finding. Final narrow reviewer runs pass
22/10/87 cases; exact reflection shows the quoted-semicolon override is detected
and the unchanged clean probe still qualifies. No finding is deferred, no DW517
policy decision remains open, and final source hashes match reviewed/built/mutated
bytes. The [spec and complete triage](spec-dw-517-evidence-content-scanning.md)
retain every report and correction.

Initial/intermediate failures remain visible: randomized NuGet negative-fixture
failures,290-case repair exposing nine historical findings, initial semantic
53/8/1 failures, review-round11 context/fallback failures, and the intermediate
full-project randomized `[8.14.0]` negative-fixture failure. That unchanged
ten-case method passes its focused rerun; final security/full-project runs also
pass. The random failing value was not retained, so no deterministic root-cause
claim is made; fixture instability remains a qualification limitation. Initial
closure-count validation also failed before any ledger write because older
closed entries lack supplementary disposition fields; recomputation correctly
uses their preserved done statuses and the204 earlier closed-record boundary.
No unrelated record or test policy was changed to satisfy either validation.

Final Gitleaks8.21.2 exits1 with1561 findings across17635 exported tracked files
plus authored scanner/contract additions. Its path/rule/redacted-match multiset
is exactly identical to entry: zero new findings or exemptions. The raw gate
remains a release blocker. Current inspected EventStore/Builds CI YAML has no
executing Gitleaks reference despite historical config comments; wiring stays
unchanged. [Comparison](dw-517-secret-scan-comparison.json) and
[eleven unchanged scan/CI/build configuration files](dw-517-configuration-preservation.json)
record that boundary; no hosted execution of uncommitted source is claimed.

Original entry Tenants `23aeb38f9784cd67b3c2fa66fe1c007799df6826` and EventStore
`24d719292b06d8a9d09771d837f8dd4d8d9b8997` remain unchanged baselines. Preserved
external heads are Tenants `51f404513f2657e98f949f58f0df43b99b68a7d0` and EventStore
`241652627a52e47c189b2b052db407cec172f813`; complete branches, working/index
state and recorded/checked-out pointers remain in the entry observations. All
1371 historical artifact hashes, B1–B8 evidence, archive and Tenants sprint match
entry; owning sprint exactly matches the external merge. DW517 gitlink validation
still exits1 on that external checkout movement, while B6/B8 checks retain their
five inherited failures. No pointer reset, baseline narrowing or false bundle
ownership attribution was used. No assistant staging, commit, push, branch,
dependency update, deployment, production replay or data migration ran.

Only DW-517 closes:643 IDs remain,319 done/324 open,288 index rows covering every
open ID exactly once. Reconciled counts are115 done/236 actionable/15 blocked/
26 awaiting-decision/9 accepted-limitation/38 uncertain; older closed records
without disposition fields stay intact. [Ledger/index verification](dw-517-ledger-validation.json)
confirms every unrelated original-entry record is unchanged. Existing seven
authority/recovery/governance decisions, provenance, raw secret-scan/CI, nested
layout/startup/provider, current-candidate NFR10/performance, hosted TLS/browser,
production authority/revocation and human/authenticated-browser holds remain.
DW517 closure establishes its local controls, not full release readiness.

**Next selected bundle: DW-105/DW-108, shared DOM-safe identity encoding.** Current
source still constructs raw tenant IDs in both grids and navigation/focus anchors;
that selection does not alter their open records or any settled decision.

Final audit reverified unchanged source digests, both empty indexes, uninitialized
nested dependencies, all1371 historical hashes, original B1–B8 readiness prefix
and exact coverage of324 open IDs by288 rows. The audit's first comparison
incorrectly trimmed the baseline file's trailing newline and was rejected before
writing any snapshot; the corrected exact-content comparison passes. This
checker failure and its correction are retained in command evidence; DW643 and
every other unrelated record remain byte-identical to original entry.


## DW-105/DW-108 remediation evidence — 2026-10-14

**Current assessment after this bundle: not ready for release. Only DW-105/DW-108 close.** Shared deterministic DOM identity encoding, safe blank/unsupported rendering, exact logical identity and existing route/authorization/command/focus rules pass independent review and actual rendering/navigation/focus regressions. Final unfiltered Debug and maintained Release UI pass4971/4971 each with zero errors/failures/skips/not-run; independent focused recheck83/83 passes. Chromium verifies33 distinct exotic anchors and scoped whitespace/wrapping,12 exact audit-row returns, four actual standalone/workspace panel detail returns, the actual all-tenants heading and subject-change cancellation. Existing browser controls and gates remain enabled.

[Complete contract, triage, initial failures, exact commands and limits](dw-105-108-validation-evidence.md), [command results](dw-105-108-command-results.json), [full UI counters](dw-105-108-full-ui-summary.json) and [entry/preservation evidence](dw-105-108-preservation.json) retain the bounded qualification. The broad Release solution command remains red on NU1903 for the pinned SignalR dependency; a strict package-only UI diagnostic remains blocked by seven existing projection-scope API gaps. Maintained UI success uses its unchanged FrontComposer source exception and retains audit warnings; it is not a package-only or all-release-gate pass.

Historical B6/B8 story-gitlink checks retain five inherited failures; DW-517 and this bundle retain the external EventStore checkout movement failure. Original full baselines/pointers are untouched and no inherited work is attributed to this bundle. Existing raw secret-scan/CI wiring, seven authority/recovery/governance decisions, nested-layout, NFR10/performance, hosted TLS/browser, production authority/revocation/provider and authenticated browser/clipboard/human AT holds remain. Local isolated Aspire resources ran healthy and were stopped, without production mutation; static rendered browser fixtures establish DOM/navigation/focus behavior, not provider or authenticated topology acceptance.

All643 IDs remain:321 done/322 open,287 index rows; reconciled117 done/234 actionable/15 blocked/26 awaiting-decision/nine accepted limitations/38 uncertain. Every unrelated ledger record, original readiness text, B1–B8, DW-517, archive, sprint,1371 inherited historical artifact hashes, indexes, dependency revisions/configuration and gates remain preserved. No staging, commit, push, branch, dependency update, deployment, production replay or data migration ran.

**Next selected bundle: DW-112**, align the authoritative-search status filter's scope-aware visible label and accessible name; current source still has the recorded mismatch. The selected item remains open. These two closures do not establish full release readiness.


## DW-112 remediation evidence — 2026-10-15

**Current assessment after this bundle: not ready for release. Only DW-112 closes.**
The status selector's visible label and host name share the existing snapshot-driven
EN/FR resource key. A limited label update keeps Fluent5's existing native combobox
name current without replacing its focused button or changing selected status,
filter/search/paging, authorization or commands. Independent review has no unresolved
finding after seven individually triaged corrections. Actual Fluent tests cover
ordinary, fallback, authoritative ready/partial/empty results, search/recovery/clear,
delayed initial loading and a held status change. Final131 focused cases in each
configuration, unfiltered per-project Microsoft.Testing.Platform UI4987/4987 in both
Debug and maintained Release, and both maintained Chromium lanes pass with zero
failures/skips. Chromium computes all30 EN/FR names, preserves native focus/identity
and rejects stale-name controls; existing browser scenarios and bounds remain intact.

[Contract, review, exact commands, initial failures and limits](dw-112-validation-evidence.md),
[command outputs](dw-112-command-results.json), [UI counters/assets](dw-112-ui-summary.json),
[entry state](dw-112-entry-state.json) and [preservation audit](dw-112-preservation.json)
retain the bounded qualification. The initial fixture/Fluent-startup/initialization
failures, failed one-expression native-name transition, interim keyed-control focus
loss, nullable fixture build failures and first provenance metadata failure remain
visible. Final stable-control results supersede those candidates without rewriting
their evidence. Existing UI skips are zero; DW-517's25 Server skips remain historical.

Broad Release solution build remains blocked by two NU1903 errors on the pinned
SignalR dependency; strict package-only UI remains blocked by seven unchanged
projection-scope API gaps. Maintained UI success keeps its existing FrontComposer
source exception and advisory warnings. B6/B8 retain five inherited story-gitlink
failures each; DW-517, DW-105/DW-108 and DW-112 retain the inherited external
EventStore checkout movement. No baseline or pointer was reset or attributed to
this bundle. Existing raw secret-scan/CI, seven authority/recovery/governance
decisions, nested-layout/startup/provider, NFR10/performance, hosted TLS/browser,
production authority/revocation and authenticated browser/clipboard/human AT holds
remain. Isolated Aspire resources became healthy and were stopped; rendered browser
fixtures do not establish authenticated topology, live Memories/provider or human AT
acceptance. No nested dependency was initialized.

All643 IDs remain:322 done/321 open,286 index rows; reconciled118 done/233 actionable/
15 blocked/26 awaiting-decision/nine accepted limitations/38 uncertain. Only DW-112
changes disposition; unrelated records/action rows, original readiness bytes,
B1–B8, DW-517, DW-105/DW-108,1371 inherited historical hashes, inherited dirty source,
external commits/pointers, archive/sprint, resources, gates and decisions are preserved.
No staging, commit, push, branch, dependency update, umbrella gitlink change,
deployment, production replay or migration ran.

**Next selected Tenants-owned bundle: DW-150 alone**, the still-live cancellation of
a combined member refresh by detail refresh. Current source reroutes only a flagged
member-page load; the combined read lacks that flag. The ledger records it as
open/actionable with no dependency or policy choice. DW-121 remains open for its
owning Hexalith.Builds workflow. This closure is scoped UI evidence, not full release
readiness.


## DW-150 remediation evidence — 2026-10-15

**Current assessment after this bundle: not ready for release. Only DW-150 closes.**
A detail refresh now reroutes either held member navigation or a refreshing member
snapshot through the existing combined read, reissuing the last-confirmed cursor
and preserving independent detail/member evidence, cancellation, generation,
caller/tenant binding, truthful failure and command/lifecycle behavior. Production
changes are one guard and its comment; no focus/rendering or public API changed.

Seventeen actual Fluent held-read cases cover repeated supersession, old completions
before/after the owner, synchronous detail with held members, cursor/history and
independent validators, failures/denial/wrong caller, cooperative cancellation,
tenant/caller/pending-caller/sign-out/disposal and obsolete/owning cursor recovery.
Independent review resolves five individually triaged test gaps; its final17-case
recheck passes with no remaining finding. Final focused332 and full per-project
MTP UI5004 pass in both Debug/maintained Release, zero failures/skips. Both unchanged
Chromium lanes and existing controls pass. [Complete contract, triage, exact
commands, initial failures and limits](dw-150-validation-evidence.md),
[command outputs](dw-150-command-results.json), [UI counters](dw-150-ui-summary.json),
[entry state](dw-150-entry-state.json), [preservation](dw-150-preservation.json) and
[ledger verification](dw-150-ledger-validation.json) retain the bounded evidence.
The initial14/14 missing-reissue failures, two configuration-state fixture failures
and one non-rendering recovery-wait failure remain visible. Gates, exclusions,
floors, timeouts, versions and nested dependency initialization remain unchanged.
Existing UI skips are zero; historical DW-517 Server25 skips are not reclassified.

Broad Release solution still exits1 on two pinned SignalR NU1903 errors; strict
package-only UI exits1 on seven existing projection-scope API gaps. Maintained UI
uses the existing FrontComposer source exception, retaining the advisory. B6/B8
retain five inherited provenance failures each; DW-517, DW-105/DW-108, DW-112 and
DW-150 retain the external EventStore checkout movement failure. No baseline or
pointer reset, new ownership declaration or validator bypass ran. Raw secret-scan/
CI wiring, seven authority/recovery/governance decisions, nested-layout/provider/
startup, current-candidate NFR10/performance, hosted TLS/browser, production
provider/authority/revocation and authenticated browser/clipboard/human AT holds
remain. Local isolated Aspire resources ran healthy and exact-target stop passed;
static Fluent browser fixtures establish their bounded DOM/focus contract, not
authenticated topology or human AT acceptance.

All643 IDs remain:323 done/320 open,285 index rows; reconciled119 done/232 actionable/
15 blocked/26 awaiting-decision/nine accepted limitations/38 uncertain. Only DW-150
changes disposition. Every unrelated record/action row, original readiness byte,
prior findings/B1–B8/DW-517/DW-105/DW-108/DW-112, inherited/external work and pointers,
archive/sprint and unresolved decision is preserved. No staging, commit, push,
branch, dependency update, umbrella gitlink edit, deployment, production replay
or data migration ran.

**Next selected bundle: DW-164/DW-183, route-switch subscription handoff.** Both
remain open/actionable/P2 with no dependency; DW-183 explicitly duplicates DW-164.
Current old-tenant setup can finish and clear its in-flight flag without scheduling
the incoming tenant's retry. Keep separate DW-166/DW-173 timeout work and existing
retry budget unchanged. DW-121 remains Builds-owned. DW-150 closure does not
establish full release readiness.

The initial DW-150 ledger audit passed preservation/coverage but rejected an
explicit-disposition-only done count. Correct recomputation retains the204 earlier
closed-record boundary and counts119 reconciled done;312 closed entries without
disposition fields remain untouched. Both audit results are preserved.


## DW-164/DW-183 remediation evidence — 2026-10-15

**Current assessment after this bundle: not ready for release. Only DW-164/DW-183 close.**
These records duplicate the same missing handoff. Obsolete setup now clears its
atomic gate and directly re-evaluates the latest authorized scope on the renderer,
without requiring an unrelated render. Route/caller epochs reject obsolete setup
and copied callbacks; caller invalidation detaches only the page's reference-counted
lease. Current failure budget/recovery, setup CancellationToken.None/timeout policy,
DW-150 combined reads, command/lifecycle, cursor/history and focus remain intact.

Thirty-two actual Fluent held-setup cases cover obsolete success/Empty/throw,
repeated/successive/returning routes, same-route paging/combined/detail refresh,
current failure budget/recovery, pending/changed/returned callers, sign-out, invalid
route and disposal, shared leases, copied callbacks and suspended unsubscribe.
Assertions prove actual authoritative requests/ETags, retained Refreshing evidence,
rendered row/configuration changes, final snapshots and exact ownership/cleanup.
Independent review resolves six evidence gaps and one synchronization correction;
final independent32/32, focused364/364 and full per-project MTP UI5036/5036 pass in
both Debug/maintained Release, zero failures/skips. Both unchanged Chromium lanes
and negative controls pass. [Complete contract, triage and validation limits](dw-164-validation-evidence.md),
[exact commands/outputs](dw-164-command-results.json), [UI summary](dw-164-ui-summary.json),
[entry](dw-164-entry-state.json), [preservation](dw-164-preservation.json) and
[ledger/index audit](dw-164-ledger-validation.json) retain the evidence.

Initial evidence remains visible: lifecycle-owned fixture could mask the wakeup;
a second fixture exhausted its budget; one build used an incorrect component name.
The corrected held-OnAfterRender baseline fails12/14. First fixed16 fails three
cleanup assertions counting earlier fixture cleanup. The independent32-case and
focused364-case rechecks each expose one all-gateway-count synchronization failure;
final renderer-atomic relevant-read/snapshot assertions correct those test races.
No gate, exclusion, floor, bound, package version or nested initialization changed.
UI skips are zero; historical DW-517 Server25 skips remain unchanged/unrerun.

Broad Release solution still exits1 on two pinned SignalR NU1903 errors; strict
package-only UI exits1 on seven existing projection-scope API gaps. Maintained UI
retains its FrontComposer source exception/advisory. B6/B8 retain five inherited
provenance failures each; DW-517, DW-105/DW-108, DW-112, DW-150 and this bundle
retain the external EventStore checkout movement failure. No baseline/pointer reset,
false ownership declaration or validator bypass ran. Raw secret-scan/CI wiring,
seven authority/recovery/governance decisions, nested-layout/startup/provider,
current-candidate NFR10/performance, hosted TLS/browser, production authority/
revocation/provider and authenticated browser/clipboard/human AT holds remain.
Isolated Aspire startup reached Running/security Healthy and exact-target stop
passed; initial Waiting/Unhealthy TLS observations are retained. These local/static
checks do not establish authenticated topology or release acceptance.

All643 IDs remain:325 done/318 open,284 index rows; reconciled121 done/230 actionable/
15 blocked/26 awaiting-decision/nine accepted limitations/38 uncertain. Only the
two duplicate dispositions and their grouped action row close. All unrelated
records/rows, original readiness bytes, B1–B8, DW-517, DW-105/DW-108, DW-112, DW-150,
inherited/external work and pointers, archive/sprint, gates and unresolved decisions
are preserved. No staging, commit, push, branch, dependency update, umbrella gitlink
edit, deployment, production replay or data migration ran.

**Next separate bundle: DW-166/DW-173**, apply the existing shared configured finite
resolver/subscription bound and replaceable cancellation decisions, preserving
three-failure accounting. Current source retains the recorded unbounded/default-
cancellation paths. DW-176/DW-184 remain separately tracked dependent consumers;
DW-121 remains Builds-owned. This closure is scoped UI evidence, not full release
readiness.
