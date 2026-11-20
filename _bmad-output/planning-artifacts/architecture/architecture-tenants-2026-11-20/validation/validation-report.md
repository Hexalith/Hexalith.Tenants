# Hexalith.Tenants architecture validation

20 November 2026 · Working tree based on `6627fc3d`

## Verdict

**Revise AD-12 and the high-impact action rules before using this document for implementation handoff.** Both gaps can change what an operator sees or is allowed to submit. The other five findings concern integration behavior and UI contracts.

| Critical | High | Medium | Mechanical findings |
| ---: | ---: | ---: | ---: |
| 0 | 2 | 5 | 0 |

## Findings

### High · A matching value cannot establish command success

AD-12 treats a pre-existing state as already applied. FR-14 requires every successful tenant metadata edit to emit an update, even when the submitted values are identical. An unrelated update can advance the aggregate version while those values stay the same. The UI regression cases cover both outcomes: without evidence for this command the result is unverifiable; with matching audit provenance it can be confirmed.

**Suggested correction.** Reserve “already applied” for operations that define an authoritative NoOp. For an identical metadata edit, require the expected projection plus evidence attributable to that command before confirming. Carry this exception into the command confirmation guidance.

[architecture.md L155](../../../architecture.md#L155) · [prd.md L275](../../../prds/prd-tenants-2026-06-02/prd.md#L275) · [TenantUpdateMetadataCommandSnapshot.cs L254](../../../../../src/Hexalith.Tenants.UI/State/TenantCommands/TenantUpdateMetadataCommandSnapshot.cs#L254) · [TenantUpdateMetadataCommandSnapshotTests.cs L124](../../../../../tests/Hexalith.Tenants.UI.Tests/State/TenantUpdateMetadataCommandSnapshotTests.cs#L124) · [TenantUpdateMetadataCommandSnapshotTests.cs L143](../../../../../tests/Hexalith.Tenants.UI.Tests/State/TenantUpdateMetadataCommandSnapshotTests.cs#L143)

### High · High-impact actions need a measured safe viewport

The shared eligibility recipe requires valid inputs, freshness and authority. A new flow following that recipe could permit submission after a resize hides critical facts or before layout measurement completes. The removal and administrator flows already check that the current viewport can present the safety context.

**Suggested correction.** Add measured viewport safety to preview, dispatch and retry eligibility for high-impact flows. An unknown or unsafe measurement blocks the action and gives the operator a visible reason. Preserve read-only triage and status recovery.

[architecture.md L155](../../../architecture.md#L155) · [architecture.md L657](../../../architecture.md#L657) · [prd.md L175](../../../prds/prd-tenants-2026-06-02/prd.md#L175) · [RemoveTenantMemberFlow.razor L272](../../../../../src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor#L272) · [GlobalAdministratorsPage.razor L1149](../../../../../src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor#L1149) · [GlobalAdministratorsPage.razor L1376](../../../../../src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor#L1376)

### Medium · The generated REST API uses camel-case JSON

The naming guidance prescribes PascalCase on the wire. The generated-controller contract test reads the literal JSON properties `items` and `tenantId`. A case-sensitive client following the document would look for different keys.

**Suggested correction.** Distinguish C# property names from public JSON names. Cite the generated REST contract and include one literal response example.

[architecture.md L589](../../../architecture.md#L589) · [TenantsApiGeneratedControllerTests.cs L156](../../../../../tests/Hexalith.Tenants.IntegrationTests/TenantsApiGeneratedControllerTests.cs#L156)

### Medium · Command recovery depends on where the conflict occurred

The retry guidance groups HTTP 409 and the tracked failure reason `ConcurrencyConflict` under one recovery action. The gateway distinguishes transport status, a domain rejection during submission, and a verified command-status result. Receiving HTTP 409 alone cannot establish that a retryable tracked command exists.

**Suggested correction.** Document submission and status lookup separately. Select recovery from the operation result and verified reason. Keep the original message ID for a permitted retry or status lookup.

[architecture.md L499](../../../architecture.md#L499) · [architecture.md L661](../../../architecture.md#L661) · [TenantCommandGateway.cs L565](../../../../../src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs#L565) · [TenantCommandGateway.cs L718](../../../../../src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs#L718) · [TenantCommandGateway.cs L949](../../../../../src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs#L949) · [TenantCommandGateway.cs L1310](../../../../../src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs#L1310) · [TenantCommandGatewayTests.cs L2143](../../../../../tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs#L2143)

### Medium · Memories search needs a deployed index publisher

The architecture assigns index maintenance to a Tenants consumer. In this checkout, only the sample host registers the four lifecycle handlers that publish curated entries. AppHost can start Memories while omitting the sample. In that topology, the search service can be reachable while new or renamed tenants are absent from its candidate set. Authoritative hydration still protects returned rows.

**Suggested correction.** Name the registered publisher and distinguish sample composition from the production deployment decision. For search-enabled deployments, assign an owner for event delivery and verify that a tenant creation and rename reach the index.

[architecture.md L507](../../../architecture.md#L507) · [Program.cs L18](../../../../../samples/Hexalith.Tenants.Sample/Program.cs#L18) · [Program.cs L183](../../../../../src/Hexalith.Tenants.AppHost/Program.cs#L183) · [Program.cs L238](../../../../../src/Hexalith.Tenants.AppHost/Program.cs#L238)

### Medium · The state lists omit operator-visible outcomes

The architecture calls four audit states and six list states canonical. Audit availability has four non-success states plus `Available`. The shared list view also distinguishes `Unauthorized` and `SearchPageEmpty`. Treating either list as exhaustive could hide a verified receipt or present an authorization denial as an empty result.

**Suggested correction.** Identify the enumerations as the source of truth. Label subsets by purpose and retain separate rendering for verified evidence, authorization denial and an empty search page.

[architecture.md L250](../../../architecture.md#L250) · [architecture.md L527](../../../architecture.md#L527) · [architecture.md L653](../../../architecture.md#L653) · [TenantAuditAvailabilityState.cs L5](../../../../../src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditAvailabilityState.cs#L5) · [ListSurfaceStates.razor L43](../../../../../src/Hexalith.Tenants.UI/Components/Shared/ListSurfaceStates.razor#L43)

### Medium · Localization examples use the wrong placeholder syntax

The naming guidance shows literal named placeholders such as `{tenantName}`. The English and French resource files use indexed composite formatting, and Razor passes ordered arguments through `IStringLocalizer`. Copying the documented syntax into a resource can cause a format error when the operator opens that view.

**Suggested correction.** Keep whole-string localized resources and use indexed placeholders such as `{0}` and `{1}` consistently across cultures. Show a resource value beside the call that supplies its arguments.

[architecture.md L533](../../../architecture.md#L533) · [architecture.md L604](../../../architecture.md#L604) · [TenantsResources.resx L424](../../../../../src/Hexalith.Tenants.UI/Resources/TenantsResources.resx#L424) · [TenantsResources.fr.resx L115](../../../../../src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx#L115) · [SetTenantConfigurationFlow.razor L18](../../../../../src/Hexalith.Tenants.UI/Components/Tenants/Configuration/SetTenantConfigurationFlow.razor#L18)

## Verified decisions and open evidence

The direct read path through `tenants-api`, separate command/status egress, scoped search continuation and per-circuit, per-aggregate command admission agree with the implementation. The project targets .NET 11 with SDK 11.0.100. The declared package baseline agrees with the repository build files. [TenantsUiServiceCollectionExtensions.cs L139](../../../../../src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs#L139) · [TenantSearchCursorCodec.cs L9](../../../../../src/Hexalith.Tenants.UI/Services/Gateways/TenantSearchCursorCodec.cs#L9) · [TenantAggregateCommandAdmissionGate.cs L6](../../../../../src/Hexalith.Tenants.UI/State/TenantCommands/TenantAggregateCommandAdmissionGate.cs#L6) · [global.json](../../../../../global.json) · [Directory.Build.props](../../../../../Directory.Build.props)

The September audit-grid reference run met its documented budgets. Candidate qualification still applies after relevant changes. The four November checks below already have owners in the sprint tracker and are excluded from the seven new findings. [story-5-1-performance-decision.md L3](../../../../implementation-artifacts/story-5-1-performance-decision.md#L3) · [sprint-status.yaml L408](../../../../implementation-artifacts/sprint-status.yaml#L408)

| Open check | Owner | Evidence still due |
| --- | --- | --- |
| Hosted qualification | Release owner | Cold bootstrap, persisted state, authenticated REST and authenticated UI on the same candidate and shipping package set. |
| First enforcing consumer | Consumer deployment owner | Durable state, authorized history and correct access after delayed removal and restart. |
| Deployed identity | Deployment owner | Redacted verification of issuer, audience, discovery TLS, ingress and key rotation. |
| Tenant-index capacity | Deployment owner | Index size, write contention and update latency under representative load. |

The canonical handoff also records the server-component audit-data boundary, UI health and telemetry, platform topology and scale-out prerequisites, and tenant receipt pairing. Those items retain their existing closure conditions. [architecture.md L818](../../../architecture.md#L818)

## Scope and checks

This standalone validation reviewed the uncommitted canonical document at SHA-256 `a8f30660db2f0a45cd38fc0b2fe571b9c87b69e3c78abdf6fa344bde3828d021`. AD-1 through AD-15 are consecutive. BMAD lint returned zero findings. All 46 listed input files and all 50 previously linked local references in the architecture and its update reviews resolved. [Canonical architecture](../../../architecture.md)

Three fresh reviews assessed requirement coverage, technology and implementation, and the places where independent teams could interpret a rule differently. Findings shared by more than one reviewer are counted once. Priority reflects the effect on a future implementation. This review inspected source and existing test assertions. It did not execute product suites or hosted profiles.

[Requirements review](reviews/review-rubric.md) · [Technology review](reviews/review-technology.md) · [Independent implementation review](reviews/review-divergence.md)
