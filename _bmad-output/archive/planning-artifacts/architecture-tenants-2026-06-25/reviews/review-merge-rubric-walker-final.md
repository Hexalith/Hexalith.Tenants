# Final Merge Review — Rubric Walker

**Reviewed:** `_bmad-output/planning-artifacts/architecture/architecture-tenants-2026-06-25/ARCHITECTURE-SPINE.md`  
**Merge target:** `_bmad-output/planning-artifacts/architecture.md`  
**Prior review:** `reviews/review-merge-rubric-walker.md`  
**Lens:** BMad Architecture Reviewer Gate — Good-spine checklist  
**Date:** 2026-07-15

## Final Gate Verdict

**PASS WITH FOLLOW-UPS — the architecture spine is mechanically sound, the canonical merge faithfully incorporates AD-1..AD-14, and every prior critical/high semantic finding is resolved.** The legacy architecture set can be archived as superseded by the canonical document. Two medium consistency cleanups remain; neither changes an AD or blocks the merge/archive operation.

The explicitly recorded AD-6/AD-8 query-provenance, AD-13 orchestration-migration, and AD-14 multi-replica implementation gaps are real implementation-conformance work. They do not make the architecture contract internally invalid because the source and canonical documents identify them as current-state divergences and state the target rule and remediation sequence unambiguously.

Deterministic evidence remains clean:

```text
lint_spine.py: ok=true, total_findings=0
```

## Prior-Finding Closure

| Prior finding | Final status | Evidence |
| --- | --- | --- |
| C-1 AppHost policy conflict | **Resolved in architecture** | AD-13 now makes the UI host domain-owned and orchestration platform/composing-host-owned; the repository AppHost is explicitly transitional legacy to migrate/remove and prohibited from expanding. The current code divergence is recorded under Implementation Conformance and Deferred. |
| H-1 stale technology reality | **Resolved** | Stack now matches central pins: .NET `10.0.302`, Fluent `5.0.0-rc.4-26180.1`, FrontComposer `3.1.1`, EventStore `3.64.1`, Memories `2.5.0`, Dapr `1.18.4`, Aspire `13.4.6`, xUnit `3.2.2`, and bUnit `2.8.4-preview`. Source SHAs are correctly treated as mutable implementation state rather than architecture invariants. |
| H-2 invalid NFR traceability | **Resolved** | Frontmatter binds `NFR-1..NFR-5`; the capability map references the same real PRD range; the canonical coverage section maps all five requirements semantically. |
| H-3 missing operational envelope | **Resolved** | AD-14 binds configuration, secrets, health, telemetry, container defaults, persistence, and replica count; it fixes single-replica posture until DataProtection, session routing, and cursor durability are verified. AD-13 fixes topology ownership. |
| H-4 FC-CNC contradiction | **Resolved** | AD-12 and canonical API guidance now define one lock scope: `(interactive circuit, AggregateIdentity)`, active until terminal evidence, with unrelated aggregates allowed to proceed. The old “until FC-CNC lands” contradiction is removed. |
| M-1 component-location drift | **Partially resolved; remains M-1 below** | The spine and canonical complete tree now match the brownfield structure, but two historical canonical passages still name obsolete sibling folders. |
| M-2 approval seam | **Substantially resolved / low residual** | Canonical prose names `DomainUiFluentConformanceTests`, the fallback approval record, and story-based guard governance. AD wording remains terse but is enforceable when read with those canonical controls. |

## Canonical Incorporation Audit

The canonical precedence section at `architecture.md:68-178` includes the complete final spine decision set:

- AD-1 through AD-11 retain their source `Binds`, `Prevents`, and `Rule` semantics.
- AD-2 faithfully includes the clarified canonical/compatibility/contextual route model.
- AD-10 faithfully includes deterministic Memories paging, hydration, authorization filtering, degradation, and opaque-cursor behavior.
- AD-12 faithfully includes aggregate-scoped command locking and command-specific projection evidence.
- AD-13 faithfully establishes platform-owned orchestration and transitional treatment of the current repository AppHost.
- AD-14 faithfully establishes production operational and scaling constraints.
- All seven Deferred items are preserved with concrete revisit conditions.
- Current implementation divergences are copied without weakening the governing ADs.

No canonical AD weakens or contradicts its source counterpart. The precedence statement now covers navigation, state, ownership, host, and operations wording.

## Remaining Medium Findings

### M-1 — Two canonical component-location references remain stale

**Checklist:** fixes the real divergence points for the level below; ratifies the brownfield codebase.  
**Evidence:**

- The spine convention and structural seed consistently use `Components/Pages`, `Components/Tenants`, `Components/Users`, and `Components/Shared`.
- The brownfield tree uses `Components/Tenants/Audit` and has no sibling `Components/Audit` or `Components/GlobalAdministrators` folder.
- Canonical Naming Patterns still list `Components/Audit/` (`architecture.md:609`).
- Canonical requirements mapping still places global administration in `Components/GlobalAdministrators/*` (`architecture.md:783`).
- The canonical validation narrative still says audit is homed in `Components/Audit/` (`architecture.md:849`).

**Impact:** A later story agent can cite the historical passages and create a new sibling folder inconsistent with both the canonical complete tree and current code.

**Disposition:** **Autofix in canonical cleanup.** Replace the audit references with `Components/Tenants/Audit`; map global-administrator UI to its actual page/domain-component location; retain the complete tree as the single structural seed.

### M-2 — “Inherited Context” states the target AD-6 transport as current reality

**Checklist:** ratifies rather than contradicts a brownfield codebase.  
**Evidence:**

- `ARCHITECTURE-SPINE.md:42` labels “Direct Tenants REST reads with projection freshness metadata” as inherited context sourced from the current gateway tree.
- `ARCHITECTURE-SPINE.md:247-253` accurately states that `TenantQueryGateway` currently uses `IEventStoreGatewayClient.SubmitQueryAsync`, the generic route normalizes provenance to `Unknown`, and direct REST plus metadata preservation is remediation work.
- The canonical precedence/conformance sections preserve the accurate remediation statement.

**Impact:** The decision itself is clear, but the inherited-fact table and conformance section describe different current states. A brownfield scan should not call an intended target an inherited implementation fact.

**Disposition:** **Autofix in the archived source only if it remains discoverable; canonical clarification preferred.** Relabel the row as “Required direct Tenants REST read contract” or mark it “target; not yet conformant.” This is documentation-state precision, not an AD change.

## Low Residual Notes

- The source Commands convention says “one-at-a-time submission” without repeating the AD-12 lock key. AD-12 is precise and authoritative, so this is not contradictory, but adding “per `(interactive circuit, AggregateIdentity)`” would remove the last shorthand ambiguity.
- AD-3/AD-4/AD-11 use terse phrases such as “documented gaps,” “approved fallback,” and “explicit approved story.” Canonical prose now supplies the relevant conformance allowlist and approval artifact, making these workable; future extraction would be stronger if the ADs linked those controls directly.

## Good-Spine Checklist — Final

| Check | Result | Reason |
| --- | --- | --- |
| Fixes real divergence points; misses none | **Pass** | UI composition, navigation, transport, state, freshness, safety, search, command concurrency, topology ownership, and production scale are fixed. The component-path residue is lower-level stale prose, not a missing decision. |
| Every Rule is enforceable and prevents its divergence | **Pass** | ADs have concrete boundaries, named gateway/state/lock seams, and conformance-test controls. |
| Deferred does not hide uncontrolled divergence | **Pass** | Each item has a revisit condition; AD-13/AD-14 constrain behavior while migration/scaling remain deferred. |
| Named technology verified-current | **Pass** | Exact pins align with current centralized package governance; mutable source SHAs are not architecture invariants. |
| Ratifies brownfield or records intentional change | **Pass with M-2 cleanup** | Existing divergences are named with sequenced remediation; one inherited-context row is mislabeled. |
| Covers the driving spec | **Pass** | FR-1..FR-25 and real NFR-1..NFR-5 are mapped; UX rules remain bound. |
| No inherited invariant weakened | **Pass** | AD-13 now aligns with platform-owned orchestration; no AD weakens the repository boundary. |
| Every owned dimension decided/deferred/open | **Pass** | AD-13 and AD-14 close the previously silent operational/environmental envelope. |

## Final Disposition

The semantic merge is accepted. Archive of the legacy architecture set may proceed while preserving this review with the archived evidence. Track the two medium documentation cleanups in the canonical artifact; keep the three explicitly listed implementation remediations open until code/platform evidence proves conformance.

