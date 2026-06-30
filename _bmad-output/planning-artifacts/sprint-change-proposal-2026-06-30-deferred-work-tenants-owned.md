# Sprint Change Proposal — Deferred-Work Backlog (Tenants-Owned, Actionable)

- **Date:** 2026-06-30
- **Author:** Correct Course workflow (Administrator)
- **Trigger artifact:** `_bmad-output/implementation-artifacts/deferred-work.md`
- **Scope chosen:** Tenants-owned, actionable (this repo only). Cross-submodule handoffs and Product/IA-blocked items stay deferred.
- **Review mode:** Batch.
- **Change scope classification:** **Minor** (direct developer implementation — bounded UI hardening + regression tests; no epic/PRD/architecture change).

---

## Section 1 — Issue Summary

`deferred-work.md` is the routing index for follow-ups raised by adversarial code reviews. Most entries are already resolved; this proposal navigates only the **open, Tenants-owned** items that can be fixed in this repository now. Each was re-verified against live source on 2026-06-30 (several cited line numbers had shifted because the files were edited on 06-30).

The common thread: the **global-administrator** correction path (story 5.7) was hardened on 06-29/06-30 (freshness-gated confirm, invariant-culture proof parse, time-tie-back proof lookup, all-terminal-state focus), but the **tenant-domain** correction path (stories 5.6/5.8) and two audit-page lifecycle paths did **not** receive the parity fixes. They are latent, mostly fail-safe, but real.

## Section 2 — Impact Analysis

- **Epic impact:** None. No epic can-still-be-completed question is raised; these are post-implementation hardening follow-ups inside already-`done` Epic 5 stories (5.6/5.7/5.8). No resequencing.
- **Story impact:** Touches code owned by stories 5.6 (tenant correction panel), 5.7 (global-admin/audit page), 5.8 (projection-refresh provider). No story re-opens; these land as a focused correction-course follow-through.
- **Artifact conflicts:** None. PRD, architecture, UX, and epics are unaffected — behavior moves *toward* the documented contracts in `project-context.md` (fail-closed on stale, support-safe, projection-confirmed success, focus/live-region parity). No doc edits required beyond the deferred-work routing index + `sprint-status.yaml` action-item registration.
- **Technical impact:** UI-layer only (`Hexalith.Tenants.UI`) + its bUnit tests. No contracts, no server, no DAPR/topology, no gateway-routing change. Release package surface unchanged.

## Section 3 — Recommended Approach

**Option 1 — Direct Adjustment (SELECTED).** Apply the seven verified fixes as a single focused change, mirroring the already-approved global-administrator hardening patterns so the tenant-domain and audit-page paths reach parity. Add regression tests for each behavioral change.

- *Rollback (Option 2):* N/A — nothing to revert; these are additive hardenings.
- *MVP review (Option 3):* N/A — MVP unaffected.
- **Effort:** Medium · **Risk:** Low–Medium (item 6 freshness gate and item 7 proof tie-back change observable behavior; both are covered by new tests and mirror shipped GA patterns).

### Verification dispositions (what changed since the backlog was written)

| # | Deferred item | Live status (2026-06-30) | Action |
|---|---|---|---|
| 1 | No 5.7 gateway-routing test | **Already covered** — `TenantCommandGatewayTests` already pins the full `system / global-administrators / global-administrators` triple + CommandType + literal payload for **both** Set & Remove (lines 22–74). The item is explicitly conditional ("add *if the gateway is touched*"); the gateway is unchanged. | **Close, no new test** (would be a near-duplicate). |
| 2 | Focus call lacks `JSDisconnectedException` guard | **Confirmed open** — both `CorrectionStartPanel.OnAfterRenderAsync` and `GlobalAdministratorCorrectionPanel.OnAfterRenderAsync` call `_lifecycleElement.FocusAsync()` with no guard. | **Fix both panels.** |
| 3 | Global-admin projection query unguarded in page-load | **Confirmed open** — `LoadAsync` awaits `RefreshGlobalAdministratorsProjectionAsync()` unguarded; gateway catches only `EventStoreGatewayException`, so any transport/parse fault breaks the whole audit page. | **Guard at the page-load call site only** (confirm-time path keeps throwing). |
| 4 | Tenant panel terminal-state focus parity | **Confirmed open** — `CorrectionStartPanel.SetSnapshot` focuses only `Confirmed`/`Failed`; GA panel covers six terminal states. | **Broaden tenant panel to match GA.** |
| 5 | Concurrent correction opens finish out of order | **Confirmed open** — `OpenCorrectionAsync` awaits refresh then sets `_activeCorrectionIntent`; out-of-order completion can leave the older intent active. | **Add a generation guard.** |
| 6 | Tenant correction confirms from stale/degraded detail | **Confirmed open** — provider strips freshness; `TenantCorrectionPreviewSnapshot.ConfirmProjection` checks id+role only. GA gates on `Freshness=Current`. | **Gate the tenant confirm path on `Current`.** |
| 7 | Tenant corrective-proof lookup links unrelated rows + culture | **Confirmed open** — `QueryCorrectiveProofAsync` is match-only (no time tie-back); `ProofTimestampLabel` + the `WithCorrectiveProof` parse use ambient culture. GA was fixed to invariant + `Timestamp > original`. | **Mirror GA tie-back + invariant culture.** |
| 8 | Create-tenant freshness gate narrowed `Current or Unknown → Current` | **Resolved** — `TenantsWorkspace.razor:106` is back to `Current or Unknown`, matching the documented first-tenant bootstrap exception. The "restore" path was taken. | **Close as resolved.** |

### Explicitly kept deferred (out of selected scope)

- Global-admin projection **pagination ignored (>20 admins)** — design-level (projection paging/aggregation); the 06-30 re-review noted the confirm-time false-`Confirmed` path raises severity. **Route to a dedicated story**, not a quick fix.
- **EventCallback→Func** parent-re-render watch-item (benign), **ETag special-character** robustness (latent, non-exploitable) — remain watch-items.
- IA-blocked: GA/Audit **discoverability**, `GlobalAdministratorPolicy` registered-but-unconsumed, page-local **empty tabpanels** — await the Product/UX IA decision.
- Cross-submodule handoffs (FrontComposer `FcContentLabel`/`FocusHeadingAsync` docs, EventStore SVG `<g tabindex>`).

## Section 4 — Detailed Change Proposals

All edits are in `src/Hexalith.Tenants.UI/**` + `tests/Hexalith.Tenants.UI.Tests/**`. Patterns are copied from the shipped global-administrator path for consistency.

### Edit A — `JSDisconnectedException` guard on panel focus (item 2)

`Components/Tenants/Audit/CorrectionStartPanel.razor` and `GlobalAdministratorCorrectionPanel.razor`, in `OnAfterRenderAsync`:

```
// BEFORE
_focusLifecyclePending = false;
await _lifecycleElement.FocusAsync().ConfigureAwait(false);

// AFTER
_focusLifecyclePending = false;
try
{
    await _lifecycleElement.FocusAsync().ConfigureAwait(false);
}
catch (JSDisconnectedException)
{
    // Circuit dropped between the terminal SetSnapshot and this render; focus is no longer observable.
}
```
Add `@using Microsoft.JSInterop` to each panel. **Rationale:** mirrors the already-present `JSDisconnectedException` guards on `TenantAuditPage` focus/dispose paths.

### Edit B — Page-load guard for global-admin enrichment (item 3)

`Components/Pages/TenantAuditPage.razor`, `LoadAsync`:

```
// BEFORE
if (_snapshot.Rows.Any(IsGlobalAdministratorRow))
{
    await RefreshGlobalAdministratorsProjectionAsync().ConfigureAwait(false);
}

// AFTER
if (_snapshot.Rows.Any(IsGlobalAdministratorRow))
{
    // Page-load enrichment only: the audit list already rendered. A transport/parse fault that escapes
    // the gateway's EventStoreGatewayException mapping must degrade this supplementary global-administrator
    // evidence, not tear down the audit page. The confirm-time path (OpenCorrectionAsync / the panel
    // ProjectionRefreshProvider) keeps propagating so a correction never silently confirms on a failed read.
    try
    {
        await RefreshGlobalAdministratorsProjectionAsync().ConfigureAwait(false);
    }
    catch (Exception ex) when (ex is EventStoreGatewayException or HttpRequestException or JsonException)
    {
        // Supplementary evidence unavailable; the audit page stays usable.
    }
}
```
Add usings as needed (`Hexalith.EventStore.Client.Gateway`, `System.Net.Http`, `System.Text.Json`). **Scope note:** the catch is narrowed to the transport/serialization faults that can escape the gateway, matching the codebase convention (`TenantCommandGateway` catches the same set) rather than a blanket `catch (Exception)`.

### Edit C — Tenant panel terminal-state focus parity (item 4)

`Components/Tenants/Audit/CorrectionStartPanel.razor`, `SetSnapshot`:

```
// BEFORE
&& _snapshot.LifecycleState is TenantCommandLifecycleState.Confirmed or TenantCommandLifecycleState.Failed)

// AFTER (mirror GlobalAdministratorCorrectionPanel.SetSnapshot)
&& _snapshot.LifecycleState is TenantCommandLifecycleState.Confirmed
    or TenantCommandLifecycleState.Failed
    or TenantCommandLifecycleState.Rejected
    or TenantCommandLifecycleState.Degraded
    or TenantCommandLifecycleState.UnableToVerify
    or TenantCommandLifecycleState.AlreadyApplied)
```

### Edit D — Concurrent-open ordering guard (item 5)

`Components/Pages/TenantAuditPage.razor`. Add field `private int _correctionOpenGeneration;`. In `OpenCorrectionAsync`, capture a generation synchronously at entry (before the first `ConfigureAwait(false)`), and apply the intent only if still latest:

```
private async Task OpenCorrectionAsync(TenantCorrectionStartIntent intent)
{
    int generation = ++_correctionOpenGeneration; // runs on the dispatcher before any await
    ... existing projection refresh ...
    await InvokeAsync(() =>
    {
        if (generation != _correctionOpenGeneration)
        {
            return; // A newer correction open superseded this one while its projection refreshed.
        }
        _activeCorrectionIntent = intent;
        _activeCorrectionFocusReference = intent.OriginalAuditReference;
    }).ConfigureAwait(false);
}
```

### Edit E — Tenant confirm fail-closed on stale/degraded (item 6, minimal-surface)

`Components/Pages/TenantAuditPage.razor`, `RefreshTenantProjectionAsync` (the tenant panel's confirm-time `ProjectionRefreshProvider`): return the detail **only when fresh**, else `null` so the existing `ConfirmProjection(null)` fails closed.

```
// AFTER (tail of RefreshTenantProjectionAsync)
await InvokeAsync(() => _tenantDetailSnapshot = snapshot).ConfigureAwait(false);
// Confirm-time provider contract: only a Current (fresh, Ready) projection is admissible as correction
// proof. Stale/Degraded/Unknown returns null so ConfirmProjection fails closed instead of confirming a
// tenant correction off stale evidence (parity with the global-administrator Freshness=Current gate).
return snapshot?.Freshness is ReadModelFreshnessState.Current ? snapshot.Detail : null;
```
**Why minimal-surface (not the full GA-style signature change):** gating at the provider boundary reaches the same fail-closed result without re-typing the `ProjectionRefreshProvider` delegate or disturbing the gateway-null fallback path used by tests. `ConfirmProjection(null)` already returns a non-confirming snapshot focused on Refresh.

### Edit F — Tenant corrective-proof time tie-back + invariant culture (item 7)

`Components/Tenants/Audit/CorrectionStartPanel.razor`:
1. `QueryCorrectiveProofAsync` — mirror `GlobalAdministratorCorrectionPanel`: parse `originalTimestamp` via a `TryParseOriginalTimestamp` helper (`InvariantCulture` + `DateTimeStyles.RoundtripKind`); request audit with `From: originalTimestamp`; filter `row.Timestamp > originalTimestamp`; `OrderByDescending(row => row.Timestamp).FirstOrDefault(...)`. Missing/malformed timestamp ⇒ return `null` ⇒ `AuditDelayed`.
2. `ProofTimestampLabel` — `CultureInfo.CurrentCulture` → `CultureInfo.InvariantCulture`.

`State/TenantAudit/TenantCorrectionPreviewSnapshot.cs`, `WithCorrectiveProof`: parse `originalTimestamp` with `CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind`.

### Test plan (bUnit / xUnit v3 / Shouldly)

- **C:** terminal focus parity — a `Rejected` / `Degraded` / `UnableToVerify` terminal state sets focus on the lifecycle region in the tenant panel.
- **E:** stale-confirm fail-closed — a `Stale`/`Degraded` provider result keeps the tenant correction in a non-`Confirmed` state.
- **F:** proof tie-back — an older matching audit row is **not** linked; only a row newer than the original timestamp is; a missing/malformed `originalTimestamp` ⇒ `AuditDelayed`.
- **B:** page-load resilience — a global-admin projection refresh that throws `HttpRequestException` during `LoadAsync` still renders the audit list.
- **D:** generation guard — a superseded open does not overwrite the newer active intent.
- **A:** `JSDisconnectedException` guard is not bUnit-observable (bUnit renders on the dispatcher and does not drop the circuit); covered by mirroring the established `TenantAuditPage` pattern — noted, not unit-tested.
- Maintain EN/FR resource parity; no new user-facing strings expected (reusing existing audit-delayed/lifecycle keys).

**Validation commands (per-project, CI shape):**
`dotnet build Hexalith.Tenants.slnx -c Release -warnaserror` and `dotnet test tests/Hexalith.Tenants.UI.Tests`.

## Section 5 — Implementation Handoff

- **Recipient:** Developer agent (direct implementation) — scope is **Minor**.
- **Deliverables:** Edits A–F + regression tests; full `Hexalith.Tenants.UI.Tests` green; Release `-warnaserror` clean.
- **Tracking:** register the closed/deferred dispositions in `_bmad-output/implementation-artifacts/sprint-status.yaml action_items`; update `deferred-work.md` routing notes (close items 1–8 as resolved/covered, keep pagination + watch-items + IA + cross-submodule deferred).
- **Success criteria:** tenant-domain correction path reaches parity with the shipped global-administrator hardening (fail-closed on stale, invariant-culture time-tie-back proof, all-terminal-state focus); audit page survives a supplementary global-admin read fault; no new analyzer warnings; no contract/server/topology change.
