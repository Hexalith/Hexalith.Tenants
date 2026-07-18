# Adversarial Consistency Review — PRD + Addendum vs. 2026-07-15 Readiness Correction

- **Reviewed:** `prd.md` (460 lines) and `addendum.md` (128 lines) in `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/`
- **Review date:** 2026-07-17
- **Scope:** fidelity/consistency of the applied 2026-07-15 readiness correction only (no product-scope proposals)
- **Method:** full read of both documents, token sweeps for stale-language patterns, and resolution of every cited cross-reference target (§16.14, addendum §A–§I, WP IDs, AD-10/AD-12, linked files)

## Verdict

**PASS with no CRITICAL findings.** The correction's core moves are applied consistently: BFF-only assembly of receipts/previews/rejections; a six-read inventory including global administrators in both documents; aggregate-scoped AD-12 locking with the one-at-a-time scope explicitly superseded at every restatement that names it; protected search cursor (SEARCH-CURSOR-1) with no plaintext-offset language; Fluent pinned at `5.0.0-rc.4-26180.1` in both files; audit performance moved to blocked decision record §16.14 with no numeric render claims; Epic 5 / Epic 4 / Story 2.1 completion statements reframed as historical evidence; and addendum §I present with all six work packages. Five MEDIUM residuals (mostly readiness-framing drift and one PRD-wide missing work-package citation) and nine LOW style/precision items remain.

## Concurrency caution (process, not content)

The files were edited by another session **during this review** (mtime 2026-07-17 18:38; a new untracked `reconcile-scp-2026-07-15.md` appeared alongside). Two defects present in the pre-18:38 state were already fixed by that edit and are therefore **not** findings against the current text — recorded here so nobody re-reports or reverts them:

- PRD §16.2 formerly listed the "one-at-a-time" fallback with no supersession note; it now reads "the one-at-a-time scope is since superseded — aggregate-scoped per AD-12, see §4 / addendum §B" (line 434).
- Addendum §C formerly said "**Open:** confirm against deployed gateway vs. `/api/commands` alias"; it now reads "**Resolved (2026-07-15):** this endpoint is preserved explicitly; no unversioned `/api/commands` alias is adopted (PRD §16.1)" (line 42), matching PRD §16.1.

All findings below are against the 18:38 state. Re-verify against disk before committing fixes — the documents are under active concurrent reconciliation.

---

## CRITICAL

None.

---

## MEDIUM

### M1. Direct-read/provenance contract stated in flat present tense vs. the addendum's "until verified" interim reality

The PRD and addendum §C assert the corrected read path as current fact, while addendum §D says the opposite is true until three work packages are verified. A reader of §C or §11 alone would believe the six direct reads and authoritative provenance already exist.

- `prd.md:335` (NFR-1): "freshness is surfaced, not hidden, and carries **authoritative provenance** — ETag, projection version, and read-model freshness from the six direct Tenants REST reads (PLAT-FRESH-1 / UI-READ-1, addendum §I)"
- `prd.md:366` (§11): "All UI reads route directly to Tenants with authoritative freshness provenance (UI-READ-1 / PLAT-FRESH-1, addendum §I); the generic EventStore query route is not a Tenants read path"
- `addendum.md:40` (§C): "All six UI reads route directly to Tenants REST; commands and status lookup stay on the EventStore command client; the generic EventStore query route is not a Tenants read path." — no gating qualifier in this sentence at all.

Versus:

- `addendum.md:65` (§D): "Until PLAT-FRESH-1, HOST-REF-1, and UI-READ-1 are verified, the current generic EventStore query route normalizes provenance to Unknown and freshness-dependent stories carry `blockedBy` metadata."
- `addendum.md:125` (§I, UI-READ-1 scope): "remove the generic EventStore query route from Tenants UI reads" — i.e., that route is the *current* read path.

The WP-ID citations partially anchor the PRD sentences, but the phrasing is indicative ("route", "carries", "is not"), not normative ("must route", "will carry once §I verified"). Recommend normative phrasing or an explicit "(target contract; interim per addendum §D)" qualifier, especially in `addendum.md:40`.

### M2. HOST-REF-1 is never cited anywhere in the PRD

`grep -c "HOST-REF" prd.md` → **0**. The read-path unblocking trio is PLAT-FRESH-1 / HOST-REF-1 / UI-READ-1 (`addendum.md:65`), and HOST-REF-1 is a defined §I package (`addendum.md:124`). But every PRD passage that names the read-path gates cites only two of the three:

- `prd.md:205` (FR-1): "(the search index never supplies row data; UI-READ-1 / PLAT-FRESH-1, addendum §I)"
- `prd.md:335` (NFR-1): "(PLAT-FRESH-1 / UI-READ-1, addendum §I)"
- `prd.md:366` (§11): "(UI-READ-1 / PLAT-FRESH-1, addendum §I)"
- `prd.md:410` (§14.3) enumerates the §I packages as "(freshness provenance, split reads, protected search cursor, removal proof, production boundary)" — "split reads" can be read as covering HOST-REF-1+UI-READ-1 jointly, but the ID itself never appears.

A PRD reader tallying prerequisites for the six-read path misses one of the three gates. Recommend adding HOST-REF-1 to at least the §11 hosting/backend bullets.

### M3. FC-CNC readiness "confirmed" rests on evidence the same passage declares superseded; no verifier named for the AD-12-scoped policy

- `prd.md:361` (§11): the bullet is headed "**Confirmed by Story 1.0 (2026-06-05):**" yet its FC-CNC entry reads "(`FC-CNC` — since aggregate-scoped per AD-12: one active command per (circuit, AggregateIdentity), unrelated aggregates proceed; the historical global one-at-a-time scope is superseded)". Story 1.0 (2026-06-05) cannot have confirmed a lock scope adopted 2026-07-15 — the item sits under a confirmation header whose evidence the parenthetical itself retires.
- `addendum.md:28` (§B): readiness column "**confirmed**" while the note says "Story 1.0's global one-at-a-time policy is superseded historical evidence (2026-07-15 correction)."

The §B closing paragraph (`addendum.md:36`) generically demotes all confirmations to historical evidence, but neither location says what verifies the *aggregate-scoped* policy or who owns that reverification (every other gated capability points at a §I package or story evidence). Recommend an explicit reverification owner/evidence pointer for the AD-12 lock scope, or a readiness value other than bare "confirmed".

### M4. Risk R-4 was not swept by the correction: present-tense confirmations as grounds, and a remaining-gates list that omits the §I work packages

- `prd.md:376` (R-4): "Story 1.0 confirms `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-A11Y`, `FC-L10N`, and `FC-DOC`; Story 1.2 resolves the `FC-TBL` tenant-list boundary. Remaining gates are story-specific evidence, `FC-TOK` fallback discipline, and audit/proof evidence readiness — not fallback approval or tenant-list grid decisioning."

Two problems: (a) it is the only remaining passage using completion statements in unreframed present tense as grounds ("confirms", "resolves") with no "historical evidence, not readiness waivers" rider — the header still says "contract confirmation updated 2026-06-27", predating the correction; (b) its "Remaining gates" list **omits the addendum §I prerequisite work packages**, directly diverging from §14.3 (`prd.md:410`): "Remaining promotion gates are story-specific evidence, `FC-TOK` fallback discipline, audit/proof evidence readiness, **and the addendum §I prerequisite work packages**". A reader consulting the risk register for what still gates promotion gets the pre-correction answer. Related, minor: R-1 (`prd.md:373`) also states "`FC-LYT`, `FC-CMD`, `FC-CNC`, … are confirmed by Story 1.0 (2026-06-05)." with no AD-12 qualifier on FC-CNC (it does not restate the one-at-a-time scope, so the exposure is lower).

### M5. §16.7 headline "RESOLVED" over-claims relative to its own body

- `prd.md:439`: "**Cursor durability across replicas/restarts — RESOLVED (2026-07-15).** UI behavior on cursor invalidation is defined: restart from page 1 with an honest localized notice (SEARCH-CURSOR-1 / AD-10). Durability verification itself belongs to PLATFORM-OPS-1 before any multi-replica claim; the deferred backend epic stands."

Only the UI-behavior sub-question is resolved; the titular question — durability — is explicitly still open ("the deferred backend epic stands"), and R-3 (`prd.md:375`) still calls it "deferred". A status-scanner of §16 ticks durability as done. Recommend retitling, e.g. "— UI invalidation behavior RESOLVED (2026-07-15); durability verification open (PLATFORM-OPS-1)".

---

## LOW

### L1. §16.14 pre-commits the "500-event" number inside the record that claims no numerics exist

- `prd.md:446`: "Approve, before Story 5.1 is Ready: the representative 500-event dataset shape; … No numeric budget exists until this record is approved". The old "~500 event" render *claims* are gone everywhere (verified), but fixing the dataset size at 500 while deferring only budgets partially pre-decides the record. If the size itself is up for approval, say "the representative dataset size and shape"; if 500 is deliberately carried over as the proposed shape, mark it as proposed.

### L2. AD-10 / AD-12 cited bare with no source document named in either file

Cited at `prd.md:150, 259, 361, 405, 434, 439` and `addendum.md:28, 36, 67`. Both resolve correctly to `_bmad-output/planning-artifacts/architecture.md` (AD-10 "Memories Is Search-As-Index-Only", line 130; AD-12 "Command Flows Share One FrontComposer Command Posture", line 142), and the cited content matches the decisions verbatim in substance (offset-advance-by-raw-hits rule under AD-10; `(interactive circuit, AggregateIdentity)` lock from submit through terminal evidence under AD-12) — so nothing is misnamed. But unlike WP IDs (anchored to "addendum §I") and specs (named files), AD IDs are anchored nowhere; a first-time reader cannot resolve them. Recommend one parenthetical "(architecture.md AD-12)" at first use in each document.

### L3. FR-12 Notes attribute aggregate-scoped locking to FC-CMD rather than FC-CNC

- `prd.md:259`: "uses the Product/UX-approved `FC-CNS` inline consequence fallback and the `FC-CMD` command-lifecycle contract with aggregate-scoped command locking (AD-12)". Everywhere else the lock policy belongs to FC-CNC (`prd.md:150`, `prd.md:361`, `addendum.md:28`).

### L4. Bare "FR1"/"FR15" style drift vs. canonical "FR-N"

- "FR1": `prd.md:28` ("covers FR1 whole-set search"), `prd.md:395` ("covers FR1 whole-set search behavior").
- "FR15": `prd.md:406` ("FR15 is a reversible lifecycle soft-delete"), `prd.md:410` ("reclassifies FR15 as eligible"), `addendum.md:36` ("reclassifies FR15 as a high-impact reversible lifecycle control"). Same lines mix in correctly hyphenated "FR-19", "FR-12" etc.

### L5. §14.2 Phase 2c header parenthetical is stale

- `prd.md:406`: "**Phase 2c — high-impact, audit & recovery (gated on FrontComposer components / fallback approvals):**" — but the fallback-approval gate "is satisfied" (`prd.md:410`) and the operative gates are now story evidence + the §I packages (§14.3, R-4-corrected framing). The parenthetical describes the pre-2026-06-03 gating.

### L6. "normalizes provenance to Unknown" — capitalized token in a casing-significant vocabulary

- `addendum.md:65`: "the current generic EventStore query route normalizes provenance to Unknown". §G (`addendum.md:88`) declares casing significant and the freshness state is lowercase `unknown` (`addendum.md:92`). If "Unknown" here means a provenance enum member rather than the freshness state, name the type; otherwise lowercase it.

### L7. §B FC-CNC note elides the lock-rule phrase

- `addendum.md:28`: "Lock scope is aggregate-scoped per AD-12: (interactive circuit, AggregateIdentity);" — a colon followed by a bare tuple; the glossary form (`prd.md:150`) "one active command per (interactive circuit, AggregateIdentity) from submit through terminal evidence" is complete. Minor parse stumble in a changed passage.

### L8. §A row 7.9 omits Story 5.3, which the adjacent note and PRD §7.9 both include

- `addendum.md:17` (row 7.9 backlog ids): "Epic 5 Stories 5.5 and 5.6 (`epics.md`)" vs. `addendum.md:19` (note): "Stories 5.3, 5.5, and 5.6" and `prd.md:321`: "Stories 5.3, 5.5, and 5.6 plus the matching story records". Story 5.3 (FR-22 receipt) has no home in either the 7.8 or 7.9 table row.

### L9. §2 retains the last unreframed "built and tested" claim

- `prd.md:40`: "The **Phase 1 backend is built and tested** — … all exist." It is hedged in the same bullet ("ready-but-not-frozen, not flawless") and is Why-Now motivation rather than story-readiness grounds, so it does not violate the correction — but it is the only completion statement left without the historical-evidence framing applied everywhere else.

### L10. §16 punctuation drift (pre-existing)

- `prd.md:440` (§16.8): "…(FR-16/FR-17) - RESOLVED 2026-06-29." uses a hyphen where sibling items (§16.1, §16.3, §16.7) use an em dash. Predates the 2026-07-15 edit; noted only for a future sweep.

---

## Verified clean (checked, no finding)

- **BFF assembly:** receipts, previews, and rejections are BFF-assembled/redacted at every occurrence (`prd.md:138, 143, 149, 312, 321, 355`; `addendum.md:43, 69, 70, 115`); zero client-side-assembly residue.
- **Six-read inventory:** both documents enumerate all six reads including global administrators (`prd.md:366`; `addendum.md:40`); no five-read remnant anywhere.
- **Global one-at-a-time:** every passage that restates the one-at-a-time scope carries the AD-12 supersession (`prd.md:150, 361, 434`; `addendum.md:28`). (Unqualified *confirmation lists* → M3/M4.)
- **Search cursor:** protected, scope-bound cursor with page-1 recovery; "plaintext Memories offset" appears only as an explicit prohibition (`prd.md:205`; `addendum.md:67`).
- **Fluent pin:** `5.0.0-rc.4-26180.1` in both files (`prd.md:437`; `addendum.md:73`); no rc.3 anywhere.
- **Audit performance:** no numeric render/budget claim outside the blocked §16.14 record; FR-20 and NFR-1 defer to it (`prd.md:304, 331, 335, 446`) (L1 nuance aside).
- **Nav "areas":** "three primary nav areas" appears only as a superseded model (`prd.md:155`); Audit/Global Administrators consistently contextual/module-internal (`prd.md:131, 161, 398, 407, 441`).
- **Blazor Auto:** appears only as an explicit negation (`addendum.md:73`).
- **Epic 5 vs FR-12/WP-2A:** consistent — Story 2.4 + WP-2A own removal proof with "no dependency on Epic 5" (`prd.md:259`); Epic 5 evidence is historical and reverification-gated (`prd.md:299, 304, 321`; `addendum.md:36`); Story 5.7 owns the global-admin correction slice in both files.
- **Cross-references:** §16.14 exists and is item 14 (`prd.md:446`); addendum §A–§I all exist; every cited WP ID (PLAT-FRESH-1, HOST-REF-1, UI-READ-1, SEARCH-CURSOR-1, WP-2A, PLATFORM-OPS-1) is defined in §I (`addendum.md:123–128`); §16.2's "see §4 / addendum §B" resolves; linked files all exist on disk (`sprint-change-proposal-2026-07-15.md`, `fallback-approval-record-2026-06-03.md`, `story-1-0-spike-note-2026-06-05.md`, `deferred-work.md`).
- **State-set counts:** Truth badge 13, freshness 5, command lifecycle 10, layered feedback 10, reasons 6, audit availability 4 — glossary, CP-1/CP-10, and §G enumerations all agree, including the deliberate `audit pending` vs `audit_pending` casing split.
- **§9 lock-retention evidence** ("through `accepted` and `projection_pending`", `prd.md:348`) matches AD-12's rule text ("from submit through accepted/projection-pending until terminal evidence", architecture.md:150) — initially suspected as an understatement; checked and consistent.
