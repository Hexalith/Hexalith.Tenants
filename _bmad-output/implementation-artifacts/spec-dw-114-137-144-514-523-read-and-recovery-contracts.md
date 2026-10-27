---
title: 'Close five deferred read and recovery contract gaps'
type: 'bugfix'
created: '2026-10-26'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'cc0efc7df3399f1ea0e3789033ee75926bd06b35'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Complete DW-114, DW-137, DW-144, DW-514 and DW-523, then commit and push the reviewed changes to main. The requested five-item batch includes shared-module changes and their parent gitlinks.

Qualify Memories paging, reversible identifiers and Dapr reads under the recorded August decisions; align causation recovery and publication polling with existing contracts.

## Boundaries & Constraints

Preserve authorization, literal compatibility, opaque cursors, bounded reads and independent command/projection/audit truth. Shared plumbing belongs in its owning module. Commit shared changes before parent gitlinks; use owning pinned commitlint. Never migrate data, deploy, rewrite history, add silent read retries, widen the domain POST-only ACL or close unrelated records. Preserve historical findings and release holds.

## I/O & Edge-Case Matrix

| Input / state | Expected behavior |
| --- | --- |
| Indexed document missing a required field | Filter after raw paging; preserve raw total |
| Slash, backslash, dots, Unicode, percent text | Exact identifier; malformed encoding rejected before authorization |
| BFF GET with conditional headers | Preserve route and metadata; deny other callers/verbs |
| Null/empty/whitespace causation | Admission's MessageId fallback persists during resume |
| PublishFailed, Retryable true/false/null | Retry-After only for armed recovery; no implied success |

</frozen-after-approval>

## Open Questions

- **DW-137 downstream identity policy:** Current generated API sockets preserve ordinary encoded identifiers and reject malformed encoding, but EventStore's existing `SubmitQueryRequestValidator` and `AggregateIdentity` reject slash, backslash, dot-only and Unicode identifiers after decoding. Choose whether to extend the shared logical/physical identifier contract with collision-safe actor/key encoding, or preserve that restriction and replace DW-137 with another deferred item. An asynchronous user question is pending; do not broaden the shared policy or close DW-137 without the answer.

## Code Map

Paths are relative to owning Tenants, `references/Hexalith.EventStore` or `references/Hexalith.Memories`.

- Memories: `tests/Hexalith.Memories.IntegrationTests/Search/SyntacticSearchIntegrationTests.cs` supplies Redis fixtures; SyntacticSearchService filters after LIMIT; clarify SearchResult.TotalCount.
- EventStore: `src/Hexalith.EventStore.RestApi.Generators/RestApiControllerEmitter.cs` resolves tenant scope; reuse AddEventStoreDaprServiceInvocation.
- Tenants: `src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryClient.cs`, `src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs`, `src/Hexalith.Tenants.AppHost/Program.cs` and `deploy/dapr/` own BFF reads and topology.
- EventStore: `src/Hexalith.EventStore.Server/Actors/AggregateActor.cs` has inconsistent fallback; reuse CommandRecoveryIntegrityTests. `src/Hexalith.EventStore/Controllers/CommandStatusController.cs` ignores Retryable for polling.

## Tasks & Acceptance

- [ ] Memories search integration test above: prove offset-before-filtering and untrimmed totals against Redis; clarify the result contract.
- [ ] EventStore `src/Hexalith.EventStore.Contracts/Rest/RestRouteIdentifierCodec.cs` plus generator and BFF: add explicit canonical UTF-8/base64url-v1 route encoding before authorization; qualify malformed values, discriminator collisions and traversal.
- [ ] Tenants AppHost, BFF registration and `deploy/dapr/`: add the UI sidecar, API receiver ACL, correct HTTPS app channel and retry-free API resiliency target. Qualify all six routes and denied callers/verbs.
- [ ] EventStore AggregateActor above: align whitespace fallback with admission; prove persisted drain/index/reminder state and resumed exact-range completion.
- [ ] EventStore status controller above: make polling eligibility record-aware; align command API and brownfield lifecycle documentation; test true/false/null controls while retaining enum/replay compatibility.
- [ ] Tenants `_bmad-output/implementation-artifacts/deferred-work.md`: close exactly these five after review, maintain its index and record exact validation evidence.

Given an invalid indexed document, when paging advances by the requested size, then later valid matches remain reachable with raw totals.

Given literal/encoded identifiers, when reads traverse Dapr, then exact identity governs authorization without traversal or retries.

Given whitespace causation, when recovery resumes, then MessageId governs durable ownership and exact-range completion.

Given PublishFailed, when status is polled, then Retry-After agrees with recovery metadata without implying success.

## Implementation Notes

- User approved this five-item plan and continuation on 2026-10-26. Implementations and validation are retained in `dw-114-137-144-514-523-validation-evidence.md`; no selected ledger record has been closed yet, and independent review/commits/push remain pending.
- Local deny-default ACLs require authenticated peer identities. Reused Platform-owned Sentry/control-plane bootstrap and generated-sidecar binding rather than adding a separate identity implementation to Tenants. Platform baseline: `eecea3fa467d27226ef6173c19f853add36bb26c`. This necessary third shared-module prerequisite is included in the implementation footprint.
- The ordinary mTLS AppHost passes six real reads plus caller/verb/bearer denial controls. The current API also builds in a disposable source lane excluding only the unrelated, already-uncompilable Contracts/Routing directory; normal encoded reads and malformed-header cases pass against its sockets. Special IDs expose the shared backend restriction described in Open Questions. This is partial DW-137 evidence, not end-to-end special-ID acceptance.
- The four other selected implementations have passing relevant tests. Full EventStore Server: 5,871 passed/25 existing skips/zero failures; generator139/139; codec25/25; Memories Redis19/19; BFF129/129; focused composition6/6; AppHost/shared identity15/15. Broader source/package/provenance blockers remain explicit in the validation record.
Local deny-by-default Dapr policies require authenticated SPIFFE peer identities. The ordinary self-hosted AppHost previously supplied none, so its apparently healthy sidecars denied BFF reads. The implementation therefore also reuses and exposes the existing Platform-owned `DaprSelfHostedMtls.cs` bootstrap/binding source for Sentry, secure placement/scheduler and native generated sidecars; Tenants consumes that canonical source using Platform's established source-link pattern. This necessary third shared-module movement starts from Platform `eecea3fa467d27226ef6173c19f853add36bb26c` (clean `main`, root-declared submodule, owning guidance/configuration inspected). No nested dependencies are initialized.

## Spec Change Log

## Review Triage Log

## Design Notes

Use `X-Hexalith-Route-Encoding: base64url-v1`; absent header preserves literals. Reject unknown/duplicate discriminators, noncanonical encoding and invalid UTF-8. Encode the four parameterized reads; list/admin have no route identifiers.

Use dedicated local/production API receiver ACLs; domain accesscontrol.tenants.yaml stays POST-only. Preserve authoritative routing headers/deadlines; configure the HTTPS app channel.

DW-161 was already accepted against EventStore3.128.2 at this baseline. Preserve that record and historical evidence; controlled sidecar304 proof qualifies only the new transport.

Shared baselines: EventStore `935c21ac5ebc5e50738ae6ee79dc661acd2fd5a8`; Memories `cdd73d7361f67781a523f63f04853491a687f426`. The later verified downstream identifier-policy question is recorded above; no data migration has run.

## Verification

Run owning Debug/source projects: Memories IntegrationTests (focused Redis search); EventStore Contracts.Tests, RestApi.Generators.Tests and Server.Tests; Tenants UI.Tests, AppHost.Tests and focused authenticated route IntegrationTests. Use maintained MTP commands, recording exact results and blockers.

Use the Aspire skill to inspect/restart the AppHost around topology edits. Require real Dapr authorization checks, controlled conditional-response forwarding, meaningful rejection controls and independent review.

Declare each final gitlink movement and reason; run the story-gitlink validator against this spec. Validate owning commit messages, fetch and inspect divergence, then push the authorized changes to main.
