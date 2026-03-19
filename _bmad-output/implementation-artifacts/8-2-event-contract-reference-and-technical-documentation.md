# Story 8.2: Event Contract Reference & Technical Documentation

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer integrating tenant events into a consuming service,
I want comprehensive documentation on event contracts, cross-aggregate timing, and compensating commands,
So that I can design my integration correctly and handle edge cases with confidence.

## Acceptance Criteria

1. **Given** `docs/event-contract-reference.md` exists
   **When** a developer reads the document
   **Then** it documents all 12 commands and 11 events with their full schemas (field names, types, descriptions), organized by aggregate (TenantAggregate, GlobalAdministratorAggregate)

2. **Given** the event contract reference
   **When** a developer looks up a specific event (e.g., UserAddedToTenant)
   **Then** the documentation includes: event name, all fields with types, a JSON example, the command that produces it, and the topic it is published on

3. **Given** `docs/cross-aggregate-timing.md` exists
   **When** a developer reads the document
   **Then** it includes: timing window explanation between tenant commands and subscriber processing, a sequence diagram showing the event propagation flow, guidance on designing for eventual consistency, and a reference to the planned Phase 2 auth plugin as the synchronous enforcement option

4. **Given** `docs/compensating-commands.md` exists
   **When** a developer reads the document
   **Then** it includes: compensating command definition, a worked example showing AddUserToTenant after an incorrect RemoveUserFromTenant, and an explanation of why the role must be explicitly specified (not auto-restored from previous state)

## Tasks / Subtasks

- [x] Task 0: PREREQUISITE — Verify enum serialization format (MUST complete before writing ANY JSON examples)
  - [x] 0.1: **CRITICAL GATE**: Grep for `JsonStringEnumConverter` in EventStore submodule and CommandApi source to determine how `TenantRole` and `TenantStatus` enums serialize in event payloads. System.Text.Json serializes enums as **integers by default** unless `JsonStringEnumConverter` is configured. If string: use `"role": "TenantContributor"` in all JSON examples. If integer: use `"role": 1`. This determination affects EVERY JSON example in the entire document. Do NOT proceed to Task 3+ until this is resolved.

- [x] Task 1: Create `docs/event-contract-reference.md` — Overview and conventions (AC: #1)
  - [x] 1.1: Write document header with purpose statement: comprehensive reference for all tenant domain commands, events, and rejection events
  - [x] 1.2: Add a table of contents with anchor links to each major section (Enums, TenantAggregate, GlobalAdministratorAggregate, Rejections, Quick Reference, Idempotency). This doc will be long — developers land via search and need to jump to a specific event fast
  - [x] 1.3: Document the event delivery model: all events published via DAPR pub/sub as CloudEvents 1.0 on topic `system.tenants.events`; consumers filter by event type. Also mention the dead letter topic `deadletter.tenants.events` — events that fail subscriber processing after retries are routed here; operators should monitor it for delivery failures. Note: commands are submitted via the CommandApi — link to [Quickstart Guide](quickstart.md) for command submission details; do NOT add curl examples for every command in this doc
  - [x] 1.4: Document the identity scheme: platform tenant = `system`, domain = `tenants`, aggregateId = managed tenant ID or `global-administrators`
  - [x] 1.5: Document the three-outcome model: Success (events produced), Rejection (rejection events produced), NoOp (idempotent, no events)
  - [x] 1.6: Document event envelope metadata: all events wrapped in EventStore's event envelope with `eventId`, `aggregateVersion`, `timestamp`, `correlationId`, `causationId`, `userId` — link to EventStore event envelope docs at `Hexalith.EventStore/docs/concepts/event-envelope.md` for full envelope schema
  - [x] 1.7: Add contract stability notice: pre-v1.0 schemas may change; post-v1.0 only additive changes (new fields with defaults). Include:
    - A concrete example of a backward-compatible change: e.g., "In v1.1, a new optional field `Tags` is added to `TenantCreated`. Existing subscribers continue working because System.Text.Json ignores unknown properties by default (`JsonSerializerOptions.UnmappedMemberHandling` defaults to `Skip`)."
    - **Forward-compatible enum handling guidance**: Subscribers should handle unknown `TenantRole` values gracefully (log a warning, treat as lowest-permission `TenantReader` or skip) rather than throwing. This is important because Phase 2 may add custom/extensible roles beyond the current `TenantOwner`/`TenantContributor`/`TenantReader` set

- [x] Task 2: Create `docs/event-contract-reference.md` — Enums section (AC: #1)
  - [x] 2.1: Document `TenantRole` enum: `TenantOwner`, `TenantContributor`, `TenantReader`
  - [x] 2.2: Document `TenantStatus` enum: `Active`, `Disabled`

- [x] Task 3: Create `docs/event-contract-reference.md` — TenantAggregate section (AC: #1, #2)
  - [x] 3.1: Document each command-event pair for tenant lifecycle:
    - `CreateTenant(TenantId, Name, Description?)` → `TenantCreated(TenantId, Name, Description?, CreatedAt)` | Rejections: `TenantAlreadyExistsRejection`
    - `UpdateTenant(TenantId, Name, Description?)` → `TenantUpdated(TenantId, Name, Description?)` | Rejections: `TenantNotFoundRejection`, `TenantDisabledRejection`
    - `DisableTenant(TenantId)` → `TenantDisabled(TenantId, DisabledAt)` | Rejections: `TenantNotFoundRejection` | NoOp if already disabled
    - `EnableTenant(TenantId)` → `TenantEnabled(TenantId, EnabledAt)` | Rejections: `TenantNotFoundRejection` | NoOp if already active
  - [x] 3.2: Document each command-event pair for user-role management:
    - `AddUserToTenant(TenantId, UserId, Role)` → `UserAddedToTenant(TenantId, UserId, Role)` | Rejections: `TenantNotFoundRejection`, `TenantDisabledRejection`, `UserAlreadyInTenantRejection(TenantId, UserId, ExistingRole)`, `RoleEscalationRejection`, `InsufficientPermissionsRejection`
    - `RemoveUserFromTenant(TenantId, UserId)` → `UserRemovedFromTenant(TenantId, UserId)` | Rejections: `TenantNotFoundRejection`, `TenantDisabledRejection`, `UserNotInTenantRejection`, `InsufficientPermissionsRejection`
    - `ChangeUserRole(TenantId, UserId, NewRole)` → `UserRoleChanged(TenantId, UserId, OldRole, NewRole)` | Rejections: `TenantNotFoundRejection`, `TenantDisabledRejection`, `UserNotInTenantRejection`, `RoleEscalationRejection`, `InsufficientPermissionsRejection`
  - [x] 3.3: Document each command-event pair for tenant configuration:
    - `SetTenantConfiguration(TenantId, Key, Value)` → `TenantConfigurationSet(TenantId, Key, Value)` | Rejections: `TenantNotFoundRejection`, `TenantDisabledRejection`, `ConfigurationLimitExceededRejection(TenantId, LimitType, CurrentCount, MaxAllowed)`, `InsufficientPermissionsRejection`
    - `RemoveTenantConfiguration(TenantId, Key)` → `TenantConfigurationRemoved(TenantId, Key)` | Rejections: `TenantNotFoundRejection`, `TenantDisabledRejection`, `InsufficientPermissionsRejection`
    - Note for configuration events: Keys follow dot-delimited namespace convention (FR21), e.g., `billing.plan`, `parties.maxContacts`. Subscribing services should filter by key prefix to process only their own namespace (e.g., `key.startsWith("billing.")` for the Billing service). Include this guidance in the configuration command-event section
  - [x] 3.4: For EACH event, include a concise JSON example (3-5 lines per payload, not the envelope), using realistic field values (e.g., TenantId = "acme-corp", UserId = "jane-doe"). Use collapsible `<details>` tags for JSON examples if the document exceeds ~300 lines to keep it scannable
  - [x] 3.5: Use the enum serialization format determined in Task 0 for all JSON examples. Do NOT guess — Task 0 must be complete before this point
  - [x] 3.6: For EACH event entry, state: "Published on topic: `system.tenants.events`"

- [x] Task 4: Create `docs/event-contract-reference.md` — GlobalAdministratorAggregate section (AC: #1, #2)
  - [x] 4.1: Document each command-event pair:
    - `BootstrapGlobalAdmin(UserId)` → `GlobalAdministratorSet(TenantId, UserId)` | Rejections: `GlobalAdminAlreadyBootstrappedRejection` | Note: reuses same event type as SetGlobalAdministrator
    - `SetGlobalAdministrator(UserId)` → `GlobalAdministratorSet(TenantId, UserId)` | NoOp if user already admin
    - `RemoveGlobalAdministrator(UserId)` → `GlobalAdministratorRemoved(TenantId, UserId)` | Rejections: `LastGlobalAdministratorRejection(TenantId, UserId)` | NoOp if user not admin
  - [x] 4.2: Note that GlobalAdmin commands have NO TenantId in the command — the `TenantId` field in GlobalAdmin events is always `"system"` (the platform tenant context)
  - [x] 4.3: Note the aggregate uses `aggregateId: "global-administrators"` (singleton)
  - [x] 4.4: For EACH event, include a JSON example

- [x] Task 5: Create `docs/event-contract-reference.md` — Rejection events reference (AC: #1)
  - [x] 5.1: Create a consolidated rejection event table with all 10 rejection types. Use EXACTLY these corrective action texts:
    | Rejection | Fields | HTTP Status | Corrective Action |
    | `TenantAlreadyExistsRejection` | `TenantId` | 409 | Use a different tenant ID, or query the existing tenant |
    | `TenantNotFoundRejection` | `TenantId` | 404 | Ensure CreateTenant has been processed for this tenant ID |
    | `TenantDisabledRejection` | `TenantId` | 422 | Enable the tenant with EnableTenant before sending commands |
    | `GlobalAdminAlreadyBootstrappedRejection` | `TenantId` | 422 | Bootstrap already completed — proceed with normal operations |
    | `LastGlobalAdministratorRejection` | `TenantId, UserId` | 422 | Add another global administrator before removing the last one |
    | `UserAlreadyInTenantRejection` | `TenantId, UserId, ExistingRole` | 409 | User is already a member — use ChangeUserRole to modify their role |
    | `UserNotInTenantRejection` | `TenantId, UserId` | 422 | Add the user first with AddUserToTenant |
    | `RoleEscalationRejection` | `TenantId, UserId, AttemptedRole` | 403 | TenantOwner cannot assign GlobalAdministrator — use SetGlobalAdministrator instead |
    | `InsufficientPermissionsRejection` | `TenantId, ActorUserId, ActorRole?, CommandName` | 403 | The acting user needs TenantOwner or GlobalAdministrator role for this command |
    | `ConfigurationLimitExceededRejection` | `TenantId, LimitType, CurrentCount, MaxAllowed` | 422 | Remove existing configuration entries or reduce value size |
  - [x] 5.2: Note on `InsufficientPermissionsRejection`: the `ActorRole?` field is nullable — a null value indicates the actor is not a member of the tenant at all (vs. having an insufficient role). The corrective action should distinguish: null role = "user is not a member of this tenant, add them first"; non-null role = "user has {role} but needs TenantOwner or GlobalAdministrator"
  - [x] 5.3: Document the RFC 7807 Problem Details error response format with a JSON example (NOTE: status must match the rejection table — `TenantNotFoundRejection` maps to **404**, not 422):
    ```json
    {
      "type": "TenantNotFoundRejection",
      "title": "Tenant 'acme-test' does not exist.",
      "detail": "Ensure CreateTenant has been processed for this tenant ID.",
      "status": 404,
      "correlationId": "abc-123"
    }
    ```

- [x] Task 6: Create `docs/event-contract-reference.md` — Quick Reference table and idempotency guidance (AC: #1)
  - [x] 6.1: Add a "Quick Reference" summary table at the end of the event sections — a single condensed table for developers who already know the system and just need a lookup:
    | Command | Success Event | Possible Rejections |
    List all 12 commands, one row each. The "Possible Rejections" column must list **specific rejection type names** (e.g., `TenantNotFoundRejection, TenantDisabledRejection, InsufficientPermissionsRejection`) — not a generic "see rejections section." This table is the consumer developer's primary lookup tool
  - [x] 6.2: Document that all events include `eventId` and `aggregateVersion` in the envelope for consumer-side deduplication
  - [x] 6.3: Link to `docs/idempotent-event-processing.md` for detailed idempotent processing patterns — DO NOT duplicate that content
  - [x] 6.4: Note DAPR pub/sub at-least-once delivery guarantee and why deduplication matters

- [x] Task 7: Create `docs/cross-aggregate-timing.md` (AC: #3)
  - [x] 7.1: Write a timing window explanation: when a tenant command is processed (e.g., `RemoveUserFromTenant`), the event is stored atomically in the event store but delivered to subscribers asynchronously via DAPR pub/sub. Include a concrete timing estimate: "Under normal load, the propagation window is typically 50-200ms. Under pub/sub backpressure or network latency, it can extend to low seconds." There is a window where:
    - The Tenant aggregate has already applied the state change
    - Subscribing services have NOT yet received/processed the event
    - During this window, a subscribing service's local projection still reflects the old state
  - [x] 7.2: Create a Mermaid `sequenceDiagram` (NOT ASCII art — Mermaid renders natively on GitHub and is maintainable) showing the **simplified** consumer-facing event propagation flow. Do NOT show the internal 5-step actor pipeline. Required participants and interactions:
    - **Participants**: Client, CommandApi, Event Store, DAPR Pub/Sub, Service A, Service B
    - **Flow**: Client sends command → CommandApi stores events atomically in Event Store → Client gets 202 Accepted → Event Store publishes async to DAPR Pub/Sub → Pub/Sub delivers to Service A and Service B → each service updates its local projection
    - **Key visual**: The diagram must clearly show the synchronous (atomic store + response) vs asynchronous (pub/sub delivery) boundary — this IS the timing window
    - Craft the Mermaid syntax to fit the document's narrative flow; do not copy a template verbatim
  - [x] 7.3: Document guidance for designing for eventual consistency:
    - Subscribing services should treat tenant state as eventually consistent
    - **Event ordering nuance**: within a single aggregate instance, events are strictly ordered (aggregate version is monotonically increasing). Across different aggregates and across different subscribing services, there is NO ordering guarantee. Do NOT assume events arrive in the same order across different services
    - Design handlers to be idempotent (reference `docs/idempotent-event-processing.md`)
    - Use the query endpoint `GET /api/tenants/{id}` for read-after-write confirmation when needed
    - Command responses include the aggregate ID for direct navigation
  - [x] 7.4: Document the Phase 2 auth plugin as the synchronous enforcement option:
    - For security-critical scenarios where eventual consistency is insufficient
    - The planned EventStore authorization plugin will use a local projection of tenant-user-role state to reject unauthorized commands at the MediatR pipeline level, BEFORE they reach any domain service
    - This closes the timing window by providing synchronous enforcement
    - MVP approach: document the window, design for eventual consistency

- [x] Task 8: Create `docs/compensating-commands.md` (AC: #4)
  - [x] 8.1: Define compensating commands: commands that undo or correct a previous operation by issuing a new command that moves state to the desired outcome
  - [x] 8.2: Explain why event sourcing does NOT support "undo" — events are immutable facts. Corrections are new events that represent the corrective action
  - [x] 8.3: Write a worked example with the RemoveUserFromTenant mistake scenario. Include the **actual command payload JSON** (the `payload` object, not curl/HTTP) for each step so the example is copy-pasteable:
    1. Sofia (GlobalAdmin) removes "jdoe-contractor" from "acme-corp" tenant — `RemoveUserFromTenant` produces `UserRemovedFromTenant`. Show payload: `{ "tenantId": "acme-corp", "userId": "jdoe-contractor" }`
    2. Sofia realizes she removed the wrong user — she meant "jdoe-consulting"
    3. To compensate: Sofia issues `AddUserToTenant` — show payload: `{ "tenantId": "acme-corp", "userId": "jdoe-contractor", "role": "TenantContributor" }` — this produces `UserAddedToTenant` restoring the user
    4. Sofia then issues `RemoveUserFromTenant` for the correct user — show payload: `{ "tenantId": "acme-corp", "userId": "jdoe-consulting" }`
    IMPORTANT: Show the **full command body JSON** (with `tenant`, `domain`, `aggregateId`, `commandType`, and `payload` fields) matching the quickstart guide's `POST /api/v1/commands` format — NOT just the inner payload object. The anti-pattern "DO NOT add curl examples" means no `curl -X POST ...` shell wrapper, but the JSON body itself is what developers need to copy. Verify format consistency with `docs/quickstart.md` examples
  - [x] 8.4: Explain WHY the role must be explicitly specified in the compensating `AddUserToTenant`:
    - The system does NOT auto-restore the previous role
    - The previous role information is in the event history (`UserRemovedFromTenant` does not carry role info; the original `UserAddedToTenant` or last `UserRoleChanged` does)
    - If the role changed between when the user was originally added and when they were removed, auto-restore could assign a stale role
    - The human (or calling service) must decide which role to assign based on current business context
    - The event history provides the information needed to make this decision, but the decision is explicit
  - [x] 8.5: Document the audit trail: the event stream preserves full auditability — the mistake, the correction, the timestamps, the actor who performed each action. This is an advantage of event sourcing over CRUD, where corrections overwrite state

- [x] Task 9: Validation
  - [x] 9.1: Verify all three docs files are well-formed markdown with language-annotated code blocks
  - [x] 9.2: Verify all event field names and types match the actual record definitions in `src/Hexalith.Tenants.Contracts/`
  - [x] 9.3: Verify JSON examples use valid JSON and realistic field values. Use ISO 8601 with timezone offset for `DateTimeOffset` fields (e.g., `"2026-03-19T14:30:00+00:00"`, not `"2026-03-19T00:00:00Z"` which looks like a date)
  - [x] 9.4: Verify links between docs (cross-references to `idempotent-event-processing.md`, `quickstart.md`)
  - [x] 9.5: Cross-doc consistency check: verify that any command payload JSON in `compensating-commands.md` uses the same format structure as `quickstart.md` examples (same field names, same casing, same envelope structure)

## Dev Notes

### Architecture Context

This is a **documentation-only** story — no C# code changes, no tests to write. The deliverables are three markdown files in the `docs/` directory.

The documentation must accurately reflect the current implemented system state. All event/command record definitions must be verified against the actual source code in `src/Hexalith.Tenants.Contracts/`.

### What Already Exists (DO NOT Recreate)

| Component | Path | Relevance |
|-----------|------|-----------|
| All command records | `src/Hexalith.Tenants.Contracts/Commands/*.cs` | Source of truth for command schemas |
| All event records | `src/Hexalith.Tenants.Contracts/Events/*.cs` | Source of truth for event schemas |
| All rejection records | `src/Hexalith.Tenants.Contracts/Events/Rejections/*.cs` | Source of truth for rejection schemas |
| Enums | `src/Hexalith.Tenants.Contracts/Enums/` | TenantRole, TenantStatus definitions |
| Identity helpers | `src/Hexalith.Tenants.Contracts/Identity/TenantIdentity.cs` | Identity scheme constants |
| Idempotent processing doc | `docs/idempotent-event-processing.md` | Already written — link to it, do NOT duplicate |
| Quickstart guide | `docs/quickstart.md` | Already written in Story 8.1 (confirmed: exists in commit `5810018`) — link to it where appropriate |
| EventStore event envelope doc | `Hexalith.EventStore/docs/concepts/event-envelope.md` | Link to this for full envelope schema — do NOT reproduce envelope fields in detail |

### Exact Record Definitions (Verified from Source Code)

**Commands:**

| Command | Fields | Aggregate |
|---------|--------|-----------|
| `CreateTenant` | `string TenantId, string Name, string? Description` | TenantAggregate |
| `UpdateTenant` | `string TenantId, string Name, string? Description` | TenantAggregate |
| `DisableTenant` | `string TenantId` | TenantAggregate |
| `EnableTenant` | `string TenantId` | TenantAggregate |
| `AddUserToTenant` | `string TenantId, string UserId, TenantRole Role` | TenantAggregate |
| `RemoveUserFromTenant` | `string TenantId, string UserId` | TenantAggregate |
| `ChangeUserRole` | `string TenantId, string UserId, TenantRole NewRole` | TenantAggregate |
| `SetTenantConfiguration` | `string TenantId, string Key, string Value` | TenantAggregate |
| `RemoveTenantConfiguration` | `string TenantId, string Key` | TenantAggregate |
| `BootstrapGlobalAdmin` | `string UserId` | GlobalAdministratorAggregate |
| `SetGlobalAdministrator` | `string UserId` | GlobalAdministratorAggregate |
| `RemoveGlobalAdministrator` | `string UserId` | GlobalAdministratorAggregate |

**Events (all implement IEventPayload):**

| Event | Fields | Produced By |
|-------|--------|-------------|
| `TenantCreated` | `string TenantId, string Name, string? Description, DateTimeOffset CreatedAt` | CreateTenant |
| `TenantUpdated` | `string TenantId, string Name, string? Description` | UpdateTenant |
| `TenantDisabled` | `string TenantId, DateTimeOffset DisabledAt` | DisableTenant |
| `TenantEnabled` | `string TenantId, DateTimeOffset EnabledAt` | EnableTenant |
| `UserAddedToTenant` | `string TenantId, string UserId, TenantRole Role` | AddUserToTenant |
| `UserRemovedFromTenant` | `string TenantId, string UserId` | RemoveUserFromTenant |
| `UserRoleChanged` | `string TenantId, string UserId, TenantRole OldRole, TenantRole NewRole` | ChangeUserRole |
| `TenantConfigurationSet` | `string TenantId, string Key, string Value` | SetTenantConfiguration |
| `TenantConfigurationRemoved` | `string TenantId, string Key` | RemoveTenantConfiguration |
| `GlobalAdministratorSet` | `string TenantId, string UserId` | BootstrapGlobalAdmin, SetGlobalAdministrator |
| `GlobalAdministratorRemoved` | `string TenantId, string UserId` | RemoveGlobalAdministrator |

**Key differences between commands and events:**
- `TenantCreated` adds `DateTimeOffset CreatedAt` (generated by Handle method)
- `TenantDisabled` adds `DateTimeOffset DisabledAt`; `TenantEnabled` adds `DateTimeOffset EnabledAt`
- `UserRoleChanged` includes `OldRole` (from state) in addition to `NewRole` (from command)
- GlobalAdmin events include `TenantId` (always `"system"`) even though GlobalAdmin commands do NOT have `TenantId`

**Rejection Events (all implement IRejectionEvent):**

| Rejection | Fields |
|-----------|--------|
| `TenantAlreadyExistsRejection` | `string TenantId` |
| `TenantNotFoundRejection` | `string TenantId` |
| `TenantDisabledRejection` | `string TenantId` |
| `GlobalAdminAlreadyBootstrappedRejection` | `string TenantId` |
| `LastGlobalAdministratorRejection` | `string TenantId, string UserId` |
| `UserAlreadyInTenantRejection` | `string TenantId, string UserId, TenantRole ExistingRole` |
| `UserNotInTenantRejection` | `string TenantId, string UserId` |
| `RoleEscalationRejection` | `string TenantId, string UserId, TenantRole AttemptedRole` |
| `InsufficientPermissionsRejection` | `string TenantId, string ActorUserId, TenantRole? ActorRole, string CommandName` |
| `ConfigurationLimitExceededRejection` | `string TenantId, string LimitType, int CurrentCount, int MaxAllowed` |

### Critical Patterns to Follow

**DAPR Pub/Sub Topic**: All events published on `system.tenants.events`. The topic name follows EventStore's `NamingConventionEngine`: `{domain}.events` → `tenants.events`, prefixed with platform tenant `system` → `system.tenants.events`.

**CloudEvents 1.0 Envelope**: Events are wrapped in EventStore's event envelope which provides CloudEvents 1.0 compliance. The event contract reference should document the PAYLOAD fields (the domain-specific content), not the full envelope. Reference EventStore docs for the envelope schema.

**RFC 7807 Problem Details**: All domain rejections are mapped to HTTP error responses following RFC 7807. The `type` field uses the rejection event type name. HTTP status code mapping is defined in the architecture doc.

**JSON Serialization**: System.Text.Json with `camelCase` naming policy. All JSON examples must use camelCase field names (e.g., `tenantId`, `userId`, `createdAt`).

### Anti-Patterns to Avoid

- **DO NOT** duplicate content from `docs/idempotent-event-processing.md` — link to it
- **DO NOT** document internal implementation details (aggregate state classes, Handle method signatures) — this is a consumer-facing reference
- **DO NOT** invent field names — verify against the actual record definitions listed above
- **DO NOT** use PascalCase in JSON examples — System.Text.Json uses camelCase by default
- **DO NOT** document query endpoints — those are API documentation, not event contracts
- **DO NOT** reference Phase 2 features as if they exist now — clearly label the auth plugin as "planned"
- **DO NOT** add curl examples or command submission instructions for every command — link to the [Quickstart Guide](docs/quickstart.md) for command submission; this doc is about event contracts, not API usage

### Project Structure Notes

Files to create:
- **CREATE**: `docs/event-contract-reference.md` — comprehensive event contract documentation (FR61)
- **CREATE**: `docs/cross-aggregate-timing.md` — timing behavior documentation (FR64)
- **CREATE**: `docs/compensating-commands.md` — compensating command patterns (FR65)

Existing docs structure after this story:
```
docs/
  idempotent-event-processing.md  (exists — Epic 4)
  quickstart.md                   (exists — Story 8.1)
  event-contract-reference.md     (to create — this story)
  cross-aggregate-timing.md       (to create — this story)
  compensating-commands.md        (to create — this story)
```

### Technology Stack Reference

| Technology | Version | Relevance |
|-----------|---------|-----------|
| DAPR pub/sub | 1.17.3 | Event delivery mechanism — CloudEvents 1.0 |
| System.Text.Json | .NET 10 built-in | JSON serialization — camelCase by default |
| EventStore event envelope | (submodule) | CloudEvents 1.0 wrapper with metadata |

### Previous Story Intelligence

**Story 8.1 (Quickstart Guide & README)** was implemented in the most recent commit (`5810018`). Key learnings:
- The quickstart follows the EventStore quickstart pattern: Aspire AppHost launch → Keycloak JWT → Swagger UI command submission
- Command endpoint path uses `/api/v1/commands` (from EventStore's CommandsController)
- Bootstrap command must precede tenant commands
- Eventual consistency is documented with retry guidance for query verification
- The README and quickstart are already in place — this story's docs should cross-reference them where appropriate

### Git Intelligence

Recent commits show Story 8.1 was just completed (quickstart + README + integration tests). The codebase is stable with all Epics 1-7 complete and Epic 8 in progress. No code changes are needed for this documentation story.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 8.2] — Acceptance criteria
- [Source: _bmad-output/planning-artifacts/architecture.md#Implementation Patterns] — Handle method three-outcome model, naming conventions
- [Source: _bmad-output/planning-artifacts/architecture.md#Communication Patterns] — Event payload structure, versioning
- [Source: _bmad-output/planning-artifacts/architecture.md#Format Patterns] — RFC 7807, HTTP status codes, JSON conventions
- [Source: _bmad-output/planning-artifacts/architecture.md#Data Architecture] — Pub/sub topic, DAPR state store
- [Source: _bmad-output/planning-artifacts/prd.md#FR61] — Event contract reference requirement
- [Source: _bmad-output/planning-artifacts/prd.md#FR64] — Cross-aggregate timing documentation
- [Source: _bmad-output/planning-artifacts/prd.md#FR65] — Compensating command patterns
- [Source: _bmad-output/planning-artifacts/prd.md#Journey 4] — First error experience, timing window
- [Source: _bmad-output/planning-artifacts/prd.md#Journey 7] — Sofia's compensating command scenario
- [Source: src/Hexalith.Tenants.Contracts/] — All command, event, and rejection record definitions
- [Source: docs/idempotent-event-processing.md] — Existing idempotent processing docs (link, don't duplicate)

## Dev Agent Record

### Agent Model Used
Claude Opus 4.6 (1M context)

### Debug Log References
- Task 0: Verified enum serialization by grepping for `JsonStringEnumConverter` across all source and EventStore submodule. Found it ONLY in `TenantsProjectionActor.cs` for query response serialization (not event payloads). EventStore's `EventPersister.cs` uses `JsonSerializer.SerializeToUtf8Bytes(eventPayload, eventPayload.GetType())` with default options → enums serialize as integers. Confirmed by quickstart.md which uses `"Role": 1`.
- Task 9: Validated all field names against actual C# records (TenantCreated, UserRoleChanged, InsufficientPermissionsRejection, ConfigurationLimitExceededRejection, GlobalAdministratorSet, AddUserToTenant, RoleEscalationRejection, UserAlreadyInTenantRejection, TenantDisabled, ChangeUserRole). All match. Verified cross-doc links exist (quickstart.md, idempotent-event-processing.md, EventStore event-envelope.md). Verified compensating-commands.md command body format matches quickstart.md (same envelope structure: tenant, domain, aggregateId, commandType, payload with PascalCase inner fields).

### Completion Notes List
- Created `docs/event-contract-reference.md` (~620 lines) covering all 12 commands, 11 events, and 10 rejection events with full schemas, JSON examples, quick reference table, and idempotency guidance
- Created `docs/cross-aggregate-timing.md` with timing window explanation, Mermaid sequence diagram, eventual consistency guidance, and Phase 2 auth plugin reference
- Created `docs/compensating-commands.md` with full worked example (Sofia scenario), explicit role specification rationale, and audit trail documentation
- All JSON examples use integer enum values (confirmed via Task 0 gate)
- All DateTimeOffset fields use ISO 8601 with timezone offset format
- Used collapsible `<details>` tags for JSON examples in event-contract-reference.md (doc exceeds 300 lines)
- Cross-references between docs verified: links to quickstart.md, idempotent-event-processing.md, and EventStore event-envelope.md all target existing files

### File List
- **CREATED**: `docs/event-contract-reference.md` — Comprehensive event contract documentation (FR61)
- **CREATED**: `docs/cross-aggregate-timing.md` — Cross-aggregate timing and eventual consistency documentation (FR64)
- **CREATED**: `docs/compensating-commands.md` — Compensating command patterns documentation (FR65)

### Change Log
- 2026-03-19: Created three documentation files for Story 8.2 — event contract reference, cross-aggregate timing, and compensating commands. All acceptance criteria satisfied. Documentation-only story, no C# code changes.
