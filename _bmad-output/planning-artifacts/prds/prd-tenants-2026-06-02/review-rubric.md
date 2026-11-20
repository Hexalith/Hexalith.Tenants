# PRD Quality Review — Tenants Management UI

## Overall verdict

The revised PRD resolves the 13 earlier findings and gives Product and Release a clear account of the evidence still owed. Its requirements can guide planning, but several acceptance tests still require consulting the source stories: the first-tenant exception, approval to copy a literal, membership-removal proof, and the meaning of an audit entry opened from a user or command. The engineering handoff also needs a precise account of which server components hold proof metadata and which module supplies command admission.

## Decision-readiness — strong

Sections 14 and 16 distinguish the September audit performance approval from the November deployment decision. The dedicated release-candidate workload has an owner, budgets and fallback. Addendum §J identifies separate evidence for hosted package qualification, first-consumer recovery, deployed identity and index capacity. A reader can tell who must act and what would close each check.

## Substance over theater — strong

The distinction between zero owners and zero membership history changes who can recover an active tenant. The exact-key configuration display policy has different consequences for an administrator and a tenant-scoped caller. These concrete examples give FR-24 and FR-6 testable meaning. The recorded November run counts establish local coverage without making a deployment claim.

## Strategic coherence — strong

SM-6 measures the product’s central promise by matching the first displayed or announced success to authoritative evidence for that command. The proposed per-candidate cohort, denominator, unassessed count and no-result rule make zero incidents interpretable. Product and Release confirmation of the proposal is an explicit decision. SM-C1 and SM-C3 also protect preview and freshness safeguards against pressure to complete tasks faster.

## Done-ness clarity — adequate

All 25 FRs give a reader an observable consequence. FR-25 now distinguishes a confirmed correction, missing tenant receipt pairing and independently verified governance proof. Several other requirements omit meaningful positive or negative acceptance cases.

### Findings

- **[medium]** First-tenant creation omits its named freshness exception (§6 CP-2, §7.5 FR-13) — CP-2 permits an approved exception but FR-13 omits the case a global administrator encounters on a fresh installation. Story 3.1 permits creation with unknown freshness only when the tenant list is authoritatively empty in the caller’s scope and no first-write timestamp exists. An empty later page, an ambiguous projection or unproven authority must still block. See `spec-3-1-create-tenant-with-projection-confirmation.md:28`, `TenantsWorkspace.razor:565–581` and `TenantsWorkspaceTests.cs:1554–1606,1763–1776`. *Fix:* Name this exception in FR-13 and cross-reference CP-2. Include authorized first creation and the blocked look-alikes as acceptance cases.
- **[medium]** Copy eligibility needs a positive approval test (§7.2 FR-7, §10, addendum §E) — “Copied content is the full id” and §10’s forbidden-field list do not establish who may approve that literal for copying. Story 1.8 requires an authorized read surface to approve the exact value and its kind. An unapproved value gets no copy control and never enters copy-component state. See `spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md:29,39–40` and `SupportSafeCopyClassifier.cs:15–32`. *Fix:* Pair the approved literal’s unchanged clipboard result with a refusal case for an unapproved literal, including absence from the control’s state.
- **[medium]** Member-removal audit proof lacks a discriminating acceptance case (§7.4 FR-12, addendum §I WP-2A) — The progression to `audit_available` omits what distinguishes this removal from an older removal of the same member. Current behavior requires the authorized proof lookup to establish the exact attempt and committed sequence across stable, complete evidence. An inconclusive walk preserves the confirmed removal and withholds the receipt. See `spec-2-4b-wp-2a-removal-proof-and-audit-available.md:45–51`, `release-readiness.md:542` and `RemoveTenantMemberFlow.razor:1722–1805`. *Fix:* Add exact-match and incomplete, changing or unmatched-read cases. Include the pre-dispatch block when proof capability is absent.
- **[medium]** Audit entry context can be mistaken for an authoritative filter (§7.8 FR-21) — “Each entry point lands scoped to the relevant tenant/user/command” suggests that a user or command entry restricts the returned rows. Story 5.2 makes tenant scope authoritative, with date and category filters. User and command origins provide context hints. The UI expressly disclaims exhaustive user matches and command proof. See `spec-5-2-reach-scoped-audit-evidence-from-context.md:24` and `TenantsResources.resx:3465–3472`. *Fix:* Describe the eligible origin, the authoritative filters, and the meaning of the hint in separate acceptance cases.

## Scope honesty — strong

Sections 2 and 14 distinguish the historical read-only milestone from the delivered command flows. FR-6, FR-15 and FR-25 define limits on data disclosure, hard deletion and tenant audit pairing. Open language and accessibility decisions have been kept conditional. The four external checks in §J have concrete owners and acceptance evidence.

## Downstream usability — thin

The glossary, stable identifiers and story mappings support extraction. The current text still gives a reader the wrong expectation about a server-component boundary and the provider of an aggregate-scoped lock. The removal preview’s own reference section also omits required facts.

### Findings

- **[medium]** The component data boundary and server-side proof model disagree (§7.8 FR-22, §10, addendum §D) — The PRD says rendered components never receive internal correlations. The server-side audit page passes rows with internal `CommandCorrelationId` and aggregate sequence to the grid. The mapped receipt exposes support-safe fields. This is a mismatch between the stated architecture and its implementation. The reviewed evidence does not establish browser disclosure. See `TenantAuditRow.cs:39–43,92–93`, `TenantAuditPage.razor:222`, `AuditDataGrid.razor:178` and `TenantAuditReceipt.cs:23–33`. *Fix:* Decide whether the categorical component boundary remains required. If retained, record the implementation gap. If server-only verification state is accepted, specify its permitted owner and test the prohibitions on rendering, copying, serialization to clients and logging.
- **[medium]** The dependency table attributes Tenants command behavior to FrontComposer (§11, addendum §B) — The shared command contracts are marked “confirmed” beside an aggregate-scoped lock. The July 19 review recorded limitations in the shared contracts. Story 2.1 explicitly selected a Tenants-owned admission gate keyed by aggregate identity while the FrontComposer gate remains circuit-wide. Inferring package capability from product behavior would serialize unrelated commands in another integration. See `story-1-0-frontcomposer-fluent-reverification-2026-07-19.md:51–52`, `spec-2-1-reverify-projection-confirmed-membership-command-foundation.md:40` and `TenantAggregateCommandAdmissionGate.cs:6–17`. *Fix:* Give the shared component and the Tenants implementation separate owner and evidence entries for command feedback and admission.
- **[medium]** The removal preview summary omits three displayed safety facts (§7.4 FR-12, addendum §H) — FR-12 requires the tenant, target and current role as inputs. Section H describes a “canonical 10-item set” but its extracted list omits these three as fields the operator must see. A preflight check alone would pass while a user confirms removal of the wrong member. The shipped dialog and its regression explicitly display them. See `docs/tenants-ui-remove-user-from-tenant-journey-spec.md:69–80`, `RemoveTenantMemberFlow.razor:722–734` and `RemoveTenantMemberFlowTests.cs:326–352`. *Fix:* Enumerate the displayed facts or link directly to the complete approved list and explain how the dialog groups the facts and shows platform standing.

## Shape fit — adequate

The journeys suit a shared operator and owner interface. Users can follow the recovery examples while engineers can use the addendum for command and projection behavior. One navigation attribution could misdirect a downstream UX handoff.

### Findings

- **[low]** The Users tab description conflates self-audit and lookup (§5.1) — The PRD assigns FR-3 “My Tenants” and FR-4 lookup to the Users tab after describing the self-membership filter under Tenants. The canonical workspace places `scope=mine` under Tenants and lookup under Users. See `1-1-reverify-ui-host-bootstrap-and-canonical-workspace.md:64` and `TenantsWorkspace.razor:304–326`. *Fix:* Map FR-3 to the Tenants self-membership scope and FR-4 to the Users lookup tab.

## Mechanical notes

- FR-1–25, UJ-1–6, SM-1–6, and SM-C1–3 each have a unique definition. Each journey names its protagonist. SM-6 is grouped with the primary measures.
- All 52 explicit relative Markdown links in the current PRD and addendum resolve. The 28 line and heading fragments resolve against their target files. Section 17 indexes the substantive tagged assumptions, including the proposed SM-6 measurement.
- The prior findings reconciliation and the decision log distinguish the 13 completed document corrections from outstanding product and deployment decisions. This assessment used the edited working tree at Tenants revision `8318f917` and the cited source and test contracts. It did not execute the product qualification suites.
