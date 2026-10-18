---
title: 'DW-622 evaluated Tenants UI read endpoint regression'
type: 'chore'
created: '2026-10-18'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
baseline_commit: '482b04769855531ec5630e01b4c0403444bad0d5'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** DW-622 records that source-text assertions cannot detect an AppHost override redirecting the UI's authorized tenant reads away from the generated Tenants API.

**Approach:** Evaluate the actual AppHost UI environment after all environment callbacks in both Keycloak and symmetric authentication modes. Prove its final `Tenants__BaseAddress` references and resolves to the `tenants-api` HTTPS endpoint. Preserve runtime wiring, existing user changes, and unrelated deferred work.

</frozen-after-approval>

## Implementation Notes

- Investigation: `src/Hexalith.Tenants.AppHost/Program.cs` already configures the correct endpoint. `tests/Hexalith.Tenants.AppHost.Tests/AppHostSecurityResourceGraphTests.cs::GetEnvironmentAsync` evaluates the resource's environment callbacks in order; reuse this fixture without starting the app. `.github/workflows/ci.yml` already includes this project in the blocking integration lane.
- Intent gaps: none. Irreversible actions: none. Footprint: one existing test file, this spec, validation evidence, and DW-622-only ledger/index reconciliation. No public API or runtime change.
- Verify both authentication modes against the actual endpoint resource/name and deterministic allocated HTTPS URL. Qualify refusal of a later wrong-resource or HTTP override with isolated test-build overlays, restoring normal build outputs in finally. Run the maintained per-project AppHost suite and an ordinary Debug source build.
- Entry revision: `482b04769855531ec5630e01b4c0403444bad0d5`. The pre-existing browser/workflow edits were externally committed during read-only investigation; preserve their file hashes. Do not modify either file.
- Existing Aspire session: read-only describe reports EventStore, security, Tenants, Tenants API and UI running/healthy; the live UI read address equals the API HTTPS address. Reuse the session without restarting or stopping it. The regression evaluates a separate non-started model.
- Initial ordinary Debug source build: zero warnings/errors; maintained AppHost suite: 8 passed, zero failed/skipped. The first mutation harness had a trailing quote in its temporary XML compile path and failed with CS2001 before executing controls. Its finally block rebuilt ordinary source and all eight tests passed. The corrected harness uses XML element construction and reuses built dependency assemblies for isolated AppHost/test compilation.
- Independent review corrections: allocate the API HTTPS endpoint before evaluating callbacks and apply a five-second cancellation deadline to callback waits and URL resolution. Normalize double-underscore/colon and case-equivalent read configuration keys and require one unambiguous binding. Extend controls with uppercase/colon aliases and an override conditional on endpoint allocation. These remain within the original read-endpoint qualification goal.


- Final qualification: all five later override variants reject exactly the two new cases while six existing cases pass; the forced ordinary-source restoration passes 8/8. All twelve isolated compilations have zero warnings/errors. The earlier incremental restoration retained an overlay assembly and correctly failed 6/8; the final wrapper forces compilation and verifies restoration in finally.
- Final ordinary Debug source graph build has zero warnings/errors and the maintained AppHost suite passes 8/8 with zero failures/skips. The independent correction recheck accepts both review fixes. Durable commands, results, initial harness failures and scope limits are in `dw-622-validation-evidence.md`.
- Only DW-622 closes: all 655 IDs remain, 380 done/275 open across 247 exactly covering maintenance rows; original fields and unrelated blocks/rows remain preserved. Story gitlink and diff checks pass. The pinned commitlint CLI accepts the exact full commit candidate with zero problems/warnings.
- External browser work was independently committed as `579ec157ac38eb30593e736042aedac4d1acd08d`; do not include or attribute it to this item. No runtime wiring, package/dependency pointers, archive or sprint tracking changes belong to DW-622.

- Final artifact recheck independently confirms all five control outcomes, twelve clean compilations, exact ledger/index coverage and unrelated-record preservation, with no further findings or deferrals.

## Review Triage Log

- medium — patched: evaluating callbacks before endpoint allocation could miss a supported allocation-dependent override; an awaited endpoint callback also had no cancellation bound. Allocate the API HTTPS endpoint first and pass a cancellation token through the helper with bounded callback awaits.
- medium — patched: the case-sensitive exact-key lookup missed additional uppercase or colon-delimited configuration aliases. Match normalized configuration keys and reject multiple equivalent bindings before checking the resource, HTTPS name and resolved URL.
