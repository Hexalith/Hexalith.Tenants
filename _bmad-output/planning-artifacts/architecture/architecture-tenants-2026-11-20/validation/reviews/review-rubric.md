# Architecture rubric review

**Verdict.** Revise the high-impact command admission rule before handing it to another team. One high and two medium documentation findings follow. The existing member-removal flow already enforces the missing safeguard.

## High · High-impact command admission omits measured viewport safety

AD-12 and the shared preview recipe require validation, freshness, authority and preview evidence. Neither tells a component what to do before a viewport measurement arrives or when the layout can no longer display the safety context. Sibling implementations can satisfy the stated checks yet offer different commands on a phone. The PRD reserves mobile for read-only triage and requires a visible explanation when safety context cannot fit. The removal component explicitly checks the measured viewport. [AD-12](../../../../architecture.md#L155) · [Preview recipe](../../../../architecture.md#L657) · [PRD form factors](../../../../prds/prd-tenants-2026-06-02/prd.md#L175) · [Circuit measurement](../../../../../../src/Hexalith.Tenants.UI/State/TenantDetail/TenantHighImpactViewportObservation.cs#L29) · [Removal gate](../../../../../../src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor#L471)

**Correction.** Require measured safe layout before initiating, confirming or retrying a high-impact action. Recheck safety when the viewport changes, show the reason while unsafe or unmeasured, and preserve tracking and recovery for commands already sent. [Removal lifecycle](../../../../../../src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor#L163) · [Viewport-change regression](../../../../../../tests/Hexalith.Tenants.UI.Tests/Components/RemoveTenantMemberFlowTests.cs#L2507)

## Medium · Public REST field casing conflicts with the generated controller

The naming rule prescribes PascalCase on the wire. The authenticated generated-controller fixture reads the actual JSON keys `items` and `tenantId`. A case-sensitive client following the documented casing will fail to read that response. Specify the public REST serializer contract, retain PascalCase for CLR members, and show a real response field. [Architecture naming rule](../../../../architecture.md#L589) · [Controller response assertion](../../../../../../tests/Hexalith.Tenants.IntegrationTests/TenantsApiGeneratedControllerTests.cs#L156) · [CLR contract](../../../../../../src/Hexalith.Tenants.Contracts/Queries/TenantSummary.cs#L8)

## Medium · The documented placeholders are incompatible with the UI formatter

The localization rule demonstrates named placeholders such as `{tenantName}`. Existing whole-string resources use .NET composite formatting with positional placeholders such as `{0}` and `{1}`. The UI passes those arguments by position. Following the example can cause a format error when the control renders. Require whole localized strings with positional placeholders and preserve their argument indices in English and French. [Architecture example](../../../../architecture.md#L604) · [Single-argument resource](../../../../../../src/Hexalith.Tenants.UI/Resources/TenantsResources.resx#L115) · [Two-argument resource](../../../../../../src/Hexalith.Tenants.UI/Resources/TenantsResources.resx#L424) · [Localizer call](../../../../../../src/Hexalith.Tenants.UI/Components/Tenants/Configuration/SetTenantConfigurationFlow.razor#L18)

## Coverage and existing qualifications

The decisions cover shell ownership, public routes, read and command boundaries, state and evidence ownership, authorization, operator recovery, deployment and downstream membership. The feature map accounts for FR-1 through FR-25. The document assigns the four external checks to release and deployment owners and separately records the server-side audit data boundary, UI telemetry, orchestration, scale-out prerequisites and tenant receipt pairing. This review compared documents, source and test assertions. Production qualification requires the evidence assigned to those owners. [Open owner checks](../../../../architecture.md#L183) · [Feature map](../../../../architecture.md#L761) · [Handoff](../../../../architecture.md#L819)
