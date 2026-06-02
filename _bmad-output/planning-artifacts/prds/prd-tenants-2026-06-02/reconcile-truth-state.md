# Input Reconciliation — Truth State / Action-Availability Spec vs PRD + Addendum

**Source spec:** `docs/tenants-ui-truth-state-and-action-availability-spec.md` (the canonical truth/feedback contract, Story 9.3)
**Reconciled against:** `prd-tenants-2026-06-02/prd.md` (esp. §4 Glossary, §6 CP-1..CP-9, §7 FRs, §9) + `addendum.md`
**Date:** 2026-06-02

Scope of this report: only what the spec contains that the PRD + addendum **missed, dropped, weakened, or misrepresented**. Items the PRD already preserves faithfully (the four fail-closed conditions in CP-2; the five-dimension model in CP-1; the 5 freshness state names in the Glossary; non-collapse `accepted ≠ confirmed ≠ proven` in CP-3; SignalR-nudge-not-proof in CP-4; "undo" ban / correct-forward in CP-7; the `If-None-Match`→`304` freshness primitive and SignalR rationale in addendum D; NoOp/"already applied"; support-safe reference rules in §10) are not listed.

---

## GAP 1 — The canonical 13-state Truth State Badge set is never enumerated; the "never reinterpret per screen" mandate is lost

**Spec location:** §2.1 (the AC1 badge states are *exactly* these thirteen: current, refreshing, aging, stale, unknown, eligible, blocked, pending, accepted, confirmed, failed, audit pending, audit available) and §2.2 (grouping by dimension so screens cannot reinterpret "current"/"accepted"/"confirmed"/"audited"); AC1 traceability row in §11.

**What the PRD does:** §4 Glossary defines "Truth State Badge" conceptually ("combining freshness, authorization, command lifecycle, projection confirmation, and audit dimensions") but never enumerates the 13 canonical states and never carries the spec's core purpose — that this fixed vocabulary exists *so that meaning is not re-invented per screen*. CP-1 names the 5 dimensions but not the badge's enumerated state set.

**Severity:** High — this is the spec's central contract; without the enumerated, dimension-grouped set the PRD cannot enforce cross-screen consistency, which is the spec's whole reason to exist.

**Suggested PRD fix:** In §6 (or a Glossary sub-note) add the 13-state canonical Truth State Badge set grouped by the 5 dimensions, with the rule "these states are never reinterpreted or re-sorted per screen," referencing spec §2.

---

## GAP 2 — The rich 10-state command-lifecycle vocabulary is collapsed; `previewed`, `duplicate`, `timeout`, `unknown`, `eligible` (as lifecycle states) are dropped

**Spec location:** §1 (Command lifecycle dimension: "Distinguish `eligible`, `previewed`, `submitted`, `accepted`, `rejected`, `already applied`, `failed`, `duplicate`, `timeout`, and `unknown`"); §2.2 closing note ("the richer command-lifecycle vocabulary ... must not collapse"); §5.3 worked model (`unknown` = status lookup/SignalR/projection confirmation unavailable, distinct from `failed`).

**What the PRD does:** Command lifecycle is reduced to `submitted → accepted → projection-confirmed` plus `rejected`/`failed` (FR-11, FR-12, FR-13, CP-3). The terms `duplicate`, `timeout`, and `previewed` appear nowhere in PRD or addendum. `unknown` as a *command-lifecycle/confirmation* state (distinct from a `failed` outcome and from `unknown` *freshness*) is not preserved — yet the spec treats "confirmation became unknown" as a first-class state demanding "avoid success language" + retry/escalate.

**Severity:** High — `duplicate`/`timeout`/`unknown` are exactly the at-least-once-delivery / projection-lag cases the product's trust proposition depends on; merging them into "failed" risks false-failure or false-success messaging.

**Suggested PRD fix:** Add a CP or FR-12 consequence enumerating the full command-lifecycle vocabulary (eligible, previewed, submitted, accepted, rejected, already applied, failed, duplicate, timeout, unknown) and require `unknown`/`duplicate`/`timeout` be handled distinctly from `rejected`/`failed`, citing spec §1 and §5.3.

---

## GAP 3 — The 10-state layered feedback set is not enumerated; `degraded` and `unable to verify` are not preserved as distinct states

**Spec location:** §5.1 (AC4 feedback states, distinct and not to be merged: request sent (submitted), accepted, projection pending, confirmed, rejected, already applied, degraded, audit pending, audit available, unable to verify); AC4 traceability row in §11.

**What the PRD does:** CP-3 covers the accepted/confirmed/proven non-collapse and CP-8 lists recovery mappings, but the enumerated 10-state layered feedback set is absent. Critically, **`degraded`** ("a capability is unavailable; explain what is unavailable and what still works") appears only once in the §9 localization copy list, never as a feedback state; and **`unable to verify`** (status lookup / SignalR / projection confirmation unavailable; avoid success language) appears nowhere. The PRD's audit-only `audit pending/delayed/unavailable` (FR-23) does not substitute for the command-level `degraded` / `unable to verify` states.

**Severity:** High — `unable to verify` is the spec's explicit guard against showing success when confirmation cannot be obtained; dropping it as a named state weakens CP-3/CP-4 in practice.

**Suggested PRD fix:** Add the enumerated 10-state layered feedback set (referencing spec §5.1) to §6 or FR-12, explicitly naming `degraded` and `unable to verify` as distinct, success-language-prohibited states.

---

## GAP 4 — The 6 Unavailable Action Reason categories are collapsed to 4; the reason→evidence-source mapping (FC-CMD/FC-CNS/FC-AUD ties) is dropped

**Spec location:** §4.1 (reason categories are *exactly* these six: missing permission, stale data, missing lifecycle support, missing consequence preview, missing audit proof, high-impact flow not ready); §4.4 (reason → evidence source → dependency-tie mapping table).

**What the PRD does:** FR-9 and the Glossary use four buckets — "missing permission vs. stale data vs. blocked risk vs. unavailable implementation dependency." This matches spec §4.3's *distinctness grouping*, but **collapses the three dependency categories** (missing lifecycle support / missing consequence preview / missing audit proof) into one "unavailable implementation dependency," and the PRD provides **no reason→evidence-source mapping** (the FC-CMD / FC-CNS / FC-AUD ties from §4.4). A user/operator cannot tell *which* dependency is missing.

**Severity:** Medium — the 4-way safety distinction is preserved, but the spec's finer 6-category vocabulary and its evidence-source traceability (needed to drive readiness/`blockedBy`) are weakened.

**Suggested PRD fix:** In FR-9 consequences (and/or §6 CP-2), enumerate the 6 reason categories and note that the dependency reasons further resolve to missing lifecycle / consequence / audit support, referencing spec §4.1 and §4.4.

---

## GAP 5 — The `aging` freshness state is functionally orphaned: its distinct "usable-but-friction" behavior is not preserved

**Spec location:** §2.2 (`aging` = "Projection may still be usable, but action friction may be needed"; `stale` = "Action is blocked or requires refresh" — two distinct behaviors); §1 freshness row.

**What the PRD does:** §4 Glossary lists `aging` as one of the five freshness values, but the Freshness Gate Glossary entry and CP-2 define gating only as "blocks ... when freshness is `stale`/`unknown` (fail-closed)." `aging` therefore appears as a label with **no defined behavior** — the spec's intermediate "still usable, add friction" semantics are dropped, effectively merging `aging` into either `current` or `stale`.

**Severity:** Medium — loses a deliberate middle gating tier the spec defines for access-impacting actions.

**Suggested PRD fix:** In the Freshness Gate Glossary entry / CP-2, state that `aging` permits the action with added friction (distinct from `stale` block and `current` pass), per spec §2.2.

---

## GAP 6 — Audit `approved fallback` is dropped as an audit-evidence state, and the PRD substitutes a non-spec term

**Spec location:** §1 (Audit evidence dimension: show "`audit pending`, `audit available`, `delayed`, `unavailable`, or `approved fallback`") and §7.3 heading (audit-evidence states: pending / available / delayed / unavailable / approved fallback).

**What the PRD does:** FR-23 enumerates "`audit pending`, `audit delayed`, `audit unavailable`, and `missing implementation support`." This **drops the spec's `approved fallback` audit-evidence state** and introduces "missing implementation support," which is an Unavailable-Action-Reason concept, not one of the spec's five audit-evidence states. (The PRD does define "Approved fallback" generically in the Glossary and FR-20, but not as an *audit-evidence display state* per §1.)

**Severity:** Medium — misrepresents the audit-evidence vocabulary; a UX/dev consumer following the PRD would build the wrong audit state set.

**Suggested PRD fix:** Align FR-23 to the spec's five audit-evidence states (audit pending, audit available, delayed, unavailable, approved fallback), citing spec §1 / §7.3.

---

## GAP 7 — Recovery vocabulary is incomplete: "inspect audit" and "continue read-only" recovery verbs are missing from CP-8

**Spec location:** §5.4 (each concurrency/recovery case names a concrete recovery from the closed verb set: refresh, wait, retry status lookup, **inspect audit**, **continue read-only**, request permission, start a compensating command, escalate) and the closing rule ("use 'start correction', 'restore intended access', 'retry status lookup', 'inspect audit', or 'escalate'").

**What the PRD does:** CP-8 lists: stale→refresh; pending→wait; status-lookup failure→retry; missing permission→request access; wrong change→start correction; unverifiable→escalate. It omits **"inspect audit"** and **"continue read-only"** as recovery actions — both load-bearing in the spec (e.g. "already applied" → inspect audit; continue read-only; and the "continue read-only while command flows aren't ready" posture).

**Severity:** Medium — narrows the spec's recovery taxonomy; "never dead-end" is asserted but two valid exits are missing.

**Suggested PRD fix:** Add "inspect audit" and "continue read-only" to CP-8's recovery vocabulary, referencing spec §5.4.

---

## GAP 8 — §6.2 "global message bars reserved" is not in the PRD's interaction contract

**Spec location:** §6.2 ("Global message bars (`FluentMessageBar`) are reserved for page-level degradation or system-wide service state only ... not used for row-level or command-level feedback"); AC5 traceability row in §11.

**What the PRD does:** §5.1 captures the proximity half ("Command lifecycle is never a primary navigation area ... shown inline, anchored to the affected row/panel"), but the **reservation rule for global message bars** (page-level / system-wide only) is absent from §6 and from FRs. AC5 in the spec is two-pronged (proximity *and* global-bar reservation); the PRD preserves only one prong.

**Severity:** Low — placement guidance, but a named spec rule the PRD silently drops.

**Suggested PRD fix:** Add a CP (or extend §5.1) stating global message bars are reserved for page-level/system-wide degradation only, never row/command feedback, per spec §6.2.

---

## GAP 9 — §6.3 distinction "delayed evidence vs not-yet-built implementation support" is not stated at contract level

**Spec location:** §6.3 ("Distinguish delayed evidence from missing implementation support (audit unavailable: delayed vs not-yet-built)").

**What the PRD does:** FR-23 lists `audit delayed`, `audit unavailable`, and `missing implementation support` as separate badge values, which partially encodes the distinction, but the PRD never states the *rule* that delayed-but-coming evidence must be presented differently from not-yet-built capability (and §6.3 also governs non-audit degraded surfaces generally). The cross-cutting principle is weakened to a single FR's state list.

**Severity:** Low — partially covered via FR-23 states; the generalized rule is missing.

**Suggested PRD fix:** Note in §6 / NFR-3 that the UI must distinguish delayed evidence from not-yet-built implementation support across degraded surfaces, per spec §6.3.

---

## GAP 10 — §4.2 "tooltip MAY supplement, cannot be the only explanation" is reduced to "hover-free"

**Spec location:** §4.2 ("the UI exposes a visible inline reason. A tooltip may supplement the inline reason but cannot be the only explanation").

**What the PRD does:** FR-9 / Glossary say the Unavailable Action Reason is "hover-free" / "inline-visible (not hover-only)." This captures the prohibition but **drops the explicit allowance** that a tooltip *may supplement*. Minor, but the spec deliberately permits tooltips as a secondary layer; "hover-free" could be over-read as banning tooltips entirely.

**Severity:** Low — near-equivalent; slight risk of over-restriction.

**Suggested PRD fix:** Reword FR-9 to "reason is inline-visible; a tooltip may supplement but is never the sole explanation," per spec §4.2.

---

## Summary table

| # | Gap | Spec ref | Severity |
|---|-----|----------|----------|
| 1 | 13-state canonical badge set + "no per-screen reinterpretation" not enumerated | §2.1, §2.2 | High |
| 2 | 10-state command-lifecycle vocabulary collapsed (`previewed`/`duplicate`/`timeout`/`unknown` dropped) | §1, §2.2, §5.3 | High |
| 3 | 10-state layered feedback set not enumerated; `degraded` + `unable to verify` not preserved | §5.1 | High |
| 4 | 6 Unavailable-Action-Reason categories collapsed to 4; reason→evidence mapping dropped | §4.1, §4.4 | Medium |
| 5 | `aging` freshness state orphaned (distinct friction behavior lost) | §2.2 | Medium |
| 6 | Audit `approved fallback` state dropped; "missing implementation support" substituted | §1, §7.3 | Medium |
| 7 | Recovery verbs "inspect audit" + "continue read-only" missing from CP-8 | §5.4 | Medium |
| 8 | Global-message-bar reservation rule absent from contract | §6.2 | Low |
| 9 | Delayed-vs-not-yet-built distinction not stated as a rule | §6.3 | Low |
| 10 | Tooltip-may-supplement allowance reduced to "hover-free" | §4.2 | Low |
