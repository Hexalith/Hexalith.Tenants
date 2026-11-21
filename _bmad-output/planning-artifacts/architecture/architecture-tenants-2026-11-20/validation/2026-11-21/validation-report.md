# Hexalith.Tenants architecture validation

21 November 2026 · Standalone review of the working tree based on `f15f6170`.

## Verdict

**Revise AD-8 and AD-12 before implementation handoff.** The PRD settles how stale reads affect high-impact writes. Product and Architecture still need to reconcile when a tenant becomes writable again after an operator stops tracking an unresolved command.

| Critical | High | Medium | Low | Spine lint findings |
| ---: | ---: | ---: | ---: | ---: |
| 0 | 2 | 5 | 2 | 0 |

### High · Decide when an unresolved command releases the tenant lock

AD-12 and the PRD addendum hold the aggregate lock until terminal evidence arrives. After a possibly delivered enable or disable command, **Stop tracking** discards the local attempt and releases the lock. The remote outcome remains unknown. Configuration edits also release tracking on explicit abandonment or retention expiry. A second command can then acquire admission on that tenant.

Product and Architecture need to agree when another write is admissible after abandonment or expiry, what evidence it requires, and which recovery actions remain available. Record the rule in AD-12 and the PRD addendum. Preserve the distinction between ending local tracking and cancelling or confirming the remote command. [AD-12](../../../../architecture.md#L159) · [PRD addendum](../../../../prds/prd-tenants-2026-06-02/addendum.md#L28) · [Lifecycle release](../../../../../../src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleActionAvailability.razor#L445) · [Ambiguous delivery test](../../../../../../tests/Hexalith.Tenants.UI.Tests/Components/TenantLifecycleActionAvailabilityTests.cs#L2206) · [Configuration release](../../../../../../src/Hexalith.Tenants.UI/Components/Tenants/Configuration/SetTenantConfigurationFlow.razor#L946)

### High · A valid stale 304 can support display while a high-impact write remains blocked

AD-8 permits reuse of a cached projection after a valid stale HTTP 304. Its blocking rule covers missing, contradictory and degraded evidence. AD-12 asks the caller to recheck freshness without specifying the result for well-formed stale evidence. A new surface could therefore enable a destructive preview from a cached row that is safe to display. The PRD and the current availability evaluator both block that action.

State the stale and unknown outcomes explicitly at preview, dispatch and delivery retry. Keep the narrowly scoped first-tenant creation exception in AD-8. For the lifecycle and configuration controls, refreshing requires a current baseline. [AD-8](../../../../architecture.md#L132) · [AD-12 high-impact actions](../../../../architecture.md#L161) · [PRD CP-2](../../../../prds/prd-tenants-2026-06-02/prd.md#L188) · [High-impact evaluator](../../../../../../src/Hexalith.Tenants.UI/State/TenantDetail/TenantHighImpactActionAvailabilityEvaluator.cs#L34)

### Medium · Use one lock per circuit and aggregate

AD-12 permits two unrelated tenants to proceed in one circuit. The cross-component guidance says the shared gate admits one operation per circuit, and the sibling-control test guidance leaves the aggregate unspecified.

Scope both instructions to the same aggregate. The regression case acquires locks for tenants alpha and beta concurrently and releases them independently. [AD-12](../../../../architecture.md#L159) · [Cross-component guidance](../../../../architecture.md#L543) · [Sibling controls](../../../../architecture.md#L660) · [Two-tenant regression](../../../../../../tests/Hexalith.Tenants.UI.Tests/State/TenantAggregateCommandAdmissionGateTests.cs#L90)

### Medium · Include projection lifecycle in conditional-read validation

D6 describes current, stale and unknown freshness. The REST client also reads projection lifecycle. Rebuilding, Degraded, Unavailable and LocalOnly each invalidate a 304, even with a matching validator. A client following only the freshness description could reuse an invalid response.

Document the lifecycle check and its relationship to the stale flag. Verify the four invalid lifecycle cases independently of the valid stale display case. [D6](../../../../architecture.md#L443) · [REST metadata handling](../../../../../../src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryClient.cs#L406) · [304 regression cases](../../../../../../tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantsRestQueryClientTests.cs#L1111)

### Medium · Separate a status observation from proof of a correction

The status guidance requires the returned domain to match for every lookup. For an ordinary tracked command the gateway rejects an explicitly wrong domain, yet can accept a status with the field absent when the other identity checks pass. The same incomplete response supplies no committed sequence for correction proof.

Describe ordinary status lookup, governance recovery and correction proof separately. Retain the full scope and sequence checks before using a result as proof. [Status guidance](../../../../architecture.md#L502) · [Status identity checks](../../../../../../src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs#L630) · [Correction proof checks](../../../../../../src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs#L678) · [Incomplete-scope regression](../../../../../../tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs#L2102)

### Medium · Require elevated confirmation for a tenant member with verified platform authority

AD-12 names the last-owner case. The PRD independently raises the level of confirmation when the target also holds global-administrator authority, even if other tenant owners remain. The membership UI implements both conditions.

Include this second trigger in AD-12. Reflect verified standing, present unresolved standing as unknown, and keep the command scoped to tenant membership. [AD-12](../../../../architecture.md#L162) · [PRD CP-6](../../../../prds/prd-tenants-2026-06-02/prd.md#L192) · [Removal risk checks](../../../../../../src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor#L598) · [Confirmation copy](../../../../../../src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor#L744)

### Medium · Check authority before explaining a blocked action

AD-12 lists admission checks without specifying which reason to display when several fail. For lifecycle and configuration controls, missing or indeterminate authority takes precedence over stale data or domain state. Returning a tenant-specific reason first could reveal state to a caller whose authority is unverified.

Bind these controls to the existing evaluator. Report missing permission and suppress domain outcomes until authority is established. [AD-12](../../../../architecture.md#L161) · [Reason precedence](../../../../../../src/Hexalith.Tenants.UI/State/TenantDetail/TenantHighImpactActionAvailabilityEvaluator.cs#L24) · [Multiple-failure test](../../../../../../tests/Hexalith.Tenants.UI.Tests/State/TenantHighImpactActionAvailabilityTests.cs#L50)

### Low · Align the PRD with the localization format in AD-9

The architecture and resources use indexed .NET placeholders such as `{0}`. The PRD still calls for named placeholders. A translator following that instruction could produce a string that fails to format.

Correct the PRD wording. Keep each argument index and its meaning stable across cultures. [AD-9](../../../../architecture.md#L138) · [Worked resource example](../../../../architecture.md#L585) · [PRD localization](../../../../prds/prd-tenants-2026-06-02/prd.md#L354)

### Low · Describe the supported local Dapr startup

The architecture describes slim mode as the development startup. The quickstart calls for full initialization to supply local services, while the AppHost configures a private control plane including Sentry, placement and scheduler.

Point developers to the quickstart and state the prerequisites for an intentional slim setup. [Development workflow](../../../../architecture.md#L782) · [Quickstart](../../../../../../docs/quickstart.md#L42) · [AppHost setup](../../../../../../src/Hexalith.Tenants.AppHost/Program.cs#L33)

## Verified in this checkout

All 15 decision IDs match the committed architecture. All 47 declared inputs and 51 relative file references in the canonical document resolve. The BMAD spine linter reported zero findings. The reviewed working copy has SHA-256 `2e218ac1fd0e96522ffd084bce9bfbff8440161616739f088121260e34fc5042`. [Canonical architecture](../../../../architecture.md)

The authenticated-routes workflow defines the four suites named in AD-14: `rest`, `persisted`, `anonymous-ui` and `authenticated-ui`. The local host-selection check located the declared EventStore 5.4.0 source commit `2ec21f63`. All 15 Python tests in the source-catalog and product-integration profile suites passed. These exercise evidence and lifecycle handling. Hosted runs and .NET product suites were outside this validation. [Hosted matrix](../../../../../../.github/workflows/ci.yml#L188) · [Selection check](../../../../../../scripts/prepare-bootstrap-compatibility.sh#L23) · [Runtime evidence](../../../../../../scripts/prepare-source-catalog-profile.py#L252)

The refreshed Builds checkout `e1380b8` selects EventStore 5.4.0, FrontComposer 6.0.1 and Memories 4.1.0. The committed Tenants revision still points to Builds `a8e2ae29`, which lacks the release declaration the selector requires. The local check used the refreshed checkout. [Catalog comparison](../../../../architecture.md#L288)

## Evidence still owed by owners

Release owns the four hosted results on the same candidate and shipping packages. The first access-enforcing consumer owns checkpoint and restart evidence. Deployment owns the production identity and tenant-index capacity checks. The architecture also tracks the audit-component data boundary, UI telemetry, topology and replica prerequisites, and causal pairing of tenant correction receipts. These previously recorded items are separate from the nine findings. [Conformance and qualification](../../../../architecture.md#L184) · [Handoff](../../../../architecture.md#L787) · [Sprint tracker](../../../../../implementation-artifacts/sprint-status.yaml#L408)

## Independent reviews

[Requirements and enforceability](reviews/review-rubric.md) · [Technology and implementation](reviews/review-technology.md) · [Independent implementation choices](reviews/review-divergence.md)
