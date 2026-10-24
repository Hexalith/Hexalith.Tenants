---
title: 'Qualify authenticated workspace tabs and grant modal focus'
type: 'bugfix'
created: '2026-10-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
baseline_commit: c1c3ca672d481aaa52eff6e20b37ec9a77b24f12
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Close five deferred verification records, DW-333, DW-335, DW-336, DW-357 and DW-359, by observing the authenticated, running Tenants Blazor workspace and administrator grant preview in Chromium. Verify that inactive workspace panels are invisible and excluded from keyboard/accessibility navigation, and that the real grant dialog contains focus through native Tab and Shift+Tab, cancels with Escape from Fluent controls, restores launcher focus and applies measured viewport restrictions.

Use the existing local Aspire topology and existing credential-parameter handoff. Preserve credentials and session material in memory or private temporary files only. Exercise preview and cancellation without dispatching administrator commands. Record exact source identity, commands, measured browser results and any limitations; close records only after their required observations pass. Commit and push the completed batch to main as requested.

</frozen-after-approval>

## Implementation Notes

- Investigation: `TenantsWorkspace.razor` composes `FcPageTabs` with tenant and workspace-users panels. `GlobalAdministratorsPage.razor` owns grant preview sentinels, inert background, viewport admission and Escape callbacks. Reuse these production surfaces unchanged where verification passes.
- Reuse the OIDC login pattern in `tests/performance/tenant-audit/audit-performance.spec.ts`. Existing `Browser/validate-tenants-focus-browser.sh` uses exported markup and cannot close authenticated callback requirements.
- Footprint: one hosted browser validation script, this spec, batch evidence and five ledger dispositions. No new public API, dependencies, persistent data mutation or policy choice is needed. Browser actions are read/preview/cancel only; there are no unresolved intent gaps.
- Initial baseline: `aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json`, followed by `aspire describe`, reached healthy resources after cold Keycloak startup. Use a fresh non-persistent realm with explicitly chosen temporary credentials per `docs/quickstart.md`; never extract generated credentials from resources.

- Implemented: move the inline Razor comment above the grant input to prevent InvalidCharacterError/circuit termination; extend EN/FR input regressions; add a credential-safe hosted Chromium validator and retained measured results.
- Validation: Debug source build has zero warnings/errors; focused component checks pass 442/442 and the complete UI project passes 7,496/7,496. Nine authenticated browser scenarios pass with zero circuit errors. The current Fluent host has no focusable inner shadow control; results record that fact and the validator requires real inner focus if one exists.
- Runtime synchronization: explicitly restart the no-build UI resource after compiling; await hydrated retained content and settled Blazor sentinel callbacks. Live 767px safety restrictions and 390px layout hiding are separate existing behaviors.

## Review Triage Log

- R1 — medium, patch: resize began with unchecked acknowledgement and disabled confirmation. The validator now acknowledges, proves Confirm enabled, and requires its transition to disabled after resize.
- R2 — medium, patch: the ordinary heading was not a valid positive focus target. The production lifecycle section has tabindex=-1; establish its focus before opening and prove inert prevents refocusing it afterward.
- R3 — medium, patch: disabled-state traversal counted expected controls without rejecting unexpected Confirm focus. Assert the disabled attribute and reject every control outside the expected set in each native cycle.
- R4 — medium, patch: maintenance rows and latest totals were stale. Remove the two grouped rows, add matching closure summaries, and verify exact coverage of 92 open IDs by 85 rows; 565 of 657 records are done.
- R5 — medium, patch: a file hash alone cannot attest an independently supplied endpoint's loaded assembly. Rename the field localUiAssemblySha256 and explicitly distinguish on-disk provenance from the separately inspected local Aspire process/restart.
- R6 — medium, patch: boolean summaries did not retain actual focus observations. Add a whitelist of control names, native key/direction/step, deepest active tag/shadow depth, observed restoration and safe failure state; retain no identity text or session data.
- R7 — medium, patch and recheck: preserve the earlier five-second launcher-wait failure and subsequent failed authentication result. Align focus polling with the twenty-second browser operation deadline (optional circuit setup itself permits ten seconds), retain measured restoration durations and state, and require reviewed runtime acceptance. Keycloak's independently observed exit 137 explains the later unavailable authentication fixture; it does not establish the cause of the earlier focus timeout.

- Reviewed acceptance: the final validator passes all nine authenticated scenarios with 108 retained native key observations and zero circuit errors. Launcher focus returns in 30/33/29ms. Startup network/circuit negotiation is awaited before interacting with prerendered controls; no visibility assertion is skipped. The failed historical five-second observation remains retained, with its cause explicitly unproven.

- Final independent recheck verifies the accepted artifact/hash, all nine runtime scenarios and all seven review corrections. No outstanding finding or newly deferred work remains in this batch.
