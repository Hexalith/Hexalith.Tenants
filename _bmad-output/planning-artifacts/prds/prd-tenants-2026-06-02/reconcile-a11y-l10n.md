# Input Reconciliation — Accessibility & Localization

**Source spec:** `docs/tenants-ui-accessibility-localization-and-acceptance-evidence-spec.md`
**Reconciled against:** PRD §9 (Accessibility & Localization) + addendum.md
**Date:** 2026-06-02
**Scope of this check:** only obligations the spec carries that the PRD + addendum dropped, weakened, or misrepresented. Items the PRD already covers adequately are intentionally omitted.

## Summary of preservation status

PRD §9 preserves the core a11y/l10n contract well: exact WCAG framing (2.1 AA baseline / conditional 2.2 AA with no unconditional promise), keyboard/focus basics, screen-reader and live-region politeness rules (assertive reserved; never announce unconfirmed success), NVDA + browser/SR pairing, the six required acceptance scenarios, no-runtime-sentence-fragment-assembly with named placeholders, resource-ownership openness, and the responsive testing widths. The gaps below are the obligations that did NOT survive intact.

---

## Gap 1 — Ready-gate / evidence-citation rule is absent from §9

**Spec location:** §6.4 "Ready-gate rule"; §6 lead-in; reinforced by §9 (Future Implementation Story Rules, items 1–11) and Story 9.6 deferred-decision linkage.

**What the spec obligates:** A Phase 2 UI story may NOT be marked `ready` or `ready-with-approved-fallback` until the applicable accessibility, localization, responsive, AND documentation/reference evidence is cited. If reusable FrontComposer evidence is unavailable, an approved row-specific fallback must explicitly record five things: (1) keyboard/focus/live-region behavior, (2) localizable copy responsibility, (3) documentation/reference evidence, (4) replacement path, and (5) owner approval.

**What the PRD has:** PRD §9 has no readiness gate at all. The nearest coverage is §14.3 ("a phase item promotes only when its FrontComposer dependencies resolve or an approved fallback is recorded") and addendum §B ("a story promotes to `ready` only when its `blockedBy` set empties or a named approved fallback is recorded"). Both are dependency-resolution gates, not the spec's evidence-citation gate, and neither requires citing a11y/l10n/responsive/doc evidence nor enumerates the five fallback record items.

**Severity:** High — this is the operative acceptance gate the whole spec exists to establish (the evidence gate Story 9.6 deferred to 9.7); losing it removes the mechanism that ties all other §9 obligations to story readiness.

**Suggested PRD fix:** Add a "Ready-gate" bullet to §9: a UI story is not `ready`/`ready-with-approved-fallback` until applicable accessibility, localization, responsive, and documentation/reference evidence is cited; any approved fallback must record keyboard/focus/live-region behavior, localizable-copy responsibility, documentation/reference evidence, replacement path, and owner approval.

---

## Gap 2 — Documentation/reference (FC-DOC) evidence dropped from the acceptance-evidence definition of done

**Spec location:** §6.4 ("documentation/reference evidence is cited"); §9 rule 11 ("Documentation/reference evidence through `FC-DOC` or an approved equivalent reference path").

**What the spec obligates:** Documentation/reference evidence (via `FC-DOC` or an approved equivalent path) is a first-class member of the evidence set required before a story is ready.

**What the PRD has:** PRD §9 "Acceptance evidence (definition of done)" enumerates keyboard, screen-reader, automated checks, forced-colors/high-contrast, reduced-motion, contrast, live-region, focus return, and hover-free disabled explanations — but omits documentation/reference evidence entirely. Addendum §B notes FC-DOC is "Required for 'ready'", but the PRD-body definition of done does not list it.

**Severity:** Medium — a named evidence category in the spec's definition of done is missing from the PRD's definition of done.

**Suggested PRD fix:** Add "documentation/reference evidence (via FC-DOC or an approved equivalent reference path)" to the §9 acceptance-evidence definition-of-done list.

---

## Gap 3 — Narrow-width behavior evidence dropped from responsive testing

**Spec location:** §6.1 ("plus horizontal table overflow, navigation collapse, and command preview/dialog behavior at narrow widths"); §9 rule 9 ("Full responsive testing widths AND narrow-width behavior evidence").

**What the spec obligates:** Responsive evidence must cover not only the enumerated widths but explicitly horizontal table overflow, navigation collapse, and command preview/dialog behavior at narrow widths.

**What the PRD has:** PRD §9 lists the widths (desktop 1024/1366/1440 + wide; tablet 768/1024; mobile 375/430) but stops there — the explicit narrow-width behavior obligations (table overflow, nav collapse, dialog/preview behavior) are not stated as required evidence.

**Severity:** Medium — concrete required behaviors are dropped, leaving the responsive evidence as widths-only.

**Suggested PRD fix:** Append to the §9 responsive line: "plus horizontal table overflow, navigation collapse, and command-preview/dialog behavior at narrow widths."

---

## Gap 4 — "Complete or exit every workflow" keyboard obligation not stated

**Spec location:** §2 ("Keyboard users must be able to complete or exit every modal, preview, table, and command workflow."); §9 rule 2.

**What the spec obligates:** A standalone guarantee that keyboard-only users can both complete and exit every modal, preview, table, and command workflow — not merely reach elements and trap/return focus.

**What the PRD has:** PRD §9 keyboard bullet covers reachability, focus order, visible focus, modal trap, safe escape, and focus return, but does not state the completion/exit-of-every-workflow guarantee.

**Severity:** Low-Medium — the operational outcome (no keyboard dead-ends in any workflow) is left implicit.

**Suggested PRD fix:** Add to the §9 keyboard bullet: "keyboard-only users can complete or exit every modal, preview, table, and command workflow."

---

## Gap 5 — "Tooltips may supplement but cannot be the only explanation" nuance lost

**Spec location:** §2 ("Disabled or unavailable action explanations must be reachable without mouse hover. Inline-visible reasons are required; tooltips may supplement but cannot be the only explanation."); also Glossary parity with the Story 9.3 Unavailable Action Reason pattern.

**What the spec obligates:** Inline-visible reasons are required; a tooltip is permitted only as a supplement and may never be the sole carrier of a disabled/unavailable explanation.

**What the PRD has:** PRD §9 says "hover-free disabled explanations" (acceptance evidence) and §7 FR-9 says reasons are "inline-visible (not hover-only)." This captures the hover-free requirement but not the explicit "tooltips may supplement but cannot be the only explanation" allowance/limit.

**Severity:** Low — substantially covered by "inline-visible / not hover-only"; only the supplement-vs-sole-carrier nuance is missing.

**Suggested PRD fix:** Optional clarifying clause in §9: "tooltips may supplement an inline reason but may never be its only carrier."

---

## Notes on items checked and found adequately covered (not gaps)

- WCAG 2.1 AA baseline / conditional 2.2 AA with explicit "no unconditional 2.2 promise" — PRD §9 line 1 matches spec §1/§9.1.
- Pinned-package verification (`Microsoft.FluentUI.AspNetCore.Components 5.0.0-rc.3-26138.1`) and FC-A11Y `needs-confirmation` — addendum §D and §B.
- Live-region politeness, assertive reservation, never-announce-unconfirmed-success, SignalR-as-nudge — PRD §9 screen-reader bullet + CP-3/CP-4.
- Absolute/exact timestamp labels (not relative-only) — PRD §9 screen-reader bullet.
- Stable automation selectors / no reliance on row text or color — PRD NFR-4.
- Localizable categories, culture-aware formatting, whole-string + named placeholders, no runtime sentence-fragment assembly — PRD §9 localization bullet.
- Resource-ownership openness (shell vs Tenants-owned) — PRD §9 + §16.4 + addendum §B.
- Support-safe labels (no payloads/tokens/stack traces/correlation ids/raw EventStore metadata/PII) and RFC 7807 boundary composition — PRD §10 + addendum §D. (Spec names `RejectionToHttpStatusMapper`; PRD/addendum keep this in mechanics, which is acceptable for product altitude.)
- Forced-colors/high-contrast across light/dark/high-contrast/forced-colors; color never sole signal; reduced-motion independence — PRD §9 + §5.2.
- NVDA + at least one browser/SR pairing; the six required acceptance scenarios — PRD §9 acceptance-evidence line matches spec §6.2/§6.3.
- Responsive testing widths (the numeric list) — PRD §9 matches spec §6.1 (the dropped part is the narrow-width behaviors; see Gap 3).
