# Input Reconciliation — Audit Evidence & Compensating Recovery

**Source spec:** `docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md`
**Targets:** PRD `prd.md` (features 7.8/7.9, FR-20..FR-25) + `addendum.md`
**Date:** 2026-06-02
**Scope of check:** audit-evidence model (receipt fields, NarrativePayload, AuditEventCategory, projection marker, pending/delayed/unavailable states) and compensating-recovery model (forward-only, preview-against-current-state, linked records, last-owner-allowed-by-design, empty-tenant bootstrap).

---

## What is already covered adequately (NOT flagged below)

For reviewer confidence, the following spec elements are preserved well enough in the PRD and are deliberately **not** listed as gaps: the 8-field receipt content set (FR-22, glossary), the four audit-availability states with stated recovery (FR-23), the five audit entry points each carrying scope (FR-21, §5.1), the flat DataGrid fallback with loading/empty/filtered-empty/error states and the "approved fallback / absent timeline" framing (FR-20, R-1, glossary "Approved fallback"), the cursor-only / ~500-event target and `GET /api/tenants/{tenantId}/audit` → `GetTenantAuditQuery` sourcing for the audit *list* (FR-20, NFR-1, feature NFR, addendum §C), the non-collapse invariant and partial-completion `audit pending` (CP-3, FR-22), SignalR-nudge-only (CP-4, addendum §D), forward-only "never undo" (CP-7, FR-24, glossary), preview-against-current-state and linked records (FR-25, UJ-4), last-owner-allowed-by-design / reduce-to-zero-owners (CP-6, UJ-3, FR-12, glossary), grouped audit mode deferred fast-follow (§13 Non-Goals), and the "no new backend endpoints" boundary at the *generic* level (§11).

---

## Gap 1 — `NarrativePayload` model dropped (not named; not characterized; Target resolution rule lost)

**Spec location:** Guardrail 1 (lines 30), §3.2 receipt-field source table (lines 114–125), §8 (line 268). Contract: `src/Hexalith.Tenants.Contracts/Queries/TenantAuditEntry.cs`.

**Severity:** High

The spec repeatedly insists `NarrativePayload` is a **structured, support-safe narrative — NOT the raw persisted event payload**, and defines the receipt **Target** as resolved from `NarrativePayload` `userId` → `key` → falling back to `TenantId` (matching `TenantAuditEntry.ResolveTarget()`). The PRD only says receipts "use a structured support-safe narrative, never the raw event payload" (§10, line 329) — it never names `NarrativePayload`, never states the not-raw-payload distinction as a sourcing rule for the receipt, and entirely omits the Target-resolution fallback chain. The receipt-field → source mapping (which field comes from which `TenantAuditEntry` member) is not in the PRD (it is the addendum's job, but the addendum lacks it too).

**Suggested PRD fix:** In `addendum.md` add a receipt-field → `TenantAuditEntry` source row table (actor=`ActorId`, target=resolved `NarrativePayload.userId`→`key`→`TenantId`, scope=`TenantId`, outcome=`EventType`+`Category`, timestamp=`Timestamp`, audit ref=`EventId`) and state explicitly that `NarrativePayload` is a structured support-safe narrative, not the raw event payload.

---

## Gap 2 — `AuditEventCategory` values (`Access` / `Administrative`) never named

**Spec location:** §2.1 filters (line 74), §3.1/§3.2 outcome field (lines 119, 124). Contract: `src/Hexalith.Tenants.Contracts/Enums/AuditEventCategory.cs`.

**Severity:** Medium

The spec mandates the flat audit DataGrid's type filter be exactly the `AuditEventCategory` enum — `Access` / `Administrative` — and includes `Category` in the receipt outcome. The PRD says only "date and category filters" (FR-20, line 281) and never enumerates the two values, so a downstream UX/dev reader cannot know the category vocabulary is a fixed two-value backend enum (not free text or an open set).

**Suggested PRD fix:** In FR-20 (or addendum §A/§C) name the two filter categories `Access` and `Administrative` from `AuditEventCategory`, and note `Category` is part of the receipt outcome.

---

## Gap 3 — Empty-tenant bootstrap boundary (`HasMembershipHistory == false`) entirely absent

**Spec location:** Guardrail 2 (line 31), §5.3 (line 190), §8 (line 272). Source: `_bmad-output/project-context.md#Aggregates`, `#Domain Correctness`.

**Severity:** High

The spec names the empty-tenant bootstrap path as the **relevant boundary for the restore-after-last-owner-removal narrative**: `AddUserToTenant` skips owner-only RBAC when `state.HasMembershipHistory == false`, which is precisely what makes "restore intended access" possible after the last owner was (legitimately) removed. The PRD covers last-owner-allowed-by-design (CP-6) but the recovery FRs (FR-24/25) and UJ-4 never explain *how* a restore succeeds when there are zero owners. Without the bootstrap boundary, the forward-recovery story for the last-owner case is ungrounded, and a reader could wrongly assume restore is blocked (re-introducing the "≥1 owner" invariant the spec explicitly forbids inventing).

**Suggested PRD fix:** In FR-24 consequences (or addendum §D) note that restore-after-last-owner-removal relies on the empty-tenant bootstrap path — `AddUserToTenant` skips owner-only RBAC when the tenant has no membership history (`HasMembershipHistory == false`) — and that no "≥1 owner" backend invariant exists.

---

## Gap 4 — "No new *receipt/consequence* endpoint" sourcing rule not bound to the receipt

**Spec location:** Guardrail 1 (line 30), §3.2 (lines 110–125), §8 (line 267).

**Severity:** Medium

The spec's specific guardrail is stronger than the PRD's generic "does not add backend endpoints" (§11): it says **do NOT add a backend "receipt" or "consequence" endpoint** — the receipt composes from the *existing* audit read model plus the *client-tracked* `FC-CMD` command lifecycle, and the support-safe command reference is **client-side**, not server-produced. FR-22 and the addendum never state where the receipt is assembled, so a downstream architect could reasonably propose a "receipt" or "consequence" endpoint — the exact mistake the spec calls out.

**Suggested PRD fix:** In addendum §C/§D state that the Audit Evidence Receipt is composed client-side from the existing audit read model + the client-tracked `FC-CMD` lifecycle, and that no `receipt`/`consequence` backend endpoint is to be added.

---

## Gap 5 — "Projection marker" not tied to the read-model freshness primitive (ETag → 304)

**Spec location:** §3.2 source table (line 122), §6.4 item 2 (line 231), §8 (line 267).

**Severity:** Low

The receipt's **projection marker** is defined by the spec as the read-model freshness marker — timestamp / projection version / **ETag → `304`** served by `CachingProjectionActor`. The PRD lists "projection marker" as a receipt field (FR-22, glossary) and separately describes the ETag/304 freshness primitive (addendum §D), but never connects the two: the receipt's projection marker IS that freshness marker. A reader may treat the receipt projection marker as a distinct/new datum.

**Suggested PRD fix:** In addendum §D (or the receipt source table from Gap 1) note the receipt's projection marker is the same read-model freshness marker (ETag → 304 via the caching projection actor), not a separate value.

---

## Gap 6 — Distinct `tenants` vs `global-administrators` domain rule missing from *recovery* copy

**Spec location:** Guardrail 3 (line 32), §5.4 (lines 192–194), §8 (line 271).

**Severity:** Medium

The PRD keeps the two scopes distinct for *governance review* (FR-18/19, glossary "Global administrator", CP-6) but the **recovery** FRs do not carry the spec's §5.4 rule: a compensating correction of global-administrator authority is a **separate command on the singleton `global-administrators` domain** (`SetGlobalAdministrator` / `RemoveGlobalAdministrator`), and **does not edit a tenant aggregate**. FR-24/25 only reference `AddUserToTenant`/role change, so recovery copy could conflate a global-admin authority correction with a tenant-membership correction.

**Suggested PRD fix:** In FR-24 consequences (or addendum) state that a global-administrator correction is a separate `global-administrators`-domain command (`SetGlobalAdministrator`/`RemoveGlobalAdministrator`) that does not edit a tenant aggregate, kept distinct from tenant-membership corrections.

---

## Gap 7 — "Reassign tenant owner" recovery path omitted from the recovery flow

**Spec location:** §5.2 useful recovery paths (line 186), flow diagram (lines 172–184).

**Severity:** Low

The spec lists the concrete recovery paths from audit-evidence detail: **reassign tenant owner**, restore intended access (new `AddUserToTenant`), retry access removal, open audit evidence, escalate with a support-safe reference. The PRD FR-24/UJ-4 capture "restore intended access" / "start correction" and escalate, but omit **reassign tenant owner** and **retry access removal** as named recovery paths.

**Suggested PRD fix:** In FR-24 consequences list the named compensating paths including "reassign tenant owner" and "retry access removal", not only "restore intended access".

---

## Gap 8 — Receipt copy-safe token allow-list narrower in spec than PRD blanket "narrative" statement

**Spec location:** §3.3 (lines 128–129), §7.1 (lines 250–251), §8 (line 270).

**Severity:** Low

Beyond the deny-list the PRD already has (§10), the spec gives a **positive allow-list** for copyable reference content: support-safe command reference, tenant/user reference, projection version/freshness marker, accepted timestamp, and audit event reference **or fallback state**. The PRD enumerates the deny-list well but does not state the closed positive allow-list (especially "audit event reference *or fallback state*", which matters when audit is unavailable/missing-support).

**Suggested PRD fix:** In §10 (or addendum) add the positive allow-list of support-safe reference tokens, including "audit event reference or fallback state" for the unavailable/missing-support cases.

---

## Summary table

| # | Gap | Severity | Spec ref |
|---|-----|----------|----------|
| 1 | `NarrativePayload` model + Target resolution rule dropped | High | Guardrail 1, §3.2, §8 |
| 2 | `AuditEventCategory` values (`Access`/`Administrative`) unnamed | Medium | §2.1, §3.1/§3.2 |
| 3 | Empty-tenant bootstrap (`HasMembershipHistory == false`) absent | High | Guardrail 2, §5.3, §8 |
| 4 | "No new receipt/consequence endpoint" not bound to receipt | Medium | Guardrail 1, §3.2, §8 |
| 5 | Projection marker not tied to ETag→304 freshness primitive | Low | §3.2, §6.4, §8 |
| 6 | tenants vs global-administrators distinction missing in recovery copy | Medium | Guardrail 3, §5.4, §8 |
| 7 | "Reassign tenant owner" / "retry access removal" recovery paths omitted | Low | §5.2 |
| 8 | Positive copy-safe token allow-list (incl. fallback state) not stated | Low | §3.3, §7.1, §8 |

**No misrepresentations found** in what the PRD does cover: last-owner-allowed-by-design, forward-only/never-undo, preview-against-current-state, linked records, four audit states, and the five entry points are all faithful to the spec.
