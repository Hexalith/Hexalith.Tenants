# Story 1.10 Evidence — Direct Tenants Reads and Authoritative Freshness

Date: 2026-07-28  
Story baseline commit: `8d64563c75423c861b0be0e3a7cc4de18f673d37`  
Repository revision under the working tree: `7d7b701` + uncommitted working-tree changes

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

## Hosted fail-closed route regression (found and fixed 2026-07-28)

The full `IntegrationTests` lane — which the earlier record never ran, having filtered to
`TenantsApiGeneratedControllerTests` — failed two tests. Both were real, both are fixed.

**`TenantsUiRouteSmokeTests.Global_administrators_route_renders_fail_closed_unavailable_state_in_hosted_ui`
answered HTTP 500 instead of the required fail-closed render.** The story had added
`@attribute [Authorize(Policy = …)]` to `GlobalAdministratorsPage.razor` and an `AuthorizeRouteView`
wrapper to `Routes.razor`. That attribute is the only endpoint authorization metadata in the module, and
it makes `WebApplication` insert the authorization middleware. The host calls `AddAuthentication` /
`UseAuthentication` only when OIDC is configured, so on the Keycloak-disabled topology the middleware's
challenge path threw:

```text
System.InvalidOperationException: Unable to find the required 'IAuthenticationService' service.
  at Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions.ChallengeAsync(HttpContext)
  at Microsoft.AspNetCore.Authorization.AuthorizationMiddleware.Invoke(HttpContext)
```

Reproduced out of band against the standalone host (`/global-administrators` → 500 while `/tenants` →
200), so the diagnosis is a captured stack trace, not an inference. Platform authority is now enforced
solely by the page's rendered fail-closed state — the mechanism the acceptance criteria and the
component tests already exercise — and the unreachable `AuthorizeRouteView` fragments were removed.
`TenantsUiCompositionTests.Routable_components_fail_closed_in_page_without_endpoint_authorization_metadata`
pins the invariant in the fast lane so the Aspire lane is no longer the only detector. It was confirmed
red against the defect before the fix landed.

Removing the 500 exposed a second, independent break in the same route: the story's new restricted
branch replaced the whole page, dropping the `tenants-global-admins-area` container and the
`tenants-global-admins-live-region` announcement element that the unauthorized state published before
this story. The spec's KEEP list preserves existing accessibility work, so the restricted branch now
nests inside the page area and carries the live region, while keeping the story's
`tenants-global-admins-denied-message` contract. The restricted copy is unchanged, so both the
component test asserting "fails closed" and the hosted contract asserting "Platform area unavailable" /
"The area fails closed" hold against one rendering. The message text appears once, inside the live
region within the alert region — it is not duplicated for screen readers.

**`AspireTopologyTests.Aha_moment_demo_revokes_sample_access_from_tenant_events` asserted superseded
audit semantics.** It still expected `Degraded` / `MissingPayload` for a first load that returns no
payload. Review repair loop 1 deliberately changed that to a true error state, which
`Get_tenant_audit_maps_missing_payload_without_retained_rows_to_safe_error_state` already pins. The
assertion was updated to `Error` / `GatewayFailure`; its essential invariant — empty rows, unknown
freshness and lifecycle, no correction-eligible evidence — is unchanged.

## Verification record

The dependency gitlinks were **not** at baseline when this session started, contrary to the earlier
version of this record. Four pointers had moved inside the story range and none belonged to Story 1.10
(dependency version bumps, a release-workflow edit, a lane script, and OpenBao documentation tests):

```text
references/Hexalith.Builds        53d53ae -> 86aa4cb   REVERTED
references/Hexalith.EventStore    5a1d277 -> 589da8b   REVERTED
references/Hexalith.FrontComposer 7870526 -> b6efcad   REVERTED
references/Hexalith.Memories      1868c8f -> 115d30b   REVERTED
```

All four were restored to `baseline_commit` per the story's KEEP constraint, and every result below was
produced against that restored tree. The `Hexalith.Builds` revert also restores
`HexalithMemoriesVersion` to `2.16.2` and `HexalithTenantsVersion` to `3.2.18`; the full gate passes on
those baseline versions. The final gitlinks are:

```text
references/Hexalith.Builds        53d53ae42abf7c87d385a078ab260531480bbf8a
references/Hexalith.EventStore    5a1d277ec0583e304986488d299eb3e6e5022487
references/Hexalith.FrontComposer 7870526090a8596082e3df034ecacf4c07881a04
references/Hexalith.Memories      1868c8f94ca1ec723a30b256a29c7c8495bc8cca
```

Project-scoped restores were serialized and forced (`-m:1 -nr:false --force`) immediately before each
focused `--no-restore` command. That is required, not cosmetic: the `IntegrationTests` graph reaches the
AppHost and restores shared source projects in source-reference mode, while every other lane evaluates
in package mode, so the two cannot share `obj` state. Results on the final tree were:

| Command | Result |
| --- | --- |
| `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --no-restore` | **PASS — 1,416 passed, 0 failed, 0 skipped** |
| `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --no-restore --filter FullyQualifiedName~TenantsApiGeneratedControllerTests` | **PASS — 26 passed, 0 failed, 0 skipped** |
| `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --no-restore --filter "Category!=Performance"` | **PASS — 167 passed, 0 failed, 0 skipped** |
| `dotnet test Hexalith.Tenants.slnx --no-restore` | **PASS — 2,711 passed, 1 skipped (`Category=Performance`), 0 failed, 0 warnings** |
| Regression lanes: Contracts / Client / Testing / Server / Sample | **PASS — 120 / 50 / 181 / 738 / 39, 0 failed** |
| `rg -n "SubmitQueryAsync\|/api/v1/queries\|QueryRouter\|HandlerAwareQueryRouter" src/Hexalith.Tenants.UI` | **PASS — no matches** |
| `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-1-10-direct-tenants-reads-and-authoritative-freshness.md` | **PASS — exit 0 against the final tree, after the four reverts above** |
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

### SOLUTION-GRAPH-1 — CLOSED, was an `obj`-state effect

This was previously recorded as an unowned build-graph blocker that stopped
`dotnet test Hexalith.Tenants.slnx --no-restore` before any test ran. It does not reproduce. On the
final tree the solution lane passes outright: **2,711 passed, 1 skipped, 0 failed, 0 warnings**, the
skip being the `Category=Performance` test that runs only on the nightly schedule.

The failure was a stale-`obj` artifact, not a property of the solution. Anything that restores the
AppHost graph — the `IntegrationTests` project, or a bare `dotnet run` on a UI project, which restores
implicitly even with `--no-build` — rewrites shared entries such as
`src/Hexalith.Tenants.Contracts/obj/project.assets.json` in source-reference mode. Package-mode
evaluation (`UseHexalithProjectReferences=false`) then cannot resolve the EventStore contract types, and
every subsequent lane fails with the same wall of `CS0234`/`CS0246` errors until the assets are rebuilt.
It was reproduced and cleared twice in this session by exactly that mechanism.

The remedy is a forced package-mode restore per project (`dotnet restore <project> -m:1 -nr:false
--force`) before the `--no-restore` lane, which also leaves the subsequent solution restore a no-op
(`44 of 45 projects are up-to-date`). The `CS1704` duplicate-`Hexalith.Commons.UniqueIds` conflict on
the `-p:UseHexalithProjectReferences=true` diagnostic fallback is a separate, still-real source-mode
conflict; it is not on any required path, since the package-mode solution lane now passes.

## File list — closing session (2026-07-28)

Changes made after the earlier record, all uncommitted in the working tree:

| Path | Change |
| --- | --- |
| `references/Hexalith.Builds` | Gitlink reverted to `baseline_commit` (`53d53ae`) |
| `references/Hexalith.EventStore` | Gitlink reverted to `baseline_commit` (`5a1d277`) |
| `references/Hexalith.FrontComposer` | Gitlink reverted to `baseline_commit` (`7870526`) |
| `references/Hexalith.Memories` | Gitlink reverted to `baseline_commit` (`1868c8f`) |
| `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor` | Removed the `[Authorize]` endpoint attribute; restricted branch nests in the page area and republishes the live region |
| `src/Hexalith.Tenants.UI/Components/Routes.razor` | Reverted to `RouteView`; the `AuthorizeRouteView` fragments were unreachable and untested |
| `tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs` | Added `Routable_components_fail_closed_in_page_without_endpoint_authorization_metadata` |
| `tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs` | Route contract now asserts the absence of authorize metadata |
| `tests/Hexalith.Tenants.IntegrationTests/AspireTopologyTests.cs` | Audit assertion moved to the first-load `Error`/`GatewayFailure` truth |
| `tests/test-summary.md` | Story 1.10 totals, the two fixed regressions, and the closed `SOLUTION-GRAPH-1` |
| `_bmad-output/implementation-artifacts/story-1-10-…-evidence-2026-07-28.md` | This record |

The four gitlink reverts are staged; everything else is unstaged. No `references/` submodule *content*
was modified — only the superproject pointers, and only back to baseline.
