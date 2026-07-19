# Validation Report — Tenants Management UI (SCP-2026-07-15 UX-slice update)

- **DESIGN.md:** `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md`
- **EXPERIENCE.md:** `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md`
- **Run at:** 2026-07-19
- **Scope:** single user-picked lens — adversarial SCP cross-artifact consistency (the full rubric + accessibility gate ran at the original 2026-06-02 finalize; those reports persist unchanged). All findings below were **resolved in-spine the same run**; resolutions are noted per finding.

## Overall verdict

The UX slice of approved SCP-2026-07-15 substantively landed: all 8 items (BFF assembly boundary, AD-12 aggregate-scoped locking, InteractiveServer runtime, rc.4 pin, global-search removal, six-read freshness provenance, SEARCH-CURSOR-1, historical-evidence framing) appear in the correct spine — EXPERIENCE for behavior, DESIGN for visuals — with superseded policies correctly framed as historical evidence subject to reverification. No "client-side assembly", Blazor Auto, global one-at-a-time-as-current, rc.3-as-current, five-read, or plaintext-offset language survives as current intent.

The reviewer found 0 critical, 1 high, 3 medium, 3 low findings. All seven were fixed in the spines before close; one companion PRD errata (stale audit-nav wording at prd.md:101/307) is handed off to the next bmad-prd run and recorded in the memlog.

## Category verdicts

- Landed check (8 SCP items) — **strong** (8/8 landed, correct spine placement, no ownership inversion)
- Stale-language hunt — **adequate → resolved** (1 high + 1 medium + 1 low, all fixed)
- Cross-artifact agreement — **adequate → resolved** (2 medium + 2 low, all fixed)

## Findings by severity

### Critical (0)

### High (1)

**Stale-language** — UJ-4 opened the audit trail "from nav" (EXPERIENCE.md, UJ-4 step 1)
Contradicted the spine's own IA table and SCP P7.4 (Audit is a contextual route); a developer following UJ-4 could add an Audit left-nav entry.
Fix applied: reworded to "from a contextual entry — a tenant row, tenant detail, user lookup, or command result". Companion PRD errata (prd.md:101 "from nav"; prd.md:307 "from navigation") handed off to bmad-prd.

### Medium (3)

**Stale-language** — interim wire-vocabulary clause was ambiguous (EXPERIENCE.md, State Patterns §2)
"Until then the wire vocabulary is current/stale/unknown" could be read as licensing `current`/`stale` claims on the unverified generic route, which per addendum §C/§D normalizes provenance to `unknown`.
Fix applied: split the two regimes — generic route normalizes to `unknown` (fail-closed); `current`/`stale`/`unknown` becomes the wire vocabulary only once PLAT-FRESH-1 / HOST-REF-1 / UI-READ-1 verify; `aging` joins later still.

**Cross-artifact** — ready-gate acceptance-scenario list narrower than PRD §9 (EXPERIENCE.md, Accessibility Floor)
Missing accepted-but-projection-pending, data-unavailable-but-not-authorization-denied, and AD-12 command-lock retention evidence.
Fix applied: list extended to mirror PRD §9 and PRD §9 declared authoritative.

**Cross-artifact** — GA-correction interim gate unstated (EXPERIENCE.md, Readiness severity split)
Story 5.7's verification was framed as historical evidence but the current unavailability was only implied.
Fix applied: added "Until Story 5.7 is reverified under the corrected contract, global-administrator correction is unavailable with `high-impact flow not ready`."

### Low (3)

**Stale-language** — fallback #1 "delivered … by Epic 5" read alone as a current completion claim (EXPERIENCE.md, approved fallbacks). Fix applied: inline "(historical evidence, subject to reverification)".

**Cross-artifact** — DESIGN receipt forbidden-list omitted raw `NarrativePayload` itself (DESIGN.md, audit-evidence-receipt). Fix applied: added to the list.

**Cross-artifact** — search fallback trigger said "degraded" where SCP P4 / PRD FR-1 say "unavailable" (EXPERIENCE.md, tenant-data-grid). Fix applied: "unavailable or degraded".

## Reviewer files

- `review-scp-consistency.md` — this run (2026-07-19)
- `review-rubric.md`, `review-accessibility.md` — historical (2026-06-02 finalize; not re-run)
