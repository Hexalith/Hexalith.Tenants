---
stepsCompleted: [1, 2, 3, 4, 5, 6]
inputDocuments: []
workflowType: 'research'
lastStep: 6
research_type: 'technical'
research_topic: 'Hexalith.FrontComposer to create the Tenants UX'
research_goals: 'Define the architecture, integration points, implementation approach, and practical UX composition strategy for a Tenants frontend built with FrontComposer.'
user_name: 'Jerome'
date: '2026-05-26'
web_research_enabled: true
source_verification: true
---

# Research Report: technical

**Date:** 2026-05-26
**Author:** Jerome
**Research Type:** technical

---

## Research Overview

This research evaluates how Hexalith.FrontComposer can be used as the UX composition layer for Hexalith.Tenants. It combines local repository evidence with current public documentation for FrontComposer-style composition, event-sourced UX patterns, .NET Aspire, Dapr, OpenTelemetry, Blazor testing, and Azure Well-Architected operational guidance.

The core finding is that FrontComposer is a strong fit when used as a generated composition layer over EventStore and Tenants contracts, not as a reason to reshape existing domain contracts. The recommended approach is an adapter module that owns UI-friendly command models, projection models, mappings, generated registration, and custom overrides for audit, destructive, and authorization-sensitive workflows.

The final Research Synthesis section consolidates the stack analysis, integration patterns, architectural design, implementation plan, risks, roadmap, and source verification into an implementation-oriented reference for future Tenants UX stories.

## Technical Research Scope Confirmation

**Research Topic:** Hexalith.FrontComposer to create the Tenants UX

**Research Goals:** Define the architecture, integration points, implementation approach, and practical UX composition strategy for a Tenants frontend built with FrontComposer.

**Technical Research Scope:**

- Architecture Analysis - design patterns, frameworks, system architecture
- Implementation Approaches - development methodologies, coding patterns
- Technology Stack - languages, frameworks, tools, platforms
- Integration Patterns - APIs, protocols, interoperability
- Performance Considerations - scalability, optimization, patterns

**Research Methodology:**

- Current web data with rigorous source verification
- Multi-source validation for critical technical claims
- Confidence level framework for uncertain information
- Comprehensive technical coverage with architecture-specific insights

**Scope Confirmed:** 2026-05-26

---

## Technology Stack Analysis

### Web Search Analysis

The external verification pass used primary or near-primary sources: Microsoft Learn for Blazor, SignalR, Aspire, and .NET release positioning; Dapr documentation for infrastructure building blocks; Fluent UI Blazor documentation and NuGet package data for component/runtime assumptions; the Fluxor repository and NuGet package page for state-management assumptions; and the Model Context Protocol documentation plus NuGet package data for MCP assumptions.

Confidence is high for the stack boundaries because the local repositories already pin the core package versions and the public documentation confirms the technology roles. Confidence is medium for Fluent UI Blazor v5 API details because the local repo pins a prerelease package, so component APIs should be treated as version-sensitive until the library reaches a stable v5 package.

### Programming Languages

The Tenants UX should stay in the same language family as the rest of the Hexalith platform: C# on .NET 10, with Razor/Blazor for UI components and Roslyn incremental generators for generated UI artifacts. This aligns with both local repository constraints and Microsoft's current Blazor model. Microsoft documents Blazor as a framework for building interactive web UI with .NET and C#, and its render-mode documentation supports static server rendering, interactive server rendering, interactive WebAssembly rendering, and Interactive Auto.

For Tenants, the primary language decision is not "which frontend language," but "which C# contract surface is safe to expose to FrontComposer." The existing Tenants command contracts are immutable positional records such as `CreateTenant(string TenantId, string Name, string? Description)`. Local FrontComposer source analysis shows that `[Command]` generation currently expects a public parameterless constructor, public writable setters for non-derivable fields, and a `MessageId` property for command correlation. Directly annotating the existing Tenants domain command records would therefore create generator diagnostics and likely force undesirable public contract changes.

The recommended language-level pattern is a C# adapter model layer:

- Keep Tenants domain contracts immutable and stable.
- Add FrontComposer-specific partial classes or mutable view models in a Tenants UX integration assembly.
- Annotate those UX models with FrontComposer attributes.
- Map UX models to the existing Tenants command/query contracts inside the FrontComposer command/query service adapters.

_Popular Languages:_ C# is the primary implementation language. Razor is the component authoring surface. TypeScript/JavaScript should remain limited to Playwright E2E tests and browser tooling, not application logic.

_Emerging Languages:_ No alternate application language is justified for this UX. The main "emerging" surface is Roslyn source-generation driven C# rather than hand-written UI code.

_Language Evolution:_ Blazor render modes make the C# UI model more flexible across prerender, server interactivity, WebAssembly interactivity, and Auto mode. FrontComposer components must continue to avoid direct browser-only APIs during prerender and use storage/service abstractions.

_Performance Characteristics:_ C# and Razor are appropriate for a dense operational UX. The high-risk performance work is not language execution speed; it is generated component size, query paging, projection cache invalidation, and avoiding excessive re-rendering when SignalR projection nudges arrive.

_Sources:_ Local: `Hexalith.FrontComposer/src/Hexalith.FrontComposer.SourceTools/Parsing/CommandParser.cs`, `src/Hexalith.Tenants.Contracts/Commands/CreateTenant.cs`. Web: https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes and https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview

### Development Frameworks and Libraries

FrontComposer is already designed around Blazor, Fluent UI Blazor, Fluxor, SignalR, and Roslyn source generation. The Shell project references `Fluxor.Blazor.Web`, `Microsoft.FluentUI.AspNetCore.Components`, `Microsoft.AspNetCore.SignalR.Client`, `Microsoft.AspNetCore.Authentication.OpenIdConnect`, `NUlid`, and `System.Reactive`. Its contracts include `ICommandService`, `IQueryService`, tenant-aware projection change notification, render contracts, projection templates, projection slots, command lifecycle tracking, and registry contracts.

For Tenants, FrontComposer should be used as a composition framework rather than a visual component library. The implementation should register a Tenants bounded context and expose UX artifacts for:

- Tenant list and tenant detail projections.
- Tenant membership and role management.
- Tenant configuration key/value management.
- Tenant audit timeline.
- Global administrator management.
- Command lifecycle and rejection display.

Fluent UI Blazor is the correct UI foundation because it is already the Shell dependency and the local FrontComposer product brief treats Fluent UI as the design-system authority. Fluxor is the correct client state model because FrontComposer's generator emits Fluxor actions, reducers, and features. SignalR should be used only as a nudge channel for projection changes; the authoritative data path should remain REST query re-fetches with tenant context and ETag/cache handling.

_Major Frameworks:_ Blazor for UI runtime, Fluent UI Blazor for components, Fluxor for state, SignalR for projection-change notifications, Roslyn incremental generators for generated artifacts, OpenID Connect for authentication integration.

_Micro-frameworks:_ System.Reactive appears in the Shell for bounded event streams such as badge counts. NUlid supports command correlation/message IDs. These should stay implementation details of FrontComposer adapters rather than leak into Tenants domain contracts unless already part of the domain contract.

_Evolution Trends:_ Blazor has moved toward explicit render modes and component interactivity placement. FrontComposer should keep generated components render-mode tolerant and avoid browser-only work during prerender.

_Ecosystem Maturity:_ Blazor, SignalR, and OpenID Connect are mature ASP.NET Core surfaces. Fluxor is community maintained but already pinned and integrated locally. Fluent UI Blazor v5 remains prerelease in this repository, so APIs should be wrapped through existing Shell patterns and updated deliberately.

_Sources:_ Local: `Hexalith.FrontComposer/Directory.Packages.props`, `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.csproj`, `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Contracts/Communication`. Web: https://learn.microsoft.com/en-us/aspnet/core/blazor, https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction, https://github.com/mrpmorris/Fluxor, https://www.nuget.org/packages/Fluxor.Blazor.Web/6.9.0, https://www.fluentui-blazor.net/

### Database and Storage Technologies

The Tenants UX should not introduce a direct database dependency. Tenants is event-sourced through Hexalith.EventStore and Dapr-backed infrastructure; FrontComposer should consume command and query APIs, not storage engines. The UX state stack should therefore be split into three levels:

- Durable domain state: EventStore aggregates, events, snapshots, and projections owned by the Tenants backend.
- Projection access state: REST query results, ETags, paging/filter/sort state, and tenant-scoped caches in FrontComposer Shell services.
- Ephemeral UX state: Fluxor state, command lifecycle state, navigation state, density/theme preferences, and local storage through FrontComposer abstractions.

Dapr documentation validates the infrastructure pattern: Dapr provides building blocks such as service invocation, state management, pub/sub, actors, secrets, configuration, workflow, and observability. This supports the existing Hexalith rule that FrontComposer and Tenants should not bind directly to Redis, Kafka, Cosmos DB, or another concrete infrastructure store.

For the first Tenants UX, the key storage work is a query adapter and cache policy, not a database design. Tenant lists, details, memberships, and audit rows should be loaded through Tenants query endpoints. Projection change notifications should invalidate or refresh matching tenant-scoped query cache entries.

_Relational Databases:_ Not part of the FrontComposer/Tenants UX contract. If a deployment uses SQL underneath Dapr or EventStore in the future, that remains backend infrastructure.

_NoSQL Databases:_ Redis or another Dapr state-store backend may exist in the local Aspire topology, but the UX should see only EventStore and Tenants APIs.

_In-Memory Databases:_ In-memory state is appropriate for Shell caches and test doubles, not for durable tenant state.

_Data Warehousing:_ Not relevant for the core Tenants UX. Audit projection export or analytics can be a later integration, not an initial FrontComposer concern.

_Sources:_ Local: `src/Hexalith.Tenants.Client/Projections`, `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/State/ETagCache`, project context Dapr/EventStore rules. Web: https://docs.dapr.io/concepts/building-blocks-concept/ and https://docs.dapr.io/developing-applications/building-blocks/state-management/

### Development Tools and Platforms

The development toolchain should remain the repo's existing .NET 10 toolchain with central package management, `.slnx` solutions, Roslyn generators, xUnit v3, Shouldly, bUnit, Playwright, and Aspire for local orchestration. FrontComposer source generation is a core development platform capability: it discovers `[Projection]`, `[Command]`, and template marker attributes, then emits Razor, Fluxor, registration, lifecycle bridge, command renderer/page, and MCP manifest artifacts.

The practical implementation model for Tenants is:

- Create a Tenants FrontComposer integration project or package, for example `Hexalith.Tenants.FrontComposer` or a host-owned UX assembly.
- Reference `Hexalith.Tenants.Contracts`, `Hexalith.FrontComposer.Contracts`, and the source generator package/project.
- Define UX command models and projection view models that satisfy generator constraints.
- Register generated domain manifests into the FrontComposer Shell registry.
- Implement `ICommandService` and `IQueryService` adapters against Tenants REST/EventStore submission semantics.
- Add bUnit coverage for generated/custom components and Playwright coverage for key tenant workflows.

This avoids modifying high-blast-radius Tenants contract records just to satisfy UI generation constraints.

_IDE and Editors:_ Visual Studio, Rider, or VS Code can work, but generated-source navigation and analyzer diagnostics are part of the development loop.

_Version Control:_ Git submodules are used for `Hexalith.FrontComposer`, `Hexalith.EventStore`, and related Hexalith modules. Only root-level submodules should be initialized or updated unless nested submodules are explicitly requested.

_Build Systems:_ Use `dotnet build`, `dotnet test`, central package management, and `.slnx`. Do not add inline package versions.

_Testing Frameworks:_ xUnit v3 and Shouldly for unit tests; bUnit for component tests; Playwright for E2E; accessibility checks should use the existing Playwright/axe conventions in FrontComposer.

_Sources:_ Local: `Hexalith.FrontComposer/src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs`, `Hexalith.FrontComposer/tests`, `Hexalith.FrontComposer/Directory.Packages.props`, project AGENTS instructions. Web: https://learn.microsoft.com/en-us/dotnet/core/tools/, https://learn.microsoft.com/en-us/aspnet/core/test/blazor, https://playwright.dev/dotnet/

### Cloud Infrastructure and Deployment

The Tenants UX should be hosted as part of the Hexalith distributed application model, with Aspire coordinating local development topology and Dapr abstracting runtime infrastructure. Microsoft documents .NET Aspire as a stack for building observable, production-ready distributed applications, with an AppHost project that defines and orchestrates app resources during development. This fits the existing Tenants AppHost and the FrontComposer product requirement for on-premise, sovereign cloud, and major-cloud portability.

The frontend deployment decision should remain conservative:

- Use Blazor Web App / Blazor Auto only if the host can support prerender, server circuits, and WASM handoff cleanly.
- Keep authentication host-owned through OpenID Connect/Keycloak or the configured provider.
- Keep Dapr sidecars and EventStore/Tenants services behind typed HTTP adapters.
- Use SignalR for live projection nudges, with fallback polling already represented in FrontComposer Shell state.

For production deployment, the UX should be packageable as a .NET web app/container and orchestrated alongside Tenants and EventStore resources. Aspire can model the topology; Dapr components keep state store, pub/sub, service invocation, and secret-store providers swappable.

_Major Cloud Providers:_ Azure, AWS, GCP, sovereign cloud, or on-premise can be supported if the runtime contract remains .NET containers plus Dapr components rather than provider-specific SDK calls inside the UX.

_Container Technologies:_ Existing project context favors .NET SDK container publishing and Aspire/Dapr orchestration. Kubernetes or Azure Container Apps are viable targets when the AppHost/deployment pipeline is configured for them.

_Serverless Platforms:_ Not a first fit for the interactive Tenants UX because Blazor Server/Auto and SignalR connections benefit from long-running web hosting. Backend event processing may still use event-driven infrastructure behind Dapr.

_CDN and Edge Computing:_ Useful for static assets and WebAssembly payloads if Blazor WebAssembly/Auto is used, but not a primary architectural driver.

_Sources:_ Local: `src/Hexalith.Tenants.AppHost`, `Hexalith.FrontComposer/_bmad-output/A-Product-Brief/platform-requirements.md`, project context Aspire/Dapr rules. Web: https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview, https://learn.microsoft.com/en-us/dotnet/aspire/app-host/overview, https://docs.dapr.io/operations/hosting/

### Technology Adoption Trends

The stack direction is coherent: .NET 10, Blazor render modes, source generation, Dapr building blocks, and Aspire orchestration all favor a C#-first distributed application with strongly typed contracts and local-to-cloud parity. That is exactly the niche FrontComposer is trying to occupy for Hexalith.EventStore services.

The main adoption risk is not technology mismatch; it is contract-shape mismatch between event-sourced domain contracts and generated UI view models. FrontComposer is optimized for generated forms and grids, while Tenants domain commands are optimized for immutable event-store command contracts. Treating the UI model as an adapter layer resolves that conflict without weakening either side.

MCP is also relevant because FrontComposer emits MCP metadata and has MCP runtime contracts. Model Context Protocol documentation positions MCP as a standard way for applications to expose context and tools to language models. For Tenants, MCP should be treated as a secondary surface for agent-driven operations and documentation/resource exposure, not the primary business-user UX.

_Migration Patterns:_ Move from hand-written CRUD screens to generated command/projection screens, but keep explicit custom components for workflows where generated UI would be unsafe or confusing, such as role escalation, last-owner warnings, global admin bootstrap visibility, and destructive tenant disable/remove actions.

_Emerging Technologies:_ Blazor Auto, source-generated UI, MCP metadata, and schema fingerprint/drift checks are the meaningful emerging pieces in this stack.

_Legacy Technology:_ Avoid CRUD scaffolding patterns that bypass command semantics. Avoid direct JavaScript SPA state stores unless a specific browser-only feature requires them.

_Community Trends:_ The broader .NET ecosystem supports Blazor, Aspire, Dapr, and SignalR as known building blocks, while FrontComposer's differentiator is local: event-sourcing aware generated UI from Hexalith domain contracts.

_Sources:_ Local: `Hexalith.FrontComposer/_bmad-output/A-Product-Brief/project-brief.md`, `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Contracts/Mcp`, `Hexalith.FrontComposer/src/Hexalith.FrontComposer.SourceTools/Transforms/SchemaFingerprintTransform.cs`. Web: https://modelcontextprotocol.io/introduction, https://www.nuget.org/packages/ModelContextProtocol.AspNetCore/1.3.0, https://learn.microsoft.com/en-us/dotnet/aspire/

### Step 2 Conclusion

Use FrontComposer for Tenants through a dedicated UX composition layer, not by reshaping Tenants domain contracts. The strongest technology path is:

- C#/.NET 10 and Blazor as the single application language/runtime.
- Fluent UI Blazor and FrontComposer Shell as the user-facing design system.
- Fluxor for generated state, command lifecycle, navigation, and projection page state.
- Tenants REST/EventStore APIs as the authoritative command/query boundary.
- SignalR projection notifications as invalidation nudges only.
- Dapr and Aspire for infrastructure abstraction and local distributed orchestration.
- Adapter models to bridge FrontComposer generator requirements to immutable Tenants contracts.

This preserves Hexalith's event-sourced domain model while giving Tenants a generated, consistent, accessible operational UX.

---

## Integration Patterns Analysis

### Web Search Analysis

Current external guidance supports the integration model already present in the Hexalith codebase:

- ASP.NET Core SignalR is the right fit for live projection change notifications because it provides real-time server-to-client messaging over WebSockets with fallbacks.
- ASP.NET Core JWT bearer authentication remains the right API security baseline; access tokens should come from OIDC/OAuth flows and APIs must validate issuer, audience, expiry, signature, and required claims.
- Dapr service invocation, pub/sub, state management, and actors are backend building blocks. They should connect EventStore, Tenants, projection handlers, and infrastructure components, but the browser-facing Tenants UX should not call Dapr sidecars directly.
- HTTP conditional request semantics, specifically ETag and `If-None-Match`, are appropriate for FrontComposer query caching and projection revalidation.
- ProblemDetails is the expected HTTP API error envelope for machine-readable UX error handling.
- CloudEvents remains the backend event envelope convention for pub/sub notifications and cross-service event delivery.

Source quality is high because the primary references are Microsoft Learn, Dapr documentation, RFC 9110, and the CloudEvents project site.

### API Design Patterns

The Tenants UX should use the EventStore gateway API as its primary command/query boundary, not the Tenants domain service endpoints directly.

FrontComposer already has an EventStore adapter with defaults that match the gateway:

- `CommandEndpointPath = "/api/v1/commands"`
- `QueryEndpointPath = "/api/v1/queries"`
- `ProjectionChangesHubPath = "/hubs/projection-changes"`

EventStore exposes authenticated command and query controllers at those routes. Tenants exposes a useful REST-friendly query controller under `/api/tenants`, but that controller is a thin translation layer over `SubmitQuery`; it is best treated as a specialized convenience API for hand-written screens, not the primary generated FrontComposer path.

Recommended API composition:

- Commands: generated or hand-written Tenants UX command model -> `ICommandService` -> FrontComposer `EventStoreCommandClient` -> `POST /api/v1/commands`.
- Queries: generated projection page/query adapter -> `IQueryService` -> FrontComposer `EventStoreQueryClient` -> `POST /api/v1/queries`.
- Projection updates: `IProjectionSubscription` -> SignalR hub `/hubs/projection-changes` -> refresh affected FrontComposer projection state.
- Domain-specific convenience reads: optional hand-written adapter -> Tenants `/api/tenants` GET endpoints when the UX needs the existing cursor or shaped response behavior.

This keeps generated UX infrastructure aligned with FrontComposer while preserving Tenants' existing command/query contracts and authorization pipeline.

### Communication Protocols

Use HTTPS JSON APIs for the browser-to-backend boundary. Use SignalR for real-time projection invalidation. Keep Dapr behind the backend boundary.

Primary protocols:

- REST over HTTPS for commands and queries.
- SignalR over WebSockets/fallback transports for projection change nudges.
- Dapr service invocation for EventStore-to-Tenants command processing through the Tenants `/process` endpoint.
- Dapr pub/sub and CloudEvents for backend projection/event distribution through the Tenants `/project` endpoint and subscription handler.
- Dapr actors/state for EventStore and Tenants projection/aggregate internals.

Do not add GraphQL for the initial Tenants UX. The existing EventStore query envelope already carries tenant, domain, projection, query type, aggregate/entity identity, ETag, paging, filters, and cache discriminators. GraphQL would duplicate those semantics and complicate the security model before it creates clear product value.

Do not add gRPC at the browser UX boundary. It may be useful later for internal service-to-service paths, but the current Blazor/FrontComposer shell already has HTTP JSON and SignalR clients. Browser-facing gRPC would add hosting and proxy complexity without improving the Tenants operational UX.

Webhooks are not needed for the in-app Tenants UX. SignalR handles interactive invalidation, and Dapr pub/sub handles backend event delivery.

### Data Formats and Standards

The integration format should remain JSON with explicit envelopes:

- Command submission uses an EventStore command envelope containing message ID, tenant, domain, aggregate ID, command type, and payload.
- Query submission uses an EventStore query envelope containing tenant, domain, projection type, query type, aggregate/entity identity, paging/filter data, and cache metadata.
- API errors should remain ProblemDetails-compatible so FrontComposer can map HTTP failures to validation, rejection, auth redirect, warning, or generic query/command failure states.
- Query caching should use ETags and `If-None-Match`, with `304 Not Modified` resolving from FrontComposer's cache when possible.
- Projection notifications should be invalidation nudges, not full read-model payloads. After a nudge, the UX should re-query through `IQueryService`.
- Backend pub/sub events should keep CloudEvents 1.0 as the interoperability envelope.

For Tenants query identifiers, use the existing contract constants:

- `list-tenants` with projection type `tenant-index`.
- `get-user-tenants` with projection type `tenant-index`.
- `get-tenant`, `get-tenant-users`, and `get-tenant-audit` with projection type `tenants`.

For Tenants aggregate targeting, keep domain identity in the contract layer (`tenants` for tenant aggregates and `global-administrators` for global administrator projection flow). The UX adapter should not invent new domain strings.

### System Interoperability Approaches

FrontComposer should be integrated as a host-level composition layer:

1. Add a Tenants UX composition assembly such as `Hexalith.Tenants.FrontComposer` or a host-owned equivalent.
2. Define FrontComposer-friendly command and projection models when generator constraints require mutable, partial, or UI-shaped types.
3. Map those UX models to immutable Tenants domain command contracts in an adapter.
4. Register FrontComposer generated manifests/components with the shell.
5. Register `AddHexalithEventStore` so `ICommandService`, `IQueryService`, and `IProjectionSubscription` resolve to the EventStore-backed implementations.
6. Configure `EventStoreOptions.BaseAddress` to the Aspire/EventStore service endpoint, with the default command/query/hub paths unless deployment requires a reverse proxy prefix.

The important interoperability rule is that FrontComposer composes UX from domain metadata, but it should not force the Tenants domain contracts to become UI models. The adapter boundary is where generated form models, typed query requests, domain command contracts, and gateway envelopes meet.

### Microservices Integration Patterns

The existing topology is a CQRS/event-sourced microservice pattern:

- EventStore is the gateway for command/query submission and projection notification.
- Tenants is the domain service that processes tenant commands and updates tenant projections.
- Dapr service invocation connects EventStore to Tenants command processing.
- Dapr pub/sub and projection dispatch connect events to projection handlers.
- Aspire coordinates local distributed app resources and endpoints.

For FrontComposer, that means:

- The browser-facing UX should target EventStore as the command/query/read-model gateway.
- Tenants domain service endpoints such as `/process` and `/project` should remain backend integration endpoints.
- Read model freshness should be managed through EventStore query responses, ETags, cache invalidation, and SignalR nudges.
- Cross-service workflow orchestration should remain backend-side. Do not implement sagas in the UX shell.

If future tenant workflows require multi-service orchestration, the saga/process-manager belongs in backend application services or Dapr Workflow, with the UX only submitting the initiating command and observing progress.

### Event-Driven Integration

Projection update handling should use a nudge-and-requery pattern:

1. The UX subscribes to projection groups by projection type and tenant ID.
2. EventStore SignalR authorizes the connection and validates tenant access before joining a group.
3. When projection changes occur, clients receive a change notification.
4. FrontComposer schedules refresh/pending command checks and re-queries the affected projection.

This avoids shipping sensitive or stale read-model payloads through push notifications. It also keeps EventStore query authorization as the authoritative read boundary.

Group naming and validation matter: EventStore uses `{projectionType}:{tenantId}` and rejects colon-containing projection or tenant values. The Tenants UX should treat projection type and tenant ID as validated identifiers, not arbitrary user input.

### Integration Security Patterns

The security model should be zero-trust at every integration boundary:

- Use OIDC/OAuth to obtain access tokens; do not mint application-specific JWTs in the UX.
- Validate JWT issuer, audience, signature, expiry, and required claims in APIs.
- Use the `sub` claim as the authenticated user identity, matching the EventStore and Tenants controllers.
- Keep tenant authorization and RBAC checks server-side before command/query processing and before SignalR group joins.
- Do not trust client-supplied command extensions for identity, tenant access, or global administrator privileges.
- Use ProblemDetails responses to drive precise UX outcomes without leaking internals.
- Do not log raw command/query payloads or personally identifiable tenant/user data from the UX integration layer.
- Keep Dapr access control deny-by-default for backend app-to-app calls.
- Use HTTPS for all browser-facing endpoints and secure token storage appropriate to the chosen Blazor hosting mode.

The generated UX should surface authorization and validation errors clearly, but it must not become an authorization decision point. FrontComposer can hide disabled actions based on metadata, but EventStore/Tenants must remain authoritative.

### Recommended Tenants UX Integration Flow

For a concrete Tenants screen, the flow should look like this:

1. The page resolves the active tenant context and projection type.
2. The page queries via `IQueryService.QueryAsync<TProjection>()`.
3. `EventStoreQueryClient` posts to `/api/v1/queries`, using `If-None-Match` when a cache entry exists.
4. EventStore validates the JWT subject, tenant access, and RBAC before dispatching the query.
5. Tenants projection actors return the read model payload.
6. The page renders a FrontComposer grid/detail/form component.
7. A command form dispatches through `ICommandService.DispatchAsync<TCommand>()`.
8. `EventStoreCommandClient` posts to `/api/v1/commands`, returning accepted/syncing lifecycle state.
9. EventStore invokes Tenants through Dapr service invocation at `/process`.
10. Projection updates are delivered backend-side and a SignalR nudge causes the page to re-query.

This flow matches FrontComposer's command lifecycle and Tenants' existing event-sourced backend design.

### Step 3 Conclusion

The integration pattern for Tenants UX should be: FrontComposer shell and generated UI -> EventStore gateway HTTP/SignalR adapters -> Dapr-backed EventStore/Tenants backend topology.

Use EventStore's generic `/api/v1/commands`, `/api/v1/queries`, and `/hubs/projection-changes` routes as the default integration surface. Use Tenants' `/api/tenants` GET endpoints selectively for specialized hand-written adapters. Keep Dapr, actors, pub/sub, and projection dispatch behind the server boundary. Keep authorization server-side and treat SignalR messages as invalidation nudges only.

_Sources:_ Local: `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreOptions.cs`, `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreCommandClient.cs`, `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs`, `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs`, `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Extensions/EventStoreServiceExtensions.cs`, `Hexalith.EventStore/src/Hexalith.EventStore/Controllers/CommandsController.cs`, `Hexalith.EventStore/src/Hexalith.EventStore/Controllers/QueriesController.cs`, `Hexalith.EventStore/src/Hexalith.EventStore/SignalRHub/ProjectionChangedHub.cs`, `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`, `src/Hexalith.Tenants/Program.cs`, `src/Hexalith.Tenants.AppHost/Program.cs`, `src/Hexalith.Tenants.Contracts/Queries`. Web: https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction, https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication, https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling-api, https://docs.dapr.io/developing-applications/building-blocks/service-invocation/, https://docs.dapr.io/developing-applications/building-blocks/pubsub/, https://www.rfc-editor.org/rfc/rfc9110, https://cloudevents.io/

---

## Architectural Patterns and Design

### Web Search Analysis

Current architecture sources validate the local Hexalith direction:

- Microsoft .NET microservices guidance positions DDD and CQRS as appropriate for complex bounded contexts with changing business rules, which matches tenant lifecycle, membership, role escalation, global administration, and audit requirements.
- Azure Architecture Center documents event sourcing as an append-only source-of-truth pattern that supports auditability, historical reconstruction, and materialized views, while warning that it adds schema, concurrency, query, and migration complexity. Tenants has already accepted those trade-offs through Hexalith.EventStore.
- Dapr documentation positions actors as stateful identity-based components that process messages one at a time, which aligns with EventStore aggregate serialization and Tenants invariant enforcement.
- Dapr building blocks validate the backend split between service invocation, state management, pub/sub, actors, configuration, and secrets, while keeping domain services infrastructure-independent.
- Azure Architecture Center cache guidance reinforces the selected ETag/cache-aside style: cached query data must have stale-data handling and invalidation/revalidation behavior.
- ASP.NET Core Blazor security guidance reinforces that authorization must be applied both to UI surfaces and server API/data access paths, especially when render modes can cross server and client execution.
- Aspire documentation supports AppHost-based, code-defined orchestration for frontends, APIs, containers, and backing services with observability and deployment portability.

Source quality is high because the web inputs are Microsoft Learn, Azure Architecture Center, Dapr documentation, and the Aspire documentation site. The local inputs are the Tenants architecture document, FrontComposer architecture document, EventStore architecture overview, and current source code.

### System Architecture Patterns

The right architecture for Tenants UX is a modular, generated Blazor shell composed over a CQRS/event-sourced backend.

Recommended system pattern:

- `Hexalith.Tenants.FrontComposer` or equivalent host-owned assembly provides Tenants-specific UI composition.
- `Hexalith.FrontComposer.SourceTools` generates components, Fluxor state, registration, command forms, and manifests from UX-facing command/projection models.
- `Hexalith.FrontComposer.Shell` provides the runtime shell: navigation, registry, rendering, lifecycle state, ETag cache, tenant context, authorization decoration, and EventStore HTTP/SignalR adapters.
- `Hexalith.EventStore` remains the gateway for command/query submission and projection notifications.
- `Hexalith.Tenants` remains the domain service for tenant aggregate behavior and projection/query logic.
- Dapr remains backend-only infrastructure for actors, service invocation, state, pub/sub, and configuration.
- Aspire remains the local and deployment topology model for EventStore, Tenants, admin services, Dapr components, and the eventual Tenants UX host.

This is not a CRUD admin application. The dominant backend pattern is CQRS + event sourcing with materialized projections. The dominant frontend pattern is generated operational UX with explicit lifecycle and cache invalidation.

Trade-offs:

- Benefit: the UX can be generated consistently while preserving Tenants domain invariants and EventStore submission semantics.
- Benefit: auditability and event replay remain first-class; read models can be shaped for UX needs without weakening the command model.
- Cost: generated UI models and domain contracts cannot be treated as the same type system. Adapter models are required.
- Cost: eventual consistency must be visible in UX lifecycle states, not hidden behind optimistic CRUD assumptions.
- Cost: source-generation diagnostics and generated baselines become part of architecture, not build decoration.

_Sources:_ Local architecture and source: `Hexalith.FrontComposer/_bmad-output/planning-artifacts/architecture.md`, `Hexalith.FrontComposer/src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs`, `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell`, `Hexalith.EventStore/docs/concepts/architecture-overview.md`, `src/Hexalith.Tenants`. Web: https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/, https://learn.microsoft.com/en-us/azure/architecture/patterns/event-sourcing

### Design Principles and Best Practices

Use a hexagonal boundary around Tenants UX composition:

- Inside: generated components, Fluxor state, UI command models, projection view models, validation metadata, and UX-specific affordances.
- Ports: `ICommandService`, `IQueryService`, `IProjectionSubscription`, `IFrontComposerRegistry`, tenant/user context abstractions, and storage/cache interfaces.
- Adapters: EventStore command/query clients, SignalR projection subscription, OIDC token access, and any specialized Tenants REST query adapter.
- Domain boundary: immutable Tenants contracts, query contracts, events, and aggregates remain owned by `Hexalith.Tenants.Contracts` and `Hexalith.Tenants.Server`.

The design rule is: generated UX types adapt to domain contracts; domain contracts do not adapt to generated UX constraints.

Concrete design decisions:

- Define Tenants FrontComposer command models that satisfy generator requirements such as `MessageId`, public construction, and UI-friendly setters.
- Map those models into existing Tenants command records before dispatch.
- Define projection view models that are `partial` and UI-shaped when the generator needs them.
- Map from Tenants query DTOs into FrontComposer projection rows if the existing DTO shape is not generator-friendly.
- Keep command lifecycle state in FrontComposer, but keep command truth in EventStore/Tenants.
- Keep authorization decisions server-side. Use FrontComposer policy metadata to shape UI affordances, not to grant access.
- Keep generated code deterministic and validated through diagnostics, source-generator tests, and registry startup validation.

This follows the local FrontComposer architecture: contracts are isolated, SourceTools has Roslyn/generator concerns, Shell owns runtime UX behavior, and EventStore integration is a removable adapter.

_Sources:_ Local: `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Contracts`, `Hexalith.FrontComposer/src/Hexalith.FrontComposer.SourceTools`, `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Extensions/EventStoreServiceExtensions.cs`, `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Registration/FrontComposerRegistry.cs`, `src/Hexalith.Tenants.Contracts`. Web: https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/apply-simplified-microservice-cqrs-ddd-patterns

### Scalability and Performance Patterns

The Tenants UX does not need independent frontend scaling tricks before it respects the backend consistency and projection architecture.

Primary scalability decisions:

- Scale EventStore and Tenants services horizontally where their current design permits; keep state in Dapr-backed stores and actors, not process memory.
- Use EventStore/Dapr actor serialization to protect per-aggregate invariants instead of adding client-side or API-level locks.
- Use materialized projections for list/detail/user/audit reads instead of replaying events during UX requests.
- Use ETags and `If-None-Match` through `EventStoreQueryClient` to reduce payload and rendering churn.
- Use SignalR projection notifications only as invalidation nudges; re-query through the gateway for authorized, current data.
- Keep FrontComposer grid state, ETag cache, navigation state, and preferences scoped by tenant/user/discriminator to avoid cross-tenant cache bleed.
- Use virtualization, paging, and cache eviction in grid-heavy views such as tenant list, user memberships, and audit timeline.

Performance constraints from the Tenants architecture remain relevant:

- Tenant list scale is expected around 1K tenants in the current architecture.
- Tenant aggregates can reach hundreds of users and configuration entries, so dashboard/detail read models should precompute counts needed by UX warnings.
- Snapshot intervals and projection caching matter more than client-side micro-optimizations for command and query latency.

Trade-offs:

- Local ETag cache improves perceived performance but must never cache sensitive or security-decision data without revalidation.
- SignalR improves freshness perception but introduces connection lifecycle handling; fallback polling remains required.
- Cross-tenant index projections are convenient for list/search UX, but shared write targets require explicit concurrency handling.

_Sources:_ Local: `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/State/DataGridNavigation`, `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs`, `_bmad-output/planning-artifacts/architecture.md`, `Hexalith.EventStore/docs/concepts/architecture-overview.md`. Web: https://learn.microsoft.com/en-us/azure/architecture/patterns/cache-aside, https://docs.dapr.io/developing-applications/building-blocks/state-management/state-management-overview/

### Integration and Communication Patterns

Use a layered communication architecture:

- UI layer: generated Blazor components and FrontComposer shell services.
- UX service layer: `ICommandService`, `IQueryService`, `IProjectionSubscription`, lifecycle services, tenant context, storage/cache.
- Gateway layer: EventStore REST APIs and SignalR hub.
- Backend integration layer: Dapr service invocation from EventStore to Tenants `/process`, Dapr pub/sub/projection dispatch to Tenants `/project`, Dapr actors/state for aggregates and projections.

Command path:

1. Generated command form dispatches a UX command model.
2. Adapter maps to Tenants command contract.
3. `EventStoreCommandClient` submits to `/api/v1/commands`.
4. EventStore validates, routes, and invokes Tenants through Dapr.
5. Tenants aggregate returns success, rejection, or no-op events.
6. FrontComposer lifecycle moves through submitting, acknowledged, syncing, confirmed, or rejected.

Query path:

1. Projection page dispatches a typed `QueryRequest`.
2. `EventStoreQueryClient` submits to `/api/v1/queries`, optionally with ETag.
3. EventStore validates tenant/RBAC before lookup.
4. Tenants projection actor/query handler returns DTO payload.
5. FrontComposer maps and renders projection state.

Notification path:

1. Projection updates trigger EventStore projection change notification.
2. SignalR hub broadcasts a tenant/projection nudge.
3. FrontComposer subscription service schedules refresh and pending command checks.
4. Projection page re-queries through `IQueryService`.

This communication model avoids direct coupling from the UX to Dapr, actors, state stores, or Tenants internals.

_Sources:_ Local: `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Contracts/Communication`, `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore`, `Hexalith.EventStore/src/Hexalith.EventStore/Controllers`, `Hexalith.EventStore/src/Hexalith.EventStore/SignalRHub`, `src/Hexalith.Tenants/Program.cs`. Web: https://docs.dapr.io/developing-applications/building-blocks/, https://docs.dapr.io/developing-applications/building-blocks/service-invocation/, https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/, https://docs.dapr.io/developing-applications/building-blocks/actors/actors-overview/

### Security Architecture Patterns

Security must be layered, with the server as the authority:

- Authentication: OIDC/OAuth sign-in at the UX host; JWT bearer tokens for EventStore/Tenants APIs.
- UI authorization: FrontComposer policy metadata and tenant context determine which commands and navigation entries are visible or disabled.
- Gateway authorization: EventStore validates token identity, tenant access, RBAC, command/query policy inputs, and SignalR group membership.
- Domain authorization: Tenants aggregates enforce business invariants such as owner-only actions, global-admin actions, role escalation boundaries, disabled-tenant rejection, and last-global-admin protection.
- Query-side filtering: Tenants query handlers enforce row-level result filtering for user membership search and audit-style scenarios.
- Infrastructure authorization: Dapr component access remains scoped; domain services should not bypass EventStore to state stores, pub/sub, or config stores.

For Blazor render modes, assume server-rendered and client-rendered code paths can differ. Any server-side service method used during rendering still needs server-side authorization. Do not rely on component-level visibility to protect data.

Security-specific FrontComposer patterns:

- Reject synthetic/demo tenant context in production.
- Revalidate tenant context before command, query, and subscription operations.
- Keep cache keys tenant/user scoped.
- Never log raw command/query/event payloads or PII.
- Treat SignalR messages as non-sensitive invalidation events.
- Surface ProblemDetails and rejection events as useful UX feedback without exposing internals.

_Sources:_ Local: `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Infrastructure/Tenancy`, `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Registration/FrontComposerCommandPolicyLookup.cs`, `Hexalith.EventStore/src/Hexalith.EventStore/Controllers`, `Hexalith.EventStore/src/Hexalith.EventStore/SignalRHub/ProjectionChangedHub.cs`, `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`, `_bmad-output/planning-artifacts/architecture.md`. Web: https://learn.microsoft.com/en-us/aspnet/core/blazor/security/?view=aspnetcore-10.0, https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication, https://learn.microsoft.com/en-us/azure/well-architected/security/tradeoffs

### Data Architecture Patterns

The Tenants UX should respect the existing split between write-side events and read-side projections.

Write side:

- Tenants aggregates remain the source for business invariants.
- Events remain immutable business facts.
- Rejections remain explicit domain outcomes rather than thrown business exceptions.
- EventStore owns envelope metadata, idempotency, persistence, publishing, and command status.

Read side:

- Tenants projection actors/read models shape data for tenant detail, tenant index, global admin, user memberships, and audit.
- FrontComposer projection models shape data again for rendering, sorting, filtering, badge display, timeline presentation, and consequence previews.
- ETags provide cache coherence between EventStore projection state and FrontComposer local state.
- Audit should remain projection-backed. Query-time event replay is not the right UX path for filtered timelines.

Mapping strategy:

- Domain command contracts -> immutable event-sourced API contracts.
- UX command models -> mutable/generated form contracts.
- Query DTOs -> API read contracts.
- FrontComposer projection rows -> UI read contracts.

Do not collapse these into one model. The extra mapping is deliberate architectural insulation.

_Sources:_ Local: `_bmad-output/planning-artifacts/architecture.md`, `src/Hexalith.Tenants.Server/Aggregates`, `src/Hexalith.Tenants.Server/Projections`, `src/Hexalith.Tenants.Contracts/Queries`, `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Contracts/Communication/QueryRequest.cs`, `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Contracts/Communication/QueryResult.cs`. Web: https://learn.microsoft.com/en-us/azure/architecture/patterns/event-sourcing, https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/

### Deployment and Operations Architecture

The deployment architecture should stay consistent with Hexalith's existing AppHost/Dapr model:

- Add the Tenants UX host as another Aspire resource beside EventStore and Tenants.
- Configure the UX to reference the EventStore endpoint as its command/query/hub base address.
- Keep EventStore SignalR enabled for projection changes.
- Keep Dapr components and sidecars backend-scoped; the UX should not require direct Dapr sidecar access.
- Publish the UX as a .NET web app/container.
- Use OpenTelemetry and structured logs consistently across UX, EventStore, and Tenants.
- Keep operational dashboards focused on command status, projection lag/freshness, SignalR connection health, query cache hit/not-modified rate, and auth failures.

Operational risks to track:

- Projection lag makes lifecycle confirmation feel unreliable.
- SignalR outage should degrade to polling without breaking command submission.
- ETag cache corruption should fail closed and re-fetch.
- Misconfigured tenant context or OIDC claims should block command/query/subscription operations with diagnosable failures.
- Generated contract drift should fail in build/test, not at runtime.

_Sources:_ Local: `src/Hexalith.Tenants.AppHost/Program.cs`, `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Extensions/EventStoreServiceExtensions.cs`, `Hexalith.EventStore/docs/concepts/architecture-overview.md`, `Hexalith.EventStore/src/Hexalith.EventStore/Program.cs`. Web: https://aspire.dev/, https://learn.microsoft.com/en-us/azure/well-architected/reliability/, https://learn.microsoft.com/en-us/azure/well-architected/operational-excellence/design-patterns

### Recommended Architecture Decision

Create the Tenants UX as a FrontComposer composition module with adapter models, not as direct annotations on existing Tenants contracts.

Recommended structure:

```text
src/
  Hexalith.Tenants.FrontComposer/
    Commands/              # FrontComposer-friendly command form models
    Projections/           # FrontComposer-friendly projection row/detail models
    Mapping/               # UX model <-> Tenants contract/query DTO mapping
    Registration/          # generated/static domain registration composition
    Components/            # custom overrides for high-risk workflows
    Security/              # policy constants and UX authorization metadata
```

Use generated components for standard list/detail/form workflows, but keep custom overrides for:

- tenant disable/enable confirmations,
- role escalation and global-admin workflows,
- last-owner/last-global-admin consequence previews,
- audit timeline display,
- user membership search and anomaly indicators,
- command rejection/ProblemDetails presentation where generated defaults would be too generic.

### Step 4 Conclusion

The architectural fit is strong if the UX is treated as a generated composition layer over EventStore, not as a rewrite of Tenants contracts. The core decision is to keep four boundaries explicit:

- FrontComposer UX models and generated shell behavior.
- EventStore gateway contracts and lifecycle semantics.
- Tenants domain contracts, aggregates, projections, and RBAC.
- Dapr/Aspire infrastructure topology.

This architecture preserves Hexalith's event-sourced backend while allowing a generated, consistent, tenant-aware operational UX.

---

## Implementation Approaches and Technology Adoption

### Technology Adoption Strategies

Tenants should adopt Hexalith.FrontComposer through an incremental adapter strategy. The correct adoption model is a strangler-style modernization path: introduce a FrontComposer composition module beside the current Tenants backend, route selected UX capabilities through it, and expand only when the adapter, testing, accessibility, and operations evidence is stable.

The recommended first module is `Hexalith.Tenants.FrontComposer`. It should be a separate source project that owns FrontComposer-friendly command models, projection rows, mapping code, registration metadata, and custom high-risk UI overrides. It should not mutate the existing immutable Tenants command contracts or sealed/read-only query DTOs to satisfy UI generation conventions.

Recommended adoption sequence:

1. Establish a technical spike for EventStore adapter wiring, tenant context propagation, authorization metadata, SignalR projection subscriptions, and generated component registration.
2. Implement read-only UX slices first: tenant list, tenant detail, tenant users, configuration display, and audit fallback where the current DataGrid/projection primitives are enough.
3. Add command workflows with clear adapter boundaries: create tenant, enable/disable tenant, add/remove/change users, set/remove global administrator, and set/remove configuration.
4. Introduce custom components for high-risk interactions rather than forcing generated defaults into workflows that need consequence preview, accessibility nuance, or complex command feedback.
5. Convert planning-only dependencies into implementation stories only when local evidence exists for audit timeline, consequence preview, command batching, localization, accessibility, and component documentation.

This approach keeps FrontComposer adoption reversible at screen or workflow granularity and avoids coupling the event-sourced domain model to UI generator constraints.

_Sources:_ Local: `docs/tenants-ui-frontcomposer-dependency-map.md`, `src/Hexalith.Tenants.Contracts/Commands`, `src/Hexalith.Tenants.Contracts/Queries`, `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Contracts`, `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell`. Web: https://learn.microsoft.com/en-us/azure/architecture/patterns/strangler-fig

### Development Workflows and Tooling

The implementation workflow should stay close to the existing repository conventions:

- Use the checked-in .NET SDK baseline from `global.json` (`10.0.300` with `latestPatch` roll-forward).
- Keep central package versioning in `Directory.Packages.props`.
- Add the FrontComposer UX project to the solution rather than embedding UI composition into the current backend host.
- Keep generated FrontComposer code and metadata deterministic enough for build-time validation and code review.
- Add adapter tests before adding generated UI flows, because most integration risk sits at contract shape boundaries.
- Keep CI tiering aligned with current workflow intent: restore, build, Tier 1 unit/contract tests, Tier 2 integration tests, then optional or gated E2E/accessibility lanes.

The repo already has the right foundation for this: central package management, Dapr, Aspire, OpenTelemetry packages, multiple test projects, and GitHub Actions workflows. The new implementation work should add a FrontComposer-focused test surface instead of broadening the backend tests until they become slow and noisy.

Two toolchain risks should be fixed early:

- The CI and release workflows initialize Dapr `1.16.0`, while central Dapr package versions are `1.17.9`; align runtime and package versions before trusting integration evidence.
- `src/Hexalith.Tenants/Hexalith.Tenants.csproj` references the EventStore web host project and carries a local TODO about `wwwroot` and configuration collisions; resolve or isolate that dependency before adding a real shell/UX host.

_Sources:_ Local: `global.json`, `Directory.Packages.props`, `.github/workflows/ci.yml`, `.github/workflows/release.yml`, `src/Hexalith.Tenants/Hexalith.Tenants.csproj`. Web: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test, https://docs.github.com/en/actions/automating-builds-and-tests/building-and-testing-net

### Testing and Quality Assurance

Testing should be layered around the actual failure modes of a generated tenant UX:

| Layer | Purpose | Recommended Evidence |
| --- | --- | --- |
| Adapter unit tests | Verify UX command/projection models map correctly to Tenants contracts and query DTOs. | xUnit, Shouldly, NSubstitute where useful. |
| Generator/manifest tests | Catch missing metadata, route drift, authorization metadata gaps, and generated component contract changes. | FrontComposer testing helpers and snapshot/approval tests where appropriate. |
| Component tests | Validate form behavior, table rendering, loading states, command feedback, localization, focus, and forced-colors behavior. | bUnit plus `Hexalith.FrontComposer.Testing`. |
| Contract/API tests | Verify command and query payloads sent to EventStore match Tenants backend expectations. | Server/integration tests with typed clients and ProblemDetails assertions. |
| Distributed integration tests | Verify Aspire/Dapr/EventStore/Tenants/UX topology, projection updates, ETags, and SignalR subscription behavior. | Aspire testing and Testcontainers where external dependencies are needed. |
| Browser E2E tests | Validate realistic operator flows and regression-critical UX states. | Playwright .NET or existing FrontComposer Playwright setup. |
| Accessibility tests | Verify keyboard navigation, focus order, live-region feedback, labels, contrast, reduced motion, and axe checks. | Playwright with axe plus manual checkpoints for high-risk flows. |

Current FrontComposer evidence is especially useful because it already contains adopter-facing testing infrastructure, bUnit usage, Playwright E2E tests, and axe accessibility helpers. Tenants should reuse those conventions rather than inventing a parallel QA model.

Quality gates for the first production-capable UX should include:

- build fails on generated metadata drift,
- adapter tests cover every command/query/projection model used by the UX,
- command rejection and validation errors are represented as accessible UI states,
- SignalR subscription loss degrades to polling or explicit refresh,
- ETag/not-modified behavior is tested for list/detail reads,
- destructive actions have consequence text, focus behavior, and localization evidence,
- audit screens meet bounded rendering expectations before large-result use.

_Sources:_ Local: `tests`, `Hexalith.FrontComposer/tests`, `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Testing`, `Hexalith.FrontComposer/tests/e2e`, `Hexalith.FrontComposer/docs/accessibility-verification/README.md`. Web: https://learn.microsoft.com/en-us/dotnet/aspire/testing/, https://playwright.dev/dotnet/, https://bunit.dev/

### Deployment and Operations Practices

The UX should run as an Aspire-managed .NET web resource beside the existing Tenants and EventStore resources. It should call EventStore command/query endpoints and subscribe to projection changes through the EventStore SignalR hub. It should not require direct Dapr sidecar access from the browser-facing UI layer.

Operational practices should focus on the user-visible reliability of event-sourced UX behavior:

- command dispatch success/failure rate,
- command pending duration,
- projection confirmation latency,
- SignalR connection and reconnect rate,
- query cache hit/not-modified rate,
- stale projection refresh count,
- authorization/tenant-context failures,
- command rejection ProblemDetails classification,
- page-level accessibility and localization regression status.

OpenTelemetry should be used consistently across UX, EventStore, Tenants, and the Aspire host so a UX command can be correlated with EventStore command handling, domain processing, projection update, SignalR notification, and final UI confirmation.

Release practice should require source-backed evidence for each FrontComposer dependency ID consumed by a Tenants UI story. Planning aliases such as `useCommand`, `<AuditTimeline>`, and `<ConsequencePreview>` should not be treated as implementation evidence unless a current source path or approved fallback exists.

_Sources:_ Local: `src/Hexalith.Tenants.AppHost/Program.cs`, `src/Hexalith.Tenants.ServiceDefaults`, `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Extensions/EventStoreServiceExtensions.cs`, `docs/tenants-ui-frontcomposer-dependency-map.md`. Web: https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-with-otel, https://learn.microsoft.com/en-us/azure/well-architected/operational-excellence/, https://learn.microsoft.com/en-us/azure/well-architected/reliability/

### Team Organization and Skills

The work needs a small cross-functional implementation team because the highest-risk decisions cross UI generation, event-sourced backend contracts, security, and accessibility:

- FrontComposer integrator: owns generated metadata, shell registration, adapter models, and component override strategy.
- Tenants domain engineer: owns contract mapping correctness, command/query semantics, authorization, and projection freshness assumptions.
- Test engineer or quality lead: owns adapter/component/integration/E2E strategy and CI gating.
- UX/accessibility reviewer: owns consequence preview, audit timeline, keyboard/focus behavior, localization readiness, and fallback approval.
- Operations owner: owns Aspire topology, OpenTelemetry, dashboards, alerts, and release evidence.

The team should treat generated UI as a productivity tool, not a substitute for product judgment. Destructive workflows, authorization-sensitive controls, audit review, and cross-tenant incident response need explicit UX review even when a generator can produce a working form or table.

### Cost Optimization and Resource Management

FrontComposer can reduce repeated table/form/detail implementation cost, but only if model boundaries and generated metadata are stable. The main cost controls are:

- centralize adapter mappings instead of duplicating transformations in each component,
- use generated components for low-risk list/detail/form surfaces,
- reserve custom components for destructive, audit, authorization, and incident-response workflows,
- keep integration tests focused on cross-process behavior instead of repeating unit coverage,
- use Playwright smoke paths for common flows and targeted E2E cases for high-risk states,
- rely on Aspire for local orchestration rather than requiring each developer to manually start Dapr, EventStore, Tenants, and the UX.

Cost risk appears when the UX tries to use one model for domain commands, API contracts, projection DTOs, and generated display rows. The extra adapter layer is intentional and should lower long-term cost by preventing generator conventions from leaking into backend contracts.

_Sources:_ Local: `Directory.Packages.props`, `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Testing`, `docs/tenants-ui-frontcomposer-dependency-map.md`. Web: https://learn.microsoft.com/en-us/azure/well-architected/cost-optimization/

### Risk Assessment and Mitigation

| Risk | Evidence | Impact | Mitigation |
| --- | --- | --- | --- |
| UI generator model mismatch | Tenants commands are immutable records; FrontComposer command generation expects mutable UI shapes. | Build/runtime adapter failures or pressure to weaken domain contracts. | Use UX command adapter models and tested mappings. |
| Projection model mismatch | Tenants read DTOs are not necessarily partial/generator-shaped. | Generated projection components fail or become too coupled to API DTOs. | Use UI projection rows/details as adapter models. |
| EventStore host dependency collision | Tenants project currently references EventStore web host with TODO for `wwwroot`/config collisions. | Shell/host integration instability. | Replace with stable library/package references or isolate host composition. |
| Dapr version mismatch | CI initializes Dapr `1.16.0`; packages reference Dapr `1.17.9`. | Integration tests pass/fail inconsistently or hide runtime issues. | Align CI/runtime/package versions. |
| Missing audit timeline | Dependency map marks audit timeline evidence as missing. | Audit UX overpromises unsupported component behavior. | Use approved DataGrid-backed flat audit fallback or create `FC-AUD`. |
| Missing consequence preview | Dependency map marks consequence preview evidence as missing. | Destructive workflows lack clear, accessible risk communication. | Build reusable `FC-CNS` or approve scoped inline fallback. |
| Command batching gap | No verified toast batching/concurrent command policy. | Rapid incident-response commands overwhelm users or produce ambiguous feedback. | Limit early destructive workflows to one pending action or implement batching. |
| Accessibility/localization gaps | Tenants-specific coverage is not verified. | Generated UX may be unusable for keyboard/screen-reader/localized users. | Make `FC-A11Y` and `FC-L10N` explicit story gates. |
| Projection freshness ambiguity | Event-sourced confirmation depends on projection updates and subscriptions. | Users may not trust command completion. | Surface pending/confirmed/rejected states and monitor projection latency. |

_Sources:_ Local: `docs/tenants-ui-frontcomposer-dependency-map.md`, `.github/workflows/ci.yml`, `Directory.Packages.props`, `src/Hexalith.Tenants/Hexalith.Tenants.csproj`. Web: https://learn.microsoft.com/en-us/azure/well-architected/security/, https://learn.microsoft.com/en-us/azure/well-architected/reliability/

## Technical Research Recommendations

### Implementation Roadmap

**Phase 0 - Readiness cleanup**

- Align Dapr package/runtime versions in CI, release, and local developer documentation.
- Resolve the EventStore web-host project reference collision risk.
- Decide whether `Hexalith.Tenants.FrontComposer` is a source project in this repo or a package/module consumed by a separate UI host.
- Record source-backed status for every dependency ID from `docs/tenants-ui-frontcomposer-dependency-map.md`.

**Phase 1 - FrontComposer foundation**

- Create the FrontComposer module.
- Add UX command adapter models and mappings for a small command set.
- Add projection adapter rows/details for tenant list and tenant detail.
- Wire EventStore command/query/hub endpoints.
- Add generator/manifest validation tests.

**Phase 2 - Read-only operational UX**

- Implement tenant list, tenant detail overview, user membership read-only table, configuration read-only display, and audit flat-list fallback.
- Add ETag, loading, empty, stale, and error states.
- Validate keyboard and screen-reader behavior for generated tables and detail views.

**Phase 3 - Command workflows**

- Add create tenant, enable/disable tenant, add/remove/change user role, set/remove global administrator, and set/remove configuration.
- Use custom overrides for destructive and authorization-sensitive flows.
- Add command pending/confirmed/rejected feedback and projection confirmation tests.

**Phase 4 - Product readiness**

- Add or approve fallback for audit timeline and consequence preview.
- Add localization evidence for labels, roles, statuses, warnings, and audit timestamps.
- Add Playwright/axe E2E gates for critical flows.
- Add operational dashboards and release evidence.

### Technology Stack Recommendations

Use:

- .NET SDK `10.0.300` baseline from `global.json`.
- Blazor/Fluent UI through FrontComposer shell conventions.
- FrontComposer Contracts/Shell/Testing as the UX composition foundation.
- EventStore command/query/hub adapter endpoints for browser-facing operations.
- Aspire for local/distributed orchestration.
- Dapr for backend infrastructure only where already used.
- OpenTelemetry for correlated traces/logs/metrics.
- xUnit, Shouldly, bUnit, FrontComposer.Testing, Aspire testing, and Playwright/axe for the quality stack.

Avoid:

- direct generator annotations on existing immutable Tenants command contracts,
- direct browser/Dapr coupling,
- treating planning aliases as source evidence,
- using generated defaults for destructive or cross-tenant security workflows without UX review,
- merging domain command contracts, API DTOs, and UI projection rows into one model.

### Skill Development Requirements

The team should build working knowledge in:

- FrontComposer generator conventions and shell registration,
- Blazor component testing with bUnit,
- Fluent UI accessibility behavior,
- EventStore command/query/projection semantics,
- SignalR projection-change subscriptions,
- Dapr/Aspire orchestration and diagnostics,
- OpenTelemetry trace correlation,
- destructive-action UX and accessible consequence disclosure,
- localization boundaries between shell-owned and Tenants-owned strings.

### Success Metrics and KPIs

Implementation success should be measured with technical and user-visible signals:

- all adapter mappings covered by tests,
- generated metadata validation runs in CI,
- command confirmation latency measured and within target for normal flows,
- projection update SignalR reconnects tracked and recoverable,
- ETag not-modified behavior verified for high-traffic reads,
- zero unreviewed dependency-map `missing` items in production stories,
- critical keyboard flows pass component and browser tests,
- axe scans pass for target screens,
- destructive workflows include consequence, localization, focus, and audit expectations,
- CI gates complete within an acceptable PR feedback window,
- operations dashboards show command, projection, auth, SignalR, and query-cache health.

### Step 5 Conclusion

The implementation path is practical, but it should be treated as a phased product integration rather than a generator flip. FrontComposer is strongest for consistent list, detail, projection, and command composition. Tenants still needs explicit adapter models, custom destructive-flow components, source-backed dependency decisions, and rigorous accessibility/localization testing before the UX can be considered production-ready.

---

## Research Synthesis: FrontComposer as a Tenant-Aware UX Composition Layer

### Executive Summary

Hexalith.FrontComposer can be used to create the Hexalith.Tenants UX if it is positioned as a composition and generation layer over the existing event-sourced platform. The strongest technical path is not to annotate or reshape existing Tenants contracts. The stronger path is to create a dedicated `Hexalith.Tenants.FrontComposer` module that maps between FrontComposer-friendly UX models and the current immutable command contracts, query DTOs, projection read models, EventStore command/query endpoints, and SignalR projection-change stream.

The research found a solid architectural fit among Tenants, Hexalith.EventStore, and Hexalith.FrontComposer. Tenants already owns event-sourced domain behavior, Dapr/Aspire hosting, projections, authorization, and contract tests. EventStore provides the command/query gateway and projection-change hub. FrontComposer provides shell, rendering, generated component, communication, feedback, and testing infrastructure. The integration risk is concentrated at boundaries: command model shape, projection model shape, tenant/auth context propagation, projection freshness, destructive workflow UX, accessibility, localization, and source-backed dependency readiness.

The recommended strategy is phased adoption. First, clean up readiness risks and create the FrontComposer adapter module. Then ship read-only tenant operations screens. Then add command workflows with strong pending/confirmed/rejected feedback. Finally, harden audit timeline, consequence preview, localization, accessibility, E2E evidence, and operations dashboards before treating the UX as production-ready.

**Key Technical Findings:**

- FrontComposer fits best as an adapter-backed composition module, not as a direct modification to Tenants domain contracts.
- Tenants command contracts and DTOs should remain domain/API contracts; FrontComposer should receive UI adapter models designed for generation and rendering.
- EventStore is the correct browser-facing gateway for command, query, and projection-change UX behavior.
- Aspire should orchestrate the UX, Tenants, EventStore, Dapr dependencies, and observability consistently.
- Current dependency evidence marks audit timeline, consequence preview, command batching, semantic tokens, Tenants-specific accessibility, localization, and docs as readiness items.
- Two repo risks should be resolved early: EventStore web-host project reference collision and Dapr runtime/package version mismatch.

**Technical Recommendations:**

- Create `src/Hexalith.Tenants.FrontComposer` as the integration boundary.
- Implement read-only generated UX first, then command workflows, then high-risk custom overrides.
- Use typed adapter tests, component tests, Aspire integration tests, Playwright E2E tests, and accessibility checks as release gates.
- Keep browser-facing UX coupled to EventStore endpoints and SignalR, not directly to Dapr sidecars.
- Treat destructive actions, audit timelines, and authorization-sensitive operations as custom UX surfaces with explicit accessibility and localization evidence.

### Table of Contents

1. Technical Research Introduction and Methodology
2. Technical Landscape and Architecture Analysis
3. Implementation Approaches and Best Practices
4. Technology Stack Evolution and Current Trends
5. Integration and Interoperability Patterns
6. Performance and Scalability Analysis
7. Security and Compliance Considerations
8. Strategic Technical Recommendations
9. Implementation Roadmap and Risk Assessment
10. Future Technical Outlook and Innovation Opportunities
11. Technical Research Methodology and Source Verification
12. Technical Appendices and Reference Materials

### 1. Technical Research Introduction and Methodology

#### Technical Research Significance

Tenants is an operational domain where UX correctness is tightly linked to event-sourced behavior. Operators need to understand whether tenant commands were accepted, rejected, pending, projected, stale, or unauthorized. A generated UX layer can reduce repeated table, form, and detail-view work, but only if it preserves domain boundaries and exposes event-sourced consistency honestly.

The technical significance is therefore twofold. First, FrontComposer can create a consistent operator experience across tenant list, detail, membership, configuration, global administrator, audit, and command workflows. Second, the platform must avoid using the generator as a shortcut around domain contracts, authorization rules, accessibility requirements, or projection-lag behavior.

_Sources:_ Local repository evidence in `src`, `tests`, `docs/tenants-ui-frontcomposer-dependency-map.md`, and `Hexalith.FrontComposer/src`. Web: https://learn.microsoft.com/en-us/azure/architecture/patterns/event-sourcing, https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs, https://learn.microsoft.com/en-us/azure/architecture/patterns/strangler-fig

#### Technical Research Methodology

This research used four evidence layers:

- Local Tenants source: contracts, server, client, AppHost, ServiceDefaults, tests, CI, release, package versions, and planning artifacts.
- Local Hexalith.FrontComposer source: Contracts, Shell, Testing, EventStore adapter wiring, generated rendering primitives, testing docs, accessibility evidence, and E2E tests.
- Local Hexalith.EventStore evidence: command/query gateway patterns, projection-change SignalR hub, tenant context, authorization, and event-sourced hosting concepts.
- Current public technical documentation: Microsoft Azure Architecture Center, Microsoft Learn, Dapr docs, Playwright .NET, bUnit, and Azure Well-Architected Framework.

The analysis framework separated technology stack, integration contracts, architecture, implementation approach, risk, quality, operations, and roadmap. Claims that depend on current ecosystem behavior were checked against live official documentation where available.

#### Research Goals and Objectives

**Original Technical Goal:** Define the architecture, integration points, implementation approach, and practical UX composition strategy for a Tenants frontend built with FrontComposer.

**Achieved Objectives:**

- Identified the correct integration boundary: `Hexalith.Tenants.FrontComposer`.
- Confirmed that adapter models are required for commands and projections.
- Mapped FrontComposer to EventStore command, query, ETag, SignalR, tenant context, and authorization behavior.
- Defined the phased implementation roadmap.
- Identified source-backed readiness gaps and risks.
- Defined testing, accessibility, observability, and release evidence requirements.

### 2. Technical Landscape and Architecture Analysis

#### Current Technical Architecture Patterns

The relevant architecture is a generated Blazor/FrontComposer UX over an event-sourced backend. Tenants owns domain commands, projections, policies, and backend behavior. Hexalith.EventStore provides command/query dispatch and projection notifications. Hexalith.FrontComposer provides the shell, metadata, generated UI composition, command/projection rendering, feedback patterns, and testing utilities.

The dominant patterns are:

- Event Sourcing for domain state and auditability.
- CQRS for command submission and projection-backed reads.
- Adapter/anti-corruption layer between UI generation and backend contracts.
- Strangler-style phased adoption for the new UX.
- Aspire AppHost orchestration for local and distributed topology.
- OpenTelemetry-based observability across UX, gateway, backend, and projection behavior.

The main architectural trade-off is accepting extra mapping code in exchange for protecting domain contracts. That is the correct trade-off. Removing the adapter layer would reduce short-term code but would leak generator requirements into event-sourced contracts and increase long-term coupling.

_Sources:_ https://learn.microsoft.com/en-us/azure/architecture/patterns/event-sourcing, https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs, https://learn.microsoft.com/en-us/azure/architecture/patterns/strangler-fig, https://learn.microsoft.com/en-us/dotnet/aspire/

#### System Design Principles

The UX architecture should follow these principles:

- Preserve domain contracts: do not change Tenants commands or DTOs just to satisfy generator conventions.
- Separate UX models from API contracts: generated forms and rows get their own model shapes.
- Keep command lifecycle visible: pending, confirmed, rejected, stale, and unauthorized states must be visible and accessible.
- Make eventual consistency explicit: projection freshness should be part of the UX behavior.
- Fail closed for tenant/auth ambiguity: missing tenant context or insufficient claims should prevent commands, queries, and subscriptions.
- Treat destructive workflows as custom surfaces: generated defaults are not enough for consequence-heavy operations.
- Require source-backed dependency status: planning aliases are not implementation evidence.

### 3. Implementation Approaches and Best Practices

#### Current Implementation Methodologies

The implementation should be incremental and test-first around the adapter boundary. The first useful implementation slice is not a polished UI screen. It is a working path that proves:

- a FrontComposer command adapter model can map to a Tenants command,
- a projection adapter can render data from a Tenants query/projection,
- EventStore endpoints can be configured from the UX host,
- SignalR projection-change subscriptions update the UI or trigger refresh,
- tenant context and authorization metadata are enforced,
- generated metadata is stable enough for CI validation.

After that foundation, read-only screens should come before command workflows. Command workflows should come before high-risk destructive UX. Audit timeline and consequence preview should be implemented or explicitly approved as fallbacks before production use.

_Sources:_ https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test, https://docs.github.com/en/actions/automating-builds-and-tests/building-and-testing-net, https://learn.microsoft.com/en-us/dotnet/aspire/testing/

#### Implementation Framework and Tooling

The recommended tooling is already mostly present:

- .NET SDK `10.0.300` with latest patch roll-forward.
- Central package management through `Directory.Packages.props`.
- Aspire packages and AppHost project.
- Dapr packages and Dapr-backed backend patterns.
- OpenTelemetry packages and ServiceDefaults.
- xUnit, Shouldly, NSubstitute, Testcontainers, bUnit/FrontComposer.Testing evidence, and Playwright E2E conventions.
- GitHub Actions CI and semantic-release workflow.

Additions should be scoped:

- a FrontComposer integration project,
- adapter/generator validation tests,
- component tests for generated and custom Tenants UI surfaces,
- Playwright/axe tests for critical flows,
- dashboards or telemetry queries for command/projection UX health.

### 4. Technology Stack Evolution and Current Trends

#### Current Technology Stack Landscape

The stack is a modern .NET distributed application stack:

- .NET and Blazor for application and UX composition.
- Fluent UI through FrontComposer shell conventions.
- EventStore-style command/query/projection architecture.
- Dapr for backend actor, pub/sub, and distributed building-block integration.
- Aspire for orchestration, local developer experience, service discovery, and observability-friendly composition.
- OpenTelemetry for traces, metrics, and logs.
- GitHub Actions for CI and release automation.

This stack aligns with current Microsoft guidance around cloud-native .NET applications, explicit distributed app orchestration, observable systems, and testable service composition.

_Sources:_ https://learn.microsoft.com/en-us/dotnet/aspire/, https://docs.dapr.io/developing-applications/building-blocks/actors/actors-overview/, https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/, https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-with-otel

#### Technology Adoption Patterns

The adoption pattern should not be a rewrite. It should be a controlled extension:

- Introduce FrontComposer beside the existing Tenants backend.
- Use EventStore as the integration gateway.
- Keep Dapr behavior backend-scoped.
- Use generated UX for stable low-risk patterns.
- Use custom components for high-risk human decisions.
- Expand once test and operations evidence exists.

This matches the Strangler Fig modernization principle: deliver new functionality around the existing system while avoiding a large, risky replacement step.

### 5. Integration and Interoperability Patterns

#### Current Integration Approaches

The key integration path is:

```text
FrontComposer Shell
  -> UX command/projection adapter models
  -> EventStore command/query client services
  -> EventStore command/query endpoints
  -> Tenants handlers, aggregates, projections
  -> EventStore projection changes hub
  -> FrontComposer subscription/cache/refresh behavior
```

The UX should use typed command and query services registered by FrontComposer/EventStore integration. It should not dispatch directly into Tenants internals or Dapr sidecars from the browser-facing layer.

Interoperability boundaries:

- Command boundary: UI command models map to immutable Tenants command contracts.
- Query boundary: UI read requests map to Tenants query DTOs.
- Projection boundary: Tenants projections map to FrontComposer display rows/details.
- Feedback boundary: EventStore and projection updates map to pending/confirmed/rejected UI states.
- Auth boundary: identity, tenant context, and policies map to authorized regions and disabled actions.

_Sources:_ Local: `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Extensions/EventStoreServiceExtensions.cs`, `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Contracts/Communication`, `src/Hexalith.Tenants.Contracts`. Web: https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs

#### Interoperability Standards and Protocols

The practical standards and protocols are:

- HTTP APIs for command/query submission.
- SignalR for projection-change notifications.
- ETags for read-side cache coherence.
- OpenID Connect/JWT claims for identity propagation.
- OpenTelemetry for correlation and diagnostics.
- ProblemDetails-style error reporting for validation/rejection states where available.

The integration should prefer typed clients and structured result models over stringly typed UI dispatch.

### 6. Performance and Scalability Analysis

#### Performance Characteristics and Optimization

The most important performance dimension is not raw page load speed. It is perceived reliability across command submission, projection update, and UI confirmation. Operators must be able to see whether a command is pending, accepted, rejected, or not yet reflected in projections.

Performance priorities:

- keep generated list/detail components bounded and virtualized where needed,
- use ETags and not-modified responses for read-heavy projection data,
- avoid query-time event replay for audit timelines,
- measure projection confirmation latency,
- keep SignalR reconnect and fallback behavior observable,
- make loading, empty, stale, and error states explicit.

Audit is the highest-risk screen. A flat audit timeline fallback can be acceptable, but it needs bounded rendering and keyboard evidence before large-result use.

_Sources:_ https://learn.microsoft.com/en-us/azure/well-architected/reliability/, https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-with-otel

#### Scalability Patterns and Approaches

Scalability should come from the existing event-sourced and projection-backed design:

- commands remain small and explicit,
- reads are served from projections,
- browser state is refreshed from projection notifications and cache validation,
- the UX host scales as a stateless web resource,
- Dapr and backend infrastructure remain server-side concerns,
- telemetry identifies projection lag and query hotspots.

The UX should avoid adding backend requirements for consequence previews unless a screen already loads the required projection data. If necessary data is absent, the story should record `needs-confirmation` instead of inventing a new consequence endpoint.

### 7. Security and Compliance Considerations

#### Security Best Practices and Frameworks

The security model must preserve Tenants authorization semantics across the generated UX:

- every command-capable region should be policy-aware,
- unauthorized actions should not be presented as active controls,
- tenant context must be validated for command, query, and subscription operations,
- SignalR group joins must enforce tenant/authorization checks,
- destructive workflows need explicit consequence disclosure,
- errors should be diagnosable without leaking internal details,
- frontend authorization affordances must not replace backend authorization.

Blazor and OIDC/JWT integration should follow platform guidance, while final authorization remains server-enforced.

_Sources:_ https://learn.microsoft.com/en-us/aspnet/core/blazor/security/, https://learn.microsoft.com/en-us/azure/well-architected/security/

#### Compliance and Governance Considerations

The main governance concern is auditability. Tenant lifecycle changes, membership changes, global administrator changes, and high-impact configuration changes should be visible in projection-backed audit UX. The UX should not synthesize audit truth independently from backend projections.

Governance practices:

- source-backed dependency IDs for UI stories,
- clear ownership for shell-owned versus Tenants-owned localization strings,
- explicit accessibility evidence for command, audit, and destructive flows,
- release evidence for high-risk workflows,
- traceability from UX action to command, backend handling, event, projection update, and audit display.

### 8. Strategic Technical Recommendations

#### Technical Strategy and Decision Framework

Recommended architecture decision:

```text
src/
  Hexalith.Tenants.FrontComposer/
    Commands/
    Projections/
    Mapping/
    Registration/
    Components/
    Security/
    Localization/
    Testing/
```

Use generated FrontComposer primitives for:

- tenant list,
- tenant detail summary,
- member table,
- configuration read-only display,
- simple validated forms once adapter tests exist.

Use custom overrides for:

- disable/enable tenant,
- remove user,
- role changes,
- global administrator changes,
- high-impact configuration changes,
- audit timeline,
- command rejection details,
- incident-response workflows.

#### Competitive Technical Advantage

The advantage of this approach is consistency without domain compromise. The Tenants UX can gain generated shell coherence, uniform feedback, shared testing infrastructure, and faster screen delivery while preserving event-sourced correctness and policy boundaries.

The differentiator is not generation alone. It is generated UX plus event-sourced lifecycle transparency: users can see command state, projection freshness, authorization behavior, and audit evidence as first-class parts of the experience.

### 9. Implementation Roadmap and Risk Assessment

#### Technical Implementation Framework

**Phase 0 - Readiness cleanup**

- Align Dapr runtime and package versions.
- Resolve the EventStore web-host reference collision risk.
- Decide module ownership and package/reference strategy.
- Reconfirm dependency-map status for `FC-TBL`, `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-AUD`, `FC-CNS`, `FC-TOK`, `FC-A11Y`, `FC-L10N`, and `FC-DOC`.

**Phase 1 - FrontComposer foundation**

- Create `Hexalith.Tenants.FrontComposer`.
- Add command and projection adapter models.
- Add mapping tests.
- Register EventStore command/query/hub clients.
- Add generated metadata validation.

**Phase 2 - Read-only UX**

- Tenant list.
- Tenant detail.
- User membership table.
- Configuration display.
- Audit flat-list fallback if approved.
- Loading, empty, stale, and error states.

**Phase 3 - Command UX**

- Create/update/enable/disable tenant.
- Add/remove/change member roles.
- Set/remove global administrator.
- Set/remove configuration.
- Pending/confirmed/rejected feedback and projection confirmation.

**Phase 4 - Production readiness**

- Consequence preview or approved fallback.
- Audit timeline or approved fallback.
- Localization and accessibility evidence.
- Playwright/axe E2E gates.
- Operational dashboards and alerting.

#### Technical Risk Management

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Command model mismatch | Generator pressure on immutable domain contracts. | Use mutable UX adapters and tested mappings. |
| Projection shape mismatch | Generated projections coupled to API DTOs. | Use UI projection adapter rows/details. |
| EventStore host reference collision | Host/static/config conflict in UX composition. | Replace with stable library/package references or isolate host composition. |
| Dapr version mismatch | Unreliable integration evidence. | Align CI/runtime/package versions. |
| Missing audit timeline | Audit UX overpromises unsupported component behavior. | Build `FC-AUD` or approve DataGrid-backed fallback. |
| Missing consequence preview | Destructive flows lack sufficient risk disclosure. | Build `FC-CNS` or approve scoped inline fallback. |
| Command batching gap | Rapid operations create confusing feedback. | Limit first slice or implement batching policy. |
| Accessibility/localization gaps | Generated UX fails product readiness. | Gate with `FC-A11Y` and `FC-L10N` evidence. |
| Projection freshness ambiguity | Users distrust command completion. | Surface pending/projection-confirmed states and monitor latency. |

### 10. Future Technical Outlook and Innovation Opportunities

#### Near-Term Evolution

The near-term opportunity is to turn the dependency map into implementation-ready stories. FrontComposer already has enough verified primitives for projection list/detail work, but high-risk workflows need source-backed readiness or approved fallbacks.

Expected near-term work:

- harden command feedback,
- clarify layout variants,
- define localization ownership,
- add Tenants-specific component tests,
- implement or approve audit/consequence fallbacks.

#### Medium-Term Trends

In the medium term, the platform can standardize generated operational UX across Hexalith modules. If Tenants proves the adapter pattern, other bounded contexts can follow the same model: domain contracts stay stable, FrontComposer adapters generate common UX, and custom components handle high-risk workflows.

#### Innovation Opportunities

Useful innovation areas:

- metadata-driven policy-aware command regions,
- generated trace links from command submission to projection update,
- reusable consequence-preview metadata patterns,
- audit timeline virtualization and accessibility templates,
- contract-drift checks between domain contracts and UX adapters,
- reusable localized status/action vocabularies across Hexalith modules.

### 11. Technical Research Methodology and Source Verification

#### Primary Technical Sources

Local:

- `src/Hexalith.Tenants.Contracts`
- `src/Hexalith.Tenants.Server`
- `src/Hexalith.Tenants.AppHost`
- `src/Hexalith.Tenants.ServiceDefaults`
- `tests`
- `.github/workflows/ci.yml`
- `.github/workflows/release.yml`
- `docs/tenants-ui-frontcomposer-dependency-map.md`
- `Hexalith.FrontComposer/src`
- `Hexalith.FrontComposer/tests`
- `Hexalith.FrontComposer/docs`
- `Hexalith.EventStore/src`
- `Hexalith.EventStore/docs`

Web:

- Event Sourcing: https://learn.microsoft.com/en-us/azure/architecture/patterns/event-sourcing
- CQRS: https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs
- Strangler Fig: https://learn.microsoft.com/en-us/azure/architecture/patterns/strangler-fig
- .NET Aspire: https://learn.microsoft.com/en-us/dotnet/aspire/
- Aspire testing: https://learn.microsoft.com/en-us/dotnet/aspire/testing/
- Dapr actors: https://docs.dapr.io/developing-applications/building-blocks/actors/actors-overview/
- Dapr pub/sub: https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/
- .NET OpenTelemetry: https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-with-otel
- Blazor security: https://learn.microsoft.com/en-us/aspnet/core/blazor/security/
- `dotnet test`: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test
- GitHub Actions for .NET: https://docs.github.com/en/actions/automating-builds-and-tests/building-and-testing-net
- Playwright .NET: https://playwright.dev/dotnet/
- bUnit: https://bunit.dev/
- Azure Well-Architected Reliability: https://learn.microsoft.com/en-us/azure/well-architected/reliability/
- Azure Well-Architected Operational Excellence: https://learn.microsoft.com/en-us/azure/well-architected/operational-excellence/
- Azure Well-Architected Security: https://learn.microsoft.com/en-us/azure/well-architected/security/
- Azure Well-Architected Cost Optimization: https://learn.microsoft.com/en-us/azure/well-architected/cost-optimization/

#### Technical Web Search Queries Used

- Hexalith FrontComposer source and local docs inspection.
- Microsoft Azure Architecture Center event sourcing, CQRS, and strangler modernization patterns.
- Microsoft Learn .NET Aspire distributed application and testing guidance.
- Microsoft Learn .NET OpenTelemetry observability guidance.
- Dapr actors and pub/sub building-block documentation.
- Microsoft Learn Blazor security guidance.
- GitHub Actions .NET build/test documentation.
- Playwright .NET and bUnit testing documentation.
- Azure Well-Architected reliability, operational excellence, security, and cost optimization guidance.

#### Research Quality and Limitations

Confidence is high for local repository findings because they were based on checked-out source, project files, workflows, docs, and tests. Confidence is high for platform guidance because official documentation was used where possible.

The main limitation is that some FrontComposer deliverables are planning aliases or not verified in the current checkout. This report intentionally does not claim implementation readiness for audit timeline, consequence preview, toast batching, missing semantic tokens, Storybook coverage, or Tenants-specific accessibility/localization evidence unless source or approved fallback evidence exists.

### 12. Technical Appendices and Reference Materials

#### Architectural Pattern Summary

| Pattern | Use in Tenants UX | Decision |
| --- | --- | --- |
| Event Sourcing | Domain state and auditability. | Preserve backend model. |
| CQRS | Commands through EventStore; reads through projections. | Use as UX lifecycle foundation. |
| Adapter Layer | UI model to domain/API mapping. | Required. |
| Strangler Fig | Incremental UX adoption beside existing backend. | Recommended. |
| Generated UI Composition | Tables, forms, detail views, metadata-driven rendering. | Use for low-risk surfaces. |
| Custom UX Override | Destructive, audit, authorization-sensitive, incident-response workflows. | Required for high-risk surfaces. |
| Aspire AppHost | Distributed local/runtime topology. | Use for UX host integration. |
| OpenTelemetry | Trace UX command to backend and projection behavior. | Required for production operations. |

#### Technology Stack Recommendation

| Area | Recommended Choice |
| --- | --- |
| Runtime | .NET SDK `10.0.300` baseline with latest patch roll-forward. |
| UX shell | Hexalith.FrontComposer Shell and Contracts. |
| UI components | FrontComposer plus Fluent UI conventions. |
| Backend gateway | Hexalith.EventStore command/query/hub endpoints. |
| Backend platform | Tenants event-sourced domain and projections. |
| Distributed app host | Aspire AppHost. |
| Backend building blocks | Dapr where already used by the backend. |
| Observability | OpenTelemetry and structured logs. |
| Unit tests | xUnit, Shouldly, NSubstitute. |
| Component tests | bUnit and Hexalith.FrontComposer.Testing. |
| Integration tests | Aspire testing and Testcontainers where needed. |
| Browser tests | Playwright plus axe accessibility checks. |

#### Production Readiness Checklist

- `Hexalith.Tenants.FrontComposer` project exists and is referenced intentionally.
- Dapr runtime and package versions are aligned.
- EventStore host reference collision is resolved or isolated.
- Command adapter models are tested.
- Projection adapter models are tested.
- Generated metadata is validated in CI.
- EventStore endpoints and hub paths are configured.
- Tenant context and authorization behavior are tested.
- Read-only screens handle loading, empty, stale, and error states.
- Command workflows show pending, confirmed, and rejected states.
- Destructive workflows include consequence disclosure.
- Audit timeline or approved fallback exists.
- Localization ownership is defined.
- Accessibility evidence exists for critical flows.
- Playwright/axe gates cover release-critical screens.
- OpenTelemetry dashboards cover command, projection, SignalR, query cache, and authorization health.

## Technical Research Conclusion

Hexalith.FrontComposer can create the Tenants UX effectively, but the successful architecture is an adapter-backed composition layer, not a direct generator pass over the existing domain contracts. The platform should preserve Tenants' event-sourced command and projection model, use Hexalith.EventStore as the browser-facing command/query/projection-change gateway, and introduce a dedicated FrontComposer module that owns UI model shapes and generated shell registration.

The main implementation decision is to be deliberate about boundaries. Domain commands, API DTOs, read projections, and UI rendering models should remain separate. That extra mapping cost is justified because it protects backend correctness, improves testability, and lets FrontComposer evolve without destabilizing Tenants contracts.

The next technical step is readiness cleanup plus a small vertical spike: create the FrontComposer module, map one command, map one projection, route through EventStore, receive a projection-change notification, validate metadata in CI, and prove accessibility-friendly feedback in a component test. That spike will turn the research into implementation evidence.

**Technical Research Completion Date:** 2026-05-26  
**Research Period:** Current comprehensive technical analysis  
**Source Verification:** Local repository evidence plus current official documentation  
**Technical Confidence Level:** High for architecture and implementation direction; medium for unresolved FrontComposer dependency readiness until source or approved fallback evidence exists.

---

<!-- Content will be appended sequentially through research workflow steps -->
