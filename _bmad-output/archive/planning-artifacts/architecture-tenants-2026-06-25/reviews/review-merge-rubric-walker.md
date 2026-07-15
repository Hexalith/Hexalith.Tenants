# Merge Review — Rubric Walker

**Reviewed:** `_bmad-output/planning-artifacts/architecture/architecture-tenants-2026-06-25/ARCHITECTURE-SPINE.md`  
**Merge target:** `_bmad-output/planning-artifacts/architecture.md`  
**Lens:** BMad Architecture Reviewer Gate — Good-spine checklist  
**Date:** 2026-07-15

## Gate Verdict

**FAIL — the AD-1..AD-13 merge is textually faithful, but the merged architecture is not yet a contradiction-free, current brownfield contract.** One governing-policy conflict blocks an unqualified implementation-ready verdict. Four high-severity findings and two medium findings should be dispositioned before the legacy architecture is treated as safely archived.

The deterministic pass is clean:

```text
lint_spine.py: ok=true, total_findings=0
```

That establishes mechanical integrity only: all thirteen ADs have unique identifiers and complete `Binds` / `Prevents` / `Rule` fields, the stack table is syntactically pinned, and no placeholders remain.

## Canonical Incorporation Audit

The canonical document incorporates all thirteen decisions under an explicit precedence clause at `architecture.md:68-152`.

| Source decisions | Incorporation result | Notes |
| --- | --- | --- |
| AD-1 | Faithful | `Binds`, `Prevents`, and `Rule` preserve the source meaning. |
| AD-2 | Faithful with a non-weakening expansion | The canonical `Binds` adds the global-administrator return flow; the `Prevents` and `Rule` text is unchanged in substance. |
| AD-3..AD-11 | Faithful | Differences are punctuation/conjunction edits only; no rule is weakened. |
| AD-12 | Faithful | The one-at-a-time fallback and replacement condition are preserved. A conflicting later FC-CNC status statement is a separate active-guidance defect (H-4). |
| AD-13 | Faithful | The repository-owned UI host/AppHost rule is preserved. That rule conflicts with the governing repository instruction (C-1). |

The merge also preserves all six source Deferred decisions at `architecture.md:158-164` and explicitly records the current AD-6 implementation divergence at `architecture.md:154-156`. The precedence clause is useful, but it does not make contradictory lower sections harmless: builders still consume the entire canonical document, and an explicit architecture contract should not require them to infer which same-document status claim is stale.

## Critical Finding

### C-1 — AD-13 conflicts with the governing domain-module rule

**Checklist:** ratifies rather than contradicts the brownfield codebase and inherited constraints; inherited invariants are not weakened.  
**Evidence:**

- `ARCHITECTURE-SPINE.md:119-123` requires `src/Hexalith.Tenants.AppHost` to wire the UI host.
- `architecture.md:523-528` keeps that AppHost and attempts to narrow the platform policy to domain-service modules.
- `references/Hexalith.AI.Tools/hexalith-llm-instructions.md:116-129`, which the repository's `AGENTS.md` makes governing, says a domain module **must not** ship its own `*.AppHost`, `*.Aspire`, or `*.ServiceDefaults` project; it contains no presentation-host exception.
- The current code and `_bmad-output/project-context.md:77-78` do contain and endorse the AppHost, so brownfield reality and repository governance themselves disagree.

**Impact:** Two teams following their respective authoritative inputs can make incompatible topology/ownership choices. AD-13 cannot simultaneously be an adopted invariant and conform to the governing module boundary as currently written.

**Disposition:** **Discuss / block.** Choose one authority explicitly: move orchestration to the platform/host repository and amend AD-13, or formally amend the governing repository instruction to define the repository-specific composition-root exception. Do not rely on the explanatory parenthesis at `architecture.md:526-528`; it asserts an exception absent from the governing rule.

## High Findings

### H-1 — The pinned technology reality is stale

**Checklist:** named technology is verified-current; the spine ratifies the brownfield codebase.  
**Evidence:**

- The spine pins Fluent `5.0.0-rc.3-26138.1`, FrontComposer source `e2ac85aac67d`, EventStore source `60e63a95bed8` / package `3.19.0`, and Memories source `24757db93c90` / package `1.31.1` at `ARCHITECTURE-SPINE.md:169-184`.
- The current root-declared submodules resolve to FrontComposer `4aa4210d4aeb...`, EventStore `1a01e0eae50e...`, and Memories `c212a1ba6af0...`.
- Current central pins in `references/Hexalith.Builds/Props/Directory.Packages.props` are Fluent `5.0.0-rc.4-26180.1` (`:200-201`), EventStore `3.64.1` (`:5`), and Memories `2.5.0` (`:9`).
- The canonical document repeats the old Fluent/package assumptions in its technology and validation narrative.

**Impact:** FrontComposer, Fluent, EventStore metadata, and Memories search contracts are load-bearing to AD-3, AD-6, AD-8, AD-10, and AD-12. A syntactically pinned but obsolete stack does not satisfy the reviewer gate's verified-current requirement and can make the claimed conformance tests validate the wrong API surface.

**Disposition:** **Autofix after verification.** Refresh the canonical stack facts from the current central package file and root-declared submodule SHAs, then re-check the affected APIs and conformance tests. The legacy source may remain historical once archived, but the canonical document must not present its old pins as current.

### H-2 — Requirements traceability cites nonexistent NFR identifiers

**Checklist:** if a spec drove the spine, it covers the spec's capabilities.  
**Evidence:**

- Spine frontmatter claims it binds `NFR-1..NFR-10` (`ARCHITECTURE-SPINE.md:10-14`).
- The capability map claims `NFR-6, NFR-7, NFR-8, NFR-9` evidence (`ARCHITECTURE-SPINE.md:240`).
- The final PRD defines only `NFR-1..NFR-5` (`prd.md:335-339`), corroborated by its downstream-readiness review.
- NFR-2 authorization, NFR-4 testability, and NFR-5 no-data-store-edits are represented semantically by AD-5/AD-7/AD-9, AD-11, and AD-12, but the spine does not trace those real identifiers correctly.

**Impact:** The map can produce false-positive coverage in implementation-readiness checks while omitting direct trace links for three real requirements. This is a traceability defect, not merely editorial numbering.

**Disposition:** **Autofix.** Change the bind range to `NFR-1..NFR-5`; replace the nonexistent NFR row with explicit mappings for NFR-2, NFR-4, and NFR-5, and keep NFR-1/NFR-3 links on AD-6/AD-8/AD-7 as appropriate.

### H-3 — The operational/environmental envelope is incomplete

**Checklist:** every feature-altitude dimension is decided, deferred, or open, especially deployment/environments, infrastructure/provider strategy, and operations.  
**Evidence:**

- AD-13 and the Deployment convention decide app/container ownership and local AppHost wiring (`ARCHITECTURE-SPINE.md:119-123`, `:167`).
- The spine does not decide or defer production environment topology, provider/hosting strategy, InteractiveServer multi-replica/session-affinity behavior, UI circuit/reconnect scaling, secrets/key management, health/SLO ownership, telemetry/alerting, or deployment rollback/forward strategy.
- `Multi-replica cursor durability` is deferred, but only for backend cursor keys; it does not cover the stateful InteractiveServer/BFF operational model.
- The canonical document adds CI, container, OpenTelemetry, and dev/prod auth notes, yet still does not close or explicitly defer the production scaling/ownership questions.

**Impact:** Two deployment units can choose incompatible session routing, key sharing, identity/secrets wiring, and operations ownership while each still claims compliance with AD-13.

**Disposition:** **Defer explicitly or decide.** A feature spine may inherit a parent platform envelope, but it must name that parent as binding. Otherwise add explicit Deferred/open items with revisit conditions for the production topology and operating model.

### H-4 — Canonical FC-CNC guidance contradicts itself

**Checklist:** every Rule is enforceable and actually prevents its divergence; contradictory active guidance is absent.  
**Evidence:**

- AD-12 says the approved one-at-a-time fallback remains until a stronger FrontComposer command contract replaces it (`architecture.md:142-146`).
- D3 says `FC-CNC` is already a confirmed contract (`architecture.md:401-403`).
- The communication section says the `FC-CNC fallback` remains **until FC-CNC lands** (`architecture.md:481-483`).
- Elsewhere the action-item history says FC-CNC confirmation is closed.

**Impact:** A command-flow implementer cannot tell whether the fallback remains authoritative, whether FC-CNC has landed but is insufficient, or whether the status text is stale. This is precisely the command-posture divergence AD-12 is meant to prevent.

**Disposition:** **Autofix.** State one status once. If FC-CNC is confirmed but does not yet replace the one-at-a-time policy, say that explicitly and define the replacement acceptance condition; otherwise mark it unlanded consistently.

## Medium Findings

### M-1 — Component-location guidance permits incompatible structures

**Checklist:** the spine fixes real divergence points for the level below.  
**Evidence:**

- The spine convention restricts surfaces to `Components/Tenants`, `Components/Users`, `Components/Pages`, or `Components/Shared` (`ARCHITECTURE-SPINE.md:157`).
- Canonical Structure Patterns introduce sibling `Components/Audit` and `Components/GlobalAdministrators` (`architecture.md:610-615`).
- The canonical complete tree omits those siblings (`architecture.md:716-721`), while the requirements map uses `Components/GlobalAdministrators` and `Components/Tenants/Audit` (`architecture.md:767-769`).
- Current code uses `Components/Tenants/Audit` and has no sibling `Components/Audit` or `Components/GlobalAdministrators` directory.

**Impact:** Story agents can place the same feature in three different locations while citing valid-looking architecture text.

**Disposition:** **Autofix.** Ratify the current tree (`Components/Tenants/Audit` and the current global-administrator page/component placement) in one canonical structure and remove obsolete alternatives.

### M-2 — Three governance phrases lack an enforceable approval seam

**Checklist:** every Rule is enforceable and prevents its stated divergence.  
**Evidence:** AD-3 allows custom markup for `documented gaps`; AD-4 allows an `approved fallback`; AD-11 allows guard loosening through an `explicit approved story`. None names the required record, approver, or test/allowlist that makes approval machine- or reviewer-verifiable.

**Impact:** Independent stories can each document or approve their own exception and still satisfy the literal rule, weakening AD-3/AD-4/AD-11 at exactly the FrontComposer boundary they protect.

**Disposition:** **Autofix.** Bind exceptions to the existing conformance allowlist plus a named Product/UX/FrontComposer approval artifact, and require the story to link that artifact rather than self-approve.

## Good-Spine Checklist Summary

| Check | Result | Reason |
| --- | --- | --- |
| Fixes real divergence points; misses none | **Partial** | Strong UI/state/transport decisions; operations and component placement remain divergent. |
| Every Rule is enforceable and prevents its divergence | **Partial** | Most are crisp; exception approval and FC-CNC replacement status are ambiguous. |
| Deferred does not hide divergence | **Partial** | Existing items have good revisit conditions; production operating-model gaps are silent rather than deferred. |
| Named technology verified-current | **Fail** | Multiple package pins and all three source SHAs are stale. |
| Ratifies brownfield codebase | **Partial / blocked** | Most decisions match code; AD-6 is honestly recorded as nonconformant, while AppHost governance and stack facts conflict with current authorities. |
| Covers the driving spec | **Partial** | FR coverage is strong, but NFR trace IDs are invalid and real NFR links are incomplete. |
| No inherited rule weakened | **Fail** | AD-13 conflicts with the governing domain-module instruction unless a formal exception is established. |
| Every owned dimension decided/deferred/open | **Fail** | Production environment, scaling, provider, and operations ownership are not closed or inherited explicitly. |

## Strengths Preserved by the Merge

- The thirteen ADs are lean, stable, and mechanically complete; their `Prevents` clauses name genuine multi-story divergence risks.
- AD-1 through AD-12 establish a coherent UI composition, transport, truth-state, freshness, safety, search, test, and command posture.
- The canonical merge does not conceal the current AD-6 code divergence: `TenantQueryGateway` still injects `IEventStoreGatewayClient` and calls `SubmitQueryAsync`, and both source and target identify that as remediation rather than weakening AD-6.
- Deferred items are specific and carry concrete revisit conditions.
- The explicit precedence clause makes the AD layer clearly dominant over historical prose; the remaining issue is to reconcile, not merely subordinate, active status and structure statements that builders still encounter.

## Required Gate Disposition

Before declaring the merge implementation-ready and archiving the legacy set as fully superseded:

1. Resolve C-1 by aligning the governing AppHost policy and AD-13.
2. Refresh canonical technology/source pins and re-verify affected contracts.
3. Correct NFR traceability to the PRD's actual `NFR-1..NFR-5` set.
4. Normalize FC-CNC status and component locations.
5. Either inherit a named parent operational envelope or add explicit production-operations Deferred/open items.

