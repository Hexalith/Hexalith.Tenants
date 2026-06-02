# Input Reconciliation — Responsive Layout & Visual System

**Source spec:** `docs/tenants-ui-responsive-layout-and-visual-system-spec.md` (Story 9.6)
**Checked against:** PRD `§5` (Information Architecture & Visual Language), with cross-checks to PRD `§9`, `§13`, `§16` and `addendum.md` `§B`/`§D`.
**Scope of this review:** only what the spec contains that the PRD + addendum **missed, dropped, or misrepresented**. Items the PRD already covers adequately (Fluent-as-authority, semantic-roles-not-hex, calm-operations-console tone, no-color-only, fail-closed responsive rule, anchoring command status to context / lifecycle-never-a-nav-area, FC-TOK "do not assert a token name as ready") are intentionally **not** listed.

---

## GAP 1 — Exact breakpoint set is absent (incl. the "wide desktop" tier)

- **Spec location:** §5.1 ("Breakpoints (verbatim)") and §7 rule 8; AC5 (§10 table). Verbatim: mobile **320–767px**, tablet **768–1023px**, desktop **1024px and above**, wide desktop **1440px and above** — "These are the layout rule."
- **PRD state:** §5.3 names the three form factors (desktop-first / tablet / mobile) but gives **no numeric breakpoints** and **no "wide desktop" tier at all**. The numbers that do appear in the PRD (§9: 1024/1366/1440 + wide, 768/1024, 375/430) are explicitly **testing widths**, which the spec (§5.3) deliberately separates from the breakpoint *rule* and defers to Story 9.7. The PRD has imported the test-evidence widths but dropped the actual layout-rule breakpoints.
- **Severity:** HIGH (a core AC5 deliverable; the breakpoint set is the one quantitative contract in the spec, and the 4th "wide desktop" tier is entirely missing).
- **Suggested PRD fix:** In §5.3, state the four breakpoints verbatim as the layout rule (mobile 320–767px, tablet 768–1023px, desktop 1024px+, wide desktop 1440px+), and note these are distinct from the §9 responsive *test* widths owned by acceptance evidence.

## GAP 2 — "Never drop a safety-critical column" DataGrid invariant is weakened

- **Spec location:** §5.2 and §7 rule 9. The rule is hard: at narrow widths, **column dropping must never remove a safety-critical column** — identity, status, freshness, role, risk — prefer horizontal scroll or row-detail expansion; composes the Story 9.2 invariant that sort/pagination must never hide pending/stale indicators.
- **PRD state:** §5.3 says tablet tables are "preserved via scroll/column-priority" — it states the *mechanism* but drops the *invariant* and the *named protected columns*. As written, "column-priority" could be read as permitting a safety-critical column to be deprioritized off-screen, which the spec forbids.
- **Severity:** HIGH (this is a truth/safety guarantee, the product's core trust proposition applied to responsive behavior — not a styling preference).
- **Suggested PRD fix:** In §5.3, add that column priority/scroll must never hide safety-critical state (identity, status, freshness, role, risk); narrow widths use horizontal scroll or row-detail expansion instead of dropping those columns.

## GAP 3 — Card dashboards, hero-scale type, advanced visual modes, and per-state color literals are not flagged as OUT of the first visual slice

- **Spec location:** §7.1 ("First-slice scope boundary") and §2.3 ("Explicitly forbidden … marketing-style card dashboards and hero-scale type"). Out of the first slice: **decorative dashboards, branded palettes, hero-scale type, grouped/advanced visual modes, and bespoke per-state color literals** — unless product/UX explicitly promotes them.
- **PRD state:** §5.2 captures "no branded palette" and "not marketing," and §13 lists "grouped/session audit mode … advanced analytics." But the PRD nowhere states that **card/decorative dashboards, hero-scale type, advanced/grouped visual modes, and per-state color literals** are explicitly excluded from the first visual slice. The spec treats these as a named scope boundary; the PRD leaves the visual slice's exclusions implicit.
- **Severity:** MEDIUM (scope-boundary omission; risks a downstream story re-introducing a card dashboard or hero type as "obviously fine").
- **Suggested PRD fix:** In §5.2 (or §13 Non-Goals), add an explicit first-visual-slice exclusion line: no card/decorative dashboards, no hero-scale type, no grouped/advanced visual modes, no bespoke per-state color literals — unless product/UX promotes them.

## GAP 4 — Explicit prohibition of decorative card grids and the "whitespace groups meaning, not drama" intent is dropped

- **Spec location:** §2.3. Prefer **tables, split views, tabs, side panels, dialogs, inline status regions over decorative card grids**; "Whitespace groups meaning, not drama"; "Tenants is an operational console, not a marketing site or card dashboard."
- **PRD state:** §5.2 says "compact density" and "not marketing" but does **not** carry the positive preference list (tables/split views/tabs/side panels/dialogs/inline status over card grids) nor the qualitative "whitespace groups meaning, not drama" stance. This is exactly the kind of "feel" instruction that is easy to lose — it's the difference between a console and a dashboard, and the PRD's "compact density" alone does not encode it.
- **Severity:** MEDIUM (qualitative intent loss; reinforces GAP 3 — together they define the operational-console-not-dashboard feel).
- **Suggested PRD fix:** In §5.2, add that surfaces favor tables, split views, tabs, side panels, dialogs, and inline status regions over decorative card grids, and that whitespace is used to group meaning, not for drama.

## GAP 5 — The 4px spacing rhythm and compact-density rule are not represented

- **Spec location:** §2.3. "Fluent-compatible spacing rhythm: a **4px base** with common **8px, 12px, 16px, 24px, 32px** steps"; "full-width operational surfaces with constrained readable inner regions where needed."
- **PRD state:** §5.2 says "compact density" with no spacing model; the full-width-vs-constrained inner-region notion is reflected only as the open `FC-LYT` question (PRD §16.3 / addendum §B), not as the spec's stated density rule. At product altitude a spacing scale may be considered mechanic, but the spec states it as a visual-language rule and the PRD's lone "compact density" under-specifies the intent.
- **Severity:** LOW (borderline mechanic; the qualitative "compact, full-width surfaces with constrained readable regions" intent is the load-bearing part).
- **Suggested PRD fix:** In §5.2, note a Fluent-compatible 4px spacing rhythm and full-width operational surfaces with constrained readable inner regions (decision tied to the open `FC-LYT` contract).

## GAP 6 — Desktop-first nuance ("first *design target* is keyboard-and-mouse workstation") flattened; tablet "not gesture-redesigned" nuance dropped

- **Spec location:** §4.1 ("the first design target is keyboard-and-mouse workstation usage"; responsive behavior only *prevents breakage*) and §4.2 ("Touch targets remain large enough, but the product is not redesigned around gesture-heavy workflows").
- **PRD state:** §5.3 says "Desktop-first (the primary admin workstation…)" and "Tablet: navigation collapses, regions stack" — it states *that* desktop is primary but not the spec's framing that smaller screens exist only to *prevent breakage*, nor that tablet is deliberately **not** redesigned around gestures (touch targets sufficient, but no gesture-first workflows). Minor "feel" erosion of why responsiveness exists.
- **Severity:** LOW (intent nuance; PRD's blanket "desktop-first" is directionally safe).
- **Suggested PRD fix:** In §5.3, clarify that responsive behavior exists to prevent breakage (not to re-target smaller screens), and that tablet keeps adequately sized touch targets without being redesigned around gesture-heavy workflows.

---

### Items checked and judged ADEQUATELY covered (not gaps)

- Fluent UI as the visual authority; no separate branded palette (PRD §5.2, §17). ✔
- Meaning → semantic theme roles, never hard-coded colors (PRD §5.2, addendum §D). ✔
- Calm/professional/precise operations-console tone, system UI typography, modest hierarchy, plain-language labels (PRD §5.2). ✔
- No-color-only encoding, legible in light/dark/high-contrast/forced-colors (PRD §5.2, §9). ✔
- Fail-closed responsive rule for high-impact actions at insufficient width, with visible reason (PRD §5.3, CP-2). ✔
- Command status/lifecycle anchored to the affected row/panel; lifecycle never a primary nav area (PRD §5.1). ✔
- Mobile = read-only triage / lookup / audit reference only; no high-impact command flows (PRD §5.3, §13). ✔
- Layout stable / reserves space to avoid shift; destructive/warning styling used sparingly (PRD §5.2). ✔
- FC-TOK readiness truth — tokens missing, Fluent/FC badges only as named fallback, "do not assert a token name as available," pinned Fluent v5 verification (addendum §B, §D). ✔
