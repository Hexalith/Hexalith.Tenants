---
title: 'DW-113: Localized fallback for unsupported tenant-list reasons'
type: 'bugfix'
created: '2026-10-20'
status: 'done'
route: 'oneshot'
review_loop_iteration: 1
baseline_commit: '6c5e3e10204c82fd22226755e5c13e33fede1953'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** `ListSurfaceStates` constructs a reason resource key for every non-None `TenantListReason`, although only five of the eleven reasons have that resource in English and French. Unsupported reasons can therefore display internal resource names instead of operator-facing copy. DW-113 is the oldest actionable ledger entry with no dependencies; current production callers have not been shown to reach its unsupported inputs.

**Approach:** Retain existing translated reason messages when available and otherwise render the existing localized message for the surface kind. Preserve the search-empty message that distinguishes a page with further results, current controls and announcement semantics. Verify supported, unsupported, None and invalid reasons against shipped English and French resources. Close only DW-113 and reconcile its maintenance index after verification; then commit and push on main as requested.

</frozen-after-approval>

## Implementation Notes

- Entry revision: `6c5e3e10204c82fd22226755e5c13e33fede1953`; clean main. No epic story or sprint-status update applies.
- No unresolved intent choices, data migration, dependency change or new public API. Commit/push are explicitly authorized. The implementation footprint is the shared component, a focused component test class, this spec, validation evidence and the ledger/index.
- Read repository baseline, UX/Git guidance, build configuration, resource values, relevant architecture/UX constraints and current callers. An independent read-only investigation confirms the latent defect and recommends preserving the separate notice vocabulary rather than reusing notice messages on error surfaces.
- Reuse the existing surface resources and `CultureBoundTenantsLocalizer`; test actual rendered paragraphs with real shipped resources. Resolve a reason using localization metadata, then fall back when its resource is absent. Preserve all existing resource values.
- The unchanged component fails 160 of the new 280 rendered cases; all six unsupported reasons and both invalid enum values reproduce raw-key output across both cultures and every surface. The 120 None/supported-reason controls pass. Initial Debug source build has zero warnings/errors.
- Exact reproduction: `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 -v:q`, then `dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build -p:UseNuGetDeps=false -- --filter-class '*ListSurfaceStatesTests'`. Original output is in `/tmp/dw113-baseline-build.log` and `/tmp/dw113-baseline-tests.log`; see [complete evidence](dw-113-validation-evidence.md) for commands, configuration and final reports.
- The component now checks `LocalizedString.ResourceNotFound` before accepting reason copy. No reason-specific notices are repurposed, and the original surface/search-continuation fallback remains shared with the None case.
- Aspire baseline: 23 resources Running/Healthy, but the bounded Error telemetry query still returns three Memories errors. The started AppHost was stopped successfully before the final build. This does not establish an entirely healthy topology or browser qualification.
- Final Debug source build has zero warnings/errors; focused 280/280 and full UI 6564/6564 pass with zero failures/skips. Exact commands and original-production failures are retained in [validation evidence](dw-113-validation-evidence.md). Own-spec gitlink and whitespace checks pass.
- Ledger closure preserves all replaced fields and every unrelated record. All 656 IDs remain, 396 done/260 open across 235 exact-covering maintenance rows. Pinned commitlint 21.2.3 validates the exact full commit message with zero problems/warnings; origin fetch shows no divergence.
- Independent review requested three small test/documentation improvements. The expected message now follows availability in the independently queried shipped ResourceManager while still requiring all five established reason resources; rendered reset/refresh actions now prove callback delivery; the exact baseline commands and evidence paths are linked directly above. No production change or deferred follow-up is needed.
- The reviewer rechecked every correction and the ledger/index, confirmed all findings addressed and found no concrete remaining issue. Strengthened-test rechecks pass focused 280/280 and full UI 6564/6564, zero failures/skips, with a zero-warning/error Debug source rebuild. The test method is named in PascalCase per the baseline; a final compile/focused recheck covers that naming-only adjustment.

## Review Triage Log

- Low / patch: the fixed expected-key allowlist would need manual updates for a future reason translation. Expected availability now comes from shipped ResourceManager values; the existing five reason controls still assert their resources are present.
- Low / patch: no new test exercised reset/refresh callbacks with an unsupported reason, although the change leaves their markup intact. The existing 280-case theory now clicks each applicable rendered Fluent control and asserts one callback, with zero calls to the other action.
- Low / patch: the original implementation note omitted the reproduction command and log paths. Added the exact Debug/source build and original-production test commands, logs and validation-evidence link. Frozen intent is unchanged.
- Independent correction recheck: all three findings addressed; targeted ledger/index counts and isolation verified; no remaining concrete issue or deferred follow-up.
