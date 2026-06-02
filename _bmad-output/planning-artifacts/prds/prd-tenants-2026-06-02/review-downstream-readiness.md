# Downstream-Readiness Review — Tenants Management UI PRD

**Scope:** Can BMAD's UX, architecture, and epic/story-creation workflows source-extract from this PRD + addendum cleanly?
**Files reviewed:**
- `prd.md`
- `addendum.md`

**Date:** 2026-06-02
**Reviewer role:** Downstream-readiness auditor (source-extraction lens)

---

## Verdict

**Conditionally ready — extractable, but fix the canonical-state-name drift first.** ID continuity is clean (FR/UJ/SM/CP/NFR all contiguous, unique, no gaps or dupes) and almost every inline cross-reference resolves. The structure is downstream-friendly: globally numbered FRs, a single referenced interaction contract (§6), an assumptions index, and a navigational addendum. The dominant risk is **canonical-vocabulary drift between the PRD body and addendum §G** (hyphen vs underscore vs spaced forms of the very state names CP-10 mandates be "used verbatim"). A UX/epic workflow that lifts state names from the PRD body will produce identifiers that do not match the addendum/spec enumeration. Secondary issues: a few unanchored/over-broad cross-references and minor glossary drift. None are blocking on their own, but the state-name drift should be resolved before the truth-state vocabulary is propagated into stories and component contracts.

---

## ID-Continuity Check Table

| Series | Defined range | Count | Contiguous? | Unique? | Gaps | Dupes | Orphan refs (referenced but undefined) |
|---|---|---|---|---|---|---|---|
| **FR** | FR-1 … FR-25 | 25 | Yes | Yes | None | None | None — every `FR-N` reference resolves to a definition |
| **UJ** | UJ-1 … UJ-6 | 6 | Yes | Yes | None | None | None |
| **SM** | SM-1 … SM-5 | 5 | Yes | Yes | None | None | None |
| **SM-C** (counter) | SM-C1 … SM-C3 | 3 | Yes | Yes | None | None | None |
| **CP** | CP-1 … CP-10 | 10 | Yes | Yes | None | None | None |
| **NFR** | NFR-1 … NFR-5 | 5 | Yes | Yes | None | None | None |
| **R** (risks) | R-1 … R-6 | 6 | Yes | Yes | None | None | None |
| **FC** (FrontComposer deps) | FC-TBL, FC-LYT, FC-CMD, FC-CNC, FC-TOK, FC-AUD, FC-CNS, FC-A11Y, FC-L10N, FC-DOC | 10 | n/a (named) | Yes | None | None | None — all 10 appear in both prd.md and addendum §B |
| **ui-NN** (backlog) | ui-01 … ui-15 | 15 | Yes | Yes | None | None | None (FR-22/24/25 correctly flagged as having no `ui-NN` row yet) |

**Cross-reference resolution summary:** All `§N` numeric-section refs (§3.2, §3.3, §4, §5.2, §5.3, §6, §7, §7.2, §7.4, §7.6, §8, §9, §10, §11, §12, §13, §14, §14.2, §15, §16, §17) resolve to real headings. All `addendum §[A–H]` letter-section refs used in prd.md (§B, §D, §G, §H) resolve. Addendum's back-refs to PRD (§1, §4, §4.1, §5.1, §5.3, §5.4, §7.9, §9, §10, §12, §13, §16.1, §16.4, §16.7, §16.12, §2.1, §2.2 [spec-internal]) resolve, with one structural nuance noted below (§16.N sub-numbering). CP-9's `§8 NFR-2 / §10` resolves. `FR-N`, `UJ-N`, `CP-N`, `SM-N`, `NFR-N` inline cross-refs all resolve.

---

## Findings

### Finding 1 — Canonical state-name drift: hyphen (PRD body) vs underscore (addendum §G) vs spaced forms

- **Severity:** High
- **Location:** prd.md §4 Glossary (line 135), §6 CP-3 (line 172), UJ-3 (line 91), FR-12 (line 243) vs addendum §G (line 76); plus FR-12 internal inconsistency
- **Issue:** CP-10 (line 179) and addendum §G header both mandate the canonical state sets be "used verbatim … no per-screen reinterpretation." But the same lifecycle/confirmation states are spelled three different ways:
  - PRD body uses **hyphenated/spaced**: `projection-confirmed` (lines 91, 135, 172, 243), `audit available` (lines 91, 135, 172), `proven (audit available)` (lines 135, 172).
  - Addendum §G uses **underscored** (mirroring the spec): `projection_pending → confirmed | … | audit_pending | audit_available` (line 76).
  - FR-12 (line 243) itself mixes forms: `projection-confirmed → audit-available` (hyphenates "audit-available", which neither the glossary's spaced `audit available` nor the addendum's `audit_available` uses).

  A downstream UX/epic/story workflow extracting state identifiers cannot tell which token is canonical. If it lifts `projection-confirmed` / `audit available` from the PRD body (the human-readable layer it will read first) it will mismatch the addendum/spec enumeration (`confirmed` / `audit_available`) that component contracts and automation selectors must key on (NFR-4). This is the highest-leverage drift because it touches the product's core trust vocabulary (CP-3/CP-10).
- **Suggested fix:** Pick one canonical spelling per state and state explicitly which layer owns the machine token. Recommended: the addendum/spec underscore forms (`projection_pending`, `confirmed`, `audit_pending`, `audit_available`) are the verbatim machine tokens; the PRD body should either use those same tokens in `code font` or add a one-line note in §6/§4 that body prose uses readable labels whose canonical machine names are in addendum §G. At minimum, fix FR-12's stray `audit-available` to match whichever form is chosen. Also reconcile `accepted` ≠ `projection-confirmed` (glossary/CP-3) against the addendum's `accepted → projection_pending → confirmed` so the "non-collapse" triplet maps unambiguously.

---

### Finding 2 — SM-1 validation range over-reaches into a read-only FR

- **Severity:** Medium
- **Location:** prd.md §15, SM-1 (line 396): "Validates FR-10..FR-19."
- **Issue:** SM-1 measures "share of routine tenant/access/lifecycle **operations** performed through the UI rather than raw command-API calls" — i.e. command FRs. The range `FR-10..FR-19` sweeps in **FR-18 (Review global administrators)**, which is explicitly **read-only, MVP** (line 275) and has no command/operation to count. A traceability/test-design workflow that builds an SM→FR matrix from this will assert a non-existent "operation" coverage link for FR-18 (and arguably FR-14 "edit metadata" is a weak fit for "access/lifecycle operations," though defensible). This is a contiguous-range shorthand masking a semantic gap.
- **Suggested fix:** Change SM-1 to an explicit command-only list: "Validates FR-10, FR-11, FR-12, FR-13, FR-14, FR-15, FR-16, FR-17, FR-19" (excluding read-only FR-18), or split the range as "FR-10..FR-17, FR-19." If FR-18 coverage is intended elsewhere, it is already carried by SM-2 (read/answer metrics), which correctly lists FR-18.

---

### Finding 3 — Audit-availability states (`audit delayed`, `missing implementation support`) are undefined in the glossary and drift from the glossary's reason category

- **Severity:** Medium
- **Location:** prd.md FR-23 (line 299) vs §4 Glossary (lines 132, 138–140) and UJ-4 (line 100)
- **Issue:** FR-23 enumerates four audit-availability states a user must "tell apart": `audit pending`, `audit delayed`, `audit unavailable`, `missing implementation support`. Problems for source-extraction:
  1. **None of these four is defined in the Glossary (§4).** They are only said to live in addendum §G's "layered feedback (10 states)" set, which is itself cited-not-listed ("Full set in truth-state §5.1"). A story/UX workflow extracting "what states must FR-23 render" finds no enumerated, verbatim source in the PRD or addendum — it must reach into a spec file the PRD says it should not duplicate.
  2. **`audit delayed`** appears only here; UJ-4 (the journey FR-23 supports) uses only `audit pending` / `audit unavailable` (line 100). The state vocabulary the FR demands is richer than the journey that motivates it — a downstream UJ→FR extraction will under-spec.
  3. **`missing implementation support`** is close to but not identical with the Glossary's Unavailable-Action-Reason category **`unavailable implementation dependency`** (line 132) and FR-9's "unavailable implementation dependency" (line 228). Two near-synonyms for the same concept invite drift.
- **Suggested fix:** Either (a) list the four audit-availability states verbatim in the Glossary (or addendum §G) with their recovery mapping, or (b) explicitly tie FR-23's four states to the named §G layered-feedback subset and to the canonical recovery verbs. Normalize `missing implementation support` to the glossary term `unavailable implementation dependency` (or add a glossary entry making them explicitly the same). Add `audit delayed` to UJ-4's edge cases so the journey and the FR agree.

---

### Finding 4 — Phase-label format drift across §7, §14, and addendum §A; and §A is never cross-referenced from the body

- **Severity:** Medium
- **Location:** prd.md §7 feature headings (lines 185–302), §14 (lines 375–389), addendum §A Phase column (lines 9–17)
- **Issue:** The three places that label phasing use three different vocabularies for the same phases:
  - §7 headings: "MVP", "Phase 2b/2c", "MVP; ui-15 commands — Phase 2c".
  - §14: "Phase 2a (MVP)", "Phase 2b", "Phase 2c" — and equates MVP = Phase 2a (line 375).
  - addendum §A: "2a (MVP)", "2b / 2c", "2a read / 2c cmd", "2c (blocked)", "2c (needs a story)".

  So a feature is "MVP" in §7 but "2a (MVP)" in §14/§A; "Phase 2b/2c" in §7 vs "2b / 2c" in §A. A workflow correlating feature→phase across the two documents must normalize three label styles. Additionally, the **addendum §A is never referenced from the PRD body** (no `§A` appears in prd.md) even though §A is the authoritative feature→phase→backlog→spec map — the body points to §B, §D, §G, §H but not the one section a story-creation workflow most needs as the entry point.
- **Suggested fix:** Standardize on one phase token set everywhere — recommend "Phase 2a (MVP)", "Phase 2b", "Phase 2c" verbatim, and use the same compound form (e.g. "Phase 2b/2c") in both §7 and §A. Add an explicit pointer to addendum §A from §7's lead-in and/or §11 ("Detailed mapping … lives in addendum.md") naming §A as the feature→backlog→phase table.

---

### Finding 5 — Glossary term drift: "global admin" vs "Global administrator"

- **Severity:** Low
- **Location:** prd.md NFR-2 (line 320): "global admin sees all"
- **Issue:** The Glossary defines **"Global administrator"** (line 124) and the navigation area **"Global Administrators"** (line 127). NFR-2 uses the informal short form **"global admin."** The Glossary preamble (line 118) states "FRs, UJs, and SMs use them verbatim" — NFR-2 is technically an NFR, not strictly in that list, but the abbreviation still introduces a synonym a glossary-validation pass will flag, and it weakens the deliberate `global-administrators`-scope-vs-tenant-membership distinction the PRD works hard to preserve (CP-6, FR-18/19).
- **Suggested fix:** Replace "global admin" with "global administrator" in NFR-2 (line 320).

---

### Finding 6 — "pending" / "pending state" used as a column and recovery trigger but not defined as a domain noun

- **Severity:** Low
- **Location:** prd.md FR-1 (line 190): row shows "pending state"; "sorting or paging never hides a pending or stale marker"; CP-8 (line 177): "pending → wait"
- **Issue:** FR-1 makes "pending state" a first-class displayed column and a marker that must never be hidden, and CP-8 makes "pending" a recovery trigger. But "pending" is not a Glossary entry. It is ambiguous against several defined concepts: the command-lifecycle `projection_pending` / `audit_pending` (addendum §G), the "no invitation/**pending** step" non-goal (FR-10/§13), and FR-1's tenant-row context. A downstream column-set extraction for the tenant list cannot tell what data field "pending state" maps to. (Note: §13 explicitly removes "pending-member flows," which makes a bare "pending" column on the tenant list more confusing, not less.)
- **Suggested fix:** Define "pending" (or "pending state") in the Glossary and/or qualify the FR-1 column (e.g. "pending command/operation indicator" or "in-flight change marker") so it is unambiguous and clearly distinct from the removed pending-member concept and from the lifecycle `*_pending` states.

---

### Finding 7 — Addendum's `§16.N` sub-references assume anchors that §16 does not structurally provide

- **Severity:** Low
- **Location:** addendum.md lines 33, 42, 49 ("PRD §16.4", "PRD §16.1", "PRD §16.7"); body lines 33, 36 ("PRD §16.4"); also `PRD §16.12` in §E
- **Issue:** §16 "Open Questions" is a **numbered list (items 1–12)**, not a set of sub-headings (there is no `### 16.1`). The references `§16.1`, `§16.4`, `§16.7`, `§16.12` therefore resolve only by reading "§16, list item N." They do resolve content-wise (item 1 = command route, item 4 = localization ownership, item 7 = cursor durability, item 12 = ID-scheme) and the numbers are correct, so this is not a broken reference — but a strict anchor-resolving extractor (or a doc that gets sharded by heading) may fail to find a `§16.4` target. The PRD body's own references to the same questions use the looser, safe form "(§16)".
- **Suggested fix:** Either keep the precise references but note that §16 items are list-numbered (or render §16 as sub-headings `### 16.1 …`), or relax the addendum references to "(PRD §16, Q4)" / "(PRD §16, item 4)" to survive sharding and anchor resolution.

---

### Finding 8 — A few FRs lean on soft language; mostly bounded, but two spots could tighten

- **Severity:** Low
- **Location:** prd.md FR-20 (line 288), NFR-1 (line 319), feature-NFR (line 315): "without unacceptable degradation" / "without unacceptable latency"; §5.2 (line 153) "without drama"
- **Issue:** The review asked to flag vague "graceful/reasonable/user-friendly" language. The PRD is generally strong here — almost every FR has a concrete testable consequence (NoOp→"already applied", cursor-not-offset, "success only after projection confirmation", "no PII/tokens," distinct named states). The remaining soft spots are the **~500-event target qualified by "without unacceptable degradation/latency"** (FR-20, NFR-1, line 315 feature-NFR). "Unacceptable" is undefined, so the testable threshold is only the count (~500), not a latency bound — a TEA/NFR-audit workflow cannot derive a pass/fail latency number. (§5.2 "without drama" is tone guidance, not a requirement, so acceptable.)
- **Suggested fix:** Attach a concrete latency budget to the ~500-event target (e.g. "renders within N ms / interactive within N ms at ~500 events") or explicitly mark the number as `[ASSUMPTION]` pending a product/perf figure (consistent with how freshness thresholds are deferred in §16 item 10). No change needed to the tone language in §5.2.

---

## Positive Confirmations (no action needed)

- **ID continuity:** FR-1..25, UJ-1..6, SM-1..5, SM-C1..3, CP-1..10, NFR-1..5, R-1..6 are all contiguous, unique, gap-free, dupe-free. No dangling references.
- **Assumptions Index roundtrip (§17):** Every substantive inline `[ASSUMPTION]` maps to a §17 entry and vice-versa:
  - §3.2 integrators → §17 ✔; §3.3/§7.4/§13 direct-add (3 inline tags, lines 67/114/363) → §17 ✔; §5.2 Fluent roles → §17 ✔; §5.3 mobile read-only → §17 ✔; §7.2/FR-6 sensitive config (2 inline, lines 213/369) → §17 ✔; §7.6/FR-16 preview scope → §17 ✔; §9 WCAG/RTL/L10n-ownership → §17 ✔; §14.2 audit-area → §17 ✔; §15 metric targets → §17 ✔; "General MVP=read-only" → §17 ✔.
  - The two non-substantive `[ASSUMPTION]` mentions (line 26 explaining the convention; line 393 stating SM targets are assumptions, which maps to the §15 entry) are correctly not separate index items.
- **`[NOTE FOR PM]` (2):** FR-12 (line 244, FrontComposer blockers) and §7.9 (line 305, FR-22/24/25 lack a `ui-NN` row). Both are self-contained and consistent with addendum §A's "no backlog row yet" note (line 17) and §B. No floating notes; no orphaned index expectation (the PRD does not promise a separate NOTE index).
- **UJs each have a named protagonist with inline context:** UJ-1/2/3 Elena, UJ-4 Sofia, UJ-5 Nadia, UJ-6 Elena — each carries Persona+context, Entry state, Path, Climax, Resolution, Edge case inline. No floating UJs. Every UJ is claimed by ≥1 FR's "Realizes UJ-N," and every UJ-N referenced by FRs exists.
- **CP-9 cross-ref:** `§8 NFR-2` (NFR-2 lives in §8) and `§10` both resolve; §10's privacy/authorization-scope content supports the reference (minor: §10 is titled "Guardrails — Privacy & Support-Safety," so the authorization-enforcement point is primarily in NFR-2, but the ref is defensible).
- **Glossary coverage of major nouns:** Tenant, Member, Role, Owner/Owner count, Global administrator, Tenant lifecycle status, Configuration, Operations Shell, Projection, Truth State Badge, Freshness, Freshness Gate, Unavailable Action Reason, Consequence Preview, Command Lifecycle Panel, Non-collapse invariant, Fail-closed, Compensating command, Audit Evidence Receipt, AuditEventCategory, Support-safe reference, Orphan membership, NoOp, Rejection, Proposed fallback — all defined and used consistently (subject to the drift items in Findings 1, 3, 5, 6).
- **Section standalone-readability:** Each numbered section is self-describing; §6 (contract) and §4 (glossary) are referenced by ID rather than repeated, which is exactly the pattern downstream extraction wants. Addendum sections A–H each stand alone as a navigational bridge.
- **`proven (audit available)`** is used consistently in both the Glossary (line 135) and CP-3 (line 172) — internally consistent within the body (the cross-document drift to `audit_available` is captured in Finding 1).

---

## Recommended Fix Order (highest downstream leverage first)

1. **Finding 1** — Resolve canonical state-name drift (hyphen/underscore/spaced) and declare the verbatim machine tokens. (Blocks clean truth-state extraction into stories/components.)
2. **Finding 3** — Define/normalize the four audit-availability states and the `unavailable implementation dependency` synonym.
3. **Finding 4** — Standardize phase labels across §7/§14/§A and add a body pointer to addendum §A.
4. **Finding 2** — Tighten SM-1's FR range to command-only FRs.
5. **Findings 5, 6, 7, 8** — Glossary "global admin" fix, define "pending," relax/clarify §16.N anchors, attach a latency budget to the ~500-event target.
