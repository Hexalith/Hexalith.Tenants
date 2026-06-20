---
title: 'DAPR deployment docs and deferred record cleanup'
type: 'correct-course-hardening'
created: '2026-06-19'
status: 'review'
baseline_commit: '8c332d331ce6193f78a61f164348c82acde1d4a8'
sprint_key: 'cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup'
source_proposal: '_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-19-deferred-work.md'
approval: 'Administrator approved sprint-change-proposal-2026-06-19-deferred-work.md on 2026-06-19'
context:
  - '{project-root}/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/docs/cross-aggregate-timing.md'
  - '{project-root}/tests/Hexalith.Tenants.Server.Tests/Documentation/CrossAggregateTimingDocumentationTests.cs'
  - '{project-root}/_bmad-output/implementation-artifacts/deferred-work.md'
---

<frozen-after-approval reason="human-approved correct-course scope - deployment/docs cleanup only">

## Intent

Make deployment pub/sub scope documentation and deferred-work records truthful after the June 18 DAPR dead-letter correction, and close stale entries that current source no longer supports.

## Boundaries & Constraints

**Always:**
- Keep Tenants domain packages free of broker/provider dependencies.
- Treat EventStore's application-level dead-letter publisher as the current working dead-letter mechanism unless a separate owner-approved story changes the topology.
- Verify DAPR topic scoping against official DAPR topic-scoping syntax before changing `publishingScopes` or `subscriptionScopes`.
- Keep local AppHost and production deployment templates intentionally different only when the difference is documented.

**Never:**
- Do not add DAPR native dead-letter subscription wiring unless the EventStore subscription owner approves it.
- Do not edit EventStore or FrontComposer submodule files from this repository.
- Do not carry stale review claims after current source disproves them.

</frozen-after-approval>

## Code Map

- `deploy/dapr/pubsub.yaml`
- `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml`
- `docs/cross-aggregate-timing.md`
- `tests/Hexalith.Tenants.Server.Tests/Documentation/CrossAggregateTimingDocumentationTests.cs`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/tests/test-summary.md` if evidence changes

## Tasks & Acceptance

**Execution:**
- [x] Verify `publishingScopes` / `subscriptionScopes` behavior against DAPR topic-scoping docs and the intended EventStore publisher / sample subscriber contract.
- [x] Correct or remove `publishingScopes: "sample="` if it is inert, misleading, or inverted.
- [x] Compare local AppHost and production pub/sub component YAML and document any intentional difference.
- [x] Update `docs/cross-aggregate-timing.md` so the subscriber-failure branch does not imply DAPR component dead-lettering to `deadletter.tenants.events`.
- [x] Update `CrossAggregateTimingDocumentationTests` to assert the truthful application-level dead-letter wording and the topic-scope contract.
- [x] Keep the stale EventStore Admin actor-routing entry closed based on current source verification.
- [x] Normalize `deferred-work.md` so it remains a routing index, not a contradictory review-history dump.

**Acceptance Criteria:**
1. Given `deploy/dapr/pubsub.yaml` says EventStore publishes and sample subscribes, when topic scopes are configured, then `publishingScopes` and `subscriptionScopes` match that intent or are omitted with a documented reason. The current suspicious `publishingScopes: "sample="` must be verified against DAPR topic scoping and corrected if inert or misleading.
2. Given local and production DAPR pub/sub components are compared, when topic-scope policy differs, then the difference is intentional and documented.
3. Given `docs/cross-aggregate-timing.md` shows the propagation sequence, when subscriber failure is diagrammed, then it does not imply DAPR component dead-lettering to `deadletter.tenants.events`. The diagram should distinguish subscriber redelivery from EventStore's application-level dead-letter publisher.
4. Given `CrossAggregateTimingDocumentationTests` guards the guide, when docs/YAML change, then tests assert the truthful application-level dead-letter wording and topic-scope contract.
5. Given `deferred-work.md` still says EventStore Admin routes tenant queries through `TenantProjectionRouting.ActorTypeName`, when current EventStore source no longer does that, then the entry is marked stale/resolved with the verification command and date instead of being carried as open work.
6. Given `deferred-work.md` has contradictory June 18 review-record wording, when the cleanup runs, then entries are normalized to a current, non-contradictory status with source artifact references.

## Verification

- `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Release --no-restore --filter CrossAggregateTimingDocumentationTests`
- `git diff --check`

## Reference

- DAPR pub/sub topic scoping: `https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-scopes/`

## Dev Agent Record

### Debug Log

- Resolved `bmad-dev-story` workflow customization: no activation prepend/append steps; persistent project context loaded.
- Verified DAPR v1.17 and latest v1.18 topic-scoping docs: `publishingScopes` and `subscriptionScopes` are semicolon-separated app-to-topic mappings; an empty topic list denies that app, while omitted apps retain broad default access unless otherwise constrained.
- Confirmed current stale EventStore Admin routing claim remains false with `rg -n "ProjectionActorType|TenantProjectionRouting|TenantsProjectionActor" Hexalith.EventStore/src/Hexalith.EventStore.Admin.Server/Services/DaprTenantQueryService.cs` returning no matches.
- Red phase: added DAPR topic-scope/dead-letter documentation assertions, then confirmed the focused documentation test failed against `publishingScopes: "sample="`.
- Green/refactor phase: corrected production topic scopes, documented local-vs-production scope differences, removed DAPR subscriber-failure-to-dead-letter implication, updated stale configuration guards, and normalized routing/evidence records.

### Completion Notes

- Production `deploy/dapr/pubsub.yaml` now explicitly allows `eventstore` to publish `tenants.events` and `deadletter.tenants.events`, denies `sample` publishing, and allows `sample` to subscribe to `tenants.events`.
- Local AppHost pub/sub intentionally omits topic-level scopes while retaining component scopes; the difference is documented in local YAML, production docs, and the cross-aggregate timing guide.
- `docs/cross-aggregate-timing.md` now separates subscriber redelivery on `tenants.events` from EventStore's application-level dead-letter publisher for `deadletter.tenants.events`.
- Server documentation/configuration tests now guard against reintroducing inert DAPR component dead-letter metadata and assert the truthful topic-scope contract.
- `deferred-work.md` remains a routing index with the EventStore Admin actor-routing item stale/resolved and re-verified on 2026-06-20.

### File List

- `_bmad-output/implementation-artifacts/cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `deploy/dapr/README.md`
- `deploy/dapr/pubsub.yaml`
- `docs/cross-aggregate-timing.md`
- `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml`
- `tests/Hexalith.Tenants.Server.Tests/Configuration/EventPublicationConfigurationTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Documentation/CrossAggregateTimingDocumentationTests.cs`

### Change Log

- 2026-06-20: Implemented DAPR deployment docs and deferred record cleanup; added/updated tests and evidence.

### Validation

- `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Release --no-restore --filter CrossAggregateTimingDocumentationTests` passed 7/7.
- `dotnet test tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Release --no-restore` passed 106/106.
- `dotnet test tests/Hexalith.Tenants.Client.Tests/Hexalith.Tenants.Client.Tests.csproj -c Release --no-restore` passed 47/47.
- `dotnet test tests/Hexalith.Tenants.Testing.Tests/Hexalith.Tenants.Testing.Tests.csproj -c Release --no-restore` passed 181/181.
- `dotnet test samples/Hexalith.Tenants.Sample.Tests/Hexalith.Tenants.Sample.Tests.csproj -c Release --no-restore` passed 32/32.
- `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Release --no-restore` passed 700/700.
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore` passed 731/731.
- `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj -c Release --no-restore` passed 204, skipped 33.
- `git diff --check` passed with an existing line-ending warning for `_bmad-output/implementation-artifacts/sprint-status.yaml`.
