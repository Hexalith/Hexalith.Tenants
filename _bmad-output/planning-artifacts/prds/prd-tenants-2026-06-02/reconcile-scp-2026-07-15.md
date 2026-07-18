# Reconciliation Review — PRD + Addendum vs. Sprint Change Proposal 2026-07-15

- **Date:** 2026-07-17
- **Contract:** `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-15.md`
- **Reviewed artifacts:** `prds/prd-tenants-2026-06-02/prd.md` (461 lines), `prds/prd-tenants-2026-06-02/addendum.md` (128 lines)
- **Scope:** only proposal requirements targeting the PRD and addendum. Epics, UX docs, sprint-status, architecture, CI/test implementation, and backlog structure are downstream handoffs and were not assessed.
- **Verdict: 2 GAPS (both minor, one-line fixes). All other checked items SATISFIED.**

---

## Proposal 1 — PRD FR-12 note (complete vertical slice, WP-2A, no Epic 5 dependency)

**SATISFIED.**

- PRD §7.4 FR-12 Notes (line 259): "Story 2.4 delivers the complete FR-12 vertical slice — fail-closed gating, consequence preview, elevated friction, projection-confirmed removal, audit-availability handling, and minimum support-safe removal proof (work package WP-2A, addendum §I). It has no dependency on Epic 5; Epic 5 generalizes audit browsing and recovery from this foundation, it does not complete FR-12 retroactively." This matches the proposal's NEW block in meaning, item for item.
- Addendum §I WP-2A row (line 127) carries the work-package substance: BFF-assembled, redacted removal-proof view model over the existing audit read path, **no new receipt endpoint**, covering pending/delayed/unavailable/available audit states without false success — matching the proposal's WP-2A bullet list (the gateway/component/integration evidence bullet is test-tier handoff, correctly not restated in the PRD).
- Historical-evidence reverification is covered generally by PRD line 28 and §14 line 395 ("historical evidence, not readiness waivers … must be reverified against the corrected contracts").

## Proposal 3 — Truthful Freshness Provenance

**SATISFIED** on all six sub-checks.

1. **Provenance truthfulness:** PRD §8 NFR-1 (line 335): freshness "carries **authoritative provenance** — ETag, projection version, and read-model freshness from the six direct Tenants REST reads (PLAT-FRESH-1 / UI-READ-1, addendum §I)". Addendum §D freshness primitive (line 65) additionally states the honest interim truth: "Until PLAT-FRESH-1, HOST-REF-1, and UI-READ-1 are verified, the current generic EventStore query route normalizes provenance to Unknown and freshness-dependent stories carry `blockedBy` metadata."
2. **Six-read inventory:** addendum §C (line 40) lists the exact six routes from the proposal (`GET /api/tenants`, `/api/tenants/{tenantId}`, `/api/tenants/{tenantId}/users`, `/api/users/{userId}/tenants`, `/api/tenants/{tenantId}/audit`, `/api/global-administrators`) with their query names; PRD §11 (line 366) mirrors the six-read inventory and references §C.
3. **PLAT-FRESH-1 / HOST-REF-1 / UI-READ-1 present and gating:** all three defined in addendum §I (lines 123–125) with the proposal's substance (metadata on 200/304/empty/authorization-safe; split Tenants-query vs. EventStore-command references, AppHost not expanded; all six reads to Tenants, commands/status on the EventStore command client, generic query route removed). Gating: §I intro (line 119) — "freshness-, search-, and production-dependent stories carry `blockedBy` metadata until the relevant packages are verified" — plus §D line 65 and PRD lines 28/395/410.
4. **ServedAt not projection age:** PRD NFR-1 (line 335), addendum §D (line 65), and the PLAT-FRESH-1 row (line 123) all state "`ServedAt` is never a substitute for projection age."
5. **Aging not claimed on the wire:** addendum §D (line 65): "`aging` is not claimed on the wire until authoritative projection-time provenance supports it."
6. **Refreshing client-transient:** addendum §D (line 65): "`refreshing` remains client-transient." (The badge derives `current/stale/unknown` from the wire provenance — matching the proposal's current/stale/unknown acceptance triple.)

## Proposal 4 — Protected Memories Search Cursor

**SATISFIED** on all six sub-checks.

1. **No plaintext offset:** PRD FR-1 search consequences (line 205): "search paging uses a **protected, scope-bound cursor**, never a plaintext Memories offset". No residual plaintext-offset language found in either file (grep-verified; the only raw-offset mentions are inside the protection statements themselves).
2. **SEARCH-CURSOR-1 scope binding:** addendum §D Pagination (line 67): protected "by the approved server-side cursor codec/DataProtection path, bound to (authenticated user, normalized query, status, sort, direction, page size)" — the exact scope tuple. §I row (line 126) registers the work package with cross-user isolation tests.
3. **Support-safe handling:** §D (line 67): "kept out of visible copy, DOM attributes, logs, telemetry tags, and copy actions."
4. **Page-1 recovery:** §D (line 67) and PRD FR-1 (line 205): on scope mismatch, decode failure, or invalidation, restart from page 1 with an honest localized notice. Also reflected in PRD §16.7 (line 439).
5. **AD-10 offset advance:** §D (line 67): "the internal offset advances by raw hits consumed, including dropped malformed, duplicate, unauthorized, or unhydrated hits (AD-10)."
6. **Non-blocking Memories fallback preserved:** PRD FR-1 (line 205): "search **never blocks the list** — if Memories is unavailable the list falls back to the cursor view with a non-blocking notice."

## Proposal 5 — BFF-Assembled Receipt / Preview / Rejection Safety

**SATISFIED** on all sub-checks.

- **Glossary:** Consequence Preview (line 138) "Assembled and redacted in the server-side BFF"; Audit Evidence Receipt (line 143) "Assembled and redacted in the server-side BFF from a structured **NarrativePayload** (never the raw event payload); rendered components receive only support-safe localized fields"; Rejection (line 149) "surfaced through a BFF-assembled, support-safe rejection view model".
- **FR-22 (line 312):** BFF-assembled/redacted from NarrativePayload, "(no new backend receipt endpoint)", with the full forbidden-field list.
- **Guardrails §10 (line 355):** "the server-side BFF assembles and redacts every receipt, consequence-preview, and rejection view model … forbidden fields must be provably un-renderable, un-copyable, un-announceable, un-loggable, and un-serializable into component state" — captures the proposal's negative-test intent at PRD altitude.
- **Forbidden-field list:** "raw `NarrativePayload`, event bodies, command payloads, tokens, internal correlations, ETags, or raw metadata" appears verbatim in FR-22, §10, and addendum §D (line 70).
- **Addendum §C (line 43):** "No new backend endpoints for consequence/receipt/command-status — the server-side BFF assembles and redacts … from already-loaded projection/read-model fields." **§D** (lines 69–70) covers rejection and receipt assembly; **§H** (line 115) covers preview assembly ("Preview assembly and redaction occur in the server-side BFF (§C, §D); the inline rendering receives only the support-safe view model").
- **Story 5.3 derivation-in-BFF** (behaviorally same, moved server-side) is reflected at PRD altitude by the §7.9 note (line 321): "Receipt and preview derivation occurs in the server-side BFF (FR-22)."
- **No new backend receipt/preview endpoint:** confirmed in FR-22, §11 (line 366: "does not add backend endpoints (including receipt, preview, list-filter, or correction endpoints)"), and addendum §C.

## Proposal 6 — AD-12 Aggregate-Scoped Command Locking

**GAP (minor) — one residual unmarked "one-at-a-time" site.**

Satisfied sites:

- PRD Glossary "Proposed fallback" (line 150): "The `FC-CNC` one-at-a-time scope is since superseded (2026-07-15): command locking is **aggregate-scoped per AD-12** — one active command per (interactive circuit, AggregateIdentity) from submit through terminal evidence, while unrelated aggregates proceed; bulk submission, toast batching, and multiple simultaneous commands for one aggregate remain prohibited." Historical fallback explicitly marked superseded — exactly the proposal's NEW meaning.
- PRD §11 (line 361): FC-CNC "since aggregate-scoped per AD-12 … the historical global one-at-a-time scope is superseded".
- PRD §14.2 (line 405): FC-CNC "since aggregate-scoped per AD-12".
- PRD FR-12 Notes (line 259): "the `FC-CMD` command-lifecycle contract with aggregate-scoped command locking (AD-12)".
- PRD §9 acceptance evidence (line 348): "command-lock retention through `accepted` and `projection_pending` states" — lock-retention invariant preserved.
- Addendum §B FC-CNC row (line 28): full AD-12 scope statement plus "Story 1.0's global one-at-a-time policy is superseded historical evidence (2026-07-15 correction)."

**GAP-1:** PRD §16 Open Question 2 (line 434) still says "Product/UX approved the flat-audit-list, inline-consequence-preview, and **one-at-a-time** fallbacks … Story 1.0 confirmed … `FC-CNC` …" with **no superseded/aggregate-scoped marker**. Every other FC-CNC/one-at-a-time site carries the AD-12 correction; this is the only residual that a reader could take as an active global-lock statement. The proposal requires the aggregate-scoped language everywhere FC-CNC/one-at-a-time appeared, with the historical fallback marked superseded.

- **Where:** PRD §16.2, line 434.
- **Suggested wording:** after "one-at-a-time fallbacks", append e.g. "(the `FC-CNC` one-at-a-time scope is since superseded — command locking is aggregate-scoped per AD-12; see §4 / addendum §B)".
- **Severity:** minor — historical-record framing, and the normative definitions all carry the correction; but it is the exact class of residual the sweep was meant to remove.

## Proposal 7 — Canonical Artifact Synchronization (PRD/addendum slices)

1. **Six-read language:** SATISFIED. No "five-read" residual in either file (grep-verified; PRD CP-1 "Five truth dimensions" is the truth-dimension count, not the read inventory). Six reads: PRD §11 (line 366), NFR-1 (line 335), addendum §C (line 40), §D (line 65), UI-READ-1 (line 125).
2. **Runtime InteractiveServer + BFF:** SATISFIED. Addendum §D pinned stack (line 73): "Blazor **InteractiveServer** with a server-side BFF (normative runtime — Blazor Auto is not)". The only "Blazor Auto" occurrence in either file is this negation. PRD states runtime facts only via §11 (line 368, InteractiveServer one-replica) — consistent. No-optimistic-success invariants preserved intact (CP-3/CP-4, lines 186–187; SM-6 line 420).
3. **Fluent baseline:** SATISFIED. No RC3 residual (grep-verified). Pinned `5.0.0-rc.4-26180.1`, centrally consumed, with build-time component/icon/ARIA verification: addendum §D (line 73); PRD §16.5 (line 437).
4. **Information architecture:** SATISFIED. One Tenants shell entry with Tenants/Users page-local tabs; Global Administrators and Audit contextual, not left-menu areas: PRD Glossary "Operations Shell" (line 131), §5.1 (lines 155–163), §14.1 (line 398), §14.2 audit-contextual note (line 407), §16.9 (line 441 — "The IA itself is settled: audit is a contextual route, not a nav area").
5. **No global-search entry:** SATISFIED. Zero occurrences of "global search"/"global-search" in either file (grep-verified); the only search surfaces are the tenant-list search (FR-1) and user lookup (FR-4).
6. **Orchestration ownership / AppHost transitional:** SATISFIED. PRD §11 (line 368): domain-owned UI host; orchestration/shared hosting/health/telemetry/production ownership platform/composing-host (PLATFORM-OPS-1); "the repository AppHost is transitional and is not expanded with shared platform plumbing." Addendum §I HOST-REF-1 (line 124) and PLATFORM-OPS-1 (line 128).
7. **State implementation:** SATISFIED. Addendum §D (line 73): "Typed immutable state is required; Fluxor is not a mandatory architecture constraint." No other Fluxor mention exists (grep-verified).
8. **FR-19 gated, not categorically blocked:** SATISFIED. PRD FR-19 (lines 295–297) reflects last-GA as unavailable-with-reason; §14.2 (line 406): "gated on fixed-scope routing, freshness, last-administrator protection, and evidence gates — **not categorically blocked**"; addendum §B (line 36): "FR-19 is not categorically blocked: it is gated on …".
9. **PRD NFR-1..5 kept:** SATISFIED. §8 (lines 333–339) contains exactly NFR-1 through NFR-5, unchanged in identity. (DQR renames are epics-side, out of scope here.)
10. **Open questions:** SATISFIED in the PRD (§16.1 command endpoint RESOLVED 2026-07-15; §16.2/§16.3 fallback/contract/grid resolved; §16.7 cursor-invalidation behavior RESOLVED with durability routed to PLATFORM-OPS-1; §16.8 config-preview resolved; retained product/ops questions: §16.4 localization ownership, §16.5 WCAG scope, §16.6 RTL, §16.9 audit-entry hide/stub, §16.10 freshness thresholds, §16.11 sensitive configuration, §16.13 owner depth, §16.14 performance budget approval — precisely the "explicitly owned product/operations decisions" the proposal retains). **But see GAP-2 below:** the addendum still carries a stale "Open" marker for the already-resolved command-endpoint question.

**GAP-2:** Addendum §C (line 42): "**Command endpoint:** `POST /api/v1/commands` … **Open:** confirm against deployed gateway vs. `/api/commands` alias (PRD §16.1)." PRD §16.1 (line 433) resolves this on 2026-07-15: "the 2026-07-15 correction preserves it explicitly and **adopts no unversioned alias**." The addendum's residual "Open … alias" marker contradicts the closed decision and (weakly) the proposal Constraint "Preserve POST /api/v1/commands" by re-inviting an alias evaluation the correction rejected.

- **Where:** addendum §C, line 42.
- **Suggested wording:** replace the "**Open:** …" clause with "Resolved (2026-07-15): `POST /api/v1/commands` is preserved explicitly; no unversioned `/api/commands` alias is adopted (PRD §16.1)."
- **Severity:** minor — the endpoint itself is preserved everywhere; this is a stale cross-reference, not a substantive contract error.

## Proposal 9 (PRD side) — Objective Quality and Production Gates

**SATISFIED.**

- **"~500 events without unacceptable degradation" replaced:** no residual of that phrasing anywhere (grep-verified). PRD §16.14 (line 446) is the blocked decision record: "Audit performance contract (blocked decision record — Product/Operations owned; **blocks Story 5.1 Ready**)" and enumerates **all six required elements**: representative 500-event dataset shape; page size and filter mix; reference environment and network assumptions; initial-render and interaction percentile budgets; authoritative test tier and repeatability method; fallback trigger for stricter paging or virtualization. "No numeric budget exists until this record is approved" — no number invented.
- **Gating wired through the PRD:** FR-20 (line 304, "governed by the audit performance decision record (§16.14) — no numeric budget is claimed here"); §7 feature NFR (line 331, "Story 5.1 is not Ready before it is approved"); §8 NFR-1 (line 335, no numeric audit budget claimed).
- **Production-boundary honesty:** PRD §11 (line 368): "Production-readiness claims must not exceed recorded evidence; InteractiveServer stays at one replica until shared DataProtection, circuit/session routing, and cursor durability are verified." Addendum §I PLATFORM-OPS-1 (line 128) mirrors it, including ServiceDefaults/health/OpenTelemetry/configuration/secrets/non-root defaults. §16.7 (line 439) routes durability verification to PLATFORM-OPS-1 "before any multi-replica claim."

## Proposal 10 — Current Authority vs. Historical Evidence

**SATISFIED.**

- **Current intent + prerequisite status stated:** PRD post-readiness update (line 28), §14 build-readiness status "updated 2026-07-15" (line 395), §14.3 promotion gates naming the §I packages (line 410); addendum §B closing statement (line 36) and §I (lines 117–128).
- **Historical completion is not a readiness waiver:** stated explicitly and repeatedly — PRD line 28 ("completion statements are historical evidence, not readiness waivers: affected work must be reverified against the corrected contracts"), line 395, line 410, FR-20 (line 304), §7.9 note (line 321), §14.2 (lines 405–406); addendum §B (line 36 — "Confirmation and completion statements in this section are historical evidence, not readiness waivers").
- **History preserved, not deleted:** Story 1.0/1.2/2.1 evidence, Epic 4/5 delivery references, the 2026-06-03 fallback approval record, and the 2026-06-06/2026-06-27 correction notes all remain, re-labeled as historical evidence with links intact.

## Constraints Check

- **POST /api/v1/commands preserved:** yes (addendum §C line 42, PRD §11 line 366, PRD §16.1 line 433) — modulo the stale "Open … alias" marker (GAP-2).
- **No new Tenants receipt/preview/list-filter/correction endpoints:** upheld — PRD §11 (line 366) and FR-22 (line 312); addendum §C (line 43).
- **Direct Tenants REST reads preserved:** yes — six-read inventory is the read path (§C, §11, NFR-1).
- **Projection-confirmed success / fail-closed / forward-only correction / support safety / non-collapse:** all intact — CP-2/CP-3/CP-5/CP-7 (lines 185–190), Glossary non-collapse (line 140), §10 (lines 351–356), addendum §G (lines 86–102 unchanged canonical sets).
- **Repository AppHost not expanded with shared platform plumbing:** stated verbatim (PRD line 368; addendum HOST-REF-1 line 124).
- **No submodule implementation authorized:** addendum §I intro (line 119) restates it.

---

## Summary of Gaps

| # | Location | Issue | Fix |
|---|---|---|---|
| GAP-1 | PRD §16.2 (line 434) | Residual "one-at-a-time fallbacks" mention without the AD-12 aggregate-scoped/superseded marker — the only FC-CNC site missed by the Proposal 6 sweep | Append "(since superseded — aggregate-scoped per AD-12; see §4 / addendum §B)" |
| GAP-2 | Addendum §C (line 42) | Stale "**Open:** confirm against deployed gateway vs. `/api/commands` alias" contradicts PRD §16.1's 2026-07-15 resolution (endpoint preserved, no alias adopted) | Replace the Open clause with a Resolved (2026-07-15) note: endpoint preserved, no unversioned alias |

Both gaps are one-line wording residues; no substantive contract requirement of the proposal is missing from the edited PRD/addendum.
