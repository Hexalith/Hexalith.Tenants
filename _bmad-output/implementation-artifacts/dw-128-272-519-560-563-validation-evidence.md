# DW-128, DW-272, DW-519, DW-560, DW-563 validation evidence

Date: 2026-10-25. Tenants baseline `86293c99ff1b4dfe2c81474eaae5bd934ec18f3a`. EventStore baseline `38393a15659eeb8a979764a97d185e868c2b33e6`.

## Commands

- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --filter FullyQualifiedName~List_search_ends_paging_when_every_candidate_is_hidden|FullyQualifiedName~List_search_keeps_paging_when_a_window_is_only_missing|FullyQualifiedName~List_search_reaches_an_authorized_match|FullyQualifiedName~List_search_renders_a_fully_hidden_window|FullyQualifiedName~Pending_audit_probe_does_not_report|FullyQualifiedName~Resolved_missing_audit_capability|FullyQualifiedName~Disconnected_read_surface_defers` — 7 passed after the row-reason assertion correction; the first run failed 3 audit assertions because the reason catalog always renders every reason.
- `dotnet test references/Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/Hexalith.EventStore.Server.Tests.csproj --filter FullyQualifiedName~GetEventsAsync_CountAboveLegacyArrayBudget|FullyQualifiedName~GetEventsAsync_PayloadAboveLegacyArrayBudget` — 2 passed.
- `dotnet test tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --filter FullyQualifiedName~PackageGovernanceTests.Release_workflow_packs_validates_and_publishes_only_expected_packages|FullyQualifiedName~PackageGovernanceTests.Release_workflows_publish_to_nuget_through_trusted_publishing|FullyQualifiedName~PackageGovernanceTests.Release_workflow_proves_the_tag_floor_covers_the_registry_before_approval` — 3 passed.

## Limits

`PartialReleasePublicationTests.UnchangedSourceCanPublishMissingArtifacts` already fails on the unmodified scripts because `oci_registry_validator.py` requires `--created` and `scripts/partial-release-recovery-common.sh` does not pass it. The recovery container publication therefore stays on the existing SDK registry push. That failure is outside these five records.

DW-127, DW-570, DW-618, the archive, sprint status, and unrelated ledger bodies stay open.
