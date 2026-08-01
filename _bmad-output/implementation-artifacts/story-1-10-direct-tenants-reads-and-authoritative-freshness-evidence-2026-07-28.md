# Story 1.10 Evidence — Direct Tenants Reads and Authoritative Freshness

Date: 2026-07-28  
Story baseline commit: `8d64563c75423c861b0be0e3a7cc4de18f673d37`  
Repository revision originally reviewed: `09947a2` + completion metadata changes
Current review base: `cfd5b67` + the 2026-08-01 chunk-C repair tree recorded at the end of this file

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

The six path-building sites are lines 52, 71, 91, 113, 135, and 167 (corrected in review loop 4; the
previously recorded 39/53/68/85/102/120 no longer resolved to the code they described). Literal
route identities and query values are escaped after all-dot and separator-bearing route identifiers are
rejected; those unsafe values are never transported. The client accepts only HTTP/HTTPS base addresses,
does not follow redirects, accepts payloads only from the exact route's `200 OK`, and conditionally retains
only from `304 Not Modified`. Every 3xx and every other 2xx is rejected.

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
  classification. Retained data is never relabelled with another ETag. Its projection version is retained
  too, except for the approved global-administrator exact-validator case: only there may supported `304`
  metadata advance the retained snapshot's projection version and recompute completeness.
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

- Bearer relay is attached only to the server-side configured Tenants client. Service discovery is
  deliberately not attached: AppHost supplies an already-resolved plain HTTP/HTTPS endpoint, and compound
  discovery schemes fail closed on their owning side.
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
200), so the diagnosis is a captured stack trace, not an inference. The static component attribute and the
unreachable `AuthorizeRouteView` fragments were removed. The final topology is conditional: without OIDC,
the page's rendered fail-closed state remains reachable; with OIDC, `Program.cs` attaches the global-
administrator policy to that component endpoint as defence in depth. The fast-lane composition theory
materializes the hosted endpoint in both topologies and asserts both halves of that pairing.

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

> **CORRECTED 2026-07-29 (review loop 3).** The paragraphs below previously stated that four gitlinks had
> been reverted to `baseline_commit` and that the gitlink validator exited 0 against the final tree. Both
> statements were false against the reviewed tree. The pointers were never reverted; published commits
> `f425b49` and `96bdfd8` moved five pointers in the final story range. Two of the end-SHAs this record
> originally named (`589da8b`, `115d30b`) do not exist in the tree at all, so the initial verification
> section was not written against the tree it describes.

> **SUPERSEDED — historical record only (disclaimer added by review loop 13, 2026-08-01).**
> Every `references/` pointer table in this evidence file records the tree **as it stood when that section
> was written**, and later commits moved several of those pointers again. Five target SHAs below and in the
> sibling tables further down are false against the current tree. The authoritative table is the one in
> `spec-1-10-…md`'s File List, which at the time of writing reads `Builds 53d53ae -> b529b66`,
> `EventStore 5a1d277 -> e4618d9`, `Memories 1868c8f -> a1f64d5`, with `AI.Tools`, `Commons` and
> `FrontComposer` unchanged.
>
> This disclaimer exists because the guard cannot substitute for it: decision D-H deliberately scopes
> `stated_targets` to the *spec's* File List and Completion Notes, and the mandated validator command targets
> the spec, so **nothing checks the tables in this file**. Story prose about gitlinks has twice been false in
> this repository (stories 1.4 and 1.6), which is precisely why an unmarked stale table here is a hazard.
> Verify against `python3 scripts/validate-story-gitlinks.py <spec>` and `git ls-tree HEAD references/`,
> never against the tables below.

The dependency gitlinks were **not** at baseline when this session started. Five pointers moved inside the
final story range. They are dependency/workflow/documentation provenance carried by published commits,
not review-loop-3 runtime implementation:

```text
references/Hexalith.Builds        53d53ae -> 86aa4cb   DECLARED (not reverted)
references/Hexalith.Commons       427530e -> f2b5f1b   DECLARED (not reverted)
references/Hexalith.EventStore    5a1d277 -> b1d08da   DECLARED (not reverted)
references/Hexalith.FrontComposer 7870526 -> b6efcad   DECLARED (not reverted)
references/Hexalith.Memories      1868c8f -> fc92c4d   DECLARED (not reverted; user-owned working pointer)
```

They are declared in the spec's File List and Completion Notes List rather than reverted. The committed
movements live in published commits, and the additional Memories working pointer pre-dated this loop and
is user-owned. All five SHAs are reachable on their respective `origin/main`, so the superproject remains
cloneable. The actual gitlinks in the final reviewed working tree are:

```text
references/Hexalith.Builds        86aa4cbdee5e6b3f94d3ec2d95c85fa9593e64bd
references/Hexalith.Commons       f2b5f1b12b478dce902756876138a60cde4fde65
references/Hexalith.EventStore    b1d08dac328ee6a2f9b4ef07a1a14ad5756ba94e
references/Hexalith.FrontComposer b6efcad5b293017f9805e4fc7dc982b92abff678
references/Hexalith.Memories      fc92c4d8ac63601cbc01741bd92b91ee7e6bcdfe
```

**Consequence for every result below.** The `Hexalith.Builds` pointer at HEAD sets
`HexalithMemoriesVersion 2.19.4` and `HexalithTenantsVersion 5.0.0`, whereas the table below was produced
against `2.16.2` / `3.2.18`. A major-version bump of Tenants sits between the recorded evidence and the
reviewed tree, so the lanes marked PASS below are **not** evidence for this commit and must be re-run.
Only the UI lane has been re-executed at HEAD (1,416 passed, 0 failed).

Project-scoped restores were serialized and forced (`-m:1 -nr:false --force`) immediately before each
focused `--no-restore` command. That is required, not cosmetic: the `IntegrationTests` graph reaches the
AppHost and restores shared source projects in source-reference mode, while every other lane evaluates
in package mode, so the two cannot share `obj` state. Results on the final tree were:

| Command | Result | Valid at HEAD? |
| --- | --- | --- |
| `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --no-restore` | **PASS — 1,416 passed, 0 failed, 0 skipped** | **Yes** — re-executed at HEAD during review loop 2 |
| `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --no-restore --filter FullyQualifiedName~TenantsApiGeneratedControllerTests` | PASS — 26 passed, 0 failed, 0 skipped | **No** — ran against `3.2.18`/`2.16.2` pins; must re-run |
| `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --no-restore --filter "Category!=Performance"` | PASS — 167 passed, 0 failed, 0 skipped | **No** — must re-run at `5.0.0`/`2.19.4` |
| `dotnet test Hexalith.Tenants.slnx --no-restore` | PASS — 2,711 passed, 1 skipped (`Category=Performance`), 0 failed, 0 warnings | **No** — must re-run at `5.0.0`/`2.19.4` |
| Regression lanes: Contracts / Client / Testing / Server / Sample | PASS — 120 / 50 / 181 / 738 / 39, 0 failed | **No** — must re-run at `5.0.0`/`2.19.4` |
| `rg -n "SubmitQueryAsync\|/api/v1/queries\|QueryRouter\|HandlerAwareQueryRouter" src/Hexalith.Tenants.UI` | **PASS — no matches** | **Yes** — source-only check, re-confirmed |
| `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-1-10-direct-tenants-reads-and-authoritative-freshness.md` | **PASS — exit 0** | **Yes** — passes on a genuine File List declaration of the four pointers, not on a revert |
| `git diff --check` and `git diff --cached --check` | **PASS** | **Yes** |

The generated-controller lane is executable `PLAT-FRESH-1` evidence for all six route/controller and
response-header behaviors. The direct-client suite additionally exercises exact route/query
construction, dot-only escaping, real bearer composition, 200/304/empty/401/403/404/5xx, other-2xx
rejection, no/different/exact validator handling, oversized/weak/malformed/contradictory metadata,
null page items, missing continuation cursor, `ServedAt` independence, header/body failures, caller
cancellation, and safe failure categories.

## Review loop 2 — 2026-07-28 (code review of Story 1.10)

Every lane below was executed on the reviewed tree, at the current `references/` pointers
(`HexalithTenantsVersion 5.0.0`, `HexalithMemoriesVersion 2.19.4`) — not the baseline pins the table above
was produced against.

| Command | Result |
| --- | --- |
| `dotnet build Hexalith.Tenants.slnx -c Release -m:1 -nr:false -warnaserror` | **PASS — 0 warnings, 0 errors** |
| `dotnet test Hexalith.Tenants.slnx -m:1 -nr:false` | **PASS — 2,726 passed, 1 skipped (`Category=Performance`), 0 failed** |
| `dotnet test tests/Hexalith.Tenants.UI.Tests` | **PASS — 1,431 passed, 0 failed** |
| `dotnet test tests/Hexalith.Tenants.IntegrationTests --filter "Category!=Performance"` | **PASS — 167 passed, 1 skipped, 0 failed** |
| Regression lanes: Contracts / Client / Testing / Server / Sample | **PASS — 120 / 50 / 181 / 738 / 39, 0 failed** |
| `python3 scripts/validate-story-gitlinks.py <spec>` | **PASS — exit 0 on a File List declaration of the four pointers** |

`HOST-REF-1` is **CLOSED**. The AppHost `tenants-ui` resource now references `tenants-api` and sets
`Tenants__BaseAddress` (`src/Hexalith.Tenants.AppHost/Program.cs`). This required retiring
`EventPublicationConfigurationTests`' `program.ShouldNotContain("Tenants__BaseAddress")` assertion, which
described the pre-1.10 architecture; left standing it would have kept the read transport unreachable in
every deployment while CI stayed green. Wiring the host was an explicit owner decision that waives this
story's "do not edit AppHost" constraint.

Closing it changed four hosted smoke tests: with a composed read surface and an unauthenticated request the
routes render authorization-safe *unauthorized* states rather than "read surface unavailable". The tests
were renamed and re-pointed at the unauthorized markers, which is the distinction the acceptance criteria
require (empty, unauthorized, not-found, error and degraded must stay distinct).

Two review findings were **rejected as false positives** after being checked against existing pinned
behavior, and the corresponding changes were reverted:

- *"The 304 branch must not relabel retained rows with the response projection version."*
  `Get_global_administrators_current_not_modified_promotes_unknown_truth_and_recomputes_completeness`
  asserts the promotion, and it is sound: `IsSupportedNotModified` already requires a strong exact ETag
  match with projection-backed, versioned, non-degraded metadata, so the 304 proves the retained payload is
  identical to what the service holds at that newer version.
- *"The grant path is missing a completeness gate."*
  `Incomplete_current_page_with_more_results_allows_safe_initiation` pins that grant is deliberately
  permitted on incomplete evidence. Granting cannot remove the last administrator, so it needs no
  population evidence; only removal does. The unused `Grant.Unavailable.Incomplete` resource key was dead
  copy and was removed from both `.resx` files (EN/FR parity re-verified: 1,206 / 1,206, zero one-sided).

## Historical evidence and environment blockers

### HOST-REF-1 — CLOSED in review loop 2

The original blocker text stated that the AppHost did not reference the Tenants API. That state is
superseded by review loop 2: the owner-approved AppHost change wires `tenants-ui` to `tenants-api` and
sets `Tenants__BaseAddress`, as recorded at lines 227-237 above. HOST-REF-1 is closed and is not an open
Story 1.10 evidence limitation.

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
| `references/Hexalith.Builds` | Gitlink moved `53d53ae -> 86aa4cb` by `f425b49`; **declared**, not reverted |
| `references/Hexalith.Commons` | Historical table note: later moved `427530e -> f2b5f1b` by published commit `96bdfd8`; **declared**, not reverted |
| `references/Hexalith.EventStore` | Gitlink moved `5a1d277 -> 7ab1f08` by `f425b49`; **declared**, not reverted |
| `references/Hexalith.FrontComposer` | Gitlink moved `7870526 -> b6efcad` by `f425b49`; **declared**, not reverted |
| `references/Hexalith.Memories` | Gitlink moved `1868c8f -> a451765` by `f425b49`; **declared**, not reverted |
| `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor` | Removed the `[Authorize]` endpoint attribute; restricted branch nests in the page area and republishes the live region |
| `src/Hexalith.Tenants.UI/Components/Routes.razor` | Reverted to `RouteView`; the `AuthorizeRouteView` fragments were unreachable and untested |
| `tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs` | Added `Routable_components_fail_closed_in_page_without_endpoint_authorization_metadata` |
| `tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs` | Route contract now asserts the absence of authorize metadata |
| `tests/Hexalith.Tenants.IntegrationTests/AspireTopologyTests.cs` | Audit assertion moved to the first-load `Error`/`GatewayFailure` truth |
| `tests/test-summary.md` | Story 1.10 totals, the two fixed regressions, and the closed `SOLUTION-GRAPH-1` |
| `_bmad-output/implementation-artifacts/story-1-10-…-evidence-2026-07-28.md` | This record |

No `references/` submodule *content* was modified — only the superproject pointers. Those pointers were
moved forward (not back to baseline) by the separate published commit `f425b49`, and are declared in the
spec's File List and Completion Notes List. The gitlink validator exits 0 on that declaration.

## Review loop 3 — 2026-07-29 (development of remaining review patches)

This loop closed all 13 test-efficacy patches left by review loop 2. Each assertion was first shown red
against a temporary mutation of the behavior it guards and then green after restoration. The mutations
covered direct failure mapping, snapshot kind/reason/scope state, authentication transition attachment and
rendering, recovery selectors, audit lease/generation guards, member invalid-cursor recovery and duplicate
suppression, independent request/response ETag limits, body-phase and resolver mid-flight cancellation,
exception-free logging, direct typed-client query arguments, authentication teardown, and canonical return
URL keys.

One production defect was exposed: a background global-administrator authorization restore applied the
new snapshot but did not publish a render. `GlobalAdministratorsPage` now invokes `StateHasChanged()` after
that background result is accepted. All other changes in this loop are executable test/evidence artifacts.

Every test project was restored and tested individually as required by the repository baseline. The
package-mode projects used this exact restore shape immediately before their Release test:

```text
dotnet restore <project.csproj> -p:Configuration=Release -p:UseHexalithProjectReferences=false -p:NuGetAudit=false -m:1 -nr:false --verbosity minimal
dotnet test <project.csproj> --configuration Release --no-restore -p:UseHexalithProjectReferences=false -p:NuGetAudit=false -m:1 -nr:false --verbosity minimal
```

The authoritative final results are:

| Lane / exact command | Result |
| --- | --- |
| `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-restore -p:UseHexalithProjectReferences=false -p:NuGetAudit=false -m:1 -nr:false --verbosity minimal` | **PASS — 1,452 passed, 0 failed, 0 skipped** |
| `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Release --no-restore -p:UseHexalithProjectReferences=false -p:NuGetAudit=false -m:1 -nr:false --verbosity minimal --filter FullyQualifiedName~TenantsApiGeneratedControllerTests` | **PASS — 26 passed, 0 failed, 0 skipped** |
| `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Release --no-build --no-restore -p:UseHexalithProjectReferences=false -p:NuGetAudit=false -m:1 -nr:false --verbosity minimal --filter "Category!=Performance"` | **PASS — 167 passed, 0 failed, 0 skipped** |
| Contracts / Client / Testing / Server / Sample, each via the package-mode command above | **PASS — 120 / 50 / 181 / 738 / 39; 0 failed, 0 skipped** |
| `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -p:UseHexalithProjectReferences=false -p:NuGetAudit=false -warnaserror -m:1 -nr:false --verbosity minimal` | **PASS — 0 warnings, 0 errors** |
| `rg -n "SubmitQueryAsync\|/api/v1/queries\|QueryRouter\|HandlerAwareQueryRouter" src/Hexalith.Tenants.UI` | **PASS — no matches (expected exit 1)** |
| `rg -n 'tenant-index:system\|"tenant-index"' src/Hexalith.Tenants.UI tests/Hexalith.Tenants.UI.Tests` | **PASS — no matches (expected exit 1)** |
| `git diff --check` and `git diff --cached --check` | **PASS** |
| `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-1-10-direct-tenants-reads-and-authoritative-freshness.md` | **PASS — exit 0; all moved pointers declared** |

`references/Hexalith.Memories` currently has an additional unstaged, user-owned pointer movement from the
published `a451765` gitlink to `fc92c4d`. It pre-dated review-loop-3 work, was not modified or reverted,
and remains explicitly declared in the story File List. No dependency submodule content was changed.

### File list — review loop 3 (2026-07-29)

- `_bmad-output/implementation-artifacts/spec-1-10-direct-tenants-reads-and-authoritative-freshness.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/story-1-10-direct-tenants-reads-and-authoritative-freshness-evidence-2026-07-28.md`
- `references/Hexalith.Commons` (published commit-range pointer, declared)
- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor`
- `tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Configuration/TenantConfigurationReadPolicyTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantsRestQueryClientTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/TenantReadRefreshSubscriptionTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/State/TenantUsersSnapshotTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs`
- `tests/test-summary.md`
- `references/Hexalith.Memories` (pre-existing user-owned gitlink movement, preserved)

## Completion revalidation — 2026-07-29

The published `09947a2` dependency commit advanced `references/Hexalith.Memories` after review-loop-3
validation. The completion workflow therefore treated every earlier result as historical and re-ran the
required lanes against `09947a2` plus the final documentation/status changes, using .NET SDK `10.0.302`.

Each test project was restored and tested individually in Release package mode with
`UseHexalithProjectReferences=false`, `NuGetAudit=false`, serialized MSBuild, and `--no-restore` on the
test command. The generated-controller proof and the complete non-performance integration lane were run
separately from the same freshly built integration assembly.

| Lane | Result |
| --- | --- |
| UI | **PASS — 1,507 passed, 0 failed, 0 skipped** |
| Generated-controller integration | **PASS — 26 passed, 0 failed, 0 skipped** |
| IntegrationTests (`Category!=Performance`) | **PASS — 167 passed, 0 failed, 0 skipped** |
| Contracts | **PASS — 120 passed, 0 failed, 0 skipped** |
| Client | **PASS — 50 passed, 0 failed, 0 skipped** |
| Testing | **PASS — 181 passed, 0 failed, 0 skipped** |
| Server | **PASS — 738 passed, 0 failed, 0 skipped** |
| Sample | **PASS — 39 passed, 0 failed, 0 skipped** |
| Release solution build (`-warnaserror`) | **PASS — 0 warnings, 0 errors** |
| Generic-query scan | **PASS — no matches (expected `rg` exit 1)** |
| Invented tenant-index scan | **PASS — no matches (expected `rg` exit 1)** |
| Staged and unstaged whitespace checks | **PASS** |
| `validate-story-gitlinks.py` | **PASS — exit 0; all six moved pointers declared** |

The gitlink validator compared story baseline `8d64563` to `09947a2` plus the completion tree and reported:

```text
[declared] references/Hexalith.AI.Tools  991e8ea -> 859d53b
[declared] references/Hexalith.Builds  53d53ae -> 13cad86
[declared] references/Hexalith.Commons  427530e -> f2b5f1b
[declared] references/Hexalith.EventStore  5a1d277 -> 1d42528
[declared] references/Hexalith.FrontComposer  7870526 -> b6efcad
[declared] references/Hexalith.Memories  1868c8f -> ccd8efa
```

At this checkpoint no live `HOST-REF-1`, dependency, test, build, support-safety, or gitlink blocker
remained. The File List then excluded Story 1.11 and unrelated BMAD render files. Later review loops
superseded that boundary for range provenance: the authoritative current File List deliberately declares
the Story 1.11 spec and three BMAD render artifacts while stating that they do not carry Story 1.10 runtime
behavior. This dated paragraph must not be read as the final ownership boundary.

## Review loop 6 — open-decision closure (2026-07-29)

Scoped run: only the two `[Review][Decision]` items left unchecked by review loop 4 were in scope. No new
adversarial layers were run over the diff.

**D-A source verification, before the owner call.** Two claims in the original finding were checked and one
was corrected:

| Claim under test | Result |
| --- | --- |
| `MemberAccessReview.ActionsAreEvidenceBacked` is exposed by lifecycle-less `Current` | **False.** `MemberAccessReview.razor:407` already requires `Lifecycle is ProjectionLifecycleState.Current` on both the detail and tenant-users snapshots. Only the global-administrator surface was exposed. |
| The exposure is reachable against the owned server | **False.** `RestApiControllerEmitter.cs:464-480` derives `X-Hexalith-Is-Stale` from `ProjectIsStale(lifecycle, …)` and emits the lifecycle header whenever lifecycle is known; all six handlers build metadata via `ToQueryResponseMetadata` (`TenantQueryResult.cs:50`), where `IsStale: false` implies `Lifecycle: Current`. A lifecycle-less `Current` can only come from a non-conforming producer. |

**Mutation verification of the D-A patch.** Green alone is not evidence; each guard was removed and the
suite re-run:

| Mutation applied | Result |
| --- | --- |
| `IsMutationEvidenceBacked` reverted to `Freshness is Current` | **5 failures** — page grant/remove test, `ConfirmProjection` test, and 3 matrix cases (`Current`+`Unknown`, `Current`+`Stale`, `Current`+`Degraded`) |
| Only the two page gates reverted, predicate kept | **1 failure** — the rendered page test, proving the page consumes the predicate rather than the predicate being tested in isolation |
| Both mutations reverted | **0 failures** |

**Commands and results:**

| Check | Result |
| --- | --- |
| `dotnet build tests/Hexalith.Tenants.UI.Tests/... --configuration Release -warnaserror -m:1 -nr:false` | **PASS — 0 warnings, 0 errors** |
| `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` | **PASS — 1,553 passed, 0 failed, 0 skipped** (1,544 before this loop, +9 new) |
| `dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false` | **PASS — 0 warnings, 0 errors** |
| `grep -rn "SubmitQueryAsync\|/api/v1/queries\|QueryRouter\|HandlerAwareQueryRouter" src/Hexalith.Tenants.UI` | **PASS — no matches** |
| `python3 scripts/validate-story-gitlinks.py <1.10 spec>` | **PASS — exit 0; all six moved pointers declared** |
| `python3 scripts/validate-story-gitlinks.py <1.11 spec>` | **PASS — exit 0** (was **FAIL — exit 1, all six UNDECLARED** before this loop; see below) |

Not re-run, and why: the change is confined to `src/Hexalith.Tenants.UI`. Contracts, Client, Server, Testing,
Sample and the integration lanes have no dependency on the edited files, and no public contract, query
contract, command/status route, resource string or dependency pointer changed in this loop.

**Pre-existing blocker surfaced and closed by D-B.** Story 1.11 had no `## File List` section at all, so its
gitlink validator exited 1 with all six `references/` pointers UNDECLARED — a live blocker for a story
already sitting in `review`. Its baseline (`2e61f57`) is inside Story 1.10's range, so it inherits the same
six movements from the published dependency commits `f425b49` and `09947a2`. The re-cut File List declares
them with an explicit cross-reference to Story 1.10, which owns the range; the validator now exits 0.

## Review repair loop 12 — 2026-08-01

This loop closes all 30 findings recorded after loop 11. The production repairs preserve independent
command leases, prevent a dismissed configuration command from reacquiring activity, retain paging-recovery
announcements until a later successful refresh, and clear audit cursor history on the dispatcher before a
concurrent Previous operation can pop it. The accompanying regressions also pin retry-budget resets,
one-shot focus, subscription disposal, factory descriptors, configuration diagnostic aggregation, and the
existing metadata, accessibility, route-validation, and support-safety contracts.

### Final executable evidence

| Lane / exact command shape | Result |
| --- | --- |
| `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -warnaserror -m:1 -nr:false --verbosity minimal` | **PASS — 0 warnings, 0 errors** |
| `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release` | **PASS — 1,820 passed, 0 failed, 0 skipped** |
| Contracts / Client / Testing / Server / `samples/Hexalith.Tenants.Sample.Tests`, from the Release build with `--no-build --no-restore` | **PASS — 120 / 50 / 181 / 738 / 39; 0 failed** |
| IntegrationTests, `--filter FullyQualifiedName~TenantsApiGeneratedControllerTests` | **PASS — 27 passed, 0 failed, 0 skipped** |
| IntegrationTests, `--filter FullyQualifiedName!~AspireTopologyTests` | **PASS — 162 passed, 0 failed, 1 skipped, 163 total**; only the performance test is skipped |
| IntegrationTests, `--filter "Category!=Performance"` | **PARTIAL — 166 passed, 2 failed, 0 skipped, 168 total**; both failures are 60-second Aspire topology HTTP timeouts |
| EN/FR resource-key comparison | **PASS — 1,228 keys on each side, no difference** |
| `rg -n "SubmitQueryAsync|/api/v1/queries|QueryRouter|HandlerAwareQueryRouter" src/Hexalith.Tenants.UI` | **PASS — no matches (expected exit 1)** |
| `rg -n 'tenant-index:system|"tenant-index"' src/Hexalith.Tenants.UI tests/Hexalith.Tenants.UI.Tests` | **PASS — no matches (expected exit 1)** |
| `git diff --check` and `git diff --cached --check` | **PASS** |

The full non-performance integration failures were:

- `AspireTopologyTests.Tenants_resource_reports_ready_only_after_prepared_dependencies_are_available`
- `AspireTopologyTests.Aha_moment_demo_revokes_sample_access_from_tenant_events`

Both timed out at the configured 60-second HTTP bound. The same tree passes the generated-controller lane
27/27 and the non-Aspire integration lane 162/162 (plus one intentional performance skip), so the result is
kept as the already-owned local-topology evidence limitation. No live-host success is claimed. The required
AppHost-first attempt also failed to produce a resource model within its 120-second startup bound while
restore remained in progress:

```text
❌ Timed out waiting 120s for AppHost to start. If the AppHost is still building or starting, set ASPIRE_CLI_START_TIMEOUT to a higher value and try again.
```

### Gitlink-validator extension evidence

The parser was exercised directly for declaration-section scoping, last-stated-target semantics,
multi-pointer lines, decorated path tokens, chained arrows, en-dash arrows, and Unicode `Cf` format
characters. A temporary story whose baseline equaled HEAD and whose File List falsely stated
`references/Hexalith.Builds ... -> deadbee` exited 1 with:

```text
[MISSTATED] references/Hexalith.Builds is b529b66 in the tree, but the story states deadbee.
```

Decision D-K intentionally retains this as manual executable verification because the repository has no
Python test or CI infrastructure. The mandatory final-tree command is:

```text
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-1-10-direct-tenants-reads-and-authoritative-freshness.md
```

It reports all six pointer movements as `[declared]`, exits 0, and ends with the verbatim verdict:

```text
RESULT: PASS
```

## Review loop 14 — chunk-C completion repair (2026-08-01)

This section is the authoritative completion evidence for repository revision `cfd5b67` plus the chunk-C
repair tree. It supersedes the evidence file's loop-12 ending and the manual-only D-K statements above.
Review loop 13 had already reversed D-K and added the Python/CI lane, but that result existed only in the
story spec; this repair records it in both completion artifacts and extends the lane through the production
CLI rather than parser helpers alone.

The repair also closes three implementation findings:

- `/global-administrators` carries the global-administrator endpoint policy when OIDC is configured, while
  the Keycloak-disabled topology carries no endpoint authorization metadata and still renders its page-level
  fail-closed state. The hosted endpoint theory materializes and asserts both topologies.
- The Tenants typed client's actual primary `HttpClientHandler` has `AllowAutoRedirect = false`; 3xx
  responses remain fixed unavailable results and cannot retarget an exact direct read.
- A blank audit tenant identifier now produces `Error / MissingTenantId`, not an empty `Degraded` snapshot
  that falsely implies retained evidence.

The story contract and evidence were reconciled with the already-resolved final architecture: no service-
discovery handler, AppHost-resolved plain HTTP/HTTPS endpoints, rejection of all-dot/separator-bearing route
identifiers, the approved global-administrator `304` projection-version exception, per-project test lanes,
the final declared gitlink set, the refuted audit-adapter claim, and the current File List boundary.

### Exact executable evidence

| Exact command | Result |
| --- | --- |
| `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-restore -p:UseHexalithProjectReferences=false -p:NuGetAudit=false -warnaserror -m:1 -nr:false --verbosity minimal` | **PASS — 0 warnings, 0 errors** |
| `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` | **PASS — 1,844 passed, 0 failed, 0 skipped** |
| `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.TenantsUiCompositionTests` | **PASS — 82 passed, 0 failed, 0 skipped** |
| `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.Services.Gateways.TenantsRestQueryClientTests` | **PASS — 118 passed, 0 failed, 0 skipped** |
| `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.Services.Gateways.TenantQueryGatewayTests` | **PASS — 393 passed, 0 failed, 0 skipped** |
| `python3 tests/scripts/test_validate_story_gitlinks.py` | **PASS — 19 passed, 0 failed**; includes matching, undeclared, and misstated real-gitlink CLI repositories |
| `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-1-10-direct-tenants-reads-and-authoritative-freshness.md` | **PASS — six pointer movements declared; `RESULT: PASS`** |
| `rg -n "SubmitQueryAsync\|/api/v1/queries\|QueryRouter\|HandlerAwareQueryRouter" src/Hexalith.Tenants.UI` | **PASS — no matches (expected exit 1)** |
| `rg -n 'tenant-index:system\|"tenant-index"' src/Hexalith.Tenants.UI tests/Hexalith.Tenants.UI.Tests` | **PASS — no matches (expected exit 1)** |
| `git diff --check` | **PASS** |

The previously accepted live six-route socket limitation is unchanged: this repair does not reinterpret a
self-skipping/non-blocking Aspire lane as proof. The generated-controller and live-topology code did not
change; the changed standalone-host topology is exercised by the real `WebApplicationFactory` endpoint data
source in the blocking UI project.
