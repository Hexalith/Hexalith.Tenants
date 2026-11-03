---
title: 'Align current Builds contract fixtures with the published SDK cohort'
type: 'bugfix'
created: '2026-11-03'
status: 'in-review'
route: 'oneshot'
baseline_commit: '10163c3e4ffd84ddb9b2831dea767a04da04f2ef'
context:
  - '/home/administrator/projects/hexalith/tenants/AGENTS.md'
  - '/home/administrator/projects/hexalith/tenants/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
---

<frozen-after-approval reason="User authorized dependency updates and fixing all CI failures without human checkpoints">

## Intent

Update current synthetic Builds schema/manifest/readiness fixtures to the newly selected, published EventStore 4.0.2 cohort. Preserve the existing positive and negative contract meanings and their cryptographic checks. Do not weaken validators or assertions, change production behavior to accept stale cohorts, or rewrite historical publication evidence.

</frozen-after-approval>

## Implementation Notes

- Work in `references/Hexalith.Builds`. Main is at the baseline above, already pushed. The root agent has separately regenerated `Tools/package-version-audit.json`; preserve that dirty file exactly and do not stage/commit/push anything. Its live validation and generator suite pass; root is running validator fixture tests. No nested submodule initialization or skill loading under references.
- Current CI `41556709977`: Package catalog contracts fails only because the prior audit retained 4.0.0 (root has fixed this). Additional .NET tests have 98 failures among 2,733, all explained by stale manifest HXM016 validation; `ManifestValidationTests.PlatformPinSchemaAndRuntimeValidationRemainInParity` explicitly expects catalog 4.0.2 while schema const is 4.0.0. Build/package contracts compile and pack successfully, then reject source readiness evidence in `Tools/test-g4-tool-package-contracts.ps1:615`.
- Inspect tracked owning guidance, configuration, current schema, fixtures, fixture tests, and relevant recent commits before editing. The previous `6585f5860e87c146c26aa066d3a52b94eb56f271` commit and its preceding readiness repair commits show how to preserve these same coupled fixture bindings.
- Update `schemas/hexalith.module-manifest.v1.json` platform eventStoreVersion const to 4.0.2; update current positive and executable manifests under `test/fixtures/module/` plus unrelated negative manifests whose platform pins must be valid for their intended other failure. Preserve intentional tampered/superseded SDK controls.
- Refresh the synthetic positive source-readiness artifact `test/fixtures/evidence/positive/evidence/release-passed.json`: topology EventStore pin and invocation manifestHash must identify the updated positive manifest's actual bytes. Recompute its SHA-256 references in readiness YAML fixtures and other current synthetic fixture bindings as needed. Preserve deliberately incorrect hashes and all intended negative outcomes; never hardcode success or suppress errors.
- Run the focused schema/manifest tests, relevant source-readiness/evidence fixtures, and the G-4 package contract check that failed. Existing broad Module.Tests may be run once if necessary to prove all 98 failures are fixed; run projects individually. Do not rerun the root-owned audit generator/validator suites. Keep artifact line endings required by `.gitattributes` and preserve exact byte-bound fixtures except for intentional content and binding updates.
- Return changed paths and exact verification results, distinguishing synthetic fixture repairs from production qualification. Root owns final review, commitlint, commits, pushes, and remote CI.

- Implementation completed in 106 schema/fixture files. Changes are limited to the selected 4.0.2 version and dependent SHA-256 bindings, with deliberate tampered/superseded pins, expected negative outcomes, historical evidence, and original line endings preserved. The separately root-owned audit file SHA-256 remains 77b0ec8c45ce38353ba98cbd1dfb34499a1819f5b17ee6cb5a6da09aa7ee8b30.
- Verification passed: 51 manifest/schema tests; 116 readiness/acceptance/selected-cleanup tests; full Module.Tests total 2733 with zero failures/errors and 3 pre-existing environment skips; G4 package contracts with -RequireControls -SkipSourceValidation exit 0; two Release builds with zero warnings/errors; git diff --check. G4 evidence: /tmp/builds-sdk-cohort-g4-987b32ca0e6b44ad96cd9023f033de0c/g4-tool-package-inventory.json. This is synthetic fixture/package-contract qualification, not a claim of clean-source release eligibility.

## Review Triage Log

| Finding | Verdict | Evidence and disposition |
| --- | --- | --- |
| Blind 1: source readiness fixture Dapr metadata 1.18.0 differs from manifest 1.18.7 | low; defer | Verified this exact inconsistency exists at the unchanged baseline. Source fixtures are synthetic and do not establish runtime acceptance; retain as a separate existing metadata follow-up. |
| Blind 2: readiness validation does not bind that Dapr field | low; defer | Existing Source readiness behavior, not introduced here. Full G4 acceptance independently checks all supported platform pins in G4P0AcceptanceValidator. Assess the intended Source-profile metadata contract together with the preceding follow-up; no validator weakening or new guard is part of this fixture update. |
| Blind 3: add specific retired 4.0.0 manifest | low; rejected | Optional data specialization. Existing generic superseded-pin negatives and the schema/runtime parity test exercise the same inequality, with no new branch introduced by this constant update. |
| Blind 4: explicitly test schema rejection of 4.0.0 and 4.0.1 | low; rejected | Schema const is 4.0.2 and existing mismatch/parity tests already cover rejection semantics. Additional value-by-value tests would mirror fixture data. |
| Blind 5: acceptance root old EventStore control | low; rejected | Optional specialization of existing exact pin checks. G4P0AcceptanceValidator explicitly compares EventStore, Dapr runtime/SDK, and FrontComposer fields; current pin-mismatch coverage and unchanged logic remain exercised. |
| Blind 6: correctly hashed old-pin persisted evidence | low; rejected | No runtime logic changed. Existing exact artifact/root pin comparisons and bound negative evidence tests pass; this proposes additional value-specific coverage. |
| Blind 7: old-pin cleanup artifact | low; rejected | Existing foreign-run and command controls plus exact artifact/root platform comparisons remain intact; no new validation path was added. |
| Blind 8: old-pin cancelled/prerequisite controls | low; rejected | Existing cancellation/prerequisite cases and native-report refusals remain unchanged and pass; optional extra data variants do not demonstrate a regression. |
| Blind 9: audit implies pending compatibility despite selected 4.0.2 | false | The packet explicitly distinguishes current registry/consumer observations from runtime acceptance. Its selected/audited/latest values correctly match 4.0.2, and this spec labels synthetic qualification without claiming release eligibility. |
| Blind 10: add reusable fixture-refresh tooling | low; rejected | Future maintenance improvement requiring new tooling beyond a direct correction. Root independently read every changed fixture and verified all edits are only version literals and SHA-256 replacements; existing independent negative-outcome oracles passed. |

Edge-case review returned no findings. Verification-gap review found no gaps and additionally passed all 7 existing synthetic-preparation tests. One grouped pre-existing Source readiness metadata/binding follow-up is recorded separately.
