# Sprint Change Proposal — 2026-04-20

**Change trigger:** "fix compilation and unit tests"
**Author:** Bob (Scrum Master) — collaborative session with Jerome
**Mode:** Incremental
**Project:** Hexalith.Tenants
**Status:** Awaiting final approval

---

## 1. Issue Summary

Running `dotnet build` on branch `main` (working tree) succeeded with 0 errors, 0 warnings. However, `dotnet test` reported **14 failures across 3 test projects**:

| Project | Before | After scope |
|---|---|---|
| `Hexalith.Tenants.Server.Tests` | 6 failed / 240 total | — |
| `Hexalith.Tenants.Testing.Tests` | 5 failed / 89 total | — |
| `Hexalith.Tenants.IntegrationTests` | 3 failed / 20 total | — |

**Discovery context.** The failure surface appeared after two concurrent changes on `main`:

- Submodule bump `a0fdc26 Update Hexalith.EventStore submodule to latest commit` (pulled EventStore through commit `e84550f "code style"`, 2026-04-16).
- Local controller refactor `53166e7 refactor(api): update tenant endpoints and code style` (renamed `ProjectionActorType` → `ProjectionType` and member identifiers; unrelated to failures).

Tests rely on a `actor:globalAdmin` extension key being carried through the command pipeline. After the submodule bump, this key was silently stripped, causing RBAC tests to take the wrong code path and NonAdmin_Owner conformance tests to unexpectedly succeed where a rejection was expected.

## 2. Impact Analysis

### Failure clusters

| Cluster | Count | Root cause |
|---|---|---|
| **A — Dropped Extensions** | 12 tests (incl. cascade into `SnapshotPerformanceTests`) | `Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Commands/CommandEnvelope.cs:75-77` regressed `new Dictionary<string,string>(Extensions)` → `[]` in submodule commit `e84550f`. The "defensive copy" produced an **empty** dictionary, silently dropping every caller-supplied key including `actor:globalAdmin`. |
| **B — Missing controller route** | 1 test (`CommandApiRuntimeIntegrationTests.Commands_endpoint_returns_problem_details_for_domain_rejection`) | `/api/v1/commands` is provided by `CommandsController` which lives in the `Hexalith.EventStore` assembly. `src/Hexalith.Tenants/Program.cs` called `AddControllers()` without `AddApplicationPart(typeof(CommandsController).Assembly)`, so MVC never discovered that route in the Tenants domain service. Latent since commit `e6189e5 fix(tenants): remove server pipeline to fix tenant creation deadlock`. |
| **C — Transient infra** | 1 test (`GracefulDegradationTests.DrainRecovery_PublishesPendingEvents_WhenPubSubRecovers`) | Tier-3 test dependent on live DAPR/Docker pub/sub. Failed on first run with 90 s timeout, passed on rerun in 30 s. No code defect. |
| **D — Pre-existing test-design defect (unmasked)** | 1 test (`SnapshotPerformanceTests.ColdStartRehydration_CompletesWithin30Seconds_With500KEvents`) | Was hidden behind Cluster A. Seed loop generates unique `config-key-{i}` keys every 3rd event; crosses `TenantAggregate.MaxConfigurationKeys = 100` around event 301, producing `ConfigurationLimitExceededRejection`. Test was never able to reach its stated 500 K event target. Not caused by this branch. |

### Artifact impact

| Artifact | Impact |
|---|---|
| **PRD** | None |
| **Epics** | None — all 8 epics remain `done` |
| **Architecture doc** | None — the `CommandEnvelope` contract already prescribes defensive copy; the submodule drifted from its own specification |
| **UX spec** | None |
| **Sprint status** | None |
| **CI/CD** | None structural; post-fix Tier 1+2 will pass |

## 3. Recommended Approach — Direct Adjustment (Option 1)

**Selected:** Two small targeted fixes + document the two remaining integration-test issues as out-of-scope / pre-existing.

**Rationale:**
- Cluster A is a straightforward regression in the submodule. The fix is a one-line restoration of the documented defensive copy.
- Cluster B is also a one-line registration addition in the Tenants `Program.cs`. It restores an endpoint that tests expect and that the refactor to a standalone domain service left dangling.
- Cluster C is environmental — no code-level fix warranted.
- Cluster D is a pre-existing defect in `SnapshotPerformanceTests`. It became visible only after Cluster A was fixed (previously failed earlier with a permissions error). Fixing it requires a test-design change that is independent of the submodule bump and should be sequenced as a separate defect ticket.

**Effort:** Low — two single-line edits already applied and verified.
**Risk:** Low — changes restore documented behavior and do not alter architecture.
**Timeline impact:** None.

## 4. Detailed Change Proposals

### 4.1 Cluster A — Restore CommandEnvelope.Extensions defensive copy (APPLIED)

```
File: Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Commands/CommandEnvelope.cs
Lines: 74-77 (Extensions property initializer)

OLD:
    public Dictionary<string, string>? Extensions { get; init; } = Extensions is not null
        ? []
        : null;

NEW:
    public Dictionary<string, string>? Extensions { get; init; } = Extensions is not null
        ? new Dictionary<string, string>(Extensions)
        : null;
```

Rationale: Restore the defensive-copy semantics documented by the XML summary ("Defensively copied to preserve immutability"). The `[]` expression produced a new empty Dictionary and dropped every caller-supplied key, breaking every RBAC test that relies on `actor:globalAdmin`. This was the bug introduced in EventStore commit `e84550f`.

**Handoff:** Commit inside the EventStore submodule with a `fix(contracts):` Conventional-Commit message, then bump the submodule pointer in `Hexalith.Tenants`.

### 4.2 Cluster B — Register EventStore controllers as an application part (APPLIED)

```
File: src/Hexalith.Tenants/Program.cs
Lines: 78-79

OLD:
    builder.Services.AddControllers();
    builder.Services.AddActors(options => options.Actors.RegisterActor<TenantsProjectionActor>());

NEW:
    builder.Services.AddControllers()
        .AddApplicationPart(typeof(Hexalith.EventStore.Controllers.CommandsController).Assembly);
    builder.Services.AddActors(options => options.Actors.RegisterActor<TenantsProjectionActor>());
```

Rationale: The Tenants domain service depends on `CommandsController` (and other controllers in the EventStore assembly) to serve `/api/v1/commands`. Without an explicit application part, MVC's default discovery only scans the entry assembly, silently omitting those routes. All supporting services (`ICommandRouter`, `ICommandStatusStore`, `ICommandArchiveStore`, `ExtensionMetadataSanitizer`, `DomainCommandRejectedExceptionHandler`) are already registered in `Program.cs`, so this single line completes the wiring.

### 4.3 Cluster C — No change (DOCUMENTED)

`GracefulDegradationTests.DrainRecovery_PublishesPendingEvents_WhenPubSubRecovers` is a Tier-3 integration test that relies on live DAPR + Docker pub/sub. It failed once with a 90-second drain timeout and then passed on rerun in 30 seconds. Treat as a known-flaky environmental test.

### 4.4 Cluster D — Pre-existing defect, out of scope (BACKLOG)

`SnapshotPerformanceTests.ColdStartRehydration_CompletesWithin30Seconds_With500KEvents` was masked by Cluster A. With A fixed, the test fails at event 301 with `ConfigurationLimitExceededRejection` because its seed loop creates 100+ unique configuration keys against a hard cap of 100 (`TenantAggregate.MaxConfigurationKeys = 100`).

Proposed remediation (to be sprinted separately — NOT applied here):
1. Reuse a bounded set of keys via `config-key-{i % 50}` so SetTenantConfiguration hits idempotent / update branches past the 100-key cap, **or**
2. Remove SetTenantConfiguration from the seed mix and substitute UpdateTenant / AddUserToTenant variants, **or**
3. Raise `MaxConfigurationKeys` if the domain decision supports it.

Decision deferred to product/architecture review.

## 5. Verification (post-application)

| Layer | Result |
|---|---|
| `dotnet build` | 0 errors, 0 warnings |
| `Hexalith.Tenants.Contracts.Tests` | 34 / 34 passed |
| `Hexalith.Tenants.Client.Tests` | 48 / 48 passed |
| `Hexalith.Tenants.Sample.Tests` | 17 / 17 passed |
| `Hexalith.Tenants.Testing.Tests` | 89 / 89 passed (was 84 / 89) |
| `Hexalith.Tenants.Server.Tests` | 240 / 240 passed (was 234 / 240) |
| `Hexalith.Tenants.IntegrationTests — CommandApi rejection` | passing |
| `Hexalith.Tenants.IntegrationTests — DrainRecovery` | passing on rerun (transient) |
| `Hexalith.Tenants.IntegrationTests — ColdStartRehydration` | failing — pre-existing test defect (see §4.4) |

## 6. Implementation Handoff

**Scope classification:** **Minor** — two single-line edits already applied; no backlog restructuring.

| Role | Responsibility |
|---|---|
| **Dev (Jerome)** | Commit the submodule fix (`fix(contracts): restore CommandEnvelope defensive copy for Extensions`) and the `Program.cs` edit (`fix(server): register EventStore controllers as application part`). Bump the submodule pointer in `Hexalith.Tenants`. |
| **SM (Bob)** | Open a separate defect ticket for Cluster D before performance regression testing resumes. |
| **QA / CI** | Confirm CI Tier 1+2 remains green after the submodule bump. Tier 3 remains manual. |

**Success criteria:** `dotnet test` returns 0 failures across Tier 1+2 on a clean checkout with the submodule pointer updated.

---
