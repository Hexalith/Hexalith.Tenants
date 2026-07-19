---
name: Tenants Management UI
description: Trust-first operations console for the Hexalith Tenants domain. Inherits Microsoft Fluent UI Blazor v5 via the FrontComposer shell; this DESIGN.md specifies the semantic-role + density delta only — no bespoke brand palette.
status: final
sources:
  - "{planning_artifacts}/prds/prd-tenants-2026-06-02/prd.md"
  - "{planning_artifacts}/prds/prd-tenants-2026-06-02/addendum.md"
  - "{planning_artifacts}/sprint-change-proposal-2026-07-15.md"
updated: 2026-07-19
colors:
  # No hex. Every status role inherits a Fluent v5 BadgeColor semantic role by NAME.
  # Fluent owns the rendered CSS token value; verify exact values against the pinned
  # package (5.0.0-rc.4-26180.1, centrally consumed) at build. NEVER restate a hex
  # here, and NEVER assert a token name not in the verified BadgeColor vocabulary
  # (Brand · Danger · Important · Informative · Severe · Subtle · Success · Warning).
  status-success:
    note: 'Inherit Fluent BadgeColor.Success — RESERVED for PROVEN truth only (current · confirmed · audit available · tenant active).'
  status-informative:
    note: 'Inherit Fluent BadgeColor.Informative — in-flight, not yet proven (refreshing · previewed · submitted · accepted · projection pending · audit pending).'
  status-warning:
    note: 'Inherit Fluent BadgeColor.Warning — usable-with-friction, caution tier 1 (aging · timeout · audit delayed).'
  status-severe:
    note: 'Inherit Fluent BadgeColor.Severe — blocks the action but is not an error, caution tier 2 (stale · tenant disabled · audit unavailable).'
  status-danger:
    note: 'Inherit Fluent BadgeColor.Danger — refusal / failure / high-impact destructive, caution tier 3, used sparingly (rejected · failed · risk high).'
  status-important:
    note: 'Inherit Fluent BadgeColor.Important — must act, state uncertain (freshness unknown · lifecycle unknown · authorization blocked).'
  status-subtle:
    note: 'Inherit Fluent BadgeColor.Subtle — benign neutral (eligible · already applied · duplicate · risk low · missing implementation support · not-yet-available).'
  brand-accent:
    note: 'Inherit Fluent BadgeColor.Brand / theme accent — chrome and primary-action accents ONLY. NEVER a status.'
typography:
  # Inherit the Fluent UI Blazor / system-UI type ramp wholesale. Fluent owns the
  # rendered sizes/weights/line-heights; reference roles by name, do not restate px.
  body:
    note: 'Inherit Fluent / system-UI body ramp — plain-language operational copy.'
  label:
    note: 'Inherit Fluent / system-UI label ramp — column headers, field labels, badge text.'
  heading:
    note: 'Inherit Fluent / system-UI heading ramp — modest hierarchy (surface/panel titles); no hero/display scale.'
  caption:
    note: 'Inherit Fluent / system-UI caption ramp — secondary context, counts, reason text.'
  mono:
    note: 'Inherit Fluent / system monospace ramp — TenantId / UserId / support-safe references and absolute timestamps, so glyph-similar ids stay scannable and copyable.'
rounded:
  note: 'Inherit Fluent UI Blazor shape/corner radii wholesale (BadgeShape.Rounded for status badges; Fluent defaults elsewhere). No bespoke radii; verify exact values against the pinned package at build.'
spacing:
  # Fluent-compatible 4px rhythm. Reserve space to keep layout stable (no shift).
  '1': 4px
  '2': 8px
  '3': 12px
  '4': 16px
  '6': 24px
  '8': 32px
components:
  truth-state-badge:
    color: '{colors.status-success} | {colors.status-informative} | {colors.status-warning} | {colors.status-severe} | {colors.status-danger} | {colors.status-important} | {colors.status-subtle}'
    appearance: 'Fluent BadgeAppearance.Tint (default) · BadgeAppearance.Filled (Danger + Severe)'
    shape: '{rounded} (BadgeShape.Rounded)'
    icon: 'Fluent IconStart + IconLabel (aria) from the verified per-state set in the truth-state-badge component spec — color is NEVER the sole signal'
    gap: '{spacing.1}'
  consequence-preview:
    color: '{colors.status-warning} | {colors.status-severe} | {colors.status-danger} | {colors.status-subtle}'
    layout: 'Constrained inner region; inline structured text fallback for missing FC-CNS, carrying the full 10-item content set'
    padding: '{spacing.4}'
    rowGap: '{spacing.3}'
  command-lifecycle-panel:
    color: '{colors.status-informative} | {colors.status-success} | {colors.status-danger} | {colors.status-subtle}'
    layout: 'Fluent FluentMessageBar · MessageBarLayout.Notification — inline, anchored to the affected row/panel'
    padding: '{spacing.4}'
    stepGap: '{spacing.2}'
  unavailable-action-reason:
    color: '{colors.status-important} | {colors.status-severe} | {colors.status-subtle}'
    layout: 'Inline (never hover-only); 6 reason categories'
    gap: '{spacing.2}'
  audit-evidence-receipt:
    color: '{colors.status-success} | {colors.status-informative} | {colors.status-warning} | {colors.status-severe} | {colors.status-subtle}'
    layout: 'Fluent FluentMessageBar · MessageBarLayout.Notification — support-safe; BFF-assembled, redacted view model (from NarrativePayload)'
    padding: '{spacing.4}'
    fieldGap: '{spacing.3}'
    idType: '{typography.mono}'
  tenant-data-grid:
    base: 'Fluent FluentDataGrid (cursor pagination)'
    pinnedColumns: 'DataGridColumnPin.Start on identity · status · freshness (safety-critical)'
    statusCell: '{components.truth-state-badge}'
    rowPadding: '{spacing.3}'
  member-table:
    base: 'Fluent FluentDataGrid (read-only; must not imply mutation)'
    pinnedColumns: 'DataGridColumnPin.Start on identity · role · status · freshness; risk pinned where shown'
    statusCell: '{components.truth-state-badge}'
    actionCell: '{components.unavailable-action-reason}'
    rowPadding: '{spacing.3}'
  audit-data-grid:
    base: 'Fluent FluentDataGrid (flat, stably-ordered, cursor-paginated; FC-AUD timeline fallback)'
    pinnedColumns: 'DataGridColumnPin.Start on timestamp · actor · outcome'
    statusCell: '{components.truth-state-badge}'
    timestampCell: '{typography.mono}'
    rowPadding: '{spacing.3}'
  primary-command-button:
    color: '{colors.brand-accent}'
    appearance: 'Fluent FluentButton Appearance.Accent — chrome accent, never a status carrier'
    paddingX: '{spacing.4}'
    paddingY: '{spacing.2}'
  destructive-control:
    color: '{colors.status-danger}'
    appearance: 'Fluent FluentButton, low-emphasis until gated — NOT a primary/casual button; gated behind {components.consequence-preview} + confirmation'
    paddingX: '{spacing.4}'
    paddingY: '{spacing.2}'
---

## Brand & Style

The Tenants Management UI is a **calm, precise operations console — honesty about state**. It is a workstation for people acting on real tenants, members, and access under incident pressure, in an eventually-consistent, event-sourced system. Its single overriding aesthetic obligation is *trust*: the surface must never look more certain than the system actually is. Success is shown only when something is proven; in-flight work looks in-flight; a thing the UI cannot verify is never dressed up as done. Whitespace groups meaning rather than adding drama; nothing is decorative that could be mistaken for state.

This is a **Fluent-delta / inheritance spec, not a from-scratch visual identity.** Microsoft Fluent UI Blazor v5 (pinned `5.0.0-rc.4-26180.1`, centrally consumed) is the visual authority, reached through the **Hexalith.FrontComposer** shell. There is **no bespoke brand palette to invent.** Exactly as the shadcn-based reference inherits shadcn wholesale and specifies only its brand-layer delta, this document inherits Fluent's components, type ramp, shapes, and elevation as the contract — and specifies only the *delta a trust-first operations console requires*: a fixed mapping of meaning onto Fluent **semantic roles**, a strict no-color-only rule, a compact 4px density, and ten domain components that compose Fluent primitives. Everything not named here inherits Fluent as-is; customizing Fluent's primitives beyond this delta is against the discipline.

Two consequences run through every section below. First, **meaning maps to Fluent semantic roles, never to hard-coded hex** — this file references `BadgeColor` roles *by name* and never restates or invents a color value. Second, the exact CSS token values, component parameters, and ARIA behaviors **must be verified against the pinned package at build**; this document never asserts a Fluent token name outside the verified vocabulary, and any genuine gap is marked `[ASSUMPTION]`.

Behavioral specification — when each state appears, the live-region politeness, focus and recovery flows, the command lifecycle transitions, validation-before-preview — lives in **EXPERIENCE.md**. This file governs how those states *look*. The two are designed to be read together and share the token names defined in this frontmatter.

## Colors

This is the centerpiece of the spec, and the one place the delta is almost entirely semantic. There is **no palette to introduce** — Drift added two brand colors on top of shadcn; this console adds *zero*. Instead it pins eight meanings onto Fluent's verified `BadgeColor` vocabulary (Brand · Danger · Important · Informative · Severe · Subtle · Success · Warning) and forbids any other use of color to mean state. Bind to the semantic role; never to a hex.

> ### THE LOCKED INVARIANT — Success is reserved for PROVEN truth
>
> `{colors.status-success}` (Fluent **Success**) may appear **only** for states that are proven against the source-of-truth projection: freshness `current`, command `confirmed`, `audit available`, tenant `active`. **Every in-flight or pending state — `accepted`, `submitted`, `previewed`, `projection pending`, `audit pending`, `refreshing` — is `{colors.status-informative}`, never Success.** This is the honesty firewall: it enforces CP-3 (never show success — in styling, copy, or announcement — before the projection confirms it) **at the color level**, so the system literally cannot turn green until the truth is in. If you are ever unsure whether a state is proven, it is not Success. `accepted` is the most tempting trap and is explicitly Informative.

**The 3-tier caution ramp.** Caution escalates through three distinct roles, never collapsing into one "bad" color:

- **Tier 1 — `{colors.status-warning}` (Fluent Warning): usable, with friction.** The action can still proceed, but the operator should slow down. States: freshness `aging`, command `timeout`, `audit delayed`. This is the "proceed deliberately" tier.
- **Tier 2 — `{colors.status-severe}` (Fluent Severe): blocks the action, but is not an error.** Something legitimate stands in the way; the path forward is to resolve it (refresh, enable), not to read it as a failure. States: freshness `stale`, tenant `disabled`, `audit unavailable`. Severe is the most under-used-elsewhere role and the one that keeps "blocked" from being mis-read as "broken."
- **Tier 3 — `{colors.status-danger}` (Fluent Danger): refusal, failure, or high-impact destruction.** Used **sparingly**, so that when it appears it carries full weight. States: command `rejected`, `failed`, risk `high`, and the destructive-confirmation moment. Danger is loud on purpose; spending it on routine caution would deafen it.

**Per-role story.**

- **`{colors.status-success}` — Proven (Fluent Success).** The only "all clear." Earned, never assumed. Carries `current`, `confirmed`, `audit available`, tenant `active`. Governed entirely by the firewall above.
- **`{colors.status-informative}` — In flight (Fluent Informative).** The workhorse of an eventually-consistent UI: the system is doing something and has *not yet* proven the result. `refreshing`, `previewed`, `submitted`, `accepted`, `projection pending`, `audit pending`. Reading Informative, an operator knows "wait — this is not done." It is the visual buffer that makes optimistic-looking UIs honest.
- **`{colors.status-warning}` — Caution tier 1 (Fluent Warning).** See ramp. `aging`, `timeout`, `audit delayed`.
- **`{colors.status-severe}` — Caution tier 2 (Fluent Severe).** See ramp. `stale`, tenant `disabled`, `audit unavailable`, and `degraded` (a success-prohibited partial-availability state).
- **`{colors.status-danger}` — Caution tier 3 (Fluent Danger).** See ramp. `rejected`, `failed`, risk `high`. Reserved and rare.
- **`{colors.status-important}` — Must act, state uncertain (Fluent Important).** Distinct from both caution and in-flight: the system *cannot tell you* the state and you must not assume the benign reading. Freshness `unknown`, lifecycle `unknown`, authorization `blocked`, and `unable to verify` (a success-prohibited reconciliation outcome). Important says "stop and find out," which is exactly the fail-closed posture the PRD demands when freshness is unmeasurable.
- **`{colors.status-subtle}` — Benign neutral (Fluent Subtle).** The quiet, no-drama outcomes that are *fine* and need no alarm: `eligible`, `already applied`, `duplicate`, risk `low`, plus the honest capability-gap states `missing implementation support` and "not yet available." Subtle keeps "this already happened / nothing to do here" from ever reading as either success or failure.
- **`{colors.brand-accent}` — Chrome & primary action ONLY (Fluent Brand).** The single non-semantic color. It dresses navigation, selection, and the primary command button. **It is never a status.** Brand on a row would imply meaning where there is none; status roles never bleed into chrome, and Brand never bleeds into status. This is the same discipline as the reference's "accent means *live*, nothing else" — here, Brand means *chrome*, nothing else.

**No-color-only is mandatory and absolute.** Color is never the sole signal. Every status is rendered as `BadgeColor` **+** a Fluent `IconStart` glyph **+** an `IconLabel` / visible text label, legible in light, dark, high-contrast, and **forced-colors** modes (where the badge fill may be overridden by the OS and the icon + text must still carry the meaning). A reader who cannot perceive the hue must lose **nothing**. The role-to-appearance rule adds visual emphasis but is **not** an accessibility differentiator: `BadgeAppearance.Tint` is the calm default for all roles; `BadgeAppearance.Filled` is reserved for **Danger** and **Severe** to draw the eye. Because the OS overrides the fill in forced-colors, `Filled` is **not** counted toward no-color-only — the cross-role-distinct **icon + text** carry the meaning when color and fill are gone.

Avoid, hard: introducing any brand/sentiment palette; hard-coding or restating a hex value; using Brand for status or a status role for chrome; using `{colors.status-success}` for anything not projection-proven; collapsing the three caution tiers into one color; relying on hue alone. (Mapping rationale and the verified `BadgeColor` vocabulary: `.decision-log.md`, 2026-06-02 entries "status semantic mapping (locked)" and "verified Fluent UI Blazor v5 vocabulary.")

## Typography

Typography inherits the **Fluent UI Blazor / system-UI ramp wholesale** — Fluent owns the rendered sizes, weights, and line-heights, and this spec references roles by name (`{typography.body}`, `{typography.label}`, `{typography.heading}`, `{typography.caption}`) rather than restating values. The delta is one of *restraint and one added role*, not new type design.

**Modest hierarchy.** This is "professional, calm, precise — not marketing": there is **no hero or display scale.** `{typography.heading}` is used at a modest step for surface and panel titles; the working text is `{typography.body}` in plain language; `{typography.label}` carries column headers, field labels, and badge text; `{typography.caption}` carries secondary context, counts, and reason text. Hierarchy comes from layout, weight, and the 4px rhythm — not from large type.

**The one delta role: `{typography.mono}` for identifiers and timestamps.** TenantId and UserId are **meaningful caller-supplied strings (not ULIDs)** and support-safe references are exact tokens; rendering them in a monospace role keeps glyph-similar characters (`0`/`O`, `1`/`l`/`I`) distinguishable and makes "copy full id" (FR-7) visibly faithful to the literal string. The same `{typography.mono}` role carries **absolute timestamps** — the accessibility floor mandates absolute, not relative-only, times, so audit and freshness times are shown as fixed, culture-formatted, copyable values, not "3 minutes ago."

All labels — state names, role names, timestamps, reasons, recovery verbs — are **localizable whole strings with named placeholders** (no runtime sentence-fragment assembly) and culture-aware formatting; layout reserves room for the longest localized label so translation never causes truncation or shift.

## Layout & Spacing

**Full-width operational surfaces, with constrained inner regions.** Tables and work areas span the viewport — this is a dense admin workstation, and full width is what best satisfies "safety-critical columns never drop" with the least horizontal-scroll risk. Forms, consequence previews, command-lifecycle panels, and dialogs render inside **constrained, readable inner regions** within that full-width frame, anchored to the row or panel they concern.

> Story 1.0 update (2026-06-05, historical evidence — not a readiness waiver): full-width-with-constrained-inner-regions remains the UX **intent**, and the FrontComposer shell layout contract **FC-LYT** is confirmed by `_bmad-output/implementation-artifacts/story-1-0-spike-note-2026-06-05.md`. Implementing stories still verify exact shell behavior and responsive evidence at build.

**4px spacing rhythm.** All spacing follows the Fluent-compatible scale: `{spacing.1}` 4px · `{spacing.2}` 8px · `{spacing.3}` 12px · `{spacing.4}` 16px · `{spacing.6}` 24px · `{spacing.8}` 32px. Compact density throughout (tight, scannable rows); the larger steps separate major regions, the smaller steps bind tightly-related elements. The positive layout preference is **tables, split views, tabs, side panels, dialogs, and inline status regions over decorative card grids.**

**Stable layout — reserve space, never shift.** Status cells, action cells, freshness markers, and reason slots **reserve their footprint** whether or not content is currently present, so a row never reflows when a badge updates, an action becomes available, or a localized label is longer. Row actions hold stable width and placement under data, sort, and page change.

**Pinned safety-critical columns.** Under full-width horizontal scroll, the columns that must never disappear — **identity, status, freshness, role, risk** — are pinned via `DataGridColumnPin.Start` so they remain visible regardless of scroll position. "Column priority" never means a safety-critical column may go off-screen; if a width genuinely cannot preserve full safety context for a high-impact action, that action becomes *unavailable with a visible reason* rather than rendering unsafely (the fail-closed responsive rule; behavioral detail in EXPERIENCE.md). Pinned columns read with a subtle leading divider/elevation once the grid scrolls, plus an accessible "pinned" indication (not color-only); the treatment is consistent across all three grids.

## Elevation & Depth

Inherit Fluent's elevation language; add **nothing**. Depth is minimal and calm — surfaces are distinguished by Fluent's tonal layering, not by dramatic shadow, and elevation is **not** used as a primary hierarchy device (hierarchy comes from layout, the 4px rhythm, and modest type). Shadow appears only where Fluent already uses it for a transient overlay (dialog, popover, the focus-trapped confirmation). The discipline mirrors the reference examples: "Fluent's elevation is correct; this product reads quieter, not deeper."

## Shapes

Inherit Fluent's corner radii wholesale (`{rounded}` note); there are **no bespoke radii.** Status badges use Fluent **`BadgeShape.Rounded`** for a soft, legible chip; everything else takes Fluent defaults. The aesthetic is calm and unembellished — shapes carry no meaning of their own, so they never compete with the semantic-color + icon + text signal that does. Verify the exact radius values against the pinned package at build.

## Components

Ten domain components compose Fluent primitives. The first five are the **trust-specific** core and carry the most detail; the three grids are `FluentDataGrid` configurations with pinned safety columns; the last two are the action controls. Each lists its anatomy, the semantic role(s) it draws from, sizing via `{spacing.*}`, and state appearance. **When** each state appears, the announcement politeness, and the recovery flow are the **behavioral spec in EXPERIENCE.md**; below is purely visual.

### truth-state-badge

The atom of the whole system — one component that fuses **color + icon + text** so no status is ever color-only. It renders the canonical state vocabularies (freshness, command lifecycle, projection confirmation, audit availability, authorization, risk) as a single Fluent `FluentBadge`.

- **Anatomy:** `IconStart` glyph + visible text label + `IconLabel` (aria name), in `BadgeShape.Rounded`, with `{spacing.1}` between icon and label. The text is the state token (localized whole string); the icon is a fixed per-state glyph from the **verified icon set below** — the icon reinforces the role so color-blind and forced-colors users get the same signal.
- **Color:** the full status set, by state — `{colors.status-success}` (proven), `{colors.status-informative}` (in-flight), `{colors.status-warning}` / `{colors.status-severe}` / `{colors.status-danger}` (caution tiers 1-3), `{colors.status-important}` (uncertain), `{colors.status-subtle}` (benign). Bound to semantic role, never hex. Governed by the Success-is-proven firewall.
- **Appearance / state:** `BadgeAppearance.Tint` is the default for every role (calm). `BadgeAppearance.Filled` for **Danger** and **Severe** only, giving the action-stopping tiers extra non-chromatic weight. Distinct states are **never collapsed** into one badge — `accepted`, `confirmed`, and `audit available` are three different badges, never merged.
- **Sizing / stability:** occupies a reserved cell footprint so updating the badge never reflows the row.

**Verified status icon set.** Every name below was confirmed present in `5.0.0-rc.3-26138.1` (≈ MCP `5.0.0.26139`) via the Fluent icon catalog; the pin is now **`5.0.0-rc.4-26180.1`, centrally consumed** — re-verify names, sizes, and variants against the rc.4 package at build. The set is **cross-role distinct by design** — no single glyph is shared by two different role colors, so the meaning survives **forced-colors** (where the OS may drop the badge fill, leaving glyph + text to carry it). **Pin status-badge glyphs to size 20:** several (`ClipboardClock`, `ShieldProhibited`, `DocumentProhibited`, `ClockToolbox`, `ClockDismiss`) ship **no Size16 variant**, so a 16px dense-row badge would silently drop them. The bare `Warning` triangle denotes risk `high` only; the Clock-family glyphs (`Clock` aging · `ClockAlarm` stale · `ClockDismiss` timeout · `ClockWarning` audit-delayed · `ClipboardClock` audit-pending · `ClockToolbox` not-built) are each a distinct glyph. Verify exact size/variant at build — the **names** are checked.

| State(s) | Role | Fluent icon (`Icons.Regular.Size20.*`) |
|---|---|---|
| `current` · tenant `active` · `confirmed` | Success | `CheckmarkCircle` |
| `audit available` | Success | `DocumentCheckmark` |
| `refreshing` | Informative | `ArrowClockwise` |
| `submitted` · `accepted` · `pending` · `projection_pending` | Informative | `ArrowClockwise` — **never a checkmark; in-flight is not done** |
| `previewed` | Informative | `Document` |
| `audit pending` / `audit_pending` | Informative | `ClipboardClock` |
| `aging` | Warning | `Clock` |
| `timeout` | Warning | `ClockDismiss` |
| `audit delayed` | Warning | `ClockWarning` |
| risk `high` | Danger | `Warning` |
| `stale` | Severe | `ClockAlarm` |
| tenant `disabled` | Severe | `Power` (turned-off, not a prohibition — distinct from `rejected`) |
| `audit unavailable` | Severe | `DocumentProhibited` |
| `degraded` | Severe | `ShieldError` |
| `rejected` | Danger | `Prohibited` |
| `failed` | Danger | `DismissCircle` |
| freshness/lifecycle `unknown` | Important | `QuestionCircle` |
| authorization `blocked` · `missing permission` | Important | `ShieldProhibited` |
| `unable to verify` | Important | `ShieldQuestion` |
| `eligible` · `already applied` · `duplicate` · risk `low` | Subtle | `CheckmarkCircleHint` (`Shield` for risk `low`) |
| `missing implementation support` · "not yet available" | Subtle | `ClockToolbox` |

The 6 `unavailable-action-reason` categories reuse these by role: `stale data` → `ClockAlarm` (Severe); `missing permission` → `ShieldProhibited` (Important); the four capability-gap categories (`missing lifecycle support` / `missing consequence preview` / `missing audit proof` / `high-impact flow not ready`) → `ClockToolbox` (Subtle).

### consequence-preview

The pre-submission "here is exactly what will happen" panel shown before every destructive or high-impact action (and every config edit). It is a **constrained inner region** of structured text. Because `FC-CNS` (`<ConsequencePreview>`) is **missing**, the approved v1 form (Product/UX approval recorded 2026-06-03 — see the [Fallback Approval Record](../../fallback-approval-record-2026-06-03.md)) is an **inline structured-text fallback** that must carry the **full 10-item content set** — completeness is non-negotiable and the panel **fails closed** if any item is unavailable (it does not render a partial preview that could mislead).

- **Anatomy (the 10 items, each a labelled line, `{spacing.3}` row gap, `{spacing.4}` padding):** (1) tenant; (2) target user; (3) current role; (4) owner-count impact, incl. last-owner / drop-to-zero; (5) specific access being revoked/changed; (6) current freshness of the inputs; (7) recovery path afterward; (8) audit expectation; (9) target's platform standing (e.g. *also a global administrator*); (10) explicit **known consequences vs. known unknowns** — over-claiming is forbidden, so session/token invalidation reads as a *known unknown* unless proven.
- **Color:** mostly neutral text. Semantic color enters only where an item *is* a state: the freshness line uses `{colors.status-warning}` / `{colors.status-severe}` per the ramp; a *safe* owner-count drop reads `{colors.status-subtle}` (neutral) — only a last-owner / zero-owner impact escalates to `{colors.status-warning}` (warning + friction, never a hard block); a high-risk standing draws `{colors.status-danger}`; benign "already applies / no change" reads `{colors.status-subtle}`. Known-unknowns are visually marked as *unproven*, not colored as either success or failure.
- **State:** if inputs are incomplete the panel shows the blocking reason and **submission is disabled** (fail-closed). The known-vs-unknown split is always present, never elided.

### command-lifecycle-panel

Tracks a dispatched command **inline, anchored to the affected row/panel — never a nav area** — without ever overwriting confirmed projection data. Rendered as a Fluent `FluentMessageBar` in **`MessageBarLayout.Notification`** (title / message / actions on separate lines).

- **Anatomy:** a title line naming the action, a current-state line carrying a `{components.truth-state-badge}`, and a step sequence (`submitted → accepted → projection pending → confirmed`, then `audit pending → audit available`) with `{spacing.2}` between steps; `{spacing.4}` padding.
- **Color:** the in-flight steps are `{colors.status-informative}` — including **`accepted`**, which is *not* success. Only `confirmed` and `audit available` flip to `{colors.status-success}`. `rejected` / `failed` are `{colors.status-danger}`; `already applied` / `duplicate` are `{colors.status-subtle}`.
- **State:** states are **never collapsed** — the panel shows the real lifecycle position and never renders a "done" until the projection confirms it. Live SignalR notifications are freshness nudges only and never advance the panel to `confirmed`. The MessageBar's intent maps `Danger`→`Error` (see Do/Don't on token names). It coexists with — does not overwrite — the confirmed data on the row.

### unavailable-action-reason

The plain-language, **inline-visible (never hover-only)** explanation of *why* an action a user might expect is not currently available. Tooltips may supplement but can never be the only explanation.

- **Anatomy:** an icon + a short localized sentence, `{spacing.2}` gap, sitting in the row's reserved action slot in place of the action control. One of **6 reason categories:** `missing permission`; `stale data`; `missing lifecycle support` (→ FC-CMD); `missing consequence preview` (→ FC-CNS); `missing audit proof` (→ FC-AUD); `high-impact flow not ready` (unresolved backlog dependency).
- **Color:** `{colors.status-important}` for `missing permission` (authorization `blocked` — must act, uncertain); `{colors.status-severe}` for `stale data` (blocks the action); `{colors.status-subtle}` for the three capability-gap categories and "not yet ready" (benign `missing implementation support` family).
- **State:** always rendered where the action would be, so the absence is *explained in place*, and the slot's footprint is reserved so swapping action↔reason causes no shift.

### audit-evidence-receipt

The **support-safe** receipt for a recorded action — a view model **assembled and redacted by the server-side BFF** from a structured `NarrativePayload`; the rendered component receives only support-safe localized fields (never raw `NarrativePayload`, raw event payload, event bodies, command payloads, tokens, correlation ids, ETags, raw metadata, or PII). Rendered as a Fluent `FluentMessageBar` in **`MessageBarLayout.Notification`** so actor / target / outcome / reference stack clearly.

- **Anatomy (fields at `{spacing.3}` gap, `{spacing.4}` padding):** who acted · on whom · tenant scope · outcome · **absolute timestamp** · projection marker (read-model freshness) · audit/command **reference**. Ids and the reference render in `{typography.mono}`; the timestamp is absolute and culture-formatted.
- **Color:** outcome carries a `{components.truth-state-badge}` — `{colors.status-success}` only when `audit available` is genuinely proven; otherwise the honest audit state: `{colors.status-informative}` (`audit pending`), `{colors.status-warning}` (`audit delayed`), `{colors.status-severe}` (`audit unavailable`), `{colors.status-subtle}` (`missing implementation support`). **None of the not-yet-proven states is ever shown as success** — partial completion shows the actual lifecycle state, never pre-rendered proof.
- **State:** when evidence is unavailable, the receipt shows the honest fallback state and a copy-safe "reference or fallback state," never fabricated proof.

### tenant-data-grid

The default triage surface — a full-width Fluent `FluentDataGrid` with **cursor pagination (never offset/limit)**.

- **Anatomy / columns:** tenant identity, status, member count, owner count, pending state, and a `{components.truth-state-badge}` with freshness. Row padding `{spacing.3}`.
- **Pinned safety columns:** **identity · status · freshness** pinned `DataGridColumnPin.Start` so they never drop under horizontal scroll.
- **State:** renders six distinct, **non-collapsible** list states — loading, empty, filtered-empty, error, stale, degraded — each visually distinct (filtered-empty offers a filter reset; stale shows a freshness marker + refresh path; degraded explains what is unavailable). **Sorting or paging must never hide a pending or stale marker.** Empty is authorization-safe (no leak of out-of-scope tenants).

### member-table

The per-tenant access-review surface — a **read-only** Fluent `FluentDataGrid` that **must not imply mutation**.

- **Anatomy / columns:** member identity, role, owner count, status, freshness, orphan/disabled context, and a per-row action slot. Row padding `{spacing.3}`. Exposes accessible table semantics (headers, sort state, row relationships).
- **Pinned safety columns:** **identity · role · status · freshness** pinned `DataGridColumnPin.Start`; risk pinned where shown.
- **Components in cells:** status via `{components.truth-state-badge}`; the action slot shows the available action **or** a `{components.unavailable-action-reason}` (reserved footprint, no shift). Orphan / disabled context is flagged with the appropriate semantic role (`{colors.status-severe}` for a disabled-tenant member context).
- **State:** read-only styling throughout — affordances must not look like editable cells.

### audit-data-grid

The audit trail — a **flat, stably-ordered, cursor-paginated** Fluent `FluentDataGrid`, the approved interim form in place of `<AuditTimeline>` (FC-AUD missing; Product/UX approval recorded 2026-06-03 — see the [Fallback Approval Record](../../fallback-approval-record-2026-06-03.md)).

- **Anatomy / columns:** timestamp (absolute, `{typography.mono}`), actor, target, `AuditEventCategory`, outcome. Row padding `{spacing.3}`. Filters: date + `AuditEventCategory` (`Access` / `Administrative`).
- **Pinned safety columns:** **timestamp · actor · outcome** pinned `DataGridColumnPin.Start`.
- **State:** four distinct states — loading, empty, filtered-empty, error; stable ordering is preserved across paging. In MVP the whole audit area may render an honest "**not yet available**" placeholder (`{colors.status-subtle}`, `missing implementation support` family) rather than a broken surface, with the nav shape unchanged across phases.

### primary-command-button

The affirmative action control for **non-destructive** command flows.

- **Anatomy / color:** Fluent `FluentButton` with `Appearance.Accent`, drawing `{colors.brand-accent}` (Fluent **Brand**) — a **chrome accent, never a status carrier.** Padding `{spacing.4}` × `{spacing.2}`.
- **State:** standard Fluent button states (rest / hover / focus / disabled); disabled when its preconditions are not met, paired with an inline reason where relevant. Brand here means *primary action*, and is never reused to signal state on a row.

### destructive-control

The control that triggers a destructive or high-impact action (remove user, disable tenant, remove global administrator). **It is NOT a primary or casual button** — it must never read as the easy, obvious thing to click.

- **Anatomy / color:** a Fluent `FluentButton` kept at **low emphasis** until the flow has been deliberately entered; it draws `{colors.status-danger}` (Fluent **Danger**) **sparingly**, reserving the loud color for the genuine destructive moment. Padding `{spacing.4}` × `{spacing.2}`.
- **Gating / state:** always gated behind a full `{components.consequence-preview}` and an asymmetric confirmation (last-owner → warning + elevated friction, never blocked; last-global-administrator → surfaced as *unavailable* with a reason, not a completable confirmation). The confirmation is a focus-trapped dialog whose **safe escape does not commit** the action. Visual weight is earned by the flow, not granted by default. (Trigger conditions and friction logic: behavioral spec in EXPERIENCE.md.)

## Do's and Don'ts

| Do | Don't |
|---|---|
| Reserve `{colors.status-success}` for projection-**proven** truth (`current` · `confirmed` · `audit available` · `active`) | Show Success — color, copy, or announcement — before the projection confirms it (esp. `accepted`) |
| Render every status as `BadgeColor` + `IconStart` + `IconLabel`/text, legible in forced-colors | Encode any status with color alone, or rely on hue to carry meaning |
| Bind to Fluent semantic **roles** by name (`{colors.status-*}`, Brand) | Invent, hard-code, or restate a hex value anywhere |
| Keep the 3-tier ramp distinct — Warning (friction) → Severe (blocks, not error) → Danger (refusal/failure) | Collapse caution into one "bad" color, or spend Danger on routine caution |
| Use `{colors.brand-accent}` (Brand) for chrome & primary action only | Use Brand for a status, or a status role for chrome |
| Use `BadgeAppearance.Tint` by default; `Filled` only for Danger + Severe (emphasis, not an a11y signal) | Make routine badges Filled, or count `Filled` toward no-color-only (the OS drops it in forced-colors) |
| Bind live-region politeness to a dedicated announcement-intent field | Derive `AriaLive` from `BadgeColor`/`MessageBarIntent` — it over-announces `risk high` and misses Important/Severe alerts |
| Style `destructive-control` sparingly and gate it behind preview + confirmation | Render a destructive action as a primary or casual button |
| Verify every Fluent token/component/ARIA name against the pinned package at build | Assert a Fluent token name that is not in the verified vocabulary |
| Keep `MessageBar` intent `Error` and `Badge` color `Danger` as separate token names | Unify the two token names because they mean the same concept |
| Pin identity/status/freshness/role/risk via `DataGridColumnPin.Start`; reserve cell space | Let sorting, paging, or scroll hide a pending or stale marker — or shift the layout |
| Use `{typography.mono}` + absolute timestamps for ids and times | Show relative-only times, or parse a caller-supplied id as a ULID/Guid |
| Keep distinct states distinct (`accepted` ≠ `confirmed` ≠ `audit available`) | Merge lifecycle/audit states into one badge or one "done" |
