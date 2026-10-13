---
title: 'B6 separate tenant and global-administrator audit evidence'
type: 'bugfix'
created: '2026-10-13'
status: 'in-review'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '57b881beebfbcdf0fe6d7bc4c23a0fc70903136d'
context:
  - 'AGENTS.md'
  - '_bmad-output/project-context.md'
  - 'references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - 'references/Hexalith.AI.Tools/hexalith-state-instructions.md'
  - 'references/Hexalith.AI.Tools/hexalith-ux-instructions.md'
---

<frozen-after-approval reason="User explicitly authorizes complete B6 implementation, review and validation">

## Intent

DW-626/DW-628 are verified: tenant `system` and global singleton both write `audit:system`; global replay replaces tenant evidence. Separate writers and provide compatible reads that preserve evidence and fail closed on ambiguous legacy ownership. Literal `system` remains valid.

## Boundaries & Constraints

Always use EventStore `IReadModelStore` and `ReadModelWritePolicy`; inspect and follow the required Hexalith baseline including state instructions. Preserve historical B1–B5 evidence, all unrelated ledger entries, archive, sprint and unresolved decisions. Initial tree is clean on `main`; inherited B5 is committed in full baseline above. B6 diff includes only newly changed files. User authorizes autonomous implementation; no additional permission checkpoint is needed for reversible local work.

Never migrate/replay production data, change dependencies/submodules, stage, commit, push or deploy. Leave B7 and DW-627 serialization/version decisions open. Read repository build/format/test guidance before editing; one C# type per file.

## I/O & Edge-Case Matrix

| Scenario | State/action | Expected behavior |
| --- | --- | --- |
| Both orders | tenant `system`, then global; reverse order | Separate persisted audit records retain each owner's evidence |
| Replay and duplicates | full, repeated and shorter replay, duplicate delivery | Preserve stored audit evidence; deduplicate same owner/EventId |
| Legacy | global-only, tenant-only, mixed legacy `audit:system` | Immutable legacy plus new records remain readable by ownership |
| Ambiguous legacy | unknown/null/inconsistent row or conflicting same-owner EventId | No evidence returned; safe unavailable result; original bytes unchanged |
| Reads | ordinary tenant; system default; explicit tenant/global stream | Ordinary contract preserved; system default combined, selectors isolate ownership |
| Authority/provenance | unauthorized; missing canonical global replay/version | Authorization first; no global confirmation from tenant/legacy sequence |
| Identity collision | tenant with colon/prefix-looking identifier | Cannot share global/system dedicated keys |
| Paging/cache | selector changes or source changes | Cursor and retained snapshots bind selector; validator covers every contributing source |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Tenants/Projections/{TenantProjectionHandler,GlobalAdministratorProjectionHandler}.cs`: current audit writes; tenant uses guarded merge, global replaces. Reuse platform write policy.
- `src/Hexalith.Tenants.Server/Projections/{TenantAuditReadModel,TenantAuditProjection}.cs`: typed rows and global causation/sequence; exact event types establish ownership.
- `src/Hexalith.Tenants/Queries/Handlers/{GetTenantAuditQueryHandler,TenantQueryHandlerBase}.cs`, `Queries/TenantQueryCursorScopes.cs`: authorization, payload parsing, freshness/ETag, protected paging.
- `src/Hexalith.Tenants.Contracts/Queries/GetTenantAuditQuery.cs`: generated REST query; additive optional `Stream` member.
- UI `State/TenantAudit/{TenantAuditRequest,TenantAuditSnapshot}.cs`, `Services/Gateways/{TenantsRestQueryClient,TenantQueryGateway,GlobalAdministratorAuditProofLookup}.cs`: forward/bind selector and select global stream for exact proof.
- Existing server projection/query tests and integration fixtures: preserve maintained gates; add serialized persisted-store end-state coverage and real store tests where feasible.
- `docs/event-contract-reference.md`, deferred ledger, maintenance index, release readiness: compatible contract, rollout preconditions and two closures only.

## Tasks & Acceptance

- [ ] Projection handlers and shared ownership/key helper: ordinary `audit:{id}` unchanged; `system` tenant uses `audit-tenants:system`, global uses `audit-global-administrators:singleton` (outside arbitrary tenant prefix). Never write legacy `audit:system`. Merge global audit by EventId through guarded policy so shorter replay cannot delete evidence. Validate incoming ownership before writes.
- [ ] Query reader/helper: union new owner records with exact-classified legacy subsets without rewriting legacy. Known tenant/global event allowlists and exact TenantId establish ownership; null/unknown/inconsistent ownership and conflicting duplicates fail closed. Deduplicate by owner plus EventId. Selectors `tenant` and `global-administrators`, global allowed only for system; omitted selector preserves default combined system read. Combined view supplies an opaque composite version so existing UI metadata readiness remains supported; it cannot expose one aggregate sequence. Only canonical complete global evidence may supply ordered global version. Freshness remains conservative and ETag must cover contributing sources; omit validator when necessary rather than reuse one source's ETag.
- [ ] Contract, REST, cursor and UI seams: parse/validate and forward optional Stream; cursor and snapshot retention bind it; global proof lookup and `UI/Components/Pages/TenantAuditPage.razor` retained original-global-row recovery request global stream. Do not silently reinterpret `system` as reserved tenant.
- [ ] Tests: all matrix scenarios assert persisted serialized end-state and legacy preservation, reads/filters/paging, cancellation, invalid selectors, cache isolation and proof selection. Run focused Debug project-reference builds/tests followed by maintained per-project Release checks, coverage, browser and package consumers.
- [ ] Documentation, independent review and closure: record initial failures/exact commands/results/limits; remove only the two closed maintenance references and adjust counts. Preserve historical findings/pre-B6 verification/actions. Root handles review, maintained validation and final closure; implementation agent may draft rollout docs but leaves ledger/readiness closure to root.

Given both aggregate histories, when either projection replays, then the other owner's persisted audit evidence survives. Given legacy uncertainty, when audit is read, then safe failure prevents falsely attributed evidence. Given canonical global replay, when exact proof is requested, then only global ordered version/rows can confirm it.

## Implementation Notes

Storage/read choices above are reversible code changes within the authorized scope. No production mutation occurs. Logs and initial hashes: `/tmp/tenants-b6-20261013/`. Maintain exact failure evidence, never weaken gates. Read all four user-specified evidence documents for relevant constraints. No new intent gap remains.

## Spec Change Log

## Review Triage Log

All three independent lenses were launched before collecting findings; all returned before triage. Review content was the B6-only unstaged diff against the full baseline; no Git staging occurred.

| Finding | Verdict | Evidence and disposition |
| --- | --- | --- |
| Blind 1: repeated cross-owner EventId rejected by gateway | medium | Verified gateway uses EventId and reader allowed cross-owner duplicates. Preserve the existing reference contract: default combined reads must fail closed on an ambiguous receipt reference; explicit owner reads remain available. Patch reader and persisted regressions. |
| Blind 2: grid keys/receipt selection lack owner | high | Grid keys and receipt URIs use EventId; cross-page equal references can select another owner's receipt. Reject ambiguous combined references before pagination, preserving isolated reads, rather than expand the public receipt identity. Same root cause as Blind 1. |
| Blind 3: gateway accepts wrong selected owner | medium | Validator checks supported types but not Stream. Patch exact owner checks and selector-sensitive gateway tests. |
| Blind 4: contradictory same sequence/message DTOs establish completeness | medium | Sequence map compared only MessageId. Patch complete envelope comparison and persisted regression; duplicate identical delivery remains valid. |
| Blind 5: unordered replay can advertise canonical version with wrong fold | high | Changed max-version stamping exposes a higher sequence while fold followed input order. Apply global history in aggregate sequence order and test persisted authority/audit state. Distributed stale rebuild DW-627 remains open. |
| Blind 6: non-object recognized rejection throws | medium | TryGetProperty on array/null/string throws InvalidOperationException. Guard root shape; malformed history must withhold ordered provenance while retaining valid success evidence. |
| Blind 7: incomplete rejection fields counted | medium | TenantId-only check accepted missing required UserId/ActorUserId/CommandName. Validate recognized rejection contract shape; test malformed fields at persistence/read boundary. |
| Blind 8: short rejection event names unsupported | medium | Success names accept simple/full names; rejection branch only full names. Support both representations and test every recognized rejection. |
| Blind 9: malformed canonical row sequences | medium | Reader did not reject nonpositive or duplicate conflicting sequence assignments. Withhold ordered version and preserve evidence; test corrupted dedicated records. |
| Blind 10: policy retry diagnostics suppressed | medium | Dispatcher logs terminal failure, but policy's successful retry diagnostics used NullLogger. Pass dispatcher logger through an internal constructor; preserve public constructors and existing diagnostics. |
| Blind 11: AuditUnavailable maps to Error/500 | false | The current SDK has no domain unavailable sentinel; its sanitized 500 and UI Error return no evidence or metadata and fail closed. User requires safe failure, not a new shared HTTP taxonomy. Document and verify this exact existing boundary rather than edit dependencies or add HTTP plumbing. |
| Edge 1: non-object rejection | medium | Independently verified; same patch as Blind 6. |
| Edge 2: combined reference consumer mismatch | medium | Independently verified; same root cause and fail-closed patch as Blind 1/2. |
| Verification 1: gateway stream forwarding unpinned | medium | Prepared Arg.Any response hid omitted selector. Add selector-sensitive ordered/opaque responses and assert global proof success with exact selector. |
| Verification 2: rejection histories untested | medium | Existing provenance cases contain success only. Add complete success/rejection histories for each supported contract/name representation with persisted row/version/read assertions. |
| Verification other: page fixtures lack stream | medium | Initial 359-case run failed nine cases. Positive fixtures now bind requested Stream; new wrong-stream recovery refusal added. Rerun passes 360/360; production scope guard retained. |

The compatible public receipt contract keys selection by EventId. Repeated references across owners therefore make an omitted-selector combined feed ambiguous, even if each underlying row has a known owner. Fail closed across the entire combined source before filtering/pagination; explicit tenant/global reads preserve and expose each owner's original identifier/evidence. This is a safety refinement within the user's requested compatibility strategy, with no new public identity format. Original B6 implementation and initial failures remain in the logs/diff.

## Verification

Focused: `dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -m:1 -nr:false`; invoke built xUnit DLL with single-dash class filters. Required maintained gate: restore/build `.slnx` Release with warnings-as-errors; individual project suites, non-performance integration, browser, coverage and package validation. Capture exact commands/results and preserve baseline hashes, ledger record text and existing B1–B5 evidence.
