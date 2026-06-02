# Input Reconciliation — Operations Shell Spec vs. PRD + Addendum

Source spec: `docs/tenants-ui-operations-shell-spec.md` (Story 9.2 — Operations Shell + read-only access-review surfaces, planning-only)
Compared against: `prd.md` and `addendum.md` (prd-tenants-2026-06-02)
Date: 2026-06-02

Scope note: this reconciliation covers only what the spec contains that the PRD/addendum **missed, dropped, or contradicted**. Items the PRD already covers adequately are not listed. Net-new ideas beyond the spec are not introduced.

---

## GAP-1: ID scheme directly contradicted — "ULIDs" (spec) vs. "caller-supplied strings, not ULIDs" (PRD)

- **Spec location:** §5.1 — "**IDs are ULIDs, not GUIDs.** Long tenant IDs (ULIDs), user IDs, and references truncate **visually**…"
- **PRD/addendum:** Glossary (§4) — "identified by a meaningful caller-supplied string id (**not a ULID**)"; addendum §D — "tenant ids and user ids are **meaningful caller-supplied strings, not ULIDs** (envelope ids like `MessageId` may be ULIDs)."
- **Severity:** critical
- **Why it matters:** The two documents assert opposite things about the identity type that drives truncation/accessibility (AC5) and search behaviour. Downstream UX/automation cannot resolve "truncate the ULID" vs. "this is a short human-supplied string." The spec's "IDs are ULIDs" statement appears to conflate envelope/message ULIDs with domain tenant/user ids (which the addendum correctly separates).
- **Suggested PRD fix:** Reconcile to the addendum's distinction — tenant/user ids are caller-supplied strings (not ULIDs); only envelope ids (MessageId, correlation) are ULIDs — and flag the spec §5.1 wording as needing correction so AC5 truncation rules apply to long *references*, not to a presumed-ULID tenant id.

---

## GAP-2: Tenant list displayed columns dropped (Member count, Owner count, Pending state)

- **Spec location:** §2.3 Displayed columns — Tenant status, **Member count**, **Owner count**, Freshness, **Pending state**.
- **PRD/addendum:** FR-1 consequences only require "each row shows **tenant identity, status, and a Truth State Badge with freshness**." Member count, owner count, and pending state are silently dropped from the tenant-list row contract.
- **Severity:** high
- **Why it matters:** Member/owner counts are the at-a-glance triage signal (UJ-1) and owner count is load-bearing for the last-owner rule; "pending state" is a distinct per-row column the spec mandates. FR-1 as written would pass acceptance without them.
- **Suggested PRD fix:** Expand FR-1 consequences to require the five spec columns (tenant status, member count, owner count, freshness, pending state).

---

## GAP-3: Tenant list mandates six distinct, non-collapsible surface states; PRD only names empty + error

- **Spec location:** §2.4 — six states each with distinct user-facing copy, "**none may be collapsed into another**": `loading`, `empty`, `filtered-empty`, `error`, `stale`, `degraded`.
- **PRD/addendum:** FR-1 names only "an empty result and an error state." The full six-state set is specified in the PRD **only for the audit surface** (FR-20: loading/empty/filtered-empty/error). The tenant list (and the read surfaces generally) lose `filtered-empty`, `stale`, and `degraded` as required, distinct states.
- **Severity:** high
- **Why it matters:** `filtered-empty` ("offer a clear filter reset"), `stale` ("show freshness marker + refresh path"), and `degraded` ("explain what is unavailable and what still works") carry distinct copy and recovery affordances. Collapsing them is exactly what the spec forbids, and it is the core honesty proposition of the product.
- **Suggested PRD fix:** Add the six distinct, non-collapsible states (loading/empty/filtered-empty/error/stale/degraded) to FR-1 (and reference them as the read-surface standard), not only to the audit FR.

---

## GAP-4: "Sort and pagination must never hide pending or stale-state indicators" invariant absent

- **Spec location:** §2.5 Invariants (AC2) — "**Sort and pagination must never hide pending or stale-state indicators.** Reordering or paging the list must keep the pending and stale markers visible for affected rows."
- **PRD/addendum:** No FR or NFR carries this invariant. PRD §5.2 has a generic "Layout is stable (reserves space to avoid shift)" but nothing about preserving pending/stale markers across sort/page.
- **Severity:** high
- **Why it matters:** This is an explicit AC2 acceptance condition that protects against an operator paging/sorting past a stale or in-flight row and acting on it — a direct trust/safety regression.
- **Suggested PRD fix:** Add a testable consequence to FR-1 (or an NFR) that sorting/paging must never hide pending or stale markers for affected rows.

---

## GAP-5: Read-only member table "must not imply a removal or role change has been applied"

- **Spec location:** §3.3 — "The read-only member table **must not imply that a removal or role change has been applied**"; member rows surface data "**without implying command completion or membership mutation**."
- **PRD/addendum:** FR-8 says the table is "read-only" but does not carry the explicit anti-implication intent; it is the qualitative UX honesty rule that read surfaces must not look like they mutated state.
- **Severity:** medium
- **Why it matters:** In the MVP, action availability is "reflected" (FR-9) next to read rows; without this rule a reflected/disabled action could read as a completed change. This is a specific phrasing the FR list silently drops.
- **Suggested PRD fix:** Add to FR-8 a consequence that the read-only member table must not imply any membership mutation or applied role/removal change.

---

## GAP-6: "Custom command flows, not generated CRUD" boundary missing from PRD body

- **Spec location:** §3.3, §4.1, §4.2, §8 — add/remove/change-role and grant/remove global-admin are "**custom command flows, not generated CRUD**"; "Cross-tenant revoke/remove actions are custom high-risk command flows and must **not** be generated from query rows"; "Generated FrontComposer composition is appropriate **only** for low-risk, read-only, projection-backed surfaces."
- **PRD/addendum:** PRD §11 says Tenants "composes" backend queries/commands and addendum §F covers build-in-FrontComposer-vs-Tenants, but neither states the **generated-vs-custom** boundary: that command/mutation flows must be hand-authored custom flows and must not be auto-generated CRUD from query rows.
- **Severity:** medium
- **Why it matters:** This is a concrete architectural guardrail (a key reason FrontComposer generation is "safe" only for read surfaces). Dropping it risks a downstream story auto-generating membership/global-admin mutations from query rows.
- **Suggested PRD fix:** Add to §11 (Dependencies/Boundary) that generated FrontComposer composition is limited to low-risk read-only projection-backed surfaces, and all command/mutation flows are custom flows — never generated CRUD from query rows.

---

## GAP-7: Flat audit list overstated as "approved fallback" — spec leaves it conditional/unapproved

- **Spec location:** §6 — "the first audit slice is a flat DataGrid-backed list **only if product/UX approves the fallback**"; "**Do not claim an `<AuditTimeline>` component exists.**" Audit surfaces are `blocked`.
- **PRD/addendum:** FR-20 ("**flat list is an approved fallback**"), Glossary ("Approved fallback … e.g. a flat audit list"), and addendum §B/§F ("**Approved fallback: flat audit DataGrid**", "flat list **chosen** as the approved fallback") present it as already approved — while PRD §16.2 still lists securing that approval as an open question. Internally inconsistent and ahead of the spec.
- **Severity:** medium
- **Why it matters:** The spec is deliberately cautious (approval pending; no AuditTimeline claim). The PRD asserts the decision as made in three places, which could greenlight the fallback before Product/UX sign-off (contradicting its own Open Question #2 and R-1).
- **Suggested PRD fix:** Downgrade "approved fallback" wording to "proposed/pending-approval fallback" for audit until §16.2 closes, keeping it consistent with the spec's "only if product/UX approves."

---

## GAP-8: Global-administrator domain specifics dropped (singleton aggregate, fixed id, no tenant-domain routing)

- **Spec location:** §4.2 (AC4) — global admins live in the separate `global-administrators` domain, "**singleton aggregate, ID `"global-administrators"`**", "**must not be modeled as tenant membership** and must not route global-administrator data as normal tenant-domain data."
- **PRD/addendum:** Glossary describes a "separate `global-administrators` scope; distinct from tenant membership," and FR-18/19 keep it "never conflated with tenant membership," but the **singleton-aggregate / fixed-id** shape and the explicit **routing prohibition** (do not route as tenant-domain data) are not captured.
- **Severity:** medium
- **Why it matters:** The routing prohibition and singleton shape are concrete constraints for the read surface (`ui-06`) and prevent treating the global-admin list as just another tenant projection. AC4 hinges on this platform-vs-membership distinction.
- **Suggested PRD fix:** Note in FR-18 (or §11/addendum §D) that global administrators are a singleton aggregate (id `"global-administrators"`) and their data must not be routed/modeled as tenant-domain data.

---

## GAP-9: "Row actions stay stable in width and placement" invariant not carried

- **Spec location:** §2.5 — "**Row actions stay stable in width and placement** as data, sort order, and page change."
- **PRD/addendum:** PRD §5.2 has the generic "Layout is stable (reserves space to avoid shift)" but not the specific row-action width/placement stability under data/sort/page change.
- **Severity:** low
- **Why it matters:** Stable action placement under change is both a usability and an automation-selector-stability concern (ties to NFR-4 / FC-A11Y). It is a discrete invariant the spec calls out.
- **Suggested PRD fix:** Add row-action width/placement stability (under data, sort, and page change) as a consequence of FR-1/FR-8 or to §5.2.

---

## GAP-10: "User lookup is secondary but reachable" framing flattened to co-equal primary nav

- **Spec location:** §1.2 (AC4) — "**User lookup is secondary but reachable** from shell navigation (the Users area) and from access-review contexts"; §4 — "Access questions can begin with a user or a platform role, **not only a tenant**."
- **PRD/addendum:** PRD §5.1 lists Users as one of four co-equal primary nav areas and omits the "secondary but reachable" emphasis and the "access can begin with a user/role, not only a tenant" intent.
- **Severity:** low
- **Why it matters:** The spec's nuance is about *priority and reachability* (tenant list is the default triage entry; Users is a secondary-but-always-reachable path), not equal billing. Losing it could over-promote the Users area in IA.
- **Suggested PRD fix:** In §5.1 note that Users is a secondary-but-reachable area (tenant list remains the default triage entry) and that access questions may begin from a user or platform role, not only a tenant.

---

## GAP-11: Freshness evidence is a specific triple (timestamp / projection version / ETag); PRD reduces to ETag/304 mechanics

- **Spec location:** §2.1, §0/§Truth-State, §7 — "surface a freshness marker (**timestamp / projection version / ETag**)"; "Freshness markers … must use timestamp / projection version / ETag evidence available from the read model; if freshness cannot be measured, the state is `unknown`."
- **PRD/addendum:** Glossary defines Freshness states and addendum §D names the ETag/304 primitive, but neither states the three explicit evidence sources the marker must derive from (timestamp, projection version, ETag) nor the "if unmeasurable → `unknown`" rule at the marker level (PRD has the spirit in UJ-1's edge case but not as a read-surface requirement).
- **Severity:** low
- **Why it matters:** Specifies *what evidence the freshness marker is allowed to use*, which constrains implementation honesty (don't fabricate freshness). Currently only implicit.
- **Suggested PRD fix:** Add to NFR-1 / FR-5 that the freshness marker derives from timestamp, projection version, and/or ETag evidence, and is `unknown` when none can be measured.

---

## GAP-12: Configuration "namespace-grouped" display presentation dropped

- **Spec location:** §3.1 item 3 — "Configuration (read-only) — **namespace-grouped** key/value display."
- **PRD/addendum:** FR-6 covers prefix filtering ("values outside the caller's prefix are not shown") but not the **grouped-by-namespace presentation** the spec specifies for the read surface.
- **Severity:** low
- **Why it matters:** Minor UX presentation detail, but it is an explicit display contract for `ui-05` that the FR omits.
- **Suggested PRD fix:** Add to FR-6 that authorized configuration key/values are displayed grouped by namespace.

---

## Items checked and found adequately covered (not gaps)

- Cursor-only pagination (never offset/limit) — PRD FR-1, NFR-1, addendum §D. Covered.
- ETag/304 freshness primitive + SignalR "nudges, not proof" — PRD CP-4, NFR-3, addendum §D. Covered.
- Support-safe reference exclusions (no payloads/tokens/correlation ids/PII) — PRD §10, FR-22, Glossary. Covered (PRD is equal-or-more detailed).
- Configuration consumer-owned dot-prefix filtering — PRD Glossary + FR-6. Covered (more detailed than spec).
- Authorization lives in projection/query, UI reflects only — PRD NFR-2, CP-9. Covered.
- Context preservation (selected tenant + filters on return; deep-link) — PRD FR-2. Covered (adds deep-link, consistent with spec §3.2).
- No-color-only encoding; stable automation selectors (not row text) — PRD §5.2, NFR-4, §9. Covered.
- Authorization-safe empty/error (no leak of out-of-scope memberships) — PRD FR-1, FR-4, §10. Covered.
- Command lifecycle never a primary nav area; shown inline — PRD §5.1. Covered.
- Planning-only / "does NOT build UI" boundary — outside PRD altitude (PRD is the product frame above the planning specs); not a gap.
