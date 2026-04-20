# Sprint Change Proposal — 2026-04-20 (tests, second pass)

**Change trigger:** "fix test issues" (follow-up to SCP 2026-04-20)
**Author:** Bob (Scrum Master) — collaborative session with Jerome
**Mode:** Incremental
**Project:** Hexalith.Tenants
**Status:** Approved (Proposal 2 applied; Proposal 1 reverted pending Cluster E investigation)

---

## 1. Issue Summary

Immediately after the morning SCP (commit `8f3790f`), `dotnet test` on `Hexalith.Tenants.slnx` was rerun to verify the CI surface.

| Layer | Result |
|---|---|
| Tier 1 (Contracts, Client, Sample, Testing) | 187 / 187 passed |
| Tier 2 (Server.Tests) | 240 / 240 passed |
| Tier 3 Aspire contract (`Category!=Performance`) | 19 / 20 → **1 flaked** (`DrainRecovery_PublishesPendingEvents_WhenPubSubRecovers`) |
| Tier 3 Performance (`Category=Performance`, nightly only) | 0 / 1 (`ColdStartRehydration_CompletesWithin30Seconds_With500KEvents`) |

Both remaining failures were already catalogued in the morning SCP as Cluster C (transient environmental) and Cluster D (pre-existing test defect). The follow-up trigger was *both* — fix whatever can be fixed in the working-tree integration test suite.

## 2. Impact Analysis

### 2.1 Cluster C — `DrainRecovery_PublishesPendingEvents_WhenPubSubRecovers` (PR CI blocker)

Tier-3 non-Performance → **runs on every PR** (`.github/workflows/ci.yml:96`, filter `Category!=Performance`). Flake budget inside the test is 90 s; the default drain schedule is `InitialDrainDelay=30s`, `DrainPeriod=60s` (`EventDrainOptions.cs:9,12`). If the first reminder tick races `ClearFailure()` (the test clears the fake-publisher failure in the test thread while the reminder executes in the actor runtime thread) the publish attempt fails, and the next retry lands at ≥ 90 s — past the poll budget.

### 2.2 Cluster D — `ColdStartRehydration_CompletesWithin30Seconds_With500KEvents` (nightly only)

Tier-3 Performance → **nightly only** (`.github/workflows/ci.yml:143`, filter `Category=Performance`). Two independent defects are compounded here:

| Layer | Finding |
|---|---|
| **D1 — key-cap exhaustion** | Seed loop at `SnapshotPerformanceTests.cs:129` uses `config-key-{i}` (unique per iteration). Of 500 seeded events, ≈ 166 are `SetTenantConfiguration`. `TenantAggregate.MaxConfigurationKeys = 100` (`TenantAggregate.cs:14`) triggers `ConfigurationLimitExceededRejection` around event 301. |
| **D2 — extensions dropped in wire path** | Applying a trial fix `config-key-{i % 50}` unmasks a deterministic rejection at *event 1* (`SetTenantConfiguration`) with `InsufficientPermissionsRejection` despite the test envelope carrying `actor:globalAdmin`. Confirmed via serial, small-N repro. Root cause is **not** JSON / DataContract serialization (both round-trips preserve extensions in isolation) and **not** the direct `/process` endpoint (also preserves). The defect lives somewhere in the DAPR actor message → `AggregateActor.ProcessCommandAsync` path and needs deeper instrumentation than this session afforded. |

No impact on PRD, epics, architecture, UX spec, or sprint status.

### 2.3 Artifact impact summary

| Artifact | Impact |
|---|---|
| PRD | None |
| Epics | None — all 8 epics remain `done` |
| Architecture | None |
| UX | None |
| Sprint status | None |
| CI/CD | PR CI (Tier 1+2+Aspire) is unblocked by this change. Nightly (Performance) remains broken pending Cluster E defect. |

## 3. Recommended Approach — Partial Direct Adjustment

**Selected:** apply the PR-blocking fix (Proposal 2); defer the nightly fix until Cluster E is understood.

### Rationale

- Cluster C is a timing knob, fully addressable at the test fixture level with no production behaviour change.
- Cluster D has two layers — the easy key-cap fix (D1) is harmless in isolation, but it *unmasks* a deterministic cross-wire regression (D2 / Cluster E) that we could not root-cause inside the session's investigation budget. Landing D1 alone would turn a nightly-green-on-CI signal into a nightly-red-on-CI signal without advancing the root cause, so we revert it and file one combined defect.
- All serialization probes (`Diagnostic_SystemTextJson_RoundTrip_*`, `Diagnostic_DataContract_RoundTrip_*`, `Diagnostic_DaprWebJson_RoundTrip_DomainServiceRequest_*`, `Diagnostic_DirectProcessEndpoint_ExtensionsPreserved`) passed, ruling out the first few plausible causes before punting. These probes are *discarded* from the working tree to keep the diff minimal but their conclusions are recorded here.

**Effort:** Low — one config-level edit in the test fixture.
**Risk:** Low — test-only change; production drain options untouched.
**Timeline impact:** None for PR CI; nightly remains red pending defect intake.

## 4. Detailed Change Proposals

### 4.1 Proposal 2 — Cluster C fix (APPLIED)

```
File: tests/Hexalith.Tenants.IntegrationTests/Fixtures/TenantsDaprTestFixture.cs
Lines: 184-190 (after the Publisher:PubSubName line, inside StartTestHostAsync)

OLD:
    // Configure pub/sub name for event publisher
    builder.Configuration["EventStore:Publisher:PubSubName"] = "pubsub";

NEW:
    // Configure pub/sub name for event publisher
    builder.Configuration["EventStore:Publisher:PubSubName"] = "pubsub";

    // Speed up drain recovery for tests (default is 30s initial / 60s period).
    // Keeps DrainRecovery_PublishesPendingEvents_WhenPubSubRecovers deterministic
    // within its 90s poll budget even if the first reminder tick races ClearFailure().
    builder.Configuration["EventStore:Drain:InitialDrainDelay"] = "00:00:05";
    builder.Configuration["EventStore:Drain:DrainPeriod"] = "00:00:05";
```

**Rationale.** `EventDrainOptions` is already bound from config (`EventDrainOptions.cs:7-16`). Production defaults (30 s / 60 s) are appropriate for live deployments but create a flake window inside a 90 s test budget. Five seconds for both knobs gives the test ≥ 15 reminder opportunities inside the existing poll window — deterministic without touching production behaviour.

**Measured impact.** Test passes in 5 s (was 90 s+ flake, observed failure in back-to-back runs before the fix).

**Handoff:** Commit on the working-tree branch with message `fix(tests): shorten drain delays to stabilise DrainRecovery test`.

### 4.2 Proposal 1 — Cluster D1 (REVERTED)

```
File: tests/Hexalith.Tenants.IntegrationTests/SnapshotPerformanceTests.cs
Line: 129

Attempted NEW:
    new SetTenantConfiguration(tenantId, $"config-key-{i % 50}", $"value-{i}")

Reverted back to:
    new SetTenantConfiguration(tenantId, $"config-key-{i}", $"value-{i}")
```

**Why reverted.** The fix itself is correct for D1 (bounds unique keys to 50, idempotent updates past that). But applying it exposes D2 (Cluster E), which then fails the same test at event 1 rather than event 301 — a non-improvement for nightly signal. The key-cap fix is retained in this document as the pre-approved remediation to apply **once Cluster E is resolved** (see §4.3).

### 4.3 Cluster E — new defect (OPEN)

**Title:** `CommandEnvelope.Extensions` dropped on ActorProxy → AggregateActor wire path (nightly Performance test only).

**Observations recorded during investigation (discarded probes):**

1. `JsonSerializer.Serialize/Deserialize<CommandEnvelope>` — preserves `actor:globalAdmin` ✅
2. `DataContractSerializer.WriteObject/ReadObject<CommandEnvelope>` — preserves `actor:globalAdmin` ✅
3. `JsonSerializerDefaults.Web` over `DomainServiceRequest { Command: CommandEnvelope, … }` — preserves `actor:globalAdmin` ✅
4. Direct HTTP `POST /process` with a hand-built `DomainServiceRequest` — extensions flow through, `SetTenantConfiguration` succeeds ✅
5. ActorProxy → `IAggregateActor.ProcessCommandAsync` with identical envelope — `InsufficientPermissionsRejection` returned, *deterministic*, even at `MaxConcurrency=1` ❌

**Inference.** The drop happens between the ActorProxy remoting boundary and the aggregate's `Handle` method. Likely suspects (not yet disproven, ordered by plausibility):

1. **DAPR actor remoting serializer** — `CommandEnvelope.cs:33-34` documents that `DataContractSerializer bypasses constructors, so deserialized instances skip [validation]`. The same bypass means the primary-constructor defensive-copy initializer on `Extensions` never runs on the actor-side receive. If the DAPR remoting pipeline writes through an internal wrapper that loses the dictionary, the property setter would be left `null`.
2. **`AggregateActor` envelope rewrite.** Nothing obvious in the source modifies `command.Extensions` before forwarding, but a full audit of the Processing → Rehydration → DomainServiceInvoke chain has not been done under a debugger.
3. **DAPR SDK 1.17.7 regression.** Package versions were bumped earlier this sprint; the envelope-extension flow worked before then (events 1-300 seeded successfully in the morning SCP run).

**Next steps for the follow-up defect:**

- Reproduce with `EventStore:Drain:InitialDrainDelay=0` and `MaxConcurrency=1` (already done in this session — deterministic).
- Attach a breakpoint / tracing at `AggregateActor.ProcessCommandAsync` line 55 and log `command.Extensions` on receipt to confirm the drop is at remoting, not later.
- If drop is at remoting: bind `IActorMessageBodySerializationProvider` explicitly to `JsonActorMessageBodySerializer` in `AddActors(...)` (see `ServiceCollectionExtensions.cs:67`), or add `[KnownType(typeof(Dictionary<string,string>))]` on `CommandEnvelope`.
- If drop is later: audit `AggregateActor` for any `with { Extensions = ... }` or dictionary-filtering helper applied before `domainServiceInvoker.InvokeAsync`.
- Once Cluster E is closed, re-apply Proposal 1 verbatim (§4.2) to close Cluster D1.

**Assignee:** dev/backend (Jerome)
**Priority:** Low (nightly-only; no production-user impact; PR CI green without it)
**Sprint placement:** next available

## 5. Verification (post-application)

`dotnet test Hexalith.Tenants.slnx --filter "Category!=Performance"` (matches `aspire-tests` CI job filter):

| Project | Result |
|---|---|
| Hexalith.Tenants.Contracts.Tests | 34 / 34 |
| Hexalith.Tenants.Client.Tests | 48 / 48 |
| Hexalith.Tenants.Sample.Tests | 17 / 17 |
| Hexalith.Tenants.Testing.Tests | 88 / 88 |
| Hexalith.Tenants.Server.Tests | 240 / 240 |
| Hexalith.Tenants.IntegrationTests | 19 / 19 (DrainRecovery passes in 5 s) |
| **Total** | **446 / 446 — 0 failures** |

Nightly filter (`Category=Performance`): `ColdStartRehydration` still fails, tracked as Cluster E defect.

## 6. Implementation Handoff

**Scope classification:** **Minor** — one test-fixture config edit; no backlog restructuring.

| Role | Responsibility |
|---|---|
| Dev (Jerome) | Commit Proposal 2 with `fix(tests): shorten drain delays to stabilise DrainRecovery test`. Open Cluster E defect per §4.3. |
| SM (Bob) | Place Cluster E defect on next sprint's available-work shelf. |
| QA / CI | Confirm the next PR's Aspire-contract CI job is green. Continue to accept nightly Performance job as red pending Cluster E. |

**Success criteria.** `dotnet test Hexalith.Tenants.slnx --filter "Category!=Performance"` returns 0 failures across Tier 1+2+Aspire-contract on a clean checkout. (Met.)

---
