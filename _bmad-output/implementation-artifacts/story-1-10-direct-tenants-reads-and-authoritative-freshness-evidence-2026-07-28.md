# Story 1.10 Evidence — Direct Tenants Reads and Authoritative Freshness

Date: 2026-07-28  
Story baseline commit: `8d64563c75423c861b0be0e3a7cc4de18f673d37`  
Repository revision under the working tree: `54fabf9852168b7e1f1639f9253472889397915a`

## Outcome

The Tenants UI BFF performs all six query reads through a server-side typed REST client configured from
`Tenants:BaseAddress`. EventStore remains the independently configured command/status dependency. A
missing read or command reference fails closed on its own side; neither side falls back to the other.

Tenant members have a dedicated immutable paged tenant-users snapshot. Visible rows, paging, ETag,
projection version, lifecycle, and freshness come only from that read. Detail-owned owner/risk context
and member actions are available only when detail and tenant-users evidence are both current and have
the same supported projection version. Existing detail re-query remains command-confirmation authority.

Optional projection notifications are exact-scope, reference-counted, coalesced nudges. A matching
signal exposes refreshing intent and requests an authoritative direct re-query while retaining that
read's last-confirmed data. It does not change payload, ETag, projection version, freshness, command
confirmation, or audit availability.

## Six-route and transport proof

`TenantsRestQueryClient` contains exactly these typed production GET path shapes:

1. `/api/tenants?cursor=…&pageSize=…`
2. `/api/tenants/{tenantId}`
3. `/api/tenants/{tenantId}/users?cursor=…&pageSize=…`
4. `/api/users/{userId}/tenants?cursor=…&pageSize=…`
5. `/api/tenants/{tenantId}/audit?from=…&to=…&category=…&cursor=…&pageSize=…`
6. `/api/global-administrators?cursor=…&pageSize=…`

The six path-building sites were lines 39, 53, 68, 85, 102, and 120 at verification time. Literal
route identities and query values are escaped; dot-only route identities cannot be normalized into a
different resource. The client accepts only HTTP/HTTPS base addresses, payloads only from `200 OK`, and
conditional retention only from `304 Not Modified`. Every other 2xx is rejected.

The final structural command

```text
rg -n "SubmitQueryAsync|/api/v1/queries|QueryRouter|HandlerAwareQueryRouter" src/Hexalith.Tenants.UI
```

returned no matches (the expected `rg` exit code 1). Thus no production Tenants UI read path contains
the generic EventStore query route or submission/router symbols. Commands and status remain outside
that query-read prohibition.

## Authoritative metadata and failure behavior

- Request and response validators must be bounded, strong ETags. A `304` is accepted only if a valid
  validator was actually sent, the response repeats that exact normalized validator, and response
  metadata is projection-backed, non-degraded, versioned, and has a supported freshness/lifecycle
  classification. Retained data is never relabelled with another ETag or projection version.
- `ServedAt`, notifications, request time, payload presence, and command state do not prove currentness.
  Missing, malformed, weak, contradictory, degraded, or non-projection evidence fails closed.
- Paginated `Items` must be non-null, and `HasMore` requires a usable continuation cursor. A generic
  HTTP 400 maps to invalid request; it is not inferred to be an invalid cursor and is not silently
  retried as page one.
- Header-stage and body-stream timeout, network, and I/O exceptions map to fixed safe results. Caller
  cancellation propagates. Response bodies and Problem Details are neither exposed nor logged.
- A transient refresh failure retains only matching prior rows, ETag, cursor context, and honest
  degraded/unknown state. A first-load failure has a real unavailable/error state.
- Route/scope generation and cancellation prevent obsolete detail, member, audit, global-admin, and
  paging completions from applying. Member cursor history is bounded, page-one recovery resets history,
  and duplicate in-flight page requests are suppressed.

## Notification producer/subscriber compatibility

The pair mapping is based on producer source, not a UI-invented alias:

- `src/Hexalith.Tenants.Server/Projections/TenantProjection.cs:9` declares
  `[EventStoreDomain("tenants")]`.
- `src/Hexalith.Tenants.Server/Projections/GlobalAdministratorProjection.cs:9` declares
  `[EventStoreDomain("global-administrators")]`.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Aggregates/EventStoreProjection.cs:248-269`
  derives the projection type from that domain and publishes
  `NotifyProjectionChangedAsync(projectionType, TenantId)`.
- The global-administrator handler accepts only tenant `system` and aggregate
  `global-administrators` (`GlobalAdministratorProjectionHandler.IsValidGlobalAdministratorIdentity`).

The consumers therefore subscribe to `tenants:{routedTenantId}` for tenant detail/member and audit,
and to `global-administrators:system` only while the global-administrator surface is authorized and
connected. List, workspace, and user-membership surfaces do not subscribe to the unroutable invented
`tenant-index:system` pair. The production/test scan for `tenant-index:system` and the literal
`"tenant-index"` in Tenants UI source and tests returned no matches.

Focused tests prove one backend subscription for identical leases, independent callback disposal,
final unsubscribe, matching-only dispatch, coalescing, optional-service no-op, late setup disposal, and
safe reason-only diagnostics. Rendered component tests prove a nonmatching notification causes no read,
a matching tenant notification makes both direct reads with their independent prior ETags, confirmed
member rows remain visible while refreshing, and the final authoritative rows render. The privileged
surface test proves an unauthorized page never calls `SubscribeAsync("global-administrators", "system")`.

## Support and composition safety

- Bearer relay and service discovery are attached only to the server-side configured Tenants client.
  Executed composition tests observe the real outbound `Authorization: Bearer` header when relay is
  enabled and its absence when disabled.
- `Tenants:BaseAddress` and `EventStore:BaseAddress` are registered independently, including the two
  single-dependency and missing-dependency cases.
- Snapshot diagnostics expose bounded state/count flags, not route identities, ETags, versions,
  cursors, payloads, tokens, raw headers, response bodies, correlations, or stack traces.
- EN/FR copy and accessible loading, stale, degraded, unknown, unavailable, empty, visible-page count,
  and action-disabled states are covered by the UI regression suite.

## Verification record

The dependency gitlinks in the final working tree are the story baseline values:

```text
references/Hexalith.Builds     53d53ae42abf7c87d385a078ab260531480bbf8a
references/Hexalith.EventStore 5a1d277ec0583e304986488d299eb3e6e5022487
references/Hexalith.Memories   1868c8f94ca1ec723a30b256a29c7c8495bc8cca
```

Project-scoped restores were serialized (`-m:1 -nr:false`) immediately before the focused
`--no-restore` commands because the current solution restore generates a conflicting source-reference
asset graph. Results on the final implementation were:

| Command | Result |
| --- | --- |
| `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --no-restore` | **PASS — 1,373 passed, 0 failed, 0 skipped** |
| `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --no-restore --filter FullyQualifiedName~TenantsApiGeneratedControllerTests` | **PASS — 26 passed, 0 failed, 0 skipped** |
| `dotnet test Hexalith.Tenants.slnx --no-restore` | **BLOCKED before test execution — `SOLUTION-GRAPH-1` below** |
| `rg -n "SubmitQueryAsync\|/api/v1/queries\|QueryRouter\|HandlerAwareQueryRouter" src/Hexalith.Tenants.UI` | **PASS — no matches** |
| `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-1-10-direct-tenants-reads-and-authoritative-freshness.md` | **PASS — all three gitlinks match `baseline_commit`** |
| `git diff --check` and `git diff --cached --check` | **PASS** |

The generated-controller lane is executable `PLAT-FRESH-1` evidence for all six route/controller and
response-header behaviors. The direct-client suite additionally exercises exact route/query
construction, dot-only escaping, real bearer composition, 200/304/empty/401/403/404/5xx, other-2xx
rejection, no/different/exact validator handling, oversized/weak/malformed/contradictory metadata,
null page items, missing continuation cursor, `ServedAt` independence, header/body failures, caller
cancellation, and safe failure categories.

## Open evidence and environment blockers

### HOST-REF-1 — live composing host

This evidence remains open and is not claimed closed. The transitional repository AppHost's
`tenants-ui` resource references EventStore and Memories and sets their base addresses at
`src/Hexalith.Tenants.AppHost/Program.cs:135-144`; it does not reference the Tenants API or supply
`Tenants:BaseAddress`. The AppHost is unchanged from the story baseline, as required. Hosted smoke tests
prove the resulting read surfaces fail closed with no EventStore query fallback, but no authenticated
live-host direct REST call is claimed. The composing-host owner must provide the Tenants service
reference and configuration before that runtime proof can be collected.

### SOLUTION-GRAPH-1 — solution asset-graph mismatch

`dotnet restore Hexalith.Tenants.slnx -m:1 -nr:false` succeeds (3 projects restored, 42 up to date), but
the required subsequent `dotnet test Hexalith.Tenants.slnx --no-restore` fails during compilation before
any tests run. The solution restore writes source-project entries such as
`src/Hexalith.Tenants.Contracts/obj/project.assets.json` with project/placeholder assets while normal
evaluation has `UseHexalithProjectReferences=false` and expects package assemblies. The result is the
same large set of missing EventStore contract/type errors on repeated and serialized test attempts.

The permitted source-reference diagnostic fallback
`dotnet test Hexalith.Tenants.slnx --no-restore -p:UseHexalithProjectReferences=true -m:1 -nr:false`
also fails before tests for an independent dependency conflict: compiler `CS1704` reports duplicate
`Hexalith.Commons.UniqueIds` assemblies (the source graph exposes version 1.0.0/3.83.0 dependencies
alongside package version 2.29.0), followed by downstream `MSB4181` failures. No dependency, build,
solution, or gitlink change is authorized by Story 1.10, so this owned external/build-graph blocker is
recorded rather than worked around. Direct project restores select the intended package asset and are
the prerequisite used for the passing focused UI and PLAT-FRESH-1 gates above.
