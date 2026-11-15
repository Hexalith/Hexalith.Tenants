---
title: 'Align current Builds contract fixtures with the published SDK cohort'
type: 'bugfix'
created: '2026-11-03'
status: 'done'
route: 'oneshot'
baseline_commit: 'be43b928ce700ae7433b1d078eec356ccd6079f8'
owning_repository_baseline_commit: '10163c3e4ffd84ddb9b2831dea767a04da04f2ef'
gitlink_audit_ref: 'ae40c9657175ae5567cdd182384d3840af64d8a2'
context:
  - '/home/administrator/projects/hexalith/tenants/AGENTS.md'
  - '/home/administrator/projects/hexalith/tenants/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
---

<frozen-after-approval reason="User authorized dependency updates and fixing all CI failures without human checkpoints">

## Intent

Update current synthetic Builds schema/manifest/readiness fixtures to the newly selected, published EventStore 4.0.2 cohort. Preserve the existing positive and negative contract meanings and their cryptographic checks. Do not weaken validators or assertions, change production behavior to accept stale cohorts, or rewrite historical publication evidence.

</frozen-after-approval>

## File List

- `references/Hexalith.Builds`
- `references/Hexalith.FrontComposer`

- `references/Hexalith.EventStore`
- `references/Hexalith.McpCli`
- `references/Hexalith.Memories`
- `references/Hexalith.Platform`

## Completion Notes List

- The root release sync moved `references/Hexalith.Builds` to the verified fixture repair revision after the EventStore 4.0.2 catalog update.
- That same root sync moved `references/Hexalith.FrontComposer` to its already verified Firefox CI repair revision; its implementation is recorded in the companion spec.
- `references/Hexalith.Builds` 10163c3e4ffd84ddb9b2831dea767a04da04f2ef -> 70850b18e0d0b80facd2df0809974020dd935732
- `references/Hexalith.FrontComposer` e4df7256be0e89b48c830c0792033ecfeabec9ad -> f842711d67004200e3248b3c42b7d9bcb2146907

Historical audit reconciliation (2026-11-15): `ae40c9657175ae5567cdd182384d3840af64d8a2` is the latest committed story-associated pointer event before the current documentation-only repair. The unchanged baseline is `be43b928ce700ae7433b1d078eec356ccd6079f8`. The complete baseline-to-endpoint inventory below includes incidental workspace dependency movement; it records historical range contents and grants no runtime, publication, enrollment or maintainer approval. Original implementation notes, checklists and source-specific receipts retain their original attribution.

| Dependency | Historical baseline | Historical audit endpoint |
| --- | --- | --- |
| `references/Hexalith.Builds` | `10163c3e4ffd84ddb9b2831dea767a04da04f2ef` | `725754075e1d6369430a3adf94d46fb71e6eaf9e` |
| `references/Hexalith.EventStore` | `f06abada8dc3aab4a5fd240847fd680d515c98a5` | `16dff1f018e8a68a9d244975e8228d96429eb61c` |
| `references/Hexalith.FrontComposer` | `e4df7256be0e89b48c830c0792033ecfeabec9ad` | `b105c846c31844441f1cff84b5c187da0eff3546` |
| `references/Hexalith.McpCli` | `edaecb04b1a8edf78f6ad28e5bf253db594003f4` | `58afd4766849e33e75471c9047ee945025ca3b6e` |
| `references/Hexalith.Memories` | `d9d61d8a95c7dc8827acf68ae723859711703729` | `d959bdae5af9c22f53573eda71c091cb5c0aae4c` |
| `references/Hexalith.Platform` | `1f5958e24d1b414d8956d079270f89556e23415d` | `8432397c66ebc0fd22e5671ce620a7d2139f7453` |

The original owning-repository baseline `10163c3e4ffd84ddb9b2831dea767a04da04f2ef` and creation record `35ddf94679ea92fb937b59f4202c4352bf7edf49` remain historical provenance. The corrected superproject baseline already exists in both this endpoint and the event-base story, so this later audit uses that unchanged committed start. Its earlier correction endpoint was `35ddf94679ea92fb937b59f4202c4352bf7edf49`; the broader endpoint above additionally retains the later story-associated pointer event.

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

## Resumed Review — 2026-11-15

The requested task already exists as the historical Builds repair
`70850b18e0d0b80facd2df0809974020dd935732`, from
`10163c3e4ffd84ddb9b2831dea767a04da04f2ef`. No additional production or fixture
edit is needed. This continuation reviews that exact 106-file schema/fixture
patch, excluding the separately owned package audit and unrelated later work.
The complete parent diff since the original baseline is retained separately as
audit context; it is not attributed to this task.

The current Builds checkout is
`e2bbbd0893f586e4d447ef8358ff747ea3d34435` at verification time. Its independent
frozen v1 contract uses EventStore 4.2.0, Dapr runtime/SDK 1.19.1 and FrontComposer
5.0.0; the current catalog selects EventStore 5.1.0 and FrontComposer 6.0.0.
The historical 4.0.2 repair does not authorize relabelling these later contracts.

Three independent review layers completed. Edge-case review found no issues;
verification-gap review found no gaps after tracing the historical tests and CI
enrollment. Blind findings were individually classified below. Previously
recorded dispositions remain carried; two additional pre-existing fixture
follow-ups are deferred. There are no new patch, intent-gap or bad-spec entries.

| Finding | Verdict | Evidence and disposition |
| --- | --- | --- |
| Resumed blind 1: Source readiness Dapr metadata differs from the manifest | low | Carried from original Blind 1: the unchanged baseline already reports 1.18.0 against 1.18.7. Keep the existing defer disposition; do not defer it again. |
| Resumed blind 2: Source readiness topology names differ from the manifest | low | Both baseline and repair describe module-a/module-b against the manifest's p0-fixture-orders/p0-fixture-inventory. This descriptive fixture inconsistency predates the repair and does not change the Source-profile binding checks; defer reconciliation of this metadata. |
| Resumed blind 3: Source readiness needs persisted assertions and sequences | false | This artifact exercises Source readiness, whose validator checks successful executed tests and command/manifest/profile/filter/fixture bindings. Persisted runtime qualification is a separate G4 acceptance path with its own positive bound artifacts and assertion checks. The spec explicitly makes no production qualification claim. |
| Resumed blind 4: non-test command negative has several independent invalid bindings | low | Baseline binding-mismatch.yaml and down-run.json already combine a down command, null profile/filter and an intentionally incorrect manifest hash. That fixture alone cannot isolate rejection of the command; defer improving this pre-existing control without changing the current repair's negative meanings. |
| Resumed blind 5: add 4.0.0 and 4.0.1 schema/manifest negatives | low | Carried from original Blind 3/4: exact schema const and schema/runtime parity already cover the inequality. Keep the rejected disposition for optional value-specific variants. |
| Resumed blind 6: add a record-level retired EventStore acceptance control | low | Carried from original Blind 5: the exact EventStore comparison and existing pin-mismatch coverage are unchanged. Keep the rejected disposition for optional specialization. |
| Resumed blind 7: add correctly hashed stale EventStore run topology | low | Carried from original Blind 6: no runtime validation branch changed, and exact artifact/root pin checks and bound negatives remain exercised. Keep the rejected disposition. |
| Resumed blind 8: add stale EventStore cleanup topology | low | Carried from original Blind 7: existing cleanup ownership/command controls and exact platform comparisons remain unchanged. Keep the rejected disposition. |
| Resumed blind 9: add neutral-pin checks for malformed negatives | false | No new neutral-field drift is demonstrated: all 106 patched files contain only the intended version/hash substitutions, while malformed and superseded controls preserve their original outcomes. Early rejection of malformed input is deliberate; the independent valid-manifest and schema/runtime parity tests cover admitted pins. |
| Resumed blind 10: add repeatable fixture refresh tooling | low | Carried from original Blind 10: adding a maintained refresh utility is beyond a direct correction. The independent byte audit confirms all changed hashes bind the same fixture paths and all deliberate negative outcomes remain covered. Keep the rejected disposition. |

## Verification — 2026-11-15

Evidence is retained under
`_bmad-output/implementation-artifacts/evidence/fix-builds-sdk-cohort-fixtures-2026-11-15/`.

- `historical-byte-audit.json`: all 106 repaired files contain only the intended
  EventStore substitutions and dependent hashes, with original line endings
  preserved. All 29 changed hash bindings identify the same fixture paths;
  positive readiness manifest/artifact and acceptance run/cleanup bindings match.
- `run-with-temp python3 /home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/evidence/fix-builds-sdk-cohort-fixtures-2026-11-15/verify-current.py`:
  the Module.Tests Debug build passed with zero warnings/errors; 298 selected
  manifest/platform-manifest/catalog tests passed, with no failures or skips.
  The first evidence attempt failed because the temporary source copy omitted
  the `.git` marker used by readiness repository-root discovery. Its exact
  failed result is retained under `initial-snapshot-without-git-marker/`.
- The verification script now supplies that repository-root marker without
  changing repository source. Rerunning the affected lane with the same command
  plus `--evidence-only` passed the Evidence.Tests Debug build with zero
  warnings/errors and all 119 readiness/acceptance/selected-cleanup/command
  tests, with no failures or skips. Disposable copies and outputs were removed
  by the wrapper after each job exited.
- `current-verification.json` retains exact commands and exit codes for the two
  successful builds and 417 passing focused tests. Historical full-suite and
  G4 package-contract results above remain historical observations; this
  continuation does not claim a new full-suite, package or production run.
- `run-with-temp python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-fix-builds-sdk-cohort-fixtures.md --ref 35ddf94679ea92fb937b59f4202c4352bf7edf49`:
  passed for both declared historical pointers. `git diff --check` passed in
  the parent and Builds repositories. Builds remains clean; the parent review
  notes and evidence remain unstaged, preserving the user's unrelated changes
  and the task's existing instruction not to stage, commit or push.
