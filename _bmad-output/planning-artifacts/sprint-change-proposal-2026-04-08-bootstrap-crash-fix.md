# Sprint Change Proposal — Bootstrap Host Crash on DAPR Timeout

**Date:** 2026-04-08
**Trigger:** Story 2-4 (Tenant Service Bootstrap) — host crashes when DAPR actor times out during bootstrap
**Scope:** Minor — 1 file, 1 line changed
**Status:** Approved and implemented

## Section 1: Issue Summary

`TenantBootstrapHostedService.StartAsync` crashes the entire host process when the DAPR sidecar is slow to start. The bootstrap sends a `SubmitCommand` via MediatR which routes to a DAPR actor. If the actor invocation times out (100s `HttpClient.Timeout`), a `TaskCanceledException` is thrown. This inherits from `OperationCanceledException`, which the bootstrap catch block re-throws unconditionally — intended for graceful host shutdown only.

**Stack trace:** `TaskCanceledException` → caught by `catch (OperationCanceledException) { throw; }` → propagates to `Host.StartAsync` → `Unhandled exception` → process exit.

## Section 2: Impact Analysis

- **Epic 2** (done): Story 2-4 bug fix. No story status change needed.
- **No other epics affected.**
- **No PRD/Architecture/UX conflicts.**

## Section 3: Recommended Approach

**Direct Adjustment** — Add `when (cancellationToken.IsCancellationRequested)` exception filter.

- Only actual host shutdown cancellation (token canceled by host) re-throws
- DAPR timeouts (`TaskCanceledException` with internal CTS) fall through to generic `catch (Exception ex)` which logs "Bootstrap failed — will retry on next restart" and lets the service continue

**Effort:** Low (1 line)
**Risk:** Low — narrower catch is strictly safer

## Section 4: Change Detail

**File:** `src/Hexalith.Tenants/Bootstrap/TenantBootstrapHostedService.cs` (line 49)

```csharp
// OLD:
catch (OperationCanceledException) {
    throw;
}

// NEW:
catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
    throw;
}
```

## Section 5: Implementation Handoff

**Scope:** Minor — direct implementation
**Success criteria:** Tenants service stays running after DAPR timeout during bootstrap (logs warning instead of crashing)
