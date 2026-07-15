# Merge Technology And Reality Check Review

Verdict: **fail — the merge is not technology/reality-clean yet.**

The canonical decisions are directionally compatible with the brownfield UI, but the merged architecture overstates AD-6's remediation readiness, overstates implementation conformance, and carries a stale dependency/version ledger. The AD-6 finding is accurate at the first step — `TenantQueryGateway` does use the generic EventStore gateway — but incomplete as a remediation diagnosis because the current Tenants REST host routes through that same generic gateway and does not presently preserve the projection-backed metadata AD-6 requires.

## Evidence Used

Local repository state was treated as the authority; no web research was necessary.

- Architecture inputs: `ARCHITECTURE-SPINE.md` and merged `_bmad-output/planning-artifacts/architecture.md`.
- UI read path: `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`, `src/Hexalith.Tenants.UI/Program.cs`, and `src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs`.
- REST host and generated route behavior: `src/Hexalith.Tenants.Api/Program.cs`, `src/Hexalith.Tenants.Api/RestApiAssemblyInfo.cs`, query-contract `[RestRoute]` annotations, and `references/Hexalith.EventStore/src/Hexalith.EventStore.RestApi.Generators/RestApiControllerEmitter.cs`.
- Query metadata semantics: `references/Hexalith.EventStore/src/Hexalith.EventStore/Queries/HandlerAwareQueryRouter.cs`, `references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Gateway/EventStoreGatewayClient.cs`, and Tenants query handlers/result construction under `src/Hexalith.Tenants/Queries`.
- AppHost composition: `src/Hexalith.Tenants.AppHost/Program.cs` and `HexalithTenantsUI.cs`.
- Version/dependency reality: `global.json`, root `Directory.Build.props`, `references/Hexalith.Builds/Props/Directory.Packages.props`, `src/Hexalith.Tenants.UI/obj/project.assets.json`, and current root-declared submodule revisions.
- Governing project facts: `_bmad-output/project-context.md`, the dependency project-context files, and `references/Hexalith.AI.Tools/hexalith-llm-instructions.md`.

## Critical Finding

### 1. AD-6 identifies the current divergence, but its stated remediation path does not satisfy AD-6 in the current platform

`TenantQueryGateway` injects `IEventStoreGatewayClient` and calls `SubmitQueryAsync` for detail, list, membership, global-administrator, audit, and search hydration reads. `Program.cs` registers the gateway only from `EventStore:BaseAddress`. Therefore the merged conformance statement is correct that the implementation does **not** call the direct Tenants REST endpoints.

However, changing only the UI transport to the current Tenants REST host would not establish the promised ETag/read-model-freshness contract:

1. The generated Tenants REST actions build a `SubmitQueryRequest` and call `IEventStoreGatewayClient.SubmitQueryAsync` themselves.
2. EventStore's `HandlerAwareQueryRouter` routes the Tenants `IDomainQueryHandler` and stamps the result as `HandlerComputed`, explicitly leaving `ProjectionType` null.
3. `EventStoreGatewayClient.NormalizeMetadata` retains ETag, not-modified, stale, and projection-version fields only for `ProjectionBacked` provenance; for other provenance it clears ETag, `IsNotModified`, `IsStale`, and `ProjectionVersion`.
4. The generated REST controller emits an ETag/304 only for `ProjectionBacked` provenance.

The Tenants handlers do create `QueryResponseMetadata` from persisted `IReadModelFreshness` and state-store ETags, but the current generic route's provenance policy prevents that metadata from surviving as the HTTP caching/freshness contract described by AD-6.

The AppHost also wires `tenants-ui` to `eventStore` and Memories only, supplies only `EventStore__BaseAddress`, and does not give the UI a `tenants-api` reference/base address. No typed Tenants REST query client exists in the current UI gateway folder. Consequently, the remediation is a cross-component/platform contract change, not a local `TenantQueryGateway` substitution.

**Required correction:** retain the finding that current UI code diverges from AD-6, but replace the implementation priority with a gated plan that first defines how projection-backed provenance and ETag/freshness metadata survive the Tenants REST path. That plan must cover EventStore/Tenants.Api semantics, UI typed client/error mapping, AppHost `tenants-api` wiring, bearer relay, and end-to-end 200/304/stale/unknown evidence. Until then, AD-6 is an adopted target with a blocked implementation path, not a ready remediation.

## High Findings

### 2. The merged conformance statement overclaims AD-8 and overall readiness

The document says current code confirms AD-1 through AD-5 and AD-7 through AD-13. Structurally, the UI does consume `QueryResponseMetadata` and maps `IsStale` to `ReadModelFreshnessState`; operationally, the configured generic handler route normalizes that freshness evidence away. `TenantQueryGateway.ResolveFreshness` therefore receives no authoritative stale/current classification on the real route and falls back to `Unknown`.

This makes “AD-8 confirmed” too strong and weakens the “READY WITH REMEDIATION / HIGH confidence” assessment. The design rule is present, but its end-to-end metadata source is not conformant. AD-8 should be grouped with AD-6 as open end-to-end conformance rather than claimed complete.

### 3. The stack/version ledger is stale and describes the wrong dependency posture

The spine and merged architecture name several pins that no longer match the checked-out build:

| Item | Architecture claim | Local current evidence |
| --- | --- | --- |
| Fluent UI Blazor | `5.0.0-rc.3-26138.1` | `5.0.0-rc.4-26180.1` in central packages and UI assets |
| FrontComposer source | `e2ac85aac67d` | current root submodule `4aa4210d4aeb...` |
| EventStore source | `60e63a95bed8` | current root submodule `1a01e0eae50e...` |
| EventStore package | `3.19.0` | `3.64.1` |
| Memories source | `24757db93c90` | current root submodule `c212a1ba6af0...` |
| Memories package | `1.31.1` | `2.5.0` |

The dependency-mode wording is also reversed for EventStore and Memories: root `Directory.Build.props` uses NuGet dependencies by default and enables source references only when `UseHexalithProjectReferences=true` (or the inverse legacy switch is explicitly set). FrontComposer is directly source-referenced by the UI project. Current submodules are useful source evidence but are not the default build closure for every dependency.

The following named pins still match local reality: .NET SDK `10.0.301`, Dapr packages `1.18.4`, Aspire `13.4.6`, xUnit v3 `3.2.2`, and bUnit `2.8.4-preview`.

**Required correction:** refresh every version/hash claim from central package files and root submodule state, state the default package-versus-source mode accurately, and remove the repeated claim that Fluent RC3 is the current inherited pin.

### 4. AD-13 is not reconciled with the authoritative domain-module boundary instruction

AD-13 adopts `src/Hexalith.Tenants.AppHost` as repository-owned wiring and the brownfield code implements that topology. `_bmad-output/project-context.md` also calls this AppHost an allowed repository-specific technical component. In contrast, `references/Hexalith.AI.Tools/hexalith-llm-instructions.md` identifies Hexalith.Tenants as a domain module and says a domain module must not ship its own `*.AppHost`, `*.Aspire`, or `*.ServiceDefaults` project; the EventStore project context repeats the same platform boundary.

The merge cannot truthfully say “No contradiction remains” while these governing sources disagree. Existing code establishes brownfield reality, but it does not silently waive the higher-level boundary rule.

**Required correction:** obtain and record an explicit exception/updated platform rule for this presentation-host topology, or move the topology ownership to the platform/host repository and revise AD-13. Until one is chosen, AD-13 is a governance conflict, not fully adopted conformance.

## Medium Findings

### 5. The fixed read-surface count is six, not five

The architecture's “5 read endpoints” list omits the implemented `GET /api/global-administrators` contract while later sections depend on it for FR-18/FR-19. The current contracts expose six GET query routes: list tenants, tenant detail, tenant users, user tenants, tenant audit, and global administrators. Correct the count and endpoint inventory so gateway/client scope is not underestimated.

### 6. Brownfield comments and generated evidence contradict the canonical transport rule

`src/Hexalith.Tenants.Api/Program.cs` explicitly says the interactive UI uses EventStore client libraries directly, and tests/source in the current workspace are centered on `IEventStoreGatewayClient` rather than a `TenantsQueryApiClient`. Meanwhile, `tests/test-summary.md` contains historical claims that a REST-backed typed client exists even though that client is absent from current source. These are not architecture authorities, but they are drift signals. The AD-6 remediation should include removing or reconciling stale comments/evidence after the transport contract is genuinely implemented.

## Reality-Confirmed Areas

- `global.json` and project defaults support .NET 10 / `net10.0`, nullable, implicit usings, latest language version, and warnings as errors.
- `Program.cs` uses `AddInteractiveServerComponents` and `AddInteractiveServerRenderMode`; InteractiveServer is a real brownfield choice.
- Tenants registers exactly one FrontComposer navigation entry at `/tenants`, with page-local workspace routing and compatibility pages.
- UI components depend on `ITenantQueryGateway`/`ITenantCommandGateway`; direct browser-to-backend HTTP calls were not found in the reviewed surface.
- The UI host is a publishable SDK-container project and the AppHost currently adds it to the resource graph.
- Memories search is implemented as an id match-set followed by authoritative tenant hydration, matching the intended search-as-index-only pattern at the composition level.

## Gate Conclusion

Do not treat the merged architecture as implementation-ready solely after changing `TenantQueryGateway` to call `tenants-api`. First resolve the projection-provenance/metadata contract and AppHost client wiring, then prove AD-6/AD-8 end to end. In parallel, refresh the stack ledger and reconcile the AD-13 platform-boundary conflict. The remaining brownfield composition decisions are broadly credible.
