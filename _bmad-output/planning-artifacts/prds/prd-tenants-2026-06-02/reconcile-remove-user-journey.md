# Reconcile: Remove-User Journey vs PRD + Addendum

Input-reconciliation for PRD finalize.

- **SOURCE SPEC:** `docs/tenants-ui-remove-user-from-tenant-journey-spec.md` (Story 9.4 — the worked RemoveUserFromTenant command journey)
- **PRD DRAFT:** `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md` (UJ-3 §3.3, FR-12 §7.4, cross-cutting contract §6)
- **PRD ADDENDUM:** `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md`

Scope: only gaps where the SOURCE SPEC's UJ-3/FR-12 edge cases and guards are **dropped, weakened, or misrepresented** by the PRD + addendum. Items the PRD already covers adequately (last-owner-never-block CP-6/FR-12/UJ-3; audit-as-proof CP-3; never-undo CP-7; fail-closed CP-2/CP-5; non-collapse CP-3) are not relisted.

---

## Gap 1 — Consequence-Preview 10-item content set collapsed to 2 items (`known consequences` + `known unknowns`)

- **Spec location:** §2.1 (enumerated exactly 10 items); restated as an implementation-story rule in §7 item 5 ("the ten-item preview content set (§2.1)"); AC2 / traceability §9.
- **What the PRD does:** The Glossary (§4 "Consequence Preview") and CP-5 define the preview as only "known consequences and known unknowns" (spec items 9-10). FR-12's consequence bullet references only "incomplete preview inputs block submission (CP-5)". UJ-3 says only "a **Consequence Preview** of what removal will and won't do". The other eight required content items are absent: **tenant (1), target user (2), current role (3), owner count (4), affected access path (5), freshness (6), recovery path (7), audit expectation (8)**.
- **Severity:** High — a future implementer reading FR-12 alone would build a two-line "consequences/unknowns" panel and silently drop owner-count, affected-access-path, recovery-path, and audit-expectation content. This is the core deliverable of the worked journey.
- **Suggested PRD fix:** In FR-12 (or a referenced note) state that the Consequence Preview must present the full ten-item content set defined in the remove-user journey spec §2.1 (tenant, target user, current role, owner count, affected access path, freshness, recovery path, audit expectation, known consequences, known unknowns), composed from read models only.

---

## Gap 2 — "Target user also holds global-administrator authority" friction case is dropped from the remove-user journey

- **Spec location:** §3.2 (second elevated-friction case), §3.3 ("Global-administrator authority is a platform-level flag, not a domain switch"), guardrail 2 in "Two critical guardrails", AC3.
- **What the PRD does:** CP-6 names only the **last-owner** and **last-global-administrator** cases. FR-19 separately covers granting/removing a global administrator. Nowhere in FR-12 / UJ-3 / CP-6 is the distinct case captured where *removing a user's tenant membership* must raise **platform-level friction because that same target also holds global-admin authority* — explicitly **without** dispatching `RemoveGlobalAdministrator` or editing the `global-administrators` aggregate. The PRD's "last-global-administrator" (CP-6) is a different scenario (governing the global-admin scope itself), so this is not coverage by substitution.
- **Severity:** High — this is one of AC3's three named friction triggers for the remove-user flow and carries an explicit "do not conflate with editing the global-administrators aggregate" guard. Dropping it loses both the friction requirement and the anti-conflation guard.
- **Suggested PRD fix:** Add to FR-12 (and/or CP-6) that when the removal target also holds global-administrator authority, the flow raises platform-level consequence friction as a reflected flag only — it does not change which command is dispatched (still `RemoveUserFromTenant` in the `tenants` scope, never `RemoveGlobalAdministrator`).

---

## Gap 3 — "Already applied" / NoOp-on-remove reconciliation outcome not surfaced for FR-12

- **Spec location:** §5.1 ("Already applied" row), §5.4 ("Target already removed before submit" → already applied; "Duplicate submit / browser refresh during pending" → deduplicated, do not double-apply).
- **What the PRD does:** The PRD models NoOp/"already applied" for *add* and *role change* (FR-10, FR-11, Glossary "NoOp"). For removal, FR-12 lists only submitted → accepted → projection-confirmed → audit-available plus "every failure mode maps to a stated recovery (CP-8)". The distinct **already-removed → "already applied" / no double-apply** outcome (target removed before submit, or duplicate submit/refresh during pending) is not stated for the remove path. UJ-3 surfaces only the lost-permission edge case.
- **Severity:** Medium — without it an implementer may treat a second/duplicate removal as an error or a fresh destructive action rather than a safe idempotent "already applied", risking a misleading failure or double-apply UX.
- **Suggested PRD fix:** Add to FR-12 that an already-removed target (including duplicate submit / refresh during pending) reconciles as "already applied" (deduplicated, no double-apply, offer inspect-audit / continue read-only), per the journey spec §5.1/§5.4.

---

## Gap 4 — "Unable to verify / unknown" reconciliation outcome not enumerated for FR-12

- **Spec location:** §5.1 ("Unable to verify" → unknown, "avoid success language"), §5.4 ("Status lookup failed → confirmation unknown"; "SignalR disconnected / nudge only" → unable to verify).
- **What the PRD does:** FR-12 enumerates only the happy-path lifecycle plus a generic "every failure mode maps to a stated recovery (CP-8)". The spec treats **unable-to-verify / unknown** as a *first-class reconciliation outcome of this command* with an explicit "avoid success language" rule. CP-8 covers "unverifiable → escalate" generically and CP-4 covers SignalR-as-nudge globally, but FR-12 does not name unknown/unable-to-verify as a possible terminal state of the remove flow.
- **Severity:** Medium — the false-success risk (R-2) is highest precisely at the unable-to-verify state; leaving it implicit risks an implementer defaulting to optimistic success when status lookup fails.
- **Suggested PRD fix:** In FR-12 list the five reconciliation outcomes from journey spec §5.1 (rejected, accepted, already-applied, projection-pending, unable-to-verify) and state that unable-to-verify/unknown must avoid success language and offer retry-status-lookup / inspect-audit / escalate.

---

## Gap 5 — Required-field validation *before the preview opens* understated

- **Spec location:** §4.1 ("Required fields are validated **before** the Consequence Preview opens or the command submits: target user, tenant, and role context resolved, and the freshness and authorization gates passed (`eligible`)"); §7 item 5.
- **What the PRD does:** UJ-3 says "the system validates inputs and gates (freshness + authorization must be `eligible`...) → she opens a Consequence Preview". This implies gating but does not state that **target/tenant/role context resolution and the eligible gate must pass before the preview opens** (the spec gates preview-open, not just submit). FR-12 does not mention pre-preview validation at all.
- **Severity:** Low-Medium — risk that an implementer opens a preview on unresolved/ineligible inputs and only blocks at submit, weakening the fail-closed posture for a destructive flow.
- **Suggested PRD fix:** Note in FR-12 (or tighten UJ-3) that target user, tenant, and role context must be resolved and the freshness + authorization gates must be `eligible` *before the Consequence Preview opens*, not only before submit.

---

## Gap 6 — "Destructive action is not a casual/primary button; reason is inline, not tooltip-only" not tied to FR-12

- **Spec location:** §3.2 ("Destructive actions must **not appear as casual primary actions**: the remove control is not a default/primary button, the high-risk path requires intentional confirmation, and the inline Unavailable Action Reason (not a tooltip alone) explains any block").
- **What the PRD does:** §5.2 has a general "destructive/warning styling is used sparingly" and CP-2 routes block reasons to an Unavailable Action Reason; FR-9 says reasons are "inline-visible (not hover-only)" for the read surface. But FR-12 itself carries no button-hierarchy / not-a-primary-action constraint, and the "inline reason, not tooltip-only" rule is asserted for FR-9 (review) rather than for the destructive remove control.
- **Severity:** Low — partially covered by §5.2 and CP-2/FR-9, but the explicit "remove control is not a default/primary button" guard for this destructive command is not stated where an implementer of FR-12 would look.
- **Suggested PRD fix:** Add to FR-12 that the remove control must not be a default/primary button and that any block reason is shown inline (tooltip is supplemental only), per journey spec §3.2.

---

## Gap 7 — "Do not over-claim consequences" (session revocation / token invalidation / downstream enforcement are unknowns) not preserved

- **Spec location:** §2.1 item 10 (known unknowns: "e.g. session revocation, downstream enforcement, token invalidation"); §2.3 ("The UI must not present session revocation, downstream enforcement, or token invalidation as known consequences unless backend evidence exists ... Overstating downstream effects is itself a false-success risk").
- **What the PRD does:** The Glossary mentions "known unknowns" abstractly and CP-5 references them, but neither FR-12 nor §6 names the concrete over-claim hazards (session revocation, token invalidation, downstream enforcement) that the spec specifically forbids presenting as consequences. This is a named anti-pattern, not a generic principle.
- **Severity:** Low-Medium — without the concrete examples an implementer may list "user is signed out / tokens revoked" as a consequence of removal, which the spec classifies as an unproven over-claim and a false-success risk.
- **Suggested PRD fix:** Note in FR-12 (or CP-5) that session revocation, token invalidation, and downstream enforcement are **known unknowns**, not known consequences, unless backend evidence exists — overstating them is a false-success risk.

---

## Gap 8 — `RemoveUserFromTenant` ≠ `RemoveGlobalAdministrator` command-distinction guard not carried into the PRD/addendum

- **Spec location:** "Two critical guardrails" #2; §3.3; §1 boundary. Removal of tenant membership is `RemoveUserFromTenant` (domain `tenants`, AggregateId = managed tenant id); platform global-admin removal is a separate `RemoveGlobalAdministrator` on the `global-administrators` singleton (backlog `ui-15`).
- **What the PRD does:** The addendum (§C) lists both commands but does not record the guard that the remove-user journey must **not** dispatch or be conflated with the global-admin command. The PRD body keeps the two *scopes* distinct (FR-19, Glossary) but does not state the command-level anti-conflation guard specifically for the remove-user flow.
- **Severity:** Low — the scope separation is preserved generally, but the explicit "this journey removes only tenant membership; global-admin removal is a different command" guard (which protects against an implementer wiring the global-admin flag to the wrong command) is absent. Closely related to Gap 2.
- **Suggested PRD fix:** In the addendum's remove-user mapping (or FR-12), record that `RemoveUserFromTenant` and `RemoveGlobalAdministrator` are distinct commands on distinct scopes and the remove-user flow never dispatches the latter even when the target is also a global admin.

---

## Adequately covered (not flagged)

- **Fail-closed on incomplete inputs** — CP-2, CP-5, FR-12, UJ-3 (named-fallback escape is in CP-2/CP-5).
- **Last-owner elevated friction, never a backend block** — CP-6, FR-12, UJ-3, Glossary, R-1 alignment with domain (no ≥1-owner invariant).
- **Non-collapse / audit-as-proof ("proven")** — CP-3, FR-12, UJ-3, FR-22/FR-23.
- **Live signals are nudges, never proof (SignalR)** — CP-4, addendum §D (covered globally; the per-case "SignalR disconnected → unable to verify" mapping is folded into Gap 4).
- **Never-undo / compensating-command recovery** — CP-7, Glossary, FR-24/FR-25, NFR-5.
- **No internal leakage in rejection text / support-safe references** — §10, CP-8, FR-22, addendum §D.
- **"Tenant status changed while preview open → stale; operator lost permission mid-flow → request permission/escalate"** — surfaced via CP-2/CP-8 and UJ-3 (lost-permission); treated as adequately covered, distinct from the dedup/unknown gaps above.
