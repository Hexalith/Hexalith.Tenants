# Independent implementation divergence review

20 November 2026 · Hexalith.Tenants · Working-tree architecture at parent revision `6627fc3d`

**Verdict.** One high and two medium documentation findings warrant an update. They concern the command result reported for an identical metadata edit, the conditions for dispatching a high-impact action, and the vocabulary used by read surfaces. Source inspection covered the current checkout.

## High · A matching pre-existing value cannot decide command outcome

AD-12 classifies a pre-existing state as already applied. One UI can end a same-value metadata edit as soon as its existing projection matches. Another can wait for proof of the new `TenantUpdated` event and confirm that specific edit. FR-14 requires every successful metadata edit to emit the event. The snapshot regression covers both an unrelated projection advance and matching audit provenance.

Reserve `already applied` for a verified operation-specific NoOp. For identical metadata, require evidence attributable to the edit and an authoritative matching projection. An unrelated version advance cannot establish confirmation. [AD-12](../../../../architecture.md#L155) · [FR-14](../../../../prds/prd-tenants-2026-06-02/prd.md#L275) · [Confirmation logic](../../../../../../src/Hexalith.Tenants.UI/State/TenantCommands/TenantUpdateMetadataCommandSnapshot.cs#L260) · [Paired regression cases](../../../../../../tests/Hexalith.Tenants.UI.Tests/State/TenantUpdateMetadataCommandSnapshotTests.cs#L124)

## Medium · High-impact dispatch needs current viewport evidence

The listed preview and dispatch inputs omit the measured viewport. One implementation could use CSS to hide controls after a resize while its pending grant or retry stays eligible. Another could recheck the measurement and block dispatch. The PRD requires a visible unavailable reason when the safety context cannot fit. The governance page requires an observed safe viewport for grant dispatch and ambiguous removal retry.

Bind preview, dispatch and retry for affected flows to the shared viewport observation. Unknown or unsafe measurements should keep protected work unavailable and preserve read-only access. [AD-12](../../../../architecture.md#L155) · [Process rules](../../../../architecture.md#L657) · [Form factors](../../../../prds/prd-tenants-2026-06-02/prd.md#L179) · [Grant prerequisites](../../../../../../src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor#L1149) · [Removal retry](../../../../../../src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor#L1376)

## Medium · Enumerated subsets read as complete display contracts

The architecture requires four audit values verbatim and calls six named list states the shared pattern for every grid. A literal implementation could omit the verified-receipt state from its availability control or map denied access to an empty or failed list. A second implementation using the shared controls would render `Available` on proven evidence and `Unauthorized` distinctly. Even the search-specific empty-page recovery has its own list state.

Describe the four non-success audit outcomes and the six baseline list presentations as subsets. Point implementers to the typed audit and list controls for the complete outcomes and recovery behavior. [Truth-state vocabulary](../../../../architecture.md#L525) · [List patterns](../../../../architecture.md#L653) · [Audit availability enum](../../../../../../src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditAvailabilityState.cs#L5) · [List state mapping](../../../../../../src/Hexalith.Tenants.UI/Components/Shared/ListSurfaceStates.razor#L43)

## Recorded evidence boundaries

AD-9 already records internal proof fields reaching a server-side grid. The four hosted qualification and rollout checks have named owners. The September audit-grid benchmark refers to its dedicated workload. The AD-15 checkpoint rule covers durable contiguous progress, contested writes and observed sequence gaps. These items do not add to the three findings above. [Support-safe component boundary](../../../../architecture.md#L135) · [Consumer checkpoint](../../../../architecture.md#L175) · [Outstanding evidence](../../../../architecture.md#L183)
