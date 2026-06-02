# Input Reconciliation — PRD vs. FrontComposer Dependency Map

**Source spec:** `docs/tenants-ui-frontcomposer-dependency-map.md` (Story 9.1 / 12.1 / 12.2 / 12.3 readiness artifact, FrontComposer checkout `17c3605`, Fluent UI pin `5.0.0-rc.3-26138.1`)
**Reconciled against:** `prd.md` §11–§12, §14 and `addendum.md` §B (FrontComposer dependency readiness table), §F.

Scope of this pass: dependency accuracy only. Gaps where the PRD/addendum **understate a missing/blocking dependency** or **overstate readiness** are flagged. Items the PRD already covers adequately are not repeated. Findings are limited to what the spec states; nothing is invented beyond it.

---

## Readiness reconciliation matrix

Spec canonical readiness is the Story 12.1 Dependency ID Catalog (spec line 31: "the only place where dependency IDs are defined"), with Story 12.2/12.3 refinements noted.

| ID | Spec 12.1 catalog | Spec 12.2/12.3 refinement | Addendum §B | Verdict |
|---|---|---|---|---|
| FC-TBL | available | — | available | match |
| FC-LYT | needs-confirmation | needs-confirmation | needs-confirmation | match (but PRD §11 body contradicts — see Gap 2) |
| FC-CMD | needs-confirmation | needs-confirmation | needs-confirmation | match (but PRD §11 body contradicts — see Gap 2) |
| **FC-CNC** | **missing** | **missing** (12.2 L207, 12.3 L312/322) | **needs-confirmation** | **MISMATCH — understated (Gap 1)** |
| FC-TOK | missing | missing | missing | match |
| FC-AUD | missing | needs-confirmation (12.2 L188) | missing | minor mismatch, conservative (Gap 6) |
| FC-CNS | missing | needs-confirmation (12.2 L190) | missing | minor mismatch, conservative (Gap 6) |
| FC-A11Y | needs-confirmation | needs-confirmation | needs-confirmation | match |
| FC-L10N | needs-confirmation | needs-confirmation | needs-confirmation | match |
| FC-DOC | needs-confirmation | needs-confirmation | needs-confirmation | match |

---

## Gap 1 — FC-CNC readiness is understated (`needs-confirmation` instead of `missing`)

**Spec location:** Story 12.1 catalog line 39; Story 12.2 readiness row line 192; Story 12.2 Dependency Decisions line 207; Story 12.3 readiness row line 312; Story 12.3 reviewable table line 322 — **every occurrence reads `missing`** ("no verified toast batching component or policy path found").
**Addendum location:** §B line 26 — reads `needs-confirmation`, note "Affects command FRs."
**Severity: HIGH.** This is the one true understatement of a missing/blocking dependency. `needs-confirmation` implies "some reusable contract exists, just not validated," whereas the spec's `missing` means **no verified evidence at all** for toast batching / burst consolidation. The addendum note "Affects command FRs" is also weaker than the spec policy: spec says any workflow that can dispatch overlapping/rapid commands is **"blocked or planning-only unless product/UX approves a one-at-a-time fallback"** (lines 192, 207, 312). This directly bears on the remove-user incident-response flow (UJ-4, FR-12) and user-search bulk revocation (spec Screen Matrix line 55).
**Suggested PRD fix:** In addendum §B set FC-CNC readiness to `missing`; change the note to "No verified toast-batching/burst-consolidation evidence; multi-row/rapid command flows are blocked or planning-only until Product/UX approve a one-at-a-time fallback." Add FC-CNC to PRD §12 R-1 alongside FC-AUD/FC-CNS/FC-TOK.

## Gap 2 — PRD §11 lists shell/layout and command-lifecycle feedback as "provides — treat as given," but spec marks both `needs-confirmation`

**Spec location:** FC-LYT `needs-confirmation` (lines 37, 197, 212); FC-CMD `needs-confirmation` (lines 38, 191, 206, 311). Spec is explicit that the current shell layout exists but the **full-width/constrained contract is not validated**, and that command-lifecycle source paths exist but the **Tenants-compatible contract has not been exercised end-to-end**.
**PRD location:** §11 line 334 — "FrontComposer (provides — treat as given): the application shell/layout, … command lifecycle feedback (three-phase, projection-confirmed) …".
**Severity: HIGH.** "Treat as given" overstates readiness for two dependencies the spec rates `needs-confirmation`, and the PRD body **contradicts its own addendum** (§B lines 24–25 correctly mark both `needs-confirmation`). A reader of §11 alone would assume layout and command feedback are settled.
**Suggested PRD fix:** Split §11's "provides" bullet — keep FC-TBL (DataGrid/projection), status/role badges, destructive-confirmation dialog, localization resources, and the command client as "available," but move "application shell/layout (full-width/constrained variant)" and "command lifecycle feedback contract" into a "provides, pending confirmation (FC-LYT, FC-CMD)" sub-bullet referencing §12.

## Gap 3 — Flat-audit-list and inline-consequence fallbacks are stated as "approved" but the spec records them as not-yet-approved

**Spec location:** FC-AUD line 40 ("product/UX **approves** a specific fallback such as a DataGrid-backed flat audit list" — conditional/future); line 128 ("only when product/UX approves accessibility, localization, loading, replacement path…"); line 188; line 203. FC-CNS line 41 ("**Implementation convenience is not approval**"); line 232 ("`FcDestructiveConfirmationDialog` is adjacent evidence … **not proof** that consequence preview behavior exists").
**PRD/addendum location:** Addendum §B line 28 "**Approved fallback: flat audit DataGrid**"; §F line 60 "flat list chosen as **the approved fallback**"; PRD FR-20 line 282 "**flat list is an approved fallback**".
**Severity: MEDIUM-HIGH.** Overstated readiness. The spec treats the flat-list/inline-text fallbacks as *candidate fallbacks awaiting Product/UX sign-off*, not approved. The PRD is also internally inconsistent: R-4 (line 344) and Open Question 2 (line 397) correctly say approval is still pending, while §B/§F/FR-20 assert it is already approved.
**Suggested PRD fix:** Change "approved fallback" → "proposed fallback pending Product/UX approval (Open Question 2)" in addendum §B FC-AUD, §F, and PRD FR-20. Note the FC-CNS row already hedges ("needs Product/UX approval") — align FC-AUD to the same wording.

## Gap 4 — FC-CNS (and FC-CNC) blocking of the remove-user flow (FR-12 / UJ-3) is not surfaced

**Spec location:** High-Risk Workflow map line 136 (Remove user requires `FC-CNS, FC-CMD, FC-CNC, FC-TOK, FC-A11Y, FC-L10N, FC-DOC`); copy block lines 261–266 (`blockedBy: [FC-CNS, FC-CMD, FC-CNC, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]`); line 205 ("FC-CNC applies uniformly to all four destructive classes").
**PRD location:** FR-12 (lines 236–238) lists CP-3/CP-5/CP-6/CP-8 consequences but names **no FrontComposer blocker**; §12 R-1 (line 341) names only FC-AUD and FC-CNS as "do not exist" and FC-LYT/FC-TOK, never FC-CNC. FR-12 is the PRD's flagship worked journey (UJ-3, the remove-user spec).
**Severity: MEDIUM.** The most safety-critical command flow understates its dependency stack; FC-CNC in particular (a `missing` dependency per Gap 1) gates it but is invisible in the FR.
**Suggested PRD fix:** Add to FR-12 consequences (or a per-FR dependency note): "Blocked on FC-CNS + FC-CMD + FC-CNC until a Product/UX-approved consequence-preview and one-at-a-time-command fallback is recorded (addendum)." Ensure §14.2 Phase 2c notes FC-CNC as a blocker for command flows generally.

## Gap 5 — The spec's binary blocked-vs-planning-only criterion is flattened into one "Phase 2c gated" bucket

**Spec location:** "Readiness and Status Conventions" line 178 — destructive status is **binary**: platform-wide destructive (Disable Tenant, Remove Global Admin) = **`blocked`**; tenant-scoped destructive (Remove User, high-impact config) = `planning-only`. Reinforced in copy blocks: Remove User `planning-only` (line 262), Global Admin Remove `blocked` (line 270), Disable Tenant `blocked` (line 278), High-Impact Config `planning-only` (line 286).
**PRD location:** §14.2 (line 370) groups FR-12, FR-15, FR-16–17, FR-19, FR-20–25 into a single "Phase 2c (gated on FrontComposer / fallback approvals)" bucket with no severity distinction. §12 does not record that disable-tenant (FR-15) and remove-global-admin (FR-19) are categorically harder-gated (`blocked`) than remove-user (FR-12) / config (FR-16–17) (`planning-only`).
**Severity: MEDIUM.** A real readiness distinction (platform-wide actions are `blocked`, not merely `planning-only`) is dropped, which understates the gate on FR-15 and FR-19.
**Suggested PRD fix:** In §14.2 or §12, split Phase 2c into "blocked until reusable FC component or approved fallback — FR-15 disable/enable, FR-19 global-admin" vs. "planning-only, fallback-eligible — FR-12 remove-user, FR-16–17 config," citing the spec's platform-wide vs tenant-scoped criterion.

## Gap 6 — FC-AUD / FC-CNS readiness in addendum (`missing`) lags the spec's own refinement (`needs-confirmation`)

**Spec location:** Story 12.2 readiness rows — FC-AUD `needs-confirmation` (line 188), FC-CNS `needs-confirmation` (line 190). The Story 12.2 addendum (line 176) explicitly explains that `<AuditTimeline>`/`<ConsequencePreview>` sit at the `evidence: missing` row sentinel but the *dependency* is rated `needs-confirmation` because an approved fallback path exists.
**Addendum location:** §B FC-AUD line 28 and FC-CNS line 29 both read `missing`.
**Severity: LOW.** This is the **conservative** direction (addendum is stricter than spec), so it does not understate risk — but it is a literal mismatch with the spec's latest refinement and with the PRD's own intent to ship via approved fallbacks. Note the PRD body (R-1 line 341) says these components "do not exist," which matches the `evidence: missing` component sentinel, so PRD prose is defensible; only the addendum table's readiness *value* diverges.
**Suggested PRD fix:** Optionally align addendum §B FC-AUD/FC-CNS readiness to `needs-confirmation (component evidence: missing)` to match Story 12.2, or add a footnote that "missing" refers to component evidence while the dependency is fallback-eligible. Low priority.

---

## Items checked and found adequately covered (no fix needed)

- **FC-TOK as a blocker** — PRD R-1 (line 341) already names FC-TOK missing; spec lines 42/193/208 agree it blocks polished audit/consequence visuals. Adequate.
- **SignalR = nudge only / projection is source of truth** — PRD CP-4 (line 169) + addendum §D match spec lines 335, 392, 407 (`FcProjectionConnectionStatus` is source-backed evidence). Adequate.
- **Command endpoint `/api/v1/commands` vs `/api/commands` alias** — PRD Open Question 1 + addendum §C match spec line 114. Adequate.
- **No-invitation / direct-add, NoOp, cursor opacity, RFC 7807, ULID note** — addendum §D matches spec; not dependency-readiness gaps.
- **ui-NN vs backend-epic namespace collision** — PRD R-5 + addendum §E match spec lines 54–56 / Story 9.1. Adequate.
- **FC-AUD grouped mode is fast-follow, not first-slice** — PRD non-goals (line 354) + addendum §F match spec lines 180, 189, 204. Adequate.
