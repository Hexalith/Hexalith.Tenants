# Story 1.10 Evidence — Direct Tenants Reads and Authoritative Freshness

Date: 2026-07-28
Baseline: `8d64563` plus the Story 1.10 working tree

## Outcome

The Tenants UI BFF now performs its six query reads through a server-side typed REST client configured
from `Tenants:BaseAddress`. EventStore remains the command/status dependency. Missing read and command
references resolve independently and fail closed without falling back across that boundary.

The tenant member region now owns a dedicated paged tenant-users snapshot. Its rows, cursor, ETag,
projection version, lifecycle, and freshness are independent from tenant detail. Member actions require
both reads to be current, lifecycle-current, and projection-version consistent; the existing detail
re-query remains the command-confirmation authority.

Optional projection notifications are scoped and coalesced. A matching signal exposes only local
refreshing intent, retains the last-confirmed snapshot, and requests a direct authoritative re-query.
The signal itself never changes freshness, projection version, payload, confirmation, or audit
availability.

## Six-route proof

The production client contains exactly these direct GET path shapes:

1. `/api/tenants?cursor=…&pageSize=…`
2. `/api/tenants/{tenantId}`
3. `/api/tenants/{tenantId}/users?cursor=…&pageSize=…`
4. `/api/users/{userId}/tenants?cursor=…&pageSize=…`
5. `/api/tenants/{tenantId}/audit?from=…&to=…&category=…&cursor=…&pageSize=…`
6. `/api/global-administrators?cursor=…&pageSize=…`

Literal route identities and query values are URI-escaped. Conditional requests accept only a strong,
bounded validator. Caller cancellation propagates. Non-success response content is not deserialized or
logged.

The structural negative proof was:

```text
if rg -n '/api/v1/queries|SubmitQueryAsync|QueryRouter|HandlerAwareQueryRouter' \
  src/Hexalith.Tenants.UI -g '*.cs' -g '*.razor'; then exit 1; else ...; fi
PASS: no generic EventStore query route or submission symbols in Tenants UI production source.
```

The companion route scan returned only the six path-building locations in
`TenantsRestQueryClient.cs` (lines 38, 52, 67, 84, 101, and 119 at verification time).

## Metadata and safety findings

- `200` responses preserve projection-backed provenance, strong ETag, projection version, lifecycle,
  `IsStale`, `IsDegraded`, and `ServedAt`; `ServedAt` is retained as metadata but never used as freshness
  evidence.
- A `304` retains data only when the response itself repeats projection-backed provenance, a strong
  ETag, projection version, non-degraded evidence, and a supported current/stale classification.
- Missing, malformed, weak, contradictory, degraded, or non-projection evidence resolves to unknown or
  a fixed invalid-metadata failure and never proves recovery.
- `401`, `403`, `404`, invalid request, timeout, network/5xx, invalid payload, and invalid metadata map to
  fixed categories. Problem Details and raw response bodies are not propagated.
- The direct HTTP client removes default HTTP loggers, and bearer relay is attached only in server-side
  authorized composition. A focused composition test proves that enabling relay adds exactly one client
  handler action.
- Snapshot `ToString()` output remains support-safe: it reports state/count flags, never ETags,
  projection versions, cursors, payloads, tokens, correlations, or raw headers.

## Verification record

Project-specific restores were run before the focused project commands because this repository's
source/package reference modes produce different project asset graphs. A solution restore was run
before the solution command. The requested `--no-restore` commands then produced:

| Command | Result |
| --- | --- |
| `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --no-restore` | PASS — 1,351 passed, 0 failed, 0 skipped |
| `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --filter FullyQualifiedName~TenantsApiGeneratedControllerTests --no-restore` | PASS — 26 passed, 0 failed, 0 skipped |
| `dotnet test Hexalith.Tenants.slnx --no-restore` | PASS — Contracts 120, Client 50, Server 738, Testing 181, Sample 39, UI 1,351, Integration 167; 0 failed; one explicit 500K-event performance test skipped |
| `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-1-10-direct-tenants-reads-and-authoritative-freshness.md` | PASS — no `references/` pointer changes |
| `git diff --check` | PASS |

The generated-controller lane is the executable PLAT-FRESH-1 evidence for the six route/controller and
response-header contract. Focused transport tests additionally cover exact path/query construction,
route escaping, `200`/`304`, empty and authorization status handling, metadata failures, `ServedAt`
independence, cancellation, and safe failure categories.

## HOST-REF-1 status

`HOST-REF-1` remains open as an evidence limitation. The transitional repository AppHost creates the
external `tenants-api` resource, but its `tenants-ui` resource currently references only EventStore and
Memories and supplies no `Tenants:BaseAddress`. Story authority explicitly prohibited editing AppHost to
work around that gap.

Hosted route smoke tests now prove the required consequence: list, detail, self-audit, audit, and global
administrator reads render fail-closed unavailable states and do not fall back to EventStore. Therefore
the full solution is green, but this record does not claim an authenticated live-host direct REST call.
That proof can be added when the composing host owner supplies the `Tenants` service reference and
`Tenants:BaseAddress`.
