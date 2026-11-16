# Publication and recovery review corrections

The owning change set contains ten files: the nightly workflow, the strict report validator's policy import/extraction, two private PowerShell files, one finite Python control file and the five requested C# regression classes. `review-corrections.patch` is a delta against this packet's retained `pre-fix` owner bytes. `final-owner-inputs` retains the complete final affected files; `summary.json` binds every final hash. All prior evidence packets remain untouched.

## Publication

`Normalize-ArtifactText` and `Test-UnsafeText` now live in `eng/mutation-artifact-policy.ps1`. Their function bodies are exact canonical copies of the former validator functions. `policy-preservation.json` proves both bodies and every remaining validator statement are unchanged after removing the new import. No native verifier, tool, mutation config/predicate/threshold, manifest, triage or protection change occurred.

`eng/prepare-mutation-artifacts.ps1` uses that same policy on the finite artifact names before publication. It preflights all selected text before writing normalized content, refuses linked paths and directories occupying allowed filenames, and preserves safe failed-report status/receipt values. Both segment and aggregate uploads now require `always() && steps.publication.outcome == 'success'`; mutation failures remain failures. The aggregate summary shares that guard. The original strict BOTH invocation and separate successful-segment requirement remain in effect.

Nine final publication tests passed in attempt05. They executed18 actual PowerShell guard processes: two safe failed-report positives and16 refusals, including actual unsafe content in both aggregate segment trees/summaries, absent output, linked file/directory and a directory occupying an allowed report name. These are finite synthetic policy fixtures, not mutation qualification. No GitHub upload/download service was executed.

## Regression assertions

The renderer case now asserts canonical contractId/schemaVersion/protocol and independent renderer-ID fingerprint sensitivity. Razor cases assert each enum member's exact slot and all relevant initial plain-text fallback branches, actual declaration-order iteration, and source-valid initial generated compilation. Template cases assert each included original packed version, the healthy descriptor's full projection/role/type/version and count, and its preservation. All four recovery helpers now restore the original damaged syntax tree on the same driver and compare full diagnostics, original fallback/suppression output and healthy output after the reverse edit. Every existing assertion remains.

Focused C# receipts cover53 unique cases: the final unchanged ErrorFollowthroughBehaviorTests bytes passed all25 cases in attempt01; after correcting test assumptions, the four current recovery classes passed all28 cases in attempt02. Both builds used the actual original project, Release/package mode, MTP and normal analyzers, with zero build warnings/errors. This is explicitly split evidence, not one final combined53-case run. Full verification remains parent-owned.

## Commands, retained attempts and limits

The complete commands/environment/source archives/working overlays are in each `verification.json`; all raw stdout and TRX files remain beside them. The main command was `TMPDIR=/dev/shm run-with-temp python3 .../fc-review-publication-and-recovery/run-focused.py`. Build/test commands select only the five edited classes initially and four changed recovery classes on retry. Final publication runs use `python3 -B tests/eng/test_mutation_artifact_publication.py` inside fresh wrappers. Actionlint1.7.12 passed on the identical final workflow bytes in attempt01.

- Attempt01 retains53 cases (50pass/3failure) and initial publication assertion failures. Roslyn diagnostic ordering, multiple actual rendering sites and wrapped PowerShell stderr were test assumptions, not new product defects. Exact inputs/raw logs/TRX remain.
- Attempt02 retains28passing corrected recovery cases and seven passing initial publication methods. Diagnostic comparison preserves identity/count/location/message while disregarding enumeration order; badge checks cover every applicable rendering site.
- Attempt03 retains two actual pre-fix shape admissions. Refusal tests failed as expected, demonstrating the guard's initial filesystem-shape gap without performing an upload.
- Attempt04 retains a fail-closed StrictMode Directory property error introduced while expanding enumeration; attempt05 fixes the getter and passes all nine methods.

`final-audit.py` verifies6461 unrelated owner input files, all21 previously sealed lifetime evidence files, the exact successful class hashes and actual TRX/process counts. All five wrapper roots are removed; no process retains their TMPDIR. No owned work remains running. No full mutation rerun, accepted triage, source-wide qualification, Git/remote mutation or authority update was performed. The packet remains in-review for the parent's independent review and broader verification.
