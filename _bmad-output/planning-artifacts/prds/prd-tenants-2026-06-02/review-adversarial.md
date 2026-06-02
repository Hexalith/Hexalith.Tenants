# Adversarial Review — Tenants Management UI PRD (2026-06-02)

Reviewer stance: cynical, adversarial. The job is to find what is weak, hand-wavy, over-claimed, internally inconsistent, or theatrical — and to say so plainly, with quotes. This PRD is unusually well-written prose, which is precisely the problem: the writing is good enough to disguise that **nothing in it is buildable today**, that its headline claims are contradicted by its own appendix and source backlog, and that half its surface area is furniture. Findings below, sorted by severity.

---

## Finding 1 — CRITICAL — The "green-light to build" MVP is not buildable; even Phase 2a is blocked top to bottom

**Location:** §14.1 "In scope (MVP — Phase 2a)", §14.1 Dependency note, §2 "Why Now", contradicted by `docs/tenants-ui-phase-2-story-backlog.md` lines 62, 68–73.

**The attack.** The PRD frames an MVP — "the **read-only foundation only**" — as the thing you build now. But the authoritative backlog this PRD claims to sit above states flatly:

> "There are no `ready` or `ready-with-approved-fallback` rows in the current evidence set because no row has `blockedBy: []`, and no fallback has been recorded as approved." (backlog line 62)

Every single MVP read story — `ui-01` through `ui-06`, the *entire* Phase 2a scope — carries `readiness: planning-only` and `blockedBy: [FC-LYT, FC-A11Y, FC-L10N, FC-DOC]` (or `+FC-TOK`). The PRD half-admits this in one buried sentence:

> "even this read-only MVP depends on the shell/layout contract (`FC-LYT`, needs-confirmation) being resolved; only `FC-TBL` is available today."

That is not a footnote — that is the whole story. **`FC-LYT` is `needs-confirmation`, and it gates `ui-01` through `ui-15`** (backlog line 127). So the MVP is not "the safe, tractable, do-it-now slice." It is blocked on the exact same unresolved layout contract as the scariest Phase 2c platform-wide command. The document's structure (a confident §14.1 "In scope" list, a calm phasing arrow in §14.3) sells a buildable foundation; the dependency reality is that **2a is as blocked as 2c on its critical-path dependency.** The thesis "only the human surface is missing" (§2) is false at the level that matters: the surface can't be composed because the shell contract it composes into is unconfirmed, and three cross-cutting gates (`FC-A11Y`, `FC-L10N`, `FC-DOC`) are all `needs-confirmation` for every read row.

This is the central deception of the PRD. It reads as a green-light; it is a list of things that cannot start.

**Suggested fix.** Demote §14.1 from "In scope (build now)" to "Candidate first slice (currently blocked)." State in the *first sentence of §14* that **zero stories are implementation-ready**, that `FC-LYT` is on the critical path for 100% of the backlog, and that the gating decision (§16.3) must close before any story — including 2a — leaves planning. Stop letting "read-only" imply "unblocked"; they are unrelated.

---

## Finding 2 — CRITICAL — "Phase 1 backend is complete" is over-claimed and contradicted by the PRD's own deferred-epic admissions and the backlog's evidence keys

**Location:** §2 "Why Now" first bullet; contradicted by §12 R-3, §16.7, addendum §D, and backlog backendEvidence keys `post-epic-5-r5a2`, `post-epic-5-r5a3`.

**The attack.** §2 asserts:

> "The **Phase 1 backend is complete** — tenant/member/role/configuration/lifecycle commands, queries, cursor pagination, authorization, projection safety, and production JWT configuration all exist and are tested. The capability is real; only the human surface is missing."

"Cursor pagination" is explicitly listed as complete. Yet R-3 says:

> "**Query cursor durability across replicas** is deferred (a separate backend epic); the UI must not assume cursors survive restarts/replica changes yet."

So the pagination the read MVP (FR-1, FR-20) depends on is *not* production-durable — it is a deferred backend epic, and §16.7 lists "what is the UI's expected behavior on cursor invalidation?" as an open question with no answer. The backlog corroborates that "complete" is aspirational: read stories cite backend evidence keys named `post-epic-5-r5a2-get-user-tenants-scoped-authorization` and `post-epic-5-r5a3-tenant-audit-projection-query` — *post-epic* remediation work, i.e. things bolted on after the epic that the "complete" claim refers to. The audit query (FR-20) literally depends on `post-epic-5-r5a3`. You cannot call a backend "complete and tested" in the vision and then reveal in Risks that a load-bearing piece (durable cursors) is a separate unshipped epic with undecided UI behavior.

**Suggested fix.** Soften §2 to "Phase 1 command/query/auth surface exists and is tested; **cursor durability across replicas is a separate, not-yet-shipped backend epic** (R-3) and the audit/scoped-lookup queries rest on post-epic remediation (`r5a2`/`r5a3`)." Then answer §16.7 before claiming the read MVP is fundable, because list and audit paging are core to it.

---

## Finding 3 — HIGH — "Both audiences, role-scoped" is asserted as a headline but is bolted on, with no owner-scoped command journeys or FRs of its own

**Location:** §1 Vision ("A single role-scoped application serves two audiences"), §3 Target Users; contradicted by the FR/UJ inventory. Owner-specific surface = UJ-5 + FR-3 only.

**The attack.** The Vision's distinctive selling point is dual-audience symmetry:

> "A single role-scoped application serves two audiences from the same surfaces: **platform operators / global administrators** … and **tenant owners**, who manage only their own tenant."

Count the actual owner-owned product surface. There is exactly **one** owner journey (UJ-5, Nadia) and exactly **one** owner-first FR (FR-3 "Self-audit My Tenants"). Every other journey is operator-driven: UJ-1, UJ-2, UJ-3, UJ-4, UJ-6 are Elena/Sofia. (Mentions in the PRD body: Elena/Sofia = 10, Nadia = 5, several of Nadia's being cross-references in operator FRs.) Worse, UJ-5's only *write* capability — "(later phase) changes a teammate's role" — maps to FR-11, which is written generically as "**An authorized user** can change a member's role" with no owner-specific consequences, no owner-specific last-owner UX, no statement of how an owner's scoped view differs from an operator's beyond "she never sees other tenants." There is no owner add/remove journey distinct from the operator's, no owner audit journey (UJ-4 is Sofia, an operator), no owner recovery story. The owner is a persona with one read screen and a borrowed write verb.

So "both audiences" is real only in the trivial sense that authorization scoping (NFR-2) hides rows. That is not "serves two audiences from the same surfaces" as a *product* claim — it is one product for operators with a filtered view exposed to owners. There is no owner-scoped owner of any of these journeys.

**Suggested fix.** Either (a) honestly reframe: "primarily an operator console; tenant owners get an authorization-scoped read view plus self-service role change," dropping the symmetric-dual-audience framing; or (b) commit owner-scoped FRs and at least one owner-owned command journey (owner adds/removes their own members, owner reads their own audit) with owner-specific consequence/last-owner copy, and give SM-3 more than FR-3+FR-11 to validate.

---

## Finding 4 — HIGH — The truth-state safety model is asserted everywhere but made testable almost nowhere; the canonical state sets are not even in the PRD

**Location:** §6 CP-1..CP-10, §4 Glossary (Truth State Badge, Non-collapse invariant), addendum §G; cross-ref §9 acceptance scenarios.

**The attack.** The product's entire claimed distinctiveness is "honesty about state" (§1) and the non-collapse invariant (CP-3): the UI "**never shows success it has not confirmed against the source-of-truth projection.**" Strong words. But where is the testable definition?

- The "**full canonical state set**" of the Truth State Badge "is defined once (see §6 and addendum §G)." Go to addendum §G and the **13-state set is not listed** — it says: "the complete 13-state enumeration is defined in **truth-state §2.1–2.2** and must be used verbatim." So the PRD that claims verbatim-everywhere discipline *does not contain the vocabulary* and points to a spec. CP-10 demands "no per-screen reinterpretation" of an enumeration the PRD itself declines to enumerate.
- The Unavailable Action Reason set is "(6)" but §4 and addendum §G both punt: "(+ the sixth per spec)." The PRD cannot name its own sixth reason category.
- The command lifecycle is given as "(illustrative, RemoveUserFromTenant)" with "the full lifecycle vocabulary (incl. `duplicate`, `timeout`, lifecycle-`unknown`) … in truth-state §1 / §5.3."

A safety invariant you cannot enumerate in the controlling document is a slogan, not a contract. The acceptance hooks exist (§9 lists "stale projection, rejected command, unknown confirmation, audit unavailable, last-owner warning, permission-missing"; NFR-4 demands stable selectors), which is good — but those six scenarios test six points against a 13-state badge × 10 layered-feedback states × 6 reason categories matrix that lives in another file. The bulk of the "never collapse these states" claim is untested by anything in this PRD. The phrase "**success-prohibited** states" (CP-3, addendum §G) is asserted with no test that proves `degraded` and `unable to verify` can never render as success across the surface.

**Suggested fix.** Either inline the canonical enumerations into addendum §G (the PRD claims they are "mirrored" — actually mirror them, including the sixth reason and the 13 badge states), or drop the "defined once / used verbatim" claim. Add explicit acceptance scenarios that exercise every success-prohibited state on at least one command flow, and a coverage statement mapping each of the 13 badge states to where it is tested.

---

## Finding 5 — HIGH — "Approved fallback" is treated as a real gate path while the PRD simultaneously states none exists — the phasing model rests on a door that is locked

**Location:** §14.3, §4 ("Proposed fallback"), §12 R-1/R-4, addendum §B; backlog line 62, line 104.

**The attack.** The phasing/readiness model offers a way out of blockage:

> "it promotes to ready only when its FrontComposer dependencies resolve **or** an approved fallback is recorded." (§14.3)

But the same PRD says, twice, that this escape hatch is empty:

> "**No fallback is approved yet**" (§4); "Blocked stories cannot become 'ready' without Product/UX fallback approval (none granted yet)." (R-4)

And the backlog confirms every fallbackDecision is `proposed` or `deferred`, never `approved` (lines 68–82). So the "or approved fallback" branch is, today, a non-path. The PRD presents a two-branch promotion rule where **both branches are currently closed** (dependencies unresolved AND no fallback approved) and does not say so in the phasing section. This is approval theater: a process that looks like it has a release valve, when in fact every route to "ready" is blocked and the only listed owner ("Tenants Product/UX") has approved nothing. Compounding it, the audit fallback (flat list, FR-20) is described in §7.8 as a real deliverable target while being, per backlog `ui-11`, `blocked` with a `deferred` fallback decision — i.e. it is not even a *proposed-and-pending* path, it is deferred.

**Suggested fix.** In §14.3, state explicitly: "As of this PRD, **both** promotion branches are closed for all rows — no FrontComposer dependency is `available` beyond `FC-TBL`, and **zero fallbacks are approved**. No story can reach `ready` until at least one branch opens." Make securing fallback approvals (or scheduling the FC components) the first gating action, not a mitigation buried in R-1/R-4.

---

## Finding 6 — MEDIUM — Internal status contradiction: `FC-CMD`/`FC-CNC` are described inconsistently ("needs confirmation" vs "missing"), and the "no command flows unblocked today" admission guts Phase 2b's framing as "most tractable"

**Location:** §11 (Dependencies), §14.2 Phase 2b; addendum §B; backlog `ui-07`/`ui-09`.

**The attack.** §11 lists `FC-CMD` under "Needs confirmation" and `FC-CNC` under "Missing." §14.2 then says Phase 2b items "are the most tractable command flows — but note **every** command flow depends on the command-feedback contract (`FC-CMD`, needs-confirmation) and the concurrent-command policy (`FC-CNC`, **missing**) resolving first. **There are no command flows that are unblocked today.**" So the section that is supposed to scope the "next, tractable" phase concedes in its own body that *nothing in it can start*. Calling something "the most tractable command flows" in the same breath as "there are no command flows unblocked today" is a contradiction in tone if not in letter — tractability is meaningless when the precondition is universally unmet. Meanwhile addendum §B and backlog rows show `FC-CMD: needs-confirmation` *and* every command row also blocked by `FC-CNC: missing` *and* `FC-LYT: needs-confirmation` — so Phase 2b commands are blocked by at least three independent unresolved dependencies, not "tractable."

Separately, the readiness vocabulary is muddied: §11's three-bucket scheme ("Available today / Needs confirmation / Missing") does not match the backlog's per-row `readiness` enum (`planning-only` / `blocked`), and the PRD never reconciles "needs-confirmation dependency" with "planning-only story" vs "missing dependency" with "blocked story." A reader cannot derive story readiness from the PRD's dependency buckets.

**Suggested fix.** Delete "most tractable" or qualify it as "least-blocked *relative to 2c*, but still fully blocked today." Add a one-line mapping: needs-confirmation/missing dependency → `planning-only`/`blocked` story, so dependency state and story readiness are reconcilable from the PRD alone.

---

## Finding 7 — MEDIUM — FR-22/FR-24/FR-25 are "committed product intent" with no backlog row and no backend evidence — scope that exists only as prose

**Location:** §7.9 note, FR-22, FR-24, FR-25; addendum §A (row: "**no backlog row yet**"), addendum §A scope-honesty note.

**The attack.** To the PRD's credit it flags this — but it still lists FR-22/24/25 as numbered, "testable" Functional Requirements with detailed consequences, then admits:

> "compensating recovery (FR-24, FR-25) and the evidence-receipt assembly (FR-22) are committed product intent but are **not yet backed by a dedicated `ui-NN` backlog row or backend evidence**."

So three FRs — including the entire **Compensating Recovery** feature (§7.9), which realizes UJ-4, one of only six journeys and the centerpiece of the "recover by correcting forward" thesis (§1) — have no backend evidence and no story. "Committed product intent" with no backend and no backlog row is a wish, not a requirement. The Audit Evidence Receipt (FR-22) is the mechanism the whole "prove what happened" value prop (§3.1, SM-5) rests on, and it is assembled from a "**NarrativePayload**" that appears nowhere in addendum §C's list of consumed backend surfaces — addendum §C says "No new backend endpoints for … receipt … the UI assembles those client-side from already-loaded projection/read-model fields," but never establishes that the NarrativePayload *is* an already-loaded field. The recovery thesis may be unfunded at the data layer.

**Suggested fix.** Mark FR-22/24/25 as **provisional / not requirements** until a story and backend evidence exist; move them to an "intended, unbacked" appendix so they aren't counted as scope. Confirm in addendum §C that `NarrativePayload` is an actual field on an existing read model, or flag it as a backend gap that blocks UJ-4 entirely.

---

## Finding 8 — MEDIUM — Success metrics measure activity and adoption, not the stated thesis; targets are hollow placeholders

**Location:** §15 SM-1..SM-5, all targets `[ASSUMPTION]`.

**The attack.** The product's thesis is *trustworthy, honest-about-state operations* — "a console operators can trust under incident pressure." But the **primary** metrics measure adoption and speed, not trust or correctness:

- SM-1 "share of routine … operations performed through the UI rather than raw command-API calls" — pure activity/adoption. Migrating people off the API measures channel shift, not whether the truth-state model works.
- SM-2 "median time to answer who-has-access/what-changed" — speed. Directly in tension with the entire safety thesis, and the PRD knows it (SM-C3 exists to counter it).
- Targets are non-numbers: "**majority within one quarter**," "under **~1 minute**," "**rising quarter over quarter**," "target trend **up**." Every primary target is hedged with `~` or a direction word and stamped "Targets are `[ASSUMPTION]` pending your numbers." A target that is an assumption pending numbers is not a target.

The thesis-true signal is buried in the **counter-metrics** (SM-C1 "actions completed without viewing a Consequence Preview should stay at ~0," SM-C2 "a drop in surfaced errors is a red flag," SM-C3 "acting on stale/unknown data is a regression"). Those are the only measures that actually test "honesty about state" — and they are demoted to "do not optimize" guardrails rather than the scoreboard. The product is graded on adoption and speed; its differentiator is graded only defensively.

**Suggested fix.** Promote a trust/correctness metric to primary, e.g. "% of confirmed-success displays that match source-of-truth projection on re-query (target ~100%)" and "% of high-impact actions that passed through a Consequence Preview." Put real numbers on at least SM-2 and SM-C1/C3 or stop calling them targets. Otherwise this product can hit every primary metric while being exactly the optimistic-success liar it claims not to be.

---

## Finding 9 — MEDIUM — Open-question density vs. a document that presents itself as decided; several "open questions" are unmade decisions dodging commitment

**Location:** §16 (12 open questions), §17 (Assumptions Index), inline `[ASSUMPTION]` tags; tension with §0/§14 framing.

**The attack.** This PRD carries **12 open questions** and a 10-entry assumptions index, and several of them are load-bearing decisions disguised as "questions to resolve later":

- §16.1 Command endpoint route — `POST /api/v1/commands` vs `/api/commands` — is unconfirmed "before any command phase." You don't know the endpoint you POST commands to. That's not an open question, that's an unverified integration assumption underlying every command FR (FR-10..19, 24, 25).
- §16.3 Layout contract — "**gates even the read-only MVP**" — is listed as question #3 in a backlog-of-questions, when it is in fact *the* critical-path blocker for the entire product (Finding 1). Burying the single most important unresolved decision as one of twelve bullets is altitude inversion.
- §16.6 "RTL support — in or out for v1?" and §16.5 WCAG 2.2 are scope decisions ("none of the specs commit") punted to "undecided" rather than decided. §9 hedges accessibility itself: "target WCAG 2.2 AA **where the … stack supports it** (conditional — no unconditional 2.2 promise)." A PRD that won't commit to its own accessibility floor is dodging.
- §16.8 "Consequence Preview scope for config edits — always required, or only for a high-risk key subset?" is flagged as "also a phasing lever," i.e. an open product-safety decision that changes scope, left open.

A document with this open-question density is a *discovery artifact*, not a "green-light to build" — yet §0 and §14 present it with the confidence of an approved plan. Many of these aren't questions the team will research; they're decisions someone is declining to make and relabeling as "open."

**Suggested fix.** Split §16 into (a) **true unknowns** requiring investigation (cursor durability behavior, what the pinned Fluent build supports) and (b) **decisions pending an owner** (RTL in/out, config-preview scope, endpoint route) — and assign each a decision owner and a "must-close-before" gate. Stop calling deferred decisions "questions." Promote §16.3 (layout) out of the list into §14 as the headline blocker.

---

## Finding 10 — LOW/MEDIUM — Vision/visual/NFR theater: passages that drive no decision and exist to sound rigorous

**Location:** §1 ¶3, §5.2, §5.3, §8 NFR-1, §10.

**The attack.** Several passages are furniture — they read well, assert virtue, and gate nothing testable:

- §1 ¶3 ("its honesty about state … refuses to fake certainty … a console operators can trust under incident pressure and owners can use without fear") is a manifesto. It restates CP-3/CP-4 in inspirational prose and adds no constraint a downstream owner can act on that §6 doesn't already carry. Pure vision theater.
- §5.2 "Tone is a **professional, calm, precise operations console** — not marketing … whitespace groups meaning rather than adding drama." This is mood copy. It binds nothing; you cannot fail a story for being "dramatic."
- §5.3 breakpoints "320–767 / 768–1023 / 1024+ / 1440+" are presented as "a layout rule, not just test widths," but no FR consumes the 1440+ "wide desktop" tier differently from 1024+, and the only *substantive* responsive rule (the fail-closed width rule) appears once and is good — the rest is standard-issue breakpoint boilerplate dressed as policy.
- §8 NFR-1 "conditional requests so unchanged data is cheap" — a real mechanism, but stated at NFR altitude with no measurable target beyond the audit "~500 events" figure that is itself repeated three times (§7.8, NFR-1, §15-adjacent) as if repetition were specification.
- §10's support-safety list is genuinely load-bearing (good), but partially duplicates the Glossary's "Support-safe reference" and NFR-2 without adding a test, so it reads as emphasis-by-repetition.

None of these is *wrong*; collectively they pad the document so that the buildability problem (Finding 1) and the unbacked-recovery problem (Finding 7) are harder to spot in the wordcount.

**Suggested fix.** Cut §1 ¶3 to one sentence and let §6 carry the contract. Delete §5.2 tone prose or convert it to a checkable rule ("no decorative card grids in the first slice" already exists in §5.2 — keep that, cut the mood). Drop the unused 1440+ tier or give it an FR. State the ~500-event target once (in NFR-1) and reference it.

---

## Finding 11 — LOW — The PRD knows its own source specs are wrong (ULID vs caller-supplied id) and ships anyway, deferring the correction

**Location:** §12 R-6, §16.12, addendum §E; specs uncorrected.

**The attack.** R-6 / §16.12 / addendum §E all document that "**Several UI specs state tenant/user ids are ULIDs**" while "the authoritative domain rule says they are caller-supplied strings." The PRD says it "follows the domain rule" and that "the specs need correcting." But this PRD's §0 explicitly defers to those very specs as "the source of technical depth" and says "Source of truth for mechanics is the `docs/tenants-ui-*` specs" (addendum top). So the PRD points downstream owners at documents it *knows contain a correctness-critical error* (parsing a `TenantId` as a ULID would break FR-7 "copy full id" and any lookup), and merely flags "the specs should be corrected" as open question #12 — without correcting them. That is shipping a known landmine with a "mind the landmine" sticky note. The honest move is to fix the specs (or stop citing them as source-of-truth) before green-lighting, not to enumerate the contradiction and proceed.

**Suggested fix.** Either correct the ID-scheme statements in the cited specs now, or downgrade those specs from "source of truth for mechanics" to "reference, superseded by this PRD on the ID scheme." Don't simultaneously cite a spec as authoritative and document that it is wrong.

---

## Overall verdict

**Not a green-light. This is a discovery/planning artifact wearing a build-ready costume.** The prose quality is high and the safety thinking is genuinely good in places (the non-collapse invariant, fail-closed responsive rule, counter-metrics, the boundary policy). But three structural truths are obscured rather than stated: (1) **nothing is buildable today** — every story is `planning-only`/`blocked`, all gated by `FC-LYT` which is unconfirmed, so the "read-only MVP" is no more startable than the scary platform-wide commands; (2) the **"backend is complete"** headline is contradicted by the PRD's own deferred-cursor-epic admission and post-epic remediation evidence keys; and (3) the **"both audiences"** and **"correct-forward recovery"** value props are thin — owners get one read FR and a borrowed verb, and the entire recovery feature (FR-22/24/25, UJ-4) has no backend evidence and no backlog row. The truth-state model that is the product's whole reason to exist is asserted as a slogan but its canonical vocabulary isn't even in the document, and the scoreboard (§15) grades adoption and speed while demoting the trust signal to "do not optimize." Fix Findings 1, 2, 5, and 7 before anyone treats this as approved scope.
