---
title: 'DAPR deployment docs and deferred record cleanup'
type: 'correct-course-hardening'
created: '2026-06-19'
status: 'ready-for-dev'
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
- [ ] Verify `publishingScopes` / `subscriptionScopes` behavior against DAPR topic-scoping docs and the intended EventStore publisher / sample subscriber contract.
- [ ] Correct or remove `publishingScopes: "sample="` if it is inert, misleading, or inverted.
- [ ] Compare local AppHost and production pub/sub component YAML and document any intentional difference.
- [ ] Update `docs/cross-aggregate-timing.md` so the subscriber-failure branch does not imply DAPR component dead-lettering to `deadletter.tenants.events`.
- [ ] Update `CrossAggregateTimingDocumentationTests` to assert the truthful application-level dead-letter wording and the topic-scope contract.
- [ ] Keep the stale EventStore Admin actor-routing entry closed based on current source verification.
- [ ] Normalize `deferred-work.md` so it remains a routing index, not a contradictory review-history dump.

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

Pending implementation.
