# Validation Report — Tenants Management UI

- **PRD**  `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md`
- **Rubric**  `.agents/skills/bmad-prd/assets/prd-validation-checklist.md`
- **Run at**  2026-11-20T15:24:10+01:00
- **Grade**  Fair

## Overall verdict

The PRD gives operators and tenant owners a testable promise about when they can trust an access change. Several older passages would send a team to seek an approval already granted, rely on a recovery permission the domain denies, or present audit proof the service cannot establish. Resolve those passages before using the PRD for a new implementation handoff. Reviewed the November 20 working-tree PRD and addendum against repository revision `8318f917` and its recorded evidence.

Validated the November 20 working-tree PRD and addendum at Tenants commit `8318f917`. The earlier focused review passed for the eight November audit corrections. The sprint tracker still lists four open checks covering hosted package qualification, the first enforcement consumer, deployed identity, and index capacity. This review assessed the documentation against repository evidence. [Focused audit review](review-scp-2026-11-20.md) · [Sprint tracker](../../../implementation-artifacts/sprint-status.yaml#L408)

0 critical, 4 high, 7 medium, 2 low findings.

## Dimension verdicts

- **Decision-readiness — thin.** The four open November checks in §14 and addendum §J have owners and observable outcomes. Other sections still treat completed decisions as pending. A release owner needs the approved contract and the conditions for qualifying a new revision.
- **Substance over theater — strong.** FR-8 distinguishes a genuinely empty roster from rejected creation history, including what each caller may learn. FR-19 defines when uncertain grant delivery can be retried and when to check status. The release qualification and consumer examples in addendum §J give the handoff practical weight.
- **Strategic coherence — strong.** The progression from reading current state to previewing, confirming and correcting a change supports the product’s trust objective. SM-6 measures false success and SM-C1 protects the consequence-preview requirement. The following measurement gap limits interpretation of a good outcome.
- **Done-ness clarity — thin.** The interaction contract distinguishes acceptance, projection confirmation and audit evidence. The configuration and recovery rules below would nevertheless produce materially different acceptance tests from those supported by the current service.
- **Scope honesty — adequate.** Sections 13 and 14 preserve the original read-only MVP while explaining the later command phases. Addendum §J distinguishes local measurements from the four open owner checks. The September CLI decision needs to travel with a frequently extracted exclusion.
- **Downstream usability — thin.** FR and journey identifiers are stable. Current implementation evidence is harder to follow because parts of the transport inventory and story mapping retain superseded descriptions.
- **Shape fit — adequate.** The named journeys suit an operations UI shared by members, owners and global administrators. The addendum gives engineering a place for detailed contracts. The product-level sections work best when a single current decision governs each feature and the chronology stays with its supporting evidence.

## Findings by severity

### Critical (0)

None.

### High (4)

**Decision-readiness — An approved audit performance contract still appears blocked (§§7.8–7.9, 8 NFR-1, 16.14)**

The PRD says “no numeric budget exists.” The owner approved the 500-entry, 50-row-page workload on September 23. The September 24 run passed all 126 percentile groups and ten functional gates. Initial render has p75 ≤ 2.5 s and p95 ≤ 4.0 s on the dedicated environment. Reopening the budget would delay qualification and leave teams measuring different outcomes. See `_bmad-output/implementation-artifacts/story-5-1-performance-decision.md:3` and `release-readiness.md:544` in the same folder. [Approved budget and run](../../../implementation-artifacts/story-5-1-performance-decision.md#L29) · [Candidate rerun criteria](../../../implementation-artifacts/release-readiness.md#L544)

**Suggested change.** Reference the decision and accepted run, identify which candidate changes require a rerun, and keep the November production index-capacity measurement separate.

**Done-ness clarity — Configuration display lacks the exact-key safety rule (§7.2 FR-6, §§10, 16.11)**

“Filtered to the namespaces they own” allows an acceptance test to pass when an unapproved key shares an authorized prefix. The approved DW-599 policy requires an exact display-safe key for everyone. Global administrators bypass only the prefix check. Missing or malformed policy withholds configuration from the UI. See `_bmad-output/implementation-artifacts/dw-117-118-127-599-618-validation-evidence.md:10` and `TenantConfigurationSafeComposer.cs:177–201`. [Configuration display decision](../../../implementation-artifacts/dw-117-118-127-599-618-validation-evidence.md#L10)

**Suggested change.** Specify separate acceptance cases for an unlisted key under an allowed prefix, an administrator viewing an unlisted key, and unavailable policy. Keep this display rule distinct from the authorized API response.

**Done-ness clarity — Last-owner recovery cites a bootstrap that cannot reopen (§7.9 FR-24, addendum §D)**

The matrix says first-member bootstrap “enables restore-after-last-owner.” Removing a member leaves `HasMembershipHistory` true. An active tenant that previously had members still requires an existing owner or a global administrator to add one. With zero owners, recovery requires a global administrator. `TenantState.cs:42–50`, `TenantAggregate.cs:103–109`, and `TenantAggregateTests.RBAC_previously_populated_tenant_cannot_reopen_bootstrap_after_becoming_empty` establish the distinction. [Membership history](../../../../src/Hexalith.Tenants.Server/Aggregates/TenantState.cs#L42) · [Add authorization](../../../../src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs#L96) · [Regression](../../../../tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs#L1626)

**Suggested change.** Give initial enrollment and post-removal recovery separate rows. Specify who can restore the owner, the unauthorized outcome and the escalation path.

**Done-ness clarity — Tenant corrections promise proof without a causal link (§3.3 UJ-4, §7.9 FR-25)**

“Both audit records reference each other” makes pairing unconditional. The October 21 Story 5.6 correction and `TenantCorrectionPreviewSnapshot.cs:402–410` retain the original evidence, clear the proof link and return `MissingSupport` when the tenant workflow cannot establish the association. Following FR-25 alone could turn a confirmed state change into false audit proof. [Superseded Story 5.6 disposition](../../../implementation-artifacts/5-6-preview-and-confirm-correction-with-linked-proof.md#L11) · [Tenant proof handling](../../../../src/Hexalith.Tenants.UI/State/TenantAudit/TenantCorrectionPreviewSnapshot.cs#L410) · [Administrator receipt requirements](../../../implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md#L81)

**Suggested change.** Specify the tenant missing-proof outcome and its recovery. Require verified causation before displaying a pair, and describe the distinct global-administrator proof path.

### Medium (7)

**Decision-readiness — The handoff still describes the global-administrator correction as unavailable (§7.9, addendum §§A–B)**

The PRD ties availability to Story 5.7 completion. The sprint tracker marks it done, `TenantAuditPage.razor:266` mounts its panel, and `release-readiness.md:104` describes the fixed-scope evidence path. Operators can qualify or withhold this action per attempt today. [Story status](../../../implementation-artifacts/sprint-status.yaml#L82) · [Pagination resolution](../../../implementation-artifacts/sprint-status.yaml#L329) · [Audit page](../../../../src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor#L266) · [Recovery tests](../../../../tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorCorrectionPanelRecoveryTests.cs#L39)

**Suggested change.** Describe the delivered capability, the conditions that disable it for an individual attempt, and outstanding evidence for a particular release.

**Strategic coherence — Zero observed false successes has no defined observation coverage (§15 SM-6)**

“Zero confirmed cases” could describe zero observed command journeys. The PRD does not say which submitted journeys are checked or how to match a displayed success with projection evidence.

**Suggested change.** Define the observed cohort, qualifying evidence, owner and reporting window. Report observation coverage alongside the incident count using support-safe data.

**Done-ness clarity — The disabled-tenant rule would block reactivation (§4, §7.5 FR-15, addendum §D)**

“Any command targeting a disabled tenant” includes the command that restores it. `TenantAggregate.cs:58–93` allows an authorized global administrator to enable a disabled tenant and returns a lifecycle-already-set rejection for repeated disable. Its access-refresh handler also supports a disabled tenant under its own authorization rules. [Lifecycle handlers](../../../../src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs#L58) · [Snapshot rule](../../../../src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs#L245)

**Suggested change.** Scope the rejection to ordinary disabled-tenant mutations. State the expected results for enable, repeated transitions, unauthorized calls and the supported refresh operation.

**Scope honesty — Hard-deletion text assigns a future tool that the governing decision superseded (§0, §7.5 FR-15, §14.3, addendum §B)**

The September decision names `Hexalith.McpCli` as the target for approved operations. FR-15 still assigns hard deletion to “future independent administrators-only CLI tooling.” A team extracting the feature could infer approval to create a module CLI.

**Suggested change.** Preserve hard deletion as a UI non-goal and refer future proposals to the approved enrollment and authorization decision.

**Downstream usability — The read and freshness handoff describes superseded routing (§8 NFR-1, §11, addendum §§C–D)**

Addendum §D calls the generic EventStore query route “current.” The UI uses its REST client and classifies verified projection metadata on successful and conditional responses. The six read categories also span a dedicated global-administrator audit path omitted from §C. See `TenantsRestQueryClient.cs:128–157,251–255,406–429` and `release-readiness.md:55`.

**Suggested change.** Date the prior transport description, list the fixed-scope audit route under the audit read, and distinguish per-response provenance from deployment qualification.

**Downstream usability — Administrator delivery references point to different stories in the current tracker (§14.2, addendum §B)**

“Stories 4.3 and 4.4” refers to June identifiers. The current tracker maps grant to 4.2 and removal to 4.3. The migration is documented in `_bmad-output/implementation-artifacts/release-readiness.md:127`.

**Suggested change.** Cite current story IDs and keep the original dated artifacts as historical evidence.

**Domain fidelity — Role promotion (FR-11, addendum §D)**

“Role escalation” could be read as prohibiting promotion between valid tenant roles. An authorized owner can promote a reader to contributor. The rejection named `RoleEscalation` applies to the unassignable `Unknown` sentinel or an out-of-range target role.

**Suggested change.** Specify actor authorization and allowed target roles separately, with a valid promotion example. [Role handler](../../../../src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs#L198) · [Owner promotion regression](../../../../tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs#L1455)

### Low (2)

**Shape fit — Accessibility verification names an obsolete Fluent dependency (§16.5, addendum §D)**

The PRD calls `5.0.0-rc.4-26180.1` the pinned version. The centrally imported Builds package configuration selects `5.1.0` in `Props/Directory.Packages.props`. Reviewers could qualify controls against the wrong package.

**Suggested change.** Point to the version used by the qualifying build and retain the open WCAG 2.2 evidence requirement.

**Domain fidelity — Metadata editing (FR-14, addendum §D)**

“Contributor or global administrator” omits tenant owners when read as a list of eligible roles. The handler treats contributor as a minimum and the owner test succeeds.

**Suggested change.** Include contributors, owners and global administrators explicitly. [Metadata handler](../../../../src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs#L40) · [Owner regression](../../../../tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs#L1523)

## Domain fidelity cross-check

FR-24 gives the wrong authority for restoring the last tenant owner. FR-25 also promises a paired audit receipt where the tenant correction workflow explicitly reports missing support. Resolve both before using these requirements as acceptance criteria. The current domain supports deliberate removal of the last tenant owner and protects the last global administrator. FR-12’s “already applied” outcome is supported when a fresh projection proves the target is absent after a membership rejection. [Last-owner regression](../../../../tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs#L1333) · [Last-administrator invariant](../../../../src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsAggregate.cs#L50) · [Removal confirmation](../../../../src/Hexalith.Tenants.UI/State/TenantCommands/TenantRemoveMemberCommandSnapshot.cs#L344)

## Mechanical notes

- FR-1 through FR-25, UJ-1 through UJ-6, SM-1 through SM-6 and SM-C1 through SM-C3 each have a unique definition. All six journeys have named protagonists. The 17 explicit Markdown links in the PRD and addendum resolve to local files.
- Section 17 covers the substantive inline assumptions. Section 9 still calls localization ownership an open question although §16.4 and §17 record the D4 decision. R-6 also requests a correction already visible in `docs/tenants-ui-operations-shell-spec.md:15,186`, which identifies tenant and user IDs as caller-supplied strings.

## Reviewer files

- [Full rubric review](review-rubric-pre-update-2026-11-20.md)
- [Domain fidelity and source evidence](review-domain-2026-11-20.md)
