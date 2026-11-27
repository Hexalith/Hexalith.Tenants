---
title: 'Validate durable tenant history before exposing access state'
type: 'bugfix'
created: '2026-11-27'
status: 'done'
route: 'oneshot'
review_loop_iteration: 1
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

DW-684 and DW-683 protect tenant access state during durable history recovery. Given an authoritative range with an unsupported event or a payload whose `TenantId` differs from the stream aggregate, recovery must keep the checkpoint at the last trusted position and withhold access state. Given a valid supported event, including known checkpoint-only rejections and `TenantAccessRefreshed`, recovery must preserve existing projection semantics.

</frozen-after-approval>

## Implementation Notes

- `TenantProjectionCheckpointTransition` pre-resolves all supported tenant projected and rejection contracts with their readable `string TenantId` property and declared payload version (`EventPayloadVersionResolver.GetDeclaredVersion`). Both live (`DecodedEvent`) and replayed (`Payload`) events validate contract membership and ordinal `TenantId == Stream.AggregateId` before folding; replayed events require explicit `"json"` serialization format and matching declared payload version, failing with `NotSupportedException` / `InvalidDataException` so the SDK keeps the checkpoint gapped and releases the recovery lease.
- `DurableTenantProjectionStoreTests` and `TenantProjectionTransitionTests` exercise SDK recovery over the gateway range contract for valid `TenantAccessRefreshed` and post-creation rejections inside gaps, foreign/missing/malformed payloads, excluded `GlobalAdministrator*` events, unsupported contracts/metadata, direct `ITenantProjectionSnapshotStore.ApplyAsync` mismatches, and restarted fail-closed reads.
- `docs/idempotent-event-processing.md` documents replay contract/identity rules, live-vs-replay behavior, pre-creation checkpoint-only semantics, and coordinated rebuild steps into an isolated projection store when prior permissive replay is suspected.

## Review Triage Log

- **F1 (`medium`, patched)** — `TenantProjectionCheckpointTransition.Apply` previously bypassed contract allowlist and ordinal `TenantId` validation for live `DecodedEvent` deliveries, reachable via direct `ITenantProjectionSnapshotStore.ApplyAsync` or custom `AddHexalithTenants(configureOptions)` overriding `PayloadAggregateIdPropertyName`. Patched to enforce contract allowlist, exact type match, and ordinal `TenantId` validation for both live and replayed events, with direct `ApplyAsync` test coverage.
- **F2 (`low`, patched)** — Non-null `DecodedEvent` not implementing `IEventPayload` silently fell through `as IEventPayload ?? Decode(@event)` into a misleading decode error. Patched with an explicit `@event.DecodedEvent is not null` branch throwing `NotSupportedException`.
- **F3 (`medium`, patched)** — `SerializationFormat ?? "json"` accepted `null` format as JSON, diverging from `EventStoreDomainEventProcessor.IsSupportedSerializationFormat` and `TenantOwnerAuthorityQueryHandler.TryFold`. Patched to require explicit `"json"` (case-insensitive), updated `TenantProjectionTransitionTests`, and added null/blank format tests (noting `StreamReadPageValidator` rejects null/blank formats on gateway reads as `HistoryUnavailable`, while `TenantProjectionCheckpointTransition` rejects `null` directly).
- **F4 (`low`, patched)** — Hardcoded `@event.PayloadVersion is < 1 or > 1` instead of `EventPayloadVersionResolver.GetDeclaredVersion(type)`. Patched to compare `(@event.PayloadVersion ?? 1)` against each contract's pre-resolved declared version.
- **F5 (`medium`, patched)** — Uncached `type.GetProperty("TenantId")` without `BindingFlags`/readability/type/indexer guards ran per replayed event and threw `JsonException` on identity mismatch. Patched to pre-resolve and validate the public readable `string TenantId` property once in `s_historyTypes` and throw `InvalidDataException` on mismatch.
- **F6 (`false`, rejected)** — Claimed `TenantProjectionCheckpointTransition.Apply` could fold a `global-administrators` stream because it does not check `@event.Stream.Domain`. Disproved: `TenantProjectionCheckpointTransition` is `internal sealed` and its sole production caller `DurableTenantProjectionStore.ApplyAsync` (`DurableTenantProjectionStore.cs:161`) returns `CheckpointedProjectionOutcome.Duplicate` for any `context.Domain != TenantIdentity.Domain` before invoking `_writer.ApplyAsync`, and recovery reads use the exact stream recorded from that live delivery.
- **F7 (`low`, patched)** — `NotSupportedException` messages omitted offending `EventType`, `SerializationFormat`, and `PayloadVersion`. Patched to include contract name, format, and version in exception messages.
- **F8 (`high`, patched)** — `TenantAccessRefreshed` was only delivered live in `DurableTenantProjectionStoreTests`, never decoded from authoritative history inside a gap despite the spec requirement. Patched by adding `HistoricalRefreshInsideAGapRecoversWithoutAlteringBusinessState` and `HistoricalRefreshWithForeignTenantIdentityKeepsTheRecordGapped`.
- **F9 (`medium`, patched)** — Missing recovery tests for post-creation rejection in a gap, full `ProjectedEventTypes`/`RejectionEventTypes` decode round-trip, excluded `GlobalAdministrator*` events in tenant history, and malformed/empty/null JSON payloads. Patched with dedicated tests in `DurableTenantProjectionStoreTests` and `TenantProjectionTransitionTests`.
- **F10 (`low`, patched)** — `HistoricalRejectionMustNameItsStreamTenantEvenBeforeCreation` and `VerifyBlockedHistoryAsync` omitted `Gateway.Reads`, `Expected`, `RecoveryThrough`, `CorruptionReason`, `Pending`, restarted read, and retry assertions. Patched to assert all persisted recovery state, restarted fail-closed reads, and retryable `TransitionFailed` behavior.
- **F11 (`low`, patched)** — `TenantProjectionTransitionTests` omitted `SerializationFormat = "json"` on `rejection` and lacked direct transition failure-path tests. Patched to set `SerializationFormat = "json"` and add direct round-trip and failure-path assertions.
- **F12 (`low`, patched)** — `docs/idempotent-event-processing.md` omitted coordinated rebuild steps, pre-creation checkpoint-only clarification, and live-vs-replay handling distinction. Patched paragraph in `docs/idempotent-event-processing.md`.
