---
title: 'Five deferred items: administrator recovery and authority evidence'
type: 'bugfix'
created: '2026-10-26'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: bd9b458bf80a4defbd80dea769b9ffb39f84e4f6
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Lost administrator command responses strand ownership after projections change (DW-387/DW-625). Teardown needs distinguishing tests (DW-154), whitespace identities need visual qualification (DW-390), and index reads need producer authority without store ETags (DW-568).

**Approach:** Complete these five records together, as requested, through Tenants-owned changes. Commit and push validated changes to `main`. The approved recovery/display decisions below govern implementation.

## Boundaries & Constraints

**Always:** Bind recovery to original caller/message/tenant/domain/aggregate; retain payload, ULID and dispatched ownership. Confirmation requires exact event proof plus authoritative projection advancement. Preserve initial removal/last-administrator safeguards, EN/FR support-safe copy, shared persistence policies and Fluent composition.

**Never:** Infer success from absence/status alone; invent correlation/global sequence; weaken normal tracking, authorization or self-demotion redaction; change shared modules/dependencies; publish releases.

## I/O & Edge-Case Matrix

| State | Required outcome |
| --- | --- |
| Ambiguous original command, changed projection | Selected recovery policy; no new identity |
| Missing/mismatched recovery scope | Reject safely; retain unresolved ownership |
| No store tag, complete index watermarks | Opaque deterministic validator |
| Incomplete legacy coverage | No manufactured authority |

## Approved Decisions

- Recovery: authorized MessageId status lookup obtains correlation only after complete exact message/tenant/domain/aggregate validation. Do not redispatch after projection preconditions change; ordinary correlation-bound tracking stays strict.
- Display: if browser checks reproduce whitespace collisions, add visible Unicode scalar annotations using existing escaping while preserving literal identity and exact command/clipboard values.

</frozen-after-approval>

## Code Map

Path aliases: UI=`src/Hexalith.Tenants.UI`, UT=`tests/Hexalith.Tenants.UI.Tests`, ST=`tests/Hexalith.Tenants.Server.Tests`.

- `UI/Components/Pages/GlobalAdministratorsPage.razor` and `UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor`: retained recovery and teardown.
- `UI/Services/Gateways/{ITenantCommandGateway,TenantCommandGateway,TenantCommandStatusResponse}.cs`: existing MessageId endpoint currently requires known correlation; add a distinct recovery seam.
- `UI/State/GlobalAdministrators/GlobalAdministratorIdentityDisplay.cs`: visual whitespace is literal; accessible encoding tokenizes it.
- `src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs`: retry-safe index updates; preserve existing audit producer versions.
- `UT/Components/AuditAvailabilityStateTests.cs`: reusable real-renderer barriers and dispatcher fault injection.

## Tasks & Acceptance

**Execution:**

- [x] `UT/Components/GlobalAdministratorsPageTests.cs`: worker/blocked-renderer reset and disposal-during-notification tests; removing marshaling/containment must fail. Restore dispatchers/barriers in `finally`.
- [x] Gateway/components above and `UI/Resources/Tenants*.resx`: selected recovery for grant/removal in both surfaces; accurate refusal copy. Cover caller/permission changes, legacy/mismatched status, rejection, remount, late callbacks, duplicate activation and exact confirmation proof.
- [x] Identity encoder and consuming components: qualify actual Fluent markup in maintained Chromium/fonts; retain screenshots/accessibility names, known collision rejection control and exact copy/payload assertions. Implement selected annotations if needed.
- [x] `src/Hexalith.Tenants.Server/Projections/TenantIndexProjectionVersion.cs` and projection handler: fingerprint canonical sorted index semantics/owner watermarks inside the retry transform; exclude timestamps/version. Cover semantic changes, dictionary order, stale/no-op replay, conflicts and incomplete legacy coverage.
- [x] `ST/Projections/TenantProjectionHandlerTests.cs`, `ST/Queries/TenantQueryHandlerETagTests.cs` and `tests/Hexalith.Tenants.IntegrationTests/TenantsApiGeneratedControllerTests.cs`: persisted projection→REST→UI metadata without store tags, tenant audit and store-tag precedence.
- [x] `_bmad-output/implementation-artifacts/deferred-work.md`, colocated validation evidence and `docs/maintenance-backlog.md`: close exactly five after review/checks; preserve all 657 IDs, unrelated bodies, historical fields and release holds.

**Acceptance Criteria:**

- Given an authorized retained attempt, when its projection changed, then the same circuit obtains correctly bound recovery without weakening confirmation or creating a new command.
- Given teardown races, when controlled interleavings execute, then mutation-sensitive tests distinguish correct dispatcher ownership and containment.
- Given rendered identities/no-tag reads, when checked, then executable evidence proves distinction/authority within recorded browser/legacy-data limits.

## Implementation Notes

- Baseline: `bd9b458bf80a4defbd80dea769b9ffb39f84e4f6`, clean main. Read-only parallel investigation only; no tests/runtime checks run.
- Footprint: domain/UI code, one version helper, tests/evidence and ledger. No deployment/data mutation. User approved both recommended policies and continuation on 2026-10-26.

- Runtime baseline established before source edits: `aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj` succeeded. Initial `aspire describe` showed Keycloak initializing and dependent apps waiting; the safe resource-state snapshot is `/tmp/tenants-five-deferred-aspire-baseline.json`. Continue observing this exact AppHost rather than starting another. `aspire logs` uses `--tail`, not `--limit`.

## Spec Change Log

- 2026-10-26: Added a narrow owning deployment-readiness documentation/test correction after the complete Server suite demonstrated that its referenced EventStore validator is absent at the pinned checkout. The no-unsupported-Tenants-validation guarantee remains enforced; no shared dependency was changed.

## Review Triage Log

| Finding | Verdict | Evidence | Route/group |
| --- | --- | --- | --- |
| BH-1 | medium | Nullable capture continues initial dispatch; a later lost response is unrecoverable because both recovery routes require the absent caller. | patch: caller binding |
| BH-2 | medium | New principal awaits omit the existing finite operation policy and caller lifetime; the correction lease is acquired before the unbounded read. | patch: bounded lookup |
| BH-3 | medium | Fourteen physical .log files are ignored and absent from the staged diff, so retained links would break after checkout. | patch: durable evidence |
| BH-4 | medium | The mutation runner accepts every nonzero exit without proving the selected test or expected assertion ran. | patch: mutation qualification |
| BH-5 | low | The runner restores source bytes but not the last mutated binaries; direct assembly runs can execute stale mutants. | patch: mutation cleanup |
| BH-6 | medium | Pixel comparison was executed by the coordinator, but the retained runner only asserts display strings; preserve the actual visual comparison and a same-check collision control. | patch: visual evidence |
| BH-7 | low | The saved Playwright fragment requires a fixed temporary server/directory and omits asset/export setup. Direct reproduction instructions/runner are needed. | patch: browser reproduction |
| BH-8 | medium | The page late-result disposal branch lacks its own replacement/duplicate lookup case; the panel test does not cover that separate branch. | patch: page late coverage |
| BH-9 | medium | Only the page tests caller transition after its final principal read; the separately implemented panel publication guard lacks that interleaving. | patch: panel transition coverage |
| BH-10 | low | The review snapshot contains transitional acceptance and pending-review text. Finalize evidence and review bookkeeping before publication; no intent change is required. | patch: workflow finalization |
| EH-1 | medium | Confirmed the nullable-capture continuation at all three initial dispatch sites; the undispatched lease must be abandoned on refusal. | patch: caller binding |
| EH-2 | medium | A noncompleting authentication provider reaches the new unbounded lookup after correction lease acquisition, before the panel retains that lease for disposal. | patch: bounded lookup |
| EH-3 | medium | Deserialized legacy dictionary values are not runtime-nonnull-guaranteed; a null unrelated tenant entry passes coverage checks and is dereferenced during hashing. | patch: invalid legacy row |
| EH-4 | medium | The mutation exit-code condition also accepts infrastructure failure before the selected assertion. | patch: mutation qualification |
| VG-1 | medium | Pre-verified regression gap: all recovery tests substitute the new production BFF subject resolver; add executing real-composition cases. | patch: production resolver coverage |
| VG-2 | medium | Pre-verified regression gap: delayed panel recovery tests keep the caller unchanged; add the distinguishing queued authentication transition. | patch: panel transition coverage |

## Verification

- Final implementation checks: UI 7,809/7,809; Server 1,525/1,525; generated-controller Integration 47/47; both teardown mutations rejected; maintained Chromium fixtures qualified. Broad Integration source/topology blockers remain separately recorded.
- Implementation evidence, exact commands, screenshots, mutation controls and broad-gate limits are recorded in [validation-dw-154-387-390-568-625.md](validation-dw-154-387-390-568-625.md). Ledger/index closure remains pending coordinating-agent acceptance and review.

- Establish Aspire resource baseline before code changes; record exact blockers separately.
- Build affected UI/Server/Integration test projects in Debug with `-p:UseHexalithProjectReferences=true`; run focused built xUnit v3 assemblies, then affected complete projects individually.
- Run maintained Chromium plus identity qualification and mutation controls; no NVDA/Firefox/WebKit claims.
- Focused formatting, diff hygiene, ledger preservation and independent review; pinned commitlint validation, fetch/divergence inspection, commit and push main after completion.

- Coordinating acceptance: source and targeted tests inspected; all matrix rows executed and passed. Five selected ledger records/index rows reconciled with original fields retained. Formal workflow review follows before commit/push.

- Review pass 1: all three layers completed; every finding is graded above before root-cause grouping. All accepted fixes are bounded corrections/tests/evidence for demonstrated states, with no new public surface or policy decision. The frozen approved intent remains unchanged.

- Final review resolution: BH-1/EH-1 caller refusal, BH-2/EH-2 bounded cleanup, EH-3 null legacy entry, VG-1 production resolver coverage, BH-8 page remount coverage, BH-9/VG-2 panel transition coverage, BH-3 durable logs, BH-4/EH-4 exact mutation qualification, BH-5 restored binaries, BH-6 pixel controls, BH-7 reproduction and BH-10 final bookkeeping are complete. Independent rechecks confirmed no remaining concrete defects/gaps.
- Final qualification over the approved owned snapshot: UI 7835, Server 1527 and focused REST 47 pass; all builds are Debug/source with zero warnings/errors. Maintained browser and normalized pixel reproduction pass. Two test files received only formatter whitespace changes, with non-whitespace content verified unchanged.
- Concurrent main advanced to 19f95ae8d57c6899d2af6aaff57b0334d68cbb85 (routing/catalog work outside ownership). It is preserved; its combined-main SDK Routing compilation blocker is recorded separately. The isolated snapshot uses baseline bd9b458bf80a4defbd80dea769b9ffb39f84e4f6 plus this change and the nine original dependency gitlinks. No shared-module/dependency changes were made.

- Workflow complete: all selected tasks and matrix rows verified, all review corrections resolved, and exact frozen intent preserved. The original request explicitly authorizes committing and pushing main; no additional push approval is needed.
